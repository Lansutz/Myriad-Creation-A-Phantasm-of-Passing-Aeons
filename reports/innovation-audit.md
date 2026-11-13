# 革新 / 科技（Innovation）系统实现状态审计

审计对象：`D:\Myriad-Creation-A-Phantasm-of-Passing-Aeons`（Unity 6.3 / 6000.6.3f1，工程目录 `MyriadCreation`）
审计方式：只读代码审计 + 只读数据脚本统计。未修改任何游戏代码 / 内容 / 文档；仅在 `reports/` 与 `_fc_verify/` 下新增文件。
审计时间：2026-11-09
结论口径：**不采信 `docs/` 下任何计划文档**，全部结论来自当前树里的 `.cs` 与 JSON 本体。

---

## 1. 目标与验收标准

目标：查清"革新/科技（Innovation）系统"的**真实实现状态**，用 file:line 证据回答 9 个问题（分层、逐项真伪、主循环 tick、玩家入口、解锁生效、存档、内容层一致性、测试覆盖、完成度判定与缺口清单）。

验收标准：
- 每个结论都带 `文件:行` 证据；
- 明确区分「真实可运行逻辑 / 只有数据结构 / 空壳占位」；
- 明确区分「已验证（读到调用点与方法体）」与「推断（无运行期实测）」；
- 报告落在 `reports/innovation-audit.md`。

---

## 2. 输入与前提

阅读范围（均逐行读过）：

| 类别 | 文件 |
| --- | --- |
| 内容定义 | `InnovationDef.cs`、`InnovationEra.cs`、`InnovationField.cs`、`InnovationKind.cs`、`InnovationDomain.cs`、`InnovationDomainMap.cs`、`InnovationUnlock.cs`、`InnovationPrerequisiteNode.cs`、`InnovationPrerequisiteRationale.cs`、`InnovationSoftPrerequisite.cs`、`InnovationReviewPolicy.cs`、`CapabilityChannels.cs` |
| 状态容器 | `InnovationTree.cs`（971 行）、`InnovationProgress.cs`、`InnovationProgressConfig.cs`、`InnovationDiscovery.cs`、`InnovationTreeSaveData.cs` |
| 系统 | `InnovationKnowledgeSystem.cs`、`InnovationResearchSchedule.cs`、`ResearchPlanSystem.cs`、`ResearchPlanData.cs`、`ResearchPlanExecutor.cs`、`ResearchPlanSchedule.cs`、`CharacterResearchManager.cs`、`PlanSystem.cs`、`PlanSchedule.cs` |
| 命令 / 查询 | `StartInnovationResearchCommand.cs`、`StartInnovationResearchCommandHandler.cs`、`InnovationQueryAdapter.cs`、`WorldReadModel.cs`、`GameWorld.ReadModel.cs`、`WorldReadModelExtensionContracts.cs` |
| 法政耦合 | `PolityComponentInnovations.cs`、`LawReform.cs`、`LawSystem.cs`、`LawContracts.cs`、`LawAndDecreeSystem.cs`、`LawRealmHost.cs`、`RegimeChangeDynamics.cs` |
| 消费方 | `ResourceAccess.cs`、`SettlementProduction.cs`、`RecipeCatalog.cs`、`CombatManager.cs`、`BuildingSystem.cs`、`SocialClassAvailability.cs`、`RealmTitleEvolution.cs`、`FamilyNode.cs`、`CultureContextAdapter.cs`、`BuildingAvailabilitySystem.cs` |
| 主循环 / 调度 | `GameWorld.Simulation.cs`（1225 行）、`GameWorld.Core.cs`、`GameWorld.Generation.cs`、`SimulationScheduler.cs`、`SimulationCadence.cs`、`AISchedule.cs`、`AIManager.cs`、`AIController.cs`、`AIIntentExecutor.cs` |
| 内容加载 | `ContentDefinitionIndex.cs`（433–510）、`GoodsPropertyCatalog.cs` |
| 存档 | `SaveSystem.cs`、`SaveData.cs`、`SaveJsonMigration.cs` |
| 内容数据 | `Base/Innovation/Innovations.json`（231,104 B）、`Base/Crafting/Recipes.json`、`Base/Goods/GoodsProperties.json` |
| 测试 | `InnovationSystemTests.cs`、`InnovationMaterialTests.cs`、`InnovationPersistenceTests.cs`、`InnovationPrerequisiteExpressionTests.cs`、`InnovationSchemaTests.cs`、`InnovationMigrationTests.cs`、`InnovationReviewPolicyTests.cs`、`InnovationUnlockRefTests.cs`、`DiscoveryTests.cs`、`ItemInnovationGateTests.cs`、`RecipeGateTests.cs` |

环境：Windows + `python 3.12.10`（`C:\Users\25652\AppData\Local\Programs\Python\Python312\python.exe`）仅用于只读统计 JSON。**没有运行 Unity、没有跑 EditMode 测试**（本会话无法启动 Editor，见 §12.3 未验证项）。

---

## 3. 动作清单

