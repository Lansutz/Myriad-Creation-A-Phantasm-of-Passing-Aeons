# Implementation Plan: 世界模拟数据集接入（分层、许可闸门与内容管线）

**Date**: 2026-10-27 | **Resource index**: [世界模拟资源索引（核查版）](../../research/2026-10-27-world-simulation-resource-index.md)

## Summary

把外部数据库分成四层：**不接入（在线查询）**、**设计期参照（人工查、不进包）**、**构建期生成（离线脚本产出小参数表进 `StreamingAssets`）**、**运行时内容（裁剪后随游戏分发）**。先过许可闸门，再做字段映射，最后用 EditMode 测试与 `DATASETS.lock.json` 锁死"这份数据从哪来、什么许可、什么版本"。本计划与法律模块计划互不阻塞，但两者共用同一条内容管线（`StreamingAssets/Base` → `ContentRuntime` → `ContentRegistry`/`ContentResolvers`）。

## Technical Context

- **引擎/版本**：Unity 6000.6.3f1，C# 9；内容以 JSON 放在 `MyriadCreation/Assets/StreamingAssets/Base/<Module>/`，由 `Simulation/Modding/ContentRuntime` 加载、`ContentRegistry` 注册、`ContentResolvers` 查询；模组走 `ContentSourceCatalog` 覆盖。
- **现有内容模块**：`Biome / Culture / EthnicGroup / Ethos / FamilyTradition / Innovation / Language / MentalHealth / Race / Religion / Title / Tradition / CharacterTemplate / Dna`（15 个 Base JSON）。
- **现有关键类型**：`LanguageDef`（名字池/地名后缀，**无音系**）、`CultureData`（七板块字段已 `[Obsolete]`；新增 `ethnicGroupId/ethosId/traditionIds`）、`EthnicGroupDef`（单 `cultureId`、traditions、innovations）、`TraditionDef`（已有 `category/minRole/factors/solidification` 字段但 JSON 未迁移）、`InnovationDef`、`BiomeDef`、`ReligionDef`。
- **验证**：EditMode Test Runner（现有 341 条，`Logs/editmode-results.xml`）；`-executeMethod MyriadCreation.Editor.BatchCompileCheck.Run` 可做无界面编译门禁。
- **约束**：构建期不联网；原始大数据不入仓库；NC/ND/订阅类数据不得进入 `StreamingAssets`；所有分发数据必须可署名。

## 分层策略

| 层 | 定义 | 本计划中的对象 | 是否进包 |
|---|---|---|---|
| **T0 在线查询** | 运行时依赖外部 API | 全部数据库（本项目不采用） | 否 |
| **T1 设计期参照** | 人工查阅、写进设计文档或校准报告，不复制数据行 | D-PLACE、Seshat、eHRAF、SAILS、Global Jukebox、AADR、Open Context、tDAR、ArchaeoGLOBE、BEAST2/IE-CoR | 否 |
| **T2 构建期生成** | 离线脚本读取外部数据 → 产出**自建的小参数表** | Köppen/RESOLVE 映射、Crop Origins/GAEZ 校准、Neotoma/PMIP 气候校准、PHOIBLE 校验报告 | 是（自建表，非原始数据） |
| **T3 运行时内容** | 裁剪后可直接加载的内容定义 | Glottolog 谱系树（裁剪）、语言身份字段 | 是（CC BY，署名） |

## 许可闸门（先过这一关，再谈字段）

| 许可类型 | 数据 | 允许动作 | 禁止动作 | 工程处置 |
|---|---|---|---|---|
| CC BY 4.0 | Glottolog、Grambank、WALS、Lexibank、ASJP、CLICS⁴、CrossGram | 读取、改写、商用、随包分发（署名） | 去掉署名 | 可进 T3/T2；`LICENSES/datasets.md` 列署名 |
| CC BY-SA 3.0 | PHOIBLE | 同 CC BY，但**衍生数据须同许可** | 用更严格条款分发衍生 | 只做 T1 校验；若分发衍生库存，单独隔离并声明 SA |
| CC BY-NC 4.0 | D-PLACE、Ethnographic Atlas | 非商用研究/参照 | **随游戏分发** | 只做 T1；数值只能以"我们自己的参数"形式重写，不逐行复制 |
| CC BY-NC-SA | Seshat | 非商用 + 同许可 | 随包分发、闭源衍生 | 只做 T1；阈值校准写入设计文档而非数据文件 |
| CC BY-NC-ND 2.0 | SAILS | 非商用、原样 | 商用、演绎 | 只做 T1 阅读，不整理入库 |
| 订阅/受限 | eHRAF、Smithsonian 类 | 依订阅条款 | 再分发 | 只做 T1 人工查阅，不摘录成库 |
| 不明/待核 | AUTOTYP、MAGRAM、RESOLVE、GAEZ、AADR、tDAR、口头传统库、Global Jukebox | 先只读 | 分发 | 补核许可后才可能进 T2/T3 |

