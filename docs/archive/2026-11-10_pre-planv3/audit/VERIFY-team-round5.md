# VERIFY-team-round5：复核第五轮（场景迁移 / UI 可点击 / 确定性重算 / 场景清理 + 旧程序集删除）

- **复核者**：verifier（只读）
- **约束**：未改 `MyriadCreation/**`、未跑 Unity/编译/测试、git 只读、只写本文件
- **两阶段证据（均已由我独立复算，且均晚于对应代码改动）**：

| 阶段 | EditMode | PlayMode 无图形 | PlayMode 有图形 | 编译 |
|---|---|---|---|---|
| A：task-24~27 之后、task-28 删除之前 | **361/361**（`14:37:16Z`，0 failed/0 skipped） | **2/2**（`14:37:39Z`） | **2/2**（`14:38:13Z`） | `team5-compile5.log` 0 error |
| B：task-28 删除之后（当前工作区） | **361/361**（`14:42:56Z`，0 failed/0 skipped） | **2/2**（`14:43:21Z`） | **2/2**（`14:43:51Z`，`playmode-gfx-results.xml` 22:44:02） | `team5-compile6-legacydeleted.log` 0 error |

- 删除动作时间线：`team5-compile6` 22:42:35（旧程序集已删后的编译）→ EditMode 22:42:56 → PlayMode 22:43:21/22:43:51；`playmode-gfx.log` 采样：MainMenu 5 按钮、射线命中 `开始游戏,Root`、`state MainMenu→Playing land=524287`；MapVisible `12/64、unique 13、亮度 0.145~0.730`
- 计数闭合：361 = 357 + 4（SeaLand 确定性 3 + parity 1）；PlayMode 2 = MainMenuUi + MapVisibility

---

## 1. task-24：Main.unity 旧 MapEditor GUID → 新 RuntimeMapEditingController

| 检查 | 结果 | 证据 |
|---|---|---|
| 旧 GUID `e768…`（旧 MapEditor）残留 | **0 处** | `grep -c e768508a… Main.unity` = 0 |
| 新 GUID `c0ef…` 出现次数 | **1 处** | `Main.unity:5111`，解析为 `Assets/Scripts/Rendering/Editing/RuntimeMapEditingController.cs` |
| 字段绑定 | **正确** | 该组件（挂在 GameWorld 对象上）序列化字段 `world/mapRenderer/mainCamera/brushRadius/editingActive` 与类字段名逐一对应（`RuntimeMapEditingController.cs:17-21`），引用 fileID 非 0（`826249784/454776274/199869214`） |
| 其它旧程序集脚本 GUID | **0** | 场景 197 处 `m_Script`、19 个唯一 GUID 全部可解析（0 missing），项目脚本仅 Rendering/MapRenderer、Simulation/GameWorld、RuntimeMapEditingController、UI/Bootstrap、GameManager、UIManager |
| 场景结构 | **合法** | SceneRoots = **8**；`StartMenuPanel/NewGamePanel/SettingsPanel/MapModeBar` 均 0；`MapModePanel` 保留 |

**结论：PASS-with-caveat。** caveat：① "仅 1 行"无法用 git 证明——工作区 Main.unity 相对 HEAD 为 `6931+/15517-`（含更早轮次恢复+本轮清理），只能证明最终状态"旧 GUID 唯一被替换、无其它旧 GUID"；② 场景里 6 处 `m_EditorClassIdentifier` 仍是旧程序集字符串（`MyriadCreation.Runtime::…`：MapRenderer/GameWorld/**Map.MapEditor**/Bootstrap/GameManager/UIManager），Unity 按 GUID 解析、不影响运行，但属卫生问题（建议 Unity 内重存场景清掉）。

## 2. task-25/26：SeaLandGenerator 确定性重算（两副本）

