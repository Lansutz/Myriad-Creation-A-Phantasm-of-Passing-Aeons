using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 战争议事：宣战/改目标前先问朝堂。协商通过不额外收费；
    /// 多数反对时选择遵从则作罢，强推或不协商则付出关系与稳定度代价。
    /// </summary>
    public class CouncilConsultationTests
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
            var go = new GameObject("CouncilWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = 48;
            world.mapHeight = 24;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(29);
            return world;
        }

        private static void PickTwoRealms(GameWorld world, out RealmData attacker, out RealmData defender)
        {
            attacker = null;
            defender = null;
            foreach (var realm in world.realms.Values)
            {
                if (attacker == null) attacker = realm;
                else { defender = realm; break; }
            }
        }

        private static CharacterData AddCouncilor(GameWorld world, RealmData realm, string name, int officeId,
            float boldness, float compassion, float opinionToRuler)
        {
            var manager = world.GetCharacterManager();
            CharacterData councilor = manager.CreateCharacter(
                name, "氏", 40, true, realm.primaryCultureId, 0, realm.stateReligionId,
                CharacterRole.Noble);
            councilor.realmId = realm.realmId;
            councilor.boldness = boldness;
            councilor.compassion = compassion;
            councilor.honor = 20f;
            councilor.greed = 40f;
            councilor.relations ??= new Dictionary<int, CharacterRelation>();
            if (realm.monarchId >= 0)
            {
                councilor.relations[realm.monarchId] = new CharacterRelation
                {
                    otherCharacterId = realm.monarchId,
                    opinion = opinionToRuler
                };
            }
            realm.officeHolders[officeId] = councilor.characterId;
            return councilor;
        }

        private static CasusBelli BorderIncident(int attacker, int defender)
        {
            return new CasusBelli
            {
                cbId = 90,
                type = GameEnums.CasusBelliType.BorderIncident,
                holderRealmId = attacker,
                targetRealmId = defender,
                expiryDay = -1,
                justificationStrength = 200f
            };
        }

        private static void MakeSupportiveCouncil(GameWorld world, RealmData realm)
        {
            AddCouncilor(world, realm, "主战甲", 0, 90f, 0f, 100f);
            AddCouncilor(world, realm, "主战乙", 1, 80f, 10f, 80f);
            AddCouncilor(world, realm, "主战丙", 2, 70f, 20f, 60f);
        }

        private static void MakeOpposedCouncil(GameWorld world, RealmData realm)
        {
            AddCouncilor(world, realm, "反战甲", 0, 0f, 100f, -100f);
            AddCouncilor(world, realm, "反战乙", 1, 10f, 90f, -150f);
            AddCouncilor(world, realm, "反战丙", 2, 5f, 80f, -120f);
        }

        [Test]
        public void SupportiveCouncil_AllowsDeclarationWithoutOverridePenalty()
        {
            var world = NewWorld();
            PickTwoRealms(world, out RealmData attacker, out RealmData defender);
            Assert.IsNotNull(defender, "前置：应有两个政权");
            MakeSupportiveCouncil(world, attacker);
            float stabilityBefore = attacker.stability;

            bool declared = world.DeclareWarWithJustification(attacker.realmId, defender.realmId, BorderIncident(attacker.realmId, defender.realmId), null, "议事通过");

            Assert.IsTrue(declared, "议事多数支持时应允许宣战");
            WarState war = world.GetWars().Find(w => !w.ended);
            Assert.IsNotNull(war);
            Assert.IsTrue(war.councilConsulted);
            Assert.IsFalse(war.councilOverridden);
            Assert.IsFalse(war.councilSkipped);
            Assert.Greater(war.councilSupporters, war.councilOpponents);
            Assert.AreEqual(stabilityBefore, attacker.stability, "协商通过不应额外扣稳定度");
        }

        [Test]
        public void OpposedCouncil_AbideMode_BlocksDeclaration()
        {
            var world = NewWorld();
            PickTwoRealms(world, out RealmData attacker, out RealmData defender);
            Assert.IsNotNull(defender, "前置：应有两个政权");
            MakeOpposedCouncil(world, attacker);

            bool declared = world.DeclareWarWithJustification(attacker.realmId, defender.realmId, BorderIncident(attacker.realmId, defender.realmId), null, "议事反对",
                WarDecisionMode.ConsultAndAbide);

            Assert.IsFalse(declared, "多数反对且选择遵从时不得宣战");
            Assert.IsFalse(world.GetDiplomacyManager().GetRelation(attacker.realmId, defender.realmId).isAtWar);
            Assert.IsNull(world.GetWars().Find(w => !w.ended), "失败宣战不得留下战争运行态");
        }

        [Test]
        public void OpposedCouncil_OverrideMode_DeclaresWithRelationAndStabilityCost()
        {
            var world = NewWorld();
            PickTwoRealms(world, out RealmData attacker, out RealmData defender);
            Assert.IsNotNull(defender, "前置：应有两个政权");
            MakeOpposedCouncil(world, attacker);
            int councilorId = attacker.officeHolders[0];
            CharacterData councilor = world.GetCharacterManager().GetCharacter(councilorId);
            float opinionBefore = councilor.relations[attacker.monarchId].opinion;
            float stabilityBefore = attacker.stability;

            bool declared = world.DeclareWarWithJustification(attacker.realmId, defender.realmId, BorderIncident(attacker.realmId, defender.realmId), null, "强推",
                WarDecisionMode.ConsultAndOverride);

            Assert.IsTrue(declared, "强推应能宣战");
            WarState war = world.GetWars().Find(w => !w.ended);
            Assert.IsTrue(war.councilOverridden);
            Assert.Greater(war.councilOpponents, war.councilSupporters);
            Assert.Less(attacker.stability, stabilityBefore, "强推的代价应落在稳定度");
            float opinionAfter = councilor.relations[attacker.monarchId].opinion;
            Assert.Less(opinionAfter, opinionBefore, "反对者对被压过的统治者应更不满");
        }

        [Test]
        public void SkipConsultation_DeclaresButPaysUnilateralCost()
        {
            var world = NewWorld();
            PickTwoRealms(world, out RealmData attacker, out RealmData defender);
            Assert.IsNotNull(defender, "前置：应有两个政权");
            MakeSupportiveCouncil(world, attacker);
            int councilorId = attacker.officeHolders[0];
            CharacterData councilor = world.GetCharacterManager().GetCharacter(councilorId);
            float opinionBefore = councilor.relations[attacker.monarchId].opinion;
            float stabilityBefore = attacker.stability;

            bool declared = world.DeclareWarWithJustification(attacker.realmId, defender.realmId, BorderIncident(attacker.realmId, defender.realmId), null, "不协商",
                WarDecisionMode.SkipConsultation);

            Assert.IsTrue(declared, "不协商也能宣战");
            WarState war = world.GetWars().Find(w => !w.ended);
            Assert.IsTrue(war.councilSkipped);
            Assert.IsFalse(war.councilConsulted);
            Assert.Less(attacker.stability, stabilityBefore, "绕过议事应有稳定度代价");
            Assert.Less(councilor.relations[attacker.monarchId].opinion, opinionBefore, "绕过议事应损伤朝臣好感");
        }

        [Test]
        public void Consultation_ExposesStanceCounts()
        {
            var world = NewWorld();
            PickTwoRealms(world, out RealmData attacker, out RealmData defender);
            Assert.IsNotNull(defender, "前置：应有两个政权");
            MakeOpposedCouncil(world, attacker);

            CouncilConsultationResult result = CouncilConsultationSystem.Consult(world, attacker.realmId, null);

            Assert.IsTrue(result.hasCouncil);
            Assert.AreEqual(3, result.members);
            Assert.AreEqual(3, result.opponents);
            Assert.AreEqual(0, result.supporters);
            Assert.IsFalse(result.MajoritySupport);
            Assert.Less(result.approval, 0f);
        }
    }
}
