using NUnit.Framework;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 本地化系统 EditMode 测试（CK3 localization 模式：键→文本，缺键回退）
    /// 真实加载 Assets/StreamingAssets/Base/Localization/zh-Hans.json
    /// </summary>
    public class LocalizationTests
    {
        [SetUp]
        public void Setup()
        {
            Localization.Reset();
            Localization.Initialize("zh-Hans");
        }

        [Test]
        public void Localization_LoadsChineseTable()
        {
            Assert.IsTrue(Localization.IsLoaded, "本地化表应加载");
            Assert.AreEqual("zh-Hans", Localization.CurrentLanguage);
        }

        [Test]
        public void Localization_Get_ReturnsText()
        {
            Assert.AreEqual("坚忍", Localization.Get("ethos_endurance_name"));
            Assert.AreEqual("农耕礼俗", Localization.Get("trad_agrarian_rites_name"));
            Assert.AreEqual("莱希斯语", Localization.Get("laethis_lang_name"));
            Assert.AreEqual("莱希斯族群", Localization.Get("ethnos_laethis_name"));
        }

        [Test]
        public void Localization_Get_MissingKey_FallsBackToKey()
        {
            Assert.AreEqual("nonexistent_key", Localization.Get("nonexistent_key"), "缺键应回退键名（开发期可见）");
        }

        [Test]
        public void Localization_Get_WithFallback()
        {
            Assert.AreEqual("缺键回退", Localization.Get("nonexistent_key", "缺键回退"));
        }

        [Test]
        public void Localization_Has_KeyExists()
        {
            Assert.IsTrue(Localization.Has("ethos_endurance_name"));
            Assert.IsFalse(Localization.Has("ethos_endurance_nonexistent"));
        }

        [Test]
        public void Localization_DefinitionTables_UseKeys()
        {
            // 定义文件只存键：Ethos 定义内不应有中文文本字段（键化设计验证）
            Assert.IsTrue(ContentRegistry.TryGetEthos("ethos_endurance", out var ethos));
            Assert.AreEqual("坚忍", ethos.GetName(), "显示名应经本地化解析");
            Assert.IsFalse(string.IsNullOrEmpty(ethos.GetDescription()));
        }
    }
}
