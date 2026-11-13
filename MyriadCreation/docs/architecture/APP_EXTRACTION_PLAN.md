# App 抽取方案（APP_EXTRACTION_PLAN）

> 卡片：**task-73（B3-part2a：设置服务抽到 MyriadCreation.App + App 抽取审计方案）**
> 状态：**A 部分已执行**（设置服务已迁入 `MyriadCreation.App`）；**B 部分为本文件（只审计+方案，不改 Bootstrap/GameManager/SceneFlowController 本体）**。
> 快照：2026-11-01 14:0x（+08:00）。基线文档：`docs/architecture/SYSTEM_OWNERSHIP.md`（task-64）。
> 约束：本卡未跑 Unity/编译/测试；法律/信仰/文化由并行任务处理，本方案不触碰其文件。

---

## 0. 结论摘要

1. **A 已完成**：`SettingsModel/SettingsService` 从 `Assets/UI/Settings/` 移到 `Assets/Scripts/App/MyriadCreation.App/Settings/`，新建 `MyriadCreation.App` asmdef（refs = Core / Infrastructure / UnityEngine.UI，**不引 UI**）；namespace 改为 `MyriadCreation.App.Settings`；UI 5 个源文件 + 2 个测试文件 using 已更新；UI/Tests asmdef 已加 `MyriadCreation.App` 引用。App 内 `using MyriadCreation.UI` = 0，旧 namespace 引用全仓 = 0。
2. **当前 App 三件套仍在 UI 程序集**：`Bootstrap`(203 行)/`GameManager`(354 行)/`SceneFlowController`(252 行) 在 `Assets/UI/MyriadCreation.UI/`、namespace `MyriadCreation.UI`，是"App 逻辑放在 UI 程序集"的根因；它们同时被 UI 面板与测试直接引用（清单见 §2/§4）。
3. **抽取的硬约束是"程序集依赖不能成环"**：App 最终必须引用 UI（SceneFlow 创建 `MainMenuUI`、GameManager 操作 `UIManager`/`SettlementDetailPanel`），所以 **UI 不能引用 App 具体类型**。当前 UI→App 的引用（Bootstrap/SceneFlowController/GameManager 单例 + App.Settings）必须替换成 **App.Contracts 接口/服务定位器**；否则 App 一旦加 UI 引用即循环。
4. **推荐中间程序集 `MyriadCreation.App.Contracts`**（refs = Core/World/Simulation，不引 UI/App）：放 `IAppFlow`/`IGameSession`/`ISettingsService`/`AppServices` 定位器；UI 只引用 Contracts，App 引用 Contracts + UI。这样断裂环，且 `IAppFlow`/`IGameSession` 可以同时服务 UI 与测试。
5. **GameWorld 纯化切割线**（task-70 已拆 partial，本方案在 partial 之上再切）：`WorldState` 只保留数据字段 + `Queries`；`Core` 的组合根/初始化、`Generation` 的生成编排、`Persistence` 的读档桥接、`Editing` 的编辑执行、`Simulation` 的 tick/调度注册分别迁到 App 的 `WorldCompositionRoot`/`WorldFactory`/`SessionPersistence`/`WorldEditService`/`SimulationRunner`；`Succession` 属模拟逻辑，迁 `Simulation.Succession`（不是 App）。
6. **分步顺序**（考虑循环后的唯一可行顺序）：`App.Contracts` → 迁 `GameManager` → 迁 `SceneFlowController` → 迁 `Bootstrap` → 迁 `RuntimeMapEditingController` → GameWorld 纯化收尾。每一步独立编译/测试门禁与回滚点（§6）。卡片原顺序"先契约→移 GameManager/SceneFlow→移 Bootstrap→最后编辑执行器"与此一致，**但 Bootstrap 必须在 GameManager/SceneFlow 迁走后才能迁**（它 new 这两个类型；迁移顺序上把 Bootstrap 与 GameManager 同批/紧跟，见 §6 备注）。

---

## 1. 当前程序集图（A 之后）

> 注：并行卡（Content/Faith/Culture 抽取等）仍在移动文件；下表计数为 2026-11-01 14:0x 快照，后续以 `find` 重扫为准。`MyriadCreation.Content`（95 个 .cs）已由并行卡建立，不在本卡范围。

| 程序集 | 路径 | 直接 references | 文件数 |
|---|---|---|---|
| `MyriadCreation.App` | `Assets/Scripts/App/MyriadCreation.App.asmdef` | Core, Infrastructure, `UnityEngine.UI` | 2（Settings） |
| `MyriadCreation.UI` | `Assets/UI/MyriadCreation.UI.asmdef` | Core, Simulation, Rendering, UnityEngine.UI, Unity.TextMeshPro, Infrastructure, World, **Content**, **App** | 30（A 后少 2） |
| `MyriadCreation.Infrastructure` | `Assets/Scripts/Infrastructure/...` | Core, World, Simulation | 23 |
| `MyriadCreation.Simulation` | `Assets/Scripts/Simulation/sim.asmdef` | Core, World | 474（并行 Content 抽取后） |
| `MyriadCreation.Content` | `Assets/Scripts/Content/...` | Core（+内容定义） | 95 |
| `MyriadCreation.World` | `Assets/Scripts/World/world.asmdef` | Core | 90 |
| `MyriadCreation.Rendering` | `Assets/Scripts/Rendering/...` | Core, World, Simulation | 11 |
| Tests / Editor | `Assets/Tests/**`、`Assets/Editor/...` | 全部（含 App） | 89 / 7 |

