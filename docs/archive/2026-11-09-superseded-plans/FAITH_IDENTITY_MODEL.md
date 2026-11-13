# 信仰系统借用族群认同机制

> 2026-11-05。对照：`ARCHITECTURE_2_0_LAW_OFFICE_NAME_REGION_MODEL.md` 5A/5A.5A 的族群认同机制。
> 结论：**信仰系统不该硬分层，应该借用族群认同的“嵌套 + 渠道 + 阀门 + 下沉度 + 边界”模型。**

---

## 1. 现有族群认同机制是什么

设计文档 5A/5A.5A 里，族群认同不是层级树，而是：

```text
Ethnos（族群认同）
  ├─ nestedParentId          // 外层容器，可空；多层并列挂靠
  ├─ reachByRole             // 五阶层 → Active / Passive / Absent
  ├─ salienceByRole          // 各阶层显著度
  ├─ accessChannels          // 法律/军队/共同体/教育/婚姻/官方媒介
  ├─ boundaryValves          // 血统/法律/宗教皈依/语言/仪式
  ├─ endogamyTendency        // 内婚
  ├─ classificationStrength  // 制度分类
  ├─ memorySolidity          // 谱系/起源记忆/仪式固化
  └─ coreTerritoryHeld       // 本土地理核心区保有
```

关键：

- 认同是**自我归属与边界**，不是文化内容；
- 文化可借用、可混合，认同可以不变；
- 同一人可持有多层认同，情境激活；
- 精英/大众下沉度不同：Active / Passive / Absent；
- 渠道决定认同怎么下沉；
- 阀门决定谁能进入 Active；
- 分离/吸纳/同化是独立动态。

代码现状：

- `PopulationIdentity` 目前只是社会阶级/身份 flags，不是族群认同；
- `EthnicGroupDef` 有定义但运行时未挂接；
- 5A/5A.5A 是设计，不是已实现；
- 所以信仰系统现在没有可借用的“认同实体”。

---

## 2. 信仰系统与族群认同的相似处

| 族群认同 | 信仰系统 |
|---|---|
| 自我归属/边界 | 信仰归属/ boundary |
| 文化内容可借用 | 教义/礼仪/实践可借用、混合 |
| 多层嵌套 | 家族/地方/教派/文明宗教容器 |
| Active/Passive/Absent | 精英虔诚/被动归属/无概念 |
| 渠道（法律/军队/教育/婚姻/媒介） | 教会/教区/寺院/朝圣/经典教育/婚姻/官方祭祀 |
| 阀门（血统/法律/皈依/语言/仪式） | 皈依/入教/割礼/饮食/礼拜/宣誓/授予 |
| 边界维持（内婚/分类/记忆/核心区） | 内婚/教籍/经典/圣地/礼仪边界 |
| 分离/吸纳/同化 | 分裂/改宗/融合/复兴 |

所以你说“有些东西很像”，是对的：**宗教归属本质上也是一种认同边界，不只是信念集合。**

---

## 3. 信仰系统应该怎么借

### 3.1 三层分离

```text
FaithTradition（内容）
  = 教义/礼仪/伦理/组织/物质实践的可组合条目

FaithCommunity（共同体/组织）
  = 具体教会/教团/寺坊/教派/修会/网络

FaithIdentity（归属/边界）
  = 谁认为自己属于哪个信仰共同体、边界怎么维持
```

- `FaithTradition` 像 `TraditionDef`：可组合、互斥、升格、固化；
- `FaithCommunity` 像文化群体/组织：有成员、财产、教职、圣地；
- `FaithIdentity` 像 `Ethnos`：多层、渠道、阀门、下沉度。

### 3.2 FaithIdentity 字段

