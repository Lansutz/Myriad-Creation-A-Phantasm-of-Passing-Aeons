using System;
using System.Collections.Generic;

namespace MyriadCreation.Content.Modding
{
public sealed class ContentResolver<TKey, TValue> : IContentResolver<TKey, TValue>
{
	private readonly Func<TKey, TValue> _lookup;

	private readonly Func<IEnumerable<TValue>> _all;

	public IEnumerable<TValue> All => _all();

	public ContentResolver(Func<TKey, TValue> lookup, Func<IEnumerable<TValue>> all)
	{
		_lookup = lookup ?? throw new ArgumentNullException("lookup");
		_all = all ?? throw new ArgumentNullException("all");
	}

	public bool TryGet(TKey id, out TValue value)
	{
		value = _lookup(id);
		return !EqualityComparer<TValue>.Default.Equals(value, default);
	}
}
}
