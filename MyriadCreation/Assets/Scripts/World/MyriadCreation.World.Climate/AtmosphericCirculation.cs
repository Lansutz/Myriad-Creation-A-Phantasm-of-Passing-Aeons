using System;
using System.Linq;
using MyriadCreation.Core.Data;
using UnityEngine;

namespace MyriadCreation.World.Climate
{
public class AtmosphericCirculation
{
	public float PlanetOmega = 7.292E-05f;

	public float PlanetRadius = 6371000f;

	public float AxialTilt = 0.409f;

	public float SolarConstant = 1361f;

	public float Season;

	public float OrographicPrecipFactor = 800f;

	public float RainShadowFactor = 0.3f;

	public float MonsoonStrength = 0.6f;

	private readonly int _width;

	private readonly int _height;

	public float[] Pressure { get; private set; }

	public float[] WindU { get; private set; }

	public float[] WindV { get; private set; }

	public float[] WindSpeed { get; private set; }

	public float[] SpecificHumidity { get; private set; }

	public float[] Precipitation { get; private set; }

	public AtmosphericCirculation(int width, int height)
	{
		_width = width;
		_height = height;
		int num = width * height;
		Pressure = new float[num];
		WindU = new float[num];
		WindV = new float[num];
		WindSpeed = new float[num];
		SpecificHumidity = new float[num];
		Precipitation = new float[num];
	}

