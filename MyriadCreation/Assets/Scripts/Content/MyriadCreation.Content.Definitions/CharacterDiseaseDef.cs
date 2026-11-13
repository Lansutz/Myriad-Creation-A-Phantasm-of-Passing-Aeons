using System;

namespace MyriadCreation.Content.Definitions
{
[Serializable]
public struct CharacterDiseaseDef
{
	public string diseaseId;

	public string diseaseName;

	public string description;

	public CharacterDiseaseCategory category;

	public TransmissionType transmission;

	public string causesImpairmentId;

	public float impairmentProgressionRate;

	public int initialImpairmentStage;

	public float baseInfectionRate;

	public float baseMortalityRate;

	public float baseRecoveryRate;

	public int incubationDays;

	public int acuteDurationDays;

	public bool isChronic;

	public bool isPermanent;

	public bool isGenetic;

	public float healthMod;

	public float prowessMod;

	public float socialMod;

	public float militaryMod;

	public float managementMod;

	public float conspiracyMod;

	public float scholarshipMod;

	public float charmMod;

	public float fertilityMod;

	public int minAgeOnset;

	public int maxAgeOnset;

	public float obesityThreshold;

	public float stressThreshold;

	public bool combatInjury;

	public bool accidentInjury;

	public bool treatable;

	public float treatmentRecoveryBonus;

	public string requiredInnovation;
}
}
