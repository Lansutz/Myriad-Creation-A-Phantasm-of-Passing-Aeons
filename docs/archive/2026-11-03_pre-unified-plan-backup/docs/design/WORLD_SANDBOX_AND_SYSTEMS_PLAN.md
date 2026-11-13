# 世界沙盒、物种 DNA、聚落影响力与法令系统：现状审计与实施计划

> 状态：设计稿（先审计后设计）。本文件只做规划，不包含行为改动。
> 目标：把“初始内容可选的世界生成 + 持续自然生成 + 运行中编辑 + 可定义物种/DNA + 聚落影响力 + 法律/法令”接进现有代码，而不是另起一套平行系统。

---

## 0. 结论先行

现有代码已经具备一半以上地基，不需要从零重写：

- 地图生成已有 `TerrainScale.World / Regional` 两种分类，但**区域模式目前只是模板参数不同，生成器仍是同一个球面生成器**，没有局部边界条件。
- 初始内容目前是**硬编码**的：`InitializeDefaultRealms()` 永远创建 3 个政权并瓜分全部陆地；`GenerateSettlements()` 永远按省生成聚落。
- 地图编辑器已有运行时画笔（抬高/降低/平滑/造陆/填海/增删地块），但**没有框选、移动大陆、地壳抬升、放置物种/种族、撤销重做**，且运行时编辑器放在 `Assets/Editor` 特殊目录，打包后不可用。
- DNA 已有雏形：固定 6 个基因座、孟德尔遗传、近亲系数、天赋/缺陷定义；但**基因座是硬编码枚举、只用于有名角色、表达期使用 `Random`、没有物种/基因组定义编辑**。
- 聚落控制已有 `SettlementControlSystem` 和 `AnchorData.influenceRadius`，但半径只按聚落等级、欧氏距离扫描、不看地形/巡逻，也没有“影响强度档位”；地图上有一套圆形影响范围叠加和三角虚控制叠加。
- 法律已有非常重的 `LawComposition`（7 维、29 组、primary/secondary、子选项、模板、下拉框 UI），与你想要的“组只是组 + 每组只选一个具体法律 + 法令需要条件”方向相反，需要迁移而不是加层。
- 自然生成已有 `MapActorManager`（流民/游牧/商队/野兽/土匪/蝗灾）和概率参数，可以直接被沙盒生成配置驱动。

---

## 1. 现有事实表（代码位置 + 真实行为）

| 概念 | 代码位置 | 真实行为 | 备注 |
|---|---|---|---|
| 程序集方向 | `Core`、`World`、`Simulation`、`Infrastructure`、`Rendering` asmdef | Core ← World ← Simulation；Infrastructure 引用三者；World 不引用 Simulation | 新数据模型优先 Core/World，运行时逻辑进 Simulation |
| 地形规模 | `World/Terrain/TerrainTemplateSystem.cs:11` | `TerrainScale.World` / `Regional` | 仅分类和模板预设，尚无独立区域生成器 |
| 地图配置 | `World/Generation/MapGenerationConfig.cs:30` | 世界/局部模板、种子、尺寸、省数、气候、水文参数 | 尺寸只有 Medium/Large/Huge 预设 |
| 局部模板 | 同上 `RegionalTemplate` | 河流流域/半岛/群岛/内海/山脉/盆地/沿海平原/峡湾 | `ApplyPreset` 只改 `PlanetTerrainGenerator` 参数 |
| 地形生成 | `World/Terrain/PlanetTerrainGenerator.cs:54` | 球面噪声+板块 Voronoi+分位数海陆+气候+水文 | 支持 `tile.locked` 保留手绘高程/海陆 |
| 生成管线 | `Simulation/Generation/GenerationPipeline.cs` | Seed→Terrain→Features→Climate→Hydrology→Biomes→Provinces→Settlements→Society | `Provinces` 调用 `GenerateProvinces()`；`Settlements`/`Society` 无条件执行 |
| 初始政权 | `Simulation/WorldState/GameWorld.Initialization.cs:471` | 固定 3 个政权（中原/游牧/海洋），各分 1/3 陆地，给每格加人族人口 | 无开关、无沙盒空世界 |
| 聚落生成 | `Simulation/WorldState/GameWorld.Terrain.cs:243` | `AnchorGenerator` 按省生成城市/港口/堡垒/村庄 | 无“不生成”选项 |
| 单位自然生成 | `Simulation/Actors/MapActorManager.cs:43` | 流民/游牧/商队/野兽/土匪/蝗灾，各有概率与上限 500 | 已有沙盒生成基础 |
| 聚落控制 | `Simulation/Systems/SettlementControlSystem.cs` | 高等级→低等级，欧氏半径扫描，同政权，控制进度受驻军影响 | 半径=`GetInfluenceRadius` 仅按等级 |
| 锚点数据 | `World/Anchor/AnchorData.cs:38` | `influenceRadius`、`controllerAnchorId`、`controlledAnchorIds`、`controlProgress` | 运行时控制字段部分 `[NonSerialized]` |
| 聚落面板字段 | `World/Settlement/SettlementData.cs:30-34` | `controllerAnchorId/controlledAnchorIds/controlProgress/influenceRadius/isControlStable` 代理到 Anchor | 面板可复用 |
| 虚控制渲染 | `World/Anchor/VirtualControlCalculator.cs` | 同政权主要聚落做 Delaunay 三角形，三角形内部=虚控制 | 渲染层工具，不是模拟 |
| 影响范围渲染 | `Rendering/MapRenderer.Texture.cs:301` | 政治模式按等级画半透明圆；`showInfluenceRadius` 默认 false | 目前是“每聚落一圈” |
| DNA 基因座 | `Simulation/Definitions/DnaLocus.cs` | 固定 6 枚举：寿命/智慧/勇武/抗性/外观/天赋缺陷 | 不能数据驱动扩展 |
| DNA 个体 | `Simulation/State/DnaData.cs` | `Dictionary<DnaLocus, LocusPair>` + 突变数 + 近亲系数 | 仅有名角色存个体 DNA |
| DNA 系统 | `Simulation/Systems/DnaSystem.cs` | 随机生成、孟德尔遗传、突变、近亲、表达；表达使用 `UnityEngine.Random` | 表达不确定，不能做基因预览/编辑 |
| 种族数据 | `Simulation/State/RaceData.cs` | 生理参数 + 变革性 + 经济/军事偏好 + DNA 基准 + 基因频率 | 固定字段，不是通用物种定义 |
| 内容定义 | `Simulation/Modding/ContentRegistry.cs` | Base/Mods 的 Race/Dna/Culture/Religion 等 JSON | `RaceDefs.json` 目前为空；`DnaDefs.json` 只有天赋/缺陷 |
| 法律组合 | `Simulation/State/LawComposition.cs` | 7 维、29 组、primary/secondary、子选项、模板 | 组=一道选择题；与目标方向相反 |
| 法律约束 | `Simulation/Systems/LawConstraints.cs:1-120` | 域→族→组三层；组内选项/模板/前置 | 29 组硬编码枚举 |
| 法律 UI | `UI/Panels/GovernmentPanel.cs:36-220` | 按 `LawDimension` 生成下拉框和子项，Apply 写回 `realm.laws` | 需要替换为组→具体法律列表 |
| 存档版本 | `Core/Constants/GameConstants.cs:19` | `SaveVersion = 9` | 新字段必须升版本/做迁移 |
| 运行时地图编辑器 | `Editor/MapEditing/MapEditor.cs` | BrushMode：抬高/降低/平滑/造陆/填海/增删地块 | 位于 `Assets/Editor`，打包不可用 |
| 另一套编辑器 | `Editor/RenderTools/MapEditor.cs` | EditorTool + 省份/聚落画笔 + EditAction | 与运行时编辑器重复，需要收口 |
| 地块字段 | `Core/Data/TileData.cs` | `locked`、`ownerRealmId`、`provinceId` 等 | `locked` 已能保护手绘；政治字段应逐步移出地块 |

---

## 2. 设计原则

1. **一个权威**：同一条数据只能有一个可写权威。
   - 聚落控制关系：`AnchorData.controllerAnchorId` 是权威；关系表只做投影，不另存一份。
   - 法律：新 `RealmLawState.activeLawByGroup` 是权威；旧 `LawComposition` 只做旧档读取/派生。
   - DNA：`GenomeDef` 是定义权威；个体 `GenomeInstance` 只存等位基因引用，不复制定义文本。
2. **聚落是总称，不是类型**：UI 可以写“聚落”“聚落面板”“聚落关系”，但具体类型仍显示村落/集镇/城邑/都会/大都会/堡垒/营地。不要把“聚落”做成一个 `SettlementLevel` 或类型徽标。
3. **影响范围不是抽象一个数**：用“可到达/可巡逻”的通行成本场计算；强度档位只是结果，不直接当输入。
4. **不新增平行系统**：所有生成、刷出、编辑都走现有 `GameWorld`/`GenerationPipeline`/`MapActorManager`/`AnchorGenerator` 入口。
5. **524k 地块不建对象**：地块保持 `TileData[]`；选择/移动/影响场用数组、位图、优先队列；撤销只存改动差异。
6. **编辑要可保存**：手绘地块用 `locked`；玩家自定义物种/基因组写用户 Mod 目录，不写 `StreamingAssets`。

---

## 3. 模块设计

### 3.1 世界创建选项与沙盒生命周期

新增 `WorldSetupOptions`（建议放 `Core/Generation` 或 `World/Generation`，不依赖 Simulation）：

```csharp
public enum InitialContentMode {
    Empty,          // 只有地形/气候/水文
    GeographyOnly,  // 有省/聚落，无政权、无单位
    Minimal,        // 少量政权+聚落+单位
    Standard,       // 当前默认：3 政权、按省聚落
    Custom          // 玩家逐项配置
}

[Serializable]
public class WorldSetupOptions {
    public int seed;
    public MapSizePreset mapSize;
    public TerrainScale terrainScale;
    public WorldTemplate worldTemplate;
    public RegionalTemplate regionalTemplate;

    public InitialContentMode initialContent;
    public bool generatePolities;
    public bool generateSettlements;
    public bool generateInitialUnits;
    public int initialPolityCount;
    public float initialPopulationScale;

    public bool naturalSpawning;         // 运行中自然生成（流民/野兽/新聚落）
    public SandboxSpawnRates spawnRates; // 可按类型开关/倍率
}
```

改造点：

- `GameManager.StartNewGame` 增加 `WorldSetupOptions` 重载；现有签名保留并转成 `Standard`。
- `GameWorld.GenerateTerrainBase` 只保留地形/河流/气候基础；`InitializeDefaultRealms` / `GenerateSettlements` / 初始单位从 base 移出，变成可选项。
- `GenerationPipeline` 的 `Settlements` / `Society` 阶段读选项；`Empty` 时直接标记完成。
- 新增 `SandboxSpawnSystem`（Simulation，订阅 Daily/Weekly）：
  - 驱动 `MapActorManager` 已有概率；
  - 新聚落：按适居度/资源/水源/交通在空 tile 上低频生成营地→村落；
  - 新种群：按物种适居度投放 `PopulationBlock`；
  - 全部受 `naturalSpawning`/`spawnRates` 控制，可暂停。

验收：

- 空世界生成后既无政权、无聚落、无单位；
- 只地形世界可正常进游戏、渲染、保存/读档；
- 自然生成可在设置中关掉；
- 不新增 `GameState`，复用 Loading/Playing。

---

### 3.2 地形生成：世界模式 vs 局部模式

现状：`TerrainScale` 只是模板分类，生成仍走 `PlanetTerrainGenerator`。目标：

- 增加 `ITerrainGenerator`：
  ```csharp
  public interface ITerrainGenerator {
      void Generate(TileData[] tiles, int width, int height, TerrainGenerationContext ctx);
  }
  ```
- `WorldModeGenerator`：包装现有 `PlanetTerrainGenerator`（球面、板块、全球气候）。
- `RegionalModeGenerator`（新，World/Terrain）：
  - 边界条件：`EdgeMask`（哪边是海/陆地/开放边界）、`PrevailingWindDirection`、`RiverInflow/Outflow`、`SeaLevel`、`ElevationRange`；
  - 局部域扭曲噪声 + 模板形状（河谷/半岛/盆地/山脉/峡湾）；
  - 不强制球面坐标；可保留左右 wrap，也可关闭；
  - 气候仍走现有 `CalculateClimate`，但边缘边界作为初始条件（迎风/背风、河流入口）。
- `MapGenerationConfig` 增加局部模式参数：`EdgeMask`、`PrevailingWind`、`BoundaryRiver`、`RegionalReliefScale`。
- `GenerationPipeline.Terrain` 通过工厂选择生成器；`GenerateTerrainWithConfig` 同样走工厂。
- 参数借鉴：
  - Azgaar/FMG：高度图、海岸破碎、温度/降水纬度带；
  - Earth Simulator：风/洋流/湿度连续场，但**不照搬 80×60 规模**。

验收：

- 同一 `TerrainScale` 下种子可复现；
- 局部模式边界条件生效（例如“西侧海、东侧陆地、西风”产生迎风坡降水）；
- 切换模式不破坏省/聚落/存档路径。

---

### 3.3 物种与 DNA：从“角色基因”升级为“可定义生命”

现状问题：

- `DnaLocus` 是 6 个硬编码枚举；
- `DnaData` 用 `Dictionary<DnaLocus, LocusPair>`，JsonUtility 不能直接序列化；
- `DnaSystem.ComputeExpression` 用 `Random` 生成偏移，同一基因型每次表达不同；
- 只服务有名角色；种族字段固定，不能定义动物/植物/怪物；
- 定义只在 `DnaDefs.json` 里放天赋/缺陷，不能编辑基因组。

目标数据模型（Core/World）：

```csharp
public enum SpeciesCategory { Sapient, Animal, Plant, Fungus, Microbe, Construct }

public class SpeciesDef {
    public string speciesId;       // 稳定键，不随语言变
    public string name;            // 本地化键/默认名
    public SpeciesCategory category;
    public string genomeId;
    public string parentSpeciesId; // 进化/变体
    public float bodySize;
    public float lifespanYears;
    public float reproductionRate;
    public float dietHerbivore;    // 0..1
    public float dietCarnivore;
    public float habitatTemperatureMin/Max;
    public float habitatHumidityMin/Max;
    public List<string> behaviorTags;
    public string iconKey/modelKey;
}

public class GenomeDef {
    public string genomeId;
    public List<LocusDef> loci;
}

public class LocusDef {
    public string locusId;
    public int chromosome;
    public int position;
    public float mutationRate;
    public List<AlleleDef> alleles;
    public List<LocusEffect> effects; // 确定性表达表
}

public class AlleleDef {
    public string alleleId;
    public int dominanceRank;      // 0=隐性，越大越显性
    public List<StatEffect> effects;
    public List<string> traits;    // 天赋/缺陷/外观标签
}

public class GenomeInstance {
    public string genomeId;
    public LocusPair[] pairs;      // 按 GenomeDef.loci 索引
    public float inbreedingCoefficient;
    public int mutationCount;
}
```

关键决策：

- **表达确定性**：`ComputeExpression` 不再用 `Random`；由 `AlleleDef.effects` + 显性规则 + 环境修正查表得出，保证同一基因型可预览、可测试、可存档复现。
- **个体 vs 种群**：
  - 有名角色/可追踪单位：`GenomeInstance` 个体 DNA；
  - 人口块/普通种群：只存 `GeneFrequency[]`（按物种/基因组），不逐人存 DNA；
  - 植物/动物/怪物：按 `SpeciesDef` + 种群数量粗粒度模拟，必要时抽稀个体。
- **兼容旧档**：
  - 新默认基因组 `genome_humanoid` 把旧 6 基因座映射为 locus id（length/intelligence/martial/resistance/appearance/talent）；
  - `DnaData` 在 SaveVersion 10 迁移为 `GenomeInstance`；
  - `TalentDefectDef` 保留为 trait 定义的一种，不废弃现有内容。
- **编辑器**：
  - `ContentAuthoring` 增加物种/基因组编辑器：模板、基因座、等位基因、显性、效果、突变率、表型预览；
  - 校验：重复 id、空等位基因、效果引用不存在、物种引用不存在；
  - 保存到用户 Mod 目录（如 `StreamingAssets/Mods/User/...` 或可写 `persistentDataPath/Mods`，按现有 Mod 加载路径定）；
  - 运行中沙盒工具“放置物种/种族”直接引用 `SpeciesDef`。
- **参数借鉴**：
  - CK3 的 trait/基因继承结构（显性/隐性、先天特质）；
  - Earth Simulator 的物种/生态参数（寿命、繁殖、食性、适居温度/降水），但保持我们自己的确定性 DNA 表达。

验收：

- 同一 `GenomeInstance` 多次表达结果一致；
- 孟德尔比例测试（AA×aa→全 Aa；Aa×Aa→1:2:1）；
- 旧档角色 DNA 能迁移且属性不突变；
- 自定义物种/基因组可保存、读回、在沙盒中放置；
- 物种定义缺失时回退默认，不崩溃。

---

### 3.4 聚落影响力与控制

现状：

- `AnchorData.influenceRadius` 是整数半径；
- `SettlementControlSystem.GetInfluenceRadius` 只按等级；
- 扫描是欧氏圆形，不看地形/道路/巡逻；
- 渲染是“每个聚落一圈”。

目标：

- `InfluenceTier` 四档：
  ```csharp
  public enum InfluenceTier { None, Periphery, Contested, Secure, Core }
  ```
  （边缘/争夺/稳固/核心；无影响=0）
- `SettlementInfluenceProfile`（World/Anchor 或 Simulation）：
  - 基础行动力：等级、人口、驻军、行政容量；
  - 日巡逻预算：驻军/巡逻单位每天能覆盖的“通行成本”总量；
  - 通行成本：地形、坡度、河流渡口、道路、关卡、季节；
  - 从聚落做**有预算的 Dijkstra/BFS**，得到每 tile 的 `reachCost`；
  - 按 cost 阈值映射 `InfluenceTier`；
  - 聚落之间：controller 对 target 所在 tile 的 tier 决定有效控制。
- 控制关系：
  - `AnchorData.controllerAnchorId` 仍是权威；
  - 新增只读结果 `EffectiveControl`（0..1，由 tier × 关系合法性 × 距离/巡逻维持）；
  - 低 tier：控制进度增长慢/衰减快，`isControlStable` 不成立；
  - 高 tier：可稳定、可征税、可征募；
  - 与 `SettlementControlSystem` 的 `controlProgress` 对接，不另建一套控制状态。
- 地图表现：
  - 默认不显示单个聚落影响圈；
  - 政治模式叠加层 `showPolityInfluence`：只显示**政权范围**，用所有聚落影响的并集，alpha 随 tier；
  - 不显示“每个聚落一圈”；调试模式可单独看，但不作为正式图层。
- 性能：
  - 每月/每 30 天重算一次，或地形/驻军/道路变化时标脏；
  - 只对主要聚落算，预算限制扩展 tile 数；
  - 结果缓存为 `byte[] tier` + `int[] sourceAnchor`，不建对象。
- 术语：
  - 总称“聚落”；
  - 具体类型仍是村落/集镇/城邑/都会/大都会/堡垒/营地；
  - 关系仍用“上级/下级”，政权归属用“所属”。

验收：

- 同一聚落，平原地形影响范围大于山地/沼泽；
- 有道路/河流渡口时范围扩大；
- 驻军为 0 时影响萎缩，控制进度下降；
- 城镇管辖村庄但村庄处于低影响档时，控制不稳定；
- 政治叠加层只显示政权并集，不出现逐聚落圆圈。

---

### 3.5 法律组与法令

现状问题：

- `LawComposition` 是 7 维 29 组的“选择题组合”，有 primary/secondary、子选项、模板；
- `GovernmentPanel` 直接按 `LawDimension` 生成下拉框；
- 你要求：组只是组；下面具体法律；每组只选一个；法令需要条件。

目标模型：

