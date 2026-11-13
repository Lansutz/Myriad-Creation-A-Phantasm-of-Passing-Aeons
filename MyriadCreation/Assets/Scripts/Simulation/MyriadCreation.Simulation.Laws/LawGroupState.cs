using System;

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
}
}
