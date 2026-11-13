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
		return SocialAxisReportSystem.Build(this, realmId);
	}
}
}
