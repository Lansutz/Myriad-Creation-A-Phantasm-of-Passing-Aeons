using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.State
{
public static class InnovationDiscovery
{
	public static bool CanGroupDiscover(string group, InnovationDef def)
	{
		if (def?.discoveryGroups == null || string.IsNullOrEmpty(group))
		{
			return false;
		}
		foreach (string discoveryGroup in def.discoveryGroups)
		{
			if (discoveryGroup == group)
			{
				return true;
			}
		}
		return false;
	}

	public static bool TryDiscover(InnovationTree tree, int realmId, int innovationId, string group)
	{
		if (tree == null)
		{
			return false;
		}
		InnovationDef innovation = tree.GetInnovation(innovationId);
		if (!CanGroupDiscover(group, innovation))
		{
			return false;
		}
		return tree.Discover(realmId, innovationId);
	}
}
}
