using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Core.Constants;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Dto;
using CivilizationEvolution.Core.Enums;
using CivilizationEvolution.Simulation.AI;
using CivilizationEvolution.Simulation.Characters;
using CivilizationEvolution.Simulation.Culture;
using CivilizationEvolution.Simulation.Diplomacy;
using CivilizationEvolution.Simulation.Disaster;
using CivilizationEvolution.Simulation.Economy;
using CivilizationEvolution.Simulation.Events;
using CivilizationEvolution.Simulation.Generation;
using CivilizationEvolution.Simulation.Innovation;
using CivilizationEvolution.Simulation.Modding;
using CivilizationEvolution.Simulation.Politics;
using CivilizationEvolution.Simulation.Population;
using CivilizationEvolution.Simulation.Religion;
using CivilizationEvolution.Simulation.Settlement;
using CivilizationEvolution.Simulation.Society;
using CivilizationEvolution.Simulation.Warfare;
using CivilizationEvolution.World.Biome;
using CivilizationEvolution.World.Climate;
using CivilizationEvolution.World.Generation;
using CivilizationEvolution.World.Hydrology;
using CivilizationEvolution.World.Settlement;
using CivilizationEvolution.World.Terrain;

















namespace CivilizationEvolution.Simulation.WorldState
{
 /// GameWorld.Events —— 事件系统（事件队列/分发/各类型事件处理器）（partial class，与 GameWorld.cs 共享字段与子系统）
    public partial class GameWorld
    {

 /// <summary>事件系统</summary>
        public void EnqueueEvent(GameEvent evt)
        {
            _eventQueue.Enqueue(evt);
        }


        private void ProcessEvents()
        {
            while (_eventQueue.Count > 0)
            {
                var evt = _eventQueue.Dequeue();
                foreach (var listener in _eventListeners)
                {
                    listener.OnGameEvent(evt);
                }

                switch (evt.eventType)
                {
                    case GameEventType.Famine:
                        HandleFamine(evt);
                        break;
                    case GameEventType.Rebellion:
                        HandleRebellion(evt);
                        break;
                    case GameEventType.SeasonChange:
                        HandleSeasonChange(evt);
                        break;
                    case GameEventType.WarDeclaration:
                        HandleWarDeclaration(evt);
                        break;
                    case GameEventType.PeaceTreaty:
                        HandlePeaceTreaty(evt);
                        break;
                    case GameEventType.Plague:
                        HandlePlague(evt);
                        break;
                    case GameEventType.NaturalDisaster:
                        HandleNaturalDisaster(evt);
                        break;
                    case GameEventType.EconomicCrisis:
                        HandleEconomicCrisis(evt);
                        break;
                }
            }
        }


        private void HandleFamine(GameEvent evt)
        {
            if (evt.tileIndex < 0 || evt.tileIndex >= tiles.Length) return;
            for (int i = 0; i < tiles[evt.tileIndex].populationBlocks.Count; i++)
            {
                var pb = tiles[evt.tileIndex].populationBlocks[i];
                pb.count *= 0.95f;
                pb.satisfaction = Mathf.Max(0f, pb.satisfaction - 20f);
                tiles[evt.tileIndex].populationBlocks[i] = pb;
            }
            tiles[evt.tileIndex].stability = Mathf.Max(0f, tiles[evt.tileIndex].stability - 10f);
            Debug.Log($"[Event] 饥荒 @ 地块{evt.tileIndex}");
        }


        private void HandleRebellion(GameEvent evt)
        {
            if (evt.tileIndex < 0 || evt.tileIndex >= tiles.Length) return;
            tiles[evt.tileIndex].order = Mathf.Max(0f, tiles[evt.tileIndex].order - 15f);
            tiles[evt.tileIndex].stability = Mathf.Max(0f, tiles[evt.tileIndex].stability - 10f);
            Debug.Log($"[Event] 叛乱 @ 地块{evt.tileIndex}，严重度{evt.severity:F0}");
        }


        private void HandleSeasonChange(GameEvent evt)
        {
            string[] seasonNames = { "春", "夏", "秋", "冬" };
            int s = Mathf.Clamp(Mathf.RoundToInt(evt.severity), 0, 3);
            Debug.Log($"[Event] 季节变换：{seasonNames[s]}季");
        }


        private void HandleWarDeclaration(GameEvent evt)
        {
            Debug.Log($"[Event] 战争爆发：政权{evt.realmId}，严重度{evt.severity:F0}");
        }


        private void HandlePeaceTreaty(GameEvent evt)
        {
            Debug.Log($"[Event] 和平条约：政权{evt.realmId}");
        }


        private void HandlePlague(GameEvent evt)
        {
            if (evt.tileIndex >= 0 && evt.tileIndex < tiles.Length)
            {
                _diseaseSystem.OutbreakDisease(DiseaseType.Plague, currentDay, currentYear, evt.tileIndex);
                Debug.Log($"[Event] 瘟疫爆发 @ 地块{evt.tileIndex}");
            }
        }


        private void HandleNaturalDisaster(GameEvent evt)
        {
            if (evt.tileIndex >= 0 && evt.tileIndex < tiles.Length)
            {
                _disasterSystem.TriggerDisaster(DisasterType.Earthquake, currentDay, currentYear, evt.tileIndex);
                Debug.Log($"[Event] 自然灾害 @ 地块{evt.tileIndex}");
            }
        }


        private void HandleEconomicCrisis(GameEvent evt)
        {
            if (realms.TryGetValue(evt.realmId, out var realm))
            {
                realm.treasury *= 0.7f;
                Debug.Log($"[Event] 经济危机：政权{evt.realmId}，国库-30%");
            }
        }


        public void RegisterEventListener(IGameEventListener listener)
        {
            _eventListeners.Add(listener);
        }


        public void UnregisterEventListener(IGameEventListener listener)
        {
            _eventListeners.Remove(listener);
        }

    }
}
