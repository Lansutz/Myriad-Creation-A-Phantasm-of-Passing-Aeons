# 方案：头衔授予/继承与剩余收口计划

> 2026-11-08。性质：**计划，不改代码**。依据：`DECISIONS_M1_M2_M3.md`（§7–§9.2、§8.13）、`05-batch-audit.md`、代码现状。
> 目标：把审计②剩余的「头衔授予/继承玩法」做成最小可用闭环，并给命名空间环、疾病双轨两条遗留问题定路线。

## 0. 结论与范围

- **头衔继承按三模式**（`TitleSuccessionMode`：None / RealmLaw / OwnLaw）+ 模板绑定 `definitionId`；暂不做多领地头衔分配（还没有 land title 实体）。
- 官职头衔**不继承**：在任者死亡/空缺即结束，按现有任命流程重填（已在 `GameWorld.Simulation.cs` 官职循环接线）。
- `TitleManager` 的 `ByCategory/ByTier` 在 P2 接成消费者；`ChildrenOf/GetChain` 依赖头衔层级（`superiorTitle`），等领地头衔体系再开。
- 命名空间环与疾病双轨各给一条明确路线，不夹带在头衔批次里。

## 1. 原则（沿用已定口径）

1. 封闭开发：不保留旧模型、不写迁移、不标 legacy。
2. 单一真源：模板进 `TitleManager`，实例进 `TitleInstance`，存档只存实例。
3. 先定口径再写代码；每批 `COMPILE_OK` + EditMode 全绿；改行为跑 PlayMode×2。
4. 不新增平行系统；接现有继位/任命/存档入口。

## 2. 现状事实（本计划的前提）

| 事实 | 位置 |
|---|---|
| 模板：`TitleDefinition`（category/rank/weight/…） | `Simulation/Definitions/TitleDefinition.cs` |
| 模板注册：`TitleManager`（LoadFromContent 从 Titles.json 映射 7 字段：titleId/category/successionMode/ownLawPreset/rank/weight/semantic） | `Simulation/Systems/TitleManager.cs` |
| 运行时：`TitleInstance`（titleId + holders）、`TitleHolding`（role/share/日期）、`TitleInstanceRegistry` | `Simulation/Politics/` |
| 政权主头衔键：`RealmData.primaryTitleKey`（惰性生成 `realm_<id>_primary`） | `Simulation/State/RealmData.cs` |
| 继位：`NotifyRulerTransition` 按 `TitleSuccessionMode` 分支（None→EndActiveHoldings；RealmLaw/OwnLaw→TransferSenior），definitionId 自动绑定 | `GameWorld.Succession.cs` |
| 官职：六类 `officeHolders`；任命→`TransferSenior`，死亡/空缺→`EndActiveHoldings` | `GameWorld.Simulation.cs` |
| 存档：`TitleInstanceSaveData`（含 definitionId）/`TitleHoldingSaveData` + `realms.primaryTitleKey` | `Infrastructure/Save/` |
| 继承法：`InheritanceLaw`（scope/gender/branch/age/titleMode/landMode）、`InheritanceHeirSelection`；`SuccessionSystem` 只换 `monarchId` | `Content.Definitions` / `WorldState` |
| 未用查询：`TitleManager.ByTier/ChildrenOf/GetChain/FilterEligible` | 审计② |

## 3. 范围切分

| 类别 | 内容 |
|---|---|
| 做（本计划） | 主权头衔实例懒创建、按 `TitleSuccessionMode`（None/RealmLaw/OwnLaw）继承、模板绑定 `definitionId`、`ByCategory` 消费者、测试与存档往返 |
| 延后 | 多领地头衔分配（`DistributeLand` 真跑）、`ChildrenOf/GetChain`、官职 `FilterEligible` 资格校验 |
| 不做 | 新的头衔系统；CK3 式硬 kind；旧档迁移 |
| 另案 | 命名空间环拆分、疾病双轨收敛 |

## 4. 阶段计划

> **补丁（2026-11-08）**：头衔继承规则以 §8「头衔继承法归属」为准；P0/P1 中"只做 SoleHeir"的表述被 §8 的三模式取代。

### P0 口径与数据结构（先做，0.5 批）

