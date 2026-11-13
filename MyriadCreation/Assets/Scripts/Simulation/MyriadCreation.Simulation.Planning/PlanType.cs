using System;

namespace MyriadCreation.Simulation.Planning
{
public enum PlanType
{
	Research,
	Intrigue,
	Engineering,
	Construction,
	Exploration,
	// 以下四类保留仅为枚举序号稳定（存档里按 int 落盘）；D32 已裁决不走 Plan：
	// 战争走 Warfare/DiplomacyManager/ManpowerSystem，贸易/外交/迁徙各有专系统。
	[Obsolete("D32：战争不走 Plan，改走 Warfare/ManpowerSystem。")]
	Trade,
	[Obsolete("D32：外交不走 Plan，改走 DiplomacyManager。")]
	Diplomacy,
	[Obsolete("D32：迁徙不走 Plan。")]
	Migration,
	[Obsolete("D32：战争不走 Plan，改走 Warfare/ManpowerSystem。")]
	Military,
	Custom
}
}
