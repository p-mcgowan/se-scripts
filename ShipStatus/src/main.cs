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
;airlock=true
;production=false
;cargo=false
;power=false
;health=false
;  airlock config (defaults are shown)
;airlockOpenTime=750
;airlockAllDoors=false
;airlockDoorMatch=Door(.*)
;airlockDoorExclude=Hangar
;  health config (defaults are shown)
;healthIgnore=
;healthOnHud=false

[LCD Panel]
output=
|Jump drives: {power.jumpDrives}
|{power.jumpBar}
|Batteries: {power.batteries}
|{power.batteryBar}
|Reactors: {power.reactors}, Output: {power.reactorOutputMW} MW  ({power.reactorUr} Ur)
|Solar panels: {power.solars}, Output: {power.solarOutputMW} MW
|Wind turbines: {power.turbines}, Output: {power.turbineOutputMW} MW
|H2 Engines: {power.engines}, Output: {power.engineOutputMW} MW
|Energy IO: {power.ioString}
|{power.ioBar}
|{power.ioLegend}
|
|Ship status: {health.status}
|{health.blocks}
|
|{production.status}
|{production.blocks}
|
|Cargo: {cargo.stored} / {cargo.cap}
|{cargo.bar}
|{cargo.items}
*/

public StringBuilder log = new StringBuilder("");
Dictionary<string, DrawingSurface> drawables = new Dictionary<string, DrawingSurface>();
List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
List<IMyTerminalBlock> allBlocks = new List<IMyTerminalBlock>();
List<string> strings = new List<string>();
MyIni ini = new MyIni();
Template template;
Config config = new Config();
Dictionary<string, string> templates = new Dictionary<string, string>();
IEnumerator<string> stateMachine;
int i = 0;

public class Config {
    public Dictionary<string, string> settings;
    public string customData;

    public Config() {
        this.settings = new Dictionary<string, string>();
    }

    public void Clear() {
        this.settings.Clear();
        this.customData = null;
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

    config.Clear();
    config.customData = Me.CustomData;
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
        if (ini.Get("global", "airlockDoorMatch").TryGetString(out setting)) {
            config.Set("airlockDoorMatch", setting);
        }
        if (ini.Get("global", "airlockDoorExclude").TryGetString(out setting)) {
            config.Set("airlockDoorExclude", setting);
        }
        if (ini.Get("global", "healthOnHud").TryGetString(out setting)) {
            config.Set("healthOnHud", setting);
        }
        if (ini.Get("global", "getAllGrids").TryGetString(out setting)) {
            config.Set("getAllGrids", setting);
        }
    }

    foreach (string outname in strings) {
        if (outname == "global") {
            continue;
        }

        var tpl = ini.Get(outname, "output");

        if (!tpl.IsEmpty) {
            templates[outname] = tpl.ToString();
        }
    }

    string name;
    string surfaceName;
    IMyTextSurface surface;
    drawables.Clear();
    bool hasNumberedSurface;

    blocks.Clear();
    GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(blocks);

    foreach (IMyTextSurfaceProvider block in blocks) {
        for (int i = 0; i < block.SurfaceCount; i++) {
            name = ((IMyTerminalBlock)block).CustomName;
            surfaceName = $"{name} <{i}>";

            hasNumberedSurface = strings.Contains(surfaceName);
            if (!strings.Contains(name) && !hasNumberedSurface) {
                continue;
            }

            surface = block.GetSurface(i);
            if (hasNumberedSurface) {
                drawables.Add(surfaceName, new DrawingSurface(surface, this, surfaceName));
            } else {
                drawables.Add(surfaceName, new DrawingSurface(surface, this, name));
            }
        }
    }

    return true;
}

public bool Configure() {
    if (!ParseCustomData()) {
        Runtime.UpdateFrequency &= UpdateFrequency.None;
        Echo("Failed to parse custom data");

        return false;
    }

    Runtime.UpdateFrequency = UpdateFrequency.Update100;
    // airlocks on 10
    if (config.Enabled("airlock")) {
        Runtime.UpdateFrequency |= UpdateFrequency.Update10;
    }

    if (stateMachine != null) {
        stateMachine.Dispose();
    }
    stateMachine = RunStuffOverTime();
    Runtime.UpdateFrequency |= UpdateFrequency.Once;

    return true;
}

