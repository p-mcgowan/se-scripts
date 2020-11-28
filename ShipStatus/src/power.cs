/*
 * POWER
 */
PowerDetails powerDetails;
public class PowerDetails {
    public Program program;
    public List<IMyPowerProducer> powerProducerBlocks;
    public List<IMyJumpDrive> jumpDriveBlocks;
    public List<MyInventoryItem> items;

    public int jumpDrives;
    public float jumpMax;
    public float jumpCurrent;

    public int batteries;
    public float batteryMax;
    public float batteryCurrent;

    public int reactors;
    public float reactorOutputMW;
    public MyFixedPoint reactorUranium;

    public int solars;
    public float solarOutputMW;
    public float solarOutputMax;

    public PowerDetails(Program _program) {
        program = _program;
        powerProducerBlocks = new List<IMyPowerProducer>();
        jumpDriveBlocks = new List<IMyJumpDrive>();
        items = new List<MyInventoryItem>();
        jumpDrives = 0;
        jumpMax = 0f;
        jumpCurrent = 0f;
        batteries = 0;
        batteryMax = 0f;
        batteryCurrent = 0f;
        reactors = 0;
        reactorOutputMW = 0f;
        reactorUranium = 0;
        solars = 0;
        solarOutputMW = 0f;
        solarOutputMax = 0f;
        GetBlocks();
    }

    public void Clear() {
        jumpDrives = 0;
        jumpMax = 0f;
        jumpCurrent = 0f;
        batteries = 0;
        batteryMax = 0f;
        batteryCurrent = 0f;
        reactors = 0;
        reactorOutputMW = 0f;
        reactorUranium = 0;
        solars = 0;
        solarOutputMW = 0f;
        solarOutputMax = 0f;
    }

    public void GetBlocks() {
        powerProducerBlocks.Clear();
        program.GridTerminalSystem.GetBlocksOfType<IMyPowerProducer>(powerProducerBlocks, b => b.IsSameConstructAs(program.Me));
        jumpDriveBlocks.Clear();
        program.GridTerminalSystem.GetBlocksOfType<IMyJumpDrive>(jumpDriveBlocks, b => b.IsSameConstructAs(program.Me));
    }

    public float GetPercent(float current, float max) {
        if (max == 0) {
            return 0f;
        }
        return current / max;
    }

    public void Refresh() {
        Clear();

        foreach (IMyPowerProducer powerBlock in powerProducerBlocks) {
            if (powerBlock is IMyBatteryBlock) {
                batteries += 1;
                batteryCurrent += ((IMyBatteryBlock)powerBlock).CurrentStoredPower;
                batteryMax += ((IMyBatteryBlock)powerBlock).MaxStoredPower;
            } else if (powerBlock is IMyReactor) {
                reactors += 1;
                reactorOutputMW += ((IMyReactor)powerBlock).CurrentOutput;

                items.Clear();
                var inv = ((IMyReactor)powerBlock).GetInventory(0);
                inv.GetItems(items);
                for (var i = 0; i < items.Count; i++) {
                    reactorUranium += items[i].Amount;
                }
            } else if (powerBlock is IMySolarPanel) {
                solars += 1;
                solarOutputMW += ((IMySolarPanel)powerBlock).CurrentOutput;
                solarOutputMax += ((IMySolarPanel)powerBlock).MaxOutput;
            }
        }

        foreach (IMyJumpDrive jumpDrive in jumpDriveBlocks) {
            jumpDrives += 1;
            jumpCurrent += jumpDrive.CurrentStoredPower;
            jumpMax += jumpDrive.MaxStoredPower;
        }
    }

    public override string ToString() {
        return $"{jumpDrives} Jump drive{Util.Plural(jumpDrives, "", "s")}:\n" +
            $"{jumpCurrent} / {jumpMax}\n" +
            $"{batteries} Batter{Util.Plural(batteries, "y", "ies")}\n" +
            $"{batteryCurrent} / {batteryMax}\n" +
            $"{reactors} Reactor{Util.Plural(reactors, "", "s")}\n" +
            $"{reactorOutputMW} MW, {Util.FormatNumber(reactorUranium)} Fuel";
    }
}
/* POWER */
