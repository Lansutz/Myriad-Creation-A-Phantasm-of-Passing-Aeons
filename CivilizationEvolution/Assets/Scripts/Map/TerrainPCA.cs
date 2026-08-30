using System;
using CivilizationEvolution.Core;
using UnityEngine;

namespace CivilizationEvolution.Map
{
    /// <summary>
    /// PCA 地形特征提取（Principal Component Analysis for Terrain Characterization）
    /// 从多维地形特征中提取主成分，用于：
    ///   1. 地形分类：平原/山地/高原/盆地/丘陵/峡谷等
    ///   2. 大陆形状分析：PCA分析大陆轮廓的主轴方向（推断板块运动方向）
    ///   3. 特征降维：将高维地形特征降维到2-3个主成分，减少后续计算量
    ///   4. 地形粗糙度/分割度量化
    ///
    /// 提取的6维地形特征：
    ///   [0] 高程（elevation）
    ///   [1] 坡度（slope）
    ///   [2] 坡向sin（aspect_sin）
    ///   [3] 坡向cos（aspect_cos）
    ///   [4] 曲率（curvature，二阶差分）
    ///   [5] 局部粗糙度（roughness，3x3标准差）
    ///
    /// PCA实现：协方差矩阵 + 幂迭代法求前K个主成分（避免复杂矩阵分解）
    /// </summary>
    public class TerrainPCA
    {
        private readonly int _width;
        private readonly int _height;
        private readonly int _featureDim = 6;

        // 输出
        public float[] PC1 { get; private set; }      // 第一主成分（整体高程/规模）
        public float[] PC2 { get; private set; }      // 第二主成分（地形粗糙度/分割度）
        public float[] PC3 { get; private set; }      // 第三主成分（坡向/形态）
        public float[] Eigenvalues { get; private set; } // 特征值（解释方差比例）
        public float[,] Eigenvectors { get; private set; } // 特征向量（主成分方向）
        public int[] TerrainClass { get; private set; }  // 地形分类（0=平原,1=丘陵,2=山地,3=高原,4=盆地,5=峡谷）
        public Vector2 ContinentPrincipalAxis { get; private set; } // 大陆主轴方向

        public TerrainPCA(int width, int height)
        {
            _width = width;
            _height = height;
            int n = width * height;
            PC1 = new float[n];
            PC2 = new float[n];
            PC3 = new float[n];
            TerrainClass = new int[n];
        }

        /// <summary>
        /// 运行PCA地形特征提取
        /// </summary>
        /// <param name="elevation">高程（0-1）</param>
        /// <param name="isLand">是否陆地</param>
        public void Run(float[] elevation, bool[] isLand)
        {
            int n = _width * _height;
            Debug.Log($"[TerrainPCA] PCA开始：{_width}x{_height}，{_featureDim}维特征");

            // 第1步：提取多维地形特征
            var features = new float[n, _featureDim];
            ExtractFeatures(elevation, isLand, features);

            // 第2步：标准化（均值为0，方差为1）
            var means = new float[_featureDim];
            var stds = new float[_featureDim];
            Standardize(features, isLand, means, stds);

            // 第3步：计算协方差矩阵
            var cov = ComputeCovariance(features, isLand);

            // 第4步：幂迭代法求前3个主成分
            Eigenvectors = new float[3, _featureDim];
            Eigenvalues = new float[3];
            PowerIteration(cov, Eigenvectors, Eigenvalues);

            // 第5步：投影到主成分空间
            for (int i = 0; i < n; i++)
            {
                if (!isLand[i]) continue;
                PC1[i] = Dot(features, i, Eigenvectors, 0);
                PC2[i] = Dot(features, i, Eigenvectors, 1);
                PC3[i] = Dot(features, i, Eigenvectors, 2);
            }

            // 第6步：基于主成分分类地形
            ClassifyTerrain(elevation, isLand);

            // 第7步：大陆主轴分析（PCA分析陆地轮廓）
            AnalyzeContinentAxis(elevation, isLand);

            float totalVar = Eigenvalues[0] + Eigenvalues[1] + Eigenvalues[2];
            Debug.Log($"[TerrainPCA] PCA完成：PC1解释{Eigenvalues[0] / totalVar * 100:F1}%，PC2={Eigenvalues[1] / totalVar * 100:F1}%，PC3={Eigenvalues[2] / totalVar * 100:F1}%");
            Debug.Log($"[TerrainPCA] 大陆主轴方向：({ContinentPrincipalAxis.x:F2}, {ContinentPrincipalAxis.y:F2})");
        }

