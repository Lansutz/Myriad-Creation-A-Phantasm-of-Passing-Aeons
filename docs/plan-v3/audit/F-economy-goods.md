# 经济 / 物资 / 生产 / 贸易 / 货币 / 税收 / 工艺 / 建筑 / 装备 / 阶层需求 审计（2026-11-09）

## 0. 范围

读取的文件（`MyriadCreation/` 相对路径，括号为行数）：

- `Assets/Scripts/Simulation/MyriadCreation.Simulation.Economy/`：`DynamicGoodsRegistry.cs`(189)、`ClassEmergenceEvents.cs`(74)、`ImproveRealmEconomyCommand.cs`(8)、`ImproveRealmEconomyCommandHandler.cs`(45)
- `Simulation.Systems/`：`EconomyManager.cs`(545)、`EconomySchedule.cs`(28)、`TaxSystem.cs`(58)、`CurrencySystem.cs`(67)、`ClassNeedsSystem.cs`(221)、`BuildingSystem.cs`(332)、`BuildingSchedule.cs`(26)、`BuildingAvailabilitySystem.cs`(439)、`BuildingAvailability.cs`(28)、`BuildingData.cs`(9)、`ConstructionPlanSystem.cs`(71)、`ConstructionPlanExecutor.cs`(21)、`ResourceAccess.cs`(90)、`ManpowerSystem.cs`(88)、`ManpowerSystem.cs`、`SettlementProduction.cs`(67)、`RealmSituationBuilder.cs`(205)、`SocietyManager.cs`(171)、`PoliticsSimulationSystem.cs`(147)、`SettlementSimulationSystem.cs`(88)
- `Simulation.State/`：`TradeCenter.cs`(143)、`TradeCenterDTO.cs`(85)、`TaxSystemDTO.cs`(65)、`ClassNeedReport.cs`(29)、`ClassNeedWeightTable.cs`(33)、`RealmData.cs`、`RealmDTO.cs`(181)、`ActiveBuilding.cs`(18)、`Army.cs`(421，经济相关段)
- `Simulation.Units/`：`TreasuryLedger.cs`(341)、`TreasuryVault.cs`(131)、`UnitProvisioning.cs`(129)、`UnitFounding.cs`(55)
- `Content.Definitions/`：`GoodsDef.cs`(49)、`GoodsRecipe.cs`(25)、`BuildingDef.cs`(18)、`CraftProcess.cs`(19)、`EquipmentDef.cs`、`InnovationUnlock.cs`、`InnovationDef.cs`
- `Content.Modding/`：`RecipeCatalog.cs`(140)、`GoodsPropertyCatalog.cs`(106)
- `World.Settlement/`：`SettlementMarket.cs`(85)、`SettlementEconomy.cs`(81)、`SettlementData.cs`（`GetOrCreateMarket` 段）
- `Infrastructure.Save/`：`SaveSystem.cs`(554，读 340–479)、`SaveData.cs`(78)、`SettlementMarketSaveData.cs`(54)、`GameSaveData.cs`(94)
- `GameWorld.Core.cs`(995，读 515–870)、`GameWorld.Simulation.cs`(1225，读 40–170/585–900)、`GameWorld.Generation.cs`(585，读 305–344)、`CultureStageEvolutionHost.cs`、`AIDecisionSystem.cs`(334)
- 数据：`Assets/StreamingAssets/Base/{Goods,Crafting,Equipment,Properties,Innovation}/*.json`
- 测试：`Assets/Tests/EditMode/` 中 12 个经济相关文件（`EconomyConservationTests`281、`EntityRelationAndLivingGoodsTests`283、`DynamicGoodsTests`125、`SettlementMarketTests`87、`SettlementEconomyTests`80、`SettlementFoundingAndMarketSaveTests`81、`RecipeGraphTests`66、`RecipeGateTests`56、`RecipeTests`55、`RecipeSystemTests`51、`ShipDesignTaxonomyTests`39、`MilitaryLawTaxonomyTests`37）
- UI：`Assets/UI/` 全目录（6 个面板 + Common + Presentation）

未覆盖：`SettlementControlRuntime`/`SettlementEvolutionRuntime` 内部财政细节（只确认它们不写 TradeCenter/市场）；`Navy.cs` 舰队经济（属海军域）；`ClassEmergenceEvents` 门控细节；法律系统的税种法律效果（属法律域，仅在此核对乘子消费点）。

