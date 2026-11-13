# 项目信仰/宗教系统现状勘察（供 CK3 借鉴决策用）

> 2026-11-06。只读勘察，未改动任何 project 代码。
> 目标：区分「已实现 / 仅设计稿 / 完全没有」，并给出设计稿之间的互斥结论。
> 一句话结论：**代码里有一个自洽的"宗教内容表 + 信仰读模型 + UI 文本"骨架（约 1900 行），但没有任何玩法回路真正驱动它；5 份设计稿是同一场辩论的连续修正稿，彼此结论互相推翻，且没有任何一行落地。**

## 0. 路径缩写（下文证据一律用缩写:行号）

| 缩写 | 绝对路径 |
|---|---|
| `CD/` | `MyriadCreation/Assets/Scripts/Content/MyriadCreation.Content.Definitions/` |
| `CM/` | `MyriadCreation/Assets/Scripts/Content/MyriadCreation.Content.Modding/` |
| `SF/` | `MyriadCreation/Assets/Scripts/Simulation/MyriadCreation.Simulation.Faith/` |
| `FH/` | `MyriadCreation/Assets/Scripts/Simulation/MyriadCreation.Simulation.FaithHost/` |
| `WS/` | `MyriadCreation/Assets/Scripts/Simulation/MyriadCreation.Simulation.WorldState/` |
| `SY/` | `MyriadCreation/Assets/Scripts/Simulation/MyriadCreation.Simulation.Systems/` |
| `LW/` | `MyriadCreation/Assets/Scripts/Simulation/MyriadCreation.Simulation.Laws/` |
| `CC/` | `MyriadCreation/Assets/Scripts/Core/MyriadCreation.Core.Contracts/` |
| `SA/` | `MyriadCreation/Assets/Scripts/Infrastructure/MyriadCreation.Infrastructure.Save/` |
| `RD/` | `MyriadCreation/Assets/Scripts/Rendering/MyriadCreation.Rendering/` |
| `UIP/` | `MyriadCreation/Assets/UI/MyriadCreation.UI.Presentation/` |
| `UIC/` | `MyriadCreation/Assets/UI/Common/` |
| `AS/` | `MyriadCreation/Assets/StreamingAssets/Base/` |
| `TE/` | `MyriadCreation/Assets/Tests/EditMode/` |
| `D/` | `docs/design/` |

---

## 1. 代码现状

### 1.1 规模与归属（信仰相关代码总量）

| 层 | 程序集 / 目录 | 文件数 | 行数 | 角色 |
|---|---|---|---|---|
| 内容定义 | `MyriadCreation.Content.Definitions/Faith/` | 13 | 575 | 纯 POCO/枚举 + 2 个静态注册表 |
| 运行时（纯 C#） | `MyriadCreation.Faith`（独立 asmdef） | **2** | **189** | `FaithSystem` + `Doctrine` |
| 宿主粘合 | `MyriadCreation.Simulation.FaithHost/` | 7 | 755 | 契约适配、封圣、大圣战、调度、事件桥 |
| 契约 | `CC/FaithContracts.cs` | 1 | 133 | `IFaithQuery/IFaithCommand/IFaithContext` + 3 个 DTO |
| 存档 | `SA/FaithSaveData.cs`、`SA/FaithPersistence.cs` | 2 | 260 | 运行态入档/还原 |
| 地图渲染 | `RD/` | 3 | — | 3 个宗教图层 + 配色 |
| UI | `UIP/ReligionPanelText.cs`、`UIC/UIManager*.cs` | 3 | — | 单文本面板 |

`MyriadCreation.Faith.csproj` 只编译 2 个文件（`Simulation/MyriadCreation.Simulation.Faith/Runtime/FaithSystem.cs`、`State/Doctrine.cs`），asmdef 只 refs `MyriadCreation.Core` + `MyriadCreation.Content`，并由 `TE/AsmdefArchitectureGateTests.cs:250` 强制断言"Faith 不得引用上层"。**架构分层是真的、有测试的；领域模型不是。**

### 1.2 类型清单（关键字段/方法 + 证据）

