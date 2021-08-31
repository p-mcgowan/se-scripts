/*
[blocks]
connector=Miner Connector Drill
landingGears=Miner Landing Gears
merge=Miner Merge Block
piston=Miner Piston
projector=Miner Projector
rotor=Miner Advanced Rotor
sensor=Miner Sensor
drills=Miner Drills
grinders=Miner Grinders
welders=Miner Welders

[output]
outPanel=Miner Sci-Fi One-Button Terminal
*/

public enum RunMode {
    Drill,
    Retract
};

MyIni ini = new MyIni();
List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
List<MyDetectedEntityInfo> entities = new List<MyDetectedEntityInfo>();

IMyPistonBase piston;
IMyShipConnector connector;
IMyProjector projector;
IMyBlockGroup welders;
IMyBlockGroup landingGears;
IMyBlockGroup grinders;
IMyBlockGroup drills;
IMyShipMergeBlock merge;
IMySensorBlock sensor;
IMyTextSurfaceProvider outPanel;
IMyMotorAdvancedStator rotor;

// float rotorLowerLimitRad = 270f * ((float)Math.PI / 180f);
// float rotorUpperLimitRad = 360f * ((float)Math.PI / 180f);
float rotorUpperLimitRad = (float)Math.PI;
float rotorLowerLimitRad = (float)Math.PI;
float rotorAngleThresholdRad = 0.05f;
float rotorSpinSpeedRPM = 1.6f;
float pistonDrillSpeed = -0.045f;
float pistonGrindSpeed = -0.5f;
float pistonUpSpeed = 1f;
float pistonDownFindConnectorSpeed = -0.05f;
float pistonDistanceThreshold = 0.1f;

public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.None;

    if (!ParseCustomData()) {
        Stop(true);
        DrawStatus("Not configured");
        return;
    }
}

public void Main(string argument, UpdateType updateSource) {
    if (!ParseCustomData()) {
        Stop(true);
        DrawStatus("Not configured");
        return;
    }

    if (argument == "stop" || !IsLandingGearLocked()) {
        Stop(true);
        DrawStatus(IsLandingGearLocked() ? "Stopped" : "Unlocked", updateSource);
        return;
    }

    bool grinding = GetOnOffBlockGroup(grinders);
    RunMode current = grinding ? RunMode.Retract : RunMode.Drill;
    RunMode next = grinding ? RunMode.Drill : RunMode.Retract;

    if (argument == "toggle") {
        piston.Velocity = 0f;
        SetRunMode(next);
        DrawStatus($"Mode: {Enum.GetName(typeof (RunMode), next)}", updateSource);
    } else if (argument == "start") {
        SetRunMode(current);
        Runtime.UpdateFrequency = UpdateFrequency.Update100;
        DrawStatus($"Mode: {Enum.GetName(typeof (RunMode), current)}", updateSource);
    } else if ((updateSource & UpdateType.Update100) == UpdateType.Update100) {
        DrawStatus(TickState(grinding), updateSource);
    }
}

public string TickState(bool grinding) {
    return grinding ? Retract() : Drill();
}

public string Retract() {
    if (!rotor.RotorLock) {
        rotor.LowerLimitRad = rotorLowerLimitRad;
        rotor.UpperLimitRad = rotorUpperLimitRad;
        rotor.TargetVelocityRPM = -1f * rotorSpinSpeedRPM;
        piston.Velocity = 0f;

        if (rotor.Angle >= rotorLowerLimitRad - rotorAngleThresholdRad && rotor.Angle <= rotorUpperLimitRad + rotorAngleThresholdRad) {
            piston.Velocity = -1 * pistonUpSpeed;
            rotor.RotorLock = true;
        } else {
            return "Retract:rotating";
        }
    }

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

        piston.Velocity = pistonGrindSpeed;

        if (SensorDetecting()) {
            piston.Velocity = pistonDownFindConnectorSpeed;
        } else {
            Stop();
            return "Idle";
        }
        Unmerge();
        return "Retract:down";
    }

    if ((PistonNearMax() && connected) || (piston.Velocity == pistonDownFindConnectorSpeed && connected) || (piston.Velocity == pistonGrindSpeed)) {
        piston.Velocity = pistonGrindSpeed;
        return "Retract:grind";
    }

    piston.Velocity = pistonUpSpeed;
    if (merged && connected) {
        Disconnect();
    }

    return "Retract:reposition up";
}

public string Drill() {
    rotor.LowerLimitDeg = float.MinValue;
    rotor.UpperLimitDeg = float.MaxValue;
    rotor.TargetVelocityRPM = rotorSpinSpeedRPM;
    rotor.RotorLock = false;

    bool connected = connector.Status == MyShipConnectorStatus.Connected;
    bool merged = merge.IsConnected;
    string state = "Drill:unknown";

    if (PistonNearMin() && merged) {
        connector.Connect();
        state = "Drill:connect";
    }
    if (PistonNearMin() && merged && connected && projector.RemainingBlocks == 0) {
        if (DrillsNearEmpty()) {
            Unmerge();
            piston.Velocity = pistonUpSpeed;
            return "Drill:reposition";
        }

        return "Drill:emptying";
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
        piston.Velocity = pistonDrillSpeed;
        return "Drill:mine";
    }
    if (piston.Velocity == pistonDrillSpeed) {
        return "Drill:mine";
    }
    if (connected && !merged) {
        if (PistonNearMin() && (piston.Velocity == pistonDrillSpeed || piston.Velocity == 0f)) {
            merge.Enabled = true;
        }
        return "Drill:realign";
    }
    if (piston.Velocity == 0f) {
        piston.Velocity = pistonDrillSpeed;
        return "Drill:mine";
    }

    return state;
}

