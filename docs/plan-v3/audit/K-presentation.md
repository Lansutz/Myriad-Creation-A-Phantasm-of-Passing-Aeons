# 表现与入口层 审计（2026-11-09）

> 只读。路径相对 `MyriadCreation/`。三态：`在跑` / `半跑` / `空转` / `不存在`。不写方案。

## 0. 范围

- `Assets/UI/**` **28** 个 .cs：UIManager 四 partial（553+747+200+140）、Panels 6 个（441/343/704/445/299/51）、Presentation 12 个、Common 其余 4 个。
- `Assets/Scripts/App/**` **16** 个 .cs：Bootstrap 216、GameManager 474、SceneFlowController 287、SettingsModel 540、SettingsService 184、WorldEditService 216、App.Contracts 8 个。
- `Assets/Scripts/Rendering/**` **12** 个 .cs：MapRenderer 四 partial（757+381+353+186）、SettlementIconLayer 536、SphericalMapRenderer 299、CyowariMapStyle 258、Editing 2 个 297、MapLayerConfig/Registry 227、IMapEditController 20。
- `Assets/Editor/**` **7** 个 .cs（任务写 3 个，实为 7：Tools 5 + TMP 1 + Content 1；TerrainPainter 只有 2 个枚举文件）。
- `Assets/Scenes/Main.unity` 14762 行 / 34.0 MB：全量提取 107 个命名对象及其 `m_IsActive`；精读 UICanvas 的 UIManager 序列化块 7726-7804、TopBar 6298、MapPlane 2418、MapModePanel 8532、MapModeList 9807。
- 未覆盖：着色数学逐式核对、`Assets/Tests/**` 只作"有无测试"引用、`Assets/Art/**`、Sim 侧适配器内部实现。

## 1. 系统清单

| 系统 | 三态 | 驱动证据 | 入口证据 | 存档证据 | 测试 | 一句话现状 |
|---|---|---|---|---|---|---|
| 主菜单 MainMenuUI | 在跑 | `Assets/Scripts/App/MyriadCreation.App/SceneFlowController.cs:56-67` | 5 按钮 `Assets/UI/Panels/MainMenuUI.cs:119-126` | 前置，无 | PlayMode 冒烟 | 唯一链路完整的入口 |
| 存档选择 SaveLoadPanel | 半跑（缺"游戏内入口"） | `MainMenuUI.cs:362-374` | 仅主菜单「加载世界」 | 读 `SaveSystem.ListSaves` `SaveLoadPanel.cs:120`；删 `:312` | `Tests/PlayMode/SaveLoadPanelPlayModeTests.cs` | 能读能删，**无"存档"按钮** |
| 设置 SettingsPanel | 半跑（缺"游戏内入口"） | `MainMenuUI.cs:413-423` | 仅主菜单「设置」 | PlayerPrefs 15 键 `Settings/SettingsModel.cs:234-249`，**不入 .sav** | 无面板级测试 | 12 项设置确实落到引擎 `SettingsService.cs:118,134,161-169` |
| 政府 GovernmentPanel | 在跑 | 代码创建 `Assets/UI/Common/UIManager.Panels.cs:706-744` | 顶栏「政府」按钮 `:343-363` + G 键 `UIManager.cs:360` | 制度/法令由 `GameWorld.Simulation.cs:802` 每日推进 | `Tests/PlayMode/GovernmentPanelPlayModeTests.cs` | 4 个写命令全走通 |
| 聚落详情 SettlementDetailPanel | 在跑 | `GameManager.cs:428-442` | 地图图标左键 `SettlementIconLayer.cs:418,321` | 只读 DTO | 有 | 唯一"点地图有效果"的入口 |
| 社会 SocietyPanel.cs | **不存在** | `Assets/UI/Panels/SocietyPanel.cs:11` 实为 `SocietyPanelText` 静态类 | 场景 `societyPanel` 的开启按钮为 0（`Main.unity:7779`） | — | 无 | 面板是场景对象，入口丢失（§6） |
| 地块/角色/家族树/音乐/总览 面板 | 空转 | 场景对象存在、序列化引用为 0（§4） | 无 | — | 角色/地块有测试直调 `SetSelectedTile` | 见 §6 |

