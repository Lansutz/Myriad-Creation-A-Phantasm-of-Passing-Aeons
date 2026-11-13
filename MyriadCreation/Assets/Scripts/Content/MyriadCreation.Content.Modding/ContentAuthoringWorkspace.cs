using System;
using System.Collections.Generic;

namespace MyriadCreation.Content.Modding
{
public sealed class ContentAuthoringWorkspace : IContentAuthoringWorkspace
{
	private readonly Dictionary<string, Dictionary<string, string>> _definitions = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

	private readonly Dictionary<string, string> _files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	public ContentPackageManifest Manifest { get; private set; } = new ContentPackageManifest();

	public void SetManifest(ContentPackageManifest manifest)
	{
		Manifest = manifest ?? new ContentPackageManifest();
	}

	public void Upsert(string contentType, string id, string serializedDefinition)
	{
		if (string.IsNullOrWhiteSpace(contentType))
		{
			throw new ArgumentException("contentType");
		}
		if (string.IsNullOrWhiteSpace(id))
		{
			throw new ArgumentException("id");
		}
		if (serializedDefinition == null)
		{
			throw new ArgumentNullException("serializedDefinition");
		}
		if (!_definitions.TryGetValue(contentType, out var value))
		{
			value = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			_definitions[contentType] = value;
		}
		value[id] = serializedDefinition;
		if (!Manifest.contentTypes.Contains(contentType))
		{
			Manifest.contentTypes.Add(contentType);
		}
	}

	public void UpsertFile(string relativePath, string content)
	{
		if (string.IsNullOrWhiteSpace(relativePath))
		{
			throw new ArgumentException("relativePath");
		}
		if (relativePath.Contains(".."))
		{
			throw new ArgumentException("Path traversal is not allowed.", "relativePath");
		}
		_files[relativePath.Replace('\\', '/')] = content ?? string.Empty;
	}

	public bool Remove(string contentType, string id)
	{
		if (_definitions.TryGetValue(contentType, out var value))
		{
			return value.Remove(id);
		}
		return false;
	}

	public IReadOnlyDictionary<string, string> GetFiles()
	{
		return _files;
	}

	public IReadOnlyDictionary<string, string> GetDefinitions(string contentType)
	{
		if (_definitions.TryGetValue(contentType, out var value))
		{
			return value;
		}
		return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	}
}
}
