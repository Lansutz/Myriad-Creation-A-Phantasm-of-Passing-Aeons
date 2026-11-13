# 审计 D：测试有效性与回归风险

- **项目**：Myriad Creation（Unity 6 文明模拟），`MyriadCreation/` 子目录
- **分支/基线**：`architecture-2-0-refactor`，工作区有大量未提交改动
- **审计方式**：只读。只使用 grep / 文件读取 / 只读 git（status/diff/log/show）；未运行 Unity、未编译、未跑测试、未修改 `Assets/**`
- **快照时间**：2026-10-31 17:37–17:43（Asia/Shanghai）。注意：审计期间有队友在同工作区并发新增测试（见 P0-1、P2-4），结论以本时间点为准
- **测试结果文件**：`MyriadCreation/Logs/editmode-results.xml`，`testcasecount="298" passed="298" failed="0"`，`start-time="2026-10-31 09:31:52Z"`（=17:31:52 本地）
- **审计文件量**：56 个测试 .cs（47 未跟踪 + 9 修改），约 1,199 行断言语句；精读/抽样 ~30 个文件

---

## 一、结论摘要

1. **298/298 的"绿"来自一个尚未提交的新测试套件，同时工作区删除了 278 条旧测试。** 当前 `Assets/Tests/EditMode/` 56 个文件中 47 个是未跟踪新文件，32 个旧测试文件（共 278 个 `[Test]`）在工作区被删除且未提交（`git diff --numstat`：新增 56 行 / 删除 6471 行）。仓库里 `HEAD` 的测试状态 ≠ 当前的 298/298，别人 checkout 这个分支无法复现这套绿。
2. **SaveGame→LoadGame 全链路在 298 基线中覆盖为 0。** 17:31 的结果文件里没有任何测试调用 `SaveSystem.SaveGame/LoadGame`；当时的测试只覆盖 DTO `JsonUtility` 往返与 `Capture*/Restore*` 辅助函数。链路中的顺序敏感逻辑（重建子系统 → 就地填充省份/聚落 → 军队与 ID 计数器 → 实体恢复 → 地图视图重登记）完全未被验证。
3. **审计期间（17:41）队友并发新增了 `WorldGenerationSmokeTests`，补上了全链路冒烟，但仍只做"数量级"断言**：省份/聚落/军队/角色只比较 Count，v13 的"读档后自增 ID 续接"、聚落内容字段、营地/市场/区划、actor 恢复均未断言。P0 风险未完全关闭。
4. **删除的测试中有仍在活代码路径上的系统**：`TerrainGenerator`（新 `GameWorld` 生成地形时调用）、`SuccessionSystem`（新 `GameWorld` 继承时调用）、`ReligionSimulationEventHandler` / `DiplomacyManager`（新模块仍在）都失去了对应测试文件，当前套件没有等价替代。
5. **确实存在一处"为了绿而放宽断言"的痕迹（有 git diff 实证）**：`AdminDivisionUiTextTests` 把"治理头衔：title_gov_general"的具体断言改成了只看前缀 `"治理头衔："`。其余修改文件只是 API 重命名/using 调整，没有发现第二处被改弱的断言。
6. **未发现常见的"假绿"手段**：全套 0 个 `[Ignore]`/`[Explicit]`/`Assert.Pass`/`Assert.Inconclusive`；0 个 try/catch 吞断言；0 个 `Assert.IsTrue(true)` 型恒真；遗传学测试全部使用固定种子（`new Random(7)/(11)/(3)` 等）。这部分是干净的。
7. **`new GameWorld()`（Unity fake-null）仍有 2 处**：`EntityRelationAndLivingGoodsTests.cs:293`、`UnitFoundingTests.cs:58`。两处都靠反射调用私有 `InitializeGoodsDefs`，目前能跑通，但对象 `== null` 为 true、`Awake` 未执行，任何被调代码以后加判空就会静默跳过。
8. **静态注册表存在跨测试泄漏与执行顺序依赖**：`RelationProjectionTests.cs:50` 断言全局 `RelationRegistry` 为空，但该 fixture 没有 `SetUp`/`TearDown` 清理；而新的 `SaveSystem.LoadGame` 会全局 `Restore` Relation/Contract/DynamicGoods 并重登记 MapUnit——全链路测试一旦没有严格清理，会污染后续测试（并发新增的 smoke 已清 4 个注册表，但漏了 `DynamicGoodsRegistry` 与 `ContentRegistry.Reset`）。
9. **同时存在两套存档实现**：活路径是 `SaveSystem`（`GameManager.cs:123,131` 调用），`GameSaveSystem` / `MapSaveSystem` 在运行时无任何引用（死代码）。但测试唯一涉及的旧档迁移 `SaveJsonMigration` 恰恰只被死路径使用（`HabitationSiteTests.cs:239`），活路径 `SaveSystem` 自己的版本兼容/迁移分支（`SaveSystem.cs:79-87`）无测试。
10. **覆盖薄的公开 API 已成清单**：`DecreePropagationSystem.AttachCourier/CourierArrived`、`SettlementInfluenceSystem.GetTierAt`、`GeneticsEngine.CreateFounder/ExpectedHeterozygosity`、`UnitProvisioning.TryFund` 的 4 个重载与 `Equip` 均 0 引用（见 P2-1）。

---

## 二、P0（阻塞级：绿不代表可靠）

