using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Units
{
public static class TransportCatalog
{
	public static PackAnimalProfile GetAnimal(PackAnimalKind kind)
	{
		return kind switch
		{
			PackAnimalKind.Horse => new PackAnimalProfile
			{
				capacity = 60f,
				speedBonus = 0.5f,
				draftStrength = 1,
				minTempC = -10f,
				maxTempC = 30f,
				maxHumidityPct = 80f,
				maxSoilHumidityPct = 70f,
				maxSlopeDegrees = 25f,
				maxElevation01 = 0.5f
			}, 
			PackAnimalKind.Camel => new PackAnimalProfile
			{
				capacity = 80f,
				speedBonus = 0.35f,
				draftStrength = 1,
				minTempC = 5f,
				maxTempC = 50f,
				maxHumidityPct = 40f,
				maxSoilHumidityPct = 30f,
				maxSlopeDegrees = 20f,
				maxElevation01 = 0.5f
			}, 
			PackAnimalKind.Donkey => new PackAnimalProfile
			{
				capacity = 40f,
				speedBonus = 0.2f,
				draftStrength = 1,
				minTempC = -5f,
				maxTempC = 35f,
				maxHumidityPct = 70f,
				maxSoilHumidityPct = 60f,
				maxSlopeDegrees = 30f,
				maxElevation01 = 0.6f
			}, 
			PackAnimalKind.Mule => new PackAnimalProfile
			{
				capacity = 50f,
				speedBonus = 0.25f,
				draftStrength = 1,
				minTempC = -5f,
				maxTempC = 30f,
				maxHumidityPct = 75f,
				maxSoilHumidityPct = 65f,
				maxSlopeDegrees = 30f,
				maxElevation01 = 0.6f
			}, 
			PackAnimalKind.Ox => new PackAnimalProfile
			{
				capacity = 70f,
				speedBonus = 0.05f,
				draftStrength = 2,
				minTempC = 0f,
				maxTempC = 35f,
				maxHumidityPct = 85f,
				maxSoilHumidityPct = 80f,
				maxSlopeDegrees = 20f,
				maxElevation01 = 0.4f
			}, 
			PackAnimalKind.Yak => new PackAnimalProfile
			{
				capacity = 60f,
				speedBonus = 0.15f,
				draftStrength = 1,
				minTempC = -20f,
				maxTempC = 20f,
				maxHumidityPct = 80f,
				maxSoilHumidityPct = 70f,
				maxSlopeDegrees = 30f,
				maxElevation01 = 0.8f
			}, 
			PackAnimalKind.Reindeer => new PackAnimalProfile
			{
				capacity = 45f,
				speedBonus = 0.25f,
				draftStrength = 1,
				minTempC = -30f,
				maxTempC = 15f,
				maxHumidityPct = 85f,
				maxSoilHumidityPct = 75f,
				maxSlopeDegrees = 25f,
				maxElevation01 = 0.7f
			}, 
			PackAnimalKind.Llama => new PackAnimalProfile
			{
				capacity = 35f,
				speedBonus = 0.2f,
				draftStrength = 1,
				minTempC = -5f,
				maxTempC = 25f,
				maxHumidityPct = 60f,
				maxSoilHumidityPct = 50f,
				maxSlopeDegrees = 30f,
				maxElevation01 = 0.7f
			}, 
			_ => new PackAnimalProfile
			{
				capacity = 50f,
				speedBonus = 0.2f,
				draftStrength = 1,
				minTempC = 0f,
				maxTempC = 35f,
				maxHumidityPct = 70f,
				maxSoilHumidityPct = 60f,
				maxSlopeDegrees = 25f,
				maxElevation01 = 0.5f
			}, 
		};
	}

	public static CartProfile GetCart(CartKind kind)
	{
		return kind switch
		{
			CartKind.Handcart => new CartProfile
			{
				capacity = 80f,
				requiredDraftStrength = 0,
				canBePulledByPeople = true,
				speedPenalty = 0.15f
			}, 
			CartKind.OxCart => new CartProfile
			{
				capacity = 320f,
				requiredDraftStrength = 2,
				canBePulledByPeople = false,
				speedPenalty = 0.2f
			}, 
			_ => new CartProfile
			{
				capacity = 500f,
				requiredDraftStrength = 4,
				canBePulledByPeople = false,
				speedPenalty = 0.3f
			}, 
		};
	}
}
}
