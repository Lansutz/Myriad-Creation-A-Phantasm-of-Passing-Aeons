using UnityEngine;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Core
{
    /// <summary>
    /// 游戏启动入口
    /// 挂载到场景中的Bootstrap物体上，自动初始化GameManager和GameWorld
    /// </summary>
    public class Bootstrap : MonoBehaviour
    {
        [Header("地图配置")]
        [SerializeField] private int mapWidth = 128;
        [SerializeField] private int mapHeight = 64;
        [SerializeField] private int randomSeed = 42;
        [SerializeField] private bool autoStartOnBoot = true;

        [Header("引用")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Light directionalLight;

        private void Awake()
        {
            Debug.Log($"[{GameConstants.GameNameShort}] {GameConstants.GameNameEn} | {GameConstants.GameNameZh} | v{GameConstants.Version}");

            // 加载内容注册表（Base/Mods 双目录数据驱动）
            ContentRegistry.Initialize();

            // 确保GameManager存在
            if (GameManager.Instance == null)
            {
                var gmObj = new GameObject("GameManager");
                gmObj.AddComponent<GameManager>();
            }

            // 相机设置
            if (mainCamera == null)
                mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.orthographic = true;
                mainCamera.orthographicSize = 50f;
                mainCamera.transform.position = new Vector3(64f, 100f, 32f);
                mainCamera.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
                mainCamera.backgroundColor = new Color(0.1f, 0.15f, 0.25f);
            }

            // 灯光设置
            if (directionalLight != null)
            {
                directionalLight.type = LightType.Directional;
                directionalLight.intensity = 1.2f;
                directionalLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            Debug.Log("[Bootstrap] 游戏初始化完成");
        }

        private void Start()
        {
            if (autoStartOnBoot)
            {
                StartNewGame();
            }
        }

        /// <summary>开始新游戏</summary>
        public void StartNewGame()
        {
            GameManager.Instance.StartNewGame(mapWidth, mapHeight, randomSeed);
            Debug.Log($"[Bootstrap] 新游戏已启动：{mapWidth}x{mapHeight}，种子={randomSeed}");
        }

        /// <summary>保存游戏</summary>
        public void SaveGame(string saveName = "autosave")
        {
            GameManager.Instance.SaveGame(saveName);
        }

        /// <summary>加载游戏</summary>
        public void LoadGame(string saveName)
        {
            GameManager.Instance.LoadGame(saveName);
        }

        private void Update()
        {
            // F5快速保存
            if (Input.GetKeyDown(KeyCode.F5))
            {
                SaveGame("quicksave");
                Debug.Log("[Bootstrap] 快速保存完成");
            }
            // F9快速加载
            if (Input.GetKeyDown(KeyCode.F9))
            {
                LoadGame("quicksave");
                Debug.Log("[Bootstrap] 快速加载完成");
            }
        }
    }
}
