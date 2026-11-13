using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace MyriadCreation.World.Terrain
{
public class SphericalVoronoi
{
	public struct Seed(float x, float y, float z, int id)
	{
		public float x = x;

		public float y = y;

		public float z = z;

		public int id = id;
	}

	private readonly Random _rng;

	public Seed[] Seeds { get; private set; }

	public int SeedCount => Seeds?.Length ?? 0;

	public SphericalVoronoi(int seed = 42)
	{
		_rng = new Random(seed);
	}

	public void Generate(int seedCount, int lloydIterations = 3)
	{
		Seeds = FarthestPointSampling(seedCount);
		for (int i = 0; i < lloydIterations; i++)
		{
			LloydRelaxation();
		}
	}

	private Seed[] FarthestPointSampling(int count)
	{
		List<Seed> list = new List<Seed>(count);
		(float, float, float) tuple = RandomSpherePoint();
		list.Add(new Seed(tuple.Item1, tuple.Item2, tuple.Item3, 0));
		int num = Math.Max(100, count * 20);
		for (int i = 1; i < count; i++)
		{
			float num2 = -1f;
			(float, float, float) tuple2 = (0f, 0f, 0f);
			for (int j = 0; j < num; j++)
			{
				(float, float, float) tuple3 = RandomSpherePoint();
				float num3 = float.MaxValue;
				foreach (Seed item in list)
				{
					float num4 = SphereDistanceSq(tuple3.Item1, tuple3.Item2, tuple3.Item3, item.x, item.y, item.z);
					if (num4 < num3)
					{
						num3 = num4;
					}
				}
				if (num3 > num2)
				{
					num2 = num3;
					tuple2 = tuple3;
				}
			}
			list.Add(new Seed(tuple2.Item1, tuple2.Item2, tuple2.Item3, i));
		}
		return list.ToArray();
	}

	private void LloydRelaxation()
	{
		int num = Seeds.Length * 500;
		(float, float, float, int)[] array = new (float, float, float, int)[Seeds.Length];
		for (int i = 0; i < num; i++)
		{
			(float, float, float) tuple = RandomSpherePoint();
			int num2 = FindNearestSeed(tuple.Item1, tuple.Item2, tuple.Item3);
			array[num2].Item1 += tuple.Item1;
			array[num2].Item2 += tuple.Item2;
			array[num2].Item3 += tuple.Item3;
			array[num2].Item4++;
		}
		for (int j = 0; j < Seeds.Length; j++)
		{
			if (array[j].Item4 > 0)
			{
				float num3 = Mathf.Sqrt(array[j].Item1 * array[j].Item1 + array[j].Item2 * array[j].Item2 + array[j].Item3 * array[j].Item3);
				if (num3 > 0.001f)
				{
					Seeds[j].x = array[j].Item1 / num3;
					Seeds[j].y = array[j].Item2 / num3;
					Seeds[j].z = array[j].Item3 / num3;
				}
			}
		}
	}

	public int FindNearestSeed(float x, float y, float z)
	{
		int result = 0;
		float num = float.MaxValue;
		for (int i = 0; i < Seeds.Length; i++)
		{
			float num2 = SphereDistanceSq(x, y, z, Seeds[i].x, Seeds[i].y, Seeds[i].z);
			if (num2 < num)
			{
				num = num2;
				result = i;
			}
		}
		return result;
	}

	public int[] AssignToGrid(int width, int height)
	{
		int[] array = new int[width * height];
		for (int i = 0; i < height; i++)
		{
			for (int j = 0; j < width; j++)
			{
				var (x, y, z) = SphericalNoise.GridToSphere(j, i, width, height);
				array[i * width + j] = FindNearestSeed(x, y, z);
			}
		}
		return array;
	}

	public bool[] DetectBoundaries(int[] grid, int width, int height, bool wrapX = true)
	{
		bool[] array = new bool[width * height];
		for (int i = 0; i < height; i++)
		{
			for (int j = 0; j < width; j++)
			{
				int num = i * width + j;
				int num2 = grid[num];
				if (j > 0 && grid[num - 1] != num2)
				{
					array[num] = true;
				}
				else if (j < width - 1 && grid[num + 1] != num2)
				{
					array[num] = true;
				}
				else if (i > 0 && grid[num - width] != num2)
				{
					array[num] = true;
				}
				else if (i < height - 1 && grid[num + width] != num2)
				{
					array[num] = true;
				}
				else if (wrapX && j == 0 && grid[i * width + width - 1] != num2)
				{
					array[num] = true;
				}
				else if (wrapX && j == width - 1 && grid[i * width] != num2)
				{
					array[num] = true;
				}
			}
		}
		return array;
	}

	private (float x, float y, float z) RandomSpherePoint()
	{
		float num = Mathf.Acos((float)_rng.NextDouble() * 2f - 1f);
		float num2 = (float)_rng.NextDouble() * MathF.PI * 2f;
		return (x: Mathf.Sin(num) * Mathf.Cos(num2), y: Mathf.Cos(num), z: Mathf.Sin(num) * Mathf.Sin(num2));
	}

	private static float SphereDistanceSq(float x1, float y1, float z1, float x2, float y2, float z2)
	{
		float num = x1 * x2 + y1 * y2 + z1 * z2;
		return 1f - num;
	}
}
}