硬规则：**许可不明的数据，默认按"不可分发"处理**；任何进包文件必须在 `DATASETS.lock.json` 有记录，否则测试失败。

## 字段映射（目标注入点）

| 数据 | 生成文件（T2/T3） | 目标类型/字段 | 用途 |
|---|---|---|---|
| Glottolog 谱系 | `Base/_generated/language-families.json` | `LanguageDef` 新增 `glottocode`、`familyId`、`parentLanguageId`；`CultureData` 新增 `languageFamilyId` | 语言树、命名/文化分化的谱系参照 |
| Glottolog 身份 | 同上（裁剪：仅游戏用到的语言节点 + 其祖先链） | `LanguageDef.languageId` 保持自建字符串键，`glottocode` 作外部锚 | 稳定 ID、模组可替换 |
| PHOIBLE 校验 | `docs/research/reports/phoible-validation.md`（不进包） | `LanguageDef` 新增 `phonemeInventory: List<string>`（自建生成结果） | 生成音系的类型学落点校验；无 attested 对应时标 `TYPOLOGICALLY_UNATTESTED` |
| CLICS | 设计文档 | 命名/词根语义关联的人工参照 | 语义演化参考，不入库 |
| Lexibank/ASJP | 设计文档 | 词表样本、同源关系参照 | 自建词表的质量对照 |
| Grambank/WALS/AUTOTYP | 校验报告 | `tools/` 输出的类型学签名报告 | 参数组合是否落在自然语言空间 |
| RESOLVE/Köppen | `Base/_generated/biome-climate-map.json` | `BiomeDef` 新增 `climateZone`/`ecoregionClass`（自建映射） | 群系→气候/生态约束 |
| Crop Origins/GAEZ/DAD-IS | `Base/_generated/production-calibration.json` | `GoodsDef`/农业参数表（自建数值） | 可驯化—可生产—可储存链条 |
| Neotoma/PMIP | `docs/research/reports/climate-calibration.md` + `WorldConfig` 数值 | 温度/降水区间、季节振幅 | 古气候基线校正 |
| D-PLACE/Seshat | `docs/research/reports/institution-priors.md` | 设计文档（制度参数先验与阈值） | 制度空间参照；NC 不入包 |

## 导入管线

```text
tools/datasets/
  datasets.lock.json          # 每个外部源：url、version、sha256、license、fetched_date、裁剪规则
  fetch_glottolog.py          # 下载 Glottolog CLDF → 裁剪祖先链 → Base/_generated/language-families.json
  validate_phonology.py       # 读 PHOIBLE → 校验 Base 语言的音系落点 → docs/research/reports/phoible-validation.md
  gen_biome_map.py            # 读 RESOLVE/Köppen 属性表 → Base/_generated/biome-climate-map.json
  calibrate_climate.py        # PMIP/Neotoma 数值 → docs/research/reports/climate-calibration.md（人工确认后写回 WorldConfig）
  calibrate_production.py     # Crop Origins/GAEZ/DAD-IS → Base/_generated/production-calibration.json
  LICENSES/datasets.md        # 署名与许可声明（随包分发）
```

原则：脚本只读外部数据、只写**自建结构**；不把原始 CSV/栅格复制进仓库；生成文件必须可重复（固定版本 + 校验和 + 固定随机种子）。

## 体积预算

| 文件 | 预算 | 说明 |
|---|---|---|
| `language-families.json` | < 150 KB | 只含游戏实际使用的语言节点及祖先链，不含 8,238 全量 |
| `biome-climate-map.json` | < 50 KB | 群系→气候/生态区映射，不存栅格 |
| `production-calibration.json` | < 30 KB | 作物/家畜参数子集 |
| 许可与锁文件 | < 20 KB | 纯文本 |
| **合计** | **< 250 KB** | 原始数据一律不入库 |

## 分阶段任务

### Phase 0：许可与占位（阻塞后续）
- [ ] D-T1 建 `tools/datasets/datasets.lock.json` 与 `LICENSES/datasets.md` 空骨架；定义字段（url/version/sha256/license/fetched/date/裁剪规则）。
- [ ] D-T2 对 12 个候选源做许可结论表（可分发/仅设计期/不明），写入索引文档第 3 节并锁定；不明者不许进 Phase 2+。
- [ ] D-T3 在 `Assets/Tests/EditMode` 加 `DatasetIntegrityTests`：每个 `Base/_generated/*` 必须在 lock 中有条目且有许可声明。

