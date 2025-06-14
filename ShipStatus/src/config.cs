/*
 * CONFIG
 */
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

    if (Me.CustomData == "") {
        Me.CustomData = customDataInit;
    }

    config.Clear();
    config.customData = Me.CustomData;
    strings.Clear();
    ini.GetSections(strings);
    template.Reset();
    templates.Clear();
    config.Set("airlock", "true");

    string themeConfig = "";

    if (ini.ContainsSection("global")) {
        string setting = "";
        List<MyIniKey> keys = new List<MyIniKey>();
        ini.GetKeys("global", keys);
        foreach (MyIniKey key in keys) {
            if (ini.Get(key).TryGetString(out setting)) {
                string configKey = key.ToString().Replace("global/", "");
                config.Set(configKey, setting);
                if (configKey == "config") {
                    themeConfig = $"{{config:{setting}}}\n";
                }
            }
        }
    }

    foreach (string outname in strings) {
        if (outname == "global") {
            continue;
        }

        var tpl = ini.Get(outname, "output");

        if (!tpl.IsEmpty) {
            templates[outname] = themeConfig + tpl.ToString();
        }
    }

    string name;
    string surfaceName;
    IMyTextSurface surface;
    drawables.Clear();
    bool hasNumberedSurface;

    blocks.Clear();
    if (config.Enabled("getAllGrids")) {
        GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(blocks);
    } else {
        GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(blocks, block => block.IsSameConstructAs(Me));
    }

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
                drawables[surfaceName] = new DrawingSurface(surface, this, surfaceName);
            } else {
                drawables[name] = new DrawingSurface(surface, this, name);
            }
        }
    }

    return true;
}

public bool Configure() {
    if (stateMachine != null) {
        stateMachine.Dispose();
    }

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

    stateMachine = RunStuffOverTime();
    Runtime.UpdateFrequency |= UpdateFrequency.Once;

    return true;
}

public void RefetchBlocks() {
    powerDetails.Clear();
    cargoStatus.Clear();
    blockHealth.Clear();
    productionDetails.Clear();
    airlock.Clear();
    gasStatus.Clear();

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
        gasStatus.GetBlock(block);
    }

    powerDetails.GotBLocks();
    cargoStatus.GotBLocks();
    blockHealth.GotBLocks();
    productionDetails.GotBLocks();
    airlock.GotBLocks();
    gasStatus.GotBLocks();
}

public bool RecheckFailed() {
    if (String.CompareOrdinal(config.customData, Me.CustomData) != 0) {
        return !Configure();
    }

    if ((tickCount++) % 2 == 0) {
        RefetchBlocks();
    }
    if (tickCount % 20 == 0) {
        log.Clear();
        tickCount = 0;
    }

    return false;
}
/* CONFIG */
