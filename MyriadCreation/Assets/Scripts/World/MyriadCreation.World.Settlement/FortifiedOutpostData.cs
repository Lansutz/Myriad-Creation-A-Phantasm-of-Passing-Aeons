using System;

namespace MyriadCreation.World.Settlement
{
[Serializable]
public sealed class FortifiedOutpostData
{
	public string purpose = string.Empty;

	/// <summary>据点用途；采矿/采石等是据点类型，不是定居点类型，也不是设施。</summary>
	public OutpostKind kind = OutpostKind.Generic;

}
}
