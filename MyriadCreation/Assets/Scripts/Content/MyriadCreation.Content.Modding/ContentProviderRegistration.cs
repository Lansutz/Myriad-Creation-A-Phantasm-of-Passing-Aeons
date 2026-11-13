using System;

namespace MyriadCreation.Content.Modding
{
public sealed class ContentProviderRegistration<TKey, TValue> : IContentProviderRegistration
{
	private readonly IContentProvider<TKey, TValue> _provider;

	private readonly IContentStore<TKey, TValue> _target;

	public string ContentType => _provider.ContentType;

	public ContentProviderRegistration(IContentProvider<TKey, TValue> provider, IContentStore<TKey, TValue> target)
	{
		_provider = provider ?? throw new ArgumentNullException("provider");
		_target = target ?? throw new ArgumentNullException("target");
	}

	public void Load(ContentSource source)
	{
		_provider.Load(source, _target);
	}
}
}
