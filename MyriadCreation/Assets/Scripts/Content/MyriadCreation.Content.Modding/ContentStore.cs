using System;
using System.Collections.Generic;

namespace MyriadCreation.Content.Modding
{
public sealed class ContentStore<TKey, TValue> : IContentStore<TKey, TValue>, IContentResolver<TKey, TValue>
{
	private readonly Dictionary<TKey, TValue> _values = new Dictionary<TKey, TValue>();

	public IEnumerable<TValue> All => _values.Values;

	public bool TryGet(TKey id, out TValue value)
	{
		return _values.TryGetValue(id, out value);
	}

	public void Set(TKey id, TValue value)
	{
		if (EqualityComparer<TKey>.Default.Equals(id, default))
		{
			throw new ArgumentException("Content id cannot be the default key.", "id");
		}
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		_values[id] = value;
	}

	public bool Remove(TKey id)
	{
		return _values.Remove(id);
	}

	public void Clear()
	{
		_values.Clear();
	}
}
}
