using System.Collections.Generic;


using MyriadCreation.Core;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;


namespace MyriadCreation.World.Settlement
{
    /// <summary>
    /// 聚居点进化树系统
    /// 定义不同起源方式的聚居点的发展路径和各阶段名称。
    ///
    /// 核心思想：不是简单的等级制（村落→集镇→城邑），而是进化树——
    /// 不同起源方式的聚落在不同分支上演化，同一"规模"的聚落因为起源不同表现完全不同。
    ///
    /// 重要调整：去掉了硬性的经济起源分类（农业/渔业/矿业/牧业），
    /// 正常的定居点发展使用 NaturalGrowth，具体的经济成分由成分系统（EconomicComposition）决定。
    /// 地理条件和物产自然决定了城市的经济成分比例，不需要人为分类。
    ///
    /// 保留的特殊起源方式：
    /// - 建城（PlannedCity）：人为直接建城，不是自然发展
    /// - 堡垒起源（FortressGrowth）：从军事堡垒发展来的
    /// - 城堡起源（CastleGrowth）：从贵族城堡发展来的
    /// - 港口起源（PortDevelopment）：从港口/渡口发展来的
    ///
    /// 阶段名称基于规模和功能，不是基于政治地位（没有"帝都"这种东西）。
    /// 最高级统一为"大都会"，中间阶段根据起源有不同的特色名称。
    /// </summary>
    public static class SettlementEvolutionTree
    {
        // ===== 进化路径阶段名称定义 =====
        // 每个路径有5个阶段，对应 SettlementLevel Ⅰ-Ⅴ级
        // 最高级统一为"大都会"，中间阶段根据起源有不同的特色名称

        /// <summary>自然发展：村落→集镇→城邑→都会→大都会（最常见，正常的定居点发展）</summary>
        private static readonly string[] NaturalStages =
        {
            "村落",       // Ⅰ级：小型定居点
            "集镇",       // Ⅱ级：周边产品的集散地
            "城邑",       // Ⅲ级：有城墙和市场的区域中心
            "都会",       // Ⅳ级：区域经济文化中心
            "大都会"      // Ⅴ级：跨区域的超级城市
        };

        /// <summary>规划建城：直接建城（迁都/殖民/军屯），从Ⅲ级开始</summary>
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
            { UpgradePath.NaturalGrowth, NaturalStages },
            { UpgradePath.PlannedCity, PlannedStages },
            { UpgradePath.FortressGrowth, FortressStages },
            { UpgradePath.CastleGrowth, CastleStages },
            { UpgradePath.PortDevelopment, PortStages }
        };

        // ===== 进化路径最大等级限制（某些起源不适合发展到最高级）=====
        private static readonly Dictionary<UpgradePath, SettlementLevel> PathMaxLevel = new()
        {
            { UpgradePath.NaturalGrowth, SettlementLevel.LevelV },
            { UpgradePath.PlannedCity, SettlementLevel.LevelV },
            { UpgradePath.FortressGrowth, SettlementLevel.LevelV },
            { UpgradePath.CastleGrowth, SettlementLevel.LevelV },
            { UpgradePath.PortDevelopment, SettlementLevel.LevelV }
        };

        /// <summary>获取聚居点当前阶段的名称（根据进化路径和等级）</summary>
        public static string GetStageName(SettlementData burg)
        {
            if (burg == null) return "未知";
            if (!PathStages.TryGetValue(burg.upgradePath, out var stages))
                stages = NaturalStages;

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
        public static bool IsAtMaxLevel(SettlementData burg)
        {
            return burg != null && burg.settlementLevel >= GetMaxLevel(burg.upgradePath);
        }

        /// <summary>
        /// 根据地块特征和初始类型推导起源方式
        /// 注意：这不再推导经济起源（农业/渔业/矿业/牧业），
        /// 只推导起源方式（自然发展/建城/堡垒/城堡/港口）。
        /// 具体的经济成分由成分系统（EconomicComposition）根据地理条件和物产自然决定。
        /// </summary>
        public static UpgradePath DeriveEvolutionPath(TileData tile, SettlementData burg)
        {
            // 1. 军事据点 → 堡垒起源
            if (burg.settlementType == SettlementType.Fort)
            {
                return UpgradePath.FortressGrowth;
            }

            // 2. 港口 → 港口起源
            if (burg.portTier >= PortTier.IntermediatePort)
                return UpgradePath.PortDevelopment;

            // 3. 贵族城堡 → 城堡起源（有城堡特征的据点）
            if (burg.fortSubtype == FortSubtype.RoyalCastle ||
                burg.fortSubtype == FortSubtype.ManorFort)
                return UpgradePath.CastleGrowth;

            // 4. 默认 → 自然发展（正常的定居点发展，经济成分由成分系统决定）
            return UpgradePath.NaturalGrowth;
        }

        /// <summary>获取进化路径的描述</summary>
        public static string GetPathDescription(UpgradePath path)
        {
            return path switch
            {
                UpgradePath.NaturalGrowth => "自然发展的定居点，经济成分由地理条件和物产自然决定",
                UpgradePath.PlannedCity => "人为规划建造的城市（迁都/殖民/军屯）",
                UpgradePath.FortressGrowth => "从军事堡垒发展而来的路径",
                UpgradePath.CastleGrowth => "从贵族城堡发展而来的路径",
                UpgradePath.PortDevelopment => "从港口/渡口发展而来的贸易路径",
                _ => "未知发展路径"
            };
        }
    }
}
