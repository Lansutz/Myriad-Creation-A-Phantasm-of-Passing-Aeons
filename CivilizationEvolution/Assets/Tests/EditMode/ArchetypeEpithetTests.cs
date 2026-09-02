using NUnit.Framework;
using UnityEngine;
using CivilizationEvolution.Role;
using CivilizationEvolution.Culture;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 原型学术画像 + 称号系统测试
    /// 原型=性格学术画像（任何人适用——非身份预设——非称号）
    /// 称号/谥号=行为后验（EpithetSystem——与原型分离）
    /// </summary>
    public class ArchetypeEpithetTests
    {
        [Test]
        public void Archetype_Describe_CoversAnyPerson()
        {
            var cm = new CharacterManager();

            // 平民性格（非君王——描述不预设身份）
            var peasant = cm.CreateCharacter("张", "三", 30, true, 0, 0, 0, CharacterRole.Commoner);
            peasant.boldness = 70f; peasant.compassion = 60f; peasant.honor = 50f;
            var desc = PersonalityArchetype.Describe(peasant);
            Assert.IsNotEmpty(desc, "任意性格组合都有画像");
            Assert.IsFalse(desc.Contains("之君") && desc.Contains("之主"), "描述不预设君王身份");
            Assert.IsTrue(desc.Contains("进取") || desc.Contains("仁厚") || desc.Contains("宽厚"), "学术词汇呈现");

            // 将军性格（刚烈）
            var general = cm.CreateCharacter("李", "四", 35, true, 0, 0, 0, CharacterRole.Commoner);
            general.boldness = 80f; general.vengefulness = 60f; general.compassion = -50f;
            var gDesc = PersonalityArchetype.Describe(general);
            Assert.IsNotEmpty(gDesc, "刚烈性格有画像");

            // 学者性格（理性）
            var scholar = cm.CreateCharacter("王", "五", 40, true, 0, 0, 0, CharacterRole.Commoner);
            scholar.rationality = 85f; scholar.boldness = -30f;
            var sDesc = PersonalityArchetype.Describe(scholar);
            Assert.IsNotEmpty(sDesc, "理性性格有画像");

            // 中性性格（各维 0 附近——中庸也有画像）
            var neutral = cm.CreateCharacter("赵", "六", 30, true, 0, 0, 0, CharacterRole.Commoner);
            var nDesc = PersonalityArchetype.Describe(neutral);
            Assert.IsNotEmpty(nDesc, "中庸性格也有画像（全覆盖）");
        }

        [Test]
        public void Archetype_TypeName_Classifies()
        {
            var cm = new CharacterManager();

            var devout = cm.CreateCharacter("僧", "人", 40, true, 0, 0, 0, CharacterRole.Commoner);
            devout.piety = 90f;
            Assert.IsTrue(PersonalityArchetype.TypeName(devout).Contains("虔信"), "虔信→虔信型");

            var conqueror = cm.CreateCharacter("将", "军", 40, true, 0, 0, 0, CharacterRole.Commoner);
            conqueror.boldness = 90f; conqueror.greed = 70f; conqueror.vengefulness = 70f;
            Assert.IsTrue(PersonalityArchetype.TypeName(conqueror).Contains("支配") || 
                          PersonalityArchetype.TypeName(conqueror).Contains("雄略"), "高支配→雄略支配型");

            var kind = cm.CreateCharacter("善", "人", 40, true, 0, 0, 0, CharacterRole.Commoner);
            kind.boldness = 60f; kind.compassion = 80f; kind.honor = 70f;
            Assert.IsTrue(PersonalityArchetype.TypeName(kind).Contains("仁厚"), "进取+仁厚→仁厚开拓型");
        }

        [Test]
        public void Epithet_GrantAndPosthumous()
        {
            var cm = new CharacterManager();
            var c = cm.CreateCharacter("威", "廉", 50, true, 0, 0, 0, CharacterRole.Ruler);

            // 绰号授予（行为后验——一次授予不覆盖）
            Assert.IsTrue(EpithetSystem.GrantEpithet(c, "征服者"), "授予绰号");
            Assert.IsFalse(EpithetSystem.GrantEpithet(c, "大冒险家"), "已有绰号不覆盖");
            Assert.AreEqual("征服者", c.epithet, "绰号保持");

            // 谥号（华夏式——按一生行为定谥）
            // 暴君：报复高+悲悯低 → 暴
            var tyrant = cm.CreateCharacter("桀", "纣", 50, true, 0, 0, 0, CharacterRole.Ruler);
            tyrant.boldness = 0f; tyrant.greed = 0f; tyrant.honor = 0f;
            tyrant.rationality = 0f; tyrant.piety = 0f;
            tyrant.vengefulness = 80f; tyrant.compassion = -80f;
            Assert.AreEqual("暴", EpithetSystem.DeterminePosthumousTitle(tyrant, 0, 0, false), "暴君谥=暴");

            // 武功：征服 3+ → 武（显式全维中性——CreateCharacter 隐性随机性格会干扰谥号分支）
            var conqueror = cm.CreateCharacter("武", "王", 50, true, 0, 0, 0, CharacterRole.Ruler);
            conqueror.boldness = 50f; conqueror.greed = 0f; conqueror.honor = 0f;
            conqueror.rationality = 0f; conqueror.vengefulness = 0f; conqueror.piety = 0f;
            conqueror.compassion = 30f;
            Assert.AreEqual("武", EpithetSystem.DeterminePosthumousTitle(conqueror, 5, 3, false), "征服者谥=武");

            // 仁德：悲悯高 → 仁
            var kind = cm.CreateCharacter("仁", "君", 50, true, 0, 0, 0, CharacterRole.Ruler);
            kind.boldness = 0f; kind.greed = 0f; kind.honor = 0f;
            kind.rationality = 0f; kind.vengefulness = 0f; kind.piety = 0f;
            kind.compassion = 70f;
            Assert.AreEqual("仁", EpithetSystem.DeterminePosthumousTitle(kind, 0, 0, false), "仁君谥=仁");

            // 荒政（饥荒）→ 荒
            Assert.AreEqual("荒", EpithetSystem.DeterminePosthumousTitle(kind, 0, 0, true), "饥荒谥=荒");
        }
    }
}
