IMyThrust thrust;
IMyShipController brakes;

public Program() {
  thrust = (IMyThrust)GridTerminalSystem.GetBlockWithName("Sci-Fi Atmospheric Thruster");
  brakes = (IMyShipController)GridTerminalSystem.GetBlockWithName("Buggy Cockpit");
}

public void Main() {
  thrust.Enabled = !thrust.Enabled;
  brakes.HandBrake = !brakes.HandBrake;
}
