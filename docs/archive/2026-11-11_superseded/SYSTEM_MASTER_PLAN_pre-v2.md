# 系统总纲 v3（SYSTEM MASTER PLAN）

> 生效：2026-11-09。**本文件是唯一的系统级计划入口**。
> 取代 `docs/architecture/UNIFIED_MASTER_PLAN.md`（V2，2026-11-03）的**状态表与排期**，继承其 §4 已定稿决策。
> 架构/文件放置规则不在此重议：见 `docs/architecture/ARCHITECTURE_RULES.md`。
> 证据：`docs/plan-v3/audit/*.md`（八域只读审计）+ `_fc_verify/*.txt`（结构/内容清单）。
> 本文只写"现状—顺序—验收"，不写实现细节；实现细节归各域设计文档。

---

## 0. 三十秒版：本文件最重要的六个结论

1. **这座工程不是"没做完"，是"每个系统都差最后一环"**。1025 个 `.cs`、706 个测试全绿，但八域审计的默认判定是"半跑"：有代码、有测试、缺一条到玩家或到下游的链。
2. **玩家真动词只有 6 个左右**：开档/读档/调速、**F5 存档**、看聚落详情（图标左键）、推行法律与法令（4 命令）、设置。而**地图点选、显示模式切换、存档按钮、角色/家族面板、地图编辑、编年史存档全部不可达**。29 项调度里 22 项没有任何界面可触发或观察——其余由 AI 代玩。
3. **三条 P0 级结构断链**：① 物资只有消耗没有生产（粮食从开局 1000 单调归零）；② 军队从不生成（`CreateArmy` 零调用）⇒ 战争系统在空集合上跑；③ 玩家改法不回写 `LawComposition`（改法不影响治理）。
4. **三条"永不发生"**：① 宣战后白和条件 730 天 > 天数上限 365 ⇒ 战争永不结束；② 聚落等级/建设度推进函数零调用 ⇒ 村落永不长大；③ 新局 `monarchId = -1` ⇒ 统治者死后无人继位（继承/摄政/谥号全部不触发）。
5. **文档债的根因是"计划先于事实"**：`docs/` 下 129 份非归档文档、47 份计划彼此冲突，`PLAN_INDEX` 已在做仲裁但仍不够——所以本轮先把代码事实钉死（八域审计 800+ 行），再写计划。
6. **方法论修正**：从"横向铺系统"转为"纵向切片"（§5）。每个里程碑必须满足三件齐备（tick + 入口 + 存档）+ 一个可观察量变化，否则不算完成。
7. **与设计宪章的落差**（`DESIGN_CHARTER.md`）：宪章的第一原则"多路径建国"、第二原则的"能动性"一半，**在代码里都是零调用**——`FormRealmFromActor` 无人调用、人物能力无消费者。也就是说，**最像"文明演化"的两件事，恰好是没通电的两件**。
8. **定位（用户口径，已修正我此前的"三支柱"说法）**：这是一部**纯历史模拟**（世界可架空），玩家可在**任意尺度**观看与干预——个人 / 家族 / 政权 / 世界是同一世界的缩放层级，不是三种模式。大战略/上帝模拟/RPG 是外部类型标签，不是内部支柱；RPG 的存在理由是"让人可指认，从而更真实"。
9. **最重要的设计约束：不用抽象数值推动历史**（宪章原则 5）。数值只能作**度量**，不能作**原因**。现存反例已在代码里定位：`ApplyPathBonuses` 的 5 个硬编码加成、地块稳定度向 `50+centralization*20` 的纯 Lerp、法律乘子体系、`RaceData` 平铺常数。判据三问（有物质载体吗／经过既有环节吗／删掉会断链吗）已写进 §7 门禁。
10. **解耦只做了一半**（用户怀疑正确）：程序集层做了（15 asmdef 无环、UI 硬边界），但**内容层与状态层没做**——**18 张内容表**在 C#、状态有 **6 组双真源**（人口/法律/遗传/存档/建筑可用性/**金库 vault↔treasury**）。更硬的是**模组通路本身有洞**：`Mods/` 样例因布局判定永不加载；14 个包域零 schema 校验；**一条 `id=0` 的坏数据会让整份 JSON 剩余条目静默丢弃**；革新有 1 条被代码无条件覆盖。而**状态没有守卫**：`RealmData` 37 个字段全 public，`treasury` 14 处直接写。清单见 §4.4 与 D23。
11. **生物学基底是两层，现况不对称**：**DNA（个体差异）在跑**——6 基因座、父母等位基因 + 重组/突变（0.0075/0.015）、表达为寿命/智/武/抗性/外观/才能缺陷，`GenomeInstance` 入档，`DnaDefs.json` 8 条（4 才能 + 4 缺陷）；**种族（群体基线）是空的**——`RaceDefs.json` 0 条，只有硬编码"人族"。所以不是"没有个体差异"，而是**所有族群共用同一套基因组与同一组等位频率（缺省 0.5）⇒ 没有群体分化**；且个体差异**到不了决策**（`FindBestGeneral`/`CalculateRuleAbility` 零调用）。见 §3.0 S14 与宪章 §1.6。

---

## 0.1 怎么读这份文件

| 你想知道 | 看哪节 |
|---|---|
| 这游戏现在到底能玩什么 | §1 产品定义 + §4 可玩性主干 |
| 某个系统做到哪一步了 | §3 系统状态矩阵 |
| 下一步该做什么、为什么是这个顺序 | §5 里程碑 + §6 依赖 DAG |
| 什么算"做完" | §7 完成定义（DoD）与门禁 |
| 哪份旧文档还算数 | `docs/plan-v3/DOC_DISPOSITION.md` |
| 证据在哪 | §9 证据索引 |

### 0.2 覆盖度声明（别拿这张表当"全知"）

这份总纲**不是**逐行读完 1025 个文件写出来的。实际覆盖：

| 读法 | 范围 |
|---|---|
| **Lead 亲自读/grep 过** | 主循环与调度（`GameWorld.Simulation.cs`、`SimulationScheduler`、各 `*Schedule`）、法律契约层、经济日结、政治日结、`RaceData`/`DnaSystem`、`SocialClassAvailability`/`ClassEmergenceEvents`、`Plan`/`PlanSystem`/`PlanType`、`ContentSource`、`Chronicle`、存档 `SaveData`/`SaveSystem` 关键段 |
| **子审计读过并留锚点（12 份）** | A 世界（23 系统）/ B 聚落人口（~30）/ C 角色遗传（~30）/ D 政体法律（~27）/ E 文化信仰（~18）/ F 经济（~12）/ G 军事外交（~28）/ H 革新与主干（29 调度项）/ I 内容与模组（178 行）/ J 单位与行为体（143 行）/ K 表现与入口（161 行）/ L 状态模型（94 行） |
| **已补覆盖（I/J/K/L）** | 合计补出：**18 张内容表在 C#**、**Mods 样例永不加载**、**14 个包域零校验**、**三套实体并行（只有商队在跑）**、**行为体对客户端不可见**、**12 种显示模式无切换入口**、**地图点选空实现**、**RealmData 37 字段无守卫**、**金库双源**、**事件 0 订阅**、**存档静默丢 20+ 字段（含 `primaryCultureId`，读档后继承法断链）** |
| **仍未覆盖（诚实标注）** | `Tests`（144 个文件的逐条语义）、`App`(16) 与 `UI`(28) 的逐行实现、`Simulation.Modding` 的 45 个文件（I 只按加载链读）、`Content.Definitions` 131 个文件的逐域细节（I 已给清单与三态） |

> 这张表的存在意义：**任何"没有系统 X"的断言都必须先看它在不在表里**。计划系统（`Simulation.Planning`，10 个文件）就是漏在第一版里的例子——它其实是通用"计划/项目"引擎，见 §3.5。

**判定口径**（全项目统一，禁止再用"差不多做完"这种说法）：

- `在跑`：①在主循环 tick 或在真实调用链上被调用；②有玩家 UI 入口**或**AI/规则自动驱动；③状态入档。三者齐备。
- `半跑`：满足 1–2 条，必须写明缺哪条（缺入口 = 玩家看不见；缺存档 = 读档回退）。
- `空转`：有类型/字段/方法，零调用者（附 grep 证据）。
- `不存在`：连数据模型都没有。

> 为什么这么严：法律系统曾 703 个测试全绿，而革新门槛实参恒为 `null`——"有代码 + 有测试"不等于玩家能感知。

---

## 1. 产品定义（以代码与内容数据反推）

**一句话**：一部六边形地图上的**历史模拟器**（世界可架空）——世界自己演化（气候→生计→人口→聚落→政权→法→战），玩家可以在**任意尺度**上看与干预：一个人、一个家族、一个政权、或整个世界的条件。

**这是"视角"不是"模式"**：世界只有一套状态、一套存档；你能做的是"停在某一层看"与"对某一层下命令"，而不是"进入某种玩法"（宪章 §0.5）。RPG 内容的作用是**让人可指认**——有属性、亲属、寿命、野心的人，而不是一个人口块。

**已存在的世界骨架**（有代码、有内容，见 §2）：

- 地理：程序化海陆/高程 → 气候（环流/季风/雨影/四季）→ 水文（河流/洋流/通航）→ 群系（92 条内容）→ 资源；
- 社会：聚落与行省、人口块与阶层、文化/宗教/传统/语言、政权与法律、物产与配方、革新树（186 条）、军队与战争、角色与家族遗传；
- 时间：日 tick，29 个调度注册项（§4.2）。

**玩家现在真正能做的动词**（`App.Contracts` 全部接口 = `IGameSession` / `IGameUiHost` / `ISettlementDetailUi` / `IMainMenuUi` / `ISettingsService` / `IUiFactory` / `IAppFlow`）：

1. 开新档/读档/存档/暂停/调速（`IGameSession`）；
2. 看地图（6 种显示模式）、涂改地形（Rendering/Editing）；
3. 点聚落看详情（`OpenSettlementDetail`）；
4. **推行法律 / 颁布法令**（`ILawQuery` / `ILawCommand` / `IDecreeCommand`）——目前**唯一**深交互系统；
5. 设置、主菜单。

**结论**：这是一个"世界会动、玩家只能立法"的沙盒。总纲的任务就是把这个事实变成**有意的设计**，并把缺失的动词按顺序补上——而不是继续横向铺系统。

**产品目标（v1.0 可玩定义）**：

> 玩家能在一局 100–300 年（模拟年）里，通过 ①法律/法令 ②税收与财政 ③战争与和约 ④研究与采纳革新 四类动词，改变一个政权的命运，并能在 UI 上看到改变的结果（人口、财政、疆域、技术、稳定）。

**不追求**（明确排除，避免范围膨胀）：多人联机、Mod 编辑器 GUI、3D、实时战术战斗、百科式内容量、民法/刑法细则、国际组织法。

**方向由宪章约束**（[`DESIGN_CHARTER.md`](DESIGN_CHARTER.md)）：玩家是**历史条件中的干预者**，不是历史目的的执行者。因此——

- 不设"文明等级/时代进度条"这类辉格式总指标，阶段用可观察状态（人口规模/政体形态/阶层数/疆域）表达；
- 因果必须从下往上（地理→生计→剩余→分层→制度），不允许从结果倒推；
- 时代（`InnovationEra`）只作内容分层与 AI 偏好，**不作解锁硬门槛**（反辉格史观）。

---

## 2. 世界与内容现状（事实盘点）

| 维度 | 数值 | 来源 |
|---|---|---|
| Runtime `.cs` | 1025 | `_fc_verify/recon-structure.txt` |
| 程序集（asmdef） | 15（12 运行时 + 3 工具/测试） | `CURRENT_ARCHITECTURE.md` |
| Simulation 侧 `.cs` | 449（Systems 109 / State 71 / Laws 34 / WorldState 34 / Units 30 / Genetics 26 …） | 同上 |
| World 侧 `.cs` | 90（Settlement 48 / Terrain 14 / Anchor 5 / Biome 4 / Climate 4 / Hydrology 4 / Generation 5 / Editing 4） | 同上 |
| 主循环调度注册项 | **29**（27 Daily + 2 Monthly） | `GameWorld.Simulation.cs:792-852`；逐项清单见 `audit/H-innovation-spine.md` |
| EditMode / PlayMode 测试文件 | 137 / 7 | `_fc_verify/recon-structure.txt` |
| SaveVersion | 17（未批准不改） | `Infrastructure/Save/*.cs` |
| UI 面板 | 6（SaveLoad / Society / SettlementDetail / Government / Settings / MainMenu） | `Assets/UI/Panels/` |
| 内容域 | 21 个目录 / 31 个 JSON | `_fc_verify/content-inventory.txt` |

**内容量（JSON 实测）**：

| 内容 | 条目 | 备注 |
|---|---|---|
| 革新 `Innovations.json` | 186 | 231 KB，最大内容包 |
| 群系 `Biomes.json` | 92 | |
| 宗教 `Religions.json` | 44（教义 46） | |
| 法律 `Laws.json` | 12 组 / 49 法 / 2 法令 | 48 KB |
| 配方 `Recipes.json` | 43 | |
| 物产属性 `GoodsProperties.json` | 29 | |
| 装备 `Equipment.json` | 23 | |
| 头衔 `Titles.json` | 15 | |
| 传统 `Traditions.json` | 13 | |
| 家族传统 | 9 | |
| 种族 `RaceDefs.json` | **0**（代码硬编码 `races[0]` 兜底，`GameWorld.Core.cs:850`） | 内容层实际为空 |
| 本地化 | en/zh-Hans/zh-Hant 各 135 条；de/es/fr/ja/ko 各 0 条 | UI 文案大量硬编码 |

> 这张表本身就是结论：**内容层最厚的是革新（186）与群系（92），最薄的是种族（0）、族群（1）、语言（1）**——而这三者恰好是"文明演化"叙事的主角。内容分布与产品目标不匹配，这是 §8 的第一个待议项。

---

## 3. 系统状态矩阵

> 每行证据锚点在 `docs/plan-v3/audit/<域>.md`；此处只给结论与"缺哪一条"。
> 域缩写：A 世界地基 / B 聚落人口 / C 角色遗传 / D 政体法律 / E 文化信仰 / F 经济物资 / G 军事外交 / H 革新与主干。

### 3.0 系统清单（total 16 组，后续里程碑只在此清单内编号）

| 组 | 系统 | 状态备注 |
|---|---|---|
| **⚠ 重构中（不计完成度）** | **法律 / 信仰宗教 / 社会四维 + 需求** | 见 §3.0.1；总纲对它们的"半跑/空转"只是快照 |

