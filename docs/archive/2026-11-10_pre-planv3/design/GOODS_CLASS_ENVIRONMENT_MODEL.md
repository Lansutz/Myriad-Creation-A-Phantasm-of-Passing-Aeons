# 物产 / 阶层需求 / 环境 —— 模型修正与设计（Addendum）

> 本文修正上一版 `GEOLOGICAL_CLIMATE_DYNAMICS_PLAN.md` 中把经济简化为"抽象粮食"的错误表述。
> 本项目**不是**"一个粮食数值"模型：物产是具体的 `GoodsDef`，有物理属性、加工链、产地与保质；
> 需求是**阶层化的**（`ClassNeedWeightTable` 的 5 类身份 × 8 个需求维度），不是"生理条件"。
> 环境系统要影响的是**具体物产的产出/可获得性/价格**，再经阶层需求篮子影响满意度与政治。

---

## 一、现有物产模型（代码事实）

### 1.1 物产是具体条目，不是抽象类别

`GoodsDef`（`Content.Definitions`）字段：

| 字段 | 含义 |
|---|---|
| `goodsId/goodsName` | 具体物产（0 粮食、1 肉类、2 鱼类、3 盐、10 谷物、11 棉花、20 马…90 奴隶） |
| `category` | Food/Crop/Livestock/Wood/Stone/MetalOre/Metal/Equipment/Luxury/Slave/Cloth/Leather/Fiber/Hide/Fur/Clay/Ceramic/Glass |
| `baseValue / weight` | 价值、重量（运输/载重） |
| `hasShelfLife / shelfLifeDays / isPerishable / spoilagePerDay` | 保质/腐烂 |
| `originBiomes / spawnCondition / baseAbundance` | 产地群系、生成条件、丰度 |
| `isNaturalResource / resourceType / renewable / isLiving` | 自然资源、可再生、生物 |
| `processedFromId / processingRatio` | 加工链（原料→成品） |
| `properties[]`（`GoodsPropertyValue`） | **物理属性**：meltingPoint / hardness / toughness / fuelValue / ductility 等 |
| `canBeSlaughtered / slaughterGoodsId` | 畜牧屠宰 |
| `requiredInnovation` | 技术门槛 |

此外有：
- `GoodsRecipe`/`RecipeIngredient`/`RecipeCatalog`：配方（多输入→输出、`CanCraft`）；
- `DynamicGoodsRegistry`：运行时合金/合成物（按输入物产属性加权，`FirstDynamicGoodsId=10000`）；
- `TradeCenter/SettlementMarket`：库存、供需、价格、容量；
- `TradeCenter` 的 `localSupply = fertility × development × 0.5` 只影响价格；税基用 `fertility × development × 10`；
- `SettlementProduction.TryCraft`：**已存在但从未被运行时调用**（配方合成未接线）；
- `ResourceGenerator`：**只打日志、不落资源点**（生成期自然资源分布未接线）；
- `ProcessConsumption`：当前用 0 号粮食与 3 号盐做简化消耗，是**临时简化**，不是目标模型。

### 1.2 阶层需求是身份化的

`ClassNeedWeightTable`（`Simulation.State`）：5 个阶层 × 8 个维度权重：

| 阶层 | 权重重点（示例） |
|---|---|
| Royalty | 合法性、特权、安全、经济机会 |
| NobilityClergy | 政治参与、安全、合法性、特权 |
| MerchantFreeman | 经济机会、政治参与、税负、安全 |
| Peasant | 生存（Subsistence）、安全、税负 |
| Slave | 生存、安全 |

`ClassNeedsSystem` 用 `RealmSituation`（foodSecurity、publicOrder、税负、政治参与、贸易、货币、合法性、特权…）算 8 维分数，再按阶层权重合成满意度。**没有"生理条件"建模**；"Subsistence"是社会经济维度，不是生理需求。

### 1.3 真正的缺口（修正后的表述）

不是"缺食物系统"，而是**物产链未闭环**：

