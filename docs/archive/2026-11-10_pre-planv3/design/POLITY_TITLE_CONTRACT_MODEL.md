# 政治实体 / 头衔 / 土地 / 契约 / 组织 / 法律：统一模型

> 2026-11-05。目标：一次性把“政治实体间关系、契约条约、组织、国际组织/跨国组织、头衔与土地、法律与官僚”收进同一套本体。
> 核心判断：**不能再用 `ownerRealmId` + “国家颜色”理解前现代政治**；要用 **头衔—持有者—土地—契约—法律秩序** 的图。

---

## 1. 先定词

| 词 | 定义 | 不是什么 |
|---|---|---|
| **政治实体 Polity** | 有法律秩序/统治关系的政治单位：王国、公国、城市、部落、帝国、复合体、联盟 | 不是“国家”现代概念 |
| **复合政治体 CompositePolity** | 由多个次级政治实体构成、有共同中央制度/头衔的实体（神罗、法兰克王国、复合君主制） | 不是国际组织 |
| **霸权网络/联邦式网络 HegemonicConfederacy** | 多个自治政治实体承认一个象征性/资深中心（罗斯、爱尔兰） | 不是中央集权国家 |
| **政治实体间组织 InterPolityOrganization** | 成员是政治实体的组织：联盟、同盟、邦联、条约组织 | 不是跨国组织 |
| **跨国组织 TransnationalOrganization** | 跨政治实体、但不拥有领土主权的组织：教会修会、行会、公司、网络 | 不是国际组织 |
| **头衔 Title** | 法律地位/权利束，不等于土地，也不等于政治实体 | 不是某个人的属性 |
| **聚落/控制网 SettlementControlNetwork** | 以聚落为最小单位、由控制关系连接的网络 | 不是 `Territory` 实体；代码里没有 Territory |
| **契约 Contract** | 两方/多方之间的权利义务；不传递 | 不是“归属” |
| **法律秩序 LegalOrder** | 一套规范+管辖+执行的系统，挂在政治实体/头衔/契约上 | 不等于一个政权 |

---

## 2. 法兰克：共有头衔 = 共治，不是分裂

- 法兰克王权常由家族内成年男性成员共享/分领；`regnum Francorum` 仍是一个政治体；
- “法兰克人之王”头衔可在家族内多人共有；
- 不是把王国切成几个独立国家，而是同一王权下的共治/分领；
- Salic 法后来被用来强化继承规则，但不能简化成“萨利克法=长子继承”。

模型：

```text
TitleInstance: FrankishKingdom
  ├─ TitleHolding(person A, share/role=senior)
  ├─ TitleHolding(person B, share/role=co-king)
  ├─ TitleHolding(person C, share/role=co-king)
  └─ SharedTitleRule: family adult males with land
```

- 头衔共有 ≠ 政治实体分裂；
- 每个共治者可能有自己的宫廷/领地，但共同王权仍是一个法律秩序；
- 共治者之间的契约/习惯决定继承、分领、战争。

参考：

