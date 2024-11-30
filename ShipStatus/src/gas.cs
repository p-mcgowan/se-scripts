/*
 * GAS
 */
 GasStatus gasStatus;

public class GasStatus {
    public Program program;
    public Template template;
    public List<IMyGasTank> o2Tanks;
    public List<IMyGasTank> h2Tanks;
    public List<IMyGasGenerator> oxyGens;
    public Dictionary<string, double> tankMap;
    public double o2CurrentVolume;
    public float o2MaxVolume;
    public double o2FillPct;
    public int o2TankCount;
    public double h2CurrentVolume;
    public float h2MaxVolume;
    public double h2FillPct;
    public int h2TankCount;
    public double enableFillPct;
    public double disableFillPct;

    public GasStatus(Program program, Template template = null) {
        this.program = program;
        this.template = template;
        this.o2Tanks = new List<IMyGasTank>();
        this.h2Tanks = new List<IMyGasTank>();
        this.oxyGens = new List<IMyGasGenerator>();
        this.tankMap = new Dictionary<string, double>();

        this.Reset();
    }

    public void Reset() {
        this.Clear();
        if (this.program.config.Enabled("gas")) {
            this.RegisterTemplateVars();
        }
    }

    public void Clear() {
        this.ClearTotals();
        this.o2Tanks.Clear();
        this.h2Tanks.Clear();
        this.tankMap.Clear();
    }

    public void RegisterTemplateVars() {
        if (this.template == null) {
            return;
        }

        this.template.Register("gas.o2CurrentVolume", () => $"{Util.FormatNumber(this.o2CurrentVolume)}");
        this.template.Register("gas.h2CurrentVolume", () => $"{Util.FormatNumber(this.h2CurrentVolume)}");
        this.template.Register("gas.o2MaxVolume", () => $"{Util.FormatNumber(this.o2MaxVolume)}");
        this.template.Register("gas.h2MaxVolume", () => $"{Util.FormatNumber(this.h2MaxVolume)}");
        this.template.Register("gas.o2FillPct", () => Util.PctString(this.o2FillPct));
        this.template.Register("gas.h2FillPct", () => Util.PctString(this.h2FillPct));
        this.template.Register("gas.o2Tanks", (DrawingSurface ds, string text, DrawingSurface.Options options) => {
            this.PrintTanks(this.o2Tanks, ds, options);
        });
        this.template.Register("gas.h2Tanks", (DrawingSurface ds, string text, DrawingSurface.Options options) => {
            this.PrintTanks(this.h2Tanks, ds, options);
        });
        this.template.Register("gas.generationEnabled", (DrawingSurface ds, string text, DrawingSurface.Options options) => {
            string message = options.custom.Get("txtDisabled") ?? "Gas generation off";
            if (this.GetGenerators()) {
                message = options.custom.Get("txtEnabled") ?? "Gas generation on";
            }
            ds.Text(message, options);
        });
        this.template.Register("gas.generationEnabledExtended", (DrawingSurface ds, string text, DrawingSurface.Options options) => {
            this.GasExtendedMessage(ds, text, options);
        });
        this.template.Register("gas.o2Bar", (DrawingSurface ds, string text, DrawingSurface.Options options) => {
            this.GasBar((float)o2FillPct, ds, options);
        });
        this.template.Register("gas.h2Bar", (DrawingSurface ds, string text, DrawingSurface.Options options) => {
            this.GasBar((float)h2FillPct, ds, options);
        });
    }

    public void GetBlock(IMyTerminalBlock block) {
        if (block is IMyGasTank) {
            string name = block.DefinitionDisplayNameText.ToString();
            if (name.Contains("Oxygen Tank")) {
                this.o2Tanks.Add((IMyGasTank)block);
            } else if (name.Contains("Hydrogen Tank")) {
                this.h2Tanks.Add((IMyGasTank)block);
            }
        } else if (block is IMyGasGenerator) {
            this.oxyGens.Add((IMyGasGenerator)block);
        }
    }

    public void GotBLocks() {
        this.enableFillPct = Util.ParseFloat(this.program.config.Get("gasEnableFillPct"), -1f);
        this.disableFillPct = Util.ParseFloat(this.program.config.Get("gasDisableFillPct"), -1f);
    }

