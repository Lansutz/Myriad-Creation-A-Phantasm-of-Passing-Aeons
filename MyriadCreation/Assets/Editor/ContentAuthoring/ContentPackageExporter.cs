#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using MyriadCreation.Simulation.Modding;

namespace MyriadCreation.Editor.ContentAuthoring
{
    /// <summary>
    /// Exports editor-authored definitions into the exact StreamingAssets/Mods package
    /// layout consumed by the runtime ContentRegistry.
    /// </summary>
    public static class ContentPackageExporter
    {
        public static string Export(
            IContentAuthoringWorkspace workspace,
            string modsRoot,
            bool overwrite = true)
        {
            if (workspace == null) throw new ArgumentNullException(nameof(workspace));
            if (workspace.Manifest == null) throw new InvalidOperationException("Missing content manifest.");
            if (string.IsNullOrWhiteSpace(workspace.Manifest.packageId))
                throw new InvalidOperationException("Package id is required.");

            string packageRoot = Path.Combine(modsRoot, workspace.Manifest.packageId);
            Directory.CreateDirectory(packageRoot);

            WriteJson(
                Path.Combine(packageRoot, ContentPackageLayout.ManifestFileName),
                workspace.Manifest,
                overwrite);

            // File-level export is intentional: existing runtime loaders already define
            // canonical filenames/wrapper formats. The editor must preserve those formats
            // instead of inventing a second serialization model.
            foreach (var pair in workspace.GetFiles())
            {
                string relativePath = pair.Key.Replace('/', Path.DirectorySeparatorChar);
                WriteText(Path.Combine(packageRoot, relativePath), pair.Value, overwrite);
            }

            AssetDatabase.Refresh();
            return packageRoot;
        }

        private static void WriteJson<T>(string path, T value, bool overwrite)
        {
            WriteText(path, JsonUtility.ToJson(value, true), overwrite);
        }

        private static void WriteText(string path, string content, bool overwrite)
        {
            if (!overwrite && File.Exists(path))
                throw new IOException("File already exists: " + path);

            File.WriteAllText(path, content ?? string.Empty);
        }

        private static string SanitizeFileName(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');
            return value;
        }
    }
}
