# Faith / Culture 内容管线对齐（定义是定义、脚本是脚本）

- 所属批次：Arch 13 / task-67（只做 Faith/Culture 内部细分 + 内容管线盘点与 schema 方案；**不硬做**加载器改造）
- 相关目录：`Assets/Scripts/Simulation/MyriadCreation.Simulation.Faith/`（`Definitions/ State/ Runtime/`）、`Assets/Scripts/Simulation/MyriadCreation.Simulation.Culture/`（`State/ Runtime/`）
- 相关读者：内容管线（`Simulation.Modding.ContentRuntime/ContentRegistry`）、Faith/Culture 系统、存档、UI
- 约定参照：`SYSTEM_OWNERSHIP.md`（§7.2/§7.3）；"大系统 namespace + 子文件夹表示角色"

## 1. 结论摘要

1. **Faith/Culture 的主体内容已经 JSON 化**：`Religions.json`、`Doctrines.json`、`Ethos.json`、`Traditions.json`、每文化 `CultureData.json` + 姓名 CSV 均由 `ContentRuntime` 加载，**C# 里没有硬编码的宗教/教义条目**（grep 固定 id/中文名 0 命中）。
2. **仍需迁移的"C# 硬编码内容"只剩 3 处非代码契约数据**：
   - 文化阶段演进阈值（`CultureStageEvolutionSystem.cs:16-22`，4 个人口阈值 + switch 内条件）；
   - 宗教调色板（`ReligionCatalog.cs:284-304` 的 `_palette` / HSV 生成规则，属表现数据）；
   - 圣人（`SaintDef` 目前只由 `CanonizationSystem.cs:52` 运行时创建，无 JSON 内容表）。
3. **纯枚举/类型属于"代码契约"，不建议迁 JSON**：`FaithType`、`FaithOrganizationType`、`ReligionMapLevel`、`ReligionNodeType`、`DoctrineCategory`、`GameEnums.CultureStage`（`Core.Enums GameEnums.cs:279`）。JSON 中用 int/字符串映射即可。
4. 目标结构（CK3 思路）：`StreamingAssets/Base/Faith/{Religions,Doctrines,Saints}.json`、`Base/Culture/CultureStages.json`；现有 `Base/Religion/` 路径保留兼容。
5. **本卡选择"只写方案"**：不编译/不跑测试的硬约束下，改加载器（ContentRuntime + 回退 + 测试）风险不可控；本文给出可直接落地的 patch 清单、schema 与测试名，作为下一张实现卡执行。

## 2. 现行管线（数据流）

```
Assets/StreamingAssets/Base/**
        │  文件路径见 ContentRuntime.cs
        ▼
ContentRuntime（JsonUtility.FromJson<Wrapper>，Wrapper 顶层对象含数组）
  LoadCultureRoot   :99   → Base/Culture/<文化>/CultureData.json（:253）+ 姓名 CSV（:263-266）
  LoadEthosRoot     :121  → Base/Ethos/Ethos.json（:123）
  LoadTraditionRoot :139  → Base/Tradition/Traditions.json（:141）
  LoadReligionRoot  :215  → Base/Religion/Religions.json（:217）→ ReligionCatalog.Load(...)（ContentRuntime.cs:478）
  LoadDoctrineRoot  :224  → Base/Religion/Doctrines.json（:226）→ DoctrinePool.Load(...)（ContentRuntime.cs:496）
        │
        ▼
IContentStore → ContentRegistry（Religions/Doctrines/Cultures/Ethos/... 字典，ContentRegistry.cs:37/100）
        │
        ▼
系统：ReligionCatalog / DoctrinePool / CultureData / CultureStageEvolutionSystem / UI / 存档适配器
```

约束：`JsonUtility` **不支持顶层数组**，所有文件必须是 `{ "字段名": [ ... ] }` 的 wrapper 结构（现有文件均遵守）。

## 3. Faith 盘点

