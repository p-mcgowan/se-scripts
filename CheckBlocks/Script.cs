float GetHealth(IMyTerminalBlock block) {
    IMySlimBlock slimblock = block.CubeGrid.GetCubeBlock(block.Position);
    float MaxIntegrity = slimblock.MaxIntegrity;
    float BuildIntegrity = slimblock.BuildIntegrity;
    float CurrentDamage = slimblock.CurrentDamage;
    return (BuildIntegrity - CurrentDamage) / MaxIntegrity;
}

public void Main(string argument, UpdateType updateSource) {
    var blocks = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocks);
    foreach (var b in blocks) {
        var health = GetHealth(b);
        if (health != 1f) {
            Echo(String.Format("{0}: {1}", b.CustomName, GetHealth(b)));
            b.ShowOnHUD = true;
        } else {
            b.ShowOnHUD = false;
        }
    }
}
