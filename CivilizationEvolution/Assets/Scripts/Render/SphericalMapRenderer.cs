using UnityEngine;

namespace CivilizationEvolution.Render
{
    /// <summary>
    /// 球形地图渲染器
    /// 将平面地图纹理通过经纬度UV映射到球面上
    /// 支持相机围绕球面旋转查看
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SphericalMapRenderer : MonoBehaviour
    {
        [Header("球面参数")]
        [Tooltip("球面半径")]
        public float radius = 10f;

        [Tooltip("经度分段数（越高越平滑）")]
        [Range(16, 256)] public int longitudeSegments = 128;

        [Tooltip("纬度分段数（越高越平滑）")]
        [Range(8, 128)] public int latitudeSegments = 64;

        [Header("引用")]
        [Tooltip("平面地图渲染器（获取其输出纹理）")]
        public MapRenderer planarRenderer;

        [Header("相机控制")]
        [Tooltip("是否启用鼠标拖拽旋转球面")]
        public bool enableMouseRotation = true;

        [Tooltip("旋转速度")]
        public float rotationSpeed = 0.5f;

        [Tooltip("自动旋转速度（0为不旋转）")]
        public float autoRotateSpeed = 0f;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Mesh _sphereMesh;
        private Texture2D _mapTexture;
        private Material _sphereMaterial;

        // 鼠标旋转状态
        private bool _isDragging;
        private Vector2 _lastMousePos;
        private float _rotationX;
        private float _rotationY;

        void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            CreateSphereMesh();
            CreateMaterial();
        }

        void Start()
        {
            if (planarRenderer != null)
            {
                // 延迟一帧获取纹理（确保MapRenderer已初始化）
                Invoke(nameof(ApplyPlanarTexture), 0.1f);
            }
        }

        void Update()
        {
            HandleMouseRotation();
            if (autoRotateSpeed > 0f)
            {
                _rotationY += autoRotateSpeed * Time.deltaTime;
                UpdateRotation();
            }
        }

        // ===== 创建球面Mesh =====
        private void CreateSphereMesh()
        {
            _sphereMesh = new Mesh();
            _sphereMesh.name = "SphericalMapMesh";

            int vertCount = (longitudeSegments + 1) * (latitudeSegments + 1);
            Vector3[] vertices = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            int[] triangles = new int[longitudeSegments * latitudeSegments * 6];

            int vertIndex = 0;
            int triIndex = 0;

            for (int lat = 0; lat <= latitudeSegments; lat++)
            {
                // 纬度：从北极(90°)到南极(-90°)
                float v = (float)lat / latitudeSegments;
                float phi = Mathf.PI * v; // 0到PI

                for (int lon = 0; lon <= longitudeSegments; lon++)
                {
                    // 经度：0到360°
                    float u = (float)lon / longitudeSegments;
                    float theta = 2f * Mathf.PI * u; // 0到2PI

                    // 球面坐标转笛卡尔坐标
                    float sinPhi = Mathf.Sin(phi);
                    float cosPhi = Mathf.Cos(phi);
                    float sinTheta = Mathf.Sin(theta);
                    float cosTheta = Mathf.Cos(theta);

                    vertices[vertIndex] = new Vector3(
                        radius * sinPhi * cosTheta,
                        radius * cosPhi,
                        radius * sinPhi * sinTheta
                    );

                    // UV映射：u=经度(0-1), v=纬度(0-1，翻转使北极在上)
                    uvs[vertIndex] = new Vector2(u, 1f - v);

                    vertIndex++;
                }
            }

            // 生成三角形
            for (int lat = 0; lat < latitudeSegments; lat++)
            {
                for (int lon = 0; lon < longitudeSegments; lon++)
                {
                    int current = lat * (longitudeSegments + 1) + lon;
                    int next = current + longitudeSegments + 1;

                    // 第一个三角形
                    triangles[triIndex++] = current;
                    triangles[triIndex++] = next;
                    triangles[triIndex++] = current + 1;

                    // 第二个三角形
                    triangles[triIndex++] = current + 1;
                    triangles[triIndex++] = next;
                    triangles[triIndex++] = next + 1;
                }
            }

            _sphereMesh.vertices = vertices;
            _sphereMesh.uv = uvs;
            _sphereMesh.triangles = triangles;
            _sphereMesh.RecalculateNormals();
            _sphereMesh.RecalculateBounds();

            _meshFilter.mesh = _sphereMesh;
        }

        // ===== 创建材质 =====
        private void CreateMaterial()
        {
            // 使用Unlit/Texture着色器，不受光照影响，保证地图颜色准确
            Shader shader = Shader.Find("Unlit/Texture");
            if (shader == null) shader = Shader.Find("Standard");

            _sphereMaterial = new Material(shader);
            _sphereMaterial.name = "SphericalMapMaterial";

            if (_mapTexture != null)
            {
                _sphereMaterial.mainTexture = _mapTexture;
            }

            _meshRenderer.material = _sphereMaterial;
        }

