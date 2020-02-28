// Config
const string OUT_PANEL = "LCD Panel Production";
const string CMD_PANEL = "LCD Panel Production Command";
const double OUT_TIME_MS = 3 * 1000;  // after entering cmd, show text for this length of time
const double CHECK_FREQ_MS = 2 * 60 * 1000;  // how often we turn the machines back on
const string IGNORE_STRING = "[x]";  // Don't show these blocks

// Internals
const double ON_WAIT_MS = 5 * 1000;
double lastCheck = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
double timeDisabled = 0;
double idleTime = 0;
double outLock = 0;
bool checking = false;
IMyTextPanel outputPanel;
IMyTextPanel inputPanel;
List<ProductionBlock> blocks = new List<ProductionBlock>();
public static readonly float[] FONT_DIM = { 30f - 2f, 42f - 2f };
public static readonly int[] SCREEN_DIM = { 736, 708 };
Color blue = new Color(0, 60, 255);

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
        return this.program.TimeFormat(this.Now() - this.idleTime);
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


public string TimeFormat(double ms, bool s = false) {
    TimeSpan t = TimeSpan.FromMilliseconds(ms);
    if (t.Hours != 0) {
        return String.Format("{0:D}h{1:D}m", t.Hours, t.Minutes);
    } else if (t.Minutes != 0) {
        return String.Format("{0:D}m", t.Minutes);
    } else {
        return (s ? String.Format("{0:D}s", t.Seconds) : "< 1m");
    }
}

public void GetProductionBlocks(Program p) {
    var producers = new List<IMyProductionBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyProductionBlock>(producers, b =>
        b.CubeGrid == Me.CubeGrid && (b is IMyAssembler || b is IMyRefinery) && !b.CustomName.Contains(IGNORE_STRING));
    blocks.Clear();
    foreach (var block in producers) {
        blocks.Add(new ProductionBlock(p, block));
    }
    blocks = blocks.OrderBy(b => b.block.CustomName).ToList();
    outputPanel = GridTerminalSystem.GetBlockWithName(OUT_PANEL) as IMyTextPanel;
}

public string ToItemName(MyProductionItem i) {
    string id = i.BlueprintId.ToString();
    if (id.Contains('/')) {
        return id.Split('/')[1];
    }
    Echo(i.BlueprintId.ToString());
    return id;
}

void PrintLCD(string panelName, string msg) {
    var panels = new List<IMyTextPanel>();
    GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, p => p.CubeGrid == Me.CubeGrid && p.CustomName == panelName);
    foreach (var panel in panels) {
        PrintLCD(panel, msg);
    }
}

void PrintLCD(IMyTextPanel p, string s) {
    if (p != null && p is IMyTextPanel) {
        p.Font = "Monospace";
        p.FontColor = blue;
        p.ContentType = ContentType.TEXT_AND_IMAGE;
        p.TextPadding = 0.5f;
        p.WriteText(s);
    } else {
        Echo("err writing to screen");
    }
}

void RunCmd() {
    inputPanel = GridTerminalSystem.GetBlockWithName(CMD_PANEL) as IMyTextPanel;
    if (inputPanel == null || !(inputPanel is IMyTextPanel)) {
        return;
    }
    var now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

    // Wait 4s before accepting more input
    if (outLock != 0) {
        if (now - outLock > OUT_TIME_MS) {
            outLock = 0;
            PrintLCD(inputPanel, ">: ");
            } else {
                return;
            }
    }

    System.Text.RegularExpressions.Regex pre =
        new System.Text.RegularExpressions.Regex("^[ ]*>:[ ]*",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    string text = pre.Replace(inputPanel.GetText(), "");

    switch (text) {
        case "d":
        case "disassemble":
            var dis = blocks.Where(a => a.block is IMyAssembler && a.block.CustomName.IndexOf("disassemble", StringComparison.CurrentCultureIgnoreCase) >= 0).ToList();
            foreach (var d in dis) {
                d.Enabled = true;
            }
            outLock = now;
            PrintLCD(OUT_PANEL, "Disassembling started.");
            checking = true;
        break;
        case "on":
            foreach (var b in blocks) {
                b.Enabled = true;
            }
            outLock = now;
            PrintLCD(OUT_PANEL, "Producers on.");
            checking = true;
        break;
        case "off":
            foreach (var b in blocks) {
                b.Enabled = false;
            }
            outLock = now;
            PrintLCD(OUT_PANEL, "Producers off.");
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
                    "Run some simple commands to the production program.",
                    "on             : turn on all machines",
                    "off            : turn off all machines",
                    "d, disassemble : run disassembler(s)",
                    "h, help        : show this menu" };
                PrintLCD(OUT_PANEL, String.Join("\n", o));
            }
        break;
    }
}

void Main() {
    if (!blocks.Any()) {
        GetProductionBlocks(this);
    }

    bool allIdle = true;
    string output = "";
    int assemblers = 0;
    int refineries = 0;
    foreach (var block in blocks) {
        bool idle = block.IsIdle();
        allIdle = allIdle && idle;
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
            foreach (var block in blocks) {
                block.Enabled = false;
            }
            timeDisabled = timeNow;
        } else {
            if (!checking) {
                if (timeNow - lastCheck > CHECK_FREQ_MS)  {
                    // We disabled them over CHECK_FREQ_MS ago, and need to check them
                    // Do another check for blocks, just to make sure we have the latest
                    GetProductionBlocks(this);
                    foreach (var block in blocks) {
                        block.Enabled = true;
                    }
                    checking = true;
                    lastCheck = timeNow;
                    output = String.Format("Power saving mode {0} (checking)\n\n", TimeFormat(timeNow - idleTime));
                }
            } else {
                if (timeNow - lastCheck > ON_WAIT_MS) {
                    // We waited 5 seconds and they are still not producing
                    foreach (var block in blocks) {
                        block.Enabled = false;
                    }
                    checking = false;
                    lastCheck = timeNow;
                } else {
                    output = String.Format("Power saving mode {0} (checking)\n\n", TimeFormat(timeNow - idleTime));
                }
            }
        }
        if (output == "") {
            output = String.Format("Power saving mode {0} (check in {1})\n\n",
                TimeFormat(timeNow - idleTime),
                TimeFormat(CHECK_FREQ_MS - (timeNow - lastCheck), true));
        }
    } else {
        if (blocks.Where(b => b.Status() == "Blocked").ToList().Any()) {
            output += "Production Enabled (Halted)\n";
        } else {
            output += "Production Enabled\n";
        }

        // If any assemblers are on, make sure they are all on (master/slave)
        if (assemblers > 0) {
            foreach (var block in blocks.Where(b => b.block is IMyAssembler).ToList()) {
                block.Enabled = true;
            }
        }

        idleTime = 0;
        timeDisabled = 0;
        checking = false;
    }
    bool sep = false;
    foreach (var block in blocks) {
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
                output += String.Format("  {0} x {1}\n", i.Amount.ToIntSafe(), ToItemName(i));
            }
        }
    }

    if (outLock == 0) {
        PrintLCD(OUT_PANEL, output);
    }

    RunCmd();
}

Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
}
