using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
public static class MovementControlSystem
{
	public const float NoneCostMultiplier = 1f;

	public const float LooseCostMultiplier = 1.1f;

	public const float LimitedCostMultiplier = 1.25f;

	public const float StrictWithAccessCostMultiplier = 1.15f;

	public const float LimitedSurveillanceChance = 0.1f;

	public const float StrictSurveillanceChance = 0.25f;

	public const float LooseTariffRate = 0.02f;

	public const float LimitedTariffRate = 0.05f;

	public const float StrictTariffRate = 0.03f;

	public static GameEnums.MovementControlLevel GetEffectiveControlLevel(TileData tile, RealmData ownerRealm)
	{
		if (ownerRealm == null)
		{
			return GameEnums.MovementControlLevel.None;
		}
		if (ownerRealm.tileMovementControlOverrides != null && ownerRealm.tileMovementControlOverrides.TryGetValue(tile.tileIndex, out var value))
		{
			return value;
		}
		return ownerRealm.movementControl;
	}

	public static GameEnums.MovementControlLevel GetMaxControlLevel(RealmData realm)
	{
		if (realm == null)
		{
			return GameEnums.MovementControlLevel.None;
		}
		GameEnums.MovementControlLevel movementControlLevel = realm.movementControl;
		if (realm.tileMovementControlOverrides != null)
		{
			foreach (GameEnums.MovementControlLevel value in realm.tileMovementControlOverrides.Values)
			{
				if (value > movementControlLevel)
				{
					movementControlLevel = value;
				}
			}
		}
		return movementControlLevel;
	}

	public static bool CanMilitaryPass(TileData tile, int armyRealmId, RealmData ownerRealm, bool isAtWar)
	{
		if (ownerRealm == null || ownerRealm.realmId < 0)
		{
			return true;
		}
		if (ownerRealm.realmId == armyRealmId)
		{
			return true;
		}
		if (isAtWar)
		{
			GetEffectiveControlLevel(tile, ownerRealm);
			return false;
		}
		switch (GetEffectiveControlLevel(tile, ownerRealm))
		{
		case GameEnums.MovementControlLevel.None:
		case GameEnums.MovementControlLevel.Loose:
		case GameEnums.MovementControlLevel.Limited:
			return true;
		case GameEnums.MovementControlLevel.Strict:
			return HasMilitaryAccess(ownerRealm, armyRealmId);
		default:
			return true;
		}
	}

	public static bool RequiresMilitaryAccess(TileData tile, RealmData ownerRealm, int armyRealmId, bool isAtWar)
	{
		if (ownerRealm == null || ownerRealm.realmId == armyRealmId)
		{
			return false;
		}
		if (isAtWar)
		{
			return true;
		}
		if (GetEffectiveControlLevel(tile, ownerRealm) == GameEnums.MovementControlLevel.Strict)
		{
			return !HasMilitaryAccess(ownerRealm, armyRealmId);
		}
		return false;
	}

	public static bool HasMilitaryAccess(RealmData ownerRealm, int armyRealmId)
	{
		if (ownerRealm == null || ownerRealm.militaryAccessGranted == null)
		{
			return false;
		}
		return ownerRealm.militaryAccessGranted.Contains(armyRealmId);
	}

	public static bool GrantMilitaryAccess(RealmData ownerRealm, int targetRealmId)
	{
		if (ownerRealm == null || ownerRealm.militaryAccessGranted == null)
		{
			return false;
		}
		if (ownerRealm.realmId == targetRealmId)
		{
			return false;
		}
		return ownerRealm.militaryAccessGranted.Add(targetRealmId);
	}

	public static bool RevokeMilitaryAccess(RealmData ownerRealm, int targetRealmId)
	{
		if (ownerRealm == null || ownerRealm.militaryAccessGranted == null)
		{
			return false;
		}
		return ownerRealm.militaryAccessGranted.Remove(targetRealmId);
	}

	public static void OnWarDeclared(RealmData ownerRealm, int enemyRealmId)
	{
		RevokeMilitaryAccess(ownerRealm, enemyRealmId);
	}

