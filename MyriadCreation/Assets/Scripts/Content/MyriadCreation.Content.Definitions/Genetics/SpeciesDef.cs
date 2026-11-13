using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyriadCreation.Content.Definitions
{
[Serializable]
public class SpeciesDef
{
	public string speciesId = "";

	public string name = "";

	public SpeciesCategory category = SpeciesCategory.Animal;

	public string genomeId = "";

	public string parentSpeciesId = "";

	public float bodySize = 1f;

	public float lifespanYears = 20f;

	public float reproductionRate = 1f;

	public float dietHerbivore = 0.5f;

	public float dietCarnivore = 0.5f;

	public float habitatTempMinC = -10f;

	public float habitatTempMaxC = 35f;

	public float habitatHumidityMinPct;

	public float habitatHumidityMaxPct = 100f;

	public List<string> behaviorTags = new List<string>();

	public ReproductiveIsolation reproductiveIsolation;

	[Range(0f, 1f)]
	public float paternalIntrogressionRate = 1f;

	[Range(0f, 1f)]
	public float maternalIntrogressionRate = 1f;

	public bool offspringUsesMotherSpecies = true;

	public string iconKey = "";

	public string modelKey = "";

	public float introgressionRate => Math.Min(paternalIntrogressionRate, maternalIntrogressionRate);
}
}
