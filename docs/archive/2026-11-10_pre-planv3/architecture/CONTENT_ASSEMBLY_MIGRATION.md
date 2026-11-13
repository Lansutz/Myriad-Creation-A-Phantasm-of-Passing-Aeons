# Content 程序集迁移方案：Modding / ContentRegistry 下沉 Content

> 卡片：**task-117（B2-next 方案：Modding/ContentRegistry 下沉 Content）**  
> 性质：**只读方案**。本卡仅写本文档；未移动/删除/修改任何 `.cs`/`.asmdef`，未跑 Unity/编译/测试，未提交。  
> 快照：2026-11-02 00:5x（+08:00）；基线：`Assets/Scripts/Simulation/MyriadCreation.Simulation.Modding/**`、`Content/MyriadCreation.Content.asmdef`、`Simulation/sim.asmdef`、`ArchitectureContractsTests`。

---

## 0. 结论摘要（给 Lead）

1. **规模**：`Simulation.Modding` 共 **50 个 `.cs` / 2,804 行**。其中 **47 个文件 / 1,856 行**（94%）没有任何 Simulation/World/Culture/Faith/Laws 类型依赖，只依赖 Core、Content 定义、自身类型与 Unity API，**可以直接整体迁入 Content**。
2. **真正的耦合集中在 3 个文件 / 948 行**：
   - `ContentRegistry.cs`（256 行）
   - `ContentRuntime.cs`（650 行）
   - `ContentResolvers.cs`（42 行）
   它们只实际引用 **4 个外部内容形状类型**：`RaceData`（Simulation.State）、`DiscoveryGroups`（Simulation.State）、`CultureData`（Culture）、`BiomeDef`/`BiomeRegistry`（World.Biome）。Faith/Laws 只是未使用的 `using`，不是真实依赖。
3. **不能直接把 Modding 整体搬进 Content**：当前 `Simulation → Content`，若 Content 再引用 Simulation 形成直接环；`Culture/Faith/Laws` 已 `→ Content`，Content 也不能反向引用它们。Content 目标只能是 **Content → Core**。
4. **推荐拆分**（不移动 RaceData/CultureData/BiomeDef，不碰 Save 字段）：
   - Content：**内容定义/索引 + JSON→DTO 加载 + 内容门禁**。
   - Simulation host：**运行时注册表 + 运行时类型映射 + `IContentLookup` 适配器 + 兼容 facade（ContentRegistry/ContentResolvers）**。
   - Core.Contracts：`IContentLookup<TKey,TValue>`、`IContentCatalog`（只引用 Core 类型）。
5. **推荐顺序**：`P0 契约与门禁` → `P1a 纯定义迁 Content` → `P1b 纯基础设施迁 Content` → `P2 JSON 加载/索引拆分` → `P3 运行时 host 适配/Resolvers 瘦身` → `P4 命名空间/门禁收口`。每步单独可编译、可回滚。

---

## 1. 当前快照

### 1.1 程序集与文件规模

| 程序集 | 目录 | 现有 refs | 文件数 | 行数（约） |
|---|---|---|---:|---:|
| `MyriadCreation.Core` | `Assets/Scripts/Core` | — | — | — |
| `MyriadCreation.Content` | `Assets/Scripts/Content` | **Core** | 117 | 3,243 |
| `MyriadCreation.World` | `Assets/Scripts/World` | **Core** | — | — |
| `MyriadCreation.Culture` | `Simulation/MyriadCreation.Simulation.Culture` | **Core, Content** | 7 | — |
| `MyriadCreation.Faith` | `Simulation/MyriadCreation.Simulation.Faith` | **Core, Content** | 2 | — |
| `MyriadCreation.Laws` | `Simulation/MyriadCreation.Simulation.Laws` | **Core, Content** | 32 | — |
| `MyriadCreation.Simulation` | `Assets/Scripts/Simulation`（含 Modding） | **Core, World, Content, Faith, Culture, Laws** | 482 | 12,328 |

`Simulation.Modding`：**50 文件 / 2,804 行**；`ContentRegistry` 被 **85 处 / 27 个文件**引用，`ContentResolvers` 被 **32 处 / 17 个文件**引用；**42 个文件**含 `using MyriadCreation.Simulation.Modding`。

### 1.2 已存在的前置先例

