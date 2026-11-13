using MyriadCreation.Core.Data;
using UnityEngine;

namespace MyriadCreation.World.Terrain
{
public class TerrainGenerator
{
	private readonly int _seed;

	private int _width;

	private int _height;

	public TerrainGenerator(int seed)
	{
		_seed = seed;
	}

	private float Hash(int x, int y)
	{
		int num = (x * 73856093) ^ (y * 19349663) ^ (_seed * 83492791);
		int num2 = (num ^ (num >>> 16)) * 2146121005;
		int num3 = (num2 ^ (num2 >>> 15)) * -2073254261;
		return (float)(uint)((num3 ^ (num3 >>> 16)) & 0xFFFFFF) / 16777215f;
	}

	private float SmoothNoise(float x, float y)
	{
		int num = Mathf.FloorToInt(x);
		int num2 = Mathf.FloorToInt(y);
		float num3 = x - (float)num;
		float num4 = y - (float)num2;
		num3 = num3 * num3 * (3f - 2f * num3);
		num4 = num4 * num4 * (3f - 2f * num4);
		float num5 = Hash(num, num2);
		float num6 = Hash(num + 1, num2);
		float num7 = Hash(num, num2 + 1);
		float num8 = Hash(num + 1, num2 + 1);
		return Mathf.Lerp(Mathf.Lerp(num5, num6, num3), Mathf.Lerp(num7, num8, num3), num4);
	}

	private float Fbm(float x, float y, int octaves = 5, float lacunarity = 2f, float gain = 0.5f)
	{
		float num = 0f;
		float num2 = 1f;
		float num3 = 1f;
		float num4 = 0f;
		for (int i = 0; i < octaves; i++)
		{
			num += SmoothNoise(x * num3, y * num3) * num2;
			num4 += num2;
			num2 *= gain;
			num3 *= lacunarity;
		}
		return num / num4;
	}

	private float Ridge(float x, float y, int octaves = 4, float lacunarity = 2.2f, float gain = 0.5f)
	{
		float num = 0f;
		float num2 = 1f;
		float num3 = 1f;
		float num4 = 0f;
		for (int i = 0; i < octaves; i++)
		{
			float num5 = 1f - Mathf.Abs(2f * SmoothNoise(x * num3, y * num3) - 1f);
			num += num5 * num5 * num2;
			num4 += num2;
			num2 *= gain;
			num3 *= lacunarity;
		}
		return num / num4;
	}

	public void Generate(TileData[] tiles, int width, int height, float continentScale, float mountainStrength)
	{
		_width = width;
		_height = height;
		float num = 1.2f / Mathf.Max(8f, (float)Mathf.Min(width, height) / 16f);
		for (int i = 0; i < tiles.Length; i++)
		{
			int num2 = i % width;
			int num3 = i / width;
			float num4 = (float)num2 / (float)width;
			float num5 = (float)num3 / (float)height;
			float num6 = Fbm(num4 * 1.5f, num5 * 1.5f, 3);
			float num7 = Fbm(num4 * 3f + 100f, num5 * 3f + 100f, 3);
			float num8 = Ridge(num4 * num * 2f + 50f, num5 * num * 2f + 50f);
			float num9 = num6 * 0.65f + num7 * 0.25f;
			num9 += num8 * num8 * mountainStrength * 0.5f;
			tiles[i].elevation01 = Mathf.Clamp01(num9);
			tiles[i].slopeDegree = Mathf.Clamp(num8 * 60f * mountainStrength + Mathf.Abs(num9 - 0.5f) * 20f, 0f, 60f);
		}
	}

	public void TrackRivers(TileData[] tiles)
	{
		for (int i = 0; i < tiles.Length; i++)
		{
			tiles[i].isRiver = false;
		}
		for (int j = 0; j < _height; j += 4)
		{
			for (int k = 0; k < _width; k += 4)
			{
				int num = j * _width + k;
				if (tiles[num].elevation01 < 0.65f)
				{
					continue;
				}
				int num2 = num;
				int num3 = 0;
				while (num3++ < 500 && tiles[num2].isLand && !tiles[num2].isRiver)
				{
					tiles[num2].isRiver = true;
					int num4 = LowestNeighbour(num2, tiles);
					if (num4 < 0 || tiles[num4].elevation01 >= tiles[num2].elevation01)
					{
						break;
					}
					num2 = num4;
				}
			}
		}
	}

	private int LowestNeighbour(int index, TileData[] tiles)
	{
		int num = index % _width;
		int num2 = index / _width;
		int result = -1;
		float num3 = float.MaxValue;
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				if (j == 0 && i == 0)
				{
					continue;
				}
				int num4 = num + j;
				int num5 = num2 + i;
				if (num4 >= 0 && num4 < _width && num5 >= 0 && num5 < _height)
				{
					int num6 = num5 * _width + num4;
					if (tiles[num6].elevation01 < num3)
					{
						num3 = tiles[num6].elevation01;
						result = num6;
					}
				}
			}
		}
		return result;
	}
}
}
