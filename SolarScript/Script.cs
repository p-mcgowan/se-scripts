// Isy's Solar Alignment Script
// ============================
// Version: 4.0.4
// Date: 2018-04-30

// =======================================================================================
//                                                                            --- Configuration ---
// =======================================================================================

// --- Essential Configuration ---
// =======================================================================================

// Name of the group with all the solar related rotors (not needed in gyro mode)
string rotorGroupName = "Solar Rotors";

// By enabling gyro mode, the script will no longer use rotors but all gyroscopes on the grid instead.
// This mode only makes sense when used on a SHIP in SPACE. Gyro mode deactivates the following
// features: night mode, rotate to sunrise, time calculation and triggering external timer blocks.
bool useGyroMode = false;

// Name of the reference group for gyro mode. Put your main cockpit or flight seat in this group!
string referenceGroupName = "Solar Reference";


// --- Rotate to sunrise --- 
// =======================================================================================

// Rotate the panels towards the sunrise during the night? (Possible values: true | false, default: true)
// The angle is figured out automatically based on the first lock of the day.
// If you want to set the angles yourself, set manualAngle to true and adjust the angles to your likings.
bool rotateToSunrise = true;
bool manualAngle = false;
int manualAngleVertical = 0;
int manualAngleHorizontal = 0;


// --- Reactor fallback --- 
// =======================================================================================

// With this option, you can enable your reactors as a safety fallback, if not enough power is available
// to power all your machines or if the battery charge gets low. By default, all reactors on the same grid
// will be used. If you only want to use specific ones, put their names or group in the list.
// Example: string[] fallbackReactors = { "Small Reactor 1", "Base reactor group", "Large Reactor" };
bool useReactorFallback = false;
string[] fallbackReactors = { };

// Activation conditions
bool activateOnLowBattery = true;
bool activateOnOverload = true;

// Tresholds for the reactor to kick in in percent
double lowBatteryPercentage = 10;
double overloadPercentage = 90;


// --- Base Light Management ---
// =======================================================================================

// Enable base light management? (Possible values: true | false, default: false)
// Lights will be turned on/off based on daytime.
bool baseLightManagement = false;

// Simple mode: toggle lights based on max. solar output (percentage). Time based toggle will be deactivated.
bool simpleMode = false;
int simpleThreshold = 50;

// Define the times when your lights should be turned on or off. If simple mode is active, this does nothing.
int lightOffHour = 8;
int lightOnHour = 18;

// To only toggle specific lights, declare groups for them.
// Example: string[] baseLightGroups = { "Interior Lights", "Spotlights", "Hangar Lights" };
string[] baseLightGroups = { };


// --- LCD panels ---
// =======================================================================================

// List of LCD Panels to display various information (single Names or group names).
// You can enable or disable specific informations on the LCD by editing its custom data.
// Example: string[] lcdNames = { "LCD Solar Alignment", "LCD 2", "LCD Solar Output" };
string[] lcdNames = { "LCD Solar Alignment" };


// --- Corner LCD panels ---
// =======================================================================================

// List of corner LCD Panels to display basic output information (single Names or group names).
// Example: string[] cornerLcdNames = { "Corner LCD 1", "Corner LCD 2", "LCD Solar Output" };
// Optional: Put keywords in the LCD's custom data for different stats: time, battery, oxygen
string[] cornerLcdNames = { };


// --- Terminal statistics ---
// =======================================================================================

// The script can display informations in the names of the used blocks. The shown information is a percentage of
// the current efficiency (solar panels and oxygen farms) or the fill level (batteries and tanks).
// You can enable or disable single statistics or disable all using the master switch below.
bool enableTerminalStatistics = true;

bool showSolarStats = true;
bool showBatteryStats = true;
bool showOxygenFarmStats = true;
bool showOxygenTankStats = true;


// --- External timer blocks ---
// =======================================================================================

// Trigger external timer blocks at specific events? (action "Start" will be applied which takes the delay into account)
// Events can be: "sunrise", "sunset", a time like "15:00" or a number for every X seconds
// Every event needs a timer block name in the exact same order as the events.
// Calling the same timer block with multiple events requires it's name multiple times in the timers list!
// Example:
// string[] events = { "sunrise", "sunset", "15:00", "30" };
// string[] timers = { "Timer 1", "Timer 1", "Timer 2", "Timer 3" };
// This will trigger "Timer 1" at sunrise and sunset, "Timer 2" at 15:00 and "Timer 3" every 30 seconds.
bool triggerTimerBlock = false;
string[] events = { };
string[] timers = { };


// --- Settings for enthusiasts ---
// =======================================================================================

// Percentage of the last locked output where the rotors should search for a new best output (default: 98)
double searchPercentage = 98;
double searchPercentageGyro = 95;

// Percentage of the max detected output where the script starts night mode (default: 10)
double nightPercentage = 10;

// Percentage of the max detected output where the script detects night for time calculation (default: 50)
double nightTimePercentage = 50;

// Rotor speeds for rotating to sunrise or rotating to a user defined angle
const float rotorSpeed = 0.2f;
const float rotorSpeedFast = 1.0f;

// Should the script set the preferred inertia tensor automatically?
bool setInertiaTensor = true;

// Min gyro RPM, max gyro RPM and gyro power for gyro mode
const double minGyroRPM = 0.1;
const double maxGyroRPM = 1;
const float gyroPower = 1f;

// Debugging
string debugLcd = "LCD Solar Alignment Debugging";
bool showInstructionCount = true;
bool showExecutionTime = true;
bool showScriptExecutionThread = true;
bool showBlockCounts = true;


// =======================================================================================
//                                                                      --- End of Configuration ---
//                                                        Don't change anything beyond this point!
// =======================================================================================

// Removed whitespaces at the front to save space

// Output variables
double rotorOutput = 0;
double maxOutputAP = 0;
double maxOutputAPLast = 0;
double maxDetectedOutputAP = 0;
double currentOutputAP = 0;

// Lists
List<IMyMotorStator> rotors = new List<IMyMotorStator>();
List<IMyMotorStator> vRotors = new List<IMyMotorStator>();
List<IMyMotorStator> hRotors = new List<IMyMotorStator>();
List<IMyGyro> gyros = new List<IMyGyro>();
List<IMyTextPanel> lcds = new List<IMyTextPanel>();
List<IMyTextPanel> cornerLcds = new List<IMyTextPanel>();
List<IMyInteriorLight> lights = new List<IMyInteriorLight>();
List<IMyReflectorLight> spotlights = new List<IMyReflectorLight>();
List<IMyReactor> reactors = new List<IMyReactor>();

// Rotor variables
bool hasSolarsOrOxygenFarms = false;
List<IMySolarPanel> solarPanels = new List<IMySolarPanel>();
List<IMyCubeGrid> scannedGrids = new List<IMyCubeGrid>();
int solarPanelsCount = 0;
bool nightModeActive = false;
bool sunrisePosReached = false;
int nightModeTimer = 30;
int realignTimer = 90;
bool rotateAllInit = true;
string[] defaultCustomDataRotor = new string[] {
	"output=0",
	"outputLast=0",
	"outputLocked=0",
	"outputMax=0",
	"outputMaxAngle=0",
	"direction=1",
	"directionChanged=0",
	"directionTimer=0",
	"allowRotation=1",
	"timeSinceRotation=0",
	"firstLockOfDay=0",
	"sunriseAngle=0"
};

// Gyro variables
List<IMyCockpit> gyroReference = new List<IMyCockpit>();
double outputLockedRoll = 0;
double outputLockedPitch = 0;
double directionRoll = 1;
double directionPitch = 1;
bool directionChangedRoll = false;
bool directionChangedPitch = false;
double directionTimerRoll = 0;
double directionTimerPitch = 0;
bool allowRoll = true;
bool allowPitch = true;
double timeSinceRoll = 0;
double timeSincePitch = 0;

// Battery variables
List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
double batteriesCurrentInput = 0;
double batteriesMaxInput = 0;
double batteriesCurrentOutput = 0;
double batteriesMaxOutput = 0;
double batteriesPower = 0;
double batteriesMaxPower = 0;

// Oxygen farm and tank variables
List<IMyOxygenFarm> oxygenFarms = new List<IMyOxygenFarm>();
List<IMyGasTank> oxygenTanks = new List<IMyGasTank>();
double oxygenFarmEfficiency = 0;
double oxygenTankCapacity = 0;
double oxygenTankFillLevel = 0;
int oxygenFarmsCount = 0;

// String variables for showing the information
string maxOutputAPStr = "0 kW";
string maxDetectedOutputAPStr = "0 kW";
string currentOutputAPStr = "0 kW";
string batteriesCurrentInputStr = "0 kW";
string batteriesMaxInputStr = "0 kW";
string batteriesCurrentOutputStr = "0 kW";
string batteriesMaxOutputStr = "0 kW";
string batteriesPowerStr = "0 kW";
string batteriesMaxPowerStr = "0 kW";
string oxygenTankCapacityStr = "0 L";
string oxygenTankFillLevelStr = "0 L";

// Information strings
string currentOperation = "Checking setup...";
string currentOperationInfo;
string workingIndicator = "/";
int workingCounter = 0;

// Variables for time measuring
int dayTimer = 0;
int safetyTimer = 270;
const int dayLengthDefault = 7200;
int dayLength = dayLengthDefault;
const int sunSetDefault = dayLengthDefault / 2;
int sunSet = sunSetDefault;

// LCD variables
Dictionary<long, List<int>> scroll = new Dictionary<long, List<int>>();
string[] defaultCustomDataLCD = {
	"showCurrentOperation=true",
	"showSolarStats=true",
	"showBatteryStats=true",
	"showOxygenStats=true",
	"showLocationTime=true"
};


// Error handling
string error, warning;
int errorCount = 0;
int warningCount = 0;

// First run variable
bool firstRun = true;

// Command line parameters
string action = "align";
int actionTimer = 3;
bool pause = false;
string rotateMode = "both";
bool pauseAfterRotate = false;
double rotateHorizontalAngle = 0;
double rotateVerticalAngle = 0;


// Script timing variables
DateTime lastRuntime = DateTime.Now;
int execCounter = 1;

// Debugging
double maxInstructions, maxRuntime;
int avgCounter = 0;
List<int> instructions = new List<int>(new int[100]);
List<double> runtime = new List<double>(new double[100]);

// Pre-Run preparations
public Program()
{
	// Load variables out of the programmable block's custom data field
	Load();

	// Settings for nerds recalculation
	searchPercentage = (searchPercentage % 100) / 100;
	searchPercentageGyro = (searchPercentageGyro % 100) / 100;
	nightPercentage = (nightPercentage % 100) / 100;
	nightTimePercentage = (nightTimePercentage % 100) / 100;

	// Set UpdateFrequency for starting the programmable block over and over again
	Runtime.UpdateFrequency = UpdateFrequency.Update1;
}


/// <summary>
/// Main method
/// </summary>
/// <param name="parameter">Any of these command line arguments: "", "pause", "realign, "reset", "rotate h double", "rotate v double", "rotate double double"</param>
void Main(string parameter)
{
	// Store the parameter
	if (parameter != "") {
		action = parameter.ToLower();
		execCounter = 4;
	}

	// Stop all rotors and create initial information
	if (firstRun) {
		GetBlocks();
		StopAll();
		RemoveTerminalStatistics();
		firstRun = false;
	}

	// Script timing
	if ((DateTime.Now - lastRuntime).TotalMilliseconds < 200) {
		return;
	} else {
		lastRuntime = DateTime.Now;
	}

	// Execute on the 1st tick of the whole cycle
	if (execCounter == 1) {
		errorCount = 0;
		warningCount = 0;

		// Get all blocks
		GetBlocks();
	}

	// Execute on the 2nd tick of the whole cycle
	if (execCounter == 2 && error == null) {
		// Get the output of all measured blocks
		GetOutput();
	}

	// Execute on the 3rd tick of the whole cycle
	if (execCounter == 3 && !useGyroMode && error == null) {
		// Time Calculation
		TimeCalculation();

		// Switch the lights if base light management is activated
		if (baseLightManagement) LightManagement();

		// Trigger a timer block if triggerTimerBlock is true
		if (triggerTimerBlock) TriggerExternalTimerBlock();
	}

	// Execute on the 4th tick of the whole cycle
	if (execCounter == 4 && error == null) {
		// Either execute argument or use main rotation logic
		if (!ExecuteArgument(action)) {
			if (useGyroMode) {
				RotationLogicGyro();
			} else {
				RotationLogic();
			}
		}
	}

	// Execute on the 5th tick of the whole cycle
	if (execCounter == 5 && error == null) {
		// Reactor fallback
		if (useReactorFallback) ReactorFallback();

		// Update variables for the next run
		foreach (var rotor in rotors) {
			double outputLast = ReadCustomData(rotor, "output");
			WriteCustomData(rotor, "outputLast", outputLast);
		}
		maxOutputAPLast = maxOutputAP;

		// Save variables into the programmable block's custom data field
		Save();
	}

	// Write the information to various channels
	Echo(CreateInformation(true));
	WriteLCD();
	WriteCornerLCD();
	WriteDebugLCD();

	// Update the script execution counter
	if (execCounter >= 5) {
		// Reset the counter
		execCounter = 1;

		// Reset errors and warnings if none were counted
		if (errorCount == 0) {
			error = null;
		}

		if (warningCount == 0) {
			warning = null;
		}
	} else {
		execCounter++;
	}

	// Update the working counter for the LCDs
	if (workingCounter >= 3) {
		workingCounter = 0;
	} else {
		workingCounter++;
	}
}


