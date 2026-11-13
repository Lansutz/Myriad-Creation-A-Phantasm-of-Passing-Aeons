# 最高权力归属模型（Supreme Authority）

> 2026-11-05。问题：现有 `LawComposition` 把最高权力压成 `Monarchy / Republic` 二元，无法表达共治、双君主/多君主、最高权力机构、混合主权。
> 结论：最高权力归属必须独立成组，不能和“继承方式”“权力范围”混在一起。

---

## 1. 现有代码的问题

```csharp
public enum SupremeSovereignty
{
    Monarchy,
    Republic
}
```

问题：

- 把“谁掌握最高权力”压成二元；
- 罗马双执政官、斯巴达双王、拜占庭共治皇帝、纳瓦拉共治国王、复合君主制、等级会议主权都表达不了；
- `SupremeSuccession` 假定只有一个继承对象；
- `LawEffects.CalcSupreme` 只按君主/共和给固定加成；
- `ProposalAuthority` 只有首脑/中央机构/特许持有者，没有“主权机构/共治者”。

---

## 2. 历史形态

### 2.1 共治/双君主/多君主

- 罗马共和国：**双执政官**，同僚制、任期一年、相互否决；
- 斯巴达：**双王**，两王并立、权力受长老会/监察官制约；
- 拜占庭：**共治皇帝**（co-emperorship），资深/ junior 共治；
- 中世纪：共治国王、父子共治、兄弟共治、夫妻共治（如纳瓦拉）；
- 复合君主制：一个君主兼多个王冠，但各王国法律/议会不同。

参考：

