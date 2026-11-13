using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Simulation.Faith;
using MyriadCreation.Simulation.FaithHost;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P1.3：Shunned 不入罪，但同信仰者会对当事人产生好感惩罚。</summary>
    public class DoctrineShunnedTests
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
            var go = new GameObject("DoctrineShunnedWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = 48;
            world.mapHeight = 24;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(103);
            return world;
        }

        private static CharacterData CreateAdult(GameWorld world, RealmData realm, string name)
        {
            CharacterData character = world.GetCharacterManager().CreateCharacter(
                name, "氏", 20, false, realm.primaryCultureId, 0, realm.stateReligionId, CharacterRole.Noble);
            character.realmId = realm.realmId;
            character.faithId = realm.stateReligionId;
            return character;
        }

        [Test]
        public void ShunnedBehavior_LowersCoReligionistOpinion()
        {
            var world = NewWorld();
            RealmData realm = null;
            foreach (var r in world.realms.Values) { realm = r; break; }
            FaithSystem faith = world.GetFaithSystem(realm.stateReligionId);
            Assert.IsNotNull(faith);
            Assert.IsTrue(ContentResolvers.Religions.TryGet(faith.religionId, out ReligionDef religion));
            var original = new List<string>(religion.selectedDoctrines);
            try
            {
                religion.selectedDoctrines.Add("doctrine_adultery_shunned");
                Assert.IsTrue(world.IsDoctrineShunned(realm.realmId, DoctrineCrimeBehavior.Adultery));

                CharacterData actor = CreateAdult(world, realm, "当事人");
                CharacterData witness = CreateAdult(world, realm, "同信仰者");
                witness.ModifyRelation(actor.characterId, 0f, "initial");
                float before = witness.relations[actor.characterId].opinion;

                world.TryTrialDoctrineCrime(realm.realmId, DoctrineCrimeBehavior.Adultery, actor.characterId);

                Assert.Less(witness.relations[actor.characterId].opinion, before, "忌讳行为应降低同信仰者的好感");
            }
            finally
            {
                religion.selectedDoctrines.Clear();
                religion.selectedDoctrines.AddRange(original);
            }
        }
    }
}
