# Architecture 2.0 Domain Audit 18

> 本轮继续完成 Innovation 研究状态与 Fort/Barrier 的调用关系核对。仍不执行最终迁移。

## 1. InnovationTree 目前实际上包含两套不同生命周期

### A. 旧的 realm research 生命周期
`_realmCurrentResearch[realmId] → _realmResearchPoints[realmId] → CompleteResearch → _realmInnovations[realmId]`。

这套模型仍由 `StartResearch / DailyTick / CompleteResearch / GetResearchProgress / GetCurrentResearch` 完整支撑，并且完成事件携带 `(realmId, innovationId)`。

### B. 新的 practice-driven progress 生命周期
`MonthlyTickProgress(world, realmId, innovationId, monthlyOutput, averageQuality, researcherCount, hasFacility) → InnovationProgress`。

这里 `InnovationProgress` 的 key 仍只有 `innovationId`，但输入包含 `realmId`，同时 `_resourceCumulativeOutput` 又明确使用 `realmId_goodsId` 作为 key。

这是目前最重要的结构性矛盾：
- 实践来源按 realm 计算。
- 资源累计按 realm 计算。
- 但革新进度本体不按 realm 存储。
- 同一个 `innovationId` 的不同政权调用 `MonthlyTickProgress` 会共享同一 `InnovationProgress`。

因此当前实现并不能证明“InnovationProgress = 全球社会知识”；它更准确地属于一个**正在形成、但作用域尚未完成定义的实践进度聚合**。

## 2. `_realmInnovations` 与 InnovationProgress 也不是同一回事

`_realmInnovations` 明确表示某 realm 已拥有某 innovation，并被大量政府改革、社会阶层、文化阶段、AI 等系统查询。

因此至少存在三个不同概念：

1. `InnovationDef`：革新的静态定义。
2. `InnovationProgress`：实践积累/研究进度，目前作用域设计存在缺陷。
3. `_realmInnovations`：realm 已获得的社会/制度性革新状态。

后续不能把三者合并为一个 `InnovationKnowledge` 对象。

## 3. CharacterResearch → InnovationTree 的关系已经得到直接证据

`CharacterResearchSystem.GetResearcherCount(innovationId)` 被设计为给 `InnovationTree.MonthlyTickProgress(... researcherCount ...)` 提供参数。

这意味着当前调用链是：
`CharacterResearch → researcherCount → InnovationProgress`。

角色研究本身并不直接拥有社会革新完成状态；它是实践进度的一个输入来源。

因此 2.0 边界应保持：
`Character Research State → Innovation Practice/Knowledge Input → Social Innovation State`。

而不是：
`CharacterResearch → InnovationTree 内部字段共享`。

## 4. ResearchPlan 的定位进一步稳定

`ResearchPlanSystem` 构造函数显式接收 `GameWorld + PlanSystem`，而 `GameWorldPlanning` 单独持有 `_researchPlanSystem`。

这说明 ResearchPlan 是 Planning domain 与 Innovation domain 的桥接层，而不是 InnovationTree 的内部数据结构。

目标链应保持：
`Character Intent → ResearchPlan → Activity/Process → CharacterResearch → Innovation Effect/Knowledge`。

ResearchPlan 不应成为社会革新状态容器。

## 5. Resource cumulative output 的真实作用域

`InnovationTree._resourceCumulativeOutput` 使用 `realmId_goodsId` key，并由 `MonthlyTickProgress` 根据相关资源月产量累加。

因此它是**政权×物产**的累计实践指标，不是 InnovationProgress 本身的全局指标。

这也意味着未来应该把它从 InnovationTree 大聚合中拆成：
`Production/Economy → Practice Evidence → Innovation Query`。

不能让 Innovation domain 永久直接扫描 `GameWorld.tiles` 作为生产数据权威。

## 6. Fort 本体与 Fort Influence 已经可以明确拆开

`BurgData` 同时保存：
- `fortification`
- `garrison`
- `fortSubtype`
- `wallLevel`
- `settlementType == Fort` / `BurgType.Fortress` 等定居点/军事属性。

而 `BarrierSystem.CalculateFortInfluence` 只根据堡垒的 settlement 类型、`tileIndex`、`settlementLevel` 等信息计算：
- `nearbyFortId`
- `fortInfluenceLevel`。

所以必须区分：

`Fortification/Fort Settlement State` ≠ `Fort Influence`。

前者是堡垒本体；后者是空间派生关系。

