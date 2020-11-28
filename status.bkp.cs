/*
 * User config
 */
// non-empty strings enable programs
// for surface selection, use 'name <number>' eg: 'Cockpit <1>'
// You can also set the program block's CustomData instead (split on '\n' or ',')
// This will override settings eg:
/*
BLOCK_HEALTH=Text panel Status
POWER=Control Seat <0>
PRODUCTION=Text panel Production
CARGO=Text panel Cargo
CARGO_CAP=Control Seat <2>
CARGO_CAP_STYLE=small
CARGO_LIGHT=Spotlight 2
INPUT=Corner LCD
POWER_BAR=Control Seat <1>
JUMP_BAR=Jump panel
DO_AIRLOCK=
HEALTH_IGNORE=Hydrogen Thruster, Suspension
*/
public Dictionary<CFG, string> settings = new Dictionary<CFG, string>{
    { CFG.BLOCK_HEALTH, "Corvette Cockpit <2>" },
    { CFG.POWER, "Control Seat <0>" },
    { CFG.PRODUCTION, "" },
    { CFG.CARGO, "Control Seat <0>" },
    { CFG.CARGO_CAP, "" },
    { CFG.CARGO_LIGHT, "" },
    // If style is "small", does not print "Cargo status: " on the first line
    // (only the precent bar)
    { CFG.CARGO_CAP_STYLE, "small" },
    { CFG.INPUT, "" },
    { CFG.POWER_BAR, "Corvette Cockpit <1>" },
    { CFG.JUMP_BAR, "" },
    { CFG.DO_AIRLOCK, "true" },
    { CFG.HEALTH_IGNORE, "" }
};

/*
 * Script Config
 */
public enum CFG {
    BLOCK_HEALTH,
    POWER,
    PRODUCTION,
    CARGO,
    CARGO_CAP,
    CARGO_LIGHT,
    CARGO_CAP_STYLE,
    INPUT,
    POWER_BAR,
    JUMP_BAR,
    DO_AIRLOCK,
    HEALTH_IGNORE
};

public Dictionary<string, CFG> stringToConfig = new Dictionary<string, CFG> {
    { "BLOCK_HEALTH", CFG.BLOCK_HEALTH },
    { "POWER", CFG.POWER },
    { "PRODUCTION", CFG.PRODUCTION },
    { "CARGO", CFG.CARGO },
    { "CARGO_CAP", CFG.CARGO_CAP },
    { "CARGO_LIGHT", CFG.CARGO_LIGHT },
    { "CARGO_CAP_STYLE", CFG.CARGO_CAP_STYLE },
    { "INPUT", CFG.INPUT },
    { "POWER_BAR", CFG.POWER_BAR },
    { "JUMP_BAR", CFG.JUMP_BAR },
    { "DO_AIRLOCK", CFG.DO_AIRLOCK },
    { "HEALTH_IGNORE", CFG.HEALTH_IGNORE }
};

// Run once on start - set to false to disable CustomData check
public bool shouldCheckCustomData = true;

// drawing config
const char BAR_EMPTY = '_';
const char BAR_FULL = '\u2588';
const char LINE_SPACER = ' ';  // for small fonts, will help find corresponding value
public static readonly float[] FONT_DIM = { 30f - 2f, 42f - 2f };
public static readonly int[] SCREEN_DIM = { 738, 708 };

// production config
const double PRODUCTION_CHECK_FREQ_MS = 2 * 60 * 1000;  // how often we turn the machines back on
const double PRODUCTION_ON_WAIT_MS = 5 * 1000;
const double PRODUCTION_OUT_TIME_MS = 3 * 1000;  // after entering cmd, show text for this length of time
const string PRODUCTION_IGNORE_STRING = "[x]";  // Don't show these blocks
bool checking = false;
double idleTime = 0;
double lastCheck = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
double outLock = 0;
double timeDisabled = 0;

// globals so we don't look for them every update
List<IMyTerminalBlock> cargo = new List<IMyTerminalBlock>();
List<ProductionBlock> productionBlocks = new List<ProductionBlock>();
List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();

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

public static System.Text.RegularExpressions.Regex Regex(
    string pattern,
    System.Text.RegularExpressions.RegexOptions opts = System.Text.RegularExpressions.RegexOptions.None
) {
    return new System.Text.RegularExpressions.Regex(pattern, opts);
}

public bool CanWriteToSurface(string name) {
    int chars;
    return name != "" && GetPanelWidthInChars(name, out chars);
}

