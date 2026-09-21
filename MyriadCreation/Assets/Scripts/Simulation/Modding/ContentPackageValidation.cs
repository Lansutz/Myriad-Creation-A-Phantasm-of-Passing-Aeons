using System;
using System.Collections.Generic;
using System.IO;

namespace CivilizationEvolution.Simulation.Modding
{
    public sealed class ContentPackageValidationResult
    {
        private readonly List<string> _errors = new List<string>();
        private readonly List<string> _warnings = new List<string>();

        public IReadOnlyList<string> Errors => _errors;
        public IReadOnlyList<string> Warnings => _warnings;
        public bool IsValid => _errors.Count == 0;

        internal void Error(string message) => _errors.Add(message);
        internal void Warning(string message) => _warnings.Add(message);
    }

    /// <summary>
    /// Validates package boundaries before a package is exported or loaded.
    /// This intentionally validates the package contract, not every domain schema.
    /// Domain-specific definition validation remains the responsibility of the
    /// corresponding content provider.
    /// </summary>
    public static class ContentPackageValidator
    {
        public const string CurrentFormatVersion = "1";

        public static ContentPackageValidationResult Validate(
            ContentPackageManifest manifest,
            IEnumerable<string> relativeFiles = null)
        {
            var result = new ContentPackageValidationResult();

            if (manifest == null)
            {
                result.Error("Manifest is required.");
                return result;
            }

            if (string.IsNullOrWhiteSpace(manifest.formatVersion))
                result.Error("formatVersion is required.");
            else if (!string.Equals(manifest.formatVersion, CurrentFormatVersion, StringComparison.Ordinal))
                result.Error($"Unsupported package formatVersion '{manifest.formatVersion}'.");

            ValidatePackageId(manifest.packageId, result);

            if (string.IsNullOrWhiteSpace(manifest.displayName))
                result.Warning("displayName is empty.");

            var declaredTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (manifest.contentTypes != null)
            {
                foreach (var type in manifest.contentTypes)
                {
                    if (string.IsNullOrWhiteSpace(type))
                    {
                        result.Error("contentTypes contains an empty entry.");
                        continue;
                    }

                    if (!IsSupportedContentDirectory(type))
                        result.Error($"Unsupported content type '{type}'.");

                    if (!declaredTypes.Add(type))
                        result.Error($"Duplicate content type '{type}'.");
                }
            }

            if (relativeFiles != null)
            {
                foreach (var relativePath in relativeFiles)
                    ValidateRelativePath(relativePath, result);
            }

            return result;
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
                if (!(char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.'))
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

            var normalized = relativePath.Replace('\\', '/').TrimStart('/');
            if (Path.IsPathRooted(relativePath)
                || normalized == ".."
                || normalized.StartsWith("../", StringComparison.Ordinal)
                || normalized.Contains("/../", StringComparison.Ordinal))
            {
                result.Error($"Package file path escapes package root: '{relativePath}'.");
                return;
            }

            if (string.Equals(normalized, ContentPackageLayout.ManifestFileName, StringComparison.OrdinalIgnoreCase))
                return;

            var separator = normalized.IndexOf('/');
            var directory = separator >= 0 ? normalized.Substring(0, separator) : normalized;
            if (!IsSupportedContentDirectory(directory))
                result.Error($"Package file '{relativePath}' is outside a supported content directory.");
        }

        private static bool IsSupportedContentDirectory(string value)
        {
            for (int i = 0; i < ContentPackageLayout.SupportedContentDirectories.Length; i++)
            {
                if (string.Equals(
                    ContentPackageLayout.SupportedContentDirectories[i],
                    value,
                    StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