| 组 | 系统 | 归属里程碑 |
|---|---|---|
| S1 世界与时间 | 世界生成、地形、气候、水文、群系、地形动态、地图编辑、时间与调度 | M2 |
| S2 空间与聚落 | 锚点（Anchor）、聚落、行省、影响力场、控制权、聚落演化、废弃、毁灭 | M2/M3 |
| S3 人口 | 人口块、阶层与职业、迁移、承载力、饥荒、疾病与健康 | M3 |
| S4 角色 | 角色本体、遗传/DNA、身体部位、特质、家族、关系、知识、称号 | M8 |
| S5 政权 | 政权、政体形态、政体组件、行政区划、官职、继承 | M4/M8 |
| S6 法律 | 法律组/法律、法令、推行（pending）、效果结算、逐级下达、司法、规范阶段 | M5 |
| S7 经济 | 物产、配方、生产、贸易中心、市场与价格、货币、税收、建筑与建造、阶层需求 | M3/M4/M6 |
| S8 军事 | 军队、海军、单位、行军、寻路、战斗、战争、和约、战争法理 | M7 |
| S9 外交与关系 | 外交、条约、关系值、威胁感知 | M7 |
| S10 革新 | 革新树、研究点、角色练习/熟练、突破、解锁引用 | M6 |
| S11 文化信仰 | 文化、传统、信仰/宗教、教义、语言、族群、思潮、传教；**信仰大系统单列见 S18** | M9 || S12 表现与工具 | UI 面板与展示模型、地图渲染、App/启动、编辑器工具、本地化、音频 | M10（贯穿） |
| S13 地基 | 调度器、事件总线、存档 DTO/迁移、门禁测试、确定性 | M1（贯穿） |
| **S14 生物学基底** | **种族（群体基线：等位频率/基因组 id/三项基准/寿命/耐受/偏好）** + **DNA（个体差异：6 基因座/基因组重组/表现型/才能缺陷）** | M3（群体）/ M8（个体→决策） |
| **S15 计划与执行** | `Plan` 通用项目框架（10 类：研究/密谋/工程/建设/探险/贸易/外交/迁徙/军事/自定义）+ 执行器 + 状态机 + 等待条件 + 参与者/需求 | 横切（M3–M7 的承载方式） |
| **S16 头衔与官职** | 头衔定义（tier/superior/模块/权利束/绑定）、头衔实例与持有（sole/senior/co-ruler/elector/regent + share）、继承三模式、官职（office 模块 + 职权）、称谓/尊号/绰号 | M5（官职/授予）与 M8（继承/共持） |
| **S17 关系层** | `EntityRelation` 17 种类型（领属/占领/附庸/共治/保护/租借/朝贡/委任/雇佣/指挥/从属/驻军/空间从属/行政…）+ 注册表 + 投影 + 端点校验 + 存档策略 | 横切 M5/M7/M8/M9 |
| **S18 信仰大系统** | `family → religion（必挂）→ denomination（必挂，承载主流/分歧度）→ faith（传播/分裂单位）`；`lineage`（传承）= faith 指向父信仰；**神祇/万神殿 + 四种结局（消亡/降下位/妖魔化/剥离神性）**；**主保 Patronage 统一**（神祇=多神教、圣人=一神教、祖先=原始，同一结构）；封圣四步流程；**非独立组织统一模型**（修会四型、商会、兄弟会、学会、会社 = 实体+首领头衔+章程+财产+特许权）；**特许权 `Privilege`** 作为法律例外；+ 教理 8 词 + 个人层（个人信条 / 灵性满足）；性能：交互按索引分发 | M9（重构中）与 M5（特许权法律侧） |

> **S14 两层不对称**：
> - **DNA 层已完成且真在跑**：`DnaSystem.ActiveLoci` 六基因座（Longevity/Intelligence/Martial/Resistance/Appearance/TalentDefect）、`GenerateRandom` 用种族频率播种（缺省 0.5）、`Inherit` 父母等位基因 + 重组/突变、`ComputeExpression` 表达为属性偏移；`GeneticsEngine` 616 行 / `GeneticsCatalog` 390 行 / `GenomeInstance` 入档；内容 `DnaDefs.json` 8 条（4 才能 + 4 缺陷）。
> - **种族层是空的**：`RaceDefs.json` 0 条，只有 `InitializeDefaultRaces()` 硬编码的"人族"（`GameWorld.Core.cs:848-862`）⇒ 所有族群共用 `genome_human` 与同一组等位频率 ⇒ **无群体分化**。
> - **真正缺的不是"差异"，而是"差异进入历史"**：`GetEffectiveStats` 只有 4 个下游；选将/立法/继承/指挥全部零调用（`FindBestGeneral`/`CalculateRuleAbility` 等）；表型 `RefreshAppearance` 零调用；legacy `DnaData` 读档置 null 造成衰老曲线漂移、`genome` 读档重随机。

> 状态矩阵按"先复验、后子审计"排列：3.1 全局统计 → 3.2 Lead 亲验行 → 3.3 起为各域补充行（D/B/A/E/C/G）。

#### 3.0.1 重构中的系统（只登方向与接口，不登完成度）

| 系统 | 正在改什么 | 代码现状（快照） | 里程碑只需做 |
|---|---|---|---|
| **法律** | 域/组/项三层、位阶与冲突裁决、根本化 | 主线在跑（唯一深交互）；双源单向下行、司法链空转、死字段多（D 域） | 固定 `ILawQuery/ILawCommand/IDecreeCommand` 契约 + 接回下游（`LawComposition` 回写）；**不冻结内部形态** |
| **信仰 / 宗教** | 传统为主 vs 宗教两级（D8 待裁决）；冻结 8 条革新（`InnovationReviewPolicy.IsFrozen`：602/604/601/809/810/958/982/1017） | CK3 式 Religion→Succession 两级在跑；传统无运行时对象（E 域） | 固定"宗教身份必填 + 查询契约"；不推进内容重构（`REFERENCE_GAMES_UNIFIED_PLAN` §3 冻结域） |
| **社会四维 + 需求** | **四维 = 阶级 / 阶层 / 身份 / 职业**（统一在 `IdentityProfile`）；**需求四轴各出一套**；legacy `SocialClass`(5)/`SocialSubclass`(12) 降为映射源 | **数据层已建好**：`IdentityProfile { economicClass（阶级 8）, stratum（阶层 5）, identity（身份 7 位标志）, occupations（职业 14 位标志） }`；`PopulationIdentity.Resolve` 四轴默认时回退 `LegacyIdentityMapping`；`SocietyManager.cs:153` 已按阶级/阶层聚合（`[NonSerialized]` 不入档）。**决策层仍只用 legacy 大类**：需求 `EvaluateClass(SocialClass)` 按 5 大类评 8 维；身份/职业零判定 | 固定四轴读取契约；需求按四轴各出（`INeedQuery.GetReports(axis)`）；**不锁内部形态** |

> 规则：这三块在重构完成前**不计入"半跑/空转"统计**，也不作为里程碑验收对象；里程碑只保证"别人能通过契约读到它、它的输出有下游"。没有列进补充行的系统，以 `docs/plan-v3/audit/<域>.md` 的 §1 表为准。

#### S15 计划与执行（`Simulation.Planning`，10 个文件；Lead 亲读）

| 项 | 事实 | 证据 |
|---|---|---|
| 类型 | **10 类**：Research / Intrigue / Engineering / Construction / Exploration / Trade / Diplomacy / Migration / Military / Custom | `PlanType.cs:3-15` |
| 形态 | `Plan` 带 `initiatorId / ownerId / targetId / targetKind / purpose / title / description / progress / elapsedDays / estimatedDays / participants / requirements / activity(id/defId/stateCode) / waitCondition / isWaiting / resultCode` | `Plan.cs:10-56` |
| 状态机 | Proposed → Accepted → Preparing → Executing → (Paused) → Completed / Failed / Cancelled；转移表显式校验 | `PlanSystem.cs:375-412` |
| 调度 | `plans.daily`（order 30）每日驱动 active 计划；`IPlanExecutor` 按 `PlanType` 注册 | `PlanSchedule.cs:20`；`PlanSystem.cs:237-304` |
| 事件 | `PlanCreated / PlanStateChanged / PlanCompleted / PlanFailed / PlanCancelled` | `PlanSystem.cs:29-37` |
| **执行器** | **只有 2 个**：`ResearchPlanExecutor`、`ConstructionPlanExecutor` | `ResearchPlanExecutor.cs:7`、`ConstructionPlanExecutor.cs:5` |
| **创建者** | **只有 1 个在跑**：`ResearchPlanSystem`（`:208` 调 `CreatePlan`）；`ConstructionPlanSystem.CreateConstructionPlan` **零调用者** | grep `CreatePlan(` 生产命中仅此两处 |
| 存档 | **计划不入档、也没有 DTO**（H 域） | `SaveData.cs` 无 plans 段 |
| 判定 | **半跑**：框架完整、研究类在转、建设类只差一个调用者、其余 8 类从零 | — |

> 这条比它看起来重要：`Plan` 是这座工程里**唯一"过程化"的通用载体**——有计划、有中断、有失败、有参与者、有需求、有等待条件。它天然就是宪章原则 11（过程优先）与"玩家动词落地"的骨架：造城、开垦、远航、联姻、密谋、围城、变法，本来就该是计划而不是瞬时数值。**现状是十种过程只有研究一种在转。**

#### S16 头衔与官职（Lead 亲读 + D 域互证）

权威裁决在 `docs/design/DECISIONS_M1_M2_M3.md` §1（M1）：**头衔 = 法律地位（权利束 + 绑定），官职只是带 office 模块/权利的头衔**；`kind` 是**派生视图不落库**（Landed/Office/Honor/NobleRank/Claim）。

| 层 | 事实 | 证据 |
|---|---|---|
| **三模式是假分支** | `GameWorld.Succession.cs:62-77` 的 `definitionId` **恒绑 `TitleCatalog.Highest(Landed)`**，而 Highest 恒为 `title_basileus`(3.1) ⇒ `successionMode` **恒为 RealmLaw**；`None` 分支不可达；`OwnLaw` 与 `RealmLaw` 走同一 else；`ownLawPreset` **全仓零消费者**（`InheritanceLawCatalog` 仅测试引用） | M 域 §1 |
| 称号退化 | 同一 Highest → `RealmTitleEvolution.TitleWord:48-59` **无 basileus 分支** ⇒ 编年史即位条目一律"某人即位为王"；`RealmTitleEvolutionTests` 通过属**巧合** | M 域 §2 |
| 定义层 | `TitleDefinition` 36 字段，`LoadFromContent` **只映射 7 个**；`tier`/`superiorTitle`/`eligibility`/`seats` **JSON 里不存在** ⇒ **29/36 字段无生产者**；`ResolveAppellation`/`FilterEligible`/`ByTier`/`ChildrenOf`/`GetChain`/`CountByTier` 生产 0 调用 | `TitleManager.cs:32-41` |
| 实例/持有 | `TitleInstance`（titleId + holders）+ `TitleInstanceRegistry`；`TitleHolding.role` 生产只写 `Sole`，`share` 恒 1、`institutionId` 恒 −1；**`TitleInstanceRegistry.Clear()` 的唯一生产调用在 Obsolete 路径** ⇒ 读档/新局**残留上一局的持有者** | M 域 §3 |
| 开局/读档 | 头衔持有**只在继位时写入**（`NotifyRulerTransition`，由 `:159/:182` 触发）⇒ **开局没有任何头衔持有**；绝嗣时还会写入 `characterId=-1` 的在任 Sole 持有（无"空缺"语义） | M 域 §4 |
| 官职三套并行 | ① `officeHolders`（**真源，入活档**）② `office_*` 影子 `TitleInstance`（**无 definitionId**）③ `AdminDivision.holderCharacterId`（独立）。6 席补缺唯一调用在 `GameWorld.Simulation.cs:880`（**仅战末**）⇒ **开局 0 官职** | M 域 §7 |
| 死字段 | `CharacterData.ownedTitles` **入档但 0 写 0 读**；`EpithetCatalog` 81 条**零引用**（grep=1 仅定义） | M 域 §6 |
| 存档（致命） | 头衔实例与 `primaryTitleKey` **只写在 `GameSaveData`/`GameSaveSystem`**（整类 `[Obsolete]`）；活存档 `SaveData` 无这两个字段 | `GameSaveData.cs:55,66,92` vs `SaveData.cs`（grep 0） |
| 称谓 | `appellationId` 无生产者、`ResolveAppellation` 零调用、`TitleAppellationContext` 五字段零使用 | D/M 域 |
| UI | 无头衔面板、无授予（`ActionIds.AppointTitle` 零使用者）；"官职体系"段因 `UIManager.Panels.cs:679` 不传参**永不渲染**；区划树把 `titleId` 原串（如 `title_zhuhou`）**直接打印给玩家** | K/M 域 |

> **判定：半跑，且"设计的 5 条只落地 1 条"**。已落地：头衔实例 + 继位转交（single holder）。未落地：① `kind` 派生视图（无）；② 位阶组合序（仍是单值 `rank`）；③ 共持/摄政/选举人角色（只写 `Sole`）；④ 资格/席位/任期（字段不存在）；⑤ 称谓层（只有壳）。加上**存档记在死路径**与**官职三套并行**，这套系统目前只是"有名无实的一行记录"。
> **连带结论**：M8（角色与继承）不能先做——② 必须先由 **M1 闭合存档**（`titleInstances`/`primaryTitleKey` 迁入 `SaveSystem`，并补 `Clear()`）；③ 官职三套先合成一套（`officeHolders` 为真源）；④ 位阶改组合序后再谈头衔树与授予 UI。

#### S17 关系层（`EntityRelation`，17 类）——类型最丰富、实现最空

| 项 | 三态 | 事实 | 证据 |
|---|---|---|---|
| 关系类型 | — | **17 种**：DirectRule / MilitaryOccupation / Vassalage / PersonalUnion / Protectorate / DelegatedRule / Lease / Condominium / Tributary / Commission / Employment / Command / Subordination / Garrison / SpatialSubordination / Administrative | `RelationType.cs` |
| 关系实体 | — | `from/to`（`EntityRef{kind,id}`）+ `type` + `legitimacy/stability` + `isPrimary` + **`isProjection`** + `sinceDay/expiresDay` + **`obligations`（裸字符串列表）** | `EntityRelation.cs` |
| 注册表 | **空转** | 按**目标**索引；`Add` 过 `RelationRules.IsAllowed`；`isPrimary` 冲突时**后写覆盖**；`Capture()` **跳过投影** | `RelationRegistry.cs` |
| 投影 | **仅测试调用** | 从物质状态推导：附庸（`suzerainId`）/领属（地块归属+锚点）/军事占领/委任（军队）/雇佣（行为体）；唯一调用点 `RelationProjectionTests.cs:40` | `RelationProjection.cs` |
| 生产写入 | **0** | 全仓无生产 `RelationRegistry.Add`；`GameWorld.Core.cs:480` 只有 `Clear()` | grep |
| 存档 | 存空表 | `SaveSystem.cs:376` 捕获 + `:435` 还原，但表恒空 | SaveSystem |
| 消费者 | **0** | 没有任何系统按关系查询（附庸链/领属/雇佣各自读自己的字段） | grep |

> **设计意图（从 `isProjection`+`Capture` 反推）**：**物质关系是投影（不入档、可重算），契约关系是真源（入档）**。这条规则清楚，但从未被兑现。
> **与现有存储的重复（5 处）**：`RealmData.suzerainId/vassalIds`、`DiplomacyManager._subordinations`、`DiplomaticRelation`、`TitleHolding`、`CharacterRelation`——关系层是**第 6 套**。必须先决定它是"统一读模型"还是"真源"，否则接入只会制造第 7 套。
> 详见 [`RELATION_LAYER.md`](RELATION_LAYER.md)（17 类清单、六点缺口、五条验收信号）。

> **判定：半跑，且"存错了地方"**。模型与继承接线是真的（头衔实例、继位转交、官职实例都跑），但：① 位阶仍是单值 `rank`，与已裁决的"组合序+绑定"冲突；② 定义只加载 7/36 字段，头衔树/资格/席位全空；③ **头衔落在一个已废弃的存档器里**，活存档读回来是空的；④ 称谓体系只有字段壳。
> **连带结论**：M8（角色与继承）不能先做，必须先由 **M1 把存档闭合**（把 `titleInstances`/`primaryTitleKey` 迁进 `SaveSystem`），否则做的继承在"读档后"全部蒸发。

#### 已由 Lead 亲自复核的行（不依赖子审计；证据均为本人 grep/read）

