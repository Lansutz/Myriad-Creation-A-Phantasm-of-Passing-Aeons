using System;
using System.Collections.Generic;

namespace MyriadCreation.Content.Definitions
{
[Serializable]
public sealed class BuildingMaterialCostDef
{
	public int goodsId;

	public float amount;
}

/// <summary>建筑内容表行（JSON 形态；BuildingDef.materialCost 是 NonSerialized 字典，故分开声明）。</summary>
[Serializable]
public sealed class BuildingContentDef
{
	public int buildingId;

	public string buildingName = "";

	/// <summary>BuildingCategory 的 int 值（0=Agriculture…5=Admin）。</summary>
	public int category;

	public int tier;

	public float buildCost;

	public List<BuildingMaterialCostDef> materialCost = new List<BuildingMaterialCostDef>();

	public int buildDays;

	public float maintenanceCost;
}

/// <summary>
/// 建筑外提表（P2.4）：内容加载后由 BuildingSystem 复制；未加载时仍走代码兜底表。
/// 同 id 后者覆盖前者（Base→Mods 顺序由内容源目录保证）。
/// </summary>
public static class BuildingCatalog
{
	private static Dictionary<int, BuildingDef> _defs;

	public static bool IsLoaded => _defs != null && _defs.Count > 0;

	public static IReadOnlyDictionary<int, BuildingDef> All => _defs;

	public static void LoadFromContent(IEnumerable<BuildingContentDef> defs)
	{
		_defs ??= new Dictionary<int, BuildingDef>();
		if (defs == null)
		{
			return;
		}
		foreach (BuildingContentDef def in defs)
		{
			if (def == null || def.buildingId <= 0)
			{
				continue;
			}
			Dictionary<int, float> materials = new Dictionary<int, float>();
			if (def.materialCost != null)
			{
				for (int i = 0; i < def.materialCost.Count; i++)
				{
					BuildingMaterialCostDef cost = def.materialCost[i];
					if (cost != null && cost.goodsId > 0)
					{
						materials[cost.goodsId] = cost.amount;
					}
				}
			}
			_defs[def.buildingId] = new BuildingDef
			{
				buildingId = def.buildingId,
				buildingName = def.buildingName ?? "",
				category = (BuildingCategory)ClampCategory(def.category),
				tier = def.tier <= 0 ? 1 : def.tier,
				buildCost = def.buildCost,
				materialCost = materials,
				buildDays = def.buildDays > 0 ? def.buildDays : Math.Max(10, def.tier * 10),
				maintenanceCost = def.maintenanceCost
			};
		}
	}

	public static void ResetContent()
	{
		_defs = null;
	}

	private static int ClampCategory(int value)
	{
		return value < 0 || value > (int)BuildingCategory.Admin ? (int)BuildingCategory.Agriculture : value;
	}
}
}
