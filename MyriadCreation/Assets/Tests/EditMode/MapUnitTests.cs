using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Units;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P1 第一阶段：MapActor/Army 统一为 MapUnit 视图、类型映射与转换。</summary>
    public class MapUnitTests
    {
        [TearDown]
        public void TearDown() => MapUnitRegistry.Clear();

        [Test]
        public void FromArmy_MapsLeaderAndType()
        {
            var army = new Army { armyId = 7, armyName = "北军", commanderId = 42, ownerRealmId = 3, currentTileIndex = 11 };
            var unit = MapUnit.FromArmy(army);

            Assert.IsNotNull(unit);
            Assert.AreEqual(UnitSourceKind.Army, unit.source);
            Assert.AreEqual(7, unit.sourceId);
            Assert.AreEqual(TransportUnitType.Army, unit.type);
            Assert.AreEqual(42, unit.leaderCharacterId);
            Assert.AreEqual(3, unit.ownerRealmId);
            Assert.AreEqual(11, unit.currentTile);
            Assert.IsTrue(unit.IsArmy);
        }

        [Test]
        public void FromMapActor_MapsTypes()
        {
            Assert.AreEqual(TransportUnitType.Caravan,
                MapUnit.MapActorTypeToUnit(MapActorType.Caravan));
            Assert.AreEqual(TransportUnitType.MercenaryCompany,
                MapUnit.MapActorTypeToUnit(MapActorType.Mercenary));
            Assert.AreEqual(TransportUnitType.Warband,
                MapUnit.MapActorTypeToUnit(MapActorType.Bandit));
            Assert.AreEqual(TransportUnitType.Settlers,
                MapUnit.MapActorTypeToUnit(MapActorType.Refugee));
            Assert.AreEqual(TransportUnitType.Army,
                MapUnit.MapActorTypeToUnit(MapActorType.Pilgrim));

            var actor = new MapActor
            {
                actorId = 9,
                type = MapActorType.Caravan,
                actorName = "南队",
                ownerRealmId = 2,
                currentTile = 5,
            };
            var unit = MapUnit.FromMapActor(actor);
            Assert.AreEqual(UnitSourceKind.MapActor, unit.source);
            Assert.AreEqual(9, unit.sourceId);
            Assert.AreEqual(TransportUnitType.Caravan, unit.type);
        }

        [Test]
        public void Unit_CanConvertTypes()
        {
            var unit = MapUnit.FromArmy(new Army { armyId = 1, commanderId = 1 });
            Assert.IsTrue(unit.ConvertTo(TransportUnitType.Warband), "军队/战团互转");
            Assert.IsTrue(unit.ConvertTo(TransportUnitType.MercenaryCompany), "战团/雇佣兵互转");
            Assert.IsFalse(unit.ConvertTo(TransportUnitType.Settlers), "军队不能直接变移民队");
            Assert.AreEqual(TransportUnitType.MercenaryCompany, unit.transport.type, "转换同步到载具组件");
        }

        [Test]
        public void TerrainConditions_ReadRawFields()
        {
            var tile = new TileData
            {
                isLand = true, annualTemp = 40f, airHumidityPct = 10f,
                soilHumidityPct = 5f, annualPrecipMm = 50f,
                elevation01 = 0.3f, slopeDegree = 5f, isRiver = true,
                riverWidthMeters = 120f, riverDepthMeters = 3f, riverCurrentMps = 1.5f,
            };
            var c = TerrainConditions.FromTile(tile);
            Assert.AreEqual(40f, c.TemperatureC, 0.01f);
            Assert.AreEqual(10f, c.HumidityPct, 0.01f);
            Assert.AreEqual(5f, c.SoilHumidityPct, 0.01f);
            Assert.AreEqual(0.3f, c.Elevation01, 0.01f);
            Assert.AreEqual(5f, c.SlopeDegrees, 0.01f);
            Assert.IsTrue(c.IsLand);
            Assert.IsTrue(c.IsRiver);
            Assert.IsTrue(c.IsDeepRiver, "120m 宽 / 3m 深是深河");
            Assert.Greater(c.GroundBearing(), 0f);

            var water = new TileData { isLand = false };
            Assert.AreEqual(0f, TerrainConditions.FromTile(water).GroundBearing(), 0.001f);
        }

        [Test]
        public void Registry_RegisterBySource_AndRemove()
        {
            var army = new Army { armyId = 3, commanderId = 1 };
            MapUnitRegistry.RegisterArmy(army);
            Assert.AreEqual(1, MapUnitRegistry.Count);
            Assert.IsTrue(MapUnitRegistry.RemoveBySource(UnitSourceKind.Army, 3));
            Assert.AreEqual(0, MapUnitRegistry.Count);

            var actor = new MapActor { actorId = 4, type = MapActorType.Caravan };
            MapUnitRegistry.RegisterActor(actor);
            Assert.AreEqual(1, MapUnitRegistry.Count);
            Assert.AreEqual(TransportUnitType.Caravan, MapUnitRegistry.All.First().type);
        }

        [Test]
        public void Registry_AddAndSync()
        {
            var unit = MapUnit.FromArmy(new Army { armyId = 2, commanderId = 1 });
            MapUnitRegistry.Add(unit);
            Assert.AreEqual(1, MapUnitRegistry.Count);
            Assert.IsTrue(MapUnitRegistry.TryGet(unit.unitId, out var found));
            Assert.AreEqual(2, found.sourceId);
        }
    }
}
