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
long requester = -1;

public void StatusRequest(MyIGCMessage msg) {
    Log($"responding to ping");
    emitter.Emit("STATUS", state, msg.Source);
}

public bool CanProcess(MyIGCMessage msg) {
    if (state == "Idle") {
        return true;
    }

    Log($"NACK {msg.Tag}");
    emitter.Emit("NACK", msg.Tag, msg.Source);

    return false;
}

public void Ack(MyIGCMessage msg) {
    if (!CanProcess(msg)) {
        return;
    }

    MyTuple<string, string, Vector3D> response = new MyTuple<string, string, Vector3D>(
        (string)msg.Data,
        config.Get("general/id") ?? Me.CubeGrid.Name,
        Me.GetPosition()
    );
    Log($"ACK {msg.Tag}");
    emitter.Emit("ACK", response, msg.Source);
}

public void HandleJob(MyIGCMessage msg) {
    if (!CanProcess(msg)) {
        return;
    }

    string task = msg.As<string>();
    Log($"Trying to do work: {msg.Tag}:{task}");

    switch (task) {
        case "battery":
            requester = msg.Source;
            ExecuteBatteryRequest(msg);
            return;
    }

    Log($"Not sure how to handle '{msg.Tag}:{task}'");
}

public void SetupListeners() {
    emitter = new IGCEmitter(this, true);
    emitter.Hello(config.Get("general/id"));

    emitter
        .On("STATUS", StatusRequest)
        .On("STATUS", StatusRequest, unicast: true)
        .On("JOB", Ack)
        .On("JOB", HandleJob, unicast: true);
}

public Program() {
    Me.GetSurface(0).WriteText("online\n");
    if (Me.CustomData == "") {
        Me.CustomData = $"[general]\nid={Me.CubeGrid.CustomName}\nsolar=Solar Farm";
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
    emitter.Process();

    if (argument != null && argument != "") {
        HandleCliArgs(argument);
    }

    GetCurrentTask();
}

public void HandleCliArgs(string argument) {
    IMyShipConnector connector;
    IMyShipMergeBlock mergeBlock;
    IMyRemoteControl remoteControl;

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
            // dock parking "Solar Farm"
            Log($"sending dock request '{channel}' to {msg}");
            AddTask("Awaiting docking instruction", Wait);
            emitter.Once("DOCKING_REQUEST", AnswerParkingSpace, unicast: true);
            emitter.Emit("DOCKING_REQUEST", channel, emitter.Who(msg));

            // emitter.Once("DOCKING_REQUEST", (MyIGCMessage igcm) => {
            //     remoteControl = (IMyRemoteControl)GetBlock(remoteControlId);
            //     remoteControl.ClearWaypoints();
            //     state = "Idle";
            //     ParseDockingCoords(igcm);
            //     Log("Setting target");
            //     AddTask("Go to approach", GoToLocation, target ?? "_approach");
            //     ProcessTasks();
            // }, unicast: true);
            // emitter.Emit("DOCKING_REQUEST", channel, emitter.Who(msg));
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
            SetDroneThrusters(true);
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

public string ToGps(Vector3D point, string name = "", string colour = "") {
    return $"GPS:{name}:{point.X}:{point.Y}:{point.Z}:{colour}:";
}
