using System;
using System.Collections.Generic;

namespace CivilizationEvolution.Simulation.Modding
{
    /// <summary>
    /// Authoring contract. UI/editor implementations manipulate definitions through
    /// this boundary rather than writing directly into runtime registries.
    /// </summary>
    public interface IContentAuthoringWorkspace
    {
        ContentPackageManifest Manifest { get; }
        void SetManifest(ContentPackageManifest manifest);
        void Upsert(string contentType, string id, string serializedDefinition);
        bool Remove(string contentType, string id);
        IReadOnlyDictionary<string, string> GetDefinitions(string contentType);
    }

    /// <summary>
    /// In-memory authoring workspace used by runtime editor implementations and tests.
    /// It deliberately does not depend on ContentRegistry.
    /// </summary>
    public sealed class ContentAuthoringWorkspace : IContentAuthoringWorkspace
    {
        private readonly Dictionary<string, Dictionary<string, string>> _definitions =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        public ContentPackageManifest Manifest { get; private set; } = new ContentPackageManifest();

        public void SetManifest(ContentPackageManifest manifest)
        {
            Manifest = manifest ?? new ContentPackageManifest();
        }

        public void Upsert(string contentType, string id, string serializedDefinition)
        {
            if (string.IsNullOrWhiteSpace(contentType)) throw new ArgumentException("contentType");
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("id");
            if (serializedDefinition == null) throw new ArgumentNullException(nameof(serializedDefinition));

            if (!_definitions.TryGetValue(contentType, out var bucket))
            {
                bucket = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                _definitions[contentType] = bucket;
            }

            bucket[id] = serializedDefinition;
            if (!Manifest.contentTypes.Contains(contentType))
                Manifest.contentTypes.Add(contentType);
        }

        public bool Remove(string contentType, string id)
        {
            return _definitions.TryGetValue(contentType, out var bucket) && bucket.Remove(id);
        }

        public IReadOnlyDictionary<string, string> GetDefinitions(string contentType)
        {
            if (_definitions.TryGetValue(contentType, out var bucket))
                return bucket;
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