- [The Frankish Kingdoms, 817–911 — New Cambridge Medieval History](https://www.cambridge.org/core/books/abs/new-cambridge-medieval-history/frankish-kingdoms-817911-the-east-and-middle-kingdoms/08AD8BCF8109DCB91EC08FFDCA97EACE)
- [The Salic Law and the Valois succession to the French crown](https://scholar.google.com.vn/citations?view_op=view_citation&hl=en&user=aeDkOkYAAAAJ&citation_for_view=aeDkOkYAAAAJ:4DMP91E08xMC)

---

## 3. 罗斯：不是朝贡，是霸权式邦联

- 各王公/宗长各自管理领地；
- 基辅大公被尊为**至高宗长**，但不是直接统治者；
- 关系不是朝贡，更像**霸权式邦联/宗族网络**；
- `rota`/阶梯继承是家产制，不是现代轮值。

模型：

```text
HegemonicConfederacy (Rus)
  ├─ SeniorTitle: GrandPrinceOfKiev（象征/仲裁/荣誉）
  ├─ MemberPolity: Principality of ...
  │    ├─ own Title, own druzhina, own law
  │    └─ Contract: kinship/seniority, not tribute
  └─ No direct central administration
```

- 中央没有常设官僚/税收/军队；
- 成员只承认 seniority/宗族秩序；
- 统一/分裂是网络关系变化，不是“省份独立”。

参考：

- [The Rus' principalities (1125–1246) — Cambridge History of Russia](https://www.cambridge.org/core/books/abs/cambridge-history-of-russia/rus-principalities-11251246/E34ACDA963FD2080F778025DDC29C7BD)
- [Sistema Rota](https://es.m.wikipedia.org/wiki/Sistema_Rota)
- [ЛЕСТВИЧНЫЙ ПОРЯДОК](https://resource.history.org.ua/cgi-bin/eiu/history.exe?C21COM=S&I21DBN=DOP&P21DBN=EIU&S21CNR=20&S21COLORTERMS=0&S21FMT=eiu_all&S21P01=0&S21P02=0&S21P03=TRN=&S21REF=10&S21STN=1&S21STR=Lestvichny_poryadok_zamischennya)

---

## 4. 爱尔兰：推举的至高王

- 至高王 `Ard Rí` 不是世袭中央君主，常由次级王/贵族推举/承认；
- 次级王国高度自治；
- 更像**选举霸权网络**，不是中央集权国家。

模型：

```text
ElectiveHegemony (Ireland)
  ├─ HighKingship（elective/recognized）
  ├─ SubKingdoms（autonomous）
  └─ Contracts: tribute? clientship? personal ties
```

参考：

- [Irish Kings and High-kings](https://books.google.com.sg/books?id=tY9nAAAAMAAJ)

---

## 5. 神罗：复合政治体，不是“国际组织”

神罗不是“组织”，是**实体**，底下有大量次级实体：

```text
CompositePolity: Holy Roman Empire
  ├─ ImperialTitle（elective）
  ├─ Electors / Princes / Cities / Bishoprics
  ├─ Imperial institutions: Diet, courts, circles
  └─ Member polities retain rights/armies/laws
```

- 次级邦国是“历来就有”的，不是中央创建的行政区；
- 大家“加盟”，让渡部分权利给中央；
- 像联邦的源头，但不是现代联邦；
- 不能叫国际组织，因为成员不是“外国”，而是帝国成员。

参考：

- [Still a Monstrosity? Reflections on Early Modern German Statehood](https://www.cambridge.org/core/journals/historical-journal/article/abs/still-a-monstrosity-some-reflections-on-early-modern-german-statehood/6CACFDF1E619317C00FB72D0D316C9F7)
- [A Europe of Composite Monarchies — Elliott](https://scholar.google.com/scholar_lookup?hl=en&volume=137&publication_year=1992&pages=48-71&journal=%0APast+and+Present%0A&author=John+H.+Elliott&title=A+Europe+of+Composite+Monarchies.)

---

## 6. 法兰西/英格兰：契约网络，不是“国家吞并”

- 英王在法国有封臣/领地，但那些领地仍是法国王冠下的采邑；
- 不能把“英王在法国的领地”直接画进“英格兰政治实体”；
- 应看：**土地/头衔的持有者与谁的契约**；
- A-B 有契约，B-C 有契约，不代表 A-C 有直接契约；
- 同一个人可以既是某人的封臣，又是某地的国王。

模型：

```text
Person: KingOfEngland
  ├─ holds Title: KingOfEngland
  ├─ holds Title: DukeOfAquitaine (liege: KingOfFrance)
  └─ Contract: homage/fealty to KingOfFrance for Aquitaine

Territory: Aquitaine
  ├─ sovereigntyTitle: KingdomOfFrance (ultimate)
  ├─ possessionTitle: DuchyOfAquitaine
  ├─ holder: KingOfEngland (personally)
  └─ contract: French crown–English king
```

- 土地/城镇的“归属”“领有”“管辖权”分开；
- 不是 `ownerRealmId=England`；
- 战争/外交/法律义务按 title/contract 走，不按“国家颜色”。

参考：

- [A Europe of Composite Monarchies — Elliott](https://scholar.google.com/scholar_lookup?hl=en&volume=137&publication_year=1992&pages=48-71&journal=%0APast+and+Present%0A&author=John+H.+Elliott&title=A+Europe+of+Composite+Monarchies.)
- [Fiefs and Vassals — Reynolds](https://global.oup.com/academic/product/fiefs-and-vassals-9780198206484)

---

## 7. 统一数据模型

### 7.1 政治实体

```csharp
Polity {
  int polityId;
  List<int> titleIds;          // 头衔决定它“是什么”
  List<int> memberPolityIds;   // 有成员关系才是复合/霸权/邦联
  int liegePolityId;           // 有从属关系才有宗主
  List<int> contractIds;
  LegalOrderId legalOrderId;
  // 不设 PolityKind：形态由 title/member/liege/institution 推导
}
```

### 7.2 头衔

```csharp
TitleDef { id, name, rank, titleKind, customarySuccession, claimRules }

TitleInstance {
  int titleId;
  List<int> settlementIds; // 没有 Territory 实体
  List<TitleHolding> holders; // 可多人共有
  int liegeTitleId;
  List<int> contractIds;
  RightsBundle rights;
}

TitleHolding {
  int personId;
  TitleRole role; // sole/senior/co-king/elector/...
  float share; // 共有份额，不是土地分割
}
```

### 7.3 土地

```csharp
Territory {
  List<int> settlementIds; // 没有 Territory 实体
  int sovereigntyTitleId; // 最高法理
  int possessionTitleId;  // 领有
  int administrativePolityId;
  RightsBundle rights;    // 司法/征税/军事/通行/任命
  List<int> contractIds;
}
```

### 7.4 契约

```csharp
Contract {
  int contractId;
  ContractKind kind; // homage/fealty/lease/treaty/charter/alliance
  List<ContractParty> parties; // person/title/polity
  List<Obligation> obligations;
  int startDay, endDay;
  bool inheritable;
  BreachRules breach;
}
```

- 契约不传递：A-B、B-C 不生成 A-C；
- 契约可绑定 person/title/territory；
- 违约/战争/继承会改变图，但不自动合并。

### 7.5 组织

```csharp
Organization {
  int orgId;
  OrganizationKind kind;
  // InterPolityOrganization: members are polities, treaty-based
  // TransnationalOrganization: members are persons/groups, no territory
}
```

- `CompositePolity` 不是 Organization；
- 不设 `PolityKind`：复合/霸权/邦联是关系与头衔的结果，不是枚举标签；
- `InterPolityOrganization` 成员是 polity；
- `TransnationalOrganization` 成员是人/团体，跨政治实体但不拥有领土主权。

---

## 8. 法律系统怎么接

- `LegalOrder` 挂在 polity/title/contract，不只挂 realm；
- 英王在法国的领地适用法国王冠的法律/契约，不自动适用英格兰法；
- 共有头衔的共治者可能有共同/各自法律秩序；
- 神罗：帝国法律 + 邦国法律 + 城市法 + 教会法，多层；
- 罗斯：各公国法律 + 宗族/ seniority 习惯；
- 官僚因子挂在 polity/title/office，不挂“国家”一个浮点。

```text
Title/SettlementControlNetwork
  → LegalOrder（适用哪套法）
  → Contract（义务/特权）
  → Bureaucracy（执行能力）
  → Law/Decree（规范）
```

---

## 9. 模拟必要性

| 机制 | 必要 | 作用 |
|---|---|---|
| 头衔共有/共治 | 高 | 法兰克共治、继承、内战 |
| 复合政治体 | 高 | 神罗、复合君主制 |
| 霸权网络/邦联 | 高 | 罗斯、爱尔兰 |
| 契约图 | 高 | 封臣/封君、跨海领地、义务 |
| 土地权利束 | 高 | 归属/领有/司法/税收分离 |
| 政治实体间组织 | 中高 | 联盟、邦联、条约组织 |
| 跨国组织 | 中 | 修会、行会、公司 |
| 法律秩序多层 | 高 | 跨法域、特许、城市法 |

不做：

- 不把 HRE 叫国际组织；
- 不把英王法国领地直接算英格兰；
- 不把共治当分裂；
- 不用 `ownerRealmId` 单层覆盖。

---

## 10. 落地顺序

1. 定义 `Polity/Title/SettlementControlNetwork/Contract/Organization/LegalOrder` 本体；
2. 把头衔从“人属性”改为 `TitleInstance + TitleHolding`；
3. 聚落控制从 `ownerRealmId` 改为 `controllerAnchorId/controlledAnchorIds` + 权利束 + title；
4. 契约图 + 非传递性；
5. 政治实体关系/组织区分；
6. 法律秩序挂接；
7. UI 分层：政治实体/头衔/契约/组织/法律；
8. 旧档迁移。

---

## 11. 学术参考

- [The Frankish Kingdoms, 817–911 — New Cambridge Medieval History](https://www.cambridge.org/core/books/abs/new-cambridge-medieval-history/frankish-kingdoms-817911-the-east-and-middle-kingdoms/08AD8BCF8109DCB91EC08FFDCA97EACE)
- [The Salic Law and the Valois succession](https://scholar.google.com.vn/citations?view_op=view_citation&hl=en&user=aeDkOkYAAAAJ&citation_for_view=aeDkOkYAAAAJ:4DMP91E08xMC)
- [The Rus' principalities (1125–1246)](https://www.cambridge.org/core/books/abs/cambridge-history-of-russia/rus-principalities-11251246/E34ACDA963FD2080F778025DDC29C7BD)
- [Sistema Rota](https://es.m.wikipedia.org/wiki/Sistema_Rota)
- [Irish Kings and High-kings](https://books.google.com.sg/books?id=tY9nAAAAMAAJ)
- [A Europe of Composite Monarchies — Elliott](https://scholar.google.com/scholar_lookup?hl=en&volume=137&publication_year=1992&pages=48-71&journal=%0APast+and+Present%0A&author=John+H.+Elliott&title=A+Europe+of+Composite+Monarchies.)
- [Still a Monstrosity? Early Modern German Statehood](https://www.cambridge.org/core/journals/historical-journal/article/abs/still-a-monstrosity-some-reflections-on-early-modern-german-statehood/6CACFDF1E619317C00FB72D0D316C9F7)
- [Fiefs and Vassals — Reynolds](https://global.oup.com/academic/product/fiefs-and-vassals-9780198206484)
- [Polycentric sovereignty](https://scholar.google.de/citations?view_op=view_citation&hl=fi&user=Ql1JNb0AAAAJ&citation_for_view=Ql1JNb0AAAAJ:Fu2w8maKXqMC)
