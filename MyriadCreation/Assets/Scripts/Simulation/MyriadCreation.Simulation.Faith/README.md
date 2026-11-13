# MyriadCreation.Simulation.Faith（信仰/宗教大系统）

**namespace 恒为 `MyriadCreation.Simulation.Faith`，不随子目录变化。**

本目录采用"**大系统 namespace + 子文件夹表示角色**"的约定（与 `MyriadCreation/docs/architecture/SYSTEM_OWNERSHIP.md` 对齐；对应 CK3 的"定义是定义、脚本是脚本"）：

| 子目录 | 角色 | 放什么 | 规则 |
|---|---|---|---|
| `Definitions/` | 定义 | `ReligionDef`、`ReligionCatalog`（注册表）、`FaithType`、`FaithOrganizationType`、`ReligionMapLevel`、`ReligionNodeType`、`DoctrineCategory`、`DoctrineOptionDef`、`SaintDef` | 纯数据/枚举/POCO + 只读注册表；不持有世界运行期可变状态；内容数据优先来自 JSON（`StreamingAssets/Base/Religion/*.json`），代码里不放内容表 |
| `State/` | 状态 | `Doctrine`、`DoctrinePool`、`GreatHolyWarState` | 可序列化、生命周期跟世界走（或与内容绑定的注册表缓存） |
| `Runtime/` | 运行时 | `FaithSystem`、`CanonizationSystem`、`ReligionSimulationEventHandler`、`WarfareReligionSchedule`、`GreatHolyWarSystem` | 系统/调度/事件处理；不得在此硬编码宗教内容表（名字、描述、数值） |

约定要点：
1. 新增文件先判断"角色"再放子目录；namespace 一律 `MyriadCreation.Simulation.Faith`（**不要**改成 `...Faith.Definitions` 之类）。
2. 内容（宗教、教义、圣人等条目）优先放 `Assets/StreamingAssets/Base/Religion/`（或后续 `Base/Faith/`），由 `ContentRuntime` 加载并回退；`Definitions/` 只保留类型与注册表。
3. 跨层：Core 事件（`FaithConflictTriggeredEvent`）留在 Core；存档适配器（`Infrastructure.Save.FaithSaveData/FaithPersistence`）留在 Infrastructure，本目录不反向依赖它们。
4. 目录名与 namespace 不再一一对应是**有意为之**（角色细分层），如 CI 有"目录=namespace"校验，需把本系统列入白名单。
