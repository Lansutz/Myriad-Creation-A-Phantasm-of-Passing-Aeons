using System;
using System.Collections.Generic;
using MyriadCreation.Core.Enums;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public class Faction
{
	public int factionId;

	public int realmId;

	public string factionName = "";

	public FactionStance stance;

	[NonSerialized]
	public Dictionary<GameEnums.SocialClass, float> classBacking = new Dictionary<GameEnums.SocialClass, float>();

	public GameEnums.SocialClass primaryClass = GameEnums.SocialClass.Peasant;

	public int leaderCharacterId = -1;

	public List<int> memberCharacterIds = new List<int>();

	public FactionPlatform platform;

	[Range(0f, 100f)]
	public float power;

	[Range(0f, 100f)]
	public float cohesion = 60f;

	public bool isInGovernment;

	public Faction()
	{
	}

	public Faction(int id, int realmId, FactionStance stance)
	{
		factionId = id;
		this.realmId = realmId;
		this.stance = stance;
	}

	public bool BackedBy(GameEnums.SocialClass cls, float threshold = 0.2f)
	{
		return classBacking.GetValueOrDefault(cls, 0f) >= threshold;
	}
}
}
