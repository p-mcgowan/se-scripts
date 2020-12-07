List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
Graphics graphics = new Graphics();
Template template;
string[] templateStrings = new string[] {
    "This is a test {no.registered.method}",
    "This is another line, below is ",
    "a registered method returning Random",
    "{test.random}"
};


class Test {
    private Random random;

    public Test() {
        this.random = new Random();
    }

    public void Random(DrawingSurface ds, string text) {
        ds.Text($"{this.random.Next(0, 100)}");
    }
}
Test test = new Test();

public Program() {
    blocks.Clear();
    GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(blocks);
    foreach (IMyTextSurfaceProvider block in blocks) {
        for (int i = 0; i < block.SurfaceCount; i++) {
            IMyTextSurface surface = block.GetSurface(i);
            graphics.drawables.Add($"{((IMyTerminalBlock)block).CustomName} <{i}>", new DrawingSurface(surface, this));
        }
    }
    template = new Template(this);
    template.RegisterRenderAction("test.random", test.Random);
}

public void Main(string argument, UpdateType updateSource) {
    foreach (string key in graphics.drawables.Keys) {
        DrawingSurface ds = graphics.drawables[key];
        ds.Text($"begin {key}").Newline();
        template.Render(ref ds, templateStrings);
    }
}
/* MAIN */
