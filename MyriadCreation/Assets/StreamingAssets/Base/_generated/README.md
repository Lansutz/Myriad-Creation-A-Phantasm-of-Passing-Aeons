# `_generated/` 内容说明与署名

本目录存放由外部数据源离线生成、随游戏分发的**小体积产物**。生成工具：`Myriad Data Forge`（独立仓库 `D:\Myriad-Data-Forge`）。

## language-families.json

- **来源**：Glottolog 5.3（Max Planck Institute for Evolutionary Anthropology）
- **许可**：CC BY 4.0（https://creativecommons.org/licenses/by/4.0/）
- **数据页**：https://glottolog.org/ ；CLDF 发布：Zenodo 记录 18840967
- **处理**：只裁剪游戏用到的语言节点及其祖先链（75 个节点），不复制全量 8,238 languoids；父链来自 Glottolog 的 Newick 子分类串。
- **生成器**：`forge.cli build-languages`（锚定版本与 sha256 记录在 Forge 仓库的 `datasets.lock.json`）
- **署名要求**：分发本文件或其衍生内容时保留本说明与 Glottolog 链接。

## 约束

- 本目录只允许出现 `requires: redistributable` 的产物；`design_only`（NC/ND/订阅/PHOIBLE 衍生）报告一律留在 Forge 仓库的 `docs/`，不得进入游戏包。
- 新增产物必须在 Forge 仓库的 `datasets.lock.json` 中有来源、版本与许可记录。