| 系统 | 三态 | 驱动证据 | 入口 | 存档 | 关键缺口 |
|---|---|---|---|---|---|
| S6 法律/法令（主线） | 在跑 | `laws.pending.daily`(26)/`laws.effects.yearly`(24)/`decrees.propagation.daily`(37) `GameWorld.Simulation.cs:802,807,840` | `GovernmentPanel` → `ILawCommand`/`IDecreeCommand` | 政权 lawState 随 realm 入档 | 革新门槛已接通（`LawContracts.cs:65,229,258`），但 `normKind/scope/precedence/overrides` 无消费者 |
| **S6 法律生命周期（八态）** | **半跑（只 3 个转移在跑）** | `LegalNormStage` 八态齐全（Unknown/Known/Proposed/Pending/Active/Consolidated/Fundamentalized/Repealed），注释明确"根本化需革新+程序+合法性"；但 **Consolidated/Fundamentalized 无写入方**，`fundamentalizedLawIds` **无读无写** | 无 | 未入档 | 见 [`LAW_LIFECYCLE.md`](LAW_LIFECYCLE.md) |
| **S6 习惯（Custom）法源** | **骨架在，桥没有** | `LegalNormKind.Custom`；`LawGroupDef.fallbackLawId` + `LawDef.isFallback`（JSON **0 次使用**）；传统侧 `TraditionDef.solidification`/`RoleGateFulfilled`/`upgradesFrom`/`linkedInnovationIds` **全零消费者** | 无 | 传统不入档 | 习惯→法、法→习惯两座桥均未实现 |
| S6 法律冲突与效力 | 空转 | `normKind/scope/precedence/issuerCharacterId/overrides` 全在，**JSON 0 次使用、无读取点**；`LegalScope` 无消费者 | 无 | 随目录克隆 | 无层级、无地域效力、无人格效力（身份轴未接） |
| S6 司法（罪行→审判→判决） | 空转 | `justice.daily`(27)`:830` 每日跑，但 `GetOrCreateJudicialSystem` 唯一调用方是零调用者的 `TryTrialCrime`（`:1191`） | 无 | 不入档 | 全链 0% 通电，且无案件实体 |
| S6 规范阶段（根本化） | 空转 | `LegalNormStage` 唯一使用点=字段默认值 | 无 | — | 六态永不出现；`fundamentalizedLawIds` 无读无写 |
| S10 革新（AI 推进链） | 半跑·缺入口 | `InnovationResearchSchedule:835` / `ResearchPlanSchedule:837` / `PlanSchedule:838` | **无**（`Assets/UI` 零 `Innovation/Research` 命中） | 只存政权级结果 | 玩家不能选课题；`unlockRefs` 84 条无消费者 |
| S10 革新·发现/进度/熟练 | 空转 | `MonthlyTickProgress`、`Discover`、`DiscoveryActors` 零调用 | 无 | `progress`/`resourceOutput` 恒空 | 熟练永不涨 ⇒ 突破链只能吃 19 条根革新 |
| S10 革新 ↔ 法律门槛 | 在跑（2026-11-09 修复） | `LawContracts.OwnedInnovationIds` 三处调用 | 经法律面板生效 | — | — |
| S3 人口（增长/承载力/满意度） | 在跑 | `PopulationSchedule` → `PopulationTick`（`GameWorld.Simulation.cs:53-109`）：逐块增长 × 承载力压制 × 满意度 → 饥荒/叛乱事件 | 间接（聚落详情） | PopulationBlock 随 tile 入档 | 饥荒判定用 `tradeCenter.inventory[0]` 单一物产代替食物篮子（`:90-92`） |
| S5 政权（政治经济日循环） | 在跑 | `PoliticsSchedule` → `PoliticsSimulationSystem.DailyTick`（`:74-130`）：收税→金库、稳定度向集权度回归、阶层关系、派系、政体变迁 | `GovernmentPanel` 部分 | 随 realm 入档 | 玩家不能调税率/看收支表 |
| S7 税收/财政 | 在跑 | `PoliticsSimulationSystem.cs:86` `SettleTaxes(realmId, LawEffectRuntime.GetTaxMultiplier(...))` → `treasury +=` | **无**（仅"税负"文案 `ClassPanelText.cs:33`） | 金库入档 | 法律乘子有效，玩家无一等公民入口 |
| S7 贸易/物流 | 在跑 | `EconomySchedule` → `EconomyManager.DailyTick`（`:109-127`）：库存容量/供需/路线效率/发车/移动/消费/货币/保质期；发车在 `InitiateTrades` 内建商队（`:199-208`） | 无 | TradeCenter/商队部分入档 | 纯自动经济，玩家既看不到也调不了（无市场面板） |
| S7 配方/生产（工艺链） | 空转 | `SettlementProduction.TryCraft` 全仓仅测试调用（`Tests/EditMode/{RecipeGateTests,SettlementMarketTests,DynamicGoodsTests}`） | 无 | `SettlementMarketSaveData` 存在 | 43 条配方与革新 `unlockRefs` 全部无产品侧消费者 |
| S8 军队/海军实体 | 不存在（生产路径） | `CreateArmy`/`CreateNavy`（`:1151,1167`）**零产品调用者**，仅 `SaveSystemRoundTripTests:110`、`WorldGenerationSmokeTests:97,141` | 无 | 有 `ArmySaveData`/`NavySaveData` | 没有军队生成本身 ⇒ 战争系统在空集合上跑 |
| S8 战争（宣战/结算/和约） | 半跑 | AI 可宣战 `AIController.cs:309`；结算 `CombatManager.UpdateWarOutcomes` ← `ProcessWarOutcomes`（`:862`） | 无（仅 `MapDisplayMode.Diplomacy` 地图模式 `UIManager.cs:496`） | `SaveData.cs:48-49` 战争列表入档 | 无军队 ⇒ 无接战；无玩家宣战入口 |
| S13 存档 | 在跑 | `SaveSystem` + 22 个 DTO 文件；SaveVersion 17；`Logs/editmode-results.xml` 实测 706/706 绿 | — | — | 革新研究计划/熟练、司法状态不入档（**连 DTO 都没有**，非"Adapter 缺失"） |
| S10 创新·研究计划链 | 半跑 | `research.breakthrough.daily`(29) 在跑，但 mastery 恒 0 ⇒ 卡死 167/186 条带前置革新 | 无 | 不入档 | 唯一"新链"零测试覆盖 |
| S10 创新·旧研究链 | 半跑 | `innovation.legacy_research.daily`(29) | 无 | 只存政权级结果 | 与突破链共享 `currentResearch` 槽位，可互相卡死 |
| S10 Plans（计划系统） | 半跑 | `plans.daily`(30) 只跑 Research 分支；Construction 分支因 `CreateConstructionPlan` 零调用者而死 | 无 | 不入档 | 计划是"玩家意志"的天然载体，目前没有入口 |
| S12 AI | 半跑 + 死代码 | `ai.daily`(28) 在跑，8 类 intent 均映射到已注册 command（无断链） | 无（AI 代玩家决策） | AI 状态不入档 | **512 行 AI 死代码**：`AIDecisionSystem`(312)/`AISettlementAISystem`(164)/`AIState`(22)/`AIBuildDecision`(11) 零实例化零测试 |
| S12 观测面（UI） | 半跑 | 6 面板只消费 6 个查询接口 | — | — | **29 项调度里 22 项没有任何界面可触发或观察** |
| S7 物资生产（P0） | **不存在** | `TradeCenter.inventory` 全部写入者只有三处：开局播种 `GameWorld.Core.cs:842-843`、商队到货 `TradeCenter.cs:64`、读档 `TradeCenterDTO.cs:72`（Lead 亲验 grep） | 无 | 库存入档 | **粮食从 1000 单调递减到 0，之后饥荒长期触发**——生产链根本不存在 |
| S7 加工/配方执行器 | 空转 | `SettlementProduction.TryCraft` 只被测试调用；`SettlementData.GetOrCreateMarket` 亦然 | 无 | market 入档 | 43 条配方 / 34 条革新门槛无可达执行器 |
| S7 贸易路线 | 空转 | `TradeRoute` 全库只有测试 `new`（`EconomyConservationTests:141` 等），生产期从不建路线 | 无 | — | 发车与价格公式遍历空表 ⇒ 物流"看起来在跑其实无货" |
| S7 货币四阶段 | 空转 | `currentStage` 无写入者；`DebaseCoin`/`IssuePaperMoney` 零调用；日结衰减的是全局副本而非 `realm.currencySystem` | 无 | 货币入档 | 通胀只进评分，不影响交易 |
| S7 建筑/施工 | 空转 | `BuildBuilding`/`StartPlannedBuilding`/`Demolish` 零调用；`buildingLevels` 恒 0 | 无 | 入档 | 29 条 `BuildingDef` + 上限 5 + 完工写级全部无入口 |
| S7 税收（深度） | 半跑 | 每日入 `realm.treasury`（`PoliticsSimulationSystem.cs:86-87`，法律乘子已接） | 无 | 金库入档 | 用全局 `TaxSystem` 而非 `realm.taxSystem`；税基=`fertility×development×10`（无人口/控制度）；`miningTax`/`saltMonopolyTax` 不进公式；税率无写入者 |
| S8 军事-经济联动 | 部分 | 只通"传令官"一条链（`GameWorld.Simulation.cs:671-686` → `TreasuryLedger`） | 无 | — | `UnitDef.recruitCost` 写而不读；`Army.supply` 是抽象 0–100（`Army.cs:123-137`） |

#### D 域（政体/法律/头衔）补充行——三条 P0

| 系统 | 三态 | 事实 | 影响 |
|---|---|---|---|
| 法律双源（`LawComposition` ↔ `LawDef` 目录） | **半跑（单向下行）** | `LawReform` 可下行同步（`RegimeChangeDynamics.cs:261` → `LawRealmHost.ForceReformSync:197`），但**目录→Composition 无任何路径**：玩家在 `GovernmentPanel` 推行的法律只写 `realm.lawState` | 玩家改"常备军/重赋"**不影响**区划层数、AI 扩张性、政治通道、特权保障；反之派系政变改的参数不被面板反映 |
| 头衔实例存档 | **丢失** | 唯一实现 `GameSaveSystem.cs:292-311,636-646` 带 `[System.Obsolete("Legacy save path; use SaveSystem. Not wired into the game loop.")]`（`:39`）且零外部引用；活路径 `SaveData.cs:22-76` 无 `titleInstances` | 读档后 `TitleInstanceRegistry` 为空，持有沿革蒸发；每次继位被当"首次" |
| 头衔层级 | **扁平化** | `TitleManager.LoadFromContent:32-41` 只映射 7/36 字段；`Titles.json` 无 `tier`/`superiorTitle` 键 → `Highest(Landed)` 恒为 `title_basileus`(rank 3.1) | 每个政权的 primaryTitle 都绑 basileus，编年史恒为"XX之王"；`ByTier/GetChain/FilterEligible` 零调用者 |
| 人力乘子（49 法中 27 法带非 1 值） | **空转** | `LawEffectRuntime.GetManpowerMultiplier` 唯一调用者 `ManpowerSystem.cs:83`，而该重载仅测试调用；`ManpowerSystem` 全部重载在 `Assets/Scripts` 内零产品调用者 | 法律对可征兵额与补员**完全无效** |
| 官职补缺 | **半跑（低频）** | `EnsureOfficeHolders`（`GameWorld.Simulation.cs:270-346`）唯一调用点 `:880`（战争结算后） | 6 席官位平时长期空缺；摄政推举候选池、法令传递目标都拿不到人 |
| `centralization` | **无法律来源** | 唯一由法律推导的点 `LawSystem.cs:40` 所在类**从未被实例化**；写入点只有 `GameWorld.Generation.cs:91`、`CultureStageEvolutionHost.cs:85,94` | `LawEffects` 里 20+ 项集权度加减全部落空 |
| `GovernmentPanel.Refresh` | **副作用** | `:288-289` 在 UI 刷新里调 `TickPendingLaws`/`TickDecreeExpiry` | 打开面板就推进模拟；且 `TickDecreeExpiry` 无 tick 侧调用者 → `decreeState.active` 只增不减 |
| `realmForm` / `heirId` | **不入档** | `RealmDTO` 与 `RealmOfficeSaveData` 均无这两个字段 | 读档后行国回落定居（影响游牧人口统计口径）；指定继承人丢失 |

#### 3.1 全局统计（先看这张表；各域补充行在本节其后）

| 域 | 系统数 | 在跑 | 半跑 | 空转 | 不存在 | 该域最硬的一条 |
|---|---|---|---|---|---|---|
| A 世界地基 | 23 | 6 | 10 | 5 | 2 | 地形编辑破坏性重算（抬一格即沉海） |
| B 聚落/人口 | ~30 | 8 | 8 | 12 | 1 | 聚落等级永不推进 + 两套人口不收敛 |
| C 角色/遗传 | ~30 | 6 | 8 | 13 | 1 | 新局 `monarchId=-1` ⇒ 统治者死了无人继位；无婚姻 |
| D 政体/法律/头衔 | ~27 | 11 | 8 | 8 | 0 | 法律双源单向下行（改法不影响治理） |
| E 文化/信仰 | ~18 | 4 | 3 | 11 | 0 | `stateReligionId` 无写入 ⇒ 5 链同死 |
| F 经济/物资 | ~12 | 3 | 3 | 6 | 1 | 物资只有消耗没有生产 |
| G 军事/外交 | ~28 | 2 | ~11 | ~14 | 1 | 军队从不生成；宣战后**永不结束**（白和条件 730 天 vs 天数上限 365） |
| H 革新/主干 | ~29 调度项 | — | 3 | 1 | — | 玩家入口 0 项；22/29 无任何界面 |

> 统计口径：各行数字来自各自审计的逐项判定；同一系统在三态上按"最弱环节"归类（半跑优先于在跑）。C 域（30 项 / 在跑 6 / 半跑 8 / 空转 13 / 不存在 1）与其余六域均为**逐项判定**，无遗漏域。
> 已可下的总判断：**"半跑"是这座工程的默认状态**——不是少数系统没做完，而是绝大多数系统都停在"有代码、有测试、缺一条到玩家或到下游的链"。

#### K 域（表现与入口）补充行——玩家能点的比"动词表"还少

| 项 | 三态 | 事实 | 影响 |
|---|---|---|---|
| 地图点选 | **不存在** | `HandleMouseClick` 空实现（`UIManager.cs:81-84`）；`_selectedTile` 仅测试可写 | 地块详情恒 "-"、`viewRealmButton` 恒隐藏 ⇒ 政权总览/区划下钻整链不可达 |
| 显示模式切换 | **不存在** | `Main.unity:7757-7760` 的 `mapMode*` 三件套全为 `{fileID:0}`；`MapModeList` 子节点为空（`:9824`） | 12 种视图（不是 6）实际只有 Religion 一种 |
| 序列化断链 | **9 处** | `characterPanel:7763`、`societyOpenButton:7779`、`musicOpenButton:7783`、`familyTreeOpenButton:7794`、`mapMode*(7757-7760)`、`mapInfoText:7804`；对应场景对象仍在 | 4 个面板 + 模式栏 + 地图统计 = 死 UI |
| 存档入口 | 半跑 | `SaveLoadPanel` 只能读+删，**没有存档按钮**；游戏内保存只有 F5 | 玩家会以为"存不了档" |
| 读档失败 | 缺陷 | F9 失败只 `ChangeState(MainMenu)`，不 `ReturnToMainMenu`（`GameManager.cs:315-319`） | 停在 MainMenu 状态但界面仍是 HUD ⇒ 软锁 |
| 地图编辑 | 不存在 | `RuntimeMapEditingController` 场景无实例；`IMapEditController` 全工程 0 实现 | 12 个编辑命令 0 可达 |
| 契约空转 | 半跑 | `ISettingsService`/`IAppFlow`/`IUiFactory.EnsureGovernmentButton` 在 `Assets/UI` 零消费；`ICulture/IInnovation/IFaction/IRegime/ISettlementPeople` 五子契约无 UI 用 | "UI 只依赖契约"的解耦目标只做了一半 |
| 面板计数修正 | — | `SocietyPanel.cs` 实为 `SocietyPanelText` 静态类（`:11`），**不是面板** ⇒ 真面板 5 个 | 我之前"6 面板"的说法要改成"5 面板 + 1 静态文本类" |
| 渲染 | 半跑 | `MapRenderer` 非每帧重算，但每次重绘最多 6 趟全图（1s 节流）；政治图效忠深度恒 0（`Colors.cs:332-336` TODO） | 可接受，但政治图层信息不完整 |
| 编辑器 | 半跑 | `Assets/Editor/**` 实为 **7 个** .cs；`[MenuItem]`/`EditorWindow` 命中 0，全部是 `-executeMethod` 工具；`BrushMode`/`BrushShape`/`ContentPackageExporter` 零消费者 | 没有可视化工具链，只有命令行工具 |
| 演示层死代码 | 空转 | `PopulationCompositionPanelModel`、`SiteTypeLabels`、`SettlementPeopleText`（仅测试）、`FactionNames`/`JunctureNames`/`ClassNames`（0 消费者）、`ShowToast`/`ShowConfirmation`（0 调用，且后者是假确认） | 观感层有大量"写好了没接" |