- `Content/MyriadCreation.Content.Definitions/IContentLookup.cs`：**Category-specific** 内容查询接口（文化传统因子/民族组传统 id），由 `Simulation.CultureHost/ContentLookupAdapter` 实现，Culture 程序集只依赖 Content 接口。
- 这说明「Content 定接口 + Simulation host 实现」的模式已经跑通；本次需要把它 **泛化为 Core.Contracts 的 `IContentLookup<TKey,TValue>` / `IContentCatalog`**，并处理 Modding 的 4 个运行类型。

---

## 2. `Simulation.Modding` 逐文件依赖审计

### 2.1 审计方法

- 逐文件读取 `using` 与类型引用；
- 用 State/Systems/World/Faith/Culture/Laws 的公开类型名做词边界扫描，排除注释；
- 外部依赖按「真实类型引用」而非 `using` 计（Faith/Laws 在 3 个文件中是未使用 using，单独标注）；
- 分类：
  - **纯内容定义/基础设施**：无 Simulation/World/Culture/Faith/Laws 类型引用，可直接进 Content；
  - **内容索引/加载（可拆）**：JSON/目录/字典为主，但经 4 个外部类型；
  - **运行时注入/宿主适配**：运行类型字典、Resolvers、静态注入点。

### 2.2 3 个混合依赖文件（文件:行）

#### A. `ContentRegistry.cs`（256 行）

`using`：`:4 Simulation.State`、`:5 World.Biome`、`:7 Simulation.Faith`、`:8 Simulation.Culture`（Faith 实际未用）。

真实外部类型引用：

| 行 | 类型 | 归属 | 用途 |
|---:|---|---|---|
| 30 | `CultureData` | Culture | `CultureContentPack.data` 字段 |
| 39 | `RaceData` | State | `Races` 字典 |
| 67 | `BiomeDef` + `BiomeRegistry` | World.Biome | `Biomes => BiomeRegistry.Overrides` |
| 78 | `RaceData` | State | `Initialize` 重建字典 |
| 92 | `BiomeRegistry` | World.Biome | `Overrides.Clear()` |
| 142 | `RaceData` | State | `TryGetRace` |
| 212 | `BiomeDef` | World.Biome | `TryGetBiome` |

生命周期/静态状态：`:71` static ctor → `Initialize()`；`:73` `Initialize`；`:100` 调 `ContentRuntime.Load`；`:102-105` 初始化 `LanguageFamilyCatalog/PropertyDefinitionCatalog/GoodsPropertyCatalog/RecipeCatalog`；`:110` `Reset`（`:112` 清空、`Initialize` 重建）。属**内容索引 + 运行时类型字典混合**。

#### B. `ContentResolvers.cs`（42 行）

`using`：`:1 State`、`:2 World.Biome`、`:3 Simulation.Faith`（Faith 实际未用）。

| 行 | 类型 | 归属 | 用途 |
|---:|---|---|---|
| 12 | `RaceData` | State | `IContentResolver<int,RaceData> Races` |
| 40 | `BiomeDef` | World.Biome | `IContentResolver<int,BiomeDef> Biomes` |

其余 14 个 Resolver 均为 Content 定义（`InnovationDef/ReligionDef/EthosDef/...`）。属**内容查询 facade，但混入 2 个运行类型**；本身不持有运行时状态。

#### C. `ContentRuntime.cs`（650 行）

`using`：`:4 State`、`:5 World.Biome`、`:7 Faith`（未用）、`:8 Culture`、`:10 Laws`（未用）。

| 行 | 类型 | 归属 | 用途 |
|---:|---|---|---|
| 19/113/282/289/622/626 | `RaceData` | State | `RaceDefsWrapper`、`LoadRaceRoot/LoadRaceDefs`、`Load`、Provider 注册 |
| 79/243/583/590/622/640 | `BiomeDef` | World.Biome | `BiomesWrapper`、`LoadBiomeRoot/LoadBiomes`、`Load`、Provider |
| 254/259 | `CultureData` | Culture | `CultureData.json` 反序列化 |
| 538/550/553/569/579 | `DiscoveryGroups` | State | 原创/回退革新内容的 discoveryGroups 常量 |

`ContentRuntime` 其余部分是：JSON 文件读取、`JsonUtility.FromJson<Wrapper>`、`IContentStore/IContentProvider/ContentProviderCatalog`、路径编排、回退数据。属**JSON→对象加载器**；除 4 个外部类型外可直接进 Content。

