using System.Collections.Generic;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.State
{
public class PersonalityTrait
{
	public string traitId;

	public string traitName;

	public string description;

	public TraitTier tier;

	public TraitCategory category;

	public float martialMod;

	public float diplomacyMod;

	public float stewardshipMod;

	public float intrigueMod;

	public float learningMod;

	public float warfareMod;

	public float charmMod;

	public List<string> conflictingTraits = new List<string>();

	public List<string> requiredTraits = new List<string>();

	public virtual void OnAcquired(CharacterData character)
	{
	}

	public virtual void OnRemoved(CharacterData character)
	{
	}

	public virtual void ApplyDailyEffect(CharacterData character)
	{
	}

	public bool IsCompatibleWith(CharacterData character)
	{
		foreach (PersonalityTrait trait in character.traits)
		{
			if (conflictingTraits.Contains(trait.traitId))
			{
				return false;
			}
		}
		return true;
	}
}
}
