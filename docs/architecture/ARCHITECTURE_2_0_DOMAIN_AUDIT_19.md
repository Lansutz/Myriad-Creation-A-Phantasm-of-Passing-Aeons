# Architecture 2.0 Domain Audit 19

> 本轮收口 Save DTO 证据，并确认 Innovation / Research / Fort / Barrier / Road 当前是否存在独立持久化边界。

## 1. Save Schema 现状：Road 有明确持久化，Innovation/Research 专属 DTO 未发现

当前代码搜索没有发现 `InnovationTreeDTO`、`InnovationSaveData`、`CharacterResearchDTO`、`ResearchPlanDTO` 等独立存档 DTO。

相反，Road 明确存在于 `TileSaveData.roadLevel`，并由 `MapSaveSystem` 与 `GameSaveSystem` 从 `TileData.roadLevel` 写入/恢复。

因此目前可以确定：
- Road 是旧 TileData Save Schema 的一部分。
- Innovation / CharacterResearch / ResearchPlan 尚未证明具有独立 Save Schema。
- 不能据此断言这些系统完全不保存；它们可能通过 Realm/Character/通用世界对象间接保存，但当前证据不足。

## 2. ResearchPlanData 的性质

`ResearchPlanData` 是 `[Serializable]` 数据类，由 `ResearchPlanSystem` 管理。

已确认至少有：
- `planId`
- `characterId`
- `realmId`
- `innovationId`。

它属于运行时计划状态，而不是已证明的 Save DTO。

因此在 Domain 2.0 中仍应保持：
`ResearchPlanData ≠ Save DTO ≠ InnovationProgress`。

## 3. Innovation 的持久化边界目前仍未闭合

当前 InnovationTree 中至少存在：
- `_realmInnovations`
- `_realmResearchPoints`
- `_realmCurrentResearch`
- `_innovationProgress`
- `_resourceCumulativeOutput`。

但没有发现与这些字段一一对应的专用 DTO。

这说明下一阶段不能直接创建一个笼统的 `InnovationSaveData` 把所有字段照搬进去。

正确顺序应该是先确定每一项的**领域权威 + 作用域 + 是否可重建**，再决定 Save Schema。

## 4. Road 的 Save 边界已经可以明确

`TileSaveData.roadLevel` 当前是持久化字段，而 Road 本身实际上由 BuildingSystem 修改：
`BuildingCategory.Road → TileData.roadLevel`。

同时它被：
- BarrierSystem / Movement 读取。
- MilitaryMovementSystem 读取。
- Army 读取。
- TradeRoute 读取。
- UI 读取。
- MapSaveSystem / GameSaveSystem 保存。

因此 Road 是一个真正的跨域共享状态，而不是纯 Query 结果。

2.0 的正确方向是：
`RoadSegment/RoadNetwork state → Save DTO`，
而不是：
`MapCell.roadLevel → Save DTO`。

旧 `TileSaveData.roadLevel` 应在存档迁移期作为 compatibility input。

## 5. Barrier / Gate 的 Save 风险

搜索证据显示 Barrier 的核心字段仍在 `TileData`：
- `hasBarrier`
- `isGate`
- `barrierOwnerRealmId`
- `barrierStrength`。

本轮未找到专门的 BarrierSaveData，因此不能假设 Barrier 已经拥有独立存档结构。

这意味着未来拆分 Barrier Structure 时，必须提供旧 TileSaveData → Structure/Political DTO 的迁移步骤。

特别注意：`barrierOwnerRealmId` 与 `barrierStrength` 不是同一种数据：
- owner 是政治关系。
- strength 是结构状态。

二者不能在新 DTO 中继续作为一个混合字段组处理。

## 6. Fort 的 Save 风险更明显

Fort 本体当前嵌入 `BurgData` / Settlement 状态；而 `nearbyFortId` 与 `fortInfluenceLevel` 属于 MapCell 派生缓存。

因此未来 Save Schema 应分别考虑：
- Settlement/Fort 本体 DTO。
- Garrison / Fortification 状态。
- Fort Influence 是否可重建。

`nearbyFortId` / `fortInfluenceLevel` 原则上不应作为最终 Save Authority；如果它们可以从 Fort + Geography 重新计算，就应作为 derived cache 而不是核心存档事实。

## 7. InnovationProgress 是否需要保存：现在不能直接决定

这里存在一个关键问题：

`InnovationProgress` 当前 key=innovationId，但 `MonthlyTickProgress` 接受 realmId。

如果它最终被定义为：
- 全球/文明共享知识：可以按 innovation 保存。
- realm-specific social practice：必须加入 realm scope。
- culture-specific knowledge：必须加入 culture scope。

三种 Save Schema 完全不同。

所以当前阶段只记录：
`InnovationProgress = persistent candidate, scope unresolved`。

不能提前设计最终 DTO。

## 8. 正式 Domain Boundary Matrix 的准备状态

Audit 04–19 已经足以建立第一版矩阵，但矩阵必须保留 unresolved 状态，而不是假装所有字段都已确定。

目前可以确定的强边界：

| Domain | 当前结论 |
|---|---|
| Map/MapCell | Geography/Hydrology 为主，不承载 Road/Barrier/Fort Influence authority |
| Anchor | 空间存在与位置关系 |
| Settlement | Settlement state / evolution / destruction / political role |
| Building | Settlement-local building |
| Structure | RoadSegment / Barrier / Gate / Wall 等空间结构 |
| Network | TradeRoute / RoadNetwork / FrontierDefenseNetwork 等路线系统 |
| Political Relation | owner/control/occupation/passage/settlement control |
| Fort Influence | Query / Derived |
| Movement | Query/Resolver |
| Innovation Definition | Definition Registry |
| Innovation Social State | `_realmInnovations` 候选 authority |
| Innovation Practice Progress | `_innovationProgress`，scope unresolved |
| Character Research | Character domain |
| Research Plan | Planning bridge |
| Economy Evidence | realm×resource cumulative output，未来应由 Economy/Production 提供 |
| Save | 独立 DTO/Schema，不等同 Domain Model |

## 9. 下一步：开始建立正式矩阵

现在证据已经足以开始写：
`docs/ARCHITECTURE_2_0_DOMAIN_BOUNDARY_MATRIX.md`
`docs/ARCHITECTURE_2_0_FIELD_MIGRATION_MATRIX.md`。

矩阵第一版应覆盖：
MapCell、Anchor、Settlement、Building、Structure、Network、Political、Movement、Economy、Innovation、CharacterResearch、ResearchPlan、GreatProject、Save。

每个字段必须标记：
`Current Authority / Target Authority / Scope / Derived? / Save? / Compatibility? / Migration Status / Evidence / Unresolved`。

仍然禁止直接迁移代码；矩阵先作为后续重构的唯一依据。