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
public bool Wait() {
    return false;
}

public void ExecuteBatteryRequest(MyIGCMessage msg) {
    if (!CanProcess(msg)) {
        return;
    }

    state = "Processing battery request";
    SetDroneBatteryMode(ChargeMode.Auto);
    ReleaseMergeBlock();
    SetDroneThrusters(true);
    AddTask("Fetch new battery", FetchNewBattery);

    ProcessTasks();
}

public void QueueMergeSteps() {
    AddTask("Go to approach", GoToLocation, "fast fwd _approach");
    AddTask("Enable merge block", () => {
        IMyShipMergeBlock mergeBlock = (IMyShipMergeBlock)GetBlock(mergeBlockId);
        mergeBlock.Enabled = true;

        return true;
    });
    AddTask("Move to merge block", GoToLocation, "slower back _rcPos");

    AddTask("Connect to merge block", ConnectToMergeBlock);
    AddTask("Set batteries", () => {
        SetDroneBatteryMode(ChargeMode.Recharge);
        SetBatteryBlockMode(ChargeMode.Discharge);

        return true;
    });
    AddTask("Release connector", ReleaseConnector);

    AddTask("Go to approach", GoToLocation, "slow fwd _approach");
}

public void QueueConnectorSteps(ChargeMode chargeMode) {
    AddTask("Go to approach", GoToLocation, "fast fwd _approach");
    AddTask("Move to connector", GoToLocation, "slow back _rcPos");

    AddTask("Realign connector", GoToLocation, "slow fwd _approach");
    AddTask("Move to connector", GoToLocation, "slow back _rcPos");
    AddTask("Connect to connector", ConnectToConnector);

    AddTask("Set batteries", () => {
        SetDroneBatteryMode(ChargeMode.Auto);
        SetBatteryBlockMode(chargeMode);

        return true;
    });
    AddTask("Release merge block", ReleaseMergeBlock);

    AddTask("Leaving dock", GoToLocation, "slow fwd _approach");
}

public bool FetchNewBattery() {
    Log($"asking for dock [FetchNewBattery]");
    AddTask("Awaiting docking instruction", Wait);
    emitter.Once("DOCKING_REQUEST", AnswerFetchNewBattery, unicast: true);
    emitter.Emit("DOCKING_REQUEST", "merge", emitter.Who(config.Get("general/solar")));

    return true;
}

public void AnswerFetchNewBattery(MyIGCMessage msg) {
    ParseDockingCoords(msg);
    QueueMergeSteps();

    AddTask("Deposit new battery", DepositNewBattery);
    RemoveCurrentTask("Receieved docking instruction");
}

public bool DepositNewBattery() {
    Log($"asking for dock [DepositNewBattery]");
    AddTask("Awaiting docking instruction", Wait);
    emitter.Once("DOCKING_REQUEST", AnswerDepositNewBattery, unicast: true);
    emitter.Emit("DOCKING_REQUEST", "connector", requester);

    return true;
}

public void AnswerDepositNewBattery(MyIGCMessage msg) {
    ParseDockingCoords(msg);
    QueueConnectorSteps(ChargeMode.Discharge);

    AddTask("Fetch old battery", FetchOldBattery);
    RemoveCurrentTask("Receieved docking instruction");
}


public bool FetchOldBattery() {
    Log($"asking for dock [FetchOldBattery]");
    AddTask("Awaiting docking instruction", Wait);
    emitter.Once("DOCKING_REQUEST", AnswerFetchOldBattery, unicast: true);
    emitter.Emit("DOCKING_REQUEST", "merge", requester);

    return true;
}

public void AnswerFetchOldBattery(MyIGCMessage msg) {
    ParseDockingCoords(msg);
    QueueMergeSteps();

    AddTask("Deposit old battery", DepositOldBattery);
    RemoveCurrentTask("Receieved docking instruction");
}

public bool DepositOldBattery() {
    Log($"asking for dock [DepositOldBattery]");
    AddTask("Awaiting docking instruction", Wait);
    emitter.Once("DOCKING_REQUEST", AnswerDepositOldBattery, unicast: true);
    emitter.Emit("DOCKING_REQUEST", "connector", emitter.Who(config.Get("general/solar")));

    return true;
}