| 类型 | 位置 | 形态 | 关键字段 / 方法 | 备注 |
|---|---|---|---|---|
| `ReligionDef` | `CD/Faith/ReligionDef.cs:8` | class，JSON DTO | `religionId:10` `religionName:12` `parentReligionId:14` `schoolParentId:16` `nodeType:18` `hasSuccession:20` `worldview:22` `founder:24` `orthodoxy:26` `virtues:28` `sins:30` `headName:32` `riteHeadName:34` `communionName:36` `isMainstreamTradition:38` `liturgicalLanguage:40` `scripturalLanguage:42` **`selectedDoctrines:44`（List\<string\>）** `requiredInnovations:46` `rites:48` `riteFamily:50` `school:52` `color:54` `IsRoot:56` `PrimaryRite:58` | 同时承担 宗教/继承/传统/学派 4 种角色；**没有 FaithType / 组织类型字段** |
| `ReligionNodeType` | `CD/Faith/ReligionNodeType.cs:4` | enum 5 | `Religion=0, Succession=1, Tradition=2, School=3, ReligiousSchool=4` | 硬分层，代码里真实存在 |
| `ReligionMapLevel` | `CD/Faith/ReligionMapLevel.cs:4` | enum 3 | `Religion, Succession, Tradition` | **与 NodeType 不一致**（缺 School/ReligiousSchool） |
| `FaithType` | `CD/Faith/FaithType.cs:4` | enum 7 | `Animistic/Polytheistic/Henotheistic/Monotheistic/Pantheistic/Atheistic/Cosmic` | 只被 `FaithSystem.type` 与 `ThoughtManager.CreateFaith` 使用，**JSON 无法写入** |
| `FaithOrganizationType` | `CD/Faith/FaithOrganizationType.cs:4` | enum 6 | `Decentralized/Congregational/Episcopal/Papal/Theocratic/StateChurch` | 同上，构造时永远是默认 0（Decentralized） |
| `DoctrineCategory` | `CD/Faith/DoctrineCategory.cs:4` | enum 7 | `Cosmology/Soteriology/Ethics/Ritual/Ecclesiology/Eschatology/Political` | **与实际数据用的 pillar 字符串词表不同**（见 1.4） |
| `DoctrineOptionDef` | `CD/Faith/DoctrineOptionDef.cs:7` | class，JSON DTO | `optionId:9` `pillar:11`（string）`optionName:13` `description:15` `exclusiveReligionIds:17` `enhancedFrom:19` | 无 effects / requirements / exclusions |
| `DoctrinePool` | `CD/Faith/DoctrinePool.cs:5` | static 注册表 | `Load:11` `Get:33` `GetOptions(pillar,religionId):42` | `GetOptions` **0 调用者** |
| `ReligionCatalog` | `CD/Faith/ReligionCatalog.cs:7` | static 注册表 | `All:15` `Load:17` `Get:33` `GetRoot:42` `CreateTradition:53` `CreateRite:70` `CreateSuccession:85` `CreateReligion:108` `GetDivergence:126` `GetPillarWeight:159` `IsAvailable:174` `ResolveTraditionId:199` `GetColor:248` `EnsureColors:282` | **Create\*/GetDivergence/IsAvailable/ResolveTraditionId 全部 0 调用者** |
| `SaintDef` | `CD/Faith/SaintDef.cs:6` | class | `saintId:8` `linkedCharacterId:10` `saintName:12` `domain:14` `faithId:16` `canonizationPiety:18` | 无 cultus/feast/shrine |
| `Deity` | `CD/Faith/Deity.cs:7` | struct | `name:9` `domain:11` `importance:13` `symbol:15` `festivals:17` | `FaithSystem.deities` **0 外部引用**，从不填充 |
| `EcclesiasticalTitle` | `CD/Faith/EcclesiasticalTitle.cs:3` | class | `titleName:5` `level:7` `temporalAppointment:9` | 未标 `[Serializable]`，存档靠并行数组绕开 |
| `FaithSystem` | `SF/Runtime/FaithSystem.cs:10` | class（可序列化的**内容+状态混合体**） | `faithId:12` `faithName:14` `type:18` `deities:20` `chiefDeity:22` `doctrines:24` `rituals:26` `taboos:28` `orgType:30` `highPriestCharacterId:32` `churchWealth:34` `churchInfluence:36` `holySiteTileIndices:38` `missionaryZeal:40` `tolerance:42` `regionAdherence:45` `followerCharacterIds:47` `faithRelations:50` `virtues:52` `sins:54` `hierarchyLevel:56` `hierarchyTitles:58` `fervor:60` `GreatHolyWarThreshold=60:62`；`SetHierarchyLevel:64` `AddFervor:83` `CanDeclareGreatHolyWar:88` `GetVirtueScore:98/147` `GetSinScore:122/152` `CalculateReligiousAuthority:157` `DailyTick:167` | `DailyTick` **0 调用者**；`deities/rituals/taboos` **0 外部引用** |
| `Doctrine` | `SF/State/Doctrine.cs:7` | struct | `name:9` `description:11` `category:13` `strictness:15` | **与 `DoctrineOptionDef` 是两套教义类型** |
| `IFaithQuery` / `IFaithCommand` / `IFaithContext` | `CC/FaithContracts.cs:73 / 126 / 106` | interface | Query 11 个方法；Command 2 个；Context 7 个 | 契约层干净，UI 只依赖它 |
| `FaithSnapshotDto` / `FaithSaintDto` / `FaithDoctrineDto` | `CC/FaithContracts.cs:6 / 44 / 58` | DTO | `reformPillars:40` `hierarchyName:37` `authority:29` `isStateReligion:32` | 大量字段**只有测试读**，UI 不用 |
| `FaithContracts` | `FH/FaithContracts.cs:15` | class | `GetStateReligionId:40` `GetFervor:55` `TryGetFaithSnapshot:70` `CanDeclareGreatHolyWar:118` `GetSaints:129` `GetDoctrines:160`（先查 `ReligionCatalog` 再回退 `FaithSystem.doctrines`）`GetPatronSaintId:205` `HierarchyName:231` `GetReformPillars:244` `SetStateReligion:280` `AddFervor:298` | 教义读取的**唯一**消费点 |
| `FaithContextAdapter` | `FH/FaithContextAdapter.cs:13` | class | 实现 `IFaithContext`，包装 `GameWorld/CharacterManager/RealmData` | 依赖反转真实完成 |
| `CanonizationSystem` | `FH/CanonizationSystem.cs:10` | **static** class | `_saintsByFaith:14` `IsCanonizationCandidate:32`（死亡 + `spiritualFulfillment>=80:42` + `virtue>=2:46`）`Canonize:49`（需 `highPriestCharacterId>=0:55`）`GetSaints:83` `Reset:92` | 静态跨世界状态 |
| `GreatHolyWarSystem` / `GreatHolyWarState` | `FH/GreatHolyWarSystem.cs:10` / `FH/GreatHolyWarState.cs:8` | **static** + class | `ActiveWars:14` `Declare:16`（`fervor<60` 拒绝:22）`Rally:62`（按 `stateReligionId==faithId` 召集）`Resolve:77`（受益人走 `SuccessionSystem`）`Cleanup:101` | 静态列表 |
| `ReligionSimulationEventHandler` | `FH/ReligionSimulationEventHandler.cs:9` | class | 订阅 `FaithConflictTriggeredEvent`，双方 `AddFervor(25):28` | 唯一的事件驱动信仰动态 |
| `WarfareReligionSchedule` | `FH/WarfareReligionSchedule.cs:9` | static | `ScheduleId="warfare-religion.daily":11`，`Register:13` → 战斗/宗教/圣战/战争结算 4 个 tick:51 | tick 已接线 |
| `FaithSaveData` / `FaithPersistence` | `SA/FaithSaveData.cs:21`、`SA/FaithPersistence.cs:17` | DTO + static | `FromFaith:50` `ApplyTo:123`；Dictionary 拍平为并行数组；读取时按 `ReligionCatalog` 重建骨架再 overlay | SaveVersion 15 起 |
| Laws 侧信仰词汇 | `LW/LawConstraints.cs:49`（`LawDomain.Faith`）`:61`（`LawFamily.FaithGovernance`）`:142 FaithStatus` `:148 FaithLawPriority` `:156 HeterodoxPolicy` `:162 FaithTitle` | enum | `LawComposition.cs:187` `establishedFaith=FaithStatus.Established` | **与 `RealmData.stateReligionId` 是两套"国教"** |
| `MissionarySystem` | `SY/MissionarySystem.cs:12` | static | `CalculateSuccessChance:14`（同根 0.6 / 异根 0.2）`ConvertTile:38`（平移 3% 人口）`TaxPressureConversion:80` | 消费 `PopulationBlock.faithId` |
| `CharacterManager.UpdatePiety` | `SY/CharacterManager.cs:102` | 方法 | virtues×0.01 − sins×0.02 − `isSecretBeliever`0.01 → `spiritualFulfillment` | 信仰唯一的角色级效果 |
| `ThoughtManager`（第二信仰注册表） | `SY/ThoughtManager.cs:16` `_faiths`、`:24` `_nextFaithId=1`、`:43` `CreateFaith`、`:151` `GetDominantFaith` | 实例 | 自带 `Dictionary<int,FaithSystem>`，**与 `GameWorld._faithSystems` 完全无关** | 并行状态 |
| `ReligionCatalog` 自增 id | `CD/Faith/ReligionCatalog.cs:13` `_nextId=1000` | — | 与 JSON 中 100–600 的 religionId 并存 | 三套 id 空间之一 |

