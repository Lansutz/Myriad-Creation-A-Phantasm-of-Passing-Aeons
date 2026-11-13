namespace MyriadCreation.World.Settlement
{
/// <summary>
/// 建筑实例容纳人物的通用语义。地牢不是独立系统，也不属于墙体设施：
/// 它只是 <see cref="ContainmentKind.Confinement"/> 的建筑块。
/// </summary>
public enum ContainmentKind
{
	None,
	/// <summary>居所：领主府、宅邸等长期居住。</summary>
	Residence,
	/// <summary>拘禁：地牢、牢房、软禁处。</summary>
	Confinement,
	/// <summary>避难：宗教/外交庇护、避难所。</summary>
	Shelter,
	/// <summary>值驻：军营、岗位、值班房等因职责停留。</summary>
	Duty
}

/// <summary>访问策略：描述谁能进入，不决定容量，也不替代拘禁状态。</summary>
public enum ContainmentAccess
{
	Open,
	Restricted,
	Sealed
}
}