public void AnswerDepositOldBattery(MyIGCMessage msg) {
    ParseDockingCoords(msg);
    QueueConnectorSteps(ChargeMode.Recharge);
    AddTask("Request parking space", RequestParkingSpace);
    RemoveCurrentTask("Receieved docking instruction");
}

public bool RequestParkingSpace() {
    Log($"asking for dock [RequestParkingSpace]");
    AddTask("Awaiting docking instruction", Wait);
    emitter.Once("DOCKING_REQUEST", AnswerParkingSpace, unicast: true);
    emitter.Emit("DOCKING_REQUEST", "parking", emitter.Who(config.Get("general/solar")));

    return true;
}

public void AnswerParkingSpace(MyIGCMessage msg) {
    ParseDockingCoords(msg);

    AddTask("Go to approach", GoToLocation, "fast fwd _approach");
    AddTask("Enable merge block", () => {
        IMyShipMergeBlock mergeBlock = (IMyShipMergeBlock)GetBlock(mergeBlockId);
        mergeBlock.Enabled = true;

        return true;
    });
    AddTask("Move to merge block", GoToLocation, "slower back _rcPos");

    AddTask("Connect to merge block", ConnectToMergeBlock);
    AddTask("Recharging", () => {
        SetDroneBatteryMode(ChargeMode.Recharge);
        SetDroneThrusters(false);

        return true;
    });

    RemoveCurrentTask("Receieved docking instruction");
}
public IMyTerminalBlock GetBlock(long id) {
    if (id == -1) {
        return null;
    }
    return GridTerminalSystem.GetBlockWithId(id);
}

public void GetDroneBlocks() {
    blocks.Clear();
    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocks, b =>
        b.IsSameConstructAs(Me) &&
        b.CustomName.Contains("Drone") &&
        (b is IMyShipConnector || b is IMyShipMergeBlock || b is IMyRemoteControl || b is IMyBatteryBlock)
    );

    batteryIds.Clear();

    foreach (var block in blocks) {
        if (block is IMyShipMergeBlock){
            mergeBlockId = block.EntityId;
        } else if (block is IMyShipConnector){
            connectorId = block.EntityId;
        } else if (block is IMyRemoteControl) {
            remoteControlId = block.EntityId;
        } else if (block is IMyBatteryBlock) {
            batteryIds.Add(block.EntityId);
        }
    }
}

public void SetDroneThrusters(bool enabled) {
    blocks.Clear();
    GridTerminalSystem.GetBlocksOfType<IMyThrust>(blocks, b =>
        b.IsSameConstructAs(Me) &&
        b.CustomName.Contains("Drone") &&
        (b is IMyThrust)
    );
    foreach (IMyThrust block in blocks) {
        block.Enabled = enabled;
    }
}

public bool GetBatteryConnector() {
    if (batteryConnectorId == -1 || GetBlock(batteryConnectorId) == null || GetBlock(batteryConnectorId).WorldMatrix.Translation == Vector3.Zero) {
        blocks.Clear();
        GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(blocks, b => b.IsSameConstructAs(Me) && b.CustomName.Contains("BattPack"));

        if (blocks.Count == 1) {
            batteryConnectorId = blocks[0].EntityId;
        } else {
            Log($"Could not find battery connector ({blocks.Count})");
            return false;
        }
    }

    return true;
}

public Vector3D GetOffsetFromRc(IMyTerminalBlock block) {
    IMyRemoteControl remoteControl = (IMyRemoteControl)GetBlock(remoteControlId);

    if (block != null && remoteControl != null) {
        return remoteControl.GetPosition() - block.GetPosition();
    } else {
        return Vector3D.Zero;
    }
}

public bool SetDroneBatteryMode(ChargeMode mode) {
    foreach (long id in batteryIds) {
        IMyBatteryBlock b = (IMyBatteryBlock)GetBlock(id);
        if (b != null) {
            b.ChargeMode = mode;
        }
    }

    return true;
}

