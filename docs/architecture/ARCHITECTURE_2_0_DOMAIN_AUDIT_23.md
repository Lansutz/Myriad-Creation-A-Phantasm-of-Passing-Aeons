# Architecture 2.0 Domain Audit 23

> 本轮继续闭合 InnovationKnowledgeSystem 字段、ResearchPlan 生命周期与 Save 边界。

## 1. InnovationKnowledgeSystem 的真实状态结构
当前已确认内部至少有两类个人知识状态：
- character×innovation 的 mastery 数据：`Dictionary<int, Dictionary<int, byte>>`。
- character×innovation 的 practice 数据：`Dictionary<string, float> _practiceByCharacterInnovation`。

这两个状态都以角色为第一作用域，因此它们不应归属于 InnovationTree 的社会 Innovation authority。

当前目标边界：
`Character → InnovationKnowledge → Mastery / Practice Evidence`。

## 2. ResearchPlanData 是独立于通用 Plan 的领域状态
ResearchPlanSystem 同时维护：
- `PlanSystem._plans` 中的通用 `Plan`；
- `_data : Dictionary<int, ResearchPlanData>` 中的研究专属数据。

ResearchPlanData 因此不是 Plan 的替代物，而是 Research Plan 的 domain extension/state。

## 3. ResearchPlan 创建路径已经确认
ResearchPlanSystem 创建 Research Plan 时：
`_plans.CreatePlan(PlanType.Research, characterId, innovationId, ...)`
然后建立自己的 `ResearchPlanData`。

这验证了 2.0 中：
`Plan = generic lifecycle`
`ResearchPlanData = research-specific state`。

## 4. Plan 生命周期由 PlanSystem 控制
通用 PlanSystem 明确拥有：
- CreatePlan
- CancelPlan
- CompletePlan
- ProcessStatus → Plan 状态转换。

因此 ResearchPlanExecutor 不应自行成为第二套通用 Plan lifecycle authority。

其职责应保持：
`ResearchPlan domain behavior → Process/Executor → PlanSystem lifecycle`。

## 5. ResearchPlan 的当前双路径
ResearchPlanExecutor 同时连接两套 Innovation 路径：

A. 旧 Realm Research：
`ResearchPlan → InnovationTree.StartResearch(realmId, innovationId)`。

B. Character Practice：
`ResearchPlan / Practice → InnovationKnowledgeSystem.RecordPractice(characterId, innovationId, amount)`。

所以 ResearchPlan 当前确实是两套 Innovation 生命周期之间的桥。

## 6. CharacterResearch Save 仍未闭合
`CharacterResearchData` 是 `[Serializable]`，但当前检索没有找到 `CharacterSaveData`、`SaveCharacter` 或 `LoadCharacter` 等专门的 Save/Load 入口。

这意味着 `[Serializable]` 不能被当成“已经进入 GameSaveData”的证据。

当前状态：
`CharacterResearch = Runtime state + Save unresolved`。

## 7. InnovationKnowledge Save 也未闭合
同样，InnovationKnowledgeSystem 内部 Dictionary 是运行时容器。

当前没有证据表明：
- mastery dictionary 已进入 SaveData；
- practice dictionary 已进入 SaveData；
- load 时已经恢复 character×innovation 状态。

因此必须在 Field Migration Matrix 中加入独立 Save Audit 项。

## 8. 新的领域关系图
`Character`
├─ `CharacterResearchData`
│  └─ 当前研究 / 能力 / 灵感 / 完成记录
│
└─ `InnovationKnowledge`
   ├─ mastery(character, innovation)
   └─ practice(character, innovation)

`ResearchPlan`
├─ generic Plan lifecycle → PlanSystem
└─ research state → ResearchPlanData

`InnovationTree`
├─ realm/social innovation state
├─ legacy realm research
└─ practice aggregation（scope unresolved）

## 9. Save Schema 新增风险
当前至少有以下尚未证明进入完整 Save/Load 的运行时状态：
- CharacterResearchData
- InnovationKnowledge mastery
- InnovationKnowledge practice
- ResearchPlanData
- InnovationTree practice aggregation
- InnovationTree realm research compatibility state

因此在真正执行 Domain Migration 前，需要建立独立的 Save Adapter 设计。

## 10. 本轮不迁移代码
原因：当前这些数据都有实际调用方，且 Save/Load 尚未闭合。

特别是不能因为某字段看起来是“旧系统”就删除；必须先证明：
`No Callers + No Save Dependency + Replacement Exists`。

## 11. 下一步
下一轮继续：
1. 查 InnovationKnowledgeSystem 的完整公开 API 与 mastery 生命周期。
2. 查 ResearchPlanData 全字段及创建/执行/完成/取消时的写入。
3. 查 CharacterManager / CharacterData 是否通过通用 DTO 间接保存 CharacterResearch。
4. 查 SaveSystem / GameSaveSystem 的全部 Character/Realm 字段。
5. 完成后回写两张 Migration Matrix。