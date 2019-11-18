List<IMyTerminalBlock> list = new List<IMyTerminalBlock>();  
void Main(string argument)  
{  
    Vector3D origin = new Vector3D(0, 0, 0);  
    if (argument == null || argument == "")  
    {  
        origin = Me.GetPosition();  
        this.Storage = origin.ToString();  
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
        Vector3D player = new Vector3D(0, 0, 0);  
        bool success = remote.GetNearestPlayer(out player);  
        if (success)  
        {  
            bool gotoOrigin = false;  
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(list);  
            if (list.Count == 0)  
            {  
                gotoOrigin = true;  
            }  
            else  
            {  
                bool hasUsableGun = false;  
                for (int i = 0; i < list.Count; ++i)  
                {  
                    var weapon = list[i];  
                    if (!weapon.IsFunctional) continue;  
                    if (weapon.HasInventory() && !weapon.GetInventory(0).IsItemAt(0)) continue;  
                    hasUsableGun = true;  
                }  
                if (!hasUsableGun)  
                {  
                    gotoOrigin = true;  
                }  
            }  
            if (Vector3D.DistanceSquared(player, origin) > 20000 * 20000)  
            {  
                gotoOrigin = true;  
            }  
            if (gotoOrigin)  
            {  
                remote.AddWaypoint(origin, "Origin");  
            }  
            else  
            {  
                remote.AddWaypoint(player, "Player");  
            }  
            remote.SetAutoPilotEnabled(true);  
        }  
    }  
}