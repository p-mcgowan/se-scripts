List<IMyTerminalBlock> list = new List<IMyTerminalBlock>();
void Main(string argument)  
{  
    Vector3D origin = new Vector3D(0, 0, 0);  
    if (argument == null || argument == "")  
    {   
        GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(list);
        var remote = list[0];
        origin = remote.GetPosition() + (500000*remote.WorldMatrix.Forward); 
        Me.TerminalRunArgument = origin.ToString();
        return;
    }  
    else
    {  
        Vector3D.TryParse(argument, out origin);  
    }  
    GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(list);  
    if (list.Count > 0)
    {  
        var remote = list[0] as IMyRemoteControl;
        remote.ClearWaypoints();  
        remote.AddWaypoint(origin, "Origin");  
        remote.SetAutoPilotEnabled(true);  
    }  
}