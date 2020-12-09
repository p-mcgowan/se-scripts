/*
; CustomData config:
; the [global] section applies to the whole program, or sets defaults for shared
;
; For surface selection, use 'name <number>' eg: 'Cockpit <1>' - by default, the
; first surface is selected (0)
;
; The output section of the config is the template to render to the screen

[global]
;  global program settings (will overide settings detected in templates)
;  eg if a template has {power.bar}, then power will be enabled unless false here
;airlock=false
;production=false
;cargo=false
;power=false
;health=false
;  airlock config (defaults are shown)
;airlockOpenTime=750
;airlockAllDoors=false
;  health config (defaults are shown)
;healthIgnore=
;healthOnHud=false

[LCD Panel]
output=
|Jump drives: {power.jumpDrives}
|{power.jumpBar:bgColour=60,60,0}
|Batteries: {power.batteries}
|{power.batteryBar:bgColour=60,60,0}
|Reactors: {power.reactors} ({power.reactorMw} MW, {power.reactorUr} Ur)
|
|Ship status: {health.status}
|{health.blocks}
|{production.status}
|{production.blocks}
|
|Cargo: {cargo.stored} / {cargo.cap}
|{cargo.bar:bgColour=60,60,60}
|{cargo.items}

[Status panel]
output=
|{health.status}
|{health.blocks}
*/

Dictionary<string, DrawingSurface> drawables = new Dictionary<string, DrawingSurface>();
List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
List<string> strings = new List<string>();
MyIni ini = new MyIni();
Template template;
Config config = new Config();

public class Config {
    public Dictionary<string, string> settings;

    public Config() {
        this.settings = new Dictionary<string, string>();
    }

    public void Set(string name, string value) {
        this.settings[name] = value;
    }

    public string Get(string name, string alt = null) {
        return this.settings.Get(name, alt);
    }

    public bool Enabled(string name) {
        return this.settings.Get(name) == "true";
    }
}

public bool ParseCustomData() {
    MyIniParseResult result;
    if (!ini.TryParse(Me.CustomData, out result)) {
        Echo($"Failed to parse config:\n{result}");
        return false;
    }

    strings.Clear();
    ini.GetSections(strings);

    if (ini.ContainsSection("global")) {
        string setting = "";
        if (ini.Get("global", "airlock").TryGetString(out setting)) {
            config.Set("airlock", setting);
        }
        if (ini.Get("global", "production").TryGetString(out setting)) {
            config.Set("production", setting);
        }
        if (ini.Get("global", "cargo").TryGetString(out setting)) {
            config.Set("cargo", setting);
        }
        if (ini.Get("global", "power").TryGetString(out setting)) {
            config.Set("power", setting);
        }
        if (ini.Get("global", "health").TryGetString(out setting)) {
            config.Set("health", setting);
        }
        if (ini.Get("global", "healthIgnore").TryGetString(out setting)) {
            config.Set("healthIgnore", setting);
        }
        if (ini.Get("global", "airlockOpenTime").TryGetString(out setting)) {
            config.Set("airlockOpenTime", setting);
        }
        if (ini.Get("global", "airlockAllDoors").TryGetString(out setting)) {
            config.Set("airlockAllDoors", setting);
        }
        if (ini.Get("global", "healthOnHud").TryGetString(out setting)) {
            config.Set("healthOnHud", setting);
        }
    }


    foreach (string s in strings) {
        if (s == "global") {
            continue;
        }

        var tpl = ini.Get(s, "output");

        if (!tpl.IsEmpty) {
            Echo($"added output for {s}");
            Dictionary<string, bool> tokens = template.PreRender(s, tpl.ToString());
            foreach (var kv in tokens) {
                config.Set(kv.Key, config.Get(kv.Key, "true")); // don't override globals
            }
        }
    }

    string name;
    string surfaceName;
    IMyTextSurface surface;
    blocks.Clear();
    GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(blocks);
    foreach (IMyTextSurfaceProvider block in blocks) {
        for (int i = 0; i < block.SurfaceCount; i++) {
            name = ((IMyTerminalBlock)block).CustomName;
            surfaceName = $"{name} <{i}>";
            if (!strings.Contains(name) && !strings.Contains(surfaceName)) {
                continue;
            }

            surface = block.GetSurface(i);
            drawables.Add(surfaceName, new DrawingSurface(surface, this, $"{name} <{i}>"));
            if (i == 0 && block.SurfaceCount == 1) {
                drawables.Add(name, new DrawingSurface(surface, this, name));
            }
        }
    }

    return true;
}

public Program() {
    template = new Template(this);

    if (!ParseCustomData()) {
        Runtime.UpdateFrequency &= UpdateFrequency.None;
        Echo("Failed to parse custom data");
        return;
    }
    Echo($"airlock    : {config.Enabled("airlock")}");
    Echo($"power      : {config.Enabled("power")}");
    Echo($"cargo      : {config.Enabled("cargo")}");
    Echo($"health     : {config.Enabled("health")}");
    Echo($"production : {config.Enabled("production")}");

    powerDetails = new PowerDetails(this, template);
    cargoStatus = new CargoStatus(this, template);
    blockHealth = new BlockHealth(this, template);
    productionDetails = new ProductionDetails(this, template);
    airlock = new Airlock(this);

    Runtime.UpdateFrequency = UpdateFrequency.Update100;
    // airlocks on 10
    if (config.Enabled("airlock")) {
        Runtime.UpdateFrequency = UpdateFrequency.Update100 | UpdateFrequency.Update10;
    }
}

public void Main(string argument, UpdateType updateSource) {
    if (config.Enabled("airlock") && (updateSource & UpdateType.Update10) == UpdateType.Update10) {
        airlock.CheckAirlocks();

        if ((updateSource & UpdateType.Update100) != UpdateType.Update100) {
            return;
        }
    }

    if (config.Enabled("power")) {
        powerDetails.Refresh();
    }
    if (config.Enabled("cargo")) {
        cargoStatus.Refresh();
    }
    if (config.Enabled("health")) {
        blockHealth.Refresh();
    }
    if (config.Enabled("production")) {
        productionDetails.Refresh();
    }

    foreach (var kv in drawables) {
        template.Render(kv.Value);
    }
}
/* MAIN */
