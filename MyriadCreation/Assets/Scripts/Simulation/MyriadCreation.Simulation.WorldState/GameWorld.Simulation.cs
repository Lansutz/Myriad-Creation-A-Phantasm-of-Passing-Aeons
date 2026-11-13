using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Core.Events;
using MyriadCreation.Simulation.Scheduling;
using MyriadCreation.Simulation.AI;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.Diplomacy;
using MyriadCreation.Simulation.Economy;
using MyriadCreation.Simulation.Events;
using MyriadCreation.Simulation.Generation;
using MyriadCreation.Simulation.Innovation;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Simulation.Planning;
using MyriadCreation.Simulation.Politics;
using MyriadCreation.Simulation.Settlement;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.ActionGate;
using GateService = MyriadCreation.Simulation.ActionGate.ActionGate;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Simulation.Units;
using MyriadCreation.Simulation.Warfare;
using MyriadCreation.World;
using MyriadCreation.World.Anchor;
using MyriadCreation.World.Biome;
using MyriadCreation.World.Climate;
using MyriadCreation.World.Generation;
using MyriadCreation.World.Settlement;
using MyriadCreation.World.Terrain;
using UnityEngine;
using MyriadCreation.Simulation.Faith;
using MyriadCreation.Simulation.Culture;
using MyriadCreation.Simulation.CultureHost;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.LawHost;
using Object = UnityEngine.Object;
using Random = System.Random;
using MyriadCreation.Simulation.FaithHost;

namespace MyriadCreation.Simulation.WorldState
{
public partial class GameWorld : MonoBehaviour
{

	private void PopulationTick()
	{
		ConcurrentQueue<GameEvent> parallelEvents = new ConcurrentQueue<GameEvent>();
		Parallel.For(0, tiles.Length, (int i) =>
		{
			if (tiles[i].exists && tiles[i].isLand && tiles[i].populationBlocks != null)
			{
				for (int j = 0; j < tiles[i].populationBlocks.Count; j++)
				{
					PopulationBlock populationBlock = tiles[i].populationBlocks[j];
					if (races.TryGetValue(populationBlock.raceId, out var value))
					{
						float num = value.CalculatePopulationGrowthRate(tiles[i], populationBlock.satisfaction);
						float capacity = CarryingCapacitySystem.CalculateCarryingCapacity(tiles[i], tradeCenters);
						float overloadRatio = CarryingCapacitySystem.GetOverloadRatio(tiles[i], capacity);
						if (overloadRatio > 1f)
						{
							float num2 = Mathf.Clamp01((overloadRatio - 1.1f) / 0.9f);
							num *= 1f - num2;
						}
						populationBlock.count *= 1f + num * (float)daysPerTick;
					}
					float num3 = CalculateSatisfactionDelta(i, populationBlock);
					populationBlock.satisfaction = Mathf.Clamp(populationBlock.satisfaction + num3, 0f, 100f);
					if (populationBlock.satisfaction < 20f && Roll01(i, j, 1, 0.01f))
					{
						parallelEvents.Enqueue(new GameEvent
						{
							eventType = GameEventType.Rebellion,
							tileIndex = i,
							severity = 100f - populationBlock.satisfaction
						});
					}
					tiles[i].populationBlocks[j] = populationBlock;
				}
				if (tradeCenters.TryGetValue(tiles[i].regionId, out var value2))
				{
					float valueOrDefault = value2.inventory.GetValueOrDefault(0, 0f);
					float tilePopulation = GetTilePopulation(i);
					if (valueOrDefault < tilePopulation * 0.01f && tilePopulation > 0f)
					{
						parallelEvents.Enqueue(new GameEvent
						{
							eventType = GameEventType.Famine,
							tileIndex = i,
							severity = 50f
						});
					}
				}
			}
		});
		GameEvent result;
		while (parallelEvents.TryDequeue(out result))
		{
			EnqueueEvent(result);
		}
	}

	private float CalculateSatisfactionDelta(int tileIndex, PopulationBlock pb)
	{
		float num = 0f;
		ref TileData reference = ref tiles[tileIndex];
		num += (reference.stability - 50f) * 0.01f;
		if (realms.TryGetValue(reference.ownerRealmId, out var value))
		{
			num += value.taxSystem.GetTaxSatisfactionImpact(pb.socialClass) * 0.01f;
		}
		if (cultures.TryGetValue(pb.cultureId, out var _))
		{
			num += (pb.culturePenetration - 50f) * 0.005f;
		}
		if (races.TryGetValue(pb.raceId, out var value3))
		{
			num += (value3.ClassRelationBaseline - 50f) * 0.005f;
		}
		return num * (float)daysPerTick;
	}

	private void EnsureAdminDivisions()
	{
		for (int i = 0; i < realms.Count; i++)
		{
			RealmData realmData = realms[i];
			if (realmData == null || realmData.adminDivisions.Count > 0)
			{
				continue;
			}
			HashSet<int> hashSet = new HashSet<int>(realmData.coreTiles);
			for (int j = 0; j < tiles.Length; j++)
			{
				if (tiles[j].ownerRealmId == realmData.realmId)
				{
					hashSet.Add(j);
				}
			}
			if (hashSet.Count != 0)
			{
				float administrativeCapacity = ((realmData.laws != null) ? LawEffects.CalculateEffects(realmData.laws) : null)?.administrativeCapacity ?? 0.5f;
				realmData.adminDivisions = AdminDivisionSystem.Generate(realmData, realmData.laws, administrativeCapacity, hashSet);
				AssignAdminHolders(realmData);
			}
		}
	}