## 2. 玩家命令表（按钮 → 契约 → 后端）

| 动词 | 面板 | 契约方法 | 后端入口 | 走通 |
|---|---|---|---|---|
| 开始游戏（默认世界） | 主菜单 | `IAppFlow.GenerateWorldWithLoading(WorldSetupOptions)` → `IGameSession.StartNewGameAsync(WorldSetupOptions,Action)` | `GameManager.cs:239-242` → `WorldFactory.Create` + `WorldGenerationService.GenerateTerrainAsync` | 是 |
| 创建世界（自定义数量） | 主菜单 | 同上，`InitialContentMode.Custom` | `NewGameRequestMapper.cs:44-47` `options.Normalize()` | 是（数值被 `ParseCount` 夹到 `MainMenuUI.cs:320-322`） |
| 加载世界 | 主菜单+SaveLoadPanel | `Bootstrap.LoadGame` → `IGameSession.LoadGame` | `GameManager.cs:284-320` 重绑渲染/UI/编辑/图标 | 是 |
| 删除存档（二次确认） | SaveLoadPanel | `SaveSystem.DeleteSave` | `SaveLoadPanel.cs:312` | 是 |
| 应用/重置设置 | SettingsPanel | `SettingsModel.Save` + `SettingsService.Apply` | `SettingsPanel.cs:201-231` | 是 |
| 暂停 / 1x / 2x / 3x | 顶栏速度组 | `IGameSession.SetGameSpeed` | `GameManager.cs:323-329` → `Time.timeScale` | 是 |
| 暂停切换 | 无 UI（空格） | `GameManager.TogglePause` | `GameManager.cs:332-338`；按键 `:467-471` | 是（但无按钮） |
| 推行制度 | 政府 | `ILawCommand.BeginEnactLaw` | `LawHost/LawContracts.cs:183-193` | 是 |
| 取消推行 | 政府 | `ILawCommand.CancelPendingLaw` | `GovernmentPanel.cs:355-363` | 是 |
| 颁布法令 | 政府 | `ILawCommand.EnactDecree` + `IDecreeCommand.IssueDecreeOrders` | `GovernmentPanel.cs:375-389` → `DecreeCommandAdapter.cs:18` | 是 |
| 撤销法令 | 政府 | `ILawCommand.RevokeDecree` | `GovernmentPanel.cs:365-373` | 是 |
| 切换显示模式 | **无入口** | `MapRenderer.SetDisplayMode` | `MapRenderer.cs:584-589` | **否**（§3/§6-8） |
| 打开政权总览 / 区划下钻 | **入口恒隐藏** | — | `UIManager.Panels.cs:512` | **否**（§6-5） |
| 地图编辑 12 命令 | **无入口** | `IMapEditCommandService` ×12 | `WorldEditService.cs:19` | **否**（§3） |
| 快速存/读（F5/F9） | 键盘 | `GameManager.SaveGame/LoadGame` | `Bootstrap.cs:203-213` | 部分（F9 失败会软锁，§5-e） |

只读消费：`IMapStatsView`（顶栏 `UIManager.cs:52-79`）、`IRealmQuery`（`GetRealm` `:69`、`GetRuler/GetAllRealms` `GovernmentPanel.cs:250,261`）、`ICharacterQuery/ICharacterDetailQuery`（`UIManager.Panels.cs:528,562`）、`ITileInfoQuery`（`:469`）、`ISettlementQuery`（`SettlementDetailPanel.cs:200`）、`ISocietyQuery`（`ClassPanelText.cs:72`）、`IFaithQuery`（`ReligionPanelText.cs:25-43`）。

## 3. 契约层