    public void Refresh() {
        this.ClearTotals();
        this.GetGasLevels();
    }

    public void ClearTotals() {
        this.o2CurrentVolume = 0f;
        this.o2MaxVolume = 0f;
        this.o2FillPct = 0f;
        this.o2TankCount = 0;

        this.h2CurrentVolume = 0f;
        this.h2MaxVolume = 0f;
        this.h2FillPct = 0f;
        this.h2TankCount = 0;
    }

    public void PrintTanks(List<IMyGasTank> tanks, DrawingSurface ds, DrawingSurface.Options options) {
        foreach (IMyGasTank tank in tanks) {
            double currentVolume = (tank.FilledRatio * tank.Capacity);

            ds
                .Text($@"{tank.CustomName}: {Util.FormatNumber(currentVolume)} / {Util.FormatNumber((double)tank.Capacity)} L ({Util.PctString(tank.FilledRatio)})", options)
                .Newline();
        }
        ds.Newline(reverse: true);
    }

    public void GasBar(float pct, DrawingSurface ds, DrawingSurface.Options options) {
        options.pct = pct;
        options.text = Util.PctString(pct);
        options.textColour = options.textColour ?? ds.surface.ScriptForegroundColor;

        ds.Bar(options);
    }

    public void SetGenerators(bool enabled) {
        foreach (IMyGasGenerator genny in this.oxyGens) {
            genny.Enabled = enabled;
        }
    }

    public bool GetGenerators() {
        foreach (IMyGasGenerator genny in this.oxyGens) {
            if (genny.Enabled) {
                return true;
            }
        }
        return false;
    }

    public void GetGasLevels() {
        this.tankMap.Clear();

        foreach (IMyGasTank tank in this.o2Tanks) {
            this.o2CurrentVolume += (tank.FilledRatio * tank.Capacity);
            this.o2MaxVolume += tank.Capacity;
            this.o2TankCount++;

            if (!this.tankMap.ContainsKey(tank.CustomName)) {
                this.tankMap.Add(tank.CustomName, tank.FilledRatio);
            } else {
                this.tankMap[tank.CustomName] = this.tankMap[tank.CustomName] + tank.FilledRatio;
            }
        }

        foreach (IMyGasTank tank in this.h2Tanks) {
            this.h2CurrentVolume += (tank.FilledRatio * tank.Capacity);
            this.h2MaxVolume += tank.Capacity;
            this.h2TankCount++;

            if (!this.tankMap.ContainsKey(tank.CustomName)) {
                this.tankMap.Add(tank.CustomName, tank.FilledRatio);
            } else {
                this.tankMap[tank.CustomName] = this.tankMap[tank.CustomName] + tank.FilledRatio;
            }
        }

        this.o2FillPct = this.o2TankCount == 0 ? 0 : this.o2CurrentVolume / this.o2MaxVolume;
        this.h2FillPct = this.h2TankCount == 0 ? 0 : this.h2CurrentVolume / this.h2MaxVolume;

        if (this.enableFillPct != -1 && (this.o2FillPct <= this.enableFillPct || this.h2FillPct <= this.enableFillPct)) {
            this.SetGenerators(true);
        }
        if (this.disableFillPct != -1 && this.o2FillPct >= this.disableFillPct && this.h2FillPct >= this.disableFillPct) {
            this.SetGenerators(false);
        }
    }

    public void GasExtendedMessage(DrawingSurface ds, string text, DrawingSurface.Options options) {
        var max = Math.Floor(100 * Math.Max(this.o2FillPct, this.h2FillPct));
        var min = Math.Ceiling(100 * Math.Min(this.o2FillPct, this.h2FillPct));
        string message = options.custom.Get("txtDisabled") ?? $"Gas generation off";
        if (this.enableFillPct != -1) {
            message = message + $", enabled at {Math.Floor(100 * this.enableFillPct)}% (current: {min}%)";
        }
        if (this.GetGenerators()) {
            message = options.custom.Get("txtEnabled") ?? $"Gas generation on";
            if (this.disableFillPct != -1) {
                message = message + $", disabled at {Math.Ceiling(100 * this.disableFillPct)}% (current: {max}%)";
            }
        }
        ds.Text(message, options);
    }
}
/* GAS */
