using System;
using System.Collections;
using UnityEngine;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.UI
{
 /// 场景链路控制器——统一管理主菜单→世界生成→游戏→返回主菜单的完整链路。
 /// 解决：主菜单显示无统一管理、世界生成同步阻塞导致 loadingPanel 来不及显示、
 /// 返回主菜单未对接等问题。
    public class SceneFlowController : MonoBehaviour
    {
        public static SceneFlowController Instance { get; private set; }

        [Header("引用")]
        [SerializeField] private CivilizationEvolution.Core.Bootstrap bootstrap;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private MainMenuUI mainMenu;

        [Header("链路设置")]
        [SerializeField] private bool useCodeGeneratedMainMenu = true; // 用代码动态主菜单替代场景旧主菜单
        [SerializeField] private float minLoadingDisplayTime = 0.5f; // loading 面板最短显示时间（避免闪烁）

        public enum FlowState { MainMenu, WorldGeneration, Playing, Editor }
        public FlowState CurrentState { get; private set; } = FlowState.MainMenu;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            if (bootstrap == null) bootstrap = FindAnyObjectByType<CivilizationEvolution.Core.Bootstrap>();
            if (uiManager == null) uiManager = FindAnyObjectByType<UIManager>();

 // 初始化主菜单
            if (useCodeGeneratedMainMenu)
            {
                if (mainMenu == null)
                {
                    var obj = new GameObject("MainMenuUI");
                    mainMenu = obj.AddComponent<MainMenuUI>();
                    obj.transform.SetParent(transform);
                }
                mainMenu.Build();
                mainMenu.Show();
 // 主菜单状态下隐藏游戏内UI
                if (uiManager != null) uiManager.HideGameUI();
                Debug.Log("[SceneFlow] 代码动态主菜单已接管，游戏内UI已隐藏");
            }
            CurrentState = FlowState.MainMenu;
        }

 /// <summary>从主菜单进入世界生成（开始新游戏）</summary>
        public void EnterWorldGeneration()
        {
            CurrentState = FlowState.WorldGeneration;
            if (mainMenu != null && mainMenu.IsVisible) mainMenu.Hide();
            Debug.Log("[SceneFlow] 进入世界生成");
        }

 /// <summary>世界生成完成，进入游戏</summary>
        public void EnterPlaying()
        {
            CurrentState = FlowState.Playing;
            Debug.Log("[SceneFlow] 进入游戏");
        }

 /// <summary>进入地图编辑器（从主菜单点开始游戏——全海空白地图+右侧编辑器UI+右下速度控制）</summary>
        public void EnterMapEditor()
        {
            CurrentState = FlowState.Editor;
            if (mainMenu != null && mainMenu.IsVisible) mainMenu.Hide();
 // 进入地图编辑器时显示游戏内UI
            if (uiManager != null) uiManager.ShowGameUI();

            #if UNITY_EDITOR
            if (FindAnyObjectByType<CivilizationEvolution.Bootstrap.MapEditorBootstrap>() == null)
            {
                var editorObj = new GameObject("MapEditorBootstrap");
                var bootstrap = editorObj.AddComponent<CivilizationEvolution.Bootstrap.MapEditorBootstrap>();
                bootstrap.startWithEmptyOcean = true;
                Debug.Log("[SceneFlow] MapEditorBootstrap 已启动（全海空白地图模式）");
            }
            #endif
            Debug.Log("[SceneFlow] 进入地图编辑器");
        }

 /// <summary>进入地图编辑器并加载指定存档</summary>
        public void EnterMapEditorWithSave(string saveFileName)
        {
            CurrentState = FlowState.Editor;
            if (mainMenu != null && mainMenu.IsVisible) mainMenu.Hide();
            if (uiManager != null) uiManager.ShowGameUI();

            #if UNITY_EDITOR
            if (FindAnyObjectByType<CivilizationEvolution.Bootstrap.MapEditorBootstrap>() == null)
            {
                var editorObj = new GameObject("MapEditorBootstrap");
                var bootstrap = editorObj.AddComponent<CivilizationEvolution.Bootstrap.MapEditorBootstrap>();
                bootstrap.loadSaveFileName = saveFileName;
                Debug.Log($"[SceneFlow] MapEditorBootstrap 已启动（加载存档模式: {saveFileName}）");
            }
            #endif
            Debug.Log($"[SceneFlow] 进入地图编辑器（加载存档: {saveFileName}）");
        }

 /// <summary>进入地图编辑器（旧入口，保留兼容）</summary>
        public void EnterEditor()
        {
            EnterMapEditor();
        }

 /// <summary>返回主菜单（游戏中按 Esc 或点返回按钮）</summary>
        public void ReturnToMainMenu()
        {
            CurrentState = FlowState.MainMenu;
            if (mainMenu != null) mainMenu.Show();
 // 返回主菜单时隐藏游戏内UI
            if (uiManager != null) uiManager.HideGameUI();
 // 隐藏游戏 UI（如果需要）
            Debug.Log("[SceneFlow] 返回主菜单");
        }

 /// <summary>带 loading 面板的世界生成（解决同步阻塞导致 loading 来不及显示的问题）</summary>
        public void GenerateWorldWithLoading(Action onComplete)
        {
            StartCoroutine(GenerateWorldCoroutine(onComplete));
        }

        private IEnumerator GenerateWorldCoroutine(Action onComplete)
        {
 // 等待一帧让 loading 面板渲染出来
            yield return null;

            float t0 = Time.realtimeSinceStartup;

 // 同步生成世界（阻塞）
            onComplete?.Invoke();

 // 确保 loading 面板最短显示时间
            float elapsed = Time.realtimeSinceStartup - t0;
            if (elapsed < minLoadingDisplayTime)
                yield return new WaitForSecondsRealtime(minLoadingDisplayTime - elapsed);

            EnterPlaying();
        }

        void Update()
        {
 // Esc 返回主菜单（游戏中）
            if (Input.GetKeyDown(KeyCode.Escape) && CurrentState == FlowState.Playing)
            {
                ReturnToMainMenu();
            }
        }
    }
}
