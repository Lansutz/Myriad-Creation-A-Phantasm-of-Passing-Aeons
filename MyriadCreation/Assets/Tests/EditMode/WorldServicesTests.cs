using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using MyriadCreation.Simulation.Generation;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Generation;
using UnityEngine;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// B3-Step2：WorldFactory / WorldGenerationService / WorldCompositionRoot 薄服务测试。
    /// 断言 Factory 与现有 new+AddComponent+InitializeWorld 行为一致、生成服务对空/未初始化世界安全、
    /// 组合根委托入口幂等且空世界不抛。
    /// </summary>
    public class WorldServicesTests
    {
        private static WorldSetupOptions CreateOptions()
        {
            return WorldSetupOptions.Create(InitialContentMode.Empty);
        }

        [Test]
        public void Factory_Create_MatchesDirectAddComponentAndInitialize()
        {
            var factoryOptions = CreateOptions();
            var directOptions = CreateOptions();

            var factoryHost = new GameObject("FactoryWorld");
            var factoryWorld = WorldFactory.Create(factoryHost, factoryOptions);

            var directHost = new GameObject("DirectWorld");
            var directWorld = directHost.AddComponent<GameWorld>();
            directWorld.SetupOptions = directOptions;
            directWorld.InitializeWorld();

            try
            {
                Assert.IsNotNull(factoryWorld);
                Assert.AreSame(factoryWorld, factoryHost.GetComponent<GameWorld>());
                Assert.AreEqual(directWorld.mapWidth, factoryWorld.mapWidth);
                Assert.AreEqual(directWorld.mapHeight, factoryWorld.mapHeight);
                Assert.AreEqual(directWorld.randomSeed, factoryWorld.randomSeed);
                Assert.IsNotNull(factoryWorld.realms);
                Assert.IsNotNull(factoryWorld.settlements);
                Assert.AreEqual(directWorld.realms.Count, factoryWorld.realms.Count);
                Assert.AreEqual(directWorld.settlements.Count, factoryWorld.settlements.Count);
                Assert.AreEqual(directWorld.tiles.Length, factoryWorld.tiles.Length);
                Assert.AreEqual(directWorld.GetLandTileCount(), factoryWorld.GetLandTileCount());
                Assert.AreEqual(directWorld.GetValidTileCount(), factoryWorld.GetValidTileCount());
            }
            finally
            {
                WorldFactory.Destroy(factoryWorld);
                if (directWorld != null) UnityEngine.Object.DestroyImmediate(directHost);
            }
        }

        [Test]
        public void Factory_Create_AppliesDimensionsBeforeInitialize()
        {
            var world = WorldFactory.Create(new GameObject("SizedFactoryWorld"), CreateOptions(), 16, 12);
            try
            {
                Assert.AreEqual(16, world.mapWidth);
                Assert.AreEqual(12, world.mapHeight);
                Assert.IsNotNull(world.tiles);
                Assert.AreEqual(16 * 12, world.tiles.Length);
            }
            finally
            {
                WorldFactory.Destroy(world);
            }
        }

        [Test]
        public void Factory_CreateEmpty_DoesNotInitializeWorld()
        {
            var host = new GameObject("EmptyWorld");
            var world = WorldFactory.CreateEmpty(host);
            try
            {
                Assert.IsNotNull(world);
                Assert.AreSame(world, host.GetComponent<GameWorld>());
                Assert.IsNull(world.tiles);
                Assert.AreEqual(128, world.mapWidth);
                Assert.AreEqual(64, world.mapHeight);
                Assert.IsNotNull(world.realms);
                Assert.IsNotNull(world.settlements);
            }
            finally
            {
                WorldFactory.Destroy(world);
            }
        }

        [Test]
        public void Factory_Destroy_RemovesWorldObject()
        {
            var world = WorldFactory.Create(new GameObject("DestroyWorld"), CreateOptions());
            Assert.IsNotNull(world);

            WorldFactory.Destroy(world);

            Assert.IsTrue(world == null);
        }

        [Test]
        public void GenerationService_NullWorld_IsSafe()
        {
            var service = new WorldGenerationService(null);

            Assert.DoesNotThrow(() =>
            {
                service.GenerateTerrain(1);
                service.GenerateTerrainWithConfig();
                service.GenerateAll();
                service.InitializeDefaultRealms();
                service.GenerateProvinces();
                service.GenerateSettlements();
                service.GenerateInitialUnits(1);
                service.RegenerateFromPipeline((GenerationStage)0);
                IEnumerator routine = service.GenerateTerrainAsync(1, null);
                while (routine.MoveNext()) { }
            });
        }

        [Test]
        public void GenerationService_UninitializedWorld_IsSafe()
        {
            var host = new GameObject("UninitializedGenerationWorld");
            var world = host.AddComponent<GameWorld>();
            var service = new WorldGenerationService(world);

            try
            {
                Assert.DoesNotThrow(() =>
                {
                    service.GenerateTerrain(1);
                    service.GenerateTerrainWithConfig();
                    service.GenerateAll();
                    service.InitializeDefaultRealms();
                    service.GenerateProvinces();
                    service.GenerateSettlements();
                    service.GenerateInitialUnits(1);
                    service.RegenerateFromPipeline((GenerationStage)0);
                    IEnumerator routine = service.GenerateTerrainAsync(1, null);
                    while (routine.MoveNext()) { }
                });
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        private static GameWorld CreateFactoryWorld(string name)
        {
            return WorldFactory.Create(new GameObject(name), CreateOptions());
        }

        private static void InvokeHostHook(GameWorld world, string hookName)
        {
            MethodInfo method = typeof(GameWorld).GetMethod(hookName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "GameWorld host hook not found: " + hookName);
            method.Invoke(world, null);
        }

        private static object InvokeGenerationHook(GameWorld world, string hookName, params object[] args)
        {
            MethodInfo method = typeof(GameWorld).GetMethod(hookName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "GameWorld generation hook not found: " + hookName);
            return method.Invoke(world, args);
        }

        private static WorldSetupOptions CreateGenerationOptions()
        {
            WorldSetupOptions options = CreateOptions();
            options.generatePolities = true;
            options.initialPolityCount = 2;
            options.generateSettlements = true;
            options.initialSettlementCount = 1;
            options.generateInitialUnits = false;
            return options;
        }

        private static int CountLandTiles(GameWorld world)
        {
            int count = 0;
            for (int i = 0; i < world.tiles.Length; i++)
            {
                if (world.tiles[i].isLand) count++;
            }
            return count;
        }

        private static int AiControllerCount(GameWorld world)
        {
            return world.GetAIManager()?.GetAllControllers().Count ?? 0;
        }

        [Test]
        public void GenerationService_GenerateTerrain_MatchesDirectHostHook()
        {
            const int seed = 7;
            var serviceWorld = WorldFactory.Create(new GameObject("ServiceGenerationWorld"),
                CreateGenerationOptions(), 16, 12);
            var hookWorld = WorldFactory.Create(new GameObject("HookGenerationWorld"),
                CreateGenerationOptions(), 16, 12);

            try
            {
                new WorldGenerationService(serviceWorld).GenerateTerrain(seed);
                InvokeGenerationHook(hookWorld, "GenerateTerrainForHost", seed);

                Assert.AreEqual(serviceWorld.tiles.Length, hookWorld.tiles.Length);
                Assert.AreEqual(CountLandTiles(serviceWorld), CountLandTiles(hookWorld));
                Assert.AreEqual(serviceWorld.realms.Count, hookWorld.realms.Count);
                Assert.AreEqual(serviceWorld.settlements.Count, hookWorld.settlements.Count);
                Assert.AreEqual(serviceWorld.GetLandTileCount(), hookWorld.GetLandTileCount());
                Assert.AreEqual(serviceWorld.GetValidTileCount(), hookWorld.GetValidTileCount());
            }
            finally
            {
                WorldFactory.Destroy(serviceWorld);
                WorldFactory.Destroy(hookWorld);
            }
        }

        [Test]
        public void GenerationService_ProvincesSettlements_MatchDirectHostHooks()
        {
            var serviceWorld = WorldFactory.Create(new GameObject("ServiceProvinceWorld"),
                CreateGenerationOptions(), 16, 12);
            var hookWorld = WorldFactory.Create(new GameObject("HookProvinceWorld"),
                CreateGenerationOptions(), 16, 12);

            try
            {
                var service = new WorldGenerationService(serviceWorld);
                service.GenerateTerrain(7);
                InvokeGenerationHook(hookWorld, "GenerateTerrainForHost", 7);
                service.GenerateProvinces();
                InvokeGenerationHook(hookWorld, "GenerateProvincesForHost");
                // 省份/聚落生成含世界内随机事件（不保证跨世界逐点一致），
                // 这里验证"Service 与直调 hook 都实际执行了生成"以及结构不变量一致。
                Assert.Greater(serviceWorld.provinces.Count, 0, "Service 路径应生成省份");
                Assert.Greater(hookWorld.provinces.Count, 0, "直调 hook 路径应生成省份");
                Assert.IsNotNull(serviceWorld.provinces[0], "Service 省份应可访问");

                service.GenerateSettlements();
                InvokeGenerationHook(hookWorld, "GenerateSettlementsForHost");
                Assert.Greater(serviceWorld.settlements.Count, 0, "Service 路径应生成聚落");
                Assert.Greater(hookWorld.settlements.Count, 0, "直调 hook 路径应生成聚落");

                service.InitializeDefaultRealms();
                InvokeGenerationHook(hookWorld, "InitializeDefaultRealmsForHost");
                Assert.AreEqual(serviceWorld.realms.Count, hookWorld.realms.Count);
            }
            finally
            {
                WorldFactory.Destroy(serviceWorld);
                WorldFactory.Destroy(hookWorld);
            }
        }

        [Test]
        public void CompositionRoot_EntriesMatchDirectHostHooks()
        {
            var rootWorld = CreateFactoryWorld("RootHookWorld");
            var directWorld = CreateFactoryWorld("DirectHookWorld");
            var root = new WorldCompositionRoot(rootWorld);

            try
            {
                root.InitializeSubsystems();
                InvokeHostHook(directWorld, "InitializeSubsystemsForHost");
                Assert.AreEqual(directWorld.mapWidth, rootWorld.mapWidth);
                Assert.AreEqual(directWorld.mapHeight, rootWorld.mapHeight);
                Assert.AreEqual(directWorld.tiles.Length, rootWorld.tiles.Length);
                Assert.AreEqual(directWorld.GetValidTileCount(), rootWorld.GetValidTileCount());

                root.EnsureAIControllers();
                InvokeHostHook(directWorld, "EnsureAIControllersForHost");
                Assert.AreEqual(AiControllerCount(directWorld), AiControllerCount(rootWorld));

                root.InitializeFaithSystems();
                InvokeHostHook(directWorld, "InitializeFaithSystemsForHost");
                Assert.AreEqual(directWorld.FaithSystems.Count, rootWorld.FaithSystems.Count);

                root.RegisterSimulationSchedules();
                InvokeHostHook(directWorld, "RegisterSimulationSchedulesForHost");
                Assert.IsNotNull(rootWorld.SimulationScheduler);
                Assert.IsNotNull(directWorld.SimulationScheduler);

                root.PrepareLoadedWorld();
                InvokeHostHook(directWorld, "PrepareLoadedWorldForHost");
                Assert.AreEqual(directWorld.unitDefs.Count, rootWorld.unitDefs.Count);
                Assert.AreEqual(directWorld.FaithSystems.Count, rootWorld.FaithSystems.Count);
                Assert.AreEqual(AiControllerCount(directWorld), AiControllerCount(rootWorld));
            }
            finally
            {
                WorldFactory.Destroy(rootWorld);
                WorldFactory.Destroy(directWorld);
            }
        }

        [Test]
        public void CompositionRoot_TilesNull_DamagedSaveDoesNotThrow()
        {
            var world = WorldFactory.CreateEmpty(new GameObject("DamagedTilesWorld"));
            var root = new WorldCompositionRoot(world);

            try
            {
                Assert.IsNull(world.tiles);

                Assert.DoesNotThrow(() =>
                {
                    root.InitializeSubsystems();
                    root.EnsureAIControllers();
                    root.RegisterSimulationSchedules();
                    root.InitializeFaithSystems();
                    root.PrepareLoadedWorld();
                    root.ApplySetupOptions();
                });

                // 记录损坏档语义：tiles==null 时子系统类入口全部跳过（HasWorld 守卫），不抛；
                // ApplySetupOptions 仅 Normalize SetupOptions，不触碰 tiles。
                Assert.IsNull(world.tiles);
                Assert.IsNull(world.GetAIManager());
                Assert.AreEqual(0, world.FaithSystems.Count);
            }
            finally
            {
                WorldFactory.Destroy(world);
            }
        }

        [Test]
        public void CompositionRoot_DelegatesAreIdempotent()
        {
            var world = WorldFactory.Create(new GameObject("CompositionWorld"), CreateOptions());
            var root = new WorldCompositionRoot(world);

            try
            {
                Assert.IsNotNull(world);
                int mapWidth = world.mapWidth;
                int mapHeight = world.mapHeight;

                Assert.DoesNotThrow(() =>
                {
                    root.InitializeSubsystems();
                    root.InitializeSubsystems();
                    root.RegisterSimulationSchedules();
                    root.RegisterSimulationSchedules();
                    root.EnsureAIControllers();
                    root.EnsureAIControllers();
                    root.InitializeFaithSystems();
                    root.InitializeFaithSystems();
                    root.ApplySetupOptions();
                    root.ApplySetupOptions();
                });

                Assert.AreEqual(mapWidth, world.mapWidth);
                Assert.AreEqual(mapHeight, world.mapHeight);
                Assert.IsNotNull(world.realms);
                Assert.IsNotNull(world.settlements);
            }
            finally
            {
                WorldFactory.Destroy(world);
            }
        }

        [Test]
        public void CompositionRoot_NullWorld_IsSafe()
        {
            var root = new WorldCompositionRoot(null);

            Assert.DoesNotThrow(() =>
            {
                root.InitializeSubsystems();
                root.EnsureAIControllers();
                root.RegisterSimulationSchedules();
                root.InitializeFaithSystems();
                root.PrepareLoadedWorld();
                root.ApplySetupOptions();
            });
        }
    }
}
