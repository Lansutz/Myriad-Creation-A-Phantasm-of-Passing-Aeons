# 法律 / 革新系统：代码实测状态 + 本次修复

> 2026-11-09 00:47（Asia/Shanghai）。所有结论以 `MyriadCreation/Assets` 下的代码与
> `StreamingAssets/Base/{Laws,Innovation}` 的内容数据为准；`docs/` 只作对照，不作为结论依据。

---

## 1. 目标与验收标准

**用户诉求**：法律与革新目前"压根没完成"，我此前把它们当成"旧计划"处理是错的；要求按系统代码重新判定真实状态，
并且继续把它们当在建系统推进，而不是再出一份纸面计划。

**验收标准**：

1. 两系统各自给出**逐项**完成度判定（哪些真的在跑、哪些只有数据结构、哪些是死代码），每条带 `file:line`；
2. 至少一处**真实缺陷**被修复，并用「编译 + EditMode 测试」实跑验证，不靠推断；
3. `docs/PLAN_INDEX.md` 不再把两系统标成"状态未核"，改为实测状态。

---

## 2. 输入与前提

| 项 | 值 |
|---|---|
| 引擎 | Unity `6000.6.3f1`（`MyriadCreation/ProjectSettings/ProjectVersion.txt`） |
| 编辑器路径 | `D:\Unity Hub\6000.6.3f1\Editor\Unity.exe` |
| 工程根 | `D:\Myriad-Creation-A-Phantasm-of-Passing-Aeons\MyriadCreation` |
| Git HEAD | `f1ca764f refactor: 政体系统重命名为法律系统，加领域划分` |
| 工作树状态 | 大量未提交改动；`Assets/Scripts/Simulation/MyriadCreation.Simulation.LawHost/` 与 `docs/PLAN_INDEX.md` 在 git 中是 **untracked**（`git status --short` 返回 `??`），因此 `git diff` 看不到本次改动，需逐文件核对 |

**主要读取范围**（全部逐行读过，只列承载结论的部分）：

- 法律规则/状态：`Simulation/MyriadCreation.Simulation.Laws/` 全 32 个 `.cs`
- 法律宿主：`Simulation/MyriadCreation.Simulation.LawHost/`（`LawRealmHost` / `LawContracts` / `LawDecreeView` / `DecreePropagationSystem` / `LawEffectRuntime` / `JudicialSystem` / `DefaultLawCatalog` / `PolityComponentInnovations` / `LawReform` / `LawSystem`）
- 革新：`Simulation/MyriadCreation.Simulation.State/InnovationTree.cs`(971 行)、`Simulation/MyriadCreation.Simulation.Systems/{InnovationKnowledgeSystem,InnovationResearchSchedule,ResearchPlanSystem,ResearchPlanExecutor}.cs`、`Simulation/MyriadCreation.Simulation.Innovation/*`、`InnovationQueryAdapter.cs`
- 主循环：`Simulation/MyriadCreation.Simulation.WorldState/GameWorld.Simulation.cs`(1225 行)
- 存档：`Infrastructure/MyriadCreation.Infrastructure.Save/SaveSystem.cs`
- UI：`Assets/UI/**`
- 内容：`StreamingAssets/Base/Laws/Laws.json`(46.8KB)、`StreamingAssets/Base/Innovation/Innovations.json`(225.7KB)
- 测试：`Assets/Tests/EditMode/` 全部 `Law*/Legal*/Decree*/Crime*/Innovation*/Research*/Recipe*` 文件

---

## 3. 动作清单

按执行顺序，命令与参数逐字保留。

1. **编译验证**（基线，改动前）

   ```
   "D:\Unity Hub\6000.6.3f1\Editor\Unity.exe" -batchmode -quit -nographics \
     -projectPath "D:\Myriad-Creation-A-Phantasm-of-Passing-Aeons\MyriadCreation" \
     -executeMethod MyriadCreation.Editor.BatchCompileCheck.Run \
     -logFile "D:\Myriad-Creation-A-Phantasm-of-Passing-Aeons\_fc_verify\compile-latest.log"
   ```

2. **EditMode 基线测试**（改动前）

   ```
   -batchmode -nographics -runTests -testPlatform EditMode
   -testResults _fc_verify\test-editmode.xml -logFile _fc_verify\test-run.log
   ```

3. **两个只读子审计并行**（未改任何代码）：`law-audit`、`innovation-audit`，产出
   `reports/law-audit.md`（436 行）、`reports/innovation-audit.md`。

4. **代码改动**（3 个生产文件 + 1 个新测试，见 §3.1）。

