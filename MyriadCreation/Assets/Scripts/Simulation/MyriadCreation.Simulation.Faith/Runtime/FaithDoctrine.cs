using System;
using System.Collections.Generic;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Faith
{
/// <summary>
/// 教义选择的读取口径：运行时覆盖优先，空则回落到内容表 religion 的 selectedDoctrines。
/// 合一/改宗只写覆盖，不改共享的内容定义。
/// </summary>
public static class FaithDoctrine
{
	public static IReadOnlyList<string> GetIds(FaithSystem faith, ReligionDef contentFallback)
	{
		if (faith?.doctrineIds != null && faith.doctrineIds.Count > 0)
		{
			return faith.doctrineIds;
		}
		return contentFallback?.selectedDoctrines ?? (IReadOnlyList<string>)Array.Empty<string>();
	}

	/// <summary>把运行时教义覆盖写回内容口径（空列表表示清除覆盖）。</summary>
	public static void SetIds(FaithSystem faith, IEnumerable<string> ids)
	{
		if (faith == null)
		{
			return;
		}
		faith.doctrineIds ??= new List<string>();
		faith.doctrineIds.Clear();
		if (ids == null)
		{
			return;
		}
		foreach (string id in ids)
		{
			if (!string.IsNullOrEmpty(id))
			{
				faith.doctrineIds.Add(id);
			}
		}
	}
}
}
