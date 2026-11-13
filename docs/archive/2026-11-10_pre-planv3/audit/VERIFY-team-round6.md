# VERIFY-team-round6：复核第六/七轮（新系统收口 / Succession 端到端与修复 / seed 接线 / 读档重绑 / 删除收尾）

- **复核者**：verifier（只读）
- **约束**：未改 `MyriadCreation/**`、未跑 Unity/编译/测试、git 只读、只写本文件
- **证据复算（均由我独立核对，且晚于全部代码改动）**：

| 证据 | 结果 | 时间 |
|---|---|---|
| 全量 EditMode `Logs/editmode-results.xml` | **376/376**，0 failed / 0 skipped | `start 15:39:23Z`（23:39:23 本地）；代码最新 mtime 23:38:00（SuccessionEndToEndTests） |
| PlayMode 无图形 `playmode-results.xml` | **2/2** | `15:39:46Z` |
| PlayMode 有图形 `playmode-gfx-results.xml` | **2/2** | `15:40:14Z` |
| 定向 round6 `team-round6.xml` | **15/15**（Succession 7 + SeaLand 3 + WorldSeed 2 + Rebind 3） | `15:29:36Z` |
| 定向 round7 `team-round7-succession.xml` | **11/11** | `15:39:04Z` |
| 计数闭合 | 361 + 11（Succession）+ 3（Rebind）+ 2（WorldSeed）− 1（SeaLand parity 删除）= **376** ✅ | 逐类 XML 计数核对 |
| 运行后工作区 | 23:40 之后 `Assets/**` 无任何改动（结果对应当前最终代码） | `find -newermt 23:40` = 空 |

---

## 1. task-30：重复 SeaLandGenerator 删除 / 坏资产修复 / Editor 命名空间

| 检查 | 结果 | 证据 |
|---|---|---|
| Generation 重复类删除 | **已完成** | 全库只剩 `Scripts/World/MyriadCreation.World.Terrain/SeaLandGenerator.cs` 一个定义；`World.Generation/` 仅剩 InitialContentMode/MapGenerationConfig/SandboxSpawnRates/Seed/WorldSetupOptions；**0** 处引用 `World.Generation.SeaLandGenerator` |
| 生产引用 | **指向 Terrain 版** | `GameWorld.cs:124/571/630` 使用 `MyriadCreation.World.Terrain.SeaLandGenerator`；`Army/CombatManager/PlanetClimateSimulator` 的 `using` 均解析到该版本（编译 0 error） |
| 坏资产修复 | **通过** | `ScriptableObjects/DefaultWorldConfig.asset` 的 `m_Script` guid `c8dd9470…` → 现存在 `Core.Data/WorldConfig.cs.meta`；`m_EditorClassIdentifier` 已为 `MyriadCreation.Core::MyriadCreation.Core.Data.WorldConfig`；场景 GameWorld `config` 引用 `cf2df798…` → 现唯一解析到该资产（**重复 GUID 已消除**：全库重复 GUID 扫描 = **0**） |
| Editor 命名空间 | **已改** | `AutoOpenMainScene.cs:6`、`GraphicsApiFix.cs:5` 均为 `MyriadCreation.EditorTools`；执行入口注释同步 |
| 重复类型审计表 | **未见落盘** | 仓库 `docs/**`/根目录 23:40 前无新增审计表文件（可能仅在协作消息中交付）→ P2 证据缺口 |
| 额外删除 | **需说明** | `git status` 另有 tracked 删除 `Assets/Scripts/World/Terrain/SeaLandGenerator.cs`（旧路径第三份），0 引用、编译/测试通过；卡描述未明示，归入去重亦可 |

**结论：PASS-with-caveat（可合入）。** caveat：审计表未落盘；`World/Terrain/SeaLandGenerator.cs` 删除需在变更说明中登记。

## 2. task-31：Succession 端到端（真实 Realm + CharacterManager 夹具）

- 夹具：`SuccessionEndToEndTests` 用真实 `RealmData`；`CharacterManager` 无公开"注入既有角色"API，测试用反射写入私有 `_characters`，家族用公开 `CreateFamily` 建立（文件头 :17-21 明确声明）。
- 覆盖：长子/幼子继承、绝嗣、幼主、共和选举、继承线与大圣战受益人、在世/空参早退（7 条，`team-round6.xml` 7/7）。
- 暴露的 6 条缺陷由 task-34 修复（下节）。**结论：PASS**。

