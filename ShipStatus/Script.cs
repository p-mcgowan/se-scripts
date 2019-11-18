/*
 * Config
 */

enum CFG {
    BLOCK_HEALTH,
    POWER,
    PRODUCTION,
    CARGO,
    CARGO_CAP,
    CARGO_CAP_STYLE,
    INPUT,
    POWER_BAR,
    JUMP_BAR
};

// By default, most things go to the same panel
const string MAIN_PANEL = "Text panel status";

// non-empty strings enable programs
Dictionary<CFG, string> settings = new Dictionary<CFG, string>{
    { CFG.BLOCK_HEALTH, MAIN_PANEL },
    { CFG.POWER, MAIN_PANEL },
    { CFG.PRODUCTION, MAIN_PANEL },
    { CFG.CARGO, MAIN_PANEL },
    { CFG.CARGO_CAP, "Cargo bar" },
    // If style is "small", does not print "Cargo status: " on the first line
    // (only the precent bar)
    { CFG.CARGO_CAP_STYLE, "default" },
    { CFG.INPUT, "Console input" },
    { CFG.POWER_BAR, "Power bar" },
    { CFG.JUMP_BAR, "Jump bar" }
};

const char BAR_EMPTY = '_';
const char BAR_FULL = '\u2588';
const char LINE_SPACER = ' ';  // for small fonts, will help find corresponding value
const double CHECK_FREQ_MS = 2 * 60 * 1000;  // how often we turn the machines back on
const double ON_WAIT_MS = 5 * 1000;
const double OUT_TIME_MS = 3 * 1000;  // after entering cmd, show text for this length of time
const string IGNORE_STRING = "[x]";  // Don't show these blocks
const string PRODUCTION_INPUT_PANEL = "Text panel production input";
const string STATUS_PANEL_CFG = "Text panel status";
public static readonly float[] FONT_DIM = { 30f - 2f, 42f - 2f };
public static readonly int[] SCREEN_DIM = { 738, 708 };

bool checking = false;
Color blue = new Color(0, 60, 255);
Color green = new Color(0, 240, 0);
Color red = new Color(255, 0, 0);
Color yellow = new Color(255, 255, 0);
double fontSize = 0;
double idleTime = 0;
double lastCheck = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
double outLock = 0;
double timeDisabled = 0;
int chars = 0;
List<IMyTerminalBlock> cargo = new List<IMyTerminalBlock>();
List<ProductionBlock> productionBlocks = new List<ProductionBlock>();

public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
}

// Util
public static class Util {

    public static string FormatNumber(VRage.MyFixedPoint input) {
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

    public static string TimeFormat(double ms, bool s = false) {
        TimeSpan t = TimeSpan.FromMilliseconds(ms);
        if (t.Hours != 0) {
            return String.Format("{0:D}h{1:D}m", t.Hours, t.Minutes);
        } else if (t.Minutes != 0) {
            return String.Format("{0:D}m", t.Minutes);
        } else {
            return (s ? String.Format("{0:D}s", t.Seconds) : "< 1m");
        }
    }

    public static string ToItemName(MyProductionItem i) {
        string id = i.BlueprintId.ToString();
        if (id.Contains('/')) {
            return id.Split('/')[1];
        }
        return id;
    }

    public static string PctString(float val) {
        return String.Format("{0,3:0}%", 100 * val);
    }
}

public void SetPanelCharacters(string panelName) {
    IMyTextPanel panel = GridTerminalSystem.GetBlockWithName(panelName) as IMyTextPanel;
    if (panel == null || !(panel is IMyTextPanel)) {
        return;
    }

    fontSize = panel.FontSize;
    var padding = panel.TextPadding;
    var beforePad = SCREEN_DIM[0] / (FONT_DIM[0] * fontSize);
    chars = (int)Math.Floor(beforePad - (2 * beforePad * (padding / 100)));
}

public void ClearOutputs() {
    foreach (string lcd in settings.Values) {
        if (lcd != settings[CFG.INPUT]) {
            WriteToLCD(lcd, "", true);
        }
    }
}

public void AppendToLCD(string panelName, string msg, bool silent = true) {
    IMyTextPanel panel = GridTerminalSystem.GetBlockWithName(panelName) as IMyTextPanel;
    if (panel == null || !(panel is IMyTextPanel)) {
        if (!silent) {
            Echo("ERROR: Could not find panel \"" + panelName + "\". ");
        }
        return;
    }
    string current = panel.GetText();
    panel.Font = "Monospace";
    panel.WriteText(current + msg);
}

public void WriteToLCD(string panelName, string msg, bool silent = true) {
    IMyTextPanel panel = GridTerminalSystem.GetBlockWithName(panelName) as IMyTextPanel;
    if (panel == null || !(panel is IMyTextPanel)) {
        if (!silent) {
            Echo("ERROR: Could not find panel \"" + panelName + "\". ");
        }
        return;
    }

    panel.Font = "Monospace";
    panel.WriteText(msg);
}

public string ProgressBar(float charge, bool withPct = true, int gaps = 6) {
    var pct = Util.PctString(charge);
    var barLen = (int)chars - gaps;
    var barFillLen = (int)Math.Floor(barLen * charge);
    return "[".PadRight(barFillLen, BAR_FULL) +
        "".PadLeft(barLen - barFillLen, BAR_EMPTY) +
        "] " + (withPct ? pct : "");
}

public class ProductionBlock {
    public Program program;
    public double idleTime;
    public IMyProductionBlock block;
    public bool Enabled {
        get { return this.block.Enabled; }
        set {
            if (this.block.DefinitionDisplayNameText.ToString() == "Survival kit") {
                return;
            }
            this.block.Enabled = value;
        }
    }