1. 生成期 `ResourceGenerator` 未落资源点；物产产地/丰度只存在于定义里。
2. `SettlementProduction`（配方→物产）无运行时调用；加工/冶炼/酿造链没接。
3. 人口消耗简化为 0 号粮食+盐；没有"某阶层消费哪些物产"的**需求篮子**。
4. 地形/气候只影响 `fertility` 与 `development` 的**价格/税基**，不进入"某物产产量"。
5. 环境变化（气候/植被/土壤/海平面）不会改变谷物/木材/鱼/盐/矿石的产出与可获得性。
6. 灾害只扣稳定度/肥力，不扣具体物产库存与产量。
7. 阶层需求与具体物产没有映射：一个农民缺粮、缺盐、缺木材、缺布，在模型里都只表现为抽象的 `foodSecurity`。

---

## 二、修正后的环境→物产→阶层链条

```
地质/气候/植被/土壤/海平面
        │ 影响
        ▼
各物产的"产出函数"（按 tile / 聚落 / 区域）
  - 可耕性 → 谷物/棉花/亚麻/染料作物
  - 牧草/植被 → 肉/奶/毛/皮/役畜
  - 森林覆盖 → 原木/树脂/野味/药材
  - 洋流/上升流/海岸 → 鱼/鲸/盐
  - 板块/岩性/矿脉 → 矿石/石料/宝石/燃料
  - 气候/群系 → 丝绸/香料/酒/奢侈品
        │
        ▼
市场（库存/供需/价格/运输） → 可获得性 & 价格
        │
        ▼
阶层需求篮子（每个阶层 × 具体物产权重）
  - 满足度 → 8 个需求维度 → 满意度/政治稳定
        │
        ▼
人类开发反馈：伐木/开垦/灌溉/过牧/采矿/城市化
  → 植被↓、土壤退化、盐碱化、侵蚀↑、矿区枯竭
  → 回到产出函数（闭环）
```

### 2.1 环境→物产映射（每种物产用其定义字段驱动）

| 物产组 | 驱动字段 | 环境变化后果 |
|---|---|---|
| 谷物/棉花/亚麻/染料作物 | `originBiomes` + 可耕性（T/P/土壤有机质/坡度/灌溉） | 变冷/变干→减产；盐碱化→弃耕；海侵→沿海农田消失 |
| 肉/奶/毛/皮/役畜 | 牧草/植被覆盖、载畜量 | 过牧→植被↓→载畜量↓→肉毛减产；荒漠化→游牧南迁 |
| 原木/树脂/野味/药材 | 森林覆盖率/植被潜在值 | 伐木→森林↓→木材减产+侵蚀↑；造林/禁伐→恢复 |
| 鱼/鲸/盐 | 洋流/上升流/海岸/盐田/盐度 | 洋流改向→渔场转移；海侵→盐田/港口受损；上升流→鱼获↑ |
| 矿石/石料/宝石/燃料 | 板块/岩性/矿脉/侵蚀暴露 | 火山/造山→新矿脉；侵蚀→露天矿暴露；采矿→枯竭 |
| 丝绸/香料/酒/奢侈品 | 特定气候/群系 + 技术 | 气候带移动→产区转移；贸易路线改变→价格 |

### 2.2 阶层需求篮子（内容可配，示例）

新增 `ClassDemandProfile`（JSON：`StreamingAssets/Base/Economy/ClassDemand.json`）：

| 阶层 | 生存/日用 | 生产/装备 | 身份/奢侈 |
|---|---|---|---|
| Royalty | 谷物、肉、盐 | 武器、盔甲、马 | 丝绸、香料、酒、金银器 |
| NobilityClergy | 谷物、肉、酒 | 武器、盔甲、马 | 香料、丝绸、书籍/圣物 |
| MerchantFreeman | 谷物、鱼、盐 | 工具、布、货币 | 香料、染料、奢侈品 |
| Peasant | 谷物、鱼、盐 | 木材、粗布、工具 | 酒/节庆（低权重） |
| Slave | 谷物/杂粮、盐 | 粗布 | —（极低） |

- 每项：`goodsId` + 权重 + 最低/期望数量 + 可替代组（如"谷物/鱼/肉"可替代）；
- 市场缺货/价格高 → 需求满足度下降 → 对应维度（Subsistence/EconomicOpportunity/Privilege…）下降；
- 环境变化通过"产量/价格/可获得性"传导，而不是直接改满意度。

