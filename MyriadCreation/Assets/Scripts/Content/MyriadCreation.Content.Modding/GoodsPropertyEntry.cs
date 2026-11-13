using System;
using System.Collections.Generic;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Content.Modding
{
[Serializable]
public class GoodsPropertyEntry
{
	public int goodsId;

	public List<GoodsPropertyValue> properties = new List<GoodsPropertyValue>();
}
}
