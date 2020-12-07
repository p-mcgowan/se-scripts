/*
; CustomData config:
; the [global] section applies to the whole program, or sets defaults for shared
; config (either using mode=merge or mode = replace)
;
; For surface selection, use 'name <number>' eg: 'Cockpit <1>' - by default, the
; first surface is selected (0)

[global]
airlock=true
healthIgnore=Hydrogen Thruster,Suspension

[Interior light 13]
cargoLight=true

[Programmable block <0>]
output=
|Jump drives: {power.jumpDrives}
|{power.jumpBar}
|Batteries: {power.batteries}
|{power.batteryBar}
|Solar: {power.solars}
|Energy IO: {power.io}
|{power.energyio}
|Reactors: {power.reactors}
|
|Ship status: {health.status}
|{health.blocks}
|
|{production.mode}
|{production.blocks}
|
|Cargo: {cargo.stored} / {cargo.cap}
|{cargo.bar}
|{cago.items}
healthIgnore=Wheel
healthIgnoreMode=merge

[Status panel]
output=
|health
healthIgnore=
healthIgnoreMode=replace
*/

List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
List<string> strings = new List<string>();
MyIni ini = new MyIni();
SurfaceManager surfaceManager;
Template template = new Template();

// Dictionary<string, List<IMyTextSurface>> programOutputs = new Dictionary<string, List<IMyTextSurface>>();
// public string[] programKeys = { "AIRLOCK", "BLOCK_HEALTH", "CARGO", "CARGO_CAP", "CARGO_CAP_STYLE", "CARGO_LIGHT", "HEALTH_IGNORE", "INPUT", "JUMP_BAR", "POWER", "POWER_BAR", "PRODUCTION" };

public class SurfaceManager: Graphics {
    public Program program;
    public Dictionary<string, string> panelTemplates;

    // drawables dict<panelName, drawable>

    public SurfaceManager(Program program) {
        this.program = program;
        this.panelTemplates = new Dictionary<string, string>();
    }

    // public MyIniValue GetIni(string surface, string key) {
    //     return this.program.ini.Get(surface, key);
    // }

    // public void FormatOutput(string surfaceId) {
    //     if (this.GetIni(surfaceId, "output") == null) {
    //         return;
    //     }

    //     strings.Clear();
    //     ini.Get(name, "output").GetLines(strings);
    //     foreach (var line in strings) {
    //         var propertyB = classB.GetType().GetProperty(y);
    //         var propertyA = classA.GetType().GetProperty(x);
    //     }
    // }
}

public class Panel {
    // surface
    // config
    // cargo
    // health
    // production
    public string name;
    public int surfaceId;

    public Panel(string _name, int _surfaceId = 0) {
        name = _name;
        surfaceId = _surfaceId;
    }
}

public void ParsePanelConfig(string input, ref Panel panel) {
    var matches = Util.surfaceExtractor.Matches(input);
    if (matches.Count > 0 && matches[0].Groups.Count > 1) {
        Int32.TryParse(matches[0].Groups[1].Value, out panel.surfaceId);
        var panelName = input.Replace(matches[0].Groups[0].Value, "");
        panel.name = panelName;
    }

    return;
}

public bool ParseCustomData() {
    MyIniParseResult result;
    if (!ini.TryParse(Me.CustomData, out result)) {
        Echo($"Failed to parse config:\n{result}");
        return false;
    }

    surfaceManager = new SurfaceManager(this);
    strings.Clear();
    ini.GetSections(strings);

    if (ini.ContainsSection("global")) {
        // airlock=true
        // healthIgnore=Hydrogen Thruster,Suspension
    }

    foreach (string name in strings) {
        if (name == "global") {
            continue;
        }

        var tpl = ini.Get(name, "output");

        if (!tpl.IsEmpty) {
            Echo($"added output for {name}");
            surfaceManager.panelTemplates.Add(name, tpl.ToString());
        }
    }

    // blocks.Clear();
    // GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(blocks, b => b.IsSameConstructAs(Me));
    // Dictionary<string, IMyTextSurfaceProvider> blockHash = new Dictionary<string, IMyTextSurfaceProvider>();

    // foreach (IMyTextSurfaceProvider block in blocks) {
    //     blockHash.Add(((IMyTerminalBlock)block).CustomName, block);
    // }
    // Panel panel = new Panel("meh");

    // foreach (string key in programKeys) {
    //     var value = ini.Get(key, "enabled").ToBoolean();
    //     if (ini.Get(key, "enabled").ToBoolean()) {
    //         string outputs = ini.Get(key, "output").ToString();
    //         if (outputs != "") {
    //             List<IMyTextSurface> surfaces = new List<IMyTextSurface>();

    //             // split on newlines, fetch surfaces, find in blokcs and add to list
    //             foreach (string outname in outputs.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)) {
    //                 ParsePanelConfig(outname, ref panel);
    //                 if (blockHash.ContainsKey(panel.name)) {
    //                     surfaces.Add(blockHash[panel.name].GetSurface(panel.surfaceId));
    //                 }
    //             }

    //             programOutputs.Add(key, surfaces);
    //         }
    //     }
    // }

    // var items = sx.Split(new[] { '\n', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Split(new[] { '=' }));

    // blocks.Clear();
    // GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(blocks);
    // foreach (IMyTextSurfaceProvider block in blocks) {
    //     if (is in config){}
    //     for (int i = 0; i < block.SurfaceCount; i++) {
    //         IMyTextSurface surface = block.GetSurface(i);
    //         graphics.drawables.Add($"{((IMyTerminalBlock)block).CustomName} <{i}>", new DrawingSurface(surface, this));
    //     }
    // }
    return true;
}

