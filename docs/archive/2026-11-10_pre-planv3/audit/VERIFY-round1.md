# VERIFY-round1：Round1「8 项 P0 修复」独立只读复核

- **复核者**：verifier（只读）
- **复核对象**：Lead 依据 `docs/audit/A-save-persistence.md`、`C-economy-provisioning.md` 修复的 8 项 P0 + 7 条新测试
- **基线**：`architecture-2-0-refactor`，工作区 1360 个未提交改动；`MyriadCreation/Logs/editmode-results.xml` = `testcasecount=305, passed=305, failed=0, skipped=0`（start 2026-10-31T09:49:27Z）
- **快照时间**：2026-10-31 17:50–18:0x（Asia/Shanghai）
- **约束遵守**：未修改 `MyriadCreation/Assets/**`，未运行 Unity/编译/测试，git 只用只读命令，唯一写入本文件

## 0. 复核方法与被掩盖的基线问题（必读）

1. **`git diff` 只能覆盖 1/8 个修复文件。** 任务要求的 8 个文件中，只有 `Infrastructure/Save/SaveSystem.cs` 是 tracked（`git diff` 显示 191 增，混有 v11–v13 历史改动）；其余 7 个文件全部是 `??`（未跟踪），**无法用 git 区分"本轮改动"与"历史未提交内容"**。复核因此以「A/C 报告引用的 pre-fix 代码 + 当前源码」比对为准，逐条给出当前行号。
2. **305/305 可信度已复算**：`Logs/editmode-results.xml` 中 `total/passed/failed/skipped = 305/305/0/0`，`0` 处 `result="Failed"`。文件 mtime 证明全量测试跑在最终代码之后：`GameWorld.cs` 17:47:16 → `ProvisioningLedgerTests.cs` 17:48:52 → XML 17:49:27。Delta 与"新增 7 条"吻合（298 基线 + 7 = 305，D 报告基线为 298）。
3. **B 报告已在复核期间落盘**（`docs/audit/B-arch-recovery.md`，17:52），但其 P0（场景/程序集）不在本轮 8 项范围内，仅交叉引用未验证。
4. **未验证项一律标注**：无法运行 Unity，涉及运行时行为的结论为静态推演（见 §5）。

---

## 1. 逐项复核表

