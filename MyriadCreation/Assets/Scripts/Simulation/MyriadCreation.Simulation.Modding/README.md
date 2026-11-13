# MyriadCreation.Simulation.Modding（内容加载器，暂留 Simulation 程序集）

本目录是**内容加载器/注册表运行时**（`ContentRuntime`、`ContentRegistry`、各 `*Wrapper`、`ContentResolvers` 等），
虽然名字带 "Modding"，但它与 **运行时** 强耦合：

- `using MyriadCreation.Simulation.State`（3 文件）
- `using MyriadCreation.Simulation.Faith`（3 文件）
- `using MyriadCreation.Simulation.Culture`（2 文件）
- 以及 10 文件 `using MyriadCreation.Content.Definitions`（其中 95 个定义已迁入 `MyriadCreation.Content`，namespace 为 `MyriadCreation.Content.Definitions`；本程序集经 asmdef 引用 Content）

因此 **本目录继续编译在 `MyriadCreation.Simulation` 程序集**（`MyriadCreation.Simulation.Modding` namespace 不变），不迁入 `MyriadCreation.Content`。

后续（B2+ 内容管线批次）：先做 Content ↔ Simulation 的接口反转（`IContentLookup` / `IContentStore` 等已存在，需把 `ReligionCatalog/DoctrinePool` 等注册表与加载器解耦），再把加载器拆到 `MyriadCreation.Content.Loading`（独立程序集或 Content 内）。届时目录可改名为 `Simulation/MyriadCreation.Simulation.ContentLoading/`。
