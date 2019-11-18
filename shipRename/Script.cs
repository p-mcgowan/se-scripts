/*
 * Replace or insert new ship name prefix to all blocks
 *
 * If oldName is not empty (""), replaces "<oldname> " (note following space) with "<newname> "
 */

string oldName = "oldname";
string newName = "newname";
// Don't replace anything with this tag
string ignore = "[dont-replace]";
// Only replace if oldName present
bool onlyReplaceOldName = false;

bool SubStr(string input, string substring) {
    return input.IndexOf(substring, StringComparison.CurrentCultureIgnoreCase) >= 0;
}

System.Text.RegularExpressions.Regex Regex(string r, System.Text.RegularExpressions.RegexOptions o = 0) {
    return new System.Text.RegularExpressions.Regex(r, o);
}

/*
void Main()
{
    for (int idx = 0; idx < GridTerminalSystem.Blocks.Count; idx++)
    {
        IMyTerminalBlock Block = GridTerminalSystem.Blocks[idx];

        Block.SetCustomName(String.Format("{0} ({1},{2},{3})",
            Block.DefinitionDisplayNameText,
            Block.Position.AxisValue(VRageMath.Base6Directions.Axis.LeftRight),
            Block.Position.AxisValue(VRageMath.Base6Directions.Axis.UpDown),
            Block.Position.AxisValue(VRageMath.Base6Directions.Axis.ForwardBackward)
        ));
        // IEnumerable<string> kvs =
        //     from n in blocks
        //     group n by n.DefinitionDisplayNameText.ToString() into nGroup
        //     where nGroup.Count() == 1
        //     select nGroup.Key;
    }
}
*/

public void RenameList(bool dryRun) {
    var blocks = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocks);
    Dictionary<string, int> nameToCount = new Dictionary<string, int>();

    foreach (var b in blocks) {
        int current;
        string blockName = b.DefinitionDisplayNameText.ToString();
        if (!nameToCount.TryGetValue(blockName, out current)) {
            current = 1;
        }
        var newBlockName = (newName == "" ? "" : newName + " ") + blockName + (current == 1 ? "" : " " + current.ToString());
        nameToCount.Remove(blockName);
        nameToCount.Add(blockName, ++current);
        Echo(b.CustomName.ToString() + " => " + newBlockName);
        if (dryRun) {
            continue;
        }
        b.CustomName = newBlockName;
    }
}

public void Main(string arg) {
    bool dryRun = !arg.Contains("run");
    if (!dryRun) {
        Echo("Changing names");
    } else {
        Echo("Dry run - use argument 'run' to change names");
    }
    if (arg.Contains("list")) {
        RenameList(dryRun);
        return;
    }

    var blocks = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocks);
    var oldNameReg = Regex(@"" + oldName + "[ ]*", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    foreach (var b in blocks) {
        if (!b.CustomName.Contains(ignore) && (onlyReplaceOldName ? b.CustomName.Contains(oldName) : true)) {
            string newNameString = b.CustomName;
            if (oldName.Length > 0) {
                newNameString = oldNameReg.Replace(b.CustomName + " ", "");
            }
            if (!SubStr(newNameString, newName)) {
                if (newName != "") {
                    newNameString = String.Format("{0} {1}", newName, newNameString);
                }
            }
            if (newNameString != b.CustomName) {
                Echo(b.CustomName);
                Echo("  =>" + newNameString + '\n');
                if (dryRun) {
                    continue;
                }
                b.CustomName = newNameString;
            }
        }
    }
}
