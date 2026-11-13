using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.LawHost;
using MyriadCreation.Simulation.Laws;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 守卫：DefaultLawCatalog 的深拷贝（JSON 内容 → 运行时目录的唯一通路）必须逐字段保真。
    /// 历史缺陷：CloneLaw 丢 excludesLawIds/overrides/normKind/scope/precedence/isFallback/issuerCharacterId、
    /// CloneRequirements 丢 requiredDecreeIds/requiredPolityForm、CloneDecree 丢 excludesDecreeIds、
    /// CloneGroups 丢 fallbackLawId —— 导致互斥/法令前置/规范类型在真实游戏里静默失效。
    /// 其它法律测试都直接 new LawDef，绕过了克隆，所以这些字段曾经漏拷仍全绿。
    /// </summary>
    public class LawCatalogCloneParityTests
    {
        private static LawDef BuildFullLaw()
        {
            return new LawDef
            {
                lawId = "probe_law",
                groupId = "probe_group",
                name = "探针法",
                description = "clone parity",
                tier = 3,
                enactmentCostTreasury = 12.5f,
                enactmentCostPrestige = 7.5f,
                upkeepPerYear = 4.5f,
                isInitiallyKnown = false,
                isFallback = true,
                normKind = LegalNormKind.Charter,
                scope = LegalScope.Personal,
                precedence = 9,
                issuerCharacterId = 42,
                enactmentDays = 45,
                proposalAuthority = ProposalAuthority.CharterHolders,
                requiredInstitution = CentralInstitution.Assembly,
                requirements = new LawRequirement
                {
                    minTreasury = 100f,
                    minLegitimacy = 20f,
                    minCentralization = 30f,
                    requiredLawIds = new List<string> { "tax_standard" },
                    requiredInnovationIds = new List<int> { 995, 601 },
                    requiredDecreeIds = new List<string> { "decree_tax_relief" },
                    requiredPolityForm = "feudal"
                },
                effects = new LawEffect
                {
                    taxMultiplier = 1.25f,
                    stabilityPerYear = 1.5f,
                    legitimacyPerYear = 2.5f,
                    manpowerMultiplier = 1.1f,
                    unrestPerYear = -1.5f
                },
                overrides = new List<string> { "customary_law" },
                excludesLawIds = new List<string> { "tax_heavy" }
            };
        }

        private static void AssertEffectEqual(LawEffect expected, LawEffect actual)
        {
            Assert.AreEqual(expected.taxMultiplier, actual.taxMultiplier, "effects.taxMultiplier");
            Assert.AreEqual(expected.stabilityPerYear, actual.stabilityPerYear, "effects.stabilityPerYear");
            Assert.AreEqual(expected.legitimacyPerYear, actual.legitimacyPerYear, "effects.legitimacyPerYear");
            Assert.AreEqual(expected.manpowerMultiplier, actual.manpowerMultiplier, "effects.manpowerMultiplier");
            Assert.AreEqual(expected.unrestPerYear, actual.unrestPerYear, "effects.unrestPerYear");
        }

        private static void AssertRequirementEqual(LawRequirement expected, LawRequirement actual)
        {
            Assert.AreEqual(expected.minTreasury, actual.minTreasury, "requirements.minTreasury");
            Assert.AreEqual(expected.minLegitimacy, actual.minLegitimacy, "requirements.minLegitimacy");
            Assert.AreEqual(expected.minCentralization, actual.minCentralization, "requirements.minCentralization");
            CollectionAssert.AreEqual(expected.requiredLawIds, actual.requiredLawIds, "requirements.requiredLawIds");
            CollectionAssert.AreEqual(expected.requiredInnovationIds, actual.requiredInnovationIds, "requirements.requiredInnovationIds");
            CollectionAssert.AreEqual(expected.requiredDecreeIds, actual.requiredDecreeIds, "requirements.requiredDecreeIds");
            Assert.AreEqual(expected.requiredPolityForm, actual.requiredPolityForm, "requirements.requiredPolityForm");
        }

        private static void AssertLawEqual(LawDef expected, LawDef actual)
        {
            Assert.AreEqual(expected.lawId, actual.lawId, "lawId");
            Assert.AreEqual(expected.groupId, actual.groupId, "groupId");
            Assert.AreEqual(expected.name, actual.name, "name");
            Assert.AreEqual(expected.description, actual.description, "description");
            Assert.AreEqual(expected.tier, actual.tier, "tier");
            Assert.AreEqual(expected.enactmentCostTreasury, actual.enactmentCostTreasury, "enactmentCostTreasury");
            Assert.AreEqual(expected.enactmentCostPrestige, actual.enactmentCostPrestige, "enactmentCostPrestige");
            Assert.AreEqual(expected.upkeepPerYear, actual.upkeepPerYear, "upkeepPerYear");
            Assert.AreEqual(expected.isInitiallyKnown, actual.isInitiallyKnown, "isInitiallyKnown");
            Assert.AreEqual(expected.isFallback, actual.isFallback, "isFallback");
            Assert.AreEqual(expected.normKind, actual.normKind, "normKind");
            Assert.AreEqual(expected.scope, actual.scope, "scope");
            Assert.AreEqual(expected.precedence, actual.precedence, "precedence");
            Assert.AreEqual(expected.issuerCharacterId, actual.issuerCharacterId, "issuerCharacterId");
            Assert.AreEqual(expected.enactmentDays, actual.enactmentDays, "enactmentDays");
            Assert.AreEqual(expected.proposalAuthority, actual.proposalAuthority, "proposalAuthority");
            Assert.AreEqual(expected.requiredInstitution, actual.requiredInstitution, "requiredInstitution");
            CollectionAssert.AreEqual(expected.overrides, actual.overrides, "overrides");
            CollectionAssert.AreEqual(expected.excludesLawIds, actual.excludesLawIds, "excludesLawIds");
            AssertRequirementEqual(expected.requirements, actual.requirements);
            AssertEffectEqual(expected.effects, actual.effects);
        }

        [Test]
        public void CloneGroups_LawAndGroupFields_ArePreserved()
        {
            var law = BuildFullLaw();
            var group = new LawGroupDef
            {
                groupId = "probe_group",
                name = "探针组",
                order = 12,
                description = "probe",
                fallbackLawId = "customary_default",
                laws = new List<LawDef> { law }
            };

            List<LawGroupDef> clone = DefaultLawCatalog.CloneGroups(new List<LawGroupDef> { group });

            Assert.AreEqual(1, clone.Count, "组数量");
            Assert.AreEqual("customary_default", clone[0].fallbackLawId, "fallbackLawId（曾漏拷）");
            Assert.AreEqual(12, clone[0].order, "order");
            Assert.AreEqual(1, clone[0].laws.Count, "法数量");
            AssertLawEqual(law, clone[0].laws[0]);
        }

        [Test]
        public void CloneDecree_AllFields_ArePreserved()
        {
            var decree = new DecreeDef
            {
                decreeId = "probe_decree",
                name = "探针令",
                description = "probe",
                costTreasury = 150f,
                costAuthority = 8f,
                durationDays = 120,
                cooldownDays = 300,
                excludesDecreeIds = new List<string> { "decree_tax_relief" },
                requirements = new LawRequirement
                {
                    minLegitimacy = 55f,
                    requiredDecreeIds = new List<string> { "decree_tax_relief" },
                    requiredPolityForm = "centralized"
                },
                effects = new LawEffect { taxMultiplier = 0.8f, unrestPerYear = -2f, stabilityPerYear = 1f }
            };

            List<DecreeDef> clone = DefaultLawCatalog.CloneDecrees(new List<DecreeDef> { decree });

            Assert.AreEqual(1, clone.Count, "法令数量");
            DecreeDef actual = clone[0];
            Assert.AreEqual(decree.decreeId, actual.decreeId, "decreeId");
            Assert.AreEqual(decree.costTreasury, actual.costTreasury, "costTreasury");
            Assert.AreEqual(decree.costAuthority, actual.costAuthority, "costAuthority");
            Assert.AreEqual(decree.durationDays, actual.durationDays, "durationDays");
            Assert.AreEqual(decree.cooldownDays, actual.cooldownDays, "cooldownDays");
            CollectionAssert.AreEqual(decree.excludesDecreeIds, actual.excludesDecreeIds, "excludesDecreeIds（曾漏拷）");
            AssertRequirementEqual(decree.requirements, actual.requirements);
            AssertEffectEqual(decree.effects, actual.effects);
        }

        [Test]
        public void CloneGroups_IsDeepCopy_NotSharedReferences()
        {
            var law = BuildFullLaw();
            var group = new LawGroupDef { groupId = "probe_group", laws = new List<LawDef> { law } };

            List<LawGroupDef> clone = DefaultLawCatalog.CloneGroups(new List<LawGroupDef> { group });
            clone[0].laws[0].excludesLawIds.Add("mutated_after_clone");
            clone[0].laws[0].requirements.requiredInnovationIds.Add(123456);

            Assert.AreEqual(1, law.excludesLawIds.Count, "克隆体修改不得回写源 law.excludesLawIds");
            Assert.AreEqual(2, law.requirements.requiredInnovationIds.Count, "克隆体修改不得回写源 requiredInnovationIds");
        }
    }
}
