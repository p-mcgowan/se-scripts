/*
 * POWER
 */
PowerDetails powerDetails;

public class PowerDetails {
    public Program program;
    public Template template;
    public List<IMyTerminalBlock> powerProducerBlocks;
    public List<IMyTerminalBlock> jumpDriveBlocks;
    public List<IMyTerminalBlock> consumers;
    public List<MyInventoryItem> items;
    public List<float> ioFloats;
    public List<Color> ioColours;
    public Dictionary<string, Color> ioLegendNames;
    public Dictionary<string, float> consumerDict;

    public float batteryCurrent;
    public float batteryInput;
    public float batteryInputMax;
    public float batteryMax;
    public float batteryOutputDisabled;
    public float batteryOutputMax;
    public float batteryOutputMW;
    public float batteryPotential;
    public float hEngineOutputDisabled;
    public float hEngineOutputMax;
    public float hEngineOutputMW;
    public float hEnginePotential;
    public float jumpCurrent;
    public float jumpMax;
    public float reactorOutputDisabled;
    public float reactorOutputMax;
    public float reactorOutputMW;
    public float reactorPotential;
    public float solarOutputDisabled;
    public float solarOutputMax;
    public float solarOutputMW;
    public float solarPotential;
    public float turbineOutputDisabled;
    public float turbineOutputMax;
    public float turbineOutputMW;
    public float turbinePotential;
    public int batteries;
    public int batteriesDisabled;
    public int hEngines;
    public int hEnginesDisabled;
    public int jumpDrives;
    public int reactors;
    public int reactorsDisabled;
    public int solars;
    public int solarsDisabled;
    public int turbines;
    public int turbinesDisabled;
    public MyFixedPoint reactorUranium;

