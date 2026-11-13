using System;
using MyriadCreation.Core.Enums;
using UnityEngine;

namespace MyriadCreation.World.Biome
{
[Serializable]
public class BiomeDef
{
	public int biomeId;

	public string name = "";

	public string color = "#808080";

	public float movementCost = 1.5f;

	public float habitability = 50f;

	public float fertilityMultiplier = 1f;

	public string category = "C";

	public bool isOcean;

	public string icon = "";

	public int iconDensity;

	public GameEnums.BiomeType BiomeType => (GameEnums.BiomeType)biomeId;

	public Color ParseColor()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		Color result;
		if (ColorUtility.TryParseHtmlString(color, out result))
		{
			return result;
		}
		return Color.gray;
	}
}
}