	private void AssignAdminHolders(RealmData realm)
	{
		if (realm?.adminDivisions == null || _characterManager == null)
		{
			return;
		}
		List<CharacterData> charactersByRealm = _characterManager.GetCharactersByRealm(realm.realmId);
		HashSet<int> hashSet = new HashSet<int>();
		int supremeRulerId = realm.GetSupremeRulerId();
		foreach (AdminDivision adminDivision in realm.adminDivisions)
		{
			if (adminDivision.holderCharacterId >= 0)
			{
				hashSet.Add(adminDivision.holderCharacterId);
				continue;
			}
			CharacterData characterData = null;
			if (adminDivision.level == 1 && supremeRulerId >= 0)
			{
				characterData = _characterManager.GetCharacter(supremeRulerId);
			}
			if (characterData == null)
			{
				foreach (CharacterData item in charactersByRealm)
				{
					if (item != null && item.isAlive && !hashSet.Contains(item.characterId) && item.role != CharacterRole.Ruler && item.role != CharacterRole.Heir && item.role != CharacterRole.Spouse && item.role != CharacterRole.Commoner)
					{
						characterData = item;
						break;
					}
				}
			}
			if (characterData == null)
			{
				continue;
			}
			adminDivision.holderCharacterId = characterData.characterId;
			hashSet.Add(characterData.characterId);
			characterData.aiCategory = CharacterAICategory.Official;
			characterData.isPublicFigure = true;
			CharacterKnowledgeRegistry.Learn(realm.realmId, characterData, currentDay, "appointment");
			if (settlements == null)
			{
				continue;
			}
			foreach (SettlementData value in settlements.Values)
			{
				if (adminDivision.tiles != null && adminDivision.tiles.Contains(value.tileIndex))
				{
					characterData.currentSettlementAnchorId = value.anchorId;
					characterData.currentTile = value.tileIndex;
					break;
				}
			}
		}
	}

	private void MissionaryTick()
	{
		_missionaryTimer += daysPerTick;
		if (_missionaryTimer < 15)
		{
			return;
		}
		_missionaryTimer = 0;
		if (tiles == null)
		{
			return;
		}
		// 确定性：每次传入世界 seed + 当日派生，避免共享 Random 状态跨调用漂移（同 seed 同结果）。
		_missionaryRng = MyriadCreation.Core.DeterministicRandom.Create(randomSeed, currentDay, 17);
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		foreach (RealmData value2 in realms.Values)
		{
			if (value2 != null && value2.stateReligionId >= 0)
			{
				dictionary[value2.realmId] = value2.stateReligionId;
			}
		}
		if (dictionary.Count == 0)
		{
			return;
		}
		for (int i = 0; i < tiles.Length; i++)
		{
			ref TileData reference = ref tiles[i];
			if (!reference.exists || reference.populationBlocks == null || reference.populationBlocks.Count == 0)
			{
				continue;
			}
			int ownerRealmId = reference.ownerRealmId;
			if (ownerRealmId >= 0 && dictionary.TryGetValue(ownerRealmId, out var value) && PopulationStats.GetDominantFaith(reference) != value)
			{
				float num = MissionarySystem.CalculateSuccessChance(reference, value, (int id) => ReligionCatalog.Get(id), (int id) => ReligionCatalog.GetRoot(id)?.religionId ?? (-1));
				if (num > 0f)
				{
					MissionarySystem.ConvertTile(reference, value, ownerRealmId, num, _missionaryRng);
				}
			}
		}
	}

	private void OnInnovationCompletedHandler(int realmId, int innovationId)
	{
		if (realms.TryGetValue(realmId, out var value))
		{
			CultureData value2 = null;
			if (value.primaryCultureId >= 0)
			{
				cultures.TryGetValue(value.primaryCultureId, out value2);
			}
			ClassEmergenceEvents.RecordEmergence(innovationId, value.realmName, value2, _innovationTree, realmId, _chronicle);
		}
	}

	private void EnsureOfficeHolders()
	{
		if (_characterManager == null)
		{
			return;
		}
		for (int i = 0; i < realms.Count; i++)
		{
			RealmData realmData = realms[i];
			if (realmData == null)
			{
				continue;
			}
			int supremeRulerId = realmData.GetSupremeRulerId();
			HashSet<int> hashSet = new HashSet<int>();
			for (int j = 0; j < 6; j++)
			{
				if (realmData.officeHolders.TryGetValue(j, out var value) && value >= 0)
				{
					CharacterData character = _characterManager.GetCharacter(value);
					if (character == null || !character.isAlive)
					{
						realmData.officeHolders[j] = -1;
						TitleInstanceRegistry.GetOrCreate("realm_" + realmData.realmId + "_office_" + j)?.EndActiveHoldings(currentDay);
					}
					else
					{
						hashSet.Add(value);
					}
				}
			}
			for (int k = 0; k < 6; k++)
			{
				if (realmData.officeHolders.ContainsKey(k) && realmData.officeHolders[k] >= 0)
				{
					continue;
				}
				List<CharacterData> charactersByRealm = _characterManager.GetCharactersByRealm(realmData.realmId);
				CharacterData characterData = null;
				foreach (CharacterData item in charactersByRealm)
				{
					if (item != null && item.characterId != supremeRulerId && item.isAlive && !hashSet.Contains(item.characterId) && (item.role == CharacterRole.Noble || item.role == CharacterRole.Military || item.role == CharacterRole.Scholar))
					{
						characterData = item;
						break;
					}
				}
				if (characterData == null)
				{
					foreach (CharacterData item2 in charactersByRealm)
					{
						if (item2 != null && item2.characterId != supremeRulerId && item2.isAlive && !hashSet.Contains(item2.characterId))
						{
							characterData = item2;
							break;
						}
					}
				}
				if (characterData != null)
				{
					realmData.officeHolders[k] = characterData.characterId;
					TitleInstance officeInstance = TitleInstanceRegistry.GetOrCreate("realm_" + realmData.realmId + "_office_" + k);
					if (officeInstance != null && (officeInstance.GetSeniorHolder(currentDay)?.characterId ?? -1) != characterData.characterId)
					{
						officeInstance.TransferSenior(characterData.characterId, currentDay);
					}
					hashSet.Add(characterData.characterId);
					characterData.aiCategory = CharacterAICategory.Official;
				}
				else
				{
					realmData.officeHolders[k] = -1;
					TitleInstanceRegistry.GetOrCreate("realm_" + realmData.realmId + "_office_" + k)?.EndActiveHoldings(currentDay);
				}
			}
		}
	}

