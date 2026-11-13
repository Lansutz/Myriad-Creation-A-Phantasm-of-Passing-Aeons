# 关系层审计（S17）

> 2026-11-09 03:30（Asia/Shanghai）。用户提示"还有关系"。结论：**17 种关系类型、注册表、投影、端点校验、存档策略全在，生产代码零写入者。**

---

## 1. 代码全貌

| 部件 | 内容 |
|---|---|
| `RelationType` | **17 种**：DirectRule / MilitaryOccupation / Vassalage / PersonalUnion / Protectorate / DelegatedRule / Lease / Condominium / Tributary / Commission / Employment / Command / Subordination / Garrison / SpatialSubordination / Administrative |
| `EntityRelation` | `from/to`（`EntityRef{kind,id}`）、`type`、`legitimacy`、`stability`、`isPrimary`、**`isProjection`**、`sinceDay`、`expiresDay`、**`obligations`（裸字符串）** |
| `RelationRegistry` | 按**目标**索引；`Add` 过 `RelationRules.IsAllowed`；`isPrimary` 冲突**后写覆盖**；`Capture()` **跳过投影** |
| `RelationProjection` | 从物质状态推导：附庸（`suzerainId`）/ 领属（地块归属+锚点）/ 军事占领 / 委任（军队）/ 雇佣（行为体），全标 `isProjection=true` |
| `RelationEndpointValidator` | 读档时丢弃端点已不存在的关系 |
| `RelationPersistencePolicy` | 世界/地图两套捕获策略 |

## 2. 现状：整套空转

- **生产写入 0**：全仓 `RelationRegistry.Add` 只在测试与 `Merge` 内部；`GameWorld.Core.cs:480` 只有 `Clear()`。
- **投影仅测试调用**：`RelationProjection.Build` 唯一调用点 `RelationProjectionTests.cs:40`。
- **存档存空表**：`SaveSystem.cs:376` 捕获 → `:435` 还原，表恒空。
- **消费者 0**：附庸链、领属、雇佣各自读自己的字段，没人走这层。

## 3. 代码暗示的规则

`isProjection` + `Capture()` 跳过投影 ⇒ **物质关系是投影（不入档、可重算），契约关系是真源（入档）**。

这条规则清楚，但从未兑现。兑现后读档流程是：恢复物质状态 → `Build` 一次投影 → 关系层自动一致；契约关系单独入档，重算不会丢。

## 4. 它已经是第 6 套"关系"

| 存储 | 内容 |
|---|---|
| `RealmData.suzerainId/vassalIds` | 宗主—附庸 |
| `DiplomacyManager._subordinations` | 不平等从属 |
| `DiplomaticRelation` | 政权间态度（relation/trust/threat） |
| `TitleHolding` | 头衔持有 |
| `CharacterRelation` / `CharacterBond` | 人际关系 |
| **`RelationRegistry`** | 通用关系（空） |

⇒ 开工前必须先决定：**关系层是"统一读模型"还是"真源"**。混着做会造出第 7 套。

## 5. 六点缺口

1. `obligations` 是裸字符串 ⇒ 无法被法律条款/条约/朝贡额引用。
2. 无时间演化：`legitimacy/stability` 只是初始浮点，没有 tick（合法性会衰减、稳定会被挑战）。
3. 无冲突消解：同一目标多 from，除 `isPrimary` 外无规则（占领 vs 领属 vs 契约谁优先？）。
4. `expiresDay` 存在但无人读；违约无后果。
5. 人际关系与实体关系无桥（联姻→同盟 这条链不存在）。
6. 无 UI：玩家看不到附庸链、保护关系、租借地。

## 6. 落盘

| 文档 | 改动 |
|---|---|
| `docs/plan-v3/RELATION_LAYER.md` | 新增：17 类清单、投影/契约切分、6 点缺口、5 条验收信号 |
| `SYSTEM_MASTER_PLAN.md` §3.0 | 新增 **S17 关系层**（横切 M5/M7/M8/M9）+ §3 S17 块 |
| `DECISIONS.md` D28 | 采纳"投影不入档 / 契约入档"；倾向"统一读模型 + 契约真源" |
| 本报告 | 审计记录 |

## 7. 未验证

- 未跑 Unity；"空转"结论来自生产 `Add` 零命中 + 投影唯一调用点是测试。
- `RelationRules.IsAllowed` 的端点白名单未逐条读完（只确认了 `Realm→Anchor` 领属合法、`Realm→Unit` 领属非法两条测试）。
