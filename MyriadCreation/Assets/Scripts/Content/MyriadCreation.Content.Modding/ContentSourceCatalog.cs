using System.Collections.Generic;
using System.IO;
using MyriadCreation.Core;

namespace MyriadCreation.Content.Modding
{
public static class ContentSourceCatalog
{
	public static List<ContentSource> CreateDefault(string streamingAssetsRoot)
	{
		List<ContentSource> list = new List<ContentSource>();
		if (string.IsNullOrEmpty(streamingAssetsRoot))
		{
			return list;
		}
		list.Add(new ContentSource(ContentSourceKind.Base, "base", ContentPaths.BaseDir(streamingAssetsRoot), 0));
		list.Add(new ContentSource(ContentSourceKind.Mod, "mods", ContentPaths.ModsDir(streamingAssetsRoot), 100));
		list.Sort((ContentSource a, ContentSource b) => a.priority.CompareTo(b.priority));
		return list;
	}
}
}
