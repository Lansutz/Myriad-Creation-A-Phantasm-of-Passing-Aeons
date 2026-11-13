using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.State;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
public static class ResourceAccess
{
	public const string MiningShaftCapability = "mining.shaft";

	public const string MiningDeepCapability = "mining.deep";

	public static string DescribeDepth(ResourceAccessDepth depth)
	{
		return depth switch
		{
			ResourceAccessDepth.Surface => "露天", 
			ResourceAccessDepth.Shaft => "坑采", 
			ResourceAccessDepth.Deep => "深井", 
			_ => "未知", 
		};
	}

	public static bool IsDepthAccessible(ResourceAccessDepth depth, HashSet<string> capabilities)
	{
		if (depth == ResourceAccessDepth.Surface)
		{
			return true;
		}
		if (capabilities == null)
		{
			return false;
		}
		if (depth != ResourceAccessDepth.Shaft)
		{
			return capabilities.Contains("mining.deep");
		}
		return capabilities.Contains("mining.shaft");
	}

	public static bool IsDepthAccessible(ResourceAccessDepth depth, InnovationTree innovations, int realmId)
	{
		return IsDepthAccessible(depth, innovations?.GetRealmCapabilities(realmId));
	}

	public static bool IsTechAccessible(in GoodsDef goods, InnovationTree innovations, int realmId)
	{
		if (goods.requiredInnovation >= 0)
		{
			return innovations?.HasInnovation(realmId, goods.requiredInnovation) ?? false;
		}
		return true;
	}

	public static bool CanDevelop(in GoodsDef goods, in TileResource resource, InnovationTree innovations, int realmId)
	{
		if (!resource.discovered)
		{
			return false;
		}
		if (!IsTechAccessible(in goods, innovations, realmId))
		{
			return false;
		}
		return IsDepthAccessible(resource.accessDepth, innovations, realmId);
	}

	public static string DescribeStatus(in GoodsDef goods, in TileResource resource, InnovationTree innovations, int realmId)
	{
		if (!resource.discovered)
		{
			return "未发现";
		}
		if (!IsTechAccessible(in goods, innovations, realmId))
		{
			return "存在但需技术";
		}
		if (!IsDepthAccessible(resource.accessDepth, innovations, realmId))
		{
			return "存在但不可采（需" + DescribeDepth(resource.accessDepth) + "）";
		}
		if (!resource.developed)
		{
			return DescribeDepth(resource.accessDepth) + "·可采";
		}
		return "已开发";
	}
}
}