	private void AdvanceTime()
	{
		currentDay += daysPerTick;
		if (currentDay > 365)
		{
			currentDay = 1;
			currentYear++;
		}
		int num = (currentDay - 1) / 91;
		if (num != currentSeason && num < 4)
		{
			currentSeason = num;
			_climateSimulator.UpdateForSeason(currentSeason);
			SimulationEvents.Publish(new SeasonChangedEvent(currentDay, currentYear, currentSeason));
			EnqueueEvent(new GameEvent
			{
				eventType = GameEventType.SeasonChange,
				severity = currentSeason
			});
		}
	}

	private void UpdateFaithFervor(int currentDay)
	{
		if (currentDay - _faithFervorDay < 30)
		{
			return;
		}
		_faithFervorDay = currentDay;
		foreach (FaithSystem faithSystem in _faithSystems)
		{
			if (faithSystem.fervor > 10f && currentDay % 360 == 0)
			{
				faithSystem.AddFervor(-10f);
			}
			CheckHolySiteLoss(faithSystem);
		}
	}

	private void CheckHolySiteLoss(FaithSystem faith)
	{
		foreach (int holySiteTileIndex in faith.holySiteTileIndices)
		{
			int item = faith.faithId * 100000 + holySiteTileIndex;
			if (_holySiteLostProcessed.Contains(item) || holySiteTileIndex < 0 || holySiteTileIndex >= tiles.Length)
			{
				continue;
			}
			int ownerRealmId = tiles[holySiteTileIndex].ownerRealmId;
			if (ownerRealmId >= 0 && ownerRealmId < realms.Count)
			{
				int stateReligionId = realms[ownerRealmId].stateReligionId;
				if (stateReligionId >= 0 && stateReligionId != faith.faithId)
				{
					faith.AddFervor(50f);
					_holySiteLostProcessed.Add(item);
					_chronicle?.Add("religion", "圣地失陷：" + faith.faithName + " 的圣地被异教控制——信仰热忱高涨", true, ownerRealmId);
				}
			}
		}
	}

	private void CheckGreatHolyWarSettlements()
	{
		foreach (GreatHolyWarState ghw in new List<GreatHolyWarState>(GreatHolyWarSystem.ActiveWars))
		{
			if (ghw.ended)
			{
				continue;
			}
			WarState warState = _wars.Find((WarState w) => w.warId == ghw.linkedWarId);
			if (warState != null && warState.ended)
			{
				bool flag = warState.winnerId == ghw.callerRealmId;
				ghw.holySideWon = flag;
				RealmData callerRealm = ((ghw.callerRealmId >= 0 && ghw.callerRealmId < realms.Count) ? realms[ghw.callerRealmId] : null);
				int num = GreatHolyWarSystem.Resolve(ghw, GetCharacterManager(), callerRealm, Mathf.Max(1, GetRealmDivisibleEstates(ghw.callerRealmId)), GetEffectiveLaw(ghw.callerRealmId));
				if (flag && num >= 0 && ghw.targetTile >= 0 && ghw.targetTile < tiles.Length)
				{
					tiles[ghw.targetTile].ownerRealmId = ghw.callerRealmId;
					_chronicle?.Add("religion", $"大圣战胜利：{GetCharacterName(num)} 受封圣地（地块 {ghw.targetTile}）", true, ghw.callerRealmId);
				}
				ghw.ended = true;
			}
		}
		GreatHolyWarSystem.Cleanup();
	}

	private void NotifyWarDefeat(WarState war, int day)
	{
		if (_regimeDynamics == null)
		{
			return;
		}
		int winnerId = war.winnerId;
		int num = ((winnerId == war.attackerId) ? war.defenderId : war.attackerId);
		if (!realms.ContainsKey(num))
		{
			return;
		}
		int num2 = 0;
		int num3 = 0;
		TileData[] array = tiles;
		for (int i = 0; i < array.Length; i++)
		{
			TileData tileData = array[i];
			if (tileData.exists && tileData.ownerRealmId == num)
			{
				num2++;
				if (tileData.occupyingRealmId == winnerId)
				{
					num3++;
				}
			}
		}
		float num4 = ((num2 > 0) ? ((float)num3 / (float)num2) : 0f);
		CriticalJunctureType type = ((!(num4 >= 0.3f)) ? CriticalJunctureType.WarDefeat : CriticalJunctureType.ForeignConquest);
		float stability = realms[num].stability;
		float rawSeverity = Mathf.Clamp(55f + num4 * 70f + (100f - stability) * 0.15f, 0f, 100f);
		_regimeDynamics.NotifyEvent(day, num, type, rawSeverity);
	}

	public void CalculateClimate()
	{
		if (_atmosphericCirculation == null)
		{
			_atmosphericCirculation = new AtmosphericCirculation(mapWidth, mapHeight);
		}
		float[] array = new float[tiles.Length];
		bool[] array2 = new bool[tiles.Length];
		float[] array3 = new float[tiles.Length];
		for (int i = 0; i < tiles.Length; i++)
		{
			array[i] = tiles[i].elevation01;
			array2[i] = tiles[i].isLand;
			array3[i] = tiles[i].annualTemp;
		}
		GenConfig.ApplyToGCM(_atmosphericCirculation);
		_atmosphericCirculation.Run(array, array2, array3);
		for (int j = 0; j < tiles.Length; j++)
		{
			tiles[j].annualPrecipMm = _atmosphericCirculation.Precipitation[j];
			tiles[j].airHumidityPct = Mathf.Clamp(_atmosphericCirculation.SpecificHumidity[j] * 10000f, 0f, 100f);
			_worldMapSimulation?.MarkClimateDirty(j);
		}
		for (int k = 0; k < tiles.Length; k++)
		{
			float latAbs = Mathf.Abs((float)((double)TileGrid.ToY(k, mapWidth) - (double)mapHeight * 0.5) / (float)mapHeight * 180f);
			tiles[k].biome = HoldridgeBiomeClassifier.Classify(tiles[k].annualTemp, tiles[k].annualPrecipMm, tiles[k].elevation01, tiles[k].isLand, tiles[k].isCoast, tiles[k].isRiver, tiles[k].slopeDegree, latAbs);
			tiles[k].movementCost = BiomeAttributes.GetMovementCost(tiles[k].biome);
		}
		Debug.Log((object)"[GameWorld] 气候计算完成：GCM已运行，降水/湿度/生物群系已更新");
	}

