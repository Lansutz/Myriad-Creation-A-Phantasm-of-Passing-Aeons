using MyriadCreation.Simulation.Economy;
using MyriadCreation.Content.Modding;
using MyriadCreation.World.Settlement;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.State;
using System;

namespace MyriadCreation.Simulation.Systems
{
public static class SettlementProduction
{
	public static bool TryCraft(SettlementMarket market, int outputGoodsId, out float produced)
	{
		return TryCraftCore(market, outputGoodsId, out produced, null, -1, null, enforceGates: false);
	}

	/// <summary>
	/// 带科技/设施门禁的合成；旧三参调用保持“只校验输入/容量”的行为。
	/// </summary>
	public static bool TryCraft(SettlementMarket market, int outputGoodsId, out float produced, InnovationTree innovations, int realmId, Func<int, bool> hasBuilding)
	{
		return TryCraftCore(market, outputGoodsId, out produced, innovations, realmId, hasBuilding, enforceGates: true);
	}

	private static bool TryCraftCore(SettlementMarket market, int outputGoodsId, out float produced, InnovationTree innovations, int realmId, Func<int, bool> hasBuilding, bool enforceGates)
	{
		produced = 0f;
		if (market == null)
		{
			return false;
		}
		if (!RecipeCatalog.TryGet(outputGoodsId, out var recipe) && !DynamicGoodsRegistry.TryGetRecipe(outputGoodsId, out recipe))
		{
			return false;
		}
		if (!RecipeCatalog.CanCraft(recipe, market.inventory))
		{
			return false;
		}
		if (enforceGates)
		{
			if (recipe.requiredInnovation > 0 && (innovations == null || !innovations.HasInnovation(realmId, recipe.requiredInnovation)))
			{
				return false;
			}
			if (innovations != null && !innovations.IsUnlockGranted(realmId, InnovationUnlockKind.Recipe, outputGoodsId))
			{
				return false;
			}
			if (recipe.requiredBuilding > 0 && hasBuilding != null && !hasBuilding(recipe.requiredBuilding))
			{
				return false;
			}
		}
		if (market.TotalStock() + recipe.outputAmount > market.capacity + 0.0001f)
		{
			return false;
		}
		foreach (RecipeIngredient input in recipe.inputs)
		{
			if (!market.TryTake(input.goodsId, input.amount))
			{
				return false;
			}
		}
		market.AddStock(outputGoodsId, recipe.outputAmount);
		produced = recipe.outputAmount;
		if (innovations != null && recipe.requiredInnovation > 0)
		{
			InnovationDef def = innovations.GetInnovation(recipe.requiredInnovation);
			if (def != null) innovations.AddPractice(realmId, def.field, produced * 0.1f);
		}
		return true;
	}
}
}
