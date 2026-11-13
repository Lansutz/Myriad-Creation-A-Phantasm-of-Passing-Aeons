# 内容定义与模组化 审计（2026-11-09）

## 0. 范围

- 读取：`Assets/Scripts/Content/MyriadCreation.Content.Definitions/**` 131 个 .cs；`Assets/Scripts/Content/MyriadCreation.Content.Modding/**` 45 个 .cs。
- 读取：`Assets/StreamingAssets/{Base,Mods}/**` 全部 47 个文件（含 1 个 mod 样例）。
- 跨域追调用链（只读引用，不改）：`ContentRegistry.cs` / `ContentRuntime.cs` / `ContentResolvers.cs` / `LawContentLoader.cs` / `CultureStageCatalog.cs` / `Localization.cs` / `BuildingSystem.cs` / `BuildingAvailabilitySystem.cs` / `DisasterSystem.cs` / `GameWorld.Core.cs` / `PolityComponentInnovations.cs` / `SocialClassAvailability.cs` / `LawEffects.cs` / `InnovationTree.cs` / `Editor/Content/ContentPackageExporter.cs` / `Tests/EditMode/*`（抽样 6 个）。
- 未覆盖：UI 层是否展示这些内容（`Assets/UI` 未查）；存档往返（除 `SaveSystem.cs` 里 content 字段外未逐一验）；单位/建筑的经济与战争效果链；测试套件未实跑（只读测试源码，不声称绿）。

## 1. 内容域清单（三态）

| 域 | 定义 / 注册点 | 加载器（file:line） | 内容文件（条目数） | 三态 | 消费者证据 |
|---|---|---|---|---|---|
| Ethos | `EthosDef` | `ContentDefinitionIndex.cs:201-205` | `Base/Ethos/Ethos.json`（8） | 半跑（加载✓，查询✗） | 无：`EthosDef` 除注册表/Resolver 外零解引用；`CultureData.cs:63 ethosId` 无人查表 |
| Heritage | `HeritageDef` | `:207-211` | **文件不存在**（无 `Base/Heritage/` 目录） | 空转 | 0 条 + `ContentResolvers.Heritages` 零外部调用 |
| Tradition | `TraditionDef` | `:213-217` | `Traditions.json`（13） | 在跑 | `ContentLookupAdapter.cs:26,63` → 文化传统因子 |
| Language | `LanguageDef` + 4 个 CSV | `:219-228, 332-347` | `Base/Language/Laethis`（1） | 在跑 | 命名 `GameWorld.Simulation.cs:914,979`；`ContentRegistry.cs:236-273` |
| EthnicGroup | `EthnicGroupDef` | `:230-234` | `EthnicGroups.json`（1） | 在跑 | `ContentLookupAdapter.cs:37` |
| FamilyTradition | `FamilyTraditionDef` | `:236-240` | `FamilyTraditions.json`（9） | 在跑 | `FamilyNode.cs:136,152-155,177`（含革新门槛） |
| CharacterTemplate | `CharacterTemplateDef` | `:242-246` | `CharacterTemplates.json`（6） | 半跑（只用 2 个硬编码 id） | `CharacterManager.cs:984-985` 只取 `tmpl_ruler`/`tmpl_spouse` |
| Dna | `TalentDefectDef` | `:248-252` | `DnaDefs.json`（8） | 在跑 | `DnaSystem.cs:126,145,164` |
| MentalHealth | `MentalDisorderDef` | `:254-258` | `MentalHealthDefs.json`（5） | 半跑（仅取显示串） | `MentalHealthSystem.cs:85` |
| Innovation | `InnovationDef` | `:260-264` + 兜底 `:440-509` | `Innovations.json`（186）+ 代码注入 2 | 在跑 | `InnovationTree.cs:274-300,684` |
| Religion | `ReligionDef` | `:266-270` → `ReligionCatalog.Load` | `Religions.json`（44） | 在跑 | `ReligionCatalog.cs:174-197`（但 `IsAvailable` 零调用者，见 §3） |
| Doctrine | `DoctrineOptionDef` | `:272-276` → `DoctrinePool.Load` | `Doctrines.json`（46） | 在跑 | `FaithContracts.cs:172,253` |
| Title | `TitleDef` | `:294-298` + `TitleManager.LoadFromContent`（`TitleManager.cs:21-40`） | `Titles.json`（15） | 在跑 | 全量由 JSON 驱动，无硬编码名 |
| Equipment | `EquipmentDef` | `:278-292` | `Equipment.json`（23） | 空转 | 唯一消费者 `InnovationTree.cs:260 AreItemInnovationsMet` 零调用者 |
| Culture（host） | `CultureData` + CSV | `ContentRuntime.cs:31-40,60-85` | `Base/Culture/Laethis`（1） | 在跑 | 文化包全链 |
| Race（host） | `RaceData` | `ContentRuntime.cs:42-49,87-98` | `RaceDefs.json`（**0** 条） | 空转 | 全代码 `raceId` 恒 0：`CharacterManager.cs:981`、`GameWorld.Core.cs:852`、`GameWorld.Generation.cs:107`、`SandboxSpawnSystem.cs:71` |
| Biome（host） | `BiomeDef` | `ContentRuntime.cs:51-58,100-112` | `Biomes.json`（92） | 在跑 | `ContentRegistry.cs:70` → `BiomeRegistry.Overrides` |
| Recipe | `GoodsRecipe` | `RecipeCatalog.cs:25-37` | `Crafting/Recipes.json`（43） | 在跑 | `SettlementProduction.cs:32,36` |
| PropertyDefinition | `PropertyDefinition` | `PropertyDefinitionCatalog.cs:22-34` | `Properties/PropertyDefinitions.json`（5） | 半跑（只作白名单） | 仅 `GoodsPropertyCatalog.cs:87` 校验用，无业务读取 |
| GoodsProperties | `GoodsPropertyValue` | `GoodsPropertyCatalog.cs:20-32` | `Goods/GoodsProperties.json`（29） | 在跑 | `GameWorld.Core.cs:684-692` |
| LanguageFamily | `LanguageFamilyNode` | `LanguageFamilyCatalog.cs:23-37` | `_generated/language-families.json`（75） | 空转 | 除 `ContentRegistry.cs:113,143` 的 Init/Reset 外零调用 |
| Law | `LawGroupDef`/`DecreeDef` | `LawContentLoader.cs:71-73` | `Base/Laws/Laws.json`（12 组/2 法令） | 在跑 | `LawContracts.cs:41,129` |
| CultureStage | `CultureStageDefinition` | `CultureStageCatalog.cs:24,32`（Base-only） | `Base/Culture/CultureStages.json`（5） | 在跑 | `CultureStageEvolutionSystem.cs:26-151` |
| Localization | key→str | `Localization.cs:37-41` | `Base/Localization/*.json`（8 语言） | 在跑 | `Localization.Get` 全仓 |

