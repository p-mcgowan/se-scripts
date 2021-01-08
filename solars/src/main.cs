string panelGroup = "Panels";
string rotorName = "Rotor";
string rotorRevName = "Rotor 2";

IMyBlockGroup panels;
IMyMotorStator rotor;
IMyMotorStator rotorRev;
int solarCount;
List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
DrawingSurface output;
MyIni ini = new MyIni();
float prev = 0f;
float current = 0f;
int reductions = 0;
int maxReductions = 3;
float rotorSpinSpeed = 0.1f;
bool starting = true;
float closeEnough = 0.05f;
bool foundLocalMax = false;

public void Bail(string message) {
    Echo(message);
    string[] messages = message.Split(new [] { '\n' });
    foreach (string msg in messages) {
        output.Text(msg).Newline();
    }
    Runtime.UpdateFrequency = UpdateFrequency.None;
    Echo("Exiting - recompile after fixing errors");
}

public Program() {
    output = new DrawingSurface(Me.GetSurface(0), this, "PB");

    MyIniParseResult result;
    if (!ini.TryParse(Me.CustomData, out result)) {
        Bail($"Failed to parse config:\n{result}");
    }

    if (!ini.ContainsSection("panels") || !ini.Get("panels", "name").TryGetString(out panelGroup)) {
        Bail("No panel name in custom data");
        return;
    }
    if (!ini.ContainsSection("rotor") || !ini.Get("rotor", "name").TryGetString(out rotorName)) {
        Bail("No rotor name in custom data");
        return;
    }
    if (ini.ContainsSection("rotorRev") && !ini.Get("rotorRev", "name").TryGetString(out rotorRevName)) {
        Bail("No rotorRev name in custom data");
        return;
    }

    panels = (IMyBlockGroup)GridTerminalSystem.GetBlockGroupWithName(panelGroup);
    rotor = (IMyMotorStator)GridTerminalSystem.GetBlockWithName(rotorName);
    rotorRev = (IMyMotorStator)GridTerminalSystem.GetBlockWithName(rotorRevName);

    if (panels == null || rotor == null) {
        Bail($"Did not find all blocks\n" +
            $"panelGroup: {(panels == null ? "no" : "yes")}\n" +
            $"rotor: {(rotor == null ? "no" : "yes")}\n" +
            $"rotorRev (optional): {(rotorRev == null ? "no" : "yes")}");
    }

    Runtime.UpdateFrequency = UpdateFrequency.Update100;
}

public float GetMaxOut() {
    blocks.Clear();
    panels.GetBlocks(blocks);
    solarCount = 0;
    float max = 0f;

    foreach (IMySolarPanel panel in blocks) {
        if (panel == null || panel.WorldMatrix.Translation == Vector3.Zero || !panel.IsWorking || !panel.IsFunctional) {
            continue;
        }
        solarCount++;
        max += panel.MaxOutput;
    }

    return solarCount == 0f ? 0f : max / solarCount;
}

public void SetRotorRPM(float a) {
    rotor.TargetVelocityRPM = a;
    if (rotorRev != null) {
        rotorRev.TargetVelocityRPM = -1 * rotor.TargetVelocityRPM;
    }
}

public void Initialize() {
    float backAngle = 0f;
    if (rotorRev != null) {
        backAngle = MathHelper.TwoPi - rotorRev.Angle;
    }

    if (rotorRev == null || rotor.Angle > backAngle - closeEnough && rotor.Angle < backAngle + closeEnough) {
        current = GetMaxOut();
        ReportStatus("Finding sun");
        if (prev == 0f) {
            SetRotorRPM(-2 * rotorSpinSpeed);
            prev = current;
            return;
        }

        if (current < prev) {
            if (foundLocalMax) {
                SetRotorRPM(0f);
                starting = false;
            } else {
                foundLocalMax = true;
                SetRotorRPM(-1 * rotor.TargetVelocityRPM);
            }
        }
        prev = current;
        return;
    }
    ReportStatus("Initializing");

    float diff = backAngle - rotor.Angle;
    float speed = Math.Max(Math.Abs(diff), 0.5f);
    float dir = diff > 0 ? 1 : -1;
    rotorRev.TargetVelocityRPM = dir * speed;
    rotor.TargetVelocityRPM = 0;
}

public void ReportStatus(string state) {
    Echo($"current: {current}");
    Echo($"prev: {prev}");
    Echo($"reductions: {reductions}");
    Echo(state);

    output
        .Text($"SOLAR MODULE").Newline()
        .Text($"{solarCount} panels").Newline()
        .Text($"{solarCount * current} MW potential").Newline()
        .Newline()
        .Text($"Status: {state}")
        .Draw();
}

public void Main(string argument, UpdateType updateType) {
    if (starting) {
        Initialize();
        return;
    }

    current = GetMaxOut();

    if (rotor.TargetVelocityRPM == 0f) {
        if (current < prev && ++reductions > maxReductions) {
            SetRotorRPM(rotorSpinSpeed);
            ReportStatus("Repositioning");
            reductions = 0;
        } else {
            ReportStatus("Online");
        }
    } else {
        if (current < prev && ++reductions > maxReductions) {
            SetRotorRPM(0);
            reductions = 0;
            ReportStatus("Online");
        } else {
            ReportStatus("Repositioning");
        }
    }

    prev = current;
}
