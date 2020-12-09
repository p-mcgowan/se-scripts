/*
 * PRODUCTION
 */
public ProductionDetails productionDetails;

public class ProductionDetails {
    public Program program;
    public Template template;
    public List<MyProductionItem> productionItems;
    public List<ProductionBlock> productionBlocks;
    public List<IMyProductionBlock> blocks;
    public Dictionary<ProductionBlock, string> blockStatus;
    public Dictionary<string, Color> statusDot;
    public double productionCheckFreqMs = 2 * 60 * 1000;
    public double productionOnWaitMs = 5 * 1000;
    public double productionOutTimeMs = 3 * 1000;
    public string productionIgnoreString = "[x]";
    public string status;
    public StringBuilder queueBuilder;
    public double idleTime = 0;
    public double timeDisabled = 0;
    public bool checking = false;
    public double lastCheck = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
    public char[] splitNeline;

    public ProductionDetails(Program program, Template template) {
        this.program = program;
        this.template = template;
        this.blocks = new List<IMyProductionBlock>();
        this.productionItems = new List<MyProductionItem>();
        this.productionBlocks = new List<ProductionBlock>();
        this.blockStatus = new Dictionary<ProductionBlock, string>();
        this.statusDot = new Dictionary<string, Color>() {
            { "Idle", DrawingSurface.stringToColour.Get("dimgreen") },
            { "Working", DrawingSurface.stringToColour.Get("dimyellow") },
            { "Blocked", DrawingSurface.stringToColour.Get("dimred") }
        };
        this.queueBuilder = new StringBuilder();
        this.splitNeline = new[] { '\n' };

        if (this.program.config.Enabled("power")) {
            this.GetProductionBlocks();
            this.RegisterTemplateVars();
        }
    }

    public void RegisterTemplateVars() {
        if (this.template == null) {
            return;
        }

        this.template.Register("production.status", () => this.status);
        this.template.Register("production.blocks",  (DrawingSurface ds, string text, Dictionary<string, string> options) => {
            foreach (KeyValuePair<ProductionBlock, string> blk in this.blockStatus) {
                string status = blk.Key.Status();
                string blockName = $"{blk.Key.block.CustomName}: {status} {(blk.Key.IsIdle() ? blk.Key.IdleTime() : "")}";
                ds.TextCircle(this.statusDot.Get(status)).Text(blockName).Newline();

                foreach (string str in blk.Value.Split(this.splitNeline, StringSplitOptions.RemoveEmptyEntries)) {
                    ds.Text(str).Newline();
                }
            }
        });
    }

    public void GetProductionBlocks() {
        this.blocks.Clear();
        this.program.GridTerminalSystem.GetBlocksOfType<IMyProductionBlock>(this.blocks, b =>
            b.IsSameConstructAs(this.program.Me) &&
            (b is IMyAssembler || b is IMyRefinery) &&
            !b.CustomName.Contains(this.productionIgnoreString)
        );
        this.productionBlocks.Clear();
        foreach (IMyProductionBlock block in this.blocks) {
            this.productionBlocks.Add(new ProductionBlock(this.program, block));
        }
        this.productionBlocks = this.productionBlocks.OrderBy(b => b.block.CustomName).ToList();
    }

