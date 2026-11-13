using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 革新系统完备性：物产门槛的世界上下文、取消研究、可用列表、内容完整性。
    /// </summary>
    public class InnovationSystemTests
    {
        private InnovationTree _tree;

        [SetUp]
        public void Setup()
        {
            ContentRegistry.Reset();
            ContentRegistry.Initialize();
            _tree = new InnovationTree();
        }

        private static void CompleteChain(InnovationTree tree, int id)
        {
            var def = tree.GetInnovation(id);
            Assert.IsNotNull(def, $"革新 {id} 不存在");
            foreach (int pre in def.GetPrerequisiteIds())
                if (!tree.HasInnovation(1, pre)) CompleteChain(tree, pre);
            Assert.IsTrue(tree.StartResearch(1, id), $"革新 {id} 应可开始（科技前置已满足）");
            tree.DailyTick(1, 100000f);
        }

        [Test]
        public void ResourceGatedTech_StartsWithoutWorld_AfterTechPrereqs()
        {
            // 201 青铜铸造带物产前置（56）；无 world 时只校验科技前置，不应被整棵锁死
            CompleteChain(_tree, 201);
            Assert.IsTrue(_tree.HasInnovation(1, 201), "青铜铸造应完成");
        }

        private InnovationDef RegisterResourceGate(int id)
        {
            var def = new InnovationDef
            {
                innovationId = id,
                innovationName = "测试资源门槛",
                field = InnovationField.Craft,
                era = 0,
                researchCost = 10f,
                requiredAnyResources = new List<int> { 56 },
                requiredResourceAmount = 0f,
            };
            _tree.RegisterInnovation(def);
            return def;
        }

        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _objects) Object.DestroyImmediate(go);
            _objects.Clear();
        }

        /// <summary>必须用 AddComponent 创建：new GameWorld() 是 Unity fake-null（== null 为 true），会跳过物产校验。</summary>
        private GameWorld WorldWithoutResources()
        {
            var go = new GameObject("InnovationTestWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.tiles = new[]
            {
                new TileData { exists = true, ownerRealmId = 1, resources = new List<TileResource>() }
            };
            return world;
        }
        [Test]
        public void ResourceGatedTech_BlockedWhenWorldLacksResource()
        {
            var def = RegisterResourceGate(9100);
            var world = WorldWithoutResources();

            Assert.IsFalse(_tree.ArePrerequisitesMet(world, 1, def), "无物产点时物产前置不满足");

            world.tiles[0].resources = new List<TileResource>
            {
                new TileResource { goodsId = 56, discovered = true }
            };
            Assert.IsTrue(_tree.ArePrerequisitesMet(world, 1, def), "发现物产点后物产前置满足");
        }

        [Test]
        public void ResourceGatedInnovation_AvailableListDependsOnWorld()
        {
            RegisterResourceGate(9101);
            var world = WorldWithoutResources();
            Assert.IsTrue(_tree.GetAvailableInnovations(1).Exists(item => item.innovationId == 9101),
                "无 world 时物产无法判定，应出现在可研究列表");
            Assert.IsFalse(_tree.GetAvailableInnovations(1, world).Exists(item => item.innovationId == 9101),
                "world 无对应物产时不应出现");

            world.tiles[0].resources = new List<TileResource>
            {
                new TileResource { goodsId = 56, discovered = true }
            };
            Assert.IsTrue(_tree.GetAvailableInnovations(1, world).Exists(item => item.innovationId == 9101),
                "有对应物产后应出现");
        }

        [Test]
        public void CancelResearch_ClearsCurrentAndAllowsRestart()
        {
            Assert.IsTrue(_tree.StartResearch(1, 700), "以物易物应可开始");
            Assert.IsNotNull(_tree.GetCurrentResearch(1));
            Assert.IsTrue(_tree.CancelResearch(1));
            Assert.IsNull(_tree.GetCurrentResearch(1));
            Assert.AreEqual(0f, _tree.GetResearchProgress(1));
            Assert.IsTrue(_tree.StartResearch(1, 700), "取消后可重新开始");
        }

        [Test]
        public void AvailableList_ExcludesResearchedInnovation()
        {
            CompleteChain(_tree, 700);
            var available = _tree.GetAvailableInnovations(1);
            Assert.IsFalse(available.Exists(item => item.innovationId == 700), "已完成革新不应再出现");
        }

        [Test]
        public void Content_PrerequisitesExist_NoEraRegression_NoCycle()
        {
            var innovations = ContentRegistry.Innovations;
            Assert.Greater(innovations.Count, 150, "革新内容数量异常");
            foreach (var pair in innovations)
            {
                var def = pair.Value;
                Assert.Greater(def.researchCost, 0f, $"{def.innovationId} 研究成本必须为正");
                Assert.LessOrEqual(def.era, 5, $"{def.innovationId} era 越界");
                foreach (int pre in def.GetPrerequisiteIds())
                {
                    Assert.IsTrue(innovations.ContainsKey(pre), $"{def.innovationId} 的前置 {pre} 不存在");
                    Assert.LessOrEqual(innovations[pre].era, def.era,
                        $"{def.innovationId}(era {def.era}) 前置 {pre} 的 era 不能更晚");
                }
            }
        }

        [Test]
        public void Content_PrerequisiteGraphHasNoCycle()
        {
            var innovations = ContentRegistry.Innovations;
            var state = new Dictionary<int, int>(); // 0=未访问,1=访问中,2=完成
            foreach (int id in innovations.Keys) state[id] = 0;

            bool Visit(int id)
            {
                state[id] = 1;
                foreach (int pre in innovations[id].GetPrerequisiteIds())
                {
                    if (state[pre] == 1) return false;
                    if (state[pre] == 0 && !Visit(pre)) return false;
                }
                state[id] = 2;
                return true;
            }

            foreach (int id in innovations.Keys)
                if (state[id] == 0)
                    Assert.IsTrue(Visit(id), $"革新前置出现环（涉及 {id}）");
        }
    }
}
