using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MyriadCreation.Core.Constants;
using MyriadCreation.Core.Data;
using MyriadCreation.Infrastructure.Save;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.Units;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Generation;
using MyriadCreation.Simulation.Faith;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 信仰运行态入档回归（SaveVersion 15；审计 A P1-3 剩余项）。
    ///
    /// FaithSystem 的内容定义（faithId/faithName/virtues/sins）读档后由 ReligionCatalog 重建、不入档；
    /// 本测试覆盖运行态：fervor / churchWealth / churchInfluence / 传教热忱 / 宽容度 / 大祭司 /
    /// 教阶+教职 / 圣地 / 追随者 / 地区归附 / 教际关系 的存读往返，旧档缺 faiths 段保持内容默认，
    /// 以及 DTO 的 JsonUtility 并行数组往返。
    /// </summary>
    public class FaithPersistenceTests
    {
        private const string SaveName = "__faith_saveload_test";
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
            var go = new GameObject("FaithPersistenceTestWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = width;
            world.mapHeight = height;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(11);
            return world;
        }

        private static string SavePath()
            => Path.Combine(SaveSystem.GetSaveDirectory(), SaveName + ".sav");

        private static SaveData ReadRawSave()
            => JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath()));

        [Test]
        public void SaveLoad_FaithRuntimeState_RoundTrips()
        {
            var world = NewWorld();
            var faiths = new List<FaithSystem>(world.FaithSystems);
            Assert.Greater(faiths.Count, 0, "前置：内容表应提供教统（ReligionCatalog Succession 节点）");

            var faith = faiths[0];
            int faithId = faith.faithId;
            string faithName = faith.faithName;

            // 构造可识别的运行态（默认：fervor=50 / 无圣地 / 无追随者 / 无归附 / 无关系 / 教阶 0）。
            faith.fervor = 87.5f;
            faith.churchWealth = 1234.5f;
            faith.churchInfluence = 66.25f;
            faith.missionaryZeal = 0.8f;
            faith.tolerance = 0.2f;
            faith.highPriestCharacterId = 42;
            faith.AddHierarchyTitle("牧首", 3, temporalAppointment: false); // 同时把 hierarchyLevel 抬到 3
            faith.holySiteTileIndices.Clear();
            faith.holySiteTileIndices.Add(7);
            faith.holySiteTileIndices.Add(11);
            faith.followerCharacterIds.Clear();
            faith.followerCharacterIds.Add(101);
            faith.followerCharacterIds.Add(102);
            faith.regionAdherence.Clear();
            faith.regionAdherence[5] = 0.6f;
            faith.regionAdherence[9] = 0.25f;
            faith.faithRelations.Clear();
            faith.faithRelations[999] = 0.75f;
            faith.faithRelations[1000] = -0.5f;

            Assert.IsTrue(SaveSystem.SaveGame(world, SaveName), "存盘应成功");

            // 捕获顺序：DTO 按 faithId 升序（存档稳定，可与内容表对齐）。
            var raw = ReadRawSave();
            Assert.IsNotNull(raw, "存档应可解析");
            Assert.IsNotNull(raw.faiths, "存档应含 faiths 段（SaveVersion 15）");
            for (int i = 1; i < raw.faiths.Count; i++)
            {
                Assert.Less(raw.faiths[i - 1].faithId, raw.faiths[i].faithId, "捕获应按 faithId 升序");
            }

            var loaded = SaveSystem.LoadGame(SaveName);
            Assert.IsNotNull(loaded, "读档应成功");
            _objects.Add(loaded.gameObject);
            Assert.AreEqual(faiths.Count, loaded.FaithSystems.Count, "教统数量应与内容重建值一致");

            var loadedFaith = loaded.GetFaithSystem(faithId);
            Assert.IsNotNull(loadedFaith, "应按 faithId 找回教统");
            Assert.AreEqual(faithName, loadedFaith.faithName, "内容定义 faithName 应由内容表重建、不被运行态覆盖");
            Assert.AreEqual(87.5f, loadedFaith.fervor, 0.0001f, "fervor");
            Assert.AreEqual(1234.5f, loadedFaith.churchWealth, 0.0001f, "churchWealth");
            Assert.AreEqual(66.25f, loadedFaith.churchInfluence, 0.0001f, "churchInfluence");
            Assert.AreEqual(0.8f, loadedFaith.missionaryZeal, 0.0001f, "missionaryZeal");
            Assert.AreEqual(0.2f, loadedFaith.tolerance, 0.0001f, "tolerance");
            Assert.AreEqual(42, loadedFaith.highPriestCharacterId, "highPriestCharacterId");
            Assert.AreEqual(3, loadedFaith.hierarchyLevel, "hierarchyLevel");
            Assert.AreEqual(1, loadedFaith.hierarchyTitles.Count, "hierarchyTitles 数量");
            Assert.AreEqual("牧首", loadedFaith.hierarchyTitles[0].titleName, "教职名");
            Assert.AreEqual(3, loadedFaith.hierarchyTitles[0].level, "教职等级");
            Assert.IsFalse(loadedFaith.hierarchyTitles[0].temporalAppointment, "教职任命方式");
            CollectionAssert.AreEqual(new List<int> { 7, 11 }, loadedFaith.holySiteTileIndices, "holySiteTileIndices");
            CollectionAssert.AreEqual(new List<int> { 101, 102 }, loadedFaith.followerCharacterIds, "followerCharacterIds");
            Assert.AreEqual(2, loadedFaith.regionAdherence.Count, "regionAdherence 数量");
            Assert.AreEqual(0.6f, loadedFaith.regionAdherence[5], 0.0001f, "regionAdherence[5]");
            Assert.AreEqual(0.25f, loadedFaith.regionAdherence[9], 0.0001f, "regionAdherence[9]");
            Assert.AreEqual(2, loadedFaith.faithRelations.Count, "faithRelations 数量");
            Assert.AreEqual(0.75f, loadedFaith.faithRelations[999], 0.0001f, "faithRelations[999]");
            Assert.AreEqual(-0.5f, loadedFaith.faithRelations[1000], 0.0001f, "faithRelations[1000]");
        }

        [Test]
        public void LoadGame_OldSaveWithoutFaithsSection_KeepsContentDefaults()
        {
            var world = NewWorld();
            int faithCount = world.FaithSystems.Count;
            Assert.Greater(faithCount, 0, "前置：内容表应提供教统");
            Assert.IsTrue(SaveSystem.SaveGame(world, SaveName), "前置：存盘应成功");

            // 构造 v14 旧档：删掉 faiths 段并把版本号改小（LoadGame 只警告、仍加载）。
            var oldSave = ReadRawSave();
            Assert.IsNotNull(oldSave, "前置：存档应可解析");
            oldSave.faiths = null;
            oldSave.version = GameConstants.SaveVersion - 1;
            File.WriteAllText(SavePath(), JsonUtility.ToJson(oldSave, true));

            var loaded = SaveSystem.LoadGame(SaveName);
            Assert.IsNotNull(loaded, "旧档缺 faiths 段不应读档失败");
            _objects.Add(loaded.gameObject);
            Assert.AreEqual(faithCount, loaded.FaithSystems.Count, "旧档读档后教统数应为内容重建值");

            foreach (var faith in loaded.FaithSystems)
            {
                Assert.AreEqual(50f, faith.fervor, 0.0001f, "旧档缺段时 fervor 保持内容默认 50");
                Assert.AreEqual(-1, faith.highPriestCharacterId, "旧档缺段时大祭司保持 -1");
                Assert.AreEqual(0, faith.hierarchyLevel, "旧档缺段时教阶保持 0");
                Assert.AreEqual(0, faith.hierarchyTitles.Count, "旧档缺段时教职保持空");
                Assert.AreEqual(0, faith.holySiteTileIndices.Count, "旧档缺段时圣地保持空");
                Assert.AreEqual(0, faith.followerCharacterIds.Count, "旧档缺段时追随者保持空");
                Assert.AreEqual(0, faith.regionAdherence.Count, "旧档缺段时地区归附保持空");
                Assert.AreEqual(0, faith.faithRelations.Count, "旧档缺段时教际关系保持空");
            }
        }

        [Test]
        public void LoadGame_UnknownFaithId_SkipsWithWarning()
        {
            var world = NewWorld();
            int faithCount = world.FaithSystems.Count;
            Assert.Greater(faithCount, 0, "前置：内容表应提供教统");
            Assert.IsTrue(SaveSystem.SaveGame(world, SaveName), "前置：存盘应成功");

            var raw = ReadRawSave();
            Assert.IsNotNull(raw, "前置：存档应可解析");
            raw.faiths = raw.faiths ?? new List<FaithSaveData>();
            raw.faiths.Add(new FaithSaveData { faithId = 99999, fervor = 77f });
            File.WriteAllText(SavePath(), JsonUtility.ToJson(raw, true));

            LogAssert.Expect(LogType.Warning,
                "[FaithPersistence] 存档中的 faithId=99999 在内容表中不存在，跳过恢复");
            var loaded = SaveSystem.LoadGame(SaveName);
            Assert.IsNotNull(loaded, "未知 faithId 不应导致读档失败");
            _objects.Add(loaded.gameObject);
            Assert.AreEqual(faithCount, loaded.FaithSystems.Count, "未知 faithId 不应凭空建出教统");
            Assert.IsNull(loaded.GetFaithSystem(99999), "GetFaithSystem(未知 id) 应为 null");
        }

        [Test]
        public void FaithSaveData_JsonRoundTrip_PreservesParallelArrays()
        {
            var dto = new FaithSaveData
            {
                faithId = 104,
                fervor = 88.5f,
                churchWealth = 321.25f,
                churchInfluence = 55.5f,
                missionaryZeal = 0.7f,
                tolerance = 0.3f,
                highPriestCharacterId = 12,
                hierarchyLevel = 2,
                holySiteTileIndices = new[] { 3, 9 },
                followerCharacterIds = new[] { 21, 22 },
                regionAdherenceRegionIds = new[] { 4, 8 },
                regionAdherenceValues = new[] { 0.4f, 0.9f },
                relationFaithIds = new[] { 2, 3 },
                relationValues = new[] { -0.5f, 0.5f },
                hierarchyTitleNames = new[] { "大主教", "主教" },
                hierarchyTitleLevels = new[] { 2, 1 },
                hierarchyTitleTemporalAppointments = new[] { true, false }
            };

            string json = JsonUtility.ToJson(dto);
            var back = JsonUtility.FromJson<FaithSaveData>(json);
            Assert.IsNotNull(back, "FaithSaveData 应可通过 JsonUtility 往返");
            Assert.AreEqual(dto.faithId, back.faithId);
            Assert.AreEqual(dto.fervor, back.fervor, 0.0001f);
            Assert.AreEqual(dto.churchWealth, back.churchWealth, 0.0001f);
            Assert.AreEqual(dto.churchInfluence, back.churchInfluence, 0.0001f);
            Assert.AreEqual(dto.missionaryZeal, back.missionaryZeal, 0.0001f);
            Assert.AreEqual(dto.tolerance, back.tolerance, 0.0001f);
            Assert.AreEqual(dto.highPriestCharacterId, back.highPriestCharacterId);
            Assert.AreEqual(dto.hierarchyLevel, back.hierarchyLevel);
            CollectionAssert.AreEqual(dto.holySiteTileIndices, back.holySiteTileIndices, "holySiteTileIndices 往返");
            CollectionAssert.AreEqual(dto.followerCharacterIds, back.followerCharacterIds, "followerCharacterIds 往返");
            CollectionAssert.AreEqual(dto.regionAdherenceRegionIds, back.regionAdherenceRegionIds, "regionAdherenceRegionIds 往返");
            Assert.AreEqual(dto.regionAdherenceValues.Length, back.regionAdherenceValues.Length, "regionAdherenceValues 长度");
            for (int i = 0; i < dto.regionAdherenceValues.Length; i++)
            {
                Assert.AreEqual(dto.regionAdherenceValues[i], back.regionAdherenceValues[i], 0.0001f,
                    "regionAdherenceValues[" + i + "] 往返");
            }
            CollectionAssert.AreEqual(dto.relationFaithIds, back.relationFaithIds, "relationFaithIds 往返");
            Assert.AreEqual(dto.relationValues.Length, back.relationValues.Length, "relationValues 长度");
            for (int i = 0; i < dto.relationValues.Length; i++)
            {
                Assert.AreEqual(dto.relationValues[i], back.relationValues[i], 0.0001f,
                    "relationValues[" + i + "] 往返");
            }
            CollectionAssert.AreEqual(dto.hierarchyTitleNames, back.hierarchyTitleNames, "hierarchyTitleNames 往返");
            CollectionAssert.AreEqual(dto.hierarchyTitleLevels, back.hierarchyTitleLevels, "hierarchyTitleLevels 往返");
            CollectionAssert.AreEqual(dto.hierarchyTitleTemporalAppointments, back.hierarchyTitleTemporalAppointments,
                "hierarchyTitleTemporalAppointments 往返");
        }

        [Test]
        public void Denominations_ParentPointers_AreHierarchyOnly()
        {
            var world = NewWorld();
            int real = 0;
            int defaults = 0;
            foreach (var denomination in world.Denominations)
            {
                Assert.IsNotNull(denomination);
                if (denomination.isDefault)
                {
                    defaults++;
                    continue;
                }
                real++;
                ReligionDef parent = ReligionCatalog.Get(denomination.religionId);
                Assert.IsNotNull(parent, $"宗派 {denomination.denominationId} 的上位 religion 应存在");
                Assert.AreEqual(ReligionNodeType.Religion, parent.nodeType,
                    $"宗派 {denomination.denominationId} 的 parentReligionId 只能指向 religion");
            }

            Assert.AreEqual(15, world.Denominations.Count, "当前内容：8 个真实宗派 + 7 个默认宗派");
            Assert.AreEqual(8, real, "真实宗派数");
            Assert.AreEqual(7, defaults, "默认宗派数");

            AssertDenominationReligion(world, 304, 300);
            AssertDenominationReligion(world, 305, 300);
            AssertDenominationReligion(world, 306, 300);
            AssertDenominationReligion(world, 307, 300);
            AssertDenominationReligion(world, 308, 300);
            AssertDenominationReligion(world, 404, 400);
            AssertDenominationReligion(world, 405, 400);
            AssertDenominationReligion(world, 502, 500);

            Assert.AreEqual(304, world.GetDenomination(307).parentDenominationId, "日莲宗分裂自法华宗");
            Assert.AreEqual(305, world.GetDenomination(308).parentDenominationId, "灭喜禅分裂自禅宗");
            Assert.AreEqual(-1, world.GetDenomination(304).parentDenominationId, "法华宗直接由 religion 产生");

            ReligionDef prophecy = ReligionCatalog.Get(402);
            Assert.IsNotNull(prophecy);
            Assert.AreEqual(400, prophecy.parentReligionId, "谶纬的层级父应是华夏祭祀体系");
            Assert.AreEqual(401, prophecy.schoolParentId, "谶纬与儒教的学派关系走 schoolParentId");
        }

        [Test]
        public void EveryFaith_IsAttachedToDenomination_OfItsReligion()
        {
            var world = NewWorld();
            int expectedFaiths = 0;
            foreach (var def in MyriadCreation.Content.Definitions.ReligionCatalog.All.Values)
            {
                if (def.nodeType == MyriadCreation.Content.Definitions.ReligionNodeType.Succession) expectedFaiths++;
            }
            Assert.AreEqual(expectedFaiths, world.FaithSystems.Count, "每个 Succession 节点应建一条 faith 运行态");

            foreach (var faith in world.FaithSystems)
            {
                Assert.AreNotEqual(-1, faith.denominationId, $"faith {faith.faithId} 应挂宗派");
                Denomination denomination = world.GetDenomination(faith.denominationId);
                Assert.IsNotNull(denomination, $"faith {faith.faithId} 的宗派 {faith.denominationId} 应存在");
                Assert.AreEqual(faith.religionId, denomination.religionId,
                    $"faith {faith.faithId} 的宗派应与 faith 同属一个 religion");
                CollectionAssert.Contains(denomination.faithIds, faith.faithId,
                    $"宗派 {denomination.denominationId} 的 faithIds 应含 {faith.faithId}");
            }
        }

        [Test]
        public void SaveLoad_DenominationRuntimeState_RoundTrips()
        {
            var world = NewWorld();
            Denomination denomination = world.GetDenomination(304);
            Assert.IsNotNull(denomination, "前置：法华宗宗派应存在");
            denomination.status = DenominationStatus.Heterodox;
            denomination.divergence = 42.5f;
            denomination.sinceDay = 321;

            Assert.IsTrue(SaveSystem.SaveGame(world, SaveName), "存盘应成功");
            var loaded = SaveSystem.LoadGame(SaveName);
            Assert.IsNotNull(loaded, "读档应成功");
            _objects.Add(loaded.gameObject);

            Denomination loadedDenomination = loaded.GetDenomination(304);
            Assert.IsNotNull(loadedDenomination, "读档后法华宗宗派应存在");
            Assert.AreEqual(DenominationStatus.Heterodox, loadedDenomination.status, "status");
            Assert.AreEqual(42.5f, loadedDenomination.divergence, 0.0001f, "divergence");
            Assert.AreEqual(321, loadedDenomination.sinceDay, "sinceDay");
            Assert.AreEqual(304, loaded.GetDenomination(307).parentDenominationId, "宗派分裂父");
        }

        [Test]
        public void DenominationFallback_ReanchorsFromChildFaith_OnNegativeDefaultId()
        {
            var original = new List<ReligionDef>(ReligionCatalog.All.Values);
            var go = new GameObject("DenominationFallbackTestWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            try
            {
                ReligionCatalog.Load(new List<ReligionDef>
                {
                    new ReligionDef
                    {
                        religionId = 900,
                        religionName = "Fallback Religion",
                        parentReligionId = -1,
                        nodeType = ReligionNodeType.Religion
                    },
                    new ReligionDef
                    {
                        religionId = 901,
                        religionName = "Parent Faith",
                        parentReligionId = 900,
                        nodeType = ReligionNodeType.Succession
                    },
                    new ReligionDef
                    {
                        religionId = 902,
                        religionName = "Child Faith",
                        parentReligionId = 901,
                        nodeType = ReligionNodeType.Succession
                    }
                });

                var faiths = (List<FaithSystem>)typeof(GameWorld)
                    .GetField("_faithSystems", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(world);
                faiths.Clear();
                faiths.Add(new FaithSystem { faithId = 901, faithName = "Parent Faith", religionId = 900 });
                faiths.Add(new FaithSystem { faithId = 902, faithName = "Child Faith", religionId = 901 });

                MethodInfo initialize = typeof(GameWorld)
                    .GetMethod("InitializeDenominations", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(initialize);
                initialize.Invoke(world, null);

                Assert.AreEqual(-100900, world.GetFaithSystem(901).denominationId,
                    "父 faith 应挂 religion 默认宗派");
                Assert.AreEqual(-100900, world.GetFaithSystem(902).denominationId,
                    "子 faith 应上溯到同一个默认宗派；负号 id 不能被旧守卫误判为未找到");
                Assert.IsNotNull(world.GetDenomination(-100900), "默认宗派应真实存在");
            }
            finally
            {
                ReligionCatalog.Load(original);
            }
        }

        private static void AssertDenominationReligion(GameWorld world, int denominationId, int religionId)
        {
            Denomination denomination = world.GetDenomination(denominationId);
            Assert.IsNotNull(denomination, $"宗派 {denominationId} 应存在");
            Assert.AreEqual(religionId, denomination.religionId, $"宗派 {denominationId} 的上位 religion");
        }
    }
}