## 3. task-32：世界 seed 接入 SeaLand 重算路径（重点评估"破碎化生产不触发"）

| 检查 | 结果 | 证据 |
|---|---|---|
| seed 接线实现 | **正确** | `InitializeSubsystems` 用 `randomSeed` 构造生成器并存 `_seaLandRecalcSeed`（`GameWorld.cs:571-572`）；`EnsureSeaLandGeneratorSeed`（`:624-633`）在 seed 变化时重建 + `WMS.ReplaceSeaLandGenerator`；调用点：`GenerateTerrainBase`（`:1728`）、`GenerateTerrainWithConfig`（`:1772`）、`GameWorld.RecalculateAll`（`:2006`）、`WMS.BeforeFullRecalculate`（`:89`） |
| 世界级测试 | **通过且敏感** | `SeaLandWorldSeedRecalcTests`（2 条）：同 world.seed 两次 `world.RecalculateAll()` 逐地块一致；异 seed 必须不同（若不接线则两世界完全相同 → 必红） |
| **破碎化生产触发** | **不触发（属实）** | `SetFragmentNoise` 全库仅测试调用（`SeaLandRecalcDeterminismTests:79,191`）；生产无任何 `_fragmentNoise` 生产者 → 默认全 0 → 破碎化分支永不进入 |
| 严重性评估 | **P2（非回归）** | ① 该路径在改动前同样是死路径（`landFragment/coastFragment` 配置从未生效），本轮未使其更差；② 用户可见影响：无（seed 接线对生产行为无可观测变化）；③ 代价：`+2` 世界级测试+seed 基建目前只覆盖合成注入场景，属"为未来功能预置的确定性保证"。**若产品要求破碎化实际生效，则是功能缺失（建议单独立卡：由地形噪声/特征系统产出 fragmentNoise 并接入）** |
| 衍生 caveat | P2 | ⓐ 重建生成器后 `PlanetClimateSimulator._seaLand` 仍持有旧实例（`WorldMapSimulationSystem.ReplaceSeaLandGenerator` 只换 WMS 持有者），两处会短暂分裂；ⓑ `randomSeed` **未入档**（SaveData 无该字段）→ 读档后 world.randomSeed=42，重算 seed 与原局不符（与审计 A P1-2 同源） |

**结论：PASS-with-caveat（可合入）。** 代码与测试正确；"生产不触发"为**既有死路径**，非本轮回归，P2 记录并由产品决定是否立卡。

## 4. task-33：读档重绑 + 存档删除二次确认

| 检查 | 结果 | 证据 |
|---|---|---|
| `RuntimeMapEditingController.BindWorld` | **实现** | `:54-66`：换 world、重找 MapRenderer、camera 空则取主相机、清 selection/history/drag |
| GameManager 读档重绑 | **active+inactive 都覆盖** | `GameManager.LoadGame`（`:209-231`）：销毁旧世界→重绑 `MapRenderer`/`UIManager`/`RuntimeMapEditingController`（`FindObjectsInactive.Include`） |
| 测试判别性 | **强** | `WorldRebindOnLoadTests` 3 条：active/inactive 控制器读档后 `Assert.AreSame(loadedWorld, controller.world)`（不重绑必红）；`BindWorld(null)` 不抛；`GameManager` 用 **inactive GameObject** 创建避免 Singleton/DDOL 污染 ✅ |
| 删除二次确认 | **实现** | `SaveLoadPanel`：第一次点"删除"→文案"确认删除"；同条目再点才 `SaveSystem.DeleteSave`；`_pendingDeleteName` 在刷新/换条目/开关面板复位（`:108-110,296-320`） |
| **误触父条目加载风险** | **未发现** | 每行是父 `Button`（点击=加载并 Hide）；删除是**子 Button**，自带 `Image`（raycastTarget 默认 true），子 Label `raycastTarget=false`（`:238-247`）→ uGUI 命中链顶端为子按钮，`ExecuteHierarchy` 只派发给最近的 click 处理器 → 父条目不会同时收到点击。删除按钮与行按钮的 onClick 互不重叠 |
| 删除路径安全 | **通过** | 名称来自 `SaveSystem.ListSaves()`（已校验名集合）→ `DeleteSave` 内再次 `IsValidSaveName`；失败仅 warning + 刷新对齐磁盘 |
| 测试缺口 | P2 | 删除按钮嵌套点击/二次确认**无自动化测试**（3 条测试只覆盖重绑）；建议补 1 条 EditMode（构造面板→点删除两次→断言文件消失、父加载未触发） |

