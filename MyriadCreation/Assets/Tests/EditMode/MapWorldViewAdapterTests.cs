using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.Culture;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World;
using MyriadCreation.World.Settlement;
using UnityEngine;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// B7-part1：IMapWorldView / MapWorldViewAdapter 测试。
    /// 小世界构造（手动 tiles/cultures/config/settlements），断言视图与 GameWorld 直接查询一致、越界安全。
    /// </summary>
    public class MapWorldViewAdapterTests
    {
        private GameObject _go;
        private GameWorld _world;
        private WorldConfig _config;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("MapWorldViewTestWorld");
            _world = _go.AddComponent<GameWorld>();
            _world.mapWidth = 4;
            _world.mapHeight = 3;
            _world.tiles = new TileData[12];
            for (int i = 0; i < 12; i++)
            {
                _world.tiles[i] = new TileData
                {
                    tileIndex = i,
                    exists = true,
                    isLand = (i % 2 == 0),
                    elevation01 = i * 0.05f,
                    slopeDegree = i,
                    ownerRealmId = (i % 3 == 0) ? 3 : -1
                };
            }
            _config = WorldConfig.CreateRuntimeInstance();
            _config.wrapX = true;
            _config.wrapY = false;
            _world.config = _config;
            _world.PlayerRealmId = 3;
            if (_world.cultures == null) _world.cultures = new Dictionary<int, CultureData>();
            if (_world.settlements == null) _world.settlements = new Dictionary<int, SettlementData>();
            _world.cultures[7] = new CultureData
            {
                cultureId = 7,
                parentCultureId = -1,
                allowsBranching = true,
                color = new Color(0.2f, 0.4f, 0.6f, 1f)
            };
            _world.cultures[8] = new CultureData
            {
                cultureId = 8,
                parentCultureId = 7,
                allowsBranching = false,
                color = new Color(0.1f, 0.2f, 0.3f, 0.5f)
            };
            _world.settlements[42] = new SettlementData();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            if (_config != null) Object.DestroyImmediate(_config);
        }

        [Test]
        public void View_SizeWrapPlayer_MatchWorld()
        {
            var view = new MapWorldViewAdapter(_world);
            Assert.AreEqual(_world.mapWidth, view.MapWidth);
            Assert.AreEqual(_world.mapHeight, view.MapHeight);
            Assert.IsTrue(view.WrapX);
            Assert.IsFalse(view.WrapY);
            Assert.AreEqual(_world.PlayerRealmId, view.PlayerRealmId);
        }

        [Test]
        public void View_TryGetTile_MatchesWorld_AndBoundsSafe()
        {
            var view = new MapWorldViewAdapter(_world);
            Assert.IsTrue(view.TryGetTile(5, out TileData tile));
            Assert.AreEqual(_world.tiles[5].tileIndex, tile.tileIndex);
            Assert.AreEqual(_world.tiles[5].exists, tile.exists);
            Assert.AreEqual(_world.tiles[5].isLand, tile.isLand);
            Assert.AreEqual(_world.tiles[5].elevation01, tile.elevation01, 0.0001f);
            Assert.AreEqual(_world.tiles[5].ownerRealmId, tile.ownerRealmId);

            Assert.IsTrue(view.TileExists(0));
            Assert.IsFalse(view.TileExists(-1));
            Assert.IsFalse(view.TileExists(_world.mapWidth * _world.mapHeight));
            Assert.IsFalse(view.TryGetTile(-1, out _));
            Assert.IsFalse(view.TryGetTile(_world.mapWidth * _world.mapHeight, out _));
            Assert.IsFalse(view.TryGetTile(999, out _));
        }

        [Test]
        public void View_Culture_MatchesWorld_AndParent()
        {
            var view = new MapWorldViewAdapter(_world);
            Assert.IsTrue(view.TryGetCulture(7, out MapCultureView culture));
            Assert.AreEqual(7, culture.cultureId);
            Assert.AreEqual(_world.cultures[7].color.r, culture.colorR, 0.0001f);
            Assert.AreEqual(_world.cultures[7].color.g, culture.colorG, 0.0001f);
            Assert.AreEqual(_world.cultures[7].color.b, culture.colorB, 0.0001f);
            Assert.AreEqual(_world.cultures[7].color.a, culture.colorA, 0.0001f);
            Assert.AreEqual(_world.cultures[7].parentCultureId, culture.parentCultureId);
            Assert.AreEqual(_world.cultures[7].allowsBranching, culture.allowsBranching);

            Assert.IsTrue(view.TryGetCulture(8, out MapCultureView child));
            Assert.AreEqual(7, child.parentCultureId);
            Assert.IsFalse(child.allowsBranching);
            Assert.IsFalse(view.TryGetCulture(999, out _));
        }

        [Test]
        public void View_Diplomacy_NoManager_ReturnsFalse_AndAnchorsMatchWorld()
        {
            var view = new MapWorldViewAdapter(_world);
            Assert.IsFalse(view.TryGetDiplomacyRelation(0, 1, out MapDiplomacyView relation));
            Assert.IsNull(relation);
            Assert.IsFalse(view.IsSubordinate(0, 1));

            IReadOnlyList<int> anchors = view.GetSettlementAnchorIds();
            CollectionAssert.AreEqual(new List<int> { 42 }, anchors);
        }

        [Test]
        public void View_WrapMode_ProvinceBorder_MatchesWorld_AndBoundsSafe()
        {
            var view = new MapWorldViewAdapter(_world);
            Assert.AreEqual(_world.wrapMode, view.WrapMode);
            Assert.IsFalse(view.IsProvinceBorder(-1));
            Assert.IsFalse(view.IsProvinceBorder(_world.mapWidth * _world.mapHeight));
            Assert.AreEqual(Province.IsBorder(_world.tiles, _world.mapWidth, _world.mapHeight, 0),
                view.IsProvinceBorder(0));
        }

        [Test]
        public void View_SettlementAndOverlayNoWorldData_ReturnsEmptySafe()
        {
            var view = new MapWorldViewAdapter(_world);
            Assert.AreEqual(1, view.SettlementCount);
            Assert.IsTrue(view.TryGetSettlement(42, out MapSettlementInfo settlement));
            Assert.AreEqual(42, settlement.anchorId);
            Assert.IsFalse(view.TryGetSettlement(999, out _));
            Assert.IsFalse(view.TryGetAnchorAtTile(-1, out _));

            Assert.AreEqual(0, view.ProvinceCount);
            Assert.IsFalse(view.TryGetAdminDivisionOverlay(out _));
        }

        [Test]
        public void View_NomadicRealms_NoRealms_ReturnsEmpty()
        {
            var view = new MapWorldViewAdapter(_world);
            IReadOnlyList<MapRealmInfo> realms = view.GetNomadicRealms();
            Assert.IsNotNull(realms);
            Assert.AreEqual(0, realms.Count);
        }
    }
}
