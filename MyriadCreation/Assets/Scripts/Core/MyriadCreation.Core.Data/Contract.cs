using System;
using System.Collections.Generic;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Core.Data
{
[Serializable]
public class Contract
{
	public string contractId = Guid.NewGuid().ToString("D");

	public ContractType type;

	public List<EntityRef> parties = new List<EntityRef>();

	public int startDay;

	public int expiresDay = -1;

	public List<string> terms = new List<string>();

	public EntityRef target;

	public bool hasTarget;

	public bool breached;

	public bool IsActive(int day)
	{
		if (!breached)
		{
			if (expiresDay >= 0)
			{
				return day < expiresDay;
			}
			return true;
		}
		return false;
	}

	public Contract Clone()
	{
		return new Contract
		{
			contractId = contractId,
			type = type,
			parties = new List<EntityRef>(parties ?? new List<EntityRef>()),
			startDay = startDay,
			expiresDay = expiresDay,
			terms = new List<string>(terms ?? new List<string>()),
			target = target,
			hasTarget = hasTarget,
			breached = breached
		};
	}
}
}
