using System;
using UnityEngine;

namespace MyriadCreation.Content.Definitions
{
[Serializable]
public struct FactionPlatform
{
	[Range(-1f, 1f)]
	public float openness;

	[Range(-1f, 1f)]
	public float centralization;

	[Range(-1f, 1f)]
	public float commerce;

	[Range(-1f, 1f)]
	public float taxRelief;

	public static FactionPlatform operator +(FactionPlatform a, FactionPlatform b)
	{
		return new FactionPlatform
		{
			openness = a.openness + b.openness,
			centralization = a.centralization + b.centralization,
			commerce = a.commerce + b.commerce,
			taxRelief = a.taxRelief + b.taxRelief
		};
	}

	public FactionPlatform Scaled(float k)
	{
		return new FactionPlatform
		{
			openness = openness * k,
			centralization = centralization * k,
			commerce = commerce * k,
			taxRelief = taxRelief * k
		};
	}

	public void Clamp()
	{
		openness = Mathf.Clamp(openness, -1f, 1f);
		centralization = Mathf.Clamp(centralization, -1f, 1f);
		commerce = Mathf.Clamp(commerce, -1f, 1f);
		taxRelief = Mathf.Clamp(taxRelief, -1f, 1f);
	}
}
}
