using System;
using System.Collections.Generic;
using MyriadCreation.Core.Enums;
using UnityEngine;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public class RealmSociety
{
	public int realmId;

	[NonSerialized]
	public Dictionary<GameEnums.SocialClass, ClassProfile> classes = new Dictionary<GameEnums.SocialClass, ClassProfile>();

	public float totalPopulation;

	[NonSerialized]
	public Dictionary<EconomicClass, float> populationByEconomicClass = new Dictionary<EconomicClass, float>();

	[NonSerialized]
	public Dictionary<SocialStratum, float> populationByStratum = new Dictionary<SocialStratum, float>();

	public EconomicClass dominantEconomicClass;

	[Range(0f, 100f)]
	public float unrestScore;

	public GameEnums.SocialClass dominantClass = GameEnums.SocialClass.Peasant;

	public GameEnums.SocialClass mostRestlessClass = GameEnums.SocialClass.Peasant;

	public ClassProfile Get(GameEnums.SocialClass c)
	{
		if (!classes.TryGetValue(c, out var value))
		{
			return null;
		}
		return value;
	}
}
}
