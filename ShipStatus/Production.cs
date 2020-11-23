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
