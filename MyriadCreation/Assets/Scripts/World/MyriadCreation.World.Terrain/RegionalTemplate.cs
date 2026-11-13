using UnityEngine;

namespace MyriadCreation.World.Terrain
{
public enum RegionalTemplate
{
	[Tooltip("一条大河贯穿，周围为流域平原，两侧缓升。")]
	RiverValley,
	[Tooltip("陆地从一侧伸入海洋，三面环海。")]
	Peninsula,
	[Tooltip("一片群岛散布在海洋中，无大块陆地。")]
	Archipelago,
	[Tooltip("中央为海，周围被陆地环绕，仅一侧可能通外海。")]
	InlandSea,
	[Tooltip("一条大山脉横贯，两侧为低地/平原。")]
	MountainRange,
	[Tooltip("四周高山环绕，中央为低地/平原，可能有内流湖。")]
	Basin,
	[Tooltip("一侧为海洋，陆地向另一侧缓慢升高，海岸线平直。")]
	CoastalPlain,
	[Tooltip("冰川侵蚀形成的锯齿状海岸，多峡湾和岛屿。")]
	Fjord,
	[Tooltip("不使用模板预设，完全由参数滑块控制。")]
	Custom
}
}
