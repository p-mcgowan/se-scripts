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
