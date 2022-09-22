List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();

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

public string GetRayPoint(IMyCameraBlock block, float distance) {
    block.EnableRaycast = true;
    MyDetectedEntityInfo info = block.Raycast(distance == -1 ? block.AvailableScanRange : distance, 0, 0);
    Vector3D? point = info.HitPosition;

    return GpsAt((Vector3D)point, "cast");
}

public void SetCamRaycasting(string blockName, bool on) {
    IMyCameraBlock block = (IMyCameraBlock)GetBlockWithName(blockName);
    block.EnableRaycast = on;
}

public string GetCli(MyCommandLine cli, string args, int argc) {
    foreach (string arg in args.Split(',').ToList()) {
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

public void Run(string argument, UpdateType updateSource) {
    MyCommandLine cli = new MyCommandLine();
    cli.TryParse(argument);

    if (cli.Switch("-help") || cli.Switch("h")) {
        Echo("Raycast or poor-mans raycast at");
        Echo("things");
        Echo("options:");
        Echo("-o, --on");
        Echo("  enable raycasting");
        Echo("-O, --off");
        Echo("  disable raycasting");
        Echo("-c, --cast CAM");
        Echo("  Use camera called \"cam\" to");
        Echo("  raycast and save as gps");
        Echo("-d, --distance DIST");
        Echo("  Set max distance to raycast");
        Echo("  (default infinite)");
        Echo("-l, --left CAM");
        Echo("  Using camera CAM, write a");
        Echo("  plane to custom data");
        Echo("-r, --right CAM");
        Echo("  Using line from camera CAM and");
        Echo("  plane in custom data, save");
        Echo("  intersection as gps to custom");
        Echo("  data");
        Echo("-n, --name NAME  set gps name");
        Echo("-p, --point PNT  use point from --cast cam (meters)");

        return;
    }

    string distance = GetCli(cli, "--distance,-d", 0);
    string raycast = GetCli(cli, "--cast,-c", 0);
    string leftCam = GetCli(cli, "--left,-l", 0);
    string rightCam = GetCli(cli, "--right,-r", 0);
    string name = GetCli(cli, "--name,-n", 0);
    string point = GetCli(cli, "--point,-p", 0);

    if (cli.Switch("-on") || cli.Switch("o") || cli.Switch("-off") || cli.Switch("O")) {
        bool on = cli.Switch("-on") || cli.Switch("o");
        SetCamRaycasting(raycast, on);
        SetCamRaycasting(leftCam, on);
        SetCamRaycasting(rightCam, on);

        return;
    }

    if (point != null) {
        IMyTerminalBlock block = GetBlockWithName(raycast);
        Vector3D vec = new Vector3D(block.GetPosition() + float.Parse(point, System.Globalization.CultureInfo.InvariantCulture) * block.WorldMatrix.Forward);
        Me.CustomData = GpsAt(vec, name ?? "point");

        return;
    }

    if (raycast != null) {
        IMyTerminalBlock block = GetBlockWithName(raycast);
        float dist = distance == null ? -1 : float.Parse(distance, System.Globalization.CultureInfo.InvariantCulture);
        Me.CustomData = GetRayPoint((IMyCameraBlock)block, dist);

        return;
    }

    if (leftCam != null) {
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
}

public void Main(string argument, UpdateType updateSource) {
    try {
        Run(argument, updateSource);
    } catch (Exception e) {
        Echo("Lidar failed:");
        Echo(e.Message);
    }
}
