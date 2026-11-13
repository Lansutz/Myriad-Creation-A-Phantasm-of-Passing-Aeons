# 采集/据点/载具模型

> 本文把用户 2026-11-03 的四点要求落成可执行结构：软前置候选、资源三层、据点采集/采矿设施、单位物品栏的“载具”层。
> 对应实现：`InnovationSoftPrerequisite`、`ResourceAccessDepth`、`BuildingCategory.Mining/Gathering`、`VehicleCategory`。

---

## 1. 软前置候选：只给“有物理阈值”的东西

软前置不是万能的；只有**目标物品的物理属性能映射到能力通道**时才给。

| 革新 | 关联物产 | 物理属性 → 能力通道 | 系数 | 说明 |
|---|---|---|---|---|
| 201 青铜冶炼 | 铜 51 | meltingPoint → heat.maxTemperature | 0.9 | 铜熔点 1085×0.9≈976；炉温够即可，不锁死燃料 |
| 202 块炼铁冶炼 | 铁矿石 50 | meltingPoint → heat.maxTemperature | 0.7 | 固态还原，不需熔化铁 |
| 203 渗碳炼钢 | 钢 57 | meltingPoint → heat.maxTemperature | 0.9 | 渗碳/锻打温度区间 |
| 931 生铁高炉 | 铁矿石 50 | meltingPoint → heat.maxTemperature | 1.0 | 需要熔化生铁 |
| 930 渗碳淬火 | 钢 57 | meltingPoint → heat.maxTemperature | 0.8 | 淬火/渗碳温度 |

**不要给**：单纯采集、伐木、狩猎、捕鱼这类“没有明确物理阈值”的革新硬塞软前置；它们的软前置应来自**资源层级/工具能力**，见 §2、§3。

能力通道目前只有 `heat.maxTemperature`；后续可扩：

- `material.hardnessMax`（石材/金属加工）
- `material.toughnessMax`（木材/纤维/皮革）
- `mining.surface/shaft/deep`（布尔能力，走 `requiredCapabilities`，不是数值软前置）

---

## 2. 资源三层：露天 / 坑采 / 深井

现有模型：

```text
ResourceAccessDepth = Surface | Shaft | Deep
TileResource.accessDepth
ResourceAccess.IsDepthAccessible(depth, capabilities)
```

能力：

| 层级 | 能力 | 代表革新 |
|---|---|---|
| 露天 | `mining.surface` | 802 露天采掘 |
| 坑采 | `mining.shaft` | 803 深井采矿 |
| 深井 | `mining.deep` | 803 深井采矿、933 井盐深钻 |

已修正：此前任何 Mining 革新都会被兜底塞入 `mining.surface/shaft/deep`，等于白送深井；现在 802/803/933/934 都显式声明，测试 `ResourceTierCapabilityTests` 锁定。

软前置与层级的关系：

- 需要某层资源才能满足的工艺，应挂 `requiredCapabilities`（布尔）或 `softPrerequisites` 的 `capabilityChannel`；
- 不要用“必须先有某条采矿革新”当硬前置；只要求能力/资源可达。

---

## 3. 定居点 / 据点 / 营地：三者定位

| 层 | 定位 | 代码 | 采石/采矿 |
|---|---|---|---|
| **定居点** | 长期农业/城市/工商聚落，家庭、市场、行政 | `SiteMode.SedentarySettlement` | 不是采石场本身；可围绕采石业形成城镇 |
| **据点** | 长期设防节点，可住人、驻军、有经济成分；资源/军事/交通 | `SiteMode.FortifiedOutpost` + `OutpostKind` | **采石/采矿就是据点的一种用途** |
| **营地** | 临时/移动营地，生命周期短 | `SiteMode.TemporaryEncampment` | 可以在附近采集，但不把“采集营地”做成据点类型 |

### 据点用途 OutpostKind

```text
Generic / Military / Mining / Quarrying / Logging /
Fishing / Hunting / Gathering / Trade / Waystation
```

- 大型采石场/矿山 = **据点**（`FortifiedOutpost` + `OutpostKind.Quarrying/Mining`），不是设施，也不是 `SedentarySettlement`；
- 据点可以有 `population`、`garrison`、`economicComposition`；
- 据点不是“定居点”的别名，也不是“临时营地”。

### 设施 vs 据点

- `FacilityType` 只表示据点/聚落内部、**无独立人口/驻军**的附属设施（墙、门、壕沟等）；
- 采石场本身不能叫 `FacilityType.Quarry`；
- 小型井口/栈桥可作设施，但要按是否有独立人口/驻军判断。

### 单位

- `UnitActivity`（`TransportUnit.activity`）：Gathering/Mining/Logging/Fishing/Hunting；
- 单位采集/采矿不要求据点；
- 据点与单位两侧共用资源/物产定义。

## 4. 采集物品的软前置

“采集什么”本身应能给出软前置，但不应硬编码“采集浆果需要 X 科技”。建议：

```text
CollectibleDef（或 GoodsDef 的采集侧扩展）
  ├─ resourceKind: Plant / Fungal / Shellfish / Fish / Game / Mineral
  ├─ accessDepth: Surface / Shaft / Deep
  ├─ toolProperty: hardness / toughness / none
  └─ requiredCapabilities: 例如 "gathering.softFood"
```

对应革新（采集方法）只写软前置：

- 块根采集：需要 `gathering.digging` 能力（木/骨/石工具）；
- 贝类采集：需要 `gathering.shore`；
- 渔猎：需要 `gathering.water`/`gathering.projectile`；
- 硬果/坚果：需要 `gathering.climbing` 或工具硬度。

能力来源由已解锁的工具/工艺提供；**不**要求“先有农业才能采集”。

---

## 5. 单位物品栏：载具层

目标：

```text
Inventory
  └─ Vehicle（载具）
       ├─ LandVehicle（车类）
       │    ├─ Handcart
       │    ├─ OxCart
       │    └─ Wagon
       └─ WaterVehicle（船类）
            ├─ Raft
            ├─ Canoe
            ├─ Sailboat
            └─ ...
```

已加 `VehicleCategory { Other, Land, Water }` 作为大类入口；下一步：

1. 新增 `VehicleDef`（Content.Definitions）：
   - `vehicleId / name / category / requiredInnovation`
   - `cargoCapacity / crewRequired / draftAnimals`
   - `List<VehicleModuleSlot> moduleSlots`（可模组化：货箱/帆/桨/护甲/武器）
2. `TransportUnit`：
   - 用 `List<VehicleInstance> vehicles` 作为统一载具列表；
   - `CartKind`/`boats` 迁移为 `VehicleDef` 的具体条目（封闭开发，可不保留旧字段）；
   - 现有 `DailyFoodDemand`/`DraftStrength`/`CrossingMultiplier` 从载具类别派生；
3. 存档 DTO `WorldEntitySaveData` 同步为 `vehicleIds + moduleIds`；
4. UI 物品栏按“载具 → 车/船 → 模块”三级展示。

---

## 6. 实施顺序

1. 软前置候选（201/202/203/930/931）；
2. 资源三层能力（已完成 802/803/933/934 显式化）；
3. 据点 `OutpostKind` / 设施 `FacilityType` / 单位 `UnitActivity`（已有入口，待接产出）；
4. 采集侧 `CollectibleDef` + 采集能力；
5. 载具 `VehicleDef` + `TransportUnit.vehicles` + 存档/UI。
