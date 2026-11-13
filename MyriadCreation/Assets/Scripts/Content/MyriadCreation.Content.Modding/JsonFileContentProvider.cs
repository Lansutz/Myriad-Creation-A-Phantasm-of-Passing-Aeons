using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MyriadCreation.Content.Modding
{
public sealed class JsonFileContentProvider<TKey, TValue, TWrapper> : IContentProvider<TKey, TValue>
{
	private readonly string _relativeFile;

	private readonly Func<string, TWrapper> _deserialize;

	private readonly Func<TWrapper, IEnumerable<TValue>> _items;

	private readonly Func<TValue, TKey> _key;

	public string ContentType { get; }

	public JsonFileContentProvider(string contentType, string relativeFile, Func<string, TWrapper> deserialize, Func<TWrapper, IEnumerable<TValue>> items, Func<TValue, TKey> key)
	{
		if (string.IsNullOrEmpty(contentType))
		{
			throw new ArgumentException("Content type is required.", "contentType");
		}
		ContentType = contentType;
		_relativeFile = relativeFile ?? throw new ArgumentNullException("relativeFile");
		_deserialize = deserialize ?? throw new ArgumentNullException("deserialize");
		_items = items ?? throw new ArgumentNullException("items");
		_key = key ?? throw new ArgumentNullException("key");
	}

	public void Load(ContentSource source, IContentStore<TKey, TValue> target)
	{
		if (target == null)
		{
			throw new ArgumentNullException("target");
		}
		foreach (string item in EnumerateRoots(source.rootPath))
		{
			string text = Path.Combine(item, _relativeFile);
			if (!File.Exists(text))
			{
				continue;
			}
			try
			{
				TWrapper val = _deserialize(File.ReadAllText(text));
				if (val == null)
				{
					continue;
				}
				IEnumerable<TValue> enumerable = _items(val);
				if (enumerable == null)
				{
					continue;
				}
				foreach (TValue item2 in enumerable)
				{
					if (item2 != null)
					{
						TKey val2 = _key(item2);
						if (!EqualityComparer<TKey>.Default.Equals(val2, default))
						{
							target.Set(val2, item2);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.LogWarning((object)("[ContentProvider] " + ContentType + " 加载失败：" + text + "：" + ex.Message));
			}
		}
	}

	private static IEnumerable<string> EnumerateRoots(string root)
	{
		if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
		{
			yield break;
		}
		if (Directory.Exists(Path.Combine(root, "Culture")) || Directory.Exists(Path.Combine(root, "Race")) || Directory.Exists(Path.Combine(root, "Innovation")) || Directory.Exists(Path.Combine(root, "Religion")))
		{
			yield return root;
			yield break;
		}
		string[] directories = Directory.GetDirectories(root);
		Array.Sort(directories, StringComparer.Ordinal);
		string[] array = directories;
		for (int i = 0; i < array.Length; i++)
		{
			yield return array[i];
		}
	}
}
}
