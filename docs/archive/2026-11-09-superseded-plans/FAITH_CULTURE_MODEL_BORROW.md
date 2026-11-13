# 信仰系统借用文化机制：放弃硬分层

> 2026-11-05。先读代码：`CultureData`、`TraditionDef`、`CultureSimilarity`、`CultureTransition`、`CultureLineage`、`FaithSystem`、`ReligionDef`、`DoctrinePool`。
> 结论：**信仰系统应该借文化机制，而不是硬套 family→religion→faith→rite 四层。**

---

## 1. 文化机制实际怎么做的

### 1.1 文化不是硬分层，是“传统集合 + 因子 + 关系”

```text
CultureData
  ├─ parentCultureId / childCultureIds
  ├─ traditionIds[]        // 可组合的传统集合
  ├─ ethosId / heritageId
  ├─ languageId / languageFamilyId
  ├─ faithId
  ├─ maturity / spreadPower
  └─ ...（旧七板块已废弃，改由传统因子推导）
```

### 1.2 传统是可组合、可冲突、可升格、可固化的组件

`TraditionDef` 有：

- `category`：Habitation / Production / Political / Ritual / Societal；
- `effects` / `factors`；
- `incompatibleWith`：传统之间互斥；
- `requiredInnovations`：革新门槛；
- `exclusiveToCultureIds`：文化限定；
- `upgradesFrom`：升格路径；
- `solidification`：固化强度；
- `linkedInnovationIds`；
- `minRole` / 角色门槛。

这比“每层一个标志”灵活得多。

### 1.3 相似度靠集合重叠 + 因子，不靠层级

`CultureSimilarity`：

- `CalculateTraditionOverlap`：传统集合 Jaccard；
- `CalculateFactorSimilarity`：四因子向量；
- `CalculateFullSimilarity`：传统通道 + 旧七维回退；
- 分离/融合阻力、传播速率都从因子算。

### 1.4 文化有谱系/分支

`CultureData.parentCultureId/childCultureIds`；
`CultureLineage` 从语言谱系补父文化。

---

## 2. 信仰硬分层的问题（对照文化机制）

| 硬分层 | 问题 | 文化机制的做法 |
|---|---|---|
| family→religion→faith→rite | 很多传统没有这些层 | 没有硬层，只有传统集合 |
| 每层一个标志 | 标志重叠、伪精度 | 传统有 category/tags/factors |
| denomination 普适 | 基督教中心 | 用数据驱动标签，不硬编码 |
| rite 最低单位 | 礼仪中心 | 共同体由传统集合涌现 |
| 教义/礼仪/组织分属不同层 | 词性不平行 | 传统可跨 category，组织另立 |

---

## 3. 宗教学依据

- **lived religion**：宗教是日常实践、身体、物质、地方共同体的 lived 经验，不只是官方教义/层级；
- **vernacular religion**：普通人的宗教实践，常与官方/制度版本不同；
- **宗教范畴批判**：`religion`、`denomination`、`world religions` 是近现代/基督教中心的建构，不是普适实体；
- **多孔/嵌入宗教**：前现代宗教常嵌入政治、法律、族群、地方生活，不是独立“宗教系统”；
- **多义/多形**：同一传统内部实践差异大，不宜用一个固定层级树。

参考：

- [Lived religion / vernacular religion 研究](https://pure.au.dk/ws/portalfiles/portal/428923059/Collective_Testimonies_to_Christianity_and_Time_Anne_Agersnap_Dissertation.pdf)
- [Objects and Ancient Religions](https://oxfordre.com/religion/display/10.1093/acrefore/9780199340378.001.0001/acrefore-9780199340378-e-22)
- [Talal Asad / religion 范畴批判](https://www.jstage.jst.go.jp/article/rsjars/100/1/100_1/_article/-char/en)
- [J.Z. Smith on religion](https://www.taylorfrancis.com/chapters/edit/10.4324/9781315267678-4/smith-study-religion-humanities-human-nature-kurtis-schaeffer)
- [Categorizing Religious Organizations](https://academic.oup.com/edited-volume/28304/chapter-abstract/214986854)

---

## 4. 建议模型：信仰传统集合 + 共同体

### 4.1 传统组件

```text
FaithTraditionDef
  ├─ traditionId
  ├─ category: Cosmology / Soteriology / Ethics / Ritual / Organization / Political / Material
  ├─ effects / factors
  ├─ incompatibleWith[]
  ├─ requiredInnovations[]
  ├─ upgradesFrom
  ├─ solidification
  ├─ linkedInnovationIds[]
  ├─ scope: individual/community/region
  └─ exclusiveTo...
```

- 像 `TraditionDef` 一样数据驱动；
- 可以互斥、升格、固化；
- 不预设层级；
- 教义、礼仪、伦理、组织都是传统组件。

### 4.2 共同体

```text
FaithCommunity
  ├─ communityId
  ├─ traditionIds[]       // 核心集合
  ├─ organizationId       // 可选
  ├─ practices[]          // 地方实践
  ├─ languageId
  ├─ calendarId
  ├─ holySiteIds[]
  ├─ patronId
  ├─ legitimacy / fervor
  └─ communityTypeId      // 可选显示标签
```

- 可玩单位；
- 由传统集合 + 组织 + 实践涌现；
- 不是固定层级节点；
- 相似度用传统重叠 + 因子；
- 分裂/融合/改革用传统增删/替换。

### 4.3 分组是视图，不是层

```text
FaithFamily / Religion / Faith
  = 标签/视图/地图分组
  ≠ 强制层级
```

- 可以给共同体打 `familyId/religionId` 标签；
- 但共同体不必须属于某个 faith；
- 不强制 `denomination/rite`；
- `communityTypeId` 只做显示（礼仪/宗派/教法学派/部派/法脉/宗/共同体）。

---

## 5. 运行时机制

### 5.1 相似度

```text
CommunitySimilarity =
  tradition overlap (Jaccard)
  + factor similarity
  + organization similarity
  + practice similarity
```

### 5.2 分裂/融合

- 传统集合差异大 + 分离倾向高 → 分裂；
- 传统重叠高 + 融合倾向高 → 融合；
- 政治/法律/组织压力可强制/阻止。

### 5.3 改革

- 增删传统；
- 替换互斥传统；
- 升格传统；
- 改变组织/礼仪/语言；
- 需要合法性/革新/圣地/领袖。

### 5.4 不满/冲突

- 传统互斥 → 冲突；
- 组织/教职利益 → 派系；
- 地方实践差异 → 地方抵抗；
- 不做单一“宗教满意度”。

---

## 6. 与法律的接口

- `FaithTraditionDef` 可挂 `LawDef`/`LegalNorm` 前置；
- 宗教法、教法庭、婚姻/继承/饮食/历法规则由传统组件提供；
- 法律系统不按 religion/denomination 硬编码；
- 特许/自治可覆盖地方实践。

---

## 7. 结论

- 文化机制更适合信仰系统：**传统集合 + 因子 + 互斥/升格/固化 + 相似度**；
- 放弃 `family→religion→faith→rite` 硬分层；
- `FaithCommunity` 是可玩单位，由传统集合涌现；
- `FaithTraditionDef` 是组件，不是层级；
- `family/religion` 只做可选标签/视图；
- `communityTypeId` 只做显示，不决定结构；
- 宗教学上的 lived/vernacular/embedded 研究支持这种“实践/传统集合”模型。
