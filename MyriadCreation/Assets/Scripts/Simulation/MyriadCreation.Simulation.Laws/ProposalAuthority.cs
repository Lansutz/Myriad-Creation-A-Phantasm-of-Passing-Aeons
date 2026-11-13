using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Laws
{
/// <summary>法律提案权归属：由法律自身配置，决定谁能推行该法。</summary>
public enum ProposalAuthority
{
	/// <summary>首脑独断：君主/执政官/摄政（首脑 id：ILawContext.GetSupremeRulerId）。</summary>
	HeadOfState,
	/// <summary>中央机构官员：政权中央机构需匹配 requiredInstitution，提案人须持有中央官职/行政区。</summary>
	CentralInstitution,
	/// <summary>特许权持有者：由首脑授予特许（RealmLawState.charterHolderIds）。</summary>
	CharterHolders
}
}
