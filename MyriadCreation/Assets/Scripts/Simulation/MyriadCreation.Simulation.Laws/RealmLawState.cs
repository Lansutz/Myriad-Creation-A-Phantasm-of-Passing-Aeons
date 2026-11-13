using System;
using System.Collections.Generic;

namespace MyriadCreation.Simulation.Laws
{
[Serializable]
public class RealmLawState
{
	public List<LawGroupState> groups = new List<LawGroupState>();

	public float legitimacy = 50f;

	public float authority = 50f;

	// ===== 已知 / 推行中 / 特许权（随 RealmDTO.lawState 自动入档，旧档缺省为空表）=====
	/// <summary>政权已知道的法律 id（只有已知法可被提案/推行）。</summary>
	public List<string> knownLawIds = new List<string>();

	/// <summary>已根本化法律 id；普通法律/法令不能覆盖。</summary>
	public List<string> fundamentalizedLawIds = new List<string>();

	/// <summary>正在推行的法律（同组唯一；到期前旧法继续有效）。</summary>
	public List<PendingLaw> pendingLaws = new List<PendingLaw>();

	/// <summary>持有特许权的角色 id（可推行 ProposalAuthority.CharterHolders 的法律）。</summary>
	public List<int> charterHolderIds = new List<int>();

	public string GetActiveLaw(string groupId)
	{
		if (string.IsNullOrEmpty(groupId))
		{
			return null;
		}
		if (groups != null)
		{
			foreach (LawGroupState group in groups)
			{
				if (group != null && group.groupId == groupId)
				{
					return group.activeLawId;
				}
			}
		}
		return null;
	}

	public void SetActiveLaw(string groupId, string lawId, int day)
	{
		if (string.IsNullOrEmpty(groupId))
		{
			return;
		}
		groups ??= new List<LawGroupState>();
		LawGroupState lawGroupState = groups.Find((LawGroupState g) => g != null && g.groupId == groupId);
		if (lawGroupState == null)
		{
			lawGroupState = new LawGroupState
			{
				groupId = groupId
			};
			groups.Add(lawGroupState);
		}
		if (!string.IsNullOrEmpty(lawGroupState.activeLawId) && lawGroupState.activeLawId != lawId)
		{
			lawGroupState.repealedLawIds ??= new List<string>();
			if (!lawGroupState.repealedLawIds.Contains(lawGroupState.activeLawId))
			{
				lawGroupState.repealedLawIds.Add(lawGroupState.activeLawId);
			}
		}
		lawGroupState.activeLawId = lawId;
		lawGroupState.enactedDay = day;
		lawGroupState.yearsActive = 0;
		lawGroupState.stage = LegalNormStage.Active;
	}

	/// <summary>法律是否在政权已知集中。</summary>
	public bool IsLawKnown(string lawId)
	{
		if (string.IsNullOrEmpty(lawId) || knownLawIds == null)
		{
			return false;
		}
		return knownLawIds.Contains(lawId);
	}

	/// <summary>登记一条已知法律（重复登记返回 false）。</summary>
	public bool AddKnownLaw(string lawId)
	{
		if (string.IsNullOrEmpty(lawId))
		{
			return false;
		}
		knownLawIds ??= new List<string>();
		if (knownLawIds.Contains(lawId))
		{
			return false;
		}
		knownLawIds.Add(lawId);
		return true;
	}

	/// <summary>取某组的推行中法律（没有则 null）。同组唯一由 BeginEnactLaw 保证。</summary>
	public PendingLaw GetPendingLaw(string groupId)
	{
		if (string.IsNullOrEmpty(groupId) || pendingLaws == null)
		{
			return null;
		}
		return pendingLaws.Find((PendingLaw p) => p != null && p.groupId == groupId);
	}

	public bool HasPendingLaw(string groupId)
	{
		return GetPendingLaw(groupId) != null;
	}

	public bool IsCharterHolder(int characterId)
	{
		return charterHolderIds != null && charterHolderIds.Contains(characterId);
	}

	/// <summary>授予特许权（重复授予返回 false）。</summary>
	public bool AddCharterHolder(int characterId)
	{
		if (characterId < 0)
		{
			return false;
		}
		charterHolderIds ??= new List<int>();
		if (charterHolderIds.Contains(characterId))
		{
			return false;
		}
		charterHolderIds.Add(characterId);
		return true;
	}

	/// <summary>收回特许权（不存在返回 false）。</summary>
	public bool RemoveCharterHolder(int characterId)
	{
		return charterHolderIds != null && charterHolderIds.Remove(characterId);
	}
}
}
