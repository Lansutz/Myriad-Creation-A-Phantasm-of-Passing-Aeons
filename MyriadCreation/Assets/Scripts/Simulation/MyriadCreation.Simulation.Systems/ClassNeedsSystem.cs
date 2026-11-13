using System;
using System.Collections.Generic;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
public class ClassNeedsSystem
{
	private const float SubsistenceBase = 40f;

	private const float SubsistenceSlope = 50f;

	private const float SubsistenceDisasterPenalty = 0.2f;

	private const float WarPenalty = 15f;

	private const float HomeSoilWarPenalty = 30f;

	private const float SecurityDisasterPenalty = 0.15f;

	private const float EconTradeWeight = 30f;

	private const float EconMonetaryWeight = 20f;

	private const float EconBase = 50f;

	private const float RecognizedScore = 75f;

	private const float UnrecognizedScore = 20f;

	public const float GrievanceThreshold = 45f;

	public ClassNeedWeightTable Weights { get; set; } = new ClassNeedWeightTable();

	public ClassNeedReport EvaluateClass(GameEnums.SocialClass cls, RealmSituation sit)
	{
		ClassNeedReport classNeedReport = new ClassNeedReport
		{
			socialClass = cls
		};
		float[] weights = Weights.GetWeights(cls);
		Add(classNeedReport, ClassNeedDimension.Subsistence, EvalSubsistence(cls, sit), SubsistenceReason(sit));
		Add(classNeedReport, ClassNeedDimension.Security, EvalSecurity(sit), SecurityReason(sit));
		Add(classNeedReport, ClassNeedDimension.TaxBurden, EvalTaxBurden(cls, sit), TaxReason(cls, sit));
		Add(classNeedReport, ClassNeedDimension.PoliticalAccess, EvalPoliticalAccess(cls, sit), AccessReason(cls, sit));
		Add(classNeedReport, ClassNeedDimension.EconomicOpportunity, EvalEconomic(sit), EconReason(sit));
		Add(classNeedReport, ClassNeedDimension.InstitutionalRecognition, EvalRecognition(cls, sit), RecognitionReason(cls, sit));
		Add(classNeedReport, ClassNeedDimension.Legitimacy, EvalLegitimacy(sit), LegitimacyReason(sit));
		Add(classNeedReport, ClassNeedDimension.Privilege, EvalPrivilege(sit), PrivilegeReason(sit));
		float num = 0f;
		float num2 = 0f;
		foreach (ClassNeedDimension value2 in Enum.GetValues(typeof(ClassNeedDimension)))
		{
			int num3 = (int)value2;
			if (!(weights[num3] <= 0f) && classNeedReport.dimensions.TryGetValue(value2, out var value))
			{
				num += value.score * weights[num3];
				num2 += weights[num3];
			}
		}
		classNeedReport.overallSatisfaction = ((num2 > 0f) ? Mathf.Clamp(num / num2, 0f, 100f) : 50f);
		classNeedReport.worstScore = 100f;
		foreach (KeyValuePair<ClassNeedDimension, NeedDimensionScore> dimension in classNeedReport.dimensions)
		{
			int key = (int)dimension.Key;
			if (key < weights.Length && !(weights[key] <= 0f) && dimension.Value.score < classNeedReport.worstScore)
			{
				classNeedReport.worstScore = dimension.Value.score;
				classNeedReport.worstDimension = dimension.Key;
			}
		}
		return classNeedReport;
	}

	public Dictionary<GameEnums.SocialClass, ClassNeedReport> EvaluateRealm(RealmSituation sit)
	{
		Dictionary<GameEnums.SocialClass, ClassNeedReport> dictionary = new Dictionary<GameEnums.SocialClass, ClassNeedReport>();
		foreach (GameEnums.SocialClass value in Enum.GetValues(typeof(GameEnums.SocialClass)))
		{
			dictionary[value] = EvaluateClass(value, sit);
		}
		return dictionary;
	}

