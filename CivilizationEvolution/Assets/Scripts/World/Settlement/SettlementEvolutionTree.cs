using System.Collections.Generic;


using CivilizationEvolution.Core;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Enums;


namespace CivilizationEvolution.World.Settlement
{
    /// <summary>
    /// 聚居点进化树系统
    /// 定义不同起源的聚居点的发展路径和各阶段名称。
    ///
    /// 核心思想：不是简单的等级制（村落→集镇→城邑），而是进化树——
    /// 不同起源的聚落在不同分支上演化，同一"规模"的聚落因为起源不同表现完全不同。
    ///
    /// 起源基于经济基础和地理条件，不是基于附加属性（宗教/商业/战略等）：
    /// - 定居点起源：农业、渔业、矿业、牧业、规划（直接建城）
    /// - 据点起源：堡垒、城堡、港口（条件满足时可发展为定居点）
    ///
    /// 阶段名称基于规模和功能，不是基于政治地位（没有"帝都"这种东西）。
    /// 最高级统一为"大都会"，中间阶段根据起源有不同的特色名称。
    /// </summary>
    public static class SettlementEvolutionTree
    {
        // ===== 进化路径阶段名称定义 =====
        // 每个路径有5个阶段，对应 SettlementLevel Ⅰ-Ⅴ级
        // 最高级统一为"大都会"，中间阶段根据起源有不同的特色名称

        /// <summary>农业起源：农业村→集镇→城邑→都会→大都会</summary>
        private static readonly string[] AgriculturalStages =
        {
            "农业村",     // Ⅰ级：以农业生产为主的小型村落
            "集镇",       // Ⅱ级：周边农产品的集散地
            "城邑",       // Ⅲ级：有城墙和市场的区域中心
            "都会",       // Ⅳ级：区域经济文化中心
            "大都会"      // Ⅴ级：跨区域的超级城市
        };

        /// <summary>渔业起源：渔村→渔业集镇→港口城市→贸易都会→大都会</summary>
        private static readonly string[] FisheryStages =
        {
            "渔村",       // Ⅰ级：以渔业为主的小型村落
            "渔业集镇",   // Ⅱ级：渔获集散地和小型码头
            "港口城市",   // Ⅲ级：有完善港口设施的城市
            "贸易都会",   // Ⅳ级：区域贸易中心
            "大都会"      // Ⅴ级：跨区域贸易枢纽
        };

        /// <summary>矿业起源：矿村→矿业集镇→矿业城市→工业都会→大都会</summary>
        private static readonly string[] MiningStages =
        {
            "矿村",       // Ⅰ级：矿工聚居的小型村落
            "矿业集镇",   // Ⅱ级：矿石集散地和初加工点
            "矿业城市",   // Ⅲ级：有冶炼和加工能力的城市
            "工业都会",   // Ⅳ级：区域手工业中心
            "大都会"      // Ⅴ级：跨区域制造业中心
        };

        /// <summary>牧业起源：牧业点→牧业集镇（通常最高Ⅱ级，不适合发展成大城市）</summary>
        private static readonly string[] PastoralStages =
        {
            "牧业点",     // Ⅰ级：牧民季节性聚居点
            "牧业集镇",   // Ⅱ级：畜牧产品集散地（通常到此为止）
            "牧业城镇",   // Ⅲ级：极少数条件极好的地方可达到
            "牧业都会",   // Ⅳ级：几乎不存在
            "大都会"      // Ⅴ级：几乎不存在
        };

        /// <summary>规划起源：直接建城（迁都/殖民/军屯），从Ⅲ级开始</summary>
        private static readonly string[] PlannedStages =
        {
            "新城",       // Ⅰ级：刚建立的新城（通常跳过）
            "府城",       // Ⅱ级：有初步规划的城市（通常跳过）
            "规划城",     // Ⅲ级：有完整规划的城市
            "都会",       // Ⅳ级：区域中心
            "大都会"      // Ⅴ级：跨区域中心
        };

        /// <summary>堡垒起源：堡垒→军镇→军事城市→军事都会→大都会</summary>
        private static readonly string[] FortressStages =
        {
            "堡垒",       // Ⅰ级：纯军事防御设施
            "军镇",       // Ⅱ级：驻军+家属+少量服务人员
            "军事城市",   // Ⅲ级：周围发展出平民区，军事仍主导
            "军事都会",   // Ⅳ级：区域军事中心
            "大都会"      // Ⅴ级：超级军事要塞城市
        };

        /// <summary>城堡起源：城堡→贵族城→行政城市→行政都会→大都会</summary>
        private static readonly string[] CastleStages =
        {
            "城堡",       // Ⅰ级：贵族居所+防御
            "贵族城",     // Ⅱ级：城堡周围发展出平民区
            "行政城市",   // Ⅲ级：成为区域行政中心
            "行政都会",   // Ⅳ级：高级行政中心
            "大都会"      // Ⅴ级：跨区域行政中心
        };

        /// <summary>港口起源：锚地/渡口→港口集镇→港口城市→贸易都会→大都会</summary>
        private static readonly string[] PortStages =
        {
            "锚地",       // Ⅰ级：简易停泊点/渡口
            "港口集镇",   // Ⅱ级：有码头和仓库的集镇
            "港口城市",   // Ⅲ级：有完善港口设施的城市
            "贸易都会",   // Ⅳ级：区域贸易中心
            "大都会"      // Ⅴ级：跨区域贸易枢纽
        };