/// <summary>
/// Gets all blocks that should be used by the script and sorts them into their respective lists
/// </summary>
void GetBlocks()
{
	// LCDs
	lcds.Clear();

	// Cycle through all the items in regularLcds to find groups or LCDs    
	foreach (var item in lcdNames) {
		// If the item is a group, get the LCDs and join the list with lcds list
		var lcdGroup = GridTerminalSystem.GetBlockGroupWithName(item);
		if (lcdGroup != null) {
			var tempLcds = new List<IMyTextPanel>();
			lcdGroup.GetBlocksOfType<IMyTextPanel>(tempLcds);
			lcds.AddRange(tempLcds);
			// Else try adding a single LCD
		} else {
			IMyTextPanel regularLcd = GridTerminalSystem.GetBlockWithName(item) as IMyTextPanel;
			if (regularLcd != null) {
				lcds.Add(regularLcd);
			}
		}
	}

	// Gyro Mode
	if (useGyroMode) {
		// Create error if the grid is stationary
		if (Me.CubeGrid.IsStatic) {
			CreateError("The grid is stationary!\nConvert it to a ship in the Info tab!");
			return;
		}

		// Get reference group
		var referenceGroup = GridTerminalSystem.GetBlockGroupWithName(referenceGroupName);
		if (referenceGroup != null) {
			referenceGroup.GetBlocksOfType<IMyCockpit>(gyroReference);

			if (gyroReference.Count == 0) {
				CreateError("There are no cockpits or flight seats in the reference group:\n'" + referenceGroupName + "'");
				return;
			}
		} else {
			CreateError("Reference group not found!\nPut your main cockpit or flight seat in a group called '" + referenceGroupName + "'!");
			return;
		}

		// Get gyroscopes
		GridTerminalSystem.GetBlocksOfType<IMyGyro>(gyros, g => g.CubeGrid == Me.CubeGrid);

		// Create error if no gyroscopes were found
		if (gyros.Count == 0) {
			CreateError("No gyroscopes found!");
			return;
		}

		// Get solar panels and oxygen farms
		GridTerminalSystem.GetBlocksOfType<IMySolarPanel>(solarPanels);
		GridTerminalSystem.GetBlocksOfType<IMyOxygenFarm>(oxygenFarms);

	}

	// Rotor Mode
	if (!useGyroMode) {
		// Get rotors
		var rotorGroup = GridTerminalSystem.GetBlockGroupWithName(rotorGroupName);

		// If present, copy rotors into rotors list, else throw message
		if (rotorGroup != null) {
			rotorGroup.GetBlocksOfType<IMyMotorStator>(rotors);

			// Create error if no rotor was in the group
			if (rotors.Count == 0) {
				CreateError("There are no rotors in the rotor group:\n'" + rotorGroupName + "'");
				return;
			}
		} else {
			CreateError("Rotor group not found:\n'" + rotorGroupName + "'");
			return;
		}

		var grids = new List<IMyCubeGrid>();

		// Get unique grids and prepare the rotors
		foreach (var rotor in rotors) {
			if (!grids.Exists(grid => grid == rotor.CubeGrid)) {
				grids.Add(rotor.CubeGrid);
			}

			// Set basic stats for every rotor
			rotor.Torque = float.MaxValue;

			// Give warning, if the owner is different
			if (rotor.OwnerId != Me.OwnerId) {
				CreateWarning("'" + rotor.CustomName + "' has a different owner!\nAll blocks should have the same owner!");
			}
		}

		// Find vertical and horizontal rotors and add them to their respective list
		vRotors.Clear();
		hRotors.Clear();
		foreach (var rotor in rotors) {
			if (grids.Exists(grid => grid == rotor.TopGrid)) {
				vRotors.Add(rotor);
			} else {
				hRotors.Add(rotor);

				// Set inertia tensor for horizontal rotors that are not on the main grid and if active in the config
				if (rotor.CubeGrid != Me.CubeGrid && setInertiaTensor) {
					try {
						rotor.SetValueBool("ShareInertiaTensor", true);
					} catch (Exception) {
						// Ignore if it fails on DS
					}
				}
			}
		}

		// Check, if a U-Shape is used and rebuild the list with only one of the connected rotors
		List<IMyMotorStator> hRotorsTemp = new List<IMyMotorStator>();
		hRotorsTemp.AddRange(hRotors);
		hRotors.Clear();
		bool addRotor;

		foreach (var rotorTemp in hRotorsTemp) {
			addRotor = true;

			foreach (var rotor in hRotors) {
				if (rotor.TopGrid == rotorTemp.TopGrid) {
					rotorTemp.Enabled = false;
					rotorTemp.RotorLock = false;
					rotorTemp.TargetVelocityRPM = 0f;
					rotorTemp.Torque = 0f;
					rotorTemp.BrakingTorque = 0f;
					addRotor = false;
					break;
				}
			}

			if (addRotor) hRotors.Add(rotorTemp);
		}

		// Get solar panels and oxygen farms
		solarPanels.Clear();
		oxygenFarms.Clear();

		// Cycle through all hRotors and check if they have solar panels or oxygen farms and sum up their output
		foreach (var hRotor in hRotors) {
			rotorOutput = 0;
			scannedGrids.Clear();
			hasSolarsOrOxygenFarms = false;

			// Find all solar panels and oxygen farms that are on this rotor
			ScanRecursive(hRotor);

			// Print a warning if a rotor has neither a solar panel nor an oxygen farm
			if (!hasSolarsOrOxygenFarms) {
				CreateWarning("'" + hRotor.CustomName + "' can't see the sun!\nAdd a solar panel or oxygen farm to it!");
			}

			// Write the output in the custom data field
			WriteCustomData(hRotor, "output", rotorOutput);

			// If it's higher than the max detected output, write it, too and also remember the rotor's current angle
			if (rotorOutput > ReadCustomData(hRotor, "outputMax")) {
				WriteCustomData(hRotor, "outputMax", rotorOutput);
				WriteCustomData(hRotor, "outputMaxAngle", GetAngle(hRotor));
			}
		}

		// Read and store the combined output of all hRotors that are on top of vRotors
		foreach (var vRotor in vRotors) {
			double output = 0;

			foreach (var hRotor in hRotors) {
				if (hRotor.CubeGrid == vRotor.TopGrid) {
					output += ReadCustomData(hRotor, "output");
				}
			}

			// Write the output in the custom data field
			WriteCustomData(vRotor, "output", output);

			// If it's higher than the max detected output, write it, too and also remember the rotor's current angle
			if (output > ReadCustomData(vRotor, "outputMax")) {
				WriteCustomData(vRotor, "outputMax", output);
				WriteCustomData(vRotor, "outputMaxAngle", GetAngle(vRotor));
			}
		}
	}

	// If solar panels or oxygen farm count changed, reset maxOutput of all rotors
	if (solarPanelsCount != solarPanels.Count || oxygenFarmsCount != oxygenFarms.Count) {
		foreach (var rotor in rotors) {
			WriteCustomData(rotor, "outputMax", 0);
		}
		maxDetectedOutputAP = 0;

		// Update solar panels and oxygen farms count
		solarPanelsCount = solarPanels.Count;
		oxygenFarmsCount = oxygenFarms.Count;

		CreateError("Amount of solar panels or oxygen farms changed!\nRestarting..");
		return;
	}

	// Show error if no solar panels or oxygen farms were found
	if (solarPanels.Count == 0 && oxygenFarms.Count == 0) {
		CreateError("No solar panels or oxygen farms found!\nHow should I see the sun now?");
		return;
	}

	// Batteries
	batteries.Clear();
	GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(batteries, b => b.CubeGrid == Me.CubeGrid);

	// Show warning if no battery was found
	if (batteries.Count == 0) {
		CreateWarning("No batteries found!\nDon't you want to store your Power?");
	}

	// Oxygen tanks
	oxygenTanks.Clear();
	GridTerminalSystem.GetBlocksOfType<IMyGasTank>(oxygenTanks, t => !t.BlockDefinition.SubtypeId.Contains("Hydrogen") && t.CubeGrid == Me.CubeGrid);

	// Reactors
	if (useReactorFallback) {
		reactors.Clear();

		// Cycle through all the items in regularLcds to find groups or LCDs    
		foreach (var item in fallbackReactors) {
			// If the item is a group, get the reactors and join the list with reactors list
			var reactorGroup = GridTerminalSystem.GetBlockGroupWithName(item);
			if (reactorGroup != null) {
				var tempReactors = new List<IMyReactor>();
				reactorGroup.GetBlocksOfType<IMyReactor>(tempReactors);
				reactors.AddRange(tempReactors);
				// Else try adding a single reactor
			} else {
				IMyReactor reactor = GridTerminalSystem.GetBlockWithName(item) as IMyReactor;
				if (reactors != null) {
					reactors.Add(reactor);
				} else {
					CreateWarning("Reactor not found:\n'" + reactor + "'\nUsing all reactors on the grid!");
				}
			}
		}

		// If the list is still empty, add all reactors on the grid
		if (reactors.Count == 0) {
			GridTerminalSystem.GetBlocksOfType<IMyReactor>(reactors, r => r.CubeGrid == Me.CubeGrid);
		}
	}

	// Lights
	if (baseLightManagement) {
		lights.Clear();
		spotlights.Clear();

		// If set, fill the list only with the group's lights
		if (baseLightGroups.Length > 0) {
			var tempLights = new List<IMyInteriorLight>();
			var tempSpotlights = new List<IMyReflectorLight>();
			foreach (var group in baseLightGroups) {
				var lightGroup = GridTerminalSystem.GetBlockGroupWithName(group);
				if (lightGroup != null) {
					lightGroup.GetBlocksOfType<IMyInteriorLight>(tempLights);
					lights.AddRange(tempLights);
					lightGroup.GetBlocksOfType<IMyReflectorLight>(tempSpotlights);
					spotlights.AddRange(tempSpotlights);
				} else {
					CreateWarning("Light group not found:\n'" + group + "'");
				}
			}

			// Else search for all interior lights and spotlights and fill the groups
		} else {
			GridTerminalSystem.GetBlocksOfType<IMyInteriorLight>(lights, l => l.CubeGrid == Me.CubeGrid);
			GridTerminalSystem.GetBlocksOfType<IMyReflectorLight>(spotlights, l => l.CubeGrid == Me.CubeGrid);
		}
	}

	// Corner LCDs
	cornerLcds.Clear();

	// Cycle through all the items in cornerLcds to find groups or corner LCDs    
	foreach (var item in cornerLcdNames) {
		// If the item is a group, get the LCDs and join the list with lcds list
		var lcdGroup = GridTerminalSystem.GetBlockGroupWithName(item);
		if (lcdGroup != null) {
			var tempLcds = new List<IMyTextPanel>();
			lcdGroup.GetBlocksOfType<IMyTextPanel>(tempLcds);
			cornerLcds.AddRange(tempLcds);
			// Else try adding a single corner LCD
		} else {
			IMyTextPanel cornerLcd = GridTerminalSystem.GetBlockWithName(item) as IMyTextPanel;
			if (cornerLcd == null) {
				CreateWarning("Corner-LCD not found:\n'" + item + "'");
			} else {
				cornerLcds.Add(cornerLcd);
			}
		}
	}
}


