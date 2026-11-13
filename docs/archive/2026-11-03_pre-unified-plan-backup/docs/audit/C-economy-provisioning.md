# 审计 C：经济 / 补给 / 金库链路一致性

- 审计员：economy-auditor（只读）
- 仓库：`D:\Myriad-Creation-A-Phantasm-of-Passing-Aeons`，分支 `architecture-2-0-refactor`（工作区大量未提交改动，本审计未做任何 git 写操作）
- 方式：纯静态阅读 + 行号取证；未运行 Unity、未编译、未跑测试、未修改 `MyriadCreation/Assets/**`
- 唯一写入：本文件
- 证据格式：`MyriadCreation/Assets/.../File.cs:行号`（行号以当前工作区版本为准）
- 总判定：**"国库付了口粮、队伍背包却没有粮食"基本属实，但比原线索更严重**——粮食链路存在 sentinel（0 号）语义冲突、结构体按值传参丢失选择结果、默认法律口径下"个人/家族出资"完全不扣款、以及口粮用量公式本身必然不足四层问题叠加。金库/市场还有若干守恒与静默失败。

---

## 0. 结论摘要

| 级别 | 数量 | 一句话 |
|---|---|---|
| P0 | 4 | 0 号物产语义冲突导致粮食既不扣也不进包；补给选择结果按值丢失；默认"个人出资"凭空造钱货；传令官口粮公式必然短缺 |
| P1 | 8 | 区域金库永远无法支付并静默降级；商队/仓储守恒缺口；城市缺粮无后果；国库双账本；补给不可见；军费只扣不出；驻军 NRE；保质期常量失真 |
| P2 | 8 | 动态物产与静态表双轨、载重不校验、退料非原子、死常量、双套市场、存档固化假账、消耗口径分散等 |

最重要的三件事（建议先修）：
1. **统一 0/哨兵语义**：内容表 `0 = 粮食`，但几乎所有 API 把 `<=0` 当"无效/未选择"。要么把哨兵改成 `-1` 并迁移存档，要么把粮食主键从 0 改走（推荐前者 + `GoodsId.Invalid = -1`），否则粮食永远进不了背包/金库。
2. **补给"选择结果"只允许有一个出口**：`UnitProvisioning.TryFund` 现在把选中的物产 id 写在按值传入的 cost 副本里，`Equip` 又用调用方原始 cost 覆盖 funding，造成"钱扣了、货没发"。
3. **默认法律口径不能免费**：`LawComposition` 默认 `nationalTax/localTax/militaryOrganization = -1`、`localSuccession = 0`，`ResolveFundingSource` 于是返回 `Personal`；`TreasuryLedger` 的 Personal/Household 分支不扣任何账户，`Equip` 却照常发货 → 每个传令官凭空得到货币货物（修好粮食后还会凭空得到粮食）。

---

## 1. P0

### P0-1 `0 号物产（粮食）`被当成"无效 id"，粮食既不扣款也不进背包

**证据链（详见第 4 章专章）**

- 内容表：`AddGoodsDef(0, "粮食", GoodsCategory.Food, 1f, 0.1f, ...)` — `MyriadCreation/Assets/Scripts/Simulation/MyriadCreation.Simulation.WorldState/GameWorld.cs:615`
- 选择器会把 0 选出来：`TreasuryLedger.SelectFoodGoodsId` 返回"第一个 Food 类物产"（遍历 `goodsDefs` 命中即 return）— `.../MyriadCreation.Simulation.Units/TreasuryLedger.cs:61-90`；`GameWorld.FindFoodGoodsId` 同样返回第一个 Food/Crop — `GameWorld.cs:2062-2072`
- 但承载侧把 0 判为非法：
  - `TransportUnit.AddCargo`：`if (goodsId <= 0 || amount <= 0f) return;` — `.../MyriadCreation.Simulation.Units/TransportUnit.cs:59-63`
  - `TransportUnit.RemoveCargo`：`goodsId <= 0` 直接失败 — `TransportUnit.cs:84-88`
  - `TreasuryVault.Get/Add/TrySpend`：`goodsId <= 0` 一律返回 0/false/不写 — `.../MyriadCreation.Simulation.Units/TreasuryVault.cs:14-18, 40-45, 66-75`
- 供给侧把 `<=0` 当"没选到"：
  - `UnitProvisioning.TryFund`：`if (cost.food > 0f && cost.foodGoodsId <= 0) cost.foodGoodsId = SelectFoodGoodsId(...)` — `.../MyriadCreation.Simulation.Units/UnitProvisioning.cs:73-76`（选中结果仍是 0）
  - `TreasuryLedger.TryPay`：`if (funding.foodGoodsId <= 0) funding.foodGoodsId = SelectFoodGoodsId(...)` — `TreasuryLedger.cs:159-162`
  - 国库扣粮条件：`if (cost.food > 0f && funding.foodGoodsId > 0 && !treasuryVault.TrySpend(...))` — `TreasuryLedger.cs:290`（`foodGoodsId == 0` 时整段跳过，**不扣粮、也不报错**，随后 `reason` 仍为 `""`，`funding.foodPaid = 0` — `TreasuryLedger.cs:311`）
- 发货侧同样跳过：`if (cost.foodGoodsId > 0 && cost.food > 0f) unit.AddCargo(...)` — `UnitProvisioning.cs:104-107`

**触发条件**：任何一次真实游戏内的补给（`SpawnDecreeCouriers` 是当前唯一生产调用点，`GameWorld.cs:2146-2163`）。`FindFoodGoodsId()` 命中 0 号（Dictionary 插入序首位，见 4.2 的"待验证"说明）。

**后果（资金/货物对账）**

| 环节 | 钱（币货 60 金/银） | 粮（物产 0） |
|---|---|---|
| 国家金库 `TreasuryVault` | 按设计被 `TrySpend(currencyGoodsId, money)` 扣减（`TreasuryLedger.cs:268-289`） | **不扣**（`:290` 条件为假，静默通过） |
| 区域金库 `TradeCenter.inventory` | 城/省库路径因粮 id 非法必然整单失败，降级国库（见 P1-1） | **不扣** |
| 队伍背包 `TransportUnit.cargo` | 若调用方 cost 里币货 id 有效（60）则进包（`UnitProvisioning.cs:108-111`） | **不进包**（`AddCargo` 拒绝 0） |
| `UnitFunding` 记账 | `moneyPaid = cost.money` | `foodPaid = 0`（国库路径）/ `foodPaid = cost.food`（个人路径，见 P0-3，是假账） |

