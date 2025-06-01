/*
example custom data config:
[blocks]
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

[output]
outPanel=Sci-Fi One-Button Terminal
*/

public enum RunMode {
    Drill,
    Retract
};

MyIni ini = new MyIni();
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
IMySensorBlock sensor;
IMyTextSurfaceProvider outPanel;

float pistonDownSpeed = -0.5f;
float pistonUpSpeed = 5f;
float pistonDownFindConnectorSpeed = -0.05f;
float pistonDistanceThreshold = 0.1f;

public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.None;
}

public void Main(string argument, UpdateType updateSource) {
    if (!ParseCustomData()) {
        Stop(true);
        DrawStatus("Not configured");
        return;
    }

    if (argument == "stop" || !landingGear.IsLocked) {
        Stop(true);
        DrawStatus(landingGear.IsLocked ? "Stopped" : "Unlocked", updateSource);
        return;
    }

    bool grinding = GetOnOffBlockGroup(grinders);
    RunMode current = grinding ? RunMode.Retract : RunMode.Drill;
    RunMode next = grinding ? RunMode.Drill : RunMode.Retract;
    if (argument == "toggle") {
        piston.Velocity = 0f;
        SetRunMode(next);
        DrawStatus($"Mode: {Enum.GetName(typeof (RunMode), next)}", updateSource);
        return;
    }
    if (argument == "start") {
        SetRunMode(current);
        Runtime.UpdateFrequency = UpdateFrequency.Update100;
        DrawStatus($"Mode: {Enum.GetName(typeof (RunMode), current)}", updateSource);
        return;
    }
    if ((updateSource & UpdateType.Update100) == UpdateType.Update100) {
        DrawStatus(TickState(grinding), updateSource);
        return;
    }
}

public string TickState(bool grinding) {
    return grinding ? Retract() : Drill();
}

public string Retract() {
    bool connected = connector.Status == MyShipConnectorStatus.Connected;
    bool merged = merge.IsConnected;

    if (PistonNearMin() && merged) {
        if (connected) {
            Disconnect();
        } else {
            piston.Velocity = pistonUpSpeed;
        }
        return "Retract:up";
    }
    if (PistonNearMin() && connected) {
        if (!merged) {
            merge.Enabled = true;
            return "Retract:merge";
        }
        Disconnect();
        return "Retract:disconnect";
    }
    if (PistonNearMax() && merged) {
        if (connector.Status == MyShipConnectorStatus.Connectable) {
            connector.Connect();
            return "Retract:connect";
        }

        piston.Velocity = pistonDownSpeed;

        if (SensorDetecting()) {
            piston.Velocity = pistonDownFindConnectorSpeed;
        } else {
            Stop();
            return "Idle";
        }
        Unmerge();
        return "Retract:down";
    }

    if ((PistonNearMax() && connected) || (piston.Velocity == pistonDownFindConnectorSpeed && connected) || (piston.Velocity == pistonDownSpeed)) {
        piston.Velocity = pistonDownSpeed;
        return "Retract:grind";
    }

    piston.Velocity = pistonUpSpeed;
    return "Retract:reposition up";
}

public string Drill() {
    bool connected = connector.Status == MyShipConnectorStatus.Connected;
    bool merged = merge.IsConnected;
    string state = "Drill:unknown";

    if (PistonNearMin() && merged) {
        connector.Connect();
        state = "Drill:connect";
    }
    if (PistonNearMin() && merged && connected && projector.RemainingBlocks == 0) {
        Unmerge();
        piston.Velocity = pistonUpSpeed;
        return "Drill:reposition";
    }
    if (PistonNearMax() && !merged) {
        merge.Enabled = true;
        state = "Drill:merge";
    }
    if (PistonNearMax() && merged && connected) {
        Disconnect();
        state = "Drill:disconnect";
    }
    if (PistonNearMax() && merged && !connected) {
        piston.Velocity = pistonDownSpeed;
        return "Drill:mine";
    }
    if (piston.Velocity == pistonDownSpeed) {
        return "Drill:mine";
    }
    if (connected && !merged) {
        if (PistonNearMin() && (piston.Velocity == pistonDownSpeed || piston.Velocity == 0f)) {
            merge.Enabled = true;
        }
        return "Drill:realign";
    }

    return state;
}

