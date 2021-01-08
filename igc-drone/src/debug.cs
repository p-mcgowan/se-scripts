StringBuilder logs = new StringBuilder("");

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

public Vector3D OldGetOffsetFromRc(IMyTerminalBlock block) {
    IMyShipMergeBlock mergeBlock = (IMyShipMergeBlock)GetBlock(mergeBlockId);
    IMyShipConnector connector = (IMyShipConnector)GetBlock(connectorId);
    IMyRemoteControl remoteControl = (IMyRemoteControl)GetBlock(remoteControlId);

    if (block != null && remoteControl != null) {
        return remoteControl.GetPosition() - block.GetPosition();
        // Vector3D back = block is IMyShipConnector ? block.WorldMatrix.Backward : block.WorldMatrix.Left;
        // Vector3D toRc = remoteControl.GetPosition() - block.GetPosition();

        // var angle = Math.Acos(Vector3D.Dot(back, Vector3D.Normalize(toRc)));
        // var axis = back.Cross(toRc);
        // var matTransform = MatrixD.CreateFromAxisAngle(axis, (float)angle);
        // var vecTransformed = Vector3D.Transform(targetPoint, matTransform);

        // Log(angle.ToString());
        // Log(axis.ToString());
        // Log(matTransform.ToString());
        // Log(vecTransformed.ToString());

        // return vecTransformed;
    } else {
        return Vector3D.Zero;
    }
}
