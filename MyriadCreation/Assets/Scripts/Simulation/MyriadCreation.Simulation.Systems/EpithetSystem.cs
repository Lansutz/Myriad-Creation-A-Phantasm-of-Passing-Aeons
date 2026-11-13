using MyriadCreation.Simulation.State;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
public static class EpithetSystem
{
	public static bool GrantEpithet(CharacterData c, string epithet)
	{
		if (c == null || string.IsNullOrEmpty(epithet))
		{
			return false;
		}
		if (!string.IsNullOrEmpty(c.epithet))
		{
			return false;
		}
		c.epithet = epithet;
		return true;
	}

	public static bool PromoteToLegendary(CharacterData c, string legendaryEpithet)
	{
		if (c == null || string.IsNullOrEmpty(legendaryEpithet))
		{
			return false;
		}
		c.epithet = legendaryEpithet;
		return true;
	}

	public static string EvaluateAndGrant(CharacterData c, AchievementRecord rec)
	{
		if (c == null)
		{
			return "";
		}
		float num = EvaluationSystem.CalculateScore(rec);
		EvaluationLevel evaluationLevel = EvaluationSystem.LevelFromScore(num);
		bool flag = c.role == CharacterRole.Ruler;
		if (flag)
		{
			if (evaluationLevel >= EvaluationLevel.Legendary && rec.conquests >= 12 && rec.warsWon >= 15)
			{
				if (!PromoteToLegendary(c, "征服王"))
				{
					return c.epithet;
				}
				return "征服王";
			}
			if (evaluationLevel >= EvaluationLevel.Legendary && ((rec.expeditions >= 8 && rec.conquests >= 3) || rec.expeditions >= 12))
			{
				if (!PromoteToLegendary(c, "冒险王"))
				{
					return c.epithet;
				}
				return "冒险王";
			}
			if (rec.expeditions >= 5 && num >= 550f && rec.poetryActs >= 3)
			{
				if (!PromoteToLegendary(c, "诗人王"))
				{
					return c.epithet;
				}
				return "诗人王";
			}
			if (HasTraitLevel(c, "arrogant", 2) && HasTraitAny(c, "paranoid") && c.vengefulness > 30f && c.rationality < -20f && c.boldness > 30f && c.honor < 0f && rec.reignYears >= 10f)
			{
				if (!GrantEpithet(c, "疯王"))
				{
					return c.epithet;
				}
				return "疯王";
			}
			if (num >= 750f && c.compassion >= 60f && c.honor >= 60f && !rec.famineUnderRule && rec.reignYears >= 15f)
			{
				if (!PromoteToLegendary(c, "圣君"))
				{
					return c.epithet;
				}
				return "圣君";
			}
		}
		if (rec.regionalInfluence >= 0.6f && GrantEpithet(c, "伟大者"))
		{
			return "伟大者";
		}
		if (string.IsNullOrEmpty(c.epithet))
		{
			if (!string.IsNullOrEmpty(c.mentalDisorderId))
			{
				if (!GrantEpithet(c, c.isMale ? "疯子" : "疯女"))
				{
					return "";
				}
				if (!c.isMale)
				{
					return "疯女";
				}
				return "疯子";
			}
			if (!flag && EvaluationSystem.CalculateScore(rec) >= 550f && (rec.warsWon >= 5 || rec.conquests >= 3 || rec.reignYears >= 10f))
			{
				if (!GrantEpithet(c, "无冠者"))
				{
					return "";
				}
				return "无冠者";
			}
			if (!c.isMale)
			{
				if (rec.canonized)
				{
					if (!GrantEpithet(c, "圣女"))
					{
						return "";
					}
					return "圣女";
				}
				if (flag && c.spouseId < 0 && rec.reignYears >= 5f)
				{
					if (!GrantEpithet(c, "童贞女王"))
					{
						return "";
					}
					return "童贞女王";
				}
				if (c.compassion < -30f && c.vengefulness > 40f && rec.schemesSucceeded >= 3)
				{
					if (!GrantEpithet(c, "母狼"))
					{
						return "";
					}
					return "母狼";
				}
				if (flag && rec.schemesSucceeded >= 5 && c.honor < 0f)
				{
					if (!GrantEpithet(c, "黑王后"))
					{
						return "";
					}
					return "黑王后";
				}
				if (flag && rec.reignYears >= 15f && !rec.famineUnderRule && rec.rebellions == 0)
				{
					if (!GrantEpithet(c, "富女"))
					{
						return "";
					}
					return "富女";
				}
				if (HasBodyMark(c, "俊美"))
				{
					if (!GrantEpithet(c, "美人"))
					{
						return "";
					}
					return "美人";
				}
			}
			if (flag && rec.youngAccession && (rec.ruledUnderRegency || c.rationality < -20f))
			{
				if (!GrantEpithet(c, "年轻者"))
				{
					return "";
				}
				return "年轻者";
			}
			if (!flag && rec.ruledUnderRegency && rec.reignYears >= 8f && EvaluationSystem.CalculateScore(rec) >= 350f)
			{
				if (!GrantEpithet(c, "护国公"))
				{
					return "";
				}
				return "护国公";
			}
			if (HasBodyMark(c, "癞病"))
			{
				if (!GrantEpithet(c, "癞病人"))
				{
					return "";
				}
				return "癞病人";
			}
			if (HasBodyMark(c, "驼背"))
			{
				if (!GrantEpithet(c, "驼背"))
				{
					return "";
				}
				return "驼背";
			}
			if (HasBodyMark(c, "俊美"))
			{
				if (!GrantEpithet(c, "美男子"))
				{
					return "";
				}
				return "美男子";
			}
			if (rec.schemesSucceeded >= 6 && c.rationality >= 40f)
			{
				if (!GrantEpithet(c, "金口"))
				{
					return "";
				}
				return "金口";
			}
			if (c.honor < -40f && rec.schemesSucceeded >= 4)
			{
				if (!GrantEpithet(c, "背信者"))
				{
					return "";
				}
				return "背信者";
			}
			if (c.vengefulness > 60f && c.boldness > 50f && rec.warsWon >= 5)
			{
				if (!GrantEpithet(c, "狂暴者"))
				{
					return "";
				}
				return "狂暴者";
			}
			if (rec.reignYears >= 15f && rec.warsWon <= 1 && rec.schemesSucceeded >= 3)
			{
				if (!GrantEpithet(c, "调停者"))
				{
					return "";
				}
				return "调停者";
			}
			if (rec.defeatedBattles >= 4 || (rec.famineUnderRule && rec.defeatedBattles >= 2))
			{
				if (!GrantEpithet(c, "不幸者"))
				{
					return "";
				}
				return "不幸者";
			}
			if (c.honor >= 50f && rec.warsWon >= 6 && rec.defeatedBattles == 0)
			{
				if (!GrantEpithet(c, "鹰"))
				{
					return "";
				}
				return "鹰";
			}
			if (rec.conquests >= 5)
			{
				if (!GrantEpithet(c, "征服者"))
				{
					return "";
				}
				return "征服者";
			}
			if (rec.schemesSucceeded >= 5)
			{
				if (!GrantEpithet(c, "狐狸"))
				{
					return "";
				}
				return "狐狸";
			}
			if (rec.warsWon >= 8 && rec.defeatedBattles <= 2)
			{
				if (!GrantEpithet(c, "狮子"))
				{
					return "";
				}
				return "狮子";
			}
			if (rec.warsWon >= 5 && rec.defeatedBattles == 0)
			{
				if (!GrantEpithet(c, "常胜者"))
				{
					return "";
				}
				return "常胜者";
			}
			if (rec.poetryActs >= 4)
			{
				if (!GrantEpithet(c, "诗人"))
				{
					return "";
				}
				return "诗人";
			}
			if (rec.cultureActs >= 6)
			{
				if (!GrantEpithet(c, "智者"))
				{
					return "";
				}
				return "智者";
			}
			if (rec.religionActs >= 6)
			{
				if (!GrantEpithet(c, "虔诚者"))
				{
					return "";
				}
				return "虔诚者";
			}
			if (rec.faithChanges >= 2)
			{
				if (!GrantEpithet(c, "叛教者"))
				{
					return "";
				}
				return "叛教者";
			}
			if (rec.threatsResolved >= 5)
			{
				if (!GrantEpithet(c, "警觉者"))
				{
					return "";
				}
				return "警觉者";
			}
			if (rec.reignYears >= 20f && c.vengefulness < 0f && rec.threatsResolved >= 3)
			{
				if (!GrantEpithet(c, "忍耐者"))
				{
					return "";
				}
				return "忍耐者";
			}
			if (c.compassion > 60f && rec.warsWon <= 2)
			{
				if (!GrantEpithet(c, "仁慈者"))
				{
					return "";
				}
				return "仁慈者";
			}
			if (rec.rebellions == 0 && c.compassion >= 40f && rec.reignYears >= 15f)
			{
				if (!GrantEpithet(c, "受爱戴者"))
				{
					return "";
				}
				return "受爱戴者";
			}
			if (rec.rebellions >= 3 && c.compassion < 0f)
			{
				if (!GrantEpithet(c, "被憎恨者"))
				{
					return "";
				}
				return "被憎恨者";
			}
			if (c.compassion < -60f && (rec.warsWon >= 3 || rec.threatsResolved >= 2))
			{
				if (!GrantEpithet(c, "无情者"))
				{
					return "";
				}
				return "无情者";
			}
			if (!rec.famineUnderRule && rec.reignYears >= 15f && rec.rebellions == 0)
			{
				if (!GrantEpithet(c, "公正者"))
				{
					return "";
				}
				return "公正者";
			}
			if (c.boldness < -40f && rec.defeatedBattles == 0 && rec.warsWon >= 1)
			{
				if (!GrantEpithet(c, "谨慎者"))
				{
					return "";
				}
				return "谨慎者";
			}
			if (c.boldness > 50f && rec.warsWon >= 3)
			{
				if (!GrantEpithet(c, "勇敢者"))
				{
					return "";
				}
				return "勇敢者";
			}
			if (rec.expeditions >= 4)
			{
				if (!GrantEpithet(c, "远行者"))
				{
					return "";
				}
				return "远行者";
			}
			if (rec.lostAllLands > 0 && rec.conquests == 0)
			{
				if (!GrantEpithet(c, "无地者"))
				{
					return "";
				}
				return "无地者";
			}
			if (rec.conquests >= 4 && c.compassion < -40f)
			{
				if (!GrantEpithet(c, "狼"))
				{
					return "";
				}
				return "狼";
			}
		}
		if (string.IsNullOrEmpty(c.epithet))
		{
			if (rec.massacres >= 1)
			{
				if (!GrantEpithet(c, "屠夫"))
				{
					return "";
				}
				return "屠夫";
			}
			if (c.obesity > 70f)
			{
				if (!GrantEpithet(c, "胖子"))
				{
					return "";
				}
				return "胖子";
			}
			if (rec.usurpedThrone)
			{
				if (!GrantEpithet(c, "篡位者"))
				{
					return "";
				}
				return "篡位者";
			}
			if (c.compassion < -60f && (rec.threatsResolved >= 3 || rec.warsWon >= 5 || rec.massacres >= 1) && rec.reignYears >= 10f)
			{
				if (!GrantEpithet(c, "恐怖者"))
				{
					return "";
				}
				return "恐怖者";
			}
			if (rec.reignYears >= 20f && rec.warsWon == 0 && rec.conquests == 0)
			{
				if (!GrantEpithet(c, "和平者"))
				{
					return "";
				}
				return "和平者";
			}
			if (rec.cultureActs >= 10)
			{
				if (!GrantEpithet(c, "学者"))
				{
					return "";
				}
				return "学者";
			}
			if (c.greed < -40f && c.compassion >= 30f)
			{
				if (!GrantEpithet(c, "宽宏者"))
				{
					return "";
				}
				return "宽宏者";
			}
			if (c.greed > 60f)
			{
				if (!GrantEpithet(c, "吝啬鬼"))
				{
					return "";
				}
				return "吝啬鬼";
			}
			if (rec.expeditions >= 4 && rec.defeatedBattles == 0 && !rec.famineUnderRule)
			{
				if (!GrantEpithet(c, "幸运者"))
				{
					return "";
				}
				return "幸运者";
			}
			if (rec.defensiveWins >= 3)
			{
				if (!GrantEpithet(c, "铁锤"))
				{
					return "";
				}
				return "铁锤";
			}
			if (rec.religionActs >= 10)
			{
				if (!GrantEpithet(c, "传道者"))
				{
					return "";
				}
				return "传道者";
			}
			if (rec.reignYears >= 15f && rec.warsWon == 0 && rec.conquests == 0 && rec.cultureActs == 0 && rec.religionActs == 0)
			{
				if (!GrantEpithet(c, "懒王"))
				{
					return "";
				}
				return "懒王";
			}
			if (EvaluationSystem.CalculateScore(rec) >= 350f && c.compassion >= 30f && c.honor >= 30f && !rec.famineUnderRule && !rec.usurpedThrone)
			{
				if (!GrantEpithet(c, "好人"))
				{
					return "";
				}
				return "好人";
			}
			if (rec.canonized)
			{
				if (!GrantEpithet(c, "圣者"))
				{
					return "";
				}
				return "圣者";
			}
		}
		if (string.IsNullOrEmpty(c.epithet) && c.bodyMarks != null)
		{
			foreach (string bodyMark in c.bodyMarks)
			{
				string text = null;
				switch (bodyMark)
				{
				case "秃顶":
					text = "秃头";
					break;
				case "跛足":
					text = "瘸子";
					break;
				case "失明":
					text = "瞎子";
					break;
				case "矮小":
					text = "矮子";
					break;
				case "红须":
					text = "红胡子";
					break;
				case "黑色":
					if (flag)
					{
						text = "黑王";
					}
					else if (HasPrinceRole(c))
					{
						text = "黑王子";
					}
					else
					{
						text = ((c.role == CharacterRole.Military) ? "黑骑士" : null);
					}
					break;
				case "白色":
					if (flag)
					{
						text = "白王";
					}
					else if (HasPrinceRole(c))
					{
						text = "白王子";
					}
					else
					{
						text = ((c.role == CharacterRole.Military) ? "白骑士" : null);
					}
					break;
				case "金发":
					text = "美发者";
					break;
				case "白发":
					text = "白发者";
					break;
				case "黑发":
					text = "黑发者";
					break;
				}
				if (text != null)
				{
					return GrantEpithet(c, text) ? text : "";
				}
			}
		}
		return c.epithet;
	}

