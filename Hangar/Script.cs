const string STATE_PANEL = "LCD Panel Output";

Dictionary<string, string> state;

public string GetKey(string str) {
    string test;
    if (state.TryGetValue(str, out test)) {
        return test;
    }
    return "";
}

public void SetKey(string str, string val) {
    string test;
    if (state.TryGetValue(str, out test)) {
        state.Remove(str);
    }
    state.Add(str, val);
}

public void Input() {
    if (state == null) {
        state = new Dictionary<string, string>();
    } else {
        state.Clear();
    }
    string[] tokens = Storage.Split('\n');
    if (tokens.Count() == 0) {
        log("storage was empty");
        return;
    }
    foreach (string token in tokens) {
        string[] keyValue = token.Split('=');
        if (keyValue.Count() < 2) {
            continue;
        }
        SetKey(keyValue[0], keyValue[1]);
    }
}

public void Output(string force = null) {
    IMyTextPanel panel = GridTerminalSystem.GetBlockWithName(STATE_PANEL) as IMyTextPanel;
    string output = force ?? string.Join("\n", state.Select(x => x.Key + "=" + x.Value).ToArray());
    Storage = output;
    if (panel == null) {
        Echo("couldn'd find panel '" + STATE_PANEL + "'");
        Echo(output);
        return;
    }
    panel.WritePublicText(output);
    panel.ShowPublicTextOnScreen();
}

public double Now() {
    return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
}

public void log(string s) {
    if (state == null) {
        state = new Dictionary<string, string>();
    }
    string olog = GetKey("log");
    SetKey("log", olog + "\n" + s);
}

public class HangarProgram {
    private Program program;
    private IMyGridTerminalSystem gts;
    private List<IMyTerminalBlock> doors;
    private List<IMyTerminalBlock> vents;
    private List<IMyTerminalBlock> lights;

    public HangarProgram(Program program, IMyGridTerminalSystem gts) {
        this.program = program;
        this.gts = gts;
        this.doors = new List<IMyTerminalBlock>();
        this.vents = new List<IMyTerminalBlock>();
        this.lights = new List<IMyTerminalBlock>();

        (this.gts.GetBlockGroupWithName("Hangar Doors Main Deck") as IMyBlockGroup).GetBlocks(this.doors);
        (this.gts.GetBlockGroupWithName("Hangar Vents") as IMyBlockGroup).GetBlocks(this.vents);
        (this.gts.GetBlockGroupWithName("Hangar Lights") as IMyBlockGroup).GetBlocks(this.lights);
    }

    public void run() {
        this.program.SetKey("running", "hangars");
        string currentStage = this.program.GetKey("stage");
        string task = this.program.GetKey("task");
        DoorStatus doorStatus = (this.doors[0] as IMyDoor).Status;

        if ((doorStatus == DoorStatus.Closed && task != "close") || task == "open") {
            if (doorStatus == DoorStatus.Closed && currentStage == "") {
                this.program.SetKey("stage", "start");
            }
            this.open();
        } else if ((doorStatus == DoorStatus.Open && task != "open") || task == "close") {
            if (doorStatus == DoorStatus.Open && currentStage == "") {
                this.program.SetKey("stage", "start");
            }
            this.close();
        }
    }

    public void blinkLights(string action) {
        this.program.SetKey("lights", action);
        if (this.lights.Count() == 0) {
            return;
        }
        foreach (IMyFunctionalBlock light in this.lights) {
            if (action == "off") {
                light.Enabled = false;
            } else {
                light.Enabled = true;
            }
        }
    }

    public void toggleDepressurize(string action) {
        this.program.SetKey("vents", action);
        if (this.vents.Count() == 0) {
            return;
        }
        foreach (var vent in this.vents) {
            if (action == "suck") {
                vent.ApplyAction("Depressurize_On");
            } else {
                vent.ApplyAction("Depressurize_Off");
            }
        }
    }

    public void toggleDoors(string action) {
        this.program.SetKey("doors", action);
        if (this.doors.Count() == 0) {
            return;
        }
        foreach (IMyDoor door in this.doors) {
            if (action == "open") {
                door.OpenDoor();
            } else {
                door.CloseDoor();
            }
        }
    }

    public void open() {
        string openStage = this.program.GetKey("stage");
        this.program.SetKey("task", "open");

        switch (openStage) {
            case "start":
                this.program.SetKey("stage", "depressurizing");
                string now = this.program.Now().ToString();
                this.program.SetKey("start", now);
                this.blinkLights("on");
                this.toggleDepressurize("suck");
            break;

            case "depressurizing":
                // waiting
                double runtime = this.program.Now() - Convert.ToInt64(this.program.GetKey("start"));
                if (runtime > 10 * 1000) {
                    this.toggleDoors("open");
                    this.program.SetKey("stage", "opening");
                } else {
                    TimeSpan t = TimeSpan.FromMilliseconds((10 * 1000 - runtime));
                    this.program.log("Depressurized in " + String.Format("{0:D}s", t.Seconds));
                }
            break;

            case "opening":
                // finished opening
                if ((this.doors[0] as IMyDoor).Status == DoorStatus.Open) {
                    this.blinkLights("off");
                    this.program.state.Clear();
                }
            break;

            default:
                this.program.log("unknown open stage: " + openStage);
            break;
        }
    }

    public void close() {
        string openStage = this.program.GetKey("stage");
        this.program.SetKey("task", "close");

        switch (openStage) {
            // starting
            case "start":
                this.blinkLights("on");
                this.toggleDepressurize("blow");
                this.toggleDoors("close");
                this.program.SetKey("stage", "closing");
            break;

            case "closing":
                if ((this.doors[0] as IMyDoor).Status == DoorStatus.Closed) {
                    this.program.SetKey("stage", "pressurizing");
                    this.program.SetKey("start", Convert.ToString(this.program.Now()));
                }
            break;

            // waiting
            case "pressurizing":
                double runtime = this.program.Now() - Convert.ToInt64(this.program.GetKey("start"));
                if (runtime > 10 * 1000) {
                    this.blinkLights("off");
                    this.program.state.Clear();
                } else {
                    TimeSpan t = TimeSpan.FromMilliseconds((10 * 1000 - runtime));
                    this.program.log("Pressurized in " + String.Format("{0:D}s", t.Seconds));
                }
            break;

            default:
                this.program.log("unknown close stage: " + openStage);
            break;
        }
    }
}

HangarProgram hp;

public void Main(string argument, UpdateType updateSource) {
    if (argument == "reset") {
        Storage = null;
        Output("");
        return;
    }
    Input();
    // Terminal, Trigger, Update100
    string running = GetKey("running");

    if (running == "hangars" || argument == "hangars") {
        if (hp == null) {
            hp = new HangarProgram(this, GridTerminalSystem);
        }
        hp.run();
        Output();
        return;
    }

    Output("Idle");

}

Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
}