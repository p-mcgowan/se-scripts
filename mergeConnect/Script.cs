const string MERGE = "Merge Block Main";
const string SORTER = "IniScrub Conveyor Sorter";
const string TOOLCON = "Connector Main";
const string CARGOCON = "IniScrub Cargo Connector";
const string WELDCON = "IniScrub Welder Output Connector";

const string TOOL_LIGHT = "IniScrub Tool Light";
const string CARGO_LIGHT = "IniScrub Cargo Light";
const string WELD_LIGHT = "IniScrub Welder Light";

Dictionary<string, IMyTerminalBlock> blocks = new Dictionary<string, IMyTerminalBlock>();

// Multiple interface workaround
public MyShipConnectorStatus Do(string name, string what) {
    IMyTerminalBlock block = blocks[name];
    if (block == null) {
        Echo("Block not found: " + name);
        return MyShipConnectorStatus.Unconnected;
    }
    switch (what) {
        case "SwitchLock":
            (block as IMyShipConnector).ApplyAction("SwitchLock");
        break;
        case "Lock":
            (block as IMyShipConnector).ApplyAction("Lock");
        break;
        case "Unlock":
            (block as IMyShipConnector).ApplyAction("Unlock");
        break;
        case "OnOff_On":
            (block as IMyShipMergeBlock).ApplyAction("OnOff_On");
        break;
        case "OnOff_Off":
            (block as IMyShipMergeBlock).ApplyAction("OnOff_Off");
        break;
        case "Enable":
            (block as IMyFunctionalBlock).Enabled = true;
        break;
        case "Disable":
            (block as IMyFunctionalBlock).Enabled = false;
        break;
        default:
        case "Status":
            return (block as IMyShipConnector).Status;
    }
    return MyShipConnectorStatus.Connected;
}

// Connector helper
public bool Is(string name, string what) {
    IMyShipConnector block = blocks[name] as IMyShipConnector;
    if (block == null) {
        Echo("Block not found: " + name);
        return false;
    }
    MyShipConnectorStatus status = block.Status;
    switch (what) {
        case "Connected": return status == MyShipConnectorStatus.Connected;
        case "Connectable": return status == MyShipConnectorStatus.Connectable;
        case "Unconnected": return status == MyShipConnectorStatus.Unconnected;
    }
    return false;
}

public void setLights() {
    Do(TOOL_LIGHT, "Disable");
    Do(CARGO_LIGHT, "Disable");
    Do(WELD_LIGHT, "Disable");
    if (Is(WELDCON, "Connected")) {
        Do(WELD_LIGHT, "Enable");
    }
    if (Is(TOOLCON, "Connected")) {
        Do(TOOL_LIGHT, "Enable");
    }
    if (Is(CARGOCON, "Connected")) {
        Do(CARGO_LIGHT, "Enable");
    }
}

public bool SetConnection(string which, string dir = "Lock") {
    switch (which) {
        case CARGOCON:
            Do(SORTER, "Enable");
            Do(TOOLCON, "Lock");
            Do(WELDCON, "Unlock");
        break;
        case WELDCON:
            Do(SORTER, "Disable");
            Do(TOOLCON, "Unlock");
        break;
        case TOOLCON:
            Do(SORTER, "Enable");
            Do(WELDCON, "Unlock");
        break;
    }
    Do(which, dir);
    setLights();
    if (dir == "Lock") {
        return Is(which, "Connected");
    } else {
        return !Is(which, "Connected");
    }
}

public void Main(string arg) {
    blocks.Clear();
    blocks.Add(MERGE, GridTerminalSystem.GetBlockWithName(MERGE) as IMyShipMergeBlock);
    blocks.Add(SORTER, GridTerminalSystem.GetBlockWithName(SORTER) as IMyConveyorSorter);
    blocks.Add(TOOLCON, GridTerminalSystem.GetBlockWithName(TOOLCON) as IMyShipConnector);
    blocks.Add(CARGOCON, GridTerminalSystem.GetBlockWithName(CARGOCON) as IMyShipConnector);
    blocks.Add(WELDCON, GridTerminalSystem.GetBlockWithName(WELDCON) as IMyShipConnector);
    blocks.Add(TOOL_LIGHT, GridTerminalSystem.GetBlockWithName(TOOL_LIGHT) as IMyInteriorLight);
    blocks.Add(CARGO_LIGHT, GridTerminalSystem.GetBlockWithName(CARGO_LIGHT) as IMyInteriorLight);
    blocks.Add(WELD_LIGHT, GridTerminalSystem.GetBlockWithName(WELD_LIGHT) as IMyInteriorLight);

    var err = false;
    foreach (var dictionKeyVal in blocks) {
        if (dictionKeyVal.Value == null) {
            err = true;
            Echo("WARN: Couldnt find block: " + dictionKeyVal.Key);
        }
    }
    // if (err) { return; }

    if (arg.Length > 0 && arg == "TOGGLE") {
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
    } else if (arg.Length > 0 && arg == "MERGE") {
        bool mergeOn = blocks[MERGE].GetValue<bool>("OnOff");
        bool toolConnected = Is(TOOLCON, "Connected");
        if (mergeOn && Is(TOOLCON, "Connected")) {  // Merge block off
            Do(TOOLCON, "Unlock");
            Do(WELDCON, "Unlock");
            Do(MERGE, "OnOff_Off");
        } else if (mergeOn && Is(TOOLCON, "Connectable")) {
            Do(TOOLCON, "Lock");
        } else {
            Do(MERGE, "OnOff_On");
            Do(TOOLCON, "Lock");  // Chances are this will not lock, but try anyway
        }
        setLights();
        return;
    }

    Do(MERGE, "OnOff_On");
    if (Is(CARGOCON, "Connectable")) {
        SetConnection(CARGOCON);
    } else if (Is(CARGOCON, "Connected")) {
        SetConnection(CARGOCON, "Unlock");
    } else {
        var grinders = new List<IMyShipGrinder>();
        GridTerminalSystem.GetBlocksOfType<IMyShipGrinder>(grinders);
        if (!grinders.Any() && Is(TOOLCON, "Connected")) {
            SetConnection(WELDCON);
        } else {
            SetConnection(TOOLCON);
        }
    }
}
