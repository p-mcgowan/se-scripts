const string PANEL_NAME = "LCD Panel Energy";
Color blue = new Color(0, 60, 255);
Color green = new Color(0, 240, 0);

const char BAR_FULL = '\u2588';
const char BAR_EMPTY = '_';
public static readonly float[] FONT_DIM = { 30f - 2f, 42f - 2f };
public static readonly int[] SCREEN_DIM = { 736, 708 };
double fontSize = 0;
int chars = 0;

string PctString(float val) {
    return String.Format("{0,3:0}%", 100 * val);
}

string ProgressBar(float charge) {
    var pct = PctString(charge);
    var barLen = (int)chars - 6;
    var barFillLen = (int)Math.Floor(barLen * charge);
    return "[".PadRight(barFillLen, BAR_FULL) + "".PadLeft(barLen - barFillLen, BAR_EMPTY) + "] " + pct;
}

void Main() {
    List<IMyJumpDrive> jumpDrives = new List<IMyJumpDrive>();
    List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyJumpDrive>(jumpDrives, b => b.CubeGrid == Me.CubeGrid);
    GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(batteries, b => b.CubeGrid == Me.CubeGrid);

    var panel = GridTerminalSystem.GetBlockWithName(PANEL_NAME) as IMyTextPanel;
    if (panel == null) {
        List<IMyTextPanel> panels = new List<IMyTextPanel>();
        GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels, p => p.CubeGrid == Me.CubeGrid);
        if (panels.Count != 1) {
            Echo("ERROR: Could not figure out which panel to write to.");
            return;
        } else {
            Echo("WARN: Did not find panel \"" + PANEL_NAME + "\". Using " + panels[0].CustomName);
            panel = panels[0];
        }
    }
    fontSize = panel.FontSize;
    chars = (int)Math.Floor(SCREEN_DIM[0] / (FONT_DIM[0] * fontSize));

    float maxCharge = 0;
    float currentCharge = 0;

    float maxBattCharge = 0;
    float currentBattCharge = 0;

    List<string> progressBars = new List<string>();
    string output = "";
    if (jumpDrives.Any()) {
        foreach (IMyJumpDrive d in jumpDrives) {
            progressBars.Add(d.CustomName + PctString(d.CurrentStoredPower / d.MaxStoredPower).PadLeft((int)chars - d.CustomName.Length));
            currentCharge += d.CurrentStoredPower;
            maxCharge += d.MaxStoredPower;
        }
        output += "Jump drive status:\n" + ProgressBar(currentCharge / maxCharge) + "\n";
    }

    if (batteries.Any()) {
        foreach (IMyBatteryBlock d in batteries) {
            progressBars.Add(d.CustomName + PctString(d.CurrentStoredPower / d.MaxStoredPower).PadLeft((int)chars - d.CustomName.Length));
            currentBattCharge += d.CurrentStoredPower;
            maxBattCharge += d.MaxStoredPower;
        }
        output += "Battery status:\n" + ProgressBar(currentBattCharge / maxBattCharge) + "\n";
    }

    output += "\n";

    foreach (string s in progressBars) {
        output += s + '\n';
    }

    // panel.FontColor = (currentCharge == maxCharge ? green : blue);
    panel.FontColor = blue;
    panel.Font = "Monospace";
    panel.WritePublicText(output);
    panel.ShowPublicTextOnScreen();
}

public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
}

