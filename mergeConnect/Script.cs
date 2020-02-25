public enum CFG {
    CARGO_LIGHT,
    CARGOCON,
    MERGE,
    SORTER,
    TOOL_LIGHT,
    TOOLCON,
    WELD_LIGHT,
    WELDCON
};

public Dictionary<CFG, string> settings = new Dictionary<CFG, string>{
    { CFG.MERGE, "Miner Merge Block" },
    { CFG.TOOLCON, "Miner Connector Tool" },
    { CFG.SORTER, "" },
    { CFG.CARGOCON, "" },
    { CFG.WELDCON, "" },
    { CFG.TOOL_LIGHT, "" },
    { CFG.CARGO_LIGHT, "" },
    { CFG.WELD_LIGHT, "" }
};

Dictionary<CFG, IMyTerminalBlock> blocks = new Dictionary<CFG, IMyTerminalBlock>();

// Multiple interface workaround
public MyShipConnectorStatus Do(CFG name, string what) {
    if (!blocks.ContainsKey(name)) {
        Echo("Block not found: " + name);
        return MyShipConnectorStatus.Unconnected;
    }

    IMyTerminalBlock block = blocks[name];
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
public bool Is(CFG name, string what) {
    if (!blocks.ContainsKey(name)) {
        Echo("Block not found: " + name);
        return false;
    }
    IMyShipConnector block = blocks[name] as IMyShipConnector;
    MyShipConnectorStatus status = block.Status;
    switch (what) {
        case "Connected": return status == MyShipConnectorStatus.Connected;
        case "Connectable": return status == MyShipConnectorStatus.Connectable;
        case "Unconnected": return status == MyShipConnectorStatus.Unconnected;
    }
    return false;
}

public void setLights() {
    Do(CFG.TOOL_LIGHT, "Disable");
    Do(CFG.CARGO_LIGHT, "Disable");
    Do(CFG.WELD_LIGHT, "Disable");
    if (Is(CFG.WELDCON, "Connected")) {
        Do(CFG.WELD_LIGHT, "Enable");
    }
    if (Is(CFG.TOOLCON, "Connected")) {
        Do(CFG.TOOL_LIGHT, "Enable");
    }
    if (Is(CFG.CARGOCON, "Connected")) {
        Do(CFG.CARGO_LIGHT, "Enable");
    }
}

public bool SetConnection(CFG which, string dir = "Lock") {
    if (blocks[which] == null) {
        return false;
    }
    if (which == CFG.CARGOCON) {
        Do(CFG.SORTER, "Enable");
        Do(CFG.TOOLCON, "Lock");
        Do(CFG.WELDCON, "Unlock");
    } else if (which == CFG.WELDCON) {
        Do(CFG.SORTER, "Disable");
        Do(CFG.TOOLCON, "Unlock");
    } else if (which == CFG.TOOLCON) {
        Do(CFG.SORTER, "Enable");
        Do(CFG.WELDCON, "Unlock");
    }

    Do(which, dir);
    setLights();
    if (dir == "Lock") {
        return Is(which, "Connected");
    } else {
        return !Is(which, "Connected");
    }
}

public void SetBlocks() {
    blocks.Clear();
    foreach (KeyValuePair<CFG, string> setting in settings) {
        if (setting.Value != "") {
            blocks.Add(setting.Key, GridTerminalSystem.GetBlockWithName(setting.Value));
        }
    }
}

public void Main(string arg) {
    SetBlocks();

    foreach (var dictionKeyVal in blocks) {
        if (dictionKeyVal.Value == null) {
            Echo("WARN: Couldnt find block: " + dictionKeyVal.Key);
        }
    }

    if (arg.Length > 0 && arg.ToLower() == "toggle") {
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
    } else if (arg.Length > 0 && arg.ToLower() == "merge") {
        bool mergeOn = blocks[CFG.MERGE].GetValue<bool>("OnOff");
        bool toolConnected = Is(CFG.TOOLCON, "Connected");
        bool toolConnectable = Is(CFG.TOOLCON, "Connectable");

        Echo("mergeOn: " + mergeOn.ToString());
        Echo("toolConnected: " + toolConnected.ToString());
        Echo("toolConnectable: " + toolConnectable.ToString());

        if (mergeOn) {  // CFG.Merge block off
            if (toolConnected) {
                Do(CFG.TOOLCON, "Unlock");
                Do(CFG.WELDCON, "Unlock");
                Do(CFG.MERGE, "OnOff_Off");
            } else if (toolConnectable) {
                Do(CFG.TOOLCON, "Lock");
            } else {
                Do(CFG.MERGE, "OnOff_Off");
            }
        } else {
            Do(CFG.MERGE, "OnOff_On");
            Do(CFG.TOOLCON, "Lock");  // Chances are this will not lock, but try anyway
        }
        setLights();
        return;
    }

    Do(CFG.MERGE, "OnOff_On");
    if (Is(CFG.CARGOCON, "Connectable")) {
        SetConnection(CFG.CARGOCON);
    } else if (Is(CFG.CARGOCON, "Connected")) {
        SetConnection(CFG.CARGOCON, "Unlock");
    } else {
        var grinders = new List<IMyShipGrinder>();
        GridTerminalSystem.GetBlocksOfType<IMyShipGrinder>(grinders);
        if (!grinders.Any() && Is(CFG.TOOLCON, "Connected")) {
            SetConnection(CFG.WELDCON);
        } else {
            SetConnection(CFG.TOOLCON);
        }
    }
}
