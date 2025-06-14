const string customDataInit = @"; CustomData config:
; the [global] section applies to the whole program, or sets defaults for shared
;
; For surface selection, use 'name <number>' eg: 'Cockpit <1>' - by default, the
; first surface is selected (0)
;
; The output section of the config is the template to render to the screen

[global]
;  global program settings (will overide settings detected in templates)
;  eg if a template has {power.bar}, then power will be enabled unless false here
;airlock=true
;production=false
;cargo=false
;power=false
;health=false
;gas=false
;gasEnableFillPct=-1
;gasDisableFillPct=-1
;  airlock config (defaults are shown)
;  just name 2 doors the same name (matching airlockDoorMatch) and they will only open 1 at a time
;  airlockAllDoors=true will auto close all open doors, unless the name matches airlockDoorExclude
;  airlockDoorManual will still lock, but not auto close airlock pairs matching the name (by default ""[AL]"")
;airlockOpenTime=750
;airlockAllDoors=false
;airlockDoorMatch=Door(.*)
;airlockDoorExclude=Hangar
;airlockDoorManual=\\[AL\\]
;  health config (defaults are shown)
;healthIgnore=
;healthOnHud=false

[Programmable Block <0>]
output=
|{config:size=0.45;bgColour=0,10,30}
|{text:colour=120,50,50:JUMP DRIVES:} {power.jumpDrives}
|{power.jumpBar}
|{text:colour=120,50,50:BATTERIES:} {power.batteries}
|{power.batteryBar}
|{text:colour=120,50,50:ENERGY IO:} {power.ioString}
|{power.ioBar}
|{power.ioLegend}
|
|{text:colour=120,50,50:PRODUCTION:}{setCursor:x=50%}{setCursor:x=+1.5}{text:colour=120,50,50:DAMAGE:} {health.status}
|{?saveCursor}
|{production.status}
|{production.blocks}
|{?setCursor:x=50%}{setCursor:x=+1.5}{saveCursor:y=y}
|{health.blocks}
|{?setCursor:x=0;y=~y}{saveCursor}
|
|{text:colour=120,50,50:CARGO:} {cargo.fullString}
|{cargo.bar}
|{cargo.items}
";

public interface Runnable {
    void Reset();
    void Clear();
    void GetBlock(IMyTerminalBlock block);
    void GotBLocks();
    void Refresh();
}

public StringBuilder log = new StringBuilder("");
Dictionary<string, DrawingSurface> drawables = new Dictionary<string, DrawingSurface>();
List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
List<IMyTerminalBlock> allBlocks = new List<IMyTerminalBlock>();
List<string> strings = new List<string>();
MyIni ini = new MyIni();
Template template;
Config config = new Config();
Dictionary<string, string> templates = new Dictionary<string, string>();
IEnumerator<string> stateMachine;
int tickCount = 1;

PowerDetails powerDetails;
CargoStatus cargoStatus;
BlockHealth blockHealth;
ProductionDetails productionDetails;
Airlock airlock;
GasStatus gasStatus;

public Program() {
    GridTerminalSystem.GetBlocks(allBlocks);
    template = new Template(this);
    powerDetails = new PowerDetails(this, template);
    cargoStatus = new CargoStatus(this, template);
    blockHealth = new BlockHealth(this, template);
    productionDetails = new ProductionDetails(this, template);
    airlock = new Airlock(this);
    gasStatus = new GasStatus(this, template);

    if (!Configure()) {
        return;
    }
    RefetchBlocks();
}

public void Main(string argument, UpdateType updateType) {
    Echo(log.ToString());

    if ((updateType & UpdateType.Once) == UpdateType.Once) {
        RunStateMachine();
        return;
    }

    if ((updateType & UpdateType.Update10) == UpdateType.Update10 && config.Enabled("airlock")) {
        airlock.CheckAirlocks();
    }

    if ((updateType & UpdateType.Update100) == UpdateType.Update100) {
        if (RecheckFailed()) {
            Runtime.UpdateFrequency &= UpdateFrequency.None;
            Echo("Failed to parse custom data");
            return;
        }

        if (stateMachine == null) {
            stateMachine = RunStuffOverTime();
            Runtime.UpdateFrequency |= UpdateFrequency.Once;
        }
    }
}
/* MAIN */
