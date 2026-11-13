using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.App.Contracts;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.UI;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// task-120：App 会话契约兼容面测试。
    /// - HasWorld 与 concrete World 一致；
    /// - IGameSession 的新 DTO 入口存在且可调用（不实际生成世界）；
    /// - 旧 concrete WorldSetupOptions 重载仍保留给 UI/PlayMode；
    /// - 无订阅的 WorldChanged 已从 GameManager 公开面移除。
    /// GameManager 用 inactive 物体创建，避免 EditMode 下触发 Awake 的 DontDestroyOnLoad/单例副作用。
    /// </summary>
    public class AppSessionContractTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _created)
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
            _created.Clear();
        }

        private GameManager CreateInactiveManager()
        {
            var go = new GameObject("AppSessionContractGameManager");
            go.SetActive(false);
            _created.Add(go);
            return go.AddComponent<GameManager>();
        }

        private static FieldInfo WorldField =>
            typeof(GameManager).GetField("world", BindingFlags.Instance | BindingFlags.NonPublic);

        private static void AssertMethod(Type type, string name, params Type[] parameterTypes)
        {
            MethodInfo method = type.GetMethod(name,
                BindingFlags.Instance | BindingFlags.Public, null, parameterTypes, null);
            Assert.IsNotNull(method,
                type.Name + "." + name + "(" + string.Join(", ", (object[])parameterTypes) + ") 应存在");
        }

        [Test]
        public void HasWorld_MatchesConcreteWorldPresence()
        {
            GameManager manager = CreateInactiveManager();
            Assert.IsNotNull(WorldField, "GameManager.world 字段应存在（concrete World 兼容面）");
            Assert.IsFalse(manager.HasWorld, "新 GameManager 不应有世界");
            Assert.IsNull(manager.World, "新 GameManager.World 应为 null");

            var worldGo = new GameObject("AppSessionContractWorld");
            worldGo.SetActive(false);
            _created.Add(worldGo);
            GameWorld world = worldGo.AddComponent<GameWorld>();
            WorldField.SetValue(manager, world);

            Assert.IsTrue(manager.HasWorld, "world 非 null 时 HasWorld 应为 true");
            Assert.AreSame(world, manager.World, "concrete World 应返回同一世界");

            WorldField.SetValue(manager, null);
            Assert.IsFalse(manager.HasWorld, "world 置空后 HasWorld 应为 false");
        }

        [Test]
        public void DtoEntries_ExistOnInterfaceAndConcrete_AndAreCallable()
        {
            GameManager manager = CreateInactiveManager();
            Assert.IsInstanceOf<IGameSession>(manager, "GameManager 必须实现 IGameSession");

            Assert.IsNotNull(typeof(IGameSession).GetProperty(nameof(IGameSession.HasWorld)),
                "IGameSession.HasWorld 应存在");
            AssertMethod(typeof(IGameSession), nameof(IGameSession.StartNewGame), typeof(NewGameRequestDto));
            AssertMethod(typeof(IGameSession), nameof(IGameSession.StartNewGameAsync),
                typeof(NewGameRequestDto), typeof(Action));

            AssertMethod(typeof(GameManager), nameof(GameManager.StartNewGame), typeof(NewGameRequestDto));
            AssertMethod(typeof(GameManager), nameof(GameManager.StartNewGameAsync),
                typeof(NewGameRequestDto), typeof(Action));

            // 通过接口调用安全入口，不触发世界生成。
            IGameSession session = manager;
            Assert.DoesNotThrow(() =>
                session.ConfigureNewGameDefaults(16, 8, 99, MapWrapMode.Cylindrical));
            Assert.IsFalse(session.HasWorld, "ConfigureNewGameDefaults 不应创建世界");

            // DTO 入口可绑定为委托（不实际调用 StartNewGame，避免 EditMode 下生成世界）。
            MethodInfo dtoMethod = typeof(GameManager).GetMethod(nameof(GameManager.StartNewGame),
                BindingFlags.Instance | BindingFlags.Public, null,
                new[] { typeof(NewGameRequestDto) }, null);
            Assert.IsNotNull(dtoMethod);
            Assert.DoesNotThrow(() =>
                dtoMethod.CreateDelegate(typeof(Action<NewGameRequestDto>), manager));
        }

        [Test]
        public void LegacyConcreteOverloads_RemainForUiCompatibility()
        {
            Assert.IsNotNull(typeof(GameManager).GetProperty("World"),
                "concrete GameManager.World 仍需保留（WorldRebindOnLoadTests 使用）");

            AssertMethod(typeof(GameManager), nameof(GameManager.StartNewGame),
                typeof(int), typeof(int), typeof(int), typeof(MapWrapMode), typeof(WorldSetupOptions));
            AssertMethod(typeof(GameManager), nameof(GameManager.StartNewGameAsync),
                typeof(int), typeof(int), typeof(int), typeof(MapWrapMode), typeof(Action),
                typeof(WorldSetupOptions));
            AssertMethod(typeof(GameManager), nameof(GameManager.StartNewGameAsync),
                typeof(WorldSetupOptions), typeof(Action));

            // UI MainMenuUI 仍调用 concrete SceneFlowController 重载。
            AssertMethod(typeof(SceneFlowController), nameof(SceneFlowController.StartNewGame),
                typeof(WorldSetupOptions));
            AssertMethod(typeof(SceneFlowController), nameof(SceneFlowController.GenerateWorldWithLoading),
                typeof(WorldSetupOptions), typeof(Action));
        }

        [Test]
        public void WorldChanged_HasNoPublicEventAnymore()
        {
            Assert.IsNull(typeof(GameManager).GetEvent("WorldChanged",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
                "WorldChanged 无订阅者，应已从 GameManager 兼容面移除");
        }

        [Test]
        public void NewGameRequestDto_DefaultsUseNamedSentinels()
        {
            var request = new NewGameRequestDto();
            Assert.AreEqual(NewGameRequestDto.DefaultMapSizeSentinel, request.mapWidth);
            Assert.AreEqual(NewGameRequestDto.DefaultMapSizeSentinel, request.mapHeight);
            Assert.AreEqual(NewGameRequestDto.DefaultSeedSentinel, request.seed);
            Assert.AreEqual(NewGameRequestDto.DefaultWrapModeSentinel, request.wrapMode);
            Assert.AreEqual(NewGameRequestDto.DefaultContentModeSentinel, request.contentMode);
            Assert.AreEqual(-1, NewGameRequestDto.DefaultMapSizeSentinel);
            Assert.AreEqual(-1, NewGameRequestDto.DefaultWrapModeSentinel);
            Assert.AreEqual(-1, NewGameRequestDto.DefaultContentModeSentinel);
            Assert.AreEqual(int.MinValue, NewGameRequestDto.DefaultSeedSentinel);
        }
    }
}