## 2. 加载链

**谁调谁（包通路）**：`ContentRegistry` 静态构造 `ContentRegistry.cs:76` → `Initialize()` `:78` → `ContentRuntime.LoadRuntime`（Culture/Race/Biome，`:94`）+ `ContentDefinitionIndex.Initialize`（14 个纯定义域，`:95-96`）→ `ContentSourceCatalog.CreateDefault` → 逐 source `providerCatalog.Load(source)`（`ContentDefinitionIndex.cs:163-169`）→ `ContentProviderCatalog.Load` 遍历所有注册 provider（`ContentProviderCatalog.cs:16-22`）→ `ContentProviderRegistration.Load`（`:19-22`）→ `DelegateContentProvider.Load` → `EnumerateRoots` → `LoadXxxRoot` → `JsonUtility.FromJson` → `IContentStore.Set`（`DictionaryContentStore.cs:32` 索引器赋值）。

**priority 比较方向**：`ContentSourceCatalog.cs:16-18` 里 Base=0、Mods=100，`Sort` 升序 → 先 Base 后 Mods；同名 key 由后写覆盖前写（`DictionaryContentStore.cs:32`），故 **Mods 覆盖 Base 成立**。证据：`ContentRuntime.cs:79-84` 在写入前 `TryGet` 探测并打印"被 Mods 覆盖"。覆盖粒度=整个条目按 id 替换，**无字段级合并、无删除语义**（`IContentStore.Remove` 存在但加载链从不调用）。