1. `TitleInstance` 增加 `definitionId`（模板 id，空 = 未绑定）。
2. 明确实例键规则：主权 `realm_<id>_primary`；官职 `realm_<id>_office_<k>`；未来领地 `realm_<id>_land_<titleId>`。
3. 明确 `titleMode` 语义：
   - `SoleHeir` → 单一继承，`TransferSenior(heir)`；
   - `FamilyShared` → 保留共治（senior + co-ruler），MVP 先记录 TODO，不实现。
4. 决策点确认（见 §5）后动 P1。

### P1 主权继承最小闭环（核心）

> 注：P0/P1 的 `titleMode`（SoleHeir/FamilyShared）表述已被 §8 的三模式（`TitleSuccessionMode`）取代；实现见 §8.14。

1. `TitleInstance`/Registry：懒创建（已有 `GetOrCreate`）。
2. `NotifyRulerTransition` 改为：
   - 取政权主实例；
   - 若 `definitionId` 为空，调 `TitleManager.ByCategory(Landed)` 取 rank 最高者绑定（无内容则保持空）；
   - 按 `TitleSuccessionMode`：None→`EndActiveHoldings`；RealmLaw/OwnLaw→`TransferSenior(heir, day)`；`FamilyShared`（分配方式）仍 TODO；
   - 若继承规则需要资格校验，用 `TitleManager.Get(definitionId)?.eligibility` + `TitleCandidateContext`（MVP 先跳过）。注意：绑定目前走 `TitleCatalog.Highest`，它仍依赖 `rank`；而 §11 计划删除 rank——**必须先有替代的位阶/组合序，再删 rank**，否则 Highest 无法排序。
3. 测试（EditMode）：
   - 懒创建 + 继位转移 + 旧持有结束；
   - `definitionId` 绑定后 `TitleManager.Get` 能取到；
   - 官职实例不随主权继承变化；
   - 存档往返（已有 `TitleSaveRoundTripTests`，补 `definitionId` 字段）。
4. 门禁：Compile OK + EditMode 全绿 + PlayMode×2（继位是行为变更）。

### P2 模板绑定与查询消费者

1. 政权创建/首任君主时绑定 `definitionId`（`ByCategory(Landed)` 最高 rank；`ByTier` 用于 tier 校验/显示）。
2. `RealmTitleEvolution.GetRulerTitleDisplay`：优先用实例 `definitionId` 的模板语义/称谓，回退现有 titleId switch。
3. 测试：绑定后编年史称谓来自模板；无模板时回退不崩。

### P3（可选）官职资格

- 为 `OfficialOffice` 六值在 `Titles.json` 建对应 `TitleDefinition`（category=Office）；
- 任命选人时用 `TitleManager.FilterEligible` + `TitleCandidateContext`；不到必需，放 P3。

### P4 收口（另案，与头衔批次分开）

1. **命名空间环**：目标方向 `Politics（模型+实例） ← Systems（服务） ← Definitions（纯 DTO）`。批次：先移 `TitleManager`/`TitleCatalog` 到 `Simulation.Politics`，再让 `Definitions` 只留 DTO；每步编译+测试。若代价过大，降级为"只移模型、服务留 Systems，用接口隔离"。
2. **疾病双轨**：用户已定 **C**（人口级权威——疾病的意义是减人口；角色级保留为有名角色病程层）——见 §9；人口级不再直接扣血/杀角色，人口减少走人口块结算。
3. 审计复跑 + `DECISIONS`/`CHARACTER_PLAN_SEAM` 同步。

## 5. 待拍板点（D1–D4；D4 见 §8.3）

| # | 问题 | 推荐 |
|---|---|---|
| D1 | `FamilyShared` 是否 MVP 支持 | **先不做**，只 `SoleHeir`；共治等"多领地/多王"玩法 |
| D2 | 是否现在绑定 `definitionId` 并按 rank 选模板 | **做**：消费者少、能立刻让 `ByCategory/ByTier` 真跑 |
| D3 | 疾病双轨：删角色级（A）/角色级接管（B） | **C（用户定）**：人口级是权威（疾病的意义是减人口）；角色级保留为有名角色病程层，不是统计权威 |

## 6. 风险与回滚

- 继位改行为：PlayMode×2 + 现有继位测试；失败回滚到"只转移、不绑定"。
- `TitleManager` 静态注册表：`ContentRegistry.Reset` 生命周期已处理；测试需 `Clear`。
- 存档新字段：`definitionId` 为增量字段，旧档缺省空；不升 SaveVersion。
- 命名空间拆分若牵动面大：先出依赖图，再决定是否降级为接口隔离。

