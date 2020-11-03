public string GpsAt(Vector3D point, string name = ".", string colour = "") {
    if (point == null) {
        return "";
    }

    return "GPS:" + name + ":" +
        point.X.ToString() + ":" +
        point.Y.ToString() + ":" +
        point.Z.ToString() + ":" + colour + ":";
}

public Plane GetBlockPlane(IMyTerminalBlock block) {
    if (block == null) {
        return new Plane(0,0,0,0);
    }

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
    Echo(block.AvailableScanRange.ToString());
    MyDetectedEntityInfo info = block.Raycast(distance == -1 ? block.AvailableScanRange : distance, 0, 0);
    Vector3D? point = info.HitPosition;
    Echo(info.HitPosition.ToString());
    if (point.HasValue) {
        return GpsAt((Vector3D)point, "cast");
    }
    return "";
}

public void SetCamRaycasting(string blockName, bool on) {
    if (blockName == "" || blockName == null) {
        return;
    }

    IMyCameraBlock block = (IMyCameraBlock)GridTerminalSystem.GetBlockWithName(blockName);
    if (block != null) {
        block.EnableRaycast = on;
    }
}

public void Main(string argument, UpdateType updateSource) {
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

        return;
    }

    string distance = cli.Switch("--distance", 0) == null ? cli.Switch("-d", 0) : cli.Switch("--distance", 0);
    string raycast = cli.Switch("--cast", 0) == null ? cli.Switch("-c", 0) : cli.Switch("--cast", 0);
    string leftCam = cli.Switch("--left", 0) == null ? cli.Switch("-l", 0) : cli.Switch("--left", 0);
    string rightCam = cli.Switch("--right", 0) == null ? cli.Switch("-r", 0) : cli.Switch("--right", 0);

    if (cli.Switch("-on") || cli.Switch("o") || cli.Switch("-off") || cli.Switch("O")) {
        bool on = cli.Switch("-on") || cli.Switch("o");
        SetCamRaycasting(raycast, on);
        SetCamRaycasting(leftCam, on);
        SetCamRaycasting(rightCam, on);

        return;
    }

    if (raycast != null) {
        IMyTerminalBlock block = GridTerminalSystem.GetBlockWithName(raycast);
        if (block == null) {
            Echo("Did not find cam: " + raycast);
            return;
        }
        float dist = distance == null ? -1 : float.Parse(distance, System.Globalization.CultureInfo.InvariantCulture);
        Me.CustomData = GetRayPoint((IMyCameraBlock)block, dist);
    }

    if (leftCam != null) {
        IMyTerminalBlock block = GridTerminalSystem.GetBlockWithName(leftCam);
        if (block == null) {
            Echo("Did not find cam: " + leftCam);
            return;
        }
        Plane leftPlane = GetBlockPlane(block);
        WritePlaneToCustomData(leftPlane);
    }

    if (rightCam != null) {
        IMyTerminalBlock block = GridTerminalSystem.GetBlockWithName(rightCam);
        if (block == null) {
            Echo("Did not find cam: " + rightCam);
            return;
        }
        Ray ray = new Ray(block.GetPosition(), block.WorldMatrix.Forward);
        Plane leftCamPlane = ReadPlaneFromCustomData();
        float? intersectionDistance = ray.Intersects(leftCamPlane);

        if (intersectionDistance != null) {
            Vector3D point = new Vector3D(block.GetPosition() + (float)intersectionDistance * block.WorldMatrix.Forward);
            Me.CustomData = GpsAt(point, "target");
        }
    }
}
