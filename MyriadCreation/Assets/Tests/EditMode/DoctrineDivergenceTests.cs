using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P2.2 分歧度：按组比较，sequence 取档距、category 取集合距离。</summary>
    public class DoctrineDivergenceTests
    {
        private static Dictionary<string, DoctrineOptionDef> Options()
        {
            var options = new Dictionary<string, DoctrineOptionDef>();
            Add(options, "doctrine_homosexuality_crime", "ethics", "category", 0);
            Add(options, "doctrine_homosexuality_accepted", "ethics", "category", 1);
            Add(options, "doctrine_adultery_crime", "ethics", "category", 0);
            Add(options, "doctrine_adultery_shunned", "ethics", "category", 1);
            Add(options, "doctrine_clerical_gender_male", "clerical_gender", "sequence", 0);
            Add(options, "doctrine_clerical_gender_either", "clerical_gender", "sequence", 1);
            Add(options, "doctrine_divorce_forbidden", "marriage", "sequence", 0);
            Add(options, "doctrine_divorce_permitted", "marriage", "sequence", 1);
            Add(options, "doctrine_divorce_allowed", "marriage", "sequence", 2);
            return options;
        }

        private static void Add(Dictionary<string, DoctrineOptionDef> options, string id, string group, string kind, int level)
        {
            options[id] = new DoctrineOptionDef { optionId = id, groupId = group, kind = kind, level = level };
        }

        [Test]
        public void IdenticalSets_HaveZeroDivergence()
        {
            var set = new List<string> { "doctrine_homosexuality_crime", "doctrine_clerical_gender_male" };
            Assert.AreEqual(0f, DoctrineDivergence.Calculate(set, set, Options()), 0.0001f);
        }

        [Test]
        public void CategoryDifference_IsHalfWhenOnlyOneOfTwoGroupsDiffers()
        {
            var a = new List<string> { "doctrine_homosexuality_crime", "doctrine_clerical_gender_male" };
            var b = new List<string> { "doctrine_homosexuality_accepted", "doctrine_clerical_gender_male" };
            Assert.AreEqual(50f, DoctrineDivergence.Calculate(a, b, Options()), 0.0001f);
        }

        [Test]
        public void SequenceDifference_UsesLevelDistance()
        {
            var a = new List<string> { "doctrine_clerical_gender_male" };
            var b = new List<string> { "doctrine_clerical_gender_either" };
            Assert.AreEqual(100f, DoctrineDivergence.Calculate(a, b, Options()), 0.0001f);

            var c = new List<string> { "doctrine_divorce_forbidden" };
            var d = new List<string> { "doctrine_divorce_permitted" };
            Assert.AreEqual(50f, DoctrineDivergence.Calculate(c, d, Options()), 0.0001f);
        }

        [Test]
        public void PartialCategoryOverlap_UsesUnion()
        {
            var a = new List<string> { "doctrine_homosexuality_crime", "doctrine_adultery_crime" };
            var b = new List<string> { "doctrine_homosexuality_crime", "doctrine_adultery_shunned" };
            // ethics 组：并集 3 项，交集 1 项 ⇒ 1 - 1/3
            Assert.AreEqual(66.6667f, DoctrineDivergence.Calculate(a, b, Options()), 0.01f);
        }

        [Test]
        public void MissingGroupOnOneSide_CountsAsFullDifference()
        {
            var a = new List<string> { "doctrine_homosexuality_crime" };
            var b = new List<string> { "doctrine_clerical_gender_male" };
            Assert.AreEqual(100f, DoctrineDivergence.Calculate(a, b, Options()), 0.0001f);
        }
    }
}
