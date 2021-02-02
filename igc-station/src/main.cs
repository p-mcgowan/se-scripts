IGCEmitter emitter;
List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
List<IMyBlockGroup> groups = new List<IMyBlockGroup>();
List<MyIGCMessage> responses = new List<MyIGCMessage>();
MyCommandLine cli = new MyCommandLine();
bool newMessageReceieved = false;

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

public bool IsAlive(IMyTerminalBlock block) {
    return block != null && block.WorldMatrix.Translation != Vector3.Zero && block.IsWorking && block.IsFunctional;
}

public IMyTerminalBlock FindDockBlock(MyIGCMessage msg) {
    string type = msg.Data.ToString();

    blocks.Clear();
    groups.Clear();
    if (type == "merge") {
        MyTuple<IMyShipMergeBlock, float> best = new MyTuple<IMyShipMergeBlock, float>(null, -1f);
        GridTerminalSystem.GetBlockGroups(groups, g => g.Name.Contains("BattPack"));

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

            if (Me.CubeGrid.CustomName == "Solar Farm") {
                // return highest batt
                if (candidate != null && best.Item1 == null || battCharge > best.Item2) {
                    best.Item1 = candidate;
                    best.Item2 = battCharge;
                }
            } else {
                // return lowest batt
                if (candidate != null && best.Item1 == null || battCharge < best.Item2) {
                    best.Item1 = candidate;
                    best.Item2 = battCharge;
                }
            }
        }
        if (best.Item1 != null) {
            Log($"found best result: {best.Item1.CustomName}, {best.Item2}");
            return best.Item1;
        }
    } else if (type == "connector") {
        GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(blocks, b => b.IsSameConstructAs(Me));
        foreach (IMyShipConnector con in blocks) {
            if (IsAlive(con) && con.Status == MyShipConnectorStatus.Unconnected && con.CustomName.Contains("BattPack")) {
                return con;
            }
        }
    }
    Log($"did not find applicable dock: groups {groups.Count}, blocks {blocks.Count}");

    return null;
}

public void HandleDockingRequests(MyIGCMessage msg) {
    Log("incoming DOCKING_REQUEST");
    IMyTerminalBlock block = FindDockBlock(msg);
    if (block != null) {
        Log("responding to docking request");
        emitter.Emit("DOCKING_REQUEST", DockingInfo(block), msg.Source);
    } else {
        Log("no free blocks");
    }
}

public void HandleStatusRequests(MyIGCMessage msg) {
    Log($"[{emitter.Who(msg.Source)}]: {msg.Data}");
}

public void SetupListeners() {
    emitter = new IGCEmitter(this, true);
    emitter.Hello();

    emitter.On("DOCKING_REQUEST", HandleDockingRequests, unicast: true);
    emitter.On("STATUS", HandleStatusRequests, unicast: true);
    emitter.On("ACK", BufferAck, unicast: true);
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
    string tag = "";
    Vector3D me = Me.GetPosition();

    foreach (var msg in responses) {
        MyTuple<string, string, Vector3D> data = msg.As<MyTuple<string, string, Vector3D>>();
        tag = data.Item1;
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
        Log($"giving {tag} task to {emitter.Who(src)}");
        emitter.Emit(tag, "CONTINUE", src);
    }

    responses.Clear();
    Runtime.UpdateFrequency |= UpdateFrequency.Once;
}

public Program() {
    Me.GetSurface(0).WriteText("online\n");
    if (Me.CustomData == "") {
        Me.CustomData = $"[general]\nid={Me.CubeGrid.CustomName}";
    }
    config.Parse(this);
    SetupListeners();
    Runtime.UpdateFrequency |= UpdateFrequency.Update100;
}

public void Main(string argument, UpdateType updateType) {
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
        }
    }

    ProcessAcks();
}

public string ToGps(Vector3D point, string name = "", string colour = "") {
    return $"GPS:{name}:{point.X}:{point.Y}:{point.Z}:{colour}:";
}
