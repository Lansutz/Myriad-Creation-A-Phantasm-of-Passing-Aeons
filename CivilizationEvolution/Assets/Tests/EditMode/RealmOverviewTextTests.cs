using System.Collections.Generic;
using NUnit.Framework;
using CivilizationEvolution.Politics;
using CivilizationEvolution.Culture;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 政权总览面板测试（聚合已有系统——人口用 RealmSociety.totalPopulation——
    /// 不重复计算——设计定稿：全局数值不上顶栏——政权级数据集中）
    /// </summary>
    public class RealmOverviewTextTests
    {
        [Test]
        public void Overview_Build_AggregatesExistingSystems()
        {
            // 复用已有系统构造（RealmSociety.totalPopulation 现成人口）
            var realm = new RealmData { realmId = 1, realmName = "测试国" };
            realm.treasury = 5000f;
            realm.stability = 70f;
            realm.centralization = 0.6f;
            realm.stateReligionId = 104; // 罗马公教会
            realm.composition = new GovernmentComposition();
            realm.composition.supremeSovereignty = GovernmentConstraints.SupremeSovereignty.Monarchy;

            var society = new RealmSociety { realmId = 1, totalPopulation = 12000f }; // count——×50=60万人

            var text = RealmOverviewText.Build(realm, society, null,
                ReligionCatalog.Get(104), "某圣", isPlayerRealm: true);

            Assert.IsTrue(text.Contains("测试国（本家）"), "政权名+本家标记");
            Assert.IsTrue(text.Contains("600,000"), "人口=totalPopulation×50（复用已有系统——非重算）");
            Assert.IsTrue(text.Contains("国库：5000"), "国库聚合");
            Assert.IsTrue(text.Contains("君主制"), "政体摘要");
            Assert.IsTrue(text.Contains("罗马公教会"), "国教聚合");
            Assert.IsTrue(text.Contains("主保圣人：某圣"), "主保聚合");

            // 官职区（officeDisplay 聚合）
            var offices = new Dictionary<int, string> { { 0, "1. 总督：张三" } };
            var text2 = RealmOverviewText.Build(realm, society, offices);
            Assert.IsTrue(text2.Contains("── 官职体系 ──"), "官职区");
            Assert.IsTrue(text2.Contains("总督：张三"), "官职内容");

            // 空政权（未选中——友好提示）
            var empty = RealmOverviewText.Build(null, null);
            Assert.IsTrue(empty.Contains("未选中政权"), "空政权提示");
        }
    }
}