public void DrawStatus(string state, UpdateType updateSource = UpdateType.None) {
    string report = $"state: {state}"
       + "\n" + $"connected: {(connector == null ? "--" : (connector.Status == MyShipConnectorStatus.Connected).ToString())}"
       + "\n" + $"merged: {(merge == null ? "--" : merge.IsConnected.ToString())}"
       + "\n" + $"SensorDetecting: {SensorDetecting()}"
       + "\n" + $"p.Velocity: {(piston == null ? "--" : piston.Velocity.ToString())}"
       + "\n" + $"prj.Enabled: {(projector == null ? "--" : projector.Enabled.ToString())}"
       + "\n" + $"tick: {DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond}"
       + "\n" + $"source: {updateSource.ToString()}";

    Echo(report);

    IMyTextSurface surface = ((IMyTextSurfaceProvider)Me).GetSurface(0);
    surface.ContentType = ContentType.TEXT_AND_IMAGE;
    surface.WriteText(report, false);

    if (outPanel != null) {
      IMyTextSurface output = outPanel.GetSurface(0);
      output.ContentType = ContentType.TEXT_AND_IMAGE;
      output.WriteText(state, false);
    }
}

public void SetRunMode(RunMode mode) {
    bool drilling = (mode == RunMode.Drill);
    piston.Enabled = true;
    rotor.RotorLock = false;
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
    Runtime.UpdateFrequency = UpdateFrequency.None;

    if (emergency) {
        rotor.RotorLock = true;
        rotor.TargetVelocityRPM = 0f;
    } else {
        rotor.RotorLock = false;
        rotor.LowerLimitRad = 0f;
        rotor.UpperLimitRad = 0f;
        piston.Velocity = -2 * pistonUpSpeed;
        rotor.TargetVelocityRPM = rotorSpinSpeedRPM;
    }

    projector.Enabled = false;

    SetOnOffBlockGroup(grinders, false);
    SetOnOffBlockGroup(drills, false);
    SetOnOffBlockGroup(welders, false);

    if (piston != null && emergency) {
        piston.Enabled = false;
    }
}

public bool GetOnOffBlockGroup(IMyBlockGroup blockGroup) {
    blocks.Clear();
    blockGroup.GetBlocks(blocks);
    foreach (IMyFunctionalBlock block in blocks) {
        if (block.Enabled) {
            return true;
        }
    }

    return false;
}

public void SetOnOffBlockGroup(IMyBlockGroup blockGroup, bool enabled) {
    Echo($"{blockGroup.Name}: {enabled}");
    blocks.Clear();
    blockGroup.GetBlocks(blocks);
    foreach (IMyFunctionalBlock block in blocks) {
        block.Enabled = enabled;
    }
}

public bool DrillsNearEmpty() {
    blocks.Clear();
    drills.GetBlocks(blocks);
    foreach (IMyShipDrill drill in blocks) {
        var inv = drill.GetInventory(0);
        var pct = (float)inv.CurrentVolume / (float)inv.MaxVolume;
        Echo($"{pct}");
        if (pct > 0.1f) {
            return false;
        }
    }

    return true;
}

public bool IsLandingGearLocked() {
    blocks.Clear();
    landingGears.GetBlocks(blocks);
    foreach (IMyLandingGear block in blocks) {
        if (block.IsLocked) {
            return true;
        }
    }

    return false;
}

public bool SensorDetecting() {
    if (sensor == null) {
        return false;
    }
    entities.Clear();
    sensor.DetectedEntities(entities);

    return entities.Count != 0;
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

    landingGears = (IMyBlockGroup)GridTerminalSystem.GetBlockGroupWithName(ini.Get("blocks", "landingGears").ToString());
    piston = (IMyPistonBase)GridTerminalSystem.GetBlockWithName(ini.Get("blocks", "piston").ToString());
    connector = (IMyShipConnector)GridTerminalSystem.GetBlockWithName(ini.Get("blocks", "connector").ToString());
    projector = (IMyProjector)GridTerminalSystem.GetBlockWithName(ini.Get("blocks", "projector").ToString());
    welders = (IMyBlockGroup)GridTerminalSystem.GetBlockGroupWithName(ini.Get("blocks", "welders").ToString());
    grinders = (IMyBlockGroup)GridTerminalSystem.GetBlockGroupWithName(ini.Get("blocks", "grinders").ToString());
    drills = (IMyBlockGroup)GridTerminalSystem.GetBlockGroupWithName(ini.Get("blocks", "drills").ToString());
    merge = (IMyShipMergeBlock)GridTerminalSystem.GetBlockWithName(ini.Get("blocks", "merge").ToString());
    sensor = (IMySensorBlock)GridTerminalSystem.GetBlockWithName(ini.Get("blocks", "sensor").ToString());
    rotor = (IMyMotorAdvancedStator)GridTerminalSystem.GetBlockWithName(ini.Get("blocks", "rotor").ToString());

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

    if (landingGears == null) {
        notFound += "landingGears, ";
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

    if (notFound == "") {
        Echo("All blocks found");
        return "";
    }

    return notFound.Substring(0, notFound.Length - 2);
}
