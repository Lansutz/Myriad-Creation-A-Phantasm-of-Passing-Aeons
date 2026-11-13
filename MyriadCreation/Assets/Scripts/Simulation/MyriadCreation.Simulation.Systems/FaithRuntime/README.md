# MyriadCreation.Simulation.FaithHost

Faith（`MyriadCreation.Simulation.Faith`）的 **host 粘合层**（B4 首步，见 `docs/architecture/ASMDEF_SPLIT_FAITH_CULTURE_LAWS.md` §7.2）。

## 职责

- 只放依赖 `GameWorld/RealmData/CharacterManager/Systems/Scheduling` 的胶水与契约适配；
- 纯 Faith 运行时（`FaithSystem`、`Doctrine`）留在 Faith 程序集；C 阶段通过 `IFaithContext` 反转 FaithSystem 的 CharacterData/Realm 依赖；
- 依赖方向固定为 **FaithHost → Faith/Simulation**；Faith 运行时不引用 FaithHost（asmdef 翻转后 Faith 只 refs Core+Content）。

## 当前文件

| 文件 | 内容 | 旧位置 |
|---|---|---|
| `FaithContracts.cs` | `IFaithQuery/IFaithCommand` 的 GameWorld 适配器 | `Faith/Runtime/FaithContracts.cs` |
| `CanonizationSystem.cs` | 封圣判定（CharacterData/State 胶水） | `Faith/Runtime/CanonizationSystem.cs` |
| `GreatHolyWarSystem.cs` | 大圣战战争结算（RealmData/CharacterManager/Laws） | `Faith/Runtime/GreatHolyWarSystem.cs` |
| `GreatHolyWarState.cs` | 大圣战状态（host war state） | `Faith/State/GreatHolyWarState.cs` |
| `WarfareReligionSchedule.cs` | 战争/宗教日调度注册（Scheduling/CombatManager） | `Faith/Runtime/WarfareReligionSchedule.cs` |
| `ReligionSimulationEventHandler.cs` | 事件总线桥（Core.Events） → Faith 运行时 | `Faith/Runtime/ReligionSimulationEventHandler.cs` |

## 后续

- `FaithSystem` 的 `GetVirtueScore/GetSinScore(CharacterData)` 与 `CanonizationSystem` 的 CharacterData 入参在 C 阶段改 `IFaithContext`/characterId；
- `FaithSaveData` 字段名/版本不变（Infrastructure 只引用 Faith 运行时）；
- asmdef 翻转（D）：`Faith` 独立程序集 refs Core+Content；FaithHost 留在 Simulation 主程序集。
