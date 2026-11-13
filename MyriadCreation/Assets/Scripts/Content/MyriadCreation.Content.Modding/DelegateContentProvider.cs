using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MyriadCreation.Content.Modding
{
public sealed class DelegateContentProvider<TKey, TValue> : IContentProvider<TKey, TValue>
{
	private readonly Action<string, IContentStore<TKey, TValue>> _loadRoot;

	public string ContentType { get; }

	public DelegateContentProvider(string contentType, Action<string, IContentStore<TKey, TValue>> loadRoot)
	{
		if (string.IsNullOrEmpty(contentType))
		{
			throw new ArgumentException("Content type is required.", "contentType");
		}
		ContentType = contentType;
		_loadRoot = loadRoot ?? throw new ArgumentNullException("loadRoot");
	}

	public void Load(ContentSource source, IContentStore<TKey, TValue> target)
	{
		if (target == null)
		{
			throw new ArgumentNullException("target");
		}
		foreach (string item in EnumerateRoots(source.rootPath))
		{
			try
			{
				_loadRoot(item, target);
			}
			catch (Exception ex)
			{
				Debug.LogWarning((object)("[ContentProvider] " + ContentType + " 加载失败：" + item + "：" + ex.Message));
			}
		}
	}

	private static IEnumerable<string> EnumerateRoots(string root)
	{
		if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
		{
			yield break;
		}
		if (Directory.Exists(Path.Combine(root, "Culture")) || File.Exists(Path.Combine(root, "Race", "RaceDefs.json")) || File.Exists(Path.Combine(root, "Innovation", "Innovations.json")) || File.Exists(Path.Combine(root, "Religion", "Religions.json")) || File.Exists(Path.Combine(root, "Genetics", "Genomes.json")))
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
