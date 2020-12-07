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