**目标图（B3-part2 完成后）**：

```
Core (纯契约/DTO)  ←  World / Content  ←  Simulation / Infrastructure / Rendering
        ↑
  App.Contracts（接口/定位器；不引 UI/App）
        ↑                                ↑
       UI（只定义/交互） ——→ App（组合根/场景流/设置/编辑执行器；引用 UI+Contracts）
        ↑
   Tests / Editor
```

- **UI → App 只允许经过 `App.Contracts`**（`IAppFlow`/`IGameSession`/`ISettingsService` + `AppServices`）；UI 不 `using MyriadCreation.App`。
- **App 可以引用 UI**（创建/绑定主菜单、UIManager、聚落详情面板、编辑执行器），因此 UI 与 App 不能互相直接引用。
- `MyriadCreation.App.Contracts` 依赖 Simulation/World 是因为 `IGameSession.World` 目前返回 `GameWorld`；后续若把 World 表面收窄为 DTO/查询接口，可把 Contracts 降到只依赖 Core。

---

## 2. 三个 App 类现状审计

### 2.1 `Bootstrap.cs`（203 行，`Assets/UI/MyriadCreation.UI/Bootstrap.cs:30`；namespace `MyriadCreation.UI`）

| 职责 | 行号 |
|---|---|
| 场景入口（场景 `Main.unity` 上的 MonoBehaviour 组件） | 30-46 |
| `Awake`：应用设置（`SettingsService.ApplySaved`） | 48-64（L52） |
| 地图尺寸预设 → `_mapWidth/_mapHeight`、存档 default map size 读取 | 55-64 |
| 内容/本地化初始化：`ContentRegistry.Initialize()`、`Localization.Initialize(...)` | 69-73 |
| 确保 `GameManager` 存在（代码 new + `AddComponent<GameManager>`） | 75-80 |
| 相机/灯光场景设置 | 82-101 |
| 创建 `SceneFlowController`（代码 new） | 106-110 |
| `Start`：按 `autoStartOnBoot/startViaMenu` 决定是否自动开局/等待主菜单 | 113-125 |
| 开局包装：`StartNewGame`/`StartNewGameAsync`/`StartNewGameAsyncWithOptions`/`ConfigureAndStartNewGame` → `GameManager.Instance.*` | 128-174 |
| 存档包装：`SaveGame`/`LoadGame` → `GameManager.Instance.*` | 176-185 |
| F5/F9 快速存读档（`Input`） | 187-201 |

**外部引用（谁引用 Bootstrap）**
- `SceneFlowController.cs:36`（`[SerializeField] private Bootstrap bootstrap`）、`:57`（`FindAnyObjectByType<Bootstrap>`）、`:156-174`（生成协程调用 `bootstrap.StartNewGameAsync*`）、`:232`（失效重查）。
- `MainMenuUI.cs:210,329,382`（`FindAnyObjectByType<Bootstrap>` + 错误提示）；`:213,332,385` 注释/错误日志。
- 测试：`MapVisibilityPlayModeTests.cs:61`（`FindAnyObjectByType<Bootstrap>`）。
- 场景：`Main.unity` 上有 Bootstrap 组件（序列化引用其 Camera/Light 等）；脚本资产 GUID 来自 `Bootstrap.cs.meta`。
- 其它：`UIManager.Panels.cs:434` 注释、`SettingsModel.cs:375` 注释（无代码依赖）。

**Bootstrap 内对外部系统的引用（迁 App 后同程序集依赖）**：GameManager、SceneFlowController、SettingsService（已 App）、ContentRegistry、Localization、GameConstants、WorldConfig/MapSizePreset、相机/灯光；这些都在 App 目标依赖范围内。

### 2.2 `GameManager.cs`（354 行，`Assets/UI/MyriadCreation.UI/GameManager.cs:27`；namespace `MyriadCreation.UI`）

| 职责 | 行号 |
|---|---|
| 单例 + `DontDestroyOnLoad` + `Awake`/`Start` | 29,63-77 |
| 状态机 `GameState`（MainMenu/Loading/Playing/Paused/MapEditor）+ `ChangeState`/`Time.timeScale` | 40,49,86-107 |
| 世界生命周期：`StartNewGame`/`StartNewGameAsync`/`GenerateWorldAsync`（`world.InitializeWorld()`/`GenerateTerrain`/`GenerateTerrainAsync`） | 110-206 |
| 存档/读档：`SaveGame`/`LoadGame`（调用 `SaveSystem`、把旧世界销毁/重绑 `MapRenderer`/`UIManager`/`RuntimeMapEditingController`/`SettlementIconLayer`/`SettlementDetailPanel`） | 209-247 |
| 速度/暂停：`SetGameSpeed`/`TogglePause` | 250-265 |
| 返回主菜单：销毁世界、解绑图标、藏详情面板 | 268-279 |
| 聚落图标→详情面板接线：`RebindSettlementIcons`/`OnSettlementIconClicked`/`EnsureSettlementDetailPanel` | 281-342 |
| 空格暂停（`Update`） | 344-352 |
| 事件：`OnGameStateChanged/OnGameSpeedChanged/OnNewGameStarted/OnGameLoaded/OnGameSaved` | 53-57 |

