using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.Faith;
using MyriadCreation.Simulation.FaithHost;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using UnityEngine;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// UI 4b3b：IFaithQuery 扩展（snapshot 字段/大圣战/saints/doctrine）测试。
    /// 小世界手搭 GameWorld + FaithSystem + Canonization 静态表；断言与直接读取 FaithSystem 一致、无 faith 安全。
    /// </summary>
    public class FaithQueryExtensionTests
    {
        private const int RealmId = 1;
        private const int StateFaithId = 777777;
        private const int OtherFaithId = 888888;

        private GameObject _go;
        private GameWorld _world;
        private FaithSystem _stateFaith;
        private FaithSystem _otherFaith;
        private FaithContracts _contracts;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("FaithQueryExtensionWorld");
            _world = _go.AddComponent<GameWorld>();
            _world.mapWidth = 4;
            _world.mapHeight = 4;
            _world.tiles = new TileData[16];
            for (int i = 0; i < _world.tiles.Length; i++)
            {
                _world.tiles[i] = new TileData { tileIndex = i, exists = true };
            }
            if (_world.realms == null) _world.realms = new Dictionary<int, RealmData>();
            _world.realms[RealmId] = new RealmData
            {
                realmId = RealmId,
                realmName = "Faith Realm",
                stateReligionId = StateFaithId
            };
            _world.PlayerRealmId = RealmId;

            _stateFaith = new FaithSystem
            {
                faithId = StateFaithId,
                faithName = "State Faith",
                fervor = 70f,
                type = FaithType.Monotheistic,
                orgType = FaithOrganizationType.Episcopal,
                hierarchyLevel = 3,
                highPriestCharacterId = 5,
                churchInfluence = 50f
            };
            _stateFaith.holySiteTileIndices.Add(0);
            _stateFaith.holySiteTileIndices.Add(1);
            _stateFaith.regionAdherence[RealmId] = 10f;
            _stateFaith.followerCharacterIds.Add(1);
            _stateFaith.followerCharacterIds.Add(2);
            _stateFaith.doctrines.Add(new Doctrine
            {
                name = "Test Doctrine",
                description = "Doctrine description",
                category = (DoctrineCategory)0,
                strictness = 0.5f
            });

            _otherFaith = new FaithSystem
            {
                faithId = OtherFaithId,
                faithName = "Other Faith",
                fervor = 10f,
                type = FaithType.Animistic,
                orgType = FaithOrganizationType.Decentralized,
                hierarchyLevel = 0,
                highPriestCharacterId = -1
            };

            var faithField = typeof(GameWorld).GetField("_faithSystems",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var faiths = (List<FaithSystem>)faithField.GetValue(_world);
            faiths.Clear();
            faiths.Add(_stateFaith);
            faiths.Add(_otherFaith);

            CanonizationSystem.Reset();
            var saintsField = typeof(CanonizationSystem).GetField("_saintsByFaith",
                BindingFlags.Static | BindingFlags.NonPublic);
            var saints = (Dictionary<int, List<SaintDef>>)saintsField.GetValue(null);
            saints[StateFaithId] = new List<SaintDef>
            {
                new SaintDef
                {
                    saintId = 2,
                    linkedCharacterId = 20,
                    saintName = "Saint B",
                    domain = "war",
                    faithId = StateFaithId,
                    canonizationPiety = 88f
                },
                new SaintDef
                {
                    saintId = 1,
                    linkedCharacterId = 10,
                    saintName = "Saint A",
                    domain = "peace",
                    faithId = StateFaithId,
                    canonizationPiety = 92f
                }
            };

            _contracts = new FaithContracts(_world);
        }

        [TearDown]
        public void TearDown()
        {
            CanonizationSystem.Reset();
            if (_go != null) UnityEngine.Object.DestroyImmediate(_go);
        }

        [Test]
        public void FaithSnapshot_ExtendedFields_MatchFaithSystem()
        {
            Assert.IsTrue(_contracts.TryGetFaithSnapshot(StateFaithId, out FaithSnapshotDto snapshot));
            Assert.AreEqual(StateFaithId, snapshot.faithId);
            Assert.AreEqual("State Faith", snapshot.faithName);
            Assert.AreEqual(70f, snapshot.fervor, 0.0001f);
            Assert.AreEqual((int)FaithType.Monotheistic, snapshot.faithType);
            Assert.AreEqual((int)FaithOrganizationType.Episcopal, snapshot.faithOrganizationType);
            Assert.AreEqual(3, snapshot.hierarchyLevel);
            Assert.AreEqual(_stateFaith.CalculateReligiousAuthority(), snapshot.authority, 0.0001f);
            Assert.AreEqual(2, snapshot.holySiteCount);
            Assert.AreEqual(5, snapshot.highPriestCharacterId);
            Assert.IsTrue(snapshot.isStateReligion);

            Assert.IsTrue(_contracts.TryGetFaithSnapshot(OtherFaithId, out FaithSnapshotDto other));
            Assert.AreEqual((int)FaithType.Animistic, other.faithType);
            Assert.AreEqual((int)FaithOrganizationType.Decentralized, other.faithOrganizationType);
            Assert.IsFalse(other.isStateReligion);

            Assert.IsFalse(_contracts.TryGetFaithSnapshot(999999, out FaithSnapshotDto missing));
            Assert.IsNull(missing);
        }

        [Test]
        public void CanDeclareGreatHolyWar_ReusesFaithSystemCondition()
        {
            Assert.IsTrue(_contracts.CanDeclareGreatHolyWar(RealmId));
            Assert.IsFalse(_contracts.CanDeclareGreatHolyWar(999));

            _stateFaith.highPriestCharacterId = -1;
            Assert.IsFalse(_contracts.CanDeclareGreatHolyWar(RealmId), "无领袖不满足大圣战条件");

            _stateFaith.highPriestCharacterId = 5;
            _stateFaith.fervor = 10f;
            Assert.IsFalse(_contracts.CanDeclareGreatHolyWar(RealmId), "热忱<60 不满足大圣战条件");
        }

        [Test]
        public void Saints_ReturnCanonizationList_InIdOrder()
        {
            IReadOnlyList<FaithSaintDto> saints = _contracts.GetSaints(StateFaithId);
            Assert.AreEqual(2, saints.Count);
            Assert.AreEqual(1, saints[0].saintId);
            Assert.AreEqual("Saint A", saints[0].saintName);
            Assert.AreEqual("peace", saints[0].domain);
            Assert.AreEqual(92f, saints[0].canonizationPiety, 0.0001f);
            Assert.AreEqual(10, saints[0].linkedCharacterId);
            Assert.AreEqual(2, saints[1].saintId);

            Assert.AreEqual(0, _contracts.GetSaints(OtherFaithId).Count);
            Assert.AreEqual(0, _contracts.GetSaints(999999).Count);
        }

        [Test]
        public void Doctrines_ReturnSelectedOrRuntimeDoctrines()
        {
            IReadOnlyList<FaithDoctrineDto> doctrines = _contracts.GetDoctrines(StateFaithId);
            Assert.AreEqual(1, doctrines.Count);
            Assert.AreEqual("Test Doctrine", doctrines[0].name);
            Assert.AreEqual("Doctrine description", doctrines[0].description);

            Assert.AreEqual(0, _contracts.GetDoctrines(OtherFaithId).Count);
            Assert.AreEqual(0, _contracts.GetDoctrines(999999).Count);
        }

        [Test]
        public void WorldReadModel_ReusesExtendedFaithQuery()
        {
            var model = new WorldReadModel(_world);
            Assert.IsTrue(model.TryGetFaithSnapshot(StateFaithId, out FaithSnapshotDto snapshot));
            Assert.AreEqual(3, snapshot.hierarchyLevel);
            Assert.IsTrue(snapshot.isStateReligion);
            Assert.IsTrue(model.CanDeclareGreatHolyWar(RealmId));
            Assert.AreEqual(2, model.GetSaints(StateFaithId).Count);
            Assert.AreEqual(1, model.GetDoctrines(StateFaithId).Count);
        }

        [Test]
        public void NoFaithAndNullWorld_AreSafe()
        {
            var noWorld = new FaithContracts(null);
            Assert.IsFalse(noWorld.TryGetFaithSnapshot(StateFaithId, out FaithSnapshotDto snapshot));
            Assert.IsNull(snapshot);
            Assert.IsFalse(noWorld.CanDeclareGreatHolyWar(RealmId));
            Assert.AreEqual(0, noWorld.GetSaints(StateFaithId).Count);
            Assert.AreEqual(0, noWorld.GetDoctrines(StateFaithId).Count);

            var model = new WorldReadModel(null);
            Assert.IsFalse(model.TryGetFaithSnapshot(StateFaithId, out _));
            Assert.IsFalse(model.CanDeclareGreatHolyWar(RealmId));
            Assert.AreEqual(0, model.GetSaints(StateFaithId).Count);
            Assert.AreEqual(0, model.GetDoctrines(StateFaithId).Count);
        }
    }
}
