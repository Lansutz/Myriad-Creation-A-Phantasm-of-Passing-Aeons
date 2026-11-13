using System;

namespace MyriadCreation.World.Terrain
{
public class SphericalNoise
{
	private readonly byte[] _perm = new byte[512];

	private readonly byte[] _permMod12 = new byte[512];

	private static readonly float[][] Grad3 = new float[12][]
	{
		new float[3] { 1f, 1f, 0f },
		new float[3] { -1f, 1f, 0f },
		new float[3] { 1f, -1f, 0f },
		new float[3] { -1f, -1f, 0f },
		new float[3] { 1f, 0f, 1f },
		new float[3] { -1f, 0f, 1f },
		new float[3] { 1f, 0f, -1f },
		new float[3] { -1f, 0f, -1f },
		new float[3] { 0f, 1f, 1f },
		new float[3] { 0f, -1f, 1f },
		new float[3] { 0f, 1f, -1f },
		new float[3] { 0f, -1f, -1f }
	};

	private const float F3 = 1f / 3f;

	private const float G3 = 1f / 6f;

	public SphericalNoise(int seed = 1337)
	{
		Random random = new Random(seed);
		byte[] array = new byte[256];
		for (int i = 0; i < 256; i++)
		{
			array[i] = (byte)i;
		}
		for (int num = 255; num > 0; num--)
		{
			int num2 = random.Next(num + 1);
			ref byte reference = ref array[num];
			ref byte reference2 = ref array[num2];
			byte b = array[num2];
			byte b2 = array[num];
			reference = b;
			reference2 = b2;
		}
		for (int j = 0; j < 512; j++)
		{
			_perm[j] = array[j & 0xFF];
			_permMod12[j] = (byte)(_perm[j] % 12);
		}
	}

	public static (float x, float y, float z) LatLonToSphere(float lat, float lon)
	{
		float x = lat * MathF.PI / 180f;
		float x2 = lon * MathF.PI / 180f;
		float num = MathF.Cos(x);
		return (x: num * MathF.Cos(x2), y: MathF.Sin(x), z: num * MathF.Sin(x2));
	}

	public static (float x, float y, float z) GridToSphere(int x, int y, int width, int height)
	{
		float lon = (float)x / (float)width * 360f - 180f;
		return LatLonToSphere(90f - (float)y / (float)height * 180f, lon);
	}

