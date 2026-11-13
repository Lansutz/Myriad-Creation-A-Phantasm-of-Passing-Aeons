using System;
using System.Collections.Generic;
using System.IO;
using MyriadCreation.Core;
using UnityEngine;

namespace MyriadCreation.Content.Modding
{
public static class LanguageFamilyCatalog
{
	public const string RelativePath = "_generated/language-families.json";

	private static readonly Dictionary<string, LanguageFamilyNode> _nodes = new Dictionary<string, LanguageFamilyNode>();

	public static bool IsLoaded { get; private set; }

	public static int Count => _nodes.Count;

	public static IReadOnlyDictionary<string, LanguageFamilyNode> All => _nodes;

	public static LanguageFamilySource Source { get; private set; }

	public static void Initialize(string streamingAssetsRoot)
	{
		Reset();
		if (string.IsNullOrEmpty(streamingAssetsRoot) || !Directory.Exists(streamingAssetsRoot))
		{
			Debug.LogWarning((object)("[LanguageFamilyCatalog] StreamingAssets 不存在：" + streamingAssetsRoot));
			IsLoaded = true;
			return;
		}
		LoadFile(ContentPaths.BasePath(streamingAssetsRoot, "_generated/language-families.json"));
		LoadFile(ContentPaths.ModsPath(streamingAssetsRoot, "_generated/language-families.json"));
		int num = Validate();
		IsLoaded = true;
		Debug.Log((object)($"[LanguageFamilyCatalog] 加载 {_nodes.Count} 个节点" + ((num > 0) ? $"，丢弃 {num} 个非法节点" : string.Empty) + ((Source != null) ? ("（" + Source.dataset + " " + Source.version + "，" + Source.license + "）") : string.Empty)));
	}

	public static void Reset()
	{
		_nodes.Clear();
		Source = null;
		IsLoaded = false;
	}

	public static bool TryGet(string id, out LanguageFamilyNode node)
	{
		if (string.IsNullOrEmpty(id))
		{
			node = null;
			return false;
		}
		return _nodes.TryGetValue(id, out node);
	}

	public static string GetRootFamilyId(string id)
	{
		if (!_nodes.TryGetValue(id, out var value))
		{
			return null;
		}
		int num = 0;
		while (value != null && !string.IsNullOrEmpty(value.parent) && num++ < 10000)
		{
			if (!_nodes.TryGetValue(value.parent, out value))
			{
				return null;
			}
		}
		return value?.id;
	}

	public static List<string> GetAncestors(string id, bool includeSelf = false)
	{
		List<string> list = new List<string>();
		if (!_nodes.TryGetValue(id, out var value))
		{
			return list;
		}
		if (includeSelf)
		{
			list.Add(value.id);
		}
		int num = 0;
		while (value != null && !string.IsNullOrEmpty(value.parent) && num++ < 10000 && _nodes.TryGetValue(value.parent, out value))
		{
			list.Add(value.id);
		}
		return list;
	}

	private static void LoadFile(string path)
	{
		if (!File.Exists(path))
		{
			return;
		}
		try
		{
			LanguageFamilyArtifact languageFamilyArtifact = JsonUtility.FromJson<LanguageFamilyArtifact>(File.ReadAllText(path));
			if (languageFamilyArtifact == null)
			{
				Debug.LogWarning((object)("[LanguageFamilyCatalog] 解析失败：" + path));
				return;
			}
			if (languageFamilyArtifact.source != null)
			{
				Source = languageFamilyArtifact.source;
			}
			if (languageFamilyArtifact.nodes == null)
			{
				return;
			}
			foreach (LanguageFamilyNode node in languageFamilyArtifact.nodes)
			{
				if (node != null && !string.IsNullOrEmpty(node.id))
				{
					_nodes[node.id] = node;
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning((object)("[LanguageFamilyCatalog] 读取异常 " + path + ": " + ex.Message));
		}
	}

	private static int Validate()
	{
		int num = 0;
		bool flag = true;
		while (flag)
		{
			flag = false;
			List<string> list = new List<string>();
			foreach (KeyValuePair<string, LanguageFamilyNode> node in _nodes)
			{
				if (!string.IsNullOrEmpty(node.Value.parent) && !_nodes.ContainsKey(node.Value.parent))
				{
					list.Add(node.Key);
				}
			}
			foreach (string item in list)
			{
				_nodes.Remove(item);
				num++;
				flag = true;
			}
		}
		List<string> list2 = new List<string>();
		foreach (string key in _nodes.Keys)
		{
			if (HasCycle(key))
			{
				list2.Add(key);
			}
		}
		foreach (string item2 in list2)
		{
			_nodes.Remove(item2);
			num++;
		}
		if (num > 0)
		{
			Debug.LogWarning((object)$"[LanguageFamilyCatalog] 丢弃 {num} 个父节点缺失/成环节点");
		}
		return num;
	}

	private static bool HasCycle(string start)
	{
		HashSet<string> hashSet = new HashSet<string>();
		string text = start;
		int num = 0;
		while (!string.IsNullOrEmpty(text) && num++ < 10000)
		{
			if (!hashSet.Add(text))
			{
				return true;
			}
			if (!_nodes.TryGetValue(text, out var value))
			{
				return false;
			}
			text = value.parent;
		}
		return false;
	}
}
}
