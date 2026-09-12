using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Map;
using CivilizationEvolution.Climate;
using CivilizationEvolution.Race;
using CivilizationEvolution.Culture;
using CivilizationEvolution.Economy;
using CivilizationEvolution.Politics;
using CivilizationEvolution.War;
using CivilizationEvolution.Diplomacy;
using CivilizationEvolution.Character;
using CivilizationEvolution.Thought;
using CivilizationEvolution.Disaster;
using CivilizationEvolution.World;
using CivilizationEvolution.Building;
using CivilizationEvolution.Tech;
using CivilizationEvolution.AI;

namespace CivilizationEvolution.Core
{
 /// GameWorld.Initialization —— 世界初始化与数据定义（物资/兵种/种族/文化/政权/信仰）（partial class，与 GameWorld.cs 共享字段与子系统）
    public partial class GameWorld
    {

 /// <summary>初始化世界（StartNewGame 设好尺寸后显式调用）</summary>
        public void InitializeWorld()
        {
 // 初始化生成管线（在所有子系统之前，幂等）
            Pipeline ??= new GenerationPipeline(this);
 // 幂等（重复 StartNewGame 安全——重新建 tiles）
            if (startYear < 0) { startYear = currentYear; startDay = currentDay; }
 // 内容注册表幂等初始化（与 Bootstrap.Awake 无执行顺序依赖）
            if (!ContentRegistry.IsInitialized)
                ContentRegistry.Initialize();

 // 配置资产：拖入了SO则Instantiate运行时副本（避免污染资产），否则创建默认实例
            if (config == null)
            {
                config = WorldConfig.CreateRuntimeInstance();
            }
            else
            {
                config = Instantiate(config);
                config.name = "RuntimeWorldConfig";
                config.hideFlags = HideFlags.DontSave;
            }

            tiles = new TileData[mapWidth * mapHeight];
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i] = new TileData
                {
                    tileIndex = i,
                    regionId = i / 16,
                    provinceId = -1, // 省份归属（沃罗诺伊省区生成后赋值）
                    ownerRealmId = -1,
                    occupyingRealmId = -1,
                    exists = false, // 默认不存在，地形生成时创建陆地地块
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
            InitializeGoodsDefs();
            InitializeUnitDefs();
            InitializeTradeCenters();
            InitializeDefaultRaces();
            InitializeDefaultCultures();
            ApplyContentOverrides(); // 内容注册表按 id 覆盖内置默认（数据驱动优先）
 // InitializeDefaultRealms 移到 GenerateTerrain 之后（需要地形数据）
            Debug.Log($"[GameWorld] 世界初始化完成：{mapWidth}x{mapHeight} = {tiles.Length} 地块容量");
        }


 /// <summary>初始化所有子系统</summary>
        private void InitializeSubsystems()
        {
            _seaLandGenerator = new SeaLandGenerator(config, tiles, mapWidth, mapHeight);
            _featureManager = new FeatureManager(tiles, mapWidth, mapHeight);
            _mapActorManager = new MapActorManager(this);
            _campManager = new CampManager(this);
            _climateSimulator = new PlanetClimateSimulator(config, tiles, mapWidth, mapHeight, _seaLandGenerator);
            _currencySystem = new CurrencySystem();
            _taxSystem = new TaxSystem();
            _economyManager = new EconomyManager(tiles, tradeCenters, goodsDefs, _currencySystem, _taxSystem);
            _politicalManager = new PoliticalManager(tiles, realms);
            _combatManager = new CombatManager(tiles, unitDefs, _seaLandGenerator);
            _diplomacyManager = new DiplomacyManager(realms);
            _characterManager = new CharacterManager();
            _characterManager.Races = races; // 注入种族表（DNA 表达/混血基准依赖）
            _characterManager.Economy = _economyManager; // 注入经济（角色饮食联动）
            _characterManager.Tiles = tiles;   // 注入地块表（饮食按政权地块定位贸易中心）
            _characterManager.Realms = realms; // 注入政权表（饮食/领地定位）
            _thoughtManager = new ThoughtManager();
            _disasterSystem = new DisasterSystem(tiles, mapWidth, mapHeight);
            _diseaseSystem = new DiseaseSystem(tiles, _characterManager, mapWidth, mapHeight);
            _buildingSystem = new BuildingSystem(tiles);
            _innovationTree = new InnovationTree();
            _characterManager.Innovations = _innovationTree; // 注入革新树（家族传统解锁前置依赖）
 // 阶层出现事件订阅（革新完成→检测解锁阶层→编年史——查漏补缺接线）
            _innovationTree.OnInnovationCompleted += OnInnovationCompletedHandler;
            _chronicle = new Chronicle(); // 编年史（世界大事日志）
 // 政体变迁动力学需要革新树（可行性约束）与编年史（记录节点）
            _regimeDynamics = new RegimeChangeDynamics(_innovationTree, _chronicle);
            _diplomacyManager.Chronicle = _chronicle;
            _aiManager = new AIManager();
        }


