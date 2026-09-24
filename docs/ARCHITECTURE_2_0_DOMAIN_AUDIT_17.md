# Architecture 2.0 Domain Audit 17

> 范围：InnovationProgress / CharacterResearch / ResearchPlan，以及 Fort / Barrier / Road / Save 边界。
> 本轮只做证据审计，不创建最终领域类，不执行字段迁移。

## 1. 革新：三层研究状态已经明确分层，但旧社会研究状态仍共存

### 1.1 InnovationProgress
- `InnovationProgress` 是单个 innovation 的实践驱动进度对象。
- 字段：`innovationId`、`progress`、`monthlyGain`、`isAvailable`、`lockedReason`、`gainBreakdown`、`cumulativeOutput`、`averageQuality`、`oldMethodPracticeCount`。
- 语义明显属于“社会/革新进度层”的状态，而不是角色个人状态，也不是 `ResearchPlan`。
- `cumulativeOutput`、`averageQuality` 与资源实践直接相关；`gainBreakdown`、`lockedReason` 明显包含 UI/解释性派生信息，后续不应默认视为核心权威状态。

### 1.2 当前关键问题：InnovationProgress 的作用域
- `InnovationTree` 当前以 `Dictionary<int, InnovationProgress>` 保存，key 只有 `innovationId`。
- 同一 `InnovationTree` 另有 `_realmResearchPoints` 与 `_realmCurrentResearch`，它们按 realm 区分。
- 因而当前代码同时存在“innovationId 单键实践进度”和“realm 单键旧研究状态”。
- 不能在迁移前假设 `InnovationProgress` 是 realm-local；必须先确定它究竟代表全球发现、社会知识，还是某一政权的共享实践。
- 在此问题解决前，不删除 `_realmResearchPoints` / `_realmCurrentResearch`，也不把 `InnovationProgress` 直接改成 `(realmId, innovationId)`。

## 2. CharacterResearch

`CharacterResearchData` 明确属于 Character → personal research 层：
- `characterId`：角色关联。
- `currentResearchInnovationId`：当前个人研究目标。
- `specialty`：个人研究领域。
- `researchAbility`：个人研究能力。
- `inspiration` / `isInspired` / `inspirationRemainingDays`：个人灵感状态。
- `totalResearchContribution`：个人累计研究贡献。
- `completedInnovations`：个人完成研究次数。

`CharacterResearchManager` 按 `characterId` 管理这些状态，并向 `InnovationTree` 提供 `GetResearcherCount` 与 `GetTotalResearchEfficiency`。

结论：
- CharacterResearch 不是 Innovation 社会知识的权威。
- CharacterResearch 也不是 PlanSystem 的替代品。
- 它是 Character domain 的个人研究能力/状态，与 InnovationKnowledge / InnovationProgress 发生跨域输入输出。
- `ResearchEfficiency` 是基于个人状态计算的值，不应成为 InnovationTree 的持久化权威。

## 3. ResearchPlan

`ResearchPlanSystem` 已明确写出边界：`PlanSystem` 负责生命周期，本系统负责革新领域规则。

`ResearchPlanData` 至少包含：
- `planId`
- `characterId`
- `realmId`
- `innovationId`
- `relevance`
- 其余字段应继续按完整文件逐项核对后再迁移。

当前流程是：
`实践记录 → InnovationKnowledge → 个人突破候选 → ResearchPlan → PlanSystem 生命周期 → 完成后写入 InnovationTree 兼容层`。

这进一步确认：
- Practice / Mastery 属于个人知识层。
- ResearchPlan 是个人意图/验证活动。
- InnovationTree 的社会解锁/兼容写入是结果边界，而不是 ResearchPlan 的内部状态。
- 不能把 ResearchPlan 并入 InnovationProgress，也不能把 PlanSystem 变成 Innovation 系统。

## 4. Fort：`fortInfluenceLevel` 是派生空间缓存，不应成为 MapCell 长期权威

当前 `TileData` 保存：
- `nearbyFortId`
- `fortInfluenceLevel`

`BarrierSystem` 的区域影响计算会先清空这两个字段，再根据堡垒、距离和影响等级写回。说明它们不是独立持久世界事实，而是由堡垒实体 + 空间关系计算出的缓存/派生结果。

`MilitaryMovementSystem` 使用 `fortInfluenceLevel` 计算：
- 己方堡垒的通行成本修正。
- 敌方堡垒的损耗/速度影响。
- `CalculateArmyLosses` 也直接读取该等级。

目标边界：
`Fort/Settlement Military Role → Fort Influence Query → Movement Query`。

因此：
- `nearbyFortId` 不应继续作为 MapCell 权威实体关系。
- `fortInfluenceLevel` 不应迁移为新的 MapCell 核心字段。
- 可保留短期 compatibility cache，但应最终由 Fort Influence Resolver/Query 生成。

## 5. Barrier / Gate：当前 TileData 仍承担完整状态权威

当前 `BarrierSystem` 明确区分：
- Barrier：狭窄通道的直接阻挡。
- Fort：区域控制，不直接阻挡。

BarrierSystem 会直接修改：
- `hasBarrier`
- `barrierOwnerRealmId`
- `barrierStrength`
- `isGate`

例如移除 Barrier 时会将这些字段直接清零/重置。这说明当前实现仍把 Passage Structure + Political ownership + durability 压缩进 TileData。

