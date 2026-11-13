using System;
using System.Collections.Generic;
using System.IO;
using MyriadCreation.Core;

namespace MyriadCreation.Content.Modding
{
public static class ContentSourceCatalog
{
	/// <summary>
	/// Base 优先，其次 Mods。Mods 布局：一级子目录 = 一个 mod 的根（mod 内再放 Religion/ 等标准目录），
	/// 按目录名稳定排序后依次给 100、101…优先级；没有子目录时才把 Mods 根当作单个平铺 mod（旧布局兼容）。
	/// </summary>
	public static List<ContentSource> CreateDefault(string streamingAssetsRoot)
	{
		List<ContentSource> list = new List<ContentSource>();
		if (string.IsNullOrEmpty(streamingAssetsRoot))
		{
			return list;
		}
		list.Add(new ContentSource(ContentSourceKind.Base, "base", ContentPaths.BaseDir(streamingAssetsRoot), 0));

		string modsDir = ContentPaths.ModsDir(streamingAssetsRoot);
		if (Directory.Exists(modsDir))
		{
			string[] modDirs = Directory.GetDirectories(modsDir);
			Array.Sort(modDirs, StringComparer.OrdinalIgnoreCase);
			if (modDirs.Length > 0)
			{
				for (int i = 0; i < modDirs.Length; i++)
				{
					string id = Path.GetFileName(modDirs[i]);
					list.Add(new ContentSource(ContentSourceKind.Mod, id, modDirs[i], 100 + i));
				}
			}
			else
			{
				list.Add(new ContentSource(ContentSourceKind.Mod, "mods", modsDir, 100));
			}
		}

		list.Sort((ContentSource a, ContentSource b) => a.priority.CompareTo(b.priority));
		return list;
	}
}
}