| 契约 | 定义 | 实现者 | UI 消费者 | 判定 |
|---|---|---|---|---|
| `IGameSession` | `App.Contracts/IGameSession.cs:13` | `GameManager.cs:30` | `SceneFlowController.cs:165-167,195-203` | 在跑 |
| `IGameUiHost` | `App.Contracts/IGameUiHost.cs:10` | `UIManager.cs:22` | `GameManager.cs:104,176,296`；`SceneFlowController.cs:73,107,188` | 在跑 |
| `ISettlementDetailUi` | `App.Contracts/ISettlementDetailUi.cs:10` | `SettlementDetailPanel.cs:16` | `GameManager.cs:431,441`；`UiFactory.cs:37-58` | 在跑 |
| `IUiFactory` | `App.Contracts/IUiFactory.cs:9` | `UiFactory.cs:12` | `SceneFlowController.cs:58`；`UIManager.cs:278` | 半跑：`EnsureGovernmentButton`（`IUiFactory.cs:14`/`UiFactory.cs:73`）**0 调用者**，且实现只是 `ShowGameUI()` |
| `IMainMenuUi` | `App.Contracts/IMainMenuUi.cs:6` | `MainMenuUI.cs:18` | `SceneFlowController.cs:84,104,117,135` | 在跑 |
| `ISettingsService` | `App.Contracts/ISettingsService.cs:7` | `AppSettingsService.cs:9` | **0 处**（UI 直接用 `SettingsService` 静态） | 空转 |
| `IAppFlow` | `App.Contracts/IAppFlow.cs:20` | `SceneFlowController.cs:17` | **0 处**（UI 直接用 `SceneFlowController.Instance` `MainMenuUI.cs:222`） | 空转 |
| `IWorldReadModel`（13 子契约聚合） | `Core.Contracts/IWorldReadModel.cs:8-11` | `Simulation.WorldState/WorldReadModel.cs:15` | 注入 `UIManager.cs:87-96` | 在跑，但只用 7 类子契约 |
| `IMapStatsView` | `Core.Contracts/IMapStatsView.cs:8` | `WorldReadModelAdapter.cs:9`、`WorldReadModel` | `UIManager.cs:52-79` | 半跑：`SettlementCount/RealmCount` 无消费者；`mapInfoText` 场景为 0（`Main.unity:7804`）→ 地图统计行永不显示 |
| `IRealmQuery` | `Core.Contracts/UiQueryContracts.cs:140` | `WorldState/RealmQueryAdapter.cs:15` | 见 §2 末段 | 在跑 |
| `ILawQuery`/`ILawCommand` | `Core.Contracts/LawContracts.cs:59,73` | `Simulation.LawHost/LawContracts.cs:15` | `GovernmentPanel.cs:287-290,341,360,370,381` | 在跑 |
| `IDecreeCommand` | `Core.Contracts/UiCommandContracts.cs:7` | `WorldState/DecreeCommandAdapter.cs:9` | `GovernmentPanel.cs:385` | 在跑 |
| `ICultureQuery`/`IInnovationQuery`/`IFactionQuery`/`IRegimeQuery`/`ISettlementPeopleQuery` | 同聚合体内 | Simulation 侧 | `Assets/UI` 下 grep 命中 0 | 空转（契约聚合进来但无 UI 用） |

- "契约有定义无 UI 用"：`ISettingsService`、`IAppFlow`、`IUiFactory.EnsureGovernmentButton`、上述 5 个子查询契约。
- "UI 需要但契约没有"：**地块点选**。`UIManager.cs:81-84` 明确注释"UI 无地块契约，改为安全空实现"；`MapRenderer.ScreenToTile:628` 存在但 UI 无任何路径调用 → 地块详情面板失去数据来源（§6-6）。
- 越界检查：`Assets/UI` 全目录 grep `MyriadCreation.Simulation` = **0 命中**；`MyriadCreation.UI.asmdef:4-14` 依赖列表无 Simulation，App 依赖 Simulation（`MyriadCreation.App.asmdef:10`）——硬边界成立。

## 4. 地图与渲染

