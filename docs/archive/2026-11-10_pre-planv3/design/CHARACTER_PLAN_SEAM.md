# 角色系统并入综合大规划：接缝、门禁与代码缺口

> 2026-11-07。依据：`docs/research/character-2026-11-07/01-character-code-audit.md`（代码只读审计）、`docs/research/character-2026-11-07/03-plan-docs-inventory.md`（现有大规划盘点）；行动门禁部分由 Lead 直接核对代码补入（原定 02 报告的审计未完成，结论已并入本文 §3）。
> 定位：**不是又一份角色设计稿**，而是「角色域 → 大规划」的接缝文档。设计内容仍以 `docs/角色系统整体设计.docx` 为现状基线、以 8.1 新稿为待补设计输入。

## 0. 结论

1. 8.1 的方向对：三层属性、专精=能力+训练、反对把政权行为塞进个人技能树。但代码只落地了 **0.5 层**：专精六维是创角随机数；基本能力恒为 50；技能树、点数、前置、门槛全部不存在。
2. 用户担心的"层级混乱"，在代码里的实际形态**不是**"宣战绑了角色技能"，而是反过来的：宣战**没有任何主体门禁**。`DiplomacyManager.DeclareWar` 只查外交关系、停战期与（带借口时）CB；AI 只看 `treasury` 与邻居强弱（`AIController.cs:326-351`）——不查统治者、官府、法律、资源。CK3 的毛病是把政权行为塞进个人生活方式树；这里的问题是政权行为**没有政治主体**。两者要用同一套四层门禁修。
3. 并入大规划的顺序：**先定本体与门禁（3 个裁决）→ 写接缝表 → 最后写角色内容/技能树**。角色应作为 `UNIFIED_MASTER_PLAN` 的一个 WBS 工作包（如 W23），不另起一份"大规划"。

---

## 1. 8.1 设计稿审阅

| # | 设计条目 | 判断 | 问题 / 建议 |
|---|---|---|---|
| 1 | 基本能力 4 类 × 3 子属性 | ✓ 方向对 | 需要生成来源（DNA/种族/成长）与消费清单；当前代码恒 50（`BasicAbilities.cs:55` `Random` 零调用） |
| 2 | 专精六维 0–100 | ✓ | 需要"专精=能力×训练"的公式（§1.1），当前是 `Random.Range(20,80)`（`CharacterManager.cs:747-761`） |
| 3 | 专精 = 基本能力 + 学到的技能 | ✓ 但要落成公式 | "相加"会让高天赋者无训练也强；建议乘法/几何平均（§1.1），基本能力同时决定成长斜率与上限 |
| 4 | 技能树（分叉/前置/门槛） | ✓ | 节点效果必须分"能力型 / 权限型"（§1.2）；权限型不能进个人树 |
| 5 | 健康双维（肉体 0–100 + 压力/精神疾病） | ✓ 且代码已实现 | 阈值与设计一致：压力>60 人格漂移翻倍（`CharacterData.cs:358-363`）、>80 计日（`:350-357`）、90 天发病（`CharacterManager.cs:565-570`）；另有恐惧→偏执、年迈→失智支线。**但压力输入几乎为零**（只有缺粮 +2/日，`CharacterManager.cs:550`），需要事件/性格/关系/职位压力源 |
| 6 | 性格（4 档/7–8 维） | ⚠ 部分 | 枚举 8 维含 `Sociability`，`CharacterData` 只有 7 个字段，赋值时被静默吞掉（`CharacterData.cs:436-438`）；特质 108 条齐全但 `AddTrait` 零调用 |
| 7 | 家族/继承 | ⚠ 代码比稿子完整 | `FamilyNode` 树 + 继承法 + 摄政/绝嗣已实现；缺头衔/领地/财产转移（`ownedTitles` 零生产者、`DistributeLand` 零调用） |
| 8 | 教育/成长 | ✗ 缺 | "训练"从哪来没定义：童年教育、师承、学院、任职经验、自学；这是专精公式的输入 |
| 9 | 语言/读写 | ✗ 缺 | 任官、外交、考试都要读写门槛，角色侧没有字段（现有语言系统只服务命名/称谓） |
| 10 | 关系 | ⚠ 结构完整、运行期空转 | `ModifyRelation` 零调用、`CreateBond` 仅测试调用；关系没有来源 |
| 11 | 命名 clash | ✗ | 交涉 = `BasicAbilities.Diplomacy`，与政权外交同名；"社交"同时指 `SpecialtyType.Social`/`specialty.social`/`PersonalityDimension.Sociability`；`charm` 与 `charisma` 重复 |

### 1.1 基本能力 → 专精的公式

```
basic_norm   = 映射到该专精的基本能力子属性的加权均值 / 100   （矩阵见下）
training_norm = 该专精的训练积累 / 100（教育/实践/任职经验）
specialty    = 100 × (basic_norm^0.5 × training_norm^0.5) + 天赋/特质/疾病修正
```

