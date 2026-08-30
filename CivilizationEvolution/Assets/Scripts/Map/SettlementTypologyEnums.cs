using System;

namespace CivilizationEvolution.Map
{
    // ============================================================
    // 聚落类型学核心枚举
    // 综合：用户设计文档（村镇/城/堡形态系统）+ 文明引擎IN模块
    // （港口层级/关隘瓶颈/要塞体系/城镇功能类型学）
    // ============================================================

    /// <summary>
    /// 聚落等级（Ⅰ-Ⅴ级）
    /// 对应：村落→集镇→城邑→都会→大都会
    /// </summary>
    public enum SettlementLevel
    {
        LevelI = 0,    // Ⅰ级 聚落（村落）
        LevelII = 1,   // Ⅱ级 集镇
        LevelIII = 2,  // Ⅲ级 城邑
        LevelIV = 3,   // Ⅳ级 都会
        LevelV = 4     // Ⅴ级 大都会
    }

    /// <summary>
    /// 聚落功能类型（文明引擎IN5城镇功能类型学）
    /// 一个聚落可同时拥有多种功能，主功能决定发展倾向
    /// </summary>
    [Flags]
    public enum SettlementFunction
    {
        None = 0,
        Administrative = 1 << 0,   // IN5a 行政城镇：政权所在地、官僚驻地
        Military = 1 << 1,         // IN5b 军事城镇：边防、驻军、通道控制
        Commercial = 1 << 2,       // IN5c 商业市镇：市集、手工业、贸易
        Religious = 1 << 3,        // IN5d 宗教圣地城镇：朝圣、祭祀、宗教教育
        Mining = 1 << 4,           // IN5e 矿业城镇：矿产开采、冶炼
        Agricultural = 1 << 5,     // IN5f 农业市镇：周边农业区的集散中心
        Crossing = 1 << 6,         // IN5g 渡口城镇：河流/海峡渡口控制
        Tariff = 1 << 7,           // IN5h 关税城镇：边境/关隘税收
        Port = 1 << 8,             // 港口功能（叠加IN1）
        Fortress = 1 << 9,         // 要塞功能（叠加IN4）
        PostStation = 1 << 10,     // 驿站功能（叠加IN3）
        Cultural = 1 << 11,        // 文教城镇：大学、图书馆、文化中心
        Industrial = 1 << 12       // 工业城镇：作坊、工场、制造业集中
    }

    /// <summary>
    /// 城市重心（都会级别Ⅳ-Ⅴ级的核心发展方向）
    /// 决定城市建筑布局、经济结构、军事价值
    /// </summary>
    public enum CityFocus
    {
        Balanced,           // 综合型（各功能均衡发展，通常为首都）
        Defense,            // 防御重心（军事要塞型城市，城防等级极高）
        Hub,                // 节点枢纽重心（交通要冲，水陆交汇）
        Trade,              // 贸易重心（商业港口/市集，财富集中）
        Administrative,     // 行政重心（官僚机构集中，治理辐射广）
        Religious,          // 宗教重心（圣地/教廷，朝圣经济）
        Cultural,           // 文教重心（大学/学术中心，人才产出）
        Industrial,         // 工业重心（手工业/制造业集中）
        Mining              // 矿业重心（矿产资源型城市）
    }

    /// <summary>
    /// 城的形态（平面布局形态）
    /// 由地形、建城时代、文化传统共同决定
    /// </summary>
    public enum CityForm
    {
        Circular,           // 圆城：平原河流交汇，防御最优，自然生长
        Square,             // 方城：网格规划，行政中心，棋盘式街道
        Irregular,          // 不规则城：地形约束（山地/半岛/河曲），有机生长
        TwinCity,           // 双城：卫城+下城（山顶要塞+山麓居民区）
        MountainCity,       // 山城：山顶/山脊建城，居高临下
        WaterCity,          // 水城：水网密集（威尼斯/苏州式），运河为街
        StarCity,           // 星城：棱堡时代，星形防御工事环绕
        WalledTown,         // 围镇：单一城墙环绕的集镇（Ⅲ级以下常见）
        CitadelCity,        // 堡城：核心要塞+外围城区，军事主导
        PlannedCity         // 规划城：完全人工规划的新城（迁都/殖民）
    }

    /// <summary>
    /// 堡垒亚型（堡的具体形态，用户设计文档4种区域堡寨+关口堡）
    /// </summary>
    public enum FortSubtype
    {
        // ===== 关口堡（Pass）：通道控制 =====
        PassFort,           // 关口堡：峡谷/隘口/狭河谷通道控制

