namespace MyriadCreation.Simulation.Scheduling
{
public readonly struct SimulationTickContext(int day, int year, int deltaDays)
{
	public readonly int day = day;

	public readonly int year = year;

	public readonly int deltaDays = deltaDays;
}
}
