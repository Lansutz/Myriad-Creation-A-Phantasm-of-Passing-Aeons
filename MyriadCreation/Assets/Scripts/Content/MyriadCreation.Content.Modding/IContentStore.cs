namespace MyriadCreation.Content.Modding
{
public interface IContentStore<TKey, TValue> : IContentResolver<TKey, TValue>
{
	void Set(TKey id, TValue value);

	bool Remove(TKey id);

	void Clear();
}
}
