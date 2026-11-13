using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
public static class MilitaryMovementSystem
{
	public const float EnemyFortCostMultiplier_L1 = 1.3f;

	public const float EnemyFortCostMultiplier_L2 = 1.6f;

	public const float EnemyFortCostMultiplier_L3 = 2f;

	public const float EnemyFortLossRate_L1 = 0.005f;

	public const float EnemyFortLossRate_L2 = 0.012f;

	public const float EnemyFortLossRate_L3 = 0.025f;

	public const float FriendlyFortCostReduction_L1 = 0.9f;

	public const float FriendlyFortCostReduction_L2 = 0.8f;

	public const float FriendlyFortCostReduction_L3 = 0.7f;

	public const float FriendlyFortSupplyRate_L1 = 0.003f;

	public const float FriendlyFortSupplyRate_L2 = 0.008f;

	public const float FriendlyFortSupplyRate_L3 = 0.015f;

	public const float FriendlyFortMoraleBonus = 0.1f;

	public const float BarrierDefenseBonus = 0.3f;

	public static bool IsPassable(TileData tile, int armyRealmId, bool isAtWarWithBarrierOwner = true)
	{
		if (!tile.exists)
		{
			return false;
		}
		if (tile.hasBarrier && tile.barrierOwnerRealmId >= 0)
		{
			if (tile.barrierOwnerRealmId == armyRealmId)
			{
				return true;
			}
			if (tile.barrierOwnerRealmId == -1)
			{
				return true;
			}
			if (isAtWarWithBarrierOwner)
			{
				return false;
			}
			return true;
		}
		return true;
	}

	public static bool RequiresBarrierBreach(TileData tile, int armyRealmId)
	{
		if (tile.hasBarrier && tile.barrierOwnerRealmId >= 0)
		{
			return tile.barrierOwnerRealmId != armyRealmId;
		}
		return false;
	}

	public static float CalculateActualMovementCost(TileData tile, int armyRealmId, int fortOwnerRealmId = -1)
	{
		if (!tile.passable)
		{
			return 50f;
		}
		float num = tile.movementCost;
		if (tile.fortInfluenceLevel > 0 && fortOwnerRealmId >= 0)
		{
			if (fortOwnerRealmId == armyRealmId)
			{
				float num2 = num;
				num = num2 * tile.fortInfluenceLevel switch
				{
					1 => 0.9f, 
					2 => 0.8f, 
					3 => 0.7f, 
					_ => 1f, 
				};
			}
			else
			{
				float num3 = num;
				num = num3 * tile.fortInfluenceLevel switch
				{
					1 => 1.3f, 
					2 => 1.6f, 
					3 => 2f, 
					_ => 1f, 
				};
			}
		}
		if (tile.hasBarrier && tile.barrierOwnerRealmId == armyRealmId)
		{
			num *= 0.95f;
		}
		return Mathf.Max(0.2f, num);
	}

	public static float CalculateActualMovementCostWithControl(TileData tile, int armyRealmId, int fortOwnerRealmId, RealmData ownerRealm, bool isAtWar)
	{
		float num = CalculateActualMovementCost(tile, armyRealmId, fortOwnerRealmId);
		if (ownerRealm != null && ownerRealm.realmId != armyRealmId)
		{
			num *= MovementControlSystem.CalculateControlCostMultiplier(tile, armyRealmId, ownerRealm, isAtWar);
		}
		return Mathf.Max(0.2f, num);
	}

	public static bool IsPassableWithControl(TileData tile, int armyRealmId, RealmData ownerRealm, bool isAtWar, bool isAtWarWithBarrierOwner = true)
	{
		if (!IsPassable(tile, armyRealmId, isAtWarWithBarrierOwner))
		{
			return false;
		}
		return MovementControlSystem.CanMilitaryPass(tile, armyRealmId, ownerRealm, isAtWar);
	}

	public static float CalculateArmyLosses(TileData tile, float armySize, int armyRealmId, int fortOwnerRealmId, int ticks = 1)
	{
		if (tile.fortInfluenceLevel <= 0 || fortOwnerRealmId < 0)
		{
			return 0f;
		}
		if (fortOwnerRealmId == armyRealmId)
		{
			return 0f;
		}
		return armySize * tile.fortInfluenceLevel switch
		{
			1 => 0.005f, 
			2 => 0.012f, 
			3 => 0.025f, 
			_ => 0f, 
		} * (float)ticks;
	}