```csharp
public class LawGroupDef {
    public string groupId;
    public string name;
    public int order;
    public string description;
}

public class LawDef {
    public string lawId;
    public string groupId;
    public string name;
    public string description;
    public int tier;                 // 同组内层级/强度
    public LawRequirements requirements; // 科技/革新/文化/宗教/资源/前置法律
    public LawEffects effects;
    public float enactmentCostTreasury;
    public float enactmentCostPrestige;
    public float upkeep;
}

public class RealmLawState {
    public Dictionary<string,string> activeLawByGroup; // groupId -> lawId（唯一）
    public Dictionary<string,int> enactedDay;
    public float legitimacy;
}

public class DecreeDef {
    public string decreeId;
    public string name;
    public string description;
    public DecreeRequirements requirements; // 财政/权威/合法性/资源/法律前置/宗教
    public DecreeEffects effects;
    public int durationDays;
    public int cooldownDays;
    public float costTreasury;
    public float costAuthority;
}
```

规则：

- 一个 `LawGroup` 下可以有多个 `LawDef`，但 `RealmLawState` 每组只存一个 active；切换要走 `LawReform`（成本、阻力、日期）；
- 法律组不是“选择题”，UI 显示组名 + 当前法律 + 可选法律列表；
- 法令是**临时/一次性**：消耗财政/权威/合法性/资源，满足前置法律与条件，持续 N 天，有冷却；可撤销但返还很少；
- 参数借鉴：CK3 的威望/虔诚/合法性成本与改革阻力；EU4 的行政点/财政/阶层影响力；Imperator 的权威/合法性。具体参数先给可调表，不写死。

迁移：

- 保留 `LawComposition` 作为**旧档读取器**：旧档按现有 primary 值映射到新 `lawId`；
- 新档只写 `RealmLawState.activeLawByGroup`；
- `LawCatalog`/`LawEffects` 改为从 active laws + decrees 生成；
- `GovernmentPanel` 改为：组列表 → 具体法律卡片 →  enact 按钮；删除子选项/模板/次要选择 UI；
- `LawConstraints` 的域/族/组保留给罪名/派系/资格等系统引用，但不再驱动玩家选择题；
- SaveVersion 升到 10，旧字段保留只读兼容。

验收：

- 每组只能有一个 active law；
- 不满足条件时无法颁布，UI 显示缺什么；
- 法令消耗资源、到期失效、冷却生效；
- 旧档能映射并继续运行；
- 无“法律组选择题/次要选项/模板”UI。

---

### 3.6 运行中地图编辑器与沙盒工具

现状：

- 运行时 `Editor/MapEditing/MapEditor.cs` 在 `Assets/Editor` 下，**打包不可用**；
- 只有画笔，没有选择/移动/撤销/物种放置；
- `TileData.locked` 已存在，`PlanetTerrainGenerator` 会尊重它。

目标：

1. **把编辑核心移出 Editor 程序集**
   - `MapEditCommand` / `MapSelection` / `MapEditHistory` 放 `World` 或新 `Gameplay` 运行时程序集；
   - `Assets/Editor` 只保留 Unity 编辑器窗口薄壳；
   - 运行时通过 `MapEditorController`（MonoBehaviour/UI）调用命令。
2. **工具**
   - 选择：矩形/套索/按省/按政权/按生物群系；
   - 移动选区：平移整片大陆/岛屿；wrapX 处理；越界裁剪；
   - 复制/粘贴/填充/擦除；
   - 地壳抬升/沉降：大半径平滑抬升，带 falloff，不是单格 `+strength`；
   - 放置物种/种族（引用 `SpeciesDef`）、聚落、单位、资源；
   - 地形画笔保留（抬高/降低/平滑/造陆/填海）。
3. **编辑后的重算**
   - 移动/抬升后：重算海岸、河流、气候、省界、锚点/聚落；
   - 提供“只改地形/重算全部”两档；
   - 锁定 tile 不覆盖；
   - 大范围操作分帧执行（预算器），避免卡死。
4. **撤销/重做**
   - 只存改动 tile 的旧值 + 被移动的锚点/聚落快照；
   - 命令栈上限，合并连续画笔。

验收：

- 打包后仍能打开运行时编辑器；
- 框选一片大陆移动后，地形、省界、聚落位置一致；
- 抬升产生山脉/高原而不是单格尖刺；
- 撤销能恢复地形/聚落；
- 放置物种/种族后地图和面板可见。

---

## 4. 与现有代码的接口点

| 新模块 | 接现有位置 | 方式 |
|---|---|---|
| 世界创建选项 | `GameManager.StartNewGame`、`Bootstrap`、`SceneFlowController`、`GameWorld.InitializeWorld` | 增加重载；旧签名转默认选项 |
| 初始内容开关 | `GameWorld.GenerateTerrainBase`、`GenerationPipeline.ExecuteStage` | 把 `InitializeDefaultRealms`/`GenerateSettlements` 从 base 拆出，按选项调用 |
| 自然生成 | `MapActorManager.Tick`、`SimulationScheduler` | 新 `SandboxSpawnSchedule`；复用已有概率/上限 |
| 区域生成器 | `GenerationPipeline.Terrain`、`GameWorld.GenerateTerrainWithConfig` | `ITerrainGenerator` 工厂按 `TerrainScale` 选择 |
| 物种/DNA | `RaceData`、`DnaData`、`CharacterData.dna`、`ContentRegistry`、`SaveSystem` | 新 `SpeciesDef/GenomeDef`；旧 `DnaData` 迁移；内容 JSON 扩展 |
| DNA 编辑器 | `Editor/ContentAuthoring` + `ContentRuntime.Load` | 新编辑窗口；写用户 Mod 目录；运行时重载/校验 |
| 聚落影响 | `AnchorData.influenceRadius`、`SettlementControlSystem`、`MapRenderer.Texture`、`MapLayerConfig` | 用 `InfluenceField` 替换半径扫描；渲染只保留政权并集 |
| 法律/法令 | `RealmData.laws`、`LawComposition`、`LawCatalog`、`LawEffects`、`GovernmentPanel` | 新 `RealmLawState` 权威；旧组合只读迁移；UI 重写 |
| 地图编辑 | `Assets/Editor/MapEditing/MapEditor.cs`、`TileData.locked`、`GameWorld.PaintTerrain` | 移核心到运行时；命令 + 历史；重算管线 |
| 存档 | `GameConstants.SaveVersion`、`SaveSystem`、DTO | 升 10；新字段 DTO；旧档迁移测试 |

---

## 5. 分阶段实施（每阶段可独立验收）

### Phase 0：契约与审计（本文件）
- 完成事实表、接口、迁移映射。
- 不写行为代码。

### Phase 1：创建选项 + 沙盒自然生成
- `WorldSetupOptions`；`Empty/GeographyOnly/Minimal/Standard`；
- 拆出 `InitializeDefaultRealms`/`GenerateSettlements`；
- `SandboxSpawnSystem` 驱动现有 `MapActorManager`；
- 新游戏 UI 增加“初始内容/自然生成”选项。
- 测试：空世界、少内容世界、自然生成开关、存读档。

### Phase 2：局部地形模式
- `ITerrainGenerator` 工厂；
- `RegionalModeGenerator` + 边界条件参数；
- `MapGenerationConfig` 局部参数；
- 测试：边界/风/河流入口、种子复现。

### Phase 3：运行时编辑器核心
- 迁移 `MapEditor` 核心出 Editor 程序集；
- 选择/移动大陆/地壳抬升/撤销；
- 分帧执行 + 重算选项；
- 测试：移动、抬升、撤销、打包构建（至少编译）。

### Phase 4：物种与 DNA
- `SpeciesDef/GenomeDef/GenomeInstance`；
- 确定性表达 + 旧档迁移；
- 内容编辑器 + 校验；
- 沙盒放置物种/种族；
- 测试：孟德尔、确定性、迁移、内容 round-trip。

### Phase 5：聚落影响力
- `InfluenceTier` + 预算化 Dijkstra + 地形/道路/巡逻；
- 接入 `SettlementControlSystem` 的有效控制；
- 政治叠加层只显示政权并集；
- 测试：平原/山地、驻军、道路、控制稳定性。

### Phase 6：法律/法令重构
- `LawGroupDef/LawDef/RealmLawState`；
- 旧 `LawComposition` 迁移；
- `GovernmentPanel` 重写；法令系统；
- 测试：每组唯一 active、条件/成本/冷却、旧档迁移。

### Phase 7：存档/UI/术语收尾
- SaveVersion 10；全量存读档；
- UI 总称统一“聚落”；
- 文档/参数表；
- 全量基线对比。

---

## 6. 参数建议（先可调，不锁死）

- **聚落影响力**：
  - tier 阈值：Core ≥0.75、Secure ≥0.5、Contested ≥0.25、Periphery >0；
  - 基础行动力：`等级×20 + 人口^0.5×2 + 驻军/100`；
  - 地形成本：平地 1、丘陵 2、山地 4、沼泽 5、森林 2、渡口 3、道路 0.5；
  - 驻军为 0：影响范围衰减 50%/月；有巡逻：恢复；
- **法律**：
  - 颁布成本：财政 `100×tier`、威望 `50×tier`、合法性 `-5×tier`；
  - 维持：财政 `tier×2/月`；
  - 改革阻力：既得利益阶层影响力 × 法律强度；
- **法令**：
  - 成本：财政/权威/合法性/资源；持续时间 90–720 天；冷却 180–1080 天；
  - 条件：前置法律、科技/革新、宗教/文化、地形/战争状态；
- **自然生成**：
  - 聚落：每 30 天按适居度概率；上限按省；
  - 物种：按 `SpeciesDef` 适居度/食物链；上限按 tile 承载；
  - 全部可倍率/开关。

---

## 7. 风险与待定

1. **旧档迁移**：法律/DNA 是破坏性结构变化，必须保留只读兼容；建议 SaveVersion 10 + 迁移测试。
2. **影响场性能**：524k tile 上做 Dijkstra 必须预算化/缓存；只在主要聚落、每月重算。
3. **编辑器重算**：移动大陆后省/聚落/锚点会失效；需要明确“保留关系但重算位置”还是“重建”策略。
4. **物种范围**：所有生物可定义，但植物/动物不必逐个体 DNA；按物种+种群频率模拟，避免 524k 对象。
5. **法律旧 UI**：直接删除会让旧存档玩家困惑；先保留只读旧面板入口一版，再移除。
6. **术语**：总称“聚落”，但具体类型不显示“聚落”作为类型；`AnchorData` 是内部词，不进 UI。

---

## 8. 实施状态（2026-10-30）

已落地并通过测试：

- 初始内容默认 0：`WorldSetupOptions` 默认 `Custom`，政权/聚落/单位数量均可填 0；自然生成默认开启。
- 初始生成 gate：`InitializeDefaultRealms` / `GenerateSettlements` / `GenerationPipeline` / 异步入口全部按选项执行；`initialSettlementCount` 支持 `0/-1/N`；`initialUnitCount` 生成中立地图单位。
- 遗传学核心：`Simulation/Genetics`（孟德尔分离、Haldane 重组、突变、X/Y/线粒体遗传、加性-显性表型、致死纯合、HWE）。
- 聚落影响力：`SettlementInfluenceSystem`（地形/道路/河流成本的有预算 Dijkstra、4 档强度、写入控制关系）。
- 法律/法令核心：`Simulation/Laws`（组→具体法律、每组唯一 active、条件/成本、法令时限/冷却/效果）。
- 地图编辑核心：`World/Editing`（选择、移动选区、抬升、撤销/重做，只存差异）。
- 局部地形边界：`RegionalTerrainShaper`（西海/东海/北海/南海/岛屿边缘海洋化）。
- 异步省生成 + Loading 进度 + 自适应帧预算（前序）。

仍需接线（未完成）：

- ~~新游戏 UI 的政权/聚落/单位数量输入~~（已完成：MainMenuUI 创建世界面板，默认 0）
- ~~运行时地图编辑输入~~（已完成：Rendering/Editing/RuntimeMapEditingController，F2 开关、框选、Shift+方向移动、+/- 抬升、Ctrl+Z/Y）
- ~~DNA 与 RaceData/CharacterData 迁移~~（已完成基础接线；角色生育改走 GeneticsEngine；旧 DnaData 适配）
- ~~法律新 UI、旧 LawComposition 迁移、RealmData/DTO~~（已完成：GovernmentPanel 重写；LawCompositionMigration；RealmDTO 保存）
- ~~聚落影响力政治图层并集~~（已完成：MapRenderer 只画政权并集，不逐聚落圆圈）
- ~~SaveVersion 升级~~（已升 10；WorldSetupOptions/Race 基因字段/法律状态入档）
- 仍待：完整区域边界条件生成器（当前是边缘塑形）、玩家自定义物种/基因组编辑器、法令撤销 UI、旧角色 DNA DTO 全量存档。

## 8. 建议的下一步

先做 **Phase 1（创建选项 + 沙盒生成）** 和 **Phase 4 的数据契约（Species/Genome）**，因为它们不改现有法律/影响逻辑，风险最低，且能立刻验证“可选初始内容”和“可定义物种”的方向。

确认以下三点后即可开工：

1. 初始内容默认档位：`Standard`（3 政权）还是 `Minimal`（1 政权+少量聚落）？
2. 物种范围：只做智慧种族 + 动物/植物粗粒度，还是动物也逐个个体？
3. 旧法律面板：保留只读一版还是直接替换？


---

## 9. 2026-10-30 双轮补充：嵌套系统与性染色体

### 单位分类
- `UnitCategory`：Military / Trade / Migration / Bandit / Wilderness；
- `MapActor` 与 `MapUnit` 均带 category，用于决定加载哪套模块/UI；
- 具体行为仍由 `TransportUnitType` / `MapActorType` 表达，分类不替代类型。

### 法令传递（不是瞬移）
- `DecreeOrder`：法令沿行政区划父→子下发；
- `DecreePropagationSystem`：
  - 路程天数按区块中心距离计算；
  - 到达后由官员 `management/conspiracy`、腐败、地方动荡、合法性决定 Enforced / Delayed / Refused / Profiteering；
  - `GameWorld` 每日推进并结算；UI 颁布法令后自动下发。
- 三层状态：`RealmLawState`=已固化法律；`ActiveDecree`=生效法令；`DecreeOrder`=传递中的命令。

### 性染色体系统
- `SexDeterminationSystem`：XY / ZW / XO / Haplodiploid / Environmental / Hermaphroditic / Multiple；
- `SexChromosomeSystemDef` 可定义异配/同配染色体；
- `GeneticSex` 扩展：Intersex / Hermaphrodite / Worker / Queen / Drone / Sterile；
- `GenomeInstance.ploidy` 支持单倍体雄蜂；
- 测试：ZW 女儿 W/儿子 Z、XO 儿子无父源 X、单倍二倍体、环境性别、雌雄同体。

### 基因隔离 / 杂交
- `ReproductiveIsolation`：None / Prezygotic / Postzygotic / Genetic；
- `introgressionRate`：无生殖隔离但基因隔离时父方基因渗入比例，0=后代几乎只有母种基因；
- `GeneticsEngine.ReproduceHybrid` 按渗入率保留父方基因；
- `GeneticCompatibility.Evaluate` 返回可杂交性/渗入率/后代归属。

### 外观与身体部件
- `AppearanceChannel`：皮肤/毛发/眼睛/血液色素、眼睛结构、毛发结构、皮肤纹理、体型/身高；
- `PigmentDef` / `AppearanceStyleDef`：血液色素（血红蛋白/血蓝蛋白/血绿蛋白等）、眼睛显色模型（structural/pigment/reflective/compound/emissive）；
- `GeneticsAppearance` 把基因型映射为皮肤/毛发/眼睛/血液颜色；
- `CharacterData.appearance` 缓存派生外观；
- 人类基因组已把 appearance 位点映射到皮肤/毛发/眼睛色素通道。

### 内容管理器
- `GeneticsContentManager`：注册 GenomeDef / SpeciesDef / AppearanceStyleDef；JSON 导入导出；引用校验；
- 默认内容包可由 `CreateDefault()` 生成；
- 仍未做游戏内编辑器 UI。

### 验证
- 全量 EditMode：553 测试 / 545 过 / 8 失败（既有无关失败）。


## 10. 单位细化与传令官实体化（2026-10-30）

- 大类严格保持五类：`Military / Trade / Migration / Bandit / Wilderness`。
- 新增 `UnitSubcategory`（OrdinaryMigrant/Refugee/Nomad/Courier/Envoy/TradeCaravan/SupplyCaravan/Bandit/Predator/LocustSwarm/RodentPlague…）；
- `UnitSubcategoryRules` 决定 `UnitAICategory` 与 `UnitInteraction`；
- “动物灾害”条目删除，统一为 `Wildlife`（小类决定捕食/蝗群/鼠患等 AI 与互动）；
- `TransportUnit` 增加粮草 `provisions`、经费 `money`、每日消耗、短缺反馈、拆分分遣队 `DetachSubunit`；
- `UnitProvisioning`：组建单位要粮/钱/马/车，经费来源由 `UnitFundingSource` 决定（个人/家族/城库/省库/国库）；
- 法令传令官改为真实 `MapActorType.Courier` 移动，按距离/经费派出，到达才送达；执行力度连续化（`PartiallyEnforced`），不再只有拖延/拒绝。
- 全量 EditMode：560 测试 / 552 过 / 8 失败。


## 11. 背包/单位健康/聚落传令修正（2026-10-30）

- 移除单位上的 `provisions` / `money` 专用字段：背包统一为 `CargoEntry` 贸易品货物；
- 货币作为货物进入背包；口粮消耗按 `GoodsDef.category`（Food/Crop/Livestock）从 cargo 扣除；
- 单位健康低于阈值持续一段时间 → 数字人口减员；有名成员按各自 `CharacterData.health/抗性` 独立判定，不会随数字人口一起掉；
- 传令目标改为**聚落**：按 `SettlementData.controlledAnchorIds` / `controllerAnchorId` 管辖关系下发，`DecreeOrder.targetAnchorId`，必须走到目标聚落所在地块；
- 区划不再作为传令目标；区划只用于找对应官员；
- 经费来源由法律组决定：地方城市特许/选举→城库，地方世袭/本地税→省库，国家税/常设军制→国库，不再用集权度；
- 全量 EditMode：562 测试 / 554 过 / 8 失败。


## 12. 骑砍式“进入聚落”修正（2026-10-30）

- 单位位置权威改为聚落：`MapActor.currentSettlementAnchorId` / `targetSettlementAnchorId`；
- 传令官到达的是**目标聚落**（`targetAnchorId`），不是裸地块；
- `CharacterData.currentSettlementAnchorId/currentTile/isPublicFigure`；
- 队伍成员进入聚落时同步角色位置；
- `CharacterManager.CreateOfficial`；`AdminDivision.holderCharacterId` 空缺时按需物化为真实角色（懒物化）；
- `CharacterLocationService`：角色可进入任何聚落；UI 只显示观察者“知道”的人物（公众人物跨政权可见，普通人默认同政权可见）；
- `MapActor.leaderCharacterId` + `SetLeader`；队伍 AI 与领袖分离，领袖可替换；
- 物资仍走贸易品 cargo；单位组建/传令消耗真实货物与经费；
- 全量 EditMode：564 测试 / 556 过 / 8 失败。


## 13. 团队分线：UI/角色AI/旧代码/存档/金库寻路（2026-10-31）

- 聚落 UI：新增 `SettlementPeopleText`，接 `CharacterKnowledgeRegistry`；地块详情/区划详情显示“已知且在该聚落”的角色，未知人物不显示，点击进角色面板。
- 角色 AI：`UnitAIFactory.CreateForActor/CreateForLeader`；领队 `CharacterAICategory` 优先，无领队才按 UnitSubcategory 回退；领队替换即时重绑；compassion/boldness/greed/military/prowess/health 进入目标与行军权重。
- 旧 UI 清理：删除 50 个零引用文件（旧编辑器 UI 栈、死组件、15 个 Interface prefab），删除 Main.unity 的 StartMenuPanel/NewGamePanel/SettingsPanel/MapModeBar 4 个对象；空目录清理；编译通过。
- 存档：新增 Character/TransportUnit/MapActor/MapUnit/Family/Office/Knowledge DTO；SaveVersion 11；读档重建不重复造人；AdminDivision holder/Army 实体仍未入档。
- 金库/寻路：TreasuryVault/TreasuryLedger + TradeCenter inventory 视图；UnitProvisioning 优先从金库货物支付；UnitPathfinder 接地形 A*，MapActor 逐格移动，无路不瞬移。
- 全量：564 测试 / 556 过 / 8 既有失败。

## 14. 恢复后测试对齐与存读档 v12（2026-10-31）

反编译恢复 `Core/World/Simulation` 之后，先把测试基线拉回全绿，再补存读档缺口。

### 测试对齐

