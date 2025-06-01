/*
 * PRODUCTION
 */

public struct ProductionBlock {
    public double idleTime;
    public IMyProductionBlock block;

    public ProductionBlock(IMyProductionBlock block) {
        this.idleTime = -1;
        this.block = block;
    }
}

public class ProductionDetails : Runnable {
    public Program program;
    public Template template;
    public List<MyProductionItem> productionItems;
    public Dictionary<long, ProductionBlock> productionBlocks;
    public List<IMyProductionBlock> blocks;
    public Dictionary<long, string> blockStatus;
    public Dictionary<string, string> statusDotColour;
    public Dictionary<string, VRage.MyFixedPoint> queueItems;
    public double productionCheckFreqMs = 2 * 60 * 1000;
    public double productionOnWaitMs = 5 * 1000;
    public double productionOutTimeMs = 3 * 1000;
    public string productionIgnoreString = "[x]";
    public string status;
    public StringBuilder queueBuilder;
    public double allIdleSince = 0;
    public double timeDisabled = 0;
    public bool checking = false;
    public double lastCheck = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
    public char[] splitNewline;

    public ProductionDetails(Program program, Template template) {
        this.program = program;
        this.template = template;
        this.blocks = new List<IMyProductionBlock>();
        this.productionItems = new List<MyProductionItem>();
        this.productionBlocks = new Dictionary<long, ProductionBlock>();
        this.blockStatus = new Dictionary<long, string>();
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
        this.productionBlocks.Clear();

        if (this.program.config.Enabled("power")) {
            this.RegisterTemplateVars();
        }
    }