### 1.3 运行时装配（真实链路）

| 环节 | 证据 | 实际行为 |
|---|---|---|
| 内容加载 | `CM/ContentDefinitionIndex.cs:158` provider、`:266` `LoadReligionRoot`、`:410 LoadReligions`、`:424 LoadDoctrines` | `JsonUtility` 读 `Religion/Religions.json` + `Doctrines.json` → 写入 Content store → `ReligionCatalog.Load` + `EnsureColors`（`:419`）、`DoctrinePool.Load` |
| 世界初始化 | `WS/GameWorld.Core.cs:953` 调用 → `:956 InitializeFaithSystems` | **只对 `nodeType == Succession` 建 `FaithSystem`（`:962`）**，`fervor=50`、`highPriest=-1`，只搬 `virtues/sins`（`:971-972`） |
| 数量 | `AS/Religion/Religions.json`（44 条） | 7 Religion / **22 Succession** / 8 Tradition / 6 School / 1 ReligiousSchool → **运行时只有 22 个 FaithSystem** |
| 读档 | `WS/GameWorld.Persistence.cs:67` 重建骨架 → `SA/FaithPersistence.cs` overlay | 内容不存档，只存运行态 |
| 每日调度 | `WS/GameWorld.Simulation.cs:801` → `WarfareReligionSchedule` | `UpdateFaithFervor`（`:360`，每 30 天一次，`day%360==0` 才 −10）+ `CheckHolySiteLoss`（`:377`）+ `CheckGreatHolyWarSettlements`（`:400`） |
| 传教 | `WS/GameWorld.Simulation.cs:211 MissionaryTick`（挂 `AISchedule:819`） | `:226` 收集 `stateReligionId>=0` 的政权，**`dictionary.Count==0` 就整体 return** |
| 信仰命令 | `WS/GameWorld.Contracts.cs:33` `FaithCommand` | `SetStateReligion` **只在测试里被调用**（`TE/SystemContractsTests.cs:232`），游戏内无入口 |

### 1.4 数据词表分裂（同一个"教义"概念有两套分类）

| 分类体系 | 定义处 | 取值 | 消费处 |
|---|---|---|---|
| `DoctrineCategory`（enum，7） | `CD/Faith/DoctrineCategory.cs:4` | Cosmology/Soteriology/Ethics/Ritual/Ecclesiology/Eschatology/Political | `FaithSystem.doctrines[].category`；`FaithContracts.GetReformPillars:267` 后备 |
| `pillar`（string，6） | `CD/Faith/DoctrineOptionDef.cs:11`；权重表 `CD/Faith/ReligionCatalog.cs:159-172` | `doctrine/ritual/ethics/institution/myth/experience/material`（权重 0.3/0.2/0.15/0.15/0.1/0.05/0.05） | JSON 实际数据；`GetReformPillars:254` |
| UI 中文名（第三套） | `UIP/ReligionPanelText.cs:55 PillarName` | doctrine=教义/ethics=伦理教法/ritual=仪式/experience=体验/institution=组织/myth=神话/material=物质 | 私有方法，**`PillarName` 自身 0 调用者** |

注意：数据里实际出现 6 个 pillar，**`myth` 权重表里有但数据里 0 条**；`DoctrineCategory` 的 7 类与 pillar 的 6 类只有 ethics/ritual 两个词重合。

---

## 2. 数据现状

### 2.1 载体判定

| 问题 | 结论 | 证据 |
|---|---|---|
| 硬编码 / JSON / SO / 定义表？ | **JSON（`JsonUtility`）为主 + 极少量代码内默认值；零 ScriptableObject** | `CM/ContentDefinitionIndex.cs:410/424`；全仓 `ScriptableObject` 仅 4 文件且与信仰无关 |
| 目录 | `MyriadCreation/Assets/StreamingAssets/Base/Religion/` | 只有 2 个文件 |
| 文件 | `Religions.json`（842 行，25 KB）、`Doctrines.json`（56 行，7.6 KB） | — |
| 是否走 Mod 管线 | 是，经 Content provider 三层（Base/Mods/_generated） | `CM/ContentDefinitionIndex.cs:158`；`CM/DelegateContentProvider.cs:49` 探测 `Religion/Religions.json` 判断根目录有效性 |
| 有没有"圣地/礼仪/圣人"数据文件 | **没有** | 目录内仅上述 2 个 JSON；`SaintDef` 纯运行时生成（`FH/CanonizationSystem.cs:70`）；`holySiteTileIndices` 由 `WS/GameWorld.Simulation.cs:993 CreateHolySite` 运行时加，**但该方法 0 gameplay 调用者** |

### 2.2 数据统计

| 项 | 数值 |
|---|---|
| 宗教条目 | 44（`religionId` 100–600；缺 103/106–119 等断号） |
| 节点分布 | Religion 7（基督教 100 / 伊斯兰教 200 / 佛教 300 / 华夏祭祀体系 400 / 祆教 500 / 摩尼教 505 / 原始崇拜 600）、Succession 22、Tradition 8、School 6、ReligiousSchool 1 |
| 有 `selectedDoctrines` 的条目 | 63 条引用 / 分布在全表 |
| 教义选项 | 46 条，pillar 分布 doctrine 16 / ethics 12 / institution 6 / ritual 5 / experience 4 / material 3 |
| 带 `exclusiveReligionIds` 的教义 | 2 条 |
| 带 `enhancedFrom` 的教义 | 1 条 |

### 2.3 样例（真实内容，直接取自文件）

`AS/Religion/Doctrines.json`（第 3 行起）：

