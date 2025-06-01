Template template;
int refetchBlocks = 0;
string outputTemplate = @"
[oxygen]
enableFillPct=0.3
disableFillPct=0.7

[Programmable Block Oxygen <0>]
output=
|Oxygen Status
|{oxygen.currentVolume} / {oxygen.maxVolume} L ({oxygen.fillPct})
|
|{oxygen.generationEnabled}
|
|{oxygen.blocks}
";
float enableFillPct = 0.3f;
float disableFillPct = 0.7f;
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
        ds.Newline(reverse: true);
    });
    template.Register("oxygen.generationEnabled", (DrawingSurface ds, string text, DrawingSurface.Options options) => {
        string message = options.custom.Get("txtDisabled") ?? "Oxygen generation off";
        if (oxygen.GetGenerators()) {
            message = options.custom.Get("txtEnabled") ?? "Oxygen generation on";
        }
        ds.Text(message, options);
    });

    oxygen = new OxygenSystem(this);
    oxygen.RefetchBlocks();

    Runtime.UpdateFrequency |= UpdateFrequency.Update100;
}

public void ReloadConfig() {
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
            template.PreRender(ds.name, this.config.Get($"{panelName}/output"));
        }
    }

    enableFillPct = Util.ParseFloat(config.Get("oxygen/enableFillPct"), 0.3f);
    disableFillPct = Util.ParseFloat(config.Get("oxygen/disableFillPct"), 0.7f);
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
        ReloadConfig();
    }
    if (++refetchBlocks % 4 == 0) {
        oxygen.RefetchBlocks();
        refetchBlocks = 0;
    }
    oxygen.GetOxygenLevels();

    if (oxygen.fillPct <= enableFillPct) {
        oxygen.SetGenerators(true);
    } else if (oxygen.fillPct >= disableFillPct) {
        oxygen.SetGenerators(false);
    }

    DrawStatus();
}