    public void Refresh() {
        if (!this.productionBlocks.Any()) {
            return;
        }

        this.blockStatus.Clear();
        bool allIdle = true;
        this.status = "";
        int assemblers = 0;
        int refineries = 0;
        foreach (var block in this.productionBlocks) {
            bool idle = block.IsIdle();
            if (block.block.DefinitionDisplayNameText.ToString() != "Survival kit") {
                allIdle = allIdle && idle;
            }
            if (idle) {
                if (block.block is IMyAssembler) {
                    assemblers++;
                } else {
                    refineries++;
                }
            }
        }
        double timeNow = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

        if (allIdle) {
            idleTime = (idleTime == 0 ? timeNow : idleTime);

            if (timeDisabled == 0) {
                foreach (var block in this.productionBlocks) {
                    block.Enabled = false;
                }
                timeDisabled = timeNow;
            } else {
                if (!checking) {
                    if (timeNow - lastCheck > this.productionCheckFreqMs)  {
                        // We disabled them over this.productionCheckFreqMs ago, and need to check them
                        foreach (var block in this.productionBlocks) {
                            block.Enabled = true;
                        }
                        checking = true;
                        lastCheck = timeNow;
                        this.status = $"Power saving mode {Util.TimeFormat(timeNow - idleTime)} (checking)";
                    }
                } else {
                    if (timeNow - lastCheck > this.productionOnWaitMs) {
                        // We waited 5 seconds and they are still not producing
                        foreach (var block in this.productionBlocks) {
                            block.Enabled = false;
                        }
                        checking = false;
                        lastCheck = timeNow;
                    } else {
                        this.status = $"Power saving mode {Util.TimeFormat(timeNow - idleTime)} (checking)";
                    }
                }
            }
            if (this.status == "") {
                this.status = $"Power saving mode {Util.TimeFormat(timeNow - idleTime)} " +
                    $"(check in {Util.TimeFormat(this.productionCheckFreqMs - (timeNow - lastCheck), true)})";
            }
        } else {
            if (this.productionBlocks.Where(b => b.Status() == "Blocked").Any()) {
                this.status = "Production Enabled (Halted)";
            } else {
                this.status = "Production Enabled";
            }

            // If any assemblers are on, make sure they are all on (master/slave)
            if (assemblers > 0) {
                foreach (var block in this.productionBlocks.Where(b => b.block is IMyAssembler).ToList()) {
                    block.Enabled = true;
                }
            }

            idleTime = 0;
            timeDisabled = 0;
            checking = false;
        }

        foreach (var block in this.productionBlocks) {
            this.queueBuilder.Clear();
            // output += String.Format("{0}: {1} {2}\n", block.block.CustomName, block.Status(), (idle ? block.IdleTime() : ""));
            if (!block.IsIdle()) {
                block.GetQueue(this.productionItems);
                foreach (MyProductionItem i in this.productionItems) {
                    this.queueBuilder.Append($"  {Util.FormatNumber(i.Amount)} x {Util.ToItemName(i)}\n");
                }
            }

            this.blockStatus.Add(block, this.queueBuilder.ToString());
        }

        return;
    }
}

public class ProductionBlock {
    public Program program;
    public double idleTime;
    public IMyProductionBlock block;
    public bool Enabled {
        get { return block.Enabled; }
        set {
            if (block.DefinitionDisplayNameText.ToString() == "Survival kit") {
                return;
            }
            block.Enabled = value;
        }
    }

    public ProductionBlock(Program program, IMyProductionBlock block) {
        this.idleTime = -1;
        this.block = block;
        this.program = program;
    }

    public void GetQueue(List<MyProductionItem> productionItems) {
        productionItems.Clear();
        block.GetQueue(productionItems);
    }

    public bool IsIdle() {
        string status = this.Status();
        if (status == "Idle") {
            this.idleTime = (this.idleTime == -1) ? this.Now() : this.idleTime;
            return true;
        } else if (status == "Blocked" && !block.Enabled) {
            block.Enabled = true;
        }
        this.idleTime = -1;
        return false;
    }

    public string IdleTime() {
        return Util.TimeFormat(this.Now() - this.idleTime);
    }

    public string Status() {
        if (this.block.IsQueueEmpty && !this.block.IsProducing) {
            return "Idle";
        } else if (this.block.IsProducing) {
            return "Working";
        } else if (!this.block.IsQueueEmpty && !this.block.IsProducing) {
            return "Blocked";
        }
        return "???";
    }

    public double Now() {
        return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
    }
}
/* PRODUCTION */
