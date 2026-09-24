# Architecture 2.0 — Domain Audit 08

## Great Wall / Settlement Typology / Save Boundary

### 1. Great Wall 不是普通 Structure

用户设计已明确：长城本质不是一个“构筑物实体”。

它是由多个据点、关隘、交通节点等**连接形成的空间—军事网络/体系**。

其核心功能包括：
- 运兵；
- 限制外部进入；
- 限制内部离开；
- 形成防御纵深；
- 连接沿线据点与关隘；
- 可能承担交通、控制、预警等其他功能。

因此此前“GreatWall → Structure”的规划需要撤回。

应区分：

**Wall / Barrier**
- 可以是具体构筑物；
- 是某一地点的实体防御设施。

**GreatWall / Frontier Defense Network**
- 是多个据点、关隘、道路、墙段等形成的网络/体系；
- 本身不是一堵“巨大建筑”；
- 不应该要求自己拥有一个连续建筑几何体；
- 可以由多个节点和连接段组成。

目标关系更接近：

GreatWallSystem / DefenseNetwork
→ Nodes
  → Outpost / Fort / Gate / Anchor
→ Links
  → Road / WallSegment / Pass / Route
→ Functions
  → MilitaryTransit
  → EntryRestriction
  → ExitRestriction
  → Defense
  → Detection / Control（以后确认）

因此长城的“存在”应该是网络关系和军事地理体系，而不是一个单一 Structure。

### 2. 这会进一步区分 Structure 与 Network

Structure：
- WallSegment
- RoadSegment
- Gate
- Bridge（如最终确认）
- 其他具体空间构筑物

Network / System：
- RoadNetwork
- GreatWall / FrontierDefenseNetwork
- TradeRoute
- 其他由节点与连接组成的空间体系

一个 Network 可以引用多个 Structure，但不能反过来要求 Structure 必须属于某个 Network。

### 3. 当前代码已经暴露这种网络雏形

现有 SettlementControlSystem 强调聚居点节点控制周边聚落；
BarrierSystem 又通过 Fortress/Burg 与 Tile barrier 计算区域影响；
MilitaryMovementSystem 使用 barrierStrength / roadLevel 影响通行；
TradeRoute 使用 roadLevel。

这说明当前系统事实上已经存在：
- 节点；
- 连接；
- 通行；
- 阻挡；
- 区域影响；

只是这些关系目前被压缩到 TileData、BurgData 和几个 System 中。

后续应把它们拆成：
Spatial Node / Spatial Link / Network / Derived Influence。

### 4. SettlementTypologySystem 当前职责仍然过宽

现有 SettlementTypologySystem 同时处理：
- 类型推导；
- 升级路线；
- 形态约束；
- 城形；
- 堡型；
- 港口层级；
- 关隘瓶颈；
- 要塞体系。

BurgGenerator 创建 Burg 后立即调用 DeriveInitialType；
随后又由 BurgTypeInferrer 强制覆盖 settlementType。

这说明当前至少存在：
- Geography-based inference；
- Definition；
- Runtime state；
- Legacy compatibility mapping；

四种不同责任。

后续 SettlementDefinition 应成为静态内容定义；
Typology Query / Inference 负责根据地理、经济、政治、军事条件选择/推导；
SettlementState 保存当前结果；
Evolution Process 负责长期变化。

### 5. Capital 不应继续作为普通 SettlementType

现有 BurgType 包含 Capital，而 BurgTypeInferrer 把它映射到 SettlementType.City。

这实际上混淆了：
- 城市形态；
- 政治首都角色。

目标应是：

Settlement Typology = Village / Town / City / Fortification-oriented forms / Port-oriented forms ...

Political Role = Capital / Administrative Center / etc.

因此“首都”可以是一个 City，也可以在未来规则下由其他 Settlement Typology 承担，而不是必须成为独立地理类型。

### 6. SaveData 已经把 Map 与 Settlement 分开了一半

当前存在：
- MapSaveData → TileSaveData + ProvinceSaveData + BurgSaveData
- GameSaveData → tiles / provinces / burgs / realms ...

说明存档层已经把 Tile 与 Burg 作为不同记录保存。

但 BurgSaveData 仍然包含大量 BurgData 的混合状态，因此未来 DTO 边界可以自然演化为：
- MapCellSaveData / GeographySaveData
- AnchorSaveData
- SettlementSaveData
- BuildingSaveData
- Structure/Network SaveData
- Political/Distribution SaveData

不要让新的领域模型被当前 SaveData 反向绑死。

### 7. 当前正式模型修正

Map
└── MapCell
    └── Geography / Hydrology / Terrain / Climate

MapCell
├── Anchor
│   └── Settlement
│       ├── Buildings
│       └── Manors
│
├── Structure
│   ├── WallSegment
│   ├── RoadSegment
│   └── ...
│
└── MapUnit

Network / System
├── RoadNetwork
├── TradeRoute
└── GreatWall / FrontierDefenseNetwork
    ├── Settlement / Outpost Nodes
    ├── Fort / Gate Nodes
    └── Links / Segments

注意：这里仍然不是要求所有对象都成为 MapCell 的直接子对象；MapCell 主要提供空间索引。

### 8. 下一步

继续审计：
1. GreatWall/Barrier/TradeRoute/MilitaryMovement 的实际关系；
2. SettlementTypologySystem 全部 enum 和方法；
3. BurgSaveData / TileSaveData 的完整字段；
4. 再形成第一版正式“字段迁移矩阵”。

在字段迁移矩阵完成前，不创建最终 Anchor / Settlement / Structure / Network 类。
