using System.Collections.Generic;

namespace MyriadCreation.Core.Contracts
{
/// <summary>信仰摘要（跨系统只读视图，Core 可表达，不引用 Simulation 类型）。</summary>
public sealed class FaithSnapshotDto
{
	public int faithId;

	public string faithName = "";

	public float fervor;

	public int holySiteCount;

	/// <summary>FaithType 枚举 int 值（Content.Definitions.FaithType）。</summary>
	public int faithType;

	/// <summary>FaithOrganizationType 枚举 int 值（Content.Definitions.FaithOrganizationType）。</summary>
	public int faithOrganizationType;

	/// <summary>faithOrganizationType 的短别名（供 UI 直接使用）。</summary>
	public int organizationType;

	/// <summary>教阶 0..4。</summary>
	public int hierarchyLevel;

	/// <summary>宗教权威（FaithSystem.CalculateReligiousAuthority）。</summary>
	public float authority;

	/// <summary>该信仰是否为玩家/首个政权国教。</summary>
	public bool isStateReligion;

	public int highPriestCharacterId = -1;

	/// <summary>教阶中文名（由 host 按 hierarchyLevel 映射，UI 直接显示）。</summary>
	public string hierarchyName = "";

	/// <summary>改革支柱列表（ReligionDef.selectedDoctrines 或 FaithSystem.doctrines 的 pillar 去重）。</summary>
	public IReadOnlyList<string> reformPillars = new List<string>();
}

/// <summary>圣人条目（封圣，UI 信仰面板用）。</summary>
public sealed class FaithSaintDto
{
	public int saintId;

	public string saintName = "";

	public string domain = "";

	public float canonizationPiety;

	public int linkedCharacterId = -1;
}

/// <summary>教义/支柱条目（UI 信仰面板用；最小字段）。</summary>
public sealed class FaithDoctrineDto
{
	public string doctrineId = "";

	public string name = "";

	public string pillar = "";

	public string description = "";
}

/// <summary>
/// 信仰系统查询契约（B4）。UI/Rendering/其它系统依赖本接口而不是 Simulation.Faith 具体类，
/// 实现适配器在 Simulation 侧转调现有 FaithSystem/RealmData API。
/// </summary>
public interface IFaithQuery
{
	/// <summary>政权国教 faithId；未设国教返回 -1。</summary>
	int GetStateReligionId(int realmId);

	bool HasFaith(int faithId);

	string GetFaithName(int faithId);

	float GetFervor(int faithId);

	IReadOnlyList<int> GetHolySiteTiles(int faithId);

	bool TryGetFaithSnapshot(int faithId, out FaithSnapshotDto snapshot);

	/// <summary>该政权当前国教是否满足大圣战条件（转调 FaithSystem.CanDeclareGreatHolyWar）。</summary>
	bool CanDeclareGreatHolyWar(int realmId);

	IReadOnlyList<FaithSaintDto> GetSaints(int faithId);

	IReadOnlyList<FaithDoctrineDto> GetDoctrines(int faithId);

	/// <summary>政权主保圣人 id；无则 -1。</summary>
	int GetPatronSaintId(int realmId);

	/// <summary>政权主保圣人名字；无/未找到返回空串。</summary>
	string GetPatronSaintName(int realmId);
}

/// <summary>
/// 信仰系统宿主上下文契约（B4，只读）：host 实现并注入；
/// Faith 运行时只通过 id + 简单值读取角色/政权/教统事实，不引用 Simulation.State/Systems/WorldState。
/// </summary>
public interface IFaithContext
{
	int GetDay();

	int GetRealmStateReligionId(int realmId);

	float GetFaithFervor(int faithId);

	IReadOnlyList<int> GetHolySiteIds(int faithId);

	/// <summary>教统在区域表中的 adherence 条目数（无则 0）。</summary>
	int GetAdherenceCount(int faithId);

	string GetCharacterName(int characterId);

	/// <summary>角色性格特质 id 列表（Faith 的 virtue/sin 评分用；空列表表示无）。</summary>
	IReadOnlyList<string> GetCharacterTraitIds(int characterId);
}

/// <summary>信仰系统命令契约（B4，最小集）。</summary>
public interface IFaithCommand
{
	/// <summary>设置政权国教；faithId=-1 表示清除。失败给 reason。</summary>
	bool SetStateReligion(int realmId, int faithId, out string reason);

	bool AddFervor(int faithId, float delta, out string reason);
}
}
