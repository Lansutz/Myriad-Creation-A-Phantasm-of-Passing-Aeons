using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Simulation.WorldState;
using UnityEngine;
using MyriadCreation.Simulation.Culture;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Content.Modding;
using Object = UnityEngine.Object;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public class InnovationTree
{
	private readonly Dictionary<int, InnovationDef> _innovations = new Dictionary<int, InnovationDef>();

	private readonly Dictionary<int, HashSet<int>> _realmInnovations = new Dictionary<int, HashSet<int>>();

	private readonly Dictionary<int, float> _realmResearchPoints = new Dictionary<int, float>();

	private readonly Dictionary<int, int> _realmCurrentResearch = new Dictionary<int, int>();

	private readonly Dictionary<int, HashSet<int>> _realmDiscovered = new Dictionary<int, HashSet<int>>();

	private readonly Dictionary<int, InnovationProgress> _innovationProgress = new Dictionary<int, InnovationProgress>();

	private readonly Dictionary<string, float> _resourceCumulativeOutput = new Dictionary<string, float>();

	public event Action<int, int> OnInnovationCompleted;

	public event Action<int, int> OnInnovationDiscovered;

	public InnovationTree()
	{
		LoadFromRegistry();
	}

	private void LoadFromRegistry()
	{
		if (!ContentRegistry.IsInitialized)
		{
			throw new InvalidOperationException("Content runtime must be initialized before loading innovation definitions.");
		}
		_innovations.Clear();
		foreach (InnovationDef item in ContentResolvers.Innovations.All)
		{
			_innovations[item.innovationId] = item;
		}
		if (_innovations.Count == 0)
		{
			Debug.LogWarning((object)"[InnovationTree] 革新定义为空（Innovation/Innovations.json 缺失或未加载）");
		}
	}

	public void RegisterInnovation(InnovationDef def)
	{
		if (def != null && def.innovationId > 0)
		{
			_innovations[def.innovationId] = def;
		}
	}

	public HashSet<string> GetRealmCapabilities(int realmId)
	{
		HashSet<string> hashSet = new HashSet<string>();
		if (!_realmInnovations.TryGetValue(realmId, out var value))
		{
			return hashSet;
		}
		foreach (int item in value)
		{
			if (!_innovations.TryGetValue(item, out var value2) || value2.providesCapabilities == null)
			{
				continue;
			}
			foreach (string providesCapability in value2.providesCapabilities)
			{
				if (!string.IsNullOrEmpty(providesCapability))
				{
					hashSet.Add(providesCapability);
				}
			}
		}
		return hashSet;
	}

	public bool HasCapabilities(int realmId, InnovationDef def)
	{
		if (def?.requiredCapabilities == null || def.requiredCapabilities.Count == 0)
		{
			return true;
		}
		HashSet<string> realmCapabilities = GetRealmCapabilities(realmId);
		foreach (string requiredCapability in def.requiredCapabilities)
		{
			if (string.IsNullOrEmpty(requiredCapability) || !realmCapabilities.Contains(requiredCapability))
			{
				return false;
			}
		}
		return true;
	}

	public float GetCapabilityValue(int realmId, string channel)
	{
		if (string.IsNullOrEmpty(channel))
		{
			return 0f;
		}
		float num = 0f;
		float num2 = 1f;
		if (_realmInnovations.TryGetValue(realmId, out var value))
		{
			foreach (int item in value)
			{
				if (!_innovations.TryGetValue(item, out var value2) || value2.capabilityModifiers == null)
				{
					continue;
				}
				foreach (CapabilityModifier capabilityModifier in value2.capabilityModifiers)
				{
					if (!(capabilityModifier.channel != channel))
					{
						num += capabilityModifier.add;
						if (capabilityModifier.multiply > 0f)
						{
							num2 *= capabilityModifier.multiply;
						}
					}
				}
			}
		}
		return (CapabilityChannels.GetBaseValue(channel) + num) * num2;
	}

	public bool HasCapabilityValues(int realmId, InnovationDef def)
	{
		if (def == null)
		{
			return true;
		}
		if (def.requiredCapabilityValues != null)
		{
			foreach (CapabilityRequirement requiredCapabilityValue in def.requiredCapabilityValues)
			{
				if (!string.IsNullOrEmpty(requiredCapabilityValue.channel) && GetCapabilityValue(realmId, requiredCapabilityValue.channel) < requiredCapabilityValue.minValue)
				{
					return false;
				}
			}
		}
		if (def.softPrerequisites != null)
		{
			foreach (InnovationSoftPrerequisite soft in def.softPrerequisites)
			{
				if (soft == null || soft.goodsId < 0 || string.IsNullOrEmpty(soft.propertyId) || string.IsNullOrEmpty(soft.capabilityChannel))
				{
					continue;
				}
				if (!GoodsPropertyCatalog.TryGetValue(soft.goodsId, soft.propertyId, out float value))
				{
					continue;
				}
				float required = value * Mathf.Max(0f, soft.propertyFactor);
				if (GetCapabilityValue(realmId, soft.capabilityChannel) < required)
				{
					return false;
				}
			}
		}
		return true;
	}

	public bool StartResearch(int realmId, int innovationId, GameWorld world = null)
	{
		if (!_innovations.TryGetValue(innovationId, out var def))
		{
			return false;
		}
		if (HasInnovation(realmId, innovationId))
		{
			return false;
		}
		if (!ArePrerequisitesMet(world, realmId, def))
		{
			return false;
		}
		_realmCurrentResearch[realmId] = innovationId;
		if (!_realmResearchPoints.ContainsKey(realmId))
		{
			_realmResearchPoints[realmId] = 0f;
		}
		return true;
	}

	private static bool HasPrerequisiteContent(InnovationPrerequisiteNode node)
	{
		if (node == null)
		{
			return false;
		}
		if (node.IsLeaf)
		{
			return node.innovationId > 0;
		}
		if (node.children != null)
		{
			foreach (InnovationPrerequisiteNode child in node.children)
			{
				if (HasPrerequisiteContent(child))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool EvaluatePrerequisiteNode(int realmId, InnovationPrerequisiteNode node)
	{
		if (node == null)
		{
			return true;
		}
		if (node.IsLeaf)
		{
			return node.innovationId > 0 && HasInnovation(realmId, node.innovationId);
		}
		if (node.children == null || node.children.Count == 0)
		{
			return false;
		}
		if (node.kind == InnovationPrerequisiteKind.All)
		{
			foreach (InnovationPrerequisiteNode child in node.children)
			{
				if (!EvaluatePrerequisiteNode(realmId, child))
				{
					return false;
				}
			}
			return true;
		}
		if (node.kind == InnovationPrerequisiteKind.Any)
		{
			foreach (InnovationPrerequisiteNode child in node.children)
			{
				if (EvaluatePrerequisiteNode(realmId, child))
				{
					return true;
				}
			}
			return false;
		}
		return false;
	}

	public bool AreItemInnovationsMet(int realmId, EquipmentDef equipment)
	{
		if (equipment == null) return false;
		if (!IsUnlockGranted(realmId, InnovationUnlockKind.Equipment, equipment.equipmentId))
		{
			return false;
		}
		if (equipment.requiredInnovation > 0 && !HasInnovation(realmId, equipment.requiredInnovation))
		{
			return false;
		}
		if (equipment.requiredInnovationTree != null && HasPrerequisiteContent(equipment.requiredInnovationTree))
		{
			return EvaluatePrerequisiteNode(realmId, equipment.requiredInnovationTree);
		}
		return true;
	}

	public bool ArePrerequisitesMet(GameWorld world, int realmId, InnovationDef def)
	{
		if (def.prerequisiteTree != null && HasPrerequisiteContent(def.prerequisiteTree))
		{
			if (!EvaluatePrerequisiteNode(realmId, def.prerequisiteTree))
			{
				return false;
			}
		}
		if (!HasCapabilities(realmId, def))
		{
			return false;
		}
		if (!HasCapabilityValues(realmId, def))
		{
			return false;
		}
		if ((Object)(object)world == (Object)null)
		{
			return true;
		}
		if (def.requiredAllResources != null)
		{
			foreach (int requiredAllResource in def.requiredAllResources)
			{
				if (!HasResource(world, realmId, requiredAllResource, def.allowTradeResource, def.requiredResourceAmount))
				{
					return false;
				}
			}
		}
		if (def.requiredAnyResources != null && def.requiredAnyResources.Count > 0)
		{
			bool flag = false;
			foreach (int requiredAnyResource in def.requiredAnyResources)
			{
				if (HasResource(world, realmId, requiredAnyResource, def.allowTradeResource, def.requiredResourceAmount))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
		}
		return true;
	}

	public void DailyTick(int realmId, float researchRate)
	{
		if (!_realmCurrentResearch.TryGetValue(realmId, out var value) || !_innovations.TryGetValue(value, out var value2))
		{
			return;
		}
		if (!_realmResearchPoints.ContainsKey(realmId))
		{
			_realmResearchPoints[realmId] = 0f;
		}
		_realmResearchPoints[realmId] += researchRate;
		if (_realmResearchPoints[realmId] >= value2.researchCost)
		{
			CompleteResearch(realmId, value);
		}
	}

	private void CompleteResearch(int realmId, int innovationId)
	{
		if (!_realmInnovations.ContainsKey(realmId))
		{
			_realmInnovations[realmId] = new HashSet<int>();
		}
		_realmInnovations[realmId].Add(innovationId);
		_realmResearchPoints[realmId] = 0f;
		_realmCurrentResearch.Remove(realmId);
		if (_innovations.TryGetValue(innovationId, out var value))
		{
			Debug.Log((object)$"[Innovation] 政权 {realmId} 完成研究：{value.GetName()}（{value.Domain}/{value.field}）");
		}
		OnInnovationCompleted?.Invoke(realmId, innovationId);
	}

	public bool IsDiscovered(int realmId, int innovationId)
	{
		return _realmDiscovered.TryGetValue(realmId, out var value) && value.Contains(innovationId);
	}

	public bool Discover(int realmId, int innovationId)
	{
		if (!_innovations.ContainsKey(innovationId))
		{
			return false;
		}
		if (!_realmDiscovered.TryGetValue(realmId, out var value))
		{
			_realmDiscovered[realmId] = value = new HashSet<int>();
		}
		if (!value.Add(innovationId))
		{
			return false;
		}
		OnInnovationDiscovered?.Invoke(realmId, innovationId);
		return true;
	}

	public List<InnovationDef> GetDiscoveredInnovations(int realmId)
	{
		List<InnovationDef> list = new List<InnovationDef>();
		if (!_realmDiscovered.TryGetValue(realmId, out var value))
		{
			return list;
		}
		foreach (int item in value)
		{
			if (_realmInnovations.TryGetValue(realmId, out var value2) && value2.Contains(item))
			{
				continue;
			}
			if (_innovations.TryGetValue(item, out var value3))
			{
				list.Add(value3);
			}
		}
		list.Sort((a, b) => a.innovationId.CompareTo(b.innovationId));
		return list;
	}

	public int GetDiscoveredCount(int realmId)
	{
		return _realmDiscovered.TryGetValue(realmId, out var value) ? value.Count : 0;
	}

	/// <summary>是否存在任何革新把该内容列为解锁目标；未引用的内容不设革新门槛。</summary>
	public bool IsUnlockReferenced(InnovationUnlockKind kind, int id)
	{
		foreach (InnovationDef def in _innovations.Values)
		{
			if (def?.unlockRefs == null)
			{
				continue;
			}
			foreach (InnovationUnlock unlock in def.unlockRefs)
			{
				if (unlock != null && unlock.kind == kind && unlock.id == id)
				{
					return true;
				}
			}
		}
		return false;
	}

	/// <summary>内容是否已由本政权已完成的革新解锁；未被任何革新引用时视为可用。</summary>
	public bool IsUnlockGranted(int realmId, InnovationUnlockKind kind, int id)
	{
		if (!IsUnlockReferenced(kind, id))
		{
			return true;
		}
		if (!_realmInnovations.TryGetValue(realmId, out HashSet<int> owned))
		{
			return false;
		}
		foreach (int innovationId in owned)
		{
			if (!_innovations.TryGetValue(innovationId, out InnovationDef def) || def?.unlockRefs == null)
			{
				continue;
			}
			foreach (InnovationUnlock unlock in def.unlockRefs)
			{
				if (unlock != null && unlock.kind == kind && unlock.id == id)
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool IsUnlockReferenced(InnovationUnlockKind kind, string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			return false;
		}
		foreach (InnovationDef def in _innovations.Values)
		{
			if (def?.unlockRefs == null)
			{
				continue;
			}
			foreach (InnovationUnlock unlock in def.unlockRefs)
			{
				if (unlock != null && unlock.kind == kind && unlock.key == key)
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool IsUnlockGranted(int realmId, InnovationUnlockKind kind, string key)
	{
		if (!IsUnlockReferenced(kind, key))
		{
			return true;
		}
		if (!_realmInnovations.TryGetValue(realmId, out HashSet<int> owned))
		{
			return false;
		}
		foreach (int innovationId in owned)
		{
			if (!_innovations.TryGetValue(innovationId, out InnovationDef def) || def?.unlockRefs == null)
			{
				continue;
			}
			foreach (InnovationUnlock unlock in def.unlockRefs)
			{
				if (unlock != null && unlock.kind == kind && unlock.key == key)
				{
					return true;
				}
			}
		}
		return false;
	}

	/// <summary>写入已完成革新（事件/测试/调试用），并触发完成事件。</summary>
	public bool GrantInnovation(int realmId, int innovationId)
	{
		if (!_innovations.ContainsKey(innovationId))
		{
			return false;
		}
		if (!_realmInnovations.TryGetValue(realmId, out HashSet<int> set))
		{
			_realmInnovations[realmId] = set = new HashSet<int>();
		}
		if (!set.Add(innovationId))
		{
			return false;
		}
		OnInnovationCompleted?.Invoke(realmId, innovationId);
		return true;
	}

	public bool HasInnovation(int realmId, int innovationId)
	{
		return _realmInnovations.TryGetValue(realmId, out var value) && value.Contains(innovationId);
	}

	public float GetResearchProgress(int realmId)
	{
		if (!_realmCurrentResearch.TryGetValue(realmId, out var value) || !_innovations.TryGetValue(value, out var value2))
		{
			return 0f;
		}
		if (!_realmResearchPoints.TryGetValue(realmId, out var value3))
		{
			return 0f;
		}
		return value3 / value2.researchCost;
	}

	public InnovationDef GetCurrentResearch(int realmId)
	{
		if (_realmCurrentResearch.TryGetValue(realmId, out var value) && _innovations.TryGetValue(value, out var value2))
		{
			return value2;
		}
		return null;
	}

	public bool CancelResearch(int realmId)
	{
		if (!_realmCurrentResearch.Remove(realmId))
		{
			return false;
		}
		_realmResearchPoints[realmId] = 0f;
		return true;
	}

	public (List<int> and, List<int> or) GetDirectPrerequisites(int innovationId)
	{
		if (!_innovations.TryGetValue(innovationId, out var value))
		{
			return (and: new List<int>(), or: new List<int>());
		}
		return (and: value.GetPrerequisiteIds(), or: new List<int>());
	}

	public List<InnovationDef> GetAvailableInnovations(int realmId, GameWorld world = null)
	{
		List<InnovationDef> list = new List<InnovationDef>();
		foreach (InnovationDef value in _innovations.Values)
		{
			if (!HasInnovation(realmId, value.innovationId) && ArePrerequisitesMet(world, realmId, value))
			{
				list.Add(value);
			}
		}
		return list;
	}

	public float GetLearningDifficulty(int realmId, int innovationId)
	{
		if (!_innovations.TryGetValue(innovationId, out var value))
		{
			return 0f;
		}
		List<int> prerequisiteIds = value.GetPrerequisiteIds();
		int num = prerequisiteIds.Count;
		int num2 = 0;
		foreach (int prerequisite in prerequisiteIds)
		{
			if (HasInnovation(realmId, prerequisite))
			{
				num2++;
			}
		}
		if (num == 0)
		{
			return 1f;
		}
		float num3 = (float)num2 / (float)num;
		return 0.4f + 0.6f * num3;
	}

	public float GetEffectiveResearchRate(int realmId, int innovationId, float baseRate, CultureData culture)
	{
		float num = baseRate * GetLearningDifficulty(realmId, innovationId);
		if (_innovations.TryGetValue(innovationId, out var value))
		{
		}
		if (culture != null && _innovations.TryGetValue(innovationId, out var value2))
		{
			bool flag = culture.HasInnovationAffinity(value2.field.ToString());
			if (!flag && value2.affinityTags != null)
			{
				foreach (string affinityTag in value2.affinityTags)
				{
					if (culture.HasInnovationAffinity(affinityTag))
					{
						flag = true;
						break;
					}
				}
			}
			if (flag)
			{
				num *= 1.25f;
			}
		}
		return num;
	}

	public List<InnovationDef> GetInnovationsByDomain(InnovationDomain domain)
	{
		List<InnovationDef> list = new List<InnovationDef>();
		foreach (InnovationDef value in _innovations.Values)
		{
			if (value.Domain == domain)
			{
				list.Add(value);
			}
		}
		return list;
	}

	public List<InnovationDef> GetInnovationsByField(InnovationField field)
	{
		List<InnovationDef> list = new List<InnovationDef>();
		foreach (InnovationDef value in _innovations.Values)
		{
			if (value.field == field)
			{
				list.Add(value);
			}
		}
		return list;
	}

	public InnovationDef GetInnovation(int id)
	{
		if (!_innovations.TryGetValue(id, out var value))
		{
			return null;
		}
		return value;
	}

	public IReadOnlyDictionary<int, InnovationDef> GetAllInnovations()
	{
		return _innovations;
	}

	public HashSet<int> GetRealmInnovations(int realmId)
	{
		if (!_realmInnovations.TryGetValue(realmId, out var value))
		{
			return new HashSet<int>();
		}
		return value;
	}

	public int GetRealmInnovationCount(int realmId)
	{
		if (!_realmInnovations.TryGetValue(realmId, out var value))
		{
			return 0;
		}
		return value.Count;
	}

	public InnovationProgress GetProgress(int innovationId)
	{
		if (!_innovationProgress.TryGetValue(innovationId, out var value))
		{
			value = new InnovationProgress
			{
				innovationId = innovationId
			};
			_innovationProgress[innovationId] = value;
		}
		return value;
	}

	public bool HasResource(GameWorld world, int realmId, int goodsId, bool allowTrade, float minAmount = 0f)
	{
		if ((Object)(object)world == (Object)null || world.tiles == null)
		{
			return false;
		}
		bool flag = false;
		for (int i = 0; i < world.tiles.Length; i++)
		{
			ref TileData reference = ref world.tiles[i];
			if (!reference.exists || reference.ownerRealmId != realmId || reference.resources == null)
			{
				continue;
			}
			foreach (TileResource resource in reference.resources)
			{
				if (resource.goodsId == goodsId && (resource.discovered || resource.developed))
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				break;
			}
		}
		if (!flag)
		{
			return false;
		}
		if (minAmount > 0f)
		{
			string key = realmId + "_" + goodsId;
			if (_resourceCumulativeOutput.TryGetValue(key, out var value) && value >= minAmount)
			{
				return true;
			}
			return false;
		}
		return true;
	}

	private void AccumulateResourceOutput(int realmId, int goodsId, float monthlyOutput)
	{
		if (!(monthlyOutput <= 0f))
		{
			string key = realmId + "_" + goodsId;
			_resourceCumulativeOutput.TryGetValue(key, out var value);
			_resourceCumulativeOutput[key] = value + monthlyOutput;
		}
	}

	public float GetResourceCumulativeOutput(int realmId, int goodsId)
	{
		string key = realmId + "_" + goodsId;
		if (!_resourceCumulativeOutput.TryGetValue(key, out var value))
		{
			return 0f;
		}
		return value;
	}

	public float GetResourceMonthlyOutput(GameWorld world, int realmId, int goodsId)
	{
		if ((Object)(object)world == (Object)null || world.tiles == null)
		{
			return 0f;
		}
		float num = 0f;
		for (int i = 0; i < world.tiles.Length; i++)
		{
			ref TileData reference = ref world.tiles[i];
			if (!reference.exists || reference.ownerRealmId != realmId || reference.resources == null)
			{
				continue;
			}
			foreach (TileResource resource in reference.resources)
			{
				if (resource.goodsId == goodsId && resource.developed)
				{
					num += resource.EffectiveOutput * 10f;
				}
			}
		}
		return num;
	}

	public void MonthlyTickProgress(GameWorld world, int realmId, int innovationId, float monthlyOutput, float averageQuality, int researcherCount, bool hasFacility)
	{
		if (!_innovations.TryGetValue(innovationId, out var value) || HasInnovation(realmId, innovationId))
		{
			return;
		}
		InnovationProgress progress = GetProgress(innovationId);
		progress.isAvailable = ArePrerequisitesMet(world, realmId, value);
		if (!progress.isAvailable)
		{
			progress.lockedReason = GetLockedReason(world, realmId, value);
			progress.monthlyGain = 0f;
			return;
		}
		progress.gainBreakdown.Clear();
		progress.cumulativeOutput += monthlyOutput;
		progress.averageQuality = averageQuality;
		if (monthlyOutput > 0f)
		{
			progress.oldMethodPracticeCount++;
		}
		if (value.requiredAnyResources != null)
		{
			foreach (int requiredAnyResource in value.requiredAnyResources)
			{
				AccumulateResourceOutput(realmId, requiredAnyResource, GetResourceMonthlyOutput(world, realmId, requiredAnyResource));
			}
		}
		if (value.requiredAllResources != null)
		{
			foreach (int requiredAllResource in value.requiredAllResources)
			{
				AccumulateResourceOutput(realmId, requiredAllResource, GetResourceMonthlyOutput(world, realmId, requiredAllResource));
			}
		}
		float num = averageQuality * 50f;
		float num2 = Mathf.Min(progress.cumulativeOutput, num);
		float num3 = 0f;
		if (num2 > 0f)
		{
			float num4 = 1f + (averageQuality - 5f) * 0.1f;
			num3 = Mathf.Log(1f + num2) * 0.4f * Mathf.Max(0.1f, num4);
			progress.gainBreakdown["生产实践"] = num3;
		}
		if (progress.cumulativeOutput > num)
		{
			progress.gainBreakdown["产量已达品质上限(需提升品质)"] = 0f;
		}
		float num5 = (float)researcherCount * 0.15f;
		if (progress.oldMethodPracticeCount >= 30)
		{
			num5 *= 2f;
			progress.gainBreakdown["角色灵感加成"] = num5 * 0.5f;
		}
		if (researcherCount > 0)
		{
			progress.gainBreakdown["角色研究"] = num5;
		}
		float num6 = 0f;
		if (hasFacility)
		{
			num6 = 0.2f;
			progress.gainBreakdown["设施支持"] = num6;
		}
		float num7 = 0.1f;
		progress.gainBreakdown["基础观察"] = num7;
		float num8 = num7 + num3 + num5 + num6;
		float num9 = 1f / (1f + progress.progress / 60f);
		progress.gainBreakdown["边际递减系数"] = num9;
		float num10 = num8 * num9;
		progress.progress += (progress.monthlyGain = Mathf.Min(num10, 2f));
		progress.lockedReason = "";
		if (progress.progress >= 100f)
		{
			CompleteResearch(realmId, innovationId);
			progress.progress = 100f;
		}
	}

	private float GetRelevantResourceOutput(GameWorld world, int realmId, InnovationDef def)
	{
		float num = 0f;
		if (def.requiredAnyResources != null && def.requiredAnyResources.Count > 0)
		{
			foreach (int requiredAnyResource in def.requiredAnyResources)
			{
				num = Mathf.Max(num, GetResourceMonthlyOutput(world, realmId, requiredAnyResource));
			}
		}
		if (def.requiredAllResources != null && def.requiredAllResources.Count > 0)
		{
			float num2 = float.MaxValue;
			foreach (int requiredAllResource in def.requiredAllResources)
			{
				num2 = Mathf.Min(num2, GetResourceMonthlyOutput(world, realmId, requiredAllResource));
			}
			if (num2 < float.MaxValue)
			{
				num = num2;
			}
		}
		return num;
	}

	private string GetLockedReason(GameWorld world, int realmId, InnovationDef def)
	{
		foreach (int prerequisite in def.GetPrerequisiteIds())
		{
			if (!HasInnovation(realmId, prerequisite))
			{
				InnovationDef innovation = GetInnovation(prerequisite);
				return "需要前置革新：" + ((innovation != null) ? innovation.GetName() : prerequisite.ToString());
			}
		}
		if (def.requiredAllResources != null)
		{
			foreach (int requiredAllResource in def.requiredAllResources)
			{
				if (!HasResource(world, realmId, requiredAllResource, def.allowTradeResource, def.requiredResourceAmount))
				{
					string text = (((Object)(object)world != (Object)null && world.goodsDefs != null && world.goodsDefs.TryGetValue(requiredAllResource, out var value)) ? value.goodsName : requiredAllResource.ToString());
					return "缺少物产：" + text;
				}
			}
		}
		if (def.requiredAnyResources != null && def.requiredAnyResources.Count > 0)
		{
			bool flag = false;
			foreach (int requiredAnyResource in def.requiredAnyResources)
			{
				if (HasResource(world, realmId, requiredAnyResource, def.allowTradeResource, def.requiredResourceAmount))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				List<string> list = new List<string>();
				foreach (int requiredAnyResource2 in def.requiredAnyResources)
				{
					list.Add(((Object)(object)world != (Object)null && world.goodsDefs != null && world.goodsDefs.TryGetValue(requiredAnyResource2, out var value2)) ? value2.goodsName : requiredAnyResource2.ToString());
				}
				return "需要以下物产之一：" + string.Join("/", list);
			}
		}
		return "条件未满足";
	}

	// ===== 存档（SaveVersion 14）：只存运行时状态，革新定义读档后由构造函数重载 =====

	public InnovationTreeSaveData CaptureState()
	{
		var data = new InnovationTreeSaveData();

		var realmIds = new SortedSet<int>(_realmInnovations.Keys);
		realmIds.UnionWith(_realmResearchPoints.Keys);
		realmIds.UnionWith(_realmCurrentResearch.Keys);
		realmIds.UnionWith(_realmDiscovered.Keys);
		var realmList = new List<RealmInnovationSaveData>(realmIds.Count);
		foreach (int realmId in realmIds)
		{
			_realmInnovations.TryGetValue(realmId, out var innovations);
			_realmDiscovered.TryGetValue(realmId, out var discovered);
			_realmResearchPoints.TryGetValue(realmId, out var points);
			_realmCurrentResearch.TryGetValue(realmId, out var current);

			var innovationIds = new List<int>();
			if (innovations != null) { innovationIds.AddRange(innovations); innovationIds.Sort(); }
			var discoveredIds = new List<int>();
			if (discovered != null) { discoveredIds.AddRange(discovered); discoveredIds.Sort(); }

			realmList.Add(new RealmInnovationSaveData
			{
				realmId = realmId,
				innovations = innovationIds.ToArray(),
				discovered = discoveredIds.ToArray(),
				researchPoints = points,
				currentResearch = current > 0 ? current : -1
			});
		}
		data.realms = realmList.ToArray();

		var progressIds = new List<int>(_innovationProgress.Keys);
		progressIds.Sort();
		var progressList = new List<InnovationProgressSaveData>(progressIds.Count);
		foreach (int innovationId in progressIds)
		{
			var progress = _innovationProgress[innovationId];
			if (progress == null) continue;
			var keys = new List<string>();
			var values = new List<float>();
			if (progress.gainBreakdown != null)
			{
				var breakdownKeys = new List<string>(progress.gainBreakdown.Keys);
				breakdownKeys.Sort(StringComparer.Ordinal);
				foreach (string key in breakdownKeys)
				{
					keys.Add(key);
					values.Add(progress.gainBreakdown[key]);
				}
			}
			progressList.Add(new InnovationProgressSaveData
			{
				innovationId = progress.innovationId > 0 ? progress.innovationId : innovationId,
				progress = progress.progress,
				monthlyGain = progress.monthlyGain,
				isAvailable = progress.isAvailable,
				lockedReason = progress.lockedReason ?? string.Empty,
				gainBreakdownKeys = keys.ToArray(),
				gainBreakdownValues = values.ToArray(),
				cumulativeOutput = progress.cumulativeOutput,
				averageQuality = progress.averageQuality,
				oldMethodPracticeCount = progress.oldMethodPracticeCount
			});
		}
		data.progress = progressList.ToArray();

		var resourceKeys = new List<string>(_resourceCumulativeOutput.Keys);
		resourceKeys.Sort(StringComparer.Ordinal);
		var resourceList = new List<StringFloatEntry>(resourceKeys.Count);
		foreach (string key in resourceKeys)
		{
			resourceList.Add(new StringFloatEntry { key = key, value = _resourceCumulativeOutput[key] });
		}
		data.resourceOutput = resourceList.ToArray();

		return data;
	}

	/// <summary>还原运行时状态（不清空革新定义表；旧档 data 为 null 时按空树处理）。</summary>
	public void RestoreState(InnovationTreeSaveData data)
	{
		_realmInnovations.Clear();
		_realmResearchPoints.Clear();
		_realmCurrentResearch.Clear();
		_realmDiscovered.Clear();
		_innovationProgress.Clear();
		_resourceCumulativeOutput.Clear();

		if (data == null) return;

		if (data.realms != null)
		{
			foreach (var realm in data.realms)
			{
				if (realm == null) continue;
				if (realm.innovations != null && realm.innovations.Length > 0)
					_realmInnovations[realm.realmId] = new HashSet<int>(realm.innovations);
				if (realm.discovered != null && realm.discovered.Length > 0)
					_realmDiscovered[realm.realmId] = new HashSet<int>(realm.discovered);
				if (realm.researchPoints != 0f)
					_realmResearchPoints[realm.realmId] = realm.researchPoints;
				if (realm.currentResearch > 0)
					_realmCurrentResearch[realm.realmId] = realm.currentResearch;
			}
		}

		if (data.progress != null)
		{
			foreach (var saved in data.progress)
			{
				if (saved == null) continue;
				var progress = new InnovationProgress
				{
					innovationId = saved.innovationId,
					progress = saved.progress,
					monthlyGain = saved.monthlyGain,
					isAvailable = saved.isAvailable,
					lockedReason = saved.lockedReason ?? string.Empty,
					cumulativeOutput = saved.cumulativeOutput,
					averageQuality = saved.averageQuality,
					oldMethodPracticeCount = saved.oldMethodPracticeCount
				};
				if (saved.gainBreakdownKeys != null && saved.gainBreakdownValues != null
					&& saved.gainBreakdownKeys.Length == saved.gainBreakdownValues.Length)
				{
					for (int i = 0; i < saved.gainBreakdownKeys.Length; i++)
						progress.gainBreakdown[saved.gainBreakdownKeys[i]] = saved.gainBreakdownValues[i];
				}
				_innovationProgress[saved.innovationId] = progress;
			}
		}

		if (data.resourceOutput != null)
		{
			foreach (var entry in data.resourceOutput)
			{
				if (entry == null || string.IsNullOrEmpty(entry.key)) continue;
				_resourceCumulativeOutput[entry.key] = entry.value;
			}
		}
	}
}
}
