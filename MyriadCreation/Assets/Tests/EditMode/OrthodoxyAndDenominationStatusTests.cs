using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.Faith;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P2.2：正统边界只按声明判定；宗派状态由越界/分歧度共同决定。</summary>
    public class OrthodoxyAndDenominationStatusTests
    {
        [Test]
        public void NoDeclaredBoundary_IsUnspecified()
        {
            var orthodox = new ReligionDef();
            Assert.AreEqual(OrthodoxyVerdict.Unspecified,
                OrthodoxyEvaluator.Evaluate(orthodox, new List<string> { "doctrine_any" }));
        }

        [Test]
        public void MissingTenet_AndExcludedDoctrine_AreOutOfBounds()
        {
            var orthodox = new ReligionDef
            {
                orthodoxyTenets = new List<string> { "doctrine_monotheism" },
                orthodoxyExclusions = new List<string> { "doctrine_iconoclasm" }
            };
            var violations = new List<string>();

            Assert.AreEqual(OrthodoxyVerdict.OutOfBounds,
                OrthodoxyEvaluator.Evaluate(orthodox, new List<string> { "doctrine_iconoclasm" }, violations));
            Assert.AreEqual(2, violations.Count, "缺必备 + 含排除各记一条");

            violations.Clear();
            Assert.AreEqual(OrthodoxyVerdict.InBounds,
                OrthodoxyEvaluator.Evaluate(orthodox, new List<string> { "doctrine_monotheism" }, violations));
            Assert.AreEqual(0, violations.Count);
        }

        [Test]
        public void Status_DeclaredViolationIsHeterodox_ElseDivergenceDecides()
        {
            Assert.AreEqual(DenominationStatus.Heterodox, DenominationStatusRules.Evaluate(0f, 1));
            Assert.AreEqual(DenominationStatus.Tolerated, DenominationStatusRules.Evaluate(30f, 0));
            Assert.AreEqual(DenominationStatus.Mainstream, DenominationStatusRules.Evaluate(29f, 0));
        }
    }
}
