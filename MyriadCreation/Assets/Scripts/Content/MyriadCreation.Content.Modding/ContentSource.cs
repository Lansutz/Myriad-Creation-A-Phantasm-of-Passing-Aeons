using System.IO;

namespace MyriadCreation.Content.Modding
{
public readonly struct ContentSource(ContentSourceKind kind, string id, string rootPath, int priority)
{
	public readonly ContentSourceKind kind = kind;

	public readonly string id = id ?? string.Empty;

	public readonly string rootPath = rootPath ?? string.Empty;

	public readonly int priority = priority;

	public bool Exists
	{
		get
		{
			if (!string.IsNullOrEmpty(rootPath))
			{
				return Directory.Exists(rootPath);
			}
			return false;
		}
	}
}
}
