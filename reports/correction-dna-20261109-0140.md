# 更正报告：DNA 系统已在跑，我说错了

> 2026-11-09 01:40（Asia/Shanghai）。更正对象：上一轮报告与宪章 §1.6 中"所有人的差异恒等于零"的论断。

---

## 1. 错在哪

我上一轮写：`RaceDefs.json` 0 条 + 代码硬编码"人族"全 1f ⇒ **所有人的差异恒等于零**。

**这是错的。** 个体差异来自 **DNA 系统**，它存在、有内容、真在跑；种族提供的是**群体基线**，不是个体差异的来源。把两者混为一谈，等于把用户已经写好的系统当成不存在。

---

## 2. DNA 系统的实际状况（逐条带锚点）

| 环节 | 事实 | 证据 |
|---|---|---|
| 基因座 | 6 个：`Longevity / Intelligence / Martial / Resistance / Appearance / TalentDefect` | `DnaSystem.cs:24-31` 的 `ActiveLoci` |
| 初始生成 | 每个位点按**种族提供的显性频率**抽两个等位基因（种族缺失时缺省 0.5） | `DnaSystem.cs:210-215`；`RaceData.GetLocusAFrequency`（`:160-173`，无 `locusFrequencies` → 0.5） |
| 遗传 | 子代从父母各取一个等位基因，按位点突变率 0.0075 / 才能位点 0.015 | `DnaSystem.cs:226-258` |
| 表达 | 6 位点 → 寿命偏移（±`lifespanRangeYears`=15 年）、智/武/抗偏移（±15）、外观、才能/缺陷 | `DnaSystem.cs:263-278, 295, 310`；`RaceData.lifespanBaseYears=75`、`intelligenceBaseline=50` 等 |
| 多基因座基因组 | `GeneticsEngine`(616 行) 支持重组/交换/突变/近交；`GenomeInstance` 入档 | `GeneticsEngine.Reproduce`；`CharacterSaveData.cs:184` |
| 内容 | `DnaDefs.json` **8 条**：4 才能（`talent_photographic`+learn5、`talent_divine_strength`+martial5、`talent_iron_body`+lifespan10、`talent_keen_mind`+learn3）+ 4 缺陷（`defect_frail`−lifespan15、`defect_feeblemind`−learn20、`defect_hemophilia`−lifespan10、`defect_pale`） | `StreamingAssets/Base/Dna/DnaDefs.json`（Lead 实测） |
| 表现型 | `ComputePhenotype` → `GeneticsAppearance.Build` → `appearance` | `CharacterData.cs:277-280`（`RefreshAppearance`） |

**结论**：个体差异真实存在，且**可遗传、可突变、可表达为属性**——这正是"这个人到底是什么玩意儿"的物质来源。

---

## 3. 种族层的真实作用与真实空缺

**种族提供的是群体基线**（`RaceData`）：

- 每个基因座的显性等位频率（`locusFrequencies`；缺失 → 全部 0.5）
- 基因组 / 物种 id（`genomeId="genome_human"`、`speciesId="species_human"`）
- 智/武/抗三项基准（各 50）、寿命基准 75 年 ± 15
- 冷/热/旱/湿/海拔五项耐受 → `CalculateEnvironmentFitness`（`:175-182`）→ `CalculatePopulationGrowthRate`（`:184-189`）
- 军事偏好（步/骑/海/士气）、物产生产/消费偏好（现 `[NonSerialized]`）、变革性
- 环境适配 → 疫病抗性折算（`DiseaseSystem.cs:203`）

**`RaceDefs.json` 为空的真实代价**：不是"没有差异"，而是**没有群体分化**——所有族群共用同一个 `genome_human` 与同一组等位频率（0.5），所以"不同物种/亚种在恶地/寒地/湿地上分化出不同人口曲线"这件事无法发生。这才是 P0 的准确表述。

---

## 4. 真正该修的四件事（已写进计划）

1. **差异到不了历史**（最重）：`GetEffectiveStats` 只有 4 个下游（研究突破 / 派系力量 / 法令传播 / 行军速度）；`FindBestGeneral` / `CalculateRuleAbility` / `CalculateOverallAbility` 全零调用 ⇒ 智/武/寿命再高，不影响选将、立法、继承。
2. **表型只算不存**：`RefreshAppearance` 零调用（`ComputePhenotype` 在生产链上从不执行）、`appearance` 不入档。
3. **读档漂移**：legacy `DnaData` 读档置 null（`CharacterSaveData.cs:529`）改变衰老曲线；`genome` 读档从 dna 派生 vs 新局随机创始人 ⇒ 同一角色读档前后等位基因不同。
4. **群体分化缺失**：补 `RaceDefs`（≥3 条，差异落在等位频率/基因组/基准/耐受），去掉 `productionModifiers/consumptionModifiers` 的 `[NonSerialized]`。

---

## 5. 已同步的文档改动

| 文档 | 改动 |
|---|---|
| `DESIGN_CHARTER.md` §1.6 | 重写为"种族（群体基线）+ DNA（个体差异）"两层表；明确"个体差异存在，来自 DNA"；列出四项真实缺口 |
| `SYSTEM_MASTER_PLAN.md` §0 第 11 条 | 由"所有人的差异恒等于零"改为"DNA 在跑、种族层空、差异到不了决策" |
| `SYSTEM_MASTER_PLAN.md` §3.0 S14 | 改为两层描述 + 逐条锚点 + 四项缺口 |
| `SYSTEM_MASTER_PLAN.md` M3 | "补生物学基底"改为"补种族层（群体基线）"；DoD 改为"同地理、不同种族 → 不同人口与扩张" |
| `DECISIONS.md` D21 | 重写：更正错误论断，明确种族=群体基线、DNA=个体差异，并给出五项施工 |

---

## 6. 教训（写下来避免再犯）

1. **先查系统有没有，再判断它空不空**。我看到 `RaceDefs.json` 是空的就下"差异为零"的结论，没查 DNA 这条链——这是"只看一个文件就推断全局"。
2. **"内容空"与"机制空"是两回事**：DNA 的内容（8 条才能/缺陷）在，机制也在；种族是内容空，但它的**基线字段**仍在生效（50/75/耐受）。
3. 三个"空"要分开说：**内容空**、**机制空**、**消费空**（差异产生了但没人用）。本项目最普遍的是第三种，而我这次把第一种误报成第三种。
