public void Main(string argument, UpdateType updateSource) {
    var pads = new List<IMyLandingGear>();
    GridTerminalSystem.GetBlocksOfType<IMyLandingGear>(pads);
    foreach (var g in pads) {
        if (g.IsLocked) {
            var r = new System.Text.RegularExpressions.Regex("Landing Gear (.*)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var group = r.Match(g.CustomName).Groups[1];
            if (group.Value.Length != 0) {
                var con = GridTerminalSystem.GetBlockWithName("Connector " + group.Value) as IMyShipConnector;
                if (con != null) {
                    con.Disconnect();
                    g.Unlock();
                    return;
                } else { g.Unlock(); }
            }
        }
     }
    Echo("Something went wrong....");
}
