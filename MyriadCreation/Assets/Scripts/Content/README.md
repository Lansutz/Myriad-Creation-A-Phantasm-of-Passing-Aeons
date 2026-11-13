# MyriadCreation.Content（内容定义层，CK3 `common/` 对齐）

**职责**：纯内容定义与数据契约（枚举/POCO/注册表/查表逻辑），对应 CK3 的 "定义是定义、脚本是脚本" 中的"定义"侧。

## 依赖规则（硬约束）

- 本程序集 **只能引用 `MyriadCreation.Core`**（UnityEngine 除外）。
- **禁止**引用 `MyriadCreation.Simulation` / `.World` / `.Rendering` / `.Infrastructure` / `.UI`——否则形成反向依赖/环。
- 本目录内任何 `.cs` 不允许出现 `using MyriadCreation.Simulation.*`（CI 可用 grep 把关）。
- Simulation 引用 Content；Rendering/Infrastructure/UI/Tests(EditMode) 按需引用 Content（见各 asmdef）。

## 当前结构与命名空间

- `MyriadCreation.Content.Definitions/`：来自原 `Simulation/MyriadCreation.Simulation.Definitions` 中**无 Simulation/World 运行时依赖**的 95 个定义文件（含枚举、DTO、查表/注册表类）。
- **namespace 已统一为 `MyriadCreation.Content.Definitions`**（task-71 完成，全仓 using/限定引用已同步更新）。
- 仍留在 `Simulation/MyriadCreation.Simulation.Definitions/` 的 12 个"带运行时依赖的定义"文件继续使用 `MyriadCreation.Simulation.Definitions`（见那边 `README.md` 的 12 条反转清单）。
- Modding 加载器（ContentRuntime/ContentRegistry）依赖运行时，仍留在 Simulation 程序集，见 `Simulation/MyriadCreation.Simulation.Modding/README.md`。