### 2.3 人类开发反馈（按物产而非抽象土地）

| 行为 | 直接效果 | 长期环境效果 |
|---|---|---|
| 伐木 | 木材产量↑ | 森林覆盖↓、侵蚀↑、野味/药材↓ |
| 开垦农田 | 谷物产量↑ | 有机质↓、生物多样性↓ |
| 灌溉（干旱区） | 谷物产量↑（短期） | 盐碱化↑→产量↓→弃耕/荒漠化 |
| 过牧 | 肉/毛↑ | 植被↓、侵蚀↑、载畜量↓ |
| 采矿 | 矿石↑ | 矿脉枯竭、地表破坏、水土污染 |
| 城市化 | 手工业/贸易↑ | 可耕/牧地↓、下渗↓、洪水风险↑、热岛 |

政策/革新（轮作、休耕、梯田、水利、护林、矿政）作为系数修正，让玩家能治理。

### 2.3b 资源分档与枯竭/回长（借鉴 civ-atlas development.ts）

- 每个州/区域对每类资源有 **0–3 档储量**（0 无、1 少、2 中、3 多），由地形/群系/气候/板块评分后按全世界排名分档；矿/煤另加本地随机。
- 底子（储量）不随年份变；**可采量**随技术/使用变化：矿要技术发现、开采会枯竭（最后三成越采越少）；林/渔/田/牧会被人多/开发久吃空，人少时每年回长；盐不变。
- **地形大事后按新地形重新分档**，储量按新旧档位比例缩放（civ-atlas 的做法），保证"火山/海侵"后资源格局变化但不重置历史。
- 时代门槛卡资源（青铜要铜锡、铁器要铁、工业要煤铁、电气要石油、信息要稀有矿）；缺盐/马/铁/金银会驱动战争——与本项目"具体物产驱动"一致。

### 2.4 灾害按物产结算

| 灾害 | 受影响物产/系统 |
|---|---|
| 干旱 | 谷物、牧草、水力（磨坊） |
| 洪水 | 谷物、道路、仓储、沿海盐田 |
| 风暴 | 鱼获、航运、沿海建筑 |
| 寒潮/热浪 | 谷物、牲畜、劳动效率 |
| 蝗灾 | 谷物、牧草、植被 |
| 野火 | 木材、野味、植被、聚落 |
| 地震 | 矿石/石料、建筑、道路、水渠 |
| 火山 | 火山灰→短期作物减产、长期火山土壤肥力↑、新矿脉 |
| 海啸/海侵 | 沿海鱼/盐/农田、港口、建筑 |
| 咸潮入侵 | 沿海农田盐碱化、淡水 |
| 沙尘暴 | 牧草/农田、健康（不建模生理，只影响产出/满意度） |

---

## 三、修订后的环境系统设计（与原 Plan 的差异）

保留原设计的"生成期 / 局内双模式 + 开关 + 数据字段 + SaveVersion 17 + 确定性 + 性能预算"，但把目标从"食物产量"改为**物产产出**：

### 3.1 新增系统（替换原 FoodProduction 设想）

| 系统 | 调度 | 职责 |
|---|---|---|
| `GoodsProductionSystem` | Daily/Seasonal（区域聚合） | 按 tile/聚落/建筑/技术，对每种可产物产计算产量 → 市场库存 |
| `ResourceRegenerationSystem` | LongTerm | 可再生资源（森林/鱼/野味/牧草）按植被/水文/洋流恢复；不可再生（矿）衰减 |
| `ClassDemandSystem` | Monthly | 按阶层需求篮子 + 市场库存/价格 → 消费、满足度、短缺 |
| `TradeFlowSystem`（已有 TradeCenter/商队，需接线） | Daily | 区域间运输、价格差、贸易路线生成 |
| `LandUseSystem` | Yearly | 人口/聚落/农田/牧场/伐木/采矿 → 环境压力 |
| `EnvironmentTickSystem` | LongTerm | 植被/土壤/侵蚀/海平面/气候距平（原设计） |
| `DisasterSystem`（扩展） | Daily | 灾害 → 具体物产库存/产量/建筑/人口 |

### 3.2 `TileData` 字段（补充物产相关）