### P0-1 工作区测试套件被整体重写：47 个新文件未跟踪 + 32 个旧文件（278 条测试）被删

**证据**

- `git status --short -- MyriadCreation/Assets/Tests/EditMode/`：`47 ??`（新测试）、`9 M`（修改）、`32 D`（删除）。
- `git diff --numstat -- MyriadCreation/Assets/Tests/`：`added 56 / deleted 6471`。
- 被删的 32 个 EditMode 测试文件，按 `git show HEAD:<file> | grep -c "\[Test\]"` 统计共 **278 条测试**，包括体量最大的几条：
  - `ReligionSystemTests.cs` 27 条、`CharacterSystemTests.cs` 26 条、`InnovationTreeTests.cs` 22 条、`GovernmentCompositionTests.cs` 19 条、`DnaSystemTests.cs` 16 条、`InheritanceLawTests.cs` 15 条、`ContentRegistryTests.cs` 14 条、`SocialInnovationLinkTests.cs` 13 条、`PersonalityArchetypeTests.cs` 12 条。
- 当前 298 条（17:31 XML）+ 17:41 并发新增 2 条 = 当前源文件 288 个 `[Test]` + 12 个 `[TestCase]` = 300 条，**全部未在 298/298 那次跑过**。

**为什么有风险**：这不是"删掉无关失败"，而是整代测试被替换。绿基线不可用 git 复现；回归保护在替换过程中可能被静默丢弃；review 时 `git diff` 里 6471 行删除很难逐条判断哪些是合法随架构 2.0 拆分下线、哪些是"删测试求绿"。

**建议**（本次审计不改文件）：把"删除 32 个测试文件 / 278 条测试"拆成独立提交并逐个说明去向（迁移到新 fixture、或对应实现已删除）；对仍在活代码路径上的系统（见 P0-3）先补最小替代测试再删。

### P0-2 SaveGame→LoadGame 全链路：298 基线 0 覆盖，链路核心逻辑无断言

**证据（基线状态）**

- `MyriadCreation/Logs/editmode-results.xml`：`testcasecount="298" passed="298"`，`start-time="2026-10-31 09:31:52Z"`。
- 17:37 对 `Assets/Tests/EditMode/` 做 `grep -rn "SaveSystem"` 结果为空（17:41 才出现并发新增的 `WorldGenerationSmokeTests.cs`）。当时的 4 个持久化测试文件全部只做 DTO/辅助函数级验证：
  - `SettlementPersistenceTests.cs:141-146`：`JsonUtility.ToJson(new SaveData{...})` → `FromJson`，再手动 `SettlementPersistence.RestoreSites`；
  - `ArmyPersistenceTests.cs:91-96`：同样 DTO 往返 + 手动 `ArmyPersistence.RestoreArmies(target, ...)`；
  - `AdminDivisionPersistenceTests.cs:62-67`：`RealmDTO` 往返；
  - `SettlementBlockSaveTests.cs` / `SettlementFoundingAndMarketSaveTests.cs`：`Capture*`/`Restore*` 单函数往返。
- 活链路的顺序敏感逻辑 100% 未被覆盖，位于 `MyriadCreation/Assets/Scripts/Infrastructure/Save/SaveSystem.cs`：
  - `173-244` `ApplyToWorld`：`world.ReinitializeSubsystems()` → `SettlementPersistence.RestoreProvinces/RestoreSites`（226-227）→ `ArmyPersistence.RestoreArmies` + `AdoptRuntimeIdCounters`（231-236）→ `RestorePersistentEntities`（240）→ `ArmyPersistence.RegisterArmyViews`（243）。注释自己写明"顺序不能反"（224-225、238-239、242），但没有任何测试验证这个约束。
  - `249-278` / `281+` 角色/家族/官职/地图单位/角色知识捕获与恢复。
  - `79-87` 版本不一致的兼容分支。

**并发变化**：17:41 出现 `MyriadCreation/Assets/Tests/EditMode/WorldGenerationSmokeTests.cs`（未跟踪，未跑）；`SaveLoad_FullChain_RestoresTilesProvincesSettlementsRealms`（73-100）已能触发 `SaveSystem.SaveGame`/`LoadGame`，但断言仅 `Snapshot` 计数比较（tiles/land/provinces/settlements/realms/characters/armies，87-93）+ 聚落锚点存在性（96-99）。下列关键点仍无断言：军队字段（士气/补给/行军路径）、自增 ID 续接、营地/市场/城区/城墙、MapActor/商队、CharacterKnowledge、关系/契约注册表、旧档版本迁移。

**为什么有风险**：v12（省份/聚落/营地）与 v13（军队、ID 续接）是本轮新增核心功能，恰好放在最易受"初始化顺序/子系统重建"影响的存档链路上；DTO 往返全绿与全链路正确之间没有推导关系。

**建议**：见第六节"SaveGame→LoadGame 最小补测方案"。

### P0-3 被删测试对应系统仍在活代码路径（覆盖回归）

**证据**