- **模式数量不是 6 而是 12**：`MapDisplayMode` 12 值 `MapRenderer.cs:742-756`；UI 9 类别 `UIManager.cs:475-478`；`ModeMap` 只填 12 条且**缺 (5,0)** `:489-503` → 类别「经济」无映射（点了也无 mode 可设）。
- 数据来源：Terrain=`isRiver/elevation01/annualPrecipMm/fertility/annualTemp` + 本世界陆地 min/max（`Texture.cs:31-44`、`Colors.cs:126-167`）；Climate=`annualTemp`（`Colors.cs:169-171`）；Biome=`tile.biome`（`:173-177`）；Political=`ownerRealmId`+`IMapRealmView.IsSubordinate`（`Colors.cs:293-327`），其中效忠深度**恒返回 0**（`:332-336` TODO 未接）；Population=`populationBlocks` 求和/100（`:182-188`）；Diplomacy/Alliance=`TryGetDiplomacyRelation`（`:218-246`）；Culture/CultureBranch=主人口块 `cultureId`+`TryGetCulture`（`:250-271`）；Religion/Succession/Tradition=主人口块 `faithId`+`ReligionCatalog.GetColor`（`:280-285`）。
- **MapRenderer 每帧≠全图重算，但每次重绘是多趟全图**：`Update()`（`MapRenderer.cs:178-213`）每帧只做尺寸漂移检测 + 相机输入 + 编辑输入（`#if UNITY_EDITOR`），纹理重绘按 1s / 编辑 0.2s 节流（`:55-56`）。单次重绘的趟数：Terrain 高程 prepass（`Texture.cs:31-44`）→ 基色（`:50-58`）→ 占领条纹（`:79-92`）→ 省界（`:114-121`）→ 网格（`:131-141`）→ `SetPixels`（`:158`），即 1024×512=524288 像素 × 最多 6 趟，每趟都 `TryGetTileView`。
- `SettlementIconLayer.Update`（`:400-419`）每帧：视图/计数比对（可能 `Rebuild`）+ 缩放变化时重算 LOD + 左键 `SelectAtScreen`。
- **`IMapEditCommandService` 实际入口 = 无**：12 方法（`Editing/IMapEditCommandService.cs:15-48`），唯一实现 `WorldEditService.cs:19`，唯一调用方 `RuntimeMapEditingController.cs:171-219`；该组件在场景中**无实例**（Main.unity 107 个命名对象无此项；只有测试 `Tests/EditMode/MapEditCommandServiceTests.cs:241` 用 AddComponent 造），因此 `GameManager.InjectMapEditService:363-375` 的 `FindAnyObjectByType` 恒 null。另一条 `IMapEditController`（`MapRenderer.cs:59-60`）**全工程 0 实现** → `HandleEditorInput`（`:248-267`）恒早退。编辑器侧亦无画笔实现（`Editor/Tools/TerrainPainter/{BrushMode,BrushShape}.cs` 只是枚举，零消费者）。

## 5. 场景 vs 代码双源

**场景摆的**（UICanvas 12 个子节点 `Main.unity:7880-7892`）：TopBar(含 DateText/RealmNameText/CharOpenBtn)、TileInfoPanel、EventLogPanel、LoadingPanel、MusicPanel、FamilyTreePanel、SocietyPanel、ReligionPanel、OverviewPanel、MapModePanel、SpeedGroup、DivisionButtons。
**运行时建的**：MainMenuUI(`SceneFlowController.cs:58`)、SettlementDetailPanel(`UiFactory.cs:53-57`)、GovernmentPanel+遮罩(`UIManager.Panels.cs:724-736`)、「政府」按钮(`:343-363`)、模式按钮(`MapMode.cs:121-176`)、SettlementPeoplePanel(`Panels.cs:75-141`)、Toast(`UIManager.Feedback.cs:78-128`)、SaveLoadPanel/SettingsPanel(`MainMenuUI.cs:366,417`)、聚落图标层(`SettlementIconLayer.cs:100-110`)。

失效点（`Main.unity:7726-7804` 引用断链，对应场景对象仍存在）：

