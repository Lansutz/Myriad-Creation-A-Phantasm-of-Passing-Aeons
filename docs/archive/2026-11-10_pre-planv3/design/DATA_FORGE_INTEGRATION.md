# Myriad Data Forge 接入说明（游戏侧）

> 对象：独立数据管线仓库 `D:\Myriad-Data-Forge`（Glottolog/PHOIBLE/WALS/Grambank/UD/Concepticon/CLICS/D-PLACE/Seshat/Neotoma 等）。
> 本文只描述**游戏仓库如何消费其产物**；数据获取/许可/裁剪在 Forge 仓库完成，游戏构建期不联网。

## 1. 当前接入状态（代码事实）

| 项 | 状态 |
|---|---|
| `Assets/StreamingAssets/Base/_generated/language-families.json` | ✅ 已随包（Glottolog 5.3，CC BY 4.0，裁剪 75 节点） |
| `LanguageFamilyCatalog` + `LanguageFamilyNode` | ✅ 已加载、校验 parent/环；测试 `LanguageFamilyTests` |
| `_generated/README.md` 署名 | ✅ 已有 |
| `_generated/datasets.lock.json`（游戏内副本） | ✅ 本轮新增（Glottolog 条目） |
| `_generated/LICENSES.md` | ✅ 本轮新增（Glottolog/CC BY 4.0 署名 + 约束） |
| `DatasetIntegrityTests`（EditMode） | ✅ 本轮新增：header/schema/source/requires/redistributable/lock 一致性/谱系无环/署名检查 |
| 音系库存/概念表/命名合法性/环境校准产物 | ❌ 尚未进游戏包 |
| `LanguageDef.phonemeInventory` 等字段 | ❌ 尚未扩展 |

## 2. Forge 产物 → 游戏系统映射

| Forge 产物 | 层 | 游戏落点 | 用途 |
|---|---|---|---|
| `out/language-families.json` | T3 CC BY | `LanguageFamilyCatalog` / `LanguageDef.familyId/parentLanguageId` / `CultureData.languageFamilyId` | 语言谱系、命名/文化分化参照（**已接入**） |
| `data/conlang-inventories.json` | 自建（可分发） | `LanguageDef.phonemeInventory`（新增） | 各语言音段库存；用于音节生成与 PHOIBLE 落点校验 |
| `out/core-concept-lists.json` | 需查 lock（Concepticon） | 词库语义组 / 互通度核心词 | 语义字段分组（水/山/人/自由…）、Swadesh 式互通度 |
| `out/clics-colexification.json` | 设计期 | 词义关联参考 | 命名/词根语义演化参考，不直接进包 |
| `out/wold-borrowing.json`、`segbo-coverage` | 设计期 | 借词层参数 | 语言接触/借词率参考 |
| `out/phoible-validation.json` | **design_only（CC BY-SA）** | 只在报告/测试中读 | 校验自建音系是否有类型学落点；**不得进 StreamingAssets** |
| `out/wals-coverage/grambank-coverage/ud-*`、`areal-typology-baseline` | 设计期 | 类型学校验报告 | 检查生成语言/句法参数是否落在自然语言空间 |
| `out/language-scripts.json`、`script-codes.json` | 设计期/可再分发需查 lock | `LanguageDef.scriptId`/转写表 | 文字/转写；命名合法性的一部分 |
| `out/xriver/03-legality.csv`、`02-morpheme-types.csv`、`04-frame-semantics.csv`、`14-prosody.csv`、`22-generated-names.csv` | 自建/设计期 | 命名合法性 schema 的**模板**（不是通用数据） | 词/语素/框架/韵律/生成校验的字段与状态机 |
| `out/biome-climate-map.json`（由 `data/biome-map.csv` 生成） | T2 自建 | `BiomeDef.climateZone/ecoregionClass` | 群系→气候约束（生成/展示） |
| `out/production-calibration.json` | T2 自建 | `GoodsDef`/农业参数 | 作物热量/储存/驯化链 |
| `out/climate-calibration.json`、`data/sea-level.csv`、`data/climate-anomalies.csv` | T2/设计期 | `WorldConfig`/环境默认值 | 温度/降水区间、海平面、异常事件基线 |
| `out/institution-priors.json` | **design_only（NC）** | 设计文档 | 制度参数先验；不得进包 |
| `datasets.lock.json` | 元数据 | 游戏侧副本（已加） | 来源/版本/许可/分发审计 |

## 3. 接入架构（游戏侧）

