namespace MyriadCreation.World.Settlement
{
    /// <summary>
    /// 据点（FortifiedOutpost）的用途/主营方向。
    /// 据点不是营地（临时）也不是定居点（长期农业/城市）；它可以长期存在、住人、驻军、采矿/采石。
    /// </summary>
    public enum OutpostKind
    {
        Generic = 0,
        Military = 1,
        Mining = 2,
        Quarrying = 3,
        Logging = 4,
        Fishing = 5,
        Hunting = 6,
        Gathering = 7,
        Trade = 8,
        Waystation = 9,
    }
}
