using System;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public class CasusBelli
{
	public int cbId;

	public GameEnums.CasusBelliType type;

	public int holderRealmId;

	public int targetRealmId;

	public int generatedDay;

	public int expiryDay;

	public string description;

	public float justificationStrength;

	public int relatedTileIndex;

	public bool isUsed;

	public bool IsValid(int currentDay)
	{
		if (!isUsed)
		{
			if (expiryDay >= 0)
			{
				return currentDay < expiryDay;
			}
			return true;
		}
		return false;
	}
}
}
