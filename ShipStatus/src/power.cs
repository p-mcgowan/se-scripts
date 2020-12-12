/*
 * POWER
 */
PowerDetails powerDetails;

public class PowerDetails {
    public Program program;
    public Template template;
    public List<IMyPowerProducer> powerProducerBlocks;
    public List<IMyJumpDrive> jumpDriveBlocks;
    public List<MyInventoryItem> items;
    public List<float> ioFloats;
    public List<Color> ioColours;

    public int jumpDrives;
    public float jumpMax;
    public float jumpCurrent;

    public int batteries;
    public float batteryMax;
    public float batteryCurrent;
    public float batteryInput;
    public float batteryOutput;
    public float batteryOutputDisabled;
    public float batteryOutputMax;
    public float batteryInputMax;

    public int reactors;
    public float reactorOutputMW;
    public float reactorOutputMax;
    public float reactorOutputDisabled;
    public MyFixedPoint reactorUranium;

    public int solars;
    public float solarOutputMW;
    public float solarOutputDisabled;
    public float solarOutputMax;

    public int turbines;
    public float turbineOutputMW;
    public float turbineOutputDisabled;
    public float turbineOutputMax;

    public int hEngines;
    public float hEngineOutputMW;
    public float hEngineOutputDisabled;
    public float hEngineOutputMax;

    public float battChargeMax = 12f;

    public Color reactorColour = Color.Blue;
    public Color hEnginesColour = DrawingSurface.stringToColour["dimred"];
    public Color batteriesColour = DrawingSurface.stringToColour["dimgreen"];
    public Color turbinesColour = DrawingSurface.stringToColour["dimyellow"];
    public Color solarsColour = Color.Darken(Color.Cyan, 0.6);

    public PowerDetails(Program program, Template template = null) {
        this.program = program;
        this.template = template;
        this.powerProducerBlocks = new List<IMyPowerProducer>();
        this.jumpDriveBlocks = new List<IMyJumpDrive>();
        this.items = new List<MyInventoryItem>();
        this.ioFloats = new List<float>();
        this.ioColours = new List<Color>() {
            this.reactorColour,
            new Color(this.reactorColour, 0.01f),
            this.hEnginesColour,
            new Color(this.hEnginesColour, 0.01f),
            this.batteriesColour,
            new Color(this.batteriesColour, 0.01f),
            this.turbinesColour,
            new Color(this.turbinesColour, 0.01f),
            this.solarsColour,
            new Color(this.solarsColour, 0.01f)
        };

        this.Reset();
    }

    public void Reset() {
        this.Clear();

        if (this.program.config.Enabled("power")) {
            this.GetBlocks();
            this.RegisterTemplateVars();
        }
    }

    public void RegisterTemplateVars() {
        if (this.template == null) {
            return;
        }
        this.template.Register("power.jumpDrives", () => this.jumpDrives.ToString());
        this.template.Register("power.jumpBar", (DrawingSurface ds, string text, Dictionary<string, string> options) => {
            if (this.jumpDrives == 0) {
                return;
            }
            float pct = this.GetPercent(this.jumpCurrent, this.jumpMax);
            options["text"] = text ?? Util.PctString(pct);
            options["pct"] = pct.ToString();
            ds.Bar(options).Newline();
        });
        this.template.Register("power.batteries", () => this.batteries.ToString());
        this.template.Register("power.batteryBar", this.BatteryBar);
        this.template.Register("power.solars", () => this.solars.ToString());
        this.template.Register("power.turbines", () => this.turbines.ToString());
        this.template.Register("power.engines", () => this.hEngines.ToString());
        this.template.Register("power.reactors", () => this.reactors.ToString());
        this.template.Register("power.reactorMw", (DrawingSurface ds, string text, Dictionary<string, string> options) => {
            ds.Text($"{this.reactorOutputMW}{text}");
        });
        this.template.Register("power.reactorUr", (DrawingSurface ds, string text, Dictionary<string, string> options) => {
            ds.Text($"{Util.FormatNumber(this.reactorUranium)}{text}");
        });
        this.template.Register("power.io", () => {
            float io = this.CurrentInput() - this.CurrentOutput();
            float max = this.MaxOutput();

            return String.Format("{0:0.00} / {1:0.00} MWh ({2})", io, max, Util.PctString(Math.Abs(io) / max));
        });
        this.template.Register("power.ioBar", this.IoBar);
    }

