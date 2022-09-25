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
        Log($"{block.CustomName}");
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