- `ContentRegistry` 增加静态初始化、`ContentRuntime` 修正 StreamingAssets 根路径；
- 旧 `Innovations.json` 补默认 `discoveryGroups`，补 `mining.surface/shaft/deep`、`heat.high` 等能力默认值；
- 区划详情 UI 只显示人类可读头衔名（`TitleDef.semantic`），不再泄漏 `title_gov_general` 这类内部键；
- 旧档键迁移：实测 **`JsonUtility.FromJson` 不识别 `[FormerlySerializedAs]`**（旧键 `settlements` 不会映射到 `habitation_sites`），
  因此 `SaveJsonMigration` 在 `GameSaveSystem`/`MapSaveSystem` 读档入口显式迁移 `burgs/settlements → habitationSites`、
  `anchorId/settlementName/settlementCategory/settlementForm → 规范字段`；
- 全量 EditMode：**298/298 通过**（此前 290 条里长期有 1 条旧档映射失败）。

### 存读档 v12：省份 / 聚落 / 营地

- `SaveData` 新增 `provinces`、`habitationSites` 两段；`SaveVersion` 由 11 升 **12**；
- 新增 `SettlementPersistence`：
  - `CaptureProvinces/RestoreProvinces`：省名、省中心、成员地块；
  - `CaptureSites/RestoreSites`：聚落（城墙/建筑块树、设施、区划、市场库存、经济成分、人口/财富/守军/建造进度）+ 营地（类型、补给、士气、永久度、属性、归属）；
  - 还原**就地清空并填充**现有字典：`InitializeSubsystems` 时子系统已捕获这些字典实例，换实例会让系统指向旧容器；
- 读档顺序：地块 → 子系统重建 → 省 → 聚落/营地 → 角色/家族/官职/地图单位（单位要引用聚落锚点，顺序不能反）；
- 旧档（无这两段）按空世界处理，不残留上一局聚落；
- 验证：`SettlementPersistenceTests` 3/3（省份/聚落 JSON 往返、营地还原、旧档空世界）。

### 存读档 v12：行政区划树

- 新增 `AdminDivisionSaveData`（`divisionId/realmId/level/name/parentDivisionId/childIds/tiles/titleId/holderCharacterId`），
  挂在 `RealmDTO.adminDivisions`；
- `AdminDivision` 的 `tiles` 是 HashSet、`childIds` 是 List，JsonUtility 不能直接序列化，统一转数组；
- 读档后区划树、治理者绑定、法令下发路径都在；`EnsureAdminDivisions` 只在 `Count == 0` 时懒生成，不会覆盖入档结果；
- 验证：`AdminDivisionPersistenceTests` 2/2（树/治理者/辖境往返、旧档空表）。

### 存读档 v13：军队实体

- 新增 `ArmySaveData`（编制 `unitCounts` 拍平、位置、`state`、组织/士气/补给/训练、行军路径/目标/进度、嵌套 `TransportUnitSaveData` 队伍与货物）；
- 新增 `ArmyPersistence`：`CaptureArmies` / `RestoreArmies`（就地清空填充 `world.armies`，返回下一个 armyId）/ `RegisterArmyViews`；
- 地图单位里的军队视图**不入档**，读档后按军队实体重新登记（`MapUnitRegistry.RegisterArmy`，幂等），避免两份数据打架；
- 新增 `GameWorld.AdoptRuntimeIdCounters(nextWarId, nextArmyId)`：读档后自增 ID 从存档最大值之后继续，修掉"读档后新建军队/战争撞 ID"的隐患；
- 验证：`ArmyPersistenceTests` 3/3（状态 JSON 往返含队伍货物、视图派生、旧档空军队表）。

### 仍未入档

- 旧角色 DNA DTO 全量存档。

### 发现的待决问题（未改）

- 单位货物把 `goodsId == 0` 当"空"，但内容表里 **0 是粮食**：
  - `TransportUnit.AddCargo` / `TreasuryVault` 用 `goodsId <= 0` 直接丢弃；
  - `GameWorld.FindFoodGoodsId()` 对默认内容表返回 **0**（粮食），`UnitProvisioning.Equip` 又用 `foodGoodsId > 0` 判断，结果"国库付了口粮、队伍背包里却没有粮食"；
  - 现有测试全部用 1/2/10 等非零食物 ID，绕开了这条路径。
  建议：把"未指定"换成显式哨兵（-1），或让选粮逻辑跳过 0；需要先定 0 号粮食是否允许进背包。

## 15. 深度检修（2026-10-31）：4 线只读审计 + P0 修复

### 审计团队

拉 4 名只读审计员并行（约束：不改 `Assets/**`、不跑 Unity/编译/测试，只写各自报告）：

| 线 | 报告 | 主要结论 |
|---|---|---|
| A 存档/持久化 | [docs/audit/A-save-persistence.md](../audit/A-save-persistence.md) | 5 条 P0：读档不初始化、AI 缺失、InnovationTree 未入档、MapUnitRegistry id、读档非原子 |
| B 架构/恢复残留 | [docs/audit/B-arch-recovery.md](../audit/B-arch-recovery.md) | 旧 `CivilizationEvolution.Runtime` 程序集与新程序集 **296 个同名类型**、场景仍挂旧 MapRenderer/MapEditor |
| C 经济/补给 | [docs/audit/C-economy-provisioning.md](../audit/C-economy-provisioning.md) | 4 条 P0：0 号物产 sentinel、TryFund 按值+Equip 覆盖、Personal 凭空造钱、口粮公式必缺 |
| D 测试质量 | [docs/audit/D-test-quality.md](../audit/D-test-quality.md) | 绿基线来自未提交新套件、SaveGame→LoadGame 零覆盖、1 条确证断言放宽 |

### 本轮已修 P0

1. **世界生成崩溃**（冒烟发现）：军事隘口/河谷/荒漠走廊上的聚落初始定型走了"已有聚落改型"规则，`TryTransition(Sedentary→FortifiedOutpost)` 被拒后抛异常，建城/圈地/生成聚落都会崩世界生成。
   修法：`SettlementTypologySystem.DeriveSettlementForm` 对新建聚落直接定型据点（不走转换规则）；`AnchorGenerator` 用派生后的 `siteMode` 算等级；显式 City 不被派生结果降级。
2. **读档不初始化**（A-P0-1/2/P1-3）：新增 `GameWorld.PrepareLoadedWorld()`，读档末尾补齐 `unitDefs`、`FaithSystem`、每政权 `AIController`。
3. **MapUnitRegistry 自增 id 未续接**（A-P0-4）：新增 `AdoptNextId`，读档后推进到最大值之后。
4. **读档非原子**（A-P0-5）：`LoadGame` 先快照 Relation/Contract/DynamicGoods 全局表，失败时回滚 + `DestroyImmediate` 半成品世界。
5. **0 号物产 sentinel**（C-P0-1）：0 保留为粮食，无效改 -1；`AddCargo/RemoveCargo`、`TreasuryVault`、`TreasuryLedger`、`UnitProvisioning`、`UnitFunding`、`UnitProvisioningCost`、`FindFoodGoodsId/FindCurrencyGoodsId` 全链路统一。
6. **付款/发货两个事实源**（C-P0-2）：`Equip` 只信 `funding` 的物产 id，不再用原始 cost 反向覆盖；选不到物产时 `TryPay` 明确失败。
7. **个人出资凭空造钱**（C-P0-3，过渡修法）：Personal/Household 尚无真实账户，改为**显式降级到国库**并把 `funding.source` 改成 `NationalTreasury`，不再"不扣款却记账发货"。
8. **传令官口粮必然短缺**（C-P0-4）：`ForCourier` 改为"每日需求 2（1 人 + 1 马）×（路程天数 + 3 天缓冲）× 1.15"。

### 新增测试

- `WorldGenerationSmokeTests` 2 条：标准初始内容在世界生成不崩（含据点派生）；`SaveGame→LoadGame` 全链路（表数量 + 锚点 + unitDefs/AI 补齐 + 军队 ID 续接）。
- `ProvisioningLedgerTests` 5 条：0 号粮食进背包/金库、`TryFund` 付粮并发货、`Equip` 只信 funding、缺粮食物产明确失败、口粮公式覆盖行程。

### 验证（Round 1）

- 编译 `COMPILE_OK`
- 全量 EditMode：**305/305 通过，0 失败**

### 深度检修 Round 2：复核修正 + 场景/asset 修复

**复核（verifier，报告 [VERIFY-round1.md](../audit/VERIFY-round1.md)）** 抓出 2 个 FAIL，已修：

1. **R2（A-P0-4 实际未生效）**：`SaveSystem` 里 `RegisterArmyViews` 在 `AdoptNextId` 之前，`RestorePersistentEntities` 的 `Clear()` 把 `_nextId` 重置为 1，军队视图仍从 1 分配并覆盖已还原的商队/难民条目。
   修法：先 `AdoptNextId(max+1)` 再登记军队视图；`WorldGenerationSmokeTests` 增加"军队 + 地图单位共存"断言（`MapUnitRegistry.Count == armies + actors`）。
2. **R1（P0-3 修法把法令功能打死）**：Personal/Household 降级国库后，国家金库**没有实物粮草来源** → 传令官必然"国库粮草货物不足"发不出。
   修法：`TryPay` 在个人出资时先试国库、失败再回退**区域库**（生成局城库 `inventory[0]=1000` 有粮），成功后把 `funding.source` 回写成真实付款方；城库/省库降级国库时同样回写 source（R4）。
3. **R3（读档回滚不完整）**：回滚快照补上 `MapUnitRegistry`（新增 `Restore(snapshot)`）；`CharacterKnowledgeRegistry` 需要角色管理器才能还原，暂留 P1 并记录。
4. **R5（结构体默认 id）**：money-only 重载显式传 `InvalidGoodsId`，避免把粮食当货币扣。

**场景 / asset 恢复（B 线 P0）**：

- 工作区 `Main.unity` 曾被未跟踪的 `_Recovery/0.unity` 覆盖（HEAD 80 个 TMP 组件 → 0 TMP + 17 个旧 `UI.Text`，3 个组件 GUID 悬空、2 个被改绑旧程序集）。
- 修法：从 HEAD 恢复 `Main.unity`，并把 4 个脚本 GUID 重绑到当前 `.meta`（Bootstrap / GameManager / GameWorld / 新 `Rendering.MapRenderer`）。
  实测：`roots=8 comps=642 missing=0`、TMP 组件 80 个、`UIManager` 23 个 TMP 字段只有 2 个为空（恢复版是 23 全空）。
- `TMP Settings.asset` 的 linebreaking 两处 GUID 指向被重生成前的旧 meta → `MissingReferenceException`；已按当前 `LineBreaking Leading/Following Characters.txt` 的 GUID 修正。
- `MapRenderer` 在 `world.config` 为空时 NRE（场景里 GameWorld 尚未开局）：`ClampCameraToMap` / 配色 / 游牧图层全部加 `config != null` 守卫。

**验证（Round 2）**：编译 `COMPILE_OK`；全量 EditMode **306/306**。

**Round 2 复核（`docs/audit/VERIFY-round2.md`）** 又抓出 3 个问题，Round 3 已修：

1. **R1 仍未真的修好**：生成局区域库只播粮（`inventory[0]`）、国家金库只有货币 → "国库有钱无粮 + 区域库有粮无钱"两条路都付不出，传令官依旧发不出。
   修法：`InitializeTradeCenters` 同时播粮和**货币**（用与结算相同的 `TreasuryLedger.SelectCurrencyGoodsId`，避免"播铁结算找金"）；新增 `GeneratedWorld_RegionalVault_SeedsFoodAndCurrency` 测试。
2. **PlayMode "地图可见"是假绿**：`-nographics` 下相机渲染是单一底色，写死的背景色阈值让断言恒真。
   修法：无图形设备时明确跳过像素断言（只证明场景/对象/渲染链不崩）；有图形设备时断言颜色多样性（unique>8）、亮度层次（range>0.02）与非背景比例。
3. **R2 新断言不敏感**：交换测试里 SpawnActor 与 CreateArmy 的顺序，让"军队视图覆盖商队"的旧 bug 真能让 Count 断言失败。

**Round 3 验证**：全量 EditMode **307/307**；PlayMode 有图形设备运行 `Logs/playmode-gfx-results.xml` **1/1 PASS**，实测 `unique colors 10 / 非背景 12/64 / 亮度 0.145~1.000`（截图 `D:/map_verify.png`，地图面已真实渲染；颜色偏白，下一步查贴图管线）。

### Round 4：存读档 v14（革新树入档）+ 驻军 NRE + 地形配色验证

- **革新/科技树入档（A-P0-3，SaveVersion 14）**：新增 `InnovationTreeSaveData`（政权革新集合/研究点/当前研究/已发现/进度含增益明细/资源累计产出），
  `InnovationTree.CaptureState()` / `RestoreState()` 就地还原；读档后科技不再清零。
  验证：`InnovationPersistenceTests` 2/2（完成一项革新后 SaveGame→LoadGame 仍在；DTO JSON 往返保留当前研究与研究点）。
- **驻军统计每日 NRE（C-P1-7）**：`SettlementControlSystem.UpdateGarrisonInfo` 原来 `army.GetTotalManpower(null)`；
  现在 unitDefs 由 `SettlementControlRuntime` 从 `world.unitDefs` 传入，`Army.GetTotalManpower/CalculateCombatPower/DailyTick` 对 null 返回 0 而不抛异常。
  验证：`ArmyAndGarrisonSafetyTests` 2/2（null 安全 + 驻军人数真的计入军队人力）。
- **PlayMode 视觉验证加强**：截图前显式切到 `MapDisplayMode.Terrain`（此前默认被 UI 切到宗教/教统，缺数据时整幅纯白会被误读成管线坏了）；
  地形模式下实测 unique 颜色 10、亮度 0.145~1.000、非背景 12/64，截图确认地形条纹已真实渲染。

**验证**：全量 EditMode **311/311**；PlayMode 有图形设备 1/1 PASS。

### Round 5：地图取景/网格修复 + 动态物产跨局泄漏

- **地图看不见/偏到角落（PlayMode 截图暴露的渲染 bug）**，三个叠加错误：
  1. `InitializeRenderer` 把网格建在 **XY 平面（竖直广告牌）**，而 45° 俯视相机是按"地面平面"推导的 → 中心射线永远打不到网格中心；改为 XZ 平面（y=0）。
  2. `SetupMapDisplay` 对**已经按世界单位生成的网格**又做了 `worldW/10` 缩放 + 半宽平移（按 Unity Plane 基元 10×10 的旧假设），地图被放大 10 倍并偏到右下角；改为 `localScale=one`、`localPosition=zero`。
  3. 相机放在地图中心 z 上、而 forward=(0,-0.707,0.707) 会向 +z 俯冲 → 看向地图外；改为"注视点 `_cameraTarget` + 固定 45° 几何反推相机位置 `target+(0,d,-d)`"，平移/缩放改为移动注视点，边界 clamp 也按注视点。
  验证：PlayMode 新增取景回归断言（平面不被缩放、中心射线落到网格中心 ±8%），有图形设备 1/1 PASS，截图为完整居中的地形地图。
- **DynamicGoods 跨局泄漏（A-P1-6）**：`InitializeWorld` 清空全局静态表 `DynamicGoodsRegistry`，新局不再继承上一局的合金/合成物。

### Round 6：地图配色/叠加层诊断与降噪

诊断数据（Unity 内实测生成）：

- 256×128 地图：陆地 9830、省份 1228、**省界地块占陆地 87.8%**；
- 1024×512 地图：陆地 157286、省份 6049、聚落 10077（主聚落 9482）。

由此定位三个让地图"发白/发糊"的渲染问题，已修：

1. **地形高程映射错配**：陆地高程分布 0.29~1.00，旧公式 `(e+1)/2*255` 把 89% 陆地映射到"山地/雪山"色档。
   改为按当前世界陆地 min/max 归一化，并只映射到调色板 0.45~0.88（上限不取 1.0，避免高程噪声把 20%+ 地块打成纯白雪点）。
2. **省界整格填 0.9 白**：小省（8~26 格/省）时边界地块占比极高，整格填白 ≈ 把地图刷成白噪点；而且地形模式里内联描边 + 叠加层又重复画了两次。
   改为内联 **35% 混色描边**，叠加层只服务非地形模式（35%）并跳过地形模式。
3. **聚落图标过密过大**：10077 个聚落、主聚落 9482，每个图标 2~7 **纹理像素 = 2~7 个地块**，全图画出来覆盖约 36% 画面。
   改为：满图视野不画标记（`_zoomLevel > 全图视野×0.45` 时跳过），拉近才显示；图标尺寸收紧到 1~3px。

**验证**：全量 EditMode **311/311**；PlayMode 有图形设备 1/1 PASS（取景+颜色断言）。地图已完整居中、有地形与河流层次；
剩余观感问题（陆地整体偏卡其色、河流/海岸线不突出）属于调色板设计，下一轮单独做。

### Round 7：地形配色重做（地图观感）

Round 6 之后地图仍偏卡其/发白，继续定位到材质与调色板两层问题：

1. **材质用错**：地图平面用 `Standard`，被场景方向光/环境光二次照亮，实测把草绿 `(102,148,69)` 冲成 `(200,220,170)`。
   地图纹理本身已经烘焙了山体阴影/纸纹，改为 **`Unlit/Texture`** 直出。
2. **调色板与地形分布不匹配**：重做 256 级色带——深海→浅海→沙滩→低地草甸→草原→干草原→丘陵→岩石→雪线，
   陆地按归一化高程走 0.45~0.95 段；低地按降水/肥力做干湿差异（草绿↔干黄），年均温 < -8℃ 的高处按温度覆雪。
3. **水域映射错位**：旧公式把近海 `e≈-0.03` 映到调色板 0.5 附近（陆地草绿段），海面出现绿色斑块；
   改为 `e∈[-1,0] → 调色板 [0,0.44]`。

PlayMode 增加**饱和度断言**（非背景像素平均 S > 0.10）：防止以后有人把材质改回受光材质导致"地图被洗白"再次悄悄回归。

**验证**：全量 EditMode 311/311；PlayMode 有图形设备 1/1（取景 + 颜色多样性 + 亮度层次 + 饱和度）。
地图观感：草绿低地 + 灰蓝山地 + 雪线，海岸有浅色带。

### Round 8：Agent Teams 并行修复（原子存档 / 经济守恒）

按团队流程拉卡开工（每卡一个 owner + write scope + merge gate）：

| 卡 | Owner | 内容 | 验收 |
|---|---|---|---|
| task-7 | save-fixer | 存档原子写：`.sav.tmp` → `File.Replace/File.Move`，`.bak` 清理、失败保留旧档；`IsValidSaveName`（空/路径分隔/`..`/非法字符）；写入后回读校验；5 条新测试 | 编译 + 13 定向测试 |
| task-8 | economy-fixer | `TradeCenter.AddGoodsReturningAccepted`（实际入库量）+ 旧 bool 兼容；`EconomyManager` 商队到货守恒（未入账留在 cargo 不蒸发）、退役退库/最近库回退、缺粮后果（稳定度/秩序/人口下降 + 日志）；7 条新测试 | 编译 + 13 定向测试 |
| task-9 | lead | `SaveSystemRoundTripTests` 字段级全链路（日历/地块/省份/聚落含市场区划设施/军队编制士气/国库/区划 holder/革新/SaveVersion） | 定向测试通过 |
| task-10 | verifier | 只读复核以上两张实现卡与 Lead 测试（原子写失败路径、守恒恒等式、商队滞留、缺粮双罚、测试有效性、越界改动） | 报告 + merge gate 结论 |

Lead 统一执行（agent 环境不能并行跑 Unity）：编译 `COMPILE_OK`；定向测试 **13/13**；全量 EditMode **324/324**；PlayMode 有图形设备 1/1。

已修掉的审计项：
- A-P1-8（写盘非原子、异常吞栈）→ 原子写 + 回读校验 + 异常带堆栈；saveName 校验。
- C-P1-2（市场/仓储守恒、商队货物蒸发）→ 实际入库量语义 + 到货剩余留 cargo + 退役退库/损失可追踪。
- C-P1-3（城市缺粮无后果）→ 缺粮惩罚（稳定度/秩序/人口 + 汇总日志）。
- D 补测（字段级存档回归）→ SaveSystemRoundTripTests。

**验证**：全量 EditMode **324/324**，PlayMode 有图形设备 1/1。

