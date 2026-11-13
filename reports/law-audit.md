# 法律系统真实实现状态审计

- 审计类型：只读代码审计（未修改任何游戏代码/文档）
- 项目根：`D:\Myriad-Creation-A-Phantasm-of-Passing-Aeons`
- 工程：`MyriadCreation`（Unity 6.3 / 6000.6.3f1）
- 审计时间：2026-11-09
- 结论口径：所有判断以代码字面为准；`docs/` 下计划文档未被采信。凡属推断均显式标注「推断」。

---

## 0. 一句话结论

**法律系统是"内容 + 状态机 + 结算已真实落地并进入主循环"，但司法/罪行/审判/判决这半条链路是死的（有规则无触发、有调度无对象），且法律定义的 13 个声明字段中 9 个从未被消费、4 组字段在 JSON→运行时克隆中被静默丢弃。**

具体：法律 Def/组/推行流水线/法令颁布与传播/效果结算 = 真实可运行；法律阶段机（LegalNormStage）、根本化、特许权授予、罪行生成、审判触发 = 未接通；最高法院/最高继承 `SupremeSuccessionLevel` 属另一层（LawComposition 政体组件层），是真实在用的。

---

## 1. 输入与前提

### 1.1 实际读取的代码文件（全部逐行读过）

Laws 核心层 `MyriadCreation\Assets\Scripts\Simulation\MyriadCreation.Simulation.Laws\`：
`LawAndDecreeSystem.cs`(541行)、`LawDef.cs`(63)、`LawGroupDef.cs`(22)、`LawRequirement.cs`(25)、`LawEffect.cs`(18)、`RealmLawState.cs`(135)、`RealmDecreeState.cs`(15)、`LawGroupState.cs`(17)、`PendingLaw.cs`(33)、`DecreeDef.cs`(30)、`DecreeOrder.cs`(44)、`LegalNormStage.cs`(18)、`LegalNormKind.cs`(23)、`ProposalAuthority.cs`(15)、`SupremeSuccessionLevel.cs`(49)、`TrialResult.cs`(19)、`Law.cs`(26)、`LawCatalog.cs`(40)、`LawCategory.cs`(16)、`LawEffects.cs`(519，读头部+结构)

LawHost 层 `MyriadCreation\Assets\Scripts\Simulation\MyriadCreation.Simulation.LawHost\`（**该目录确实存在，任务书未列出**）：
`LawRealmHost.cs`(391)、`JudicialSystem.cs`(145)、`LawContracts.cs`(282)、`LawDecreeView.cs`(223)、`LawEffectRuntime.cs`(192)、`LawContentLoader.cs`(173)、`DefaultLawCatalog.cs`(293)、`LawSystem.cs`(151，读前60)、`LawReform.cs`(103)、`DecreePropagationSystem.cs`(239)

入口/宿主/存档/UI：
`GameWorld.Simulation.cs`、`GameWorld.Succession.cs`、`GameWorld.Queries.cs`、`GameWorld.Core.cs`、`RealmData.cs`、`RealmDTO.cs`、`RealmLawContext.cs`、`SaveSystem.cs`、`SaveData.cs`、`GameSaveSystem.cs`、`GameSaveData.cs`、`GovernmentPanel.cs`、`UIManager.Panels.cs`、`UIManager.cs`、`SimulationScheduler.cs`、`SimulationCadence.cs`、`ThoughtManager.cs`

测试：`Assets\Tests\EditMode\` 下 14 个法律相关文件全部统计（含 `LawCatalogReference.cs`）

### 1.2 数据文件
`MyriadCreation\Assets\StreamingAssets\Base\Laws\Laws.json`（47965 字节，已用 PowerShell `ConvertFrom-Json` 解析统计，脚本输出见 §5）

### 1.3 方法说明
- 文件发现/内容检索用 `glob` / `grep` 工具（未使用 shell 的 find/grep）。
- JSON 统计与测试计数用 `pwsh`，输出重定向到 `E:\TEMP\*.txt`（**在仓库外**，未污染工程目录）后再读。
- 仓库内唯一新建文件是本报告。

---

## 2. 问题 1：法律系统由哪几层组成，实际存在哪些概念

代码里**同时存在两套互不共享数据的"法律"**，这是理解本系统的关键：

### 层 A：政体组件层（LawComposition）——真实在用，但与 Laws.json 无关

| 概念 | 位置 | 说明 |
|---|---|---|
| `LawComposition`（30+ 组件槽） | `LawComposition.cs:10-82` | 政体形态快照：君权/继承/中央机构/地方权限/军制/信仰/土地/婚姻/税收/关津…，每槽是 `ComponentChoice` 整数主组件+次组件 |
| `LawConstraints`（枚举总表） | `LawConstraints.cs`、`LawComposition.cs:305-402` | `LawGroup`(30+ 组)/`LawDomain`/`LawFamily` 的分类学 |
| `LawEffects.CalculateEffects` → `LawEffectData`（19 字段） | `LawEffects.cs:51`、`:10-49` | 由组件组合算出的效果包 |
| `SupremeSuccessionLevel`（最高继承判定） | `SupremeSuccessionLevel.cs:5-47` | 君主制/共和制/选举制判定 |
| 消费点 | `GameWorld.Simulation.cs:150`（行政容量）、`AIDecisionSystem.cs:40-41`（经济/军事倾向）、`SuccessionSystem.cs:27,154,204`（继承）、`PoliticalAccessAnalyzer.cs:19`（政治参与） | ✅ 真实消费 |

这层是**活的**：改 `realm.laws` 会真实影响 AI 决策与继承判定。

### 层 B：法律 Def 层（Laws.json + RealmLawState）——本次审计主体

| 概念 | 类型 | 位置 |
|---|---|---|
| 法律定义 | `LawDef`（21 字段） | `LawDef.cs:8-62` |
| 法律组定义 | `LawGroupDef` | `LawGroupDef.cs:7-21` |
| 法令定义 | `DecreeDef` | `DecreeDef.cs:7-29` |
| 前提条件 | `LawRequirement` | `LawRequirement.cs:7-24` |
| 效果 | `LawEffect`（**仅 5 字段**） | `LawEffect.cs:6-17` |
| 政权法律状态 | `RealmLawState` | `RealmLawState.cs:7-134` |
| 组状态 | `LawGroupState`（含 `stage`） | `LawGroupState.cs:6-16` |
| 推行中法律 | `PendingLaw` | `PendingLaw.cs:10-32` |
| 法令状态 | `RealmDecreeState`（active/cooldowns/orders） | `RealmDecreeState.cs:7-14` |
| 生效法令 | `ActiveDecree` | `ActiveDecree.cs` |
| 法令冷却 | `DecreeCooldown` | `DecreeCooldown.cs` |
| 法令传令命令 | `DecreeOrder`（18 字段，含传令官/行程/合规度） | `DecreeOrder.cs:6-43` |
| 规范类型 | `LegalNormKind`（6 值）+ `LegalScope`（5 值） | `LegalNormKind.cs:4-22` |
| 生命周期阶段 | `LegalNormStage`（8 值） | `LegalNormStage.cs:7-17` |
| 提案权 | `ProposalAuthority`（3 值） | `ProposalAuthority.cs:6-14` |
| 罪行类型 | `CrimeType`（16 值，见 `CrimeRegistry`） | `CrimeType.cs` |
| 罪行定义 | `CrimeDefinition`（含 behaviors/baseSeverity/defaultPunishment） | `CrimeDefinition.cs:10-18` |
| 罪行行为标签 | `CrimeBehavior` | `CrimeBehavior.cs` |
| 罪行静态表 | `CrimeRegistry`（16 条硬编码） | `CrimeRegistry.cs:9-260` |
| 罪行目标类别 | `CrimeTargetClass` | `CrimeTargetClass.cs` |
| 司法系统 | `JudicialSystem`（效率/腐败/执法/刑种开关/laws corpus） | `JudicialSystem.cs:12-144` |
| 法条 corpus | `Law` 结构体 + `LawCatalog.BuildCorpus` | `Law.cs:6-25`、`LawCatalog.cs:7-38` |
| 审判结果 | `TrialResult` | `TrialResult.cs:7-18` |
| 判决类型 | `VerdictType` | `VerdictType.cs` |
| 刑罚类型 | `PunishmentType`（在 `Content.Definitions`） | `JudicialSystem.cs:91-138` 引用 |

**注意层 B 的双重法条模型**：`LawCatalog.BuildCorpus(LawComposition)`（`LawCatalog.cs:14-37`）**完全不读 Laws.json**，而是遍历 `LawConstraints.LawGroup` 枚举生成"法律条文"，`lawId = list.Count + 1`（`:19`）、`isActive = true`（`:24`）恒真。也就是说司法系统眼中的"法律"是政体组件的投影，与玩家在政府面板推行的 `LawDef` 毫无关联。

### 层 C：编排/适配层

`LawRealmHost`（RealmData 侧入口，`LawRealmHost.cs:14`）、`LawContracts`（`ILawQuery`/`ILawCommand` 实现，`LawContracts.cs:15`）、`LawDecreeViewBuilder`（三段视图，`LawDecreeView.cs:49-222`）、`DecreePropagationSystem`（传令官传播，`DecreePropagationSystem.cs:12`）、`LawReform`（政变改革→法律同步，`LawReform.cs:8-41`）、`LawContentLoader`（JSON 加载，`LawContentLoader.cs:29`）、`DefaultLawCatalog`（回退集+索引+克隆）。

### 层 D：遗留/死层
`LawCategory`（`[Obsolete]`，`LawCategory.cs:5`）、`LawSystem`（从未实例化，见 §3.4）。

---

## 3. 问题 2：逐项真实度判定

判定标准：①方法体是否有实际逻辑（非空/非恒返回）；②是否被调度器或真实调用链到达；③是否写状态或产出被消费的效果。

### 3.1 真实可运行（有状态、有推进、有效果结算）

| # | 能力 | 证据 | 说明 |
|---|---|---|---|
| 1 | 法律可行性判定 | `LawAndDecreeSystem.cs:10-97` | 财政/威望/合法性/集权度/前置法/互斥/革新 7 类检查，逐条返回中文原因 |
| 2 | 推行流水线（已知→推行中→生效） | `LawRealmHost.cs:82-119`（登记+扣费）、`LawAndDecreeSystem.cs:435-460`（到期固化）、`GameWorld.Simulation.cs:802-805`（每日调度） | ✅ 真实状态机：`pendingLaws` 同组唯一（`LawRealmHost.cs:90-94`）、扣费 `:105-106`、到期 `SetActiveLaw` + 移除 pending（`:454-455`）、完成写编年史（`GameWorld.Simulation.cs:788`） |
| 3 | 推行取消 | `LawRealmHost.cs:144-165` | 校验发起人/首脑（`:158`），已扣成本不退 |
| 4 | 政变直改法律 | `LawRealmHost.cs:173-190` | 绕前置/授权，清同组 pending + 并入 known；真实接入 `RegimeChangeDynamics.cs:261` → `LawReform.cs:32` |
| 5 | 政体改革→法律映射表 | `LawRealmHost.cs:215-388` | 7 维度 × 多组件 → (groupId,lawId)，含 `charter_free_city`/`standing_army` 等，测试 `FactionLawChangeTests.cs:16` 覆盖全维度 |
| 6 | 提案权三分支 | `LawAndDecreeSystem.cs:506-537` | HeadOfState / CentralInstitution（含 `requiredInstitution` 匹配 `:516`）/ CharterHolders |
| 7 | 法令颁布（扣费+冷却+到期） | `LawAndDecreeSystem.cs:206-224`（扣财政/权威、`endDay=currentDay+max(1,durationDays)`、写 cooldown `:222`）、`:245-252`（到期清 active+cooldown）、`:226-243`（撤销） | ✅ 真实 |
| 8 | 法令下发传播（传令官制） | `DecreePropagationSystem.cs:58`（按聚落生成命令）、`:180-219`（合规度公式 `:206`：`0.45 + 管理/200 - 腐败*0.3 - 动荡*0.25 + 合法性*0.2`，五档结果）、`GameWorld.Simulation.cs:705-761`（每日 tick：传令官死亡→Failed、抵达→Delivered、Resolve、逐级下发）、`:840-843`（`decrees.propagation.daily` order 37） | ✅ 真实且有物理载体（MapActor 传令官） |
| 9 | 法律/法令效果结算 | `LawAndDecreeSystem.cs:254-294`（求和）、`LawEffectRuntime.cs:61-108`（合并/按天折算/夹紧写回 `realm.stability`/`realm.unrest`/`lawState.legitimacy`）、`:120-141`（按 currentYear 变化做年度 guard）、`GameWorld.Succession.cs:373-376` + `GameWorld.Simulation.cs:807-810` | ✅ 但仅 5 个字段（见 §3.3） |
| 10 | 效果乘子被真实生产消耗 | `PoliticsSimulationSystem.cs:86`（税收×）、`ManpowerSystem.cs:83`（人力×）、`Army.cs:160`（补员×） | ✅ |
| 11 | 司法效率日漂移 | `JudicialSystem.cs:140-143`（±0.001 随机夹紧 0.1~1）、调度 `GameWorld.Simulation.cs:830-833` → `:1217-1223` | ⚠️ 代码真实但对象集合恒空（见 3.3-8） |
| 12 | 审判判定与量刑 | `JudicialSystem.cs:64-89`（`judicialEfficiency*(1-corruption)*lawEnforcement*0.8` 对 `Random.value`）、`:91-138`（刑种开关降级链） | ⚠️ 规则真实但无触发方（见 3.3-9） |
| 13 | 罪行行为→罪行打分匹配 | `CrimeRegistry.cs:303-320+` | ⚠️ 有算法无调用方（见 3.3-10） |
| 14 | 最高继承/共和判定 | `SupremeSuccessionLevel.cs:7-47` 被 `SuccessionSystem.cs:27,154,204`、`PoliticalAccessAnalyzer.cs:19` 调用 | ✅ 真实（属层 A） |

### 3.2 只有数据结构/枚举，没有系统推进

| # | 概念 | 证据 | 判定 |
|---|---|---|---|
| 1 | `LegalNormStage`（8 阶段生命周期） | 枚举定义 `LegalNormStage.cs:7-17`；唯一使用点 `LawGroupState.cs:15` `public LegalNormStage stage = LegalNormStage.Active;`。全仓 grep **无任何赋值语句**（`SetActiveLaw` 只写 `activeLawId`/`enactedDay`，`RealmLawState.cs:47-65`） | **空壳**：字段恒为 `Active`，Unknown/Proposed/Pending/Consolidated/Fundamentalized/Repealed 永不出现。测试 `LegalNormLifecycleTests.cs:10-17` 只断言枚举序，`:25` 只断言默认值 = Active，等于**把"没实现"写成断言** |
| 2 | `LegalNormKind` / `LegalScope`（规范类型/适用层级） | 定义 `LegalNormKind.cs:4-22`；`LawDef.cs:37,40` 声明字段；全仓**无任何读取点**（仅 `LegalNormSchemaTests.cs:15-25` 赋值后自断言） | **空壳**：注释宣称"冲突排序用"（`LawDef.cs:36,42`），但无排序实现 |
| 3 | `RealmLawState.fundamentalizedLawIds`（根本化） | 声明 `RealmLawState.cs:20`（注释"普通法律/法令不能覆盖"，`LawDef.cs` 无对应实现）；全仓**无读无写**，仅 `LegalNormLifecycleTests.cs:23` 直接 `Add` 后断言 | **空壳** |
| 4 | `LawGroupDef.fallbackLawId`（兜底法） | 声明 `LawGroupDef.cs:20`（注释"未主动选择时生效"）；全仓**仅 schema 测试** `LawFallbackSchemaTests.cs:12-14`；`LawDecreeView`/`RealmLawState` 均不用 | **空壳**：组无生效法时不做任何兜底 |
| 5 | `LawRequirement.requiredPolityForm`（政体形式前置） | 声明 `LawRequirement.cs:23`；全仓**无消费**，仅 `LawFallbackSchemaTests.cs:21-22` | **空壳** |
| 6 | `LawDef.upkeepPerYear`（年维护费） | 声明 `LawDef.cs:28`；仅被 `DefaultLawCatalog.cs:232` 克隆、`LawContentJsonTests.cs:117` 断言相等；**无任何扣除/结算点** | **半空壳**：数据流通但无效果 |
| 7 | `LawDef.isFallback` | 声明 `LawDef.cs:34`；仅 `LawFallbackSchemaTests.cs:13-15`；且 `CloneLaw` 根本不复制它（`DefaultLawCatalog.cs:219-238`） | **空壳** |
| 8 | `charterHolderIds` / 特许权授予 | `IsCharterHolder` 被 `CanPropose` 使用（`RealmLawState.cs:108` ← `LawAndDecreeSystem.cs:527`）；但授予入口 `LawRealmHost.GrantCharter:41-49` **无任何生产调用者**（全仓仅测试 `LawDecreePipelineTests.cs:190,275,344`） | **空壳链**：`charterHolderIds` 在生产中永为空 → `ProposalAuthority.CharterHolders` 分支不可达 → JSON 里 `charter_free_city`（auth=2）、`local_succ_charter`（auth=2）**永久不可提案** |
| 9 | 罪行/案件（Crime）作为"事件源" | `CrimeRegistry` 16 条静态定义（`CrimeRegistry.cs:13-260`）；**全仓 `CrimeCase`/`CaseFile`/`caseId`/`CourtCase` 零命中**；无任何系统生成罪行 | **只有查表，没有案件实体**：不存在"谁犯了什么罪"的对象，因此审判无从发起 |
| 10 | `GameWorld.MatchCrimes` / `TryTrialCrime` | 定义 `GameWorld.Simulation.cs:1186-1189`、`:1191-1206`；全仓**无调用者**（仅定义处） | **死 public API** |
| 11 | 司法系统集合 | `GameWorld.Core.cs:203` `Dictionary<int,JudicialSystem> _judicialSystems`，唯一创建点 `GetOrCreateJudicialSystem`（`GameWorld.Queries.cs:262-281`），而它只被 `TryTrialCrime` 调用 | **集合恒空** → `DailyJusticeTick`（`GameWorld.Simulation.cs:1217-1223`）每日遍历 0 项；`RebuildJudicialLaws`（`:1208-1215`）同样无人调用 |
| 12 | `ThoughtManager` 的另一套 JudicialSystem | 字典 `ThoughtManager.cs:18`、创建 `:55-65`、日 tick `:90-93` | 创建方法**无调用者** → 该字典永远空，`DailyTick` 该段空转 |
| 13 | `LawSystem`（政体组件效果刷新器） | 全文 `LawSystem.cs:9-151`，含 30 秒刷新（`:50`）、`ApplyLawEffects` 写 `realm.centralization`（`:40`） | **死类**：全仓无 `new LawSystem(` 命中，从未实例化 → 层 A 的 `centralization`/`stability` 等 19 字段中除 3 个外全部算了不用 |
| 14 | `LawCategory` | `LawCategory.cs:5` `[Obsolete]` | 已废弃 |

### 3.3 字段级：声明了但从未被消费 / 被静默丢弃

这是本次审计**最有修复价值的发现**：`LawDef` 声明 21 个字段，运行时真正参与判定的只有 12 个；而 JSON 内容经 `DefaultLawCatalog` 克隆后，**又有 4 组字段被丢掉**。

| 字段 | 声明 | 运行时是否消费 | 克隆是否保留 |
|---|---|---|---|
| `lawId`/`groupId`/`name`/`description`/`tier` | `LawDef.cs:10-18` | ✅ | ✅ |
| `requirements`（部分） | `:20` | ✅（见下） | ⚠️ 部分丢失 |
| `effects` | `:22` | ✅ 5 字段 | ✅ |
| `enactmentCostTreasury/Prestige` | `:24-26` | ✅ `LawRealmHost.cs:105-106` | ✅ |
| `upkeepPerYear` | `:28` | ❌ 无结算点 | ✅ 仅克隆 |
| `isInitiallyKnown` | `:31` | ✅ `LawAndDecreeSystem.cs:405` | ✅ |
| `isFallback` | `:34` | ❌ | ❌ **丢失** |
| `normKind` | `:37` | ❌ | ❌ **丢失** |
| `scope` | `:40` | ❌ | ❌ **丢失** |
| `precedence` | `:43` | ❌ | ❌ **丢失** |
| `issuerCharacterId` | `:46` | ❌ | ❌ **丢失** |
| `overrides` | `:49` | ❌ | ❌ **丢失** |
| `excludesLawIds` | `:52` | ✅ `LawAndDecreeSystem.cs:65-75` | ❌ **丢失** ← **功能 bug** |
| `enactmentDays` | `:55` | ✅ `LawRealmHost.cs:104` | ✅ |
| `proposalAuthority` | `:58` | ✅ `LawAndDecreeSystem.cs:506` | ✅ |
| `requiredInstitution` | `:61` | ✅ `LawAndDecreeSystem.cs:516` | ✅ |

克隆函数实证：
- `DefaultLawCatalog.CloneLaw`（`:219-238`）只复制 `lawId,groupId,name,description,tier,requirements,effects,enactmentCostTreasury,enactmentCostPrestige,upkeepPerYear,isInitiallyKnown,enactmentDays,proposalAuthority,requiredInstitution` —— **无 `excludesLawIds`、无 `isFallback`、无 `normKind/scope/precedence/issuerCharacterId/overrides`**。
- `DefaultLawCatalog.CloneRequirements`（`:256-275`）只复制 `minTreasury/minLegitimacy/minCentralization/requiredLawIds/requiredInnovationIds` —— **无 `requiredDecreeIds`（`LawRequirement.cs:20`）、无 `requiredPolityForm`（`:23`）**。
- `DefaultLawCatalog.CloneDecree`（`:240-254`）**无 `excludesDecreeIds`（`DecreeDef.cs:28`）**。
- `DefaultLawCatalog.CloneGroups`（`:180-186`）只复制 `groupId/name/order/description` —— **无 `fallbackLawId`**。

**后果（已验证的代码路径）**：`LawRealmHost.BeginEnactLaw` 的 `law` 实参来自 `LawContracts.LawIndex()`（`LawContracts.cs:37-44` → `DefaultLawCatalog.IndexLaws(DefaultLawCatalog.CreateGroups())`），而 `CreateGroups()` 返回**克隆体**（`DefaultLawCatalog.cs:15-22` → `CloneGroups`）。因此传给 `CanEnactLaw` 的实例 `excludesLawIds` 恒为构造时的空 `List<string>`（`LawDef.cs:52` 默认值），`LawAndDecreeSystem.cs:65-75` 的互斥检查**恒不触发**。同理 `CanEnactDecree` 的前置法令（`:161-171`）与互斥法令（`:172-182`）对 JSON 来源的法令恒不触发。

> 该缺陷在测试中被掩盖：`LawAndDecreeConflictTests.cs:15,25,60` 直接 `new LawDef{excludesLawIds=...}` 构造实例，**绕过了克隆**，所以 3 个用例全绿却与实际运行路径不符。

### 3.4 另一个正交缺口：革新前置永不校验

`CanEnactLaw` 的革新检查被 `ownedInnovationIds != null` 短路（`LawAndDecreeSystem.cs:76`）。所有生产调用点都传 `null`：
- `LawDecreeView.cs:178`（可推行候选）
- `LawContracts.cs:215`（→ `LawRealmHost.BeginEnactLaw(...,null,...)`）
- `LawRealmHost.cs:82-100`（`ownedInnovationIds` 透传，实参恒 null）
- `CanEnactDecree` 同样：`LawDecreeView.cs:199`、`LawContracts.cs:244` 传 `null`

而 JSON 内容明确声明了革新前置（如 `standing_army` 需 995，见测试基准 `LawCatalogReference.cs:38`）。→ **内容声明了革新门槛，运行时等于没有**。

---

## 4. 问题 3：主循环调用与 tick 频率

### 4.1 调度注册点（`Simulation\MyriadCreation.Simulation.WorldState\GameWorld.Simulation.cs`）

`RegisterSimulationSchedules()` 定义在 `:792`，法律相关共 **4 条注册**，**全部真实注册成功**：

| 调度 id | cadence | order | 注册行 | 执行体 | 频率 |
|---|---|---|---|---|---|
| `laws.effects.yearly` | Daily | 24 | `:807-810` | `TickLawEffectsYearly()` (`GameWorld.Succession.cs:373-376`) | 每日 tick，内部按 `currentYear` 变化做 guard（`LawEffectRuntime.cs:120-141`）→ **实际每游戏年 1 次** |
| `laws.pending.daily` | Daily | 26 | `:802-805` | `TickPendingLawsDaily()` (`:775-790`) → `LawRealmHost.TickAllPendingLaws` (`:781`) → `LawAndDecreeSystem.TickPendingLaws` | **每游戏日 1 次** |
| `justice.daily` | Daily | 27 | `:830-833` | `DailyJusticeTick()` (`:1217-1223`) → 遍历 `_judicialSystems` 调 `DailyTick` | 每游戏日 1 次，但**集合恒空 → 空转** |
| `decrees.propagation.daily` | Daily | 37 | `:840-843` | `TickDecreeOrders()` (`:705-761`) | **每游戏日 1 次** |

`SimulationCadence.Daily` 的语义：`SimulationScheduler.GetIntervalDays`（`SimulationScheduler.cs:174-184`）返回 `1`（`Weekly=7/Monthly=30/Seasonal=90/LongTerm=365`，`_:1`）。主 tick 入口 `GameTick()`（`GameWorld.Simulation.cs:764-769`）→ `TickBudgeted(...)`，带预算治理（`:767` `_budgetGovernor.NextBudget`）。

### 4.2 结论

- ✅ **`LawAndDecreeSystem` 的法律流水线（推行→固化）与法令到期、法令传播确实被 tick**，间隔 = 1 游戏日。
- ✅ 效果结算确实被 tick，间隔 = 1 游戏年（Daily 注册 + 年份 guard）。
- ❌ **`LawAndDecreeSystem` 本身不是被 tick 的对象**——它是纯静态规则类，无 `Tick` 方法，只被上面 4 个宿主方法调用。任务书设想的"LawAndDecreeSystem 的 Schedule"不存在，实际是 4 条宿主侧匿名委托。
- ❌ **审判链路无调度**：没有任何 `case`/`trial` 的 Schedule；`TryTrialCrime` 只能被外部主动调用，而它无调用者。
- ⚠️ `justice.daily` 注册了但对象不存在，属于"调度空转"。

---

## 5. 问题 4：存档

### 5.1 走的是哪条存档路径（关键前提）

工程里有**两套**存档实现，必须区分：

| 实现 | 位置 | 状态 |
|---|---|---|
| `SaveSystem`（JsonUtility + DTO，`.sav`） | `Infrastructure\MyriadCreation.Infrastructure.Save\SaveSystem.cs:33` | ✅ **在用**：`GameManager.cs:279` 存、`:287` 读；`SaveLoadPanel.cs:119-120` 列表 |
| `GameSaveSystem`（`RealmBasicSaveData[]`） | `GameSaveSystem.cs:39-40` | ❌ `[System.Obsolete("Legacy save path; use SaveSystem. Not wired into the game loop.")]` |

因此只有 `SaveSystem` 的语义有意义。

### 5.2 法律状态：已入档

链路：`SaveSystem.SaveGame` → `data.realms` 为 `List<RealmDTO>`（`SaveData.cs:39`）→ 逐政权 `RealmDTO.FromRealmData`（`SaveSystem.cs:384`）→ DTO 直接持有 `lawState`/`decreeState` 引用（`RealmDTO.cs:20-22`、`FromRealmData` 赋值 `:80-81`）。读档 `SaveSystem.cs:426-428` → `dto.ToRealmData()`（`RealmDTO.cs:130-179`，`:137-138` 回填）。

JsonUtility 会递归序列化 `[Serializable]` 嵌套类，故以下字段**全部入档**：

| 字段 | 位置 | 入档 |
|---|---|---|
| `RealmLawState.groups[].{groupId,activeLawId,enactedDay,stage}` | `RealmLawState.cs:9` / `LawGroupState.cs:8-15` | ✅ |
| `RealmLawState.legitimacy / authority` | `:11,13` | ✅ |
| `knownLawIds` | `:17` | ✅ |
| `fundamentalizedLawIds` | `:20` | ✅（存了但无人用，见 3.2-3） |
| `pendingLaws[]`（lawId/groupId/startDay/daysToEnact/proposerCharacterId/status） | `:23` / `PendingLaw.cs:12-26` | ✅ |
| `charterHolderIds` | `:26` | ✅（存了但无授予入口） |
| `RealmDecreeState.active[]`（decreeId/startDay/endDay） | `RealmDecreeState.cs:9` / `ActiveDecree.cs` | ✅ |
| `RealmDecreeState.cooldowns[]` | `:11` | ✅ |
| `RealmDecreeState.orders[]`（18 字段含 `courierActorId`/`progressDays`） | `:13` / `DecreeOrder.cs:8-42` | ✅ |
| `LawComposition`（层 A 全量） | `RealmDTO.cs:18` | ✅ |

读档侧还有旧档兼容：`RealmDTO.NormalizeLegacyFields`（`:64-71`，`composition`→`laws`）、`LawCompositionMigration.MigrateIfEmpty`（`RealmDTO.cs:177` → `LawCompositionMigration.cs:54-56`）。

### 5.3 丢失的部分（明确清单）

| 丢失对象 | 证据 | 影响 |
|---|---|---|
| **`JudicialSystem` 全部**（`judicialEfficiency`/`corruption`/`lawEnforcement`/`severity`/6 个刑种开关/`laws` corpus/`source`） | `_judicialSystems` 是 `GameWorld` 的 `readonly Dictionary`（`GameWorld.Core.cs:203`），`Infrastructure\...\Save\` 目录下 grep `LawPersistence|LawSaveData|RealmLawState|JudicialSystem` → **0 命中** | 读档后司法参数回默认（`JudicialSystem.cs:22-38` 硬编码 0.5/0.2/0.5/0.5 + 6 个 true），任何在局内漂移过的效率丢失。当前因集合恒空，实际无可见损失（推断：一旦接通审判就会立刻暴露） |
| **罪行/案件** | 全仓无案件类型（§3.2-9） | 无对象可丢，也无可存档 |
| `ThoughtManager` 的司法/信仰/学派字典 | 未见于 `SaveData`；`ThoughtManager.cs:18` 私有字典 | 信仰有独立 `FaithPersistence.cs`，但司法无对应持久化 |
| `LawEffectRuntime._lawById/_decreeById` 缓存 | `LawEffectRuntime.cs:33-35` static | 无需入档（由 `LawContentLoader` 重建），但 `_lastLawEffectsYear`（`GameWorld.Succession.cs:367`）也未入档 → 读档后首年可能重复结算一次（推断，年 guard 用 `int.MinValue` 初值） |

`:204` 另有一处相关空壳：`RealmData.GetEffectiveSuccessionLaw()`（`RealmData.cs:109-116`）是正确实现，但 `GameWorld.Succession.cs:362-365` 的 `GetEffectiveLaw(realmId)` **恒返回 `InheritanceLaw.Primogeniture()`**，`:357-360` 的 `GetRealmDivisibleEstates` **恒返回 1** —— 大圣战结算（`GameWorld.Simulation.cs:424`）因此忽略真实继承法。

---

## 6. 问题 5：内容层 Laws.json 与 schema 一致性

### 6.1 规模（PowerShell 实测）

```
GroupCount: 12      TOTAL_LAWS: 49      DecreeCount: 2      SizeBytes: 47965
groups: taxation(3) military(4) military_organization(3) privilege(2) succession(4)
        supreme_scope(5) central_succession(4) local_succession(4) local_scope(4)
        spatial(3) military_admin(8) border(5)
```

> **文档腐烂证据**：三处注释仍写"9 组 / 32 法 / 2 法令"——`LawContentLoader.cs:24`、`DefaultLawCatalog.cs:12`、`LawCatalogReference.cs:8`。实际为 **12 组 / 49 法**。代码是对的（测试 `LawContentJsonTests.cs:58-61` 断言 12/49/2 通过），**注释是过期的**。

### 6.2 schema 一致性

`LawCatalogFile`（`LawContentLoader.cs:15-20`）= `{groups: List<LawGroupDef>, decrees: List<DecreeDef>}`，字段名与 C# public 字段一一对应（注释 `:12` 声明此约定），JsonUtility 按名匹配。

| 层级 | JSON 实际出现的字段（实测） | C# 声明 | 结论 |
|---|---|---|---|
| group | `groupId, name, order, description, laws` | `LawGroupDef.cs:9-17` + `fallbackLawId:20` | 等价（`fallbackLawId` JSON 未用，代码也不消费） |
| law | `lawId, groupId, name, description, tier, requirements, effects, enactmentCostTreasury, enactmentCostPrestige, enactmentDays, isInitiallyKnown, proposalAuthority, requiredInstitution, upkeepPerYear` | `LawDef.cs:8-62`（21 字段） | 见 §6.3 |
| requirements | `minTreasury, minLegitimacy, minCentralization, requiredLawIds, requiredInnovationIds` | `LawRequirement.cs:9-17` + `requiredDecreeIds:20, requiredPolityForm:23` | JSON 未用后两者（代码也不消费） |
| effects | `taxMultiplier, stabilityPerYear, legitimacyPerYear, manpowerMultiplier, unrestPerYear` | `LawEffect.cs:8-16` | ✅ **完全一致（5/5）** |
| decree | `decreeId, name, description, requirements, effects, costTreasury, costAuthority, durationDays, cooldownDays` | `DecreeDef.cs:9-25` + `excludesDecreeIds:28` | 一致（`excludesDecreeIds` 未用） |

**JSON 有 / 代码不读**：`upkeepPerYear`（49 条法律全部带，无任何结算点）、`requiredInnovationIds`（JSON 有值，运行时因 `ownedInnovationIds=null` 永不校验）。
**代码要求 / JSON 没有**：无硬缺失。JsonUtility 缺字段即取默认值，且测试 `LawContentJsonTests.cs:108-135` 逐字段比对通过（含 `upkeepPerYear:117`），说明当前 JSON 与 C# 默认值自洽。
**代码有 / 克隆时丢**：`excludesLawIds`、`requiredDecreeIds`、`excludesDecreeIds`、`isFallback`、`normKind`、`scope`、`precedence`、`issuerCharacterId`、`overrides`、`fallbackLawId`、`requiredPolityForm`（详见 §3.3）。

### 6.3 内容质量问题（实测）
- `military_organization` 与 `military_admin` **order 都是 21**，`border` 是 22 → `LawDecreeViewBuilder` 的排序（`LawDecreeView.cs:65`）在相同 order 下顺序不稳定（推断：`List.Sort` 非稳定排序，表现为面板分组顺序随机）。
- `military` 组在 JSON 有 4 条法，回退集只有 2 条（`DefaultLawCatalog.cs:45-55`）——JSON 缺失时 `feudal_host`/`mercenary_army` 消失，`TryMapReformToCatalogLaw` 映射仍指向 `standing_army`（`LawRealmHost.cs:308-313`）故不炸，但 `FactionLawChangeTests` 覆盖的其他 lawId 会落空。

---

## 7. 问题 6：UI

### 7.1 有完整可操作面板（已验证入口可达）

`GovernmentPanel`（`Assets\UI\Panels\GovernmentPanel.cs`），**不是只读展示**，有 5 段内容 + 4 类按钮：

| 段落 | 行 | 控件 |
|---|---|---|
| 已固化的制度 | `:298-300` | 只读文本 |
| 正在生效的法令 | `:302-305` | **「撤销」按钮** → `TryRevokeDecree` (`:365-373`) |
| 正在推行的法令 | `:307-313` | **「取消」按钮** → `TryCancelPendingLaw` (`:355-363`) |
| 可推行的制度 | `:315-323` | **「推行：X」按钮** → `TryBeginEnactLaw` (`:335-353`) |
| 可颁布的法令 | `:325-332` | **「颁布：X」按钮** → `TryEnactDecree` (`:375-389`)，成功后还调 `DecreeCommand.IssueDecreeOrders`（`:385`） |

刷新时先做三件事（`:287-289`）：`EnsureKnownLaws` → `TickPendingLaws` → `TickDecreeExpiry`，即**打开面板会顺带补 tick**（双保险，也说明主循环 tick 曾经缺失过）。

**入口可达性**：`UIManager.Panels.cs:363` 有政府按钮 `_governmentOpenButton.onClick → ToggleGovernmentPanel()`；`:706-736` `EnsureGovernmentPanel` 支持场景查找或运行时 new；`:738-744` `OpenGovernmentPanel`。服务注入链 `UIManager.cs:106-132` → `GovernmentPanel.cs:30,45`。测试 `UiDecouplingTests.cs:312,330` 用反射验证按钮回调。

### 7.2 完全没有入口
- **罪行 / 审判 / 判决 / 司法**：`UI\` 目录 grep `Law|Decree|Crime|Judicial|Trial` 共 63 命中，**`Crime`/`Judicial`/`Trial` 命中数 = 0**。玩家无法查看任何罪行、案件、审判结果、司法参数。
- **`LegalNormStage` / `normKind` / `scope` / 根本化**：UI 无任何呈现。
- **`upkeepPerYear`**：面板不显示。
- 面板刻意隐藏分类信息（`LawDecreeView.cs:30` 注释 + 测试 `LawDecreeView_ThreeSegments_NoGroupLeak`），故玩家也看不到组名/域族。

---

## 8. 问题 7：测试

### 8.1 清单（`Assets\Tests\EditMode\`，14 个文件，71 个测试特性，其中 `[SetUp]/[TearDown]` 4 个 → **67 个 `[Test]` 用例**，无 `[TestCase]`/`[UnityTest]`）

| 文件 | 行数 | 用例数 | 测的是真实行为还是 schema |
|---|---|---|---|
| `LawAndDecreeTests.cs` | 110 | 6 | ✅ **真实行为**：扣费、单组唯一、法令过期/冷却、前置法、撤销、效果只算生效法 |
| `LawAndDecreeConflictTests.cs` | 65 | 3 | ✅ 真实行为（互斥/前置法令）——**但走 `new LawDef` 直构，绕过克隆，掩盖 §3.3 缺陷** |
| `LawDecreePipelineTests.cs` | 364 | 12 | ✅ **真实行为**（最扎实的一个）：未知法不可提案、pending 期旧法有效、到期才替换、同组唯一、取消权限、提案权矩阵、ForceSetLaw 绕过、三视图不泄漏组、RealmDTO 往返 |
| `LawEffectRuntimeTests.cs` | 348 | 13 | ✅ **真实行为**：税收/人力乘子、年度效果叠加、过期法令不计、按天折算、年 guard 抗跳日、`SettleTaxes`/`ManpowerSystem`/`ReinforceUnit` 真实消费、catalog 可切换 |
| `FactionLawChangeTests.cs` | 215 | 7 | ✅ **真实行为**：政变映射表全 7 维度、Composition 不被篡改、无映射不改法 |
| `DecreePropagationTests.cs` | 114 | 6 | ✅ 真实行为：命令生成、行程天数、目标为聚落非行政区中心、经费来源、Resolve 两分支 |
| `LawModuleTests.cs` | 77 | 6 | ⚠️ **混合**：corpus 覆盖/family 派生/crime 引用 = schema 与派生；`:63-64` 直接构造 `JudicialSystem` 测 `TrialCrime` 返回 `Dismissed`（**只覆盖 1 个分支，非 0 条真实链路**） |
| `LawContentJsonTests.cs` | 242 | 3 | ❌ **纯 schema/序列化**：12 组 49 法 2 法令计数、逐字段与测试基准等值、回退集、损坏回退、幂等 |
| `LawFallbackRobustnessTests.cs` | 172 | 2 | ⚠️ schema + **空值健壮性**（null 列表不抛、未知 lawId 返回中性、旧 LawComposition 迁移） |
| `LawFallbackSchemaTests.cs` | 25 | 2 | ❌ **纯 schema**：`isFallback`/`fallbackLawId`/`requiredPolityForm` 赋值后自断言——**测的是"字段存在"，不是"字段生效"** |
| `LegalNormSchemaTests.cs` | 36 | 2 | ❌ **纯 schema**：`normKind/scope/precedence/issuerCharacterId/overrides` 赋值后自断言 + 枚举序 |
| `LegalNormLifecycleTests.cs` | 28 | 2 | ❌ **纯 schema**：只断言枚举大小序 + `stage` 默认值 = Active —— 等价于测试"生命周期机未实现" |
| `MilitaryLawTaxonomyTests.cs` | 40 | 3 | ❌ **纯 schema**：组→域族映射与组名文案 |
| `LawCatalogReference.cs` | 256 | 0 | 测试基准数据（非测试类），自述"仅用于校验 JSON 逐项一致，不参与运行时"（`:8-10`） |

### 8.2 统计结论

- 真实行为测试：**6 + 3 + 12 + 13 + 7 + 6 ≈ 44 个用例**（占 66%），集中在前 6 个文件。
- 纯 schema/序列化/枚举序测试：**3 + 2 + 2 + 2 + 3 ≈ 12 个用例**（占 18%），且这 12 个**全部在测"声明存在"而非"逻辑生效"** —— 它们让 §3.2/§3.3 的空壳字段看起来"有测试覆盖"。
- 空白区：**0 个用例覆盖** `SaveSystem`（磁盘 `.sav`）里的法律往返；`LawDecreePipelineTests.cs:262-292` 的"往返"是进程内 `JsonUtility` 直转，未经过 `SaveSystem.SaveGame/LoadGame`。`_judicialSystems`、`ThoughtManager` 司法、`TryTrialCrime` 的 Guilty/NotGuilty 分支、`MatchCrimes` 打分、`charterHolderIds` 授予链均**无测试**。

---

## 9. 问题 8：完成度与缺口清单

### 9.1 一句话判定

**内容 + 状态机 + 效果结算已具备并真实进入主循环（每游戏日 tick，法律 49 条 / 法令 2 条 / 12 组），但司法—罪行—审判—判决整条支链 0% 接通（无案件实体、无触发方、调度空转），且法律 Def 的 13 个"高级"字段中 9 个从未被消费、4 组在 JSON→运行时克隆中被静默丢弃。**

按子系统打分（以"玩家可感知的真实行为"为口径）：

| 子系统 | 状态 |
|---|---|
| 法律定义内容（49 条） | 90% —— 内容齐、schema 自洽；`upkeepPerYear` 无效果 |
| 法律推行流水线 | 85% —— 完整状态机 + 每日 tick + 编年史；缺革新前置校验、缺互斥（克隆丢字段） |
| 法令 | 80% —— 颁布/冷却/到期/撤销/传令官传播全真实；缺"命令是否执行"对效果的门控 |
| 效果结算 | 70% —— 5 字段真实消费 3 处生产结算点；层 A 的 19 字段只消费 3 个 |
| 法律阶段/规范层级 | **5%** —— 枚举 + 字段，零推进 |
| 提案权 | 60% —— 三分支代码真实，但 CharterHolders 分支不可达（无授予入口） |
| 政体组件 ↔ 法律同步 | 75% —— 政变映射表 7 维度真实接线；单向（组件→法律），法律→组件无回写 |
| 罪行 | **15%** —— 16 条静态定义 + 匹配算法，无案件实体、无生成者 |
| 审判 | **10%** —— 判定/量刑规则真实，零调用方 |
| 判决 | **10%** —— 数据结构 + 刑种降级链，零调用方，无后果结算 |
| UI | 70% —— 法律/法令完整面板可操作；司法/罪行 0 入口 |
| 存档 | 85% —— lawState/decreeState 全字段入档；司法系统不入档 |
| 测试 | 55% —— 行为测试 44 例扎实；schema 测试 12 例掩盖空壳；存档/司法/罪行 0 覆盖 |

**综合：约 55%**（"法律立法与施行"半条链可用，"司法裁判"半条链未通电）。

### 9.2 缺口清单（每条：缺口 → 证据 → 影响）

| # | 缺口 | 证据（file:line） | 影响 |
|---|---|---|---|
| G1 | 克隆丢 `excludesLawIds`，法律互斥永不生效 | `LawDef.cs:52` 声明；`LawAndDecreeSystem.cs:65-75` 消费；`DefaultLawCatalog.cs:219-238` 克隆未复制 | 高：可同时生效互斥法（如绝对君权+法理受限），且测试全绿掩盖问题 |
| G2 | 克隆丢 `requiredDecreeIds` + `excludesDecreeIds`，法令前置/互斥永不生效 | `LawRequirement.cs:20`、`DecreeDef.cs:28`；消费 `LawAndDecreeSystem.cs:161-182`；克隆 `DefaultLawCatalog.cs:240-275` | 高 |
| G3 | 革新前置永不校验（`ownedInnovationIds` 恒 null） | `LawAndDecreeSystem.cs:76` 短路；调用点 `LawDecreeView.cs:178`、`LawContracts.cs:215,244` 传 null；JSON 有值（`LawCatalogReference.cs:38`） | 中：内容声明的门槛形同虚设，任意制度可直推 |
| G4 | `LegalNormStage` 状态机未实现 | `LawGroupState.cs:15` 默认 Active；全仓无赋值；测试仅断言枚举序 `LegalNormLifecycleTests.cs:10-17` | 中：Unknown/Proposed/Consolidated/Fundamentalized/Repealed 六态不存在，无法表达"法律被根本化" |
| G5 | `fundamentalizedLawIds` 无读写 | `RealmLawState.cs:20`；全仓零消费 | 中：注释承诺"普通法律/法令不能覆盖"，无实现 |
| G6 | 特许权授予链断裂 → `CharterHolders` 提案权不可达 | `LawRealmHost.cs:41-49` 无生产调用者；`ProposalAuthority.cs:13`；JSON `charter_free_city`/`local_succ_charter` auth=2 | 中：2 条法律永久不可提案 |
| G7 | 罪行无案件实体、无生成者 | 全仓 `CrimeCase/CaseFile/caseId/CourtCase` 零命中；`CrimeRegistry.cs` 仅静态表 | **高**：罪行系统停留在词表 |
| G8 | `MatchCrimes` / `TryTrialCrime` 零调用者 | `GameWorld.Simulation.cs:1186-1189`、`:1191-1206` 仅定义 | 高：审判在游戏中永不发生 |
| G9 | `_judicialSystems` 恒空 → `justice.daily` 空转 | `GameWorld.Core.cs:203`；唯一创建点 `GameWorld.Queries.cs:262` 只被 `TryTrialCrime` 调用；调度 `GameWorld.Simulation.cs:830-833` | 高：注册了调度但无对象 |
| G10 | `JudicialSystem` 不入档 | `Infrastructure\...\Save\` 下 0 命中 `JudicialSystem`/`LawPersistence` | 中：一旦接通审判，司法参数与 corpus 读档即丢 |
| G11 | 法令效果不看命令执行结果 | `LawAndDecreeSystem.cs:275-294` 仅按 `currentDay < endDay` 计入；`DecreePropagationSystem` 的 Enforced/Refused/Delayed（`DecreePropagationSystem.cs:208-219`）不参与 | 中：传令官半路死亡，法令效果照样满额生效 |
| G12 | `LawSystem` 死类 | `LawSystem.cs:9-151` 无 `new LawSystem(` 命中；其 `centralization` 回写（`:40`）从不执行 | 中：层 A 19 字段效果除 3 个外全废 |
| G13 | 层 A 与层 B 无双向同步 | 政变→法律单向（`LawReform.cs:32`）；法律→组件无路径 | 中：玩家在面板推行"世袭继承"，实际继承规则（`RealmData.GetEffectiveSuccessionLaw` `:109-116`）不变 |
| G14 | 大圣战忽略真实继承法 | `GameWorld.Succession.cs:362-365` 恒 `Primogeniture()`；`:357-360` 恒 `1` | 中：与 G13 同源但独立存在 |
| G15 | `fallbackLawId`/`requiredPolityForm`/`isFallback`/`normKind`/`scope`/`precedence`/`issuerCharacterId`/`overrides` 8 字段纯声明 | `LawGroupDef.cs:20`、`LawRequirement.cs:23`、`LawDef.cs:34-49`；仅 schema 测试引用 | 低-中：schema 过度设计，误导后续开发者以为已有排序/兜底/政体过滤 |
| G16 | `LawCatalog` 从组件枚举伪造法条，与 Laws.json 无关 | `LawCatalog.cs:14-37`（`lawId = list.Count+1`、`isActive = true` 恒真） | 中：司法系统"引用哪条法"与立法系统完全脱节 |
| G17 | 无 `SaveSystem` 级法律往返测试 | `LawDecreePipelineTests.cs:262-292` 是进程内 JsonUtility；`Infrastructure` 侧 0 法律测试 | 中：`SaveSystem` DTO 演进可静默破坏法律存档 |
| G18 | 12 个 schema 测试伪覆盖 | `LegalNormSchemaTests.cs`、`LegalNormLifecycleTests.cs`、`LawFallbackSchemaTests.cs`、`MilitaryLawTaxonomyTests.cs` | 中：空壳字段有"测试覆盖"假象 |
| G19 | 注释与内容脱节（9组/32法 vs 12组/49法） | `LawContentLoader.cs:24`、`DefaultLawCatalog.cs:12`、`LawCatalogReference.cs:8` | 低：误导内容维护 |
| G20 | JSON 组 `order` 重复（`military_organization` 与 `military_admin` 均 21） | JSON 实测；排序 `LawDecreeView.cs:65` | 低：面板分组顺序不稳定（推断） |
| G21 | 玩家无法看到任何罪行/审判/司法信息 | `UI\` 下 `Crime|Judicial|Trial` 0 命中 | 高（对玩家可感知度而言）：即使接通也无可视化 |
| G22 | 法律 upkeep 不扣钱 | `LawDef.cs:28`、`DefaultLawCatalog.cs:232`、`LawContentJsonTests.cs:117`；无结算点 | 低-中：财政压力模型缺一环 |

### 9.3 建议的修复顺序（按"性价比 / 阻塞关系"）

1. **G1+G2（半小时级，最高性价比）**：在 `CloneLaw/CloneRequirements/CloneDecree/CloneGroups` 补全 4 组字段的深拷贝，并加一条"克隆后逐字段等价"的守卫测试（可直接复用 `LawContentJsonTests.AssertLawEqual` 的思路扩展到全部 21 字段）。修完互斥/前置法令立即生效。
2. **G18+测试策略**：把 4 个纯 schema 测试改为行为测试，或明确标记 `[Category("Schema")]`，避免继续积累伪覆盖。
3. **G17**：加 `SaveSystem.SaveGame/LoadGame` 的法律往返测试（`RealmLawState` + `RealmDecreeState` + `DecreeOrder`），这是 G10 的前置。
4. **G7+G8+G9+G21 一起做**：司法支链需要一次"案件实体 → 罪行生成 → 审判调度 → UI 呈现 → 入档"的纵向切片，缺任一环都是空转。
5. **G4+G5+G6**：生命周期机需要真实驱动（谁把 Active 推到 Consolidated/Fundamentalized、需要什么条件），否则不如把这两个字段从 schema 里删掉，减少误导。
6. **G13+G14+G16**：层 A/层 B 关系要么合并要么明确"组件层是权威、Def 层是展示"，否则每次改动都在两套法条模型间打架。

---

## 10. 遗留与风险（未验证项）

以下属于**本次未验证**，不应被当作事实：

1. **未运行编译器/测试**：本次为纯静态审计，未执行 Unity batchmode 编译或 EditMode 测试套件。所有"测试通过"的表述来自读测试源码断言值（如 `LawContentJsonTests.cs:58-61` 断言 12/49/2 与 JSON 实测一致），**非**实跑结果。
2. **`DecreeOrder` 的 `courierActorId` 读档有效性未验证**：`MapActor` 的 actorId 是否在 `SaveSystem` 里与 `decreeState.orders[].courierActorId` 保持一致，未追踪 `MapActorManager` 存档链路。若不一致，读档后 `TickDecreeOrders` 会在 `GameWorld.Simulation.cs:721-727` 把所有命令判 `Failed`（推断，风险点）。
3. **`ThoughtManager` 是否被 `SaveSystem` 序列化**：只确认其内部私有字典不在 `SaveData` 里，未追查 ThoughtManager 实例本身的持久化方式（其信仰部分有 `FaithPersistence.cs`，司法部分无）。
4. **`PolityComponentInnovations.cs` 未读**：`LawReform.CanReform` 依赖它的 `IsComponentAvailable` 判定前置是否真实生效，未审计。
5. **`ILawContext.cs` 未逐行读**：只通过 `LawRealmContext.cs:28,32` 与 `CanPropose` 调用点确认三个成员（`GetSupremeRulerId`/`IsCentralOfficeHolder`/`Legitimacy`）存在，未核对其全部契约方法。
6. **`docs/` 未采信**：按要求未用任何计划文档当结论，因此无法判断上述缺口是"计划中未做"还是"做了又退化"。
7. **根因推断**：`LawAndDecreeSystem.cs:424-430` 有 6 行连续空行（方法被删除的痕迹），`LawSystem.cs` 整类失联，`clone` 集体漏字段 —— 这三处**推断**为一次"Laws 从 DefaultLawCatalog 硬编码目录迁移到 JSON 内容"（`LawContentLoader.cs:24` 注释所述）时的未完成迁移，但**无提交历史证据**（未查 git log）。

---

## 11. 产物清单

| 路径 | 说明 |
|---|---|
| `reports\law-audit.md` | 本报告（唯一新建文件） |
| `E:\TEMP\law-json-1.txt` | Laws.json 解析统计的原始输出（仓库外临时文件，供复查） |
| `E:\TEMP\law-tests.txt` | 法律测试文件与用例计数的原始输出（仓库外临时文件，供复查） |

**未修改任何游戏代码、资产或 docs/ 文档。**
