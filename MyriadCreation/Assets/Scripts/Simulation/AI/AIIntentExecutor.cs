using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core.Contracts;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Simulation.Innovation;

namespace CivilizationEvolution.Simulation.AI
{
    /// <summary>
    /// AI 意图到领域写入契约的过渡适配器。
    /// 外交/战争 Intent 已经进入 Domain Command；经济/内政 Intent 暂留此处，
    /// 避免迁移过程中改变既有模拟行为，待对应领域边界确认后再迁移。
    /// </summary>
    public sealed class AIIntentExecutor
    {
        private readonly SimulationCommandBus _commands;
        private readonly InnovationTree _innovations;
        private readonly Dictionary<int, RealmData> _realms;
        private readonly TileData[] _tiles;

        public AIIntentExecutor(
            SimulationCommandBus commands,
            InnovationTree innovations,
            Dictionary<int, RealmData> realms,
            TileData[] tiles)
        {
            _commands = commands ?? throw new ArgumentNullException(nameof(commands));
            _innovations = innovations ?? throw new ArgumentNullException(nameof(innovations));
            _realms = realms ?? throw new ArgumentNullException(nameof(realms));
            _tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
        }

        public void Execute(IEnumerable<AIIntent> intents)
        {
            if (intents == null) return;

            foreach (var intent in intents)
            {
                switch (intent.Type)
                {
                    case AIIntentType.StartResearch:
                        ExecuteStartResearch(intent);
                        break;
                    case AIIntentType.RaidSettlement:
                        _commands.Send(new Diplomacy.RaidSettlementCommand(
                            intent.ActorRealmId,
                            intent.TargetRealmId,
                            intent.TargetTileIndex,
                            intent.RaidType));
                        break;
                    case AIIntentType.DeclareWar:
                        _commands.Send(new Diplomacy.DeclareWarCommand(
                            intent.ActorRealmId,
                            intent.TargetRealmId,
                            "领土扩张"));
                        break;
                    case AIIntentType.ProposeAlliance:
                        _commands.Send(new Diplomacy.ProposeAllianceCommand(
                            intent.ActorRealmId,
                            intent.TargetRealmId,
                            intent.AllianceType));
                        break;
                    case AIIntentType.SendGift:
                        _commands.Send(new Diplomacy.SendGiftCommand(
                            intent.ActorRealmId,
                            intent.TargetRealmId,
                            intent.Amount));
                        break;
                    case AIIntentType.ImproveEconomy:
                        ExecuteImproveEconomy(intent);
                        break;
                    case AIIntentType.ConsolidateRealm:
                        ExecuteConsolidation(intent);
                        break;
                    case AIIntentType.MilitaryBuildUp:
                        ExecuteMilitaryBuildUp(intent);
                        break;
                }
            }
        }

        private void ExecuteStartResearch(AIIntent intent)
        {
            if (intent.InnovationId != 0)
                _innovations.StartResearch(intent.ActorRealmId, intent.InnovationId);
        }

        private void ExecuteImproveEconomy(AIIntent intent)
        {
            if (!_realms.TryGetValue(intent.ActorRealmId, out var realm)) return;

            foreach (int idx in realm.coreTiles)
            {
                if (idx >= 0 && idx < _tiles.Length && realm.treasury > 50f)
                {
                    _tiles[idx].development = Mathf.Min(1f, _tiles[idx].development + 0.01f);
                    realm.treasury -= 10f;
                }
            }
        }

        private void ExecuteConsolidation(AIIntent intent)
        {
            if (!_realms.TryGetValue(intent.ActorRealmId, out var realm)) return;

            foreach (int idx in realm.coreTiles)
            {
                if (idx >= 0 && idx < _tiles.Length)
                {
                    _tiles[idx].stability = Mathf.Min(100f, _tiles[idx].stability + 1f);
                    _tiles[idx].order = Mathf.Min(100f, _tiles[idx].order + 0.5f);
                }
            }
        }

        private void ExecuteMilitaryBuildUp(AIIntent intent)
        {
            if (_realms.TryGetValue(intent.ActorRealmId, out var realm))
                realm.treasury = Mathf.Max(0f, realm.treasury - 50f);
        }
    }
}
