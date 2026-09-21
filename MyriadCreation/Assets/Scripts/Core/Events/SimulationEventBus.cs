using System;
using System.Collections.Generic;

namespace CivilizationEvolution.Core.Events
{
    /// <summary>
    /// Lightweight in-process simulation event bus.
    /// Domain systems communicate through facts/events instead of holding references to each other.
    /// </summary>
    public sealed class SimulationEventBus
    {
        private readonly Dictionary<Type, Delegate> _handlers = new Dictionary<Type, Delegate>();

        public void Subscribe<T>(Action<T> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var existing))
                _handlers[type] = Delegate.Combine(existing, handler);
            else
                _handlers[type] = handler;
        }

        public void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null) return;
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var existing)) return;

            var next = Delegate.Remove(existing, handler);
            if (next == null) _handlers.Remove(type);
            else _handlers[type] = next;
        }

        public void Publish<T>(T evt)
        {
            if (!_handlers.TryGetValue(typeof(T), out var handler)) return;
            ((Action<T>)handler).Invoke(evt);
        }

        public void Clear() => _handlers.Clear();
    }

    /// <summary>
    /// Cross-domain fact: a character performed real practice with an innovation.
    /// The producer does not know which systems consume the fact.
    /// </summary>
    public readonly struct PracticeRecordedEvent
    {
        public readonly int characterId;
        public readonly int innovationId;
        public readonly float amount;

        public PracticeRecordedEvent(int characterId, int innovationId, float amount)
        {
            this.characterId = characterId;
            this.innovationId = innovationId;
            this.amount = amount;
        }
    }
}
