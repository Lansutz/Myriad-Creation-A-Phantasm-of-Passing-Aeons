# Architecture 2.0 Domain Audit 20

> 本轮继续闭合 Save/Load 与 Innovation、CharacterResearch、ResearchPlan 的调用边界。结论只基于仓库中已检索到的实际代码证据。

## 1. Save 系统的总体结构
当前存在两套明确的存档入口：
- SaveSystem：v2、JsonUtility、可序列化 DTO、版本号。
- GameSaveSystem / GameSaveData：完整游戏存档入口。
- MapSaveSystem / MapSaveData：地图专项存档。
SaveSystem 明确说明 Dictionary/HashSet 需要转换为 List 包装，说明运行时集合不能直接假定为 Save Schema。

## 2. GameSaveData 当前覆盖范围
GameSaveData 的注释明确描述为地图、时间、政权、编年史等核心状态；GameSaveSystem 当前代码注释还明确写有“角色、军队、战争、外交等复杂数据后续逐步扩展”。
本轮代码搜索没有发现：
- InnovationTree 专用 Save DTO
- InnovationProgress 专用 Save DTO
- CharacterResearchData 专用 Save DTO
- ResearchPlanData 专用 Save DTO
- Barrier 专用 Save DTO
- Fort 专用 Save DTO
因此 Audit 19 的“专用 DTO 尚未证明存在”结论得到进一步确认。
**重要：不能把“没有专用 DTO”理解成“这些运行时状态已经确定不需要保存”。它只表示当前 Save Schema 尚未覆盖/尚未显式覆盖这些领域。**

## 3. Innovation：当前存在明显的 Save 缺口
InnovationTree 当前至少有以下状态：
- _realmInnovations : Dictionary<int, HashSet<int>>
- _realmResearchPoints : Dictionary<int, float>
- _realmCurrentResearch : Dictionary<int, int>
- _innovationProgress : Dictionary<int, InnovationProgress>
- _resourceCumulativeOutput : Dictionary<string, float>
其中 _realmInnovations 已被 CultureStageEvolutionSystem 等运行时系统读取，是实际世界状态；_realmResearchPoints / _realmCurrentResearch 属于旧研究生命周期；_innovationProgress 属于新实践驱动生命周期，但 scope 尚未确定；_resourceCumulativeOutput 明确是 realm × goods 的累计产量证据。
这些状态目前没有被证明已经进入 GameSaveData。
因此当前架构存在一个需要后续处理的明确问题：**Domain State 已经比 Save Schema 更丰富。**
不能在没有 Save 设计的情况下直接删除旧 Innovation state，也不能在不确认 scope 的情况下新增一个“万能 InnovationSaveData”。

## 4. InnovationProgress 的 scope 问题进一步确认
InnovationTree 明确以 innovationId 作为 _innovationProgress 的 Dictionary key。
但是实践推进入口 MonthlyTickProgress(...) 同时接收 realmId，而资源累计产量使用的是 realmId_goodsId key。
这意味着：
- practice progress 在调用层面具有 realm 输入；
- storage 层面却没有 realm key；
- 不同 realm 对同一 innovation 的实践可能写入同一个 InnovationProgress。
因此目前不能将其正式定义为 Realm Innovation Progress、Culture Innovation Progress 或 Global Innovation Progress。
必须在 Call Graph 审计后决定其真实语义。
**当前状态：UNRESOLVED，禁止迁移。**

## 5. CharacterResearch：属于角色域，不属于 InnovationTree
CharacterResearchData 已确认包含：characterId、currentResearchInnovationId、specialty、researchAbility、inspiration、isInspired、inspirationRemainingDays、totalResearchContribution、completedInnovations。
这组字段具有明显的个人状态性质。
同时 CharacterResearchSystem 的研究者数量/效率会被 InnovationTree 的实践进度使用。
因此关系应保持：
CharacterResearch → Practice Evidence → Innovation Progress
而不是：
InnovationTree → CharacterResearch State
CharacterResearch 的 Save Schema 后续应跟 Character domain 一起处理，而不是塞进 InnovationTree DTO。