**复核（`docs/audit/VERIFY-team-round.md`）merge gate**：task-7 可合入（`.bak` 回读、读/删路径校验、陈旧 tmp 清扫三条 follow-up）；task-8 需修后再合入（P1 商队滞留占位→贸易死锁；P2 缺粮双罚与半日需死循环）。
已开 follow-up 卡：task-11（save-fixer）、task-12（economy-fixer），修完 Lead 重跑编译+全量测试。

**Follow-up 完成**：
- task-11：`LoadGame` 在 `.sav` 缺失时从 `.bak` 恢复（Copy→restore.tmp→Move，失败保留 .bak）；`ListSaves` 补列仅 `.bak` 档；`LoadGame/DeleteSave` 复用 `IsValidSaveName`（读/删防穿越）；`SaveGame` 前清陈旧 `.tmp`。测试 +4。
- task-12：商队到货容量不足计滞留天数，连续 5 天退役（退起点/最近区域库，装不下的记损失+Warn）；`InitiateTrades` 上限 `>=20` 且发车循环内复查；缺粮改为"先吃光剩余库存再轻罚"（stability −0.2/order −0.1/pop −0.02%），重度饥荒仍由 GameWorld Famine 事件负责。测试 +3。

**最终验证**：全量 EditMode **331/331**；PlayMode 有图形设备 1/1（PIL 复核：unique=15049、主色=背景，真地图渲染）；定向 20/20。

**复核 Round2（`docs/audit/VERIFY-team-round2.md`）merge gate**：task-11 / task-12 均**可合入**，无 P0/P1。
剩余 P2（下迭代）：损坏 `.sav`+有效 `.bak` 不回退、`.restore.tmp` 崩溃残留未清、2 条非判别测试、满仓时"发车-5天退役-退货"空转（自愈有上限）、缺粮双罚口径、新增 public 入口语义、DeleteSave 仅 bak 返 true。

**团队流程小结**（本次 Agent Teams 三阶段）：4 名只读审计 → Lead 修 P0 → 2 轮 verifier 抓出"修了没生效/假绿" → 2 张实现卡（原子写、经济守恒）+ Lead 字段级测试 → verifier merge gate 两轮（Round1 抓出 2 P1，Round2 全 PASS）。所有卡：task-1..13 全部 completed。

### Round 9：Agent Teams 第三轮（P2 收口 + 角色运行态入档）

| 卡 | Owner | 内容 | 结果 |
|---|---|---|---|
| task-14 | save-fixer | 损坏 `.sav` 回退有效 `.bak`（TryParseSaveData + 先删后 rename）、`.restore.tmp` 清理、`DeleteFileQuietly` 复位只读、2 条非判别测试改判别 + 1 新 | +1 测试 |
| task-15 | economy-fixer | 发车前目的地容量预检（余量 <1 或 <计划 10% 不发车）+ Warn 节流；缺粮口径 (a)：本系统只罚 stability/order，人口归 GameWorld Famine | +2 测试 |
| task-16 | character-fixer | 角色运行态入档 5 项：traits/relations/skills/activeDiseases/impairments（并行数组拍平 + 旧档兼容） | +6 测试 |
| task-17 | lead | 旧档 v11 兼容测试（缺新段可读、版本不一致只警告、非法 JSON 不崩） | +3 测试 |
| task-18 | verifier | 只读复核以上（进行中） | — |

**验证**：编译 `COMPILE_OK`；全量 EditMode **343/343**；PlayMode 有图形设备 1/1；定向 32/32。

已知仍不入档（写进 character handoff）：`bodyParts`（三层嵌套）、`CharacterBond`、`personalTenets/bodyMarks/成就计数`、派生缓存 dnaExpression/appearance。

**复核（`docs/audit/VERIFY-team-round3.md`）merge gate**：task-14 / task-15 / task-16 / Lead Legacy 测试**全部可合入**，无 P0/P1。
残余 P2：`DeleteFileQuietly` 复位只读的语义变化（设计决策）、RestoreSaveFromBackup 先删后 rename 崩溃窗口（可自愈）、LogAssert 精确文案契约（建议常量化）、relations 孤儿（目标角色未入档时消费侧 TryGetValue 安全，建议 restore 后过滤）、impairment 捕获缺判空、预检阈值保守、存档体积随关系史增长。

### Round 10：第四轮团队（asmdef 去旧依赖 / 信仰入档 / bodyParts+bonds）

| 卡 | Owner | 内容 | 结果 |
|---|---|---|---|
| task-19 | arch-auditor（代实现） | 4 个新程序集 asmdef 删除对旧 `CivilizationEvolution.Runtime` 的引用（Simulation/World/Infrastructure/Rendering） | 编译 COMPILE_OK；新程序集不再看到 262 组同名旧类型 |
| task-20 | character-fixer | bodyParts 三层拍平（19 个并行数组）+ CharacterBond DTO/Manager API（Lead 补 SaveData/SaveSystem 3 行接线） | +5 测试 |
| task-21 | save-fixer | 信仰运行态入档：FaithSaveData/FaithPersistence + SaveData.faiths + overlay + **SaveVersion 15** + GameWorld.FaithSystems 访问器 | +4 测试 |
| task-22 | lead | 被删专项补测：FaithSystem 规则（fervor clamp/大圣战阈值/教阶）+ 地形同 seed 确定性 | +5 测试 |
| task-23 | verifier | 只读复核（进行中） | — |

**B 线进展**：旧程序集已从 4 个新程序集依赖图中移除（旧目录/旧 asmdef 仍在、仍独立编译；场景 Main.unity 的旧 MapEditor 绑定未动）。下一步：迁移场景旧 MapEditor → 新 RuntimeMapEditingController，再删旧目录与旧 asmdef。

**验证**：编译 `COMPILE_OK`；全量 EditMode **357/357**（343+14 闭合）；PlayMode 有图形设备 1/1；定向 46/46。

**复核（`docs/audit/VERIFY-team-round4.md`）**：task-19 / task-20 / task-21 / Lead RestoredSystemTests **全部可合入**，无 P0/P1；bodyParts/纽带/信仰三项缺口闭环。
剩余 P2：asmdef 改动无法从 git 完整归因（Simulation/World asmdef 未跟踪）、bodyParts 实际 20 个并行数组且两个 count 数组仅自描述、`CaptureBodyParts` 未判空 part、**编辑器全量重算路径 `SeaLandGenerator.RecalculateAll/Dirty` 仍用未播种 UnityEngine.Random（重算不可复现）**、未知 faith 测试依赖精确文案、RestoredSystemTests 不清静态注册表、v15 前向兼容会静默丢运行态、存档体积未评估。


### Round 11：第五轮团队（B 线场景迁移 / UI 可点击 / 确定性重算）

| 卡 | Owner | 内容 | 结果 |
|---|---|---|---|
| task-24 | arch-auditor | Main.unity 旧 MapEditor GUID → `RuntimeMapEditingController`（字段名 world/mapRenderer/mainCamera/brushRadius 兼容 + Awake 兜底） | 场景已无旧程序集脚本 |
| task-25/26 | economy-fixer | `SeaLandGenerator` 重算路径改确定性 RNG（FNV/Murmur、可选 ctor seed，不碰全局 Random）；镜像到**生产在用的 Terrain 副本** + Terrain/Generation parity 测试 | +4 测试 |
| task-27 | character-fixer | 主菜单可点击性：Canvas 挂回 MainMenuUI 层级（原先根对象 DDOL→冒烟找不到）、新增「开始游戏」+ 异步生成不卡主线程、动态文本显式赋中文字体、EventSystem 兜底、加载存档改真读档、主菜单期间禁用地图点击；删 5 个零引用旧 UI 脚本 | 修复 D1-D6 |
| Lead | lead | 场景清理：删除 UICanvas/StartMenuPanel、NewGamePanel、SettingsPanel、SpeedGroup/MapModeBar（备份 `D:/TEMP/Main.unity.bak-ui-cleanup`）；新增 PlayMode `MainMenuUiPlayModeTests`（射线命中 + onClick + 离开主菜单）；PlayMode asmdef 加 `Unity.TextMeshPro` | 场景 34.27MB→34.00MB，missing=0 |

| task-28 | arch-auditor | B 线第二步 b：删除旧程序集 20 个顶层目录 + 旧 asmdef + 孤儿 `Assets/_Recovery/`（22 目录 / 210 文件；备份 `D:/TEMP/legacy-runtime-backup.tar.gz`） | 删除后编译 COMPILE_OK，全量测试全绿 |

**验证**：编译 `COMPILE_OK`；全量 EditMode **361/361**（357+4）；PlayMode 无图形 2/2、有图形 **2/2**（主菜单点击冒烟 + 地图渲染回归）。

**B 线完成（删旧程序集后复验）**：全工程 `using CivilizationEvolution`=0、`CivilizationEvolution.` 仅 2 处自建 namespace + 2 处注释、旧 GUID 非 .cs 资产残留 0、`Assets/Scripts` 仅剩 5 个新目录；删后全量 EditMode **361/361**、PlayMode 无图形/有图形各 **2/2** 全绿。

**复核（`docs/audit/VERIFY-team-round5.md`）merge gate**：task-24 / task-25/26 / task-27 / task-28 / Lead 场景清理**全部可合入**，本轮无 P0/P1；round4 的"重算不可复现"P2 已闭环。
残余 P2：①场景 6 条旧 `m_EditorClassIdentifier` 字符串待重存清理；②`DefaultWorldConfig.asset` 的脚本 GUID 悬空（既有坏资产）；③备份 tar 还原映射未记录；④PlayMode 测试无 TearDown/射线断言仅 `Count>0`（近乎恒真）；⑤异步生成路径无测试覆盖；⑥`_recalcSeed` 未接世界 seed（恒 0，可复现但与 seed 无关）；⑦4096² SDF 字体开销未评估；⑧`Assets/Editor/UiCleanup.cs`/`UiDiagnostics.cs` 为临时破坏性工具，不应提交；⑨Editor 两文件旧命名空间 `CivilizationEvolution.EditorTools`；⑩task-27 实际删 6 个零引用脚本（卡写 5）。

### Round 12：第六轮团队（新系统收口 / 继承端到端 / seed 接线 / 读档重绑）

| 卡 | Owner | 内容 | 结果 |
|---|---|---|---|
| task-30 | arch-auditor | 删生产 0 引用的重复类 `World.Generation.SeaLandGenerator`（保留 Terrain 版）+ 删 parity 测试；修悬空资产 `ScriptableObjects/DefaultWorldConfig.asset` 的 `m_Script` GUID（原先 Main.unity 的 GameWorld 引用它 = Missing Script）；Editor 命名空间 `CivilizationEvolution.EditorTools`→`MyriadCreation.EditorTools`；6 程序集重复类型审计（7 组同名，MapSizePreset/UnitCategory 同名不同概念、CirculationMode 值漂移） | 4 改 2 删 |
| task-31 | character-fixer | `SuccessionEndToEndTests`（7 条真实 Realm+CharacterManager 夹具：长子/幼子/绝嗣/共和选举/幼主/继承线+大圣战受益人/异常输入），暴露 6 条 Succession/GameWorld 真实缺陷（绝嗣重复危机、世袭忽略资格、幼主事件语义等） | +7 测试 |
| task-32 | economy-fixer | 世界 seed 接入 SeaLandGenerator 重算路径（`_seaLandRecalcSeed` + `EnsureSeaLandGeneratorSeed` + WMS `BeforeFullRecalculate`/`ReplaceSeaLandGenerator`）；同/异 seed 世界级重算测试 | +2 测试；发现破碎噪声生产流程未注入 |
| task-33 | save-fixer | 读档后世界级组件重建（`RuntimeMapEditingController.BindWorld` + GameManager 重绑，含 inactive）；存档面板"删除存档"入口（二次确认） | +3 测试 |
| Lead | lead | 场景 6 条旧 `m_EditorClassIdentifier` 直接改为新程序集::类型（Unity 重存不重写）；PlayMode 主菜单冒烟加 TearDown + 严格射线断言（命中链必须含开始按钮） | — |
| task-34 | character-fixer | 修 Succession 缺陷：世袭资格过滤（P1-2）、绝嗣 `heirId=-1` + GameWorld 按死君主 id 去重"继承危机"编年史（P1-1）、幼主"即位+危机"双记录（P2-5）、共和 scope 资格（P2-4）；测试 7→11 条 | 语义修复 +4 测试 |
| task-35 | arch-auditor | 删 0 引用重复接口 `Core.Simulation.ISimulationCommand/Query` + 悬空 `Assets/Data/DefaultWorldConfig.asset`；项目自有悬空脚本资产扫描 = 0 | 删 6 文件 |

**验证**：编译 `COMPILE_OK`；全量 EditMode **376/376**（361−1 parity+2 seed+7 succession+3 rebind+4 succession 修复）；PlayMode 无图形/有图形各 **2/2**；定向 Succession 11/11。

**"所有代码都是新系统"进度**：旧程序集已删除；重复 SeaLandGenerator 副本删除（只留 Terrain 版）；0 引用重复接口删除；悬空配置资产修复/删除；Editor 命名空间归一；场景 6 条旧 `m_EditorClassIdentifier` 已改为新程序集::类型。剩余重复概念（**仅报告不阻塞**）：`CirculationMode` 两套枚举（GameEnums 版被 world/climate 使用，MapGenerationConfig 版生成内部使用，无交叉）、`MapSizePreset`/`UnitCategory` 同名不同概念、`Seed` 两处相同实现（保留）。

### Round 13：产品决策 + 架构解耦彻底落实（大轮）

**产品决策**
| 卡 | Owner | 内容 | 结果 |
|---|---|---|---|
| task-37 | save-fixer | 破碎化生效：SeaLandGenerator 构造函数按 recalcSeed 生成确定性 2D 分形值噪声（不碰 UnityEngine.Random），默认 landFragment=0.4/coastFragment=0.3；`randomSeed` 入档（**SaveVersion 16**） | +6 测试 |
| task-42 | save-fixer | 初始开图也应用破碎化（GenerateTerrainBase/WithConfig 末尾 RecalculateAll；读档不二次破碎） | +4 测试 |
| task-38 | character-fixer | 摄政/推举兜底：绝嗣进入 interregnum，家族成年→行政长官→高威望选摄政；出现合法继承人自动加冕；RealmDTO 持久化；无合格者空转不刷屏 | +7 测试 |
| task-39 | economy-fixer | 同名不同概念收尾：`GenerationMapSize`/`GenerationCirculationMode`（+ToGameEnums 映射）、`GameEnums.UnitArmType`；序列化数值/字段名未变 | 7 文件 |
| Lead | lead | 存档面板删除交互 PlayMode 测试（二次确认 + 不触发加载）；PlayMode 两处测试修正（按钮查找根/背景色判定） | +1 PlayMode |
| task-49 | save-fixer | 生成后清 terrain dirty（防首个每日 tick 二次改写地形） | 进行中 |

**架构解耦彻底落实（B1-B5，ARCH-COMPLETION-PLAN.md）**
- B1（task-43）：平铺目录物理归位 42 文件（Infrastructure/{Audio,Save} → MyriadCreation.Infrastructure.*；Rendering 根/Overlays/Terrain → MyriadCreation.Rendering；Editor/UI 按命名空间分流）。
- B2（task-44）：结算簇 4 文件归位 `World.Settlement`（namespace 同步），**V1-V3 跨层违规归零**（Core/World 内 `using MyriadCreation.Simulation` = 0）。
- B3（task-45）：删 0 引用 `MapSaveSystem`（607 行）+ Infrastructure asmdef 移除 Rendering 引用。
- B4（task-47）：Presentation 拆分——12 文件 → `UI.Presentation`，PlaceNameGenerator→Simulation.Generation、LawComponentNames→Simulation.Laws，3 个死代码删除。
- B5（task-46）：Core.Simulation 灰区 11 文件 → `Simulation.Scheduling`（namespace 改名，35 文件引用更新）。
- Lead 清理：删除 TMP Examples & Extras（34 个 Assembly-CSharp 脚本，备份 `D:/TEMP/tmp-examples-backup.tar.gz`）、`TEMPdecompiled-sim`、`.reverse`、陈旧 `CivilizationEvolution.*.csproj/slnx`。

**最终架构状态（自检）**：程序集图 `Core[] ← World[Core] ← Simulation[Core,World] ← {Rendering, Infrastructure}[Core,World,Simulation] ← UI[全部]`；目录↔程序集层错位 **0**；跨层违规 **0**；Infrastructure 不依赖 Rendering；`Assets` 内无 asmdef 之外的 `.cs`。

**验证（含 P1 修复后终值）**：编译 `COMPILE_OK`；全量 EditMode **399/399**；PlayMode 无图形/有图形各 **3/3**（MainMenu ~5.4s，MapVisibility ~5.8s）。

**verifier 复核后的两项 P1 已修**（task-50/51）：
- **P1-1 性能**：破碎化重算热路径从"每次邻接遍历 new int[6]+迭代器"改为构造期扁平邻居表 + `ReadOnlySpan`（0 分配）+ 缓冲复用 + 邻接局部化；新增 `SeaLandPerformanceTests`（1024×512 同 seed 两次生成 + 地形哈希一致 + <10s 预算）。实测 **1024×512 GenerateTerrain 2.55–2.75s**（原 85s 级）。
- **P1-2 一致性**：破碎化前后快照 → 中位数估计统一高程折算（~0.498）→ 只对实际变化地块（默认 1024×512 世界 106234/524288）重算 annualTemp/annualPrecipMm/biome/fertility（海地 fertility=0；温度用破碎化前绝对高程防偏暖；`CalculateClimate` 全量路径未改）；新增 `FragmentationClimateConsistencyTests` 3 条（changed 一致/同 seed 确定/读档不重算）。
- 另修 PlayMode 测试间大世界 GC 泄漏（TearDown + GC.Collect），消除 ~109s 抖动。

**残留已知债务**：编辑器 `GenerateAll()`/`RecalculateHydrology` 路径仍会留全表 dirty（正确修法为 hydrology 后直接全量重算），已记录；地形尺寸 2048×1024+ 开图仍有数百 ms 级全量重算成本。

### Round 14：法令/法律流水线 + 完整设置 + 聚落图标 + P2 收尾

| 卡 | Owner | 内容 | 结果 |
|---|---|---|---|
| task-52 | character-fixer | **法令/法律流水线**：已知法（knownLawIds + EnsureKnownLaws/AddKnownLaw）；推行中 PendingLaw（BeginEnactLaw 登记 pending、旧法继续有效、TickPendingLaws 到期固化、同组唯一、可取消、成本登记扣除）；提案授权 ProposalAuthority（HeadOfState / CentralInstitution / CharterHolders，CanPropose 校验）；法令 active/pending；`ForceSetLaw` 供政变直接改制度；`LawDecreeView` 三段式（已固化制度/生效法令/推行中法令，不暴露组/类别）；GovernmentPanel 三段 UI；RealmDTO 持久化 | 12 条测试 |
| task-53 | economy-fixer | **完整游戏设置**：SettingsModel/SettingsService（分辨率/显示模式/vSync/画质/帧率上限/UI 缩放/语言/主·音乐·音效音量/默认地图尺寸/重置默认/存档路径提示）；PlayerPrefs `MyriadCreation.Settings.*` 持久化；Bootstrap 启动应用；SettingsPanel 代码 UI | 10 EditMode + 2 PlayMode |
| task-54 | arch-auditor | **聚落锚点图标层**：SettlementIconLayer（程序化 Sprite 按聚落等级/类型着色、MapRenderer.TileToWorld、LOD 近全远仅大城、点击只选中高亮不弹 UI、暴露 IconCount/SelectedSettlementAnchorId/SelectAtScreen、随 BindWorld 重建） | PlayMode 图标测试 |
| task-55 | save-fixer | P2：airHumidityPct/climateZone 纳入破碎化后重算；温度/降水/湿度公式收敛单一 helper；编辑器 RecalculateHydrology 后直接全量重算+清 dirty；性能预算 10s→8s | +2 测试 |
| Lead | lead | 修复 task-52/53/54 中断后的收尾（测试 using/GetEntityId/湿度测试改用公开重算索引 API）；PlayMode 主菜单冒烟改为点「设置」不再生成 1024×512 世界（消除后续测试 GC 膨胀 ~100s）；暴露 `GameWorld.LastFragmentationRecomputedTiles` 供测试 | — |

