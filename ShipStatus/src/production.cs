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
    public Dictionary<string, string> statusDotColour;
    public Dictionary<string, VRage.MyFixedPoint> queueItems;
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
    public char[] splitNewline;

    public ProductionDetails(Program program, Template template) {
        this.program = program;
        this.template = template;
        this.blocks = new List<IMyProductionBlock>();
        this.productionItems = new List<MyProductionItem>();
        this.productionBlocks = new List<ProductionBlock>();
        this.blockStatus = new Dictionary<ProductionBlock, string>();
        this.queueItems = new Dictionary<string, VRage.MyFixedPoint>();
        this.statusDotColour = new Dictionary<string, string>() {
            { "Broken", "dimred" },
            { "Idle", "dimgray" },
            { "Working", "dimgreen" },
            { "Blocked", "dimyellow" }
        };
        this.queueBuilder = new StringBuilder();
        this.splitNewline = new[] { '\n' };

        this.Reset();
    }

    public void Reset() {
        this.Clear();

        if (this.program.config.Enabled("power")) {
            this.RegisterTemplateVars();
        }
    }

    public void Clear() {
        this.blocks.Clear();
        this.productionItems.Clear();
        this.productionBlocks.Clear();
        this.blockStatus.Clear();
        this.queueItems.Clear();
        this.idleTime = 0;
        this.timeDisabled = 0;
        this.checking = false;
    }

    public void RegisterTemplateVars() {
        if (this.template == null) {
            return;
        }

        this.template.Register("production.status", () => this.status);
        this.template.Register("production.blocks",  (DrawingSurface ds, string text, DrawingSurface.Options options) => {
            bool first = true;
            foreach (KeyValuePair<ProductionBlock, string> blk in this.blockStatus) {
                if (!first) {
                    ds.Newline();
                }
                string status = blk.Key.Status();
                string blockName = $" {blk.Key.block.CustomName}: {status} {(blk.Key.IsIdle() ? blk.Key.IdleTime() : "")}";
                Color? colour = DrawingSurface.StringToColour(this.statusDotColour.Get(status));
                ds.TextCircle(colour, outline: false).Text(blockName);

                foreach (string str in blk.Value.Split(this.splitNewline, StringSplitOptions.RemoveEmptyEntries)) {
                    ds.Newline().Text(str);
                }
                first = false;
            }
        });
    }

    public void GetBlock(IMyTerminalBlock block) {
        if ((block is IMyAssembler || block is IMyRefinery) && !block.CustomName.Contains(this.productionIgnoreString)) {
            this.productionBlocks.Add(new ProductionBlock(this.program, block as IMyProductionBlock));
        }
    }

    public void GotBLocks() {
        this.productionBlocks = this.productionBlocks.OrderBy(b => b.block.CustomName).ToList();
    }

    public void Refresh() {
        if (this.productionBlocks.Count == 0) {
            return;
        }

        string itemName;
        bool allIdle = true;
        int assemblers = 0;
        int refineries = 0;

        this.blockStatus.Clear();
        this.status = "";

        foreach (var block in this.productionBlocks) {
            if (block == null || !Util.BlockValid(block.block)) {
                continue;
            }
            bool idle = block.IsIdle();
            if (block.block.DefinitionDisplayNameText.ToString() != "Survival Kit") {
                allIdle = allIdle && idle;
            }
            if (idle) {
                if (block.block is IMyAssembler) {
                    assemblers++;
                } else {
                    refineries++;
                }
            }

            this.queueItems.Clear();

            if (!block.IsIdle()) {
                block.GetQueue(this.productionItems);
                foreach (MyProductionItem i in this.productionItems) {
                    itemName = Util.ToItemName(i);
                    if (!this.queueItems.ContainsKey(itemName)) {
                        this.queueItems.Add(itemName, i.Amount);
                    } else {
                        this.queueItems[itemName] = this.queueItems[itemName] + i.Amount;
                    }
                }
            }

            this.queueBuilder.Clear();
            foreach (var kv in this.queueItems) {
                this.queueBuilder.Append($"  {Util.FormatNumber(kv.Value)} x {kv.Key}\n");
            }

            this.blockStatus.Add(block, this.queueBuilder.ToString());
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

            // If any assemblers are on, make sure they are all on (in case working together)
            if (assemblers > 0) {
                foreach (var block in this.productionBlocks.Where(b => b.block is IMyAssembler).ToList()) {
                    block.Enabled = true;
                }
            }

            idleTime = 0;
            timeDisabled = 0;
            checking = false;
        }
    }
}

public class ProductionBlock {
    public Program program;
    public double idleTime;
    public IMyProductionBlock block;
    public bool Enabled {
        get { return block.Enabled; }
        set {
            if (block.DefinitionDisplayNameText.ToString() == "Survival Kit") {
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
        if (status == "Idle" || status == "Broken") {
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
        if (!this.block.IsFunctional) {
            return "Broken";
        } else if (this.block.IsQueueEmpty && !this.block.IsProducing) {
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
