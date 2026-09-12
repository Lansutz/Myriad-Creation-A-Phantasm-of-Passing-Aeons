





using System.Collections.Generic;
using CivilizationEvolution.Core;
using CivilizationEvolution.Core.Constants;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Dto;
using CivilizationEvolution.Core.Enums;
using CivilizationEvolution.Simulation.Characters;
using CivilizationEvolution.Simulation.Diplomacy;
using CivilizationEvolution.Simulation.Economy;
using CivilizationEvolution.Simulation.Events;
using CivilizationEvolution.Simulation.Generation;
using CivilizationEvolution.Simulation.Innovation;
using CivilizationEvolution.Simulation.Modding;
using CivilizationEvolution.Simulation.Politics;
using CivilizationEvolution.Simulation.Population;
using CivilizationEvolution.Simulation.Society;
using CivilizationEvolution.Simulation.WorldState;


namespace CivilizationEvolution.Simulation.AI
{
    public class AIManager
    {
        private readonly Dictionary<int, AIController> _controllers = new Dictionary<int, AIController>();

 /// <summary>为政权创建AI控制器</summary>
        public AIController CreateController(int realmId, AIPersonality? personality = null)
        {
            var p = personality ?? AIPersonality.RandomPersonality();
            var controller = new AIController(realmId, p);
            _controllers[realmId] = controller;
            return controller;
        }

 /// <summary>每日所有AI Tick</summary>
        public void DailyTick(
            Dictionary<int, RealmData> realms,
            TileData[] tiles,
            DiplomacyManager diplomacy,
            EconomyManager economy,
            InnovationTree innovations,
            CivilizationEvolution.Simulation.Characters.CharacterManager characters = null)
        {
            foreach (var controller in _controllers.Values)
            {
                controller.DailyTick(realms, tiles, diplomacy, economy, innovations, characters);
            }
        }

        public AIController GetController(int realmId) =>
            _controllers.TryGetValue(realmId, out var c) ? c : null;

 /// <summary>同步各政权统治者的七维人格到 AI 偏置（人格漂移实时反映到决策）</summary>
        public void SyncRulers(CharacterManager characters)
        {
            if (characters == null) return;
            foreach (var kv in _controllers)
            {
                var ruler = characters.FindRulerOfRealm(kv.Key);
                kv.Value.SyncPersonality(ruler);
            }
        }

        public IReadOnlyDictionary<int, AIController> GetAllControllers() => _controllers;
    }
}