**目录布局真相**：`DelegateContentProvider.cs:43-61` 双模式——root 下若存在 `Culture/` 目录或 `Race/RaceDefs.json`、`Innovation/Innovations.json`、`Religion/Religions.json` 之一，则 root 当唯一内容根；否则把 root 的**每个子目录**当一个内容根。所以 Mod 必须是 `Mods/<pkgId>/<ContentDir>/<file>`。
- 仓库唯一 mod 样例 `Mods/Dna/DnaDefs.json`（2 条）落在该规则下被判为包目录 `Dna`，随后找 `Mods/Dna/Dna/DnaDefs.json`（`ContentDefinitionIndex.cs:250`）→ 不存在 → **运行时永不加载**（grep 无其他读 `Mods/Dna` 的路径）。
- 平铺 `Mods/<ContentDir>/<file>` 仅在同时存在 4 个哨兵之一时生效。

**另一套并行通路（平铺 Base+Mods，不走包语义）**：`RecipeCatalog.cs:33-34`、`PropertyDefinitionCatalog.cs:30-31`、`GoodsPropertyCatalog.cs:28-29`、`LanguageFamilyCatalog.cs:32-33`、`Localization.cs:37-41` 直接读 `Mods/<相对路径>`。同一个 `Mods/` 目录下并存两种互不兼容的布局约定。
**Base-only（完全无 Mods 通路）**：`LawContentLoader.cs:32`、`CultureStageCatalog.cs:24`（两者注释都写"mod 可通过同路径 JSON 覆盖"，即要求直接改 Base 文件）。

## 3. 校验

| 域 | 校验点 | 强度 | 证据 | 运行时是否执行 |
|---|---|---|---|---|
| Mod 包/清单 | formatVersion、packageId 字符集、contentTypes ∈ 14 目录、相对路径逃逸 | 结构级，无内容级 | `ContentPackageValidator.cs:11-60, 79-99`；白名单 `ContentPackageLayout.cs:7-11` | **否**：唯一调用者 `ContentExtensionApi.cs:20`，而 `ContentExtensionApi` 全仓零调用者；`Editor/Content/ContentPackageExporter.cs:17` 无 MenuItem、零调用者 |
| 法律 | groupId/lawId/decreeId 非空、至少 1 法 | 仅主键非空 | `LawContentLoader.cs:110-139` | 是（但无引用完整性：`requiredInnovationIds` 是否存在于革新表不校验） |
| 语言谱系 | 父节点缺失 + 成环 → 丢弃节点 | 真引用完整性 | `LanguageFamilyCatalog.cs:128-168, 170-188` | 是（但该域零消费者，§4） |
| 配方 | 输出 id<0、输入空、输入量≤0、自引用 | 结构级 | `RecipeCatalog.cs:129-151` | 是 |
| 物产属性 | 属性 id 必须在 `PropertyDefinitionCatalog` 中，否则 Warn+丢弃该属性 | 跨表引用 | `GoodsPropertyCatalog.cs:87-94` | 是 |
| Ethos/Heritage/Tradition/Language/EthnicGroup/FamilyTradition/CharacterTemplate/Dna/MentalHealth/Innovation/Religion/Doctrine/Title/Equipment/Culture/Race/Biome | **无**（只 `FromJson` + 主键非空） | — | `ContentDefinitionIndex.cs` 全文无 Validate；`ContentRuntime.cs:60-112` 无 Validate | — |
| 革新前置 | 只在求值期判 `innovationId > 0 && HasInnovation` | 求值期，非加载期 | `InnovationTree.cs:274-300, 198-269` | 是；悬空 id → 永久不可满足，**无告警** |
| 宗教革新门槛 | `requiredInnovations` 任一满足 | 求值期 | `ReligionCatalog.cs:174-197` | **否**：`IsAvailable` 零调用者（grep 全仓仅声明行） |
| 法律革新门槛（旧） | `LawSystem.CheckInnovationRequirements` / `GetMissingInnovations` | 求值期 | `LawSystem.cs:116-149` | **否**：两者零调用者；实际门槛走 `LawAndDecreeSystem.cs:76-96, 183-200`（`requiredInnovationIds`，语料来自 JSON） |
| 数据集许可 | datasets.lock.json ↔ LICENSES.md ↔ 文件头 license | 测试级 | `Tests/EditMode/DatasetIntegrityTests.cs:98-177` | **否**：grep `datasets.lock` 仅命中 Tests，运行时无许可门禁 |

