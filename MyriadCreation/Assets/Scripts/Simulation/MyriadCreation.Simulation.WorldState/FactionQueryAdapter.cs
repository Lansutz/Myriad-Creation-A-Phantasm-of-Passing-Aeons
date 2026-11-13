using System.Collections.Generic;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Simulation.WorldState
{
/// <summary>
/// UI 4b5a：IFactionQuery 的 Simulation host 适配器。
/// 转调 GameWorld.Factions（FactionManager.GetFactions）；null/空安全。
/// </summary>
public sealed class FactionQueryAdapter : IFactionQuery
{
	private readonly GameWorld _world;

	public FactionQueryAdapter(GameWorld world)
	{
		_world = world;
	}

	public IReadOnlyList<FactionSnapshotDto> GetFactions(int realmId)
	{
		List<FactionSnapshotDto> list = new List<FactionSnapshotDto>();
		FactionManager manager = _world?.Factions;
		if (manager == null)
		{
			return list;
		}
		IReadOnlyList<Faction> factions = manager.GetFactions(realmId);
		if (factions == null || factions.Count == 0)
		{
			return list;
		}
		float totalPower = 0f;
		foreach (Faction faction in factions)
		{
			if (faction != null)
			{
				totalPower += faction.power;
			}
		}
		foreach (Faction faction in factions)
		{
			if (faction == null)
			{
				continue;
			}
			list.Add(new FactionSnapshotDto
			{
				factionId = faction.factionId,
				realmId = faction.realmId,
				factionName = faction.factionName ?? "",
				stanceId = (int)faction.stance,
				stanceName = StanceName(faction.stance),
				power = faction.power,
				share = totalPower > 0f ? faction.power / totalPower * 100f : 0f,
				isRuling = faction.isInGovernment,
				cohesion = faction.cohesion,
				leaderCharacterId = faction.leaderCharacterId,
				primaryClassId = (int)faction.primaryClass
			});
		}
		list.Sort((a, b) => a.factionId.CompareTo(b.factionId));
		return list;
	}

	private static string StanceName(FactionStance stance)
	{
		switch (stance)
		{
		case FactionStance.Conservative: return "保守派";
		case FactionStance.Reformist: return "改革派";
		case FactionStance.Radical: return "激进派";
		case FactionStance.Reactionary: return "复辟派";
		default: return stance.ToString();
		}
	}
}
}