**外部引用（GameManager 被谁用）**
- UI：`UIManager.cs:260,369,390-391`（`CurrentState`/`SetGameSpeed`）、`UIManager.Panels.cs:862`（`CurrentState`）、`MainMenuUI.cs:392`（读档后校验 `World/CurrentState`）、`SettlementDetailPanel.cs:202`（`GameManager.Instance.World` 兜底）。
- App（当前 UI 程序集内）：`Bootstrap.cs:76-80,130-150,178-184`（创建/转发）、`SceneFlowController.cs:168`（`GameManager.Instance.LoadGame`? 见 flow 生成/读档路径）。
- 测试：`WorldRebindOnLoadTests.cs:85-97,109,125`（直接 new `GameManager`、`LoadGame`、读 `World`）；PlayMode：`MainMenuUiPlayModeTests.cs:32`、`GovernmentPanelPlayModeTests.cs:38,73,141`、`SettlementIconLayerPlayModeTests.cs:49`、`MapVisibilityPlayModeTests.cs`（间接）。
- 场景：GameManager 是代码 new 的 DDOL 对象，无场景序列化引用。

**GameManager 内对外部系统的引用**：UIManager、SettlementDetailPanel、MapRenderer、RuntimeMapEditingController、SettlementIconLayer（Rendering）、SaveSystem（Infrastructure）、GameWorld/Simulation 生成（Simulation），以及 `GameState`（Core.Enums）。

### 2.3 `SceneFlowController.cs`（252 行，`Assets/UI/MyriadCreation.UI/SceneFlowController.cs:31`；namespace `MyriadCreation.UI`）

| 职责 | 行号 |
|---|---|
| 单例 + DDOL + `Awake` | 33,47-52 |
| `Start`：隐藏旧 UI、确保 `Bootstrap`/`UIManager`、确保 `EventSystem`、代码创建 `MainMenuUI` 并 `Build/Show` | 54-80 |
| 流程状态 `FlowState`（MainMenu/WorldGeneration/Playing/Editor） | 44-45 |
| 流程切换：`EnterWorldGeneration`/`EnterPlaying`/`EnterMapEditor`/`EnterEditor`/`EnterMapEditorWithSave`/`ReturnToMainMenu` | 83-133 |
| 世界生成协程 `GenerateWorldWithLoading`（loading UI + GameManager 生成） | 136-185 |
| 旧 UI 清理 `HideLegacyUi` | 208-226 |
| ESC：Playing/Editor → 返回主菜单（**与 GovernmentPanel ESC 双处理的风险点**，见 §7） | 236-247 |
| 其它：`EnsureEventSystem` | 约 187-206 |

**外部引用（SceneFlowController 被谁用）**
- UI：`MainMenuUI.cs:217,336,396`（`SceneFlowController.Instance` + `EnterWorldGeneration/GenerateWorldWithLoading/EnterMapEditorWithSave`）。
- App（当前 UI 程序集内）：`Bootstrap.cs:106-110`（创建）、`:156-174`（生成协程调用）；测试：`MainMenuUiPlayModeTests.cs:28,49-50`、`MapVisibilityPlayModeTests.cs:45`、`SettlementIconLayerPlayModeTests.cs:34`、`GovernmentPanelPlayModeTests.cs:36`。
- 场景：代码 new（DDOL），无场景序列化引用。

**SceneFlowController 内对外部系统的引用**：Bootstrap、UIManager、MainMenuUI、GameManager、SettlementDetailPanel、Math/WorldSetupOptions（World.Generation）、SaveSystem/ContentRegistry 等。**App→UI 依赖就在这里**（`MainMenuUI`/`UIManager`），是"不能 UI→App"的根本原因。

### 2.4 交叉引用矩阵（App 三件套 × 外部）

| 被引用方 → | Bootstrap | GameManager | SceneFlowController | UIManager | MainMenuUI | SettlementDetailPanel |
|---|---|---|---|---|---|---|
| **Bootstrap** | - | 创建/转发 (76-80,130-150) | 创建 (106-110) | 间接 | - | - |
| **GameManager** | - | - | 读档调用 (168) | 直接 (82,161,168) | - | 创建/绑定 (61,308,331-341) |
| **SceneFlowController** | 引用/重查 (36,57,156-174,232) | 通过生成调用 | - | 直接 (37,58,76,103,149,241) | 创建/Build/Show/Hide (36-38,67-77,86,101,128,149,183) | ESC 关闭 (236-247) |
| **MainMenuUI（UI）** | Find (210/329/382) | 读档校验 (392) | 单例 (217/336/396) | - | - | - |
| **UIManager（UI）** | - | Instance (260/369/390-391/862) | - | - | - | - |

---

## 3. 需要先定义的 App 契约（recommended: 新程序集 `MyriadCreation.App.Contracts`）

### 3.1 程序集位置与依赖

```
Assets/Scripts/App.Contracts/MyriadCreation.App.Contracts.asmdef
  name: MyriadCreation.App.Contracts
  references: MyriadCreation.Core, MyriadCreation.World, MyriadCreation.Simulation
  （不引用 MyriadCreation.UI / MyriadCreation.App）
```

- UI 引用 `MyriadCreation.App.Contracts`（UI 已有 Core/World/Simulation 引用的基础上新增一个 Assembly 引用）。
- App 引用 `MyriadCreation.App.Contracts` + `MyriadCreation.UI`。
- Contracts 里可以出现 `GameWorld`/`WorldSetupOptions`/`MapWrapMode` 等类型（因此需要 World/Simulation 引用）；若后续把 `IGameSession.World` 改成 `IWorldQuery`/DTO，可把 Contracts 降到只依赖 Core。

### 3.2 接口草案