## 1. 系统清单

| 系统 | 三态 | 驱动证据（file:line） | 入口证据 | 存档证据 | 测试 | 一句话现状 |
|---|---|---|---|---|---|---|
| 经济日结（容量/供需/路线/发车/商队/消耗/保质） | 在跑 | `EconomySchedule.cs:21-25` → `GameWorld.Simulation.cs:797` | 无玩家经济操作 UI；AI 走 `AIDecisionSystem.cs:169-176`（仅 treasury−50/稳定+5） | `SaveSystem.cs:385` 存 `TradeCenterDTO`（含 inventory），`:432` 恢复 | `EconomyConservationTests` 直接驱动子步骤 | 日结是真的，但**只有消耗没有生产**（见 §4-1） |
| 物资生产（物资入流） | **不存在** | 全库写 `TradeCenter.inventory` 只有 3 处：生成播种 `GameWorld.Core.cs:842-843`、商队到货 `TradeCenter.cs:64`、读档 `TradeCenterDTO.cs:72` | — | — | 无 | 没有任何"地块/建筑/人口 → 产出入库"的代码路径；`GoodsDef.processedFromId` 无读者 |
| 贸易发车 / 市场 / 价格 | 空转 | `EconomyManager.cs:152-213`（遍历 `tradeRoute`）、`:140-149`（效率）在日结内 | 无 UI；AI 不参与 | `TradeCenterDTO.cs:34/68` 直传 `tradeRoutes` 列表（可入档） | `EconomyConservationTests:141/175-176/195/219` 是**唯一** `new TradeRoute` 的地方 | `TradeRoute` 无任何运行时创建者 → 发车循环永远遍历空列表，价格公式只有测试消费者 |
| 加工链 Recipe / Crafting | 空转 | `RecipeCatalog.cs`（加载 43 条，`ContentRegistry.cs:116`）；执行器 `SettlementProduction.cs:12-65` | 无 | 配方是内容数据，不入档 | `RecipeTests`/`RecipeGraphTests`/`RecipeGateTests`/`SettlementMarketTests:68` | 唯一执行器 `SettlementProduction.TryCraft` 只被测试调用（grep 命中 9 处，全在 `Assets/Tests/`） |
| 货币四阶段 / 通胀 / 劣币 | 空转 | `CurrencySystem.cs:62-65` 由 `EconomyManager.cs:125` 驱动，但驱动的是 `GameWorld.Core.cs:551` 的**全局副本** | 无 UI | `RealmDTO.cs:40/88/145` 存政权 `currencySystem`（含 vault） | 无任何测试 | `currentStage` 无写入者；`DebaseCoin:40`/`IssuePaperMoney:46` 零调用者；`inflationRate` 只在被丢弃的全局副本上日减 |
| 税收结算 | 半跑 | `PoliticsSimulationSystem.cs:86-87`（日结、法律乘子、入 treasury） | 无税率 UI/AI 调整（`AIDecisionSystem.cs:184-191` 是空桩） | `RealmDTO.cs:38/87/144` 存 `TaxSystemDTO` | 无 TaxSystem 专项测试 | 钱确实进了 realm.treasury；但税率取自全局 `TaxSystem`（`EconomyManager.cs:485`），不是政权 `realm.taxSystem` |
| 阶层需求（8 维度打分） | 在跑 | `SocietyManager.cs:87` ← `PoliticsSimulationSystem.cs:139`（日结） | `SocietyPanel` → `ClassPanelText.cs:131-134`（主要不满） | 报告是瞬时值，不单独入档（`RealmSociety` 每次重建，`LawEffectRuntime.cs:103-105` 注释明说） | `SocialClassBridgeTests` | 打分链条完整：需求→`unrestScore`→派系/政权更迭；输入是聚合评分（粮食安全、税痛感），非真实消费 |
| 建筑（29 条定义 + 施工） | 空转 | `BuildingSchedule.cs:20-23` → `GameWorld.Simulation.cs:798` 在跑 | 无（`BuildBuilding`/`StartPlannedBuilding` 零调用者，grep 命中仅定义处） | `tile.buildingLevels` 随 `TileData` 入档；`_tileBuildings` **不入档** | 仅 `ArchitectureContractsTests.cs:33` `new BuildingSystem(new TileData[0])` | 施工队列永远是空字典；`buildingLevels` 全 0（只由永不执行的 `OnBuildingComplete:250-262` 写） |
| 建筑可用性（30 个 BuildableType 地形门禁） | 空转 | `BuildingAvailabilitySystem.cs:290-407` 无 tick | 唯一消费者 `SettlementAISystem.cs:17`，而 `SettlementAISystem` 自身零调用者 | — | 无 | 439 行地形/坡向门禁无人调用；与上表的 `BuildingDef` 是**两套互不相通的建筑分类表** |
| 建造计划（计划→执行→完工） | 半跑 | 执行器已挂 `PlanSystem`（`ConstructionPlanSystem.cs:25`），`PlanSchedule` 日结 `GameWorld.Simulation.cs:838` | `GameWorld.Core.cs:324 CreateConstructionPlan` 零调用者 → 无计划产生 | `PlanSystem` 纯内存（`PlanSystem.cs:11`），`SaveData.cs` 无 plans 字段 | `PlanSchedule` 有测试；建造无 | 管道通电但**没有水源**：无人创建 Construction 计划 |
| 资源开采门禁（深度/技术） | 半开 | `ResourceAccess.cs:47/56` 仅经 `InnovationQueryAdapter.cs:133/139` 暴露为读模型契约 | `IsTechAccessibleForGoods` 在 UI 无调用者 | — | `ResourceDepthTests`/`ResourceTierCapabilityTests` | 有真实革新校验逻辑，但只到达读模型接口，无消费者 |
| 人力池 | 空转 | `ManpowerSystem.cs:31/63/78` 无 tick | 零生产调用者（grep：只有 `LawEffectRuntimeTests.cs:275-282`） | — | 1 处（法律乘子） | 无调用者；`GetRealmTotalManpower(...currentDay)` 法律乘子重载无人用 |
| 军事—经济硬联动 | 半跑 | `GameWorld.Simulation.cs:671-686` 传令官补给真扣物产（`TreasuryLedger.cs:134-339`） | 无玩家入口 | `RealmDTO` 存 vault/treasury | `ProvisioningLedgerTests` | 只有"传令官"一条链真扣货；兵种 `recruitCost` 零消费者；`Army.supply` 是抽象 0–100 |
| 装备目录（23 条） | 空转 | `ContentRegistry.cs:288` 加载 | 唯一门禁 `InnovationTree.cs:260 AreItemInnovationsMet` | 内容数据 | `EquipmentCatalogTests`/`ItemInnovationGateTests` | 门禁函数零生产调用者 |
| 动态物资（合金合成） | 空转 | `DynamicGoodsRegistry.TryCreateAlloy` 零生产调用者 | 无 | `SaveSystem.cs:375/434` 有 Capture/Restore | `DynamicGoodsTests` | 只有存档往返是活的 |

