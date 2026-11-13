# VERIFY-team-round7：终局复核（破碎化/seed16/摄政/重命名/架构 B1-B5/TMP 清理）

- **复核者**：verifier（只读）
- **约束**：未改 `MyriadCreation/**`、未跑 Unity/编译/测试、git 只读、只写本文件
- **证据复算（均由我独立核对，晚于全部代码改动）**：

| 证据 | 结果 | 时间 |
|---|---|---|
| 全量 EditMode `Logs/editmode-results.xml` | **393/393**，0 failed / 0 skipped | `start 17:11:05Z`（01:11:05 本地）；最新代码 mtime 01:02 |
| PlayMode 无图形 `playmode-results.xml` | **3/3** | `17:11:26Z`（总时长 **89.95s**） |
| PlayMode 有图形 `playmode-gfx-results.xml` | **3/3** | `17:13:13Z`（总时长 **92.35s**） |
| 计数闭合 | **376 + 10（WorldSeedAndFragmentation）+ 7（Regency）= 393**；PlayMode 2+1（SaveLoadPanel）= 3 ✅ | 逐类 XML 计数核对（67 个测试类） |
| 既有测试未被删/未减数 | ArchitectureContractsTests 18 / SettlementMarketTests 6 / SimulationSchedulerBudgetTests 4 与上轮一致 | XML 逐类计数 |
| 运行后工作区 | 01:02 之后 `Assets/**` 无改动 | 结果对应最终代码 ✅ |

---

## 1. task-37/42：破碎化生效 + 初始开图应用 + seed 入档

| 检查 | 结果 | 证据 |
|---|---|---|
| 默认配置真实生效 | **是** | `SeaLandGenerator` 构造函数用 `BuildFragmentNoise(length,width,height,recalcSeed)` 生成确定性值噪声（3 层分形、整数格点哈希，`SeaLandGenerator.cs:66-150`），不再全 0；`ApplyLandFragmentation/ApplyCoastFragmentation` 的三处判定已改 `GetDeterministicRecalcValue < p`（`:207/231/238`）；日志实测 1024×512 世界 **陆地 151865/524288（29%）**，与 WorldConfig `landAmount=0.29` 一致 |
| 不触碰 UnityEngine.Random | **是** | 文件中仅 4 处注释提及 Random，无调用；重算噪声为纯整数/浮点哈希 |
| 初始开图应用 | **是** | `GenerateTerrainBase` 末尾（`:1796`）与 `GenerateTerrainWithConfig`（`:1864`）调 `ApplyInitialSeaLandFragmentation`，且都在省份/聚落/初始单位之前 → 下游按最终海陆生成；日志 `[SeaLand] 全量重算` 出现 3 次 |
| 读档不二次破碎 | **是** | `SaveSystem` 中 0 处 `GenerateTerrain*/RecalculateAll/RecalculateDirty`（grep 空）；读档只走 `ReinitializeSubsystems/PrepareLoadedWorld`；`RecalculateDirty` 本身**不含**破碎化分支（只按 cached threshold 重算单格）；测试 `SaveLoad_LoadedTerrain_MatchesSavedPerTile` 逐地块比对 isLand/coast/oceanTier/seaConnect/elevation ✅ |
| seed 入档（SaveVersion 16） | **是** | `GameConstants.SaveVersion=16`；`SaveData.randomSeed`（`:31`）；捕获 `SaveSystem:364`，恢复 `:452-453` 放在 `ReinitializeSubsystems` 之前（生成器构造即用新 seed）；旧档缺省回退 `DefaultWorldSeed=42`；测试覆盖往返可复现与 v15 旧档回退 |
| **性能** | **FAIL（P1）** | PlayMode 有图形总时长 **92.35s**、MapVisibility 单测 **85.77s**（无图形总时长 89.95s）；同样 1024×512 世界的同一测试在 round2 实测仅 **4.79s**。开图新增加一次全量重算，`RecalculateAll` 对每格调用 `GetNeighbourIndices`（`IEnumerable<int>` 迭代器，**每次 new int[6]×2 + 迭代器分配**、:419-440），一次全量重算含 RecalculateSingleTile ×N + `RecalculateAdjacencyForAll` ×2 + 连通 BFS ×N ≈ 2.6M 次调用/520k 格，GC 压力巨大；按面积外推 Large(2048×1024)≈4×、Huge(4096×2048)≈16× 不可接受 |
| **属性一致性** | **PASS-with-caveat（P1 债务）** | 重算把 `elevation01` 从"绝对高程"改为"相对海平面阈值"并改写 isLand，而 **biome/fertility/annualTemp/precip 仍是重算前由 PlanetTerrainGenerator/气候算出的**（代码注释 `GameWorld.cs:1800-1803` 自承"本卡不改"）。真实开图路径 `GenerateTerrainAsync`（`:2161+`）只做 TerrainBase→省份→聚落→单位，**不调用 CalculateClimate/RecalculateHydrology** → 新陆地的群系/肥力仍是旧海洋值，影响 Biome 视图、聚落选址、人口产出。作者已知并显式延后 |

