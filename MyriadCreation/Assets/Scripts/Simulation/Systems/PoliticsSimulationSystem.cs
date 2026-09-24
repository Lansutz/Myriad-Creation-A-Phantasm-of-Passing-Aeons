using System;
using System.Collections.Generic;
using UnityEngine;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Simulation;
using MyriadCreation.Simulation.Characters;
using MyriadCreation.Simulation.Disaster;
using MyriadCreation.Simulation.Economy;
using MyriadCreation.Simulation.Innovation;
using MyriadCreation.Simulation.Society;
using MyriadCreation.Simulation.Warfare;

namespace MyriadCreation.Simulation.Systems
{
    /// <summary>
    /// 政治领域运行时。
    /// 负责政治经理、税收、稳定、社会分化、派系与政体动力学的完整日常推进。
    /// 不再把 GameWorld 私有方法作为调度器回调。
    /// </summary>
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

        public PoliticsSimulationSystem(
            PoliticalManager politics,
            Dictionary<int, RealmData> realms,
            TileData[] tiles,
            Dictionary<int, Army> armies,
            List<WarState> wars,
            EconomyManager economyManager,
            DisasterSystem disasterSystem,
            InnovationTree innovationTree,
            SocietyManager societyManager,
            FactionManager factionManager,
            RegimeChangeDynamics regimeDynamics,
            CharacterManager characterManager,
            Dictionary<int, RealmSociety> societyCache)
        {
            _politics = politics ?? throw new ArgumentNullException(nameof(politics));
            _realms = realms ?? throw new ArgumentNullException(nameof(realms));
            _tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
            _armies = armies ?? throw new ArgumentNullException(nameof(armies));
            _wars = wars ?? throw new ArgumentNullException(nameof(wars));
            _economyManager = economyManager ?? throw new ArgumentNullException(nameof(economyManager));
            _disasterSystem = disasterSystem ?? throw new ArgumentNullException(nameof(disasterSystem));
            _innovationTree = innovationTree ?? throw new ArgumentNullException(nameof(innovationTree));
            _societyManager = societyManager ?? throw new ArgumentNullException(nameof(societyManager));
            _factionManager = factionManager ?? throw new ArgumentNullException(nameof(factionManager));
            _regimeDynamics = regimeDynamics ?? throw new ArgumentNullException(nameof(regimeDynamics));
            _characterManager = characterManager ?? throw new ArgumentNullException(nameof(characterManager));
            _societyCache = societyCache ?? throw new ArgumentNullException(nameof(societyCache));
        }

        public void DailyTick(SimulationTickContext context)
        {
            _politics.DailyTick();

            _differentiationTimer += context.deltaDays;
            bool doDifferentiation = _differentiationTimer >= DifferentiationIntervalDays;
            if (doDifferentiation) _differentiationTimer = 0f;

            foreach (var realm in _realms.Values)
            {
                float taxIncome = _economyManager.SettleTaxes(realm.realmId);
                realm.treasury += taxIncome;
            }

            var centralCache = new Dictionary<int, float>(_realms.Count);
            foreach (var realm in _realms.Values)
                if (realm != null) centralCache[realm.realmId] = 50f + realm.centralization * 20f;

            for (int i = 0; i < _tiles.Length; i++)
            {
                if (!_tiles[i].exists) continue;
                int owner = _tiles[i].ownerRealmId;
                if (owner < 0) continue;
                if (centralCache.TryGetValue(owner, out float target))
                    _tiles[i].stability = Mathf.Lerp(_tiles[i].stability, target, 0.01f * context.deltaDays);
            }

            var realmTilesIndex = new Dictionary<int, List<int>>();
            for (int i = 0; i < _tiles.Length; i++)
            {
                if (!_tiles[i].exists) continue;
                int owner = _tiles[i].ownerRealmId;
                if (owner < 0) continue;
                if (!realmTilesIndex.TryGetValue(owner, out var list))
                {
                    list = new List<int>();
                    realmTilesIndex[owner] = list;
                }
                list.Add(i);
            }

            foreach (var realm in _realms.Values)
            {
                realmTilesIndex.TryGetValue(realm.realmId, out var realmTiles);
                SocietyPulse(realm, doDifferentiation, realmTiles, context);
            }
        }

        private void SocietyPulse(
            RealmData realm,
            bool doDifferentiation,
            IReadOnlyList<int> realmTiles,
            SimulationTickContext context)
        {
            var situation = RealmSituationBuilder.Build(
                realm,
                _tiles,
                _economyManager,
                _wars,
                _armies,
                _disasterSystem,
                _innovationTree,
                realmTiles);

            if (doDifferentiation)
                SocialDifferentiation.DifferentiateRealm(realm, _tiles, situation, realmTiles);

            var society = _societyManager.EvaluateRealm(realm, _tiles, situation, realmTiles);
            _societyManager.ApplyClassRelations(realm, society, context.deltaDays);
            var characters = _characterManager.GetCharactersByRealm(realm.realmId);
            _factionManager.UpdateRealmFactions(society, realm, characters);
            _regimeDynamics.Tick(context.day, realm, society, situation, _factionManager);
            _societyCache[realm.realmId] = society;
        }
    }
}