**验证（终值）**：编译 `COMPILE_OK`；全量 EditMode **423/423**（413 + 设置 10）；PlayMode 无图形/有图形各 **6/6**（主菜单 ~2.4s、地图 ~5.7s、存档删除 ~0.15s、聚落图标 ~0.4s、设置面板 2 条）。

**收尾修复**：
- task-52/53/54 中途 agent 失败后的收尾：图标测试 using/`GetEntityId`；湿度测试改用新增 `GameWorld.LastFragmentationRecomputedTiles` 公开重算索引（避免 changed 语义歧义）；主菜单 PlayMode 冒烟改点「设置」不再生成 1024×512 世界（消除后续测试 ~100s GC 膨胀）；`MainMenuUI` 创建 CanvasScaler 后应用已保存 UI 缩放（修复"重启后 UI 缩放对主菜单不生效"）。

**已知后续增强**：分辨率下拉未含刷新率（Unity 6 RefreshRate 兼容）、音效音量仅持久化（无集中 SFX 总线）、法律组/类别仅在代码内不再展示（LawDecreeView 三段式）、派系/政变系统尚未调用 `ForceSetLaw`（API 已备）。

### Round 15：法令/法律落地到游戏循环 + 聚落详情面板 + 设置增强

| 卡 | Owner | 内容 | 结果 |
|---|---|---|---|
| task-58 | character-fixer | 派系/政变 → catalog 法律同步：`LawAndDecreeSystem.ForceReformSync`（LawComposition 维度→catalog law 最小映射：CentralInstitution Court/Assembly/EldersCouncil→privilege/charter_free_city、BureaucraticCore/MilitaryCouncil→military/standing_army）；`LawReform.Reform` 成功分支调用并写 `law_reform_sync` 编年史；`RegimeChangeDynamics` 传 day | +6 测试 |
| Lead | lead | `GameWorld.RegisterSimulationSchedules` 新增 `laws.pending.daily`：每日 `TickAllPendingLaws`，到期才固化并写编年史（此前 pending 只在 GovernmentPanel 打开时推进） | 1 处接线 |
| task-56 | economy-fixer | `SettlementDetailPanel`（名称/类型/政权/人口/财富/守军/建造等级/区划/设施/市场/首都港口等，ESC/关闭按钮，无效 anchor 安全）+ `UiScaleBinder`（动态 Canvas 应用已保存 UI 缩放） | +1 PlayMode |
| task-57 | arch-auditor | `SettlementIconLayer.SettlementClicked(anchorId)` 事件 + `GameManager` 订阅并 `panel.Show(anchorId)`（含换世界/读档解绑重建、防重复订阅） | PlayMode 断言面板打开且含聚落名 |
| task-59 | save-fixer | 设置增强：分辨率刷新率（Unity 6 `RefreshRate`/`refreshRateRatio`，去重/Find/持久化/回退）；`SfxPlayer` 音效总线（AudioSource 池 6、`SetVolume` 由 SettingsService 推送，批量模式不实际播放） | +4 EditMode |

**验证（终值）**：编译 `COMPILE_OK`；全量 EditMode **433/433**；PlayMode 无图形/有图形各 **7/7**（新增聚落详情面板 PlayMode 测试）。修复过程中处理了 3 个 teammate 中断后的收尾（laws/图标/设置 UI 缩放）与一次 Unity 批处理进程残留锁。

**遗留增强**：catalog 法律映射表目前仅 CentralInstitution；法律效果（SumLawEffects）仍无引擎消费方；SfxPlayer 暂无音频片段资源（总线已备）；面板独立 Canvas。

### Round 16：旧 UI 文件清理 + 政府/法律面板接入

| 项 | 内容 | 结果 |
|---|---|---|
| 旧 UI 资产清理 | 删除 `Assets/Art/Interface`（旧 UI 皮肤 shader/material/texture，全自引用、场景/预制体 0 引用）、空 UI 目录 16 个（仅 .gitkeep 的 Character/Diplomacy/District/HUD/… 等旧板块占位）、死面板 `DiplomacyPanel`、临时 Editor 工具 `UiDiagnostics/UiCleanup/ResaveScene`；备份 `D:/TEMP/old-ui-backup.tar.gz` | UI 目录只剩 Common/MyriadCreation.UI/UI.Presentation/Panels/Settings |
| task-60 | GovernmentPanel 支持代码动态 `Build(parent)`（模态遮罩/滚动内容/三区块、中文字体、世界/政权 null 安全、PlayerRealmId 回退）；UIManager 接线：TopBar 动态「政府」按钮 + G 快捷键 + `Open/Close/Toggle/IsGovernmentPanelOpen`；新增 GovernmentPanel PlayMode 测试 | +1 PlayMode |
| Lead | ESC 栈守卫：`SceneFlowController` 返回主菜单前检查 `IsGovernmentPanelOpen` / `SettlementDetailPanel.IsOpen`，避免"关面板同时回主菜单" | 1 处 |

**验证（终值）**：编译 `COMPILE_OK`；全量 EditMode **433/433**；PlayMode 无图形/有图形各 **8/8**（新增政府面板测试）。

**清理过程中处理的问题**：Unity 批处理残留进程导致的 `Temp/UnityLockfile` 占用（清理后重跑）；删除旧 UI 资产后全量回归确认无引用破坏。

### Round 17：法律映射/效果真正生效 + UI 音效 + 顶栏窄屏

| 卡 | Owner | 内容 | 结果 |
|---|---|---|---|
| task-61 | character-fixer | catalog 映射扩到 7 个治理维度（新增 succession/supreme_scope/central_succession/local_succession/local_scope/spatial 6 组 23 法，共 9 组/32 法；30 个组件组合命中）；`LawEffectRuntime`：SumLawEffects+SumDecreeEffects 合并、`GetTaxMultiplier/GetManpowerMultiplier/ApplyYearlyEffects`；税收乘子接入 `EconomyManager.SettleTaxes`+`PoliticsSimulationSystem.DailyTick`；`Army.ReinforceUnit` 预接人力乘子；GameWorld 新增 `laws.effects.yearly` 调度 | 9 条 LawEffectRuntimeTests + Faction 测试 7 条 |
| task-62 | economy-fixer | 生成 5 个占位 UI 音效 WAV（`Assets/Resources/Audio/UI/`）+ `UiSfx`（Resources.Load 缓存→SfxPlayer 总线）+ 主菜单/设置/政府/存档/聚落面板接线；顶栏「政府」按钮 LayoutElement（86/64、字号 13/12、窄屏自动缩）+ 屏宽变化重排；SettlementDetailPanel Canvas 层级 500→600（HUD 0-399 / 政府 500 / 聚落详情 600 / 菜单 1000+） | 4 EditMode + 1 PlayMode |

**验证（终值）**：编译 `COMPILE_OK`；全量 EditMode **447/447**；PlayMode 无图形/有图形各 **9/9**。

**遗留**：`laws.effects.yearly` 用 LongTerm(365)，daysPerTick>1 时可能跳年（建议年份变化 guard）；`Army.ReinforceUnit` 人力乘子暂无生产调用方（预接 API）；`LawEffectRuntime` catalog 缓存仅默认目录；年度 unrest 用 stability 近似（无政权级持久 unrest 字段）。

### Round 18：系统职责文件夹化（法律归拢 + 信仰/文化独立目录 + 架构规范）

| 卡 | Owner | 内容 | 结果 |
|---|---|---|---|
| task-63 | character-fixer | **法律/法令全部归拢**到 `Simulation/MyriadCreation.Simulation.Laws/`：State（Law/LawComposition/LawEffects）、Definitions（InheritanceLaw/LawCategory/LawSource）、Systems（LawCatalog/LawConstraints/LawReform/LawSystem）10 文件迁入 + namespace 统一 `Simulation.Laws` + 21 文件补 using；剩法律收尾：yearly 年 guard（Daily+`currentYear` 变化，daysPerTick>1 不跳年）、`ManpowerSystem.GetRealmTotalManpower(day)` 接人力乘子、`LawEffectRuntime` catalog 可切换（mod 扩展点）、`RealmData/RealmDTO.unrest` 持久字段（stability 与 unrest 分离，不改 SaveVersion） | 编译通过 + LawEffectRuntimeTests 13 条 |
| task-64 | economy-fixer | **`docs/architecture/SYSTEM_OWNERSHIP.md`**（459 行）：顶层目录职责表、系统 owner 现状/目标/接口/依赖方向、兼容层规则 R1–R8 与允许通道、V1–V16 违反点（文件:行）、B1–B8 拆分批次（B1 同 asmdef 归位→B2 Content→B3 App/GameWorld 拆分→B4 Faith→B5 Culture→B6 Laws→B7 World/Rendering 视图契约→B8 契约/门禁） | 文档 |
| task-65 | arch-auditor | **信仰/文化独立大文件夹**：`Simulation/MyriadCreation.Simulation.Faith/`（17 文件：FaithSystem/CanonizationSystem/Religion*/Doctrine*/Saint*/GreatHolyWar*）、`Simulation/MyriadCreation.Simulation.Culture/`（6 文件：CultureData/Culture* 系统），namespace 统一；26 消费文件 31 条 using 更新；Core 事件与 Infrastructure 存档保留兼容层 | 编译通过 |

**验证（终值）**：编译 `COMPILE_OK`；全量 EditMode **451/451**；PlayMode 无图形/有图形各 **9/9**。

**架构原则落地（用户主张）**：系统各自拥有文件夹/namespace（法律/法令、信仰、文化已独立），跨系统连接只经 Core 事件/契约或 Infrastructure 兼容层；美术只放图/材质/纹理/效果（`Assets/Art` 已 0 代码），UI 只放 UI 定义与交互。后续按 SYSTEM_OWNERSHIP 的 B2–B8 分批抽程序集/反转接口（Faith/Culture/Laws 独立 asmdef 需先做 IFaith*/ICulture*/ILaw* 查询/命令契约）。剩余"法律相邻未移"：ComponentChoice/PolityComponentInnovations/SupremeSuccessionLevel/Inheritance* 枚举/JudicialSystem/CrimeRegistry（建议随 B1 下一批处理）。

### Round 19：CK3 风格"定义与脚本分离"落地（法律 JSON 化 + 系统内部分层）

| 卡 | Owner | 内容 | 结果 |
|---|---|---|---|
| task-66 | character-fixer | 法律相邻文件全部归拢 `Simulation.Laws`（ComponentChoice/PolityComponentInnovations/TrialResult、6 个 Inheritance*、SupremeSuccessionLevel、CrimeType/CrimeBehavior/CrimeDefinition/CrimeTargetClass/VerdictType、JudicialSystem/CrimeRegistry，共 17 文件+meta/namespace）；**法律定义 JSON 化（CK3 `common/` 对齐）**：`StreamingAssets/Base/Laws/Laws.json`（9 组/32 法/2 法令，数组结构）+ `LawContentLoader`（懒加载/校验/失败回退硬编码/`LawEffectRuntime` 索引重置）+ `DefaultLawCatalog` 改为 JSON 优先、硬编码回退、深拷贝返回 | 编译通过 + LawContentJsonTests 5 条 |
| task-67 | arch-auditor | Faith/Culture 内部分层：`Faith/{Definitions,State,Runtime}`（9/3/5）、`Culture/{State,Runtime}`（1/5），纯移动+README（约定"大系统 namespace + 子文件夹角色"）；内容管线盘点文档 `docs/architecture/CONTENT_PIPELINE_FAITH_CULTURE.md`（已 JSON 化 vs 仍硬编码：CultureStage 阈值/宗教调色板/SaintDef；schema 与可落地试点 patch） | 编译通过 |
| Lead | lead | `SYSTEM_OWNERSHIP.md` 追加 **附录 A：CK3 对齐**（common/events/gui/gfx/localization/history/map_data → 本项目 StreamingAssets/UI/Art 的映射；本项目更进一步的三条约定） | 文档 |

**验证（终值）**：编译 `COMPILE_OK`；全量 EditMode **456/456**；PlayMode 无图形/有图形各 **9/9**。

**下一批架构（SYSTEM_OWNERSHIP B2–B8）**：B2 抽 `MyriadCreation.Content`（Modding+Definitions，反转 Definitions→State 11 条）→ B3 抽 `MyriadCreation.App`（Bootstrap/GameManager/SceneFlow/设置服务/地图编辑执行器 + 拆 GameWorld God object）→ B4/B5/B6 Faith/Culture/Laws 独立 asmdef（先做 IFaith*/ICulture*/ILaw* 查询/命令契约反转）→ B7 World/Rendering 视图契约（Rendering 去 Simulation 直接 import）→ B8 契约/事件推广 + 架构门禁（asmdef 反向=0、UI 禁 import Simulation）。法律 JSON 与硬编码目前双份（等值测试防漂移），JSON 稳定后删除硬编码回退体量。

### Round 20：B2 Content 程序集 + B3-part1 GameWorld 拆分 + CultureStage JSON

| 卡 | Owner | 内容 | 结果 |
|---|---|---|---|
| task-68 | arch-auditor | **B2 抽 `MyriadCreation.Content` 程序集**：95 个 Definitions 文件（+95 meta，SHA 一致）迁入 `Assets/Scripts/Content/MyriadCreation.Content.Definitions/`（namespace 暂保留 `MyriadCreation.Simulation.Definitions`，README 注明后续改名）；12 个有运行时依赖的定义留在 Simulation（反转清单见 README）；Modding 依赖运行时保留 Simulation；新建 `MyriadCreation.Content.asmdef`（refs 仅 Core），Simulation/Rendering/Infrastructure/UI/Tests 各加 Content 引用；Content 内 `using MyriadCreation.Simulation`=0、World 引用=0 | 编译通过（853cs 总数不变） |
| task-70 | economy-fixer | **B3-part1：GameWorld 拆 partial**：3246 行 God object → 8 文件（GameWorld.cs 声明 11 行 + Core 950/Generation 586/Simulation 1130/Succession 341/Queries 318/Persistence 88/Editing 115）；零逻辑改动（body 行多重集完全相等、155 块逐块 SHA 一致、字段/特性计数一致） | 编译通过 |
| task-69 | character-fixer | **CultureStage 阈值 JSON 化**：`StreamingAssets/Base/Culture/CultureStages.json`（5 阶段，数值严格=旧代码）；`CultureStageDef` + `CultureStageCatalog`（懒加载/校验/回退旧常量/查询 API）；`CultureStageEvolutionSystem` 改读 catalog、分支结构不变 | 编译通过 + CultureStageEvolutionDataTests 6 条 |

**验证（终值）**：编译 `COMPILE_OK`；全量 EditMode **462/462**；PlayMode 无图形/有图形各 **9/9**。

**下一批**：B3-part2（从 GameWorld 抽 App 程序集：Bootstrap/GameManager/SceneFlow/设置服务/地图编辑执行器；WorldState 逐步变纯数据容器）→ B4/B5/B6 Faith/Culture/Laws 独立 asmdef（先做 IFaith*/ICulture*/ILaw* 查询/命令契约反转）→ B7 World/Rendering 视图契约 → B8 契约/事件推广 + 架构门禁。Content namespace 改名（`MyriadCreation.Content.Definitions`）作为独立小批（全仓 using 批量替换）。法律 JSON 与硬编码双份保持等值测试防漂移。

### Round 21：Content 改名收尾 + App 程序集首刀 + 三系统契约层

| 卡 | Owner | 内容 | 结果 |
|---|---|---|---|
| task-71 | arch-auditor | **Content namespace 改名**：95 个 Content 文件 `MyriadCreation.Simulation.Definitions` → `MyriadCreation.Content.Definitions`；205 引用文件 using 更新（新增 205 / 删除 142 / 9 文件新旧并存）；10 处全限定引用改新；12 个留守定义保持旧 namespace | 编译通过 |
| task-73 | economy-fixer | **B3-part2a：设置服务抽到 `MyriadCreation.App` 程序集**：SettingsModel/SettingsService（+meta）迁 `Assets/Scripts/App/MyriadCreation.App/Settings/`，namespace `MyriadCreation.App.Settings`；新建 App asmdef（refs Core/Infrastructure/UnityEngine.UI，**不引 UI**）；UI 5 文件 using + UI/Tests asmdef 加 App；App 内 UI 引用=0；产出 `docs/architecture/APP_EXTRACTION_PLAN.md`（381 行：IAppFlow/IGameSession/ISettingsService 契约、UI→App 反向依赖 U1–U9、GameWorld 纯化切割线、S0–S7 分步与门禁） | 编译通过 |
| task-72 | character-fixer | **B4/B5/B6-part1：Faith/Culture/Laws 契约接口**：`Core.Contracts` 新增 IFaithQuery/Command、ICultureQuery/Command、ILawQuery/Command（纯 Core/ID/DTO，无 Simulation 引用）；Faith/Culture/Laws 各实现适配器；`GameWorld.Contracts.cs` 暴露 6 个懒加载 facade；首批消费者 GovernmentPanel（完整切 ILaw*）、UIManager 国教读取（IFaithQuery）；SystemContractsTests 5 条（Core 无 Simulation 依赖/委托一致性/命令生效） | 编译通过 + 5 测试 |

**验证（终值）**：编译 `COMPILE_OK`；全量 EditMode **467/467**；PlayMode 无图形/有图形各 **9/9**。

**下一批**：B3-part2b（按 APP_EXTRACTION_PLAN：先做 IAppFlow/IGameSession/ISettingsService 下沉 → GameManager/SceneFlow 迁 App → Bootstrap 最后 → RuntimeMapEditingController 改命令执行）→ B4/B5/B6-part2（Faith/Culture/Laws 各自独立 asmdef，依赖 Core.Contracts 接口 + Simulation 基础类型反转）→ B7 World/Rendering 视图契约 → B8 契约/事件推广 + 架构门禁。法律硬编码回退体量缩减独立小批。

### Round 22：App.Contracts 下沉 + GameManager/SceneFlow 迁入 App + 独立 asmdef 方案

| 卡 | Owner | 内容 | 结果 |
|---|---|---|---|
| task-74 | arch-auditor | **B3-part2b**：新建 `MyriadCreation.App.Contracts`（refs Core/World/Simulation）：`IAppFlow/IGameSession/ISettingsService/IGameUiHost/IMainMenuUi/ISettlementDetailUi/IUiFactory/AppServices`；UI 侧反向实现（`UIManager : IGameUiHost`、`MainMenuUI : IMainMenuUi`、`SettlementDetailPanel : ISettlementDetailUi`、新增 `UiFactory`），GameManager/SceneFlowController 改为只依赖 AppServices 接口并实现 `IGameSession`/`IAppFlow`；阶段 2 把 GameManager/SceneFlowController（+meta，GUID 保留）迁入 `Scripts/App/MyriadCreation.App/`，App asmdef += App.Contracts/World/Simulation/Rendering，**App 内 `using MyriadCreation.UI`=0**；Bootstrap 暂留 UI | 编译通过 |
| task-75 | character-fixer | **B4/B5/B6-part2 方案** `docs/architecture/ASMDEF_SPLIT_FAITH_CULTURE_LAWS.md`（494 行）：目标只加 `MyriadCreation.Faith/.Culture/.Laws`（refs Core+Content，互不引用；Simulation 反向引用）；环检测（State↔Laws、Culture/Faith/Laws↔Simulation）；Phase 0 前置（定义下沉 Content、`IContentLookup`、`IFaith/ICulture/ILawContext`、host glue 移出）；推荐顺序 **B6 Laws → B5 Culture → B4 Faith**；每系统最小首步 3 项、A/B 分步、R0–R3 回滚点；存档零语义变化（SaveVersion 16 不变） | 文档 |

**验证（终值）**：编译 `COMPILE_OK`；全量 EditMode **467/467**；PlayMode 无图形/有图形各 **9/9**（主菜单/开始游戏/设置经 App 迁移后仍绿）。

**下一批**：按 ASMDEF_SPLIT 文档 Phase 0 + **B6 Laws 首步**（host glue 8 文件移出、`ILawContext`、InheritanceLaw 配置下沉 Content）→ B5 Culture → B4 Faith；并行 B3-part2c `RuntimeMapEditingController` 改命令执行；Bootstrap 最后迁入 App；B7/B8 随后。

### Round 23：B6 Laws 首步 + B5 Culture 首步 + B3-part2c 地图编辑命令化