        // ===== 应用平面地图纹理 =====
        public void ApplyPlanarTexture()
        {
            if (planarRenderer == null)
            {
                Debug.LogWarning("[SphericalMapRenderer] planarRenderer未设置，无法应用纹理");
                return;
            }

            // 通过反射获取MapRenderer的mapTexture（私有字段）
            var field = typeof(MapRenderer).GetField("mapTexture",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                _mapTexture = field.GetValue(planarRenderer) as Texture2D;
                if (_mapTexture != null && _sphereMaterial != null)
                {
                    _sphereMaterial.mainTexture = _mapTexture;
                    Debug.Log($"[SphericalMapRenderer] 已应用平面纹理: {_mapTexture.width}x{_mapTexture.height}");
                }
            }
            else
            {
                Debug.LogWarning("[SphericalMapRenderer] 无法获取MapRenderer.mapTexture字段");
            }
        }

        /// <summary>手动设置地图纹理</summary>
        public void SetTexture(Texture2D texture)
        {
            _mapTexture = texture;
            if (_sphereMaterial != null)
            {
                _sphereMaterial.mainTexture = texture;
            }
        }

        /// <summary>刷新纹理（当地图重新生成后调用）</summary>
        public void RefreshTexture()
        {
            ApplyPlanarTexture();
        }

        // ===== 鼠标旋转控制 =====
        private void HandleMouseRotation()
        {
            if (!enableMouseRotation) return;

            if (Input.GetMouseButtonDown(0))
            {
                _isDragging = true;
                _lastMousePos = Input.mousePosition;
            }
            if (Input.GetMouseButtonUp(0))
            {
                _isDragging = false;
            }

            if (_isDragging)
            {
                Vector2 delta = (Vector2)Input.mousePosition - _lastMousePos;
                _rotationY += delta.x * rotationSpeed * 0.1f;
                _rotationX -= delta.y * rotationSpeed * 0.1f;
                _rotationX = Mathf.Clamp(_rotationX, -89f, 89f);
                UpdateRotation();
                _lastMousePos = Input.mousePosition;
            }
        }

        private void UpdateRotation()
        {
            transform.rotation = Quaternion.Euler(_rotationX, _rotationY, 0f);
        }

        /// <summary>重置球面旋转</summary>
        public void ResetRotation()
        {
            _rotationX = 0f;
            _rotationY = 0f;
            UpdateRotation();
        }

        // ===== 经纬度坐标转换工具 =====

        /// <summary>
        /// 经纬度转球面坐标
        /// </summary>
        /// <param name="longitude">经度(-180到180)</param>
        /// <param name="latitude">纬度(-90到90)</param>
        /// <returns>球面上的3D坐标</returns>
        public Vector3 LatLonToSpherePoint(float longitude, float latitude)
        {
            float lonRad = longitude * Mathf.Deg2Rad;
            float latRad = latitude * Mathf.Deg2Rad;

            float cosLat = Mathf.Cos(latRad);
            return new Vector3(
                radius * cosLat * Mathf.Cos(lonRad),
                radius * Mathf.Sin(latRad),
                radius * cosLat * Mathf.Sin(lonRad)
            );
        }

        /// <summary>
        /// 球面坐标转经纬度
        /// </summary>
        /// <param name="point">球面上的3D坐标</param>
        /// <returns>(经度, 纬度)</returns>
        public (float longitude, float latitude) SpherePointToLatLon(Vector3 point)
        {
            point = point.normalized * radius;
            float latitude = Mathf.Asin(point.y / radius) * Mathf.Rad2Deg;
            float longitude = Mathf.Atan2(point.z, point.x) * Mathf.Rad2Deg;
            return (longitude, latitude);
        }

        /// <summary>
        /// 平面地图UV坐标转经纬度
        /// </summary>
        /// <param name="u">平面UV的u(0-1)</param>
        /// <param name="v">平面UV的v(0-1)</param>
        /// <returns>(经度, 纬度)</returns>
        public static (float longitude, float latitude) UVToLatLon(float u, float v)
        {
            float longitude = (u * 360f) - 180f;
            float latitude = (v * 180f) - 90f;
            return (longitude, latitude);
        }

        /// <summary>
        /// 经纬度转平面地图UV坐标
        /// </summary>
        /// <param name="longitude">经度(-180到180)</param>
        /// <param name="latitude">纬度(-90到90)</param>
        /// <returns>(u, v) 0-1</returns>
        public static (float u, float v) LatLonToUV(float longitude, float latitude)
        {
            float u = (longitude + 180f) / 360f;
            float v = (latitude + 90f) / 180f;
            return (u, v);
        }
    }
}
