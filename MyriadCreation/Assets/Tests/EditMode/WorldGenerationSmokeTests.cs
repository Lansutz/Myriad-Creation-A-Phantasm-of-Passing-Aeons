using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Core.Data;
using MyriadCreation.Infrastructure.Save;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.Units;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Generation;
using MyriadCreation.World.Settlement;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 世界生成与整档往返的冒烟测试。
    ///
    /// 之前的 298 条测试没有一条真正跑过"生成世界 → 存盘 → 读盘"，
    /// 因此既没发现军事隘口聚落在初始定型时会抛 ForbiddenTransition 崩世界生成，
    /// 也没验证 SaveVersion 12/13 的省份/聚落/军队在整档链路里能回来。
    /// </summary>
    public class WorldGenerationSmokeTests
    {
        private const string SaveName = "__smoke_saveload_test";
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

        private GameWorld NewWorld(int width, int height)
        {
            var go = new GameObject("SmokeWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = width;
            world.mapHeight = height;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            return world;
        }

        private static int FirstLandTile(GameWorld world)
        {
            for (int i = 0; i < world.tiles.Length; i++)
            {
                if (world.tiles[i].exists && world.tiles[i].isLand) return i;
            }
            return -1;
        }

        [Test]
        public void StandardSetup_GenerateTerrain_SucceedsOnBottleneckTerrain()
        {
            var world = NewWorld(64, 32);

            // 种子 7 / 64x32 上有军事隘口（山地隘口/河谷/荒漠走廊）地块：
            // 旧实现在初始定型时对 Sedentary→FortifiedOutpost 走转换规则并抛异常。
            Assert.DoesNotThrow(() => world.GenerateTerrain(7), "标准初始内容的世界生成不应抛异常");
            Assert.Greater(world.settlements.Count, 0, "标准初始内容应生成聚落");
            Assert.Greater(world.provinces.Count, 0, "应生成省份");

            bool hasOutpost = false;
            foreach (var settlement in world.settlements.Values)
            {
                if (settlement.siteMode == SiteMode.FortifiedOutpost) { hasOutpost = true; break; }
            }
            Assert.IsTrue(hasOutpost, "军事隘口聚落应被定型为据点（而不是抛 ForbiddenTransition）");
        }

        [Test]
        public void SaveLoad_FullChain_RestoresTilesProvincesSettlementsRealms()
        {
            var world = NewWorld(48, 24);
            world.GenerateTerrain(11);
            Assert.Greater(world.settlements.Count, 0, "前置：世界应有聚落");
            int firstRealmId = -1;
            foreach (var key in world.realms.Keys) { firstRealmId = key; break; }
            Assert.GreaterOrEqual(firstRealmId, 0, "前置：应有政权");
            // 顺序很关键：先 SpawnActor（unitId=1）再 CreateArmy（unitId=2）。
            // 旧 bug 下读档后军队视图会重新抢到 id=1 并覆盖商队；这样构造 Count 断言才真能失败。
            int landTile = FirstLandTile(world);
            Assert.GreaterOrEqual(landTile, 0, "前置：应有陆地");
            world.MapActors.SpawnActor(MapActorType.Caravan, landTile, 1, null, UnitSubcategory.TradeCaravan);
            world.CreateArmy(firstRealmId, -1, 0);
            Assert.Greater(world.MapActors.AllActors.Count, 0, "前置：应有地图单位（用于验证读档后军队视图不覆盖它们）");

            var before = new Snapshot(world);
            Assert.IsTrue(SaveSystem.SaveGame(world, SaveName), "存盘应成功");

            var loaded = SaveSystem.LoadGame(SaveName);
            Assert.IsNotNull(loaded, "读档应返回世界");
            _objects.Add(loaded.gameObject);

            var after = new Snapshot(loaded);
            Assert.AreEqual(before.tiles, after.tiles, "地块数");
            Assert.AreEqual(before.land, after.land, "陆地数");
            Assert.AreEqual(before.provinces, after.provinces, "省份数");
            Assert.AreEqual(before.settlements, after.settlements, "聚落数（SaveVersion 12）");
            Assert.AreEqual(before.realms, after.realms, "政权数");
            Assert.AreEqual(before.characters, after.characters, "角色数（SaveVersion 11）");
            Assert.AreEqual(before.armies, after.armies, "军队数（SaveVersion 13）");

            // 聚落内容不能只是"数量对"：锚点、坐标、模式要回来
            foreach (var loadedSettlement in loaded.settlements.Values)
            {
                Assert.IsTrue(loaded.anchors.ContainsKey(loadedSettlement.anchorId), "聚落锚点应还原");
            }

            // 读档补齐不入档的运行时定义/控制器（审计 A P0-1/P0-2）
            Assert.Greater(loaded.unitDefs.Count, 0, "读档后兵种定义必须重建（否则军队战力恒 0、战斗永不结束）");
            Assert.AreEqual(loaded.realms.Count, loaded.GetAIManager().GetAllControllers().Count,
                "每个政权都应有 AI 控制器（否则读档后 AI 全停）");

            // 地图单位 + 军队共存：军队视图登记不能覆盖已还原的商队/难民条目（审计 A P0-4 / 复核 R2）
            Assert.Greater(loaded.MapActors.AllActors.Count, 0, "地图单位应还原");
            Assert.AreEqual(loaded.armies.Count + loaded.MapActors.AllActors.Count, MapUnitRegistry.Count,
                "地图单位视图数量应等于军队 + 地图单位，不能被覆盖");

            // 读档后自增 ID 续接（审计 A P0-4 / 测试审计 D 补测 1）
            int maxArmyId = 0;
            foreach (var armyId in loaded.armies.Keys)
            {
                if (armyId > maxArmyId) maxArmyId = armyId;
            }
            int realmId = -1;
            foreach (var key in loaded.realms.Keys) { realmId = key; break; }
            Assert.GreaterOrEqual(realmId, 0, "前置：应有政权");
            var newArmy = loaded.CreateArmy(realmId, -1, 0);
            Assert.Greater(newArmy.armyId, maxArmyId, "读档后新建军队 ID 必须从存档最大值之后续接");
        }

        private readonly struct Snapshot
        {
            public readonly int tiles;
            public readonly int land;
            public readonly int provinces;
            public readonly int settlements;
            public readonly int realms;
            public readonly int characters;
            public readonly int armies;

            public Snapshot(GameWorld world)
            {
                tiles = world.tiles?.Length ?? 0;
                land = world.GetLandTileCount();
                provinces = world.provinces?.Count ?? 0;
                settlements = world.settlements?.Count ?? 0;
                realms = world.realms?.Count ?? 0;
                characters = world.GetCharacterManager()?.GetAllCharacters().Count ?? 0;
                armies = world.armies?.Count ?? 0;
            }
        }
    }
}