净效果：**国库（或区域库）被扣了"钱"，队伍拿到了钱，但队伍没有口粮，金库也没被扣粮**；玩家看到 `TryFund` 返回 `true`、传令官成功生成，随后传令官沿途饿死（见 P0-4 的后果链）。

**额外不一致**：0 号物产在"市场侧"完全可用，只在"背包/金库侧"不可用：
- `EconomyManager.ProcessConsumption` 用 `RemoveGoods(0, amount)` 扣人口口粮 — `.../MyriadCreation.Simulation.Systems/EconomyManager.cs:206`
- `TradeCenter.UpdateSupplyDemand` 用键 0 建粮食供需 — `.../MyriadCreation.Simulation.State/TradeCenter.cs:89, 92`
- `CharacterSystem.DailyDiet` 用 `RemoveGoods(0, 1f)` 扣角色口粮 — `.../Assets/Scripts/Role/CharacterSystem.cs:1784`
- 而 `TransportUnit.TryAddCargo` 用 `goodsId < 0` 判断（允许 0）— `TransportUnit.cs:419-421`；`TryRemoveCargo` 用 `goodsId < 0`（允许 0）— `TransportUnit.cs:449-451`
  → 同一个 0 号物产，走 `TryAddCargo` 能进包、走 `AddCargo` 不能；走 `TryRemoveCargo` 能删、走 `RemoveCargo` 不能。**同类内部 API 语义都不统一。**

---

### P0-2 `TryFund` 结构体按值传参 + `Equip` 覆盖 funding：钱扣了、货没发

**证据**

- `UnitProvisioningCost` 是 `struct` — `.../MyriadCreation.Simulation.Units/UnitProvisioningCost.cs:5-33`
- `UnitProvisioning.TryFund(...)` 形参是 `UnitProvisioningCost cost`（按值），内部给 `cost.currencyGoodsId/foodGoodsId` 赋选中值 — `UnitProvisioning.cs:67-78`。这些赋值**只改局部副本**，调用方的 cost 不变。
- `TreasuryLedger.TryPay` 把选中结果写进 `funding.currencyGoodsId/foodGoodsId` 并按此扣款 — `TreasuryLedger.cs:139-162, 268-289`
- `UnitProvisioning.Equip` 随后又用调用方原始 cost **覆盖** funding 中正确的 id：
  ```csharp
  if (funding != null) { funding.foodGoodsId = cost.foodGoodsId; funding.currencyGoodsId = cost.currencyGoodsId; }  // UnitProvisioning.cs:99-103
  if (cost.foodGoodsId > 0 && cost.food > 0f) unit.AddCargo(cost.foodGoodsId, cost.food, unitWeight);                 // :104-107
  if (cost.currencyGoodsId > 0 && cost.money > 0f) unit.AddCargo(cost.currencyGoodsId, cost.money, unitWeight2);     // :108-111
  ```

**触发条件**：调用方传给 `TryFund` 的 cost 里 id 为 0（"请内部自选"），而内部成功选中并扣了款。`UnitProvisioningCost.ForCourier(distanceTiles)` 的默认参数就是 `foodGoodsId = 0, currencyGoodsId = 0` — `UnitProvisioningCost.cs:20-32`。当前唯一生产调用点 `SpawnDecreeCouriers` 恰好显式传入了 id（`GameWorld.cs:2146-2148`），所以币货没中招；但 `UnitProvisioning.TryFund(realm, payer, source, UnitProvisioningCost, ...)` 这组公开重载（`UnitProvisioning.cs:56-59`）以及未来任何"不传 id"的调用都会中招。

**后果**：`funding.moneyPaid = cost.money`（国库路径 `TreasuryLedger.cs:310`）记录"已付款"，金库确实减少，但背包 `GetCargoAmount(currencyGoodsId) == 0`。**这是独立于 0 号物产的第二类"扣了没给"。**

**修法方向（不改源码）**：`TryFund` 返回"实际生效的 cost"（`out UnitProvisioningCost effectiveCost` 或用 `ref`），`Equip` 只信 `funding` 里的 id（funding 是唯一事实源），不要反向覆盖。

---

### P0-3 默认法律口径 = `Personal` 出资，但 Personal/Household 分支不扣任何账户，`Equip` 照常发货

**证据**

- 默认法律组合：`localSuccession = new ComponentChoice(0)`、`nationalTax/localTax/militaryOrganization = new ComponentChoice(-1)` — `.../MyriadCreation.Simulation.State/LawComposition.cs:28, 36, 60, 62`
- `ResolveFundingSource` 对默认组合逐条落空，返回 `UnitFundingSource.Personal` — `UnitProvisioning.cs:11-45`（默认组合下 `num=0`、`flag2/flag3/flag4=false`，落到 `:44`）
- `TreasuryLedger.TryPay` 的 Personal/Household 分支：
  ```csharp
  case UnitFundingSource.Personal:
  case UnitFundingSource.Household:
      funding.moneyPaid = cost.money;
      funding.foodPaid = cost.food;   // ← 粮 id 为 0 也照样记"已付粮"
      return true;                    // ← 没有从任何账户扣一分钱
  ```
  — `TreasuryLedger.cs:165-169`
- 不扣款不是"漏写扣款"，而是**无款可扣**：payer 只留了 `payerCharacterId`（`UnitFunding.cs:10`），`CharacterData`/`RealmData` 侧没有可扣的个人钱包字段（本次检索 `CharacterData.cs` 无 money/wealth 字段；`RealmData.treasury` 是唯一国库账户，`RealmData.cs:29`）
- 随后 `Equip` 无条件把 `cost.food/cost.money` 变成真实货物 — `UnitProvisioning.cs:104-111`
- 默认组合恰好是 `Personal`（非城/省/国库），于是**每条法令传令官都免费获得一份货币货物**（粮食因 P0-1 暂时拿不到，修好 id 后也会免费拿到）。

**后果**：守恒被破坏（凭空造物），`funding.moneyPaid/foodPaid` 是假账，存档还会把它固化（`.../Infrastructure/Save/WorldEntitySaveData.cs:51, 77`）。这是"个人/家族出资"设计口径的真正断点：没有个人钱包/家族账本，却按"已付款"记账并发货。

---

### P0-4 传令官口粮公式必然短缺：`8 + 0.08×距离` 对 2/天消耗，只够 `4 + 0.04×距离` 天，而路程 ≥ 距离 天

