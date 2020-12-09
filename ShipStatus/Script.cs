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
|{?power.jumpBar}
|Batteries: {power.batteries}
|{power.batteryBar}
|Solar panels: {power.solars}
|Energy IO: {power.io}
|{?power.ioBar}
|Reactors: {power.reactors} {power.reactorMw:: MW} {power.reactorUr:: Ur}
|
|Ship status: {health.status}
|{health.blocks}
|{production.status}
|{production.blocks}
|
|Cargo: {cargo.stored} / {cargo.cap}
|{cargo.bar}
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

        if (this.program.config.Enabled("health")) {
            this.GetBlocks();
            this.RegisterTemplateVars();

            string ignore = this.program.config.Get("healthIgnore");
            if (ignore != "" && ignore != null) {
                this.ignoreHealth = Util.Regex(System.Text.RegularExpressions.Regex.Replace(ignore, @"\s*,\s*", "|"));
            }
        }
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
        bool showOnHud = this.program.config.Enabled("healthOnHud");

        foreach (var b in this.blocks) {
            if (this.ignoreHealth != null && this.ignoreHealth.IsMatch(b.CustomName)) {
                continue;
            }

            var health = this.GetHealth(b);
            if (health != 1f) {
                this.damaged[b.CustomName] = Util.PctString(health);
            }
            if (showOnHud) {
                b.ShowOnHUD = health != 1f;
            }
        }

        this.status = $"{(this.damaged.Count == 0 ? "No damage" : "Damage")} detected";
    }
}
/* BLOCK_HEALTH */
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
            return DrawingSurface.stringToColour.Get(colour);
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
        if (text == "" || text == null) {
            return this;
        }

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

    public DrawingSurface MidBar(Dictionary<string, string> options) {
        float net = Util.ParseFloat(options.Get("net"), 0f);
        float low = Util.ParseFloat(options.Get("low"), 1f);
        float high = Util.ParseFloat(options.Get("high"), 1f);
        float width = Util.ParseFloat(options.Get("width"), 0f);
        float height = Util.ParseFloat(options.Get("height"), 0f);
        float pad = Util.ParseFloat(options.Get("pad"), 0.1f);
        Color? bgColour = this.GetColourOpt(options.Get("colour", null));

        return this.MidBar(net: net, low: low, high: high, width: width, height: height, pad: pad, bgColour: bgColour);
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

        width = (width == 0f) ? this.width - this.cursor.X : width;
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
        float pct = net / (high == 0f ? 1f : high);
        if (net < 0) {
            pct = net / (low == 0f ? 1f : low);
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
        if (options == null || options.Get("pct", null) == null) {
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

        width = (width == 0f) ? this.width - this.cursor.X : width;
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
            this.options = options ?? new Dictionary<string, string>();
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
    public Dictionary<string, bool> templateVars;

    public char[] splitSemi = new[] { ';' };
    public char[] splitDot = new[] { '.' };
    public string[] splitLine = new[] { "\r\n", "\r", "\n" };

    public Template(Program program = null) {
        this.tokenizer = Util.Regex(@"(\{[^\}]+\}|[^\{]+)", System.Text.RegularExpressions.RegexOptions.Compiled);
        this.cmdSplitter = Util.Regex(@"(?<newline>\?)?(?<name>[^:]+)(:(?<params>[^:]*))?(:(?<text>.+))?", System.Text.RegularExpressions.RegexOptions.Compiled);
        this.token = new Token();
        this.methods = new Dictionary<string, DsCallback>();
        this.program = program;
        this.renderNodes = new Dictionary<string, List<Node>>();
        this.templateVars = new Dictionary<string, bool>();

        this.Register("text", this.RenderText);
        this.Register("textCircle", (DrawingSurface ds, string text, Dictionary<string, string> options) => ds.TextCircle(options));
        this.Register("circle", (DrawingSurface ds, string text, Dictionary<string, string> options) => ds.Circle(options));
        this.Register("bar", (DrawingSurface ds, string text, Dictionary<string, string> options) => ds.Bar(options));
        this.Register("midBar", (DrawingSurface ds, string text, Dictionary<string, string> options) => ds.MidBar(options));
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

    public Dictionary<string, bool> PreRender(string outputName, string templateStrings) {
        return this.PreRender(outputName, templateStrings.Split(splitLine, StringSplitOptions.None));
    }

    public Dictionary<string, bool> PreRender(string outputName, string[] templateStrings) {
        this.templateVars.Clear();
        List<Node> nodeList = new List<Node>();

        bool autoNewline;
        foreach (string line in templateStrings) {
            autoNewline = true;
            this.match = null;

            while (this.GetToken(line)) {
                if (this.token.isText) {
                    nodeList.Add(new Node("text", this.token.value));
                    continue;
                }

                System.Text.RegularExpressions.Match m = this.cmdSplitter.Match(this.token.value);
                if (m.Success) {
                    var opts = this.StringToDict(m.Groups["params"].Value);
                    if (m.Groups["newline"].Value != "") {
                        opts.Set("noNewline", "true");
                        autoNewline = false;
                    }
                    opts.Set("text", m.Groups["text"].Value);
                    this.AddTemplateTokens(m.Groups["name"].Value);
                    nodeList.Add(new Node(m.Groups["name"].Value, m.Groups["text"].Value, opts));
                } else {
                    this.AddTemplateTokens(this.token.value);
                    nodeList.Add(new Node(this.token.value));
                }
            }

            if (autoNewline) {
                nodeList.Add(new Node("newline"));
            }
        }

        this.renderNodes.Add(outputName, nodeList);

        return this.templateVars;
    }

    public void AddTemplateTokens(string name) {
        string prefix = "";
        foreach (string part in name.Split(splitDot, StringSplitOptions.RemoveEmptyEntries)) {
            this.templateVars[$"{prefix}{part}"] = true;
            prefix = $"{prefix}{part}.";
        }
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
 * AIRLOCK
 */
Airlock airlock;

public class Airlock {
    public Program program;
    public Dictionary<string, AirlockDoors> airlocks;
    public List<IMyTerminalBlock> airlockBlocks;
    public Dictionary<string, List<IMyFunctionalBlock>> locationToAirlockMap;
    public System.Text.RegularExpressions.Regex include;
    public System.Text.RegularExpressions.Regex exclude;

    // The name to match (Default will match regular doors). The capture group "(.*)" is used when grouping airlock doors.
    public string doorMatch = "Door(.*)";
    // The exclusion tag (can be anything).
    public string doorExclude = "Hangar";
    // Duration before auto close (milliseconds)
    public double timeOpen = 720f;

    public Airlock(Program program) {
        this.program = program;
        this.airlocks = new Dictionary<string, AirlockDoors>();
        this.airlockBlocks = new List<IMyTerminalBlock>();
        this.locationToAirlockMap = new Dictionary<string, List<IMyFunctionalBlock>>();
        this.include = Util.Regex(this.doorMatch, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        this.exclude = Util.Regex(this.doorExclude, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (this.program.config.Enabled("airlock")) {
            this.GetMappedAirlocks();
        }
        this.timeOpen = Util.ParseFloat(this.program.config.Get("airlockOpenTime"), 750f);
    }

    public void CheckAirlocks() {
        if (!this.program.config.Enabled("airlock")) {
            return;
        }
        foreach (var al in this.airlocks) {
            al.Value.Check();
        }
    }

    public void GetMappedAirlocks() {
        if (!this.program.config.Enabled("airlock")) {
            return;
        }
        this.airlockBlocks.Clear();
        this.program.GridTerminalSystem.GetBlocksOfType<IMyDoor>(this.airlockBlocks, door => door.IsSameConstructAs(this.program.Me));

        // Parse into hash (identifier => List(door)), where name is "Door <identifier>"
        this.locationToAirlockMap.Clear();

        // Get all door blocks
        foreach (var block in this.airlockBlocks) {
            var match = this.include.Match(block.CustomName);
            var ignore = this.exclude.Match(block.CustomName);
            if (!match.Success || ignore.Success) {
                continue;
            }
            var key = match.Groups[1].ToString();
            if (!this.locationToAirlockMap.ContainsKey(key)) {
                this.locationToAirlockMap.Add(key, new List<IMyFunctionalBlock>());
            }
            this.locationToAirlockMap[key].Add(block as IMyFunctionalBlock);
        }

        bool doAllDoors = this.program.config.Enabled("airlockAllDoors");
        foreach (var keyval in this.locationToAirlockMap) {
            if (!doAllDoors && keyval.Value.Count < 2) {
                continue;
            }
            this.airlocks.Add(keyval.Key, new AirlockDoors(keyval.Value, this.program));
        }
    }
}

public class AirlockDoors {
    public Program program;
    private List<IMyFunctionalBlock> blocks;
    private List<IMyFunctionalBlock> areClosed;
    private List<IMyFunctionalBlock> areOpen;
    private double openTimer;
    public double timeOpen;

    public AirlockDoors(List<IMyFunctionalBlock> doors, Program program, double timeOpen = 750f) {
        this.program = program;
        this.blocks = new List<IMyFunctionalBlock>(doors);
        this.areClosed = new List<IMyFunctionalBlock>();
        this.areOpen = new List<IMyFunctionalBlock>();
        this.openTimer = timeOpen;
        this.timeOpen = timeOpen;
    }

    private bool IsOpen(IMyFunctionalBlock door) {
        return (door as IMyDoor).OpenRatio > 0;
    }

    private void Lock(List<IMyFunctionalBlock> doors = null) {
        doors = doors ?? this.blocks;
        foreach (var door in doors) {
            (door as IMyDoor).Enabled = false;
        }
    }

    private void Unlock(List<IMyFunctionalBlock> doors = null) {
        doors = doors ?? this.blocks;
        foreach (var door in doors) {
            (door as IMyDoor).Enabled = true;
        }
    }

    private void OpenClose(string action, IMyFunctionalBlock door1, IMyFunctionalBlock door2 = null) {
        (door1 as IMyDoor).ApplyAction(action);
        if (door2 != null) {
            (door2 as IMyDoor).ApplyAction(action);
        }
    }

    private void Open(IMyFunctionalBlock door1, IMyFunctionalBlock door2 = null) {
        this.OpenClose("Open_On", door1, door2);
    }

    private void OpenAll() {
        foreach (var door in this.blocks) {
            this.OpenClose("Open_On", door);
        }
    }

    private void Close(IMyFunctionalBlock door1, IMyFunctionalBlock door2 = null) {
        this.OpenClose("Open_Off", door1, door2);
    }

    private void CloseAll() {
        foreach (var door in this.blocks) {
            this.OpenClose("Open_Off", door);
        }
    }

    public bool Check() {
        int openCount = 0;
        this.areClosed.Clear();
        this.areOpen.Clear();

        foreach (var door in this.blocks) {
            if (this.IsOpen(door)) {
                openCount++;
                this.areOpen.Add(door);
            } else {
                this.areClosed.Add(door);
            }
        }

        if (areOpen.Count > 0) {
            this.openTimer -= this.program.Runtime.TimeSinceLastRun.TotalMilliseconds;
            if (this.openTimer < 0) {
                this.CloseAll();
            } else {
                this.Lock(this.areClosed);
                this.Unlock(this.areOpen);
            }
        } else {
            this.Unlock();
            this.openTimer = this.timeOpen;
        }

        return true;
    }
}
/* AIRLOCK */
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
    public float batteryInput;
    public float batteryOutput;

    public int reactors;
    public float reactorOutputMW;
    public MyFixedPoint reactorUranium;

    public int solars;
    public float solarOutputMW;
    public float solarOutputMax;

    // turbines
    // hydro engines

    public PowerDetails(Program program, Template template = null) {
        this.program = program;
        this.template = template;
        this.powerProducerBlocks = new List<IMyPowerProducer>();
        this.jumpDriveBlocks = new List<IMyJumpDrive>();
        this.items = new List<MyInventoryItem>();
        this.Clear();

        if (this.program.config.Enabled("power")) {
            this.GetBlocks();
            this.RegisterTemplateVars();
        }
    }

    public void RegisterTemplateVars() {
        if (this.template == null) {
            return;
        }
        this.template.Register("power.jumpDrives", () => this.jumpDrives.ToString());
        this.template.Register("power.jumpBar", (DrawingSurface ds, string text, Dictionary<string, string> options) => {
            if (this.jumpDrives == 0) {
                return;
            }
            float pct = this.GetPercent(this.jumpCurrent, this.jumpMax);
            options.Set("text", Util.PctString(pct));
            options.Set("pct", pct.ToString());
            ds.Bar(options).Newline();
        });
        this.template.Register("power.batteries", () => this.batteries.ToString());
        this.template.Register("power.batteryBar", (DrawingSurface ds, string text, Dictionary<string, string> options) => {
            if (this.batteries == 0) {
                return;
            }
            float pct = this.GetPercent(this.batteryCurrent, this.batteryMax);
            options.Set("text", Util.PctString(pct));
            options.Set("pct", pct.ToString());
            ds.Bar(options).Newline();
        });
        this.template.Register("power.solars", () => this.solars.ToString());
        this.template.Register("power.reactors", () => this.reactors.ToString());
        this.template.Register("power.reactorMw", (DrawingSurface ds, string text, Dictionary<string, string> options) => {
            ds.Text($"{this.reactorOutputMW}{text}");
        });
        this.template.Register("power.reactorUr", () => $"{Util.FormatNumber(reactorUranium)} kg");
        this.template.Register("power.io", () => this.PowerIo().ToString());
        this.template.Register("power.ioBar", (DrawingSurface ds, string text, Dictionary<string, string> options) => {
            options.Set("net", this.PowerIo().ToString());
            options.Set("low", this.batteryCurrent.ToString());
            options.Set("high", (this.batteryMax - this.batteryCurrent).ToString());
            ds.MidBar(options).Newline();
        });
    }

    public void Clear() {
        this.jumpDrives = 0;
        this.jumpMax = 0f;
        this.jumpCurrent = 0f;
        this.batteries = 0;
        this.batteryMax = 0f;
        this.batteryCurrent = 0f;
        this.batteryOutput = 0f;
        this.batteryInput = 0f;
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

    public float PowerIo() {
        return this.reactorOutputMW + this.solarOutputMW + this.batteryInput;
    }

    public void Refresh() {
        Clear();

        foreach (IMyPowerProducer powerBlock in this.powerProducerBlocks) {
            if (powerBlock is IMyBatteryBlock) {
                this.batteries += 1;
                this.batteryCurrent += ((IMyBatteryBlock)powerBlock).CurrentStoredPower;
                this.batteryMax += ((IMyBatteryBlock)powerBlock).MaxStoredPower;
                this.batteryInput += ((IMyBatteryBlock)powerBlock).CurrentInput;
                this.batteryOutput += ((IMyBatteryBlock)powerBlock).CurrentOutput;
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

        if (this.program.config.Enabled("cargo")) {
            this.GetCargoBlocks();
            this.RegisterTemplateVars();
        }
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
            this.widths[1] = ds.width / 2 - 1.5f * ds.charSizeInPx.X;
            this.widths[2] = ds.width / 2 + 1.5f * ds.charSizeInPx.X;
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

        if (this.program.config.Enabled("power")) {
            this.GetProductionBlocks();
            this.RegisterTemplateVars();
        }
    }

    public void RegisterTemplateVars() {
        if (this.template == null) {
            return;
        }

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

    public static TValue Set<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value) {
        return dict[key] = dict.Get(key, value);
    }

    public static string Print<TKey, TValue>(this Dictionary<TKey, TValue> dict) {
        StringBuilder sb = new StringBuilder("{ ");
        foreach (KeyValuePair<TKey, TValue> keyValues in dict) {
            sb.Append($"{keyValues.Key}: {keyValues.Value}, ");
        }

        return sb.Append("}").ToString();
    }
/* UTIL */