**结论：功能 PASS，但两项 P1（性能 + 属性一致性）→ merge gate「需修/或产品明确接受后再合入」。** P2：`randomSeed<=0` 的合法世界会被回退成 42（`data.randomSeed > 0` 判定），若允许用户输入 0/负 seed 会换图。

## 2. task-38：摄政/推举兜底

| 检查 | 结果 | 证据 |
|---|---|---|
| 状态字段/持久化 | **实现** | `RealmData:87-96`（inInterregnum/regentCharacterId/interregnumStartDay/Months）；`RealmDTO:52-58,85-88,141-144` 编码/解码完整 |
| 状态机 | **实现** | `SuccessionSystem.TryEnterInterregnum:87-100`（进期即推举摄政，无合格者 -1 空转）；`TickInterregnum:108-177`：跨年回绕算天数/月数 → 法理君主在世（外部新君）→ 结束；指定 heir 或重找合法继承人 → 加冕结束；否则校验/重选摄政者；`EndInterregnum:179-185` 复位全部字段 |
| 与既有 Succession 兼容 | **是** | `GetSupremeRulerId` 摄政期返回摄政者、`GetLegalRulerId` 返回死君主（`RealmData:110-137`）→ 状态机不会把活摄政者误当君主；`GameWorld:1513-1517` 摄政期走 `HandleInterregnum` 并 `continue`，不与 `ExecuteSuccession` 重复结算 |
| 编年史 | **合理** | 进入空位/摄政者/空转/结束时各写一次（`GameWorld:1563-1615`），无每日刷条；加冕写 `succession` 并记录 `accessionDay/youngAccession/ruledUnderRegency` |
| 边界测试 | **7 条且判别性强** | 无继承人进入并选家族摄政（含 `GetLegalRulerId/GetSupremeRulerId` 断言）、继承人出现加冕并复位字段、无合格摄政者空转且编年史不刷、摄政者死亡重选、`TryEnter` 幂等、DTO 往返、外部新君结束摄政 |
| 旧档 | **安全** | 新字段默认 false/-1/0，旧档反序列化即"非摄政期" |

**结论：PASS（可合入）。** P2：`interregnumMonths` 只累计、无超时强制推举/加冕；摄政字段与 seed 共用 SaveVersion 16（同批 schema 变更，未来需注意版本纪律）。

## 3. task-39：重命名序列化兼容

- `MapGenerationConfig`：**只改枚举类型名**（`GenerationMapSize`/`GenerationCirculationMode`），序列化字段名 `MapSize`/`Circulation` 未变；`ToGameEnums` 明确映射（EnhancedMonsoon→TripleCell，季风增强由 MonsoonStrength×1.5 承担）。
- 场景 YAML 校验：`GenConfig` 块 **46 个序列化键全部能在类中找到**（0 缺失），数值 `MapSize: 0`（=Medium 1024×512）、`Circulation: 2`（=ThreeCell）保持不变 → 数值语义兼容 ✅。
- `GameEnums.UnitArmType`：由旧名改为 Infantry/Cavalry/Navy，**成员顺序不变**（注释声明），仓库内 0 处旧枚举名残留；UnitDef 为运行时定义、无序列化资产依赖。
- **结论：PASS（可合入）。**

