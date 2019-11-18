/**
 * Airlock doors - Auto close doors and lock airlock pairs"
 *
 * By default, all doors with default names will auto close (see DOOR_MATCH).
 * For airlock doors to pair together (lock when the other is open), give them the same name. This works for any number of doors.
 * If you want to include all doors by default but exclude a few, name the doors so that they contain the DOOR_EXCLUDE tag.
 */

// Config vars
const double TIME_OPEN = 750f;           // Durarion before auto close (milliseconds)
const string DOOR_MATCH = "Door(.*)";  // The name to match (Default will match regular doors).
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

Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
}

public void Main(string argument, UpdateType updateSource)
 {
    if (!airlocks.Any()) {
        GetMappedAirlocks();
    }

    foreach (var al in airlocks) {
        al.Value.Test();
    }
}
