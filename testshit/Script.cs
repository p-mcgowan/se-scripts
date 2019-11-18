// Config Vars
const string PANEL_NAME = "Text panel";
const string LIGHT_NAME = "IniTug Cargo Light";
const bool SHOW_PERCENT = true;
const bool SHOW_ITEMS = true;

// Globals
List<IMyTerminalBlock> cargo = new List<IMyTerminalBlock>();
IMyLightingBlock light;
List<IMyTextPanel> panels;
const char BAR_FULL = '\u2588';
const char BAR_EMPTY = '_';
const char LINE_SPACER = ' ';  // for small fonts, will help find corresponding value
public static readonly float[] FONT_DIM = { 30f - 2f, 42f - 2f };
public static readonly int[] SCREEN_DIM = { 738, 708 };
double fontSize = 0;
int chars = 0;
Color red = new Color(255, 0, 0);
Color yellow = new Color(255, 255, 0);
Color green = new Color(0, 240, 0);
Color blue = new Color(0, 60, 255);

public Program()
{
Runtime.UpdateFrequency = UpdateFrequency.Update100;
}

public void Init()
{
panels = new List<IMyTextPanel>();
light = GridTerminalSystem.GetBlockWithName(LIGHT_NAME) as IMyLightingBlock;
// panels = GridTerminalSystem.GetBlockWithName(PANEL_NAME) as IMyTextPanel;
GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, p => p.CubeGrid == Me.CubeGrid && p.CustomName == PANEL_NAME);
GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(cargo, c => c.CubeGrid == Me.CubeGrid &&
    (c is IMyCargoContainer || c is IMyShipDrill || c is IMyShipWelder || c is IMyShipGrinder));
}

public string FormatNumber(VRage.MyFixedPoint input)
{
string fmt;
int n = Math.Max(0, (int)input);
if (n < 10000)
{
    fmt = "##";
}
else if (n < 1000000)
{
    fmt = "###0,K";
}
else
{
    fmt = "###0,,M";
}
return n.ToString(fmt, System.Globalization.CultureInfo.InvariantCulture);
}

string PctString(float val)
{
return String.Format("{0,3:0}%", 100 * val);
}

string ProgressBar(float charge)
{
var pct = PctString(charge);
var barLen = (int)chars - 7;  // [] xxx%
var barFillLen = (int)Math.Floor(barLen * charge);
return String.Format("[{0}{1}] {2}",
    "".PadRight(barFillLen, BAR_FULL),
    "".PadLeft(barLen - barFillLen, BAR_EMPTY),
    pct);
}

public void Main(string argument, UpdateType updateSource)
{
    var surfaces = new List<IMyTextSurfaceProvider>();
    GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(surfaces);
    Echo(surfaces.Count.ToString());
    for (var i = 0; i < surfaces.Count; i++)
    {
         Echo(surfaces[i].ToString());
        Echo(surfaces[i].GetSurface(0).Name);
        surfaces[i].GetSurface(0).WriteText("hello" + i.ToString());
        //Echo(surfaces[i]);
    }
    // var surface = surfaces[0].GetSurface(0);
    // Echo(surface.ToString());
}