> **对 §1 的修正**：所谓"4 个真动词"要收紧——真正走通的只有 **立法/法令 4 个命令 + 聚落详情（图标左键）+ 新档/读档/调速 + 设置**；地图点选、模式切换、存档按钮、角色面板、地图编辑**全部不可达**。

#### I 域（内容与模组）补充行——内容一半在代码里，模组通路有一处硬伤

| 项 | 三态 | 事实 | 影响 |
|---|---|---|---|
| 内容域清单 | — | **24 项**：包通路 14 域（`ContentDefinitionIndex.cs:148-161`）+ host 3（Culture/Race/Biome，`ContentRuntime.cs:31-58`）+ 平铺 5（Recipe/Property/Goods/LanguageFamily/Localization）+ Base-only 2（Laws:32、CultureStages:24） | 覆盖面比"21 个 JSON 目录"看起来的宽，但有 5 个域是平铺另路 |
| 加载链 | 在跑 | `ContentRegistry.cs:76 → :95 → ContentSourceCatalog.cs:16-18`（Base=0 / Mods=100 升序）→ `DictionaryContentStore.cs:32` 后写覆盖 | Mods 覆盖 Base **成立**，但只支持整条覆盖，**无字段级合并/删除** |
| **Mods 布局硬伤** | 缺陷 | 仓库唯一 mod 样例 `Mods/Dna/DnaDefs.json` 被判定为**包目录**，实际会去找 `Mods/Dna/Dna/DnaDefs.json`（`ContentDefinitionIndex.cs:250`）⇒ **运行时永不加载**；`Mods` 下并存"包目录"与"平铺"两套互斥布局 | 现有 mod 通路实际不可用 |
| 内容校验 | 空转 | 唯一的包校验器**零运行时调用**（`ContentExtensionApi` 无调用者；`ContentPackageExporter.cs:17` 无 MenuItem）；14 域**无 schema/引用校验**；真校验只有 3 处：`LanguageFamilyCatalog.cs:128-168`（孤儿/成环）、`RecipeCatalog.cs:129-151`、`GoodsPropertyCatalog.cs:87`；许可门禁只在测试（`DatasetIntegrityTests.cs:98-177`） | 内容写错不会报错，只会在运行时静默缺失 |
| 空转内容域 | 空转 | `Heritage`（有加载器无文件）、`Race`（0 条且 `raceId` 恒 0 出现在 4 处）、`Equipment`（23 条，唯一消费者零调用）、`LanguageFamily`（75 节点无查询）、`Ethos`（8 条无查询） | 五个域"有内容没消费者" |
| **硬编码内容（新增发现）** | 半跑 | 商品 **57**（`GameWorld.Core.cs:601-659`）、灾害 **12**（`DisasterSystem.cs:34-144`）、建筑选址 **30**（`BuildingAvailabilitySystem.cs:22-70`）、法律→革新映射（`LawEffects.cs:390-435`，**string 键 vs int id 两套**）、绰号 **81**（`EpithetCatalog`）、经济原型 11、资源默认群系排除（`GameWorld.Core.cs:733-742`）、加载体注入革新 1/1025 与能力缺省（`ContentDefinitionIndex.cs:440-509`）、**角色疾病 32**、**精神障碍 5**（与 `Base/MentalHealth` **双源**）、**宗教 3**、地名后缀、法律迁移默认值 | 连同已知 5 张，**共 18 张表在 C# 里**；`Titles.json` 是 100% 内容驱动的反例 |
| 代码覆盖 JSON | 缺陷 | 革新 **1/1025 条**在解析循环之后被**无条件覆盖**（`ContentDefinitionIndex.cs:491,501`）⇒ 该条永远以代码为准 | 内容改了不生效，且无提示 |
| **单条坏数据杀死整个文件** | 缺陷 | `LoadReligions` 只判 `item != null`（`:416`），而 `DictionaryContentStore.cs:22-33` 对 `id=0` **抛异常**；异常被 `DelegateContentProvider.cs:32-39` 吞成 `LogWarning` ⇒ 该 JSON **剩余条目全部静默丢弃**，且 `ReligionCatalog.Load` 不执行 | 一条 id=0 就能让整份宗教内容消失 |
| 加载器空类 | 空转 | `JsonFileContentProvider` **全仓零实例化**（grep `new JsonFileContentProvider` = 0）；`Recipe/Property/GoodsProperty/LanguageFamily` + `Laws/CultureStages` 走手写 Base→Mods，**无 priority** | 两套加载路径，行为不一致 |
| 模组可行性 | — | 新革新=纯 JSON（零代码）；**新种族=数据可入但须改 4 处 `raceId=0`**；**新建筑=必改 `BuildingSystem.cs` + `BuildingAvailabilitySystem.cs` + 枚举**；**新法律=无 Mods 通路**（包白名单不含 Laws），且效果硬编码 `LawEffects.cs:51` | "加内容不改代码"目前只对革新成立 |

> 结论：**"高度可模组化"目前只在革新一个域成立**。其余域要么在代码里、要么加载器有洞（Mods 布局）、要么没有校验。这三件事与 D18/D20/D23 直接冲突。

#### J 域（单位与行为体）补充行——三套实体并行，只有一套真正在动

| 项 | 三态 | 事实 | 影响 |
|---|---|---|---|
| 行为体 | 在跑 | `settlement.map-actors.daily`（`SettlementSchedule.cs:31`）→ `MapActorManager.cs:97/106`；`SpawnActor` 运行期 5 处（`Generation.cs:484`、`Simulation.cs:680`、`LandAbandonment:147/189`、`SettlementDestruction:259`） | **AI 实现只有 9 个类**（"12"是 `CharacterAICategory` 枚举值数，含 None） |
| 生成缺陷 | 缺陷 | 开局 `SpawnActor` **不传 subcategory**（`Generation.cs:484` + `UnitAIFactory.cs:37-38`）⇒ 流民/游牧/野兽/土匪**全部走 `NomadAI`** | 不同行为体行为一样 |
| **三套实体** | 半跑 | ① `Army`（State）② `MapActor`（Actors）③ `Caravan`（State+EconomyManager）。`Army` 与 `MapActor` **互不引用**，只共享 `TransportUnit`；**`Army` 无任何 tick**；`CreateArmy` 仍 0 运行期调用者（且未被别的路径绕过，`new Army` 只在读档）；`RegisterArmy` 之后的读者**只有存档与测试** | 真正在跑的物流是 ③（商队），①完全空转 |
| 传令官链 | 半跑 | `Simulation.cs:648-703`：算距→`ResolveFundingSource`→`TreasuryLedger.TryPay`→`SpawnActor Courier`→`Equip`→`SetLeader`→`SetTargetSettlement`；`TickDecreeOrders:705`（注册 `:840-843`），失联即 Failed | **唯一入口是玩家在 `GovernmentPanel:385` 下诏**；AI 不会自动下诏 ⇒ 玩家不下诏时整链 0 实例 |
| 停摆（有类无实例） | 空转 | `PilgrimAI` 从不 `new`；`MercenaryAI` 与 6 个 leader 分支不可达（唯一 `SetLeader` 在 `:697` 只给 Courier）；`UnitFounding`/`DetachSubunit` 仅测试 | 雇佣兵/将领/建城全部不可达 |
| 停摆（有实例无 tick） | 空转 | `Army`、`MapUnitRegistry` 条目、非传令官的 `TransportUnit`（cargo/members 恒空）、`BanditAI.hasCamp` 只写不读 | 军队即便有实体也不动 |
| 停摆（有 tick 无数据源） | 半跑 | `map-actors` 的粮耗**恒 0**（寿命由常量 `supplies-0.5/天` 决定）；`camps.daily` 非沙盒恒空；`decrees.daily` 无诏；土匪劫掠无经济后果 | "行军中的人会饿"这件事没实现 |
| **行为体在客户端不可见** | **P0** | `Rendering/` 12 个文件对 actor/unit **零命中**；UI 只有 5 个面板；`MapUnitRegistry` 唯一消费者是存档（`SaveSystem.cs:488/532`） | 地图上跑着商队/难民/传令/土匪，玩家**看不到**——这是"观测面"最大的洞 |
| 传令官资金漏水 | 缺陷 | `TryFund(:674)` 早于 `FindCourierLeader(:687)`；无领队时只 `RemoveActor` **不回滚** | 玩家下诏可能白扣钱粮与货物 |
| 单位定义与属性 | 半跑 | `UnitDef` **纯硬编码 14 条**（`GameWorld.Core.cs:761-776`）；`terrainModifiers` 恒空字典（`:821`）且唯一读者 `Army.cs:110` ⇒ 零效果；`recruitCost` 17 处命中**全在写入端、零读者** | 兵种差异与地形修正都不生效 |
| 营寨补给 | 缺陷 | 营寨无补给来源，80 人营约 **12 天必自灭** | 沙盒里建立据点在机制上不可能存活 |

#### L 域（状态模型）补充行——状态没有守卫，存档静默丢字段

| 项 | 三态 | 事实 | 影响 |
|---|---|---|---|
| 规模 | — | State 71 / Definitions 12 / Events 2 = **85 文件 8738 行**；`public` 字段 **821**、`[NonSerialized]` **39** | 状态几乎全是裸字段 |
| **路径更正** | — | `TileData`/`SettlementData`/`AnchorData`/`SimulationEventBus`/`Plan` **不在** State/Definitions/Events 三目录，而在 `Core.Data`/`World.Settlement`/`World.Anchor`/`Core.Events`/`Simulation.Planning` | 后续任何"按目录审状态"的做法都会漏 |
| 不变量 | **缺失** | `RealmData` **37 个字段全 public 且无 setter/校验入口**；`treasury` **15 写点 / 7 域**；`stability` **64 写点 / 20 文件**；`population` 被 **6 个系统各自增删 List** | 任何系统都能改任何量，没有单点可审计 |
| **金库双源** | 缺陷 | `TreasuryLedger.cs:334` 用 vault 覆盖 `treasury` ⇒ 两处记账互相覆盖 | 财政账目可能对不上，且无从判断谁是真源 |
| 事件 | 半跑 | 9 类型 / 11 发布点 / **只有 3 类订阅者**；`SettlementTileResettled/Abandoned/Destroyed` **0 订阅**；`SimulationStatisticsRecorder` 全库**无实例化**（War/Season/Economy 四型事实无人听）；`RegisterEventListener` 无调用者；第二通道 `GameEvent` 空转 | "世界发生了事"没有观察者 |
| 事件派发（**Lead 更正**） | 在跑 | `ProcessEvents` **经 `EventSchedule` 方法组注册**为 `events.daily`（order 29，`GameWorld.Simulation.cs:836` → `EventSchedule.cs:20-23`），故其 5 个处理器（饥荒/叛乱/季节/宣战…）**可达**；子审计"0 调用者"是 grep 方法组导致的**误报** | 事件"没人订阅"是真问题，但"处理器不可达"不成立 |
| **外交全表不入档** | **P0** | `DiplomacyManager.cs:15/17` 的 `relations`/`alliances`/`treaties`/`casusBelli`/`subordinations` **全部无 DTO**；读档后重新 `Random` 生成，与已入档的 `wars` 互相矛盾 | 外交状态是"读档即重置"，M7 之前必须先补 |
| 存档静默丢失（补充） | 缺陷 | 除已列字段，`settlementLevel` 的同步只发生在 **Unity 序列化回调**（`SettlementData.cs:658/675`），**DTO 路径不触发** ⇒ 读档后全聚落回落 LevelI（读点 40+/10 域）；`primaryCultureId` 读档 −1 ⇒ 继承法/头衔断链 | 读档后世界不是存档前那个世界 |
| 无读者/无写者字段 | — | 无读者 **20 个**（RealmData 2 / SettlementData 8 / CharacterData 7（与 `AchievementRecord` 重复） / TileData 3）；无写者：`influenceRadius`/`bottleneckTileIndex`/`isCapital`（全库无 `= true`）/`movementControl`/`nomadicSubtype` | 20 个字段只占内存 |
| 不变量（量化） | 缺陷 | `treasury` **15 写点 / 7 域**（`TreasuryLedger.cs:334` 双货币表示）；`stability` **64 写点 / 20 文件**；`fertility` **量纲冲突**（`BuildingSystem.cs:271` 按 0–1，`SwarmAI.cs:49` 判 `>30f`）；`SocialDifferentiation.cs:54-55` 重复 `list.Add` ⇒ 收敛双跑 | 同一量有多套语义并存 |
| 内容边界错位 | 缺陷 | **108 条特质 + 4 类伤残硬编码**在 `Definitions`（`StreamingAssets` 无 Trait/Impairment 目录）；13 个革新常量、5×8 阶级权重、5 条精神障碍塞在 `State`，且精神障碍 **JSON 与内建 Def 双源** | "内容在内容层"这条在角色域不成立 |
| 量纲 bug | 缺陷 | `SwarmAI.cs:62` 对 0–1 量纲的 `fertility` **减 2f**（其余写点都 clamp 到 [0,1]）⇒ 一次接触即归零 | 虫群过境后土地永久贫瘠 |

#### 跨域一致性警示：README 与代码相反的三处硬声明

| README 声明 | 代码事实 | 证据 |
|---|---|---|
| 「兵种合成与损耗填补直接消耗物资，无抽象维护费」 | 维护费是抽象 `supply` 0–100；`recruitCost` 写而不读 | `Army.cs:123-137`、F 域 §7 |
| 「建筑：资源消耗/建造时间」 | `materialCost` 写而不读；建造入口零调用 | `BuildingSystem.cs:131,162,250-262` |
| 「税收 9 税种」 | 4 种不进公式（mining/salt 完全不参与，luxury/livestock 只进满意度）；税率无写入者 | `EconomyManager.cs:483-485` |

#### B 域（聚落/行省/人口）补充行——聚落生命周期只有两段活着

