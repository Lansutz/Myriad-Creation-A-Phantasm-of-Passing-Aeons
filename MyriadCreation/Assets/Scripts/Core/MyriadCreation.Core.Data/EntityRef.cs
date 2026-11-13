using System;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Core.Data
{
[Serializable]
public struct EntityRef(EntityKind kind, int id)
{
	public EntityKind kind = kind;

	public int id = id;

	public bool IsValid => id >= 0;

	public override string ToString()
	{
		return $"{kind}:{id}";
	}
}
}