- `TerrainGenerator`：活路径 `MyriadCreation/Assets/Scripts/Simulation/MyriadCreation.Simulation.WorldState/GameWorld.cs:1622`、`1666`、`1732` 调用 `new TerrainGenerator(seed).TrackRivers(tiles)`；而 `TerrainGeneratorTests.cs`（4 条测试）被删，当前无替代。
- `SuccessionSystem`：活路径 `GameWorld.cs:1448` 调用 `SuccessionSystem.ExecuteSuccession(...)`；`SuccessionSystemTests.cs`（6 条）被删。
- `ReligionSimulationEventHandler` / `ReligionCatalog` / `ReligionPanelText` 仍存在于 `Scripts/Simulation/MyriadCreation.Simulation.Systems|Definitions|Presentation/`；`ReligionSystemTests.cs`（27 条）、`ReligionMapTests.cs`（6 条）被删，当前无宗教专项 fixture。
- `DiplomacyManager` / `DiplomacyRelationBridge` / `DiplomacySchedule` 仍在 `Scripts/Simulation/MyriadCreation.Simulation.Systems/`；`DiplomacySystemTests.cs`（7 条）被删，当前无外交专项 fixture。
- 被删测试引用的命名空间能确认已随架构拆分消失的有 `MyriadCreation.Simulation.Characters` / `.Religion` / `.Culture` / `.Society` / `.Population`（0 处声明），这部分删除有架构依据；但上述 TerrainGenerator/SuccessionSystem/Religion/Diplomacy 都仍有实现文件。

**为什么有风险**：新架构把模块拆分后，旧测试不能直接编译，但"删旧测试 → 不补新测试"会让这些系统进入无保护状态；尤其 `TerrainGenerator.TrackRivers` 直接决定河网/地形，`ExecuteSuccession` 直接决定政权延续。

**建议**：按上表逐项补最小 smoke 测试；若某项确认是死代码，应在实现侧删除而不是留"有代码无测试"。

---

## 三、P1（高：具体的假绿/污染机制）

### P1-1 断言放宽实证：治理头衔只断言前缀

**证据**（`git diff` 对比 HEAD）

```
-            Assert.IsTrue(text.Contains("治理头衔：title_gov_general"), "治理头衔");
+            Assert.IsTrue(text.Contains("治理头衔："), "治理头衔显示人类可读名");
+            Assert.IsFalse(text.Contains("title_gov_general"), "不得泄漏内部头衔键");
```

- 文件：`MyriadCreation/Assets/Tests/EditMode/AdminDivisionUiTextTests.cs:58-59`
- 影响：现在只要输出包含 `"治理头衔："` 且不含内部键就通过；如果 `RealmDivisionText` 的头衔名解析回归为空串（例如 "治理头衔：\n"），测试仍绿。旧断言至少锁定了 `title_gov_general` 的存在，新断言把具体值验证让掉了。
- 建议：断言具体人类可读头衔名（如 `text.Contains("治理头衔：郡守")`，实际名从 `TitleCatalog` 取），保留"不含内部键"的负断言。

> 说明：对 9 个 tracked 修改文件做全量 `git diff | grep "^-.*Assert"`，除本条外删除的断言行都来自被删文件（见 P0-1），未发现第二处针对同一文件的断言放宽。API 重命名类修改（`ArchitectureContractsTests.cs` 的 `Tick(30,1)`→`Tick(210,1,1)`、`result.success`→`result.Success`）、`HabitationSiteTests.cs` 改走 `SaveJsonMigration.FromGameSaveJson` 属于跟随实现，合理。

### P1-2 `new GameWorld()`（Unity fake-null）仍有 2 处

**证据**

- `MyriadCreation/Assets/Tests/EditMode/EntityRelationAndLivingGoodsTests.cs:293`：`var world = new GameWorld();` 然后反射调用私有 `InitializeGoodsDefs`（294-295），断言 `world.goodsDefs[...]`。
- `MyriadCreation/Assets/Tests/EditMode/UnitFoundingTests.cs:58`：同样 `new GameWorld()` + 反射 `InitializeGoodsDefs`（59-62）。
- 对照：同套件其它 3 个文件已明确避免该反模式并写了注释——`SettlementPersistenceTests.cs:32-36`（"必须用 AddComponent：new GameWorld() 是 Unity fake-null"）、`ArmyPersistenceTests.cs:34-38`、`InnovationSystemTests.cs:73-78`。

**为什么有风险**：`GameWorld : MonoBehaviour`（`GameWorld.cs:43`），`new GameWorld()` 得到的引用 `== null` 为 true 且不会执行 `Awake`。当前两处只调私有初始化函数、函数内无判空，所以"碰巧"能跑；一旦 `InitializeGoodsDefs`/`AddGoodsDef` 未来加上 `world == null` 判空或依赖 `Awake` 初始化的子系统，测试会静默变成"没测到任何东西"或抛异常。此外反射调用私有方法使测试与实现内部结构强耦合。

**建议**：改为 `new GameObject(...).AddComponent<GameWorld>()` 并在 TearDown 中 `DestroyImmediate`（照抄 `SettlementPersistenceTests` 的模式）；或把 `InitializeGoodsDefs` 抽成纯静态函数后直接测静态函数。

### P1-3 静态注册表跨测试泄漏 + 执行顺序依赖

**证据**