1. `glob` 定位全部 `Innovation*.cs` / `Research*.cs` / `Polity*.cs`；
2. 逐个 `read` 上述文件（含 971 行的 `InnovationTree.cs` 全文）；
3. `grep` 全仓反查调用点：`MonthlyTickProgress|InnovationDiscovery\.|TryDiscover|GetDiscoveryCandidates|ProcessPracticeBreakthroughs|ResearchPlans`、`PracticeRecordedEvent|SetMastery|\.Learn\(|GetMastery\(`、`unlockRefs|InnovationUnlock|\.unlocks\b`、`HasInnovation\(|GetRealmCapabilities|AreItemInnovationsMet|IsComponentAvailable`、`TryCraft|SettlementProduction\.|CanCraft\(`、`TechAccessible|CanAccessResource|CanDevelop`、`InnovationProgressConfig|InnovationReviewPolicy|GetProgress\(|HasFoundation|GetLearningDifficulty|GetEffectiveResearchRate`、`StartInnovationResearch|CheckInnovationRequirements|GetMissingInnovations|BeginEnactLaw`、`OnInnovationCompleted|OnInnovationDiscovered`、`CharacterResearchManager`、`DiscoveryActors\.`、`CreateController\(`、`minTechLevel|techLevel`；
4. `grep` UI 层：`MyriadCreation\Assets\UI` 全目录搜 `Innovation` / `革新` / `研究` / `Research` / `TechAccessible` / `WorldReadModel`；
5. 新建并运行三个只读统计脚本（均在 `_fc_verify\`）：`inno_audit.py`（条目数/字段覆盖/前置图环与悬空/解锁统计）、`inno_struct.py`（前置节点结构异常）、`inno_unlock.py` + `inno_badref.py`（unlockRefs 的 id/key 使用与坏引用）、`goods_check.py`（软前置依赖的物产属性）；
6. 读 `Base/Goods/GoodsProperties.json` 与 `Base/Crafting/Recipes.json` 的对应字段。

运行命令（关键参数逐字保留）：

```
python _fc_verify\inno_audit.py   > _fc_verify\inno-audit.txt   2>&1
python _fc_verify\inno_struct.py  > _fc_verify\inno-struct.txt  2>&1
python _fc_verify\inno_unlock.py  > _fc_verify\inno-unlock.txt  2>&1
python _fc_verify\inno_badref.py  (stdout)
python _fc_verify\goods_check.py  (stdout)
```

首次运行 `inno_audit.py` 因 `leaves()` 返回 `None` 抛 `TypeError: 'NoneType' object is not iterable`（`_fc_verify\inno-audit.txt:40-44`），修正函数返回后 exit=0。此失败如实记录，不影响后续结论（数据统计部分在失败前已完成，且修正后结果一致）。

---

## 4. 问题 1：系统由哪几层组成，概念落在哪个类 / 哪个状态容器

| 概念 | 定义位置 | 运行期状态容器 | 性质 |
| --- | --- | --- | --- |
| InnovationDef（革新条目） | `Content.Definitions\InnovationDef.cs:8-131`（字段 10–80） | 不可变内容，载入 `ContentRegistry`；`InnovationTree._innovations`（`InnovationTree.cs:17`）持有只读副本 | 静态内容 + 只读索引 |
| Era 时代 | `InnovationEra.cs:5`（`StartYears = {-100000,-10000,-3300,-1200,500,1500}`）、`7-17`、`19-36` | 无状态（纯静态函数） | 真实计算，被 AI 排序消费 |
| Field 领域（26 值） | `InnovationField.cs:3-31` | `InnovationDef.field` | 分类标签 |
| Kind 类别（7 值） | `InnovationKind.cs:4-14` | `InnovationDef.kind` | 仅元数据（无运行时读取方） |
| Domain 域（4 值） | `InnovationDomain.cs:3-9` + `InnovationDomainMap.cs:7-44` | `InnovationDef.Domain => InnovationDomainMap.GetDomain(field)`（`InnovationDef.cs:113`） | 真实映射，被 AI 偏好与日志消费 |
| 硬前置树 | `InnovationPrerequisiteNode.cs:6-47`（Leaf/All/Any，可嵌套） | `InnovationDef.prerequisiteTree`（`InnovationDef.cs:69`） | 真实求值 |
| 软前置 | `InnovationSoftPrerequisite.cs:10-25`（goodsId/propertyId/capabilityChannel/propertyFactor） | `InnovationDef.softPrerequisites`（`InnovationDef.cs:40`） | 真实求值（能力阈值） |
| 前置理由 | `InnovationPrerequisiteRationale.cs:7-11` | `InnovationDef.prerequisiteRationale` | 仅元数据/测试 |
| ReviewPolicy | `InnovationReviewPolicy.cs:8-22`（宗教/思想 5 个 field 冻结） | 无状态（纯静态判定） | 无运行时消费者 |
| Discovery 发现 | `InnovationDiscovery.cs:7-35` + `InnovationDef.discoveryGroups` | `InnovationTree._realmDiscovered`（`InnovationTree.cs:25`） | **运行期永不写入** |
| Progress 进度 | `InnovationProgress.cs:7-26` + `InnovationProgressConfig.cs:3-32` | `InnovationTree._innovationProgress`（`InnovationTree.cs:27`） | **推进函数无调用方** |
| ResearchPlan 研究计划 | `ResearchPlanData.cs:5-23`、`ResearchPlanSystem.cs:13-311`、`ResearchPlanExecutor.cs:7-48` | `ResearchPlanSystem._data`（`ResearchPlanSystem.cs:21`）+ `PlanSystem._plans`（`PlanSystem.cs:11`） | **真实推进** |
| Knowledge 知识/熟练 | `InnovationKnowledgeSystem.cs:8-140`（mastery 0–3、practice） | `_masteryByCharacter` / `_practiceByCharacterInnovation`（`InnovationKnowledgeSystem.cs:18-20`） | 容器存在、**熟练永远为 0** |
| 政权革新集合（主干状态） | `InnovationTree.cs:19-29`（6 张私有字典） | `_realmInnovations` / `_realmResearchPoints` / `_realmCurrentResearch` / `_realmDiscovered` / `_innovationProgress` / `_resourceCumulativeOutput` | 真实状态，且入档 |
| 旧版"研究点"推进 | `InnovationResearchSchedule.cs:36-56` + `InnovationTree.DailyTick`（`InnovationTree.cs:324-339`） | 同上 `_realmResearchPoints` | **真实推进（AI 驱动）** |
| 法政制度耦合 | `PolityComponentInnovations.cs:21-170`（硬编码 7 维 × 组件的革新 id 表） | 无状态常量表 | 真实门禁，被政变/改革路径消费 |
| 查询契约 | `WorldReadModelExtensionContracts.cs:118-146`（`IInnovationQuery`） | `InnovationQueryAdapter.cs:15-157` | 已实现，无 UI 消费者 |
| 命令 | `StartInnovationResearchCommand.cs:5-10` + `Handler:20-27` | 注册于 `GameWorld.Core.cs:339` | 只有 AI 意图执行器在发 |

---

## 5. 问题 2：逐项真伪判定

### 5.1 真实可运行逻辑（有状态推进 / 条件判定 / 生效）

| 项 | 证据 | 说明 |
| --- | --- | --- |
| 硬前置求值（Leaf/All/Any 可嵌套） | `InnovationTree.cs:221-258`（`EvaluatePrerequisiteNode`）、调用点 `274-282` | 空节点返回 true（`223-226`），All 全真、Any 一真 |
| 前置内容存在性短路 | `InnovationTree.cs:198-219` + `276-282` | 树为空（无 Leaf id）时视为"无前置"，不锁死 |
| 能力（capability 字符串）门禁 | `InnovationTree.cs:65-104`（`GetRealmCapabilities`/`HasCapabilities`）+ 消费点 `283-286` | 由已研究革新的 `providesCapabilities` 汇总 |
| 能力数值 + 软前置门禁 | `InnovationTree.cs:106-174`（`GetCapabilityValue` / `HasCapabilityValues`），软前置块 `154-172` → `GoodsPropertyCatalog.TryGetValue`（`GoodsPropertyCatalog.cs:45-61`） | 数据可解析：goods 50/51/57 均有 `meltingPoint`（1538/1085/1500，见 `_fc_verify` 中 `goods_check.py` 输出） |
| 物产前置（含累计产量） | `InnovationTree.cs:274-322`（`requiredAllResources` / `requiredAnyResources`）+ `589-630`（`HasResource`） | 逻辑真实，但**内容里一个条目都没配**（见 §10.4） |
| 旧版研究点推进（AI 选课题 → 每日累加 → 完成） | 注册 `GameWorld.Simulation.cs:835` → `InnovationResearchSchedule.cs:36-56` → `InnovationTree.DailyTick:324-339` → `CompleteResearch:341-355` | 每日按 `AIController.CalculateResearchRate`（`AIController.cs:161-179`，基数 0.5×开发度×性格乘子）累加，`points >= researchCost` 即完成 |
| AI 选题 | `AIController.cs:131-158`（`SelectDailyIntent`）+ `AISchedule.cs:53-59` + `AIIntentExecutor.cs:32-34` → `StartInnovationResearchCommandHandler.cs:20-27` | 按 `preferredDomains`（`Domain`）→ era 权重（`InnovationEra.GetPreferenceWeight:19-36`）→ 成本排序取第 0 项 |
| 研究计划链（角色突破 → 计划 → 验证 → 正式解锁） | `ResearchPlanSystem.cs:135-178`（`TryDailyBreakthrough`）→ `194-222`（`CreateResearchPlan`）→ `224-235`（`StartResearchPlan`）；执行器 `ResearchPlanExecutor.cs:18-47`；固化 `ResearchPlanSystem.cs:285-310`（`TryFormalizeResearch`） | 概率上限 0.0005×…→ clamp 0.03/日（`267-273`）；验证进度 `(0.004 + scholarship/100*0.012)*(0.75+relevance*0.5)*days`（`ResearchPlanExecutor.cs:29-31`），到 1 时 `StartResearch + DailyTick(cost)` **瞬间完成**（`ResearchPlanSystem.cs:299-309`） |
| 练习量采集 | `BuildingSystem.cs:223-227`、`239-242`（建造中每日发 `PracticeRecordedEvent`）；`CombatManager.cs:141-167`（按 `UnitDef.requiredInnovations` 按兵力发）→ `GameWorld.Core.cs:290-302`；订阅 `ResearchPlanSystem.cs:48,51-54` | 事件链真实闭环 |
| 研究计划每日调度 | 注册 `GameWorld.Simulation.cs:837` → `ResearchPlanSchedule.cs:20-23` → `ResearchPlanSystem.ProcessPracticeBreakthroughs:92-111` | 只处理"当天有过练习记录"的角色（`_practiceDirtyCharacters`，`67-75`） |
| 计划执行 tick | `PlanSchedule.cs:20-23`（order 30）→ `PlanSystem.DailyTick:237-304` → 执行器 | 计划状态机真实（`PlanSystem.cs:330-373`） |
| 法政制度门禁 | `PolityComponentInnovations.cs:142-170`（id 表 21-140）→ `LawReform.CanReform:10-17` → 调用点 `RegimeChangeDynamics.cs:261`（由 `PoliticsSimulationSystem.cs:143` 每日驱动） | 政变/派系改革必须持有对应革新（或该组件无要求时 `158-161` 直接 true） |
| 社会阶层 / 头衔 / 家族传统 门禁 | `SocialClassAvailability.cs:28,46,56,86-100`；`RealmTitleEvolution.cs:20`（革新 1025）；`FamilyNode.cs:152-160` | 都是真实调用（阶层出现 `ClassEmergenceEvents.cs:43`、分层 `SocialStratificationCalculator.cs:58-76`、局势面板 `RealmSituationBuilder.cs:149`） |
| 文化侧读取 | `CultureContextAdapter.cs:94-100`（`GetRealmInnovations`） | 月度文化演化真实使用 |
| 存档 | `SaveSystem.cs:397`（Capture）与 `464`（Restore） | 见 §8 |

### 5.2 只有数据结构 / 没有系统推进

| 项 | 证据 | 判定依据 |
| --- | --- | --- |
| **Discovery 发现** | 状态容器 `InnovationTree.cs:25`；写入唯一入口 `InnovationTree.Discover:362-378`；全仓唯一调用方是 `InnovationDiscovery.TryDiscover:23-35`，而 `TryDiscover` 的调用方**只有测试**（`DiscoveryTests.cs:49,53,56,67,68,73,79`） | 游戏运行期 `_realmDiscovered` 永不写入；`OnInnovationDiscovered`（`InnovationTree.cs:33`）无任何 `+=` 订阅方 → 死事件 |
| **发现人群（商人/工匠/学者）** | `DiscoveryActors`（`DiscoveryTests.cs:92-119` 使用）+ `discoveryGroups`（`ContentDefinitionIndex.cs:462-487` 自动补全） | 全仓 `DiscoveryActors.` 在 `Assets\Scripts` 下**零命中**（只有测试用）→ 人群概念未接入任何 tick |
| **Progress 进度（`MonthlyTickProgress` 那套完整公式）** | `InnovationTree.cs:677-754`（生产实践 `712-724`、角色研究 `725-734`、设施 `735-740`、边际递减 `744-747`）| 全仓唯一引用是定义本身（grep `MonthlyTickProgress` 仅 1 处命中）→ **零调用方** |
| **`InnovationProgressConfig` 全部常量** | `InnovationProgressConfig.cs:3-32` | 全仓 0 引用（grep 仅定义处）；`MonthlyTickProgress` 内部直接写字面量 0.15f/0.2f/0.1f/50f/60f/2f（`713,717,718,725,739,742,744,747`）而非引用常量 |
| **Knowledge 熟练/练习（角色级）** | 容器 `InnovationKnowledgeSystem.cs:18-20`；写入口 `SetMastery:31-50`、`Learn:74-91` | `SetMastery` 全仓仅 `Learn:90` 调用；`Learn` 仅 `ResearchPlanSystem.Learn:242-254` 调用；而 `ResearchPlanSystem.Learn` **零调用方** ⇒ 运行期熟练永远 0 ⇒ `HasFoundation:93-96`、`GetDiscoveryCandidates` 的 `IsKnowledgeNeighbor:115-134` 对"有前置的革新"永远返回 false |
| **`CharacterResearchManager`（角色研究方向/效率/成本）** | `CharacterResearchManager.cs:6-88` | 全仓无 `new CharacterResearchManager`、无字段持有（grep 仅 2 处命中，都在这份文件里）→ 完全未接线；它本该给 `MonthlyTickProgress` 提供 `researcherCount` |
| **物品多革新门槛** | `InnovationTree.AreItemInnovationsMet:260-272` | 唯一调用方是 `ItemInnovationGateTests.cs:45,47,49,56,58`；`Assets\Scripts` 下零消费者 |
| **配方革新门槛** | `SettlementProduction.TryCraft(…, innovations, realmId, hasBuilding)`（`SettlementProduction.cs:20-23`，`enforceGates:true` 见 `40-50`） | 该 6 参重载在 `Assets\Scripts` 下**零调用方**；`SettlementProduction.` 在全仓 Scripts 也只有自身定义（grep 2 命中）。即：43 条配方里 34 条带 `requiredInnovation>0`，但游戏内没有任何合成调用会走门禁 |
| **资源技术/深度门槛** | `ResourceAccess.IsTechAccessible:47-54`、`CanDevelop:56-67` | 只经 `InnovationQueryAdapter.cs:126-146`（查询契约）暴露；`CanDevelop` 只有测试调用（`ResourceDepthTests.cs:109-124`）；且 `TileResource.developed` 在 `Assets\Scripts` 下**没有任何赋值点**（grep `developed = true` 0 命中）→ 门槛结果无人消费 |
| **法律革新要求（int 版）** | `LawAndDecreeSystem.cs:76-95` + `LawRealmHost.BeginEnactLaw(…, ownedInnovationIds, …):82-119` | 唯一的 host 入口 `LawContracts.cs:215` 传的是 **`null`** ⇒ `ownedInnovationIds != null` 判定短路，革新要求永不生效；只读展示路径 `LawDecreeView.cs:178,199` 同样传 `null` |
| **法律革新要求（string 版）** | `LawSystem.cs:116-149`（`CheckInnovationRequirements` / `GetMissingInnovations`） | 两者零调用方（grep 仅定义处）；`LawEffects.cs:398-435` 用的是 `"bureaucracy"`/`"writing"` 等字符串，与 `InnovationTree`（int 集合）根本不同源 |
| **建筑"科技等级"** | `BuildingAvailabilitySystem.cs:398-401`（`techLevel < minTechLevel`）、`290` 与 `409` 的 `techLevel = 0` 默认参数 | `CheckAvailability`/`GetAvailableBuildings` 在 `Assets\Scripts` 下无外部调用方；`SettlementAISystem.CheckBuildingAvailability:15-17` 本身也是死方法（grep 仅定义）且默认传 0 ⇒ 与 InnovationTree 无连接 |
| **Era 硬门槛** | `InnovationTree.GetAvailableInnovations:453-464` | 只过滤"已拥有 + 前置满足"，**完全没有 era 比较** ⇒ Era 只是 AI 排序权重（`AIController.cs:154-156`），不是解锁条件 |

### 5.3 空壳 / 占位 / 恒真恒假

| 项 | 证据 | 实情 |
| --- | --- | --- |
| `InnovationQueryAdapter.GetAvailableInnovations` 的 `canResearch` | `InnovationQueryAdapter.cs:74` `canResearch = true` | 恒真字面量（列表已由 `tree.GetAvailableInnovations` 预过滤，此字段无独立语义） |
| `InnovationTree.GetDirectPrerequisites` 的 `or` 分支 | `InnovationTree.cs:444-451` `return (and: value.GetPrerequisiteIds(), or: new List<int>())` | `or` 恒为空；Any 分支被展平成 and（后果见 §12.2 缺口 6） |
| `CapabilityChannels.GetBaseValue` | `CapabilityChannels.cs:7-11` `_ = channel == "heat.maxTemperature"; return 0f;` | **恒返回 0**（所有能力通道基线都是 0），"按通道给基线"是占位 |
| `InnovationReviewPolicy.IsFrozen` | `InnovationReviewPolicy.cs:8-22`；全仓唯一调用方 `InnovationReviewPolicyTests.cs:19,29` | 判定真实但**无运行时消费者** ⇒ "冻结"不影响研究/解锁 |
| `InnovationUnlock` / `InnovationDef.unlockRefs` | 定义 `InnovationUnlock.cs:22-38`、字段 `InnovationDef.cs:72`；内容 84/186 条带 `unlockRefs` | 全仓除定义与 1 处测试（`InnovationPrerequisiteExpressionTests.cs:112-130` 序列化往返）外**零读取** ⇒ 结构化解锁表是纯数据 |
| `InnovationDef.unlocks`（字符串清单） | 字段 `InnovationDef.cs:74`；186/186 条都有值（197 条，其中 145 条是 `capability:xxx`） | 零读取方 |
| `InnovationDef` 元数据字段 | `termEn`(54)、`definition`(56)、`scope`(58)、`category`(62)、`sources`(64)、`prerequisiteRationale`(66)、`legacyIds`(76)、`variantOf`(78)、`regions`(80)、`contentSchemaVersion`(52) | 运行期零消费者（grep `\.termEn|\.variantOf|\.legacyIds|\.regions|\.prerequisiteRationale|contentSchemaVersion` 只命中 `InnovationSchemaTests.cs` 与 `InnovationDef.cs` 自身） |
| `InnovationTree.GetLearningDifficulty` / `GetEffectiveResearchRate` | `InnovationTree.cs:466-488`、`490-516` | 互相调用之外**零调用方**（grep 仅定义 + 内部 492）⇒ 文化亲和加成（`affinityTags`，1.25×，`498-513`）永不生效 |
| `ResearchPlanSystem.DailyTick` | `ResearchPlanSystem.cs:118-133` | 零调用方（调度只调 `ProcessPracticeBreakthroughs`，`ResearchPlanSchedule.cs:22`）⇒ 那条"全角色每日尝试"入口是死代码 |
| `ResearchPlanSystem.HasActiveResearchPlan` 之外的 `DrainPracticeDirtyCharacters` 外部使用者 | `ResearchPlanSystem.cs:77-90` | 仅有内部 `99` 调用 |
| `InnovationTree.GetProgress` 的进度字典 | `InnovationTree.cs:576-587` | 唯一写入点是死掉的 `MonthlyTickProgress:683`；另注意 `InnovationProgress` **没有 realmId 字段**（`InnovationProgress.cs:7-26`），键只有 innovationId ⇒ 即便启用也会跨政权共享（设计缺陷） |
| `StartInnovationResearchCommand` 的玩家侧入口 | `GameWorld.Core.cs:339` 注册；唯一发送方 `AIIntentExecutor.cs:33` | 契约在，但 UI 不发（见 §6） |
| `EvaluatePrerequisiteNode` 的"非叶且无子"分支 | `InnovationTree.cs:231-234` 返回 false；而 `InnovationPrerequisiteNode.IsLeaf:46` 把"无子节点"一律当 Leaf（即使 kind=All） | 恒假分支；内容层无此形态（`_fc_verify`：All/Any 空子节点 0 个、单子节点 0 个、kind=0 却带子节点 0 个）⇒ 当前只是防御代码 |

---

## 6. 问题 3：主循环是否 tick，注册点与频率

| 系统 | 是否注册 | 位置 | 频率 / 顺序 |
| --- | --- | --- | --- |
| `InnovationResearchSchedule` | **是** | `GameWorld.Simulation.cs:835` | `SimulationCadence.Daily`，order **29**，`ScheduleId = "innovation.legacy_research.daily"`（`InnovationResearchSchedule.cs:12,36`） |
| `ResearchPlanSchedule` | **是** | `GameWorld.Simulation.cs:837` | `SimulationCadence.Daily`，order **29**，`ScheduleId = "research.breakthrough.daily"`（`ResearchPlanSchedule.cs:8,20`） |
| `PlanSchedule`（研究计划的执行器真正跑的地方） | **是** | `GameWorld.Simulation.cs:838` | Daily，order **30**（`PlanSchedule.cs:20`） |
| `AISchedule`（选题 → 发命令） | **是** | `GameWorld.Simulation.cs:834` | Daily，order **28**（`AISchedule.cs:53`），先于 29 |
| `InnovationKnowledgeSystem` | **否（不是 tick 系统）** | 由 `ResearchPlanSystem` 以字段持有并 new：`ResearchPlanSystem.cs:19` | 无独立调度；只被动调用 |
| `CharacterResearchManager` | **否** | 无任何实例化（§5.2） | — |

调度语义（`SimulationScheduler.cs:120-172`）：`Daily` ⇒ `intervalDays = 1`（`174-184`，`default => 1`），每次 tick 按 `order` 升序执行，同 order 用 id 字符串序（`136-140`）。因此实际顺序是 `ai.daily(28) → innovation.legacy_research.daily(29) / research.breakthrough.daily(29) → plans.daily(30)`。同日建的计划可在同日被 `plans.daily` 推进，AI 新建的研究当日就能吃到研究点增量。

结论：**除 Knowledge/Discovery/Progress/CharacterResearch 这几层外，主干（AI 选题 + 研究点推进 + 计划突破）确实在主循环里每日跑。**

---

## 7. 问题 4：玩家入口

- **UI 层零引用**。`MyriadCreation\Assets\UI` 全目录（28 个文件，含 `Panels\`、`Common\`、`MyriadCreation.UI.Presentation\`）grep `Innovation` / `革新` / `研究` / `Research` = **0 命中**；grep `TechAccessible|IInnovationQuery|GetAvailableInnovations` 只在 `UIManager`/`GovernmentPanel`/`SocietyPanel` 等处命中通用 `IWorldReadModel`，无一处调 `IInnovationQuery` 的方法。存在 UI 文件清单见 `_fc_verify` 首次 inventory 输出。
- **没有"科技/革新"面板**：不存在 `InnovationPanel.cs` / `TechPanel.cs`（`glob` `*Innovation*` 只命中脚本、测试、JSON 与 .meta）。
- **契约存在且已实现**：`IInnovationQuery`（`WorldReadModelExtensionContracts.cs:118-146`）由 `InnovationQueryAdapter`（`InnovationQueryAdapter.cs:15-157`）实现，并经 `WorldReadModel.cs:170-198`、`GameWorld.ReadModel.cs:111-138` 双重转发；`GameWorldReadModelTests.cs:142`、`UiReadModelExtensionTests.cs:350` 断言类型可用。**但无 UI 消费者**。
- **写入口只有 AI**：`StartInnovationResearchCommand`（`StartInnovationResearchCommand.cs:5-10`）唯一发送点 `AIIntentExecutor.cs:33`；`GameWorld.UiCommands.cs` 内 `Innovation|Research` = 0 命中。
- **推断（未实测）**：`AIManager.DailyTick:21-27` 遍历 `_controllers` 中所有政权，没有 `PlayerRealmId`（`GameWorld.Core.cs:94`）过滤；`GameWorld.Generation.cs:117` 对每个政权都 `CreateController`。因此玩家所属政权的研究课题也由 AI 自动挑选、自动完成 —— 玩家既不能选择，也不能停止。

**判定：玩家没有可用入口（只读契约存在但无人调用；无任何可操作界面）。**

---

## 8. 问题 5：解锁是否真的生效

分两条线看，结论不同：

### 8.1 结构化"解锁清单"（`unlockRefs` / `unlocks`）——**无消费者**

- `InnovationDef.unlockRefs`（`InnovationDef.cs:72`）与 `unlocks`（`:74`）：全仓 grep `unlockRefs|InnovationUnlock|\.unlocks\b` 共 8 处命中，全部在 `InnovationDef.cs` / `InnovationUnlock.cs` 自身定义内，外加 1 处序列化测试（`InnovationPrerequisiteExpressionTests.cs:112-130`）。
- 内容层 84 条革新带 `unlockRefs`（合计 120 条引用：Recipe 31 / Building 29 / Law 30 / Good 24 / Unit 6），197 条 `unlocks` 字符串。
- **明确结论：解锁清单无消费者。** 研究完成不会因为 `unlockRefs` 而让任何配方/建筑/法律/物品变得可用——那些门槛用的是**另外几条硬字段引用**。

### 8.2 真正生效的"间接门槛"——全部都挂在 `HasInnovation` / capability 上，而不是 `unlockRefs`

| 门槛 | 生效方式 | 是否在真实游戏路径上 |
| --- | --- | --- |
| 法政制度组件 | `PolityComponentInnovations.IsComponentAvailable`（`:151-170`）← `LawReform.CanReform`（`:10-17`）← `RegimeChangeDynamics.cs:261`（由 `PoliticsSimulationSystem.cs:143` 每日 Tick） | **是**（政变/派系改革被革新 id 表卡住） |
| 社会阶层出现/分层 | `SocialClassAvailability.cs:28,46,56`（硬编码 952/600/505）← `ClassEmergenceEvents.cs:43`、`SocialStratificationCalculator.cs:58-76`、`RealmSituationBuilder.cs:149` | **是** |
| 统治者头衔 | `RealmTitleEvolution.cs:20`（革新 1025，由 `ContentDefinitionIndex.cs:491-500` 注入）← `GameWorld.Succession.cs:94` | **是** |
| 家族传统 | `FamilyNode.cs:152-160` | **是**（`Debug.Log` 拒绝添加） |
| 文化演化 | `CultureContextAdapter.cs:94-100` | **是**（月度） |
| 研究可用性（AI 选题） | `InnovationTree.ArePrerequisitesMet` | **是** |
| 物产是否可采（`GoodsDef.requiredInnovation`） | `ResourceAccess.IsTechAccessible:47-54` → 仅经 `IInnovationQuery`（`InnovationQueryAdapter.cs:126-134`）；`CanDevelop:56-67` 只被测试调 | **否**：模拟内没有任何地方用它拦开采；`TileResource.developed` 无赋值点 |
| 配方可造（`GoodsRecipe.requiredInnovation`） | `SettlementProduction.TryCraft` 6 参重载（`:20-23`）→ `40-50` | **否**：6 参重载零调用方 |
| 物品/装备门槛（`EquipmentDef.requiredInnovation`） | `InnovationTree.AreItemInnovationsMet:260-272` | **否**：零调用方（只有测试） |
| 法律可立（int 版） | `LawAndDecreeSystem.cs:76-95` | **否**：`LawContracts.cs:215` 传 `null` |
| 法律可立（string 版） | `LawSystem.cs:116-149` | **否**：零调用方，且字符串集与 int 树不同源 |
| 建筑"科技等级" | `BuildingAvailabilitySystem.cs:398-401` | **否**：`techLevel` 恒 0，调用链本身也没接上 |

---

## 9. 问题 6：存档

| 数据 | 是否入档 | 证据 |
| --- | --- | --- |
| 政权已获革新 / 已发现 / 研究点 / 当前研究 | **是** | `InnovationTree.CaptureState:828-905`（`832-857` 拍平 4 张字典）→ `SaveSystem.cs:397`；还原 `InnovationTree.RestoreState:908-969` ← `SaveSystem.cs:464` |
| 单项革新进度（`_innovationProgress`） | 结构有、**实际恒空** | DTO `InnovationTreeSaveData.cs:33-45`；序列化 `InnovationTree.cs:860-893`。但唯一写入点 `MonthlyTickProgress:683` 零调用（§5.2）⇒ 存档里该数组永远为空 |
| 资源累计产出（`_resourceCumulativeOutput`） | 结构有、**实际恒空** | `InnovationTree.cs:895-902`；唯一写入点 `AccumulateResourceOutput:632-640` 只被死函数 `MonthlyTickProgress:702,709` 调用 |
| 角色熟练 / 练习量（`InnovationKnowledgeSystem`） | **未入档** | `InnovationTreeSaveData.cs:13-53` 无对应字段；`SaveData.cs:20-77` 无对应字段；`_masteryByCharacter`/`_practiceByCharacterInnovation`（`InnovationKnowledgeSystem.cs:18-20`）读档即归零 |
| `ResearchPlanData`（计划↔角色↔革新↔验证进度） | **未入档** | `ResearchPlanData.cs:5-23` 无任何 DTO；`SaveData.cs:20-77` 无计划段；`ResearchPlanSystem._data`（`:21`）是纯内存字典 |
| `PlanSystem._plans`（含研究计划本体、计划 id 计数器） | **未入档** | `SaveData.cs` 全文无 plans 字段（grep `Plan|plan` 在 Save 目录 0 命中） |
| 迁移 | **无革新相关迁移** | `SaveJsonMigration.cs:22-101` 只处理 `<v11` 的聚落旧键（`burgs`/`settlements` → `habitation_sites`）；`innovations` 字段缺失时 `SaveSystem.cs:464` 走 `RestoreState(null)` ⇒ 清空（`InnovationTree.cs:917` 提前 return） |

**丢失字段清单（读档直接蒸发）**：角色熟练等级、角色练习量、研究计划全部字段（含 `verificationProgress`、`formallyUnlocked`、`relevance`）、计划 id 自增计数器、`_practiceDirtyCharacters`。

副作用（推断）：读档后已"发现"过的角色突破全部作废，必须靠新的建造/战斗练习重新积累；但**已经固化的革新不会丢**（落在 `_realmInnovations`），故不会出现科技回退。

---

## 10. 问题 7：内容层（Innovations.json）与代码 schema 一致性

统计脚本：`_fc_verify\inno_audit.py`、`inno_struct.py`、`inno_unlock.py`、`inno_badref.py`（输出 `_fc_verify\inno-audit.txt`、`inno-struct.txt`、`inno-unlock.txt`）。

### 10.1 规模与分布

| 项 | 值 |
| --- | --- |
| 顶层键 | 仅 `innovations`（`inno-audit.txt:1`） |
| 条目数 | **186**（`inno-audit.txt:2`） |
| id 范围 | 100 – 1601，无重复、无 `<=0`（`inno-audit.txt:3-4, inno-struct.txt:9`） |
| 运行期实际条目数 | 186 + 2 = **188**（`ContentDefinitionIndex.cs:491-509` 无条件 `Set(1025)` 与 `Set(1)`；两者 JSON 中均不存在，已核验） |
| `contentSchemaVersion` | 全部 = 2（186/186，`inno-audit.txt:53`）——注意 `InnovationSchemaTests.cs:9-11` 的注释仍写"旧 185 条 schema 0/1 豁免"，注释已过期 |
| `era` 分布 | 0:24 / 1:36 / 2:51 / 3:53 / 4:20 / 5:2（`inno-audit.txt:55`） |
| `kind` 分布 | 2=75, 4=61, 6=22, 3=18, 5=8, 1=2（`inno-audit.txt:54`），全部落在 `InnovationKind` 1–6 |
| `field` 分布 | 25 个枚举值均用到 0–25（`inno-audit.txt:56`），无越界 |
| `researchCost` | 20–1500，无 `<=0`（`inno-audit.txt:57`） |
| 前置 | 167 条有 `prerequisiteTree`；19 条根；节点 kind 计数 Leaf 245 / All 63 / Any 8（`inno-audit.txt:38-39,59-60`） |
| 前置图健康 | **悬空引用 0，自引用 0，环 0**（`inno-audit.txt:41-45`）；`InnovationSystemTests.cs:135-175` 另有两处运行时断言一致 |
| 节点结构异常 | kind=0 却带子节点 0；All/Any 空子 0；All/Any 单子 0（`inno-struct.txt:1-3`） |

### 10.2 JSON 有、代码字段在、但**无运行时读取方**（“白存”）

`termEn`(186/186)、`definition`(186/186)、`scope`(186/186)、`category`(186/186)、`sources`(186/186)、`prerequisiteRationale`(167/186)、`legacyIds`(38/186)、`variantOf`(38/186)、`regions`(38/186)、`unlockRefs`(84/186)、`unlocks`(186/186)、`kind`(186/186)、`contentSchemaVersion`(186/186) —— 均只被 `InnovationSchemaTests` / `InnovationMigrationTests` / `InnovationPrerequisiteExpressionTests` 读取（见 §5.3 证据）。

### 10.3 代码要、JSON 完全没有的字段

`requiredAnyResources`、`requiredAllResources`、`requiredCapabilities`、`requiredCapabilityValues`、`practiceTags` —— 0/186（`inno-audit.txt:34`）。另外 `allowTradeResource`、`requiredResourceAmount`、`discoveryGroups` 在 JSON 中也不存在，但：

- `discoveryGroups` 由 loader 按 field 自动补全（`ContentDefinitionIndex.cs:462-487`）⇒ 运行期非空，`DiscoveryTests.cs:25-39` 依赖此兜底；
- `allowTradeResource`/`requiredResourceAmount` 依赖 C# 字段初始值（`InnovationDef.cs:28,30`）。**推断**：`JsonUtility` 对缺失字段是否会保留字段初始化值，本会话无法实测；代码自身处处对同批字段做 null 判空（`InnovationDef.cs:91`、`InnovationTree.cs:295,299`、`ContentDefinitionIndex.cs:440,455,462`），与"缺失即默认/null"一致。因 `requiredAnyResources`/`requiredAllResources` 全无内容，该推断当前无游戏性影响。

### 10.4 内容 vs 代码的功能性缺口

- **物产前置整条链在真实内容上永不触发**：JSON 无任何 `requiredAnyResources/requiredAllResources` ⇒ `ArePrerequisitesMet` 的物产分支（`InnovationTree.cs:295-320`）对 186 条内容全部跳过；只有测试临时注册的条目会走到（`InnovationSystemTests.cs:45-59`）。
- **能力硬门槛（`requiredCapabilities`）在真实内容上也永不触发**（0/186）；但 loader 仍给 Mining/Metallurgy 注入 `providesCapabilities`（`ContentDefinitionIndex.cs:440-453`：Mining ⇒ `mining.surface/shaft/deep` 全给；Metallurgy ⇒ `heat.high`）与 `heat.maxTemperature +700`（`454-461`）⇒ 提供方存在、需求方为空。
- **软前置是真实生效的**（5/186：201 青铜冶炼 / 202 块炼铁 / 203 渗碳炼钢 / 930 渗碳淬火 / 931 生铁高炉），且依赖物产属性可解析（`GoodsProperties.json`：50=1538、51=1085、52=1538、54=232、56=950、57=1500 熔点；脚本 `goods_check.py`）。以 202 为例：需 `1538 × 0.7 = 1076.6`，而 `heat.maxTemperature` 由 `900 用火(+700)`、`200 陶器制作(+700)` 与所有 Metallurgy 字段自动 +700 叠加（`CapabilityChannels.GetBaseValue` 恒 0）。
- **`unlockRefs` 有 1 条坏引用**：革新 **918 麦类耕作** → `{kind:1(Good), id:0, key:""}`（`inno_badref.py` 输出）。`InnovationUnlockRefTests.cs:13-18` 的白名单 `Goods` 集合**包含 0**，所以这条坏引用能通过测试。
- `Recipes.json` 43 条配方、34 条 `requiredInnovation>0`、1 条 `requiredBuilding>0`，但 §8.2 已说明游戏内无调用方走门禁。
- `InnovationEra.StartYears`（`InnovationEra.cs:5`）与 JSON `era` 0–5 一一对应，无越界；`InnovationSchemaTests`/`InnovationSystemTests` 断言 `era<=5` 且前置 era 不晚于本体（`InnovationSystemTests.cs:143,147`）。

---

## 11. 问题 8：测试清单与"测的是真行为还是 schema"

共 11 个革新相关测试文件（另 `ResourceDepthTests.cs`、`SaveSystemRoundTripTests.cs`、`GameWorldReadModelTests.cs`、`UiReadModelExtensionTests.cs`、`ArchitectureContractsTests.cs` 含相关断言）。

| 测试文件 | 用例 | 测什么 | 是否覆盖真实游戏行为 |
| --- | --- | --- | --- |
| `InnovationSystemTests.cs` | 7（`37,82,97,115,126,134,153`） | 无 world 时只校科技前置、物产门槛阻断/放开、可用列表随 world 变、CancelResearch、可用列表排除已完成、内容完整性（成本/era/前置存在/无环） | **部分**：`InnovationTree` 直调，未过调度器/AI |
| `InnovationMaterialTests.cs` | 5（`50,62,107,144,160`） | capability 硬门槛、内容里 provider 必须在前置闭包内、软前置=能力阈值、Era 权重与年份映射、冶铁链 | 真行为（`InnovationTree` 层） |
| `InnovationPrerequisiteExpressionTests.cs` | 3（`62,81,97`） | All/Any 嵌套求值 + JSON 往返（含 `unlockRefs`） | 真行为（求值）；第 3 例仅序列化 |
| `InnovationPersistenceTests.cs` | 2（`74,97`） | 存读档后已研究集合 / 当前研究 / 研究点保留 | 真行为（含 `SaveSystem`） |
| `InnovationSchemaTests.cs` | 4（`32,53,81,110`） | schemaVersion≥2 必备字段、每个前置有 rationale、名称精确化黑名单、legacyIds/variantOf 指向有效 | **纯 schema/内容规范** |
| `InnovationMigrationTests.cs` | 5（`18,25,34,43,60`） | 1018 已移除、1016 依赖 1110、行政/经济/军工/技术簇改名的具体字符串 | **纯内容断言**（硬编码中文名，改文案即红） |
| `InnovationReviewPolicyTests.cs` | 2（`13,23`） | 6 个 id 冻结、6 个 id 不冻结 | 纯静态判定，且 `IsFrozen` 无运行时消费者 |
| `InnovationUnlockRefTests.cs` | 1（`44`） | 84 条 `unlockRefs` 的 id/key 指到"存在"的目标（白名单硬编码 + `RecipeCatalog.TryGet`） | schema 校验；白名单含 0 漏掉坏引用（§10.4） |
| `DiscoveryTests.cs` | 6（`24,41,61,76,88,106`） | discoveryGroups 合法性、分组匹配才发现、商/学各自领域、已掌握不进候选、聚落→人群映射 | **测的是死路径**（`InnovationDiscovery.TryDiscover` / `DiscoveryActors` 在运行期零调用，§5.2） |
| `ItemInnovationGateTests.cs` | 2（`41,52`） | 多革新 All 门槛、单革新兼容 | **测的是死路径**（`AreItemInnovationsMet` 零消费者） |
| `RecipeGateTests.cs` | 3（`38,48,55`） | 未掌握 202 时不能炼铁、旧三参重载不受影响、设施缺失阻断 | **测的是死路径**（6 参 `TryCraft` 零调用方） |
| 相关补充 | `ResourceDepthTests.cs:109-124`、`SaveSystemRoundTripTests.cs:134`、`GameWorldReadModelTests.cs:217-258`、`UiReadModelExtensionTests.cs:434-563`、`ArchitectureContractsTests.cs:51-58,243-254` | 资源深度、存读档后可用列表、查询契约一致性、事件总线 | 前两者含死路径（`CanDevelop`）；后两者一致 |

**缺口：完全没有任何测试覆盖真正在跑的那条链** —— `ResearchPlanSystem`（突破概率 / 计划创建 / `TryFormalizeResearch` 固化）、`ResearchPlanExecutor`（验证进度→解锁）、`InnovationResearchSchedule`（AI 研究点推进）、`AIIntentExecutor → StartInnovationResearchCommand`、`InnovationKnowledgeSystem` 熟练。grep `ResearchPlanSystem|ProcessPracticeBreakthroughs|RecordInnovationPractice` 在 `Assets\Tests` 下 **0 命中**。

---

## 12. 问题 9：完成度判定与缺口清单

### 12.1 一句话判定

**革新系统的"内容 + 前置求值 + 单一 AI 推进链"是可运行的半成品：内容层 186 条前置图干净可用，`InnovationTree` 的前置/能力/软前置判定真实有效，并在主循环里以「AI 选题 + 每日研究点」和「角色练习→突破计划→验证固化」两条链每日推进；但发现/进度/角色熟练/解锁清单四层基本是空转的数据结构，玩家没有任何入口，配方与物品门槛在游戏内根本不被调用，存档只保住政权级结果。**

### 12.2 缺口清单

| # | 缺口 | 证据 | 影响 |
| --- | --- | --- | --- |
| 1 | **玩家无入口**：无革新 UI、无 UI 侧命令；契约 `IInnovationQuery` 已实现但零消费 | `Assets\UI` 全目录 grep `Innovation|革新|研究|Research` = 0；`WorldReadModelExtensionContracts.cs:118-146` 有实现（`InnovationQueryAdapter.cs:15-157`）；`AIIntentExecutor.cs:33` 是唯一命令发送方 | 玩家不能选课题、看不到树；研究完全由 AI 代劳（推断：AI 不过滤 `PlayerRealmId`，`AIManager.cs:21-27`） |
| 2 | **`unlockRefs` / `unlocks` 无消费者** | `InnovationDef.cs:72,74` 定义；全仓仅定义处 + 1 处序列化测试；内容侧 84 条 `unlockRefs`、197 条 `unlocks` | 内容里精心标注的"解锁了哪些配方/建筑/法律/单位"对游戏零影响，文档与实现脱节 |
| 3 | **发现（Discovery）层不工作** | `_realmDiscovered`（`InnovationTree.cs:25`）唯一写入 `Discover:362-378`，唯一上游 `InnovationDiscovery.TryDiscover:23-35` 只被测试调用；`OnInnovationDiscovered`（`:33`）无订阅；`DiscoveryActors` 零调用 | "谁能发现什么"的规则（商人/工匠/学者）形同虚设；存档里 `discovered` 数组永远为空 |
| 4 | **进度（Progress）层不工作** | `MonthlyTickProgress`（`InnovationTree.cs:677-754`）零调用；`InnovationProgressConfig` 全常量零引用；`InnovationProgress` 缺 `realmId`（`InnovationProgress.cs:7-26`） | 精细的"生产实践/品质/研究者/设施/边际递减"公式全部白写；存档 `progress`、`resourceOutput` 两段恒空 |
| 5 | **角色熟练（Knowledge）永不提升** | `SetMastery`（`InnovationKnowledgeSystem.cs:31-50`）只被 `Learn:90` 调；`Learn` 只被 `ResearchPlanSystem.Learn:242-254` 调；后者零调用方 | 直接后果：`IsKnowledgeNeighbor:115-134` 要求"某个前置熟练≥2"，永远不成立 ⇒ **突破链只能发现 19 条根革新**（`inno-audit.txt:60` 根 id 列表），根革新学完后该链彻底停摆 |
| 6 | **`GetDirectPrerequisites` 把 Any 前置当 All 前置上报** | `InnovationTree.cs:444-451` 返回 `or = new List<int>()`；`GetPrerequisiteIds:83-111` 把 Any 下所有叶子展平；`InnovationQueryAdapter.cs:83-109` 直接拼两者 | 8 个含 Any 节点的革新（1–2 层）在前置展示/难度计算上被当成"全部必需"；一旦接 UI 会误导玩家（`GetLearningDifficulty:466-488` 同源问题） |
| 7 | **配方 / 物品 / 资源技术门槛在游戏内不被调用** | `SettlementProduction.TryCraft(6 参)`（`:20-23`，`enforceGates:true` 在 `40-50`）零调用方；`AreItemInnovationsMet:260-272` 零调用方；`ResourceAccess.CanDevelop:56-67` 仅测试；`TileResource.developed` 无任何赋值点 | 34/43 条配方的 `requiredInnovation` 白配；物品与资源门槛只活在测试里 |
| 8 | **法律革新要求永不生效** | `LawContracts.cs:215` 调 `BeginEnactLaw(..., null, ...)`；`LawAndDecreeSystem.cs:76-95` 因此短路；`LawSystem.CheckInnovationRequirements:116-131`、`GetMissingInnovations:133-149` 零调用方，且用 `string` 集合（`LawEffects.cs:398-435`）与 int 树不同源 | 法律对科技的依赖完全断裂；存在两套互不相通的"革新"命名体系 |
| 9 | **建筑"科技等级"与革新树无连接** | `BuildingAvailabilitySystem.cs:398-401`；`techLevel` 默认 0（`:290,409`）且 `CheckAvailability`/`GetAvailableBuildings` 无外部调用方 | 需要 `minTechLevel≥4` 的建筑（如 `:76`）永远不可用；即使接线也没有数据来源 |
| 10 | **Era 不构成解锁门槛** | `GetAvailableInnovations:453-464` 无 era 比较；`InnovationEra` 只用于 AI 排序（`AIController.cs:141,154-156`） | 时代只是"偏好权重"，玩家/AI 可以跳过时代直接研究晚期条目（只要前置满足） |
| 11 | **研究计划与熟练完全不进存档** | `InnovationTreeSaveData.cs:13-53`；`SaveData.cs:20-77`（无 plans / knowledge 段）；`SaveJsonMigration.cs:22-101` 无相关迁移 | 读档后所有角色的练习量、熟练、进行中的研究计划归零；突破链需从零重来（已固化的革新不丢） |
| 12 | **研究计划链零测试覆盖** | grep `ResearchPlanSystem|ProcessPracticeBreakthroughs|RecordInnovationPractice|ResearchPlans` 在 `Assets\Tests` 0 命中 | 唯一真正在跑的新链（`ResearchPlanSystem` + 执行器 + 调度）无回归保护 |
| 13 | **`StartResearch` 不检查"已在研究"，且研究点跨课题继承** | `InnovationTree.cs:176-196`：只查 `HasInnovation` 与前置，直接 `_realmCurrentResearch[realmId] = innovationId`（`:190`），不复位 `_realmResearchPoints`（`:191-194` 仅在键不存在时置 0）；`CancelResearch:434-442` 会复位 | 中途换课题会继承旧进度点，研究成本实质被摊薄；同时只能有一个课题的约束只靠 AI 的 `GetCurrentResearch != null` 早退（`AIController.cs:131-135`）隐性维持 |
| 14 | **`CharacterResearchManager` 整类未接线** | `CharacterResearchManager.cs:6-88`，全仓仅自身 2 处命中 | "研究者数量/效率/月度成本"这套设定完全没进模拟，`MonthlyTickProgress` 的 `researcherCount` 参数永远没有来源 |
| 15 | **内容层：能力需求与物产需求全空** | `inno-audit.txt:34,61-63`：`requiredCapabilities` 0 条、`requiredAnyResources/requiredAllResources` 0 条、`requiredCapabilityValues` 0 条 | 三条真实的门槛逻辑在 186 条内容上都空跑；只有 5 条软前置真实生效 |
| 16 | **内容层：1 条坏解锁引用被测试白名单掩盖** | 918 麦类耕作 `{kind:1, id:0, key:""}`（`inno_badref.py`）；`InnovationUnlockRefTests.cs:13-18` 的 `Goods` 含 0 | 内容校验形同虚设（且该字段本身无人消费，双重无效） |
| 17 | **`CapabilityChannels.GetBaseValue` 恒 0** | `CapabilityChannels.cs:7-11`（`_ = channel == "heat.maxTemperature"; return 0f;`） | 所有能力通道无基线，只能靠 `capabilityModifiers` 从 0 累加；新增通道（如 `mining.*` 的数值化）没有落脚点 |
| 18 | **元数据字段（12 个）无运行时读取方** | 见 §10.2 + `InnovationSchemaTests` 唯一消费 | 186 条内容的 definition/termEn/scope/category/sources/rationale 等只服务于自检测试与人工阅读 |

### 12.3 已验证 vs 未验证

**已验证（读代码 + 读数据可确证）**
- 分层与状态容器归属（§4 表，全部 file:line）；
- 三处调度注册与 order/频率（`GameWorld.Simulation.cs:835,837,838`、`AISchedule.cs:53`、`SimulationScheduler.cs:174-184`）；
- 死代码判定：`MonthlyTickProgress`、`Discover`、`InnovationDiscovery`、`DiscoveryActors`、`AreItemInnovationsMet`、6 参 `TryCraft`、`LawSystem.CheckInnovationRequirements`、`CharacterResearchManager`、`InnovationProgressConfig`、`GetLearningDifficulty/GetEffectiveResearchRate`、`ResearchPlanSystem.DailyTick`；
- 存档范围（含"progress/resourceOutput 恒空"的因果链）；
- 内容统计与前置图健康（无环、无悬空、186 条全 v2、软前置数据可解析）；
- 测试清单与覆盖性质。

**未验证（本会话无法执行）**
- 未运行 Unity Editor / EditMode 测试套件，因此 §11 的"测试测的是死路径"是指**被测 API 无游戏侧调用方**，不是断言这些测试会失败（它们会通过，只是保护的是死代码）；
- 未实测 `JsonUtility` 对"JSON 缺字段"是否保留 C# 字段初始化值（§10.3 已标推断）；
- 未实测玩家政权研究被 AI 托管的最终观感（§7 已标推断，依据是缺少 `PlayerRealmId` 过滤）；
- 未做运行时数值平衡评估（研究点速率 vs 成本区间 20–1500 天数的实际节奏）。

---

## 13. 遗留与风险

1. **两条并存的"研究"推进被混用**：`_realmResearchPoints`+`_realmCurrentResearch`（旧，AI 用）与 `ResearchPlanSystem`（新，角色突破用）共享同一个 `_realmInnovations` 与同一个 `currentResearch` 槽位（`ResearchPlanSystem.cs:295-299` 遇到占用直接返回 false 并把 `verificationProgress` 卡在 0.999，`ResearchPlanExecutor.cs:32-35`）。若 AI 长期占槽，突破计划会被无限期卡住——是否可接受需产品决策。
2. **`TryFormalizeResearch` 是"瞬间完成"**（`ResearchPlanSystem.cs:308`：`DailyTick(realmId, max(1, researchCost))`），研究点机制对计划链形同虚设；两条链的成本语义不一致（旧链按天累加、新链一次结清）。
3. **`InnovationProgress` 缺 `realmId`**（§5.3）：若后续启用 `MonthlyTickProgress`，进度会跨政权串味，属数据模型级返工点。
4. **删除/改名风险**：`InnovationMigrationTests.cs:34-68` 硬编码了 20 余条中文名断言，任何文案调整都会红；`PolityComponentInnovations.cs:21-83` 硬编码了 20 余个革新 id（302/500/501/502/503/504/505/601/603/952/959/960/980/994），与 JSON id 体系强耦合，改动内容 id 会静默改变政体可用性。
5. **`PolityComponentInnovations` 的语义是"或"不是"与"**：`IsComponentAvailable:151-170` 里多个 id 是"任一持有即可"（如 `C1Appointed = {959,960}`），命名容易误读为"都要"。
6. **`LawEffects` 的 string 革新名与 int 树彻底并存**（§12.2#8），若将来"接通"法律门槛，必须先决定以哪套为准，否则会出现双重标准。

---

## 14. 产物清单

| 路径 | 说明 |
| --- | --- |
| `reports/innovation-audit.md` | 本报告（唯一交付物） |
| `_fc_verify/inno_audit.py` → 输出 `_fc_verify/inno-audit.txt` | 只读统计：条目数、字段覆盖、前置图环/悬空、解锁分布（含首次 TypeError 记录） |
| `_fc_verify/inno_struct.py` → `_fc_verify/inno-struct.txt` | 前置节点结构异常统计（空分组/单子/leaf 带子） |
| `_fc_verify/inno_unlock.py` → `_fc_verify/inno-unlock.txt` | `unlockRefs` 按 kind 的 id/key 使用分布 |
| `_fc_verify/inno_badref.py` | 定位 id<=0 的坏解锁引用（命中 918 麦类耕作） |
| `_fc_verify/goods_check.py` | 软前置依赖物产的 `meltingPoint` 取值核对 |
| `_fc_verify/inno-head.txt`、`recipes.txt`、`goods_probe.py` | 辅助探针（Innovations.json 头部、Recipes.json 统计、Goods 路径探测） |

未修改任何游戏代码、内容数据或 `docs/`。
