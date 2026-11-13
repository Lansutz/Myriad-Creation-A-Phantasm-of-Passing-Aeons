using System.Collections.Generic;

namespace MyriadCreation.Content.Modding
{
public interface IContentAuthoringWorkspace
{
	ContentPackageManifest Manifest { get; }

	void SetManifest(ContentPackageManifest manifest);

	void Upsert(string contentType, string id, string serializedDefinition);

	void UpsertFile(string relativePath, string content);

	bool Remove(string contentType, string id);

	IReadOnlyDictionary<string, string> GetDefinitions(string contentType);

	IReadOnlyDictionary<string, string> GetFiles();
}
}
