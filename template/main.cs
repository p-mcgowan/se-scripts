List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
Dictionary<string, DrawingSurface> drawables = new Dictionary<string, DrawingSurface>();
Template template;
string templateStrings =
@"This is a test {no.registered.method}
This is another line, below is
a registered method returning Random
{test.random:min=0;max=10}

does this still print
{text:colour=0,0,100; i'm blue, abadee abadaa}
{text:colour=red; some like it red}
???";


Random random = new Random();

public void Random(DrawingSurface ds, string text, Dictionary<string, string> opts) {
    int min = Util.ParseInt(opts.Get("min", "0"), 0);
    int max = Util.ParseInt(opts.Get("max", "100"), 100);
    ds.Text($"{this.random.Next(min, max)}");
}

public Program() {
    template = new Template(this);
    template.PreRender("the_same_text_string_for_everything", templateStrings);
    blocks.Clear();
    GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(blocks);
    foreach (IMyTextSurfaceProvider block in blocks) {
        for (int i = 0; i < block.SurfaceCount; i++) {
            IMyTextSurface surface = block.GetSurface(i);
            string name = $"{((IMyTerminalBlock)block).CustomName} <{i}>";

            drawables.Add(name, new DrawingSurface(surface, this, name));

            // template.PreRender(name, block.CustomData);
        }
    }

    template.RegisterRenderAction("test.random", Random);
}

public void Main(string argument, UpdateType updateSource) {
    foreach (KeyValuePair<string, DrawingSurface> drawable in drawables) {
        template.Render(drawable.Value, "the_same_text_string_for_everything");
    }
}
/* MAIN */