| # | 复核项 | 结论 | 关键证据（当前源码行号） |
|---|---|---|---|
| 1 | SettlementTypologySystem：新建聚落直接定型据点、显式 City 不降级 | **PASS-with-caveat** | `SettlementTypologySystem.cs:20-24`（form 守卫）、`:181-188`（bottleneck 时**仍改写 siteMode**）；调用方 `GameWorld.cs:2420/2424`、`AnchorGenerator.cs:138` |
| 2 | AnchorGenerator：用派生 siteMode 算等级 | **PASS-with-caveat** | `AnchorGenerator.cs:138`（先派生）、`:141`（**再用入参 type 覆盖 form**）、`:143-150`（等级用派生 siteMode） |
| 3a | `FindFoodGoodsId/FindCurrencyGoodsId` 返回 -1 | **PASS** | `GameWorld.cs:2097-2121`（返回 `UnitProvisioningCost.InvalidGoodsId`） |
| 3b | `PrepareLoadedWorld`（unitDefs/Faith/AI 补齐） | **PASS-with-caveat** | `GameWorld.cs:622-646`；`CombatManager` 与 `unitDefs` 共享字典实例 `GameWorld.cs:577` → 补填生效；`InitializeFaithSystems` 幂等 `:1020-1043`；`EnsureAIControllers` 幂等 `:638-646` |
| 4 | `MapUnitRegistry.AdoptNextId` 续接（A P0-4） | **FAIL（未生效）** | API 正确 `MapUnitRegistry.cs:29-36`；但 `SaveSystem.cs:259` 先 `RegisterArmyViews`，`:262-265` 才 `AdoptNextId` → 读档时军队视图仍从 1 分配、覆盖已还原地图单位 |
| 5 | SaveSystem 原子回滚（A P0-5） | **PASS-with-caveat** | 快照为深拷贝 `RelationRegistry.cs:141-154`、`ContractRegistry.cs:52`、`DynamicGoodsRegistry.cs:145`；`DestroyImmediate` `SaveSystem.cs:106-110`；但回滚只覆盖 3 个全局表，漏 `MapUnitRegistry`、`CharacterKnowledgeRegistry` |
| 6a | 0 号物产合法、无效=-1（全链路） | **PASS** | `TreasuryVault.cs:16-19/43-47/74-77`、`TransportUnit.cs:62/88`、`:423/453`、`UnitProvisioning.cs:70-77`、`TreasuryLedger.cs:155-173`；全库 grep 无残留 `goodsId <= 0` / `goodsId > 0` 判定（唯一命中 `DynamicGoodsRegistry.cs:41 def.goodsId < 10000` 是动态 id 分界，语义正确） |
| 6b | `Equip` 只信 funding | **PASS** | `UnitProvisioning.cs:87-92`（funding 优先，cost 仅兜底）；无回写 funding |
| 6c | Personal/Household 降级国库 + `funding.source` 自洽 | **FAIL（引入回归 R1）** | `TreasuryLedger.cs:176-181`（Personal→改 source 后走国库）；**但国家金库没有任何粮草来源** → `:302-317` 必然"国库粮草货物不足"；城/省库降级分支 `:182-203` 却**不回写 source**（自洽只做了一半）；UI `GovernmentPanel.cs:170-175` 不过滤 Failed，用户看到假成功 |
| 6d | `ForCourier` 口粮公式（C P0-4） | **PASS-with-caveat** | `UnitProvisioningCost.cs:28-42`（`food = 2×(d+3)×1.15`）；仍假设 1 格/天，地形 `1/movementCost` 减速时（`MapActor.cs:181-193`，`movementCost` 由 `GameWorld.cs:1745` 按 biome 给出）必然再度短缺 |
| 7 | 7 条新测试有效性 | **PASS-with-caveat** | `WorldGenerationSmokeTests.cs`（2 条）+ `ProvisioningLedgerTests.cs`（5 条）均出现在 XML 且通过；但都不覆盖 #4 与 #6c 的反例 |

### 1.1 关于"realms/wars 没有回滚"的结论（任务重点之一）

**不是污染源，无需回滚。** `_wars` 是新 GameWorld 的实例字段（`GameWorld.cs:106`），`realms` 也是实例字典；`ApplyToWorld` 对它们的 `Clear()/AddRange` 只作用于**新建世界**，旧世界实例完全不受影响。真正的污染面在**静态表**：

- `SaveSystem.cs:353-356`：`CharacterKnowledgeRegistry.Clear()/InstallKnownCharacters`（静态）
- `SaveSystem.cs:368`：`MapUnitRegistry.Clear()`（静态）
- 两处都发生在 `ApplyToWorld` 中段，之后还有多个可能抛异常/失败的步骤（`dto.ToUnit()`、`Equip`、`PrepareLoadedWorld`），而 catch（`:104-116`）只恢复 Relation/Contract/DynamicGoods → **读档失败时旧世界地图单位视图与角色知识已被清空且无法恢复**（P0-5 被缩小但未关闭）。

---

## 2. 发现的回归（按严重度）

### R1【P0 · 功能打死】默认法律下所有法令传令官必然失败（"国库粮草货物不足"）

**链路（静态推演，证据齐全）**

1. 默认法律组合 → `ResolveFundingSource` 返回 `Personal`（`UnitProvisioning.cs:11-45`，默认组合落 `:44`）。
2. `TryPay` 把 Personal/Household 改成国家金库：`TreasuryLedger.cs:176-181`。
3. 金库粮草支出：`TreasuryLedger.cs:302-317` 若 `TrySpend(foodGoodsId=0, cost.food)` 失败 → 退款 + `reason="国库粮草货物不足"` → `return false`。
4. `SpawnDecreeCouriers` 拿 false → `order.state=Failed; order.note=reason; continue`（`GameWorld.cs:2186-2191`）→ **发不出传令官**。

