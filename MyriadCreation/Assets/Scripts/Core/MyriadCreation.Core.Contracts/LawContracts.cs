using System.Collections.Generic;

namespace MyriadCreation.Core.Contracts
{
/// <summary>法律/制度条目视图（已固化/可推行/推行中通用；label 已由 Simulation 侧格式化）。</summary>
public sealed class LawItemDto
{
	public string lawId = "";

	public string groupId = "";

	public string label = "";
}

/// <summary>法令条目视图（正在生效/正在推行/可颁布；label 已格式化）。</summary>
public sealed class DecreeItemDto
{
	public string decreeId = "";

	public string label = "";

	public int orderCount;
}

/// <summary>推行中法律信息（供非 UI 消费者按数据读取）。</summary>
public sealed class PendingLawInfoDto
{
	public string lawId = "";

	public string groupId = "";

	public string name = "";

	public int remainingDays;

	public int proposerCharacterId = -1;
}

/// <summary>政府面板三段式视图（已固化制度/生效法令/推行中法令 + 候选按钮数据）。</summary>
public sealed class LawGovernmentViewDto
{
	public List<LawItemDto> enactedLaws = new List<LawItemDto>();

	public List<LawItemDto> pendingLaws = new List<LawItemDto>();

	public List<DecreeItemDto> activeDecrees = new List<DecreeItemDto>();

	public List<DecreeItemDto> pendingDecrees = new List<DecreeItemDto>();

	public List<LawItemDto> proposableLaws = new List<LawItemDto>();

	public List<DecreeItemDto> enactableDecrees = new List<DecreeItemDto>();
}

/// <summary>
/// 法律/法令系统查询契约（B6）。参数只用 id/字符串，避免 Core→Simulation 反向依赖；
/// 实现适配器在 Simulation.Laws 内转调 LawAndDecreeSystem/LawDecreeViewBuilder。
/// </summary>
public interface ILawQuery
{
	/// <summary>某组当前生效法律 id；无则返回空串。</summary>
	string GetActiveLaw(int realmId, string groupId);

	IReadOnlyList<PendingLawInfoDto> GetPendingLaws(int realmId);

	IReadOnlyList<string> GetKnownLaws(int realmId);

	/// <summary>政府面板三段式视图（含已格式化 label）。</summary>
	LawGovernmentViewDto GetGovernmentView(int realmId, int currentDay);
}

/// <summary>法律/法令系统命令契约（B6，最小集）。</summary>
public interface ILawCommand
{
	/// <summary>补齐政权已知法律集（旧档/新政权初始化用）。</summary>
	void EnsureKnownLaws(int realmId);

	/// <summary>推进推行中的法律；返回本 tick 完成数量。</summary>
	int TickPendingLaws(int realmId, int currentDay);

	/// <summary>清理到期法令（active/cooldown）。</summary>
	void TickDecreeExpiry(int realmId, int currentDay);

	bool BeginEnactLaw(int realmId, string lawId, int proposerCharacterId, int currentDay, out string reason);

	bool CancelPendingLaw(int realmId, string groupId, int requesterCharacterId, out string reason);

	bool EnactDecree(int realmId, string decreeId, int currentDay, out string reason);

	bool RevokeDecree(int realmId, string decreeId, out string reason);

	/// <summary>派系/政变直改路径（绕过已知/授权/推行）。</summary>
	bool ForceSetLaw(int realmId, string groupId, string lawId, int currentDay, out string reason);
}
}
