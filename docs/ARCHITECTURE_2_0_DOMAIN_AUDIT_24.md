# Architecture 2.0 Domain Audit 24

> 本轮完成 InnovationKnowledgeSystem 与 ResearchPlanData 的字段/生命周期证据收口，并更新正式矩阵。

## 1. InnovationKnowledgeSystem：个人知识 authority 已闭合

当前公开接口确认：GetMastery(characterId, innovationId)、SetMastery(characterId, innovationId, level)、RecordPractice(characterId, innovationId, amount)、GetPractice(characterId, innovationId)。

内部状态至少包括 character × innovation → mastery level（byte）与 character × innovation → practice amount（float）。因此这一层的 scope 已明确：**Character × Innovation**。它不是 Realm Innovation，也不是 InnovationTree 的社会完成状态。

## 2. Mastery / Practice 生命周期

Mastery 被限制在 Level 0–3，提升不能一次跨越多个等级，因此是长期个人状态。RecordPractice 对 character × innovation 累积实践量，GetPractice 被 ResearchPlanSystem 用于研究/验证进度。因此 practice amount 不是纯日志，而是持续影响计算的状态。

## 3. ResearchPlanData

ResearchPlanSystem 内部同时维护 PlanSystem、InnovationKnowledgeSystem、Dictionary<int, ResearchPlanData> _data、ResearchPlanExecutor 与 _practiceDirtyCharacters。ResearchPlanData 是持久化候选；_practiceDirtyCharacters 是运行时 dirty queue，不应进入最终 Save Schema。

当前已确认 ResearchPlanData 至少包含：planId、characterId、realmId、innovationId、relevance、verificationProgress。

字段边界：planId=Planning identity；characterId=Character relation；realmId=Realm context；innovationId=Innovation target；relevance=Research evaluation；verificationProgress=Research activity state。

## 4. Executor 边界

ResearchPlanExecutor 实现 IPlanExecutor，通过 PlanSystem 的通用生命周期运行，同时读取 ResearchPlanData 并处理研究领域行为。当前不需要把 ResearchPlanSystem 改造成新的通用 Process 系统。

## 5. Dirty Queue

RecordPractice 后会把 characterId 放入 _practiceDirtyCharacters，用于后续突破检查/刷新。它是 Runtime Derived/Dirty Queue，不是领域事实，不应保存。

## 6. Save Schema 结论

| State | Scope | Save |
|---|---|---|
| CharacterResearchData | Character | 需要 |
| InnovationKnowledge.mastery | Character × Innovation | 需要 |
| InnovationKnowledge.practice | Character × Innovation | 需要 |
| ResearchPlanData | Plan | 需要 |
| _practiceDirtyCharacters | Runtime dirty | 不需要 |
| InnovationTree._realmInnovations | Realm/Social | 需要 |
| InnovationTree._realmResearchPoints | Realm | 兼容期需要 |
| InnovationTree._realmCurrentResearch | Realm | 兼容期需要 |
| InnovationTree._innovationProgress | 未闭合 | 暂缓 |

## 7. Matrix 更新规则

现在可以明确：InnovationKnowledge mastery、InnovationKnowledge practice、ResearchPlanSystem._data 都有明确目标 authority；_practiceDirtyCharacters 明确为 derived/non-save；_innovationProgress 仍 unresolved。

统一 Save 链：Runtime Authority → Save DTO → Load Adapter → Compatibility Removal。

## 8. 下一步

继续闭合 CharacterResearch 的 Save Adapter、InnovationKnowledge 的 Save Adapter、ResearchPlanData 的 Save Adapter，以及 InnovationTree._innovationProgress 的 scope。之后进入 Save Schema Design，而不是直接修改运行时代码。
