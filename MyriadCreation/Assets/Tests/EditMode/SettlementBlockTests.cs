using NUnit.Framework;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 栖居点块树：区划只有大城市才有，建筑/功能块可挂区划也可直接挂栖居点；
    /// 城墙是“另一种区划”（带子块/空间字段），块可嵌套。
    /// </summary>
    public class SettlementBlockTests
    {
        [Test]
        public void SmallSettlement_CanHaveDirectBlocks_WithoutDistricts()
        {
            var village = new SettlementData();
            village.AddBlock(SettlementBlockKind.Building, null, 1, SettlementBlockSubtype.None, buildingDefId: 7);
            village.AddBlock(SettlementBlockKind.Field, null, 1);

            Assert.AreEqual(2, village.blocks.Count);
            Assert.IsTrue(village.HasBlock(SettlementBlockKind.Building));
            Assert.IsTrue(village.HasBlock(SettlementBlockKind.Field));
            Assert.IsFalse(village.HasBlock(SettlementBlockKind.District), "小聚落没有区划系统");
        }

        [Test]
        public void LargeCity_DistrictContainsBlocksAndBuildings()
        {
            var city = new SettlementData();
            var district = city.AddBlock(SettlementBlockKind.District, null, 1);
            district.districtType = CityDistrictType.Commercial;
            district.hasSpatialData = true;
            district.areaRatio = 0.25f;
            district.populationRatio = 0.3f;

            var market = city.AddBlock(SettlementBlockKind.Market, district, 2);
            city.AddBlock(SettlementBlockKind.Building, market, 1,
                SettlementBlockSubtype.Stall, buildingDefId: 11);

            Assert.AreEqual(1, city.blocks.Count, "根层只有区划块");
            Assert.AreEqual(CityDistrictType.Commercial, city.blocks[0].districtType);
            Assert.AreEqual(0.25f, city.blocks[0].areaRatio);
            Assert.IsTrue(city.HasBlock(SettlementBlockKind.Market));
            Assert.IsTrue(city.HasBlock(SettlementBlockKind.Building));
            Assert.IsNotNull(city.GetBlock(SettlementBlockKind.Building));
        }

        [Test]
        public void Wall_IsADistrictLikeBlock_WithChildSegments()
        {
            var city = new SettlementData();
            var wall = city.AddBlock(SettlementBlockKind.Wall, null, 3);
            wall.hasSpatialData = true;
            wall.length = 120f;
            wall.AddChild(SettlementBlockKind.Building, 1, SettlementBlockSubtype.WallSegment);
            wall.AddChild(SettlementBlockKind.Building, 1, SettlementBlockSubtype.Gate);
            wall.AddChild(SettlementBlockKind.Building, 2, SettlementBlockSubtype.Tower);

            var found = city.GetBlock(SettlementBlockKind.Wall);
            Assert.AreEqual(3, found.level);
            Assert.AreEqual(120f, found.length);
            Assert.AreEqual(3, found.children.Count);
            Assert.IsTrue(found.children.Exists(c => c.subtype == SettlementBlockSubtype.Tower));
        }

        [Test]
        public void LegacyMigration_CreatesRootBlocksFromBooleans()
        {
            var settlement = new SettlementData
            {
                hasMarket = true,
                hasTemple = true,
                hasUniversity = true,
                isPort = true,
            };
            settlement.EnsureBlocksFromLegacy();
            Assert.IsTrue(settlement.HasBlock(SettlementBlockKind.Market));
            Assert.IsTrue(settlement.HasBlock(SettlementBlockKind.Temple));
            Assert.IsTrue(settlement.HasBlock(SettlementBlockKind.University));
            Assert.IsTrue(settlement.HasBlock(SettlementBlockKind.Port));

            settlement.EnsureBlocksFromLegacy();
            Assert.AreEqual(4, settlement.blocks.Count, "迁移必须幂等");
        }

        [Test]
        public void LegacyWalls_MigrateFacilitiesToChildBlocks()
        {
            var settlement = new SettlementData { fortificationLevel = FortificationLevels.StoneWall };
            settlement.facilities.Add(new FacilityData { facilityType = FacilityType.Gate });
            settlement.facilities.Add(new FacilityData { facilityType = FacilityType.Wall });
            settlement.facilities.Add(new FacilityData { facilityType = FacilityType.Trench });

            settlement.EnsureBlocksFromLegacy();
            var wall = settlement.GetBlock(SettlementBlockKind.Wall);
            Assert.IsNotNull(wall);
            Assert.AreEqual(FortificationLevels.StoneWall, wall.level);
            Assert.AreEqual(2, wall.children.Count);
            Assert.IsTrue(wall.children.Exists(c => c.subtype == SettlementBlockSubtype.Gate));
            Assert.IsTrue(wall.children.Exists(c => c.subtype == SettlementBlockSubtype.WallSegment));
            Assert.AreEqual(1, wall.wallMoatLevel, "旧 Trench 设施迁移为城墙自身的护城河属性");
        }

        [Test]
        public void LegacyDistricts_BecomeDistrictBlocksWithFunctionalChildren()
        {
            var settlement = new SettlementData();
            settlement.districts.Add(new CityDistrict { districtType = CityDistrictType.Commercial });
            settlement.districts.Add(new CityDistrict { districtType = CityDistrictType.Cultural });

            settlement.EnsureBlocksFromLegacy();

            var districts = settlement.blocks.FindAll(b => b.kind == SettlementBlockKind.District);
            Assert.AreEqual(2, districts.Count);
            Assert.IsTrue(settlement.HasBlock(SettlementBlockKind.Market));
            Assert.IsTrue(settlement.HasBlock(SettlementBlockKind.University));
        }

        [Test]
        public void MultipleWallBlocks_CanCoexist()
        {
            var city = new SettlementData();
            var outer = city.AddBlock(SettlementBlockKind.Wall, null, 3);
            outer.wallLayerCount = 2;
            outer.wallLayout = WallLayout.Ring;
            outer.hasSpatialData = true;

            var inner = city.AddBlock(SettlementBlockKind.Wall, null, 2);
            inner.wallLayerCount = 1;
            inner.wallLayout = WallLayout.Ring;

            var palace = city.AddBlock(SettlementBlockKind.Wall, null, 1);
            palace.wallLayerCount = 3;
            palace.wallLayout = WallLayout.MultiCircuit;

            var walls = city.GetBlocks(SettlementBlockKind.Wall);
            Assert.AreEqual(3, walls.Count, "同一栖居点可以有多道城墙");
            Assert.IsTrue(walls.Exists(w => w.wallLayerCount == 2));
            Assert.IsTrue(walls.Exists(w => w.wallLayerCount == 3));
            Assert.IsTrue(walls.Exists(w => w.wallLayout == WallLayout.MultiCircuit));
            Assert.AreSame(outer, city.GetBlock(SettlementBlockKind.Wall), "GetBlock 返回第一道");
        }

        [Test]
        public void LegacyWallMigration_KeepsManuallyAddedWalls()
        {
            var city = new SettlementData { fortificationLevel = FortificationLevels.StoneWall };
            city.EnsureBlocksFromLegacy();

            var legacyWall = city.GetBlock(SettlementBlockKind.Wall);
            Assert.AreEqual(WallLayout.Ring, legacyWall.wallLayout, "旧墙迁移默认环城布局");
            Assert.AreEqual(1, legacyWall.wallLayerCount);
            Assert.AreEqual(WallRole.Primary, legacyWall.wallRole);
            Assert.AreSame(legacyWall, city.GetPrimaryWall());

            var extra = city.AddBlock(SettlementBlockKind.Wall, null, 2);
            extra.wallLayerCount = 2;
            city.EnsureBlocksFromLegacy();

            Assert.AreEqual(2, city.GetBlocks(SettlementBlockKind.Wall).Count,
                "再次迁移不应吞掉或重复已有城墙块");
        }

        [Test]
        public void ConstantinopleWalls_AreIndependentWallBlocks()
        {
            var city = new SettlementData();

            var severan = city.AddBlock(SettlementBlockKind.Wall, null, 2);
            severan.displayName = "塞维鲁城墙";
            severan.wallLayout = WallLayout.Ring;
            severan.wallLayerCount = 1;
            severan.wallRole = WallRole.Inner;

            var constantinian = city.AddBlock(SettlementBlockKind.Wall, null, 3);
            constantinian.displayName = "君士坦丁城墙";
            constantinian.wallLayout = WallLayout.Ring;
            constantinian.wallLayerCount = 1;
            constantinian.wallRole = WallRole.Inner;

            var theodosian = city.AddBlock(SettlementBlockKind.Wall, null, 4);
            theodosian.displayName = "狄奥多西城墙";
            theodosian.wallLayout = WallLayout.Ring;
            theodosian.wallLayerCount = 2;   // 双层城墙
            theodosian.wallMoatLevel = 1;    // 护城河
            theodosian.wallRole = WallRole.Primary;
            theodosian.hasSpatialData = true;
            theodosian.length = 5600f;

            var walls = city.GetBlocks(SettlementBlockKind.Wall);
            Assert.AreEqual(3, walls.Count, "同一时期最多可同时存在三座城墙");
            Assert.IsTrue(walls.Exists(w => w.displayName == "塞维鲁城墙"));
            Assert.IsTrue(walls.Exists(w => w.displayName == "君士坦丁城墙"));

            var found = walls.Find(w => w.displayName == "狄奥多西城墙");
            Assert.IsNotNull(found);
            Assert.AreEqual(2, found.wallLayerCount, "狄奥多西城墙是双层墙");
            Assert.AreEqual(1, found.wallMoatLevel, "狄奥多西城墙带护城河");
            Assert.AreEqual(5600f, found.length);
            // 各城墙是独立块，不需要彼此相邻（可以隔得很远）
            Assert.AreNotSame(severan, constantinian);
            Assert.AreNotSame(constantinian, found);
            Assert.AreSame(found, city.GetPrimaryWall(), "狄奥多西城墙是当前主城墙");
        }
    }
}
