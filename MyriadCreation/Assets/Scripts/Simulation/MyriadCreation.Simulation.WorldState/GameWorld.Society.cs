using System.Collections.Generic;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Simulation.WorldState
{
public partial class GameWorld
{
	/// <summary>D25 四轴人口报告（阶级/阶层/身份/职业各一套）；只读。</summary>
	public List<SocialAxisReport> GetSocialAxisReports(int realmId)
	{
		if (realms == null || !realms.TryGetValue(realmId, out RealmData realm) || realm == null)
		{
			return SocialAxisReportSystem.Build(this, realmId);
		}
		RealmSituation situation = RealmSituationBuilder.Build(realm, tiles, _economyManager, _wars, armies, _disasterSystem, _innovationTree);
		return SocialAxisReportSystem.Build(this, realmId, situation);
	}

	/// <summary>四轴加权 unrest（0–100）；权重可配，改权重即可复现不同结果。</summary>
	public float GetSocialAxisUnrest(int realmId, SocialAxisWeights weights = null)
	{
		return SocialAxisReportSystem.ComputeAxisUnrest(GetSocialAxisReports(realmId), weights);
	}
}
}
