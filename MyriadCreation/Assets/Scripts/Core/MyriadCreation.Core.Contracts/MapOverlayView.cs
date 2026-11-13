using System.Collections.Generic;

namespace MyriadCreation.Core.Contracts
{
/// <summary>行政区划层级叠加（每个 tile 的最深 division/level；-1/0 表示无）。</summary>
public sealed class MapAdminDivisionOverlay
{
	public int[] divisionIdByTile;

	public byte[] levelByTile;
}

/// <summary>虚控制范围叠加（tile→realm + 控制点连接线；anchor id 对）。</summary>
public sealed class MapVirtualControlOverlay
{
	public Dictionary<int, int> realmByTile = new Dictionary<int, int>();

	public List<int> connectionFrom = new List<int>();

	public List<int> connectionTo = new List<int>();
}

/// <summary>政权影响力叠加（tier/sourceAnchor by tile）。</summary>
public sealed class MapInfluenceOverlay
{
	public byte[] tierByTile;

	public int[] sourceAnchorByTile;
}

/// <summary>
/// 地图叠加层只读视图契约（B7-part2）：AdminDivision/InfluenceField/SettlementInfluenceSystem 的计算全部在 host 适配器内完成，
/// Rendering 只消费数组/简单值，不引用 Simulation 类型。
/// </summary>
public interface IMapOverlayView
{
	int ProvinceCount { get; }

	bool TryGetAdminDivisionOverlay(out MapAdminDivisionOverlay overlay);

	bool TryGetVirtualControlOverlay(out MapVirtualControlOverlay overlay);

	bool TryGetInfluenceOverlay(out MapInfluenceOverlay overlay);
}
}
