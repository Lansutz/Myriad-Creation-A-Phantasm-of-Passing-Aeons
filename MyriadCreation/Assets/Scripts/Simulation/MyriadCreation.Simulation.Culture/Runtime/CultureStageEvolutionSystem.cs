using System.Collections.Generic;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Simulation.Culture
{
public static class CultureStageEvolutionSystem
{
	public const int POP_BAND_TO_TRIBE = 100;

	public const int POP_TRIBE_TO_CHIEFDOM = 1000;

	public const int POP_CHIEFDOM_TO_ETHNIC = 5000;

	public const int POP_ETHNIC_TO_CIVILIZATION = 10000;

	public static StageEvolutionResult EvaluateEvolution(GameEnums.CultureStage currentStage, int population, HashSet<int> ownedInnovations, float socialStratification, float specialization, bool hasUrbanCenter)
	{
		StageEvolutionResult result = new StageEvolutionResult
		{
			targetStage = currentStage + 1,
			unmetConditions = new List<string>()
		};
		ownedInnovations = ownedInnovations ?? new HashSet<int>();
		// 阶段阈值/条件优先读内容目录（StreamingAssets/Base/Culture/CultureStages.json）；
		// JSON 缺失/损坏时目录回退代码默认（与旧硬编码常量一致），保证行为不变。
		CultureStageCatalog.EnsureLoaded();
		int num = 0;
		int num2 = 0;
		int nextPopulationThreshold = -1;
		switch (currentStage)
		{
		case GameEnums.CultureStage.Band:
			num2 = 2;
			nextPopulationThreshold = CultureStageCatalog.GetNextPopulationThreshold(GameEnums.CultureStage.Band);
			if (population >= nextPopulationThreshold)
			{
				num++;
			}
			else
			{
				result.unmetConditions.Add($"人口 {population}/{nextPopulationThreshold}");
			}
			if (ownedInnovations.Contains(900))
			{
				num++;
			}
			else
			{
				result.unmetConditions.Add("需要掌握用火");
			}
			break;
		case GameEnums.CultureStage.Tribe:
			num2 = 4;
			nextPopulationThreshold = CultureStageCatalog.GetNextPopulationThreshold(GameEnums.CultureStage.Tribe);
			if (population >= nextPopulationThreshold)
			{
				num++;
			}
			else
			{
				result.unmetConditions.Add($"人口 {population}/{nextPopulationThreshold}");
			}
			if (ownedInnovations.Contains(100) || HasAnyInnovation(ownedInnovations, 916, 917, 918) || HasAnimalDomestication(ownedInnovations))
			{
				num++;
			}
			else
			{
				result.unmetConditions.Add("需要农业或畜牧技术");
			}
			if (HasAnyInnovation(ownedInnovations, 200, 201))
			{
				num++;
			}
			else
			{
				result.unmetConditions.Add("需要陶器制作");
			}
			if (socialStratification >= CultureStageCatalog.GetRequiredSocialStratification(GameEnums.CultureStage.Tribe))
			{
				num++;
			}
			else
			{
				result.unmetConditions.Add("社会需要初步分层（贵族/平民萌芽）");
			}
			break;
		case GameEnums.CultureStage.Chiefdom:
			num2 = 4;
			nextPopulationThreshold = CultureStageCatalog.GetNextPopulationThreshold(GameEnums.CultureStage.Chiefdom);
			if (population >= nextPopulationThreshold)
			{
				num++;
			}
			else
			{
				result.unmetConditions.Add($"人口 {population}/{nextPopulationThreshold}");
			}
			if (HasAnyInnovation(ownedInnovations, 1101, 202, 1105, 201))
			{
				num++;
			}
			else
			{
				result.unmetConditions.Add("需要金属冶炼技术");
			}
			if (socialStratification >= CultureStageCatalog.GetRequiredSocialStratification(GameEnums.CultureStage.Chiefdom))
			{
				num++;
			}
			else
			{
				result.unmetConditions.Add("需要完整社会分层（贵族/平民/奴隶）");
			}
			if (specialization >= CultureStageCatalog.GetRequiredSpecialization(GameEnums.CultureStage.Chiefdom))
			{
				num++;
			}
			else
			{
				result.unmetConditions.Add("需要专业化生产（工匠/商人/军人脱离食物生产）");
			}
			break;
		case GameEnums.CultureStage.EthnicGroup:
			num2 = 4;
			nextPopulationThreshold = CultureStageCatalog.GetNextPopulationThreshold(GameEnums.CultureStage.EthnicGroup);
			if (population >= nextPopulationThreshold)
			{
				num++;
			}
			else
			{
				result.unmetConditions.Add($"人口 {population}/{nextPopulationThreshold}");
			}
			if (HasAnyInnovation(ownedInnovations, 503, 504, 505))
			{
				num++;
			}
			else
			{
				result.unmetConditions.Add("需要官僚制度或贵族议事制度");
			}
			if (hasUrbanCenter || !CultureStageCatalog.RequiresUrbanCenter(GameEnums.CultureStage.EthnicGroup))
			{
				num++;
			}
			else
			{
				result.unmetConditions.Add("需要城市级定居点或固定营地体系");
			}
			if (socialStratification >= CultureStageCatalog.GetRequiredSocialStratification(GameEnums.CultureStage.EthnicGroup) && specialization >= CultureStageCatalog.GetRequiredSpecialization(GameEnums.CultureStage.EthnicGroup))
			{
				num++;
			}
			else
			{
				result.unmetConditions.Add("需要成熟的社会分层与专业化分工");
			}
			break;
		default:
			result.canEvolve = false;
			result.progress = 1f;
			return result;
		}
		result.progress = ((num2 > 0) ? ((float)num / (float)num2) : 0f);
		result.canEvolve = num >= num2;
		return result;
	}

	public static GameEnums.CultureStage CheckRegress(GameEnums.CultureStage currentStage, int population, float socialStratification, bool lostUrbanCenter)
	{
		if (!lostUrbanCenter)
		{
			return currentStage;
		}
		bool flag = currentStage switch
		{
			GameEnums.CultureStage.HighCivilization => (float)population < 5000f, 
			GameEnums.CultureStage.EthnicGroup => (float)population < 2500f, 
			GameEnums.CultureStage.Chiefdom => (float)population < 500f, 
			_ => false, 
		};
		bool flag2 = socialStratification < 0.2f;
		if ((flag & flag2) && currentStage > GameEnums.CultureStage.Band)
		{
			return currentStage - 1;
		}
		return currentStage;
	}

	private static bool HasAnyInnovation(HashSet<int> owned, params int[] ids)
	{
		foreach (int item in ids)
		{
			if (owned.Contains(item))
			{
				return true;
			}
		}
		return false;
	}

	private static bool HasAnimalDomestication(HashSet<int> owned)
	{
		foreach (int item in owned)
		{
			if (item >= 910 && item <= 930)
			{
				return true;
			}
		}
		return false;
	}
}
}
