using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.World.Generation;
using UnityEngine;

namespace MyriadCreation.World.Terrain
{
public static class RegionalTerrainShaper
{
	public static void Apply(TileData[] tiles, int width, int height, MapGenerationConfig.RegionalEdgeMode edgeMode, float falloffRatio, float seaLevel)
	{
		if (tiles == null || width <= 0 || height <= 0 || edgeMode == MapGenerationConfig.RegionalEdgeMode.Open)
		{
			return;
		}
		float num = Mathf.Clamp(falloffRatio, 0.02f, 0.3f);
		float num2 = Mathf.Max(1f, (float)width * num);
		float num3 = Mathf.Max(1f, (float)height * num);
		for (int i = 0; i < height; i++)
		{
			for (int j = 0; j < width; j++)
			{
				int num4 = i * width + j;
				if (tiles[num4].exists)
				{
					float num5 = 0f;
					switch (edgeMode)
					{
					case MapGenerationConfig.RegionalEdgeMode.SeaWest:
						num5 = Mathf.Clamp01(1f - (float)j / num2);
						break;
					case MapGenerationConfig.RegionalEdgeMode.SeaEast:
						num5 = Mathf.Clamp01(1f - (float)(width - 1 - j) / num2);
						break;
					case MapGenerationConfig.RegionalEdgeMode.SeaNorth:
						num5 = Mathf.Clamp01(1f - (float)i / num3);
						break;
					case MapGenerationConfig.RegionalEdgeMode.SeaSouth:
						num5 = Mathf.Clamp01(1f - (float)(height - 1 - i) / num3);
						break;
					case MapGenerationConfig.RegionalEdgeMode.Island:
					{
						float num6 = (float)Mathf.Min(j, width - 1 - j) / num2;
						float num7 = (float)Mathf.Min(i, height - 1 - i) / num3;
						num5 = Mathf.Clamp01(1f - Mathf.Min(num6, num7));
						break;
					}
					}
					if (!(num5 <= 0f))
					{
						float num8 = seaLevel - 0.02f;
						tiles[num4].elevation01 = Mathf.Lerp(tiles[num4].elevation01, num8, num5);
						tiles[num4].isLand = tiles[num4].elevation01 > seaLevel;
						tiles[num4].oceanTier = (tiles[num4].isLand ? GameEnums.OceanTier.Land : GameEnums.OceanTier.Coast);
					}
				}
			}
		}
	}
}
}
