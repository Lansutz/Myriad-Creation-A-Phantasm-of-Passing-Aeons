using System.Collections.Generic;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Laws;
using UnityEngine;

namespace MyriadCreation.Simulation.LawHost
{
public static class LawCompositionMigration
{
	public static RealmLawState ToLawState(LawComposition legislation)
	{
		RealmLawState realmLawState = new RealmLawState();
		if (legislation == null)
		{
			return realmLawState;
		}
		int num = ((legislation.nationalTax != null) ? legislation.nationalTax.primary : (-1));
		if (num >= 0)
		{
			string lawId;
			if (num >= 2)
			{
				lawId = "tax_heavy";
			}
			else
			{
				lawId = ((num == 1) ? "tax_standard" : "tax_light");
			}
			TrySetMigratedLaw(realmLawState, "taxation", lawId);
		}
		int num2 = ((legislation.militaryOrganization != null) ? legislation.militaryOrganization.primary : (-1));
		int num3 = ((legislation.militaryRecruitment != null) ? legislation.militaryRecruitment.primary : (-1));
		if (num2 >= 0)
		{
			TrySetMigratedLaw(realmLawState, "military", "standing_army");
		}
		else if (num3 >= 0)
		{
			TrySetMigratedLaw(realmLawState, "military", "militia");
		}
		return realmLawState;
	}

	/// <summary>
	/// 旧档/新政权法律状态兜底：空状态按 LawComposition 迁移，并补齐"已知法律"集
	/// （旧档没有 knownLawIds 字段时，把初始已知法与当前生效法补进去，保证旧档可提案）。
	/// </summary>
	public static void MigrateIfEmpty(RealmData realm)
	{
		if (realm == null)
		{
			return;
		}
		if (realm.lawState == null || realm.lawState.groups == null || realm.lawState.groups.Count <= 0)
		{
			realm.lawState = ToLawState(realm.laws);
		}
		LawRealmHost.EnsureKnownLaws(realm);
	}

	private static void TrySetMigratedLaw(RealmLawState state, string groupId, string lawId)
	{
		Dictionary<string, LawDef> laws = DefaultLawCatalog.IndexLaws(DefaultLawCatalog.CreateGroups());
		if (laws == null || !laws.TryGetValue(lawId, out LawDef law) || law == null)
		{
			Debug.LogWarning("[LawCompositionMigration] 旧档映射 lawId 不在当前法律目录，跳过：" + lawId + "（groupId=" + groupId + "，无 JSON 时请确认最小回退集）");
			return;
		}
		state.SetActiveLaw(groupId, lawId, 0);
	}

	public static Dictionary<string, LawDef> DefaultLaws()
	{
		return DefaultLawCatalog.IndexLaws(DefaultLawCatalog.CreateGroups());
	}
}
}
