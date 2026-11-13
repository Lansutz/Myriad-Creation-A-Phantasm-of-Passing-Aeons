using MyriadCreation.Simulation.Laws;
using MyriadCreation.Simulation.State;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.LawHost
{
public static class LawReform
{
	public static bool CanReform(RealmData realm, PolityComponentInnovations.PolityDimension dimension, int newComponent, InnovationTree innovations)
	{
		if (realm == null || innovations == null)
		{
			return false;
		}
		return PolityComponentInnovations.IsComponentAvailable(dimension, newComponent, innovations, realm.realmId);
	}

	public static bool Reform(RealmData realm, PolityComponentInnovations.PolityDimension dimension, int newComponent, InnovationTree innovations, Chronicle chronicle = null, int day = -1)
	{
		if (!CanReform(realm, dimension, newComponent, innovations))
		{
			return false;
		}
		string componentName = GetComponentName(dimension, GetCurrent(realm, dimension));
		string componentName2 = GetComponentName(dimension, newComponent);
		Apply(realm, dimension, newComponent);
		realm.stability = Mathf.Max(0f, realm.stability - 5f);
		chronicle?.Add("reform", realm.realmName + " 政体改革：" + componentName + " → " + componentName2 + "（稳定性下降）", true, realm.realmId);
		// 政变/派系路径成功后，把结果同步到 catalog 法律流水线（清同组 pending、并入 known、立即固化）；
		// 同步失败只记录日志，不影响上面已写入的 realm.laws 改革结果。
		if (LawRealmHost.ForceReformSync(realm, dimension, newComponent, (day >= 0) ? day : 0, out string syncReason))
		{
			chronicle?.Add("law_reform_sync", realm.realmName + " 派系/政变推动制度变更：" + componentName2 + " 已同步为现行制度", true, realm.realmId);
		}
		else if (!string.IsNullOrEmpty(syncReason))
		{
			Debug.Log("[LawReform] catalog 法律同步跳过：" + syncReason);
		}
		return true;
	}

	private static int GetCurrent(RealmData realm, PolityComponentInnovations.PolityDimension dimension)
	{
		LawComposition laws = realm.laws;
		return dimension switch
		{
			PolityComponentInnovations.PolityDimension.SupremeSuccession => laws.supremeSuccession.primary, 
			PolityComponentInnovations.PolityDimension.SupremeScope => laws.supremeScope.primary, 
			PolityComponentInnovations.PolityDimension.CentralSuccession => laws.centralSuccession.primary, 
			PolityComponentInnovations.PolityDimension.CentralInstitution => laws.centralInstitution.primary, 
			PolityComponentInnovations.PolityDimension.LocalSuccession => laws.localSuccession.primary, 
			PolityComponentInnovations.PolityDimension.LocalScope => laws.localScope.primary, 
			PolityComponentInnovations.PolityDimension.SpatialStructure => laws.spatialStructure.primary, 
			_ => -1, 
		};
	}

	private static void Apply(RealmData realm, PolityComponentInnovations.PolityDimension dimension, int component)
	{
		LawComposition laws = realm.laws;
		switch (dimension)
		{
		case PolityComponentInnovations.PolityDimension.SupremeSuccession:
			laws.supremeSuccession.primary = component;
			break;
		case PolityComponentInnovations.PolityDimension.SupremeScope:
			laws.supremeScope.primary = component;
			break;
		case PolityComponentInnovations.PolityDimension.CentralSuccession:
			laws.centralSuccession.primary = component;
			break;
		case PolityComponentInnovations.PolityDimension.CentralInstitution:
			laws.centralInstitution.primary = component;
			break;
		case PolityComponentInnovations.PolityDimension.LocalSuccession:
			laws.localSuccession.primary = component;
			break;
		case PolityComponentInnovations.PolityDimension.LocalScope:
			laws.localScope.primary = component;
			break;
		case PolityComponentInnovations.PolityDimension.SpatialStructure:
			laws.spatialStructure.primary = component;
			break;
		}
	}

	private static string GetComponentName(PolityComponentInnovations.PolityDimension dimension, int component)
	{
		return dimension switch
		{
			PolityComponentInnovations.PolityDimension.SupremeSuccession => LawComponentNames.NameSupremeSuccession(component), 
			PolityComponentInnovations.PolityDimension.SupremeScope => LawComponentNames.NameSupremeScope(component), 
			PolityComponentInnovations.PolityDimension.CentralSuccession => LawComponentNames.NameCentralSuccession(component), 
			PolityComponentInnovations.PolityDimension.CentralInstitution => LawComponentNames.NameCentralInstitution(component), 
			PolityComponentInnovations.PolityDimension.LocalSuccession => LawComponentNames.NameLocalSuccession(component), 
			PolityComponentInnovations.PolityDimension.LocalScope => LawComponentNames.NameLocalScope(component), 
			PolityComponentInnovations.PolityDimension.SpatialStructure => LawComponentNames.NameSpatialStructure(component), 
			_ => "?", 
		};
	}
}
}