## 7. 验收（每批）

1. `mc-compile.ps1` → `COMPILE_OK`。
2. EditMode 全绿（当前基线 703，新增测试不得回退）。
3. 改行为批次跑 PlayMode×2（9/9 + 9/9）。
4. `DECISIONS` 记录批次；只读审计（如需要）。
5. 交付说明：改了什么/没改什么/风险。

---

## 8. 补充口径：头衔继承法归属（2026-11-08）

用户提出：头衔不能都按政权继承法走——要有**自带独立继承法的头衔**，也要有**无继承法的头衔**。据此把继承规则改为三模式：

| 模式 `TitleSuccessionMode` | 含义 | 典型头衔 | 继位时行为 |
|---|---|---|---|
| `None` | 无继承法 | 官职、委任/终身职、教职 | 不转移；在任者死亡/去职即结束持有，回到空缺与任命/推举流程 |
| `RealmLaw` | 随政权继承法 | 主权头衔（王国/帝国级）、部分领地 | 用政权当前继承法选继承序，转移给新持有者 |
| `OwnLaw` | 自带继承法 | 特殊领地/头衔（如萨利克、塔尼斯特、分地继承的特定头衔） | 用该头衔自己的继承法选人，不受政权法覆盖 |

### 8.1 数据与代码落点（实现状态见 §8.14）

- 新枚举 `TitleSuccessionMode { None, RealmLaw, OwnLaw }`（放 `Core.Enums`，跨层可读）。
  - 注意：已有 `TitleInheritanceMode { SoleHeir, FamilyShared }` 是"头衔分配方式"，**不要混用**。
- `TitleDefinition` 增字段：
  - `successionMode`（默认 `None`，官职/未配置头衔天然无继承）；
  - `ownLawPreset`（`OwnLaw` 时使用；字符串预设 id）。
- 新增 `InheritanceLawCatalog`（`Content.Definitions` 或 `Simulation.Definitions`）：
  - 把现有静态工厂（`Primogeniture / Ultimogeniture / Tanistry / Matrilineal / FrankishPartible / Salic`）映射为预设 id；
  - `Resolve(presetId)` 返回 `InheritanceLaw`，未知 id 回退长子继承并记日志；
  - 不新建法律实体、不复制 JSON。
- `Titles.json`：每个头衔补 `successionMode`（整数）与可选 `ownLawPreset`；官职类默认 `None`。
- 继承执行（P1）：
  1. 取头衔实例；
  2. `None` → `EndActiveHoldings(day)`，标记空缺，走任命/推举；
  3. `RealmLaw` → 用政权当前继承法；
  4. `OwnLaw` → `InheritanceLawCatalog.Resolve(ownLawPreset)`；
  5. 用 `InheritanceHeirSelection.GetHeirOrderedPool(law, candidates, currentRuler)` 选出人选 → `TransferSenior`。
- 存档：继承法在模板上，**实例不必存法**；`TitleInstanceSaveData` 不变。

### 8.2 与多领地/共治的关系

- `FamilyShared`（共治）只在"该头衔自己的法"或"政权法"是分地/共治型时触发；`None` 头衔永远不共治。
- 多领地头衔分配（`DistributeLand`）仍延后；本批只保证"每类头衔按自己的法转移"。

### 8.3 待拍板 D4

| # | 问题 | 推荐 |
|---|---|---|
| D4 | `OwnLaw` 的存储：预设 id（引用）还是内嵌完整 law 对象 | **预设 id + `InheritanceLawCatalog`**：Titles.json 不重复法律字段，一处定义多处引用 |

### 8.4 验收

- EditMode：三模式测试（None 空缺、RealmLaw 转移、OwnLaw 只记录法源、未按法重选人——见 §8.14 备注）+ 未知预设回退；
- 存档往返：`successionMode/ownLawPreset` 在模板侧（Titles.json）不需入档，实例测试沿用现有；
- 行为变更批次跑 PlayMode×2。

---

## 9. 疾病系统收敛：人口级权威 + 角色级病程层（C 方案，2026-11-08 用户定）

> 修正：D3 不是"角色级接管"，也不是"删角色级"。**疾病模拟的意义在减人口**——人口级 `DiseaseSystem` 是权威；角色级只表现有名个体的病程与死因。

### 9.1 分工

