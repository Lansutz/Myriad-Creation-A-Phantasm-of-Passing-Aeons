using System;
using System.Collections.Generic;

namespace MyriadCreation.Simulation.AI
{
[Serializable]
public class AIState
{
	public int realmId;

	public float aggression = 0.5f;

	public float expansionism = 0.5f;

	public float diplomacyFocus = 0.5f;

	public float economyFocus = 0.5f;

	public float militaryFocus = 0.5f;

	public List<int> rivals = new List<int>();

	public List<int> allies = new List<int>();

	public List<int> threatenedBy = new List<int>();

	public AIStance currentStance;

	public int targetRealmId = -1;

	public float warReadiness;

	public float stabilityConcern;
}
}
