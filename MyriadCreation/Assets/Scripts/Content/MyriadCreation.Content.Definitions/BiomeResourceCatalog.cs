using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MyriadCreation.Content.Definitions
{
[Serializable]
public sealed class BiomeResourceEntry
{
	public int goodsId;
	public float probability;
	public float abundanceMultiplier = 1f;
}

[Serializable]
public sealed class BiomeResourceGroup
{
	public int biome;
	public List<BiomeResourceEntry> resources = new List<BiomeResourceEntry>();
}

[Serializable]
internal sealed class BiomeResourceFile
{
	public List<BiomeResourceGroup> biomes = new List<BiomeResourceGroup>();
}

/// <summary>
/// 群系→资源数据表（借鉴 FantasyMapSimulator）：概率/丰度修正按群系内容化，
/// ResourceGenerator 只查表；表缺失时回退 GoodsDef.originBiomes 旧口径。
/// </summary>
public static class BiomeResourceCatalog
{
	private static bool _attempted;
	private static Dictionary<int, Dictionary<int, BiomeResourceEntry>> _byBiome;

	public static bool IsLoaded => _byBiome != null && _byBiome.Count > 0;

	public static bool EnsureLoaded()
	{
		if (!_attempted) LoadFromFile(ResolveDefaultPath());
		return IsLoaded;
	}

	public static string ResolveDefaultPath()
	{
		string root = Application.streamingAssetsPath;
		return string.IsNullOrEmpty(root) ? "" : Path.Combine(root, "Base/Biome/BiomeResources.json");
	}

	public static bool LoadFromFile(string path)
	{
		_attempted = true;
		_byBiome = null;
		if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
		try
		{
			BiomeResourceFile file = JsonUtility.FromJson<BiomeResourceFile>(File.ReadAllText(path));
			if (file?.biomes == null || file.biomes.Count == 0) return false;
			Dictionary<int, Dictionary<int, BiomeResourceEntry>> map = new Dictionary<int, Dictionary<int, BiomeResourceEntry>>();
			foreach (BiomeResourceGroup group in file.biomes)
			{
				if (group?.resources == null) continue;
				Dictionary<int, BiomeResourceEntry> entries = new Dictionary<int, BiomeResourceEntry>();
				foreach (BiomeResourceEntry entry in group.resources)
				{
					if (entry != null && entry.goodsId > 0) entries[entry.goodsId] = entry;
				}
				map[group.biome] = entries;
			}
			_byBiome = map;
			return IsLoaded;
		}
		catch (Exception ex)
		{
			Debug.LogError("[BiomeResourceCatalog] 读取 BiomeResources.json 失败：" + ex.Message);
			return false;
		}
	}

	public static bool TryGet(int biome, int goodsId, out BiomeResourceEntry entry)
	{
		entry = null;
		return _byBiome != null
			&& _byBiome.TryGetValue(biome, out Dictionary<int, BiomeResourceEntry> entries)
			&& entries.TryGetValue(goodsId, out entry);
	}
}
}
