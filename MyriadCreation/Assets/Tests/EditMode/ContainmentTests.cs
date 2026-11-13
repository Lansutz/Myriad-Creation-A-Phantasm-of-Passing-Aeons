using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Infrastructure.Save;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World;
using MyriadCreation.World.Generation;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// D36 人物容纳落在建筑实例 SettlementBlock：容量、占用者对账、块级位置与存档往返。
    /// 领主地牢只是 Building 块 + Confinement 语义，不是 FacilityType，也不是平行监狱系统。
    /// </summary>
    public class ContainmentTests
    {
        private const string SaveName = "__containment_test";
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _objects)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _objects.Clear();
            SaveSystem.DeleteSave(SaveName);
        }

        private GameWorld NewWorld()
        {
            var go = new GameObject("ContainmentWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = 48;
            world.mapHeight = 24;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(37);
            return world;
        }

        private static SettlementData FirstSettlement(GameWorld world)
        {
            foreach (var settlement in world.settlements.Values) return settlement;
            return null;
        }

        private static CharacterData FirstAliveCharacter(GameWorld world)
        {
            foreach (var character in world.GetCharacterManager().GetAllCharacters().Values)
            {
                if (character != null && character.isAlive) return character;
            }
            return null;
        }

        private static SettlementBlock AddDungeon(SettlementData settlement, int capacity)
        {
            SettlementBlock block = settlement.AddBlock(SettlementBlockKind.Building, null, 1, SettlementBlockSubtype.None);
            block.displayName = "领主地牢";
            block.containmentKind = ContainmentKind.Confinement;
            block.containmentCapacity = capacity;
            block.containmentAccess = ContainmentAccess.Sealed;
            return block;
        }

        [Test]
        public void BlockCapacity_IsEnforcedAndReusable()
        {
            var block = new SettlementBlock(SettlementBlockKind.Building, 1)
            {
                containmentKind = ContainmentKind.Confinement,
                containmentCapacity = 1,
                containmentAccess = ContainmentAccess.Sealed
            };

            Assert.IsTrue(block.TryAddOccupant(10), "空位应可收押");
            Assert.IsFalse(block.TryAddOccupant(11), "满员后不得再收押");
            Assert.IsFalse(block.ContainsOccupant(11));
            Assert.IsTrue(block.RemoveOccupant(10), "释放应成功");
            Assert.IsTrue(block.TryAddOccupant(11), "释放后容量应复用");
            Assert.IsFalse(block.TryAddOccupant(11), "同一人不得重复占用");
        }

        [Test]
        public void EnterAndLeaveBlock_KeepsAnchorAndOccupancyInSync()
        {
            var world = NewWorld();
            SettlementData settlement = FirstSettlement(world);
            CharacterData character = FirstAliveCharacter(world);
            Assert.IsNotNull(settlement, "前置：应有聚落");
            Assert.IsNotNull(character, "前置：应有存活人物");
            SettlementBlock block = AddDungeon(settlement, 2);

            Assert.IsTrue(CharacterLocationService.EnterBlock(world, character.characterId, settlement.anchorId, settlement.tileIndex, block.blockId));
            Assert.AreEqual(settlement.anchorId, character.currentSettlementAnchorId, "聚落锚点仍是权威位置");
            Assert.AreEqual(block.blockId, character.currentBlockId, "块级位置应写入人物");
            Assert.IsTrue(block.ContainsOccupant(character.characterId), "占用者对账应同步");

            Assert.IsTrue(CharacterLocationService.LeaveBlock(world, character.characterId));
            Assert.AreEqual("", character.currentBlockId);
            Assert.IsFalse(block.ContainsOccupant(character.characterId));
        }

        [Test]
        public void EnterFullBlock_IsRejectedWithoutChangingLocation()
        {
            var world = NewWorld();
            SettlementData settlement = FirstSettlement(world);
            Assert.IsNotNull(settlement, "前置：应有聚落");
            CharacterData first = null;
            CharacterData second = null;
            foreach (var character in world.GetCharacterManager().GetAllCharacters().Values)
            {
                if (character == null || !character.isAlive) continue;
                if (first == null) first = character;
                else { second = character; break; }
            }
            Assert.IsNotNull(second, "前置：应有两个存活人物");
            SettlementBlock block = AddDungeon(settlement, 1);

            Assert.IsTrue(CharacterLocationService.EnterBlock(world, first.characterId, settlement.anchorId, settlement.tileIndex, block.blockId));
            Assert.IsFalse(CharacterLocationService.EnterBlock(world, second.characterId, settlement.anchorId, settlement.tileIndex, block.blockId), "满员应拒绝");
            Assert.AreEqual("", second.currentBlockId, "拒绝进入不得改位置");
        }

        [Test]
        public void ContainmentAndBlockLocation_RoundTripThroughSave()
        {
            var world = NewWorld();
            SettlementData settlement = FirstSettlement(world);
            CharacterData character = FirstAliveCharacter(world);
            Assert.IsNotNull(settlement, "前置：应有聚落");
            Assert.IsNotNull(character, "前置：应有存活人物");
            SettlementBlock block = AddDungeon(settlement, 2);
            Assert.IsTrue(CharacterLocationService.EnterBlock(world, character.characterId, settlement.anchorId, settlement.tileIndex, block.blockId));

            Assert.IsTrue(SaveSystem.SaveGame(world, SaveName), "存盘应成功");
            var loaded = SaveSystem.LoadGame(SaveName);
            Assert.IsNotNull(loaded, "读档应返回世界");
            _objects.Add(loaded.gameObject);

            Assert.IsTrue(loaded.settlements.TryGetValue(settlement.anchorId, out SettlementData loadedSettlement), "聚落应还原");
            SettlementBlock loadedBlock = loadedSettlement.FindBlock(block.blockId);
            Assert.IsNotNull(loadedBlock, "容纳块应还原");
            Assert.AreEqual(ContainmentKind.Confinement, loadedBlock.containmentKind);
            Assert.AreEqual(ContainmentAccess.Sealed, loadedBlock.containmentAccess);
            Assert.AreEqual(2, loadedBlock.containmentCapacity);
            Assert.IsTrue(loadedBlock.ContainsOccupant(character.characterId), "占用者应还原");

            CharacterData loadedCharacter = loaded.GetCharacterManager().GetCharacter(character.characterId);
            Assert.IsNotNull(loadedCharacter, "人物应还原");
            Assert.AreEqual(block.blockId, loadedCharacter.currentBlockId, "块级位置应还原");
            Assert.AreEqual(settlement.anchorId, loadedCharacter.currentSettlementAnchorId, "锚点应还原");
        }
    }
}
