using System.Collections.Generic;

namespace MyriadCreation.Content.Modding
{
public sealed class ContentProviderCatalog
{
	private readonly List<IContentProviderRegistration> _providers = new List<IContentProviderRegistration>();

	public int Count => _providers.Count;

	public void Add<TKey, TValue>(IContentProvider<TKey, TValue> provider, IContentStore<TKey, TValue> target)
	{
		_providers.Add(new ContentProviderRegistration<TKey, TValue>(provider, target));
	}

	public void Load(ContentSource source)
	{
		foreach (IContentProviderRegistration provider in _providers)
		{
			provider.Load(source);
		}
	}
}
}