5. **复跑 EditMode**：706 用例，705 通过、1 失败；失败用例是我新写的
   `LawCatalogCloneParityTests.CloneGroups_IsDeepCopy_NotSharedReferences`——断言写错
   （源 `requiredInnovationIds` 我放了 2 个元素，断言写成 1），**不是产品缺陷**。

6. **修正该断言**（`1` → `2`）后复跑：706/706 全绿。

7. **内容影响量化**：`_fc_verify/law-inno-check.ps1`、`_fc_verify/law-gate-impact.ps1`、
   `_fc_verify/law-gate-inno-detail.ps1`（只读，输出在同目录 `.txt`）。

8. **更新** `docs/PLAN_INDEX.md` §5：删除"状态未核"，改为实测状态表。

### 3.1 改动明细（逐字）

**(a) `LawHost/DefaultLawCatalog.cs` —— 深拷贝补齐字段**

修复前 `CloneLaw` / `CloneRequirements` / `CloneDecree` / `CloneGroups` 漏拷 9 个字段。改动后：

- `CloneGroups`：新增 `fallbackLawId = group.fallbackLawId`（`:186`）
- `CloneLaw`：新增 `isFallback`(`:235`)、`normKind`、`scope`、`precedence`、`issuerCharacterId`、以及列表深拷
  `copy.overrides.AddRange(...)`、`copy.excludesLawIds.AddRange(source.excludesLawIds)`(`:250`)
- `CloneDecree`：新增 `copy.excludesDecreeIds.AddRange(source.excludesDecreeIds)`(`:271`)
- `CloneRequirements`：新增 `lawRequirement.requiredDecreeIds.AddRange(...)`(`:296`)、
  `lawRequirement.requiredPolityForm = source.requiredPolityForm`(`:298`)

**(b) `LawHost/LawContracts.cs` —— 把政权真实革新集合接进法律门槛**

新增私有辅助（`:65`）：

```csharp
private IReadOnlyCollection<int> OwnedInnovationIds(int realmId)
{
    InnovationTree tree = _world?.Innovations;
    if (tree == null) return null;
    return tree.GetRealmInnovations(realmId);
}
```

三处调用点由 `null` 改为 `OwnedInnovationIds(realmId)`：

- `:229` `LawRealmHost.BeginEnactLaw(..., OwnedInnovationIds(realmId), out reason)`
- `:258` `LawAndDecreeSystem.EnactDecree(..., currentDay, OwnedInnovationIds(realmId), out reason)`
- `:129` `LawDecreeViewBuilder.Build(..., ResolveCharacterName, OwnedInnovationIds(realmId))`

**(c) `LawHost/LawDecreeView.cs` —— 视图与命令同源**

- `Build(...)` 追加可选参数 `IReadOnlyCollection<int> ownedInnovationIds = null`（`:51`，保持旧调用方零改动）
- 候选集两处 `CanEnactLaw`(`:178`) / `CanEnactDecree`(`:199`) 由传 `null` 改为传 `ownedInnovationIds`

**(d) 新增 `Assets/Tests/EditMode/LawCatalogCloneParityTests.cs`**（3 个用例）

`CloneGroups_LawAndGroupFields_ArePreserved`、`CloneDecree_AllFields_ArePreserved`、
`CloneGroups_IsDeepCopy_NotSharedReferences`——覆盖 `LawDef` 21 字段、`LawRequirement` 7 字段、
`DecreeDef` 10 字段，并断言克隆体与源不共享列表引用。**修复前必失败，修复后通过**，
是这次改动的回归门禁。

---

## 4. 证据

### 4.1 编译

`_fc_verify/compile-latest.log`：

```
[BatchCompileCheck] COMPILE_OK
```

### 4.2 测试（三次实跑）

| 轮次 | 文件 | 结果 | 说明 |
|---|---|---|---|
| 基线（改动前） | `_fc_verify/test-editmode.xml` | `total="703" passed="703" failed="0"` | 全绿 |
| 改动后 | `_fc_verify/test-editmode-2.xml` | `total="706" passed="705" failed="1"` | 唯一失败=我新测试的断言笔误 |
| 断言修正后 | `_fc_verify/test-editmode-3.xml` | `total="706" passed="706" failed="0"` | 全绿 |

关键判读：**基线 703 全绿并不能证明法律系统没问题**——互斥/前置法令字段漏拷、革新门槛恒被 `null` 短路，
都发生在"内容 JSON → 运行时目录"这条链上，而现有法律测试全部直接 `new LawDef{...}` 绕过克隆，
所以缺陷一直是绿的。

