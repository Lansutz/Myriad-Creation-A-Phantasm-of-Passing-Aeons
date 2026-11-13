using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.State;
using MyriadCreation.World.Settlement;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
public sealed class SettlementControlRuntime
{
	private readonly Dictionary<int, SettlementData> _settlements;

	private readonly TileData[] _tiles;

	private readonly int _mapWidth;

	private readonly int _mapHeight;

	private readonly Dictionary<int, Army> _armies;

	private readonly Dictionary<int, MyriadCreation.Content.Definitions.UnitDef> _unitDefs;

	public SettlementControlRuntime(Dictionary<int, SettlementData> settlements, TileData[] tiles, int mapWidth, int mapHeight, Dictionary<int, Army> armies,
		Dictionary<int, MyriadCreation.Content.Definitions.UnitDef> unitDefs = null)
	{
		_settlements = settlements ?? throw new ArgumentNullException("settlements");
		_tiles = tiles ?? throw new ArgumentNullException("tiles");
		_mapWidth = mapWidth;
		_mapHeight = mapHeight;
		_armies = armies ?? throw new ArgumentNullException("armies");
		_unitDefs = unitDefs;
	}

	public void DailyTick()
	{
		SettlementControlSystem.DailyTick(_settlements, _tiles, _mapWidth, _mapHeight, _armies, _unitDefs);
	}

	public SettlementData GetSuzerain(SettlementData settlement)
	{
		return SettlementControlSystem.GetSuzerain(settlement, _settlements);
	}

	public float GetTaxSiphonModifier(SettlementData settlement)
	{
		return SettlementControlSystem.GetTaxSiphonModifier(settlement, _settlements);
	}

	public float GetTradeSiphonModifier(SettlementData settlement)
	{
		return SettlementControlSystem.GetTradeSiphonModifier(settlement, _settlements);
	}
}
}
