using System.Collections.Generic;
using UnityEngine;

namespace MyriadCreation.Core.Data
{
public static class TileGrid
{
	public static int ToIndex(int x, int y, int width)
	{
		return y * width + x;
	}

	public static int ToX(int index, int width)
	{
		return index % width;
	}

	public static int ToY(int index, int width)
	{
		return index / width;
	}

	public static int WrapX(int x, int width)
	{
		return (x % width + width) % width;
	}

	public static int WrapY(int y, int height)
	{
		return (y % height + height) % height;
	}

	public static List<int> GetNeighbours(int index, int width, int height, bool wrapX = true, bool wrapY = false)
	{
		int num = ToX(index, width);
		int num2 = ToY(index, width);
		List<int> list = new List<int>(6);
		int[,] array = new int[6, 2]
		{
			{ 1, 0 },
			{ 0, -1 },
			{ -1, -1 },
			{ -1, 0 },
			{ -1, 1 },
			{ 0, 1 }
		};
		int[,] array2 = new int[6, 2]
		{
			{ 1, 0 },
			{ 1, -1 },
			{ 0, -1 },
			{ -1, 0 },
			{ 0, 1 },
			{ 1, 1 }
		};
		int[,] array3 = ((num2 % 2 == 0) ? array : array2);
		for (int i = 0; i < 6; i++)
		{
			int num3 = num + array3[i, 0];
			int num4 = num2 + array3[i, 1];
			if (wrapX)
			{
				num3 = WrapX(num3, width);
			}
			else if (num3 < 0 || num3 >= width)
			{
				continue;
			}
			if (wrapY)
			{
				num4 = WrapY(num4, height);
			}
			else if (num4 < 0 || num4 >= height)
			{
				continue;
			}
			list.Add(ToIndex(num3, num4, width));
		}
		return list;
	}

	public static int HexDistance(int indexA, int indexB, int width, bool wrapX = true)
	{
		int num = ToX(indexA, width);
		int num2 = ToY(indexA, width);
		int num3 = ToX(indexB, width);
		int num4 = ToY(indexB, width);
		if (wrapX)
		{
			int num5 = Mathf.Abs(num - num3);
			num5 = Mathf.Min(num5, width - num5);
			num = 0;
			num3 = num5;
		}
		int num6 = num - (num2 - (num2 & 1)) / 2;
		int num7 = num2;
		int num8 = -num6 - num7;
		int num9 = num3 - (num4 - (num4 & 1)) / 2;
		int num10 = num4;
		int num11 = -num9 - num10;
		return (Mathf.Abs(num6 - num9) + Mathf.Abs(num8 - num11) + Mathf.Abs(num7 - num10)) / 2;
	}

	public static List<int> GetCircleBrush(int centerIndex, int radius, int width, int height, bool wrapX = true)
	{
		List<int> list = new List<int>();
		int num = ToX(centerIndex, width);
		int num2 = ToY(centerIndex, width);
		for (int i = -radius; i <= radius; i++)
		{
			for (int j = -radius; j <= radius; j++)
			{
				int num3 = num + j;
				int num4 = num2 + i;
				if (wrapX)
				{
					num3 = WrapX(num3, width);
				}
				else if (num3 < 0 || num3 >= width)
				{
					continue;
				}
				if (num4 >= 0 && num4 < height && (Mathf.Abs(j) + Mathf.Abs(i) + Mathf.Abs(j + i)) / 2 <= radius)
				{
					list.Add(ToIndex(num3, num4, width));
				}
			}
		}
		return list;
	}

	public static List<int> GetSquareBrush(int centerIndex, int halfSize, int width, int height, bool wrapX = true)
	{
		List<int> list = new List<int>();
		int num = ToX(centerIndex, width);
		int num2 = ToY(centerIndex, width);
		for (int i = -halfSize; i <= halfSize; i++)
		{
			for (int j = -halfSize; j <= halfSize; j++)
			{
				int num3 = num + j;
				int num4 = num2 + i;
				if (wrapX)
				{
					num3 = WrapX(num3, width);
				}
				else if (num3 < 0 || num3 >= width)
				{
					continue;
				}
				if (num4 >= 0 && num4 < height)
				{
					list.Add(ToIndex(num3, num4, width));
				}
			}
		}
		return list;
	}
}
}