| 层 | 权威范围 | 内容 |
|---|---|---|
| 人口级 `DiseaseSystem` | **疾病 → 人口变化**（模拟意义所在） | 感染率/死亡率/生育率/迁移/劳动力/税收影响；按地区/聚落结算人口块 |
| 角色级 `CharacterDiseaseSystem` | 有名角色的病程表现 | 统治者/廷臣/将领的感染、分期、治疗、属性修正、死因；死亡回写人口 |

- 人口级决定"这片地区会死多少人"；角色级决定"死的是谁、病成什么样、对决策有什么影响"。
- 不再让角色级成为人口统计的权威，也不删除它；删掉人口级就等于删掉减人口的模拟意义。

### 9.2 批次

1. **人口级参数审计**：现有 `DiseaseSystem` 的死亡率/生育率/迁移/劳动力影响是否真的作用到人口块（先测再改）。
2. **人口结算闭环**：疾病 → 人口块（死亡/出生/迁移）→ 劳动力/税收/兵源；确保可观测（地区人口曲线）。
3. **角色感染挂钩**：人口疾病压力 → 有名角色感染判定（`InfectCharacter`）；角色死亡原因链保留，死亡同时回写人口。
4. **属性与决策影响**：`GetEffectiveStats` 合并疾病修正（保留角色层价值）。
5. **存档/测试**：人口疾病状态与角色 `activeDiseases` 往返；人口减少回归断言。
6. **验收**：Compile OK、EditMode 全绿、PlayMode×2；人口级疾病改动必须有"人口减少"的可观测断言。

### 9.3 与头衔批次的关系

独立 P4 批次，放在 P0–P2 之后；人口级改动影响面大（人口/经济/军事），单独跑基线对比。

### 9.4 CK3 对照（实证：`common/epidemics/`，7 种疫病）

CK3 的疫病系统不是"人口系统"，是**省份感染等级 + 爆发强度 + 角色感染概率**三层：

| 层 | CK3 字段/机制 | 说明 |
|---|---|---|
| 疫病类型 | `trait`、`color`、`shader_data`、`priority` | 一种病 = 一个 trait + 地图表现 |
| 角色感染 | `can_infect_character`、`character_infection_chance`（每月，按角色）、`on_character_infected` | 角色靠 trait 表现疾病 |
| 省份感染 | 每省 `infection level 0-100`；`infection_levels` 按阈值给 province/county/realm modifier | 不是人口，是修正 |
| 爆发强度 | `minor / major / apocalyptic` 三档；每档 `outbreak_chance`（省/年）、`spread_chance`（相邻省/月）、`max_provinces`、`infection_duration / progress_duration / recovery_duration` | 强度决定规模、时长、通知 |
| 效果 | `development_decline`、`monthly_county_control_decline_add`、`supply_limit_mult`、`levy_size/levy_reinforcement_rate`、`apply_infection_development_loss` | **用发展度/控制力/征召/补给代理"人口损失"** |

关键结论：

- CK3 **没有人口数**，所以它的"减人口"是间接的（发展度、控制、兵源、补给下降），不是真实人口曲线。
- 我们有人口块，正好可以补上 CK3 做不到的那一层：**省份感染等级 → 人口死亡率/生育率/迁移/劳动力**，这才是"减人口"的正门。
- 值得照抄的结构：疫病类型定义、省份感染等级与阈值修正、三档爆发强度、相邻传播概率、最大省份数、感染/恢复时长、角色每月感染概率与 on_* 钩子。
- 不照抄：用 development 代理人口（我们有人口，不该只降发展度）。

因此 §9 的批次补一条：**先建"省份感染等级 + 强度档 + 传播"骨架，再接人口块结算**；角色层只消费感染概率与 trait。

---

## 10. 项目原则：机制优先，少用抽象数值（2026-11-08 用户定）

> 用户口径：本项目**尽量不搞抽象数值**，不用抽象数值去"模拟"，而是**通过机制**（规则、资格、权限、程序、关系、状态、制度）来表达。

### 10.1 判据（每个设计点先过三问）

1. **这段逻辑是不是拿一个抽象分数替代了本该存在的规则？**
   - 例：`treasury >= 200` 决定能不能宣战 → 应有"战争经费从哪来"的程序（国库/借款/封建义务/非常税）。
2. **能不能用资格 / 权限 / 程序 / 关系 / 状态机表达？**
   - 例：头衔位阶用 `rank 2.0 vs 3.0` 比较 → 应表达为法律位阶、权利束、臣属/共治关系。