除原环境字段外，增加**类别级适宜度**（避免 per-goods 大字典）：

| 字段 | 范围 | 用途 |
|---|---|---|
| `arableSuitability` | 0..1 | 谷物/棉花/亚麻等作物产出 |
| `pastureSuitability` | 0..1 | 肉/奶/毛/皮/役畜 |
| `forestSuitability` | 0..1 | 原木/树脂/野味/药材 |
| `fisherySuitability` | 0..1 | 鱼/鲸/盐（沿海/洋流） |
| `mineralRichness` | 0..1 | 矿石/石料/宝石/燃料 |
| `vegetationCover/Potential` | 0..1 | 森林/牧草基础，非 100% |
| `soilOrganic/soilSalinity/erosionRate/waterTable` | 0..1 | 肥力/退化/盐碱 |
| `landUse/irrigation` | 0..1 | 人类压力 |
| `plateId/tectonicStress/crustAge` | — | 地质 |

物产到类别映射由 `GoodsDef.category/originBiomes/resourceType` 决定，个别特殊物产用内容表覆盖（如丝绸只限特定群系）。

### 3.3 `fertility` 的位置

`fertility` 不再是"粮食产量"，而是**土地综合生产力**（0..1），由土壤/植被/盐碱/侵蚀派生，作为 `arableSuitability/pastureSuitability` 的输入之一；税基/价格公式保留但改读"物产实际产出/交易额"。

### 3.4 内容表

新增/扩展 JSON：
- `StreamingAssets/Base/Economy/Goods.json`（把 `GameWorld.Core` 硬编码 30+ 物产迁出；当前硬编码在 `InitializeGoodsDefs`）
- `GoodsProperties.json`（已有，物理属性）
- `Recipes.json`（已有，加工链）
- `ClassDemand.json`（阶层需求篮子，新增）
- `Environment/`（气候参数、植被上限、灾害定义、板块预设）

---

## 四、为什么这个修正很重要

1. **玩家看到的是"木材/铁/鱼/盐/丝绸"**，不是"粮食"；环境变化必须能解释"为什么今年木材贵、鱼获少、盐田被淹、铁矿产区转移"。
2. **阶层需求是政治系统的基础**：贵族缺丝绸/香料与农民缺谷物，造成的满意度/合法性后果不同；不能用一个 `foodSecurity` 概括。
3. **物产有物理属性**（硬度/熔点/韧性/燃料值），这决定了工具/武器/合金/建筑；环境改变资源可得性，但**不改变物产属性本身**。
4. 这也解释了为什么现有 `SettlementProduction`/`RecipeCatalog`/`DynamicGoodsRegistry` 必须接线：环境新增的是**原料与产量**，加工链已有。

## 五、参考项目分工（按现有设计取舍）

| 参考 | 借什么 | 不借什么 |
|---|---|---|
| CK3 | 阶层/合法性/法律框架、需求维度权重 | CK3 没有物产经济 |
| Victoria 3 | 物产—人口—市场—价格链路、阶层消费篮子 | 不照搬其全国市场抽象层级 |
| EU4 | 贸易节点/发展度/价格影响 | 不照搬抽象"贸易价值" |
| Imperator | 人口/粮食/补给与地形关系 | 不引入"抽象粮食" |
| 本项目既有 | `GoodsDef` 物理属性、配方、动态合金、阶层权重 | — |

## 六、对原计划 E0-E7 的修订

- **E0 前置**改为：
  - `ResourceGenerator` 真正落资源点（按 `originBiomes/spawnCondition`）；
  - `SettlementProduction`/`RecipeCatalog` 接线（聚落/工坊按配方产出）；
  - `ClassDemandSystem` 阶层需求篮子 → 消费具体物产；
  - 修 0 号物产 sentinel 语义（审计 P0-1）；
  - 修 `UpdateStorageCapacities` O(n²)；
  - RNG 确定性。
- E1-E7 保留，但所有"food/产量"改为"物产产量/可获得性/价格"。
- 验收新增：给一个农民家庭，断谷物→Subsistence 维度下降；断盐→不同维度；断木材→建筑/燃料受影响；贵族断丝绸→Privilege/合法性变化；这些必须通过**具体物产**断言，而不是抽象粮食。
