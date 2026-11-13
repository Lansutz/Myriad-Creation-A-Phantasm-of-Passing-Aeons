using System.Collections.Generic;

namespace MyriadCreation.Core.Contracts
{
/// <summary>政权只读快照（UI 用；仅 Core 类型，颜色以 float 三元组表达）。</summary>
public sealed class RealmSnapshotDto
{
	public int realmId;

	public string realmName = "";

	public float colorR;

	public float colorG;

	public float colorB;

	/// <summary>当前政权实际控制的 exists 地块数（无 tiles 时回退 coreTiles 数量）。</summary>
	public int territoryCount;

	/// <summary>当前政权控制地块上的人口合计。</summary>
	public float population;

	public float treasury;

	public float stability;

	public float legitimacy;

	public float centralization;

	public bool isMonarchy;

	public int rulerId = -1;

	public string rulerName = "";

	/// <summary>RealmForm 枚举 int 值（0=Sedentary,1=SemiSedentary,2=Nomadic，便于 UI 显示）。</summary>
	public int governmentForm;

	public string governmentLabel = "";

	public int capitalAnchorId = -1;
}

/// <summary>行政区划只读节点（UI 树/列表用）。</summary>
public sealed class AdminDivisionNodeDto
{
	public int divisionId;

	public int realmId;

	public string name = "";

	public int level;

	public int parentId = -1;

	public int tileCount;

	public string titleId = "";

	public int holderCharacterId = -1;

	public string holderName = "";
}

/// <summary>聚落只读快照（UI 用；枚举均以 int 值表达，避免 Core→World/Simulation 反向依赖）。</summary>
public sealed class SettlementSnapshotDto
{
	public int anchorId = -1;

	public string settlementName = "";

	public int tileIndex = -1;

	public float population;

	public float wealth;

	public int garrison;

	public float garrisonQuality;

	public int garrisonQuantity;

	public int constructionTier;

	/// <summary>SiteMode int 值（0=SedentarySettlement,1=FortifiedOutpost,2=TemporaryEncampment）。</summary>
	public int siteMode;

	/// <summary>SettlementForm int 值（0=Village,1=City）。</summary>
	public int settlementForm;

	/// <summary>SettlementLevel int 值（0..4）。</summary>
	public int settlementLevel;

	public bool isCapital;

	public bool isPort;

	public bool isRuined;

	public int majorityCultureId = -1;

	public int stateReligionId = -1;

	public int realmId = -1;
}

/// <summary>角色行只读快照（UI 列表用）。</summary>
public sealed class CharacterRowDto
{
	public const int FlagAlive = 1;

	public const int FlagRuler = 2;

	public const int FlagPublicFigure = 4;

	public int characterId;

	public string name = "";

	public int age;

	public bool isMale;

	public int realmId = -1;

	/// <summary>Content.Definitions.CharacterRole int 值。</summary>
	public int role;

	public int officeTitleId = -1;

	/// <summary>FlagAlive/FlagRuler/FlagPublicFigure 位标志。</summary>
	public int flags;
}

/// <summary>政权查询契约（UI 4b 第一批）：实现适配器在 Simulation host 转调 RealmData，UI 只依赖本接口。</summary>
public interface IRealmQuery
{
	RealmSnapshotDto GetRealm(int realmId);

	IReadOnlyList<RealmSnapshotDto> GetAllRealms();

	IReadOnlyList<AdminDivisionNodeDto> GetAdminDivisions(int realmId);
}

/// <summary>聚落查询契约（UI 4b 第一批）。</summary>
/// <summary>城市区块行（俯视图布局输入）：只读快照，不含运行时对象。</summary>
public sealed class SettlementBlockRowDto
{
	public string blockId = "";
	public float areaRatio;
	public int districtType;
	public int kind;
	public bool isWall;
	public int wallLayerCount;
	public bool enabled = true;
}

public interface ISettlementQuery
{
	SettlementSnapshotDto GetSettlement(int anchorId);

	/// <summary>聚落区块列表（俯视图用）；未知聚落返回空表。</summary>
	IReadOnlyList<SettlementBlockRowDto> GetSettlementBlocks(int anchorId);

	IReadOnlyList<SettlementSnapshotDto> GetSettlementsInRealm(int realmId);

	/// <summary>按地块 Manhattan 距离（不环绕）筛选半径内聚落；radius&lt;0 返回空。</summary>
	IReadOnlyList<SettlementSnapshotDto> GetSettlementsNearTile(int tileIndex, int radius);
}

/// <summary>角色查询契约（UI 4b 第一批）。</summary>
public interface ICharacterQuery
{
	CharacterRowDto GetCharacter(int characterId);

	IReadOnlyList<CharacterRowDto> GetCharactersInRealm(int realmId);

	CharacterRowDto GetRuler(int realmId);
}
}
