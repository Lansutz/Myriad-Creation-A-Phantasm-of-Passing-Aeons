using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>实体关系（结构）、契约（协议）分离；种类对校验；活物/食物标记。</summary>
    public class EntityRelationAndLivingGoodsTests
    {
        [TearDown]
        public void TearDown()
        {
            RelationRegistry.Clear();
            ContractRegistry.Clear();
        }

        [Test]
        public void Relation_AllowsOnlyKindPairs()
        {
            var rule = new EntityRelation
            {
                from = new EntityRef(EntityKind.Realm, 1),
                to = new EntityRef(EntityKind.Anchor, 10),
                type = RelationType.DirectRule,
                isPrimary = true,
            };
            Assert.IsTrue(RelationRegistry.Add(rule), "Realm→Anchor 领属合法");

            var invalid = new EntityRelation
            {
                from = new EntityRef(EntityKind.Realm, 1),
                to = new EntityRef(EntityKind.Unit, 5),
                type = RelationType.DirectRule,
            };
            Assert.IsFalse(RelationRegistry.Add(invalid), "Realm→Unit 不能是领属");

            Assert.IsTrue(RelationRegistry.TryGetPrimary(EntityKind.Anchor, 10, out var primary));
            Assert.AreEqual(RelationType.DirectRule, primary.type);
        }

        [Test]
        public void Relation_PrimaryAndLayers_WithValidation()
        {
            RelationRegistry.Add(new EntityRelation
            {
                from = new EntityRef(EntityKind.Realm, 10),
                to = new EntityRef(EntityKind.Anchor, 1),
                type = RelationType.DirectRule, isPrimary = true,
            });
            RelationRegistry.Add(new EntityRelation
            {
                from = new EntityRef(EntityKind.Unit, 20),
                to = new EntityRef(EntityKind.Anchor, 1),
                type = RelationType.MilitaryOccupation, legitimacy = 0.2f,
            });
            Assert.AreEqual(2, RelationRegistry.GetRelations(EntityKind.Anchor, 1).Count);
            Assert.IsTrue(RelationRegistry.TryGetPrimary(EntityKind.Anchor, 1, out var primary));
            Assert.AreEqual(RelationType.DirectRule, primary.type);
        }

        [Test]
        public void RelationRules_SupportCharacterEndpoints()
        {
            Assert.IsTrue(RelationRules.IsAllowed(EntityKind.Character, EntityKind.Unit,
                RelationType.Command), "角色指挥单位");
            Assert.IsFalse(RelationRules.IsAllowed(EntityKind.Unit, EntityKind.Unit,
                RelationType.Command), "指挥不是单位→单位");
            Assert.IsTrue(RelationRules.IsAllowed(EntityKind.Realm, EntityKind.Character,
                RelationType.DelegatedRule), "委任落到人");
            Assert.IsTrue(RelationRules.IsAllowed(EntityKind.Character, EntityKind.Unit,
                RelationType.Employment), "个人雇佣单位");
            Assert.IsFalse(RelationRules.IsAllowed(EntityKind.Character, EntityKind.Anchor,
                RelationType.Garrison), "个人不能驻扎");
        }

        [Test]
        public void RelationRules_CoreMatrix()
        {
            Assert.IsTrue(RelationRules.IsAllowed(EntityKind.Realm, EntityKind.Anchor, RelationType.DirectRule));
            Assert.IsTrue(RelationRules.IsAllowed(EntityKind.Realm, EntityKind.Realm, RelationType.Vassalage));
            Assert.IsTrue(RelationRules.IsAllowed(EntityKind.Character, EntityKind.Unit, RelationType.Command));
            Assert.IsTrue(RelationRules.IsAllowed(EntityKind.Unit, EntityKind.Anchor, RelationType.Garrison));
            Assert.IsTrue(RelationRules.IsAllowed(EntityKind.Realm, EntityKind.Province, RelationType.Administrative));
            Assert.IsTrue(RelationRules.IsAllowed(EntityKind.Anchor, EntityKind.Anchor, RelationType.SpatialSubordination));
            Assert.IsFalse(RelationRules.IsAllowed(EntityKind.Unit, EntityKind.Unit, RelationType.DirectRule));
            Assert.IsFalse(RelationRules.IsAllowed(EntityKind.Realm, EntityKind.Character, RelationType.Garrison));
            Assert.IsFalse(RelationRules.IsAllowed(EntityKind.Anchor, EntityKind.Realm, RelationType.Vassalage));
        }

        [Test]
        public void WorldSavePolicy_KeepsOnlyPersistedEndpoints()
        {
            var all = new List<EntityRelation>
            {
                new EntityRelation
                {
                    from = new EntityRef(EntityKind.Realm, 1),
                    to = new EntityRef(EntityKind.Province, 2),
                    type = RelationType.Administrative,
                },
                new EntityRelation
                {
                    from = new EntityRef(EntityKind.Realm, 1),
                    to = new EntityRef(EntityKind.Anchor, 3),
                    type = RelationType.DirectRule,
                },
                new EntityRelation
                {
                    from = new EntityRef(EntityKind.Character, 4),
                    to = new EntityRef(EntityKind.Unit, 5),
                    type = RelationType.Command,
                },
            };
            var saved = MyriadCreation.Infrastructure.Save.RelationPersistencePolicy.ForWorldSave(all);
            Assert.AreEqual(1, saved.Count);
            Assert.AreEqual(RelationType.Administrative, saved[0].type);
        }

        [Test]
        public void Relation_Expiry_AndRestoreClearsOld()
        {
            var leased = new EntityRelation
            {
                from = new EntityRef(EntityKind.Realm, 1),
                to = new EntityRef(EntityKind.Anchor, 2),
                type = RelationType.Lease,
                sinceDay = 0, expiresDay = 10,
            };
            Assert.IsTrue(leased.IsActive(9));
            Assert.IsFalse(leased.IsActive(10));

            RelationRegistry.Add(new EntityRelation
            {
                from = new EntityRef(EntityKind.Realm, 1),
                to = new EntityRef(EntityKind.Anchor, 3),
                type = RelationType.DirectRule, isPrimary = true,
            });
            var snapshot = RelationRegistry.Capture();
            RelationRegistry.Add(new EntityRelation
            {
                from = new EntityRef(EntityKind.Realm, 2),
                to = new EntityRef(EntityKind.Anchor, 4),
                type = RelationType.DirectRule, isPrimary = true,
            });
            RelationRegistry.Restore(snapshot);
            Assert.AreEqual(0, RelationRegistry.GetRelations(EntityKind.Anchor, 4).Count,
                "Restore 应清掉快照之后的旧关系");
        }

        [Test]
        public void DiplomacyBridge_MapsSubordinationTypes()
        {
            Assert.AreEqual(RelationType.Tributary,
                MyriadCreation.Simulation.Systems.DiplomacyRelationBridge.MapSubordination(
                    MyriadCreation.Content.Definitions.SubordinationType.Tributary));
            Assert.AreEqual(RelationType.Protectorate,
                MyriadCreation.Simulation.Systems.DiplomacyRelationBridge.MapSubordination(
                    MyriadCreation.Content.Definitions.SubordinationType.Protectorate));
            Assert.AreEqual(RelationType.Vassalage,
                MyriadCreation.Simulation.Systems.DiplomacyRelationBridge.MapSubordination(
                    MyriadCreation.Content.Definitions.SubordinationType.Vassal));
            Assert.AreEqual(RelationType.Vassalage,
                MyriadCreation.Simulation.Systems.DiplomacyRelationBridge.MapSubordination(
                    MyriadCreation.Content.Definitions.SubordinationType.Puppet));
        }

        [Test]
        public void MapSavePolicy_IncludesOnlyAnchorRealmRelations()
        {
            var all = new List<EntityRelation>
            {
                new EntityRelation
                {
                    from = new EntityRef(EntityKind.Realm, 1),
                    to = new EntityRef(EntityKind.Anchor, 2),
                    type = RelationType.DirectRule,
                },
                new EntityRelation
                {
                    from = new EntityRef(EntityKind.Anchor, 2),
                    to = new EntityRef(EntityKind.Anchor, 3),
                    type = RelationType.SpatialSubordination,
                },
                new EntityRelation
                {
                    from = new EntityRef(EntityKind.Realm, 1),
                    to = new EntityRef(EntityKind.Unit, 4),
                    type = RelationType.Commission,
                },
            };
            var saved = MyriadCreation.Infrastructure.Save.RelationPersistencePolicy.ForMapSave(all);
            Assert.AreEqual(2, saved.Count);
            Assert.IsTrue(saved.TrueForAll(r =>
                r.from.kind == EntityKind.Anchor || r.to.kind == EntityKind.Anchor));
        }

        [Test]
        public void Registry_MergeAndRemoveAnchorTargets()
        {
            RelationRegistry.Add(new EntityRelation
            {
                from = new EntityRef(EntityKind.Realm, 1),
                to = new EntityRef(EntityKind.Anchor, 10),
                type = RelationType.DirectRule, isPrimary = true,
            });
            RelationRegistry.Add(new EntityRelation
            {
                from = new EntityRef(EntityKind.Realm, 1),
                to = new EntityRef(EntityKind.Realm, 2),
                type = RelationType.Vassalage,
            });

            Assert.AreEqual(1, RelationRegistry.RemoveByTargetKind(EntityKind.Anchor));
            Assert.AreEqual(0, RelationRegistry.GetRelations(EntityKind.Anchor, 10).Count);
            Assert.AreEqual(1, RelationRegistry.GetRelations(EntityKind.Realm, 2).Count);

            RelationRegistry.Merge(new[]
            {
                new EntityRelation
                {
                    from = new EntityRef(EntityKind.Realm, 1),
                    to = new EntityRef(EntityKind.Anchor, 11),
                    type = RelationType.DirectRule,
                },
            });
            Assert.AreEqual(1, RelationRegistry.GetRelations(EntityKind.Anchor, 11).Count);
            Assert.AreEqual(1, RelationRegistry.GetRelations(EntityKind.Realm, 2).Count, "Merge 不清旧的世界关系");
        }

        [Test]
        public void Contract_IsSeparateFromRelation()
        {
            var charter = new Contract
            {
                type = ContractType.Charter,
                parties =
                {
                    new EntityRef(EntityKind.Realm, 1),
                    new EntityRef(EntityKind.Unit, 2),
                },
                terms = { "settle_land", "raise_troops" },
                target = new EntityRef(EntityKind.Anchor, 7),
                hasTarget = true,
                startDay = 0,
                expiresDay = 100,
            };
            ContractRegistry.Add(charter);

            Assert.AreEqual(1, ContractRegistry.Count);
            Assert.IsTrue(ContractRegistry.TryGet(charter.contractId, out var found));
            Assert.AreEqual(ContractType.Charter, found.type);

            var byParty = ContractRegistry.GetByParty(EntityKind.Unit, 2);
            Assert.AreEqual(1, byParty.Count);
            Assert.AreEqual(0, RelationRegistry.GetRelations(EntityKind.Unit, 2).Count,
                "契约不自动变成归属关系");
        }

        [Test]
        public void RelationAndContract_CaptureRestore()
        {
            RelationRegistry.Add(new EntityRelation
            {
                from = new EntityRef(EntityKind.Realm, 1),
                to = new EntityRef(EntityKind.Anchor, 3),
                type = RelationType.DirectRule, isPrimary = true,
                obligations = { "tribute" },
            });
            ContractRegistry.Add(new Contract
            {
                type = ContractType.MercenaryContract,
                parties = { new EntityRef(EntityKind.Unit, 1), new EntityRef(EntityKind.Unit, 2) },
            });

            var relations = RelationRegistry.Capture();
            var contracts = ContractRegistry.Capture();
            RelationRegistry.Clear();
            ContractRegistry.Clear();
            RelationRegistry.Restore(relations);
            ContractRegistry.Restore(contracts);

            Assert.IsTrue(RelationRegistry.TryGetPrimary(EntityKind.Anchor, 3, out var relation));
            CollectionAssert.Contains(relation.obligations, "tribute");
            Assert.AreEqual(1, ContractRegistry.Count);
        }

        [Test]
        public void LivingGoods_FlagsAndSlaughter()
        {
            var world = new GameWorld();
            typeof(GameWorld).GetMethod("InitializeGoodsDefs", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(world, null);

            Assert.IsTrue(world.goodsDefs[1].isPerishable, "肉类易腐");
            var horse = world.goodsDefs[20];
            Assert.IsTrue(horse.isLiving);
            Assert.IsTrue(horse.canBeSlaughtered);
            Assert.AreEqual(1, horse.slaughterGoodsId, "马可宰杀为肉类");
            Assert.IsFalse(world.goodsDefs[90].canBeSlaughtered, "奴隶不是食物");
        }
    }
}