3. **这个数字是"度量"还是"决策源"？**
   - 计数（人口、兵力、距离、日期、年龄）、用户 8.1 设计里的 0-100 属性刻度：可以作为度量/输入；但**决策与结果**要由机制决定，不能"掷分定生死"。

### 10.2 已做的抽象数值审查

| 现状 | 问题 | 机制化方向 |
|---|---|---|
| 门禁资源层 `RealmTreasuryProvider` 固定门槛 200 | 平阈值替代财政程序 | 战争经费决议/借款/封建义务/非常税；阈值只作"最低可行性"而非规则 |
| 头衔 `rank` 浮点（2.0/3.0 比较） | 位阶用数值比较 | 法律位阶 + 权利束 + 臣属关系；rank 仅派生显示 |
| 继承 `OwnLaw` 只选法源、仍用政权已选人 | 法不参与选人 | 按该头衔的资格规则（scope/gender/branch/age + 权利/职位要求）构建候选池 |
| 疾病 `infection level 0-100` | 抽象等级驱动全部结果 | 状态机（洁净→潜伏→爆发→消退）+ 传播途径（贸易/旅行/军队/人口密度）+ 隔离/卫生/埋葬制度；人口减少由死亡机制结算 |
| 专精/特质修正纯 +% / +值 | 数值加成替代资格与职权 | 保留属性作度量；效果优先走"资格/权限/职位/事件分支"，数值修正只作次要度量 |
| 门禁能力层 `ICapabilityProvider` 无门槛 | 方向对（能力不授权） | 保持：能力只影响执行机制（战术选择/工程质量），不产生许可 |

### 10.3 对后续批次的影响

- **P2（OwnLaw 选人）**：按"头衔资格规则 + 候选池"实现，不做"rank 加权抽签"。
- **P4（疾病）**：先做状态机与传播机制，再谈人口结算；`infection level` 若要保留，只作 UI 度量，不作唯一决策源。
- **门禁**：把 `treasury` 固定阈值降级为"最低可行性检查"，把"能不能宣战"交给经费程序/合法性/职权。
- **评估**：每个新批次交付时加一节"本批是否新增抽象数值决策源"，有则说明理由。

### 10.4 例外（允许的数值）

- 人口/兵力/粮食等**计数**；
- 日期、距离、时长；
- 用户 8.1 设计的 0-100 属性刻度（作为度量与输入）；
- 机制内部的临时权重（但不得成为对外可见的"决策分数"）。

---

## 11. 位阶：指代 + 称谓的组合序，跨域不可比（对齐语义字段表）

> 依据：`TITLE_SEMANTIC_FIELD_SCHEMA.md`（指代 / 称谓 / 语义特征 / 分布）。
> 结论：**位阶不是全局数值**；完整头衔 = **指代 + 称谓**；位阶只在同域（同词干 / 同权威来源）内由**称谓组合结构**给出偏序；跨域由礼制 / 臣属 / 主权裁决。

### 11.1 四个层次

| 层 | 内容 | 作用 |
|---|---|---|
| 指代 | 族群 / 地域 / 行政区划（建制）/ 官职机构 / 宗族家族 / 宗教细分 / 人物（可选） | 决定头衔**类型**（统治型/官职型/亲属型/宗教型/荣誉型）与显示模板；**不直接定位阶** |
| 称谓 | 词干 + 修饰（bound）+ 关系式（X-of-X）+ 复合（皇+帝）+ 累积（尊号链） | 同词干内给出**严格序**；修饰不能独立时不得单列 |
| 绑定 | landed / office / honorific / claim；liege 链 | 位阶**硬依据**：有领地/政权绑定 > 纯官职 > 荣誉 |
| 权威来源 | `SemanticAuthority` 10 值（可多值来源栈） | 跨域**不可比**的边界 |

### 11.2 同域偏序（称谓组合）

| 称谓 | 组合结构 | 序 |
|---|---|---|
| 王 | 基础词干 | 基准 |
| 大王 | 修饰（`great`）+ 词干 | 高于王 |
| 王中之王 | **关系式**（X-of-X），不是修饰 | 高于大王 |
| 皇帝 | **复合词**（皇+帝） | 不是"王+1"；加冕/礼制单独规则 |
| 尊号链 | 累积 | 是集合/链，不参与单一序 |
| 神 / 圣 | distribution = **both** | 可独立（神/圣人）也可修饰（神王/圣王）；不能定为"只能修饰" |

