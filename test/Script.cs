public void Main(string argument) {
    string[] countWhat = argument.Split(' ');

    double count;
    if (!double.TryParse(countWhat[0], out count)) {
        Echo($"invalid count - use eg. '3 veg'");

        return;
    }

    MyDefinitionId? what = null;

    switch (countWhat[1]) {
        case "veg":
            what = MyDefinitionId.Parse("MyObjectBuilder_BlueprintDefinition/Position0030_Seeds_Vegetables");
        break;

        case "grain":
            what = MyDefinitionId.Parse("MyObjectBuilder_BlueprintDefinition/Position0020_Seeds_Grain");
        break;

        case "fruit":
            what = MyDefinitionId.Parse("MyObjectBuilder_BlueprintDefinition/Position0010_Seeds_Fruit");
        break;

        case "mush":
            what = MyDefinitionId.Parse("MyObjectBuilder_BlueprintDefinition/Position0040_Spores_Mushrooms");
        break;
    }

    if (what == null) {
        Echo($"invalid req - must be 'veg', 'grain', 'fruit', or 'mush'");

        return;
    }

    IMyProductionBlock processor = (IMyProductionBlock)GridTerminalSystem.GetBlockWithName("Food Processor");
    processor.AddQueueItem((MyDefinitionId)what, count);
}