public void DrawStatus(string state, UpdateType updateSource = UpdateType.None) {
    Echo(
        $"state: {state}\n" +
        $"connected: {connector.Status == MyShipConnectorStatus.Connected}\n" +
        $"merged: {merge.IsConnected}\n" +
        $"SensorDetecting: {SensorDetecting()}\n" +
        $"p.Velocity: {piston.Velocity}\n" +
        $"prj.Enabled: {projector.Enabled}\n" +
        $"tick: {DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond}\n" +
        $"source: {updateSource.ToString()}"
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

public void SetRunMode(RunMode mode) {
    bool drilling = (mode == RunMode.Drill);
    piston.Enabled = true;
    projector.Enabled = drilling;
    SetOnOffBlockGroup(welders, drilling);
    SetOnOffBlockGroup(grinders, !drilling);
    SetOnOffBlockGroup(drills, true);
}

public float SwitchPistonDirection() {
    if (PistonNearMin() || PistonNearMax()) {
        return piston.Velocity;
    }
    if (piston.Velocity == pistonDownFindConnectorSpeed) {
        return (piston.Velocity = pistonUpSpeed);
    }
    return (piston.Velocity = -1 * piston.Velocity);
}

public void Stop(bool emergency = false) {
    projector.Enabled = false;
    SetOnOffBlockGroup(grinders, false);
    SetOnOffBlockGroup(drills, false);
    SetOnOffBlockGroup(welders, false);
    Runtime.UpdateFrequency = UpdateFrequency.None;
    if (emergency) {
        piston.Enabled = false;
    } else {
        piston.Velocity = -1 * pistonUpSpeed;
    }
}

public bool GetOnOffBlockGroup(IMyBlockGroup blockGroup) {
    blocks.Clear();
    blockGroup.GetBlocks(blocks);
    foreach (IMyFunctionalBlock block in blocks) {
        if (block.Enabled) {
            Echo("returning true");
            return true;
        }
    }

    return false;
}

public void SetOnOffBlockGroup(IMyBlockGroup blockGroup, bool enabled) {
    blocks.Clear();
    blockGroup.GetBlocks(blocks);
    foreach (IMyFunctionalBlock block in blocks) {
        block.Enabled = enabled;
    }
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
    return Math.Abs(piston.CurrentPosition - piston.MaxLimit) < pistonDistanceThreshold;
}

public bool PistonNearMin() {
    return Math.Abs(piston.CurrentPosition - piston.MinLimit) < pistonDistanceThreshold;
}

public bool ParseCustomData() {
    MyIniParseResult result;
    if (!ini.TryParse(Me.CustomData, out result)) {
        Echo("failed to parse config");
        return false;
    }

    hinge = (IMyMotorAdvancedStator)GridTerminalSystem.GetBlockWithName(ini.Get("blocks", "hinge").ToString());
    landingGear = (IMyLandingGear)GridTerminalSystem.GetBlockWithName(ini.Get("blocks", "landingGear").ToString());
    piston = (IMyPistonBase)GridTerminalSystem.GetBlockWithName(ini.Get("blocks", "piston").ToString());
    connector = (IMyShipConnector)GridTerminalSystem.GetBlockWithName(ini.Get("blocks", "connector").ToString());
    projector = (IMyProjector)GridTerminalSystem.GetBlockWithName(ini.Get("blocks", "projector").ToString());
    welders = (IMyBlockGroup)GridTerminalSystem.GetBlockGroupWithName(ini.Get("blocks", "welders").ToString());
    grinders = (IMyBlockGroup)GridTerminalSystem.GetBlockGroupWithName(ini.Get("blocks", "grinders").ToString());
    drills = (IMyBlockGroup)GridTerminalSystem.GetBlockGroupWithName(ini.Get("blocks", "drills").ToString());
    merge = (IMyShipMergeBlock)GridTerminalSystem.GetBlockWithName(ini.Get("blocks", "merge").ToString());
    sensor = (IMySensorBlock)GridTerminalSystem.GetBlockWithName(ini.Get("blocks", "sensor").ToString());

    outPanel = (IMyTextSurfaceProvider)GridTerminalSystem.GetBlockWithName(ini.Get("output", "outPanel").ToString());

    string notFound = BlocksNotFound();
    if (notFound != "") {
        Echo($"{notFound} not found");
        return false;
    }

    return true;
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
    if (sensor == null) {
        notFound += "sensor, ";
    }

    return notFound == "" ? "" : notFound.Substring(0, notFound.Length - 2);
}
