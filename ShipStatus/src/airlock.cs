/*
 * AIRLOCK
 */
public struct AirlockDoors {
    public double openTimer;
    public List<IMyFunctionalBlock> blocks;
    public bool shouldAutoClose;

    public AirlockDoors(List<IMyFunctionalBlock> doors, bool shouldAutoClose, double openTimer) {
        this.blocks = doors;
        this.shouldAutoClose = shouldAutoClose;
        this.openTimer = openTimer;
    }
}

public class Airlock : Runnable {
    private Program program;
    private Dictionary<string, AirlockDoors> airlocks;
    private Dictionary<string, List<IMyFunctionalBlock>> locationToAirlockMap;
    // The name to match (Default will match regular doors). The capture group "(.*)" is used when grouping airlock doors, config  = "airlockDoorMatch", defualt = "Door(.*)";
    private System.Text.RegularExpressions.Regex include;
    // // The exclusion tag (can be anything). config = "airlockDoorExclude", default = "Hangar|Hatch";
    private System.Text.RegularExpressions.Regex exclude;
    // Manual airlocks are paired but not auto closed, config = "airlockDoorManual", default = "\\[AL\\]";
    private System.Text.RegularExpressions.Regex manual;
    // Duration before auto close (milliseconds), config = "airlockOpenTime", default = "750"
    private double timeOpen = 750f;
    private List<IMyFunctionalBlock> areClosed;
    private List<IMyFunctionalBlock> areOpen;
    private bool doAllDoors;
    private bool enabled;

    public Airlock(Program program) {
        this.program = program;
        this.airlocks = new Dictionary<string, AirlockDoors>();
        this.locationToAirlockMap = new Dictionary<string, List<IMyFunctionalBlock>>();
        this.areClosed = new List<IMyFunctionalBlock>();
        this.areOpen = new List<IMyFunctionalBlock>();

        this.Reset();
    }

    public void Reset() {
        this.Clear();

        this.include = Util.Regex(this.program.config.Get("airlockDoorMatch", "Door(.*)"));
        this.exclude = Util.Regex(this.program.config.Get("airlockDoorExclude", "Hangar|Hatch"));
        this.manual = Util.Regex(this.program.config.Get("airlockDoorManual", "\\[AL\\]"));
        this.timeOpen = Util.ParseFloat(this.program.config.Get("airlockOpenTime"), 750f);
        this.doAllDoors = this.program.config.Enabled("airlockAllDoors");
        this.enabled = this.program.config.Enabled("airlock");
    }

    public void Clear() {
        this.airlocks.Clear();
        this.locationToAirlockMap.Clear();
    }

    public void CheckAirlocks() {
        if (!this.enabled) {
            return;
        }
        foreach (string key in this.airlocks.Keys.ToList()) {
            AirlockDoors airlockDoors = this.airlocks.Get(key);
            this.Check(ref airlockDoors);
            this.airlocks[key] = airlockDoors;
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
        foreach (var keyval in this.locationToAirlockMap) {
            if (!this.doAllDoors && keyval.Value.Count < 2) {
                continue;
            }
            bool shouldAutoClose = true;
            foreach (var door in keyval.Value) {
                if (manual.Match(door.CustomName).Success) {
                    shouldAutoClose = false;
                }
            }
            this.airlocks.Add(keyval.Key, new AirlockDoors(keyval.Value, shouldAutoClose, this.timeOpen));
        }
    }

    public void Refresh() {}

    private bool IsOpen(IMyFunctionalBlock door) {
        return (door as IMyDoor).OpenRatio > 0;
    }

    private void Lock(List<IMyFunctionalBlock> doors) {
        foreach (var door in doors) {
            (door as IMyDoor).Enabled = false;
        }
    }

    private void Unlock(List<IMyFunctionalBlock> doors) {
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

    private void CloseAll(List<IMyFunctionalBlock> doors) {
        foreach (var door in doors) {
            this.OpenClose("Open_Off", door);
        }
    }

    public bool Check(ref AirlockDoors airlockDoors) {
        int openCount = 0;
        this.areClosed.Clear();
        this.areOpen.Clear();

        bool shouldLog = airlockDoors.blocks[0].CustomName[0] == '_';

        foreach (IMyFunctionalBlock door in airlockDoors.blocks) {
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

        if (this.areOpen.Count == 0) {
            this.Unlock(airlockDoors.blocks);
            airlockDoors.openTimer = this.timeOpen;

            return true;
        }

        airlockDoors.openTimer -= this.program.Runtime.TimeSinceLastRun.TotalMilliseconds;

        if (airlockDoors.openTimer < 0 && airlockDoors.shouldAutoClose) {
            this.CloseAll(airlockDoors.blocks);
        } else {
            this.Lock(this.areClosed);
            this.Unlock(this.areOpen);
        }

        return true;
    }
}
/* AIRLOCK */