| 卡 | Owner | 内容 | 结果 |
|---|---|---|---|
| task-76 | arch-auditor | **B6 Laws Phase 0/首步**：8 个 host glue 文件从 `Simulation.Laws` 移入 `Simulation.LawHost`（LawContracts/LawDecreeView/LawEffectRuntime/LawSystem/JudicialSystem/DecreePropagationSystem/LawCompositionMigration/LawReform，namespace LawHost）；`Core.Contracts.ILawContext` + `WorldState/LawRealmContext` + `LawHost/LawRealmHost`（12 个 RealmData 入口迁 host），`LawAndDecreeSystem` 只留 13 个纯规则方法；`InheritanceLaw` + 6 枚举下沉 `Content.Definitions`，`DetermineHeir/GetHeirOrderedPool/IsSibling` 移到 host `InheritanceHeirSelection`；**Laws 内 WorldState/Systems/CharacterData 引用 = 0** | 编译通过 |
| task-78 | character-fixer | **B5 Culture 首步**：`CultureStageEvolutionSystem` 的 host 方法（FormRealmFromActor/ApplyPathBonuses/MonthlyTick/HasUrbanCenter）移到 `Simulation.CultureHost`；`Core.Contracts.ICultureContext` + `CultureContextAdapter`，`MonthlyTick(ICultureContext)` 生产路径已走 context；`StageEvolutionResult` 归 Culture；Culture 运行时 WorldState/Systems 引用 = 0；内容查询加 IContentLookup TODO | 编译通过 |
| task-77 | economy-fixer | **B3-part2c**：新增 `IMapEditCommandService`（App.Contracts）+ `AppServices.MapEdit`；App `WorldEditService`（普通操作 `GameWorld.PaintTerrain/CreateTile/RemoveTile/RecalculateDirty`、选区复用 MapEditHistory、Commit 复刻 MarkDirty）；`RuntimeMapEditingController` 保留输入/射线/选区，执行全走服务，删除直接写 tiles/RecalculateDirty/_history；GameManager/Bootstrap 注册兜底；`MapEditCommandServiceTests` 10 条 | 编译通过 + 10 测试 |

**验证（终值）**：编译 `COMPILE_OK`；全量 EditMode **477/477**；PlayMode 无图形/有图形各 **9/9**。

**下一批**：B6 Laws 翻 asmdef（Laws refs=Core/Content）→ B5 Culture 翻 asmdef → B4 Faith 首步/翻 asmdef；B7 World/Rendering 视图契约（Rendering 去 Simulation import，编辑已命令化）；B3-part2d Bootstrap 迁 App；B8 契约/事件推广 + 架构门禁；法律硬编码回退缩减独立小批。

### Round 24：Bootstrap 迁 App + Laws/Faith 独立 asmdef 翻转（Culture 待续）

| 项 | 内容 | 结果 |
|---|---|---|
| task-80 | economy-fixer：**Bootstrap 迁入 App**（`Assets/Scripts/App/MyriadCreation.App/Bootstrap.cs`，GUID 保留、namespace 暂 `MyriadCreation.UI`）；删除 UiFactory 直接注册 → UI 侧 `UiFactory` 自注册（`RuntimeInitializeOnLoadMethod(BeforeSceneLoad)`）；App 内 `using MyriadCreation.UI`=0；旧 `Assets/UI/MyriadCreation.UI/` 目录删除 | 编译通过 |
| task-81 | character-fixer（部分，Lead 收尾）：**Faith 定义下沉 Content**（ReligionCatalog/ReligionDef/FaithType/FaithOrganizationType/ReligionMapLevel/ReligionNodeType/DoctrineCategory/DoctrineOptionDef/SaintDef/DoctrinePool/Deity/EcclesiasticalTitle；DoctrinePool 因 ReligionCatalog 依赖一并下沉避免 Content→Faith 环）；**host glue 移出** `Simulation.FaithHost`（FaithContracts/CanonizationSystem/GreatHolyWarSystem/WarfareReligionSchedule/GreatHolyWarState/ReligionSimulationEventHandler）；`Core.Contracts.IFaithContext` + `FaithContextAdapter`；**Lead 翻 asmdef** `MyriadCreation.Faith`（refs=Core+Content） | 编译通过（2 文件 Faith 纯规则） |
| Lead | **Laws 翻 asmdef**：`MyriadCreation.Laws`（refs=Core+Content，32 文件；sim/UI/Tests asmdef += Laws）；**Culture 暂未翻**（CultureData 仍依赖 Simulation.Law/Modding/State/Scheduling，需 Phase0 继续下沉后再翻，避免成环） | 编译通过 |
| 故障处理 | arch-auditor 与 character-fixer 在收尾阶段 turn 失败（无 closing message），Lead 从磁盘状态接管：修复 LawRealmHost 中间态、补 Faith C 的 IFaithContext 检查、完成 Laws/Faith asmdef 翻转；Culture flip 延后 | — |

**验证（终值）**：编译 `COMPILE_OK`（Laws/Faith 独立程序集 + Bootstrap 迁 App 后）；全量 EditMode **477/477**；PlayMode 无图形/有图形各 **9/9**。

**依赖图现状**：`Core ← Content ← {Laws, Faith, Culture(仍在 Simulation)}`；`Simulation ← {Rendering, Infrastructure} ← App ← UI`（App.Contracts 被 App/UI/Rendering 引用）。Culture 翻 asmdef 的关键：CultureData 去 `Simulation.Laws/Modding/State/Scheduling`（IContentLookup + host 化）→ 再放 `MyriadCreation.Culture.asmdef`（refs=Core+Content）。

**后续**：B5 Culture 翻 asmdef 收尾 → B7 World/Rendering 视图契约 → B8 契约/事件推广 + 架构门禁（asmdef 反向=0、UI 禁 import Simulation）→ 法律硬编码回退体量缩减 → 文档同步。

### Round 25：Culture 独立 asmdef + B7 视图契约首批 + B8 架构门禁

| 卡 | Owner | 内容 | 结果 |
|---|---|---|---|
| task-82 | arch-auditor | **Culture 翻独立 asmdef**：`OfficeTitleEntry` 下沉 `Content.Definitions`；`IContentLookup`（Content.Definitions，仅 Core/Content 类型）+ `CultureHost/ContentLookupAdapter`；`CultureData` 5 个内容方法注入 IContentLookup；`CultureTransition` 抵抗度方法注入，`ListTraditionCandidates` 移 `CultureTransitionHost`；`CultureLineage`/`CultureSchedule` 移 `CultureHost`；新建 `MyriadCreation.Culture.asmdef`（refs=**Core+Content**），sim/Infrastructure/UI/Tests += Culture；Culture 内 `using Simulation`=0 | 编译通过 |
| task-84 | character-fixer | **B7-part1 Rendering 视图契约**：`Core.Contracts.IMapWorldView`（+MapCultureView/MapDiplomacyView，仅 Core 类型）、Simulation `MapWorldViewAdapter`（缓存宽高、越界安全）；`MapRenderer.SetWorldView` + legacy fallback helpers；**`MapRenderer.Colors.cs` 移除 11 个 Simulation using**；GameManager 在 StartNewGame/Async/LoadGame 注入 adapter；`MapWorldViewAdapterTests` 4 条 | 编译通过 + 4 测试 |
| task-83 | economy-fixer | **B8-part1 架构门禁**：`AsmdefArchitectureGateTests`（2 条）：asmdef 图无环、Content 边界、Laws/Faith(/Culture) refs ⊆ {Core,Content}、App 不引 UI、UI 引 App、分层序无向上引用（UI→Simulation 显式白名单+TODO）；发现 0 硬违规；观察项 Rendering→App.Contracts（编辑命令契约暂放 App.Contracts，建议 B7 下沉） | 编译通过 + 2 测试 |
| Lead | lead | 修复 B7 引起的 PlayMode 崩溃：`SetWorldView` 不再改 mapWidth/mapHeight（尺寸/纹理生命周期仍由 BindWorld/Start/Update 的 world 同步），避免"视图先注入、世界后绑定"时 `SetPixels` 越界；补 `MapRenderer.cs` Content using、Rendering asmdef += Culture | PlayMode 9/9 恢复 |

**验证（终值）**：编译 `COMPILE_OK`；全量 EditMode **483/483**；PlayMode 无图形/有图形各 **9/9**。

**依赖图**：`Core ← Content ← {Laws, Faith, Culture}`；`Content ← ... ← Simulation ← {Rendering, Infrastructure} ← App ← UI`；Laws/Faith/Culture 均为独立程序集且只依赖 Core+Content。

**下一批**：B7-part2（MapRenderer 核心/Texture/Nomadic/SettlementIconLayer/Cyowari/Editing 依次切 IMapWorldView 或扩展 IMapSettlementView，最后删 Rendering→Simulation 引用）→ B8-part2（Rendering→App.Contracts 编辑命令契约下沉/白名单、Culture 加入门禁分层）→ 法律硬编码回退体量缩减 → 文档同步。

### Round 26：B7 完成（Rendering 完全去 Simulation）+ B8 门禁收口 + 法律硬编码缩减

| 卡 | Owner | 内容 | 结果 |
|---|---|---|---|
| task-85 | character-fixer | **B7-part2 完成**：新增 `IMapSettlementView`/`IMapOverlayView`/`IMapRealmView`（Core.Contracts，仅 Core 类型）；`MapWorldViewAdapter` 实现 4 接口；`MapRenderer.cs/Texture.cs/Nomadic.cs` 全切视图；`SettlementIconLayer` 改 `IMapSettlementView` + `SetSettlementView`；`CyowariMapStyle` 用 `WorldView`；`RuntimeMapEditingController` 用 `IMapWorldView`（编辑命令保留）；删除 3 个 `*WorldCompat.cs`；**`Rendering.asmdef` 移除 `Simulation` 引用**，Rendering 源码 Simulation using = 0 | 编译通过 + adapter 7 条测试 |
| task-86 | economy-fixer | **B8-part2**：门禁强制 Culture（refs⊆{Core,Content}，加入分层 rank 2）；新增 `CheckSubsystemForbiddenRefs`（Laws/Faith/Culture 禁引上层程序集）；`App.Contracts` rank=6 + `Rendering→App.Contracts` 白名单 TODO（B7/B3 后下沉）；UI→Simulation 白名单保留 TODO；新增白名单回归测试 | 3 条测试 |
| task-87 | save-fixer | **法律硬编码缩减**：`DefaultLawCatalog` 397→293 行（-104），删除完整 9 组/32 法数组；JSON 为唯一完整来源，缺失时最小回退 2 组/5 法/1 法令；原完整目录迁测试基准 `LawCatalogReference.cs`（207 行，internal fixture）；`LawContentJsonTests` 改为 JSON↔fixture 逐字段比对 + 最小回退断言 | 测试通过 |
| Lead | lead | 修复 `MapWorldViewAdapter` 编译 usings（UnityEngine/Systems/World.Anchor/World/Content）、新增 Settlement/Overlay 空数据安全（`anchorId` 用字典键兜底、无行政区划时 overlay 返回 false）、`MapWorldViewAdapterTests` 补 `using MyriadCreation.World` | 编译 + 测试收口 |

**验证（终值）**：编译 `COMPILE_OK`；全量 EditMode **487/487**；PlayMode 无图形/有图形各 **9/9**。

**架构现状**：Rendering 已完全不依赖 Simulation（也不在 asmdef 引用），仅保留 World（SiteMode/SettlementForm 枚举）、Content、App.Contracts（编辑命令契约，白名单）；本轮已删除 Rendering asmdef 中无用的 Faith/Culture 引用。依赖图：`Core ← Content ← {Laws,Faith,Culture} ← World? ← Simulation ← {Rendering,Infrastructure} ← App ← UI`。

**遗留**：`MapRenderer` 不再有序列化 `GameWorld world` 字段（场景初始引用失效，靠 GameManager 注入）；`UIManager.cs` 仍调 `mapRenderer.BindWorld(newWorld)`（Obsolete 空壳，UI 单独换世界会绕过重绑）；`MapRenderer.Colors` 等仍有少量 `World` 枚举依赖。

### Round 27：B8 收口（Rendering 去 App.Contracts）+ Rendering→World 方案

| 卡 | Owner | 内容 | 结果 |
|---|---|---|---|
| task-88 | economy-fixer | **B8-final**：`IMapEditCommandService` 从 App.Contracts 下沉到 `Rendering/Editing/`（namespace `MyriadCreation.Rendering.Editing`；因 B7 后 Rendering 不引 Simulation，接口移除 `BindWorld(GameWorld)`，世界绑定由 App `WorldEditService` provider 完成）；RuntimeMapEditingController 改 `SetEditService` 显式注入、删 `AppServices.MapEdit` 回退；GameManager/Bootstrap 直接注入；删除 AppServices.MapEdit/RegisterMapEdit；`Rendering.asmdef` 删除 App.Contracts（refs=Core/World/Content）；门禁删白名单 + 新断言 `Rendering_DoesNotReferenceAppContracts`、`RenderingSource_DoesNotUseAppContractsNamespace` | 编译通过 + 测试 |
| task-89 | arch-auditor | **Rendering→World 方案** `docs/architecture/RENDERING_WORLD_CONTRACTS.md`（202 行）：实际 World 依赖仅 3 类型/10 行（SiteMode/SettlementForm/MapSelection），16 行 using 中 13 行残留；3a 枚举下沉 Core.Enums（~27 文件）→ 3b 清残留/DTO 收尾 → 3c MapSelection 下沉 Core.Contracts（7 使用方）→ 3d 删 World 引用（目标 `Rendering.refs={Core,Content}`）；含每步门禁/回滚点/降级路线（DTO 标志位） | 文档 |
| Lead | lead | 编译收口、修复 `MapWorldViewAdapter` 结算/overlay 空数据安全、docs 同步 | — |

**验证（终值）**：编译 `COMPILE_OK`；全量 EditMode **488/488**；PlayMode 无图形/有图形各 **9/9**。

**架构现状**：`Rendering.refs = {Core, World, Content}`（无 Simulation/App.Contracts）；App→Rendering 单向注入（view + edit service）；门禁只剩 UI→Simulation 白名单（TODO B7-part3 后删除）。

**下一批**：B7-part3（3a 枚举下沉 Core.Enums → 3b 清 Rendering 残留 using → 3c MapSelection 下沉 → 3d 删 Rendering→World 引用）→ 最终门禁只剩 UI→Simulation 待收 → 文档同步。

### Round 28：B7-part3 完成（Rendering 仅依赖 Core+Content）+ UI 去 Simulation 方案 + 架构文档现状版

| 卡 | Owner | 内容 | 结果 |
|---|---|---|---|
| task-90 | arch-auditor | **B7-part3**：3a `SiteMode/SettlementForm/SettlementLevel` 下沉 `Core.Enums`（3 文件+meta，23 消费方补 using，值/顺序不变）；3b 清 Rendering 13 行 0 类型引用的 World using；3c `MapSelection` 下沉 `Core.Contracts`（7 使用方）；3d **Rendering.asmdef refs 收敛 {Core, Content}**，门禁新增 `CheckRenderingBoundary` + `Rendering_ReferencesOnlyCoreAndContent`（禁止 World/Simulation/App.Contracts） | 编译通过 |
| task-91 | economy-fixer | **UI 去 Simulation 方案** `docs/architecture/UI_SIMULATION_DECOUPLING.md`（309 行）：UI 28 文件/5793 行，13 文件 69 行 import Simulation*（3 文件 19 行纯残留可先清），Top 类型 GameWorld 27/AdminDivision 13/CharacterData 8…；根阻塞 `IGameSession.World`/`IGameUiHost.BindWorld(GameWorld)`；新契约 IWorldReadModel/IRealmQuery/ISettlementQuery/ICharacterQuery/ISocietyQuery 等；4a 契约脚手架→4b 查询/DTO→4c 命令→4d 删 4 个 asmdef 引用+门禁白名单 | 文档 |
| task-92 | save-fixer | **架构文档现状版**：新建根 `docs/architecture/CURRENT_ARCHITECTURE.md`（程序集图/依赖/门禁/职责/遗留）、根 `docs/architecture/SYSTEM_OWNERSHIP.md` 当前版；`docs/audit/ARCH-COMPLETION-PLAN.md` 顶部执行状态摘要；`MyriadCreation/docs/architecture/SYSTEM_OWNERSHIP.md` 加 §0A 摘要 | 文档 |
| Lead | lead | 编译收口；后续把 3d 结果同步进文档 | — |

**验证（终值）**：编译 `COMPILE_OK`；全量 EditMode **489/489**；PlayMode 无图形/有图形各 **9/9**。

**当前程序集图（15 asmdef / 908 cs）**：Core(61) ← Content(117) ← {Laws(32), Faith(2), Culture(7)} ← World(90) ← Simulation(421) ← {Rendering(12: refs=Core+Content), Infrastructure(23)} ← {App.Contracts(8), App(7)} ← UI(28)。门禁：唯一白名单 `UI→Simulation`；Rendering 恰为 {Core,Content}；无环、Content/Laws/Faith/Culture 边界、App 不引 UI。

**剩余大项**：UI 去 Simulation（4a-4d，方案已就绪）→ GameWorld 组合根/数据容器收口（App/WorldState 纯化）→ Modding/ContentRegistry 下沉 Content → App.Contracts 去 World/Simulation 依赖 → Editor/Tests 门禁豁免例外收敛。

### Round 29：UI 4a/4b1 契约落地 + GameWorld 收口方案

| 卡 | Owner | 内容 | 结果 |
|---|---|---|---|
| task-93 | economy-fixer | **UI 4a**：删 3 个 import-only 文件的 Simulation using（UI 残留 13→10 文件/69→50 行）；新增 `Core.Contracts.IMapStatsView`（12 int）+ `Simulation.WorldState.WorldReadModelAdapter`；`IGameUiHost` 新增 `SetMapStatsView/SetLawServices`（World 签名未改）；`GameManager.InjectUiReadViews` 注入；`UIManager.Panels.UpdateTopBar` 日期/统计改读视图（无注入 "-"）；GovernmentPanel 改注入 `ILawQuery/ILawCommand`（`SetLawServices`，无注入兼容回退）；`UiDecouplingTests` 4 条 | 编译通过 + 4 测试 |
| task-94 | character-fixer | **UI 4b1**：`Core.Contracts.UiQueryContracts`（RealmSnapshotDto/AdminDivisionNodeDto/SettlementSnapshotDto/CharacterRowDto + IRealmQuery/ISettlementQuery/ICharacterQuery）；Simulation host `RealmQueryAdapter/SettlementQueryAdapter/CharacterQueryAdapter`（空世界安全、DTO 无泄漏）；`UiQueryContractsTests` 4 条（字段一致/颜色枚举映射/realm 过滤/越界安全） | 编译通过 + 4 测试 |
| task-95 | arch-auditor | **GameWorld 收口方案** `docs/architecture/GAMEWORLD_COMPOSITION_SPLIT.md`（207 行）：8 partial 3600 行职责统计（组合根 ~30 成员/生成 ~20/字段 60+/查询 ~25）；调用方（App/UI/SaveSystem 直接 AddComponent<GameWorld>）；推荐 GameWorld 保留数据+查询+MonoBehaviour 宿主，新增 WorldServices/WorldCompositionRoot/WorldGenerationService/WorldLoadInitializer/WorldFactory（Factory 放 Simulation host 避免 Infra→App 反向）；4 步（只读接口→生成服务→组合根→App/Save 切换）+ 门禁/回滚点/最小首步 | 文档 |

**验证（终值）**：编译 `COMPILE_OK`；全量 EditMode **497/497**；PlayMode 无图形/有图形各 **9/9**。

**UI 去 Simulation 进度**：4a 完成（地图统计/政府面板已走 Core.Contracts），4b1 契约+适配器完成（Realm/Settlement/Character 查询）；剩 4b2（把 RealmOverviewText/SettlementPeopleText/UIManager 等切到新契约）、4c 命令接口收口、4d 删 UI 对 Simulation/Laws/Faith/Culture 四个 asmdef 引用+门禁白名单。

**下一步**：GameWorld Step1（IWorldReadModel + 只读接口）或 UI 4b2 面板切换；随后 4c/4d、B3 组合根收口。

### Round 30：WorldReadModel 聚合 + UI 4b2/4c

