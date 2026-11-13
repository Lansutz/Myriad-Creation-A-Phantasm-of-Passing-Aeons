namespace MyriadCreation.Content.Modding
{
public interface IContentProviderRegistration
{
	string ContentType { get; }

	void Load(ContentSource source);
}
}