	public void Run(float[] elevation, bool[] isLand, float[] temperature)
	{
		int num = _width * _height;
		Debug.Log((object)$"[AtmosphericCirculation] GCM开始：{_width}x{_height}，季节={Season:F2}");
		float[] array = new float[num];
		for (int i = 0; i < _height; i++)
		{
			float num2 = 90f - (float)i / (float)_height * 180f;
			float num3 = num2 * (MathF.PI / 180f);
			float num4 = AxialTilt * Mathf.Sin(Season * MathF.PI * 2f);
			float num5 = 0f;
			float num6 = Mathf.Sin(num3) * Mathf.Sin(num4) + Mathf.Cos(num3) * Mathf.Cos(num4) * Mathf.Cos(num5);
			num6 = Mathf.Max(0f, num6);
			float num7 = CalculateDayLength(num2, num4);
			array[i * _width] = SolarConstant * num6 * (num7 / 24f) * 0.25f;
			for (int j = 1; j < _width; j++)
			{
				array[i * _width + j] = array[i * _width];
			}
		}
		for (int k = 0; k < num; k++)
		{
			int num8 = k / _width;
			float num9 = 90f - (float)num8 / (float)_height * 180f;
			float num10 = Mathf.Abs(num9);
			float num11;
			if (num10 < 10f)
			{
				num11 = 1010f;
			}
			else if (num10 < 20f)
			{
				num11 = 1013f;
			}
			else if (num10 < 35f)
			{
				num11 = 1022f;
			}
			else if (num10 < 45f)
			{
				num11 = 1015f;
			}
			else
			{
				num11 = ((!(num10 < 65f)) ? 1018f : 1008f);
			}
			float num12 = 0f;
			if (isLand[k])
			{
				float num13 = temperature[k];
				float num14 = Mathf.Cos(Season * MathF.PI * 2f - MathF.PI / 2f);
				if (num9 > 0f)
				{
					num14 = 0f - num14;
				}
				num12 = (0f - num13) * 0.3f * num14 * MonsoonStrength;
			}
			Pressure[k] = num11 + num12;
		}
		for (int l = 0; l < num; l++)
		{
			int num15 = l % _width;
			int num16 = l / _width;
			float num17 = 90f - (float)num16 / (float)_height * 180f;
			float num18 = Mathf.Abs(num17);
			float num19 = 2f * PlanetOmega * Mathf.Sin(num17 * (MathF.PI / 180f));
			if (Mathf.Abs(num19) < 1E-06f)
			{
				num19 = 1E-06f * Mathf.Sign(num17);
			}
			int num20 = (num15 - 1 + _width) % _width;
			int num21 = (num15 + 1) % _width;
			int num22 = Mathf.Max(0, num16 - 1);
			int num23 = Mathf.Min(_height - 1, num16 + 1);
			float num24 = (Pressure[num16 * _width + num21] - Pressure[num16 * _width + num20]) / (222000f * Mathf.Cos(num17 * (MathF.PI / 180f)));
			float num25 = (Pressure[num23 * _width + num15] - Pressure[num22 * _width + num15]) / 222000f;
			float num26 = 1.2f;
			float num27 = (0f - 1f / (num19 * num26)) * num25;
			float num28 = 1f / (num19 * num26) * num24;
			float num29;
			if (num18 < 30f)
			{
				num29 = ((num17 > 0f) ? (-1f) : 1f) * 3f;
			}
			else
			{
				num29 = ((!(num18 < 60f)) ? (((num17 > 0f) ? (-1f) : 1f) * 1.5f) : (((num17 > 0f) ? 1f : (-1f)) * 2f));
			}
			float num30;
			if (num18 < 30f)
			{
				num30 = ((num17 > 0f) ? (-1f) : 1f) * 5f;
			}
			else
			{
				num30 = ((!(num18 < 60f)) ? (((num17 > 0f) ? (-1f) : 1f) * 3f) : (((num17 > 0f) ? 1f : (-1f)) * 8f));
			}
			WindU[l] = num27 * 0.3f + num30;
			WindV[l] = num28 * 0.3f + num29;
			WindSpeed[l] = Mathf.Sqrt(WindU[l] * WindU[l] + WindV[l] * WindV[l]);
		}
		for (int m = 0; m < num; m++)
		{
			float num31 = temperature[m] + 273.15f;
			float num32 = 6.112f * Mathf.Exp(17.67f * (num31 - 273.15f) / (num31 - 29.65f));
			float num33 = (isLand[m] ? 0.6f : 0.8f);
			SpecificHumidity[m] = 0.622f * (num33 * num32) / (Pressure[m] - 0.378f * num33 * num32) / 1000f;
		}
		for (int n = 0; n < num; n++)
		{
			int num34 = n % _width;
			int num35 = n / _width;
			float num36 = Mathf.Abs(90f - (float)num35 / (float)_height * 180f);
			float num37;
			if (num36 < 10f)
			{
				num37 = 2000f;
			}
			else if (num36 < 25f)
			{
				num37 = 800f;
			}
			else if (num36 < 45f)
			{
				num37 = 1200f;
			}
			else
			{
				num37 = ((!(num36 < 70f)) ? 200f : 600f);
			}
			if (num36 > 20f && num36 < 35f && isLand[n])
			{
				num37 *= 0.4f;
			}
			float num38 = 0f;
			if (isLand[n] && elevation[n] > 0.55f)
			{
				float num39 = (0f - WindU[n]) / WindSpeed[n];
				float num40 = (0f - WindV[n]) / WindSpeed[n];
				int num41 = num34 + (int)Mathf.Sign(num39) * 2;
				int num42 = num35 + (int)Mathf.Sign(num40) * 2;
				num41 = (num41 + _width) % _width;
				num42 = Mathf.Clamp(num42, 0, _height - 1);
				float num43 = elevation[num42 * _width + num41];
				if (num43 > elevation[n])
				{
					num38 = OrographicPrecipFactor * (num43 - elevation[n]) * SpecificHumidity[n] * 100f;
				}
				else
				{
					num37 *= RainShadowFactor;
				}
			}
			float num44 = 0f;
			if (num36 < 20f && temperature[n] > 20f && isLand[n])
			{
				num44 = 500f * SpecificHumidity[n] * 50f;
			}
			float num45 = 1f;
			if (isLand[n])
			{
				bool flag = false;
				for (int num46 = -2; num46 <= 2; num46++)
				{
					if (flag)
					{
						break;
					}
					for (int num47 = -2; num47 <= 2; num47++)
					{
						if (flag)
						{
							break;
						}
						int num48 = (num34 + num46 + _width) % _width;
						int num49 = Mathf.Clamp(num35 + num47, 0, _height - 1);
						if (!isLand[num49 * _width + num48])
						{
							flag = true;
						}
					}
				}
				if (flag)
				{
					num45 = 1.3f;
				}
			}
			Precipitation[n] = Mathf.Max(0f, (num37 + num38 + num44) * num45);
		}
		Debug.Log((object)$"[AtmosphericCirculation] GCM完成：平均降水{Precipitation.Average():F0}mm，最大{Precipitation.Max():F0}mm");
	}

	public void ApplyToTiles(TileData[] tiles, float[] temperature)
	{
		int num = Math.Min(tiles.Length, Precipitation.Length);
		for (int i = 0; i < num; i++)
		{
			tiles[i].annualPrecipMm = Precipitation[i];
			tiles[i].airHumidityPct = Mathf.Clamp01(SpecificHumidity[i] * 100f);
		}
	}

	private static float CalculateDayLength(float lat, float declination)
	{
		float num = Mathf.Acos(Mathf.Clamp((0f - Mathf.Tan(lat * (MathF.PI / 180f))) * Mathf.Tan(declination), -1f, 1f)) * 57.29578f;
		return 2f * num / 15f;
	}
}
}
