void Main(string argument) 
{ 
    var list = new List<IMyTerminalBlock>(); 
    GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(list); 
    if (list.Count > 0) 
    { 
        var remote = list[0] as IMyRemoteControl; 
        remote.ClearWaypoints(); 
        Vector3D player = new Vector3D(0, 0, 0); 
        Vector3D mindistance = new Vector3D(0,0,5000);
        Vector3D currentposition = new Vector3D(0, 0, 0); 
        bool success = remote.GetNearestPlayer(out player); 
        if (success) 
        { 
            currentposition = remote.GetPosition();
            if (Vector3D.DistanceSquared(player, currentposition) < 4000 * 4000)   
            {   
                return;   
            }  
            else
            {
            player = player - mindistance;
            remote.AddWaypoint(player, "Player"); 
            remote.SetAutoPilotEnabled(true);
            }
        } 
    } 
}