using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Generation;
using MyriadCreation.App;
using MyriadCreation.App.Contracts;

namespace MyriadCreation.UI
{
 /// 场景链路控制器——统一管理主菜单→世界生成→游戏→返回主菜单的完整链路。
 /// B3-part2b：不再直接引用 UIManager/MainMenuUI/Bootstrap，全部通过 App.Contracts（AppServices）反转调用；
 /// 阶段 2 将本文件迁入 MyriadCreation.App，依赖方向变为 UI→App 单向。
    public class SceneFlowController : MonoBehaviour, IAppFlow
    {
        public static SceneFlowController Instance { get; private set; }

        [Header("链路设置")]
        [SerializeField] private bool useCodeGeneratedMainMenu = true; // 用代码动态主菜单替代场景旧主菜单
        [SerializeField] private float minLoadingDisplayTime = 0.5f; // loading 面板最短显示时间（避免闪烁）

        public enum FlowState { MainMenu, WorldGeneration, Playing, Editor }
        public FlowState CurrentState { get; private set; } = FlowState.MainMenu;

        /// <summary>IAppFlow 契约：与内部 FlowState 一一对应（数字值保持一致）。</summary>
        AppFlowState IAppFlow.State => (AppFlowState)(int)CurrentState;

        public event Action<AppFlowState> StateChanged;
        public event Action<bool> LoadingChanged;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            AppServices.RegisterAppFlow(this);
        }

        void OnDestroy()
        {
            AppServices.UnregisterAppFlow(this);
        }

        void Start()
        {
            HideLegacyUi();

            // EventSystem 是全部 uGUI 点击的前提：场景旧 UI 被清理时若连 EventSystem 一起删掉，
            // 动态主菜单会"看得见点不动"，这里兜底补一个（旧 Input Manager + StandaloneInputModule）。
            EnsureEventSystem();

 // 初始化主菜单（通过 UI 工厂创建，App 不直接依赖 UI 类型）
            if (useCodeGeneratedMainMenu)
            {
                var factory = AppServices.UiFactory;
                if (factory != null)
                {
                    var menu = factory.CreateMainMenuUi(transform);
                    if (menu != null)
                    {
                        menu.Build();
                        menu.Show();
                        AppServices.RegisterMainMenuUi(menu);
                    }
                }
                else
                {
                    Debug.LogWarning("[SceneFlow] UiFactory 尚未注册（Bootstrap 未初始化？），主菜单未创建");
                }
                AppServices.UiHost?.HideGameUI();
                Debug.Log("[SceneFlow] 代码动态主菜单已接管，游戏内UI已隐藏");
            }
            CurrentState = FlowState.MainMenu;
            StateChanged?.Invoke((AppFlowState)(int)CurrentState);
        }

 /// <summary>从主菜单进入世界生成（开始新游戏）</summary>
        public void EnterWorldGeneration()
        {
            CurrentState = FlowState.WorldGeneration;
            var menu = AppServices.MainMenuUi;
            if (menu != null && menu.IsVisible) menu.Hide();
            LoadingChanged?.Invoke(true);
            StateChanged?.Invoke((AppFlowState)(int)CurrentState);
            Debug.Log("[SceneFlow] 进入世界生成");
        }

 /// <summary>世界生成完成，进入游戏</summary>
        public void EnterPlaying()
        {
            CurrentState = FlowState.Playing;
            LoadingChanged?.Invoke(false);
            StateChanged?.Invoke((AppFlowState)(int)CurrentState);
            Debug.Log("[SceneFlow] 进入游戏");
        }

 /// <summary>进入地图编辑器（从主菜单点开始游戏——全海空白地图+右侧编辑器UI+右下速度控制）</summary>
        public void EnterMapEditor()
        {
            CurrentState = FlowState.Editor;
            var menu = AppServices.MainMenuUi;
            if (menu != null && menu.IsVisible) menu.Hide();
 // 进入地图编辑器时显示游戏内UI
            AppServices.UiHost?.ShowGameUI();
            StateChanged?.Invoke((AppFlowState)(int)CurrentState);

            Debug.Log("[SceneFlow] 进入地图编辑器");
        }

 /// <summary>进入地图编辑器并加载指定存档</summary>
        public void EnterMapEditorWithSave(string saveFileName)
        {
            CurrentState = FlowState.Editor;
            var menu = AppServices.MainMenuUi;
            if (menu != null && menu.IsVisible) menu.Hide();
            AppServices.UiHost?.ShowGameUI();
            StateChanged?.Invoke((AppFlowState)(int)CurrentState);

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
            var menu = AppServices.MainMenuUi;
            if (menu != null) menu.Show();
 // 返回主菜单时隐藏游戏内UI
            AppServices.UiHost?.HideGameUI();
            LoadingChanged?.Invoke(false);
            StateChanged?.Invoke((AppFlowState)(int)CurrentState);
            Debug.Log("[SceneFlow] 返回主菜单");
        }

 // ===== IAppFlow：新游戏/读档入口（契约层只依赖 Core DTO；concrete WorldSetupOptions 仅兼容 UI） =====
        public void StartNewGame()
        {
            EnterWorldGeneration();
            GenerateWorldWithLoading((NewGameRequestDto)null, null);
        }

        public void StartNewGame(NewGameRequestDto request)
        {
            EnterWorldGeneration();
            GenerateWorldWithLoading(request, null);
        }