	public static float CalculateControlCostMultiplier(TileData tile, int armyRealmId, RealmData ownerRealm, bool isAtWar)
	{
		if (ownerRealm == null || ownerRealm.realmId == armyRealmId)
		{
			return 1f;
		}
		GameEnums.MovementControlLevel effectiveControlLevel = GetEffectiveControlLevel(tile, ownerRealm);
		if (isAtWar)
		{
			return effectiveControlLevel switch
			{
				GameEnums.MovementControlLevel.None => 1.5f, 
				GameEnums.MovementControlLevel.Loose => 1.8f, 
				GameEnums.MovementControlLevel.Limited => 2.5f, 
				GameEnums.MovementControlLevel.Strict => 999f, 
				_ => 1.5f, 
			};
		}
		return effectiveControlLevel switch
		{
			GameEnums.MovementControlLevel.None => 1f, 
			GameEnums.MovementControlLevel.Loose => 1.1f, 
			GameEnums.MovementControlLevel.Limited => 1.25f, 
			GameEnums.MovementControlLevel.Strict => HasMilitaryAccess(ownerRealm, armyRealmId) ? 1.15f : 999f, 
			_ => 1f, 
		};
	}

	public static float CalculateTariff(TileData tile, int armyRealmId, RealmData ownerRealm, float cargoValue)
	{
		if (ownerRealm == null || ownerRealm.realmId == armyRealmId)
		{
			return 0f;
		}
		return cargoValue * GetEffectiveControlLevel(tile, ownerRealm) switch
		{
			GameEnums.MovementControlLevel.Loose => 0.02f, 
			GameEnums.MovementControlLevel.Limited => 0.05f, 
			GameEnums.MovementControlLevel.Strict => HasMilitaryAccess(ownerRealm, armyRealmId) ? 0.03f : 0f, 
			_ => 0f, 
		};
	}

	public static float CalculateSurveillanceChance(TileData tile, int armyRealmId, RealmData ownerRealm)
	{
		if (ownerRealm == null || ownerRealm.realmId == armyRealmId)
		{
			return 0f;
		}
		return GetEffectiveControlLevel(tile, ownerRealm) switch
		{
			GameEnums.MovementControlLevel.Limited => 0.1f, 
			GameEnums.MovementControlLevel.Strict => HasMilitaryAccess(ownerRealm, armyRealmId) ? 0.25f : 0f, 
			_ => 0f, 
		};
	}

	public static void SetTileControlOverride(RealmData realm, int tileIndex, GameEnums.MovementControlLevel level)
	{
		if (realm != null && realm.tileMovementControlOverrides != null)
		{
			realm.tileMovementControlOverrides[tileIndex] = level;
		}
	}

	public static void ClearTileControlOverride(RealmData realm, int tileIndex)
	{
		if (realm != null && realm.tileMovementControlOverrides != null)
		{
			realm.tileMovementControlOverrides.Remove(tileIndex);
		}
	}

	public static bool ShouldGrantMilitaryAccess(RealmData ownerRealm, RealmData requesterRealm, float relation, float threatLevel)
	{
		if (ownerRealm == null || requesterRealm == null)
		{
			return false;
		}
		if (relation >= 50f && threatLevel < 0.3f)
		{
			return true;
		}
		if (relation >= 30f && threatLevel < 0.5f)
		{
			return Random.value > 0.5f;
		}
		if (!(relation < 0f))
		{
			_ = 0.7f;
		}
		return false;
	}

	public static string GetControlLevelDescription(GameEnums.MovementControlLevel level)
	{
		return level switch
		{
			GameEnums.MovementControlLevel.None => "无管制：军队可自由通过", 
			GameEnums.MovementControlLevel.Loose => "松散管制：可通过，有关税和检查，速度略降", 
			GameEnums.MovementControlLevel.Limited => "有限管制：可通过，需登记，速度明显下降，可能被监视", 
			GameEnums.MovementControlLevel.Strict => "严格管制：需军事通行权，否则不可通过", 
			_ => "未知", 
		};
	}

	public static string GetPassStatus(TileData tile, int armyRealmId, RealmData ownerRealm, bool isAtWar)
	{
		if (ownerRealm == null)
		{
			return "无主土地：自由通行";
		}
		if (ownerRealm.realmId == armyRealmId)
		{
			return "己方领土：自由通行";
		}
		GameEnums.MovementControlLevel effectiveControlLevel = GetEffectiveControlLevel(tile, ownerRealm);
		if (isAtWar)
		{
			return $"战争状态：敌国领土，不可通过（管制等级：{effectiveControlLevel}）";
		}
		switch (effectiveControlLevel)
		{
		case GameEnums.MovementControlLevel.Strict:
			if (!HasMilitaryAccess(ownerRealm, armyRealmId))
			{
				return "严格管制：无军事通行权，不可通过";
			}
			return "严格管制：已获军事通行权，可通过（有通关检查）";
		case GameEnums.MovementControlLevel.None:
			return "无管制：自由通行";
		case GameEnums.MovementControlLevel.Loose:
			return "松散管制：可通过（关税+检查）";
		case GameEnums.MovementControlLevel.Limited:
			return "有限管制：可通过（登记+监视）";
		default:
			return "未知";
		}
	}
}
}
