# P5 自然涌现与机制化财政计划

> 触发：P4.0 以“开局已有政权、再补 3 名宫廷人物、靠官员硬门禁收税”为前提，违背本项目“世界生成之后自然演化”的总纲。
> 本计划取代 `P4_DEFERRED_PLAN.md` §P4.0 的前提，并修正 `DECISIONS.md` D132 中“开局宫廷/开局官职”的部分；D132 的“无征税官则无税”方向保留，但征收资格改由行政区划与仓储机制决定。
> 施工原则不变：机制优先于数值（D16）、可模组化（D18）、性能预算（D19）、因果耦合/代码解耦（D20）、确定性、旧档兼容、每批 build 全绿后提交推送。

---

## 0. 事实基线（本轮已核对代码）

| 事实 | 位置 | 判定 |
|---|---|---|
| 默认新世界 `CreateDefault()` 是 `Custom`：不生成政权、不生成聚落、不生成单位 | `WorldSetupOptions.cs:33-40` | 默认世界几乎是纯地理，和“自然演化”也不符 |
| `CreateStandard()` 预置 3 个政权、每个君主+配偶、国教、高祭司=君主、一支军队、`PlayerRealmId=0` | `WorldSetupOptions.cs:90-99`、`GameWorld.Generation.cs:61-152` | 沙盒预置；被 30+ 测试当作标准世界 |
| 生成末段调用 `CreateInitialCourtiers` + `EnsureOfficeHolders` | `GameWorld.Generation.cs:172-174`、`395-400` | **P4.0 的错误前提**，必须移除 |
| 宫廷 = 每政权 3 名成年贵族/学者/军人 | `CharacterManager.cs:1063-1095` | 为“开局有官”而造人，倒果为因 |
| `FillVacancies` 对内容表里所有 `enabled` 官职补缺，只要求“有一个活人” | `OfficeSystem.cs:82-130` | 官职不随制度/人口/领土出现 |
| 税收 = `fertility × development × 10 × 人口因子 × 秩序因子 × 税率`，直接加进 `realm.treasury` | `EconomyManager.cs:639-665`、`TaxSystem.cs:34-44`、`PoliticsSimulationSystem.cs:84-91` | 典型抽象数值驱动；地方仓储与货币完全没参与 |
| `FormRealmFromActor` 已有“行为体→建国”，但全工程零调用 | `CultureStageEvolutionHost.cs:24-97`；D12 已记录 | 死代码；且 `ApplyPathBonuses` 给 `stability=55/treasury=2000` 等硬加成 |
| 村→镇演化存在：营地人口≥50 且永久度满可成聚落 | `SettlementEvolutionSystem.cs:61-159` | 可复用；但营地补给无补给来源，约一个月内饿死（`CampData.cs:92-124`） |
| 行政区划已存在且会分配持有人 | `AdminDivisionSystem.cs`、`GameWorld.Simulation.cs:157-181/183-...` | 征税资格应走这里，而不是“全局有没有一个征税官” |
| 地方仓储 = 每地区 `TradeCenter.inventory`；国家仓 = `TreasuryVault`；`RealmTreasury` 是货币唯一入口 | `TradeCenter.cs`、`TreasuryLedger.cs`、`RealmTreasury.cs` | 机制化财政的现成骨架 |

结论：不是“再补几个人/再调一个税率”，而是把**政权、官职、税收三者的出现顺序**倒回来：先有聚落与剩余，再有政权；先有领土与人口，再有官；先有官与仓储，再有税。

---

## 1. 目标口径

1. **默认新世界 0 政权、0 君主、0 宫廷、0 官职、0 税收、0 国库货币**；只有地理、聚落、人口与自然过程。
2. 政权由**聚落/行为体的物质条件**在世界运行中自然形成；`generatePolities` 只保留为沙盒/调试/测试开关，默认关闭。
3. 官职随政权规模与制度出现；没有合格人选、没有足够领土/人口/中枢能力，就没有官。
4. 税收是**实物从地方仓储被征收、运输、入库**的过程；货币税只在有货币阶段、有市场、有真实货物可售时出现。禁止 `fertility × development × 10` 之类税基。
5. 同一 seed 同结果；旧档仍可读；每批改动有测试、`build` 全绿、提交推送。

---

## 2. 阶段

### P5.0 时间线审计与默认世界口径

- 新建本文件作为审计表；后续每个“开局即存在”的字段都在表里标注处置（保留为沙盒/改为过程产生/删除）。
- `WorldSetupOptions.CreateDefault()` 改为自然基线：
  - `generatePolities=false`、`initialPolityCount=0`；
  - `generateSettlements=true`、`initialSettlementCount=-1`（按省生成聚落，不是国家）；
  - `generateInitialUnits=false`、`initialUnitCount=0`；
  - `enableNaturalSpawning=true`、`spawnSettlements=true`、`spreadPopulation=true`。
