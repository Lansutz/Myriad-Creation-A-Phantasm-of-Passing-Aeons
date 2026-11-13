using System.Collections.Generic;
using Stopwatch = System.Diagnostics.Stopwatch;
using UnityEngine;
using TMPro;
using UnityEngine.TextCore.LowLevel;
using MyriadCreation.App.Settings;

namespace MyriadCreation.UI.Common
{
    /// <summary>
    /// TMP 字体工具：按 <see cref="SettingsModel.FontChoice"/> 生成/缓存中文 TMP SDF 字体并应用到 TMP 文本。
    /// 0=系统黑体 simhei（默认）、1=思源黑体、2=思源宋体；字体资源位于
    /// Art/Fonts/Resources（simhei）与 Art/Fonts/Resources/Fonts（Source Han OTF）。
    /// 生成失败回退 simhei；TMP Settings 缺失时安全返回 null。
    /// </summary>
    public static class TMPFontUtility
    {
        private const string DefaultFontResource = "simhei";
        private const string SourceHanSansResource = "Fonts/SourceHanSansSC-Regular";
        private const string SourceHanSerifResource = "Fonts/SourceHanSerifSC-Regular";

        private static readonly Dictionary<int, TMP_FontAsset> FontAssets = new Dictionary<int, TMP_FontAsset>();

        /// <summary>已创建并缓存的字体资产数量（测试/诊断用）。</summary>
        public static int CachedFontAssetCount => FontAssets.Count;

        /// <summary>实际执行 CreateFontAsset 的次数（测试诊断用，重复调用不应增加）。</summary>
        public static int CreatedFontAssetCount { get; private set; }

        /// <summary>TotalFontAssetCreations 别名（W6 诊断字段）。</summary>
        public static int TotalFontAssetCreations => CreatedFontAssetCount;

        /// <summary>最近一次实际 CreateFontAsset 的耗时毫秒（仅诊断；缓存命中不更新）。</summary>
        public static long LastCreateElapsedMs { get; private set; }

        /// <summary>测试用：清空字体缓存与诊断计数/耗时；不影响已应用到场景的 TMP 文本。</summary>
        public static void ResetDiagnosticsForTests()
        {
            FontAssets.Clear();
            CreatedFontAssetCount = 0;
            LastCreateElapsedMs = 0;
        }

        /// <summary>非法 choice 归一到 0（系统黑体）；合法值原样返回。</summary>
        public static int NormalizeChoice(int choice)
        {
            return choice >= 0 && choice <= SettingsModel.MaxFontChoice ? choice : 0;
        }

        /// <summary>choice 对应的 Resources 路径（0→simhei；1/2→Source Han 字体）。</summary>
        public static string GetResourcePath(int choice)
        {
            switch (NormalizeChoice(choice))
            {
                case 1: return SourceHanSansResource;
                case 2: return SourceHanSerifResource;
                default: return DefaultFontResource;
            }
        }

        /// <summary>choice 对应的 TMP 字体资产名（simhei-SDF/SourceHanSans-SDF/SourceHanSerif-SDF）。</summary>
        public static string GetFontAssetName(int choice)
        {
            switch (NormalizeChoice(choice))
            {
                case 1: return "SourceHanSans-SDF";
                case 2: return "SourceHanSerif-SDF";
                default: return "simhei-SDF";
            }
        }

        /// <summary>获取指定选项的 TMP 字体资产（按 choice 缓存；失败回退 simhei）。</summary>
        public static TMP_FontAsset GetFont(int choice)
        {
            int normalized = NormalizeChoice(choice);
            if (FontAssets.TryGetValue(normalized, out TMP_FontAsset cached) && cached != null)
            {
                return cached;
            }

            // TMP Settings 缺失时 CreateFontAsset 会 NRE（TMP_Settings.instance null）——防御
            if (TMP_Settings.instance == null)
            {
                Debug.LogWarning("[TMPFontUtility] TMP Settings 缺失（需运行菜单/构建生成 Resources/TMP Settings.asset）");
                return null;
            }

            var legacy = Resources.Load<Font>(GetResourcePath(normalized));
            if (legacy == null)
            {
                if (normalized != 0)
                {
                    Debug.LogWarning("[TMPFontUtility] 缺少字体资源：" + GetResourcePath(normalized) + "，回退 simhei");
                    return GetFont(0);
                }
                Debug.LogWarning("[TMPFontUtility] simhei.ttf 缺失（Art/Fonts/Resources/）——TMP 中文显示异常");
                return null;
            }

            // 动态 SDF：4096 图集（中文全量字形空间），按需渲染；仅诊断耗时。
            Stopwatch stopwatch = Stopwatch.StartNew();
            TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(legacy, 90, 9,
                GlyphRenderMode.SDFAA, 4096, 4096, AtlasPopulationMode.Dynamic);
            stopwatch.Stop();
            LastCreateElapsedMs = stopwatch.ElapsedMilliseconds;
            if (font == null)
            {
                if (normalized != 0)
                {
                    Debug.LogWarning("[TMPFontUtility] 创建 TMP 字体失败：" + GetResourcePath(normalized) + "，回退 simhei");
                    return GetFont(0);
                }
                return null;
            }

            font.name = GetFontAssetName(normalized);
            FontAssets[normalized] = font;
            CreatedFontAssetCount++;
            return font;
        }

        /// <summary>获取当前设置选择的中文字体（默认 0 = simhei；供 MainMenu/其它 UI 生成文本时使用）。</summary>
        public static TMP_FontAsset GetChineseFont()
        {
            return GetFont(GetChineseFontChoice());
        }

        /// <summary>应用指定字体到场景全部 TMP 文本；返回改动的文本数。失败（含回退失败）返回 0。</summary>
        public static int ApplyFontChoice(int choice)
        {
            TMP_FontAsset font = GetFont(choice);
            if (font == null) return 0;

            int count = 0;
            foreach (var tmp in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include))
            {
                if (tmp != null && tmp.font != font)
                {
                    tmp.font = font;
                    count++;
                }
            }
            return count;
        }

        /// <summary>按当前 SettingsService.Current 字体选择应用到全场景（兼容旧入口）。</summary>
        public static int ApplyChineseFontToAll()
        {
            return ApplyFontChoice(GetChineseFontChoice());
        }

        private static int GetChineseFontChoice()
        {
            return SettingsService.Current != null ? SettingsService.Current.FontChoice : 0;
        }
    }
}