public Program() {
    // cargo = new List<IMyTerminalBlock>();
    // items = new List<MyInventoryItem>();
    // Runtime.UpdateFrequency = UpdateFrequency.Update100;

    if (!ParseCustomData()) {
        Runtime.UpdateFrequency &= UpdateFrequency.None;
        Echo("Failed to parse custom data");
        return;
    }
    // airlocks on 10
    Runtime.UpdateFrequency = UpdateFrequency.Update100 | UpdateFrequency.Update10;

    // CargoStatus cargoStatus = new CargoStatus(this);
    template.program = this;
    powerDetails = new PowerDetails(this);
    cargoStatus = new CargoStatus(this, template);

    blocks.Clear();
    GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(blocks);
    foreach (IMyTextSurfaceProvider block in blocks) {
        for (int i = 0; i < block.SurfaceCount; i++) {
            IMyTextSurface surface = block.GetSurface(i);
            surfaceManager.drawables.Add($"{((IMyTerminalBlock)block).CustomName} <{i}>", new DrawingSurface(surface, this));
        }
    }
}

public void Main(string argument, UpdateType updateSource) {
    // Echo($"updateSource: {updateSource}");
    if (/* should airlock */(updateSource & UpdateType.Update10) == UpdateType.Update10) {
        // if (!airlocks.Any()) {
        //     GetMappedAirlocks();
        // }
        // foreach (var al in airlocks) {
        //     al.Value.Test();
        // }

        return;
    }
    // HandleInput();
    // if (outLock != 0) {
    //     return;
    // }
    // ClearOutputs();

    /* if should do power */
    powerDetails.Refresh();
    /* if should do cargo */
    cargoStatus.Refresh();

    string power = powerDetails.ToString();
    string cargo = cargoStatus.ToString();
    // Echo(power);
    // Echo(cargo);

    foreach (string key in surfaceManager.drawables.Keys) {
        if (!surfaceManager.panelTemplates.ContainsKey(key)) {
            continue;
        }
        DrawingSurface ds = surfaceManager.drawables[key];
        template.Render(ref ds, surfaceManager.panelTemplates[key].Split('\n'));
    }
    // blocks.Clear();
    // GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(blocks);
    // foreach (IMyTextSurfaceProvider block in blocks) {
    //     for (int i = 0; i < block.SurfaceCount; i++) {
    //         WriteTextToSurface(block.GetSurface(i), cargo);
    //     }
    // }
        // ProgressBar(CFG.POWER, currentCharge / maxCharge) + "\n";

    // if (CanWriteToSurface(settings[CFG.BLOCK_HEALTH])) {
    //     string blockHealth = DoBlockHealth();
    //     WriteToLCD(settings[CFG.BLOCK_HEALTH], blockHealth, true);
    // }

    // if (CanWriteToSurface(settings[CFG.PRODUCTION])) {
    //     WriteToLCD(settings[CFG.PRODUCTION], DoProductionDetails(this), true);
    // }

    // CargoStatus cStats = null;

    // if (CanWriteToSurface(settings[CFG.CARGO_CAP])) {
    //     cStats = DoCargoStatus();
    //     if (settings[CFG.CARGO_CAP_STYLE] == "small") {
    //         WriteToLCD(settings[CFG.CARGO_CAP], ProgressBar(CFG.CARGO_CAP, cStats.pct, false, 7), true);
    //     } else {
    //         WriteToLCD(settings[CFG.CARGO_CAP], "Cargo status: " + Util.PctString(cStats.pct) + '\n' + cStats.barCap, true);
    //     }
    // }

    // if (CanWriteToSurface(settings[CFG.CARGO])) {
    //     if (cStats == null) {
    //         cStats = DoCargoStatus();
    //     }

    //     // dont write status if it's on another panel
    //     if (!CanWriteToSurface(settings[CFG.CARGO_CAP])) {
    //         WriteToLCD(settings[CFG.CARGO], "Cargo status: " + Util.PctString(cStats.pct) + '\n' + cStats.bar + '\n', true);
    //     }
    //     WriteToLCD(settings[CFG.CARGO], cStats.itemText, true);
    // }
}
/* MAIN */
