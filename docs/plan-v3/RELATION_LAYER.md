# 关系层（EntityRelation）

> 2026-11-09 03:30。用户提示"还有关系"。结论：这是一套**类型最丰富、却完全没接通**的子系统——17 种关系类型、注册表、投影、校验器、存档策略全在，**生产代码里没有任何写入者**。

---

## 1. 代码有什么

| 部件 | 内容 | 位置 |
|---|---|---|
| 关系类型 | **17 种**：DirectRule / MilitaryOccupation / Vassalage / PersonalUnion / Protectorate / DelegatedRule / Lease / Condominium / Tributary / Commission / Employment / Command / Subordination / Garrison / SpatialSubordination / Administrative（+ None） | `RelationType.cs` |
| 关系实体 | `from/to`（`EntityRef{kind,id}`）、`type`、`legitimacy`、`stability`、`isPrimary`、**`isProjection`**、`sinceDay`、`expiresDay`、**`obligations`（字符串列表）** | `EntityRelation.cs` |
| 注册表 | 按**目标**索引 `(kind,id) → List<EntityRelation>`；`Add` 先过 `RelationRules.IsAllowed(from.kind, to.kind, type)`；`isPrimary` 会**降级旧 primary**；`Capture()` **跳过 `isProjection`** | `RelationRegistry.cs` |
| 投影 | 从物质状态推导关系：附庸（`realm.suzerainId`）、领属（地块 `ownerRealmId` + 锚点）、军事占领（`occupyingRealmId`）、委任（军队归属）、雇佣（地图行为体归属），全部标 `isProjection=true` | `RelationProjection.cs` |
| 端点校验 | 丢弃端点已不存在的关系（读档时用） | `RelationEndpointValidator.cs` |
| 存档策略 | `RelationPersistencePolicy` 分世界/地图两套捕获 | `RelationPersistencePolicy.cs` |

## 2. 现状：整套空转

| 环节 | 状态 | 证据 |
|---|---|---|
| 生产写入 | **0** | 全仓 `RelationRegistry.Add` 的生产调用者只有 `Merge/Add` 内部与测试；`GameWorld.Core.cs:480` 只有 `Clear()` |
| 投影调用 | **仅测试** | `RelationProjection.Build` 唯一调用点是 `RelationProjectionTests.cs:40` |
| 存档 | 存的是空表 | `SaveSystem.cs:376` 捕获 + `:435` 还原；但表恒空 |
| 校验 | 校验空表 | `RelationEndpointValidator.Filter` 只在这条路径上 |
| 消费者 | **0** | 没有任何系统按关系查询（附庸链、领属、雇佣都不走这层，各自读自己的字段） |

## 3. 设计意图（从 `isProjection` + `Capture` 反推）

代码已经暗示了一条清楚的规则：

> **物质关系是投影，契约关系是真源。**

- **投影类**（不入档，可重算）：领属（地块归属）、军事占领、委任（军队归属）、雇佣（行为体归属）、附庸（`suzerainId` 存在物质字段里）。
- **契约类**（入档，真源）：附庸契约、共治、租借、保护、朝贡、军事通行、义务条款。

这条规则的好处：读档后先恢复物质状态，再 `Build` 一次投影，关系层自动与物质状态一致；契约关系单独入档，不会因重算丢失。

## 4. 与现有系统的重复（必须先合并，否则是第 6 套"关系"）

| 现有存储 | 存什么关系 |
|---|---|
| `RealmData.suzerainId` / `vassalIds` | 宗主—附庸 |
| `DiplomacyManager._subordinations`（5 型） | 不平等从属 |
| `DiplomaticRelation`（relation/trust/threat） | 政权间态度 |
| `TitleHolding`（characterId/institutionId/share/role） | 头衔持有 |
| `CharacterRelation` / `CharacterBond` | 人际关系 |
| **`RelationRegistry`** | 通用关系（目前空） |

⇒ 关系层要么成为**统一读模型**（各系统写自己的字段，关系层只投影 + 统一查询），要么做**真源**（各系统改走关系层）。二者不能混。

## 5. 我认为缺的六点

1. **`obligations` 是裸字符串**：无法被法律条款、条约、朝贡额引用。应改成结构化（义务类型 + 对象 + 额度/期限）。
2. **没有时间演化**：`legitimacy/stability` 只是初始浮点，没有 tick（合法性会衰减、稳定会被挑战）。
3. **没有冲突消解**：同一目标允许多个 from，除 `isPrimary` 外无规则（谁优先？占领 vs 领属 vs 契约？）。
4. **没有关系到期与违约**：`expiresDay` 存在但无人读；违约（未履行义务）无后果。
5. **没有人际关系的对位**：`CharacterRelation` 与 `EntityRelation` 是两套（前者 opinion/trust/fear，后者 type/obligations），没有"家族关系投影到政权关系"的桥（联姻→同盟）。
6. **没有 UI**：玩家看不到附庸链、保护关系、租借地——`RealmDivisionText` 只读 `adminDivisions`。

## 6. 验收信号（若接入）

1. **投影可重算**：给定物质状态（地块/军队/宗主字段）→ `Build` 出的关系集合确定且可重复；读档后与物质状态一致。
2. **契约入档**：手写一条 `Vassalage`（非投影）→ 存档 → 读档仍在；投影关系**不**入档。
3. **端点校验**：删掉一个政权 → `Filter` 丢弃其相关关系，且不 crash。
4. **单一 primary**：同一目标只能有一个 `isPrimary`，由明确规则决定（现在只是"后写覆盖"）。
5. **义务可追**：一条 `obligations` 能追到具体法律条款或条约（结构化后）。

## 7. 与计划的关系

- 归 **S17 关系层**，横切 M5（法律义务）、M7（外交/附庸）、M8（头衔/家族）、M9（联姻）。
- **前置**：M1 存档闭合时一并决定"投影不入档 / 契约入档"的切分，否则关系层会再次成为空表。
- 关系层**不是重构中的系统**（法律/信仰/社会四维是），它更像"设计完成、实现搁置"，可以按里程碑接入。