## 7. Fort Influence 的当前计算仍有一个重要问题

当前 `CalculateFortInfluence` 选择的是“最近或更强的堡垒”，并把单一 `nearbyFortId + fortInfluenceLevel` 写入 MapCell。

这意味着它实际上丢失了多个堡垒重叠影响的完整关系，只保留一个代表堡垒。

因此 2.0 的 `FortInfluenceQuery` 不应机械复刻 `nearbyFortId` 字段。

未来查询至少应能够表达：
- 一个或多个有效 Fort source。
- 每个 source 的距离/影响等级。
- 所属 realm。
- 对 Movement / Supply / Attrition 的不同效果。

具体是否需要完整列表，要等军事移动规则最终审计后决定。

## 8. Barrier 当前仍是 TileData authority

`BuildBarrier / BreachBarrier / CaptureBarrier` 直接修改 `TileData`：
- `hasBarrier`
- `barrierOwnerRealmId`
- `barrierStrength`
- `isGate`。

同时 `movementCost` 也被 Build/Breach 直接乘系数。

这里存在两个需要分离的问题：

### 8.1 Structure State
Barrier/Gate 的存在、耐久、所有权等应成为独立 Structure/Political state。

### 8.2 Movement Cost
`movementCost` 不应因为某个 Barrier 操作永久乘一次后就成为新的基础事实；它应该由 Geography + Road + Passage + Unit 等输入重新计算。

否则重复建造/攻破可能产生累计乘法漂移。

因此这部分迁移时必须特别检查旧存档和运行时初始化，不能只做字段搬家。

## 9. 本轮正式 Field Boundary 更新

| 字段/状态 | 当前语义 | 2.0 目标 |
|---|---|---|
| `InnovationDef` | 静态定义 | Innovation Definition Registry |
| `_realmInnovations` | realm 已获得革新 | Social/Realm Innovation Knowledge |
| `_realmResearchPoints` | realm 旧研究点 | Legacy/Compatibility，待迁移 |
| `_realmCurrentResearch` | realm 旧研究目标 | Legacy/Compatibility，待迁移 |
| `_innovationProgress` | practice progress，但当前错误/未定作用域 | Innovation Practice Progress，作用域待定 |
| `_resourceCumulativeOutput` | realm×resource累计实践 | Economy/Production Evidence |
| `CharacterResearchData` | 个人研究状态 | Character Research |
| `ResearchPlanData` | 个人研究计划 | Research Plan |
| `BurgData.fortification` | 聚落/堡垒本体防御状态 | Settlement/Fortification |
| `BurgData.garrison` | 聚落驻军 | Military/Settlement Garrison |
| `BurgData.fortSubtype` | 堡垒空间/军事类型 | Settlement/Fort Definition/Role |
| `TileData.nearbyFortId` | Fort influence cache | Fort Influence Query cache/derived |
| `TileData.fortInfluenceLevel` | Fort influence cache | Fort Influence Query derived |
| `TileData.hasBarrier` | Barrier existence | Barrier Structure |
| `TileData.isGate` | Passage/Gate flag | Gate Structure/Passage |
| `TileData.barrierOwnerRealmId` | Barrier ownership | Political relation |
| `TileData.barrierStrength` | Barrier durability | Barrier Structure state |
| `TileData.movementCost` | 当前混合基础/派生值 | Geography/Movement Query derived |

## 10. 迁移前禁止事项

- 不把 `InnovationProgress` 直接改成 realm-keyed，除非先确认设计目标。
- 不把它直接改成 global/social keyed，当前代码也不足以证明这一点。
- 不把 `_resourceCumulativeOutput` 留在 InnovationTree 作为长期经济权威。
- 不把 `nearbyFortId` / `fortInfluenceLevel` 迁移为 MapCell 新字段。
- 不把 `movementCost` 当成静态 Geography authority。
- 不把 Fort 与 Barrier 合并。
- 不把 ResearchPlan 合并进 CharacterResearch 或 InnovationTree。

## 11. 下一步

下一轮进入两个收口工作：
1. 完整确认 Save DTO 对 Innovation / CharacterResearch / ResearchPlan / Fort / Barrier 的覆盖范围。
2. 基于 Audit 04–18 证据建立正式 `ARCHITECTURE_2_0_DOMAIN_BOUNDARY_MATRIX.md` 与 `ARCHITECTURE_2_0_FIELD_MIGRATION_MATRIX.md`，但矩阵仍标记所有 unresolved authority，不提前执行迁移。