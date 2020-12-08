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

[LCD Panel]
output=
|Jump drives: {power.jumpDrives}
|{power.jumpBar:bgColour=60,60,60}
|Batteries: {power.batteries}
|{power.batteryBar:bgColour=60,60,60}
|Reactors: {power.reactors} ({power.reactorMw} MW, {power.reactorUr} Ur)
|
|Ship status: {health.status}
|{health.blocks}
|
|{production.mode}
|{production.blocks}
|
|Cargo: {cargo.stored} / {cargo.cap}
|{cargo.bar:bgColour=60,60,60}
|{cargo.items}
healthIgnore=Wheel
healthIgnoreMode=merge

[Programmable block <0>]
output=
|Jump drives: {power.jumpDrives}
|{power.jumpBar}
|Batteries: {power.batteries}
|{power.batteryBar}
|Reactors: {power.reactors} ({power.reactorMw} MW, {power.reactorUr} Ur)
|
|Ship status: {health.status}
|{health.blocks}
|
|{production.mode}
|{production.blocks}
|
|Cargo: {cargo.stored} / {cargo.cap}
|{cargo.bar}
|{cargo.items}
healthIgnore=Wheel
healthIgnoreMode=merge

[Status panel]
output=
|health
healthIgnore=
healthIgnoreMode=replace
*/

Dictionary<string, DrawingSurface> drawables = new Dictionary<string, DrawingSurface>();
List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
List<string> strings = new List<string>();
MyIni ini = new MyIni();
Template template;

// Dictionary<string, List<IMyTextSurface>> programOutputs = new Dictionary<string, List<IMyTextSurface>>();
// public string[] programKeys = { "AIRLOCK", "BLOCK_HEALTH", "CARGO", "CARGO_CAP", "CARGO_CAP_STYLE", "CARGO_LIGHT", "HEALTH_IGNORE", "INPUT", "JUMP_BAR", "POWER", "POWER_BAR", "PRODUCTION" };

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

public class Config {
    // airlock=true
    // healthIgnore=Hydrogen Thruster,Suspension
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
            template.PreRender(name, tpl.ToString());
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
    //         drawables.Add($"{((IMyTerminalBlock)block).CustomName} <{i}>", new DrawingSurface(surface, this));
    //     }
    // }
    return true;
}

