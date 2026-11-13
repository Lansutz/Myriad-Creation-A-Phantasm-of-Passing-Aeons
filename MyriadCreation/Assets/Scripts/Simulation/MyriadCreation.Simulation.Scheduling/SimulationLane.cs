using System;

namespace MyriadCreation.Simulation.Scheduling
{
[Flags]
public enum SimulationLane
{
	None = 0,
	Environment = 1,
	Civilization = 2,
	Both = Environment | Civilization
}
}