 /// <summary>读档后重建子系统（引用类型无法序列化）</summary>
        public void ReinitializeSubsystems()
        {
            InitializeSubsystems();
            Debug.Log("[GameWorld] 子系统重建完成（读档后）");
        }


 // ===== 初始化辅助 =====
        private void InitializeGoodsDefs()
        {
            AddGoodsDef(0, "粮食", GameEnums.GoodsCategory.Food, 1.0f, 0.1f, true, 180f);
            AddGoodsDef(1, "肉类", GameEnums.GoodsCategory.Food, 2.0f, 0.15f, true, 14f);
            AddGoodsDef(2, "鱼类", GameEnums.GoodsCategory.Food, 1.5f, 0.1f, true, 7f);
            AddGoodsDef(3, "盐", GameEnums.GoodsCategory.Food, 3.0f, 0.05f, false, 0f);
            AddGoodsDef(10, "谷物", GameEnums.GoodsCategory.Crop, 0.8f, 0.1f, true, 365f);
            AddGoodsDef(11, "棉花", GameEnums.GoodsCategory.Crop, 2.5f, 0.08f, false, 0f);
            AddGoodsDef(20, "马", GameEnums.GoodsCategory.Livestock, 20f, 1f, false, 0f);
            AddGoodsDef(21, "牛", GameEnums.GoodsCategory.Livestock, 15f, 1f, false, 0f);
            AddGoodsDef(22, "羊", GameEnums.GoodsCategory.Livestock, 5f, 0.3f, false, 0f);
            AddGoodsDef(30, "原木", GameEnums.GoodsCategory.Wood, 0.5f, 0.5f, false, 0f);
            AddGoodsDef(31, "加工木材", GameEnums.GoodsCategory.Wood, 1.2f, 0.4f, false, 0f, 30, 0.8f);
            AddGoodsDef(40, "石料", GameEnums.GoodsCategory.Stone, 0.3f, 0.8f, false, 0f);
            AddGoodsDef(50, "铁矿", GameEnums.GoodsCategory.MetalOre, 1.5f, 0.3f, false, 0f);
            AddGoodsDef(51, "铜矿", GameEnums.GoodsCategory.MetalOre, 2.0f, 0.3f, false, 0f);
            AddGoodsDef(54, "锡矿", GameEnums.GoodsCategory.MetalOre, 3.0f, 0.25f, false, 0f);
            AddGoodsDef(58, "铅矿", GameEnums.GoodsCategory.MetalOre, 1.2f, 0.3f, false, 0f);
            // 金属（加工品，由矿物冶炼而来）
            AddGoodsDef(52, "铁", GameEnums.GoodsCategory.Metal, 4.0f, 0.15f, false, 0f, 50, 0.6f);
            AddGoodsDef(53, "铜", GameEnums.GoodsCategory.Metal, 5.0f, 0.15f, false, 0f, 51, 0.6f);
            AddGoodsDef(55, "锡", GameEnums.GoodsCategory.Metal, 8.0f, 0.1f, false, 0f, 54, 0.5f);
            AddGoodsDef(59, "铅", GameEnums.GoodsCategory.Metal, 2.5f, 0.2f, false, 0f, 58, 0.7f);
            AddGoodsDef(56, "青铜", GameEnums.GoodsCategory.Metal, 10.0f, 0.15f, false, 0f, 53, 0.8f);
            AddGoodsDef(57, "钢", GameEnums.GoodsCategory.Metal, 15.0f, 0.12f, false, 0f, 52, 0.5f);
            AddGoodsDef(60, "金", GameEnums.GoodsCategory.PreciousMetal, 100f, 0.05f, false, 0f);
            AddGoodsDef(61, "银", GameEnums.GoodsCategory.PreciousMetal, 15f, 0.05f, false, 0f);
            AddGoodsDef(70, "武器", GameEnums.GoodsCategory.Equipment, 10f, 0.1f, false, 0f, 50, 0.3f);
            AddGoodsDef(71, "盔甲", GameEnums.GoodsCategory.Equipment, 15f, 0.2f, false, 0f, 50, 0.5f);
            AddGoodsDef(80, "丝绸", GameEnums.GoodsCategory.Luxury, 50f, 0.02f, false, 0f);
            AddGoodsDef(81, "香料", GameEnums.GoodsCategory.Luxury, 30f, 0.01f, false, 0f);
            AddGoodsDef(90, "奴隶", GameEnums.GoodsCategory.Slave, 30f, 0f, false, 0f);
            // 标记自然资源（地图资源点生成+物产前置门槛用）
            MarkNaturalResource(50, ResourceType.Mineral, 0.4f, -1);
            MarkNaturalResource(51, ResourceType.Mineral, 0.35f, -1);
            MarkNaturalResource(54, ResourceType.Mineral, 0.15f, 1104);
            MarkNaturalResource(58, ResourceType.Mineral, 0.3f, -1);
            MarkNaturalResource(30, ResourceType.Forest, 0.6f, -1);
            MarkNaturalResource(40, ResourceType.Mineral, 0.5f, -1);
            MarkNaturalResource(3, ResourceType.Special, 0.3f, 934);
        }

