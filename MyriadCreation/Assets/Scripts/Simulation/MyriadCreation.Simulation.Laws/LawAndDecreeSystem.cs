using System;
using System.Collections.Generic;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Laws
{
public static class LawAndDecreeSystem
{
	public static bool CanEnactLaw(LawDef law, RealmLawState state, float treasury, float prestige, float legitimacy, float centralization, IReadOnlyCollection<int> ownedInnovationIds, out string reason)
	{
		reason = "";
		if (law == null)
		{
			reason = "法律不存在";
			return false;
		}
		if (state == null)
		{
			reason = "政权法律状态不存在";
			return false;
		}
		if (string.IsNullOrEmpty(law.groupId))
		{
			reason = "法律未归组";
			return false;
		}
		LawRequirement lawRequirement = law.requirements ?? new LawRequirement();
		if (treasury < law.enactmentCostTreasury)
		{
			reason = "财政不足";
			return false;
		}
		if (prestige < law.enactmentCostPrestige)
		{
			reason = "威望不足";
			return false;
		}
		if (legitimacy < lawRequirement.minLegitimacy)
		{
			reason = "合法性不足";
			return false;
		}
		if (centralization < lawRequirement.minCentralization)
		{
			reason = "集权度不足";
			return false;
		}
		if (treasury < lawRequirement.minTreasury)
		{
			reason = "财政储备不足";
			return false;
		}
		if (lawRequirement.requiredLawIds != null)
		{
			foreach (string requiredLawId in lawRequirement.requiredLawIds)
			{
				if (!string.IsNullOrEmpty(requiredLawId) && state.GetActiveLaw(FindGroupOf(state, requiredLawId)) != requiredLawId && !IsLawActive(state, requiredLawId))
				{
					reason = "缺少前置法律 " + requiredLawId;
					return false;
				}
			}
		}
		if (law.excludesLawIds != null)
		{
			foreach (string excluded in law.excludesLawIds)
			{
				if (!string.IsNullOrEmpty(excluded) && IsLawActive(state, excluded))
				{
					reason = "与已生效法律互斥：" + excluded;
					return false;
				}
			}
		}
		if (ownedInnovationIds != null && lawRequirement.requiredInnovationIds != null)
		{
			foreach (int requiredInnovationId in lawRequirement.requiredInnovationIds)
			{
				bool flag = false;
				foreach (int ownedInnovationId in ownedInnovationIds)
				{
					if (ownedInnovationId == requiredInnovationId)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					reason = $"缺少革新 {requiredInnovationId}";
					return false;
				}
			}
		}
		return true;
	}

	public static LawDef EnactLaw(LawDef law, RealmLawState state, ref float treasury, ref float prestige, int day, float legitimacy, float centralization, IReadOnlyCollection<int> ownedInnovationIds, out string reason)
	{
		if (!CanEnactLaw(law, state, treasury, prestige, legitimacy, centralization, ownedInnovationIds, out reason))
		{
			return null;
		}
		treasury -= law.enactmentCostTreasury;
		prestige -= law.enactmentCostPrestige;
		state.SetActiveLaw(law.groupId, law.lawId, day);
		return law;
	}

	public static bool CanEnactDecree(DecreeDef decree, RealmLawState lawState, RealmDecreeState decreeState, float treasury, float authority, float legitimacy, int currentDay, IReadOnlyCollection<int> ownedInnovationIds, out string reason)
	{
		reason = "";
		if (decree == null)
		{
			reason = "法令不存在";
			return false;
		}
		if (decreeState == null)
		{
			reason = "法令状态不存在";
			return false;
		}
		if (IsDecreeActive(decreeState, decree.decreeId, currentDay))
		{
			reason = "法令已生效";
			return false;
		}
		if (GetCooldownDay(decreeState, decree.decreeId) > currentDay)
		{
			reason = "法令冷却中";
			return false;
		}
		if (treasury < decree.costTreasury)
		{
			reason = "财政不足";
			return false;
		}
		if (authority < decree.costAuthority)
		{
			reason = "权威不足";
			return false;
		}
		LawRequirement lawRequirement = decree.requirements ?? new LawRequirement();
		if (legitimacy < lawRequirement.minLegitimacy)
		{
			reason = "合法性不足";
			return false;
		}
		if (lawRequirement.requiredLawIds != null)
		{
			foreach (string requiredLawId in lawRequirement.requiredLawIds)
			{
				if (!string.IsNullOrEmpty(requiredLawId) && !IsLawActive(lawState, requiredLawId))
				{
					reason = "缺少前置法律 " + requiredLawId;
					return false;
				}
			}
		}
		if (lawRequirement.requiredDecreeIds != null)
		{
			foreach (string requiredDecreeId in lawRequirement.requiredDecreeIds)
			{
				if (!string.IsNullOrEmpty(requiredDecreeId) && !IsDecreeActive(decreeState, requiredDecreeId, currentDay))
				{
					reason = "缺少前置法令 " + requiredDecreeId;
					return false;
				}
			}
		}
		if (decree.excludesDecreeIds != null)
		{
			foreach (string excluded in decree.excludesDecreeIds)
			{
				if (!string.IsNullOrEmpty(excluded) && IsDecreeActive(decreeState, excluded, currentDay))
				{
					reason = "与已生效法令互斥：" + excluded;
					return false;
				}
			}
		}
		if (ownedInnovationIds != null && lawRequirement.requiredInnovationIds != null)
		{
			foreach (int requiredInnovationId in lawRequirement.requiredInnovationIds)
			{
				bool flag = false;
				foreach (int ownedInnovationId in ownedInnovationIds)
				{
					if (ownedInnovationId == requiredInnovationId)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					reason = $"缺少革新 {requiredInnovationId}";
					return false;
				}
			}
		}
		return true;
	}

	public static ActiveDecree EnactDecree(DecreeDef decree, RealmLawState lawState, RealmDecreeState decreeState, ref float treasury, ref float authority, float legitimacy, int currentDay, IReadOnlyCollection<int> ownedInnovationIds, out string reason)
	{
		if (!CanEnactDecree(decree, lawState, decreeState, treasury, authority, legitimacy, currentDay, ownedInnovationIds, out reason))
		{
			return null;
		}
		treasury -= decree.costTreasury;
		authority -= decree.costAuthority;
		ActiveDecree activeDecree = new ActiveDecree
		{
			decreeId = decree.decreeId,
			startDay = currentDay,
			endDay = currentDay + Math.Max(1, decree.durationDays)
		};
		decreeState.active ??= new List<ActiveDecree>();
		decreeState.active.Add(activeDecree);
		SetCooldown(decreeState, decree.decreeId, currentDay + Math.Max(0, decree.cooldownDays));
		return activeDecree;
	}

	public static bool RevokeDecree(RealmDecreeState state, string decreeId, out ActiveDecree revoked)
	{
		revoked = null;
		if (state == null || state.active == null || string.IsNullOrEmpty(decreeId))
		{
			return false;
		}
		for (int i = 0; i < state.active.Count; i++)
		{
			if (!(state.active[i].decreeId != decreeId))
			{
				revoked = state.active[i];
				state.active.RemoveAt(i);
				return true;
			}
		}
		return false;
	}

	public static void TickDecreeExpiry(RealmDecreeState state, int currentDay)
	{
		if (state != null)
		{
			state.active?.RemoveAll((ActiveDecree d) => currentDay >= d.endDay);
			state.cooldowns?.RemoveAll((DecreeCooldown c) => currentDay >= c.readyDay);
		}
	}

	public static LawEffect SumLawEffects(RealmLawState state, IReadOnlyDictionary<string, LawDef> lawById)
	{
		LawEffect lawEffect = new LawEffect();
		if (state == null || lawById == null || state.groups == null)
		{
			return lawEffect;
		}
		foreach (LawGroupState group in state.groups)
		{
			if (group == null)
			{
				continue;
			}
			if (!string.IsNullOrEmpty(group.activeLawId) && lawById.TryGetValue(group.activeLawId, out var value))
			{
				AddEffects(lawEffect, value.effects);
			}
		}
		return lawEffect;
	}

	public static LawEffect SumDecreeEffects(RealmDecreeState state, int currentDay, IReadOnlyDictionary<string, DecreeDef> decreeById)
	{
		LawEffect lawEffect = new LawEffect();
		if (state == null || decreeById == null || state.active == null)
		{
			return lawEffect;
		}
		foreach (ActiveDecree item in state.active)
		{
			if (item == null)
			{
				continue;
			}
			if (currentDay < item.endDay && decreeById.TryGetValue(item.decreeId, out var value))
			{
				AddEffects(lawEffect, value.effects);
			}
		}
		return lawEffect;
	}

	private static void AddEffects(LawEffect sum, LawEffect effect)
	{
		if (effect != null)
		{
			sum.taxMultiplier *= effect.taxMultiplier;
			sum.stabilityPerYear += effect.stabilityPerYear;
			sum.legitimacyPerYear += effect.legitimacyPerYear;
			sum.manpowerMultiplier *= effect.manpowerMultiplier;
			sum.unrestPerYear += effect.unrestPerYear;
		}
	}

	private static bool IsLawActive(RealmLawState state, string lawId)
	{
		if (state == null || state.groups == null)
		{
			return false;
		}
		foreach (LawGroupState group in state.groups)
		{
			if (group != null && group.activeLawId == lawId)
			{
				return true;
			}
		}
		return false;
	}

	private static string FindGroupOf(RealmLawState state, string lawId)
	{
		if (state == null || state.groups == null)
		{
			return "";
		}
		foreach (LawGroupState group in state.groups)
		{
			if (group != null && group.activeLawId == lawId)
			{
				return group.groupId;
			}
		}
		return "";
	}

	private static bool IsDecreeActive(RealmDecreeState state, string decreeId, int day)
	{
		if (state?.active == null)
		{
			return false;
		}
		foreach (ActiveDecree item in state.active)
		{
			if (item != null && item.decreeId == decreeId && day < item.endDay)
			{
				return true;
			}
		}
		return false;
	}

	private static int GetCooldownDay(RealmDecreeState state, string decreeId)
	{
		if (state?.cooldowns == null)
		{
			return -1;
		}
		foreach (DecreeCooldown cooldown in state.cooldowns)
		{
			if (cooldown != null && cooldown.decreeId == decreeId)
			{
				return cooldown.readyDay;
			}
		}
		return -1;
	}

	private static void SetCooldown(RealmDecreeState state, string decreeId, int readyDay)
	{
		if (state == null)
		{
			return;
		}
		state.cooldowns ??= new List<DecreeCooldown>();
		state.cooldowns.RemoveAll((DecreeCooldown c) => c != null && c.decreeId == decreeId);
		state.cooldowns.Add(new DecreeCooldown
		{
			decreeId = decreeId,
			readyDay = readyDay
		});
	}
	// ===================== 法律流水线：已知 → 推行中 → 固化 =====================


	public static int EnsureKnownLaws(RealmLawState state, IEnumerable<LawGroupDef> groups)
	{
		if (state == null || groups == null)
		{
			return 0;
		}
		state.knownLawIds ??= new List<string>();
		int num = 0;
		foreach (LawGroupDef group in groups)
		{
			if (group?.laws == null)
			{
				continue;
			}
			foreach (LawDef law in group.laws)
			{
				if (law != null && law.isInitiallyKnown && state.AddKnownLaw(law.lawId))
				{
					num++;
				}
			}
		}
		if (state.groups != null)
		{
			foreach (LawGroupState groupState in state.groups)
			{
				if (groupState != null && !string.IsNullOrEmpty(groupState.activeLawId) && state.AddKnownLaw(groupState.activeLawId))
				{
					num++;
				}
			}
		}
		return num;
	}








	/// <summary>
	/// 每日推进 pending：到期者 SetActiveLaw 并清出 pending（旧法在此之前一直有效）。
	/// 返回本 tick 完成的 PendingLaw 列表（供调用方写编年史/事件）。
	/// </summary>
	public static List<PendingLaw> TickPendingLaws(RealmLawState state, int day)
	{
		List<PendingLaw> list = new List<PendingLaw>();
		if (state?.pendingLaws == null || state.pendingLaws.Count == 0)
		{
			return list;
		}
		for (int i = state.pendingLaws.Count - 1; i >= 0; i--)
		{
			PendingLaw pendingLaw = state.pendingLaws[i];
			if (pendingLaw == null || string.IsNullOrEmpty(pendingLaw.lawId))
			{
				state.pendingLaws.RemoveAt(i);
				continue;
			}
			if (day < pendingLaw.startDay + Math.Max(1, pendingLaw.daysToEnact))
			{
				continue;
			}
			state.SetActiveLaw(pendingLaw.groupId, pendingLaw.lawId, day);
			state.pendingLaws.RemoveAt(i);
			list.Add(pendingLaw);
		}
		list.Reverse();
		return list;
	}




	// ===================== 派系斗争/政变 → catalog 法律同步 =====================




	/// <summary>
	/// 提案授权（纯状态版 / B6）：Laws 只依赖 <see cref="ILawContext"/> 与显式状态（法律状态/组合），不再接触宿主实体。
	/// </summary>
	public static bool CanPropose(ILawContext ctx, RealmLawState state, LawComposition laws, int proposerCharacterId, LawDef law, out string reason)
	{
		reason = "";
		if (ctx == null)
		{
			reason = "政权不存在";
			return false;
		}
		if (law == null)
		{
			reason = "法律不存在";
			return false;
		}
		if (state == null)
		{
			reason = "政权法律状态不存在";
			return false;
		}
		if (string.IsNullOrEmpty(law.lawId) || string.IsNullOrEmpty(law.groupId))
		{
			reason = "法律未归组";
			return false;
		}
		if (!state.IsLawKnown(law.lawId))
		{
			reason = "未知法律：" + (string.IsNullOrEmpty(law.name) ? "?" : law.name);
			return false;
		}
		if (proposerCharacterId < 0)
		{
			reason = "没有提案人";
			return false;
		}
		switch (law.proposalAuthority)
		{
			case ProposalAuthority.HeadOfState:
				if (ctx.GetSupremeRulerId() == proposerCharacterId)
				{
					return true;
				}
				reason = "只有首脑有权提案";
				return false;
			case ProposalAuthority.CentralInstitution:
				if (law.requiredInstitution != CentralInstitution.None && laws != null && (CentralInstitution)laws.centralInstitution.primary != law.requiredInstitution)
				{
					reason = "政权中央机构与法律要求不符";
					return false;
				}
				if (ctx.IsCentralOfficeHolder(proposerCharacterId))
				{
					return true;
				}
				reason = "只有中央机构官员有权提案";
				return false;
			case ProposalAuthority.CharterHolders:
				if (state.IsCharterHolder(proposerCharacterId))
				{
					return true;
				}
				reason = "只有特许权持有者有权提案";
				return false;
			default:
				reason = "未知的提案权配置";
				return false;
		}
	}

}
}
