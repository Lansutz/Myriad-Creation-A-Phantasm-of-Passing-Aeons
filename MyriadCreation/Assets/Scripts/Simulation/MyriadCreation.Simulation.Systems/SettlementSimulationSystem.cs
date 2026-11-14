using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Scheduling;
using MyriadCreation.Content.Definitions;
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

	/// <summary>
	/// 社会实践的日常来源：按聚落人口的阶层构成折算到各革新领域。
	/// 农民种地/修屋/手工，商人跑贸易与记账，贵族教士治理、立法、祭祀、记史，奴隶开矿与营建。
	/// 这是“分工产生实践”的机制入口，避免只有农业/采矿能积累实践、其余领域永远锁死。
	/// </summary>
	public void PracticeDailyTick(SimulationTickContext context)
	{
		if (_world?.Innovations == null || _world.tiles == null) return;
		float days = Mathf.Max(1, context.deltaDays);
		foreach (SettlementData settlement in _settlements.Values)
		{
			if (settlement == null || settlement.IsRuined) continue;
			int tileIndex = settlement.tileIndex;
			if (tileIndex < 0 || tileIndex >= _world.tiles.Length) continue;
			TileData tile = _world.tiles[tileIndex];
			if (tile.populationBlocks == null) continue;
			int realmId = tile.ownerRealmId;
			if (realmId < 0) continue;
			for (int i = 0; i < tile.populationBlocks.Count; i++)
			{
				PopulationBlock block = tile.populationBlocks[i];
				if (block.count <= 0f) continue;
				AddClassPractice(_world.Innovations, realmId, block.socialClass, block.count * PracticePerPersonPerDay * days);
			}
		}
	}

	private const float PracticePerPersonPerDay = 0.0003f;

	public static void AddClassPractice(InnovationTree innovations, int realmId, GameEnums.SocialClass socialClass, float scale)
	{
		switch (socialClass)
		{
			case GameEnums.SocialClass.Royalty:
				Add(innovations, realmId, InnovationField.Governance, scale * 0.4f);
				Add(innovations, realmId, InnovationField.Law, scale * 0.2f);
				Add(innovations, realmId, InnovationField.Bureaucracy, scale * 0.1f);
				Add(innovations, realmId, InnovationField.Heritage, scale * 0.1f);
				Add(innovations, realmId, InnovationField.Ritual, scale * 0.1f);
				Add(innovations, realmId, InnovationField.Festival, scale * 0.1f);
				break;
			case GameEnums.SocialClass.NobilityClergy:
				Add(innovations, realmId, InnovationField.Governance, scale * 0.2f);
				Add(innovations, realmId, InnovationField.Bureaucracy, scale * 0.15f);
				Add(innovations, realmId, InnovationField.Law, scale * 0.15f);
				Add(innovations, realmId, InnovationField.Theology, scale * 0.15f);
				Add(innovations, realmId, InnovationField.ReligiousInstitution, scale * 0.1f);
				Add(innovations, realmId, InnovationField.Philosophy, scale * 0.05f);
				Add(innovations, realmId, InnovationField.Education, scale * 0.05f);
				Add(innovations, realmId, InnovationField.Ritual, scale * 0.05f);
				Add(innovations, realmId, InnovationField.NaturalCognition, scale * 0.03f);
				Add(innovations, realmId, InnovationField.Medicine, scale * 0.03f);
				Add(innovations, realmId, InnovationField.Heritage, scale * 0.03f);
				Add(innovations, realmId, InnovationField.Festival, scale * 0.02f);
				Add(innovations, realmId, InnovationField.SocialThought, scale * 0.02f);
				Add(innovations, realmId, InnovationField.MilitaryThought, scale * 0.05f);
				Add(innovations, realmId, InnovationField.MilitaryInstitution, scale * 0.03f);
				break;
			case GameEnums.SocialClass.MerchantFreeman:
				Add(innovations, realmId, InnovationField.Economic, scale * 0.3f);
				Add(innovations, realmId, InnovationField.Transport, scale * 0.2f);
				Add(innovations, realmId, InnovationField.Craft, scale * 0.2f);
				Add(innovations, realmId, InnovationField.Mathematics, scale * 0.1f);
				Add(innovations, realmId, InnovationField.Script, scale * 0.1f);
				Add(innovations, realmId, InnovationField.Law, scale * 0.1f);
				Add(innovations, realmId, InnovationField.Machinery, scale * 0.05f);
				Add(innovations, realmId, InnovationField.Metallurgy, scale * 0.05f);
				break;
			case GameEnums.SocialClass.Slave:
				Add(innovations, realmId, InnovationField.Mining, scale * 0.3f);
				Add(innovations, realmId, InnovationField.Construction, scale * 0.3f);
				Add(innovations, realmId, InnovationField.Agriculture, scale * 0.2f);
				Add(innovations, realmId, InnovationField.Craft, scale * 0.2f);
				break;
			default:
				Add(innovations, realmId, InnovationField.Agriculture, scale * 0.5f);
				Add(innovations, realmId, InnovationField.Construction, scale * 0.2f);
				Add(innovations, realmId, InnovationField.Craft, scale * 0.2f);
				Add(innovations, realmId, InnovationField.Folkways, scale * 0.1f);
				// 民间生活本身也是实践来源：节庆/仪式由全村参与，草药与物候观察、育儿口传、
				// 纠纷规范、祖先神话都在日常发生。份额低于专职教士，避免"没有教士就永远零实践"。
				Add(innovations, realmId, InnovationField.Festival, scale * 0.08f);
				Add(innovations, realmId, InnovationField.Ritual, scale * 0.05f);
				Add(innovations, realmId, InnovationField.NaturalCognition, scale * 0.03f);
				Add(innovations, realmId, InnovationField.Medicine, scale * 0.02f);
				Add(innovations, realmId, InnovationField.Education, scale * 0.02f);
				Add(innovations, realmId, InnovationField.SocialThought, scale * 0.02f);
				Add(innovations, realmId, InnovationField.Theology, scale * 0.01f);
				// 乡勇/民兵的巡逻、械斗与抵御也是军事经验的日常来源（正规军另计）。
				Add(innovations, realmId, InnovationField.MilitaryThought, scale * 0.01f);
				break;
		}
	}

	private static void Add(InnovationTree innovations, int realmId, InnovationField field, float amount)
	{
		if (amount > 0f) innovations.AddPractice(realmId, field, amount);
	}

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
			// 环境压力：河流改道/水源消失后，周边没有水体可依赖的聚落会逐日失血，满 3 年判衰退。
			if (SettlementEvolutionSystem.ApplyEnvironmentalPressure(_world, settlement, context.deltaDays))
			{
				settlement.abandonmentPressureDays = 0;
				toAbandon.Add(settlement);
				continue;
			}
			float gain = 0.02f + Mathf.Clamp(settlement.population / 5000f, 0f, 1f) * 0.08f;
			// 开发度来自持续建设劳动：人口越多，日常营建把聚落开发度推得越快；不再只减不增。
			settlement.development += gain * context.deltaDays * 0.01f;
			SyncSiteLandDevelopment(settlement, context.deltaDays);
			SettlementTypologySystem.UpdateConstructionProgress(settlement, gain * context.deltaDays);
			if (!SettlementTypologySystem.IsConstructionReady(settlement))
			{
				continue;
			}
			// 村镇→城市：人口/开发/市场/剩余到位才转形态；不读年份。
			if (settlement.settlementForm == SettlementForm.Village)
			{
				SettlementEvolutionSystem.TryUrbanize(_world, settlement);
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
			bool wasCity = settlement.settlementForm == SettlementForm.City;
			string cause = string.IsNullOrEmpty(settlement.lastDeclineCause) ? "人口流失" : settlement.lastDeclineCause;
			if (_evolutionRuntime.DegradeSettlementToCamp(anchorId) != null)
			{
				if (wasCity)
				{
					_world.CityCollapsedCount++;
					_world.LastCityDeclineCause = cause;
				}
				_world.GetChronicle()?.Add("settlement_abandoned", name + " 废弃为营地（" + cause + "）", true);
			}
		}
	}

	/// <summary>
	/// 聚落营建会逐步开垦周边土地：地块开发度（0-1）向 聚落开发度/100 缓慢靠拢。
	/// 只上不下地“追平”会抹掉灾害/战争造成的土地退化，所以按日步长移动，受灾后仍需时间恢复。
	/// </summary>
	private void SyncSiteLandDevelopment(SettlementData settlement, int deltaDays)
	{
		int tileIndex = settlement.tileIndex;
		if (tileIndex < 0 || tileIndex >= _tiles.Length) return;
		TileData tile = _tiles[tileIndex];
		if (!tile.exists || !tile.isLand) return;
		float target = Mathf.Clamp01(settlement.development / 100f);
		tile.development = Mathf.MoveTowards(tile.development, target, 0.001f * deltaDays);
		_tiles[tileIndex] = tile;
	}

	public void ProductionDailyTick(SimulationTickContext context)
	{
		_productionRuntime.DailyTick();
	}
}
}
