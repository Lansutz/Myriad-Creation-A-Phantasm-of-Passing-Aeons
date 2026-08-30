using System.Collections.Generic;
using NUnit.Framework;
using CivilizationEvolution.Core;
using CivilizationEvolution.Tech;
using CivilizationEvolution.War;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 军事拓展测试（用户定稿：后勤/通讯/组织革新 + 兵种×革新挂接）
    /// 兵种必须有对应革新才能征募（重骑兵需马镫）
    /// </summary>
    public class MilitaryExpansionTests
    {
        private InnovationTree _tree;
        private GameWorld _world;

        [SetUp]
        public void Setup()
        {
            ContentRegistry.Reset();
            ContentRegistry.Initialize();
            _tree = new InnovationTree();
            _world = new GameWorld();
            // 反射调用私有 InitializeUnitDefs（GameWorld 为 MonoBehaviour，unitDefs 需初始化）
            typeof(GameWorld).GetMethod("InitializeUnitDefs",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(_world, null);
        }

        private static void Complete(InnovationTree tree, int id)
        {
            Assert.IsTrue(tree.StartResearch(1, id), $"革新 {id} 应可研究");
            tree.DailyTick(1, 100000f);
        }

        // ===== 军事革新在位（后勤/通讯/组织） =====

        [Test]
        public void MilitaryInnovations_AllPresent()
        {
            // 组织 4
            Assert.IsNotNull(_tree.GetInnovation(995), "军队编制");
            Assert.IsNotNull(_tree.GetInnovation(998), "军功爵制");
            Assert.IsNotNull(_tree.GetInnovation(1002), "常备军");
            Assert.IsNotNull(_tree.GetInnovation(1003), "募兵制");
            // 通讯 3
            Assert.IsNotNull(_tree.GetInnovation(996), "号角军令");
            Assert.IsNotNull(_tree.GetInnovation(999), "烽燧传警");
            Assert.IsNotNull(_tree.GetInnovation(1005), "急递铺");
            // 后勤 4
            Assert.IsNotNull(_tree.GetInnovation(997), "粮秣辎重");
            Assert.IsNotNull(_tree.GetInnovation(1000), "兵站补给");
            Assert.IsNotNull(_tree.GetInnovation(1001), "驮运畜力");
            Assert.IsNotNull(_tree.GetInnovation(1004), "军屯");

            // 全部归军事相关分类（军制或交通）
            Assert.AreEqual(InnovationField.MilitaryInstitution, _tree.GetInnovation(995).field, "编制=军制");
            Assert.AreEqual(InnovationField.MilitaryInstitution, _tree.GetInnovation(1000).field, "兵站=军制");
        }

        [Test]
        public void MilitaryInnovation_PrerequisiteChains()
        {
            // 组织链：部落联盟→军队编制→（军功爵：+成文法/常备军：+中央集权/募兵：+铸币）
            Assert.IsTrue(_tree.GetInnovation(995).prerequisites.Contains(500), "编制需部落联盟");
            Assert.IsTrue(_tree.GetInnovation(998).prerequisites.Contains(995), "军功爵需编制");
            Assert.IsTrue(_tree.GetInnovation(998).prerequisites.Contains(505), "军功爵需成文法");
            Assert.IsTrue(_tree.GetInnovation(1002).prerequisites.Contains(502), "常备军需中央集权");
            Assert.IsTrue(_tree.GetInnovation(1003).prerequisites.Contains(701), "募兵需铸币");

            // 通讯链：编制→号角→烽燧（+筑城）/急递铺（+驿传）
            Assert.IsTrue(_tree.GetInnovation(999).prerequisites.Contains(996), "烽燧需号角");
            Assert.IsTrue(_tree.GetInnovation(1005).prerequisites.Contains(823), "急递铺需驿传");

            // 后勤链：轮车→粮秣→兵站（+道路）；牲畜→驮运
            Assert.IsTrue(_tree.GetInnovation(997).prerequisites.Contains(807), "粮秣需轮车");
            Assert.IsTrue(_tree.GetInnovation(1000).prerequisites.Contains(997), "兵站需粮秣");
            Assert.IsTrue(_tree.GetInnovation(1001).prerequisites.Contains(919), "驮运需牲畜");
        }

        // ===== 兵种 × 革新挂接 =====

        [Test]
        public void UnitDef_HeavyCavalry_RequiresStirrup()
        {
            // 用户点名：重骑兵必须持有马镫（924）才能征募
            var heavyCavalry = _world.unitDefs[201];
            Assert.IsTrue(heavyCavalry.requiredInnovations.Contains(924), "重骑兵需马镫");

            // 未持有马镫：不可征募（兵种可用性判定）
            Assert.IsFalse(IsUnitAvailable(_world, 201, _tree, 1), "无马镫时重骑兵不可征募");

            // 完成骑兵链（狗驯化→牲畜→马驯化→骑乘 + 铁器链→马镫）后可征募
            Complete(_tree, 911); Complete(_tree, 919); Complete(_tree, 922);
            Complete(_tree, 923);
            Complete(_tree, 200); Complete(_tree, 201); Complete(_tree, 300);
            Complete(_tree, 202); Complete(_tree, 301); // 铁制武器（马镫前置）
            Complete(_tree, 924);
            Assert.IsTrue(IsUnitAvailable(_world, 201, _tree, 1), "马镫后重骑兵可征募");
        }

        [Test]
        public void UnitDef_UnitInnovationMapping()
        {
            // 弓箭手需弓箭（907）；弩手需弩（304）；轻骑兵需骑乘术（923）
            Assert.IsTrue(_world.unitDefs[110].requiredInnovations.Contains(907), "弓箭手需弓箭");
            Assert.IsTrue(_world.unitDefs[111].requiredInnovations.Contains(304), "弩手需弩");
            Assert.IsTrue(_world.unitDefs[200].requiredInnovations.Contains(923), "轻骑兵需骑乘术");
            // 基础兵种无要求
            Assert.AreEqual(0, _world.unitDefs[100].requiredInnovations.Count, "轻装步兵基础可用");
            // 战船挂航海链
            Assert.IsTrue(_world.unitDefs[300].requiredInnovations.Contains(402), "桨帆船需桨帆船革新");
        }

        /// <summary>兵种可用性判定（requiredInnovations 全部持有——任一前置不满足不可征募）</summary>
        private static bool IsUnitAvailable(GameWorld world, int unitId, InnovationTree tree, int realmId)
        {
            var def = world.unitDefs[unitId];
            if (def.requiredInnovations == null) return true;
            foreach (int id in def.requiredInnovations)
            {
                if (!tree.HasInnovation(realmId, id)) return false;
            }
            return true;
        }
    }
}
