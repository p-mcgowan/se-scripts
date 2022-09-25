Dictionary<string, MyWaypointInfo> waypoints = new Dictionary<string, MyWaypointInfo>();

public void ParseDockingCoords(MyIGCMessage msg) {
    waypoints.Clear();

    MyTuple<string, Vector3D, Vector3D> data = msg.As<MyTuple<string, Vector3D, Vector3D>>();
    string dockType = data.Item1;
    IMyTerminalBlock dockingBlock = GetBlock(mergeBlockId);
    float offset = 1f;

    if (dockType == "connector") {
        if (!GetBatteryConnector()) {
            return;
        }
        dockingBlock = GetBlock(batteryConnectorId);
        offset = 0.5f;
    }

    Vector3D pos = data.Item2;
    Vector3D fwd = data.Item3;
    Vector3D target = pos + (fwd * Me.CubeGrid.GridSize);
    Vector3D toRc = GetOffsetFromRc(dockingBlock);
    Vector3D rcDockPosition = target + (fwd * (toRc.Length() - offset));

    waypoints["_moveOff"] = new MyWaypointInfo("_moveOff", target + (fwd * 20));
    waypoints["_approach"] = new MyWaypointInfo("_approach", target + (fwd * 60));
    waypoints["_rcPos"] = new MyWaypointInfo("_rcPos", rcDockPosition);
}

public bool TravelHas(string config, string what) {
    return config.IndexOf(what) != -1;
}

public bool TravelConfig(string cfg) {
    IMyRemoteControl remoteControl = (IMyRemoteControl)GetBlock(remoteControlId);
    if (TravelHas(cfg, "fwd")) {
        remoteControl.Direction = Base6Directions.Direction.Forward;
    } else if (TravelHas(cfg, "back")) {
        remoteControl.Direction = Base6Directions.Direction.Backward;
    } else if (TravelHas(cfg, "left")) {
        remoteControl.Direction = Base6Directions.Direction.Left;
    } else if (TravelHas(cfg, "right")) {
        remoteControl.Direction = Base6Directions.Direction.Right;
    }

    if (TravelHas(cfg, "slow")) {
        remoteControl.SetCollisionAvoidance(false);
        remoteControl.SetDockingMode(true);
        remoteControl.SpeedLimit = 5f;
    } else if (TravelHas(cfg, "fast")) {
        remoteControl.SetCollisionAvoidance(true);
        remoteControl.SetDockingMode(false);
        remoteControl.SpeedLimit = 50f;
    }

    return true;
}

public bool GoToLocation(string waypoint) {
    if (!waypoints.ContainsKey(waypoint)) {
        return AbortTask("Didn't find waypoint");
    }

    IMyRemoteControl remoteControl = (IMyRemoteControl)GetBlock(remoteControlId);

    if (remoteControl.CurrentWaypoint.IsEmpty()) {
        remoteControl.FlightMode = FlightMode.OneWay;
        remoteControl.AddWaypoint(waypoints[waypoint]);
        remoteControl.SetAutoPilotEnabled(true);

        return false;
    }

    if (remoteControl.IsAutoPilotEnabled) {
        return false;
    }

    return true;
}

int attempts = 5;
public bool FaceLocation(string waypoint) {
    if (!waypoints.ContainsKey(waypoint)) {
        return AbortTask("Didn't find waypoint");
    }

    IMyRemoteControl remoteControl = (IMyRemoteControl)GetBlock(remoteControlId);

    if (remoteControl.CurrentWaypoint.IsEmpty()) {
        attempts = 5;
        SetDroneThrusters(false);
        remoteControl.FlightMode = FlightMode.OneWay;
        remoteControl.AddWaypoint(waypoints[waypoint]);
        remoteControl.SetAutoPilotEnabled(true);

        return false;
    }

    if (attempts-- <= 0) {
        remoteControl.SetAutoPilotEnabled(false);
        remoteControl.ClearWaypoints();
        SetDroneThrusters(true);

        return true;
    }

    return false;
}

public bool ConnectToMergeBlock() {
    IMyShipMergeBlock mergeBlock = (IMyShipMergeBlock)GetBlock(mergeBlockId);
    mergeBlock.Enabled = true;

    return mergeBlock.IsConnected;
}

public bool ConnectToConnector() {
    if (!GetBatteryConnector()) {
        return AbortTask("Didn't find connector");
    }

    IMyShipConnector connector = (IMyShipConnector)GetBlock(batteryConnectorId);

    SetConnectorStrength(true);
    connector.Connect();

    return connector.Status == MyShipConnectorStatus.Connected;
}

public bool ReleaseMergeBlock() {
    IMyShipMergeBlock mergeBlock = (IMyShipMergeBlock)GetBlock(mergeBlockId);
    mergeBlock.Enabled = false;
    batteryConnectorId = -1;

    return !mergeBlock.IsConnected;
}

public bool ReleaseConnector() {
    if (!GetBatteryConnector()) {
        Log("Didn't find connector");

        return false;
    }

    IMyShipConnector connector = (IMyShipConnector)GetBlock(batteryConnectorId);
    connector.Disconnect();
    SetConnectorStrength(false);

    return connector.Status != MyShipConnectorStatus.Connected;
}
