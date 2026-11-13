using System;
using System.Collections.Generic;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.ContentHost
{
public static class ContentQueries
{
	public static string GetRandomName(ContentRegistry.CultureContentPack pack, int type, Random rng = null)
	{
		// 确定性优先：未传 rng 时固定种子（原 new Random() 用时间种子 ⇒ 同 seed 不同结果）。
		// TODO(M1)：调用方传世界 seed 派生的 rng。
		rng = rng ?? new Random(0);
		List<string> list = null;
		if (pack != null && pack.data != null && !string.IsNullOrEmpty(pack.data.languageId) && ContentResolvers.Languages.TryGet(pack.data.languageId, out var value))
		{
			list = type switch
			{
				1 => value.femaleNames, 
				2 => value.familyNames, 
				3 => value.cityNames, 
				_ => value.maleNames, 
			};
		}
		if (list == null || list.Count == 0)
		{
			if (pack == null)
			{
				return "无名";
			}
			list = type switch
			{
				1 => pack.names.femaleNames, 
				2 => pack.names.lastNames, 
				3 => pack.names.cityNames, 
				_ => pack.names.maleNames, 
			};
			if (list.Count == 0 && type != 2)
			{
				list = pack.names.lastNames;
			}
			if (list.Count == 0)
			{
				if (pack.data == null)
				{
					return "无名";
				}
				return pack.data.cultureName;
			}
		}
		return list[rng.Next(list.Count)];
	}
}
}
