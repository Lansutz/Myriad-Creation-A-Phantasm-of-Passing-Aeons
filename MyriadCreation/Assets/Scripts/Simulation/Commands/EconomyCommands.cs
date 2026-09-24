using System;
using System.Collections.Generic;
using UnityEngine;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Data;

namespace MyriadCreation.Simulation.Economy
{
    /// <summary>经济领域的政权投资写入契约。</summary>
    public readonly struct ImproveRealmEconomyCommand : ISimulationCommand
    {
        public readonly int RealmId;

        public ImproveRealmEconomyCommand(int realmId)
        {
            RealmId = realmId;
        }
    }

    public sealed class ImproveRealmEconomyCommandHandler
        : ISimulationCommandHandler<ImproveRealmEconomyCommand>
    {
        private readonly Dictionary<int, RealmData> _realms;
        private readonly TileData[] _tiles;

        public ImproveRealmEconomyCommandHandler(
            Dictionary<int, RealmData> realms,
            TileData[] tiles)
        {
            _realms = realms ?? throw new ArgumentNullException(nameof(realms));
            _tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
        }

        public CommandResult Handle(ImproveRealmEconomyCommand command)
        {
            if (!_realms.TryGetValue(command.RealmId, out var realm))
                return CommandResult.Failed("realm_not_found");

            int improved = 0;
            foreach (int index in realm.coreTiles)
            {
                if (index < 0 || index >= _tiles.Length || realm.treasury <= 50f)
                    continue;

                _tiles[index].development = Mathf.Min(1f, _tiles[index].development + 0.01f);
                realm.treasury -= 10f;
                improved++;
            }

            return improved > 0
                ? CommandResult.Succeeded("economy_improved")
                : CommandResult.Failed("economy_investment_rejected");
        }
    }
}