| 系统 | 三态 | 事实 | 影响 |
|---|---|---|---|
| 聚落生成 | 在跑 | `AnchorGenerator.cs:40-116` ← `GameWorld.Generation.cs:397` | 每行省 1 主聚落 + 港口/据点/村落 |
| 营地→聚落 | 在跑 | `settlement-evolution.daily` → `SettlementEvolutionSystem.cs:22-37`（人口 ≥50 自动演化） | 唯一"新建聚落"的活路径 |
| 聚落成长（等级/建设度） | **空转** | `UpdateConstructionProgress`/`CanLevelUp`/`PerformLevelUp`（`SettlementTypologySystem.cs:52,221,297`）外部调用 0 | `constructionTier` 恒 1，`settlementLevel` 生成后永不变 ⇒ 村落永不长大 |
| 聚落毁灭 | **空转** | `DestroySettlementCommand` 生产代码 0 派发；AI 8 种意图无此项 | `ruinLevel` 恒 0 ⇒ 恢复 tick 恒早退；屠城/废墟/流民玩法不存在 |
| 土地废弃/重定居 | **空转** | `AbandonTile`/`ResettleTile` 无派发者；`NomadicAbandon` 零调用 | 无"弃地/复耕"；挂名 `settlement-abandonment.daily` 实际只刷土匪 |
| 人口（权威源） | 在跑 | `population.daily` → `PopulationTick`（`GameWorld.Simulation.cs:53-109`）；`TileData.populationBlocks` 入档 | **成长率恒正**（`RaceData.cs:184-191`），无死亡、无迁移规则 |
| 聚落人口（第二套） | **脱钩** | `SettlementData.population` 只在生成/踩踏时变 | 与人口块永不收敛；UI 显示的是这套死数（→ D6 决策：以人口块为权威） |
| 影响力场 | 半跑 | 真 Dijkstra（`SettlementInfluenceSystem.cs:242-255`）；结果被丢弃只留副作用 | 地图叠加每帧全图重算（`MapRenderer.Texture.cs:309-318`），且查询路径**写**模拟状态 |
| 宗主控制 | 半跑 | `settlement-control.daily` → `SettlementControlSystem.cs:23-33` 真推进 `controlProgress` | **无经济后果**：`GetTaxSiphonModifier`/`GetTradeSiphonModifier` 零调用者；控制一个村不产生收益 |
| 城防 | 空转 | `WallDefense.Evaluate` 13 命中全在测试；`CombatManager` 不读它 | 城墙/护城河/材质不影响任何战斗结算 |
| 城市区划 / 经济成分 | 空转 | `CityDistrictSystem.*` 外部 0；`UpdateComposition` 0 | 生成后冻结；区划门槛需 LevelIV 而等级永不变 ⇒ 永不生成 |
| 行省 | 半跑（缺模拟） | `Province.cs:8-16` 仅 4 字段；只有 UI 省名 + 存档 | 无治理/税收/征兵，是纯几何分区 |
| 聚落详情 UI | 在跑 | `SettlementDetailPanel`（11 字段） | "形态/等级"直接打印枚举 int（`:212`）；人口构成/阶层/区划/控制/影响力**界面完全不显示** |

> 存档侧：`settlementLevel` 读档回落 **LevelI**（`SettlementPersistence.cs:269` 只赋 form）；`controllerAnchorId`/`controlProgress`/`influenceTier`/`ruinLevel`/`cityFocus`/`portTier`/`upgradePath` 全无 DTO。

#### A 域（世界地基）补充行——23 个系统：在跑 6 / 半跑 10 / 空转 5 / 不存在 2

| 缺口 | 事实 | 影响 |
|---|---|---|
| 地形编辑破坏性重算 | 每次涂改对该格与邻域**再减一次**海平面阈值（≈0.5，陆→深海）；读档后阈值归 0 不入档 ⇒ 同操作行为不同（`SeaLandGenerator.cs:301-302,228,665-689`；`WorldEditService.cs:199-207`） | 编辑过的世界与未编辑的世界不可比；存档前后不一致 |
| 河网五字段零生产者 | `riverStreamOrder/Width/Depth/Current/Flow` 全工程无写入（只有 SaveSystem 双向拷贝）；`isRiver` 又被 `TerrainGenerator.TrackRivers` 二次全表覆写（`HydrologySystem.cs:308-315`、`GameWorld.Generation.cs:151`） | 河运/渡口/河深判定**恒假**；S8 的"河流可航"是空话 |
| 双气候 + 双群系生产者互写 | GCM（`PlanetTerrainGenerator.cs:209-213`）与半经验模型（`PlanetClimateSimulator.cs:159-188`）交替写 `annualTemp/annualPrecipMm/biome`；每季 tick 全表重写陆地气候与群系（`GameWorld.Simulation.cs:360`） | 谁最后写谁赢；气候"成因驱动"的承诺不成立 |
| 生产开图绕过生成管线 | `GameManager.cs:149` 走 `GenerateTerrain(seed)`，**完全绕过** `GenerationPipeline`/`GenerateTerrainWithConfig`/8 个世界模板/`MapGenerationConfig`(440 行，无 UI 无存档) | 世界生成的可配置性在代码里存在，在游戏里不可达 |
| 编辑器地形工具 | `Assets/Editor/Tools/TerrainPainter` 只有 2 个枚举文件；全工程 `[MenuItem]`/`EditorWindow` **0 命中** | README 宣称的"地图编辑器面板/一键搭场景"已不在代码里 |

#### E 域（文化/信仰/语言/思想）补充行——一条单点断链拖死五条链

| 系统 | 三态 | 事实 | 影响 |
|---|---|---|---|
| 国教（`stateReligionId`） | **P0 单点断链** | 生产代码**从无写入**（唯一写入点 `FaithContracts.cs:280-296` 仅测试调用；`RealmData.cs:24` 默认 −1） | 连带 5 链失效：传教恒早退（`GameWorld.Simulation.cs:234-237`）、热忱只降不升、大圣战不可宣、圣地失陷不可触发、宗教面板恒显"未确立国教" |
| 文化阶段演化 | 在跑 | `CultureSchedule` → `MonthlyCultureStageEvolution`（`:812,854-860`），入档 | 唯一活的文化链 |
| 文化相似/分离/融合/传播 | **空转** | `CultureTransition.cs:14,25,30,41`、`CultureSimilarity.cs:170,177,186`、`CheckRegress:170` 全 0 调用者 | "文明演化"的核心叙事在代码里不存在；唯一改变可观察量的传统消费是 `trad_craft_guild`→Artisan |
| 传统内容 | **内容空洞** | `Traditions.json` **13/13 条 `factors` 是空数组** | 即使接线，`GetFactorSum`/`GetSettlementDegree` 恒 0；`CalculateFactorSimilarity` 因传 `content=null` 恒返回 1.0 |
| 信仰模型 | **文档与代码不一致** | `PLAN_INDEX.md:50-55` 定"传统为主运行单位/宗教层固定/中间层可选"；代码实现的是 **CK3 式 Religion(7)→Succession(22) 两级**，8 个 Tradition 节点无运行时对象，`MiddleLayerDef` grep=0，`ReligionCatalog` 分裂/挂靠 7 个 API 全 0 调用 | 需用户裁决（见 `DECISIONS.md` D8）；总纲暂按代码现状排期 |
| 宗教身份必填 | **不成立** | `RealmData.stateReligionId=-1`、`CultureData.faithId=-1`（唯一文化内容值即 −1）、初始统治者 `faithId` 硬编码 `0`（`CharacterManager.cs:986,991`）而宗教 id 从 100 起 ⇒ **悬空引用** | "宗教身份不可无"的裁决在数据上没实现 |
| 思潮（Thought） | 空转 | `thought.daily` 已注册（`:826-829`）但 4 个容器恒空，`Create*` 0 调用，无 DTO | 5 阶段生命周期有实现无实例 |
| 语言/命名 | 半跑 | 命名生成真在跑（CSV→`LanguageDef`→`ContentQueries.cs:9-49`→`CharacterManager.cs:971`）；但 `PlaceNameGenerator.Generate` 对 Laethis 恒 `""`（`Language.json` 无 `placeSuffixes`/`terrainWords`） | 人名可用、地名不可用 |
| 族群/Ethos | 空转 | `Ethos` 8 条 0 消费者；族群仅 `traditionIds` 生效，`ethosId/languageId/innovationIds` 零读取 | 族群只是标签 |
| ID 命名空间混用 | 缺陷 | 传教把 `ownerRealmId` 当 `cultureId` 写入人口块（`:251` vs `MissionarySystem.cs:38,70`）；初始统治者把 `realmId` 当 `cultureId` | 人口文化分组数据污染；`GetCultureShare` 又 0 调用 ⇒ 无人发现 |

#### C 域（角色/家族/遗传）补充行——"人"是这座工程里最空的一层

| 系统 | 三态 | 事实 | 影响 |
|---|---|---|---|
| 角色日结 | 在跑 | `characters.daily` → `CharacterManager.cs:1001-1023`（年龄/死亡/生育/饮食/心理/好感漂移） | 唯一每 tick 都跑的角色总闸 |
| 婚姻 | **空转** | `Marry` 全仓 1 命中（=定义） | 无配偶 ⇒ `spouseId` 恒 −1 ⇒ 纽带/关系两条链全断 |
| 生育 | 半跑（仅非婚路径） | `AutoProcreate` 第一段要求 `spouseId>=0`（`CharacterManager.cs:465`）永不成立，只剩同政权随机 0.0004/日 | 孩子会出生，但没有家庭结构 |
| 继承 | **半跑（新局不触发）** | `monarchId` 写入点只有 `SaveSystem.cs:590`（读档回填）与 `SuccessionSystem.cs:53,156`（继承成立时）；**开局从不赋值** ⇒ `GetSupremeRulerId()` 返回 −1 ⇒ `ExecuteSuccession` 早退（Lead 亲验 grep + `RealmData.cs:122-133`） | **未读档的局里，统治者死了不会有人继位**；测试靠手工 `_realm.monarchId=100` 掩盖 |
| 遗传（基因组） | 在跑 | `GeneticsEngine.Reproduce`（减数分裂/交换/突变/近交），`GenomeInstance` 入档 | 唯一"有结果"的遗传部分 |
| 表型/外观 | **空转** | `RefreshAppearance` 零调用 ⇒ `ComputePhenotype` 从不执行 | 外观恒默认值；不入档 |
| 身体部位 | **空转（只建表）** | 每角色建 12+ 部件并**入档**，但 `BodyPartSystem.DailyUpdate` 零调用 | 无伤残/失明/失聪后果 |
| 个体疾病/伤残 | **空转** | `new CharacterDiseaseSystem` 0 命中；990 行分期/伤残/战斗受伤从未运行 | 群体疫病（`DiseaseSystem`）是活的，个体病程是死的 |
| 心理/精神健康 | 在跑（无 UI） | `CharacterManager.cs:620-672` 写、`CharacterData.cs:321-331` 读、入档 | 玩家看不到；`dread` 无写入源 ⇒ 偏执分支恒不触发 |
| 特质 | 半跑 | 108 条特质表里只有 48 条（8 轴×2 极×3 级）可达 | `ApplyDailyEffect`/`OnAcquired` 基类空实现 |
| 人际关系/纽带 | **空转** | 唯一写入源 `Marry`→`LinkSpouses`；`opinion/trust/fear/history` **只写不读** | 不影响任何可观察结果 |
| 家族传统 | 空转 | `AddFamilyTradition` 零调用；字段 `[NonSerialized]` 不入档 | JSON 9 条 + 互斥/革新校验全在，无生产者无消费者 |
| 威望/恶名 | **空转（只降不升）** | `ModifyPrestige`/`ModifyNotoriety` 各 1 命中（=定义）；每 tick −0.05 且出生默认 0 | 全角色恒 0 ⇒ 摄政/共和打分退化为比年龄 |
| 角色/家族面板 | **不存在（不可达）** | `characterPanel` 在 `Main.unity:7763` 序列化为 `{fileID: 0}`，`CharacterPanel` 物体 `m_IsActive: 0`；家族树按钮同样为 0 | 角色信息只能从"聚落人物"列表看到姓名/角色/标签 |

> **跨域裁决**：D 域把"继承+摄政"判为"在跑"，C 域发现新局 `monarchId=-1`。**以 C 为准**（Lead 亲验 grep 与 `RealmData.cs`）。D 域该项应读作"状态机完整，但新局入口未接线"。

#### G 域（军事/外交）补充行——S8 实际上"不存在"，只剩 AI 宣战这一个动作

| 系统 | 三态 | 事实 | 影响 |
|---|---|---|---|
| 军队实体 | **空转** | `CreateArmy` 生产调用者 0（`:1167` 唯一定义）；`armies` 字典恒空 | 全军事域失去存在基础 |
| 行军 | **空转** | `Army.DailyTick` 调用者 **0**；`SetMoveTarget` 生产调用 0 | 行军代码永不可达 |
| 战斗结算 | **空转** | `CombatManager` 每日跑（经 `WarfareReligionSchedule.cs:53`），但 `:193` 遍历空 `armies` | 零场战斗；`ResolveBattle` 不可达 |
| 战争终局 | **永不结束** | 白和条件 `day - startDay >= peaceMinYears*365` = **730**，而 `currentDay` 上限 **365**（`GameWorld.Simulation.cs:350-355`） | 一旦宣战永久 `isAtWar`（AI 会宣战） |
| 和约/议和 | **空转** | `PeaceNegotiationSystem.` 非测试命中 0；`OfferPeace` 无调用者 | 26 种条款类型只有定义 |
| 战争法理 | **空转** | 5 个调用点全在死路径 | 15 CB + 15 战争目标无消费者 |
| 不平等从属 | **空转** | `EstablishSubordination` 非测试命中 1（=定义） | `_subordinations` 恒空 |
| 劫掠 | 半跑（冷启动死锁） | 门槛要求 `IsHostile && !isAtWar`，而 `hostilityLevel` 只由劫掠/边境摩擦抬高 | 互为先决，永不触发 |
| 海军 | **空转 + 写侧缺失** | `CreateNavy` 生产 0；`NavyPersistence.CaptureNavies` 0 命中；`ToSaveData` **从不给 `data.navies` 赋值** | SaveVersion 17 只做了"读"的一半 |
| 占领 | **不存在** | `OccupyTile` 非测试命中 1（=定义）；其余全是置 −1 | 有字段、有渲染、无占领行为 |
| 亲兵 / 人力 | **空转** | `new RetinueSystem` 0；`ManpowerSystem.GetRealmTotalManpower` 0 调用 | 法律→人力→补员中间段整段断 |
| 军费动作 | 半跑 | AI 唯一军事动作 `MilitaryBuildUpCommand`：只 `treasury -= 50`，**不产出任何单位** | 军事 AI 的净效果是烧钱 |
| 军队视图 | 半跑 | `MapUnitRegistry.RegisterArmy` 有 2 处生产调用，但**渲染层无消费者**（`showArmyMarkers` 默认 false 且无绘制代码） | 军队即使存在也不上图 |
| 外交关系 | 半跑 | `relation` 有真实消费者（AI 结盟/送礼/选目标 + 地图着色） | **不入档** ⇒ 读档后 `_relations` 重新随机生成，与已入档的 `wars` 互相矛盾；`trust` 唯一读取点在死分支、`threat` 只写不读、`eventHistory` 无人读 |
| 盟约/从属/条款 | 半跑 | 代码实测 **5 种盟约 / 5 种从属 / 26 种条款**（文档声称 8/8/12，代码与文档都不支持 8/8/12） | 盟约可签可清退但读档即丢；从属与条款全链零调用 |
| `WarRules` 计分 | 空转 | 8 个计分字段无读取者 | 战争分数无从产生 |

> 注 1：`reports/innovation-audit.md` §12.2 第 8 条（"法律革新要求永不生效"）**已过期**——该项 2026-11-09 修复，Lead 亲验 `LawContracts.cs:229,258` 传参不再是 `null`。引用旧审计必须对时点。
> 注 2：`CreateArmy` 零产品调用者是本轮最重的一条结构性发现：它让 S8 整域失去存在基础，直接影响 M7 的排序与工作量估计。

