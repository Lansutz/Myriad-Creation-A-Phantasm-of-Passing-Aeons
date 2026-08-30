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

        [Test]
        public void SuperHeavyCavalry_Tier4_RequiresCataphract()
        {
            // 超重装骑兵（用户设计：tier 4 超重型——cataphract/铁浮屠式）
            var cataphract = _world.unitDefs[203];
            Assert.AreEqual(4, cataphract.tier, "超重装骑兵=超重型 tier4");
            Assert.AreEqual(GameEnums.UnitCategory.Cavalry, cataphract.category);
            Assert.IsTrue(cataphract.requiredInnovations.Contains(1006), "超重装骑兵需具装甲骑革新");
            Assert.Greater(cataphract.defense, _world.unitDefs[202].defense, "防御高于精锐骑兵");
            Assert.Less(cataphract.speed, _world.unitDefs[202].speed, "重甲速度更慢");

            // 具装甲骑革新在位（前置重装骑兵+炼钢）
            var innovation = _tree.GetInnovation(1006);
            Assert.IsNotNull(innovation, "具装甲骑革新应存在");
            Assert.IsTrue(innovation.prerequisites.Contains(303), "前置重装骑兵");
            Assert.IsTrue(innovation.prerequisites.Contains(203), "前置炼钢术");
            Assert.AreEqual(InnovationField.MilitaryInstitution, innovation.field, "军制类");

            // 可用性端到端：无革新时不可征募
            Assert.IsFalse(IsUnitAvailable(_world, 203, _tree, 1), "无具装甲骑时超重装骑兵不可征募");

            // 完整链（骑兵链+铁器链+重装骑兵+炼钢）后可用
            Complete(_tree, 911); Complete(_tree, 919); Complete(_tree, 922);
            Complete(_tree, 923);
            Complete(_tree, 200); Complete(_tree, 201); Complete(_tree, 300);
            Complete(_tree, 202); Complete(_tree, 301); Complete(_tree, 302);
            Complete(_tree, 924); Complete(_tree, 303); // 重装骑兵
            Complete(_tree, 802); Complete(_tree, 803); // 深井采矿（炼钢前置 803）
            Complete(_tree, 203); // 炼钢术（前置 202 已在上段完成）
            Complete(_tree, 1006); // 具装甲骑
            Assert.IsTrue(IsUnitAvailable(_world, 203, _tree, 1), "具装甲骑后超重装骑兵可征募");
        }

        [Test]
        public void NavalShips_RammingAndTradeVessels()
        {
            // 撞角战船（用户点名）：需撞角战术（1007）；物资=原木+铁矿（撞角需铁）
            var ram = _world.unitDefs[302];
            Assert.IsTrue(ram.requiredInnovations.Contains(1007), "撞角战船需撞角战术");
            Assert.IsTrue(ram.recruitCost.ContainsKey(30), "撞角战船需原木");
            Assert.IsTrue(ram.recruitCost.ContainsKey(50), "撞角战船需铁矿（铜铁撞角）");

            // 远洋贸易船：需远洋贸易（1008）；物资=加工木材+棉花（帆布）
            var trader = _world.unitDefs[303];
            Assert.IsTrue(trader.requiredInnovations.Contains(1008), "远洋贸易船需远洋贸易");
            Assert.IsTrue(trader.recruitCost.ContainsKey(31), "远洋贸易船需加工木材");
            Assert.IsTrue(trader.recruitCost.ContainsKey(11), "远洋贸易船需棉花（帆布）");
            Assert.Greater(trader.speed, _world.unitDefs[301].speed, "贸易船比战舰快");

            // 革新在位
            var ramInnovation = _tree.GetInnovation(1007);
            Assert.IsNotNull(ramInnovation, "撞角战术应存在");
            Assert.IsTrue(ramInnovation.prerequisites.Contains(402), "撞角战术前置桨帆船");
            var tradeInnovation = _tree.GetInnovation(1008);
            Assert.IsNotNull(tradeInnovation, "远洋贸易应存在");
            Assert.IsTrue(tradeInnovation.prerequisites.Contains(404), "远洋贸易前置卡拉维尔");
            Assert.IsTrue(tradeInnovation.prerequisites.Contains(701), "远洋贸易前置铸币");
        }

        [Test]
        public void UnitRecruitCosts_MapToEconomyGoods()
        {
            // 物资对应（用户定稿）：兵种招募对应经济系统物资
            // 重骑兵：武器+盔甲+马
            var heavyCav = _world.unitDefs[201];
            Assert.IsTrue(heavyCav.recruitCost.ContainsKey(70), "重骑兵需武器");
            Assert.IsTrue(heavyCav.recruitCost.ContainsKey(71), "重骑兵需盔甲");
            Assert.IsTrue(heavyCav.recruitCost.ContainsKey(20), "重骑兵需马");

            // 超重装：人马双甲（马更多）
            var cataphract = _world.unitDefs[203];
            Assert.Greater(cataphract.recruitCost[20], heavyCav.recruitCost[20], "超重装耗马更多");

            // 帆船战舰：加工木材+铁矿
            var warship = _world.unitDefs[301];
            Assert.IsTrue(warship.recruitCost.ContainsKey(31), "战舰需加工木材");
            Assert.IsTrue(warship.recruitCost.ContainsKey(50), "战舰需铁矿");

            // 桨帆船：原木+加工木材
            var galley = _world.unitDefs[300];
            Assert.IsTrue(galley.recruitCost.ContainsKey(30), "桨帆船需原木");

            // 轻装步兵保持基础武器
            Assert.IsTrue(_world.unitDefs[100].recruitCost.ContainsKey(70), "轻装步兵需武器");
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