**证据**

- 成本公式：`food = 8f + distanceTiles * 0.08f`，`money = 4f + distanceTiles * 0.04f`，`horses = 1` — `UnitProvisioningCost.cs:20-32`
- 每日消耗：`DailyFoodDemand = members*1 + animals*1 + carts*0.25 + boats*0.25` — `TransportUnit.cs:40`；传令官 1 名成员 + 1 匹马（`Equip` 里 `cost.horses` 全转为 `PackAnimalKind.Horse` — `UnitProvisioning.cs:112-115`）→ **2 食物/天**
- 行军速度：`movementProgress += moveSpeed(1) * deltaDays(1) * 地形修正(≤1) * 载具修正 * 领袖修正` — `.../MyriadCreation.Simulation.Actors/MapActor.cs:117`；`daysPerTick` 默认 1 — `GameWorld.cs:70` → **≤1 格/天，地形差时更慢**
- 所以口粮可撑 `(8+0.08d)/2 = 4+0.04d` 天，而行程至少 `d` 天。**只要路程超过约 4 天（4 格）就开始缺粮**；地形/道路差时阈值更低。
- 缺粮后果：`ConsumeFood` 返回短缺量；`MapActor.Tick` 中 `shortage > 0` → `health -= shortage*1.5`、`morale -= shortage*0.8`（`MapActor.cs:145-149`）；`health < 30` 后每 2 天损失 `ceil(population*0.05)` 人口（`MapActor.cs:151-172`），传令官 `population = 1`（`GameWorld.cs:2157`）→ 第一次脉冲即人口归零 → `IsAlive=false` → 法令 `Failed`，note `"传令官失联"`（`GameWorld.cs:2198-2204`）。

**量化**：即使修好 0 号物产，`food = 8+0.08d` 也只能让短缺推迟到第 5 天左右；健康从 100 降到 30 需约 23 天（每天 -3），再加 1 个 2 天脉冲 → **任何超过约 26~30 天路程的传令官必定死在路上**；4~30 天路程的传令官会带着残血抵达。治理半径超过 ~25 格的法令事实上不可达。

（注：`MapActor.supplies` 是另一条隐藏补给线：初始 100（`MapActor.cs:56`），每天 -0.5（`MapActor.cs:142`），200 天才耗尽，与背包食物双轨并存，见 P2。）

---

## 2. P1

### P1-1 城库/省库补给永远走不通，且静默降级国库、来源标注不变

- `TryPay` 先试区域库：`GetRegionalVault(world, regionId)` + `CanPayFromRegional(...)` — `TreasuryLedger.cs:170-180`
- `CanPayFromRegional` 对 `cost.food > 0` 要求 `funding.foodGoodsId > 0`，否则返回 false — `TreasuryLedger.cs:218-232`
- 传令官 `cost.food` 恒 > 0（`UnitProvisioningCost.cs:25`），而 `foodGoodsId` 被选成 0（P0-1）→ **区域库路径 100% 失败**，全部静默落到 `TryPayNational`（`TreasuryLedger.cs:181-185`）
- 降级后 `funding.source` 仍是 `CityTreasury/ProvinceTreasury`（`TreasuryLedger.cs:139-145` 初始化，之后不再改写）；只有 `reason` 在个别分支被写成"城库货物不足"（`:186-189`），成功分支把 reason 清空（`:183`）→ **账面上"城库付款"，实际上扣的是国库**。后果：拨款来源统计、AI/UI、法律组的财政归因全部失真，且区域库永远不减少（无法通过组队消耗地方库存）。

### P1-2 市场/仓储守恒：`TradeCenter.AddGoods` 部分入账被忽略、商队货物蒸发

- `TradeCenter.AddGoods`：容量不足时**先加剩余容量**再 `return false` — `TradeCenter.cs:45-57`（调用方拿 false 但货已部分入账，"失败"语义不可靠）
- 到货结算忽略返回值：`value.AddGoods(item.Key, item.Value);` — `EconomyManager.cs:173` → 超出容量的部分**凭空消失**（来源库在 `RemoveGoods` 时已全额扣除，`:131-133`）
- 商队路线丢失/起点不存在时直接 `_caravans.RemoveAt(num)`，**在途货物不返还任何库** — `EconomyManager.cs:155-166`
- 反方向（发车）有容量/存在性检查，但 `RemoveGoods` 与装车之间无事务；`num4 = Mathf.Min(100f, inventory*0.3f)` 之后若 `AddGoods` 部分失败，损失由目的地承担 — `EconomyManager.cs:131-144`
- `SettlementMarket` 同类接口更严格（`AddStock` 超容量**不加**直接 false，`SettlementMarket.cs:37-49`），说明两套市场语义不一致（见 P2-6）

### P1-3 城市缺粮没有任何后果（空循环），且口粮扣减口径分散

- `EconomyManager.ProcessConsumption`：
  ```csharp
  if (!value.RemoveGoods(0, amount))
  {
      foreach (PopulationBlock populationBlock2 in _tiles[i].populationBlocks) { _ = populationBlock2; } // 空循环，无任何惩罚
  }
  value.RemoveGoods(3, amount2);  // 盐，返回值也没人看
  ```
  — `EconomyManager.cs:204-214`。缺粮只"扣不到"，不产生稳定度/人口/阶级需求后果；唯一能看到缺粮的是角色 `DailyDiet`（`CharacterSystem.cs:1784-1794`，只影响 obesity/stress），与"城市饥荒"完全脱节。
- 口粮常量分散且不一致：人口 0.01/人/天（`EconomyManager.cs:204`、`TradeCenter.cs:92`）、角色 1/人/天（`CharacterSystem.cs:1784`）、军队 `UnitDef.supplyConsumption`（`Army.cs:113-123`）、队伍 1/人/天（`TransportUnit.cs:40`）、营地 `population*0.1*deltaDays`（`CampData.cs:97`）。同一世界五种"每人消耗"口径，UI/经济报表无法对齐。

### P1-4 国库双账本：`realm.treasury`（UI 显示）与 `currencySystem.vault`（实际扣款）镜像时序有洞

