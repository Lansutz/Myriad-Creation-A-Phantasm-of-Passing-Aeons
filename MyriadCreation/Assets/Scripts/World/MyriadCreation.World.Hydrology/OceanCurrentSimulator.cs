using System;
using System.Linq;
using MyriadCreation.Core.Data;
using UnityEngine;

namespace MyriadCreation.World.Hydrology
{
public class OceanCurrentSimulator
{
	public float WindCoupling = 0.3f;

	public float CoriolisStrength = 1f;

	public float BoundaryReflection = 0.8f;

	public float Diffusion = 0.1f;

	public int Iterations = 20;

	public float WarmCurrentTempBoost = 3f;

	public float ColdCurrentTempReduction = 2f;

	public float WarmCurrentPrecipBoost = 200f;

	public float ColdCurrentPrecipReduction = 100f;

	private readonly int _width;

	private readonly int _height;

	public float[] CurrentU { get; private set; }

	public float[] CurrentV { get; private set; }

	public float[] CurrentSpeed { get; private set; }

	public bool[] IsWarmCurrent { get; private set; }

	public bool[] IsColdCurrent { get; private set; }

	public OceanCurrentSimulator(int width, int height)
	{
		_width = width;
		_height = height;
		int num = width * height;
		CurrentU = new float[num];
		CurrentV = new float[num];
		CurrentSpeed = new float[num];
		IsWarmCurrent = new bool[num];
		IsColdCurrent = new bool[num];
	}

	public void Run(bool[] isLand, float[] windU, float[] windV, float[] temperature)
	{
		int num = _width * _height;
		Debug.Log((object)$"[OceanCurrentSimulator] 洋流模拟开始：{_width}x{_height}，{Iterations}次迭代");
		for (int i = 0; i < num; i++)
		{
			if (!isLand[i])
			{
				int num2 = i / _width;
				float num3 = 90f - (float)num2 / (float)_height * 180f;
				CurrentU[i] = windU[i] * WindCoupling;
				CurrentV[i] = windV[i] * WindCoupling;
				float num4 = Mathf.Sin(num3 * (MathF.PI / 180f)) * CoriolisStrength;
				float num5 = CurrentU[i] - num4 * CurrentV[i] * 0.5f;
				float num6 = CurrentV[i] + num4 * CurrentU[i] * 0.5f;
				CurrentU[i] = num5;
				CurrentV[i] = num6;
			}
		}
		for (int j = 0; j < Iterations; j++)
		{
			float[] array = new float[num];
			float[] array2 = new float[num];
			Array.Copy(CurrentU, array, num);
			Array.Copy(CurrentV, array2, num);
			for (int k = 1; k < _height - 1; k++)
			{
				for (int l = 0; l < _width; l++)
				{
					int num7 = k * _width + l;
					if (!isLand[num7])
					{
						int num8 = (l - 1 + _width) % _width;
						int num9 = (l + 1) % _width;
						int num10 = k * _width + num8;
						int num11 = k * _width + num9;
						int num12 = (k - 1) * _width + l;
						int num13 = (k + 1) * _width + l;
						float num14 = 0f;
						float num15 = 0f;
						if (CurrentU[num7] > 0f && isLand[num11])
						{
							num14 = (0f - CurrentU[num7]) * BoundaryReflection;
						}
						if (CurrentU[num7] < 0f && isLand[num10])
						{
							num14 = (0f - CurrentU[num7]) * BoundaryReflection;
						}
						if (CurrentV[num7] > 0f && isLand[num13])
						{
							num15 = (0f - CurrentV[num7]) * BoundaryReflection;
						}
						if (CurrentV[num7] < 0f && isLand[num12])
						{
							num15 = (0f - CurrentV[num7]) * BoundaryReflection;
						}
						if (CurrentU[num7] < 0f && !isLand[num10] && isLand[k * _width + (num8 - 2 + _width) % _width])
						{
							array[num7] *= 1.5f;
							array2[num7] *= 1.5f;
						}
						float num16 = (CurrentU[num10] + CurrentU[num11] + CurrentU[num12] + CurrentU[num13] - 4f * CurrentU[num7]) * Diffusion;
						float num17 = (CurrentV[num10] + CurrentV[num11] + CurrentV[num12] + CurrentV[num13] - 4f * CurrentV[num7]) * Diffusion;
						array[num7] = CurrentU[num7] + num14 + num16;
						array2[num7] = CurrentV[num7] + num15 + num17;
					}
				}
			}
			CurrentU = array;
			CurrentV = array2;
		}
		for (int m = 0; m < num; m++)
		{
			if (!isLand[m])
			{
				int num18 = m / _width;
				float num19 = 90f - (float)num18 / (float)_height * 180f;
				CurrentSpeed[m] = Mathf.Sqrt(CurrentU[m] * CurrentU[m] + CurrentV[m] * CurrentV[m]);
				bool flag = (num19 > 0f && CurrentV[m] > 0f) || (num19 < 0f && CurrentV[m] < 0f);
				bool flag2 = (num19 > 0f && CurrentV[m] < 0f) || (num19 < 0f && CurrentV[m] > 0f);
				IsWarmCurrent[m] = flag && CurrentSpeed[m] > 0.1f && Mathf.Abs(num19) > 15f;
				IsColdCurrent[m] = flag2 && CurrentSpeed[m] > 0.1f && Mathf.Abs(num19) > 15f;
			}
		}
		int num20 = 0;
		int num21 = 0;
		for (int n = 0; n < num; n++)
		{
			if (IsWarmCurrent[n])
			{
				num20++;
			}
			if (IsColdCurrent[n])
			{
				num21++;
			}
		}
		Debug.Log((object)$"[OceanCurrentSimulator] 洋流模拟完成：暖流{num20}地块，寒流{num21}地块，最大速度{CurrentSpeed.Max():F2}m/s");
	}

	public void ApplyCoastalEffects(TileData[] tiles, bool[] isLand)
	{
		int num = Math.Min(tiles.Length, CurrentU.Length);
		for (int i = 0; i < num; i++)
		{
			if (!isLand[i] || !tiles[i].isCoast)
			{
				continue;
			}
			int num2 = i % _width;
			int num3 = i / _width;
			bool flag = false;
			bool flag2 = false;
			float num4 = 0f;
			float num5 = 0f;
			for (int j = -1; j <= 1; j++)
			{
				for (int k = -1; k <= 1; k++)
				{
					if (j == 0 && k == 0)
					{
						continue;
					}
					int num6 = (num2 + j + _width) % _width;
					int num7 = Mathf.Clamp(num3 + k, 0, _height - 1) * _width + num6;
					if (num7 < num)
					{
						if (IsWarmCurrent[num7])
						{
							flag = true;
							num4 = Mathf.Max(num4, CurrentSpeed[num7]);
						}
						if (IsColdCurrent[num7])
						{
							flag2 = true;
							num5 = Mathf.Max(num5, CurrentSpeed[num7]);
						}
					}
				}
			}
			if (flag)
			{
				float num8 = Mathf.Clamp01(num4 / 1f);
				tiles[i].annualTemp += WarmCurrentTempBoost * num8;
				tiles[i].annualPrecipMm += WarmCurrentPrecipBoost * num8;
			}
			if (flag2)
			{
				float num9 = Mathf.Clamp01(num5 / 1f);
				tiles[i].annualTemp -= ColdCurrentTempReduction * num9;
				tiles[i].annualPrecipMm = Mathf.Max(0f, tiles[i].annualPrecipMm - ColdCurrentPrecipReduction * num9);
			}
		}
	}
}
}
