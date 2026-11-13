using System;

namespace MyriadCreation.Content.Definitions
{
[Serializable]
public class AlleleTraitEffect
{
	public string traitId = "";

	public float additiveEffect;

	public float dominanceDeviation;

	public float environmentalSensitivity;

	public AppearanceChannel appearanceChannel;

	/// <summary>离散性状通道（如 wingType/hornType/skinType）；与 traitId 的加性通道互不替代。</summary>
	public string discreteTraitId = "";

	/// <summary>离散性状取值（内容中立字符串，如 membrane/keratin/scale）。</summary>
	public string discreteValue = "";

	public float appearanceAmount;
}
}