| 内容 | 载体 | 位置/证据 | 状态 | 建议 |
|---|---|---|---|---|
| 宗教条目（ReligionDef 列表） | **JSON** | `Base/Religion/Religions.json`（25.7KB，`religions` 数组）；`ContentRuntime.cs:217` → `:478 ReligionCatalog.Load` | ✅ 已数据化 | 迁到 `Base/Faith/Religions.json`（保留旧路径兼容读取） |
| 教义条目（DoctrineOptionDef 列表） | **JSON** | `Base/Religion/Doctrines.json`（7.6KB，`doctrines` 数组）；`ContentRuntime.cs:226` → `:496 DoctrinePool.Load` | ✅ 已数据化 | 迁到 `Base/Faith/Doctrines.json` |
| 宗教注册表/动态创建（CreateTradition/CreateSuccession/CreateReligion） | C# 代码 | `Faith/Definitions/ReligionCatalog.cs:55/87/110` | 代码 API（玩法动态生成分支），非内容表 | 保留 C# |
| 宗教调色板 | **C# 硬编码** | `Faith/Definitions/ReligionCatalog.cs:284-304`（`_palette`、HSV `i*0.618`） | ⚠️ 表现数据硬编码 | 可迁 `Base/Faith/ReligionColors.json`（低风险，见 §7 备选试点） |
| 教义分类/信仰类型/组织类型/地图层级/节点类型枚举 | C# 枚举（代码契约） | `Faith/Definitions/DoctrineCategory.cs:3`、`FaithType.cs:3`、`FaithOrganizationType.cs:3`、`ReligionMapLevel.cs:3`、`ReligionNodeType.cs:3` | ✅ 契约 | 保留 C#；JSON 侧使用 int |
| 圣人（SaintDef） | C# 运行时生成 | `Faith/Definitions/SaintDef.cs`（POCO）、`Faith/Runtime/CanonizationSystem.cs:52 new SaintDef` | ⚠️ 无内容表（圣人由玩法产生） | 若需要"可配置圣人/圣徒传"，新增 `Base/Faith/Saints.json` + 运行时合并（schema 见 §6） |
| 大圣战状态/教义状态 | C# 状态 | `Faith/State/GreatHolyWarState.cs`、`Doctrine.cs`、`DoctrinePool.cs` | 运行期状态 | 保留 C# |

## 4. Culture 盘点

| 内容 | 载体 | 位置/证据 | 状态 | 建议 |
|---|---|---|---|---|
| 文化条目（CultureData：颜色/阶段/生计/葬俗/信仰/语言/官衔…） | **JSON** | `Base/Culture/<文化>/CultureData.json`；`ContentRuntime.cs:99/253` → `ContentRegistry.CultureContentPack`（`ContentRegistry.cs:28`） | ✅ 已数据化 | 保持；补 schema + 校验（cultureId/枚举范围） |
| 文化姓名表 | **CSV** | `Base/Culture/<文化>/*.csv`；`ContentRuntime.cs:263-266` | ✅ 已数据化 | 保持 |
| Ethos / Traditions / FamilyTraditions / EthnicGroups | **JSON** | `Base/Ethos/Ethos.json`、`Base/Tradition/Traditions.json`、`Base/FamilyTradition/FamilyTraditions.json`、`Base/EthnicGroup/EthnicGroups.json` | ✅ 已数据化 | 保持 |
| 文化阶段枚举（Band/Tribe/Chiefdom/EthnicGroup/HighCivilization） | C# 枚举（契约） | `Core/MyriadCreation.Core.Enums/GameEnums.cs:279` | ✅ 契约 | 保留 C#；JSON 用 int/字符串映射 |
| **文化阶段演进阈值/条件** | **C# 硬编码** | `Culture/Runtime/CultureStageEvolutionSystem.cs:16-22`（4 个 `POP_*` 常量）+ switch 内 `population >= 100/1000/5000/10000` 等条件 | ⚠️ 应迁移（数值规则属内容） | **首选试点**：`Base/Culture/CultureStages.json`，代码保留默认回退（见 §7） |
| 文化血缘/相似度/转型/调度算法 | C# 逻辑 | `Culture/Runtime/CultureLineage.cs`、`CultureSimilarity.cs`、`CultureTransition.cs`、`CultureSchedule.cs` | 逻辑（非内容） | 保留 C#（权重后续可参数化） |

## 5. 缺口与风险（按优先级）

