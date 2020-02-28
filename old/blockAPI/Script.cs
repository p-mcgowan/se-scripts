static string TARGET = "VENT";
static string OUTPUT = "Text panel 8";

void Main(string argument) {
    Echo(Me.CustomName);
    var block = GridTerminalSystem.GetBlockWithName(TARGET);

    IMyTextPanel screen = ((IMyTextPanel)GridTerminalSystem.GetBlockWithName(OUTPUT));
    List<ITerminalProperty> a = new List<ITerminalProperty>();
    List<ITerminalAction> b = new List<ITerminalAction>();
    block.GetProperties(a);
    block.GetActions(b);
    screen.WritePublicText(block.CustomName + " Properties:\n");
    if (a.Count > 0) {
        for (int i = 0; i < a.Count; i++) screen.WritePublicText(a[i].Id + "    " + a[i].TypeName + "\n", true);
    } else {

        screen.WritePublicText("found nothing");
    }

    screen.WritePublicText("\n" + block.CustomName + " Actions:\n", true);
    if (b.Count > 0) {

        for (int i = 0; i < b.Count; i++)screen.WritePublicText  (b[i].Id + "    " + b[i].Name + "\n", true);
    } else {

        screen.WritePublicText("found nothing");
    }

    screen.ShowPublicTextOnScreen();
}