```json
{ "optionId": "doctrine_monotheism", "pillar": "doctrine", "optionName": "一神论", "description": "独一至上神——万物由唯一神创造与主宰。" },
{ "optionId": "doctrine_sacred_kingship", "pillar": "doctrine", "optionName": "君权神授", "description": "君主为神之代理人——统治合法性来自天命。" },
{ "optionId": "doctrine_homosexuality_crime", ... }, { "optionId": "doctrine_homosexuality_shunned", ... }, { "optionId": "doctrine_homosexuality_accepted", ... }
```

`AS/Religion/Religions.json`（罗马公教会，nodeType=1 即 Succession）：

```json
{
  "religionId": 104, "religionName": "罗马公教会", "parentReligionId": 101, "nodeType": 1,
  "rites": ["罗马礼"], "school": "拉丁教父与经院哲学传统", "riteFamily": "罗马传统（拉丁传统）",
  "headName": "教宗", "riteHeadName": "教宗（拉丁礼）", "communionName": "罗马共融",
  "worldview": "一神", "founder": "使徒彼得",
  "selectedDoctrines": ["doctrine_monotheism","doctrine_communion","doctrine_icon_veneration",
    "doctrine_clerical_gender_male","doctrine_clerical_marriage_celibate",
    "doctrine_clerical_appointment_spiritual","doctrine_funeral_burial","doctrine_pilgrimage",
    "doctrine_homosexuality_shunned"],
  "liturgicalLanguage": "拉丁语", "scripturalLanguage": "拉丁语（武加大）/希伯来语·希腊语[原典]"
}
```

**内容质量是这套系统里最实的部分**：真实历史教派（迦克墩派、正统大公教会、聂斯托里、圣多马学派等）、礼仪家族、圣书语言都写了。缺的是"接电"。

### 2.4 信仰挂在谁身上（外键全表）

| 宿主实体 | 字段 | 定义处 | 类型 | 初始化情况 |
|---|---|---|---|---|
| 地块人口块 | `faithId` | `MyriadCreation/Assets/Scripts/Core/MyriadCreation.Core.Data/PopulationBlock.cs:15` | int | **世界生成时未赋值 → 恒 0**（`WS/GameWorld.Generation.cs:105` 构造时不设 faithId） |
| 人口分组 | `faithId` | `.../Core.Data/PopulationGroup.cs:15` | int | 由 block 复制（`SocialDifferentiation.cs:234`） |
| 人口分组 | `religionTraditionId` | `.../Core.Data/PopulationComposition.cs:30` | int，默认 −1 | 未使用 |
| 角色 | `faithId` | `.../Simulation.State/CharacterData.cs:53` | int | 生成时不赋值 → 0 |
| 角色 | `spiritualFulfillment=50` | `CharacterData.cs:85` | float | 有更新路径（`CharacterManager.UpdatePiety:102`） |
| 角色（个人信仰死骨架） | `privateFaithId=-1` / `personalTenets` / `isSecretBeliever` | `CharacterData.cs:87/89/91` | int / List\<string\> / bool | **5 个操作方法 0 调用者**（`CharacterManager.cs:37-82`），`personalTenets` 不入档 |
| 政权 | `stateReligionId = -1` | `.../Simulation.State/RealmData.cs:21` | int | **无生成赋值**；只有 `SetStateReligion`（无游戏内调用者） |
| 政权 | `statePatronSaintId = -1` | `RealmData.cs:28` | int | **无任何赋值点** |
| 文化 | `faithId = -1` | `AS/Culture/Laethis/CultureData.json:34` | int | 数据里显式 −1 |
| 文化（旁支） | `worshipVector` float[] | `.../Culture/State/CultureData.cs:33` | float[] | JSON 7 维 vs 代码默认 3 维（`WS/GameWorld.Core.cs:874`），**只用于文化相似度**（`CultureSimilarity.cs:26`） |
| 聚落快照 | `stateReligionId`（= 地块主导信仰，回退国教） | `WS/SettlementQueryAdapter.cs:106/127` | int | 只读派生 |
| 地图 DTO（角色摘要） | `faithId = -1` | `CC/TileCharacterContracts.cs:125` | int | 只读派生 |

**关键事实：世界生成不产出任何信仰归属。** 全表 `faithId` 恒 0、`stateReligionId` 恒 −1，因此：传教 tick 空转（`WS/GameWorld.Simulation.cs:226-231`）、圣地失陷永不触发（`holySiteTileIndices` 空）、大圣战不可宣（`CanDeclareGreatHolyWar` 需 `highPriest>=0`）。

---

## 3. UI 现状

| 项 | 结论 | 证据 |
|---|---|---|
| 技术栈 | **旧 uGUI（Canvas + TextMeshProUGUI）**，无 UI Toolkit，无 `.uxml/.uss` | 全仓 `find *.uxml` = 0；`UIC/UIManager.cs:192 [SerializeField] Button religionOpenButton` |
| 信仰页面是否存在 | **存在**：`ReligionPanel` + `ReligionText` + `ReligionOpenBtn` + `ReligionCloseBtn`，序列化进 `MyriadCreation/Assets/Scenes/Main.unity` | `grep -o 'religion[a-zA-Z]*' Scenes/Main.unity` 命中 7 项 |
| 打开/关闭 | `UIC/UIManager.Panels.cs:236 OpenReligionPanel` / `:246 CloseReligionPanel`，按钮绑定 `UIManager.cs:433/437` | — |
| 刷新 | `UIManager.Panels.cs:683 RefreshReligionPanel` → `ReligionPanelText.Build(_worldReadModel, ViewRealmId, -1)` | 主保圣人 id 被**硬编码传 −1** |
| 显示内容 | 只有 4 行文本：`=== 教名 ===`、`信仰热忱：N/100`、`圣地：N 处`、（`statePatronSaintId>0` 时）`政权主保圣人：#N` | `UIP/ReligionPanelText.cs:45-51` |
| 未显示但已有 DTO | 教义列表、圣人列表、改革支柱、教阶名、宗教权威、组织类型、isStateReligion | `CC/FaithContracts.cs:6-67` 有字段；UI **0 引用**（`grep GetDoctrines\|GetSaints\|reformPillars UI/` = 空） |
| 地图图层 | 3 个宗教图层：`religion`（宗教）/`religion_succession`（教统）/`religion_tradition`（宗教传统） | `RD/MapLayerRegistry.cs:80-82`；`RD/MapRenderer.Colors.cs:202-209`、`:280 GetReligionColor`；`UIC/UIManager.cs:500-502` 快捷键 8/0,1,2 |
| 地图配色 | 按 `ReligionMapLevel` 取根节点/本节点/派生 HSV | `CD/Faith/ReligionCatalog.cs:248 GetColor` |
| 面板文本类遗留 | `ReligionPanelText` 声明为 `partial` 且注释说"教统细节仍由 Legacy 旧重载负责"，但**全仓只有这一份 partial 文件、Legacy 重载不存在** | `UIP/ReligionPanelText.cs:7/10` |

