using System;
using MyriadCreation.Simulation.Laws;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.Scheduling;
using MyriadCreation.Simulation.State;
using UnityEngine;
using MyriadCreation.Simulation.LawHost;
using MyriadCreation.Simulation.Units;

namespace MyriadCreation.Simulation.Systems
{
public sealed class PoliticsSimulationSystem
{
	private readonly PoliticalManager _politics;

	private readonly Dictionary<int, RealmData> _realms;

	private readonly TileData[] _tiles;

	private readonly Dictionary<int, Army> _armies;

	private readonly List<WarState> _wars;

	private readonly EconomyManager _economyManager;

	private readonly DisasterSystem _disasterSystem;

	private readonly InnovationTree _innovationTree;

	private readonly SocietyManager _societyManager;

	private readonly FactionManager _factionManager;

	private readonly RegimeChangeDynamics _regimeDynamics;

	private readonly CharacterManager _characterManager;

	private readonly Dictionary<int, RealmSociety> _societyCache;

	private float _differentiationTimer;

	private const float DifferentiationIntervalDays = 25f;

	public FactionManager Factions => _factionManager;

	public RegimeChangeDynamics RegimeDynamics => _regimeDynamics;

	public PoliticsSimulationSystem(PoliticalManager politics, Dictionary<int, RealmData> realms, TileData[] tiles, Dictionary<int, Army> armies, List<WarState> wars, EconomyManager economyManager, DisasterSystem disasterSystem, InnovationTree innovationTree, SocietyManager societyManager, FactionManager factionManager, RegimeChangeDynamics regimeDynamics, CharacterManager characterManager, Dictionary<int, RealmSociety> societyCache)
	{
		_politics = politics ?? throw new ArgumentNullException("politics");
		_realms = realms ?? throw new ArgumentNullException("realms");
		_tiles = tiles ?? throw new ArgumentNullException("tiles");
		_armies = armies ?? throw new ArgumentNullException("armies");
		_wars = wars ?? throw new ArgumentNullException("wars");
		_economyManager = economyManager ?? throw new ArgumentNullException("economyManager");
		_disasterSystem = disasterSystem ?? throw new ArgumentNullException("disasterSystem");
		_innovationTree = innovationTree ?? throw new ArgumentNullException("innovationTree");
		_societyManager = societyManager ?? throw new ArgumentNullException("societyManager");
		_factionManager = factionManager ?? throw new ArgumentNullException("factionManager");
		_regimeDynamics = regimeDynamics ?? throw new ArgumentNullException("regimeDynamics");
		_characterManager = characterManager ?? throw new ArgumentNullException("characterManager");
		_societyCache = societyCache ?? throw new ArgumentNullException("societyCache");
	}

	public RealmSociety GetRealmSociety(int realmId)
	{
		if (!_societyCache.TryGetValue(realmId, out var value))
		{
			return null;
		}
		return value;
	}

	public void DailyTick(SimulationTickContext context)
	{
		_politics.DailyTick();
		_differentiationTimer += context.deltaDays;
		bool flag = _differentiationTimer >= 25f;
		if (flag)
		{
			_differentiationTimer = 0f;
		}
		foreach (RealmData value4 in _realms.Values)
		{
			// 法律/法令税收乘子在税收结算点消费（旧法/法令到期后乘子随 TickDecreeExpiry / 法律 pending 自然回落）
			float num = _economyManager.SettleTaxes(value4.realmId, LawEffectRuntime.GetTaxMultiplier(value4, context.day));
			RealmTreasury.Add(value4, num);
			TreasuryLedger.Record(value4.realmId, context.year * 365 + context.day, num, "tax");
		}
		Dictionary<int, float> dictionary = new Dictionary<int, float>(_realms.Count);
		foreach (RealmData value5 in _realms.Values)
		{
			if (value5 != null)
			{
				dictionary[value5.realmId] = 50f + value5.centralization * 20f;
			}
		}
		for (int i = 0; i < _tiles.Length; i++)
		{
			if (_tiles[i].exists)
			{
				int ownerRealmId = _tiles[i].ownerRealmId;
				if (ownerRealmId >= 0 && dictionary.TryGetValue(ownerRealmId, out var value))
				{
					_tiles[i].stability = Mathf.Lerp(_tiles[i].stability, value, 0.01f * (float)context.deltaDays);
				}
			}
		}
		Dictionary<int, List<int>> dictionary2 = new Dictionary<int, List<int>>();
		for (int j = 0; j < _tiles.Length; j++)
		{
			if (!_tiles[j].exists)
			{
				continue;
			}
			int ownerRealmId2 = _tiles[j].ownerRealmId;
			if (ownerRealmId2 >= 0)
			{
				if (!dictionary2.TryGetValue(ownerRealmId2, out var value2))
				{
					value2 = (dictionary2[ownerRealmId2] = new List<int>());
				}
				value2.Add(j);
			}
		}
		foreach (RealmData value6 in _realms.Values)
		{
			dictionary2.TryGetValue(value6.realmId, out var value3);
			SocietyPulse(value6, flag, value3, context);
		}
	}

	private void SocietyPulse(RealmData realm, bool doDifferentiation, IReadOnlyList<int> realmTiles, SimulationTickContext context)
	{
		RealmSituation sit = RealmSituationBuilder.Build(realm, _tiles, _economyManager, _wars, _armies, _disasterSystem, _innovationTree, realmTiles);
		if (doDifferentiation)
		{
			SocialDifferentiation.DifferentiateRealm(realm, _tiles, sit, realmTiles);
		}
		RealmSociety realmSociety = _societyManager.EvaluateRealm(realm, _tiles, sit, realmTiles);
		_societyManager.ApplyClassRelations(realm, realmSociety, context.deltaDays);
		List<CharacterData> charactersByRealm = _characterManager.GetCharactersByRealm(realm.realmId);
		_factionManager.UpdateRealmFactions(realmSociety, realm, charactersByRealm);
		_regimeDynamics.Tick(context.day, realm, realmSociety, sit, _factionManager);
		_societyCache[realm.realmId] = realmSociety;
	}
}
}
