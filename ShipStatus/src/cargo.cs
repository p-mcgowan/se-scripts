public struct FillPct {
    public VRage.MyFixedPoint current;
    public VRage.MyFixedPoint max;
    public string name;

    public FillPct(VRage.MyFixedPoint current, VRage.MyFixedPoint max, string name) {
        this.current = current;
        this.max = max;
        this.name = name;
    }
}

/*
 * CARGO
 */
public class CargoStatus : Runnable {
    public Program program;
    public List<IMyTerminalBlock> cargoBlocks;
    public Dictionary<string, VRage.MyFixedPoint> cargoItemCounts;
    public Dictionary<long, FillPct> containerPercentages;
    public List<MyInventoryItem> inventoryItems;
    public System.Text.RegularExpressions.Regex itemRegex;
    public System.Text.RegularExpressions.Regex ingotRegex;
    public System.Text.RegularExpressions.Regex oreRegex;
    public System.Text.RegularExpressions.Regex filterContainers;
    public VRage.MyFixedPoint max;
    public VRage.MyFixedPoint vol;
    public Template template;
    public List<float> widths;

    public string itemText;
    public float pct;

    public CargoStatus(Program program, Template template = null) {
        this.program = program;
        this.template = template;
        this.itemRegex = Util.Regex(".*/");
        this.ingotRegex = Util.Regex("Ingot/");
        this.oreRegex = Util.Regex("Ore/(?!Ice)");
        this.widths = new List<float>() { 0, 0, 0, 0 };

        this.cargoItemCounts = new Dictionary<string, VRage.MyFixedPoint>();
        this.inventoryItems = new List<MyInventoryItem>();
        this.cargoBlocks = new List<IMyTerminalBlock>();
        this.containerPercentages = new Dictionary<long, FillPct>();
        this.itemText = "";
        this.pct = 0f;

        this.Reset();
    }

    public void Reset() {
        this.Clear();
        this.filterContainers = Util.Regex(this.program.config.Get("cargoIgnore", "$^"));

        if (this.program.config.Enabled("cargo")) {
            this.RegisterTemplateVars();
        }
    }

    public void Clear() {
        this.cargoItemCounts.Clear();
        this.inventoryItems.Clear();
        this.cargoBlocks.Clear();
        this.containerPercentages.Clear();
    }

    public void RegisterTemplateVars() {
        if (this.template == null) {
            return;
        }

        this.template.Register("cargo.stored", () => $"{Util.FormatNumber(1000 * this.vol)} L");
        this.template.Register("cargo.cap", () => $"{Util.FormatNumber(1000 * this.max)} L");
        this.template.Register("cargo.fullString", () => this.Filled(this.vol, this.max));
        this.template.Register("cargo.bar", (DrawingSurface ds, string text, DrawingSurface.Options options) => this.CargoBar(ds, text, options, this.pct));
        this.template.Register("cargo.items", this.CargoItems);
        this.template.Register("cargo.item", this.CargoItem);
        this.template.Register("cargo.containers", this.CargoContainerPercentages);
        this.template.Register("cargo.containerBars", this.CargoContainerBars);
    }

    public string Filled(VRage.MyFixedPoint vol, VRage.MyFixedPoint max) {
        string capFmt = Util.GetFormatNumberStr(1000 * max);

        return $"{Util.FormatNumber(1000 * vol, capFmt)} / {Util.FormatNumber(1000 * max, capFmt)} L";
    }

    public void CargoContainerPercentages(DrawingSurface ds, string text, DrawingSurface.Options options) {
        if (this.containerPercentages.Count() == 0) {
            ds.Text(" ");

            return;
        }

        foreach (var item in this.containerPercentages) {
            string filled = this.Filled(item.Value.current, item.Value.max);
            float pct = (float)item.Value.current / (float)item.Value.max;

            ds.Text($"{item.Value.name}").SetCursor(ds.width, null).Text($"{filled} ({Util.PctString(pct)})", textAlignment: TextAlignment.RIGHT).Newline();
        }
        ds.Newline(reverse: true);
    }

    public void CargoContainerBars(DrawingSurface ds, string text, DrawingSurface.Options options) {
        if (this.containerPercentages.Count() == 0) {
            ds.Text(" ");

            return;
        }

        foreach (var item in this.containerPercentages) {
            string filled = this.Filled(item.Value.current, item.Value.max);
            float pct = (float)item.Value.current / (float)item.Value.max;
            DrawingSurface.Options barOpts = new DrawingSurface.Options();
            barOpts.text = $"{item.Value.name} {filled} ({Util.PctString(pct)})";
            barOpts.pct = pct;
            barOpts.custom = options.custom;
            barOpts.textColour = options.textColour;
            this.CargoBar(ds, text, barOpts, pct);
            ds.Newline();
        }

        ds.Newline(reverse: true);
    }

