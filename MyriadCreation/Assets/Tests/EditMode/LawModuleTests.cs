using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MyriadCreation.Simulation.Definitions;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Simulation.LawHost;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>法律模块收尾测试：条文目录、组→族→域派生、罪名引用与审判受理。</summary>
    public class LawModuleTests
    {
        [Test]
        public void Corpus_CoversEveryLawGroupOnce()
        {
            var corpus = LawCatalog.BuildCorpus(new LawComposition());
            int groupCount = Enum.GetValues(typeof(LawConstraints.LawGroup)).Length;
            Assert.AreEqual(groupCount, corpus.Count, "每个法律组应生成一条条文");

            foreach (LawConstraints.LawGroup group in Enum.GetValues(typeof(LawConstraints.LawGroup)))
                Assert.IsTrue(corpus.Any(l => l.group == group), $"缺少法律组 {group} 的条文");
        }

        [Test]
        public void Corpus_DerivesFamilyAndDomainFromGroup()
        {
            var corpus = LawCatalog.BuildCorpus(new LawComposition());
            var law = corpus.First(l => l.group == LawConstraints.LawGroup.SupremeSuccessionGroup);
            Assert.AreEqual(LawConstraints.LawFamily.SupremePower, law.Family);
            Assert.AreEqual(LawConstraints.LawDomain.Institutional, law.Domain);
        }

        [Test]
        public void Corpus_CitesCrimesByGroupOrDomain()
        {
            var judicial = new JudicialSystem { laws = LawCatalog.BuildCorpus(new LawComposition()) };
            Assert.IsTrue(judicial.IsCrimeCited(CrimeType.Treason), "建制域条文应引用叛国");
            Assert.IsTrue(judicial.IsCrimeCited(CrimeType.Murder), "身份族条文应引用谋杀");
        }

        [Test]
        public void MatchByBehavior_MapsHomicideToMurder()
        {
            CollectionAssert.Contains(CrimeRegistry.MatchByBehavior(CrimeBehavior.Homicide), CrimeType.Murder);
        }

        [Test]
        public void TrialCrime_DismissesUncitedCrime()
        {
            var judicial = new JudicialSystem();
            judicial.laws.Add(new Law
            {
                lawId = 1,
                group = LawConstraints.LawGroup.RealmStructureGroup,
                isActive = true,
                citedCrimes = new List<CrimeType>()
            });

            Assert.IsFalse(judicial.IsCrimeCited(CrimeType.Murder));
            var result = judicial.TrialCrime(CrimeType.Murder, 1, 0, null);
            Assert.AreEqual(VerdictType.Dismissed, result.verdict);
        }

        [Test]
        public void RealmDto_LegacyCompositionField_MapsToLaws()
        {
            const string legacyJson =
                "{\"realmId\":1,\"composition\":{\"localSuccession\":{\"primary\":1,\"secondary\":[]}}}";
            var dto = UnityEngine.JsonUtility.FromJson<RealmDTO>(legacyJson);
            dto.NormalizeLegacyFields();
            Assert.AreEqual(1, dto.laws.localSuccession.primary, "旧档 composition 字段应映射到 laws");
        }
    }
}