	public void RecalculateHydrology()
	{
		if (_hydraulicErosion == null)
		{
			_hydraulicErosion = new HydraulicErosion(mapWidth, mapHeight, randomSeed);
		}
		float[] array = new float[tiles.Length];
		bool[] array2 = new bool[tiles.Length];
		for (int i = 0; i < tiles.Length; i++)
		{
			array[i] = tiles[i].elevation01;
			array2[i] = tiles[i].isLand;
		}
		GenConfig.ApplyToErosion(_hydraulicErosion, tiles.Length);
		_hydraulicErosion.WrapX = config.wrapX;
		_hydraulicErosion.Run(array, array2);
		_hydraulicErosion.ApplyToTiles(tiles, array);
		new TerrainGenerator(randomSeed).TrackRivers(tiles);
		// 水文重算改写了全表高程：直接做一次 WMS 全量重算并清 dirty（task-55），不要沿用 MarkAllTerrainDirty。
		// 否则首个 Daily tick 会用已设置的 _cachedSeaThreshold 对全表再跑一次增量重算（二次减阈值/改写海陆，
		// 与 task-42/49 的初始开图隐患同类）。WMS.RecalculateAll 内部依次走 BeforeFullRecalculate（同步 seed）→
		// SeaLand 全量重算（含破碎化）→ 气候重算，并清空 terrain/climate/config dirty；随后 ClearInitialTerrainDirty
		// 再清调度器 "world.terrain" key，保证编辑器 GenerateAll 结束后即稳定，首个 tick 不再改写地形。
		_worldMapSimulation?.RecalculateAll();
		ClearInitialTerrainDirty();
		Debug.Log((object)"[GameWorld] 水文重算完成：水力侵蚀+河网追踪已执行，并已全量重算海陆/气候、清理 terrain dirty（避免首个 tick 二次改写）");
	}

	public void RecalculateAll()
	{
		EnsureSeaLandGeneratorSeed();
		_worldMapSimulation?.RecalculateAll();
	}

	public List<DecreeOrder> IssueDecreeOrders(string decreeId, int realmId)
	{
		List<DecreeOrder> list = new List<DecreeOrder>();
		if (string.IsNullOrEmpty(decreeId) || !realms.TryGetValue(realmId, out var realm))
		{
			return list;
		}
		EnsureAdminDivisions();
		List<DecreeOrder> list2 = DecreePropagationSystem.IssueToSubordinateSettlements(decreeId, realmId, settlements, tiles, currentDay, (int anchorId) => SettlementTile(anchorId), (int anchorId) => OfficialForSettlement(realm, anchorId));
		RealmData realmData = realm;
		if (realmData.decreeState == null)
		{
			realmData.decreeState = new RealmDecreeState();
		}
		realm.decreeState.orders.AddRange(list2);
		SpawnDecreeCouriers(realm, list2);
		list.AddRange(list2);
		return list;
	}

	private CharacterData FindCourierLeader(int realmId)
	{
		CharacterManager characterManager = _characterManager;
		if (characterManager == null)
		{
			return null;
		}
		List<CharacterData> charactersByRealm = characterManager.GetCharactersByRealm(realmId);
		CharacterData characterData = null;
		foreach (CharacterData item in charactersByRealm)
		{
			if (item != null && item.isAlive)
			{
				if (item.aiCategory == CharacterAICategory.Courier)
				{
					return item;
				}
				if (characterData == null)
				{
					characterData = item;
				}
			}
		}
		return characterData;
	}

	private int FindFoodGoodsId()
	{
		foreach (KeyValuePair<int, GoodsDef> goodsDef in goodsDefs)
		{
			GameEnums.GoodsCategory category = goodsDef.Value.category;
			if (category == GameEnums.GoodsCategory.Food || category == GameEnums.GoodsCategory.Crop)
			{
				return goodsDef.Key;
			}
		}
		return MyriadCreation.Simulation.Units.UnitProvisioningCost.InvalidGoodsId;
	}

	private int FindCurrencyGoodsId()
	{
		int bestId = MyriadCreation.Simulation.Units.UnitProvisioningCost.InvalidGoodsId;
		float bestValue = -1f;
		foreach (KeyValuePair<int, GoodsDef> goodsDef in goodsDefs)
		{
			if (goodsDef.Value.category != GameEnums.GoodsCategory.Metal) continue;
			if (goodsDef.Value.baseValue > bestValue)
			{
				bestValue = goodsDef.Value.baseValue;
				bestId = goodsDef.Key;
			}
		}
		return bestId;
	}

	private int SettlementTile(int anchorId)
	{
		if (settlements == null || !settlements.TryGetValue(anchorId, out var value))
		{
			return -1;
		}
		return value.tileIndex;
	}

	private int OfficialForSettlement(RealmData realm, int anchorId)
	{
		int num = SettlementTile(anchorId);
		if (num < 0 || realm?.adminDivisions == null)
		{
			return -1;
		}
		AdminDivision adminDivision = null;
		foreach (AdminDivision adminDivision2 in realm.adminDivisions)
		{
			if (adminDivision2.tiles != null && adminDivision2.tiles.Contains(num) && (adminDivision == null || adminDivision2.level > adminDivision.level))
			{
				adminDivision = adminDivision2;
			}
		}
		if (adminDivision == null || adminDivision.holderCharacterId < 0)
		{
			return -1;
		}
		CharacterData characterData = _characterManager?.GetCharacter(adminDivision.holderCharacterId);
		if (characterData == null || !characterData.isAlive)
		{
			adminDivision.holderCharacterId = -1;
			return -1;
		}
		return characterData.characterId;
	}

