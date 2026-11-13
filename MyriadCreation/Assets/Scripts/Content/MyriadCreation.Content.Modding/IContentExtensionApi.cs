namespace MyriadCreation.Content.Modding
{
public interface IContentExtensionApi
{
	IContentAuthoringWorkspace CreateWorkspace(ContentPackageManifest manifest);

	ContentPackageValidationResult Validate(IContentAuthoringWorkspace workspace);
}
}
