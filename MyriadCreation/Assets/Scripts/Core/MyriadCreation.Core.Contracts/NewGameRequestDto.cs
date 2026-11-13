namespace MyriadCreation.Core.Contracts
{
    /// <summary>
    /// 新游戏请求（Core 纯契约 DTO）：App.Contracts 的新游戏入口只依赖本 DTO；
    /// App 内部再转换为 World.Generation.WorldSetupOptions，避免契约层反向依赖 World/Simulation。
    /// wrapMode/contentMode 使用对应枚举的 int 值表达（Core 不引用 World/Simulation）。
    /// </summary>
    public sealed class NewGameRequestDto
    {
        /// <summary>地图宽/高 sentinel：表示使用会话默认尺寸；不要传 0。</summary>
        public const int DefaultMapSizeSentinel = -1;

        /// <summary>随机种子 sentinel：表示使用会话默认种子；不要传 0。</summary>
        public const int DefaultSeedSentinel = int.MinValue;

        /// <summary>环绕模式 sentinel：表示使用会话默认环绕模式；不要传 0（0 是有效枚举值）。</summary>
        public const int DefaultWrapModeSentinel = -1;

        /// <summary>初始内容模式 sentinel：表示未指定/使用默认内容模式。</summary>
        public const int DefaultContentModeSentinel = -1;

        /// <summary>地图宽/高；<see cref="DefaultMapSizeSentinel"/> 表示使用会话默认尺寸。</summary>
        public int mapWidth = DefaultMapSizeSentinel;
        public int mapHeight = DefaultMapSizeSentinel;
        /// <summary>随机种子；<see cref="DefaultSeedSentinel"/> 表示使用会话默认种子。</summary>
        public int seed = DefaultSeedSentinel;
        /// <summary>MapWrapMode int 值；<see cref="DefaultWrapModeSentinel"/> 表示使用会话默认环绕模式。</summary>
        public int wrapMode = DefaultWrapModeSentinel;

        /// <summary>初始内容模式 int 值（对应 InitialContentMode；<see cref="DefaultContentModeSentinel"/> 表示未指定/使用默认）。</summary>
        public int contentMode = DefaultContentModeSentinel;

        public bool generatePolities;
        public bool generateSettlements;
        public bool generateInitialUnits;

        public int initialPolityCount;
        public float initialPopulationScale = 1f;
        public int initialSettlementCount;
        public int initialUnitCount;

        public bool enableNaturalSpawning = true;
        public float mapActorMultiplier = 1f;
        public bool spawnSettlements = true;
        public bool spreadPopulation = true;
    }
}