**结论：PASS-with-caveat（可合入）。** 交互风险经代码推断为安全，但缺自动化覆盖。

## 5. task-34：Succession 缺陷修复（资格 / 绝嗣去重 / 幼主双记录 / 共和 scope）

| 检查 | 结果 | 证据 |
|---|---|---|
| 世袭资格过滤（P1-2） | **实现** | `SuccessionSystem.FilterByEligibility`（`:90-110`）：先性别 `rules.Filter`，`scope != ClanOnly` 时再 `IsScopeEligible(socialClass)`；`ExecuteSuccession:30` 对世袭/选举同样横切；`IsInInheritanceLine:135` 用同一规则 |
| 绝嗣去重（P1-1） | **实现** | `GameWorld.CheckRulerSuccessions:1512-1519`：`heirId<0 && HasRecordedSuccessionCrisis(当前君主id)` 直接跳过；`RecordSuccessionCrisis:1563-1582` 按 `participants` 含死君主 id 去重；`SuccessionSystem:33-43` 绝嗣只清 `heirId`、不清 `monarchId/consulId`（注释给出理由：避免 id>=0 查询方空引用） |
| 幼主双记录（P2-5） | **实现** | 先写 `"succession"` 即位（`:1545-1548`），再按 disputed 写 `"succession_crisis"`（`:1549-1552`）；`NotifyRulerTransition` 传新君 id（`:1553-1555`） |
| 共和 scope 资格（P2-4） | **实现** | 共和路径统一走 `FilterByEligibility` 后 `SelectByEligibility`（prestige 降序） |
| 测试判别性 | **强** | 11 条含：`Monarchy_EligibilityFilter...`（继承法 Equal 下若无横切会选 35 岁长女，断言必须选 20 岁儿子 → 旧实现必红）；`Republic_ScopeEligibility...`（威望更高的平民必须被 Nobility 排除）；`NoHeir_GameWorld_RecordsCrisisOncePerDeadRuler`（第二 tick 编年史条数不得增加，且断言 `participants` 含死君主 id）；`MinorHeir_GameWorld_WritesAccessionBeforeCrisis`（即位条目索引 < 危机条目，且 flags/accessionDay 正确） |
| **旧档兼容（重点）** | **风险不成立** | ⓐ "旧 `succession_crisis` 无 participants 会多记一次"：**Chronicle 未入档**（`SaveData`/`SaveSystem` 均无 chronicle 字段，已 grep 确认），当前读档后编年史为空重建 → 不存在旧 entries；legacy `GameSaveData.participants` 属已废弃的旧存档系统，当前加载路径不使用。ⓑ 旧档 `socialClass` 默认值：`CharacterSaveData.socialClass = Peasant`，而默认 `EligibilityRules.scope = FreePeople` 的 `ScopeToSocialClasses` **包含 Peasant**，默认 `gender = MalePreference` 不做性别过滤 → 旧档角色仍具资格；只有 `Slave` 被默认排除（符合设计）。ⓒ `RealmData.laws` 默认非空（`RealmData.cs:69`），旧档不会 NRE |
| 边角（P2） | 记录 | ① 绝嗣后 `monarchId` 保留死者：政权会停在"死君主"状态直到外部摄政/推举（注释为有意设计；UI/外交可能显示已故者）；② 去重依赖编年史条目：若条目被裁剪或外部重新设置 `heirId`，去重条件失效会再触发一次；③ GameWorld 级测试用反射调私有 `CheckRulerSuccessions` + 注入私有 `_characterManager/_chronicle`（`SuccessionEndToEndTests:405-425`），重命名即断（失败可见，属可接受但脆弱的脚手架） |

**结论：PASS（可合入）。** 旧档兼容三项担忧经代码与存档 schema 核对均**不成立/已安全**；P2 仅记录设计边界与脚手架脆弱性。

## 6. task-35：删 0 引用重复接口 + 悬空资产

