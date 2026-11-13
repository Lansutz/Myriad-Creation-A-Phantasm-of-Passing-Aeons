using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MyriadCreation.Content.Definitions
{
public static class EconomicalArchetypes
{
	private static EconomicalArchetypeInfo[] _all;

	public static EconomicalArchetypeInfo[] All => _all ??= EconomicalArchetypeLoader.EnsureLoaded() ? EconomicalArchetypeLoader.Archetypes : Fallback;

	private static readonly EconomicalArchetypeInfo[] Fallback = new EconomicalArchetypeInfo[11]
	{
		new EconomicalArchetypeInfo(EconomicalArchetype.Balanced, "均衡之主", "无明显战略倾向，行事中庸", 0f, 0f, 0f, 0f, 0f),
		new EconomicalArchetypeInfo(EconomicalArchetype.Warlike, "好战之君", "崇尚武力，以战争扩张为第一要务", 0.8f, -0.2f, 0.1f, 0.1f, -0.2f),
		new EconomicalArchetypeInfo(EconomicalArchetype.Cautious, "谨慎之主", "谋定后动，偏好防御与巩固", -0.3f, 0.3f, 0.1f, 0.1f, 0.2f),
		new EconomicalArchetypeInfo(EconomicalArchetype.EconomicalBoom, "繁荣缔造者", "专注经济建设与领地开发", -0.2f, 0.8f, 0f, 0f, 0.2f),
		new EconomicalArchetypeInfo(EconomicalArchetype.PiousBuilder, "虔信营建者", "以信仰为名兴建宗教建筑与转化", -0.1f, 0.4f, 0.8f, -0.1f, 0.1f),
		new EconomicalArchetypeInfo(EconomicalArchetype.Conqueror, "征服者", "为征服而生，永不满足于现有疆土", 1f, -0.3f, 0.2f, 0.2f, -0.3f),
		new EconomicalArchetypeInfo(EconomicalArchetype.Unpredictable, "不可预测者", "行事乖张，令人难以捉摸", 0.3f, 0f, 0f, 0.3f, -0.2f),
		new EconomicalArchetypeInfo(EconomicalArchetype.Administrator, "行政巨匠", "崇尚法治与行政效率，精于吏治", -0.2f, 0.4f, 0.1f, 0f, 0.3f),
		new EconomicalArchetypeInfo(EconomicalArchetype.Schemer, "阴谋大师", "在阴影中操纵一切，偏好暗杀与勒索", 0.1f, 0f, 0f, 0.9f, -0.1f),
		new EconomicalArchetypeInfo(EconomicalArchetype.CulturalPatron, "文化赞助人", "推崇文化艺术，以软实力教化四方", -0.2f, 0.5f, 0.2f, 0f, 0.4f),
		new EconomicalArchetypeInfo(EconomicalArchetype.GodlessReformer, "不敬神的改革者", "漠视宗教权威，推动世俗化与制度改革", 0.1f, 0.3f, -0.8f, 0.2f, 0.1f)
	};

	public static EconomicalArchetypeInfo Get(EconomicalArchetype archetype)
	{
		EconomicalArchetypeInfo[] all = All;
		foreach (EconomicalArchetypeInfo economicalArchetypeInfo in all)
		{
			if (economicalArchetypeInfo.archetype == archetype)
			{
				return economicalArchetypeInfo;
			}
		}
		return All[0];
	}
}

[Serializable]
public sealed class EconomicalArchetypeDef
{
	public int archetype;
	public string displayName = "";
	public string description = "";
	public float warBias;
	public float buildBias;
	public float faithBias;
	public float schemeBias;
	public float diplomacyBias;
}

[Serializable]
internal sealed class EconomicalArchetypeListWrapper
{
	public List<EconomicalArchetypeDef> archetypes = new List<EconomicalArchetypeDef>();
}

/// <summary>经济原型文案外提（P2.4）：内容优先，代码兜底。</summary>
public static class EconomicalArchetypeLoader
{
	private static bool _attempted;
	private static EconomicalArchetypeInfo[] _archetypes = Array.Empty<EconomicalArchetypeInfo>();

	public static EconomicalArchetypeInfo[] Archetypes => _archetypes;

	public static bool EnsureLoaded()
	{
		if (!_attempted)
		{
			LoadFromFile(ResolveDefaultPath());
		}
		return _archetypes.Length > 0;
	}

	public static string ResolveDefaultPath()
	{
		string root = Application.streamingAssetsPath;
		return string.IsNullOrEmpty(root) ? "" : Path.Combine(root, "Base/Archetype/EconomicalArchetypes.json");
	}

	public static bool LoadFromFile(string path)
	{
		_attempted = true;
		_archetypes = Array.Empty<EconomicalArchetypeInfo>();
		if (string.IsNullOrEmpty(path) || !File.Exists(path))
		{
			return false;
		}
		try
		{
			EconomicalArchetypeListWrapper wrapper =
				JsonUtility.FromJson<EconomicalArchetypeListWrapper>(File.ReadAllText(path));
			if (wrapper?.archetypes == null || wrapper.archetypes.Count == 0)
			{
				return false;
			}
			List<EconomicalArchetypeInfo> parsed = new List<EconomicalArchetypeInfo>(wrapper.archetypes.Count);
			for (int i = 0; i < wrapper.archetypes.Count; i++)
			{
				EconomicalArchetypeDef def = wrapper.archetypes[i];
				if (def == null || !Enum.IsDefined(typeof(EconomicalArchetype), def.archetype))
				{
					continue;
				}
				parsed.Add(new EconomicalArchetypeInfo(
					(EconomicalArchetype)def.archetype, def.displayName, def.description,
					def.warBias, def.buildBias, def.faithBias, def.schemeBias, def.diplomacyBias));
			}
			_archetypes = parsed.ToArray();
			return _archetypes.Length > 0;
		}
		catch (Exception ex)
		{
			Debug.LogError("[EconomicalArchetypeLoader] 读取 JSON 失败：" + ex.Message);
			return false;
		}
	}
}
}