**关键事实：国家金库的粮草没有任何生产者。** `TreasuryVault.Add` 的调用点全库只有 `TreasuryLedger.cs:109/118（货币迁移）/308（退款）`；`EconomyManager.GetNationalVault` 0 调用（`EconomyManager.cs:294`）；`ReconcileNationalCurrency` 只把 `realm.treasury`（货币）迁入 vault（`TreasuryLedger.cs:92-121`）。区域库反而有粮（`GameWorld.cs:InitializeTradeCenters` 给 `inventory[0]=1000`），但默认法律路径不经区域库。

**为什么之前"能跑"**：修 P0-1 前 `foodGoodsId==0` 使 `foodGoodsId > 0` 条件为假，粮草检查被整段跳过（C 报告 `:290`），所以旧行为是"钱扣了、发个会饿死的传令官"。本轮把 0 合法化后，同一路径从"跳过"变成"必然失败"——**从静默错误变成功能不可用**。

**佐证**：`ProvisioningLedgerTests.cs:70-72` 自己手写 `nationalVault.Add(0, 100f)` 并在注释里写明"粮食需另行入库"——即测试作者已知生产路径没有粮。

**用户可见性（UI 消费侧）**：`Assets/UI/Panels/GovernmentPanel.cs:170` 在玩家颁布法令后调用 `world.IssueDecreeOrders`，`:174-175` 用 `orders.Count > 0` 显示"已下发 N 道命令"。而 `IssueDecreeOrders`（`GameWorld.cs:2051-2069`）**不过滤 Failed**：`SpawnDecreeCouriers` 即便全部失败（`order.state=Failed, note="国库粮草货物不足"`，`GameWorld.cs:2186-2191`），返回的 list 仍是满额 → **UI 显示成功、实际零传令官**。任务问的"UI 对 `funding.source` 的消费"实际不存在（全库无 UI 读 `funding.source`），但 UI 对**订单结果**的消费不自洽，属本回归的用户可见面。

**最小修复方向**（任选其一，均需补测试）：
- `TryPayNational` 粮草不足时回退到 `GetRegionalVault(world, regionId)`（区域库生成即有 1000 粮）；
- 或在 `SpawnDecreeCouriers` 前从区域市场/贸易中心调粮入库；
- 或临时把默认口径改为"粮草缺失仅告警但记录 foodPaid=0"（回到旧语义，但守恒破口仍在，不推荐）。

### R2【P0 · A P0-4 未生效】`AdoptNextId` 顺序错误，读档仍覆盖已还原的地图单位

`SaveSystem.cs` 当前顺序：

```
:256  RestorePersistentEntities(data, world);   // 内部 :368 MapUnitRegistry.Clear() → _nextId=1
                                                 // 按存档 id 回填商队/难民/野兽等（unitId>=1），_nextId 仍为 1
:259  ArmyPersistence.RegisterArmyViews(world);  // FromArmy 不设 unitId（MapUnit.cs:81-105）→ Add 从 1 分配
:262-265  MapUnitRegistry.AdoptNextId(nextUnitId); // 太晚
```

**后果**：只要有 ≥1 支军队且存档中最小非军队 unitId 为 1（生成局/正常游玩几乎必然），军队视图 `unitId=1` 直接覆盖同 id 的已还原条目；多条地图单位会按数量级丢失。`Add` 对显式 id 冲突无任何告警（审计建议的 `Debug.LogError` 未采纳）。非军队地图单位是常态而非边角：`MapActorManager.NaturalSpawning` 默认 true（`MapActorManager.cs:32/118`，`GameWorld.cs:918` 按 SetupOptions 打开），逃亡/盗匪/商队会持续注册视图。

**最小复现（EditMode 可实现，不需运行 Unity）**：生成世界 → `MapActorManager.SpawnActor(Caravan)`（得 unitId=1）→ `CreateArmy`（军队视图 unitId=2）→ `SaveGame` → `LoadGame` → 断言 `MapUnitRegistry.Count` 与 `(unitId, source, sourceId)` 集合不变 → 当前实现必失败（caravan 被 army 覆盖）。

**修法**：把 `AdoptNextId` 移到 `RegisterArmyViews` 之前（或在 `RestorePersistentEntities` 的 mapUnits 恢复循环结束后立刻推进游标），3 行改动。

