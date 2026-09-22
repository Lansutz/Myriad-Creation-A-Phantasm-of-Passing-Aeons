# Architecture 2.0 — Domain Audit 02

## 本轮结论

进一步检索后，最关键的事实是：**现有代码已经自然长出了 Query、Rule、Process 的雏形，但它们没有被统一建模。**

因此下一步不是创建大量新框架，而是把已有职责重新命名、归类，并逐步切断 Entity → Simulation Logic 的直接依赖。

## 1. Army：明确进入第一批迁移

当前 Army 已同时承担：
- GetTotalManpower
- CalculateCombatPower
- ReinforceUnit
- MoveTick
- DailyTick/移动运行状态

这些实际上属于四类不同职责：

| 当前方法 | 目标语义 |
|---|---|
| GetTotalManpower | Warfare Query |
| CalculateCombatPower | Warfare Query |
| ReinforceUnit | Warfare Action + Effect |
| MoveTick | Movement Process |
| DailyTick | Simulation Process，不属于 Army Entity |

这说明 Army 不需要重新设计成完全不同的数据结构；首先只需要把行为语义从 Entity 中剥离。

## 2. Population：已经存在 Query 原型

现有 PopulationStats 已经承担：
- GetClassShare
- Dominant Culture/Faith 等统计

ManpowerSystem 也已经从 TileData 计算政权级人力池。

因此 PopulationQuery 不应另起一套完全重复的 API。

正确做法是先把现有 PopulationStats / ManpowerSystem 的职责分类：

- Local Population Query
- Realm Population Query
- Manpower Query
- Social Composition Query

然后统一接口，而不是立即删除旧类。

## 3. PopulationBlock：保留 struct 方向

当前 PopulationBlock 是 struct，并通过 TileData.populationBlocks 承载。

这对于大规模人口模拟仍然合理。

Architecture 2.0 不应该因为“Entity/Relation 分离”而机械地把每个 PopulationBlock 变成 class。

真正应该分离的是：

PopulationBlock = 高密度事实状态
PopulationStats = Query
Migration/Conversion/Differentiation = Action/Process

## 4. Culture：已经具有 Definition 倾向

CultureData 被直接作为 CultureData.json 的内容模型，并包含文化名称、颜色、七维文化基因等内容。

这意味着 CultureData 实际上已经是一个 Content Definition，只是后续又被运行时逻辑直接使用。

README 也明确把 CultureData 作为文化包数据。

因此 Culture 的第一步不是复制成两个类后立刻全量迁移，而是确定：

CultureDefinition = 内容包/静态文化性质
CultureState = 世界中该文化当前状态

尤其 maturity、spread 等动态量不能继续进入 Definition。

## 5. Faith：命名与模型需要一起处理

FaithSystem 在代码中实际被：
- GameWorld 作为每个信仰的运行时状态列表保存
- ThoughtManager 作为 Faith 注册表使用
- CanonizationSystem 直接接受
- ReligionPanelText 直接读取

所以它不是普通服务 System，而是一个具体 Faith 对象。

因此未来重构方向应为：

FaithDefinition = 静态信仰内容
FaithState = 当前世界中的动态信仰状态
ReligionService/Process = 对 Faith 的运行规则

不要简单把 FaithSystem 改名成 Faith，然后继续把所有 DailyTick/规则塞回去。

## 6. InnovationTree：问题比 InnovationProgress 更大

InnovationProgress 本身比较干净，是进度状态。

真正的问题是 InnovationTree 同时持有：
- 革新定义/树
- 各革新进度 Dictionary
- 各政权资源累计产量
- 实践经验
- 研究推进
- 完成逻辑

因此 Architecture 2.0 下 InnovationTree 应逐渐拆成：

InnovationCatalog → Definition
InnovationProgressStore → State
InnovationQuery → 读取/可用性
InnovationProcess → 推进
InnovationEffect → 完成后的世界改变

但这属于结构迁移，暂不立即创建五个空类。

## 7. Realm：Relations 与 Derived 已经开始独立

当前 RealmData 被 RealmDTO 序列化，也被 PoliticalManager、SocietySystem、SuccessionSystem、AI 等大量系统直接读取。

这说明 RealmData 已经成为整个模拟器的公共事实状态。

正确方向不是拆掉 RealmData，而是把读取方式逐步从：

System → RealmData.field

转成：

System → Query → RealmData / Relations

其中 RealmData 仍然是 Authority State。

## 8. RealmSituation：值得重新定义

当前 RealmSituation 已存在 stability、legitimacy 等综合政治状态。

它不应简单被当作 RealmData 的复制品。

需要进一步判断每个字段属于：
- Realm Authority State
- Derived Political Situation
- Temporary Modifier
- Runtime Process State

初步判断：RealmSituation 更接近 Derived/Context State，而不是 Realm 的第二套权威事实。

这是后续政治架构的重要审计点。

## 9. SaveData：暂时保持兼容

当前 SaveData 直接保存 TileData、CultureData 等运行时对象。

这确实违反长期的 Domain/Save 边界，但现在直接改会扩大迁移面。

因此：

短期：保持兼容。
中期：建立 Save DTO Boundary。
长期：Domain Entity 与 Save Schema 独立演化。

## 10. 最重要的迁移策略修正

不再采用：

“先建立一套全新的 Architecture 2.0 Framework，再把旧代码搬进去”。

改为：

**从现有代码已经存在的正确职责开始抽取。**

例如：

Army.CalculateCombatPower
→ WarfareCombatPowerQuery

PopulationStats.GetClassShare
→ PopulationCompositionQuery

CultureData
→ 先明确 Definition 边界

FaithSystem
→ 先明确 State/Definition

InnovationTree
→ 先明确 Catalog/Progress/Process

这样可以避免创建一堆只有接口没有真实业务的“架构空壳”。

## 11. 下一批研究重点

下一轮重点不是再增加抽象，而是继续追踪：

1. Army 的全部字段，确定哪些是 State、Relation、Runtime。
2. CultureData 的全部字段，确定 Definition/State 边界。
3. FaithSystem 的全部字段，确定 Definition/State 边界。
4. InnovationProgress / InnovationTree 的完整依赖图。
5. TileData 的 Dirty / Derived 字段。

完成这一步后，才可以开始第一批真实代码迁移。