| 检查 | 结果 | 证据 |
|---|---|---|
| 两副本同步 | **是** | `Scripts/World/MyriadCreation.World.Generation/SeaLandGenerator.cs` 与 `Scripts/World/MyriadCreation.World.Terrain/SeaLandGenerator.cs` 仅 namespace 行不同（diff 6 行） |
| `UnityEngine.Random.value` 残留 | **0** | 两副本均改为 `GetDeterministicRecalcValue(tileIndex, salt)`（`:181-190`）+ 三个盐常量（`:26-30`），破碎化三处调用全部替换（`:207/231/238`） |
| 可选 ctor seed 兼容 | **是** | `SeaLandGenerator(config,tiles,w,h,int recalcSeed = 0)`（`:40`）；现有 4 参调用不破坏（生产 `GameWorld.cs:568` 仍 4 参 → `_recalcSeed=0`） |
| 测试 | **4 条且两阶段均通过** | `SeaLandRecalcDeterminismTests`：同 seed 必同、异 seed 必异、默认 4 参必同；`SeaLandVariantParityTests`：两副本同输入逐地块一致 |
| 判别性 | **敏感** | 同 seed 测试同时断言"重算实际改写地块"（`CountTileDifferences(base,first)>0`），防空转；异 seed 测试防"seed 被忽略"；parity 防单边漂移 |

**结论：PASS-with-caveat。** caveat（P2）：① 生产构造未传入世界 seed，`_recalcSeed` 恒为 0 → 重算"可复现"但**与世界 seed 无关**（注释说"需要复现时可显式传入"，实际无人传）；② 测试文档声称"不污染 UnityEngine.Random 全局状态"但无相应断言（实现确实不触碰，属覆盖缺口）；③ parity 只能抓"两份不一致"，若两份被同一错误确定性实现替换则无法发现（属镜像测试固有限制）。

## 3. task-27：主菜单可点击性 + UI 脚本清理

