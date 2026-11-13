# DNA 与独立物种计划（D 系列）

> 目标：让 DNA 从“六个数值字段”变成**有模拟意义的遗传系统**，并能用同一套基因组生成普通物种与幻想物种（如膜翼、角、鳞皮的飞行兽），为 P3.2 的独立物种层提供前置。
> 原则：机制优先于数值；每个位点必须有机制消费者；内容驱动、可模组；确定性；旧档可迁移。

---

## 0. 现状审计（2026-11-13）

| 项 | 现状 | 缺口 |
|---|---|---|
| 基因组定义 | `GeneticsCatalog.CreateHumanGenome()` 写死在代码：3 条染色体、6 个位点（longevity/intelligence/martial/resistance/appearance/talent_defect） | 不能从内容表加载，Mods 无法新增物种 |
| 物种定义 | `CreateDefaultSpecies()` 写死人类/鹿/狼/橡树；`RaceDefs.json` 为空 | 没有“物种内容表”；幻想物种只能改代码 |
| 等位基因 | `AlleleDef` 有 additive/dominance/environment/appearance 四类效果 | 没有离散“身体构造”效果（翼/角/尾/皮） |
| 表型 | `PhenotypeResult` 只有 trait 数组 + 外观通道 | 没有身体构造、代谢、体型等结构化表型 |
| 遗传 | `GeneticsEngine.Reproduce` 已支持重组/突变/近交 | 新位点加入后旧 `GenomeInstance` 数组会短一截，需迁移 |
| 机制消费 | trait 值被角色创建/疾病/继承少量消费 | 没有“翼→侦察/机动、角/爪→个人战力、鳞皮→防护、代谢→耐温/口粮”的接线 |
| 物种隔离 | `ReproductiveIsolation`/`HybridizationResult` 已有 | 没有物种目录与“由基因组推导物种”的入口 |

**结论**：DNA 的“字段”有了，但“模拟意义”和“物种生成”都还没闭环；本计划补这两块。

---

## 1. 总原则（继承宪章与既有决策，不另立一套）

本计划不新造原则，继续执行此前定下的总原则：

1. **架构分层**：纯定义进 `Content.Definitions`，运行逻辑留在 Simulation/World；不新增平行系统；单一真源（基因组、物种、表型各一条权威链）。
2. **文件/代码架构**：保持 CK3 式“定义表 / 运行态 / 引擎”分离与程序集边界；新内容走 `Base/...` JSON + `Mods` 覆盖，不把内容写死进代码。
3. **性能预算**：每个新增系统声明复杂度（O(N)/O(dirty)/O(1)），挂调度预算/增量；查询不写状态；内容表只在初始化读一次，运行期查字典。
4. **并行与锁步**：表型计算是纯函数可并行；写状态（迁移、繁殖落盘）保持在主 tick 锁步；不引入跨系统异步写。
5. **模拟真实性（尽量）**：优先用机制/因果链（翼→侦察、鳞皮→耐温、角爪→近战、食性→口粮）而不是抽象加成；必要数值必须是可解释的生理/物理量。
6. **确定性**：随机只来自 world seed 派生的 `System.Random`；表型、物种判定、存档迁移全部可复现。
7. **旧档与内容兼容**：新增字段可选、缺省有明确语义；旧 `GenomeInstance` 按基因组版本补齐；坏内容跳过并告警，不拖垮整包。
8. **可维护/可测试**：每批先读现有代码；表驱动 + 单元测试；`build` 与 `longrun` 全绿后提交。

### 1.1 DNA 专项规则

1. **每个位点一个机制消费者**：位点必须能回答“它改变哪条因果链”，否则不进表。禁止只做展示的装饰位点。
2. **离散性状用等位基因表达，不用随机标签**：翼型、角型、皮膜、尾型等由 `AlleleTraitEffect` 的离散通道表达；表现型由基因型推导，不额外抽卡。
3. **物种是基因组的“可育边界 + 身体蓝图”，不是另一套属性表**：`SpeciesDef` 引用 `genomeId`，身体蓝图基线来自基因组；物种差异应由基因频率/固定等位基因产生。
4. **幻想物种不写特判**：瓦伊凡（膜翼、角、鳞皮、长尾）只能由内容 JSON 的基因组/物种条目生成；代码里不得出现 `if (species == "wyvern")`。
5. **旧档不炸**：新位点缺等位基因时按“该位点的常见等位基因”补齐；迁移只做一次，读写往返稳定。
6. **确定性**：重组/突变只由 `System.Random`（seed 派生）驱动；表型计算纯函数、无随机。

---

## 2. 施工阶段

### D1 基因组/物种内容化
- 新增 `StreamingAssets/Base/Genetics/Genomes.json`：`{ genomes: [GenomeDef...] }`；人类基因组从代码搬到表，代码保留 `CreateHumanGenome()` 作为无内容环境兜底。
- 新增 `StreamingAssets/Base/Genetics/Species.json`：`{ species: [SpeciesDef...] }`；`RaceDefs.json` 与物种表的关系写清楚（种族=可玩外观/文化载体，物种=可育与身体蓝图）。
- 纯定义（`GenomeDef`/`GenomeLocusDef`/`AlleleDef`/`SpeciesDef` 等）迁入 `Content.Definitions`，由 `ContentDefinitionIndex` 增加 `Genomes`/`Species` 两个 provider：Base 打底、Mods 覆盖；Simulation 只保留引擎与目录桥接。校验：
  - genomeId/speciesId 唯一；
  - locusId 在基因组内唯一，等位基因索引有效；
  - species.genomeId 必须存在；
  - 非法条目跳过并告警，不拖垮内容加载。
