using System;
using System.Collections.Generic;

namespace MyriadCreation.Content.Definitions
{
[Serializable]
public class DynamicGoodsSaveData
{
	public int goodsId;

	public string goodsName;

	public int category;

	public float baseValue;

	public float weight;

	public List<GoodsPropertyValue> properties = new List<GoodsPropertyValue>();

	public GoodsRecipe recipe;
}
}