        // ===== 区域堡寨（Regional）4种亚型 =====
        HighlandKeep,       // 高地堡：丘陵高地，区域压制网，堡垒链
        ManorFort,          // 坞堡庄园：内地农耕区，自给自足，基层治理节点
        PlainGarrison,      // 平原屯堡：开阔平原边境，沿边境线防御带
        EstuaryFort,        // 河口堡：大河河口/干流要道，管控内河与近海航线

        // ===== 其他堡垒形态 =====
        StarFort,           // 棱堡：火炮时代，星形多角度防御
        HillFort,           // 垒寨：临时/半永久军事营地，山丘顶部
        BorderFort,         // 边堡：边境线小型据点，预警+驻扎
        CoastalFort,        // 海岸堡垒：保护港口/海岸线，防海上入侵
        RiverFort,          // 河防堡垒：控制河流渡口/航道
        MountainFortress,   // 山地要塞：崇山峻岭中的巨型要塞
        IslandFort,         // 岛屿要塞：海岛/河心岛，控制水域
        SiegeCastle,        // 攻城城堡：前线推进基地，围攻敌方城市
        RoyalCastle,        // 王城/宫堡：君主居所+行政中心+防御
        AbbeyFort,          // 修道院堡垒：宗教武装据点（圣殿骑士团式）
        TradingFort         // 商站堡垒：贸易据点+防御（东印度公司式）
    }

    /// <summary>
    /// 港口层级（文明引擎IN1）
    /// </summary>
    public enum PortTier
    {
        None = 0,
        Anchorage,          // IN1c 避风港/锚地：天然掩护水域，无正式设施
        RiverPort,          // IN1d 内河港：河流沿岸港口，内陆-沿海转运
        IntermediatePort,   // IN1b 中转港：中等规模，区域贸易/渔业/沿海中转
        DeepWaterPort,      // IN1a 深水港：可停泊大型船只，长途贸易枢纽/舰队基地
        ImperialPort        // 帝国港：最高级，全球贸易中心+主力舰队母港
    }

    /// <summary>
    /// 关隘瓶颈类型（文明引擎IN2）
    /// </summary>
    public enum BottleneckType
    {
        None = 0,
        MountainPass,       // IN2a 山口关隘：山脉中可通行通道
        CanyonPass,         // IN2b 峡谷通道：河流/谷地中的狭窄通道
        StraitCrossing,     // IN2c 海峡/渡口：水域跨越点
        DesertCorridor,     // IN2d 沙漠走廊：绿洲/水源点之间的受限通道
        RiverFording,       // 河流浅滩：可涉水渡河点
        IsthmusPass         // 地峡通道：两块大陆之间的狭窄陆桥
    }

    /// <summary>
    /// 聚落升级路线（决定聚落从低级向高级演化的路径依赖）
    /// 不同起点的聚落有不同的升级路径和最终形态
    /// </summary>
    public enum UpgradePath
    {
        NaturalGrowth,      // 自然生长：村落→集镇→城邑（最常见，农业基础）
        PortDevelopment,    // 港口发展：渡口/锚地→港口集镇→港口城市→贸易都会
        MilitaryGrowth,     // 军事发展：隘口村→关口堡→关城→军事都会
        MiningGrowth,       // 矿业发展：矿村→矿镇→矿业城→工业都会
        ReligiousGrowth,    // 宗教发展：圣地村→朝圣镇→宗教城→圣城
        AdministrativeGrowth, // 行政发展：官署村→行政镇→府城→首都
        StrategicGrowth,    // 战略发展：边境屯堡→军镇→战略要塞城
        CommercialGrowth    // 商业发展：市集村→商镇→商业城→商贸都会
    }

    /// <summary>
    /// 聚落演化阶段（用于渐进式演化，不突变）
    /// </summary>
    public enum EvolutionStage
    {
        Stable,             // 稳定：当前形态稳定，无明显演化趋势
        Transitioning,      // 过渡中：演化进度累积，正在向新形态转变
        Emerging,           // 萌芽：新形态特征开始出现，但旧形态仍主导
        Transformed,        // 已转化：形态切换完成，进入新形态稳定期
        Declining           // 衰退中：当前形态正在退化（如堡垒废弃→村镇）
    }

    /// <summary>
    /// 城墙等级（防御工事强度）
    /// </summary>
    public enum WallLevel
    {
        None = 0,           // 无城墙（村落/集镇常见）
        Palisade,           // 木栅栏（临时防御，边境村落）
        EarthenRampart,     // 土堤（早期城邑，夯土城墙）
        StoneWall,          // 石城墙（标准城邑，砖石结构）
        FortifiedWall,      // 加固城墙（都会级，塔楼+瓮城+护城河）
        StarFortification,  // 棱堡防御（火炮时代，星形多角度）
        MegaFortification   // 巨型防御体系（大都会级，多重城墙+外围堡垒链）
    }
}
