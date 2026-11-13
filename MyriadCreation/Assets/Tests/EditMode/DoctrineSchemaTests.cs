using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P2.2 教义 schema：组→档、域/序、参数注册表与内容校验。</summary>
    public class DoctrineSchemaTests
    {
        [Test]
        public void GroupOf_MapsPrefixFamilies()
        {
            Assert.AreEqual("ethics", DoctrineSchema.GroupOf("doctrine_homosexuality_crime"));
            Assert.AreEqual("ethics", DoctrineSchema.GroupOf("doctrine_witchcraft_shunned"));
            Assert.AreEqual("marriage", DoctrineSchema.GroupOf("doctrine_divorce_forbidden"));
            Assert.AreEqual("funeral", DoctrineSchema.GroupOf("doctrine_funeral_cremation"));
            Assert.AreEqual("clerical_gender", DoctrineSchema.GroupOf("doctrine_clerical_gender_male"));
            Assert.AreEqual("clerical_marriage", DoctrineSchema.GroupOf("doctrine_clerical_marriage_celibate"));
            Assert.AreEqual("clerical_appointment", DoctrineSchema.GroupOf("doctrine_clerical_appointment_temporal"));
            Assert.AreEqual("ritual_devotion", DoctrineSchema.GroupOf("doctrine_pilgrimage"));
            Assert.AreEqual("sacred_material", DoctrineSchema.GroupOf("doctrine_relics"));
            Assert.AreEqual("theology", DoctrineSchema.GroupOf("doctrine_monotheism"));
        }

        [Test]
        public void Group_HasDomainAndKind()
        {
            Assert.AreEqual("justice", DoctrineSchema.DomainOf("ethics"));
            Assert.AreEqual("marriage", DoctrineSchema.DomainOf("marriage"));
            Assert.AreEqual("sequence", DoctrineSchema.KindOf("marriage"));
            Assert.AreEqual("category", DoctrineSchema.KindOf("funeral"));
            Assert.AreEqual("sequence", DoctrineSchema.KindOf("clerical_gender"));
            Assert.AreEqual("", DoctrineSchema.DomainOf("nope"));
        }

        [Test]
        public void ValidateOption_RejectsUnregisteredParameter_AndAcceptsSourced()
        {
            var bad = new DoctrineOptionDef
            {
                optionId = "doctrine_divorce_permitted",
                parameters = new List<DoctrineParameterDef>
                {
                    new DoctrineParameterDef { key = "made.up.key", value = 1f, source = "TODO_M05" }
                }
            };
            Assert.IsFalse(DoctrineSchema.ValidateOption(bad, out string error));
            StringAssert.Contains("unregistered", error);

            var good = new DoctrineOptionDef
            {
                optionId = "doctrine_divorce_permitted",
                parameters = new List<DoctrineParameterDef>
                {
                    new DoctrineParameterDef { key = "marriage.consanguinity_degree", value = 3f, source = "TODO_M05" }
                }
            };
            Assert.IsTrue(DoctrineSchema.ValidateOption(good, out _));

            var noSource = new DoctrineOptionDef
            {
                optionId = "doctrine_divorce_permitted",
                parameters = new List<DoctrineParameterDef>
                {
                    new DoctrineParameterDef { key = "marriage.consanguinity_degree", value = 3f }
                }
            };
            Assert.IsFalse(DoctrineSchema.ValidateOption(noSource, out error));
            StringAssert.Contains("no source", error);
        }

        [Test]
        public void ValidateOption_RejectsUnknownExplicitGroup()
        {
            var option = new DoctrineOptionDef { optionId = "doctrine_monotheism", groupId = "not_a_group" };
            Assert.IsFalse(DoctrineSchema.ValidateOption(option, out string error));
            StringAssert.Contains("unknown doctrine group", error);
        }

        [Test]
        public void EveryShippedDoctrineOptionMapsToKnownGroup()
        {
            string path = Path.Combine(Application.dataPath, "StreamingAssets/Base/Religion/Doctrines.json");
            Assert.IsTrue(File.Exists(path), "找不到 Doctrines.json: " + path);
            string text = File.ReadAllText(path);
            MatchCollection matches = Regex.Matches(text, "\"optionId\"\\s*:\\s*\"([^\"]+)\"");
            Assert.Greater(matches.Count, 40, "内容表选项数异常");
            foreach (Match match in matches)
            {
                string optionId = match.Groups[1].Value;
                string groupId = DoctrineSchema.GroupOf(optionId);
                Assert.IsTrue(DoctrineSchema.IsKnownGroup(groupId), $"未归组：{optionId} → {groupId}");
            }
        }
    }
}
