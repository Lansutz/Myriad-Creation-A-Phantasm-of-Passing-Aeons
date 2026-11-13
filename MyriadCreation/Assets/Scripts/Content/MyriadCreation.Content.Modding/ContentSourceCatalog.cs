using System;
using System.Collections.Generic;
using System.IO;
using MyriadCreation.Core;
using UnityEngine;

namespace MyriadCreation.Content.Modding
{
public static class ContentSourceCatalog
{
	private sealed class ModEntry
	{
		public string id;
		public string dir;
		public ContentPackageManifest manifest;
	}

	/// <summary>
	/// Base 优先，其次 Mods。Mods 布局：一级子目录 = 一个 mod 的根（mod 内再放 Religion/ 等标准目录）。
	/// 有 `mod.json` 的 mod 走启动期校验：清单非法或 `enabled=false` 时跳过并留日志；无清单按兼容模式加载。
	/// 依赖按 `dependencies.packageId` 做拓扑排序；缺依赖/环只告警并按目录名稳定排序兜底。
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
				List<ModEntry> accepted = new List<ModEntry>();
				for (int i = 0; i < modDirs.Length; i++)
				{
					ModEntry entry = TryAcceptMod(modDirs[i]);
					if (entry != null)
					{
						accepted.Add(entry);
					}
				}
				List<ModEntry> ordered = SortByDependencies(accepted);
				int priority = 100;
				for (int i = 0; i < ordered.Count; i++)
				{
					list.Add(new ContentSource(ContentSourceKind.Mod, ordered[i].id, ordered[i].dir, priority++));
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

	/// <summary>启动期包校验：无清单=兼容加载；清单非法或禁用=跳过。</summary>
	private static ModEntry TryAcceptMod(string modDir)
	{
		string id = Path.GetFileName(modDir);
		string manifestPath = Path.Combine(modDir, "mod.json");
		if (!File.Exists(manifestPath))
		{
			Debug.LogWarning($"[ContentSource] mod '{id}' 缺 mod.json，按兼容模式加载");
			return new ModEntry { id = id, dir = modDir, manifest = null };
		}
		ContentPackageManifest manifest;
		try
		{
			manifest = JsonUtility.FromJson<ContentPackageManifest>(File.ReadAllText(manifestPath));
		}
		catch (Exception ex)
		{
			Debug.LogError($"[ContentSource] mod '{id}' 的 mod.json 解析失败，已跳过：{ex.Message}");
			return null;
		}
		ContentPackageValidationResult result = ContentPackageValidator.Validate(manifest, EnumerateRelativeFiles(modDir));
		if (!result.IsValid)
		{
			Debug.LogError($"[ContentSource] mod '{id}' 清单校验失败，已跳过：{string.Join("; ", result.Errors)}");
			return null;
		}
		if (!manifest.enabled)
		{
			Debug.Log($"[ContentSource] mod '{id}' 已禁用，跳过");
			return null;
		}
		return new ModEntry { id = id, dir = modDir, manifest = manifest };
	}

	/// <summary>依赖优先的稳定拓扑排序：依赖先加载；缺依赖/环告警后按目录名顺序处理。</summary>
	private static List<ModEntry> SortByDependencies(List<ModEntry> entries)
	{
		if (entries.Count <= 1)
		{
			return entries;
		}
		Dictionary<string, ModEntry> byPackage = new Dictionary<string, ModEntry>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < entries.Count; i++)
		{
			string key = entries[i].manifest?.packageId;
			if (!string.IsNullOrEmpty(key) && !byPackage.ContainsKey(key))
			{
				byPackage[key] = entries[i];
			}
		}

		List<ModEntry> ordered = new List<ModEntry>(entries.Count);
		HashSet<string> visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < entries.Count; i++)
		{
			Visit(entries[i], byPackage, visiting, visited, ordered);
		}
		return ordered;
	}

	private static void Visit(
		ModEntry entry,
		Dictionary<string, ModEntry> byPackage,
		HashSet<string> visiting,
		HashSet<string> visited,
		List<ModEntry> ordered)
	{
		string id = entry.manifest?.packageId;
		if (string.IsNullOrEmpty(id))
		{
			if (!visited.Contains(entry.id))
			{
				visited.Add(entry.id);
				ordered.Add(entry);
			}
			return;
		}
		if (visited.Contains(id))
		{
			return;
		}
		if (!visiting.Add(id))
		{
			Debug.LogWarning($"[ContentSource] mod 依赖成环：'{id}'，按当前顺序继续");
			return;
		}
		List<string> dependencies = entry.manifest.dependencies;
		if (dependencies != null)
		{
			for (int i = 0; i < dependencies.Count; i++)
			{
				string dependency = dependencies[i];
				if (string.IsNullOrEmpty(dependency))
				{
					continue;
				}
				if (!byPackage.TryGetValue(dependency, out ModEntry dependencyEntry))
				{
					Debug.LogWarning($"[ContentSource] mod '{id}' 依赖的 '{dependency}' 不在 Mods 中，忽略该依赖");
					continue;
				}
				Visit(dependencyEntry, byPackage, visiting, visited, ordered);
			}
		}
		visiting.Remove(id);
		visited.Add(id);
		ordered.Add(entry);
	}

	private static List<string> EnumerateRelativeFiles(string modDir)
	{
		List<string> list = new List<string>();
		string[] files = Directory.GetFiles(modDir, "*", SearchOption.AllDirectories);
		for (int i = 0; i < files.Length; i++)
		{
			string relative = files[i].Substring(modDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			list.Add(relative.Replace('\\', '/'));
		}
		return list;
	}
}
}
