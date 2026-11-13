# MyriadCreation.Simulation.CultureHost

Culture（`MyriadCreation.Simulation.Culture`）的 **host 粘合层**（B5 首步，见 `docs/architecture/ASMDEF_SPLIT_FAITH_CULTURE_LAWS.md` §7.2）。

## 职责

- 只放直接依赖 `GameWorld` / `RealmData` / `MapActor` / `InnovationTree` / `SocialStratificationCalculator` / `SettlementData` 的胶水代码；
- 纯规则（`EvaluateEvolution`、`CheckRegress`、革新门槛、阶段定义/JSON 加载）留在 `Culture`；
- 依赖方向固定为 **CultureHost → Culture/Simulation**；Culture 运行时不引用 CultureHost（后续独立 asmdef 时 Culture 只允许 refs Core+Content）。

## 当前文件

| 文件 | 内容 | 旧位置 |
|---|---|---|
| `CultureStageEvolutionHost.cs` | `FormRealmFromActor`、`ApplyPathBonuses`、`MonthlyTick`、`HasUrbanCenter`（按 doc 179-353 段移出） | `Culture/Runtime/CultureStageEvolutionSystem.cs` |
| `CultureContracts.cs` | `ICultureQuery/ICultureCommand` 的 host 适配器（内部转调 GameWorld/CultureData/RealmData） | `Culture/Runtime/CultureContracts.cs` |
| `CultureContextAdapter.cs` | `ICultureContext` 的 GameWorld 实现（B 阶段新增） | 新增 |
| `CultureLineage.cs` | 语言族/父语言 → 文化血缘（依赖 LanguageDef） | `Culture/Runtime/CultureLineage.cs` |
| `CultureSchedule.cs` | `culture-stage.monthly` 调度注册 | `Culture/Runtime/CultureSchedule.cs` |
| `ContentLookupAdapter.cs` | `IContentLookup` 的 ContentResolvers 实现（Culture 内容查询注入点） | 新增 |
| `CultureTransitionHost.cs` | `ListTraditionCandidates`（依赖 TraditionDef） | `Culture/Runtime/CultureTransition.cs` |

## 调度

`GameWorld.Simulation.cs` 的 `culture-stage.monthly` 注册通过 `CultureStageEvolutionHost.MonthlyTick(this)` 调用；`CultureSchedule`（注册类）已移到本目录（namespace `CultureHost`），由 GameWorld.Simulation 直接调用。

## 后续

- `Culture` 已是独立程序集（`MyriadCreation.Culture.asmdef`，refs=Core+Content），`CultureHost` 留在 `Simulation` 主程序集（host 引用 Culture）；
- `CultureData`/`CultureTransition` 的内容查询已改为注入 `IContentLookup`（接口在 Content，实现在本目录 `ContentLookupAdapter`）；
- `OfficeTitleEntry` 已下沉 `Content.Definitions`；`CultureLineage`/`CultureSchedule` 已上移本目录；`CultureTransitionHost` 承接依赖 `TraditionDef` 的传统候选查询。
