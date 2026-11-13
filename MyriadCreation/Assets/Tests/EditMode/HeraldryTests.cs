using NUnit.Framework;
using MyriadCreation.Infrastructure.Save;
using MyriadCreation.Simulation.State;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>程序化家族纹章：确定性生成、幂等补齐、入档往返。</summary>
    public class HeraldryTests
    {
        [Test]
        public void Generate_IsDeterministic()
        {
            Heraldry.Generate(7, "测试家族", 1200, out string p1, out string c1);
            Heraldry.Generate(7, "测试家族", 1200, out string p2, out string c2);

            Assert.AreEqual(p1, p2);
            Assert.AreEqual(c1, c2);
            StringAssert.Contains("|", c1);
            Assert.AreEqual(3, c1.Split('|').Length);
        }

        [Test]
        public void Ensure_FillsOnce_AndKeepsExisting()
        {
            var family = new FamilyNode { familyId = 3, familyName = "甲", foundingYear = 900 };
            Heraldry.Ensure(family);
            Assert.IsFalse(string.IsNullOrEmpty(family.coaPattern));
            string pattern = family.coaPattern;

            family.coaPattern = "custom";
            Heraldry.Ensure(family);
            Assert.AreEqual("custom", family.coaPattern);
        }

        [Test]
        public void SaveDto_RoundTripsHeraldry()
        {
            var family = new FamilyNode { familyId = 9, familyName = "乙", foundingYear = 1100 };
            Heraldry.Ensure(family);

            FamilySaveData dto = FamilySaveData.FromFamily(family);
            FamilyNode restored = dto.ToFamily();

            Assert.AreEqual(family.coaPattern, restored.coaPattern);
            Assert.AreEqual(family.coaColors, restored.coaColors);
        }
    }
}
