using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MyriadCreation.Core
{
public static class Localization
{
	[Serializable]
	private class LocalizationWrapper
	{
		public List<LocalizationEntry> entries = new List<LocalizationEntry>();
	}

	[Serializable]
	private class LocalizationEntry
	{
		public string key = string.Empty;

		public string value = string.Empty;
	}

	private static readonly Dictionary<string, string> _table = new Dictionary<string, string>();

	public static string CurrentLanguage { get; private set; } = "zh-Hans";

	public static bool IsLoaded { get; private set; } = false;

	public static void Initialize(string language = "zh-Hans")
	{
		if (!IsLoaded || !(CurrentLanguage == language))
		{
			CurrentLanguage = language;
			_table.Clear();
			string streamingAssetsPath = Application.streamingAssetsPath;
			if (Directory.Exists(streamingAssetsPath))
			{
				LoadLanguageFile(Path.Combine(streamingAssetsPath, ContentPaths.Base, "Localization", language + ".json"));
				LoadLanguageFile(Path.Combine(streamingAssetsPath, ContentPaths.Mods, "Localization", language + ".json"));
			}
			IsLoaded = true;
			Debug.Log((object)$"[Localization] 加载完成：{language}，{_table.Count} 键");
		}
	}

	public static void Reset()
	{
		IsLoaded = false;
		_table.Clear();
	}

	public static string Get(string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			return "";
		}
		if (!_table.TryGetValue(key, out var value))
		{
			return key;
		}
		return value;
	}

	public static string Get(string key, string fallback)
	{
		if (string.IsNullOrEmpty(key))
		{
			return fallback ?? "";
		}
		string text;
		if (!_table.TryGetValue(key, out var value))
		{
			text = fallback;
			if (text == null)
			{
				return key;
			}
		}
		else
		{
			text = value;
		}
		return text;
	}

	public static bool Has(string key)
	{
		if (!string.IsNullOrEmpty(key))
		{
			return _table.ContainsKey(key);
		}
		return false;
	}

	private static void LoadLanguageFile(string path)
	{
		if (!File.Exists(path))
		{
			return;
		}
		try
		{
			LocalizationWrapper localizationWrapper = JsonUtility.FromJson<LocalizationWrapper>(File.ReadAllText(path));
			if (localizationWrapper == null || localizationWrapper.entries == null)
			{
				return;
			}
			foreach (LocalizationEntry entry in localizationWrapper.entries)
			{
				if (!string.IsNullOrEmpty(entry.key))
				{
					_table[entry.key] = entry.value;
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning((object)("[Localization] 加载失败：" + path + " — " + ex.Message));
		}
	}
}
}
