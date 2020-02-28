//Customization Options
string cameraName = "[Lidar]";
string laserStatusLCDName = "[Lidar]";
string gpsLCDName = "[Lidar]";
bool writeGPSToLCD = false;
bool saveGPS = false;

// *** User Customization ***
List<string> reservedNamesStatic = new List<string> { "[LCD]", "[ShipStatus]", "[EmergencyThrust]", "[SunChaser]", "[GravDrive]", "[OrbitThruster]", "" };//Names that are [name] but not airzones
MyDetectedEntityInfo info = new MyDetectedEntityInfo();
WriteLCD laserStatusLCD;
WriteLCD gpsLCD;
List<IMyCameraBlock> cameras = new List<IMyCameraBlock>();
List<String> gpsCoordinates = new List<String>();
List<MyDetectedEntityInfo> scannedItems = new List<MyDetectedEntityInfo>();
//Helpers
string[] argMessages = new string[10];
string[] pieces = new string[2];
//string echoString = "";
List<IMyTextSurface> pbText = new List<IMyTextSurface>();


public Program() {
    // The constructor, called only once every session and
    // always before any other method is called. Use it to
    // initialize your script.
    //
    // The constructor is optional and can be removed if not
    // needed.
    ArgumentParser(Me.CustomData);
    laserStatusLCD = new WriteLCD(this, laserStatusLCDName);
    gpsLCD = new WriteLCD(this, gpsLCDName);
    if (Storage.Length > 0) {
        ArgumentParser(Storage);
    }
    BuildLists();
    if (Me.SurfaceCount > 0) pbText.Add(Me.GetSurface(0));
    foreach (IMyTextSurface de in pbText) de.ContentType = ContentType.TEXT_AND_IMAGE;
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
    //do a first pass
    //empty old lists just in case
}

public void Save() {
    // Called when the program needs to save its state. Use
    // this method to save your state to the Storage field
    // or some other means.
    //
    // This method is optional and can be removed if not
    // needed.
    StringBuilder tempstring = new StringBuilder();
    Storage = "";
    foreach (string de in gpsCoordinates) tempstring.Append($"{de}\n");
    Storage = tempstring.ToString();
}

public void Main(string argument, UpdateType updateSource) {

    //Initilization of variables
    // Arguments = "", lase, turnon, turnoff, cleangps, rebuild and
    //null checker
    for (int i = 0; i < cameras.Count; ++i) {
        if (Closed(cameras[i])) {
            BuildLists();
            break;
        }
    }
    if (argument.Length > 0) {
        ArgumentParser(argument);
    }

    if ((updateSource & UpdateType.Update100) == 0) { return; }
    //in update100
    //Clean LCDs off
    //Echo(echoString);

    //WriteToLCD(Storage);
    //Actions
    laserStatusLCD.WriteToLCD("   O.I.S. Lidar RangeFinder");
    laserStatusLCD.WriteToLCD($"\n Camera: {cameraName}");
    if (cameras.Count == 0) laserStatusLCD.WriteToLCD("\n No Camera found");
    else { laserStatusLCD.WriteToLCD($"\n Laser Range: {cameras[0].AvailableScanRange} m"); }
    if (info.IsEmpty()) {
        laserStatusLCD.WriteToLCD("\n No Objects Found");
    } else {
        laserStatusLCD.WriteToLCD($"\n Name: {info.Name}");
        laserStatusLCD.WriteToLCD($"\n Relationship: {info.Relationship}");
        if (info.HitPosition.HasValue && cameras.Count > 0) {
            var distance = Vector3D.Distance(cameras[0].GetPosition(),
                info.HitPosition.Value);
            laserStatusLCD.WriteToLCD($"\n Distance: {distance:n2} m");
            var speed = VelocityEta(FindVelocity(), info.HitPosition.Value);
            laserStatusLCD.WriteToLCD($"\n ETA: {(distance / speed):n2} s");
        }
        laserStatusLCD.WriteToLCD($"\n Position: \n{info.Position:n3}");
        laserStatusLCD.WriteToLCD($"\n Velocity: {info.Velocity:n3}");
        laserStatusLCD.WriteToLCD($"\n Type: {info.Type}");
        if (!saveGPS) {
            gpsLCD.WriteToLCD($" GPS:{info.Name}:{info.Position.X:0.00}:{info.Position.Y:0.00}:{info.Position.Z:0.00}:\n");
        }
        if (saveGPS) {
            for (int i = 0; i < gpsCoordinates.Count; ++i) {
                gpsLCD.WriteToLCD($" {gpsCoordinates[i]}\n");
            }
        }
    }
    laserStatusLCD.WriteToLCD("\n");
    //echoString = $"Orbital Flight Computer\n{laserStatusLCD.ToString()}";
    foreach (IMyTextSurface de in pbText) {
        de.WriteText($"Orbital Flight Computer\n{laserStatusLCD.ToString()}");
    }

    laserStatusLCD.FlushToLCD();
    if (writeGPSToLCD) {
        //echoString += $"\n{gpsLCD.ToString()}";
        gpsLCD.FlushToLCD();
    }
}//main