| 卡 | Owner | 内容 | 结果 |
|---|---|---|---|
| task-96 | arch-auditor | `Core.Contracts.IWorldReadModel : IMapStatsView, IRealmQuery, ISettlementQuery, ICharacterQuery`（纯组合）；`Simulation.WorldState.WorldReadModel(GameWorld)` 组合现有 4 适配器（零复制、null 安全、世界切换新建）；`WorldReadModelTests` 7 条（各接口与独立适配器一致/null 安全/重复查询一致） | 编译通过 + 7 测试 |
| task-98 | economy-fixer | **UI 4b2**：`IGameUiHost.SetWorldReadModel`；UIManager 缓存并同步 `_mapStatsView`；`GameManager.InjectUiReadViews` 注入 `WorldReadModel`（覆盖 Awake/开局/读档/Bootstrap）；`UIManager.Panels` 顶栏政权名/日期/地图统计/聚落锚点优先走聚合视图（fallback 旧 world）；`RealmOverviewText.Build(IWorldReadModel,...)` 新重载；`SettlementPeopleText.TryFindSettlementAnchorAtTile` 新入口；`UiDecouplingTests` 7 条 | 编译通过 + 7 测试 |
| task-97 | character-fixer | **UI 4c**：扫描 UI 实际 GameWorld 写点（仅 GovernmentPanel `IssueDecreeOrders` 1 处）；`Core.Contracts.IDecreeCommand`（IssueDecreeOrders/TickDecreeOrders，不泄漏 Laws 类型）；`DecreeCommandAdapter` + `GameWorld.UiCommands.cs`（`DecreeCommand`/`CreateDecreeCommand`/`TickDecreeOrdersNow`，空保护）；`UiCommandContractsTests` 3 条 | 编译通过 + 3 测试 |
| Lead | lead | 编译 + 全量测试收口 | — |

**验证（终值）**：编译 `COMPILE_OK`；全量 EditMode **510/510**；PlayMode 无图形/有图形各 **9/9**。

**UI 去 Simulation 进度**：4a 完成、4b1 契约+适配器完成、4b2 聚合注入+首批只读点切换完成、4c 命令契约完成；**UI 残留 Simulation using 10 文件/50 行**（旧重载/旧字段兼容 + 聚合契约缺 Faith/Society/People/Culture/Innovation 查询）。

**下一批**：4b3 扩展 `IWorldReadModel`（`IFaithQuery` 面板扩展、`ISocietyQuery`、按 anchor 的 `ISettlementPeopleQuery`、`ICultureQuery`/`IInnovationQuery`）→ 把旧 GameWorld 重载拆 legacy partial → 4d 删 UI 对 Simulation/Laws/Faith/Culture 四个 asmdef 引用 + 门禁白名单 → GameWorld Step1/IWorldReadModel 宿主实现 → B3 组合根。

### Round 31：IWorldReadModel 扩展 + UI legacy 拆分

| 卡 | Owner | 内容 | 结果 |
|---|---|---|---|
| task-99 | character-fixer | `IWorldReadModel` 扩展为 9 接口聚合（+IFaithQuery/ISocietyQuery/ISettlementPeopleQuery/ICultureQuery/IInnovationQuery）；新增 `SocietySnapshotDto/SocialClassSnapshotDto/ClassNeedScoreDto`、`ISettlementPeopleQuery`（按 anchor 已知/公共人物，复用 CharacterRowDto）、`InnovationSnapshotDto/IInnovationQuery`；`SocietyQueryAdapter/SettlementPeopleQueryAdapter/InnovationQueryAdapter`；`WorldReadModel` 复用 `world.FaithQuery/CultureQuery` 并 null 降级；`UiReadModelExtensionTests` 6 条 | 编译通过 + 6 测试 |
| task-100 | economy-fixer | **UI legacy 拆分**：5 个 Presentation 文本 helper 改 `partial`，旧 `GameWorld/RealmData/FaithSystem` 重载+helper 移到 `*Legacy.cs`（RealmOverview/SettlementPeople/ReligionPanel/RealmDivision/ClassPanel，行为零改动、body 行等值断言通过）；主文件 Simulation using 清零；Legacy 标注"4d 删除候选"；Panels 4 文件评估后不做危险拆分（serialized/生命周期耦合，已列原因） | 编译通过 |
| Lead | lead | 编译 + 全量测试；修正 `UiReadModelExtensionTests` 的"不存在 innovation id"用例（999 在真实内容中存在，改用 int.MaxValue） | 516/516 |

**验证（终值）**：编译 `COMPILE_OK`；全量 EditMode **516/516**；PlayMode 无图形/有图形各 **9/9**。

**已知缺口**：`IFaithQuery` 仍缺 `hierarchyLevel`/`CanDeclareGreatHolyWar()`（ReligionPanel 迁移需要）；Panels（UIManager/Government/SettlementDetail）旧 GameWorld 字段需"partial + 生命周期迁移"专项才能拆 Legacy。

**下一批**：补 IFaithQuery 缺口 + 4b4 把 ReligionPanel/Society/Class 等切到扩展聚合 → Panels partial/字段迁移 → 4d 删 UI 对 Simulation/Laws/Faith/Culture 引用 + 门禁白名单 → GameWorld Step1（宿主实现 IWorldReadModel）→ B3 组合根。

### Round 32：Faith 契约补齐 + GameWorld 实现 IWorldReadModel + UI 4b4

| 卡 | Owner | 内容 | 结果 |
|---|---|---|---|
| task-101 | character-fixer | **IFaithQuery 扩展**：FaithSnapshotDto + faithType/organizationType/hierarchyLevel/authority/isStateReligion/highPriestCharacterId；新增 FaithSaintDto/FaithDoctrineDto；新方法 CanDeclareGreatHolyWar(realmId)/GetSaints(faithId)/GetDoctrines(faithId)；FaithHost 实现（CanonizationSystem/ReligionCatalog+DoctrinePool 回退，null 安全）、WorldReadModel 透传；`FaithQueryExtensionTests` 6 条 | 编译通过 + 6 测试 |
| task-103 | arch-auditor | **GameWorld 实现 IWorldReadModel**（B3 Step1）：`GameWorld.cs` 声明 `: IWorldReadModel`，新增 `GameWorld.ReadModel.cs`——懒创建 WorldReadModel，9 接口 **42 成员显式接口实现**全部委托（只读、无缓存、无序列化字段）；GameManager 注入路径保持；`GameWorldReadModelTests` 6 条（接口/一致性/未初始化/new GameWorld 安全） | 编译通过 + 6 测试 |
| task-102 | economy-fixer | **UI 4b4**：ClassPanelText/SocietyPanelText 增加契约重载（SocietySnapshotDto/SocialClassSnapshotDto/ClassNeedScoreDto；SocietyPanel 旧版迁 `SocietyPanelLegacy.cs`）；UIManager.Panels 的 Society/Religion/People/Culture 切换（Religion 用 IFaithQuery，People 用 ISettlementPeopleQuery，创新已研究数用 IInnovationQuery，文化用 ICultureQuery/SettlementSnapshotDto）；`UiDecouplingTests` 增至 10 条 | 编译通过 + 10 测试 |
| Lead | lead | 编译 + 全量测试；修复 FakeFaithQuery 接口新成员、FaithContracts null-world 泄漏全局注册表（GetSaints/GetDoctrines 加 `_world == null` 守卫） | 534/534 |

**验证（终值）**：编译 `COMPILE_OK`；全量 EditMode **534/534**；PlayMode 无图形/有图形各 **9/9**。

**UI 残留**：现代化（非 Legacy）UI Simulation using **4 文件 / 23 行**（UIManager.cs/UIManager.Panels.cs/GovernmentPanel.cs/SettlementDetailPanel.cs）——"Legacy 除外 ≤7"目标达成；含 Legacy 共 10 文件/50 行（6 个 Legacy 保留回退）。

**已知内容缺口**：ReligionPanel 新入口缺教统/改革支柱/圣人列表/主保圣人（FaithSnapshotDto 未含全部字段）；Society 新入口缺派系/政变区；创新树/资源可采判定仍走旧 `InnovationTree`（IInnovationQuery 无等价判定）。

**下一批**：4b5 补聚合 DTO 缺口（Religion 教统/圣人、Society 派系/政变、创新树判定）→ Panels（UIManager/GovernmentPanel/SettlementDetailPanel）partial+字段迁移 → 4d 删 UI 对 Simulation/Laws/Faith/Culture 引用 + 门禁白名单 → B3 组合根（WorldFactory/WorldGenerationService）。

### Round 33：聚合 DTO 缺口补齐 + Panels 契约化 + B3 Step2 服务层

| 卡 | Owner | 内容 | 结果 |
|---|---|---|---|
| task-104 | character-fixer | **4b5a**：FaithSnapshotDto + hierarchyName/reformPillars，IFaithQuery + GetPatronSaintId/Name；新增 `IFactionQuery/FactionSnapshotDto`、`IRegimeQuery/RegimeSnapshotDto`（coupRisk 推导、juncture/activeEvents）；IInnovationQuery + GetAvailableInnovations/GetPrerequisiteInnovationIds/CanResearch/IsTechAccessibleForGoods/IsResourceDepthAccessible/CanAccessResource；适配器 + WorldReadModel 组合 + 测试扩展 | 编译通过 |
| task-105 | economy-fixer | **4b5b**：UIManager/UIManager.Panels/GovernmentPanel/SettlementDetailPanel 拆 `*Legacy.cs` partial（serialized GameWorld 字段/旧逻辑集中到 Legacy）；主文件走 `IWorldReadModel`/`ILaw*`/`IDecreeCommand`/`ISettlementQuery` 契约；**现代 UI Simulation using = 0 文件/0 行**（全 UI 含 6+4 Legacy = 10 文件/50 行）；UiDecouplingTests 15 条 | 编译通过 |
| task-106 | arch-auditor | **B3-Step2**：`WorldGenerationService`（生成管线薄委托）、`WorldFactory`（Create/Destroy）、`WorldCompositionRoot`（子系统/调度/AI/Faith 薄入口）；`WorldServicesTests` 6 条；报告 App/Save/Tests 迁移点 | 编译通过 + 6 测试 |
| Lead | lead | 编译收口：补 GameWorld 新接口成员（patron/faction/regime/innovation 8 项）、UIManager `: IGameUiHost` 声明、GovernmentPanel Legacy int 返回处理、FakeFaithQuery patron 成员、SettlementDetail 查询分支 fallback（注入 query 查不到锚点且本地有 world 时回退）、创新测试改为查找而非精确计数 | 546/546 |

**验证（终值）**：编译 `COMPILE_OK`；全量 EditMode **546/546**；PlayMode 无图形/有图形各 **9/9**。

**架构现状**：UI 现代文件 0 行 Simulation using（Legacy 文件保留回退，4d 删除候选）；GameWorld 实现 IWorldReadModel（42+8 成员委托）；B3 Step2 服务层就位（Step3 组合根/Step4 App+Save 切换待做）。

**下一批**：4d（删 UI 对 Simulation/Laws/Faith/Culture asmdef 引用 + 门禁白名单 + 删 Legacy）→ GameManager 注入 IDecreeCommand → B3 Step3/4（组合根 1:1 hooks、App/Save 走 Factory/GenerationService/CompositionRoot）→ 法律硬编码回退体量再评估。

### Round 34：UI 4d 完成（UI 程序集彻底去 Simulation 家族）

| 卡 | Owner | 内容 | 结果 |
|---|---|---|---|
| task-107 | arch-auditor | **App 契约去 GameWorld**：`IGameUiHost.BindWorld/NotifyWorldChanged` → `IWorldReadModel`，新增 `SetDecreeCommand(IDecreeCommand)`；`ISettlementDetailUi.BindWorld` → `IWorldReadModel`；两个 UI 契约内 GameWorld token=0；GameManager 读档/聚落面板重绑传 `(IWorldReadModel)world`、`InjectUiReadViews` 注入 `world.DecreeCommand` | 编译通过 |
| task-108 | economy-fixer | **UI 程序集去 Simulation 家族**：删除全部 10 个 `*Legacy.cs`；UIManager/GovernmentPanel/SettlementDetailPanel 实现新签名与契约渲染（无 view 显示"-"/"无数据"不抛）；`[SerializeField] GameWorld world` 删除；**`MyriadCreation.UI.asmdef` 删除 Simulation/Laws/Faith/Culture 引用**；`Assets/UI` 内 Simulation 家族 using=0、Legacy=0；UiDecouplingTests 16 条 + 源码扫描断言 | 编译通过 |
| Lead | lead | 收口：MainMenuUI 用 `gm.IsPlaying` 替代 `gm.World`、AdminDivisionUiTextTests using Systems、`OpenGovernmentPanel` 改 public、SettlementDetail PlayMode 断言改契约字段（所属政权/守军） | 测试全绿 |

**验证（终值）**：编译 `COMPILE_OK`；全量 EditMode **546/546**；PlayMode 无图形/有图形各 **9/9**。

**程序集现状**：`MyriadCreation.UI.asmdef` refs = {Core, Rendering, Infrastructure, World, Content, App, App.Contracts, UGUI, TMP}；**不含 Simulation/Laws/Faith/Culture**。UI 只通过 `IWorldReadModel`/`ILawQuery/Command`/`IDecreeCommand`/`ISettlementQuery`/`IAppFlow`/`IGameSession` 等契约访问模拟层；Legacy 删除后无回退。

**已知回归风险/后续**：无契约覆盖的地块详情/角色详情/家族树面板在无 view 时改为空/最小实现（PlayMode 旧冒烟未覆盖，需后续补 UI 契约）；`ISettlementDetailUi` DTO 渲染字段少于旧文本（无市场库存等）；门禁 `AsmdefArchitectureGateTests` 的 UI→Simulation 白名单已不再需要，下一批删除并加"UI 不含 Simulation 家族"断言。

**下一批**：架构门禁更新（删 UI→Simulation 白名单 + 断言 UI asmdef 不含四个程序集）→ B3 Step3/4（组合根 1:1 hooks、App/Save 走 WorldFactory/GenerationService/CompositionRoot、SaveSystem 读档创建世界）→ 法律回退体量再评估 → 文档同步。

### Round 35：门禁 UI 边界 + B3 Step3/4 + 法律降级健壮性

| 卡 | Owner | 内容 | 结果 |
|---|---|---|---|
| task-109 | economy-fixer | **门禁收口**：删除 `KnownTransitionEdges` 的 UI→Simulation 白名单与旧测试；`CheckLayering` 不再跳过白名单；新增 `CheckUiBoundary`（UI 必含 App、禁含 Simulation/Laws/Faith/Culture）+ `UI_DoesNotReferenceSimulationFamily`；当前 15 asmdef 静态模拟 0 违规 | 编译通过 + 4 门禁测试 |
| task-110 | arch-auditor | **B3-Step3/4**：`WorldFactory.Create(host, options, mapWidth, mapHeight)` + 新增 `CreateEmpty`（不 InitializeWorld）；GameManager 世界创建/生成改 WorldFactory/WorldGenerationService；SaveSystem 读档创建改 `CreateEmpty`、`ReinitializeSubsystems/ApplySetupOptions/PrepareLoadedWorld` 改 `WorldCompositionRoot`；GameManager/SaveSystem 内旧直调=0；WorldServicesTests +2 | 编译通过 + 8 测试 |
| task-111 | character-fixer（失败后 Lead 收尾） | **法律降级健壮性**：`LawCompositionMigration` 对未知 lawId `TryGet` 跳过+LogWarning；未知 lawId 查询/效果计算不抛返回安全默认；`LawFallbackRobustnessTests` 4 条（无 JSON 最小回退/LastError 可读、未知 lawId+null 列表不抛、旧 LawComposition 迁移跳过缺失、还原真 JSON 幂等） | 编译通过 + 4 测试 |
| Lead | lead | 收口编译+全量测试；确认 character 失败前已写盘的工作（LawAndDecreeSystem/DefaultLawCatalog/LawCompositionMigration/测试）可编译并全绿 | 552/552 |

**验证（终值）**：编译 `COMPILE_OK`；全量 EditMode **552/552**；PlayMode 无图形/有图形各 **9/9**。

**架构现状**：UI asmdef 不含 Simulation 家族（硬门禁）；GameWorld/App/Save 经 WorldFactory/WorldGenerationService/WorldCompositionRoot 创建/生成/重初始化；法律 JSON 缺失时最小回退且旧档迁移不崩。

**下一批**：GameWorld 组合根 1:1 抽离（WorldCompositionRoot 目前映射偏宽，Step3 需 hooks）；CompositionRoot 损坏档语义确认（tiles 空时回滚）；Rendering→World 已零；文档同步（CURRENT_ARCHITECTURE/门禁状态）；可开始"组合根/数据容器"主工程（GameWorld 纯化）。

### Round 36：B3 CompositionRoot 精确化 + UI 4e 地块/角色面板恢复

| 卡 | Owner | 内容 | 结果 |
|---|---|---|---|
| task-112 | arch-auditor | **B3 Step3b**：GameWorld 新增 `GameWorld.HostHooks.cs`（internal：InitializeSubsystems/EnsureAIControllers/InitializeFaithSystems/RegisterSimulationSchedules/PrepareLoadedWorld 的 `*ForHost` hooks）；`WorldCompositionRoot` 1:1 精确映射（不再借宽语义）+ 新增 `PrepareLoadedWorld`；SaveSystem 调用对应调整、读档顺序不变；`WorldServicesTests` 10 条（root vs 直调 hooks 逐项一致、tiles==null 损坏档 6 入口不抛、幂等） | 编译通过 + 10 测试 |
| task-113 | character-fixer | **UI 4e**：`ITileInfoQuery/TileInfoDto/TileResourceDto`、`ICharacterDetailQuery/CharacterDetailDto/FamilyMemberDto` 加入 `IWorldReadModel`；`TileInfoQueryAdapter`/`CharacterDetailQueryAdapter`（null/越界安全）；UIManager.Panels 的地块详情/角色详情/角色列表/家族树改用 view 渲染（无 view 显示 "-"）；`UiTileCharacterPanelsTests` 5 条 | 编译通过 + 5 测试 |
| task-114 | economy-fixer | **文档同步**：CURRENT_ARCHITECTURE/SYSTEM_OWNERSHIP/ARCH-COMPLETION-PLAN 更新到 Round 35（15 asmdef/门禁/批次/遗留/基线） | 文档 |
| Lead | lead | 补 GameWorld 的 ITileInfoQuery/ICharacterDetailQuery 成员委托；编译 + 全量测试 | 559/559 |

**验证（终值）**：编译 `COMPILE_OK`；全量 EditMode **559/559**；PlayMode 无图形/有图形各 **9/9**。

**架构现状**：GameWorld = 数据+查询+契约适配器+MonoBehaviour 宿主（IWorldReadModel 16 接口全部实现）；App/Save 经 WorldFactory/WorldGenerationService/WorldCompositionRoot（1:1 hooks）；UI 仅依赖 Core/App 契约（不含 Simulation 家族）；地块/角色/家族树面板恢复（契约数据）。

**下一批**：GameWorld 生成管线抽 `WorldGenerationService` 的 1:1 hooks（目前生成方法仍直接调用 GameWorld，服务为包装）；`App.Contracts` 去 Simulation/World 依赖；Modding/ContentRegistry 下沉 Content；Editor/Tests 门禁豁免收敛；法律最小回退再评估。

### Round 37：App.Contracts 收敛 Core + 生成 hooks 1:1 + Content 迁移方案

| 卡 | Owner | 内容 | 结果 |
|---|---|---|---|
| task-115 | arch-auditor | **B3-Step3c**：GameWorld 新增 `GameWorld.GenerationHooks.cs` 9 个 internal `*ForHost` 生成 hooks；`WorldGenerationService` 9 方法 1:1 映射（直调 public 生成 = 0）；WorldServicesTests +2（同 seed 地形生成等效/省份聚落结构不变量） | 编译通过 + 12 tests |
| task-116 | economy-fixer | **App.Contracts 收敛 Core**：新增 `Core.Contracts.NewGameRequestDto`（map/seed/wrap/content + 生成数量/自然生成率；sentinel 保留默认语义）；`IAppFlow`/`IGameSession` 改 DTO 入口，移除 `World/WorldChanged/WorldSetupOptions` 重载、增 `HasWorld`；App `NewGameRequestMapper` DTO↔WorldSetupOptions；GameManager/SceneFlow 保留 concrete 旧重载供 UI（UI 零改动）；**App.Contracts.asmdef refs = ["MyriadCreation.Core"]**、源码 World/Simulation using=0 | 编译通过 |
| task-117 | character-fixer | **Content 迁移方案** `docs/architecture/CONTENT_ASSEMBLY_MIGRATION.md`（416 行）：Simulation.Modding 50 文件/2804 行，94% 纯内容可直接迁；耦合仅 3 文件（ContentRegistry/ContentRuntime/ContentResolvers）；外部类型 RaceData/DiscoveryGroups/CultureData/BiomeDef/BiomeRegistry；方案 IContentLookup/IContentCatalog + host lookup 适配器；分步 P0-P4 + 每步门禁/回滚/最小首步 | 文档 |
| Lead | lead | 修复生成等效测试的非确定性（省/聚落跨世界随机不逐点一致，改为结构不变量断言） | 561/561 |

