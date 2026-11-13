using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Generation;
using MyriadCreation.Simulation.Faith;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 被删专项测试的补位（审计 D P0-3）：宗教规则 + 地形生成确定性。
    /// 纯规则/确定性验证，不需要完整对局夹具。
    /// </summary>
    public class RestoredSystemTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private int _worldSeq;

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _objects)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _objects.Clear();
        }

        // ===== 宗教规则 =====

        [Test]
        public void FaithSystem_AddFervor_ClampsTo0And100()
        {
            var faith = new FaithSystem();

            faith.fervor = 50f;
            faith.AddFervor(20f);
            Assert.AreEqual(70f, faith.fervor, 0.001f, "热忱应累加");

            faith.AddFervor(100f);
            Assert.AreEqual(100f, faith.fervor, 0.001f, "热忱上限 100");

            faith.AddFervor(-200f);
            Assert.AreEqual(0f, faith.fervor, 0.001f, "热忱下限 0");
        }

        [Test]
        public void FaithSystem_CanDeclareGreatHolyWar_RequiresFervorAndLeader()
        {
            var faith = new FaithSystem { fervor = 59f, highPriestCharacterId = 1 };
            Assert.IsFalse(faith.CanDeclareGreatHolyWar(), "热忱 <60 不可大圣战");

            faith.fervor = 60f;
            Assert.IsTrue(faith.CanDeclareGreatHolyWar(), "热忱 60 且有领袖可大圣战");

            faith.highPriestCharacterId = -1;
            Assert.IsFalse(faith.CanDeclareGreatHolyWar(), "无领袖不可大圣战");
        }

        [Test]
        public void FaithSystem_HierarchyLevelAndTitles()
        {
            var faith = new FaithSystem();
            faith.SetHierarchyLevel(4);
            Assert.AreEqual(4, faith.hierarchyLevel);
            faith.SetHierarchyLevel(9);
            Assert.AreEqual(4, faith.hierarchyLevel, "层级上限 4");
            faith.SetHierarchyLevel(-3);
            Assert.AreEqual(0, faith.hierarchyLevel, "层级下限 0");

            faith.AddHierarchyTitle("大主教", 3, false);
            Assert.AreEqual(1, faith.hierarchyTitles.Count);
            Assert.AreEqual(3, faith.hierarchyLevel, "添加高一级头衔应提升层级");
            Assert.AreEqual("大主教", faith.hierarchyTitles[0].titleName);
        }

        // ===== 地形生成确定性 =====

        [Test]
        public void TerrainGeneration_SameSeed_IsDeterministic()
        {
            var a = Generate(42);
            var b = Generate(42);

            Assert.AreEqual(a.tiles.Length, b.tiles.Length, "地块数");
            int mismatches = 0;
            string firstMismatch = "";
            for (int i = 0; i < a.tiles.Length; i++)
            {
                if (Mathf.Abs(a.tiles[i].elevation01 - b.tiles[i].elevation01) > 0.0001f
                    || a.tiles[i].isLand != b.tiles[i].isLand)
                {
                    mismatches++;
                    if (firstMismatch.Length == 0)
                        firstMismatch = $"idx={i} eA={a.tiles[i].elevation01} eB={b.tiles[i].elevation01} landA={a.tiles[i].isLand} landB={b.tiles[i].isLand}";
                }
            }
            Assert.AreEqual(0, mismatches, "同 seed 地形应逐地块一致：" + firstMismatch);
        }

        [Test]
        public void TerrainGeneration_DifferentSeed_ProducesDifferentMap()
        {
            var a = Generate(42);
            var b = Generate(43);

            int diff = 0;
            for (int i = 0; i < a.tiles.Length; i++)
            {
                if (Mathf.Abs(a.tiles[i].elevation01 - b.tiles[i].elevation01) > 0.001f) diff++;
            }
            Assert.Greater(diff, 0, "不同 seed 应产生不同地形（防 seed 被忽略）");
        }

        private GameWorld Generate(int seed)
        {
            var go = new GameObject("DeterminismWorld_" + seed + "_" + (_worldSeq++));
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = 24;
            world.mapHeight = 12;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(seed);
            return world;
        }
    }
}
