using System;
using System.Collections.Generic;

namespace MyriadCreation.Simulation.Laws
{
[Serializable]
public class ComponentChoice
{
	public int primary;

	public List<int> secondary = new List<int>();

	public ComponentChoice()
	{
	}

	public ComponentChoice(int primary, params int[] secondary)
	{
		this.primary = primary;
		if (secondary != null)
		{
			this.secondary.AddRange(secondary);
		}
	}

	public bool Contains(int component)
	{
		if (primary == component)
		{
			return true;
		}
		return secondary.Contains(component);
	}
}
}
