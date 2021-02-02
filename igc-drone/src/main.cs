IGCEmitter emitter;
List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
List<IMyBlockGroup> groups = new List<IMyBlockGroup>();
long mergeBlockId = -1;
long connectorId = -1;
long remoteControlId = -1;
long batteryConnectorId = -1;
List<long> batteryIds = new List<long>();
MyCommandLine cli = new MyCommandLine();
string state = "Idle";

public void SetupListeners() {
    emitter = new IGCEmitter(this, true);
    emitter.Hello(config.Get("general/id"));

    emitter
        .On("STATUS", StatusRequest)
        .On("STATUS", StatusRequest, unicast: true)
        .On("BATTERY", Ack)
        .On("BATTERY", ExecuteBatteryRequest, unicast: true);
}

public Program() {
    Me.GetSurface(0).WriteText("online\n");
    if (Me.CustomData == "") {
        Me.CustomData = $"[general]\nid={Me.CubeGrid.CustomName}";
    }
    config.Parse(this);
    SetupListeners();
    GetDroneBlocks();

    IMyRemoteControl remoteControl = (IMyRemoteControl)GetBlock(remoteControlId);
    if (remoteControl != null) {
        remoteControl.ClearWaypoints();
    }

    Runtime.UpdateFrequency |= UpdateFrequency.Update100;
}

public void Main(string argument, UpdateType updateType) {
    IMyShipConnector connector;
    IMyShipMergeBlock mergeBlock;
    IMyRemoteControl remoteControl;

    emitter.Process();

    if (argument != null && argument != "") {
        cli.TryParse(argument);
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
            case "state":
                if (channel == null || channel == "") {
                    Log(state);
                } else {
                    state = channel;
                }
            break;
            case "start":
                ProcessTasks();
            break;
            case "dock":
                Log($"sending dock request '{channel}' to {msg}");
                emitter.Once("DOCKING_REQUEST", (MyIGCMessage igcm) => {
                    remoteControl = (IMyRemoteControl)GetBlock(remoteControlId);
                    remoteControl.ClearWaypoints();
                    state = "Idle";
                    ParseDockingCoords(igcm);
                    Log("Setting target");
                    AddTask("Go to approach", GoToLocation, target ?? "_approach");
                    ProcessTasks();
                }, unicast: true);
                emitter.Emit("DOCKING_REQUEST", channel, emitter.Who(msg));
            break;
            case "task":
                switch (channel) {
                    case "conn":
                        AddTask("Connect to connector", ConnectToConnector);
                    break;
                    case "-conn":
                        AddTask("Release connector", ReleaseConnector);
                    break;
                    case "merge":
                        AddTask("Connect to merge block", ConnectToMergeBlock);
                    break;
                    case "-merge":
                        AddTask("Release merge block", ReleaseMergeBlock);
                    break;
                    // case "":
                    //     AddTask("Go to happy place", GoToDroneDock);
                    // break;
                }
            break;
            case "wp":
                Log(ToGps(waypoints["_approach"].Coords, "_approach"));
                Log(ToGps(waypoints["_rcPos"].Coords, "_rcPos"));
            break;
            case "reset":
                mergeBlock = (IMyShipMergeBlock)GetBlock(mergeBlockId);
                remoteControl = (IMyRemoteControl)GetBlock(remoteControlId);
                remoteControl.ClearWaypoints();
                SetDroneBatteryMode(ChargeMode.Auto);
                if (GetBatteryConnector()) {
                    connector = (IMyShipConnector)GetBlock(batteryConnectorId);
                    connector.Connect();
                }
                batteryConnectorId = -1;
                mergeBlock.Enabled = false;
                AbortTask("resetting");
            break;
        }
    }

    GetCurrentTask();
}

public string ToGps(Vector3D point, string name = "", string colour = "") {
    return $"GPS:{name}:{point.X}:{point.Y}:{point.Z}:{colour}:";
}