## 4. 未被使用的内容（定义存在 / 加载了 / 有消费者）

| 对象 | 定义存在 | 加载了 | 有消费者 | 证据 |
|---|---|---|---|---|
| `EpithetCatalog`（81 条绰号，C# 内建） | 是 | 否（懒 Build） | **否** | `EpithetCatalog.cs:42-126`；grep `EpithetCatalog\.` → 0 命中；`EpithetSystem.cs` 自带 156 处中文字面量，不引用该目录 |
| `HeritageDef` | 是 | 是（0 条） | **否** | 无 `Base/Heritage/` 目录；`ContentResolvers.Heritages` 零调用 |
| `EthosDef` | 是 | 是（8） | **否** | 仅 `ContentRegistry.cs:161` 包装，无解引用点 |
| `EquipmentDef` | 是 | 是（23） | **否** | `InnovationTree.cs:260` 唯一引用，零调用者 |
| `LanguageFamilyCatalog` | 是 | 是（75） | **否** | 仅 `ContentRegistry.cs:113,143` |
| `RaceData` | 是 | 是（0 条） | **否** | `RaceDefs.json` `races: []`；raceId 恒 0 |
| `DisasterDef`（结构） | 是 | 否（无 JSON） | 是（内建 12 条） | 见 §5；结构被 `DisasterSystem.cs:34-144` 填充 |
| 类型级零消费者（全仓仅 1 次出现） | 是 | 否 | **否** | `SemanticModifier.cs`、`SemanticAuthority.cs`、`TitleModules.cs`、`TitleHoldingMode.cs`、`InheritanceLawCatalog.cs`、`InnovationReviewPolicy.cs`、`MentalDisorderIds.cs`、`VehicleCategory.cs`、`QualityManager.cs`、`ContentStore.cs` |
| `JsonFileContentProvider` | 是 | 否 | **否**（仅测试 `ArchitectureContractsTests.cs:311` 实例化） | `JsonFileContentProvider.cs:8-96` |
| 创作侧 API | 是 | 否 | **否** | `IContentAuthoringWorkspace`/`ContentAuthoringWorkspace`/`ContentExtensionApi`/`ContentPackageManifest` 只在 `ContentExtensionApi.cs` 与测试内互引 |

## 5. 硬编码 vs 内容（C# 内建"内容"清单）

| 内容 | 规模 | 位置 | 可否 JSON 覆盖 |
|---|---|---|---|
| 建筑定义 | 29 | `BuildingSystem.cs:32-92`（`AddBuildingDef` 私有，`:95`），构造期 `:27` 建表，`_buildingDefs` 私有 `:17` | 否（无 Buildings.json） |
| 单位定义 | 14 + 招募料 12 组 | `GameWorld.Core.cs:761-788`（`AddUnitDef` `:804`） | 否 |
| 商品定义 | 57（`AddGoodsDef` 调用 58 含声明） | `GameWorld.Core.cs:601-659, 744-759`；自然资源标记 `:660-683` | 部分：`properties` 由 `GoodsProperties.json` 追加（`:684-692`） |
| 灾害 | 12 | `DisasterSystem.cs:34-144` | 否（无 Disasters.json） |
| 政体组件→革新门槛 | 35 组 list | `PolityComponentInnovations.cs:21-140`（表 `:85-140`） | 否 |
| 阶层/子阶层门槛 | 11 子阶层 + 3 类 | `SocialClassAvailability.cs:11-62, 64-79` | 否 |
| 阶层涌现映射 | — | `ClassEmergenceEvents.cs:10-43`（tick 入口 `GameWorld.Simulation.cs:266`） | 否 |
| 绰号 | 81 | `EpithetCatalog.cs:42-126` | 否（且零消费者） |
| 经济原型 | 11 | `EconomicalArchetypes.cs:7-17`（位于 Content 程序集内） | 否 |
| 建筑选址规则 | 30 条（含 allowedBiomes 枚举数组） | `BuildingAvailabilitySystem.cs:22-70`；唯一入口 `SettlementAISystem.cs:17` | 否 |
| 法律→革新映射 | 14 处 `requiredInnovations.Add` | `LawEffects.cs:390-435`（string 键 `"bureaucracy"`/`"writing"`…，与 `InnovationDef.innovationId` 的 int 体系不同源） | 否 |
| 革新加载期注入 | 2 条兜底 + 能力/发现组缺省 | `ContentDefinitionIndex.cs:440-461, 462-487, 491-509` | 加载器自己写死 |
| 人物模板 id | 2 | `CharacterManager.cs:984-985`（`tmpl_ruler`/`tmpl_spouse`） | 否 |
| 资源默认群系 | 排除 `IceSheet`/`MountainGlacier` | `GameWorld.Core.cs:733-742` | 否 |
| 政体组件中文名 | 32 处字面量 | `LawComponentNames.cs` | 否 |
| **反例：头衔** | 15 | `TitleManager.cs:21-40` 全量读 `Titles.json` | 是 |

