using System;
using MyriadCreation.Core.Data;
using UnityEngine;
using Random = System.Random;

namespace MyriadCreation.World.Terrain
{
public class HydraulicErosion
{
	public int ParticleCount = 60000;

	public int MaxLifetime = 64;

	public float ErosionRate = 0.3f;

	public float DepositionRate = 0.3f;

	public float EvaporationRate = 0.02f;

	public float Inertia = 0.05f;

	public float MinSlope = 0.01f;

	public float CapacityFactor = 4f;

	public float InitialWater = 1f;

	public float InitialSpeed = 1f;

	public bool WrapX = true;

	private readonly int _width;

	private readonly int _height;

	private readonly Random _rng;

	public HydraulicErosion(int width, int height, int seed = 42)
	{
		_width = width;
		_height = height;
		_rng = new Random(seed);
	}

	public void Run(float[] elevation, bool[] isLand)
	{
		float[] sediment = new float[_width * _height];
		Debug.Log((object)$"[HydraulicErosion] 开始侵蚀模拟：{ParticleCount}粒子，最大寿命{MaxLifetime}步");
		for (int i = 0; i < ParticleCount; i++)
		{
			int num = 0;
			int num2;
			int num3;
			int num4;
			do
			{
				num2 = _rng.Next(_width);
				num3 = _rng.Next(_height / 4, _height);
				num4 = num3 * _width + num2;
				num++;
			}
			while ((!isLand[num4] || elevation[num4] < 0.5f) && num < 50);
			if (num >= 50)
			{
				continue;
			}
			float num5 = num2;
			float num6 = num3;
			float num7 = 0f;
			float num8 = 0f;
			float num9 = InitialWater;
			float num10 = InitialSpeed;
			float num11 = 0f;
			for (int j = 0; j < MaxLifetime; j++)
			{
				int num12 = (int)num5;
				int num13 = (int)num6;
				int num14 = num13 * _width + num12;
				float u = num5 - (float)num12;
				float v = num6 - (float)num13;
				float num15 = BilinearInterpolate(elevation, num12, num13, u, v);
				(float gradX, float gradY) tuple = BilinearGradient(elevation, num12, num13, u, v);
				float item = tuple.gradX;
				float item2 = tuple.gradY;
				num7 = num7 * Inertia - item * (1f - Inertia);
				num8 = num8 * Inertia - item2 * (1f - Inertia);
				float num16 = Mathf.Sqrt(num7 * num7 + num8 * num8);
				if (num16 > 0.0001f)
				{
					num7 /= num16;
					num8 /= num16;
				}
				else
				{
					float num17 = (float)_rng.NextDouble() * MathF.PI * 2f;
					num7 = Mathf.Cos(num17);
					num8 = Mathf.Sin(num17);
				}
				float num18 = num5 + num7;
				float num19 = num6 + num8;
				if (num19 < 0f || num19 >= (float)(_height - 1))
				{
					break;
				}
				if (WrapX)
				{
					if (num18 < 0f)
					{
						num18 += (float)_width;
					}
					if (num18 >= (float)_width)
					{
						num18 -= (float)_width;
					}
				}
				else if (num18 < 0f || num18 >= (float)(_width - 1))
				{
					break;
				}
				int num20 = (int)num18;
				int num21 = (int)num19;
				int num22 = num21 * _width + num20;
				if (!isLand[num22])
				{
					break;
				}
				float num23 = BilinearInterpolate(elevation, num20, num21, num18 - (float)num20, num19 - (float)num21) - num15;
				float num24 = Mathf.Max(Mathf.Abs(num23), MinSlope) * num9 * num10 * CapacityFactor;
				if (num11 > num24 || num23 > 0f)
				{
					float num25 = ((!(num23 > 0f)) ? ((num11 - num24) * DepositionRate) : Mathf.Min(num23, num11));
					num11 -= num25;
					DepositAt(elevation, sediment, num12, num13, u, v, num25);
				}
				else
				{
					float num26 = Mathf.Min((num24 - num11) * ErosionRate, 0f - num23);
					num26 = Mathf.Min(num26, elevation[num14] * 0.5f);
					ErodeAt(elevation, sediment, num12, num13, u, v, num26);
					num11 += num26;
				}
				num10 = Mathf.Sqrt(Mathf.Max(0f, num10 * num10 + num23 * 10f));
				num9 *= 1f - EvaporationRate;
				if (num9 < 0.01f)
				{
					break;
				}
				num5 = num18;
				num6 = num19;
			}
		}
		Debug.Log((object)"[HydraulicErosion] 侵蚀模拟完成");
	}

