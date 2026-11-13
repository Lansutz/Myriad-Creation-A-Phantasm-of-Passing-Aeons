# VERIFY-team-round4：复核第四轮（asmdef 去旧依赖 / bodyParts+bonds / 信仰入档）

- **复核者**：verifier（只读）
- **基线（复核时已落盘，均在最终代码之后）**：
  - 全量 EditMode：`Logs/editmode-results.xml` = **357/357 passed，0 failed，0 skipped**（`start 2026-10-31T14:04:06Z`，end `14:04:10Z`）
  - 定向：`Logs/team-round4.xml` = **46/46**（`14:03:40Z`；= SaveAtomic 10 + Econ 12 + Character 11 + Faith 4 + Restored 5 + Legacy 3 + RoundTrip 1，逐类计数已核）
  - PlayMode gfx：`Logs/playmode-gfx-results.xml` = **1/1 passed**（`14:04:28Z`），`playmode-gfx.log` 有真实渲染采样（非背景 12/64、unique 13、亮度 0.145~0.730）
  - 编译：`editmode-team4.log` 中 `error CS` = 0
  - 数量闭合：**343 + 14 = 357** ✅（14 = Faith 4 + Character 新增 5 + Restored 5）
- **时间序**：asmdef 21:56:30 → 代码 21:58–22:02 → 测试 21:55–22:03 → 定向 22:03:40 → 全量 22:04:06 → PlayMode 22:04:28（结果覆盖最终代码）
- **约束**：未改 `Assets/**`、未跑 Unity/编译/测试、git 只读、只写本文件

---

## 1. 逐项复核表

