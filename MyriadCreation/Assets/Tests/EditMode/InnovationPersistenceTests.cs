using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Core.Data;
using MyriadCreation.Infrastructure.Save;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.Units;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 存读档 v14：革新/科技树运行时状态入档（审计 A-P0-3：此前读档后所有政权科技清零）。
    /// </summary>
    public class InnovationPersistenceTests
    {
        private const string SaveName = "__innovation_saveload_test";
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _objects)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _objects.Clear();
            MapUnitRegistry.Clear();
            CharacterKnowledgeRegistry.Clear();
            RelationRegistry.Clear();
            ContractRegistry.Clear();
            SaveSystem.DeleteSave(SaveName);
        }

        private GameWorld NewWorld(int width = 24, int height = 12)
        {
            var go = new GameObject("InnovationTestWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = width;
            world.mapHeight = height;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(11); // 政权/聚落/省在 GenerateTerrain 里生成
            return world;
        }

        private static int FirstRealmId(GameWorld world)
        {
            foreach (var key in world.realms.Keys) return key;
            return -1;
        }

        /// <summary>让政权 0 真正完成一项可研究的革新，返回革新 id；没有可研究项时返回 -1。</summary>
        private static int ResearchOneInnovation(GameWorld world, int realmId)
        {
            var tree = world.Innovations;
            var available = tree.GetAvailableInnovations(realmId, world);
            if (available == null || available.Count == 0) return -1;

            foreach (var def in available)
            {
                if (def == null) continue;
                tree.AddPractice(realmId, def.field, InnovationTree.GetRequiredPractice(def));
                if (!tree.StartResearch(realmId, def.innovationId, world)) continue;
                // 一次给足研究点，确保完成（DailyTick 内部按 researchCost 判定）
                tree.DailyTick(realmId, def.researchCost + 1f);
                if (tree.HasInnovation(realmId, def.innovationId)) return def.innovationId;
            }
            return -1;
        }

        [Test]
        public void SaveLoad_FullChain_PreservesInnovations()
        {
            var world = NewWorld();
            int realmId = FirstRealmId(world);
            Assert.GreaterOrEqual(realmId, 0, "前置：应有政权");

            int innovationId = ResearchOneInnovation(world, realmId);
            Assert.Greater(innovationId, 0, "前置：应能完成一项革新研究");

            int countBefore = world.Innovations.GetRealmInnovationCount(realmId);
            Assert.AreEqual(1, countBefore, "前置：应恰好完成 1 项革新");

            Assert.IsTrue(SaveSystem.SaveGame(world, SaveName), "存盘应成功");
            var loaded = SaveSystem.LoadGame(SaveName);
            Assert.IsNotNull(loaded, "读档应返回世界");
            _objects.Add(loaded.gameObject);

            Assert.IsTrue(loaded.Innovations.HasInnovation(realmId, innovationId),
                "读档后已完成的革新必须还在（SaveVersion 14）");
            Assert.AreEqual(countBefore, loaded.Innovations.GetRealmInnovationCount(realmId), "革新数量应一致");
        }

        [Test]
        public void CaptureRestore_RoundTripThroughJson_PreservesTreeState()
        {
            var world = NewWorld();
            int realmId = FirstRealmId(world);
            Assert.GreaterOrEqual(realmId, 0, "前置：应有政权");
            Assert.Greater(ResearchOneInnovation(world, realmId), 0, "前置：应能完成一项革新");

            // 再开一项研究但故意不完成，验证"当前研究 + 研究点"也能存
            var available = world.Innovations.GetAvailableInnovations(realmId, world);
            int pendingId = -1;
            if (available != null)
            {
                foreach (var def in available)
                {
                    if (def != null)
                    {
                        world.Innovations.AddPractice(realmId, def.field, InnovationTree.GetRequiredPractice(def));
                    }
                    if (def != null && world.Innovations.StartResearch(realmId, def.innovationId, world))
                    {
                        pendingId = def.innovationId;
                        world.Innovations.DailyTick(realmId, Mathf.Max(1f, def.researchCost * 0.25f));
                        break;
                    }
                }
            }

            var captured = world.Innovations.CaptureState();
            Assert.IsNotNull(captured);
            string json = JsonUtility.ToJson(captured);
            var reloaded = JsonUtility.FromJson<InnovationTreeSaveData>(json);
            Assert.IsNotNull(reloaded);

            var restoredTree = new InnovationTree();
            restoredTree.RestoreState(reloaded);

            int expectedCount = world.Innovations.GetRealmInnovationCount(realmId);
            Assert.AreEqual(expectedCount, restoredTree.GetRealmInnovationCount(realmId), "革新集合应还原");
            Assert.GreaterOrEqual(restoredTree.GetRealmInnovationCount(realmId), 1);
            if (pendingId > 0)
            {
                Assert.AreEqual(pendingId, restoredTree.GetCurrentResearch(realmId)?.innovationId ?? -1,
                    "当前研究应还原");
                Assert.Greater(restoredTree.GetResearchProgress(realmId), 0f, "研究点应还原");
            }
        }
    }
}
