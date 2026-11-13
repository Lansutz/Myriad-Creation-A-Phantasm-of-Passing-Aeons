using System;
using System.Collections.Generic;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Core.Data
{
[Serializable]
public class EntityRelation
{
	public EntityRef from;

	public EntityRef to;

	public RelationType type;

	public float legitimacy = 0.5f;

	public float stability = 0.5f;

	public bool isPrimary;

	public bool isProjection;

	public int sinceDay;

	public int expiresDay = -1;

	public List<string> obligations = new List<string>();

	public bool IsActive(int day)
	{
		if (expiresDay >= 0)
		{
			return day < expiresDay;
		}
		return true;
	}

	public EntityRelation Clone()
	{
		return new EntityRelation
		{
			from = from,
			to = to,
			type = type,
			legitimacy = legitimacy,
			stability = stability,
			isPrimary = isPrimary,
			isProjection = isProjection,
			sinceDay = sinceDay,
			expiresDay = expiresDay,
			obligations = new List<string>(obligations ?? new List<string>())
		};
	}
}
}
