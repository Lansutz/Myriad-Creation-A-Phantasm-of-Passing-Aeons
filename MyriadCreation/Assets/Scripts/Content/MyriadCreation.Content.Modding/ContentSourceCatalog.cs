using System;
using System.Collections.Generic;
using System.IO;
using MyriadCreation.Core;
using UnityEngine;

namespace MyriadCreation.Content.Modding
{
public static class ContentSourceCatalog
{
	/// <summary>
	/// Base 优先，其次 Mods。Mods 布局：一级子目录 = 一个 mod 的根（mod 内再放 Religion/ 等标准目录），
	/// 按目录名稳定排序后依次给 100、101…优先级；没有子目录时才把 Mods 根当作单个平铺 mod（旧布局兼容）。
	/// 有 `mod.json` 的 mod 走启动期校验：清单非法或 `enabled=false` 时跳过并留日志；无清单按兼容模式加载。
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
				int priority = 100;
				for (int i = 0; i < modDirs.Length; i++)
				{
					string id = Path.GetFileName(modDirs[i]);
					if (!TryAcceptMod(id, modDirs[i]))
					{
						continue;
					}
					list.Add(new ContentSource(ContentSourceKind.Mod, id, modDirs[i], priority++));
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
	private static bool TryAcceptMod(string id, string modDir)
	{
		string manifestPath = Path.Combine(modDir, "mod.json");
		if (!File.Exists(manifestPath))
		{
			Debug.LogWarning($"[ContentSource] mod '{id}' 缺 mod.json，按兼容模式加载");
			return true;
		}
		ContentPackageManifest manifest;
		try
		{
			manifest = JsonUtility.FromJson<ContentPackageManifest>(File.ReadAllText(manifestPath));
		}
		catch (Exception ex)
		{
			Debug.LogError($"[ContentSource] mod '{id}' 的 mod.json 解析失败，已跳过：{ex.Message}");
			return false;
		}
		ContentPackageValidationResult result = ContentPackageValidator.Validate(manifest, EnumerateRelativeFiles(modDir));
		if (!result.IsValid)
		{
			Debug.LogError($"[ContentSource] mod '{id}' 清单校验失败，已跳过：{string.Join("; ", result.Errors)}");
			return false;
		}
		if (!manifest.enabled)
		{
			Debug.Log($"[ContentSource] mod '{id}' 已禁用，跳过");
			return false;
		}
		return true;
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
