# Architecture 2.0 Domain Audit 28

## InnovationProgress 与 Legacy Research 生命周期闭合

### 1. 关键事实：当前并非一个研究进度系统

`InnovationTree` 实际同时维护三种不同粒度的状态：

1. `_realmInnovations[realmId]`：政权已经正式拥有的社会革新。
2. `_realmCurrentResearch[realmId] + _realmResearchPoints[realmId]`：旧的政权研究生命周期。
3. `_innovationProgress[innovationId]`：新的实践驱动革新进度。

因此不能把 `_innovationProgress` 简单视为 `_realmResearchPoints` 的替代品。

### 2. Legacy Realm Research

实际生命周期：
`AIController / ResearchPlanSystem → StartResearch(realmId, innovationId) → _realmCurrentResearch → DailyTick(realmId, researchRate) → _realmResearchPoints → CompleteResearch → _realmInnovations`。

AIController 仍然直接选择并启动政权研究；ResearchPlanSystem 的 `TryFormalizeResearch` 也依赖同一条旧链。

现有 EditMode 测试同样直接验证 `StartResearch → DailyTick → HasInnovation`。

结论：在兼容阶段必须保留并保存 `_realmCurrentResearch` 与 `_realmResearchPoints`，否则读档会改变 AI 和研究计划的行为。

### 3. InnovationProgress

`_innovationProgress` 的 key 明确只有 `innovationId`。

但 `MonthlyTickProgress` 同时接收 `realmId`，并在计算时读取：
- `HasInnovation(realmId, innovationId)`
- realm-specific resource output
- realm-specific cumulative resource output
- character researcher count
- facility availability

因此它实际上使用了政权作为输入上下文，却没有把 realmId 纳入进度容器 key。

这产生一个非常重要的架构事实：

`InnovationProgress` 当前是**跨政权共享的革新进度聚合状态**，至少从容器结构上如此；但它又由多个政权分别推进。

这不是一个安全的最终 2.0 authority。若 A 政权与 B 政权都调用同一 innovation 的 `MonthlyTickProgress`，两者会写入同一个 `InnovationProgress`。

### 4. 因此当前模型存在 scope mismatch

实际计算意图更接近：
`Realm × Innovation × Practice Evidence → Progress`。

实际存储却是：
`Innovation → Progress`。

这意味着 `_innovationProgress` 很可能是旧模型向实践驱动模型迁移时留下的共享聚合层，而不是最终应保存的政权状态。

### 5. `_resourceCumulativeOutput` 的边界

该字典 key 是 `realmId_goodsId`，因此明确属于 `Realm × Goods` 的经济实践证据。

它被 `HasResource` 用于累计产量门槛，也由 `MonthlyTickProgress` 更新。

因此它不应该作为 InnovationProgress 的内部状态长期存在；最终应归入 Economy/Production evidence，Innovation 通过 Query 获取。

### 6. 当前 Save 结论

| 状态 | 当前 authority | Save 结论 |
|---|---|---|
| `_realmInnovations` | Realm/Social Innovation | 必须保存 |
| `_realmCurrentResearch` | Legacy Realm Research | 兼容期必须保存 |
| `_realmResearchPoints` | Legacy Realm Research | 兼容期必须保存 |
| `_innovationProgress` | Innovation practice aggregation，scope mismatch | 暂不进入最终 Schema |
| `_resourceCumulativeOutput` | Economy/Production evidence | 不应归 Innovation Save |

### 7. 下一步

下一阶段应继续追 `MonthlyTickProgress` 的所有调用方，确认到底是谁按月推进它，以及是否已经存在真正的 Realm × Innovation 实践证据。

若调用方确认后仍无独立政权进度容器，则 2.0 应设计：
`PracticeEvidence Query → InnovationProgressCalculator → RealmInnovationProgress`，而不是继续扩大当前 `_innovationProgress`。

当前不修改运行时代码、不删除旧链、不迁移字段。