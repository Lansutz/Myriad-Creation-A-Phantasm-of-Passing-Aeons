using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using Random = System.Random;

namespace MyriadCreation.World.Terrain
{
public class TerrainPCA
{
	private readonly int _width;

	private readonly int _height;

	private readonly int _featureDim = 6;

	public float[] PC1 { get; private set; }

	public float[] PC2 { get; private set; }

	public float[] PC3 { get; private set; }

	public float[] Eigenvalues { get; private set; }

	public float[,] Eigenvectors { get; private set; }

	public int[] TerrainClass { get; private set; }

	private Vector2 _continentPrincipalAxis;

	public Vector2 ContinentPrincipalAxis
	{
		[CompilerGenerated]
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			return _continentPrincipalAxis;
		}
		[CompilerGenerated]
		private set
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			_continentPrincipalAxis = value;
		}
	}

	public TerrainPCA(int width, int height)
	{
		_width = width;
		_height = height;
		int num = width * height;
		PC1 = new float[num];
		PC2 = new float[num];
		PC3 = new float[num];
		TerrainClass = new int[num];
	}

	public void Run(float[] elevation, bool[] isLand)
	{
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		int num = _width * _height;
		Debug.Log((object)$"[TerrainPCA] PCA开始：{_width}x{_height}，{_featureDim}维特征");
		float[,] features = new float[num, _featureDim];
		ExtractFeatures(elevation, isLand, features);
		float[] means = new float[_featureDim];
		float[] stds = new float[_featureDim];
		Standardize(features, isLand, means, stds);
		float[,] cov = ComputeCovariance(features, isLand);
		Eigenvectors = new float[3, _featureDim];
		Eigenvalues = new float[3];
		PowerIteration(cov, Eigenvectors, Eigenvalues);
		for (int i = 0; i < num; i++)
		{
			if (isLand[i])
			{
				PC1[i] = Dot(features, i, Eigenvectors, 0);
				PC2[i] = Dot(features, i, Eigenvectors, 1);
				PC3[i] = Dot(features, i, Eigenvectors, 2);
			}
		}
		ClassifyTerrain(elevation, isLand);
		AnalyzeContinentAxis(elevation, isLand);
		float num2 = Eigenvalues[0] + Eigenvalues[1] + Eigenvalues[2];
		Debug.Log((object)$"[TerrainPCA] PCA完成：PC1解释{Eigenvalues[0] / num2 * 100f:F1}%，PC2={Eigenvalues[1] / num2 * 100f:F1}%，PC3={Eigenvalues[2] / num2 * 100f:F1}%");
		Debug.Log((object)$"[TerrainPCA] 大陆主轴方向：({ContinentPrincipalAxis.x:F2}, {ContinentPrincipalAxis.y:F2})");
	}

	private void ExtractFeatures(float[] elevation, bool[] isLand, float[,] features)
	{
		for (int i = 0; i < _height; i++)
		{
			for (int j = 0; j < _width; j++)
			{
				int num = i * _width + j;
				if (!isLand[num])
				{
					continue;
				}
				features[num, 0] = elevation[num];
				int num2 = (j - 1 + _width) % _width;
				int num3 = (j + 1) % _width;
				int num4 = Mathf.Max(0, i - 1);
				int num5 = Mathf.Min(_height - 1, i + 1);
				float num6 = (elevation[i * _width + num3] - elevation[i * _width + num2]) * 0.5f;
				float num7 = (elevation[num5 * _width + j] - elevation[num4 * _width + j]) * 0.5f;
				float num8 = Mathf.Sqrt(num6 * num6 + num7 * num7);
				features[num, 1] = num8;
				float num9 = Mathf.Atan2(num7, num6);
				features[num, 2] = Mathf.Sin(num9);
				features[num, 3] = Mathf.Cos(num9);
				float num10 = elevation[i * _width + num2] + elevation[i * _width + num3] + elevation[num4 * _width + j] + elevation[num5 * _width + j] - 4f * elevation[num];
				features[num, 4] = num10;
				float num11 = 0f;
				float num12 = 0f;
				int num13 = 0;
				for (int k = -1; k <= 1; k++)
				{
					for (int l = -1; l <= 1; l++)
					{
						int num14 = (j + l + _width) % _width;
						int num15 = Mathf.Clamp(i + k, 0, _height - 1) * _width + num14;
						if (isLand[num15])
						{
							num11 += elevation[num15];
							num12 += elevation[num15] * elevation[num15];
							num13++;
						}
					}
				}
				if (num13 > 1)
				{
					float num16 = num11 / (float)num13;
					float num17 = num12 / (float)num13 - num16 * num16;
					features[num, 5] = Mathf.Sqrt(Mathf.Max(0f, num17));
				}
			}
		}
	}

	private void Standardize(float[,] features, bool[] isLand, float[] means, float[] stds)
	{
		int num = _width * _height;
		int num2 = 0;
		for (int i = 0; i < num; i++)
		{
			if (isLand[i])
			{
				num2++;
				for (int j = 0; j < _featureDim; j++)
				{
					means[j] += features[i, j];
				}
			}
		}
		for (int k = 0; k < _featureDim; k++)
		{
			means[k] /= Mathf.Max(1, num2);
		}
		for (int l = 0; l < num; l++)
		{
			if (isLand[l])
			{
				for (int m = 0; m < _featureDim; m++)
				{
					float num3 = features[l, m] - means[m];
					stds[m] += num3 * num3;
				}
			}
		}
		for (int n = 0; n < _featureDim; n++)
		{
			stds[n] = Mathf.Sqrt(stds[n] / (float)Mathf.Max(1, num2));
			if (stds[n] < 1E-06f)
			{
				stds[n] = 1f;
			}
		}
		for (int num4 = 0; num4 < num; num4++)
		{
			if (isLand[num4])
			{
				for (int num5 = 0; num5 < _featureDim; num5++)
				{
					features[num4, num5] = (features[num4, num5] - means[num5]) / stds[num5];
				}
			}
		}
	}

	private float[,] ComputeCovariance(float[,] features, bool[] isLand)
	{
		int num = _width * _height;
		float[,] array = new float[_featureDim, _featureDim];
		int num2 = 0;
		for (int i = 0; i < num; i++)
		{
			if (!isLand[i])
			{
				continue;
			}
			num2++;
			for (int j = 0; j < _featureDim; j++)
			{
				for (int k = 0; k < _featureDim; k++)
				{
					array[j, k] += features[i, j] * features[i, k];
				}
			}
		}
		for (int l = 0; l < _featureDim; l++)
		{
			for (int m = 0; m < _featureDim; m++)
			{
				array[l, m] /= Mathf.Max(1, num2);
			}
		}
		return array;
	}

	private void PowerIteration(float[,] cov, float[,] eigenvectors, float[] eigenvalues)
	{
		int num = 3;
		float[,] array = (float[,])cov.Clone();
		for (int i = 0; i < num; i++)
		{
			float[] array2 = new float[_featureDim];
			Random random = new Random(42 + i);
			for (int j = 0; j < _featureDim; j++)
			{
				array2[j] = (float)random.NextDouble() - 0.5f;
			}
			Normalize(array2);
			for (int k = 0; k < 100; k++)
			{
				float[] array3 = new float[_featureDim];
				for (int l = 0; l < _featureDim; l++)
				{
					for (int m = 0; m < _featureDim; m++)
					{
						array3[l] += array[l, m] * array2[m];
					}
				}
				float num2 = Norm(array3);
				Normalize(array3);
				float num3 = 0f;
				for (int n = 0; n < _featureDim; n++)
				{
					num3 += Mathf.Abs(array3[n] - array2[n]);
				}
				array2 = array3;
				eigenvalues[i] = num2;
				if (num3 < 1E-06f)
				{
					break;
				}
			}
			for (int num4 = 0; num4 < _featureDim; num4++)
			{
				eigenvectors[i, num4] = array2[num4];
			}
			for (int num5 = 0; num5 < _featureDim; num5++)
			{
				for (int num6 = 0; num6 < _featureDim; num6++)
				{
					array[num5, num6] -= eigenvalues[i] * array2[num5] * array2[num6];
				}
			}
		}
	}

	private void ClassifyTerrain(float[] elevation, bool[] isLand)
	{
		int num = _width * _height;
		for (int i = 0; i < num; i++)
		{
			if (!isLand[i])
			{
				TerrainClass[i] = -1;
				continue;
			}
			float num2 = elevation[i];
			_ = PC1[i];
			float num3 = PC2[i];
			if (num2 < 0.35f && num3 < 0.5f)
			{
				TerrainClass[i] = 0;
			}
			else if (num2 < 0.45f && num3 >= 0.5f)
			{
				TerrainClass[i] = 1;
			}
			else if (num2 >= 0.6f && num3 >= 0.8f)
			{
				TerrainClass[i] = 2;
			}
			else if (num2 >= 0.55f && num3 < 0.6f)
			{
				TerrainClass[i] = 3;
			}
			else if (num2 < 0.4f && num3 >= 1f)
			{
				TerrainClass[i] = 4;
			}
			else
			{
				TerrainClass[i] = 1;
			}
		}
	}

	private void AnalyzeContinentAxis(float[] elevation, bool[] isLand)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		int num = _width * _height;
		float num2 = 0f;
		float num3 = 0f;
		int num4 = 0;
		for (int i = 0; i < num; i++)
		{
			if (isLand[i])
			{
				num2 += (float)(i % _width);
				num3 += (float)(i / _width);
				num4++;
			}
		}
		if (num4 == 0)
		{
			ContinentPrincipalAxis = Vector2.right;
			return;
		}
		num2 /= (float)num4;
		num3 /= (float)num4;
		float num5 = 0f;
		float num6 = 0f;
		float num7 = 0f;
		for (int j = 0; j < num; j++)
		{
			if (isLand[j])
			{
				float num8 = (float)(j % _width) - num2;
				float num9 = (float)(j / _width) - num3;
				num5 += num8 * num8;
				num6 += num8 * num9;
				num7 += num9 * num9;
			}
		}
		float num10 = num5 + num7;
		float num11 = num5 * num7 - num6 * num6;
		float num12 = (num10 + Mathf.Sqrt(Mathf.Max(0f, num10 * num10 - 4f * num11))) * 0.5f;
		float num13;
		float num14;
		if (Mathf.Abs(num6) > 1E-06f)
		{
			num13 = num12 - num7;
			num14 = num6;
		}
		else
		{
			num13 = ((num5 > num7) ? 1f : 0f);
			num14 = ((num5 > num7) ? 0f : 1f);
		}
		float num15 = Mathf.Sqrt(num13 * num13 + num14 * num14);
		if (num15 > 0f)
		{
			num13 /= num15;
			num14 /= num15;
		}
		ContinentPrincipalAxis = new Vector2(num13, num14);
	}

	private static float Dot(float[,] features, int idx, float[,] eigenvectors, int pc)
	{
		float num = 0f;
		int length = eigenvectors.GetLength(1);
		for (int i = 0; i < length; i++)
		{
			num += features[idx, i] * eigenvectors[pc, i];
		}
		return num;
	}

	private static void Normalize(float[] v)
	{
		float num = Norm(v);
		if (num > 1E-10f)
		{
			for (int i = 0; i < v.Length; i++)
			{
				v[i] /= num;
			}
		}
	}

	private static float Norm(float[] v)
	{
		float num = 0f;
		for (int i = 0; i < v.Length; i++)
		{
			num += v[i] * v[i];
		}
		return Mathf.Sqrt(num);
	}
}
}
