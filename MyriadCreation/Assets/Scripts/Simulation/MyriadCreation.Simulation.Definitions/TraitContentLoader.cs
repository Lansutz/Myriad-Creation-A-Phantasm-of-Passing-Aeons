using System;
using System.Collections.Generic;
using System.IO;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.State;
using UnityEngine;

namespace MyriadCreation.Simulation.Definitions
{
[Serializable]
public sealed class TraitContentDef
{
	public string traitId = "";

	public string traitName = "";

	public string description = "";

	public int category;

	public int tier;

	public float martialMod;

	public float diplomacyMod;

	public float stewardshipMod;

	public float intrigueMod;

	public float learningMod;

	public float warfareMod;

	public float charmMod;

	public List<string> conflictingTraits = new List<string>();

	public List<string> requiredTraits = new List<string>();
}

[Serializable]
internal sealed class TraitListWrapper
{
	public List<TraitContentDef> traits = new List<TraitContentDef>();
}

/// <summary>
/// 特质表外提（P2.4）：从 StreamingAssets/Base/Trait/Traits.json 懒加载；
/// 文件缺失/非法时返回空表，由 PersonalityTraitDatabase 回退到代码表。
/// </summary>
public static class TraitContentLoader
{
	private static bool _attempted;

	private static PersonalityTrait[] _traits = Array.Empty<PersonalityTrait>();

	public static PersonalityTrait[] Traits => _traits;

	public static bool EnsureLoaded()
	{
		if (!_attempted)
		{
			LoadFromFile(ResolveDefaultPath());
		}
		return _traits.Length > 0;
	}

	public static string ResolveDefaultPath()
	{
		string root = Application.streamingAssetsPath;
		return string.IsNullOrEmpty(root) ? "" : Path.Combine(root, "Base/Trait/Traits.json");
	}

	public static bool LoadFromFile(string path)
	{
		_attempted = true;
		_traits = Array.Empty<PersonalityTrait>();
		if (string.IsNullOrEmpty(path) || !File.Exists(path))
		{
			return false;
		}
		try
		{
			TraitListWrapper wrapper = JsonUtility.FromJson<TraitListWrapper>(File.ReadAllText(path));
			if (wrapper?.traits == null || wrapper.traits.Count == 0)
			{
				return false;
			}
			List<PersonalityTrait> parsed = new List<PersonalityTrait>(wrapper.traits.Count);
			for (int i = 0; i < wrapper.traits.Count; i++)
			{
				TraitContentDef def = wrapper.traits[i];
				if (def == null || string.IsNullOrEmpty(def.traitId))
				{
					continue;
				}
				parsed.Add(new PersonalityTrait
				{
					traitId = def.traitId,
					traitName = def.traitName ?? "",
					description = def.description ?? "",
					category = (TraitCategory)ClampEnum(def.category, typeof(TraitCategory)),
					tier = (TraitTier)ClampEnum(def.tier, typeof(TraitTier)),
					martialMod = def.martialMod,
					diplomacyMod = def.diplomacyMod,
					stewardshipMod = def.stewardshipMod,
					intrigueMod = def.intrigueMod,
					learningMod = def.learningMod,
					warfareMod = def.warfareMod,
					charmMod = def.charmMod,
					conflictingTraits = new List<string>(def.conflictingTraits ?? new List<string>()),
					requiredTraits = new List<string>(def.requiredTraits ?? new List<string>())
				});
			}
			_traits = parsed.ToArray();
			return _traits.Length > 0;
		}
		catch (Exception ex)
		{
			Debug.LogError("[TraitContentLoader] 读取 Traits.json 失败：" + ex.Message);
			return false;
		}
	}

	private static int ClampEnum(int value, Type enumType)
	{
		if (Enum.IsDefined(enumType, value))
		{
			return value;
		}
		return 0;
	}
}
}