| # | 复核项 | 结论 | 证据 |
|---|---|---|---|
| 1 | **asmdef 去旧依赖（task-19）** | **PASS-with-caveat** | 全部 10 个 asmdef 用 Python `json` 解析**均为合法 JSON**；`references` 中 **0 个** 指向 `CivilizationEvolution.*`（唯一含该名字的是旧程序集自身 asmdef，其 references=[]）；其它必需引用完整（Infrastructure→Core/World/Simulation/Rendering；Editor/UI/Tests/PlayMode 的引用齐全），未见误删。4 个新程序集源码中 `CivilizationEvolution` 仅剩 1 处**注释**（`CharacterSaveData.cs:545`），无类型引用；全工程编译 0 error（旧程序集仍独立编译）；场景绑定走 `.meta` GUID、与 asmdef references 无关，故不受影响。⚠️ **证据局限**：git diff 显示 tracked 的 Infrastructure/Rendering 两个 asmdef **净内容变化仅"文件末尾换行"**（`-}`+no newline → `+}`），即本工作区相对 HEAD 无引用行变化；Simulation/World 两个为未跟踪文件，无法用 git 证明"删除前含旧引用"。当前**最终状态合规**，但"本轮删 4 处引用"只能部分归因 |
| 2 | **bodyParts 三层拍平（task-20）** | **PASS** | 实际 **20 个并行数组**（任务描述写 19）：部件 7 + 子结构 6 + 诊断条件 7（`CharacterSaveData.cs:141-165`）。捕获：`CaptureBodyParts`（`:369-451`）按部件类型键写入、子结构/条件行记录所属部件下标；还原：`RestoreBodyParts`（`:638-715`）先 `BodyPartSystem.InitializeBodyParts()` 建默认表，再按类型 overlay（`:651-656`）→ 子结构按 type 匹配已有项（`:682-689`，不重复追加）、条件仅对存档中存在的行 `Add`（`:695-712`），未知 partType 跳过保留默认（`:651`）。**越界安全**：部件级用 `MinArrayLength(5 个标量数组)`（`:645-646`），子结构/条件的 `partIndex` 均做 `0<=idx<partCount` 检查（`:676, :698`）；计数数组不参与裁剪但仅自描述。**部分段**：`bodyPartTypes` 空→整体走默认（`ToCharacter:529`），部分缺失→缺的部件/子结构保留默认（`:650, :688`）。**不会重复叠加**：每次 `ToCharacter` 都重新 `InitializeBodyParts`，且诊断条件默认列表为空 |
| 3 | **bonds 捕获/恢复（task-20）** | **PASS** | DTO `CharacterBondSaveData`（`CharacterSaveData.cs:738-786`）覆盖 `CharacterBond` 全部 10 字段；`CharacterBondPersistence`（`SaveEntityBridge.cs:169-203`）走 `CaptureBonds/RestoreBonds` 公有 API。接线：捕获在 `CapturePersistentEntities`（`SaveSystem.cs:521-523`）；恢复在 `InstallCharacters/InstallFamilies` **之后**、realmOffices 之前（`SaveSystem.cs:558-560`）✅。无效 bondId（<=0）跳过、两端角色不存在跳过并 `LogWarning`（`CharacterManager.cs:464-469`）；`_nextBondId = maxId + 1`（`:475`）✅。测试 `Bonds_RoundTripAndNextIdContinuation` 断言新纽带 id = max+1、`Bonds_MissingCharactersSkipped_NoThrow` 断言跳过不抛 |
| 4 | **faith 运行态入档（task-21）** | **PASS** | `FaithSaveData`（内容定义不入档）+ `FaithPersistence`（`Capture` 按 faithId 升序 `:18-36`；`Restore` 按 faithId overlay `:42-71`）；接线：捕获 `SaveSystem.cs:392`、恢复在 `PrepareLoadedWorld()` **之后**（`:480-486`）✅。**不覆盖内容定义**：`ApplyTo` 只写运行态，`faithName/virtues/sins` 一律不碰（`FaithSaveData.cs:121-179`，测试 `FaithPersistenceTests:65` 断言 `faithName` 等于内容重建值）。**未知 faithId 跳过**并 `LogWarning`（`FaithPersistence.cs:54-60`，测试 `:170` + 精确 Warning 契约）。**旧档缺段**：`saved==null/空` 直接 return，保持内容默认（`FaithPersistence.cs:44-45`；测试 `:137` 逐字段断言 fervor=50/大祭司=-1/教阶 0/各集合空）。数组 overlay 全用 `Math.Min` 容忍长度不一致（`FaithSaveData.cs:151,161,173`） |
| 5 | **SaveVersion 15 / 共享常量旁路影响** | **PASS** | `GameConstants.cs:13 SaveVersion = 15`；新语义（faiths/bonds/bodyParts）均随 15 引入，带初始值的可选段（`SaveData.cs:67,70`），v14 及更早旧档反序列化为 null → 默认/跳过，属兼容升级 ✅。旁路：`GameSaveData.cs:22`、`MapSaveData.cs:11` 仅用常量做**写入初始值**，其加载路径无版本比较（grep 无 `version` 判定），且 B 报告已认定这两套为死路径 → 不产生运行时回归；`Scripts/Legacy/Core/SaveSystem.cs:68` 版本比较仅 Warning 后继续，不影响 |
| 6 | **新增 14 条测试真能失败** | **PASS-with-caveat** | Faith 4：往返（不修则 fervor 等回 50→红）、旧档默认（避免回归成"读档失败/清空"）、未知 id 精确 Warning+跳过（不修则无 Warning→红）、DTO JsonUtility 并行数组往返；Character 5：身体部位三层往返（含子结构条件/造成因素）、旧 JSON 无 bodyParts→默认、部分段保留默认、bond 往返+nextId、缺失角色跳过；Restored 5：3 条 FaithSystem 规则（clamp/大圣战门槛/教阶头衔）+ 2 条地形确定性。均含具体数值/集合断言，未见恒真断言。⚠️ P2：未知 faith 用精确 Warning 文案（措辞一变即红） |
| 7 | **地形确定性测试是否受 `UnityEngine.Random` 全局状态影响** | **PASS** | `GenerateTerrain` 路径走 `new PlanetTerrainGenerator(seed)` / `new TerrainGenerator(seed)`（`GameWorld.cs:1639-1666`），两者用 `SphericalNoise(seed)` / `SphericalVoronoi(_seed+1000)` / `HydraulicErosion(_seed+2000)` / 哈希噪声（`PlanetTerrainGenerator.cs:42-44,82,177`；`TerrainGenerator.cs:14-25`）→ 完全种子化；GameWorld 生成路径中 **0 处 `Random.`**（grep 验证）。因此同 seed 两个世界逐地块一致、不同 seed 不同（`RestoredSystemTests.cs:80-113`）。该测试对"引入全局 RNG"是**敏感**的（若生成消耗全局 `UnityEngine.Random`，顺序生成的 A/B 世界必然不同→红），非假绿；`DifferentSeed` 断言防止"seed 被忽略"的假绿。⚠️ 但 `SeaLandGenerator.RecalculateAll/RecalculateDirty`（`Scripts/World/MyriadCreation.World.Terrain/SeaLandGenerator.cs:174,198,205`）仍用**未播种**的 `UnityEngine.Random`，且默认 `landFragment=0.4 / coastFragment=0.3`（`WorldConfig.cs:24,27`）→ **地图编辑器"全量重算"路径结果不可复现**，该测试未覆盖（P2，既有问题） |
| 8 | 范围检查（只改授权文件） | **PASS** | 19:55 后共 **20 个文件**：4 asmdef、`GameConstants`、`CharacterSaveData`、`CharacterManager`、`FaithSaveData(+meta)`、`FaithPersistence(+meta)`、`SaveData`、`SaveEntityBridge`、`SaveSystem`、`GameWorld`、`CharacterPersistenceTests`、`FaithPersistenceTests(+meta)`、`RestoredSystemTests(+meta)`；无场景/asset/其它脚本改动；改动文件内无 `TODO/FIXME/D:/`/调试残留 |
| 9 | 基线计数闭合与 XML/日志一致 | **PASS** | 全量 357 = 343 + 14；逐类计数：SaveAtomic 10、Econ 12、Character 11、Faith 4、Restored 5、Legacy 3、RoundTrip 1 = 46 定向；无 Failed/Inconclusive/Skipped；PlayMode gfx 1/1 且 log 有真实采样 |

