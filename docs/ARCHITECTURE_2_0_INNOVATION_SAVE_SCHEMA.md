# Architecture 2.0 Innovation Save Schema Draft 01

## 目标
为革新/研究域建立独立 Save Schema，不把运行时对象直接序列化，也不把研究状态塞进 RealmBasicSaveData。

当前证据已经明确四类持久化状态：
1. Realm/Social Innovation State
2. Character Research State
3. Character Innovation Knowledge State
4. Research Plan State

## 1. Realm Innovation
建议 DTO：RealmInnovationSaveData
- realmId
- innovationIds[]
- legacyResearchPoints
- legacyCurrentResearchInnovationId

innovationIds 对应 InnovationTree 的 _realmInnovations[realmId]。后两项是兼容状态，不代表最终 2.0 Innovation 模型。

## 2. Character Research
建议 DTO：CharacterResearchSaveData
- characterId
- realmId
- currentResearchInnovationId
- specialty
- researchAbility
- inspiration
- isInspired
- inspirationRemainingDays
- totalResearchContribution
- completedInnovations

当前 CharacterResearchManager 是每个政权一个管理器，内部以 characterId 为键。因此 DTO 必须保存 realmId + characterId。

## 3. Character Innovation Knowledge
建议 DTO：CharacterInnovationKnowledgeSaveData
- characterId
- mastery[]: innovationId + level
- practice[]: innovationId + amount

mastery 与 practice 都是 Character × Innovation，但语义不同，不能合并为一个字段。

## 4. Research Plan
建议 DTO：ResearchPlanSaveData
- planId
- characterId
- realmId
- innovationId
- relevance
- discovered
- formallyUnlocked
- verificationProgress
- genericPlanState（引用统一 Plan Save Schema，而不是复制 PlanSystem 全部运行时对象）

ResearchPlanData 是 Innovation 域专属状态，Plan 的通用生命周期仍由 PlanSystem 管理。

## 5. InnovationProgress：暂不定稿
当前 InnovationTree._innovationProgress 的 key 只有 innovationId，但 MonthlyTickProgress 输入包含 realmId。

InnovationProgress 当前包含 progress、monthlyGain、isAvailable、lockedReason、gainBreakdown、cumulativeOutput、averageQuality、oldMethodPracticeCount。

目前不能证明它是全球共享、单政权、文化/社会群体，或迁移残留的旧聚合层。因此暂不进入最终 Save Schema。

## 6. Load 顺序
目标：Content Registry → GameSaveData 基础世界 → RealmBasicSaveData → RealmInnovationSaveData → Character/CharacterResearch → CharacterInnovationKnowledge → ResearchPlan + generic Plan state → Derived/Dirty/Query rebuild。

不能从 InnovationDef 反推已完成革新；不能从 CharacterResearch 反推 InnovationKnowledge；不能从 ResearchPlan 反推社会已解锁状态；不能从 InnovationTree 反推 CharacterResearch。

## 7. 当前不实现
本文件只是 Schema 设计，不修改运行时代码。

暂不删除旧 Realm Research、删除 InnovationProgress、合并 CharacterResearch 与 InnovationKnowledge、修改 InnovationTree authority、修改 PlanSystem、接入 Save/Load，也不声称存档已经完整。

下一步：审计 InnovationTree._innovationProgress 的全部写入/读取路径，并确认 legacy research 的完整调用链，再实现 DTO 与 Save Adapter。