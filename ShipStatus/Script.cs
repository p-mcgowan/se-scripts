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

[Programmable block <0>]
output=
|Jump drives: {power.jumpDrives}
|{power.jumpBar}
|Batteries: {power.batteries}
|{power.batteryBar}
|Solar: {power.solars}
|Energy IO: {power.io}
|{power.energyio}
|Reactors: {power.reactors}
|
|Ship status: {health.status}
|{health.blocks}
|
|{production.mode}
|{production.blocks}
|
|Cargo: {cargo.stored} / {cargo.cap}
|{cargo.bar}
|{cago.items}
healthIgnore=Wheel
healthIgnoreMode=merge

[Status panel]
output=
|health
healthIgnore=
healthIgnoreMode=replace
*/

List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
List<string> strings = new List<string>();
MyIni ini = new MyIni();
SurfaceManager surfaceManager;
Template template = new Template();

// Dictionary<string, List<IMyTextSurface>> programOutputs = new Dictionary<string, List<IMyTextSurface>>();
// public string[] programKeys = { "AIRLOCK", "BLOCK_HEALTH", "CARGO", "CARGO_CAP", "CARGO_CAP_STYLE", "CARGO_LIGHT", "HEALTH_IGNORE", "INPUT", "JUMP_BAR", "POWER", "POWER_BAR", "PRODUCTION" };

public class SurfaceManager: Graphics {
    public Program program;
    public Dictionary<string, string> panelTemplates;

    // drawables dict<panelName, drawable>

    public SurfaceManager(Program program) {
        this.program = program;
        this.panelTemplates = new Dictionary<string, string>();
    }

    // public MyIniValue GetIni(string surface, string key) {
    //     return this.program.ini.Get(surface, key);
    // }

    // public void FormatOutput(string surfaceId) {
    //     if (this.GetIni(surfaceId, "output") == null) {
    //         return;
    //     }

    //     strings.Clear();
    //     ini.Get(name, "output").GetLines(strings);
    //     foreach (var line in strings) {
    //         var propertyB = classB.GetType().GetProperty(y);
    //         var propertyA = classA.GetType().GetProperty(x);
    //     }
    // }
}

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

public bool ParseCustomData() {
    MyIniParseResult result;
    if (!ini.TryParse(Me.CustomData, out result)) {
        Echo($"Failed to parse config:\n{result}");
        return false;
    }

    surfaceManager = new SurfaceManager(this);
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
            surfaceManager.panelTemplates.Add(name, tpl.ToString());
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
    //         graphics.drawables.Add($"{((IMyTerminalBlock)block).CustomName} <{i}>", new DrawingSurface(surface, this));
    //     }
    // }
    return true;
}

