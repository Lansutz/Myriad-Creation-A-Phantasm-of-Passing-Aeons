using System;
using System.Collections.Generic;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Contracts;

namespace MyriadCreation.Simulation.Faith
{
[Serializable]
public class FaithSystem
{
	public int faithId;

	/// <summary>上位 religion；-1 = 无统一组织的合法信仰。</summary>
	public int religionId = -1;

	/// <summary>所属宗派；仅挂 religion 的 faith 需要宗派，无组织 faith 保持 -1。</summary>
	public int denominationId = -1;

	/// <summary>
	/// **传承**：本 faith 由哪个 faith 分出（G2 落地后填）。-1 = 初代信仰。
	/// 不是文化传统（TraditionDef），也不是遗产（Heritage）。
	/// </summary>
	public int lineageFaithId = -1;

	public string faithName;

	public string description;

	public FaithType type = FaithType.Polytheistic;

	public List<Deity> deities = new List<Deity>();

	public string chiefDeity;

	public List<Doctrine> doctrines = new List<Doctrine>();

	public List<string> rituals = new List<string>();

	public List<string> taboos = new List<string>();

	public FaithOrganizationType orgType;

	public int highPriestCharacterId = -1;

	/// <summary>运行时教义覆盖（optionId）；空 = 沿用内容表 religion 的 selectedDoctrines。合一/改宗写入。</summary>
	public List<string> doctrineIds = new List<string>();

	/// <summary>共融期限（到此日为止不再升级为敌意/分裂；-1 = 无共融约定）。妥协写入。</summary>
	public int communionUntilDay = -1;

	public float churchWealth;

	public float churchInfluence;

	public List<int> holySiteTileIndices = new List<int>();

	public float missionaryZeal = 0.5f;

	public float tolerance = 0.5f;

	[NonSerialized]
	public Dictionary<int, float> regionAdherence = new Dictionary<int, float>();

	public List<int> followerCharacterIds = new List<int>();

	[NonSerialized]
	public Dictionary<int, float> faithRelations = new Dictionary<int, float>();

	public List<string> virtues = new List<string>();

	public List<string> sins = new List<string>();

	public int hierarchyLevel;

	public List<EcclesiasticalTitle> hierarchyTitles = new List<EcclesiasticalTitle>();

	public float fervor = 50f;

	public const float GreatHolyWarThreshold = 60f;

	public void SetHierarchyLevel(int level)
	{
		hierarchyLevel = Mathf.Clamp(level, 0, 4);
	}

	public void AddHierarchyTitle(string titleName, int level, bool temporalAppointment)
	{
		hierarchyTitles.Add(new EcclesiasticalTitle
		{
			titleName = titleName,
			level = level,
			temporalAppointment = temporalAppointment
		});
		if (level > hierarchyLevel)
		{
			hierarchyLevel = level;
		}
	}

	public void AddFervor(float delta)
	{
		fervor = Mathf.Clamp(fervor + delta, 0f, 100f);
	}

	public bool CanDeclareGreatHolyWar()
	{
		if (fervor >= 60f)
		{
			return highPriestCharacterId >= 0;
		}
		return false;
	}

	/// <summary>按角色特质 id 列表计算美德分（Faith 不持有 CharacterData）。</summary>
	public int GetVirtueScore(IReadOnlyList<string> traitIds)
	{
		if (traitIds == null)
		{
			return 0;
		}
		int num = 0;
		foreach (string traitId in traitIds)
		{
			if (string.IsNullOrEmpty(traitId))
			{
				continue;
			}
			foreach (string virtue in virtues)
			{
				if (traitId == virtue || traitId.StartsWith(virtue + "_"))
				{
					num++;
				}
			}
		}
		return num;
	}

	public int GetSinScore(IReadOnlyList<string> traitIds)
	{
		if (traitIds == null)
		{
			return 0;
		}
		int num = 0;
		foreach (string traitId in traitIds)
		{
			if (string.IsNullOrEmpty(traitId))
			{
				continue;
			}
			foreach (string sin in sins)
			{
				if (traitId == sin || traitId.StartsWith(sin + "_"))
				{
					num++;
				}
			}
		}
		return num;
	}

	/// <summary>context 便捷重载（host 注入角色特质查询）。</summary>
	public int GetVirtueScore(int characterId, IFaithContext context)
	{
		return GetVirtueScore(context?.GetCharacterTraitIds(characterId));
	}

	public int GetSinScore(int characterId, IFaithContext context)
	{
		return GetSinScore(context?.GetCharacterTraitIds(characterId));
	}

	public float CalculateReligiousAuthority()
	{
		float num = 0f;
		foreach (KeyValuePair<int, float> item in regionAdherence)
		{
			num += item.Value;
		}
		return churchInfluence * 0.4f + num * 0.3f + (float)followerCharacterIds.Count * 0.3f;
	}

	public void DailyTick()
	{
		churchInfluence = Mathf.Lerp(churchInfluence, 50f, 0.001f);
	}
}
}