## 4. task-40：架构审计报告

`docs/audit/ARCH-COMPLETION-PLAN.md` 存在（20,826 B，00:34 落盘），含 A/B/C 类错位全量清单与 **6 个批次**计划（文中 13 处批次标记），并记录清理构建残留 6 文件与"未移动任何 .cs（本卡）"。**结论：PASS（可合入）。**

## 5. task-43~47：程序集解耦 B1-B5

| 检查 | 结果 |
|---|---|
| asmdef 图收敛 | **是**：Core→[]；World→[Core]；Simulation→[Core,World]；Rendering→[Core,World,Simulation]；**Infrastructure→[Core,World,Simulation]（已无 Rendering）**；UI→[Core,Simulation,Rendering,UI,Infrastructure,World]；Editor/Tests/PlayMode 引用齐全；9 个 asmdef 均为合法 JSON |
| 跨层引用归零 | **是**：Core/World 中 `using MyriadCreation.Simulation*` = **0**、全限定 `MyriadCreation.Simulation.` = **0**；Infrastructure 中 `using MyriadCreation.Rendering` = **0**；`MyriadCreation.Core.Simulation` 旧命名空间 = **0**，新 `Simulation.Scheduling` 被 24 个文件引用 |
| 目录↔程序集层对齐 | **基本 0 错位**：838 个 .cs 全部有最近 asmdef（**0 个 asmdef 外 .cs**）；各层根目录平铺 .cs 全部为 0；唯一 2 处命名空间/asmdef 根名差异是**有意保留**的 `Editor/MyriadCreation.EditorTools`（asmdef 仍为 MyriadCreation.Editor）|
| 移动完整性 | **是**：838 .cs / 838 .cs.meta（**0 缺失、0 孤儿**）；目标目录齐备（UI.Presentation 12、Simulation.Scheduling 11、Infrastructure.Save 20 / Audio 2、Rendering 树 10、Editor 树 10、World.Settlement 47）；旧目录 `Infrastructure/Save|Audio`、`Rendering/Overlays|Terrain`、`Simulation.Presentation` 全部消失 |
| B2 结算簇归位 | **是**：`SettlementMarket/SettlementEconomy/SettlementEconomySnapshot/DerivedEconomicProperties` 均已迁入 `World.Settlement` 且 namespace 为 `MyriadCreation.World.Settlement`（A1-A3 错位消失，Core 下已无 Simulation.* 目录） |
| B3 删 MapSaveSystem | **是**：文件删除、全仓仅 SaveLoadPanel 一处注释提及历史格式、0 代码引用；Infrastructure asmdef 已去 Rendering |
| B4 Presentation 拆分 | **是**：12 文件 → `Assets/UI/MyriadCreation.UI.Presentation/`（namespace `MyriadCreation.UI.Presentation`），2 文件归位 Simulation（PlaceNameGenerator/LawComponentNames），3 死代码（AppearancePresenter*）0 引用、已删 |
| 场景/资产未因移动损坏 | **是**：场景 197 处 `m_Script`/19 唯一 GUID/0 missing（移动同时带走 .meta，GUID 稳定）；全工程资产 `m_Script` 缺失 = 0 |

**结论：PASS（可合入）。** P2：`ArchitectureContractsTests` 名称虽含 Architecture，实际无任何 asmdef 图/层级断言（grep asmdef/AssemblyDefinition=空）→ 建议新增自动化"图层守卫"测试，防止这批解耦被回退。

## 6. Lead：TMP 示例清理 + PlayMode 测试修正