- `MyriadCreation/Assets/Tests/EditMode/RelationProjectionTests.cs:50`：`Assert.AreEqual(0, RelationRegistry.Capture().Count, "投影不写注册表");` ——该 fixture 无 `[SetUp]`/`[TearDown]`（grep 确认），断言依赖"运行到此之前全局注册表恰好为空"。
- 全局注册表清单与清理现状（只读 grep）：
  - `MapUnitRegistry`：`ArmyPersistenceTests.cs:20,26`、`MapUnitTests.cs:15-16` 有 `Clear()`，OK；但 `SaveSystem.ApplyToWorld` 结尾 `ArmyPersistence.RegisterArmyViews(world)`（`SaveSystem.cs:243`）会全局写注册表。
  - `RelationRegistry` / `ContractRegistry`：仅 `EntityRelationAndLivingGoodsTests.cs:15-21` 有 TearDown 清理；而 `SaveSystem.ApplyToWorld` 会 `RelationRegistry.Restore` / `ContractRegistry.Restore`（`SaveSystem.cs:201-202`）。
  - `CharacterKnowledgeRegistry`：`CharacterLocationTests.cs:11` 有 SetUp 清理；`SaveSystem` 也会恢复角色知识（`CapturePersistentEntities` 277）。
  - `DynamicGoodsRegistry`：`DynamicGoodsTests.cs:19-28` 有 SetUp/TearDown；`SaveSystem` 会 `Restore`（`SaveSystem.cs:200`）。
  - `ContentRegistry`：`static ContentRegistry() { Initialize(); }`（`MyriadCreation.Simulation.Modding/ContentRegistry.cs:69`）保证惰性初始化、顺序无关，这点是安全的；但 `ArchitectureContractsTests.cs:91-92`、`DiscoveryTests.cs:19-20` 会 `Reset()+Initialize()` 重置全局，任何未来"注册自定义内容"的测试都会被它清掉。
- 17:41 并发新增的 `WorldGenerationSmokeTests.cs:26-39` TearDown 清了 `MapUnitRegistry/CharacterKnowledgeRegistry/RelationRegistry/ContractRegistry` + `SaveSystem.DeleteSave`，但**没有** `DynamicGoodsRegistry.Clear()`、没有 `ContentRegistry.Reset()`；`SaveSystem.LoadGame` 会写这两个全局单例。

**为什么有风险**：NUnit 同装配体内的 fixture 执行顺序没有强保证；一旦"全链路存档测试"排在 `RelationProjectionTests` 前面（或未来加了注册自定义内容的测试），失败会表现为难以定位的串行污染，而不是清楚的断言失败。

**建议**：给 `RelationProjectionTests` 加 `[SetUp]` 清 `RelationRegistry`/`ContractRegistry`；给所有触发 `SaveSystem.LoadGame` 的测试统一加 TearDown 清 6 个注册表 + `DynamicGoodsRegistry.Clear()` + `ContentRegistry.Reset()+Initialize()`；或用基类 fixture 统一管理。

### P1-4 弱断言 / 相对断言 / 恒真断言清单

| 文件:行 | 断言 | 问题 | 最小改法（建议，不改） |
|---|---|---|---|
| `SettlementPersistenceTests.cs:250-251` | `Assert.IsTrue(reloaded.provinces == null || reloaded.provinces.Count == 0)` | "null 或空"二选一，两种状态恰好都通过；无法区分"旧档正确视为空"与"解析异常返回默认对象" | 先断言 `Assert.IsNotNull(reloaded)`，再只允许一种确定行为（实测 JsonUtility 对缺失 List 给空列表，应断言 `IsNotNull` + `Count==0`） |
| `SettlementInfluenceTests.cs:85` | `Assert.AreEqual((int)InfluenceTier.None, 0)` | 断言枚举值等于字面量 0，属契约性常量检查，不是被测行为；恒真（除非有人改枚举） | 删除或挪到枚举契约测试，改为断言 `GetTierAt(0)==InfluenceTier.None` |
| `DecreePropagationTests.cs:38` | `Assert.GreaterOrEqual(orders[0].totalTravelDays, 1)` | 只验证"至少 1 天"，没验证 `TravelDays` 公式/距离差异；瞬移 bug 若变成"固定 1 天"也绿 | 对已知地图宽度断言精确 `totalTravelDays`（`TravelDays` 是确定性函数，`DecreePropagationSystem.cs:96`），或对比近/远两个区划的天数关系 |
| `DecreePropagationTests.cs`（Hierarchy, 18-30） | 只有 2 层区划、1 个 child | 多级递归传递、同层多子节点、无 holder 分支未覆盖 | 加 3 层树 + 2 个同级子节点用例 |
| `SettlementInfluenceTests.cs:82-84` | `GreaterOrEqual(...Contested)` + `Greater(flat, hill)` | 只用相对关系，没有精确档位/预算上限；`InfluenceSettings.maxBudget` 未被断言 | 断言具体 tier 值与 `maxBudget` 限制 |
| `WorldGenerationSmokeTests.cs:87-93`（并发新增） | `Assert.AreEqual(before.X, after.X)` 全是 Count | 数量相等但内容可丢失（如聚落字段全默认、军队士气归零、ID 计数器未续接） | 见第六节字段级断言 |
| `AdminDivisionUiTextTests.cs:58` | `Contains("治理头衔：")` | 见 P1-1 | 断言具体头衔名 |

补充：`Assert.IsNotNull` 单独出现 44 次，但绝大多数后面紧跟具体值断言（如 `ArmyPersistenceTests.cs:93-119`、`HabitationSiteTests.cs:240-260`、`DiscoveryTests.cs:46-71`），属"合理的前置非空"。真正"只断言非空"的候选只有 `LawAndDecreeTests.cs:59,91`（`Assert.IsNotNull(active, reason)`），待验证其后是否还有字段断言。

### P1-5 双存档系统并存：测试覆盖的是死路径的迁移逻辑