## 2. 数据模型现状

**真实存量流的位置**：`TradeCenter.inventory`（`TradeCenter.cs:18`，`[NonSerialized] Dictionary<int,float>`）+ `TradeCenter.inventoryCapacity`（`:20`）。存量结算确实走"加/减+容量判定"：`AddGoodsReturningAccepted:52-66`、`RemoveGoods:81-90`、`GetTotalInventory:92-100`。

**消费侧（真实）**：
- 口粮：`EconomyManager.cs:419` `num*PersonFoodPerDay`（0.01/人/日）、盐 `:420`（0.002/人/日），按 `tile.regionId` 找库（`:414-418`），不足则 `ApplyFaminePenalty:451-455`（稳定度−0.2、秩序−0.1）
- 角色口粮：`CharacterManager.cs:591-592`（每角色每日从区域库扣 1 粮）
- 保质：`EconomyManager.cs:457-473`——**所有 `hasShelfLife` 物产统一 ×0.999/日**，不使用 `GoodsDef.shelfLifeDays`/`spoilagePerDay`
- 军需：`TreasuryLedger.cs:262-280` 从区域库/国库扣货币+粮

**生产侧：不存在**。`TradeCenter.inventory` 的写入者只有播种（`GameWorld.Core.cs:842-843`，每区域 1000 粮 + 1000 高值金属）、商队到货（`EconomyManager.cs:309` → `TradeCenter.cs:64`）、读档（`TradeCenterDTO.cs:72`）。`GameWorld.Core.cs:684-692` 只给 `GoodsDef` 挂属性，不产出入库。因此每区域食物存量单调下降，约 `人口×0.01/日/地块` 的速度走向 0；叠加 `GameWorld.Simulation.cs:88-101` 的 Famine 事件门槛（`库存 < 人口×0.01`），后期会长期触发。