### 2.3 47 个纯内容文件（无外部类型依赖，可直接迁 Content）

按类别分组；行数为该文件当前行数。

#### (a) 内容定义 / DTO（20 个）

| 文件 | 行 | 文件 | 行 |
|---|---:|---|---:|
| `CharacterTemplateDef.cs` | 75 | `EffectEntry.cs` | 12 |
| `EthnicGroupDef.cs` | 42 | `EthosDef.cs` | 24 |
| `FamilyTraditionDef.cs` | 28 | `GoodsPropertyArtifact.cs` | 11 |
| `GoodsPropertyEntry.cs` | 14 | `HeritageDef.cs` | 30 |
| `LanguageDef.cs` | 52 | `LanguageFamilyArtifact.cs` | 21 |
| `LanguageFamilyNode.cs` | 18 | `LanguageFamilySource.cs` | 20 |
| `PlaceSuffixDef.cs` | 12 | `PropertyDefinitionArtifact.cs` | 12 |
| `SemanticAuthority.cs` | 27 | `SemanticModifier.cs` | 23 |
| `SemanticTags.cs` | 34 | `TitleDef.cs` | 25 |
| `TitleKinds.cs` | 17 | `TraditionDef.cs` | 93 |

#### (b) 内容索引 / 静态目录（6 个）

| 文件 | 行 | 说明 |
|---|---:|---|
| `ContentQueries.cs` | 52 | 姓名随机查询，依赖 `ContentRegistry.CultureContentPack` |
| `ContentResolver.cs` | 26 | 通用只读 resolver |
| `GoodsPropertyCatalog.cs` | 105 | 静态内容目录 |
| `LanguageFamilyCatalog.cs` | 189 | 静态内容目录 |
| `PropertyDefinitionCatalog.cs` | 82 | 静态内容目录 |
| `RecipeCatalog.cs` | 111 | 静态内容目录 |

#### (c) Provider / Store / Source 基础设施（13 个）

`IContentProvider.cs`(9)、`IContentProviderRegistration.cs`(9)、`IContentStore.cs`(11)、`IContentResolver.cs`(11)、`ContentProviderCatalog.cs`(24)、`ContentProviderRegistration.cs`(24)、`ContentSource.cs`(27)、`ContentSourceCatalog.cs`(21)、`ContentSourceKind.cs`(10)、`ContentStore.cs`(40)、`DelegateContentProvider.cs`(63)、`DictionaryContentStore.cs`(45)、`JsonFileContentProvider.cs`(97)。

#### (d) 内容包创作/校验（8 个）

`ContentAuthoringWorkspace.cs`(81)、`IContentAuthoringWorkspace.cs`(21)、`ContentExtensionApi.cs`(23)、`IContentExtensionApi.cs`(9)、`ContentPackageLayout.cs`(13)、`ContentPackageManifest.cs`(23)、`ContentPackageValidationResult.cs`(27)、`ContentPackageValidator.cs`(113)。

> 合计：20 + 6 + 13 + 8 = **47 文件 / 1,856 行**。它们可作为一个提交整体迁入 Content；保留原 namespace `MyriadCreation.Simulation.Modding` 可先避免 42 个 `using` 改动（P1 采用），P4 再统一改名。

### 2.4 分类结论

| 分类 | 文件数 | 行数 | 去向 |
|---|---:|---:|---|
| 纯内容定义/基础设施 | 47 | 1,856 | **P1 整体迁 Content** |
| JSON 加载 + 内容索引（含 4 个外部类型） | 3 | 948 | P2 拆为 Content loader/index + host adapter |
| 其中真正的运行时注入/Resolvers | 1（Resolvers）+ 2 文件中的运行字典部分 | — | P3 留 Simulation host |

---

## 3. 环检测：为什么必须反转依赖

### 3.1 当前 refs 图

```
Simulation ──► Core, World, Content, Faith, Culture, Laws
Faith ───────► Core, Content
Culture ─────► Core, Content
Laws ────────► Core, Content
World ───────► Core
Content ─────► Core                    ← 目标保持
```

`MyriadCreation.Simulation.Modding` 现在编译在 **Simulation** 程序集内部，引用 `Simulation.State`、`World.Biome`、`Culture` 类型只是同程序集/引用的普通调用。

### 3.2 若直接移动 Modding → Content 会形成的环

