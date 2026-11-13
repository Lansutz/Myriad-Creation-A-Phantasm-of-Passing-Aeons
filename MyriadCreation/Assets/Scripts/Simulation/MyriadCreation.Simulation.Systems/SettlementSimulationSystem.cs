using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Scheduling;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Settlement;
using UnityEngine;

namespace MyriadCreation.Simulation.Systems
{
public sealed class SettlementSimulationSystem
{
	private readonly Dictionary<int, SettlementData> _settlements;

	private readonly TileData[] _tiles;

	private readonly int _mapWidth;

	private readonly int _mapHeight;

	private readonly Dictionary<int, Army> _armies;

	private readonly GameWorld _world;

	private readonly MapActorManager _mapActors;

	private readonly CampManager _camps;

	private readonly SettlementEvolutionRuntime _evolutionRuntime;

	private readonly SettlementControlRuntime _controlRuntime;

	private readonly SettlementDestructionRuntime _destructionRuntime;

	private readonly LandAbandonmentRuntime _abandonmentRuntime;

	private readonly SettlementProductionRuntime _productionRuntime;

	public SettlementDestructionRuntime DestructionRuntime => _destructionRuntime;

	public LandAbandonmentRuntime AbandonmentRuntime => _abandonmentRuntime;

	public SettlementSimulationSystem(Dictionary<int, SettlementData> settlements, TileData[] tiles, int mapWidth, int mapHeight, Dictionary<int, Army> armies, GameWorld world, MapActorManager mapActors, CampManager camps)
	{
		_settlements = settlements ?? throw new ArgumentNullException("settlements");
		_tiles = tiles ?? throw new ArgumentNullException("tiles");
		_mapWidth = mapWidth;
		_mapHeight = mapHeight;
		_armies = armies ?? throw new ArgumentNullException("armies");
		_world = world ?? throw new ArgumentNullException("world");
		_mapActors = mapActors ?? throw new ArgumentNullException("mapActors");
		_camps = camps ?? throw new ArgumentNullException("camps");
		_evolutionRuntime = new SettlementEvolutionRuntime(_world);
		_controlRuntime = new SettlementControlRuntime(_settlements, _tiles, _mapWidth, _mapHeight, _armies, _world.unitDefs);
		_destructionRuntime = new SettlementDestructionRuntime(_world);
		_abandonmentRuntime = new LandAbandonmentRuntime(_world);
		_productionRuntime = new SettlementProductionRuntime(_settlements, _tiles, _world);
	}

	public void ControlDailyTick(SimulationTickContext context)
	{
		_controlRuntime.DailyTick();
	}

	public void MapActorsDailyTick(SimulationTickContext context)
	{
		_mapActors.Tick(context.deltaDays);
	}

	public void CampsDailyTick(SimulationTickContext context)
	{
		_camps.Tick(context.deltaDays);
	}

	public void EvolutionDailyTick(SimulationTickContext context)
	{
		_evolutionRuntime.DailyTick();
	}

	public void RecoveryDailyTick(SimulationTickContext context)
	{
		_destructionRuntime.DailyTickRecovery();
	}

	public void AbandonmentDailyTick(SimulationTickContext context)
	{
		_abandonmentRuntime.DailyCheckBanditSpawn();
	}


	public int TypologyTickCount { get; private set; }

	public int TypologySettlementCount { get; private set; }

	public int TypologySedentaryCount { get; private set; }

	public bool HasSettlement(int anchorId) => _settlements.ContainsKey(anchorId);

	public void TypologyDailyTick(SimulationTickContext context)
	{
		TypologyTickCount++;
		TypologySettlementCount = _settlements.Count;
		TypologySedentaryCount = 0;
		const float AbandonPopulationThreshold = 50f;
		const int AbandonPressureDays = 180;
		List<SettlementData> toAbandon = new List<SettlementData>();
		foreach (SettlementData settlement in _settlements.Values)
		{
			if (settlement == null || settlement.IsRuined || settlement.siteMode != SiteMode.SedentarySettlement)
			{
				continue;
			}
			TypologySedentaryCount++;
			// P1.1 废弃端口：长期人口不足、无守军、无驻军，才积累废弃压力。
			bool hasArmy = false;
			foreach (Army army in _armies.Values)
			{
				if (army != null && army.state != GameEnums.CombatState.Dead && army.currentTileIndex == settlement.tileIndex)
				{
					hasArmy = true;
					break;
				}
			}
			if (settlement.population < AbandonPopulationThreshold && settlement.garrison <= 0 && !hasArmy)
			{
				settlement.abandonmentPressureDays++;
				if (settlement.abandonmentPressureDays >= AbandonPressureDays)
				{
					toAbandon.Add(settlement);
					continue;
				}
			}
			else
			{
				settlement.abandonmentPressureDays = 0;
			}
			float gain = 0.02f + Mathf.Clamp(settlement.population / 5000f, 0f, 1f) * 0.08f;
			// 开发度来自持续建设劳动：人口越多，日常营建把聚落开发度推得越快；不再只减不增。
			settlement.development += gain * context.deltaDays * 0.01f;
			SettlementTypologySystem.UpdateConstructionProgress(settlement, gain * context.deltaDays);
			if (!SettlementTypologySystem.IsConstructionReady(settlement))
			{
				continue;
			}
			if (SettlementTypologySystem.CanLevelUp(settlement, out _))
			{
				SettlementTypologySystem.PerformLevelUp(settlement);
				_world.GetChronicle()?.Add("settlement_level", settlement.settlementName + " 升为 " + settlement.settlementLevel, true);
			}
		}
		foreach (SettlementData settlement in toAbandon)
		{
			string name = settlement.settlementName;
			int anchorId = settlement.anchorId;
			if (_evolutionRuntime.DegradeSettlementToCamp(anchorId) != null)
			{
				_world.GetChronicle()?.Add("settlement_abandoned", name + " 因人口流失废弃为营地", true);
			}
		}
	}

	public void ProductionDailyTick(SimulationTickContext context)
	{
		_productionRuntime.DailyTick();
	}
}
}
