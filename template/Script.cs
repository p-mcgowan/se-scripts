List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
Dictionary<string, DrawingSurface> drawables = new Dictionary<string, DrawingSurface>();
Template template;
// random trailing spaces when loading from file...
string templateStrings = System.Text.RegularExpressions.Regex.Replace(
@"This is a test {no.registered.method}
This is another line, below is
a registered method returning Random
{test.random:min=0;max=10}

---------------------------------
{?test.spacing}
---------------------------------
{text::this line has text}
---------------------------------
demo bar: {bar:bgColour=red;textColour=100,100,100;fillColour=blue;pct=0.63:asdf text}

<
{?test.cdtnl:c=dimyellow:this text will print when random succeeds}
>

does this still print
{text:colour=0,0,100:i'm blue, abadee abadaa}
{text:colour=red:some like it red}
???
", @" ([\r\n]+)", "$1");


Random random = new Random();

public void Random(DrawingSurface ds, string text, DrawingSurface.Options opts) {
    int min = Util.ParseInt(opts.custom.Get("min", "0"), 0);
    int max = Util.ParseInt(opts.custom.Get("max", "100"), 100);
    ds.Text($"{this.random.Next(min, max)}");
}

public void Spacing(DrawingSurface ds, string text, DrawingSurface.Options opts) { }

public void ConditionalSpacing(DrawingSurface ds, string text, DrawingSurface.Options opts) {
    if (this.random.Next(0, 10) > 5) {
        Color? colour = DrawingSurface.StringToColour(opts.custom.Get("c"));
        ds.Text(text, colour: colour).Newline();
    }
}

public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
    template = new Template(this);
    template.PreRender("the_same_text_string_for_everything", templateStrings);
    blocks.Clear();
    GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(blocks);
    foreach (IMyTextSurfaceProvider block in blocks) {
        for (int i = 0; i < block.SurfaceCount; i++) {
            IMyTextSurface surface = block.GetSurface(i);
            string name = $"{((IMyTerminalBlock)block).CustomName} <{i}>";

            drawables.Add(name, new DrawingSurface(surface, this, name));

            // We can skip adding templates for each screen since they are the same
            // template.PreRender(name, block.CustomData);
        }
    }

    template.Register("test.random", Random);
    template.Register("test.spacing", Spacing);
    template.Register("test.cdtnl", ConditionalSpacing);
}

public void Main(string argument, UpdateType updateSource) {
    Echo(Runtime.LastRunTimeMs.ToString());
    foreach (KeyValuePair<string, DrawingSurface> drawable in drawables) {
        template.Render(drawable.Value, "the_same_text_string_for_everything");
    }
}
/* MAIN */
/*
 * GRAPHICS
 */
public class DrawingSurface {
    public class Options {
        public bool outline = false;
        public Color? bgColour = null;
        public Color? colour = null;
        public Color? fillColour = null;
        public Color? textColour = null;
        public float height = 0f;
        public float high = 1f;
        public float low = 1f;
        public float net = 0f;
        public float pad = 0.1f;
        public float pct = 0f;
        public float scale = 1f;
        public float size = 0f;
        public float width = 0f;
        public float? textPadding = null;
        public List<Color> colours = new List<Color>();
        public List<float> values = new List<float>();
        public string text = null;
        public TextAlignment? align = null;
        public Dictionary<string, string> custom;

        public Options() {
            this.custom = new Dictionary<string, string>();
        }
    }

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
    public string name;
    public int ySpace;
    public Color colourGrey = new Color(40, 40, 40);
    public readonly StringBuilder sizeBuilder;

    public static char[] underscoreSep = { '_' };
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
        { "dimgreen", Color.Darken(Color.Green, 0.3) },
        { "dimyellow", Color.Darken(Color.Yellow, 0.6) },
        { "dimorange", Color.Darken(Color.Orange, 0.2) },
        { "dimred", Color.Darken(Color.Red, 0.2) }
    };
    public static Dictionary<string, TextAlignment> stringToAlignment = new Dictionary<string, TextAlignment>() {
        { "center", TextAlignment.CENTER },
        { "left", TextAlignment.LEFT },
        { "right", TextAlignment.RIGHT }
    };

    public DrawingSurface(IMyTextSurface surface = null, Program program = null, string name = "", int ySpace = 2) {
        this.program = program;
        this.surface = surface;
        this.cursor = new Vector2(0f, 0f);
        this.savedCursor = new Vector2(0f, 0f);
        this.sb = new StringBuilder();
        this.sizeBuilder = new StringBuilder("O");
        this.charSizeInPx = new Vector2(0f, 0f);
        this.surface.ContentType = ContentType.SCRIPT;
        this.drawing = false;
        this.viewport = new RectangleF(0f, 0f, 0f, 0f);
        this.name = name;
        this.ySpace = ySpace;
    }

    public void InitScreen() {
        if (this.surface == null) {
            return;
        }

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

    public static Color? StringToColour(string colour) {
        if (colour == "" || colour == null) {
            return null;
        }
        if (!colour.Contains(",")) {
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
        if (!this.drawing) {
            this.DrawStart();
        }

        this.savedCursor = this.cursor;

        return this;
    }

    public DrawingSurface LoadCursor() {
        if (!this.drawing) {
            this.DrawStart();
        }

        this.cursor = this.savedCursor;

        return this;
    }

    public DrawingSurface SetCursor(float? x, float? y) {
        if (!this.drawing) {
            this.DrawStart();
        }

        this.cursor.X = x ?? this.cursor.X;
        this.cursor.Y = y ?? this.cursor.Y;

        return this;
    }

    public DrawingSurface Newline(bool reverse = false) {
        float height = (this.charSizeInPx.Y + this.ySpace) * (reverse ? -1 : 1);
        this.cursor.Y += height;
        this.cursor.X = this.savedCursor.X;

        return this;
    }

    public DrawingSurface Size(float? size = null) {
        this.surface.FontSize = size ?? this.surface.FontSize;
        this.charSizeInPx = this.surface.MeasureStringInPixels(this.sizeBuilder, this.surface.Font, this.surface.FontSize);

        return this;
    }

    public static MySprite TextSprite(Options options) {
        Color? colour = options.colour;
        TextAlignment textAlignment = options.align ?? TextAlignment.LEFT;
        float scale = options.scale;
        string text = options.text;

        return new MySprite() {
            Type = SpriteType.TEXT,
            Data = text,
            Color = colour,
            Alignment = textAlignment,
            RotationOrScale = scale
        };
    }

    public void AddTextSprite(MySprite sprite) {
        if (!this.drawing) {
            this.DrawStart();
        }

        sprite.Position = this.cursor + this.viewport.Position;
        sprite.RotationOrScale = this.surface.FontSize * sprite.RotationOrScale;
        sprite.FontId = this.surface.Font;
        sprite.Color = sprite.Color ?? this.surface.ScriptForegroundColor;

        this.frame.Add(sprite);

        this.AddTextSizeToCursor(sprite.Data, sprite.Alignment);
    }

    public DrawingSurface Text(string text, Options options) {
        if (options == null) {
            return this.Text(text);
        }
        TextAlignment textAlignment = options.align ?? TextAlignment.LEFT;

        return this.Text(text, colour: options.colour, textAlignment: textAlignment, scale: options.scale);
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
        colour = colour ?? this.surface.ScriptForegroundColor;

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

        this.AddTextSizeToCursor(text, textAlignment);

        return this;
    }

    public void AddTextSizeToCursor(string text, TextAlignment alignment) {
        if (alignment == TextAlignment.RIGHT) {
            return;
        }

        this.sb.Clear();
        this.sb.Append(text);

        Vector2 size = this.surface.MeasureStringInPixels(this.sb, this.surface.Font, this.surface.FontSize);
        this.cursor.X += alignment == TextAlignment.CENTER ? size.X / 2 : size.X;
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

    public DrawingSurface MidBar(Options options) {
        return this.MidBar(
            net: options.net,
            low: options.low,
            high: options.high,
            width: options.width,
            height: options.height,
            pad: options.pad,
            bgColour: options.bgColour,
            text: options.text,
            textColour: options.textColour
        );
    }

    public DrawingSurface MidBar(
        float net,
        float low,
        float high,
        float width = 0f,
        float height = 0f,
        float pad = 0.1f,
        Color? bgColour = null,
        string text = null,
        Color? textColour = null
    ) {
        if (!this.drawing) {
            this.DrawStart();
        }
        low = (float)Math.Abs(low);
        high = (float)Math.Abs(high);

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

        Color colour = DrawingSurface.stringToColour.Get("dimgreen");
        float pct = (high == 0f ? 1f : (float)Math.Min(1f, net / high));
        if (net < 0) {
            pct = (low == 0f ? -1f : (float)Math.Max(-1f, net / low));
            colour = DrawingSurface.stringToColour.Get("dimred");
        }
        float sideWidth = (float)Math.Abs(Math.Sqrt(2) * pct * width);
        float leftClip = Math.Min((width / 2), (width / 2) * (1 + pct));
        float rightClip = Math.Max((width / 2), (width / 2) * (1 + pct));

        using (this.frame.Clip((int)(pos.X + leftClip), (int)(pos.Y - height / 2), (int)Math.Abs(rightClip - leftClip), (int)height)) {
            this.frame.Add(new MySprite {
                Type = SpriteType.TEXTURE,
                Alignment = TextAlignment.CENTER,
                Data = "SquareSimple",
                Position = pos + new Vector2(width / 2, 0),
                Size = new Vector2(sideWidth / 2, width),
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

        text = text ?? Util.PctString(pct);
        if (text != null && text != "") {
            this.cursor.X += net > 0 ? (width / 4) : (3 * width / 4);
            this.Text(text, textColour ?? this.surface.ScriptForegroundColor, textAlignment: TextAlignment.CENTER, scale: 0.8f);
        } else {
            this.cursor.X += width;
        }

        return this;
    }

    public DrawingSurface Bar(Options options) {
        if (options == null || options.pct == 0f) {
            return this.Bar(0f, text: "--/--");
        }

        return this.Bar(
            options.pct,
            width: options.width,
            height: options.height,
            fillColour: options.fillColour,
            textAlignment: options.align ?? TextAlignment.CENTER,
            text: options.text,
            textColour: options.textColour,
            bgColour: options.bgColour,
            pad: options.pad
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
        TextAlignment textAlignment = TextAlignment.CENTER,
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

        text = text ?? Util.PctString(pct);
        if (text != null && text != "") {
            this.cursor.X += (width / 2);
            this.Text(text, textColour ?? this.surface.ScriptForegroundColor, textAlignment: textAlignment, scale: 0.8f);
            this.cursor.X += (width / 2);
        } else {
            this.cursor.X += width;
        }

        return this;
    }

    public DrawingSurface MultiBar(Options options) {
        return this.MultiBar(
            options.values,
            options.colours,
            width: options.width,
            height: options.height,
            text: options.text,
            textColour: options.textColour,
            bgColour: options.bgColour,
            textAlignment: options.align ?? TextAlignment.CENTER,
            pad: options.pad
        );
    }

    public DrawingSurface MultiBar(
        List<float> values,
        List<Color> colours,
        float width = 0f,
        float height = 0f,
        Vector2? position = null,
        string text = null,
        Color? textColour = null,
        Color? bgColour = null,
        TextAlignment textAlignment = TextAlignment.CENTER,
        float pad = 0.1f
    ) {
        if (!this.drawing) {
            this.DrawStart();
        }

        width = (width == 0f) ? this.width - this.cursor.X : width;
        height = (height == 0f) ? this.charSizeInPx.Y : height;
        height -= 1f;

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

        int i = 0;
        float sum = 0f;
        int length = values.Count;
        for (i = 0; i < values.Count; ++i) {
            sum += (float)Math.Abs(values[i]);
            values[i] = sum;
        }

        for (i = values.Count - 1; i >= 0; --i) {
            float pct = (float)Math.Min(values[i], 1f);
            if (pct == 0f) {
                continue;
            }
            Color colour = (colours.Count <= i) ?
                DrawingSurface.stringToColour.ElementAt(i % DrawingSurface.stringToColour.Count).Value : colours[i];
            using (frame.Clip((int)pos.X, (int)(pos.Y - height / 2), (int)(width * pct), (int)height)) {
                this.frame.Add(new MySprite {
                    Type = SpriteType.TEXTURE,
                    Alignment = TextAlignment.CENTER,
                    Data = "SquareSimple",
                    Position = pos + new Vector2((width * pct) / 2, 0),
                    Size = new Vector2((float)Math.Floor(Math.Sqrt(Math.Pow((width * pct), 2) / 2)), width),
                    Color = Color.White,
                    RotationOrScale = this.ToRad(-45f),
                });
                this.frame.Add(new MySprite {
                    Type = SpriteType.TEXTURE,
                    Alignment = TextAlignment.CENTER,
                    Data = "SquareSimple",
                    Position = pos + new Vector2((width * pct) / 2, 0),
                    Size = new Vector2((float)Math.Floor(Math.Sqrt(Math.Pow((width * pct), 2) / 2)), width),
                    Color = colour,
                    RotationOrScale = this.ToRad(-45f),
                });
            }
        }

        if (text != null && text != "") {
            this.cursor.X += (width / 2);
            this.Text(text, textColour ?? this.surface.ScriptForegroundColor, textAlignment: textAlignment, scale: 0.8f);
        } else {
            this.cursor.X += width;
        }

        return this;
    }

    public DrawingSurface TextCircle(Options options) {
        return (options == null) ? this.TextCircle() : this.TextCircle(options.colour, outline: options.outline);
    }

    public DrawingSurface TextCircle(Color? colour = null, bool outline = false) {
        this.Circle(this.charSizeInPx.X - 2f, colour, position: this.cursor + Vector2.Divide(this.charSizeInPx, 2f), outline: outline);
        this.cursor.X += 2f;

        return this;
    }

    public DrawingSurface Circle(Options options) {
        if (options == null) {
            return this.Circle(this.charSizeInPx.Y, null);
        }

        float size = (options.size <= 0f) ? this.charSizeInPx.Y : options.size;

        return this.Circle(size: size, colour: options.colour, outline: options.outline);
    }

    public DrawingSurface Circle(float size, Color? colour, Vector2? position = null, bool outline = false) {
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
            Color = colour ?? this.surface.ScriptForegroundColor,
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
        public DrawingSurface.Options options;
        public MySprite? sprite;

        public Node(string action, string text = null, DrawingSurface.Options opts = null) {
            this.action = action;
            this.text = text;
            this.options = opts ?? new DrawingSurface.Options();
            if (action == "text") {
                this.options.text = this.options.text ?? text;
                this.sprite = DrawingSurface.TextSprite(this.options);
            }
        }
    }

    public delegate void DsCallback(DrawingSurface ds, string token, DrawingSurface.Options options);
    public delegate string TextCallback();

    public Program program;
    public System.Text.RegularExpressions.Regex tokenizer;
    public System.Text.RegularExpressions.Regex cmdSplitter;
    public System.Text.RegularExpressions.Match match;
    public Token token;
    public Dictionary<string, DsCallback> methods;
    public Dictionary<string, List<Node>> renderNodes;
    public Dictionary<string, Dictionary<string, bool>> templateVars;
    public Dictionary<string, string> prerenderedTemplates;
    public List<int> removeNodes;

    public char[] splitSemi = new[] { ';' };
    public char[] splitDot = new[] { '.' };
    public string[] splitLine = new[] { "\r\n", "\r", "\n" };

    public Template(Program program = null) {
        this.program = program;
        this.tokenizer = Util.Regex(@"((?<!\\)\{([^\}]|\\\})+(?<!\\)\}|(\\\{|[^\{])+)", System.Text.RegularExpressions.RegexOptions.Compiled);
        this.cmdSplitter = Util.Regex(@"(?<newline>\?)?(?<name>[^:]+)(:(?<params>[^:]*))?(:(?<text>.+))?", System.Text.RegularExpressions.RegexOptions.Compiled);
        this.token = new Token();
        this.methods = new Dictionary<string, DsCallback>();
        this.renderNodes = new Dictionary<string, List<Node>>();
        this.templateVars = new Dictionary<string, Dictionary<string, bool>>();
        this.prerenderedTemplates = new Dictionary<string, string>();
        this.removeNodes = new List<int>();

        this.Reset();
    }

    public void Reset() {
        this.Clear();

        this.Register("textCircle", (DrawingSurface ds, string text, DrawingSurface.Options options) => ds.TextCircle(options));
        this.Register("circle", (DrawingSurface ds, string text, DrawingSurface.Options options) => ds.Circle(options));
        this.Register("bar", (DrawingSurface ds, string text, DrawingSurface.Options options) => ds.Bar(options));
        this.Register("midBar", (DrawingSurface ds, string text, DrawingSurface.Options options) => ds.MidBar(options));
        this.Register("multiBar", (DrawingSurface ds, string text, DrawingSurface.Options options) => ds.MultiBar(options));
        this.Register("right", (DrawingSurface ds, string text, DrawingSurface.Options options) => ds.SetCursor(ds.width, null));
        this.Register("center", (DrawingSurface ds, string text, DrawingSurface.Options options) => ds.SetCursor(ds.width / 2f, null));
        this.Register("saveCursor", this.SaveCursor);
        this.Register("setCursor", this.SetCursor);
    }

    public void Clear() {
        this.templateVars.Clear();
        this.prerenderedTemplates.Clear();
        this.renderNodes.Clear();
        this.methods.Clear();
    }

    public void Register(string key, DsCallback callback) {
        this.methods[key] = callback;
    }

    public void Register(string key, TextCallback callback) {
        // TODO: precalc
        this.methods[key] = (DrawingSurface ds, string text, DrawingSurface.Options options) => ds.Text(callback(), options);
    }

    public void ConfigureScreen(DrawingSurface ds, DrawingSurface.Options options) {
        ds.surface.Font = options.custom.Get("font", ds.surface.Font);
        ds.surface.FontSize = options.size == 0f ? ds.surface.FontSize : options.size;
        ds.surface.TextPadding = options.textPadding ?? ds.surface.TextPadding;
        ds.surface.ScriptForegroundColor = options.colour ?? ds.surface.ScriptForegroundColor;
        ds.surface.ScriptBackgroundColor = options.bgColour ?? ds.surface.ScriptBackgroundColor;
    }

    public bool IsPrerendered(string outputName, string templateString) {
        string value = this.prerenderedTemplates.Get(outputName, null);
        if (value == "" || value == null) {
            return false;
        }
        if (String.CompareOrdinal(value, templateString) != 0) {
            return false;
        }
        return true;
    }

    public Dictionary<string, bool> PreRender(string outputName, string templateStrings) {
        this.prerenderedTemplates[outputName] = templateStrings;
        return this.PreRender(outputName, templateStrings.Split(splitLine, StringSplitOptions.None));
    }

    private Dictionary<string, bool> PreRender(string outputName, string[] templateStrings) {
        if (this.templateVars.ContainsKey(outputName)) {
            this.templateVars[outputName].Clear();
        } else {
            this.templateVars[outputName] = new Dictionary<string, bool>();
        }
        List<Node> nodeList = new List<Node>();

        bool autoNewline;
        string text;
        for (int i = 0; i < templateStrings.Length; ++i) {
            string line = templateStrings[i].TrimEnd();
            autoNewline = true;
            this.match = null;
            text = null;

            while (this.GetToken(line)) {
                if (this.token.isText) {
                    text = System.Text.RegularExpressions.Regex.Replace(this.token.value, @"\\([\{\}])", "$1");
                    nodeList.Add(new Node("text", text));
                    continue;
                }

                System.Text.RegularExpressions.Match m = this.cmdSplitter.Match(this.token.value);
                if (!m.Success) {
                    this.AddTemplateTokens(this.templateVars[outputName], this.token.value);
                    nodeList.Add(new Node(this.token.value));
                    continue;
                }

                var opts = this.StringToOptions(m.Groups["params"].Value);

                if (m.Groups["newline"].Value != "") {
                    opts.custom["noNewline"] = "true";
                    autoNewline = false;
                }

                text = m.Groups["text"].Value == "" ? null : m.Groups["text"].Value;
                if (text != null) {
                    text = System.Text.RegularExpressions.Regex.Replace(text, @"\\([\{\}])", "$1");
                    opts.text = text;
                }

                string action = m.Groups["name"].Value;
                this.AddTemplateTokens(this.templateVars[outputName], action);

                nodeList.Add(new Node(action, text, opts));
                if (action == "config") {
                    autoNewline = false;
                }
            }

            if (autoNewline) {
                nodeList.Add(new Node("newline"));
            }
        }

        this.renderNodes[outputName] = nodeList;

        return this.templateVars[outputName];
    }

    public void AddTemplateTokens(Dictionary<string, bool> tplVars, string name) {
        string prefix = "";
        foreach (string part in name.Split(splitDot, StringSplitOptions.RemoveEmptyEntries)) {
            tplVars[$"{prefix}{part}"] = true;
            prefix = $"{prefix}{part}.";
        }
    }

    public int insertSprites = 0;
    public void Render(DrawingSurface ds, string name = null) {
        string dsName = name ?? ds.name;
        List<Node> nodeList = null;
        if (!this.renderNodes.TryGetValue(dsName, out nodeList)) {
            ds.Text("No template found").Draw();
            return;
        }

        DsCallback callback = null;
        int i = 0;
        if (++insertSprites >= 4) {
            insertSprites=0;
        }
        for (int j = 0; j < insertSprites; j++) {
            ds.frame.Add(new MySprite());
        }
        this.removeNodes.Clear();
        foreach (Node node in nodeList) {
            if (node.action == "newline") {
                ds.Newline();
                continue;
            }

            if (node.action == "text") {
                ds.AddTextSprite((MySprite)node.sprite);
                continue;
            }

            if (node.action == "config") {
                this.ConfigureScreen(ds, node.options);
                removeNodes.Add(i);
                continue;
            }

            if (this.methods.TryGetValue(node.action, out callback)) {
                callback(ds, node.text, node.options);
            } else {
                ds.Text($"{{{node.action}}}");
            }
            i++;
        }
        ds.Draw();

        foreach (int removeNode in removeNodes) {
            nodeList.RemoveAt(removeNode);
        }
    }

    public DrawingSurface.Options StringToOptions(string options = "") {
        DrawingSurface.Options opts = new DrawingSurface.Options();
        if (options == "") {
            return opts;
        }

        Dictionary<string, string> parsed = options.Split(splitSemi, StringSplitOptions.RemoveEmptyEntries)
            .Select(v => v.Split('='))
            .ToDictionary(pair => pair.Length > 1 ? pair[0] : "unknown", pair => pair.Length > 1 ? pair[1] : pair[0]);

        string value;
        if (parsed.Pop("width", out value)) { opts.width = Util.ParseFloat(value); }
        if (parsed.Pop("height", out value)) { opts.height = Util.ParseFloat(value); }
        if (parsed.Pop("outline", out value)) { opts.outline = Util.ParseBool(value); }
        if (parsed.Pop("bgColour", out value)) { opts.bgColour = DrawingSurface.StringToColour(value); }
        if (parsed.Pop("colour", out value)) { opts.colour = DrawingSurface.StringToColour(value); }
        if (parsed.Pop("fillColour", out value)) { opts.fillColour = DrawingSurface.StringToColour(value); }
        if (parsed.Pop("textColour", out value)) { opts.textColour = DrawingSurface.StringToColour(value); }
        if (parsed.Pop("height", out value)) { opts.height = Util.ParseFloat(value); }
        if (parsed.Pop("high", out value)) { opts.high = Util.ParseFloat(value); }
        if (parsed.Pop("low", out value)) { opts.low = Util.ParseFloat(value); }
        if (parsed.Pop("net", out value)) { opts.net = Util.ParseFloat(value); }
        if (parsed.Pop("pad", out value)) { opts.pad = Util.ParseFloat(value); }
        if (parsed.Pop("pct", out value)) { opts.pct = Util.ParseFloat(value); }
        if (parsed.Pop("scale", out value)) { opts.scale = Util.ParseFloat(value); }
        if (parsed.Pop("size", out value)) { opts.size = Util.ParseFloat(value); }
        if (parsed.Pop("width", out value)) { opts.width = Util.ParseFloat(value); }
        if (parsed.Pop("text", out value)) { opts.text = value; }
        if (parsed.Pop("textPading", out value)) { opts.textPadding = Util.ParseFloat(value); }
        if (parsed.Pop("align", out value)) { opts.align = DrawingSurface.stringToAlignment.Get(value); }
        if (parsed.Pop("colours", out value)) {
            opts.colours = value.Split(DrawingSurface.underscoreSep)
                .Select(col => DrawingSurface.StringToColour(col) ?? Color.White).ToList();
        }
        if (parsed.Pop("values", out value)) {
            opts.values = value.Split(DrawingSurface.underscoreSep)
                .Select(pct => Util.ParseFloat(pct, 0f)).ToList();
        }

        opts.custom = parsed;

        return opts;
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

    public float? ParseCursor(DrawingSurface ds, string input, bool vertical = false) {
        if (input == null) {
            return null;
        }

        float saved = vertical ? ds.savedCursor.Y : ds.savedCursor.X;
        if (input == "x" || input == "y") {
            return saved;
        }

        float width = vertical ? ds.height : ds.width;
        if (input[input.Length - 1] == '%') {
            return width * Util.ParseFloat(input.Remove(input.Length - 1)) / 100f;
        }

        float current = vertical ? ds.cursor.Y : ds.cursor.X;
        if (input == "~x" || input == "~y") {
            return Math.Max(current, saved);
        }

        float charSize = vertical ? ds.charSizeInPx.Y : ds.charSizeInPx.X;
        if (input[0] == '+') {
            return current + Util.ParseFloat(input) * charSize;
        }
        if (input[0] == '-') {
            return current - Util.ParseFloat(input) * charSize;
        }

        return Util.ParseFloat(input);
    }

    public void SaveCursor(DrawingSurface ds, string text, DrawingSurface.Options options) {
        float x = ds.cursor.X;
        float y = ds.cursor.Y;
        this.SetCursor(ds, text, options);
        ds.savedCursor.X = x;
        ds.savedCursor.Y = y;
    }

    public void SetCursor(DrawingSurface ds, string text, DrawingSurface.Options options) {
        float? x = this.ParseCursor(ds, options.custom.Get("x"), false);
        float? y = this.ParseCursor(ds, options.custom.Get("y"), true);
        ds.SetCursor(x, y);
    }
}
/*
 * UTIL
 */
static System.Globalization.NumberFormatInfo CustomFormat;
public static System.Globalization.NumberFormatInfo GetCustomFormat() {
    if (CustomFormat == null) {
        CustomFormat = (System.Globalization.NumberFormatInfo)System.Globalization.CultureInfo.InvariantCulture.NumberFormat.Clone();
        CustomFormat.NumberGroupSeparator = $"{(char)0xA0}";
        CustomFormat.NumberGroupSizes = new [] {3};
    }
    return CustomFormat;
}

public static class Util {
    public static StringBuilder sb = new StringBuilder("");

    public static System.Text.RegularExpressions.Regex surfaceExtractor =
        Util.Regex(@"\s<(\d+)>$", System.Text.RegularExpressions.RegexOptions.Compiled);

    public static string GetFormatNumberStr(VRage.MyFixedPoint input) {
        int n = Math.Max(0, (int)input);
        if (n == 0) {
            return "0";
        } else if (n < 10000) {
            return "#,,#";
        } else if (n < 1000000) {
            return "###,,0,K";
        }

        sb.Clear();
        for (int i = $"{n}".Length; i > 0; --i) {
            sb.Append("#");
        }

        return $"{sb}0,,.0M";
    }

    public static string FormatNumber(VRage.MyFixedPoint input, string fmt = null) {
        fmt = fmt ?? Util.GetFormatNumberStr(input);
        int n = Math.Max(0, (int)input);

        return n.ToString(fmt, GetCustomFormat());
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
        if (id.Contains("/")) {
            return id.Split('/')[1];
        }
        return id;
    }

    public static string PctString(float val) {
        return (val * 100).ToString("#,0.00", GetCustomFormat()) + " %";
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

    public static bool BlockValid(IMyCubeBlock block) {
        return block != null && block.WorldMatrix.Translation != Vector3.Zero;
    }
}
}

public static class Dict {
    public static TValue Get<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue defaultValue = default(TValue)) {
        TValue value;
        return dict.TryGetValue(key, out value) ? value : defaultValue;
    }

    public static TValue Default<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value) {
        return dict[key] = dict.Get(key, value);
    }

    public static string Print<TKey, TValue>(this Dictionary<TKey, TValue> dict) {
        StringBuilder sb = new StringBuilder("{ ");
        foreach (KeyValuePair<TKey, TValue> keyValues in dict) {
            sb.Append($"{keyValues.Key}: {keyValues.Value}, ");
        }

        return sb.Append("}").ToString();
    }

    public static bool Pop<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, out TValue result) {
        if (dict.TryGetValue(key, out result)) {
            dict.Remove(key);

            return true;
        };
        return false;
    }
/* UTIL */
