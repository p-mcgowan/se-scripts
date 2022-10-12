const string customDataInit = @"[general]
energyProvider=false
connectorName=BattPack
parkingName=Parking
";

IGCEmitter emitter;
MyCommandLine cli = new MyCommandLine();
bool isEnergyProvider = false;
string batteryConnectorName = "BattPack";
string parkingMergeName = "Parking";

public Program() {
    Me.GetSurface(0).WriteText("online\n");

    if (Me.CustomData == "") {
        Me.CustomData = $"{customDataInit}id={Me.CubeGrid.CustomName}";
        Log($"Using default customData.");
    }
    config.Parse(this);
    isEnergyProvider = config.Enabled("general/energyProvider");
    batteryConnectorName = config.Get("general/connectorName", "BattPack");
    parkingMergeName = config.Get("general/parkingName", "Parking");

    SetupListeners();
    Runtime.UpdateFrequency |= UpdateFrequency.Update100;
}

public bool ReplaceBatteries() {
    blocks.Clear();
    GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(blocks, b =>
        b is IMyBatteryBlock && IsAlive(b) && b.CustomName.Contains(batteryConnectorName)
    );

    float batteryCurrent = 0;
    float batteryMax = 0;
    foreach (IMyBatteryBlock battery in blocks) {
        batteryCurrent += battery.CurrentStoredPower;
        batteryMax += battery.MaxStoredPower;
    }

    if (batteryMax == 0f) {
        return false;
    }

    Log($"Checking battery status: {(100 * batteryCurrent / batteryMax).ToString("#,0.00")}%");

    return batteryCurrent / batteryMax < 0.3f;
}

double lastCheck = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

public void Main(string argument, UpdateType updateType) {
    emitter.Process();

    if (argument != null && argument != "") {
        HandleCliArgs(argument);
    }

    ProcessAcks();

    if (isEnergyProvider) {
        return;
    }

    double timeNow = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
    if (timeNow - lastCheck > 5 * 60 * 1000) {
        lastCheck = timeNow;
        if (ReplaceBatteries()) {
            Log($"Requesting new battery");
            emitter.Emit("JOB", "battery");
        }
    }
}

public void SetupListeners() {
    emitter = new IGCEmitter(this, true);
    emitter.Hello(config.Get("general/id"));

    emitter.On("DOCKING_REQUEST", HandleDockingRequests, unicast: true);
    emitter.On("STATUS", HandleStatusRequests, unicast: true);
    emitter.On("ACK", BufferAck, unicast: true);
}

public void HandleCliArgs(string argument) {
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
        default:
            Echo("usage: todo");
            break;
    }
}

public string ToGps(Vector3D point, string name = "", string colour = "") {
    return $"GPS:{name}:{point.X}:{point.Y}:{point.Z}:{colour}:";
}
List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
List<IMyBlockGroup> groups = new List<IMyBlockGroup>();
List<MyIGCMessage> responses = new List<MyIGCMessage>();
bool newMessageReceieved = false;

public bool IsAlive(IMyTerminalBlock block) {
    return block != null && block.WorldMatrix.Translation != Vector3.Zero && block.IsWorking && block.IsFunctional;
}

public bool ConnectorIsAvailable(IMyShipConnector con, string name) {
    return IsAlive(con) && con.Status == MyShipConnectorStatus.Unconnected && con.CustomName.Contains(name);
}

public void HandleDockingRequests(MyIGCMessage msg) {
    string type = msg.Data.ToString();

    Log($"incoming DOCKING_REQUEST for {type}");
    IMyTerminalBlock block = FindDockBlock(type);
    if (block == null) {
        return;
    }

    Log($"responding to docking request: {block.CustomName}");
    emitter.Emit("DOCKING_REQUEST", DockingInfo(block), msg.Source);
}

public IMyTerminalBlock FindDockBlock(string type) {
    blocks.Clear();
    groups.Clear();
    if (type == "merge") {
        return FindMergeBlock(batteryConnectorName);
    } else if (type == "connector") {
        return FindConnector(batteryConnectorName);
    } else if (type == "parking") {
        return FindMergeBlock(parkingMergeName);
    }
    Log($"Unsupported docking type: '{type}'");

    return null;
}