- `CreateStandard()` 明确标注为**沙盒预置**，仅供测试/调试；不再作为“默认新游戏”语义。
- 新增 EditMode 断言：默认世界生成后 `realms.Count==0`、角色数 0、官职 0、税收 0；聚落数 >0。
- 验收：默认世界能生成、能跑、无空引用；沙盒预设行为不变。

### P5.1 政权自然涌现

- 新增 `StateFormationSystem`（月 tick，优先级放在 `sandbox.spawn.monthly` 之后、文化月结之前），只做**候选判定**；写入世界由 `StateFormationHost` 完成（沿用 `*Host` 胶水层，规则与写入分离）。
- 候选条件（全部为物质/制度条件，不做玩家选择）：
  - 聚落未毁、地块无主、`currentDay - settlement.foundingTick >= 8 年`；
  - 聚落人口 ≥ 200、`development ≥ 6`；
  - 所在地区 `TradeCenter` 的食物库存 ≥ 人口 × 90 天口粮（有剩余才养得起脱产者）；
  - 本地秩序/稳定度不低于阈值（避免饥荒/战乱中建国）；
  - 全局每月最多形成 1 个政权，候选按 `settlementId` 稳定排序，保证确定性。
- 形成时：
  - 政权名取聚落名；`realmForm` 由文化移动性/聚落形态决定（定居/游牧），不写死文明分支；
  - 领土 = 聚落地块 + 相邻无主且同文化/同省的地块（最多 3 块），写 `ownerRealmId` 与 `coreTiles`；
  - 统治者 = 本地成年人物中威望最高者；没有则按当地人口/文化创建一个“地方首领”人物与家族（这是机制：有人群才有首领），不额外造 3 名宫廷；
  - `treasury=0`、`prestige` 取统治者威望、`stability` 取聚落稳定度、`centralization` 由文化阶段/聚落等级推导；**删除 `ApplyPathBonuses` 的一次性硬加成**；
  - `stateReligionId=-1`（无国教，信仰靠传播/统一机制产生）；
  - 注册 AI 控制器、补齐外交关系、写编年史。
- 行为体路径：复用 `FormRealmFromActor`，条件为游牧/流民/土匪群体人口 ≥ 200、士气/补给充足、停留时间足够；同样不送金库、不送稳定度。若本轮时间不足，先保留入口但默认不触发，另立欠账，不假装完成。
- 验收：默认世界 100 年内至少出现 1 个政权；无剩余/人口不足/无主聚落不足时不建国；同 seed 同结果；建国不产生国库货币。

### P5.2 官职与宫廷随制度出现

- 删除世界生成与旧档兜底中的 `CreateInitialCourtiers` 调用（`GameWorld.Generation.cs:173/399`、`SaveSystem.cs:599`）；保留方法仅供测试显式播种时改名/标注为测试工具，不得在生产路径调用。
- `OfficeSystem.FillVacancies` 增加**任职门槛**（内容驱动，`OfficeDef` 新增字段，缺省 0 兼容旧内容/Mods）：
  - `minTiles`：政权实际控制地块数；
  - `minPopulation`：政权人口；
  - `minCentralization`：中枢能力；
  - `minCultureStage`：文化阶段（可缺省）。
- 候选资格补一条：排除 `Ruler/Spouse/Heir/Commoner`，只允许贵族/学者/军人/官员等可任职角色；君主配偶不得因“有一个活人”自动成为宰相。
- `Offices.json` 为 8 个官职补门槛（地方长官/区长官看领土与人口，宰相/司库看人口与中枢，军官看人口与政体）；门槛是“制度能否支撑这个职位”，不是产出加成。
- 生成结束时不再 `EnsureOfficeHolders`；官职由每日 `offices.daily` 在政权存在后自然补缺。
- 验收：新建政权首日 0 官职；只有君主+配偶时 0 官职；人口/领土/人物达标后逐步出现官职；配偶不占官；旧档已任官职不被清空。

### P5.3 机制化财政

将 `EconomyManager.SettleTaxes` + `TaxSystem.CalculateTileTax` 的抽象税基替换为**仓储征收链**：

