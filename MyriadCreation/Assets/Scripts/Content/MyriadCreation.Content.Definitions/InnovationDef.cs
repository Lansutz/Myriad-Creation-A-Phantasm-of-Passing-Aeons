using System;
using System.Collections.Generic;
using MyriadCreation.Core;

namespace MyriadCreation.Content.Definitions
{
[Serializable]
public class InnovationDef
{
	public int innovationId;

	public string innovationName;

	public InnovationField field;

	public int era;

	public float researchCost;

	public string description;

	public List<string> affinityTags = new List<string>();

	public List<int> requiredAnyResources = new List<int>();

	public List<int> requiredAllResources = new List<int>();

	public bool allowTradeResource = true;

	public float requiredResourceAmount = 10f;

	public List<string> requiredCapabilities = new List<string>();

	public List<string> providesCapabilities = new List<string>();

	/// <summary>关联物产 id；软前置从这些物产的物理属性推导能力阈值。</summary>
	public List<int> associatedGoods = new List<int>();

	/// <summary>软前置：按关联物产物理属性推导能力阈值（不是硬前置树）。</summary>
	public List<InnovationSoftPrerequisite> softPrerequisites = new List<InnovationSoftPrerequisite>();

	public List<string> discoveryGroups = new List<string>();

	public List<CapabilityModifier> capabilityModifiers = new List<CapabilityModifier>();

	public List<CapabilityRequirement> requiredCapabilityValues = new List<CapabilityRequirement>();

	public List<string> practiceTags = new List<string>();


	/// <summary>内容 schema 版本：0/1=旧 185 条（待迁移），2=精确化条目（必须带定义/理由）。</summary>
	public int contentSchemaVersion;

	public string termEn = string.Empty;

	public string definition = string.Empty;

	public string scope = string.Empty;

	public InnovationKind kind;

	public string category = string.Empty;

	public List<string> sources = new List<string>();

	public List<InnovationPrerequisiteRationale> prerequisiteRationale = new List<InnovationPrerequisiteRationale>();

	/// <summary>前置表达式：All（全部）/Any（多选一）/Leaf（单个），可嵌套。</summary>
	public InnovationPrerequisiteNode prerequisiteTree;

	/// <summary>结构化解锁目标；与 unlocks 字符串并存，迁移期使用。</summary>
	public List<InnovationUnlock> unlockRefs = new List<InnovationUnlock>();

	public List<string> unlocks = new List<string>();

	public List<int> legacyIds = new List<int>();

	public int variantOf = -1;

	public List<string> regions = new List<string>();

	/// <summary>展平前置表达式中的所有叶子革新 id（UI/难度/测试用）。</summary>
	public List<int> GetPrerequisiteIds()
	{
		var result = new List<int>();
		CollectPrerequisiteIds(prerequisiteTree, result);
		return result;
	}

	private static void CollectPrerequisiteIds(InnovationPrerequisiteNode node, List<int> result)
	{
		if (node == null)
		{
			return;
		}
		if (node.IsLeaf)
		{
			if (node.innovationId > 0 && !result.Contains(node.innovationId))
			{
				result.Add(node.innovationId);
			}
			return;
		}
		if (node.children != null)
		{
			foreach (InnovationPrerequisiteNode child in node.children)
			{
				CollectPrerequisiteIds(child, result);
			}
		}
	}

	public InnovationDomain Domain => InnovationDomainMap.GetDomain(this.field);

	public string GetName()
	{
		if (!Localization.Has(innovationId + "_name"))
		{
			return innovationName;
		}
		return Localization.Get(innovationId + "_name");
	}

	public string GetDescription()
	{
		if (!Localization.Has(innovationId + "_desc"))
		{
			return description;
		}
		return Localization.Get(innovationId + "_desc");
	}
}
}
