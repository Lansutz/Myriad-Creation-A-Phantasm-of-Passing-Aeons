using System.Collections.Generic;
using System.Reflection;
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
    /// RuntimeMapEditingController 必须由 GameManager.LoadGame 重绑到新世界（含 inactive 物体），
    /// 否则框选/移动/抬升/撤销会继续作用在已销毁的旧世界上。
    /// </summary>
    public class WorldRebindOnLoadTests
    {
        private const string SaveName = "__world_rebind_on_load_test";

        private static readonly FieldInfo ControllerWorldField =
            typeof(RuntimeMapEditingController).GetField("world", BindingFlags.Instance | BindingFlags.NonPublic);

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

        private static GameWorld ControllerWorld(RuntimeMapEditingController controller)
        {
            Assert.IsNotNull(controller, "控制器不应为 null");
            Assert.IsNotNull(ControllerWorldField,
                "找不到 RuntimeMapEditingController.world 字段（反射契约变化，需同步测试）");
            return ControllerWorldField.GetValue(controller) as GameWorld;
        }

        private RuntimeMapEditingController CreateController(GameWorld initialWorld, bool active)
        {
            var go = new GameObject("RuntimeMapEditingController");
            go.SetActive(active);
            var controller = go.AddComponent<RuntimeMapEditingController>();
            _objects.Add(go);
            // 模拟场景里预先接好的引用（读档前指向旧世界）。
            controller.BindWorld(initialWorld);
            return controller;
        }

        /// <summary>
        /// GameManager 用 inactive 物体创建：避免 EditMode 下触发 Awake（DontDestroyOnLoad/单例副作用）。
        /// LoadGame 是实例方法，不依赖 Awake/Instance，可直接调用。
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

        [Test]
        public void LoadGame_RebindsActiveRuntimeMapEditingController_ToNewWorld()
        {
            var oldWorld = NewWorld();
            Assert.IsTrue(SaveSystem.SaveGame(oldWorld, SaveName), "前置：存盘应成功");
            var controller = CreateController(oldWorld, active: true);
            Assert.AreSame(oldWorld, ControllerWorld(controller), "前置：控制器初始应持有旧世界");
            var manager = CreateGameManager();

            manager.LoadGame(SaveName);

            var loadedWorld = SaveAndRegisterLoadedWorld(manager);
            Assert.AreNotSame(oldWorld, loadedWorld, "读档应新建 GameWorld 实例");
            Assert.AreSame(loadedWorld, ControllerWorld(controller),
                "读档后 RuntimeMapEditingController 应重绑到新世界（否则编辑/选择作用在已销毁旧世界）");
        }

        [Test]
        public void LoadGame_RebindsInactiveRuntimeMapEditingController_ToNewWorld()
        {
            var oldWorld = NewWorld();
            Assert.IsTrue(SaveSystem.SaveGame(oldWorld, SaveName), "前置：存盘应成功");
            var controller = CreateController(oldWorld, active: false);
            var manager = CreateGameManager();

            manager.LoadGame(SaveName);

            var loadedWorld = SaveAndRegisterLoadedWorld(manager);
            Assert.AreSame(loadedWorld, ControllerWorld(controller),
                "inactive 的控制器也应被 FindAnyObjectByType(Include) 找到并重绑");
        }

        [Test]
        public void BindWorld_NullWorld_IsNullSafe()
        {
            var world = NewWorld();
            var controller = CreateController(world, active: true);

            Assert.DoesNotThrow(() => controller.BindWorld(null), "BindWorld(null) 不应抛异常");
            Assert.IsNull(ControllerWorld(controller), "BindWorld(null) 后 world 引用应为 null");
        }
    }
}