- UI 显示：`国库：{realm.treasury:F0}` — `.../Simulation.Presentation/RealmOverviewText.cs:24`
- 多个系统直接改 `realm.treasury`：建筑 `BuildingSystem.cs:162`、外交 `DiplomacyManager.cs:63`、军费 `MilitaryBuildUpCommandHandler.cs:27`、AI 等（另有约十余处，见检索）
- 只有 `TryPayNational` 会同步：先 `ReconcileNationalCurrency`（把 `realm.treasury` 覆盖进 vault，`TreasuryLedger.cs:92-121, 262-265`），扣完后 `realm.treasury = vault.CurrencyAmount(...)`（`:306-309`）
- 漏洞：
  - `realm.treasury < 0` 时 `ReconcileNationalCurrency` 直接 `return`（`:101-104`），而 `TryPayNational` 的 `flag = realm.treasury < 0f` 使镜像回写也被跳过（`:261, 306`）→ 此时 UI 显示负国库，vault 却仍可能保留旧的正余额，**下一次补给仍能按旧余额付款**；也就是"账面已破产，实际还能花钱"。
  - 食物与货币共用同一个 `TreasuryVault.goods` 列表；`realm.treasury` 只镜像货币物产（`:308`），金库里的粮不会出现在国库 UI。
  - 如果 `goodsDefs == null` 或找不到货币物产（`SelectCurrencyGoodsId` 返回 0），则走 `realm.treasury` 直接扣款路径（`:278-287`），完全绕过 vault — 两种扣款路径并存。

### P1-5 补给不可见：没有 UI/日志，"饿死"是纯静默

- `TransportUnit.FoodDays(...)` 定义了剩余天数（`TransportUnit.cs:149-164`），**全仓库无调用者**（本次检索唯一命中是定义）
- `ConsumeFood` 的 `out shortage` 在 `MapActor.Tick` 被丢弃：`transport.ConsumeFood(world?.goodsDefs, deltaDays, out var _)` — `MapActor.cs:145`
- `UnitFunding.moneyPaid/foodPaid` 无任何 UI/日志读取（仅存档，`WorldEntitySaveData.cs:51, 77`）
- 传令官死亡只写法令 note（`GameWorld.cs:2202`），没有事件总线消息；玩家看不到"为什么法令失败"。→ 所有 P0 都被"静默"掩盖。

### P1-6 军费预算只扣款、不产出（待验证消费者）

- `MilitaryBuildUpCommandHandler.Handle`：校验后 `value.treasury -= command.Budget;` 直接返回成功 — `.../MyriadCreation.Simulation.Warfare/MilitaryBuildUpCommandHandler.cs:17-29`（全文件 31 行，无任何单位/货物/装备产出逻辑）
- 若 `CommandResult` 被 AI/UI 消费后没有别处再处理 `military_budget_committed`，即"扣了钱什么也没发生"。**待验证**：需确认 `MilitaryBuildUpCommand` 的发起方与结果消费方（本次未逐层追 AI intent executor）。

### P1-7 驻军统计空引用（跨域，顺带发现，建议另立审计）

- `SettlementControlSystem.UpdateGarrisonInfo`：`value3.garrisonQuantity += (int)value2.GetTotalManpower(null);` — `.../Simulation.Systems/SettlementControlSystem.cs:55`
- `Army.GetTotalManpower` 对 `unitDefs` 直接 `unitDefs.TryGetValue(...)`，未判空 — `.../Simulation.State/Army.cs:78-89` → 只要有一支军队停在己方聚落格且 `unitCounts` 非空，每日 `settlement-control.daily`（`SettlementSchedule.cs:31`）必抛 `NullReferenceException`
- 持有该调用的 `SettlementControlRuntime.DailyTick` 由 `SettlementSimulationSystem.ControlDailyTick` 直接调用（`SettlementSimulationSystem.cs:59-62`），注册于 `SettlementSchedule.cs:31`；`GameWorld.GameTick` 外层未见 try/catch（`GameWorld.cs:2006-2012, 2236-2240`），`SimulationScheduler` 内也未检索到 catch → 可能每天打断调度器后续条目。**待验证**：真实运行时是否被更外层（Unity `Update`）吞异常。

### P1-8 保质期常量与内容表不一致

- 内容表声明 `spoilagePerDay`：粮食 0.2、肉 0.1、鱼 0.15、谷物 0.05（`GameWorld.cs:657-660`，字段来自 `MarkPerishable`）
- 日结实际：`value2.inventory[item] *= 0.999f;`（固定 0.1%/天，与物产无关）— `EconomyManager.cs:225`
- 后果：粮食"180 天保质期 / 0.2 日损耗"设定形同虚设，仓储损耗比策划值低 1~2 个数量级；不同物产的无差别衰减使价格/稀缺信号失真。

---

## 3. P2

1. **动态物产与静态表双轨，动态食物无法用于补给**：`DynamicGoodsRegistry.Register` 只写自己的静态字典（`.../MyriadCreation.Simulation.Economy/DynamicGoodsRegistry.cs:12-55`），`TreasuryLedger.SelectFoodGoodsId/SelectCurrencyGoodsId` 只遍历传入的 `goodsDefs`（`TreasuryLedger.cs:61-90`）→ 玩家/模组注册的 10000+ 食物永远不会被选作口粮；`Register` 对 `goodsId < 10000` 返回 `-1`（`:39-44`），调用方是否处理 `-1` 待验证。
2. **`Equip` 不校验载重**：用 `AddCargo`（无容量检查，`TransportUnit.cs:59-82`），且调用时队伍成员还没加入（`CreateActor` 只建空 transport，`MapActorManager.cs:257-277`；领袖在 `Equip` 之后才 `TryAddMember`，`GameWorld.cs:2173`），此刻 `BaseCapacity = members*20 = 0`（`TransportUnit.cs:42`）。补给量一旦超过后续载重，玩家再也不能 `TryAddCargo` 补货。
3. **建城退料非原子且忽略失败**：`TryConsumeFoundingMaterials` 逐项删除，中途失败不回收已删项（`TransportUnit.cs:668-684`）；`UnitFounding` 失败退料用 `unit.TryAddCargo(...)` 且忽略返回值（`UnitFounding.cs:44-47`）→ 载重不足时材料蒸发。
4. **死常量/硬编码**：`Army.ManpowerPerBlock`、`Army.ReinforceBatchRatio`（`Army.cs:31, 33`）全仓库无引用；`ReinforceUnit` 里直接写 `targetCount * 0.1f`（`Army.cs:160`）→ 策划改常量无效。`ReinforceUnit` 无游戏内调用者（仅定义）。
5. **马/车免费**：`Equip` 按 `cost.horses/carts` 直接 `animals.Add/Carts.Add`（`UnitProvisioning.cs:112-119`），没有任何账户/货物为此付费；传令官固定 1 匹马（`UnitProvisioningCost.cs:27`）。
6. **双套市场/生产**：`TradeCenter`（区域贸易，`EconomyManager` 驱动）与 `SettlementMarket`（聚落市场，`SettlementData.market`）并存且不互通；`SettlementProduction.TryCraft`（`.../Simulation.Systems/SettlementProduction.cs:9-38`）在 `Assets/Scripts` 内无游戏内调用者（仅存档/测试触达）；`TryCraft` 逐项 `TryTake` 并在中途失败时**不返还已扣输入**（`:28-34`）。
7. **存档固化假账**：`UnitFunding` 随 transport 序列化（`WorldEntitySaveData.cs:51, 77`），`moneyPaid/foodPaid` 与 `cargo` 之间没有一致性校验，读档后"已付但无货"继续存在。
8. **数值安全细节**：
   - `ReinforceUnit` 在 `manpowerCost == 0` 时 `num3 = 0`，随后 `if (num3 > availableManpower)` 不触发除法，但会**免费补员**（`Army.cs:161-167`）；`manpowerCost < 0` 时同样免费。无除零，但 `UnitDef.manpowerCost` 默认 0（`.../Simulation.Definitions/UnitDef.cs:33`）为隐患。
   - `Army.CheckBattleEnd` 已用 `originalManpower <= 0` 保护（`Army.cs:232-239`）；`CalculateCombatPower` 的 `supply < 30` 分支正常。
   - `TradeCenter.GetGoodsPrice` 对 `localSupply` 做了 `Mathf.Max(0.01f, ...)` 保护（`TradeCenter.cs:39`）；`SettlementMarket.GetPrice` 分母 `+1f` 保护（`SettlementMarket.cs:83`）——这两处无除零。
   - `MapActor.supplies` 与 `transport` 口粮双通道同时扣（`MapActor.cs:142-155`），`supplies` 初始 100/200 不一致（`MapActor.cs:56` vs `MapActorManager.cs:202`），两套"补给"会同时给惩罚。

