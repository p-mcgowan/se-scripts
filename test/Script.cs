class Thing {
    public MyDefinitionId electricity=new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties),"Electricity");
    public MyDefinitionId ȸ=new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties),"Oxygen");
    public MyDefinitionId ȵ=new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties),"Hydrogen");

    int Ɇ=0;

    public bool Ʌ(List<IMyTerminalBlock>ł,ref double Ⱦ,ref double Ƚ,ref double ȼ,ref double Ȼ,ref double Ʉ,ref double Ƀ, bool ð){
        if(!ð)Ɇ=0;
        MyResourceSinkComponent Ȟ;
        MyResourceSourceComponent ȴ;
        for(;Ɇ<ł.Count;Ɇ++){
            if(ł[Ɇ].Components.TryGet<MyResourceSinkComponent>(out Ȟ)){
                Ⱦ+=Ȟ.CurrentInputByType(electricity);
                Ƚ+=Ȟ.MaxRequiredInputByType(electricity);
            }if(ł[Ɇ].Components.TryGet<MyResourceSourceComponent>(out ȴ)){
                ȼ+=ȴ.CurrentOutputByType(electricity);
                Ȼ+=ȴ.MaxOutputByType(electricity);
            }
            IMyBatteryBlock ɂ=(ł[Ɇ] as IMyBatteryBlock);
            Ʉ+=ɂ.CurrentStoredPower;
            Ƀ+=ɂ.MaxStoredPower;
        }
        return true;
    }

    int Ɂ=0;
    public bool ɀ(List<IMyTerminalBlock>ł,MyDefinitionId ȿ,ref double Ⱦ,ref double Ƚ,ref double ȼ,ref double Ȼ,bool ð){
        if(!ð)Ɂ=0;
        MyResourceSinkComponent Ȟ;
        MyResourceSourceComponent ȴ;
        for(;Ɂ<ł.Count;Ɂ++){
            if(ł[Ɂ].Components.TryGet<MyResourceSinkComponent>(out Ȟ)){
                Ⱦ+=Ȟ.CurrentInputByType(ȿ);
                Ƚ+=Ȟ.MaxRequiredInputByType(ȿ);
            }
            if(ł[Ɂ].Components.TryGet<MyResourceSourceComponent>(out ȴ)){
                ȼ+=ȴ.CurrentOutputByType(ȿ);
                Ȼ+=ȴ.MaxOutputByType(ȿ);
            }
        }
        return true;
    }

    int ȣ=0;
    public bool ȑ(List<IMyTerminalBlock>ł,string ȡ,ref double Ƞ,ref double ȟ,bool ð){
        if(!ð){ȣ=0;ȟ=0;Ƞ=0;}
        MyResourceSinkComponent Ȟ;
        for(;ȣ<ł.Count;ȣ++){
            IMyGasTank Ň=ł[ȣ] as IMyGasTank;
            if(Ň==null)continue;
            double ȝ=0;
            if(Ň.Components.TryGet<MyResourceSinkComponent>(out Ȟ)){
                ListReader<MyDefinitionId>Ȝ=Ȟ.AcceptedResources;
                int Y=0;
                for(;Y<Ȝ.Count;Y++){
                    if(string.Compare(Ȝ[Y].SubtypeId.ToString(),ȡ,true)==0){
                        ȝ=Ň.Capacity;
                        ȟ+=ȝ;Ƞ+=ȝ*Ň.FilledRatio;break;
                    }
                }
            }
        }
        return true;
    }
}

public MyDefinitionId electricity = new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties), "Electricity");
public MyDefinitionId oxygen = new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties), "Oxygen");
public MyDefinitionId hydrogen = new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties), "Hydrogen");
MyResourceSinkComponent resourceSink;
Dictionary<string, float> myDict = new Dictionary<string, float>();



public void Main() {
    List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();

    // get blocks
    MyResourceSinkComponent resourceSink;
    MyResourceSourceComponent resourceSource;

    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocks);


    Dictionary<string, float> myDict = new Dictionary<string, float>();

    int i = 0;
    foreach (var block in blocks) {
        if (block.Components == null) {
            continue;
        }
        if (block.Components.TryGet<MyResourceSinkComponent>(out resourceSink)) {
            myDict.Add($"{block.CustomName} {i++}", resourceSink.CurrentInputByType(electricity));
            // Echo($"{block.CustomName}");
            // Echo($"  CurrentInputByType: {resourceSink.CurrentInputByType(electricity)}");
            // Echo($"  MaxRequiredInputByType: {resourceSink.MaxRequiredInputByType(electricity)}");
        }
        // if (block.Components.TryGet<MyResourceSourceComponent>(out resourceSource)) {
        //     Echo("  sources:");
        //     Echo($"  DefinedOutputByType: {resourceSource.DefinedOutputByType(electricity)}");
        //     Echo($"  CurrentOutputByType: {resourceSource.CurrentOutputByType(electricity)}");
        //     Echo($"  MaxOutputByType: {resourceSource.MaxOutputByType(electricity)}");
        // } else {
        //     Echo("  No source");
        // }
    }
    // myDict = from entry in myDict orderby entry.Value descending select entry;
    myDict = myDict.OrderBy(x => -x.Value).ToDictionary(x => x.Key, x => x.Value);

    foreach (var item in myDict) {
        Echo($"{item.Key}: {item.Value}");
    }
}
