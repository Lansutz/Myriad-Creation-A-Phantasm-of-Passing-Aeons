using System;
using UnityEngine;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Render
{
    /// <summary>
    /// Cyowari 历史地图风格桥接组件
    /// 从 GameWorld 生成高程图/国家色块图/噪点图，传入 CyowariHistoricalMap Shader，
    /// 一键切换 MapRenderer 的渲染材质。挂在 MapRenderer 同一物体上。
    ///
    /// 风格特征：西北向地形浮雕晕渲 + 半透明国土色 + 黑色边界 + 争议斜纹 + 海洋色 + 纸张色调
    /// </summary>
    [RequireComponent(typeof(MapRenderer))]
    public class CyowariMapStyle : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private MapRenderer mapRenderer;
        [SerializeField] private GameWorld world;

        [Header("Shader 参数")]
        [Range(0f, 2f)] public float slopeScale = 0.6f;
        [Range(0f, 1f)] public float countryAlpha = 0.62f;
        [Range(0f, 0.02f)] public float borderThickness = 0.004f;
        public float stripeDensity = 12f;
        public Color oceanColor = new Color(0.62f, 0.72f, 0.82f, 1f);
        public Color paperTint = new Color(0.94f, 0.92f, 0.86f, 1f);

        [Header("噪点")]
        [Range(16, 256)] public int noiseResolution = 128;
        [Range(0f, 1f)] public float noiseStrength = 0.35f;

        // —— 运行时资源 ——
        private Texture2D _heightMap;
        private Texture2D _countryColorMap;
        private Texture2D _noiseTex;
        private Material _cyowariMaterial;
        private Material _originalMaterial;
        private bool _applied;

        // —— 政权色板（与 MapRenderer.GetRealmColor 逻辑一致）——
        private static readonly Color[] _politicalColors = new Color[16];
        static CyowariMapStyle()
        {
            for (int i = 0; i < 16; i++)
            {
                UnityEngine.Random.InitState(i * 1000);
                _politicalColors[i] = new Color(
                    UnityEngine.Random.Range(0.3f, 0.9f),
                    UnityEngine.Random.Range(0.3f, 0.9f),
                    UnityEngine.Random.Range(0.3f, 0.9f));
            }
        }

        private static Color GetRealmColor(int realmId)
        {
            if (realmId >= 0 && realmId < 16) return _politicalColors[realmId];
            float hue = (realmId * 0.6180339887f) % 1f;
            float sat = 0.55f + ((realmId >> 3) % 3) * 0.1f;
            float val = 0.62f + ((realmId >> 5) % 3) * 0.08f;
            return Color.HSVToRGB(hue, sat, val);
        }

        void Awake()
        {
            if (mapRenderer == null) mapRenderer = GetComponent<MapRenderer>();
        }

        /// <summary>应用 Cyowari 风格（生成纹理+切换材质）</summary>
        public void Apply()
        {
            if (mapRenderer == null || world == null)
            {
                Debug.LogError("[CyowariMapStyle] 缺少 MapRenderer 或 GameWorld 引用");
                return;
            }
            if (_applied) return;

            EnsureTextures();
            EnsureMaterial();
            RefreshTextures();

            // 保存原材质并切换
            var mr = mapRenderer.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                _originalMaterial = mr.material;
                mr.material = _cyowariMaterial;
            }
            _applied = true;
            Debug.Log("[CyowariMapStyle] 已应用 Cyowari 历史地图风格");
        }

        /// <summary>恢复原材质</summary>
        public void Revert()
        {
            if (!_applied) return;
            var mr = mapRenderer.GetComponent<MeshRenderer>();
            if (mr != null && _originalMaterial != null)
            {
                mr.material = _originalMaterial;
            }
            _applied = false;
            Debug.Log("[CyowariMapStyle] 已恢复原渲染风格");
        }

        /// <summary>重新生成输入纹理（地形/政权变化后调用）</summary>
        public void RefreshTextures()
        {
            if (world == null) return;
            GenerateHeightMap();
            GenerateCountryColorMap();
            if (_cyowariMaterial != null) UpdateMaterialParams();
        }

        // ===== 纹理生成 =====

        private void EnsureTextures()
        {
            int w = world.mapWidth;
            int h = world.mapHeight;
            if (_heightMap == null || _heightMap.width != w || _heightMap.height != h)
            {
                _heightMap = new Texture2D(w, h, TextureFormat.RFloat, false);
                _heightMap.filterMode = FilterMode.Bilinear;
                _heightMap.wrapMode = TextureWrapMode.Clamp;
            }
            if (_countryColorMap == null || _countryColorMap.width != w || _countryColorMap.height != h)
            {
                _countryColorMap = new Texture2D(w, h, TextureFormat.RGBAFloat, false);
                _countryColorMap.filterMode = FilterMode.Point;
                _countryColorMap.wrapMode = TextureWrapMode.Clamp;
            }
            if (_noiseTex == null || _noiseTex.width != noiseResolution)
            {
                _noiseTex = new Texture2D(noiseResolution, noiseResolution, TextureFormat.RFloat, false);
                _noiseTex.filterMode = FilterMode.Bilinear;
                _noiseTex.wrapMode = TextureWrapMode.Repeat;
                GenerateNoiseTexture();
            }
        }

        /// <summary>高程图：灰度，白色高山，黑色海洋</summary>
        private void GenerateHeightMap()
        {
            int w = world.mapWidth;
            int h = world.mapHeight;
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    var tile = world.tiles[idx];
                    float elev = tile.exists ? Mathf.Clamp01(tile.elevation01) : 0f;
                    // 海洋压低高程（浮雕只在陆地显现）
                    if (tile.exists && !tile.isLand) elev *= 0.15f;
                    pixels[idx] = new Color(elev, elev, elev, 1f);
                }
            }
            _heightMap.SetPixels(pixels);
            _heightMap.Apply();
        }

        /// <summary>国家色块图：RGB=政权色，A=0海洋 / 1普通国土 / >1争议区</summary>
        private void GenerateCountryColorMap()
        {
            int w = world.mapWidth;
            int h = world.mapHeight;
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    var tile = world.tiles[idx];
                    if (!tile.exists || !tile.isLand)
                    {
                        pixels[idx] = new Color(0f, 0f, 0f, 0f); // 海洋
                        continue;
                    }
                    Color c = GetRealmColor(tile.ownerRealmId);
                    // 占领中≠所有者时标记为争议区（alpha > 1 触发斜纹）
                    float alpha = (tile.occupyingRealmId >= 0 && tile.occupyingRealmId != tile.ownerRealmId)
                        ? 1.5f : 1f;
                    pixels[idx] = new Color(c.r, c.g, c.b, alpha);
                }
            }
            _countryColorMap.SetPixels(pixels);
            _countryColorMap.Apply();
        }

        /// <summary>程序化纸张噪点（确定性 hash——同参数同纹理）</summary>
        private void GenerateNoiseTexture()
        {
            int n = noiseResolution;
            var pixels = new Color[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    unchecked
                    {
                        uint hh = (uint)(x * 374761393 + y * 668265263 + 1337 * 2246822519);
                        hh = (hh ^ (hh >> 13)) * 1274126177;
                        hh = hh ^ (hh >> 16);
                        float v = (hh & 0xFFFF) / 65535f;
                        v = Mathf.Lerp(0.5f, v, noiseStrength);
                        pixels[y * n + x] = new Color(v, v, v, 1f);
                    }
                }
            }
            _noiseTex.SetPixels(pixels);
            _noiseTex.Apply();
        }

        // ===== 材质 =====

        private void EnsureMaterial()
        {
            if (_cyowariMaterial != null) return;
            var shader = Shader.Find("Custom/CyowariHistoricalMap");
            if (shader == null)
            {
                Debug.LogError("[CyowariMapStyle] 找不到 Shader 'Custom/CyowariHistoricalMap'");
                return;
            }
            _cyowariMaterial = new Material(shader);
            _cyowariMaterial.name = "CyowariHistoricalMap_Mat";
            UpdateMaterialParams();
        }

        private void UpdateMaterialParams()
        {
            if (_cyowariMaterial == null) return;
            _cyowariMaterial.SetTexture("_HeightMap", _heightMap);
            _cyowariMaterial.SetTexture("_CountryColorMap", _countryColorMap);
            _cyowariMaterial.SetTexture("_NoiseTex", _noiseTex);
            _cyowariMaterial.SetFloat("_SlopeScale", slopeScale);
            _cyowariMaterial.SetFloat("_CountryAlpha", countryAlpha);
            _cyowariMaterial.SetFloat("_BorderThickness", borderThickness);
            _cyowariMaterial.SetFloat("_StripeDensity", stripeDensity);
            _cyowariMaterial.SetColor("_OceanColor", oceanColor);
            _cyowariMaterial.SetColor("_PaperTint", paperTint);
        }

        void OnDestroy()
        {
            if (_cyowariMaterial != null) Destroy(_cyowariMaterial);
            if (_heightMap != null) Destroy(_heightMap);
            if (_countryColorMap != null) Destroy(_countryColorMap);
            if (_noiseTex != null) Destroy(_noiseTex);
        }
    }
}