```text
FaithIdentity
  ├─ identityId
  ├─ nestedParentId          // 外层宗教/文明容器
  ├─ communityId             // 主要共同体
  ├─ traditionIds[]          // 关联内容
  ├─ reachByRole             // Active/Passive/Absent
  ├─ salienceByRole
  ├─ accessChannels          // 教会/教区/寺院/教育/朝圣/婚姻/官方祭祀/军队
  ├─ boundaryValves          // 皈依/入教/割礼/饮食/礼拜/宣誓/教籍
  ├─ endogamyTendency
  ├─ classificationStrength  // 教籍/身份分类
  ├─ memorySolidity          // 经典/圣徒/圣地/仪式记忆
  └─ sacredCoreHeld          // 圣地/核心教区保有
```

- 不是所有社会成员同层虔诚；
- 农民可能 Passive/Absent，精英 Active；
- 城市/军队/教区/教育渠道决定下沉；
- 皈依/入教/婚姻/语言/仪式是阀门；
- 教籍/内婚/圣地/经典是边界维持。

### 3.3 动态

- **皈依/吸纳**：个人先改，群体滞后数代；渠道+阀门+制度；
- **同化**：少数信仰群体被多数本地信仰吸收；人口、通婚、语言、核心圣地丢失；
- **分裂/宗派化**：边界断裂 + 教义/礼仪/组织冲突 + 精英竞争；
- **融合/混合**：传统重叠、仪式借用，不一定合并认同；
- **复兴**：记忆固化 + 圣地 + 精英动员。

### 3.4 下沉度

```text
Active   = 主动认同、掌握经典/叙事、参与仪式
Passive  = 知道归属、能被分类，但不主动
Absent   = 只知本地/家族实践，对更大信仰容器无概念
```

这比“全民同信仰”真实得多。

---

## 4. 这样就不需要硬分层

```text
❌ family → religion → faith → rite 强制
✅ FaithTradition（内容组件）
   FaithCommunity（组织/网络）
   FaithIdentity（归属/边界，多层嵌套）
```

- `family/religion/faith` 只做可选标签/视图；
- `Rite` 只是 `communityType` 或 `tradition` 的一种；
- `FaithCommunity` 不是层级，是网络/组织；
- `FaithIdentity` 是认同边界，不是组织；
- 同一人可持多层信仰认同，情境激活。

---

## 5. 与现有代码的接口

- `ReligionDef` → `FaithTraditionDef` + `FaithCommunityDef` + 可选标签；
- `FaithSystem` → `FaithCommunity` 运行时；
- `ReligionNodeType` 的 `Tradition/School` 降为标签/网络；
- `DoctrineCategory`/`DoctrineOptionDef` → `FaithTraditionDef`；
- `SaintDef/CanonizationSystem` → 圣徒/主保，作为 memory/valve/channel；
- 教职/教会组织 → `TitleHolding`/`Office`；
- 法律/婚姻/饮食/历法 → 传统组件 + 法律系统。

---

## 6. 结论

- 信仰系统与族群认同机制高度相似；
- 应该借：嵌套、渠道、阀门、下沉度、边界维持、分离/吸纳/同化；
- 不应该硬套 family→religion→faith→rite 四层；
- `FaithTradition` 管内容，`FaithCommunity` 管组织，`FaithIdentity` 管归属/边界；
- `Rite/Denomination` 只是显示/类型标签，不是普适层；
- 这样才符合 lived/vernacular religion 和前现代宗教嵌入社会的实际。


---

# 修正：信仰是思想，不是共同体

## 7. 先认错

前面把信仰硬做成 `FaithCommunity`，是错的。

- **信仰是思想/观念/采纳**；
- **共同体/组织/实体是另一层**；
- 信仰共同体不是信仰本身；
- 共同体可以挂，也可以不挂。

## 8. 三层必须分开

```text
FaithTradition（思想/传统/流派）
  = 罗马礼、法相宗、禅宗、灭喜禅、日莲宗、临济宗……
  = 最低公开层级是“流派/传统”
  = 不是组织，不是共同体

PersonalFaith（个人信仰）
  = 个人采纳/相信/实践的东西
  = 可公开，也可私下/秘密
  = 可采纳个人化信条
  = 可挂可不挂

FaithCommunity / Organization（共同体/组织）
  = 教会、教团、寺坊、修会、宗派组织
  = 有成员、财产、教职、制度
  = 可选；不是信仰本身
```