- [Consuls and Res Publica: The origin of the consulship](https://www.cambridge.org/core/books/abs/consuls-and-res-publica/origin-of-the-consulship-in-cassius-dios-roman-history/46285DE3E70A10F2E5BD624190AF3C67)
- [Diarchy? The System of Co-Rulership in Byzantium](https://hfrir.jvolsu.com/index.php/en/archive-en/770-science-journal-of-volsu-history-area-studies-international-relations-2020-vol-25-no-6/byzantine-state-and-society/2171-lysikov-p-i-diarchy-the-system-of-co-rulership-in-byzantium-at-the-turn-of-the-13th-14th-centuries-and-its-impact-on-the-situation-in-the-state)
- [The Kings Consort of Navarre: 1284–1512](https://scholar.google.co.nz/citations?view_op=view_citation&hl=en&user=EPYfBBoAAAAJ&citation_for_view=EPYfBBoAAAAJ:UeHWp8X0CEIC)
- [Unions and divisions: new forms of rule in Medieval and Renaissance Europe](https://search.library.yale.edu/catalog/99173066393408651)

### 2.2 最高权力机构

- 罗马元老院、雅典公民大会、威尼斯大议会、城市议会、等级会议、长老会、宗教会议；
- 最高权力在机构，首脑/执政官只是执行者；
- 机构主权下的“继承”不是家族继承，而是成员资格/选举/抽签/任命。

参考：

- [Polybius and the Roman mixed constitution](https://m.riss.kr/search/detail/DetailView.do?control_no=5cf6a96f8c69f018b7998d826d417196&p_mat_type=1a0202e37d52c72d)
- [Religion, Law and the Growth of Constitutional Thought 1150–1650](https://openyls.law.yale.edu/bitstream/handle/20.500.13051/7523/Carl_Landauer.pdf?isAllowed=y&sequence=2)

### 2.3 混合/等级共治

- 君主 + 等级会议/议会/贵族会议共同掌握最高权力；
- 不是“君主制”也不是“共和制”，而是混合政体；
- 法律需要双方同意/批准。

---

## 3. 建议的模型

把“最高权力归属”拆成三个正交维度：

```text
A. 主权持有者类型 SupremeAuthorityKind
   ├─ 单一君主
   ├─ 双君主/二头共治
   ├─ 多君主/诸王共治
   ├─ 同僚制最高官职（双执政/多执政）
   ├─ 最高机构（元老院/议会/长老会/宗教会议）
   ├─ 混合主权（君主+等级会议/贵族会议）
   └─ 无中央最高（部落联盟/分权）

B. 共治关系 CoRulerRelation（仅多主体时）
   ├─ 平等同僚
   ├─ 资深/ junior
   ├─ 父子/兄弟/夫妻共治
   ├─ 轮值
   └─ 相互否决

C. 继承/产生方式 SupremeSuccession
   = 现有组；但按 A 不同而不同：
     - 单君主：世袭/推举/神命/篡夺
     - 多君主：共治者指定/共治继承/推举
     - 机构：成员资格/选举/抽签/任命
```

**关键**：A 是“谁掌握”，B 是“多主体怎么共处”，C 是“下一个怎么产生”；三者不能压成一个 `Monarchy/Republic`。

---

## 4. 早期不能现代

| 阶段 | 最高权力形态 |
|---|---|
| 部落/习俗 | 首领+长老会/部落大会；双首领/战时首领 |
| 早期王权 | 单王/双王/共治；神命/世袭/推举 |
| 中世纪 | 封建君主、共治国王、等级会议、城市议会、教会会议 |
| 早期现代 | 君主+等级会议混合、主权理论、常设官僚 |
| 现代/未来 | 议会主权、人民主权、联邦/邦联、根本法 |

不要开局给“共和制/议会主权”现代项；用“长老会/显贵会议/等级会议”等历史形态。

---

## 5. 模拟作用

| 维度 | 作用 | 必要性 |
|---|---|---|
| 主权持有者类型 | 法律批准权、宣战权、继承危机、合法性来源 | 高 |
| 共治关系 | 决策延迟、内斗、相互否决、共治崩溃 | 高 |
| 最高机构 | 立法/财政/外交决策、成员利益、派系 | 高 |
| 混合主权 | 君主-议会冲突、妥协、宪制危机 | 中高 |
| 继承方式 | 继承战争、合法性、贵族推举 | 高 |

具体机制：

- 双君主：决策需双方同意 → 延迟/僵局；关系恶化 → 内斗/内战；
- 共治皇帝：资深/ junior 权限不同 → 派系；
- 机构主权：法律由机构批准，首脑执行；机构成员利益影响立法；
- 混合主权：君主/议会互相否决 → 改革难/危机；
- 无中央最高：地方/部落自主，中央法 reach 低。

不要做“满意度 −5”，而是通过**批准权、否决权、派系、继承危机、执行延迟**表达。

---

## 6. 代码改造建议

### 6.1 新增枚举

```csharp
public enum SupremeAuthorityKind
{
    SingleMonarch,
    DualMonarch,
    MultiMonarch,
    CollegialConsuls,
    SovereignAssembly,
    MixedSovereignty,
    NoCentralAuthority,
}

public enum CoRulerRelation
{
    EqualColleagues,
    SeniorJunior,
    ParentChild,
    Siblings,
    Spouses,
    Rotating,
    MutualVeto,
}
```

### 6.2 `LawComposition` 增加字段

```csharp
public SupremeAuthorityKind supremeAuthority;
public CoRulerRelation coRulerRelation;
public int sovereignBodyInstitution; // 议会/长老会/宗教会议...
```

保留 `supremeSovereignty` 作为旧档迁移/兼容，不直接删除。

### 6.3 `LawEffects` 新效果

- 双/多君主：`stability`、decision delay、succession risk；
- 同僚制：任期/否决 → 政策波动；
- 机构主权：`legitimacy` 来源、立法速度；
- 混合主权：改革阻力/妥协；
- 无中央：`centralization` 低、地方 autonomy 高。

### 6.4 提案/批准权

`ProposalAuthority` 扩展：

```csharp
SovereignBody,
CoRuler,
CouncilOfEstates,
```

`CanPropose` 根据 A/B 判断谁有提案/批准权；不是所有法律都由首脑提案。

### 6.5 UI

- 最高权力归属：单/双/多/同僚/机构/混合；
- 共治关系：平等/资深/父子/兄弟/夫妻/轮值/否决；
- 最高机构：元老院/等级会议/长老会/宗教会议/军事委员会；
- 继承方式：按 A 动态显示。

---

## 7. 结论

- 你说得对：**最高权力由谁掌握**是核心维度，现有二元不够；
- 必须支持：共治、双/多君主、同僚制、最高机构、混合主权、无中央最高；
- 与继承方式、权力范围分开，不要压成一个选项；
- 早期用历史形态（长老会/双王/等级会议），现代项靠革新/时代解锁；
- 这会影响立法批准、宣战、继承、合法性、派系和执行延迟，模拟必要性高。

---

## 8. 学术参考

- [Consuls and Res Publica: The origin of the consulship](https://www.cambridge.org/core/books/abs/consuls-and-res-publica/origin-of-the-consulship-in-cassius-dios-roman-history/46285DE3E70A10F2E5BD624190AF3C67)
- [Diarchy? The System of Co-Rulership in Byzantium](https://hfrir.jvolsu.com/index.php/en/archive-en/770-science-journal-of-volsu-history-area-studies-international-relations-2020-vol-25-no-6/byzantine-state-and-society/2171-lysikov-p-i-diarchy-the-system-of-co-rulership-in-byzantium-at-the-turn-of-the-13th-14th-centuries-and-its-impact-on-the-situation-in-the-state)
- [The Kings Consort of Navarre: 1284–1512](https://scholar.google.co.nz/citations?view_op=view_citation&hl=en&user=EPYfBBoAAAAJ&citation_for_view=EPYfBBoAAAAJ:UeHWp8X0CEIC)
- [Unions and divisions: new forms of rule in Medieval and Renaissance Europe](https://search.library.yale.edu/catalog/99173066393408651)
- [Religion, Law and the Growth of Constitutional Thought 1150–1650](https://openyls.law.yale.edu/bitstream/handle/20.500.13051/7523/Carl_Landauer.pdf?isAllowed=y&sequence=2)
- [Birth of the Leviathan](https://www.cambridge.org/cl/universitypress/subjects/history/european-history-general-interest/birth-leviathan-building-states-and-regimes-medieval-and-early-modern-europe?format=PB&isbn=9780521484275)


---

# 修订：多头政治、法理化程度、东西分治、罗斯家产制

## 9. 不是“多君主”，而是“多头政治”

“多君主”仍然太窄。更准确的上位概念是 **多头政治 / 多中心主权（polycentric / multi-headed polity）**：

```text
多头政治
  ├─ 法理化共治（有明确规则/盟约/法律）
  │    ├─ 双王/双执政/共治皇帝
  │    ├─ 东西分治/帝国分区
  │    ├─ 同僚制最高官职
  │    ├─ 最高机构（元老院/等级会议/长老会）
  │    └─ 混合主权（君主+会议）
  └─ 非法理化/事实多头
       ├─ 军阀联盟
       ├─ 寡头集团
       ├─ 部落/氏族联盟
       └─ 军事委员会/junta
```

关键：**“多头”不等于“多君主”**，也不等于“共和”；它可能是法理化的，也可能只是事实上的权力分享。

## 10. 东西分治

东西分治不是“多君主”的同义词，而是 **分区最高权力/帝国共治**：

- 罗马帝国东西分治、四帝共治；
- 拜占庭共治皇帝（资深/ junior）；
- 法兰克王国分区继承；
- 复合君主制下的多王冠。

建议单独作为 `PartitionedRule`/`ImperialDivision`：

```text
单一帝国
  → 东西分治（两个最高统治者/两个宫廷）
  → 多区共治（四帝/多王）
```

它和“双王/双执政”不同：后者是同一政治体内的同僚，前者是**分区最高权力**。

## 11. 轮坐 = 罗斯家产制，不是通用轮值

“轮坐”不应作为抽象 rotation。其原型是 **罗斯的王公家产制/阶梯继承（rota / lestvichnyi poriadok）**：

- 王公把土地视为家产（otchina/patrimony）；
- 王公在多个公国间按辈分/资历“轮坐”；
- 不是现代官僚轮值，也不是共和制轮值；
- 与家族/家产/封地绑定。

建议：

- 把 `Rotation` 改为 `PatrimonialRotation`（家产轮坐）；
- 归入“继承/产生方式”，但注明是罗斯型家产制；
- 不与其他文化的轮值/抽签混用；
- 模拟效果：家族内部竞争、公国碎片化、中央弱。

参考：

- [The Rus' principalities (1125–1246) — Cambridge History of Russia](https://www.cambridge.org/core/books/abs/cambridge-history-of-russia/rus-principalities-11251246/E34ACDA963FD2080F778025DDC29C7BD)
- [Rethinking Ukraine: private succession / otchina](https://archive.org/download/rethinkingukrain00rudn/rethinkingukrain00rudn.pdf)
- [Sistema Rota](https://es.m.wikipedia.org/wiki/Sistema_Rota)
- [ЛЕСТВИЧНЫЙ ПОРЯДОК ЗАМІЩЕННЯ КНЯЖИХ СТОЛІВ НА РУСІ](https://resource.history.org.ua/cgi-bin/eiu/history.exe?C21COM=S&I21DBN=DOP&P21DBN=EIU&S21CNR=20&S21COLORTERMS=0&S21FMT=eiu_all&S21P01=0&S21P02=0&S21P03=TRN=&S21REF=10&S21STN=1&S21STR=Lestvichny_poryadok_zamischennya)

## 12. “神命”先删

同意：`SupremeSuccession.Divine` 与信仰系统强绑定，而信仰系统后面要重构，先删更干净。

处理（已执行）：

- 删除 `SupremeSuccession.Divine`；
- 删除 `succ_divine` 法律条目（当前 49 条法律）；
- 删除 `PolityComponentInnovations.A1Divine`；
- 删除“神权国”realm suffix 与神权政体模板；
- `SupremeScope.DivinelyBound` 也属于信仰约束，建议**暂时冻结/隐藏**，等信仰系统重构后再决定；
- 合法性来源改由“加冕/教会/传统/法律”等机制提供，不再用 `Divine` 选项。

## 13. 修订后的“最高权力”组

```text
A. 权力结构 AuthorityStructure
   ├─ 单一
   ├─ 法理化共治（双王/双执政/共治皇帝）
   ├─ 事实多头（军阀/寡头/氏族联盟）
   ├─ 分区最高权力（东西分治/四帝/多区）
   ├─ 同僚制最高官职
   ├─ 最高机构（元老院/等级会议/长老会）
   ├─ 混合主权（君主+会议）
   └─ 无中央最高（部落/分权）

B. 法理化程度 Legalization
   ├─ 成文/盟约/法律化
   ├─ 习惯/传统法理化
   └─ 纯事实/暴力均势

C. 共治关系 CoRulerRelation（多主体时）
   ├─ 平等同僚
   ├─ 资深/ junior
   ├─ 父子/兄弟/夫妻
   ├─ 分区（东西/多区）
   ├─ 相互否决
   └─ 家产轮坐（罗斯型）

D. 继承/产生方式
   ├─ 世袭（长子/诸子/选举君主）
   ├─ 推举/选举
   ├─ 指定/共治继承
   ├─ 篡夺/征服
   ├─ 家产轮坐（罗斯型）
   └─ 机构成员资格/任命
```

## 14. 模拟必要性

| 结构 | 作用 |
|---|---|
| 单一 | 决策快，继承风险集中 |
| 法理化共治 | 需协商/否决，可能僵局；规则可预期 |
| 事实多头 | 不稳定，政变/内战风险高 |
| 分区最高权力 | 中央弱、地方强，统一/分裂压力 |
| 同僚制 | 任期/否决/同僚竞争 |
| 最高机构 | 立法批准、成员利益、派系 |
| 混合主权 | 君主-会议冲突/妥协 |
| 无中央最高 | 中央法 reach 低，地方自主 |

不做抽象“满意度”，通过：批准权、否决权、继承危机、派系、执行延迟、分区离心力表达。

## 15. 结论

- 用“多头政治/多中心主权”替代“多君主”；
- 区分法理化共治与事实多头；
- 东西分治是“分区最高权力”，不是普通共治；
- 轮坐是罗斯家产制（PatrimonialRotation），不是通用轮值；
- 神命先删，信仰系统重构后再谈神圣合法性；
- `SupremeScope.DivinelyBound` 暂时冻结/隐藏，不直接删（等信仰重构）。