System.Text.RegularExpressions.Regex pnameSplitter = Regex(
    @"\s<(\d+)>$",
    System.Text.RegularExpressions.RegexOptions.Compiled
);

public IMyTerminalBlock GetBlockWithName(string name) {
    blocks.Clear();
    GridTerminalSystem.SearchBlocksOfName(name, blocks, c => c.CubeGrid == Me.CubeGrid);
    if (blocks.Count != 1) {
        return null;
    }
    return blocks[0];
}

public void GetPanelAndSurfaceId(string input, out IMyTerminalBlock panel, out int id) {
    var matches = pnameSplitter.Matches(input);
    if (matches.Count > 0 && matches[0].Groups.Count > 1) {
        int panelId = 0;
        if (!Int32.TryParse(matches[0].Groups[1].Value, out panelId)) {
           panelId = 0;
        }
        id = panelId;
        var panelName = input.Replace(matches[0].Groups[0].Value, "");
        panel = GetBlockWithName(panelName);
    } else {
        panel = GetBlockWithName(input);
        id = 0;
    }
    return;
}

public bool GetPanelWidthInChars(string panelName, out int chars) {
    chars = 0;
    if (panelName == "") {
        return false;
    }
    IMyTerminalBlock panel;
    int surfaceId;
    GetPanelAndSurfaceId(panelName, out panel, out surfaceId);
    if (panel == null || !(panel is IMyTextPanel || panel is IMyTextSurfaceProvider)) {
        return false;
    }

    IMyTextSurface surface = panel is IMyTextSurface
        ? (IMyTextSurface)panel
        : ((IMyTextSurfaceProvider)panel).GetSurface(surfaceId);
    if (surface == null) {
        return false;
    }

    surface.ContentType = ContentType.TEXT_AND_IMAGE;
    StringBuilder sb = new StringBuilder(" ");
    Vector2 charSizeInPx = surface.MeasureStringInPixels(sb, surface.Font, surface.FontSize);

    var padding = surface.TextPadding;
    var surfaceWidthPx = surface.SurfaceSize.X;
    var textableSurfacePx = surfaceWidthPx - (padding / 50 * surfaceWidthPx);
    var charsPerWidth = Math.Floor(textableSurfacePx / charSizeInPx.X);

    float aspect = surface.SurfaceSize.X / surface.SurfaceSize.Y;

    if (aspect < 1.6f) {  // irregular panels (cockpit sides, etc)
        charsPerWidth *= 2;
    } else if (aspect > 4f) {  // flight seat is huge
        charsPerWidth *= 4;
    }
    chars = (int)(charsPerWidth - 1f);  // magic numbers

    return true;
}

public void ClearOutputs() {
    foreach (string lcd in settings.Values) {
        if (lcd != settings[CFG.INPUT] && lcd != settings[CFG.CARGO_CAP_STYLE]) {
            WriteToLCD(lcd, "");
        }
    }
}

public void WriteToLCD(string panelName, string msg, bool append = false) {
    if (!CanWriteToSurface(panelName)) {
        return;
    }
    IMyTerminalBlock panel;
    int surfaceId;
    GetPanelAndSurfaceId(panelName, out panel, out surfaceId);
    if (panel == null || !(panel is IMyTextPanel || panel is IMyTextSurfaceProvider)) {
        Echo("WARN: Could not find panel \"" + panelName + "\". ");
        return;
    }

    IMyTextSurface surface;
    if (panel is IMyTextSurface) {
        surface = (IMyTextSurface)panel;
    } else {
        surface = ((IMyTextSurfaceProvider)panel).GetSurface(surfaceId);
    }

    if (surface == null) {
        Echo("no surface?" + panelName);
        return;
    }

    surface.ContentType = ContentType.TEXT_AND_IMAGE;
    surface.Font = "Monospace";
    surface.WriteText(msg + (append ? "\n" : ""), append);
}

public string ProgressBar(CFG cfg, float charge, bool withPct = true, int gaps = 6) {
    string targetPanel = settings[cfg];
    int chars;
    if (!GetPanelWidthInChars(targetPanel, out chars)) {
        return "";
    }

    var pct = Util.PctString(charge);
    var barLen = (int)chars - gaps;
    var barFillLen = (int)Math.Floor(barLen * charge);
    if (barFillLen < 0 || barLen - barFillLen < 0) {
        Echo("Got odd value for bar length: " + chars.ToString());
        return "~~~~~";
    }
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
        b.CubeGrid == Me.CubeGrid && (b is IMyAssembler || b is IMyRefinery) && !b.CustomName.Contains(PRODUCTION_IGNORE_STRING));
    productionBlocks.Clear();
    foreach (var block in producers) {
        productionBlocks.Add(new ProductionBlock(p, block));
    }
    productionBlocks = productionBlocks.OrderBy(b => b.block.CustomName).ToList();
}