| 检查 | 结果 |
|---|---|
| TMP Examples 删除 | **干净**：`Assets/TextMesh Pro/Examples & Extras/` 已不存在；备份 `D:/TEMP/tmp-examples-backup.tar.gz` = 298 条目 / **285 文件 / 34 .cs**，全部带 `Assets/TextMesh Pro/Examples & Extras` 前缀（可原样还原）；Fonts/Resources/Shaders 保留 |
| 项目引用 | **0**：删除后全工程资产 `m_Script` 缺失从 26（均为示例自身）降为 **0**；无工程代码/场景/设置引用示例 |
| 临时构建残留 | `TEMPdecompiled-sim/`、`.reverse/`、`CivilizationEvolution*.csproj` 均已不存在；Unity 正常生成的 `*.csproj` 保留 |
| MapVisibility PlayMode 修正 | **未放宽，反而更严**：保留全部取景/几何断言（地图已生成、MapRenderer/相机/网格、平面缩放=1、相机俯视、中心射线落点容差）；背景判定改为**相机自身 `backgroundColor` ±0.03**（旧为硬编码色，收紧）；新增 `avgSat>0.10` 饱和度断言；无图形分支明确只验证"场景/对象/渲染链"并打日志（有图形分支做全部像素断言） |
| 存档删除 PlayMode 新测试 | **严格**：第一次点击不删档且文案变"确认删除"、第二次真删、全程 `loadCalls==0`（证明未误触父条目加载）；有 TearDown 清理 |

**结论：PASS（可合入）。** P2：新测试按"第一个文本为『删除』的按钮"定位，若 Saves 目录存在其它存档可能误选行（当前环境为空，通过）；MapVisibility 像素断言只能测"颜色多样性/亮度/饱和度"，**无法区分海与陆**（见 §7 视觉疑点）。

## 7. 视觉疑点（P2，待人工确认）

- `D:/map_verify.png`（01:13 本轮 gfx）经像素统计：640×360、**unique 颜色仅 98**、230,400 像素中 **230,384 为蓝色系**、亮像素(>90) 仅 67、绿色系仅 7 → 画面几乎全是深蓝海洋色；而同一次运行日志里地块统计为 **陆地 151,865 / 524,288（29%）**。
- 两种解释：① 相机取景恰好落在海洋区（几何断言只保证射线落点距地图中心 < 8% 对角线，容差较大）；② 破碎化后 `elevation01` 语义改变而渲染/群系未重算导致陆地被染成海洋色。**代码证据更支持①/②中的属性不一致（§1）**，但现有 PlayMode 断言无法判别。
- 建议：为 MapVisibility 增加"陆地色相像素占比 > X%"或"地图纹理中 isLand 统计与像素分类一致"的断言，避免"海洋或空世界也能过"。

## 8. 测试有效性 / 是否放宽

- **未发现断言放宽**：全测试目录 0 个 `[Ignore]/[Explicit]/Assert.Pass/Assert.IsTrue(true)`；`ignoreFailingMessages=true` 仅出现在"预期错误日志"的失败路径测试（既有的 SaveSystem/Legacy 模式）。
- 新增 17 条 EditMode + 1 条 PlayMode 均为具体数值/状态断言（10 条破碎化/seed、7 条摄政、1 条删除交互），且关键测试含"否定性前置"（如异 seed 必须不同、第一次点击不得删档）。
- **但**：架构解耦、破碎化视觉一致性、大图性能三类风险**没有自动化守卫**（见 §5/§7）。

## 9. 问题清单

