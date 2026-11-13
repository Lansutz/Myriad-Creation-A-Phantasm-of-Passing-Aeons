using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Infrastructure.Save;
using MyriadCreation.Save.Contracts;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>栖居点块树存档：多道城墙/名字/层数/护城河/角色、区划嵌套、旧档回退。</summary>
    public class SettlementBlockSaveTests
    {
        private static SettlementData BuildCity()
        {
            var city = new SettlementData();
            var theodosian = city.AddBlock(SettlementBlockKind.Wall, null, 4);
            theodosian.displayName = "狄奥多西城墙";
            theodosian.wallLayerCount = 2;
            theodosian.wallMoatLevel = 1;
            theodosian.wallRole = WallRole.Primary;
            theodosian.wallLayout = WallLayout.Ring;
            theodosian.length = 5600f;
            theodosian.AddChild(SettlementBlockKind.Building, 2, SettlementBlockSubtype.Tower);

            var constantinian = city.AddBlock(SettlementBlockKind.Wall, null, 3);
            constantinian.displayName = "君士坦丁城墙";
            constantinian.wallRole = WallRole.Inner;

            var district = city.AddBlock(SettlementBlockKind.District, null, 1);
            district.districtType = CityDistrictType.Commercial;
            district.hasSpatialData = true;
            district.areaRatio = 0.3f;
            var market = city.AddBlock(SettlementBlockKind.Market, district, 2);
            city.AddBlock(SettlementBlockKind.Building, market, 1,
                SettlementBlockSubtype.Stall, buildingDefId: 77);
            return city;
        }

        [Test]
        public void RoundTrip_PreservesMultipleWallsAndNestedBlocks()
        {
            var city = BuildCity();
            var save = SettlementBlockSaveMapping.CaptureAll(city.blocks);
            Assert.IsNotNull(save);
            // D30 扁平化：整棵树（根 3 + 塔 + 市场 + 摊）都进同一张表，层级靠 parent_block_id
            Assert.AreEqual(6, save.Length, "扁平化后整棵树是一条表");

            var restored = new SettlementData();
            SettlementBlockSaveMapping.RestoreAll(restored.blocks, save);

            var walls = restored.GetBlocks(SettlementBlockKind.Wall);
            Assert.AreEqual(2, walls.Count, "多道城墙应持久化");
            var theodosian = walls.Find(w => w.displayName == "狄奥多西城墙");
            Assert.IsNotNull(theodosian);
            Assert.AreEqual(2, theodosian.wallLayerCount);
            Assert.AreEqual(1, theodosian.wallMoatLevel);
            Assert.AreEqual(WallRole.Primary, theodosian.wallRole);
            Assert.AreEqual(WallLayout.Ring, theodosian.wallLayout);
            Assert.AreEqual(5600f, theodosian.length);
            Assert.AreEqual(1, theodosian.children.Count);
            Assert.AreEqual(SettlementBlockSubtype.Tower, theodosian.children[0].subtype);

            Assert.IsTrue(restored.HasBlock(SettlementBlockKind.Market), "区划内的市场子块应持久化");
            Assert.IsTrue(restored.HasBlock(SettlementBlockKind.Building));
            var districtBlock = restored.GetBlock(SettlementBlockKind.District);
            Assert.AreEqual(CityDistrictType.Commercial, districtBlock.districtType);
            Assert.AreEqual(0.3f, districtBlock.areaRatio);
        }

        [Test]
        public void SaveData_WithNoBlocks_FallsBackToLegacyMigration()
        {
            var settlement = new SettlementData
            {
                hasMarket = true,
                fortificationLevel = FortificationLevels.StoneWall,
            };
            // 旧档：blocks 为 null
            SettlementBlockSaveMapping.RestoreAll(settlement.blocks, null);
            Assert.AreEqual(0, settlement.blocks.Count);
            Assert.IsFalse(settlement.HasBlock(SettlementBlockKind.Market));

            settlement.EnsureBlocksFromLegacy();
            Assert.IsTrue(settlement.HasBlock(SettlementBlockKind.Market));
            Assert.IsTrue(settlement.HasBlock(SettlementBlockKind.Wall));
        }

        [Test]
        public void SingleBlockDto_RoundTripsOwnFieldsOnly()
        {
            // D30 扁平化后：单个 DTO 不再承载子树（children 恒 null），
            // 层级由 CaptureAll/RestoreAll 用 parent_block_id 重建——见 RoundTrip_PreservesMultipleWallsAndNestedBlocks。
            var wall = new SettlementBlock(SettlementBlockKind.Wall, 2)
            {
                displayName = "塞维鲁城墙",
                wallRole = WallRole.Inner,
                wallMoatLevel = 2,
            };
            wall.AddChild(SettlementBlockKind.Building, 1, SettlementBlockSubtype.Gate);
            wall.AddChild(SettlementBlockKind.Building, 1, SettlementBlockSubtype.WallSegment);

            var dto = SettlementBlockSaveMapping.From(wall);
            var restored = dto.ToBlock();

            Assert.AreEqual(wall.blockId, restored.blockId);
            Assert.AreEqual("塞维鲁城墙", restored.displayName);
            Assert.AreEqual(WallRole.Inner, restored.wallRole);
            Assert.AreEqual(2, restored.wallMoatLevel);
            Assert.IsNull(dto.children, "扁平化后单块 DTO 不写 children");
            Assert.AreEqual(0, restored.children.Count, "子树由 RestoreAll 按父指针重建，不在这里");

            // 树要完整往返，走表级 API
            var save = SettlementBlockSaveMapping.CaptureAll(new List<SettlementBlock> { wall });
            Assert.AreEqual(3, save.Length, "根 + Gate + WallSegment 共 3 条平铺");
            var container = new SettlementData();
            SettlementBlockSaveMapping.RestoreAll(container.blocks, save);
            Assert.AreEqual(1, container.blocks.Count, "只有一个根");
            Assert.AreEqual(2, container.blocks[0].children.Count);
        }

        [Test]
        public void EmptyBlocks_CaptureReturnsNull()
        {
            Assert.IsNull(SettlementBlockSaveMapping.CaptureAll(new List<SettlementBlock>()));
            Assert.IsNull(SettlementBlockSaveMapping.CaptureAll(null));
        }
    }
}
