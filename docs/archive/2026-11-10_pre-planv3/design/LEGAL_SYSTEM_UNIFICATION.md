# 法律/法令系统统一设计

> 2026-11-04。目标：把法律、法令、特许权、领主法令收进一个**多层法律秩序**，避免“所有东西都像最高法”和互相冲突。
> 只读代码：`RealmLawState`、`LawDef`、`DecreeDef`、`LawAndDecreeSystem`、`LawComposition`、`LawConstraints`、`LawRealmHost`、`LawEffectRuntime`。

---

## 1. 当前问题

1. **只有一个 `RealmLawState`**
   - `groups`、`knownLawIds`、`charterHolderIds` 都是政权级；
   - 没有“领主/地方/特许城市自己的法律秩序”；
   - 所有法律看起来都像最高法。

2. **特许权只是“提案权”**
   - `charterHolderIds` 只用于 `ProposalAuthority.CharterHolders`；
   - 没有“特许状覆盖某条普通法”的规范模型；
   - 特许城市/行会/领主豁免无法表达。

3. **法令是临时效果，不是规范**
   - `DecreeDef` 有持续时间/冷却，但没有 scope、issuer、precedence；
   - 法令与法律冲突时没有裁决。

4. **官僚系统没有接上**
   - 现在只有 `realm.centralization` 一个浮点；
   - 没有行政能力、执法半径、法律整合度、合规度；
   - 法律效果与官僚能力之间没有约束。

---

## 2. 统一模型：多层法律秩序

```text
LegalOrder（法律秩序）
  ├─ owner: realm / lordship / city / guild / estate
  ├─ liegeOrderId（上级秩序；根=realm）
  ├─ autonomy（自治度）
  ├─ norms（规范列表）
  ├─ charters（特许状，也是 norm）
  └─ compliance / enforcementReach / legalIntegration
```

每个节点都可以有：

- `LawDef`（一般法）
- `DecreeDef`（临时法令）
- `CharterDef`（特许状，可用 `LawDef` + `normKind=Charter` 表达）
- 习惯/兜底 `Custom`

**不是每个领主都重新写一整套法律**：

- 默认继承上级秩序；
- 在 `autonomy` 范围内可以覆盖；
- 地方无法覆盖的，仍是上级法；
- 冲突时进入裁决，不是静默覆盖。

---

## 3. 统一规范字段

已加：

```csharp
LegalNormKind normKind;   // Custom/Decree/Statute/Charter/Constitutional/Fundamental
LegalScope scope;         // Realm/Province/Lordship/Locality/Personal
int precedence;
int issuerCharacterId;
List<string> overrides;
```

法律、法令、特许共用这些字段；`DecreeDef` 可加同组字段（或转成 `LawDef normKind=Decree`）。

---

## 4. 冲突裁决

### 4.1 先看适用范围

1. 规范是否覆盖该主体/地域/事项？
2. 不覆盖 → 不适用，不参与冲突。

### 4.2 再按规范层级

默认顺序：

```text
Fundamental（最高法理）
  > Constitutional（根本组织法）
  > Statute（一般成文法）
  > Charter（特许状；在授予范围内可覆盖一般法）
  > Custom（习惯/兜底）
  > Decree（临时法令）
```

但：

- **Charter 是否能覆盖 Statute，取决于授予条款**；`overrides` 明确列出可覆盖的规范；
- Charter 不能覆盖 Fundamental/Constitutional，除非授予者本身有更高权源；
- 同层级：**特别法优于一般法**（scope 更窄 +1）；再同层：**后法优于前法**；
- 仍无法裁决 → `legalConflict` 标记，按 `enforcementReach` 决定实际执行，并给合法性/稳定度惩罚。

### 4.3 互斥与前置

已加：

```csharp
LawDef.excludesLawIds;        // 与已生效法律互斥
LawRequirement.requiredDecreeIds; // 必须先生效的法令
DecreeDef.excludesDecreeIds;  // 与已生效法令互斥
```

规则：

- 互斥是**跨组**检查：任一被排除法律已生效 → 不能推行；
- 前置法令：`CanEnactDecree` 检查 `IsDecreeActive`；
- 同组互斥仍由 `LawGroupState` 保证（一组一现行法）；
- 冲突不静默覆盖，返回明确 reason。

### 4.4 已知/学习

- 法律：`CanPropose` 已检查 `RealmLawState.IsLawKnown`，未知法不能提案；
- 法令：`knownDecreeIds` 尚未接入，是下一步；
- 已知来源：初始法、文化/革新、特许、事件、法律移植；不能“想推就推”。

### 4.5 执行与合规

```text
实际执行 = 规范优先级 × enforcementReach × compliance
```

- `enforcementReach` 低：地方法律只在中心附近生效；
- `compliance` 低：领主/城市可阳奉阴违，冲突不会自动消失；
- 强制统一需要官僚能力/驻军/合法性，不是改一条法律就行。

---

## 5. 每个领主都有“法令”吗？

是，但不是每个领主都维护完整法律集：

