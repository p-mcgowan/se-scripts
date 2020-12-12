# Space Engineers Ship Status Script

<img src="images/status1.1.png">
<img src="images/ship-status.png">

### TOC
- [About](#about)
- [Api listing](#api-listing)
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
;  With the exception of airlocks, you can leave these all false and they will 
;  only be enabled if a template contains a reference to them (eg if a template 
;  has {power.bar}, then power will be enabled unless false here)
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
|{?power.jumpBar}
|Batteries: {power.batteries}
|{power.batteryBar}
|Reactors: {power.reactors} {power.reactorMw:: MW} {power.reactorUr:: Ur}
|Solar panels: {power.solars}
|Wind turbines: {power.turbines}
|H2 Engines: {power.engines}
|Energy IO: {power.io}
|{?power.ioBar}
|
|Ship status: {health.status}
|{health.blocks}
|{production.status}
|{production.blocks}
|
|Cargo: {cargo.stored} / {cargo.cap}
|{cargo.bar}
|{cargo.items}
```

### Api listing
The template vars available by default are:
template var|options|description
---|---|---
cargo.bar|none|A coloured fill bar which goes from green to red based on % full
cargo.cap|none|The cargo volume capacity
cargo.fullString|none|A nicely formatted `<current> / <max> L` volume string
cargo.items|none|A list of all items in cargo. Will split into 2 columns if display is wide enough 
cargo.stored|none|Current cargo volume
health.blocks|none|A list of all damaged blocks and their percent health
health.status|none|A message saying whether damage is detected or not
power.batteries|none|Count of batteries
power.batteryBar|none|Coloured bar showing discharge / recharge rate and time remaining
power.engines|none|Count of hydro engines
power.io|none|Shows net input / output, total potential output, and % utilization
power.ioBar|none|Bar showing contribution to total power by block. Reactors are blue, H2 engines red, betteries green, turbines yellow, solars cyan. If blocks are disabled, they are shown in a darker colour.
power.jumpBar|text: string, pct: float|Jump drive charge status. Text and percent override the default bar contents.
power.jumpDrives|none|Jump drive count.
power.reactorMw|text: string|Reactor output in MW. Text will be appended to the count (eg {power.reactorMw:: kg Ur}).
power.reactors|none|Reactor count
power.reactorUr|text: string|Reactor uranium. Text will be appended to the count.
power.solars|none|Solar panel count.
power.turbines|none|Wind turbine count.
production.blocks|none|List of assemblers and refineries, with a status icon and their queue (if any).
production.status|none|Overall production status (power saving, enabled, halted). The production class will turn on / off production blocks if they idle for a long time, then check every 4 minutes to see if they need to start up again.

You can also register any render method you want, and use any of the values in each of the status programs (Cargo, Production, etc..).

Also available from the [templating script](https://github.com/p-mcgowan/se-scripts/tree/master/template):  
bar  
circle  
midBar  
multiBar  
text  
textCircle  


### Contributing
Feedback, suggestions, comments, criticisms, and bug reports are all welcome and encouraged! Open an issue if you want, I'll try to keep an eye on it.

### Links
[steam workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=2314209066)
[template engine (github)](https://github.com/p-mcgowan/se-scripts/tree/master/template)
[template engine (workshop)](https://steamcommunity.com/sharedfiles/filedetails/?id=2314207999)
[graphics (github)](https://github.com/p-mcgowan/se-scripts/tree/master/graphics)
[graphics (workshop)](https://steamcommunity.com/sharedfiles/filedetails/?id=2314207214)
