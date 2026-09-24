# Architecture 2.0 — Domain Audit 05

## 本轮主题：Map / MapCell / Anchor / Settlement / Building / Structure / 地理变化

本轮根据现有仓库代码继续审计，不进行大规模代码迁移。

## 1. 当前 TileData 明显是混合容器

现有 TileData 同时保存：

### 更接近 Map / Geography 的事实
- elevation01
- slopeDegree
- terrainShade
- isLand / isCoast
- oceanTier / oceanDepth01 / seaConnectId
- isRiver
- annualTemp / annualPrecipMm / airHumidityPct / soilHumidityPct / climateZone
- biome

这些字段描述的是“这个空间现在是什么地理环境”。

### 明显属于世界状态/分布的事实
- ownerRealmId
- occupyingRealmId
- populationBlocks
- campId
- provinceId
- regionId

这些不应被默认视为 Map 本体。

### 明显属于经济/社会/基础设施的事实
- fertility
- development
- stability
- order
- buildingLevels
- roadLevel
- barrierOwnerRealmId / barrierStrength
- nearbyFortId / fortInfluenceLevel

因此后续迁移目标应是：

MapCell = 空间单元 + 地理状态

而不是：

MapCell = 整个世界状态容器

## 2. MapCell 这个名字可以正式作为候选

现有 TileData 的实际语义首先是“地图离散空间单元”，而不是政治领地。
由于项目已经存在 Title 头衔领域，并且未来还有 Realm / Territory 等政治概念，不应把地图空间单元命名为 Territory、Domain、Holding、Province 等。
当前建议暂定：TileData → MapCell。
注意：这是架构目标名称，不代表现在立即全局重命名。

## 3. 当前代码其实已经有三类聚居点的雏形

代码中已有 SettlementCategory：
- Burg：定居点
- Outpost：据点
- Camp：营地

代码注释已经明确：
- Burg = 永久性，以居住/生产为核心
- Outpost = 永久性，以控制/防御/通道为核心
- Camp = 临时/半永久

这与当前设计方向高度一致。

但是同时又存在 SettlementType：Village / City / Fort；以及 BurgType：Village / Town / City / Port / Capital / Fortress。

因此当前代码中“聚居点大类”和“聚居点形态/功能/等级”已经发生交叉。

后续应明确：
- SettlementCategory = 三种根本存在形式
- SettlementDefinition / SettlementType = 聚居点具体定义、形态与功能

不能继续让 Village / City / Fort 与 Burg / Outpost / Camp 处于同一分类层。

## 4. Anchor 应与 Settlement 分开

当前 BurgData 直接保存 tileIndex、x、y，说明现有 Burg 同时承担：
1. 聚居实体；
2. 地图空间定位点。

后续需要把两个语义拆开：

Anchor：固定空间位置、所在 MapCell、精确地图坐标、地图上的固定节点身份。
Settlement：聚居点本体、三种 SettlementCategory、人口、发展、功能、演化等社会状态。

因此 Anchor 不应该直接定义“这是城市/村庄/营地”。

## 5. Building 当前严重依附 Tile，而不是 Settlement

现有 BuildingSystem 使用 Dictionary<int, List<ActiveBuilding>>，以 tileIndex 注册建筑。
同时 TileData 直接保存 buildingLevels，README 还明确描述为“每地块上限 5”。

这与目标模型存在明显偏差：

Settlement → Buildings

而不是：

MapCell → Building

后续需要确认建筑是否必须挂靠 Settlement。根据当前设计方向，聚居点建筑应挂在 Settlement 下；只有未来确认存在“不属于聚居点的独立建筑/设施”时才另行处理。

## 6. Road 当前被错误地建模为 BuildingCategory

现有 BuildingCategory.Road，并且 BuildingSystem 定义土路、石砌路、帝国大道、桥梁。
同时 TileData.roadLevel 直接承载道路等级。

这说明现有实现把“道路”作为建筑系统的一部分。

根据当前架构设计，应迁移为：

Structure → Road

但这里不是简单把 Road 从一个 enum 改成另一个 enum。
道路本质上是具有空间几何/路径关系的构筑物，应能够：
- 跨越多个 MapCell；
- 连接多个 Anchor；
- 独立于单个 Settlement 存在；
- 影响 MapUnit 移动与交通。

因此最终不应继续使用“某 Tile 上有一个 Road Building Level”作为唯一权威。

## 7. Wall 也不应继续作为 Building 的普通类别

现有 BuildingSystem 中木栅栏、石墙、城堡、要塞都属于 BuildingCategory.Defense。
同时 BurgData.wallLevel 又单独保存城墙等级。