| 检查 | 结果 |
|---|---|
| 重复接口 | 现 `Core.Contracts/ISimulationCommand.cs`（namespace `MyriadCreation.Core.Contracts`）被 `SimulationCommandBus/QueryBus` 正常使用；`Core.Simulation/` 目录已无 `ISimulationCommand/ISimulationQuery` 定义，全库 0 处 `Core.Simulation.ISimulation*` 引用（编译 0 error） |
| 悬空 `Assets/Data/DefaultWorldConfig.asset` | 已删；**关键**：该文件与 `ScriptableObjects/DefaultWorldConfig.asset` **共用同一 GUID**（`cf2df798…`），删除后全库重复 GUID = **0**，场景 `config` 引用唯一解析到保留资产 ✅ |
| 其它悬空资产（项目自有） | 扫描全部 `.unity/.prefab/.asset/.mat/.controller/.anim` 的 `m_Script`：**项目自有缺失 = 0**；仅剩 **26 处**全部位于 `Assets/TextMesh Pro/Examples & Extras/**`（TMP 官方示例资产引用 URP/HDRP shader GUID，包内示例，非项目代码）→ 与本任务声明一致 |

**结论：PASS（可合入）。**

## 7. Lead：场景 identifier 重写 + PlayMode 冒烟强化

| 检查 | 结果 |
|---|---|
| 6 条 `m_EditorClassIdentifier` | 已由 `MyriadCreation.Runtime::*` 改为 **实际程序集::实际类型**：`MyriadCreation.Rendering::…MapRenderer`、`MyriadCreation.Simulation::…GameWorld`、`MyriadCreation.Rendering::…Editing.RuntimeMapEditingController`、`MyriadCreation.UI::…Bootstrap/GameManager/Common.UIManager`（逐一与其源文件 namespace/类名一致 ✅）；场景内 `MyriadCreation.Runtime` 出现次数 = **0** |
| PlayMode TearDown | 已加：销毁 SceneFlowController/MainMenuUI/GameManager/GameWorld/MainMenuCanvas（`MainMenuUiPlayModeTests:24-38`），降低 DDOL 跨测试污染 |
| 射线断言强化 | 已从 `Count>0` 改为**命中链必须包含开始按钮或其子物体**（`:85-96`）→ 被遮挡/层级错误真红；`playmode-gfx.log` 命中 `开始游戏,Root` 且点击后 `MainMenu→Playing land=524287` |
| 结论 | **PASS** |

## 8. 范围与证据核对

- 22:46 后改动 **15 个 .cs/.asset/.unity**：`Editor/{AutoOpenMainScene,GraphicsApiFix,ResaveScene}`、`Scenes/Main.unity`、`ScriptableObjects/DefaultWorldConfig.asset`、`Rendering/Editing/RuntimeMapEditingController`、`Simulation/.../{SuccessionSystem,GameWorld,WorldMapSimulationSystem}`、`Tests/EditMode/{SeaLandRecalcDeterminismTests,SuccessionEndToEndTests,WorldRebindOnLoadTests}`、`Tests/PlayMode/MainMenuUiPlayModeTests`、`UI/{GameManager,Panels/SaveLoadPanel}`；另 tracked 删除 4 项（`Data/DefaultWorldConfig.asset(+meta)`、`Scripts/World/Terrain/SeaLandGenerator.cs(+meta)`）。
- 未发现 agent 越界修改（无其它 UI/Simulation/Save 文件被动）；未见调试残留。
- **未跟踪的临时 Editor 工具已累积 4 个**：`UiCleanup.cs`、`UiDiagnostics.cs`（round5）、`ResaveScene.cs`（本轮场景重写）、`BatchCompileCheck.cs`（08:49，历史）——均属一次性 batchmode 工具（部分会打开/保存场景并 `EditorApplication.Exit(0)`），**不应随分支提交**（P2）。

## 9. 问题清单（本轮无 P0/P1）