---

## 4. 专章：0 号物产链路逐步对账

### 4.1 事实基线

| 事实 | 证据 |
|---|---|
| 内容表 `0 = 粮食`，Food 类，基础价值 1，重量 0.1，保质 180 天 | `GameWorld.cs:615` |
| 其余食物/作物：1 肉、2 鱼、3 盐、10 谷物 | `GameWorld.cs:616-619` |
| 货币物产：60 金、61 银（PreciousMetal）→ 币货 id 有效（60） | `GameWorld.cs:643-644`；`TreasuryLedger.SelectCurrencyGoodsId` `:30-59` |
| `FindFoodGoodsId()` 返回首个 Food/Crop，默认返回 0 | `GameWorld.cs:2062-2072` |
| `SelectFoodGoodsId()` 返回首个 Food，找不到返回 0 | `TreasuryLedger.cs:61-90`（`:63-66` 空表返回 0） |
| `AddCargo`/`RemoveCargo` 拒绝 `goodsId <= 0` | `TransportUnit.cs:59-63, 84-88` |
| `TryAddCargo`/`TryRemoveCargo` 允许 `goodsId == 0` | `TransportUnit.cs:419-421, 449-451` |
| `TreasuryVault` 全接口拒绝 `goodsId <= 0` | `TreasuryVault.cs:14-18, 40-45, 66-75` |
| `Equip` 用 `foodGoodsId > 0` 判断是否发货 | `UnitProvisioning.cs:104-107` |

### 4.2 生产路径（`SpawnDecreeCouriers`，唯一调用点）

```
GameWorld.SpawnDecreeCouriers (GameWorld.cs:2125-2180)
  ├─ FindFoodGoodsId()  → 0            (GameWorld.cs:2146, 2062-2072)
  ├─ FindCurrencyGoodsId() → 60        (GameWorld.cs:2147)
  ├─ ForCourier(distance, 0, 60)       (UnitProvisioningCost.cs:20-32)
  │    food = 8 + 0.08d,  money = 4 + 0.04d,  horses = 1
  ├─ ResolveFundingSource(realm)
  │    默认 LawComposition → Personal   (UnitProvisioning.cs:11-45; LawComposition.cs:28,36,60,62)
  ├─ TryFund(world, realm, null, source, regionId, cost, ...)  (GameWorld.cs:2151; UnitProvisioning.cs:61-78)
  │    cost 是 struct，按值传入 → 内部选中结果只改副本
  │    SelectFoodGoodsId(goodsDefs) → 仍是 0  (TreasuryLedger.cs:61-90)
  │    TreasuryLedger.TryPay:
  │       Personal 分支：不扣款，moneyPaid=money, foodPaid=food, return true  (TreasuryLedger.cs:165-169)
  │       或 National 分支：
  │         币货 60：Reconcile → TrySpend(60, money) → 扣钱  (TreasuryLedger.cs:256-289)
  │         粮：`cost.food>0 && funding.foodGoodsId>0` 为假 → 不扣、不报错  (TreasuryLedger.cs:290)
  │               funding.foodPaid=0                                   (TreasuryLedger.cs:311)
  │       或 City/Province 分支：
  │         CanPayFromRegional 因 foodGoodsId=0 返回 false              (TreasuryLedger.cs:218-232)
  │         → 静默 TryPayNational（来源仍标 City/Province）              (TreasuryLedger.cs:181-191)
  ├─ SpawnActor(Courier, ..., population=1)  (GameWorld.cs:2157; MapActorManager.cs:257-277)
  └─ Equip(mapActor.transport, cost, funding, goodsDefs)  (GameWorld.cs:2163; UnitProvisioning.cs:80-121)
       funding 的 id 被覆盖成 cost 的 id（粮 0、币 60）  (UnitProvisioning.cs:99-103)
       foodGoodsId=0 → 不 AddCargo（粮食丢失）           (UnitProvisioning.cs:104-107)
       currencyGoodsId=60 > 0 → AddCargo(60, money)     (UnitProvisioning.cs:108-111)
       horses=1 → animals.Add(Horse)（免费）            (UnitProvisioning.cs:112-115)
```

### 4.3 "谁扣了、谁没收到"对账表

