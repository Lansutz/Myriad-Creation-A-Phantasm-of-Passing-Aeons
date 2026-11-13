using System.Collections.Generic;

namespace MyriadCreation.Core.Contracts
{
/// <summary>行国（游牧政权）活动范围展示视图（Rendering 用；仅 Core 类型）。</summary>
public sealed class MapRealmInfo
{
	public int realmId;

	public int centerTile;

	public int summerCampTile = -1;

	public int winterCampTile = -1;

	public float coreRadius = 5f;

	public float outerRadius = 12f;

	public float influence = 50f;
}

/// <summary>
/// 政权/行国只读视图契约（B7-part2）：Nomadic 渲染只依赖本接口，不引用 Simulation.State/Systems 的 RealmData/NomadicRange/RealmForm。
/// </summary>
public interface IMapRealmView
{
	/// <summary>仅返回当前存在 nomadicRange 的游牧政权（已按 Nomadic 过滤）。</summary>
	IReadOnlyList<MapRealmInfo> GetNomadicRealms();
}
}