### 11.3 跨域不可比

`长老`（亲属/宗族）、`王`（领主）、`将军`（军事）**不排数值**。跨域只能通过：

1. 礼制位次（court precedence / 朝会次序）；
2. 臣属关系（liegeTitle / contract）；
3. 主权关系（谁向谁称臣、谁授权）。

### 11.4 实现落点（替代 rank，注意顺序）

1. **先实现替代序**：
   - `CompositionOrder(stem, composition)`：同域组合序（表驱动，不是 float）；
   - `BindingPriority`：landed > office > honorific > claim；
   - `AuthorityDomain`：跨域返回"不可比"。
2. 把 `TitleCatalog.Highest` 从 `rank` 改为按上述规则排序（先不改数据）。
3. **再删除** `TitleDefinition.rank` / `TitleDef.rank` / `Titles.json.rank`（封闭开发，不留兼容）。
4. `TitleDefinition` 增 `authority`（可多值）、`appellation`（词干/修饰/关系式/复合/累积 + 指代槽位）、`bindingType`；`precedence` 改为计算属性。
5. `TitleAppellationContext` 扩 `authority` / `register`。

> 关键：不能先删 `rank` 再补替代——`TitleCatalog.Highest` 现在靠 rank 排序，先删会直接崩。

### 11.5 与继承的衔接（权威来源 → 默认机制）

| 权威来源 | 默认继承/产生方式 |
|---|---|
| elder_council / paternal_clan | 家族/长老推举（tanistry / seniority） |
| military | 推举/强者（无选举机构则 `None` 走任命） |
| religious | 教职任命/教团推选 |
| civic_republic / election_assembly | 选举 |
| lord_master / dynastic_name | 世袭（长子/幼子/分地按政权法） |
| palace_institution | 官僚任命（`None`） |
| tribute_overlord | 朝贡继承（随宗主确认） |

与 `TitleSuccessionMode` 三模式吻合：`None` = 无继承法，走任命/推举程序。

### 11.6 验收

- 四例：`王` / `大王` / `王中之王` 同域严格序成立；`长老` 与 `王` 返回"不可比"；
- 分布测试：`大` 不能独立成称谓；`神/圣` 可独立也可修饰；
- `rank` 删除后 `Highest` 走 `CompositionOrder`，排序不崩；
- 显示仍为"指代 + 称谓"，绑定不显示。

## 12. 自审记录（2026-11-08，检查本计划与实现/最新口径）

| # | 问题 | 处置 |
|---|---|---|
| 1 | §4 P4 仍写疾病"用户已定 B"，与 §9 的 C 矛盾 | 已改 C（人口级权威 + 角色级病程层） |
| 2 | §7 基线写 699 | 已改 703（三模式批次后的实测） |
| 3 | `titleMode`（SoleHeir/FamilyShared）与 `TitleSuccessionMode`（None/RealmLaw/OwnLaw）混用 | 统一为 `TitleSuccessionMode`；P1 加注 |
| 4 | §2 写 LoadFromContent"映射 5 字段" | 实际 7 字段，已改 |
| 5 | §2 继位只写 `TransferSenior` | 补：按模式分支 + definitionId 自动绑定 |
| 6 | §2 存档未提 `definitionId` | 已补 |
| 7 | §11 写"指代可选" | 改为"可选性取决于指代类型"（对齐语义字段表） |
| 8 | §11 计划删 `rank`，但绑定/`TitleCatalog.Highest` 仍依赖 rank | 加注：**先有替代位阶/组合序，再删 rank**，否则 Highest 无法排序 |
| 9 | §8.4 验收写"OwnLaw 按预设转移" | 实际只记录法源、未按法重选人；已改表述 |
| 10 | §5 标题"三个待拍板点"却有 D1–D4 | 已改为 D1–D4 |

**仍存在的已知不一致（未改，按批次排期）**：

1. 代码里 `rank` 仍在（`TitleDefinition.rank`/`Titles.json`），而 §11 已定删除——顺序：先实现位阶/组合序，再删 rank。
2. `OwnLaw` 只做法源选择，未按该头衔的资格规则重选人（P2）。
3. `TitleManager.ByTier/ChildrenOf/GetChain/FilterEligible` 仍无消费者。
4. 命名空间环（Definitions ↔ Systems/Politics）未拆。
5. ~~§11 待对齐~~（已重写，见上）。
