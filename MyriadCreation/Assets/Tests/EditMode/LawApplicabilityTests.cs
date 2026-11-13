using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P2.5/M5：法律人格效力——豁免优先、适用范围按身份掩码。</summary>
    public class LawApplicabilityTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _objects.Clear();
        }

        [Test]
        public void ExemptionWins_OverWildcardScope()
        {
            var law = new LawDef
            {
                exemptIdentities = SocialIdentityFlags.Clergy
            };

            Assert.IsFalse(LawApplicability.AppliesTo(law, SocialIdentityFlags.Clergy));
            Assert.IsTrue(LawApplicability.AppliesTo(law, SocialIdentityFlags.Noble));
            Assert.IsTrue(LawApplicability.AppliesTo(law, SocialIdentityFlags.Citizen));
        }

        [Test]
        public void ExplicitScope_RequiresMatchingIdentity()
        {
            var law = new LawDef { appliesTo = SocialIdentityFlags.Noble | SocialIdentityFlags.Royalty };

            Assert.IsTrue(LawApplicability.AppliesTo(law, SocialIdentityFlags.Noble));
            Assert.IsFalse(LawApplicability.AppliesTo(law, SocialIdentityFlags.Citizen));
        }

        [Test]
        public void World_ClergyForum_ExemptsClergyOnly()
        {
            var go = new GameObject("LawApplicabilityWorld");
            _objects.Add(go);
            GameWorld world = go.AddComponent<GameWorld>();
            world.mapWidth = 24;
            world.mapHeight = 12;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(11);
            RealmData realm = world.realms[0];
            var manager = world.GetCharacterManager();
            CharacterData clergy = manager.CreateCharacter("教士", "测", 30, true, 0, 0, 0, CharacterRole.Clergy);
            CharacterData commoner = manager.CreateCharacter("平民", "测", 30, true, 0, 0, 0, CharacterRole.Commoner);
            clergy.realmId = realm.realmId;
            commoner.realmId = realm.realmId;
            realm.lawState.SetActiveLaw("privilege", "clergy_forum", 0);

            MyriadCreation.Simulation.Laws.LawDef forum = MyriadCreation.Simulation.LawHost.DefaultLawCatalog.FindLaw("clergy_forum");
            Assert.IsNotNull(forum, "内容表应能解析 clergy_forum");
            Assert.AreEqual(SocialIdentityFlags.Clergy, forum.exemptIdentities, "exemptIdentities 应为 Clergy");
            Assert.AreEqual(SocialIdentityFlags.Clergy, CharacterIdentity.Of(clergy, realm.realmId));
            Assert.AreEqual("clergy_forum", realm.lawState.GetActiveLaw("privilege"));
            Assert.IsTrue(world.HasLegalExemption(realm.realmId, clergy.characterId));
            Assert.IsFalse(world.HasLegalExemption(realm.realmId, commoner.characterId));
        }
    }
}