| 账户/载体 | 货币（金/银 60） | 粮食（0 号） |
|---|---|---|
| 国家金库 `TreasuryVault`（国家路径） | **扣了**：`TrySpend(60, money)`（`TreasuryLedger.cs:268-276`）；`realm.treasury` 同步回写（`:306-309`） | **没扣**：`foodGoodsId=0` 短路（`:290`），`reason` 空，调用方看到成功 |
| 区域库 `TradeCenter.inventory`（城/省路径） | **没扣**：粮 id 非法导致整单 `CanPayFromRegional=false`（`:218-232`），降级国库 | **没扣**（连尝试都没有） |
| 个人/家族账户（默认路径） | **没有账户可扣**（`TreasuryLedger.cs:165-169`；CharacterData 无钱包字段） | **没有账户可扣**，且仍记 `foodPaid=cost.food` |
| 队伍背包 `TransportUnit.cargo` | **收到**（币 id 有效） | **收不到**（`AddCargo` 拒绝 0，`TransportUnit.cs:59-63`） |
| `UnitFunding` 账本 | `moneyPaid=cost.money` | 国库路径 `foodPaid=0`；个人路径 `foodPaid=cost.food`（假账） |
| 每日消耗 | — | `ConsumeFood` 找不到食物 → `shortage=2/天` → health/morale 下降（`MapActor.cs:145-149`） |

### 4.4 结论

1. **"国库付了口粮"在真实生产路径上并没有发生**：粮在 `TryPayNational` 的 `:290` 被跳过，账上 `foodPaid=0`。真正发生的是"钱被扣了、币货进了背包、粮完全缺席"。
2. **即使国库真的为粮付了钱，队伍也收不到粮**：因为 `Equip` 用 `foodGoodsId > 0` 判断，0 号粮永远发不出去。
3. **最底层的病根是 sentinel 冲突**：内容表用 0 做主键且 0 是合法粮食，而整个代码库用 0 表示"无/未选择/无效"。这不是单点 bug，是**跨层协议不一致**；只在某个函数里把 `<= 0` 改成 `< 0` 会立刻把"未初始化"误当成粮食，必须全链路统一。
4. **市场/人口/角色链路把 0 当合法粮食用**（`EconomyManager.cs:206`、`TradeCenter.cs:89,92`、`CharacterSystem.cs:1784`），与背包/金库链路相反 → 同一物产在不同子系统的可用性不一致，这是"0 号物产在市场里可用、背包不可用"的准确表述。
5. **待验证**：`FindFoodGoodsId`/`SelectFoodGoodsId` 返回 0 依赖 `Dictionary<int, GoodsDef>` 的枚举顺序。内容表按 0 首位插入（`GameWorld.cs:615` 起），.NET `Dictionary` 在无删除时通常保持插入序，故实际取到 0；但这本身是**顺序依赖的脆弱设计**（读档重建/删除后可能变），需运行验证或改为按显式优先级选粮（例如固定优先 0 粮食 → 10 谷物 → 1 肉 → 2 鱼）。

---

## 5. 建议修法伪代码（不改源码，仅方案）

> 目标顺序：先让"钱/货一致"，再让"账面可核对"，最后补 UI 与数值。

### 5.1 统一 sentinel（P0-1 / P0-4 的前提）

```csharp
// 方案 A（推荐，风险可控）：0 保留为"粮食"，把"无效"统一改成 -1
public static class GoodsId
{
    public const int Invalid = -1;
    public static bool IsValid(int id) => id >= 0;          // 0 是合法物产
}

// 1) 内容表：粮食仍为 0，但所有"默认/未选"用 -1
struct UnitProvisioningCost {
    public int foodGoodsId     = GoodsId.Invalid;   // 不再默认 0
    public int currencyGoodsId = GoodsId.Invalid;
}
// ForCourier(distance, foodGoodsId = GoodsId.Invalid, currencyGoodsId = GoodsId.Invalid)

// 2) 承载侧：AddCargo/RemoveCargo/Get/TrySpend 判定从 `<= 0` 改成 `< 0`
//    （并给 AddCargo 补容量校验或改成返回 bool / 调用 TryAddCargo）
//    TreasuryVault.Get/Add/TrySpend 同理

// 3) 选择侧：找不到时返回 GoodsId.Invalid，而不是 0
static int SelectFoodGoodsId(defs) {
    foreach (id in PreferOrder(0 /*粮食*/, 10 /*谷物*/, 1 /*肉*/, 2 /*鱼*/, 3 /*盐*/))
        if (defs.ContainsKey(id) && IsFood(defs[id])) return id;
    return GoodsId.Invalid;
}

// 4) 存档迁移：把旧存档里"默认 0"的 id 字段（可能表示"未选"）按上下文改写为 -1；
//    对 TreasuryGoodsStack/CargoEntry 中 goodsId==0 的既有条目要保留（它们是真粮食）。
// 5) 全库替换清单（本次已定位）：
//    - TransportUnit.AddCargo/RemoveCargo/IsFood 调用点
//    - TreasuryVault.Get/Add/TrySpend
//    - TreasuryLedger.canPay/Spend/TryPayNational 中的 `> 0`/`<= 0`
//    - UnitProvisioning.TryFund/Equip 中的 `<= 0`/`> 0`
//    - `RemoveGoods(0,...)`、`AddToDict(..., 0, ...)` 等硬编码粮食键（EconomyManager.cs:206,213；
//      TradeCenter.cs:89,92；CharacterSystem.cs:1784）改成 goods.foodId 或显式常量
//    - GameWorld.FindFoodGoodsId/FindCurrencyGoodsId 返回 GoodsId.Invalid
// 备选方案 B：内容表把粮食主键改为 100，0 永久保留为"无效"——改动面小，
//            但需要存档迁移 + 所有硬编码 0 的地方改 100，且丢弃"0 号是粮食"的既有约定。
```

### 5.2 补给只保留一个事实源（P0-2）

```csharp
// TryFund 返回实际生效的 cost；Equip 只信 funding，绝不反向覆盖
static bool TryFund(GameWorld world, ..., UnitProvisioningCost cost,
                    out UnitProvisioningCost effective, out UnitFunding funding, out string reason)
{
    effective = cost;
    effective.foodGoodsId     = EnsureGoodsId(cost.foodGoodsId,     SelectFoodGoodsId(defs));
    effective.currencyGoodsId = EnsureGoodsId(cost.currencyGoodsId, SelectCurrencyGoodsId(defs));
    if (cost.food > 0 && effective.foodGoodsId == GoodsId.Invalid) { reason = "缺少粮食物产"; return false; }
    if (cost.money > 0 && effective.currencyGoodsId == GoodsId.Invalid) { reason = "缺少货币物产"; return false; }
    if (!TryPay(realm, world, source, effective, out funding, out reason)) return false;
    funding.foodGoodsId     = effective.foodGoodsId;      // 唯一事实源
    funding.currencyGoodsId = effective.currencyGoodsId;
    return true;
}

static void Equip(TransportUnit unit, UnitProvisioningCost effective, UnitFunding funding, defs)
{
    // 只用 effective/funding 的 id；校验 funding.moneyPaid > 0 才发货
    // AddCargo(0, ...) 现在是合法的（见 5.1），粮食可进包
    // 发货失败（载重不足）必须返回 bool，并让 TryFund 失败/回滚
}
```

