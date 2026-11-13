using System.Collections.Generic;

namespace MyriadCreation.Simulation.Systems
{
public class AdminDivision
{
	public int divisionId;

	public int realmId;

	public int level;

	public string name = "";

	public int parentDivisionId = -1;

	public List<int> childIds = new List<int>();

	public HashSet<int> tiles = new HashSet<int>();

	public string titleId = "";

	public int holderCharacterId = -1;
}
}
