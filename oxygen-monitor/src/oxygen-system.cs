public class OxygenSystem {
    public Program program;
    public List<IMyGasTank> gasTanks = new List<IMyGasTank>();
    public List<IMyGasGenerator> oxyGens = new List<IMyGasGenerator>();
    public Dictionary<string, double> tankMap = new Dictionary<string, double>();
    public double currentVolume = 0f;
    public float maxVolume = 0f;
    public double fillPct = 0f;
    public int tankCount = 0;

    public OxygenSystem(Program p) {
        this.program = p;
        this.Reset();
    }

    public void Reset() {
        this.gasTanks.Clear();
        this.tankMap.Clear();
        this.currentVolume = 0f;
        this.fillPct = 0f;
        this.maxVolume = 0f;
        this.tankCount = 0;
    }

    public void RefetchBlocks() {
        this.gasTanks.Clear();
        this.program.GridTerminalSystem.GetBlocksOfType<IMyGasTank>(this.gasTanks, b => b.IsSameConstructAs(this.program.Me) && b.DefinitionDisplayNameText.ToString() == "Oxygen Tank");

        this.oxyGens.Clear();
        this.program.GridTerminalSystem.GetBlocksOfType<IMyGasGenerator>(this.oxyGens, b => b.IsSameConstructAs(this.program.Me));
    }

    public void SetGenerators(bool enabled) {
        foreach (IMyGasGenerator genny in this.oxyGens) {
            genny.Enabled = enabled;
        }
    }

    public void GetOxygenLevels() {
        this.currentVolume = 0f;
        this.maxVolume = 0f;
        this.fillPct = 0f;
        this.tankCount = 0;

        this.tankMap.Clear();

        foreach (IMyGasTank tank in this.gasTanks) {
            this.currentVolume += (tank.FilledRatio * tank.Capacity);
            this.maxVolume += tank.Capacity;
            this.fillPct += tank.FilledRatio;
            this.tankCount++;

            if (!this.tankMap.ContainsKey(tank.CustomName)) {
                this.tankMap.Add(tank.CustomName, tank.FilledRatio);
            } else {
                this.tankMap[tank.CustomName] = this.tankMap[tank.CustomName] + tank.FilledRatio;
            }
        }

        this.fillPct = this.tankCount == 0 ? 0 : this.fillPct / this.tankCount;
    }
}