另：`DisasterType`/`BiomeType`/`SocialClass`/`BuildingCategory`/`InnovationField`/`EquipmentCategory` 等是 enum，内容只能覆盖既有枚举值，"新增一个"必须动枚举（如 `BiomeDef.BiomeType => (GameEnums.BiomeType)biomeId`，`BiomeDef.cs:24`）。

## 6. 模组可行性判定：新种族 + 新建筑 + 新法律 + 新革新

**新种族（数据可入、无人使用）**
1. `Base/Race/RaceDefs.json` 加条目即可被读（`ContentRuntime.cs:42-49, 87-98`；`Race` 在包目录白名单 `ContentPackageLayout.cs:9`）。
2. 但 4 处 `raceId = 0` 硬编码使新种族永不生效，必须改：`CharacterManager.cs:981`、`GameWorld.Core.cs:852`、`GameWorld.Generation.cs:107`、`SandboxSpawnSystem.cs:71`；配合 `CharacterManager.cs:758-765 ResolveRace`。
3. 生物群系偏好仍是 enum 键（`RaceDTO.cs:186`），要新群系还得改 `GameEnums.BiomeType`。

**新建筑（必须改 .cs）**
1. 无 `Buildings.json`；定义只能在 `BuildingSystem.cs:30-92` 增 `AddBuildingDef` 行（private，无外部注册 API `:95`）。
2. 选址须在 `BuildingAvailabilitySystem.cs:22-70` 注册，唯一消费点 `SettlementAISystem.cs:17`；未注册的 `BuildableType` 无法建造。
3. 类别号来自 `BuildingCategory.cs` 枚举；`_buildingDefs` 私有（`:17`）无对外枚举 → 任何"列出可建建筑"的 UI 都得改代码。

**新法律（数据可加，但通路缺）**
1. `Base/Laws/Laws.json` 加组/法可被解析（`LawContentLoader.cs:104-141`）；但 `:32` 只有 Base 路径，**无 Mods 通路**，注释 `:27` 自述"mod 可通过修改 `Base/Laws/Laws.json` 覆盖"。
2. 内容包白名单不含 `Laws`（`ContentPackageLayout.cs:7-11`）→ 通过 `ContentPackageValidator` 的合规包不能携带法律。
3. 效果须改 C#：`LawEffects.cs:390-435`（lawId 硬编码）与 `CalculateEffects`（`:51`）。
4. 革新门槛字段虽然在 JSON 里（`LawAndDecreeSystem.cs:76-96`），但 string 键（`LawEffects.cs:44`）与 int 革新 id 两套体系并存。
5. 若落在政体组件维度，还需改 `LawRealmHost.cs:215-370` 的 switch 与 `LawReform.cs:43-98`。

**新革新（唯一可零代码）**
1. `Base/Innovation/Innovations.json` 加条目即可（`ContentDefinitionIndex.cs:433-489`）；int id 需避开兜底 1/1025 与既有 186 条。
2. 若依赖新能力通道：须改 `InnovationField` 枚举 + `CapabilityChannels.cs:3-10` + `InnovationDomainMap.cs:3` 映射，否则 `discoveryGroups` 缺省落到 artisan（`:462-487`）。
3. 若被别处当作门槛引用：法律 → `LawEffects.cs`；单位 → `GameWorld.Core.cs:763-776`（params 实参）；政体组件 → `PolityComponentInnovations.cs`；阶层 → `SocialClassAvailability.cs`；宗教 → 仅 JSON（`ReligionDef.requiredInnovations`，但求值入口零调用，见 §3）。

