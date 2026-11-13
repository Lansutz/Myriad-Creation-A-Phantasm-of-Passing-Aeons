using System.Collections.Generic;
using MyriadCreation.Simulation.Units;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Settlement;
using UnityEngine;

namespace MyriadCreation.Simulation.Systems
{
public static class UnitFounding
{
	public static readonly IReadOnlyDictionary<int, float> DefaultMaterialCost = new Dictionary<int, float>
	{
		[30] = 20f,
		[40] = 10f
	};

	public static bool TryFoundSettlement(GameWorld world, TransportUnit unit, int realmId, int tileIndex, int founderCharId, out SettlementData settlement, IReadOnlyDictionary<int, float> materialCost = null)
	{
		settlement = null;
		if ((Object)(object)world == (Object)null || unit == null)
		{
			return false;
		}
		if (!unit.IsFoundingReady(materialCost ?? DefaultMaterialCost))
		{
			return false;
		}
		if (world.TryGetSiteAtTile(tileIndex, out var _))
		{
			return false;
		}
		IReadOnlyDictionary<int, float> readOnlyDictionary = materialCost ?? DefaultMaterialCost;
		if (!unit.TryConsumeFoundingMaterials(readOnlyDictionary))
		{
			return false;
		}
		settlement = world.CreateCity(realmId, tileIndex, founderCharId);
		if (settlement == null)
		{
			settlement = world.CreateWildernessSettlement(realmId, tileIndex, founderCharId);
		}
		if (settlement == null)
		{
			foreach (KeyValuePair<int, float> item in readOnlyDictionary)
			{
				unit.TryAddCargo(item.Key, item.Value, 1f);
			}
			return false;
		}
		settlement.EnsureBlocksFromLegacy();
		unit.foundingProgressDays = 0f;
		return true;
	}
}
}
