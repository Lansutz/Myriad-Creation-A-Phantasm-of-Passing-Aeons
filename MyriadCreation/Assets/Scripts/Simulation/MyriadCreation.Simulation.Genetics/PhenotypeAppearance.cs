using System;
using MyriadCreation.Content.Definitions;
using UnityEngine;

namespace MyriadCreation.Simulation.Genetics
{
[Serializable]
public struct PhenotypeAppearance
{
	public Color skinColor;

	public Color hairColor;

	public Color eyeColor;

	public Color bloodColor;

	public string eyeModel;

	public string hairStructure;

	public string skinTexture;

	public float heightScale;

	public float buildScale;
}
}