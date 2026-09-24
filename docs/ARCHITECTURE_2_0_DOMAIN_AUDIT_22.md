# Architecture 2.0 Domain Audit 22

> 本轮完成 Innovation / CharacterResearch / ResearchPlan 的调用关系核对。重点确认“个人实践—知识—研究计划—社会革新”是否已经在代码中实际分层。

## 1. InnovationKnowledgeSystem 已经形成独立个人知识层
`InnovationKnowledgeSystem` 的类注释明确说明：
- 社会知识由 InnovationTree 的政权革新集合表示；
- 本系统只保存个人掌握等级与实践积累；
- 掌握等级属于同一个 Innovation，不创建新的 Innovation 节点。

其 `RecordPractice(characterId, innovationId, amount)` 使用 `(characterId, innovationId)` 复合 key 保存实践量。

这比 Audit 18 对 InnovationProgress 的判断更重要：**当前项目实际上已经存在一个明确的 character×innovation 个人实践存储层。**

因此不能把 `_innovationProgress` 直接解释成“个人实践数据”。个人实践至少已经由 InnovationKnowledgeSystem 承担一部分 authority。

## 2. CharacterResearch 与 InnovationKnowledge 是两个不同层
当前关系可以明确为：

`CharacterResearchData`
→ 角色研究状态：当前研究、专长、能力、灵感、个人贡献、完成记录。

`InnovationKnowledgeSystem`
→ 角色对 Innovation 的掌握等级、实践积累。

`InnovationTree`
→ 社会/Realm Innovation state + 旧 Realm Research 生命周期 + 新 practice aggregation。

所以不能把 CharacterResearchData 和 InnovationKnowledgeSystem 合并成一个“CharacterInnovation”大对象。

## 3. ResearchPlanSystem 的内部结构已确认
ResearchPlanSystem 内部持有：
- `GameWorld _world`
- `PlanSystem _plans`
- `InnovationKnowledgeSystem _knowledge`
- `Dictionary<int, ResearchPlanData> _data`
- `ResearchPlanExecutor _executor`
- `_practiceDirtyCharacters`。

这证明 ResearchPlanSystem 不是单纯的 UI/命令包装器，它拥有自己的 ResearchPlan runtime state。

同时它通过 `_plans` 与通用 PlanSystem 建立关系。

正确边界仍然是：
`ResearchPlanData = domain-specific plan state`
`Plan = generic personal plan runtime`
`ResearchPlanExecutor = execution adapter`。

## 4. ResearchPlan 的实践入口已经形成明确链路
GameWorldPlanning 中存在统一实践入口：
`RecordPractice(characterId, innovationId, amount)`
→ `ResearchPlanSystem.RecordPractice(...)`
→ `InnovationKnowledgeSystem.RecordPractice(...)`。

因此个人实践不是直接写 InnovationTree。

这进一步强化了：
`Character / Knowledge → Evidence → InnovationTree aggregation`
而不是
`Character → directly mutate InnovationTree`。

## 5. ResearchPlan 仍然可以触发旧 Realm Research 生命周期
ResearchPlanExecutor 在执行计划时会检查：
- `_world.Innovations.HasInnovation(realmId, innovationId)`
- `_world.Innovations.GetCurrentResearch(realmId)`
- `_world.Innovations.StartResearch(realmId, innovationId)`。

因此旧 Realm Research 生命周期目前仍然是实际运行路径的一部分。

这不是“纯历史死代码”的证据。

当前系统实际上存在两条研究路径：

### A. Legacy / Realm-directed
`ResearchPlan → InnovationTree.StartResearch(realmId, innovationId) → DailyTick → CompleteResearch → _realmInnovations`

### B. Practice / Character-directed
`ResearchPlan / Practice → InnovationKnowledgeSystem(character, innovation) → practice evidence → InnovationTree.MonthlyTickProgress(...)`

两者目前并存。

## 6. AI 也仍然直接使用 Realm Research
AIController 会调用 `Innovations.StartResearch(realmId, innovationId)`。

因此在没有完成 AI Call Graph 重构之前，不能删除 `_realmCurrentResearch` / `_realmResearchPoints`。

## 7. InnovationProgress 的含义需要重新定位
Audit 18 原先把 `_innovationProgress` 标为“practice progress，scope unresolved”。

本轮之后应进一步细化：
- 个人 practice：InnovationKnowledgeSystem，scope 明确为 character×innovation。
- 社会 practice aggregation：InnovationTree.MonthlyTickProgress，输入包含 realmId。
- `_innovationProgress`：仍未确认最终 scope，但已经可以确定它**不是 CharacterResearchData 的简单替代品**。

因此矩阵应将其写成：
`InnovationTree._innovationProgress = aggregation/progression state; scope unresolved`。

## 8. Save 结论进一步加强
现在已经发现至少三类运行时状态：
1. CharacterResearchData
2. InnovationKnowledgeSystem._practiceByCharacterInnovation 等个人知识数据
3. InnovationTree._innovationProgress / _realmInnovations / legacy research state

而现有 Save 审计尚未找到这些状态的完整专用 Save/Load 路径。

因此 Save 缺口比 Audit 20 进一步扩大：不仅 CharacterResearch 要处理，**InnovationKnowledgeSystem 也必须进入 Save Audit。**

## 9. 需要加入 Field Migration Matrix 的新字段
| Current | Target | Status |
|---|---|---|
| InnovationKnowledgeSystem._practiceByCharacterInnovation | Character Innovation Practice Evidence | KEEP / SAVE AUDIT |
| InnovationKnowledgeSystem mastery state | Character Innovation Knowledge | KEEP / SAVE AUDIT |
| CharacterResearchData | Character Research | KEEP / SAVE AUDIT |
| ResearchPlanSystem._data | Research Plan State | KEEP / SAVE AUDIT |
| ResearchPlanSystem._practiceDirtyCharacters | Runtime dirty queue | DERIVED / NON-SAVE |
| InnovationTree._innovationProgress | Social/Realm practice aggregation | UNRESOLVED |
| InnovationTree._realmInnovations | Social/Realm Innovation Knowledge | KEEP / MOVE LATER |
| InnovationTree._realmResearchPoints | Legacy Realm Research | COMPAT |
| InnovationTree._realmCurrentResearch | Legacy Realm Research Selection | COMPAT |

## 10. 当前研究模型的准确图
`Innovation Definition`
↓
`Character Research / Research Plan`
↓
`Character Practice / Innovation Knowledge`
↓
`Practice Evidence / Aggregation`
↓
`InnovationTree Social Innovation State`

同时旧路径仍存在：
`Realm → InnovationTree.StartResearch → DailyTick → CompleteResearch`。

因此后续真正的重构不是“删旧系统换新系统”，而是：
**把两条并存生命周期最终统一到同一个明确的 Innovation domain model，同时保留兼容迁移层。**

## 11. 下一步
继续完成：
1. InnovationKnowledgeSystem 的全部字段与生命周期。
2. ResearchPlanData 全字段、Create/Execute/Complete/Cancel 生命周期。
3. CharacterManager Save/Load 是否覆盖 CharacterResearch。
4. InnovationKnowledge / ResearchPlan 的 Save/Load 缺口。
5. 最后回写两个正式矩阵。

**目前仍不进行代码迁移。**