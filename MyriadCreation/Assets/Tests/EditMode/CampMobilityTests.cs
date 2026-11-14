using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Core.Data;
using MyriadCreation.Save.Contracts;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Anchor;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P11.6：营地可在扎营/收起/迁移之间切换；归属以 ownerActorId 为真源，
    /// 地块占用与锚点必须随状态同步，不能留悬空引用。
    /// </summary>
    public class CampMobilityTests
    {
        private GameObject _go;
        private GameWorld _world;
        private CampManager _camps;

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        private CampManager NewCampWorld(int width, int height, int landCount)
        {
            _go = new GameObject("CampMobilityWorld");
            _world = _go.AddComponent<GameWorld>();
            _world.mapWidth = width;
            _world.mapHeight = height;
            _world.tiles = new TileData[width * height];
            for (int i = 0; i < _world.tiles.Length; i++)
            {
                _world.tiles[i] = new TileData { exists = true, isLand = i < landCount, provinceId = i < landCount ? 1 : -1 };
            }
            _world.anchors = new Dictionary<int, AnchorData>();
            _camps = new CampManager(_world);
            return _camps;
        }

        [Test]
        public void Pack_ClearsGroundOccupancyAndAnchor()
        {
            NewCampWorld(3, 1, 3);
            CampData camp = _camps.EstablishCamp(CampType.Nomad, 0, 50, ownerRealmId: 1, ownerActorId: 9);
            int anchorId = camp.anchorId;
            Assert.AreEqual(camp.campId, _world.tiles[0].campId);

            Assert.IsTrue(_camps.PackCamp(camp.campId));

            Assert.AreEqual(CampMobility.Packed, camp.mobility);
            Assert.AreEqual(-1, _world.tiles[0].campId, "收起后不得继续占用地面");
            Assert.IsFalse(_world.anchors.ContainsKey(anchorId), "收起后地图锚点应移除");
            CollectionAssert.IsEmpty(_camps.ActiveHabitationSites.ToList(), "收起状态不算活动栖居点");
        }

        [Test]
        public void Moving_TicksTowardTarget_AndAnchorFollows()
        {
            NewCampWorld(3, 1, 3);
            CampData camp = _camps.EstablishCamp(CampType.Nomad, 0, 50, ownerRealmId: 1, ownerActorId: 9);
            int anchorId = camp.anchorId;

            Assert.IsTrue(_camps.MoveCamp(camp.campId, 2));
            Assert.AreEqual(CampMobility.Moving, camp.mobility);

            _camps.Tick(1f);
            Assert.AreEqual(1, camp.tileIndex, "一天推进一格");
            Assert.AreEqual(1, _world.anchors[anchorId].tileIndex, "锚点随队移动");
            Assert.AreEqual(camp.campId, _world.tiles[1].campId);

            _camps.Tick(1f);
            Assert.AreEqual(2, camp.tileIndex);
            Assert.AreEqual(CampMobility.Pitched, camp.mobility, "到达后自动扎营");
            Assert.AreEqual(-1, camp.targetTileIndex);
            Assert.AreEqual(-1, _world.tiles[1].campId, "旧地块占用应清除");
        }

        [Test]
        public void Unpack_RestoresAnchorAndOccupancy()
        {
            NewCampWorld(3, 1, 3);
            CampData camp = _camps.EstablishCamp(CampType.Nomad, 0, 50, ownerRealmId: 1, ownerActorId: 9);
            int anchorId = camp.anchorId;
            _camps.PackCamp(camp.campId);

            Assert.IsTrue(_camps.UnpackCamp(camp.campId, 2));

            Assert.AreEqual(CampMobility.Pitched, camp.mobility);
            Assert.AreEqual(2, camp.tileIndex);
            Assert.AreEqual(camp.campId, _world.tiles[2].campId);
            Assert.IsTrue(_world.anchors.ContainsKey(anchorId));
            Assert.AreEqual(2, _world.anchors[anchorId].tileIndex);
        }

        [Test]
        public void PackedCamp_IsNotActiveSite_ButStillOwnedByActor()
        {
            NewCampWorld(3, 1, 3);
            CampData camp = _camps.EstablishCamp(CampType.Nomad, 0, 50, ownerRealmId: 1, ownerActorId: 9);
            _camps.PackCamp(camp.campId);

            Assert.IsFalse(_camps.ActiveHabitationSites.Any());
            Assert.AreEqual(9, camp.ownerActorId, "归属只认 ownerActorId");
            Assert.AreEqual(0, _camps.GetCampsAtTile(0).Count, "收起后不占地块");
        }

        [Test]
        public void SaveDto_CapturesMobilityAndTarget()
        {
            NewCampWorld(3, 1, 3);
            CampData camp = _camps.EstablishCamp(CampType.Nomad, 0, 50, ownerRealmId: 1, ownerActorId: 9);
            _camps.MoveCamp(camp.campId, 2);

            var dto = new HabitationSiteSaveData();
            dto.CaptureCampData(camp);

            Assert.AreEqual((int)CampMobility.Moving, dto.camp_mobility);
            Assert.AreEqual(2, dto.camp_target_tile);
        }
    }
}