	public void ApplyToTiles(TileData[] tiles, float[] elevation)
	{
		int num = Math.Min(tiles.Length, elevation.Length);
		for (int i = 0; i < num; i++)
		{
			if (tiles[i].isLand)
			{
				tiles[i].elevation01 = Mathf.Clamp01(elevation[i]);
			}
		}
	}

	private float BilinearInterpolate(float[] elevation, int x, int y, float u, float v)
	{
		int num = (WrapX ? ((x + 1) % _width) : Mathf.Min(x + 1, _width - 1));
		int num2 = Mathf.Min(y + 1, _height - 1);
		int num3 = y * _width + x;
		int num4 = y * _width + num;
		int num5 = num2 * _width + x;
		int num6 = num2 * _width + num;
		float num7 = elevation[num3];
		float num8 = elevation[num4];
		float num9 = elevation[num5];
		float num10 = elevation[num6];
		return num7 * (1f - u) * (1f - v) + num8 * u * (1f - v) + num9 * (1f - u) * v + num10 * u * v;
	}

	private (float gradX, float gradY) BilinearGradient(float[] elevation, int x, int y, float u, float v)
	{
		int num = (WrapX ? ((x + 1) % _width) : Mathf.Min(x + 1, _width - 1));
		int num2 = Mathf.Min(y + 1, _height - 1);
		int num3 = y * _width + x;
		int num4 = y * _width + num;
		int num5 = num2 * _width + x;
		int num6 = num2 * _width + num;
		float num7 = elevation[num3];
		float num8 = elevation[num4];
		float num9 = elevation[num5];
		float num10 = elevation[num6];
		float item = (num8 - num7) * (1f - v) + (num10 - num9) * v;
		float item2 = (num9 - num7) * (1f - u) + (num10 - num8) * u;
		return (gradX: item, gradY: item2);
	}

	private void DepositAt(float[] elevation, float[] sediment, int x, int y, float u, float v, float amount)
	{
		int num = (WrapX ? ((x + 1) % _width) : Mathf.Min(x + 1, _width - 1));
		int num2 = Mathf.Min(y + 1, _height - 1);
		int num3 = y * _width + x;
		int num4 = y * _width + num;
		int num5 = num2 * _width + x;
		int num6 = num2 * _width + num;
		elevation[num3] += amount * (1f - u) * (1f - v);
		elevation[num4] += amount * u * (1f - v);
		elevation[num5] += amount * (1f - u) * v;
		elevation[num6] += amount * u * v;
	}

	private void ErodeAt(float[] elevation, float[] sediment, int x, int y, float u, float v, float amount)
	{
		int num = (WrapX ? ((x + 1) % _width) : Mathf.Min(x + 1, _width - 1));
		int num2 = Mathf.Min(y + 1, _height - 1);
		int num3 = y * _width + x;
		int num4 = y * _width + num;
		int num5 = num2 * _width + x;
		int num6 = num2 * _width + num;
		elevation[num3] -= amount * (1f - u) * (1f - v);
		elevation[num4] -= amount * u * (1f - v);
		elevation[num5] -= amount * (1f - u) * v;
		elevation[num6] -= amount * u * v;
	}
}
}
