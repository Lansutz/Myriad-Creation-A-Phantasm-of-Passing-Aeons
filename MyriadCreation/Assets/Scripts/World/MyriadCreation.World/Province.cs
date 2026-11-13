using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;

namespace MyriadCreation.World
{
[Serializable]
public class Province
{
	public int provinceId;

	public string provinceName;

	public int centerTileIndex;

	public List<int> memberTiles = new List<int>();

	public static bool IsBorder(TileData[] tiles, int width, int height, int index)
	{
		if (tiles[index].provinceId < 0)
		{
			return false;
		}
		int num = index % width;
		int num2 = index / width;
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				if (j == 0 && i == 0)
				{
					continue;
				}
				int num3 = num + j;
				int num4 = num2 + i;
				if (num3 >= 0 && num3 < width && num4 >= 0 && num4 < height)
				{
					int num5 = num4 * width + num3;
					if (tiles[num5].isLand && tiles[num5].provinceId != tiles[index].provinceId)
					{
						return true;
					}
				}
			}
		}
		return false;
	}
}
}
