using System.Collections.Generic;

namespace MyriadCreation.Core.Contracts
{
/// <summary>
/// 文化系统查询契约（B5）。参数/返回值只用 int/string 等 Core 可表达类型，
/// 避免 Core→Simulation 反向依赖；实现适配器在 Simulation.Culture 内转调 CultureData。
/// </summary>
public interface ICultureQuery
{
	/// <summary>政权主体文化 id；未设返回 -1。</summary>
	int GetPrimaryCultureId(int realmId);

	string GetCultureName(int cultureId);

	/// <summary>文化阶段（对应 GameEnums.CultureStage 的 int 值）；未知返回 -1。</summary>
	int GetCultureStage(int cultureId);

	/// <summary>阶段 key（如 Band/Tribe/Chiefdom/EthnicGroup/HighCivilization）；未知返回 "Unknown"。</summary>
	string GetCultureStageKey(int stage);
}

/// <summary>
/// 文化系统宿主上下文契约（B5）：由 host（Simulation）实现并注入；
/// Culture 运行时通过 id + 简单值读取世界事实，不直接引用 GameWorld/RealmData/聚落类型。
/// </summary>
public interface ICultureContext
{
	int CurrentDay { get; }

	IReadOnlyList<int> GetRealmIds();

	/// <summary>政权主体文化 id；未设返回 -1。</summary>
	int GetRealmCultureId(int realmId);

	/// <summary>文化阶段（GameEnums.CultureStage 的 int 值）；未知返回 -1。</summary>
	int GetCultureStage(int cultureId);

	/// <summary>设置文化阶段（0..4）；非法 stage/文化不存在时忽略。</summary>
	void SetCultureStage(int cultureId, int stage);

	/// <summary>政权社会分层/专业化/人口快照（host 由 SocialStratificationCalculator 计算）。</summary>
	void GetRealmSocialState(int realmId, out float socialStratification, out float specialization, out int population);

	string GetCultureName(int cultureId);

	IReadOnlyList<int> GetRealmInnovationIds(int realmId);

	bool HasUrbanCenter(int realmId);
}

/// <summary>文化系统命令契约（B5，最小集）。</summary>
public interface ICultureCommand
{
	bool SetPrimaryCulture(int realmId, int cultureId, out string reason);

	/// <summary>设置文化阶段（0..4，对应 GameEnums.CultureStage）。</summary>
	bool SetCultureStage(int cultureId, int stage, out string reason);
}
}