### 4.3 死链证据（我本人复核，非只引用子审计）

- `MatchCrimes` / `TryTrialCrime`：全仓 grep 只有定义处（`GameWorld.Simulation.cs:1186-1189`、`:1191-1206`），**零调用者**。
- `_judicialSystems`：唯一写入点是 `GetOrCreateJudicialSystem`（`GameWorld.Queries.cs:262-281`），
  它唯一的调用方是 `TryTrialCrime` ⇒ 字典恒空 ⇒ `justice.daily`（`GameWorld.Simulation.cs:830-833`）每日空转。
- 全仓无 `CrimeCase` / `CaseFile` / `CourtCase` 命中 ⇒ 不存在"案件"实体，罪行无从发生。
- `LegalNormStage`：定义在 `LegalNormStage.cs:7-17`，唯一使用点是 `LawGroupState.cs:15` 的字段默认值 `Active`；
  **无任何赋值语句** ⇒ Unknown/Proposed/Consolidated/Fundamentalized/Repealed 六态永不出现。
- `RealmLawState.fundamentalizedLawIds`（`:20`）：无读无写。
- `LawDef.normKind` / `scope` / `precedence`（`:37,40,43`）：全仓仅定义 + 声明，**无读取点** ⇒ 没有冲突裁决。
- 修改前的 `null` 实参：`LawContracts.cs:215`（BeginEnactLaw）、`:244`（EnactDecree）、`LawDecreeView.cs:178,199`。

### 4.4 内容层实测

| 项 | 数值 | 来源 |
|---|---|---|
| `Laws.json` | 12 组 / 49 法 / 2 法令 | `_fc_verify/law-gate-impact.txt` |
| `Innovations.json` | 186 条 | `_fc_verify/law-inno-out.txt` |
| 带革新前置的法律 | 19 条（`requiredInnovationIds` 共 19 处引用） | 同上 |
| 引用到的革新 id 缺失数 | **0**（501/505/960/985/994/995/997/999/1003/1008 各命中 1 条） | `_fc_verify/law-gate-inno-detail.txt` |
| JSON 中实际使用 `excludesLawIds`/`requiredDecreeIds`/`excludesDecreeIds`/`fallbackLawId`/`normKind`/`precedence`/`overrides`/`isFallback` | **各 0 次** | `Select-String` 计数 |
| `isInitiallyKnown` 的法律 | 16 / 49 | `_fc_verify/law-gate-impact.txt` |
| 启用门槛后被挡在候选外的**初始已知**法律 | **1 条**（`military_training`，需 995） | 同上 |

**内容自证**：革新 995「军队编制」的 `unlockRefs` 明确写了
`{kind:5, key:"standing_army"}`、`command_hierarchy`、`military_discipline`、`national_army`、`central_command`
（`kind 5 = Law`，见 `InnovationUnlock.cs:12`），而这 5 条法律在 `Laws.json` 里各自反写 `requiredInnovationIds=[995]`。
即**内容层双向声明了"革新 → 法律"，只有代码这一环是断的**——这正是本次修复 (b)(c) 补的洞。

### 4.5 改动影响面

- 门槛启用后开局候选法律：16 条已知 − 1 条（`military_training`）= **15 条仍可推行**；法令 2 条中 0 条受门槛影响。
  ⇒ 不是"把面板清空"，是把内容本来就声明要锁的那一条锁上。
- `LawDecreeViewBuilder.Build` 追加的是**可选参数**，14 个法律测试与其它调用方零改动即编译通过（706/706 绿即证）。

---

## 5. 结论

### 已验证（实跑 + 代码证据）

1. 法律与革新**都不是"未开工的旧计划"**：两者都在主循环里注册并每日 tick（`GameWorld.Simulation.cs:795-851`，
   `laws.pending.daily` order 26、`laws.effects.yearly` 24、`justice.daily` 27、`decrees.propagation.daily` 37、
   `InnovationResearchSchedule`/`ResearchPlanSchedule` :835/:837）。
2. 法律主线**可玩**：已知→提案（首脑/中央机构/特许持有者三分支）→pending→到期固化，法令颁布/冷却/到期/撤销，
   法令逐级下达（传令官 + 合规度五档），年度效果结算，`GovernmentPanel` 有完整操作入口。
3. 革新**内容与前置可用**：186 条前置图无环无悬空；AI 选题+每日研究点、角色练习→突破计划两条链在跑；
   `HasInnovation` 硬门槛真实作用于政体组件、阶层、头衔、家族传统、资源开采、配方生产。