**结论：UI 是"信仰存根面板"——能显示国教名、热忱、圣地数，仅此而已。** 一个 CK3 风格的信仰页需要的一切（教义/信条/圣地列表/组织/领袖/圣战按钮）都没有承载控件。

---

## 4. 设计稿结论（5 份逐份 + 互相矛盾）

### 4.1 逐份

| 文档 | 行数 | 核心主张 | 给出的结论 | 与代码的距离 |
|---|---|---|---|---|
| `D/FAITH_SYSTEM_ANALYSIS.md` | 560 | 反对"family→religion→denomination→tradition-stream→local-custom"五层硬模型，主张借 CK3「Faith 为可玩单位 + doctrines/tenets 组件 + fervor/hostility/head/holy sites」。**但同一文件内经 3 次自我修正**：§8（`:240`）改为"CK3 是四层、Rite 才最低可玩单位"；§14（`:365`）再改为"Rite 太基督教中心、改 `FaithCommunity`"；§20-23（`:479-560`）实读 CK3 文件后**又回到**"四层真实存在、Rite 确实普适（theravada/mahayana/vajrayana 都是 Rite）、Faith 是共享教义分组" | §6（`:213`）改造清单 10 条：`ReligionDef` 拆 `FaithDef`+`FamilyDef`、`ReligionNodeType` 改为 family/religion/faith、`FaithType` 降为标签、`DoctrineOptionDef.pillar`→`groupId` 加 effects/requirements、`FaithSystem` 变成 `FaithDef` 的运行时、教职接 `TitleHolding`。§7（`:235`）"先改 `ReligionDef`/`ReligionNodeType`/`FaithSystem`，再谈内容" | **0 行落地**。`ReligionDef` 仍是单类多角色，`ReligionNodeType` 仍是 5 值，`FaithSystem` 仍是内容+状态混合，`DoctrineOptionDef.pillar` 仍是 string 且无 effects |
| `D/FAITH_IDENTITY_MODEL.md` | 312 | 借族群认同（嵌套/渠道/阀门/下沉度 Active-Passive-Absent/边界）建 `FaithIdentity`。**§7（`:165`）整段推翻自己**：「先认错——信仰是思想/观念/采纳，共同体不是信仰本身」，改为 `FaithTradition`（思想）+ `PersonalFaith`（个人采纳，可秘密）+ `FaithCommunity`（组织，可挂可不挂）+ `FaithIdentity`（社会边界，可选） | §15（`:304`）：信仰是思想不是共同体；公开信仰最低层是流派/传统；个人可单独采纳信条且可秘密；信条状态照 CK3 Core/Permitted/Prohibited/Known；族群认同模型只借边界/渠道/阀门 | **0 行落地**。代码里无 `FaithTradition`/`PersonalFaith`/`FaithIdentity`（全仓 grep 均为 0）；角色层只有 `isSecretBeliever` 一个 bool |
| `D/FAITH_CK3_TERM_MAPPING.md` | 128 | 逐词裁决"采用 / 改名降级 / 不必要"。认为可直接对应：family/religion/faith/rite、doctrine+doctrine_group+doctrine_category、tenet+tenet status、personal tenet、holy site、head of faith、fervor、hostility、venerated dead | §5（`:83`）最小实现集：`FaithTradition/Faith/Rite/Doctrine/Tenet(Core-Permitted-Prohibited-Known)/PersonalTenet/VeneratedDead/Patron/ReligiousLeader/Organization`；§4（`:108`）明确"不做 Denomination 硬层、不做 Creed/Dogma/Confession/Catechism/Orthodoxy 独立系统、三个 LanguageRegulation 合并、政治/哲学 School 不进 Faith" | **0 行落地**。`Tenet`、`Hostility`、`PersonalTenet`、`VeneratedDead`、`Patron`、`ReligiousLeader` 全不存在；`SaintDef` 是它要求的 `VeneratedDead` 的极简版 |
| `D/FAITH_CULTURE_MODEL_BORROW.md` | 197 | 借文化系统（`TraditionDef` 可组合/互斥/升格/固化 + 因子 + Jaccard 相似度），主张 `FaithTraditionDef` 组件 + `FaithCommunity`（由传统集合涌现）为可玩单位，family/religion 只做标签视图 | §7（`:189`）：**放弃** `family→religion→faith→rite` 硬分层；相似度用传统重叠+因子；分裂/融合/改革用传统增删替换；不做单一"宗教满意度" | **0 行落地**。代码里恰恰是硬分层（5 值 nodeType），没有 `FaithTraditionDef`，没有传统集合/互斥/相似度。`ReligionCatalog.GetDivergence`（`:126`）是唯一类似物，但它是"教义集合差 × 固定权重"，且 **0 调用者** |
| `D/FAITH_LONG_CONTENT_CRITIQUE.md` | 270 | 对"五层长文"的逐项批判：level/unit/marker 是研究方法不是游戏实体；每层一个标志是伪精度；`denomination`/`tradition-stream`/`local custom` 不普适；语言/教职/伦理子类词性不平行；单单词术语表丢精度；belief-system 四型混维度 | §8（`:236`）替代模型：`ReligiousFamily?→Religion?→Faith?→FaithCommunity(必须,可玩)` + communityTypeId 数据驱动 + doctrines/tenets/liturgy/language/calendar/organization/clergy/practices；§9 三问题：层级标志词性不平行、基督教中心、单词压缩 | **0 行落地**。这份的"替代模型"其实又回到了"软四层 + 可玩共同体"，与它批判的对象只差"可选"二字 |

### 4.2 互相矛盾对照（Lead 最需要看的表）