public void HandleInput() {
    if (!CanWriteToSurface(settings[CFG.INPUT])) {
        return;
    }
    var now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

    // Wait 4s before accepting more input
    if (outLock != 0) {
        if (now - outLock > PRODUCTION_OUT_TIME_MS) {
            outLock = 0;
            WriteToLCD(settings[CFG.INPUT], ">: ");
        } else {
            return;
        }
    }

    IMyTextPanel productionInputPanel = (IMyTextPanel)GetBlockWithName(settings[CFG.INPUT]);
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
                WriteToLCD(settings[CFG.PRODUCTION], String.Join("\n", o));
            }
        break;
    }
}

public class PowerDetails {
    public string main;
    public string jumpBar;
    public string powerBar;

    public PowerDetails() {
        this.main = "";
        this.jumpBar = "";
        this.powerBar = "";
    }
}

public PowerDetails DoPowerDetails(Program program) {
    if (!CanWriteToSurface(settings[CFG.POWER]) && !CanWriteToSurface(settings[CFG.JUMP_BAR]) && !CanWriteToSurface(settings[CFG.POWER]) && !CanWriteToSurface(settings[CFG.POWER_BAR])) {
        return null;
    }

    PowerDetails details = new PowerDetails();

    List<IMyJumpDrive> jumpDrives = new List<IMyJumpDrive>();
    List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
    List<IMyReactor> reactors = new List<IMyReactor>();
    program.GridTerminalSystem.GetBlocksOfType<IMyJumpDrive>(jumpDrives, b => b.CubeGrid == Me.CubeGrid);
    program.GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(batteries, b => b.CubeGrid == Me.CubeGrid);
    program.GridTerminalSystem.GetBlocksOfType<IMyReactor>(reactors, b => b.CubeGrid == Me.CubeGrid);

    float maxCharge = 0;
    float currentCharge = 0;
    float maxBattCharge = 0;
    float currentBattCharge = 0;
    float currentReactorOut = 0;
    MyFixedPoint uraniumCount = 0;
    List<string> progressBars = new List<string>();

    if (jumpDrives.Any() && (CanWriteToSurface(settings[CFG.POWER]) || CanWriteToSurface(settings[CFG.JUMP_BAR]))) {
        foreach (IMyJumpDrive d in jumpDrives) {
            currentCharge += d.CurrentStoredPower;
            maxCharge += d.MaxStoredPower;
        }
        details.main = "Jump drive status (" + jumpDrives.Count.ToString() + " found):\n" + ProgressBar(CFG.POWER, currentCharge / maxCharge) + "\n";
        details.jumpBar = "Jump drive status (" + jumpDrives.Count.ToString() + " found):\n" + ProgressBar(CFG.JUMP_BAR, currentCharge / maxCharge) + "\n";
    }

    if (batteries.Any() && (CanWriteToSurface(settings[CFG.POWER]) || CanWriteToSurface(settings[CFG.POWER_BAR]))) {
        foreach (IMyBatteryBlock d in batteries) {
            currentBattCharge += d.CurrentStoredPower;
            maxBattCharge += d.MaxStoredPower;
        }
        details.main += "Battery status (" + batteries.Count.ToString() + " found):\n" + ProgressBar(CFG.POWER, currentBattCharge / maxBattCharge) + "\n";
        details.powerBar = "Battery status (" + batteries.Count.ToString() + " found):\n" + ProgressBar(CFG.POWER_BAR, currentBattCharge / maxBattCharge) + "\n";
    }

    if (reactors.Any() && CanWriteToSurface(settings[CFG.POWER])) {
        foreach (IMyReactor d in reactors) {
            currentReactorOut += d.CurrentOutput;
            var inv = d.GetInventory(0);

            var items = new List<MyInventoryItem>();
            inv.GetItems(items);
            for (var i = 0; i < items.Count; i++) {
                uraniumCount += items[i].Amount;
            }
        }

        details.main += "Reactor status (" + reactors.Count.ToString() + " found):\n"
            + "Output: " + currentReactorOut.ToString() + " MW\nUranium: " + Util.FormatNumber(uraniumCount) + "\n";
    }

    return details;
}


