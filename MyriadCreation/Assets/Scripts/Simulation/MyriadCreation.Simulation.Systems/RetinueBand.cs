using System;
using System.Collections.Generic;

namespace MyriadCreation.Simulation.Systems
{
[Serializable]
public class RetinueBand
{
	public int bandId;

	public int leaderId;

	public List<int> retainerIds = new List<int>();

	public int size;

	public float loyalty = 80f;

	public float prestige;

	public int equipmentLevel = 1;

	public int homeTile = -1;

	public bool isActive;
}
}
