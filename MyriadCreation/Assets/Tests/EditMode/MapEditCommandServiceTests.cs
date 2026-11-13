using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.App.Editing;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Rendering.Editing;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Editing;
using MyriadCreation.Core.Contracts;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// B3-part2c：App 地图编辑命令服务回归。
    /// 覆盖：Elevate/Lower/Smooth/SetBiome/Add/Remove/Recalculate 对小世界生效；
    /// 选区命令/撤销与旧 MapEditHistory 路径一致；控制器无服务时不抛异常。
    /// </summary>
    public class MapEditCommandServiceTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _created)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _created.Clear();
        }

        private GameWorld CreateWorld(int width, int height, float elevation = 0.5f)
        {
            var go = new GameObject("MapEditTestWorld");
            _created.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = width;
            world.mapHeight = height;
            world.tiles = new TileData[width * height];
            for (int i = 0; i < world.tiles.Length; i++)
            {
                world.tiles[i] = new TileData
                {
                    tileIndex = i,
                    exists = true,
                    isLand = elevation > 0f,
                    elevation01 = elevation,
                };
            }
            return world;
        }

        private static MapSelection SelectRect(int width, int height, int x0, int y0, int x1, int y1)
        {
            var selection = new MapSelection(width, height);
            selection.SelectRect(x0, y0, x1, y1);
            return selection;
        }

        private static void LegacyMarkDirty(GameWorld world)
        {
            for (int i = 0; i < world.tiles.Length; i++) world.tiles[i].isTerrainDirty = true;
            world.RecalculateDirty();
        }

        [Test]
        public void ElevateAndLower_ClampAndMatchExpectedValues()
        {
            var world = CreateWorld(5, 5);
            var service = new WorldEditService(() => world);

            Assert.IsTrue(service.ElevateTile(6, 0.2f));
            Assert.AreEqual(0.7f, world.tiles[6].elevation01, 1e-5f, "抬升 0.2");
            Assert.IsTrue(world.tiles[6].isLand);

            Assert.IsTrue(service.LowerTile(6, 0.2f));
            Assert.AreEqual(0.5f, world.tiles[6].elevation01, 1e-5f, "降低 0.2");

            Assert.IsTrue(service.ElevateTile(6, 5f));
            Assert.AreEqual(1f, world.tiles[6].elevation01, 1e-5f, "上限夹取到 +1");
            Assert.IsTrue(service.LowerTile(6, 5f));
            Assert.AreEqual(-1f, world.tiles[6].elevation01, 1e-5f, "下限夹取到 -1");
        }

        [Test]
        public void ElevateLower_MatchesLegacyDirectWritePath()
        {
            var legacy = CreateWorld(4, 4);
            var serviceWorld = CreateWorld(4, 4);

            // 旧路径：直接改 elevation/isLand + 全部 dirty + RecalculateDirty（旧控制器 MarkDirty 语义）。
            legacy.tiles[5].elevation01 = 0.7f;
            legacy.tiles[5].isLand = true;
            LegacyMarkDirty(legacy);

            var service = new WorldEditService(() => serviceWorld);
            Assert.IsTrue(service.ElevateTile(5, 0.2f));

            for (int i = 0; i < legacy.tiles.Length; i++)
            {
                Assert.AreEqual(legacy.tiles[i].elevation01, serviceWorld.tiles[i].elevation01, 1e-5f, "elevation tile " + i);
                Assert.AreEqual(legacy.tiles[i].isLand, serviceWorld.tiles[i].isLand, "isLand tile " + i);
                Assert.AreEqual(legacy.tiles[i].isTerrainDirty, serviceWorld.tiles[i].isTerrainDirty, "dirty tile " + i);
            }
        }

        [Test]
        public void Elevate_OutOfRangeTile_ReturnsFalseAndDoesNotThrow()
        {
            var world = CreateWorld(3, 3);
            var service = new WorldEditService(() => world);
            Assert.IsFalse(service.ElevateTile(-1, 0.1f));
            Assert.IsFalse(service.ElevateTile(world.tiles.Length, 0.1f));
            Assert.IsFalse(service.SetBiome(world.tiles.Length, GameEnums.BiomeType.LowHills));
        }

        [Test]
        public void Smooth_AveragesNeighbourElevation_AndIsIdempotentOnFlatMap()
        {
            // 中心 0.9，周围 0.1：1 格半径平滑后中心 = 邻域平均 0.1；再平滑保持 0.1。
            var world = CreateWorld(5, 5, 0.1f);
            world.tiles[12].elevation01 = 0.9f;
            var service = new WorldEditService(() => world);

            Assert.IsTrue(service.SmoothTile(12, 1));
            Assert.AreEqual(0.1f, world.tiles[12].elevation01, 1e-5f);

            Assert.IsTrue(service.SmoothTile(12, 1));
            Assert.AreEqual(0.1f, world.tiles[12].elevation01, 1e-5f, "平坦图重复平滑应幂等");
        }

        [Test]
        public void SetBiome_AddRemoveTile_WorkOnSmallWorld()
        {
            var world = CreateWorld(4, 4);
            var service = new WorldEditService(() => world);

            Assert.IsTrue(service.SetBiome(5, GameEnums.BiomeType.LowHills));
            Assert.AreEqual(GameEnums.BiomeType.LowHills, world.tiles[5].biome);

            Assert.IsTrue(service.RemoveTile(5));
            Assert.IsFalse(world.tiles[5].exists);
            Assert.IsTrue(service.AddTile(5));
            Assert.IsTrue(world.tiles[5].exists);
        }

        [Test]
        public void Recalculate_MarksAllDirty_AndIsIdempotent()
        {
            var world = CreateWorld(4, 4);
            for (int i = 0; i < world.tiles.Length; i++) world.tiles[i].isTerrainDirty = false;
            var service = new WorldEditService(() => world);

            service.Recalculate();
            foreach (var tile in world.tiles) Assert.IsTrue(tile.isTerrainDirty);

            Assert.DoesNotThrow(() => service.Recalculate());
            foreach (var tile in world.tiles) Assert.IsTrue(tile.isTerrainDirty);
        }

        [Test]
        public void RaiseSelection_MatchesLegacyHistoryPath()
        {
            var legacy = CreateWorld(5, 5);
            var serviceWorld = CreateWorld(5, 5);
            legacy.tiles[7].elevation01 = 0.9f;
            serviceWorld.tiles[7].elevation01 = 0.9f;

            var legacySelection = SelectRect(5, 5, 1, 1, 2, 1);
            var history = new MapEditHistory();
            history.Execute(legacy.tiles, new RaiseSelectionCommand(legacySelection, 0.2f));
            LegacyMarkDirty(legacy);

            var serviceSelection = SelectRect(5, 5, 1, 1, 2, 1);
            var service = new WorldEditService(() => serviceWorld);
            Assert.IsTrue(service.RaiseSelection(serviceSelection, 0.2f));

            for (int i = 0; i < legacy.tiles.Length; i++)
            {
                Assert.AreEqual(legacy.tiles[i].elevation01, serviceWorld.tiles[i].elevation01, 1e-5f, "elevation tile " + i);
                Assert.AreEqual(legacy.tiles[i].isLand, serviceWorld.tiles[i].isLand, "isLand tile " + i);
            }
        }

        [Test]
        public void MoveSelection_MatchesLegacyHistoryPath_AndUndoRedo()
        {
            var legacy = CreateWorld(5, 5);
            var serviceWorld = CreateWorld(5, 5);
            legacy.tiles[6].elevation01 = 0.9f;
            serviceWorld.tiles[6].elevation01 = 0.9f;

            var legacySelection = SelectRect(5, 5, 1, 1, 1, 1);
            var history = new MapEditHistory();
            history.Execute(legacy.tiles, new MoveSelectionCommand(legacySelection, 1, 0));
            LegacyMarkDirty(legacy);

            var service = new WorldEditService(() => serviceWorld);
            var serviceSelection = SelectRect(5, 5, 1, 1, 1, 1);
            Assert.IsTrue(service.MoveSelection(serviceSelection, 1, 0));

            for (int i = 0; i < legacy.tiles.Length; i++)
            {
                Assert.AreEqual(legacy.tiles[i].exists, serviceWorld.tiles[i].exists, "exists tile " + i);
                Assert.AreEqual(legacy.tiles[i].elevation01, serviceWorld.tiles[i].elevation01, 1e-5f, "elevation tile " + i);
            }

            // 撤销/重做与旧路径语义一致：撤销恢复源地块，重做再次清空源位置。
            Assert.IsTrue(service.Undo());
            Assert.IsTrue(serviceWorld.tiles[6].exists);
            Assert.AreEqual(0.9f, serviceWorld.tiles[6].elevation01, 1e-5f);
            Assert.IsTrue(service.Redo());
            Assert.IsFalse(serviceWorld.tiles[6].exists);
        }

        [Test]
        public void RenderingSource_DoesNotUseAppContractsNamespace()
        {
            string root = Path.Combine(Application.dataPath, "Scripts", "Rendering");
            var offenders = new List<string>();
            foreach (string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(file);
                if (text.Contains("using MyriadCreation.App.Contracts;") || text.Contains("AppServices.MapEdit"))
                {
                    offenders.Add(file);
                }
            }

            Assert.That(offenders, Is.Empty,
                "Rendering 源码仍引用 App.Contracts/AppServices.MapEdit：" + string.Join(", ", offenders));
        }

        [Test]
        public void Controller_WithoutInjectedService_DoesNotThrow()
        {
            var go = new GameObject("RuntimeMapEditingController_NoService");
            _created.Add(go);
            var controller = go.AddComponent<RuntimeMapEditingController>();

            Assert.DoesNotThrow(() => controller.Undo());
            Assert.IsFalse(controller.MoveSelection(1, 0));
            Assert.IsFalse(controller.RaiseSelection(0.1f));
            Assert.IsFalse(controller.ElevateTile(0, 0.1f));
        }

        [Test]
        public void Controller_WithInjectedService_ExecutesCommands()
        {
            var world = CreateWorld(4, 4);
            var service = new WorldEditService(() => world);

            var go = new GameObject("RuntimeMapEditingController_WithService");
            _created.Add(go);
            var controller = go.AddComponent<RuntimeMapEditingController>();
            controller.SetEditService(service);
            controller.SelectRectangle(0, 0, 1, 1);
            Assert.IsTrue(controller.RaiseSelection(0.2f));
            Assert.AreEqual(0.7f, world.tiles[0].elevation01, 1e-5f);
            Assert.IsTrue(controller.Undo());
            Assert.AreEqual(0.5f, world.tiles[0].elevation01, 1e-5f);
        }
    }
}