| 边 | 结果 | 说明 |
|---|---|---|
| `Content → Simulation` | **硬环** | Simulation 已引用 Content；Content 再引用 Simulation，Unity 编译失败 |
| `Content → Culture` | **硬环** | Culture 已引用 Content |
| `Content → Faith` | **硬环** | Faith 已引用 Content |
| `Content → Laws` | **硬环** | Laws 已引用 Content |
| `Content → World` | 当前无环，但**违反目标** | World 目前只引 Core；若以后 World 引 Content 则成环 |

结论：**Content 只能引用 Core**。Content 代码里不能出现 `RaceData/CultureData/BiomeDef/BiomeRegistry/DiscoveryGroups`，也不能出现 `MyriadCreation.Simulation.Modding` 之外的 Simulation 命名空间。

### 3.3 反转方式

1. Content 的 JSON loader 只产出 **Content DTO**（`RaceContentDto`/`CultureContentDto`/`BiomeContentDto`/Content 定义）。
2. Simulation host 持有运行类型字典，实现 **`IContentLookup`** 适配器，接收 Content DTO 并映射为 `RaceData/CultureData/BiomeDef`。
3. `ContentResolvers` 的 2 个运行类型 resolver 留在 host；其余 resolver 可 delegate Content catalog 或由 host facade 统一暴露。
4. `ContentRegistry/ContentResolvers` 的**旧 public API 保留为 host 兼容 facade**，避免 42 个 using / 85 个引用一次性爆炸。

---

## 4. 目标契约设计（Core.Contracts）

> 只展示方案草案；本卡不创建文件。

### 4.1 `IContentLookup<TKey,TValue>`

```csharp
// Core.Contracts/IContentCatalog.cs（草案）
public interface IContentLookup<in TKey, TValue>
{
    bool TryGet(TKey key, out TValue value);
    IReadOnlyCollection<TKey> Keys { get; }
    IReadOnlyCollection<TValue> All { get; }
}
```

- 泛型参数不落任何 Content/Simulation 类型，Core 可安全引用。
- Content 侧可对 `InnovationDef/ReligionDef/...` 实现 typed lookup；host 侧对 `RaceData/CultureData/BiomeDef` 实现 typed lookup。
- 不同运行类型必须使用**独立 adapter 类**（同一类不能实现两个 `IContentLookup<int,X>`，`All/Keys` 会签名冲突）。

### 4.2 `IContentCatalog`

```csharp
public sealed class ContentKindInfo
{
    public string kind = "";
    public string keyType = "";
    public int count;
}

public interface IContentCatalog
{
    int Generation { get; }                 // Reset/Reload 后递增；便于消费者判断缓存失效
    IReadOnlyList<string> Kinds { get; }
    IReadOnlyList<ContentKindInfo> DescribeKinds();
    bool TryGetRaw(string kind, string id, out object value);
}
```

- `IContentCatalog` 只做**跨类别诊断/门禁/通用查询**；typed 消费仍走 Content 侧 `ContentDefinitionCatalog.Innovations` 或 host adapter 的 typed `IContentLookup`。
- **Content 内部**可有 `ContentDefinitionCatalog : IContentCatalog`，持有 `Dictionary<int,InnovationDef>` 等，并暴露 typed lookup。
- **host** 侧 `SimulationContentHost` 持有运行字典 + 4 个 typed adapter。

### 4.3 Content 侧（目标）

```
Content/
  Definitions/                 ← 现有 Content.Definitions（含 Faith/Culture 定义）
  Modding/                     ← P1 迁入的 47 个纯文件（namespace 可暂保留）
  ContentDefinitionCatalog.cs  ← 新：内容定义索引（IContentCatalog）
  ContentJsonLoader.cs         ← 新：从 ContentRuntime 拆出的 JSON→DTO 加载
  ContentLoadResult.cs         ← 新：Content DTO 结果包
  IContentLoadSink.cs          ← 新：host 映射外部 DTO 的扩展点（接口用 Content DTO 类型）
```

`ContentJsonLoader` 职责：
- 复用现有 `ContentRuntime` 的路径/包装类/回退逻辑；
- 只依赖 System/UnityEngine/Content/Core；
- `Race/Culture/Biome` 先反序列化为 `RaceContentDto/CultureContentDto/BiomeContentDto`；
- 输出 `ContentLoadResult`（Content 定义字典 + 三个外部 DTO 列表）；
- **不引用 GameWorld/CharacterManager/RealmData/BiomeRegistry**。

