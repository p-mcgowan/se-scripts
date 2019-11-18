/*
 * Config
 */

const string RECHARGE = "recharge";
const string RESUME = "resume";
const string IGNORE  = "[PWR]";

Dictionary<IMyFunctionalBlock, bool> restore = new Dictionary<IMyFunctionalBlock, bool>();

void RegularMode() {
    if (restore.Any()) {
        foreach (KeyValuePair<IMyFunctionalBlock, bool> reset in restore) {
            var wasEnabled = reset.Value;
            var block = reset.Key;
            block.Enabled = wasEnabled;
        }
    } else {
        var blocks = new List<IMyFunctionalBlock>();
        GridTerminalSystem.GetBlocksOfType<IMyFunctionalBlock>(blocks,
            block => block.CubeGrid == Me.CubeGrid && block != Me && !ShouldRemainOn(block)
            && !(
                block is IMyRefinery ||
                block is IMyAssembler ||
                block is IMyAirVent ||
                block.BlockDefinition.SubtypeName == "LargeHydrogenEngine"
            )
        );

        foreach (var block in blocks) {
            block.Enabled = true;
        }
    }

    var batteries = new List<IMyBatteryBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(batteries);
    foreach (var batt in batteries) {
        batt.ChargeMode = ChargeMode.Auto;
    }
    restore.Clear();
}

bool ShouldRemainOn(IMyFunctionalBlock b) {
    return b is IMyDoor ||
        b is IMyBatteryBlock ||
        b is IMySolarPanel ||
        b is IMyReactor ||
        b is IMyMedicalRoom ||
        b is IMyUserControllableGun ||
        b is IMyGasTank ||
        b is IMyGasGenerator ||
        b is IMyShipConnector ||
        b is IMyLandingGear ||
        b.BlockDefinition.SubtypeName == "LargeBlockOxygenFarm";
}

void RechargeMode() {
    if (restore.Any()) {
        return;
    }

    var batteries = new List<IMyBatteryBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(batteries);
    foreach (var batt in batteries) {
        batt.ChargeMode = ChargeMode.Recharge;
    }

    var blocks = new List<IMyFunctionalBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyFunctionalBlock>(blocks, block =>
        block.CustomName.IndexOf(IGNORE, StringComparison.CurrentCultureIgnoreCase) == -1 &&
        block.CubeGrid == Me.CubeGrid &&
        block != Me &&
        !ShouldRemainOn(block));
    foreach (var block in blocks) {
        restore.Add(block, block.Enabled);
        block.Enabled = false;
        if (block is IMyGasGenerator) {
            Echo(block.CustomName);
        }
    }
}

public void Main(string argument, UpdateType updateSource) {
    if (argument == Me.CustomData) {
        Echo("Should already by in " + argument + " mode.");
        return;
    }
    if (argument == RECHARGE) {
        RechargeMode();
    } else if (argument == RESUME) {
        RegularMode();
    } else {
        Echo("Valid arguments are: '" + RECHARGE + "' or '" + RESUME + "'");
        return;
    }
    Me.CustomData = argument;
}