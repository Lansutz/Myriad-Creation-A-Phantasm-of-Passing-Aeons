# MyriadCreation.Content.Modding（P1a + P1b + P2 / W4）

本目录为 **Content 程序集**内的纯内容代码：**47 个 `.cs`**（P1a 20 个定义/DTO + P1b 26 个基础设施 + P2 `ContentDefinitionIndex`），`.meta` 随迁、GUID 保留；Content asmdef 仍只引用 `MyriadCreation.Core`。

## 命名空间（W4 已统一）

- 本目录全部文件使用 **`namespace MyriadCreation.Content.Modding`**（W4 前暂用 `MyriadCreation.Simulation.Modding`，已全仓替换）；
- 全仓引用本目录类型的文件统一 `using MyriadCreation.Content.Modding;`（W4 更新 44 个 using 文件）；
- 本目录只引用 `System` / `UnityEngine` / `MyriadCreation.Core*` / `MyriadCreation.Content.Definitions`，**无** `using MyriadCreation.Simulation/World/Culture/Faith/Laws`。

## Host facade 边界（不可迁入 Content）

`Simulation/MyriadCreation.Simulation.Modding/` 下仍有 4 个 host 文件（目录名保留，命名空间已改为 **`MyriadCreation.Simulation.ContentHost`**）：

| 文件 | 为什么不能迁 Content（边界） |
|---|---|
| `ContentRegistry.cs` | 公开 API 持有 `CultureContentPack`（内含 `CultureData`）、`Races`（`RaceData`）、`Biomes`（`BiomeDef`/`BiomeRegistry.Overrides`）；这些类型属 Culture/State/World，迁 Content 会让 Content→Simulation/World 形成硬环。纯定义字典已委托 `ContentDefinitionIndex`，本类只作 host facade。 |
| `ContentRuntime.cs` | 负责 JSON→`CultureData`/`RaceData`/`BiomeDef` 运行时映射（含 CSV 名池、BiomeRegistry 填充）；运行时类型不可进 Content。 |
| `ContentResolvers.cs` | 16 个静态 resolver 中 `Cultures/Races/Biomes` 的泛型参数是 `ContentRegistry.CultureContentPack`/`RaceData`/`BiomeDef`；签名不能改（42 调用方面零改动），故保持 host facade。其余 13 个纯定义 resolver 通过 ContentRegistry 的属性（已委托 ContentDefinitionIndex）工作。 |
| `ContentQueries.cs` | `GetRandomName(ContentRegistry.CultureContentPack, ...)` 的入参是 runtime CultureData/NamePoolData；签名保留，故留 host；纯名称池 fallback 仍在 host 侧。 |

**结论**：纯内容定义/索引/JSON 在 `MyriadCreation.Content.Modding`；运行时映射/Resolvers/Queries 在 `MyriadCreation.Simulation.ContentHost`（文件暂留 Simulation.Modding 目录，后续可平移到 `Simulation/MyriadCreation.Simulation.ContentHost/` 目录，命名空间已先行）。Save 字段/静态 ctor/Initialize/Reset 语义不变；`ContentDefinitionIndex` 已承担纯定义解析。

## 逐批清单

- **P1a（20 个定义/DTO）**：CharacterTemplateDef / EffectEntry / EthnicGroupDef / EthosDef / FamilyTraditionDef / GoodsPropertyArtifact / GoodsPropertyEntry / HeritageDef / LanguageDef / LanguageFamilyArtifact / LanguageFamilyNode / LanguageFamilySource / PlaceSuffixDef / PropertyDefinitionArtifact / SemanticAuthority / SemanticModifier / SemanticTags / TitleDef / TitleKinds / TraditionDef
- **P1b（26 个基础设施）**：
  - Provider/Store/Source：IContentProvider、IContentProviderRegistration、IContentStore、IContentResolver、ContentProviderCatalog、ContentProviderRegistration、ContentSource、ContentSourceCatalog、ContentSourceKind、ContentStore、DelegateContentProvider、DictionaryContentStore、JsonFileContentProvider
  - 静态目录/工具：ContentResolver、GoodsPropertyCatalog、LanguageFamilyCatalog、PropertyDefinitionCatalog、RecipeCatalog
  - 作者工具/校验：ContentAuthoringWorkspace、IContentAuthoringWorkspace、ContentExtensionApi、IContentExtensionApi、ContentPackageLayout、ContentPackageManifest、ContentPackageValidationResult、ContentPackageValidator
- **P2（1 个索引）**：ContentDefinitionIndex（13 个纯定义字典 + 纯 JSON 解析 + InnovationFallbackGroups 注入；ContentRegistry 委托它）

> 总计 20 + 26 + 1 = 47 文件。
