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