    public MyDefinitionId electricity = new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties), "Electricity");
    public MyDefinitionId oxygen = new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties), "Oxygen");
    public MyDefinitionId hydrogen = new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties), "Hydrogen");
    public float battChargeMax = 12f;
    public Color reactorColour = Color.Lighten(Color.Blue, 0.05);
    public Color hEnginesColour = DrawingSurface.stringToColour["dimred"];
    public Color batteriesColour = DrawingSurface.stringToColour["dimgreen"];
    public Color turbinesColour = Color.Darken(DrawingSurface.stringToColour["dimyellow"], 0.1);
    public Color solarsColour = Color.Darken(Color.Cyan, 0.8);
    public char[] splitNewline = new[] { '\n' };
    public List<float> widths = new List<float>() { 0, 0, 0, 0 };

    public PowerDetails(Program program, Template template = null) {
        this.program = program;
        this.template = template;
        this.items = new List<MyInventoryItem>();
        this.ioFloats = new List<float>();
        this.ioColours = new List<Color>() {
            this.reactorColour,
            ColorExtensions.Alpha(this.reactorColour, 0.98f),
            this.hEnginesColour,
            ColorExtensions.Alpha(this.hEnginesColour, 0.98f),
            this.batteriesColour,
            ColorExtensions.Alpha(this.batteriesColour, 0.98f),
            this.turbinesColour,
            ColorExtensions.Alpha(this.turbinesColour, 0.98f),
            this.solarsColour,
            ColorExtensions.Alpha(this.solarsColour, 0.98f)
        };
        this.powerProducerBlocks = new List<IMyTerminalBlock>();
        this.jumpDriveBlocks = new List<IMyTerminalBlock>();
        this.consumers = new List<IMyTerminalBlock>();
        this.ioLegendNames = new Dictionary<string, Color>();
        this.consumerDict = new Dictionary<string, float>();

        this.Reset();
    }

    public void Reset() {
        this.Clear();
        if (this.program.config.Enabled("power")) {
            this.RegisterTemplateVars();
        }
    }

    public void RegisterTemplateVars() {
        if (this.template == null) {
            return;
        }
        this.template.Register("power.batteries", () => this.batteries.ToString());
        this.template.Register("power.batteryBar", this.BatteryBar);
        this.template.Register("power.batteryCurrent", () => String.Format("{0:0.##}", this.batteryCurrent));
        this.template.Register("power.batteryInput", () => String.Format("{0:0.##}", this.batteryInput));
        this.template.Register("power.batteryInputMax", () => String.Format("{0:0.##}", this.batteryInputMax));
        this.template.Register("power.batteryMax", () => String.Format("{0:0.##}", this.batteryMax));
        this.template.Register("power.batteryOutput", () => String.Format("{0:0.##}", this.batteryOutputMW));
        this.template.Register("power.batteryOutputMax", () => String.Format("{0:0.##}", this.batteryOutputMax));
        this.template.Register("power.consumers", this.PowerConsumers);
        this.template.Register("power.engineOutputMax", () => String.Format("{0:0.##}", this.hEngineOutputMax));
        this.template.Register("power.engineOutputMW", () => String.Format("{0:0.##}", this.hEngineOutputMW));
        this.template.Register("power.engines", () => this.hEngines.ToString());
        this.template.Register("power.input", () => String.Format("{0:0.##}", this.CurrentInput()));
        this.template.Register("power.ioBar", this.IoBar);
        this.template.Register("power.ioLegend", this.IoLegend);
        this.template.Register("power.ioString", this.IoString);
        this.template.Register("power.jumpBar", this.JumpBar);
        this.template.Register("power.jumpCurrent", () => String.Format("{0:0.##}", this.jumpCurrent));
        this.template.Register("power.jumpDrives", () => this.jumpDrives.ToString());
        this.template.Register("power.jumpMax", () => String.Format("{0:0.##}", this.jumpMax));
        this.template.Register("power.maxOutput", () => String.Format("{0:0.##}", this.MaxOutput()));
        this.template.Register("power.output", () => String.Format("{0:0.##}", this.CurrentOutput()));
        this.template.Register("power.reactorOutputMax", () => String.Format("{0:0.##}", this.reactorOutputMax));
        this.template.Register("power.reactorOutputMW", () => String.Format("{0:0.##}", this.reactorOutputMW));
        this.template.Register("power.reactors", () => this.reactors.ToString());
        this.template.Register("power.reactorString", this.ReactorString);
        this.template.Register("power.reactorUr", () => Util.FormatNumber(this.reactorUranium));
        this.template.Register("power.solarOutputMax", () => String.Format("{0:0.##}", this.solarOutputMax));
        this.template.Register("power.solarOutputMW", () => String.Format("{0:0.##}", this.solarOutputMW));
        this.template.Register("power.solars", () => this.solars.ToString());
        this.template.Register("power.turbineOutputMax", () => String.Format("{0:0.##}", this.turbineOutputMax));
        this.template.Register("power.turbineOutputMW", () => String.Format("{0:0.##}", this.turbineOutputMW));
        this.template.Register("power.turbines", () => this.turbines.ToString());
    }

    public void ClearTotals() {
        this.batteries = 0;
        this.batteriesDisabled = 0;
        this.batteryCurrent = 0f;
        this.batteryInput = 0f;
        this.batteryInputMax = 0f;
        this.batteryMax = 0f;
        this.batteryOutputDisabled = 0f;
        this.batteryOutputMax = 0f;
        this.batteryOutputMW = 0f;
        this.batteryPotential = 0f;
        this.hEngineOutputDisabled = 0f;
        this.hEngineOutputMax = 0f;
        this.hEngineOutputMW = 0f;
        this.hEnginePotential = 0f;
        this.hEngines = 0;
        this.hEnginesDisabled = 0;
        this.jumpCurrent = 0f;
        this.jumpDrives = 0;
        this.jumpMax = 0f;
        this.reactorOutputDisabled = 0f;
        this.reactorOutputMax = 0f;
        this.reactorOutputMW = 0f;
        this.reactorPotential = 0f;
        this.reactors = 0;
        this.reactorsDisabled = 0;
        this.reactorUranium = 0;
        this.solarOutputDisabled = 0f;
        this.solarOutputMax = 0f;
        this.solarOutputMW = 0f;
        this.solarPotential = 0f;
        this.solars = 0;
        this.solarsDisabled = 0;
        this.turbineOutputDisabled = 0f;
        this.turbineOutputMax = 0f;
        this.turbineOutputMW = 0f;
        this.turbinePotential = 0f;
        this.turbines = 0;
        this.turbinesDisabled = 0;
    }

    public void Clear() {
        this.ClearTotals();
        this.powerProducerBlocks.Clear();
        this.jumpDriveBlocks.Clear();
        this.consumers.Clear();
    }

    public void GetBlock(IMyTerminalBlock block) {
        this.consumers.Add(block);
        if (block is IMyPowerProducer) {
            this.powerProducerBlocks.Add(block);
        } else if (block is IMyJumpDrive) {
            this.jumpDriveBlocks.Add(block);
        }
    }

    public void GotBLocks() {}

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
        return this.reactorOutputMW + this.solarOutputMW + this.turbineOutputMW + this.hEngineOutputMW + this.batteryOutputMW;
    }

    public float DisabledMaxOutput() {
        return this.batteryOutputDisabled + this.reactorOutputDisabled + this.solarOutputDisabled + this.turbineOutputDisabled + this.hEngineOutputDisabled;
    }

    public void Refresh() {
        this.ClearTotals();

        foreach (IMyJumpDrive jumpDrive in this.jumpDriveBlocks) {
            if (!Util.BlockValid(jumpDrive)) {
                continue;
            }
            this.jumpDrives += 1;
            this.jumpCurrent += jumpDrive.CurrentStoredPower;
            this.jumpMax += jumpDrive.MaxStoredPower;
        }

        foreach (IMyPowerProducer powerBlock in this.powerProducerBlocks) {
            if (!Util.BlockValid(powerBlock)) {
                continue;
            }
            string typeString = powerBlock.BlockDefinition.TypeIdString;
            IMyBatteryBlock battery = powerBlock as IMyBatteryBlock;

            if (battery != null) {
                this.batteries++;
                this.batteryCurrent += battery.CurrentStoredPower;
                this.batteryMax += battery.MaxStoredPower;
                this.batteryInput += battery.CurrentInput;
                this.batteryOutputMW += battery.CurrentOutput;
                this.batteryOutputMax += battery.MaxOutput;
                this.batteryOutputMax += battery.MaxInput;
                if (!battery.Enabled || battery.ChargeMode == ChargeMode.Recharge) {
                    this.batteriesDisabled++;
                    this.batteryOutputDisabled += battChargeMax;
                }
            } else if (powerBlock is IMyReactor) {
                this.reactors++;
                this.reactorOutputMW += powerBlock.CurrentOutput;
                this.reactorOutputMax += powerBlock.MaxOutput;

                if (!powerBlock.Enabled) {
                    this.reactorsDisabled++;
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
                    this.solarsDisabled++;
                    this.solarOutputDisabled += powerBlock.MaxOutput;
                }
            } else if (typeString == "MyObjectBuilder_HydrogenEngine") {
                this.hEngines++;
                this.hEngineOutputMW += powerBlock.CurrentOutput;
                this.hEngineOutputMax += powerBlock.MaxOutput;
                if (!powerBlock.Enabled) {
                    this.hEnginesDisabled++;
                    this.hEngineOutputDisabled += powerBlock.MaxOutput;
                }
            } else if (typeString == "MyObjectBuilder_WindTurbine") {
                this.turbines++;
                this.turbineOutputMW += powerBlock.CurrentOutput;
                this.turbineOutputMax += powerBlock.MaxOutput;
                if (!powerBlock.Enabled) {
                    this.turbinesDisabled++;
                    this.turbineOutputDisabled += powerBlock.MaxOutput;
                }
            }
        }

        this.consumerDict.Clear();
        MyResourceSinkComponent resourceSink;

        foreach (IMyTerminalBlock block in this.consumers) {
            if (!block.Components.TryGet<MyResourceSinkComponent>(out resourceSink)) {
                continue;
            }
            float powerConsumption = resourceSink.CurrentInputByType(this.electricity);

            string blockName = block.DefinitionDisplayNameText.ToString();
            if (!this.consumerDict.ContainsKey(blockName)) {
                this.consumerDict.Add(blockName, powerConsumption);
            } else {
                this.consumerDict[blockName] = this.consumerDict[blockName] + powerConsumption;
            }
        }

        this.consumerDict = this.consumerDict.OrderBy(x => -x.Value).ToDictionary(x => x.Key, x => x.Value);

        this.reactorPotential = (this.reactors - this.reactorsDisabled) == 0 ? 0f : (this.reactorOutputMW / (float)(this.reactors - this.reactorsDisabled)) * (float)this.reactorsDisabled;
        this.hEnginePotential = (this.hEngines - this.hEnginesDisabled) == 0 ? 0f : (this.hEngineOutputMW / (float)(this.hEngines - this.hEnginesDisabled)) * (float)this.hEnginesDisabled;
        this.batteryPotential = (this.batteries - this.batteriesDisabled) == 0 ? 0f : (this.batteryOutputMW / (float)(this.batteries - this.batteriesDisabled)) * (float)this.batteriesDisabled;
        this.turbinePotential = (this.turbines - this.turbinesDisabled) == 0 ? 0f : (this.turbineOutputMW / (float)(this.turbines - this.turbinesDisabled)) * (float)this.turbinesDisabled;
        this.solarPotential = (this.solars - this.solarsDisabled) == 0 ? 0f : (this.solarOutputMW / (float)(this.solars - this.solarsDisabled)) * (float)this.solarsDisabled;
    }

    public void BatteryBar(DrawingSurface ds, string text, DrawingSurface.Options options) {
        if (this.batteries == 0) {
            return;
        }
        float io = this.batteryInput - this.batteryOutputMW;

        options.net = this.batteryCurrent;
        float remainingMins = io == 0f ? 0 : (this.batteryMax - this.batteryCurrent) * 60 / Math.Abs(io);
        string pct = Util.PctString(this.batteryCurrent / this.batteryMax);
        if (this.batteryCurrent / this.batteryMax >= 0.9999f) {
            pct = "100 %";
            io = 0;
            remainingMins = 0;
        }
        string msg = $"{pct}";
        if (io < 0) {
            options.net = (this.batteryCurrent - this.batteryMax);
            remainingMins = this.batteryCurrent * 60 / Math.Abs(io);
            msg = $"{pct}";
        }
        double minsLeft = Math.Round(remainingMins);
        options.text = text ?? $"{msg} ({(minsLeft <= 60 ? $"{minsLeft} min" : String.Format("{0:0.00} hours", minsLeft / 60))})";

        options.high = this.batteryMax;
        options.low = options.high;

        ds.MidBar(options);
    }

    public void IoBar(DrawingSurface ds, string text, DrawingSurface.Options options) {
        float max = this.CurrentOutput() + this.reactorPotential + this.hEnginePotential + this.batteryPotential + this.turbinePotential + this.solarPotential;

        this.ioFloats.Clear();
        this.ioFloats.Add(this.reactorOutputMW / max);
        this.ioFloats.Add(this.reactorPotential / max);
        this.ioFloats.Add(this.hEngineOutputMW / max);
        this.ioFloats.Add(this.hEnginePotential / max);
        this.ioFloats.Add(this.batteryOutputMW / max);
        this.ioFloats.Add(this.batteryPotential / max);
        this.ioFloats.Add(this.turbineOutputMW / max);
        this.ioFloats.Add(this.turbinePotential / max);
        this.ioFloats.Add(this.solarOutputMW / max);
        this.ioFloats.Add(this.solarPotential / max);

        options.values = this.ioFloats;
        options.colours = this.ioColours;
        options.text = text ?? options.text;
        options.align = options.align ?? TextAlignment.LEFT;

        ds.MultiBar(options);
    }

    public void IoLegend(DrawingSurface ds, string text, DrawingSurface.Options options) {
        this.ioLegendNames.Clear();
        if (this.reactors > 0) {
            this.ioLegendNames["Reactor"] = this.reactorColour;
        }
        if (this.hEngines > 0) {
            this.ioLegendNames["H2 Engine"] = this.hEnginesColour;
        }
        if (this.batteries > 0) {
            this.ioLegendNames["Battery"] = this.batteriesColour;
        }
        if (this.turbines > 0) {
            this.ioLegendNames["Wind"] = this.turbinesColour;
        }
        if (this.solars > 0) {
            this.ioLegendNames["Solar"] = this.solarsColour;
        }
        ds.sb.Clear();
        ds.sb.Append(string.Join(" / ", this.ioLegendNames.Keys));
        Vector2 size = ds.surface.MeasureStringInPixels(ds.sb, ds.surface.Font, ds.surface.FontSize);
        ds.SetCursor((ds.width - size.X) / 2, null);

        bool first = true;
        foreach (var kv in this.ioLegendNames) {
            if (!first) {
                ds.Text(" / ");
            }
            ds.Text(kv.Key, colour: kv.Value);
            first = false;
        }
    }

    public void JumpBar(DrawingSurface ds, string text, DrawingSurface.Options options) {
        if (this.jumpDrives == 0) {
            return;
        }
        options.pct = this.GetPercent(this.jumpCurrent, this.jumpMax);
        options.text = text ?? Util.PctString(options.pct);
        ds.Bar(options);
    }

    public string IoString() {
        float io = this.batteries > 0 ? this.batteryInput - this.batteryOutputMW : this.CurrentInput() - this.CurrentOutput();
        float max = io > 0 ? this.MaxInput() : this.MaxOutput();

        return String.Format("{0:0.00} MW ({1})", io, Util.PctString(max == 0f ? 0f : Math.Abs(io) / max));
    }

    public void ReactorString(DrawingSurface ds, string text, DrawingSurface.Options options) {
        if (this.reactors == 0) {
            return;
        }
        string msg = text ?? options.text ?? "Reactors: ";
        ds.Text($"{msg}{this.reactors}, Output: {this.reactorOutputMW} MW, Ur: {this.reactorUranium}", options);
    }

    public void PowerConsumers(DrawingSurface ds, string text, DrawingSurface.Options options) {
        int max = Util.ParseInt(options.custom.Get("count") ?? "10");

        if (ds.width / (ds.charSizeInPx.X + 1f) < 40) {
            foreach (var item in this.consumerDict) {
                string kw = (item.Value * 1000).ToString("#,,# kW");
                ds.Text($"{item.Key}").SetCursor(ds.width, null).Text(kw, textAlignment: TextAlignment.RIGHT);

                if (--max == 0) {
                    return;
                }
                ds.Newline();
            }
        } else {
            this.widths[0] = 0;
            this.widths[1] = ds.width / 2 - 1.5f * ds.charSizeInPx.X;
            this.widths[2] = ds.width / 2 + 1.5f * ds.charSizeInPx.X;
            this.widths[3] = ds.width;

            int i = 0;
            foreach (var item in this.consumerDict) {
                string kw = (item.Value * 1000).ToString("#,,# kW");
                ds
                    .SetCursor(this.widths[(i++ % 4)], null)
                    .Text($"{item.Key}")
                    .SetCursor(this.widths[(i++ % 4)], null)
                    .Text(kw, textAlignment: TextAlignment.RIGHT);

                if (--max == 0) {
                    return;
                }
                if ((i % 4) == 0 || i >= this.consumerDict.Count * 2) {
                    ds.Newline();
                }
            }
        }
        ds.Newline(reverse: true);
    }
}
/* POWER */
