List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
System.Text.RegularExpressions.Regex gpsRe = new System.Text.RegularExpressions.Regex(
    "^GPS:(?<name>[^:]+):(?<x>[^:]+):(?<y>[^:]+):(?<z>[^:]+):",
    System.Text.RegularExpressions.RegexOptions.None
);

public void Fail(string message) {
    throw new Exception(message);
}

public string GpsAt(Vector3D point, string name = ".", string colour = "") {
    return "GPS:" + name + ":" +
        point.X.ToString() + ":" +
        point.Y.ToString() + ":" +
        point.Z.ToString() + ":" + colour + ":";
}

public Plane GetBlockPlane(IMyTerminalBlock block) {
    return new Plane(block.GetPosition(), block.WorldMatrix.Right);
}

public void WritePlaneToCustomData(Plane plane) {
    Vector3D normal = plane.Normal;
    float distance = plane.D;

    Me.CustomData = String.Format("{0},{1},{2},{3}", normal.X, normal.Y, normal.Z, distance);
}

public Plane ReadPlaneFromCustomData() {
    string data = Me.CustomData;
    List<float> points = data.Split(',').ToList().ConvertAll<float>(s =>
        float.Parse(s, System.Globalization.CultureInfo.InvariantCulture));

    Vector3D normal = new Vector3D(points[0], points[1], points[2]);
    float distance = points[3];

    return new Plane(normal, distance);
}

public string GetRayPoint(IMyCameraBlock block, float distance, string name = "cast") {
    bool prev = block.EnableRaycast;
    block.EnableRaycast = true;
    MyDetectedEntityInfo info = block.Raycast(distance == -1 ? block.AvailableScanRange : distance, 0, 0);
    Vector3D? point = info.HitPosition;
    block.EnableRaycast = prev;

    return GpsAt((Vector3D)point, "cast");
}

public void SetCamRaycasting(string blockName, bool on) {
    IMyCameraBlock block = (IMyCameraBlock)GetBlockWithName(blockName);
    block.EnableRaycast = on;
}

public string GetCli(MyCommandLine cli, string[] args, int argc = 0) {
    foreach (string arg in args) {
        string res = cli.Switch(arg, argc);
        if (res != null) {
            return res;
        }
    }

    return null;
}

public IMyTerminalBlock GetBlockWithName(string name) {
    blocks.Clear();
    GridTerminalSystem.SearchBlocksOfName(name, blocks, c => c.CubeGrid == Me.CubeGrid && c.CustomName == name);
    if (blocks.Count != 1) {
        return null;
    }

    return blocks[0];
}

public Vector3D? GetPointFromGps(string gps) {
    System.Text.RegularExpressions.Match match = gpsRe.Match(gps);
    if (!match.Success) {
        return null;
    }

    return new Vector3D(
        float.Parse(match.Groups["x"].Value, System.Globalization.CultureInfo.InvariantCulture),
        float.Parse(match.Groups["y"].Value, System.Globalization.CultureInfo.InvariantCulture),
        float.Parse(match.Groups["z"].Value, System.Globalization.CultureInfo.InvariantCulture)
    );
}

string usage = @"Raycast or poor-mans raycast at things
options:
-o, --on
  enable raycasting
-O, --off
  disable raycasting
-c, --cast CAM
  Use camera called ""cam"" to raycast and save as gps
-d, --distance DIST
  Set max distance to raycast (default infinite)
-l, --left CAM
  Using camera CAM, write a plane to custom data
-r, --right CAM
  Using line from camera CAM and plane in custom data, save intersection as gps to custom data
-n, --name NAME
  set gps name
-p, --point PNT
  use point from --cast cam (meters)
-t, --towards KM
  use gps in custom data, get a point KM distance towards current position";

public void Run(string argument, UpdateType updateSource) {
    MyCommandLine cli = new MyCommandLine();
    cli.TryParse(argument);

    if (cli.Switch("-help") || cli.Switch("h")) {
        Echo(usage);

        return;
    }

    string distance = GetCli(cli, new[] { "--distance", "-d" });
    string raycast = GetCli(cli, new[] { "--cast", "-c" });
    string leftCam = GetCli(cli, new[] { "--left", "-l" });
    string rightCam = GetCli(cli, new[] { "--right", "-r" });
    string name = GetCli(cli, new[] { "--name", "-n" });
    string point = GetCli(cli, new[] { "--point", "-p" });
    string towards = GetCli(cli, new[] { "--towards", "-t" });

    if (cli.Switch("-on") || cli.Switch("o") || cli.Switch("-off") || cli.Switch("O")) {
        bool on = cli.Switch("-on") || cli.Switch("o");
        SetCamRaycasting(raycast, on);
        SetCamRaycasting(leftCam, on);
        SetCamRaycasting(rightCam, on);

        return;
    }

    if (point != null) {
        Me.CustomData = "";
        IMyTerminalBlock block = GetBlockWithName(raycast);
        Vector3D vec = new Vector3D(block.GetPosition() + float.Parse(point, System.Globalization.CultureInfo.InvariantCulture) * block.WorldMatrix.Forward);
        Me.CustomData = GpsAt(vec, name ?? "point");

        return;
    }

    if (raycast != null) {
        Me.CustomData = "";
        IMyTerminalBlock block = GetBlockWithName(raycast);
        float dist = distance == null ? -1 : float.Parse(distance, System.Globalization.CultureInfo.InvariantCulture);
        Me.CustomData = GetRayPoint((IMyCameraBlock)block, dist, name);

        return;
    }

    if (leftCam != null) {
        Me.CustomData = "";
        IMyTerminalBlock block = GetBlockWithName(leftCam);
        Plane leftPlane = GetBlockPlane(block);
        WritePlaneToCustomData(leftPlane);

        return;
    }

    if (rightCam != null) {
        IMyTerminalBlock block = GetBlockWithName(rightCam);
        Ray ray = new Ray(block.GetPosition(), block.WorldMatrix.Forward);
        Plane leftCamPlane = ReadPlaneFromCustomData();
        float? intersectionDistance = ray.Intersects(leftCamPlane);

        if (intersectionDistance != null) {
            Vector3D vec = new Vector3D(block.GetPosition() + (float)intersectionDistance * block.WorldMatrix.Forward);
            Me.CustomData = GpsAt(vec, name ?? "target");
        }

        return;
    }

    if (towards != null) {
        int kms = 1000 * int.Parse(towards, System.Globalization.CultureInfo.InvariantCulture);

        string initial = Me.CustomData.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)[0];
        Me.CustomData = "";

        Vector3D? res = GetPointFromGps(initial);
        if (res == null) {
            Echo($"failed to parse GPS from CustomData");

            return;
        }
        Vector3D gpsTarget = (Vector3D)res;
        Vector3D direction = Me.GetPosition() - gpsTarget;
        direction.Normalize();
        Vector3D target = gpsTarget + kms * direction;
        Me.CustomData = $"{initial}\n{GpsAt(target, name ?? "target")}";
    }
}

public void Main(string argument, UpdateType updateSource) {
    try {
        Run(argument, updateSource);
    } catch (Exception e) {
        Echo("Lidar failed:");
        Echo(e.Message);
    }
}
