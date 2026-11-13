using System.IO;

namespace MyriadCreation.Core
{
    /// <summary>
    /// StreamingAssets 内容根目录常量与最小路径帮助。
    /// 只表达目录名（Base/Mods），不改变现有加载顺序或 Resources 路径。
    /// </summary>
    public static class ContentPaths
    {
        /// <summary>StreamingAssets 下的基础内容目录名。</summary>
        public const string Base = "Base";

        /// <summary>StreamingAssets 下的 Mod 覆盖目录名。</summary>
        public const string Mods = "Mods";

        /// <summary>拼接 StreamingAssets 根下的 Base 目录。</summary>
        public static string BaseDir(string streamingAssetsRoot)
            => Path.Combine(streamingAssetsRoot, Base);

        /// <summary>拼接 StreamingAssets 根下的 Mods 目录。</summary>
        public static string ModsDir(string streamingAssetsRoot)
            => Path.Combine(streamingAssetsRoot, Mods);

        /// <summary>拼接 StreamingAssets/Base 下的相对路径。</summary>
        public static string BasePath(string streamingAssetsRoot, string relativePath)
            => Path.Combine(streamingAssetsRoot, Base, relativePath);

        /// <summary>拼接 StreamingAssets/Mods 下的相对路径。</summary>
        public static string ModsPath(string streamingAssetsRoot, string relativePath)
            => Path.Combine(streamingAssetsRoot, Mods, relativePath);
    }
}