| 级别 | 问题 | 建议 |
|---|---|---|
| **P1-1** | 开图性能回归：破碎化新增全量重算，1024×512 单次开图实测 ~80s（MapVisibility 85.77s vs 历史 4.79s）；`GetNeighbourIndices` 每调用分配 `int[6]×2`+迭代器，重算路径 ~2.6M 次分配；Large/Huge 外推不可接受 | 预计算/缓存邻居表、去掉迭代器分配；或把破碎化并入主生成单遍完成；加"1024×512 开图 < N 秒"性能测试 |
| **P1-2** | 破碎化把 elevation 改为相对值并改写 isLand 后，biome/fertility/annualTemp/precip 未重算（代码自承）；真实开图路径 `GenerateTerrainAsync` 不调用 CalculateClimate/RecalculateHydrology | 在破碎化后补气候/群系/肥力重算（或把破碎化放到 PlanetTerrainGenerator 内、气候之前）；至少为 biome/fertility 一致性加测试 |
| P2-1 | `randomSeed<=0` 被当作旧档回退 42；若允许 0/负 seed 会换图 | 用 nullable/哨兵或同时存 version 判定旧档 |
| P2-2 | `interregnumMonths` 只累计无超时推举 | 明确产品规则（是否 N 月后强制选举/摄政王） |
| P2-3 | 摄政字段与 seed 共用 SaveVersion 16 | 下批 schema 变更记得 bump |
| P2-4 | 架构解耦无自动化守卫（ArchitectureContractsTests 不测 asmdef 图） | 新增图层/引用守卫测试 |
| P2-5 | 视觉疑点：截图近全海洋 vs 数据 29% 陆地；PlayMode 断言无法区分海/陆 | 增加陆地色占比断言 + 人工看图 |
| P2-6 | PlayMode 删除测试按文本"删除"取第一个按钮，Saves 目录非空时可能选错行 | 按存档名精确定位 |
| P2-7 | `Assets/Editor/MyriadCreation.Editor/` 下留有 5 个未跟踪临时工具（BatchCompileCheck/ResaveScene/TmpSettingsCreator/UiCleanup/UiDiagnostics） | 不提交或移出 Assets |
| P2-8 | 场景/资产仍有旧 `m_EditorClassIdentifier` 字符串残留（历史）；`GraphicsApiFix` 等注释类残留不影响 | 可在 Unity 重存清理 |

## 10. Merge gate

| 卡 | 结论 |
|---|---|
| **task-37/42（破碎化生效 + seed16）** | **功能正确、测试充分，但需修（P1-1 性能、P1-2 属性一致性）后再合入**；若产品明确接受"开图慢 + 群系暂不重算"作为已知债务，可条件合入并登记 |
| **task-38（摄政/推举）** | **可合入** |
| **task-39（重命名）** | **可合入**（场景 46 键/数值全兼容） |
| **task-40（架构计划文档）** | **可合入** |
| **task-43/44/45/46/47（B1-B5 解耦）** | **可合入**（0 跨层引用、0 错位、meta 完整、场景 GUID 0 缺失） |
| **Lead（TMP 清理 + PlayMode 修正）** | **可合入**（备份可还原、项目引用 0、断言未放宽反而加强） |

**总体：393/393 + PlayMode 3/3×2 全绿，无 P0；两项 P1（开图性能、破碎化后属性未重算）需 Lead 决策/修复；其余 P2 均为卫生与守卫类。**

## 11. 待验证

1. 未运行 Unity：性能数据来自 PlayMode 实测时长（同测试历史对比），未做 profiler 归因；Large/Huge 外推未实测。
2. 视觉疑点（近全海洋）是数据/取景/属性不一致中的哪一种，需人工看 `D:/map_verify.png` 与实机。
3. 摄政期"无摄政者空转"的产品预期（是否应限时强制推举）未定义。
4. 备份还原（TMP/旧程序集）流程未实跑。

## 12. 复核完成后的工作区变更（证据时效提示）

复核结论落盘后（01:18–01:22）工作区又出现以下改动，**当前代码已不再等于 393/393 那次测试的状态**，建议修复完成后再跑一次全量 + PlayMode 再定稿：

- `Assets/Scripts/Simulation/.../GameWorld.cs`（01:19）、`WorldMapSimulationSystem.cs`（01:20）：疑似对"初始破碎化后首次 tick 的 Dirty 重算会再改写地形"与重算成本的修正；
- `Assets/Tests/EditMode/WorldSeedAndFragmentationTests.cs`（01:18）：新增 2 条测试 `GenerateTerrain_ThenDirtyRecalc_DoesNotRewriteTerrain`、`PaintTerrain_ThenDirtyRecalc_OnlyRewritesMarkedNeighbourhood` → 若重跑，测试总数应为 **395**（393+2），请以新 XML 为准；
- 新增未跟踪文件 `Assets/InitTestSceneb395173a-215b-4013-a10b-fb40e339d6f7.unity(.meta)`（编辑器测试残留，建议清理/不提交）。

