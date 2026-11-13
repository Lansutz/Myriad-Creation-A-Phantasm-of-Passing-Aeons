using System;
using System.Collections.Generic;
using MyriadCreation.Core.Enums;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Laws
{
[Serializable]
public class LawDef
{
	public string lawId = "";

	public string groupId = "";

	public string name = "";

	public string description = "";

	public int tier = 1;

	public LawRequirement requirements = new LawRequirement();

	public LawEffect effects = new LawEffect();

	public float enactmentCostTreasury;

	public float enactmentCostPrestige;

	public float upkeepPerYear;

	/// <summary>是否在政权初始已知集中（false = 需文化/革新/接触等来源解锁）。</summary>
	public bool isInitiallyKnown = true;

	/// <summary>该组无主动选择时使用的兜底条款。</summary>
	public bool isFallback;

	/// <summary>规范类型：成文法/特许/根本法/习惯等，冲突排序用。</summary>
	public LegalNormKind normKind = LegalNormKind.Statute;

	/// <summary>适用层级：realm/province/lordship/locality/personal。</summary>
	public LegalScope scope = LegalScope.Realm;

	/// <summary>冲突优先级；同类型同层级再比较具体性/时间。</summary>
	public int precedence;

	/// <summary>授予/发布者角色 id；-1=政权本身。</summary>
	public int issuerCharacterId = -1;

	/// <summary>明确可覆盖的低阶规范 id；空=不声明覆盖。</summary>
	public List<string> overrides = new List<string>();

	/// <summary>与这些法律互斥；任一已生效则不能推行。</summary>
	public List<string> excludesLawIds = new List<string>();

	/// <summary>推行所需天数（登记为 pending 后到期才替换现行法律）。</summary>
	public int enactmentDays = 30;

	/// <summary>谁能提案：首脑 / 中央机构官员 / 特许权持有者。</summary>
	public ProposalAuthority proposalAuthority = ProposalAuthority.HeadOfState;

	/// <summary>提案权为中央机构时要求的中央机构（None = 任意中央机构官员）。</summary>
	public CentralInstitution requiredInstitution = CentralInstitution.None;

	/// <summary>适用身份掩码；None = 所有人。与 exemptIdentities 同时命中时豁免优先。</summary>
	public SocialIdentityFlags appliesTo = SocialIdentityFlags.None;

	/// <summary>豁免身份掩码；命中即不适用（教士法庭/贵族特权等）。</summary>
	public SocialIdentityFlags exemptIdentities = SocialIdentityFlags.None;
}
}