**结论（事实层）**：四类内容中，仅"新革新（纯 JSON）"以及 Tradition/Religion/Title/Doctrine 可零代码落地；种族（4 处 raceId）、建筑（2 个 .cs + 枚举）、法律（Base-only 通路 + 效果硬编码）都必须改 .cs。仓库内既无 mod 加载 UI，也无可用样例（`Mods/Dna` 不生效），`ContentPackageExporter` 零调用者。

## 7. 缺口清单（按影响排序）

| # | 缺口 | 影响 | 证据 | 严重度 |
|---|---|---|---|---|
| 1 | Mod 覆盖语义只在平铺通路可用，包通路无生效样例 | 声称的 mod 支持在真实内容上未验证 | `Mods/Dna/DnaDefs.json` + `ContentDefinitionIndex.cs:250` + `DelegateContentProvider.cs:43-61` | P0 |
| 2 | 法律/文化阶段无 Mods 通路且不在包白名单 | "新增法律"不可作为 mod 交付 | `LawContentLoader.cs:32`、`CultureStageCatalog.cs:24`、`ContentPackageLayout.cs:7-11` | P0 |
| 3 | 种族域 0 条 + raceId 恒 0 | 种族相关字段（基因组/外观/生产偏好）全为默认 | `RaceDefs.json` `races: []`；`CharacterManager.cs:981` 等 4 处 | P0 |
| 4 | 建筑/单位/商品/灾害/政体门槛全硬编码 | 内容扩展必须改 .cs 并重编译 | §5 各行 | P1 |
| 5 | `ContentPackageValidator` 与创作侧 API 运行时不生效 | 任何"模组校验/导出"流程实际为 0 | `ContentExtensionApi.cs` 零调用者；`ContentPackageExporter.cs:17` 无 MenuItem | P1 |
| 6 | 14 个包通路域无任何 schema/引用校验 | 悬空 id 静默失效（革新前置、法律门槛） | `ContentDefinitionIndex.cs` 无 Validate；`InnovationTree.cs:274-300` | P1 |
| 7 | Heritage 有加载器无内容；Ethos/Equipment 加载后无消费者 | 三域数据空转 | §4 表 | P1 |
| 8 | `EpithetCatalog` 81 条内建且零消费者 | 绰号数据在 `Content.Definitions` 里但无人读 | `EpithetCatalog.cs:42-126` | P2 |
| 9 | `LanguageFamilyCatalog` 加载 75 节点无查询者 | 数据集只进内存不出结果 | 仅 `ContentRegistry.cs:113,143` | P2 |
| 10 | 两套 Mods 布局并存（包目录 vs 平铺） | 作者侧无单一事实来源 | §2 两段 | P2 |
| 11 | 数据集许可仅测试级 | 运行时无许可门禁 | `DatasetIntegrityTests.cs:98-177` | P2 |

## 8. 我核实不了的事

1. 未实跑测试：`Tests/EditMode/ContentDefinitionIndexTests.cs`、`RecipeSystemTests`、`PropertyCatalogTests`、`LanguageFamilyTests`、`ArchitectureContractsTests` 是否全绿未验证（只读了源码，其中 `ContentDefinitionIndexTests.cs:36` 直接指向真实 `streamingAssetsPath`，会把不生效的 `Mods/Dna` 一并走一遍）。
2. 未查 UI：是否存在列出建筑/头衔/法律的界面对应 `BuildingSystem._buildingDefs` 私有表（`Assets/UI` 未读）。
3. 未验存档往返：`GoodsDef`（`SaveData.cs:41`）、`RaceData`（`SaveSystem.cs:382,420`）入档，但内容表其它域是否随档走未验证。
4. `RecipeCatalog.GetByOutput`/`GetAll` 与 `PropertyDefinitionCatalog.All` 的外部消费者未逐一枚举（只确认主链路）。
5. `BuildingAvailabilitySystem` 30 条注册是否覆盖 `BuildableType` 全部枚举值未比对（未读 `BuildableType.cs` 全枚举）。
6. 未确认是否存在本仓库外的 mod 发行流程（CI/打包脚本未查）。

