using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using UnityEngine;
using MyriadCreation.Simulation.Culture;

namespace MyriadCreation.Simulation.Systems
{
public static class SocialStratificationCalculator
{
	public struct Result
	{
		public float socialStratification;

		public float specialization;

		public Dictionary<GameEnums.SocialClass, float> classPopulation;

		public float totalPopulation;

		public float nonFoodProducerPopulation;
	}

	private const float W_RULER = 0.35f;

	private const float W_FREEMAN = 0.25f;

	private const float W_PEASANT = 0.15f;

	private const float W_SLAVE = 0.25f;

	private const float FULL_SPECIALIZATION_RATIO = 0.18f;

	private const float CLASS_PRESENT_MIN_POP = 5f;

	public static Result Calculate(GameWorld world, int realmId, CultureData culture, InnovationTree innovations)
	{
		Dictionary<GameEnums.SocialClass, float> dictionary = TallyClassPopulation(world, realmId);
		float num = 0f;
		foreach (float value in dictionary.Values)
		{
			num += value;
		}
		Result result = new Result
		{
			classPopulation = dictionary,
			totalPopulation = num
		};
		if (num <= 0f)
		{
			result.socialStratification = 0f;
			result.specialization = 0f;
			return result;
		}
		float num2 = 0f;
		bool num3 = SocialClassAvailability.IsClassAvailable(GameEnums.SocialClass.Royalty, culture, innovations, realmId) || SocialClassAvailability.IsClassAvailable(GameEnums.SocialClass.NobilityClergy, culture, innovations, realmId);
		float num4 = dictionary.GetValueOrDefault(GameEnums.SocialClass.Royalty) + dictionary.GetValueOrDefault(GameEnums.SocialClass.NobilityClergy);
		if (num3 && num4 >= 5f)
		{
			num2 += 0.35f;
		}
		bool flag = SocialClassAvailability.IsClassAvailable(GameEnums.SocialClass.MerchantFreeman, culture, innovations, realmId);
		float valueOrDefault = dictionary.GetValueOrDefault(GameEnums.SocialClass.MerchantFreeman);
		if (flag && valueOrDefault >= 5f)
		{
			num2 += 0.25f;
		}
		bool flag2 = SocialClassAvailability.IsClassAvailable(GameEnums.SocialClass.Peasant, culture, innovations, realmId);
		float valueOrDefault2 = dictionary.GetValueOrDefault(GameEnums.SocialClass.Peasant);
		if (valueOrDefault2 >= 5f || (!flag2 && num >= 5f))
		{
			num2 += 0.15f;
		}
		bool flag3 = SocialClassAvailability.IsClassAvailable(GameEnums.SocialClass.Slave, culture, innovations, realmId);
		float valueOrDefault3 = dictionary.GetValueOrDefault(GameEnums.SocialClass.Slave);
		if (flag3 && valueOrDefault3 >= 5f)
		{
			num2 += 0.25f;
		}
		result.socialStratification = Mathf.Clamp01(num2);
		float num5 = valueOrDefault2;
		float num6 = num - num5;
		result.nonFoodProducerPopulation = Mathf.Max(0f, num6);
		float num7 = ((num > 0f) ? (result.nonFoodProducerPopulation / num) : 0f);
		result.specialization = Mathf.Clamp01(num7 / 0.18f);
		return result;
	}

	public static Dictionary<GameEnums.SocialClass, float> TallyClassPopulation(GameWorld world, int realmId)
	{
		Dictionary<GameEnums.SocialClass, float> dictionary = new Dictionary<GameEnums.SocialClass, float>();
		if ((Object)(object)world == (Object)null)
		{
			return dictionary;
		}
		bool flag = world.realms.TryGetValue(realmId, out var value) && value.realmForm == RealmForm.Nomadic && value.nomadicRange != null;
		for (int i = 0; i < world.tiles.Length; i++)
		{
			ref TileData reference = ref world.tiles[i];
			if (!reference.exists || reference.populationBlocks == null || !((!flag) ? (reference.ownerRealmId == realmId) : value.nomadicRange.IsInRange(i, world.mapWidth, world.mapHeight)))
			{
				continue;
			}
			foreach (PopulationBlock populationBlock in reference.populationBlocks)
			{
				if (!(populationBlock.count <= 0f))
				{
					dictionary.TryGetValue(populationBlock.socialClass, out var value2);
					dictionary[populationBlock.socialClass] = value2 + populationBlock.count;
				}
			}
		}
		return dictionary;
	}

	public static GameEnums.CultureStage StratificationToStage(float stratification, float specialization)
	{
		if (stratification >= 0.7f && specialization >= 0.5f)
		{
			return GameEnums.CultureStage.HighCivilization;
		}
		if (stratification >= 0.5f && specialization >= 0.3f)
		{
			return GameEnums.CultureStage.EthnicGroup;
		}
		if (stratification >= 0.35f)
		{
			return GameEnums.CultureStage.Chiefdom;
		}
		if (stratification >= 0.15f)
		{
			return GameEnums.CultureStage.Tribe;
		}
		return GameEnums.CultureStage.Band;
	}
}
}
