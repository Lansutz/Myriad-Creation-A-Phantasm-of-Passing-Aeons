# ARCHITECTURE 2.0 — README 对照审计规则

> 来源：`main/README.md`（当前主分支介绍文件）
> 用途：约束后续架构审计，防止把设计说明、已实现代码、旧设计与未来路线混为一谈。

## 1. README 的证据等级

后续审计必须同时区分四种状态：

| 状态 | 含义 | 处理方式 |
|---|---|---|
| README-已实现 | README 明确描述，当前代码也有对应实现 | 可作为设计意图 + 现状交叉证据 |
| README-设计未实现 | README 描述了明确设计，但当前代码没有对应实现 | 记为设计目标，不得说“已经存在” |
| README-旧设计 | README 有描述，但当前架构/代码已经被用户后续讨论明确升级 | 以当前用户最新设计 + 当前代码为准；README 只作为历史设计证据 |
| README-未来计划 | README 明确放在“后续计划”等路线图中 | 不纳入当前领域边界的实现事实，只作为未来约束 |
| Code-only | 当前代码存在，但 README 未描述或描述不足 | 以代码为当前实现事实，并单独判断是否为临时/遗留模型 |

## 2. README 中与本轮 Map / Settlement / Project 审计直接相关的内容

### 2.1 Anchor / Settlement：README 已明确设计

README 明确提出：

- 聚居点通过 Anchor 定位；
- 聚居点不是地块附属属性；
- Anchor 有自己的坐标、等级、形态、辐射范围；
- 城市可能跨多个地块；
- 关口堡可以控制通道；
- 游牧营地可以迁移；
- 三类聚居点：Burg / Outpost / Camp；
- 三者存在演化路径。

这部分不是“凭空从代码推导出来的”，而是项目原始设计的一部分。

但当前代码仍有大量 `tileIndex` 中心模型，因此必须区分：

**设计已存在 ≠ 代码已经完成。**

尤其 README 的“坐标、等级、形态、辐射范围”并不意味着当前 BurgData 的所有字段都应该原封不动进入最终 Anchor；需要结合后续架构升级重新划分。

### 2.2 MapActor：README 与当前代码高度吻合

README 把 MapActor 定义为自主地图单位：

- 可以不属于政权；
- 商队、流民、蛮族、盗匪、雇佣兵等可以在地图上活动；
- 前国家形态的人类群体也可以作为 MapActor；
- 满足条件后可以转化为正式政权。

当前代码也已经有 MapActor 及其相关类型。

因此 MapActor → Map Unit 的审计不是未来空想，而是**已有实现 + 更高层架构重新命名/分层**。

但是 README 的 MapActor 仍属于较早的设计表述。当前架构已经进一步明确：

> Map Unit 是独立于 Map / MapCell 的地图存在对象；Army Unit 是 Map Unit 的一个军事类别。

所以后续以这个升级后的边界为准。

### 2.3 聚落分类：README 需要“历史版本”处理

README 写的是：

- Burg：永久性的，以居住和生产为核心；
- Outpost：永久性的，以控制、防御、通道为核心；
- Camp：临时性或半永久性。

这可以作为三种 SettlementCategory 的设计来源。

但 README 后面又把堡垒、城堡、关口堡、港口等直接放入 Outpost 示例，并同时描述营地→坞堡→据点/定居点等演化。

当前架构已经进一步把：

- SettlementCategory
- SettlementDefinition / Typology
- SettlementLevel
- Political Role

拆成不同轴。

因此不能再把 README 中的“示例类型”直接当成最终枚举层级。

### 2.4 Geography：README 是明确的设计原则

README 明确：

- 地理是历史模拟的底层条件；
- 地形、气候、水文影响人口、军事、贸易；
- 河流、流域、三角洲属于水文系统。

当前架构又进一步明确：

> Map / MapCell 是空间基底；Geography / Hydrology 是 Map 的信息领域。

同时当前设计已经明确支持动态地理，例如海侵、海退；因此后续审计不得把 README 的“地理环境”理解成不可变静态 Tile 数据。

### 2.5 Trade：README 只提供高层设计，不证明当前 Network 已完整实现

README 明确存在：

- 贸易中心；
- 贸易路线；
- 商队；
- 高等级聚落经济虹吸。

但它没有证明当前已经实现了完整的通用 Network abstraction。

当前代码确实有 TradeRoute / Caravan / TradeCenter，但这是：

**已有具体贸易实现 → 可以作为 Network 雏形**

而不是：

**通用 Network 已经完成。**

这两者必须严格区分。

## 3. README 中与 Plan / GreatProject 的重要关系

README 当前公开的核心设计重点是：

- 角色个人化革新；
- 角色研究；
- 实践驱动；
- 个人天才突破；
- 聚落自主发展；
- 国家/社会长期演进。

README 并没有明确描述一个已经完成的 GreatProject 系统。

因此当前：

- PlanSystem = 代码中已存在的计划机制；
- Character Research = README 明确的个人化设计方向；
- ConstructionPlan = 当前代码已有的局部施工机制；
- GreatProject = 当前架构审计后提出的**新增领域机制**，不能伪装成 README 已经存在的系统。

同时，用户最新设计已经明确：

> Great Wall / 大型国家工程不能归入普通 Plan，也不能简单归入普通 Wonder / Structure。

所以 GreatProject 是当前架构升级过程中新增的正式边界。

## 4. “README 说过，但已经升级”的处理原则

后续如果发现：

> README 写 A，但当前用户设计已经变成 B。

统一记录：

`README(A) → Current Design(B) → Code Status(C)`

而不是直接把 A 当作当前架构。

例如：

`Tile → Anchor`
`BurgData → Settlement + Anchor + Relations + ...`
`Great Wall = Structure → 当前升级为 Network/System`
`Plan = 通用长期事务 → 当前明确收窄为主体计划/个人计划层`

## 5. “README 说了，但代码没做”的处理原则

不得写：

> “项目已经支持 X。”

必须写：

> “README 已定义 X，但当前代码尚未实现 / 仅存在部分实现。”

## 6. “代码有，但 README 没写”的处理原则

代码是当前实现事实，但不能自动升级成设计意图。

需要进一步判断：

- 是否为正式领域模型；
- 是否为旧代码；
- 是否为兼容层；
- 是否为临时实现；
- 是否与当前用户设计冲突。

## 7. 后续审计标准

以后每一项领域对象至少记录：

1. README 设计状态；
2. 当前用户最新设计；
3. 当前代码实现状态；
4. 三者是否一致；
5. 如果不一致，哪一个具有当前优先级；
6. 迁移时哪些字段是旧模型遗留，哪些字段是真正的设计需求。

当前优先级：

**用户最新明确设计 > 当前架构审计结论 > 当前代码事实 > README 旧设计描述。**

README 的“未来计划”只作为路线图，不作为当前已实现系统。

---

## 结论

README 应作为项目的**设计史 + 高层设计意图 + 路线图**来读，而不是当作当前代码 API 文档。

后续所有审计都会显式区分：

> **设计过 / 实现过 / 当前仍有效 / 已经升级 / 未来计划**

这样才能避免把旧 README 里的概念重新塞回已经升级过的 2.0 架构。
