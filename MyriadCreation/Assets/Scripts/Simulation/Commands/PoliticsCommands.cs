using System;
using System.Collections.Generic;
using UnityEngine;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Data;

namespace MyriadCreation.Simulation.Politics
{
    /// <summary>政治领域的政权治理写入契约。</summary>
    public readonly struct ConsolidateRealmCommand : ISimulationCommand
    {
        public readonly int RealmId;

        public ConsolidateRealmCommand(int realmId)
        {
            RealmId = realmId;
        }
    }

    public sealed class ConsolidateRealmCommandHandler
        : ISimulationCommandHandler<ConsolidateRealmCommand>
    {
        private readonly Dictionary<int, RealmData> _realms;
        private readonly TileData[] _tiles;

        public ConsolidateRealmCommandHandler(
            Dictionary<int, RealmData> realms,
            TileData[] tiles)
        {
            _realms = realms ?? throw new ArgumentNullException(nameof(realms));
            _tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
        }

        public CommandResult Handle(ConsolidateRealmCommand command)
        {
            if (!_realms.TryGetValue(command.RealmId, out var realm))
                return CommandResult.Failed("realm_not_found");

            int affected = 0;
            foreach (int index in realm.coreTiles)
            {
                if (index < 0 || index >= _tiles.Length)
                    continue;

                _tiles[index].stability = Mathf.Min(100f, _tiles[index].stability + 1f);
                _tiles[index].order = Mathf.Min(100f, _tiles[index].order + 0.5f);
                affected++;
            }

            return affected > 0
                ? CommandResult.Succeeded("realm_consolidated")
                : CommandResult.Failed("consolidation_rejected");
        }
    }
}
