using System;

namespace MyriadCreation.Content.Modding
{
public sealed class ContentExtensionApi : IContentExtensionApi
{
	public IContentAuthoringWorkspace CreateWorkspace(ContentPackageManifest manifest)
	{
		ContentAuthoringWorkspace contentAuthoringWorkspace = new ContentAuthoringWorkspace();
		contentAuthoringWorkspace.SetManifest(manifest);
		return contentAuthoringWorkspace;
	}

	public ContentPackageValidationResult Validate(IContentAuthoringWorkspace workspace)
	{
		if (workspace == null)
		{
			throw new ArgumentNullException("workspace");
		}
		return ContentPackageValidator.Validate(workspace.Manifest, workspace.GetFiles().Keys);
	}
}
}
