using System;
using UnityEngine;

namespace MyriadCreation.Content.Definitions
{
[Serializable]
public class BasicAbilities
{
	[Range(0f, 100f)]
	public float strength = 50f;

	[Range(0f, 100f)]
	public float coordination = 50f;

	[Range(0f, 100f)]
	public float endurance = 50f;

	[Range(0f, 100f)]
	public float memory = 50f;

	[Range(0f, 100f)]
	public float logic = 50f;

	[Range(0f, 100f)]
	public float inspiration = 50f;

	[Range(0f, 100f)]
	public float eloquence = 50f;

	[Range(0f, 100f)]
	public float insight = 50f;

	[Range(0f, 100f)]
	public float charisma = 50f;

	[Range(0f, 100f)]
	public float observation = 50f;

	[Range(0f, 100f)]
	public float intuition = 50f;

	[Range(0f, 100f)]
	public float reaction = 50f;

	public float Physique => (strength + coordination + endurance) / 3f;

	public float Mind => (memory + logic + inspiration) / 3f;

	public float Diplomacy => (eloquence + insight + charisma) / 3f;

	public float Perception => (observation + intuition + reaction) / 3f;

	public float Overall => (Physique + Mind + Diplomacy + Perception) / 4f;

	public static BasicAbilities Random(float baseValue = 50f, float range = 30f)
	{
		return new BasicAbilities
		{
			strength = Mathf.Clamp(baseValue + UnityEngine.Random.Range(0f - range, range), 5f, 95f),
			coordination = Mathf.Clamp(baseValue + UnityEngine.Random.Range(0f - range, range), 5f, 95f),
			endurance = Mathf.Clamp(baseValue + UnityEngine.Random.Range(0f - range, range), 5f, 95f),
			memory = Mathf.Clamp(baseValue + UnityEngine.Random.Range(0f - range, range), 5f, 95f),
			logic = Mathf.Clamp(baseValue + UnityEngine.Random.Range(0f - range, range), 5f, 95f),
			inspiration = Mathf.Clamp(baseValue + UnityEngine.Random.Range(0f - range, range), 5f, 95f),
			eloquence = Mathf.Clamp(baseValue + UnityEngine.Random.Range(0f - range, range), 5f, 95f),
			insight = Mathf.Clamp(baseValue + UnityEngine.Random.Range(0f - range, range), 5f, 95f),
			charisma = Mathf.Clamp(baseValue + UnityEngine.Random.Range(0f - range, range), 5f, 95f),
			observation = Mathf.Clamp(baseValue + UnityEngine.Random.Range(0f - range, range), 5f, 95f),
			intuition = Mathf.Clamp(baseValue + UnityEngine.Random.Range(0f - range, range), 5f, 95f),
			reaction = Mathf.Clamp(baseValue + UnityEngine.Random.Range(0f - range, range), 5f, 95f)
		};
	}
}
}
