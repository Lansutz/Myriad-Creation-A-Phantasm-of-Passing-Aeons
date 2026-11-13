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
	private const int FamineCooldownDays = 30;

	private int[] _lastFamineAbsoluteDay;

	private void EnsureFamineCooldownBuffer()
	{
		if (_lastFamineAbsoluteDay == null || _lastFamineAbsoluteDay.Length != tiles.Length)
		{
			_lastFamineAbsoluteDay = new int[tiles.Length];
			for (int i = 0; i < _lastFamineAbsoluteDay.Length; i++)
			{
				_lastFamineAbsoluteDay[i] = -100000;
			}
		}
	}

	private void PopulationTick()
	{
		ConcurrentQueue<GameEvent> parallelEvents = new ConcurrentQueue<GameEvent>();
		EnsureFamineCooldownBuffer();
		int absoluteDay = currentYear * 365 + currentDay;
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
						PopulationLedger.Scale(tiles[i].populationBlocks, j, 1f + num * (float)daysPerTick);
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
						// 同一地块 30 天内只报一次饥荒：否则长跑后几乎全图每日重复入队，
						// events.daily 会从 0.4ms 漂到 100ms+（P0.4 一年档实测）。
						int lastDay = _lastFamineAbsoluteDay[i];
						if (absoluteDay - lastDay >= FamineCooldownDays)
						{
							_lastFamineAbsoluteDay[i] = absoluteDay;
							parallelEvents.Enqueue(new GameEvent
							{
								eventType = GameEventType.Famine,
								tileIndex = i,
								severity = 50f
							});
						}
					}
				}
			}
		});
		GameEvent result;
		while (parallelEvents.TryDequeue(out result))
		{
			EnqueueEvent(result);
		}
		// D40：人口块变动后重算聚落派生快照（唯一权威仍是地块人口块）。
		SettlementPopulationSystem.RecomputeAll(this);
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
		if (_characterManager == null) return;
		int absoluteDay = OfficeSystem.AbsoluteDay(currentYear, currentDay);
		bool anyVacancy = false;
		for (int i = 0; i < realms.Count; i++)
		{
			RealmData realmData = realms[i];
			if (realmData == null) continue;
			OfficeSystem.VacateExpired(realmData, _characterManager, absoluteDay);
			if (HasOfficeVacancy(realmData)) anyVacancy = true;
		}
		// 官职已满时不扫全图人口/领土，避免百年后人物与地块规模放大日 tick。
		if (!anyVacancy) return;

		Dictionary<int, OfficeRealmContext> contexts = BuildOfficeContexts();
		for (int i = 0; i < realms.Count; i++)
		{
			RealmData realmData = realms[i];
			if (realmData == null) continue;
			contexts.TryGetValue(realmData.realmId, out OfficeRealmContext context);
			OfficeSystem.FillVacancies(realmData, _characterManager, absoluteDay, context);
		}
	}

	private static bool HasOfficeVacancy(RealmData realm)
	{
		foreach (OfficeDef def in OfficeCatalog.All.Values)
		{
			if (!def.enabled) continue;
			if (!realm.officeHolders.TryGetValue(def.slot, out int holder) || holder < 0) return true;
		}
		return false;
	}

	/// <summary>一次 O(N) 扫描给出各政权的实际控制地块/人口，供官职制度门槛判定。</summary>
	private Dictionary<int, OfficeRealmContext> BuildOfficeContexts()
	{
		Dictionary<int, int> tileCounts = new Dictionary<int, int>();
		Dictionary<int, float> populations = new Dictionary<int, float>();
		for (int i = 0; i < tiles.Length; i++)
		{
			int realmId = tiles[i].ownerRealmId;
			if (!tiles[i].exists || realmId < 0) continue;
			tileCounts.TryGetValue(realmId, out int count);
			tileCounts[realmId] = count + 1;
			populations.TryGetValue(realmId, out float population);
			populations[realmId] = population + PopulationLedger.Total(tiles[i]);
		}
		Dictionary<int, OfficeRealmContext> result = new Dictionary<int, OfficeRealmContext>();
		foreach (RealmData realm in realms.Values)
		{
			if (realm == null) continue;
			int cultureStage = 0;
			if (realm.primaryCultureId >= 0 && cultures.TryGetValue(realm.primaryCultureId, out CultureData culture) && culture != null)
			{
				cultureStage = (int)culture.stage;
			}
			result[realm.realmId] = new OfficeRealmContext
			{
				ownedTiles = tileCounts.GetValueOrDefault(realm.realmId, 0),
				population = populations.GetValueOrDefault(realm.realmId, 0f),
				centralization = realm.centralization,
				cultureStage = cultureStage
			};
		}
		return result;
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
		ArmySchedule.Register(_simulationScheduler, armies, unitDefs, tiles, _seaLandGenerator, TryOccupyByArmy);
		NavySchedule.Register(_simulationScheduler, navies);
		SettlementSchedule.Register(_simulationScheduler, _settlementSimulationSystem);
		// 影响力场：Compute 只读、Apply 只写；同一 tick 内两相锁步。
		InfluenceField pendingInfluence = null;
		_simulationScheduler.RegisterPhased("settlement-influence.daily", SimulationCadence.Daily, 18,
			(SimulationTickContext _) =>
			{
				pendingInfluence = SettlementInfluenceSystem.Calculate(settlements, tiles, mapWidth, mapHeight);
			},
			(SimulationTickContext _) =>
			{
				SettlementInfluenceSystem.Apply(settlements, pendingInfluence);
			});
		CultureSchedule.Register(_simulationScheduler, MonthlyCultureStageEvolution);
		DiplomacySchedule.Register(_simulationScheduler, _diplomacyManager, () => currentDay);
		WarfareReligionSchedule.Register(_simulationScheduler, _combatManager, armies, _wars, _diplomacyManager.WarRules, () => currentDay, () =>
		{
			UpdateFaithFervor(currentDay);
		}, CheckGreatHolyWarSettlements, () =>
		{
			ProcessWarOutcomes(currentDay);
		});
		CharacterSchedule.Register(_simulationScheduler, _characterManager, () => currentDay, () => currentYear,
			(int tileIndex) => (tileIndex >= 0 && tileIndex < tiles.Length) ? tiles[tileIndex].annualTemp : 20f);
		SuccessionSchedule.Register(_simulationScheduler, () =>
		{
			CheckRulerSuccessions();
		});
		ThoughtSchedule.Register(_simulationScheduler, () =>
		{
			_thoughtManager.DailyTick(currentYear);
		});
		// 官职：每日补缺/任期到期，并按日折算俸禄（国库不足则当日不付）。
		// 已满时只遍历槽位；只有空槽才扫候选，避免百年人物表放大成本。
		_simulationScheduler.Register("offices.daily", SimulationCadence.Daily, 12, (SimulationTickContext ctx) =>
		{
			EnsureOfficeHolders();
			if (_characterManager != null)
			{
				for (int i = 0; i < realms.Count; i++)
				{
					if (realms[i] != null) OfficeSystem.PaySalaries(realms[i], _characterManager, ctx.deltaDays);
				}
			}
		});
		// 政权旗帜（P3.4）：每月按条件刷新；无旗是合法状态。
		_simulationScheduler.Register("polity.banners.monthly", SimulationCadence.Monthly, 31, (SimulationTickContext _) =>
		{
			for (int i = 0; i < realms.Count; i++)
			{
				if (realms[i] != null) BannerSystem.Ensure(realms[i], _characterManager, _innovationTree);
			}
		});
		// 远征（P4.1）：每日推进路线、补给、拦截与知识揭示；计划层只读远征状态。
		_simulationScheduler.Register("expeditions.daily", SimulationCadence.Daily, 29, (SimulationTickContext _) =>
		{
			if (expeditions == null) return;
			foreach (Expedition expedition in expeditions.Values)
			{
				ExpeditionSystem.Tick(expedition, tiles, mapWidth, mapHeight, Knowledge, armies, _chronicle);
			}
		});
		// 地图知识（S19）：军队按斥候范围揭示；所有权地块在生成时已播种。
		_simulationScheduler.Register("knowledge.scouts.monthly", SimulationCadence.Monthly, 30, (SimulationTickContext _) =>
		{
			Knowledge.EnsureCapacity(tiles.Length);
			Knowledge.SeedOwnership(tiles, mapWidth, mapHeight);
			foreach (Army army in armies.Values)
			{
				if (army != null && army.ownerRealmId >= 0)
				{
					Knowledge.RevealRadius(army.ownerRealmId, army.currentTileIndex, Math.Max(1, army.scoutRange), mapWidth, mapHeight);
				}
			}
		});
		// 生态演替：每月推进植被与野生群落（聚合口径），确定性、无随机。
		_simulationScheduler.Register("ecology.succession.monthly", SimulationCadence.Monthly, 45, (SimulationTickContext ctx) =>
		{
			EcosystemSystem.Tick(tiles, ctx.deltaDays, ctx.year);
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
		// 教职接线：宗教领袖死亡/违规才更替，月结一次即可。
		_simulationScheduler.Register("faith.leadership.monthly", SimulationCadence.Monthly, 38, (SimulationTickContext _) =>
		{
			EnsureFaithLeadership();
		});
		// 信息滞后：非本政权人物超过 3 年无接触则从"已知"移除。
		_simulationScheduler.Register("knowledge.decay.monthly", SimulationCadence.Monthly, 44, (SimulationTickContext _) =>
		{
			CharacterKnowledgeRegistry.PruneStale(currentDay, 1095);
		});
		// 文化传播：人口块向政权主文化同化，进度取既有阻力公式。
		_simulationScheduler.Register("culture.transmission.monthly", SimulationCadence.Monthly, 39, (SimulationTickContext _) =>
		{
			CultureTransmissionSystem.MonthlyTick(realms, cultures, tiles);
		});
		// 文化分裂：有故土的文化在异政权内抗拒同化且占比足够时，分出一个子文化。
		_simulationScheduler.Register("culture.split.monthly", SimulationCadence.Monthly, 41, (SimulationTickContext _) =>
		{
			CultureSplitSystem.MonthlyTick(realms, cultures, tiles, currentDay, (parentId, childId, realmId) =>
			{
				string parentName = cultures.TryGetValue(parentId, out var parent) ? parent?.cultureName : null;
				string childName = cultures.TryGetValue(childId, out var child) ? child?.cultureName : null;
				_chronicle?.Add("culture", (parentName ?? ("#" + parentId)) + " 分出 " + (childName ?? ("#" + childId)), true, realmId);
			});
		});
		// 宗教局势：同教诸 faith 的分歧度决定阶段；达分裂阈值自动分出宗派。
		_simulationScheduler.Register("faith.stage.monthly", SimulationCadence.Monthly, 43, (SimulationTickContext _) =>
		{
			TickReligionStages();
		});
		// 教派状态：正统越界/分歧度月结，结果写 Denomination.status。
		_simulationScheduler.Register("faith.orthodoxy.monthly", SimulationCadence.Monthly, 42, (SimulationTickContext _) =>
		{
			TickDenominationStatuses();
		});
		// 文化融合：少数文化被主文化吸收，主文化吸收其可兼容传统。
		_simulationScheduler.Register("culture.fusion.monthly", SimulationCadence.Monthly, 40, (SimulationTickContext _) =>
		{
			CultureFusionSystem.MonthlyTick(realms, cultures, tiles, (minorityId, primaryId) =>
			{
				string minorityName = cultures.TryGetValue(minorityId, out var minority) ? minority?.cultureName : null;
				string primaryName = cultures.TryGetValue(primaryId, out var primary) ? primary?.cultureName : null;
				_chronicle?.Add("culture", (minorityName ?? ("#" + minorityId)) + " 融入 " + (primaryName ?? ("#" + primaryId)), true);
			});
		});
		if (_sandboxSpawnSystem == null)
		{
			_sandboxSpawnSystem = new SandboxSpawnSystem(this);
		}
		_simulationScheduler.Register("sandbox.spawn.monthly", SimulationCadence.Monthly, 36, (SimulationTickContext _) =>
		{
			_sandboxSpawnSystem.MonthlyTick();
		});
		// 政权自然形成：先有聚落、人口与剩余，才有国家；每月最多一个，确定性排序。
		_simulationScheduler.Register("state.formation.monthly", SimulationCadence.Monthly, 37, (SimulationTickContext _) =>
		{
			StateFormationSystem.MonthlyTick(this);
		});
	}

	private void MonthlyCultureStageEvolution()
	{
		if (currentDay % 30 == 0)
		{
			CultureStageEvolutionHost.MonthlyTick(this);
		}
	}

	/// <summary>P1.5：军队进入交战方领土后标记军事占领（所有权仍由和约决定）。</summary>
	private void TryOccupyByArmy(Army army)
	{
		if (army == null || tiles == null || army.currentTileIndex < 0 || army.currentTileIndex >= tiles.Length)
		{
			return;
		}
		ref TileData tile = ref tiles[army.currentTileIndex];
		int owner = tile.ownerRealmId;
		if (owner >= 0 && owner != army.ownerRealmId && tile.isLand && _diplomacyManager.AreAtWar(army.ownerRealmId, owner))
		{
			_politicalManager.OccupyTile(army.currentTileIndex, army.ownerRealmId);
			ApplyOccupationPressure(army.currentTileIndex, army.ownerRealmId);
		}
	}

	/// <summary>P1.1 毁灭端口：敌境聚落被连续占领 90 天后拆毁。</summary>
	private void ApplyOccupationPressure(int tileIndex, int occupyingRealmId)
	{
		const int OccupationRuinDays = 90;
		if (settlements == null)
		{
			return;
		}
		SettlementData target = null;
		foreach (SettlementData settlement in settlements.Values)
		{
			if (settlement != null && settlement.tileIndex == tileIndex && !settlement.IsRuined)
			{
				target = settlement;
				break;
			}
		}
		if (target == null)
		{
			return;
		}
		if (tiles[tileIndex].occupyingRealmId == occupyingRealmId)
		{
			target.occupationPressureDays++;
			if (target.occupationPressureDays >= OccupationRuinDays)
			{
				string result = SettlementDestructionSystem.DestroySettlement(this, target.anchorId, DestructionMethod.Dismantle, occupyingRealmId);
				if (!string.IsNullOrEmpty(result))
				{
					_chronicle?.Add("settlement_destroyed", target.settlementName + " 被长期占领后拆毁", true, occupyingRealmId);
				}
			}
		}
		else
		{
			target.occupationPressureDays = 0;
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
			int truceYears = ((_diplomacyManager.WarRules != null) ? _diplomacyManager.WarRules.truceYears : 5);
			Treaty treaty;
			if (item.outcome == "victory" && item.winnerId >= 0)
			{
				int loserId = ((item.winnerId == item.attackerId) ? item.defenderId : item.attackerId);
				List<WarGoal> goals = ((item.winnerId == item.attackerId) ? item.warGoals : null);
				treaty = WarJustificationSystem.GeneratePeaceTreaty(item.winnerId, loserId, item.GetScore(item.winnerId), goals, day, truceYears);
			}
			else
			{
				treaty = WarJustificationSystem.GenerateWhitePeace(item.attackerId, item.defenderId, day, truceYears);
			}
			_diplomacyManager.SignPeaceTreaty(item.attackerId, item.defenderId, treaty, day, text);
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

	/// <summary>诊断/测试用：立即跑一次聚落类型学日结（等级推进 + 废弃压力）。</summary>
	public int SettlementTypologyTickCount => _settlementSimulationSystem?.TypologyTickCount ?? -1;

	public int SettlementTypologySettlementCount => _settlementSimulationSystem?.TypologySettlementCount ?? -1;

	public int SettlementTypologySedentaryCount => _settlementSimulationSystem?.TypologySedentaryCount ?? -1;

	public bool SettlementTypologyHasSettlement(int anchorId) => _settlementSimulationSystem?.HasSettlement(anchorId) ?? false;

	public void RunSettlementTypologyNow()
	{
		_settlementSimulationSystem?.TypologyDailyTick(default);
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
		return DeclareWar(attackerId, defenderId, reason, ResolveSupremeRulerId(attackerId), WarDecisionMode.ConsultAndAbide);
	}

	/// <summary>简单宣战（可指定议事模式；测试/强制路径用 SkipConsultation）。</summary>
	public bool DeclareWar(int attackerId, int defenderId, string reason, WarDecisionMode mode)
	{
		return DeclareWar(attackerId, defenderId, reason, ResolveSupremeRulerId(attackerId), mode);
	}

	private static bool IsReligiousCasusBelli(GameEnums.CasusBelliType type)
	{
		return type == GameEnums.CasusBelliType.Crusade || type == GameEnums.CasusBelliType.ReligiousConflict;
	}

	/// <summary>P1.3：国教是否持和平主义教义（禁止圣战/宗教战争）。</summary>
	public bool IsStateFaithPacifist(int realmId)
	{
		if (realms == null || !realms.TryGetValue(realmId, out RealmData realm) || realm == null)
		{
			return false;
		}
		FaithSystem faith = GetFaithSystem(realm.stateReligionId);
		if (faith == null || !MyriadCreation.Simulation.ContentHost.ContentResolvers.Religions.TryGet(faith.religionId, out MyriadCreation.Content.Definitions.ReligionDef religion))
		{
			return false;
		}
		return MyriadCreation.Content.Definitions.DoctrineBindingHost.Resolve(religion).pacifism;
	}

	private bool DeclareWarCore(int attackerId, int defenderId, string reason, WarDecisionMode mode = WarDecisionMode.ConsultAndAbide)
	{
		WarRules rules = _diplomacyManager.WarRules ?? WarRules.Default();
		CouncilConsultationResult council = CouncilConsultationSystem.Consult(this, attackerId, null);
		if (!ResolveCouncilDecision(council, mode))
		{
			return false;
		}
		ApplyCouncilConsequences(realms[attackerId], council, mode, rules);
		if (!_diplomacyManager.DeclareWar(attackerId, defenderId, reason))
		{
			return false;
		}
		WarState warState = CreateWarState(attackerId, defenderId, reason);
		warState.councilConsulted = council.hasCouncil;
		warState.councilSkipped = !council.hasCouncil;
		warState.councilSupporters = council.supporters;
		warState.councilOpponents = council.opponents;
		warState.councilApproval = council.approval;
		_chronicle?.Add("war", realms[attackerId].realmName + " 对 " + realms[defenderId].realmName + " 宣战：" + reason, true, attackerId, defenderId);
		return true;
	}

	/// <summary>
	/// 战争运行态的唯一创建入口。战略层真源是 <see cref="WarState"/>（参战方 / 宣战理由 / 战争目标），
	/// 外交关系上的 CB 与目标是投影，不反向充当战争状态。普通宣战、带理由宣战、大圣战都必须走这里。
	/// </summary>
	private WarState CreateWarState(int attackerId, int defenderId, string reason, CasusBelli casusBelli = null, List<WarGoal> warGoals = null)
	{
		WarState warState = new WarState(_nextWarId++, attackerId, defenderId, currentDay)
		{
			reason = reason ?? "",
			casusBelli = casusBelli
		};
		if (warGoals != null)
		{
			foreach (WarGoal warGoal in warGoals)
			{
				if (warGoal == null)
				{
					continue;
				}
				warGoal.attackerRealmId = attackerId;
				warGoal.defenderRealmId = defenderId;
				warState.warGoals.Add(warGoal);
			}
		}
		_wars.Add(warState);
		_simulationEvents.Publish(new WarDeclaredEvent(warState.warId, attackerId, defenderId, currentDay));
		if (attackerId >= 0 && defenderId >= 0 && attackerId < realms.Count && defenderId < realms.Count)
		{
			int stateReligionId = realms[attackerId].stateReligionId;
			int stateReligionId2 = realms[defenderId].stateReligionId;
			if (stateReligionId >= 0 && stateReligionId2 >= 0 && stateReligionId != stateReligionId2)
			{
				OnWarBetweenFaiths(stateReligionId, stateReligionId2);
			}
		}
		return warState;
	}

	/// <summary>
	/// 带宣战理由与战争目标的正式宣战。CB / 目标同时写入战争运行态与外交投影。
	/// 默认先开战争议事：多数反对则作罢；`ConsultAndOverride` 强推、`SkipConsultation` 绕过议事都产生政治代价。
	/// </summary>
	public bool DeclareWarWithJustification(int attackerId, int defenderId, CasusBelli casusBelli, WarGoal warGoal, string reason = "", WarDecisionMode mode = WarDecisionMode.ConsultAndAbide)
	{
		return DeclareWarWithJustification(attackerId, defenderId, casusBelli, warGoal, reason, ResolveSupremeRulerId(attackerId), mode);
	}

	private bool DeclareWarWithJustification(int attackerId, int defenderId, CasusBelli casusBelli, WarGoal warGoal, string reason, int actorCharacterId, WarDecisionMode mode)
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
		if (casusBelli != null && !casusBelli.IsValid(currentDay))
		{
			casusBelli = null;
		}
		WarRules rules = _diplomacyManager.WarRules ?? WarRules.Default();
		if (casusBelli != null && IsStateFaithPacifist(attackerId) && IsReligiousCasusBelli(casusBelli.type))
		{
			return false;
		}
		if (warGoal != null)
		{
			if (casusBelli != null && !WarJustificationSystem.GetSupportedWarGoals(casusBelli.type).Contains(warGoal.type))
			{
				return false;
			}
			if (!IsWarGoalScopeValid(warGoal, attackerId, defenderId, out _))
			{
				return false;
			}
			if (!TryAuthorizeWarGoal(realms[attackerId], warGoal, rules, out int tierGap))
			{
				return false;
			}
			ApplyWarAimPoliticalCost(realms[attackerId], rules, tierGap, escalation: true);
		}
		CouncilConsultationResult council = CouncilConsultationSystem.Consult(this, attackerId, warGoal);
		if (!ResolveCouncilDecision(council, mode))
		{
			return false;
		}
		ApplyCouncilConsequences(realms[attackerId], council, mode, rules);
		if (!_diplomacyManager.DeclareWarWithJustification(attackerId, defenderId, casusBelli, warGoal, reason))
		{
			return false;
		}
		List<WarGoal> warGoals = (warGoal != null) ? new List<WarGoal> { warGoal } : null;
		WarState warState = CreateWarState(attackerId, defenderId, string.IsNullOrEmpty(reason) ? (casusBelli != null ? WarJustificationSystem.GetCBDescription(casusBelli.type) : "无借口宣战") : reason, casusBelli, warGoals);
		warState.councilConsulted = mode != WarDecisionMode.SkipConsultation && council.hasCouncil;
		warState.councilSkipped = mode == WarDecisionMode.SkipConsultation || !council.hasCouncil;
		warState.councilOverridden = mode == WarDecisionMode.ConsultAndOverride && council.hasCouncil && !council.MajoritySupport;
		warState.councilSupporters = council.supporters;
		warState.councilOpponents = council.opponents;
		warState.councilApproval = council.approval;
		return true;
	}

	/// <summary>
	/// 中途变更战争目标（统治者层）：理由（CB）固定不变，目标只能落在原 CB 授权范围内，
	/// 且必须满足接壤/目标类型约束；威望只看等级门槛，不扣点数。
	/// 等级不足时只有在政权仍稳、中央仍有权威时才能强推，代价落在稳定度、不安与廷臣好感上。
	/// </summary>
	public bool ReviseWarGoal(int warId, WarGoal proposedGoal, WarDecisionMode mode = WarDecisionMode.ConsultAndAbide)
	{
		WarState war = _wars.Find((WarState w) => w.warId == warId);
		if (war == null || war.ended || proposedGoal == null)
		{
			return false;
		}
		WarRules rules = _diplomacyManager.WarRules ?? WarRules.Default();
		if (war.lastWarGoalChangeDay >= 0 && currentDay - war.lastWarGoalChangeDay < rules.warGoalRevisionCooldownDays)
		{
			return false;
		}
		WarGoal currentGoal = (war.warGoals.Count > 0) ? war.warGoals[0] : null;
		if (!WarJustificationSystem.CanChangeWarGoal(war.casusBelli, currentGoal, proposedGoal, out _))
		{
			return false;
		}
		if (!IsWarGoalScopeValid(proposedGoal, war.attackerId, war.defenderId, out _))
		{
			return false;
		}
		float targetScore = ((proposedGoal.targetWarScore > 0f) ? proposedGoal.targetWarScore : WarJustificationSystem.GetDefaultTargetWarScore(proposedGoal.type));
		bool escalation = currentGoal == null || targetScore > currentGoal.targetWarScore;
		RealmData attacker = realms[war.attackerId];
		if (!TryAuthorizeWarGoal(attacker, proposedGoal, rules, out int tierGap))
		{
			return false;
		}
		CouncilConsultationResult council = CouncilConsultationSystem.Consult(this, war.attackerId, proposedGoal);
		if (!ResolveCouncilDecision(council, mode))
		{
			return false;
		}
		ApplyCouncilConsequences(attacker, council, mode, rules);
		war.lastWarGoalRevisionSupporters = council.supporters;
		war.lastWarGoalRevisionOpponents = council.opponents;
		ApplyWarAimPoliticalCost(attacker, rules, tierGap, escalation);
		proposedGoal.attackerRealmId = war.attackerId;
		proposedGoal.defenderRealmId = war.defenderId;
		proposedGoal.targetWarScore = targetScore;
		proposedGoal.isPrimaryGoal = true;
		war.warGoals.Clear();
		war.warGoals.Add(proposedGoal);
		war.lastWarGoalChangeDay = currentDay;
		war.warGoalRevisionCount++;
		DiplomaticRelation relation = _diplomacyManager.GetRelation(war.attackerId, war.defenderId);
		if (relation != null)
		{
			relation.activeWarGoals.Clear();
			relation.activeWarGoals.Add(proposedGoal);
		}
		_chronicle?.Add("war_goal", realms[war.attackerId].realmName + " 将战争目标改为 " + proposedGoal.type, true, war.attackerId, war.defenderId);
		return true;
	}

	/// <summary>目标范围合法性：领土类目标必须指向守方地块且与攻方接壤；区域目标必须与攻方接壤。</summary>
	private bool IsWarGoalScopeValid(WarGoal goal, int attackerId, int defenderId, out string reason)
	{
		reason = "ok";
		if (goal == null)
		{
			return true;
		}
		if (attackerId < 0 || defenderId < 0)
		{
			reason = "no_realm";
			return false;
		}
		switch (goal.type)
		{
		case GameEnums.WarGoalType.ConquerTerritory:
		case GameEnums.WarGoalType.BorderAdjustment:
		case GameEnums.WarGoalType.ConvertReligion:
			if (!IsTileOwnedBy(goal.targetTileIndex, defenderId))
			{
				reason = "target_not_defender_tile";
				return false;
			}
			if (!IsTileAdjacentToRealm(goal.targetTileIndex, attackerId))
			{
				reason = "target_not_adjacent";
				return false;
			}
			return true;
		case GameEnums.WarGoalType.ConquerRegion:
			if (goal.targetRegionId < 0)
			{
				reason = "no_region";
				return false;
			}
			if (!DoesRegionBorderRealm(goal.targetRegionId, attackerId, defenderId))
			{
				reason = "region_not_adjacent";
				return false;
			}
			return true;
		default:
			return true;
		}
	}

	private bool IsTileOwnedBy(int tileIndex, int realmId)
	{
		return tiles != null && tileIndex >= 0 && tileIndex < tiles.Length && tiles[tileIndex].ownerRealmId == realmId;
	}

	private bool IsTileAdjacentToRealm(int tileIndex, int realmId)
	{
		if (tiles == null || mapWidth <= 0 || tileIndex < 0 || tileIndex >= tiles.Length)
		{
			return false;
		}
		int x = tileIndex % mapWidth;
		int y = tileIndex / mapWidth;
		for (int dy = -1; dy <= 1; dy++)
		{
			int ny = y + dy;
			if (ny < 0 || ny >= mapHeight)
			{
				continue;
			}
			for (int dx = -1; dx <= 1; dx++)
			{
				if (dx == 0 && dy == 0)
				{
					continue;
				}
				int nx = x + dx;
				if (nx < 0 || nx >= mapWidth)
				{
					continue;
				}
				int neighbor = ny * mapWidth + nx;
				if (tiles[neighbor].ownerRealmId == realmId)
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool DoesRegionBorderRealm(int regionId, int attackerId, int defenderId)
	{
		if (tiles == null) return false;
		for (int i = 0; i < tiles.Length; i++)
		{
			if (tiles[i].regionId == regionId && tiles[i].ownerRealmId == defenderId && IsTileAdjacentToRealm(i, attackerId))
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>威望等级门槛：够级直接通过；不够级只有在政权仍稳、中央仍有权威时才能强推。</summary>
	private bool TryAuthorizeWarGoal(RealmData realm, WarGoal goal, WarRules rules, out int tierGap)
	{
		tierGap = 0;
		if (realm == null || goal == null)
		{
			return false;
		}
		PrestigeTier required = PrestigeTiers.RequiredFor(goal.type);
		PrestigeTier current = PrestigeTiers.Of(realm.prestige);
		if (current >= required)
		{
			return true;
		}
		tierGap = (int)required - (int)current;
		return realm.stability >= rules.warGoalForceMinStability && realm.centralization >= rules.warGoalForceMinCentralization;
	}

	/// <summary>改目标的代价落在政治体上：稳定度、不安、廷臣对统治者的好感，而不是扣威望点数。</summary>
	private void ApplyWarAimPoliticalCost(RealmData realm, WarRules rules, int tierGap, bool escalation)
	{
		if (realm == null)
		{
			return;
		}
		float stabilityCost = rules.warGoalRevisionStabilityCost * (escalation ? 1f : 0.5f);
		float unrestCost = rules.warGoalRevisionUnrestCost * (escalation ? 1f : 0.5f);
		if (tierGap > 0)
		{
			stabilityCost += tierGap * rules.warGoalForceStabilityCostPerTier;
			unrestCost += tierGap * rules.warGoalForceUnrestCostPerTier;
		}
		RealmStability.Adjust(realm, -stabilityCost);
		realm.unrest = Mathf.Min(100f, realm.unrest + unrestCost);
		if (tierGap > 0)
		{
			ApplyCourtOpinionPenalty(realm, tierGap * rules.warGoalForceCourtOpinionPerTier);
		}
	}

	/// <summary>强推目标会招致廷臣不满：在职官员对统治者的好感下降。</summary>
	private void ApplyCourtOpinionPenalty(RealmData realm, float amount)
	{
		if (_characterManager == null || realm.officeHolders == null || realm.officeHolders.Count == 0 || amount <= 0f)
		{
			return;
		}
		int rulerId = realm.GetSupremeRulerId();
		CharacterData ruler = (rulerId >= 0) ? _characterManager.GetCharacter(rulerId) : null;
		if (ruler == null)
		{
			return;
		}
		foreach (int holderId in realm.officeHolders.Values)
		{
			CharacterData holder = _characterManager.GetCharacter(holderId);
			if (holder == null || holder == ruler || holder.relations == null)
			{
				continue;
			}
			if (holder.relations.TryGetValue(rulerId, out CharacterRelation relation))
			{
				relation.opinion = Mathf.Clamp(relation.opinion - amount, -200f, 200f);
				holder.relations[rulerId] = relation;
			}
		}
	}

	/// <summary>议事裁决：跳过议事直接放行；有议事时，少数总统帅可强推，多数反对且选择遵从则作罢。</summary>
	private static bool ResolveCouncilDecision(CouncilConsultationResult council, WarDecisionMode mode)
	{
		if (mode == WarDecisionMode.SkipConsultation || !council.hasCouncil)
		{
			return true;
		}
		return council.MajoritySupport || mode == WarDecisionMode.ConsultAndOverride;
	}

	/// <summary>
	/// 不协商/强推的代价：不是扣抽象点数，而是议事成员对统治者的好感、政权稳定度与不安。
	/// 协商通过不额外收费；没有议事成员时按"无人背书"处理。
	/// </summary>
	private void ApplyCouncilConsequences(RealmData realm, CouncilConsultationResult council, WarDecisionMode mode, WarRules rules)
	{
		if (realm == null)
		{
			return;
		}
		bool unilateral = mode == WarDecisionMode.SkipConsultation || !council.hasCouncil;
		bool overridden = mode == WarDecisionMode.ConsultAndOverride && council.hasCouncil && !council.MajoritySupport;
		float opinionPenalty;
		float stabilityCost;
		float unrestCost;
		if (unilateral)
		{
			opinionPenalty = rules.warCouncilSkipOpinionPenalty;
			stabilityCost = rules.warCouncilSkipStabilityCost;
			unrestCost = rules.warCouncilSkipUnrestCost;
		}
		else if (overridden)
		{
			opinionPenalty = rules.warCouncilOverrideOpinionPenalty;
			stabilityCost = rules.warCouncilOverrideStabilityCost;
			unrestCost = rules.warCouncilOverrideUnrestCost;
		}
		else
		{
			return;
		}
		RealmStability.Adjust(realm, -stabilityCost);
		realm.unrest = Mathf.Min(100f, realm.unrest + unrestCost);
		if (opinionPenalty <= 0f || council.memberIds == null || _characterManager == null)
		{
			return;
		}
		int rulerId = realm.GetSupremeRulerId();
		foreach (int memberId in council.memberIds)
		{
			CharacterData member = _characterManager.GetCharacter(memberId);
			if (member == null || member.relations == null || !member.relations.TryGetValue(rulerId, out CharacterRelation relation))
			{
				continue;
			}
			bool opponent = council.opponentIds != null && council.opponentIds.Contains(memberId);
			float amount = opinionPenalty * (opponent ? 1f : 0.5f);
			relation.opinion = Mathf.Clamp(relation.opinion - amount, -200f, 200f);
			member.relations[rulerId] = relation;
		}
	}

	/// <summary>
	/// P1.6 婚姻入口：把人物 Marry 接到世界层（关系 + 编年史 + 跨政权联姻外交）。
	/// 条件仍由 CharacterManager.Marry 判定（成年、异性、非近亲、未婚）。
	/// </summary>
	public bool TryArrangeMarriage(int aId, int bId, out string reason)
	{
		reason = "";
		CharacterData a = _characterManager?.GetCharacter(aId);
		CharacterData b = _characterManager?.GetCharacter(bId);
		if (a == null || b == null)
		{
			reason = "人物不存在";
			return false;
		}
		if (!a.isAlive || !b.isAlive)
		{
			reason = "人物已故";
			return false;
		}
		if (IsCelibateReligiousLeader(aId) || IsCelibateReligiousLeader(bId))
		{
			reason = "教职独身，不可婚配";
			return false;
		}
		bool allowSameSex = false;
		if (realms != null && realms.TryGetValue(a.realmId, out RealmData realm) && realm != null)
		{
			FaithSystem faith = GetFaithSystem(realm.stateReligionId);
			if (faith != null && MyriadCreation.Simulation.ContentHost.ContentResolvers.Religions.TryGet(faith.religionId, out MyriadCreation.Content.Definitions.ReligionDef religion))
			{
				allowSameSex = MyriadCreation.Content.Definitions.DoctrineBindingHost.Resolve(religion).homosexuality == MyriadCreation.Content.Definitions.DoctrineMoralStance.Accepted;
			}
		}
		if (!_characterManager.Marry(aId, bId, allowSameSex))
		{
			reason = "不满足婚姻条件";
			return false;
		}
		if (a.realmId != b.realmId && a.realmId >= 0 && b.realmId >= 0)
		{
			_diplomacyManager.ModifyRelation(a.realmId, b.realmId, 10f, "联姻");
		}
		_chronicle?.Add("marriage", a.fullName + " 与 " + b.fullName + " 成婚", true, a.realmId, b.realmId);
		return true;
	}

	/// <summary>带主体的宣战：先过 ActionGate（能力∧职权），再走原宣战流程。actor 为发起者角色。</summary>
	public bool DeclareWar(int attackerId, int defenderId, string reason, int actorCharacterId)
	{
		return DeclareWar(attackerId, defenderId, reason, actorCharacterId, WarDecisionMode.ConsultAndAbide);
	}

	public bool DeclareWar(int attackerId, int defenderId, string reason, int actorCharacterId, WarDecisionMode mode)
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
		return DeclareWarCore(attackerId, defenderId, reason, mode);
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
		if (!faithSystem.CanDeclareGreatHolyWar() || IsStateFaithPacifist(callerRealmId))
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
		WarState warState = CreateWarState(callerRealmId, targetRealmId, reason);
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
		// P1.6：未指定指挥官时按统帅能力选将，并把军队挂到该人物身上。
		if (commanderId < 0)
		{
			commanderId = _characterManager?.FindBestGeneral(ownerRealmId)?.characterId ?? -1;
		}
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
		if (commanderId >= 0)
		{
			CharacterData commander = _characterManager?.GetCharacter(commanderId);
			if (commander != null)
			{
				commander.commandedArmyId = army.armyId;
			}
		}
		return army;
	}

	public List<CrimeType> MatchCrimes(CrimeBehavior behavior, CrimeTargetClass? target = null, string context = null)
	{
		return CrimeRegistry.MatchByBehavior(behavior, target, context);
	}

	/// <summary>P1.3：按国教伦理绑定判断行为是否入罪；无绑定/Shunned/Accepted 都不入罪。</summary>
	public bool IsDoctrineCriminal(int realmId, MyriadCreation.Content.Definitions.DoctrineCrimeBehavior behavior)
	{
		if (realms == null || !realms.TryGetValue(realmId, out RealmData realm) || realm == null)
		{
			return false;
		}
		FaithSystem faith = GetFaithSystem(realm.stateReligionId);
		if (faith == null || !MyriadCreation.Simulation.ContentHost.ContentResolvers.Religions.TryGet(faith.religionId, out MyriadCreation.Content.Definitions.ReligionDef religion))
		{
			return false;
		}
		var bindings = MyriadCreation.Content.Definitions.DoctrineBindingHost.Resolve(religion);
		return MyriadCreation.Content.Definitions.DoctrineBindingHost.IsCrime(bindings, behavior);
	}

	public bool IsDoctrineShunned(int realmId, MyriadCreation.Content.Definitions.DoctrineCrimeBehavior behavior)
	{
		if (realms == null || !realms.TryGetValue(realmId, out RealmData realm) || realm == null)
		{
			return false;
		}
		FaithSystem faith = GetFaithSystem(realm.stateReligionId);
		if (faith == null || !MyriadCreation.Simulation.ContentHost.ContentResolvers.Religions.TryGet(faith.religionId, out MyriadCreation.Content.Definitions.ReligionDef religion))
		{
			return false;
		}
		var bindings = MyriadCreation.Content.Definitions.DoctrineBindingHost.Resolve(religion);
		return MyriadCreation.Content.Definitions.DoctrineBindingHost.GetStance(bindings, behavior) == MyriadCreation.Content.Definitions.DoctrineMoralStance.Shunned;
	}

	/// <summary>忌讳行为的社会后果：同政权、同信仰的成年人对当事人好感下降。</summary>
	public void ApplyShunnedStigma(int realmId, MyriadCreation.Content.Definitions.DoctrineCrimeBehavior behavior, int actorCharacterId)
	{
		if (!IsDoctrineShunned(realmId, behavior) || _characterManager == null)
		{
			return;
		}
		CharacterData actor = _characterManager.GetCharacter(actorCharacterId);
		if (actor == null || !actor.isAlive)
		{
			return;
		}
		int applied = 0;
		foreach (CharacterData other in _characterManager.GetCharactersByRealm(realmId))
		{
			if (other == null || !other.isAlive || other.characterId == actorCharacterId || other.faithId != actor.faithId)
			{
				continue;
			}
			other.ModifyRelation(actorCharacterId, -5f, "忌讳行为");
			applied++;
			if (applied >= 12)
			{
				break;
			}
		}
		_chronicle?.Add("doctrine_shunned", actor.fullName + " 的行为受到同信仰者排斥", false, realmId);
	}

	/// <summary>P1.3：按教义伦理发起审判；非罪行直接驳回，不入司法流程。</summary>
	public TrialResult TryTrialDoctrineCrime(int realmId, MyriadCreation.Content.Definitions.DoctrineCrimeBehavior behavior, int suspectCharacterId)
	{
		if (!IsDoctrineCriminal(realmId, behavior))
		{
			if (IsDoctrineShunned(realmId, behavior))
			{
				ApplyShunnedStigma(realmId, behavior, suspectCharacterId);
			}
			return default;
		}
		CrimeType crime = (behavior == MyriadCreation.Content.Definitions.DoctrineCrimeBehavior.Witchcraft) ? CrimeType.Witchcraft : CrimeType.SexualMisconduct;
		return TryTrialCrime(realmId, crime, suspectCharacterId);
	}

	public TrialResult TryTrialCrime(int realmId, CrimeType crime, int suspectCharacterId)
	{
		if (HasLegalExemption(realmId, suspectCharacterId))
		{
			return default;
		}
		JudicialSystem orCreateJudicialSystem = GetOrCreateJudicialSystem(realmId);
		if (orCreateJudicialSystem == null)
		{
			return default;
		}
		realms.TryGetValue(realmId, out var value);
		bool hasJudge = OfficeSystem.HasModuleHolder(value, OfficeSystem.ModuleJudiciary, _characterManager, out _);
		TrialResult result = orCreateJudicialSystem.TrialCrime(crime, suspectCharacterId, realmId, value, hasJudge);
		if (_chronicle != null && result.verdict != VerdictType.Dismissed)
		{
			string eventType = ((result.verdict == VerdictType.Guilty) ? "crime_guilty" : "crime_acquitted");
			_chronicle.Add(eventType, string.Format("{0}司法审结：{1}（{2}）", value?.realmName ?? "某政权", crime, result.verdict), false, realmId);
		}
		return result;
	}

	/// <summary>
	/// 人格效力（M5）：现行法中若有对该身份不适用/豁免的条款，司法程序不启动。
	/// 只查现行 activeLawId，不扫描全部法律目录。
	/// </summary>
	public bool HasLegalExemption(int realmId, int characterId)
	{
		if (realms == null || !realms.TryGetValue(realmId, out RealmData realm) || realm?.lawState?.groups == null)
		{
			return false;
		}
		CharacterData character = _characterManager?.GetCharacter(characterId);
		if (character == null)
		{
			return false;
		}
		SocialIdentityFlags identity = CharacterIdentity.Of(character, realmId);
		for (int i = 0; i < realm.lawState.groups.Count; i++)
		{
			LawGroupState group = realm.lawState.groups[i];
			if (group == null || string.IsNullOrEmpty(group.activeLawId))
			{
				continue;
			}
			LawDef law = DefaultLawCatalog.FindLaw(group.activeLawId);
			if (law != null && !LawApplicability.AppliesTo(law, identity))
			{
				return true;
			}
		}
		return false;
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