- 性质：**乘法**。天赋高但没训练 → 专精近 0；训练足但天赋差 → 有上限。符合"体魄强但没受过战斗训练，勇武不高"。
- 基本能力同时决定成长速率（`training_gain ∝ 0.5 + basic_norm`）与专精上限（`cap = 40 + 0.6×100×basic_norm`）。
- 基本能力映射矩阵（草案，先定再写内容）：

| 专精 | 主要子属性（权重） | 次要 |
|---|---|---|
| 勇武 | 体魄·力量 0.4、协调 0.4 | 体魄·坚韧 0.2、感知·反应 |
| 军事 | 心智·逻辑 0.4、感知·观察 0.3 | 体魄·坚韧、感知·直觉 |
| 管理 | 心智·逻辑 0.4、心智·记忆 0.3 | 交涉·气度、感知·观察 |
| 社交 | 交涉·口才 0.4、交涉·气度 0.3 | 交涉·洞察、感知·直觉 |
| 阴谋 | 交涉·洞察 0.4、心智·灵感 0.3 | 感知·观察、心智·逻辑 |
| 学识 | 心智·记忆 0.4、心智·灵感 0.3 | 心智·逻辑、感知·观察 |

### 1.2 技能树节点效果分类

| 类型 | 例子 | 能否进个人技能树 |
|---|---|---|
| 能力加成 | 判定 +、产出 +、战斗 + | ✓ |
| 个人行动解锁 | 决斗、密谋、著书、游历 | ✓ |
| 事件选项 | 个人事件新分支 | ✓（条件可含职权） |
| 特殊互动 | 拉拢、勒索、求爱 | ✓（目标需另行限制） |
| **政权行动** | **宣战、立法、征税、任命、缔约** | **✗ 归 ActionGate 的职权/法理层** |

一句话：**技能树只能回答"我做得有多好/能不能亲自做"，不能回答"这个政权能不能做"。**

---

## 2. 代码 P0 / P1（摘要，详见 01 报告）

| 级别 | 问题 | 证据 |
|---|---|---|
| P0 | 三层属性断链：能力恒 50、专精纯随机、技能树不存在、`SpecialtyType` 零消费者 | `BasicAbilities.cs:55`；`CharacterManager.cs:747-761`；`SpecialtyType.cs` |
| P0 | 角色级疾病系统（990 行/32 病）从未实例化；人口级 `DiseaseSystem` 另起一套直接扣血 | `CharacterDiseaseSystem.cs:13`；`DiseaseSystem.cs:179-219` |
| P0 | `GetEffectiveStats`/`ApplyDisorderMods`/`AddTrait` 零调用 → 特质、疾病、精神疾病对数值不生效 | `CharacterData.cs:297`；`MentalHealthSystem.cs:106`；`CharacterData.cs:649` |
| P0 | 存档漏字段：`achievements/accessionDay/personalTenets/bodyMarks/identity` | `CharacterSaveData.cs` |
| P1 | 性格 8 维 vs 7 字段（Sociability 丢失）；关系无创建；纽带无效果；家族只初始化一次；继承不转移头衔/领地/财产；`reignYears` 实为年龄 | 见 01 报告 §5 |
| P1 | 压力只有"缺粮 +2"一个入口；`dread` 无上升通道；`charm` 无消费者 | `CharacterManager.cs:550/631/640` |

---

## 3. ActionGate：四层门禁（本稿核心）

### 3.1 契约

```
ActionGate.Check(ActionContext{actor, action, target, realm?})
  = Capability  ∧  Authority  ∧  Legality  ∧  Resource
```

| 层 | 问题 | 数据来源 | 角色系统参与方式 |
|---|---|---|---|
| 能力 Capability | 这个人/这个机构做不做得到 | 角色属性/专精/技能/健康；机构能力（行政力/军力） | **只在这里**：加成、判定、解锁个人行动 |
| 职权 Authority | 谁有权做 | 头衔/官职/机构席位/摄政/议会授权 | 角色占位（holder）→ 授权 |
| 法理 Legality | 合不合法 | 法律域/CB/契约/停战/宗教法 | 角色技能可减轻代价，但不创造合法性 |
| 资源 Resource | 付不付得起 | 国库/兵员/威望/合法性/时间 | 角色管理/军事影响效率 |

### 3.2 宣战示例（对照现状）

| 层 | 应该检查 | 现状 |
|---|---|---|
| 能力 | 无硬门槛；统帅专精影响战役执行与伤亡 | 不检查 |
| 职权 | 必须是元首/摄政/获授权的议会或部落大会 | **不检查**（`DeclareWar` 只有 relation/truce） |
| 法理 | CB 有效性；无 CB 可宣但付威望/合法性代价 | 部分（`DeclareWarWithJustification` 有 CB 与惩罚） |
| 资源 | 国库/兵员/补给 | AI 粗略检查 `treasury > 200`；玩家路径不检查 |

