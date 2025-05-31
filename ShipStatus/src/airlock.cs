/*
 * AIRLOCK
 */
Airlock airlock;

public class Airlock {
    public Program program;
    public Dictionary<string, AirlockDoors> airlocks;
    public Dictionary<string, List<IMyFunctionalBlock>> locationToAirlockMap;
    // The name to match (Default will match regular doors). The capture group "(.*)" is used when grouping airlock doors, config  = "airlockDoorMatch", defualt = "Door(.*)";
    public System.Text.RegularExpressions.Regex include;
    // // The exclusion tag (can be anything). config = "airlockDoorExclude", default = "Hangar|Hatch";
    public System.Text.RegularExpressions.Regex exclude;
    // Manual airlocks are paired but not auto closed, config = "airlockDoorManual", default = "\\[AL\\]";
    public System.Text.RegularExpressions.Regex manual;
    // Duration before auto close (milliseconds), config = "airlockOpenTime", default = "750"
    public double timeOpen = 720f;

    public Airlock(Program program) {
        this.program = program;
        this.airlocks = new Dictionary<string, AirlockDoors>();
        this.locationToAirlockMap = new Dictionary<string, List<IMyFunctionalBlock>>();

        this.Reset();
    }

    public void Reset() {
        this.Clear();

        this.include = Util.Regex(this.program.config.Get("airlockDoorMatch", "Door(.*)"));
        this.exclude = Util.Regex(this.program.config.Get("airlockDoorExclude", "Hangar|Hatch"));
        this.manual = Util.Regex(this.program.config.Get("airlockDoorManual", "\\[AL\\]"));
        this.timeOpen = Util.ParseFloat(this.program.config.Get("airlockOpenTime"), 750f);
    }

    public void Clear() {
        this.airlocks.Clear();
        this.locationToAirlockMap.Clear();
    }

    public void CheckAirlocks() {
        if (!this.program.config.Enabled("airlock")) {
            return;
        }
        foreach (var al in this.airlocks) {
            al.Value.Check();
        }
    }

    public void GetBlock(IMyTerminalBlock block) {
        if (!(block is IMyDoor)) {
            return;
        }

        var match = this.include.Match(block.CustomName);
        var ignore = this.exclude.Match(block.CustomName);
        if (!match.Success || ignore.Success) {
            return;
        }
        var key = match.Groups[1].ToString();
        if (!this.locationToAirlockMap.ContainsKey(key)) {
            this.locationToAirlockMap.Add(key, new List<IMyFunctionalBlock>());
        }
        this.locationToAirlockMap[key].Add(block as IMyFunctionalBlock);

    }

    public void GotBLocks() {
        bool doAllDoors = this.program.config.Enabled("airlockAllDoors");
        foreach (var keyval in this.locationToAirlockMap) {
            if (!doAllDoors && keyval.Value.Count < 2) {
                continue;
            }
            this.airlocks.Add(keyval.Key, new AirlockDoors(keyval.Value, this.program, this.timeOpen, this.manual));
        }
    }
}

public class AirlockDoors {
    public Program program;
    private List<IMyFunctionalBlock> blocks;
    private List<IMyFunctionalBlock> areClosed;
    private List<IMyFunctionalBlock> areOpen;
    private double openTimer;
    private double timeOpen;
    private bool shouldAutoClose;

    public AirlockDoors(List<IMyFunctionalBlock> doors, Program program, double timeOpen, System.Text.RegularExpressions.Regex manual) {
        this.program = program;
        this.blocks = new List<IMyFunctionalBlock>(doors);
        this.areClosed = new List<IMyFunctionalBlock>();
        this.areOpen = new List<IMyFunctionalBlock>();
        this.openTimer = timeOpen;
        this.timeOpen = timeOpen;
        this.shouldAutoClose = true;

        foreach (var door in doors) {
            if (manual.Match(door.CustomName).Success) {
                this.shouldAutoClose = false;
            }
        }
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

    private void CloseAll() {
        foreach (var door in this.blocks) {
            this.OpenClose("Open_Off", door);
        }
    }

    public bool Check() {
        int openCount = 0;
        this.areClosed.Clear();
        this.areOpen.Clear();

        foreach (var door in this.blocks) {
            if (!Util.BlockValid(door)) {
                continue;
            }
            if (this.IsOpen(door)) {
                openCount++;
                this.areOpen.Add(door);
            } else {
                this.areClosed.Add(door);
            }
        }

        if (areOpen.Count == 0) {
            this.Unlock();
            this.openTimer = this.timeOpen;
            return true;
        }

        this.openTimer -= this.program.Runtime.TimeSinceLastRun.TotalMilliseconds;
        if (this.openTimer < 0 && this.shouldAutoClose) {
            this.CloseAll();
        } else {
            this.Lock(this.areClosed);
            this.Unlock(this.areOpen);
        }

        return true;
    }
}
/* AIRLOCK */
