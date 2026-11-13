# P4 欠账收口计划（宫廷 / 远征 / 城市视图 / 生态与物种）

> 目标：把 P3 明确留下的四项欠账做完，并保持既有总原则：架构分层、文件/代码架构、可模组化、性能预算、模拟真实性（机制优先于数值）、确定性、旧档兼容。
> 前置事实：四项都在 DECISIONS 有明确欠账理由（D118/D120/D123/D125），本计划逐项给出可验证的收口条件。

---

## 0. 欠账与根因

| 欠账 | 根因 | 收口方向 |
|---|---|---|
| `Exploration` 不启用（D125） | 只有地图知识层，没有“远征实体/路线”过程载体 | 新增远征战团实体 + 路线 + 知识揭示 + 存档，接入 `PlanType.Exploration` |
| 城市俯视图只有布局数据（D123） | `CityLayoutGenerator` 已产 `CityLayoutPlan`，缺 Sprite 渲染与 UI 入口 | Rendering 侧把布局光栅化成 Sprite；UI 新增独立面板并注册 |
| `taxation` 未硬门禁（D118） | 世界生成只有君主+配偶，硬接会让前十几年国库归零 | 先补宫廷人物供给，再按 `taxation` 模块硬门禁 |
| 独立物种/渔业/火灾/放牧未做（D120） | 只有聚合 `wildlife`，没有物种表/水生/扰动反馈 | 物种表 + 渔业库存 + 火/放牧反馈，接既有生态 tick |

---

## 1. 总原则（继承，不另立）

1. 纯定义进 `Content.Definitions`；运行逻辑在 Simulation/World；不建第二套平行系统。
2. 新内容走 `Base/...` JSON + `Mods` 覆盖；坏条目跳过并告警。
3. 每系统声明复杂度；查询不写状态；内容只在初始化读一次。
4. 表型/生态推导是纯函数可并行；写状态在主 tick 锁步。
5. 机制优先：宫廷人物是“官职能被担任”的条件；远征是“到得了才知得道”；渔业/火/放牧改变的是库存与植被，不是抽象加成。
6. 确定性：随机只来自 world seed 派生；同一 seed 同结果。
7. 旧档：新字段可选、缺省有语义；存档往返稳定。
8. 每批：先读代码 → 测试 → 实现 → `build` 全绿 → `DECISIONS/PROJECT` → 提交推送。

---

## 2. 阶段

### P4.0 宫廷人物供给 + 征税硬门禁（前置）
- `CharacterManager.CreateInitialRulers` 为每个政权补 2–4 名宫廷人物（Noble/Scholar/Military，成年、同文化/种族、非君主家族），使官职在开局就能被担任。
- `EnsureOfficeHolders` 在世界生成结束时调用一次；`offices` 调度回到每日（空槽时才扫人，已满时 O(槽位)），保证holder死亡后的空窗不超过一天。
- `PoliticsSimulationSystem` 恢复 `hasCollector = OfficeSystem.HasModuleHolder(realm, taxation, ...)`；无征税官时本日税收为 0（不是税率归零）。
- 验收：新世界第一个整日有税收；拆掉/杀死征税官后税收归零、补缺后恢复；`longrun` 经济不崩。
- 测试：宫廷人数、官职开局填充、税收门禁正反向。

### P4.1 远征战团与 Exploration 执行器
- 新增 `Expedition`（`Simulation.State`）：id/ownerRealmId/leaderCharacterId/tileIndex/route/state/startDay/returnDay/supplies；`ExpeditionSaveData` 入档。
- `ExpeditionSystem`：按路线逐格移动，进入未知识地块时 `Knowledge.RevealRadius(ownerRealm, tile, 1..2)`；补给耗尽/路线走完返航；途中可被敌对军队拦截（接 `DetectionContactService`）。
- `ExplorationPlanSystem`：发起需目标地块与路线可达（已知起点/港口/边界），进度由远征位置驱动；完成/失败/中断写编年史；`PlanType.Exploration` 注册。
- 调用者：AI 每 365 日对“已知边缘外的目标”发起一次；玩家入口暂由 API + 后续 UI。
- 验收：同一 seed 下远征路线确定；走过后目标地块变已知；补给耗尽/被拦截会失败并留编年史；存档往返。
- 测试：路线移动、知识揭示、失败分支、存档往返、计划生命周期。

