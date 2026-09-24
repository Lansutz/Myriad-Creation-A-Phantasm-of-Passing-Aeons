using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Dto;
using CivilizationEvolution.Core.Enums;
using CivilizationEvolution.Simulation.Characters;
using CivilizationEvolution.Simulation.Diplomacy;
using CivilizationEvolution.Simulation.Innovation;

namespace CivilizationEvolution.Simulation.AI
{
    /// <summary>
    /// AI 行动的过渡执行边界。
    /// Controller 只产生 Intent；这里才允许调用具体领域 API/写入领域状态。
    /// 后续可将各 Intent 分别迁移到 Diplomacy/Economy/Warfare 等领域 Action。
    /// </summary>
    public sealed class AIIntentExecutor
    {
        private readonly Dictionary<int, RealmData> _realms;
        private readonly TileData[] _tiles;
        private readonly DiplomacyManager _diplomacy;
        private readonly InnovationTree _innovations;
        private readonly CharacterManager _characters;
        private readonly Func<int, int, string, bool> _declareWar;

        public AIIntentExecutor(
            Dictionary<int, RealmData> realms,
            TileData[] tiles,
            DiplomacyManager diplomacy,
            InnovationTree innovations,
            CharacterManager characters,
            Func<int, int, string, bool> declareWar)
        {
            _realms = realms ?? throw new ArgumentNullException(nameof(realms));
            _tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
            _diplomacy = diplomacy ?? throw new ArgumentNullException(nameof(diplomacy));
            _innovations = innovations ?? throw new ArgumentNullException(nameof(innovations));
            _characters = characters;
            _declareWar = declareWar ?? throw new ArgumentNullException(nameof(declareWar));
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
                        ExecuteRaid(intent);
                        break;
                    case AIIntentType.DeclareWar:
                        _declareWar(intent.ActorRealmId, intent.TargetRealmId, "领土扩张");
                        break;
                    case AIIntentType.ImproveEconomy:
                        ExecuteImproveEconomy(intent);
                        break;
                    case AIIntentType.ProposeAlliance:
                        _diplomacy.ProposeAlliance(intent.ActorRealmId, intent.TargetRealmId, intent.AllianceType);
                        break;
                    case AIIntentType.SendGift:
                        _diplomacy.SendGift(intent.ActorRealmId, intent.TargetRealmId, intent.Amount);
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

        private void ExecuteRaid(AIIntent intent)
        {
            var result = _diplomacy.RaidSettlement(
                intent.ActorRealmId,
                intent.TargetRealmId,
                intent.TargetTileIndex,
                intent.RaidType,
                _tiles);

            if (!result.success || intent.RaidType != GameEnums.RaidType.Massacre || _characters == null)
                return;

            var ruler = _characters.FindRulerOfRealm(intent.ActorRealmId);
            if (ruler != null)
                ruler.achievements.massacres++;
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
