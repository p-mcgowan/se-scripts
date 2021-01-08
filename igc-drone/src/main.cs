IGCEmitter emitter;
List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
long mergeBlockId;
long connectorId;
long remoteControlId;
long batteryConnectorId = -1;
string state = "Idle";
string uuid = "Drone 000";

public void SetupListeners() {
    emitter = new IGCEmitter(this);
    emitter.Hello(uuid);
    emitter.OnUnicast("DOCKING_REQUEST", (MyIGCMessage msg) => {
        Log("received DOCKING_REQUEST response");
        InitDocking(msg.As<MyTuple<string, Vector3D, Vector3D>>());
    });
    emitter.On("BATTERY", (MyIGCMessage msg) => {
        state = "battery_retrieval";
        // TODO: pull battery off station first, then swap at farm, then back to station
        // ACK sender, on reply then start
        // <= req battery
        // ask station for merge (lowest battery pack if more than 2)
        // ask solar farm for connector
        // ask solar farm for merge (highest battery pack if more than 2)
        // ask station for connectorresponse
        // idle
        emitter.Emit("DOCKING_REQUEST", "merge", emitter.Who("Solar Farm"));
    });
    emitter.On("STATUS", (MyIGCMessage msg) => {
        Log($"responding to ping");
        emitter.Emit("STATUS", state, msg.Source);
    });
    emitter.OnUnicast("STATUS", (MyIGCMessage msg) => {
        Log($"responding to ping");
        emitter.Emit("STATUS", state, msg.Source);
    });
}

delegate bool ActionItem();
List<ActionItem> actions = new List<ActionItem>();

public void Dock() {
    if (actions.Count > 0) {
        bool done = actions[0]();
        if (done) {
            actions.RemoveAt(0);
        }
    } else {
        Runtime.UpdateFrequency &= ~UpdateFrequency.Update100;
        IMyShipMergeBlock mergeBlock = (IMyShipMergeBlock)GetBlock(mergeBlockId);
        if (mergeBlock.IsConnected) {
            state = "battery_return";
            emitter.Emit("DOCKING_REQUEST", "connector", emitter.Who("Pertram Station"));
        } else {
            // return to dock
            state = "idle";
        }
    }
}

public IMyTerminalBlock GetBlock(long id) {
    return GridTerminalSystem.GetBlockWithId(id);
}

public void GetDroneBlocks() {
    blocks.Clear();
    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocks, b =>
        b.IsSameConstructAs(Me) &&
        b.CustomName.Contains("Drone") &&
        (b is IMyShipConnector || b is IMyShipMergeBlock || b is IMyRemoteControl)
    );

    foreach (var block in blocks) {
        if (block is IMyShipMergeBlock){
            mergeBlockId = block.EntityId;
        } else if (block is IMyShipConnector){
            connectorId = block.EntityId;
        } else if (block is IMyRemoteControl) {
            remoteControlId = block.EntityId;
        }
    }
}

public bool GetBatteryConnector() {
    blocks.Clear();
    if (batteryConnectorId == -1 || GetBlock(batteryConnectorId) == null || GetBlock(batteryConnectorId).WorldMatrix.Translation == Vector3.Zero) {
        GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(blocks, b => b.IsSameConstructAs(Me) && b.CustomName.Contains("BattPack"));
        if (blocks.Count == 1) {
            batteryConnectorId = blocks[0].EntityId;
            return true;
        } else {
            Log($"Could not find battery connector ({blocks.Count})");
            return false;
        }
    }

    return true;
}

