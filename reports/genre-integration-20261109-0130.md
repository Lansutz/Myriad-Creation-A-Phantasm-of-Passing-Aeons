# 类型定位接入报告（大战略 + 上帝模拟 + RPG）

> 2026-11-09 01:30（Asia/Shanghai）。里程碑：Plan v3 / M2.6「类型定位」。
> 输入：用户两次口述——"一款大战略 + 上帝模拟游戏"、"有 rpg 内容的"。

---

## 1. 目标与验收标准

**目标**：把"类型"从宣传语变成**三种可操作、可验收的玩家姿态**，并查出代码里各自缺什么。类型不能只是标签，它必须回答三个问题：**你是谁、你能下什么命令、你必须能看到什么**。

**验收标准**：

| # | 标准 | 结果 |
|---|---|---|
| 1 | 三种姿态各自给出"身份/命令/观测/代码锚点/现状" | ✅ 宪章 §0.5 表 |
| 2 | 每种姿态给出可断言信号 | ✅ 宪章 §2 末（3 组，共 9 条） |
| 3 | 找出并验证每种的**致命缺口**（不是"待完善"，是"不存在"） | ✅ 三处：`PlayerRealmId` 无赋值 / 地形入口空壳 / 角色面板不可达 |
| 4 | 据此修订总纲（动词表分姿态、里程碑、DoD）与决策 | ✅ 动词表重排为 14 行分姿态；M1/M2/M8/M10 改动；D13–D15 |

---

## 2. 输入与前提

- 类型表述（逐字）：**"一款大战略 + 上帝模拟游戏"**、**"有 rpg 内容的"**。
- 前提：三者是**同一模拟的三种姿态**，不是三个游戏；否则会出现"上帝看到的国"和"君主看到的国"不一致——那是最常见的缝合失败。
- 只读：未改任何运行时代码。

---

## 3. 动作清单

1. 为每种姿态找代码锚点并亲验（grep/read）：
   - 大战略：`PlayerRealmId` 的写入点（结论：0 个）、`GovernmentPanel.ResolveRealmId`（回退 `realms[0]`）、`FaithContracts.cs:103` 依赖 `PlayerRealmId`
   - 上帝：`WorldEditService`/`IMapEditCommand`、`Assets/Editor/Tools/TerrainPainter`（只有 2 个枚举文件、全工程无 `[MenuItem]`）、`SandboxSpawnSystem`、`Chronicle`（500 条上限、`GetMajorEntries`/`GetEntriesByType`）
   - RPG：`CharacterDetailQueryAdapter`、`Main.unity` 的 `characterPanel` 序列化、`Marry` 调用数、`FindBestGeneral`/`CalculateRuleAbility` 调用数、`ModifyPrestige`
2. 写宪章 §0.5（类型定位表 + 四条硬规则）与 §2 末（三姿态验收信号）、§5 第 6 条（排期改动）、§6 第 5–7 条（待议）。
3. 改总纲：§0 第 8 条结论、§1 一句话、§4.1 动词表（14 行、带姿态列）、M1/M2/M8/M10。
4. 改决策：D13 玩家身份、D14 powers 边界、D15 RPG 承载方式。

---

## 4. 证据

### 4.1 大战略：**"我的国家"不存在**

| 断言 | 证据 |
|---|---|
| `PlayerRealmId` 默认 −1 且**无任何赋值** | `GameWorld.Core.cs:94`；全仓 grep `PlayerRealmId *=` → 仅 `GameSaveSystem.cs:368`（Obsolete 读档路径） |
| 活存档没有该字段 | `SaveData.cs` grep `playerRealmId` → 无命中；只有 `GameSaveData.cs:35`（Obsolete） |
| 政府面板靠"第一个政权"冒充 | `GovernmentPanel.cs:246-256`：`targetRealmId=0`，取不到就 `realms[0]` |
| 已有系统依赖它，恒 −1 会走错分支 | `FaithContracts.cs:103-106`（国教判定，`PlayerRealmId>=0` 恒假）；`MapRenderer.Colors.cs:222,239`（玩家高亮永不出现） |

### 4.2 上帝模拟：**改得了世界的地方没有按钮，有历史的地方存不下来**

| 断言 | 证据 |
|---|---|
| 地形编辑后端存在 | `IMapEditCommand` / `WorldEditService` / `MapEditHistory`；`world.map.recalculate-dirty` 用 `RegisterDirty` 只在涂改后跑 |
| 但没有编辑器入口 | `Assets/Editor/Tools/TerrainPainter` 只有 `BrushMode.cs`/`BrushShape.cs`；全工程 `[MenuItem]`/`EditorWindow` **0 命中**（A 域） |
| 播种在跑 | `SandboxSpawnSystem` 注册为 `sandbox.spawn.monthly`（order 36），每月 tick |
| 编年史是完整观测面 | `Chronicle.cs`：500 条上限、`major` 标记、`participants`、按类型检索；事件类型 20+（succession / regency / law_enacted / war / war_end / religion / city_founded / juncture_* / life_eval / class emergence…） |
| 但编年史**不进活存档** | 只有 `GameSaveSystem.cs:314,661`（整类 `[Obsolete]`）；`SaveData.cs` 无 chronicle 字段 |

