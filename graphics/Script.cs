/*
 * GRAPHICS
 */
public class DrawingSurface {
    public Program program;
    public IMyTextSurface surface;
    public RectangleF viewport;
    public MySpriteDrawFrame frame;
    public Vector2 cursor;
    public StringBuilder sb;
    public Vector2 charSizeInPx;
    public bool drawing;
    public Vector2 padding;
    public float width;
    public float height;
    public int charsPerWidth;
    public int charsPerHeight;

    public DrawingSurface(IMyTextSurface surface, Program program) {
        this.program = program;
        this.surface = surface;
        this.cursor = new Vector2(0f, 0f);
        this.sb = new StringBuilder("j");
        this.charSizeInPx = new Vector2(0f, 0f);
        this.surface.ContentType = ContentType.SCRIPT;
        this.drawing = false;
        this.surface.Font = "Monospace";
        this.viewport = new RectangleF(0f, 0f, 0f, 0f);

        this.InitScreen();
    }

    public void InitScreen() {
        this.cursor.X = 0f;
        this.cursor.Y = 0f;
        this.surface.Script = "";

        this.padding = (surface.TextPadding / 100) * this.surface.SurfaceSize;
        this.viewport.Position = (this.surface.TextureSize - this.surface.SurfaceSize) / 2f + this.padding;
        this.viewport.Size = this.surface.SurfaceSize - (2 * this.padding);
        this.width = this.viewport.Width;
        this.height = this.viewport.Height;

        this.charSizeInPx = this.surface.MeasureStringInPixels(this.sb, this.surface.Font, this.surface.FontSize);
        this.charsPerWidth = (int)Math.Floor(this.surface.SurfaceSize.X / this.charSizeInPx.X);
        this.charsPerHeight = (int)Math.Floor(this.surface.SurfaceSize.Y / this.charSizeInPx.Y);
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

    public DrawingSurface SetCursor(float? x, float? y) {
        this.cursor.X = x ?? this.cursor.X;
        this.cursor.Y = y ?? this.cursor.Y;

        return this;
    }

    public DrawingSurface Newline() {
        this.cursor.Y += this.charSizeInPx.Y;
        this.cursor.X = 0;

        return this;
    }

    public DrawingSurface Text(
        string text,
        Color? colour = null,
        TextAlignment textAlignment = TextAlignment.LEFT,
        float scale = 1f,
        Vector2? position = null
    ) {
        this.program.Echo("1");
        if (!this.drawing) {
            this.DrawStart();
        }
        this.program.Echo("2");
        if (colour == null) {
            colour = this.surface.FontColor;
        }
        this.program.Echo("3");

        Vector2 pos = (position ?? this.cursor) + this.viewport.Position;

        this.program.Echo("4");
        this.frame.Add(new MySprite() {
            Type = SpriteType.TEXT,
            Data = text,
            Position = pos,
            RotationOrScale = this.surface.FontSize * scale,
            Color = colour,
            Alignment = textAlignment,
            FontId = surface.Font
        });

        this.program.Echo("5");
        this.cursor.X += this.charSizeInPx.X * text.Length + (float)Math.Ceiling((double)(text.Length / 2)) - 1f;

        this.program.Echo("6");
        return this;
    }

    public float ToRad(float deg) {
        return deg * ((float)Math.PI / 180f);
    }

    public Color FloatPctToColor(float pct) {
        if (pct > 0.75f) {
            return Color.Darken(Color.Green, 0.2);
        } else if (pct > 0.5f) {
            return Color.Darken(Color.Yellow, 0.4);
        } else if (pct > 0.25f) {
            return Color.Darken(Color.Orange, 0.2);
        }

        return Color.Darken(Color.Red, 0.2);
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
                Color = bgColour ?? new Color(60, 60, 60),
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
                Color = bgColour ?? new Color(60, 60, 60),
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

        if (text != null && text != "") {
            this.cursor.X += (width / 2);
            this.Text(text, textColour ?? Color.Black, textAlignment: TextAlignment.CENTER, scale: 0.9f);
        } else {
            this.cursor.X += (width / 2);
            this.cursor.Y += height;
        }


        return this;
    }

    public DrawingSurface TextCircle(Color colour, bool outline = false) {
        return this.Circle(this.charSizeInPx.Y - 5f, colour, this.cursor + Vector2.Divide(this.charSizeInPx, 2f), outline: outline);
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

public class Graphics {
    public Dictionary<string, DrawingSurface> drawables;

    public Graphics() {
        this.drawables = new Dictionary<string, DrawingSurface>();
    }
}
/* GRAPHICS */
/*
; User config - place in custom data
;
; for surface selection, use 'name <number>' eg: 'Cockpit <1>'
[Control Seat <0>]
POWER:text-bar
CARGO:text-bar

[void]
BLOCK_HEALTH: true
PRODUCTION: true
AIRLOCK:true
HEALTH_IGNORE:Hydrogen Thruster,Suspension
*/

List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
Graphics graphics = new Graphics();

public Program() {
    blocks.Clear();
    GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(blocks);
    foreach (IMyTextSurfaceProvider block in blocks) {
        for (int i = 0; i < block.SurfaceCount; i++) {
            IMyTextSurface surface = block.GetSurface(i);
            graphics.drawables.Add($"{((IMyTerminalBlock)block).CustomName} <{i}>", new DrawingSurface(surface, this));
        }
    }
}

public void Main(string argument, UpdateType updateSource) {
    foreach (KeyValuePair<string, DrawingSurface> drawable in graphics.drawables) {
        DrawingSurface ds = drawable.Value;

        // Vector2 charSquare = new Vector2(0, 0);
        Vector2 charSquare = Vector2.Divide(ds.charSizeInPx, 2f);
        float battCap = 1000f;
        float battStored = 100f;
        // float netEIO = 50f;

        ds
            .Text("Jump drives: 1 ").Newline()
            .Bar(1f, text: "100%").Newline()
            .Text("Batteries: 3   ").Newline()
            .Bar(0.7f, text: "70%").Newline()
            .Text($"Stored energy draining: {Decimal.Round((decimal)Math.Abs(-43f / battStored), 1)}% capacity / h").Newline()
            .MidBar(-43f, battStored, battCap - battStored).Newline()
            .Text($"Stored energy increasing: {Decimal.Round((decimal)(76f / (battCap - battStored)), 1)}% capacity / h").Newline()
            .MidBar(76f, battStored, battCap - battStored).Newline()
            .Text("Reactors: 3").Newline()
            .Text("Output: 60 MW, Uranium: 50 kg").Newline()
            .Newline()
            .Text("Ship status: No damage detected").Newline()
            .Newline()
            .Text("Power saving mode < 1m (check in 1m)").Newline()
            .TextCircle(Color.Green).Text("Assembler 1: Idle < 1m").Newline()
            .TextCircle(Color.Green).Text("Assembler 2: Idle < 1m").Newline()
            .TextCircle(Color.Red).Text("Assembler 3: Blocked < 1m").Newline()
            .TextCircle(Color.Yellow).Text("Assembler 4: Working < 1m").Newline()
            .TextCircle(Color.Green, true).Text("(station) Assembler 1: Idle < 1m").Newline()
            .TextCircle(Color.Green, true).Text("(station) Assembler 2: Idle < 1m").Newline()
            .TextCircle(Color.Red, true).Text("(station) Assembler 3: Blocked < 1m").Newline()
            .TextCircle(Color.Yellow, true).Text("(station) Assembler 4: Working < 1m").Newline()
            .Newline()
            .Text("Cargo: 4000 kg / 10000 kg").Newline()
            .Bar(0.4f, text: "40%", fillColour: Color.Green).Newline()
            .Newline()
            .Text("Iron Ingot").SetCursor(ds.width, null).Text("24K", textAlignment: TextAlignment.RIGHT).Newline()
            .Text("Stone Ingot").SetCursor(ds.width, null).Text("7537", textAlignment: TextAlignment.RIGHT).Newline()
            .Text("Nickel Ingot").SetCursor(ds.width, null).Text("1292", textAlignment: TextAlignment.RIGHT).Newline()
            .Text("Silicon Ingot").SetCursor(ds.width, null).Text("2153", textAlignment: TextAlignment.RIGHT).Newline()
            .Text("AutomaticRifleItem").SetCursor(ds.width, null).Text("1", textAlignment: TextAlignment.RIGHT).Newline()
            .Text("WelderItem").SetCursor(ds.width, null).Text("1", textAlignment: TextAlignment.RIGHT).Newline()
            .Text("HandDrillItem").SetCursor(ds.width, null).Text("1", textAlignment: TextAlignment.RIGHT).Newline()
            .Text("AngleGrinderItem").SetCursor(ds.width, null).Text("1", textAlignment: TextAlignment.RIGHT).Newline()
            .Text("Ice").SetCursor(ds.width, null).Text("1M", textAlignment: TextAlignment.RIGHT).Newline()
            .Draw();
    }
}
/* MAIN */
