using System.Collections.Generic;

namespace MyriadCreation.Core.Contracts
{
/// <summary>派系只读快照（UI 4b5a；SocietyPanel 展示用，仅 Core 类型）。</summary>
public sealed class FactionSnapshotDto
{
	public int factionId;

	public int realmId;

	public string factionName = "";

	/// <summary>Content.Definitions.FactionStance int 值。</summary>
	public int stanceId;

	public string stanceName = "";

	public float power;

	/// <summary>占该政权全部派系 power 的百分比（0..100；无派系时 0）。</summary>
	public float share;

	public bool isRuling;

	public float cohesion;

	public int leaderCharacterId = -1;

	/// <summary>Core.Enums.GameEnums.SocialClass int 值。</summary>
	public int primaryClassId;
}

/// <summary>派系查询契约（UI 4b5a）。</summary>
public interface IFactionQuery
{
	IReadOnlyList<FactionSnapshotDto> GetFactions(int realmId);
}

/// <summary>政体变迁/政变态势只读快照（UI 4b5a；SocietyPanel 展示用，仅 Core 类型）。</summary>
public sealed class RegimeSnapshotDto
{
	public int realmId;

	/// <summary>政变风险 0..100（host 由 tension + 改革/守旧派力量差推导）。</summary>
	public float coupRisk;

	public bool hasOpenWindow;

	/// <summary>Content.Definitions.CriticalJunctureType int 值。</summary>
	public int junctureType;

	public string junctureName = "";

	/// <summary>Content.Definitions.JunctureOutcomeType int 值。</summary>
	public int junctureOutcome;

	public string junctureOutcomeName = "";

	public float junctureSeverity;

	public int junctureRemainingDays;

	public float institutionalInertia;

	public float tensionTotal;

	public float tensionClassMismatch;

	public float tensionFiscalMilitary;

	public float tensionLegitimacyErosion;

	/// <summary>当前/历史关键节点事件文案（RegimeChangeState.history 原样；UI 自行截断）。</summary>
	public IReadOnlyList<string> activeEvents = new List<string>();
}

/// <summary>政体变迁查询契约（UI 4b5a）。</summary>
public interface IRegimeQuery
{
	RegimeSnapshotDto GetRegime(int realmId);
}
}