### 4.4 Simulation host 侧（目标）

新增 `Simulation/MyriadCreation.Simulation.ContentHost/`（仍在 Simulation asmdef，或后续单独 asmdef）：

```
SimulationContentHost.cs           ← 运行时注册表 + 应用 ContentLoadResult
RaceContentLookupAdapter.cs        ← IContentLookup<int,RaceData>
CultureContentLookupAdapter.cs     ← IContentLookup<int,CultureContentPack>
BiomeContentLookupAdapter.cs       ← IContentLookup<int,BiomeDef>
ContentRegistry.cs                 ← 兼容 facade（保留旧 public API）
ContentResolvers.cs                ← 兼容 facade（typed resolvers）
```

映射流程：

```
Bootstrap / GameWorld.Initialize
  → Content.ContentJsonLoader.Load(streamingAssetsPath)
  → ContentLoadResult
  → SimulationContentHost.Apply(result)
       - RaceContentDto → RaceData（写入 host Races）
       - CultureContentDto → CultureContentPack{CultureData}（写入 host Cultures）
       - BiomeContentDto → BiomeDef（写入 BiomeRegistry.Overrides + host Biomes）
       - 其余 Content 定义→ ContentDefinitionCatalog / ReligionCatalog / DoctrinePool
  → ContentRegistry.Cultures/Races/Biomes/... 兼容属性转发上述字典
  → ContentResolvers.* 兼容 facade 转发 typed lookups
```

- `BiomeRegistry.Overrides` 是外部静态注入点：host 负责 `Clear`/填充；Content 不接触。
- `ContentRegistry.Reset`：host 同时清空 runtime 字典 + 重置 Content catalog + `BiomeRegistry.Overrides`；再 `Initialize` 重建。
- `SimulationContentHost` 的 typed adapter 实现 `IContentLookup<TKey,TValue>`；`ContentDefinitionCatalog` 实现 Content 定义 typed lookup。

### 4.5 兼容 facade 设计

- `ContentRegistry` 旧 public 成员（`Cultures/Races/Biomes/Innovations/...`、`TryGet*`、`Initialize/Reset`、`GetRandomName`）**签名不变**，改为 host 静态 facade：
  - 定义类转发 `ContentDefinitionCatalog`；
  - `Race/Culture/Biome` 转发 `SimulationContentHost`；
  - 禁用自身静态构造器自动初始化（改由 Bootstrap/GameWorld 显式调用，避免跨程序集静态初始化顺序问题）。
- `ContentResolvers` 旧 16 个 resolver 保留；`Races/Biomes` 由 host adapter 提供，其余委托 Content catalog。
- 迁移期 `using MyriadCreation.Simulation.Modding` 不变；P4 再决定是否统一 namespace。

---

## 5. 分步迁移计划（每步编译/测试门禁 + 回滚点）

### P0 — 契约与门禁（不改行为，0 移动）

**文件影响**：Core.Contracts + 1 个 EditMode 门禁测试（约 3 文件）。
**内容**：
1. 新增 `IContentLookup<in TKey,TValue>`、`IContentCatalog`、`ContentKindInfo`（Core.Contracts）；
2. 新增 Content 侧 `ContentDefinitionCatalog` 空壳、`ContentLoadResult`/`IContentLoadSink` 接口（Content，不接现有加载）；
3. 在 `ArchitectureContractsTests` 增加门禁：`MyriadCreation.Content.asmdef.references == ["MyriadCreation.Core"]` 且 Content 下无 `using MyriadCreation.Simulation`（按目录扫描）。

**门禁**：Unity 编译；`ArchitectureContractsTests` 全绿；现有 Content/Modding 测试不受影响。
**回滚**：新增文件 + 单测提交整体 revert。

### P1a — 20 个纯定义文件迁 Content

**文件影响**：20 个定义文件移动；Content 文件数 117→137；Simulation 减少 20 文件。
**照旧保留 namespace `MyriadCreation.Simulation.Modding`**，避免 42 个 using 同时改。
**最小首步 3 项**：
1. 新建 `Content/Modding/Definitions/`，移动 2.3(a) 的 20 文件；
2. 确认无 `using MyriadCreation.Simulation.*`（除自身 namespace）并更新 asmdef（Content 自动包含）；
3. 跑 Content 定义/JSON/Architecture 测试。

