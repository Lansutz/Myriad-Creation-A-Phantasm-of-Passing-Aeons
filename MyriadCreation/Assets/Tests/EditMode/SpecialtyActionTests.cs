using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P11.1：专精节点必须真正改变可执行动作，而不只是可查询的标签。
    /// field_survey → 揭示地图知识；night_raid → 相邻夜袭打乱组织。
    /// </summary>
    public class SpecialtyActionTests
    {
        private GameObject _go;
        private GameWorld _world;

        [TearDown]
        public void TearDown()
        {
            DetectionContactService.CommanderResolver = null;
            if (_go != null) Object.DestroyImmediate(_go);
        }

        private GameWorld NewWorld()
        {
            _go = new GameObject("SpecialtyActionWorld");
            _world = _go.AddComponent<GameWorld>();
            _world.mapWidth = 16;
            _world.mapHeight = 8;
            _world.SetupOptions = WorldSetupOptions.CreateStandard();
            _world.InitializeWorld();
            _world.GenerateTerrain(11);
            return _world;
        }

        private static void RegisterCharacter(CharacterManager manager, CharacterData character)
        {
            FieldInfo field = typeof(CharacterManager).GetField("_characters", BindingFlags.Instance | BindingFlags.NonPublic);
            var characters = field?.GetValue(manager) as Dictionary<int, CharacterData>;
            Assert.IsNotNull(characters, "测试脚手架：角色字典应存在");
            characters[character.characterId] = character;
        }

        [Test]
        public void Survey_RequiresPerkAndRevealsTile()
        {
            GameWorld world = NewWorld();
            var surveyor = new CharacterData { characterId = 77 };
            RegisterCharacter(world.Characters, surveyor);

            int unknown = -1;
            for (int i = 0; i < world.tiles.Length; i++)
            {
                if (!world.Knowledge.IsKnown(999, i)) { unknown = i; break; }
            }
            Assert.GreaterOrEqual(unknown, 0, "测试世界应有未揭示地块");

            Assert.IsFalse(world.TrySurveyTile(999, 77, unknown), "未解锁 field_survey 不得踏勘");
            Assert.IsFalse(world.Knowledge.IsKnown(999, unknown));

            surveyor.unlockedPerks.Add("field_survey");
            Assert.IsTrue(world.TrySurveyTile(999, 77, unknown), "解锁后应揭示目标地块");
            Assert.IsTrue(world.Knowledge.IsKnown(999, unknown));
            Assert.IsFalse(world.TrySurveyTile(999, 77, unknown), "重复踏勘不重复计数");
        }

        [Test]
        public void NightRaid_RequiresPerkAndAdjacency()
        {
            var commander = new CharacterData { characterId = 5, military = 80f };
            DetectionContactService.CommanderResolver = id => id == 5 ? commander : null;

            var raider = new Army { armyId = 1, ownerRealmId = 1, currentTileIndex = 0, commanderId = 5, organization = 100f };
            var target = new Army { armyId = 2, ownerRealmId = 2, currentTileIndex = 1, organization = 100f, morale = 100f };
            var tiles = new TileData[100];
            var unitDefs = new Dictionary<int, UnitDef>();
            var combat = new CombatManager(tiles, unitDefs, null, null, 10);

            Assert.IsFalse(combat.TryNightRaid(raider, target, 10), "未解锁 night_raid 不得夜袭");
            Assert.AreEqual(100f, target.organization);

            commander.unlockedPerks.Add("night_raid");
            Assert.IsTrue(combat.TryNightRaid(raider, target, 10), "相邻地块应可夜袭");
            Assert.Less(target.organization, 100f, "夜袭应打乱对方组织");
            Assert.Less(target.morale, 100f);
        }

        [Test]
        public void NightRaid_RejectsNonAdjacent()
        {
            var commander = new CharacterData { characterId = 6 };
            commander.unlockedPerks.Add("night_raid");
            DetectionContactService.CommanderResolver = id => id == 6 ? commander : null;

            var raider = new Army { armyId = 1, currentTileIndex = 0, commanderId = 6 };
            var target = new Army { armyId = 2, currentTileIndex = 2, organization = 100f };
            var combat = new CombatManager(new TileData[100], new Dictionary<int, UnitDef>(), null, null, 10);

            Assert.IsFalse(combat.TryNightRaid(raider, target, 10), "隔一格不是夜袭");
            Assert.AreEqual(100f, target.organization);
        }
    }
}
