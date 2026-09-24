# Architecture 2.0 Domain Audit 16

## 本轮结论：开始形成字段级迁移证据，但暂不改代码

### 1. InnovationTree 调用面已经证明它是跨域聚合对象

当前实际引用 InnovationTree 的领域包括：

- GameWorld / Initialization：创建并持有世界级 InnovationTree，并注入 CharacterManager；
- CharacterManager / FamilyNode：用于家族传统前置检查；
- InnovationKnowledgeSystem：读取革新定义并进行角色知识候选计算；
- ResearchPlanSystem：存在明确“临时兼容桥”，旧 InnovationTree 仍负责写入社会革新集合；
- GovernmentReform / PolityComponentInnovations：用 HasInnovation 判断政体成分是否可用；
- ClassEmergenceEvents / SocialStratificationCalculator：用已完成革新计算社会阶层；
- CultureStageEvolutionSystem：用革新状态参与文化阶段演化；
- AIController：将 InnovationTree 作为 AI 决策输入；
- RealmSituationBuilder：把革新状态纳入政权 Situation；
- RealmTitleEvolution：革新影响政权显示名称；
- CharacterResearchSystem：个人研究数据独立存在，但仍与 InnovationTree 处于同一革新工作流。

因此不能简单把 InnovationTree 改名成 InnovationKnowledgeSystem。它现在同时承担“定义目录 + 社会拥有状态 + 研究选择 + 旧进度 + 新实践进度 + 前置查询”。

### 2. ResearchPlanSystem 已经明确承认自己不是社会革新权威

代码注释直接把当前调用描述为“临时兼容桥”：研究计划完成前不写入旧社会革新集合，最终仍需要 InnovationTree 接收社会革新结果。

因此当前关系可以准确描述为：

`ResearchPlan → CharacterResearch / Personal Research → completion → InnovationTree compatibility write`

而不是：

`ResearchPlan → owns Innovation`

这一点应在后续迁移中保留。

### 3. InnovationProgress 的 key 暴露出一个必须解决的模型问题

当前 `_innovationProgress` 使用 `innovationId` 单独作为 key，而不是 `(realmId, innovationId)`。

与此同时旧 `_realmResearchPoints` / `_realmCurrentResearch` 是按 realmId 管理。

因此当前新 Practice Progress 在数据结构上并不是“每个政权各自的革新进度”，而更像全局革新节点进度。

这可能是有意的“社会知识/发现水位”，也可能是历史迁移残留；在没有进一步调用方证据前不能擅自判断。

但必须记录为 Field Authority Matrix 的重点：

`InnovationProgress.progress` 的主体究竟是 Realm、Culture、InnovationKnowledgePool，还是全球发现状态？

### 4. Road / Barrier 当前权威关系

当前 BuildingSystem 仍把：

- 土路
- 石砌路
- 帝国大道
- 桥梁

注册为 `BuildingCategory.Road`。

TileData 又直接拥有：

- roadLevel
- isGate
- hasBarrier
- barrierOwnerRealmId
- barrierStrength

BarrierSystem 直接修改这些字段，MilitaryMovementSystem 直接读取它们。

因此当前真实依赖链为：

`Building / BarrierSystem → TileData → MilitaryMovement`

目标依赖链仍应为：

`Structure / Network / Political Passage → Movement Query`

### 5. Barrier 与 Fort 的领域边界得到更强证据

BarrierSystem 自身的注释明确区分：

- Barrier：狭窄通道的直接通行阻挡，攻破后才能通过；
- Fort：区域控制，不直接阻挡通行，而是敌对经过时产生损耗/速度影响，己方提供补给/支援。

这与此前审计完全一致，因此不应创建 `Fort : Barrier` 一类继承关系。

### 6. 本轮 Field Authority 初稿

| 当前字段 | 当前权威 | 2.0 目标 | 类型 |
|---|---|---|---|
| TileData.roadLevel | TileData | RoadSegment/RoadNetwork | legacy state → derived/query input |
| TileData.isGate | TileData | Gate/Passage Structure | legacy state |
| TileData.hasBarrier | TileData | Barrier/Passage Structure | legacy state |
| TileData.barrierOwnerRealmId | TileData | Political ownership/control relation of Barrier | relation |
| TileData.barrierStrength | TileData | Barrier fortification/durability state | domain state |
| TradeRoute.baseEfficiency | TradeRoute | TradeRoute | base state |
| TradeRoute.currentEfficiency | TradeRoute | TradeRoute Query/Derived | derived |
| TradeRoute.isBlocked | TradeRoute | Blockage Query | derived/compatibility |
| TradeRoute.nodeTileIndices | TradeRoute | Route path over MapCell/Anchors/Links | spatial relation |
| TradeCenter.centerTileIndex | TradeCenter | Anchor/Settlement spatial relation | relation |
| CultureData.innovationAffinities | Culture | Innovation modifier input | modifier |
| InnovationTree._realmInnovations | InnovationTree | Social Innovation Knowledge/Ownership | authoritative state candidate |
| InnovationTree._realmResearchPoints | InnovationTree | legacy research progress | compatibility candidate |
| InnovationTree._realmCurrentResearch | InnovationTree | legacy research selection | compatibility candidate |
| InnovationTree._innovationProgress | InnovationTree | InnovationProgress/Knowledge layer | unresolved authority |
| ResearchPlanSystem data | ResearchPlan | Personal Research Plan | authoritative personal state |
| CharacterResearchSystem data | Character | Personal mastery/research | authoritative character state |

### 7. 目前不能直接迁移的字段

以下字段在主体身份没有确定前禁止搬家：

- InnovationProgress.progress
- InnovationProgress.cumulativeOutput
- InnovationTree._realmResearchPoints
- InnovationTree._realmCurrentResearch
- TradeRoute.isBlocked
- TradeCenter.localDemand / localSupply
- TileData.fortInfluenceLevel

原因不是“代码复杂”，而是这些字段可能是 Derived、Social Knowledge、Personal Knowledge 或 Relation 的不同层次。

### 8. 下一阶段

继续完成：

1. InnovationProgress / CharacterResearchData / ResearchPlanData 的字段级主体审计；
2. Fort / fortInfluenceLevel 的全部引用；
3. BarrierSystem 全部读写方法；
4. RoadLevel 的全部生产者和消费者；
5. SaveData 对这些旧字段的持久化方式。

完成后再生成正式 `ARCHITECTURE_2_0_DOMAIN_BOUNDARY_MATRIX.md` 和 `ARCHITECTURE_2_0_FIELD_MIGRATION_MATRIX.md`。