本报告中 §1 的两项 P1（性能、属性一致性）与 §7 视觉疑点仍以 01:11–01:13 的测试快照为准；新改动是否解决 P1 需重新复算。

## 附录：关键只读证据

```bash
grep -o 'testcasecount|total|passed|failed|skipped|start-time' Logs/editmode-results.xml   # 393/393/0/0
逐类：WorldSeed 10 / Regency 7 / ArchitectureContracts 18 / SettlementMarket 6
duration: playmode-gfx total 92.35s；MapVisibility 85.77s；历史同测 4.79s
grep "\[SeaLand\]" Logs/playmode-gfx.log → 陆地151865/524288、陆地75/288
python: asmdef 图/838cs-838meta/0 外部 cs/0 跨层 using/0 缺失 m_Script
sed -n '1783,1820p;2161,2260p' GameWorld.cs            # 初始破碎化 + 异步路径无气候
sed -n '66,150p' SeaLandGenerator.cs                    # 确定性分形噪声 + 邻居分配热点
sed -n '87,190p' SuccessionSystem.cs；RealmData:110-137  # 摄政状态机
python: 场景 GenConfig 46 键全部匹配类字段；MapSize:0/Circulation:2
tar: D:/TEMP/tmp-examples-backup.tar.gz → 298 条目/285 文件/34 cs，路径前缀正确
grep/asmd: ARCH-COMPLETION-PLAN.md 6 批；TEMPdecompiled/.reverse/CivilizationEvolution csproj 均无
python PIL: D:/map_verify.png unique=98，蓝色占 230384/230400
```

## 13. 定点复核（两项 P1 修复后，task-50/51 + GC 修复）

**证据复算（代码 mtime ≤ 02:01:41，均早于下列运行）**

| 证据 | 结果 |
|---|---|
| 全量 EditMode | **399/399**，0 failed / 0 skipped（`18:03:01Z`，13s） |
| 计数闭合 | 393 + 2（DirtyRecalc 不二次改写）+ 1（SeaLandPerformance）+ 3（FragmentationClimateConsistency）= **399** ✅（逐类：WorldSeed 12 / SeaLandPerf 1 / Consistency 3） |
| PlayMode 无图形 | **3/3**，总 **10.80s**：MainMenu 4.86s / MapVisibility **5.65s** / SaveLoad 0.27s ✅ 与 Lead 说法一致 |
| PlayMode 有图形 | **3/3**，总 **99.15s**：**MainMenu 92.64s** / MapVisibility 6.19s / SaveLoad 0.28s ⚠️ 见下 |

### P1-1 性能：真解 ✅

- `SeaLandGenerator` 构造期构建扁平邻居表 `_neighbourStart/_neighbours`（`BuildNeighbourTable`），热路径统一走 `GetNeighbourSpanUnchecked` → `ReadOnlySpan<int>`（RecalculateSingleTile/Adjacency/Dirty/Connectivity 均无每格 `new int[6]`），旧分配热点消除。
- `SeaLandPerformanceTests.GenerateTerrain_1024x512_UnderLooseBudget_AndDeterministic`：同 seed 两次 1024×512 `GenerateTerrain`，**逐地块地形哈希**（exists/isLand/oceanTier/isCoast/seaConnectId/elevation01/oceanDepth01/waterAdjacentWeight）+ 陆地数一致 + 每次 < 10s 预算。日志实测 **2552/2746/2656/2647/2552ms**（≈2.55–2.75s，与 Lead 一致）；历史回归为 85s 级 → 预算 10s 足以判别（旧实现必红）。
- P2 备注：预算 10s 相对实测 2.6s 偏宽（约 4× 余量，CI 抖动容错可接受）；perf 测试陆地数（151959）与上轮 Custom 世界（151865）不同，源于 Custom/Standard 配置路径不同，非本修复断言范围，未展开。

### P1-2 一致性：真解 ✅

