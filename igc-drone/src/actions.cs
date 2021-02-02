public void StatusRequest(MyIGCMessage msg) {
    Log($"responding to ping");
    emitter.Emit("STATUS", state, msg.Source);
}

public bool CanProcess(MyIGCMessage msg) {
    if (state != "Idle") {
        Log($"NACK {msg.Tag}");
        emitter.Emit("NACK", msg.Tag, msg.Source);
        return false;
    }
    return true;
}

public void Ack(MyIGCMessage msg) {
    if (!CanProcess(msg)) {
        return;
    }
    MyTuple<string, string, Vector3D> response = new MyTuple<string, string, Vector3D>(
        msg.Tag,
        config.Get("general/id") ?? Me.CubeGrid.Name,
        Me.GetPosition()
    );
    Log($"ACK {msg.Tag}");
    emitter.Emit("ACK", response, msg.Source);
}

public void ExecuteBatteryRequest(MyIGCMessage msg) {
    if (!CanProcess(msg)) {
        return;
    }
    state = "Processing battery request";
    AddTask("Fetch old battery", FetchOldBattery);

    ProcessTasks();
}

public bool Wait() {
    return false;
}

public bool FetchOldBattery() {
    Log($"asking for dock [FetchOldBattery]");
    AddTask("Awaiting docking instruction", Wait);
    emitter.Once("DOCKING_REQUEST", AnswerFetchOldBattery, unicast: true);
    emitter.Emit("DOCKING_REQUEST", "merge", emitter.Who("Pertram Station"));

    return true;
}

public void AnswerFetchOldBattery(MyIGCMessage msg) {
    ParseDockingCoords(msg);
    AddTask("Config autopilot", TravelConfig, "fast fwd");
    AddTask("Go to approach", GoToLocation, "_approach");
    AddTask("Config autopilot", TravelConfig, "slow fwd");
    AddTask("Go to approach", GoToLocation, "_approach");

    AddTask("Config autopilot", TravelConfig, "slow back");
    AddTask("Enable merge block", () => {
        IMyShipMergeBlock mergeBlock = (IMyShipMergeBlock)GetBlock(mergeBlockId);
        mergeBlock.Enabled = true;

        return true;
    });
    AddTask("Move to merge block", GoToLocation, "_rcPos");
    AddTask("Connect to merge block", ConnectToMergeBlock);

    AddTask("Set batteries", () => {
        if (SetBatteryBlockMode(ChargeMode.Discharge)) {
            SetDroneBatteryMode(ChargeMode.Recharge);
        }

        return true;
    });
    AddTask("Release connector", ReleaseConnector);

    AddTask("Config autopilot", TravelConfig, "slow fwd");
    AddTask("Go to approach", GoToLocation, "_approach");

    AddTask("Deposit old battery", DepositOldBattery);
    RemoveCurrentTask("Receieved docking instruction");
}

public bool DepositOldBattery() {
    Log($"asking for dock [DepositOldBattery]");
    AddTask("Awaiting docking instruction", Wait);
    emitter.Once("DOCKING_REQUEST", AnswerDepositOldBattery, unicast: true);
    emitter.Emit("DOCKING_REQUEST", "connector", emitter.Who("Solar Farm"));

    return true;
}

public void AnswerDepositOldBattery(MyIGCMessage msg) {
    ParseDockingCoords(msg);
    AddTask("Config autopilot", TravelConfig, "fast fwd");
    AddTask("Go to approach", GoToLocation, "_approach");
    AddTask("Config autopilot", TravelConfig, "slow fwd");
    AddTask("Go to approach", GoToLocation, "_approach");

    AddTask("Config autopilot", TravelConfig, "slow back");
    AddTask("Move to connector", GoToLocation, "_rcPos");
    AddTask("Connect to connector", ConnectToConnector);
    AddTask("Set batteries", () => {
        SetDroneBatteryMode(ChargeMode.Auto);
        SetBatteryBlockMode(ChargeMode.Recharge);

        return true;
    });
    AddTask("Release merge block", ReleaseMergeBlock);

    AddTask("Config autopilot", TravelConfig, "slow fwd");
    AddTask("Go to approach", GoToLocation, "_approach");

    AddTask("Fetch new battery", FetchNewBattery);
    RemoveCurrentTask("Receieved docking instruction");
}

public bool FetchNewBattery() {
    Log($"asking for dock [FetchNewBattery]");
    AddTask("Awaiting docking instruction", Wait);
    emitter.Once("DOCKING_REQUEST", AnswerFetchNewBattery, unicast: true);
    emitter.Emit("DOCKING_REQUEST", "merge", emitter.Who("Solar Farm"));

    return true;
}

public void AnswerFetchNewBattery(MyIGCMessage msg) {
    ParseDockingCoords(msg);
    AddTask("Config autopilot", TravelConfig, "fast fwd");
    AddTask("Go to approach", GoToLocation, "_approach");
    AddTask("Config autopilot", TravelConfig, "slow fwd");
    AddTask("Go to approach", GoToLocation, "_approach");

    AddTask("Config autopilot", TravelConfig, "slow back");
    AddTask("Enable merge block", () => {
        IMyShipMergeBlock mergeBlock = (IMyShipMergeBlock)GetBlock(mergeBlockId);
        mergeBlock.Enabled = true;

        return true;
    });
    AddTask("Move to merge block", GoToLocation, "_rcPos");
    AddTask("Connect to merge block", ConnectToMergeBlock);
    AddTask("Set batteries", () => {
        if (SetBatteryBlockMode(ChargeMode.Discharge)) {
            SetDroneBatteryMode(ChargeMode.Recharge);
        }

        return true;
    });
    AddTask("Release connector", ReleaseConnector);

    AddTask("Config autopilot", TravelConfig, "slow fwd");
    AddTask("Go to approach", GoToLocation, "_approach");
    AddTask("Deposit new battery", DepositNewBattery);
    RemoveCurrentTask("Receieved docking instruction");
}

public bool DepositNewBattery() {
    Log($"asking for dock [DepositNewBattery]");
    AddTask("Awaiting docking instruction", Wait);
    emitter.Once("DOCKING_REQUEST", AnswerDepositNewBattery, unicast: true);
    emitter.Emit("DOCKING_REQUEST", "connector", emitter.Who("Pertram Station"));

    return true;
}

public void AnswerDepositNewBattery(MyIGCMessage msg) {
    ParseDockingCoords(msg);
    AddTask("Config autopilot", TravelConfig, "fast fwd");
    AddTask("Go to approach", GoToLocation, "_approach");
    AddTask("Config autopilot", TravelConfig, "slow fwd");
    AddTask("Go to approach", GoToLocation, "_approach");

    AddTask("Config autopilot", TravelConfig, "slow back");
    AddTask("Move to connector", GoToLocation, "_rcPos");
    AddTask("Connect to connector", ConnectToConnector);

    AddTask("Set batteries", () => {
        if (SetBatteryBlockMode(ChargeMode.Discharge)) {
            SetDroneBatteryMode(ChargeMode.Auto);
        }

        return true;
    });
    AddTask("Release merge block", ReleaseMergeBlock);
    AddTask("Config autopilot", TravelConfig, "slow fwd");
    AddTask("Leaving dock", GoToLocation, "_approach");
    // AddTask("Go to happy place", GoToDroneDock);
    RemoveCurrentTask("Receieved docking instruction");
}
