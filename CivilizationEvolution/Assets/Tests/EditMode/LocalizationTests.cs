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
            // 注册表自包含初始化（DefinitionTables_UseKeys 依赖；消除跨测试类静态顺序耦合）
            ContentRegistry.Reset();
            ContentRegistry.Initialize();
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

        // ===== 多语言（正式：en/zh-Hans/zh-Hant；预留：fr/de/es/ja/ko） =====

        [Test]
        public void Localization_English_LoadsAndTranslates()
        {
            Localization.Reset();
            Localization.Initialize("en");
            Assert.AreEqual("Endurance", Localization.Get("ethos_endurance_name"), "英语应返回英文文本");
            Assert.AreEqual("Agrarian Rites", Localization.Get("trad_agrarian_rites_name"));
            Assert.AreEqual("Laethis Ethnos", Localization.Get("ethnos_laethis_name"));
        }

        [Test]
        public void Localization_TraditionalChinese_LoadsAndTranslates()
        {
            Localization.Reset();
            Localization.Initialize("zh-Hant");
            Assert.AreEqual("堅忍", Localization.Get("ethos_endurance_name"), "繁体应返回繁体文本");
            Assert.AreEqual("農耕禮俗", Localization.Get("trad_agrarian_rites_name"));
            Assert.AreEqual("萊希斯族群", Localization.Get("ethnos_laethis_name"));
        }

        [Test]
        public void Localization_ReservedLanguages_FallbackToKey()
        {
            // 预留语言文件存在但为空：查询应回退键名（不崩溃、不误显示其他语言）
            foreach (var lang in new[] { "fr", "de", "es", "ja", "ko" })
            {
                Localization.Reset();
                Localization.Initialize(lang);
                Assert.AreEqual("ethos_endurance_name", Localization.Get("ethos_endurance_name"),
                    $"{lang} 为预留语言，缺键应回退键名");
            }
        }

        [Test]
        public void Localization_SwitchLanguage_ReloadsTable()
        {
            Localization.Reset();
            Localization.Initialize("zh-Hans");
            Assert.AreEqual("坚忍", Localization.Get("ethos_endurance_name"));
            Localization.Reset();
            Localization.Initialize("en");
            Assert.AreEqual("Endurance", Localization.Get("ethos_endurance_name"), "切换语言应重载表");
        }
    }
}
