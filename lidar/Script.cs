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
    IMyCameraBlock block = (IMyCameraBlock)GridTerminalSystem.GetBlockWithName(blockName);
    block.EnableRaycast = on;
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
        Echo("-p, --point PNT  use point from cam (meters)");

        return;
    }

    string distance = cli.Switch("--distance", 0) == null ? cli.Switch("-d", 0) : cli.Switch("--distance", 0);
    string raycast = cli.Switch("--cast", 0) == null ? cli.Switch("-c", 0) : cli.Switch("--cast", 0);
    string leftCam = cli.Switch("--left", 0) == null ? cli.Switch("-l", 0) : cli.Switch("--left", 0);
    string rightCam = cli.Switch("--right", 0) == null ? cli.Switch("-r", 0) : cli.Switch("--right", 0);
    string name = cli.Switch("--name", 0) == null ? cli.Switch("-n", 0) : cli.Switch("--name", 0);
    string point = cli.Switch("--point", 0) == null ? cli.Switch("-p", 0) : cli.Switch("--point", 0);

    if (cli.Switch("-on") || cli.Switch("o") || cli.Switch("-off") || cli.Switch("O")) {
        bool on = cli.Switch("-on") || cli.Switch("o");
        SetCamRaycasting(raycast, on);
        SetCamRaycasting(leftCam, on);
        SetCamRaycasting(rightCam, on);

        return;
    }

    if (point != null) {
        IMyTerminalBlock block = GridTerminalSystem.GetBlockWithName(raycast);
        Vector3D vec = new Vector3D(block.GetPosition() + float.Parse(point, System.Globalization.CultureInfo.InvariantCulture) * block.WorldMatrix.Forward);
        Me.CustomData = GpsAt(vec, name ?? "point");

        return;
    }

    if (raycast != null) {
        IMyTerminalBlock block = GridTerminalSystem.GetBlockWithName(raycast);
        float dist = distance == null ? -1 : float.Parse(distance, System.Globalization.CultureInfo.InvariantCulture);
        Me.CustomData = GetRayPoint((IMyCameraBlock)block, dist);

        return;
    }

    if (leftCam != null) {
        IMyTerminalBlock block = GridTerminalSystem.GetBlockWithName(leftCam);
        Plane leftPlane = GetBlockPlane(block);
        WritePlaneToCustomData(leftPlane);

        return;
    }

    if (rightCam != null) {
        IMyTerminalBlock block = GridTerminalSystem.GetBlockWithName(rightCam);
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