---

## 4. 可玩性主干（玩家能感知到的部分）

### 4.1 玩家动词表

> 总判断：**29 项调度中 22 项没有任何界面可触发或观察**（H 域）。玩家只在 7 项上有动作或视野，其余由 AI 代玩——这不是"没做完"，是"玩家位没定义"。
> 更硬的一条：**游戏里没有"玩家"这个概念**。`GameWorld.PlayerRealmId` 全仓从未赋值（只有 Obsolete 的 `GameSaveSystem` 存/读，活存档无字段），`GovernmentPanel` 用 `targetRealmId=0` 兜底到 `realms[0]`（`GovernmentPanel.cs:246-256`）。因此"我的国家"目前靠"列表第一个政权"冒充。

| 动词 | 层级 | 入口 | 后端 | 状态 |
|---|---|---|---|---|
| 开新档 / 读档 / 调速 | 共通 | `MainMenuUI`（默认世界 / 自定义 / 读档）+ F9 | `IGameSession` | 在跑；**F9 读档失败会软锁**（只 `ChangeState(MainMenu)` 不 `ReturnToMainMenu`，`GameManager.cs:315-319`） |
| 存档 | 共通 | **只有 F5**；`SaveLoadPanel` 只能读+删，**没有存档按钮** | `IGameSession.SaveGame` | **半跑**（快捷键可，UI 不可） |
| 地图浏览（12 种模式） | 共通 | **无入口**：`mapMode*` 三件套在 `Main.unity:7757-7760` 全为 `{fileID:0}`，模式列表子节点为空 | `IWorldReadModel` | **死**：实际恒为 Religion 模式（8,0） |
| 点击地块 / 看地块详情 | 共通 | `HandleMouseClick` **空实现**（`UIManager.cs:81-84`），`_selectedTile` 只有测试能写 | — | **不存在**：地块详情恒 "-"，`viewRealmButton` 恒隐藏 ⇒ 政权总览/区划下钻整链不可达 |
| 看聚落详情 | 共通 | 地图图标左键（`GameManager.cs:438-442` → `SettlementDetailPanel`） | `ISettlementDetailUi` | 在跑（字段以 B 域为准；"形态/等级"打印枚举 int） |
| 地形涂改 | 世界 | **双入口皆空**：`RuntimeMapEditingController` 场景无实例；`IMapEditController` 全工程 0 实现 | `IMapEditCommand` / `WorldEditService` | **不存在**（12 个编辑命令 0 可达） |
| 天灾/事件注入 | 世界 | — | `DisasterSystem` / `EventSchedule` | **无入口** |
| 播种/迁徙 | 世界 | — | `SandboxSpawnSystem`（月 tick） | 在跑但不可控 |
| 看编年史 | 世界 | 事件日志（`UIManager.Feedback`） | `Chronicle`（500 条、major、按类型检索） | **部分**：可显示，**不入活存档** |
| 推行法律 / 颁布法令 | 政权 | `GovernmentPanel` 4 个写命令 | `ILawQuery` / `ILawCommand` / `IDecreeCommand` | 在跑（**唯一完整链**）；但 `_viewRealmId` 无赋值点 ⇒ 操作对象恒为 realm 0 |
| 收税 / 调税率 | 政权 | — | — | **无入口** |
| 宣战 / 调兵 / 议和 | 政权 | — | — | **无入口**（仅地图着色 `UIManager.cs:496`） |
| 研究 / 采纳革新 | 政权 | — | — | **无入口** |
| 任命官职 / 处置头衔 | 政权 | `GovernmentPanel`（部分） | `ILawCommand` 之外 | **部分** |
| 看角色 / 家族 | 个人/家族 | `characterPanel:7763`、`familyTreeOpenButton:7794` 均 `{fileID:0}` | `CharacterDetailQueryAdapter` | **面板不可达** |
| 婚配 / 生育 / 教养 | 个人/家族 | — | `Marry` 零调用 | **无入口** |
| 设置 | 共通 | `SettingsPanel`（12 项确实落到引擎） | `ISettingsService` | 在跑（但契约在 UI 侧零消费，见 K 域 §8） |

### 4.2 主循环（`GameWorld.Simulation.cs:792-852`，**29 个注册项** = 27 Daily + 2 Monthly）

驱动链：`GameWorld.Update → GameTick → TickBudgeted`（预算 4–12 ms 浮动，超预算的项进 `_pending` 队列延到下一帧）。

**调度语义（H 域审计澄清，与直觉不同，务必按此读代码）**：

- `order` **不是优先级**，是排序主键桶；同 `order` 内按 `id` 字典序（`SimulationScheduler.cs:136-140`）。
- `cadence` 由 `day % intervalDays == 0` 决定：`Monthly=30 / Seasonal=90 / LongTerm=365`，**"Monthly" = 每 30 个模拟日，不是日历月 1 号**（`:131,174-184`）。
- `SimulationCadence` **没有 Yearly 枚举**；`Weekly/Seasonal/LongTerm` 当前零使用。任何"每年"逻辑必须注册成 Daily 并自带年份 guard（`laws.effects.yearly` 就是这么做的，`:806-810`）。
- 延迟队列（`_pending`）在下一帧会**跳过 cadence 与 dirty 校验**直接执行（`:124-128`）。
- 调度层本身不并行（`lane` 恒 `Both`）；`PopulationTick` 内部用 `Parallel.For`，但线程 RNG 以 `Environment.TickCount` 播种 ⇒ **同 seed 不可复现**（`audit/H-innovation-spine.md` §3）。

**命名的坑**：`Register(...)` 只证明"接上了调度"，不证明"有活干"。`justice.daily` 就是典型：注册了、每日跑、但内部字典恒空（见 §3 矩阵）。判定"是否空转"必须看被调方法内部**是否有非空数据源**。

另有 **1 项用 `RegisterDirty` 注册**：`world.map.recalculate-dirty`（`WorldMapSchedule.cs:22`，dirtyKey=`world.terrain`）——它**只在涂改地形后才跑**，不是每日全量。所以 29 项 = 28 个 `Register` + 1 个 `RegisterDirty`。

---

## 4.3 去数值化清单（宪章原则 5 的施工对象）

> 判据三问：①有物质载体？②经过既有环节传导？③删掉会断链？——三问不过的"驱动力"，一律按"换个机制"处理，不许留在代码里当历史原因。
> 这张表不是全部，而是**已定位的典型**；每个里程碑交付前按同一判据审一遍。

| # | 现存做法 | 问题 | 换成什么 | 归属 |
|---|---|---|---|---|
| 1 | `ApplyPathBonuses`：5 条建国路径各给一次性硬编码（`stability=55`/`centralization=0.7`/`treasury=2000`/`stability=60`/`centralization=0.3`） | 建国方式只体现为开局数值差，且是 C# 分支（不可模组） | 路径应体现为**制度组合 + 禀赋**：游牧征服 → 军事贵族/牧业剩余/低农业税基；贸易控制 → 关税依赖/城市自治/薄弱农业；宗教统一 → 祭司阶层/捐献经济/高合法性低征收 | M4/M7 |
| 2 | 地块稳定度 `Lerp(tile.stability, 50 + centralization*20, 0.01*deltaDays)` | 纯回归：稳定度没有原因，只是被拉向一个公式值 | 由**可指认事件**驱动：税负超阈值、饥荒、战败、征发过重、法令合规度不足；恢复靠时间与减负 | M4 |
| 3 | 法律 `taxMultiplier` / `stabilityPerYear` 乘子 | 把制度变成"先进度加成"（辉格式） | 改为**摩擦与成本**：征收成本（距离/地形/官僚数）、合规度（法令传播已有五档）、执行延迟；乘子只作 UI 展示 | M5 |
| 4 | `RaceData` 六个平铺常数（`GameWorld.Core.cs:850-861` 全填 1f） | 种族是"全局加成表"，无载体 | 改为**生理率**：寿命分布、生育间隔、疾病易感、劳动能力——由基因表达推出，且只在相应环节生效 | M8 |
| 5 | 聚落 `development/wealth` 只减不增 | 经济结果没有来源 | 改由**生产与贸易流**决定（依赖 M3 的生产链）：产出→市场→税基→财富 | M3 |
| 6 | `carrying capacity` 只压成长率、永不为负 | 只有上限没有崩解 | 超载应产生**可指认后果**：垦殖过度→肥力下降→产出下降→饥荒/迁移 | M3 |
| 7 | `unlockRefs` 的"解锁"如果只写不读 | 技术与社会脱节 | 每条解锁必须落在**具体环节**（配方可制/建筑可建/法律可推/阶层可现），否则删字段 | M6 |

---

## 4.4 架构耦合审计（"改一处要改多处"的清单）

> 用户的准则：**系统要耦合（因果要通），代码与文件架构要解耦（改一处不必连带改多处）**。本节按"五条信号"（宪章原则 8）逐条查违反，并给处置。

| # | 耦合点 | 违反哪条信号 | 证据 | 处置 |
|---|---|---|---|---|
| 1 | `GameWorld` 被拆成 **13 个 partial**（`GameWorld.cs` / `Core` / `Simulation` / `Generation` / `Queries` / `Contracts` / `ReadModel` / `Persistence` / `Editing` / `Succession` / `HostHooks` / `GenerationHooks` / `UiCommands`），按"技术关注点"拆而非按职责拆 | ③ 新增系统不修改既有系统 | 目录实测 13 个文件；`GameWorld.Core.cs` 995 行承载数据容器 + 初始化 + 硬编码内容 | 组合根与数据容器分离：`GameWorld` 只留装配；各域状态迁到各自 owner（分阶段，不做大爆炸） |
| 2 | **内容硬编码在 C#（18 张表）**：建筑 29、单位 14、政体组件门槛 35 组、阶层门槛、涌现映射、商品 57、灾害 12、建筑选址 30、法律→革新映射、绰号 81、经济原型 11、资源-群系排除、加载器注入革新、角色疾病 32、精神障碍 5、宗教 3、地名后缀、法律迁移默认值 | ② 内容改动不改代码 | I 域 §6 逐条；反例：`Titles.json` 100% 内容驱动 | 按 D23 逐张外提，代码只做查表 |
| 3 | **重复模型（双真源）**：两套人口（`TileData.populationBlocks` vs `SettlementData.population`）、两套法律（`LawComposition` vs `RealmLawState`）、两套遗传（legacy `DnaData` vs `GenomeInstance`）、两套存档（`SaveSystem` vs `[Obsolete] GameSaveSystem`）、两套建筑可用性（`BuildingSystem` vs `BuildingAvailabilitySystem` 的 `BuildableType`） | ① 单一真源 | B/D/C/F 域审计逐条带锚点；`GameSaveSystem` 整类 `[Obsolete]` 且零引用 | 逐对合并，先合"活的"（人口/法律），再清"死的"（GameSaveSystem/旧遗传） |
| 4 | **跨层直接写可变字段**：`RealmData`/`TileData`/`SettlementData` 大量 `public` 字段被各系统直接赋值（如 `realm.treasury +=`、`tile.stability = Lerp(...)`、`anchor.influenceTier = ...`） | ⑤ 跨层不直接写对方字段 | `PoliticsSimulationSystem.cs:87`、`:104`、`SettlementInfluenceSystem.cs:131` 等 | 关键不变量（金库/稳定度/控制权）改走命令与查询；不求一次到位，按里程碑逐步收 |
| 5 | **查询路径写模拟状态**：地图渲染每帧调 `MapWorldViewAdapter` → `SettlementInfluenceSystem` 写 `anchor.influenceTier` | ⑤ + 原则 7 性能 | `MapRenderer.Texture.cs:309-318` → `MapWorldViewAdapter.cs:334` → `SettlementInfluenceSystem.cs:131` | 渲染只读；影响力由 tick 算并缓存 |
| 6 | **场景与代码双源**：`Main.unity` 里 `characterPanel`/`familyTreeOpenButton` 序列化为 `{fileID:0}`，而代码里有 `OpenCharacterPanel` | ③/④ | C 域审计 | UI 一律运行时构建（已有 `UIFactory`），场景不持有面板引用 |
| 7 | **静态类互相直呼**：`CultureStageEvolutionHost` / `ClassEmergenceEvents` / `SocialClassAvailability` / `LawRealmHost` 等静态类以 `world` 参数穿透多域 | ④ 替换只改接口引用 | 各文件签名 | 静态 host 逐步收成实例服务（由组合根注入），先做高频改动的几个 |
| 8 | **商品两套来源**：`GameWorld.Core.cs:601-659` 硬编码 57 条商品，而 `GoodsProperties.json` 另有 29 条 | ① 单一真源 | I 域审计 §6 | 合并为 JSON 单源（D23 第 6 条） |
| 9 | **法律↔革新两套命名**：`LawEffects.cs:390-435` 用 `string` 键，`InnovationTree` 用 `int` id | ① 单一真源 | I 域 §6；`reports/innovation-audit.md` 缺口 #8 同源 | 统一为 int id + 别名映射 |
| 10 | **Mods 布局互斥**：`Mods/Dna/DnaDefs.json` 被判为"包目录"，实际去找 `Mods/Dna/Dna/DnaDefs.json` ⇒ 永不加载 | ② 内容改动不改代码 | `ContentDefinitionIndex.cs:250` | 统一一种布局并加"mod 样例可加载"的门禁测试 |
| 11 | **状态无守卫**：`RealmData` **37 个字段全 public**，无 setter/校验；`treasury` **14 处直接写**（跨 8 程序集）；`stability` **3 套夹取**；`population` 被 **6 个系统**各自增删 | ⑤ 不跨层写可变字段 | L 域 §3 | 关键量收成"读查询 + 写命令"，先做 treasury/stability/population 三项 |
| 12 | **金库双源**：`TreasuryLedger.cs:334` 用 vault 覆盖 `treasury` | ① 单一真源 | L 域 §3 | 二选一，另一者降级为派生视图 |
| 13 | **事件无人听**：9 类型 / 11 发布 / 仅 3 类订阅；`SettlementTileResettled/Abandoned/Destroyed` 0 订阅；`SimulationStatisticsRecorder` 全库无实例化；`RegisterEventListener` 无调用者 | ③ 新增系统不改既有系统（事件是解耦通道，但它没接上） | L 域 §4 | 事件要么删，要么接上订阅者（统计/UI/编年史） |

> 结论：**用户"深刻怀疑还是有些没有做全"是对的**——解耦做了程序集层（15 asmdef 无环、UI 硬边界），但**没做内容层与状态层**：内容一半在 JSON 一半在 C#，状态存在五组双真源，关键量还被跨层直接写。这三件事决定了"改一处要改多处"。

---


## 5. 里程碑（垂直切片）

> **方法论修正**：过去两年是**横向铺系统**——二十余个系统同时推进，结果全部停在"半跑"。
> 新计划改为**纵向切片**：每个里程碑交付一个"玩家能感知的完整循环"，宁可只覆盖一个系统，也要三件齐备（tick + 入口 + 存档）+ 可观察结果。
> **承载方式**：凡是"需要时间 + 人 + 物资"的效果（造城、开垦、远航、联姻、密谋、围城、变法、迁徙），一律用 S15 的 `Plan` 承载，而不是瞬时改数（宪章原则 11）。当前 10 类计划只有研究一种在转。

（每个里程碑的缺口清单在 §3 里逐条挂证据；下表是顺序与验收骨架）

### 5.0 工作包（WP）总表 —— 所有子计划必须挂在这里

> 规则（`docs/README.md` §5）：**没有 WP 编号的施工项不许开工**；子计划文件只提供细节，编号以本表为准。

