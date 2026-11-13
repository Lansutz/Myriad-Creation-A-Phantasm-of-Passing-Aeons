using NUnit.Framework;
using MyriadCreation.Simulation.ContentHost;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// I0b 迁移检查：1018 不再是革新；1016 接机械印刷出版；行政簇改为精确名称。
    /// </summary>
    public class InnovationMigrationTests
    {
        [SetUp]
        public void Setup()
        {
            ContentRegistry.Reset();
            ContentRegistry.Initialize();
        }

        [Test]
        public void RemovedNationState_IsNotAnInnovation()
        {
            Assert.IsFalse(ContentRegistry.Innovations.ContainsKey(1018),
                "民族国家是 PolityForm，不应继续作为革新条目");
        }

        [Test]
        public void Newspaper_IsDownstreamOfPrintPublishing()
        {
            Assert.IsTrue(ContentRegistry.Innovations.TryGetValue(1016, out var newspaper));
            Assert.IsNotNull(newspaper);
            CollectionAssert.Contains(newspaper.GetPrerequisiteIds(), 1110,
                "报纸应依赖机械印刷出版，而不是只依赖活字印刷");
        }

        [Test]
        public void AdministrativeCluster_UsesPreciseNames()
        {
            Assert.AreEqual("文书行政", ContentRegistry.Innovations[822].innovationName);
            Assert.AreEqual("档案典藏", ContentRegistry.Innovations[940].innovationName);
            Assert.AreEqual("上计", ContentRegistry.Innovations[941].innovationName);
            Assert.AreEqual("考课", ContentRegistry.Innovations[942].innovationName);
            Assert.AreEqual("职官俸禄", ContentRegistry.Innovations[944].innovationName);
        }
        [Test]
        public void EconomyMilitaryCluster_UsesPreciseNames()
        {
            Assert.AreEqual("水力磨粉", ContentRegistry.Innovations[106].innovationName);
            Assert.AreEqual("风力磨粉", ContentRegistry.Innovations[989].innovationName);
            Assert.AreEqual("领主庄园", ContentRegistry.Innovations[952].innovationName);
            Assert.AreEqual("庄园法庭", ContentRegistry.Innovations[953].innovationName);
            Assert.AreEqual("公地放牧", ContentRegistry.Innovations[954].innovationName);
            Assert.AreEqual("磨坊领主权", ContentRegistry.Innovations[955].innovationName);
            Assert.AreEqual("劳役地租", ContentRegistry.Innovations[956].innovationName);
            Assert.AreEqual("包税", ContentRegistry.Innovations[961].innovationName);
            Assert.AreEqual("军队编制", ContentRegistry.Innovations[995].innovationName);
            Assert.AreEqual("军功爵", ContentRegistry.Innovations[998].innovationName);
            Assert.AreEqual("募兵", ContentRegistry.Innovations[1003].innovationName);
            Assert.AreEqual("具装甲骑", ContentRegistry.Innovations[1006].innovationName);
        }

        [Test]
        public void TechArtifactCluster_UsesProcessAndPatternNames()
        {
            Assert.AreEqual("黑火药配制", ContentRegistry.Innovations[207].innovationName);
            Assert.AreEqual("弩机制作", ContentRegistry.Innovations[304].innovationName);
            Assert.AreEqual("配重式抛射机构", ContentRegistry.Innovations[305].innovationName);
            Assert.AreEqual("火绳枪机", ContentRegistry.Innovations[307].innovationName);
            Assert.AreEqual(Content.Definitions.InnovationKind.Pattern, ContentRegistry.Innovations[305].kind);
            Assert.AreEqual(Content.Definitions.InnovationKind.Pattern, ContentRegistry.Innovations[307].kind);
        }

    }
}
