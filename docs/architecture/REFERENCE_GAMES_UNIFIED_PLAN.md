# 参考游戏统一规划（2026-11-04）

> 把之前对参考项目的分析收束成一张统一规划：哪些借鉴、哪些不借鉴、落到哪个系统。
> 参考来源：[REFERENCE_PROJECTS_ANALYSIS.md](../design/REFERENCE_PROJECTS_ANALYSIS.md)。

---

## 1. 参考对象与借鉴边界

| 参考 | 借鉴 | 不借鉴 |
|---|---|---|
| **Earth Simulator** | 气候场/洋流/生态 NPP/肥力/灾害改场；每政权货币；长跑状态 | 其人口/粮食抽象、前端实现 |
| **FantasyMapSimulator** | `BiomeResource` 数据驱动（群系→资源概率/价值/修正） | 视觉/地图渲染配置 |
| **Things That Happen on the Map** | 板块→地形→气候→文明更迭；文明配置与地图分离；headless runner | 闭源 DLL 算法/数据格式 |
| **civ-atlas** | 球面网格、板块欧拉运动、侵蚀、风带/洋流、Whittaker、资源 0–3 档、确定性 RNG、分段生成 | AGPL 代码直接复制；一州一 Culture 的硬分区 |
| **CK3** | 革新时代/文化 fascination/区域变体、革新→兵种/建筑 | 硬编码“先青铜后铁”的科技链 |
| **Victoria 3** | 生产方式/配方（投入→产出）、市场与阶层需求 | 直接复制其数值/UI |
| **EU4** | 科技组/思潮/工场 | 时期标签当革新 |
| **Stellaris** | 科技权重/抽取/层级 | 随机科技树照搬 |

许可证边界：只借鉴模型/参数/数据 schema，不复制参考项目源码或数据格式；civ-atlas 为 AGPL-3.0，不复制代码。

---

## 2. 统一原则

1. **数据驱动**：群系资源、配方、革新、灾害参数都走 JSON；代码只做解析/求值。
2. **革新 = 能力/工艺/制度**：具体物品/建筑/单位走各自内容；`unlockRefs` 指向具体内容。
3. **硬前置只表达“不会就不能”**：`prerequisiteTree`；软前置是能力阈值（温度/工具/资源层级），不能退化成材料阶梯。
4. **配方连接材料**：`GoodsRecipe`（输入→输出 + 工艺/科技/设施门槛）承担“物资合成表”。
5. **资源三层**：露天/坑采/深井；能力 `mining.surface/shaft/deep`，不能白送深井。
6. **确定性**：生成/环境/灾害用 seed 派生 RNG；长跑 harness 断言守恒/无 NaN/预算。
7. **性能**：环境 tick 分条 + dirty 块 + 跨帧预算（Earth/civ-atlas 的做法）。
8. **领域隔离**：宗教/信仰/思想域冻结，等待后续重构；本轮只处理技术/制度/工艺/经济/军事。

---

## 3. 革新本轮范围

### 3.1 冻结

以下字段的革新**本轮不改内容**，只保留结构：

- `InnovationField.Philosophy`
- `InnovationField.Theology`
- `InnovationField.ReligiousInstitution`
- `InnovationField.Ritual`
- `InnovationField.SocialThought`

对应条目（当前）：

- 602 哲学、604 文艺复兴、601 一神教、809 宗族祭祀、810 冠婚丧祭、958 领土主权、982 治国论、1017 民族主义。

代码：`InnovationReviewPolicy.IsFrozen`。

### 3.2 本轮处理

- 非冻结条目：命名审计、具体 `unlockRefs`、`unlocks` capability 标签清理；
- 配方/工艺接入：201/202/203、308、400~406、807/1007/1010/1012/1014 等；
- 制度/行政/经济类：先保证 `unlockRefs` 不悬空，再判断是否需要法律/建筑/单位引用；
- 不做宗教/思想重命名，不碰其前置与定义。

### 3.3 审计指标

- 总革新：186；
- 冻结：8；
- 非冻结：178；
- 非冻结已有具体 `unlockRefs`：84；
- 非冻结仍只有 capability 标签：94。

---

## 4. 系统映射

| 参考 | 本项目落点 |
|---|---|
| 群系资源 | `GoodsDef.originBiomes/baseAbundance` + 未来 `BiomeResources.json` |
| 生产方式/配方 | `GoodsRecipe/RecipeCatalog/SettlementProduction` |
| 革新时代/扩散 | `InnovationTree`、`InnovationEra`、文化 affinity |
| 资源三层 | `ResourceAccessDepth` + `mining.*` |
| 货币 | `CurrencySystem/TreasuryLedger`；金银归 Metal，选最高价值金属 |
| 灾害改场 | `DisasterSystem` + TileData 环境字段 |
| 长跑 harness | headless 模拟 + 不变量断言（借鉴 ALAS 的 daemon/恢复模式） |

---

## 5. 实施顺序

1. 冻结域标记与测试（已完成 `InnovationReviewPolicy`）；
2. 非冻结革新命名/引用审计；
3. 配方字段/门禁（已完成）；
4. 资源三层/群系资源内容化；
5. 环境/灾害场效应；
6. 长跑 harness；
7. 宗教/思想域重构（延后，不在本轮）。

---

## 6. 不做什么

- 不把 CK3 的“时代+文化”直接搬成硬科技树；
- 不把 EU4 的时期词/科技组当革新；
- 不复制参考项目代码/数据；
- 不在本轮改宗教/思想条目；
- 不为了“全量”硬填没有具体内容的 `unlockRefs`。
