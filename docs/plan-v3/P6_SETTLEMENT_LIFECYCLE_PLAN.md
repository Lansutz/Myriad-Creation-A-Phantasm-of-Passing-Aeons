# P6 聚落生命周期与人口迁移计划

> 接 P5 §5.4 与总纲 J1：P5 解决了“国家怎么自然出现”，但**人群本身的存续链仍是断的**——
> 营地只消耗补给、不生产；人口增长靠随机地块“凭空造人”；聚落财富/开发只减不增。
> 本计划只做“聚落生命周期闭合”这一条，不扩到完整市场经济。

---

## 0. 事实基线

| 事实 | 位置 | 判定 |
|---|---|---|
| `CampData.Tick` 每天 `supplies -= population*0.1*deltaDays`，没有任何补给来源 | `CampData.cs:92-124` | 营地通常一个月内饿死，`permanence` 永远到不了 100 |
| 营地补给消耗 0.1/人/日，而人口口粮是 0.01/人/日 | `CampData.cs:98` vs `EconomyManager.PersonFoodPerDay` | 两套食物单位，营地消耗是人口的 10 倍 |
| `SandboxSpawnSystem.TrySeedPopulation` 随机挑地块、无源凭空加 4–16 人 | `SandboxSpawnSystem.cs:52-80` | 违反 D16“有物质载体” |
| 聚落 `development/wealth` 只在毁灭时下降，没有生产/贸易/建设来源 | `SettlementDestructionSystem`、`SettlementSimulationSystem` | 聚落不会成长，只会衰败 |
| 聚落等级推进 `UpdateConstructionProgress/CanLevelUp/PerformLevelUp` 已有调用链 | `SettlementSimulationSystem.cs:140-160` | 可复用，不另建系统 |

---

## 1. 阶段

### P6.0 营地补给来自本地劳动（先做）

- `AgricultureYield` 抽出 `ComputeForLabor(tile, farmers)`；`Compute(tile)` 继续按地块农民数调用。
- `EcosystemSystem` 抽出 `HarvestForPeople(ref tile, people, days)` / `HarvestFishForPeople(...)`；原 `Harvest/HarvestFish` 改为按地块人口调用。
- `CampData.Tick`：
  - 把补给消耗统一为 `population * 0.01 * deltaDays`（与人口口粮同单位）；
  - 每日补给 = 营地人口在所在地块的农业劳动产出 + 狩猎 + 捕鱼；
  - 产出从地块植被/野生/鱼群真实扣减，不凭空生成；
  - 补给不足才扣士气/人口，长期不足才废弃。
- 验收：同一地块，有劳动产出的营地能活过 5 年并把 `permanence` 推到 100、演化成聚落；贫瘠无产出地块仍会废弃。

### P6.1 人口迁移替代随机造人

- `SandboxSpawnSystem.TrySeedPopulation` 改为：
  1. 找一个人口超过承载力 1.1 倍的源地块；
  2. 在源地块周围小半径内找一个可承载、无聚落冲突的目标地块；
  3. 从源地块人口块真实扣除一小群（约 5–10%），在目标地块加同文化/同阶层的新块；
  4. 找不到过载源/合适目标就不新增人口。
- 验收：迁移后总人口守恒；过载地块人口下降、目标地块上升；无过载源时不凭空加人。

### P6.2 聚落财富/开发来源（本轮尽量做）

- 聚落 `wealth` 增加来源：本地市场成交/贸易路线收益的留存（按实际货物成交量，不按抽象贸易力）。
- `development` 增加来源：人口增长与建设完成（`ConstructionPlan`/`SettlementBlock` 完成时 +）。
- 不做完整货币银行、不做每笔交易结算；只保证“有生产/交易/建设，聚落才会变富变发达”。

### P6.3 验证

- 新增 `SettlementLifecycleTests`：营地补给正反向、营地 5 年演化、迁移守恒/无源不造人。
- `build` 全绿；自然世界短跑（20–30 年）确认营地/聚落数量不再单调衰减。
- 文档：本计划 + `DECISIONS`（D142 起）+ `PROJECT`。

---

## 2. 非目标

- 不做完整市场价格/货币流通/银行；P6.2 只做“成交/建设留下财富”。
- 不做逐人迁移/迁徙路线寻路；只做“一小群人口从过载地块移到邻近可承载地块”。
- 不改 P5 的建国/官职/税收规则。

---

## 3. 实施记录（2026-11-14）

- **P6.0 营地补给**：`AgricultureYield.ComputeForLabor`、`EcosystemSystem.HarvestForPeople/HarvestFishForPeople` 抽出；`CampData.Tick` 改为“本地农业+狩猎+捕鱼 → 补给”，消耗统一 0.01/人/日。测试：有产出营地 2200 天后未废弃且 `CanEvolveToSettlement`；无产出营地 80 天内废弃。
- **P6.1 人口迁移**：`SandboxSpawnSystem.TrySeedPopulation` 改为过载源→邻近可承载目标，人口守恒；无过载源不新增。源扣减走 `PopulationLedger.RemoveFromIndex`（通过人口写入网关测试）。
- **P6.2 财富/开发**：商队实际入库货物价值的 5% 留成目的地聚落 `wealth`；`SettlementSimulationSystem` 按人口建设劳动推进 `development`。
- **验证**：`build` `EXIT=0`、`1005/1002`、零 `error CS`（3 个 Explicit 跳过）。
- **未做**：完整市场货币流通、迁徙路线寻路、营地仓储上限、财富消费端。
