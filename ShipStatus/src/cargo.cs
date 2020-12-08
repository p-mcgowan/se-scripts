/*
 * CARGO
 */
CargoStatus cargoStatus;

public class CargoStatus {
    public Program program;
    public List<IMyTerminalBlock> cargo;
    public Dictionary<string, VRage.MyFixedPoint> cargoItemCounts;
    public List<MyInventoryItem> inventoryItems;
    public System.Text.RegularExpressions.Regex itemRegex;
    public System.Text.RegularExpressions.Regex ingotRegex;
    public System.Text.RegularExpressions.Regex oreRegex;
    public VRage.MyFixedPoint max;
    public VRage.MyFixedPoint vol;
    public Template template;
    public List<float> widths;

    public string itemText;
    public float pct;

    public CargoStatus(Program program, Template template = null) {
        this.program = program;
        this.template = template;
        this.itemText = "";
        this.pct = 0f;
        this.cargo = new List<IMyTerminalBlock>();
        this.cargoItemCounts = new Dictionary<string, VRage.MyFixedPoint>();
        this.inventoryItems = new List<MyInventoryItem>();
        this.itemRegex = Util.Regex(".*/");
        this.ingotRegex = Util.Regex("Ingot/");
        this.oreRegex = Util.Regex("Ore/(?!Ice)");
        this.widths = new List<float>() { 0, 0, 0, 0 };
        this.GetCargoBlocks();
        this.RegisterTemplateVars();
    }

    public void RegisterTemplateVars() {
        if (this.template == null) {
            return;
        }

        this.template.Register("cargo.stored", () => $"{Util.FormatNumber(1000 * this.vol)} L");
        this.template.Register("cargo.cap", () => $"{Util.FormatNumber(1000 * this.max)} L");
        this.template.Register("cargo.bar", this.RenderPct);
        this.template.Register("cargo.items", this.RenderItems);
    }

    public void RenderPct(DrawingSurface ds, string text, Dictionary<string, string> options) {
        Color colour = DrawingSurface.stringToColour.Get("dimgreen");
        if (this.pct > 60) {
            colour = DrawingSurface.stringToColour.Get("dimyellow");
        } else if (this.pct > 85) {
            colour = DrawingSurface.stringToColour.Get("dimred");
        }
        ds.Bar(this.pct, fillColour: colour, text: Util.PctString(this.pct));
    }

    public void RenderItems(DrawingSurface ds, string text, Dictionary<string, string> options) {
        if (ds.width / (ds.charSizeInPx.X + 1f) < 30) {
            foreach (var item in this.cargoItemCounts) {
                var fmtd = Util.FormatNumber(item.Value);
                ds.Text($"{item.Key}").SetCursor(ds.width, null).Text(fmtd, textAlignment: TextAlignment.RIGHT).Newline();
            }
        } else {
            this.widths[0] = 0;
            this.widths[1] = ds.width / 2 - ds.charSizeInPx.X;
            this.widths[2] = ds.width / 2 + ds.charSizeInPx.X;
            this.widths[3] = ds.width;

            int i = 0;
            foreach (var item in this.cargoItemCounts) {
                var fmtd = Util.FormatNumber(item.Value);
                ds
                    .SetCursor(this.widths[(i++ % 4)], null)
                    .Text($"{item.Key}")
                    .SetCursor(this.widths[(i++ % 4)], null)
                    .Text(fmtd, textAlignment: TextAlignment.RIGHT);

                if ((i % 4) == 0) {
                    ds.Newline();
                }
            }
        }
    }

// public Func<TResult> MethodAccess<TResult, TArg> (Func<TArg, TResult> func, TArg arg) {
//     return () => func(arg);
// }

    public void Clear() {
        this.itemText = "";
        this.pct = 0f;
        this.cargoItemCounts.Clear();
        this.inventoryItems.Clear();
    }

    public void GetCargoBlocks() {
        this.cargo.Clear();
        this.program.GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(this.cargo, c =>
            c.IsSameConstructAs(this.program.Me) &&
            (c is IMyCargoContainer || c is IMyShipDrill || c is IMyShipConnector)
            // (c is IMyCargoContainer || c is IMyShipDrill || c is IMyShipConnector || c is IMyShipWelder || c is IMyShipGrinder)
        );
    }

    public void Refresh() {
        this.Clear();

        this.max = 0;
        this.vol = 0;

        foreach (var c in this.cargo) {
            var inv = c.GetInventory(0);
            this.vol += inv.CurrentVolume;
            this.max += inv.MaxVolume;

            this.inventoryItems.Clear();
            inv.GetItems(this.inventoryItems);
            for (var i = 0; i < this.inventoryItems.Count; i++) {
                string fullName = this.inventoryItems[i].Type.ToString();
                string itemName = this.itemRegex.Replace(fullName, "");
                if (this.ingotRegex.IsMatch(fullName)) {
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

        this.pct = 0f;
        if (max != 0) {
            this.pct = (float)this.vol / (float)this.max;
        }
        // if (settings[CFG.CARGO_LIGHT] != "") {
        //     IMyLightingBlock light = (IMyLightingBlock)GetBlockWithName(settings[CFG.CARGO_LIGHT]);

        //     if (light != null && light is IMyLightingBlock) {
        //         if (pct > 0.98f) {
        //             light.Color = Color.Red;
        //         } else if (pct > 0.90f) {
        //             light.Color = Color.Yellow;
        //         } else {
        //             light.Color = Color.White;
        //         }
        //     }
        // }

        // string itemText = "";
        // int chars;
        // GetPanelWidthInChars(settings[CFG.CARGO], out chars);

        // int itemIndex = 0;
        // int doubleColumn = 60;
        // foreach (var item in cargoItemCounts) {
        //     var fmtd = Util.FormatNumber(item.Value);
        //     int maxChars = chars;
        //     if (chars > doubleColumn) {
        //         maxChars = (chars - 4) / 2;
        //     }
        //     var padLen = (int)(maxChars - item.Key.ToString().Length - fmtd.Length);
        //     string spacing = (padLen >= 0 ? "".PadRight(padLen, LINE_SPACER) : "\n  ");
        //     itemText += String.Format("{0}{1}{2}", item.Key, spacing, fmtd);
        //     if (chars <= doubleColumn || itemIndex % 2 != 0) {
        //         itemText += '\n';
        //     } else if (chars > doubleColumn) {
        //         itemText += "   ";
        //     }
        //     itemIndex++;
        // }

        // itemText = itemText;

        return;
    }

    public override string ToString() {
        string itemText = $"{pct}%";
        foreach (var item in this.cargoItemCounts) {
            var fmtd = Util.FormatNumber(item.Value);
            itemText += $"{item.Key}:{fmtd},";
            this.program.Echo($"{item.Key}:{fmtd},");
        }

        return itemText;
    }

    public void Draw(IMyTextSurface surface) {
        //todo
    }
}
/* CARGO */