- 生成段 `ApplyInitialSeaLandFragmentation` 在重算前快照 elevation/isLand/isCoast → 重算后 `RecalculateDerivedClimateAfterFragmentation`。
- changed 判定正确：海陆/海岸翻转，或 `|Δelev - 统一折算中位数| > 0.01`；中位数从样本估计（日志 `统一高程折算=0.4981`，**106234/524288** 地块），避免"全表被统一减阈值"误判成全表 changed —— 这是该修复的关键点，符合 Lead 描述。
- 单地块重算：温度用**破碎化前绝对高程**（`RecalculateBaseClimateForTile(..., elevationForTemperature=快照)`，防整体偏暖）；biome 用 `HoldridgeBiomeClassifier.Classify`、fertility 用其 `CalculateFertility`（海地 fertility=0）；与 `PlanetTerrainGenerator` 生成期内联公式逐项核对**一致**（-10+cos·38、elev·65、海洋/海岸/内陆 3/0/-2；降水带 1800/500/1200/250、坡度+400、内陆-200）。`CalculateClimate` 全量路径未改。
- 3 条测试：changed 地块一致性（独立按当前字段复算）、同 seed 确定性、读档不重算；读档测试用 sentinel（annualTemp=1234.5 等）做**否定性前置**，且 `SaveSystem` 中 0 处 Calculate/Recalculate 调用 → 读档不重算成立 ✅。
- P2：`RecalculateDerivedClimateForTile` 未同步刷新 `airHumidityPct`（生成期有 `Clamp01(annualPrecipMm/2000)`）→ changed 地块湿度仍是旧值；P2：公式在 helper 与生成内联各一份（当前完全一致，建议收敛到一处防未来漂移）。

### GC 修复 ✅（范围与边界）

- `System.GC.Collect()` **仅出现于** `MainMenuUiPlayModeTests.TearDown` 与 `MapVisibilityPlayModeTests.TearDown`（测试专用），运行时/AI 代码 0 处；headless 两项时长稳定（4.86s / 5.65s），未观察到跨测试污染或断言异常。P2：测试内强制 full GC 属"掩盖型"清理手段，长期会掩盖真实泄漏，建议仅在确实复现跨测试膨胀时保留。
- ⚠️ **证据口径需修正**：gfx 运行 MainMenu 92.64s（总 99.15s），与"MainMenu ~5.4s"不符；同代码 headless 仅 4.86s。大概率是 graphics 首测预热（日志有 `UnityShaderCompiler.exe` 连接、`CyowariHistoricalMap` fallback 警告、TMP `LoadFontAsset` 等首帧开销），非产品逻辑回归，但**建议 Lead 归因/复跑后再引用"稳定"结论**（非阻塞，报告口径应注明"headless 稳定"）。

### 顺带修复

- `ClearInitialTerrainDirty()`（生成段收尾清 `_terrainDirtyTiles` + 调度器 `world.terrain` key）：修掉首 tick 二次全表重算/二次改写（`_cachedSeaThreshold` 非 0 时必现）；新增 `GenerateTerrain_ThenDirtyRecalc_DoesNotRewriteTerrain`、`PaintTerrain_ThenDirtyRecalc_OnlyRewritesMarkedNeighbourhood` 覆盖 ✅。

**定点结论：两项 P1 均已真解，GC 修复只动测试且无污染；无阻塞项。** 仅余 P2：perf 预算偏宽、`airHumidityPct` 未同步、气候公式两处重复、gfx 首测 92.6s 需归因（headless 已符合 Lead 数字）。


## §14 Lead 补充（gfx 首测口径归因）

对 §13 的"gfx 首测 92.64s"口径做了复跑归因：连续重跑 gfx PlayMode 两次，均稳定为
`MainMenu_IsBuiltAndClickable 5.4–5.9s / Map_GeneratesAnd_VisibleInCamera 5.8–6.2s / SaveLoadPanel 0.2–0.3s`。
结合 headless 稳定 4.9–5.2s，判定该 92.64s 为**环境争用 outlier**（当时并行编译/Unity 工作进程占用 CPU），
非产品回归；gfx 与 headless 的稳态耗时分别记录在 §13/§14，测试框架已加 TearDown GC 防大世界泄漏。