	private void SpawnDecreeCouriers(RealmData realm, List<DecreeOrder> orders)
	{
		if (_mapActorManager == null || orders == null)
		{
			return;
		}
		foreach (DecreeOrder order in orders)
		{
			if (order.targetAnchorId < 0 || !settlements.TryGetValue(order.targetAnchorId, out var value))
			{
				order.state = DecreeOrderState.Failed;
				order.note = "目标聚落不存在";
				continue;
			}
			order.targetTile = value.tileIndex;
			int num = ((order.fromAnchorId >= 0) ? SettlementTile(order.fromAnchorId) : (-1));
			if (num < 0)
			{
				num = order.targetTile;
			}
			float distanceTiles = Mathf.Sqrt(Mathf.Pow((float)(order.targetTile % mapWidth - num % mapWidth), 2f) + Mathf.Pow((float)(order.targetTile / mapWidth - num / mapWidth), 2f));
			int foodGoodsId = FindFoodGoodsId();
			int currencyGoodsId = FindCurrencyGoodsId();
			UnitProvisioningCost cost = UnitProvisioningCost.ForCourier(distanceTiles, foodGoodsId, currencyGoodsId);
			UnitFundingSource source = UnitProvisioning.ResolveFundingSource(realm);
			int regionId = ((num >= 0 && num < tiles.Length) ? tiles[num].regionId : (-1));
			if (!UnitProvisioning.TryFund(this, realm, null, source, regionId, cost, out var funding, out var reason))
			{
				order.state = DecreeOrderState.Failed;
				order.note = reason;
				continue;
			}
			MapActor mapActor = _mapActorManager.SpawnActor(MapActorType.Courier, num, 1, new CourierAI(order.targetTile), UnitSubcategory.Courier);
			mapActor.ownerRealmId = realm.realmId;
			mapActor.category = UnitCategory.Migration;
			mapActor.subcategory = UnitSubcategory.Courier;
			mapActor.actorName = $"传令官#{mapActor.actorId}（{order.decreeId}）";
			mapActor.transport.subcategory = UnitSubcategory.Courier;
			UnitProvisioning.Equip(mapActor.transport, cost, funding, goodsDefs);
			CharacterData characterData = FindCourierLeader(realm.realmId);
			if (characterData == null)
			{
				order.state = DecreeOrderState.Failed;
				order.note = "没有可用的传令官角色";
				_mapActorManager.RemoveActor(mapActor.actorId);
				continue;
			}
			characterData.aiCategory = CharacterAICategory.Courier;
			mapActor.transport.TryAddMember(characterData.characterId, UnitRole.Courier);
			mapActor.SetLeader(characterData);
			characterData.currentSettlementAnchorId = order.fromAnchorId;
			characterData.currentTile = num;
			mapActor.SetTargetSettlement(order.targetAnchorId, order.targetTile);
			DecreePropagationSystem.AttachCourier(order, mapActor.actorId);
		}
	}

	private void TickDecreeOrders()
	{
		foreach (RealmData realm in realms.Values)
		{
			RealmDecreeState decreeState = realm.decreeState;
			if (decreeState?.orders == null || decreeState.orders.Count == 0)
			{
				continue;
			}
			for (int i = 0; i < decreeState.orders.Count; i++)
			{
				DecreeOrder decreeOrder = decreeState.orders[i];
				if (decreeOrder.state == DecreeOrderState.Enforced || decreeOrder.state == DecreeOrderState.Refused || decreeOrder.state == DecreeOrderState.Profiteering || decreeOrder.state == DecreeOrderState.Failed)
				{
					continue;
				}
				MapActor mapActor = ((decreeOrder.courierActorId >= 0 && _mapActorManager != null) ? _mapActorManager.GetActor(decreeOrder.courierActorId) : null);
				if (mapActor == null || !mapActor.IsAlive)
				{
					decreeOrder.state = DecreeOrderState.Failed;
					decreeOrder.note = "传令官失联";
					continue;
				}
				if (!settlements.TryGetValue(decreeOrder.targetAnchorId, out var value) || value == null)
				{
					decreeOrder.state = DecreeOrderState.Failed;
					decreeOrder.note = "目标聚落已不存在";
					continue;
				}
				decreeOrder.targetTile = value.tileIndex;
				if (decreeOrder.state != DecreeOrderState.Delivered)
				{
					if (mapActor.currentSettlementAnchorId != decreeOrder.targetAnchorId)
					{
						continue;
					}
					decreeOrder.state = DecreeOrderState.Delivered;
				}
				CharacterData characterData = _characterManager?.GetCharacter(decreeOrder.officialCharacterId);
				float legitimacy = ((realm.lawState != null) ? realm.lawState.legitimacy : 0f);
				float corruption = 0.5f;
				if (characterData != null)
				{
					characterData.GetEffectiveStats(out _, out _, out _, out _, out float effConspiracy, out _, out _);
					corruption = Mathf.Clamp01(effConspiracy / 100f);
				}
				float localUnrest = Mathf.Clamp01(1f - realm.stability / 100f);
				DecreePropagationSystem.Resolve(decreeOrder, characterData, legitimacy, corruption, localUnrest, new Random(decreeOrder.orderId * 7919));
				if (!decreeOrder.nextHopIssued && decreeOrder.targetAnchorId >= 0)
				{
					decreeOrder.nextHopIssued = true;
					List<DecreeOrder> list = DecreePropagationSystem.IssueToSubordinateSettlements(decreeOrder.decreeId, realm.realmId, settlements, tiles, currentDay, (int anchorId) => SettlementTile(anchorId), (int anchorId) => OfficialForSettlement(realm, anchorId), decreeOrder.targetAnchorId);
					decreeState.orders.AddRange(list);
					SpawnDecreeCouriers(realm, list);
				}
			}
		}
	}

	private void GameTick()
	{
		AdvanceTime();
		float frameMs = Time.unscaledDeltaTime * 1000f;
		float budgetMs = _budgetGovernor.NextBudget(frameMs, _simulationScheduler.PendingCount, _lastDeferred);
		_simulationScheduler.TickBudgeted(currentDay, currentYear, daysPerTick, budgetMs, () => Time.realtimeSinceStartupAsDouble, out var _, out _lastDeferred);
	}