    public void Clear() {
        this.blocks.Clear();
        this.productionItems.Clear();
        this.blockStatus.Clear();
        this.queueItems.Clear();
        this.allIdleSince = 0;
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
            if (this.blockStatus.Count() == 0) {
                ds.Text("");
                return;
            }

            foreach (long blockId in this.blockStatus.Keys.OrderBy(id => this.productionBlocks.Get(id).block.CustomName).ToList()) {
                ProductionBlock productionBlock = this.productionBlocks.Get(blockId);
                if (!this.productionBlocks.ContainsKey(blockId)) {
                    continue;
                }
                if (!first) {
                    ds.Newline();
                }
                first = false;

                string status = this.Status(productionBlock.block);
                string blockName = $" {productionBlock.block.CustomName}: {status} {(this.IsIdle(ref productionBlock) ? this.IdleTime(ref productionBlock) : "")}";
                Color? colour = DrawingSurface.StringToColour(this.statusDotColour.Get(status));
                ds.TextCircle(colour, outline: false).Text(blockName);

                foreach (string str in this.blockStatus[blockId].Split(this.splitNewline, StringSplitOptions.RemoveEmptyEntries)) {
                    ds.Newline().Text(str);
                }
            }
        });
    }

    public void GetBlock(IMyTerminalBlock block) {
        if (!this.productionBlocks.ContainsKey(block.EntityId) && (block is IMyAssembler || block is IMyRefinery) && !block.CustomName.Contains(this.productionIgnoreString)) {
            this.productionBlocks.Add(block.EntityId, new ProductionBlock(block as IMyProductionBlock));
        }
    }

    public void GotBLocks() {
        foreach (long key in this.productionBlocks.Keys.ToList()) {
            ProductionBlock productionBlock = this.productionBlocks.Get(key);
            if (!Util.BlockValid(productionBlock.block)) {
                this.productionBlocks.Remove(key);
            }
        }
    }

    public void Refresh() {
        this.blockStatus.Clear();
        this.status = "";

        if (this.productionBlocks.Count == 0) {
            return;
        }

        int workingAssemblers = 0;
        bool allIdle = true;
        string itemName;
        foreach (long blockId in this.productionBlocks.Keys.ToList()) {
            ProductionBlock productionBlock = this.productionBlocks.Get(blockId);
            if (!Util.BlockValid(productionBlock.block)) {
                this.productionBlocks.Remove(blockId);
                continue;
            }

            this.UpdateStatus(ref productionBlock);
            bool idle = this.IsIdle(ref productionBlock);
            if (productionBlock.block.DefinitionDisplayNameText.ToString() != "Survival Kit") {
                allIdle = allIdle && idle;
            }
            if (!idle && productionBlock.block is IMyAssembler) {
                workingAssemblers++;
            }

            this.queueItems.Clear();

            if (!idle) {
                this.GetQueue(productionBlock.block, this.productionItems);
                foreach (MyProductionItem item in this.productionItems) {
                    itemName = Util.ToItemName(item);
                    if (!this.queueItems.ContainsKey(itemName)) {
                        this.queueItems.Add(itemName, item.Amount);
                    } else {
                        this.queueItems[itemName] = this.queueItems[itemName] + item.Amount;
                    }
                }
            }

            this.queueBuilder.Clear();
            foreach (var kv in this.queueItems) {
                this.queueBuilder.Append($"  {Util.FormatNumber(kv.Value)} x {kv.Key}\n");
            }

            this.blockStatus.Add(productionBlock.block.EntityId, this.queueBuilder.ToString());
            this.productionBlocks[blockId] = productionBlock;
        }

        double timeNow = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

        if (allIdle) {
            this.allIdleSince = (this.allIdleSince == 0 ? timeNow : this.allIdleSince);

            if (this.timeDisabled == 0) {
                foreach (ProductionBlock productionBlock in this.productionBlocks.Values) {
                    this.Enable(productionBlock.block, false);
                }
                this.timeDisabled = timeNow;
            } else {
                if (!this.checking) {
                    if (timeNow - this.lastCheck > this.productionCheckFreqMs)  {
                        // We disabled them over this.productionCheckFreqMs ago, and need to check them
                        foreach (var productionBlock in this.productionBlocks.Values) {
                            this.Enable(productionBlock.block, true);
                        }
                        this.checking = true;
                        this.lastCheck = timeNow;
                        this.status = $"Power saving mode {Util.TimeFormat(timeNow - this.allIdleSince)} (checking)";
                    }
                } else {
                    if (timeNow - this.lastCheck > this.productionOnWaitMs) {
                        // We waited 5 seconds and they are still not producing
                        foreach (var productionBlock in this.productionBlocks.Values) {
                            this.Enable(productionBlock.block, false);
                        }
                        this.checking = false;
                        this.lastCheck = timeNow;
                    } else {
                        this.status = $"Power saving mode {Util.TimeFormat(timeNow - this.allIdleSince)} (checking)";
                    }
                }
            }
            if (this.status == "") {
                this.status = $"Power saving mode {Util.TimeFormat(timeNow - this.allIdleSince)} " +
                    $"(check in {Util.TimeFormat(this.productionCheckFreqMs - (timeNow - this.lastCheck), true)})";
            }
        } else {
            if (this.productionBlocks.Values.ToList().Where(b => this.Status(b.block) == "Blocked").Any()) {
                this.status = "Production Enabled (Halted)";
            } else {
                this.status = "Production Enabled";
            }

            // If any assemblers are on, make sure they are all on (in case working together)
            if (workingAssemblers > 0) {
                foreach (ProductionBlock productionBlock in this.productionBlocks.Values.ToList().Where(b => b.block is IMyAssembler).ToList()) {
                    this.Enable(productionBlock.block, true);
                }
            }

            this.allIdleSince = 0;
            this.timeDisabled = 0;
            this.checking = false;
        }
    }

    public void GetQueue(IMyProductionBlock productionBlock, List<MyProductionItem> productionItems) {
        productionItems.Clear();
        productionBlock.GetQueue(productionItems);
    }

    public bool IsIdle(ref ProductionBlock productionBlock) {
        string status = this.Status(productionBlock.block);

        return (status == "Idle" || status == "Broken");
    }

    public bool UpdateStatus(ref ProductionBlock productionBlock) {
        string status = this.Status(productionBlock.block);
        if (status == "Idle" || status == "Broken") {
            if (productionBlock.idleTime == -1) {
                productionBlock.idleTime = this.Now();
            }

            return true;
        }

        if (status == "Blocked" && !productionBlock.block.Enabled) {
            this.Enable(productionBlock.block, true);
        }
        this.allIdleSince = -1;

        return false;
    }

    public string IdleTime(ref ProductionBlock productionBlock) {
        return Util.TimeFormat(this.Now() - productionBlock.idleTime);
    }

    public string Status(IMyProductionBlock block) {
        if (!block.IsFunctional) {
            return "Broken";
        }
        if (block.IsProducing) {
            return "Working";
        }
        if (block.IsQueueEmpty) {
            return "Idle";
        } else {
            return "Blocked";
        }
    }

    public double Now() {
        return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
    }

    public void Enable(IMyProductionBlock block, bool enabled) {
        if (block.DefinitionDisplayNameText.ToString() != "Survival Kit") {
            block.Enabled = (bool)enabled;
        }
    }
}
/* PRODUCTION */