/// <summary>
/// Scan a grid on top of a rotor for more rotors and scan their togrid, too, until solar panels are found
/// </summary>
/// <param name="rotor">Rotor to scan</param>
void ScanRecursive(IMyMotorStator rotor)
{
	// Add the current grid to scannedGrids list
	scannedGrids.Add(rotor.CubeGrid);

	// Get all solar panels on the grid
	var rotorSolarPanels = new List<IMySolarPanel>();
	GridTerminalSystem.GetBlocksOfType<IMySolarPanel>(rotorSolarPanels, solarPanel => solarPanel.CubeGrid == rotor.TopGrid);

	// Get all oxygen farms on the grid
	var rotorOxygenFarms = new List<IMyOxygenFarm>();
	GridTerminalSystem.GetBlocksOfType<IMyOxygenFarm>(rotorOxygenFarms, oxygenFarm => oxygenFarm.CubeGrid == rotor.TopGrid);

	// If there are solar panels or oxygen farms, sum their output
	if (rotorSolarPanels.Count > 0 || rotorOxygenFarms.Count > 0) {
		hasSolarsOrOxygenFarms = true;

		// Solar panels
		foreach (var solarPanel in rotorSolarPanels) {
			solarPanels.Add(solarPanel);
			rotorOutput += solarPanel.MaxOutput;
		}

		// Oxygen farms
		foreach (var oxygenFarm in rotorOxygenFarms) {
			oxygenFarms.Add(oxygenFarm);
			rotorOutput += oxygenFarm.GetOutput();
		}
	}

	// Also check for rotors or pistons to see, if there are solar panels or oxygen farms on other subgrids

	// Get all rotors on the grid
	var gridRotors = new List<IMyMotorStator>();
	GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(gridRotors, gridRotor => gridRotor.CubeGrid == rotor.TopGrid);

	// Get all pistons on the grid
	var gridPistons = new List<IMyPistonBase>();
	GridTerminalSystem.GetBlocksOfType<IMyPistonBase>(gridPistons, gridPiston => gridPiston.CubeGrid == rotor.TopGrid);

	// Search for solar panels or oxygen farm again
	foreach (var gridRotor in gridRotors) {
		if (!scannedGrids.Exists(grid => grid == gridRotor.TopGrid)) {
			ScanRecursive(gridRotor);
		}
	}

	foreach (var gridPiston in gridPistons) {
		if (!scannedGrids.Exists(grid => grid == gridPiston.TopGrid)) {
			ScanRecursivePiston(gridPiston);
		}
	}
}


/// <summary>
/// Scan a grid on top of a piston for more pistons and scan their togrid, too, until solar panels are found
/// </summary>
/// <param name="piston">Piston to scan</param>
void ScanRecursivePiston(IMyPistonBase piston)
{
	// Add the current grid to scannedGrids list
	scannedGrids.Add(piston.CubeGrid);

	// Get all solar panels the rotor has
	var pistonSolarPanels = new List<IMySolarPanel>();
	GridTerminalSystem.GetBlocksOfType<IMySolarPanel>(pistonSolarPanels, solarPanel => solarPanel.CubeGrid == piston.TopGrid);

	// Get all oxygen farms the rotor has
	var pistonOxygenFarms = new List<IMyOxygenFarm>();
	GridTerminalSystem.GetBlocksOfType<IMyOxygenFarm>(pistonOxygenFarms, oxygenFarm => oxygenFarm.CubeGrid == piston.TopGrid);

	// If there are solar panels or oxygen farms, sum their output
	if (pistonSolarPanels.Count > 0 || pistonOxygenFarms.Count > 0) {
		hasSolarsOrOxygenFarms = true;

		// Solar panels
		foreach (var solarPanel in pistonSolarPanels) {
			solarPanels.Add(solarPanel);
			rotorOutput += solarPanel.MaxOutput;
		}

		// Oxygen farms
		foreach (var oxygenFarm in pistonOxygenFarms) {
			oxygenFarms.Add(oxygenFarm);
			rotorOutput += oxygenFarm.GetOutput();
		}
	}

	// Also check for rotors or pistons to see, if there are solar panels or oxygen farms on other subgrids

	// Get all rotors on the grid
	var gridRotors = new List<IMyMotorStator>();
	GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(gridRotors, gridRotor => gridRotor.CubeGrid == piston.TopGrid);

	// Get all pistons on the grid
	var gridPistons = new List<IMyPistonBase>();
	GridTerminalSystem.GetBlocksOfType<IMyPistonBase>(gridPistons, gridPiston => gridPiston.CubeGrid == piston.TopGrid);

	// Search for solar panels or oxygen farm again
	foreach (var gridRotor in gridRotors) {
		if (!scannedGrids.Exists(grid => grid == gridRotor.TopGrid)) {
			ScanRecursive(gridRotor);
		}
	}

	foreach (var gridPiston in gridPistons) {
		if (!scannedGrids.Exists(grid => grid == gridPiston.TopGrid)) {
			ScanRecursivePiston(gridPiston);
		}
	}
}


/// <summary>
/// Executes the command line action
/// </summary>
/// <param name="arg">Argument as String</param>
/// <returns>True if a valid argument was given, else false</returns>
bool ExecuteArgument(string arg)
{
	bool validArgument = true;

	// Pause the alignment when set via argument
	if (arg == "pause") {
		StopAll();

		if (pause) {
			action = "align";
			pause = false;
			return false;
		} else {
			action = "paused";
			pause = true;
		}

		currentOperation = "Automatic alignment paused.\n";
		currentOperation += "Run 'pause' again to continue..";

	// After stopping all rotors, only show the pause message in further runs (so that users can rotate manually)
	} else if (arg == "paused") {
		currentOperation = "Automatic alignment paused.\n";
		currentOperation += "Run 'pause' again to continue..";

	// Force a realign to the current best output        
	} else if (arg == "realign" && !useGyroMode) {
		Realign();

		currentOperation = "Forced realign by user.\n";
		currentOperation += "Searching highest output for " + realignTimer + " more seconds.";

		if (realignTimer == 0) {
			action = "";
			realignTimer = 90;
		} else {
			realignTimer -= 1;
		}

	// Reset the time calculation when set via argument
	} else if (arg == "reset" && !useGyroMode) {
		dayTimer = 0;
		safetyTimer = 270;
		sunSet = sunSetDefault;
		dayLength = dayLengthDefault;

		currentOperation = "Calculated time resetted.\n";
		currentOperation += "Continuing in " + actionTimer + " seconds.";

		if (actionTimer == 0) {
			action = "";
			actionTimer = 3;
		} else {
			actionTimer -= 1;
		}

	// Rotate to a specific angle when set via argument
	} else if (arg.Contains("rotate") && !useGyroMode) {
		String[] parameters = arg.Split(' ');
		bool couldParse = false;
		rotateMode = "both";
		pauseAfterRotate = false;
		if (parameters[0].Contains("pause")) pauseAfterRotate = true;

		// If 2 parameters were specified, check if it's vertical or horizontal mode
		if (parameters.Length == 2) {
			// Should only the horizontals be rotated?
			if (parameters[1].Contains("h")) {
				couldParse = Double.TryParse(parameters[1].Replace("h", ""), out rotateHorizontalAngle);
				rotateMode = "horizontalOnly";

				// Should only the verticals be rotated?
			} else if (parameters[1].Contains("v")) {
				couldParse = Double.TryParse(parameters[1].Replace("v", ""), out rotateVerticalAngle);
				rotateMode = "verticalOnly";
			}

			if (couldParse) {
				currentOperation = "Checking rotation parameters...";
				action = "rotNormal";
			} else {
				StopAll();
				CreateWarning("Wrong format!\n\nShould be (e.g. 90 degrees):\nrotate h90 OR\nrotate v90");
			}

				// If 3 parameters were specified, check whether horizontal or vertical should be moved first
		} else if (parameters.Length == 3) {
			string plannedAction = "rotNormal";

			// Should the verticals be rotated first?
			if (parameters[1].Contains("v")) {
				couldParse = Double.TryParse(parameters[1].Replace("v", ""), out rotateVerticalAngle);
				if (couldParse) couldParse = Double.TryParse(parameters[2].Replace("h", ""), out rotateHorizontalAngle);
				plannedAction = "rotVH1";

				// Else try parsing normally
			} else {
				couldParse = Double.TryParse(parameters[1].Replace("h", ""), out rotateHorizontalAngle);
				if (couldParse) couldParse = Double.TryParse(parameters[2].Replace("v", ""), out rotateVerticalAngle);
			}

			if (couldParse) {
				currentOperation = "Checking rotation parameters...";
				action = plannedAction;
			} else {
				StopAll();
				CreateWarning("Wrong format!\n\nShould be (e.g. 90 degrees):\nrotate h90 v90 OR\nrotate v90 h90");
			}
		} else {
			StopAll();
			CreateWarning("Not enough parameters!\n\nShould be 2 or 3:\nrotate h90 OR\nrotate h90 v90");
		}

	// Normal rotation
	} else if (arg == "rotNormal") {
		currentOperation = "Rotating to user defined values...";
		bool rotationDone = RotateAll(rotateMode, rotateHorizontalAngle, rotateVerticalAngle);
		if (rotationDone && pauseAfterRotate) {
			action = "paused";
		} else if (rotationDone && !pauseAfterRotate) {
			action = "resume";
		}

		// Vertical first rotation stage 1
	} else if (arg == "rotVH1") {
		currentOperation = "Rotating to user defined values...";
		bool rotationDone = RotateAll("verticalOnly", rotateHorizontalAngle, rotateVerticalAngle);
		if (rotationDone) action = "rotVH2";

		// Vertical first rotation stage 2
	} else if (arg == "rotVH2") {
		currentOperation = "Rotating to user defined values...";
		bool rotationDone = RotateAll("horizontalOnly", rotateHorizontalAngle, rotateVerticalAngle);
		if (rotationDone && pauseAfterRotate) {
			action = "paused";
		} else if (rotationDone && !pauseAfterRotate) {
			action = "resume";
		}
	} else {
		validArgument = false;
	}

	return validArgument;
}


/// <summary>
/// Read a block's custom data
/// </summary>
/// <param name="block">Block to read</param>
/// <param name="field">Field to read</param>
/// <returns>Fieldvalue as double</returns>
double ReadCustomData(IMyTerminalBlock block, string field)
{
	CheckCustomData(block);
	var customData = block.CustomData.Split('\n');

	foreach (var line in customData) {
		if (line.Contains(field + "=")) {
			return Convert.ToDouble(line.Replace(field + "=", ""));
		}
	}

	return 0;
}


/// <summary>
/// Write a block's custom data
/// </summary>
/// <param name="block">Block to write</param>
/// <param name="field">Field to write</param>
/// <param name="value">Value to write</param>
void WriteCustomData(IMyTerminalBlock block, string field, double value)
{
	CheckCustomData(block);
	var customData = block.CustomData.Split('\n');

	string newCustomData = "";
	foreach (var line in customData) {
		if (line.Contains(field + "=")) {
			newCustomData += field + "=" + value + "\n";
		} else {
			newCustomData += line + "\n";
		}
	}
	newCustomData = newCustomData.TrimEnd('\n');
	block.CustomData = newCustomData;
}


/// <summary>
/// Checks a block's custom data and restores the default custom data, if it is too short
/// </summary>
/// <param name="block">Block to check</param>
void CheckCustomData(IMyTerminalBlock block)
{
	var customData = block.CustomData.Split('\n');
	string[] defaultData = defaultCustomDataRotor;

	// Create new default customData if a too short one is found
	if (customData.Length != defaultData.Length) {
		string newCustomData = "";

		foreach (var item in defaultData) {
			newCustomData += item + "\n";
		}

		block.CustomData = newCustomData.TrimEnd('\n');
	}
}


/// <summary>
/// Read a LCD's custom data
/// </summary>
/// <param name="lcd">LCD to read</param>
/// <param name="field">Field to read</param>
/// <returns>Fieldvalue as bool</returns>
bool ReadCustomDataLCD(IMyTextPanel lcd, string field)
{
	CheckCustomDataLCD(lcd);
	var customData = lcd.CustomData.Replace(" ", "").Split('\n');

	foreach (var line in customData) {
		if (line.Contains(field + "=")) {
			try {
				return Convert.ToBoolean(line.Replace(field + "=", ""));
			} catch {
				return true;
			}
		}
	}

	return true;
}


/// <summary>
/// Checks a LCD's custom data and restores the default custom data, if it is too short
/// </summary>
/// <param name="lcd">LCD to check</param>
void CheckCustomDataLCD(IMyTextPanel lcd)
{
	var customData = lcd.CustomData.Split('\n');
	string[] defaultData = defaultCustomDataLCD;

	// Create new default customData if a too short one is found and set the default font size
	if (customData.Length != defaultData.Length) {
		string newCustomData = "";

		foreach (var item in defaultData) {
			newCustomData += item + "\n";
		}

		lcd.CustomData = newCustomData.TrimEnd('\n');
		lcd.FontSize = 0.5f;
	}
}


