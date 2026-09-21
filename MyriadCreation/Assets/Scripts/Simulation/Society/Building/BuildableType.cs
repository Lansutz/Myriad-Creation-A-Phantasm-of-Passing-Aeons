namespace CivilizationEvolution.Simulation.Society
{
    public enum BuildableType
    {
 // ===== 堡垒亚型（18种）=====
        Barrier,            // 关隘（建在狭窄通道，直接阻挡敌对势力通行）
        PassFort,           // 关口堡垒（建在关隘附近，区域控制，不直接阻挡通行）
        HighlandKeep,       // 高地堡
        ManorFort,          // 坞堡庄园
        PlainGarrison,      // 平原屯堡
        EstuaryFort,        // 河口堡
        StarFort,           // 棱堡
        HillFort,           // 垒寨
        BorderFort,         // 边堡
        CoastalFort,        // 海岸堡垒
        RiverFort,          // 河防堡垒
        MountainFortress,   // 山地要塞
        IslandFort,         // 岛屿要塞
        SiegeCastle,        // 攻城城堡
        RoyalCastle,        // 王城/宫堡
        AbbeyFort,          // 修道院堡垒
        TradingFort,        // 商站堡垒

 // ===== 特殊城形态（需要特定条件）=====
        MountainCity,       // 山城
        WaterCity,          // 水城
        StarCity,           // 星城（棱堡时代）
        CitadelCity,        // 堡城
        PlannedCity,        // 规划城

 // ===== 港口设施 =====
        DeepWaterPort,      // 深水港
        RiverPort,          // 内河港
        ImperialPort,       // 帝国港

 // ===== 特殊设施 =====
        Ferry,              // 渡口
        PostStation,        // 驿站
        Watchtower,         // 烽火台/瞭望塔
        Granary,            // 粮仓
        Market,             // 市集
        Temple,             // 神庙
        University,         // 大学/学府
        Workshop,           // 作坊/工场
        Mine,               // 矿场
        SaltWorks,          // 盐场
        Fishery,            // 渔场
        Shipyard,           // 造船厂
        Barracks,           // 兵营
        Arsenal,            // 军械库
        Aqueduct,           // 引水渠/水道
        Sewer,              // 下水道
        Bathhouse,          // 公共浴场
        Library,            // 图书馆
        Observatory,        // 观星台/天文台
        Lighthouse          // 灯塔
    }
}
