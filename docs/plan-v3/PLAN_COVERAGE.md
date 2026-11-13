# Plan 系统覆盖矩阵（S15）

> 2026-11-10 13:35。用户口径：建造接的是 Plan 里的 **Construction / Engineering（大型工程）**；而 Plan 系统还缺 **Intrigue（阴谋）** 和其他。

## 1. 类型 vs 实现（10 类）

| PlanType | 中文 | 执行器 | 创建者 | 现状 |
|---|---|---|---|---|
| `Research` | 研究 | ✅ 有 | ✅ 有 | 唯一完整链 |
| `Construction` | 建造 | ✅ 有（`ConstructionPlanSystem`） | ⚠️ 仅测试/无生产调用 | 半跑：能建，但没人发起 |
| `Engineering` | 大型工程 | ❌ 无 | ❌ 无 | 只有类型 |
| `Intrigue` | **阴谋** | ❌ 无 | ❌ 无 | 只有类型 |
| `Exploration` | 探索 | ❌ 无 | ❌ 无 | 只有类型 |
| `Trade` | 贸易（远途商队） | ❌ 无（商队另有 `Caravan`） | ❌ 无 | 只有类型 |
| `Diplomacy` | 外交（使团/谈判） | ❌ 无 | ❌ 无 | 只有类型 |
| `Migration` | 迁徙 | ❌ 无 | ❌ 无 | 只有类型 |
| `Military` | 军事行动（征募/远征） | ❌ 无 | ❌ 无 | 只有类型 |
| `Custom` | 自定义 | ❌ 无 | ❌ 无 | 扩展位 |

**结论**：10 个类型里 **1 个完整、1 个半跑、8 个空壳**。这不是"缺一个阴谋"，是 **Plan 只有骨架没有肌肉**。

## 2. 每个类型的最小闭环（共同的四件套）

任何 Plan 要能跑，必须有四样东西——缺一件就等于没有：

1. **创建**：谁能发起（人物/机构/政权）＋ 前置条件（`ActionGate` 四层：能力/职权/法理/资源）；
2. **执行**：每 tick 推进（`OnTick` 或阶段推进）；
3. **中断/失败**：`waitCondition` 不满足、参与者死亡、资源断供；
4. **结算**：产出可观察结果（新建实体/改状态/写编年史）。

## 3. 阴谋（Intrigue）该长什么样

阴谋不是"点一下暗杀"，它是**有目标、有手段、有暴露风险的长期过程**：

```
发起者(人物) → 目标(人物/头衔/政权) → 手段 → 准备阶段 → 执行 → 结算/暴露
```

| 要素 | 内容 | 依赖 |
|---|---|---|
| 手段 | 毒杀 / 伪造文书 / 煽动民变 / 收买近臣 / 婚姻渗透 | 关系层（S17 `EntityRelation` 的 obligations）、头衔（近臣=官职） |
| 前置 | 接近目标的机会（同宫廷/同城/有内应）、资源（钱、人情） | `ActionGate` 资源层 + 官职/头衔 |
| 风险 | 暴露概率随参与者数量、目标戒备、间谍能力上升 | **信息/秘密系统（S19 缺口）** |
| 结算 | 目标死亡/失势/关系破裂/阴谋者被处决 | `CharacterData` + 法律（谋杀=罪行） |
| 记录 | 编年史记"某年某月，某暴卒"（真凶可隐藏） | `Chronicle` |

**关键**：没有"谁知道什么"，阴谋就退化成掷骰。所以 `Intrigue` 应排在**信息传播**之后做。

## 4. 建造与命名/归属的接法（回应上一问）

建造 Plan 的发起者 = **下令建造的那个人**，落点就是：

```
Plan(Construction/Engineering).initiatorCharacterId
   → 完成时 SettlementBlock.builderCharacterId = initiator
   → displayName = SettlementBlockNaming.For(kind, ordinal, 发起者本名)
```

所以上一轮说的"命名接人物系统"，真正的接点是 **Plan 的 initiator 字段**，而不是 `AddBlock` 的参数。`AddBlock` 只要在**建造完成回调**里接收这两个值即可。

## 6. 哪些 PlanType 根本不该存在（用户质疑的合理性问题）