/// <summary>
/// Get the output of solar panels, batteries, oxygen farms and tanks and prepare the output strings.
/// </summary>
void GetOutput()
{
	// Solar panels
	maxOutputAP = 0;
	currentOutputAP = 0;

	foreach (var solarPanel in solarPanels) {
		maxOutputAP += solarPanel.MaxOutput;
		currentOutputAP += solarPanel.CurrentOutput;

		// Terminal solar stats
		if (showSolarStats && enableTerminalStatistics) {
			double maxPanelOutput = 0;
			double.TryParse(solarPanel.CustomData, out maxPanelOutput);

			if (maxPanelOutput < solarPanel.MaxOutput) {
				maxPanelOutput = solarPanel.MaxOutput;
				solarPanel.CustomData = maxPanelOutput.ToString();
			}

			AddStatusToName(solarPanel, true, "", solarPanel.MaxOutput, maxPanelOutput);
		}
	}

	// Oxygen farms
	foreach (var oxygenFarm in oxygenFarms) {
		maxOutputAP += oxygenFarm.GetOutput() / 1000000;
	}

	// Set the max. detected output if a higher output was measured
	if (maxOutputAP > maxDetectedOutputAP) {
		maxDetectedOutputAP = maxOutputAP;
	}

	// Format the output strings
	maxOutputAPStr = GetPowerString(maxOutputAP);
	currentOutputAPStr = GetPowerString(currentOutputAP);
	maxDetectedOutputAPStr = GetPowerString(maxDetectedOutputAP);

	// Find batteries
	batteriesCurrentInput = 0;
	batteriesMaxInput = 0;
	batteriesCurrentOutput = 0;
	batteriesMaxOutput = 0;
	batteriesPower = 0;
	batteriesMaxPower = 0;

	// Add their current values
	foreach (var battery in batteries) {
		batteriesCurrentInput += battery.CurrentInput;
		batteriesMaxInput += battery.MaxInput;
		batteriesCurrentOutput += battery.CurrentOutput;
		batteriesMaxOutput += battery.MaxOutput;
		batteriesPower += battery.CurrentStoredPower;
		batteriesMaxPower += battery.MaxStoredPower;

		if (showBatteryStats && enableTerminalStatistics) {
			string status = "";

			if (battery.CurrentStoredPower < battery.MaxStoredPower * 0.99) {
				status = "Draining";
				if (battery.CurrentInput > battery.CurrentOutput) status = "Recharging";
			}

			AddStatusToName(battery, true, status, battery.CurrentStoredPower, battery.MaxStoredPower);
		}
	}

	// Round the values to be nicely readable
	batteriesCurrentInputStr = GetPowerString(batteriesCurrentInput);
	batteriesMaxInputStr = GetPowerString(batteriesMaxInput);
	batteriesCurrentOutputStr = GetPowerString(batteriesCurrentOutput);
	batteriesMaxOutputStr = GetPowerString(batteriesMaxOutput);
	batteriesPowerStr = GetPowerString(batteriesPower, true);
	batteriesMaxPowerStr = GetPowerString(batteriesMaxPower, true);

	// Find oxygen farms and tanks
	oxygenFarmEfficiency = 0;
	oxygenTankCapacity = 0;
	oxygenTankFillLevel = 0;

	foreach (var oxygenFarm in oxygenFarms) {
		oxygenFarmEfficiency += oxygenFarm.GetOutput();

		if (showOxygenFarmStats && enableTerminalStatistics) {
			AddStatusToName(oxygenFarm, true, "", oxygenFarm.GetOutput(), 1);
		}
	}

	oxygenFarmEfficiency = Math.Round(oxygenFarmEfficiency / oxygenFarms.Count * 100, 2);

	foreach (var oxygenTank in oxygenTanks) {
		oxygenTankCapacity += oxygenTank.Capacity;
		oxygenTankFillLevel += oxygenTank.Capacity * oxygenTank.FilledRatio;

		if (showOxygenTankStats && enableTerminalStatistics) {
			AddStatusToName(oxygenTank, true, "", oxygenTank.FilledRatio, 1);
		}
	}

	oxygenTankCapacityStr = GetVolumeString(oxygenTankCapacity);
	oxygenTankFillLevelStr = GetVolumeString(oxygenTankFillLevel);
}


/// <summary>
/// Main rotation logic for gyro mode
/// </summary>
void RotationLogicGyro()
{
	if (gyros.Count == 0) return;

	if (gyroReference[0].IsUnderControl) {
		StopAll();
		currentOperation = "Automatic alignment paused.\n";
		currentOperation += "Ship is currently controlled by a player.";
		return;
	}

	// Rotation timeout
	int rotationTimeout = 10;

	// Variables for the current operation
	bool rolling = false;
	bool pitching = false;
	string direction = "";

	// Get the global output as shorter, local variables
	double output = maxOutputAP;
	double outputMax = maxDetectedOutputAP;
	double outputLast = maxOutputAPLast;

	double speed = maxGyroRPM - (maxGyroRPM - minGyroRPM) * (output / outputMax);
	speed = speed / (Math.PI * 3);

	// Pitch
	// Only move the ship, if the output is 1% below or above the last locked output and it's allowed to rotate
	if ((output <= outputLockedPitch * searchPercentageGyro || output >= outputLockedPitch * 1.01 || output < outputMax * 0.1) && allowPitch && timeSincePitch >= rotationTimeout) {
		// Disallow rolling
		allowRoll = false;
		outputLockedPitch = 0;

		// Check if the output goes down to reverse the rotation
		if (output < outputLast && directionTimerPitch == 3 && !directionChangedPitch) {
			directionPitch = -directionPitch;
			directionTimerPitch = 0;
			directionChangedPitch = true;
		}

		RotateGyros((float)(directionPitch * speed), 0, 0);

		// Information for current operation
		if (directionPitch == -1) {
			direction = "down";
		} else {
			direction = "up";
		}

		// If the output reached maximum, stop the ship
		if (output < outputLast && directionTimerPitch >= 4) {
			// Stop the gyros and allow rolling
			StopGyros();
			allowRoll = true;

			outputLockedPitch = output;
			directionChangedPitch = false;
			directionTimerPitch = 0;
			timeSincePitch = 0;
		} else {
			pitching = true;
			directionTimerPitch++;
		}

	} else if (allowPitch) {
		// Stop the gyros and allow pitching
		StopGyros();
		allowRoll = true;

		// Update directionChanged and directionTimer
		directionChangedPitch = false;
		directionTimerPitch = 0;
		timeSincePitch++;
	} else {
		timeSincePitch++;
	}

	// Roll
	// Only move the ship, if the output is 1% below or above the last locked output and it's allowed to rotate
	if ((output <= outputLockedRoll * searchPercentageGyro || output >= outputLockedRoll * 1.01) && allowRoll && timeSinceRoll >= rotationTimeout) {
		// Disallow pitching
		allowPitch = false;
		outputLockedRoll = 0;

		// Check if the output goes down to reverse the rotation
		if (output < outputLast && directionTimerRoll == 3 && !directionChangedRoll) {
			directionRoll = -directionRoll;
			directionTimerRoll = 0;
			directionChangedRoll = true;
		}

		RotateGyros(0, 0, (float)(directionRoll * speed));

		// Information for current operation
		if (directionRoll == -1) {
			direction = "left";
		} else {
			direction = "right";
		}

		// If the output reached maximum, stop the ship
		if (output < outputLast && directionTimerRoll >= 4) {
			// Stop the gyros and allow pitching
			StopGyros();
			allowPitch = true;

			outputLockedRoll = output;
			directionChangedRoll = false;
			directionTimerRoll = 0;
			timeSinceRoll = 0;
		} else {
			rolling = true;
			directionTimerRoll++;
		}

	} else if (allowRoll) {
		// Stop the gyros and allow pitching
		StopGyros();
		allowPitch = true;

		// Update directionChanged and directionTimer
		directionChangedRoll = false;
		directionTimerRoll = 0;
		timeSinceRoll++;
	} else { 
		timeSinceRoll++;
	}

	// Create information about the movement
	if (!rolling && !pitching) {
		currentOperation = "Aligned.";
	} else if (rolling) {
		currentOperation = "Aligning by rolling the ship " + direction + "..";
	} else if (pitching) {
		currentOperation = "Aligning by pitching the ship " + direction + "..";
	}
}