public string DoProductionDetails(Program p) {
    if (!CanWriteToSurface(settings[CFG.PRODUCTION])) {
        return "";
    }

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
                if (timeNow - lastCheck > PRODUCTION_CHECK_FREQ_MS)  {
                    // We disabled them over PRODUCTION_CHECK_FREQ_MS ago, and need to check them
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
                if (timeNow - lastCheck > PRODUCTION_ON_WAIT_MS) {
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
                Util.TimeFormat(PRODUCTION_CHECK_FREQ_MS - (timeNow - lastCheck), true));
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
    public string barCap;
    public string itemText;
    public float pct;

    public CargoStatus() {
        this.bar = "";
        this.barCap = "";
        this.itemText = "";
        this.pct = 0f;
    }
}

public CargoStatus DoCargoStatus() {
    if (!CanWriteToSurface(settings[CFG.CARGO]) && !CanWriteToSurface(settings[CFG.CARGO_CAP])) {
        return null;
    }

    CargoStatus status = new CargoStatus();

    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(cargo, c => c.CubeGrid == Me.CubeGrid &&
        (c is IMyCargoContainer || c is IMyShipDrill || c is IMyShipConnector));
        // (c is IMyCargoContainer || c is IMyShipDrill || c is IMyShipWelder || c is IMyShipGrinder || c is IMyShipConnector));

    VRage.MyFixedPoint max = 0;
    VRage.MyFixedPoint vol = 0;
    var itemList = new Dictionary<string, VRage.MyFixedPoint>();
    System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(".*/");
    var ingot = Regex("Ingot/");
    var ore = Regex("Ore/(?!Ice)");

    foreach (var c in cargo) {
        var inv = c.GetInventory(0);
        vol += inv.CurrentVolume;
        max += inv.MaxVolume;

        var items = new List<MyInventoryItem>();
        inv.GetItems(items);
        for (var i = 0; i < items.Count; i++) {
            string fullName = items[i].Type.ToString();
            string itemName = regex.Replace(fullName, "");
            if (ingot.IsMatch(fullName)) {
                itemName += " Ingot";
            } else if (ore.IsMatch(fullName)) {
                itemName += " Ore";
            }

            var itemQty = items[i].Amount;
            if (!itemList.ContainsKey(itemName)) {
                itemList.Add(itemName, itemQty);
            } else {
                itemList[itemName] = itemList[itemName] + itemQty;
            }
        }
    }

    status.pct = (float)vol / (float)max;
    if (settings[CFG.CARGO_LIGHT] != "") {
        IMyLightingBlock light = (IMyLightingBlock)GetBlockWithName(settings[CFG.CARGO_LIGHT]);

        if (light != null && light is IMyLightingBlock) {
            if (status.pct > 0.98f) {
                light.Color = Color.Red;
            } else if (status.pct > 0.90f) {
                light.Color = Color.Yellow;
            } else {
                light.Color = Color.White;
            }
        }
    }
    status.barCap = ProgressBar(CFG.CARGO_CAP, status.pct, false, 2);
    status.bar = ProgressBar(CFG.CARGO, status.pct, false, 2);

    string itemText = "";
    int chars;
    GetPanelWidthInChars(settings[CFG.CARGO], out chars);

    int itemIndex = 0;
    int doubleColumn = 60;
    foreach (var item in itemList) {
        var fmtd = Util.FormatNumber(item.Value);
        int maxChars = chars;
        if (chars > doubleColumn) {
            maxChars = (chars - 4) / 2;
        }
        var padLen = (int)(maxChars - item.Key.ToString().Length - fmtd.Length);
        string spacing = (padLen >= 0 ? "".PadRight(padLen, LINE_SPACER) : "\n  ");
        itemText += String.Format("{0}{1}{2}", item.Key, spacing, fmtd);
        if (chars <= doubleColumn || itemIndex % 2 != 0) {
            itemText += '\n';
        } else if (chars > doubleColumn) {
            itemText += "   ";
        }
        itemIndex++;
    }

    status.itemText = itemText;

    return status;
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
    if (!CanWriteToSurface(settings[CFG.BLOCK_HEALTH])) {
        return null;
    }

    System.Text.RegularExpressions.Regex ignoreHealth = null;
    if (settings[CFG.HEALTH_IGNORE] != "") {
        string input = System.Text.RegularExpressions.Regex.Replace(settings[CFG.HEALTH_IGNORE], @"\s*,\s*", "|");
        ignoreHealth = Regex(input);
    }
    // CFG.HEALTH_IGNORE, "Hydrogen Thruster, Suspension"

    var blocks = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocks, b => b.CubeGrid == Me.CubeGrid);
    string output = "";

    int chars;
    GetPanelWidthInChars(settings[CFG.BLOCK_HEALTH], out chars);

    foreach (var b in blocks) {
        if (ignoreHealth != null && ignoreHealth.IsMatch(b.CustomName)) {
            continue;
        }

        var health = GetHealth(b);
        if (health != 1f) {
            if (CanWriteToSurface(settings[CFG.BLOCK_HEALTH])) {
                output += b.CustomName + " [" + Util.PctString(GetHealth(b)) + "]\n";
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

public void ParseCustomData() {
    shouldCheckCustomData = false;
    string sx = Me.CustomData;
    if (Me.CustomData == null || Me.CustomData == "") {
        return;
    }

    // clear cfg
    Array values = Enum.GetValues(typeof(CFG));
    foreach (CFG val in values) {
        settings[val] = "";
    }

    var items = sx.Split(new[] { '\n', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Split(new[] { '=' }));
    foreach (var item in items) {
        if (stringToConfig.ContainsKey(item[0])) {
            CFG setting;
            stringToConfig.TryGetValue(item[0], out setting);
            settings[setting] = item[1];
        } else {
            Echo(String.Format("Unknown config '{0}'", item[0].ToString()));
        }
    }
}

/**
 * Airlock doors - Auto close doors and lock airlock pairs"
 *
 * By default, all doors with default names will auto close (see DOOR_MATCH).
 * For airlock doors to pair together (lock when the other is open), give them the same name. This works for any number of doors.
 * If you want to include all doors by default but exclude a few, name the doors so that they contain the DOOR_EXCLUDE tag.
 */

// Config vars
const double TIME_OPEN = 750f;           // Duration before auto close (milliseconds)
const string DOOR_MATCH = "Door(.*)";    // The name to match (Default will match regular doors).
                                         // The capture group "(.*)" is used when grouping airlock doors.
const string DOOR_EXCLUDE = "Hangar";    // The exclusion tag (can be anything).

// Script vars
Dictionary<string, Airlock> airlocks = new Dictionary<string, Airlock>();
System.Text.RegularExpressions.Regex include = new System.Text.RegularExpressions.Regex(DOOR_MATCH, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
System.Text.RegularExpressions.Regex exclude = new System.Text.RegularExpressions.Regex(DOOR_EXCLUDE, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

// Utility class containing for each individual airlock
//
public class Airlock {
    private List<IMyFunctionalBlock> blocks;
    private double openTimer;

    public Airlock(List<IMyFunctionalBlock> doors, IMyFunctionalBlock light = null) {
        this.blocks = new List<IMyFunctionalBlock>(doors);
        this.openTimer = -1;
    }

    private bool IsOpen(IMyFunctionalBlock door) {
        return (door as IMyDoor).OpenRatio > 0;
    }

    private void Lock(List<IMyFunctionalBlock> doors = null) {
        doors = doors ?? this.blocks;
        foreach (var door in doors) {
            (door as IMyDoor).Enabled = false;
        }
    }

    private void Unlock(List<IMyFunctionalBlock> doors = null) {
        doors = doors ?? this.blocks;
        foreach (var door in doors) {
            (door as IMyDoor).Enabled = true;
        }
    }

    private void OpenClose(string action, IMyFunctionalBlock door1, IMyFunctionalBlock door2 = null) {
        (door1 as IMyDoor).ApplyAction(action);
        if (door2 != null) {
            (door2 as IMyDoor).ApplyAction(action);
        }
    }
    private void Open(IMyFunctionalBlock door1, IMyFunctionalBlock door2 = null) {
        OpenClose("Open_On", door1, door2);
    }
    private void OpenAll() {
        foreach (var door in this.blocks) {
            OpenClose("Open_On", door);
        }
    }
    private void Close(IMyFunctionalBlock door1, IMyFunctionalBlock door2 = null) {
        OpenClose("Open_Off", door1, door2);
    }
    private void CloseAll() {
        foreach (var door in this.blocks) {
            OpenClose("Open_Off", door);
        }
    }

    public bool Test() {
        int openCount = 0;
        var areClosed = new List<IMyFunctionalBlock>();
        var areOpen = new List<IMyFunctionalBlock>();
        foreach (var door in this.blocks) {
            if (this.IsOpen(door)) {
                openCount++;
                areOpen.Add(door);
            } else {
                areClosed.Add(door);
            }
        }
        if (areOpen.Count > 0) {
            if (this.openTimer == -1) {
                this.openTimer = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                this.Lock(areClosed);
                this.Unlock(areOpen);
            } else if (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - this.openTimer > TIME_OPEN) {
                this.CloseAll();
            }
        } else {
            this.Unlock();
            this.openTimer = -1;
        }

        return true;
    }
}

// Map block list into hash
//
public void GetMappedAirlocks() {
    var airlockBlocks = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyDoor>(airlockBlocks, door => door.CubeGrid == Me.CubeGrid);

    // Parse into hash (identifier => List(door)), where name is "Door <identifier>"
    var locationToAirlockMap = new Dictionary<string, List<IMyFunctionalBlock>>();

    // Get all door blocks
    foreach (var block in airlockBlocks) {
        var match = include.Match(block.CustomName);
        var ignore = exclude.Match(block.CustomName);
        if (ignore.Success) { continue; }
        if (!match.Success) {
            continue;  // TODO: lights
        }
        var key = match.Groups[1].ToString();
        if (!locationToAirlockMap.ContainsKey(key)) {
            locationToAirlockMap.Add(key, new List<IMyFunctionalBlock>());
        }
        locationToAirlockMap[key].Add(block as IMyFunctionalBlock);
    }
    foreach (var locAirlock in locationToAirlockMap) {
        airlocks.Add(locAirlock.Key, new Airlock(locAirlock.Value));
    }
}

public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
    // Runtime.UpdateFrequency = UpdateFrequency.Update100;
}


public void Main(string argument, UpdateType updateSource) {
    if (settings[CFG.DO_AIRLOCK] == "true") {
        if (!airlocks.Any()) {
            GetMappedAirlocks();
        }

        foreach (var al in airlocks) {
            al.Value.Test();
        }
    }

    if (shouldCheckCustomData) {
        ParseCustomData();
    }
    HandleInput();
    if (outLock != 0) {
        return;
    }
    ClearOutputs();

    PowerDetails powerDetails = DoPowerDetails(this);

    if (CanWriteToSurface(settings[CFG.POWER]) && powerDetails.main != "") {
        WriteToLCD(settings[CFG.POWER], powerDetails.main, true);
    }

    if (CanWriteToSurface(settings[CFG.JUMP_BAR]) && powerDetails.jumpBar != "") {
        WriteToLCD(settings[CFG.JUMP_BAR], powerDetails.jumpBar, true);
    }

    if (CanWriteToSurface(settings[CFG.POWER_BAR]) && powerDetails.powerBar != "") {
        WriteToLCD(settings[CFG.POWER_BAR], powerDetails.powerBar, true);
    }

    if (CanWriteToSurface(settings[CFG.BLOCK_HEALTH])) {
        string blockHealth = DoBlockHealth();
        WriteToLCD(settings[CFG.BLOCK_HEALTH], blockHealth, true);
    }

    if (CanWriteToSurface(settings[CFG.PRODUCTION])) {
        WriteToLCD(settings[CFG.PRODUCTION], DoProductionDetails(this), true);
    }

    CargoStatus cStats = null;

    if (CanWriteToSurface(settings[CFG.CARGO_CAP])) {
        cStats = DoCargoStatus();
        if (settings[CFG.CARGO_CAP_STYLE] == "small") {
            WriteToLCD(settings[CFG.CARGO_CAP], ProgressBar(CFG.CARGO_CAP, cStats.pct, false, 7), true);
        } else {
            WriteToLCD(settings[CFG.CARGO_CAP], "Cargo status: " + Util.PctString(cStats.pct) + '\n' + cStats.barCap, true);
        }
    }

    if (CanWriteToSurface(settings[CFG.CARGO])) {
        if (cStats == null) {
            cStats = DoCargoStatus();
        }

        // dont write status if it's on another panel
        if (!CanWriteToSurface(settings[CFG.CARGO_CAP])) {
            WriteToLCD(settings[CFG.CARGO], "Cargo status: " + Util.PctString(cStats.pct) + '\n' + cStats.bar + '\n', true);
        }
        WriteToLCD(settings[CFG.CARGO], cStats.itemText, true);
    }
}