- **P1 缺 schema/校验**：全部 JSON 无 schema；`ContentRuntime` 对非法条目多为静默跳过（如 `LoadReligions` 只跳过 null；`LoadCulturePack` 缺 `cultureId` 才 Warn）。建议加轻量校验（id 非空/唯一/枚举范围/引用存在），错误带 `文件:条目` 日志。
- **P1 缺文件静默**：`LoadReligionRoot`（`ContentRuntime.cs:215-221`）、`LoadDoctrineRoot`（:224-231）在文件缺失时**不告警、直接跳过** → 内容缺失会表现为"游戏里没有宗教/教义"而难排查。建议缺失时 `LogWarning` 并给出期望路径。
- **P2 无 C# 回退**：JSON 缺失时注册表为空（无内置兜底内容）。CK3 思路下这是对的（内容即数据），但需保证 Base 内容随包分发；测试/工具环境需显式初始化或允许空。
- **P2 硬编码残留**：§3/§4 标 ⚠️ 的 3 处（阶段阈值、调色板、圣人）是"脚本里藏定义"的最后残留。
- **P2 路径分裂**：Faith 内容在 `Base/Religion/` 而系统目录叫 Faith；建议目标结构统一到 `Base/Faith/`（旧路径过渡期兼容读取）。

## 6. 目标结构 + JSON Schema 示例

### 6.1 目标目录（建议）

```
Assets/StreamingAssets/Base/
  Faith/
    Religions.json        # 由 Base/Religion/Religions.json 平移（wrapper: { "religions": [...] }）
    Doctrines.json        # 由 Base/Religion/Doctrines.json 平移（{ "doctrines": [...] }）
    Saints.json           # 新增（可选；对应 SaintDef）
    ReligionColors.json   # 新增（可选；替代 ReligionCatalog._palette）
  Religion/               # 过渡期：若 Faith/ 文件不存在则回退读这里（兼容旧内容包）
  Culture/
    CultureStages.json    # 新增（阶段阈值/条件，替代 CultureStageEvolutionSystem 常量）
    <Culture>/CultureData.json + *.csv
```

### 6.2 Schema 示例：`Base/Faith/Saints.json`

```jsonc
{
  "saints": [
    {
      "saintId": "saint_laethis_01",   // string，唯一
      "faithId": 100,                   // int，必须存在于 Religions.json
      "characterName": "圣·拉埃西斯",    // string
      "canonizedDay": 1234,             // int（游戏日；-1=未定）
      "patronOf": ["harvest", "travel"],// string[]（自由标签）
      "description": "……"
    }
  ]
}
```

### 6.3 Schema 示例：`Base/Culture/CultureStages.json`（首选试点）

```jsonc
{
  "stages": [
    { "stage": 0, "key": "Band",           "nextPopulation": 100,   "requiredSocialStratification": 0.0, "requiredSpecialization": 0.0, "requiresUrbanCenter": false },
    { "stage": 1, "key": "Tribe",          "nextPopulation": 1000,  "requiredSocialStratification": 0.1, "requiredSpecialization": 0.1, "requiresUrbanCenter": false },
    { "stage": 2, "key": "Chiefdom",       "nextPopulation": 5000,  "requiredSocialStratification": 0.2, "requiredSpecialization": 0.2, "requiresUrbanCenter": true  },
    { "stage": 3, "key": "EthnicGroup",    "nextPopulation": 10000, "requiredSocialStratification": 0.3, "requiredSpecialization": 0.3, "requiresUrbanCenter": true  },
    { "stage": 4, "key": "HighCivilization","nextPopulation": -1,   "requiredSocialStratification": 0.0, "requiredSpecialization": 0.0, "requiresUrbanCenter": false }
  ]
}
```

规则：`stage` 必须 0..4 连续且与 `GameEnums.CultureStage` 一致；`nextPopulation = -1` 表示终态；加载失败/缺文件时使用代码默认常量（保证现有测试绿）。

## 7. 最小试点迁移方案（下一张实现卡执行）

### 7.1 首选：文化阶段阈值 → JSON（低风险、纯数据、测试可判别）

