using System.Collections.Generic;
using System.Linq;
using CivilizationEvolution.Core;
using UnityEngine;

namespace CivilizationEvolution.Map
{
    /// <summary>
    /// 建筑可用性检查系统
    /// 负责：为每种建筑定义具体修建条件、检查地块可用性、UI过滤接口
    /// 特定建筑受地形/水文/群系限制，不符合条件的不在UI显示
    /// </summary>
    public static class BuildingAvailabilitySystem
    {
        // ===== 建筑条件注册表 =====
        private static Dictionary<BuildableType, BuildingRequirement> _requirements;
        private static bool _initialized;

        /// <summary>初始化建筑条件注册表</summary>
        public static void Initialize()
        {
            if (_initialized) return;
            _requirements = new Dictionary<BuildableType, BuildingRequirement>();
            RegisterAllRequirements();
            _initialized = true;
        }

        /// <summary>注册所有建筑的修建条件</summary>
        private static void RegisterAllRequirements()
        {
            // ===== 堡垒亚型 =====
            Register(BuildableType.Barrier, new BuildingRequirement
            {
                name = "关隘",
                requireBottleneck = true,
                requireLand = true,
                minSlope = 5f,
                maxSlope = 40f,
                description = "必须建在狭窄通道（山口/峡谷/海峡），直接阻挡敌对势力通行，需攻破才能通过",
                allowedBiomes = new[] { GameEnums.BiomeType.FoldMountains, GameEnums.BiomeType.KarstMountains, GameEnums.BiomeType.LowHills, GameEnums.BiomeType.HighMountains, GameEnums.BiomeType.CoastalLowland, GameEnums.BiomeType.Fjord }
            });

            Register(BuildableType.PassFort, new BuildingRequirement
            {
                name = "关口堡垒",
                requireBottleneck = true,
                minElevation = 0.3f,
                maxSlope = 45f,
                description = "建在关隘附近的堡垒，区域控制（敌对经过有损耗/减速，己方给补给支援），不直接阻挡通行",
                allowedBiomes = new[] { GameEnums.BiomeType.FoldMountains, GameEnums.BiomeType.KarstMountains, GameEnums.BiomeType.LowHills, GameEnums.BiomeType.HighMountains }
            });

            Register(BuildableType.HighlandKeep, new BuildingRequirement
            {
                name = "高地堡",
                requireHills = true,
                minElevation = 0.5f,
                maxElevation = 0.8f,
                minSlope = 5f,
                maxSlope = 40f,
                description = "必须建在丘陵高地（海拔0.5-0.8）",
                allowedBiomes = new[] { GameEnums.BiomeType.LowHills, GameEnums.BiomeType.FoldMountains, GameEnums.BiomeType.BrokenPlateau, GameEnums.BiomeType.LoessPlateau }
            });

            Register(BuildableType.ManorFort, new BuildingRequirement
            {
                name = "坞堡庄园",
                requirePlain = true,
                maxElevation = 0.5f,
                maxSlope = 10f,
                minPrecipitation = 300f,
                description = "必须建在肥沃农耕平原",
                allowedBiomes = new[] { GameEnums.BiomeType.AlluvialPlain, GameEnums.BiomeType.GreatRiverPlain, GameEnums.BiomeType.Delta, GameEnums.BiomeType.Interfluvial, GameEnums.BiomeType.TemperateGrassland, GameEnums.BiomeType.SedimentaryBasin }
            });

            Register(BuildableType.PlainGarrison, new BuildingRequirement
            {
                name = "平原屯堡",
                requirePlain = true,
                maxElevation = 0.4f,
                maxSlope = 5f,
                description = "必须建在开阔平原（无天险时沿边境线建防御带）",
                allowedBiomes = new[] { GameEnums.BiomeType.AlluvialPlain, GameEnums.BiomeType.GreatRiverPlain, GameEnums.BiomeType.TemperateGrassland, GameEnums.BiomeType.Savanna, GameEnums.BiomeType.SemiAridShrubland }
            });

            Register(BuildableType.EstuaryFort, new BuildingRequirement
            {
                name = "河口堡",
                requireRiverMouth = true,
                requireCoast = true,
                requireRiver = true,
                description = "必须建在大河河口（河流+海岸交汇处）"
            });

            Register(BuildableType.StarFort, new BuildingRequirement
            {
                name = "棱堡",
                requireExistingSettlement = true,
                minSettlementLevel = SettlementLevel.LevelIII,
                minTechLevel = 4, // 火药时代
                maxSlope = 15f,
                description = "需要火药技术（T≥4）和Ⅲ级以上聚落，火炮时代的星形防御工事"
            });

            Register(BuildableType.HillFort, new BuildingRequirement
            {
                name = "垒寨",
                requireHills = true,
                minElevation = 0.4f,
                maxElevation = 0.75f,
                minSlope = 8f,
                description = "建在山丘顶部的临时/半永久军事营地"
            });

            Register(BuildableType.BorderFort, new BuildingRequirement
            {
                name = "边堡",
                requireLand = true,
                description = "边境线小型据点，预警+驻扎（地形要求低）"
            });

            Register(BuildableType.CoastalFort, new BuildingRequirement
            {
                name = "海岸堡垒",
                requireCoast = true,
                minSlope = 3f,
                description = "必须沿海，保护港口/海岸线，防海上入侵"
            });

            Register(BuildableType.RiverFort, new BuildingRequirement
            {
                name = "河防堡垒",
                requireRiver = true,
                description = "必须沿河，控制河流渡口/航道"
            });

            Register(BuildableType.MountainFortress, new BuildingRequirement
            {
                name = "山地要塞",
                requireMountain = true,
                minElevation = 0.7f,
                minSlope = 20f,
                description = "必须建在崇山峻岭中（海拔>0.7，坡度>20°）",
                allowedBiomes = new[] { GameEnums.BiomeType.HighMountains, GameEnums.BiomeType.FoldMountains, GameEnums.BiomeType.KarstMountains, GameEnums.BiomeType.AlpineMeadow }
            });

            Register(BuildableType.IslandFort, new BuildingRequirement
            {
                name = "岛屿要塞",
                requireIsland = true,
                requireCoast = true,
                description = "必须建在海岛/河心岛，控制水域"
            });

            Register(BuildableType.SiegeCastle, new BuildingRequirement
            {
                name = "攻城城堡",
                requireLand = true,
                description = "前线推进基地，围攻敌方城市（需在敌方领土附近）"
            });

            Register(BuildableType.RoyalCastle, new BuildingRequirement
            {
                name = "王城/宫堡",
                requireExistingSettlement = true,
                minSettlementLevel = SettlementLevel.LevelIV,
                minDevelopment = 60f,
                description = "君主居所+行政中心+防御，需Ⅳ级以上都会"
            });

            Register(BuildableType.AbbeyFort, new BuildingRequirement
            {
                name = "修道院堡垒",
                requireExistingSettlement = true,
                minSettlementLevel = SettlementLevel.LevelII,
                description = "宗教武装据点（圣殿骑士团式），需有宗教建筑"
            });

            Register(BuildableType.TradingFort, new BuildingRequirement
            {
                name = "商站堡垒",
                requireCoast = true,
                requireExistingSettlement = false,
                minDevelopment = 10f,
                description = "贸易据点+防御（东印度公司式），沿海或沿河"
            });

            // ===== 特殊城形态 =====
            Register(BuildableType.MountainCity, new BuildingRequirement
            {
                name = "山城",
                requireMountain = true,
                minElevation = 0.6f,
                minSlope = 15f,
                description = "山顶/山脊建城，居高临下"
            });

            Register(BuildableType.WaterCity, new BuildingRequirement
            {
                name = "水城",
                requireRiver = true,
                requireCoast = false,
                maxElevation = 0.35f,
                maxSlope = 3f,
                minPrecipitation = 800f,
                description = "水网密集区，运河为街（威尼斯/苏州式）"
            });

            Register(BuildableType.StarCity, new BuildingRequirement
            {
                name = "星城",
                requireExistingSettlement = true,
                minSettlementLevel = SettlementLevel.LevelIII,
                minTechLevel = 4,
                maxSlope = 10f,
                description = "棱堡时代星形防御工事环绕，需火药技术"
            });

            Register(BuildableType.CitadelCity, new BuildingRequirement
            {
                name = "堡城",
                requireExistingSettlement = true,
                minSettlementLevel = SettlementLevel.LevelII,
                description = "核心要塞+外围城区，军事主导"
            });

            Register(BuildableType.PlannedCity, new BuildingRequirement
            {
                name = "规划城",
                requirePlain = true,
                maxElevation = 0.45f,
                maxSlope = 8f,
                minExistingSettlementLevel = SettlementLevel.LevelIII,
                description = "完全人工规划的新城（迁都/殖民），需平坦地形"
            });

            // ===== 港口设施 =====
            Register(BuildableType.DeepWaterPort, new BuildingRequirement
            {
                name = "深水港",
                requireCoast = true,
                minSlope = 8f, // 深水海岸（坡度大=水深）
                requireExistingSettlement = true,
                minSettlementLevel = SettlementLevel.LevelII,
                minDevelopment = 25f,
                description = "可停泊大型船只，需深水海岸（坡度>8°）+Ⅱ级以上聚落"
            });

            Register(BuildableType.RiverPort, new BuildingRequirement
            {
                name = "内河港",
                requireRiver = true,
                maxElevation = 0.5f,
                description = "河流沿岸港口，内陆-沿海转运"
            });

            Register(BuildableType.ImperialPort, new BuildingRequirement
            {
                name = "帝国港",
                requireCoast = true,
                requireExistingSettlement = true,
                minSettlementLevel = SettlementLevel.LevelIV,
                minDevelopment = 70f,
                minSlope = 10f,
                description = "全球贸易中心+主力舰队母港，需Ⅳ级以上都会+深水海岸"
            });

            // ===== 特殊设施 =====
            Register(BuildableType.Ferry, new BuildingRequirement
            {
                name = "渡口",
                requireRiver = true,
                maxSlope = 5f,
                description = "河流浅滩/可涉水渡河点"
            });

            Register(BuildableType.PostStation, new BuildingRequirement
            {
                name = "驿站",
                requireExistingSettlement = true,
                minSettlementLevel = SettlementLevel.LevelI,
                description = "官方信使/军队调动中继站"
            });

            Register(BuildableType.Watchtower, new BuildingRequirement
            {
                name = "烽火台/瞭望塔",
                requireLand = true,
                minElevation = 0.3f,
                description = "军事预警网络，优先高地"
            });

            Register(BuildableType.Granary, new BuildingRequirement
            {
                name = "粮仓",
                requireExistingSettlement = true,
                minSettlementLevel = SettlementLevel.LevelII,
                minPrecipitation = 200f,
                description = "军队后勤补给点，需农业区"
            });

            Register(BuildableType.Market, new BuildingRequirement
            {
                name = "市集",
                requireExistingSettlement = true,
                minSettlementLevel = SettlementLevel.LevelI,
                minDevelopment = 5f,
                description = "商业贸易节点"
            });

            Register(BuildableType.Temple, new BuildingRequirement
            {
                name = "神庙",
                requireExistingSettlement = true,
                minSettlementLevel = SettlementLevel.LevelI,
                description = "宗教祭祀建筑"
            });

            Register(BuildableType.University, new BuildingRequirement
            {
                name = "大学/学府",
                requireExistingSettlement = true,
                minSettlementLevel = SettlementLevel.LevelIII,
                minDevelopment = 40f,
                description = "高等教育/学术中心，需Ⅲ级以上城邑"
            });

            Register(BuildableType.Workshop, new BuildingRequirement
            {
                name = "作坊/工场",
                requireExistingSettlement = true,
                minSettlementLevel = SettlementLevel.LevelII,
                description = "手工业/制造业"
            });

            Register(BuildableType.Mine, new BuildingRequirement
            {
                name = "矿场",
                requireHills = true,
                minElevation = 0.4f,
                minSlope = 10f,
                description = "矿产开采，需丘陵/山地"
            });

            Register(BuildableType.SaltWorks, new BuildingRequirement
            {
                name = "盐场",
                requireCoast = true,
                maxSlope = 3f,
                minTemperature = 10f,
                description = "海盐晒制，需平坦海岸+温暖气候",
                allowedBiomes = new[] { GameEnums.BiomeType.CoastalLowland, GameEnums.BiomeType.CoastalSaltMarsh, GameEnums.BiomeType.HotDesert, GameEnums.BiomeType.CoastalDesert }
            });

            Register(BuildableType.Fishery, new BuildingRequirement
            {
                name = "渔场",
                requireCoast = true,
                description = "渔业基地，需沿海"
            });

            Register(BuildableType.Shipyard, new BuildingRequirement
            {
                name = "造船厂",
                requireCoast = true,
                requireExistingSettlement = true,
                minSettlementLevel = SettlementLevel.LevelII,
                minDevelopment = 30f,
                description = "舰船建造，需沿海港口+Ⅱ级以上聚落"
            });

            Register(BuildableType.Barracks, new BuildingRequirement
            {
                name = "兵营",
                requireExistingSettlement = true,
                minSettlementLevel = SettlementLevel.LevelII,
                description = "军队驻扎/训练"
            });

            Register(BuildableType.Arsenal, new BuildingRequirement
            {
                name = "军械库",
                requireExistingSettlement = true,
                minSettlementLevel = SettlementLevel.LevelIII,
                minDevelopment = 35f,
                description = "武器/盔甲制造与储存"
            });

            Register(BuildableType.Aqueduct, new BuildingRequirement
            {
                name = "引水渠/水道",
                requireExistingSettlement = true,
                minSettlementLevel = SettlementLevel.LevelIII,
                minDevelopment = 45f,
                description = "城市供水系统，需Ⅲ级以上城邑"
            });

            Register(BuildableType.Sewer, new BuildingRequirement
            {
                name = "下水道",
                requireExistingSettlement = true,
                minSettlementLevel = SettlementLevel.LevelIII,
                minDevelopment = 50f,
                description = "城市排污系统"
            });

            Register(BuildableType.Bathhouse, new BuildingRequirement
            {
                name = "公共浴场",
                requireExistingSettlement = true,
                minSettlementLevel = SettlementLevel.LevelII,
                minDevelopment = 25f,
                description = "公共洗浴/社交场所"
            });

            Register(BuildableType.Library, new BuildingRequirement
            {
                name = "图书馆",
                requireExistingSettlement = true,
                minSettlementLevel = SettlementLevel.LevelIII,
                minDevelopment = 40f,
                description = "知识保存/学术研究"
            });

            Register(BuildableType.Observatory, new BuildingRequirement
            {
                name = "观星台/天文台",
                requireExistingSettlement = true,
                minSettlementLevel = SettlementLevel.LevelIII,
                minElevation = 0.4f,
                description = "天文观测/历法制定，优先高地"
            });

            Register(BuildableType.Lighthouse, new BuildingRequirement
            {
                name = "灯塔",
                requireCoast = true,
                minSlope = 5f,
                description = "航海导航，需海岸高地"
            });
        }

