using NUnit.Framework;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.FaithHost;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P1.3 合并接线点：教职性别/独身/君权神授只在这一处判定。</summary>
    public class DoctrineIdentityRulesTests
    {
        private static DoctrineBindings Bindings(string optionId)
        {
            var bindings = new DoctrineBindings();
            bindings.ApplyOption(optionId);
            return bindings;
        }

        [Test]
        public void MaleOnly_BlocksFemaleClergy_AllowsMale()
        {
            DoctrineBindings bindings = Bindings("doctrine_clerical_gender_male");

            Assert.IsFalse(DoctrineIdentityRules.CanHoldReligiousOffice(bindings, isMale: false));
            Assert.IsTrue(DoctrineIdentityRules.CanHoldReligiousOffice(bindings, isMale: true));
        }

        [Test]
        public void Either_AllowsFemaleClergy()
        {
            DoctrineBindings bindings = Bindings("doctrine_clerical_gender_either");

            Assert.IsTrue(DoctrineIdentityRules.CanHoldReligiousOffice(bindings, isMale: false));
            Assert.IsTrue(DoctrineIdentityRules.CanHoldReligiousOffice(bindings, isMale: true));
        }

        [Test]
        public void Celibate_BlocksClergyMarriage_ButNotLay()
        {
            DoctrineBindings bindings = Bindings("doctrine_clerical_marriage_celibate");

            Assert.IsTrue(DoctrineIdentityRules.RequiresCelibacy(bindings));
            Assert.IsFalse(DoctrineIdentityRules.CanMarry(bindings, SocialIdentityFlags.Clergy));
            Assert.IsTrue(DoctrineIdentityRules.CanMarry(bindings, SocialIdentityFlags.Citizen));
        }

        [Test]
        public void AllowedClergyMarriage_DoesNotBlock()
        {
            DoctrineBindings bindings = Bindings("doctrine_clerical_marriage_allowed");

            Assert.IsFalse(DoctrineIdentityRules.RequiresCelibacy(bindings));
            Assert.IsTrue(DoctrineIdentityRules.CanMarry(bindings, SocialIdentityFlags.Clergy));
        }

        [Test]
        public void SacredKingship_IsFlagged()
        {
            Assert.IsTrue(DoctrineIdentityRules.IsSacredKingship(Bindings("doctrine_sacred_kingship")));
        }

        [Test]
        public void Unspecified_IsPermissive()
        {
            var bindings = new DoctrineBindings();

            Assert.IsTrue(DoctrineIdentityRules.CanHoldReligiousOffice(bindings, isMale: false));
            Assert.IsTrue(DoctrineIdentityRules.CanMarry(bindings, SocialIdentityFlags.Clergy));
            Assert.IsFalse(DoctrineIdentityRules.RequiresCelibacy(bindings));
            Assert.IsFalse(DoctrineIdentityRules.IsSacredKingship(bindings));
        }
    }
}