        private void MarkNaturalResource(int goodsId, ResourceType type, float abundance, int requiredInnovation)
        {
            if (!goodsDefs.TryGetValue(goodsId, out var def)) return;
            def.isNaturalResource = true;
            def.resourceType = type;
            def.baseAbundance = abundance;
            def.requiredInnovation = requiredInnovation;
            def.renewable = (type == ResourceType.Forest || type == ResourceType.WildAnimal || type == ResourceType.WildPlant || type == ResourceType.Aquatic);
            goodsDefs[goodsId] = def;
        }


        private void AddGoodsDef(int id, string name, GameEnums.GoodsCategory category,
            float baseValue, float weight, bool hasShelfLife, float shelfLifeDays,
            int processedFrom = -1, float processingRatio = 1f)
        {
            goodsDefs[id] = new GoodsDef
            {
                goodsId = id,
                goodsName = name,
                category = category,
                baseValue = baseValue,
                weight = weight,
                hasShelfLife = hasShelfLife,
                shelfLifeDays = shelfLifeDays,
                processedFromId = processedFrom,
                processingRatio = processingRatio,
                originBiomes = new List<GameEnums.BiomeType>()
            };
        }


        private void InitializeUnitDefs()
        {
 // 步兵（兵种×革新：重装需铁制武器、精锐需炼钢、弩手需弩；物资：武器/盔甲）
            AddUnitDef(100, "轻装步兵", GameEnums.UnitCategory.Infantry, 1, 10f, 0f, 8f, 60f, 2f, 1f, 50f, 1f);
            AddUnitDef(101, "重装步兵", GameEnums.UnitCategory.Infantry, 2, 15f, 0f, 15f, 70f, 1.5f, 1.5f, 80f, 1.5f, 301);
            AddUnitDef(102, "精锐步兵", GameEnums.UnitCategory.Infantry, 3, 20f, 0f, 22f, 80f, 1.2f, 2f, 120f, 2f, 203);
            AddUnitDef(110, "弓箭手", GameEnums.UnitCategory.Infantry, 1, 5f, 12f, 6f, 50f, 2f, 0.8f, 40f, 1f, 907);
            AddUnitDef(111, "弩手", GameEnums.UnitCategory.Infantry, 2, 6f, 18f, 8f, 55f, 1.5f, 1f, 60f, 1.2f, 304);
 // 骑兵（轻骑需骑乘术、重骑需马镫、精锐需重装骑兵；物资：武器+盔甲+马）
            AddUnitDef(200, "轻骑兵", GameEnums.UnitCategory.Cavalry, 1, 12f, 5f, 8f, 65f, 4f, 1.5f, 60f, 1f, 923);
            AddUnitDef(201, "重骑兵", GameEnums.UnitCategory.Cavalry, 2, 20f, 8f, 18f, 75f, 3f, 2.5f, 100f, 1.5f, 924);
            AddUnitDef(202, "精锐骑兵", GameEnums.UnitCategory.Cavalry, 3, 28f, 10f, 25f, 85f, 2.5f, 3f, 150f, 2f, 303);
            AddUnitDef(203, "超重装骑兵", GameEnums.UnitCategory.Cavalry, 4, 36f, 12f, 32f, 95f, 2f, 4f, 200f, 3f, 1006); // 具装甲骑（人马俱甲）
            AddUnitDef(210, "战车兵", GameEnums.UnitCategory.Cavalry, 2, 20f, 6f, 14f, 70f, 3.5f, 2.5f, 100f, 2f, 1013); // 双马战车（青铜时代战场主宰）
 // 水军（桨帆需桨帆船、战舰需克拉克、撞角需撞角战术、远洋贸易船需远洋贸易）
            AddUnitDef(300, "桨帆船", GameEnums.UnitCategory.Navy, 1, 15f, 10f, 10f, 60f, 3f, 2f, 80f, 1f, 402);
            AddUnitDef(301, "帆船战舰", GameEnums.UnitCategory.Navy, 2, 20f, 15f, 15f, 70f, 4f, 2.5f, 120f, 1.5f, 405);
            AddUnitDef(302, "撞角战船", GameEnums.UnitCategory.Navy, 2, 28f, 4f, 14f, 65f, 3.5f, 2.5f, 100f, 2f, 1007); // 舰首包铜铁撞角
            AddUnitDef(303, "远洋贸易船", GameEnums.UnitCategory.Navy, 3, 8f, 2f, 18f, 70f, 4.5f, 1.5f, 80f, 2.5f, 1008); // 商船/贸易护卫

 // 步兵：武器/盔甲
            SetUnitRecruitCosts(101, (70, 1.2f), (71, 1f));
            SetUnitRecruitCosts(102, (70, 1.5f), (71, 1.5f));
 // 骑兵：武器+盔甲+马
            SetUnitRecruitCosts(200, (70, 1f), (20, 1f));
            SetUnitRecruitCosts(201, (70, 1.2f), (71, 1.2f), (20, 1.5f));
            SetUnitRecruitCosts(202, (70, 1.5f), (71, 1.5f), (20, 2f));
            SetUnitRecruitCosts(203, (70, 2f), (71, 2f), (20, 2.5f)); // 超重装：人马双甲
            SetUnitRecruitCosts(210, (70, 1.2f), (20, 2f)); // 战车：武器+双马
 // 船只：原木/加工木材 + 铁矿/棉花
            SetUnitRecruitCosts(300, (30, 2f), (31, 1f));
            SetUnitRecruitCosts(301, (31, 2f), (50, 1f));
            SetUnitRecruitCosts(302, (30, 2f), (50, 1.5f)); // 撞角需铁
            SetUnitRecruitCosts(303, (31, 2f), (11, 1f));   // 远洋船需加工木+帆布（棉花）
        }