| WP | 名称 | 覆盖系统 | 子计划文件 | 依赖 | 验收（摘要） |
|---|---|---|---|---|---|
| **WP1** | 建造闭环：创建者 → 命名事件 → 好感/编年史 | S15 Plan、S2 聚落、S4 人物、S17 关系、S13 存档、S12 UI | `INTEGRATION_PLAN.md §3`、`CHARACTER_COUPLING.md §1`、`PLAN_COVERAGE.md §2` | WP6 存档字段 | 建造完成后区块名 = 发起者本名；人物在世则有 opinion 增量与编年史条目；读档后归属不丢 |
| **WP2** | 教理 schema + 内容重排 | S18 信仰、Content | `RELIGION_V2.md §3` | — | 教义组→档→参数可加载；缺 `source` 的参数加载失败；46 条挂进 11 域 |
| **WP3** | 教理接线八处 | S18、S6 法律、S7 经济、S4 人物、S16 头衔 | `RELIGION_V2.md §3.2` | WP2、M5 人格效力（**同一件事，合并做**） | 改一条 tenet → 至少一处下游行为改变且有测试 |
| **WP4** | 信息传播（谁知道什么） | S3 人口、S15、S17、S18、性能 | `COVERAGE_GAP_AUDIT.md §2` | M0.5 基准 | 命令延迟可观测；知识按人物/地方/事件三级记录；无 O(N) 全表扫 |
| **WP5** | Intrigue（阴谋，含保密/暴露） | S15、S17、S6、S4 | `PLAN_COVERAGE.md §3`、`CHARACTER_COUPLING.md` | WP4 | 阴谋可失败/可暴露，暴露后走司法与关系，编年史可隐藏真相 |
| **WP6** | 存档对齐 CK3（扁平表 + 定义键串化 + 深度） | S13 全部 | `ARCHITECTURE_UNFINISHED.md §7.5` | — | `Infrastructure` 只依赖 `Core + Save.Contracts`；深层结构不再被截断 |

**并行约束**：WP1 与 WP6 可并行（一个动内容、一个动格式）；WP3 必须等 WP2；WP5 必须等 WP4。

### 5.1 M0.5 历史行为验收台 + 性能基准（宪章验收与性能门禁的共同前提）

| 项 | 内容 |
|---|---|
| 为什么单独立项 | 宪章四条原则**全是跨多年、跨 seed 的行为断言**；性能门禁（D19）没有基准就是空话——706 测试全绿既不能证明"世界会演化"，也不能证明"跑得动"。口径直接对齐 CK3 性能日志：**从固定起点跑 100 年，取每日平均 tick 耗时**（他们用 1066→1166；我们用世界生成完成年起的 100 年）。 |
| 施工 | ① headless 批量：N 个 seed × **100 年**（可扩 300 年）；② **三张表**：**(a) 日耗时曲线**（按年 × 系统 × 调用次数）、**(b) 内存曲线**（每年 RSS + 关键集合计数：人口块/关系/编年史/计划/事件历史）、**(c) 函数级热点表**（每函数调用次数 + 累计耗时 top-20）；③ 指标导出：人口、聚落、国家数、`realmForm×建国路径` 分布、阶层涌现时间线、财政/稳定曲线；④ 断言脚本：宪章四组信号 + 守恒（人口/物产/金库）+ 无 NaN + 预算不超；⑤ 基线快照入库，作为后续每个里程碑的回归基准。 |
| 硬指标（首版，可调） | ① **后期/早期日耗时 ≤ 2.0**（CK3 实测 1.5，我们首版放宽到 2）；② **单日 P99 ≤ 16.7ms**；③ **内存第 10 年 / 第 1 年 ≤ 1.5**，且关键集合计数不得单调增（CK3 经验：内容启动载入、第 5 年起平稳）；④ 启动到可玩 ≤ 30s（对照 CK3 84s→16s）。 |
| 函数级热点规则 | 输出 **top-20 by 累计耗时** 与 **top-20 by 调用次数**；与上次基线比，**任一函数调用次数增长 >10× 必须解释或修复**（CK3 教训：某函数从 1500 次/月变 85 万次/月，diff 里看不出来）。 |
| DoD | 一条命令跑完 N×100 年并输出三张表 + 与基线 diff；四组宪章信号各有 1 条可执行断言（**可失败，但必须存在**）；同 seed 同哈希（依赖 M1 确定性）；**任一里程碑合并前必须附三表 diff，看不到增量不许进主干**。 |
| 依赖 | 需要 M1 的确定性快照导出；可与 M1 并行开工 |

### M1 地基：确定性 + 存档闭合 + 观测面（S13）

| 项 | 内容 |
|---|---|
| 现状 | 工程门禁全绿（EditMode 706/706 实测）；SaveVersion 17。但：`PopulationTick` 的 `Parallel.For` 用 `Environment.TickCount` 播种 RNG ⇒ 同 seed 不可复现；计划/AI/调度器/mastery/practice **连 DTO 都没有**；在途商队与在建工程不入档（读档后资产"凭空减少"）；`CultureStageEvolutionHost.cs:88` 建国直接 `treasury=2000` |
| 施工 | ① 全部 RNG 改成 seed 派生（`ThreadRng` 由 world seed + tick 派生）；② 补 5 类 DTO + 迁移；③ **`PlayerRealmId` 赋值并进活存档**（大战略的"我是谁"）；④ **`Chronicle` 进活存档**；⑤ **外交全表入档**（`relations/alliances/treaties/casusBelli/subordinations` 零 DTO，读档即随机重置，与已入档 `wars` 矛盾）；⑥ **头衔实例与 `primaryTitleKey` 从 Obsolete 的 `GameSaveSystem` 迁入 `SaveSystem`**；⑦ **`settlementLevel` 改 DTO 路径同步**（现只在 Unity 序列化回调里同步 ⇒ 读档回落 LevelI）；⑧ **`primaryCultureId`/`heirId`/`realmForm`/`nomadicSubtype` 入档**；⑨ 加"世界快照导出"（Debug/CLI）作为回归基线；⑩ SaveVersion → 18（**需单独批准**）+ 迁移测试 |
| DoD | 同 seed 跑 1000 天两次，快照哈希相等；每个域有往返测试；读档前后"物产总量/金库/人口"守恒断言通过；**读档后玩家政权与编年史都不丢**；快照导出可被 CI 比对 |

### M2 世界与时间（S1）

| 项 | 内容 |
|---|---|
| 现状 | 生成链（地形→气候→水文→群系→资源）在主循环与生成期都在跑；`WorldMapSchedule` 每日 tick；六种地图显示模式存在。但：**地形编辑是破坏性的**（每次涂改再减一次海平面阈值，且阈值不入档）；**河网 5 个字段零生产者**而 3 处消费者在用它；**双气候/双群系生产者互写**（GCM 与半经验模型），"成因驱动"只在生成那一刻成立；开图绕过生成管线与 8 个世界模板 |
| 施工 | ① 修地形编辑的幂等性（重算用真值而非缓存阈值）+ 阈值入档；② 河网字段补生产者（或删掉 3 处消费者）；③ 气候/群系**单一生产者**，另一套降级为校验；④ 开图走 `GenerationPipeline`/世界模板，让 `MapGenerationConfig` 真正可达（含 UI 与存档）；⑤ 六显示模式统一走读模型；⑥ **世界尺度干预的低端子集**：地形/河道/降灾三类接到 UI（复用 `WorldEditService` 与 `DisasterSystem`），遵守"只改条件、按时间尺度、记入编年史"（宪章原则 10） |
| DoD | 涂改地块 → 存档 → 读档 → 再涂改，结果与未存档路径一致；河运/渡口判定有真有假（能举出可航与不可航两组）；同一地块的气候/群系在生成期与运行期由同一函数产出；世界模板可选且有测试；**玩家能改地形/引灾害，且改变只经由既有因果链影响结果，并留下编年史条目** |

### M3 生产-人口-聚落闭环（S3 + S7a + S2）——第一个"世界活着"的切片

| 项 | 内容 |
|---|---|
| 现状 | 人口日循环在跑（增长×承载力×满意度→饥荒/叛乱）；但**生产链不存在**：`inventory` 只有开局播种与商队到货两个写入者 ⇒ 粮食从 1000 单调归零，饥荒常态化（F 域 P0）；配方执行器 `TryCraft` 只被测试调用；贸易路线生产期从不创建；**两套人口不收敛**（地块人口块 vs `SettlementData.population`，且聚落等级永不推进） |
| 施工 | ① **人口单源**（D6）：地块人口块为权威，聚落人口改为派生聚合；② **补种族层（群体基线）**：`RaceDefs.json` 从 0 条到可用（≥3 个种族，差异体现在等位频率/基因组 id/三项基准/寿命/环境耐受），带动"同一地理、不同种族 → 不同人口与扩张"；`productionModifiers/consumptionModifiers` 去 `[NonSerialized]`；③ 地块资源 → 产能 → 市场库存（把 `SettlementProduction` 接到 daily tick）；④ 配方消耗/产出闭环 + 保质期真实生效；⑤ 贸易路线生成（基于供需差）；⑥ 聚落成长/废弃接人口与产能阈值（同时补 `settlementLevel` 入档与读档回填） |
| DoD | 无玩家干预跑 100 年：食物库存不单调递减；富饶区与贫瘠区人口/聚落数量出现可量化分化；聚落等级能升也能降；饥荒只在灾年出现；**UI 显示的人口 = 人口块聚合值**；**换一个种族（参数不同）→ 同地理下的人口/扩张结果显著不同**（原则 2 信号 2）；存档往返后趋势不变 |

### M4 财政-阶层-稳定（S5 + S7b）

| 项 | 内容 |
|---|---|
| 现状 | 税收每日入 `realm.treasury`（法律乘子已接）、稳定度向集权度回归、阶层关系/派系/政体变迁都在跑；但税基=`fertility×development×10`（无人口/控制度）、税率无任何写入者、4/9 税种不进公式、玩家无收支表 |
| 施工 | ① 税基接人口与控制度；② 税率成为可写状态（玩家/AI 都能改）；③ 收支表 UI；④ 破产→加税→阶层不满→叛乱的因果可复现；⑤ **四轴需求报告**（D25）：`INeedQuery.GetReports(axis)` 按 阶级/阶层/身份/职业 各出一份，归桶时位标志按位展开且人口按位均分；政权 unrest 由四轴不满度按覆盖人口与轴权重合成；`ClassNeedWeightTable` 扩成按轴取权重；⑥ 派系读取改为"该派系所代表轴"的报告 |
| DoD | 收支表与金库数字一致；100 年不出现负库存/负金库；人为把税率拉高能复现叛乱；法律税收乘子在收支表可见；**四轴需求报告各一组、四轴人口合计 = 人口块总量（守恒断言）、改轴权重 → unrest 变化可复现** |

### M5 玩家干预：法律 / 法令 / 官职（S6）

| 项 | 内容 |
|---|---|
| 现状 | **唯一深交互系统**：已知→提案→pending→固化；法令颁布/冷却/到期/撤销；逐级下达 + 合规度五档；年度效果结算；`GovernmentPanel` 可操作；革新门槛 2026-11-09 已接通 |
| 施工 | ① **双源合一**：`GovernmentPanel` 推行的法律必须回写 `LawComposition`；② 每条法律至少挂一个可观察效果，并修通 **27 法的人力乘子**；③ **八态生命周期**：补 `Consolidated`（连续生效 N 年 + 合规）与 `Fundamentalized`（革新 + 制宪主体 + 合法性阈值）的写入方；④ **两座桥**（`LAW_LIFECYCLE.md` §3）：习惯→法（`TraditionDef.solidification` + 位阶 + 立法能力）与法→习惯（连续生效 + 合规 → 写回传统，撤销后习惯仍在）；⑤ **失效方式补全**：replaced / suspended / 纸面法（合规度低 → 效果按比例衰减）；⑥ 层级与冲突：`precedence/overrides/normKind` 接消费者；⑦ **人格效力**接身份轴（贵族/教士/外国人豁免或另适用）；⑧ **地域效力**接 `LegalScope`（地方习惯/特许状）；⑨ 修 `GovernmentPanel.Refresh` 的模拟副作用；⑩ 官职三套合一套 + 补缺改每日 + `ActionIds.AppointTitle`；⑪ 头衔定义层补齐、位阶改组合序；⑫ 修 UI 传参与 `titleId` 原串打印 |
| DoD | 改一条法 → 至少两个下游量变化（如区划层数 + 集权度）且可在 UI/日志观察；不存在"承诺未兑现"的 schema 字段；法令合规度可视；`decreeState.active` 有到期清理；面板刷新不改变模拟状态 |

### M6 革新-解锁（S10）

| 项 | 内容 |
|---|---|
| 现状 | AI 选题 + 每日研究点在跑；186 条内容前置图干净；但 mastery 恒 0 ⇒ 卡死 167/186；`unlockRefs` 120 条零消费者；玩家入口 0；研究计划/熟练不入档 |
| 施工 | ① **先修 mastery 链**（`Learn`/`SetMastery` 接上练习与突破）——它是 M3"文明自己长成"实验的前置，否则 167/186 条革新不可达、阶层涌现被锁死在 19 条根革新（宪章原则 3/4）；② 玩家革新面板（选课题/看树/看进度）；③ `unlockRefs` 至少接 3 类消费者（配方/建筑/法律）；④ 研究计划与熟练入档（依赖 M1） |
| DoD | 玩家能选课题并看到进度；一条革新完成后对应配方/建筑/法律真的解锁；mastery 在 UI 可见且会涨；读档后进度不丢；**随机抽 10 条革新，每条至少有 1 个下游可观察变化**（宪章原则 4 的信号） |

### M7 战争与外交（S8 + S9）

| 项 | 内容 |
|---|---|
| 现状 | **军队实体零生成**（`CreateArmy`/`CreateNavy` 零产品调用者）；`Army.DailyTick` 零调用 ⇒ 行军不可达；`CombatManager` 每日跑但遍历空 `armies` ⇒ 零战斗；AI 会宣战，而白和条件 730 天 > 天数上限 365 ⇒ **宣战后永不结束**；和约/法理/从属/占领/亲兵/人力全部空转；海军"读侧有、写侧无"；军队视图无渲染器 |
| 施工 | ① 征召（人力+物资+财政）→ 军队实体 → 编队；② 行军（寻路/移动/损耗）与补给；③ 接战与伤亡；④ 和约与疆域变更（含占领 `OccupyTile` 接上）；⑤ 修战争终局条件（白和/投降）；⑥ 玩家宣战/调兵/议和入口；⑦ 修海军写侧 + 军队渲染；⑧ **接通建国路径**：`CultureStageEvolutionHost.FormRealmFromActor` 目前零调用者（宪章原则 1），需给它一个真实触发源（游群/部族人口达阈值 + 条件判定），并让 5 条路径各自产生**后续约束**而非一次性加成 |
| DoD | 一场战争端到端可复现（AI vs AI 与玩家 vs AI 各一条）；战后疆域真实变更且 `occupyingRealmId` 有生产者；战争有明确终局（不等于天数上限）；军队入档后与地图单位一致且能上图；**300 年模拟中出现 ≥3 种 `realmForm × 建国路径` 组合**（宪章原则 1 的信号） |

### M8 角色与继承（S4 + S5）