/// <summary>
/// Main rotation logic for rotor mode
/// </summary>
void RotationLogic()
{
	// If output is less than nightPercentage of max detected output, it's night time
	if (maxOutputAP < maxDetectedOutputAP * nightPercentage && nightModeTimer >= 30) {
		currentOperation = "Night Mode.";
		nightModeActive = true;

		// Rotate the panels to the base angle or stop them
		if (rotateToSunrise && !sunrisePosReached) {
			foreach (var rotor in rotors) {
				WriteCustomData(rotor, "firstLockOfDay", 1);
			}
			if (manualAngle) {
				sunrisePosReached = RotateAll("both", manualAngleHorizontal, manualAngleVertical);
			} else {
				sunrisePosReached = RotateAll("sunrise", manualAngleHorizontal, manualAngleVertical);
			}
		} else {
			StopAll();
		}

		// If output is measured, start rotating
	} else {
		// Check the night mode setting and reset the timer, if it was night mode before
		if (nightModeActive) {
			nightModeActive = false;
			nightModeTimer = 0;
		} else if (nightModeTimer > 172800) {
			// Fail safe if no night was measured in 48 hours
			nightModeTimer = 0;
		} else {
			nightModeTimer++;
		}
		sunrisePosReached = false;
		rotateAllInit = true;

		// Rotation timeout
		int rotationTimeout = 10;

		if (maxOutputAP < maxDetectedOutputAP * 0.5) {
			rotationTimeout = 30;
		}

		// Counter variables for the currently moving rotors
		int vRotorMoving = 0;
		int hRotorMoving = 0;

		// Vertical rotors
		foreach (var vRotor in vRotors) {
			double output = ReadCustomData(vRotor, "output");
			double outputLast = ReadCustomData(vRotor, "outputLast");
			double outputLocked = ReadCustomData(vRotor, "outputLocked");
			double outputMax = ReadCustomData(vRotor, "outputMax");
			double direction = ReadCustomData(vRotor, "direction");
			double directionChanged = ReadCustomData(vRotor, "directionChanged");
			double directionTimer = ReadCustomData(vRotor, "directionTimer");
			double allowRotation = ReadCustomData(vRotor, "allowRotation");
			double timeSinceRotation = ReadCustomData(vRotor, "timeSinceRotation");
			bool forceStop = false;

			// Only move the rotor, if the output is 1% below or above the last locked output and it's allowed to rotate
			if ((output <= outputLocked * searchPercentage || output >= outputLocked * 1.01) && allowRotation == 1 && timeSinceRotation >= rotationTimeout) {
				// Disallow rotation for the hRotors on the of the vRotor
				SetAllowRotationH(vRotor, false);
				outputLocked = 0;

				// Check if the output goes down to reverse the rotation
				if (output < outputLast && directionTimer == 3 && directionChanged == 0) {
					direction = -direction;
					directionTimer = 0;
					directionChanged = 1;
				}

				// If the rotor has limits and reached it's maximum or minimum angle, reverse the rotation
				if ((vRotor.LowerLimitDeg != float.MinValue || vRotor.UpperLimitDeg != float.MaxValue) && directionTimer >= 5) {
					double vRotorAngle = GetAngle(vRotor);
					float vRotorLL = (float)Math.Round(vRotor.LowerLimitDeg);
					float vRotorUL = (float)Math.Round(vRotor.UpperLimitDeg);

					if (vRotorAngle == vRotorLL || vRotorAngle == 360 + vRotorLL || vRotorAngle == vRotorUL || vRotorAngle == 360 + vRotorUL) {
						if (output < outputLast && directionChanged == 0) {
							direction = -direction;
							directionTimer = 0;
							directionChanged = 1;
						} else {
							forceStop = true;
						}
					}
				}

				// Rotate the rotor with a speed between 0.1 and 1.1 based on current output / max output                
				Rotate(vRotor, direction, (float)(1.1 - output / outputMax));

				// If the output reached maximum or is zero, stop the rotor
				if (output < outputLast && directionTimer >= 4 || output == 0 || forceStop) {
					// Stop the rotor and allow the hRotor to rotate
					Stop(vRotor);
					SetAllowRotationH(vRotor, true);

					// If this is the first lock of the day and rotateToSunrise is true, store the angle
					if (rotateToSunrise && ReadCustomData(vRotor, "firstLockOfDay") == 1) {
						WriteCustomData(vRotor, "firstLockOfDay", 0);
						WriteCustomData(vRotor, "sunriseAngle", GetAngle(vRotor));
					}

					outputLocked = output;
					directionChanged = 0;
					directionTimer = 0;
					timeSinceRotation = 0;
				} else {
					vRotorMoving++;
					directionTimer++;
				}

				// Update outputLocked, direction, directionChanged, directionTimer and timeSinceRotation on the rotor
				WriteCustomData(vRotor, "outputLocked", outputLocked);
				WriteCustomData(vRotor, "direction", direction);
				WriteCustomData(vRotor, "directionChanged", directionChanged);
				WriteCustomData(vRotor, "directionTimer", directionTimer);
				WriteCustomData(vRotor, "timeSinceRotation", timeSinceRotation);
			} else {
				// Stop the rotor and allow the hRotor and itself to rotate
				Stop(vRotor);
				SetAllowRotationH(vRotor, true);
				WriteCustomData(vRotor, "allowRotation", 1);

				// Update directionChanged, directionTimer and timeSinceRotation on the rotor
				directionChanged = 0;
				directionTimer = 0;
				timeSinceRotation++;
				WriteCustomData(vRotor, "directionChanged", directionChanged);
				WriteCustomData(vRotor, "directionTimer", directionTimer);
				WriteCustomData(vRotor, "timeSinceRotation", timeSinceRotation);
			}
		}

		// Horizontal rotors
		foreach (var hRotor in hRotors) {
			double output = ReadCustomData(hRotor, "output");
			double outputLast = ReadCustomData(hRotor, "outputLast");
			double outputLocked = ReadCustomData(hRotor, "outputLocked");
			double outputMax = ReadCustomData(hRotor, "outputMax");
			double direction = ReadCustomData(hRotor, "direction");
			double directionChanged = ReadCustomData(hRotor, "directionChanged");
			double directionTimer = ReadCustomData(hRotor, "directionTimer");
			double allowRotation = ReadCustomData(hRotor, "allowRotation");
			double timeSinceRotation = ReadCustomData(hRotor, "timeSinceRotation");
			bool forceStop = false;

			// Only move the rotor, if the output is 1% below or above the last locked output and it's allowed to rotate
			if ((output <= outputLocked * searchPercentage || output >= outputLocked * 1.01 || output < outputMax * 0.1) && allowRotation == 1 && timeSinceRotation >= rotationTimeout) {
				// Disallow rotation for the vRotor below the hRotor
				SetAllowRotationV(hRotor, false);
				outputLocked = 0;

				// Check if the output goes down to reverse the rotation
				if (output < outputLast && directionTimer == 3 && directionChanged == 0) {
					direction = -direction;
					directionTimer = 0;
					directionChanged = 1;
				}

				// If the rotor has limits and reached it's maximum or minimum angle, reverse the rotation
				if ((hRotor.LowerLimitDeg != float.MinValue || hRotor.UpperLimitDeg != float.MaxValue) && directionTimer >= 5) {
					double hRotorAngle = GetAngle(hRotor);
					float hRotorLL = (float)Math.Round(hRotor.LowerLimitDeg);
					float hRotorUL = (float)Math.Round(hRotor.UpperLimitDeg);

					if (hRotorAngle == hRotorLL || hRotorAngle == 360 + hRotorLL || hRotorAngle == hRotorUL || hRotorAngle == 360 + hRotorUL) {
						if (output < outputLast && directionChanged == 0) {
							direction = -direction;
							directionTimer = 0;
							directionChanged = 1;
						} else {
							forceStop = true;
						}
					}
				}

				// Rotate the rotor with a speed between 0.1 and 1.1 based on current output / max output
				Rotate(hRotor, direction, (float)(1.1 - output / outputMax));

				// If the output reached maximum or is zero, force lock
				if (output < outputLast && directionTimer >= 4 || output == 0 || forceStop) {
					// Stop the rotor
					Stop(hRotor);

					// If this is the first lock of the day and rotateToSunrise is true, store the angle
					if (rotateToSunrise && ReadCustomData(hRotor, "firstLockOfDay") == 1) {
						WriteCustomData(hRotor, "firstLockOfDay", 0);
						WriteCustomData(hRotor, "sunriseAngle", GetAngle(hRotor));
					}

					outputLocked = output;
					directionChanged = 0;
					directionTimer = 0;
					timeSinceRotation = 0;
				} else {
					hRotorMoving++;
					directionTimer++;
				}

				// Update outputLocked, direction, directionChanged, directionTimer and timeSinceRotation on the rotor
				WriteCustomData(hRotor, "outputLocked", outputLocked);
				WriteCustomData(hRotor, "direction", direction);
				WriteCustomData(hRotor, "directionChanged", directionChanged);
				WriteCustomData(hRotor, "directionTimer", directionTimer);
				WriteCustomData(hRotor, "timeSinceRotation", timeSinceRotation);
			} else {
				// Stop the rotor
				Stop(hRotor);

				// Update directionChanged, directionTimer and timeSinceRotation on the rotor
				directionChanged = 0;
				directionTimer = 0;
				timeSinceRotation++;
				WriteCustomData(hRotor, "directionChanged", directionChanged);
				WriteCustomData(hRotor, "directionTimer", directionTimer);
				WriteCustomData(hRotor, "timeSinceRotation", timeSinceRotation);
			}
		}

		// Create information about the moving rotors
		if (vRotorMoving == 0 && hRotorMoving == 0) {
			currentOperation = "Aligned.";
		} else if (vRotorMoving == 0) {
			currentOperation = "Aligning " + hRotorMoving + " horizontal rotors..";
		} else if (hRotorMoving == 0) {
			currentOperation = "Aligning " + vRotorMoving + " vertical rotors..";
		} else {
			currentOperation = "Aligning " + hRotorMoving + " horizontal and " + vRotorMoving + " vertical rotors..";
		}
	}
}


/// <summary>
/// Allow or disallow vRotors to rotate
/// </summary>
/// <param name="rotor">hRotor on the topgrid of a vRotor</param>
/// <param name="value">True or False</param>
void SetAllowRotationV(IMyMotorStator rotor, bool value)
{
	foreach (var vRotor in vRotors) {
		if (rotor.CubeGrid == vRotor.TopGrid) {
			if (value) {
				WriteCustomData(vRotor, "allowRotation", 1);
			} else {
				WriteCustomData(vRotor, "allowRotation", 0);
			}
		}
	}
}


/// <summary>
/// Allow or disallow hRotors to rotate
/// </summary>
/// <param name="rotor">vRotor whoose topgrid is the hRotor's cubegrid</param>
/// <param name="value">True or False</param>
void SetAllowRotationH(IMyMotorStator rotor, bool value)
{
	foreach (var hRotor in hRotors) {
		if (rotor.TopGrid == hRotor.CubeGrid) {
			if (value) {
				WriteCustomData(hRotor, "allowRotation", 1);
			} else {
				WriteCustomData(hRotor, "allowRotation", 0);
			}
		}
	}
}


/// <summary>
/// Rotate a rotor in a specific direction with a certain speed
/// </summary>
/// <param name="rotor">Rotor to rotate</param>
/// <param name="direction">1 or -1</param>
/// <param name="speed">Any floatingpoint number</param>
void Rotate(IMyMotorStator rotor, double direction, float speed = rotorSpeed)
{
	rotor.Enabled = true;
	rotor.RotorLock = false;
	rotor.TargetVelocityRPM = speed * (float)direction;
}


/// <summary>
/// Override gyro rotation based on the way, the cockpit is oriented. Credits to SirHamsterAlot without whom I wouldn't have figured this out!
/// </summary>
/// <param name="pitch">Relative pitch as double</param>
/// <param name="yaw">Relative yaw as double</param>
/// <param name="roll">Relative roll as double</param>
void RotateGyros(double pitch, double yaw, double roll)
{
	Vector3D localRotation = new Vector3D(-pitch, yaw, roll);
	Vector3D relativeRotation = Vector3D.TransformNormal(localRotation, gyroReference[0].WorldMatrix);

	foreach (var gyro in gyros) {
		Vector3D gyroRotation = Vector3D.TransformNormal(relativeRotation, Matrix.Transpose(gyro.WorldMatrix));

		gyro.GyroOverride = true;
		gyro.GyroPower = gyroPower;

		gyro.Pitch = (float)gyroRotation.X;
		gyro.Yaw = (float)gyroRotation.Y;
		gyro.Roll = (float)gyroRotation.Z;
	}
}


/// <summary>
/// Stop a rotor
/// </summary>
/// <param name="rotor">Rotor to stop</param>
/// <param name="stayUnlocked">Stay enabled after stopping?</param>
void Stop(IMyMotorStator rotor, bool stayUnlocked = false)
{
	rotor.TargetVelocityRPM = 0f;

	if (stayUnlocked) {
		rotor.RotorLock = false;
	} else {
		rotor.RotorLock = true;
	}
}


/// <summary>
/// Stops all gyros by setting their overrides to 0.
/// </summary>
/// <param name="disableOverride">Disable the override after stopping?</param>
void StopGyros(bool disableOverride = false)
{
	foreach (var gyro in gyros) {
		gyro.Pitch = 0;
		gyro.Yaw = 0;
		gyro.Roll = 0;

		if (disableOverride) gyro.GyroOverride = false;
	}
}


/// <summary>
/// Stop all rotors and gyros
/// </summary>
void StopAll()
{
	foreach (var rotor in rotors) {
		Stop(rotor);
		WriteCustomData(rotor, "timeSinceRotation", 0);
	}

	StopGyros(true);

	timeSinceRoll = 0;
	timeSincePitch = 0;
}


/// <summary>
/// Rotate a rotor to a specific angle
/// </summary>
/// <param name="rotor">Rotor to rotate</param>
/// <param name="targetAngle">Angle in degrees</param>
/// <returns>True when finished, else false</returns>
bool RotateToAngle(IMyMotorStator rotor, double targetAngle, bool relativeAngle = true)
{
	double rotorAngle = GetAngle(rotor);
	bool invert = false;

	if (relativeAngle) {
		// Rotor angle correction
		if (rotor.CustomName.IndexOf("[90]") >= 0) {
			targetAngle += 90;
		} else if (rotor.CustomName.IndexOf("[180]") >= 0) {
			targetAngle += 180;
		} else if (rotor.CustomName.IndexOf("[270]") >= 0) {
			targetAngle += 270;
		}
		if (targetAngle >= 360) targetAngle -= 360;

		// Invert rotorangle if rotor is facing forward, up or right
		if (rotor.Orientation.Up.ToString() == "Down") {
			invert = true;
		} else if (rotor.Orientation.Up.ToString() == "Backward") {
			invert = true;
		} else if (rotor.Orientation.Up.ToString() == "Left") {
			invert = true;
		}
	}

	// If rotor has limits, limit the targetAngle too
	if (rotor.LowerLimitDeg != float.MinValue || rotor.UpperLimitDeg != float.MaxValue) {
		if (invert) targetAngle = -targetAngle;
		if (targetAngle > rotor.UpperLimitDeg) {
			targetAngle = Math.Floor(rotor.UpperLimitDeg);
		}
		if (targetAngle < rotor.LowerLimitDeg) {
			targetAngle = Math.Ceiling(rotor.LowerLimitDeg);
		}
	} else {
		if (invert) targetAngle = 360 - targetAngle;
	}

	// If angle is correct, stop the rotor
	if (rotorAngle == targetAngle || rotorAngle == 360 + targetAngle) {
		Stop(rotor);

		// Reset rotation direction
		if (invert) {
			WriteCustomData(rotor, "direction", -1);
		} else {
			WriteCustomData(rotor, "direction", 1);
		}

		// Reset timeSinceRotation
		WriteCustomData(rotor, "timeSinceRotation", 1);

		return true;

		// Else move the rotor
	} else {
		// Figure out the shortest rotation direction
		int direction = 1;
		if (rotorAngle > targetAngle) {
			direction = -1;
		}
		if (rotorAngle <= 90 && targetAngle >= 270) {
			direction = -1;
		}
		if (rotorAngle >= 270 && targetAngle <= 90) {
			direction = 1;
		}

		// Move rotor
		Single speed = rotorSpeed;
		if (Math.Abs(rotorAngle - targetAngle) > 15) speed = rotorSpeedFast;
		if (Math.Abs(rotorAngle - targetAngle) < 3) speed = 0.05f;
		Rotate(rotor, direction, speed);

		return false;
	}
}


