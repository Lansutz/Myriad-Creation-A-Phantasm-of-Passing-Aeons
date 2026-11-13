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
using MyriadCreation.Simulation.Planning;
using MyriadCreation.Simulation.Politics;
using MyriadCreation.Simulation.Settlement;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
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
using MyriadCreation.Content.Definitions;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace MyriadCreation.Simulation.WorldState
{
public partial class GameWorld : MonoBehaviour
{

	/// <summary>
	/// 重算/重生成前确保海陆生成器的重算 seed = 当前世界 seed（重算路径的破碎化 RNG 必须与世界 seed 绑定）。
	/// 仅在 seed 变化时重建，避免无谓丢弃生成器的运行时状态（如画笔/重算注入的破碎噪声）。
	/// </summary>
	private void EnsureSeaLandGeneratorSeed()
	{
		if (_seaLandGenerator == null || _seaLandRecalcSeed == randomSeed)
		{
			return;
		}
		_seaLandGenerator = new MyriadCreation.World.Terrain.SeaLandGenerator(config, tiles, mapWidth, mapHeight, randomSeed);
		_seaLandRecalcSeed = randomSeed;
		_worldMapSimulation?.ReplaceSeaLandGenerator(_seaLandGenerator);
	}

	internal void InitializeDefaultRealms()
	{
		if (!SetupOptions.generatePolities || SetupOptions.initialPolityCount <= 0)
		{
			return;
		}
		List<int> list = new List<int>();
		for (int i = 0; i < tiles.Length; i++)
		{
			if (tiles[i].isLand)
			{
				list.Add(i);
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		int num = Mathf.Clamp(SetupOptions.initialPolityCount, 1, Mathf.Max(1, Mathf.Min(12, list.Count)));
		string[] array = new string[3] { "中原王朝", "游牧汗国", "海洋城邦" };
		int num2 = Mathf.Max(1, list.Count / num);
		for (int j = 0; j < num; j++)
		{
			RealmData realmData = new RealmData
			{
				realmId = j,
				realmName = ((j < array.Length) ? array[j] : $"第{j + 1}政权"),
				treasury = 1000f,
				prestige = 50f,
				stability = 70f,
				centralization = 0.5f
			};
			int num3 = j * num2;
			int num4 = ((j == num - 1) ? list.Count : ((j + 1) * num2));
			for (int k = num3; k < num4; k++)
			{
				int num5 = list[k];
				tiles[num5].ownerRealmId = j;
				realmData.coreTiles.Add(num5);
				if (tiles[num5].populationBlocks.Count == 0)
				{
					float num6 = 10f * SetupOptions.initialPopulationScale;
					if (num6 > 0f)
					{
						tiles[num5].populationBlocks.Add(new PopulationBlock
						{
							raceId = 0,
							cultureId = j % 3,
							count = num6,
							satisfaction = 60f,
							socialClass = GameEnums.SocialClass.Peasant
						});
					}
				}
			}
			if (_faithSystems.Count > 0)
			{
				realmData.stateReligionId = _faithSystems[j % _faithSystems.Count].faithId;
			}
			realms[j] = realmData;
			_aiManager.CreateController(j);
		}
		_characterManager.CreateInitialRulers(realms, currentYear);
		// 政权在构造后才生成：外交关系表必须在此时补齐，否则新局宣战/同盟查不到关系。
		_diplomacyManager.EnsureRelations();
		foreach (RealmData realm in realms.Values)
		{
			if (realm == null || realm.monarchId < 0 || realm.stateReligionId < 0) continue;
			FaithSystem faith = _faithSystems.Find(f => f != null && f.faithId == realm.stateReligionId);
			if (faith != null && faith.highPriestCharacterId < 0)
			{
				faith.highPriestCharacterId = realm.monarchId;
			}
		}
		foreach (RealmData realm in realms.Values)
		{
			if (realm == null || realm.monarchId < 0 || realm.coreTiles.Count == 0) continue;
			int musterTile = -1;
			foreach (int tile in realm.coreTiles)
			{
				musterTile = tile;
				break;
			}
			if (musterTile < 0) continue;
			Army army = CreateArmy(realm.realmId, realm.monarchId, musterTile);
			army.unitCounts[100] = 1f;
		}
		if (PlayerRealmId < 0 && num > 0)
		{
			PlayerRealmId = 0;
		}
	}

	public void GenerateTerrain(int seed = 42)
	{
		SetupOptions.Normalize();
		GenerateTerrainBase(seed);
		if (SetupOptions.generatePolities)
		{
			InitializeDefaultRealms();
		}
		GenerateProvinces(seed);
		if (SetupOptions.generateSettlements)
		{
			GenerateSettlements(seed);
		}
		ResourceGenerator.GenerateAll(this, seed);
		GenerateInitialUnits(seed);
		ClearInitialTerrainDirty();
		SettlementPopulationSystem.RecomputeAll(this);
		BuildPreloadPlan();
		// 宫廷人物必须在所有依赖全局 Random 的生成步骤之后创建，避免改变地图/聚落/初始单位。
		_characterManager.CreateInitialCourtiers(realms, currentYear);
		EnsureOfficeHolders();
		Knowledge.EnsureCapacity(tiles?.Length ?? 0);
		Knowledge.SeedOwnership(tiles, mapWidth, mapHeight);
		for (int i = 0; i < realms.Count; i++)
		{
			if (realms[i] != null) BannerSystem.Ensure(realms[i], _characterManager, _innovationTree);
		}
		Debug.Log((object)$"[GameWorld] 地形生成完成（{SetupOptions.initialContent}），陆地{GetLandTileCount()}地块，海洋{GetSeaTileCount()}地块，省份{provinces.Count}个");
	}

	private void GenerateTerrainBase(int seed)
	{
		randomSeed = seed;
		EnsureSeaLandGeneratorSeed();
		new PlanetTerrainGenerator(seed).Generate(tiles, mapWidth, mapHeight);
		if (GenConfig != null && GenConfig.TerrainScale == TerrainScale.Regional)
		{
			RegionalTerrainShaper.Apply(tiles, mapWidth, mapHeight, GenConfig.RegionalEdge, GenConfig.RegionalEdgeFalloff, GenConfig.SeaLevel);
		}
		_worldMapSimulation?.MarkAllTerrainDirty(tiles.Length);
		SimulationScheduler.MarkDirty("world.terrain");
		new TerrainGenerator(seed).TrackRivers(tiles);
		// 初始开图也应用破碎化（task-42）：必须在省份/聚落/初始单位生成之前完成，下游按最终海陆生成。
		ApplyInitialSeaLandFragmentation(0f);
		ClearInitialTerrainDirty();
	}

	/// <summary>
	/// 初始开图时的破碎化应用（task-42）：主生成只产出基础高程/海陆，这里在生成段末尾按世界 seed 走一次
	/// SeaLandGenerator 全量重算，让 landFragment/coastFragment 在开图即生效，并重算海岸标记/海域连通。
	/// 必须在下游省份/聚落/初始单位生成之前调用（它们依赖最终 isLand）。
	/// 读档路径（SaveSystem.ApplyToWorld → ReinitializeSubsystems / PrepareLoadedWorld）不会调用本方法，
	/// 也不调用 GenerateTerrain*，因此存档地形读档后不会被二次破碎化/重算。
	/// 成本：一次 O(N log N) 海陆阈值排序 + O(N) 地块重算/邻接/连通（与运行时全量 RecalculateAll 相同），
	/// 外加破碎化前后各一次 O(N) 轻量快照与受影响地块的气候/群系/肥力补算。
	/// 行为：elevation01 由主生成的绝对高程变为"相对海平面阈值"表示；破碎化前快照 elevation01/isLand/isCoast，
	/// 重算后只对"实际变化"（海陆/海岸翻转 或 高程变化偏离整体折算量 &gt; 0.01）的地块补算
	/// climate/biome/fertility（task-51）。
	/// </summary>
	private void ApplyInitialSeaLandFragmentation(float globalTempOffset)
	{
		if (_seaLandGenerator == null)
		{
			return;
		}
		EnsureSeaLandGeneratorSeed();
		int tileCount = tiles.Length;
		float[] elevationBefore = new float[tileCount];
		bool[] isLandBefore = new bool[tileCount];
		bool[] isCoastBefore = new bool[tileCount];
		for (int i = 0; i < tileCount; i++)
		{
			elevationBefore[i] = tiles[i].elevation01;
			isLandBefore[i] = tiles[i].isLand;
			isCoastBefore[i] = tiles[i].isCoast;
		}
		_seaLandGenerator.RecalculateAll();
		RecalculateDerivedClimateAfterFragmentation(elevationBefore, isLandBefore, isCoastBefore, globalTempOffset);
	}

	/// <summary>
	/// 破碎化改写海陆/高程后，按主生成公式补算受影响地块的派生属性（task-51 / verifier P1-2）。
	///
	/// changed 判定：海陆/海岸翻转，或高程变化**偏离整体折算量**超过 0.01。SeaLandGenerator 全量重算会把所有地块的
	/// elevation01 统一减去海平面阈值（相对化），若直接用 |Δelevation01| &gt; 0.01 会把全表都当成"变化"，进而用
	/// GCM 之前的基础公式重写整张图的气候/群系（既丢失 GCM/地形细节，又整体抬高温度）。这里先从快照差值中
	/// 估计这个统一折算量（取样本中位数），只保留真正被破碎化/重定阈改写的少量地块，保证"只对 changed 重算"。
	/// 不直接使用 SeaLandGenerator.LastChangedTileIndices：初始开图时它≈全表，语义是"被写过"而非"实际变化"。
	///
	/// 重算范围：annualTemp / annualPrecipMm / airHumidityPct / climateZone / biome / fertility
	///（task-55：湿度与气候带也随新 climate 重算，海地块 fertility=0；不跑 GCM，
	/// 也不改 CalculateClimate 全量路径）。温度使用破碎化前的绝对高程，避免相对化后整体偏暖。
	/// </summary>
	private void RecalculateDerivedClimateAfterFragmentation(float[] elevationBefore, bool[] isLandBefore, bool[] isCoastBefore, float globalTempOffset)
	{
		if (elevationBefore == null || isLandBefore == null || isCoastBefore == null)
		{
			return;
		}
		float uniformElevationShift = EstimateFragmentationUniformElevationShift(elevationBefore);
		int changedCount = 0;
		var recomputedFlags = new bool[tiles.Length];
		Parallel.For(0, tiles.Length, delegate(int i)
		{
			if (!tiles[i].exists)
			{
				return;
			}
			bool changed = isLandBefore[i] != tiles[i].isLand
				|| isCoastBefore[i] != tiles[i].isCoast
				|| Mathf.Abs((elevationBefore[i] - tiles[i].elevation01) - uniformElevationShift) > 0.01f;
			if (!changed)
			{
				return;
			}
			RecalculateDerivedClimateForTile(i, elevationBefore[i], globalTempOffset);
			recomputedFlags[i] = true;
			Interlocked.Increment(ref changedCount);
		});
		_lastFragmentationRecomputedTiles.Clear();
		for (int i = 0; i < recomputedFlags.Length; i++)
		{
			if (recomputedFlags[i]) _lastFragmentationRecomputedTiles.Add(i);
		}
		if (changedCount > 0)
		{
			Debug.Log((object)$"[GameWorld] 破碎化后派生属性重算：{changedCount}/{tiles.Length} 地块（climate/湿度/气候带/biome/fertility，统一高程折算={uniformElevationShift:F4}）");
		}
	}

	/// <summary>
	/// 估计 SeaLandGenerator 全量重算对所有地块的统一高程折算量（样本中位数）：
	/// 未破碎化地块的 elevation01 都被减去同一个海平面阈值，只有破碎化改写过高程的地块偏离该值。
	/// </summary>
	private float EstimateFragmentationUniformElevationShift(float[] elevationBefore)
	{
		int stride = Mathf.Max(1, tiles.Length / 4096);
		List<float> samples = new List<float>(tiles.Length / stride + 1);
		for (int i = 0; i < tiles.Length; i += stride)
		{
			if (tiles[i].exists)
			{
				samples.Add(elevationBefore[i] - tiles[i].elevation01);
			}
		}
		if (samples.Count == 0)
		{
			return 0f;
		}
		samples.Sort();
		return samples[samples.Count / 2];
	}

	/// <summary>
	/// 按主生成公式重算单地块 climate/biome/fertility（海地块 fertility=0），供破碎化后一致性修复使用。
	/// task-55 起 helper 同时重算 annualPrecipMm / airHumidityPct / climateZone，这里只补 biome/fertility。
	/// 温度用破碎化前的绝对高程（与未受影响地块同口径）；biome/fertility 按当前字段（含相对化后的 elevation01）计算。
	/// </summary>
	private void RecalculateDerivedClimateForTile(int index, float elevationForTemperature, float globalTempOffset)
	{
		float latAbs = PlanetTerrainGenerator.RecalculateBaseClimateForTile(tiles, index, mapWidth, mapHeight, elevationForTemperature, globalTempOffset);
		TileData tile = tiles[index];
		tile.biome = HoldridgeBiomeClassifier.Classify(tile.annualTemp, tile.annualPrecipMm, tile.elevation01, tile.isLand, tile.isCoast, tile.isRiver, tile.slopeDegree, latAbs);
		tile.fertility = tile.isLand
			? HoldridgeBiomeClassifier.CalculateFertility(tile.annualTemp, tile.annualPrecipMm, tile.elevation01, tile.slopeDegree, tile.biome)
			: 0f;
		tiles[index] = tile;
	}

	/// <summary>
	/// 生成段收尾：初始地形已在生成阶段全量重算/破碎化完成。这里清掉生成期留下的"全表 terrain dirty"
	/// 与调度器 "world.terrain" dirty key——否则首个 Daily tick 的 WorldMapSchedule → RecalculateDirty
	/// 会把全表当成增量脏块，用生成时已设置的 _cachedSeaThreshold 再跑一次：elevation01 被二次减阈值、
	/// isLand/海岸/海域连通被二次改写（task-42 后 cachedThreshold 非 0，必现）。
	/// 只清初始生成这一批；之后地图编辑器的 PaintTerrain/UpdateConfig、增删地块等显式标脏仍正常重算。
	/// </summary>
	private void ClearInitialTerrainDirty()
	{
		_worldMapSimulation?.ClearTerrainDirty();
		_simulationScheduler.Dirty.Clear(WorldMapSchedule.DirtyKey);
	}

	public void GenerateTerrainWithConfig()
	{
		if (GenConfig == null)
		{
			GenConfig = new MapGenerationConfig();
		}
		int seed = (randomSeed = GenConfig.GetActualSeed());
		var (num, num2) = GenConfig.GetMapDimensions();
		if (num != mapWidth || num2 != mapHeight)
		{
			mapWidth = num;
			mapHeight = num2;
			tiles = new TileData[mapWidth * mapHeight];
			for (int i = 0; i < tiles.Length; i++)
			{
				tiles[i] = new TileData
				{
					tileIndex = i,
					provinceId = -1,
					ownerRealmId = -1,
					occupyingRealmId = -1,
					exists = false,
					elevation01 = 0f,
					fertility = 0.5f,
					development = 0.1f,
					stability = 50f,
					order = 50f,
					populationBlocks = new List<PopulationBlock>(),
					buildingLevels = new int[6]
				};
			}
			InitializeSubsystems();
		}
		EnsureSeaLandGeneratorSeed();
		_planetTerrainGenerator = new PlanetTerrainGenerator(seed);
		TerrainTemplateSystem.ApplyPreset((GenConfig.TerrainScale == TerrainScale.World) ? TerrainTemplateSystem.GetWorldPreset(GenConfig.WorldTemplate) : TerrainTemplateSystem.GetRegionalPreset(GenConfig.RegionalTemplate), _planetTerrainGenerator);
		GenConfig.ApplyToGenerator(_planetTerrainGenerator);
		_planetTerrainGenerator.Generate(tiles, mapWidth, mapHeight);
		if (GenConfig.TerrainScale == TerrainScale.Regional)
		{
			RegionalTerrainShaper.Apply(tiles, mapWidth, mapHeight, GenConfig.RegionalEdge, GenConfig.RegionalEdgeFalloff, GenConfig.SeaLevel);
		}
		new TerrainGenerator(seed).TrackRivers(tiles);
		// 初始开图也应用破碎化（task-42）：在此之后才生成政权/省份/聚落/初始单位，下游按最终海陆生成。
		// 传生成器使用中的 GlobalTempOffset，保证破碎化后补算的温度与主生成同口径（task-51）。
		ApplyInitialSeaLandFragmentation(_planetTerrainGenerator.GlobalTempOffset);
		_worldMapSimulation?.MarkAllTerrainDirty(tiles.Length);
		SimulationScheduler.MarkDirty("world.terrain");
		SetupOptions.Normalize();
		if (SetupOptions.generatePolities)
		{
			InitializeDefaultRealms();
		}
		GenerateProvinces(seed);
		if (SetupOptions.generateSettlements)
		{
			GenerateSettlements(seed);
		}
		ResourceGenerator.GenerateAll(this, seed);
		GenerateInitialUnits(seed);
		ClearInitialTerrainDirty();
		SettlementPopulationSystem.RecomputeAll(this);
		BuildPreloadPlan();
		_characterManager.CreateInitialCourtiers(realms, currentYear);
		EnsureOfficeHolders();
		Debug.Log((object)$"[GameWorld] 配置化地形生成完成（{SetupOptions.initialContent}）：陆地{GetLandTileCount()}，海洋{GetSeaTileCount()}，省份{provinces.Count}");
	}

	public void GenerateAll()
	{
		GenerateTerrainWithConfig();
		CalculateClimate();
		RecalculateHydrology();
		Knowledge.EnsureCapacity(tiles?.Length ?? 0);
		Knowledge.SeedOwnership(tiles, mapWidth, mapHeight);
		Debug.Log((object)"[GameWorld] 全部生成完成");
	}

	private int ResolveCellsPerProvince()
	{
		int num = 0;
		if (tiles != null)
		{
			for (int i = 0; i < tiles.Length; i++)
			{
				if (tiles[i].isLand)
				{
					num++;
				}
			}
		}
		int targetProvinceCount = ((GenConfig != null) ? GenConfig.ProvinceCount : 6000);
		return ProvinceGenerator.CellsPerProvinceFor(num, targetProvinceCount);
	}

	private void GenerateProvinces(int seed)
	{
		int num = ResolveCellsPerProvince();
		ProvinceGenerator provinceGenerator = new ProvinceGenerator(tiles, mapWidth, mapHeight, config.wrapX);
		int lloydIterations = ((GenConfig != null) ? GenConfig.ProvinceLloydIterations : 2);
		provinces = provinceGenerator.Generate(seed + 777, num, lloydIterations);
		Debug.Log((object)$"[GameWorld] 省份生成：{num}地块/省 → {provinces.Count}省");
	}

	private void AdoptSettlements(Dictionary<int, SettlementData> generated)
	{
		// 子系统在 InitializeWorld 时已持有 settlements 字典引用；这里必须就地替换，
		// 不能重新赋值，否则聚落模拟/生产/生命周期会继续操作一张空表。
		settlements.Clear();
		if (generated == null)
		{
			return;
		}
		foreach (KeyValuePair<int, SettlementData> pair in generated)
		{
			settlements[pair.Key] = pair.Value;
		}
	}

	private void GenerateSettlements(int seed)
	{
		if (!SetupOptions.generateSettlements || SetupOptions.initialSettlementCount == 0)
		{
			anchors = new Dictionary<int, AnchorData>();
			settlements.Clear();
			return;
		}
		(Dictionary<int, AnchorData> anchors, Dictionary<int, SettlementData> settlements) tuple = new AnchorGenerator(tiles, mapWidth, mapHeight, provinces, seed).Generate();
		Dictionary<int, AnchorData> item = tuple.anchors;
		Dictionary<int, SettlementData> item2 = tuple.settlements;
		anchors = item;
		AdoptSettlements(item2);
		foreach (SettlementData value in settlements.Values)
		{
			value.anchor = anchors[value.anchorId];
		}
		int initialSettlementCount = SetupOptions.initialSettlementCount;
		if (initialSettlementCount > 0 && settlements.Count > initialSettlementCount)
		{
			List<int> list = new List<int>();
			foreach (int key in settlements.Keys)
			{
				if (list.Count >= initialSettlementCount)
				{
					break;
				}
				list.Add(key);
			}
			Dictionary<int, AnchorData> dictionary = new Dictionary<int, AnchorData>();
			Dictionary<int, SettlementData> dictionary2 = new Dictionary<int, SettlementData>();
			foreach (int item3 in list)
			{
				dictionary[item3] = anchors[item3];
				dictionary2[item3] = settlements[item3];
			}
			anchors = dictionary;
			AdoptSettlements(dictionary2);
		}
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		foreach (SettlementData value2 in settlements.Values)
		{
			if (value2.settlementCategory == SettlementCategory.Outpost)
			{
				num3++;
			}
			else if (value2.isPort)
			{
				num++;
				num2++;
			}
			else if (value2.settlementForm == SettlementForm.City)
			{
				num++;
			}
			else
			{
				num4++;
			}
		}
		Debug.Log((object)$"[GameWorld] 子地块生成：{settlements.Count}个（城{num}/港{num2}/寨{num3}/村{num4}）");
	}

	public void GenerateInitialUnits(int seed)
	{
		if (!SetupOptions.generateInitialUnits || SetupOptions.initialUnitCount <= 0)
		{
			return;
		}
		int num = Mathf.Clamp(SetupOptions.initialUnitCount, 0, 1000);
		Random random = new Random(seed + 24680);
		for (int i = 0; i < num; i++)
		{
			int num2 = -1;
			for (int j = 0; j < 40; j++)
			{
				int num3 = random.Next(tiles.Length);
				if (tiles[num3].exists && tiles[num3].isLand)
				{
					num2 = num3;
					break;
				}
			}
			if (num2 >= 0)
			{
				MapActorType mapActorType = (i % 4) switch
				{
					0 => MapActorType.Refugee, 
					1 => MapActorType.Nomad, 
					2 => MapActorType.Wildlife, 
					_ => MapActorType.Bandit, 
				};
				MapActor mapActor = _mapActorManager.SpawnActor(mapActorType, num2, random.Next(20, 300));
				MapActor mapActor2 = mapActor;
				mapActor2.actorName = mapActorType switch
				{
					MapActorType.Refugee => $"流民#{mapActor.actorId}", 
					MapActorType.Nomad => $"游牧#{mapActor.actorId}", 
					MapActorType.Wildlife => $"野生群体#{mapActor.actorId}", 
					_ => $"土匪#{mapActor.actorId}", 
				};
			}
		}
	}

	private float CalculateBaseFertility(int index)
	{
		ref TileData reference = ref tiles[index];
		float num = Mathf.Clamp(reference.annualPrecipMm / 1000f, 0f, 1f) * 0.5f + Mathf.Clamp((reference.annualTemp + 10f) / 35f, 0f, 1f) * 0.3f;
		float num2 = (1f - reference.elevation01) * 0.2f;
		float num3 = reference.soilHumidityPct / 100f * 0.2f;
		return Mathf.Clamp(num + num2 + num3, 0.05f, 1f);
	}

	public void GenerateProvinces()
	{
		int actualSeed = GenConfig.GetActualSeed();
		int num = ResolveCellsPerProvince();
		ProvinceGenerator provinceGenerator = new ProvinceGenerator(tiles, mapWidth, mapHeight, config.wrapX);
		int lloydIterations = ((GenConfig != null) ? GenConfig.ProvinceLloydIterations : 2);
		provinces = provinceGenerator.Generate(actualSeed, num, lloydIterations);
		Debug.Log((object)$"[GameWorld] 省份划分完成：{provinces.Count} 个省份（每省约 {num} 地块）");
	}

	public void GenerateSettlements()
	{
		if (provinces.Count == 0)
		{
			Debug.LogWarning((object)"[GameWorld] 聚落生成跳过：省份尚未生成");
			return;
		}
		int actualSeed = GenConfig.GetActualSeed();
		(Dictionary<int, AnchorData> anchors, Dictionary<int, SettlementData> settlements) tuple = new AnchorGenerator(tiles, mapWidth, mapHeight, provinces, actualSeed).Generate();
		Dictionary<int, AnchorData> item = tuple.anchors;
		Dictionary<int, SettlementData> item2 = tuple.settlements;
		anchors = item;
		AdoptSettlements(item2);
		foreach (SettlementData value in settlements.Values)
		{
			value.anchor = anchors[value.anchorId];
		}
		Debug.Log((object)$"[GameWorld] 聚落生成完成：{settlements.Count} 个聚落");
	}

	public IEnumerator GenerateTerrainAsync(int seed, Action onComplete, Action<float, string> onProgress = null)
	{
		SetupOptions.Normalize();
		onProgress?.Invoke(0.05f, "生成地形…");
		GenerateTerrainBase(seed);
		if (SetupOptions.generatePolities)
		{
			InitializeDefaultRealms();
		}
		onProgress?.Invoke(0.2f, "生成省份…");
		int cellsPerProvince = ResolveCellsPerProvince();
		int lloydIterations = ((GenConfig != null) ? GenConfig.ProvinceLloydIterations : 2);
		float provinceProgress = 0f;
		Task<Dictionary<int, Province>> task = ProvinceGenerator.GenerateAsync(tiles, mapWidth, mapHeight, (Object)(object)config != (Object)null && config.wrapX, seed, cellsPerProvince, lloydIterations, (float p) =>
		{
			provinceProgress = p;
		});
		while (!task.IsCompleted)
		{
			onProgress?.Invoke(0.2f + Mathf.Clamp01(provinceProgress) * 0.65f, "生成省份…");
			yield return null;
		}
		if (task.IsFaulted)
		{
			Debug.LogError((object)("[GameWorld] 异步省份生成失败: " + task.Exception?.GetBaseException().Message));
			yield break;
		}
		provinces = task.Result;
		if (SetupOptions.generateSettlements)
		{
			onProgress?.Invoke(0.88f, "生成聚落…");
			GenerateSettlements(seed);
		}
		ResourceGenerator.GenerateAll(this, seed);
		GenerateInitialUnits(seed);
		ClearInitialTerrainDirty();
		onProgress?.Invoke(1f, "完成");
		Debug.Log((object)$"[GameWorld] 异步生成完成：陆地{GetLandTileCount()}，省份{provinces.Count}");
		onComplete?.Invoke();
	}

	public void RegenerateFromPipeline(GenerationStage fromStage)
	{
		if (Pipeline == null)
		{
			GenerationPipeline generationPipeline = (Pipeline = new GenerationPipeline(this));
		}
		Pipeline.RegenerateFrom(fromStage);
	}
}
}