### 5.3 出资来源必须"有账户可扣"（P0-3 / P1-1）

```csharp
enum FundingResult { Paid, NoAccount, Insufficient, Fallback }

// 1) Personal/Household：必须有真实账户。二选一：
//    (a) 给 CharacterData/HouseholdData 增加 wealth 字段并扣款；
//    (b) 暂不实现就返回 NoAccount，让上层退回到城/省/国库或明确失败——绝不 return true。
case Personal:
    if (payer == null || payer.wealth + 1e-4f < cost.money) { reason = "个人无钱可付"; return false; }
    payer.wealth -= cost.money;  funding.moneyPaid = cost.money;
    // 粮同理（个人粮仓/家族仓储）或明确"个人只出钱不出粮"
    funding.foodPaid = 0;        // 不再假记 foodPaid=cost.food
    break;

// 2) 区域库失败降级国库时，必须显式记录来源变化，供 UI/统计读取：
if (regionalFailed) { funding.source = UnitFundingSource.NationalTreasury;
                      funding.actualSource = UnitFundingSource.NationalTreasury;
                      funding.fallbackReason = reason; }

// 3) 单事务：付款与发货要么都成功，要么回滚（见 5.2 的 Equip 返回值）
```

### 5.4 口粮用量（P0-4）

```csharp
// 用"需求×行程天数+缓冲"算口粮，而不是拍脑袋线性式
static UnitProvisioningCost ForCourier(float distanceTiles, int foodId, int currencyId)
{
    float days    = Mathf.Max(0f, distanceTiles) / 1f;          // 保守：1 格/天（地形差时再乘 >1）
    float demand  = 1f /*人*/ + 1f /*马*/;                       // 与 DailyFoodDemand 同源
    float food    = demand * (days + 3f) * 1.15f;               // 3 天缓冲 + 15% 损耗余量
    float money   = ...;                                        // 现有公式可保留
    return new UnitProvisioningCost { food = food, money = money, horses = 1,
                                      foodGoodsId = foodId, currencyGoodsId = currencyId };
}
// 或者：TryFund 成功后用 unit.FoodDays(defs) 断言 >= estimatedDays，否则拒绝生成并给出 reason
```

### 5.5 守恒/静默失败（P1-2 ~ P1-5）

```csharp
// TradeCenter.AddGoods：返回三态，调用方必须处理
enum AddResult { Added, Partial, Rejected }
AddResult AddGoods(int id, float amount)
{
    float capacityLeft = inventoryCapacity - GetTotalInventory();
    if (amount <= capacityLeft) { inventory[id] += amount; return AddResult.Added; }
    if (capacityLeft > 0)      { inventory[id] += capacityLeft; return AddResult.Partial; }
    return AddResult.Rejected;
}
// 到货结算：Partial 时把剩余货退回/挂账（或让商队继续持有），不得直接丢弃
// 路线丢失：把 cargo 退回 from 区域库并记日志

// ProcessConsumption：缺粮必须产生后果（人口/稳定度/阶级需求），且用数据粮食 id
if (!tc.RemoveGoods(foodId, amount)) {
    starvingTiles.Add(tile);  // → 稳定度下降 / 人口损失 / 触发饥荒事件 / 通知 UI
}

// 国库单一入口：禁止直接 `realm.treasury -= x`
static bool TrySpendRealm(RealmData r, float money, string reason) { ... }  // 内部统一操作 vault + treasury 镜像
static void SyncVault(RealmData r) { /* reconcile 后再回写，任何时刻两者一致 */ }

// 补给可见：MapActor 面板显示 FoodDays 与 funding；死亡时发布事件
events.Publish(new UnitStarvedEvent(actorId, shortage, daysRemaining));
```

### 5.6 数值/常量（P1-8 / P2）

```csharp
// 保质期用内容表
inventory[id] *= Mathf.Pow(1f - def.spoilagePerDay, deltaDays);
// 军队常量
ReinforceUnit(...) => 用 ReinforceBatchRatio，不用 0.1f 字面量
// 统一"每人每日口粮"常量（例如 GoodsBalance.PersonFoodPerDay = 0.01f），
// 市场/城市/队伍/Army 全部引用同一常量，UI 用同一口径换算
```

---

## 6. 最小复现思路（纯逻辑，不需运行 Unity）

> 以下为可直接翻译成 EditMode 测试的断言级复现，按优先级排列。

### R1：0 号物产被链路丢弃（P0-1）

```csharp
// Arrange
var world = new GameWorld(...);            // 触发 InitializeGoodsDefs
Assert.AreEqual(GoodsCategory.Food, world.goodsDefs[0].category);   // GameWorld.cs:615
var cost = UnitProvisioningCost.ForCourier(100f, 0, 60);            // 模拟 FindFoodGoodsId 结果
var realm = DefaultRealm();                // 默认 LawComposition
var transport = new TransportUnit(); transport.TryAddMember(1);
// Act
bool ok = UnitProvisioning.TryFund(world, realm, null, UnitFundingSource.NationalTreasury,
                                   /*regionId*/0, cost, out var funding, out var reason);
UnitProvisioning.Equip(transport, cost, funding, world.goodsDefs);
// Assert —— 当前代码的"病态但真实"结果
Assert.IsTrue(ok);
Assert.AreEqual(0, funding.foodGoodsId);
Assert.AreEqual(0f, funding.foodPaid);                 // 粮没付
Assert.AreEqual(0f, transport.GetCargoAmount(0));      // 粮没进包
// 若用 currency 走国库：金库减少、背包有币；粮始终缺席
```

### R2：结构体按值 + Equip 覆盖 → 钱扣了货没发（P0-2）