| 项 | 内容 |
|---|---|
| 现状 | 角色日结/遗传/心理在跑；但：**新局 `monarchId` 从不赋值** ⇒ 统治者死后无人继位（C 域 P0，Lead 亲验）；`Marry` 零调用 ⇒ 无配偶/纽带/关系，人际与家族传统全断；`RefreshAppearance`/`BodyPartSystem.DailyUpdate`/`CharacterDiseaseSystem` 全零调用；威望/恶名只降不升（恒 0）；角色与家族面板在 `Main.unity` 里序列化为 null（玩家不可达）；读档后 `dna` 置 null 会改变衰老曲线、genome 被重随机 |
| 施工 | ① 开局给 `monarchId`/`consulId` 赋值（单点修复，救活继承/摄政/谥号）；② 接 `Marry` 与关系读写（让 opinion/trust 进入判定）；③ **让个体构成真正生效**（S14）：人物属性经 `GetEffectiveStats` 进入选将/立法门禁/研究/行军，并把种族生理经基因表达落到人物（寿命/抗病/体格/认知）；④ **头衔入活档**：`titleInstances`+`primaryTitleKey` 迁入 `SaveSystem`，并在活路径补 `TitleInstanceRegistry.Clear()`（现在只在死路径，读档残留上一局持有者）；⑤ **继承三模式做成真分支**：`definitionId` 不再恒绑 `Highest(Landed)`，`None/OwnLaw/RealmLaw` 各自可走、`ownLawPreset` 要有消费者；⑥ **称号按头衔决定**（补 `basileus` 等分支，不再一律"王"）；⑦ 补 `Titles.json` 的 `tier`/`superiorTitle`，`LoadFromContent` 从 7/36 补到可用（现 29/36 字段无生产者）；⑧ 修读档漂移（dna/genome/ageing）；⑨ 角色面板与家族树接线；⑩ 威望/恶名补生产端 |
| DoD | 不读档跑 100 年：继承链完整（含幼主/摄政/无嗣）；婚姻与子嗣可观察；角色属性改变决策（选将/派系/研究）；读档前后同一角色等位基因与衰老曲线一致；角色面板可用；**RPG 验收：一个角色 40 年生命周期全程可玩，属性影响 ≥3 个真实决策**（宪章 §2 末） |

### M9 文化 / 信仰 / 语言（S11）

| 项 | 内容 |
|---|---|
| 现状 | 待 E 域结论；**已知**：`stateReligionId` 生产代码无写入 ⇒ 传教/热忱/大圣战/圣地/宗教面板五链同时失效；文化只有"阶段演化"一条活链（相似/分离/融合/传播全 0 调用）；`Traditions.json` 13/13 条 `factors` 为空；思潮 4 个容器恒空；代码实现是 CK3 式两极而非文档要求的"传统为主"（见 `DECISIONS.md` D8） |
| 施工 | ① 启动链第一件事：给 `stateReligionId` 一个写入点（建国/改宗），单点修复即可复活 5 条链；② 文化相似/分离/融合接上真实消费者（先做"有阻隔才分离"的最小规则）；③ 补 `Traditions.json` 的 `factors`；④ 按 D8 裁决结果决定信仰层改造范围；⑤ 修 `realmId↔cultureId` 混用 |
| DoD | 200–500 年参数实验三组全部符合预期（无阻隔不分离 / 有阻隔+生计变化分离 / 通婚收敛）；宗教身份不可为空；传教与热忱有可观察效果；传统因子非空并影响至少一个可观察量 |

### M10 表现与规模化（S12）

| 项 | 内容 |
|---|---|
| 现状 | 6 面板 / 22 项调度无观测面 / 本地化仅 en+zh 135 条 / de,es,fr,ja,ko 为 0 |
| 施工 | 按"动词表"补面板（税率、市场、革新、军事、外交、角色、计划）；本地化补齐或裁剪；性能预算；**世界尺度干预收尾**：地形/河道/降灾/迁人/事件注入面板 + 编年史检索 + 四层视角切换（只换命令面与观测面，不换世界）；**计划面板**：把 `PlanSystem` 的 10 类计划列成"在办事项"（谁在做/要什么/进度/何时可能失败），玩家在其中下达与取消计划 |
| DoD | 玩家动词表 11 项全部有入口；1000 模拟年在预算内跑完（给出 ms/年）；无空语言文件；**同一世界同一存档下，可在个人/家族/政权/世界四层视角间自由切换**；powers 只能改条件、改动按时间尺度（地形/气候属长时段、灾害属短时段）、且每次干预记入编年史；**计划面板能显示至少 3 类计划（研究/建设/军事）并允许取消** |

---

## 6. 依赖 DAG

> 用途：①说明里程碑为什么是这个顺序；②防止"并行开工但下游空转"——这是本项目最贵的教训（革新 `unlockRefs` 84 条无消费者、法律 `normKind` 无读取点，都是上游做了、下游没做）。

```text
[S13 时间/调度/存档/门禁]  ← 一切的地基（M1）
        │
        ▼
[S1 世界：地形→气候→水文→群系→资源]        （M2）
        │
        ├──────────────► [S2 空间：锚点/聚落/行省/影响力/控制]
        │                          │
        ▼                          ▼
[S7a 生产：物产/配方/承载力]  ──► [S3 人口：人口块/阶层/迁移/饥荒]   （M3）
        │                          │
        └──────────┬───────────────┘
                   ▼
        [S7b 财政：税/货币/市场/建筑] ──► [S5 政权：形态/官职/继承/行政区划] （M4）
                   │                          │
                   │                          ▼
                   │                   [S6 法律/法令/司法]                （M5）
                   ▼                          │
        [S10 革新：研究/熟练/解锁] ◄──────────┘                          （M6）
                   │
                   ▼
        [S8 军队/战争] ◄── [S4 角色/家族] ──► [S5 继承/头衔]
                   │                                     （M7 M8）
                   ▼
        [S9 外交/条约/关系]  →  [S11 文化/信仰/语言/思潮]              （M7 M9）
                   │
                   ▼
        [S12 表现：UI/渲染/本地化/音频]                                 （M10）
```

**关键阻塞关系（不做上游就白做）**：

1. **没有确定性 + 存档闭合（M1）**，任何"半跑"判定都不可复现，等于没有验收。
2. **没有人口-食物闭环（M3）**，财政（M4）收的是空数，法律（M5）改的是空数，战争（M7）打的是空数。
3. **没有玩家入口（每个切片的 UI/命令）**，系统再完整也只是"AI 自己玩"，玩家感知为 0。
4. **革新（M6）必须晚于生产与法律**：它的输出是"解锁配方/建筑/法律门槛"，上游不存在时 `unlockRefs` 必然无消费者——这正是现状。
5. **角色（M8）不能早于法律与政权**：行动门禁（ActionGate）与继承都依赖法律与官职先有稳定结构。

---

## 7. 完成定义（DoD）与门禁

**继承** `docs/architecture/UNIFIED_MASTER_PLAN.md` §2 的工程门禁（编译 / EditMode 全绿 / PlayMode×2 全绿 / 五道静态门禁 / SaveVersion 审批 / 文档同步），**补充"可玩性 DoD"四条**——本项目过去只跑工程门禁，导致"测试全绿但玩家摸不到"：

1. **三件齐备**：该功能有 tick（或在真实调用链上）＋ 有玩家入口（UI/命令）＋ 状态入档（往返测试）。
2. **可观察变化**：该功能必须改变至少一个玩家可见量（人口/财政/疆域/技术/稳定/事件日志），并在验收脚本里断言这个量随输入变化。
3. **确定性**：同 seed + 同输入，重复模拟 N 天结果一致（快照哈希相等）。
4. **无死字段**：新增的 schema 字段必须有生产者与消费者，否则不得入库（法律 `LegalNormStage` / `fundamentalizedLawIds` 就是反例）。

**每片验收脚本要求**：一条 EditMode 测试从**真实入口**驱动（不是 `new` 对象直构），断言"输入→可观察量"；一条存档往返；一条确定性回归。

**横向门禁（宪章验收）**：每个里程碑除工程门禁外，必须至少满足**其所承载原则的一条可断言信号**（见 `DESIGN_CHARTER.md` §2–§3）。给不出信号的"完成"不成立。

**性能门禁（D19 + CK3 日志口径）**：每个里程碑合并前必须附 M0.5 的**三表 diff**——① 日耗时曲线（后期/早期 ≤2.0）、② 内存曲线（第 10 年/第 1 年 ≤1.5，关键集合计数不单调增）、③ 函数级热点（调用次数增长 >10× 必须解释）。看不到增量不许进主干；新增交互必须证明"每个 AI 每月只多算可枚举的常量次"，不做角色×交互的笛卡尔积。

**重构例外（宪章 §7）**：法律 / 信仰宗教 / 社会四维 + 需求 三块在重构完成前**不计入完成度统计**，里程碑对它们只验两件事——① 接口契约稳定（别人能读能命令）；② 输出有下游。其内部"半跑/空转"标注是快照，不作为排期依据。

| 原则 | 里程碑 | 可断言信号（摘要） |
|---|---|---|
| 1 多路径 | M3/M4/M7 | 300 年模拟**自然涌现**出 ≥3 种国家形态（不是"玩家选路径"）；每种形态的成因可追到地理/生计/剩余 |
| 2 地理×能动性 | M4/M8 | 换统治者或换法律 → 50–100 年后显著分叉；地理相似政权结局方差 > 0 |
| 3 渐进分层 | M3/M6 | 开局 ≤2 子类，100 年内按革新顺序逐个涌现并留编年史 |
| 4 革新核心 | M6 | 随机 10 条革新，每条 ≥1 个下游可观察变化 |
| **5 反数值驱动** | **全部里程碑** | 每个里程碑交付前用**三问**审一遍新增/改动的驱动：①有物质载体？②经过既有环节？③删掉会断链？——三问不过的数值加成一律不得入库 |
| **6 可模组** | M3 起逐项、M10 收口 | 新增一个内容条目（物产/革新/法律/建筑/聚落类型）**不需要改 `.cs`** |
| **7 性能** | 全部里程碑 | 新系统必须声明成本阶（全量 O(N)/增量 O(dirty)/事件 O(1)）；1000 模拟年在预算内跑完 |
| **8 架构解耦** | 全部里程碑 | 交付前过五条：单一真源 / 内容改动不改代码 / 新增系统不改既有系统 / 替换只改接口 / 不跨层写可变字段（详见 §4.4 与宪章原则 8） |
| **11 过程优先** | M3–M8 | 长周期效果（建筑/开垦/远航/围城/变法/联姻/迁徙/密谋）必须以 `Plan` 承载，可在中途中断或失败，并留下编年史与消耗；"点一下立即生效"的实现不予通过 |

---

## 8. 待议（范围扩张候选，未批准不做）

> 设计方向上已**没有悬而未决的问题**（宪章 §6）：能动性、胜利条件、偶然性预算、powers 代价均已在宪章中定为原则（2 / 9 / 10），不再是待议。
> 仍在等裁决的只有 **D8 信仰模型**（E1 以代码为准 / E2 以文档为准 / E3 折中）——这是"文档口径 vs 代码实现"的裁决，不是设计问题；M9 前必须定。
> **已否决**（不作为缺口）：`InnovationEra` 硬门槛、阶层随时代自动升级、"给某国加钱/改胜负"式的干预、"黑天鹅概率"式全局偶然参数、powers 的代价/反噬层。

1. **信仰模型三选一**：见 `DECISIONS.md` D8（E1 以代码为准 / E2 以文档为准 / E3 折中）。**M9 开工前必须裁决**。
2. **"删而不是补"清单**：以下系统/字段当前是"承诺未兑现"，建议优先考虑删除而非实现——
   - 司法链（罪行→审判→判决）：无案件实体、无触发源、无 UI；若要留，必须一次做完五环（M5 已列）。
   - `LegalNormStage` / `fundamentalizedLawIds` / `normKind` / `scope` / `precedence` / `overrides`：JSON 出现 0 次。
   - 亲兵（`RetinueSystem` 从未实例化）、海军（写侧缺失）、城防（`WallDefense` 无战斗消费者）、城市区划（门槛永不可达）。
   - 死字段：`isCapital`（全图无首都）、`SettlementData.composition`、`AnchorData.influenceRadius/bottleneckTileIndex`、`PopulationBlock.profession`。
3. **世界生成管线是否接回**：`MapGenerationConfig`(440 行) + 8 个世界模板 + `GenerationPipeline` 在代码里完整存在，但开图绕过它们（`GameManager.cs:149`）。接回成本不高，但会改动"开新档"这条主干。
4. **是否保留 21 个内容域全量**：`Properties`(5) / `MentalHealth`(5) / `Equipment`(23) 等域当前无消费者或半消费者，是否降级为"待激活"。
5. **UI 面板从 6 个扩到多少**：动词表里有 4 个"无入口"（税率、军事、革新、外交），每个入口≈一个面板 + 契约 + 测试。
6. **本地化**：de/es/fr/ja/ko 为空（0 条），是否砍掉这些语言文件或补齐。
7. **`GameSaveSystem`（642 行）处置**：整类 `[Obsolete]` 且零引用，但头衔实例、部分政权字段只在那里实现。删它之前必须先把这些实现迁进 `SaveSystem`（M8 前置）。

---

## 9. 证据索引

| 证据 | 路径 |
|---|---|
| **设计宪章（方向权威）** | `docs/plan-v3/DESIGN_CHARTER.md` |
| 结构摸底 | `_fc_verify/recon-structure.txt` |
| 内容清单 | `_fc_verify/content-inventory.txt` |
| 文档清单 | `_fc_verify/docs-inventory.txt` |
| 八域审计 | `docs/plan-v3/audit/{A..H}-*.md` |
| 法律/革新前情 | `reports/law-audit.md`、`reports/innovation-audit.md`、`reports/law-innovation-code-status-20261109-0047.md` |
| **法律与习惯生命周期** | `docs/plan-v3/LAW_LIFECYCLE.md` |
| **关系层（S17）** | `docs/plan-v3/RELATION_LAYER.md` |
| **信仰大系统（S18）** | `docs/plan-v3/BELIEF_SYSTEM_MODEL.md` |
| **性能教训（CK3 日志）** | `docs/plan-v3/PERF_LESSONS_CK3.md` |
| 架构规则 | `docs/architecture/ARCHITECTURE_RULES.md` |
| 文档处置 | `docs/plan-v3/DOC_DISPOSITION.md` |

---

## 10. 变更记录

| 日期 | 变更 |
|---|---|
| 2026-11-09 | 建立 v3 总纲：产品定义、内容盘点、判定口径、八域状态矩阵（A/B/C/D/E/F/G/H）、可玩性主干与调度语义、M1–M10 垂直切片里程碑（含施工与 DoD）、依赖 DAG、DoD、待议。**取代** `UNIFIED_MASTER_PLAN.md` 的状态表与排期（其 §4 决策继承）。 |
| 2026-11-09 | 依据审计新增决策：D6 人口单源、D7 法律双源合一、D8 信仰模型待裁决、D9 冻结内容新增。八域审计全部收齐；完成 1 次跨域裁决（继承：以 C 域"新局不触发"为准）；`DOC_DISPOSITION.md` 交付（129 份逐份处置）。 |
| 2026-11-09 | **接入设计宪章**（`DESIGN_CHARTER.md`）：8 条学术基础 → 机制含义/禁止做法/代码锚点；4 条设计原则 → 可断言信号。据此新增 **M0.5 历史行为验收台**、DoD 增补"宪章横向门禁"、M6 的 mastery 修复提前为 M3 前置、M7 增补建国路径接线；**否决** Era 硬门槛与"阶层随时代自动升级"两项伪缺口。 |
| 2026-11-09 | **定位修正（用户口径）**：撤回"三支柱"框架与"建国路径选择"；改为**纯历史模拟 + 多层级视角**，路径改判为事后分类。新增宪章原则 5/6/7（反数值驱动 / 可模组 / 性能门禁）与 §4.3 去数值化清单（7 处）；D12 撤回，新增 D16–D19。 |