public bool SetBatteryBlockMode(ChargeMode mode) {
    bool didSetAtLeastOne = false;

    groups.Clear();
    GridTerminalSystem.GetBlockGroups(groups, g => g.Name.Contains("BattPack"));

    foreach (IMyBlockGroup group in groups) {
        blocks.Clear();
        group.GetBlocksOfType<IMyBatteryBlock>(blocks);

        foreach (IMyBatteryBlock battery in blocks) {
            if (battery.IsSameConstructAs(Me)) {
                battery.ChargeMode = mode;
                didSetAtLeastOne = true;
            }
        }
    }

    return didSetAtLeastOne;
}

public void SetConnectorStrength(bool shouldPull) {
    IMyShipConnector connector = (IMyShipConnector)GetBlock(batteryConnectorId);
    if (connector == null) {
        return;
    }
    connector.PullStrength = shouldPull ? 0.0001f : 0f;
}
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

    if (TravelHas(cfg, "slower")) {
        remoteControl.SetCollisionAvoidance(false);
        remoteControl.SetDockingMode(true);
        remoteControl.SpeedLimit = 2.5f;
    } else if (TravelHas(cfg, "slow")) {
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

public bool GoToLocation(string cfg) {
    TravelConfig(cfg);
    string[] parts = cfg.Split(' ');
    string waypoint = parts[parts.Length - 1];
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
public delegate bool ActionItem(string arg = null);
public delegate bool ArglessActionItem();
Queue<Task> tasks = new Queue<Task>();

public struct Task {
    public readonly string name;
    public readonly ActionItem action;
    public readonly ArglessActionItem arglessAction;
    public readonly string arg;

    public Task(string name, ActionItem action, string arg) {
        this.name = name;
        this.action = action;
        this.arg = arg;
        this.arglessAction = null;
    }

    public Task(string name, ArglessActionItem arglessAction) {
        this.name = name;
        this.arglessAction = arglessAction;
        this.action = null;
        this.arg = null;
    }

    public bool TryComplete() {
        return this.action != null ? this.action(this.arg) : this.arglessAction();
    }
}

public void AddTask(string name, ArglessActionItem action) {
    tasks.Enqueue(new Task(name, action));
}

public void AddTask(string name, ActionItem action, string arg) {
    tasks.Enqueue(new Task(name, action, arg));
}

public void ProcessTasks() {
    logs.Clear();
    GetCurrentTask();
}

public void GetCurrentTask() {
    if (tasks.Count <= 0) {
        state = "Idle";
        return;
    }

    Task task = tasks.Peek();
    if (state != task.name) {
        Log($"> {task.name} ({tasks.Count})");
    }
    state = task.name;

    if (!task.TryComplete()) {
        return;
    }

    Task done;
    tasks.TryDequeue(out done);

    IMyRemoteControl remoteControl = (IMyRemoteControl)GetBlock(remoteControlId);
    if (remoteControl != null) {
        remoteControl.ClearWaypoints();
    }

    Runtime.UpdateFrequency |= UpdateFrequency.Once;
}

public void SetIdle() {
    tasks.Clear();
    state = "Idle";
}

public bool AbortTask(string reason) {
    Log($"AbortTask: {reason}");
    SetIdle();

    return true;
}

public void RemoveCurrentTask(string nextState = null) {
    Task task = tasks.Dequeue();
    // Log($" - {task.name}");
    Runtime.UpdateFrequency |= UpdateFrequency.Once;
    if (nextState != null) {
        state = nextState;
    }
}
public class IGCEmitter {
    public Program p;
    public long id;
    public Dictionary<string, IMyBroadcastListener> listeners;
    public Dictionary<string, List<Action<MyIGCMessage>>> handlers;
    public IMyUnicastListener unicastListener;
    public Dictionary<string, List<Action<MyIGCMessage>>> unicastHandlers;
    public Dictionary<string, long> receievers;
    public bool verbose;
    public StringBuilder logs;

    public IGCEmitter(Program p, bool verbose = false, StringBuilder logs = null) {
        this.p = p;
        this.id = this.p.IGC.Me;
        this.handlers = new Dictionary<string, List<Action<MyIGCMessage>>>();
        this.listeners = new Dictionary<string, IMyBroadcastListener>();
        this.unicastHandlers = new Dictionary<string, List<Action<MyIGCMessage>>>();
        this.receievers = new Dictionary<string, long>();
        this.verbose = verbose;
        this.logs = logs ?? new StringBuilder("");
        this.unicastListener = this.p.IGC.UnicastListener;
    }

    public IGCEmitter Emit<TData>(string channel, TData data, long target = -1) {
        if (target != -1) {
            this.p.IGC.SendUnicastMessage(target, channel, data);
        } else {
            this.p.IGC.SendBroadcastMessage(channel, data);
        }

        return this;
    }

    public IGCEmitter On(string channel, Action<MyIGCMessage> handler, bool unicast = false) {
        if (unicast) {
            this.unicastListener.SetMessageCallback(channel);
            this.AddHandler(channel, handler, this.unicastHandlers);
        } else {
            this.AddListener(channel);
            this.AddHandler(channel, handler, this.handlers);
        }

        if (this.verbose) {
            this.p.Echo($"[{this.p.Me.CubeGrid.CustomName}] listening on {channel}");
        }

        return this;
    }

    public IGCEmitter Once(string channel, Action<MyIGCMessage> handler, bool unicast = false) {
        Action<MyIGCMessage> wrapper = null;

        wrapper = (MyIGCMessage msg) => {
            handler(msg);
            this.Off(channel, wrapper, unicast);
        };
        this.On(channel, wrapper, unicast);

        return this;
    }

    public bool Off(string channel, Action<MyIGCMessage> handler = null, bool unicast = false) {
        List<Action<MyIGCMessage>> actions = null;
        var msgHandles = unicast ? this.unicastHandlers : this.handlers;

        if (!msgHandles.TryGetValue(channel, out actions) || actions.Count == 0) {
            return false;
        }

        if (handler == null) {
            actions.Clear();
        } else {
            actions.RemoveAll(h => h == handler);
        }

        if (actions.Count == 0) {
            if (unicast) {
                this.unicastListener.DisableMessageCallback();
            } else {
                this.listeners[channel].DisableMessageCallback();
            }
        }

        return true;
    }

    public bool Process(string callbackString = "") {
        bool hadMessage = false;
        List<Action<MyIGCMessage>> callbacks;
        IMyBroadcastListener listener;
        MyIGCMessage msg;

        foreach (var kv in this.listeners) {
            string channel = kv.Key;
            listener = kv.Value;

            if (listener.HasPendingMessage && this.handlers.TryGetValue(channel, out callbacks)) {
                while (listener.HasPendingMessage) {
                    msg = listener.AcceptMessage();
                    this.logs.Append($"[{this.Who(msg.Source)}] {msg.Tag}: {msg.Data}\n");

                    for (int cbIndex = callbacks.Count - 1; cbIndex >= 0; --cbIndex) {
                        callbacks[cbIndex](msg);
                    }
                    hadMessage = true;
                }
            }
        }

        while (this.unicastListener.HasPendingMessage) {
            msg = this.unicastListener.AcceptMessage();
            this.logs.Append($"[{this.Who(msg.Source)}] {msg.Tag}: {msg.Data}\n");

            if (this.unicastHandlers.TryGetValue(msg.Tag, out callbacks)) {
                for (int cbIndex = callbacks.Count - 1; cbIndex >= 0; --cbIndex) {
                    callbacks[cbIndex](msg);
                }
                hadMessage = true;
            }
        }

        return hadMessage;
    }

    public bool HasMessages() {
        if (this.unicastListener.HasPendingMessage) {
            return true;
        }

        foreach (var kv in this.listeners) {
            if (kv.Value.HasPendingMessage) {
                return true;
            }
        }

        return false;
    }

    public void Hello(string response = null) {
        response = response ?? this.p.Me.CubeGrid.CustomName;

        this
            .On("HELLO", (MyIGCMessage msg) => {
                string who = msg.Data.ToString();
                this.receievers[who] = msg.Source;
                this.Emit("HELLO", response, msg.Source);
            })
            .On("HELLO", (MyIGCMessage msg) => {
                string who = msg.Data.ToString();
                this.receievers[who] = msg.Source;

                if (this.verbose) {
                    this.p.Echo($"hello from {who}");
                }
            }, unicast: true);


        this.Emit("HELLO", response);
    }

    public void AddListener(string channel) {
        IMyBroadcastListener listener = this.p.IGC.RegisterBroadcastListener(channel);
        listener.SetMessageCallback(channel);
        this.listeners[channel] = listener;
    }

    public void AddHandler(string channel, Action<MyIGCMessage> handler, Dictionary<string, List<Action<MyIGCMessage>>> handlers) {
        List<Action<MyIGCMessage>> list;
        if (!handlers.TryGetValue(channel, out list)) {
            handlers[channel] = new List<Action<MyIGCMessage>>();
        }
        handlers[channel].Add(handler);
    }

    public long Who(string name) {
        return this.receievers[name];
    }

    public string Who(long id) {
        return this.receievers.FirstOrDefault(x => x.Value == id).Key;
    }
}
Config config = new Config();

public static TValue DictGet<TKey, TValue>(Dictionary<TKey, TValue> dict, TKey key, TValue defaultValue = default(TValue)) {
    TValue value;
    return dict.TryGetValue(key, out value) ? value : defaultValue;
}

public class Config {
    public MyIni ini;
    public string customData;
    public Dictionary<string, string> settings;
    public List<MyIniKey> keys;
    public List<string> sections;

    public Config() {
        this.ini = new MyIni();
        this.settings = new Dictionary<string, string>();
        this.keys = new List<MyIniKey>();
        this.sections = new List<string>();
    }

    public void Clear() {
        this.ini.Clear();
        this.settings.Clear();
        this.keys.Clear();
        this.customData = null;
    }

    public bool Parse(Program p) {
        bool parsed = this.Parse(p.Me.CustomData);
        if (!parsed) {
            p.Echo($"failed to parse customData");
        }

        return parsed;
    }

    public bool Parse(string iniTemplate) {
        this.Clear();

        MyIniParseResult result;
        if (!this.ini.TryParse(iniTemplate, out result)) {
            return false;
        }
        this.customData = iniTemplate;

        this.ini.GetSections(this.sections);

        string keyValue;
        this.ini.GetKeys(this.keys);
        foreach (MyIniKey key in this.keys) {
            if (this.ini.Get(key.Section, key.Name).TryGetString(out keyValue)) {
                this.Set(key.ToString(), keyValue);
            }
        }

        return true;
    }

    public void Set(string name, string keyValue) {
        this.settings[name] = keyValue;
    }

    public string Get(string name, string alt = null) {
        return DictGet<string, string>(this.settings, name, null) ?? alt;
    }

    public bool Enabled(string name) {
        return DictGet<string, string>(this.settings, name, "false") == "true";
    }
}
StringBuilder logs = new StringBuilder(512);
StringBuilder size = new StringBuilder("Q");

public int GetLineCount() {
    IMyTextSurface surface = Me.GetSurface(0);
    Vector2 charSizeInPx = surface.MeasureStringInPixels(size, surface.Font, surface.FontSize);
    float padding = (surface.TextPadding / 100) * surface.SurfaceSize.Y;
    float height = surface.SurfaceSize.Y - (2 * padding);

    return (int)(Math.Round(height / charSizeInPx.Y));
}

public void Debug(string message, bool newline = true, IMyTextSurface output = null) {
    string text = message + (newline ? "\n" : "");
    logs.Append(text);
    string res = logs.ToString();
    Echo(text);
    if (output != null) {
        output.WriteText(res);
    } else {
        Me.GetSurface(0).WriteText(res);
    }
}

public void Log(string message, bool newline = true, IMyTextSurface output = null) {
    string text = message + (newline ? "\n" : "");
    logs.Append(text);

    string res = logs.ToString();
    int lineLength = GetLineCount();
    int count = 0;
    for (int i = logs.Length - 1; i >= 0; --i) {
        if (res[i] == '\n') {
            count++;
        }
        if (count >= lineLength) {
            res = res.Substring(i, logs.Length - i);
            break;
        }
    }

    Echo(text);
    if (output != null) {
        output.WriteText(res);
    } else {
        Me.GetSurface(0).WriteText(res);
    }
}
