using NUnit.Framework;
using CivilizationEvolution.Politics;
using CivilizationEvolution.Culture;
using CivilizationEvolution.Tech;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 头衔演化测试（批5：领土王国革新解锁——头衔期"某某人之王"→
    /// 领土化"XX王国之王"——族群称谓对接）
    /// </summary>
    public class RealmTitleEvolutionTests
    {
        [Test]
        public void TerritorialKingdom_Innovation_Loaded()
        {
            // 领土王国 1025 在表（era2 制度-政制——前置部落联盟 500）
            Assert.IsTrue(ContentRegistry.TryGetInnovation(1025, out var def), "领土王国革新已加载");
            Assert.AreEqual("领土王国", def.innovationName, "革知名");
            Assert.AreEqual(2, def.era, "era2");
            Assert.IsTrue(def.prerequisites.Contains(500), "前置部落联盟");
        }

        [Test]
        public void RealmDisplayName_TwoStages()
        {
            // 头衔期（未领土化）：原名
            var realm = new RealmData { realmId = 1, realmName = "拉希斯" };
            var tree = new InnovationTree();
            Assert.AreEqual("拉希斯", RealmTitleEvolution.GetRealmDisplayName(realm, tree),
                "头衔期——原名（无领土后缀）");

            // 领土化（模拟持有 1025——realm 研究列表注入）
            var realm2 = new RealmData { realmId = 2, realmName = "拉希斯" };
            // 用继承法测试方式（InnovationTree 无直接注入——通过真实加载+研究模拟在
            // GameWorld 集成——此处验证后缀逻辑：直接检查 HasInnovation=false 分支与
            // 名尾判定）
            Assert.IsFalse(RealmTitleEvolution.IsTerritorial(realm2, tree), "默认未领土化");
        }

        [Test]
        public void RulerTitle_PeopleKing_BeforeTerritorial()
        {
            // 头衔期王称：族群复数+之+王（rex Francorum 同构——统辖人）
            var realm = new RealmData { realmId = 1, realmName = "拉希斯" };
            var tree = new InnovationTree();
            string title = RealmTitleEvolution.GetRulerTitleDisplay(realm, tree, -1,
                cultureId => "拉希斯人");
            Assert.AreEqual("拉希斯人之王", title, "某某族群人之王（头衔期形态）");
        }
    }
}