        // concrete 兼容：UI/旧调用方继续使用 WorldSetupOptions。
        public void StartNewGame(WorldSetupOptions setupOptions)
        {
            StartNewGame(NewGameRequestMapper.FromWorldSetup(setupOptions));
        }

        public void LoadGame(string saveName)
        {
            var session = AppServices.GameSession;
            if (session != null) session.LoadGame(saveName);
        }

 /// <summary>带 loading 面板的世界生成（解决同步阻塞导致 loading 来不及显示的问题）</summary>
        public void GenerateWorldWithLoading(Action onComplete)
        {
            GenerateWorldWithLoading((NewGameRequestDto)null, onComplete);
        }

        public void GenerateWorldWithLoading(NewGameRequestDto request, Action onComplete = null)
        {
            StartCoroutine(GenerateWorldCoroutine(onComplete, request));
        }

 /// <summary>concrete 兼容：UI 继续使用 WorldSetupOptions，内部转 Core DTO。</summary>
        public void GenerateWorldWithLoading(WorldSetupOptions setupOptions, Action onComplete = null)
        {
            GenerateWorldWithLoading(NewGameRequestMapper.FromWorldSetup(setupOptions), onComplete);
        }

        private IEnumerator GenerateWorldCoroutine(Action onComplete, NewGameRequestDto request)
        {
            AppServices.UiHost?.ShowLoading("世界生成中…");

 // 等待一帧让 loading 面板渲染出来
            yield return null;

            float t0 = Time.realtimeSinceStartup;

            var session = AppServices.GameSession;
            if (session != null)
            {
                bool done = false;
                Action markDone = () => done = true;
                if (request != null)
                    session.StartNewGameAsync(request, markDone);
                else
                    session.StartNewGameAsync(markDone);

                while (!done)
                {
                    // 场景卸载/异常导致会话协程提前结束时不空转（本对象 DDOL 会跨场景保活）。
                    var current = AppServices.GameSession;
                    if (current == null || !current.HasWorld) yield break;
                    yield return null;
                }
            }
            else
            {
                Debug.LogError("[SceneFlow] GameSession 缺失（GameManager 未注册），无法生成世界");
                onComplete?.Invoke();
                yield break;
            }

 // 确保 loading 面板最短显示时间
            float elapsed = Time.realtimeSinceStartup - t0;
            if (elapsed < minLoadingDisplayTime)
                yield return new WaitForSecondsRealtime(minLoadingDisplayTime - elapsed);

            AppServices.UiHost?.HideLoading();
            EnterPlaying();
            onComplete?.Invoke();
        }

        /// <summary>
        /// 场景里没有 EventSystem 时补一个（旧 Input Manager + StandaloneInputModule）。
        /// 动态主菜单完全依赖 EventSystem 才能接收点击；漏建时表现为"UI 看得见但点不动"。
        /// </summary>
        private static void EnsureEventSystem()
        {
            // 含 inactive：场景里如果保留了被禁用的 EventSystem，优先启用它，避免出现两个 EventSystem。
            var existing = UnityEngine.Object.FindObjectsByType<EventSystem>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (existing != null && existing.Length > 0)
            {
                if (!existing[0].gameObject.activeInHierarchy) existing[0].gameObject.SetActive(true);
                if (existing[0].gameObject.activeInHierarchy) return;
            }

            var obj = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            UnityEngine.Object.DontDestroyOnLoad(obj);
            Debug.LogWarning("[SceneFlow] 场景缺少 EventSystem —— 已自动创建（StandaloneInputModule），否则主菜单无法点击");
        }

        /// <summary>禁用场景中残留的旧菜单/旧地图模式栏，避免新UI隐藏后露出死 UI。</summary>
        private void HideLegacyUi()
        {
            // 使用代码动态主菜单时才需要压掉场景旧 UI；若退回场景旧菜单（useCodeGeneratedMainMenu=false）
            // 则不能隐藏，否则开始后没有任何菜单。
            if (!useCodeGeneratedMainMenu) return;

            // 场景旧对象可能已被删除（HEAD 恢复版清理）：Find 返回 null 直接跳过，不报错。
            string[] legacyNames = { "StartMenuPanel", "NewGamePanel", "SettingsPanel", "MapModeBar" };
            int hidden = 0;
            foreach (string name in legacyNames)
            {
                var go = GameObject.Find(name);
                if (go == null || !go.activeSelf) continue;
                go.SetActive(false);
                hidden++;
            }
            if (hidden > 0)
                Debug.Log($"[SceneFlow] 已隐藏 {hidden} 个场景旧主界面对象（旧 UI 仅占位，交互由代码动态主菜单接管）");
        }

        void Update()
        {
 // Esc 返回主菜单（游戏中/编辑器内）
            if (Input.GetKeyDown(KeyCode.Escape)
                && (CurrentState == FlowState.Playing || CurrentState == FlowState.Editor))
            {
                // 模态面板（政府/聚落详情等）自己处理 ESC 关闭时，不要再触发"返回主菜单"（ESC 栈：先关面板）。
                var ui = AppServices.UiHost;
                bool modalOpen = ui != null && (ui.IsGovernmentPanelOpen || ui.IsSettlementDetailOpen);
                if (!modalOpen)
                {
                    ReturnToMainMenu();
                }
            }
        }
    }
}
