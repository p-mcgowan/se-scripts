void Main(string argument)  
{  
    var list = new List<IMyTerminalBlock>();  
    GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(list);  
    if (list.Count > 0)  
    {  
        var remote = list[0] as IMyRemoteControl;  
        remote.ClearWaypoints();  
        Vector3D player = new Vector3D(0, 0, 0);  
        	Vector3D oppositedirection = new Vector3D(0, 0, 0);  
  
        bool success = remote.GetNearestPlayer(out player);  
  
        if (success)  
        {  
	            oppositedirection = remote.GetPosition ();  
	            oppositedirection = oppositedirection + oppositedirection - player;  
	            remote.AddWaypoint(oppositedirection, "FleeDirection");  
            remote.SetAutoPilotEnabled(true);  
        }  
    }  
} 