**承诺未兑现 / 半兑现字段**（写者有、读者无或只有默认值）：

| 字段 | 写者 | 读者 | 现状 |
|---|---|---|---|
| `TaxSystem.miningTax`/`saltMonopolyTax` | 字段初始化 `TaxSystem.cs:19/27`；DTO `TaxSystemDTO.cs:18/26` | **无** | 从不进入税率公式（`:41` 只用 agri/head/trade/craft/wartime） |
| `TaxSystem.luxuryTax`/`livestockTax` | `TaxSystem.cs:23/25` | 仅 `GetTaxSatisfactionImpact:52`（满意度） | 不进税收 |
| `TaxSystem.taxExemptions` | 仅 DTO 恢复 `TaxSystemDTO.cs:70-72` | `CalculateTileTax:36` | 运行时**无任何写入者** → 恒为空；且标 `[NonSerialized]`(`:32`) |
| `BuildingDef.materialCost` | `BuildingSystem.cs:104` | **无** | 建造只查/扣 `buildCost`（`:123/162`），不耗建材 |
| `UnitDef.recruitCost` | `GameWorld.Core.cs:790-801` | **无** | 兵种合成不消耗物资 |
| `GoodsDef.processedFromId`/`processingRatio` | `GameWorld.Core.cs:755-756` | **无** | 加工链只走 `Recipes.json`，这两字段无读者 |
| `GoodsDef.spoilagePerDay`/`isPerishable` | `GameWorld.Core.cs:699-700` | **无**（`ProcessShelfLife` 读 `hasShelfLife`） | 腐烂参数无效 |
| `ClassNeedsSystem.GrievanceThreshold`(const 45) | `ClassNeedsSystem.cs:34` | **无**（`ReasonIfLow:135` 用字面量 45） | 常量无消费者 |
| `InnovationDef.unlockRefs` | JSON→`InnovationDef.cs:72` | **无运行时读者**（只有 `InnovationUnlockRefTests`） | 见 §3 |
| `CurrencySystem.goldReserve`/`silverReserve`/`coinPurity`/`paperMoneyInCirculation`/`inflationRate` | 仅 `DebaseCoin:40-44`/`IssuePaperMoney:46-60`（两者零调用者） | `GetCurrencyValue:28-38` | 恒为初值 |
| `SettlementMarket.demand`/`supply` | 仅存档往返 `SettlementMarketSaveData.cs:43-46` | `GetPrice:69-84`（无调用链） | 无产出者 |

**双账本/双实例问题（事实，不含建议）**：
- 货币：`GameWorld.Core.cs:551` 造一个全局 `CurrencySystem` 交给 `EconomyManager`（`:553`），日结衰减的是它（`EconomyManager.cs:125`）；而政权实际使用/入档的是 `RealmData.cs:61 realm.currencySystem`，两者不是同一对象。
- 税收：`EconomyManager.cs:485` 用构造注入的**全局** `TaxSystem`（`GameWorld.Core.cs:552`）算税，政权自己的 `realm.taxSystem` 只用于满意度（`GameWorld.Simulation.cs:118`）与特权度（`RealmSituationBuilder.cs:167`）——两套税率可各自为政，且全局那套无人能改（`GetTaxSystem():525` 零外部调用者）。
- 国库：`realm.treasury`（`RealmData.cs:33`，初值 1000）与 `realm.currencySystem.vault`（`TreasuryVault`）双记；`TreasuryLedger.cs:90-119 ReconcileNationalCurrency` 用 `legacyCurrencyMigrated` 做一次性迁移，之后按 `treasury − vault` 补差。

**税收实际计算**（`EconomyManager.cs:476-489`）：
- 税基 `baseOutput = fertility × development × 10`（`:483`）——**不含人口、不含控制度**（`TileData` 无 control 字段）
- 稳定度乘子 `0.3 + stability/100×0.7`（`TaxSystem.cs:40`）；总税率按 `:41-42` 压缩（>0.3 部分衰减 50%）
- 阶层只用于"主导阶层免税判定"（`GetDominantClass:491-513`）
- 法律年度乘子确实接上：`PoliticsSimulationSystem.cs:86` `LawEffectRuntime.GetTaxMultiplier`（`LawEffectRuntime.cs:69-72`）；`laws.effects.yearly` 只结算 stability/legitimacy/unrest（`:84-108`），不改税率字段

