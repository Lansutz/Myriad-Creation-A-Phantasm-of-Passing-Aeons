using System;
using System.Collections.Generic;
using System.IO;
using MyriadCreation.Core;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Content.Modding
{
public static class PropertyDefinitionCatalog
{
	public const string RelativePath = "Properties/PropertyDefinitions.json";

	private static readonly Dictionary<string, PropertyDefinition> _definitions = new Dictionary<string, PropertyDefinition>();

	public static bool IsLoaded { get; private set; }

	public static int Count => _definitions.Count;

	public static IReadOnlyDictionary<string, PropertyDefinition> All => _definitions;

	public static void Initialize(string streamingAssetsRoot)
	{
		Reset();
		if (string.IsNullOrEmpty(streamingAssetsRoot) || !Directory.Exists(streamingAssetsRoot))
		{
			IsLoaded = true;
			return;
		}
		LoadFile(ContentPaths.BasePath(streamingAssetsRoot, "Properties/PropertyDefinitions.json"));
		LoadFile(ContentPaths.ModsPath(streamingAssetsRoot, "Properties/PropertyDefinitions.json"));
		IsLoaded = true;
		Debug.Log((object)$"[PropertyDefinitionCatalog] 加载 {_definitions.Count} 条属性定义");
	}

	public static void Reset()
	{
		_definitions.Clear();
		IsLoaded = false;
	}

	public static bool TryGet(string id, out PropertyDefinition definition)
	{
		return _definitions.TryGetValue(id, out definition);
	}

	public static bool IsKnown(string id)
	{
		if (!string.IsNullOrEmpty(id))
		{
			return _definitions.ContainsKey(id);
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
			PropertyDefinitionArtifact propertyDefinitionArtifact = JsonUtility.FromJson<PropertyDefinitionArtifact>(File.ReadAllText(path));
			if (propertyDefinitionArtifact?.properties == null)
			{
				return;
			}
			foreach (PropertyDefinition property in propertyDefinitionArtifact.properties)
			{
				if (property != null && !string.IsNullOrEmpty(property.id))
				{
					_definitions[property.id] = property;
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning((object)("[PropertyDefinitionCatalog] 读取异常 " + path + ": " + ex.Message));
		}
	}
}
}