## 6. ResearchPlan：仍是 Planning ↔ Innovation Bridge
ResearchPlanSystem 构造时依赖 GameWorld 与 PlanSystem。
GameWorldPlanning 也单独持有 _researchPlanSystem。
这再次证明 ResearchPlan 是计划/主体行为；CharacterResearch 是个人研究状态；InnovationTree 是社会/世界创新状态；三者不是同一个 Entity。
目标关系：
ResearchPlan → CharacterResearch / Research Activity → Evidence → Innovation State
ResearchPlan 不应成为 Innovation Save Schema 的子对象。

## 7. Burg/Fort Save 结构
仓库存在 BurgSaveData，说明 Settlement/Burg 已有专门 Save DTO。
但是当前 Fort 信息仍主要混在 BurgData/Settlement 数据中，而 nearbyFortId、fortInfluenceLevel 属于 BarrierSystem 计算出的空间影响缓存。
因此应区分：
1. Burg/Settlement 本体 Save。
2. Fortification/Garrison state。
3. Fort Influence derived state。
第三类不应简单复制进未来 MapCell Save DTO。

## 8. Road Save：已确认是旧架构的强兼容边界
TileSaveData 明确保存 roadLevel。
MapSaveSystem 与 GameSaveSystem 都会把 TileData.roadLevel 写入 Save Data。
同时运行时 BuildingSystem 修改 roadLevel；BarrierSystem / MilitaryMovementSystem、Army、TradeRoute、UI 都消费它。
所以 Road 不能在重构中顺手删掉 TileData.roadLevel。
正确迁移链：
TileSaveData.roadLevel → Road Migration Adapter → RoadSegment/RoadNetwork State
待新 Road Save Schema 完成后，再删除旧字段。

## 9. Barrier Save：目前没有独立 DTO 证据
Barrier 当前 authority 仍在 TileData：hasBarrier、isGate、barrierOwnerRealmId、barrierStrength。
BarrierSystem 会直接写这些字段。
因此未来必须把旧存档映射拆成：
- Barrier Structure state。
- Gate/Passage state。
- Political ownership relation。
不能让新的 Barrier DTO 继续把政治 owner 与结构 strength 混在一起。

## 10. 当前 Save Migration 风险等级
| Domain | Save 状态 | 当前处理 |
|---|---|---|
| Map Geography | 有 TileSaveData | 保留并逐步拆分 |
| Settlement/Burg | 有 BurgSaveData | 可作为迁移起点 |
| Road | TileSaveData 中有 | 强兼容字段 |
| Barrier/Gate | 未发现专用 DTO | 暂不迁移 |
| Fort Influence | 未发现专用 DTO | 作为 derived/cache |
| Innovation Social State | 未发现专用 DTO | 需要补齐 Save 设计 |
| Innovation Practice Progress | 未发现专用 DTO | scope 未定 |
| CharacterResearch | 未发现专用 DTO | 跟 Character Save 边界处理 |
| ResearchPlan | 未发现专用 DTO | 跟 Planning Save 边界处理 |
| GreatProject | 未发现专用 DTO | 尚未进入实现阶段 |

## 11. 本轮结论
现在可以明确一个更重要的 Architecture 2.0 原则：
**Save Schema Audit 必须先于大规模 Domain Migration。**
否则会出现：旧 TileData 拆掉 → 新 Domain 正常运行 → Save/Load 丢失状态。
因此后续 Field Migration 必须增加：
Runtime Authority → Query/Derived → Save Authority → Load Migration → Compatibility Removal
五段链路。

## 12. 下一步
下一轮优先继续做：
1. GameSaveSystem 的完整字段覆盖审计。
2. Character/Realm 当前 Save DTO 与运行时对象的映射。
3. InnovationTree 所有公开读写 API 的 Call Graph。
4. ResearchPlan 生命周期与 PlanSystem 的实际数据关系。
5. BarrierSystem / BurgSaveData 的具体 Save 缺口。
完成后再更新 Field Migration Matrix。

**暂不创建任何最终 Domain 类，也暂不删除旧字段。**