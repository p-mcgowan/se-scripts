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

    public string bar;
    public string itemText;
    public float pct;

    public CargoStatus(Program _program) {
        program = _program;
        itemText = "";
        pct = 0f;
        cargo = new List<IMyTerminalBlock>();
        cargoItemCounts = new Dictionary<string, VRage.MyFixedPoint>();
        inventoryItems = new List<MyInventoryItem>();
        itemRegex = Util.Regex(".*/");
        ingotRegex = Util.Regex("Ingot/");
        oreRegex = Util.Regex("Ore/(?!Ice)");
        GetCargoBlocks();
    }

    public void Clear() {
        itemText = "";
        pct = 0f;
        cargo.Clear();
        cargoItemCounts.Clear();
        inventoryItems.Clear();
    }

    public void GetCargoBlocks() {
        program.GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(cargo, c =>
            c.IsSameConstructAs(program.Me) &&
            (c is IMyCargoContainer || c is IMyShipDrill || c is IMyShipConnector)
            // (c is IMyCargoContainer || c is IMyShipDrill || c is IMyShipConnector || c is IMyShipWelder || c is IMyShipGrinder)
        );
    }

    public void Refresh() {
        Clear();

        VRage.MyFixedPoint max = 0;
        VRage.MyFixedPoint vol = 0;

        foreach (var c in cargo) {
            var inv = c.GetInventory(0);
            vol += inv.CurrentVolume;
            max += inv.MaxVolume;

            inventoryItems.Clear();
            inv.GetItems(inventoryItems);
            for (var i = 0; i < inventoryItems.Count; i++) {
                string fullName = inventoryItems[i].Type.ToString();
                string itemName = itemRegex.Replace(fullName, "");
                if (ingotRegex.IsMatch(fullName)) {
                    itemName += " Ingot";
                } else if (oreRegex.IsMatch(fullName)) {
                    itemName += " Ore";
                }

                var itemQty = inventoryItems[i].Amount;
                if (!cargoItemCounts.ContainsKey(itemName)) {
                    cargoItemCounts.Add(itemName, itemQty);
                } else {
                    cargoItemCounts[itemName] = cargoItemCounts[itemName] + itemQty;
                }
            }
        }

        pct = (float)vol / (float)max;
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
        foreach (var item in cargoItemCounts) {
            var fmtd = Util.FormatNumber(item.Value);
            itemText += $"{item.Key}:{fmtd},";
        }
        return itemText;
    }

    public void Draw(IMyTextSurface surface) {
        //todo
    }
}
/* CARGO */
