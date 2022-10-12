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
    AddTask("Move to merge block", GoToLocation, "slow back _rcPos");

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
    AddTask("Connect to connector", () => {
        if (!ConnectToConnector()) {
            AddTask("Realign connector", GoToLocation, "slow fwd _approach");
            AddTask("Move to connector", GoToLocation, "slow back _rcPos");
            AddTask("Connect to connector", ConnectToConnector);
        }

        return true;
    });

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
    AddTask("Move to merge block", GoToLocation, "slow back _rcPos");

    AddTask("Connect to merge block", ConnectToMergeBlock);
    AddTask("Recharging", () => {
        SetDroneBatteryMode(ChargeMode.Recharge);
        SetDroneThrusters(false);

        return true;
    });

    RemoveCurrentTask("Receieved docking instruction");
}