**建筑**：`BuildingSystem.cs:32-92` 共 **29** 条 `AddBuildingDef`（README 称 30），tier 1–4（404/503/603），`buildDays = tier×10`（`:105`）。每地块上限硬编码 5（`:131`）。完工只改 `buildingLevels[category]` + 6 类地块属性（`:250-288`），其中 `Craft` 分支为空实现（`:285-286`）。

**军事—经济**：`Army.cs:123-137` 每日 `supply -= Σ(count × supplyConsumption)`，`supply` 是 0–100 抽象值（`:116-119` 低补给降战力），**不访问任何物产**。`Army.ReinforceUnit:163-195` 只用 `manpowerCost`。真正扣物产的只有传令官（`GameWorld.Simulation.cs:671-686`）与建国建材（`UnitFounding.cs:17-53`，但该函数零调用者）。

## 3. 内容数据现状

| 目录/文件 | 条目数 | 代码引用 | 运行时被引用比例 |
|---|---|---|---|
| `Base/Goods/GoodsProperties.json` | 29 entries（幂等统计：`entries` 数组 29） | `GoodsPropertyCatalog.Initialize`（`ContentRegistry.cs:115`）→ `GameWorld.Core.cs:686` 挂到 `GoodsDef.properties`；`InnovationTree.cs:162` 读属性做门槛 | 29/29（加载期全量挂载；其中 1 个消费者是革新软门槛） |
| `Base/Crafting/Recipes.json` | 43 recipes；`requiredInnovation>0` 34 条、`requiredBuilding>0` 1 条 | `RecipeCatalog.Initialize`（`ContentRegistry.cs:116`） | **运行时 0/43**：唯一读者 `SettlementProduction.cs:32/36/42/46` 只被测试调用 |
| `Base/Equipment/Equipment.json` | 23 equipment（category 1:7 / 2:2 / 3:2 / 4:1 / 5:7 / 6:3 / 7:1） | `ContentDefinitionIndex.cs:278-288` → `ContentRegistry.Equipment` | **运行时 0/23**：门禁 `InnovationTree.cs:260` 只被 `ItemInnovationGateTests` 调用 |
| `Base/Properties/PropertyDefinitions.json` | 5 properties | `GoodsPropertyCatalog.cs:87` 加载期校验 | 5/5（校验期消费） |
| `Base/Innovation/Innovations.json` | 186 innovations，`unlockRefs` 共 120 条：Good 24 / Unit 6 / Building 29 / Law 30 / **Recipe 31** | 解析进 `InnovationDef.unlockRefs`（`InnovationDef.cs:72`） | **运行时 0/120**：无代码读 `unlockRefs`；`GameWorld.OnInnovationCompletedHandler:257-268` 只写编年史 |
| 代码内 GoodsDef | **57** 条（`GameWorld.Core.cs:603-659`），19 个 `GoodsCategory` 枚举值 | 内置硬编码 | README 声称"22 种物资、11 大类"，代码实为 57 种 / 19 类 |
| 代码内 BuildingDef | 29 条 | `BuildingSystem.cs:32-92` | 无生产者写入 `_tileBuildings` |
| 代码内 BuildableType | 30 条（`BuildingAvailabilitySystem.cs:29-281`） | 仅 `SettlementAISystem.cs:17` | 0/30 |

解锁链结论（只判有无消费者）：`recipe.requiredInnovation` 的引用是**活的**（`SettlementProduction.cs:42` 会问 `InnovationTree.HasInnovation`），但该路径运行时不可达；`equipment.requiredInnovation` 同理（`InnovationTree.cs:263`）；`goods.requiredInnovation` 走 `ResourceAccess.IsTechAccessible:47-54`，只到读模型契约即断；`unlockRefs`（120 条，含 31 条 Recipe 引用）**没有任何运行时消费者**。

## 4. 缺口清单（按影响排序）

