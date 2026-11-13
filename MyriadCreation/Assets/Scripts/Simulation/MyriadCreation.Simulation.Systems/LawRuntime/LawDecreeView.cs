using System;
using System.Collections.Generic;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Laws;

namespace MyriadCreation.Simulation.LawHost
{
/// <summary>政府面板中的一条法律（纯数据，不含组/类别信息）。</summary>
public sealed class LawViewItem
{
	public LawDef law;

	public string label = "";

	public PendingLaw pending;
}

/// <summary>政府面板中的一条法令（正在生效 / 正在推行）。</summary>
public sealed class DecreeViewItem
{
	public DecreeDef decree;

	public string label = "";

	public int orderCount;
}

/// <summary>
/// 政府面板三段式视图：已固化制度 / 正在生效法令 / 正在推行法令（另附候选按钮数据）。
/// 只输出法律/法令显示名，不暴露 groupId、组名、类别，满足"游戏内不显示域族组类别"。
/// </summary>
public sealed class LawDecreeView
{
	public List<LawViewItem> enactedLaws = new List<LawViewItem>();

	public List<LawViewItem> pendingLaws = new List<LawViewItem>();

	public List<DecreeViewItem> activeDecrees = new List<DecreeViewItem>();

	public List<DecreeViewItem> pendingDecrees = new List<DecreeViewItem>();

	/// <summary>可推行候选（仅：已知 + 有提案权 + 满足前置 + 非现行 + 无同组 pending）。</summary>
	public List<LawViewItem> proposableLaws = new List<LawViewItem>();

