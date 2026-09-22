# Architecture 2.0 — Domain Model & Refactoring Plan

> 状态：研究设计稿。先确定数据语义与边界，再迁移代码。
> 分支：architecture-2-0-refactor

## 1. 核心目标

Architecture 2.0 不是继续增加 Manager/System，而是把世界事实、关系、查询、规则、行动、效果、持续过程、领域事件、反应和派生状态分开。

核心链：
ENTITY/STATE → CONTEXT → QUERY/CONDITION/MODIFIER → INTENT → ACTION → EFFECT → STATE CHANGE → DOMAIN EVENT → REACTION → DIRTY → DERIVED STATE

CK3 只作为概念研究对象，不复制其文件结构、脚本语法或实现限制。

## 2. 当前真实数据模型审计

| 对象 | 当前性质 | 主要问题 | 目标 |
|---|---|---|---|
| TileData | 世界实体 + 大量状态 | 地理、气候、经济、人口、Dirty 混合 | TileState；派生计算移出；Dirty 独立 |
| BurgData | 聚落实体 + 状态 + 派生 | 经济、军事、形态、控制、废墟混合 | Entity + Relation + Derived |
| RealmData | 政权实体 + 关系 + 系统状态 | 财政、制度、附庸、领土、税制混合 | RealmState + Relations + Queries |
| CharacterData | 个体实体 | 属性、疾病、关系、评价、规则高度耦合 | Character Entity + Domain Logic |
| Army | 军队实体 + 行为 | DailyTick、移动、战斗力、补员都在 Entity | ArmyState + Warfare Query/Action |
| WarState | 战争状态 | 已较接近正确，但仍缺少过程层 | WarState + War Activity/Process |
| CultureData | Definition + State | 文化定义和成熟/扩散状态混合 | CultureDefinition + CultureState |
| FaithSystem | Definition + State + Logic | 名称错误；实际上是 Faith 实体 | FaithDefinition + FaithState |
| InnovationProgress | 进度状态 | UI 数据与研究状态混合 | InnovationDefinition + Progress + Knowledge |
| PopulationBlock | 人口状态 | 容易成为 Tile 的隐式万能容器 | PopulationGroup + Population Queries |

## 3. Tile

Tile 是核心世界事实实体，但不能成为万能容器。

应保留：地理、地形、海陆、气候的权威状态，以及必要的所有权/占领关系引用。

应逐步外移：movementCost、经济收入、人口统计、文化/信仰占比等可计算结果。

Dirty 是派生状态失效机制，不是地理事实。

buildingLevels 需要后续继续审计：如果建筑最终成为独立 Definition + State，则不应长期只是裸数组。

## 4. Burg

Burg 是聚落实体，而不是经济系统。

目标结构：
BurgState = Identity + Location + SettlementState + PopulationReference + ControlRelation + ConstructionState + RuinState

primarySector、primaryFunction、cityFocus、fortSubtype、portTier 等字段必须逐个判断：它是事实，还是由事实推导出的分类。

## 5. Realm

Realm 保留政权自身的权威状态，但不负责整个政治模拟。

RealmState：资源、政府构成、继承状态、主权/控制状态、政策状态。

Relations：附庸、宗主、外交、宣称、通行权、领土关系。

税收、军事力量、外交结果、社会指标等应通过 Query/Action/Process 得到。

## 6. Character

Character 是真正的 Entity。

保留：身份、生命状态、亲属关系、社会身份、文化/信仰引用、能力、特质、健康、资源、职位关系。

fullName、isAlive 等是派生值；RoleToClass、能力计算等属于规则/Query，不应成为 Character 核心职责。

Character 不应拥有 DailyTick。

## 7. Army

Army 保存状态，不负责整个战争规则。

ArmyState：兵力、所有者、指挥官、编成、士气、组织度、补给、训练、位置、移动状态。

Warfare Query：MilitaryPower、CombatPower、SupplyNeed、MovementCost。

Warfare Action：MoveArmy、Attack、Retreat、Reinforce、Disband。

当前 Army.CalculateCombatPower、DailyTick、ReinforceUnit 等是重点迁移对象。

## 8. War

WarState 已经接近正确方向。

战争应是持续 Domain State，同时由 Activity/Process 推进。

War = Participants + WarGoal + Score/Progress + Battles + Occupation + Exhaustion + Outcome。

War Activity/Process 负责推进、战斗结算、战争分数、终止检查和结果处理。

## 9. Culture

必须拆 Definition 与 State。

CultureDefinition：名称、颜色、生活方式、移动方式、葬俗、崇拜、物质风格、象征实践、环境适应、生产偏好、语言、革新亲和、默认继承规则。

CultureState：maturity、spread pressure/power、分支状态。

文化传播、演化、分化属于 Domain Logic。

## 10. Faith

当前 FaithSystem 实际上是 Faith Entity/State，命名本身就是架构问题。

FaithDefinition：身份、类型、神灵、教义、仪式、禁忌、美德/罪行、组织模型。

FaithState：教阶、财富、影响力、热忱、区域遵从度、关系。

DailyTick 应移出实体，交给 Religion Domain Process/Action。

## 11. Innovation

最终模型：
InnovationDefinition → InnovationProgress → InnovationKnowledge

Definition：革新内容、前置条件、领域、效果。
Progress：某政权/群体当前进度。
Knowledge：角色/社会掌握程度。
Effect：完成革新后产生的规则变化。

## 12. Population