| 字段 | 值 | 场景对象 | 后果 |
|---|---|---|---|
| `characterPanel` | 0（7763） | CharacterPanel 存在，act=0（9338） | 顶栏「角色」按钮能点，面板永不显示 |
| `societyOpenButton` | 0（7779） | SocietyPanel act=0（4142） | 社会面板无入口 |
| `musicOpenButton` | 0（7783） | MusicPanel act=0（5148） | 音乐面板无入口 |
| `familyTreeOpenButton` | 0（7794） | FamilyTreePanel act=0（14045） | 家族树无入口 |
| `displayModeDropdown`/`SubDropdown`/`mapModeToggleButton`/`mapModeBar` | 全 0（7757-7760） | — | 模式切换 UI 全断 |
| `mapInfoText` | 0（7804） | 无该对象 | 地图统计行永不显示 |
| `world`（UIManager 7738、MapRenderer 2453） | 非 0 | — | 代码已删该字段，Unity 忽略 → 纯残留 |

- `MapModePanel` 场景 act=0，其唯一子节点 `MapModeList` 的 `m_Children` 为**空**（`Main.unity:9824`）；而 `SetGameUIVisible(true)` 会无条件把它打开（`UIManager.cs:296-297`）→ 游戏内右上角出现空的 180×420 面板。唯一填充函数 `RefreshMapModeList`（`MapMode.cs:83`）只由 `ToggleMapModeBar`（`:68`）调用，后者只绑在 null 的 `mapModeToggleButton` 上 → 永不填充。
- 默认模式与注释不符：`InitializeUI` 想设「教统」(8,1)（`UIManager.cs:420-423`），但两个 Dropdown 为 null 使 `:422` 与 `:421` 走 null 分支，最终落到 `(8,0)` Religion。

## 6. 启动链

`Bootstrap.Awake`（`Bootstrap.cs:48-124`）：①注册 `ISettingsService`:51 → ②`SettingsService.ApplySaved()`:55 → ③设置取地图预设与尺寸:58-67 → ④`ContentRegistry.Initialize`:72、`Localization.Initialize`:76 → ⑤`AddComponent<GameManager>`:79-83（**GameManager.Awake 同步立即执行**：注册 GameSession、建 `WorldEditService`、`InjectMapEditService`、`InjectUiReadViews`，但此刻 `AppServices.UiHost` 未注册 → `GameManager.cs:380-381` 直接 return）→ ⑥再次注入:86-90 → ⑦`ConfigureNewGameDefaults`:93 → ⑧相机/灯光:96-114 → ⑨创建 `SceneFlowController`:119-123。
`SceneFlowController.Start`（`:47-78`）：`HideLegacyUi` + `EnsureEventSystem` + `IUiFactory.CreateMainMenuUi`（工厂由 `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` 注册，`UiFactory.cs:18-22`）→ `Build/Show` → `UiHost?.HideGameUI()`。
`UIManager.Awake:332-336` 只注册 UiHost；`Start:338-346` 才设 `Instance`、字体、`InitializeUI`（所有按钮 AddListener + `EnsureGovernmentButton`:460）。
点「开始游戏」→ `MainMenuUI.OnQuickStart:213-233` → `EnterWorldGeneration` + `GenerateWorldWithLoading` → `GameSession.StartNewGameAsync` → `GameManager` 建 world + `InjectMapWorldView:394-407` → `RebindSettlementIcons:413-435` → `ChangeState(Playing)` → `TryShowGameUi`。

失败点 / 降级：

- (a) UIManager.Awake 晚于 Bootstrap.Awake → ⑤⑥ 两次注入空转；靠世界建好后的 `InjectMapWorldView` 补偿。首帧顶栏显示 `-`（`UIManager.cs:64,70,77`）是设计内降级。
- (b) world 未就绪时面板降级不抛：政府面板显「世界未加载」（`GovernmentPanel.cs:273-285`）、聚落详情显「聚落查询未注入」（`SettlementDetailPanel.cs:183-186`）、角色/家族树显 `-`（`Panels.cs:517-533,584-593`）。
- (c) `IGameSession` 缺失：`GenerateWorldCoroutine` 报错即 abort（`SceneFlowController.cs:213-218`），而主菜单已在 `EnterWorldGeneration:84-86` 隐藏 → 无菜单、无 loading、无回退。
- (d) `EnsureGovernmentButton` 依赖 `GameObject.Find("TopBar")`（`Panels.cs:337-341`）；TopBar 改名/失活时按钮与 G 键入口同时失效（G 仍可用，但 `_governmentPanel` 从未创建，`ToggleGovernmentPanel:446-456` 会先建面板，故 G 是唯一兜底）。
- (e) **F9 软锁**：`GameManager.LoadGame` 失败只 `ChangeState(MainMenu)` + LogError（`:315-319`），不调用 `IAppFlow.ReturnToMainMenu`（`SceneFlowController.cs:132-142`）→ 游戏内读不存在的档后停在 MainMenu 状态，但主菜单 Canvas 未 Show、游戏 HUD 未 Hide。