**门禁**：统一编译；`LawContentJsonTests` 之外的内容测试（`RecipeTests/PropertyCatalogTests/TitleCatalogTests` 等）保持绿；Content asmdef 仍只 Core。
**回滚**：一个纯文件移动提交，`git revert` 即可。

### P1b — 27 个纯基础设施文件迁 Content

**文件影响**：27 文件移动；Content 137→164；Modding 剩余 3 文件（948 行）。
**最小首步 3 项**：
1. 移动 2.3(b) 6 个索引 + 2.3(c) 13 个 provider/store + 2.3(d) 8 个 authoring/validator；
2. 检查 `ContentExtensionApi/AuthoringWorkspace` 仅被 Editor/测试引用，更新 using（如需要）；
3. 跑 `ArchitectureContractsTests` 中 ContentProvider/Store/Authoring 用例。

**门禁**：编译 + Content 相关 EditMode；Content refs 不变；Simulation 对 Content 的引用保持。
**回滚**：第二个纯移动提交。

### P2 — JSON 加载/定义索引拆 Content

**文件影响**：新增 Content DTO 3 个 + `ContentJsonLoader` + `ContentLoadResult/IContentLoadSink`；修改 `ContentRuntime`（拆出纯加载）；`ContentRegistry` 拆定义索引；host 新增 `SimulationContentHost` + 3 adapter；约 8-10 文件改动。
**最小首步 3 项**：
1. 新增 `RaceContentDto/CultureContentDto/BiomeContentDto` 与 `DiscoveryGroups` 常量到 Content（只复制 JSON 字段，不改 Save）；
2. 从 `ContentRuntime` 抽出 `ContentJsonLoader`（纯 System/UnityEngine/Content；处理 Culture CSV/路径/回退内容），输出 `ContentLoadResult`；
3. 新建 `SimulationContentHost`（runtime 字典 + DTO→Runtime 映射 + `BiomeRegistry.Overrides` 填充）和 3 个 typed lookup adapter；`ContentRegistry.Initialize/Reset` 改调 loader→host。

**门禁**：
- `ContentRegistry.Reset/Initialize` 在测试中仍得到 1 个文化/1 个革新/语言列表（`ArchitectureContractsTests.ContentResolvers_ExposeReadOnlyStableContracts`）；
- 内容 JSON 测试、`DiscoveryTests/InnovationSystemTests/MaterialPropertyTests/ResourceDepthTests` 全绿；
- `SaveSystemRoundTripTests`（CultureData 序列化未变）；
- Content 内无 `using MyriadCreation.Simulation`/World/Culture/Faith/Laws。
**回滚**：loader/host 提交；旧的 `ContentRuntime/ContentRegistry` 原件保留到本步验证通过再删（或 feature flag 切换）。

### P3 — 运行时 Resolvers/host facade 收口

**文件影响**：`ContentResolvers` 迁移为 host adapter；`ContentRegistry` 兼容 facade；删除 Content 中残留的 4 个外部类型引用；更新 42 个 using / 85 个 `ContentRegistry` 引用 / 32 个 `ContentResolvers` 引用中受 namespace 影响的部分。
**最小首步 3 项**：
1. `ContentResolvers` 的 `Races/Biomes` 改由 `SimulationContentHost` adapter 提供，其余转 Content catalog；
2. `ContentRegistry` 仅保留 host facade + `ContentDefinitionCatalog` 转发；
3. 将 3 个混合文件从 Content 候选区移回 `Simulation/ContentHost/`（如果 P2 还未物理移动），grep 确认 Content 无外部类型。

**门禁**：编译 + 全量 EditMode/PlayMode（重点：16 个 ContentRegistry 测试文件、16 个测试文件的 Reset/Initialize、`UiDecouplingTests` 的 Content 相关路径）；Content asmdef refs 最终审计。
**回滚**：facade 提交；保留旧 API 可直接回滚。

### P4 — 命名空间/清理/最终门禁

**文件影响**：可选 namespace 改名（47 + 3 文件，42 个 using）；删除空目录；更新文档；不碰 Save 格式。
**最小首步 3 项**：
1. 决定 namespace：保留 `MyriadCreation.Simulation.Modding`（低风险）或改 `MyriadCreation.Content.Modding`（清晰但 42 using 改动）；
2. 若改，批量替换 using/namespace，并保留 `[Obsolete]` 类型转发/别名让外部迁移；
3. 最终门禁：Content→Core-only、Simulation→Content、Content 内无 `Simulation/World/Culture/Faith/Laws` token。

