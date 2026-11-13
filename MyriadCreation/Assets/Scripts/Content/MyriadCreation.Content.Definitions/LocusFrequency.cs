using System;
using UnityEngine;

namespace MyriadCreation.Content.Definitions
{
[Serializable]
public class LocusFrequency
{
	public DnaLocus locus;

	[Range(0f, 1f)]
	public float dominantFrequency = 0.5f;
}
}