## 7. 不可达清单（代码在、玩家到不了）

1. **角色面板**：`charOpenButton` 非空可点，`OpenCharacterPanel:573-578` 在 `characterPanel == null` 时只更新文本、永不 `SetActive(true)` → "能点到但没有效果"。
2. **家族树 / 社会 / 音乐面板**：开启按钮序列化为 0（`Main.unity:7794,7779,7783`），代码分支写好但永不可达（`Panels.cs:302,252,267`）。
3. **政权总览 OverviewPanel**：唯一入口 `viewRealmButton` 在 `UpdateTileInfo` 的空数据分支被 `SetActive(false)`（`Panels.cs:465,473,512`），而 `_selectedTile` 恒 `-1`。
4. **地块详情 TileInfoPanel**：`SetGameUIVisible(true)` 会显示（`UIManager.cs:294`），但文本恒 `-`（`Panels.cs:462-467`）——看得见、全是占位。
5. **区划下钻**：`OnDivisionClicked:202`、`CreateRuntimeButton:217` 零调用；`RealmOverviewText.Build` 的 `officeDisplay/selectedDivisionId` 在唯一调用点恒为默认（`Panels.cs:679`）。
6. **显示模式切换**：入口三件套全为 0 + 列表面板空 → 玩家改不了任何模式（§5）。
7. **地图编辑**：`RuntimeMapEditingController` 无场景实例 + `IMapEditController` 0 实现（§4）。
8. **聚落人物面板**：`EnsureSettlementPeopleEntryButton:43` 零调用 → 入口按钮永不创建；`SettlementPeopleText` 仅测试用。
9. **编辑器入口**：`[MenuItem]`/`EditorWindow`/`CustomEditor` 全工程命中 **0**；7 个 Editor 脚本均为 `-executeMethod` 批量工具（`BatchCompileCheck.cs:9`、`GraphicsApiFix.cs:15`、`TmpSettingsCreator.cs:13`），`ContentPackageExporter.Export:17` 与 `BrushMode/BrushShape` 零消费者。
10. **渲染空转**：`ToggleOverlay:595`、`IsOverlayEnabled:602`、`SetProjectionMode:610`（且注释承认球面投影未实现）无调用者；`SetDualSpaceViewRealm:346` 零调用（政治图恒用 `PlayerRealmId`）；`SphericalMapRenderer`/`CyowariMapStyle` 是 MonoBehaviour 但场景无实例；`MapLayerRegistry` 仅测试。
11. **展示层死代码**：`PopulationCompositionPanelModel`、`SiteTypeLabels`、`SettlementPeopleText`（仅测试）；`FactionNames`/`JunctureNames`/`ClassNames`（0 消费者）。
12. **反馈通道空转**：`ShowToast`（`UIManager.Feedback.cs:57`）、`ShowConfirmation`（`:132`）零调用；后者是"直接执行 onConfirm"的假确认框（`:132-137`）。
13. **`_viewRealmId` 永不改变**（`UIManager.cs:251`，无赋值点）→ `ViewRealmId` 恒 0：社会/宗教/总览/角色/政府面板全部只读 realm 0，与"点选地块自动跟随"注释不符。
14. **不入档**：速度、显示模式、查看政权、选中地块、面板开关均无 `.sav` 字段（`Assets/Scripts/Infrastructure` grep `gameSpeed|displayMode` = 0）；设置只进 PlayerPrefs。

## 8. 缺口清单

