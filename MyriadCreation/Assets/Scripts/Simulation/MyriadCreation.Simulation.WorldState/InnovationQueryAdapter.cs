using System;
using System.Collections.Generic;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Data;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.State;

namespace MyriadCreation.Simulation.WorldState
{
/// <summary>
/// UI 4b3：IInnovationQuery 的 Simulation host 适配器。
/// 转调 GameWorld.GetInnovationTree() 的已研究集合/名称；空世界返回 0/空/空串。
/// </summary>
public sealed class InnovationQueryAdapter : IInnovationQuery
{
	private readonly GameWorld _world;

	public InnovationQueryAdapter(GameWorld world)
	{
		_world = world;
	}

	public int GetResearchedCount(int realmId)
	{
		return _world?.GetInnovationTree()?.GetRealmInnovationCount(realmId) ?? 0;
	}

	public IReadOnlyList<int> GetResearchedInnovationIds(int realmId)
	{
		HashSet<int> ids = _world?.GetInnovationTree()?.GetRealmInnovations(realmId);
		if (ids == null)
		{
			return Array.Empty<int>();
		}
		List<int> list = new List<int>(ids);
		list.Sort();
		return list;
	}

	public InnovationSnapshotDto GetInnovationSnapshot(int realmId)
	{
		IReadOnlyList<int> ids = GetResearchedInnovationIds(realmId);
		return new InnovationSnapshotDto
		{
			realmId = realmId,
			researchedCount = ids.Count,
			researchedInnovationIds = ids
		};
	}

	public IReadOnlyList<InnovationAvailabilityDto> GetAvailableInnovations(int realmId)
	{
		List<InnovationAvailabilityDto> list = new List<InnovationAvailabilityDto>();
		InnovationTree tree = _world?.GetInnovationTree();
		if (tree == null)
		{
			return list;
		}
		List<InnovationDef> available = tree.GetAvailableInnovations(realmId, _world);
		if (available != null)
		{
			foreach (InnovationDef def in available)
			{
				if (def == null)
				{
					continue;
				}
				list.Add(new InnovationAvailabilityDto
				{
					innovationId = def.innovationId,
					name = def.GetName() ?? "",
					researched = tree.HasInnovation(realmId, def.innovationId),
					canResearch = true,
					prerequisiteIds = GetPrerequisiteInnovationIds(def.innovationId)
				});
			}
		}
		list.Sort((a, b) => a.innovationId.CompareTo(b.innovationId));
		return list;
	}

	public IReadOnlyList<int> GetPrerequisiteInnovationIds(int innovationId)
	{
		List<int> ids = new List<int>();
		InnovationTree tree = _world?.GetInnovationTree();
		if (tree == null)
		{
			return ids;
		}
		var prerequisites = tree.GetDirectPrerequisites(innovationId);
		HashSet<int> seen = new HashSet<int>();
		foreach (int id in prerequisites.Item1)
		{
			if (seen.Add(id))
			{
				ids.Add(id);
			}
		}
		foreach (int id in prerequisites.Item2)
		{
			if (seen.Add(id))
			{
				ids.Add(id);
			}
		}
		ids.Sort();
		return ids;
	}

	public bool CanResearch(int realmId, int innovationId)
	{
		InnovationTree tree = _world?.GetInnovationTree();
		if (tree == null)
		{
			return false;
		}
		InnovationDef def = tree.GetInnovation(innovationId);
		if (def == null || tree.HasInnovation(realmId, innovationId))
		{
			return false;
		}
		return tree.ArePrerequisitesMet(_world, realmId, def);
	}

	public bool IsTechAccessibleForGoods(int goodsId, int realmId)
	{
		InnovationTree tree = _world?.GetInnovationTree();
		if (_world?.goodsDefs == null || !_world.goodsDefs.TryGetValue(goodsId, out GoodsDef goods))
		{
			return false;
		}
		return ResourceAccess.IsTechAccessible(goods, tree, realmId);
	}

	public bool IsResourceDepthAccessible(int accessDepth, int realmId)
	{
		InnovationTree tree = _world?.GetInnovationTree();
		return ResourceAccess.IsDepthAccessible((ResourceAccessDepth)accessDepth, tree, realmId);
	}

	public bool CanAccessResource(int goodsId, int accessDepth, int realmId)
	{
		return IsTechAccessibleForGoods(goodsId, realmId)
			&& IsResourceDepthAccessible(accessDepth, realmId);
	}

	public bool HasInnovation(int realmId, int innovationId)
	{
		return _world?.GetInnovationTree()?.HasInnovation(realmId, innovationId) ?? false;
	}

	public string GetInnovationName(int innovationId)
	{
		return _world?.GetInnovationTree()?.GetInnovation(innovationId)?.GetName() ?? "";
	}
}
}
