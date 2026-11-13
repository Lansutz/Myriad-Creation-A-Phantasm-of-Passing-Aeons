using System.Collections.Generic;

namespace MyriadCreation.Simulation.State
{
public class ChronicleEntry
{
	public int entryId;

	public int tick;

	public int year;

	public string eventType;

	public string description;

	public bool major;

	public List<int> participants = new List<int>();
}
}
