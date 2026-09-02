using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CivilizationEvolution.Role;
using CivilizationEvolution.AI;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// AI 人格测试（角色 AI 原型——Preset 预设/性格驱动涌现命名/固定原型开关）
    /// </summary>
    public class AIPersonalityTests
    {
        [Test]
        public void Preset_Archetypes()
        {
            // 命名原型预设（模组/剧本快捷配置——底层仍是性格参数）
            var conqueror = AIPersonality.Preset("conqueror");
            Assert.AreEqual("征服王", conqueror.personalityName, "征服王原型");
            Assert.Greater(conqueror.expansionBias, 0.8f, "征服王高扩张");
            Assert.Greater(conqueror.aggression, 0.7f, "征服王高侵略");

            var adventurer = AIPersonality.Preset("adventurer");
            Assert.AreEqual("冒险王", adventurer.personalityName, "冒险王原型");
            Assert.Greater(adventurer.riskTolerance, 0.9f, "冒险王高风险承受");

            var machiavellian = AIPersonality.Preset("machiavellian");
            Assert.AreEqual("马基雅维利", machiavellian.personalityName, "权谋型原型");
            Assert.Greater(machiavellian.diplomaticBias, 0.8f, "马基雅维利高外交手腕");
            Assert.Less(machiavellian.riskTolerance, 0.6f, "马基雅维利谨慎（低风险承受）");

            var builder = AIPersonality.Preset("builder");
            Assert.Greater(builder.economicBias, 0.9f, "建设者高经济");
            Assert.Greater(builder.researchMultiplier, 1.3f, "建设者高研究");

            // 未知原型→随机（非空即可）
            var unknown = AIPersonality.Preset("nonexistent");
            Assert.IsNotNull(unknown.personalityName, "未知回退随机");
        }

        [Test]
        public void SyncPersonality_EmergentNaming()
        {
            // 性格驱动→涌现原型名（高贪婪+高报复+高大胆→征服王——非标签驱动）
            var cm = new CharacterManager();
            var conqueror = cm.CreateCharacter("暴", "君", 50, true, 0, 0, 0, CharacterRole.Ruler);
            conqueror.greed = 80f;
            conqueror.vengefulness = 80f;
            conqueror.boldness = 70f;
            conqueror.compassion = -60f;

            var controller = new AIController(1, AIPersonality.RandomPersonality());
            controller.SyncPersonality(conqueror);
            Assert.Greater(controller.personality.expansionBias, 0.5f, "贪婪→扩张偏好高");
            Assert.Greater(controller.personality.aggression, 0.5f, "报复+大胆→侵略高");
            Assert.IsNotEmpty(controller.personality.personalityName, "涌现命名非空");

            // 建设者性格（低大胆+低报复+高理性→命名偏向建设/经济）
            var builder = cm.CreateCharacter("仁", "君", 50, true, 0, 0, 0, CharacterRole.Ruler);
            builder.boldness = -60f;
            builder.vengefulness = -50f;
            builder.compassion = 70f;
            builder.rationality = 70f;
            builder.greed = -30f;
            builder.honor = 60f;
            var controller2 = new AIController(2, AIPersonality.RandomPersonality());
            controller2.SyncPersonality(builder);
            Assert.Less(controller2.personality.aggression, 0.5f, "仁君侵略低");
            // 理性高→研究乘数高（经济偏好由贪婪驱动——builder 低贪婪→经济中低合理）
            Assert.Greater(controller2.personality.researchMultiplier, 1.1f, "理性高→研究偏好");
            Assert.Less(controller2.personality.aggression, controller.personality.aggression, "仁君比暴君侵略低");
        }

        [Test]
        public void FixedArchetype_NotOverridden()
        {
            // 固定原型（模组想完全固定）——SyncPersonality 跳过性格覆盖
            var cm = new CharacterManager();
            var ruler = cm.CreateCharacter("主", "君", 50, true, 0, 0, 0, CharacterRole.Ruler);
            ruler.boldness = -80f; // 性格极谨慎——但固定原型保持高扩张

            var controller = new AIController(1, AIPersonality.Preset("conqueror", fixedArchetype: true));
            controller.SyncPersonality(ruler);
            Assert.Greater(controller.personality.expansionBias, 0.8f, "固定征服王不被谨慎性格覆盖");
            Assert.AreEqual("征服王", controller.personality.personalityName, "固定原型名保持");

            // 非固定预设——被性格覆盖（预设=起始参数+性格漂移）
            var controller2 = new AIController(2, AIPersonality.Preset("conqueror", fixedArchetype: false));
            controller2.SyncPersonality(ruler);
            Assert.Less(controller2.personality.expansionBias, 0.9f, "非固定预设随性格漂移（谨慎性格压低扩张）");
        }
    }
}
