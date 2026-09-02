using UnityEngine;
using TMPro;
using UnityEngine.TextCore.LowLevel;

namespace CivilizationEvolution.UI
{
    /// <summary>
    /// TMP 字体工具：中文字体 SDF 资产（动态生成——simhei.ttf → TMP_FontAsset
    /// SDFAA 4096 图集，Dynamic 按需渲染字形——替换 Legacy 字体提升 UI 清晰度）
    /// </summary>
    public static class TMPFontUtility
    {
        private static TMP_FontAsset _chineseFont;

        /// <summary>获取中文字体 SDF 资产（懒生成+缓存；失败返回 null——调用方回退）</summary>
        public static TMP_FontAsset GetChineseFont()
        {
            if (_chineseFont != null) return _chineseFont;

            // TMP Settings 缺失时 CreateFontAsset 会 NRE（TMP_Settings.instance null）——防御
            if (TMP_Settings.instance == null)
            {
                Debug.LogWarning("[TMPFontUtility] TMP Settings 缺失（需运行菜单/构建生成 Resources/TMP Settings.asset）");
                return null;
            }

            var legacy = Resources.Load<Font>("Fonts/simhei");
            if (legacy == null)
            {
                Debug.LogWarning("[TMPFontUtility] simhei.ttf 缺失（Resources/Fonts/）——TMP 中文显示异常");
                return null;
            }

            // 动态 SDF：4096 图集（中文全量字形空间），按需渲染
            _chineseFont = TMP_FontAsset.CreateFontAsset(legacy, 90, 9,
                GlyphRenderMode.SDFAA, 4096, 4096, AtlasPopulationMode.Dynamic);
            _chineseFont.name = "simhei-SDF";
            return _chineseFont;
        }

        /// <summary>应用中文字体到场景全部 TMP 文本</summary>
        public static int ApplyChineseFontToAll()
        {
            var font = GetChineseFont();
            if (font == null) return 0;

            int count = 0;
            foreach (var tmp in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (tmp.font == null || tmp.font.name != "simhei-SDF")
                {
                    tmp.font = font;
                    count++;
                }
            }
            return count;
        }
    }
}
