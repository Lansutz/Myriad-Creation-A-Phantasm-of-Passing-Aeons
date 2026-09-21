using CivilizationEvolution.Core.Simulation;

namespace CivilizationEvolution.Simulation.Modding
{
    /// <summary>
    /// Public content authoring boundary shared by the built-in editor and external mods.
    /// Runtime definition resolution is deliberately a separate service so this API never
    /// exposes ContentRegistry internals.
    /// </summary>
    public interface IContentExtensionApi
    {
        IContentAuthoringWorkspace CreateWorkspace(ContentPackageManifest manifest);
    }

    public sealed class ContentExtensionApi : IContentExtensionApi
    {
        public IContentAuthoringWorkspace CreateWorkspace(ContentPackageManifest manifest)
        {
            var workspace = new ContentAuthoringWorkspace();
            workspace.SetManifest(manifest);
            return workspace;
        }
    }
}
