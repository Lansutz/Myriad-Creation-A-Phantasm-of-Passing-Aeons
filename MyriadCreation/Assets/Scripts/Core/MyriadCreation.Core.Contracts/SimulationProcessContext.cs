namespace MyriadCreation.Core.Contracts
{
public readonly struct SimulationProcessContext(int day, float deltaDays, object activity = null, object worldSnapshot = null)
{
	public readonly int Day = day;

	public readonly float DeltaDays = deltaDays;

	public readonly object Activity = activity;

	public readonly object WorldSnapshot = worldSnapshot;
}
}
