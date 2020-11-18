/*
example custom data config:
connector=Minemobile Connector Drill
merge=Minemobile Merge Block Drill
hinge=Minemobile Hinge
landingGear=Minemobile Landing Gear
piston=Minemobile Piston
projector=Minemobile Projector
timer=Minemobile Timer Block
sensor=Minemobile Sensor
grinders=Grinders
welders=Welders
drills=Drills
outPanel=Sci-Fi One-Button Terminal
*/

List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
List<MyDetectedEntityInfo> entities = new List<MyDetectedEntityInfo>();
IMyMotorAdvancedStator hinge;
IMyLandingGear landingGear;
IMyPistonBase piston;
IMyShipConnector connector;
IMyProjector projector;
IMyBlockGroup welders;
IMyBlockGroup grinders;
IMyBlockGroup drills;
IMyShipMergeBlock merge;
IMyTimerBlock timer;
IMySensorBlock sensor;
IMyTextSurfaceProvider outPanel;

float pistonDownSpeed = -0.5f;
float pistonUpSpeed = 5f;
// float hingeSpeedRPM = 1.5f;

// public Program() {
//     Runtime.UpdateFrequency = UpdateFrequency.Update100;
// }


public void Main(string argument, UpdateType updateSource) {
    if (!ParseCustomData()) {
        Echo("Exiting");
        return;
    }
    string state = "";
    if (argument == "toggle") {
        bool retracting = GetOnOffBlockGroup(grinders);
        StartStop(retracting);
        if (!PistonNearMin() && !PistonNearMax()) {
            piston.Velocity = -1 * piston.Velocity;
        }
        state = "Mode: " + (retracting ? "Drilling" : "Retracting");
    } else if (argument == "stop") {
        EmergencyStop();
        state = "Stopped";
    } else {
        timer.Enabled = true;
        piston.Enabled = true;
        state = TickState();
    }

    Echo(
        $"connected: {connector.Status == MyShipConnectorStatus.Connected}\n" +
        $"merged: {merge.IsConnected}\n" +
        $"SensorDetecting: {SensorDetecting()}\n" +
        $"p.MaxLimit: {piston.MaxLimit}\n" +
        $"p.MinLimit: {piston.MinLimit}\n" +
        $"p.Velocity: {piston.Velocity}\n" +
        $"p.CurrentPosition: {piston.CurrentPosition}\n" +
        $"t.Enabled: {timer.Enabled}\n"
    );

    IMyTextSurface surface = ((IMyTextSurfaceProvider)Me).GetSurface(0);
    surface.ContentType = ContentType.TEXT_AND_IMAGE;
    surface.WriteText(state, false);

    if (outPanel != null) {
      IMyTextSurface output = outPanel.GetSurface(0);
      output.ContentType = ContentType.TEXT_AND_IMAGE;
      output.WriteText(state, false);
    }
}

public void EmergencyStop() {
    projector.Enabled = false;
    SetOnOffBlockGroup(welders, false);
    SetOnOffBlockGroup(grinders, false);
    SetOnOffBlockGroup(drills, false);
    piston.Enabled = false;
    timer.Enabled = false;
}

public bool GetOnOffBlockGroup(IMyBlockGroup group) {
    blocks.Clear();
    group.GetBlocks(blocks);
    foreach (IMyFunctionalBlock block in blocks) {
        if (block.Enabled) {
            return true;
        }
    }

    return false;
}

public void SetOnOffBlockGroup(IMyBlockGroup group, bool enabled) {
    blocks.Clear();
    group.GetBlocks(blocks);
    foreach (IMyFunctionalBlock block in blocks) {
        block.Enabled = enabled;
    }
}

public void StartStop(bool start) {
    projector.Enabled = start;
    SetOnOffBlockGroup(welders, start);
    SetOnOffBlockGroup(grinders, !start);
    SetOnOffBlockGroup(drills, true);
}

public bool SensorDetecting() {
    entities.Clear();
    sensor.DetectedEntities(entities);

    return entities.Count() != 0;
}

public void Disconnect() {
    if (!merge.IsConnected) {
        Echo("Not dropping the drill");
        return;
    }
    connector.Disconnect();
}

public void Unmerge() {
    if (connector.Status != MyShipConnectorStatus.Connected) {
        Echo("Not dropping the drill");
        return;
    }
    merge.Enabled = false;
}

public bool PistonNearMax() {
    return piston.CurrentPosition >= piston.MaxLimit - 0.2f && piston.CurrentPosition <= piston.MaxLimit + 0.2f;
}

public bool PistonNearMin() {
    return piston.CurrentPosition >= piston.MinLimit - 0.2f && piston.CurrentPosition <= piston.MinLimit + 0.2f;
}

public string Retract() {
    StartStop(false);
    bool connected = connector.Status == MyShipConnectorStatus.Connected;
    bool merged = merge.IsConnected;
    string state = "unknown";

    if (PistonNearMin() && merged) {
        if (connected) {
            Disconnect();
        } else {
            piston.Velocity = pistonUpSpeed;
        }
        return "reposition up";
    }
    if (PistonNearMin() && connected) {
        if (!merged) {
            merge.Enabled = true;
            return "merge";
        }
        Disconnect();
        return "disconnect";
    }
    if (PistonNearMax() && merged) {
        if (connector.Status == MyShipConnectorStatus.Connectable) {
            connector.Connect();
            return "connect";
        }

        piston.Velocity = pistonDownSpeed;

        if (SensorDetecting()) {
            piston.Velocity = -0.05f;
        } else {
            SetOnOffBlockGroup(grinders, false);
            timer.Enabled = false;
            piston.Velocity = -5f;
            SetOnOffBlockGroup(drills, false);
            return "Idle";
        }
        Unmerge();
        return "reposition down";
    }

    if ((PistonNearMax() && connected) || (piston.Velocity == -0.05f && connected) || (piston.Velocity == pistonDownSpeed)) {
        piston.Velocity = pistonDownSpeed;
        return "grind";
    }

    return state;
}