void ArgumentParser(string argument) {
    argMessages = argument.Split('\n');
    foreach (string de in argMessages) {
        //gps
        if (de.StartsWith("GPS:")) {
            gpsCoordinates.Add(de);
            continue;
        }
        pieces = de.Split(' ');
        if (pieces.Count() < 1) continue;
        switch (pieces[0]){
            case "lase":
                MyDetectedEntityInfo tempInfo = new MyDetectedEntityInfo();
                foreach (var fr in cameras) {
                    if (fr.EnableRaycast) {
                        tempInfo = fr.Raycast(fr.AvailableScanRange, 0, 0);
                        if (!tempInfo.IsEmpty() && !tempInfo.EntityId.Equals(Me.EntityId)) {
                            info = tempInfo;
                        }
                    }
                }
                if (saveGPS) {
                    gpsCoordinates.Add($"GPS:{info.Name}:{info.Position.X:0.00}:{info.Position.Y:0.00}:{info.Position.Z:0.00}:");
                    scannedItems.Add(info);
                }
                break;
            case "turnon":
                foreach (var fr in cameras) fr.EnableRaycast = true;
                break;
            case "turnoff":
                foreach (var fr in cameras) fr.EnableRaycast = false;
                break;
            case "cleangps":
                gpsLCD.CleanLCD();
                gpsCoordinates.Clear();
                scannedItems.Clear();
                break;
            case "rebuild":
                BuildLists();
                break;
            case "writeGPSon":
                writeGPSToLCD = true;
                break;
            case "writeGPSoff":
                writeGPSToLCD = false;
                break;
            case "saveGPSon":
                saveGPS = true;
                break;
            case "saveGPSoff":
                saveGPS = false;
                break;
        }
        if (pieces.Count() < 2) continue;
        switch (pieces[0]) {
            case "cameraName":
                cameraName = de.Substring("cameraName ".Length);
                break;
            case "laserLCDName":
                laserStatusLCDName = de.Substring("laserLCDName".Length);
                break;
            case "gpsLCDName":
                gpsLCDName = de.Substring("gpsLCDName ".Length);
                break;
        }
    }
}

void BuildLists() {
    cameras.Clear();
    GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>( cameras, b => {
        if (b.CustomName.Contains(cameraName) ) return true;
        return false;
    });
    laserStatusLCD.LCDBuild(laserStatusLCDName);
    gpsLCD.LCDBuild(gpsLCDName);
}

public bool Closed(IMyTerminalBlock block) {
    if (block == null || block.WorldMatrix == MatrixD.Identity) return true;
    return !(GridTerminalSystem.GetBlockWithId(block.EntityId) == block);
    //return (Vector3D.IsZero(block.WorldMatrix.Translation));
    //return false;
}

MyShipVelocities FindVelocity() {
    var shipControl = new List<IMyShipController>();
    GridTerminalSystem.GetBlocksOfType(shipControl);
    if (shipControl.Count < 1) { return new MyShipVelocities(); }
    return shipControl[0].GetShipVelocities();
}

double LidarRange(List<IMyCameraBlock> cameras, float pitch, float yaw) {
    double range = 9999999;
    double range2 = 9999999;
    MyDetectedEntityInfo info;
    for (int i = 0; i < cameras.Count; ++i) {
        if (cameras[i].EnableRaycast) {
            info = cameras[i].Raycast(cameras[i].AvailableScanRange, pitch, yaw);
            if (info.HitPosition.HasValue) {
                range2 = Vector3D.Distance(cameras[i].GetPosition(), info.HitPosition.Value);
            }
            if (range2 < range) range = range2;
        }
    }
    return range;
}

double VelocityEta(MyShipVelocities shipV, Vector3D toObject) {
    Vector3D rate2 = VectorProjection(shipV.LinearVelocity, toObject);
    int sign = Math.Sign(toObject.Dot(shipV.LinearVelocity));
    return sign * rate2.Length();
}

Vector3D VectorProjection(Vector3D a, Vector3D b) {//project a onto b
    Vector3D projection = a.Dot(b) / b.LengthSquared() * b;
    return projection;
}

//Whip's Profiler Graph Code
int count = 1;
int maxSeconds = 60;
StringBuilder profile = new StringBuilder();
void ProfilerGraph() {
    if (count <= maxSeconds * 1) {
        double timeToRunCode = Runtime.LastRunTimeMs;

        profile.Append(timeToRunCode.ToString()).Append("\n");
        count++;
    } else {
        var screen = GridTerminalSystem.GetBlockWithName("DEBUG") as IMyTextSurface;
        screen?.WriteText(profile.ToString());
        //screen?.ContentType = ContentType.TEXT_AND_IMAGE;
    }
}
