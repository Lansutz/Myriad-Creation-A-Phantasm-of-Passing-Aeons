using System;
using System.Collections.Generic;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using Random = UnityEngine.Random;

namespace MyriadCreation.Simulation.AI
{
[Serializable]
public struct AIPersonality
{
	public string personalityName;

	[Range(0f, 1f)]
	public float expansionBias;

	[Range(0f, 1f)]
	public float economicBias;

	[Range(0f, 1f)]
	public float diplomaticBias;

	[Range(0f, 1f)]
	public float militaryBias;

	[Range(0f, 1f)]
	public float aggression;

	[Range(0f, 1f)]
	public float riskTolerance;

	[Range(0f, 2f)]
	public float researchMultiplier;

	public List<InnovationDomain> preferredDomains;

	public bool fixedArchetype;

	private static string ClassifyName(AIPersonality p)
	{
		float[] array = new float[5] { p.expansionBias, p.economicBias, p.diplomaticBias, p.militaryBias, p.aggression };
		int num = 0;
		for (int i = 1; i < array.Length; i++)
		{
			if (array[i] > array[num])
			{
				num = i;
			}
		}
		return (new string[5] { "征服王", "建设王", "外交家", "军事统帅", "侵略者" })[num];
	}

	public static AIPersonality Preset(string archetypeId, bool fixedArchetype = false)
	{
		return archetypeId switch
		{
			"conqueror" => new AIPersonality
			{
				personalityName = "征服王",
				expansionBias = 0.9f,
				economicBias = 0.35f,
				diplomaticBias = 0.3f,
				militaryBias = 0.85f,
				aggression = 0.8f,
				riskTolerance = 0.7f,
				researchMultiplier = 1f,
				fixedArchetype = fixedArchetype
			}, 
			"adventurer" => new AIPersonality
			{
				personalityName = "冒险王",
				expansionBias = 0.7f,
				economicBias = 0.4f,
				diplomaticBias = 0.4f,
				militaryBias = 0.8f,
				aggression = 0.6f,
				riskTolerance = 0.95f,
				researchMultiplier = 0.9f,
				fixedArchetype = fixedArchetype
			}, 
			"builder" => new AIPersonality
			{
				personalityName = "建设者",
				expansionBias = 0.3f,
				economicBias = 0.95f,
				diplomaticBias = 0.6f,
				militaryBias = 0.35f,
				aggression = 0.2f,
				riskTolerance = 0.35f,
				researchMultiplier = 1.4f,
				fixedArchetype = fixedArchetype
			}, 
			"diplomat" => new AIPersonality
			{
				personalityName = "外交家",
				expansionBias = 0.4f,
				economicBias = 0.5f,
				diplomaticBias = 0.95f,
				militaryBias = 0.3f,
				aggression = 0.15f,
				riskTolerance = 0.4f,
				researchMultiplier = 1f,
				fixedArchetype = fixedArchetype
			}, 
			_ => RandomPersonality(), 
		};
	}

	public static AIPersonality RandomPersonality()
	{
		AIPersonality result = new AIPersonality
		{
			personalityName = "随机",
			expansionBias = Random.Range(0.2f, 0.8f),
			economicBias = Random.Range(0.2f, 0.8f),
			diplomaticBias = Random.Range(0.2f, 0.8f),
			militaryBias = Random.Range(0.2f, 0.8f),
			aggression = Random.Range(0.2f, 0.8f),
			riskTolerance = Random.Range(0.2f, 0.8f),
			researchMultiplier = Random.Range(0.8f, 1.5f),
			preferredDomains = new List<InnovationDomain>()
		};
		Array values = Enum.GetValues(typeof(InnovationDomain));
		int num = Random.Range(0, values.Length);
		int num2 = Random.Range(0, values.Length);
		result.preferredDomains.Add((InnovationDomain)values.GetValue(num));
		if (num2 != num)
		{
			result.preferredDomains.Add((InnovationDomain)values.GetValue(num2));
		}
		return result;
	}
}
}