- 每个 `LegalOrder` 有 `localNorms`（地方条例/领主法令）；
- 没有地方规范时，继承上级；
- 地方规范与上级冲突 → 走 §4 裁决；
- 封建制下地方 `autonomy` 高，地方规范更容易生效；
- 中央集权下 `enforcementReach` 高，上级法更容易压过地方。

所以：

```text
领主法令 = 其 LegalOrder 的 localNorms
是否与上级一致 = 冲突裁决 + compliance
```

不是“每个领主复制一套国家法律”。

---

## 6. 特许权怎么容纳

特许权不是“法律组里的一条普通法”，而是：

```text
Charter（normKind=Charter）
  ├─ grantor: 授予者
  ├─ grantee: 城市/行会/领主/个人
  ├─ scope: 地域/事项/人群
  ├─ privileges: 免税/自治/司法/驻军/通行...
  ├─ overrides: 可覆盖的普通法 id
  └─ duration / revocable
```

冲突处理：

- 特许状在 **grantee + scope** 内优先于普通法（若授予条款允许）；
- 与根本法冲突 → 根本法优先，但可能触发合法性/叛乱；
- 中央后来立法想收回特许 → 需要 `authority/legitimacy`，否则冲突/抵抗；
- 特许权持有者仍受其领主/上级秩序的战争、外交、刑事底线约束。

`charterHolderIds` 目前只表示“可提案”，应拆成：

- `charterHolders`：身份/提案权；
- `charters`：特许状规范本身。

---

## 7. 与官僚系统/官僚因子的连接

建议新增/使用：

| 因子 | 来源 | 作用 |
|---|---|---|
| `administrativeCapacity` | 官员数量、识字、道路、财政、中央机构 | 执法/登记/统计上限 |
| `enforcementReach` | capacity × centralization × 距离/地形 | 法律实际覆盖半径 |
| `legalIntegration` | 统一法源、语言、官僚训练 | 冲突裁决效率、同法程度 |
| `compliance` | 合法性、驻军、自治度、利益 | 地方是否执行 |
| `legalComplexity` | 法律组数量/例外/特许 | 行政负荷；超过 capacity 产生腐败/延迟 |

法律效果 → 官僚能力：

- 统一法/户籍/税册提高 capacity，但增加 complexity；
- 特许/地方自治降低 central reach，但提高 compliance/稳定；
- 军管/戒严提高 enforcement，降低 legitimacy。

---

## 8. 模拟上要不要全做

| 功能 | 必要 | 说明 |
|---|---|---|
| 多层 LegalOrder | 中高 | 有领主/特许城市/自治才需要；否则 realm + charter 例外即可 |
| 特许状规范 | 高 | 没有它，特许权只能当提案权，不真实 |
| 冲突裁决 | 中高 | 不做则法律互相覆盖，结果不可解释 |
| 官僚 capacity/reach | 高 | 否则中央法瞬间全国生效 |
| 每领主完整法律集 | 低 | 用继承 + 局部覆盖即可 |
| 完整法理优先级 | 低 | 模拟只需 4–5 档 + 例外表 |

原则：**先做“realm + 领主/城市例外 + 执法半径”**，不要一上来做完整法哲学。

---

## 9. 落地顺序

1. `LegalOrder` 树 + `owner/liege/autonomy`；
2. `RealmLawState` 改为挂在 root order，地方 order 继承；
3. `CharterDef`/`LawDef normKind=Charter` + `overrides`；
4. 冲突裁决器（scope → rank → specificity → time → enforcement）；
5. `LawEffectRuntime` 接 `enforcementReach/compliance`；
6. 官僚因子 `administrativeCapacity/legalComplexity`；
7. UI 分“国家法/领主法/特许状/法令”；
8. 旧档迁移：单 realm 平铺 → root order。

---

## 10. 参考

- [Legal Pluralism Explained](https://books.google.com.sg/books?id=dPUTEAAAQBAJ)
- [Legal Pluralism as a Category of Analysis — Law and History Review](https://www.cambridge.org/core/journals/law-and-history-review/article/legal-pluralism-as-a-category-of-analysis/EDFD7E065C7EA438131508CD4EE9A3CA)
- [Liberty and political power in Toulouse, 1050–1230](https://babel.hathitrust.org/cgi/ssd?id=mdp.39015066088728;page=ssd;view=plaintext;seq=124;num=108)
- [Seeing Like an Official](https://www.hup.harvard.edu/books/9780674308237)
- [Economic Consequences of State Failure — Legal Capacity, Regulatory Activity, and Market Integration in Poland, 1505–1772](https://homsy-staging.cambridgecore.org/core/journals/journal-of-economic-history/article/economic-consequences-of-state-failurelegal-capacity-regulatory-activity-and-market-integration-in-poland-15051772/5DEDC9569125F734554ED85C5E41C341)
- [The guidaticum safe-conduct in medieval Arago-Catalonia](https://brill.com/previewpdf/journals/me/1/1/article-p51_6.xml)
- [Frontiers in Question: Introduction](https://blogs.fu-berlin.de/frontiers/files/2011/04/Power_FrontiersinQuestion.pdf)
