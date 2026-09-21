using System;
using System.Collections.Generic;
using System.IO;

namespace CivilizationEvolution.Simulation.Modding
{
    /// <summary>
    /// All authoring/runtime content enters the game through a named source.
    /// Base, Mod, Scenario and Editor are sources, not different runtime data models.
    /// </summary>
    public enum ContentSourceKind
    {
        Base,
        Mod,
        Scenario,
        Editor
    }

    public readonly struct ContentSource
    {
        public readonly ContentSourceKind kind;
        public readonly string id;
        public readonly string rootPath;
        public readonly int priority;

        public ContentSource(ContentSourceKind kind, string id, string rootPath, int priority)
        {
            this.kind = kind;
            this.id = id ?? string.Empty;
            this.rootPath = rootPath ?? string.Empty;
            this.priority = priority;
        }

        public bool Exists => !string.IsNullOrEmpty(rootPath) && Directory.Exists(rootPath);
    }

    /// <summary>
    /// Deterministic source ordering. Higher priority is loaded later and therefore overlays lower priority.
    /// </summary>
    public static class ContentSourceCatalog
    {
        public static List<ContentSource> CreateDefault(string streamingAssetsRoot)
        {
            var sources = new List<ContentSource>();
            if (string.IsNullOrEmpty(streamingAssetsRoot)) return sources;

            sources.Add(new ContentSource(
                ContentSourceKind.Base, "base",
                Path.Combine(streamingAssetsRoot, "Base"), 0));

            sources.Add(new ContentSource(
                ContentSourceKind.Mod, "mods",
                Path.Combine(streamingAssetsRoot, "Mods"), 100));

            sources.Sort((a, b) => a.priority.CompareTo(b.priority));
            return sources;
        }
    }
}