	public static float CalculateMoraleLoss(TileData tile, int armyRealmId, int fortOwnerRealmId)
	{
		if (tile.fortInfluenceLevel <= 0 || fortOwnerRealmId < 0)
		{
			return 0f;
		}
		if (fortOwnerRealmId == armyRealmId)
		{
			return 0f;
		}
		return (float)tile.fortInfluenceLevel * 0.05f;
	}

	public static float CalculateSupplyRecovery(TileData tile, float armySize, int armyRealmId, int fortOwnerRealmId, int ticks = 1)
	{
		if (tile.fortInfluenceLevel <= 0 || fortOwnerRealmId < 0)
		{
			return 0f;
		}
		if (fortOwnerRealmId != armyRealmId)
		{
			return 0f;
		}
		return armySize * tile.fortInfluenceLevel switch
		{
			1 => 0.003f, 
			2 => 0.008f, 
			3 => 0.015f, 
			_ => 0f, 
		} * (float)ticks;
	}

	public static float CalculateMoraleRecovery(TileData tile, int armyRealmId, int fortOwnerRealmId)
	{
		if (tile.fortInfluenceLevel <= 0 || fortOwnerRealmId < 0)
		{
			return 0f;
		}
		if (fortOwnerRealmId != armyRealmId)
		{
			return 0f;
		}
		return (float)tile.fortInfluenceLevel * 0.1f;
	}

	public static float CalculateCombatSupportBonus(TileData tile, int armyRealmId, int fortOwnerRealmId)
	{
		if (tile.fortInfluenceLevel <= 0 || fortOwnerRealmId < 0)
		{
			return 0f;
		}
		if (fortOwnerRealmId != armyRealmId)
		{
			return 0f;
		}
		return (float)tile.fortInfluenceLevel * 0.08f;
	}

	public static float GetBarrierDefenseBonus(TileData tile)
	{
		if (!tile.hasBarrier)
		{
			return 0f;
		}
		return 0.3f * (tile.barrierStrength / 5f);
	}

	public static float CalculateBarrierBreachDifficulty(TileData tile)
	{
		if (!tile.hasBarrier)
		{
			return 0f;
		}
		return tile.barrierStrength * tile.movementCost;
	}

	public static float CalculateMovementSpeed(TileData tile, int armyRealmId, int fortOwnerRealmId)
	{
		float num = CalculateActualMovementCost(tile, armyRealmId, fortOwnerRealmId);
		return Mathf.Clamp(1f / num, 0.1f, 2f);
	}

	public static string GetMovementStatus(TileData tile, int armyRealmId, int fortOwnerRealmId)
	{
		if (!tile.exists)
		{
			return "虚空";
		}
		if (!tile.passable)
		{
			return "极难通行（强行军）";
		}
		List<string> list = new List<string>();
		if (tile.hasBarrier)
		{
			if (tile.barrierOwnerRealmId == armyRealmId)
			{
				list.Add("己方关隘");
			}
			else if (tile.barrierOwnerRealmId >= 0)
			{
				list.Add("敌对关隘");
			}
			else
			{
				list.Add("中立关隘");
			}
		}
		if (tile.fortInfluenceLevel > 0 && fortOwnerRealmId >= 0)
		{
			if (fortOwnerRealmId == armyRealmId)
			{
				list.Add($"己方堡垒区域(Lv{tile.fortInfluenceLevel})");
			}
			else
			{
				list.Add($"敌对堡垒区域(Lv{tile.fortInfluenceLevel})");
			}
		}
		if (tile.slopeDegree >= 30f)
		{
			list.Add("陡坡");
		}
		else if (tile.slopeDegree >= 15f)
		{
			list.Add("缓坡");
		}
		if (tile.roadLevel > GameEnums.RoadLevel.None)
		{
			list.Add(tile.roadLevel.ToString());
		}
		if (list.Count <= 0)
		{
			return "正常通行";
		}
		return string.Join(" / ", list);
	}

	public static string GetFullMovementStatus(TileData tile, int armyRealmId, int fortOwnerRealmId, RealmData ownerRealm, bool isAtWar)
	{
		List<string> list = new List<string>();
		string movementStatus = GetMovementStatus(tile, armyRealmId, fortOwnerRealmId);
		if (movementStatus != "正常通行")
		{
			list.Add(movementStatus);
		}
		if (ownerRealm != null && ownerRealm.realmId != armyRealmId)
		{
			string passStatus = MovementControlSystem.GetPassStatus(tile, armyRealmId, ownerRealm, isAtWar);
			list.Add(passStatus);
		}
		else if (ownerRealm != null && ownerRealm.realmId == armyRealmId)
		{
			list.Add("己方领土");
		}
		if (list.Count <= 0)
		{
			return "正常通行";
		}
		return string.Join(" | ", list);
	}
}
}