| # | 缺口 | 影响 | 证据 | 严重度 |
|---|---|---|---|---|
| 1 | 地图点选完全缺失（`HandleMouseClick` 空实现，UI 无地块契约） | 地块详情/政权总览/区划下钻/查看政权跟随链条整体失效 | `UIManager.cs:81-84`；`Panels.cs:462-467,512` | P0 |
| 2 | 显示模式无任何玩家入口 | 12 种地图视图实际只有 1 种（Religion）可见 | `Main.unity:7757-7760,9824`；`UIManager.cs:296-297,420-423` | P0 |
| 3 | 9 处序列化引用断链（fileID 0），对应场景对象仍在 | 4 个面板 + 模式栏 + 地图统计成为死 UI | `Main.unity:7763,7779,7783,7794,7757-7760,7804` | P0 |
| 4 | 地图编辑双入口皆空（控制器无实例、画笔接口 0 实现） | 12 个编辑命令 0 可达 | `RuntimeMapEditingController.cs` 场景无实例；`IMapEditController.cs:10` 无实现 | P1 |
| 5 | 游戏内无存档 UI（仅 F5 quicksave） | 玩家无法命名存档/多档管理 | `SaveLoadPanel.cs:106-144` 仅被 `MainMenuUI.cs:362-374` 调 | P1 |
| 6 | F9 读档失败软锁 | 停在 MainMenu 状态但界面仍是 HUD | `GameManager.cs:315-319` vs `SceneFlowController.cs:132-142` | P1 |
| 7 | `ISettingsService`/`IAppFlow`/`IUiFactory.EnsureGovernmentButton` 无消费者 | 契约层有壳无实，App/Host 解耦目标未用上 | `Assets/UI` grep `AppServices.Settings|AppFlow` = 0 | P2 |
| 8 | 每次纹理重绘多趟全图 + 每趟 `TryGetTileView` | 大图（2048×1024 以上）每秒重绘成本高 | `Texture.cs:31-44,50-58,79-92,114-121,131-141,158` | P2 |
| 9 | 5 个子查询契约聚合进 `IWorldReadModel` 但无 UI 消费 | 契约面虚胖，读不出真实需求 | `IWorldReadModel.cs:8-11` vs `Assets/UI` grep 0 | P2 |

## 9. 证据命令与计数

- `grep "\[MenuItem|EditorWindow|CustomEditor" Assets/**/*.cs` → **0 命中**。
- `grep "IMapEditController" Assets/**/*.cs` → 32 命中中实现类 **0**（仅接口 `IMapEditController.cs:10`、字段 `MapRenderer.cs:59`、setter `:60`）。
- `grep "AppServices\.(Settings|AppFlow|MainMenuUi|GameSession)" Assets/UI` → **0 命中**（仅 `UiFactory`/`UiHost` 两处）。
- `grep "SetSelectedTile|GetSelectedTile" Assets/**/*.cs` → 7 命中，运行时调用 **0**（测试 2 处）。
- `grep "OnDivisionClicked|CreateRuntimeButton" ` → 仅定义（`Panels.cs:202,217`）。
- `grep "EnsureGovernmentButton"` → `IUiFactory.cs:14`+`UiFactory.cs:73` 无外部调用；`UIManager` 私有同名方法由 `UIManager.cs:43,460` 调用。
- `grep "FactionNames|JunctureNames|ClassNames"` → 仅定义 3 处，消费者 0。
- `grep "gameSpeed|displayMode" Assets/Scripts/Infrastructure` → **0 命中**（不入档）。

## 10. 我核实不了的事

- 未运行 Unity，无法验证 `Main.unity` 里 9 处 fileID 0 是否在编辑器中已被 Unity 自动置空之外的运行期行为（例如是否另有脚本在运行期 `Find` 并回填这些引用）。代码侧 grep 未发现这类回填。
- 未逐帧实测纹理重绘耗时；上述"多趟全图"是静态读码结论，无 profiler 数据。
- `MapModeSubs`/`ModeMap` 之外的类别（如"经济"）是否曾在旧版本有 `MapDisplayMode` 值，未查 VCS 历史。
- `Tests/**` 中绿/红状态未运行，只据文件存在判断"有测试"。
- `SettlementIconLayer` 的 LOD/锚点池内部逻辑（536 行中约 300 行未精读）与图标点击命中判定的边界情形未核实。
