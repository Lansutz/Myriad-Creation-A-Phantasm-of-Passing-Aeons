# MyriadCreation.Simulation.Culture（文化大系统）

**namespace 恒为 `MyriadCreation.Simulation.Culture`，不随子目录变化。**

本目录采用"**大系统 namespace + 子文件夹表示角色**"的约定（与 `MyriadCreation/docs/architecture/SYSTEM_OWNERSHIP.md` 对齐；对应 CK3 的"定义是定义、脚本是脚本"）：

| 子目录 | 角色 | 放什么 | 规则 |
|---|---|---|---|
| `State/` | 状态 | `CultureData`（每个文化一份运行期数据，内容来自 `StreamingAssets/Base/Culture/<文化>/CultureData.json`） | 可序列化、跟世界走；不在代码里硬编码文化条目 |
| `Runtime/` | 运行时 | `CultureLineage`、`CultureSimilarity`、`CultureTransition`、`CultureStageEvolutionSystem`、`CultureSchedule` | 纯算法/系统/调度；**算法常量**（如文化阶段人口阈值）后续应迁到 JSON（见内容管线文档），代码保留默认回退 |

约定要点：
1. 新增文件按角色放子目录；namespace 一律 `MyriadCreation.Simulation.Culture`（**不要**改成 `...Culture.State` 之类）。
2. 文化内容（文化条目、姓名表、Ethos、传统）优先放 `Assets/StreamingAssets/Base/`：`Culture/<文化>/CultureData.json` + `*.csv`、`Ethos/Ethos.json`、`Tradition/Traditions.json` 等，由 `ContentRuntime` 加载。
3. 跨层：文化相关 UI 文本在 `Assets/UI/MyriadCreation.UI.Presentation`（`RealmOverviewText` 等）；存档在 `Infrastructure.Save`；本目录不反向依赖。
4. 目录名与 namespace 不再一一对应是**有意为之**（角色细分层），如 CI 有"目录=namespace"校验，需把本系统列入白名单。