 /// <summary>设置兵种招募物资（经济系统 goods 对接——goodsId → 数量）</summary>
        private void SetUnitRecruitCosts(int unitId, params (int goodsId, float amount)[] costs)
        {
            if (!unitDefs.TryGetValue(unitId, out var def)) return;
            def.recruitCost = new Dictionary<int, float>();
            foreach (var (goodsId, amount) in costs)
                def.recruitCost[goodsId] = amount;
            unitDefs[unitId] = def; // struct 需回写
        }


        private void AddUnitDef(int id, string name, GameEnums.UnitCategory category, int tier,
            float melee, float ranged, float def, float morale, float speed, float supply,
            float manpower, float recruitCost, params int[] requiredInnovations)
        {
            unitDefs[id] = new UnitDef
            {
                unitId = id,
                unitName = name,
                category = category,
                tier = tier,
                meleeAttack = melee,
                rangedAttack = ranged,
                defense = def,
                morale = morale,
                speed = speed,
                supplyConsumption = supply,
                manpowerCost = manpower,
                requiredInnovations = new List<int>(requiredInnovations),
                recruitCost = new Dictionary<int, float> { { 70, recruitCost } },
                terrainModifiers = new Dictionary<GameEnums.TerrainTacticType, float>()
            };
        }


        private void InitializeTradeCenters()
        {
            int regionCount = (tiles.Length + 15) / 16;
            for (int r = 0; r < regionCount; r++)
            {
                var center = new TradeCenter
                {
                    regionId = r,
                    centerName = $"地区{r}贸易中心",
                    centerTileIndex = r * 16
                };
 // 开局启动粮储（物资 id 0 = 粮食）：经济产出尚未运转前，避免有人口的地块瞬间全判饥荒
                center.inventory[0] = 1000f;
                tradeCenters[r] = center;
            }
        }


