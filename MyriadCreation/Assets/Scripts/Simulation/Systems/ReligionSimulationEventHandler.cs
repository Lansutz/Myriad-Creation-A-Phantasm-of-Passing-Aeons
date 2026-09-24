using System;
using System.Collections.Generic;
using MyriadCreation.Core.Events;

namespace MyriadCreation.Simulation.Systems
{
    /// <summary>
    /// Converts simulation facts into religion-domain state changes.
    /// This keeps warfare from reaching into FaithSystem directly.
    /// </summary>
    public sealed class ReligionSimulationEventHandler : IDisposable
    {
        private readonly IReadOnlyList<FaithSystem> _faithSystems;
        private readonly SimulationEventBus _events;

        public ReligionSimulationEventHandler(
            IReadOnlyList<FaithSystem> faithSystems,
            SimulationEventBus events)
        {
            _faithSystems = faithSystems ?? throw new ArgumentNullException(nameof(faithSystems));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _events.Subscribe<FaithConflictTriggeredEvent>(OnFaithConflictTriggered);
        }

        private void OnFaithConflictTriggered(FaithConflictTriggeredEvent evt)
        {
            if (evt.FaithA == evt.FaithB) return;

            FaithSystem a = Find(evt.FaithA);
            FaithSystem b = Find(evt.FaithB);
            a?.AddFervor(25f);
            b?.AddFervor(25f);
        }

        private FaithSystem Find(int faithId)
        {
            for (int i = 0; i < _faithSystems.Count; i++)
            {
                var faith = _faithSystems[i];
                if (faith != null && faith.faithId == faithId)
                    return faith;
            }
            return null;
        }

        public void Dispose()
        {
            _events.Unsubscribe<FaithConflictTriggeredEvent>(OnFaithConflictTriggered);
        }
    }
}
