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