这形成了两套防御设施语义。

当前目标应明确：
- Building = 建筑
- Structure = 构筑物

因此：
- 建筑物本身继续属于 Building；
- 城墙属于 Structure；
- 聚居点可以拥有/关联自己的城墙；
- 但城墙本身不因此成为 Settlement 的子类。

## 8. Great Wall / 长距离城墙证明 Structure 不能从属于 Settlement

未来长城可能：
- 穿越多个 MapCell；
- 连接多个 Anchor；
- 穿越无人区；
- 不经过任何 Settlement；
- 与多个政治实体产生关系。

所以 Settlement → Wall 只能作为一种“聚居点与城墙的关联关系”，不能成为 Structure 的唯一生命周期/所有权模型。

目标更接近：

Structure → geometry / route / MapCells / Anchors / related Settlements / political-military relations

其中“是否挂靠某聚居点”是关系，而不是 Structure 的存在条件。

## 9. Manor / Estate 不应归入 Building 或 Settlement

根据当前设计，庄园：
- 不是建筑；
- 不是聚居点；
- 不是 Anchor；
- 但挂靠于 Settlement。

因此不应把它强行塞进 Building。

暂定关系：

Settlement
- Buildings
- Manors

Manor 自身再拥有其土地、生产、管理、所有权等社会经济状态。
具体 Manor 模型暂不创建，等待后续领域设计。

## 10. Geography 必须允许动态改变

现有代码已经存在动态海陆基础：WorldConfig.seaLevel。
SeaLandGenerator 根据海平面/高程确定海陆。
HydrologySystem 根据高程和海陆计算洼地填充、流向、汇水面积、河流、流域和河流等级。

因此“海侵 / 海退”不应该被设计成 Structure 或 Settlement 行为。

目标应是：

Geography Change → 改变 Map / MapCell 的地理状态 → 重新计算相关 Derived State → 产生领域事件 → Settlement / Anchor / Structure / MapUnit / Politics / Population 响应。

例如：
- 海侵：Land → Ocean
- 海退：Ocean → Land

之后再根据规则处理通行性、海岸、河流、水文、气候、生物群系、Anchor、Settlement、Structure、MapUnit。

## 11. 运河不应因为“人工建造”而自动归入 Structure

当前设计原则确定为：

分类依据不是“自然还是人工”，而是“它是不是地理本体”。

如果项目规则把运河视为水文/地理网络的一部分，那么它属于 Geography / Hydrology。

因此当前暂定：
- River → Geography / Hydrology
- Lake → Geography / Hydrology
- Sea → Geography / Hydrology
- Coast → Geography
- Canal → 如果设计为地理水系，则 Geography / Hydrology
- Road → Structure
- Wall → Structure

这样才能保证“人工改变地理”和“建造构筑物”是两个不同概念。

## 12. 当前目标模型

Map
└── MapCell
      └── Geography / Hydrology / Terrain / Climate
             ↑
             │ Geography Change
             ├── Sea Transgression
             ├── Sea Regression
             ├── River Change
             └── Other Geographic Change

MapCell
├── Anchor
│     └── Settlement
│           ├── Buildings
│           └── Manors
│
├── Structure
│     ├── Road
│     ├── Wall
│     └── GreatWall
│
└── MapUnit
      ├── ArmyUnit
      ├── Caravan
      ├── Refugee
      └── ...

这里的关系不是全部都是父子关系。
尤其：
- Structure 可以跨 MapCell；
- Road 可以连接多个 Anchor；
- GreatWall 可以跨多个 Settlement；
- MapUnit 可以移动；
- Anchor 固定；
- Settlement 是社会实体；
- Building 是建筑；
- Manor 是特殊地产/社会经济实体；
- MapCell 是空间单元；
- Map / Geography 是地理本体。

## 13. 下一阶段迁移顺序

暂不创建最终类。

1. 完整审计 BurgData，把“空间定位 / 聚居点 / 建筑 / 城墙 / 功能 / 经济”字段逐项拆开；
2. 审计 ActiveBuilding 与 BuildingDef，确认建筑真正的父级关系；
3. 审计现有 Road / Barrier / Fort 数据，决定 Structure 的最小共同状态；
4. 审计 CampData，确定 Camp 是 SettlementCategory 还是当前独立 CampData 的兼容实现；
5. 审计 MapCell 的 Geography 字段，建立“静态地理 / 动态地理 / Derived Geography”边界；
6. 最后才设计 Anchor、Settlement、Structure、MapUnit 的正式代码目录与迁移顺序。

本轮结论：不写新框架；先把现有混合实现拆解清楚。