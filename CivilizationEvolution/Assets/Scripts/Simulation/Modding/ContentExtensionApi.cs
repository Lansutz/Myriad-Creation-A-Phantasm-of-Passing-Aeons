using System;
using CivilizationEvolution.Core.Simulation;

namespace CivilizationEvolution.Simulation.Modding
{
    /// <summary>
    /// Public extension boundary. Mods and built-in authoring tools target this API,
    /// not internal domain registries or simulation implementations.
    /// </summary>
    public interface IContentExtensionApi
    {
        IContentAuthoringWorkspace CreateWorkspace(ContentPackageManifest manifest);
        bool TryGetDefinition<T>(string id, out T definition) where T : class, IRuntimeDefinition;
    }

    public sealed class ContentExtensionApi : IContentExtensionApi
    {
        public IContentAuthoringWorkspace CreateWorkspace(ContentPackageManifest manifest)
        {
            var workspace = new ContentAuthoringWorkspace();
            workspace.SetManifest(manifest);
            return workspace;
        }

        public bool TryGetDefinition<T>(string id, out T definition) where T : class, IRuntimeDefinition
        {
            definition = null;
            return false;
        }
    }
}