        /// <summary>提取多维地形特征</summary>
        private void ExtractFeatures(float[] elevation, bool[] isLand, float[,] features)
        {
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    int i = y * _width + x;
                    if (!isLand[i]) continue;

                    // [0] 高程
                    features[i, 0] = elevation[i];

                    // [1] 坡度（中心差分）
                    int xL = (x - 1 + _width) % _width;
                    int xR = (x + 1) % _width;
                    int yU = Mathf.Max(0, y - 1);
                    int yD = Mathf.Min(_height - 1, y + 1);
                    float dzdx = (elevation[y * _width + xR] - elevation[y * _width + xL]) * 0.5f;
                    float dzdy = (elevation[yD * _width + x] - elevation[yU * _width + x]) * 0.5f;
                    float slope = Mathf.Sqrt(dzdx * dzdx + dzdy * dzdy);
                    features[i, 1] = slope;

                    // [2][3] 坡向（sin/cos，避免角度不连续）
                    float aspect = Mathf.Atan2(dzdy, dzdx);
                    features[i, 2] = Mathf.Sin(aspect);
                    features[i, 3] = Mathf.Cos(aspect);

                    // [4] 曲率（拉普拉斯算子，二阶差分）
                    float curvature = elevation[y * _width + xL] + elevation[y * _width + xR] +
                                      elevation[yU * _width + x] + elevation[yD * _width + x] -
                                      4f * elevation[i];
                    features[i, 4] = curvature;

                    // [5] 局部粗糙度（3x3标准差）
                    float sum = 0f, sumSq = 0f;
                    int count = 0;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = (x + dx + _width) % _width;
                            int ny = Mathf.Clamp(y + dy, 0, _height - 1);
                            int ni = ny * _width + nx;
                            if (isLand[ni])
                            {
                                sum += elevation[ni];
                                sumSq += elevation[ni] * elevation[ni];
                                count++;
                            }
                        }
                    }
                    if (count > 1)
                    {
                        float mean = sum / count;
                        float variance = sumSq / count - mean * mean;
                        features[i, 5] = Mathf.Sqrt(Mathf.Max(0f, variance));
                    }
                }
            }
        }

        /// <summary>标准化特征</summary>
        private void Standardize(float[,] features, bool[] isLand, float[] means, float[] stds)
        {
            int n = _width * _height;
            int count = 0;
            for (int i = 0; i < n; i++)
            {
                if (!isLand[i]) continue;
                count++;
                for (int d = 0; d < _featureDim; d++)
                    means[d] += features[i, d];
            }
            for (int d = 0; d < _featureDim; d++)
                means[d] /= Mathf.Max(1, count);

            for (int i = 0; i < n; i++)
            {
                if (!isLand[i]) continue;
                for (int d = 0; d < _featureDim; d++)
                {
                    float diff = features[i, d] - means[d];
                    stds[d] += diff * diff;
                }
            }
            for (int d = 0; d < _featureDim; d++)
            {
                stds[d] = Mathf.Sqrt(stds[d] / Mathf.Max(1, count));
                if (stds[d] < 1e-6f) stds[d] = 1f;
            }

            for (int i = 0; i < n; i++)
            {
                if (!isLand[i]) continue;
                for (int d = 0; d < _featureDim; d++)
                    features[i, d] = (features[i, d] - means[d]) / stds[d];
            }
        }

        /// <summary>计算协方差矩阵</summary>
        private float[,] ComputeCovariance(float[,] features, bool[] isLand)
        {
            int n = _width * _height;
            var cov = new float[_featureDim, _featureDim];
            int count = 0;

            for (int i = 0; i < n; i++)
            {
                if (!isLand[i]) continue;
                count++;
                for (int a = 0; a < _featureDim; a++)
                {
                    for (int b = 0; b < _featureDim; b++)
                    {
                        cov[a, b] += features[i, a] * features[i, b];
                    }
                }
            }
            for (int a = 0; a < _featureDim; a++)
                for (int b = 0; b < _featureDim; b++)
                    cov[a, b] /= Mathf.Max(1, count);

            return cov;
        }

        /// <summary>幂迭代法求前K个主成分</summary>
        private void PowerIteration(float[,] cov, float[,] eigenvectors, float[] eigenvalues)
        {
            int k = 3;
            var residual = (float[,])cov.Clone();

            for (int p = 0; p < k; p++)
            {
                // 随机初始化向量
                var v = new float[_featureDim];
                var rng = new System.Random(42 + p);
                for (int d = 0; d < _featureDim; d++) v[d] = (float)rng.NextDouble() - 0.5f;
                Normalize(v);

                // 幂迭代
                for (int iter = 0; iter < 100; iter++)
                {
                    var newV = new float[_featureDim];
                    for (int a = 0; a < _featureDim; a++)
                        for (int b = 0; b < _featureDim; b++)
                            newV[a] += residual[a, b] * v[b];

                    float eigenvalue = Norm(newV);
                    Normalize(newV);

                    float convergence = 0f;
                    for (int d = 0; d < _featureDim; d++)
                        convergence += Mathf.Abs(newV[d] - v[d]);

                    v = newV;
                    eigenvalues[p] = eigenvalue;

                    if (convergence < 1e-6f) break;
                }

                // 保存特征向量
                for (int d = 0; d < _featureDim; d++)
                    eigenvectors[p, d] = v[d];

                // 减去已找到的成分（deflation）
                for (int a = 0; a < _featureDim; a++)
                    for (int b = 0; b < _featureDim; b++)
                        residual[a, b] -= eigenvalues[p] * v[a] * v[b];
            }
        }

        /// <summary>基于主成分分类地形</summary>
        private void ClassifyTerrain(float[] elevation, bool[] isLand)
        {
            int n = _width * _height;
            for (int i = 0; i < n; i++)
            {
                if (!isLand[i]) { TerrainClass[i] = -1; continue; }

                float elev = elevation[i];
                float pc1 = PC1[i]; // 整体规模/高程
                float pc2 = PC2[i]; // 粗糙度/分割度

                // 基于高程+粗糙度分类
                if (elev < 0.35f && pc2 < 0.5f)
                    TerrainClass[i] = 0; // 平原
                else if (elev < 0.45f && pc2 >= 0.5f)
                    TerrainClass[i] = 1; // 丘陵
                else if (elev >= 0.6f && pc2 >= 0.8f)
                    TerrainClass[i] = 2; // 山地
                else if (elev >= 0.55f && pc2 < 0.6f)
                    TerrainClass[i] = 3; // 高原
                else if (elev < 0.4f && pc2 >= 1.0f)
                    TerrainClass[i] = 4; // 盆地/峡谷
                else
                    TerrainClass[i] = 1; // 默认丘陵
            }
        }

        /// <summary>大陆主轴分析（PCA分析陆地轮廓的主轴方向）</summary>
        private void AnalyzeContinentAxis(float[] elevation, bool[] isLand)
        {
            int n = _width * _height;
            float meanX = 0f, meanY = 0f;
            int count = 0;

            // 计算陆地质心
            for (int i = 0; i < n; i++)
            {
                if (!isLand[i]) continue;
                meanX += (i % _width);
                meanY += (i / _width);
                count++;
            }
            if (count == 0) { ContinentPrincipalAxis = Vector2.right; return; }
            meanX /= count;
            meanY /= count;

            // 计算协方差（2D）
            float covXX = 0f, covXY = 0f, covYY = 0f;
            for (int i = 0; i < n; i++)
            {
                if (!isLand[i]) continue;
                float dx = (i % _width) - meanX;
                float dy = (i / _width) - meanY;
                covXX += dx * dx;
                covXY += dx * dy;
                covYY += dy * dy;
            }

            // 2D PCA：求最大特征值对应的特征向量
            float trace = covXX + covYY;
            float det = covXX * covYY - covXY * covXY;
            float lambda1 = (trace + Mathf.Sqrt(Mathf.Max(0f, trace * trace - 4f * det))) * 0.5f;

            // 特征向量（主轴方向）
            float axisX, axisY;
            if (Mathf.Abs(covXY) > 1e-6f)
            {
                axisX = lambda1 - covYY;
                axisY = covXY;
            }
            else
            {
                axisX = covXX > covYY ? 1f : 0f;
                axisY = covXX > covYY ? 0f : 1f;
            }
            float len = Mathf.Sqrt(axisX * axisX + axisY * axisY);
            if (len > 0f)
            {
                axisX /= len;
                axisY /= len;
            }
            ContinentPrincipalAxis = new Vector2(axisX, axisY);
        }

        // ===== 工具 =====
        private static float Dot(float[,] features, int idx, float[,] eigenvectors, int pc)
        {
            float sum = 0f;
            int dim = eigenvectors.GetLength(1);
            for (int d = 0; d < dim; d++)
                sum += features[idx, d] * eigenvectors[pc, d];
            return sum;
        }

        private static void Normalize(float[] v)
        {
            float len = Norm(v);
            if (len > 1e-10f)
                for (int i = 0; i < v.Length; i++) v[i] /= len;
        }

        private static float Norm(float[] v)
        {
            float sum = 0f;
            for (int i = 0; i < v.Length; i++) sum += v[i] * v[i];
            return Mathf.Sqrt(sum);
        }
    }
}
