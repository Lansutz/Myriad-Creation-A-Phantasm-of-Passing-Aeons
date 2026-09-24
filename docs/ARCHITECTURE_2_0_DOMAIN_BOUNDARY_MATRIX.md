# Architecture 2.0 Domain Boundary Matrix

> 基于 ARCHITECTURE_2_0_DOMAIN_AUDIT_04–19。本文是领域边界与权威归属基线，不等于已经完成代码迁移。

## 1. 总原则

领域对象必须区分：
- **Authority**：该事实的唯一权威。
- **Relation**：实体之间的关系。
- **Derived**：可由权威事实重新计算。
- **Query**：回答问题，不持有领域事实。
- **Compatibility**：旧架构过渡数据。
- **Save Schema**：持久化边界，不等同 Domain Model。

核心链：
`ENTITY / STATE → CONTEXT → QUERY / CONDITION → AI → INTENT → ACTION → EFFECT → EVENT → REACTION → DERIVED / DIRTY`

## 2. 第一版正式边界

| Domain | Authority | 不应拥有 | 备注 |
|---|---|---|---|
| Map / MapCell | 地理、水文、地形、气候、海陆 | 政治、人口、建筑、道路权威 | MapCell 是空间/地理 substrate |
| Anchor | 空间存在、坐标、承载关系 | Settlement 经济/人口/政治状态 | 可承载 Settlement 或废墟/空 Anchor |
| Settlement | 定居状态、类别、规模、演化、毁灭/恢复 | MapCell 地理、Road/Network 权威 | Burg/Camp 不再作为完全独立顶层模型 |
| Settlement Role | Capital、Port、Fort 等政治/功能角色 | SettlementCategory 本身 | Capital 不是 SettlementType |
| Building | Settlement-local 建筑状态 | RoadNetwork、Great Wall | 建筑附属于 Settlement |
| Structure | Wall/Gate/Barrier/RoadSegment 等物理结构 | Political ownership、Network 全局拓扑 | 人造物 ≠ 自动成为普通 Building |
| Network | TradeRoute、RoadNetwork、FrontierDefenseNetwork 等拓扑/路线 | 单个 MapCell 的地理事实 | 节点可为 Anchor/Settlement/Gate/Port |
| GreatProject | 大型工程的生命周期、阶段、资金、劳工、材料、工作点 | PlanSystem 的个人意图 | GreatProject 与 Plan 平级 |
| Political Relation | 法理归属、实际控制、军事占领、通行权、SettlementControl | Geography | owner/occupier/control 不得混为一谈 |
| Population | 人口块、社会组成、迁移等人口事实 | Settlement 几何、Innovation authority | PopulationBlock 是 population state |
| Economy | 库存、供需、生产、贸易经济事实 | MapCell geography | TradeCenter 是经济实体 |
| Innovation Definition | InnovationDef/Field/Domain、前置、资源、能力、标签 | Character mastery、realm ownership | 静态内容注册表 |
| Social Innovation State | 已获得/已完成的 realm/social innovation | Personal Research Plan | 当前候选 authority 为 InnovationTree._realmInnovations |
| Innovation Practice | practice progress / knowledge accumulation | Character 本人研究状态 | _innovationProgress 当前 scope 未闭合 |
| Character Research | 个人研究能力、专长、灵感、贡献、个人完成记录 | Realm/social innovation authority | CharacterResearchData |
| Research Plan | 角色/主体准备做什么研究的计划 | Innovation 社会状态 | Planning ↔ Innovation bridge |
| Movement Query | 通行、移动成本、阻挡、补给、损耗、战斗支援等计算结果 | MapCell/Army 永久事实 | MilitaryMovementSystem 的目标边界 |
| Fort Influence | 堡垒影响范围/强度计算 | MapCell authority | nearbyFortId/fortInfluenceLevel 应为 derived/cache |
| Event | 已发生的领域事实 | State authority | EventBus 不是状态存储 |
| Process | 长时行为的技术推进机制 | 玩家可见领域语义 | Process ≠ Activity ≠ Plan |
| Save Schema | 持久化 DTO、版本、迁移 | Runtime domain authority | 必须与 Domain Model 分离 |

## 3. Settlement 三轴

Settlement 不允许再把以下概念压成一个 enum：

1. `SettlementCategory`: Camp / Outpost / Burg
2. `SettlementDefinition / Typology`: Village / Town / City / Fort / Port 等
3. `SettlementLevel`: 规模/发展程度

另有：
- Political Role：Capital 等
- Military Role：Fortification 等
- Spatial carrier：Anchor

## 4. MapCell 禁止回流

以下字段不得成为新的 MapCell authority：
- ownerRealmId
- occupyingRealmId
- populationBlocks
- campId
- provinceId / regionId
- buildingLevels
- roadLevel
- barrierOwnerRealmId
- barrierStrength
- nearbyFortId
- fortInfluenceLevel

这些字段可以在兼容期存在，但新代码不得以“MapCell 是世界所有状态容器”为设计前提。

## 5. 当前未闭合边界

以下必须保持明确的 unresolved 状态：
- InnovationProgress 的 scope：global / realm / culture 尚未确定。
- InnovationTree._realmResearchPoints / _realmCurrentResearch：旧研究模型与新实践模型的最终兼容策略未定。
- TradeRoute.isBlocked：当前仍为事实字段，但目标应转为 Blockage Facts → Query → Effective Efficiency。
- TradeCenter.localDemand/localSupply：经济 authority 已明确属于 Economy，但计算输入边界仍需继续拆解。
- GreatProject 最终 persistence schema 尚未建立。
- Barrier / Road / Fort 独立 Save DTO 尚未建立。

## 6. 使用规则

后续代码迁移必须先回答：
1. 谁拥有这个事实？
2. 它的 scope 是什么？
3. 它能否从其他事实重建？
4. 是否需要持久化？
5. 如果是旧字段，它属于 compatibility 还是仍是 current authority？
6. 哪些查询读取它？
7. 哪些 Effect 可以修改它？

任何无法回答的问题不得通过“新建一个 System/Class”绕过。