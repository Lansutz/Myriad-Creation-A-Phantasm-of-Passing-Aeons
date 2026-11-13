using System;
using System.Collections.Generic;

namespace MyriadCreation.Simulation.Laws
{
[Serializable]
public class LawGroupState
{
	public string groupId = "";

	public string activeLawId = "";

	public int enactedDay = -1;

	/// <summary>当前组的生命周期阶段；Active 之后可 Consolidated/Fundamentalized。</summary>
	public LegalNormStage stage = LegalNormStage.Active;

	/// <summary>当前法律已连续生效的整年数；换法时清零。</summary>
	public int yearsActive;

	/// <summary>本组被替换下来的旧法 id（废除记录；不丢历史）。</summary>
	public List<string> repealedLawIds = new List<string>();
}
}
