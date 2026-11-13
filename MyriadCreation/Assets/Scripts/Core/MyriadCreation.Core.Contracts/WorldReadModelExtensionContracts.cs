using System.Collections.Generic;

namespace MyriadCreation.Core.Contracts
{
/// <summary>社会阶级只读快照（UI 4b3；按 SocietyPanel/ClassPanelText 使用点最小集）。</summary>
public sealed class SocialClassSnapshotDto
{
	/// <summary>Core.Enums.GameEnums.SocialClass int 值。</summary>
	public int classId;

	public string className = "";

	public float population;

	public float populationShare;

	/// <summary>该阶级在当前政权是否已形成/可用。</summary>
	public bool unlocked;

	public string availabilityReason = "";

	public float satisfaction;

	public float loyalty;

	public float organization;

	public float influence;

	public float unrest;

	public float support;

	/// <summary>ClassNeedDimension int 值。</summary>
	public int chiefGrievanceId;

	public string chiefGrievanceReason = "";

	public IReadOnlyList<ClassNeedScoreDto> needScores = new List<ClassNeedScoreDto>();
}

/// <summary>阶级需求维度评分（UI 展示条用）。</summary>
public sealed class ClassNeedScoreDto
{
	public int dimensionId;

	public string dimensionName = "";

	public float score;

	public string reason = "";
}

/// <summary>政权社会快照（UI 4b3）。</summary>
public sealed class SocietySnapshotDto
{
	public int realmId;

	public string realmName = "";

	public float totalPopulation;

	public float unrestScore;

	public int dominantClassId;

	public string dominantClassName = "";

	public int mostRestlessClassId;

	public string mostRestlessClassName = "";

	public IReadOnlyList<SocialClassSnapshotDto> classes = new List<SocialClassSnapshotDto>();
}

/// <summary>社会/阶级查询契约（UI 4b3）：实现适配器在 Simulation host 转调 RealmSociety/ClassProfile。</summary>
public interface ISocietyQuery
{
	/// <summary>政权社会快照；无 society 数据/非法 realm 返回 null。</summary>
	SocietySnapshotDto GetSociety(int realmId);
}

/// <summary>按聚落 anchor 的人物查询契约（UI 4b3；复用 CharacterRowDto）。</summary>
public interface ISettlementPeopleQuery
{
	/// <summary>观察者政权已知的、当前位于该 anchor 的活人。</summary>
	IReadOnlyList<CharacterRowDto> GetKnownCharactersAtAnchor(int anchorId, int observerRealmId = -1);

	/// <summary>当前位于该 anchor 的公共人物（isPublicFigure）。</summary>
	IReadOnlyList<CharacterRowDto> GetPublicFiguresAtAnchor(int anchorId);
}

/// <summary>政权革新快照（UI 4b3；已研究数/已研究 id 的最小 DTO）。</summary>
public sealed class InnovationSnapshotDto
{
	public int realmId;

	public int researchedCount;

	public IReadOnlyList<int> researchedInnovationIds = new List<int>();
}

/// <summary>革新候选条目（UI 4b5a；可研究/需求列表展示用）。</summary>
public sealed class InnovationAvailabilityDto
{
	public int innovationId;

	public string name = "";

	public bool researched;

	public bool canResearch;

	public IReadOnlyList<int> prerequisiteIds = new List<int>();
}

/// <summary>革新只读查询契约（UI 4b3/4b5a；UIManager.Panels 革新树/已研究数/可采判定的最小出口）。</summary>
public interface IInnovationQuery
{
	int GetResearchedCount(int realmId);

	IReadOnlyList<int> GetResearchedInnovationIds(int realmId);

	InnovationSnapshotDto GetInnovationSnapshot(int realmId);

	bool HasInnovation(int realmId, int innovationId);

	string GetInnovationName(int innovationId);

	/// <summary>当前政权可研究（前置满足且未研究）的革新列表。</summary>
	IReadOnlyList<InnovationAvailabilityDto> GetAvailableInnovations(int realmId);

	/// <summary>革新前置 id 列表（and + any 合并；无则空）。</summary>
	IReadOnlyList<int> GetPrerequisiteInnovationIds(int innovationId);

	bool CanResearch(int realmId, int innovationId);

	/// <summary>货物是否已满足技术前置（ResourceAccess.IsTechAccessible 的契约出口）。</summary>
	bool IsTechAccessibleForGoods(int goodsId, int realmId);

	/// <summary>资源开采深度是否可采（ResourceAccessDepth int 值；ResourceAccess.IsDepthAccessible 的契约出口）。</summary>
	bool IsResourceDepthAccessible(int accessDepth, int realmId);

	/// <summary>货物+深度是否同时可采（技术 + 深度）。</summary>
	bool CanAccessResource(int goodsId, int accessDepth, int realmId);
}
}
