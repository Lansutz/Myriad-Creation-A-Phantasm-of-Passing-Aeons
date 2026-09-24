# Architecture 2.0 — GitHub Sync / Tooling Notes

## 已确认的问题

本分支 `architecture-2-0-refactor` 曾同时存在两个项目根：

- `CivilizationEvolution/`
- `MyriadCreation/`

两棵目录树大部分内容相同，但并非完全相同；`MyriadCreation` 包含本轮架构重构产生的额外文件，并且已有一批文件的 SHA 与旧根不同。

这会制造一种很容易误判的“同步失败”：

1. GitHub Code Search / `mcp__GitHub__search` 的代码搜索面向仓库默认分支，并不能可靠地作为当前工作分支的文件发现入口。
2. `mcp__GitHub__fetch_file` 可以按 `ref` 读取指定分支，但路径必须精确匹配该分支的真实目录。
3. 因此，搜索结果显示旧路径、而按新根路径读取返回 404，并不意味着文件不存在；可能只是搜索索引与当前分支不一致，或者查询时使用了错误的根目录。
4. 本分支当前真实项目根是 `MyriadCreation/`；架构重构后的新增/修改内容应以该根为准。

## 固定工作规则

后续仓库同步与架构修改遵循：

- 当前工作分支：`architecture-2-0-refactor`
- 当前项目根：`MyriadCreation/`
- 发现文件：优先读取当前分支 Git tree；Code Search 只用于辅助定位。
- 读取具体文件：使用当前分支 + 精确路径。
- 写入文件：只写入 `MyriadCreation/`，禁止继续向旧 `CivilizationEvolution/` 根写新内容。
- 如果搜索结果与当前分支 tree 冲突，以当前分支 tree + 指定 ref 的文件内容为准。
- 进行大规模重构前，先检查是否存在重复项目根，避免把同一逻辑同步成两份。

## 本次修复

本次同步工作会把旧的 `CivilizationEvolution/` 项目根从该重构分支移除，只保留 `MyriadCreation/`，从仓库结构上消除双根歧义。

这不是 Unity namespace 重命名：代码中的 `CivilizationEvolution.*` namespace 暂时保持不变，避免把“项目目录重命名”和“代码命名空间重命名”混为一项迁移。