| 争议点 | `FAITH_SYSTEM_ANALYSIS` | `FAITH_IDENTITY_MODEL` | `FAITH_CULTURE_MODEL_BORROW` | `FAITH_LONG_CONTENT_CRITIQUE` | 代码实际 |
|---|---|---|---|---|---|
| 要不要硬分层 | 要（`:240`/`:550` 四层真实）→ 不要（`:425` 改可选）→ 又要（实读 CK3 后 §23）| **不要**（`:124` 不需要硬分层）| **不要**（`:4` 放弃硬分层）| 五层强制不可留，四层可选可留（`:226`/`:236-252`）| **硬分层**（`ReligionNodeType` 5 值），但运行时只用 Succession 一层 |
| 最低可玩单位 | Faith（§4.1）→ Rite（§8）→ FaithCommunity（§15）→ Rite（§23）| FaithTradition / PersonalFaith（`FaithCommunity` 降为可选组织）| **FaithCommunity**（传统集合涌现）| **FaithCommunity** | `FaithSystem`（= 一个 Succession 节点 + 教会运行态）|
| 信仰是不是共同体 | 是（共同体=可玩单位）| **不是**（`:165` 信仰是思想，共同体另立）| **是**（共同体可玩）| 是 | **是**（`FaithSystem` 把"教统内容"与"教会财富/教阶/圣职"塞一个类）|
| 个人信仰层 | 提到（§23 末尾 Character 层）| **核心**（PublicFaith/PersonalFaith/秘密信仰 + PersonalTenet）| 未提 | 未提 | **有死骨架**：`CharacterData.privateFaithId:87` / `personalTenets:89` / `isSecretBeliever:91` + `CharacterManager` 5 个方法，**全部 0 调用者**（故 `isSecretBeliever` 永远 false）|
| 教义分类 | `pillar`→`groupId` + effects/requirements（§6.5）| 并入 `FaithTraditionDef` | `FaithTraditionDef.category` 7 类 | 组件化，不做层级 | `DoctrineCategory`(7 enum) + `pillar`(6 string) + UI 中文名(7) **三套并存** |

**判读：这 5 份不是 5 个方案，是同一场辩论的 5 个时间切片，后一份在推翻前一份。** 有引用价值的是三个"最新结论"：`FAITH_SYSTEM_ANALYSIS` §20-23（实读 CK3 文件后的四层 + Rite 普适 + 个人信条在角色层）、`FAITH_CK3_TERM_MAPPING`（最克制、可执行度最高）、`FAITH_IDENTITY_MODEL` §7-15（信仰≠共同体、公开/个人分离）。**冲突未收敛，不能直接照任一份写代码。**

---

## 5. 三栏矩阵

### 5.1 已实现（代码 + 数据 + 调用链齐备）

> 注意：本栏是「机制存在且能被调用」，**不等于「在世界里真的会跑」**——多数机制的输入（stateReligionId / faithId / holySiteTileIndices / highPriestCharacterId）在生成期恒为空，实际空转。详见 §6 R5。

| 能力 | 证据 |
|---|---|
| 宗教内容表（44 条 / 5 种节点 / 历史教派） | `AS/Religion/Religions.json`；`CD/Faith/ReligionDef.cs:8` |
| 教义选项表（46 条 / 6 pillar，含 exclusivity、upgrade） | `AS/Religion/Doctrines.json`；`CD/Faith/DoctrineOptionDef.cs:7` |
| JSON 加载 + 静态注册表 | `CM/ContentDefinitionIndex.cs:410/424`；`ReligionCatalog`/`DoctrinePool` |
| 信仰运行时骨架（22 个 FaithSystem，热忱/教阶/圣地/教职字段全有） | `WS/GameWorld.Core.cs:956`；`SF/Runtime/FaithSystem.cs` |
| 热忱机制（+25 冲突、−10/年、+50 圣地失陷、0–100 钳制） | `FH/ReligionSimulationEventHandler.cs:28`；`WS/GameWorld.Simulation.cs:371/392`；`FaithSystem.AddFervor:83` |
| 封圣判定 + 主保圣人读取（statePatronSaintId 无赋值点，选主保未实现） | `FH/CanonizationSystem.cs:49`；`FH/FaithContracts.cs:205`；`RealmData.statePatronSaintId:28` |
| 大圣战（宣战门槛、召集、结算、受益人接继承系统） | `FH/GreatHolyWarSystem.cs:16/62/77`；`WS/GameWorld.Simulation.cs:1043` |
| 传教/税收压力改宗（人口块级） | `SY/MissionarySystem.cs:38/80`；`WS/GameWorld.Simulation.cs:211` |
| 美德/罪行评分 → 灵性满足度 | `FaithSystem.GetVirtueScore:98`；`SY/CharacterManager.cs:102` |
| 契约分层（UI 只依赖 `IFaithQuery`，Faith 程序集只 refs Core+Content） | `CC/FaithContracts.cs:73`；`MyriadCreation.Faith.asmdef`；`TE/AsmdefArchitectureGateTests.cs:250` |
| 存档往返（并行数组 + 骨架重建 + overlay） | `SA/FaithSaveData.cs:50/123`；`SA/FaithPersistence.cs:17` |
| 3 个宗教地图图层 + 三档配色 | `RD/MapLayerRegistry.cs:80-82`；`ReligionCatalog.GetColor:248` |
| 信仰存根面板（名/热忱/圣地/主保 id） | `UIP/ReligionPanelText.cs:45-51`；`Scenes/Main.unity` |
| 测试（10 条：6 契约 + 4 存档） | `TE/FaithQueryExtensionTests.cs`（225 行）、`TE/FaithPersistenceTests.cs`（251 行） |

### 5.2 仅设计稿（文档写了，代码 0 行）