public string Drill() {
    StartStop(true);
    bool connected = connector.Status == MyShipConnectorStatus.Connected;
    bool merged = merge.IsConnected;
    string state = "unknown";

    // if (hinge.RotorLock && !landingGear.IsLocked) {
    //     hinge.RotorLock = false;
    //     hinge.TargetVelocityRPM = -1f * hingeSpeedRPM;
    //     // start timer
    //     return "Drill:init";
    // }
    // if (hinge.TargetVelocityRPM < 0f && landingGear.LockMode == LandingGearMode.Unlocked) {
    //     return "Drill:starting";
    // }
    // if (!hinge.RotorLock && landingGear.LockMode == LandingGearMode.ReadyToLock) {
    //     hinge.RotorLock = true;
    //     landingGear.Lock();
    //     StartStop(!GetOnOffBlockGroup(grinders));
    //     return "Drill:ready";
    // }

    if (PistonNearMin() && merged) {
        StartStop(!GetOnOffBlockGroup(grinders));
        connector.Connect();
        state = "connect";
    }
    // if (PistonNearMin() && connected && !merged) {
    //     piston.Velocity = pistonUpSpeed;
    //     return "reposition";
    // }
    if (PistonNearMin() && merged && connected && projector.RemainingBlocks == 0) {
        Unmerge();
        piston.Velocity = pistonUpSpeed;
        return "reposition";
    }
    if (PistonNearMax() && !merged) {
        merge.Enabled = true;
        state = "merge";
    }
    if (PistonNearMax() && merged && connected) {
        Disconnect();
        state = "disconnect";
    }
    if (PistonNearMax() && merged && !connected) {
        piston.Velocity = pistonDownSpeed;
        return "mine";
    }
    if (piston.Velocity == pistonDownSpeed) {
        return "mine";
    }

    return state;
}

public string TickState() {
    string state = "NONE";
    if (GetOnOffBlockGroup(grinders)) {
        state = "Retract:" + Retract();
    } else {
        state = "Drill:" + Drill();
    }

    return state;
}

public bool ParseCustomData() {
    try {
        string sx = Me.CustomData;
        if (Me.CustomData == null || Me.CustomData == "") {
            return false;
        }

        var items = sx.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Split(new[] { '=' }));
        foreach (string[] item in items) {
            if (item.Count() < 2) {
                Echo($"Malformed config {item[0]}");
                continue;
            }

            switch (item[0]) {
                case "hinge":
                    hinge = (IMyMotorAdvancedStator)GridTerminalSystem.GetBlockWithName(item[1]);
                    break;
                case "landingGear":
                    landingGear = (IMyLandingGear)GridTerminalSystem.GetBlockWithName(item[1]);
                    break;
                case "piston":
                    piston = (IMyPistonBase)GridTerminalSystem.GetBlockWithName(item[1]);
                    break;
                case "connector":
                    connector = (IMyShipConnector)GridTerminalSystem.GetBlockWithName(item[1]);
                    break;
                case "projector":
                    projector = (IMyProjector)GridTerminalSystem.GetBlockWithName(item[1]);
                    break;
                case "welders":
                    welders = (IMyBlockGroup)GridTerminalSystem.GetBlockGroupWithName(item[1]);
                    break;
                case "grinders":
                    grinders = (IMyBlockGroup)GridTerminalSystem.GetBlockGroupWithName(item[1]);
                    break;
                case "drills":
                    drills = (IMyBlockGroup)GridTerminalSystem.GetBlockGroupWithName(item[1]);
                    break;
                case "merge":
                    merge = (IMyShipMergeBlock)GridTerminalSystem.GetBlockWithName(item[1]);
                    break;
                case "timer":
                    timer = (IMyTimerBlock)GridTerminalSystem.GetBlockWithName(item[1]);
                    break;
                case "sensor":
                    sensor = (IMySensorBlock)GridTerminalSystem.GetBlockWithName(item[1]);
                    break;
                case "outPanel":
                    outPanel = (IMyTextSurfaceProvider)GridTerminalSystem.GetBlockWithName(item[1]);
                    break;
                default:
                    Echo($"ignoring: {item[0]}:{item[1]}");
                    break;
            }
        }

        string notFound = BlocksNotFound();
        if (BlocksNotFound() != "") {
            Echo($"{notFound} not found");
            return false;
        }

        return true;
    } catch (Exception e) {
        Echo(e.ToString());
        return false;
    }
}

public string BlocksNotFound() {
    string notFound = "";

    if (hinge == null) {
        notFound += "hinge, ";
    }
    if (landingGear == null) {
        notFound += "landingGear, ";
    }
    if (piston == null) {
        notFound += "piston, ";
    }
    if (connector == null) {
        notFound += "connector, ";
    }
    if (projector == null) {
        notFound += "projector, ";
    }
    if (welders == null) {
        notFound += "welders, ";
    }
    if (grinders == null) {
        notFound += "grinders, ";
    }
    if (drills == null) {
        notFound += "drills, ";
    }
    if (merge == null) {
        notFound += "merge, ";
    }
    if (timer == null) {
        notFound += "timer, ";
    }
    if (sensor == null) {
        notFound += "sensor, ";
    }

    return notFound == "" ? "" : notFound.Substring(0, notFound.Length - 2);
}
