using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.ContentHost;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P11.3：教义分组按宗教学术语扩出风俗（customs），扩充伦理；每项必须有绑定点，
    /// 有消费者的（饮酒/弑亲/放贷）必须真的改变司法判定。
    /// </summary>
    public class DoctrineCustomsTests
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            ContentRegistry.Initialize();
        }

        [Test]
        public void EveryDoctrineOption_HasValidBinding()
        {
            var missing = new List<string>();
            foreach (DoctrineOptionDef option in ContentResolvers.Doctrines.All)
            {
                if (option == null) continue;
                if (!DoctrineSchema.IsKnownBinding(option.bindsTo))
                {
                    missing.Add($"{option.optionId}='{option.bindsTo}'");
                }
            }
            Assert.IsEmpty(missing, "以下教义没有合法绑定点：" + string.Join(", ", missing));
        }

        [Test]
        public void CustomsGroup_IsRecognizedAndValidated()
        {
            Assert.AreEqual("customs", DoctrineSchema.GroupOf("doctrine_drinking_forbidden"));
            Assert.IsTrue(DoctrineSchema.IsKnownGroup("customs"));
            Assert.AreEqual("ethics", DoctrineSchema.GroupOf("doctrine_kinslaying_crime"));

            foreach (string id in new[]
            {
                "doctrine_drinking_forbidden", "doctrine_feast_communal", "doctrine_sabbath_rest",
                "doctrine_hospitality_guest", "doctrine_kinslaying_crime", "doctrine_usury_crime"
            })
            {
                Assert.IsTrue(ContentResolvers.Doctrines.TryGet(id, out DoctrineOptionDef option), id);
                Assert.IsTrue(DoctrineSchema.ValidateOption(option, out string error), $"{id}: {error}");
            }
        }

        [Test]
        public void DrinkingForbidden_IsCriminal()
        {
            DoctrineBindings bindings = DoctrineBindings.Resolve(new[] { "doctrine_drinking_forbidden" });
            Assert.AreEqual(DoctrineMoralStance.Crime, bindings.drinking);
            Assert.IsTrue(DoctrineBindingHost.IsCrime(bindings, DoctrineCrimeBehavior.Drinking));
            Assert.IsFalse(DoctrineBindingHost.IsCrime(bindings, DoctrineCrimeBehavior.Adultery));
        }

        [Test]
        public void RestrainedAndPermitted_AreNotCriminal()
        {
            DoctrineBindings restrained = DoctrineBindings.Resolve(new[] { "doctrine_drinking_restrained" });
            Assert.AreEqual(DoctrineMoralStance.Shunned, restrained.drinking);
            Assert.IsFalse(DoctrineBindingHost.IsCrime(restrained, DoctrineCrimeBehavior.Drinking));

            DoctrineBindings permitted = DoctrineBindings.Resolve(new[] { "doctrine_drinking_permitted" });
            Assert.AreEqual(DoctrineMoralStance.Accepted, permitted.drinking);
        }

        [Test]
        public void KinslayingAndUsury_BindToCrime()
        {
            DoctrineBindings bindings = DoctrineBindings.Resolve(new[]
            {
                "doctrine_kinslaying_crime", "doctrine_usury_shunned"
            });
            Assert.IsTrue(DoctrineBindingHost.IsCrime(bindings, DoctrineCrimeBehavior.Kinslaying));
            Assert.AreEqual(DoctrineMoralStance.Shunned,
                DoctrineBindingHost.GetStance(bindings, DoctrineCrimeBehavior.Usury));
            Assert.IsFalse(DoctrineBindingHost.IsCrime(bindings, DoctrineCrimeBehavior.Usury),
                "忌讳不入罪");
        }
    }
}
