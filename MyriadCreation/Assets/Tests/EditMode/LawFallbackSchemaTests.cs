using NUnit.Framework;
using MyriadCreation.Simulation.Laws;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>兜底条款与政体形式条件字段已落到法律 schema。</summary>
    public class LawFallbackSchemaTests
    {
        [Test]
        public void FallbackFields_Exist()
        {
            var group = new LawGroupDef { fallbackLawId = "customary_default" };
            var law = new LawDef { isFallback = true };
            Assert.AreEqual("customary_default", group.fallbackLawId);
            Assert.IsTrue(law.isFallback);
        }

        [Test]
        public void PolityFormCondition_Exists()
        {
            var req = new LawRequirement { requiredPolityForm = "feudal" };
            Assert.AreEqual("feudal", req.requiredPolityForm);
        }
    }
}
