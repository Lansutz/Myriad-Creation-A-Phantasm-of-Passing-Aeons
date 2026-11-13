using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
public static class DiplomacyRelationBridge
{
	public static RelationType MapSubordination(SubordinationType type)
	{
		return type switch
		{
			SubordinationType.Tributary => RelationType.Tributary, 
			SubordinationType.Protectorate => RelationType.Protectorate, 
			_ => RelationType.Vassalage, 
		};
	}

	public static List<EntityRelation> ProjectSubordinations(GameWorld world)
	{
		List<EntityRelation> list = new List<EntityRelation>();
		DiplomacyManager diplomacyManager = world?.GetDiplomacyManager();
		if (diplomacyManager == null)
		{
			return list;
		}
		IReadOnlyList<Subordination> allSubordinations = diplomacyManager.GetAllSubordinations();
		if (allSubordinations == null)
		{
			return list;
		}
		foreach (Subordination item in allSubordinations)
		{
			if (item != null && item.isActive)
			{
				list.Add(new EntityRelation
				{
					from = new EntityRef(EntityKind.Realm, item.suzerainId),
					to = new EntityRef(EntityKind.Realm, item.vassalId),
					type = MapSubordination(item.type),
					isPrimary = true,
					isProjection = true,
					sinceDay = item.establishedDay,
					obligations = BuildObligations(item)
				});
			}
		}
		return list;
	}

	public static List<Contract> ProjectTreaties(GameWorld world)
	{
		List<Contract> list = new List<Contract>();
		DiplomacyManager diplomacyManager = world?.GetDiplomacyManager();
		if (diplomacyManager == null || world.realms == null)
		{
			return list;
		}
		List<int> list2 = new List<int>(world.realms.Keys);
		for (int i = 0; i < list2.Count; i++)
		{
			for (int j = i + 1; j < list2.Count; j++)
			{
				DiplomaticRelation relation = diplomacyManager.GetRelation(list2[i], list2[j]);
				if (relation == null)
				{
					continue;
				}
				if (relation.activeTreaties != null)
				{
					foreach (Treaty activeTreaty in relation.activeTreaties)
					{
						if (activeTreaty != null && activeTreaty.isActive && !activeTreaty.IsExpired(world.currentDay))
						{
							list.Add(new Contract
							{
								contractId = "treaty-" + activeTreaty.treatyId,
								type = ContractType.TradeAgreement,
								parties = 
								{
									new EntityRef(EntityKind.Realm, activeTreaty.signerAId),
									new EntityRef(EntityKind.Realm, activeTreaty.signerBId)
								},
								startDay = activeTreaty.signedDay,
								expiresDay = activeTreaty.expiryDay
							});
						}
					}
				}
				if (relation.activeAlliances == null)
				{
					continue;
				}
				foreach (Alliance activeAlliance in relation.activeAlliances)
				{
					if (activeAlliance != null && activeAlliance.isActive && !activeAlliance.IsExpired(world.currentDay))
					{
						list.Add(new Contract
						{
							contractId = "alliance-" + activeAlliance.realmAId + "-" + activeAlliance.realmBId,
							type = ContractType.AllianceTreaty,
							parties = 
							{
								new EntityRef(EntityKind.Realm, activeAlliance.realmAId),
								new EntityRef(EntityKind.Realm, activeAlliance.realmBId)
							},
							startDay = activeAlliance.signedDay,
							expiresDay = ((activeAlliance.durationDays > 0) ? (activeAlliance.signedDay + activeAlliance.durationDays) : (-1))
						});
					}
				}
			}
		}
		return list;
	}

	private static List<string> BuildObligations(Subordination sub)
	{
		List<string> list = new List<string>();
		if (sub.tributeAmount > 0f || sub.tributeRatio > 0f)
		{
			list.Add("tribute");
		}
		if (sub.militaryObligation)
		{
			list.Add("military_service");
		}
		if (sub.foreignPolicyControl)
		{
			list.Add("foreign_policy_control");
		}
		if (sub.successionControl)
		{
			list.Add("succession_control");
		}
		if (sub.autonomy < 1f)
		{
			list.Add("autonomy:" + sub.autonomy.ToString("0.00"));
		}
		return list;
	}
}
}
