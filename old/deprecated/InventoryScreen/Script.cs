// Config Vars
const string PANEL_NAME = "Inventory Panel";
const string LIGHT_NAME = "IniTug Cargo Light";
const bool SHOW_PERCENT = true;
const bool SHOW_ITEMS = true;

// Globals
List<IMyTerminalBlock> cargo = new List<IMyTerminalBlock>();
IMyLightingBlock light;
List<IMyTextPanel> panels;
bool blink = false;
const char BAR_FULL = '\u2588';
const char BAR_EMPTY = '_';
const char LINE_SPACER = ' ';  // for small fonts, will help find corresponding value
public static readonly float[] FONT_DIM = { 30f - 2f, 42f - 2f };
public static readonly int[] SCREEN_DIM = { 738, 708 };
double fontSize = 0;
int chars = 0;
Color red = new Color(255, 0, 0);
Color yellow = new Color(255, 255, 0);
Color green = new Color(0, 240, 0);
Color blue = new Color(0, 60, 255);

public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
}

public void Init() {
    panels = new List<IMyTextPanel>();
    light = GridTerminalSystem.GetBlockWithName(LIGHT_NAME) as IMyLightingBlock;
    // panels = GridTerminalSystem.GetBlockWithName(PANEL_NAME) as IMyTextPanel;
    GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, p => p.CubeGrid == Me.CubeGrid && p.CustomName == PANEL_NAME);
    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(cargo, c => c.CubeGrid == Me.CubeGrid &&
        (c is IMyCargoContainer || c is IMyShipDrill || c is IMyShipGrinder || c is IMyShipWelder || c is IMyShipConnector));
}

public string FormatNumber(VRage.MyFixedPoint input) {
    string fmt;
    int n = Math.Max(0, (int)input);
    if (n < 10000) {
        fmt = "##";
    } else if (n < 1000000) {
        fmt = "###0,K";
    } else {
        fmt = "###0,,M";
    }
    return n.ToString(fmt, System.Globalization.CultureInfo.InvariantCulture);
}

string PctString(float val) {
    return String.Format("{0,3:0}%", 100 * val);
}

string ProgressBar(float charge) {
    var pct = PctString(charge);
    var barLen = (int)chars - 7;  // [] xxx%
    var barFillLen = (int)Math.Floor(barLen * charge);
    return String.Format("[{0}{1}]{3}{2}",
        "".PadRight(barFillLen, BAR_FULL),
        "".PadLeft(barLen - barFillLen, BAR_EMPTY),
        pct,
        blink ? LINE_SPACER : BAR_EMPTY);
}

public void Main(string argument, UpdateType updateSource) {
    blink = !blink;

    if (argument == "toggle tool") {
        var tools = new List<IMyShipToolBase>();
        GridTerminalSystem.GetBlocksOfType<IMyShipToolBase>(tools, t => t is IMyShipGrinder || t is IMyShipWelder);
        bool on = false;

        foreach (var tool in tools) {
            if (tool.Enabled) {
                on = true;
                break;
            }
        }

        foreach (var tool in tools) {
            tool.Enabled = !on;
        }

        return;
    }

    if (!cargo.Any()) {
        Init();
    }

    VRage.MyFixedPoint max = 0;
    VRage.MyFixedPoint vol = 0;
    var itemList = new Dictionary<string, VRage.MyFixedPoint>();
    string itemText = "";
    System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(".*/");

    foreach (var c in cargo) {
        var inv = c.GetInventory(0);

        if (!(c is IMyShipWelder)) {
            vol += inv.CurrentVolume;
            max += inv.MaxVolume;
        }

        var items = new List<MyInventoryItem>();
        inv.GetItems(items);
        for (var i = 0; i < items.Count; i++) {
            string itemName = regex.Replace(items[i].ToString(), "");
            var itemQty = items[i].Amount;
            if (!itemList.ContainsKey(itemName)) {
                itemList.Add(itemName, itemQty);
            } else {
                itemList[itemName] = itemList[itemName] + itemQty;
            }
        }
    }

    if (light != null) {
        var pct = 100 * (float)vol / (float)max;
        if (pct > 95f) {
            light.Color = red;
        } else if (pct > 80f) {
            light.Color = yellow;
        } else {
            light.Color = green;
        }
    }


    foreach (var panel in panels) {
        fontSize = panel.FontSize;
        chars = (int)Math.Floor(SCREEN_DIM[0] / (FONT_DIM[0] * fontSize));

        foreach (var item in itemList) {
            var fmtd = FormatNumber(item.Value);
            var padLen = (int)(chars - item.Key.ToString().Length - fmtd.Length);
            string spacing = (padLen >= 0 ? "".PadRight(padLen, LINE_SPACER) : "\n  ");
            itemText += String.Format("{0}{1}{2}\n", item.Key, spacing, fmtd);
        }

        string percentText = ProgressBar((float)vol / (float)max) + '\n';
        string output = "" + (SHOW_PERCENT ? percentText : "") + (SHOW_ITEMS ? itemText : "");

        panel.FontColor = blue;
        panel.Font = "Monospace";
        panel.ContentType = ContentType.TEXT_AND_IMAGE;
        panel.TextPadding = 0.5f;
        panel.WriteText(output);
    }
}