**回滚**：namespace 单独提交；门禁测试单独提交。

---

## 6. 存档 / 静态状态 / 测试影响

### 6.1 存档

- **不改变 Save 字段**：`SaveData.cultures: List<CultureData>`、`RaceDTO` 等路径继续使用运行类型；P2 的 `CultureContentDto→CultureData` 只影响加载，不影响保存。
- `CultureData` 若未来直接迁 Content（非本方案默认），JsonUtility 字段名不变；但需重跑 `SaveSystemRoundTripTests`，确认 `List<CultureData>` 序列化与旧档兼容。
- `ContentRegistry` 是静态内容目录，不入档；存档只需保证加载后内容 id 仍可解析。

### 6.2 静态状态

- `ContentRegistry`：static ctor/`Initialize`/`Reset`/`IsInitialized`；Bootstrap 与 GameWorld 都显式调用 Initialize；迁移后由 host 持有 runtime 字典，Content 只持定义。
- `BiomeRegistry.Overrides`：外部静态注入点；host 负责 Clear/填充；不得由 Content 写。
- `ContentResolvers` 16 个静态 resolver：对象 identity/初始化时机；迁移后必须仍指向同一底层字典（host facade + Content catalog）。
- `LanguageFamilyCatalog/PropertyDefinitionCatalog/GoodsPropertyCatalog/RecipeCatalog/ReligionCatalog/DoctrinePool`：静态内容索引；随 P1/P2 统一由 Content 管理，Reset 生命周期保持一致。
- `ContentRegistry.Initialize` 当前会调 `ContentRuntime.Load` 后初始化四个静态 catalog；拆分后必须保证顺序（先 JSON，再 catalog，最后 host runtime 映射）。

### 6.3 测试影响

| 测试组 | 影响 |
|---|---|
| `ArchitectureContractsTests` | ContentResolvers/ContentSourceCatalog/Authoring/Validator/Provider/Store 等测试；新增 asmdef 门禁 |
| `DiscoveryTests`、`InnovationSystemTests`、`MaterialPropertyTests`、`ResourceDepthTests`、`RecipeTests`、`PropertyCatalogTests`、`TitleCatalogTests` 等 | 均调 `ContentRegistry.Reset/Initialize` 或 ContentResolvers；P2/P3 需回归 |
| 16 个含 `ContentRegistry` 的 EditMode 测试文件 | 静态状态隔离；TearDown 的 Reset 行为必须保留 |
| Save 测试 | `SaveSystemRoundTripTests` 等确认 CultureData/Race 映射不变 |
| Modding 相关 | `JsonFileContentProvider`/ContentStore/ProviderCatalog 等纯测试随 P1 移动但 namespace 不变，应零改动通过 |
| 42 个 `using MyriadCreation.Simulation.Modding` 文件 | P1 保留 namespace 时零改动；P4 改名时才需要批量更新 |

---

## 7. 风险与待决策

1. **最优先风险：静态初始化顺序**。`ContentRegistry` static ctor 自动 Initialize；迁移后 Content 程序集可能先于 Bootstrap 被访问。P0/P3 明确改由 Bootstrap/GameWorld 显式初始化，静态 ctor 只置空。
2. **CultureData 字段很多**：若走 DTO 映射，必须逐字段保真；建议 P2 先只做“加载后立即映射”不持久化 DTO，避免 Save/运行状态双源。
3. **BiomeRegistry.Overrides 的全局可变性**：迁移时必须确保 host Reset 清空，否则测试间污染。
4. **现有 `Content.Definitions.IContentLookup` 与新泛型 `IContentLookup<TKey,TValue>` 同名冲突风险**：建议 P0 将现有接口重命名为 `ICultureContentLookup`（或 `IContentLookup` 放不同 namespace），并保留旧接口 `[Obsolete]` 转发一个版本。
5. **是否直接把 RaceData/CultureData/BiomeDef 迁 Content**：本方案默认不迁（用 Content DTO + host 映射），优点是不动 Save/运行类型；若 Lead 倾向“类型也归 Content”，可作为 P4 之后的独立卡，需 World→Content、Culture/Simulation using 批量调整。
6. **Content asmdef 只 Core**：P0 起加门禁；任何 Content 内 `using MyriadCreation.Simulation/World/Culture/Faith/Laws` 直接测试失败，防止回退。

