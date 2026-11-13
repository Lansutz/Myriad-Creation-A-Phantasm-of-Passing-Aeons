using System;
using System.Collections.Generic;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Content.Definitions
{
[Serializable]
public sealed class UnitCostDef
{
	public int goodsId;

	public float amount;
}

/// <summary>兵种内容表行（JSON 形态；UnitDef 的字典字段为 NonSerialized，故分开声明）。</summary>
[Serializable]
public sealed class UnitContentDef
{
	public int unitId;

	public string unitName = "";

	/// <summary>GameEnums.UnitArmType 的 int 值（0=Infantry,1=Cavalry,2=Navy）。</summary>
	public int category;

	public int tier = 1;

	public float meleeAttack;

	public float rangedAttack;

	public float defense;

	public float morale;

	public float speed;

	public float supplyConsumption;

	public float manpowerCost;

	public List<UnitCostDef> recruitCost = new List<UnitCostDef>();

	public List<int> requiredInnovations = new List<int>();
}

/// <summary>
/// 兵种外提表（P2.4）：内容加载后由 GameWorld 复制；未加载时仍走代码兜底表。
/// 同 id 后者覆盖前者（Base→Mods）。
/// </summary>
public static class UnitCatalog
{
	private static Dictionary<int, UnitDef> _defs;

	public static bool IsLoaded => _defs != null && _defs.Count > 0;

	public static IReadOnlyDictionary<int, UnitDef> All => _defs;

	public static void LoadFromContent(IEnumerable<UnitContentDef> defs)
	{
		_defs ??= new Dictionary<int, UnitDef>();
		if (defs == null)
		{
			return;
		}
		foreach (UnitContentDef def in defs)
		{
			if (def == null || def.unitId <= 0)
			{
				continue;
			}
			Dictionary<int, float> costs = new Dictionary<int, float>();
			if (def.recruitCost != null)
			{
				for (int i = 0; i < def.recruitCost.Count; i++)
				{
					UnitCostDef cost = def.recruitCost[i];
					if (cost != null && cost.goodsId > 0)
					{
						costs[cost.goodsId] = cost.amount;
					}
				}
			}
			_defs[def.unitId] = new UnitDef
			{
				unitId = def.unitId,
				unitName = def.unitName ?? "",
				category = (GameEnums.UnitArmType)ClampCategory(def.category),
				tier = def.tier <= 0 ? 1 : def.tier,
				meleeAttack = def.meleeAttack,
				rangedAttack = def.rangedAttack,
				defense = def.defense,
				morale = def.morale,
				speed = def.speed,
				supplyConsumption = def.supplyConsumption,
				manpowerCost = def.manpowerCost,
				recruitCost = costs,
				terrainModifiers = new Dictionary<GameEnums.TerrainTacticType, float>(),
				requiredInnovations = def.requiredInnovations != null ? new List<int>(def.requiredInnovations) : new List<int>()
			};
		}
	}

	public static void ResetContent()
	{
		_defs = null;
	}

	private static int ClampCategory(int value)
	{
		return value < 0 || value > (int)GameEnums.UnitArmType.Navy ? (int)GameEnums.UnitArmType.Infantry : value;
	}
}
}
