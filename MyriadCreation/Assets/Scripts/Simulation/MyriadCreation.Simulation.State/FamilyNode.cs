using System;
using System.Collections.Generic;
using MyriadCreation.Content.Modding;
using MyriadCreation.Simulation.ContentHost;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MyriadCreation.Simulation.State
{
public class FamilyNode
{
	public int familyId;

	public string familyName;

	public int founderCharacterId;

	public int foundingYear;

	public List<int> memberIds = new List<int>();

	public List<FamilyNode> branches = new List<FamilyNode>();

	[NonSerialized]
	public FamilyNode parentFamily;

	public float familyPrestige;

	public float familyWealth;

	[NonSerialized]
	public Dictionary<string, float> familyTraditions = new Dictionary<string, float>();

	public int holderRealmId = -1;

	public int homelandCountryId = -1;

	public List<string> generationMarks = new List<string>();

	[NonSerialized]
	public InnovationTree Innovations;

	public string coaPattern;

	public string coaColors;

	public void AddMember(int characterId)
	{
		if (!memberIds.Contains(characterId))
		{
			memberIds.Add(characterId);
		}
	}

	public FamilyNode CreateBranch(string branchName, int founderId, int year)
	{
		FamilyNode familyNode = new FamilyNode
		{
			familyId = Random.Range(10000, 99999),
			familyName = branchName,
			founderCharacterId = founderId,
			foundingYear = year,
			parentFamily = this
		};
		branches.Add(familyNode);
		return familyNode;
	}

	public List<int> GetAllMembers()
	{
		List<int> list = new List<int>(memberIds);
		foreach (FamilyNode branch in branches)
		{
			list.AddRange(branch.GetAllMembers());
		}
		return list;
	}

	public int GetTotalMemberCount()
	{
		int num = memberIds.Count;
		foreach (FamilyNode branch in branches)
		{
			num += branch.GetTotalMemberCount();
		}
		return num;
	}

	public int GetGenerationDepth()
	{
		if (branches.Count == 0)
		{
			return 1;
		}
		int num = 0;
		foreach (FamilyNode branch in branches)
		{
			num = Mathf.Max(num, branch.GetGenerationDepth());
		}
		return num + 1;
	}

	public FamilyNode FindCharacterFamily(int characterId)
	{
		if (memberIds.Contains(characterId))
		{
			return this;
		}
		foreach (FamilyNode branch in branches)
		{
			FamilyNode familyNode = branch.FindCharacterFamily(characterId);
			if (familyNode != null)
			{
				return familyNode;
			}
		}
		return null;
	}

	public float CalculateTotalPrestige()
	{
		float num = familyPrestige;
		foreach (FamilyNode branch in branches)
		{
			num += branch.CalculateTotalPrestige() * 0.5f;
		}
		return num;
	}

	public bool AddFamilyTradition(string traditionId)
	{
		if (string.IsNullOrEmpty(traditionId) || familyTraditions.ContainsKey(traditionId))
		{
			return false;
		}
		if (!ContentResolvers.FamilyTraditions.TryGet(traditionId, out var value))
		{
			Debug.LogWarning((object)("[Family] 家族传统 " + traditionId + " 未在注册表定义，拒绝添加"));
			return false;
		}
		if (value.incompatibleWith != null)
		{
			foreach (string key in familyTraditions.Keys)
			{
				if (value.incompatibleWith.Contains(key))
				{
					Debug.Log((object)("[Family] 家族传统 " + traditionId + " 与既有传统 " + key + " 互斥，拒绝添加"));
					return false;
				}
			}
		}
		if (Innovations != null && holderRealmId >= 0 && value.requiredInnovations != null && value.requiredInnovations.Count > 0)
		{
			foreach (int requiredInnovation in value.requiredInnovations)
			{
				if (!Innovations.HasInnovation(holderRealmId, requiredInnovation))
				{
					Debug.Log((object)$"[Family] 家族传统 {traditionId} 需要革新 {requiredInnovation} 解锁，家族所在政权尚未持有，拒绝添加");
					return false;
				}
			}
		}
		familyTraditions[traditionId] = 1f;
		return true;
	}

	public bool RemoveFamilyTradition(string traditionId)
	{
		return familyTraditions.Remove(traditionId);
	}

	public float GetTraditionEffect(string key)
	{
		float num = 0f;
		foreach (KeyValuePair<string, float> familyTradition in familyTraditions)
		{
			if (!ContentResolvers.FamilyTraditions.TryGet(familyTradition.Key, out var value) || value.effects == null)
			{
				continue;
			}
			foreach (EffectEntry effect in value.effects)
			{
				if (effect.key == key)
				{
					num += effect.value * familyTradition.Value;
				}
			}
		}
		return num;
	}
}
}
