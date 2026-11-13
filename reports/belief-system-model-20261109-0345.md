# 信仰大系统术语与数据模型（落盘记录）

> 2026-11-09 03:45（Asia/Shanghai）。输入：用户给的术语体系（三总概念 / 教理 8 词 / 制度 4 支 / 五层区分 / 各层区分标志）+ 学派类型学（宗教/政治/哲学/学术）+ 四问判据。

---

## 1. 做的三件事

1. **把术语变成字段**：每个词给"排他边界 → 数据位置 → 必答/可选"，直接可编码（`BELIEF_SYSTEM_MODEL.md` §1–§6）。
2. **改一处结构**：`tradition` 从"层级容器"改为**谱系（谁传自谁）**——这是本轮唯一的结构性判断，理由见下。
3. **给学派类型学一个可执行判据**（四问 → `schoolType`），并规定"类型决定能挂哪些子类"。

## 2. 为什么把 tradition 改成谱系

> **2026-11-09 03:50 更正**：用户澄清"传统这个词汇是用来代替 CK3 的礼仪（rite）"——所以 `tradition` 不是抽象谱系，而是**礼仪—传承基底**（≈rite），是一个**实体**。下文"谱系"的写法作废，保留作过程记录。

- 原判断（作废）：把 tradition 当"谁传自谁的链"。
- 现判断：`tradition` = 礼仪—传承基底（有 liturgy/language/calendar），在基督教模式下是**下位基底**（地方礼仪先在，宗派追认于上），在佛教模式下是**上位来源**（法脉先定，宗派分出）。
- 方向由 `religion.assemblyMode ∈ {liturgical, lineage}` 决定。

## 3. 修正用户原稿两处

| 原稿 | 修正 | 理由 |
|---|---|---|
| `penalty` 下再列 `penalty`（处分种类） | 拆成 `penaltyKind`（绝罚/停职/除名/苦修）+ `procedure`（谁判/怎么审/怎么上诉） | 自己嵌套自己是分类错误；种类与程序是两件事 |
| `language` 归 `liturgy` | 保留，但明确它只管**礼仪与经典诵读语言 + 权威译本**；日常 idiom 归 `custom` | 否则会和"地方语言惯行"混 |

## 4. 学派类型学（可执行）

```
schoolType =
  if supernaturalObject && revelationalAuthority                  -> religious-school
  else if ideologicalObject && (programAuthority||leaderAuthority) -> political-school
  else if rationalObject && argumentativeAuthority                 -> philosophical-school
  else if methodicalObject && evidenceAuthority                    -> academic-school
```

硬规则：`schoolType = political-school` 时**不能**挂 `liturgy`/`ordination`/`sacrament`（否则政治派系会被误建成教会）。

两轨制：
- 宗教轨：`family → religion → denomination → {school | order | custom}`
- 非宗教轨：`belief-system(political|philosophical|secular) → school → faction`

## 5. 与现有内容的对位

| 现有 | 量 | 对位 | 缺 |
|---|---|---|---|
| `Religions.json` | 44 | `Religion` | 无 family / creed / canon / orthodoxy |
| `Doctrines.json` | 46 | `Doctrine.tenets` | 与"类型×领域"两轴表对不上，需重排 |
| `Tradition` | 13（factors **全空**） | `Tradition` 谱系 | 无 chainFrom/chainTo；solidification 零消费者 |
| `Ethos` | 8 | `family.narrative` / `ultimacy` | 零消费者 |
| `FaithSystem` | — | churchWealth / missionaryZeal / tolerance / faithRelations | 四字段**全无消费者** |
| `RealmData.stateReligionId` | 默认 −1 | 国教 | **无写入** ⇒ 传教/热忱/大圣战/圣地/面板五链同死 |

**要新建的四类内容**：`School` / `Denomination` / `Order` / `Deity`（当前完全没有）。

## 6. 信仰也不能靠数值加成

受宪章原则 5 管，信仰只能经**五个可指认环节**起作用：

| 环节 | 机制 |
|---|---|
| 合法性 | 国教→合法性来源；异端→合法性缺口 |
| 征收 | 教会财产 + 什一税 → 实际转移支付 |
| 动员 | 圣战号召 → 征召（走 `PlanType.Military`） |
| 合规 | 戒律与法律冲突 → 合规度惩罚（走法令五档） |
| 组织 | 教职任命 → 官职与头衔（polity→office） |

## 7. 仍有四个真问题

1. **神观档位要几档**：现有对照表只有"一神/多神"两档，太粗（建议 5–7 档：一神/多神/择一神/泛神/万有在神/非神/不可知/无神）。
2. **正典是否可开合**：`canon.closed=true` 后是否永久（历史上有"正典封闭"事件）？
3. **政治学派是否允许处分**：有党纪与处分 → 更像宗派；一律无 `penalty` → 又不像政党。
4. **学术师承是"规范"还是"影响"**：若只是影响，非宗教轨不需要 `tradition.normativeAuthority`。

## 8. 落盘

| 文档 | 改动 |
|---|---|
| `docs/plan-v3/BELIEF_SYSTEM_MODEL.md` | 新增：术语表 / 四层+谱系 / 学派类型学 / 数据形状 / 代码对位 / 与 D8 的收口 / 反数值对账 / 四个待确认 |
| `SYSTEM_MASTER_PLAN.md` §3.0 | 新增 **S18 信仰大系统**（重构中，M9） |
| `DECISIONS.md` | 新增 D29（实体层级 + tradition 作谱系；修正 penalty/language；D8 → E3+） |
| §9 证据索引 | 增 `BELIEF_SYSTEM_MODEL.md` |
| 本报告 | 决策与依据 |

## 9. 未验证

- 未读 `ReligionCatalog`/`FaithSystem` 的全字段（只按 E 域审计的结论与 grep 结果对位）。
- 四层实体与现有 44 条宗教的**逐条归属**未做（需一次内容映射表，属重构工作）。
- 学派类型学的四问判据只在纸面推演，没有内容样本验证。