### R3【P1 · 回滚不完整】读档失败后旧世界的 MapUnitRegistry / CharacterKnowledgeRegistry 被清空

见 §1.1。修法：把这两个静态表纳入 `LoadGame` 的快照/恢复（可复用 `AdoptNextId` 与 `SaveEntityBridge.CaptureKnownCharacters`），或把 `RestorePersistentEntities` 中段移到"校验通过之后"。

### R4【P1 · 自洽只做一半】城/省库降级国库时 `funding.source` 仍标 CityTreasury/ProvinceTreasury

`TreasuryLedger.cs:182-203`：区域库不足 → `TryPayNational` 成功，但 `funding.source` 未改回 `NationalTreasury`；对比 Personal 分支 `:180` 明确改了。影响：存档/统计中"资金来源"记载失真（`WorldEntitySaveData.cs:77` 持久化 `funding`）。**当前无 UI 消费方**（全库 grep `funding.source` 仅测试与 `TreasuryLedger` 自身），故非用户可见 bug，但属审计 P1-1 明列的"来源标注不变"未修。

### R5【P1 · 潜在语义反转】`UnitProvisioningCost` 的"未指定"默认值仍是 0，等于粮食

`UnitProvisioningCost.cs:19-21` 字段无初始化器（struct 默认 0），只有 `ForCourier` 默认参数改成了 `InvalidGoodsId`。因此 `UnitProvisioning.cs:49-53`：

```csharp
UnitProvisioningCost cost = new UnitProvisioningCost { money = money }; // currencyGoodsId=0!
```

`TryFund(realm,payer,source,money,...)` 重载 → `currencyGoodsId=0`（不再 `<0`）→ **把 0 号粮食当货币扣**。`UnitProvisioningTests.cs:72-84` 恰好因此"通过"：`ReconcileNationalCurrency(realm, 0)` 把 100 treasury 迁成 100 粮，再扣 40 粮，断言 `treasury==60` 成立——测试绿了，语义是错的。目前无生产调用方（唯一调用点是 `SpawnDecreeCouriers`，显式传 id），属潜在陷阱；建议字段初始化 `= UnitProvisioningCost.InvalidGoodsId`。

### R6【P2】P0-4 口粮公式仍按"1 格/天"

`UnitProvisioningCost.cs:30-36` 用 `days = distance` 且注释称"保守"；实际 `MapActor.cs:117` 的泥速 = `1 × 1/movementCost(≤1) × MoveSpeedMultiplier(≤1) × 领袖系数`，`movementCost` 由 biome 给出（`GameWorld.cs:1745`）。biome 成本 1.5 时 >10 格仍缺粮，山地（更高成本）更早断粮。相对旧公式（4 天必缺）是实质改善，但不是"必定够"。

### R7【P2】误导性死注释

`SaveSystem.cs:199` 仍写"AddComponent 时 GameWorld.Awake 已先跑 InitializeWorld…unitDefs 不入档，保留默认集"，与 `GameWorld.Awake` 为空（`:2037-2039`）、`:268 PrepareLoadedWorld()` 的新事实矛盾。A 报告 P0-1 明确指出这条注释是当初遗漏的成因，建议删除或改写。

### R8【P2】`MapUnitRegistry` 两处安全网缺失

- `MapUnitRegistry.cs:96` `if (num > 0) Remove(num)` 应为 `num >= 0`（当前 id 从 1 起不可达，属潜在）。
- `Add` 对已存在 id 静默覆盖（`:38-49`），审计建议的冲突告警未加——这正是 R2 无感发生的放大器。

### R9【P2】`PrepareLoadedWorld` 的"幂等"边界

- `InitializeUnitDefs`（`GameWorld.cs:771-798`）**无 Clear**：重复调用是"覆盖默认键"，不会删除运行时/模组新增兵种，但也无法重置被改坏的定义；对读档无害。
- `InitializeFaithSystems` 幂等（Clear+Dispose+new，`:1020-1043`）**但每次重置 `fervor=50`/圣地/大祭司**——信仰运行态仍未入档（A P1-3 开放）。若未来有人在运行时调用 `PrepareLoadedWorld`，会把宗教状态清空。注释"所有步骤幂等，重复调用安全"应加限定。
- 未调用 `_aiManager.SyncRulers(characters)`（审计 A P0-2 明确建议）：读档后 AI 人格随机，同一存档重读 AI 行为漂移。