	private static bool HasBodyMark(CharacterData c, string mark)
	{
		if (c.bodyMarks == null)
		{
			return false;
		}
		return c.bodyMarks.Contains(mark);
	}

	private static bool HasTraitLevel(CharacterData c, string baseId, int minLevel)
	{
		if (c.traits == null)
		{
			return false;
		}
		foreach (PersonalityTrait trait in c.traits)
		{
			if ((trait.traitId == baseId + "_" + minLevel || trait.traitId == baseId + "_3") && (trait.traitId.EndsWith("_" + minLevel) || (minLevel == 2 && trait.traitId.EndsWith("_3"))))
			{
				return true;
			}
		}
		return false;
	}

	private static bool HasTraitAny(CharacterData c, string baseId)
	{
		if (c.traits == null)
		{
			return false;
		}
		foreach (PersonalityTrait trait in c.traits)
		{
			if (trait.traitId.StartsWith(baseId + "_"))
			{
				return true;
			}
		}
		return false;
	}

	private static bool HasPrinceRole(CharacterData c)
	{
		if (c.role != CharacterRole.Heir)
		{
			return c.role == CharacterRole.Noble;
		}
		return true;
	}

	public static string DeterminePosthumousTitle(CharacterData c, int warsWon, int conquests, bool famineUnderRule)
	{
		if (c == null)
		{
			return "";
		}
		if (c.greed < -50f && c.compassion < -30f)
		{
			return "厉";
		}
		if (famineUnderRule)
		{
			return "荒";
		}
		if (c.vengefulness > 60f && c.compassion < -40f)
		{
			return "暴";
		}
		if (conquests >= 3 || warsWon >= 5)
		{
			if (!(c.compassion >= 0f))
			{
				return "烈";
			}
			return "武";
		}
		if (c.boldness >= 0f && (conquests > 0 || warsWon > 0))
		{
			return "武";
		}
		if (c.compassion > 50f)
		{
			return "仁";
		}
		if (c.rationality > 50f)
		{
			return "明";
		}
		return "平";
	}
}
}
