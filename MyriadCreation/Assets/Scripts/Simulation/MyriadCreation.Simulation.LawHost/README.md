# MyriadCreation.Simulation.LawHost（Laws 宿主/组合根 glue，Phase 0）

> 本目录是 **B6 Laws 首步（ASMDEF_SPLIT Phase 0）** 的产物：把"宿主/组合根/视图片段"从
> `Simulation/MyriadCreation.Simulation.Laws/` 移出，让 Laws 文件夹只保留**纯规则 / 状态 / 定义**。
> 暂**不翻 asmdef**（仍编在 `MyriadCreation.Simulation` 程序集），但 namespace 独立为
> `MyriadCreation.Simulation.LawHost`，便于后续把 host 放到 WorldState 或独立 host 程序集。

## 为什么移出

`MyriadCreation.Simulation.Laws` 的目标是**纯下层系统**（只依赖 Core + Content，不依赖 Simulation 的
GameWorld/RealmData/Systems）。但以下 8 个文件是"宿主适配/组合根/视图"，天然依赖世界与运行时管理器：

| 原路径（`Simulation/MyriadCreation.Simulation.Laws/`） | 新路径（本目录） | 角色 / 宿主依赖 |
|---|---|---|
| `LawContracts.cs` | `LawContracts.cs` | `ILawQuery/ILawCommand` 适配器，依赖 `GameWorld`（`WorldState`） |
| `LawDecreeView.cs` | `LawDecreeView.cs` | 视图 DTO/Builder（`LawViewItem/DecreeViewItem/LawDecreeView/LawDecreeViewBuilder`），依赖 `RealmData`（`State`） |
| `LawEffectRuntime.cs` | `LawEffectRuntime.cs` | 法律效果运行时，依赖 `RealmData/RealmSociety`（`State`）与 Systems |
| `LawSystem.cs` | `LawSystem.cs` | 旧宿主系统门面（`GameWorld`），0 外部引用，疑似死文件；保留待清 |
| `JudicialSystem.cs` | `JudicialSystem.cs` | 司法系统，依赖 `RealmData`（`State`） |
| `DecreePropagationSystem.cs` | `DecreePropagationSystem.cs` | 敕令传播，依赖 `GameWorld`/信使/`CharacterData`（`State`/`Systems`/`World.Settlement`） |
| `LawCompositionMigration.cs` | `LawCompositionMigration.cs` | 存档迁移，依赖 `RealmData`（`State`） |
| `LawReform.cs` | `LawReform.cs` | 改革流程，依赖 `RegimeChangeDynamics/Chronicle/InnovationTree`（`State`） |

## 依赖方向约定

- **允许**：`LawHost → Laws（规则/状态/定义）` + `LawHost → State / WorldState / Systems / World / Content / Core.Contracts`。
- **禁止（目标）**：`Laws → LawHost`；`Laws` 文件夹不再 `using MyriadCreation.Simulation.WorldState / Systems`。
- 本目录文件均**不是** MonoBehaviour/ScriptableObject（纯类），namespace 变更不影响 Unity 序列化；meta GUID 随文件移动保留。

## 后续（Phase B/C 之后的归属）

1. **B 阶段（已落地）**：`Core.Contracts/ILawContext` + `WorldState/LawRealmContext`（RealmData 适配器）；
   `LawAndDecreeSystem` 的 12 个 `RealmData` 入口迁到本目录 `LawRealmHost`（公开 API 形状不变），
   Laws 核心只留纯规则与 `CanPropose(ILawContext, RealmLawState, LawComposition, …)`。
   `InheritanceGender`、`EligibilityRules`（去掉 `Filter(CharacterData)`，改由 SuccessionSystem 内联）下沉 `Content.Definitions`。
2. **C 阶段（已落地）**：`InheritanceLaw` + `InheritanceScope/Branch/Age/LandInheritanceMode/TitleInheritanceMode`
   （及 B 已移的 `InheritanceGender`）下沉 `Content.Definitions`（枚举值/字段名不变）；
   `DetermineHeir/GetHeirOrderedPool/IsSibling`（依赖 CharacterData）迁到 host `WorldState/InheritanceHeirSelection`；
   Laws 文件夹内 `CharacterData` 引用清零，Laws 只留配置+`DistributeLand/GetName`。
3. 之后若翻转 asmdef：host 可并入 `MyriadCreation.Simulation`（WorldState）或独立 `MyriadCreation.LawHost`，
   并保持 `Simulation → LawHost → Laws` 的单向依赖（Laws 的 asmdef 永不引用 `MyriadCreation.Simulation`）。