	/// <summary>可颁布法令候选（满足资源/前置/冷却）。</summary>
	public List<DecreeDef> enactableDecrees = new List<DecreeDef>();
}

public static class LawDecreeViewBuilder
{
	public static LawDecreeView Build(RealmData realm, IReadOnlyList<LawGroupDef> groups, IReadOnlyList<DecreeDef> decrees, int currentDay, Func<int, string> characterNameResolver = null, IReadOnlyCollection<int> ownedInnovationIds = null)
	{
		LawDecreeView view = new LawDecreeView();
		if (realm == null)
		{
			return view;
		}
		RealmLawState lawState = realm.lawState ??= new RealmLawState();
		RealmDecreeState decreeState = realm.decreeState ??= new RealmDecreeState();
		List<LawGroupDef> list = new List<LawGroupDef>();
		if (groups != null)
		{
			list.AddRange(groups);
		}
		list.Sort((LawGroupDef a, LawGroupDef b) => a.order.CompareTo(b.order));
		Dictionary<string, LawDef> lawById = DefaultLawCatalog.IndexLaws(list);
		Dictionary<string, DecreeDef> decreeById = DefaultLawCatalog.IndexDecrees(decrees);

		// 1) 已固化：每个组当前生效的法律（只显示法律名）
		foreach (LawGroupDef group in list)
		{
			if (group == null || string.IsNullOrEmpty(group.groupId))
			{
				continue;
			}
			string activeLawId = lawState.GetActiveLaw(group.groupId);
			if (!string.IsNullOrEmpty(activeLawId) && lawById.TryGetValue(activeLawId, out LawDef activeLaw))
			{
				view.enactedLaws.Add(new LawViewItem
				{
					law = activeLaw,
					label = "制度：" + LawName(activeLaw)
				});
			}
		}

		// 2) 正在推行：pending 法律（剩余天数 + 发起人）
		if (lawState.pendingLaws != null)
		{
			foreach (PendingLaw pendingLaw in lawState.pendingLaws)
			{
				if (pendingLaw == null || !lawById.TryGetValue(pendingLaw.lawId, out LawDef pendingDef))
				{
					continue;
				}
				string proposerName = ((characterNameResolver != null && pendingLaw.proposerCharacterId >= 0) ? characterNameResolver(pendingLaw.proposerCharacterId) : null);
				string text = "推行中：" + LawName(pendingDef) + "（剩余 " + pendingLaw.RemainingDays(currentDay) + " 天";
				if (!string.IsNullOrEmpty(proposerName))
				{
					text = text + "，发起人：" + proposerName;
				}
				text += "）";
				view.pendingLaws.Add(new LawViewItem
				{
					law = pendingDef,
					label = text,
					pending = pendingLaw
				});
			}
		}

		// 3) 正在生效的法令
		if (decreeState.active != null)
		{
			foreach (ActiveDecree activeDecree in decreeState.active)
			{
				if (activeDecree == null || currentDay >= activeDecree.endDay || !decreeById.TryGetValue(activeDecree.decreeId, out DecreeDef activeDef))
				{
					continue;
				}
				view.activeDecrees.Add(new DecreeViewItem
				{
					decree = activeDef,
					label = "法令：" + DecreeName(activeDef) + "（剩余 " + (activeDecree.endDay - currentDay) + " 天）"
				});
			}
		}

		// 3b) 正在推行的法令：尚未执行完毕的下发命令，按法令聚合
		Dictionary<string, int> orderCounts = new Dictionary<string, int>();
		if (decreeState.orders != null)
		{
			foreach (DecreeOrder order in decreeState.orders)
			{
				if (order == null || string.IsNullOrEmpty(order.decreeId) || !IsOrderInProgress(order.state))
				{
					continue;
				}
				orderCounts.TryGetValue(order.decreeId, out int count);
				orderCounts[order.decreeId] = count + 1;
			}
		}
		foreach (KeyValuePair<string, int> orderCount in orderCounts)
		{
			if (decreeById.TryGetValue(orderCount.Key, out DecreeDef pendingDef))
			{
				view.pendingDecrees.Add(new DecreeViewItem
				{
					decree = pendingDef,
					label = "法令推行中：" + DecreeName(pendingDef) + "（" + orderCount.Value + " 道命令）",
					orderCount = orderCount.Value
				});
			}
		}

		// 4) 可推行候选（按钮用）：只列已知+有权+满足前置的非现行/非同组 pending 法律
		int proposerCharacterId = realm.GetSupremeRulerId();
		foreach (LawGroupDef group in list)
		{
			if (group?.laws == null)
			{
				continue;
			}
			foreach (LawDef law in group.laws)
			{
				if (law == null || !lawState.IsLawKnown(law.lawId))
				{
					continue;
				}
				if (lawState.GetActiveLaw(law.groupId) == law.lawId || lawState.HasPendingLaw(law.groupId))
				{
					continue;
				}
				if (!LawRealmHost.CanPropose(realm, proposerCharacterId, law, out _))
				{
					continue;
				}
				if (!LawAndDecreeSystem.CanEnactLaw(law, lawState, realm.treasury, realm.prestige, lawState.legitimacy, realm.centralization, ownedInnovationIds, out _))
				{
					continue;
				}
				view.proposableLaws.Add(new LawViewItem
				{
					law = law,
					label = LawName(law)
				});
			}
		}

		// 5) 可颁布法令候选（按钮用）
		if (decrees != null)
		{
			foreach (DecreeDef decree in decrees)
			{
				if (decree == null)
				{
					continue;
				}
				if (LawAndDecreeSystem.CanEnactDecree(decree, lawState, decreeState, realm.treasury, lawState.authority, lawState.legitimacy, currentDay, ownedInnovationIds, out _))
				{
					view.enactableDecrees.Add(decree);
				}
			}
		}
		return view;
	}

	private static bool IsOrderInProgress(DecreeOrderState state)
	{
		return state != DecreeOrderState.Enforced && state != DecreeOrderState.Refused && state != DecreeOrderState.Profiteering && state != DecreeOrderState.Failed;
	}

	private static string LawName(LawDef law)
	{
		return string.IsNullOrEmpty(law?.name) ? "未知法律" : law.name;
	}

	private static string DecreeName(DecreeDef decree)
	{
		return string.IsNullOrEmpty(decree?.name) ? "未知法令" : decree.name;
	}
}
}
