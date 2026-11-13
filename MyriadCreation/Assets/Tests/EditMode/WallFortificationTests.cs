using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Infrastructure.Save;
using MyriadCreation.Save.Contracts;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 城防等级 0–15：tier×材质工艺、旧档映射、多城墙块聚合、横向段、层数上限、材质影响。
    /// </summary>
    public class WallFortificationTests
    {
        private static SettlementData City(int garrison = 1000, float fortification = 10f)
        {
            return new SettlementData
            {
                siteMode = MyriadCreation.Core.Enums.SiteMode.SedentarySettlement,
                settlementLevel = MyriadCreation.Core.Enums.SettlementLevel.LevelIII,
                settlementForm = MyriadCreation.Core.Enums.SettlementForm.City,
                cityForm = CityForm.Circular,
                population = 10000,
                garrison = garrison,
                fortification = fortification,
            };
        }

        [Test]
        public void Level_IsTierTimesThreePlusGrade()
        {
            Assert.AreEqual(1, FortificationLevels.Compose(1, 1));
            Assert.AreEqual(3, FortificationLevels.Compose(1, 3));
            Assert.AreEqual(4, FortificationLevels.Compose(2, 1));
            Assert.AreEqual(7, FortificationLevels.Compose(3, 1));
            Assert.AreEqual(15, FortificationLevels.Compose(5, 3));

            Assert.AreEqual(0, FortificationLevels.TierOf(0));
            Assert.AreEqual(2, FortificationLevels.TierOf(4));
            Assert.AreEqual(5, FortificationLevels.TierOf(15));
            Assert.AreEqual(1, FortificationLevels.GradeOf(4));
            Assert.AreEqual(3, FortificationLevels.GradeOf(15));

            Assert.AreEqual(0, FortificationLevels.Clamp(-5));
            Assert.AreEqual(15, FortificationLevels.Clamp(99));
        }

        [Test]
        public void LegacyWallLevel_MapsToNewLevels()
        {
            Assert.AreEqual(0, FortificationLevels.FromLegacy(WallLevel.None));
            Assert.AreEqual(1, FortificationLevels.FromLegacy(WallLevel.Palisade));
            Assert.AreEqual(4, FortificationLevels.FromLegacy(WallLevel.EarthenRampart));
            Assert.AreEqual(5, FortificationLevels.FromLegacy(WallLevel.StoneWall));
            Assert.AreEqual(7, FortificationLevels.FromLegacy(WallLevel.FortifiedWall));
            Assert.AreEqual(10, FortificationLevels.FromLegacy(WallLevel.StarFortification));
            Assert.AreEqual(13, FortificationLevels.FromLegacy(WallLevel.MegaFortification));
        }

        [Test]
        public void MultipleWallBlocks_AggregateLengthAndBlocks()
        {
            var city = City();
            var outer = city.AddBlock(SettlementBlockKind.Wall, null, FortificationLevels.StoneWall);
            outer.length = 1000f;
            var inner = city.AddBlock(SettlementBlockKind.Wall, null, FortificationLevels.FortifiedWall);
            inner.length = 500f;

            var snap = WallDefense.Evaluate(city);

            Assert.IsTrue(snap.hasWall);
            Assert.AreEqual(2, snap.wallBlockCount);
            Assert.AreEqual(1500f, snap.wallLengthMeters, 0.01f);
            Assert.AreEqual(FortificationLevels.FortifiedWall, snap.fortificationLevel);
        }

        [Test]
        public void WallSegments_SumLengthAndTowerCount()
        {
            var city = City();
            var wall = city.AddBlock(SettlementBlockKind.Wall, null, FortificationLevels.StoneWall);
            wall.length = 0f;
            wall.wallSegments.Add(new WallSegment { length = 300f, condition = 1f, towerCount = 2 });
            wall.wallSegments.Add(new WallSegment { length = 200f, condition = 0.5f, towerCount = 3, hasGate = true });

            var snap = WallDefense.Evaluate(city);

            Assert.AreEqual(500f, snap.wallLengthMeters, 0.01f);
            Assert.AreEqual(2, snap.wallSegmentCount);
            Assert.GreaterOrEqual(snap.towerCount, 5);
            Assert.Less(snap.averageCondition, 1f);
        }

        [Test]
        public void WallLayerCount_IsClampedToThree()
        {
            var city = City();
            var wall = city.AddBlock(SettlementBlockKind.Wall, null, FortificationLevels.StoneWall);
            wall.length = 1000f;
            wall.wallLayerCount = 9;

            var snap = WallDefense.Evaluate(city);

            Assert.AreEqual(3, snap.wallLayerCount);
        }

        [Test]
        public void MaterialAndGrade_AffectEffectiveDefense()
        {
            var wood = City();
            var woodWall = wood.AddBlock(SettlementBlockKind.Wall, null, FortificationLevels.FortifiedWall);
            woodWall.length = 1000f;
            woodWall.wallMaterial = WallMaterial.Palisade;
            woodWall.wallMaterialGrade = 1;

            var stone = City();
            var stoneWall = stone.AddBlock(SettlementBlockKind.Wall, null, FortificationLevels.FortifiedWall);
            stoneWall.length = 1000f;
            stoneWall.wallMaterial = WallMaterial.Stone;
            stoneWall.wallMaterialGrade = 3;

            var woodSnap = WallDefense.Evaluate(wood);
            var stoneSnap = WallDefense.Evaluate(stone);

            Assert.Greater(stoneSnap.effectiveFortification, woodSnap.effectiveFortification);
        }

        [Test]
        public void SaveData_RoundTripsMaterialGradeAndSegments()
        {
            var wall = new SettlementBlock(SettlementBlockKind.Wall, FortificationLevels.FortifiedWall)
            {
                wallMaterial = WallMaterial.Brick,
                wallMaterialGrade = 3,
                wallLayerCount = 2,
                length = 700f,
            };
            wall.wallSegments.Add(new WallSegment { length = 400f, condition = 0.8f, towerCount = 2, hasGate = true, moatLevel = 1 });
            wall.wallSegments.Add(new WallSegment { length = 300f, condition = 0.9f, towerCount = 1, hasGate = false, moatLevel = 2 });

            var dto = SettlementBlockSaveMapping.From(wall);
            var restored = dto.ToBlock();

            Assert.AreEqual(WallMaterial.Brick, restored.wallMaterial);
            Assert.AreEqual(3, restored.wallMaterialGrade);
            Assert.AreEqual(2, restored.wallLayerCount);
            Assert.AreEqual(2, restored.wallSegments.Count);
            Assert.AreEqual(400f, restored.wallSegments[0].length, 0.01f);
            Assert.IsTrue(restored.wallSegments[0].hasGate);
        }

        [Test]
        public void LegacySaveData_MapsOldWallLevel()
        {
            var dto = new SettlementBlockSaveData
            {
                kind = (int)SettlementBlockKind.Wall,
                level = (int)WallLevel.StoneWall,
                wallLayerCount = 1,
                wallRole = (int)WallRole.Primary,
                wallLayout = (int)WallLayout.Ring,
                wallDataVersion = 0,
            };

            var block = dto.ToBlock();

            Assert.AreEqual(FortificationLevels.StoneWall, block.level);
            Assert.AreEqual(FortificationLevels.GradeOf(FortificationLevels.StoneWall), block.wallMaterialGrade);
            Assert.AreEqual(FortificationLevels.DefaultMaterialForLevel(FortificationLevels.StoneWall), block.wallMaterial);
        }
    }
}
