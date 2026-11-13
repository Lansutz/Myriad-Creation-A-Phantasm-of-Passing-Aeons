using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public class SchoolOfThought
{
	public int schoolId;

	public string schoolName;

	public string description;

	public int founderCharacterId = -1;

	public int foundingYear;

	public List<string> coreTexts = new List<string>();

	public List<string> coreConcepts = new List<string>();

	public float metaphysicsWeight = 0.5f;

	public float ethicsWeight = 0.5f;

	public float politicsWeight = 0.5f;

	public float naturalPhilosophyWeight = 0.5f;

	public float authoritarianism = 0.5f;

	public float traditionalism = 0.5f;

	public float collectivism = 0.5f;

	public float spreadPower = 1f;

	public float conversionRate = 0.01f;

	public List<int> followerCharacterIds = new List<int>();

	[NonSerialized]
	public Dictionary<int, float> regionPenetration = new Dictionary<int, float>();

	[NonSerialized]
	public Dictionary<int, float> schoolRelations = new Dictionary<int, float>();

	public float CalculateInfluence()
	{
		float num = 0f;
		foreach (KeyValuePair<int, float> item in regionPenetration)
		{
			num += item.Value;
		}
		return (float)followerCharacterIds.Count * 0.3f + num * 0.7f;
	}

	public void DailyTick()
	{
		spreadPower = Mathf.Lerp(spreadPower, 1f, 0.001f);
	}
}
}
