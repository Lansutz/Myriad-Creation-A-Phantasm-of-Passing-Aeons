using System;
using System.Collections.Generic;
using MyriadCreation.Core.Enums;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Economy
{
public static class DynamicGoodsRegistry
{
	public const int FirstDynamicGoodsId = 10000;

	private static readonly Dictionary<int, GoodsDef> _goods = new Dictionary<int, GoodsDef>();

	private static readonly Dictionary<int, GoodsRecipe> _recipes = new Dictionary<int, GoodsRecipe>();

	private static int _nextId = 10000;

	public static int Count => _goods.Count;

	public static IReadOnlyDictionary<int, GoodsDef> All => _goods;

	public static void Clear()
	{
		_goods.Clear();
		_recipes.Clear();
		_nextId = 10000;
	}

	public static bool TryGet(int goodsId, out GoodsDef def)
	{
		return _goods.TryGetValue(goodsId, out def);
	}

	public static bool TryGetRecipe(int goodsId, out GoodsRecipe recipe)
	{
		return _recipes.TryGetValue(goodsId, out recipe);
	}

	public static int Register(GoodsDef def, GoodsRecipe recipe = null)
	{
		if (def.goodsId < 10000)
		{
			return -1;
		}
		_goods[def.goodsId] = def;
		if (recipe != null)
		{
			_recipes[def.goodsId] = recipe;
		}
		if (def.goodsId >= _nextId)
		{
			_nextId = def.goodsId + 1;
		}
		return def.goodsId;
	}

	public static bool TryCreateAlloy(IReadOnlyList<GoodsDef> inputs, IReadOnlyList<float> amounts, bool hasAlloyTech, out int newGoodsId, string nameOverride = null)
	{
		newGoodsId = -1;
		if (!hasAlloyTech || inputs == null || amounts == null || inputs.Count < 2 || inputs.Count != amounts.Count)
		{
			return false;
		}
		float num = 0f;
		GoodsRecipe goodsRecipe = new GoodsRecipe
		{
			outputAmount = 1f,
			inputs = new List<RecipeIngredient>()
		};
		Dictionary<string, float> dictionary = new Dictionary<string, float>();
		float num2 = 0f;
		float num3 = 0f;
		bool flag = true;
		List<string> list = new List<string>();
		for (int i = 0; i < inputs.Count; i++)
		{
			GoodsDef goodsDef = inputs[i];
			float num4 = amounts[i];
			if (goodsDef.goodsId < 0 || num4 <= 0f)
			{
				return false;
			}
			if (goodsDef.category != GameEnums.GoodsCategory.Metal)
			{
				flag = false;
			}
			num += num4;
			num2 += goodsDef.weight * num4;
			num3 += goodsDef.baseValue * num4;
			if (!string.IsNullOrEmpty(goodsDef.goodsName))
			{
				list.Add(goodsDef.goodsName);
			}
			goodsRecipe.inputs.Add(new RecipeIngredient
			{
				goodsId = goodsDef.goodsId,
				amount = num4
			});
			if (goodsDef.properties == null)
			{
				continue;
			}
			foreach (GoodsPropertyValue property in goodsDef.properties)
			{
				if (!string.IsNullOrEmpty(property.id))
				{
					dictionary.TryGetValue(property.id, out var value);
					dictionary[property.id] = value + property.value * num4;
				}
			}
		}
		if (num <= 0f || !flag)
		{
			return false;
		}
		int num5 = (goodsRecipe.outputGoodsId = _nextId++);
		string goodsName = (string.IsNullOrEmpty(nameOverride) ? (string.Join("-", list) + (flag ? "合金" : "合成物")) : nameOverride);
		List<GoodsPropertyValue> list2 = new List<GoodsPropertyValue>();
		foreach (KeyValuePair<string, float> item in dictionary)
		{
			list2.Add(new GoodsPropertyValue
			{
				id = item.Key,
				value = item.Value / num
			});
		}
		GoodsDef value2 = new GoodsDef
		{
			goodsId = num5,
			goodsName = goodsName,
			category = (flag ? GameEnums.GoodsCategory.Metal : GameEnums.GoodsCategory.Luxury),
			baseValue = Math.Max(0.01f, num3 / num),
			weight = num2 / num,
			processedFromId = -1,
			isNaturalResource = false,
			renewable = false,
			properties = list2
		};
		_goods[num5] = value2;
		_recipes[num5] = goodsRecipe;
		newGoodsId = num5;
		return true;
	}

	public static List<DynamicGoodsSaveData> Capture()
	{
		List<DynamicGoodsSaveData> list = new List<DynamicGoodsSaveData>();
		foreach (KeyValuePair<int, GoodsDef> good in _goods)
		{
			GoodsDef value = good.Value;
			_recipes.TryGetValue(value.goodsId, out var value2);
			list.Add(new DynamicGoodsSaveData
			{
				goodsId = value.goodsId,
				goodsName = value.goodsName,
				category = (int)value.category,
				baseValue = value.baseValue,
				weight = value.weight,
				properties = ((value.properties != null) ? new List<GoodsPropertyValue>(value.properties) : new List<GoodsPropertyValue>()),
				recipe = value2
			});
		}
		return list;
	}

	public static void Restore(IEnumerable<DynamicGoodsSaveData> data)
	{
		Clear();
		if (data == null)
		{
			return;
		}
		foreach (DynamicGoodsSaveData datum in data)
		{
			if (datum != null && datum.goodsId >= 10000)
			{
				GoodsDef value = new GoodsDef
				{
					goodsId = datum.goodsId,
					goodsName = (datum.goodsName ?? string.Empty),
					category = (GameEnums.GoodsCategory)datum.category,
					baseValue = datum.baseValue,
					weight = datum.weight,
					processedFromId = -1,
					isNaturalResource = false,
					renewable = false,
					properties = ((datum.properties != null) ? new List<GoodsPropertyValue>(datum.properties) : new List<GoodsPropertyValue>())
				};
				_goods[value.goodsId] = value;
				if (datum.recipe != null)
				{
					_recipes[value.goodsId] = datum.recipe;
				}
				if (value.goodsId >= _nextId)
				{
					_nextId = value.goodsId + 1;
				}
			}
		}
	}
}
}