### P4.2 城市俯视 Sprite + UI 入口
- Rendering：`CityLayoutRenderer` 把 `CityLayoutPlan` 光栅化为 `Texture2D`（街区/道路/城墙三色；尺寸/像素由布局推导，不存档）；同 `settlementId` 同图。
- UI：新增 `CityViewPanel`（一个 UI 一个文件），通过 `UiPanelRegistry` 注册；`SettlementDetailPanel` 增加“城市视图”入口按钮，打开当前聚落布局；无布局/空聚落显示合法空态。
- 性能：纹理按需生成、缓存 LRU（例如 8 个），不参与主 tick。
- 验收：同聚落重复打开同图；不同聚落可区分；无布局不崩；UI 注册表可查。
- 测试：布局→纹理像素采样、确定性、缓存命中/淘汰、面板注册与空态。

### P4.3 生态扩展：渔业、火灾、放牧反馈
- `TileData.fishStock`（0–1，海洋/沿海）；`EcosystemSystem` 月度更新：沿海按 NPP/温度恢复，捕捞按人口压力扣减；`EconomyManager` 从沿海地块捕鱼写入 2 号货，与狩猎同源扣减。
- 火灾：干燥（降水低/温度高）+ 植被高时按确定性 hash 触发；烧掉植被/野生/鱼群一部分，写编年史事件；不新建灾害系统，复用 `GameEvent`/Chronicle。
- 放牧：新增 `TileData.livestock` 或复用人口块？优先内容/机制最小化：以 `PopulationBlock` 的牧业阶层或 `development` 代理放牧压力，压低植被目标；若需要新字段则入档。
- 验收：沿海捕鱼产 2 号货且扣鱼群；火灾后植被下降、可恢复；放牧压力下植被低于无牧地块。
- 测试：渔业正反向、火灾确定性/恢复、放牧压力。

### P4.4 独立物种表（最小可用）
- `Base/Ecology/Species.json`：生态物种条目（id/name/category/diet/habitat/bodySize/fecundity/role），与遗传 `Species.json` 通过 `speciesId` 对齐，不复制遗传定义。
- `SpeciesCatalog`（Content 或 Genetics 目录）提供按群系/水域的物种池；`EcosystemSystem` 用物种池的食性/体型计算承载与狩猎优先级，不存逐格物种清单（避免预算爆炸）。
- 验收：同一群系物种池确定；食性/体型影响承载与捕猎；内容可 Mod 覆盖；无逐格物种数组。
- 测试：物种池解析、承载差异、Mod 覆盖。

### P4.5 收口
- `build` `EXIT=0`、零 `error CS`、EditMode 全绿；`longrun` 2 seed×100 年通过；D132–D136 + PROJECT 同步。

---

## 3. 非目标

- 不做逐格物种个体模拟、完整食物网、动物迁徙动画。
- 不做城市俯视的 3D 模型/街道级寻路；只做矩形布局到 Sprite。
- 不重做税收/经济模型；只补“征收者”条件。
- 不为远征做完整海军战斗；拦截走既有遭遇/战斗接口。

## 4. 验收清单

1. 新世界开局有宫廷人物、官职填充、第一日有税收；无征税官税收为 0。
2. 远征实体可移动、揭示知识、失败留痕、入档；`Exploration` 计划可完成/失败。
3. 城市布局可生成确定性 Sprite，UI 面板注册且能从聚落面板打开。
4. 渔业产 2 号货并扣鱼群；火灾确定性触发且可恢复；放牧压力压低植被。
5. 生态物种表可加载/Mod 覆盖，承载与捕猎受食性/体型影响。
6. 最终 `build` / `longrun` 全绿，文档同步。