目标拆分：
- `Gate` / `Barrier`：Structure / Passage。
- `barrierOwnerRealmId`：Political ownership/control relation。
- `barrierStrength`：Structure fortification/durability state。
- `isGate`：Gate/Passage definition/state，而非 MapCell geography。
- Movement Query 根据 Structure + Political Passage + Unit context 得出实际通行结果。

## 6. Road：当前仍是 Building → TileData 的旧实现

当前唯一明确的生产路径是 `BuildingSystem` 的 `BuildingCategory.Road`：
`tile.roadLevel = ...`。

主要消费者包括：
- `BarrierSystem`：道路直接修正 movement cost。
- `MilitaryMovementSystem`：道路显示/通行判断。
- `Army`：直接根据 roadLevel 修正移动速度。
- `TradeRoute`：对 nodeTileIndices 的 roadLevel 求平均并修正效率。
- UI：直接显示 TileData.roadLevel。
- `MapSaveSystem` / `GameSaveSystem`：直接保存/加载 roadLevel。

因此 Road 当前不是纯地理字段，也不是单纯的 UI 派生值；它是 Infrastructure state，但其空间载体错误地放在 MapCell。

目标：
`RoadSegment / Bridge / RoadNetwork → Movement Query / Trade Query`。

迁移注意：
- 不能只把 `TileData.roadLevel` 重命名成 `MapCell.roadLevel`。
- 必须先解决“道路是跨 MapCell 的空间结构”这一事实。
- `TradeRoute` 不应继续依赖每个 Tile 的道路枚举作为最终权威；应查询路线上的 RoadNetwork/segments。
- Army 的直接 movement 逻辑也应最终收敛到统一 Movement Query。

## 7. Save：当前存档会固化旧 TileData 边界

`TileSaveData` 明确保存 `roadLevel`，`MapSaveSystem` 与 `GameSaveSystem` 都会写入并恢复该字段。

这意味着 Road 迁移不能只改运行时类型：必须设计 Save Schema migration。

当前证据还表明 TileSaveData 同时保存 `fertility / provinceId / ownerRealmId / occupyingRealmId / development / stability / order` 等多种领域状态。因此 Save DTO 当前是历史运行时结构的快照，不应被当作 2.0 Domain Model。

目标原则：
- Save Schema 独立于 Domain Model。
- 旧存档字段应通过版本迁移进入新的 Road/Structure/Political/Settlement DTO。
- 在迁移完成前，不删除旧存档字段。

## 8. 本轮 Field Authority 结论

| 字段 | 当前权威 | 2.0 目标 | 状态 |
|---|---|---|---|
| `InnovationProgress.progress` | InnovationTree | Innovation Progress/Knowledge layer | 待确认作用域 |
| `InnovationProgress.cumulativeOutput` | InnovationTree | Practice/Innovation knowledge | 待确认作用域 |
| `InnovationProgress.averageQuality` | InnovationTree | Practice/Production-derived input | 待确认 |
| `CharacterResearchData.*` | CharacterResearchManager | Character personal research | 已较清晰 |
| `ResearchPlanData.*` | ResearchPlanSystem | Research Plan | 已较清晰 |
| `TileData.nearbyFortId` | BarrierSystem cache | Fort Influence Query | 迁移候选 |
| `TileData.fortInfluenceLevel` | BarrierSystem cache | Fort Influence Query | 迁移候选 |
| `TileData.isGate` | TileData/BarrierSystem | Gate/Passage Structure | 迁移候选 |
| `TileData.hasBarrier` | TileData/BarrierSystem | Barrier Structure | 迁移候选 |
| `TileData.barrierOwnerRealmId` | TileData/BarrierSystem | Political relation | 迁移候选 |
| `TileData.barrierStrength` | TileData/BarrierSystem | Barrier durability | 迁移候选 |
| `TileData.roadLevel` | BuildingSystem/TileData | RoadSegment/RoadNetwork | 迁移候选 |
| `TradeRoute.isBlocked` | TradeRoute | Blockage Query | 待完整 call graph |

## 9. 下一轮必须继续核对

1. 完整读取 `ResearchPlanData` 全字段与 executor 的所有写入点。
2. 完整核对 InnovationTree `_innovationProgress`、`_realmResearchPoints`、`_realmCurrentResearch` 的所有读写调用。
3. 核对 CharacterResearch 与 CharacterManager/Save 的持久化关系。
4. 核对 FortData/BurgData 中 `fortification`、`garrison`、`fortSubtype` 与 `fortInfluenceLevel` 的真实关系，避免把“堡垒影响”误当作“堡垒本体”。
5. 完整核对 BarrierSystem 所有入口及其调用者。
6. 核对 SaveData 中 Innovation / CharacterResearch / ResearchPlan / Fort / Barrier 是否已有专门 DTO；没有证据不得假设。
7. 完成这些证据后，再建立正式 `ARCHITECTURE_2_0_DOMAIN_BOUNDARY_MATRIX.md` 与 `ARCHITECTURE_2_0_FIELD_MIGRATION_MATRIX.md`。

## 10. 禁止提前执行的迁移

- 不删除 `_realmResearchPoints` / `_realmCurrentResearch`。
- 不把 `InnovationProgress` 强行改为 realm-local。
- 不把 `fortInfluenceLevel` 搬进新 MapCell。
- 不把 Road 简单改成 MapCell 字段。
- 不让 GreatProject/GreatWall/Network 在本轮提前落类。
- 不把 Save DTO 当作 Domain Model。