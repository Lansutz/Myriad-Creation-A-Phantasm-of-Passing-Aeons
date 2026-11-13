using NUnit.Framework;
using MyriadCreation.Simulation.Laws;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>军事法：域/族/组/项严格分层；兵源、编制、军事管理是三个组。</summary>
    public class MilitaryLawTaxonomyTests
    {
        [Test]
        public void MilitaryGroups_ShareMilitaryDomainAndFamily()
        {
            foreach (var group in new[]
            {
                LawConstraints.LawGroup.MilitaryRecruitmentGroup,
                LawConstraints.LawGroup.MilitaryOrganizationGroup,
                LawConstraints.LawGroup.MilitaryManagementGroup,
            })
            {
                Assert.AreEqual(LawConstraints.LawFamily.Military, LawConstraints.GetFamily(group));
                Assert.AreEqual(LawConstraints.LawDomain.Military, LawConstraints.GetDomain(group));
            }
        }

        [Test]
        public void GroupNames_AreDistinct()
        {
            Assert.AreEqual("兵役", LawConstraints.GetGroupName(LawConstraints.LawGroup.MilitaryRecruitmentGroup));
            Assert.AreEqual("军队组织", LawConstraints.GetGroupName(LawConstraints.LawGroup.MilitaryOrganizationGroup));
            Assert.AreEqual("军事管理", LawConstraints.GetGroupName(LawConstraints.LawGroup.MilitaryManagementGroup));
        }

        [Test]
        public void BorderGroups_AreFrontierFamily()
        {
            Assert.AreEqual(LawConstraints.LawFamily.Frontier, LawConstraints.GetFamily(LawConstraints.LawGroup.PassageGroup));
            Assert.AreEqual(LawConstraints.LawFamily.Frontier, LawConstraints.GetFamily(LawConstraints.LawGroup.BorderMarketGroup));
            Assert.AreEqual(LawConstraints.LawDomain.Governance, LawConstraints.GetDomain(LawConstraints.LawGroup.PassageGroup));
        }
    }
}
