# 需求四轴各出（社会四维落地形状）

> 2026-11-09 03:05（Asia/Shanghai）。用户裁决：社会四维 = 阶级 / 阶层 / 身份 / 职业；**需求按四轴各出**。

---

## 1. 裁决内容

- 不选单轴、不做交叉表：**四个维度各自产出一份需求报告**。
- 每份报告仍是现有那 8 个需求维（生存保障 / 人身安全 / 税负合理 / 政治通道 / 经济机会 / 制度承认 / 合法秩序 / 特权保障），但**评价对象不同**（阶级 8 值 / 阶层 5 值 / 身份 7 位 / 职业 14 位）。

## 2. 落地形状（已写进 D25）

```
NeedAxis { Class, Stratum, Identity, Occupation }
NeedReport { axis; key; dimensions[8]; worstDimension; score }
INeedQuery.GetReports(NeedAxis) -> IReadOnlyList<NeedReport>
```

1. **归桶（一次遍历人口块，四轴各自累加）**
   - 阶级 `EconomicClass`、阶层 `SocialStratum`：单值，各计 1 次；
   - 身份 `SocialIdentityFlags`、职业 `OccupationFlags`：**按位展开**，块人口 **按位数均分**（`count / bitCount`）——保证人口守恒，避免"一人既是农民又是兵"被算两遍。
   - 复杂度：仍是 O(人口块数)，每块多 2 次位展开，挂在 `population.daily` 之外单独一项更合适（建议随 `politics.daily` 每 25 日一算，见第 4 条）。
2. **评分**：8 维公式沿用；`ClassNeedWeightTable` 从"按 5 大类"扩成 **`(axis,key) → weights`**（四套权重，可内容化）。
3. **政权级 unrest**：每轴得出一个不满度（用 `worstDimension` 的缺口或 `1 − score`），按**该轴覆盖人口 × 轴权重**合成。轴权重进内容表，不写死代码。
4. **派系**：`FactionManager` 现在直接读 `needReport.dimensions[PoliticalAccess]`（`FactionManager.cs:114`），改为读"该派系所代表轴"的报告。
5. **存档**：报告是派生量，不入档（与现状一致）。但要注意 `RealmSociety` 的两个聚合字典现在 `[NonSerialized]`，四轴报告若要在 UI 复用，要么重算要么入档——建议重算（成本低）。

## 3. 性能与节奏

- 现在 `PoliticsSimulationSystem.SocietyPulse` 每 25 日做一次分化（`_differentiationTimer >= 25`），需求评估是**每日**（`Needs.EvaluateRealm`）。
- 四轴后单次成本约为原来的 `4 ×`（轴数）而非 `4 × keys`，因为归桶一次遍历即可；建议把需求评估降到**每 25 日与分化同频**，把"不满"当作慢变量（这也更符合布罗代尔：阶层心态不是日更的）。

## 4. 与重构的关系

- 这块属"重构中"（宪章 §7）：总纲只固定 `INeedQuery.GetReports(axis)` 这个读取契约与聚合语义，**不锁内部实现**。
- `ClassNeedsSystem.EvaluateClass(SocialClass)` 与 `ClassNeedWeightTable` 的签名必然改；UI（`SocietyPanel` / `ClassPanelText`）要跟着换成"按轴切换"的展示。
- legacy 迁移：`SocialClass`/`SocialSubclass` 保留为映射源与兼容查询，不再作为需求的评价键。

## 5. 两处细节（已确认，2026-11-09 用户"可以"）

1. **位标志轴的人口按位均分**（`count / bitCount`），保持人口守恒；不把 flags 组合当 key。
2. **四轴在政权 unrest 里的权重可配**，进内容表（不写死代码）。

验收断言：① 四轴各返回一组报告，键数 = 该轴出现的取值数；② 四轴人口合计 = 人口块总量；③ 改任一轴权重 → unrest 变化可复现。

## 6. 产物

| 路径 | 说明 |
|---|---|
| `DECISIONS.md` D25 | 改为"需求四轴各出"，含形状、归桶规则、聚合、待确认两项 |
| `docs/plan-v3/DESIGN_CHARTER.md` §7 | 四维与需求的口径更新 |
| `docs/plan-v3/SYSTEM_MASTER_PLAN.md` §3.0.1 + M4 | 登记与施工项（M4 增 ⑤⑥） |
| 本报告 | 落地形状说明 |
