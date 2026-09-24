# Architecture 2.0 Domain Audit 26

## Save pipeline evidence

本轮继续追踪 GameSaveData / GameSaveSystem。

### 1. GameSaveData 当前不是完整 Domain Save

已确认 `GameSaveData` 目前至少保存：
- `version`
- `RealmBasicSaveData[] realms`
- `ChronicleEntrySaveData[] chronicleEntries`
- 地图/时间等核心运行状态（由现有 GameSaveSystem 组装）

代码注释明确表示角色、军队、战争、外交等复杂数据仍在后续扩展阶段。

因此不能把 `realms` 的保存解释为 CharacterResearch、InnovationKnowledge 或 ResearchPlan 已经保存。

### 2. RealmBasicSaveData ≠ InnovationSaveData

现有 Realm 基础保存结构不能自动覆盖 InnovationTree 的内部 dictionary：
- `_realmInnovations`
- `_realmResearchPoints`
- `_realmCurrentResearch`
- `_innovationProgress`

这些状态都必须有显式 DTO / Save Adapter，除非明确声明可由其他 authority 完全重建。

### 3. Character Save 当前缺口更明确

搜索没有发现：
- `CharacterSaveData`
- `SaveCharacter`
- `LoadCharacter`
- CharacterResearch 专用 SaveData

所以 CharacterResearchData 当前不能依赖 GameSaveSystem 的 Realm 保存获得持久化。

### 4. 独立 Save Chunk 是合理目标

Architecture 2.0 应把 Innovation/Research 保存视为独立 domain chunk，而不是继续塞进 RealmBasicSaveData：

`GameSaveData`
→ `InnovationSaveData / ResearchSaveData`
→ explicit Load Adapter
→ runtime systems

其中可以进一步拆分：
- RealmInnovationSaveData
- CharacterResearchSaveData
- CharacterInnovationKnowledgeSaveData
- ResearchPlanSaveData

最终是否物理上拆成四个 DTO 文件，留待 Save Schema Design 决定；这里先只确定 authority 边界。

### 5. 当前禁止事项

在 Save Schema 完成前：
- 不删除 InnovationTree legacy research state。
- 不删除 CharacterResearchData。
- 不把 InnovationKnowledge 合并进 CharacterResearchData。
- 不把 ResearchPlanData 塞入 RealmBasicSaveData。
- 不把 `_innovationProgress` 当作已确定的 Character 或 Realm state。

## 结论

当前存档系统已经证明是“部分 Domain State + 明确 Save DTO”，而不是全 Runtime 自动序列化。因此 Innovation/Research 必须显式设计 Save/Load 边界。

下一阶段：继续确认 `RealmBasicSaveData` 的字段，并追踪 `GameSaveSystem.SaveRealms/LoadRealms` 的实际映射，再开始正式 Save Schema 草案。
