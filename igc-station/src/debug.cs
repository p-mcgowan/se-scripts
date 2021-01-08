StringBuilder logs = new StringBuilder("");
IMyTextPanel debug;

public void Log(string message, bool newline = true) {
    string text = message + (newline ? "\n" : "");
    logs.Append(text);
    string res = logs.ToString();
    Echo(res);
    Me.GetSurface(0).WriteText(text, true);
    Me.CustomData = res;
}

public string ToGps(Vector3D point, string name = "", string colour = "") {
    return $"GPS:{name}:{point.X.ToString()}:{point.Y.ToString()}:{point.Z.ToString()}:{colour}:";
}