1. 新增 `Assets/StreamingAssets/Base/Culture/CultureStages.json`（schema 见 §6.3）。
2. 新增 DTO（放 `Simulation/MyriadCreation.Simulation.Culture/State/CultureStageDef.cs`）：`int stage; string key; int nextPopulation; float requiredSocialStratification; float requiredSpecialization; bool requiresUrbanCenter;` + wrapper `CultureStagesWrapper { List<CultureStageDef> stages; }`。
3. 新增注册表 `Definitions/CultureStageCatalog.cs`（或放 `ContentRuntime` 旁）：`Load(List<CultureStageDef>)`、`TryGet(stage)`；默认值 = 现有常量（`POP_BAND_TO_TRIBE=100` 等），**加载失败/缺文件时使用默认**，保证行为不变。
4. `CultureStageEvolutionSystem.EvaluateEvolution` 改为读取 catalog（`TryGet(currentStage, out def)`）；阈值/条件全部走 `def`，逻辑分支结构不变。
5. `ContentRuntime` 增加 `LoadCultureStageRoot`（路径 `Culture/CultureStages.json`，注册到 `contentProviderCatalog`，仿 `LoadReligionRoot` 写法）。
6. 新增测试 `Assets/Tests/EditMode/CultureStageEvolutionDataTests.cs`：
   - JSON 加载后 `TryGet(0).nextPopulation==100` 等字段正确；
   - 缺文件/坏 JSON → 回退默认，`EvaluateEvolution` 结果与旧常量一致（回归）；
   - 现有文化测试（`LanguageFamilyTests` 等）保持绿。
7. 风险：`ContentRuntime` 只在 `ContentRegistry.Initialize` 时加载（依赖 `Application.streamingAssetsPath`）；EditMode 测试中若未初始化内容，必须走默认值分支（方案已包含回退，故安全）。

### 7.2 备选：宗教调色板 → JSON（更低风险，但不改变"定义/脚本"结构）

- 把 `ReligionCatalog.EnsureColors()`（`ReligionCatalog.cs:284-304`）的 HSV 规则改为读 `Base/Faith/ReligionColors.json`（`{ "colors": ["#RRGGBB", ...], "fallbackHueStep": 0.618 }`），缺省回退现有算法。适合作为"纯表现数据外置"的快速样板。

### 7.3 暂不做（评估风险大）：Saints → JSON

`SaintDef` 当前由 `CanonizationSystem` 按玩法动态生成（`CanonizationSystem.cs:52`），改 JSON 需要"静态模板 + 运行时实例合并 + 存档 id 稳定"三件事，跨存档/UI 面较大，建议 B 轮内容管线专项再动。

## 8. 测试清单（现有，必须保持绿）

本卡只移动文件（`cs`+`meta`），不改 namespace/using/内容，理论无编译与行为影响；Lead 跑全量时重点看：

- `Assets/Tests/EditMode/FaithPersistenceTests.cs`（4 条，Faith 存档）
- `Assets/Tests/EditMode/RestoredSystemTests.cs`（5 条，含 FaithSystem 规则）
- `Assets/Tests/EditMode/LanguageFamilyTests.cs`（7 条，文化/语言内容）
- `Assets/Tests/EditMode/RealmOverviewTextTests.cs`（1 条，文化/信仰文本展示）
- `Assets/Tests/EditMode/SuccessionEndToEndTests.cs`（11 条，间接触达 FaithSystem/GreatHolyWar 数据）

若试点落地，新增：`CultureStageEvolutionDataTests`（JSON 加载 + 回退 + EvaluateEvolution 回归）。

## 9. 本卡实际执行内容（A 部分）

- `Simulation.Faith/` → `Definitions/`(9)、`State/`(3)、`Runtime/`(5)；`Simulation.Culture/` → `State/`(1)、`Runtime/`(5)。
- **namespace 不变**（仍为 `MyriadCreation.Simulation.Faith` / `.Culture`），**不改任何 using**；仅移动 `.cs` + `.meta`（内容字节逐一 SHA-256 校验一致）。
- 两个大目录根新增 `README.md` 说明"大系统 namespace + 子文件夹表示角色"约定。
- 遗留：新子目录的 folder `.meta` 由 Unity 首次导入生成（不手造 GUID）；Culture 的 `CultureContentPack` 定义包装仍在 `Modding`（跨卡，仅报告）。
