using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Simulation.Faith;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P1.6 婚姻入口：异性/成年/未婚条件、跨政权联姻外交、失败原因。</summary>
    public class MarriageTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _objects)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _objects.Clear();
        }

        private GameWorld NewWorld()
        {
            var go = new GameObject("MarriageWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = 48;
            world.mapHeight = 24;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(89);
            return world;
        }

        private static CharacterData CreateAdult(GameWorld world, int realmId, string name, bool male)
        {
            RealmData realm = world.realms[realmId];
            CharacterData character = world.GetCharacterManager().CreateCharacter(
                name, "氏", 20, male, realm.primaryCultureId, 0, realm.stateReligionId, CharacterRole.Noble);
            character.realmId = realmId;
            return character;
        }

        [Test]
        public void ArrangesMarriage_BetweenEligibleAdults()
        {
            var world = NewWorld();
            int realmId = -1;
            foreach (int key in world.realms.Keys) { realmId = key; break; }
            CharacterData male = CreateAdult(world, realmId, "甲", true);
            CharacterData female = CreateAdult(world, realmId, "乙", false);

            bool ok = world.TryArrangeMarriage(male.characterId, female.characterId, out string reason);

            Assert.IsTrue(ok, reason);
            Assert.AreEqual(female.characterId, male.spouseId);
            Assert.AreEqual(male.characterId, female.spouseId);
        }

        [Test]
        public void SameSexMarriage_IsRejected()
        {
            var world = NewWorld();
            int realmId = -1;
            foreach (int key in world.realms.Keys) { realmId = key; break; }
            CharacterData a = CreateAdult(world, realmId, "甲", true);
            CharacterData b = CreateAdult(world, realmId, "乙", true);

            Assert.IsFalse(world.TryArrangeMarriage(a.characterId, b.characterId, out string reason));
            Assert.AreEqual("不满足婚姻条件", reason);
        }

        [Test]
        public void SameSexMarriage_AllowedWhenDoctrineAccepts()
        {
            var world = NewWorld();
            int realmId = -1;
            foreach (int key in world.realms.Keys) { realmId = key; break; }
            RealmData realm = world.realms[realmId];
            FaithSystem faith = world.GetFaithSystem(realm.stateReligionId);
            Assert.IsNotNull(faith, "前置：政权应有国教");
            Assert.IsTrue(ContentResolvers.Religions.TryGet(faith.religionId, out ReligionDef religion), "前置：应有宗教定义");
            var original = new List<string>(religion.selectedDoctrines);
            try
            {
                religion.selectedDoctrines.Add("doctrine_homosexuality_accepted");
                CharacterData a = CreateAdult(world, realmId, "甲", true);
                CharacterData b = CreateAdult(world, realmId, "乙", true);

                Assert.IsTrue(world.TryArrangeMarriage(a.characterId, b.characterId, out string reason), reason);
                Assert.AreEqual(b.characterId, a.spouseId);
            }
            finally
            {
                religion.selectedDoctrines.Clear();
                religion.selectedDoctrines.AddRange(original);
            }
        }

        [Test]
        public void CrossRealmMarriage_ImprovesRelation()
        {
            var world = NewWorld();
            int realmA = -1;
            int realmB = -1;
            foreach (int key in world.realms.Keys)
            {
                if (realmA < 0) realmA = key;
                else { realmB = key; break; }
            }
            Assert.GreaterOrEqual(realmB, 0, "前置：应有两个政权");
            CharacterData a = CreateAdult(world, realmA, "甲", true);
            CharacterData b = CreateAdult(world, realmB, "乙", false);
            var relation = world.GetDiplomacyManager().GetRelation(realmA, realmB);
            float before = relation.relation;

            Assert.IsTrue(world.TryArrangeMarriage(a.characterId, b.characterId, out string reason), reason);

            Assert.Greater(relation.relation, before, "跨政权联姻应改善关系");
        }
    }
}