判据：**Plan 是"有参与者的长期过程"的载体**。一个类型该不该留，看三问：
① 它是不是**过程**（有起止、可中断、要时间）？② 它有没有**自己的状态机**（不是别人状态机的一步）？③ 去掉它，别的系统能不能**完整承担**？

| PlanType | 是过程？ | 有自己的状态机？ | 别的系统能承担？ | 判决 |
|---|---|---|---|---|
| `Research` 研究 | 是 | 是（进度/中断） | 不能 | **保留** |
| `Construction` 建造 | 是 | 是 | 不能 | **保留** |
| `Engineering` 大型工程 | 是 | **与 Construction 同形**，只是规模/财政/中断规则不同 | 能——`Construction` + `scale` 参数即可 | **合并**（枚举多余） |
| `Intrigue` 阴谋 | 是 | 是，但**缺"秘密"维度**（Plan 现有状态机没有保密/暴露） | 不能（关系/法律/信息都给不了过程） | **保留 + 补保密维度** |
| `Exploration` 探索 | 是 | 是 | 不能——但**依赖地图知识（S19）**，现在做等于空转 | **保留，暂不启用** |
| `Trade` 贸易 | 不是 | 否 | **能——`Caravan` 实体 + 贸易路线**已在跑 | **删除**（贸易不是项目，是物流） |
| `Diplomacy` 外交 | 半是 | 否——`DiplomacyManager` 已管关系/条约/停战 | 能（使团可后补为 Plan，但不是现在的形态） | **删除**（等真有"使团远行"再做） |
| `Migration` 迁徙 | 不是 | 否——人口/游牧/聚落迁移已有 | 能（人口与行为体系统） | **删除**（迁移是人口行为） |
| `Military` 军事 | 不是 | 否——`Warfare` 有 War/army/combat 状态机 | 能（战争系统） | **删除**（打仗不是项目） |
| `Custom` 自定义 | — | — | — | **只留给模组**，本体禁用 |

⇒ **10 类应缩到 3–4 类**：`Research` / `Construction`（含大型工程）/ `Intrigue`（+保密）/（`Exploration` 待 S19）。

### 更彻底的做法（推荐）

`PlanType` 这个枚举本身就是"领域分类"，而 Plan 的职责是**过程载体**。正确形状是：

```
Plan {
  kind ∈ { Process, Works, Expedition, Scheme }   // 结构语义，只有 4 个
  planDefId : string                              // 内容决定"具体是什么活"（走 JSON）
  scale, duration, participants, requirements, waitCondition, stage
}
```

这样：
- 新增一种"活动"= 加一条 JSON，**不改枚举**（符合可模组化硬约束 D18）；
- "大型工程"退化成 `Works` 的一个内容条目（`scale=large`），不再需要 `Engineering`；
- 阴谋是 `Scheme`，天然带保密字段；
- 贸易/外交/迁徙/军事四类**从枚举里删掉**，由各自系统承担——它们本来就不是"项目"。

## 7. 附：当前 8 个空壳的处置

| 处置 | 类型 |
|---|---|
| 保留并补执行器 | `Construction`（补创建者 → 打通建造+命名+归属） |
| 合并 | `Engineering` → `Construction` + scale |
| 保留待前置 | `Exploration`（等 S19 地图知识）、`Intrigue`（补保密，排在 S19 后） |
| 删除枚举、交给别的系统 | `Trade`（商队/路线）、`Diplomacy`（DiplomacyManager）、`Migration`（人口/行为体）、`Military`（Warfare） |
| 仅模组可用 | `Custom` |


## 5. 建议顺序

1. **Construction 补创建者**（AI/玩家下建造令 → Plan → 完成回调里 `AddBlock(builderName=发起者本名)`）——一次打通"建造 + 命名 + 归属"三条链；
2. **Engineering** 不单独做：并进 Construction，用 `scale` 区分大型工程（周期长、可中断、吃财政）；
3. **信息传播**（S19）——阴谋、谣言、误判的前置；
4. **Intrigue**——四件套 + 保密/暴露 + 编年史隐藏真相；
5. `Trade`/`Diplomacy`/`Migration`/`Military` **不排期**：删枚举，回到各自系统。