```csharp
namespace MyriadCreation.App.Contracts
{
    public enum AppFlowState { MainMenu, WorldGeneration, Playing, Editor }

    public sealed class AppNewGameRequest
    {
        public int mapWidth = 128;
        public int mapHeight = 64;
        public int seed = 42;
        public MapWrapMode wrapMode = MapWrapMode.Cylindrical;
        public WorldSetupOptions setupOptions; // World.Generation.WorldSetupOptions
    }

    /// 场景/游戏流程入口：UI 只通过该接口发起流程，不接触 Bootstrap/SceneFlowController 具体类。
    public interface IAppFlow
    {
        AppFlowState State { get; }
        event System.Action<AppFlowState> StateChanged;
        event System.Action<float, string> LoadingProgressChanged; // progress, message
        event System.Action<bool, string> WorldGenerationCompleted; // success, message

        void RequestStartNewGame(AppNewGameRequest request);
        void RequestLoadGame(string saveName);
        void RequestReturnToMainMenu();
        void RequestEnterMapEditor(string saveName = null);
    }

    /// 当前游戏会话（世界/速度/存档）：UI 只读 World，命令走方法；具体实现由 App 注入。
    public interface IGameSession
    {
        bool HasWorld { get; }
        GameWorld World { get; }              // Simulation.WorldState.GameWorld
        bool IsPlaying { get; }
        float GameSpeed { get; }
        event System.Action<GameWorld> WorldChanged;
        event System.Action<float> GameSpeedChanged;

        void RegisterWorld(GameWorld world, bool showGameUi);
        bool SaveGame(string saveName);
        bool LoadGame(string saveName);
        void SetGameSpeed(float speed);
        void TogglePause();
        void ReturnToMainMenu();
    }

    /// 设置服务只读/应用接口；SettingsModel 最终下沉 Infrastructure（见 §5 步骤 2）。
    public interface ISettingsService
    {
        SettingsModel Current { get; }
        void ApplySaved();
        void Apply(SettingsModel model);
        SettingsModel ResetToDefaults();
    }

    /// 轻量服务定位器：App 启动时注册，UI 在 Start 时取；不再使用 GameManager.Instance/SceneFlowController.Instance。
    public static class AppServices
    {
        public static IAppFlow Flow { get; private set; }
        public static IGameSession Session { get; private set; }
        public static ISettingsService Settings { get; private set; }
        public static void Register(IAppFlow flow, IGameSession session, ISettingsService settings);
        public static void Reset();
    }
}
```

### 3.3 实现适配层（App 内，先包住现有类，行为不变）

- `AppFlowService : IAppFlow`：内部持 `SceneFlowController`/`Bootstrap`（迁移期）或直接实现（迁移后）；把现有 API 映射为 `Request*` + 事件。
- `GameSessionService : IGameSession`：内部包 `GameManager`（迁移期）；`World` 直接转发；`SaveGame/LoadGame/SetGameSpeed/TogglePause` 转发；`WorldChanged` 在 `StartNewGame/LoadGame/ReturnToMainMenu` 后触发（替代 UI 轮询 `GameManager.CurrentState`）。
- `SettingsServiceAdapter : ISettingsService`：包 `SettingsService` 静态类（当前）→ 步骤 2 改为 Infrastructure 实现。
- App 启动（`Bootstrap.Awake` 最终迁 App 后）调用 `AppServices.Register(...)`；UI 的 `UIManager.Start`/`MainMenuUI.Start` 若取不到服务则降级（主菜单 UI 独立测试场景/PlayMode 测试）——**空安全**很重要，测试可能直接 new UI 面板。

### 3.4 UI 侧改造规则（替换反向依赖）

| UI 现在 | 改为 |
|---|---|
| `FindAnyObjectByType<Bootstrap>()` + `bootstrap.StartNewGameAsyncWithOptions(...)` | `AppServices.Flow.RequestStartNewGame(request)`（Bootstrap 移 App 后 UI 不再看见它） |
| `SceneFlowController.Instance.EnterWorldGeneration()/GenerateWorldWithLoading(...)` | `AppServices.Flow.RequestStartNewGame(request)`（loading 由 Flow 事件驱动 UIManager） |
| `bootstrap.LoadGame(saveName)` + `GameManager.Instance.World/CurrentState` 轮询 | `AppServices.Session.LoadGame(saveName)` + `Session.WorldChanged` 事件 |
| `GameManager.Instance.CurrentState`（隐藏按钮/屏蔽点击） | `AppServices.Flow.State`（或 `Session.IsPlaying`） |
| `GameManager.Instance.SetGameSpeed(...)` | `AppServices.Session.SetGameSpeed(...)` |
| `GameManager.Instance.World`（Settlement 面板兜底） | `AppServices.Session.World`（已有 `BindWorld` 由 `WorldChanged` 推送） |
| `using MyriadCreation.App.Settings`（5 个 UI 文件） | 步骤 2 起改为 `ISettingsService`（Contracts）+ `SettingsModel`（Infrastructure/Core DTO），UI 不再引用 App |

---

## 4. UI → App 反向依赖清单（当前，A 之后）

> "反向"指 UI 程序集直接依赖 App 具体类/静态单例（Bootstrap/GameManager/SceneFlowController）；这些在当前 `MyriadCreation.UI` 里能编译，是因为 App 三件套还在 UI 程序集里；一旦它们迁入 `MyriadCreation.App` 且 App 引用 UI，就会形成 asmdef 环。

