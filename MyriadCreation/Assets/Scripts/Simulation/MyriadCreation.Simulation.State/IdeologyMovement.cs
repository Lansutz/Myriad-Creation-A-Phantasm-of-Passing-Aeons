using System;
using System.Collections.Generic;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using Random = UnityEngine.Random;

namespace MyriadCreation.Simulation.State
{
public class IdeologyMovement
{
	public int movementId;

	public string movementName;

	public string description;

	public int originRegionId;

	public int startYear;

	public int peakYear = -1;

	public int endYear = -1;

	public MovementPhase phase;

	public List<string> coreTenets = new List<string>();

	[NonSerialized]
	public Dictionary<string, float> policyPositions = new Dictionary<string, float>();

	public float momentum;

	public float radicalism = 0.5f;

	public float appeal = 0.5f;

	public List<int> supporterCharacterIds = new List<int>();

	[NonSerialized]
	public Dictionary<int, float> regionSupport = new Dictionary<int, float>();

	public List<int> associatedSchoolIds = new List<int>();

	public List<int> associatedFaithIds = new List<int>();

	public void DailyTick(int currentYear)
	{
		int num = currentYear - startYear;
		if (phase == MovementPhase.Emerging && num > 5)
		{
			phase = MovementPhase.Growing;
		}
		if (phase == MovementPhase.Growing && momentum > 70f)
		{
			phase = MovementPhase.Peak;
			peakYear = currentYear;
		}
		if (phase == MovementPhase.Peak && num > 20)
		{
			phase = MovementPhase.Declining;
		}
		if (phase == MovementPhase.Declining && momentum < 10f)
		{
			phase = MovementPhase.Extinct;
			endYear = currentYear;
		}
		float num2 = phase switch
		{
			MovementPhase.Emerging => 0.5f, 
			MovementPhase.Growing => 2f, 
			MovementPhase.Peak => 0f, 
			MovementPhase.Declining => -1.5f, 
			MovementPhase.Extinct => -0.5f, 
			_ => 0f, 
		};
		momentum = Mathf.Clamp(momentum + num2 + Random.Range(-1f, 1f), 0f, 100f);
	}
}
}
