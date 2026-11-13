using System.IO;
using NUnit.Framework;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Content.Modding;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>MC 式配方表：稳定 recipeId、同一产物可多配方、Mods 同 id 覆盖。</summary>
    public class RecipeSystemTests
    {
        [SetUp]
        public void Setup() { RecipeCatalog.Reset(); }
        [TearDown]
        public void TearDown() { RecipeCatalog.Reset(); }

        [Test]
        public void Recipes_HaveStableIds()
        {
            RecipeCatalog.Initialize(UnityEngine.Application.streamingAssetsPath);
            Assert.Greater(RecipeCatalog.Count, 10);
            Assert.IsTrue(RecipeCatalog.TryGet("smelt_iron", out var iron));
            Assert.AreEqual(52, iron.outputGoodsId);
            Assert.IsTrue(RecipeCatalog.TryGet("alloy_bronze", out var bronze));
            Assert.AreEqual(2, bronze.inputs.Count);
            Assert.IsFalse(RecipeCatalog.TryGet("missing_recipe", out _));
            Assert.GreaterOrEqual(RecipeCatalog.GetByOutput(52).Count, 1);
        }

        [Test]
        public void Mods_OverrideRecipeById()
        {
            string root = Path.Combine(Path.GetTempPath(), "mc-recipe-mod-test-" + System.Guid.NewGuid().ToString("N"));
            string baseDir = Path.Combine(root, "Base", "Crafting");
            string modDir = Path.Combine(root, "Mods", "Crafting");
            Directory.CreateDirectory(baseDir);
            Directory.CreateDirectory(modDir);
            File.WriteAllText(Path.Combine(baseDir, "Recipes.json"),
                "{\"recipes\":[{\"recipeId\":\"r1\",\"outputGoodsId\":52,\"outputAmount\":1,\"inputs\":[{\"goodsId\":50,\"amount\":1}]}]}");
            File.WriteAllText(Path.Combine(modDir, "Recipes.json"),
                "{\"recipes\":[{\"recipeId\":\"r1\",\"outputGoodsId\":57,\"outputAmount\":2,\"inputs\":[{\"goodsId\":52,\"amount\":1}]}]}");
            try
            {
                RecipeCatalog.Initialize(root);
                Assert.IsTrue(RecipeCatalog.TryGet("r1", out var recipe));
                Assert.AreEqual(57, recipe.outputGoodsId);
                Assert.AreEqual(2f, recipe.outputAmount, 0.001f);
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }
    }
}
