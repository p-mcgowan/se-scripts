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

public string[] programKeys = {
    "AIRLOCK",
    "BLOCK_HEALTH",
    "CARGO",
    "CARGO_CAP",
    "CARGO_CAP_STYLE",
    "CARGO_LIGHT",
    "HEALTH_IGNORE",
    "INPUT",
    "JUMP_BAR",
    "POWER",
    "POWER_BAR",
    "PRODUCTION"
};

MyIni ini = new MyIni();

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
/*
 * UTIL
 */
public static class Util {
    public static System.Text.RegularExpressions.Regex pnameSplitter =
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
// /**
//  * Airlock doors - Auto close doors and lock airlock pairs"
//  *
//  * By default, all doors with default names will auto close (see DOOR_MATCH).
//  * For airlock doors to pair together (lock when the other is open), give them the same name. This works for any number of doors.
//  * If you want to include all doors by default but exclude a few, name the doors so that they contain the DOOR_EXCLUDE tag.
//  */

// // Config vars
// const double TIME_OPEN = 750f;           // Duration before auto close (milliseconds)
// const string DOOR_MATCH = "Door(.*)";    // The name to match (Default will match regular doors).
//                                          // The capture group "(.*)" is used when grouping airlock doors.
// const string DOOR_EXCLUDE = "Hangar";    // The exclusion tag (can be anything).

// // Script vars
// Dictionary<string, Airlock> airlocks = new Dictionary<string, Airlock>();
// System.Text.RegularExpressions.Regex include = new System.Text.RegularExpressions.Regex(DOOR_MATCH, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
// System.Text.RegularExpressions.Regex exclude = new System.Text.RegularExpressions.Regex(DOOR_EXCLUDE, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

// // Utility class containing for each individual airlock
// //
// public class Airlock {
//     private List<IMyFunctionalBlock> blocks;
//     private double openTimer;

//     public Airlock(List<IMyFunctionalBlock> doors, IMyFunctionalBlock light = null) {
//         this.blocks = new List<IMyFunctionalBlock>(doors);
//         this.openTimer = -1;
//     }

//     private bool IsOpen(IMyFunctionalBlock door) {
//         return (door as IMyDoor).OpenRatio > 0;
//     }

//     private void Lock(List<IMyFunctionalBlock> doors = null) {
//         doors = doors ?? this.blocks;
//         foreach (var door in doors) {
//             (door as IMyDoor).Enabled = false;
//         }
//     }

//     private void Unlock(List<IMyFunctionalBlock> doors = null) {
//         doors = doors ?? this.blocks;
//         foreach (var door in doors) {
//             (door as IMyDoor).Enabled = true;
//         }
//     }

//     private void OpenClose(string action, IMyFunctionalBlock door1, IMyFunctionalBlock door2 = null) {
//         (door1 as IMyDoor).ApplyAction(action);
//         if (door2 != null) {
//             (door2 as IMyDoor).ApplyAction(action);
//         }
//     }
//     private void Open(IMyFunctionalBlock door1, IMyFunctionalBlock door2 = null) {
//         OpenClose("Open_On", door1, door2);
//     }
//     private void OpenAll() {
//         foreach (var door in this.blocks) {
//             OpenClose("Open_On", door);
//         }
//     }
//     private void Close(IMyFunctionalBlock door1, IMyFunctionalBlock door2 = null) {
//         OpenClose("Open_Off", door1, door2);
//     }
//     private void CloseAll() {
//         foreach (var door in this.blocks) {
//             OpenClose("Open_Off", door);
//         }
//     }

//     public bool Test() {
//         int openCount = 0;
//         var areClosed = new List<IMyFunctionalBlock>();
//         var areOpen = new List<IMyFunctionalBlock>();
//         foreach (var door in this.blocks) {
//             if (this.IsOpen(door)) {
//                 openCount++;
//                 areOpen.Add(door);
//             } else {
//                 areClosed.Add(door);
//             }
//         }
//         if (areOpen.Count > 0) {
//             if (this.openTimer == -1) {
//                 this.openTimer = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
//                 this.Lock(areClosed);
//                 this.Unlock(areOpen);
//             } else if (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - this.openTimer > TIME_OPEN) {
//                 this.CloseAll();
//             }
//         } else {
//             this.Unlock();
//             this.openTimer = -1;
//         }

//         return true;
//     }
// }

// // Map block list into hash
// //
// public void GetMappedAirlocks() {
//     var airlockBlocks = new List<IMyTerminalBlock>();
//     GridTerminalSystem.GetBlocksOfType<IMyDoor>(airlockBlocks, door => door.CubeGrid == Me.CubeGrid);

//     // Parse into hash (identifier => List(door)), where name is "Door <identifier>"
//     var locationToAirlockMap = new Dictionary<string, List<IMyFunctionalBlock>>();

//     // Get all door blocks
//     foreach (var block in airlockBlocks) {
//         var match = include.Match(block.CustomName);
//         var ignore = exclude.Match(block.CustomName);
//         if (ignore.Success) { continue; }
//         if (!match.Success) {
//             continue;  // TODO: lights
//         }
//         var key = match.Groups[1].ToString();
//         if (!locationToAirlockMap.ContainsKey(key)) {
//             locationToAirlockMap.Add(key, new List<IMyFunctionalBlock>());
//         }
//         locationToAirlockMap[key].Add(block as IMyFunctionalBlock);
//     }
//     foreach (var locAirlock in locationToAirlockMap) {
//         airlocks.Add(locAirlock.Key, new Airlock(locAirlock.Value));
//     }
// }
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

