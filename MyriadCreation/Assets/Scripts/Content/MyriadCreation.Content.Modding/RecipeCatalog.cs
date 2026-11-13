using System;
using System.Collections.Generic;
using System.IO;
using MyriadCreation.Core;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Content.Modding
{
    /// <summary>
    /// 数据驱动配方表（MC 式）：配方按稳定 id 存放，支持同一产物多条替代配方；
    /// Base 先加载，Mods 同 id 覆盖；新增配方只需加 JSON，不改代码。
    /// </summary>
    public static class RecipeCatalog
    {
        public const string RelativePath = "Crafting/Recipes.json";

        private static readonly Dictionary<string, GoodsRecipe> _byId = new Dictionary<string, GoodsRecipe>();
        private static readonly Dictionary<int, GoodsRecipe> _byOutput = new Dictionary<int, GoodsRecipe>();
        private static readonly List<string> _order = new List<string>();

        public static bool IsLoaded { get; private set; }
        public static int Count => _byId.Count;

        public static void Initialize(string streamingAssetsRoot)
        {
            Reset();
            if (string.IsNullOrEmpty(streamingAssetsRoot) || !Directory.Exists(streamingAssetsRoot))
            {
                IsLoaded = true;
                return;
            }
            LoadFile(ContentPaths.BasePath(streamingAssetsRoot, RelativePath));
            LoadFile(ContentPaths.ModsPath(streamingAssetsRoot, RelativePath));
            IsLoaded = true;
            Debug.Log((object)$"[RecipeCatalog] 加载 {_byId.Count} 条配方");
        }

        public static void Reset()
        {
            _byId.Clear();
            _byOutput.Clear();
            _order.Clear();
            IsLoaded = false;
        }

        public static bool TryGet(string recipeId, out GoodsRecipe recipe)
        {
            return _byId.TryGetValue(recipeId ?? string.Empty, out recipe);
        }

        /// <summary>按产物取一条配方（兼容旧调用；同一产物有多条时取首条）。</summary>
        public static bool TryGet(int outputGoodsId, out GoodsRecipe recipe)
        {
            return _byOutput.TryGetValue(outputGoodsId, out recipe);
        }

        public static List<GoodsRecipe> GetByOutput(int outputGoodsId)
        {
            var result = new List<GoodsRecipe>();
            foreach (var id in _order)
            {
                if (_byId.TryGetValue(id, out var r) && r.outputKind == RecipeOutputKind.Good && r.outputGoodsId == outputGoodsId)
                    result.Add(r);
            }
            return result;
        }

        public static IReadOnlyList<GoodsRecipe> GetAll()
        {
            var result = new List<GoodsRecipe>(_order.Count);
            foreach (var id in _order)
                if (_byId.TryGetValue(id, out var r)) result.Add(r);
            return result;
        }

        public static bool CanCraft(GoodsRecipe recipe, IReadOnlyDictionary<int, float> inventory)
        {
            if (recipe?.inputs == null || recipe.inputs.Count == 0 || inventory == null)
                return false;
            foreach (RecipeIngredient input in recipe.inputs)
            {
                if (!inventory.TryGetValue(input.goodsId, out var value))
                    return false;
                if (value + 0.0001f < input.amount)
                    return false;
            }
            return true;
        }

        private static void LoadFile(string path)
        {
            if (!File.Exists(path))
                return;
            try
            {
                RecipeArtifact artifact = JsonUtility.FromJson<RecipeArtifact>(File.ReadAllText(path));
                if (artifact?.recipes == null)
                    return;
                foreach (GoodsRecipe recipe in artifact.recipes)
                {
                    if (!TryNormalize(recipe, out string id, out string error))
                    {
                        Debug.LogWarning((object)("[RecipeCatalog] 配方非法：" + error));
                        continue;
                    }
                    if (!_byId.ContainsKey(id)) _order.Add(id);
                    _byId[id] = recipe;
                    RebuildOutputIndex();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning((object)("[RecipeCatalog] 读取异常 " + path + ": " + ex.Message));
            }
        }

        private static void RebuildOutputIndex()
        {
            _byOutput.Clear();
            foreach (var id in _order)
            {
                if (!_byId.TryGetValue(id, out var r)) continue;
                if (r.outputKind != RecipeOutputKind.Good || r.outputGoodsId < 0) continue;
                if (!_byOutput.ContainsKey(r.outputGoodsId)) _byOutput[r.outputGoodsId] = r;
            }
        }

        private static bool TryNormalize(GoodsRecipe recipe, out string id, out string error)
        {
            id = string.Empty;
            error = string.Empty;
            if (recipe == null) { error = "null"; return false; }
            if (recipe.outputKind != RecipeOutputKind.Good)
            {
                // 其他输出类型先保留，等对应系统接入；当前不进入 Goods 产物索引。
            }
            if (recipe.outputGoodsId < 0) { error = "输出 id < 0"; return false; }
            if (recipe.inputs == null || recipe.inputs.Count == 0) { error = "输入为空"; return false; }
            foreach (var input in recipe.inputs)
            {
                if (input.goodsId < 0 || input.amount <= 0f) { error = "输入非法"; return false; }
                if (recipe.outputKind == RecipeOutputKind.Good && input.goodsId == recipe.outputGoodsId)
                { error = "自引用"; return false; }
            }
            id = string.IsNullOrEmpty(recipe.recipeId)
                ? "good_" + recipe.outputGoodsId
                : recipe.recipeId;
            recipe.recipeId = id;
            return true;
        }
    }
}