    public ProductionBlock(Program p, IMyProductionBlock block) {
        this.idleTime = -1;
        this.block = block;
        this.program = p;
    }

    public List<MyProductionItem> Queue() {
        var items = new List<MyProductionItem>();
        this.block.GetQueue(items);
        return items;
    }

    public bool IsIdle() {
        string status = this.Status();
        if (status == "Idle") {
            this.idleTime = (this.idleTime == -1) ? this.Now() : this.idleTime;
            return true;
        } else if (status == "Blocked" && !this.block.Enabled) {
            this.block.Enabled = true;
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

public void GetProductionBlocks(Program p) {
    var producers = new List<IMyProductionBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyProductionBlock>(producers, b =>
        b.CubeGrid == Me.CubeGrid && (b is IMyAssembler || b is IMyRefinery) && !b.CustomName.Contains(IGNORE_STRING));
    productionBlocks.Clear();
    foreach (var block in producers) {
        productionBlocks.Add(new ProductionBlock(p, block));
    }
    productionBlocks = productionBlocks.OrderBy(b => b.block.CustomName).ToList();
}

public void HandleInput() {
    var now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

    // Wait 4s before accepting more input
    if (outLock != 0) {
        if (now - outLock > OUT_TIME_MS) {
            outLock = 0;
            SetPanelCharacters(settings[CFG.INPUT]);
            WriteToLCD(settings[CFG.INPUT], ">: ");
        } else {
            return;
        }
    }

    SetPanelCharacters(settings[CFG.INPUT]);

    var productionInputPanel = GridTerminalSystem.GetBlockWithName(settings[CFG.INPUT]) as IMyTextPanel;
    if (productionInputPanel == null || !(productionInputPanel is IMyTextPanel)) {
        Echo("No input panel: " + settings[CFG.INPUT]);
        return;
    }

    System.Text.RegularExpressions.Regex pre =
        new System.Text.RegularExpressions.Regex("^[ ]*>:[ ]*",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    string text = pre.Replace(productionInputPanel.GetText(), "");

    switch (text) {
        case "d":
        case "disassemble":
            var dis = productionBlocks.Where(a => a.block is IMyAssembler && a.block.CustomName.IndexOf("disassemble", StringComparison.CurrentCultureIgnoreCase) >= 0).ToList();
            foreach (var d in dis) {
                d.Enabled = true;
            }
            outLock = now;
            WriteToLCD(settings[CFG.INPUT], "Disassembling started.");
            checking = true;
        break;
        case "on":
            foreach (var b in productionBlocks) {
                b.Enabled = true;
            }
            outLock = now;
            WriteToLCD(settings[CFG.INPUT], "Producers on.");
            checking = true;
        break;
        case "off":
            foreach (var b in productionBlocks) {
                b.Enabled = false;
            }
            outLock = now;
            WriteToLCD(settings[CFG.INPUT], "Producers off.");
        break;
        case "":
            // do nothing
        break;
        case "h":
        case "help":
        default:
            if (outLock == 0) {
                outLock = now;
                string[] o = {
                    "Run some simple commands to the production",
                    "program.",
                    "on             : turn on all machines",
                    "off            : turn off all machines",
                    "d, disassemble : run disassembler(s)",
                    "h, help        : show this menu" };
                SetPanelCharacters(settings[CFG.PRODUCTION]);
                WriteToLCD(settings[CFG.PRODUCTION], String.Join("\n", o));
            } else {

            }
        break;
    }
}

public string[] DoPowerDetails(Program program) {
    List<IMyJumpDrive> jumpDrives = new List<IMyJumpDrive>();
    List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
    program.GridTerminalSystem.GetBlocksOfType<IMyJumpDrive>(jumpDrives, b => b.CubeGrid == Me.CubeGrid);
    program.GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(batteries, b => b.CubeGrid == Me.CubeGrid);

    float maxCharge = 0;
    float currentCharge = 0;
    float maxBattCharge = 0;
    float currentBattCharge = 0;
    List<string> progressBars = new List<string>();
    string[] powerAndJump = new string[6];

    if (jumpDrives.Any()) {
        foreach (IMyJumpDrive d in jumpDrives) {
            // progressBars.Add(d.CustomName + Util.PctString(d.CurrentStoredPower / d.MaxStoredPower).PadLeft((int)chars - d.CustomName.Length));
            currentCharge += d.CurrentStoredPower;
            maxCharge += d.MaxStoredPower;
        }
        SetPanelCharacters(settings[CFG.POWER]);
        powerAndJump[0] = "Jump drive status (" + jumpDrives.Count.ToString() + " found):\n" + ProgressBar(currentCharge / maxCharge);
        SetPanelCharacters(settings[CFG.JUMP_BAR]);
        powerAndJump[2] = "Jump drive status (" + jumpDrives.Count.ToString() + " found):\n" + ProgressBar(currentCharge / maxCharge);
    }

    if (batteries.Any()) {
        foreach (IMyBatteryBlock d in batteries) {
            // progressBars.Add(d.CustomName + Util.PctString(d.CurrentStoredPower / d.MaxStoredPower).PadLeft((int)chars - d.CustomName.Length));
            currentBattCharge += d.CurrentStoredPower;
            maxBattCharge += d.MaxStoredPower;
        }
        SetPanelCharacters(settings[CFG.POWER]);
        powerAndJump[1] = "Battery status (" + batteries.Count.ToString() + " found):\n" + ProgressBar(currentBattCharge / maxBattCharge);
        SetPanelCharacters(settings[CFG.POWER_BAR]);
        powerAndJump[3] = "Battery status (" + batteries.Count.ToString() + " found):\n" + ProgressBar(currentBattCharge / maxBattCharge);
    }

    // foreach (string s in progressBars) {
    //     output += s + '\n';
    // }
    // power power, power_bar power_bar, jump_bar jump_bar

    return powerAndJump;
}


public string DoProductionDetails(Program p) {
    SetPanelCharacters(settings[CFG.PRODUCTION]);
    if (!productionBlocks.Any()) {
        GetProductionBlocks(p);
    }

    bool allIdle = true;
    string output = "";
    int assemblers = 0;
    int refineries = 0;
    foreach (var block in productionBlocks) {
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
            foreach (var block in productionBlocks) {
                block.Enabled = false;
            }
            timeDisabled = timeNow;
        } else {
            if (!checking) {
                if (timeNow - lastCheck > CHECK_FREQ_MS)  {
                    // We disabled them over CHECK_FREQ_MS ago, and need to check them
                    // Do another check for blocks, just to make sure we have the latest
                    GetProductionBlocks(p);
                    foreach (var block in productionBlocks) {
                        block.Enabled = true;
                    }
                    checking = true;
                    lastCheck = timeNow;
                    output = String.Format("Power saving mode {0} (checking)\n\n", Util.TimeFormat(timeNow - idleTime));
                }
            } else {
                if (timeNow - lastCheck > ON_WAIT_MS) {
                    // We waited 5 seconds and they are still not producing
                    foreach (var block in productionBlocks) {
                        block.Enabled = false;
                    }
                    checking = false;
                    lastCheck = timeNow;
                } else {
                    output = String.Format("Power saving mode {0} (checking)\n\n", Util.TimeFormat(timeNow - idleTime));
                }
            }
        }
        if (output == "") {
            output = String.Format("Power saving mode {0} (check in {1})\n\n",
                Util.TimeFormat(timeNow - idleTime),
                Util.TimeFormat(CHECK_FREQ_MS - (timeNow - lastCheck), true));
        }
    } else {
        if (productionBlocks.Where(b => b.Status() == "Blocked").ToList().Any()) {
            output += "Production Enabled (Halted)\n";
        } else {
            output += "Production Enabled\n";
        }

        // If any assemblers are on, make sure they are all on (master/slave)
        if (assemblers > 0) {
            foreach (var block in productionBlocks.Where(b => b.block is IMyAssembler).ToList()) {
                block.Enabled = true;
            }
        }

        idleTime = 0;
        timeDisabled = 0;
        checking = false;
    }

    bool sep = false;
    foreach (var block in productionBlocks) {
        var idle = block.IsIdle();
        if (!sep && block.block is IMyRefinery) {
            output += '\n';
            sep = true;
        }
        output += String.Format("{0}: {1} {2}\n",
            block.block.CustomName, block.Status(), (idle ? block.IdleTime() : ""));
        if (!idle) {
            // var i = block.Queue()[0];
            foreach (MyProductionItem i in block.Queue()) {
                output += String.Format("  {0} x {1}\n", Util.FormatNumber(i.Amount), Util.ToItemName(i));
            }
        }
    }

    return output + '\n';
}

public class CargoStatus {
    public string bar;
    public string text;
    public float pct;

    public CargoStatus(string bar, string text, float pct) {
        this.bar = bar;
        this.text = text;
        this.pct = pct;
    }
}

public CargoStatus DoCargoStatus() {
    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(cargo, c => c.CubeGrid == Me.CubeGrid &&
        (c is IMyCargoContainer || c is IMyShipDrill || c is IMyShipWelder || c is IMyShipGrinder || c is IMyShipConnector));

    VRage.MyFixedPoint max = 0;
    VRage.MyFixedPoint vol = 0;
    var itemList = new Dictionary<string, VRage.MyFixedPoint>();
    System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(".*/");

    foreach (var c in cargo) {
        var inv = c.GetInventory(0);
        vol += inv.CurrentVolume;
        max += inv.MaxVolume;

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

    SetPanelCharacters(settings[CFG.CARGO]);
    string itemText = "";
    foreach (var item in itemList) {
        var fmtd = Util.FormatNumber(item.Value);
        var padLen = (int)(chars - item.Key.ToString().Length - fmtd.Length);
        string spacing = (padLen >= 0 ? "".PadRight(padLen, LINE_SPACER) : "\n  ");
        itemText += String.Format("{0}{1}{2}\n", item.Key, spacing, fmtd);
    }

    SetPanelCharacters(settings[CFG.CARGO_CAP]);
    float charge = (float)vol / (float)max;

    return new CargoStatus(ProgressBar(charge, false, 1), itemText, charge);
}

// Show damaged blocks
public float GetHealth(IMyTerminalBlock block) {
    IMySlimBlock slimblock = block.CubeGrid.GetCubeBlock(block.Position);
    float MaxIntegrity = slimblock.MaxIntegrity;
    float BuildIntegrity = slimblock.BuildIntegrity;
    float CurrentDamage = slimblock.CurrentDamage;
    return (BuildIntegrity - CurrentDamage) / MaxIntegrity;
}

public string DoBlockHealth() {
    SetPanelCharacters(settings[CFG.BLOCK_HEALTH]);
    var blocks = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocks);
    string output = "";

    foreach (var b in blocks) {
        var health = GetHealth(b);
        if (health != 1f) {
            Echo(b.CustomName + " " + Util.PctString(GetHealth(b)));
            if (settings[CFG.BLOCK_HEALTH] != "") {
                output += b.CustomName + Util.PctString(GetHealth(b)).PadLeft((int)chars - b.CustomName.Length) + '\n';
            }
            b.ShowOnHUD = true;
        } else {
            b.ShowOnHUD = false;
        }
    }

    if (output == "") {
        output = "Ship status: No damage detected\n";
    } else {
        output = "Ship status: Damage detected\n" + output;
    }

    return output + '\n';
}

public void Main(string argument, UpdateType updateSource) {
    HandleInput();
    if (outLock != 0) {
        return;
    }
    ClearOutputs();

    string[] allPower = DoPowerDetails(this);
    if (settings[CFG.POWER] != "") {
        AppendToLCD(settings[CFG.POWER], allPower[0] + "\n\n" + allPower[1] + "\n\n");
    }

    if (settings[CFG.JUMP_BAR] != "") {
        WriteToLCD(settings[CFG.JUMP_BAR], allPower[2]);
    }
    if (settings[CFG.POWER_BAR] != "") {
        WriteToLCD(settings[CFG.POWER_BAR], allPower[3]);
    }

    string blockHealth = DoBlockHealth();
    if (settings[CFG.BLOCK_HEALTH] != "") {
        AppendToLCD(settings[CFG.BLOCK_HEALTH], blockHealth);
    }

    if (settings[CFG.PRODUCTION] != "") {
        AppendToLCD(settings[CFG.PRODUCTION], DoProductionDetails(this));
    }

    CargoStatus pctItems = null;

    if (settings[CFG.CARGO_CAP] != "") {
        pctItems = DoCargoStatus();
        if (settings[CFG.CARGO_CAP_STYLE] == "small") {
            AppendToLCD(settings[CFG.CARGO_CAP], ProgressBar(pctItems.pct, false, 7));
        } else {
            AppendToLCD(settings[CFG.CARGO_CAP], "Cargo status: " + Util.PctString(pctItems.pct) + '\n' + pctItems.bar);
        }
    }

    if (settings[CFG.CARGO] != "") {
        if (pctItems == null) {
            pctItems = DoCargoStatus();
        }

        if (settings[CFG.CARGO_CAP] == "") {
            AppendToLCD(settings[CFG.CARGO], "Cargo status: " + Util.PctString(pctItems.pct) + '\n' + pctItems.bar);
        }

        AppendToLCD(settings[CFG.CARGO], pctItems.text);
    }
}