---

## 8. 推荐的最小首批（下一张实现卡）

1. **P0 三项**：Core 两个接口 + Content 空壳 catalog + `ArchitectureContractsTests` asmdef 门禁。
2. **P1a 首批 3 个定义文件**：`EthosDef/EthnicGroupDef/TraditionDef`（小、纯 Content），验证“Content 容纳 Modding 定义”可行。
3. **P1a 第二批 17 个定义文件**：一次提交完成 20 个定义迁移，P1b 再移 27 个基础设施。

> 这样下一张实现卡只需 3 次小提交即可拿到“Content 可编译 + 门禁 + 47 个纯文件迁移完毕”的阶段性成果；P2/P3 再拆运行类型，风险最低。

---

## 9. W4 收尾：namespace 统一 + host facade 边界（2026-11-02）

### 9.1 已完成

- `Content/MyriadCreation.Content.Modding/**` 的 **47 个 `.cs`** namespace 由 `MyriadCreation.Simulation.Modding` 统一改为 **`MyriadCreation.Content.Modding`**；
- 全仓 **44 个文件**的 `using MyriadCreation.Simulation.Modding;` 按依赖拆分为 `using MyriadCreation.Content.Modding;` 和/或 `using MyriadCreation.Simulation.ContentHost;`（纯定义引用 → Content；host facade 引用 → ContentHost；双方都引用 → 两条；不再使用旧 namespace 的文件移除旧 using）；
- 全仓旧 `namespace MyriadCreation.Simulation.Modding` / `using MyriadCreation.Simulation.Modding` = **0**（`CompletionGateTests.Repo_HasNoLegacySimulationModdingNamespace` 消息文本同步去除旧 token，断言逻辑不变）；
- Content asmdef 仍只 `MyriadCreation.Core`；本次未新增 asmdef 引用（逐文件核对了 Content/Simulation ref：引用 Content 新 namespace 的文件所在 asmdef 均已具备 Content；引用 ContentHost 的文件均已具备 Simulation）；
- **未改逻辑/签名/序列化**：仅 namespace、using、文档与门禁消息文本。

### 9.2 Host facade 命名空间与新边界

4 个 host 文件（目录仍暂留 `Simulation/MyriadCreation.Simulation.Modding/`）namespace 改为 **`MyriadCreation.Simulation.ContentHost`**：

| host 文件 | 保留为 facade 的边界理由（不可迁 Content） |
|---|---|
| `ContentRegistry` | 公开 API 暴露 `CultureContentPack`（含 `CultureData`）、`Races`（`RaceData`）、`Biomes`（`BiomeDef/BiomeRegistry.Overrides`）；迁 Content 会形成 Content→Culture/State/World 硬环。纯定义字典已委托 `ContentDefinitionIndex`。 |
| `ContentRuntime` | JSON→`CultureData/RaceData/BiomeDef` 运行时映射、CSV 名池、BiomeRegistry 填充；运行时类型不可进 Content。 |
| `ContentResolvers` | 16 个静态 resolver 中 `Cultures`/`Races`/`Biomes` 的泛型参数为 `ContentRegistry.CultureContentPack`/`RaceData`/`BiomeDef`；签名不可变（调用方零改动），故保留 host facade。 |
| `ContentQueries` | `GetRandomName(ContentRegistry.CultureContentPack, ...)` 入参为 runtime CultureData/NamePoolData；签名保留，故留 host。 |

**边界条款**：
1. Content.Modding 只承载纯定义/索引/JSON 基础设施；不得引用 Simulation/World/Culture/Faith/Laws 命名空间或运行时类型。
2. 运行时映射、Resolvers、Queries 作为 host facade 留在 Simulation；`Simulation.ContentHost` 可引用 `Content.Modding` 与 Simulation 运行时类型，方向单向。
3. `ContentRegistry` 的静态 ctor / `Initialize` / `Reset` / `IsInitialized`、`ContentResolvers` 16 个属性签名、`ContentQueries.GetRandomName` 签名、`CultureContentPack/NamePoolData` 结构保持；Save 字段不变。
4. 后续若将 4 个 host 文件平移到 `Simulation/MyriadCreation.Simulation.ContentHost/` 目录，只需文件移动（GUID/meta），namespace 已先行定案，不产生新 API 变化。

