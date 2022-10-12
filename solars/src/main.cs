string customDataDefault = @"[panels]
name=Solar Farm Panels
[rotor]
name=Solar Farm Rotor
reverse=false

;[rotorRev]
;name=Solar Farm Rotor 2";
bool foundLocalMax = false;
bool starting = true;
float closeEnough = 0.05f;
float currentOutput = 0f;
float currentPotential = 0f;
float prevPotential = 0f;
float rotorSpinSpeed = 0.1f;
int maxReductions = 4;
int reductions = 0;
int solarCount;
string panelGroup = "Panels";
string rotorName = "Rotor";
string rotorRevName = "Rotor 2";
DrawingSurface output;
IMyBlockGroup panels;
IMyMotorStator rotor;
IMyMotorStator rotorRev;
List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
MyIni ini = new MyIni();
string reverseRotor = "false";

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

    if (Me.CustomData == "") {
        Bail($"No customData - adding default");
        Me.CustomData = customDataDefault;
        return;
    }

    MyIniParseResult result;
    if (!ini.TryParse(Me.CustomData, out result)) {
        Bail($"Failed to parse config:\n{result}");
        return;
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
    if (ini.ContainsSection("rotor") && ini.Get("rotor", "reverse").TryGetString(out reverseRotor)) {
        Echo($"reverseRotor={reverseRotor}, willrev={reverseRotor == "true"}");
        if (reverseRotor == "true") {
            rotorSpinSpeed = -1 * rotorSpinSpeed;
        }
    }

    if (panels == null || rotor == null) {
        Bail($"Did not find all blocks\n" +
            $"panelGroup: {(panels == null ? "no" : "yes")}\n" +
            $"rotor: {(rotor == null ? "no" : "yes")}\n" +
            $"rotorRev (optional): {(rotorRev == null ? "no" : "yes")}");
    }

    Runtime.UpdateFrequency = UpdateFrequency.Update100;
}

public void GetOuputs() {
    blocks.Clear();
    panels.GetBlocks(blocks);
    solarCount = 0;
    currentOutput = 0f;
    currentPotential = 0f;

    foreach (IMySolarPanel panel in blocks) {
        if (panel == null || panel.WorldMatrix.Translation == Vector3.Zero || !panel.IsWorking || !panel.IsFunctional) {
            continue;
        }
        solarCount++;
        currentPotential += panel.MaxOutput;
        currentOutput += panel.CurrentOutput;
    }
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
        GetOuputs();
        if (currentOutput == 0f) {
            SetRotorRPM(0f);
            starting = false;
        }
        ReportStatus("Finding sun");
        if (prevPotential == 0f) {
            SetRotorRPM(-2 * rotorSpinSpeed);
            prevPotential = currentPotential;
            return;
        }

        if (currentPotential < prevPotential) {
            if (foundLocalMax) {
                SetRotorRPM(0f);
                starting = false;
            } else {
                foundLocalMax = true;
                SetRotorRPM(-1 * rotor.TargetVelocityRPM);
            }
        }
        prevPotential = currentPotential;
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
    Echo($"currentOutput: {currentOutput}");
    Echo($"currentPotential: {currentPotential}");
    Echo($"prevPotential: {prevPotential}");
    Echo($"reductions: {reductions}");
    Echo($"reverseRotor: {reverseRotor}, rev={reverseRotor == "true"}");
    Echo(state);

    output
        .Text($"SOLAR MODULE").Newline()
        .Text($"{solarCount} panels {currentOutput.ToString(("0.###"))} / {currentPotential.ToString(("0.###"))} MW").Newline()
        .Newline()
        .Text($"Status: {state}")
        .Draw();
}

// bool sunObscured = false;
// bool repositionIncreased = false;

public void Main(string argument, UpdateType updateType) {
    if (starting) {
        Initialize();
        return;
    }

    GetOuputs();

    if (currentOutput == 0f) {
        ReportStatus("Waiting for sun");
        SetRotorRPM(0f);
        return;
    }

    if (rotor.TargetVelocityRPM == 0f) {
        if (currentPotential < prevPotential && ++reductions > maxReductions) {
            SetRotorRPM(rotorSpinSpeed);
            ReportStatus("Repositioning");
            reductions = 0;
            // repositionIncreased = false;
        } else {
            ReportStatus("Online");
        }
    } else {
        // if (currentPotential >= prevPotential) {
        //     repositionIncreased = true;
        //     sunObscured = false;
        // } else {
        //     sunObscured = true;
        //     ReportStatus("Can't find sun");
        //     // return;
        // }
        // else if (!repositionIncreased) {
        //     Echo("repositioning didn't increase output");
        //     SetRotorRPM(0);
        //     reductions = 0;
        //     sunObscured = true;
        //     ReportStatus("Waiting for sun");
        // }
        if (currentPotential < prevPotential && ++reductions > maxReductions) {
            SetRotorRPM(0);
            reductions = 0;
            ReportStatus("Online");
        } else {
            ReportStatus("Repositioning");
        }
    }

    prevPotential = currentPotential;
}