        private void InitializeDefaultRaces()
        {
 // 当前仅人类
            races[0] = new RaceData
            {
                raceId = 0,
                raceName = "人族",
                baseLifespan = 1.0f,
                growthRate = 1.0f,
                reproductionRate = 1.0f,
                physicalStrength = 1.0f,
                diseaseResistance = 1.0f,
                environmentalTolerance = 1.0f,
                transformativity = 50f
            };
        }


        private void InitializeDefaultCultures()
        {
            cultures[0] = new CultureData
            {
                cultureId = 0,
                cultureName = "中原文化",
                stage = GameEnums.CultureStage.HighCivilization,
                livelihoodType = 0,
                mobilityType = 0,
                burialType = 0,
                worshipVector = new float[] { 0.8f, 0.1f, 0.1f },
                materialStyle = 0,
                symbolicFocus = 0,
                environmentAdapt = 0,
                maturity = 0.8f
            };
            cultures[1] = new CultureData
            {
                cultureId = 1,
                cultureName = "游牧文化",
                stage = GameEnums.CultureStage.Chiefdom,
                livelihoodType = 1,
                mobilityType = 1,
                burialType = 1,
                worshipVector = new float[] { 0.1f, 0.8f, 0.1f },
                materialStyle = 1,
                symbolicFocus = 1,
                environmentAdapt = 1,
                maturity = 0.5f
            };
            cultures[2] = new CultureData
            {
                cultureId = 2,
                cultureName = "海洋文化",
                stage = GameEnums.CultureStage.HighCivilization,
                livelihoodType = 2,
                mobilityType = 0,
                burialType = 2,
                worshipVector = new float[] { 0.1f, 0.1f, 0.8f },
                materialStyle = 2,
                symbolicFocus = 2,
                environmentAdapt = 2,
                maturity = 0.7f
            };
        }