/// <summary>
/// Rotates all rotors to a specific angle
/// </summary>
/// <param name="mode">"sunrise", "both", "horizontalOnly" or "verticalOnly"</param>
/// <param name="horizontalAngle">Horizontal rotor's angle in degrees</param>
/// <param name="verticalAngle">Vertical rotor's angle in degrees</param>
/// <returns>True when finished, else false</returns>
bool RotateAll(string mode, double horizontalAngle = 0, double verticalAngle = 0)
{
	// Return variable
	bool rotationDone = true;

	// Counter variables for the currently moving rotors
	int vRotorMoving = 0;
	int hRotorMoving = 0;

	// Stop all rotors when initiating the rotation
	if (rotateAllInit) {
		rotateAllInit = false;

		foreach (var rotor in rotors) {
			Stop(rotor, true);
		}

		if (mode == "horizontalOnly") {
			foreach (var rotor in vRotors) {
				Stop(rotor);
			}
		} else if (mode == "verticalOnly") {
			foreach (var rotor in hRotors) {
				Stop(rotor);
			}
		}
	}

	// Horizontal rotors
	if (mode != "verticalOnly") {
		foreach (var hRotor in hRotors) {
			// Skip locked rotors
			if (hRotor.RotorLock) continue;

			bool relativeAngle = true;
			double targetAngle = horizontalAngle;
			if (mode == "sunrise") {
				targetAngle = ReadCustomData(hRotor, "sunriseAngle");
				relativeAngle = false;
			}

			// Rotate to the target angle and while rotating, create info string
			if (!RotateToAngle(hRotor, targetAngle, relativeAngle)) {
				rotationDone = false;
				hRotorMoving++;

				// Create information
				currentOperationInfo = hRotorMoving + " horizontal rotors are set to " + horizontalAngle + "°";
				if (mode == "sunrise") currentOperationInfo = hRotorMoving + " horizontal rotors are set to sunrise position";
			}
		}
	}

	if (!rotationDone) return false;

	// Vertical rotors
	if (mode != "horizontalOnly") {
		foreach (var vRotor in vRotors) {
			// Skip locked rotors
			if (vRotor.RotorLock) continue;

			bool relativeAngle = true;
			double targetAngle = verticalAngle;
			if (mode == "sunrise") {
				targetAngle = ReadCustomData(vRotor, "sunriseAngle");
				relativeAngle = false;
			}

			// Rotate to the target angle and while rotating, create info string
			if (!RotateToAngle(vRotor, targetAngle, relativeAngle)) {
				rotationDone = false;
				vRotorMoving++;

				// Create information
				currentOperationInfo = vRotorMoving + " vertical rotors are set to " + verticalAngle + "°";
				if (mode == "sunrise") currentOperationInfo = vRotorMoving + " vertical rotors are set to sunrise position";
			}
		}
	}

	if (rotationDone) rotateAllInit = true;
	return rotationDone;
}


/// <summary>
/// Find a new best output for all rotors
/// </summary>
void Realign()
{
	// Counter variable for the currently moving rotors
	int hRotorMoving = 0;
	int vRotorMoving = 0;

	// Erase the max detected output in the first run
	if (realignTimer == 90) {
		foreach (var rotor in rotors) {
			Stop(rotor, true);

			// Set initial direction
			double initDirection = 1;
			if (rotor.Orientation.Up.ToString() == "Up") {
				initDirection = -1;
			} else if (rotor.Orientation.Up.ToString() == "Forward") {
				initDirection = -1;
			} else if (rotor.Orientation.Up.ToString() == "Right") {
				initDirection = -1;
			}

			WriteCustomData(rotor, "outputMax", ReadCustomData(rotor, "output"));
			WriteCustomData(rotor, "direction", initDirection);
			WriteCustomData(rotor, "directionChanged", 0);
			WriteCustomData(rotor, "directionTimer", 0);
			maxDetectedOutputAP = 0;
		}
	}

	// Rotate the hRotors
	foreach (var hRotor in hRotors) {
		// Skip locked rotors
		if (hRotor.RotorLock) continue;

		// Get rotor stats
		double output = ReadCustomData(hRotor, "output");
		double outputLast = ReadCustomData(hRotor, "outputLast");
		double outputMax = ReadCustomData(hRotor, "outputMax");
		double outputMaxAngle = ReadCustomData(hRotor, "outputMaxAngle");
		double direction = ReadCustomData(hRotor, "direction");
		double directionChanged = ReadCustomData(hRotor, "directionChanged");
		double directionTimer = ReadCustomData(hRotor, "directionTimer");

		// If outputMax == 0, set it to 1 in order to prevent division by zero
		if (outputMax == 0) outputMax = 1;

		// Rotate in both directions to find the highest output
		if (directionChanged != 2) {
			hRotorMoving++;

			// Check if the output goes down to reverse the rotation
			if (output < outputLast && directionTimer >= 7 && directionChanged == 0) {
				WriteCustomData(hRotor, "direction", -direction);
				WriteCustomData(hRotor, "directionChanged", 1);
				directionTimer = 0;
			}

			// If the rotor has limits and reached it's maximum or minimum angle, reverse the rotation
			if ((hRotor.LowerLimitDeg != float.MinValue || hRotor.UpperLimitDeg != float.MaxValue) && directionTimer >= 3 && directionChanged == 0) {
				double hRotorAngle = GetAngle(hRotor);
				float hRotorLL = (float)Math.Round(hRotor.LowerLimitDeg);
				float hRotorUL = (float)Math.Round(hRotor.UpperLimitDeg);

				if (hRotorAngle == hRotorLL || hRotorAngle == 360 + hRotorLL || hRotorAngle == hRotorUL || hRotorAngle == 360 + hRotorUL) {
					WriteCustomData(hRotor, "direction", -direction);
					WriteCustomData(hRotor, "directionChanged", 1);
					directionTimer = 0;
				}
			}

			// Rotate the rotor
			Rotate(hRotor, direction, (float)(2.75 - (output / outputMax) * 2));

			// If the output reached maximum or is zero, force lock
			if (output < outputLast && directionTimer >= 7 && directionChanged == 1) {
				// Stop the rotor
				Stop(hRotor, true);
				WriteCustomData(hRotor, "directionChanged", 2);
			} else {
				WriteCustomData(hRotor, "directionTimer", directionTimer + 1);
			}
		} else {
			// After that, rotate to the new found highest output
			if (!RotateToAngle(hRotor, outputMaxAngle, false)) hRotorMoving++;
		}
	}

	if (hRotorMoving != 0) return;

	// Rotate the vRotors
	foreach (var vRotor in vRotors) {
		// Skip locked rotors
		if (vRotor.RotorLock) continue;

		// Get rotor stats
		double output = ReadCustomData(vRotor, "output");
		double outputLast = ReadCustomData(vRotor, "outputLast");
		double outputMax = ReadCustomData(vRotor, "outputMax");
		double outputMaxAngle = ReadCustomData(vRotor, "outputMaxAngle");
		double direction = ReadCustomData(vRotor, "direction");
		double directionChanged = ReadCustomData(vRotor, "directionChanged");
		double directionTimer = ReadCustomData(vRotor, "directionTimer");

		// If outputMax == 0, set it to 1 in order to prevent division by zero
		if (outputMax == 0) outputMax = 1;

		// Rotate in both directions to find the highest output
		if (directionChanged != 2) {
			vRotorMoving++;

			// Check if the output goes down to reverse the rotation
			if (output < outputLast && directionTimer >= 7 && directionChanged == 0) {
				WriteCustomData(vRotor, "direction", -direction);
				WriteCustomData(vRotor, "directionChanged", 1);
				directionTimer = 0;
			}

			// If the rotor has limits and reached it's maximum or minimum angle, reverse the rotation
			if ((vRotor.LowerLimitDeg != float.MinValue || vRotor.UpperLimitDeg != float.MaxValue) && directionTimer >= 3 && directionChanged == 0) {
				double vRotorAngle = GetAngle(vRotor);
				float vRotorLL = (float)Math.Round(vRotor.LowerLimitDeg);
				float vRotorUL = (float)Math.Round(vRotor.UpperLimitDeg);

				if (vRotorAngle == vRotorLL || vRotorAngle == 360 + vRotorLL || vRotorAngle == vRotorUL || vRotorAngle == 360 + vRotorUL) {
					WriteCustomData(vRotor, "direction", -direction);
					WriteCustomData(vRotor, "directionChanged", 1);
					directionTimer = 0;
				}
			}

			// Rotate the rotor
			Rotate(vRotor, direction, (float)(2.75 - (output / outputMax) * 2));

			// If the output reached maximum or is zero, force lock
			if (output < outputLast && directionTimer >= 7 && directionChanged == 1) {
				// Stop the rotor
				Stop(vRotor, true);
				WriteCustomData(vRotor, "directionChanged", 2);
			} else {
				WriteCustomData(vRotor, "directionTimer", directionTimer + 1);
			}
		} else {
			// After that, rotate to the new found highest output
			if (!RotateToAngle(vRotor, outputMaxAngle, false)) vRotorMoving++;
		}
	}

	// End realigning when all rotors are stopped
	if (hRotorMoving == 0 && vRotorMoving == 0) {
		realignTimer = 0;
	}
}


/// <summary>
/// Adds the current percentage and status at the end of the name
/// </summary>
/// <param name="block">Block to add status to as IMyTerminalBlock</param>
/// <param name="status">Status as string</param>
/// <param name="currentValue">Value as double</param>
/// <param name="maxValue">Value as double</param>
void AddStatusToName(IMyTerminalBlock block, bool addStatus = true, string status = "", double currentValue = 0, double maxValue = 0)
{
	string newName = block.CustomName;
	string oldStatus = System.Text.RegularExpressions.Regex.Match(block.CustomName, @" *\(\d+\.*\d*%.*\)").Value;
	if (oldStatus != String.Empty) {
		newName = block.CustomName.Replace(oldStatus, "");
	}

	if (addStatus) {
		// Add percentages
		newName += " (" + GetPercentString(currentValue, maxValue);

		// Add status
		if (status != "") {
			newName += ", " + status;
		}

		// Add closing bracket
		newName += ")";
	}

	// Rename the block if the name has changed
	if (newName != block.CustomName) {
		block.CustomName = newName;
	}
}


/// <summary>
/// Create a percent string out of two double values
/// </summary>
/// <param name="numerator">Any double value</param>
/// <param name="denominator">Any double value</param>
/// <returns>String like "50%"</returns>
string GetPercentString(double numerator, double denominator)
{
	string percentage = Math.Round(numerator / denominator * 100, 1) + "%";
	if (denominator == 0) {
		return "0%";
	} else {
		return percentage;
	}
}


/// <summary>
/// Create a power string out of a double value
/// </summary>
/// <param name="value">Any double value</param>
/// <param name="wattHours">Optional: true if you want "MWh" instead of "MW"</param>
/// <returns>String like "5 MW"</returns>
string GetPowerString(double value, bool wattHours = false)
{
	string unit = "MW";

	if (value < 1) {
		value *= 1000;
		unit = "kW";
	} else if (value >= 1000 && value < 1000000) {
		value /= 1000;
		unit = "GW";
	} else if (value >= 1000000 && value < 1000000000) {
		value /= 1000000;
		unit = "TW";
	} else if (value >= 1000000000) {
		value /= 1000000000;
		unit = "PW";
	}

	if (wattHours) unit += "h";

	return Math.Round(value, 2) + " " + unit;
}


/// <summary>
/// Returns a string of a tank volume with the correct unit (L, kL, ML, ...)
/// </summary>
/// <param name="value">Tank volume as double</param>
/// <returns>A string like "100 L"</returns>
string GetVolumeString(double value)
{
	string unit = "L";

	if (value >= 1000 && value < 1000000) {
		value /= 1000;
		unit = "KL";
	} else if (value >= 1000000 && value < 1000000000) {
		value /= 1000000;
		unit = "ML";
	} else if (value >= 1000000000) {
		value /= 1000000000;
		unit = "BL";
	} else if (value >= 1000000000000) {
		value /= 1000000000000;
		unit = "TL";
	}

	return Math.Round(value, 2) + " " + unit;
}


/// <summary>
/// Returns the the angle of a rotor in degrees
/// </summary>
/// <param name="rotor">Rotor as IMyMotorStator</param>
/// <returns>Degree as double</returns>
double GetAngle(IMyMotorStator rotor)
{
	return Math.Round(rotor.Angle * 180.0 / Math.PI);
}