        private static void Register(BuildableType type, BuildingRequirement req)
        {
            req.name = type.ToString();
            _requirements[type] = req;
        }

        // ===== 可用性检查 =====

        /// <summary>
        /// 检查地块是否满足某建筑的修建条件
        /// </summary>
        public static BuildingAvailability CheckAvailability(BuildableType type, TileData tile,
            BurgData existingBurg = null, int techLevel = 0)
        {
            Initialize();

            if (!_requirements.TryGetValue(type, out var req))
                return BuildingAvailability.Available(); // 未注册条件的默认允许

            var failed = new List<string>();

            // 地形检查
            if (req.requireLand && !tile.isLand) failed.Add("需要陆地");
            if (req.requireCoast && !tile.isCoast && tile.oceanTier != GameEnums.OceanTier.Coast) failed.Add("需要沿海");
            if (req.requireRiver && !tile.isRiver) failed.Add("需要河流");
            if (req.requireRiverMouth && (!tile.isCoast || !tile.isRiver)) failed.Add("需要河口（河流+海岸）");
            if (req.requireMountain && tile.elevation01 < 0.6f) failed.Add("需要山地");
            if (req.requireHills && (tile.elevation01 < 0.4f || tile.elevation01 > 0.8f)) failed.Add("需要丘陵");
            if (req.requirePlain && (tile.elevation01 > 0.5f || tile.slopeDegree > 10f)) failed.Add("需要平原");
            if (req.requireDesert && tile.annualPrecipMm > 400f) failed.Add("需要沙漠/干旱区");

            // 海拔/坡度检查
            if (tile.elevation01 < req.minElevation) failed.Add($"海拔不足（需≥{req.minElevation:0.00}）");
            if (tile.elevation01 > req.maxElevation) failed.Add($"海拔过高（需≤{req.maxElevation:0.00}）");
            if (tile.slopeDegree < req.minSlope) failed.Add($"坡度不足（需≥{req.minSlope:0}°）");
            if (tile.slopeDegree > req.maxSlope) failed.Add($"坡度过大（需≤{req.maxSlope:0}°）");

            // 气候检查
            if (tile.annualPrecipMm < req.minPrecipitation) failed.Add($"降水不足（需≥{req.minPrecipitation:0}mm）");
            if (tile.annualPrecipMm > req.maxPrecipitation) failed.Add($"降水过多（需≤{req.maxPrecipitation:0}mm）");
            if (tile.annualTemp < req.minTemperature) failed.Add($"温度过低（需≥{req.minTemperature:0}°C）");
            if (tile.annualTemp > req.maxTemperature) failed.Add($"温度过高（需≤{req.maxTemperature:0}°C）");

            // 群系检查
            if (req.allowedBiomes != null && req.allowedBiomes.Length > 0 &&
                !req.allowedBiomes.Contains(tile.biome))
                failed.Add($"群系不允许（当前{tile.biome}）");
            if (req.forbiddenBiomes != null && req.forbiddenBiomes.Contains(tile.biome))
                failed.Add($"群系被禁止（{tile.biome}）");

            // 海洋等级检查
            if (req.allowedOceanTiers != null && req.allowedOceanTiers.Length > 0 &&
                !req.allowedOceanTiers.Contains(tile.oceanTier))
                failed.Add($"海洋等级不允许（当前{tile.oceanTier}）");

            // 瓶颈检查
            if (req.requireBottleneck)
            {
                bool isBottleneck = tile.slopeDegree > 15f || tile.isCoast ||
                    (tile.isRiver && tile.slopeDegree > 10f);
                if (!isBottleneck) failed.Add("需要瓶颈节点（山口/峡谷/海峡）");
            }

            // 岛屿检查（简化：沿海且周围3格内全是海洋）
            if (req.requireIsland && !tile.isCoast)
                failed.Add("需要岛屿");

            // 已有聚落检查
            if (req.requireExistingSettlement && existingBurg == null)
                failed.Add("需要已有聚落");
            if (existingBurg != null && req.minSettlementLevel > existingBurg.settlementLevel)
                failed.Add($"聚落等级不足（需≥{req.minSettlementLevel}）");
            if (existingBurg != null && req.minExistingSettlementLevel > existingBurg.settlementLevel)
                failed.Add($"已有聚落等级不足（需≥{req.minExistingSettlementLevel}）");
            if (existingBurg != null && existingBurg.development < req.minDevelopment)
                failed.Add($"发展度不足（需≥{req.minDevelopment:0}）");

            // 科技检查
            if (techLevel < req.minTechLevel)
                failed.Add($"科技等级不足（需≥{req.minTechLevel}）");

            return failed.Count == 0
                ? BuildingAvailability.Available()
                : BuildingAvailability.Unavailable(failed);
        }

        /// <summary>
        /// 获取某地块可修建的所有建筑列表（UI过滤接口）
        /// 不符合条件的建筑不在列表中显示
        /// </summary>
        public static List<BuildableType> GetAvailableBuildings(TileData tile,
            BurgData existingBurg = null, int techLevel = 0)
        {
            Initialize();
            var available = new List<BuildableType>();

            foreach (var type in _requirements.Keys)
            {
                if (CheckAvailability(type, tile, existingBurg, techLevel).available)
                    available.Add(type);
            }

            return available;
        }

        /// <summary>
        /// 获取某建筑的修建条件描述（用于UI tooltip）
        /// </summary>
        public static string GetRequirementDescription(BuildableType type)
        {
            Initialize();
            if (_requirements.TryGetValue(type, out var req))
                return req.description;
            return "无特殊条件";
        }

        /// <summary>
        /// 获取所有已注册的建筑类型
        /// </summary>
        public static IEnumerable<BuildableType> GetAllBuildableTypes()
        {
            Initialize();
            return _requirements.Keys;
        }
    }
}
