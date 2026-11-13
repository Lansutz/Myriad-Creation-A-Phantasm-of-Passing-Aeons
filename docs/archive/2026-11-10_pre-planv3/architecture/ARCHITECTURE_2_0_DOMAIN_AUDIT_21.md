# Architecture 2.0 Domain Audit 21

> 本轮继续追踪 GameSaveSystem 的实际 Save/Load 路径，并核对 Realm/Burg/Province 与 Innovation/CharacterResearch 的覆盖关系。

## 1. GameSaveSystem 是另一套独立 Save 管线
仓库同时存在：
- `SaveSystem → SaveData → ToSaveData(GameWorld)`
- `GameSaveSystem → GameSaveData → SaveRealms/LoadRealms/...`
- `MapSaveSystem → MapSaveData`

因此不能只审计 `SaveSystem` 就认为整个项目的存档已经覆盖。

## 2. GameSaveData 的当前组织方式
GameSaveSystem 明确按领域步骤执行保存/读取，包括：
- SaveMapData / LoadMapData
- SaveRealms / LoadRealms
- 以及其他完整游戏状态步骤。

这说明当前 Save Schema 已经具有“领域分块”倾向，而不是单一 TileData dump。

但本轮检索仍未发现 InnovationTree、CharacterResearchSystem、ResearchPlanSystem 对应的明确 Save/Load 步骤。

因此当前可以把它们分成两类：
- **已进入 Save pipeline 的领域**：Map、Realm、Burg、Province 等。
- **运行时存在但尚未证明进入完整 Save pipeline 的领域**：Innovation Practice、CharacterResearch、ResearchPlan、Barrier 独立状态等。

## 3. Realm Save 与 Innovation State 仍然不是同一件事
SaveRealms / LoadRealms 存在，但当前证据不能推出它们自动保存 InnovationTree 的内部 Dictionary。

尤其 InnovationTree 的：
- `_realmInnovations`
- `_realmResearchPoints`
- `_realmCurrentResearch`
都不是 RealmData 自身字段的证据。

所以后续必须保持：
`RealmSaveData ≠ InnovationSaveData`。

即使 Innovation state 最终按 realm 保存，也应通过明确的 Save Adapter/DTO 表达，而不是依赖 RealmData 的偶然序列化。

## 4. BurgSaveData 是当前最重要的成熟参照
仓库存在独立 `BurgSaveData`，且包含城市区划等 Settlement 相关数据。

这说明 Settlement 已经具有比 MapCell 更清晰的 Save 边界。

因此未来 Settlement 2.0 拆分可以采用：
`BurgSaveData → Settlement/Anchor/Role/Structure 迁移层`
而不是把 Burg 重新塞回 MapCell Save。

## 5. ProvinceSaveData 进一步说明行政关系应独立于 Geography
仓库存在独立 `ProvinceSaveData`。

这与此前 MapCell 不应成为政治/行政状态 authority 的结论一致。

Province/Region/Settlement 等应该继续通过关系和 DTO 保存，而不是让 MapCell 成为所有行政信息的持久化容器。

## 6. InnovationTree 的运行时依赖范围已经很广
当前公开入口和调用证据至少包括：
- `GameWorld.GetInnovationTree()`
- `CharacterManager.Innovations` 注入
- `CultureStageEvolutionSystem` 读取 realm innovations
- `RegimeChangeDynamics.SetInnovationTree(...)`
- Realm title evolution 等系统读取 InnovationTree
- CharacterResearch / InnovationKnowledgeSystem 与 InnovationTree 交互。

因此 InnovationTree 不是一个可以在 Save 审计完成后简单删除的旧系统。

它实际上是当前多个 Domain 的共享旧聚合入口。

## 7. InnovationTree 的构造方式带来一个重要 Save 风险
`GameWorld.Initialization` 中通过 `new InnovationTree()` 创建实例，构造时调用 `LoadFromRegistry()`。

这说明：
- Innovation Definition 是从 Content Registry 重建的静态数据。
- InnovationTree 的运行时状态必须另行恢复。

也就是说：
`LoadFromRegistry()` ≠ Load Save State。

未来 Save/Load 必须严格区分：
`Innovation Definition Registry` 与 `Innovation Runtime State`。

## 8. CharacterResearch 的 Save 风险
CharacterResearchData 是 Character 的运行时状态，但本轮未找到对应独立 DTO/SaveLoad 步骤。

因此如果 CharacterData 已经被保存，也不能默认 CharacterResearchData 会自动保存，尤其其字段可能位于 CharacterResearchSystem 的 Dictionary 中。

需要下一轮继续追踪 CharacterManager 的 Character Save 路径，以及 CharacterResearchSystem 的实例生命周期。

## 9. ResearchPlan 的 Save 风险
ResearchPlanSystem 由 GameWorldPlanning 独立持有，并与 PlanSystem 绑定。

目前没有证据证明 GameSaveSystem 会保存 ResearchPlanSystem 内部数据。

因此必须避免把 ResearchPlan 的持久化问题错误地归到 Innovation 或 Character Save。

## 10. 新增的 Save Boundary 结论
当前正式矩阵应增加以下原则：

1. `Content Definition` 可以由 Registry 重建，不等于 Runtime State。
2. `Runtime System Dictionary` 不会因为对象本身存在就自动进入 JsonUtility Save。
3. Realm/Burg/Province DTO 不自动涵盖其他 System 的外部状态。
4. 每个独立 Domain System 都必须有明确的 Save Adapter、可重建声明，或明确标记为非持久化 derived state。
5. Save/Load 必须成对审计；只有 Save 没有 Load 仍然不能视为完整持久化。

## 11. 下一步
继续追踪：
1. CharacterManager 的 Save/Load 以及 CharacterResearchSystem Dictionary 生命周期。
2. ResearchPlanSystem / PlanSystem 是否已有任何序列化路径。
3. InnovationTree 所有 state mutation API：StartResearch、DailyTick、CompleteResearch、MonthlyTickProgress、Practice/Knowledge 写入。
4. GameSaveData 的具体字段与每个 SaveX/LoadX 对应关系。
5. 最后更新 Domain Boundary / Field Migration Matrix 的 Save 列。

**当前仍不执行 Domain 类迁移或旧字段删除。**