**验证（终值）**：编译 `COMPILE_OK`；全量 EditMode **561/561**；PlayMode 无图形/有图形各 **9/9**。

**架构现状**：App.Contracts 仅引 Core；UI 仅依赖 Core/App 契约；GameWorld 宿主实现 IWorldReadModel 全部接口；生成/子系统/读档经 Services（1:1 hooks）；Rendering 仅 Core+Content；Laws/Faith/Culture 仅 Core+Content。

**下一批**：Content 迁移 P0-P2（IContentLookup/IContentCatalog 契约 + 20 纯定义文件迁 Content + JSON loader 拆分）；App.Contracts 的 `HasWorld` 等兼容清理；Editor/Tests 门禁豁免收敛；文档同步（CURRENT_ARCHITECTURE/门禁/Content 计划）。

### Round 38：Content P0/P1a + 门禁豁免显式化 + App 兼容清理

| 卡 | Owner | 内容 | 结果 |
|---|---|---|---|
| task-118 | arch-auditor | **Content P0/P1a**：`Core.Contracts.IContentLookup<TKey,TValue>`（TryGet/Keys/All）+ `IContentCatalog`/`ContentKindInfo`（Generation/Kinds/DescribeKinds/TryGetRaw）；**20 个纯定义文件**从 Simulation.Modding 迁 `Content/MyriadCreation.Content.Modding/`（.cs+.meta，GUID 保留，namespace 暂留 `Simulation.Modding` 待 P4 统一）；Content asmdef 仍仅 Core；Content 内 Simulation/World using=0、cs/meta 20/20；Simulation.Modding 50→30 | 编译通过 |
| task-119 | economy-fixer | **门禁豁免显式化**：`ExplicitlyExemptAsmdefs={Editor,Tests,Tests.PlayMode}` + `IsTestOrEditorAsmdef`；新增 `RuntimeAssemblies_DoNotReferenceTestsOrEditor`、`Editor_And_Tests_AreExplicitlyExempted`；共 6 条门禁测试 | 编译通过 + 6 测试 |
| task-120 | character-fixer | **App 兼容清理**：删除无订阅的 `GameManager.WorldChanged` + 3 处 Invoke；`HasWorld` 替换确认；`NewGameRequestDto` 增 4 个 sentinel 常量并用于默认值/比较；保留 concrete `World` 与全部 `WorldSetupOptions` 旧重载（UI/PlayMode 用）；新增 `AppSessionContractTests` 6 条；记录 IGameSession 暂无接口调用的成员（未删） | 编译通过 + 6 测试 |
| Lead | lead | 修复 `IContentLookup` 泛型变体错误（TKey 有 Keys getter 不能 `in`）；编译 + 全量测试 | 568/568 |

**验证（终值）**：编译 `COMPILE_OK`；全量 EditMode **568/568**；PlayMode 无图形/有图形各 **9/9**。

**架构现状**：Content 程序集 137 文件（97 Definitions + 20 Modding 纯定义 + 20 其它）；App.Contracts 仅 Core；UI 仅 Core/App 契约；Rendering 仅 Core+Content；Laws/Faith/Culture 仅 Core+Content；门禁含测试/编辑器显式豁免与运行时禁反引。

**下一批**：Content P1b（27 个纯基础设施文件迁 Content）→ P2（JSON 加载/定义索引拆 Content、host registry facade）→ P3/P4（Resolvers host 化 + namespace 统一 Content.Modding）；随后 UI 缺失面板契约（地块/角色已恢复，家族树/角色列表可用）；文档同步 Content 进度。

### Round 39：Assets 全盘资产整改（字体/TMP/音频/死资源/顶层收口）

| 步骤 | 内容 | 结果 |
|---|---|---|
| 全盘盘点 | arch-auditor 只读扫描 Assets 全目录，产出 `docs/architecture/ASSET_LAYOUT_PLAN.md`（314 行）：8 类放错、逐文件清单、引用方式、P0-P7 分步 | 文档 |
| P0 | 新增 `Assets/ASSET_README.md`（顶层职责/Resources/字体/音频/TMP 规则）；盘点/冻结清单 | ✅ |
| P1 字体 | `Assets/Fonts` 迁 `Assets/Art/Fonts`；唯一 simhei 副本 `Art/Fonts/Resources/simhei.ttf`（`Resources.Load("simhei")`），Main.unity 字体 GUID 改指该副本；删除重复 ttf（-9.3MB 与重复 meta）；4 个 SourceHan 归档 Art/Fonts | ✅ |
| P2 TMP | `Assets/TextMesh Pro` → `Assets/Art/TextMesh Pro`；`TMPFontUtility`/`GovernmentPanel` 字体路径改 `simhei`；`TmpSettingsCreator` 硬编码 `Assets/Art/TextMesh Pro/Resources`；TMP Settings 按 Resources 名加载 | ✅ |
| P3 音频 | `Resources/Audio/UI/*` → `Assets/Audio/Resources/UI/*`；`UiSfx` 路径改 `UI/<name>`；新增 `Audio/Resources/Music/README.md`；删除死代码 `MusicPlayer.cs`（0 引用）并清理 SettingsService；删除空 `Assets/Resources` 根 | ✅ |
| P4 死资源/空目录 | 删 `New Sprite Atlas.spriteatlas`、空 `Prefabs/Settings/ScriptableObjects`（内容迁 Data）、`Editor/MapEditing`、`StreamingAssets/Music`、空 Audio 占位、`.gitkeep`（Data/Editor/StreamingAssets）；`Art` 美术分类占位保留 | ✅ |
| P5 数据 | `DefaultWorldConfig.asset` → `Assets/Data/WorldConfig/`（场景 GUID 引用不变） | ✅ |
| P6/P7 | 顶层收敛为 Art/Audio/Data/Editor/Scenes/Scripts/StreamingAssets/Tests/UI + csc.rsp；Editor/UI/StreamingAssets 规则写入 ASSET_README | ✅ |

**验证（终值）**：编译 `COMPILE_OK`；全量 EditMode **568/568**；PlayMode 无图形/有图形各 **9/9**；备份 `D:/TEMP/assets-reorg-backup.tar.gz`（78MB，含迁移前字体/TMP/音频/Data）。

**明确剩余（按计划逐条执行，不再笼统"下一批"）**：
1. SourceHan 4 个未引用字体（82MB）——问用户是删除还是保留归档（当前保留在 Art/Fonts）。
2. `Art/TextMesh Pro` 是否改为 `ThirdParty/` vendor 目录——当前按"视觉资源属美术"保留 Art，需用户拍板。
3. `Editor/` 三套工具（MyriadCreation.Editor/EditorTools/ContentAuthoring/TerrainPainter）是否按 Build/TMP/Tools 子目录细分。
4. 其余架构：Content P1b（27 纯基础设施迁 Content，按 CONTENT_ASSEMBLY_MIGRATION 文档逐文件）。
5. `StreamingAssets/Base|Mods` 硬编码路径（8+ 处）收敛为常量（需代码改动）。
6. 文档：CURRENT_ARCHITECTURE 增加"资产目录规范"引用。

### Round 40：Editor 工具归位 + 字体设置项 + 字体资源归美术

| 步骤 | 内容 | 结果 |
|---|---|---|
| Editor 归位 | `Editor/MyriadCreation.Editor`、`EditorTools`、`ContentAuthoring`、`TerrainPainter` 四目录重组为 `Editor/Tools/`（BatchCompileCheck/AutoOpenMainScene/GraphicsApiFix/TerrainPainter）、`Editor/Content/`（ContentPackageExporter）、`Editor/TMP/`（TmpSettingsCreator）；新增 `Editor/README.md`；asmdef 仍在 Editor 根统一管理 | ✅ 编译 |
| 字体保留 | 4 个 SourceHan OTF 保留（用户决定）；`Art/Fonts/README.md` 说明：simhei 为运行时唯一字体，SourceHan 为设计资产并纳入设置字体选项 | ✅ |
| 字体设置项 | `SettingsModel.FontChoice`（0 系统黑体/1 思源黑体/2 思源宋体，键 `MyriadCreation.Settings.FontChoice`，默认/夹取/持久化/AllKeys）；`SettingsPanel` 新增「界面字体」下拉；`TMPFontUtility` 按 choice 缓存 TMP_FontAsset（simhei-SDF/SourceHanSans-SDF/SourceHanSerif-SDF），`ApplyFontChoice` 全场景应用、失败回退 simhei；`UIManager.Start`/`MainMenuUI.Build` 应用已保存字体 | ✅ 编译 |
| 字体资源归 Art | SourceHan OTF 移入 `Assets/Art/Fonts/Resources/Fonts/`，运行时可 `Resources.Load("Fonts/SourceHanSansSC-Regular")` 等；simhei 仍在 `Art/Fonts/Resources/simhei.ttf` | ✅ |
| 测试 | `SettingsModelTests` +2（FontChoice 默认/夹取、SaveLoad/AllKeys）；新增 `TMPFontUtilityTests` 4 条（映射/非法回退/缓存） | EditMode 574/574 |

**验证（终值）**：编译 `COMPILE_OK`；全量 EditMode **574/574**；PlayMode 无图形/有图形各 **9/9**。

**音频说明**：`Assets/Audio/Resources/{UI,Music}` 是 Unity 运行时动态加载根（`UiSfx` 用 `Resources.Load("UI/*")`、`MusicPlayerSystem` 用 `Resources.LoadAll("Music")`），不是旧 `Assets/Resources` 残留；旧 `Assets/Resources/Audio`、`Audio/Music|SFX` 空占位已删。若要求 Audio 下彻底无 Resources，需改 StreamingAssets/Addressables 加载链路，另行确认。

**下一轮按小步执行（不笼统）**：
1. `Audio/Resources` 去留由用户拍板；若保留，补 `Audio/README.md` 加载说明（已写）。
2. 字体切换性能：首次切换 4096 SDF 生成耗时监控与 `TMPFontUtility` 预热（MainMenu 提前生成）。
3. Content P1b 27 文件迁移（按文档逐文件，迁一批编译测试一批）。
4. StreamingAssets/Base|Mods 硬编码路径收敛常量。
5. 资产门禁测试：新增 EditMode 断言"无 Assets/Fonts、Assets/TextMesh Pro、Assets/Resources 根、重复字体、空占位目录"（防止回归）。

### Round 41：资产门禁 + 字体预热 + Content P1b + TMP 断引用修复

| 步骤 | 内容 | 结果 |
|---|---|---|
| 资产门禁 | 新增 `AssetLayoutGateTests`（7 条）：禁 `Assets/Fonts/TextMesh Pro/Resources 根/Prefabs/Settings/ScriptableObjects`；顶层 allowlist（Art/Audio/Data/Editor/Scenes/Scripts/StreamingAssets/Tests/UI + csc.rsp/ASSET_README）；顶层空目录报错；simhei 唯一且在 `Art/Fonts/Resources`；TMP Settings 在 Art；音频 5 wav 在 `Audio/Resources/UI` | EditMode +7 |
| 字体预热 | `MainMenuUI.Build` 应用字体后预生成当前 choice 的 TMP SDF（缓存一次），切换不卡首次 | ✅ |
| Content P1b | 26 个纯基础设施文件从 `Simulation.Modding` 迁 `Content/MyriadCreation.Content.Modding`（.cs+.meta，GUID 保留，namespace 暂留）；**Content cs 137→163**，Content.Modding 46；Simulation.Modding 仅剩 4 个运行时文件（ContentRegistry/ContentRuntime/ContentResolvers/ContentQueries，P2 处理）；ContentQueries 因依赖运行时类型不迁；Editor asmdef 补 `MyriadCreation.Content` | 编译通过 |
| 资产引用验证 | 独立扫描 1193 个 meta GUID：**0 断引用**（除 PackageCache/Unity 内置）；Main.unity 的 DefaultWorldConfig/simhei/TMP 均解析；旧路径仅剩门禁测试故意文字 | 文档/README |
| TMP 断引用修复 | 修复 TMP Essential Resources 6 个旧 GUID / 19 处：SDF Shader→55be17、LiberationSans.ttf→88c191、默认 SDF→8d44ba、Fallback→9f7f4b、StyleSheet→34cc3a；`m_defaultSpriteAsset` 置空（项目无 Sprite Asset）；Main.unity 的 SimHei 材质 shader 同步修复；复扫无 Assets 内断 GUID | ✅ |

**验证（终值）**：编译 `COMPILE_OK`；全量 EditMode **581/581**；PlayMode 无图形/有图形各 **9/9**。

**下一轮按小步执行**：
1. Content P2：`ContentRegistry/ContentRuntime/ContentResolvers/ContentQueries` 拆 Content 定义索引 + Simulation host facade（按 CONTENT_ASSEMBLY_MIGRATION P2）。
2. TMP 资产完整性门禁：新增测试/脚本断言 TMP Settings 默认字体/StyleSheet/Shader 引用均存在（防再次断链）。
3. StreamingAssets/Base|Mods 硬编码路径收敛常量（Content P1b 后残留 4 文件 + 各 Catalog）。
4. 字体切换真实耗时测量（PlayMode 或 Editor 采样），必要时预热改为异步分帧。

### Round 42：Content P2 拆分 + 路径常量 + TMP 完整性门禁

| 步骤 | 内容 | 结果 |
|---|---|---|
| Content P2 | 新增 `Content/MyriadCreation.Content.Modding/ContentDefinitionIndex.cs`（502 行：13 个纯定义字典 + 原 ContentRuntime 全部纯 JSON loader + InnovationFallbackGroups 纯 struct 注入）；`ContentRuntime.cs` 650→148 行（只留 Culture/Race/Biome 运行时 loader，internal）；`ContentRegistry` 264 行 facade（公开 API/嵌套类型/18 TryGet/static ctor/Reset 逐项保持，内部委托 index）；`ContentResolvers/ContentQueries` 未改；Content cs 163→164，Content 内 Simulation 家族 using=0；新增 `ContentDefinitionIndexTests` 5 条；现有 16 个 ContentRegistry 测试零改动 | EditMode +5，全绿 |
| 路径常量 | Core 新增 `ContentPaths`（Base/Mods const + BaseDir/ModsDir/BasePath/ModsPath）；18 处 StreamingAssets 路径字面量替换（Localization/ContentSource/GoodsProperty/LanguageFamily/PropertyDefinition/Recipe/LawContentLoader/CultureStageCatalog + 测试 fixture）；复扫路径字面量=0 | ✅ |
| TMP 完整性门禁 | 新增 `TmpAssetIntegrityTests` 2 条：TMP 资产+Main.unity 所有 guid 在 Assets/PackageCache/白名单解析；TMP Settings 默认字体/样式/SDF shader 定向断言；静态预检违规 0 | EditMode +2 |
| 文档 | README/迁移文档同步；Round 报表 | ✅ |

**验证（终值）**：编译 `COMPILE_OK`；全量 EditMode **588/588**；PlayMode 无图形/有图形各 **9/9**。

**下一轮按小步执行**：
1. Content P3/P4：`ContentResolvers/ContentQueries` 收口 + namespace 统一 `MyriadCreation.Content.Modding`（全仓 using 批量更新，分文件批次）。
2. 初始化顺序回归：ContentDefinitionIndex 先 runtime 后 index 的等价性由现有 16 测试覆盖；补一条"顺序差异"专项测试（可选）。
3. TMP/资产门禁：把 TMP 显式 shader guid 断言改为从 `TMP_SDF.shader.meta` 动态读取（防重导入误报）。
4. 字体切换耗时采样与预热策略确认。
5. UI 4f：把地标/家族树等仍为最小实现的面板补契约（如需）。

### Round 43：项目完成度评估 + 动态地质/气候系统设计

| 产出 | 内容 |
|---|---|
| [PROJECT_COMPLETION_ASSESSMENT.md](../audit/PROJECT_COMPLETION_ASSESSMENT.md) | 全项目完成度约 62–65%；16 程序集/956 cs/594+9 测试/30 JSON；分模块百分比与 P0-P2 缺口 |
| [TERRAIN_DYNAMICS_INTEGRATION.md](../audit/TERRAIN_DYNAMICS_INTEGRATION.md) | 生成管线/调度/存档/UI 接入点；12 条不合理设计（静态气候、Random 不确定、Pipeline 重复/空阶段等） |
| [TERRAIN_ECONOMY_COUPLING_AUDIT.md](../audit/TERRAIN_ECONOMY_COUPLING_AUDIT.md) | 地形→经济耦合断点：食物产出链缺失、O(n²) 容量更新、肥力只降不升、标度混用等（P0 2 + P1 10 + P2 12） |
| [GEOLOGICAL_CLIMATE_DYNAMICS_PLAN.md](GEOLOGICAL_CLIMATE_DYNAMICS_PLAN.md) | 板块构造（生成期）/局内地质气候动态（带开关）、洋流、海侵海退、盐碱/荒漠化、植被覆盖率（非 100%）、人类开发反馈、灾害整合、数据模型/存档 17/确定性/性能/测试/分阶段 E0-E7 |
| [GOODS_CLASS_ENVIRONMENT_MODEL.md](GOODS_CLASS_ENVIRONMENT_MODEL.md) | **修正稿**：物产是具体 GoodsDef（物理属性/配方/动态合金），阶层需求是 5 阶层×8 维度；环境→具体物产产出→市场→阶层需求篮子；人类开发/灾害按物产结算；参考 CK3/Vic3/EU4/Imperator 分工 |
| [REFERENCE_PROJECTS_ANALYSIS.md](REFERENCE_PROJECTS_ANALYSIS.md) | 参考项目分析：Earth Simulator（气候/生态/灾害公式与参数、ECO_STRIPE 性能策略、灾害改世界场）、FantasyMapSimulator（BiomeResource/Buff 数据驱动 schema）、Things That Happen on the Map（板块→地形→气候→文明方向）、AzurLaneAutoScript（长跑 daemon/状态机→headless 模拟 harness） |

**结论**：架构层已收口（W1-W12 ✅）；环境大功能动工前需先修 E0 前置（食物产出链、O(n²) 容量、RNG 确定性、灾害入档），否则环境变化不可观测/会放大现有性能问题。

### Round 46：统一设计 V2 + 革新精确化方案

| 产出 | 内容 |
|---|---|
| [UNIFIED_DESIGN_V2.md](UNIFIED_DESIGN_V2.md) | 统一术语（族群/大起源/民族/国界线/印刷资本主义/普遍公民权/常任文官制…）；文化分离 vs 认同分离（语言分离算法化、文化分离需实验）；命名/语言学；头衔与 IPolityRef；货币/灾害/性能；革新体系重设计（185→350–500、拆分/前置/校验） |
| 革新审计 | 实测 185 条；发现伞形词（503 官僚制度、502 中央集权、206 印刷术、1017 前置缺失）；给出 1101 国界线/1102 印刷资本主义/1103 普遍公民权/1121 常任文官制等新 ID 与旧 ID 兼容策略 |
| 文档状态 | 设计完成；未改代码；测试基线 598/598 + 9/9 |

### 下一轮候补（已入审计报告，未改）

- A：InnovationTree 入档（SaveVersion 14）、信仰运行态、角色 traits/relations/技能/疾病、CharacterBond、DynamicGoods 跨局泄漏、写盘原子化、旧档迁移、关系持久化端点过窄；
- B：删除/迁移旧 `CivilizationEvolution.Runtime` 程序集（296 个同名类型），把场景切到新 `MapRenderer`/`MapEditor`；
- C：军费只扣不出、区域库恒降级、市场/仓储守恒、realm.treasury 双账本、`SettlementControlSystem` 每日 NRE、补给不可见；
- D：补 `SaveSystemRoundTripTests` 字段级断言、恢复被删的 TerrainGenerator/Succession/Religion/Diplomacy 专项测试、修正被放宽的区划头衔断言。