| # | 缺口 | 影响 | 证据 | 严重度 |
|---|---|---|---|---|
| 1 | 物资无生产入流，只有消耗与衰减 | 每个区域粮食从 1000 单调递减到 0；此后 `Famine` 事件按地块长期触发（人口×0.95/次），世界必然饿死；贸易只是把存量搬来搬去，总量仍减 | `EconomyManager.cs:395-443`（消耗）、`GameWorld.Core.cs:842-843`（唯一入流=播种）、无任何"产出→`AddGoods`"路径 | P0 |
| 2 | 贸易路线无创建者 | `InitiateTrades`/`CalculateEfficiency`/`MoveCaravans` 整条商队管道空转；价格只存在于公式里 | `EconomyManager.cs:140-213`；`where TradeRoute` 构造仅 `Assets/Tests/EditMode/EconomyConservationTests.cs:141/175/176/195/219` | P0 |
| 3 | 加工链无运行时执行器，`SettlementMarket` 无产出者 | 43 条配方、34 条革新门槛、`CraftProcess`/`craftDays` 全部零运行时效果；聚落市场 `inventory` 永远空 | `SettlementProduction.cs:12/20`（仅测试）；`SettlementMarket.cs:36/50` 的写者只有自身与测试；`SettlementData.cs:339 GetOrCreateMarket` 仅测试调用 | P0 |
| 4 | `tradeRoutes` 在 DTO 里直传引用、但生成期从不建立 | 存档里也不会有路线；读档不会"补出"贸易 | `TradeCenterDTO.cs:34/68`；`GameWorld.Core.cs:825-846` 只播库存 | P1 |
| 5 | 货币四阶段与通胀/劣币零驱动 | 货币阶段永不推进；`inflationRate` 只在没人读的全局副本上衰减；货币不影响价格/税收/贸易 | `CurrencySystem.cs:12/40/46`（无写入/无调用者）；`EconomyManager.cs:125`（驱动的是 `GameWorld.Core.cs:551` 副本）；唯一消费者 `RealmSituationBuilder.cs:136` 只喂评分 | P1 |
| 6 | 税率与政权脱钩（全局 TaxSystem vs realm.taxSystem） | 玩家/法律/AI 改不了税率；9 税种中 4 种不进公式；税基无人口、无控制度 | `EconomyManager.cs:485`、`GameWorld.Core.cs:552`、`TaxSystem.cs:13-29/41`；`AIDecisionSystem.cs:184-191` 空桩 | P1 |
| 7 | 建筑链无入口：定义、施工、上限、完工全在但无人调用 | `buildingLevels` 恒 0 → 农业容量加成、市场供给（`TradeCenter.cs:116-119`）、防御稳定度（`PoliticalManager.cs:71`）、陆军攻城（`Army.cs:414`）全部读到 0；建材 `materialCost` 不消耗 | `BuildingSystem.cs:109/138/147/290` 纯定义；`GameWorld.Core.cs:324-330` 无调用者；`PlanSystem` 无入档（`PlanSystem.cs:11`） | P1 |
| 8 | 30 个 BuildableType 与 29 个 BuildingDef 两套建筑分类互不相通，且前者无人调用 | 地形/坡向/聚落等级门禁全部无效；`SettlementAISystem` 三个 `ShouldBuild*` 与 `IsBuildable` 零调用者 | `BuildingAvailabilitySystem.cs:29-281/290`；`SettlementAISystem.cs:15-17/28/113/143`；grep `ShouldBuildVillage|ShouldUpgradePort|ShouldBuildFerry|IsBuildable` 仅定义处 | P2 |
| 9 | 在建工程与在途商队不入档，读档后钱已扣/货已发而实体消失 | 读档造成不可解释的资产减少（非"变多"）；计划也已扣 requirements | `SaveData.cs:36-76` 无 caravan/plan 字段；`BuildingSystem.cs:162` 先扣钱；`EconomyManager.cs:288 RegisterCaravan` 零调用者 | P1 |
| 10 | 革新解锁引用（120 条，含 31 条配方）无运行时消费者 | 革新只影响法律前置与满意度评分，不解锁任何配方/建筑/装备/单位 | `InnovationDef.cs:72`；`GameWorld.Simulation.cs:257-268`；`InnovationUnlockRefTests` 仅做 id 存在性校验 | P1 |
| 11 | 人力池零调用者 | 招兵/补员不读人口，法律`manpowerMultiplier` 在人力侧不生效（仅 `ReinforceUnit` 参数位可用但无人传） | `ManpowerSystem.cs:31/63/78`；`Army.cs:163` | P2 |
| 12 | 军事—经济只通传令官一条链；`recruitCost` 与 `Army.supply` 不接物产 | README"兵种合成与损耗直接消耗物资、无抽象维护费"与代码相反：损耗走抽象 `supply`，合成不耗物产 | `Army.cs:123-137`、`GameWorld.Core.cs:790-801`（写而不读）、`MilitaryBuildUpCommandHandler.cs:23-28`（只扣 treasury） | P1 |
| 13 | 玩家入口只有"国库一个数字" | 无物资/价格/建筑/工艺/贸易/税率任何面板或操作；6 个面板中仅 `SocietyPanel.cs:27` 与 `RealmOverviewText.cs:33` 显示 treasury | `Assets/UI/Panels/` 仅 6 文件；grep 经济关键词命中 6 处，无一处是经济操作 | P1 |
| 14 | `SettlementProduction.TryCraft` 的容量判定与 `SettlementMarket.AddStock` 重复 | 行为一致但两处都判，改动易分叉（事实陈述） | `SettlementProduction.cs:51-54`、`SettlementMarket.cs:42-45` | P2 |
| 15 | 保质期口径与数据不一致：统一 ×0.999/日，忽略 `shelfLifeDays`/`spoilagePerDay` | 小麦 180 天与肉 14 天衰减速度相同 | `EconomyManager.cs:457-473`；`GoodsDef.cs:42-44` | P2 |
| 16 | 15 种"经济相关"测试全是部件级，无一条跑 `economy.daily` 的真实世界集成 | 上述 P0 全部测试绿灯（守恒测试只验证"入账/扣账正确"，不验证"有入流"） | `Assets/Tests/EditMode/` 12 个文件中无 `BuildingSystem`/`TaxSystem`/`CurrencySystem` 行为测试；`ArchitectureContractsTests.cs:33` 用空 tiles | P1 |

