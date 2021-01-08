IGCEmitter emitter;
List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();

public MyTuple<string, Vector3D, Vector3D> DockingInfo(IMyTerminalBlock block) {
    Vector3D blockPos = block.GetPosition();
    Vector3D forward = Vector3D.Zero;
    string type = "";

    if (block is IMyShipConnector) {
        type = "connector";
        forward = block.WorldMatrix.Forward;
    } else {
        type = "merge";
        forward = block.WorldMatrix.Right;
    }

    MyTuple<string, Vector3D, Vector3D> res = new MyTuple<string, Vector3D, Vector3D>(type, blockPos, forward);
    debug?.WriteText($"{ToGps(blockPos, "block")}\n{ToGps(forward, "forward")}\n");
    // Me.GetSurface(0).WriteText($"{ToGps(blockPos, "block")}\n{ToGps(forward, "forward")}\n");
    return res;
}

public bool IsAlive(IMyTerminalBlock block) {
    return block != null && block.WorldMatrix.Translation != Vector3.Zero && block.IsWorking && block.IsFunctional;
}

public IMyTerminalBlock FindDockBlock(MyIGCMessage msg) {
    blocks.Clear();
    GridTerminalSystem.GetBlocksOfType<IMyFunctionalBlock>(blocks, b =>
        (b.IsSameConstructAs(Me) && b is IMyShipConnector) || (b is IMyShipMergeBlock && b.CustomName.Contains("BattPack"))
    );

    string type = msg.Data.ToString();

    foreach (IMyTerminalBlock block in blocks) {
        if (type == "connector" || type == "") {
            IMyShipConnector con = block as IMyShipConnector;
            if (IsAlive(con) && con.Status == MyShipConnectorStatus.Unconnected && con.CustomName.Contains("BattPack")) {
                return con;
            }
        }

        if (type == "merge" || type == "") {
            IMyShipMergeBlock merge = block as IMyShipMergeBlock;
            if (IsAlive(merge) && !merge.IsConnected) {
                return merge;
            }
        }
    }

    return null;
}

public void SetupListeners() {
    emitter = new IGCEmitter(this);
    emitter.Hello();

    emitter.OnUnicast("DOCKING_REQUEST", (MyIGCMessage msg) => {
        Log("incoming DOCKING_REQUEST");
        IMyTerminalBlock block = FindDockBlock(msg);
        if (block != null) {
            Log("responding to docking request");
            emitter.Emit("DOCKING_REQUEST", DockingInfo(block), msg.Source);
        } else {
            Log("no free blocks");
        }
    });
    emitter.OnUnicast("STATUS", (MyIGCMessage msg) => {
        Log($"[{emitter.Who(msg.Source)}]: {msg.Data}");
    });
}

public Program() {
    debug = (IMyTextPanel)GridTerminalSystem.GetBlockWithName("DEBUG");
    debug?.WriteText("loaded\n");
    Me.CustomData = "";
    Me.GetSurface(0).WriteText("");
    SetupListeners();
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
}
StringBuilder logs = new StringBuilder("");
IMyTextPanel debug;

public void Log(string message, bool newline = true) {
    string text = message + (newline ? "\n" : "");
    logs.Append(text);
    string res = logs.ToString();
    Echo(res);
    Me.GetSurface(0).WriteText(text, true);
    Me.CustomData = res;
}

public string ToGps(Vector3D point, string name = "", string colour = "") {
    return $"GPS:{name}:{point.X.ToString()}:{point.Y.ToString()}:{point.Z.ToString()}:{colour}:";
}
public class IGCEmitter {
    public Program p;
    public long id;
    public Dictionary<string, IMyBroadcastListener> listeners;
    public Dictionary<string, List<Action<MyIGCMessage>>> handlers;
    public IMyUnicastListener unicastListener;
    public Dictionary<string, List<Action<MyIGCMessage>>> unicastHandlers;
    public Dictionary<string, long> receievers;

    public IGCEmitter(Program p) {
        this.p = p;
        this.id = this.p.IGC.Me;
        this.handlers = new Dictionary<string, List<Action<MyIGCMessage>>>();
        this.listeners = new Dictionary<string, IMyBroadcastListener>();
        this.unicastHandlers = new Dictionary<string, List<Action<MyIGCMessage>>>();
        this.receievers = new Dictionary<string, long>();
    }

    public IGCEmitter Emit<TData>(string channel, TData data, long target = -1) {
        if (target != -1) {
            this.p.IGC.SendUnicastMessage(target, channel, data);
        } else {
            this.p.IGC.SendBroadcastMessage(channel, data);
        }

        return this;
    }

    public IGCEmitter OnUnicast(string channel, Action<MyIGCMessage> handler) {
        this.unicastListener = this.p.IGC.UnicastListener;
        this.unicastListener.SetMessageCallback(channel);
        this.AddHandler(channel, handler, this.unicastHandlers);

        return this;
    }

    public IGCEmitter On(string channel, Action<MyIGCMessage> handler) {
        this.AddListener(channel);
        this.AddHandler(channel, handler, this.handlers);

        this.p.Echo($"[{this.id}] listening on {channel}");

        return this;
    }

    public void Off(string channel) {
        this.listeners.Remove(channel);
        this.handlers.Remove(channel);
    }

    public void Process(string callbackString = "") {
        List<Action<MyIGCMessage>> callbacks;

        foreach (var kv in this.listeners) {
            string channel = kv.Key;
            IMyBroadcastListener listener = kv.Value;

            if (listener.HasPendingMessage && this.handlers.TryGetValue(channel, out callbacks)) {
                while (listener.HasPendingMessage) {
                    MyIGCMessage msg = listener.AcceptMessage();
                    foreach (Action<MyIGCMessage> handle in callbacks) {
                        handle(msg);
                    }
                }
            }
        }

        if (this.unicastListener != null) {
            if (this.unicastListener.HasPendingMessage) {
                while (this.unicastListener.HasPendingMessage) {
                    MyIGCMessage msg = this.unicastListener.AcceptMessage();
                    if (this.unicastHandlers.TryGetValue(msg.Tag, out callbacks)) {
                        foreach (Action<MyIGCMessage> handle in callbacks) {
                            handle(msg);
                        }
                    }
                }
            }
        }
    }

    public void Log(string message) {
        this.p.Me.GetSurface(0).WriteText(message + "\n", true);
        this.p.Echo(message);
    }

    public void Hello(string response = null) {
        response = response ?? this.p.Me.CubeGrid.CustomName;

        this
            .On("HELLO", (MyIGCMessage msg) => {
                string who = msg.Data.ToString();
                this.receievers[who] = msg.Source;
                this.Emit("HELLO", response, msg.Source);
                Log($"[{msg.Tag}] <= {who}");
            })
            .OnUnicast("HELLO", (MyIGCMessage msg) => {
                string who = msg.Data.ToString();
                this.receievers[who] = msg.Source;
                Log($"[{msg.Tag}] <= {who}");
            });

        this.Emit("HELLO", response);
    }

    public void AddListener(string channel) {
        IMyBroadcastListener listener = this.p.IGC.RegisterBroadcastListener(channel);
        listener.SetMessageCallback(channel);
        this.listeners[channel] = listener;
    }

    public void AddHandler(string channel, Action<MyIGCMessage> handler,  Dictionary<string, List<Action<MyIGCMessage>>> handlers) {
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
