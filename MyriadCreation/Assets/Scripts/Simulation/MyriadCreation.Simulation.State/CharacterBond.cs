using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.State
{
public class CharacterBond
{
	public int bondId;

	public int characterAId;

	public int characterBId;

	public BondType type;

	public int establishedDay;

	public float strength = 50f;

	public float combatBonusWhenTogether;

	public float diplomacyBonus;

	public float stressReduction;

	public bool sharedFate;

	public void DailyTick()
	{
		strength = Mathf.Clamp(strength + Random.Range(-1f, 1f), 0f, 100f);
	}
}
}