### 4.3 RPG：**底座最厚，入口最薄**

| 断言 | 证据 |
|---|---|
| 底座厚度 | `GeneticsEngine` 616 行、`CharacterDiseaseSystem` 990 行、`EpithetSystem` 685 行、`CharacterManager` 1086 行、108 条特质（`PersonalityTraitDatabase` 1236 行） |
| 入口 | `Main.unity` 里 `characterPanel` 序列化为 `{fileID: 0}`、`CharacterPanel` 物体 `m_IsActive: 0`（C 域） |
| 关系/婚姻 | `Marry` 全仓 1 命中（=定义）；`CharacterRelation.opinion/trust/fear` 只写不读 |
| 属性无消费者 | `FindBestGeneral`/`FindBestRuler`/`CalculateCommandAbility`/`CalculateRuleAbility`/`CalculateOverallAbility` 各 1–2 命中（全在定义处）；`ModifyPrestige` 零调用且 prestige 每 tick −0.05 |

### 4.4 三姿态的验收信号（已写入宪章）

- **大战略**：读档后"我是哪个政权"不变；100 年内 AI 对手能扩张/结盟/立法（不只宣战）；玩家能打完一场有胜负的战争。
- **上帝模拟**：不下任何政权命令，仅用 powers 即可在 100 年后造成可量化格局差异；编年史可检索可存读；powers 不能直接改结果。
- **RPG**：一个角色 40 年（出生→成长→婚育→任职→死亡→谥号）全程可玩；属性影响 ≥3 个真实决策。

---

## 5. 结论

### 已验证
1. 三种姿态在代码里都有**真底座**（政权/法律、地形/事件/编年史、人物/遗传/谥号），所以这不是"要加三个新系统"。
2. 三者各自缺的都是**同一类东西**：**连接**——玩家身份没接、powers 没接按钮、人物没接判定。
3. 因此排期改动很小但很关键：`PlayerRealmId` 与 `Chronicle` 进 M1（存档地基），powers 低端子集进 M2，RPG 验收挂 M8，powers 面板进 M10。

### 未验证（不得当成事实）
1. 未实机验证"上帝改地形→贸易路线→人口分布"的因果链是否真的通；F 域已证贸易路线生产期从不创建，所以**这条演示链当前很可能是断的**。
2. 未验证 AI 对手"会不会玩"——AI 目前能研究/宣战/结盟/征点，但没有军队实体，扩张能力未经实测。
3. "40 年角色生命周期"的可行性依赖 M8 的婚姻与关系接线，目前**只有随机生育**。

---

## 6. 遗留与风险

1. **缝合风险（最高）**：三种姿态若各自做一套 UI 与状态，会出现"上帝看到的国 ≠ 君主看到的国"。缓解：宪章 §0.5 规则 1（一套模拟一套存档）+ M10 的"姿态只换命令面"。
2. **上帝 powers 毁掉因果**：一旦开放"给钱/改胜负"，本项目辛苦建立的史学立场立刻失效。缓解：D14 白名单 + 冷却/反作用（M10）。
3. **RPG 变成第五个半跑模块**：缓解：D15 明确"不新增系统，只接消费者"。
4. **待议 3 条**（宪章 §6 第 5–7 条）：大战略要不要胜利条件；powers 要不要代价；RPG 扮演单一角色还是王朝。

---

## 7. 产物清单

| 路径 | 说明 |
|---|---|
| `docs/plan-v3/DESIGN_CHARTER.md` | **新增 §0.5 类型定位**（三姿态表 + 四条硬规则）、§2 末三姿态验收信号、§5 第 6 条排期改动、§6 第 5–7 条待议 |
| `docs/plan-v3/SYSTEM_MASTER_PLAN.md` | §0 第 8 条结论；§1 一句话改为三姿态；§4.1 动词表重排为 **14 行 × 姿态**；M1 增 `PlayerRealmId`+`Chronicle` 入档；M2 增 powers 低端子集；M8 增 RPG 验收；M10 增 powers 面板与姿态切换 |
| `DECISIONS.md` | D13 玩家身份、D14 powers 边界、D15 RPG 承载 |
| `reports/genre-integration-20261109-0130.md` | 本报告 |