| 能力 | 出处 | 代码反证 |
|---|---|---|
| `FaithDef` / `FaithCommunity` / `FaithTradition(Def)` / `FaithIdentity` / `FaithFamily` | 全部 5 份 | 全仓 grep 类名 = 0 |
| `Tenet`（信条）+ Core/Permitted/Prohibited/Known 四态 | `FAITH_IDENTITY_MODEL.md:219`、`FAITH_CK3_TERM_MAPPING.md:17` | 无信条内容表、无四态；只有 `personalTenets: List<string>`（`CharacterData.cs:89`）与意识形态的 `IdeologyMovement.coreTenets:27` 两个字符串列表，无定义、无效果 |
| `PersonalTenet` / `PersonalFaith` / 公开-个人-秘密三层信仰 | `FAITH_IDENTITY_MODEL.md:239-247` | **只有死骨架**：`CharacterData.privateFaithId:87`、`personalTenets:89`、`isSecretBeliever:91`；`CharacterManager.InitPrivateFaith:37`/`AddPersonalTenet:46`/`RemovePersonalTenet:60`/`HasFaithDivergence:68`/`UpdateSecretBelief:79` 全部 0 调用者，且 `personalTenets` 不入档 |
| `VeneratedDead`（带 category/cultus/shrine/feastDay/patronEligible） | `FAITH_CK3_TERM_MAPPING.md:55` | 只有 `SaintDef` 6 个字段 |
| `Hostility`（Righteous/Astray/Hostile/Evil） | `FAITH_SYSTEM_ANALYSIS.md:120`、`FAITH_CK3_TERM_MAPPING.md:24` | 无；"敌对"只有外交 `ConflictLevel.Hostility`，与信仰无关 |
| `Patron` 关系（神圣/世俗，可挂个人/家族/聚落/政权/职业） | `FAITH_CK3_TERM_MAPPING.md:54` | 只有 `RealmData.statePatronSaintId` 一个 int，且无赋值点 |
| `ReligiousLeader` = 头衔 + 能力包 | `FAITH_CK3_TERM_MAPPING.md:57` | 只有 `FaithSystem.highPriestCharacterId`（int） |
| 改革/创建信仰（piety/fervor/legitimacy 门槛、加分项） | `FAITH_SYSTEM_ANALYSIS.md:119/448` | `ReligionCatalog.Create*` 存在但 **0 调用者**；`DoctrinePool.GetOptions` **0 调用者** |
| 教义产生玩法效果（婚姻/性别/圣战/奴隶/巫术/朝圣/税收/神职） | `FAITH_SYSTEM_ANALYSIS.md:178` | `selectedDoctrines` 只被 `FaithContracts.GetDoctrines:160` 与 UI 读；**无任何效果消费** |
| 信仰相似度/敌对由 family+religion+doctrine 推导 | `FAITH_SYSTEM_ANALYSIS.md:120` | `ReligionCatalog.GetDivergence:126` 存在，**0 调用者**（且只比教义集合，不看家族） |
| 信仰→文化双向（`FaithTraditionDef` 挂 `LawDef`、礼仪/语言/历法归 liturgy） | `FAITH_CULTURE_MODEL_BORROW.md:180`、`FAITH_SYSTEM_ANALYSIS.md:223` | 无；文化侧只有 `worshipVector` 参与相似度 |
| 神圣/世俗两套主保 + 节庆 + 圣地朝圣 | `FAITH_CK3_TERM_MAPPING.md:54-55` | `Deity.festivals` 字段存在但 **0 外部引用**、从不填充 |
| 教职接 `TitleHolding` | `FAITH_SYSTEM_ANALYSIS.md:224` | `EcclesiasticalTitle` 是独立小类，与头衔系统无引用关系 |

### 5.3 完全没有（文档没提、代码也没有）

| 空缺 | 说明 |
|---|---|
| 信仰的**世界生成** | 政权 `stateReligionId` 从不赋值；人口 `faithId` 恒 0 → 全部信仰机制空转 |
| **玩家改变国教的入口** | `IFaithCommand.SetStateReligion` 只有测试调用，无 UI/决策/事件入口 |
| 圣地数据 + 圣地玩法 | 无 `HolySite` 内容表；`CreateHolySite` 无 gameplay 调用者；无朝圣/节庆 |
| 信仰组织实体（教会/教团/修会，有成员/财产/会院） | `FaithSystem.churchWealth/churchInfluence` 是纯数字，无实体、无收支 |
| 教会与政权的**双向**关系（叙任、教产、绝罚、和约） | 只有 Laws 侧 `FaithStatus.Established` 一个枚举位；`HeterodoxPolicy` 定义了但无消费 |
| 信仰 UI 的功能面 | 无教义/信条/圣人/圣地/组织/领袖视图，无改革按钮 |
| 异端/分裂/改宗事件链 | 无；`FaithConflictTriggeredEvent` 只能由 `DeclareWar` 的两个异教政权触发（`WS/GameWorld.Simulation.cs:1013`） |

---

## 6. 架构风险（按严重度）

### R1【最严重】三套 id 空间 + 一层过滤，导致信仰引用静默失效

- `Religions.json` 的 `religionId` = 100–600；`ReligionCatalog._nextId` 从 **1000** 起自增（`CD/Faith/ReligionCatalog.cs:13`）；`ThoughtManager._nextFaithId` 从 **1** 起（`SY/ThoughtManager.cs:24`）；`PopulationBlock.faithId` 默认 **0**（`PopulationBlock.cs:15`，无初值）。
- `InitializeFaithSystems` 只给 `nodeType==Succession` 建运行时（`WS/GameWorld.Core.cs:962`）→ 44 条内容只有 22 个 `FaithSystem`。但 `RealmData.stateReligionId`、`PopulationBlock.faithId`、`ReligionDef` 的 `parentReligionId` 都可指向任意 nodeType。
- 后果：引用 Religion 根节点（如 100 基督教）或 School 节点时 `GetFaithSystem` 返回 null，所有 Query 静默返回 `0f`/`""`/`-1`（`FH/FaithContracts.cs:50-95` 全是 `?? 默认值`）。**没有校验、没有日志、没有测试覆盖**。
- 同一个 `int` 在 `ReligionCatalog.Get(faithId)`（`FaithContracts.cs:167`）里被当 religionId 用 —— 这个隐式等价（faithId ≡ religionId）是当前系统不崩的唯一原因，也是任何模型改造的第一个爆点。

### R2【最严重】两套并行的"信仰状态" + 两套并行的"国教"

- 内容态 `ReligionDef`（`CD/Faith/ReligionDef.cs`）与运行时态 `FaithSystem`（`SF/Runtime/FaithSystem.cs`）字段大量重叠：`virtues/sins` 两处都有（`ReligionDef.cs:28/30`、`FaithSystem.cs:52/54`）；教义两处都有（`ReligionDef.selectedDoctrines:44` vs `FaithSystem.doctrines:24`，且类型不同——`List<string>` vs `List<Doctrine>`）；礼仪一处有 `ReligionDef.rites:48`、另一处有 `FaithSystem.rituals:26` 且互不同步。
- `FaithContracts.GetDoctrines:160` 明确写了"先读 ReligionCatalog，空了再回退 FaithSystem.doctrines"——这是两套状态并行的实证补丁。
- 第二个寄存器：`ThoughtManager._faiths`（`SY/ThoughtManager.cs:16`）是完全独立的 `Dictionary<int, FaithSystem>`，自带 `CreateFaith:43` / `GetDominantFaith:151`，与 `GameWorld._faithSystems` 无任何同步。
- 第二套"国教"：`LawComposition.establishedFaith`（`LW/LawComposition.cs:187`，`FaithStatus.Established`）与 `RealmData.stateReligionId`（`RealmData.cs:21`）互不引用。CK3 里 `religious_head = title_key` + 国教是一个东西；这里会有两个真相。
- **任何借鉴 CK3 的方案都必须先决定：Faith 是一张表（Def+State 分离）还是两条表（Def/State 各一）。** 现在是"两条表 + 一半字段重复 + 两处注册表"。

