# Architecture 2.0 Domain Audit 25

## 本轮目标
继续收口 Innovation 的持久化边界，并检查 CharacterResearch / InnovationKnowledge / ResearchPlan 是否存在现有 Save Adapter。

## 1. CharacterResearchSystem

代码证据确认：
- `_characterResearch : Dictionary<int, CharacterResearchData>`，key=characterId。
- `CharacterResearchData` 是 `[Serializable]`。
- 但当前搜索没有发现 `SaveCharacter`、`LoadCharacter`、`CharacterResearchData` 对应的 SaveData/DTO 或 GameSaveSystem 专用保存入口。

结论：`[Serializable]` 不能视为已经持久化。当前应标记为 **Runtime Authority / Save Adapter Missing**。

## 2. InnovationKnowledgeSystem

内部状态确认：
- mastery：`Dictionary<int, Dictionary<int, byte>>`，即 character × innovation。
- practice：`Dictionary<string, float> _practiceByCharacterInnovation`，key 由 characterId + innovationId 构成。

该系统没有搜索到 Save/Load、DTO 或 SaveData 入口。

结论：两组数据都是明确的 Character × Innovation 长期状态，但当前 **Save Adapter Missing**。

## 3. ResearchPlanSystem

内部状态确认：
- `_data : Dictionary<int, ResearchPlanData>`。
- `_knowledge : InnovationKnowledgeSystem`。
- `_practiceDirtyCharacters : HashSet<int>`。
- `_executor : ResearchPlanExecutor`。

当前搜索没有发现 ResearchPlanData 对应的 SaveData、Save/Load 入口。

因此 `_data` 属于明确的持久化候选，而 `_practiceDirtyCharacters` 继续保持 Runtime Dirty / Non-Save。

## 4. CharacterResearch 与 InnovationKnowledge 的关系

不能合并成一个简单的 CharacterResearch 表：

`CharacterResearchData`：当前研究、专长、研究能力、灵感、贡献、已完成革新等角色研究状态。

`InnovationKnowledgeSystem`：角色对具体革新的 mastery 与 practice evidence。

两者都是 Character-scoped，但语义不同；后续 Save DTO 应保持分层，而不是把 InnovationKnowledge 塞进 CharacterResearchData。

## 5. InnovationTree practice aggregation

`InnovationTree.MonthlyTickProgress` 使用 researcherCount 等输入计算社会/政权层的 `InnovationProgress`。目前仍不能据此把 `InnovationProgress` 直接归入 CharacterResearch 或 InnovationKnowledge。

当前正确关系：

`CharacterResearch / InnovationKnowledge → practice/research evidence → InnovationTree aggregation → Realm/Social innovation state`

但 aggregation 的具体 scope 仍需继续审计。

## 6. Save Schema 当前状态

| Runtime State | Scope | Save Adapter | 当前结论 |
|---|---|---|---|
| CharacterResearchData | Character | 未发现 | SAVE REQUIRED |
| InnovationKnowledge.mastery | Character × Innovation | 未发现 | SAVE REQUIRED |
| InnovationKnowledge.practice | Character × Innovation | 未发现 | SAVE REQUIRED |
| ResearchPlanData | Plan | 未发现 | SAVE REQUIRED |
| `_practiceDirtyCharacters` | Runtime Dirty | 不需要 | NON-SAVE |
| InnovationTree._realmInnovations | Realm/Social | 尚未证明 | 继续审计 |
| InnovationTree._realmResearchPoints | Realm | 尚未证明 | Compatibility Save Candidate |
| InnovationTree._realmCurrentResearch | Realm | 尚未证明 | Compatibility Save Candidate |
| InnovationTree._innovationProgress | Aggregation | scope 未闭合 | UNRESOLVED |

## 7. 本轮关键结论

现在可以正式把“有没有 Save DTO”从推测变成明确审计项：当前三个独立 runtime state（CharacterResearch、InnovationKnowledge、ResearchPlan）均没有找到现成 Save Adapter 证据。

因此下一阶段不应直接删除旧研究模型，而应设计一个独立的 Innovation/Research Save Schema，并让 Load 过程显式重建 runtime dictionaries。

仍然不实施运行时代码迁移，直到 Save Schema、Load Adapter 和 `_innovationProgress` scope 完成。