## 9. 公开信仰 vs 个人信仰

```text
PublicFaith
  = 我对他人展示的信仰
  = 最低层级：流派/传统（罗马礼、禅宗、日莲宗……）
  = 社会识别用

PersonalFaith
  = 我个人采纳的信仰
  = 可包含个人化信条
  = 可与公开信仰不同
  = 可是秘密信仰
```

角色页应分：

```text
公开信仰：禅宗
个人信仰：禅宗 + 个人信条：某条已知信条
秘密信仰：无 / 某流派
```

## 10. 信条：照 CK3 的 Known/Core/Permitted/Prohibited

CK3 新机制里，信条在信仰内有状态：

- Core（核心）
- Permitted（允许）
- Prohibited（禁止）
- Known（已知）

个人可以把**已知信条**采纳为个人信条。

对应模型：

```text
FaithTradition
  ├─ tenets[]
  │    ├─ tenetId
  │    └─ status: Core / Permitted / Prohibited / Known
  └─ ...

PersonalFaith
  ├─ characterId
  ├─ publicTraditionId
  ├─ privateTraditionId
  ├─ personalTenetIds[]   // 从 Known 中采纳
  ├─ personalPractices[]
  ├─ observance
  └─ isSecret
```

- 公开信仰决定社会识别；
- 个人信仰决定角色内心/行为修正；
- 个人信条不能随便等于公开信条；
- Core 是公开信仰核心，Known 是可选个人采纳；
- Prohibited 不能公开采纳，但可秘密实践。

## 11. 流派粒度

公开信仰最低层是流派/传统：

- 罗马礼；
- 法相宗；
- 禅宗；
- 灭喜禅；
- 日莲宗；
- 临济宗（更细，是否必要待定）。

建议：

- 数据允许任意深度 `parentTraditionId`；
- UI 只显示到某个 **granularity**；
- 是否细分到临济宗，由内容/玩法决定，不硬编码；
- 同一层级不等于同一组织：禅宗可以有多个寺庙/教团，也可以没有统一组织。

## 12. 可挂可不挂

- `PersonalFaith` 可挂角色，也可不挂（默认继承公开信仰）；
- `FaithCommunity` 可挂流派，也可不挂；
- `FaithIdentity` 可挂社会边界，也可不挂；
- 没有组织的流派仍然存在；
- 没有公开信仰的人也可以有个人信仰/实践；
- 不要强制每个人/每个流派都有共同体。

## 13. 与族群认同机制的关系

族群认同模型仍可借，但只借**社会边界/渠道/阀门/下沉度**，不把信仰等于共同体：

```text
FaithTradition = 思想内容
PersonalFaith  = 个人采纳
FaithCommunity = 组织/实体（可选）
FaithIdentity  = 社会边界（可选）
```

- 信仰认同的边界可通过教籍、仪式、婚姻、教育、圣地维持；
- 但不等于必须有教会组织；
- 个人可以公开一个流派、私下采纳另一套信条；
- 组织可以跨流派，流派也可以无组织。

## 14. CK3 参考

- [CK3 开发日志 #1：信条 & 灵性满足](https://store.steampowered.com/news/app/1158310/view/526500154372522963)
- [CK3 Tenet Wiki](https://ck3.paradoxwikis.com/Tenet)
- [CK3 Tenets status: Core / Permitted / Prohibited / Known](https://store.steampowered.com/news/posts/?feed=steam_community_announcements&appids=1158310&appgroupname=Crusader+Kings+III&enddate=1781701624)

## 15. 结论

- 信仰是思想/采纳，不是共同体；
- 公开信仰最低层是流派/传统；
- 个人信仰可单独采纳信条，可秘密；
- 共同体/组织是另一层，可挂可不挂；
- 信条状态照 CK3：Core / Permitted / Prohibited / Known；
- 流派粒度可配置，不硬编码到临济宗；
- 族群认同模型只借边界/渠道/阀门，不把信仰做成共同体。