Population 是高频、大规模状态，不应把普通人口个体化为 Character。

PopulationGroup：Location + Size + Culture + Faith + SocialClass + Mobility + EconomicState。

Population Query：TotalPopulation、CultureShare、FaithShare、ClassShare、CarryingCapacity、Manpower。

Population Process：BirthDeath、Migration、Assimilation、Conversion、SocialDifferentiation。

## 13. Context

借鉴 CK3 Scope 的核心思想：规则需要一个当前解释环境，并能导航到相关对象。

但 Unity/C# 不建立动态字符串驱动的万能 Scope。

采用强类型 Context：SimulationContext、ActionContext、CharacterContext、SettlementContext、RealmContext、WarfareContext、WorldContext。

Context 提供当前世界、时间、Actor、Target、SecondaryTarget、Location 和必要规则环境；不保存所有业务状态。

## 14. Query / Condition / Modifier

这是 Architecture 2.0 必须补齐的一层。

IQuery<T>：读取一个值或状态。
ICondition：判断规则是否成立。
IModifier<T>：修改数值。
IUtilityModifier：修改 AI Utility。

例如 FinalTax = TaxBase × ControlModifier × BuildingModifier × GovernmentModifier × CultureModifier。

不要继续把所有计算堆进 EconomyManager.Get/CalculateXXX。

## 15. Action / Effect

Action = 我要执行什么行为，以及是否允许。
Effect = 行为执行后世界具体改变什么。

典型链：Action → Target Resolution → Condition → Cost → Effects。

CommandBus 是外部输入边界，不是整个模拟器的中心。

## 16. Intent / Command / Action

Player / AI / Character / World Rule → Intent → ActionResolver → Action → Effects。

Command 更接近输入/请求；Action 是领域行为；Effect 是实际状态突变。

## 17. Event / Reaction / Dirty

必须严格区分：

State Change：普通事实变化，例如 population 850 → 851。
Dirty：派生状态需要重新计算。
Domain Event：有领域意义的事实，例如 WarStarted、CharacterDied、SettlementDestroyed、InnovationCompleted。
Reaction：对领域事件的可复用响应。

不要把每个字段变化都发布成 Event。

## 18. Activity / Process

Activity 是领域过程：War、Construction、Migration、Expedition、Research 等。

Process 是技术执行器：Activity → ActivityExecutor → ISimulationProcess → ProcessResult。

ProcessResult：Continue、Complete、Wait、Fail、Cancel。

现有 ActivitySimulationProcess / PlanSystem 可以继续作为迁移基础，但不能让 Runtime/Process 永久只是 Legacy 静态方法的转发器。

## 19. Manager / System

Manager 适合注册表、索引、生命周期、内容仓库和外部服务。

System 适合技术基础设施、调度、地图、渲染、路径等，以及暂未迁移的旧领域逻辑。

不要继续形成 PoliticsSystem/EconomySystem/CultureSystem/ReligionSystem 等万能领域系统。

最终领域规则应由 State + Query + Condition + Modifier + Action + Effect + Process 组合。

## 20. 迁移顺序

Phase A：实体语义审计。
Tile → Population → Burg → Realm → Character → Army → War → Culture → Faith → Innovation。

Phase B：建立 Query。
优先 Population、Control、Distance、MilitaryPower、FoodBalance、Treasury、CultureShare、FaithShare。

Phase C：Action/Effect。
优先 Settlement、Warfare、Politics。

Phase D：Activity/Process。
优先 War、Construction、Migration、Research、Expedition。

Phase E：Event/Reaction/Dirty。

Phase F：AI。
AI 最终消费 Action + Condition + Utility，而不是直接知道所有 Manager。

## 21. 暂时不做

不立即实现 CK3 风格动态 Scope、脚本解释器、ScriptValue/Trigger/Effect DSL、StoryManager、SituationManager，也不做全量 Manager 删除和全量 Entity 重写。

这些都依赖底层数据语义确定后再设计。

## 22. 第一批真正值得重构的对象

1. CultureData → CultureDefinition + CultureState
2. FaithSystem → FaithDefinition + FaithState
3. TileData → State / Derived / Dirty 分离
4. Army → State 与行为分离
5. CharacterData → Entity 与 Query/Domain Logic 分离
6. RealmData → State 与 Relations/Derived 分离
7. BurgData → State / Relation / Derived 分离
8. InnovationProgress → Definition / Progress / Knowledge
9. WarState → War State + Activity/Process
10. Population → PopulationGroup + Query

## 23. 判断标准

新增代码前先问：
1. 这是事实还是计算结果？
2. 这是 Definition 还是 Runtime State？
3. 这是 Relation 还是 Entity 自身属性？
4. 这是 Query 还是 Mutation？
5. 这是 Action 还是 Effect？
6. 这是持续过程还是一次性行为？
7. 这是 Domain Event 还是普通字段变化？
8. 这是 Dirty 还是 Event？
9. 这是 AI Utility 还是游戏规则？
10. 删除这个类后，真正消失的是领域概念还是只是调用便利？

## 24. 最终方向

Architecture 2.0 的核心不是把 GameWorld 拆成更多 System，而是让世界模拟形成清晰的语义层：

World State / Entities / Relations / Definitions
→ Context
→ Query / Condition / Modifier
→ Intent
→ Action
→ Effect
→ State Change
→ Domain Event
→ Reaction
→ Dirty
→ Derived State

下一阶段先做实体语义审计，再开始第一批真正迁移。
