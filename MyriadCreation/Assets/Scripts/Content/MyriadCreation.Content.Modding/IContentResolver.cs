using System.Collections.Generic;

namespace MyriadCreation.Content.Modding
{
public interface IContentResolver<TKey, TValue>
{
	IEnumerable<TValue> All { get; }

	bool TryGet(TKey id, out TValue value);
}
}
