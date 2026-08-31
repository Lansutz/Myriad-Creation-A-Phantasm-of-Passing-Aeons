using NUnit.Framework;
using CivilizationEvolution.Core;
using CivilizationEvolution.Tech;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 青铜时代扩充 + 军妓 + 民族主义/民族国家 测试
    /// </summary>
    public class BronzeAgeNationalismTests
    {
        private InnovationTree _tree;

        [SetUp]
        public void Setup()
        {
            ContentRegistry.Reset();
            ContentRegistry.Initialize();
            _tree = new InnovationTree(); // 构造即 LoadFromRegistry
        }

        [Test]
        public void BronzeChain_LeatherToChariot()
        {
            // 皮革加工 1009 → 皮甲 1010
            Assert.IsNotNull(_tree.GetInnovation(1009), "皮革加工存在");
            Assert.IsNotNull(_tree.GetInnovation(1010), "皮甲存在");
            Assert.IsTrue(_tree.GetInnovation(1010).prerequisites.Contains(1009), "皮甲前置皮革加工");
            Assert.IsTrue(_tree.GetInnovation(1010).prerequisites.Contains(903), "皮甲前置磨制石器");

            // 实心轮 1011 → 辐条轮 1012 → 战车 1013
            Assert.IsTrue(_tree.GetInnovation(1012).prerequisites.Contains(1011), "辐条轮前置实心轮");
            var chariot = _tree.GetInnovation(1013);
            Assert.IsTrue(chariot.prerequisites.Contains(1012), "战车前置辐条轮");
            Assert.IsTrue(chariot.prerequisites.Contains(922), "战车前置马的驯化");
            Assert.IsTrue(chariot.prerequisites.Contains(300), "战车前置青铜武器");

            // 青铜盔甲 1014
            var bronzeArmor = _tree.GetInnovation(1014);
            Assert.IsTrue(bronzeArmor.prerequisites.Contains(300), "青铜盔甲前置青铜武器");
            Assert.IsTrue(bronzeArmor.prerequisites.Contains(1010), "青铜盔甲前置皮甲");
        }

        [Test]
        public void CampFollowers_InnovationExists()
        {
            var def = _tree.GetInnovation(1015);
            Assert.IsNotNull(def, "随营军妓存在");
            Assert.AreEqual(InnovationField.MilitaryInstitution, def.field, "军制");
            Assert.IsTrue(def.prerequisites.Contains(995), "前置军队编制");
        }

        [Test]
        public void Nationalism_Chain()
        {
            // 报纸 1016 → 民族主义 1017 → 民族国家 1018
            var newspaper = _tree.GetInnovation(1016);
            Assert.IsNotNull(newspaper, "报纸存在");
            Assert.AreEqual(InnovationField.Education, newspaper.field, "报纸=教育");
            Assert.IsTrue(newspaper.prerequisites.Contains(979), "报纸前置活字印刷");
            Assert.IsTrue(newspaper.prerequisites.Contains(823), "报纸前置驿传");

            var nationalism = _tree.GetInnovation(1017);
            Assert.IsNotNull(nationalism, "民族主义存在");
            Assert.AreEqual(InnovationField.SocialThought, nationalism.field, "民族主义=思潮子类");
            Assert.IsTrue(nationalism.prerequisites.Contains(1016), "民族主义前置报纸");
            Assert.IsTrue(nationalism.prerequisites.Contains(600), "民族主义前置文字");

            var nationState = _tree.GetInnovation(1018);
            Assert.IsNotNull(nationState, "民族国家存在");
            Assert.AreEqual(InnovationField.Governance, nationState.field, "民族国家=政制");
            Assert.IsTrue(nationState.prerequisites.Contains(1017), "民族国家前置民族主义");
            Assert.IsTrue(nationState.prerequisites.Contains(503), "民族国家前置官僚制度");
        }

        [Test]
        public void SocialThought_FieldMapsToThought()
        {
            Assert.AreEqual(InnovationDomain.Thought,
                InnovationDomainMap.GetDomain(InnovationField.SocialThought), "思潮归思维大类");
        }

        [Test]
        public void DirectPrerequisites_OnlyOneLayer()
        {
            // 民族国家 1018 的直接前置 = [1017, 503]（一层，不推民族主义的前置报纸）
            var (and, or) = _tree.GetDirectPrerequisites(1018);
            CollectionAssert.AreEqual(new[] { 1017, 503 }, and, "直接前置仅一层");
            Assert.IsFalse(and.Contains(1016), "不显示再上一层的报纸");
            Assert.IsFalse(and.Contains(979), "不显示全链");

            // 战车 1013：AND 链 [1012, 922, 300] + OR 链空
            var (and2, or2) = _tree.GetDirectPrerequisites(1013);
            CollectionAssert.AreEquivalent(new[] { 1012, 922, 300 }, and2);
            Assert.IsEmpty(or2);
        }
    }
}