| # | UI 文件 | 具体依赖（行号） | 迁移改法 |
|---|---|---|---|
| U1 | `Assets/UI/Panels/MainMenuUI.cs` | `Bootstrap` Find (210,329,382)；`SceneFlowController.Instance` (217,336,396)；`GameManager` (392) | 改 `AppServices.Flow/Session`（§3.4） |
| U2 | `Assets/UI/Common/UIManager.cs` | `GameManager.Instance` (260,369,390-391) | 改 `AppServices.Flow.State` / `Session.SetGameSpeed`；HUD 显示/隐藏改 `UIManager.ShowGameUI` 由 App 事件驱动 |
| U3 | `Assets/UI/Common/UIManager.Panels.cs` | `GameManager.Instance.CurrentState` (862) | 改 `AppServices.Flow.State` |
| U4 | `Assets/UI/Panels/SettlementDetailPanel.cs` | `GameManager.Instance.World` (202) | 改 `AppServices.Session.World`（或只用 `BindWorld` 注入，删除 GameManager 兜底） |
| U5 | `Assets/UI/Common/UiScaleBinder.cs` | `using MyriadCreation.App.Settings;` (3) | 步骤 2：改 `ISettingsService`/`SettingsModel` 下沉 Infrastructure |
| U6 | `Assets/UI/Panels/SettingsPanel.cs` | `using MyriadCreation.App.Settings;` (8) | 同上 |
| U7 | `Assets/UI/MyriadCreation.UI/Bootstrap.cs` | `using MyriadCreation.App.Settings;` (18) + 本身是 App 组件 | 随 Bootstrap 迁 App（U7 自动消失） |
| U8 | 测试：`WorldRebindOnLoadTests.cs` (85-97,109,125) | `GameManager` 具体类/`LoadGame`/`World` | 步骤 2 后测试引用 `MyriadCreation.App`（Tests asmdef 已允许，直接 new GameManager 仍可用；或改 `IGameSession` 适配器测试） |
| U9 | 测试：`MainMenuUiPlayModeTests.cs:28,49-50`；`MapVisibilityPlayModeTests.cs:45,61`；`SettlementIconLayerPlayModeTests.cs:34,49`；`GovernmentPanelPlayModeTests.cs:36,38,73,141` | 直接 `FindAnyObjectByType<GameManager/Bootstrap/SceneFlowController>` | 保留（测试允许引用 App）；若 App 迁走，using 改 `MyriadCreation.App` |

**说明**：U5/U6/U7 是 A 卡新产生的 UI→App.Settings 依赖；App 当前不引 UI，所以合法；**进入步骤 3（迁 SceneFlow，App 开始引用 UI）之前必须解决**——推荐步骤 2 把 Settings 下沉 `Infrastructure`（UI 已引用 Infrastructure），或把 `ISettingsService`/`SettingsModel` 放 Contracts/Infrastructure。

**App → UI 当前依赖（迁移后 App asmdef 必须新增的引用）**
- `SceneFlowController` → `MainMenuUI`(36-38,67-77)、`UIManager`(58,76,103,149,241)；
- `GameManager` → `UIManager`(82,161,168,229)、`SettlementDetailPanel`(61,308,331-341)、`MapRenderer`(228,234,292-295)、`RuntimeMapEditingController`(233-235)、`SettlementIconLayer`(60,290-312)；
- `Bootstrap` → `GameManager`/`SceneFlowController`(76-80,106-110)（迁 App 后为同程序集调用）、`UIManager` 间接。

---

## 5. GameWorld 纯化切割线（基于 task-70 的 partial 文件）

目标：`Simulation.WorldState.GameWorld` 只保留 **数据字段/状态 + 只读查询**；组合根、生成编排、读档桥接、编辑执行、tick 调度全部搬到 App（或对应系统的 Service；Succession 搬到 Simulation.Succession）。**迁移期间保留 public 方法作为兼容 wrapper，直至所有调用方切换**（避免一次性改 200+ 测试）。

