using System;
using System.Collections.Generic;
using System.Linq;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
public class FactionManager
{
	private struct StanceEnergy
	{
		public float totalPower;

		public Dictionary<GameEnums.SocialClass, float> backing;

		public GameEnums.SocialClass primaryClass;
	}

	private readonly Dictionary<int, List<Faction>> _factionsByRealm = new Dictionary<int, List<Faction>>();

	private int _nextFactionId = 1;

	private const float SpawnPowerThreshold = 8f;

	private const float DissolvePowerThreshold = 3f;

	private const float SignificantClassShare = 0.05f;

	private static readonly List<Faction> EmptyList = new List<Faction>();

	public IReadOnlyList<Faction> GetFactions(int realmId)
	{
		if (!_factionsByRealm.TryGetValue(realmId, out var value))
		{
			return EmptyList;
		}
		return value;
	}

	public Faction GetFactionByStance(int realmId, FactionStance stance)
	{
		if (!_factionsByRealm.TryGetValue(realmId, out var value))
		{
			return null;
		}
		return value.FirstOrDefault((Faction f) => f.stance == stance);
	}

	public void UpdateRealmFactions(RealmSociety society, RealmData realm, List<CharacterData> characters = null)
	{
		int realmId = society.realmId;
		if (!_factionsByRealm.TryGetValue(realmId, out var value))
		{
			value = new List<Faction>();
			_factionsByRealm[realmId] = value;
		}
		Dictionary<FactionStance, StanceEnergy> dictionary = ComputeStanceEnergy(society);
		foreach (FactionStance stance in Enum.GetValues(typeof(FactionStance)))
		{
			StanceEnergy energy = dictionary[stance];
			Faction faction = value.FirstOrDefault((Faction f) => f.stance == stance);
			if (faction == null)
			{
				if (energy.totalPower < 8f)
				{
					continue;
				}
				faction = new Faction(_nextFactionId++, realmId, stance)
				{
					factionName = DefaultName(stance),
					isInGovernment = (stance == FactionStance.Conservative)
				};
				value.Add(faction);
			}
			faction.classBacking = energy.backing;
			faction.primaryClass = energy.primaryClass;
			faction.platform = BlendPlatform(faction.classBacking, society, stance);
			MatchCharacters(faction, society, characters);
			faction.power = ComputePower(faction, energy, characters);
		}
		for (int num = value.Count - 1; num >= 0; num--)
		{
			Faction faction2 = value[num];
			faction2.cohesion = Mathf.Clamp(faction2.cohesion + ((faction2.power > 20f) ? 0.05f : (-0.1f)), 0f, 100f);
			if (faction2.power < 3f && faction2.cohesion < 10f)
			{
				value.RemoveAt(num);
			}
		}
	}

	private Dictionary<FactionStance, StanceEnergy> ComputeStanceEnergy(RealmSociety society)
	{
		Dictionary<FactionStance, StanceEnergy> dictionary = new Dictionary<FactionStance, StanceEnergy>();
		foreach (FactionStance value2 in Enum.GetValues(typeof(FactionStance)))
		{
			Dictionary<GameEnums.SocialClass, float> dictionary2 = new Dictionary<GameEnums.SocialClass, float>();
			float num = 0f;
			GameEnums.SocialClass primaryClass = GameEnums.SocialClass.Peasant;
			float num2 = -1f;
			foreach (KeyValuePair<GameEnums.SocialClass, ClassProfile> @class in society.classes)
			{
				GameEnums.SocialClass key = @class.Key;
				ClassProfile value = @class.Value;
				if (!(value.populationShare < 0.05f) || key == GameEnums.SocialClass.Royalty || key == GameEnums.SocialClass.NobilityClergy)
				{
					float num3 = (dictionary2[key] = Mathf.Clamp01(value2 switch
					{
						FactionStance.Conservative => value.support / Mathf.Max(1f, value.influence), 
						FactionStance.Reformist => GrievanceBand(value, 0.15f, 0.55f) * (0.5f + value.organization), 
						FactionStance.Radical => GrievanceBand(value, 0.45f, 1f) * (1.2f - Mathf.Clamp01((value.needReport != null) ? (value.needReport.dimensions.GetValueOrDefault(ClassNeedDimension.PoliticalAccess).score / 100f) : 0.5f)), 
						FactionStance.Reactionary => (key == GameEnums.SocialClass.NobilityClergy || key == GameEnums.SocialClass.Royalty) ? (GrievanceBand(value, 0.3f, 1f) * 0.8f) : (GrievanceBand(value, 0.5f, 1f) * 0.2f), 
						_ => 0f, 
					}));
					float num5 = value.influence * num3;
					num += num5;
					if (num5 > num2)
					{
						num2 = num5;
						primaryClass = key;
					}
				}
			}
			dictionary[value2] = new StanceEnergy
			{
				totalPower = Mathf.Clamp(num, 0f, 100f),
				backing = dictionary2,
				primaryClass = primaryClass
			};
		}
		return dictionary;
	}

	private static float GrievanceBand(ClassProfile p, float lo, float hi)
	{
		float num = Mathf.Clamp01((100f - p.satisfaction) / 100f);
		if (num <= lo || num >= hi)
		{
			return Mathf.Max(0f, 0.15f - Mathf.Min(Mathf.Abs(num - lo), Mathf.Abs(num - hi))) / 0.15f * 0.3f;
		}
		return 1f;
	}