	public float Noise3D(float xin, float yin, float zin)
	{
		float num = (xin + yin + zin) * (1f / 3f);
		int num2 = (int)MathF.Floor(xin + num);
		int num3 = (int)MathF.Floor(yin + num);
		int num4 = (int)MathF.Floor(zin + num);
		float num5 = (float)(num2 + num3 + num4) * (1f / 6f);
		float num6 = (float)num2 - num5;
		float num7 = (float)num3 - num5;
		float num8 = (float)num4 - num5;
		float num9 = xin - num6;
		float num10 = yin - num7;
		float num11 = zin - num8;
		int num12;
		int num13;
		int num14;
		int num15;
		int num16;
		int num17;
		if (num9 >= num10)
		{
			if (num10 >= num11)
			{
				num12 = 1;
				num13 = 0;
				num14 = 0;
				num15 = 1;
				num16 = 1;
				num17 = 0;
			}
			else if (num9 >= num11)
			{
				num12 = 1;
				num13 = 0;
				num14 = 0;
				num15 = 1;
				num16 = 0;
				num17 = 1;
			}
			else
			{
				num12 = 0;
				num13 = 0;
				num14 = 1;
				num15 = 1;
				num16 = 0;
				num17 = 1;
			}
		}
		else if (num10 < num11)
		{
			num12 = 0;
			num13 = 0;
			num14 = 1;
			num15 = 0;
			num16 = 1;
			num17 = 1;
		}
		else if (num9 < num11)
		{
			num12 = 0;
			num13 = 1;
			num14 = 0;
			num15 = 0;
			num16 = 1;
			num17 = 1;
		}
		else
		{
			num12 = 0;
			num13 = 1;
			num14 = 0;
			num15 = 1;
			num16 = 1;
			num17 = 0;
		}
		float num18 = num9 - (float)num12 + 1f / 6f;
		float num19 = num10 - (float)num13 + 1f / 6f;
		float num20 = num11 - (float)num14 + 1f / 6f;
		float num21 = num9 - (float)num15 + 1f / 3f;
		float num22 = num10 - (float)num16 + 1f / 3f;
		float num23 = num11 - (float)num17 + 1f / 3f;
		float num24 = num9 - 1f + 0.5f;
		float num25 = num10 - 1f + 0.5f;
		float num26 = num11 - 1f + 0.5f;
		int num27 = num2 & 0xFF;
		int num28 = num3 & 0xFF;
		int num29 = num4 & 0xFF;
		float num30 = 0.6f - num9 * num9 - num10 * num10 - num11 * num11;
		float num31;
		if (num30 < 0f)
		{
			num31 = 0f;
		}
		else
		{
			num30 *= num30;
			int num32 = _permMod12[num27 + _perm[num28 + _perm[num29]]];
			num31 = num30 * num30 * (Grad3[num32][0] * num9 + Grad3[num32][1] * num10 + Grad3[num32][2] * num11);
		}
		float num33 = 0.6f - num18 * num18 - num19 * num19 - num20 * num20;
		float num34;
		if (num33 < 0f)
		{
			num34 = 0f;
		}
		else
		{
			num33 *= num33;
			int num35 = _permMod12[num27 + num12 + _perm[num28 + num13 + _perm[num29 + num14]]];
			num34 = num33 * num33 * (Grad3[num35][0] * num18 + Grad3[num35][1] * num19 + Grad3[num35][2] * num20);
		}
		float num36 = 0.6f - num21 * num21 - num22 * num22 - num23 * num23;
		float num37;
		if (num36 < 0f)
		{
			num37 = 0f;
		}
		else
		{
			num36 *= num36;
			int num38 = _permMod12[num27 + num15 + _perm[num28 + num16 + _perm[num29 + num17]]];
			num37 = num36 * num36 * (Grad3[num38][0] * num21 + Grad3[num38][1] * num22 + Grad3[num38][2] * num23);
		}
		float num39 = 0.6f - num24 * num24 - num25 * num25 - num26 * num26;
		float num40;
		if (num39 < 0f)
		{
			num40 = 0f;
		}
		else
		{
			num39 *= num39;
			int num41 = _permMod12[num27 + 1 + _perm[num28 + 1 + _perm[num29 + 1]]];
			num40 = num39 * num39 * (Grad3[num41][0] * num24 + Grad3[num41][1] * num25 + Grad3[num41][2] * num26);
		}
		return 32f * (num31 + num34 + num37 + num40);
	}

	public float Fbm(float x, float y, float z, int octaves = 6, float lacunarity = 2f, float gain = 0.5f, float frequency = 1f)
	{
		float num = 1f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = x * frequency;
		float num5 = y * frequency;
		float num6 = z * frequency;
		for (int i = 0; i < octaves; i++)
		{
			num2 += num * Noise3D(num4, num5, num6);
			num3 += num;
			num4 *= lacunarity;
			num5 *= lacunarity;
			num6 *= lacunarity;
			num *= gain;
		}
		return num2 / num3;
	}

	public float DomainWarpFbm(float x, float y, float z, float warpStrength = 0.8f, float warpFrequency = 1.2f, int octaves = 6)
	{
		float num = Fbm(x * warpFrequency + 0f, y * warpFrequency + 5.2f, z * warpFrequency + 1.3f, 4);
		float num2 = Fbm(x * warpFrequency + 5.2f, y * warpFrequency + 1.3f, z * warpFrequency + 0f, 4);
		float num3 = Fbm(x * warpFrequency + 1.3f, y * warpFrequency + 0f, z * warpFrequency + 5.2f, 4);
		return Fbm(x + warpStrength * num, y + warpStrength * num2, z + warpStrength * num3, octaves);
	}

	public static float Smootherstep(float t)
	{
		t = Math.Clamp(t, 0f, 1f);
		return t * t * t * (t * (t * 6f - 15f) + 10f);
	}

	public static float Normalize01(float v)
	{
		return (v + 1f) * 0.5f;
	}

	public float RidgedFbm(float x, float y, float z, int octaves = 6, float lacunarity = 2f, float gain = 0.5f, float frequency = 1f)
	{
		float num = 0.5f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = x * frequency;
		float num5 = y * frequency;
		float num6 = z * frequency;
		for (int i = 0; i < octaves; i++)
		{
			float num7 = 1f - MathF.Abs(Noise3D(num4, num5, num6));
			num7 *= num7;
			num2 += num * num7;
			num3 += num;
			num4 *= lacunarity;
			num5 *= lacunarity;
			num6 *= lacunarity;
			num *= gain;
		}
		return num2 / num3;
	}
}
}