---

## 2. 问题清单（本轮无 P0/P1）

| 级别 | 问题 | 位置/建议 |
|---|---|---|
| P2-1 | asmdef 归因证据不足：tracked 两个（Infrastructure/Rendering）相对 HEAD 净变化仅换行；Simulation/World 未跟踪 → "本轮删除旧引用"无法用 git 完整证明（当前最终状态已验证合规） | 建议后续把 asmdef/测试等关键文件纳入提交或保留变更摘要 |
| P2-2 | bodyParts 并行数组实际 **20** 个（任务描述 19）；`bodyPartSubStructureCounts`/`bodyPartDiagnosedConditionCounts` 被捕获但还原未使用（仅自描述），计数与行数不一致时不报错 | `CharacterSaveData.cs:147-149,645-646`；可在还原时做一致性校验并 LogWarning |
| P2-3 | `CaptureBodyParts` 未判空 `part`（`BodyPartData` 是 class，字典值为 null 时 NRE；`SubStructureData`/`DiagnosedCondition` 为 struct 无此问题） | `CharacterSaveData.cs:400-405` |
| P2-4 | 地形**重算**路径仍用未播种 `UnityEngine.Random`（碎片化），默认配置即启用 → 编辑器全量重算不可复现；新确定性测试只覆盖 `GenerateTerrain` | `World/MyriadCreation.World.Terrain/SeaLandGenerator.cs:174,198,205`；建议重算也接受 seed 或改用 seeded RNG |
| P2-5 | 未知 faithId 测试依赖精确 Warning 文案 | `FaithPersistenceTests.cs:170-190`；建议共享常量/正则 |
| P2-6 | `RestoredSystemTests` TearDown 只销毁 GameObject，不清理静态注册表；当前 Standard 生成不产地图单位/关系，风险低 | `RestoredSystemTests.cs:20-27` |
| P2-7 | SaveVersion 15 前向兼容：新档被旧客户端读取会静默丢 bodyParts/faith/bonds 运行态（非本作要求，记录） | `GameConstants.cs:13` |
| P2-8 | 每角色 20 个数组 + 教会/纽带段，长局存档体积/序列化耗时未评估（沿用上轮 P2） | `CharacterSaveData.cs:141-165`、`FaithSaveData.cs` |

