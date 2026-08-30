using NUnit.Framework;
using UnityEngine;
using CivilizationEvolution.UI;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 资源完整性测试（用户定稿：检查项目缺不缺文件——UI/字体/Shader/GFX）
    /// 中文字体是 UI 显示的关键缺口（内置 LegacyRuntime 字体不含中文）
    /// </summary>
    public class AssetIntegrityTests
    {
        [Test]
        public void ChineseFont_Present()
        {
            // 中文字体必须在 Resources/Fonts/（UIManager.ApplyChineseFont 依赖）
            var font = Resources.Load<Font>("Fonts/simhei");
            Assert.IsNotNull(font, "中文字体缺失：Assets/Resources/Fonts/simhei.ttf");
        }

        [Test]
        public void WorldConfig_Asset_Present()
        {
            // 配置资产（场景 Bootstrap 的 config 引用）——ScriptableObjects/DefaultWorldConfig.asset
            string path = System.IO.Path.Combine(Application.dataPath, "ScriptableObjects", "DefaultWorldConfig.asset");
            Assert.IsTrue(System.IO.File.Exists(path), $"DefaultWorldConfig.asset 应存在：{path}");
        }
    }
}