	/// <summary>
	/// 每日推进各政权"正在推行"的法律（PendingLaw）：只有到期才 SetActiveLaw 固化，
	/// 生效前旧法始终有效；完成时写一条编年史。
	/// </summary>
	private void TickPendingLawsDaily()
	{
		if (realms == null || realms.Count == 0)
		{
			return;
		}
		List<(int realmId, string lawId)> completed = LawRealmHost.TickAllPendingLaws(realms.Values, currentDay);
		if (completed == null || completed.Count == 0)
		{
			return;
		}
		for (int i = 0; i < completed.Count; i++)
		{
			_chronicle?.Add("law_enacted", $"政权 {completed[i].realmId} 完成推行法律：{completed[i].lawId}", true, completed[i].realmId);
		}
	}

	private void RegisterSimulationSchedules()
	{
		_simulationScheduler.Clear();
		WorldMapSchedule.Register(_simulationScheduler, _worldMapSimulation);
		DisasterSchedule.Register(_simulationScheduler, _disasterSystem, _diseaseSystem, () => currentDay, () => currentYear);
		EconomySchedule.Register(_simulationScheduler, _economyManager, SimulationEvents, () => currentDay);
		BuildingSchedule.Register(_simulationScheduler, _buildingSystem);
		PopulationSchedule.Register(_simulationScheduler, PopulationTick);
		PoliticsSchedule.Register(_simulationScheduler, _politicsSimulationSystem);
		// 每日推进"正在推行"的法律：到期才固化（旧法在此之前始终有效）。
		_simulationScheduler.Register("laws.pending.daily", SimulationCadence.Daily, 26, delegate
		{
			TickPendingLawsDaily();
		});
		// 每年应用法律/法令的年度效果（daily + 年份 guard：daysPerTick>1 也不漏年；税收/人力乘子在各自结算点消费）。
		_simulationScheduler.Register("laws.effects.yearly", SimulationCadence.Daily, 24, delegate
		{
			TickLawEffectsYearly();
		});
		SettlementSchedule.Register(_simulationScheduler, _settlementSimulationSystem);
		CultureSchedule.Register(_simulationScheduler, MonthlyCultureStageEvolution);
		DiplomacySchedule.Register(_simulationScheduler, _diplomacyManager, () => currentDay);
		WarfareReligionSchedule.Register(_simulationScheduler, _combatManager, armies, _wars, _diplomacyManager.WarRules, () => currentDay, () =>
		{
			UpdateFaithFervor(currentDay);
		}, CheckGreatHolyWarSettlements, () =>
		{
			ProcessWarOutcomes(currentDay);
		});
		CharacterSchedule.Register(_simulationScheduler, _characterManager, () => currentDay, () => currentYear);
		SuccessionSchedule.Register(_simulationScheduler, () =>
		{
			CheckRulerSuccessions();
		});
		ThoughtSchedule.Register(_simulationScheduler, () =>
		{
			_thoughtManager.DailyTick(currentYear);
		});
		_simulationScheduler.Register("justice.daily", SimulationCadence.Daily, 27, (SimulationTickContext _) =>
		{
			DailyJusticeTick();
		});
		AISchedule.Register(_simulationScheduler, _aiManager, () => realms, () => tiles, _diplomacyManager, _innovationTree, _characterManager, MissionaryTick, _aiIntentExecutor, () => this);
		InnovationResearchSchedule.Register(_simulationScheduler, _innovationTree, _aiManager, () => realms, () => tiles);
		EventSchedule.Register(_simulationScheduler, ProcessEvents);
		ResearchPlanSchedule.Register(_simulationScheduler, ResearchPlans);
		PlanSchedule.Register(_simulationScheduler, _planSystem);
		SimulationTimeSchedule.Register(_simulationScheduler, AdvanceTime);
		_simulationScheduler.Register("decrees.propagation.daily", SimulationCadence.Daily, 37, (SimulationTickContext _) =>
		{
			TickDecreeOrders();
		});
		if (_sandboxSpawnSystem == null)
		{
			_sandboxSpawnSystem = new SandboxSpawnSystem(this);
		}
		_simulationScheduler.Register("sandbox.spawn.monthly", SimulationCadence.Monthly, 36, (SimulationTickContext _) =>
		{
			_sandboxSpawnSystem.MonthlyTick();
		});
	}

	private void MonthlyCultureStageEvolution()
	{
		if (currentDay % 30 == 0)
		{
			CultureStageEvolutionHost.MonthlyTick(this);
		}
	}

	private void ProcessWarOutcomes(int day)
	{
		foreach (WarState item in CombatManager.UpdateWarOutcomes(_wars, _diplomacyManager.WarRules, day))
		{
			if (item.outcome == "victory" && item.winnerId >= 0)
			{
				AddWarAchievement(item.winnerId, won: true);
				int realmId = ((item.winnerId == item.attackerId) ? item.defenderId : item.attackerId);
				AddWarAchievement(realmId, won: false);
				if (item.winnerId == item.defenderId)
				{
					AddDefensiveWin(item.winnerId);
				}
			}
			_simulationEvents.Publish(new WarEndedEvent(item.warId, item.attackerId, item.defenderId, item.winnerId, item.outcome, day));
			string text = ((item.outcome == "victory") ? (realms[item.winnerId].realmName + " 赢得战争胜利") : "双方白和");
			_chronicle?.Add("war_end", text, true, item.attackerId, item.defenderId);
			_diplomacyManager.ForcePeace(item.attackerId, item.defenderId, day, _diplomacyManager.WarRules.truceYears, text);
			EnsureOfficeHolders();
			EnsureAdminDivisions();
			if (item.outcome == "victory" && item.winnerId >= 0)
			{
				NotifyWarDefeat(item, day);
			}
		}
	}

