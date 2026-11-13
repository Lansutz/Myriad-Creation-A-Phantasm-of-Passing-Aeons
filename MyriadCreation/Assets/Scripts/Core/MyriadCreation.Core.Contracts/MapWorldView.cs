using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Core.Contracts
{
/// <summary>
/// 文化展示视图（Rendering 用，仅 Core 可表达类型）：
/// 颜色用 float 四元组，避免 Core.Contracts 依赖 UnityEngine；CultureData 的映射在 host 适配器内完成。
/// </summary>
public sealed class MapCultureView
{
	public int cultureId;

	public float colorR;

	public float colorG;

	public float colorB;

	public float colorA = 1f;

	public int parentCultureId = -1;

	public bool allowsBranching;
}

/// <summary>外交关系展示视图（Rendering 用，替代直接引用 DiplomaticRelation/AllianceType）。</summary>
public sealed class MapDiplomacyView
{
	public bool hasRelation;

	public bool isAtWar;

	public bool isHostile;

	public float relation;

	public bool hasActiveAlliance;

	public bool hasFactionAlliance;
}

/// <summary>
/// 只读地图世界视图契约（B7）：Rendering 只依赖本接口 + Core 类型，不直接引用 Simulation.WorldState/State/Systems/Diplomacy。
/// 由 Simulation 侧 host 适配器（MapWorldViewAdapter）转调 GameWorld；所有方法对越界/空世界安全。
/// </summary>
public interface IMapWorldView
{
	int MapWidth { get; }

	int MapHeight { get; }

	bool WrapX { get; }

	bool WrapY { get; }

	int PlayerRealmId { get; }

	MapWrapMode WrapMode { get; }

	/// <summary>返回该 tile 是否存在（索引合法且 tiles[index].exists）。</summary>
	bool TileExists(int tileIndex);

	/// <summary>取 Core.Data.TileData 只读快照；越界/空世界返回 false，tile 置 default。</summary>
	bool TryGetTile(int tileIndex, out TileData tile);

	/// <summary>取文化展示视图；未找到返回 false。</summary>
	bool TryGetCulture(int cultureId, out MapCultureView culture);

	/// <summary>取两国关系展示视图；无外交管理器/无关系返回 false。</summary>
	bool IsProvinceBorder(int tileIndex);

	bool TryGetDiplomacyRelation(int viewerRealmId, int otherRealmId, out MapDiplomacyView relation);

	/// <summary>otherRealm 是否为 viewerRealm 的附庸/朝贡（双色空间 VassalTribute 判定）。</summary>
	bool IsSubordinate(int viewerRealmId, int otherRealmId);

	/// <summary>聚落锚点 id 列表（SettlementIconLayer 后续接入用；无聚落返回空列表）。</summary>
	IReadOnlyList<int> GetSettlementAnchorIds();
}
}