```csharp
var cost = UnitProvisioningCost.ForCourier(100f);   // 两个 id 都是 0，让 TryFund 自选
Assert.IsTrue(UnitProvisioning.TryFund(world, realm, null, NationalTreasury, 0, cost, out var funding, out _));
float goldBefore = TreasuryLedger.NationalVault(realm).Get(60);
UnitProvisioning.Equip(transport, cost, funding, world.goodsDefs);
// 期望：funding.currencyGoodsId == 60 且背包有币
// 实际：funding.currencyGoodsId == 0（被 Equip 覆盖）且背包无币 → 钱已扣、货丢失
```

### R3：默认个人出资凭空造钱（P0-3）

```csharp
var realm = new RealmData();                        // laws 默认 → ResolveFundingSource == Personal
Assert.AreEqual(UnitFundingSource.Personal, UnitProvisioning.ResolveFundingSource(realm));
float treasuryBefore = realm.treasury;
Assert.IsTrue(UnitProvisioning.TryFund(realm, null, UnitFundingSource.Personal, cost, out var f, out _));
Assert.AreEqual(treasuryBefore, realm.treasury);    // 没扣钱
UnitProvisioning.Equip(transport, cost, f, world.goodsDefs);
Assert.Greater(transport.GetCargoAmount(60), 0f);   // 却拿到了钱货 → 守恒破坏
```

### R4：口粮必然不足（P0-4）

```csharp
float d = 100f;
var cost = UnitProvisioningCost.ForCourier(d);
var unit = new TransportUnit(); unit.TryAddMember(1); unit.animals.Add(PackAnimalKind.Horse);
float foodDays = (cost.food) / unit.DailyFoodDemand;   // = 8/2 = 4 天（d=100 时 (8+8)/2=8 天）
Assert.Less(foodDays, d, "口粮天数必须 ≥ 行程天数，否则必饿");
// 再接 MapActor 模拟：每天 Tick(1f) → 断言 ~26 天内 health<30；>30 天路程 population==0 → IsAlive==false
```

### R5：区域金库永远无法支付（P1-1）

```csharp
var tc = new TradeCenter(); tc.inventory[60] = 1e6f; tc.inventory[0] = 1e6f;  // 有粮有钱
var cost = UnitProvisioningCost.ForCourier(50f, 0, 60);
Assert.IsTrue(UnitProvisioning.TryFund(world, realm, null, CityTreasury, /*region*/tc.regionId, cost, out var f, out _));
// 期望走城库；实际 CanPayFromRegional 因 foodGoodsId=0 恒 false → 扣了国库，f.source 仍是 CityTreasury
Assert.AreEqual(tc.inventory.GetValueOrDefault(60,0f), 1e6f);  // 城库一分没动
```

### R6：仓储/商队守恒（P1-2）

```csharp
var tc = new TradeCenter { inventoryCapacity = 100f };
bool ok = tc.AddGoods(0, 150f);        // 返回 false，但 inventory[0] = 100（部分入账）
Assert.AreEqual(100f, tc.GetTotalInventory());   // 调用方若只看 bool 会以为整单失败 → 重复处理/丢货
// 商队：from.RemoveGoods(0, 150) 成功后，to.AddGoods 部分失败 → 50 凭空消失
```

### R7：城市缺粮无后果（P1-3）

```csharp
tc.inventory.Clear();                 // 城市彻底没粮
economy.ProcessConsumption();         // 空循环分支（EconomyManager.cs:206-212）
Assert.DoesNotChange(() => tile.stability);   // 无任何惩罚 → 与"饥荒"设计不符
```

### R8：驻军空引用（P1-7）

```csharp
var army = new Army { currentTileIndex = s.tileIndex, ownerRealmId = s.realmId };
army.unitCounts[1] = 10f;             // 非空
Assert.Throws<NullReferenceException>(() =>
    SettlementControlSystem.DailyTick(settlements, tiles, w, h, new(){[1]=army}));
// 根因：Army.GetTotalManpower(null) —— Army.cs:78-89
```

### R9：现有测试为何没抓到（重要线索）

- `UnitProvisioningTests` 用 **id 1（食物）和 2** 构造 cargo：`parent.AddCargo(1, 90f)`、`unit.AddCargo(1, 2f)` — `.../Tests/EditMode/UnitProvisioningTests.cs:40-41, 61`
- 唯一覆盖 `TryFund` 的测试只测 money、不传 goodsDefs（`UnitProvisioningTests.cs:71-84`），于是走 `realm.treasury` 回退路径，恰好绕开 0 号物产
- 结论：**测试数据与生产内容表的 id 口径不一致，测试永远绿、线上永远饿**。补一条"用真实内容表 0 号粮食 + 真实 world.goodsDefs"的端到端测试即可稳定复现本报告 P0-1~P0-4。

---

## 7. 待验证 / 未覆盖

1. **`Dictionary` 枚举顺序**：`FindFoodGoodsId`/`SelectFoodGoodsId` 取"第一个匹配"，依赖插入序（`GameWorld.cs:615` 起按序插入）。无删除时 .NET 实际保持插入序 → 取到 0；但读档重建、模组插物产、删除后重插都会改变结果。需运行验证或改为显式优先级（见 5.1）。
2. **军费命令消费者**：`MilitaryBuildUpCommand` 的发起方与 `CommandResult` 消费方未逐层追（P1-6）。
3. **Legacy 双实现**：`Assets/Scripts/Legacy/Core/GameWorld.cs` 也有 `AddGoodsDef(0, "粮食", ...)`（`:1050`）与旧经济循环；本次审计以 `MyriadCreation.Simulation.WorldState.GameWorld` 为准，Legacy 是否参与编译/运行未确认。
4. **驻军 NRE 是否被外层吞掉**：未确认 Unity `Update` 或调度器更外层有无 try/catch（P1-7）。
5. **UI 面板**：仅检查了 `RealmOverviewText`（国库文本）与检索层面；具体部队/补给面板是否存在、显示哪个字段，未逐文件核对。
6. **动态物产 `Register` 返回 -1 的调用方处理**：未逐处追（P2-1）。

---

## 8. 验收对照

- 带证据结论：**P0 4 条 + P1 8 条 + P2 8 条 = 20 条**，全部附 `文件:行`。
- 可复现的守恒/数值错误或静默失败：**6 组**（R1~R8；含"个人出资免费造钱"、"仓储部分入账丢货"、"城市缺粮无后果"、"区域库恒失败静默降级"、"口粮必不足"、"驻军 NRE"）。
- 0 号物产链路：已逐步对账并给出"谁扣了谁没收到"表（4.3）与结论（4.4）。
- 未修改源码；唯一写入本文件。
