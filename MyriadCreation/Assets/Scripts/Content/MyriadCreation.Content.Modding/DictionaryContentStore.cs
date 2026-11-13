using System;
using System.Collections.Generic;

namespace MyriadCreation.Content.Modding
{
public sealed class DictionaryContentStore<TKey, TValue> : IContentStore<TKey, TValue>, IContentResolver<TKey, TValue>
{
	private readonly Dictionary<TKey, TValue> _values;

	private readonly bool _allowDefaultKey;

	public IEnumerable<TValue> All => _values.Values;

	/// <param name="allowDefaultKey">
	/// 是否允许默认键（int 即 0）。默认禁止——它能抓住"内容条目忘了写 id"的错误。
	/// 但少数域**合法使用 0**：`Biome`（`Biomes.json` 从 biomeId=0「冲积平原」开始），
	/// 这类 store 需显式开启，否则第一个条目就会抛异常、整张表加载失败。
	/// </param>
	public DictionaryContentStore(Dictionary<TKey, TValue> values, bool allowDefaultKey = false)
	{
		_values = values ?? throw new ArgumentNullException("values");
		_allowDefaultKey = allowDefaultKey;
	}

	public bool TryGet(TKey id, out TValue value)
	{
		return _values.TryGetValue(id, out value);
	}

	public void Set(TKey id, TValue value)
	{
		if (!_allowDefaultKey && EqualityComparer<TKey>.Default.Equals(id, default))
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