	private static FactionPlatform BlendPlatform(Dictionary<GameEnums.SocialClass, float> backing, RealmSociety society, FactionStance stance)
	{
		Dictionary<GameEnums.SocialClass, FactionPlatform> dictionary = new Dictionary<GameEnums.SocialClass, FactionPlatform>
		{
			[GameEnums.SocialClass.Royalty] = new FactionPlatform
			{
				openness = -0.6f,
				centralization = 0.8f,
				commerce = 0.1f,
				taxRelief = -0.2f
			},
			[GameEnums.SocialClass.NobilityClergy] = new FactionPlatform
			{
				openness = -0.3f,
				centralization = -0.5f,
				commerce = -0.1f,
				taxRelief = 0.4f
			},
			[GameEnums.SocialClass.MerchantFreeman] = new FactionPlatform
			{
				openness = 0.7f,
				centralization = 0.2f,
				commerce = 0.8f,
				taxRelief = 0.5f
			},
			[GameEnums.SocialClass.Peasant] = new FactionPlatform
			{
				openness = 0f,
				centralization = 0.1f,
				commerce = -0.3f,
				taxRelief = 0.8f
			},
			[GameEnums.SocialClass.Slave] = new FactionPlatform
			{
				openness = 0.2f,
				centralization = 0f,
				commerce = 0f,
				taxRelief = 0f
			}
		};
		FactionPlatform factionPlatform = default;
		float num = 0f;
		foreach (KeyValuePair<GameEnums.SocialClass, float> item in backing)
		{
			ClassProfile valueOrDefault = society.classes.GetValueOrDefault(item.Key);
			float num2 = ((valueOrDefault != null) ? (item.Value * valueOrDefault.influence) : 0f);
			if (!(num2 <= 0f) && dictionary.TryGetValue(item.Key, out var value))
			{
				factionPlatform += value.Scaled(num2);
				num += num2;
			}
		}
		if (num > 0f)
		{
			factionPlatform = factionPlatform.Scaled(1f / num);
		}
		factionPlatform = factionPlatform.Scaled(stance switch
		{
			FactionStance.Conservative => 0.2f, 
			FactionStance.Reformist => 0.8f, 
			FactionStance.Radical => 1.3f, 
			FactionStance.Reactionary => -0.9f, 
			_ => 1f, 
		});
		factionPlatform.Clamp();
		return factionPlatform;
	}

	private void MatchCharacters(Faction faction, RealmSociety society, List<CharacterData> characters)
	{
		faction.memberCharacterIds.Clear();
		if (characters == null)
		{
			faction.leaderCharacterId = -1;
			return;
		}
		CharacterData characterData = null;
		float num = -1f;
		foreach (CharacterData character in characters)
		{
			if (character == null || !character.isAlive)
			{
				continue;
			}
			float valueOrDefault = faction.classBacking.GetValueOrDefault(character.socialClass, 0f);
			if (!(valueOrDefault < 0.2f))
			{
				faction.memberCharacterIds.Add(character.characterId);
				character.GetEffectiveStats(out _, out float effSocial, out _, out float effManagement, out float effConspiracy, out _, out _);
				float num2 = character.prestige * 0.5f + effSocial * 0.2f + effConspiracy * 0.15f + effManagement * 0.15f + (character.greed + 100f) * 0.05f + (character.boldness + 100f) * 0.05f + valueOrDefault * 10f;
				if (num2 > num)
				{
					num = num2;
					characterData = character;
				}
			}
		}
		faction.leaderCharacterId = characterData?.characterId ?? (-1);
	}

	private static float ComputePower(Faction faction, StanceEnergy energy, List<CharacterData> characters)
	{
		float totalPower = energy.totalPower;
		float num = 0.5f + faction.cohesion / 200f;
		float num2 = totalPower * num;
		if (faction.leaderCharacterId >= 0 && characters != null)
		{
			CharacterData characterData = characters.FirstOrDefault((CharacterData c) => c != null && c.characterId == faction.leaderCharacterId);
			if (characterData != null)
			{
				characterData.GetEffectiveStats(out _, out float effSocial2, out _, out float effManagement2, out float effConspiracy2, out _, out _);
				float num3 = characterData.prestige / 1500f * 0.15f + (effSocial2 + effConspiracy2 + effManagement2) / 300f * 0.1f;
				num2 *= 1f + Mathf.Clamp(num3, 0f, 0.25f);
			}
		}
		return Mathf.Clamp(num2, 0f, 100f);
	}

	public Faction GetDominantFaction(int realmId)
	{
		if (!_factionsByRealm.TryGetValue(realmId, out var value) || value.Count == 0)
		{
			return null;
		}
		Faction faction = null;
		foreach (Faction item in value)
		{
			if (faction == null || item.power > faction.power)
			{
				faction = item;
			}
		}
		return faction;
	}

	public void GetChangeVsStatusQuo(int realmId, out float changePower, out float statusQuoPower)
	{
		changePower = 0f;
		statusQuoPower = 0f;
		if (!_factionsByRealm.TryGetValue(realmId, out var value))
		{
			return;
		}
		foreach (Faction item in value)
		{
			if (item.stance == FactionStance.Conservative)
			{
				statusQuoPower += item.power;
			}
			else
			{
				changePower += item.power;
			}
		}
	}

	private static string DefaultName(FactionStance stance)
	{
		return stance switch
		{
			FactionStance.Conservative => "保守派", 
			FactionStance.Reformist => "改革派", 
			FactionStance.Radical => "激进派", 
			FactionStance.Reactionary => "复辟派", 
			_ => "派系", 
		};
	}
}
}
