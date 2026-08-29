namespace CivilizationEvolution.Core
{
    /// <summary>
    /// 游戏全局常量
    /// </summary>
    public static class GameConstants
    {
        /// <summary>游戏英文名（正式标题）</summary>
        public const string GameNameEn = "Myriad Creation: A Phantasm of Passing Aeons";

        /// <summary>游戏中文名（副标题）</summary>
        public const string GameNameZh = "纷繁的世界：一场流逝的幻景";

        /// <summary>短标题（用于UI空间受限处）</summary>
        public const string GameNameShort = "Myriad Creation";

        /// <summary>游戏版本号</summary>
        public const string Version = "0.1.0-alpha";

        /// <summary>存档版本号（存档结构变更时递增，用于存档兼容）</summary>
        public const int SaveVersion = 4;

        /// <summary>完整显示标题（英文主标题 + 中文副标题）</summary>
        public static string FullTitle => $"{GameNameEn}\n{GameNameZh}";
    }
}