/// <summary>
/// Create the information string for terminal and LCD output
/// </summary>
string CreateInformation(bool shortUnderline = false, float fontSize = 0.65f, bool addCurrentOperation = true, bool addSolarStats = true, bool addBatteryStats = true, bool addOxygenStats = true, bool addLocationTime = true)
{
	string info = "";
	bool infoShown = false;

	switch (workingCounter % 4) {
		case 0: workingIndicator = "/"; break;
		case 1: workingIndicator = "-"; break;
		case 2: workingIndicator = "\\"; break;
		case 3: workingIndicator = "|"; break;
	}

	// Terminal / LCD information string
	info = "Isy's Solar Alignment Script " + workingIndicator + "\n";
	info += "=======================";
	if (!shortUnderline) info += "=======";
	info += "\n\n";

	// If any error occurs, show it
	if (error != null) {
		info += "Error!\n";
		info += error + "\n\n";
		info += "Script stopped!\n\n";

		return info;
	}

	// Add warning message for minor errors
	if (warning != null) {
		info += "Warning!\n";
		info += warning + "\n\n";
		infoShown = true;
	}

	// Current Operation
	if (addCurrentOperation) {
		if (currentOperationInfo != null) currentOperation += "\n" + currentOperationInfo + "\n\n";
		info += currentOperation;
		info += StringRepeat('\n', 3 - currentOperation.Count(n => n == '\n'));
		currentOperationInfo = null;
		infoShown = true;
	}

	// Solar Panels
	if (addSolarStats) {
		info += "Statistics for " + solarPanels.Count + " Solar Panels:\n";
		info += CreateBarString(fontSize, "Efficiency", maxOutputAP, maxDetectedOutputAP, maxOutputAPStr, maxDetectedOutputAPStr);
		info += CreateBarString(fontSize, "Output", currentOutputAP, maxDetectedOutputAP, currentOutputAPStr, maxDetectedOutputAPStr) + "\n\n";
		infoShown = true;
	}

	// Batteries
	if (batteries.Count > 0 && addBatteryStats) {
		info += "Statistics for " + batteries.Count + " Batteries:\n";
		info += CreateBarString(fontSize, "Input", batteriesCurrentInput, batteriesMaxInput, batteriesCurrentInputStr, batteriesMaxInputStr);
		info += CreateBarString(fontSize, "Output", batteriesCurrentOutput, batteriesMaxOutput, batteriesCurrentOutputStr, batteriesMaxOutputStr);
		info += CreateBarString(fontSize, "Charge", batteriesPower, batteriesMaxPower, batteriesPowerStr, batteriesMaxPowerStr) + "\n\n";
		infoShown = true;
	}

	// Oxygen Farms / Tanks
	if (addOxygenStats && (oxygenFarms.Count > 0 || oxygenTanks.Count > 0)) {
		info += "Statistics for Oxygen:\n";
		if (oxygenFarms.Count > 0) {
			info += CreateBarString(fontSize, oxygenFarms.Count + " Farms", oxygenFarmEfficiency, 100);
		}

		if (oxygenTanks.Count > 0) {
			info += CreateBarString(fontSize, oxygenTanks.Count + " Tanks", oxygenTankFillLevel, oxygenTankCapacity, oxygenTankFillLevelStr, oxygenTankCapacityStr);
		}
		info += "\n\n";
		infoShown = true;
	}

	// Location time
	if (addLocationTime && !useGyroMode) {
		string inaccurate = "";
		string inaccurateLegend = "";
		string duskDawnTimer = "";

		if (dayLength < dayTimer) {
			inaccurateLegend = " inaccurate";
			inaccurate = "*";
		} else if (dayLength == dayLengthDefault || sunSet == sunSetDefault) {
			inaccurateLegend = " inaccurate, still calculating";
			inaccurate = "*";
		}

		if (dayTimer < sunSet && inaccurate == "") {
			duskDawnTimer = " / Dusk in: " + ConvertSecondsToTime(sunSet - dayTimer);
		} else if (dayTimer > sunSet && inaccurate == "") {
			duskDawnTimer = " / Dawn in: " + ConvertSecondsToTime(dayLength - dayTimer);
		}

		info += "Time of your location:\n";
		info += "Time: " + GetTimeString(dayTimer) + duskDawnTimer + inaccurate + "\n";
		info += "Dawn: " + GetTimeString(dayLength) + " / Daylength: " + ConvertSecondsToTime(sunSet) + inaccurate + "\n";
		info += "Dusk: " + GetTimeString(sunSet) + " / Nightlength: " + ConvertSecondsToTime(dayLength - sunSet) + inaccurate + "\n";

		if (inaccurate != "") {
			info += inaccurate + inaccurateLegend;
		}
		infoShown = true;
	}

	if (!infoShown) {
		info += "-- No informations to show --";
	}

	return info;
}


/// <summary>
/// Creates a string with two lines containing heading, two values, the percentage of the two and a level bar in the second row
/// </summary>
/// <param name="heading">Heading (top left) as string</param>
/// <param name="value">Value as double</param>
/// <param name="valueMax">Max value as double</param>
/// <param name="valueStr">Optional: a string instead of the double value</param>
/// <param name="valueMaxStr">Optional: a string instead of the double value</param>
/// <returns>See summary</returns>
string CreateBarString(double fontSize, string heading, double value, double valueMax, string valueStr = null, string valueMaxStr = null)
{
	string current = value.ToString();
	string max = valueMax.ToString();

	if (valueStr != null) {
		current = valueStr;
	}

	if (valueMaxStr != null) {
		max = valueMaxStr;
	}

	string percent = GetPercentString(value, valueMax);
	string values = current + " / " + max;
	int lcdWidth = (int)(26 / fontSize);

	StringBuilder firstLine, secondLine;

	// If fontSize is less than 0.6 use default layout, else slim layout
	if (fontSize <= 0.6) {
		// First line
		firstLine = new StringBuilder(heading + " ");
		firstLine.Append(StringRepeat(' ', lcdWidth / 2 - (firstLine.Length + current.Length)));
		firstLine.Append(current + " / " + max);
		firstLine.Append(StringRepeat(' ', lcdWidth - (firstLine.Length + percent.Length)));
		firstLine.Append(percent + "\n");

		// Second line
		secondLine = new StringBuilder("[" + StringRepeat('.', lcdWidth - 2) + "]\n");
		int fillLevel = (int)Math.Ceiling((lcdWidth - 2) * (value / valueMax));
		try {
			secondLine.Replace(".", "I", 1, fillLevel);
		}
		catch (Exception) {
			// ignore
		}
	} else {
		// First line
		firstLine = new StringBuilder(heading + " ");
		firstLine.Append(StringRepeat(' ', lcdWidth - (firstLine.Length + values.Length)));
		firstLine.Append(values + "\n");

		// Second line
		secondLine = new StringBuilder("[" + StringRepeat('.', lcdWidth - 8) + "]");
		secondLine.Append(StringRepeat(' ', lcdWidth - (secondLine.Length + percent.Length)));
		secondLine.Append(percent + "\n");

		int fillLevel = (int)Math.Ceiling((lcdWidth - 8) * (value / valueMax));
		try {
			secondLine.Replace(".", "I", 1, fillLevel);
		}
		catch (Exception) {
			// ignore
		}
	}

	return firstLine.Append(secondLine).ToString();
}


/// <summary>
/// Repeats a char a certain number of times and return it as a string
/// </summary>
/// <param name="charToRepeat">Char to repeat as char</param>
/// <param name="numberOfRepetitions">Number of repetitions as int</param>
/// <returns>Repeated char as string</returns>
string StringRepeat(char charToRepeat, int numberOfRepetitions)
{
	if (numberOfRepetitions <= 0) {
		return "";
	}
	return new string(charToRepeat, numberOfRepetitions);
}


/// <summary>
/// Write the informationsString on all specified LCDs
/// </summary>
void WriteLCD()
{
	if (lcds.Count == 0) return;

	foreach (var lcd in lcds) {
		// Get the wanted statistics to show
		bool addCurrentOperation = ReadCustomDataLCD(lcd, "showCurrentOperation");
		bool addSolarStats = ReadCustomDataLCD(lcd, "showSolarStats");
		bool addBatteryStats = ReadCustomDataLCD(lcd, "showBatteryStats");
		bool addOxygenStats = ReadCustomDataLCD(lcd, "showOxygenStats");
		bool addLocationTime = ReadCustomDataLCD(lcd, "showLocationTime");

		// Get the font size
		float fontSize = lcd.FontSize;

		// Create the text
		string info = CreateInformation(false, fontSize, addCurrentOperation, addSolarStats, addBatteryStats, addOxygenStats, addLocationTime);
		string lcdText = CreateScrollingText(fontSize, info, lcd);

		// Print contents to its public text
		lcd.WritePublicTitle("Isy's Solar Alignment Script");
		lcd.WritePublicText(lcdText, false);
		lcd.Font = "Monospace";
		lcd.ShowPublicTextOnScreen();
	}
}


/// <summary>
/// Write part of the informationString on all corner LCDs
/// </summary>
void WriteCornerLCD()
{
	if (cornerLcds.Count == 0) return;

	foreach (var lcd in cornerLcds) {
		// Prepare the text based on the custom data of the panel
		string cornerLcdText = "";
		if (lcd.CustomData == "time") {
			cornerLcdText += "\n";
			cornerLcdText += StringRepeat(' ', 36);
			cornerLcdText += GetTimeString(dayTimer);
		} else if (lcd.CustomData == "battery") {
			cornerLcdText += "Statistics for " + batteries.Count + " Batteries:\n";
			cornerLcdText += "Current I/O: " + batteriesCurrentInputStr + " in, " + batteriesCurrentOutputStr + " out\n";
			cornerLcdText += "Stored Power: " + batteriesPowerStr + " / " + batteriesMaxPowerStr + " (" + GetPercentString(batteriesPower, batteriesMaxPower) + ")";
		} else if (lcd.CustomData == "oxygen") {
			cornerLcdText += "Statistics for Oxygen:\n";
			if (oxygenFarms.Count > 0) {
				cornerLcdText += "Oxygen Farms: " + oxygenFarms.Count + ", Efficiency: " + oxygenFarmEfficiency + "%\n";
			}
			if (oxygenTanks.Count > 0) {
				cornerLcdText += "Oxygen Tanks: " + oxygenTanks.Count + ", " + oxygenTankFillLevelStr + " / " + oxygenTankCapacityStr + " (" + GetPercentString(oxygenTankFillLevel, oxygenTankCapacity) + ")";
			}
		} else {
			cornerLcdText += "Statistics for " + solarPanels.Count + " Solar Panels:\n";
			cornerLcdText += "Max. Output: " + maxOutputAPStr + " / " + maxDetectedOutputAPStr + " (" + GetPercentString(maxOutputAP, maxDetectedOutputAP) + ")\n";
			cornerLcdText += "Current Output: " + currentOutputAPStr + " / " + maxDetectedOutputAPStr + " (" + GetPercentString(currentOutputAP, maxDetectedOutputAP) + ")\n";
		}

		// Print contents to its public text
		lcd.WritePublicText(cornerLcdText, false);
		lcd.FontSize = 0.9f;
		lcd.ShowPublicTextOnScreen();
	}
}


/// <summary>
/// Write debugging information on the debugLcd
/// </summary>
void WriteDebugLCD()
{
	// Find the debugLcd
	IMyTextPanel lcd = GridTerminalSystem.GetBlockWithName(debugLcd) as IMyTextPanel;
	if (lcd == null) return;

	// Average counter increase/reset
	if (avgCounter == 99) {
		avgCounter = 0;
	} else {
		avgCounter++;
	}

	// Get the font size
	float fontSize = lcd.FontSize;

	// Create the debug text
	string text = "Solar Alignment Debug\n=====================\n\n";

	// Instruction count
	if (showInstructionCount) {
		int curInstructions = Runtime.CurrentInstructionCount;
		if (curInstructions > maxInstructions) maxInstructions = curInstructions;
		instructions[avgCounter] = curInstructions;
		double avgInstructions = instructions.Sum() / instructions.Count;

		text += CreateBarString(fontSize, "Instructions", curInstructions, Runtime.MaxInstructionCount);
		text += CreateBarString(fontSize, "Max. Instructions", maxInstructions, Runtime.MaxInstructionCount);
		text += CreateBarString(fontSize, "Avg. Instructions", Math.Floor(avgInstructions), Runtime.MaxInstructionCount);
		text += "\n";
	}

	// Execution time
	if (showExecutionTime) {
		double curRuntime = Runtime.LastRunTimeMs;
		if (curRuntime > maxRuntime) maxRuntime = curRuntime;
		runtime[avgCounter] = curRuntime;
		double avgRuntime = runtime.Sum() / runtime.Count;

		text += CreateBarString(fontSize, "Last runtime", curRuntime, 1, Math.Round(curRuntime, 4) + " ms", "1 ms");
		text += CreateBarString(fontSize, "Max. runtime", maxRuntime, 1, Math.Round(maxRuntime, 4) + " ms", "1 ms");
		text += CreateBarString(fontSize, "Avg. runtime", avgRuntime, 1, Math.Round(avgRuntime, 4) + " ms", "1 ms");
		text += "\n";
	}

	// Execution tick
	if (showScriptExecutionThread) {
		text += CreateBarString(fontSize, "Execution thread", execCounter, 5);
		text += "\n";
	}

	// Blocks
	if (showBlockCounts) {
		text += "Rotors: " + rotors.Count + "\n";
		text += "Gyros: " + gyros.Count + "\n";
		text += "Solar Panels: " + solarPanels.Count + "\n";
		text += "Oxygen Farms: " + oxygenFarms.Count + "\n";
		text += "Oxygen Tanks: " + oxygenTanks.Count + "\n";
		text += "Batteries: " + batteries.Count + "\n";
		text += "Reactors: " + reactors.Count + "\n";
		text += "LCDs: " + lcds.Count + "\n";
		text += "Corner LCDs: " + cornerLcds.Count + "\n";
		text += "Lights: " + lights.Count + "\n";
		text += "Spotlights: " + spotlights.Count + "\n";
		text += "Timer Blocks: " + timers.Length + "\n";
	}

	// Print contents to its public text
	lcd.WritePublicTitle("Solar Alignment Debug");
	lcd.WritePublicText(CreateScrollingText(fontSize, text, lcd), false);
	lcd.Font = "Monospace";
	lcd.ShowPublicTextOnScreen();
}