| 级别 | 问题 | 建议 |
|---|---|---|
| P2-1 | 破碎化在生产**永不触发**（无 `SetFragmentNoise` 生产者）→ task-32 seed 基建目前只对死路径生效 | 若为预期功能，单独立卡：由地形/特征系统产出 fragmentNoise 并接入；否则删除 `landFragment/coastFragment` 配置或标注未启用 |
| P2-2 | `randomSeed` 未入档 → 读档后重算 seed 恒 42，与原局不同（审计 A P1-2 同源） | 将 world seed 加入 SaveData |
| P2-3 | `EnsureSeaLandGeneratorSeed` 重建生成器后，`PlanetClimateSimulator._seaLand` 仍指旧实例 | 同步替换气候模拟器持有的引用，或让其从 WMS 取 |
| P2-4 | 场景重写工具 `ResaveScene.cs` 未跟踪且会保存场景+退出；连同 UiCleanup/UiDiagnostics/BatchCompileCheck 共 4 个临时工具 | 不提交或移出 Assets |
| P2-5 | 删除存档按钮嵌套点击/二次确认无自动化测试 | 补 1 条 EditMode 覆盖（含"点删除不应触发加载"） |
| P2-6 | 绝嗣保留死 `monarchId` 的"无主窗口"仅靠编年史去重表达；UI/外交可能显示已故君主 | 记录为设计决策，或后续摄政系统接管 |
| P2-7 | 去重依赖编年史条目存在：裁剪/外部改 `heirId` 会重新触发 | 可改为在 RealmData 记录 `lastCrisisDeadRulerId`（需入档） |
| P2-8 | Succession/GameWorld 测试脚手架用反射调私有成员与私有字段 | 可接受；若重命名会连锁失败，建议注释登记契约 |
| P2-9 | task-30 的"重复类型审计表"未落盘 | 补文档或明确为口头交付 |
| P2-10 | 额外 tracked 删除 `Scripts/World/Terrain/SeaLandGenerator.cs` 未在卡描述中登记 | 变更说明补登 |

## 10. Merge gate 结论

| 卡 | 结论 |
|---|---|
| **task-30（重复 SeaLand 删除 + 坏资产 + 命名空间）** | **可合入**（补审计表落盘/删除登记） |
| **task-31（Succession 端到端测试）** | **可合入** |
| **task-32（世界 seed 接线）** | **可合入**（"生产不触发"为既有死路径，P2 非回归；建议单独立卡决定破碎化去留） |
| **task-33（读档重绑 + 删除二次确认）** | **可合入**（建议补删除按钮自动化测试） |
| **task-34（Succession 修复）** | **可合入**（旧档兼容三项担忧均不成立；P2 设计边界已记录） |
| **task-35（重复接口 + 悬空资产）** | **可合入**（重复 GUID 清零、项目自有悬空 = 0） |
| **Lead（场景 identifier + PlayMode 强化）** | **可合入** |

**总体：376/376 + PlayMode 2/2×2 全绿，逐卡均无 P0/P1。**

## 11. 待验证

1. 未运行 Unity：破碎化接线、climate 旧引用、删除按钮真实输入下的命中链均未实机验证（代码推断/测试覆盖之外）。
2. `randomSeed` 未入档对"读档后重算结果"的实际影响未实测。
3. 未跟踪临时 Editor 工具若被误提交，会进入构建？——Editor asmdef 仅编辑器生效，不影响运行时构建，但会在编辑器菜单/批处理暴露破坏性入口。
4. task-30 审计表未落盘，无法核对"全项目重复类型"清单完整性。

## 附录：关键只读证据

```bash
grep -o 'testcasecount|total|passed|failed|skipped' Logs/editmode-results.xml   # 376/376/0/0
逐类：Succession 11 / Rebind 3 / SeaLandDeterminism 3 / WorldSeed 2 / Parity 0
grep -o 'total|passed|failed|start-time' Logs/playmode-results.xml / playmode-gfx-results.xml  # 2/2, 2/2
find Assets -name "SeaLandGenerator.cs"          # 仅 Terrain 版 1 个
grep -rn "SetFragmentNoise" Assets --include=*.cs # 仅测试调用 → 生产破碎化不触发
sed -n '1512,1599p' GameWorld.cs                  # 绝嗣去重/participants
sed -n '90,110p' SuccessionSystem.cs              # 资格横切
sed -n '288,388p;385,486p' SuccessionEndToEndTests.cs # 资格/去重/幼主判别断言
grep -n "socialClass|IsScopeEligible" EligibilityRules.cs # FreePeople 含 Peasant
python: 全库重复 GUID=0；项目自有资产 missing m_Script=0（仅 TMP 示例 26）
grep -n "m_EditorClassIdentifier: MyriadCreation" Main.unity # 6 条新程序集::类型
sed -n '24,38p;84,96p' MainMenuUiPlayModeTests.cs # TearDown + 严格射线断言
```
