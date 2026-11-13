using System;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public class ActiveCharacterDisease
{
	public string diseaseId;

	public DiseaseStage stage;

	public int elapsedDays;

	public int stageDays;

	public float severity;

	public bool isTreated;

	public int treatmentDays;

	public float infectionSource;

	public ActiveCharacterDisease()
	{
		diseaseId = "";
		stage = DiseaseStage.Incubation;
		elapsedDays = 0;
		stageDays = 0;
		severity = 0.5f;
		isTreated = false;
		treatmentDays = 0;
		infectionSource = 0f;
	}

	public ActiveCharacterDisease(string id, float source = 0f)
	{
		diseaseId = id;
		stage = DiseaseStage.Incubation;
		elapsedDays = 0;
		stageDays = 0;
		severity = 0.5f;
		isTreated = false;
		treatmentDays = 0;
		infectionSource = source;
	}
}
}