结论：现状不是"技能绑政权"，而是**政权行为没有主体**。修法：新增 `WarDeclarationGate`，输入 `realmId` → 解析 authority holder（ruler/regent/council）→ 四层检查；角色 Martial/Prowess 只进"执行质量"（战役修正、伤亡、士气），不进"能不能宣"。

### 3.3 与现有片段的关系

现有 7 个分散门禁片段（`TitleEligibility` 9 维、`ProposalAuthority` 3 值、`OfficeFactor`、四段过滤、法令执行 `DecreeOrder.officialCharacterId`、信仰组织阶段、Architecture 2.0 的 Action 位）全部收编为 `ActionGate` 的 provider，不再各写一套 `CanXxx`。角色能力 provider 是其中**唯一新增的一类**，也是 8.1 设计真正的落点。

---

## 4. 角色八域 → 其他系统接缝表

| 域 | 角色字段 | 消费系统 | 读什么 | 失败后果 |
|---|---|---|---|---|
| 属性/专精 | 六维、basicAbilities | 外交/军事/法律执行/管理/阴谋/学识 | 判定修正、产出、执行质量 | 数值空转（现状） |
| 技能 | skills、技能树 | 个人行动、事件选项、判定 | 解锁/加成 | 同上 |
| 健康 | health、disease | 死亡、继承、战争、劳动力 | 死亡判定、战力 | 已接一部分 |
| 压力/精神 | stress、mentalDisorderId | 决策质量、AI、事件 | 行为偏差、事件触发 | 只有发病、无行为影响 |
| 关系 | relations、bonds | 婚姻、派系、阴谋、外交 | 好感/信任/恐惧 | 关系无创建 |
| 家族 | familyId、FamilyNode | 继承、头衔、派系、婚姻 | 候选、声望、传统 | 家族只初始化一次 |
| 语言 | （缺） | 任官、考试、外交 | 读写门槛 | 无法表达 |
| 身份/阶层 | socialClass、role | 法律（公民权/等级）、经济 | 权利/义务 | 枚举与法律组未映射 |

---

## 5. 并入路径（三步）

1. **裁决三矛盾，出 1 页裁决表**（见 §6）。不裁决就写角色内容，必然返工。
2. **写接缝，不写新机制**：把 §3 的 ActionGate 契约与 §4 的接缝表落成代码骨架（接口 + 一个 provider 实现即可），并登记进 `UNIFIED_MASTER_PLAN` 的 WBS（建议 W23）。
3. **再写角色内容**：教育/成长 → 专精公式 → 技能树内容 → 事件/互动。技能树内容最后做，因为它的节点效果依赖 §1.2 的分类。

---

## 6. 三个待裁决点（已裁决 2026-11-07，见 DECISIONS_M1_M2_M3.md）

| 编号 | 矛盾 | 已裁决 |
|---|---|---|
| M1 | 头衔/官职本体未收敛（四套 kind、官职是否独立、共有头衔缺失） | 本体采"无硬 kind、由绑定+权利推导"；5 值 kind 只作派生分类视图；头衔持有层支持多人共有；官职只作官僚头衔标记，不另立系统 |
| M2 | "谁有权做什么"无统一门禁 | 采 §3 的 ActionGate 四层；最高权力采四轴（SUPREME），二元字段降为兼容；9 维头衔资格补"能力/健康/教育"维（只作为资格的一类，不是全部） |
| M3 | 角色域基线不唯一（docx 六维 vs README 专精→技能树 vs HONORIFIC 拆称号） | docx 作现状基线；8.1 新稿作待补设计；HONORIFIC 已定项覆盖 docx §11.7；先出"角色域现状+待定"一页表再动工 |

---

## 7. 立即修复清单（P0，可逐条验证）

1. `BasicAbilities` 生成：DNA/种族 → 12 项基础值（复用 `DnaExpression` 的 martial/intelligence 偏移）。
2. 专精派生：实现 §1.1 公式，替换 `Random.Range(20,80)`。
3. `GetEffectiveStats` 接线：所有读取六维/魅力的地方改走唯一出口（特质/疾病/精神修正才生效）。
4. 疾病系统二选一：角色级 `CharacterDiseaseSystem` 或人口级 `DiseaseSystem`，明确分工，禁止双轨。
5. 存档补 `achievements/accessionDay/personalTenets/bodyMarks`。
6. 修 `Sociability`（补字段或删维度）与 `reignYears` 公式。
7. 补 `AddTrait`、`ModifyRelation`、`CreateBond` 的生产端（至少教育/事件/婚姻三条链）。

---

## 8. 不做什么

- 不把 8.1 直接写成第三份角色设计文档；内容仍回到 `UNIFIED_MASTER_PLAN` 的 WBS。
- 不把"技能树解锁政权行动"写进任何设计。
- 不在头衔本体裁决前改 `InheritanceLaw`/头衔代码（会返工）。