### R10【info】双实现漂移：旧 `CivilizationEvolution.Map.SettlementTypologySystem` 未同修

`Scripts/Map/SettlementTypologySystem.cs`（旧程序集，操作 `BurgData`）与新 `Scripts/World/.../SettlementTypologySystem.cs` 同名双实现（B 报告 P2-1 的 262 组重复类型之一）。本轮只修新版本；若场景仍绑定旧 `MapEditor`（B P0-3），旧路径行为分叉持续。不属本轮 8 项，记录备查。

---

## 3. 测试有效性评价

**阳性（确实有效）**

- `WorldGenerationSmokeTests`：首次真正跑 `GenerateTerrain → SaveGame → LoadGame` 全链路；断言 `unitDefs.Count>0`（P0-1）、`AI 控制器数 == realms.Count`（P0-2）、`newArmy.armyId > maxArmyId`（army 游标）。这三条会真实失败，不是空转。
- `ProvisioningLedgerTests`：**没有绕开真实 `TryFund`**——`TryFund_ZeroFoodId_PaysAndEquipsFood` 走 `UnitProvisioning.TryFund → TreasuryLedger.TryPay → TryPayNational` 全链，断言 vault 实际扣减 + 背包实际到货；`Equip_UsesFundingGoodsId_NotCallerCost` 用 `funding` 与 `cost` 故意冲突验证事实源；`TryFund_FailsWhenNoFoodGoodsDefined` 验证 -1 错误路径。这 5 条质量较高。

**缺口（305/305 不代表 P0 全绿）**

1. **不覆盖 R2**：没有任何测试同时存在"非军队地图单位 + 军队"并比较读档前后 `MapUnitRegistry` 的 `(unitId,source,sourceId)` 集合（A 报告 P0-4 的最小复现思路原样留空）。305 绿完全不能说明 P0-4 已修。
2. **不覆盖 R1**：`ProvisioningLedgerTests.cs:70-72` 手工 `nationalVault.Add(0,100)`，把"生产环境国库没有粮"这一事实掩盖了；没有测试从 `IssueDecreeOrders`/`SpawnDecreeCouriers` 端到端断言"传令官真的生成了"。
3. **种子特例**：`WorldGenerationSmokeTests.cs:53-70` 硬编码 `seed=7 / 64x32` 并断言"必须有 FortifiedOutpost"——这是地形生成的实现细节，地形一变就脆断；它验证的是"该种子不崩 + 恰好有据点"，不是不变量式回归保护（价值真实但可持续性差）。
4. **静态表清理不全**：`WorldGenerationSmokeTests.cs:26-39` TearDown 清了 4 个注册表，**漏 `DynamicGoodsRegistry.Clear()`**（`LoadGame` 现在会全局 `Restore` 它）。`DynamicGoodsTests` 自己 SetUp/TearDown 会清，但 smoke 无 SetUp，执行顺序仍可能让"上一 fixture 的动态商品"被本测试存进档再灌回全局表。D 报告 P1-3 的"执行顺序依赖"未关闭。
5. **绿基线不可复现（D P0-1 仍成立）**：测试目录仍是 47 个未跟踪新文件 + 32 个文件/278 条旧测试的未提交删除；`305/305` 无法由 HEAD checkout 复现，review 难度未降低。
6. **未验证编译外的"测试真能失败"手段**：本轮未做 mutation 验证；上述缺口 1/2 是唯一两个"必然漏网"的 P0，其余断言可判为有效。

---

## 4. 建议（保持现状 / 立即修 / 回滚）

**立即修（不修则 P0 不能宣称关闭）**

