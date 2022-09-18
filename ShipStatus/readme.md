# Space Engineers Ship Status Script

<img src="images/ship-status.png">
<img src="images/status1.3.png">
<img src="images/status1.1.png">

### TOC
- [About](#about)
- [Api listing](#api-listing)
- [Examples](#examples)
- [Contributing](#contributing)
- [Links](#links)

### About
A configurable ship status display which renders sprites to different surfaces using a very flexible templating engine.

Makes use of the [templating engine](https://github.com/p-mcgowan/se-scripts/tree/master/template) and [graphics lib](https://github.com/p-mcgowan/se-scripts/tree/master/graphics) scripts to draw sprites.

Reads from CustomData, parsing a template and a display target section. Easily allows for customization and extension to render awesome screens.

The templates have some built-in functionality, but you can also build your own rendering methods.

Images generated with ini format CustomData (see [Malware's MDK](https://github.com/malware-dev/MDK-SE/wiki/Handling-configuration-and-storage)):

```ini
; CustomData config:
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
;  airlock config (defaults are shown)
;airlockOpenTime=750
;airlockAllDoors=false
;airlockDoorMatch=Door(.*)
;airlockDoorExclude=Hangar
;  health config (defaults are shown)
;healthIgnore=
;healthOnHud=false

[LCD Panel]
output=
|Jump drives: {power.jumpDrives}
|{power.jumpBar}
|Batteries: {power.batteries}
|{power.batteryBar}
|Reactors: {power.reactors}, Output: {power.reactorOutputMW} MW  ({power.reactorUr} Ur)
|Solar panels: {power.solars}, Output: {power.solarOutputMW} MW
|Wind turbines: {power.turbines}, Output: {power.turbineOutputMW} MW
|H2 Engines: {power.engines}, Output: {power.engineOutputMW} MW
|Energy IO: {power.ioString}
|{power.ioBar}
|
|Ship status: {health.status}
|{health.blocks}
|
|{production.status}
|{production.blocks}
|
|Cargo: {cargo.stored} / {cargo.cap}
|{cargo.bar}
|{cargo.items}
```

### Api listing
This is an example of the options available - the template key `{something}` is followed by the value rendered:
<img src="images/api-demo.png">

The config used to generate it:
```ini
[Sci-Fi LCD Panel 5x5 2]
output=
|{text:0,60,60:\{text:colour=60,0,60,150:\\{coloured text\\}\}}: {text:colour=60,0,60,150:\{coloured text\}}
|{text:colour=0,60,60:\{cargo.bar\}}:
|{cargo.bar}
|{text:colour=0,60,60:\{cargo.cap\}} => {cargo.cap}
|{text:colour=0,60,60:\{cargo.fullString\}} => {cargo.fullString}
|{text:colour=0,60,60:\{cargo.items\}}:
|{cargo.items}
|{text:colour=0,60,60:\{cargo.stored\}} => {cargo.stored}
|{text:colour=0,60,60:\{health.status\}} => {health.status}
|{text:colour=0,60,60:\{health.blocks\}}:
|{health.blocks}
|{text:colour=0,60,60:\{production.blocks\}}:
|{production.blocks}
|{text:colour=0,60,60:\{production.status\}} => {production.status}
|{text:colour=0,60,60:\{power.ioString\}} => {power.ioString}
|{text:colour=0,60,60:\{power.input\}} => {power.input}
|{text:colour=0,60,60:\{power.output\}} => {power.output}
|{text:colour=0,60,60:\{power.maxOutput\}} => {power.maxOutput}
|{text:colour=0,60,60:\{power.ioBar\}}:
|{power.ioBar}

[Sci-Fi LCD Panel 5x5]
output=
|{text:colour=0,60,60:\{power.jumpBar\}}:
|{power.jumpBar}
|{text:colour=0,60,60:\{power.jumpCurrent\}} => {power.jumpCurrent}
|{text:colour=0,60,60:\{power.jumpDrives\}} => {power.jumpDrives}
|{text:colour=0,60,60:\{power.jumpMax\}} => {power.jumpMax}
|{text:colour=0,60,60:\{power.batteries\}} => {power.batteries}
|{text:colour=0,60,60:\{power.batteryBar\}}: 
|{power.batteryBar}
|{text:colour=0,60,60:\{power.batteryCurrent\}} => {power.batteryCurrent}
|{text:colour=0,60,60:\{power.batteryInput\}} => {power.batteryInput}
|{text:colour=0,60,60:\{power.batteryInputMax\}} => {power.batteryInputMax}
|{text:colour=0,60,60:\{power.batteryMax\}} => {power.batteryMax}
|{text:colour=0,60,60:\{power.batteryOutput\}} => {power.batteryOutput}
|{text:colour=0,60,60:\{power.batteryOutputMax\}} => {power.batteryOutputMax}
|{text:colour=0,60,60:\{power.engineOutputMax\}} => {power.engineOutputMax}
|{text:colour=0,60,60:\{power.engineOutputMW\}} => {power.engineOutputMW}
|{text:colour=0,60,60:\{power.engines\}} => {power.engines}
|{text:colour=0,60,60:\{power.reactorOutputMax\}} => {power.reactorOutputMax}
|{text:colour=0,60,60:\{power.reactorOutputMW\}} => {power.reactorOutputMW}
|{text:colour=0,60,60:\{power.reactors\}} => {power.reactors}
|{text:colour=0,60,60:\{power.reactorString\}} => {power.reactorString}
|{text:colour=0,60,60:\{power.reactors\}} => {power.reactors}
|{text:colour=0,60,60:\{power.reactorUr\}} => {power.reactorUr}
|{text:colour=0,60,60:\{power.solarOutputMax\}} => {power.solarOutputMax}
|{text:colour=0,60,60:\{power.solarOutputMW\}} => {power.solarOutputMW}
|{text:colour=0,60,60:\{power.solars\}} => {power.solars}
|{text:colour=0,60,60:\{power.turbineOutputMax\}} => {power.turbineOutputMax}
|{text:colour=0,60,60:\{power.turbineOutputMW\}} => {power.turbineOutputMW}
|{text:colour=0,60,60:\{power.turbines\}} => {power.turbines}
|{text:colour=0,60,60:\{power.ioBar\}} (inline): {power.ioBar}
```

Global config settings:

setting|value type|description|default
---|---|---|---
airlock|true/false|Toggle program airlock|false
production|true/false|Toggle program production|auto (if in template)
cargo|true/false|Toggle program cargo|auto (if in template)
power|true/false|Toggle program power|auto (if in template)
health|true/false|Toggle program health|auto (if in template)
airlockOpenTime|integer|Set airlock auto-close time (ms)|750
airlockAllDoors|true/false|Toggle auto door closing for non-airlock doors|false
airlockDoorMatch|string|Door pattern to match (regex)|Door(.*)
airlockDoorExclude|string|Door pattern to exclude|Hangar
healthIgnore|string|Pattern to ignore (eg Thrust,Wheel)|none
healthOnHud|true/false|Enable "Show on HUD" for damaged blocks|false
config|string|Configure all displays to use a theme (see config template var below)|none


The template vars available by default are:
template var|options|description
---|---|---
config| font: set the font <br/>size: set the font size <br/>textPadding: set the padding <br/>colour: set the text colour <br/>bgColour: set the background colour |Configure the display (only runs once)
cargo.bar|none|A coloured fill bar which goes from green to red based on % full
cargo.cap|none|The cargo volume capacity
cargo.fullString|none|A nicely formatted `<current> / <max> L` volume string
cargo.items|none|A list of all items in cargo. Will split into 2 columns if display is wide enough 
cargo.stored|none|Current cargo volume
health.blocks|none|A list of all damaged blocks and their percent health
health.status|none|A message saying whether damage is detected or not
power.batteries|none|Count of batteries
power.batteryBar|[default bar options](https://github.com/p-mcgowan/se-scripts/tree/master/graphics)|Coloured bar showing discharge / recharge rate and time remaining
power.batteryCurrent|none|Total of battery charge (MWh).
power.batteryInput|none|Current battery input (MW).
power.batteryInputMax|none|Max battery input (MW).
power.batteryMax|none|Max battery capacity (MWh).
power.batteryOutput|none|Current battery output (MW).
power.batteryOutputMax|none|Max battery output (MW).
power.consumers|count: int|Prints top _count_ power consumers (default 10, all when _count_=0).
power.engineOutputMax|none|Max H2 engine output (MW).
power.engineOutputMW|none|Current H2 engine output (MW).
power.engines|none|Count of H2 engines
power.ioString|none|Formatted energy IO: net output / max output (utilization %).
power.ioLegend|none|Shows a coloured legend for the io bar.
power.input|none|Current energy input (MW).
power.output|none|Current energy output (MW).
power.maxOutput|none|Max possible energy output (MW).
power.ioBar|none|Bar showing contribution to total power by block. Reactors are blue, H2 engines red, betteries green, turbines yellow, solars cyan. If blocks are disabled, they are shown in a darker colour.
power.jumpBar|[default bar options](https://github.com/p-mcgowan/se-scripts/tree/master/graphics), text: string, pct: float|Jump drive charge status. Text and percent override the default bar contents.
power.jumpCurrent|none|Jump drive current charge (MW).
power.jumpDrives|none|Jump drive count.
power.jumpMax|none|Jump drive max charge (MW).
power.reactorOutputMax|none|Reactor max output (MW).
power.reactorOutputMW|none|Reactor current output (MW).
power.reactors|none|Reactor count.
power.reactorString|text: string|Formatted reactor string: &lt;text or Reactors: >&lt;count>, Output: &lt;output> MW, Ur: &lt;uranium> kg"
power.reactors|none|Reactor count
power.reactorUr|none|Reactor uranium in kg.
power.solarOutputMax|none|Max solar output.
power.solarOutputMW|none|Current solar output.
power.solars|none|Solar panel count.
power.turbineOutputMax|none|Max wind turbine output.
power.turbineOutputMW|none|Current wind turbine output.
power.turbines|none|Wind turbine count.
production.blocks|none|List of assemblers and refineries, with a status icon and their queue (if any).
production.status|none|Overall production status (power saving, enabled, halted). The production class will turn on / off production blocks if they idle for a long time, then check every 4 minutes to see if they need to start up again.

You can also register any render method you want, and use any of the values in each of the status programs (Cargo, Production, etc..).

Also available from the [templating script](https://github.com/p-mcgowan/se-scripts/tree/master/template):  
config  
bar  
circle  
midBar  
multiBar  
text  
textCircle  

### Examples

##### Main ship

<img src="images/overview-with-consumer-columns.png" >

```ini
[global]
config=colour=150,150,100;bgColour=black;size=0.5;

[Transparent LCD]
output=
|{config:size=0.5}
|{text:colour=120,50,50:OVERVIEW}
|Jump drives: {power.jumpDrives}
|{power.jumpBar}
|Batteries: {power.batteries} {power.batteryInput} MW / {power.batteryOutput} MW
|{power.batteryBar}
|{power.ioString}
|{power.ioBar}
|{power.ioLegend}
|
|{text:colour=120,50,50:CONSUMERS}
|{power.consumers:count=6}
|
|{text:colour=120,50,50:PRODUCTION}{setCursor:x=50%}{setCursor:x=+1.5}{text:colour=120,50,50:DAMAGE}
|{?saveCursor}
|{production.status}
|{production.blocks}
|{?setCursor:x=50%}{setCursor:x=+1.5}{saveCursor:y=y}
|{health.status}
|{health.blocks}
|{?setCursor:x=0;y=~y}{saveCursor}
```

##### Mining ship

<img src="images/miner.png">

```ini
[global]
config=colour=0,60,100;bgColour=10,10,10

[Miner Control Seat <1>]
output=
|Ship status: {health.status}
|
|{health.blocks}

[Miner Programmable block Cabin <0>]
output=
|{config:size=0.5}
|Jump drives: {power.jumpDrives}
|{power.jumpBar}
|Batteries: {power.batteries}
|{power.batteryBar}
|Reactors: {power.reactors}, Output: {power.reactorOutputMW} MW  ({power.reactorUr} Ur)
|Solar panels: {power.solars}, Output: {power.solarOutputMW} MW
|Energy IO: {power.ioString}
|{power.ioBar}
|{power.ioLegend}

[Miner Control Seat <0>]
output=
|{config:size=0.5}
|Cargo: {cargo.stored} / {cargo.cap}
|{cargo.bar}
|{cargo.items}
```

---  
##### Repair ship

<img src="images/repair-ship.png">

```ini
[global]
config=size=0.5;bgColour=15,15,10;colour=200,200,200

[Miniminer Cockpit <0>]
output=
|Cargo: {cargo.fullString}
|{cargo.bar}
|Reactors: {power.reactors}, Batteries: {power.batteries}
|{power.batteryBar}
|Output: {power.reactorOutputMW} MW @ ({power.reactorUr} Ur)
|Energy IO: {power.ioString}
|{power.ioBar}
|{power.ioLegend}

[Miniminer Cockpit <2>]
output=
|Ship status: {health.status}
|{health.blocks}

[Miniminer Cockpit <1>]
output=
|{cargo.items}
```

---  
##### Random program block surfaces:

<img src="images/pb.png">

```ini
[global]
config=colour=20,40,60;bgColour=black;size=0.6

[Programmable block <0>]
output=
|{config:colour=white;bgColour=0,0,255;size=0.5}
|A problem has been detect and SEOS has been shut down to prevent damage
|to your programmable block.
|
|If this is the first time you've seen this Stop error screen,
|restart your programmable block. If this screen appears again, follow these steps:
|
|Disable or uninstall any SPRT drones, asteroid miners
|or weld walls. Check your subgrid configuration,
|and check for any wiggles / wobbles. Run KLANG /R to check
|for hard drive corruption, and then restart your programmable block.
|
|Technical information:
|*** STOP: 0x00000024 (0xDEADC0DE, OxC0FFEEEE)

[Programmable block <1>]
output=
|{config:size=0.5;colour=150,100,0;bgColour=0,10,30}
|Cargo:
|{cargo.bar}
|Jump Drives:
|{power.jumpBar}
|Batteries:
|{power.batteryBar}
|Power Sources:
|{power.ioBar}
|{power.ioLegend}
```

---  
##### Cargo info:

<img src="images/cargo.png">

```ini
[global]
config=colour=20,40,60;bgColour=black;size=0.6

[LCD Panel 0:1]
output=
|{text:scale=1.5;colour=20,80,60:Ship cargo report:}
|
|Capacity: {cargo.cap}
|Used: {cargo.stored}
|Formatted: {cargo.fullString}
|
|{text:scale=1.5;colour=20,80,60:Cargo Manifest (for port authority):}
|
|{cargo.items}
```

---  
##### Damage report:

<img src="images/damage.png">

```ini
[global]
config=colour=20,40,60;bgColour=black;size=0.6

[LCD Panel 0:2]
output=
|{text:scale=1.5;colour=20,80,60:Damage report:}
|
|Summary: {health.status}
|
|{text:scale=1.5;colour=20,80,60:List of damaged blocks (if any):}
|
|{health.blocks}
```

---  
##### Power info:

<img src="images/power.png">

```ini
[global]
config=colour=20,40,60;bgColour=black;size=0.6

[LCD Panel 0:3]
output=
|{text:scale=1.5;colour=20,80,60:Energy and power report:}
|
|Batteries:     {right}{power.batteries:align=right}
|H2 engines:    {right}{power.engines:align=right}
|Jump drives:   {right}{power.jumpDrives:align=right}
|Reactors:      {right}{power.reactors:align=right}
|Solar Panels:  {right}{power.solars:align=right}
|Wind Turbines: {right}{power.turbines:align=right}
|
|{text:scale=1.5;colour=20,80,60:Power off the charts:}
|
|Battery charge:
|{power.batteryBar:textColour=black}
|Jump drive charge:
|{power.jumpBar:textColour=black}
|
|{text:scale=1.5;colour=20,80,60:Power producer distribution:}
|
|{power.ioBar}
|{power.ioLegend}
|
|Net power IO and utilization:        {right}{power.ioString:align=right}
|Total ship power input (MW):         {right}{power.input:align=right}
|Total ship power output (MW):        {right}{power.output:align=right}
|Maxiumum power output possible (MW): {right}{power.maxOutput:align=right}
```

---  
##### Moar power info:

<img src="images/moar-power.png">

```ini
[global]
config=colour=20,40,60;bgColour=black;size=0.6

[LCD Panel 1:3]
output=
|{text:scale=1.5;colour=20,80,60:Moar power:}
|
|Batteries current input (MW):           {right}{power.batteryInput:align=right}
|Batteries max input (MW):               {right}{power.batteryInputMax:align=right}
|Batteries current charge (MWh):         {right}{power.batteryCurrent:align=right}
|Batteries max charge (MWh):             {right}{power.batteryMax:align=right}
|Batteries current output (MW):          {right}{power.batteryOutput:align=right}
|Batteries max possible output: (MW)     {right}{power.batteryOutputMax:align=right}
|
|Hydrogen engine max output (MW):        {right}{power.engineOutputMax:align=right}
|Hydrogen engine current output (MW):    {right}{power.engineOutputMW:align=right}
|
|Jump drives current charge (MWh):       {right}{power.jumpCurrent:align=right}
|Jump drives max charge (MWh):           {right}{power.jumpMax:align=right}
|
|Reactors current output (MW):           {right}{power.reactorOutputMW:align=right}
|Reactors max possible output (MW):      {right}{power.reactorOutputMax:align=right}
|Reactors Uranium count (kg):            {right}{power.reactorUr:align=right}
|Reactors summary:                       {right}{power.reactorString:align=right}
|
|Solar panels current output (MW):       {right}{power.solarOutputMW:align=right}
|Solar panels max possible output (MW):  {right}{power.solarOutputMax:align=right}
|
|Wind turbines current output (MW):      {right}{power.turbineOutputMW:align=right}
|Wind turbines max possible output (MW): {right}{power.turbineOutputMax:align=right}
```


### Contributing
Feedback, suggestions, comments, criticisms, and bug reports are all welcome and encouraged! Open an issue if you want, I'll try to keep an eye on it.

### Links
[steam workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=2314209066)  
[template engine (github)](https://github.com/p-mcgowan/se-scripts/tree/master/template)  
[template engine (workshop)](https://steamcommunity.com/sharedfiles/filedetails/?id=2314207999)  
[graphics (github)](https://github.com/p-mcgowan/se-scripts/tree/master/graphics)  
[graphics (workshop)](https://steamcommunity.com/sharedfiles/filedetails/?id=2314207214)  