	public SettlementData CreateCity(int realmId, int tileIndex, int founderCharId)
	{
		if (tileIndex < 0 || tileIndex >= tiles.Length)
		{
			return null;
		}
		if (!tiles[tileIndex].isLand)
		{
			return null;
		}
		ref TileData reference = ref tiles[tileIndex];
		if (reference.ownerRealmId != realmId)
		{
			return null;
		}
		int num = 1;
		foreach (int key in settlements.Keys)
		{
			if (key >= num)
			{
				num = key + 1;
			}
		}
		string text = "";
		CharacterData characterData = _characterManager?.GetCharacter(founderCharId);
		if (characterData != null && cultures != null && cultures.TryGetValue(characterData.cultureId, out var value) && ContentResolvers.Languages.TryGet(value.languageId, out var value2))
		{
			text = PlaceNameGenerator.FounderCity(characterData.firstName + characterData.lastName, value2);
		}
		if (string.IsNullOrEmpty(text))
		{
			text = "新市镇";
		}
		SettlementData settlementData = new SettlementData
		{
			anchorId = num,
			anchor = new AnchorData
			{
				anchorId = num,
				provinceId = reference.provinceId,
				tileIndex = tileIndex,
				x = 0.5f,
				y = 0.5f
			},
			settlementName = text,
			siteMode = SiteMode.SedentarySettlement,
			settlementForm = SettlementForm.City,
			isCoastal = reference.isCoast
		};
		settlementData.SetLegacyCategoryFromSiteMode();
		SettlementTypologySystem.DeriveInitialType(settlementData, reference, mapWidth, mapHeight);
		anchors[num] = settlementData.anchor;
		settlements[settlementData.anchorId] = settlementData;
		settlementData.EnsureBlocksFromLegacy();
		string text2 = ((characterData != null) ? (characterData.firstName + characterData.lastName) : "某人");
		_chronicle?.Add("city_founded", text2 + " 建立城市 " + text, true, realmId);
		return settlementData;
	}

	public SettlementData CreateWildernessSettlement(int realmId, int tileIndex, int founderCharId)
	{
		if (tileIndex < 0 || tileIndex >= tiles.Length)
		{
			return null;
		}
		if (!tiles[tileIndex].isLand)
		{
			return null;
		}
		if (tiles[tileIndex].ownerRealmId >= 0)
		{
			return null;
		}
		if (!realms.TryGetValue(realmId, out var value))
		{
			return null;
		}
		ref TileData reference = ref tiles[tileIndex];
		reference.ownerRealmId = realmId;
		value.coreTiles.Add(tileIndex);
		int num = 1;
		foreach (int key in settlements.Keys)
		{
			if (key >= num)
			{
				num = key + 1;
			}
		}
		string text = "新拓居地";
		CharacterData characterData = _characterManager?.GetCharacter(founderCharId);
		if (characterData != null && cultures != null && cultures.TryGetValue(characterData.cultureId, out var value2) && ContentResolvers.Languages.TryGet(value2.languageId, out var value3))
		{
			text = PlaceNameGenerator.FounderCity(characterData.firstName + characterData.lastName, value3);
		}
		SettlementData settlementData = new SettlementData
		{
			anchorId = num,
			anchor = new AnchorData
			{
				anchorId = num,
				provinceId = reference.provinceId,
				tileIndex = tileIndex,
				x = 0.5f,
				y = 0.5f
			},
			settlementName = text,
			siteMode = SiteMode.SedentarySettlement,
			settlementForm = SettlementForm.Village,
			isCoastal = reference.isCoast
		};
		settlementData.SetLegacyCategoryFromSiteMode();
		SettlementTypologySystem.DeriveInitialType(settlementData, reference, mapWidth, mapHeight);
		anchors[num] = settlementData.anchor;
		settlements[settlementData.anchorId] = settlementData;
		settlementData.EnsureBlocksFromLegacy();
		_chronicle?.Add("wilderness_founded", value.realmName + " 在无主之地建立 " + text, true, realmId);
		return settlementData;
	}

	public bool CreateHolySite(int faithId, int tileIndex)
	{
		if (tileIndex < 0 || tileIndex >= tiles.Length)
		{
			return false;
		}
		FaithSystem faithSystem = GetFaithSystem(faithId);
		if (faithSystem == null)
		{
			return false;
		}
		if (faithSystem.holySiteTileIndices.Contains(tileIndex))
		{
			return false;
		}
		faithSystem.holySiteTileIndices.Add(tileIndex);
		_chronicle?.Add("religion", $"{faithSystem.faithName} 确立新的圣地（地块 {tileIndex}）", true);
		return true;
	}

	public void OnWarBetweenFaiths(int faithA, int faithB)
	{
		if (faithA != faithB)
		{
			_simulationEvents.Publish(new FaithConflictTriggeredEvent(faithA, faithB, currentDay));
		}
	}

	public bool DeclareWar(int attackerId, int defenderId, string reason)
	{
		return DeclareWar(attackerId, defenderId, reason, ResolveSupremeRulerId(attackerId));
	}

	private bool DeclareWarCore(int attackerId, int defenderId, string reason)
	{
		if (!_diplomacyManager.DeclareWar(attackerId, defenderId, reason))
		{
			return false;
		}
		WarState warState = new WarState(_nextWarId++, attackerId, defenderId, currentDay);
		_wars.Add(warState);
		_simulationEvents.Publish(new WarDeclaredEvent(warState.warId, attackerId, defenderId, currentDay));
		_chronicle?.Add("war", realms[attackerId].realmName + " 对 " + realms[defenderId].realmName + " 宣战：" + reason, true, attackerId, defenderId);
		if (attackerId >= 0 && defenderId >= 0 && attackerId < realms.Count && defenderId < realms.Count)
		{
			int stateReligionId = realms[attackerId].stateReligionId;
			int stateReligionId2 = realms[defenderId].stateReligionId;
			if (stateReligionId >= 0 && stateReligionId2 >= 0 && stateReligionId != stateReligionId2)
			{
				OnWarBetweenFaiths(stateReligionId, stateReligionId2);
			}
		}
		return true;
	}

