# Architecture 2.0 Domain Audit 27

## RealmBasicSaveData / GameSaveSystem 精确映射

### 1. RealmBasicSaveData 实际字段

当前 DTO 只有：
- realmId
- realmName
- monarchId
- treasury
- prestige
- stability
- centralization
- suzerainId
- vassalIds
- primaryCultureId
- stateReligionId
- controlledTiles

没有 Innovation 字段，也没有 CharacterResearch / ResearchPlan 字段。

### 2. SaveRealms 的实际来源

`SaveRealms()` 只遍历 `_world.realms.Values`，把 `RealmData` 上述基础字段写入 `RealmBasicSaveData`。

`controlledTiles` 是重新扫描 `_world.tiles[i].ownerRealmId` 得到的派生存档数据，并非 RealmData 内部的独立权威集合。

因此：
`RealmBasicSaveData ≠ Realm 全量状态`。

### 3. LoadRealms 的实际行为

`LoadRealms()` 新建 `Dictionary<int, RealmData>`，只恢复 RealmBasicSaveData 中的字段。

它没有：
- 恢复 InnovationTree `_realmInnovations`
- 恢复 `_realmResearchPoints`
- 恢复 `_realmCurrentResearch`
- 恢复 `_innovationProgress`
- 恢复 CharacterResearch
- 恢复 InnovationKnowledge
- 恢复 ResearchPlanData

因此这些状态不会因为 Realm 存档/读档而自动恢复。

### 4. 更重要的 Save Pipeline 问题

`LoadMapData()` 会重新构造 `TileData`，并明确把：
- `populationBlocks = new List<PopulationBlock>()`
- `buildingLevels = new int[0]`
- `passable = true`
- `movementCost = 1f`

写入运行时 MapCell/TileData。

这进一步证明当前 Save Schema 是一个**部分状态快照**，不是 Runtime Object Graph 的完整序列化。

这与 Architecture 2.0 的原则一致：必须逐域声明 Save Authority / Derived / Rebuild，而不能假设系统对象会自动持久化。

### 5. Innovation Save Schema 的确定方向

现在可以正式确定顶层方向：

`GameSaveData`
- `RealmBasicSaveData[] realms`
- `InnovationSaveData innovation`
- `ResearchSaveData research`

其中 ResearchSaveData 再承载 CharacterResearch / InnovationKnowledge / ResearchPlan；是否进一步拆成多个 DTO，留在 Schema Design 阶段决定。

InnovationTree 的 Realm/Social 状态不能塞进 RealmBasicSaveData。

### 6. 仍未决事项

`_innovationProgress` 仍然不能进入最终 DTO，直到确认其 aggregation scope 与 Load 重建策略。

`_realmResearchPoints` / `_realmCurrentResearch` 属于 legacy compatibility state，但由于旧 ResearchPlanExecutor 与 AIController 仍调用它们，在兼容期必须能够保存/恢复，否则读档后会产生行为不一致。

## 结论

本轮已经把 Realm Save 边界从“推测”提升为代码级事实。下一步可以正式设计 `InnovationSaveData / ResearchSaveData` 的字段，而不再继续扩大 RealmBasicSaveData。
