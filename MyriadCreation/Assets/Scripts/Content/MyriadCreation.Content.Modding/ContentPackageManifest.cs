using System;
using System.Collections.Generic;

namespace MyriadCreation.Content.Modding
{
[Serializable]
public sealed class ContentPackageManifest
{
	public string formatVersion = "1";

	public string packageId = string.Empty;

	public string displayName = string.Empty;

	public string author = string.Empty;

	public string description = string.Empty;

	public string gameVersion = string.Empty;

	public List<string> contentTypes = new List<string>();
}
}