	/// <summary>带主体的宣战：先过 ActionGate（能力∧职权），再走原宣战流程。actor 为发起者角色。</summary>
	public bool DeclareWar(int attackerId, int defenderId, string reason, int actorCharacterId)
	{
		if (attackerId < 0 || attackerId >= realms.Count)
		{
			return false;
		}
		IActionGate gate = BuildActionGate();
		ActionContext context = new ActionContext(actorCharacterId, attackerId, ActionIds.DeclareWar, targetRealmId: defenderId);
		if (!gate.Check(in context).Allowed)
		{
			return false;
		}
		return DeclareWarCore(attackerId, defenderId, reason);
	}

	private int ResolveSupremeRulerId(int realmId)
	{
		if (realmId < 0 || realmId >= realms.Count)
		{
			return -1;
		}
		return realms[realmId].GetSupremeRulerId();
	}

	private IActionGate BuildActionGate()
	{
		return new GateService(new NoCapabilityGate(), BuildRealmAuthorityResolver(), new DiplomacyLegalityProvider(_diplomacyManager), new RealmTreasuryProvider((int realmId) => (realmId >= 0 && realmId < realms.Count) ? realms[realmId].treasury : 0f, 200f));
	}

	private IAuthorityResolver BuildRealmAuthorityResolver()
	{
		return new RealmAuthorityResolver((int realmId, out int rulerId, out int regentId) =>
		{
			if (realmId < 0 || realmId >= realms.Count)
			{
				rulerId = -1;
				regentId = -1;
				return false;
			}
			RealmData realm = realms[realmId];
			rulerId = ((realm.monarchId >= 0) ? realm.monarchId : realm.consulId);
			regentId = ((realm.inInterregnum && realm.regentCharacterId >= 0) ? realm.regentCharacterId : (-1));
			return rulerId >= 0 || regentId >= 0;
		});
	}

	public bool DeclareGreatHolyWar(int faithId, int callerRealmId, int targetRealmId, int targetTile, string reason)
	{
		FaithSystem faithSystem = GetFaithSystem(faithId);
		if (faithSystem == null)
		{
			return false;
		}
		if (!faithSystem.CanDeclareGreatHolyWar())
		{
			return false;
		}
		if (callerRealmId < 0 || callerRealmId >= realms.Count)
		{
			return false;
		}
		if (targetRealmId < 0 || targetRealmId >= realms.Count)
		{
			return false;
		}
		GreatHolyWarState greatHolyWarState = GreatHolyWarSystem.Declare(faithId, callerRealmId, targetRealmId, targetTile, currentDay, hasLeader: true, faithSystem.fervor);
		if (greatHolyWarState == null)
		{
			return false;
		}
		IActionGate gate = BuildActionGate();
		ActionContext gateContext = new ActionContext(ResolveSupremeRulerId(callerRealmId), callerRealmId, ActionIds.DeclareWar, targetRealmId: targetRealmId);
		if (!gate.Check(in gateContext).Allowed)
		{
			return false;
		}
		if (!_diplomacyManager.DeclareWar(callerRealmId, targetRealmId, reason))
		{
			return false;
		}
		WarState warState = new WarState(_nextWarId++, callerRealmId, targetRealmId, currentDay);
		_wars.Add(warState);
		GreatHolyWarSystem.BindWar(greatHolyWarState, warState.warId);
		_chronicle?.Add("religion", realms[callerRealmId].realmName + " 号召大圣战讨伐 " + realms[targetRealmId].realmName, true, callerRealmId, targetRealmId);
		return true;
	}

	public Navy CreateNavy(int ownerRealmId, int commanderId, int portTileIndex)
	{
		int navyId = _nextNavyId++;
		Navy navy = new Navy
		{
			navyId = navyId,
			navyName = "舰队" + navyId,
			ownerRealmId = ownerRealmId,
			commanderId = commanderId,
			homePortTile = portTileIndex,
			currentTileIndex = portTileIndex
		};
		navies[navy.navyId] = navy;
		return navy;
	}

	public Army CreateArmy(int ownerRealmId, int commanderId, int tileIndex)
	{
		Army army = new Army
		{
			armyId = _nextArmyId++,
			armyName = realms[ownerRealmId].realmName + "军",
			ownerRealmId = ownerRealmId,
			commanderId = commanderId,
			currentTileIndex = tileIndex,
			transport = new TransportUnit
			{
				type = TransportUnitType.Army
			}
		};
		armies[army.armyId] = army;
		MapUnitRegistry.RegisterArmy(army);
		return army;
	}

	public List<CrimeType> MatchCrimes(CrimeBehavior behavior, CrimeTargetClass? target = null, string context = null)
	{
		return CrimeRegistry.MatchByBehavior(behavior, target, context);
	}

	public TrialResult TryTrialCrime(int realmId, CrimeType crime, int suspectCharacterId)
	{
		JudicialSystem orCreateJudicialSystem = GetOrCreateJudicialSystem(realmId);
		if (orCreateJudicialSystem == null)
		{
			return default;
		}
		realms.TryGetValue(realmId, out var value);
		TrialResult result = orCreateJudicialSystem.TrialCrime(crime, suspectCharacterId, realmId, value);
		if (_chronicle != null && result.verdict != VerdictType.Dismissed)
		{
			string eventType = ((result.verdict == VerdictType.Guilty) ? "crime_guilty" : "crime_acquitted");
			_chronicle.Add(eventType, string.Format("{0}司法审结：{1}（{2}）", value?.realmName ?? "某政权", crime, result.verdict), false, realmId);
		}
		return result;
	}

	public void RebuildJudicialLaws(int realmId)
	{
		if (_judicialSystems.TryGetValue(realmId, out var value) && realms != null && realms.TryGetValue(realmId, out var value2))
		{
			value.laws = LawCatalog.BuildCorpus(value2.laws);
			value.source = ((value2.laws != null) ? value2.laws.legalSource : LawSource.Customary);
		}
	}

	private void DailyJusticeTick()
	{
		foreach (JudicialSystem value in _judicialSystems.Values)
		{
			value.DailyTick();
		}
	}
}
}