**零调用者清单（grep 证据）**
- `ManpowerSystem`（整个静态类）→ 仅 `LawEffectRuntimeTests.cs:275-282`
- `SettlementProduction`（整个静态类）→ 仅 4 个测试文件
- `SettlementAISystem` / `BuildingAvailabilitySystem` → 互指，无第三方
- `ResourceAccess.CanDevelop/DescribeStatus` → 仅测试
- `UnitFounding.TryFoundSettlement` → 零（`CreateCity` 的唯一调用者）
- `BuildingSystem.BuildBuilding/StartPlannedBuilding/DemolishBuilding` → 零（仅 `ConstructionPlanSystem.cs:47` 调 `StartPlannedBuilding`，而它自己无入口）
- `GameWorld.GetEconomyManager()`(`Queries.cs:217`)、`GetBuildingSystem()`(`:293`) → 零外部调用
- `DynamicGoodsRegistry.TryCreateAlloy`、`InnovationTree.AreItemInnovationsMet`、`ContentRegistry.TryGetEquipment` → 仅测试
- `EconomyManager.GetActiveCaravans/GetCurrencySystem/GetTaxSystem` → 仅测试或零
- `ClassNeedsSystem.GrievanceThreshold`、`EconomyManager.GetRegionalVault` → 零

## 5. 我核实不了的事

1. `BuildingSystem` 与 `ConstructionPlanSystem` 是否被**编辑器工具/场景脚本**（非 `.cs` 运行时代码）调用——本轮未扫 `.unity`/`.prefab`/`Editor/` 目录的序列化引用。
2. `SettlementControlRuntime`/`SettlementEvolutionRuntime` 是否间接写 `SettlementData.market`（我只核到 `GetOrCreateMarket` 与存档两处）。
3. 是否存在运行时热更/Mod 脚本（`Assets/StreamingAssets/Mods/`）注入对 `TradeRoute`/`SettlementProduction` 的调用——未见 C# Mod 加载器可执行这些入口，但未穷举。
4. PlayMode 测试是否覆盖经济日结（本轮只读了 `Assets/Tests/EditMode/`；`Assets/Tests/PlayMode/SaveLoadPanelPlayModeTests.cs` 仅验证存读档按钮）。
5. `realm.treasury` 初值 1000（`RealmData.cs:33`）与生成期 `GameWorld.Generation.cs:88 treasury = 1000f` 是否对所有政权都执行（生成分支 `generatePolities` 未展开核对）。
