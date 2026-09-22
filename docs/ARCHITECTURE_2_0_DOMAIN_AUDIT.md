# Architecture 2.0 — Domain Audit 01

> 研究阶段记录。用于沉淀现有代码的结构判断，不代表立即迁移。

## 本轮结论

当前最重要的问题已经不是 GameWorld 是否足够“拆分”，而是数据对象本身仍然承担了过多规则职责。

已经确认的高优先级模式：
- Entity 内存在计算方法。
- Entity 内存在 DailyTick。
- Definition 与 Runtime State 混合。
- Relation 散落在实体字段与 Dictionary 中。
- Derived Value 与 Authority State 混合。
- Population 仍然物理嵌套在 TileData。
- 战争、军队等持续过程仍由实体/Manager 的 Tick 推进。
- Save/DTO 结构正在反向影响领域模型。

## 1. Tile

Tile 是整个地图模拟的核心数据载体。PopulationBlock 直接存于 TileData.populationBlocks；buildingLevels 也直接存于 TileData；编辑器通过 ref TileData 直接修改世界事实；多个系统直接读取并修改 Tile。

判断：Tile 不能简单拆成更多小类就结束。需要区分 Geography State、Climate State、Territorial Relation、Population Reference/Container、Construction State、Economic State、Derived Metrics、Dirty/Invalidation。

Population 和 Building 是下一层独立建模的关键。

## 2. PopulationBlock

当前 PopulationBlock 是 struct，并直接作为 Tile 的 List 元素。这其实是合理的性能方向：普通人口不应个体化成 Character。

但它应明确为高密度 PopulationGroup State，并建立 PopulationQuery，使总人口、文化占比、信仰占比、阶层占比、人力、满意度等统计不再由各系统自行实现。

## 3. Burg

SettlementTypologySystem、SettlementEvolutionSystem、SettlementControlSystem 已经说明 Burg 本身是状态，规则已经部分外置，这是值得保留的方向。

Burg 仍混合聚落事实、发展状态、建设状态、军事状态、控制状态、废墟状态、类型分类。下一阶段逐字段标记 Authority / Derived / Relation / Runtime。

## 4. Character

CharacterData 明显是 Entity，而不是 System。但实体内已经存在 RoleToClass、RoleToSubclass、SyncClassFromRole 等规则方法。

Character 同时拥有生命周期、社会身份、文化/信仰、血缘、DNA、能力、属性、人格、疾病、衰退、关系、技能和行为统计。

判断：Character 可以继续作为数据实体，但规则方法应逐步进入 CharacterQuery / CharacterRules / Action / Effect。不要让 Character 获得 DailyTick。

## 5. Army

Army 是最明确的 Entity + Domain Logic 混合案例。

当前已确认存在 GetTotalManpower、CalculateCombatPower、DailyTick、ReinforceUnit、MoveTick 以及战斗状态/伤亡率处理。

迁移方向：GetTotalManpower → Query；CalculateCombatPower → Warfare Query；ReinforceUnit → Warfare Action + Effect；MoveTick → Movement Process；DailyTick → 移出实体。

Army 是第一批最值得结构迁移的对象之一。

## 6. WarState

WarState 本身已经比较接近纯持续状态。

但 CombatManager 仍直接遍历战争、检查交战、推进战斗和更新战争分数。

最终应明确：WarState = State；WarProcess = Process。WarProcess 负责“战争今天发生了什么”。

## 7. Culture

Culture 必须固定为 CultureDefinition + CultureState。

“文化是什么”和“文化当前发展到什么程度”是两个不同问题。

不要继续通过增加 CultureSystem 字段解决 Definition/State 混合。

## 8. Faith

FaithSystem 这个命名已经不符合 Architecture 2.0。

如果对象代表具体信仰、教义、信徒、热忱、圣地和组织状态，那么它首先是 Faith Definition/State，而不是 System。

后续需要重命名/拆分，但暂不操作，先完整审计引用关系。

## 9. Innovation

InnovationProgress 已经接近正确的 State 对象。

真正需要解决的是 InnovationTree 同时承担 Definition/Tree、Progress Registry、Resource Practice、Research Processing、Completion。

最终方向：InnovationCatalog + InnovationProgressStore + InnovationQuery + InnovationProcess。

## 10. Realm

RealmData 应保持为 Realm Entity/State。

AI 目前直接接收 RealmData 并自行计算外交效用，说明 AI → RealmData → Manager 的紧耦合仍存在。

未来应改为 AI Context → Queries → Candidate Actions → Utility Modifiers。

## 11. Save / DTO

SaveData / RealmDTO 表明存档模型已经与运行时对象耦合。

长期原则：Save Schema ≠ Domain Model。DTO 是持久化边界，不能为了直接序列化而永久固定 Domain Entity 的结构。

## 12. 第一批迁移候选

A 级：Army；Culture；Faith；InnovationTree / InnovationProgress；PopulationQuery。

B 级：Tile Dirty / Derived；Character Rules；Burg Derived State；Realm Relations。

C 级：War Process；Save/DTO boundary；AI Utility。

原因：A 级能够建立 Architecture 2.0 所需要的基础抽象，同时不需要先推翻 GameWorld。

## 13. 暂不改

暂时不重写 TileData、CharacterData、RealmData、SaveSystem；不删除所有 Manager/System；不引入动态 Scope、脚本 DSL 或 ECS。

先做语义边界，再决定性能实现。

## 14. 下一步

继续逐字段语义审计：对象 → 字段 → Authority / Derived / Relation / Runtime / Definition → 使用者 → 最终归属。

完成后才开始第一批代码迁移。
