using System;

namespace MyriadCreation.Content.Definitions
{
[Serializable]
public class SaintDef
{
	public int saintId;

	public int linkedCharacterId = -1;

	public string saintName;

	public string domain = "";

	public int faithId;

	public float canonizationPiety = 80f;
}
}
