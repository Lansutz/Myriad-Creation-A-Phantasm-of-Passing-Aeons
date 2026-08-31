using System.Collections.Generic;
using NUnit.Framework;
using CivilizationEvolution.Core;
using CivilizationEvolution.Politics;
using CivilizationEvolution.Role;
using CivilizationEvolution.UI;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 社会政治面板测试（阶层画像/派系/政体变迁文本生成）
    /// </summary>
    public class SocietyPanelTests
    {
        [Test]
        public void SocietyText_ContainsClassSection()
        {
            var realm = new RealmData { realmId = 1, realmName = "测试国" };
            var society = new RealmSociety();
            society.classes[GameEnums.SocialClass.Royalty] = new ClassProfile
            {
                populationShare = 0.02f, satisfaction = 80f, unrest = 10f, support = 70f, influence = 42f
            };
            society.classes[GameEnums.SocialClass.Peasant] = new ClassProfile
            {
                populationShare = 0.6f, satisfaction = 40f, unrest = 35f, support = 25f, influence = 15f
            };

            string text = SocietyPanelText.Build(realm, society, null, null, 1000);

            Assert.IsTrue(text.Contains("测试国"), "含政权名");
            Assert.IsTrue(text.Contains("王室"), "含王室阶层");
            Assert.IsTrue(text.Contains("农民"), "含农民阶层");
            Assert.IsTrue(text.Contains("满足"), "含需求满足度");
            Assert.IsTrue(text.Contains("不满"), "含不满");
        }

        [Test]
        public void SocietyText_ContainsFactionSection()
        {
            var realm = new RealmData { realmId = 1, realmName = "测试国" };
            var realmDict = new Dictionary<int, RealmData> { [1] = realm };
            var factions = new FactionManager();
            var society = new RealmSociety();
            society.classes[GameEnums.SocialClass.MerchantFreeman] = new ClassProfile
            {
                populationShare = 0.1f, satisfaction = 50f, unrest = 20f, support = 30f, influence = 20f
            };

            // 手动注入一个改革派系
            var faction = new Faction
            {
                factionId = 1, realmId = 1, stance = FactionStance.Reformist,
                power = 25f, cohesion = 60f, leaderCharacterId = -1
            };
            factions.UpdateRealmFactions(society, realm, new List<CharacterData>());

            string text = SocietyPanelText.Build(realm, society, factions, null, 1000);
            Assert.IsTrue(text.Contains("派系"), "含派系区");
        }

        [Test]
        public void SocietyText_ContainsRegimeSection()
        {
            var realm = new RealmData { realmId = 1, realmName = "测试国" };
            var regime = new RegimeChangeDynamics(null, null);
            // 触发状态创建（NotifyEvent 内部 Ensure）
            regime.NotifyEvent(1000, 1, CriticalJunctureType.SuccessionCrisis, 100f);

            var text1 = SocietyPanelText.Build(realm, null, null, regime, 1000);
            Assert.IsTrue(text1.Contains("政体变迁"), "含政体变迁区");

            // 开窗状态（NotifyEvent 已开窗——强烈度事件）
            var state = regime.GetState(1);
            Assert.IsNotNull(state, "状态应创建");
            state.activeJuncture = new ActiveJuncture
            {
                type = CriticalJunctureType.SuccessionCrisis,
                startDay = 1000,
                remainingDays = 120,
                severity = 60f,
                resolved = false
            };
            var text2 = SocietyPanelText.Build(realm, null, null, regime, 1000);
            Assert.IsTrue(text2.Contains("关键节点"), "含关键节点窗口");
            Assert.IsTrue(text2.Contains("继承危机"), "含节点类型");
            Assert.IsTrue(text2.Contains("120"), "含剩余天数");
        }
    }
}