public void InitDocking(MyTuple<string, Vector3D, Vector3D> data) {
    string dockType = data.Item1;
    if (dockType == "connector" && !GetBatteryConnector()) {
        return;
    }

    Vector3D pos = data.Item2;
    Vector3D fwd = data.Item3;
    Vector3D target = pos + (fwd * Me.CubeGrid.GridSize);
    Vector3D toRc = GetOffsetFromRc(dockType == "merge" ? GetBlock(mergeBlockId) : GetBlock(batteryConnectorId));
    Vector3D rcDockPosition = target + (fwd * (toRc.Length() - 1));
    Vector3D approach = target + (fwd * 75);

    ((IMyRemoteControl)GetBlock(remoteControlId)).ClearWaypoints();
    actions.Clear();

    actions.Add(() => {
        IMyRemoteControl remoteControl = (IMyRemoteControl)GetBlock(remoteControlId);

        if (remoteControl.CurrentWaypoint.IsEmpty()) {
            remoteControl.Direction = Base6Directions.Direction.Forward;
            remoteControl.FlightMode = FlightMode.OneWay;
            remoteControl.SetCollisionAvoidance(true);
            remoteControl.SetDockingMode(false);
            remoteControl.SpeedLimit = 50f;

            remoteControl.AddWaypoint(new MyWaypointInfo("_approach", approach));
            remoteControl.SetAutoPilotEnabled(true);

            return false;
        }

        if (remoteControl.IsAutoPilotEnabled) {
            return false;
        }

        remoteControl.ClearWaypoints();

        return true;
    });

    actions.Add(() => {
        if (dockType == "connector" && !GetBatteryConnector()) {
            return false;
        }

        IMyShipMergeBlock mergeBlock = (IMyShipMergeBlock)GetBlock(mergeBlockId);
        IMyRemoteControl remoteControl = (IMyRemoteControl)GetBlock(remoteControlId);

        if (remoteControl.CurrentWaypoint.IsEmpty()) {
            remoteControl.ClearWaypoints();

            remoteControl.SetCollisionAvoidance(false);
            remoteControl.SetDockingMode(true);
            remoteControl.SpeedLimit = 5f;
            remoteControl.Direction = Base6Directions.Direction.Backward;
            mergeBlock.Enabled = true;

            remoteControl.AddWaypoint(new MyWaypointInfo("_rcPos", rcDockPosition));
            remoteControl.SetAutoPilotEnabled(true);

            return false;
        }

        if (remoteControl.IsAutoPilotEnabled) {
            return false;
        }

        if (dockType == "connector") {
            IMyShipConnector connector = (IMyShipConnector)GetBlock(batteryConnectorId);
            connector.Connect();
            if (connector.Status == MyShipConnectorStatus.Connected) {
                return true;
            }
        } else if (dockType == "merge" && mergeBlock.IsConnected) {
            // finish up
            remoteControl.ClearWaypoints();
            return true;
        }

        return false;
    });

    actions.Add(() => {
        IMyRemoteControl remoteControl = (IMyRemoteControl)GetBlock(remoteControlId);

        if (!GetBatteryConnector()) {
            return false;
        }
        IMyShipConnector connector = (IMyShipConnector)GetBlock(batteryConnectorId);
        Log(connector.ToString());
        if (connector != null) {
            Log(connector.CustomName);
        }

        if (dockType == "merge") {
            connector.Disconnect();
            if (connector.Status == MyShipConnectorStatus.Connected) {
                return false;
            }

            remoteControl.ClearWaypoints();
            return true;
        } else if (dockType == "connector") {
            connector.Connect();
            if (connector.Status == MyShipConnectorStatus.Connected) {
                remoteControl.ClearWaypoints();
                return true;
            }
        }

        return false;
    });

    if (dockType == "connector") {
        actions.Add(() => {
            IMyShipMergeBlock mergeBlock = (IMyShipMergeBlock)GetBlock(mergeBlockId);
            mergeBlock.Enabled = false;
            batteryConnectorId = -1;

            if (!mergeBlock.IsConnected) {
                IMyRemoteControl remoteControl = (IMyRemoteControl)GetBlock(remoteControlId);
                remoteControl.ClearWaypoints();
            }

            return !mergeBlock.IsConnected;
        });
    }


    actions.Add(() => {
        IMyRemoteControl remoteControl = (IMyRemoteControl)GetBlock(remoteControlId);

        if (remoteControl.CurrentWaypoint.IsEmpty()) {
            remoteControl.SetCollisionAvoidance(false);
            remoteControl.SetDockingMode(true);
            remoteControl.Direction = Base6Directions.Direction.Forward;
            remoteControl.FlightMode = FlightMode.OneWay;
            remoteControl.SpeedLimit = 20f;

            remoteControl.AddWaypoint(new MyWaypointInfo("_approach", approach));
            remoteControl.SetAutoPilotEnabled(true);

            return false;
        }

        if (remoteControl.IsAutoPilotEnabled) {
            return false;
        }

        remoteControl.ClearWaypoints();

        return true;
    });

    Runtime.UpdateFrequency |= UpdateFrequency.Update100;
}

public Vector3D GetOffsetFromRc(IMyTerminalBlock block) {
    IMyRemoteControl remoteControl = (IMyRemoteControl)GetBlock(remoteControlId);

    if (block != null && remoteControl != null) {
        return remoteControl.GetPosition() - block.GetPosition();
    } else {
        return Vector3D.Zero;
    }
}

public Program() {
    Me.CustomData = "";
    Me.GetSurface(0).WriteText("");
    SetupListeners();
    GetDroneBlocks();
}

public void Main(string argument, UpdateType updateType) {
    MyCommandLine cli = new MyCommandLine();
    cli.TryParse(argument);

    if ((updateType & UpdateType.IGC) != 0) {
        emitter.Process(argument);
    } else if (argument != null && argument != "") {
        string command = cli.Items[0];
        string channel = cli.Items.ElementAtOrDefault(1);
        string msg = cli.Items.ElementAtOrDefault(2);
        string target = cli.Items.ElementAtOrDefault(3);

        switch (command) {
            case "who":
                foreach (var kv in emitter.receievers) {
                    Log($"{kv.Key}, {kv.Value}");
                }
            break;
            case "broadcast":
                Log($"bradcasting '{msg}' on channel '{channel}'");
                emitter.Emit(channel, msg);
            break;
            case "send":
                Log($"unicasting '{msg}' on channel '{channel}' to {target}");
                emitter.Emit(channel, msg, emitter.Who(target));
            break;
        }
    }

    if ((updateType & UpdateType.Update100) == UpdateType.Update100) {
        Dock();
    }
}
