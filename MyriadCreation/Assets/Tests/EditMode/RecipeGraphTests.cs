using NUnit.Framework;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Content.Modding;
using MyriadCreation.Simulation.ContentHost;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>物资合成表：矿石→金属→合金/钢，木材→加工木材/木炭，棉花→布。</summary>
    public class RecipeGraphTests
    {
        [SetUp]
        public void Setup() { ContentRegistry.Reset(); ContentRegistry.Initialize(); }

        [Test]
        public void MaterialChain_Exists()
        {
            Assert.GreaterOrEqual(RecipeCatalog.Count, 8);
            Assert.IsTrue(RecipeCatalog.TryGet(52, out var iron), "铁矿→铁");
            Assert.AreEqual(50, iron.inputs[0].goodsId);
            Assert.IsTrue(RecipeCatalog.TryGet(57, out var steel), "铁+木炭→钢");
            Assert.IsTrue(steel.inputs.Exists(i => i.goodsId == 52));
            Assert.IsTrue(RecipeCatalog.TryGet(56, out var bronze), "铜+锡→青铜");
            Assert.IsTrue(RecipeCatalog.TryGet(12, out var cloth), "棉花→布");
            Assert.IsTrue(RecipeCatalog.TryGet(100, out var flour), "小麦→面粉");
            Assert.AreEqual(0, flour.inputs[0].goodsId);
            Assert.IsTrue(RecipeCatalog.TryGet(101, out var rice), "稻谷→米");
            Assert.AreEqual(10, rice.inputs[0].goodsId);
        }

        [Test]
        public void FiberChains_FlaxReedPapyrus()
        {
            Assert.IsTrue(RecipeCatalog.TryGet(120, out var linen), "亚麻→麻布");
            Assert.AreEqual(110, linen.inputs[0].goodsId);
            Assert.IsTrue(RecipeCatalog.TryGet(121, out var papyrus), "莎草→莎草纸");
            Assert.AreEqual(112, papyrus.inputs[0].goodsId);
            Assert.IsTrue(RecipeCatalog.TryGet(122, out var mat), "芦苇→苇席");
            Assert.AreEqual(111, mat.inputs[0].goodsId);
        }

        [Test]
        public void HideCeramicGlass_Chains()
        {
            Assert.IsTrue(RecipeCatalog.TryGet(13, out var leather), "生皮→皮革");
            Assert.AreEqual(130, leather.inputs[0].goodsId);
            Assert.IsTrue(RecipeCatalog.TryGet(141, out var pottery), "黏土→陶器");
            Assert.AreEqual(140, pottery.inputs[0].goodsId);
            Assert.IsTrue(RecipeCatalog.TryGet(142, out var lime), "石料→石灰");
            Assert.IsTrue(RecipeCatalog.TryGet(143, out var glass), "石料+木炭→玻璃");
        }

        [Test]
        public void RecipeMetadata_HasProcessAndInnovation()
        {
            Assert.IsTrue(RecipeCatalog.TryGet(52, out var iron));
            Assert.AreEqual(202, iron.requiredInnovation);
            Assert.AreEqual(CraftProcess.Smelting, iron.process);
            Assert.IsTrue(RecipeCatalog.TryGet(121, out var papyrus));
            Assert.AreEqual(947, papyrus.requiredInnovation);
            Assert.AreEqual(CraftProcess.Papermaking, papyrus.process);
        }

        [Test]
        public void WoodAndCharcoal_Chains()
        {
            Assert.IsTrue(RecipeCatalog.TryGet(31, out var wood), "原木→加工木材");
            Assert.AreEqual(30, wood.inputs[0].goodsId);
            Assert.IsTrue(RecipeCatalog.TryGet(14, out var charcoal), "原木→木炭");
            Assert.AreEqual(30, charcoal.inputs[0].goodsId);
        }
    }
}
