using UnityEngine;
using System;
using System.Collections.Generic;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Map
{
 /// 子地块类型（对齐 CK3 男爵领 / FantasyMapSimulator Burg）
 /// 一个 Province 包含多个 Burg，Burg 是城镇/港口/首都的载体


 /// 聚落形态（宏观分类，区别于 BurgType 功能类型）
 /// 村镇/城/堡 是可缓慢演化的属性，不是永久固化标签


 /// 子地块（Burg / 男爵领）
 /// 省份内的可编辑定居点，是人口、贸易、军事的具体载体
 /// 对齐 FantasyMapSimulator: BurgData / BurgsAndStateGenerator / capitalBurgID / IsBurgPortQualified
    [Serializable]
    public class BurgData
    {
        public int burgId;
        public string burgName;
        public BurgType type;
        public int provinceId;       // 所属省份
        public int tileIndex;        // 所在地块（单元格）
        public float x;              // 地块内精确坐标（0~1，用于像素级定位）
        public float y;

 // 经济与人口
        public float population;     // Burg 人口（独立于地块人口块）
        public float development;    // 发展度（0~100）
        public float wealth;         // 财富
        public float tradePower;     // 贸易力量（港口更高）

 // 军事
        public float fortification;  // 防御等级（0~10）
        public int garrison;         // 驻军人数

 // 状态
        public bool isCapital;       // 是否政权首都
        public bool isPort;          // 是否港口（沿海或沿河）
        public bool isCoastal;       // 是否沿海
        public bool hasMarket;       // 是否有市场（贸易节点）
        public bool hasTemple;       // 是否有宗教建筑
        public bool hasUniversity;   // 是否有大学（高学识）

 // 建设等级（0~3，对应村庄→集镇→城市→大都市）
        public int buildLevel;

 // ===== 聚落形态系统（村镇/城/堡，可缓慢演化）=====
 /// <summary>聚落形态：村镇/城/堡</summary>
        public SettlementType settlementType;

 /// <summary>聚落等级（Ⅰ-Ⅴ级：村落→集镇→城邑→都会→大都会）</summary>
        public SettlementLevel settlementLevel;

 /// <summary>主功能类型（决定发展倾向）</summary>
        public SettlementFunction primaryFunction;

 /// <summary>次要功能（Flags枚举，可叠加）</summary>
        public SettlementFunction secondaryFunctions;

 /// <summary>城市重心（都会级Ⅳ-Ⅴ的核心发展方向）</summary>
        public CityFocus cityFocus;

 /// <summary>城的形态（圆城/方城/山城/水城等）</summary>
        public CityForm cityForm;

 /// <summary>堡垒亚型（关口堡/高地堡/坞堡/平原屯堡/河口堡等）</summary>
        public FortSubtype fortSubtype;

 /// <summary>港口层级（避风港/内河港/中转港/深水港/帝国港）</summary>
        public PortTier portTier;

 /// <summary>关隘瓶颈类型（山口/峡谷/海峡/沙漠走廊等）</summary>
        public BottleneckType bottleneckType;

 /// <summary>升级路线（自然生长/港口发展/军事发展/矿业发展等）</summary>
        public UpgradePath upgradePath;

 /// <summary>演化阶段（稳定/过渡中/萌芽/已转化/衰退中）</summary>
        public EvolutionStage evolutionStage;

 /// <summary>城墙等级（无→木栅栏→土堤→石墙→加固城墙→棱堡→巨型防御）</summary>
        public WallLevel wallLevel;

 /// <summary>形态演化进度（0~100），累积到阈值后完成形态切换</summary>
        public float settlementEvolution;

 /// <summary>当前形态的稳定度（0~100），越高越难被演化推动改变</summary>
        public float settlementStability;

 /// <summary>形态演化的目标方向（null表示自然演化——可空类型不参与 Unity 序列化）</summary>
        [System.NonSerialized] public SettlementType? evolutionTarget;

 /// <summary>距上次形态切换的Tick数（用于冷却期）</summary>
        public int ticksSinceLastTransition;

 /// <summary>建城Tick（用于计算城龄）</summary>
        public int foundingTick;

 /// <summary>母城ID（从哪个聚落发展/分化而来，-1表示无）</summary>
        public int parentBurgId = -1;

 // ===== 聚落控制/影响力范围系统 =====
 /// <summary>控制本聚落的高等级聚落ID（-1=独立，未被控制）</summary>
        public int controllerBurgId = -1;
 /// <summary>本聚落控制的低等级聚落ID列表</summary>
        [System.NonSerialized] public List<int> controlledBurgIds = new List<int>();
 /// <summary>对各下属聚落的控制进度（burgId -> 0~100）</summary>
        [System.NonSerialized] public Dictionary<int, float> controlProgress = new Dictionary<int, float>();
 /// <summary>影响力半径（地块数，按等级：Ⅰ=1~Ⅴ=5）</summary>
        public int influenceRadius = 1;
 /// <summary>驻扎部队平均质量（训练度0~100）</summary>
        [System.NonSerialized] public float garrisonQuality = 0f;
 /// <summary>驻扎部队数量</summary>
        [System.NonSerialized] public int garrisonQuantity = 0;
 /// <summary>控制是否稳固（进度到100后变为稳固，流失更慢）</summary>
        public bool isControlStable = false;

 /// <summary>关联瓶颈地块ID（关口堡/渡口城的控制节点，-1表示无）</summary>
        public int bottleneckTileIndex = -1;

 /// <summary>显示用名称（含类型前缀）</summary>
        public string DisplayName => type switch
        {
            BurgType.Capital => $"【首都】{burgName}",
            BurgType.City => $"【城】{burgName}",
            BurgType.Port => $"【港】{burgName}",
            BurgType.Fortress => $"【寨】{burgName}",
            BurgType.Town => $"【镇】{burgName}",
            _ => burgName
        };

 /// <summary>是否为主要定居点（城市/港口/首都/要塞）</summary>
        public bool IsMajorSettlement =>
            type == BurgType.City || type == BurgType.Port ||
            type == BurgType.Capital || type == BurgType.Fortress;

 /// <summary>形态显示名称</summary>
        public string SettlementTypeName => settlementType switch
        {
            SettlementType.Village => "村镇",
            SettlementType.City => "城",
            SettlementType.Fort => "堡",
            _ => "未知"
        };

 /// <summary>形态等级上限（软性约束，AI遵循，玩家可突破）</summary>
        public int MaxBuildLevelForType => settlementType switch
        {
            SettlementType.Village => 1,  // 村镇最高到集镇（Ⅱ级），极少数交通要道可到Ⅲ级
            SettlementType.City => 3,     // 城可到Ⅳ-Ⅴ级大都会
            SettlementType.Fort => 3,     // 堡等级跨度完整，可到Ⅳ-Ⅴ级巨型要塞
            _ => 3
        };

 /// <summary>该形态的初始军政倾向权重</summary>
        public float MilitaryWeightBase => settlementType switch
        {
            SettlementType.Village => 0.2f,  // 村镇军政天然偏低
            SettlementType.City => 0.5f,      // 城四类倾向自由发展
            SettlementType.Fort => 0.8f,      // 堡军政初始权重很高
            _ => 0.5f
        };

 /// <summary>该形态的初始经贸倾向权重</summary>
        public float EconomyWeightBase => settlementType switch
        {
            SettlementType.Village => 0.7f,  // 村镇经贸、农耕产出偏高
            SettlementType.City => 0.6f,      // 城可经济主导
            SettlementType.Fort => 0.3f,      // 堡经贸通常偏低
            _ => 0.5f
        };
    }

 /// 子地块生成器
 /// 对齐 FantasyMapSimulator: BurgsAndStateGenerator
 /// 规则：每个省份至少 1 个 Burg（省中心），沿海省份有港口，高发展度省份有更多 Burg

}
