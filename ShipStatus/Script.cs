const string customDataInit=@"; CustomData config:
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
public interface Runnable{void Reset();void Clear();void GetBlock(IMyTerminalBlock block);void GotBLocks();void Refresh();}public StringBuilder log=new StringBuilder("");Dictionary<string,DrawingSurface>
drawables=new Dictionary<string,DrawingSurface>();List<IMyTerminalBlock>blocks=new List<IMyTerminalBlock>();List<IMyTerminalBlock>allBlocks=new List<IMyTerminalBlock>();List<string>strings=new List<string>();
MyIni ini=new MyIni();Template template;Config config=new Config();Dictionary<string,string>templates=new Dictionary<string,string>();IEnumerator<string>stateMachine;int tickCount=1;PowerDetails powerDetails;
CargoStatus cargoStatus;BlockHealth blockHealth;ProductionDetails productionDetails;Airlock airlock;GasStatus gasStatus;public Program(){GridTerminalSystem.GetBlocks(allBlocks);template=new Template(this);
powerDetails=new PowerDetails(this,template);cargoStatus=new CargoStatus(this,template);blockHealth=new BlockHealth(this,template);productionDetails=new ProductionDetails(this,template);airlock=new Airlock(this);
gasStatus=new GasStatus(this,template);if(!Configure()){return;}RefetchBlocks();}public void Main(string argument,UpdateType updateType){Echo(log.ToString());if((updateType&UpdateType.Once)==UpdateType.Once)
{RunStateMachine();return;}if((updateType&UpdateType.Update10)==UpdateType.Update10&&config.Enabled("airlock")){airlock.CheckAirlocks();}if((updateType&UpdateType.Update100)==UpdateType.Update100){if(RecheckFailed())
{Runtime.UpdateFrequency&=UpdateFrequency.None;Echo("Failed to parse custom data");return;}if(stateMachine==null){stateMachine=RunStuffOverTime();Runtime.UpdateFrequency|=UpdateFrequency.Once;}}}public
struct AirlockDoors{public double openTimer;public List<IMyFunctionalBlock>blocks;public bool shouldAutoClose;public AirlockDoors(List<IMyFunctionalBlock>doors,bool shouldAutoClose,double openTimer){this.blocks
=doors;this.shouldAutoClose=shouldAutoClose;this.openTimer=openTimer;}}public class Airlock:Runnable{private Program program;private Dictionary<string,AirlockDoors>airlocks;private Dictionary<string,List<IMyFunctionalBlock>>
locationToAirlockMap;private System.Text.RegularExpressions.Regex include;private System.Text.RegularExpressions.Regex exclude;private System.Text.RegularExpressions.Regex manual;private double timeOpen
=750f;private List<IMyFunctionalBlock>areClosed;private List<IMyFunctionalBlock>areOpen;private bool doAllDoors;private bool enabled;public Airlock(Program program){this.program=program;this.airlocks=
new Dictionary<string,AirlockDoors>();this.locationToAirlockMap=new Dictionary<string,List<IMyFunctionalBlock>>();this.areClosed=new List<IMyFunctionalBlock>();this.areOpen=new List<IMyFunctionalBlock>();
this.Reset();}public void Reset(){this.Clear();this.include=Util.Regex(this.program.config.Get("airlockDoorMatch","Door(.*)"));this.exclude=Util.Regex(this.program.config.Get("airlockDoorExclude","Hangar|Hatch"));
this.manual=Util.Regex(this.program.config.Get("airlockDoorManual","\\[AL\\]"));this.timeOpen=Util.ParseFloat(this.program.config.Get("airlockOpenTime"),750f);this.doAllDoors=this.program.config.Enabled("airlockAllDoors");
this.enabled=this.program.config.Enabled("airlock");}public void Clear(){this.airlocks.Clear();this.locationToAirlockMap.Clear();}public void CheckAirlocks(){if(!this.enabled){return;}foreach(string key
in this.airlocks.Keys.ToList()){AirlockDoors airlockDoors=this.airlocks.Get(key);this.Check(ref airlockDoors);this.airlocks[key]=airlockDoors;}}public void GetBlock(IMyTerminalBlock block){if(!(block is
IMyDoor)){return;}var match=this.include.Match(block.CustomName);var ignore=this.exclude.Match(block.CustomName);if(!match.Success||ignore.Success){return;}var key=match.Groups[1].ToString();if(!this.locationToAirlockMap.ContainsKey(key))
{this.locationToAirlockMap.Add(key,new List<IMyFunctionalBlock>());}this.locationToAirlockMap[key].Add(block as IMyFunctionalBlock);}public void GotBLocks(){foreach(var keyval in this.locationToAirlockMap)
{if(!this.doAllDoors&&keyval.Value.Count<2){continue;}bool shouldAutoClose=true;foreach(var door in keyval.Value){if(manual.Match(door.CustomName).Success){shouldAutoClose=false;}}this.airlocks.Add(keyval.Key,
new AirlockDoors(keyval.Value,shouldAutoClose,this.timeOpen));}}public void Refresh(){}private bool IsOpen(IMyFunctionalBlock door){return(door as IMyDoor).OpenRatio>0;}private void Lock(List<IMyFunctionalBlock>
doors){foreach(var door in doors){(door as IMyDoor).Enabled=false;}}private void Unlock(List<IMyFunctionalBlock>doors){foreach(var door in doors){(door as IMyDoor).Enabled=true;}}private void OpenClose(string
action,IMyFunctionalBlock door1,IMyFunctionalBlock door2=null){(door1 as IMyDoor).ApplyAction(action);if(door2!=null){(door2 as IMyDoor).ApplyAction(action);}}private void CloseAll(List<IMyFunctionalBlock>
doors){foreach(var door in doors){this.OpenClose("Open_Off",door);}}public bool Check(ref AirlockDoors airlockDoors){int openCount=0;this.areClosed.Clear();this.areOpen.Clear();bool shouldLog=airlockDoors.blocks[0].CustomName[0]
=='_';foreach(IMyFunctionalBlock door in airlockDoors.blocks){if(!Util.BlockValid(door)){continue;}if(this.IsOpen(door)){openCount++;this.areOpen.Add(door);}else{this.areClosed.Add(door);}}if(this.areOpen.Count
==0){this.Unlock(airlockDoors.blocks);airlockDoors.openTimer=this.timeOpen;return true;}airlockDoors.openTimer-=this.program.Runtime.TimeSinceLastRun.TotalMilliseconds;if(airlockDoors.openTimer<0&&airlockDoors.shouldAutoClose)
{this.CloseAll(airlockDoors.blocks);}else{this.Lock(this.areClosed);this.Unlock(this.areOpen);}return true;}}public class CargoStatus:Runnable{public Program program;public List<IMyTerminalBlock>cargoBlocks;
public Dictionary<string,VRage.MyFixedPoint>cargoItemCounts;public List<MyInventoryItem>inventoryItems;public System.Text.RegularExpressions.Regex itemRegex;public System.Text.RegularExpressions.Regex
ingotRegex;public System.Text.RegularExpressions.Regex oreRegex;public VRage.MyFixedPoint max;public VRage.MyFixedPoint vol;public Template template;public List<float>widths;public string itemText;public
float pct;public CargoStatus(Program program,Template template=null){this.program=program;this.template=template;this.itemRegex=Util.Regex(".*/");this.ingotRegex=Util.Regex("Ingot/");this.oreRegex=Util.Regex("Ore/(?!Ice)");
this.widths=new List<float>(){0,0,0,0};this.cargoItemCounts=new Dictionary<string,VRage.MyFixedPoint>();this.inventoryItems=new List<MyInventoryItem>();this.cargoBlocks=new List<IMyTerminalBlock>();this.itemText
="";this.pct=0f;this.Reset();}public void Reset(){this.Clear();if(this.program.config.Enabled("cargo")){this.RegisterTemplateVars();}}public void Clear(){this.cargoItemCounts.Clear();this.inventoryItems.Clear();
this.cargoBlocks.Clear();}public void RegisterTemplateVars(){if(this.template==null){return;}this.template.Register("cargo.stored",()=>$"{Util.FormatNumber(1000 * this.vol)} L");this.template.Register("cargo.cap",
()=>$"{Util.FormatNumber(1000 * this.max)} L");this.template.Register("cargo.fullString",()=>{string capFmt=Util.GetFormatNumberStr(1000*this.max);return$"{Util.FormatNumber(1000 * this.vol, capFmt)} / {Util.FormatNumber(1000 * this.max, capFmt)} L";
});this.template.Register("cargo.bar",this.CargoBar);this.template.Register("cargo.items",this.CargoItems);this.template.Register("cargo.item",this.CargoItem);}public void CargoBar(DrawingSurface ds,string
text,DrawingSurface.Options options){Color?defaultText=options.textColour??ds.surface.ScriptForegroundColor;string colourName=options.custom.Get("colourLow")??"dimgreen";options.textColour=DrawingSurface.StringToColour(options.custom.Get("textColourLow"))
??defaultText;if(this.pct>0.85){colourName=options.custom.Get("colourHigh")??"dimred";options.textColour=DrawingSurface.StringToColour(options.custom.Get("textColourHigh"))??defaultText;}else if(this.pct
>0.60){colourName=options.custom.Get("colourMid")??"dimyellow";options.textColour=DrawingSurface.StringToColour(options.custom.Get("textColourMid"))??defaultText;}options.pct=this.pct;options.fillColour
=DrawingSurface.StringToColour(colourName);options.text=Util.PctString(this.pct);ds.Bar(options);}public void CargoItem(DrawingSurface ds,string text,DrawingSurface.Options options){string name=options.custom.Get("name");
VRage.MyFixedPoint itemCount;if(!this.cargoItemCounts.TryGetValue(name,out itemCount)){itemCount=0;}ds.Text(Util.FormatNumber(itemCount),options);}public void CargoItems(DrawingSurface ds,string text,
DrawingSurface.Options options){if(this.cargoItemCounts.Count()==0){ds.Text(" ");return;}if(ds.width/(ds.charSizeInPx.X+1f)<40){foreach(var item in this.cargoItemCounts){var fmtd=Util.FormatNumber(item.Value);
ds.Text($"{item.Key}").SetCursor(ds.width,null).Text(fmtd,textAlignment:TextAlignment.RIGHT).Newline();}}else{this.widths[0]=0;this.widths[1]=ds.width/2-1.5f*ds.charSizeInPx.X;this.widths[2]=ds.width/
2+1.5f*ds.charSizeInPx.X;this.widths[3]=ds.width;int i=0;foreach(var item in this.cargoItemCounts){var fmtd=Util.FormatNumber(item.Value);ds.SetCursor(this.widths[(i++%4)],null).Text($"{item.Key}").SetCursor(this.widths[(i++
%4)],null).Text(fmtd,textAlignment:TextAlignment.RIGHT);if((i%4)==0||i>=this.cargoItemCounts.Count*2){ds.Newline();}}}ds.Newline(reverse:true);}public void GetBlock(IMyTerminalBlock block){if(block is
IMyCargoContainer||block is IMyShipDrill||block is IMyShipConnector||block is IMyAssembler||block is IMyRefinery){this.cargoBlocks.Add(block);}}public void GotBLocks(){}public void Refresh(){if(this.cargoBlocks
==null){return;}this.cargoItemCounts.Clear();this.inventoryItems.Clear();this.max=0;this.vol=0;this.pct=0f;string fullName="";string itemName="";foreach(var c in this.cargoBlocks){if(!Util.BlockValid(c))
{continue;}var inv=c.GetInventory(0);this.vol+=inv.CurrentVolume;this.max+=inv.MaxVolume;this.inventoryItems.Clear();inv.GetItems(this.inventoryItems);for(var i=0;i<this.inventoryItems.Count;i++){fullName
=this.inventoryItems[i].Type.ToString();itemName=this.itemRegex.Replace(fullName,"");if(itemName=="Stone"){if(this.ingotRegex.IsMatch(fullName)){itemName="Gravel";}else{itemName="Stone";}}else if(this.ingotRegex.IsMatch(fullName))
{itemName+=" Ingot";}else if(this.oreRegex.IsMatch(fullName)){itemName+=" Ore";}var itemQty=this.inventoryItems[i].Amount;if(!this.cargoItemCounts.ContainsKey(itemName)){this.cargoItemCounts.Add(itemName,
itemQty);}else{this.cargoItemCounts[itemName]=this.cargoItemCounts[itemName]+itemQty;}}}this.cargoItemCounts=this.cargoItemCounts.OrderBy(x=>-x.Value.ToIntSafe()).ToDictionary(x=>x.Key,x=>x.Value);if(this.max
!=0){this.pct=(float)this.vol/(float)this.max;}return;}}public class Config{public Dictionary<string,string>settings;public string customData;public Config(){this.settings=new Dictionary<string,string>();
}public void Clear(){this.settings.Clear();this.customData=null;}public void Set(string name,string value){this.settings[name]=value;}public string Get(string name,string alt=null){return this.settings.Get(name,
alt);}public bool Enabled(string name){return this.settings.Get(name)=="true";}}public bool ParseCustomData(){MyIniParseResult result;if(!ini.TryParse(Me.CustomData,out result)){Echo($"Failed to parse config:\n{result}");
return false;}if(Me.CustomData==""){Me.CustomData=customDataInit;}config.Clear();config.customData=Me.CustomData;strings.Clear();ini.GetSections(strings);template.Reset();templates.Clear();config.Set("airlock",
"true");string themeConfig="";if(ini.ContainsSection("global")){string setting="";List<MyIniKey>keys=new List<MyIniKey>();ini.GetKeys("global",keys);foreach(MyIniKey key in keys){if(ini.Get(key).TryGetString(out
setting)){string configKey=key.ToString().Replace("global/","");config.Set(configKey,setting);if(configKey=="config"){themeConfig=$"{{config:{setting}}}\n";}}}}foreach(string outname in strings){if(outname
=="global"){continue;}var tpl=ini.Get(outname,"output");if(!tpl.IsEmpty){templates[outname]=themeConfig+tpl.ToString();}}string name;string surfaceName;IMyTextSurface surface;drawables.Clear();bool hasNumberedSurface;
blocks.Clear();if(config.Enabled("getAllGrids")){GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(blocks);}else{GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(blocks,block=>block.IsSameConstructAs(Me));
}foreach(IMyTextSurfaceProvider block in blocks){for(int i=0;i<block.SurfaceCount;i++){name=((IMyTerminalBlock)block).CustomName;surfaceName=$"{name} <{i}>";hasNumberedSurface=strings.Contains(surfaceName);
if(!strings.Contains(name)&&!hasNumberedSurface){continue;}surface=block.GetSurface(i);if(hasNumberedSurface){drawables[surfaceName]=new DrawingSurface(surface,this,surfaceName);}else{drawables[name]=
new DrawingSurface(surface,this,name);}}}return true;}public bool Configure(){if(stateMachine!=null){stateMachine.Dispose();}if(!ParseCustomData()){Runtime.UpdateFrequency&=UpdateFrequency.None;Echo("Failed to parse custom data");
return false;}Runtime.UpdateFrequency=UpdateFrequency.Update100;if(config.Enabled("airlock")){Runtime.UpdateFrequency|=UpdateFrequency.Update10;}stateMachine=RunStuffOverTime();Runtime.UpdateFrequency
|=UpdateFrequency.Once;return true;}public void RefetchBlocks(){powerDetails.Clear();cargoStatus.Clear();blockHealth.Clear();productionDetails.Clear();airlock.Clear();gasStatus.Clear();GridTerminalSystem.GetBlocks(allBlocks);
foreach(IMyTerminalBlock block in allBlocks){if(!Util.BlockValid(block)){continue;}if(!config.Enabled("getAllGrids")&&!block.IsSameConstructAs(Me)){continue;}powerDetails.GetBlock(block);cargoStatus.GetBlock(block);
blockHealth.GetBlock(block);productionDetails.GetBlock(block);airlock.GetBlock(block);gasStatus.GetBlock(block);}powerDetails.GotBLocks();cargoStatus.GotBLocks();blockHealth.GotBLocks();productionDetails.GotBLocks();
airlock.GotBLocks();gasStatus.GotBLocks();}public bool RecheckFailed(){if(String.CompareOrdinal(config.customData,Me.CustomData)!=0){return!Configure();}if((tickCount++)%2==0){RefetchBlocks();}if(tickCount
%20==0){log.Clear();tickCount=0;}return false;}public void RunStateMachine(){if(stateMachine==null){return;}bool hasMoreSteps=stateMachine.MoveNext();if(hasMoreSteps){Runtime.UpdateFrequency|=UpdateFrequency.Once;
}else{stateMachine.Dispose();stateMachine=null;Runtime.UpdateFrequency&=~UpdateFrequency.Once;}}public IEnumerator<string>RunStuffOverTime(){string content;string outputName;while(templates.Count>0){outputName
=templates.Keys.Last();templates.Pop(templates.Keys.Last(),out content);Dictionary<string,bool>tokens;if(template.IsPrerendered(outputName,content)){tokens=template.templateVars[outputName];}else{log.Append($"Adding {outputName}\n");
tokens=template.PreRender(outputName,content);}foreach(var kv in tokens){config.Set(kv.Key,config.Get(kv.Key,"true"));}yield return$"templates {outputName}";if(templates.Count==0){powerDetails.Reset();
cargoStatus.Reset();blockHealth.Reset();productionDetails.Reset();gasStatus.Reset();airlock.Reset();yield return"reset";RefetchBlocks();}yield return"updated";}if(config.Enabled("power")){powerDetails.Refresh();
yield return"powerDetails";}if(config.Enabled("cargo")){cargoStatus.Refresh();yield return"cargoStatus";}if(config.Enabled("health")){blockHealth.Refresh();yield return"blockHealth";}if(config.Enabled("production"))
{productionDetails.Refresh();yield return"productionDetails";}if(config.Enabled("gas")){gasStatus.Refresh();yield return"gasStatus";}DrawingSurface.Options themeOpts=null;string screenConfig=config.Get("config");
if(screenConfig!=null){themeOpts=template.StringToOptions(screenConfig);}for(int j=0;j<drawables.Count;++j){var dw=drawables.ElementAt(j);if(themeOpts!=null){template.ConfigureScreen(dw.Value,themeOpts);
}template.Render(dw.Value);yield return$"render {dw.Key}";}config.Set("config",null);yield break;}public class GasStatus:Runnable{public Program program;public Template template;public List<IMyGasTank>
o2Tanks;public List<IMyGasTank>h2Tanks;public List<IMyGasGenerator>oxyGens;public Dictionary<string,double>tankMap;public double o2CurrentVolume;public float o2MaxVolume;public double o2FillPct;public
int o2TankCount;public double h2CurrentVolume;public float h2MaxVolume;public double h2FillPct;public int h2TankCount;public double enableFillPct;public double disableFillPct;public GasStatus(Program program,
Template template=null){this.program=program;this.template=template;this.o2Tanks=new List<IMyGasTank>();this.h2Tanks=new List<IMyGasTank>();this.oxyGens=new List<IMyGasGenerator>();this.tankMap=new Dictionary<string,
double>();this.Reset();}public void Reset(){this.Clear();if(this.program.config.Enabled("gas")){this.RegisterTemplateVars();}}public void Clear(){this.ClearTotals();this.o2Tanks.Clear();this.h2Tanks.Clear();
this.tankMap.Clear();}public void RegisterTemplateVars(){if(this.template==null){return;}this.template.Register("gas.o2CurrentVolume",()=>$"{Util.FormatNumber(this.o2CurrentVolume)}");this.template.Register("gas.h2CurrentVolume",
()=>$"{Util.FormatNumber(this.h2CurrentVolume)}");this.template.Register("gas.o2MaxVolume",()=>$"{Util.FormatNumber(this.o2MaxVolume)}");this.template.Register("gas.h2MaxVolume",()=>$"{Util.FormatNumber(this.h2MaxVolume)}");
this.template.Register("gas.o2FillPct",()=>Util.PctString(this.o2FillPct));this.template.Register("gas.h2FillPct",()=>Util.PctString(this.h2FillPct));this.template.Register("gas.o2Tanks",(DrawingSurface
ds,string text,DrawingSurface.Options options)=>{this.PrintTanks(this.o2Tanks,ds,options);});this.template.Register("gas.h2Tanks",(DrawingSurface ds,string text,DrawingSurface.Options options)=>{this.PrintTanks(this.h2Tanks,
ds,options);});this.template.Register("gas.generationEnabled",(DrawingSurface ds,string text,DrawingSurface.Options options)=>{string message=options.custom.Get("txtDisabled")??"Gas generation off";if
(this.GetGenerators()){message=options.custom.Get("txtEnabled")??"Gas generation on";}ds.Text(message,options);});this.template.Register("gas.generationEnabledExtended",(DrawingSurface ds,string text,
DrawingSurface.Options options)=>{this.GasExtendedMessage(ds,text,options);});this.template.Register("gas.o2Bar",(DrawingSurface ds,string text,DrawingSurface.Options options)=>{this.GasBar((float)o2FillPct,
ds,options);});this.template.Register("gas.h2Bar",(DrawingSurface ds,string text,DrawingSurface.Options options)=>{this.GasBar((float)h2FillPct,ds,options);});}public void GetBlock(IMyTerminalBlock block)
{if(block is IMyGasTank){string name=block.DefinitionDisplayNameText.ToString();if(name.Contains("Oxygen Tank")){this.o2Tanks.Add((IMyGasTank)block);}else if(name.Contains("Hydrogen Tank")){this.h2Tanks.Add((IMyGasTank)block);
}}else if(block is IMyGasGenerator){this.oxyGens.Add((IMyGasGenerator)block);}}public void GotBLocks(){this.enableFillPct=Util.ParseFloat(this.program.config.Get("gasEnableFillPct"),-1f);this.disableFillPct
=Util.ParseFloat(this.program.config.Get("gasDisableFillPct"),-1f);}public void Refresh(){this.ClearTotals();this.GetGasLevels();}public void ClearTotals(){this.o2CurrentVolume=0f;this.o2MaxVolume=0f;
this.o2FillPct=0f;this.o2TankCount=0;this.h2CurrentVolume=0f;this.h2MaxVolume=0f;this.h2FillPct=0f;this.h2TankCount=0;}public void PrintTanks(List<IMyGasTank>tanks,DrawingSurface ds,DrawingSurface.Options
options){if(tanks.Count==0){ds.Text("");return;}foreach(IMyGasTank tank in tanks){double currentVolume=(tank.FilledRatio*tank.Capacity);ds.Text($@"{tank.CustomName}: {Util.FormatNumber(currentVolume)} / {Util.FormatNumber((double)tank.Capacity)} L ({Util.PctString(tank.FilledRatio)})",
options).Newline();}ds.Newline(reverse:true);}public void GasBar(float pct,DrawingSurface ds,DrawingSurface.Options options){options.pct=pct;options.text=Util.PctString(pct);options.textColour=options.textColour
??ds.surface.ScriptForegroundColor;ds.Bar(options);}public void SetGenerators(bool enabled){foreach(IMyGasGenerator genny in this.oxyGens){genny.Enabled=enabled;}}public bool GetGenerators(){foreach(IMyGasGenerator
genny in this.oxyGens){if(genny.Enabled){return true;}}return false;}public void GetGasLevels(){this.tankMap.Clear();foreach(IMyGasTank tank in this.o2Tanks){this.o2CurrentVolume+=(tank.FilledRatio*tank.Capacity);
this.o2MaxVolume+=tank.Capacity;this.o2TankCount++;if(!this.tankMap.ContainsKey(tank.CustomName)){this.tankMap.Add(tank.CustomName,tank.FilledRatio);}else{this.tankMap[tank.CustomName]=this.tankMap[tank.CustomName]
+tank.FilledRatio;}}foreach(IMyGasTank tank in this.h2Tanks){this.h2CurrentVolume+=(tank.FilledRatio*tank.Capacity);this.h2MaxVolume+=tank.Capacity;this.h2TankCount++;if(!this.tankMap.ContainsKey(tank.CustomName))
{this.tankMap.Add(tank.CustomName,tank.FilledRatio);}else{this.tankMap[tank.CustomName]=this.tankMap[tank.CustomName]+tank.FilledRatio;}}this.o2FillPct=this.o2TankCount==0?0:this.o2CurrentVolume/this.o2MaxVolume;
this.h2FillPct=this.h2TankCount==0?0:this.h2CurrentVolume/this.h2MaxVolume;if(this.enableFillPct!=-1&&(this.o2FillPct<=this.enableFillPct||this.h2FillPct<=this.enableFillPct)){this.SetGenerators(true);
}if(this.disableFillPct!=-1&&this.o2FillPct>=this.disableFillPct&&this.h2FillPct>=this.disableFillPct){this.SetGenerators(false);}}public void GasExtendedMessage(DrawingSurface ds,string text,DrawingSurface.Options
options){var max=Math.Floor(100*Math.Max(this.o2FillPct,this.h2FillPct));var min=Math.Ceiling(100*Math.Min(this.o2FillPct,this.h2FillPct));string message=options.custom.Get("txtDisabled")??$"Gas generation off";
if(this.enableFillPct!=-1){message=message+$", enabled at {Math.Floor(100 * this.enableFillPct)}% (current: {min}%)";}if(this.GetGenerators()){message=options.custom.Get("txtEnabled")??$"Gas generation on";
if(this.disableFillPct!=-1){message=message+$", disabled at {Math.Ceiling(100 * this.disableFillPct)}% (current: {max}%)";}}ds.Text(message,options);}}class BlockHealth:Runnable{public Program program;
public Template template;public System.Text.RegularExpressions.Regex ignoreHealth;public List<IMyTerminalBlock>blocks;public Dictionary<string,string>damaged;public Dictionary<long,IMyTerminalBlock>shownOnHud;
public string status;public BlockHealth(Program program,Template template){this.program=program;this.template=template;this.blocks=new List<IMyTerminalBlock>();this.damaged=new Dictionary<string,string>();
this.shownOnHud=new Dictionary<long,IMyTerminalBlock>();this.Reset();}public void Reset(){this.Clear();this.shownOnHud.Clear();if(this.program.config.Enabled("health")){this.RegisterTemplateVars();string
ignore=this.program.config.Get("healthIgnore");if(ignore!=""&&ignore!=null){this.ignoreHealth=Util.Regex(System.Text.RegularExpressions.Regex.Replace(ignore,@"\s*,\s*","|"));}}}public void Clear(){this.blocks.Clear();
this.damaged.Clear();}public void RegisterTemplateVars(){if(this.template==null){return;}this.template.Register("health.status",()=>this.status);this.template.Register("health.blocks",(DrawingSurface ds,
string text,DrawingSurface.Options options)=>{if(this.damaged.Count()==0){ds.Text("");return;}foreach(KeyValuePair<string,string>block in this.damaged){ds.Text($"{block.Key} [{block.Value}]").Newline();
}ds.Newline(reverse:true);});}public float GetHealth(IMyTerminalBlock block){IMySlimBlock slimblock=block.CubeGrid.GetCubeBlock(block.Position);if(slimblock==null){return 1f;}float MaxIntegrity=slimblock.MaxIntegrity;
float BuildIntegrity=slimblock.BuildIntegrity;float CurrentDamage=slimblock.CurrentDamage;return(BuildIntegrity-CurrentDamage)/MaxIntegrity;}public void GetBlock(IMyTerminalBlock block){this.blocks.Add(block);
}public void GotBLocks(){}public void Refresh(){if(this.blocks==null){return;}this.damaged.Clear();bool showOnHud=this.program.config.Enabled("healthOnHud");foreach(var b in this.blocks){if(!Util.BlockValid(b))
{continue;}if(this.ignoreHealth!=null&&this.ignoreHealth.IsMatch(b.CustomName)){continue;}var health=this.GetHealth(b);if(health!=1f){this.damaged[b.CustomName]=Util.PctString(health);if(showOnHud&&!b.ShowOnHUD)
{this.shownOnHud[b.EntityId]=b;b.ShowOnHUD=true;}}}if(showOnHud){foreach(long key in this.shownOnHud.Keys.ToList()){IMyTerminalBlock block=this.shownOnHud[key];if(block==null||!Util.BlockValid(block))
{this.shownOnHud.Remove(key);}if(this.GetHealth(block)==1f){block.ShowOnHUD=false;this.shownOnHud.Remove(key);}}}this.status=$"{(this.damaged.Count == 0 ? "No damage" : "Damage")} detected";}}public class
PowerDetails:Runnable{public Program program;public Template template;public List<IMyTerminalBlock>powerProducerBlocks;public List<IMyTerminalBlock>jumpDriveBlocks;public List<IMyTerminalBlock>consumers;
public List<MyInventoryItem>items;public List<float>ioFloats;public List<Color>ioColours;public Dictionary<string,Color>ioLegendNames;public Dictionary<string,float>consumerDict;public float batteryCurrent;
public float batteryInput;public float batteryInputMax;public float batteryMax;public float batteryOutputDisabled;public float batteryOutputMax;public float batteryOutputMW;public float batteryPotential;
public float hEngineOutputDisabled;public float hEngineOutputMax;public float hEngineOutputMW;public float hEnginePotential;public float jumpCurrent;public float jumpMax;public float reactorOutputDisabled;
public float reactorOutputMax;public float reactorOutputMW;public float reactorPotential;public float solarOutputDisabled;public float solarOutputMax;public float solarOutputMW;public float solarPotential;
public float turbineOutputDisabled;public float turbineOutputMax;public float turbineOutputMW;public float turbinePotential;public int batteries;public int batteriesDisabled;public int hEngines;public
int hEnginesDisabled;public int jumpDrives;public int reactors;public int reactorsDisabled;public int solars;public int solarsDisabled;public int turbines;public int turbinesDisabled;public MyFixedPoint
reactorUranium;public MyDefinitionId electricity=new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties),"Electricity");public MyDefinitionId oxygen=new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties),
"Oxygen");public MyDefinitionId hydrogen=new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties),"Hydrogen");public float battChargeMax=12f;public Color reactorColour
=Color.Lighten(Color.Blue,0.05);public Color hEnginesColour=DrawingSurface.stringToColour["dimred"];public Color batteriesColour=DrawingSurface.stringToColour["dimgreen"];public Color turbinesColour=Color.Darken(DrawingSurface.stringToColour["dimyellow"],
0.1);public Color solarsColour=Color.Darken(Color.Cyan,0.8);public char[]splitNewline=new[]{'\n'};public List<float>widths=new List<float>(){0,0,0,0};public PowerDetails(Program program,Template template
=null){this.program=program;this.template=template;this.items=new List<MyInventoryItem>();this.ioFloats=new List<float>();this.ioColours=new List<Color>(){this.reactorColour,ColorExtensions.Alpha(this.reactorColour,
0.98f),this.hEnginesColour,ColorExtensions.Alpha(this.hEnginesColour,0.98f),this.batteriesColour,ColorExtensions.Alpha(this.batteriesColour,0.98f),this.turbinesColour,ColorExtensions.Alpha(this.turbinesColour,
0.98f),this.solarsColour,ColorExtensions.Alpha(this.solarsColour,0.98f)};this.powerProducerBlocks=new List<IMyTerminalBlock>();this.jumpDriveBlocks=new List<IMyTerminalBlock>();this.consumers=new List<IMyTerminalBlock>();
this.ioLegendNames=new Dictionary<string,Color>();this.consumerDict=new Dictionary<string,float>();this.Reset();}public void Reset(){this.Clear();if(this.program.config.Enabled("power")){this.RegisterTemplateVars();
}}public void RegisterTemplateVars(){if(this.template==null){return;}this.template.Register("power.batteries",()=>this.batteries.ToString());this.template.Register("power.batteryBar",this.BatteryBar);
this.template.Register("power.batteryCurrent",()=>String.Format("{0:0.##}",this.batteryCurrent));this.template.Register("power.batteryInput",()=>String.Format("{0:0.##}",this.batteryInput));this.template.Register("power.batteryInputMax",
()=>String.Format("{0:0.##}",this.batteryInputMax));this.template.Register("power.batteryMax",()=>String.Format("{0:0.##}",this.batteryMax));this.template.Register("power.batteryOutput",()=>String.Format("{0:0.##}",
this.batteryOutputMW));this.template.Register("power.batteryOutputMax",()=>String.Format("{0:0.##}",this.batteryOutputMax));this.template.Register("power.consumers",this.PowerConsumers);this.template.Register("power.engineOutputMax",
()=>String.Format("{0:0.##}",this.hEngineOutputMax));this.template.Register("power.engineOutputMW",()=>String.Format("{0:0.##}",this.hEngineOutputMW));this.template.Register("power.engines",()=>this.hEngines.ToString());
this.template.Register("power.input",()=>String.Format("{0:0.##}",this.CurrentInput()));this.template.Register("power.ioBar",this.IoBar);this.template.Register("power.ioLegend",this.IoLegend);this.template.Register("power.ioString",
this.IoString);this.template.Register("power.jumpBar",this.JumpBar);this.template.Register("power.jumpCurrent",()=>String.Format("{0:0.##}",this.jumpCurrent));this.template.Register("power.jumpDrives",
()=>this.jumpDrives.ToString());this.template.Register("power.jumpMax",()=>String.Format("{0:0.##}",this.jumpMax));this.template.Register("power.maxOutput",()=>String.Format("{0:0.##}",this.MaxOutput()));
this.template.Register("power.output",()=>String.Format("{0:0.##}",this.CurrentOutput()));this.template.Register("power.reactorOutputMax",()=>String.Format("{0:0.##}",this.reactorOutputMax));this.template.Register("power.reactorOutputMW",
()=>String.Format("{0:0.##}",this.reactorOutputMW));this.template.Register("power.reactors",()=>this.reactors.ToString());this.template.Register("power.reactorString",this.ReactorString);this.template.Register("power.reactorUr",
()=>Util.FormatNumber(this.reactorUranium));this.template.Register("power.solarOutputMax",()=>String.Format("{0:0.##}",this.solarOutputMax));this.template.Register("power.solarOutputMW",()=>String.Format("{0:0.##}",
this.solarOutputMW));this.template.Register("power.solars",()=>this.solars.ToString());this.template.Register("power.turbineOutputMax",()=>String.Format("{0:0.##}",this.turbineOutputMax));this.template.Register("power.turbineOutputMW",
()=>String.Format("{0:0.##}",this.turbineOutputMW));this.template.Register("power.turbines",()=>this.turbines.ToString());}public void ClearTotals(){this.batteries=0;this.batteriesDisabled=0;this.batteryCurrent
=0f;this.batteryInput=0f;this.batteryInputMax=0f;this.batteryMax=0f;this.batteryOutputDisabled=0f;this.batteryOutputMax=0f;this.batteryOutputMW=0f;this.batteryPotential=0f;this.hEngineOutputDisabled=0f;
this.hEngineOutputMax=0f;this.hEngineOutputMW=0f;this.hEnginePotential=0f;this.hEngines=0;this.hEnginesDisabled=0;this.jumpCurrent=0f;this.jumpDrives=0;this.jumpMax=0f;this.reactorOutputDisabled=0f;this.reactorOutputMax
=0f;this.reactorOutputMW=0f;this.reactorPotential=0f;this.reactors=0;this.reactorsDisabled=0;this.reactorUranium=0;this.solarOutputDisabled=0f;this.solarOutputMax=0f;this.solarOutputMW=0f;this.solarPotential
=0f;this.solars=0;this.solarsDisabled=0;this.turbineOutputDisabled=0f;this.turbineOutputMax=0f;this.turbineOutputMW=0f;this.turbinePotential=0f;this.turbines=0;this.turbinesDisabled=0;}public void Clear()
{this.ClearTotals();this.powerProducerBlocks.Clear();this.jumpDriveBlocks.Clear();this.consumers.Clear();}public void GetBlock(IMyTerminalBlock block){this.consumers.Add(block);if(block is IMyPowerProducer)
{this.powerProducerBlocks.Add(block);}else if(block is IMyJumpDrive){this.jumpDriveBlocks.Add(block);}}public void GotBLocks(){}public float GetPercent(float current,float max){if(max==0){return 0f;}return
current/max;}public float MaxOutput(){return this.hEngineOutputMax+this.reactorOutputMax+this.solarOutputMax+this.turbineOutputMax+(battChargeMax*this.batteries);}public float MaxInput(){return(battChargeMax
*this.batteries);}public float CurrentInput(){return this.batteryInput;}public float CurrentOutput(){return this.reactorOutputMW+this.solarOutputMW+this.turbineOutputMW+this.hEngineOutputMW+this.batteryOutputMW;
}public float DisabledMaxOutput(){return this.batteryOutputDisabled+this.reactorOutputDisabled+this.solarOutputDisabled+this.turbineOutputDisabled+this.hEngineOutputDisabled;}public void Refresh(){this.ClearTotals();
foreach(IMyJumpDrive jumpDrive in this.jumpDriveBlocks){if(!Util.BlockValid(jumpDrive)){continue;}this.jumpDrives+=1;this.jumpCurrent+=jumpDrive.CurrentStoredPower;this.jumpMax+=jumpDrive.MaxStoredPower;
}foreach(IMyPowerProducer powerBlock in this.powerProducerBlocks){if(!Util.BlockValid(powerBlock)){continue;}string typeString=powerBlock.BlockDefinition.TypeIdString;IMyBatteryBlock battery=powerBlock
as IMyBatteryBlock;if(battery!=null){this.batteries++;this.batteryCurrent+=battery.CurrentStoredPower;this.batteryMax+=battery.MaxStoredPower;this.batteryInput+=battery.CurrentInput;this.batteryOutputMW
+=battery.CurrentOutput;this.batteryOutputMax+=battery.MaxOutput;this.batteryOutputMax+=battery.MaxInput;if(!battery.Enabled||battery.ChargeMode==ChargeMode.Recharge){this.batteriesDisabled++;this.batteryOutputDisabled
+=battChargeMax;}}else if(powerBlock is IMyReactor){this.reactors++;this.reactorOutputMW+=powerBlock.CurrentOutput;this.reactorOutputMax+=powerBlock.MaxOutput;if(!powerBlock.Enabled){this.reactorsDisabled++;
this.reactorOutputDisabled+=powerBlock.MaxOutput;}this.items.Clear();var inv=powerBlock.GetInventory(0);inv.GetItems(this.items);for(var i=0;i<items.Count;i++){this.reactorUranium+=items[i].Amount;}}else
if(powerBlock is IMySolarPanel){this.solars++;this.solarOutputMW+=powerBlock.CurrentOutput;this.solarOutputMax+=powerBlock.MaxOutput;if(!powerBlock.Enabled){this.solarsDisabled++;this.solarOutputDisabled
+=powerBlock.MaxOutput;}}else if(typeString=="MyObjectBuilder_HydrogenEngine"){this.hEngines++;this.hEngineOutputMW+=powerBlock.CurrentOutput;this.hEngineOutputMax+=powerBlock.MaxOutput;if(!powerBlock.Enabled)
{this.hEnginesDisabled++;this.hEngineOutputDisabled+=powerBlock.MaxOutput;}}else if(typeString=="MyObjectBuilder_WindTurbine"){this.turbines++;this.turbineOutputMW+=powerBlock.CurrentOutput;this.turbineOutputMax
+=powerBlock.MaxOutput;if(!powerBlock.Enabled){this.turbinesDisabled++;this.turbineOutputDisabled+=powerBlock.MaxOutput;}}}this.consumerDict.Clear();MyResourceSinkComponent resourceSink;foreach(IMyTerminalBlock
block in this.consumers){if(!block.Components.TryGet<MyResourceSinkComponent>(out resourceSink)){continue;}float powerConsumption=resourceSink.CurrentInputByType(this.electricity);string blockName=block.DefinitionDisplayNameText.ToString();
if(!this.consumerDict.ContainsKey(blockName)){this.consumerDict.Add(blockName,powerConsumption);}else{this.consumerDict[blockName]=this.consumerDict[blockName]+powerConsumption;}}this.consumerDict=this.consumerDict.OrderBy(x
=>-x.Value).ToDictionary(x=>x.Key,x=>x.Value);this.reactorPotential=(this.reactors-this.reactorsDisabled)==0?0f:(this.reactorOutputMW/(float)(this.reactors-this.reactorsDisabled))*(float)this.reactorsDisabled;
this.hEnginePotential=(this.hEngines-this.hEnginesDisabled)==0?0f:(this.hEngineOutputMW/(float)(this.hEngines-this.hEnginesDisabled))*(float)this.hEnginesDisabled;this.batteryPotential=(this.batteries
-this.batteriesDisabled)==0?0f:(this.batteryOutputMW/(float)(this.batteries-this.batteriesDisabled))*(float)this.batteriesDisabled;this.turbinePotential=(this.turbines-this.turbinesDisabled)==0?0f:(this.turbineOutputMW
/(float)(this.turbines-this.turbinesDisabled))*(float)this.turbinesDisabled;this.solarPotential=(this.solars-this.solarsDisabled)==0?0f:(this.solarOutputMW/(float)(this.solars-this.solarsDisabled))*(float)this.solarsDisabled;
}public void BatteryBar(DrawingSurface ds,string text,DrawingSurface.Options options){if(this.batteries==0){return;}float io=this.batteryInput-this.batteryOutputMW;options.net=this.batteryCurrent;float
remainingMins=io==0f?0:(this.batteryMax-this.batteryCurrent)*60/Math.Abs(io);string pct=Util.PctString(this.batteryCurrent/this.batteryMax);if(this.batteryCurrent/this.batteryMax>=0.9999f){pct="100 %";
io=0;remainingMins=0;}string msg=$"{pct}";if(io<0){options.net=(this.batteryCurrent-this.batteryMax);remainingMins=this.batteryCurrent*60/Math.Abs(io);msg=$"{pct}";}double minsLeft=Math.Round(remainingMins);
options.text=text??$"{msg} ({(minsLeft <= 60 ? $"{minsLeft}min" : String.Format("{0:0.00}hours", minsLeft / 60))})";options.high=this.batteryMax;options.low=options.high;ds.MidBar(options);}public void
IoBar(DrawingSurface ds,string text,DrawingSurface.Options options){float max=this.CurrentOutput()+this.reactorPotential+this.hEnginePotential+this.batteryPotential+this.turbinePotential+this.solarPotential;
this.ioFloats.Clear();this.ioFloats.Add(this.reactorOutputMW/max);this.ioFloats.Add(this.reactorPotential/max);this.ioFloats.Add(this.hEngineOutputMW/max);this.ioFloats.Add(this.hEnginePotential/max);
this.ioFloats.Add(this.batteryOutputMW/max);this.ioFloats.Add(this.batteryPotential/max);this.ioFloats.Add(this.turbineOutputMW/max);this.ioFloats.Add(this.turbinePotential/max);this.ioFloats.Add(this.solarOutputMW
/max);this.ioFloats.Add(this.solarPotential/max);options.values=this.ioFloats;options.colours=this.ioColours;options.text=text??options.text;options.align=options.align??TextAlignment.LEFT;ds.MultiBar(options);
}public void IoLegend(DrawingSurface ds,string text,DrawingSurface.Options options){this.ioLegendNames.Clear();if(this.reactors>0){this.ioLegendNames["Reactor"]=this.reactorColour;}if(this.hEngines>0)
{this.ioLegendNames["H2 Engine"]=this.hEnginesColour;}if(this.batteries>0){this.ioLegendNames["Battery"]=this.batteriesColour;}if(this.turbines>0){this.ioLegendNames["Wind"]=this.turbinesColour;}if(this.solars
>0){this.ioLegendNames["Solar"]=this.solarsColour;}ds.sb.Clear();ds.sb.Append(string.Join(" / ",this.ioLegendNames.Keys));Vector2 size=ds.surface.MeasureStringInPixels(ds.sb,ds.surface.Font,ds.surface.FontSize);
ds.SetCursor((ds.width-size.X)/2,null);bool first=true;foreach(var kv in this.ioLegendNames){if(!first){ds.Text(" / ");}ds.Text(kv.Key,colour:kv.Value);first=false;}}public void JumpBar(DrawingSurface
ds,string text,DrawingSurface.Options options){if(this.jumpDrives==0){ds.Text("");return;}options.pct=this.GetPercent(this.jumpCurrent,this.jumpMax);options.text=text??Util.PctString(options.pct);ds.Bar(options);
}public string IoString(){float io=this.batteries>0?this.batteryInput-this.batteryOutputMW:this.CurrentInput()-this.CurrentOutput();float max=io>0?this.MaxInput():this.MaxOutput();return String.Format("{0:0.00} MW ({1})",
io,Util.PctString(max==0f?0f:Math.Abs(io)/max));}public void ReactorString(DrawingSurface ds,string text,DrawingSurface.Options options){if(this.reactors==0){ds.Text("");return;}string msg=text??options.text
??"Reactors: ";ds.Text($"{msg}{this.reactors}, Output: {this.reactorOutputMW} MW, Ur: {this.reactorUranium}",options);}public void PowerConsumers(DrawingSurface ds,string text,DrawingSurface.Options options)
{int max=Util.ParseInt(options.custom.Get("count")??"10");if(this.consumerDict.Count()==0){ds.Text("");return;}if(ds.width/(ds.charSizeInPx.X+1f)<40){foreach(var item in this.consumerDict){string kw=(item.Value
*1000).ToString("#,,# kW");ds.Text($"{item.Key}").SetCursor(ds.width,null).Text(kw,textAlignment:TextAlignment.RIGHT);if(--max==0){return;}ds.Newline();}}else{this.widths[0]=0;this.widths[1]=ds.width/
2-1.5f*ds.charSizeInPx.X;this.widths[2]=ds.width/2+1.5f*ds.charSizeInPx.X;this.widths[3]=ds.width;int i=0;foreach(var item in this.consumerDict){string kw=(item.Value*1000).ToString("#,,# kW");ds.SetCursor(this.widths[(i++
%4)],null).Text($"{item.Key}").SetCursor(this.widths[(i++%4)],null).Text(kw,textAlignment:TextAlignment.RIGHT);if(--max==0){return;}if((i%4)==0||i>=this.consumerDict.Count*2){ds.Newline();}}}ds.Newline(reverse:
true);}}public struct ProductionBlock{public double idleTime;public IMyProductionBlock block;public ProductionBlock(IMyProductionBlock block){this.idleTime=-1;this.block=block;}}public class ProductionDetails
:Runnable{public Program program;public Template template;public List<MyProductionItem>productionItems;public Dictionary<long,ProductionBlock>productionBlocks;public List<IMyProductionBlock>blocks;public
Dictionary<long,string>blockStatus;public Dictionary<string,string>statusDotColour;public Dictionary<string,VRage.MyFixedPoint>queueItems;public double productionCheckFreqMs=2*60*1000;public double productionOnWaitMs
=5*1000;public double productionOutTimeMs=3*1000;public string productionIgnoreString="[x]";public string status;public StringBuilder queueBuilder;public double allIdleSince=0;public double timeDisabled
=0;public bool checking=false;public double lastCheck=DateTime.Now.Ticks/TimeSpan.TicksPerMillisecond;public char[]splitNewline;public ProductionDetails(Program program,Template template){this.program
=program;this.template=template;this.blocks=new List<IMyProductionBlock>();this.productionItems=new List<MyProductionItem>();this.productionBlocks=new Dictionary<long,ProductionBlock>();this.blockStatus
=new Dictionary<long,string>();this.queueItems=new Dictionary<string,VRage.MyFixedPoint>();this.statusDotColour=new Dictionary<string,string>(){{"Broken","dimred"},{"Idle","dimgray"},{"Working","dimgreen"},
{"Blocked","dimyellow"}};this.queueBuilder=new StringBuilder();this.splitNewline=new[]{'\n'};this.Reset();}public void Reset(){this.allIdleSince=0;this.timeDisabled=0;this.checking=false;this.Clear();
this.productionBlocks.Clear();if(this.program.config.Enabled("power")){this.RegisterTemplateVars();}}public void Clear(){this.blocks.Clear();this.productionItems.Clear();this.blockStatus.Clear();this.queueItems.Clear();
}public void RegisterTemplateVars(){if(this.template==null){return;}this.template.Register("production.status",()=>this.status);this.template.Register("production.blocks",(DrawingSurface ds,string text,
DrawingSurface.Options options)=>{bool first=true;if(this.blockStatus.Count()==0){ds.Text("");return;}foreach(long blockId in this.blockStatus.Keys.OrderBy(id=>this.productionBlocks.Get(id).block.CustomName).ToList())
{ProductionBlock productionBlock=this.productionBlocks.Get(blockId);if(!this.productionBlocks.ContainsKey(blockId)){continue;}if(!first){ds.Newline();}first=false;string status=this.Status(productionBlock.block);
string blockName=$" {productionBlock.block.CustomName}: {status} {(this.IsIdle(ref productionBlock) ? this.IdleTime(ref productionBlock) : "")}";Color?colour=DrawingSurface.StringToColour(this.statusDotColour.Get(status));
ds.TextCircle(colour,outline:false).Text(blockName);foreach(string str in this.blockStatus[blockId].Split(this.splitNewline,StringSplitOptions.RemoveEmptyEntries)){ds.Newline().Text(str);}}});}public void
GetBlock(IMyTerminalBlock block){if(!this.productionBlocks.ContainsKey(block.EntityId)&&(block is IMyAssembler||block is IMyRefinery)&&!block.CustomName.Contains(this.productionIgnoreString)){this.productionBlocks.Add(block.EntityId,
new ProductionBlock(block as IMyProductionBlock));}}public void GotBLocks(){foreach(long key in this.productionBlocks.Keys.ToList()){ProductionBlock productionBlock=this.productionBlocks.Get(key);if(!Util.BlockValid(productionBlock.block))
{this.productionBlocks.Remove(key);}}}public void Refresh(){this.blockStatus.Clear();this.status="";if(this.productionBlocks.Count==0){return;}int workingAssemblers=0;bool allIdle=true;string itemName;
foreach(long blockId in this.productionBlocks.Keys.ToList()){ProductionBlock productionBlock=this.productionBlocks.Get(blockId);if(!Util.BlockValid(productionBlock.block)){this.productionBlocks.Remove(blockId);
continue;}this.UpdateStatus(ref productionBlock);bool idle=this.IsIdle(ref productionBlock);if(productionBlock.block.DefinitionDisplayNameText.ToString()!="Survival Kit"){allIdle=allIdle&&idle;}if(!idle
&&productionBlock.block is IMyAssembler){workingAssemblers++;}this.queueItems.Clear();if(!idle){this.GetQueue(productionBlock.block,this.productionItems);foreach(MyProductionItem item in this.productionItems)
{itemName=Util.ToItemName(item);if(!this.queueItems.ContainsKey(itemName)){this.queueItems.Add(itemName,item.Amount);}else{this.queueItems[itemName]=this.queueItems[itemName]+item.Amount;}}}this.queueBuilder.Clear();
foreach(var kv in this.queueItems){this.queueBuilder.Append($"  {Util.FormatNumber(kv.Value)} x {kv.Key}\n");}this.blockStatus.Add(productionBlock.block.EntityId,this.queueBuilder.ToString());this.productionBlocks[blockId]
=productionBlock;}double timeNow=DateTime.Now.Ticks/TimeSpan.TicksPerMillisecond;if(allIdle){if(this.allIdleSince==0){this.allIdleSince=timeNow;}double timeIdle=timeNow-this.allIdleSince;string timeIdleMsg
=Util.TimeFormat(timeIdle);if(this.timeDisabled==0){foreach(ProductionBlock productionBlock in this.productionBlocks.Values){this.Enable(productionBlock.block,false);}this.timeDisabled=timeNow;}else{if
(!this.checking){if(timeNow-this.lastCheck>this.productionCheckFreqMs){foreach(var productionBlock in this.productionBlocks.Values){this.Enable(productionBlock.block,true);}this.checking=true;this.lastCheck
=timeNow;this.status=$"Power saving mode {timeIdleMsg} (checking)";}}else{if(timeNow-this.lastCheck>this.productionOnWaitMs){foreach(var productionBlock in this.productionBlocks.Values){this.Enable(productionBlock.block,
false);}this.checking=false;this.lastCheck=timeNow;}else{this.status=$"Power saving mode {timeIdleMsg} (checking)";}}}if(this.status==""){this.status=$"Power saving mode {timeIdleMsg} (check in {Util.TimeFormat(this.productionCheckFreqMs - timeIdle, true)})";
}}else{if(this.productionBlocks.Values.ToList().Where(b=>this.Status(b.block)=="Blocked").Any()){this.status="Production Enabled (Halted)";}else{this.status="Production Enabled";}if(workingAssemblers>
0){foreach(ProductionBlock productionBlock in this.productionBlocks.Values.ToList().Where(b=>b.block is IMyAssembler).ToList()){this.Enable(productionBlock.block,true);}}this.allIdleSince=0;this.timeDisabled
=0;this.checking=false;}}public void GetQueue(IMyProductionBlock productionBlock,List<MyProductionItem>productionItems){productionItems.Clear();productionBlock.GetQueue(productionItems);}public bool IsIdle(ref
ProductionBlock productionBlock){string status=this.Status(productionBlock.block);return(status=="Idle"||status=="Broken");}public bool UpdateStatus(ref ProductionBlock productionBlock){string status=
this.Status(productionBlock.block);if(status=="Idle"||status=="Broken"){if(productionBlock.idleTime==-1){productionBlock.idleTime=this.Now();}return true;}if(status=="Blocked"&&!productionBlock.block.Enabled)
{this.Enable(productionBlock.block,true);}return false;}public string IdleTime(ref ProductionBlock productionBlock){return Util.TimeFormat(this.Now()-productionBlock.idleTime);}public string Status(IMyProductionBlock
block){if(!block.IsFunctional){return"Broken";}if(block.IsProducing){return"Working";}if(block.IsQueueEmpty){return"Idle";}else{return"Blocked";}}public double Now(){return DateTime.Now.Ticks/TimeSpan.TicksPerMillisecond;
}public void Enable(IMyProductionBlock block,bool enabled){if(block.DefinitionDisplayNameText.ToString()!="Survival Kit"){block.Enabled=(bool)enabled;}}}public class DrawingSurface{public class Options
{public bool outline=false;public Color?bgColour=null;public Color?colour=null;public Color?fillColour=null;public Color?textColour=null;public float height=0f;public float high=1f;public float low=1f;
public float net=0f;public float pad=0.1f;public float pct=0f;public float scale=1f;public float size=0f;public float width=0f;public float?textPadding=null;public List<Color>colours=new List<Color>();
public List<float>values=new List<float>();public string text=null;public TextAlignment?align=null;public Dictionary<string,string>custom;public Options(){this.custom=new Dictionary<string,string>();}
}public Program program;public IMyTextSurface surface;public RectangleF viewport;public MySpriteDrawFrame frame;public Vector2 cursor;public Vector2 savedCursor;public StringBuilder sb;public Vector2 charSizeInPx;
public bool drawing;public Vector2 padding;public float width;public float height;public string name;public int ySpace;public Color colourGrey=new Color(40,40,40);public bool mpSpriteSync=false;public
StringBuilder sizeBuilder;public static char[]underscoreSep={'_'};public static char[]commaSep={','};public static Dictionary<string,Color>stringToColour=new Dictionary<string,Color>(){{"black",Color.Black},
{"blue",Color.Blue},{"brown",Color.Brown},{"cyan",Color.Cyan},{"dimgray",Color.DimGray},{"dimgrey",Color.DimGray},{"gray",Color.Gray},{"green",Color.Green},{"orange",Color.Orange},{"pink",Color.Pink},
{"purple",Color.Purple},{"red",Color.Red},{"tan",Color.Tan},{"transparent",Color.Transparent},{"white",Color.White},{"yellow",Color.Yellow},{"dimgreen",Color.Darken(Color.Green,0.3)},{"dimyellow",Color.Darken(Color.Yellow,
0.6)},{"dimorange",Color.Darken(Color.Orange,0.2)},{"dimred",Color.Darken(Color.Red,0.2)}};public static Dictionary<string,TextAlignment>stringToAlignment=new Dictionary<string,TextAlignment>(){{"center",
TextAlignment.CENTER},{"left",TextAlignment.LEFT},{"right",TextAlignment.RIGHT}};public DrawingSurface(IMyTextSurface surface=null,Program program=null,string name="",int ySpace=2){this.program=program;
this.surface=surface;this.cursor=new Vector2(0f,0f);this.savedCursor=new Vector2(0f,0f);this.sb=new StringBuilder();this.sizeBuilder=new StringBuilder("O");this.charSizeInPx=new Vector2(0f,0f);this.surface.ContentType
=ContentType.SCRIPT;this.drawing=false;this.viewport=new RectangleF(0f,0f,0f,0f);this.name=name;this.ySpace=ySpace;}public void InitScreen(){if(this.surface==null){return;}this.cursor.X=0f;this.cursor.Y
=0f;this.surface.Script="";this.padding=(this.surface.TextPadding/100)*this.surface.SurfaceSize;this.viewport.Position=(this.surface.TextureSize-this.surface.SurfaceSize)/2f+this.padding;this.viewport.Size
=this.surface.SurfaceSize-(2*this.padding);this.width=this.viewport.Width;this.height=this.viewport.Height;this.Size();}public static Color?StringToColour(string colour){if(colour==""||colour==null){return
null;}if(!colour.Contains(",")){return DrawingSurface.stringToColour.Get(colour);}string[]numbersStr=colour.Split(DrawingSurface.commaSep);if(numbersStr.Length<2){return null;}int r,g,b;int a=255;if(!int.TryParse(numbersStr[0],
out r)||!int.TryParse(numbersStr[1],out g)||!int.TryParse(numbersStr[2],out b)||(numbersStr.Length>3&&!int.TryParse(numbersStr[3],out a))){return null;}else{return new Color(r,g,b,a);}}public void DrawStart()
{this.InitScreen();this.drawing=true;this.frame=this.surface.DrawFrame();this.mpSpriteSync=!this.mpSpriteSync;if(this.mpSpriteSync){this.frame.Add(new MySprite(){Type=SpriteType.TEXTURE,Data="SquareSimple",
Color=surface.BackgroundColor,Position=new Vector2(0,0),Size=new Vector2(0,0)});}}public DrawingSurface Draw(){this.drawing=false;this.frame.Dispose();return this;}public DrawingSurface SaveCursor(){if
(!this.drawing){this.DrawStart();}this.savedCursor=this.cursor;return this;}public DrawingSurface LoadCursor(){if(!this.drawing){this.DrawStart();}this.cursor=this.savedCursor;return this;}public DrawingSurface
SetCursor(float?x,float?y){if(!this.drawing){this.DrawStart();}this.cursor.X=x??this.cursor.X;this.cursor.Y=y??this.cursor.Y;return this;}public DrawingSurface Newline(bool reverse=false){float height
=(this.charSizeInPx.Y+this.ySpace)*(reverse?-1:1);this.cursor.Y+=height;this.cursor.X=this.savedCursor.X;return this;}public DrawingSurface Size(float?size=null){this.surface.FontSize=size??this.surface.FontSize;
this.charSizeInPx=this.surface.MeasureStringInPixels(this.sizeBuilder,this.surface.Font,this.surface.FontSize);return this;}public static MySprite TextSprite(Options options){Color?colour=options.colour;
TextAlignment textAlignment=options.align??TextAlignment.LEFT;float scale=options.scale;string text=options.text;return new MySprite(){Type=SpriteType.TEXT,Data=text,Color=colour,Alignment=textAlignment,
RotationOrScale=scale};}public void AddTextSprite(MySprite sprite){if(!this.drawing){this.DrawStart();}sprite.Position=this.cursor+this.viewport.Position;sprite.RotationOrScale=this.surface.FontSize*sprite.RotationOrScale;
sprite.FontId=this.surface.Font;sprite.Color=sprite.Color??this.surface.ScriptForegroundColor;this.frame.Add(sprite);this.AddTextSizeToCursor(sprite.Data,sprite.Alignment);}public DrawingSurface Text(string
text,Options options){if(options==null){return this.Text(text);}TextAlignment textAlignment=options.align??TextAlignment.LEFT;return this.Text(text,colour:options.colour,textAlignment:textAlignment,scale:
options.scale);}public DrawingSurface Text(string text,Color?colour=null,TextAlignment textAlignment=TextAlignment.LEFT,float scale=1f,Vector2?position=null){if(text==""||text==null){return this;}if(!this.drawing)
{this.DrawStart();}colour=colour??this.surface.ScriptForegroundColor;Vector2 pos=(position??this.cursor)+this.viewport.Position;this.frame.Add(new MySprite(){Type=SpriteType.TEXT,Data=text,Position=pos,
RotationOrScale=this.surface.FontSize*scale,Color=colour,Alignment=textAlignment,FontId=surface.Font});this.AddTextSizeToCursor(text,textAlignment);return this;}public void AddTextSizeToCursor(string text,
TextAlignment alignment){if(alignment==TextAlignment.RIGHT){return;}this.sb.Clear();this.sb.Append(text);Vector2 size=this.surface.MeasureStringInPixels(this.sb,this.surface.Font,this.surface.FontSize);
this.cursor.X+=alignment==TextAlignment.CENTER?size.X/2:size.X;}public float ToRad(float deg){return deg*((float)Math.PI/180f);}public Color FloatPctToColor(float pct){if(pct>0.75f){return DrawingSurface.stringToColour.Get("dimgreen");
}else if(pct>0.5f){return DrawingSurface.stringToColour.Get("dimyellow");}else if(pct>0.25f){return DrawingSurface.stringToColour.Get("dimorange");}return DrawingSurface.stringToColour.Get("dimred");}
public DrawingSurface MidBar(Options options){return this.MidBar(net:options.net,low:options.low,high:options.high,width:options.width,height:options.height,pad:options.pad,bgColour:options.bgColour,text:
options.text,textColour:options.textColour);}public DrawingSurface MidBar(float net,float low,float high,float width=0f,float height=0f,float pad=0.1f,Color?bgColour=null,string text=null,Color?textColour
=null){if(!this.drawing){this.DrawStart();}low=(float)Math.Abs(low);high=(float)Math.Abs(high);width=(width==0f)?this.width-this.cursor.X:width;height=(height==0f)?this.charSizeInPx.Y:height;height-=1f;
Vector2 pos=this.cursor+this.viewport.Position;pos.Y+=(height/2)+1f;using(frame.Clip((int)pos.X,(int)(pos.Y-height/2),(int)width,(int)height)){this.frame.Add(new MySprite{Type=SpriteType.TEXTURE,Alignment
=TextAlignment.CENTER,Data="SquareSimple",Position=pos+new Vector2(width/2,0),Size=new Vector2((float)Math.Sqrt(Math.Pow(width,2)/2),width),Color=bgColour??this.colourGrey,RotationOrScale=this.ToRad(-45f),
});}pad=(float)Math.Round(pad*height);pos.X+=pad;width-=2*pad;height-=2*pad;Color colour=DrawingSurface.stringToColour.Get("dimgreen");float pct=(high==0f?1f:(float)Math.Min(1f,net/high));if(net<0){pct
=(low==0f?-1f:(float)Math.Max(-1f,net/low));colour=DrawingSurface.stringToColour.Get("dimred");}float sideWidth=(float)Math.Abs(Math.Sqrt(2)*pct*width);float leftClip=Math.Min((width/2),(width/2)*(1+pct));
float rightClip=Math.Max((width/2),(width/2)*(1+pct));using(this.frame.Clip((int)(pos.X+leftClip),(int)(pos.Y-height/2),(int)Math.Abs(rightClip-leftClip),(int)height)){this.frame.Add(new MySprite{Type
=SpriteType.TEXTURE,Alignment=TextAlignment.CENTER,Data="SquareSimple",Position=pos+new Vector2(width/2,0),Size=new Vector2(sideWidth/2,width),Color=colour,RotationOrScale=this.ToRad(-45f)});}this.frame.Add(new
MySprite{Type=SpriteType.TEXTURE,Alignment=TextAlignment.CENTER,Data="SquareSimple",Position=pos+new Vector2((width/2),-1f),Size=new Vector2(2f,height+2f),Color=Color.White,RotationOrScale=0f});text=text
??Util.PctString(pct);if(text!=null&&text!=""){this.cursor.X+=net>0?(width/4):(3*width/4);this.Text(text,textColour??this.surface.ScriptForegroundColor,textAlignment:TextAlignment.CENTER,scale:0.8f);}
else{this.cursor.X+=width;}return this;}public DrawingSurface Bar(Options options){if(options==null||options.pct==0f){options.text=options.text??"--/--";}return this.Bar(options.pct,width:options.width,
height:options.height,fillColour:options.fillColour,textAlignment:options.align??TextAlignment.CENTER,text:options.text,textColour:options.textColour,bgColour:options.bgColour,pad:options.pad);}public
DrawingSurface Bar(float pct,float width=0f,float height=0f,Color?fillColour=null,Vector2?position=null,string text=null,Color?textColour=null,Color?bgColour=null,TextAlignment textAlignment=TextAlignment.CENTER,
float pad=0.1f){if(!this.drawing){this.DrawStart();}width=(width==0f)?this.width-this.cursor.X:width;height=(height==0f)?this.charSizeInPx.Y:height;height-=1f;Color fill=fillColour??this.FloatPctToColor(pct);
Vector2 pos=(position??this.cursor)+this.viewport.Position;pos.Y+=(height/2)+1f;using(frame.Clip((int)pos.X,(int)(pos.Y-height/2),(int)width,(int)height)){this.frame.Add(new MySprite{Type=SpriteType.TEXTURE,
Alignment=TextAlignment.CENTER,Data="SquareSimple",Position=pos+new Vector2(width/2,0),Size=new Vector2((float)Math.Sqrt(Math.Pow(width,2)/2),width),Color=bgColour??this.colourGrey,RotationOrScale=this.ToRad(-45f),
});}pad=(float)Math.Round(pad*height);pos.X+=pad;width-=2*pad;height-=2*pad;using(frame.Clip((int)pos.X,(int)(pos.Y-height/2),(int)(width*pct),(int)height)){this.frame.Add(new MySprite{Type=SpriteType.TEXTURE,
Alignment=TextAlignment.CENTER,Data="SquareSimple",Position=pos+new Vector2((width*pct)/2,0),Size=new Vector2((float)Math.Floor(Math.Sqrt(Math.Pow((width*pct),2)/2)),width),Color=fill,RotationOrScale=
this.ToRad(-45f),});}text=text??Util.PctString(pct);if(text!=null&&text!=""){this.cursor.X+=(width/2);this.Text(text,textColour??this.surface.ScriptForegroundColor,textAlignment:textAlignment,scale:0.875f);
this.cursor.X+=(width/2);}else{this.cursor.X+=width;}return this;}public DrawingSurface MultiBar(Options options){return this.MultiBar(options.values,options.colours,width:options.width,height:options.height,
text:options.text,textColour:options.textColour,bgColour:options.bgColour,textAlignment:options.align??TextAlignment.CENTER,pad:options.pad);}public DrawingSurface MultiBar(List<float>values,List<Color>
colours,float width=0f,float height=0f,Vector2?position=null,string text=null,Color?textColour=null,Color?bgColour=null,TextAlignment textAlignment=TextAlignment.CENTER,float pad=0.1f){if(!this.drawing)
{this.DrawStart();}width=(width==0f)?this.width-this.cursor.X:width;height=(height==0f)?this.charSizeInPx.Y:height;height-=1f;Vector2 pos=(position??this.cursor)+this.viewport.Position;pos.Y+=(height/
2)+1f;using(frame.Clip((int)pos.X,(int)(pos.Y-height/2),(int)width,(int)height)){this.frame.Add(new MySprite{Type=SpriteType.TEXTURE,Alignment=TextAlignment.CENTER,Data="SquareSimple",Position=pos+new
Vector2(width/2,0),Size=new Vector2((float)Math.Sqrt(Math.Pow(width,2)/2),width),Color=bgColour??this.colourGrey,RotationOrScale=this.ToRad(-45f),});}pad=(float)Math.Round(pad*height);pos.X+=pad;width
-=2*pad;height-=2*pad;int i=0;float sum=0f;int length=values.Count;for(i=0;i<values.Count;++i){sum+=(float)Math.Abs(values[i]);values[i]=sum;}for(i=values.Count-1;i>=0;--i){float pct=(float)Math.Min(values[i],
1f);if(pct==0f){continue;}Color colour=(colours.Count<=i)?DrawingSurface.stringToColour.ElementAt(i%DrawingSurface.stringToColour.Count).Value:colours[i];using(frame.Clip((int)pos.X,(int)(pos.Y-height
/2),(int)(width*pct),(int)height)){this.frame.Add(new MySprite{Type=SpriteType.TEXTURE,Alignment=TextAlignment.CENTER,Data="SquareSimple",Position=pos+new Vector2((width*pct)/2,0),Size=new Vector2((float)Math.Floor(Math.Sqrt(Math.Pow((width
*pct),2)/2)),width),Color=Color.White,RotationOrScale=this.ToRad(-45f),});this.frame.Add(new MySprite{Type=SpriteType.TEXTURE,Alignment=TextAlignment.CENTER,Data="SquareSimple",Position=pos+new Vector2((width
*pct)/2,0),Size=new Vector2((float)Math.Floor(Math.Sqrt(Math.Pow((width*pct),2)/2)),width),Color=colour,RotationOrScale=this.ToRad(-45f),});}}if(text!=null&&text!=""){this.cursor.X+=(width/2);this.Text(text,
textColour??this.surface.ScriptForegroundColor,textAlignment:textAlignment,scale:0.8f);}else{this.cursor.X+=width;}return this;}public DrawingSurface TextCircle(Options options){return(options==null)?
this.TextCircle():this.TextCircle(options.colour,outline:options.outline);}public DrawingSurface TextCircle(Color?colour=null,bool outline=false){this.Circle(this.charSizeInPx.X-2f,colour,position:this.cursor
+Vector2.Divide(this.charSizeInPx,2f),outline:outline);this.cursor.X+=2f;return this;}public DrawingSurface Circle(Options options){if(options==null){return this.Circle(this.charSizeInPx.Y,null);}float
size=(options.size<=0f)?this.charSizeInPx.Y:options.size;return this.Circle(size:size,colour:options.colour,outline:options.outline);}public DrawingSurface Circle(float size,Color?colour,Vector2?position
=null,bool outline=false){if(!this.drawing){this.DrawStart();}Vector2 pos=(position??this.cursor)+this.viewport.Position;this.frame.Add(new MySprite(){Type=SpriteType.TEXTURE,Alignment=TextAlignment.CENTER,
Data=outline?"CircleHollow":"Circle",Position=pos,Size=new Vector2(size,size),Color=colour??this.surface.ScriptForegroundColor,RotationOrScale=0f,});this.cursor.X+=size;return this;}}public class Template
{public class Token{public bool isText=true;public string value=null;}public class Node{public string action;public string text;public DrawingSurface.Options options;public MySprite?sprite;public Node(string
action,string text=null,DrawingSurface.Options opts=null){this.action=action;this.text=text;this.options=opts??new DrawingSurface.Options();if(action=="text"){this.options.text=this.options.text??text;
this.sprite=DrawingSurface.TextSprite(this.options);}}}public delegate void DsCallback(DrawingSurface ds,string token,DrawingSurface.Options options);public delegate string TextCallback();public Program
program;public System.Text.RegularExpressions.Regex tokenizer;public System.Text.RegularExpressions.Regex cmdSplitter;public System.Text.RegularExpressions.Match match;public Token token;public Dictionary<string,
DsCallback>methods;public Dictionary<string,List<Node>>renderNodes;public Dictionary<string,Dictionary<string,bool>>templateVars;public Dictionary<string,string>prerenderedTemplates;public List<int>removeNodes;
public char[]splitSemi=new[]{';'};public char[]splitDot=new[]{'.'};public string[]splitLine=new[]{"\r\n","\r","\n"};public Template(Program program=null){this.program=program;this.tokenizer=Util.Regex(@"((?<!\\)\{([^\}]|\\\})+(?<!\\)\}|(\\\{|[^\{])+)",
System.Text.RegularExpressions.RegexOptions.Compiled);this.cmdSplitter=Util.Regex(@"(?<newline>\?)?(?<name>[^:]+)(:(?<params>[^:]*))?(:(?<text>.+))?",System.Text.RegularExpressions.RegexOptions.Compiled);
this.token=new Token();this.methods=new Dictionary<string,DsCallback>();this.renderNodes=new Dictionary<string,List<Node>>();this.templateVars=new Dictionary<string,Dictionary<string,bool>>();this.prerenderedTemplates
=new Dictionary<string,string>();this.removeNodes=new List<int>();this.Reset();}public void Reset(){this.Clear();this.Register("textCircle",(DrawingSurface ds,string text,DrawingSurface.Options options)
=>ds.TextCircle(options));this.Register("circle",(DrawingSurface ds,string text,DrawingSurface.Options options)=>ds.Circle(options));this.Register("bar",(DrawingSurface ds,string text,DrawingSurface.Options
options)=>ds.Bar(options));this.Register("midBar",(DrawingSurface ds,string text,DrawingSurface.Options options)=>ds.MidBar(options));this.Register("multiBar",(DrawingSurface ds,string text,DrawingSurface.Options
options)=>ds.MultiBar(options));this.Register("right",(DrawingSurface ds,string text,DrawingSurface.Options options)=>ds.SetCursor(ds.width,null));this.Register("center",(DrawingSurface ds,string text,
DrawingSurface.Options options)=>ds.SetCursor(ds.width/2f,null));this.Register("saveCursor",this.SaveCursor);this.Register("setCursor",this.SetCursor);}public void Clear(){this.templateVars.Clear();this.prerenderedTemplates.Clear();
this.renderNodes.Clear();this.methods.Clear();}public void Register(string key,DsCallback callback){this.methods[key]=callback;}public void Register(string key,TextCallback callback){this.methods[key]
=(DrawingSurface ds,string text,DrawingSurface.Options options)=>ds.Text(callback(),options);}public void ConfigureScreen(DrawingSurface ds,DrawingSurface.Options options){ds.surface.Font=options.custom.Get("font",
ds.surface.Font);ds.surface.FontSize=options.size==0f?ds.surface.FontSize:options.size;ds.surface.TextPadding=options.textPadding??ds.surface.TextPadding;ds.surface.ScriptForegroundColor=options.colour
??ds.surface.ScriptForegroundColor;ds.surface.ScriptBackgroundColor=options.bgColour??ds.surface.ScriptBackgroundColor;}public bool IsPrerendered(string outputName,string templateString){string value=
this.prerenderedTemplates.Get(outputName,null);if(value==""||value==null){return false;}if(String.CompareOrdinal(value,templateString)!=0){return false;}return true;}public Dictionary<string,bool>PreRender(string
outputName,string templateStrings){this.prerenderedTemplates[outputName]=templateStrings;return this.PreRender(outputName,templateStrings.Split(splitLine,StringSplitOptions.None));}private Dictionary<string,
bool>PreRender(string outputName,string[]templateStrings){if(this.templateVars.ContainsKey(outputName)){this.templateVars[outputName].Clear();}else{this.templateVars[outputName]=new Dictionary<string,
bool>();}List<Node>nodeList=new List<Node>();bool autoNewline;string text;for(int i=0;i<templateStrings.Length;++i){string line=templateStrings[i].TrimEnd();autoNewline=true;this.match=null;text=null;
while(this.GetToken(line)){if(this.token.isText){text=System.Text.RegularExpressions.Regex.Replace(this.token.value,@"\\([\{\}])","$1");nodeList.Add(new Node("text",text));continue;}System.Text.RegularExpressions.Match
m=this.cmdSplitter.Match(this.token.value);if(!m.Success){this.AddTemplateTokens(this.templateVars[outputName],this.token.value);nodeList.Add(new Node(this.token.value));continue;}var opts=this.StringToOptions(m.Groups["params"].Value);
if(m.Groups["newline"].Value!=""){opts.custom["noNewline"]="true";autoNewline=false;}text=m.Groups["text"].Value==""?null:m.Groups["text"].Value;if(text!=null){text=System.Text.RegularExpressions.Regex.Replace(text,
@"\\([\{\}])","$1");opts.text=text;}string action=m.Groups["name"].Value;this.AddTemplateTokens(this.templateVars[outputName],action);nodeList.Add(new Node(action,text,opts));if(action=="config"){autoNewline
=false;}}if(autoNewline){nodeList.Add(new Node("newline"));}}this.renderNodes[outputName]=nodeList;return this.templateVars[outputName];}public void AddTemplateTokens(Dictionary<string,bool>tplVars,string
name){string prefix="";foreach(string part in name.Split(splitDot,StringSplitOptions.RemoveEmptyEntries)){tplVars[$"{prefix}{part}"]=true;prefix=$"{prefix}{part}.";}}public void Render(DrawingSurface ds,
string name=null){string dsName=name??ds.name;List<Node>nodeList=null;if(!this.renderNodes.TryGetValue(dsName,out nodeList)){ds.Text("No template found").Draw();return;}DsCallback callback=null;int i=
0;this.removeNodes.Clear();foreach(Node node in nodeList){if(node.action=="newline"){ds.Newline();continue;}if(node.action=="text"){ds.AddTextSprite((MySprite)node.sprite);continue;}if(node.action=="config")
{this.ConfigureScreen(ds,node.options);removeNodes.Add(i);continue;}if(this.methods.TryGetValue(node.action,out callback)){callback(ds,node.text,node.options);}else{ds.Text($"{{{node.action}}}");}i++;
}ds.Draw();foreach(int removeNode in removeNodes){nodeList.RemoveAt(removeNode);}}public DrawingSurface.Options StringToOptions(string options=""){DrawingSurface.Options opts=new DrawingSurface.Options();
if(options==""){return opts;}Dictionary<string,string>parsed=options.Split(splitSemi,StringSplitOptions.RemoveEmptyEntries).Select(v=>v.Split('=')).ToDictionary(pair=>pair.Length>1?pair[0]:"unknown",pair
=>pair.Length>1?pair[1]:pair[0]);string value;if(parsed.Pop("width",out value)){opts.width=Util.ParseFloat(value);}if(parsed.Pop("height",out value)){opts.height=Util.ParseFloat(value);}if(parsed.Pop("outline",
out value)){opts.outline=Util.ParseBool(value);}if(parsed.Pop("bgColour",out value)){opts.bgColour=DrawingSurface.StringToColour(value);}if(parsed.Pop("colour",out value)){opts.colour=DrawingSurface.StringToColour(value);
}if(parsed.Pop("fillColour",out value)){opts.fillColour=DrawingSurface.StringToColour(value);}if(parsed.Pop("textColour",out value)){opts.textColour=DrawingSurface.StringToColour(value);}if(parsed.Pop("height",
out value)){opts.height=Util.ParseFloat(value);}if(parsed.Pop("high",out value)){opts.high=Util.ParseFloat(value);}if(parsed.Pop("low",out value)){opts.low=Util.ParseFloat(value);}if(parsed.Pop("net",
out value)){opts.net=Util.ParseFloat(value);}if(parsed.Pop("pad",out value)){opts.pad=Util.ParseFloat(value);}if(parsed.Pop("pct",out value)){opts.pct=Util.ParseFloat(value);}if(parsed.Pop("scale",out
value)){opts.scale=Util.ParseFloat(value);}if(parsed.Pop("size",out value)){opts.size=Util.ParseFloat(value);}if(parsed.Pop("width",out value)){opts.width=Util.ParseFloat(value);}if(parsed.Pop("text",
out value)){opts.text=value;}if(parsed.Pop("textPading",out value)){opts.textPadding=Util.ParseFloat(value);}if(parsed.Pop("align",out value)){opts.align=DrawingSurface.stringToAlignment.Get(value);}if
(parsed.Pop("colours",out value)){opts.colours=value.Split(DrawingSurface.underscoreSep).Select(col=>DrawingSurface.StringToColour(col)??Color.White).ToList();}if(parsed.Pop("values",out value)){opts.values
=value.Split(DrawingSurface.underscoreSep).Select(pct=>Util.ParseFloat(pct,0f)).ToList();}opts.custom=parsed;return opts;}public void Echo(string text){if(this.program!=null){this.program.Echo(text);}
}public bool GetToken(string line){if(this.match==null){this.match=this.tokenizer.Match(line);}else{this.match=this.match.NextMatch();}if(this.match.Success){try{string _token=this.match.Groups[1].Value;
if(_token[0]=='{'){this.token.value=_token.Substring(1,_token.Length-2);this.token.isText=false;}else{this.token.value=_token;this.token.isText=true;}}catch(Exception e){this.Echo($"err parsing token {e}");
return false;}return true;}else{return false;}}public float?ParseCursor(DrawingSurface ds,string input,bool vertical=false){if(input==null){return null;}float saved=vertical?ds.savedCursor.Y:ds.savedCursor.X;
if(input=="x"||input=="y"){return saved;}float width=vertical?ds.height:ds.width;if(input[input.Length-1]=='%'){return width*Util.ParseFloat(input.Remove(input.Length-1))/100f;}float current=vertical?
ds.cursor.Y:ds.cursor.X;if(input=="~x"||input=="~y"){return Math.Max(current,saved);}float charSize=vertical?ds.charSizeInPx.Y:ds.charSizeInPx.X;if(input[0]=='+'){return current+Util.ParseFloat(input)
*charSize;}if(input[0]=='-'){return current-Util.ParseFloat(input)*charSize;}return Util.ParseFloat(input);}public void SaveCursor(DrawingSurface ds,string text,DrawingSurface.Options options){float x
=ds.cursor.X;float y=ds.cursor.Y;this.SetCursor(ds,text,options);ds.savedCursor.X=x;ds.savedCursor.Y=y;}public void SetCursor(DrawingSurface ds,string text,DrawingSurface.Options options){float?x=this.ParseCursor(ds,
options.custom.Get("x"),false);float?y=this.ParseCursor(ds,options.custom.Get("y"),true);ds.SetCursor(x,y);}}static System.Globalization.NumberFormatInfo CustomFormat;public static System.Globalization.NumberFormatInfo
GetCustomFormat(){if(CustomFormat==null){CustomFormat=(System.Globalization.NumberFormatInfo)System.Globalization.CultureInfo.InvariantCulture.NumberFormat.Clone();CustomFormat.NumberGroupSeparator=$"{(char)0xA0}";
CustomFormat.NumberGroupSizes=new[]{3};}return CustomFormat;}public static class Util{public static StringBuilder sb=new StringBuilder("");public static System.Text.RegularExpressions.Regex surfaceExtractor
=Util.Regex(@"\s<(\d+)>$",System.Text.RegularExpressions.RegexOptions.Compiled);public static string GetFormatNumberStr(double input){return Util.GetFormatNumberStr((VRage.MyFixedPoint)input);}public static
string GetFormatNumberStr(float input){return Util.GetFormatNumberStr((VRage.MyFixedPoint)input);}public static string GetFormatNumberStr(VRage.MyFixedPoint input){int n=Math.Max(0,(int)input);if(n==0)
{return"0";}if(n<10000){return"#,,#";}if(n<1000000){return"###,,0,K";}sb.Clear();for(int i=$"{n}".Length;i>0;--i){sb.Append("#");}return$"{sb}0,,.0M";}public static string FormatNumber(double input,string
fmt=null){return Util.FormatNumber((VRage.MyFixedPoint)input,fmt);}public static string FormatNumber(float input,string fmt=null){return Util.FormatNumber((VRage.MyFixedPoint)input,fmt);}public static
string FormatNumber(VRage.MyFixedPoint input,string fmt=null){fmt=fmt??Util.GetFormatNumberStr(input);int n=Math.Max(0,(int)input);return n.ToString(fmt,GetCustomFormat());}public static string TimeFormat(double
ms,bool s=false){TimeSpan t=TimeSpan.FromMilliseconds(ms);if(t.Hours!=0){return String.Format("{0:D}h{1:D}m",t.Hours,t.Minutes);}if(t.Minutes!=0){return String.Format("{0:D}m",t.Minutes);}return(s?String.Format("{0:D}s",
t.Seconds):"< 1m");}public static string ToItemName(MyProductionItem i){string id=i.BlueprintId.ToString();if(id.Contains("IngotBasic")){return"Stone to ingot";}if(id.Contains("/")){return id.Split('/')[1];
}return id;}public static string PctString(double val){return Util.PctString((float)val);}public static string PctString(float val){return(val*100).ToString("#,0.00",GetCustomFormat())+" %";}public static
System.Text.RegularExpressions.Regex Regex(string pattern,System.Text.RegularExpressions.RegexOptions opts=System.Text.RegularExpressions.RegexOptions.None){return new System.Text.RegularExpressions.Regex(pattern,
opts);}public static string Plural(int count,string ifOne,string otherwise){return count==1?ifOne:otherwise;}public static int ParseInt(string str,int defaultValue=0){int output;if(!int.TryParse(str,out
output)){output=defaultValue;}return output;}public static float ParseFloat(string str,float defaultValue=0f){float output;if(!float.TryParse(str,out output)){output=defaultValue;}return output;}public
static bool ParseBool(string str,bool defaultValue=false){bool output;if(!bool.TryParse(str,out output)){output=defaultValue;}return output;}public static bool BlockValid(IMyCubeBlock block){return block
!=null&&block.WorldMatrix.Translation!=Vector3.Zero;}}}public static class Dict{public static TValue Get<TKey,TValue>(this Dictionary<TKey,TValue>dict,TKey key,TValue defaultValue=default(TValue)){TValue
value;return dict.TryGetValue(key,out value)?value:defaultValue;}public static TValue Default<TKey,TValue>(this Dictionary<TKey,TValue>dict,TKey key,TValue value){return dict[key]=dict.Get(key,value);
}public static string Print<TKey,TValue>(this Dictionary<TKey,TValue>dict){StringBuilder sb=new StringBuilder("{ ");foreach(KeyValuePair<TKey,TValue>keyValues in dict){sb.Append($"{keyValues.Key}: {keyValues.Value}, ");
}return sb.Append("}").ToString();}public static bool Pop<TKey,TValue>(this Dictionary<TKey,TValue>dict,TKey key,out TValue result){if(dict.TryGetValue(key,out result)){dict.Remove(key);return true;};
return false;}