---

## 3. Merge gate 结论

| 卡 | 结论 | 说明 |
|---|---|---|
| **task-19（asmdef 去旧依赖）** | **可合入** | 当前状态：0 个 asmdef/代码引用旧程序集、JSON 合法、引用无缺失、全工程编译 0 error；tracked 文件的 git 净变化仅换行（归因见 P2-1，不阻塞） |
| **task-20（bodyParts + bonds）** | **可合入** | 三层拍平索引安全、无重复叠加、旧档/部分段保留默认；bonds 恢复顺序正确、跳过无效项、`_nextBondId` 续接；10 条测试覆盖（Character 11 条中 5 条为新增） |
| **task-21（信仰运行态 SaveVersion 15）** | **可合入** | overlay 不碰内容定义、未知 faithId 跳过、旧档缺段保持默认、接线顺序正确；4 条测试覆盖 |
| **Lead RestoredSystemTests** | **可合入** | 3 条宗教规则 + 2 条地形确定性；确定性测试经种子链验证**不依赖 UnityEngine.Random 全局状态**，是有效的敏感用例 |

**总体：本轮无 P0/P1；task-19/20/21 与 Lead 测试均可合入。** 上一轮 P2 中"角色运行态（bodyParts）/纽带/信仰"三项缺口已闭环。

---

## 4. 待验证

1. 未运行 Unity：asmdef 删除的"改动前状态"只能从当前结果与编译推断（P2-1）；未做场景实机加载验证（PlayMode gfx 1/1 间接证明场景可加载）。
2. "GenerateTerrain 无全局 RNG"由 grep + 测试双证，但未穷举所有 `SetupOptions` 预设/`GenerateTerrainWithConfig` 分支；测试仅用 Standard。
3. bodyParts 与"内容新增/删除部件类型"的跨版本恢复只做了逻辑审读与部分段测试，未构造真实内容差异档。
4. 重算路径（P2-4）的不确定性未做实验验证（需在编辑器触发 RecalculateAll 两次比较）。

## 附录：关键只读证据

```bash
python -c "json 解析全部 asmdef + 打印 references"                     # 10/10 合法，0 个引旧程序集
grep -rn "CivilizationEvolution" Scripts/{Simulation,World,Infrastructure,Rendering}  # 仅 1 处注释
grep -o 'testcasecount|total|passed|failed|skipped' Logs/editmode-results.xml          # 357/357/0/0
逐类计数：10+12+11+4+5+3+1=46（team-round4.xml）
sed -n '141,165p;369,451p;638,715p' CharacterSaveData.cs                 # bodyParts 20 数组/捕获/还原
sed -n '448,476p' CharacterManager.cs                                    # RestoreBonds + _nextBondId
sed -n '169,203p' SaveEntityBridge.cs                                    # CharacterBondPersistence
sed -n '42,71p' FaithPersistence.cs；'121,179p' FaithSaveData.cs          # 信仰 overlay/跳过/容忍
sed -n '1639,1666p' GameWorld.cs + PlanetTerrainGenerator.cs(42-44,82,177) # 种子链
grep -n "Random\." GameWorld.cs                                          # 0（生成路径无全局 RNG）
find Assets -newermt "2026-10-31 19:55" -type f                          # 范围=20 个授权文件
```
