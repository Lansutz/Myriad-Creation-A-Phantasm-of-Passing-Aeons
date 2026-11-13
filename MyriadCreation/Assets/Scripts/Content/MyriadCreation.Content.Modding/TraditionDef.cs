using System;
using System.Collections.Generic;
using MyriadCreation.Core;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Content.Modding
{
[Serializable]
public class TraditionDef
{
	public string traditionId;

	public List<EffectEntry> effects = new List<EffectEntry>();

	public List<string> incompatibleWith = new List<string>();

	public List<int> requiredInnovations = new List<int>();

	public List<int> exclusiveToCultureIds = new List<int>();

	public string upgradesFrom = "";

	public TraditionCategory category = TraditionCategory.Production;

	public CharacterRole minRole;

	public List<EffectEntry> factors = new List<EffectEntry>();

	public float solidification = 0.3f;

	public List<int> linkedInnovationIds = new List<int>();

	public bool IsCommon
	{
		get
		{
			if (exclusiveToCultureIds != null)
			{
				return exclusiveToCultureIds.Count == 0;
			}
			return true;
		}
	}

	public string GetName()
	{
		return Localization.Get(traditionId + "_name");
	}

	public string GetDescription()
	{
		return Localization.Get(traditionId + "_desc");
	}

	public bool CanCultureHold(int cultureId)
	{
		if (IsCommon)
		{
			return true;
		}
		if (exclusiveToCultureIds != null)
		{
			return exclusiveToCultureIds.Contains(cultureId);
		}
		return false;
	}

	public float GetFactor(TraditionFactorKey key)
	{
		if (factors == null)
		{
			return 0f;
		}
		foreach (EffectEntry factor in factors)
		{
			if (string.Equals(factor.key, key.ToString(), StringComparison.OrdinalIgnoreCase))
			{
				return factor.value;
			}
		}
		return 0f;
	}

	public bool RoleGateFulfilled(CharacterRole role)
	{
		if (category != TraditionCategory.Political)
		{
			return true;
		}
		return TraditionElevation.Rank(role) >= TraditionElevation.Rank(minRole);
	}
}
}