        internal void InitializeDefaultRealms()
        {
 // 找到陆地地块分配给初始政权
            var landTiles = new List<int>();
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i].isLand)
                    landTiles.Add(i);
            }

            if (landTiles.Count == 0) return;

 // 创建3个初始政权，各占1/3陆地
            int perRealm = landTiles.Count / 3;
            for (int r = 0; r < 3; r++)
            {
                var realm = new RealmData
                {
                    realmId = r,
                    realmName = r == 0 ? "中原王朝" : (r == 1 ? "游牧汗国" : "海洋城邦"),
                    treasury = 1000f,
                    prestige = 50f,
                    stability = 70f,
                    centralization = 0.5f
                };

                int start = r * perRealm;
                int end = (r == 2) ? landTiles.Count : (r + 1) * perRealm;
                for (int i = start; i < end; i++)
                {
                    int tileIdx = landTiles[i];
                    tiles[tileIdx].ownerRealmId = r;
                    realm.coreTiles.Add(tileIdx);

 // 给每块地分配初始人口（，全部人族）
                    if (tiles[tileIdx].populationBlocks.Count == 0)
                    {
                        tiles[tileIdx].populationBlocks.Add(new PopulationBlock
                        {
                            raceId = 0,
                            cultureId = r % 3,
                            count = 10f,
                            satisfaction = 60f,
                            socialClass = GameEnums.SocialClass.Peasant
                        });
                    }
                }

                realms[r] = realm;

 // 为每个政权创建AI控制器
                _aiManager.CreateController(r);
            }

 // 为每个政权创建统治者与配偶（角色系统与生育机制的源头）
            _characterManager.CreateInitialRulers(realms, currentYear);

 // 外交关系懒加载：首次GetRelation时自动创建
        }


 /// 内容注册表覆盖（数据驱动优先）
 /// ContentRegistry 已加载的 Base/Mods 内容按 id 覆盖内置默认；
 /// 与 ContentRegistry 的 "Mods 同名 Id 覆盖 Base" 语义一致。
 /// 注意：内容包 cultureId/raceId 须 > 0（ContentRegistry 的约定），内置默认为 0,1,2，
 /// 内容 id 与内置重叠时以内容为准。
        private void ApplyContentOverrides()
        {
            if (!ContentRegistry.IsInitialized) return;

            int raceOverrides = 0;
            foreach (var kv in ContentRegistry.Races)
            {
                races[kv.Key] = kv.Value;
                raceOverrides++;
            }

            int cultureOverrides = 0;
            foreach (var kv in ContentRegistry.Cultures)
            {
                cultures[kv.Key] = kv.Value.data;
                cultureOverrides++;
            }

 // 文化地图颜色自动分配（未配置的文化按 id 取色）
            foreach (var kv in cultures)
                if (kv.Value.color == Color.white)
                    kv.Value.color = Color.HSVToRGB((kv.Key * 0.618f) % 1f, 0.55f, 0.85f);

            if (raceOverrides > 0 || cultureOverrides > 0)
                Debug.Log($"[GameWorld] 内容注册表覆盖：种族 +{raceOverrides}，文化 +{cultureOverrides}（数据驱动优先）");

            InitializeFaithSystems();
        }


 /// <summary>初始化宗教运行时（每教统一个 FaithSystem——热忱/美德罪行/领袖）</summary>
        private void InitializeFaithSystems()
        {
            _faithSystems.Clear();
            foreach (var kv in Culture.ReligionCatalog.All)
            {
                var def = kv.Value;
                if (def.nodeType != Culture.ReligionNodeType.Succession) continue; // 只教统有运行时
                var faith = new FaithSystem
                {
                    faithId = def.religionId,
                    faithName = def.religionName,
                    fervor = 50f,
                    highPriestCharacterId = -1
                };
                faith.virtues.AddRange(def.virtues);
                faith.sins.AddRange(def.sins);
                _faithSystems.Add(faith);
            }
            Debug.Log($"[GameWorld] 宗教运行时初始化：{_faithSystems.Count} 个教统");
        }

    }
}
