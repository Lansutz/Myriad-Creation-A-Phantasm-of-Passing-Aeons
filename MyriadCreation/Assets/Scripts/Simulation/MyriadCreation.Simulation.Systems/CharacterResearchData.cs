using System;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
[Serializable]
public class CharacterResearchData
{
	public int characterId;

	public int currentResearchInnovationId = -1;

	public InnovationField specialty = InnovationField.Craft;

	public float researchAbility = 3f;

	public float inspiration;

	public bool isInspired;

	public int inspirationRemainingDays;

	public float totalResearchContribution;

	public int completedInnovations;

	public float ResearchEfficiency
	{
		get
		{
			float num = 0.5f + researchAbility * 0.15f;
			if (isInspired)
			{
				num *= 2f;
			}
			return num;
		}
	}

	public CharacterResearchData(int characterId)
	{
		this.characterId = characterId;
	}

	public bool StartResearch(int innovationId, InnovationField field)
	{
		if (currentResearchInnovationId >= 0)
		{
			return false;
		}
		currentResearchInnovationId = innovationId;
		if (field == specialty)
		{
			inspiration = Mathf.Min(100f, inspiration + 20f);
		}
		return true;
	}

	public void CompleteResearch()
	{
		completedInnovations++;
		totalResearchContribution = 0f;
		currentResearchInnovationId = -1;
		inspiration = Mathf.Min(100f, inspiration + 30f);
	}

	public void DailyTick()
	{
		inspiration = Mathf.Max(0f, inspiration - 0.1f);
		if (isInspired)
		{
			inspirationRemainingDays--;
			if (inspirationRemainingDays <= 0)
			{
				isInspired = false;
				inspirationRemainingDays = 0;
			}
		}
		if (inspiration >= 100f && !isInspired)
		{
			isInspired = true;
			inspirationRemainingDays = 30;
			inspiration = 0f;
		}
	}

	public void PracticeOldMethod(float amount)
	{
		inspiration = Mathf.Min(100f, inspiration + amount * 0.5f);
	}
}
}
