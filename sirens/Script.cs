List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
IMyBlockGroup right;
IMyBlockGroup left;

public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.None;
    right = GridTerminalSystem.GetBlockGroupWithName("Right");
    left = GridTerminalSystem.GetBlockGroupWithName("Left");
    GridTerminalSystem.GetBlocksOfType<IMyTextPanel, IMyReflectorLight>(blocks);
}

public void Main(string argument, UpdateType updateSource) {
  if (argument == "toggle") {
    if (Runtime.UpdateFrequency == UpdateFrequency.Update10) {
      Runtime.UpdateFrequency = UpdateFrequency.None;
      blocks.Clear();
      left.GetBlocks(blocks);
      foreach (IMyReflectorLight block in blocks) {
          block.Color = Color.White;
      }
      blocks.Clear();
      right.GetBlocks(blocks);
      foreach (IMyReflectorLight block in blocks) {
          block.Color = Color.White;
      }
    } else {
      Runtime.UpdateFrequency = UpdateFrequency.Update10;
      blocks.Clear();
      left.GetBlocks(blocks);
      foreach (IMyReflectorLight block in blocks) {
          block.Color = Color.Red;
      }
      blocks.Clear();
      right.GetBlocks(blocks);
      foreach (IMyReflectorLight block in blocks) {
          block.Color = Color.Blue;
      }
    }
    return;
  }

  blocks.Clear();
  left.GetBlocks(blocks);
  foreach (IMyReflectorLight block in blocks) {
    block.Color = block.Color == Color.Blue ? Color.Red : Color.Blue;
  }
  blocks.Clear();
  right.GetBlocks(blocks);
  foreach (IMyReflectorLight block in blocks) {
    block.Color = block.Color == Color.Blue ? Color.Red : Color.Blue;
  }
}
