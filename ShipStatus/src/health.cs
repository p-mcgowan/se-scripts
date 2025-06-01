/*
 * BLOCK_HEALTH
 */
class BlockHealth : Runnable {
    public Program program;
    public Template template;
    public System.Text.RegularExpressions.Regex ignoreHealth;
    public List<IMyTerminalBlock> blocks;
    public Dictionary<string, string> damaged;
    public Dictionary<long, IMyTerminalBlock> shownOnHud;
    public string status;

    public BlockHealth(Program program, Template template) {
        this.program = program;
        this.template = template;
        this.blocks = new List<IMyTerminalBlock>();
        this.damaged = new Dictionary<string, string>();
        this.shownOnHud = new Dictionary<long, IMyTerminalBlock>();

        this.Reset();
    }

    public void Reset() {
        this.Clear();
        this.shownOnHud.Clear();

        if (this.program.config.Enabled("health")) {
            this.RegisterTemplateVars();

            string ignore = this.program.config.Get("healthIgnore");
            if (ignore != "" && ignore != null) {
                this.ignoreHealth = Util.Regex(System.Text.RegularExpressions.Regex.Replace(ignore, @"\s*,\s*", "|"));
            }
        }
    }

    public void Clear() {
        this.blocks.Clear();
        this.damaged.Clear();
    }

    public void RegisterTemplateVars() {
        if (this.template == null) {
            return;
        }

        this.template.Register("health.status", () => this.status);
        this.template.Register("health.blocks",
            (DrawingSurface ds, string text, DrawingSurface.Options options) => {
                if (this.damaged.Count() == 0) {
                    ds.Text("");
                    return;
                }
                foreach (KeyValuePair<string, string> block in this.damaged) {
                    ds.Text($"{block.Key} [{block.Value}]").Newline();
                }
                ds.Newline(reverse: true);
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

    public void GetBlock(IMyTerminalBlock block) {
        this.blocks.Add(block);
    }

    public void GotBLocks() {}

    public void Refresh() {
        if (this.blocks == null) {
            return;
        }

        this.damaged.Clear();
        bool showOnHud = this.program.config.Enabled("healthOnHud");

        foreach (var b in this.blocks) {
            if (!Util.BlockValid(b)) {
                continue;
            }
            if (this.ignoreHealth != null && this.ignoreHealth.IsMatch(b.CustomName)) {
                continue;
            }

            var health = this.GetHealth(b);
            if (health != 1f) {
                this.damaged[b.CustomName] = Util.PctString(health);

                if (showOnHud && !b.ShowOnHUD) {
                    this.shownOnHud[b.EntityId] = b;
                    b.ShowOnHUD = true;
                }
            }
        }

        if (showOnHud) {
            foreach (long key in this.shownOnHud.Keys.ToList()) {
                IMyTerminalBlock block = this.shownOnHud[key];
                if (block == null || !Util.BlockValid(block)) {
                    this.shownOnHud.Remove(key);
                }
                if (this.GetHealth(block) == 1f) {
                    block.ShowOnHUD = false;
                    this.shownOnHud.Remove(key);
                }
            }
        }

        this.status = $"{(this.damaged.Count == 0 ? "No damage" : "Damage")} detected";
    }
}
/* BLOCK_HEALTH */