```text
StreamingAssets/Base/_generated/
  <artifact>.json          # schema_version/generated_at/artifact/requires/source
  datasets.lock.json       # 来源/许可（游戏内副本）
  LICENSES.md              # 署名与约束
        │
        ▼
ContentDefinitionIndex / 专用 Catalog（LanguageFamilyCatalog 等）
  - 未知字段忽略；缺省字段默认；非法 id 跳过并 LogWarning（不得导致世界生成崩溃）
  - 优先 `Base`，再 `Mods` 覆盖；`schema_version` 不兼容时拒绝并回退
        │
        ▼
运行时系统（LanguageDef/CultureData/BiomeDef/GoodsDef/WorldConfig）
```

规则：
1. `_generated` 只允许 `requires: redistributable`；`design_only` 报告留在 Forge `docs/`。
2. 每个新增产物必须在 `datasets.lock.json` 有来源/版本/许可；`DatasetIntegrityTests` 强制。
3. 游戏内部 ID 自建（`languageId` 等），Forge 的 `glottocode`/外部 id 只作锚字段。
4. 许可：Glottolog CC BY（署名）；PHOIBLE CC BY-SA 只做校验；D-PLACE/Seshat NC 只作设计参照；Concepticon/CLICS/WALS/Grambank 按 lock 的 distribution 决定能否进包。
5. 构建期不联网：Forge 产物提交到游戏 `_generated/` 后，游戏只读文件。

## 4. 缺口与下一步

### D1 语言音系（高优先，接语言/命名设计）

- Forge 已有 `conlang-inventories.json`（自建、可分发）→ 加 `LanguageDef.phonemeInventory`；
- 游戏侧再定义音节模板/禁音/重音/命名 pattern（Forge 只提供音段库存，不提供完整音系规则）；
- 用 Forge 的 `validate-phonology`（PHOIBLE 报告，design_only）做校验，报告留在 Forge `docs/`。

### D2 语义/核心词（支撑命名与互通度）

- `core-concept-lists.json`（Concepticon 核心概念）→ 词库语义组、Swadesh 核心词；
- 用于：`NameGenerator` 的语义槽（水/山/人/自由…）、语言差异意识中的词汇重叠/互通度；
- 需按 lock 的许可决定进包或只做设计期映射。

### D3 命名合法性 schema

- Forge 的 XRiver 数据是**某一个 conlang 的分析结果**，不是通用数据；但 `07-606-name-analysis-protocol.md` 的**状态机/字段**可复用：
  - `NameLegalityProfile`（phonology/prosody/morphology/semantics/pragmatics）；
  - `MorphemeType`（FREE_ROOT/BOUND_ROOT/DERIVATIONAL_AFFIX/INFLECTIONAL_AFFIX/CRANBERRY_LIKE）；
  - `SemanticFrame`（FrameNet 元素）；
  - 合法性状态（LEGAL/PARTIAL/ILLEGAL + 原因码）。
- 为游戏各语言生成 `NameProfiles` 内容（自建），Forge 只提供协议/校验脚本。

### D4 环境/物产校准

- `biome-climate-map.json`、`production-calibration.json`、`climate-calibration.json`、`sea-level.csv`、`climate-anomalies.csv`；
- 接入 `BiomeDef/GoodsDef/WorldConfig`，作为环境/农业默认值与可调参数；
- 许可未确认的源（GAEZ 等）只输出自建参数+来源说明，不复制原始值。

### D5 制度先验

- D-PLACE/Seshat 是 NC/NC-SA，只写设计文档/报告，不进包；游戏侧参数自建。

## 5. 现有缺口（本轮发现）

- 游戏侧 `_generated` 只有 `language-families.json`，其余产物未接入；
- `LanguageDef` 缺 `phonemeInventory`、`scriptId`、`namePatterns`、`semanticFrames`；
- `BiomeDef` 缺 `climateZone/ecoregionClass`；`GoodsDef` 缺农业校准字段；
- 命名合法性 schema 尚未落地（Forge 只有 XRiver 单语言分析）；
- `DatasetIntegrityTests` 本轮已补，但 Forge 侧新增产物仍需同步 lock/LICENSES。

## 6. 与其它文档的关系

- 语言/命名/族群：[LANGUAGE_NAMING_IDENTITY.md](LANGUAGE_NAMING_IDENTITY.md)
- 认知/头衔/货币：[CIV_COGNITION_TITLE_CURRENCY.md](CIV_COGNITION_TITLE_CURRENCY.md)
- 环境/板块/气候：[GEOLOGICAL_CLIMATE_DYNAMICS_PLAN.md](GEOLOGICAL_CLIMATE_DYNAMICS_PLAN.md)
- 数据管线细节：[Myriad-Data-Forge 的 04-adapter-contract.md](file:///D:/Myriad-Data-Forge/docs/04-adapter-contract.md)