public void RefetchBlocks() {
    cargoStatus.Clear();
    airlock.Clear();
    blockHealth.Clear();
    powerDetails.Clear();
    productionDetails.Clear();

    GridTerminalSystem.GetBlocks(allBlocks);
    foreach (IMyTerminalBlock block in allBlocks) {
        if (!Util.BlockValid(block)) {
            continue;
        }
        if (!config.Enabled("getAllGrids") && !block.IsSameConstructAs(Me)) {
            continue;
        }

        powerDetails.GetBlock(block);
        cargoStatus.GetBlock(block);
        blockHealth.GetBlock(block);
        productionDetails.GetBlock(block);
        airlock.GetBlock(block);
    }

    cargoStatus.GotBLocks();
    airlock.GotBLocks();
    blockHealth.GotBLocks();
    powerDetails.GotBLocks();
    productionDetails.GotBLocks();
}

public bool RecheckFailed() {
    if (++i % 5 == 0 || String.CompareOrdinal(config.customData, Me.CustomData) != 0) {
        return !Configure();
    }

    if (i % 2 == 0) {
        RefetchBlocks();
        log.Clear();
    }

    return false;
}

public Program() {
    GridTerminalSystem.GetBlocks(allBlocks);
    template = new Template(this);
    powerDetails = new PowerDetails(this, template);
    cargoStatus = new CargoStatus(this, template);
    blockHealth = new BlockHealth(this, template);
    productionDetails = new ProductionDetails(this, template);
    airlock = new Airlock(this);

    if (!Configure()) {
        return;
    }
}

public void Main(string argument, UpdateType updateType) {
    Echo(log.ToString());

    if ((updateType & UpdateType.Once) == UpdateType.Once) {
        RunStateMachine();
        return;
    }

    if ((updateType & UpdateType.Update10) == UpdateType.Update10 && config.Enabled("airlock")) {
        airlock.CheckAirlocks();
    }

    if ((updateType & UpdateType.Update100) == UpdateType.Update100) {
        if (RecheckFailed()) {
            Runtime.UpdateFrequency &= UpdateFrequency.None;
            Echo("Failed to parse custom data");
            return;
        }

        if (stateMachine == null) {
            stateMachine = RunStuffOverTime();
            Runtime.UpdateFrequency |= UpdateFrequency.Once;
        }
    }
}

public void RunStateMachine() {
    if (stateMachine != null) {
        bool hasMoreSteps = stateMachine.MoveNext();

        if (hasMoreSteps) {
            Runtime.UpdateFrequency |= UpdateFrequency.Once;
        } else {
            stateMachine.Dispose();
            stateMachine = null;
            Runtime.UpdateFrequency &= ~UpdateFrequency.Once;
        }
    }
}

public IEnumerator<string> RunStuffOverTime()  {
    string content;
    string outputName;
    while (templates.Any()) {
        outputName = templates.Keys.Last();
        templates.Pop(templates.Keys.Last(), out content);

        Dictionary<string, bool> tokens;
        if (template.IsPrerendered(outputName, content)) {
            tokens = template.templateVars[outputName];
        } else {
            log.Append($"Adding or updating {outputName}\n");
            tokens = template.PreRender(outputName, content);
        }

        foreach (var kv in tokens) {
            config.Set(kv.Key, config.Get(kv.Key, "true")); // don't override globals
        }

        yield return $"templates {outputName}";

        if (templates.Count == 0) {
            powerDetails.Reset();
            cargoStatus.Reset();
            blockHealth.Reset();
            productionDetails.Reset();
            airlock.Reset();

            yield return "reset";
            RefetchBlocks();
        }

        yield return "updated";
    }

    if (config.Enabled("power")) {
        powerDetails.Refresh();
        yield return "powerDetails";
    }
    if (config.Enabled("cargo")) {
        cargoStatus.Refresh();
        yield return "cargoStatus";
    }
    if (config.Enabled("health")) {
        blockHealth.Refresh();
        yield return "blockHealth";
    }
    if (config.Enabled("production")) {
        productionDetails.Refresh();
        yield return "productionDetails";
    }

    for (int j = 0; j < drawables.Count; ++j) {
        var dw = drawables.ElementAt(j);
        template.Render(dw.Value);
        yield return $"render {dw.Key}";
    }

    yield break;
}
/* MAIN */