| 现文件（task-70 拆分后） | 保留/迁移 | 具体切割线（方法锚点） | 外部调用方 |
|---|---|---|---|
| `GameWorld.Core.cs`（950） | 字段/属性/常量**保留**；组合根/初始化**迁 App `WorldCompositionRoot`** | `InitializeWorld:470`、`InitializeSubsystems:532`（私有）、`ReinitializeSubsystems:577`、`InitializeGoodsDefs`/`InitializeUnitDefs`/`InitializeTradeCenters`/`InitializeDefaultRaces`/`InitializeDefaultCultures`/`ApplySetupOptions`/`ApplyContentOverrides`/`InitializeFaithSystems`/`EnsureAIControllers`、计划系统工厂/事件 handler/`RegisterDiplomacyCommandHandlers`；`Awake:936`（空）/`Update:940`（tick 循环） | GameManager 131/132/160/177；SaveSystem 458；大量测试（见下） |
| `GameWorld.Generation.cs`（586） | 生成**编排**迁 App `WorldFactory` 或 World.Generation 服务；纯生成算法留 World | `GenerateTerrain:123`、`GenerateTerrainBase:141`（私有）、`GenerateTerrainWithConfig:293`、`GenerateAll:356`、`GenerateTerrainAsync:537`、`GenerateProvinces/Settlements/InitialUnits`、`ApplyInitialSeaLandFragmentation`、`InitializeDefaultRealms` | GameManager 131-132/160/177；测试直接调用（SaveSystemRoundTripTests 52-53 等） |
| `GameWorld.Simulation.cs`（1130） | tick/调度**迁 App `SimulationRunner`**；气候/水文重算可留 World 服务或迁 `WorldMapSimulationSystem` | `AdvanceTime:335`（私有）、`GameTick:743`（私有）、`RegisterSimulationSchedules:771`（私有）、`CalculateClimate:457`、`RecalculateHydrology:489`、`RecalculateAll:517`、PopulationTick/各类 Daily/Yearly tick | 仅 `GameWorld.Update`（940）内部 + WorldMapSchedule；外部无 |
| `GameWorld.Succession.cs`（341） | **迁 `Simulation.Succession` 系统文件夹**（不是 App）；由 SimulationRunner 调用 | `NotifyRulerTransition:47`、`CheckRulerSuccessions`、`HandleInterregnum`、`TryStartInterregnum`、`TickLawEffectsYearly` 等 | 外部少量（grep 后再改；当前主要 World 内部） |
| `GameWorld.Persistence.cs`（88） | **迁 App `SessionPersistence`**；SaveSystem 构建/应用 world 的编排也归 App | `PrepareLoadedWorld:56`、`AdoptRuntimeIdCounters:76` | `SaveSystem.cs:458`（ReinitializeSubsystems）、`:474`、`:492`；`FaithPersistence.cs:12` 注释 |
| `GameWorld.Editing.cs`（115） | **迁 App `WorldEditService`**；Rendering 的 input adapter 只产生命令/请求 | `RecalculateDirty:47`、`PaintTerrain:52`、`UpdateConfig:62`、`CreateTile:69`、`RemoveTile:97` | `RuntimeMapEditingController.cs:140/149/157/165/184`；测试 `WorldSeedAndFragmentationTests.cs:480,515,520,594` |
| `GameWorld.Queries.cs`（318） | **保留**（WorldState 只读查询表面；后续可抽 `IWorldQuery` 到 Contracts） | `GetTile`/`GetLandTileCount:180`/`GetTilePopulation:163`/`GetCharacterManager:247`/`TryGetSite*`/`Get*Manager` 等 | UI（UIManager/面板/Presentation）、Rendering、测试大量使用 |

**外部对 GameWorld 生命周期的调用点（迁 App 后应只剩 App 内部）**
- `GameManager.cs:131,132,160,177`（世界创建/生成）；
- `SaveSystem.cs:274`（`worldObject.AddComponent<GameWorld>()`）、`:458,474,492`（读档重绑/ID 计数/PrepareLoadedWorld）；
- `RuntimeMapEditingController.cs:140,149,157,165,184`（编辑/重算）；
- 测试（EditMode 大量直接 `AddComponent<GameWorld>` + `InitializeWorld/GenerateTerrain`；例：`SaveSystemRoundTripTests.cs:48-53`、`WorldSeedAndFragmentationTests.cs:479-520`、`FragmentationClimateConsistencyTests.cs:66-82`、`RestoredSystemTests.cs:120-125`、`SeaLandPerformanceTests.cs:55/96-101`）——迁移期间兼容 wrapper 必须保留，否则千行测试要一次性改。

**RuntimeMapEditingController 改造**（Rendering→App）
1. Rendering 保留 input adapter：把拖拽/快捷键转成 `IMapEditCommand`（World.Editing 已有 `MoveSelectionCommand`/`RaiseSelectionCommand`/`MapEditHistory`）；
2. App `WorldEditService` 持有 `MapEditHistory`，执行命令并调用 `world.RecalculateDirty()`（`RuntimeMapEditingController.cs:139-149,183-184` 的职责迁出）；
3. UI 的编辑面板只发 `IWorldEditCommand`（Contracts 或 App 事件），不再持有 GameWorld；`GameManager.LoadGame` 的 `mapEditing.BindWorld(world)`（`:233-235`）改为 App 会话 `WorldChanged` 后重新注册 service；
4. `WorldRebindOnLoadTests` 反射 `RuntimeMapEditingController.world` 的契约需同步更新（测试改成验证 App service）。

---

## 6. 分步顺序、门禁、回滚点

> 前置：每一步提交/打标签（或至少备份 `Assets/Scripts/App`、`Assets/UI`、asmdefs、Tests），保证单步可 revert。移动文件时**连同 `.meta` 一起移**，保持 GUID（场景/预制体引用不受影响）；改 namespace 时同步 grep 所有引用。