**证据**

- 活路径：`MyriadCreation/Assets/Scripts/Legacy/Core/GameManager.cs:123` `SaveSystem.SaveGame(world, saveName)`、`:131` `SaveSystem.LoadGame(saveName)`（`SaveSystem` 即 `Infrastructure/Save/SaveSystem.cs`）。
- `GameSaveSystem`：`grep -rn "GameSaveSystem" Scripts` 除自身文件外只在 `SettlementPersistence.cs:18` 的一句注释中出现 → 运行时零引用（死代码）。
- `MapSaveSystem`：除自身文件外零引用。
- 但测试唯一的旧档迁移用例 `HabitationSiteTests.cs:238-239` 用的是 `SaveJsonMigration.FromGameSaveJson(legacyJson)`，而 `SaveJsonMigration` 只被 `GameSaveSystem.cs:335`、`MapSaveSystem.cs:244` 调用 → **测试覆盖的是死路径的迁移函数，活的 `SaveSystem` 的旧档路径没有测试**。
- `SaveSystem.cs:79-87` 版本不一致只 `LogWarning` 后"尝试兼容加载"；`72-77` 对 `tiles == null` 直接失败。没有任何测试覆盖这两个分支。

**为什么有风险**：以后若把 `GameManager` 切到 `GameSaveSystem`（它看起来是"更完整的新实现"），当前测试会给出虚假安全感：DTO/迁移测试全绿，但活链路换了一条完全没测过的实现。

**建议**：明确哪一套是唯一存档实现并在实现侧删除另一套；在 `SaveSystem` 上补版本不一致/旧档（v11 缺字段、v12 缺军队）加载用例。

### P1-6 存档链路测试的磁盘/路径依赖与清理风险

**证据**

- `SaveSystem.cs:33` `private static string SaveDirectory => Path.Combine(Application.persistentDataPath, "Saves");` —— 路径不可注入。
- 并发新增的 `WorldGenerationSmokeTests.cs:23` 用固定存档名 `"__smoke_saveload_test"` 写真实 `persistentDataPath/Saves`，只在 `TearDown`（38）删除；若测试进程崩溃/被中断，磁盘残留；并行跑两个 fixture 用同名会互踩。
- 测试中另一处平台路径假设：`LanguageFamilyTests.cs:41` `Application.streamingAssetsPath`（读 StreamingAssets 语言数据）。

**建议**：给 `SaveSystem` 增加可注入的保存目录（internal `SetSaveDirectoryForTests` 或在测试里用 `#if UNITY_INCLUDE_TESTS` 的 `GetSaveDirectory()` 覆盖），测试用独立临时目录 + `[TearDown]` 兜底删除。

---

## 四、P2（中低：覆盖与工程卫生）

### P2-1 指定系统的覆盖现状与公开 API 缺口

| 系统 | 现有测试 | 未覆盖/薄弱点（0 引用 = grep 全测试目录计数为 0） |
|---|---|---|
| Save（v11–v13） | `SettlementPersistenceTests`(3)、`ArmyPersistenceTests`(3)、`AdminDivisionPersistenceTests`(2)、`SettlementBlockSaveTests`(4)、`SettlementFoundingAndMarketSaveTests`(4)、`HabitationSiteSaveMappingTests`、外加并发新增 smoke(2) | **全链路**见 P0-2；`SaveSystem.DeleteSave/ListSaves`、版本兼容分支、`CapturePersistentEntities/RestorePersistentEntities` 全无直接测试 |
| DecreePropagationSystem（8 个公开方法，`DecreePropagationSystem.cs:14-178`） | `DecreePropagationTests`(6) 覆盖 IssueDownward/Advance/Resolve/IssueToSubordinateSettlements | `AttachCourier`=0、`CourierArrived`=0、`TravelDays` 仅被间接使用；多级/并行子节点、enforced→回执路径未测 |
| SettlementInfluenceSystem（4 个公开方法） | `SettlementInfluenceTests`(3) 覆盖 Calculate/UpdateControlTiers/TerrainCost | `GetTierAt`=0；`InfluenceSettings` 的 budget/maxBudget/人口/驻军系数只测了部分组合 |
| GeneticsEngine（12 个公开重载）+ Content | `GeneticsEngineTests`(7)、`GeneticsContentAndHybridTests`(6)、`SexChromosomeSystemTests`(8) | `CreateFounder`=0、`ExpectedHeterozygosity`=0；`ReproduceHybrid` 只有 1 处、`BuildGamete` 只有 1 处；遗传病/表现型-环境交互覆盖薄 |
| UnitProvisioning / UnitFounding / Transport | `UnitProvisioningTests`(6)、`UnitFoundingTests`(4)、`TransportUnitTests`(12) | `UnitProvisioning.TryFund` 的 (world/goodsDefs/region/cost) 重载=0、`Equip`=0；`UnitFounding.TryFoundSettlement` 成功分支未测（只有 null world 失败分支，`UnitFoundingTests.cs:43-53`） |

**为什么有风险**：这些系统都是"规则密集、纯函数多"的类型，单测性价比最高；目前被覆盖的主要是 happy path 的入口，分支/边界大量裸露。

### P2-2 部分 fixture 清理不对称

