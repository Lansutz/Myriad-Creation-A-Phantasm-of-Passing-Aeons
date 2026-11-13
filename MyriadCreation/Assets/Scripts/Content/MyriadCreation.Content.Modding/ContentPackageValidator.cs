using System;
using System.Collections.Generic;
using System.IO;

namespace MyriadCreation.Content.Modding
{
public static class ContentPackageValidator
{
	public const string CurrentFormatVersion = "1";

	public static ContentPackageValidationResult Validate(ContentPackageManifest manifest, IEnumerable<string> relativeFiles = null)
	{
		ContentPackageValidationResult contentPackageValidationResult = new ContentPackageValidationResult();
		if (manifest == null)
		{
			contentPackageValidationResult.Error("Manifest is required.");
			return contentPackageValidationResult;
		}
		if (string.IsNullOrWhiteSpace(manifest.formatVersion))
		{
			contentPackageValidationResult.Error("formatVersion is required.");
		}
		else if (!string.Equals(manifest.formatVersion, "1", StringComparison.Ordinal))
		{
			contentPackageValidationResult.Error("Unsupported package formatVersion '" + manifest.formatVersion + "'.");
		}
		ValidatePackageId(manifest.packageId, contentPackageValidationResult);
		if (string.IsNullOrWhiteSpace(manifest.displayName))
		{
			contentPackageValidationResult.Warning("displayName is empty.");
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (manifest.contentTypes != null)
		{
			foreach (string contentType in manifest.contentTypes)
			{
				if (string.IsNullOrWhiteSpace(contentType))
				{
					contentPackageValidationResult.Error("contentTypes contains an empty entry.");
					continue;
				}
				if (!IsSupportedContentDirectory(contentType))
				{
					contentPackageValidationResult.Error("Unsupported content type '" + contentType + "'.");
				}
				if (!hashSet.Add(contentType))
				{
					contentPackageValidationResult.Error("Duplicate content type '" + contentType + "'.");
				}
			}
		}
		if (relativeFiles != null)
		{
			foreach (string relativeFile in relativeFiles)
			{
				ValidateRelativePath(relativeFile, contentPackageValidationResult);
			}
		}
		return contentPackageValidationResult;
	}

	private static void ValidatePackageId(string packageId, ContentPackageValidationResult result)
	{
		if (string.IsNullOrWhiteSpace(packageId))
		{
			result.Error("packageId is required.");
			return;
		}
		foreach (char c in packageId)
		{
			if (!char.IsLetterOrDigit(c) && c != '_' && c != '-' && c != '.')
			{
				result.Error($"packageId '{packageId}' contains invalid character '{c}'.");
				break;
			}
		}
	}

	private static void ValidateRelativePath(string relativePath, ContentPackageValidationResult result)
	{
		if (string.IsNullOrWhiteSpace(relativePath))
		{
			result.Error("Package contains an empty relative file path.");
			return;
		}
		string text = relativePath.Replace('\\', '/').TrimStart('/');
		if (Path.IsPathRooted(relativePath) || text == ".." || text.StartsWith("../", StringComparison.Ordinal) || text.Contains("/../", StringComparison.Ordinal))
		{
			result.Error("Package file path escapes package root: '" + relativePath + "'.");
		}
		else if (!string.Equals(text, "mod.json", StringComparison.OrdinalIgnoreCase))
		{
			int num = text.IndexOf('/');
			if (!IsSupportedContentDirectory((num >= 0) ? text.Substring(0, num) : text))
			{
				result.Error("Package file '" + relativePath + "' is outside a supported content directory.");
			}
		}
	}

	private static bool IsSupportedContentDirectory(string value)
	{
		for (int i = 0; i < ContentPackageLayout.SupportedContentDirectories.Length; i++)
		{
			if (string.Equals(ContentPackageLayout.SupportedContentDirectories[i], value, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}
}
}
