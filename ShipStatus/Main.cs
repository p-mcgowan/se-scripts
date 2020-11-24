/*
; User config - place in custom data
;
; for surface selection, use 'name <number>' eg: 'Cockpit <1>'

[BLOCK_HEALTH]
enabled=false
output=
|Text panel
|Text panel 2

[POWER]
enabled=true
output=Control Seat <0>

[PRODUCTION]
enabled=false
output=Text panel Production

[CARGO]
enabled=false
output=Text panel Cargo

[CARGO_CAP]
enabled=false
output=Control Seat <2>

[CARGO_CAP_STYLE]
enabled=false
output=small

[CARGO_LIGHT]
enabled=false
output=Spotlight 2

[INPUT]
enabled=false
output=Corner LCD

[POWER_BAR]
enabled=false
output=Control Seat <1>

[JUMP_BAR]
enabled=false
output=Jump panel

[AIRLOCK]
enabled=false
time_open=750
door_exclude=Hangar

[HEALTH_IGNORE]
enabled=false
blocks=
|Hydrogen Thruster
|Suspension
*/

List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
Dictionary<string, List<IMyTextSurface>> programOutputs = new Dictionary<string, List<IMyTextSurface>>();
MyIni ini = new MyIni();
public string[] programKeys = { "AIRLOCK", "BLOCK_HEALTH", "CARGO", "CARGO_CAP", "CARGO_CAP_STYLE", "CARGO_LIGHT", "HEALTH_IGNORE", "INPUT", "JUMP_BAR", "POWER", "POWER_BAR", "PRODUCTION" };

public class Panel {
    public string name;
    public int surfaceId;

    public Panel(string _name, int _surfaceId = 0) {
        name = _name;
        surfaceId = _surfaceId;
    }
}

public void ParsePanelConfig(string input, ref Panel panel) {
    var matches = Util.pnameSplitter.Matches(input);
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

    blocks.Clear();
    GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(blocks, b => b.IsSameConstructAs(Me));
    Dictionary<string, IMyTextSurfaceProvider> blockHash = new Dictionary<string, IMyTextSurfaceProvider>();

    foreach (IMyTextSurfaceProvider block in blocks) {
        blockHash.Add(((IMyTerminalBlock)block).CustomName, block);
    }
    Panel panel = new Panel("meh");

    foreach (string key in programKeys) {
        var value = ini.Get(key, "enabled").ToBoolean();
        if (ini.Get(key, "enabled").ToBoolean()) {
            string outputs = ini.Get(key, "output").ToString();
            if (outputs != "") {
                List<IMyTextSurface> surfaces = new List<IMyTextSurface>();

                // split on newlines, fetch surfaces, find in blokcs and add to list
                foreach (string outname in outputs.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)) {
                    ParsePanelConfig(outname, ref panel);
                    if (blockHash.ContainsKey(panel.name)) {
                        surfaces.Add(blockHash[panel.name].GetSurface(panel.surfaceId));
                    }
                }

                programOutputs.Add(key, surfaces);
            }
        }
    }

    // var items = sx.Split(new[] { '\n', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Split(new[] { '=' }));
    return true;
}

public Program() {
    // cargo = new List<IMyTerminalBlock>();
    // items = new List<MyInventoryItem>();
    // CargoStatus cargoStatus = new CargoStatus(this);
    powerDetails = new PowerDetails(this);
    cargoStatus = new CargoStatus(this);
    // Runtime.UpdateFrequency = UpdateFrequency.Update100;
    // // Runtime.UpdateFrequency = UpdateFrequency.Update100 | UpdateFrequency.Update10;
    if (!ParseCustomData()) {
        Runtime.UpdateFrequency = UpdateFrequency.None;
        Echo("Failed to parse custom data");
        return;
    }
}

public void Main(string argument, UpdateType updateSource) {
    Echo($"updateSource: {updateSource}");
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
    Echo(power);
    Echo(cargo);

    blocks.Clear();
    GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(blocks);
    foreach (IMyTextSurfaceProvider block in blocks) {
        for (int i = 0; i < block.SurfaceCount; i++) {
            WriteTextToSurface(block.GetSurface(i), cargo);
        }
    }
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

public void WriteTextToSurface(IMyTextSurface surface, string text /*Drawable drawable*/) {
    if (surface.ContentType == ContentType.NONE) {
        surface.ContentType = ContentType.SCRIPT;
    }
    surface.Script = "";
    surface.Font = "Monospace";

    RectangleF viewport = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 2f, surface.SurfaceSize);

    using (MySpriteDrawFrame frame = surface.DrawFrame()) {
        Vector2 position = new Vector2(0, 0) + viewport.Position;
        // CharInfo chars = CharsPerWidth(surface);

        MySprite sprite;
        sprite = new MySprite() {
            Type = SpriteType.TEXT,
            Data = text,
            Position = position,
            RotationOrScale = surface.FontSize,
            Color = surface.FontColor,
            Alignment = TextAlignment.LEFT,
            FontId = surface.Font
        };
        frame.Add(sprite);
        // foreach (var toDraw in drawable.lines) {
        //     if (toDraw.Key == DrawableType.TEXT || toDraw.Key == DrawableType.SPLIT) {
        //         string text = toDraw.Value[0];
        //         if (toDraw.Key == DrawableType.SPLIT) {
        //             //
        //         }


        //         //
        //     } else if (toDraw.Key == DrawableType.BAR) {
        //     } else if (toDraw.Key == DrawableType.SPLIT) {
        //         //
        //     }
        //     frame.Add(sprite);
        // }
    }
}
/* MAIN */

