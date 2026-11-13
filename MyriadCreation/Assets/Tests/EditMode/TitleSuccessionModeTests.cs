using NUnit.Framework;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Definitions;
using MyriadCreation.Simulation.Politics;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P0/P1：头衔继承模式与继承法预设目录。</summary>
    public class TitleSuccessionModeTests
    {
        [Test]
        public void Catalog_ResolvesKnownPresets()
        {
            Assert.IsNotNull(InheritanceLawCatalog.Resolve("primogeniture"));
            Assert.IsNotNull(InheritanceLawCatalog.Resolve("tanistry"));
            Assert.IsNotNull(InheritanceLawCatalog.Resolve("salic"));
        }

        [Test]
        public void Catalog_UnknownPreset_FallsBackToPrimogeniture()
        {
            InheritanceLaw law = InheritanceLawCatalog.Resolve("not_a_preset");
            Assert.IsNotNull(law);
            Assert.AreEqual(InheritanceLaw.Primogeniture().branch, law.branch);
        }

        [Test]
        public void TitleDefinition_DefaultsToNoSuccession()
        {
            var definition = new TitleDefinition { titleId = "title_x" };
            Assert.AreEqual(TitleSuccessionMode.None, definition.successionMode);
            Assert.IsEmpty(definition.ownLawPreset);
        }

        [Test]
        public void TitleInstance_DefaultsToUnboundDefinition()
        {
            var instance = new TitleInstance("realm_1_primary");
            Assert.IsEmpty(instance.definitionId);
        }
    }
}
