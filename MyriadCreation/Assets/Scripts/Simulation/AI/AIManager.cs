





using System.Collections.Generic;
using MyriadCreation.Core;
using MyriadCreation.Core.Constants;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Dto;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Characters;
using MyriadCreation.Simulation.Diplomacy;
using MyriadCreation.Simulation.Economy;
using MyriadCreation.Simulation.Events;
using MyriadCreation.Simulation.Generation;
using MyriadCreation.Simulation.Innovation;
using MyriadCreation.Simulation.Modding;
using MyriadCreation.Simulation.Politics;
using MyriadCreation.Simulation.Population;
using MyriadCreation.Simulation.Society;
using MyriadCreation.Simulation.WorldState;


namespace MyriadCreation.Simulation.AI
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
            InnovationTree innovations,
            MyriadCreation.Simulation.Characters.CharacterManager characters = null)
        {
            foreach (var controller in _controllers.Values)
            {
                controller.DailyTick(realms, tiles, diplomacy, innovations, characters);
            }
        }


        /// <summary>执行本 Tick 已产生的 AI 意图；决策与领域状态变更分离。</summary>
        public void ExecutePendingIntents(AIIntentExecutor executor)
        {
            if (executor == null) return;
            foreach (var controller in _controllers.Values)
                executor.Execute(controller.DrainIntents());
        }

        public AIController GetController(int realmId) =>
            _controllers.TryGetValue(realmId, out var c) ? c : null;

        /// <summary>为革新领域提供 AI 研究速率修正；AI 负责提供偏置，革新领域负责推进研究状态。</summary>
        public float GetResearchRate(int realmId, RealmData realm, TileData[] tiles)
        {
            if (realm == null || tiles == null) return 0f;
            var controller = GetController(realmId);
            if (controller == null) return 0f;
            return controller.GetResearchRate(realm, tiles);
        }

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
