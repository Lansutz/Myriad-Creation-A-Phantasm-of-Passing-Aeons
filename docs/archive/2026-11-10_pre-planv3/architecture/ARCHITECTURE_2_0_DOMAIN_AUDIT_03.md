# Architecture 2.0 — Domain Audit 03

## 本轮主题：Map、Distribution、History、Settlement、Map Unit、Army 与 Title 的边界

本轮根据当前架构讨论，对“地图上的东西”重新划分语义边界。核心结论：**地图本体、地图上的分布、实体本身、地图单位、聚居点、军事实体、头衔不能使用同一层模型表达。**

## 1. Map 只表示地图本体

Map 的职责是空间本体，而不是世界实体的当前分布。

Map 可以包含：
- Tile / 空间单元
- 坐标
- 地形
- 海陆
- 河流
- 地理结构
- 其他固有空间属性

Map 不应直接拥有：
- 当前 Realm
- Culture 分布
- Faith 分布
- Population 分布
- Settlement 实体
- Army 实体
- Map Unit 所属关系

这些是世界状态在空间上的投影/关联，而不是地图本体。

## 2. Distribution 是“实体在空间上的分布”，不是 Map

当前设计需要一个明确概念表示：某类世界实体如何分布、占据或关联到地图空间。

候选正式命名：Distribution / WorldDistribution，最终名称需要结合仓库现有命名继续确认。

典型内容包括：
- Political Distribution / Control Distribution
- Culture Distribution
- Faith Distribution
- Population Distribution
- Settlement Distribution

这里的 Distribution 不代表实体本身，只表示实体与空间之间的空间化状态/关系。

## 3. History 与 Distribution 不应直接等同

“History”可以承担世界状态随时间变化的记录/初始化/历史演化数据；Distribution 表示某一时刻或某一历史状态下的空间分布。

因此暂不把所有 Distribution 机械命名为 History。

需要继续确认现有工程中的历史文件究竟同时承担：
1. 初始世界分布；
2. 时间变化记录；
3. 当前世界状态；
4. 存档持久化。

如果四者混杂，应在后续审计中分别拆语义，而不是仅改文件夹名称。

## 4. Settlement 是独立实体

Settlement 不是 Map Unit，也不是 Map。

Settlement 表示聚居点本身：
- 聚居身份
- 聚居人口
- 聚居发展状态
- 聚居设施/建筑
- 聚居控制关系
- 聚居生命周期

Settlement 与 Map 的关系是“位于某空间”，与 Distribution/History 的关系是“在世界状态中具有空间存在”。

Settlement 不因为位于地图上就变成 Map Unit。

## 5. Map Unit 是独立领域概念

Map Unit 表示“作为地图上的单位而运行的实体”。它不等于 Map，也不属于 Map 本体。

Map Unit 主要负责：
- 地图位置
- 移动状态
- 是否移动
- 地图单位生命周期
- 是否存在归属
- 当前归属关系
- 是否有人带领
- 当前带领者
- 与地图单位活动相关的运行状态

Map Unit 不负责：
- 兵种定义
- 军队构成
- 编制
- 战斗规则
- 补给规则
- 士气规则
- 军事组织本身

## 6. Army Unit 与 Army 必须区分

Army Unit 是 Map Unit 的一种分类；Army 则属于军事领域。

语义上：

Army Unit 回答：
> 这支军队作为地图单位在哪里、是否移动、归谁、由谁带领、处于什么地图生命周期？

Army 回答：
> 这支军队本身由什么构成、具有什么军事能力、采用什么军事组织？

因此不能把 Army 的兵种、Composition、Manpower、Combat、Supply、Morale 等塞入 Map Unit。

建议后续代码结构遵循：
- Map Unit / Army Unit：地图运行层
- Military / Army：军事领域
- Troop / Composition / Formation / Combat / Supply 等：军事子领域

是否使用 composition、reference 或独立 state 来连接 Army Unit 与 Army，必须在审计现有 Army、MapActorManager、Army 存储和移动代码后确定，不提前假设。

## 7. Map Unit 生命周期

Map Unit 的归属不是“是否为 Map Unit”的判定条件，而是 Map Unit 的一项关系/状态。

因此 Realm 崩溃不能简单等价于删除 Army。

可能的生命周期反应包括：
- 转移给新 Realm；
- 失去归属，成为无归属 Map Unit；
- 按具体世界规则移除或转化。

这里还需要区分：
- 当前 Owner
- 当前 Controller
- Leader / Commander
- 原归属 / 来源归属（若规则确实需要历史追踪）

不能在未检查现有字段前擅自合并这些概念。

## 8. Army 解散是转化，不是简单删除

如果军队解散，预期语义为：

Military Action
→ Army Disbanded Domain Event
→ Population Effect
→ Population State

因此 Army Unit 的地图生命周期结束与 Army 军事状态结束可以是同一行为的不同后果，但不能把“Destroy Army”当作唯一模型。

需要继续检查现有代码是否已经存在 Army → Population / Manpower → Population 的转换逻辑，避免重复建立机制。

## 9. Title 保留给“头衔”

本项目已经存在明确的 Title 领域：
- TitleCatalog
- titleId
- TitleRank
- monarch / noble / bureaucratic 等 Title 内容
- holder / succession 等头衔相关关系

因此 Title 不能用于表示“地图上的政治分布”“领土分布”或其他空间状态。

应严格区分：
- Title = 头衔/名位/统治权载体
- Realm = 政治实体
- Control / Territory / Political Distribution = 政治实体与空间之间的关系/状态
- Map Tile = 空间本体

## 10. 当前术语表

| 概念 | 语义 | 不应承担 |
|---|---|---|
| Map | 空间本体 | 世界实体分布 |
| Tile | 空间单元 | 直接承载全部世界状态 |
| Distribution | 实体与空间的分布/空间化状态 | 实体本身 |
| History | 世界状态随时间的历史记录/演化语义 | 单纯空间本体 |
| Settlement | 聚居点实体 | 地图单位通用移动逻辑 |
| Map Unit | 地图单位实体/运行状态 | 军事构成 |
| Army Unit | 军队作为地图单位的状态 | 兵种与战斗体系 |
| Army | 军事实体 | 通用地图移动职责 |
| Title | 头衔 | 地图政治分布 |
| Realm | 政治实体 | 地图本体 |

## 11. 本轮代码结论

暂不直接创建 Distribution 新框架，也不立即重命名大量文件。

先继续做代码审计：
1. 搜索现有 Title 相关目录与引用，确认没有把 Title 当 Territory/Distribution 使用；
2. 审计 TileData 中所有政治、文化、信仰、人口、Settlement 字段；
3. 审计 MapActorManager 及 Map Actor 数据结构，确认其实际职责；
4. 审计 Army 的存储、位置、归属、Leader、移动、解散与人口转换；
5. 审计现有 History/历史初始化文件，确认其是否真正承担 Distribution；
6. 最后再确定正式目录名：History、WorldState、Distribution 是否需要分层。

本轮原则：**先确认已有代码承担的事实，再确定最终文件夹和类名；不为概念完整性制造空壳类。**