public Program() {
    // cargo = new List<IMyTerminalBlock>();
    // items = new List<MyInventoryItem>();
    // Runtime.UpdateFrequency = UpdateFrequency.Update100;

    if (!ParseCustomData()) {
        Runtime.UpdateFrequency &= UpdateFrequency.None;
        Echo("Failed to parse custom data");
        return;
    }
    // airlocks on 10
    Runtime.UpdateFrequency = UpdateFrequency.Update100 | UpdateFrequency.Update10;

    // CargoStatus cargoStatus = new CargoStatus(this);
    template.program = this;
    powerDetails = new PowerDetails(this);
    cargoStatus = new CargoStatus(this, template);

    blocks.Clear();
    GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(blocks);
    foreach (IMyTextSurfaceProvider block in blocks) {
        for (int i = 0; i < block.SurfaceCount; i++) {
            IMyTextSurface surface = block.GetSurface(i);
            surfaceManager.drawables.Add($"{((IMyTerminalBlock)block).CustomName} <{i}>", new DrawingSurface(surface, this));
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
    // Echo(power);
    // Echo(cargo);

    foreach (string key in surfaceManager.drawables.Keys) {
        if (!surfaceManager.panelTemplates.ContainsKey(key)) {
            continue;
        }
        DrawingSurface ds = surfaceManager.drawables[key];
        template.Render(ref ds, surfaceManager.panelTemplates[key].Split('\n'));
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
// /*
//  * PRODUCTION
//  */
// List<MyProductionItem> productionItems = new List<MyProductionItem>();
// List<ProductionBlock> productionBlocks = new List<ProductionBlock>();

// public class ProductionBlock {
//     public Program program;
//     public double idleTime;
//     public IMyProductionBlock block;
//     public bool Enabled {
//         get { return block.Enabled; }
//         set { if (block.DefinitionDisplayNameText.ToString() == "Survival kit") { return; } block.Enabled = value; }
//     }

//     public ProductionBlock(Program _program, IMyProductionBlock _block) {
//         idleTime = -1;
//         block = _block;
//         program = _program;
//     }

//     public void GetQueue(ref List<MyProductionItem> productionItems) {
//         productionItems.Clear();
//         block.GetQueue(productionItems);
//     }

//     public bool IsIdle() {
//         string status = Status();
//         if (status == "Idle") {
//             idleTime = (idleTime == -1) ? Now() : idleTime;
//             return true;
//         } else if (status == "Blocked" && !block.Enabled) {
//             block.Enabled = true;
//         }
//         idleTime = -1;
//         return false;
//     }

//     public string IdleTime() {
//         return Util.TimeFormat(Now() - idleTime);
//     }

//     public string Status() {
//         if (block.IsQueueEmpty && !block.IsProducing) {
//             return "Idle";
//         } else if (block.IsProducing) {
//             return "Working";
//         } else if (!block.IsQueueEmpty && !block.IsProducing) {
//             return "Blocked";
//         }
//         return "???";
//     }

//     public double Now() {
//         return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
//     }
// }

// public void GetProductionBlocks(Program p) {
//     blocks.clear();
//     GridTerminalSystem.GetBlocksOfType<IMyProductionBlock>(blocks, b =>
//         b.IsSameConstructAs(Me) &&
//         (b is IMyAssembler || b is IMyRefinery) &&
//         !b.CustomName.Contains(PRODUCTION_IGNORE_STRING)
//     );
//     productionBlocks.Clear();
//     foreach (IMyProductionBlock block in (List<IMyProductionBlock>)blocks) {
//         productionBlocks.Add(new ProductionBlock(p, block));
//     }
//     productionBlocks = productionBlocks.OrderBy(b => b.block.CustomName).ToList();
// }

// public string DoProductionDetails(Program p) {
//     if (!productionBlocks.Any()) {
//         return;
//     }

//     bool allIdle = true;
//     string output = "";
//     int assemblers = 0;
//     int refineries = 0;
//     foreach (var block in productionBlocks) {
//         bool idle = block.IsIdle();
//         if (block.block.DefinitionDisplayNameText.ToString() != "Survival kit") {
//             allIdle = allIdle && idle;
//         }
//         if (idle) {
//             if (block.block is IMyAssembler) {
//                 assemblers++;
//             } else {
//                 refineries++;
//             }
//         }
//     }
//     double timeNow = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

//     if (allIdle) {
//         idleTime = (idleTime == 0 ? timeNow : idleTime);

//         if (timeDisabled == 0) {
//             foreach (var block in productionBlocks) {
//                 block.Enabled = false;
//             }
//             timeDisabled = timeNow;
//         } else {
//             if (!checking) {
//                 if (timeNow - lastCheck > PRODUCTION_CHECK_FREQ_MS)  {
//                     // We disabled them over PRODUCTION_CHECK_FREQ_MS ago, and need to check them
//                     foreach (var block in productionBlocks) {
//                         block.Enabled = true;
//                     }
//                     checking = true;
//                     lastCheck = timeNow;
//                     output = String.Format("Power saving mode {0} (checking)\n\n", Util.TimeFormat(timeNow - idleTime));
//                 }
//             } else {
//                 if (timeNow - lastCheck > PRODUCTION_ON_WAIT_MS) {
//                     // We waited 5 seconds and they are still not producing
//                     foreach (var block in productionBlocks) {
//                         block.Enabled = false;
//                     }
//                     checking = false;
//                     lastCheck = timeNow;
//                 } else {
//                     output = String.Format("Power saving mode {0} (checking)\n\n", Util.TimeFormat(timeNow - idleTime));
//                 }
//             }
//         }
//         if (output == "") {
//             output = String.Format("Power saving mode {0} (check in {1})\n\n",
//                 Util.TimeFormat(timeNow - idleTime),
//                 Util.TimeFormat(PRODUCTION_CHECK_FREQ_MS - (timeNow - lastCheck), true));
//         }
//     } else {
//         if (productionBlocks.Where(b => b.Status() == "Blocked").ToList().Any()) {
//             output += "Production Enabled (Halted)\n";
//         } else {
//             output += "Production Enabled\n";
//         }

//         // If any assemblers are on, make sure they are all on (master/slave)
//         if (assemblers > 0) {
//             foreach (var block in productionBlocks.Where(b => b.block is IMyAssembler).ToList()) {
//                 block.Enabled = true;
//             }
//         }

//         idleTime = 0;
//         timeDisabled = 0;
//         checking = false;
//     }

//     bool sep = false;
//     foreach (var block in productionBlocks) {
//         var idle = block.IsIdle();
//         if (!sep && block.block is IMyRefinery) {
//             output += '\n';
//             sep = true;
//         }
//         output += String.Format("{0}: {1} {2}\n", block.block.CustomName, block.Status(), (idle ? block.IdleTime() : ""));
//         if (!idle) {
//             block.GetQueue(productionItems);
//             foreach (MyProductionItem i in productionItems) {
//                 output += String.Format("  {0} x {1}\n", Util.FormatNumber(i.Amount), Util.ToItemName(i));
//             }
//         }
//     }

//     return output;
// }
// /* PRODUCTION */
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
    public Template template;
    public VRage.MyFixedPoint max;
    public VRage.MyFixedPoint vol;

    public string itemText;
    public float pct;

    public CargoStatus(Program program, Template template) {
        this.program = program;
        this.itemText = "";
        this.pct = 0f;
        this.cargo = new List<IMyTerminalBlock>();
        this.cargoItemCounts = new Dictionary<string, VRage.MyFixedPoint>();
        this.inventoryItems = new List<MyInventoryItem>();
        this.itemRegex = Util.Regex(".*/");
        this.ingotRegex = Util.Regex("Ingot/");
        this.oreRegex = Util.Regex("Ore/(?!Ice)");
        this.template = template;
        this.GetCargoBlocks();
        this.RegisterTemplateVars();
    }

    public void RegisterTemplateVars() {
        this.template.RegisterRenderAction("cargo.stored",
            (ref DrawingSurface ds, string text, string options) => ds.Text($"{Util.FormatNumber(1000 * this.vol)} L"));
        this.template.RegisterRenderAction("cargo.cap",
            (ref DrawingSurface ds, string text, string options) => ds.Text($"{Util.FormatNumber(1000 * this.max)} L"));
        this.template.RegisterRenderAction("cargo.bar", this.RenderPct);
        this.template.RegisterRenderAction("cago.items", this.RenderItems);
    }

    public void RenderPct(ref DrawingSurface ds, string text, string options = "") {
        Color colour = Color.Green;
        if (this.pct > 60) {
            colour = Color.Yellow;
        } else if (this.pct > 85) {
            colour = Color.Red;
        }
        ds.Bar(this.pct, fillColour: colour, text: Util.PctString(this.pct));
    }

    public void RenderItems(ref DrawingSurface ds, string text, string options = "") {
        foreach (var item in this.cargoItemCounts) {
            var fmtd = Util.FormatNumber(item.Value);
            ds.Text($"{item.Key}").SetCursor(ds.width, null).Text(fmtd, textAlignment: TextAlignment.RIGHT).Newline();
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
        if (!this.drawing) {
            this.DrawStart();
        }
        if (colour == null) {
            colour = this.surface.FontColor;
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

        this.cursor.X += this.charSizeInPx.X * text.Length + (float)Math.Ceiling((double)(text.Length / 2)) - 1f;

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
 * POWER
 */
PowerDetails powerDetails;
public class PowerDetails {
    public Program program;
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

    public PowerDetails(Program _program) {
        program = _program;
        powerProducerBlocks = new List<IMyPowerProducer>();
        jumpDriveBlocks = new List<IMyJumpDrive>();
        items = new List<MyInventoryItem>();
        jumpDrives = 0;
        jumpMax = 0f;
        jumpCurrent = 0f;
        batteries = 0;
        batteryMax = 0f;
        batteryCurrent = 0f;
        reactors = 0;
        reactorOutputMW = 0f;
        reactorUranium = 0;
        solars = 0;
        solarOutputMW = 0f;
        solarOutputMax = 0f;
        GetBlocks();
    }

    public void Clear() {
        jumpDrives = 0;
        jumpMax = 0f;
        jumpCurrent = 0f;
        batteries = 0;
        batteryMax = 0f;
        batteryCurrent = 0f;
        reactors = 0;
        reactorOutputMW = 0f;
        reactorUranium = 0;
        solars = 0;
        solarOutputMW = 0f;
        solarOutputMax = 0f;
    }

    public void GetBlocks() {
        powerProducerBlocks.Clear();
        program.GridTerminalSystem.GetBlocksOfType<IMyPowerProducer>(powerProducerBlocks, b => b.IsSameConstructAs(program.Me));
        jumpDriveBlocks.Clear();
        program.GridTerminalSystem.GetBlocksOfType<IMyJumpDrive>(jumpDriveBlocks, b => b.IsSameConstructAs(program.Me));
    }

    public float GetPercent(float current, float max) {
        if (max == 0) {
            return 0f;
        }
        return current / max;
    }

    public void Refresh() {
        Clear();

        foreach (IMyPowerProducer powerBlock in powerProducerBlocks) {
            if (powerBlock is IMyBatteryBlock) {
                batteries += 1;
                batteryCurrent += ((IMyBatteryBlock)powerBlock).CurrentStoredPower;
                batteryMax += ((IMyBatteryBlock)powerBlock).MaxStoredPower;
            } else if (powerBlock is IMyReactor) {
                reactors += 1;
                reactorOutputMW += ((IMyReactor)powerBlock).CurrentOutput;

                items.Clear();
                var inv = ((IMyReactor)powerBlock).GetInventory(0);
                inv.GetItems(items);
                for (var i = 0; i < items.Count; i++) {
                    reactorUranium += items[i].Amount;
                }
            } else if (powerBlock is IMySolarPanel) {
                solars += 1;
                solarOutputMW += ((IMySolarPanel)powerBlock).CurrentOutput;
                solarOutputMax += ((IMySolarPanel)powerBlock).MaxOutput;
            }
        }

        foreach (IMyJumpDrive jumpDrive in jumpDriveBlocks) {
            jumpDrives += 1;
            jumpCurrent += jumpDrive.CurrentStoredPower;
            jumpMax += jumpDrive.MaxStoredPower;
        }
    }

    public override string ToString() {
        return $"{jumpDrives} Jump drive{Util.Plural(jumpDrives, "", "s")}:\n" +
            $"{jumpCurrent} / {jumpMax}\n" +
            $"{batteries} Batter{Util.Plural(batteries, "y", "ies")}\n" +
            $"{batteryCurrent} / {batteryMax}\n" +
            $"{reactors} Reactor{Util.Plural(reactors, "", "s")}\n" +
            $"{reactorOutputMW} MW, {Util.FormatNumber(reactorUranium)} Fuel";
    }
}
/* POWER */
/*
 * UTIL
 */
public static class Util {
    public static System.Text.RegularExpressions.Regex surfaceExtractor =
        Util.Regex(@"\s<(\d+)>$", System.Text.RegularExpressions.RegexOptions.Compiled);

    public static string FormatNumber(VRage.MyFixedPoint input) {
        string fmt;
        int n = Math.Max(0, (int)input);
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
}
/* UTIL */
public class Token {
    public bool isText = true;
    public string value = null;
}

public delegate void Del(ref DrawingSurface ds, string token, string options = "");

public class Template {
    public Program program;
    public Dictionary<string, Del> methods;
    public System.Text.RegularExpressions.Regex tokenizer;
    public System.Text.RegularExpressions.Regex cmdSplitter;
    public System.Text.RegularExpressions.Match match;
    public Token token;
    public char[] splitSpace;

    public Template(Program program = null) {
        this.tokenizer = Util.Regex(@"(\{[^\}]+\}|[^\{]+)", System.Text.RegularExpressions.RegexOptions.Compiled);
        this.cmdSplitter = Util.Regex(@"(?<name>[^: ]+)(:(?<params>[^; ]+);? )(?<text>.*)", System.Text.RegularExpressions.RegexOptions.Compiled);
        this.token = new Token();
        this.methods = new Dictionary<string, Del>();
        this.program = program;
        this.RegisterRenderAction("text", this.RenderText);
        this.splitSpace = new[] { ' ' };
    }

    public void RegisterRenderAction(string key, Del callback) {
        this.methods[key] = callback;
    }

    public void RenderText(ref DrawingSurface ds, string text, string options = "") {
        Color? colour = null;
        if (options != "") {
            Dictionary<string, string> keyValuePairs = options.Split(';')
                .Select(value => value.Split('='))
                .ToDictionary(pair => pair[0], pair => pair[1]);

            if (keyValuePairs["colour"] != null) {
                var cols = keyValuePairs["colour"].Split(',').Select(value => Int32.Parse(value)).ToArray();
                colour = new Color(
                    cols.ElementAtOrDefault(0),
                    cols.ElementAtOrDefault(1),
                    cols.ElementAtOrDefault(2),
                    cols.Length > 3 ? cols[3] : 255
                );
            }
        }
        this.program.Echo($"c:{colour}");
        ds.Text(text, colour: colour);
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
                this.program.Echo($"err parsing token {e}");
                return false;
            }

            return true;
        } else {
            return false;
        }
    }

    public void Render(ref DrawingSurface ds, string[] templateStrings) {
        foreach (string line in templateStrings) {
            this.match = null;

            while (this.GetToken(line)) {
                if (this.token.isText) {
                    ds.Text(this.token.value);
                    continue;
                }

                string name = this.token.value;
                string opts = "";
                string text = "";
                System.Text.RegularExpressions.Match m = this.cmdSplitter.Match(this.token.value);
                if (m.Success) {
                    opts = m.Groups["params"].Value;
                    text = m.Groups["text"].Value;
                    name = m.Groups["name"].Value;
                }
                this.program.Echo($"name:{name},text:{text},opts:{opts}");

                // string[] parts = this.token.value.Split(this.splitSpace, 2);
                // string[] opts = this.token.value.Split(':');
                // this.program.Echo(String.Format("p1: {0}, p2?: {1}", parts[0], parts.Length > 1 ? parts[1] : "naw"));

                if (this.methods.ContainsKey(name)) {
                    this.methods[name](ref ds, text, opts);
                } else {
                    ds.Text($"{{{this.token.value}}}");
                }
            }
            ds.Newline();
        }
        ds.Draw();
    }
}
// /*
//  * BLOCK_HEALTH
//  */
// class BlockHealth {
//     public Program program;
//     public System.Text.RegularExpressions.Regex ignoreHealth;
//     public List<IMyTerminalBlock> blocks;

//     pubic BlockHealth(Program program) {
//         this.blocks = new List<IMyTerminalBlock>();
//         this.program = program;
//     }

//     public float GetHealth(IMyTerminalBlock block) {
//         IMySlimBlock slimblock = block.CubeGrid.GetCubeBlock(block.Position);
//         float MaxIntegrity = slimblock.MaxIntegrity;
//         float BuildIntegrity = slimblock.BuildIntegrity;
//         float CurrentDamage = slimblock.CurrentDamage;

//         return (BuildIntegrity - CurrentDamage) / MaxIntegrity;
//     }

//     public string DoBlockHealth() {
//         // System.Text.RegularExpressions.Regex ignoreHealth = null;
//         // if (settings[CFG.HEALTH_IGNORE] != "") {
//         //     string input = System.Text.RegularExpressions.Regex.Replace(settings[CFG.HEALTH_IGNORE], @"\s*,\s*", "|");
//         //     ignoreHealth = Regex(input);
//         // }
//         // CFG.HEALTH_IGNORE, "Hydrogen Thruster, Suspension"

//         this.blocks.Clear();
//         GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(this.blocks, b => b.IsSameConstructAs(Me));
//         string output = "";

//         // int chars;
//         // GetPanelWidthInChars(settings[CFG.BLOCK_HEALTH], out chars);

//         foreach (var b in this.blocks) {
//             if (this.ignoreHealth != null && this.ignoreHealth.IsMatch(b.CustomName)) {
//                 continue;
//             }

//             var health = this.GetHealth(b);
//             if (health != 1f) {
//                 if (CanWriteToSurface(settings[CFG.BLOCK_HEALTH])) {
//                     output += b.CustomName + " [" + Util.PctString(GetHealth(b)) + "]\n";
//                 }
//                 b.ShowOnHUD = true;
//             } else {
//                 b.ShowOnHUD = false;
//             }
//         }

//         if (output == "") {
//             output = "Ship status: No damage detected\n";
//         } else {
//             output = "Ship status: Damage detected\n" + output;
//         }

//         return output + '\n';
//     }
// }
// /* BLOCK_HEALTH */
