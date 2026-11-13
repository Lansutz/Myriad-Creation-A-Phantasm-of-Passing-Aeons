using System;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Content.Modding
{
[Serializable]
public class TitleDef
{
	public string titleId;

	public MyriadCreation.Core.Enums.TitleSuccessionMode successionMode;

	public string ownLawPreset = "";

	public TitleDerivedCategory category;

	public float rank;

	public float weight = 1f;

	public string semantic = "";
}
}
