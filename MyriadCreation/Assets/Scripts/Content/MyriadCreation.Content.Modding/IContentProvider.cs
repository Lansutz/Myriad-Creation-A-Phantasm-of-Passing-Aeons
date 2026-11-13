namespace MyriadCreation.Content.Modding
{
public interface IContentProvider<TKey, TValue>
{
	string ContentType { get; }

	void Load(ContentSource source, IContentStore<TKey, TValue> target);
}
}