    public string bar;
    public string itemText;
    public float pct;

    public CargoStatus(Program _program) {
        program = _program;
        itemText = "";
        pct = 0f;
        cargo = new List<IMyTerminalBlock>();
        cargoItemCounts = new Dictionary<string, VRage.MyFixedPoint>();
        inventoryItems = new List<MyInventoryItem>();
        itemRegex = Util.Regex(".*/");
        ingotRegex = Util.Regex("Ingot/");
        oreRegex = Util.Regex("Ore/(?!Ice)");
        GetCargoBlocks();
    }

    public void Clear() {
        itemText = "";
        pct = 0f;
        cargo.Clear();
        cargoItemCounts.Clear();
        inventoryItems.Clear();
    }

    public void GetCargoBlocks() {
        program.GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(cargo, c =>
            c.IsSameConstructAs(program.Me) &&
            (c is IMyCargoContainer || c is IMyShipDrill || c is IMyShipConnector)
            // (c is IMyCargoContainer || c is IMyShipDrill || c is IMyShipConnector || c is IMyShipWelder || c is IMyShipGrinder)
        );
    }

    public void Refresh() {
        Clear();

        VRage.MyFixedPoint max = 0;
        VRage.MyFixedPoint vol = 0;

        foreach (var c in cargo) {
            var inv = c.GetInventory(0);
            vol += inv.CurrentVolume;
            max += inv.MaxVolume;

            inventoryItems.Clear();
            inv.GetItems(inventoryItems);
            for (var i = 0; i < inventoryItems.Count; i++) {
                string fullName = inventoryItems[i].Type.ToString();
                string itemName = itemRegex.Replace(fullName, "");
                if (ingotRegex.IsMatch(fullName)) {
                    itemName += " Ingot";
                } else if (oreRegex.IsMatch(fullName)) {
                    itemName += " Ore";
                }

                var itemQty = inventoryItems[i].Amount;
                if (!cargoItemCounts.ContainsKey(itemName)) {
                    cargoItemCounts.Add(itemName, itemQty);
                } else {
                    cargoItemCounts[itemName] = cargoItemCounts[itemName] + itemQty;
                }
            }
        }

        pct = (float)vol / (float)max;
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
        foreach (var item in cargoItemCounts) {
            var fmtd = Util.FormatNumber(item.Value);
            itemText += $"{item.Key}:{fmtd},";
        }
        return itemText;
    }

    public void Draw(IMyTextSurface surface) {
        //todo
    }
}
/* CARGO */
// // Show damaged blocks
// public float GetHealth(IMyTerminalBlock block) {
//     IMySlimBlock slimblock = block.CubeGrid.GetCubeBlock(block.Position);
//     float MaxIntegrity = slimblock.MaxIntegrity;
//     float BuildIntegrity = slimblock.BuildIntegrity;
//     float CurrentDamage = slimblock.CurrentDamage;

//     return (BuildIntegrity - CurrentDamage) / MaxIntegrity;
// }

// public string DoBlockHealth() {
//     if (!CanWriteToSurface(settings[CFG.BLOCK_HEALTH])) {
//         return null;
//     }

//     System.Text.RegularExpressions.Regex ignoreHealth = null;
//     if (settings[CFG.HEALTH_IGNORE] != "") {
//         string input = System.Text.RegularExpressions.Regex.Replace(settings[CFG.HEALTH_IGNORE], @"\s*,\s*", "|");
//         ignoreHealth = Regex(input);
//     }
//     // CFG.HEALTH_IGNORE, "Hydrogen Thruster, Suspension"

//     var blocks = new List<IMyTerminalBlock>();
//     GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocks, b => b.CubeGrid == Me.CubeGrid);
//     string output = "";

//     int chars;
//     GetPanelWidthInChars(settings[CFG.BLOCK_HEALTH], out chars);

//     foreach (var b in blocks) {
//         if (ignoreHealth != null && ignoreHealth.IsMatch(b.CustomName)) {
//             continue;
//         }

//         var health = GetHealth(b);
//         if (health != 1f) {
//             if (CanWriteToSurface(settings[CFG.BLOCK_HEALTH])) {
//                 output += b.CustomName + " [" + Util.PctString(GetHealth(b)) + "]\n";
//             }
//             b.ShowOnHUD = true;
//         } else {
//             b.ShowOnHUD = false;
//         }
//     }

//     if (output == "") {
//         output = "Ship status: No damage detected\n";
//     } else {
//         output = "Ship status: Damage detected\n" + output;
//     }

//     return output + '\n';
// }
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
