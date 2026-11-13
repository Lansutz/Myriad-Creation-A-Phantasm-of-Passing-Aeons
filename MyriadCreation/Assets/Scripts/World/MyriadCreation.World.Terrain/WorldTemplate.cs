using UnityEngine;

namespace MyriadCreation.World.Terrain
{
public enum WorldTemplate
{
	[Tooltip("一块超级大陆，周围环绕海洋。陆地集中，海洋连通。")]
	Pangaea,
	[Tooltip("两块大陆隔海相望，中间为海洋通道。")]
	DualContinents,
	[Tooltip("3-5块大陆散布，海洋分割各大陆。")]
	MultiContinents,
	[Tooltip("无大块陆地，大量岛屿散布，陆地比例低。")]
	ArchipelagoWorld,
	[Tooltip("大陆环绕中央内海，类似地中海但规模更大。")]
	RingContinent,
	[Tooltip("中央海为核心，周围多块陆地环绕，海陆交错。")]
	Mediterranean,
	[Tooltip("类似地球的海陆分布：几块大陆+大片海洋。")]
	EarthLike,
	[Tooltip("不使用模板预设，完全由参数滑块控制。")]
	Custom
}
}
