using System;
using System.Collections.Generic;

namespace CivilizationEvolution.Simulation.Modding
{
    /// <summary>
    /// Stable storage contract for one runtime content type.
    /// Domain code should consume the resolver contract rather than this mutable store.
    /// </summary>
    public interface IContentStore<TKey, TValue> : IContentResolver<TKey, TValue>
    {
        void Set(TKey id, TValue value);
        bool Remove(TKey id);
        void Clear();
    }

    public sealed class ContentStore<TKey, TValue> : IContentStore<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> _values = new Dictionary<TKey, TValue>();

        public bool TryGet(TKey id, out TValue value) => _values.TryGetValue(id, out value);

        public IEnumerable<TValue> All => _values.Values;

        public void Set(TKey id, TValue value)
        {
            if (EqualityComparer<TKey>.Default.Equals(id, default(TKey)))
                throw new ArgumentException("Content id cannot be the default key.", nameof(id));
            if (ReferenceEquals(value, null))
                throw new ArgumentNullException(nameof(value));

            _values[id] = value;
        }

        public bool Remove(TKey id) => _values.Remove(id);

        public void Clear() => _values.Clear();
    }

    /// <summary>
    /// A source-aware provider loads one content type into a store.
    /// This is the extension point for Base, Mod, Editor-generated and Scenario content.
    /// </summary>
    public interface IContentProvider<TKey, TValue>
    {
        string ContentType { get; }
        void Load(ContentSource source, IContentStore<TKey, TValue> target);
    }
}
