using System;
using UnityEngine;

namespace MyriadCreation.Content.Definitions
{
[Serializable]
public class ActiveJuncture
{
	public CriticalJunctureType type;

	public int startDay;

	public int remainingDays;

	[Range(0f, 100f)]
	public float severity;

	public bool resolved;

	public JunctureOutcomeType outcome;

	public string note = "";
}
}