- `CharacterLocationTests.cs:10-11` 只有 `[SetUp]` 清 `CharacterKnowledgeRegistry`，无 `[TearDown]`。SetUp 方案本身可接受（下一用例前复位），但与其它 fixture 的 TearDown 风格不一致，且若该 fixture 之后新增用例依赖"知识跨用例累积"会踩坑。
- `MapUnitTests.cs:15-16` 只有 TearDown `Clear()`，没有 SetUp；当前安全是因为唯一另一个用 `MapUnitRegistry` 的 `ArmyPersistenceTests` 也有清理，但这是隐式耦合（同 P1-3）。

### P2-3 阳性发现（明确没有以下假绿手段）

- `[Ignore]` / `[Explicit]` / `Assert.Pass` / `Assert.Inconclusive`：0 处。
- `catch`：0 处（测试目录内无任何 try/catch，故不存在 try/catch 吞断言）。
- `Assert.IsTrue(true)` / `Assert.IsFalse(false)` 型恒真：0 处。
- 随机性：遗传学/法令测试全部用固定种子（`GeneticsEngineTests.cs:67,84,125,155,165`、`SexChromosomeSystemTests.cs:65,83,111`、`DecreePropagationTests.cs:100,108`、`GeneticsContentAndHybridTests.cs:71`、`TitleCatalogTests.cs:81`），不存在"随机种子未固定"。
- 时间敏感：测试目录内无 `DateTime.Now` / `Time.` 使用。
- `Assert` 结构（全目录 1,199 行断言语句）：`AreEqual` 602、`IsTrue` 285、`IsFalse` 109、`Greater*` 116、`IsNotNull` 44、`CollectionAssert` 19、`StringAssert` 16 —— 具体值断言占主导，整体不是"只断言非空"的套件。

### P2-4 并发编辑导致审计/结果快照失效

- 17:37 首次扫描时测试目录 55 个 .cs；17:41 出现 `WorldGenerationSmokeTests.cs`（未跟踪）；17:42 时 56 个文件、288 个 `[Test]`。`editmode-results.xml` 生成于 17:31，**不包含**这些新增测试。
- 建议：给测试结果文件命名加时间/commit 标识；审计或 CI 报告以"结果文件中的 testcasecount + 对应提交"为准，不能只看"298/298"这个数字。

---

## 五、可疑 weakened assertions 清单（结论）

| 序 | 位置 | 变更/形态 | 判定 |
|---|---|---|---|
| 1 | `AdminDivisionUiTextTests.cs:58`（git diff vs HEAD） | `Contains("治理头衔：title_gov_general")` → `Contains("治理头衔：")` + `IsFalse(Contains("title_gov_general"))` | **确认放宽**：具体值验证丢失，解析成空串也能过。保留负断言算部分补偿 |
| 2 | `SettlementPersistenceTests.cs:250-251` | `x == null \|\| x.Count == 0` | 二选一宽松，建议只允许一种确定行为（JsonUtility 实测给空列表） |
| 3 | `SettlementInfluenceTests.cs:85` | `AreEqual((int)InfluenceTier.None, 0)` | 恒真/契约常量断言，非行为 |
| 4 | `DecreePropagationTests.cs:38` | `GreaterOrEqual(totalTravelDays, 1)` | 相对下界，未锁公式/距离 |
| 5 | `WorldGenerationSmokeTests.cs:87-93`（并发新增） | 全链路仅比较各表 Count + 锚点存在 | 不足以防"数量对、内容丢"回归 |
| 6 | `ArchitectureContractsTests.cs:95-97` | `IsTrue(TryGet(...))` + `IsNotNull(x)` 后无字段断言 | 弱，但 TryGet 已表达存在性，可接受；建议顺手补字段 |

**未发现**：`Assert.IsTrue(true)`、被注释掉的 `Assert`、`Assert.Pass`、`[Ignore]`、`try{...}catch{Assert.Pass}`、条件跳过测试。

---

## 六、"SaveGame→LoadGame 全链路缺失"结论与最小补测方案

### 明确结论

- **在 298/298 基线（17:31）中，全链路覆盖为 0**，仅有 DTO 往返与辅助函数往返。证据：该时刻测试目录对 `SaveSystem` 的引用为 0；6 个持久化测试文件的断言只触及 `JsonUtility`/`Capture*`/`Restore*`。
- **审计期间 17:41 并发新增的 `WorldGenerationSmokeTests.SaveLoad_FullChain_...` 补了链路冒烟，但只验证"数量"**，且该文件未跟踪、未在结果文件中出现。因此：**"v12/v13 全链路正确"目前不成立，只能算"链路不抛异常 + 各表数量一致"**。
- 风险最高、且现有测试完全没锁的三条实现约束（都在 `SaveSystem.ApplyToWorld` 的注释里被强调）：
  1. 子系统重建后再就地填充省份/聚落（`SaveSystem.cs:222-227`）；
  2. 军队恢复后必须 `AdoptRuntimeIdCounters(nextWarId, nextArmyId)`（`231-236`，实现见 `GameWorld.cs:2746-2756`）；
  3. 实体恢复必须在 `RestorePersistentEntities` 之后，且 `RegisterArmyViews` 必须最后（`238-243`）。

### 最小补测方案（建议新增 `Tests/EditMode/SaveSystemRoundTripTests.cs`，不修改现有文件）

测试环境与清理：

