using NUnit.Framework;
using CivilizationEvolution.Core;
using CivilizationEvolution.Culture;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 地名语义组合引擎测试（批2：词干×语义后缀——规则限制——
    /// 语言真实词形——纪念名）
    /// </summary>
    public class PlaceNameGeneratorTests
    {
        private LanguageDef _lang;

        [SetUp]
        public void Setup()
        {
            _lang = new LanguageDef
            {
                languageId = "test_lang",
                terrainWords = new System.Collections.Generic.List<PlaceSuffixDef>
                {
                    new PlaceSuffixDef { semantic = "cliff", word = "崖" },
                    new PlaceSuffixDef { semantic = "plain", word = "原" },
                    new PlaceSuffixDef { semantic = "river", word = "川" },
                },
                placeSuffixes = new System.Collections.Generic.List<PlaceSuffixDef>
                {
                    new PlaceSuffixDef { semantic = "city", word = "城" },
                    new PlaceSuffixDef { semantic = "fort", word = "堡" },
                    new PlaceSuffixDef { semantic = "port", word = "港" },
                    new PlaceSuffixDef { semantic = "region", word = "之地" },
                }
            };
        }

        [Test]
        public void Generate_ValidCombination()
        {
            // 山崖+城=崖城（合法——cliff 可配 city）
            Assert.AreEqual("崖城", PlaceNameGenerator.Generate("cliff", "city", _lang), "山崖之城（语言合成）");
            // 山崖+堡=崖堡（合法）
            Assert.AreEqual("崖堡", PlaceNameGenerator.Generate("cliff", "fort", _lang), "山崖堡垒");
            // 平原+城=原城（合法）
            Assert.AreEqual("原城", PlaceNameGenerator.Generate("plain", "city", _lang), "平原之城");
        }

        [Test]
        public void Generate_RuleRestriction()
        {
            // 规则限制：山崖不能配港（非水缘）——返回空
            Assert.AreEqual("", PlaceNameGenerator.Generate("cliff", "port", _lang), "山崖不可配港（规则）");
            // 平原可配城但引擎查词——川+港合法（river 可配 port）
            Assert.AreEqual("川港", PlaceNameGenerator.Generate("river", "port", _lang), "水缘配港");
            // 未知地形语义→无词→空
            Assert.AreEqual("", PlaceNameGenerator.Generate("tundra", "city", _lang), "无词干→空");
            // 语言缺后缀词→空
            var poor = new LanguageDef { terrainWords = new System.Collections.Generic.List<PlaceSuffixDef> { new PlaceSuffixDef { semantic = "plain", word = "原" } } };
            Assert.AreEqual("", PlaceNameGenerator.Generate("plain", "city", poor), "缺后缀词→空");
        }

        [Test]
        public void FounderCity_MemorialName()
        {
            // 建城者纪念名（亚历山大式——人名+城后缀）
            var lang = new LanguageDef
            {
                placeSuffixes = new System.Collections.Generic.List<PlaceSuffixDef>
                {
                    new PlaceSuffixDef { semantic = "city", word = "城" }
                }
            };
            Assert.AreEqual("亚历山大城", PlaceNameGenerator.FounderCity("亚历山大", lang), "建城者纪念名");
            Assert.AreEqual("", PlaceNameGenerator.FounderCity("", lang), "空名不生成");
            Assert.AreEqual("", PlaceNameGenerator.FounderCity("张三", null), "无语言不生成");
        }

        [Test]
        public void CanCombine_Rules()
        {
            Assert.IsTrue(PlaceNameGenerator.CanCombine("cliff", "city"), "山崖配城合法");
            Assert.IsFalse(PlaceNameGenerator.CanCombine("cliff", "port"), "山崖配港非法（规则限制）");
            Assert.IsTrue(PlaceNameGenerator.CanCombine("any", "region"), "模糊地区通配");
            Assert.IsFalse(PlaceNameGenerator.CanCombine("unknown", "city"), "未知地形不可配城");
        }
    }
}