- 测试：默认表加载、Mod 覆盖、坏引用被拒、人类基因组与代码兜底等价。

### D2 身体构造与代谢位点
新增位点（离散为主，加性为辅）：
- `body_plan`：`wingType`（none / membrane / feathered）、`hornType`（none / keratin / antler）、`tailType`（none / short / long / prehensile）、`posture`（biped / quadruped / serpentine）。
- `integument`：`skinType`（skin / scale / feather / chitin）、`insulation`（0–1，加性）、`naturalArmor`（0–1，加性）。
- `metabolism`：`dietMix`（herbivore↔carnivore，加性）、`tempToleranceC`（加性，±°C）、`size`（加性，0.3–3.0）。
- 每个位点 2–4 个等位基因，显性关系明确；离散通道写入 `PhenotypeResult` 的新结构：
  - `BodyPlanPhenotype { wingType, hornType, tailType, posture }`
  - `IntegumentPhenotype { skinType, insulation, naturalArmor }`
  - `MetabolicPhenotype { dietHerbivore, dietCarnivore, tempToleranceC, size }`
- 测试：同基因型同表型；显性/隐性组合；突变能改变离散性状；旧实例缺位点回退默认。

### D3 表型 → 机制接线（至少 3 条）
1. **角/爪/体型 → 个人战力**：`SpecialtyDerivation` 或角色创建时，`prowess` 由 `naturalArmor`/`hornType`/`size` 推导（生理因果，不是通用 buff）。
2. **鳞皮/隔热 → 温度与疾病**：`CharacterDiseaseSystem` 的寒/热相关判定读取 `insulation`/`tempToleranceC`；极端气候下死亡率/患病率变化。
3. **代谢 → 口粮结构**：物种/人口的 `dietHerbivore/dietCarnivore` 决定可消费货物（作物 vs 肉/鱼），与 P3.2 食物篮子共用一条链；纯肉食种群在缺肉时才会饥荒。
4. （可选）**翼 → 侦察**：由具翼角色指挥的军队 `scoutRange` 提升，接入 `DetectionContactService`；不新建视野系统。

每条接线都要有“反向断言”：改掉位点/表型后机制结果随之改变。

### D4 物种层
- `SpeciesCatalog`：内容表加载、按 `speciesId` 查询、`ResolveForGenome(genomeId)`。
- `SpeciesDef` 增 `bodyPlanBaseline`（由基因组推导的默认蓝图）与 `diet`/`habitat` 的显式字段；物种创建时从基因组固定等位基因生成“模式标本”。
- 生殖隔离：沿用 `ReproductiveIsolation`；新增“物种×物种”兼容矩阵测试（同物种可育、近缘可杂交、远缘不可育）。
- 与 P3.2 的接口：`EcosystemSystem` 暂只保留聚合野生动物；物种层先提供目录/表型/兼容，不硬塞逐格物种丰度（留给后续）。

### D5 幻想物种内容（验收样例）
- `Genomes.json` 增加 `genome_wyvern`：膜翼、角、鳞皮、长尾、四足/双足、肉食、体型 1.8；全部由表驱动。
- `Species.json` 增加 `species_wyvern`（category 自定，如 `Sapient` 或 `Animal`），`genomeId=genome_wyvern`。
- 测试：加载后 `ComputePhenotype` 得到 `wingType=membrane / hornType=keratin / skinType=scale / dietCarnivore≈1`；两个瓦伊凡个体繁殖仍为瓦伊凡；与人类杂交按隔离规则失败（或按配置产生不可育后代）。
- 内容里再放一个“无翼但角/鳞”的近缘物种，验证不是按名字特判。

### D6 存档迁移
- `GenomeInstance` 增 `genomeVersion`（int）。读档时若实例数组长度 < 基因组位点数，按“该位点 `defaultFrequency` 最高的等位基因”补齐；写回保持新长度。
- 旧档（无 genomeVersion）走同一补齐路径；`LegacyDnaAdapter` 仍只负责旧 `DnaData` 输入。
- 测试：旧 6 位点实例读入新 10 位点基因组 → 表型默认值、可继续繁殖、存档往返稳定。

### D7 文档与验证
- `DECISIONS.md` 新增 D126+：内容化、位点规范、机制接线、物种层、迁移策略。
- `PROJECT.md` 记录每阶段验证数字。
- 最终：`_fc_verify/build.cmd` `EXIT=0`、零 `error CS`、EditMode 全绿；`longrun` 2 seed×100 年通过。

---

## 3. 非目标（本轮不做）

- 逐格物种丰度/食物网（P3.2 的物种层后续；本轮只到目录/表型/兼容）。
- 真实遗传学全模型（连锁图谱、表观遗传、群体遗传漂变）——只保留能驱动机制的部分。
- 用 DNA 直接生成 3D 模型/贴图；只产出结构化表型，渲染层后续消费。
- 城市俯视 Sprite、Exploration 远征实体（P3/P4 尾巴，另行安排）。

## 4. 验收清单

1. 基因组/物种可从 `Base/Genetics/*.json` 加载，Mod 可覆盖，坏引用被拒。
2. 新位点（body_plan/integument/metabolism）可遗传、可突变、可推导表型。
3. 至少 3 条机制接线有正/反向测试。
4. 瓦伊凡样例完全由 JSON 生成，代码无物种特判。
5. 旧档 `GenomeInstance` 迁移后表型稳定、可繁殖、存档往返。
6. `build` 全绿、`longrun` 2×100 年通过、`DECISIONS/PROJECT` 同步。