```csharp
// SetUp/TearDown 必须清全局单例（SaveSystem.LoadGame 会写它们）：
// MapUnitRegistry.Clear(); RelationRegistry.Clear(); ContractRegistry.Clear();
// CharacterKnowledgeRegistry.Clear(); DynamicGoodsRegistry.Clear();
// ContentRegistry.Reset(); ContentRegistry.Initialize();
// SaveSystem.DeleteSave(SaveName);
// TearDown 里 DestroyImmediate(world.gameObject) 与 loaded.gameObject（如非空）
```

1. **最小世界 + 一条完整往返**（3–5 条断言即可覆盖 P0-2 主干）：
   - `var go = new GameObject("SaveChainWorld"); var world = go.AddComponent<GameWorld>();`
     设 `mapWidth/mapHeight/tiles`（4×1，全 `exists=true`），按 `SettlementPersistenceTests.NewWorld()` 的方式填 `provinces/settlements/anchors`，再 `world.armies[4] = ...`（照 `ArmyPersistenceTests.BuildArmy()`）。
   - `Assert.IsTrue(SaveSystem.SaveGame(world, "__audit_chain"));`
   - `var loaded = SaveSystem.LoadGame("__audit_chain"); Assert.IsNotNull(loaded);`
   - 字段级断言（不是 Count）：
     - `loaded.provinces.Count == 1` 且 `loaded.provinces[3].provinceName == "测试省"`、`memberTiles` 集合相等；
     - `loaded.settlements.Count == 1` 且 `settlements[anchorId].settlementName/population/siteMode` 与写入一致、`loaded.anchors.ContainsKey(anchorId)`、`loaded.tiles[tile].campId` 回指；
     - `loaded.armies[4].morale/supply/moveProgress/unitCounts[50]/transport.GetCargoAmount(10)` 与写入一致（`ArmyPersistenceTests.cs:101-119` 的断言集可直接复用）；
     - `loaded.realms.Count == before.realms.Count` 且某个 `adminDivisions.Count` / `holderCharacterId` 一致（覆盖 v12 行政区划树）。
2. **v13 自增 ID 续接（当前完全无断言，建议单独一条）**：
   - 存档里最大 `armyId = 4`；读档后 `var newArmy = loaded.CreateArmy(ownerRealmId: 0, commanderId: 12, tileIndex: 0);`
   - `Assert.AreEqual(5, newArmy.armyId, "读档后新军队 ID 必须接在存档最大值之后");`
   - 再断言 `loaded.armies.ContainsKey(5)`，且 `MapUnitRegistry` 里视图 id 与 army id 对应。
   - 注意：`CreateArmy` 会索引 `realms[ownerRealmId]`（`GameWorld.cs:2723-2748`），因此测试里至少要构造一个 `realms[0]`，或改为直接断言 `ArmyPersistence.RestoreArmies` 返回值（`=5`）随后不通过 `CreateArmy` 而通过 `loaded.AdoptRuntimeIdCounters` 的可观察结果——若不想依赖 `CreateArmy`，可对 `RestoreArmies` 返回值断言（已由 `ArmyPersistenceTests.cs:98` 覆盖，但**世界级 adopt 仍未被覆盖**）。
3. **顺序回归哨兵**：读档后 `Assert.IsNotNull(loaded.GetCharacterManager())` 且 `loaded.MapActors` 中 actor 数量 == 存档前（覆盖"人物/商队蒸发"历史 bug，`SaveSystem.cs:238-239` 注释）。
4. **旧档兼容**：
   - v12 档（`armies = null`）：`SaveSystem.LoadGame` 不抛异常、军队表为空、`nextArmyId` 从 1 开始（`ArmyPersistence.cs:42-43`）；
   - `version` 与当前不一致但 `tiles` 存在：应走 `LogWarning` 后继续加载（`SaveSystem.cs:79-87`），断言读档成功且 `version` 迁移到当前值。
5. **磁盘清理**：固定 `SaveName`，TearDown `SaveSystem.DeleteSave(SaveName)`；断言 `SaveSystem.ListSaves()` 不含该名（顺带覆盖 `ListSaves`，当前 0 测试）。

> 上述 1+2 大约 60–80 行代码，即可把 P0-2 从"冒烟"提升为"字段级 + 顺序 + ID 续接"三层保护。若时间只够一条，**优先第 2 条（ID 续接）**——它是 v13 的核心承诺且当前完全无覆盖。

---

## 七、高价值补测清单（≤10，按优先级）

