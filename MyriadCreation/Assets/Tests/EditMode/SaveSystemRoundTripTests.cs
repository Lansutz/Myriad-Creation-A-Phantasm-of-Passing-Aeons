using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Core.Constants;
using MyriadCreation.Core.Data;
using MyriadCreation.Infrastructure.Save;
using MyriadCreation.Simulation.Economy;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.Units;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World;
using MyriadCreation.World.Generation;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 存读档字段级回归（审计 D：现有全链路测试只比表数量，字段丢失发现不了）。
    /// 覆盖：日历 / 地块 / 省份 / 聚落(人口财富守军市场区划设施) / 军队(士气补给编制) /
    /// 政权金库 / 行政区划 holder / 革新完成状态 / SaveVersion。
    /// </summary>
    public class SaveSystemRoundTripTests
    {
        private const string SaveName = "__roundtrip_field_test";
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

        private GameWorld NewWorld()
        {
            var go = new GameObject("RoundTripWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = 48;
            world.mapHeight = 24;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(11);
            return world;
        }

        private static int FirstRealmId(GameWorld world)
        {
            foreach (var key in world.realms.Keys) return key;
            return -1;
        }

        private static SettlementData FirstSettlement(GameWorld world)
        {
            foreach (var s in world.settlements.Values) return s;
            return null;
        }

        [Test]
        public void SaveLoad_FieldLevel_RoundTripsAllRepresentativeState()
        {
            var world = NewWorld();
            int realmId = FirstRealmId(world);
            Assert.GreaterOrEqual(realmId, 0, "前置：应有政权");
            var settlement = FirstSettlement(world);
            Assert.IsNotNull(settlement, "前置：应有聚落");

            // ---- 制造可识别状态 ----
            world.currentYear = 321;
            world.currentDay = 77;

            int modifiedTile = settlement.tileIndex;
            var tile = world.tiles[modifiedTile];
            tile.development = 0.77f;
            tile.stability = 61f;
            world.tiles[modifiedTile] = tile;

            Province province = null;
            foreach (var p in world.provinces.Values) { province = p; break; }
            Assert.IsNotNull(province, "前置：应有省份");
            province.provinceName = "测试省";
            province.centerTileIndex = modifiedTile;

            settlement.population = 4321f;
            settlement.wealth = 987f;
            settlement.garrison = 654;
            settlement.constructionTier = 4;
            var market = settlement.GetOrCreateMarket();
            market.capacity = 500f; // 前置：保证库存放得下（仓储块可能尚未建成，容量为 0）
            Assert.IsTrue(market.AddStock(53, 42f), "前置：市场应能入库");
            settlement.districts.Add(new CityDistrict(CityDistrictType.Commercial, "测试东市", 0.3f, 0.4f, 5f, 100f, 12f));
            settlement.facilities.Add(new FacilityData
            {
                facilityType = FacilityType.Wall,
                constructionState = ConstructionState.Complete,
                damageState = FacilityDamageState.Damaged,
                repairProgress = 40f
            });

            var army = world.CreateArmy(realmId, -1, modifiedTile);
            army.morale = 61f;
            army.supply = 44f;
            army.organization = 78f;
            army.training = 55f;
            army.unitCounts[100] = 3f;
            army.moveTargetTile = modifiedTile;
            army.moveProgress = 0.25f;

            world.PlayerRealmId = realmId;
            var realm = world.realms[realmId];
            realm.treasury = 12345f;
            realm.primaryTitleKey = "title_roundtrip";
            realm.heirId = realm.monarchId >= 0 ? realm.monarchId : 4242;
            realm.realmForm = RealmForm.Nomadic;
            realm.nomadicSubtype = NomadicSubtype.Steppe;
            int primaryCultureId = -1;
            foreach (int cultureId in world.cultures.Keys) { primaryCultureId = cultureId; break; }
            Assert.GreaterOrEqual(primaryCultureId, 0, "前置：应有文化");
            realm.primaryCultureId = primaryCultureId;
            realm.successionLawFromCulture = false;
            realm.senateSeats = 42;

            var division = new AdminDivision
            {
                divisionId = 9001,
                realmId = realmId,
                level = 1,
                name = "测试国",
                titleId = "title_ruler",
                holderCharacterId = 42
            };
            world.realms[realmId].adminDivisions.Add(division);

            // 完成一项革新（如可研究）
            int researchedInnovation = -1;
            var available = world.Innovations.GetAvailableInnovations(realmId, world);
            if (available != null)
            {
                foreach (var def in available)
                {
                    if (def == null) continue;
                    if (!world.Innovations.StartResearch(realmId, def.innovationId, world)) continue;
                    world.Innovations.DailyTick(realmId, def.researchCost + 1f);
                    if (world.Innovations.HasInnovation(realmId, def.innovationId))
                    {
                        researchedInnovation = def.innovationId;
                        break;
                    }
                }
            }

            // ---- 存盘 ----
            Assert.IsTrue(SaveSystem.SaveGame(world, SaveName), "存盘应成功");

            // 版本写入校验
            string path = Path.Combine(SaveSystem.GetSaveDirectory(), SaveName + ".sav");
            Assert.IsTrue(File.Exists(path), "存档文件应存在");
            var raw = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            Assert.IsNotNull(raw);
            Assert.AreEqual(GameConstants.SaveVersion, raw.version, "存档版本应为当前 SaveVersion");

            // ---- 读档并逐字段断言 ----
            var loaded = SaveSystem.LoadGame(SaveName);
            Assert.IsNotNull(loaded, "读档应返回世界");
            _objects.Add(loaded.gameObject);

            Assert.AreEqual(321, loaded.currentYear, "年");
            Assert.AreEqual(77, loaded.currentDay, "日");
            Assert.AreEqual(0.77f, loaded.tiles[modifiedTile].development, 0.001f, "地块发展度");
            Assert.AreEqual(61f, loaded.tiles[modifiedTile].stability, 0.001f, "地块稳定度");

            Province loadedProvince = null;
            foreach (var p in loaded.provinces.Values) { loadedProvince = p; break; }
            Assert.IsNotNull(loadedProvince, "省份应还原");
            Assert.AreEqual("测试省", loadedProvince.provinceName, "省份名");
            Assert.AreEqual(modifiedTile, loadedProvince.centerTileIndex, "省中心");

            Assert.IsTrue(loaded.settlements.TryGetValue(settlement.anchorId, out var loadedSettlement), "聚落应还原");
            Assert.AreEqual(4321f, loadedSettlement.population, 0.01f, "聚落人口");
            Assert.AreEqual(987f, loadedSettlement.wealth, 0.01f, "聚落财富");
            Assert.AreEqual(654, loadedSettlement.garrison, "聚落守军");
            Assert.AreEqual(4, loadedSettlement.constructionTier, "建造等级");
            Assert.AreEqual(42f, loadedSettlement.market.GetStock(53), 0.01f, "市场库存");
            Assert.AreEqual(1, loadedSettlement.districts.Count, "城市区划");
            Assert.AreEqual("测试东市", loadedSettlement.districts[0].districtName);
            Assert.AreEqual(1, loadedSettlement.facilities.Count, "设施");
            Assert.AreEqual(40f, loadedSettlement.facilities[0].repairProgress, 0.01f, "设施修复进度");

            Assert.IsTrue(loaded.armies.TryGetValue(army.armyId, out var loadedArmy), "军队应还原");
            Assert.AreEqual(61f, loadedArmy.morale, 0.01f, "军队士气");
            Assert.AreEqual(44f, loadedArmy.supply, 0.01f, "军队补给");
            Assert.AreEqual(78f, loadedArmy.organization, 0.01f, "军队组织");
            Assert.AreEqual(55f, loadedArmy.training, 0.01f, "军队训练");
            Assert.AreEqual(3f, loadedArmy.unitCounts[100], 0.001f, "军队编制");
            Assert.AreEqual(modifiedTile, loadedArmy.currentTileIndex, "军队位置");
            Assert.AreEqual(modifiedTile, loadedArmy.moveTargetTile, "军队行军目标");
            Assert.AreEqual(0.25f, loadedArmy.moveProgress, 0.001f, "军队行军进度");

            Assert.AreEqual(realmId, loaded.PlayerRealmId, "玩家政权");
            Assert.IsTrue(loaded.realms.TryGetValue(realmId, out var loadedRealm), "政权应还原");
            Assert.AreEqual(12345f, loadedRealm.treasury, 0.01f, "国库");
            Assert.AreEqual("title_roundtrip", loadedRealm.primaryTitleKey, "主头衔");
            Assert.AreEqual(realm.heirId, loadedRealm.heirId, "继承人");
            Assert.AreEqual(RealmForm.Nomadic, loadedRealm.realmForm, "政权形态");
            Assert.AreEqual(NomadicSubtype.Steppe, loadedRealm.nomadicSubtype, "游牧亚型");
            Assert.AreEqual(primaryCultureId, loadedRealm.primaryCultureId, "主体文化");
            Assert.IsFalse(loadedRealm.successionLawFromCulture, "继承法来源");
            Assert.AreEqual(42, loadedRealm.senateSeats, "议会席位");

            AdminDivision loadedDivision = null;
            foreach (var d in loadedRealm.adminDivisions)
            {
                if (d.divisionId == 9001) { loadedDivision = d; break; }
            }
            Assert.IsNotNull(loadedDivision, "行政区划应还原");
            Assert.AreEqual("title_ruler", loadedDivision.titleId, "区划头衔");
            Assert.AreEqual(42, loadedDivision.holderCharacterId, "区划 holder");

            Assert.Greater(researchedInnovation, 0, "前置：应能完成一项革新研究（否则说明内容/前置条件异常）");
            Assert.IsTrue(loaded.Innovations.HasInnovation(realmId, researchedInnovation),
                "已完成的革新应还原（SaveVersion 14）");
        }
    }
}
