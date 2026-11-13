using System;
using System.Collections.Generic;
using MyriadCreation.Core.Enums;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Culture
{
[Serializable]
public class CultureData
{
	public int cultureId;

	public string cultureName;

	public string cultureColorHex = "#ffffff";

	public GameEnums.CultureStage stage;

	[Obsolete("5A.3：七板块由传统因子与革新推导，新代码请读 GetSettlementDegree 等聚合方法")]
	public int livelihoodType;

	[Obsolete("5A.3：七板块由传统因子与革新推导，新代码请读聚合方法")]
	public int mobilityType;

	[Obsolete("5A.3：七板块由传统因子与革新推导")]
	public int burialType;

	[Obsolete("5A.3：七板块由传统因子与革新推导")]
	public List<int> burialTypes = new List<int>();

	[Obsolete("5A.3：七板块由传统因子与革新推导")]
	public float[] worshipVector;

	[Obsolete("5A.3：七板块由传统因子与革新推导")]
	public int materialStyle;

	[Obsolete("5A.3：七板块由传统因子与革新推导")]
	public int symbolicFocus;

	[Obsolete("5A.3：七板块由传统因子与革新推导")]
	public List<int> symbolicFoci = new List<int>();

	[Obsolete("5A.3：七板块由传统因子与革新推导")]
	public int environmentAdapt;

	[Obsolete("5A.3：七板块由传统因子与革新推导")]
	public List<int> environmentAdapts = new List<int>();

	[Obsolete("5A.3：并入生产传统 ResourcePreference 因子")]
	public int productionPreference;

	public int faithId = -1;

	public string languageId = "";

	public string languageFamilyId = "";

	public string ethnicGroupId = "";

	public string heritageId = "";

	public string ethosId = "";

	public List<string> traditionIds = new List<string>();

	public float maturity;

	public float spreadPower = 1f;

	public int parentCultureId = -1;

	public List<int> childCultureIds = new List<int>();

	public bool allowsBranching = true;

	/// <summary>该文化是否已具备家族纹章习俗（内容/事件开启；默认无，不是所有文化都有）。</summary>
	public bool heraldryEnabled;

	/// <summary>被哪个文化吸收（-1=独立）；吸收后保留实体供人物引用，但不再作为可传播文化。</summary>
	public int mergedIntoCultureId = -1;

	/// <summary>最近一次分裂的日期（-1=从未）；用于同一文化两次分裂之间的冷却。</summary>
	public int lastSplitDay = -1;

	public Color color = Color.white;

	public InheritanceLaw defaultSuccessionLaw = InheritanceLaw.Primogeniture();

	public List<OfficeTitleEntry> officialTitles = new List<OfficeTitleEntry>();

	public List<string> innovationAffinities = new List<string>();

	public bool HasInnovationAffinity(string fieldName)
	{
		if (string.IsNullOrEmpty(fieldName) || innovationAffinities == null)
		{
			return false;
		}
		foreach (string innovationAffinity in innovationAffinities)
		{
			if (string.Equals(innovationAffinity, fieldName, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	public bool HasTradition(string traditionId, IContentLookup content = null)
	{
		if (string.IsNullOrEmpty(traditionId))
		{
			return false;
		}
		if (traditionIds != null && traditionIds.Contains(traditionId))
		{
			return true;
		}
		// B5：内容查询通过 host 注入的 IContentLookup（Culture 不直接引用 Modding/ContentResolvers）。
		if (content != null && content.TryGetEthnicGroupTraditionIds(cultureId, out string[] groupTraditionIds) && groupTraditionIds != null)
		{
			foreach (string groupTraditionId in groupTraditionIds)
			{
				if (groupTraditionId == traditionId)
				{
					return true;
				}
			}
		}
		return false;
	}

	public float GetFactorSum(TraditionFactorKey key, IContentLookup content = null)
	{
		if (traditionIds == null || content == null)
		{
			return 0f;
		}
		float num = 0f;
		foreach (string traditionId in traditionIds)
		{
			if (content.TryGetTraditionFactor(traditionId, key, out float value))
			{
				num += value;
			}
		}
		return num;
	}

	public float GetSettlementDegree(IContentLookup content = null)
	{
		float factorSum = GetFactorSum(TraditionFactorKey.SettlementDegree, content);
		if (traditionIds != null && traditionIds.Count > 0)
		{
			return Mathf.Clamp01(factorSum);
		}
		float num = mobilityType switch
		{
			2 => 0.8f, 
			1 => 0.45f, 
			_ => 0.1f, 
		};
		if (livelihoodType == 2)
		{
			num = 0.1f;
		}
		return Mathf.Clamp01(Mathf.Max(num, factorSum));
	}

	public float GetSeparationTendency(IContentLookup content = null)
	{
		float factorSum = GetFactorSum(TraditionFactorKey.SeparationTendency, content);
		if (traditionIds != null && traditionIds.Count > 0)
		{
			return Mathf.Clamp01(factorSum);
		}
		return Mathf.Clamp01(Mathf.Max(mobilityType switch
		{
			0 => 0.9f, 
			1 => 0.55f, 
			_ => 0.25f, 
		}, factorSum));
	}

	public float GetFusionTendency(IContentLookup content = null)
	{
		float factorSum = GetFactorSum(TraditionFactorKey.FusionTendency, content);
		if (traditionIds != null && traditionIds.Count > 0)
		{
			return Mathf.Clamp01(factorSum);
		}
		return Mathf.Clamp01(Mathf.Max(mobilityType switch
		{
			0 => 0.85f, 
			1 => 0.5f, 
			_ => 0.2f, 
		}, factorSum));
	}

	public CultureData()
	{
	}
}
}