## 9. 附：文件清单（机械枚举，176 文件 / 5242 行）

枚举：`glob Content/**/*.cs`；类名 `grep '^\s*(public|internal)\s+.*(class|struct|enum|interface)\s+\w+'` → Definitions 136 命中、Modding 46 命中（`ContentDefinitionIndex.cs` 含 2 个声明）。行数 `Measure-Object -Line`：Definitions 根 **119 文件 / 2740 行**、`Definitions/Faith` **12 / 501**、`Modding` **45 / 2001**。

**`Content.Modding`(45)**｜源与优先级：`ContentSource` `ContentSourceKind` `ContentSourceCatalog`｜契约：`IContentProvider` `IContentProviderRegistration` `IContentStore` `IContentResolver` `IContentExtensionApi` `IContentAuthoringWorkspace`｜实现：`ContentProviderCatalog` `ContentProviderRegistration` `ContentStore` `DictionaryContentStore` `ContentResolver` `DelegateContentProvider` `JsonFileContentProvider`｜打包校验：`ContentExtensionApi` `ContentAuthoringWorkspace` `ContentPackageLayout` `ContentPackageManifest` `ContentPackageValidationResult` `ContentPackageValidator`｜索引：`ContentDefinitionIndex`｜定义载体：`EthosDef` `HeritageDef` `TraditionDef` `FamilyTraditionDef` `LanguageDef` `EthnicGroupDef` `CharacterTemplateDef` `TitleDef` `PlaceSuffixDef` `EffectEntry`｜附属表：`GoodsPropertyCatalog` `GoodsPropertyEntry` `GoodsPropertyArtifact` `PropertyDefinitionCatalog` `PropertyDefinitionArtifact` `RecipeCatalog` `LanguageFamilyCatalog` `LanguageFamilyNode` `LanguageFamilySource` `LanguageFamilyArtifact` `SemanticAuthority` `SemanticModifier`

**`Definitions/Faith`(12)** `ReligionDef` `ReligionCatalog` `FaithType` `FaithOrganizationType` `ReligionMapLevel` `ReligionNodeType` `DoctrineCategory` `DoctrineOptionDef` `DoctrinePool` `SaintDef` `Deity` `EcclesiasticalTitle`

**`Definitions` 根(119)**
- 纯枚举 70 文件（67 单枚举 + `ShipDesignTaxonomy` 4 枚举 + `InnovationPrerequisiteNode`/`InnovationUnlock` 各 enum+class）：`Allele` `AssemblyComposition` `AllianceType` `BodyPartType` `BondType` `BuildableType` `BuildingCategory` `CentralInstitution` `CentralSuccession` `CharacterDiseaseCategory` `CharacterRole` `ClassNeedDimension` `CriticalJunctureType` `DiplomaticEventType` `DisasterCategory` `DisasterType` `DiseaseStage` `DiseaseType` `DnaLocus` `EconomicalArchetype` `EligibilityScope` `EpithetConnotation` `EpithetTier` `EvaluationLevel` `FactionStance` `GameEventType` `InnovationDomain` `InnovationField` `InheritanceAge` `InheritanceBranch` `InheritanceGender` `InheritanceScope` `JunctureOutcomeType` `LandInheritanceMode` `LocalLordship` `LocalAppointAuthority` `LocalScope` `LocalSuccession` `MovementPhase` `OfficialOffice` `PersonalityDimension` `PunishmentType` `RelationshipType` `ResourceType` `SpatialStructure` `SpecialBondType` `SpecialtyType` `SubordinationType` `SubStructureType` `SupremeScope` `SupremeSuccession` `TitleAppointmentMethod` `TitleHoldingMode` `TitleInheritanceMode` `TitleResidence` `TraditionCategory` `TraditionFactorKey` `TraitCategory` `TraitTier` `TransmissionType` `TreatyClauseType` `CraftProcess` `EquipmentCategory` `VehicleCategory` `WeaponFamily` `InnovationKind` `RecipeOutputKind`
- struct 12：`BuildingDef` `UnitDef` `GoodsDef` `DisasterDef` `DiseaseDef` `CharacterDiseaseDef` `CapabilityModifier` `CapabilityRequirement` `FactionPlatform` `GoodsPropertyValue` `RecipeIngredient` `ResourceSpawnCondition`
- class 22：`ActiveJuncture` `BasicAbilities` `DynamicGoodsSaveData` `EconomicalArchetypeInfo` `EligibilityRules` `EpithetDef` `EquipmentDef` `GoodQuality` `GoodsRecipe` `IdentityProfile` `InheritanceLaw` `InnovationDef` `InnovationPrerequisiteRationale` `InnovationSoftPrerequisite` `LocusFrequency` `MentalDisorderDef` `OfficeTitleEntry` `PropertyDefinition` `QualityManager` `RecipeArtifact` `TalentDefectDef` `TitleCandidateContext`
- 静态表/helper 14：`CapabilityChannels` `EconomicalArchetypes` `EpithetCatalog` `InnovationDomainMap` `InnovationEra` `InnovationReviewPolicy` `LegacyIdentityMapping` `MentalDisorderIds` `PersonalityDimensions` `SpecialtyDerivation` `SpecialtyTypeExtensions` `TitleModules` `TraditionElevation` `InheritanceLawCatalog`
- 接口 1：`IContentLookup`

