using System;
using System.Collections.Generic;

namespace MyriadCreation.Content.Definitions
{
[Serializable]
public class GoodsRecipe
{
	/// <summary>稳定配方 id（模组覆盖键）；空则由 outputGoodsId 生成。</summary>
	public string recipeId = string.Empty;

	public string displayName = string.Empty;

	public RecipeOutputKind outputKind = RecipeOutputKind.Good;

	public int outputGoodsId;

	public float outputAmount = 1f;

	public List<RecipeIngredient> inputs = new List<RecipeIngredient>();

	/// <summary>所需革新 id；0=无要求（当前配方系统尚未在运行时校验，字段先落数据）。</summary>
	public int requiredInnovation;

	/// <summary>所需建筑/设施 id；0=无要求。</summary>
	public int requiredBuilding;

	/// <summary>工时（天）；0=未配置。</summary>
	public float craftDays;

	/// <summary>工艺大类；细分写 processDetail。</summary>
	public CraftProcess process = CraftProcess.None;

	public string processDetail = string.Empty;
}
}
