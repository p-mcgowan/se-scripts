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
