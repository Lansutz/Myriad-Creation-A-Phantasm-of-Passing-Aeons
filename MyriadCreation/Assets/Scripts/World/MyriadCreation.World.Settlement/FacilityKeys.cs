using System;

namespace MyriadCreation.World.Settlement
{
public static class FacilityKeys
{
	public static string ToFacilityTypeKey(FacilityType type)
	{
		return type switch
		{
			FacilityType.Wall => "wall", 
			FacilityType.Gate => "gate", 
			FacilityType.Trench => "trench", 
			FacilityType.Palisade => "palisade", 
			FacilityType.Earthwork => "earthwork", 
			FacilityType.Other => "other", 
			_ => throw new ArgumentOutOfRangeException("type", type, "Unknown facility type."), 
		};
	}

	public static bool TryParseFacilityType(string key, out FacilityType type)
	{
		switch (key)
		{
		case "wall":
			type = FacilityType.Wall;
			return true;
		case "gate":
			type = FacilityType.Gate;
			return true;
		case "trench":
			type = FacilityType.Trench;
			return true;
		case "palisade":
			type = FacilityType.Palisade;
			return true;
		case "earthwork":
			type = FacilityType.Earthwork;
			return true;
		case "other":
			type = FacilityType.Other;
			return true;
		default:
			type = FacilityType.Wall;
			return false;
		}
	}

	public static string ToConstructionStateKey(ConstructionState state)
	{
		return state switch
		{
			ConstructionState.Planned => "planned", 
			ConstructionState.UnderConstruction => "under_construction", 
			ConstructionState.Complete => "complete", 
			ConstructionState.Ruined => "ruined", 
			_ => throw new ArgumentOutOfRangeException("state", state, "Unknown construction state."), 
		};
	}

	public static bool TryParseConstructionState(string key, out ConstructionState state)
	{
		switch (key)
		{
		case "planned":
			state = ConstructionState.Planned;
			return true;
		case "under_construction":
			state = ConstructionState.UnderConstruction;
			return true;
		case "complete":
			state = ConstructionState.Complete;
			return true;
		case "ruined":
			state = ConstructionState.Ruined;
			return true;
		default:
			state = ConstructionState.Planned;
			return false;
		}
	}

	public static string ToDamageStateKey(FacilityDamageState state)
	{
		return state switch
		{
			FacilityDamageState.Intact => "intact", 
			FacilityDamageState.Damaged => "damaged", 
			FacilityDamageState.Disabled => "disabled", 
			FacilityDamageState.Destroyed => "destroyed", 
			_ => throw new ArgumentOutOfRangeException("state", state, "Unknown facility damage state."), 
		};
	}

	public static bool TryParseDamageState(string key, out FacilityDamageState state)
	{
		switch (key)
		{
		case "intact":
			state = FacilityDamageState.Intact;
			return true;
		case "damaged":
			state = FacilityDamageState.Damaged;
			return true;
		case "disabled":
			state = FacilityDamageState.Disabled;
			return true;
		case "destroyed":
			state = FacilityDamageState.Destroyed;
			return true;
		default:
			state = FacilityDamageState.Intact;
			return false;
		}
	}
}
}
