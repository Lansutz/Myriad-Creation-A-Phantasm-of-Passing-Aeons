using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyriadCreation.Core.Data;

namespace MyriadCreation.World
{
public class ProvinceGenerator
{
	private readonly TileData[] _tiles;

	private readonly int _width;

	private readonly int _height;

	private readonly bool _wrapX;

	private int[] _seedX;

	private int[] _seedY;

	private int[] _bucketHead;

	private int[] _bucketNext;

	private int _bucketWidth;

	private int _bucketHeight;

	private int _bucketSize;

	public const int DefaultCellsPerProvince = 64;

	public const int DefaultLloydIterations = 3;

	public ProvinceGenerator(TileData[] tiles, int width, int height, bool wrapX)
	{
		_tiles = tiles ?? throw new ArgumentNullException("tiles");
		_width = width;
		_height = height;
		_wrapX = wrapX;
	}

	public static Task<Dictionary<int, Province>> GenerateAsync(TileData[] tiles, int width, int height, bool wrapX, int seed, int cellsPerProvince, int lloydIterations, Action<float> progress = null)
	{
		return Task.Run(() => new ProvinceGenerator(tiles, width, height, wrapX).Generate(seed, cellsPerProvince, lloydIterations, progress));
	}

	public static int CellsPerProvinceFor(int landCount, int targetProvinceCount)
	{
		if (targetProvinceCount <= 0)
		{
			return 64;
		}
		if (landCount <= 0)
		{
			return 1;
		}
		return Math.Max(8, landCount / targetProvinceCount);
	}

	public Dictionary<int, Province> Generate(int seed, int cellsPerProvince = 64, int lloydIterations = 3)
	{
		return Generate(seed, cellsPerProvince, lloydIterations, null);
	}

	public Dictionary<int, Province> Generate(int seed, int cellsPerProvince, int lloydIterations, Action<float> progress)
	{
		if (_tiles.Length == 0)
		{
			return new Dictionary<int, Province>();
		}
		Random rng = new Random(seed);
		for (int i = 0; i < _tiles.Length; i++)
		{
			_tiles[i].provinceId = -1;
		}
		int num = 0;
		int[] array = new int[_tiles.Length];
		for (int j = 0; j < _tiles.Length; j++)
		{
			if (_tiles[j].isLand)
			{
				array[num++] = j;
			}
		}
		if (num == 0)
		{
			return new Dictionary<int, Province>();
		}
		int val = Math.Max(1, num / Math.Max(1, cellsPerProvince));
		val = Math.Min(val, num);
		int[] array2 = SampleSeeds(array, num, val, rng);
		progress?.Invoke(0.1f);
		int[] array3 = new int[_tiles.Length];
		Array.Fill(array3, -1);
		int[] array4 = new int[val];
		int[] array5 = new int[val];
		int[] array6 = new int[val];
		int num2 = Math.Max(0, lloydIterations);
		for (int k = 0; k < num2; k++)
		{
			BuildSeedIndex(array2, num);
			for (int l = 0; l < num; l++)
			{
				int num3 = array[l];
				array3[num3] = FindNearestSeed(num3 % _width, num3 / _width);
			}
			Array.Clear(array4, 0, array4.Length);
			Array.Clear(array5, 0, array5.Length);
			Array.Clear(array6, 0, array6.Length);
			for (int m = 0; m < num; m++)
			{
				int num4 = array[m];
				int num5 = array3[num4];
				if (num5 >= 0)
				{
					array4[num5] += num4 % _width;
					array5[num5] += num4 / _width;
					array6[num5]++;
				}
			}
			for (int n = 0; n < val; n++)
			{
				if (array6[n] != 0)
				{
					int num6 = array4[n] / array6[n];
					int num7 = array5[n] / array6[n] * _width + num6;
					if (num7 >= 0 && num7 < _tiles.Length && _tiles[num7].isLand)
					{
						array2[n] = num7;
					}
				}
			}
			progress?.Invoke(0.1f + 0.72f * (float)(k + 1) / (float)Math.Max(1, num2));
		}
		BuildSeedIndex(array2, num);
		for (int num8 = 0; num8 < num; num8++)
		{
			int num9 = array[num8];
			array3[num9] = FindNearestSeed(num9 % _width, num9 / _width);
		}
		Dictionary<int, Province> dictionary = new Dictionary<int, Province>(val);
		for (int num10 = 0; num10 < val; num10++)
		{
			dictionary[num10] = new Province
			{
				provinceId = num10,
				provinceName = GenerateProvinceName(_tiles[array2[num10]]),
				centerTileIndex = array2[num10],
				memberTiles = new List<int>(Math.Max(1, cellsPerProvince))
			};
		}
		for (int num11 = 0; num11 < num; num11++)
		{
			int num12 = array[num11];
			int num13 = array3[num12];
			if (num13 >= 0)
			{
				dictionary[num13].memberTiles.Add(num12);
				_tiles[num12].provinceId = num13;
			}
		}
		progress?.Invoke(1f);
		return dictionary;
	}

