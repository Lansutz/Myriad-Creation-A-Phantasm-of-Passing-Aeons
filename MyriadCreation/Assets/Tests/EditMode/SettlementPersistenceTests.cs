using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Core.Data;
using MyriadCreation.Infrastructure.Save;
using MyriadCreation.Save.Contracts;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World;
using MyriadCreation.World.Anchor;
using MyriadCreation.World.Settlement;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>存读档 v12：省份/聚落/营地入档，读档不再把城墙/建筑/市场/区划/营地清空。</summary>
    public class SettlementPersistenceTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _objects)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _objects.Clear();
        }

        private GameWorld NewWorld()
        {
            // 必须用 AddComponent：new GameWorld() 是 Unity fake-null（== null 为 true），
            // CaptureSites/CampManager 的判空会直接跳过。
            var go = new GameObject("SettlementPersistenceWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            var tiles = new TileData[4];
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i] = new TileData
                {
                    tileIndex = i,
                    exists = true,
                    isLand = true,
                    provinceId = 0
                };
            }

            world.mapWidth = 4;
            world.mapHeight = 1;
            world.tiles = tiles;
            world.provinces = new Dictionary<int, Province>();
            world.anchors = new Dictionary<int, AnchorData>();
            world.settlements = new Dictionary<int, SettlementData>();
            return world;
        }

        private static SettlementData BuildCity(GameWorld world)
        {
            var anchor = new AnchorData
            {
                anchorId = 7,
                provinceId = 0,
                tileIndex = 1,
                x = 1.5f,
                y = 0.5f
            };

            var city = new SettlementData
            {
                siteId = "00000000-0000-0000-0000-000000000007",
                siteMode = SiteMode.SedentarySettlement,
                siteName = "狄奥多西堡",
                settlementForm = SettlementForm.City,
                anchorId = 7,
                anchor = anchor,
                population = 5000f,
                development = 12f,
                wealth = 340f,
                tradePower = 8f,
                fortification = 25f,
                garrison = 300,
                isCapital = true,
                isPort = false,
                hasMarket = true,
                constructionTier = 3,
                constructionProgress = 0.6f,
                primarySector = EconomicSector.Artisanal,
                economicInitialized = true
            };
            city.economicComposition[EconomicSector.Artisanal] = 0.55f;
            city.economicComposition[EconomicSector.Commerce] = 0.45f;

            var wall = city.AddBlock(SettlementBlockKind.Wall, null, 4);
            wall.displayName = "狄奥多西城墙";
            wall.wallLayerCount = 2;
            wall.wallMoatLevel = 1;
            wall.wallRole = WallRole.Primary;

            city.AddBlock(SettlementBlockKind.Building, wall, 2, SettlementBlockSubtype.Tower);

            city.districtsGenerated = true;
            city.districts.Add(new CityDistrict(CityDistrictType.Commercial, "东市", 0.3f, 0.4f, 5f, 100f, 12f));

            city.facilities.Add(new FacilityData
            {
                facilityType = FacilityType.Wall,
                constructionState = ConstructionState.Complete,
                damageState = FacilityDamageState.Damaged,
                repairProgress = 40f
            });

            city.market = new SettlementMarket(capacity: 500f);
            city.market.AddStock(53, 10f);
            city.market.demand[53] = 4f;

            world.anchors[anchor.anchorId] = anchor;
            world.settlements[anchor.anchorId] = city;
            return city;
        }

        [Test]
        public void RoundTrip_ThroughJson_PreservesProvinceAndSettlement()
        {
            var world = NewWorld();
            world.provinces[0] = new Province
            {
                provinceId = 0,
                provinceName = "洛水",
                centerTileIndex = 1,
                memberTiles = new List<int> { 0, 1, 2, 3 }
            };
            BuildCity(world);

            var provinces = SettlementPersistence.CaptureProvinces(world);
            var sites = SettlementPersistence.CaptureSites(world);
            Assert.AreEqual(1, provinces.Count, "省份应入档");
            Assert.AreEqual(1, sites.Count, "聚落应入档");

            // 走一遍 JSON：验证字段名/数组在磁盘格式里不丢
            var json = JsonUtility.ToJson(new SaveData
            {
                provinces = provinces,
                habitationSites = sites
            });
            var reloaded = JsonUtility.FromJson<SaveData>(json);
            Assert.IsNotNull(reloaded.provinces);
            Assert.IsNotNull(reloaded.habitationSites);

            var target = NewWorld();
            SettlementPersistence.RestoreProvinces(target, reloaded.provinces);
            SettlementPersistence.RestoreSites(target, reloaded.habitationSites);

            Assert.IsTrue(target.provinces.ContainsKey(0), "省份还原");
            Assert.AreEqual("洛水", target.provinces[0].provinceName);
            Assert.AreEqual(4, target.provinces[0].memberTiles.Count);
            Assert.AreEqual(1, target.provinces[0].centerTileIndex);

            Assert.IsTrue(target.anchors.ContainsKey(7), "锚点还原");
            Assert.AreEqual(0, target.anchors[7].provinceId);
            Assert.AreEqual(1, target.anchors[7].tileIndex);

            Assert.IsTrue(target.settlements.ContainsKey(7), "聚落还原");
            var restored = target.settlements[7];
            Assert.AreEqual("狄奥多西堡", restored.settlementName);
            Assert.AreEqual(5000f, restored.population);
            Assert.AreEqual(340f, restored.wealth);
            Assert.AreEqual(300, restored.garrison);
            Assert.IsTrue(restored.isCapital);
            Assert.IsTrue(restored.hasMarket);
            Assert.AreEqual(3, restored.constructionTier);
            Assert.AreEqual(EconomicSector.Artisanal, restored.primarySector);
            Assert.AreEqual(0.55f, restored.economicComposition[EconomicSector.Artisanal], 0.001f);

            Assert.IsTrue(restored.HasBlock(SettlementBlockKind.Wall), "城墙不应读档蒸发");
            var wall = restored.GetBlocks(SettlementBlockKind.Wall)[0];
            Assert.AreEqual("狄奥多西城墙", wall.displayName);
            Assert.AreEqual(2, wall.wallLayerCount);
            Assert.AreEqual(1, wall.wallMoatLevel);
            Assert.AreEqual(WallRole.Primary, wall.wallRole);

            Assert.AreEqual(1, restored.facilities.Count);
            Assert.AreEqual(40f, restored.facilities[0].repairProgress, 0.01f);

            Assert.AreEqual(1, restored.districts.Count);
            Assert.AreEqual("东市", restored.districts[0].districtName);
            Assert.IsTrue(restored.districtsGenerated);

            Assert.IsNotNull(restored.market, "市场不应读档蒸发");
            Assert.AreEqual(10f, restored.market.GetStock(53), 0.01f);
            Assert.AreEqual(500f, restored.market.capacity, 0.01f);
        }

        [Test]
        public void RoundTrip_PreservesCamps()
        {
            var world = NewWorld();
            var camps = new CampManager(world);

            var campSave = new HabitationSiteSaveData
            {
                site_id = "00000000-0000-0000-0000-000000000009",
                site_mode = SiteModeKeys.TemporaryEncampment,
                site_class = "bandit",
                location = new SiteLocationSaveData { x = 3.5f, y = 0.5f },
                anchor_id = 9,
                site_name = "北营",
                provinceId = 0,
                tileIndex = 3,
                population = 120f,
                camp_id = 5,
                camp_type = CampType.Bandit.ToString(),
                camp_tile_index = 3,
                owner_realm_id = 2,
                established_day = 88,
                camp_defense = 14f,
                camp_supplies = 42f,
                camp_morale = 66f,
                camp_permanence = 0.25f,
                camp_mobility = (int)CampMobility.Moving,
                camp_target_tile = 7,
                camp_attribute_keys = new[] { "mood" },
                camp_attribute_values = new[] { 0.5f }
            };

            var json = JsonUtility.ToJson(new SaveData { habitationSites = new List<HabitationSiteSaveData> { campSave } });
            var reloaded = JsonUtility.FromJson<SaveData>(json);

            var target = NewWorld();
            var targetCamps = new CampManager(target);
            SettlementPersistence.RestoreSites(target, reloaded.habitationSites, targetCamps);

            Assert.AreEqual(1, targetCamps.Count, "营地应还原");
            var camp = targetCamps.AllCamps[0];
            Assert.AreEqual(5, camp.campId);
            Assert.AreEqual(CampType.Bandit, camp.type);
            Assert.AreEqual("北营", camp.siteName);
            Assert.AreEqual(42f, camp.supplies, 0.01f);
            Assert.AreEqual(2, camp.ownerRealmId);
            Assert.AreEqual(0.5f, camp.attributes["mood"], 0.001f);
            Assert.IsTrue(target.anchors.ContainsKey(9), "营地锚点应还原");
            Assert.AreEqual(5, target.tiles[3].campId, "地块应回指营地");
            Assert.AreEqual(CampMobility.Moving, camp.mobility, "迁移状态应还原");
            Assert.AreEqual(7, camp.targetTileIndex, "迁移目标应还原");
        }

        [Test]
        public void LegacySave_WithoutSiteFields_LoadsAsEmptyWorld()
        {
            // v11 之前的档没有 provinces/habitationSites 段
            var json = "{\"version\":11,\"mapWidth\":4,\"mapHeight\":1}";
            var reloaded = JsonUtility.FromJson<SaveData>(json);
            // JsonUtility 对缺失的 List 字段给空列表（不是 null），两种都要能读
            Assert.IsTrue(reloaded.provinces == null || reloaded.provinces.Count == 0);
            Assert.IsTrue(reloaded.habitationSites == null || reloaded.habitationSites.Count == 0);

            var target = NewWorld();
            target.settlements[3] = new SettlementData();
            SettlementPersistence.RestoreProvinces(target, reloaded.provinces);
            SettlementPersistence.RestoreSites(target, reloaded.habitationSites);

            Assert.AreEqual(0, target.provinces.Count);
            Assert.AreEqual(0, target.settlements.Count, "旧档应按空世界处理，不残留上一局聚落");
        }
    }
}
