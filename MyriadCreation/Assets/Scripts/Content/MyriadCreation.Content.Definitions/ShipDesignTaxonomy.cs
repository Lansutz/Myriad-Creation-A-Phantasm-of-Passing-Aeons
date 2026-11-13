namespace MyriadCreation.Content.Definitions
{
    /// <summary>船体结构：只描述物理形态，不描述战术。</summary>
    public enum ShipHullForm
    {
        Other = 0,
        Raft = 1,
        Dugout = 2,
        Long = 3,      // 窄长、浅吃水；维京长船是典型，不是战船类型
        Broad = 4,
        Composite = 5,
    }

    /// <summary>动力：桨、帆、桨帆、轮桨、蒸汽等。</summary>
    public enum ShipPropulsion
    {
        Other = 0,
        Oar = 1,
        SquareSail = 2,
        Lateen = 3,
        OarSail = 4,
        Paddlewheel = 5,
        SteamScrew = 6,
    }

    /// <summary>撞角/冲撞机理：尖锐=穿透，攻城锤=倾覆，鳍式=制造长裂缝。</summary>
    public enum RamType
    {
        None = 0,
        Sharp = 1,
        Hammer = 2,
        Fin = 3,
    }

    /// <summary>火力布置：炮门、单层炮甲板、多层炮甲板等。</summary>
    public enum BatteryLayout
    {
        None = 0,
        GunPort = 1,
        BroadsideSingleDeck = 2,
        BroadsideMultiDeck = 3,
    }
}
