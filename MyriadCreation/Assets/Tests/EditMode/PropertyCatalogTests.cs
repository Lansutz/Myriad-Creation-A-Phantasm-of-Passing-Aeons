using System.Collections.Generic;
using MyriadCreation.Core;
using NUnit.Framework;
using MyriadCreation.Content.Modding;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Simulation.State;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 可模组化属性定义（B1）：定义目录、物产属性引用、未定义属性拒绝、革新引用属性校验。
    /// </summary>
    public class PropertyCatalogTests
    {
        private InnovationTree _tree;

        [SetUp]
        public void Setup()
        {
            ContentRegistry.Reset();
            ContentRegistry.Initialize();
            _tree = new InnovationTree();
        }

        [TearDown]
        public void TearDown()
        {
            ContentRegistry.Reset();
            ContentRegistry.Initialize();
        }

        [Test]
        public void Definitions_LoadAndDescribePhysicalProperties()
        {
            Assert.GreaterOrEqual(PropertyDefinitionCatalog.Count, 4);
            Assert.IsTrue(PropertyDefinitionCatalog.TryGet("meltingPoint", out var melting));
            Assert.AreEqual("°C", melting.unit);
            Assert.AreEqual("physical", melting.kind);
            Assert.IsTrue(melting.higherIsBetter);
            Assert.IsTrue(PropertyDefinitionCatalog.TryGet("hardness", out var hardness));
            CollectionAssert.Contains(hardness.tags, "mechanical");
            Assert.IsTrue(PropertyDefinitionCatalog.TryGet("ductility", out var ductility));
            Assert.AreEqual("延展性", ductility.name);
            Assert.IsTrue(GoodsPropertyCatalog.TryGetValue(60, "ductility", out float goldDuctility));
            Assert.IsTrue(GoodsPropertyCatalog.TryGetValue(52, "ductility", out float ironDuctility));
            Assert.Greater(goldDuctility, ironDuctility, "金的延展性高于铁");
        }

        [Test]
        public void Goods_ReferenceDefinedProperties()
        {
            Assert.GreaterOrEqual(GoodsPropertyCatalog.Count, 25);
            Assert.IsTrue(GoodsPropertyCatalog.TryGetValue(52, "meltingPoint", out float ironMelt));
            Assert.AreEqual(1538f, ironMelt, 0.1f);
            Assert.IsTrue(GoodsPropertyCatalog.TryGetValue(57, "hardness", out float steelHardness));
            Assert.IsTrue(GoodsPropertyCatalog.TryGetValue(52, "hardness", out float ironHardness));
            Assert.Greater(steelHardness, ironHardness);
            Assert.IsTrue(GoodsPropertyCatalog.TryGetValue(30, "fuelValue", out float woodFuel));
            Assert.Greater(woodFuel, 0f);
            Assert.IsTrue(GoodsPropertyCatalog.TryGetValue(0, "meltingPoint", out float foodMelt));
            Assert.AreEqual(0f, foodMelt);
        }

        [Test]
        public void ModsCanAddDefinedProperty()
        {
            // 模组新增属性：必须同时提供定义（本测试用临时目录模拟 Base 内容）
            string root = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), "mc-props-" + System.Guid.NewGuid().ToString("N"));
            try
            {
                string defsDir = System.IO.Path.Combine(root, ContentPaths.Base, "Properties");
                string goodsDir = System.IO.Path.Combine(root, ContentPaths.Base, "Goods");
                System.IO.Directory.CreateDirectory(defsDir);
                System.IO.Directory.CreateDirectory(goodsDir);
                System.IO.File.WriteAllText(System.IO.Path.Combine(defsDir, "PropertyDefinitions.json"),
                    "{\"properties\":[{\"id\":\"shininess\",\"name\":\"光泽\",\"unit\":\"0-10\","
                    + "\"kind\":\"physical\",\"aggregation\":\"max\",\"higherIsBetter\":true}]}");
                System.IO.File.WriteAllText(System.IO.Path.Combine(goodsDir, "GoodsProperties.json"),
                    "{\"entries\":[{\"goodsId\":500,\"properties\":[{\"id\":\"shininess\",\"value\":7}]},"
                    + "{\"goodsId\":501,\"properties\":[{\"id\":\"undefinedProperty\",\"value\":3}]}]}");

                PropertyDefinitionCatalog.Initialize(root);
                GoodsPropertyCatalog.Initialize(root);

                Assert.IsTrue(PropertyDefinitionCatalog.IsKnown("shininess"));
                Assert.IsTrue(GoodsPropertyCatalog.TryGetValue(500, "shininess", out float shininess));
                Assert.AreEqual(7f, shininess);
                Assert.IsFalse(GoodsPropertyCatalog.TryGetValue(501, "undefinedProperty", out _),
                    "未定义属性应被拒绝");
            }
            finally
            {
                if (System.IO.Directory.Exists(root)) System.IO.Directory.Delete(root, true);
            }
        }

        [Test]
        public void InnovationAssociatedGoods_AreDefined()
        {
            foreach (var pair in ContentRegistry.Innovations)
            {
                if (pair.Value.associatedGoods == null) continue;
                foreach (int goodsId in pair.Value.associatedGoods)
                {
                    Assert.IsTrue(GoodsPropertyCatalog.TryGet(goodsId, out var properties) && properties != null,
                        $"革新 {pair.Key} 关联的物产 {goodsId} 没有属性记录");
                }
            }
        }

        [Test]
        public void GoodsDef_ExposesPropertyLookup()
        {
            var goods = new GoodsDef { goodsId = 52 };
            goods.properties = new List<GoodsPropertyValue>
            {
                new GoodsPropertyValue { id = "meltingPoint", value = 1538f },
            };
            Assert.IsTrue(goods.TryGetProperty("meltingPoint", out float value));
            Assert.AreEqual(1538f, value);
            Assert.IsFalse(goods.TryGetProperty("hardness", out _));
        }
    }
}