| # | 优先级 | 测什么 | 建议文件 | 最小断言 |
|---|---|---|---|---|
| 1 | P0 | 读档后新建军队 ID 续接（v13 核心承诺） | `Tests/EditMode/ArmyPersistenceTests.cs`（扩展）或新 `SaveSystemRoundTripTests.cs` | `新建 army.armyId == 存档最大 armyId + 1`；`loaded.armies` 同时包含旧/新军队 |
| 2 | P0 | SaveGame→LoadGame 字段级往返（省份/聚落/营地） | 新 `SaveSystemRoundTripTests.cs` | `provinceName/memberTiles/settlementName/population/anchorId/tiles[i].campId` 逐字段相等 |
| 3 | P0 | 读档军队全字段 + 队伍货物 + 行军路径 | 同上（复用 `ArmyPersistenceTests.BuildArmy` 断言集） | `morale/supply/organization/movePath/moveProgress/transport.GetCargoAmount` 相等 |
| 4 | P0 | 读档后角色/家族/MapActor/CharacterKnowledge 不蒸发（历史 bug 哨兵） | 同上 | `GetCharacterManager().GetAllCharacters().Count > 0`；`MapActors` 数量相等；读档后 `SaveSystem.SaveGame(loaded, ...)` 再读回的 Count 不递减 |
| 5 | P1 | 3 层行政树法令下发的旅行时间与递归传递 | `DecreePropagationTests.cs` | 近/远区划 `totalTravelDays` 单调递增；`orders.Count == 子节点数`；3 层时最深层也收到订单 |
| 6 | P1 | 法令信使路径（Courier） | `DecreePropagationTests.cs` | `AttachCourier` 后 `CourierArrived(order, courierTile)` 才 true；未到达 false；到达后 `Advance` 才推进状态 |
| 7 | P1 | 影响力档位与预算上限 | `SettlementInfluenceTests.cs` | `GetTierAt(0)==None`；`maxBudget` 收紧后 tier 不升；人口/驻军系数改变导致 tier 变化 |
| 8 | P1 | 活存档系统的旧档/版本兼容 | 新 `SaveSystemCompatTests.cs` | v11 JSON（无 armies/provinces）读档成功且表为空；`version` 不匹配仍加载成功并将 `saveData.version` 视为当前 |
| 9 | P1 | 地形生成河网与继承（被删测试的替代） | `WorldGenerationSmokeTests.cs`（扩展）或新 `TerrainAndSuccessionTests.cs` | `TrackRivers` 后至少 1 条 `tile.isRiver`；`SuccessionSystem.ExecuteSuccession` 在有/无继承人两种情况下返回非 null 且 holder 改变 |
| 10 | P2 | 单位补给完整路径 | `UnitProvisioningTests.cs` | `TryFund(world, goodsDefs, realm, payer, source, regionId, cost, ...)` 成功扣款/扣物；`Equip` 后 `unit` 获得对应装备；`UnitFounding.TryFoundSettlement` 成功分支创建聚落且扣材料 |

---

## 八、待验证（无法在本轮只读审计中确认）

1. **被删 32 个测试文件的"替代关系"**：部分命名空间已随架构 2.0 删除（`Simulation.Characters/Religion/Culture/Society/Population` 均 0 声明），删除有依据；但 `SuccessionSystem`、`GovernmentComposition`、`InnovationTree`、`DnaSystem` 等类仍在 `Scripts/` 中，**是否仍被活构建/活路径引用需逐模块确认**（已确认 `TerrainGenerator`、`SuccessionSystem` 在新 `GameWorld` 中被引用；其余未逐一验证）。
2. **历史"8 条已知无关失败"的具体去向**：仓内未找到记录；从 `Logs/*.xml` 可见多轮中间失败（如 `content-tests.xml failed="4"`、`settlement-persist.xml failed="3"`、`projection-tests.xml failed="2"`），随后多为 `*2.xml failed="0"`。失败集合中的用例名与当前 298 套件同名（说明是"修好"而非"删掉"），但修的是实现还是断言，因这些测试文件大多未提交、无 diff 可查，**无法区分**；唯一确证的断言放宽是 P1-1。
3. **并发新增测试的最终状态**：`WorldGenerationSmokeTests.cs`（17:41）尚未运行（不在 `editmode-results.xml`），其 `NewWorld()` 在 `AddComponent`（触发 `Awake`）后又显式 `InitializeWorld()`（`WorldGenerationSmokeTests.cs:43-49`），是否双重初始化/资源泄漏待跑测确认。
4. **`editmode-results.xml` 中 `asserts="0"`**：Unity Test Framework 少见的断言计数缺失，不影响 pass/fail，但无法据此评估断言密度；本报告的断言统计基于源码 grep。
5. **`Legacy/Core` 与 `Simulation/...` 双份 `GameWorld/SaveSystem/ContentRegistry` 的编译归属**：测试 asmdef 只引用 `MyriadCreation.Core/Infrastructure/Simulation/World/Rendering/UI/Editor`，未引用根 `CivilizationEvolution.Runtime`；`Scripts/Legacy/**` 在哪个装配体、是否随构建生效，需结合 `Library/ScriptAssemblies` 或 asmdef 归属确认（本轮未运行 Unity）。

---

## 附录：关键证据命令（只读，可复现）

```bash
git status --short -- MyriadCreation/Assets/Tests/EditMode/          # 47 ?? / 9 M / 32 D
git diff --numstat -- MyriadCreation/Assets/Tests/                   # added 56 / deleted 6471
git show HEAD:MyriadCreation/Assets/Tests/EditMode/ReligionSystemTests.cs | grep -c "\[Test\]"
git diff -- MyriadCreation/Assets/Tests/EditMode/AdminDivisionUiTextTests.cs   # 断言放宽实证
grep -rn "SaveSystem" MyriadCreation/Assets/Tests/                    # 17:37 为 0；17:41 后仅 smoke 文件
grep -rc "\[Ignore\]\|\[Explicit\]\|Assert.Pass\|Assert.Inconclusive" MyriadCreation/Assets/Tests/EditMode/   # 0
grep -rn "new GameWorld()" MyriadCreation/Assets/Tests/EditMode/      # 2 处
grep -rn "Random" MyriadCreation/Assets/Tests/EditMode/               # 全部固定种子
sed -n '173,244p' MyriadCreation/Assets/Scripts/Infrastructure/Save/SaveSystem.cs
sed -n '2723,2756p' MyriadCreation/Assets/Scripts/Simulation/MyriadCreation.Simulation.WorldState/GameWorld.cs
```
