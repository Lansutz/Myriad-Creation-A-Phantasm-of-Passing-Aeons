using NUnit.Framework;
using MyriadCreation.Simulation.Laws;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>法律/法令/特许统一规范模型：类型、层级、优先级、发布者、覆盖声明。</summary>
    public class LegalNormSchemaTests
    {
        [Test]
        public void Law_CanBeCharterWithScope()
        {
            var law = new LawDef
            {
                lawId = "charter_test",
                normKind = LegalNormKind.Charter,
                scope = LegalScope.Lordship,
                precedence = 3,
                issuerCharacterId = 7,
            };
            law.overrides.Add("local_custom_1");

            Assert.AreEqual(LegalNormKind.Charter, law.normKind);
            Assert.AreEqual(LegalScope.Lordship, law.scope);
            Assert.AreEqual(7, law.issuerCharacterId);
            CollectionAssert.Contains(law.overrides, "local_custom_1");
        }

        [Test]
        public void NormKindOrder_IsExplicit()
        {
            Assert.Less((int)LegalNormKind.Decree, (int)LegalNormKind.Statute);
            Assert.Less((int)LegalNormKind.Statute, (int)LegalNormKind.Charter);
            Assert.Less((int)LegalNormKind.Charter, (int)LegalNormKind.Constitutional);
        }
    }
}