/// <summary>
/// Creates a scrolling text for an LCD panel
/// </summary>
/// <param name="text">Text to display as string</param>
/// <param name="lcd">LCD that should use the text as IMyTextPanel (this is just for saving the current scrolling)</param>
/// <returns>Scrolled substring of the input text as string</returns>
string CreateScrollingText(float fontSize, string text, IMyTextPanel lcd)
{
	// Get the LCD EntityId
	long id = lcd.EntityId;

	// Create default entry for the LCD in the dictionary
	if (!scroll.ContainsKey(id)) {
		scroll[id] = new List<int>{ 1, 3, 3 };
	}

	int scrollDirection = scroll[id][0];
	int scrollWait = scroll[id][1];
	int lineStart = scroll[id][2];

	// Figure out the amount of lines for scrolling content
	var linesTemp = text.TrimEnd('\n').Split('\n');
	List<string> lines = new List<string>();
	int lcdHeight = (int)Math.Ceiling(17 / fontSize);
	int lcdWidth = (int)(26 / fontSize);
	string lcdText = "";

	// Build the lines list out of lineTemp and add line breaks if text is too long for one line
	foreach (var line in linesTemp) {
		if (line.Length <= lcdWidth) {
			lines.Add(line);
		} else {
			try {
				int lastSpace = line.LastIndexOf(' ', lcdWidth);
				lines.Add(line.Substring(0, lastSpace));
				lines.Add(line.Substring(lastSpace));
			}
			catch (Exception) {
				lines.Add(line);
			}
		}
	}

	if (lines.Count > lcdHeight) {
		if (execCounter % 5 == 0) {
			if (scrollWait > 0) scrollWait--;
			if (scrollWait <= 0) lineStart += scrollDirection;

			if (lineStart + lcdHeight - 3 >= lines.Count && scrollWait <= 0) {
				scrollDirection = -1;
				scrollWait = 3;
			}
			if (lineStart <= 3 && scrollWait <= 0) {
				scrollDirection = 1;
				scrollWait = 3;
			}
		}
	} else {
		lineStart = 3;
		scrollDirection = 1;
		scrollWait = 3;
	}

	// Save the current scrolling in the dictionary
	scroll[id][0] = scrollDirection;
	scroll[id][1] = scrollWait;
	scroll[id][2] = lineStart;

	// Always create header
	for (var line = 0; line < 3; line++) {
		lcdText += lines[line] + "\n";
	}

	// Create scrolling content based on the starting line
	for (var line = lineStart; line < lines.Count; line++) {
		lcdText += lines[line] + "\n";
	}

	return lcdText;
}


/// <summary>
/// Calculate the location time based on output measuring
/// </summary>
void TimeCalculation()
{
	// Continous day timer in seconds
	dayTimer += 1;
	safetyTimer += 1;

	// Failsafe for day timer if no day / night cycle could be measured after 48 hours
	if (dayTimer > 172800) {
		dayTimer = 0;
		safetyTimer = 0;
	}

	double nightOutput = maxDetectedOutputAP * nightTimePercentage;

	// Detect sunset
	if (maxOutputAP < nightOutput && maxOutputAPLast >= nightOutput && safetyTimer > 300) {
		sunSet = dayTimer;
		safetyTimer = 0;
	}

	// Reset day timer (sunrise)
	if (maxOutputAP > nightOutput && maxOutputAPLast <= nightOutput && safetyTimer > 300) {
		if (sunSet != sunSetDefault) {
			dayLength = dayTimer;
		}
		dayTimer = 0;
		safetyTimer = 0;
	}

	// Correction of daylength in case sunset is higher from an old run
	if (sunSet > dayLength) {
		dayLength = sunSet * 2;
	}
}


/// <summary>
/// Create a time string based on a double value
/// </summary>
/// <param name="timeToEvaluate">Any double value</param>
/// <param name="returnHour">Optional: true only returns the hour</param>
/// <returns>String like "16:30"</returns>
string GetTimeString(double timeToEvaluate, bool returnHour = false)
{
	string timeString = "";

	// Mod the timeToEvaluate by dayLength in order to avoid unrealistic times
	timeToEvaluate = timeToEvaluate % dayLength;

	// Calculate Midnight
	double midNight = sunSet + (dayLength - sunSet) / 2D;

	// Calculate Time
	double hourLength = dayLength / 24D;
	double time;
	if (timeToEvaluate < midNight) {
		time = (timeToEvaluate + (dayLength - midNight)) / hourLength;
	} else {
		time = (timeToEvaluate - midNight) / hourLength;
	}

	double timeHour = Math.Floor(time);
	double timeMinute = Math.Floor((time % 1 * 100) * 0.6);
	string timeHourStr = timeHour.ToString("00");
	string timeMinuteStr = timeMinute.ToString("00");

	timeString = timeHourStr + ":" + timeMinuteStr;

	if (returnHour) {
		return timeHour.ToString();
	} else {
		return timeString;
	}
}


/// <summary>
/// Creates a mixed string of hours, minutes and seconds out of an integer of seconds
/// </summary>
/// <param name="seconds">Any integer value</param>
/// <returns>String like "03:30:20"</returns>
string ConvertSecondsToTime(int seconds)
{
	string result = "";

	TimeSpan ts = TimeSpan.FromSeconds(seconds);
	result = ts.ToString(@"hh\:mm\:ss");

	return result;
}


/// <summary>
/// Activate reactors on certain power conditions
/// </summary>
void ReactorFallback()
{
	if (reactors.Count == 0) return;

	bool enableReactors = false;
	double lowBattery = lowBatteryPercentage % 100 / 100;
	double overload = overloadPercentage % 100 / 100;

	// Activate on low battery charge
	if (activateOnLowBattery && batteriesPower < batteriesMaxPower * lowBattery) {
		enableReactors = true;
		currentOperationInfo = "Reactors active: Low battery charge!";
	}

	// Activate on overload
	if (activateOnOverload && batteriesCurrentOutput + currentOutputAP > (batteriesMaxOutput + maxOutputAP) * overload) {
		enableReactors = true;
		currentOperationInfo = "Reactors active: Overload!";
	}

	// Set the reactor state
	foreach (var reactor in reactors) {
		if (enableReactors) {
			reactor.Enabled = true;
		} else {
			reactor.Enabled = false;
		}
	}
}


/// <summary>
/// Switch the lights based on the current time
/// </summary>
void LightManagement()
{
	if (lights.Count == 0 && spotlights.Count == 0) return;

	int hour = 0;
	int.TryParse(GetTimeString(dayTimer, true), out hour);
	bool lightState = true;

	// Figure out if the lights should be on or off
	if (!simpleMode) {
		if (dayTimer != dayLength && hour >= lightOffHour && hour < lightOnHour) {
			lightState = false;
		} else if (dayTimer == dayLength && maxOutputAP > maxDetectedOutputAP * nightTimePercentage) {
			lightState = false;
		}
	} else {
		if (maxOutputAP > maxDetectedOutputAP * (simpleThreshold % 100) / 100) lightState = false;
	}

	// Toggle all interior lights
	foreach (var light in lights) {
		light.Enabled = lightState;
	}

	// Toggle all spotlights
	foreach (var spotLight in spotlights) {
		spotLight.Enabled = lightState;
	}
}


/// <summary>
/// Trigger an external timer block based on a set of different events
/// </summary>
void TriggerExternalTimerBlock()
{
	// Error management
	if (events.Length == 0) {
		CreateWarning("No events for triggering specified!");
	} else if (timers.Length == 0) {
		CreateWarning("No timers for triggering specified!");
	} else if (events.Length != timers.Length) {
		CreateWarning("Every event needs a timer block name!\nFound " + events.Length + " events and " + timers.Length + " timers.");
	} else {
		int timerToTrigger = -1;
		string triggerEvent = "";
		int seconds;

		// Cycle through each entry in events and check if the current conditions match the entry
		for (int i = 0; i <= events.Length - 1; i++) {
			if (events[i] == "sunrise" && dayTimer == 0) {
				timerToTrigger = i;
				triggerEvent = "sunrise";
			} else if (events[i] == "sunset" && dayTimer == sunSet) {
				timerToTrigger = i;
				triggerEvent = "sunset";
			} else if (int.TryParse(events[i], out seconds) == true && dayTimer % seconds == 0) {
				timerToTrigger = i;
				triggerEvent = seconds + " seconds";
			} else if (GetTimeString(dayTimer) == events[i]) {
				timerToTrigger = i;
				triggerEvent = events[i];
			}
		}

		// Cycle through all the timers and see if everything is set up correctly
		foreach (var item in timers) {
			var timer = GridTerminalSystem.GetBlockWithName(item) as IMyTimerBlock;
			if (timer == null) {
				CreateWarning("External timer block not found:\n'" + timer.CustomName + "'");
			} else {
				if (timer.OwnerId != Me.OwnerId) {
					CreateWarning("'" + timer.CustomName + "' has a different owner!\nAll blocks should have the same owner!");
				}
				if (timer.Enabled == false) {
					CreateWarning("'" + timer.CustomName + "' is turned off!\nTurn it on in order to be used by the script!");
				}
			}
		}

		// Trigger the timer block if a event matches the current conditions
		if (timerToTrigger >= 0) {
			// Find the timer block
			var timer = GridTerminalSystem.GetBlockWithName(timers[timerToTrigger]) as IMyTimerBlock;

			if (timer != null) {
				timer.ApplyAction("Start");
				currentOperation = "External timer triggered! Reason: " + triggerEvent;
			}
		}
	}
}


/// <summary>
/// Creates an error with the given text and stops all rotors or gyros
/// </summary>
/// <param name="text">Errortext as string</param>
void CreateError(string text)
{
	StopAll();
	if (error == null) error = text;
	errorCount++;
}


/// <summary>
/// Creates a warning with the given text
/// </summary>
/// <param name="text">Warningtext as string</param>
void CreateWarning(string text)
{
	if (warning == null) warning = text;
	warningCount++;
}


/// <summary>
/// Removes all terminal statistics
/// </summary>
void RemoveTerminalStatistics()
{
	// Solar Panels
	foreach (var solarPanel in solarPanels) {
		solarPanel.CustomData = "";
		AddStatusToName(solarPanel, false);
	}

	// Batteries
	foreach (var battery in batteries) {
		AddStatusToName(battery, false);
	}

	// Oxygen Farms
	foreach (var oxygenFarm in oxygenFarms) {
		AddStatusToName(oxygenFarm, false);
	}

	// Oxygen Tanks
	foreach (var oxygenTank in oxygenTanks) {
		AddStatusToName(oxygenTank, false);
	}
}


/// <summary>
/// Loads the time calculation after world load or recompile
/// </summary>
void Load()
{
	// Load variables out of the programmable block's custom data field
	if (Me.CustomData.Length > 0) {
		var data = Me.CustomData.Split('\n');

		foreach (var line in data) {
			var entry = line.Split('=');
			if (entry.Length != 2) continue;

			if (entry[0] == "dayTimer") {
				int.TryParse(entry[1], out dayTimer);
			} else if (entry[0] == "dayLength") {
				int.TryParse(entry[1], out dayLength);
			} else if (entry[0] == "sunSet") {
				int.TryParse(entry[1], out sunSet);
			} else if (entry[0] == "outputLast") {
				double.TryParse(entry[1], out maxOutputAPLast);
			} else if (entry[0] == "maxDetectedOutput") {
				double.TryParse(entry[1], out maxDetectedOutputAP);
			} else if (entry[0] == "solarPanelsCount") {
				int.TryParse(entry[1], out solarPanelsCount);
			} else if (entry[0] == "oxygenFarmsCount") {
				int.TryParse(entry[1], out oxygenFarmsCount);
			} else if (entry[0] == "action") {
				action = entry[1];
			}
		}

		if (action == "paused") pause = true;
	}
}


/// <summary>
/// Save the time calculation on world close and recompile
/// </summary>
public void Save()
{
	// Save variables into the programmable block's custom data field   
	string customData = "";

	customData += "dayTimer=" + dayTimer + "\n";
	customData += "dayLength=" + dayLength + "\n";
	customData += "sunSet=" + sunSet + "\n";
	customData += "outputLast=" + maxOutputAPLast + "\n";
	customData += "maxDetectedOutput=" + maxDetectedOutputAP + "\n";
	customData += "solarPanelsCount=" + solarPanels.Count + "\n";
	customData += "oxygenFarmsCount=" + oxygenFarms.Count + "\n";
	customData += "action=" + action;

	Me.CustomData = customData;
}