public Program() {
    template = new Template(this);
    powerDetails = new PowerDetails(this, template);
    cargoStatus = new CargoStatus(this, template);
    blockHealth = new BlockHealth(this, template);
    productionDetails = new ProductionDetails(this, template);

    if (!ParseCustomData()) {
        Runtime.UpdateFrequency &= UpdateFrequency.None;
        Echo("Failed to parse custom data");
        return;
    }
    // airlocks on 10
    Runtime.UpdateFrequency = UpdateFrequency.Update100 | UpdateFrequency.Update10;

    blocks.Clear();
    GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(blocks);
    foreach (IMyTextSurfaceProvider block in blocks) {
        for (int i = 0; i < block.SurfaceCount; i++) {
            string name = ((IMyTerminalBlock)block).CustomName;
            string surfaceName = $"{name} <{i}>";
            if (!strings.Contains(name) && !strings.Contains(surfaceName)) {
                continue;
            }

            IMyTextSurface surface = block.GetSurface(i);
            drawables.Add(surfaceName, new DrawingSurface(surface, this, $"{name} <{i}>"));
            if (i == 0 && block.SurfaceCount == 1) {
                drawables.Add(name, new DrawingSurface(surface, this, name));
            }
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

    /* if should do power */
    powerDetails.Refresh();
    /* if should do cargo */
    cargoStatus.Refresh();
    /* if should do health */
    blockHealth.Refresh();
    /* if should do production */
    productionDetails.Refresh();

    foreach (var kv in drawables) {
        template.Render(kv.Value);
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
/*
 * GRAPHICS
 */
public class DrawingSurface {
    public Program program;
    public IMyTextSurface surface;
    public RectangleF viewport;
    public MySpriteDrawFrame frame;
    public Vector2 cursor;
    public Vector2 savedCursor;
    public StringBuilder sb;
    public Vector2 charSizeInPx;
    public bool drawing;
    public Vector2 padding;
    public float width;
    public float height;
    public int charsPerWidth;
    public int charsPerHeight;
    public string name;
    public Color colourGrey = new Color(40, 40, 40);

    public static char[] commaSep = { ',' };
    public static Dictionary<string, Color> stringToColour = new Dictionary<string, Color>() {
        { "black", Color.Black },
        { "blue", Color.Blue },
        { "brown", Color.Brown },
        { "cyan", Color.Cyan },
        { "dimgray", Color.DimGray },
        { "gray", Color.Gray },
        { "green", Color.Green },
        { "orange", Color.Orange },
        { "pink", Color.Pink },
        { "purple", Color.Purple },
        { "red", Color.Red },
        { "tan", Color.Tan },
        { "transparent", Color.Transparent },
        { "white", Color.White },
        { "yellow", Color.Yellow },
        { "dimgreen", Color.Darken(Color.Green, 0.4) },
        { "dimyellow", Color.Darken(Color.Yellow, 0.6) },
        { "dimorange", Color.Darken(Color.Orange, 0.2) },
        { "dimred", Color.Darken(Color.Red, 0.2) }
    };
    public static Dictionary<string, TextAlignment> stringToAlignment = new Dictionary<string, TextAlignment>() {
        { "center", TextAlignment.CENTER },
        { "left", TextAlignment.LEFT },
        { "right", TextAlignment.RIGHT }
    };

    public DrawingSurface(IMyTextSurface surface, Program program, string name = "") {
        this.program = program;
        this.surface = surface;
        this.cursor = new Vector2(0f, 0f);
        this.savedCursor = new Vector2(0f, 0f);
        this.sb = new StringBuilder(" ");
        this.charSizeInPx = new Vector2(0f, 0f);
        this.surface.ContentType = ContentType.SCRIPT;
        this.drawing = false;
        // this.surface.Font = "Monospace";
        this.viewport = new RectangleF(0f, 0f, 0f, 0f);
        this.name = name;

        this.InitScreen();
    }

    public void InitScreen() {
        this.cursor.X = 0f;
        this.cursor.Y = 0f;
        this.surface.Script = "";

        this.padding = (this.surface.TextPadding / 100) * this.surface.SurfaceSize;
        this.viewport.Position = (this.surface.TextureSize - this.surface.SurfaceSize) / 2f + this.padding;
        this.viewport.Size = this.surface.SurfaceSize - (2 * this.padding);
        this.width = this.viewport.Width;
        this.height = this.viewport.Height;

        this.Size();
    }

    public Color? GetColourOpt(string colour) {
        if (colour == "" || colour == null) {
            return null;
        }
        if (!colour.Contains(',')) {
            return DrawingSurface.stringToColour.Get("colour");
        }

        string[] numbersStr = colour.Split(DrawingSurface.commaSep);

        if (numbersStr.Length < 2) {
            return null;
        }

        int r, g, b;
        int a = 255;
        if (
            !int.TryParse(numbersStr[0], out r) ||
            !int.TryParse(numbersStr[1], out g) ||
            !int.TryParse(numbersStr[2], out b) ||
            (numbersStr.Length > 3 && !int.TryParse(numbersStr[3], out a))
        ) {
            return null;
        } else {
            return new Color(r, g, b, a);
        }
    }

    public void DrawStart() {
        this.InitScreen();
        this.drawing = true;
        this.frame = this.surface.DrawFrame();
    }

    public DrawingSurface Draw() {
        this.drawing = false;
        this.frame.Dispose();

        return this;
    }

    public DrawingSurface SaveCursor() {
        this.savedCursor = this.cursor;

        return this;
    }

    public DrawingSurface SetCursor(float? x, float? y) {
        this.cursor.X = x ?? this.cursor.X;
        this.cursor.Y = y ?? this.cursor.Y;

        return this;
    }

    public DrawingSurface Newline(bool resetX = true) {
        this.cursor.Y += this.charSizeInPx.Y;
        this.cursor.X = resetX ? 0 : this.savedCursor.X;

        return this;
    }

    public DrawingSurface Size(float? size = null) {
        this.surface.FontSize = size ?? this.surface.FontSize;

        this.charSizeInPx = this.surface.MeasureStringInPixels(this.sb, this.surface.Font, this.surface.FontSize);
        this.charsPerWidth = (int)Math.Floor(this.surface.SurfaceSize.X / this.charSizeInPx.X);
        this.charsPerHeight = (int)Math.Floor(this.surface.SurfaceSize.Y / this.charSizeInPx.Y);

        return this;
    }

    public DrawingSurface Text(string text, Dictionary<string, string> options) {
        if (options == null) {
            return this.Text(text);
        }
        Color? colour = this.GetColourOpt(options.Get("colour", null));
        TextAlignment textAlignment = DrawingSurface.stringToAlignment.Get(options.Get("align", "left"));
        float scale = Util.ParseFloat(options.Get("scale"), 1f);

        return this.Text(text, colour: colour, textAlignment: textAlignment, scale: scale);
    }

    public DrawingSurface Text(
        string text,
        Color? colour = null,
        TextAlignment textAlignment = TextAlignment.LEFT,
        float scale = 1f,
        Vector2? position = null
    ) {
        if (!this.drawing) {
            this.DrawStart();
        }
        if (colour == null) {
            colour = this.surface.ScriptForegroundColor;
        }

        Vector2 pos = (position ?? this.cursor) + this.viewport.Position;

        this.frame.Add(new MySprite() {
            Type = SpriteType.TEXT,
            Data = text,
            Position = pos,
            RotationOrScale = this.surface.FontSize * scale,
            Color = colour,
            Alignment = textAlignment,
            FontId = surface.Font
        });

        this.sb.Clear();
        this.sb.Append(text);
        Vector2 size = this.surface.MeasureStringInPixels(this.sb, this.surface.Font, this.surface.FontSize);
        this.sb.Clear();
        this.sb.Append(" ");


        this.cursor.X += size.X;

        return this;
    }

    public float ToRad(float deg) {
        return deg * ((float)Math.PI / 180f);
    }

    public Color FloatPctToColor(float pct) {
        if (pct > 0.75f) {
            return DrawingSurface.stringToColour.Get("dimgreen");
        } else if (pct > 0.5f) {
            return DrawingSurface.stringToColour.Get("dimyellow");
        } else if (pct > 0.25f) {
            return DrawingSurface.stringToColour.Get("dimorange");
        }

        return DrawingSurface.stringToColour.Get("dimred");
    }

    public float Hypo(float a, float b) {
        return (float)Math.Sqrt(Math.Pow(a, 2) + Math.Pow(b, 2));
    }

    public DrawingSurface MidBar(
        float net,
        float low,
        float high,
        float width = 0f,
        float height = 0f,
        float pad = 0.1f,
        Color? bgColour = null
    ) {
        if (!this.drawing) {
            this.DrawStart();
        }

        width = (width == 0f) ? this.width : width;
        height = (height == 0f) ? this.charSizeInPx.Y : height;
        height -= 1f;

        Vector2 pos = this.cursor + this.viewport.Position;
        pos.Y += (height / 2) + 1f;

        using (frame.Clip((int)pos.X, (int)(pos.Y - height / 2), (int)width, (int)height)) {
            this.frame.Add(new MySprite {
                Type = SpriteType.TEXTURE,
                Alignment = TextAlignment.CENTER,
                Data = "SquareSimple",
                Position = pos + new Vector2(width / 2, 0),
                Size = new Vector2((float)Math.Sqrt(Math.Pow(width, 2) / 2), width),
                Color = bgColour ?? this.colourGrey,
                RotationOrScale = this.ToRad(-45f),
            });
        }

        pad = (float)Math.Round(pad * height);
        pos.X += pad;
        width -= 2 * pad;
        height -= 2 * pad;

        Color colour = Color.Green;
        float pct = net / high;
        if (net < 0) {
            pct = net / low;
            colour = Color.Red;
        }
        float sideWidth = (float)Math.Sqrt(2) * width * pct;
        float leftClip = Math.Min((width / 2), (width / 2) + (width / 2) * pct);
        float rightClip = Math.Max((width / 2), (width / 2) + (width / 2) * pct);

        using (this.frame.Clip((int)(pos.X + leftClip), (int)(pos.Y - height / 2), (int)Math.Abs(rightClip - leftClip), (int)height)) {
            this.frame.Add(new MySprite {
                Type = SpriteType.TEXTURE,
                Alignment = TextAlignment.CENTER,
                Data = "SquareSimple",
                Position = pos + new Vector2(width / 2, 0),
                Size = new Vector2(Math.Abs(sideWidth) / 2, width),
                Color = colour,
                RotationOrScale = this.ToRad(-45f)
            });
        }

        this.frame.Add(new MySprite {
            Type = SpriteType.TEXTURE,
            Alignment = TextAlignment.CENTER,
            Data = "SquareSimple",
            Position = pos + new Vector2((width / 2), -1f),
            Size = new Vector2(2f, height + 2f),
            Color = Color.White,
            RotationOrScale = 0f
        });

        return this;
    }

    public DrawingSurface Bar(
        Dictionary<string, string> options,
        float width = 0f,
        float height = 0f,
        Color? fillColour = null,
        string text = null,
        Color? textColour = null,
        Color? bgColour = null,
        TextAlignment textAlignment = TextAlignment.LEFT,
        float pad = 0.1f
    ) {
        if (options == null) {
            return this.Bar(0f, text: "--/--");
        }

        float pct = Util.ParseFloat(options.Get("pct"));
        width = Util.ParseFloat(options.Get("width"), width);
        height = Util.ParseFloat(options.Get("height"), height);
        fillColour = this.GetColourOpt(options.Get("fillColour")) ?? fillColour;
        textAlignment = DrawingSurface.stringToAlignment.Get(options.Get("align", "null"), textAlignment);
        text = options.Get("text") ?? text;
        textColour = this.GetColourOpt(options.Get("textColour")) ?? textColour;
        bgColour = this.GetColourOpt(options.Get("bgColour")) ?? bgColour;
        pad = Util.ParseFloat(options.Get("pad"), pad);

        return this.Bar(
            pct,
            width: width,
            height: height,
            fillColour: fillColour,
            textAlignment: textAlignment,
            text: text,
            textColour: textColour,
            bgColour: bgColour,
            pad: pad
        );
    }

    public DrawingSurface Bar(
        float pct,
        float width = 0f,
        float height = 0f,
        Color? fillColour = null,
        Vector2? position = null,
        string text = null,
        Color? textColour = null,
        Color? bgColour = null,
        TextAlignment textAlignment = TextAlignment.LEFT,
        float pad = 0.1f
    ) {
        if (!this.drawing) {
            this.DrawStart();
        }

        width = (width == 0f) ? this.width : width;
        height = (height == 0f) ? this.charSizeInPx.Y : height;
        height -= 1f;

        Color fill = fillColour ?? this.FloatPctToColor(pct);
        Vector2 pos = (position ?? this.cursor) + this.viewport.Position;
        pos.Y += (height / 2) + 1f;

        using (frame.Clip((int)pos.X, (int)(pos.Y - height / 2), (int)width, (int)height)) {
            this.frame.Add(new MySprite {
                Type = SpriteType.TEXTURE,
                Alignment = TextAlignment.CENTER,
                Data = "SquareSimple",
                Position = pos + new Vector2(width / 2, 0),
                Size = new Vector2((float)Math.Sqrt(Math.Pow(width, 2) / 2), width),
                Color = bgColour ?? this.colourGrey,
                RotationOrScale = this.ToRad(-45f),
            });
        }

        pad = (float)Math.Round(pad * height);
        pos.X += pad;
        width -= 2 * pad;
        height -= 2 * pad;

        using (frame.Clip((int)pos.X, (int)(pos.Y - height / 2), (int)(width * pct), (int)height)) {
            this.frame.Add(new MySprite {
                Type = SpriteType.TEXTURE,
                Alignment = TextAlignment.CENTER,
                Data = "SquareSimple",
                Position = pos + new Vector2((width * pct) / 2, 0),
                Size = new Vector2((float)Math.Floor(Math.Sqrt(Math.Pow((width * pct), 2) / 2)), width),
                Color = fill,
                RotationOrScale = this.ToRad(-45f),
            });
        }

        if (text == null) {
            text = Util.PctString(pct);
        }
        if (text != null && text != "") {
            this.cursor.X += (width / 2);
            this.Text(text, textColour ?? Color.Black, textAlignment: TextAlignment.CENTER, scale: 0.9f);
        } else {
            this.cursor.X += (width / 2);
            this.cursor.Y += height;
        }


        return this;
    }

    public DrawingSurface TextCircle(Dictionary<string, string> options) {
        if (options == null) {
            return this.TextCircle(this.surface.FontColor);
        }

        Color colour = this.GetColourOpt(options.Get("colour")) ?? this.surface.FontColor;
        bool outline = Util.ParseBool(options.Get("outline"));

        return this.TextCircle(colour, outline);
    }

    public DrawingSurface TextCircle(Color colour, bool outline = false) {
        return this.Circle(this.charSizeInPx.Y - 5f, colour, this.cursor + Vector2.Divide(this.charSizeInPx, 2f), outline: outline);
    }

    public DrawingSurface Circle(Dictionary<string, string> options) {
        if (options == null) {
            return this.Circle(this.charSizeInPx.Y, this.surface.FontColor);
        }

        float size = Util.ParseFloat(options.Get("size"), this.charSizeInPx.Y);
        Color colour = this.GetColourOpt(options.Get("colour")) ?? this.surface.FontColor;
        bool outline = Util.ParseBool(options.Get("outline"), false);

        return this.Circle(size: size, colour: colour, outline: outline);
    }

    public DrawingSurface Circle(float size, Color colour, Vector2? position = null, bool outline = false) {
        if (!this.drawing) {
            this.DrawStart();
        }

        Vector2 pos = (position ?? this.cursor) + this.viewport.Position;

        this.frame.Add(new MySprite() {
            Type = SpriteType.TEXTURE,
            Alignment = TextAlignment.CENTER,
            Data = outline ? "CircleHollow" : "Circle",
            Position = pos,
            Size = new Vector2(size, size),
            Color = colour,
            RotationOrScale = 0f,
        });

        this.cursor.X += size;

        return this;
    }
}
/* GRAPHICS */
/*
 * POWER
 */
PowerDetails powerDetails;

public class PowerDetails {
    public Program program;
    public Template template;
    public List<IMyPowerProducer> powerProducerBlocks;
    public List<IMyJumpDrive> jumpDriveBlocks;
    public List<MyInventoryItem> items;

    public int jumpDrives;
    public float jumpMax;
    public float jumpCurrent;

    public int batteries;
    public float batteryMax;
    public float batteryCurrent;

    public int reactors;
    public float reactorOutputMW;
    public MyFixedPoint reactorUranium;

    public int solars;
    public float solarOutputMW;
    public float solarOutputMax;

    public PowerDetails(Program program, Template template = null) {
        this.program = program;
        this.template = template;
        this.powerProducerBlocks = new List<IMyPowerProducer>();
        this.jumpDriveBlocks = new List<IMyJumpDrive>();
        this.items = new List<MyInventoryItem>();
        this.jumpDrives = 0;
        this.jumpMax = 0f;
        this.jumpCurrent = 0f;
        this.batteries = 0;
        this.batteryMax = 0f;
        this.batteryCurrent = 0f;
        this.reactors = 0;
        this.reactorOutputMW = 0f;
        this.reactorUranium = 0;
        this.solars = 0;
        this.solarOutputMW = 0f;
        this.solarOutputMax = 0f;
        this.GetBlocks();
        this.RegisterTemplateVars();
    }

    public void RegisterTemplateVars() {
        if (this.template == null) {
            return;
        }
        this.template.Register("power.jumpDrives", () => this.jumpDrives.ToString());
        this.template.Register("power.jumpBar",
            (DrawingSurface ds, string text, Dictionary<string, string> options) => {
                float pct = this.GetPercent(this.jumpCurrent, this.jumpMax);
                ds.Bar(pct, text: Util.PctString(pct));
            });
        this.template.Register("power.batteries", () => this.batteries.ToString());
        this.template.Register("power.batteryBar",
            (DrawingSurface ds, string text, Dictionary<string, string> options) => {
                float pct = this.GetPercent(this.batteryCurrent, this.batteryMax);
                ds.Bar(pct, text: Util.PctString(pct));
            });
        this.template.Register("power.solars", () => this.solars.ToString());
        this.template.Register("power.reactors", () => this.reactors.ToString());
        this.template.Register("power.reactorMw", () => this.reactorOutputMW.ToString());
        this.template.Register("power.reactorUr", () => $"{Util.FormatNumber(reactorUranium)} kg");
    }

    public void Clear() {
        this.jumpDrives = 0;
        this.jumpMax = 0f;
        this.jumpCurrent = 0f;
        this.batteries = 0;
        this.batteryMax = 0f;
        this.batteryCurrent = 0f;
        this.reactors = 0;
        this.reactorOutputMW = 0f;
        this.reactorUranium = 0;
        this.solars = 0;
        this.solarOutputMW = 0f;
        this.solarOutputMax = 0f;
    }

    public void GetBlocks() {
        this.powerProducerBlocks.Clear();
        this.program.GridTerminalSystem.GetBlocksOfType<IMyPowerProducer>(this.powerProducerBlocks, b => b.IsSameConstructAs(this.program.Me));
        this.jumpDriveBlocks.Clear();
        this.program.GridTerminalSystem.GetBlocksOfType<IMyJumpDrive>(this.jumpDriveBlocks, b => b.IsSameConstructAs(this.program.Me));
    }

    public float GetPercent(float current, float max) {
        if (max == 0) {
            return 0f;
        }
        return current / max;
    }

    public void Refresh() {
        Clear();

        foreach (IMyPowerProducer powerBlock in this.powerProducerBlocks) {
            if (powerBlock is IMyBatteryBlock) {
                this.batteries += 1;
                this.batteryCurrent += ((IMyBatteryBlock)powerBlock).CurrentStoredPower;
                this.batteryMax += ((IMyBatteryBlock)powerBlock).MaxStoredPower;
            } else if (powerBlock is IMyReactor) {
                this.reactors += 1;
                this.reactorOutputMW += ((IMyReactor)powerBlock).CurrentOutput;

                this.items.Clear();
                var inv = ((IMyReactor)powerBlock).GetInventory(0);
                inv.GetItems(this.items);
                for (var i = 0; i < items.Count; i++) {
                    this.reactorUranium += items[i].Amount;
                }
            } else if (powerBlock is IMySolarPanel) {
                this.solars += 1;
                this.solarOutputMW += ((IMySolarPanel)powerBlock).CurrentOutput;
                this.solarOutputMax += ((IMySolarPanel)powerBlock).MaxOutput;
            }
        }

        foreach (IMyJumpDrive jumpDrive in jumpDriveBlocks) {
            this.jumpDrives += 1;
            this.jumpCurrent += jumpDrive.CurrentStoredPower;
            this.jumpMax += jumpDrive.MaxStoredPower;
        }
    }

    public override string ToString() {
        return
            $"{this.jumpDrives} Jump drive{Util.Plural(this.jumpDrives, "", "s")}:\n" +
            $"{this.jumpCurrent} / {this.jumpMax}\n" +
            $"{this.batteries} Batter{Util.Plural(this.batteries, "y", "ies")}\n" +
            $"{this.batteryCurrent} / {this.batteryMax}\n" +
            $"{this.reactors} Reactor{Util.Plural(this.reactors, "", "s")}\n" +
            $"{this.reactorOutputMW} MW, {Util.FormatNumber(this.reactorUranium)} Fuel";
    }
}
/* POWER */
public class Template {
    public class Token {
        public bool isText = true;
        public string value = null;
    }

    public class Node {
        public string action;
        public string text;
        public Dictionary<string, string> options;

        public Node(string action, string text = null, Dictionary<string, string> options = null) {
            this.action = action;
            this.text = text;
            this.options = options;
        }
    }

    public delegate void DsCallback(DrawingSurface ds, string token, Dictionary<string, string> options);
    public delegate string TextCallback();

    public Program program;
    public System.Text.RegularExpressions.Regex tokenizer;
    public System.Text.RegularExpressions.Regex cmdSplitter;
    public System.Text.RegularExpressions.Match match;
    public Token token;
    public Dictionary<string, DsCallback> methods;
    public Dictionary<string, List<Node>> renderNodes;

    public char[] splitSemi = new[] { ';' };

    public Template(Program program = null) {
        this.tokenizer = Util.Regex(@"(\{[^\}]+\}|[^\{]+)", System.Text.RegularExpressions.RegexOptions.Compiled);
        this.cmdSplitter = Util.Regex(@"(?<name>[^:; ]+)(:(?<params>[^ ]+)?;?)? ?(?<text>.*)?", System.Text.RegularExpressions.RegexOptions.Compiled);
        this.token = new Token();
        this.methods = new Dictionary<string, DsCallback>();
        this.program = program;
        this.renderNodes = new Dictionary<string, List<Node>>();

        this.Register("text", this.RenderText);
        this.Register("textCircle", (DrawingSurface ds, string text, Dictionary<string, string> options) => ds.TextCircle(options));
        this.Register("circle", (DrawingSurface ds, string text, Dictionary<string, string> options) => ds.Circle(options));
        this.Register("bar", (DrawingSurface ds, string text, Dictionary<string, string> options) => ds.Bar(options));
    }

    public void Register(string key, DsCallback callback) {
        this.methods[key] = callback;
    }

    public void Register(string key, TextCallback callback) {
        this.methods[key] = (DrawingSurface ds, string text, Dictionary<string, string> options) => ds.Text(callback(), options);
    }

    public void RenderText(DrawingSurface ds, string text, Dictionary<string, string> options) {
        ds.Text(text, options);
    }

    public void PreRender(string outputName, string templateStrings) {
        this.PreRender(outputName, templateStrings.Split('\n'));
    }

    public void PreRender(string outputName, string[] templateStrings) {
        List<Node> nodeList = new List<Node>();

        foreach (string line in templateStrings) {
            this.match = null;

            while (this.GetToken(line)) {
                if (this.token.isText) {
                    nodeList.Add(new Node("text", this.token.value));
                    continue;
                }

                System.Text.RegularExpressions.Match m = this.cmdSplitter.Match(this.token.value);
                if (m.Success) {
                    nodeList.Add(new Node(m.Groups["name"].Value, m.Groups["text"].Value, this.StringToDict(m.Groups["params"].Value)));
                } else {
                    nodeList.Add(new Node(this.token.value, options: this.StringToDict()));
                }
            }
            nodeList.Add(new Node("newline"));
        }

        this.renderNodes.Add(outputName, nodeList);
    }

    public void Render(DrawingSurface ds, string name = null) {
        string dsName = name ?? ds.name;
        List<Node> nodeList = null;
        if (!this.renderNodes.TryGetValue(dsName, out nodeList)) {
            ds.Text("No template found").Draw();
            return;
        }

        DsCallback callback = null;
        foreach (Node node in nodeList) {
            if (node.action == "newline") {
                ds.Newline();
                continue;
            }

            if (this.methods.TryGetValue(node.action, out callback)) {
                callback(ds, node.text, node.options);
            } else {
                ds.Text($"{{{node.action}}}");
            }
            callback = null;
        }

        ds.Draw();
    }

    public Dictionary<string, string> StringToDict(string options = "") {
        if (options == "") {
            return new Dictionary<string, string>();
        }

        return options.Split(splitSemi, StringSplitOptions.RemoveEmptyEntries)
            .Select(value => value.Split('='))
            .ToDictionary(pair => pair[0], pair => pair[1]);
    }

    public void Echo(string text) {
        if (this.program != null) {
            this.program.Echo(text);
        }
    }

    public bool GetToken(string line) {
        if (this.match == null) {
            this.match = this.tokenizer.Match(line);
        } else {
            this.match = this.match.NextMatch();
        }

        if (this.match.Success) {
            try {
                string _token = this.match.Groups[1].Value;
                if (_token[0] == '{') {
                    this.token.value = _token.Substring(1, _token.Length - 2);
                    this.token.isText = false;
                } else {
                    this.token.value = _token;
                    this.token.isText = true;
                }
            } catch (Exception e) {
                this.Echo($"err parsing token {e}");
                return false;
            }

            return true;
        } else {
            return false;
        }
    }
}
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
/*
 * CARGO
 */
CargoStatus cargoStatus;

public class CargoStatus {
    public Program program;
    public List<IMyTerminalBlock> cargo;
    public Dictionary<string, VRage.MyFixedPoint> cargoItemCounts;
    public List<MyInventoryItem> inventoryItems;
    public System.Text.RegularExpressions.Regex itemRegex;
    public System.Text.RegularExpressions.Regex ingotRegex;
    public System.Text.RegularExpressions.Regex oreRegex;
    public VRage.MyFixedPoint max;
    public VRage.MyFixedPoint vol;
    public Template template;
    public List<float> widths;

    public string itemText;
    public float pct;

    public CargoStatus(Program program, Template template = null) {
        this.program = program;
        this.template = template;
        this.itemText = "";
        this.pct = 0f;
        this.cargo = new List<IMyTerminalBlock>();
        this.cargoItemCounts = new Dictionary<string, VRage.MyFixedPoint>();
        this.inventoryItems = new List<MyInventoryItem>();
        this.itemRegex = Util.Regex(".*/");
        this.ingotRegex = Util.Regex("Ingot/");
        this.oreRegex = Util.Regex("Ore/(?!Ice)");
        this.widths = new List<float>() { 0, 0, 0, 0 };
        this.GetCargoBlocks();
        this.RegisterTemplateVars();
    }

    public void RegisterTemplateVars() {
        if (this.template == null) {
            return;
        }

        this.template.Register("cargo.stored", () => $"{Util.FormatNumber(1000 * this.vol)} L");
        this.template.Register("cargo.cap", () => $"{Util.FormatNumber(1000 * this.max)} L");
        this.template.Register("cargo.bar", this.RenderPct);
        this.template.Register("cargo.items", this.RenderItems);
    }

    public void RenderPct(DrawingSurface ds, string text, Dictionary<string, string> options) {
        Color colour = DrawingSurface.stringToColour.Get("dimgreen");
        if (this.pct > 60) {
            colour = DrawingSurface.stringToColour.Get("dimyellow");
        } else if (this.pct > 85) {
            colour = DrawingSurface.stringToColour.Get("dimred");
        }
        ds.Bar(this.pct, fillColour: colour, text: Util.PctString(this.pct));
    }

    public void RenderItems(DrawingSurface ds, string text, Dictionary<string, string> options) {
        if (ds.width / (ds.charSizeInPx.X + 1f) < 30) {
            foreach (var item in this.cargoItemCounts) {
                var fmtd = Util.FormatNumber(item.Value);
                ds.Text($"{item.Key}").SetCursor(ds.width, null).Text(fmtd, textAlignment: TextAlignment.RIGHT).Newline();
            }
        } else {
            this.widths[0] = 0;
            this.widths[1] = ds.width / 2 - ds.charSizeInPx.X;
            this.widths[2] = ds.width / 2 + ds.charSizeInPx.X;
            this.widths[3] = ds.width;

            int i = 0;
            foreach (var item in this.cargoItemCounts) {
                var fmtd = Util.FormatNumber(item.Value);
                ds
                    .SetCursor(this.widths[(i++ % 4)], null)
                    .Text($"{item.Key}")
                    .SetCursor(this.widths[(i++ % 4)], null)
                    .Text(fmtd, textAlignment: TextAlignment.RIGHT);

                if ((i % 4) == 0) {
                    ds.Newline();
                }
            }
        }
    }

// public Func<TResult> MethodAccess<TResult, TArg> (Func<TArg, TResult> func, TArg arg) {
//     return () => func(arg);
// }

    public void Clear() {
        this.itemText = "";
        this.pct = 0f;
        this.cargoItemCounts.Clear();
        this.inventoryItems.Clear();
    }

    public void GetCargoBlocks() {
        this.cargo.Clear();
        this.program.GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(this.cargo, c =>
            c.IsSameConstructAs(this.program.Me) &&
            (c is IMyCargoContainer || c is IMyShipDrill || c is IMyShipConnector)
            // (c is IMyCargoContainer || c is IMyShipDrill || c is IMyShipConnector || c is IMyShipWelder || c is IMyShipGrinder)
        );
    }

    public void Refresh() {
        this.Clear();

        this.max = 0;
        this.vol = 0;

        foreach (var c in this.cargo) {
            var inv = c.GetInventory(0);
            this.vol += inv.CurrentVolume;
            this.max += inv.MaxVolume;

            this.inventoryItems.Clear();
            inv.GetItems(this.inventoryItems);
            for (var i = 0; i < this.inventoryItems.Count; i++) {
                string fullName = this.inventoryItems[i].Type.ToString();
                string itemName = this.itemRegex.Replace(fullName, "");
                if (this.ingotRegex.IsMatch(fullName)) {
                    itemName += " Ingot";
                } else if (this.oreRegex.IsMatch(fullName)) {
                    itemName += " Ore";
                }

                var itemQty = this.inventoryItems[i].Amount;
                if (!this.cargoItemCounts.ContainsKey(itemName)) {
                    this.cargoItemCounts.Add(itemName, itemQty);
                } else {
                    this.cargoItemCounts[itemName] = this.cargoItemCounts[itemName] + itemQty;
                }
            }
        }

        this.pct = 0f;
        if (max != 0) {
            this.pct = (float)this.vol / (float)this.max;
        }
        // if (settings[CFG.CARGO_LIGHT] != "") {
        //     IMyLightingBlock light = (IMyLightingBlock)GetBlockWithName(settings[CFG.CARGO_LIGHT]);

        //     if (light != null && light is IMyLightingBlock) {
        //         if (pct > 0.98f) {
        //             light.Color = Color.Red;
        //         } else if (pct > 0.90f) {
        //             light.Color = Color.Yellow;
        //         } else {
        //             light.Color = Color.White;
        //         }
        //     }
        // }

        // string itemText = "";
        // int chars;
        // GetPanelWidthInChars(settings[CFG.CARGO], out chars);

        // int itemIndex = 0;
        // int doubleColumn = 60;
        // foreach (var item in cargoItemCounts) {
        //     var fmtd = Util.FormatNumber(item.Value);
        //     int maxChars = chars;
        //     if (chars > doubleColumn) {
        //         maxChars = (chars - 4) / 2;
        //     }
        //     var padLen = (int)(maxChars - item.Key.ToString().Length - fmtd.Length);
        //     string spacing = (padLen >= 0 ? "".PadRight(padLen, LINE_SPACER) : "\n  ");
        //     itemText += String.Format("{0}{1}{2}", item.Key, spacing, fmtd);
        //     if (chars <= doubleColumn || itemIndex % 2 != 0) {
        //         itemText += '\n';
        //     } else if (chars > doubleColumn) {
        //         itemText += "   ";
        //     }
        //     itemIndex++;
        // }

        // itemText = itemText;

        return;
    }

    public override string ToString() {
        string itemText = $"{pct}%";
        foreach (var item in this.cargoItemCounts) {
            var fmtd = Util.FormatNumber(item.Value);
            itemText += $"{item.Key}:{fmtd},";
            this.program.Echo($"{item.Key}:{fmtd},");
        }

        return itemText;
    }

    public void Draw(IMyTextSurface surface) {
        //todo
    }
}
/* CARGO */
/*
 * PRODUCTION
 */
public ProductionDetails productionDetails;

public class ProductionDetails {
    public Program program;
    public Template template;
    public List<MyProductionItem> productionItems;
    public List<ProductionBlock> productionBlocks;
    public List<IMyProductionBlock> blocks;
    public Dictionary<ProductionBlock, string> blockStatus;
    public Dictionary<string, Color> statusDot;
    public double productionCheckFreqMs = 2 * 60 * 1000;
    public double productionOnWaitMs = 5 * 1000;
    public double productionOutTimeMs = 3 * 1000;
    public string productionIgnoreString = "[x]";
    public string status;
    public StringBuilder queueBuilder;
    public double idleTime = 0;
    public double timeDisabled = 0;
    public bool checking = false;
    public double lastCheck = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
    public char[] splitNeline;

    public ProductionDetails(Program program, Template template) {
        this.program = program;
        this.template = template;
        this.blocks = new List<IMyProductionBlock>();
        this.productionItems = new List<MyProductionItem>();
        this.productionBlocks = new List<ProductionBlock>();
        this.blockStatus = new Dictionary<ProductionBlock, string>();
        this.statusDot = new Dictionary<string, Color>() {
            { "Idle", DrawingSurface.stringToColour.Get("dimgreen") },
            { "Working", DrawingSurface.stringToColour.Get("dimyellow") },
            { "Blocked", DrawingSurface.stringToColour.Get("dimred") }
        };
        this.queueBuilder = new StringBuilder();
        this.splitNeline = new[] { '\n' };

        this.GetProductionBlocks();
        this.RegisterTemplateVars();
    }

    public void RegisterTemplateVars() {
        this.template.Register("production.status", () => this.status);
        this.template.Register("production.blocks",  (DrawingSurface ds, string text, Dictionary<string, string> options) => {
            foreach (KeyValuePair<ProductionBlock, string> blk in this.blockStatus) {
                string status = blk.Key.Status();
                string blockName = $"{blk.Key.block.CustomName}: {status} {(blk.Key.IsIdle() ? blk.Key.IdleTime() : "")}";
                ds.TextCircle(this.statusDot.Get(status)).Text(blockName).Newline();

                foreach (string str in blk.Value.Split(this.splitNeline, StringSplitOptions.RemoveEmptyEntries)) {
                    ds.Text(str).Newline();
                }
            }
        });
    }

    public void GetProductionBlocks() {
        this.blocks.Clear();
        this.program.GridTerminalSystem.GetBlocksOfType<IMyProductionBlock>(this.blocks, b =>
            b.IsSameConstructAs(this.program.Me) &&
            (b is IMyAssembler || b is IMyRefinery) &&
            !b.CustomName.Contains(this.productionIgnoreString)
        );
        this.productionBlocks.Clear();
        foreach (IMyProductionBlock block in this.blocks) {
            this.productionBlocks.Add(new ProductionBlock(this.program, block));
        }
        this.productionBlocks = this.productionBlocks.OrderBy(b => b.block.CustomName).ToList();
    }

    public void Refresh() {
        if (!this.productionBlocks.Any()) {
            return;
        }

        this.blockStatus.Clear();
        bool allIdle = true;
        this.status = "";
        int assemblers = 0;
        int refineries = 0;
        foreach (var block in this.productionBlocks) {
            bool idle = block.IsIdle();
            if (block.block.DefinitionDisplayNameText.ToString() != "Survival kit") {
                allIdle = allIdle && idle;
            }
            if (idle) {
                if (block.block is IMyAssembler) {
                    assemblers++;
                } else {
                    refineries++;
                }
            }
        }
        double timeNow = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

        if (allIdle) {
            idleTime = (idleTime == 0 ? timeNow : idleTime);

            if (timeDisabled == 0) {
                foreach (var block in this.productionBlocks) {
                    block.Enabled = false;
                }
                timeDisabled = timeNow;
            } else {
                if (!checking) {
                    if (timeNow - lastCheck > this.productionCheckFreqMs)  {
                        // We disabled them over this.productionCheckFreqMs ago, and need to check them
                        foreach (var block in this.productionBlocks) {
                            block.Enabled = true;
                        }
                        checking = true;
                        lastCheck = timeNow;
                        this.status = $"Power saving mode {Util.TimeFormat(timeNow - idleTime)} (checking)";
                    }
                } else {
                    if (timeNow - lastCheck > this.productionOnWaitMs) {
                        // We waited 5 seconds and they are still not producing
                        foreach (var block in this.productionBlocks) {
                            block.Enabled = false;
                        }
                        checking = false;
                        lastCheck = timeNow;
                    } else {
                        this.status = $"Power saving mode {Util.TimeFormat(timeNow - idleTime)} (checking)";
                    }
                }
            }
            if (this.status == "") {
                this.status = $"Power saving mode {Util.TimeFormat(timeNow - idleTime)} " +
                    $"(check in {Util.TimeFormat(this.productionCheckFreqMs - (timeNow - lastCheck), true)})";
            }
        } else {
            if (this.productionBlocks.Where(b => b.Status() == "Blocked").Any()) {
                this.status = "Production Enabled (Halted)";
            } else {
                this.status = "Production Enabled";
            }

            // If any assemblers are on, make sure they are all on (master/slave)
            if (assemblers > 0) {
                foreach (var block in this.productionBlocks.Where(b => b.block is IMyAssembler).ToList()) {
                    block.Enabled = true;
                }
            }

            idleTime = 0;
            timeDisabled = 0;
            checking = false;
        }

        foreach (var block in this.productionBlocks) {
            this.queueBuilder.Clear();
            // output += String.Format("{0}: {1} {2}\n", block.block.CustomName, block.Status(), (idle ? block.IdleTime() : ""));
            if (!block.IsIdle()) {
                block.GetQueue(this.productionItems);
                foreach (MyProductionItem i in this.productionItems) {
                    this.queueBuilder.Append($"  {Util.FormatNumber(i.Amount)} x {Util.ToItemName(i)}\n");
                }
            }

            this.blockStatus.Add(block, this.queueBuilder.ToString());
        }

        return;
    }
}

public class ProductionBlock {
    public Program program;
    public double idleTime;
    public IMyProductionBlock block;
    public bool Enabled {
        get { return block.Enabled; }
        set {
            if (block.DefinitionDisplayNameText.ToString() == "Survival kit") {
                return;
            }
            block.Enabled = value;
        }
    }

    public ProductionBlock(Program program, IMyProductionBlock block) {
        this.idleTime = -1;
        this.block = block;
        this.program = program;
    }

    public void GetQueue(List<MyProductionItem> productionItems) {
        productionItems.Clear();
        block.GetQueue(productionItems);
    }

    public bool IsIdle() {
        string status = this.Status();
        if (status == "Idle") {
            this.idleTime = (this.idleTime == -1) ? this.Now() : this.idleTime;
            return true;
        } else if (status == "Blocked" && !block.Enabled) {
            block.Enabled = true;
        }
        this.idleTime = -1;
        return false;
    }

    public string IdleTime() {
        return Util.TimeFormat(this.Now() - this.idleTime);
    }

    public string Status() {
        if (this.block.IsQueueEmpty && !this.block.IsProducing) {
            return "Idle";
        } else if (this.block.IsProducing) {
            return "Working";
        } else if (!this.block.IsQueueEmpty && !this.block.IsProducing) {
            return "Blocked";
        }
        return "???";
    }

    public double Now() {
        return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
    }
}
/* PRODUCTION */
/*
 * UTIL
 */
public static class Util {
    public static System.Text.RegularExpressions.Regex surfaceExtractor =
        Util.Regex(@"\s<(\d+)>$", System.Text.RegularExpressions.RegexOptions.Compiled);

    public static string FormatNumber(VRage.MyFixedPoint input) {
        string fmt;
        int n = Math.Max(0, (int)input);
        if (n == 0) {
            return "0";
        }
        if (n < 10000) {
            fmt = "##";
        } else if (n < 1000000) {
            fmt = "###0,K";
        } else {
            fmt = "###0,,M";
        }
        return n.ToString(fmt, System.Globalization.CultureInfo.InvariantCulture);
    }

    public static string TimeFormat(double ms, bool s = false) {
        TimeSpan t = TimeSpan.FromMilliseconds(ms);
        if (t.Hours != 0) {
            return String.Format("{0:D}h{1:D}m", t.Hours, t.Minutes);
        } else if (t.Minutes != 0) {
            return String.Format("{0:D}m", t.Minutes);
        } else {
            return (s ? String.Format("{0:D}s", t.Seconds) : "< 1m");
        }
    }

    public static string ToItemName(MyProductionItem i) {
        string id = i.BlueprintId.ToString();
        if (id.Contains('/')) {
            return id.Split('/')[1];
        }
        return id;
    }

    public static string PctString(float val) {
        return String.Format("{0,3:0}%", 100 * val);
    }

    public static System.Text.RegularExpressions.Regex Regex(
        string pattern,
        System.Text.RegularExpressions.RegexOptions opts = System.Text.RegularExpressions.RegexOptions.None
    ) {
        return new System.Text.RegularExpressions.Regex(pattern, opts);
    }

    public static string Plural(int count, string ifOne, string otherwise) {
        return count == 1 ? ifOne : otherwise;
    }

    public static int ParseInt(string str, int defaultValue = 0) {
        int output;
        if (!int.TryParse(str, out output)) {
            output = defaultValue;
        }

        return output;
    }

    public static float ParseFloat(string str, float defaultValue = 0f) {
        float output;
        if (!float.TryParse(str, out output)) {
            output = defaultValue;
        }

        return output;
    }

    public static bool ParseBool(string str, bool defaultValue = false) {
        bool output;
        if (!bool.TryParse(str, out output)) {
            output = defaultValue;
        }

        return output;
    }
}
}

public static class Dict {
    public static TValue Get<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue defaultValue = default(TValue)) {
        TValue value;
        return dict.TryGetValue(key, out value) ? value : defaultValue;
    }

    public static string Print<TKey, TValue>(this Dictionary<TKey, TValue> dict) {
        StringBuilder sb = new StringBuilder("{ ");
        foreach (KeyValuePair<TKey, TValue> keyValues in dict) {
            sb.Append($"{keyValues.Key}: {keyValues.Value}, ");
        }

        return sb.Append("}").ToString();
    }
/* UTIL */
