using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Core.Data;
using MyriadCreation.Infrastructure.Save;
using MyriadCreation.Rendering.Editing;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.Units;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.UI;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 读档后世界级场景组件重绑回归（task-33 A）。
    ///
    /// SaveSystem.LoadGame 会新建 GameWorld 并让旧世界变成 fake-null；场景里的
    /// RuntimeMapEditingController 必须由 GameManager.LoadGame 重绑（含 inactive 物体）：
    /// 控制器本身不再持有 GameWorld，编辑命令经注入的 WorldEditService 按 provider 解析当前世界，
    /// 因此断言点在"服务已注入 + 命令落在新世界"，而不是旧的对象引用。
    /// </summary>
    public class WorldRebindOnLoadTests
    {
        private const string SaveName = "__world_rebind_on_load_test";

        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
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
            var go = new GameObject("RebindTestWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = width;
            world.mapHeight = height;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(11);
            return world;
        }

        private RuntimeMapEditingController CreateController(bool active)
        {
            var go = new GameObject("RuntimeMapEditingController");
            go.SetActive(active);
            var controller = go.AddComponent<RuntimeMapEditingController>();
            _objects.Add(go);
            return controller;
        }

        /// <summary>
        /// GameManager 用 inactive 物体创建：避免 EditMode 下触发 Awake（DontDestroyOnLoad/单例副作用）。
        /// 这也顺带覆盖"快速读档早于 Awake 注入"的兜底路径。
        /// </summary>
        private GameManager CreateGameManager()
        {
            var go = new GameObject("GameManager");
            go.SetActive(false);
            var manager = go.AddComponent<GameManager>();
            _objects.Add(go);
            return manager;
        }

        private GameWorld SaveAndRegisterLoadedWorld(GameManager manager)
        {
            var loaded = manager.World;
            Assert.IsNotNull(loaded, "LoadGame 成功后 GameManager.World 应为新世界");
            _objects.Add(loaded.gameObject);
            return loaded;
        }

        private static int FirstExistingTile(GameWorld world)
        {
            for (int i = 0; i < world.tiles.Length; i++)
            {
                if (world.tiles[i].exists) return i;
            }
            Assert.Fail("世界生成后应至少有一个可用地块");
            return -1;
        }

        private static void AssertEditLandsOnNewWorld(RuntimeMapEditingController controller, GameWorld loaded)
        {
            Assert.IsNotNull(controller.EditService, "读档后应注入编辑命令服务（否则框选/抬升无实现）");
            int tile = FirstExistingTile(loaded);
            Assert.IsTrue(controller.EditService.SetTerrain(tile, 0.3f),
                "编辑命令 handler 应能写入新世界的地块");
            Assert.AreEqual(0.3f, loaded.tiles[tile].elevation01, 1e-3f,
                "编辑命令必须落在读档后的新世界，而不是已销毁的旧世界");
        }

        [Test]
        public void LoadGame_RebindsActiveRuntimeMapEditingController_ToNewWorld()
        {
            var oldWorld = NewWorld();
            Assert.IsTrue(SaveSystem.SaveGame(oldWorld, SaveName), "前置：存盘应成功");
            var controller = CreateController(active: true);
            var manager = CreateGameManager();

            manager.LoadGame(SaveName);

            var loadedWorld = SaveAndRegisterLoadedWorld(manager);
            Assert.AreNotSame(oldWorld, loadedWorld, "读档应新建 GameWorld 实例");
            AssertEditLandsOnNewWorld(controller, loadedWorld);
        }

        [Test]
        public void LoadGame_RebindsInactiveRuntimeMapEditingController_ToNewWorld()
        {
            var oldWorld = NewWorld();
            Assert.IsTrue(SaveSystem.SaveGame(oldWorld, SaveName), "前置：存盘应成功");
            var controller = CreateController(active: false);
            var manager = CreateGameManager();

            manager.LoadGame(SaveName);

            var loadedWorld = SaveAndRegisterLoadedWorld(manager);
            AssertEditLandsOnNewWorld(controller, loadedWorld);
        }
    }
}
