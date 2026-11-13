using System;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Definitions
{
[Serializable]
public class TitleEligibility
{
	public InheritanceGender? gender;

	public EligibilityScope? scope;

	public bool? royal;

	public string institution;

	public string cultureId;

	public string faithId;

	public string languageId;

	public string regionId;

	public int developmentStage = -1;

	public float scaleMin;

	public bool IsHardPass(TitleCandidateContext candidate)
	{
		if (candidate == null)
		{
			return false;
		}
		if (gender.HasValue)
		{
			if (gender.Value == InheritanceGender.MaleOnly && !candidate.isMale)
			{
				return false;
			}
			if (gender.Value == InheritanceGender.FemaleOnly && candidate.isMale)
			{
				return false;
			}
		}
		if (scope.HasValue && scope.Value != EligibilityScope.ClanOnly && candidate.scope != scope.Value)
		{
			return false;
		}
		if (royal.HasValue && candidate.royal != royal.Value)
		{
			return false;
		}
		if (!string.IsNullOrEmpty(institution) && candidate.institution != institution)
		{
			return false;
		}
		if (!string.IsNullOrEmpty(cultureId) && candidate.cultureId != cultureId)
		{
			return false;
		}
		if (!string.IsNullOrEmpty(faithId) && candidate.faithId != faithId)
		{
			return false;
		}
		if (!string.IsNullOrEmpty(languageId) && candidate.languageId != languageId)
		{
			return false;
		}
		if (!string.IsNullOrEmpty(regionId) && candidate.regionId != regionId)
		{
			return false;
		}
		if (developmentStage >= 0 && candidate.developmentStage < developmentStage)
		{
			return false;
		}
		if (scaleMin > 0f && candidate.scale < scaleMin)
		{
			return false;
		}
		return true;
	}
}
}