	private static float EvalSubsistence(GameEnums.SocialClass cls, RealmSituation s)
	{
		float num = ((cls == GameEnums.SocialClass.Slave) ? 40f : 50f);
		return Mathf.Clamp(40f + Mathf.Clamp(s.foodSecurity, 0f, 1.2f) / 1.2f * num - s.disasterSeverity * 0.2f, 0f, 100f);
	}

	private static float EvalSecurity(RealmSituation s)
	{
		return Mathf.Clamp(s.publicOrder - (s.atWar ? 15f : 0f) - (s.warOnHomeSoil ? 30f : 0f) - s.disasterSeverity * 0.15f, 0f, 100f);
	}

	private static float EvalTaxBurden(GameEnums.SocialClass cls, RealmSituation s)
	{
		return Mathf.Clamp(100f - s.GetTaxPain(cls), 0f, 100f);
	}

	private static float EvalPoliticalAccess(GameEnums.SocialClass cls, RealmSituation s)
	{
		return Mathf.Clamp01(s.GetPoliticalAccess(cls)) * 100f;
	}

	private static float EvalEconomic(RealmSituation s)
	{
		return Mathf.Clamp(50f + Mathf.Clamp01(s.tradeFlow) * 30f + Mathf.Clamp01(s.monetaryStability) * 20f, 0f, 100f);
	}

	private static float EvalRecognition(GameEnums.SocialClass cls, RealmSituation s)
	{
		if (!s.IsRecognized(cls))
		{
			return 20f;
		}
		return 75f;
	}

	private static float EvalLegitimacy(RealmSituation s)
	{
		return Mathf.Clamp(s.stability * 0.5f + s.legitimacy * 0.5f, 0f, 100f);
	}

	private static float EvalPrivilege(RealmSituation s)
	{
		return Mathf.Clamp01(s.privilegeSecurity) * 100f;
	}

	private static string ReasonIfLow(float score, string reason)
	{
		if (!(score < 45f))
		{
			return "";
		}
		return reason;
	}

	private static string SubsistenceReason(RealmSituation s)
	{
		return ReasonIfLow(40f + Mathf.Clamp(s.foodSecurity, 0f, 1.2f) / 1.2f * 50f, "粮食短缺/饥荒");
	}

	private static string SecurityReason(RealmSituation s)
	{
		if (s.warOnHomeSoil)
		{
			return "本土遭兵燹劫掠";
		}
		if (s.atWar)
		{
			return "战争征兵与不安";
		}
		if (s.publicOrder < 45f)
		{
			return "治安崩坏";
		}
		return "";
	}

	private static string TaxReason(GameEnums.SocialClass cls, RealmSituation s)
	{
		return ReasonIfLow(100f - s.GetTaxPain(cls), cls switch
		{
			GameEnums.SocialClass.MerchantFreeman => "商税/手工业税过重", 
			GameEnums.SocialClass.Peasant => "农业/人头税过重", 
			_ => "税负过重", 
		});
	}

	private static string AccessReason(GameEnums.SocialClass cls, RealmSituation s)
	{
		return ReasonIfLow(s.GetPoliticalAccess(cls) * 100f, "被排除在政治通道之外");
	}

	private static string EconReason(RealmSituation s)
	{
		if (s.tradeFlow < 0.5f)
		{
			return "商路受阻/市场萧条";
		}
		if (s.monetaryStability < 0.5f)
		{
			return "货币贬值通胀";
		}
		return "";
	}

	private static string RecognitionReason(GameEnums.SocialClass cls, RealmSituation s)
	{
		if (!s.IsRecognized(cls))
		{
			return "该阶层未获制度承认";
		}
		return "";
	}

	private static string LegitimacyReason(RealmSituation s)
	{
		return ReasonIfLow(s.stability * 0.5f + s.legitimacy * 0.5f, "政权合法性/秩序动摇");
	}

	private static string PrivilegeReason(RealmSituation s)
	{
		return ReasonIfLow(s.privilegeSecurity * 100f, "世袭/免税/土地特权受威胁");
	}

	private static void Add(ClassNeedReport r, ClassNeedDimension d, float score, string reason)
	{
		r.dimensions[d] = new NeedDimensionScore
		{
			dimension = d,
			score = score,
			reason = reason
		};
	}
}
}
