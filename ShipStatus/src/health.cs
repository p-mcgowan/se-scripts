/*
 * BLOCK_HEALTH
 */
BlockHealth blockHealth;

class BlockHealth {
    public Program program;
    public Template template;
    public System.Text.RegularExpressions.Regex ignoreHealth;
    public List<IMyTerminalBlock> blocks;
    public Dictionary<string, string> damaged;
    public string status;

    public BlockHealth(Program program, Template template) {
        this.program = program;
        this.template = template;
        this.blocks = new List<IMyTerminalBlock>();
        this.damaged = new Dictionary<string, string>();

        this.Reset();
    }

    public void Reset() {
        this.Clear();

        if (this.program.config.Enabled("health")) {
            this.GetBlocks();
            this.RegisterTemplateVars();

            string ignore = this.program.config.Get("healthIgnore");
            if (ignore != "" && ignore != null) {
                this.ignoreHealth = Util.Regex(System.Text.RegularExpressions.Regex.Replace(ignore, @"\s*,\s*", "|"));
            }
        }
    }

    public void Clear() {
        this.damaged.Clear();
    }

    public void RegisterTemplateVars() {
        if (this.template == null) {
            return;
        }

        this.template.Register("health.status", () => this.status);
        this.template.Register("health.blocks",
            (DrawingSurface ds, string text, Dictionary<string, string> options) => {
                foreach (KeyValuePair<string, string> block in this.damaged) {
                    ds.Text($"{block.Key} [{block.Value}]").Newline();
                }
            }
        );
    }

    public float GetHealth(IMyTerminalBlock block) {
        IMySlimBlock slimblock = block.CubeGrid.GetCubeBlock(block.Position);
        if (slimblock == null) {
            return 1f;
        }
        float MaxIntegrity = slimblock.MaxIntegrity;
        float BuildIntegrity = slimblock.BuildIntegrity;
        float CurrentDamage = slimblock.CurrentDamage;

        return (BuildIntegrity - CurrentDamage) / MaxIntegrity;
    }

    public void GetBlocks() {
        this.blocks.Clear();
        this.program.GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(this.blocks, b =>
            this.program.config.Enabled("getAllGrids") || b.IsSameConstructAs(this.program.Me));
    }

    public void Refresh() {
        this.damaged.Clear();
        bool showOnHud = this.program.config.Enabled("healthOnHud");

        foreach (var b in this.blocks) {
            if (b == null) {
                continue;
            }
            if (this.ignoreHealth != null && this.ignoreHealth.IsMatch(b.CustomName)) {
                continue;
            }

            var health = this.GetHealth(b);
            if (health != 1f) {
                this.damaged[b.CustomName] = Util.PctString(health);
            }
            if (showOnHud) {
                b.ShowOnHUD = health != 1f;
            }
        }

        this.status = $"{(this.damaged.Count == 0 ? "No damage" : "Damage")} detected";
    }
}
/* BLOCK_HEALTH */