1. **征收资格**：某地区有主地块，且该地块所属最低一级 `AdminDivision` 有持有人，持有人拥有 `taxation` 职权（或其本人就是君主且该地区为首都所在地区）；否则该地区本日不征。
2. **征什么**：只征地方 `TradeCenter.inventory` 里**真实存在**的货物，按 `TaxSystem` 的类别税率：
   - 食物/谷物 ← `agriculturalTax`；
   - 牲畜 ← `livestockTax`；
   - 矿石/金属/石/宝石 ← `miningTax`；
   - 装备/陶瓷/布/皮/玻璃 ← `craftTax`；
   - 奢侈品 ← `luxuryTax`；
   - 贸易中心库存另计 `tradeTax`。
3. **保留口粮**：征税前先扣除该地区人口 90 天口粮（`reserveDays` 作为 `TaxSystem` 政策字段），只对剩余部分征；不得把人口口粮征走制造饥荒。
4. **运输与入库**：按地区到首都的陆地距离计算运输损耗（距离 × 单位损耗，设上限），实际入库量 = 征收量 × (1 − 损耗)；货物进入 `TreasuryVault`，并记 `TreasuryTransaction`（地区、货物、数量）。
5. **货币化**：只有在政权 `CurrencyStage >= MintedCoin` 且当地市场有需求时，才把部分入库货物按当地 `TradeCenter.GetGoodsPrice` 出售，销售量受 `localDemand` 限制；所得计入 `realm.treasury`。没有货币阶段/没有市场/没有货物 ⇒ 国库货币不增加。
6. **俸禄与军费**：继续从 `realm.treasury` 支付；国库无钱时，允许从国家仓的粮食按同一价格折算支付（机制：实物俸禄），仍不足则欠俸，不凭空生成货币。
7. **删除**：`SettleTaxes` 的抽象税基、`CalculateTileTax` 的 `baseOutput` 参数与 `fertility × development` 公式；`PoliticsSimulationSystem` 不再直接 `RealmTreasury.Add(抽象税额)`。
8. **性能**：按地区而非按地块结算；每地区 O(库存货物种类)，地区数 × 货物种类远小于地块数 × 全货物；日 tick 只在有政权的地区跑。

验收：
- 无行政区划持有人 ⇒ 0 征收；
- 有持有人且地区有货 ⇒ 地方库存真实下降、国家仓真实上升；
- 货物不足保留口粮 ⇒ 不征食物、不引发饥荒；
- 无货币阶段 ⇒ 国库货币不变；有货币阶段且有市场 ⇒ 货币增加量与售价×销量一致；
- 同 seed 同结果；移除持有人后停止征收；旧档可读。

### P5.4 同类问题修正（本轮范围内）

- 默认世界不再调用 `InitializeDefaultRealms`；沙盒预置保留但只在 `generatePolities=true` 时生效。
- 生成结束不再补官、不再造宫廷、不再自动设国教/高祭司；这些只属于沙盒预置。
- 自然政权不继承 `RealmData` 的 `treasury=1000/prestige=50/stability=50` 默认值，形成时显式写入由实际条件推导的值（0 货币）。
- `SandboxSpawnSystem.TrySeedPopulation` 的“随机地块凭空造人”列为下一轮欠账（应改为从人口过载地块迁移），本轮先记录，不在自然建国链上依赖它。
- `PoliticsSimulationSystem` 的稳定度向 `50 + centralization×20` 回归、`RaceData` 常数、聚落财富只减不增等抽象驱动，列入审计表“后续处理”，不与本轮混做。

### P5.5 验证与收口

- `build.cmd`：`EXIT=0`、零 `error CS`、EditMode 全绿。
- 新增 `NaturalEmergenceTests`（默认世界无政权、建国条件正反向、确定性、官职门槛、税收机制正反向）。
- 新增 `natural` 长跑：默认世界 2 seed × 100 年，要求 ≥1 政权、≥1 官职、有税收流水、无异常；不复用沙盒预置。
- 旧档：读档后已有政权/官职/国库不被清零；新字段缺省有语义。
- 文档：本计划 + `DECISIONS.md` 新决策（D138 起）+ `PROJECT.md` 状态同步。

---

## 3. 非目标

- 不重做完整货币银行体系；只做“货物→市场→货币”的最小闭环。
- 不做逐地区运输队列/道路寻路；本轮用距离损耗表达运输成本。
- 不做玩家选择建国路径；`StateFormationPath` 仍是事后分类。
- 不修复 `ARCHITECTURE_UNFINISHED.md` 的 A–F 全部架构欠账；本轮只保证新增代码分层正确（规则/宿主/状态不混写）。
- 不追求文化阶段完美：本轮允许无主聚落先无文化，政权形成后按主导人口文化赋值；文化地理生成另立欠账。

---

## 4. 风险与回退