### Phase 1：语言身份与谱系（T3，CC BY）
- [ ] D-T4 `fetch_glottolog.py`：下载固定版本（先 Glottolog 5.3），输出谱系裁剪表；生成规则写入 lock。
- [x] D-T5 `LanguageDef` 增 `glottocode`/`familyId`/`parentLanguageId`；`CultureData` 增 `languageFamilyId`；`ContentRuntime` 加载新文件；旧 JSON 缺字段时给默认值（向后兼容）。
- [x] D-T6 生成侧接入：文化/语言的父系关系优先读谱系表，读不到回退现有 `parentCultureId`。
- [x] D-T7 EditMode：谱系引用完整性（parent 存在/无环）、未知 glottocode 拒绝、生成文件体积断言。

### Phase 2：音系与类型学校验（T1/T2，PHOIBLE/Grambank/WALS）
- [ ] D-T8 `validate_phonology.py`：对 Base 语言的全部音系库存，检查每个音段/库存是否在 PHOIBLE 有 attested 对应；输出报告与 `TYPOLOGICALLY_UNATTESTED` 清单。
- [ ] D-T9 `LanguageDef` 增 `phonemeInventory`（自建生成结果，非 PHOIBLE 行复制）；生成器输出后跑 D-T8 校验。
- [ ] D-T10 类型学签名报告：对生成语言的特征组合与 Grambank/WALS 分布做偏离度报告（只进 `docs/research/reports`）。

### Phase 3：环境与物产校准（T2，许可待核先做设计期）
- [ ] D-T11 `gen_biome_map.py`：RESOLVE/Köppen → 自建群系映射；许可结论为"可分发"才进 Base，否则只留报告。
- [ ] D-T12 `calibrate_production.py`：作物/家畜参数表；GAEZ 许可未定前只输出"自建参数 + 来源说明"，不复制 GAEZ 数值格。
- [ ] D-T13 `calibrate_climate.py`：PMIP/Neotoma → 气候区间报告；人工确认后写回 `WorldConfig` 默认值，报告留档。

### Phase 4：制度先验（T1，NC 数据只出文档）
- [x] D-T14 `docs/research/reports/institution-priors.md`：D-PLACE/Seshat 的制度变异范围与复杂度阈值，注明"参考而非复制"，并列出不能入包的数值。

### Phase 5：验证与收口
- [ ] D-T15 全量验证：编译门禁 + EditMode 内容测试 + 体积门禁 + 许可清单检查。
- [ ] D-T16 更新资源索引与 `LICENSES/datasets.md`，记录版本、访问日期、署名文本。

## 验证策略

- **内容加载**：`ContentRegistry.TryGet*` 能取到新增生成文件的每一条；缺失字段给默认值不抛异常。
- **引用完整性**：`parentLanguageId`/`familyId` 都指向存在的节点，无环；未知 ID 在加载时报错并列出。
- **许可**：`DatasetIntegrityTests` 遍历 `Base/_generated` 与 `LICENSES/datasets.md`，任何文件缺 lock/许可记录即失败。
- **体积**：生成的 4 个文件总量 < 250 KB（测试断言）。
- **类型学**：校验报告必须区分"attested / unattested / no-data"，禁止把 no-data 当 unattested。
- **构建可重复**：同一 lock 版本重复运行脚本，输出字节级一致（固定排序 + 固定 JSON 序列化）。

## 风险与决策点

| 风险 | 处置 |
|---|---|
| NC/ND 数据被误打包 | 许可闸门 + `DatasetIntegrityTests`；NC 数据只进 docs |
| SA 传染（PHOIBLE） | 只做校验报告；若分发衍生，隔离文件并声明 SA |
| 版本漂移（库每年更新） | lock 文件锁版本 + sha256；升级需单独任务 |
| 原始数据过大 | 只提交生成表；原始文件放 `.gitignore` 的外部缓存目录 |
| 口径混淆（语言数/变体数/生态区 846 vs 867） | 字段名显式编码口径，如 `variantCount`/`languageCount`；文档记录 |
| 现代基线用于古代 | 环境参数一律经 PMIP/Neotoma 校准或显式标注"现代基线"，不静默使用 |
| 数据库 ID 与游戏 ID 冲突 | 游戏内部 ID 自建，外部 ID 只作 `glottocode`/`societyId` 锚字段 |

**待拍板**：
1. 是否随包分发 Glottolog 裁剪谱系（建议：是，CC BY，署名；体积 <150 KB）。
2. PHOIBLE 是否只做校验（建议：是，避免 SA 扩散）。
3. RESOLVE/Köppen 许可确认后是否进 Base（当前按"待核"处理）。
4. 是否把制度先验的数值写进代码默认值（建议：只写设计文档与可调参数，不硬编码阈值）。

## 与其它计划的衔接

- 本计划不改法律/头衔/文化运行时逻辑，只扩展内容字段与生成管线；与 `2026-10-25-law-module-completion-plan.md` 可并行。
- `LanguageDef`/`CultureData` 的字段扩展会与「文化/族群/传统重构」设计稿（`ARCHITECTURE_2_0_LAW_OFFICE_NAME_REGION_MODEL.md` 5A 节）交叉，需在实现前对齐字段命名，避免两套 ID。

