using System;
using System.Collections.Generic;

namespace MyriadCreation.Content.Modding
{
[Serializable]
public class LanguageFamilyArtifact
{
	public int schema_version;

	public string generated_at;

	public string artifact;

	public string requires;

	public LanguageFamilySource source;

	public List<LanguageFamilyNode> nodes = new List<LanguageFamilyNode>();
}
}
