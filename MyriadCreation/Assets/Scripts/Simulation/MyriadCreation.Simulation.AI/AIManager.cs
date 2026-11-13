using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.WorldState;

namespace MyriadCreation.Simulation.AI
{
public class AIManager
{
	private readonly Dictionary<int, AIController> _controllers = new Dictionary<int, AIController>();

	public AIController CreateController(int realmId, AIPersonality? personality = null)
	{
		AIPersonality personality2 = personality ?? AIPersonality.RandomPersonality();
		AIController aIController = new AIController(realmId, personality2);
		_controllers[realmId] = aIController;
		return aIController;
	}

	public void DailyTick(Dictionary<int, RealmData> realms, TileData[] tiles, DiplomacyManager diplomacy, InnovationTree innovations, CharacterManager characters = null, GameWorld world = null)
	{
		foreach (AIController value in _controllers.Values)
		{
			value.DailyTick(realms, tiles, diplomacy, innovations, characters, world);
		}
	}

	public void ExecutePendingIntents(AIIntentExecutor executor)
	{
		if (executor == null)
		{
			return;
		}
		foreach (AIController value in _controllers.Values)
		{
			executor.Execute(value.DrainIntents());
		}
	}

	public AIController GetController(int realmId)
	{
		if (!_controllers.TryGetValue(realmId, out var value))
		{
			return null;
		}
		return value;
	}

	public float GetResearchRate(int realmId, RealmData realm, TileData[] tiles)
	{
		if (realm == null || tiles == null)
		{
			return 0f;
		}
		return GetController(realmId)?.GetResearchRate(realm, tiles) ?? 0f;
	}

	public void SyncRulers(CharacterManager characters)
	{
		if (characters == null)
		{
			return;
		}
		foreach (KeyValuePair<int, AIController> controller in _controllers)
		{
			CharacterData ruler = characters.FindRulerOfRealm(controller.Key);
			controller.Value.SyncPersonality(ruler);
		}
	}

	public IReadOnlyDictionary<int, AIController> GetAllControllers()
	{
		return _controllers;
	}
}
}
