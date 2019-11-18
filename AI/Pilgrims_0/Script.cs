//With this modification, it saves the end point so it can be modified every time instead of generated anew 
        private Vector3D fleeTo; 
 
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
                    if (fleeTo == null) //If there's no direction/end point picked yet..; 
                    { 
                        oppositedirection = remote.GetPosition(); 
                        oppositedirection = oppositedirection + oppositedirection - player; 
                        remote.AddWaypoint(oppositedirection, "FleeDirection"); 
                        fleeTo = oppositedirection; 
                    } 
                    else 
                    { 
                        fleeTo += remote.WorldMatrix.Forward * Vector3D.Distance(remote.GetPosition(), player); 
                        remote.AddWaypoint(fleeTo, "FleeDirection"); 
                    } 
                    remote.SetAutoPilotEnabled(true); 
                } 
 
            } 
        }