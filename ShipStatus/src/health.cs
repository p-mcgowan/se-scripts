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
        this.GetBlocks();
        this.RegisterTemplateVars();
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
        float MaxIntegrity = slimblock.MaxIntegrity;
        float BuildIntegrity = slimblock.BuildIntegrity;
        float CurrentDamage = slimblock.CurrentDamage;

        return (BuildIntegrity - CurrentDamage) / MaxIntegrity;
    }

    public void GetBlocks() {
        this.blocks.Clear();
        this.program.GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(this.blocks, b => b.IsSameConstructAs(this.program.Me));
    }

    public void Refresh() {
        this.damaged.Clear();

        // System.Text.RegularExpressions.Regex ignoreHealth = null;
        // if (settings[CFG.HEALTH_IGNORE] != "") {
        //     string input = System.Text.RegularExpressions.Regex.Replace(settings[CFG.HEALTH_IGNORE], @"\s*,\s*", "|");
        //     ignoreHealth = Regex(input);
        // }
        // CFG.HEALTH_IGNORE, "Hydrogen Thruster, Suspension"


        // int chars;
        // GetPanelWidthInChars(settings[CFG.BLOCK_HEALTH], out chars);

        foreach (var b in this.blocks) {
            if (this.ignoreHealth != null && this.ignoreHealth.IsMatch(b.CustomName)) {
                continue;
            }

            var health = this.GetHealth(b);
            if (health != 1f) {
                this.damaged[b.CustomName] = Util.PctString(health);
                b.ShowOnHUD = true;
            } else {
                b.ShowOnHUD = false;
            }
        }

        this.status = $"{(this.damaged.Count == 0 ? "No damage" : "Damage")} detected";
    }
}
/* BLOCK_HEALTH */