## 10. 第二遍独立复核（只列第一遍未覆盖或需修正的事实）

1. **隐式整档丢弃（新失败模式）**：`LoadReligions`（`ContentDefinitionIndex.cs:410-420`）只判 `item != null` 就 `target.Set(item.religionId, item)`，而真正被使用的存储是 `DictionaryContentStore`（注册点 `ContentDefinitionIndex.cs:148-161`），其 `Set` 对 default key **抛** `ArgumentException`（`DictionaryContentStore.cs:22-33`）。异常在 `foreach` 内抛出 → `DelegateContentProvider.cs:32-39` 捕获后仅 `LogWarning` → **同一 JSON 的剩余条目全部丢弃**，且 `ReligionCatalog.Load`/`EnsureColors`（`:418-419`）不再执行。第一遍 §3 记作"仅主键非空"，未含此抛异常路径。
2. **哨兵强度不一致**：`DelegateContentProvider.cs:49` 为 `Directory.Exists(Culture)` + 3×`File.Exists(具体文件名)`（严格）；`JsonFileContentProvider.cs:83` 四项全用 `Directory.Exists(...)`（宽松）。对"只有 `Religion/` 目录、无 `Religions.json`"的平铺根，两者判定相反（前者下钻子目录、后者视为内容根）。宽松实现目前零实例化（第一遍 §4 已记），故差异只在测试面。
3. **革新 1/1025 是"代码压过 JSON"而非"兜底"**：`Set(1025,…)`（`ContentDefinitionIndex.cs:491`）与 `Set(1,…)`（`:501`）位于解析循环（`:437-489`）**之后且无条件**，因此 JSON 中同 id 条目必被代码版整体替换（名称"领土王国"/"基础革新"、费用 800/10、前置 500 均硬编码）。第一遍 §6 只提示"需避开兜底 1/1025"。
4. **维度内"新法律选项"由枚举决定，不由硬编码表决定**：`LawConstraints.GetAllOptions:1281-1295` 用 `Enum.GetValues(typeof(X)).Cast<int>().ToList()` 展开；但新增一个枚举值必须同步 4 处：`LawConstraints.cs:1097-1105`（LawDimension→LawGroup）、`:1197-1205`（现值读取）、`:1214-1222`（中文名）、`:1231-1271`（合法性判定）。第一遍 §6"新法律"未列 `LawConstraints`。
5. **Localization 8 文件实为 3 活 5 空**：`Base/Localization/{de,es,fr,ja,ko}.json` 均 `entries=0`；仅 `en`/`zh-Hans`/`zh-Hant` 各 135 条（`ConvertFrom-Json` 逐文件计数）。第一遍 §1 记作"8 语言 / 在跑"。
6. **条目数复核：第一遍数字全部正确**（逐文件 `ConvertFrom-Json` 计数）：ethos 8、tradition 13、innovation 186、religion 44、doctrine 46、title 15、equipment 23、biome 92、recipe 43、goodsProperties 29、propertyDefinition 5、languageFamily 75、laws 12 组+2 法令、characterTemplate 6、dna 8、familyTradition 9、mentalHealth 5、ethnicGroup 1、race **0**、cultureStages 5。