| 风险 | 处置 |
|---|---|
| 自然世界 100 年不出政权 | 先做“单聚落 + 充足剩余”的定点测试；长跑门槛按实际调，但不降低到“开局送政权” |
| 默认世界无聚落导致空转 | `CreateDefault` 改为生成聚落；聚落是人群，不是国家 |
| 税收改动击穿沙盒经济 | 沙盒预置保留初始国库；自然政权从 0 开始，先实物后货币 |
| 30+ 测试依赖 `CreateStandard` | 保留沙盒预设行为，不改测试基线；新增自然世界测试单独建世界 |
| 旧档宫廷/官职被清空 | 只删“生成时自动补”，不删读档恢复；读档恢复逻辑保持 |
| 性能回退 | 税收按地区结算，官职只在空槽扫人；长跑对比 tick 预算 |

---

## 5. 实施记录（2026-11-14）

### 5.1 已落地

- **P5.0**：`WorldSetupOptions.CreateDefault()` = 自然基线（聚落开、政权关）；`CreateStandard()` 明确为沙盒预置；新增 `WorldSetupOptionsTests.Default_HasSettlementsButNoPolitiesOrUnits` 与 `NaturalEmergenceTests.DefaultWorld_HasSettlementsButNoPolitiesCharactersOrOffices`。
- **P5.1**：`StateFormationSystem`（月 tick 37）+ `StateFormationHost`；建国条件为“无主聚落 ≥8 年、人口 ≥200、开发 ≥6、地区仓储 ≥90 天口粮”；建国写入首领/家族/地方显贵/领土；`ApplyPathBonuses` 删除；`FormRealmFromActor` 改为零货币/零威望 + 首领/编年史，不再送硬加成。
- **P5.2**：`CreateInitialCourtiers` 从生产路径移除；`OfficeDef` 增四类门槛并在 `Offices.json` 落地；`FillVacancies` 用 `OfficeRealmContext` 判定；配偶/继承人/平民/信使不进入候选池；生成结束不再填官。
- **P5.3**：`SettleTaxes`/`CalculateTileTax` 删除；`TaxCollectionSystem` 按行政区划持有人、地区仓储、90 天口粮、距离损耗、铸币阶段与市场需求的顺序结算；`TaxReceiptLedger` 记实物流水；`PoliticsSimulationSystem` 只调用 `CollectTaxes`。

### 5.2 施工中发现并修复的前置 bug

1. **`AnchorGenerator.CreatePair` 未回填 `SettlementData.anchor`**：生成期 `settlement.tileIndex` 返回 -1，`SeedPopulation` 把 500–2000 人写进 -1 号地块；聚落人口快照为 0，180 天后被 `SettlementSimulationSystem` 当成“人口流失”废弃为营地。自然世界因此只剩 17 个军事据点、0 个城市，永远不可能建国。修复：`CreatePair` 创建 Settlement 时即 `anchor = item`。
2. **聚落地块开发度为 0**：`AgricultureYield.Compute` 用 `tile.development`，生成聚落只设了 `SettlementData.development`；城市按未开发土地产粮，产出低于口粮。修复：`SeedPopulation` 把聚落开发度折算为 0–1 写回地块。
3. **年份回绕**：`StateFormationSystem` 用 `currentDay`（每年回绕 1）算聚落年龄，永远不足 8 年；建国年龄与 `SettlementEvolutionSystem.foundingTick` 改为 `currentYear*365+currentDay` 绝对日。

### 5.3 验证

- 全量 EditMode：`EXIT=0`、`1003` 例、`1000` 通过、`0` 失败、零 `error CS`（2 个 Explicit 跳过）。
- 自然世界 100 年长跑：`NaturalEmergenceTests.NaturalLongRun_FormsRealmOfficesAndTaxReceipts`（2 seed × 100 年，Explicit）见 §5.4。
- 沙盒预置（`CreateStandard`）行为保持：3 政权、标准测试基线不变。

### 5.4 遗留（不在 P5 收口范围）

- 营地补给仍无来源（`CampData.Tick` 只消耗、不采集），营地→聚落路径实际难以自然发生；本轮自然建国走生成聚落，营地路径待补“采集/仓储”机制。
- `SandboxSpawnSystem.TrySeedPopulation` 仍是“随机地块凭空造人”，应改为从人口过载地块迁移。
- `PoliticsSimulationSystem` 的稳定度向 `50+centralization×20` 回归、`RaceData` 常数、聚落财富只减不增，仍是 D16 违反清单，另立里程碑。
- 货币化只做到“铸币阶段 + 市场真实需求卖非食用货物”；铸币/纸币发行、汇率、在途运输队列未做。