    public void Clear() {
        this.jumpDrives = 0;
        this.jumpMax = 0f;
        this.jumpCurrent = 0f;
        this.batteries = 0;
        this.batteryMax = 0f;
        this.batteryCurrent = 0f;
        this.batteryInput = 0f;
        this.batteryOutput = 0f;
        this.batteryOutputDisabled = 0f;
        this.batteryOutputMax = 0f;
        this.batteryInputMax = 0f;
        this.reactors = 0;
        this.reactorOutputMW = 0f;
        this.reactorOutputDisabled = 0f;
        this.reactorOutputMax = 0f;
        this.reactorUranium = 0;
        this.solars = 0;
        this.solarOutputMW = 0f;
        this.solarOutputDisabled = 0f;
        this.solarOutputMax = 0f;
        this.turbines = 0;
        this.turbineOutputMW = 0f;
        this.turbineOutputDisabled = 0f;
        this.turbineOutputMax = 0f;
        this.hEngines = 0;
        this.hEngineOutputMW = 0f;
        this.hEngineOutputDisabled = 0f;
        this.hEngineOutputMax = 0f;
    }

    public void GetBlocks() {
        this.powerProducerBlocks.Clear();
        this.program.GridTerminalSystem.GetBlocksOfType<IMyPowerProducer>(this.powerProducerBlocks, b =>
            this.program.config.Enabled("getAllGrids") || b.IsSameConstructAs(this.program.Me));
        this.jumpDriveBlocks.Clear();
        this.program.GridTerminalSystem.GetBlocksOfType<IMyJumpDrive>(this.jumpDriveBlocks, b =>
            this.program.config.Enabled("getAllGrids") || b.IsSameConstructAs(this.program.Me));
    }

    public float GetPercent(float current, float max) {
        if (max == 0) {
            return 0f;
        }
        return current / max;
    }

    public float MaxOutput() {
        return this.hEngineOutputMax + this.reactorOutputMax + this.solarOutputMax + this.turbineOutputMax + (battChargeMax * this.batteries);
    }

    public float MaxInput() {
        return (battChargeMax * this.batteries);
    }

    public float CurrentInput() {
        return this.batteryInput;
    }

    public float CurrentOutput() {
        return this.reactorOutputMW + this.solarOutputMW + this.turbineOutputMW + this.hEngineOutputMW + this.batteryOutput;
    }

    public float DisabledMaxOutput() {
        return this.batteryOutputDisabled + this.reactorOutputDisabled + this.solarOutputDisabled + this.turbineOutputDisabled + this.hEngineOutputDisabled;
    }

