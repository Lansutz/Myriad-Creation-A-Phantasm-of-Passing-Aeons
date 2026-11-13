# MyriadCreation.Simulation.Definitions（仅存"带运行时依赖的定义"）

> 本目录原 107 个定义文件中，**95 个已迁往 `Assets/Scripts/Content/MyriadCreation.Content.Definitions/`**（无 Simulation/World 依赖的定义，编入 `MyriadCreation.Content` 程序集，namespace 已改为 `MyriadCreation.Content.Definitions`）。
> 本目录遗留 **12 个** 文件：它们引用 Simulation 运行时（State/Modding/Laws）或 World 类型，**必须留在 `MyriadCreation.Simulation` 程序集**；namespace 保持 `MyriadCreation.Simulation.Definitions`。

## 12 条反转清单（后续做接口反转后再迁 Content）

| # | 文件 | 依赖（留在 Simulation 的原因） |
|---|---|---|
| 1 | `CognitionImpairmentDef.cs` | `MyriadCreation.Simulation.State` |
| 2 | `HearingImpairmentDef.cs` | `MyriadCreation.Simulation.State` |
| 3 | `MobilityImpairmentDef.cs` | `MyriadCreation.Simulation.State` |
| 4 | `SpeechImpairmentDef.cs` | `MyriadCreation.Simulation.State` |
| 5 | `VisionImpairmentDef.cs` | `MyriadCreation.Simulation.State` |
| 6 | `ImpairmentRegistry.cs` | `MyriadCreation.Simulation.State`（注册表/查询逻辑） |
| 7 | `PersonalityArchetype.cs` | `MyriadCreation.Simulation.State` |
| 8 | `PersonalityTraitDatabase.cs` | `MyriadCreation.Simulation.State`（注册表/查询逻辑） |
| 9 | `TitleCatalog.cs` | `MyriadCreation.Simulation.Modding`（注册表/查询逻辑） |
| 10 | `TitleEligibility.cs` | `MyriadCreation.Simulation.Laws` |
| 11 | `BuildingRequirement.cs` | `MyriadCreation.World.Settlement` |
| 12 | `TitleDefinition.cs` | 闭包：被上面留 Simulation 的类型引用（自身无显式依赖） |

> 反转方向建议：State 侧只暴露查询接口（`ICharacterQuery/IPersonalityQuery` 等）、Modding 侧暴露 `IContentLookup`、Laws 侧暴露 `ILawQuery`、World 侧把 `BuildingRequirement` 的 World 依赖以数据形式下沉到 Content 后，再把对应文件迁入 `Content/MyriadCreation.Content.Definitions/`（namespace 届时一并改为 `MyriadCreation.Content.Definitions`）。
