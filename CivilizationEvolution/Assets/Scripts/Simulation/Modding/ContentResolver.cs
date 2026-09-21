using System;
using System.Collections.Generic;

namespace CivilizationEvolution.Simulation.Modding
{
    /// <summary>
    /// Stable read-only contract for resolving runtime content.
    /// Consumers depend on this contract instead of ContentRegistry's internal dictionaries.
    /// </summary>
    public interface IContentResolver<TKey, TValue>
    {
        bool TryGet(TKey id, out TValue value);
        IEnumerable<TValue> All { get; }
    }

    /// <summary>
    /// Small adapter used while the legacy ContentRegistry is being migrated.
    /// The backing implementation may later become a dense array, cache, package index,
    /// or another storage mechanism without changing consumers.
    /// </summary>
    public sealed class ContentResolver<TKey, TValue> : IContentResolver<TKey, TValue>
    {
        private readonly Func<TKey, TValue> _lookup;
        private readonly Func<IEnumerable<TValue>> _all;

        public ContentResolver(Func<TKey, TValue> lookup, Func<IEnumerable<TValue>> all)
        {
            _lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
            _all = all ?? throw new ArgumentNullException(nameof(all));
        }

        public bool TryGet(TKey id, out TValue value)
        {
            value = _lookup(id);
            return !EqualityComparer<TValue>.Default.Equals(value, default(TValue));
        }

        public IEnumerable<TValue> All => _all();
    }
}
