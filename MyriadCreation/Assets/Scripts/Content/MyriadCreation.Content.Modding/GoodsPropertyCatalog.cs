using System;
using System.Collections.Generic;
using System.IO;
using MyriadCreation.Core;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Content.Modding
{
public static class GoodsPropertyCatalog
{
	public const string RelativePath = "Goods/GoodsProperties.json";

	private static readonly Dictionary<int, List<GoodsPropertyValue>> _entries = new Dictionary<int, List<GoodsPropertyValue>>();

	public static bool IsLoaded { get; private set; }

	public static int Count => _entries.Count;

	public static void Initialize(string streamingAssetsRoot)
	{
		Reset();
		if (string.IsNullOrEmpty(streamingAssetsRoot) || !Directory.Exists(streamingAssetsRoot))
		{
			IsLoaded = true;
			return;
		}
		LoadFile(ContentPaths.BasePath(streamingAssetsRoot, "Goods/GoodsProperties.json"));
		LoadFile(ContentPaths.ModsPath(streamingAssetsRoot, "Goods/GoodsProperties.json"));
		IsLoaded = true;
		Debug.Log((object)$"[GoodsPropertyCatalog] 加载 {_entries.Count} 个物产的属性");
	}

	public static void Reset()
	{
		_entries.Clear();
		IsLoaded = false;
	}

	public static bool TryGet(int goodsId, out List<GoodsPropertyValue> properties)
	{
		return _entries.TryGetValue(goodsId, out properties);
	}

	public static bool TryGetValue(int goodsId, string propertyId, out float value)
	{
		value = 0f;
		if (!_entries.TryGetValue(goodsId, out var value2))
		{
			return false;
		}
		foreach (GoodsPropertyValue item in value2)
		{
			if (!(item.id != propertyId))
			{
				value = item.value;
				return true;
			}
		}
		return false;
	}

	private static void LoadFile(string path)
	{
		if (!File.Exists(path))
		{
			return;
		}
		try
		{
			GoodsPropertyArtifact goodsPropertyArtifact = JsonUtility.FromJson<GoodsPropertyArtifact>(File.ReadAllText(path));
			if (goodsPropertyArtifact?.entries == null)
			{
				return;
			}
			foreach (GoodsPropertyEntry entry in goodsPropertyArtifact.entries)
			{
				if (entry == null || entry.goodsId < 0 || entry.properties == null)
				{
					continue;
				}
				List<GoodsPropertyValue> list = new List<GoodsPropertyValue>();
				foreach (GoodsPropertyValue property in entry.properties)
				{
					if (!string.IsNullOrEmpty(property.id))
					{
						if (!PropertyDefinitionCatalog.IsKnown(property.id))
						{
							Debug.LogWarning((object)$"[GoodsPropertyCatalog] 物产 {entry.goodsId} 引用了未定义属性 {property.id}");
						}
						else
						{
							list.Add(property);
						}
					}
				}
				_entries[entry.goodsId] = list;
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning((object)("[GoodsPropertyCatalog] 读取异常 " + path + ": " + ex.Message));
		}
	}
}
}