| 步骤 | 内容 | 产物 | 门禁（Lead 执行） | 回滚点 |
|---|---|---|---|---|
| **S0（本卡 A）** | 设置服务 → App asmdef；UI 6 处 using；UI/Tests asmdef | `MyriadCreation.App.asmdef`、`App/Settings/*`、文档 | 编译 + `SettingsModelTests` + `SettingsPanelPlayModeTests` + Main 场景冒烟 | 把 2 文件+meta 移回、asmdef/refs 还原 |
| **S1 契约层** | 新建 `MyriadCreation.App.Contracts`（§3）；`AppServices` 定位器；在 App 内加 `GameSessionService`/`AppFlowService` 适配现有单例；UI 不改或仅新增订阅（行为不变） | Contracts asmdef + 接口 + 适配器 | 编译 + 全量 EditMode + PlayMode 主菜单/设置 | 删除 Contracts asmdef/接口（无消费者） |
| **S2 设置下沉** | 把 `SettingsModel/SettingsService` 从 App 移到 **Infrastructure**（或 SettingsModel→Core.Data、Service→Infrastructure；UI 已引用 Infrastructure）；namespace 改 `MyriadCreation.Infrastructure.Settings`；UI 5 文件/Tests using 从 `App.Settings` 改 `Infrastructure.Settings`；App 只留 `ISettingsService` 适配 | Infrastructure 新增 Settings，App 去掉 Settings | 编译 + 设置测试 + UI 依赖检查（UI 不再 using App） | 反向移动回 App |
| **S3 迁 GameManager** | 移 `GameManager.cs`+meta → `Assets/Scripts/App/MyriadCreation.App/`；namespace `MyriadCreation.App`；App asmdef 新增 refs：Contracts、Simulation、World、Rendering、UI、Unity.TextMeshPro（按需）；`Bootstrap`/`SceneFlow`/UI 对 GameManager 的引用改 `AppServices.Session`；`Tests` using 改 App；`SaveSystem`/`WorldRebind` 适配 | GameManager in App | 编译 + 全量 EditMode（WorldRebind/SaveSystem）+ PlayMode MainMenu/MapVisibility/SettlementIcon/Government | 文件+meta 移回 UI，refs/using 还原（GUID 不变） |
| **S4 迁 SceneFlowController** | 移 `SceneFlowController.cs`+meta → App；namespace 改 `MyriadCreation.App`；App refs UI（已具）；UI `MainMenuUI` 不再 `SceneFlowController.Instance`，改 `AppServices.Flow`；`Bootstrap` 创建逻辑改 `AppFlowService`；UIManager loading 事件化 | SceneFlow in App | 编译 + PlayMode MainMenu/MapVisibility + ESC 回归（SceneFlow ESC 与面板 ESC 双处理需一并看）+ 主菜单开始游戏/返回 | 移回 + 引还原 |
| **S5 迁 Bootstrap** | 移 `Bootstrap.cs`+meta → App（场景 `Main.unity` 序列化引用保持——脚本 GUID 随 meta）；namespace 改 `MyriadCreation.App`；启动顺序：App 创建 GameManager/SceneFlow、注册 AppServices、Settings；UI 不再见 Bootstrap | Bootstrap in App；App 成为场景入口 | 编译 + 全量 EditMode/PlayMode + 主场景启动/开始游戏/F5 F9/设置应用 + 新档读档 | 移回 + 引还原 |
| **S6 迁编辑执行器** | `RuntimeMapEditingController`（Rendering→App），实现 `IMapEditCommand` 输入适配；App 新建 `WorldEditService`；`GameManager.LoadGame` 的 mapEditing 重绑改 App 事件；更新 WorldRebind 测试 | 编辑执行器 in App | 编译 + MapEditing/WorldRebind/WorldSeed 测试 + PlayMode 地图编辑冒烟 | 移回 Rendering |
| **S7 GameWorld 纯化** | 按 §5 把 GameWorld partial 的 Core/Generation/Persistence/Editing/Simulation/Succession 内容迁 App/World/Simulation；GameWorld 只留字段+Queries；public 方法先留兼容 wrapper（标记 `[Obsolete]`）→ 调 App service；逐步改测试调用 | WorldState 纯化 | 每小步编译 + 对应 EditMode（SaveSystem/WorldSeed/Succession/Editing）+ 全量回归 | 按 partial 文件回滚（task-70 的拆分保留，可直接恢复单 partial） |

**S3/S4/S5 的顺序理由**：Bootstrap 会 `new GameManager`/`new SceneFlowController`，它必须等这两个类在 App 后自己再迁（否则 UI 程序集引用 App 具体类型 + App 引用 UI 成环）；S3 先让 UI 通过 `AppServices.Session` 不再见 GameManager，S4 再让 UI 通过 `AppServices.Flow` 不再见 SceneFlow，S5 最后移 Bootstrap（场景入口）。S2 是 S3 前置：App 一旦加 UI 引用，UI 不能再引用 `App.Settings`。

**每步门禁固定项**：
1) Unity Console 无编译错误/新增 warning（Lead 统一跑）；
2) EditMode 全量（当前 400+）零失败；
3) PlayMode：`MainMenuUiPlayModeTests`、`MapVisibilityPlayModeTests`、`SettlementIconLayerPlayModeTests`、`GovernmentPanelPlayModeTests`、`SettingsPanelPlayModeTests`；
4) 主场景手测：启动→主菜单→开始游戏（loading→游戏）→HUD→返回主菜单→F5/F9→设置应用；
5) asmdef 依赖检查：`App → UI` 单向，UI 无 `using MyriadCreation.App`；Contracts 无 UI/App 引用；
6) 文件移动保留 `.meta` GUID；场景 `Main.unity` 的 Bootstrap 引用有效性（S5 重点）。

**回滚点**：每个 S 步骤的备份（git commit/tag 或临时目录 `Assets/Scripts/App` + 三个 asmdef + 测试 asmdef 的副本）；S2 之前所有改动都是"移动+namespace+引用"，回滚 = 反向移动+refs 还原，无数据/场景风险；S3-S5 需要同时还原 asmdef 引用与测试 using。

---

## 7. 风险与待确认