    public void Refresh() {
        this.Clear();

        foreach (IMyPowerProducer powerBlock in this.powerProducerBlocks) {
            if (powerBlock == null) {
                continue;
            }
            string typeString = powerBlock.BlockDefinition.TypeIdString;
            var battery = powerBlock as IMyBatteryBlock;

            if (battery != null) {
                this.batteries++;
                this.batteryCurrent += battery.CurrentStoredPower;
                this.batteryMax += battery.MaxStoredPower;
                this.batteryInput += battery.CurrentInput;
                this.batteryOutput += battery.CurrentOutput;
                this.batteryOutputMax += battery.MaxOutput;
                this.batteryOutputMax += battery.MaxInput;
                if (!battery.Enabled || battery.ChargeMode == ChargeMode.Recharge) {
                    this.batteryOutputDisabled += battChargeMax;
                }
            } else if (powerBlock is IMyReactor) {
                this.reactors++;
                this.reactorOutputMW += powerBlock.CurrentOutput;
                this.reactorOutputMax += powerBlock.MaxOutput;

                if (!powerBlock.Enabled) {
                    this.reactorOutputDisabled += powerBlock.MaxOutput;
                }

                this.items.Clear();
                var inv = powerBlock.GetInventory(0);
                inv.GetItems(this.items);
                for (var i = 0; i < items.Count; i++) {
                    this.reactorUranium += items[i].Amount;
                }
            } else if (powerBlock is IMySolarPanel) {
                this.solars++;
                this.solarOutputMW += powerBlock.CurrentOutput;
                this.solarOutputMax += powerBlock.MaxOutput;
                if (!powerBlock.Enabled) {
                    this.solarOutputDisabled += powerBlock.MaxOutput;
                }
            } else if (typeString == "MyObjectBuilder_HydrogenEngine") {
                this.hEngines++;
                this.hEngineOutputMW += powerBlock.CurrentOutput;
                this.hEngineOutputMax += powerBlock.MaxOutput;
                if (!powerBlock.Enabled) {
                    this.hEngineOutputDisabled += powerBlock.MaxOutput;
                }
            } else if (typeString == "MyObjectBuilder_WindTurbine") {
                this.turbines++;
                this.turbineOutputMW += powerBlock.CurrentOutput;
                this.turbineOutputMax += powerBlock.MaxOutput;
                if (!powerBlock.Enabled) {
                    this.turbineOutputDisabled += powerBlock.MaxOutput;
                }
            }
        }

        foreach (IMyJumpDrive jumpDrive in jumpDriveBlocks) {
            if (jumpDrive == null) {
                continue;
            }
            this.jumpDrives += 1;
            this.jumpCurrent += jumpDrive.CurrentStoredPower;
            this.jumpMax += jumpDrive.MaxStoredPower;
        }
    }

    public void BatteryBar(DrawingSurface ds, string text, Dictionary<string, string> options) {
        if (this.batteries == 0) {
            return;
        }
        float io = this.batteryInput - this.batteryOutput;

        float net = this.batteryCurrent;
        float remainingMins = io == 0f ? 0 : (this.batteryMax - this.batteryCurrent) * 60 / Math.Abs(io);
        string pct = Util.PctString(this.batteryCurrent / this.batteryMax);
        if (this.batteryCurrent / this.batteryMax >= 0.9999f) {
            pct = "100 %";
            io = 0;
            remainingMins = 0;
        }
        string msg = $"{pct}";
        if (io < 0) {
            net = (this.batteryCurrent - this.batteryMax);
            remainingMins = this.batteryCurrent * 60 / Math.Abs(io);
            msg = $"{pct}";
        }
        double minsLeft = Math.Round(remainingMins);
        text = text ?? $"{msg} ({(minsLeft <= 60 ? $"{minsLeft} min" : String.Format("{0:0.00} hours", minsLeft / 60))})";

        float high = this.batteryMax;
        float low = high;

        options["net"] = net.ToString();
        options["low"] = low.ToString();
        options["high"] = high.ToString();
        options["text"] = text;

        ds.MidBar(options).Newline();
    }

    public void IoBar(DrawingSurface ds, string text, Dictionary<string, string> options) {
        float max = this.CurrentOutput() + this.DisabledMaxOutput();

        this.ioFloats.Clear();
        this.ioFloats.Add(this.reactorOutputMW / max);
        this.ioFloats.Add(this.reactorOutputDisabled / max);
        this.ioFloats.Add(this.hEngineOutputMW / max);
        this.ioFloats.Add(this.hEngineOutputDisabled / max);
        this.ioFloats.Add(this.batteryOutput / max);
        this.ioFloats.Add(this.batteryOutputDisabled / max);
        this.ioFloats.Add(this.turbineOutputMW / max);
        this.ioFloats.Add(this.turbineOutputDisabled / max);
        this.ioFloats.Add(this.solarOutputMW / max);
        this.ioFloats.Add(this.solarOutputDisabled / max);

        ds.MultiBar(this.ioFloats, this.ioColours, text: text, textAlignment: TextAlignment.LEFT).Newline();
    }
}
/* POWER */