4. 两系统的**真实缺口**已定位到"具体哪个方法没人调用"这一级，见 `reports/law-audit.md` §9.2（22 条）与
   `reports/innovation-audit.md` §12.2（18 条）。
5. 本次修复的 4 项改动**编译通过、706/706 测试全绿**，并新增 3 个防回归用例。

### 未验证（不得当成事实）

1. **运行时观感未验证**：没有进编辑器实机跑一局，未观察"革新门槛生效后玩家实际候选列表/提示文案"。
2. **存档往返未实跑**：`SaveSystem` 级法律/革新往返测试仍为 0 覆盖（子审计已标注为缺口 G17）。
3. **`PolityComponentInnovations` 硬编码 id（302/500/501/…）与 JSON 内容的长期一致性**未做自动化校验。
4. 未做数值平衡评估（研究点速率 vs `researchCost` 20–1500 的实际节奏）。
5. 本次未触碰的部分（罪行链、根本化、冲突裁决、玩家革新 UI）**仍处于原本的未完成状态**，不要因为测试全绿而认为已实现。

---

## 6. 遗留与风险

1. **克隆缺陷此前是"潜在"而非"已发生"**：`Laws.json` 目前 0 次使用 `excludesLawIds` / `requiredDecreeIds` /
   `excludesDecreeIds` / `fallbackLawId` / `normKind` / `precedence` / `overrides` / `isFallback`，
   所以修复前**没有内容被静默破坏**；一旦有人往 JSON 写这些字段才爆。这条纠正了子审计"互斥法律永不生效（影响高）"的
   严重度判断：机制上确实失效，但当前内容未触发。
2. **真正在生效的缺口是革新门槛**：19 条法律的 `requiredInnovationIds` 一直被 `null` 短路，本次才接上。
   若后续有人把 `_world.Innovations` 置空（如测试替身），门槛会自动退回"不校验"——这是有意的降级路径，
   但需要知晓：`OwnedInnovationIds` 返回 `null` = 不校验，返回空集合 = 生效但一条都拿不到。
3. **法律双源未合并**：`LawComposition`（政体组件层，`LawSystem.cs` 整类零实例化=死类）与 `LawDef` 层并行；
   玩家在面板推行"世袭继承"不会改变 `RealmData.GetEffectiveSuccessionLaw`。子审计 G13/G16。
4. **罪行—审判—判决整链 0% 通电**，且 `JudicialSystem` 不入档。要接必须一次做完"案件实体→罪行生成→审判调度→UI→存档"，
   缺一环就还是空转。
5. **`LegalNormStage` 与 `fundamentalizedLawIds` 是"承诺未兑现"的字段**：要么实现根本化（需定义"谁在什么条件下推进阶段"），
   要么从 schema 删掉，否则每个新读者都会被误导。
6. **`PLAN_INDEX.md` §5 的表格是我按 2026-11-09 的代码写的**，三个 `reports/*.md` 是证据；如果代码继续变，索引需要同步更新，
   否则它又会变成"过期权威"。

---

## 7. 产物清单

| 路径 | 说明 |
|---|---|
| `reports/law-innovation-code-status-20261109-0047.md` | 本报告 |
| `reports/law-audit.md` | 法律系统逐项审计（436 行，22 条缺口） |
| `reports/innovation-audit.md` | 革新系统逐项审计（18 条缺口） |
| `MyriadCreation/Assets/Scripts/Simulation/MyriadCreation.Simulation.LawHost/DefaultLawCatalog.cs` | 深拷贝补齐 9 个字段 |
| `MyriadCreation/Assets/Scripts/Simulation/MyriadCreation.Simulation.LawHost/LawContracts.cs` | 接入政权真实革新集合（3 处调用） |
| `MyriadCreation/Assets/Scripts/Simulation/MyriadCreation.Simulation.LawHost/LawDecreeView.cs` | 视图候选与命令同源校验 |
| `MyriadCreation/Assets/Tests/EditMode/LawCatalogCloneParityTests.cs` | 新增 3 个防回归用例 |
| `docs/PLAN_INDEX.md` | §5 由"状态未核"改为实测状态表 |
| `_fc_verify/compile-latest.log` | 编译证据（COMPILE_OK） |
| `_fc_verify/test-editmode{,-2,-3}.xml` | 三次测试结果证据 |
| `_fc_verify/law-gate-impact.txt`、`law-gate-inno-detail.txt`、`law-inno-out.txt` | 内容层与门槛影响量化输出 |
| `_fc_verify/law-inno-check.ps1`、`law-gate-impact.ps1`、`law-gate-inno-detail.ps1` | 生成上述输出的只读脚本 |