| 检查 | 结果 | 证据 |
|---|---|---|
| 5/6 个"零引用"UI 脚本删除 | **实际删除 6 个 .cs**（任务描述 5）+ 6 meta + `UI/Settlement/.gitkeep` = 13 条 D | TypographyConfig/UIButtonController/UIInputFieldController/UIProgressBarController/UITabController/UITooltipController；全库引用=0、场景引用=0、编译 0 error |
| Canvas 层级 | **正确** | `MainMenuUI.Build`：Canvas 挂在 MainMenuUI 自身层级（`MainMenuUI.cs:48-56`），`ScreenSpaceOverlay`、`sortingOrder=1000`（高于场景 UICanvas 0），CanvasScaler 1920×1080；不独立 DDOL（随 DDOL 的 `SceneFlowController` 保活，`SceneFlowController` 由 Bootstrap 运行时创建，`Bootstrap.cs:98-101`） |
| 主菜单禁地图点击 | **成立** | 全屏 `Root` 面板带 `Image`（`CreatePanel:176-185`，raycastTarget 默认 true）→ 主菜单可见时吞掉指针；隐藏后无遮挡 |
| 开始游戏异步 | **实现** | `GameManager.StartNewGameAsync`（`GameManager.cs:135-160`）→ `GenerateWorldAsync` 协程 + loading 面板 + `MissingReferenceException` 防护与 `world!=sourceWorld` 判据（`:161-205`） |
| TMP 中文字体 | **实现** | `TMPFontUtility.GetChineseFont`：`Resources/Fonts/simhei`（`Assets/Resources/Fonts/simhei.ttf` 存在）→ 动态 SDF 4096²；`TMP_Settings.instance` / 字体缺失均有 warning 回退 |
| EventSystem 兜底 | **安全** | `SceneFlowController.EnsureEventSystem`（`:191-205`）先含 inactive 查找、已有则启用、仅缺失才创建（DDOL），场景已有 EventSystem → 不产生重复 |
| 加载存档真读档 | **实现** | `SaveLoadPanel` 改用 `SaveSystem.GetSaveDirectory()/ListSaves()`（`:98-127`，修掉旧 MapSaves/*.json 扫空问题）；`MainMenuUI:387` 调用 `bootstrap.LoadGame(saveName)` |
| PlayMode 判别性 | **基本成立** | 日志证明 5 个按钮、射线命中 `开始游戏,Root`、点击后 `MainMenu→Playing、land=524287`；无图形/有图形两轮均通过 |

**结论：PASS-with-caveat。** caveat（P2）：① "可点击"射线断言仅 `graphics.Count>0`——全屏 Root 保证任何位置都命中，断言本身近乎恒真（日志可见顶端命中确为按钮，建议断言"命中目标==开始按钮或其子物体"）；② PlayMode 两个测试都**没有 TearDown 清理 DDOL 的 GameManager/SceneFlowController/MainMenuUI**，测试间共享跨场景对象（顺序依赖风险，当前顺序下通过）；③ PlayMode 覆盖的是同步 `bootstrap.StartNewGame()`，**异步路径（用户实际点击路径）未被测试覆盖**；④ 动态 SDF 4096² 的启动开销/显存未评估。

## 4. Lead 场景清理（任务外）

| 检查 | 结果 |
|---|---|
| 删除范围 | 备份 vs 当前：**removed 69 个 GameObject、added 0**；移除名全部属于 `StartMenuPanel/NewGamePanel/SettingsPanel/MapModeBar` 及其子物体（含 StartGameBtn/NewGameStartBtn/SettingsApplyBtn/DisplayModeDropdown/MapModeToggle 等）；**未误删** MapModePanel 或其它游戏面板 |
| 被删对象是否有自定义脚本 | **无**（逐个组件 GUID 解析：移除对象的所有 MonoBehaviour 均无项目脚本，仅 UGUI/TMP 组件） |
| 场景合法性 | roots=8（与 Lead 声明一致）；`UiDiagnostics`/独立复核均 0 missing script；文件 34,267,027→34,001,399 B（与 34.27MB→34.00MB 一致） |
| UIManager 对 null 字段 | **安全**：`mapModeBar/mapModePanel`（`UIManager.cs:199-200`）、`displayModeDropdown/displayModeSubDropdown/mapModeToggleButton`（`:288-302`、`MapMode.cs:41,80-87`）全部 null 保护 |
| PlayMode UI 冒烟 | 场景清理后（阶段 B）MainMenu 2/2 通过 |

**结论：PASS。** 唯一 caveat（P2）：`Assets/Editor/UiCleanup.cs`、`UiDiagnostics.cs` 是**未跟踪的一次性 batchmode 工具**（UiCleanup 会打开并保存场景后 `EditorApplication.Exit(0)`），不应随分支提交（或移出 Assets/加 `#if` 保护）。

## 5. task-28：旧程序集删除（B 线第二步 b）

| 检查 | 结果 | 证据 |
|---|---|---|
| 备份完整性 | **通过** | `D:/TEMP/legacy-runtime-backup.tar.gz` 356,445 B；sha256 `ead3baf4ccaee3af6e8b76f95d8b4634de9b07fa5920787f915133d59292293f`（与 Lead 前缀一致）；232 条目 = **210 文件 + 22 目录**；文件构成：91 `.cs` + 116 `.meta` + 1 `.asmdef` + 2 `.unity`（`_Recovery/0.unity`、`0 (1).unity`） |
| 删除范围与备份一致 | **一致** | 备份顶层 = 20 个旧顶层目录（AI/Audio/Bootstrap/Building/Climate/Culture/Diplomacy/Disaster/Economy/Legacy/Map/Military/Politics/Race/Render/Role/Tech/Thought/UI/War）+ 旧 asmdef(+meta) + `_Recovery`(+meta)；**无任何新架构路径**（Core/Infrastructure/Rendering/Simulation/World）被删 |
| 删除是否产生意外 git D 行 | **否** | 这些文件原本未跟踪（`git ls-files` 命中 0），删除后对应路径 D 行 = 0；`git status` 现为 `M 142 / D 945 / ?? 273`，其中 945 D 为历史遗留、与本轮删除无关 |
| 旧 GUID 非 .cs 资产残留 | **0** | 用备份中 116 个 `.meta` 的 GUID 反查当前全部非 `.cs` 资产（.unity/.prefab/.asset/.mat + ProjectSettings）：命中 0 |
| 场景/构建场景完好 | **是** | 删后场景 197 `m_Script`、19 唯一、**0 missing**；roots=8；`EditorBuildSettings` 仅 1 个场景 `Assets/Scenes/Main.unity`，其 guid `b3ae5f6a…` 与 `Main.unity.meta` 一致 |
| 路径字符串/反射静态依赖 | **无** | `.cs` 中指向 `Scripts/Map|Scripts/Legacy|Scripts/Render|Assets/_Recovery` 的字符串 = 0；`using CivilizationEvolution` = 0；`CivilizationEvolution` 仅剩 2 处自建 namespace（`Editor/AutoOpenMainScene.cs:6`、`Editor/GraphicsApiFix.cs:5`）+ 2 处注释（`GraphicsApiFix.cs:11` 用法说明、`CharacterSaveData.cs:545`） |
| 删后回归 | **通过** | 编译 0 error；EditMode **361/361**、PlayMode 无图形 **2/2**、有图形 **2/2**，时间均晚于删除（见基线表） |

**结论：PASS-with-caveat（可合入）。** caveat：
- **P2**：备份内 20 个旧目录以 `Assets/Scripts/` 为相对根（`AI/…`），而 `_Recovery` 以 `Assets/` 为相对根——**还原映射未随备份记录**，日后恢复需自行判断（建议补一份 `RESTORE.md` 或重新打包时保留完整相对路径）。
- **P2**：删除未触发"必须清理"的悬空引用，但两处历史遗留仍在：① 场景 6 处 `m_EditorClassIdentifier: MyriadCreation.Runtime::…`（含已不存在的 `MyriadCreation.Map.MapEditor`）；② `Assets/Data/DefaultWorldConfig.asset` 与 `Assets/ScriptableObjects/DefaultWorldConfig.asset`：`m_Script guid e156bdf2…` **在 Assets/PackageCache/备份中均不存在**（B 报告 P1-5 的历史问题，非本轮删除造成），且两个资产无代码/场景引用——建议删除或重建。均不影响运行（GUID 场景引用 0 缺失、编译/测试全绿）。
- **P2**：`AutoOpenMainScene`/`GraphicsApiFix` 仍用 `CivilizationEvolution.EditorTools` 命名空间（仅编辑器工具，无依赖）。

## 6. 上一轮（round4）P2 闭合情况

| round4 P2 | 现状 |
|---|---|
| P2-4 重算路径未播种 RNG（不可复现） | **已关闭**（task-25/26：两副本确定性散列 + 4 条测试） |
| P2-1 asmdef 归因证据不足 | 因 task-28 已删旧程序集，问题消解（当前全部 asmdef 无旧引用） |
| P2-2 bodyParts 20 vs 19 / count 数组仅自描述 | 未处理（仍存在） |
| P2-3 `CaptureBodyParts` 未判空 `part` | 未处理（仍存在） |
| P2-5 未知 faith 测试依赖精确文案 | 未处理（仍存在） |
| P2-6 `RestoredSystemTests` 不清静态注册表 | 未处理；本轮新增 PlayMode 两测试同样**无 TearDown**（跨测试 DDOL 污染风险） |
| P2-7 存档前向兼容 / P2-8 体积 | 未处理（记录） |

## 7. 问题清单（本轮无 P0/P1）

| 级别 | 问题 | 位置/建议 |
|---|---|---|
| P2-1 | 场景 6 处 + 两个 config 资产残留旧 `m_EditorClassIdentifier`（含不存在的 `MyriadCreation.Map.MapEditor`） | 在 Unity 内重存场景/资产可清；不影响运行 |
| P2-2 | 两个 `DefaultWorldConfig.asset` 的脚本 GUID `e156bdf2…` 无解析目标（历史遗留、资产未被引用） | 建议删除或重建资产 |
| P2-3 | 备份 tar 路径缺父级前缀，还原映射未记录 | 补 restore 说明 |
| P2-4 | PlayMode 两测试无 TearDown，DDOL GameManager/SceneFlowController/MainMenuUI 跨测试保留 | 增加测试隔离（销毁 DDOL 单例/重置 Instance） |
| P2-5 | MainMenu PlayMode 射线断言仅 `Count>0`（全屏 Root 保证命中） | 断言顶端命中==开始按钮 |
| P2-6 | 异步开始游戏路径（真实用户路径）无 PlayMode 覆盖（测试走同步 `StartNewGame`） | 补一条异步 onboarding 测试 |
| P2-7 | 生产 SeaLand 构造不传 world seed，重算随机与世界 seed 无关 | 接线 `randomSeed` |
| P2-8 | `TMPFontUtility` 运行时动态 4096² SDF 的启动开销/显存未评估；simhei 缺失时中文回退仅 warning | 评估/预烘焙字体资产 |
| P2-9 | `Assets/Editor/UiCleanup.cs`/`UiDiagnostics.cs` 为未跟踪的一次性破坏性 batchmode 工具 | 不随分支提交，或移到 Assets 外 |
| P2-10 | `AutoOpenMainScene`/`GraphicsApiFix` 残留旧命名空间 | 改名 `MyriadCreation.EditorTools` |
| P2-11 | 删除的 UI 脚本数量为 6（任务描述写 5） | 文档口径对齐 |

## 8. Merge gate 结论

| 卡 | 结论 | 说明 |
|---|---|---|
| **task-24（场景 GUID 迁移）** | **可合入** | 旧 GUID 0 残留、新 GUID 单点、字段绑定正确、删后场景 0 missing；建议 follow-up 重存清 stale identifier |
| **task-25/26（确定性重算）** | **可合入** | 两副本镜像、无 `Random.value`、可选 seed 兼容、4 条判别性测试两阶段全绿 |
| **task-27（UI 可点击 + 脚本清理）** | **可合入** | 主菜单/字体/EventSystem/读档/禁穿点击均已实现且有 PlayMode 证据；建议 follow-up 强化射线断言、补异步路径测试与 DDOL 隔离 |
| **task-28（旧程序集删除）** | **可合入** | 备份可核（size/sha256/232 条目）、范围精确（仅 20 旧目录+asmdef+_Recovery，无新架构文件）、非 .cs 资产旧 GUID 残留 0、场景/构建场景完好、删后编译与 EditMode/PlayMode 全绿；建议 follow-up 补 tar 还原说明、清理 stale identifier/orphan 资产、移除临时 Editor 工具 |
| Lead 场景清理 | **可合入** | 69 对象删除、0 新增、0 自定义脚本对象、UIManager null 安全、roots=8、PlayMode 通过 |

**总体：本轮无 P0/P1；task-24/25/26/27/28 与 Lead 场景清理均可合入。**

## 9. 待验证

1. 未运行 Unity：异步开始游戏/主菜单真实点击链路只在代码与日志层面验证；动态字体 4096² 的性能需实测。
2. 备份还原流程未实际执行（只核对了归档内容与删除集合的一致性）。
3. 两个 orphan `DefaultWorldConfig.asset` 在编辑器中的表现（是否产生 missing script 警告）未实机确认；代码/场景均未引用。
4. PlayMode 跨测试 DDOL 污染在当前执行顺序下通过，但换顺序/单独运行某测试的风险未测。

## 附录：关键只读证据

```bash
# 两阶段结果
grep -o 'total|passed|failed|start-time' Logs/editmode-results.xml          # 361/361, 14:42:56Z
grep -o 'total|passed|failed|start-time' Logs/playmode-results.xml          # 2/2, 14:43:21Z
grep -o 'total|passed|failed|start-time' Logs/playmode-gfx-results.xml      # 2/2, 14:43:51Z
# task-28 备份
python: tarfile → 232 条目/210 文件/91cs+116meta+1asmdef+2unity; sha256 ead3baf4…293f
# 删除后残留
grep -rn "using CivilizationEvolution" Assets --include=*.cs | wc -l        # 0
python: 备份 meta GUID × 当前非 .cs 资产 → 0 命中
python: Main.unity m_Script 197/19/0 missing；SceneRoots=8
grep EditorBuildSettings → 仅 Main.unity（guid 与 .meta 一致）
# 场景清理
python 备份 vs 当前：removed 69 / added 0；移除对象无项目脚本
# SeaLand
diff 两副本 → 仅 namespace 差；grep Random.value → 0
# UI
grep 删除的 6 个 UI 脚本名 → 0 引用；PlayMode 日志 buttons=5 / 命中开始按钮 / land=524287
```