	private static int[] SampleSeeds(int[] land, int landCount, int count, Random rng)
	{
		for (int i = 0; i < count; i++)
		{
			int num = i + rng.Next(landCount - i);
			int num2 = land[i];
			land[i] = land[num];
			land[num] = num2;
		}
		int[] array = new int[count];
		Array.Copy(land, array, count);
		return array;
	}

	private void BuildSeedIndex(int[] seeds, int landCount)
	{
		int num = seeds.Length;
		_seedX = new int[num];
		_seedY = new int[num];
		for (int i = 0; i < num; i++)
		{
			_seedX[i] = seeds[i] % _width;
			_seedY[i] = seeds[i] / _width;
		}
		float num2 = (float)landCount / (float)Math.Max(1, num);
		_bucketSize = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(num2) * 2.0));
		_bucketWidth = (_width + _bucketSize - 1) / _bucketSize;
		_bucketHeight = (_height + _bucketSize - 1) / _bucketSize;
		int num3 = Math.Max(1, _bucketWidth * _bucketHeight);
		_bucketHead = new int[num3];
		Array.Fill(_bucketHead, -1);
		_bucketNext = new int[num];
		Array.Fill(_bucketNext, -1);
		for (int j = 0; j < num; j++)
		{
			int num4 = _seedX[j] / _bucketSize;
			int num5 = _seedY[j] / _bucketSize;
			if (num4 >= 0 && num4 < _bucketWidth && num5 >= 0 && num5 < _bucketHeight)
			{
				int num6 = num5 * _bucketWidth + num4;
				_bucketNext[j] = _bucketHead[num6];
				_bucketHead[num6] = j;
			}
		}
	}

	private int FindNearestSeed(int x, int y)
	{
		int[] seedX = _seedX;
		if (seedX == null || seedX.Length == 0)
		{
			return -1;
		}
		if (_bucketHead == null || _bucketSize <= 0)
		{
			return BruteNearest(x, y);
		}
		int num = x / _bucketSize;
		int num2 = y / _bucketSize;
		int num3 = -1;
		float num4 = float.MaxValue;
		int num5 = Math.Max(_bucketWidth, _bucketHeight) + 1;
		for (int i = 0; i <= num5; i++)
		{
			int num6 = num - i;
			int num7 = num + i;
			int num8 = num2 - i;
			int num9 = num2 + i;
			for (int j = num8; j <= num9; j++)
			{
				if (j < 0 || j >= _bucketHeight)
				{
					continue;
				}
				bool flag = j == num8 || j == num9;
				for (int k = num6; k <= num7; k++)
				{
					if (!flag && i > 0 && k != num6 && k != num7)
					{
						continue;
					}
					int num10 = k;
					if (_wrapX)
					{
						num10 %= _bucketWidth;
						if (num10 < 0)
						{
							num10 += _bucketWidth;
						}
					}
					else if (k < 0 || k >= _bucketWidth)
					{
						continue;
					}
					int num11 = j * _bucketWidth + num10;
					for (int num12 = _bucketHead[num11]; num12 >= 0; num12 = _bucketNext[num12])
					{
						int num13 = _seedX[num12];
						int num14 = _seedY[num12];
						int num15 = Math.Abs(x - num13);
						if (_wrapX)
						{
							num15 = Math.Min(num15, _width - num15);
						}
						int num16 = y - num14;
						float num17 = num15 * num15 + num16 * num16;
						if (num17 < num4)
						{
							num4 = num17;
							num3 = num12;
						}
					}
				}
			}
			if (num3 >= 0)
			{
				float num18 = (float)Math.Max(0, i - 1) * (float)_bucketSize;
				if (num18 * num18 > num4)
				{
					break;
				}
			}
		}
		if (num3 < 0)
		{
			return BruteNearest(x, y);
		}
		return num3;
	}

	private int BruteNearest(int x, int y)
	{
		int result = 0;
		float num = float.MaxValue;
		for (int i = 0; i < _seedX.Length; i++)
		{
			int num2 = Math.Abs(x - _seedX[i]);
			if (_wrapX)
			{
				num2 = Math.Min(num2, _width - num2);
			}
			int num3 = y - _seedY[i];
			float num4 = num2 * num2 + num3 * num3;
			if (num4 < num)
			{
				num = num4;
				result = i;
			}
		}
		return result;
	}

	private static string GenerateProvinceName(TileData center)
	{
		string text = ((center.elevation01 > 0.55f) ? "山地" : "平原");
		if (center.isCoast)
		{
			text = "滨海" + text;
		}
		string text2;
		if (center.annualPrecipMm > 900f)
		{
			text2 = "青";
		}
		else
		{
			text2 = ((center.annualPrecipMm < 300f) ? "赤" : "沃");
		}
		return text2 + text + "原";
	}
}
}
