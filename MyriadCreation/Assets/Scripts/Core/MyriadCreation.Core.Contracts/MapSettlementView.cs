using System.Collections.Generic;

namespace MyriadCreation.Core.Contracts
{
/// <summary>聚落展示视图（Rendering 用；仅 Core 类型，枚举以 int 传递）。</summary>
public sealed class MapSettlementInfo
{
	public int anchorId = -1;

	public int tileIndex = -1;

	public string name = "";

	public bool isMajor;

	public bool isCapital;

	public bool isPort;

	public bool isRuined;

	/// <summary>World.Settlement.SiteMode 的 int 值（0=SedentarySettlement,1=FortifiedOutpost,2=TemporaryEncampment）。</summary>
	public int siteMode;

	/// <summary>World.Settlement.SettlementForm 的 int 值（0=Village,1=City）。</summary>
	public int settlementForm;

	/// <summary>World.Settlement.SettlementLevel 的 int 值（0..4）。</summary>
	public int settlementLevel;

	public float population;
}

/// <summary>
/// 聚落只读视图契约（B7-part2）：Texture 的聚落标记与 SettlementIconLayer 共用；
/// 由 Simulation host 适配器转调 GameWorld.SettlementData。
/// </summary>
public interface IMapSettlementView
{
	int SettlementCount { get; }

	int AnchorCount { get; }

	IReadOnlyList<MapSettlementInfo> GetSettlements();

	IReadOnlyList<MapSettlementInfo> GetMajorSettlements();

	bool TryGetSettlement(int anchorId, out MapSettlementInfo settlement);

	/// <summary>按 tile 找 site 的 anchorId（图标点击/选择用）；越界/无 site 返回 false。</summary>
	bool TryGetAnchorAtTile(int tileIndex, out int anchorId);
}
}
