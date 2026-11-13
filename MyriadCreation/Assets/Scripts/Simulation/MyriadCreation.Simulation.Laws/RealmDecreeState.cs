using System;
using System.Collections.Generic;

namespace MyriadCreation.Simulation.Laws
{
[Serializable]
public class RealmDecreeState
{
	public List<ActiveDecree> active = new List<ActiveDecree>();

	public List<DecreeCooldown> cooldowns = new List<DecreeCooldown>();

	public List<DecreeOrder> orders = new List<DecreeOrder>();
}
}
