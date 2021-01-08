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
