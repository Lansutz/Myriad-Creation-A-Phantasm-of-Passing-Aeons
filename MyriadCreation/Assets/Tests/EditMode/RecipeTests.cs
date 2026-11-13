using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Content.Modding;
using MyriadCreation.Simulation.ContentHost;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>多输入配方：青铜 = 铜 + 锡，不能再按单输入处理。</summary>
    public class RecipeTests
    {
        [SetUp]
        public void Setup()
        {
            ContentRegistry.Reset();
            ContentRegistry.Initialize();
        }

        [TearDown]
        public void TearDown() => ContentRegistry.Reset();

        [Test]
        public void BronzeRecipe_RequiresCopperAndTin()
        {
            Assert.GreaterOrEqual(RecipeCatalog.Count, 1);
            Assert.IsTrue(RecipeCatalog.TryGet(56, out var bronze), "青铜应有配方");
            Assert.AreEqual(2, bronze.inputs.Count);
            Assert.IsTrue(bronze.inputs.Exists(i => i.goodsId == 53), "青铜需要铜");
            Assert.IsTrue(bronze.inputs.Exists(i => i.goodsId == 55), "青铜需要锡");
        }

        [Test]
        public void CanCraft_BronzeNeedsBothMetals()
        {
            RecipeCatalog.TryGet(56, out var bronze);
            var onlyCopper = new Dictionary<int, float> { [53] = 10f };
            var copperAndTin = new Dictionary<int, float> { [53] = 10f, [55] = 10f };
            var insufficientTin = new Dictionary<int, float> { [53] = 10f, [55] = 0.1f };

            Assert.IsFalse(RecipeCatalog.CanCraft(bronze, onlyCopper), "只有铜不能炼青铜");
            Assert.IsTrue(RecipeCatalog.CanCraft(bronze, copperAndTin), "铜+锡齐全才能炼青铜");
            Assert.IsFalse(RecipeCatalog.CanCraft(bronze, insufficientTin), "锡不足不能炼青铜");
        }

        [Test]
        public void CanCraft_EmptyInventoryOrNullRecipe_IsFalse()
        {
            RecipeCatalog.TryGet(56, out var bronze);
            Assert.IsFalse(RecipeCatalog.CanCraft(bronze, new Dictionary<int, float>()));
            Assert.IsFalse(RecipeCatalog.CanCraft(null, new Dictionary<int, float> { [53] = 1f, [55] = 1f }));
        }

        [Test]
        public void Recipes_DoNotReferenceThemselves()
        {
            foreach (var output in new[] { 56 })
            {
                if (!RecipeCatalog.TryGet(output, out var recipe)) continue;
                Assert.IsFalse(recipe.inputs.Exists(i => i.goodsId == output), "配方不能自引用");
            }
        }
    }
}