### R3 字符串外键 + 三套教义分类词表

- `selectedDoctrines: List<string>`（`ReligionDef.cs:44`）、`DoctrineOptionDef.optionId:9`、`pillar:11`、`enhancedFrom:19`、`riteFamily/school/headName` 全是 string。无编译期校验；`DoctrinePool.Get` 返回 null 时 `FaithContracts.GetDoctrines:173` 直接 `continue`（静默丢条目）。
- 教义分类有三套互不兼容的词表：`DoctrineCategory` 7 值 enum（`DoctrineCategory.cs:4`）、`pillar` 6 值 string（数据实际值 + `ReligionCatalog.GetPillarWeight:159` 权重表 7 分支含未被使用的 `myth`）、UI 中文映射第三套（`UIP/ReligionPanelText.cs:55`，该方法本身 0 调用者）。
- `ReligionNodeType`(5) 与 `ReligionMapLevel`(3) 不一致（`ReligionNodeType.cs:4` vs `ReligionMapLevel.cs:4`），地图按 level 取色时 School 节点无对应 level（`ReligionCatalog.GetColor:262` 落 default 分支）。

### R4 静态全局运行态，没有世界生命周期所有者

- `ReligionCatalog` / `DoctrinePool` / `CanonizationSystem` / `GreatHolyWarSystem` 全 static（`ReligionCatalog.cs:7`、`DoctrinePool.cs:5`、`CanonizationSystem.cs:10/14`、`GreatHolyWarSystem.cs:10/14`）。
- `CanonizationSystem._saintsByFaith` 与 `GreatHolyWarSystem.ActiveWars` 是**跨世界、跨测试**的全局可变状态；只有 `Reset()`（`CanonizationSystem.cs:92`）可清，且无世界级拥有者调用。
- `FaithContracts.GetSaints:133` 已经写了 `if (_world == null) return list;` 的防御注释（"避免 null-world 查询泄漏跨世界/跨测试数据"）——架构不干净的自白。
- 存档必须靠 `PrepareLoadedWorld → InitializeFaithSystems` 重建骨架再 overlay（`SA/FaithPersistence.cs:13-15`），意味着"内容可重建"是硬约束；一旦 CK3 式的"动态创建 faith/rite"实现，静态注册表 + 无 id 分配的模型会立刻破坏读档一致性。

### R5 名义"大系统"、实际"读模型门面"

- 所有创造/演化 API 都是死代码：`ReligionCatalog.CreateTradition/CreateSuccession/CreateReligion/CreateRite` = 0 调用者；`GetDivergence` = 0；`IsAvailable` = 0；`ResolveTraditionId` = 0；`DoctrinePool.GetOptions` = 0；`FaithSystem.DailyTick` = 0；`CreateHolySite` 无 gameplay 调用者；`IFaithCommand` 无 gameplay 调用者；个人信仰 5 个方法（`CharacterManager.cs:37-82`）0 调用者，导致 `isSecretBeliever` 恒 false，`UpdatePiety` 里那一行 `if (c.isSecretBeliever) num -= 0.01f`（`CharacterManager.cs:112`）是死分支。
- `FaithSystem` 有 24 个字段，实测外部引用：`deities`/`rituals`/`taboos` = 0；`missionaryZeal`/`tolerance`/`churchWealth`/`churchInfluence` 仅出现在存档 DTO 与自身（`SA/FaithSaveData.cs`），无 gameplay 读取方；`orgType` 永远默认值（无赋值路径）。
- `FaithSnapshotDto` 里 `faithType/organizationType/hierarchyName/reformPillars/authority/isStateReligion` 全部产出，但 UI 一个都不显示（`UIP/ReligionPanelText.cs` 只读 4 个字段）。DTO 字段的事实消费者是 `TE/UiReadModelExtensionTests.cs` + `TE/FaithQueryExtensionTests.cs`。
- 含义：**信仰系统目前是"给未来预留的接口层 + 一份写得不错的内容表"**，不是游戏机制。判断"要不要借 CK3"时，不应把已有代码当作"已投入的沉没成本"。

---

## 7. 给 Lead 的三条判读

1. **可复用的只有两样**：`Religions.json`/`Doctrines.json` 的历史内容（44+46 条，质量高于项目其他内容表平均水平），以及 `IFaithQuery`→`FaithContracts`→`ReligionPanelText` 这条"契约隔离 + 存档 overlay"的工程管线（有测试、有 asmdef 门禁）。领域模型（5 值 nodeType、ReligionDef 多角色、FaithSystem 内容状态混合）**没有复用价值**，且已有设计稿全部主张要拆掉它。
2. **设计稿不能直接作为实现依据**：5 份文档互相推翻且未收敛（§4.2），唯一稳定共识只有三条——① 不要五层强制；② 教义/信条/礼仪/组织要拆成组件而非层级；③ 信仰 ≠ 共同体（组织可选）。CK3 的 `family(可选) → religion(可选) → faith → rite` + doctrines/tenets/holy sites/head/fervor 是唯一在文档里被实读验证过的结构（`FAITH_SYSTEM_ANALYSIS.md:479-560`）。
3. **真正的缺口不在数据结构，在"接线"**：即使一行模型都不改，只要给 `stateReligionId` 与人口 `faithId` 一个生成期赋值、给教义一个效果消费点、给玩家一个改教入口，现有骨架就能跑出可玩循环。反过来若先做 CK3 式重构（Faith/Rite/PersonalFaith 分层）而不接线，会得到第二套死代码。

---

*证据截止：2026-11-06，仓库工作区未提交状态。信仰专属测试 10 条（`TE/FaithQueryExtensionTests.cs`、`TE/FaithPersistenceTests.cs`），架构门禁测试 `TE/AsmdefArchitectureGateTests.cs` 强制 Faith 依赖边界。*
