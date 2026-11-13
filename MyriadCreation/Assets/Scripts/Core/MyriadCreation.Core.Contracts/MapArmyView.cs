using System.Collections.Generic;

namespace MyriadCreation.Core.Contracts
{
/// <summary>军队展示视图（Rendering 用；只带 Core 类型，枚举以 int 传递）。</summary>
public sealed class MapArmyInfo
{
	public int armyId = -1;

	public int realmId = -1;

	public int tileIndex = -1;

	public string armyName = "";

	public float manpower;

	public float organization;

	/// <summary>GameEnums.CombatState 的 int 值（0=Idle,1=Marching,2=Besieging,3=InCombat,4=Retreating,5=Routed,6=Dead）。</summary>
	public int combatState;

	public bool isMoving;
}

/// <summary>
/// 军队只读视图契约（P1.5）：ArmyMarkerLayer 用；由 Simulation host 适配器转调 GameWorld.armies。
/// 与 IMapSettlementView 同构，不把 GameWorld/Army 类型带进 Rendering。
/// </summary>
public interface IMapArmyView
{
	int ArmyCount { get; }

	IReadOnlyList<MapArmyInfo> GetArmies();
}
}