        // ===== 进化路径→阶段名称映射 =====
        private static readonly Dictionary<UpgradePath, string[]> PathStages = new()
        {
            { UpgradePath.AgriculturalGrowth, AgriculturalStages },
            { UpgradePath.FisheryGrowth, FisheryStages },
            { UpgradePath.MiningGrowth, MiningStages },
            { UpgradePath.PastoralGrowth, PastoralStages },
            { UpgradePath.PlannedCity, PlannedStages },
            { UpgradePath.FortressGrowth, FortressStages },
            { UpgradePath.CastleGrowth, CastleStages },
            { UpgradePath.PortDevelopment, PortStages }
        };

        // ===== 进化路径最大等级限制（某些起源不适合发展到最高级）=====
        private static readonly Dictionary<UpgradePath, SettlementLevel> PathMaxLevel = new()
        {
            { UpgradePath.AgriculturalGrowth, SettlementLevel.LevelV },
            { UpgradePath.FisheryGrowth, SettlementLevel.LevelV },
            { UpgradePath.MiningGrowth, SettlementLevel.LevelV },
            { UpgradePath.PastoralGrowth, SettlementLevel.LevelII }, // 牧业通常最高Ⅱ级
            { UpgradePath.PlannedCity, SettlementLevel.LevelV },
            { UpgradePath.FortressGrowth, SettlementLevel.LevelV },
            { UpgradePath.CastleGrowth, SettlementLevel.LevelV },
            { UpgradePath.PortDevelopment, SettlementLevel.LevelV }
        };

        /// <summary>获取聚居点当前阶段的名称（根据进化路径和等级）</summary>
        public static string GetStageName(BurgData burg)
        {
            if (burg == null) return "未知";
            if (!PathStages.TryGetValue(burg.upgradePath, out var stages))
                stages = AgriculturalStages;

            int levelIndex = (int)burg.settlementLevel;
            if (levelIndex < 0) levelIndex = 0;
            if (levelIndex >= stages.Length) levelIndex = stages.Length - 1;

            return stages[levelIndex];
        }

        /// <summary>获取进化路径的最大等级限制</summary>
        public static SettlementLevel GetMaxLevel(UpgradePath path)
        {
            return PathMaxLevel.TryGetValue(path, out var maxLevel)
                ? maxLevel
                : SettlementLevel.LevelV;
        }

        /// <summary>检查聚居点是否达到进化路径的最大等级</summary>
        public static bool IsAtMaxLevel(BurgData burg)
        {
            return burg != null && burg.settlementLevel >= GetMaxLevel(burg.upgradePath);
        }

        /// <summary>
        /// 根据地块特征和初始类型推导进化路径（起源）
        /// 这是进化树的核心：不同起源决定不同的发展路径。
        /// 起源基于经济基础和地理条件，不是基于附加属性。
        /// </summary>
        public static UpgradePath DeriveEvolutionPath(TileData tile, BurgData burg)
        {
            // 1. 军事据点 → 堡垒起源
            if (burg.settlementType == SettlementType.Fort)
            {
                return UpgradePath.FortressGrowth;
            }

            // 2. 港口 → 港口起源
            if (burg.portTier >= PortTier.IntermediatePort)
                return UpgradePath.PortDevelopment;

            // 3. 沿海/沿河 + 非农业 → 渔业起源
            if ((tile.isCoast || tile.isRiver) &&
                burg.primaryFunction != SettlementFunction.Mining &&
                burg.primaryFunction != SettlementFunction.Military)
                return UpgradePath.FisheryGrowth;

            // 4. 山地 + 矿业 → 矿业起源
            if (tile.elevation01 > 0.65f && burg.primaryFunction == SettlementFunction.Mining)
                return UpgradePath.MiningGrowth;

            // 5. 干旱 + 无水源 → 牧业起源
            if (tile.annualPrecipMm < 300f && !tile.isRiver)
                return UpgradePath.PastoralGrowth;

            // 6. 默认 → 农业起源（最常见）
            return UpgradePath.AgriculturalGrowth;
        }

        /// <summary>获取进化路径的描述</summary>
        public static string GetPathDescription(UpgradePath path)
        {
            return path switch
            {
                UpgradePath.AgriculturalGrowth => "以农业生产为基础的自然发展路径",
                UpgradePath.FisheryGrowth => "以渔业和海洋贸易为核心的发展路径",
                UpgradePath.MiningGrowth => "以矿产开采和加工为核心的发展路径",
                UpgradePath.PastoralGrowth => "以畜牧业为核心的发展路径（通常规模有限）",
                UpgradePath.PlannedCity => "人为规划建造的城市（迁都/殖民/军屯）",
                UpgradePath.FortressGrowth => "从军事堡垒发展而来的路径",
                UpgradePath.CastleGrowth => "从贵族城堡发展而来的路径",
                UpgradePath.PortDevelopment => "从港口/渡口发展而来的贸易路径",
                _ => "未知发展路径"
            };
        }
    }
}
