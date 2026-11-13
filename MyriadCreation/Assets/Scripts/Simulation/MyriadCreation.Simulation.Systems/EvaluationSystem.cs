using MyriadCreation.Simulation.State;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
public static class EvaluationSystem
{
	public static string LevelName(EvaluationLevel level)
	{
		return level switch
		{
			EvaluationLevel.Legendary => "传奇", 
			EvaluationLevel.Preeminent => "卓越", 
			EvaluationLevel.Distinguished => "杰出", 
			EvaluationLevel.Excellent => "优秀", 
			EvaluationLevel.Mediocre => "平平", 
			EvaluationLevel.Ordinary => "平庸", 
			EvaluationLevel.Obscure => "无名", 
			EvaluationLevel.Infamous => "遗臭", 
			_ => "未知", 
		};
	}

	public static EvaluationLevel LevelFromScore(float score)
	{
		if (score >= 900f)
		{
			return EvaluationLevel.Legendary;
		}
		if (score >= 750f)
		{
			return EvaluationLevel.Preeminent;
		}
		if (score >= 550f)
		{
			return EvaluationLevel.Distinguished;
		}
		if (score >= 350f)
		{
			return EvaluationLevel.Excellent;
		}
		if (score >= 200f)
		{
			return EvaluationLevel.Mediocre;
		}
		if (score >= 100f)
		{
			return EvaluationLevel.Ordinary;
		}
		if (score >= 0f)
		{
			return EvaluationLevel.Obscure;
		}
		return EvaluationLevel.Infamous;
	}

	public static float CalculateScore(AchievementRecord r)
	{
		float num = 50f;
		num += (float)r.warsWon * 30f;
		num += (float)r.conquests * 40f;
		num += (float)r.cultureActs * 35f;
		num += (float)r.poetryActs * 40f;
		num += (float)r.religionActs * 25f;
		num -= (float)r.defeatedBattles * 20f;
		num -= (float)r.massacres * 60f;
		num -= (float)r.rebellions * 15f;
		num += (float)r.defensiveWins * 25f;
		if (r.usurpedThrone)
		{
			num -= 30f;
		}
		if (r.canonized)
		{
			num += 80f;
		}
		num += (float)r.schemesSucceeded * 20f;
		num += (float)r.threatsResolved * 30f;
		num += (float)r.expeditions * 45f;
		if (r.famineUnderRule)
		{
			num -= 150f;
		}
		if (r.lostAllLands > 0)
		{
			num -= 100f;
		}
		return Mathf.Clamp(num, -100f, 1000f);
	}
}
}
