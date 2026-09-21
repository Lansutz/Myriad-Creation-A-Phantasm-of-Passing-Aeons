using System;
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
        ContentPackageValidationResult Validate(IContentAuthoringWorkspace workspace);
    }

    public sealed class ContentExtensionApi : IContentExtensionApi
    {
        public IContentAuthoringWorkspace CreateWorkspace(ContentPackageManifest manifest)
        {
            var workspace = new ContentAuthoringWorkspace();
            workspace.SetManifest(manifest);
            return workspace;
        }

        public ContentPackageValidationResult Validate(IContentAuthoringWorkspace workspace)
        {
            if (workspace == null)
                throw new ArgumentNullException(nameof(workspace));

            return ContentPackageValidator.Validate(
                workspace.Manifest,
                workspace.GetFiles().Keys);
        }
    }
}
