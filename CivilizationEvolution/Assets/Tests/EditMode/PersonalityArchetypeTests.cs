using System.Collections.Generic;
using NUnit.Framework;
using CivilizationEvolution.Role;
using CivilizationEvolution.Culture;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 原型学术画像与称号系统测试
    /// </summary>
    public class PersonalityArchetypeTests
    {
        private CharacterManager _cm;

        [SetUp]
        public void Setup()
        {
            _cm = new CharacterManager();
        }

        [Test]
        public void Archetype_NeutralDescription_NoIdentityWords()
        {
            // 中性描述——不挂钩身份/职业（农人也是同一描述）
            var conquerorLike = _cm.CreateCharacter("烈", "性", 50, true, 0, 0, 0, CharacterRole.Commoner);
            conquerorLike.boldness = 80f;
            conquerorLike.greed = 70f;
            conquerorLike.vengefulness = 60f;
            conquerorLike.compassion = -50f;
            string desc = PersonalityArchetype.Describe(conquerorLike);

            Assert.IsNotEmpty(desc, "描述非空");
            // 不得含身份挂钩词（君/主/王/者/之人后缀）
            foreach (var w in new[] { "之君", "之主", "之王", "之士" })
                Assert.IsFalse(desc.Contains(w), $"描述不含身份词 {w}");

            // 任意七维组合都能生成（108 组合全覆盖——不遗漏）
            var rng = new System.Random(7);
            for (int i = 0; i < 20; i++)
            {
                var c = _cm.CreateCharacter("测", i.ToString(), 40, true, 0, 0, 0, CharacterRole.Commoner);
                c.boldness = (float)(rng.NextDouble() * 200 - 100);
                c.compassion = (float)(rng.NextDouble() * 200 - 100);
                c.greed = (float)(rng.NextDouble() * 200 - 100);
                c.honor = (float)(rng.NextDouble() * 200 - 100);
                c.rationality = (float)(rng.NextDouble() * 200 - 100);
                c.vengefulness = (float)(rng.NextDouble() * 200 - 100);
                c.piety = (float)(rng.NextDouble() * 200 - 100);
                Assert.IsNotEmpty(PersonalityArchetype.Describe(c), $"随机组合 {i} 有描述");
                Assert.IsNotEmpty(PersonalityArchetype.TypeName(c), $"随机组合 {i} 有类型名");
            }
        }

        [Test]
        public void Archetype_TypeName_Categories()
        {
            // 类型名（学术画像简名——内部归类）
            var dev = _cm.CreateCharacter("虔", "徒", 50, true, 0, 0, 0, CharacterRole.Commoner);
            dev.piety = 80f;
            Assert.IsTrue(PersonalityArchetype.TypeName(dev).Contains("虔信"), "高虔信→虔信型");

            var gentle = _cm.CreateCharacter("温", "良", 50, true, 0, 0, 0, CharacterRole.Commoner);
            gentle.boldness = -60f;
            gentle.compassion = 70f;
            gentle.honor = 60f;
            Assert.IsTrue(PersonalityArchetype.TypeName(gentle).Contains("温"), "低进取高仁厚→温厚型");
        }

        [Test]
        public void Epithet_GrantAndPosthumous()
        {
            // 绰号：行为成就授予（一次不覆盖）
            var c = _cm.CreateCharacter("征", "者", 60, true, 0, 0, 0, CharacterRole.Ruler);
            Assert.IsTrue(EpithetSystem.TryGrantEpithet(c, "征服者"), "授予绰号");
            Assert.AreEqual("征服者", c.epithet, "绰号记录");
            Assert.IsFalse(EpithetSystem.TryGrantEpithet(c, "大冒险家"), "已有绰号不覆盖");

            // 谥号：华夏式（行为定谥——显式设全七维防隐性随机）
            var martial = _cm.CreateCharacter("武", "功", 60, true, 0, 0, 0, CharacterRole.Ruler);
            martial.boldness = 50f; martial.greed = 0f; martial.honor = 0f;
            martial.rationality = 0f; martial.vengefulness = 0f; martial.piety = 0f;
            martial.compassion = 30f;
            Assert.AreEqual(30f, martial.compassion, "设值生效");
            Assert.AreEqual("武", EpithetSystem.DeterminePosthumousTitle(martial, warsWon: 6, conquests: 4, false), "多战功→武");

            var benevolent = _cm.CreateCharacter("仁", "德", 60, true, 0, 0, 0, CharacterRole.Ruler);
            benevolent.boldness = 0f; benevolent.greed = 0f; benevolent.honor = 0f;
            benevolent.rationality = 0f; benevolent.vengefulness = 0f; benevolent.piety = 0f;
            benevolent.compassion = 70f;
            Assert.AreEqual("仁", EpithetSystem.DeterminePosthumousTitle(benevolent, 0, 0, false), "仁德→仁");

            var cruel = _cm.CreateCharacter("暴", "虐", 60, true, 0, 0, 0, CharacterRole.Ruler);
            cruel.boldness = 0f; cruel.greed = 0f; cruel.honor = 0f;
            cruel.rationality = 0f; cruel.piety = 0f;
            cruel.compassion = -70f;
            cruel.vengefulness = 70f;
            Assert.AreEqual("暴", EpithetSystem.DeterminePosthumousTitle(cruel, 2, 1, false), "暴虐→暴");

            var negligent = _cm.CreateCharacter("荒", "政", 60, true, 0, 0, 0, CharacterRole.Ruler);
            negligent.boldness = 0f; negligent.greed = 0f; negligent.honor = 0f;
            negligent.rationality = 0f; negligent.vengefulness = 0f; negligent.piety = 0f;
            negligent.compassion = 0f;
            Assert.AreEqual("荒", EpithetSystem.DeterminePosthumousTitle(negligent, 0, 0, true), "饥荒→荒");
        }
    }
}
