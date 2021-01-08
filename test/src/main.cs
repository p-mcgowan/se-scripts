IMyShipConnector connector;

public Program() {
  connector = (IMyShipConnector)GridTerminalSystem.GetBlockWithName("Connector");
}

public string GpsAt(Vector3D point, string name = ".", string colour = "") {
    return "GPS:" + name + ":" +
        point.X.ToString() + ":" +
        point.Y.ToString() + ":" +
        point.Z.ToString() + ":" + colour + ":";
}

public void Main() {
    // Echo($"{connector.Orientation}");
    // Echo($"{connector.Position}");
    Vector3D pos = connector.GetPosition();
    Vector3D vec = new Vector3D(connector.GetPosition() + 100 * connector.WorldMatrix.Forward);
    Me.GetSurface(0).WriteText($"pos: {pos}\n{GpsAt(pos, "conn")}\nvec: {vec}\n{GpsAt(vec, "approach")}");
    // get block coords and facing
}