/*/
// Tag any LCD Custom Data box with this to display the Images.
const string lcdImage = "[Jump Image]";

// Tag any LCD Custom Data box with this to display a Text bar.
const string lcdText = "[Jump Text]";

// The Image set to use.
const string imagePrefix = "Jump ";

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
}

void Main()
{
    string imageName = lcdImage.ToLower();
    string textName = lcdText.ToLower();

    List<IMyTerminalBlock> jumpDrives = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyJumpDrive>(jumpDrives, b => b.CubeGrid == Me.CubeGrid);
    List<IMyTerminalBlock> lcds = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(lcds, b => b.CubeGrid == Me.CubeGrid);

    float maxCharge = 0;
    float currentCharge = 0;
    float totalCharge = 0;
    int ready = 0;
    int charging = 0;
    float fontSize = 0.66f;
    float stringSize = 0f;
    if (fontSize > 1)
    {
        stringSize = 28f * fontSize;
    }
    else if (fontSize <= 1)
    {
        stringSize = 28f / fontSize;
    }
    string count = jumpDrives.Count.ToString();

    if (!jumpDrives.Any())
    {
        Echo("No Jump Drives on grid.");
        return;
    }
    if (jumpDrives.Any())
    {
        foreach (IMyJumpDrive d in jumpDrives)
        {
            currentCharge += d.CurrentStoredPower * 1000000;
            maxCharge += d.MaxStoredPower * 1000000;
            switch( d.Status )
            {
            case  MyJumpDriveStatus.Charging:
                charging++;
                break;
            case MyJumpDriveStatus.Ready:
                ready++;
                break;
            }
        }
        totalCharge += (currentCharge / maxCharge) * 100;
    }

    string imagename = imagePrefix + totalCharge.ToString("000");
    float imageC = 0;
    float textC = 0;

    foreach (IMyTextPanel lcd in lcds)
    {
        string value = lcd.CustomData;

        if (value == lcdImage)
        {
            lcd.ShowTextureOnScreen();
            if (imagename != lcd.CurrentlyShownImage)
            {
                lcd.AddImageToSelection(imagename);
                lcd.RemoveImageFromSelection(lcd.CurrentlyShownImage);
            }
            if (lcd.CurrentlyShownImage == null)
            {
                lcd.AddImageToSelection(imagename);
            }
            imageC++;
        }
        if (value == lcdText)
        {
            lcd.ClearImagesFromSelection();
            lcd.ShowPublicTextOnScreen();
            lcd.WritePublicText(volumeBars(count, stringSize, totalCharge, maxCharge, currentCharge, ready, charging), false);
            lcd.SetValue("FontSize", fontSize);
            lcd.SetValue("Font", (long)1147350002);
            textC++;
        }

    }
    if (imageC > 0)
    {
        Echo("Displaying Image on " + imageC + " LCD(s).");
    }
    if (textC > 0)
    {
        Echo("Displaying Text on " + textC + " LCD(s).");
    }
}

static char rgb(byte r, byte g, byte b)
{
    return (char)(0xe100 + (r << 6) + (g << 3) + b);
}

public string CenterText(string text, float size)
{
    string blankText = "";

    for (int i = 1; i < (size - text.Length) / 2; i++)
    { blankText = blankText + " "; }

    string printLine = blankText + text;
    return printLine;
}

public string volumeBars(string count, float stringSize, float totalCharge, float maxCharge, float currentCharge, int ready, int charging)
{
    StringBuilder output = new StringBuilder();
    output.Clear();
    string total = "Total: " + (totalCharge / 100).ToString("P2") + "\n";
    string counting = "Jump Drives: " + count + " | " + total;

    output.Append(CenterText(counting, stringSize));
    //output.Append(CenterText(total, stringSize));
    output.Append(barBuilder(totalCharge));
    output.Append("\nCharging: " + charging + "\n   Ready: " + ready + "\n\n");
    output.Append("Maximum Charge: " + DisplayLargeNumber(maxCharge) + "Wh\n");
    output.Append("Current Charge: " + DisplayLargeNumber(currentCharge ) + "Wh\n\n");

    string volumeBar = output.ToString();
    output.Clear();
    return volumeBar;
}

string DisplayLargeNumber(float number) {
    string powerValue = " kMGTPEZY";
    float result = number;
    int ordinal = 0;
    while (ordinal < powerValue.Length && result >= 1000) {
        result /= 1000;
        ordinal++;
    }
    string resultString = Math.Round(result, 2, MidpointRounding.AwayFromZero).ToString();
    if (ordinal > 0) {
        resultString += " " + powerValue[ordinal];
    }
    return resultString;
}

public string barBuilder(float num)
{
    double p = 0.0d;
    int i = 0;
    int l = 0;
    int m = 0;
    int n =0;
    StringBuilder barString = new StringBuilder();
    p = num;
    m = 27;
    while (m > 0)
    {
        barString.Append(rgb(3,3,3));  //Empty Space
        m--;
    }
    barString.Append("\n" + rgb(3,3,3));
    for (i = 0; i < (p / 4); i++)
    {
        if (p > 0 && p <= 20)
        {
            barString.Append(rgb(7, 0, 0)); // Red Bar
        }
        else if (p > 20 && p <= 40)
        {
            barString.Append(rgb(7, 4, 0)); // Orange Bar
        }
        else if (p > 40 && p <= 60)
        {
            barString.Append(rgb(7, 7, 0)); // Yellow Bar
        }
        else if (p > 60 && p <= 80)
        {
            barString.Append(rgb(0, 7, 0)); // Green Bar
        }
        else if (p > 80 && p < 100)
        {
            barString.Append(rgb(0, 0, 7)); // Blue Bar
        }
        else if (p == 100)
        {
            barString.Append(rgb(0, 7, 7)); // Cyan Bar
        }
    }
    l = 25 - i;
    while (l > 0)
    {
        barString.Append(rgb(1, 1, 1));  //Empty Space
        l--;
    }
    barString.Append(rgb(3,3,3) + "\n");
    n = 27;
    while (n > 0)
    {
        barString.Append(rgb(3, 3, 3));  //Empty Space
        n--;
    }
    barString.Append("\n");
    string barOutput = barString.ToString();
    barString.Clear();
    return barOutput;
}
/*/