1. **R2（3 行，最高性价比）**：`SaveSystem.cs` 把 `MapUnitRegistry.AdoptNextId` 提到 `ArmyPersistence.RegisterArmyViews` 之前；同时给 `MapUnitRegistry.Add` 加"id 已存在"告警。补 1 条 `MapUnitRegistry` 存读测试（R2 最小复现）。
2. **R1（必须决策，二选一）**：
   - 推荐：`TryPayNational` 粮草不足时回退区域库（`GetRegionalVault(world, regionId)`，生成局即有 1000 粮），并把实际支付方写回 `funding.source`；
   - 备选：在 `SpawnDecreeCouriers` 前显式从区域市场调粮入国库。
   加一条端到端测试：生成世界 → 默认法律 → `IssueDecreeOrders` → 断言 courier 被 spawn（`MapActors`/order.state ≠ Failed）。
   *不建议为省事回滚 P0-3 修复*：回滚会让"钱扣了货没发/凭空造货"的守恒破口回来；R1 的正确形态是补供给，不是放弃修复。
3. **R3**：把 `MapUnitRegistry`、`CharacterKnowledgeRegistry` 纳入 `LoadGame` 快照/回滚（或把 `RestorePersistentEntities` 挪到所有可能抛出步骤之后）。
4. **R5**：`UnitProvisioningCost` 两个 id 字段加 `= InvalidGoodsId` 初始化器（1 行），并把 `UnitProvisioningTests.Funding_FromNationalTreasury...` 的"货币被当成粮食扣款"改为显式传 `currencyGoodsId`，否则该测试持续掩盖语义错误。

**保持现状（复核通过，不动）**

- sentinel 统一（`<=0 → <0`、`InvalidGoodsId=-1`）全库扫描干净，无残留语义反转点；
- `Equip` 以 funding 为唯一事实源；
- `TryPayNational` 的失败退款逻辑（`:302-317`）；
- `DestroyImmediate` 半成品世界 + 3 表深拷贝回滚（方向正确，只需补 R3 两张表）；
- `PrepareLoadedWorld` 主体（unitDefs 共享引用生效、Faith/AI 补齐幂等）。

**回滚**：本轮 8 项中**没有需要整体回滚**的项。唯一"改坏>改好"的单点是 R1 的副作用，但修法是补链路而非回滚（见上）。

---

## 5. 待验证（不编造）

1. **R1 为静态推演**：未运行 Unity，未实际触发 `IssueDecreeOrders`。建议 Lead 用 EditMode 端到端测试确认（不需要 Play 模式，生成世界 + 默认法律即可）。
2. `City form + FortifiedOutpost siteMode` 混合态（复核项 1/2 的 caveat）在下游（法律解锁/建筑/征兵/AI 判定）的消费行为未逐一验证；只确认了等级按派生 siteMode 计算（`AnchorGenerator.cs:145-150`）。
3. `PrepareLoadedWorld` 未补 `InitializeGoodsDefs/InitializeDefaultRaces/Cultures/ApplyContentOverrides/CultureLineage.Apply`——v13 存档靠 `data.goodsDefs/races/cultures` 兜住（已核对字段存在），**v11 以前/外部手改档缺段时的降级行为未验证**。
4. B 报告的 P0（场景被 `_Recovery/0.unity` 覆盖、程序集绑定悬空）不在本轮范围，未复核；其 P0-5"双实现漂移"与本报告 R10 相关。
5. 旧程序集 `CivilizationEvolution.Runtime` 的 `SettlementTypologySystem` 是否仍被场景内旧 MapEditor/BurgData 实际调用，未验证。

---

## 附录 A：本次复核使用的只读证据命令

```bash
git status --porcelain                     # 目标文件均为 ??（未跟踪），仅 SaveSystem.cs 为 M
git diff --stat -- <8 个目标文件>           # 只有 SaveSystem.cs 有 diff（191+）
grep -rn "goodsId <= 0|GoodsId <= 0|goodsId > 0|... :" Scripts/ Tests/   # sentinel 残留：0 命中
grep -rn "foodGoodsId|currencyGoodsId" Scripts/ Tests/                  # 全量人工核对
grep -rn "TreasuryVault|\.vault|GetNationalVault" Scripts/              # 证明金库粮草无生产者
grep -rn "AdoptNextId" Scripts/ Tests/                                  # 仅 SaveSystem 一处
grep -c 'result="Failed"' Logs/editmode-results.xml                     # 0
```
