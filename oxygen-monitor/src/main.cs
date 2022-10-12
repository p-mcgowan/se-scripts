Template template;
int refetchBlocks = 0;
string outputTemplate = @"[Programmable Block Oxygen <0>]
output=
|Oxygen Status
|{oxygen.currentVolume} / {oxygen.maxVolume} L ({oxygen.fillPct})
|
|{oxygen.blocks}
";
OxygenSystem oxygen;
List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
List<DrawingSurface> drawables = new List<DrawingSurface>();
string lastCustomData = "";

public Program() {
    if (Me.CustomData == "") {
        Me.CustomData = outputTemplate;
    }

    template = new Template(this);
    template.Register("oxygen.currentVolume", (DrawingSurface ds, string text, DrawingSurface.Options options) =>
        ds.Text($"{Util.FormatNumber(oxygen.currentVolume)}", options)
    );
    template.Register("oxygen.maxVolume", (DrawingSurface ds, string text, DrawingSurface.Options options) =>
        ds.Text($"{Util.FormatNumber(oxygen.maxVolume)}", options)
    );
    template.Register("oxygen.fillPct", (DrawingSurface ds, string text, DrawingSurface.Options options) =>
        ds.Text(Util.PctString(oxygen.fillPct), options)
    );
    template.Register("oxygen.blocks", (DrawingSurface ds, string text, DrawingSurface.Options options) => {
        foreach (IMyGasTank tank in oxygen.gasTanks) {
            double currentVolume = (tank.FilledRatio * tank.Capacity);

            ds
                .Text($@"{tank.CustomName} {Util.FormatNumber(currentVolume)} / {Util.FormatNumber(tank.Capacity)} L ({Util.PctString(tank.FilledRatio)})", options)
                .Newline();
        }
    });

    oxygen = new OxygenSystem(this);
    oxygen.RefetchBlocks();

    Runtime.UpdateFrequency |= UpdateFrequency.Update100;
}

public void FindDrawables() {
    string name;
    string surfaceName;
    IMyTextSurface surface;
    bool hasNumberedSurface;
    drawables.Clear();
    GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(blocks, block => block.IsSameConstructAs(Me));
    foreach (IMyTextSurfaceProvider block in blocks) {
        name = ((IMyTerminalBlock)block).CustomName;
        for (int i = 0; i < block.SurfaceCount; i++) {
            surfaceName = $"{name} <{i}>";

            hasNumberedSurface = this.config.sections.Contains(surfaceName);
            if (!this.config.sections.Contains(name) && !hasNumberedSurface) {
                continue;
            }

            surface = block.GetSurface(i);
            string panelName = hasNumberedSurface ? surfaceName : name;
            DrawingSurface ds = new DrawingSurface(surface, this, panelName);
            drawables.Add(ds);
            Echo(this.config.Get($"{panelName}/output"));
            template.PreRender(ds.name, this.config.Get($"{panelName}/output"));
        }
    }
}

public void DrawStatus() {
    foreach (DrawingSurface ds in drawables) {
        template.Render(ds);
    }
}

public void Main(string argument, UpdateType updateType) {
    if (Me.CustomData != lastCustomData) {
        Echo("reloading templates");
        config.Parse(Me.CustomData);
        lastCustomData = Me.CustomData;
        FindDrawables();
    }
    if (++refetchBlocks % 4 == 0) {
        oxygen.RefetchBlocks();
        refetchBlocks = 0;
    }
    oxygen.GetOxygenLevels();

    if (oxygen.fillPct < 0.3) {
        oxygen.SetGenerators(true);
    } else if (oxygen.fillPct > 0.7) {
        oxygen.SetGenerators(false);
    }

    DrawStatus();
}
