using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.FaithHost;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P1.3 WP3：optionId 解析成可消费绑定点；未选中保持 Unspecified。</summary>
    public class DoctrineBindingTests
    {
        [Test]
        public void Resolve_MapsEthicsAndInstitutionOptions()
        {
            var religion = new ReligionDef
            {
                selectedDoctrines = new List<string>
                {
                    "doctrine_homosexuality_crime",
                    "doctrine_adultery_shunned",
                    "doctrine_witchcraft_accepted",
                    "doctrine_divorce_allowed",
                    "doctrine_funeral_cremation",
                    "doctrine_clerical_gender_male",
                    "doctrine_clerical_marriage_celibate",
                    "doctrine_pacifism",
                    "doctrine_sacred_kingship"
                }
            };

            DoctrineBindings bindings = DoctrineBindingHost.Resolve(religion);

            Assert.AreEqual(DoctrineMoralStance.Crime, bindings.homosexuality);
            Assert.AreEqual(DoctrineMoralStance.Shunned, bindings.adultery);
            Assert.AreEqual(DoctrineMoralStance.Accepted, bindings.witchcraft);
            Assert.AreEqual(DoctrineMarriageStance.Allowed, bindings.divorce);
            Assert.AreEqual(DoctrineFuneralRite.Cremation, bindings.funeral);
            Assert.AreEqual(DoctrineClericalGender.MaleOnly, bindings.clericalGender);
            Assert.AreEqual(DoctrineClericalMarriage.Celibate, bindings.clericalMarriage);
            Assert.IsTrue(bindings.pacifism);
            Assert.IsTrue(bindings.sacredKingship);
        }

        [Test]
        public void Resolve_EmptyKeepsUnspecified()
        {
            DoctrineBindings bindings = DoctrineBindingHost.Resolve(new ReligionDef { selectedDoctrines = new List<string>() });

            Assert.AreEqual(DoctrineMoralStance.Unspecified, bindings.homosexuality);
            Assert.AreEqual(DoctrineMarriageStance.Unspecified, bindings.divorce);
            Assert.AreEqual(DoctrineFuneralRite.Unspecified, bindings.funeral);
            Assert.AreEqual(DoctrineClericalGender.Unspecified, bindings.clericalGender);
            Assert.AreEqual(DoctrineClericalMarriage.Unspecified, bindings.clericalMarriage);
            Assert.IsFalse(bindings.pacifism);
            Assert.IsFalse(bindings.sacredKingship);
        }
    }
}