| 风险 | 说明 | 缓解 |
|---|---|---|
| **asmdef 环** | App→UI 与 UI→App 不能同时存在；当前 A 的 UI→App.Settings 是临时合法，S3 前必须解决 | S1 Contracts + S2 设置下沉；CI 检查 asmdef 图 |
| **序列化/GUID** | 移动文件+meta 保留 GUID。Bootstrap 在 `Main.unity` 有序列化引用（Camera/Light/字段）；GameManager/SceneFlow 是代码 new 无场景引用；`GameWorld` 在场景里有对象但字段在 any partial（task-70 已处理） | S5 前后各做一次场景加载手测；不要手工重建 meta |
| **单例静态** | `GameManager.Instance`/`SceneFlowController.Instance` 被 UI/测试大量使用；改 `AppServices` 后若注册顺序/初始化失败，UI 空引用 | 适配器保留单例兜底一个版本（`AppServices.Flow ?? SceneFlowAdapter.Instance`）；UI 判空 |
| **测试直接 new App 类** | `WorldRebindOnLoadTests` 反射 `RuntimeMapEditingController.world`；PlayMode 测试 `FindAnyObjectByType<GameManager>`；GameManager 用 inactive GO 避免 Awake | S3 时同步改测试 using/反射契约；Tests asmdef 已允许引用 App |
| **ESC 双处理** | `SceneFlowController.cs:228-250` 与 `GovernmentPanel.Update` 都对 Escape 响应（task-60 已知） | 与 S4 一并处理：AppFlow 查询 `IsModalOpen`/面板 ESC 栈，SceneFlow ESC 在模态打开时不返回主菜单 |
| **设置 model 位置** | A 把 Settings 放 App，但 UI 仍直接 using App.Settings；S3 后成环 | S2 下沉 Infrastructure / Core DTO；UI 只依赖 Infrastructure + Contracts |
| **Performance/行为回归** | 迁移不改行为但调用路径变化（事件化 loading/world changed、保存顺序） | 每步跑全量 + 主场景手测；S7 逐 partial 小步 |
| **并行卡冲突** | 法律/信仰/文化（task-63/65 等）在移动文件；B3-part2 只动 App/UI/Bootstrap/GameManager/SceneFlow | 每步开始前 `git status`/重扫；不与 task-63/65 同文件并行 |

**待 Lead 决策**
1) 契约程序集命名：`MyriadCreation.App.Contracts`（推荐，独立小 asmdef）还是临时把接口放 `MyriadCreation.Core.Contracts`（需把 `IGameSession.World` 降为不暴露 `GameWorld` 的 DTO 查询）？
2) `IGameSession.World` 直接返回 `GameWorld`（迁移快、Contracts 依赖 Simulation）还是收窄为 `IWorldQuery`（更干净、工作量更大）？
3) 设置服务最终位置：Infrastructure（推荐，UI/App 都可用、无环）还是 Core.Data DTO + Infrastructure 实现？
4) S3/S4 是否接受"App 引用 UI"（一旦如此，UI 必须彻底去掉 App 具体类引用；需要同步改 U1-U9）？

---

## 8. 验证命令/门禁（只读，Lead 执行编译前可用）

```bash
# 1) asmdef 依赖环检查（无环 + App 不引 UI）
python - <<'PY'
import json,glob,os
for p in sorted(glob.glob('Assets/**/*.asmdef', recursive=True)):
    d=json.load(open(p,encoding='utf-8-sig'))
    print(d.get('name'),'->',d.get('references'))
PY

# 2) UI 不得 using MyriadCreation.App（A 期间允许 App.Settings，S2 后应为 0）
grep -rn "using MyriadCreation.App" Assets/UI --include='*.cs'

# 3) App 不得 using UI
grep -rn "using MyriadCreation.UI\|MyriadCreation.UI\." Assets/Scripts/App --include='*.cs'

# 4) 旧 namespace 残留
grep -rn "MyriadCreation.UI.Settings" Assets --include='*.cs'

# 5) 文件+meta 配对（移动后）
python - <<'PY'
import os
for d in ['Assets/Scripts/App','Assets/UI/MyriadCreation.UI']:
    print(d, 'cs', sum(1 for f in os.listdir(d) if f.endswith('.cs')), 'meta', sum(1 for f in os.listdir(d) if f.endswith('.cs.meta')))
PY
```

---

## 9. 本次 A 部分落地清单（供 Lead 对照）

| 项 | 内容 |
|---|---|
| 新 asmdef | `Assets/Scripts/App/MyriadCreation.App.asmdef`：name `MyriadCreation.App`、refs `MyriadCreation.Core`/`MyriadCreation.Infrastructure`/`UnityEngine.UI`、无 UI 引用 |
| 移动文件 | `Assets/UI/Settings/SettingsModel.cs(+meta)`、`SettingsService.cs(+meta)` → `Assets/Scripts/App/MyriadCreation.App/Settings/`；GUID `878b0ac8…`/`e259741c…` 随 meta 保留 |
| namespace | `MyriadCreation.UI.Settings` → `MyriadCreation.App.Settings`（两个文件） |
| UI using | `UiScaleBinder.cs:3`、`Bootstrap.cs:18`、`MainMenuUI.cs:10`、`SettingsPanel.cs:8`、`SettlementDetailPanel.cs:11` |
| asmdef refs | `MyriadCreation.UI.asmdef` + `MyriadCreation.App`；`Tests`/`Tests.PlayMode` asmdef + `MyriadCreation.App` |
| 测试 using | `SettingsModelTests.cs`、`SettingsPanelPlayModeTests.cs` → `using MyriadCreation.App.Settings` |
| 残留 | `Assets/UI/Settings/` 仅剩 `.gitkeep`（+ 旧文件夹 meta）；可留可删，本卡未删 |
| 未改动 | Bootstrap/GameManager/SceneFlowController 本体未动；法律/信仰/文化未动；场景/存档未动 |