    public void CargoBar(DrawingSurface ds, string text, DrawingSurface.Options options, float pct) {
        Color defaultText = options.textColour ?? ds.surface.ScriptForegroundColor;
        string colourName = options.custom.Get("colourLow") ?? "dimgreen";
        options.textColour = DrawingSurface.StringToColour(options.custom.Get("textColourLow")) ?? defaultText;
        if (pct > 0.85) {
            colourName = options.custom.Get("colourHigh") ?? "dimred";
            options.textColour = DrawingSurface.StringToColour(options.custom.Get("textColourHigh")) ?? defaultText;
        } else if (pct > 0.60) {
            colourName = options.custom.Get("colourMid") ?? "dimyellow";
            options.textColour = DrawingSurface.StringToColour(options.custom.Get("textColourMid")) ?? defaultText;
        }

        options.pct = pct;
        options.fillColour = DrawingSurface.StringToColour(colourName);
        options.text = options.text ?? Util.PctString(pct);

        ds.Bar(options);
    }

    public void CargoItem(DrawingSurface ds, string text, DrawingSurface.Options options) {
        string name = options.custom.Get("name");
        VRage.MyFixedPoint itemCount;
        if (!this.cargoItemCounts.TryGetValue(name, out itemCount)) {
            itemCount = 0;
        }
        ds.Text(Util.FormatNumber(itemCount), options);
    }

    public void CargoItems(DrawingSurface ds, string text, DrawingSurface.Options options) {
        if (this.cargoItemCounts.Count() == 0) {
            ds.Text(" ");

            return;
        }

        if (ds.width / (ds.charSizeInPx.X + 1f) < 40) {
            foreach (var item in this.cargoItemCounts) {
                var fmtd = Util.FormatNumber(item.Value);
                ds.Text($"{item.Key}").SetCursor(ds.width, null).Text(fmtd, textAlignment: TextAlignment.RIGHT).Newline();
            }
        } else {
            this.widths[0] = 0;
            this.widths[1] = ds.width / 2 - 1.5f * ds.charSizeInPx.X;
            this.widths[2] = ds.width / 2 + 1.5f * ds.charSizeInPx.X;
            this.widths[3] = ds.width;

            int i = 0;
            foreach (var item in this.cargoItemCounts) {
                var fmtd = Util.FormatNumber(item.Value);
                ds
                    .SetCursor(this.widths[(i++ % 4)], null)
                    .Text($"{item.Key}")
                    .SetCursor(this.widths[(i++ % 4)], null)
                    .Text(fmtd, textAlignment: TextAlignment.RIGHT);

                if ((i % 4) == 0 || i >= this.cargoItemCounts.Count * 2) {
                    ds.Newline();
                }
            }
        }
        ds.Newline(reverse: true);
    }

    public void GetBlock(IMyTerminalBlock block) {
        if (block is IMyCargoContainer || block is IMyShipDrill || block is IMyShipConnector || block is IMyAssembler || block is IMyRefinery) {
            if (!this.filterContainers.Match(block.CustomName).Success) {
                this.cargoBlocks.Add(block);
            }
        }
    }

    public void GotBLocks() {}

    public void Refresh() {
        if (this.cargoBlocks == null) {
            return;
        }

        this.cargoItemCounts.Clear();
        this.inventoryItems.Clear();
        this.containerPercentages.Clear();
        this.max = 0;
        this.vol = 0;
        this.pct = 0f;
        string fullName = "";
        string itemName = "";

        foreach (var c in this.cargoBlocks) {
            if (!Util.BlockValid(c)) {
                continue;
            }
            var inv = c.GetInventory(0);
            if (inv.MaxVolume == 0) {
                continue;
            }
            this.containerPercentages.Add(c.EntityId,  new FillPct(inv.CurrentVolume, inv.MaxVolume, c.CustomName));
            this.vol += inv.CurrentVolume;
            this.max += inv.MaxVolume;

            this.inventoryItems.Clear();
            inv.GetItems(this.inventoryItems);
            for (var i = 0; i < this.inventoryItems.Count; i++) {
                fullName = this.inventoryItems[i].Type.ToString();
                itemName = this.itemRegex.Replace(fullName, "");
                if (itemName == "Stone") {
                    if (this.ingotRegex.IsMatch(fullName)) {
                        itemName = "Gravel";
                    } else {
                        itemName = "Stone";
                    }
                } else if (this.ingotRegex.IsMatch(fullName)) {
                    itemName += " Ingot";
                } else if (this.oreRegex.IsMatch(fullName)) {
                    itemName += " Ore";
                }

                var itemQty = this.inventoryItems[i].Amount;
                if (!this.cargoItemCounts.ContainsKey(itemName)) {
                    this.cargoItemCounts.Add(itemName, itemQty);
                } else {
                    this.cargoItemCounts[itemName] = this.cargoItemCounts[itemName] + itemQty;
                }
            }
        }

        this.cargoItemCounts = this.cargoItemCounts.OrderBy(x => -x.Value.ToIntSafe()).ToDictionary(x => x.Key, x => x.Value);

        if (this.max != 0) {
            this.pct = (float)this.vol / (float)this.max;
        }

        return;
    }
}
/* CARGO */
