/*
 * AIRLOCK
 */
Airlock airlock;

public class Airlock {
    public Program program;
    public Dictionary<string, AirlockDoors> airlocks;
    public List<IMyTerminalBlock> airlockBlocks;
    public Dictionary<string, List<IMyFunctionalBlock>> locationToAirlockMap;
    public System.Text.RegularExpressions.Regex include;
    public System.Text.RegularExpressions.Regex exclude;

    // The name to match (Default will match regular doors). The capture group "(.*)" is used when grouping airlock doors.
    public string doorMatch = "Door(.*)";
    // The exclusion tag (can be anything).
    public string doorExclude = "Hangar";
    // Duration before auto close (milliseconds)
    public double timeOpen = 720f;

    public Airlock(Program program) {
        this.program = program;
        this.airlocks = new Dictionary<string, AirlockDoors>();
        this.airlockBlocks = new List<IMyTerminalBlock>();
        this.locationToAirlockMap = new Dictionary<string, List<IMyFunctionalBlock>>();
        this.include = Util.Regex(this.doorMatch, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        this.exclude = Util.Regex(this.doorExclude, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (this.program.config.Enabled("airlock")) {
            this.GetMappedAirlocks();
        }
        this.timeOpen = Util.ParseFloat(this.program.config.Get("airlockOpenTime"), 750f);
    }

    public void CheckAirlocks() {
        if (!this.program.config.Enabled("airlock")) {
            return;
        }
        foreach (var al in this.airlocks) {
            al.Value.Check();
        }
    }

    public void GetMappedAirlocks() {
        if (!this.program.config.Enabled("airlock")) {
            return;
        }
        this.airlockBlocks.Clear();
        this.program.GridTerminalSystem.GetBlocksOfType<IMyDoor>(this.airlockBlocks, door => door.IsSameConstructAs(this.program.Me));

        // Parse into hash (identifier => List(door)), where name is "Door <identifier>"
        this.locationToAirlockMap.Clear();

        // Get all door blocks
        foreach (var block in this.airlockBlocks) {
            var match = this.include.Match(block.CustomName);
            var ignore = this.exclude.Match(block.CustomName);
            if (!match.Success || ignore.Success) {
                continue;
            }
            var key = match.Groups[1].ToString();
            if (!this.locationToAirlockMap.ContainsKey(key)) {
                this.locationToAirlockMap.Add(key, new List<IMyFunctionalBlock>());
            }
            this.locationToAirlockMap[key].Add(block as IMyFunctionalBlock);
        }

        bool doAllDoors = this.program.config.Enabled("airlockAllDoors");
        foreach (var keyval in this.locationToAirlockMap) {
            if (!doAllDoors && keyval.Value.Count < 2) {
                continue;
            }
            this.airlocks.Add(keyval.Key, new AirlockDoors(keyval.Value, this.program));
        }
    }
}

public class AirlockDoors {
    public Program program;
    private List<IMyFunctionalBlock> blocks;
    private List<IMyFunctionalBlock> areClosed;
    private List<IMyFunctionalBlock> areOpen;
    private double openTimer;
    public double timeOpen;

    public AirlockDoors(List<IMyFunctionalBlock> doors, Program program, double timeOpen = 750f) {
        this.program = program;
        this.blocks = new List<IMyFunctionalBlock>(doors);
        this.areClosed = new List<IMyFunctionalBlock>();
        this.areOpen = new List<IMyFunctionalBlock>();
        this.openTimer = timeOpen;
        this.timeOpen = timeOpen;
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
        this.OpenClose("Open_On", door1, door2);
    }

    private void OpenAll() {
        foreach (var door in this.blocks) {
            this.OpenClose("Open_On", door);
        }
    }

    private void Close(IMyFunctionalBlock door1, IMyFunctionalBlock door2 = null) {
        this.OpenClose("Open_Off", door1, door2);
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
            if (this.IsOpen(door)) {
                openCount++;
                this.areOpen.Add(door);
            } else {
                this.areClosed.Add(door);
            }
        }

        if (areOpen.Count > 0) {
            this.openTimer -= this.program.Runtime.TimeSinceLastRun.TotalMilliseconds;
            if (this.openTimer < 0) {
                this.CloseAll();
            } else {
                this.Lock(this.areClosed);
                this.Unlock(this.areOpen);
            }
        } else {
            this.Unlock();
            this.openTimer = this.timeOpen;
        }

        return true;
    }
}
/* AIRLOCK */