public MyTuple<string, Vector3D, Vector3D> DockingInfo(IMyTerminalBlock block) {
    Vector3D blockPos = block.GetPosition();
    Vector3D forward = Vector3D.Zero;
    string type = "";

    if (block is IMyShipConnector) {
        type = "connector";
        forward = block.WorldMatrix.Forward;
    } else if (block is IMyShipMergeBlock) {
        type = "merge";
        forward = block.WorldMatrix.Right;
    } else /* if (block is IMyLandingGear) */ {
        Log($"Invalid docking target request: {block.BlockDefinition}");
    }

    return new MyTuple<string, Vector3D, Vector3D>(type, blockPos, forward);
}

public IMyShipConnector FindConnector(string name) {
    GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(blocks, b => b.IsSameConstructAs(Me));
    foreach (IMyShipConnector con in blocks) {
        if (ConnectorIsAvailable(con, name)) {
            return con;
        }
    }

    Log($"did not find applicable dock: groups {groups.Count}, blocks {blocks.Count}");

    return null;
}

public IMyShipMergeBlock FindMergeBlock(string name) {
    MyTuple<IMyShipMergeBlock, float> best = new MyTuple<IMyShipMergeBlock, float>(null, -1f);
    GridTerminalSystem.GetBlockGroups(groups, g => g.Name.Contains(name));

    Log($"finding {(isEnergyProvider ? "highest" : "lowest")} of {groups.Count()} '{name}' groups");

    foreach (IMyBlockGroup group in groups) {
        blocks.Clear();
        group.GetBlocks(blocks);

        IMyShipMergeBlock candidate = null;
        float battCharge = 0f;
        foreach (IMyTerminalBlock block in blocks) {
            IMyShipMergeBlock merge = block as IMyShipMergeBlock;
            if (IsAlive(merge) && !merge.IsConnected) {
                candidate = merge;
                continue;
            }

            IMyBatteryBlock battery = block as IMyBatteryBlock;
            if (IsAlive(battery)) {
                battCharge += battery.CurrentStoredPower;
                continue;
            }
        }

        if (
            (candidate != null && best.Item1 == null)
            || (isEnergyProvider && battCharge > best.Item2)
            || (!isEnergyProvider && battCharge < best.Item2)
        ) {
            best.Item1 = candidate;
            best.Item2 = battCharge;
        }
    }

    if (best.Item1 == null) {
        Log($"did not find applicable '{name}' dock");
    }

    return best.Item1;

}

public void HandleStatusRequests(MyIGCMessage msg) {
    Log($"[{emitter.Who(msg.Source)}]: {msg.Data}");
}

public void BufferAck(MyIGCMessage msg) {
    var res = msg.As<MyTuple<string, string, Vector3D>>();
    Log($"recd ack {res.Item1}, {emitter.Who(msg.Source)}");
    newMessageReceieved = true;
    responses.Add(msg);
    Runtime.UpdateFrequency |= UpdateFrequency.Update100;
}

public void ProcessAcks() {
    if (responses.Count == 0) {
        return;
    }

    if (newMessageReceieved) {
        newMessageReceieved = false;
        Log($"recd {responses.Count} responses");
        return;
    }

    long src = -1;
    double nearest = 0;
    string channel = "";
    Vector3D me = Me.GetPosition();

    foreach (var msg in responses) {
        MyTuple<string, string, Vector3D> data = msg.As<MyTuple<string, string, Vector3D>>();
        channel = data.Item1;
        string name = data.Item2;
        Vector3D pos = data.Item3;

        double dist = Vector3D.Distance(me, pos);

        Log($"{emitter.Who(msg.Source)} is {(int)dist}m away");
        if (nearest == 0 || dist < nearest) {
            dist = nearest;
            src = msg.Source;
        }
    }

    if (src != -1) {
        Log($"giving '{channel}' task to {emitter.Who(src)}");
        emitter.Emit("JOB", channel, src);
    }

    responses.Clear();
    Runtime.UpdateFrequency |= UpdateFrequency.Once;
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
