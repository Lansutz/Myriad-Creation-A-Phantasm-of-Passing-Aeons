# 生成内容署名与许可（游戏内副本）

本目录 `_generated/` 的产物由独立数据管线 **Myriad Data Forge**（`D:\Myriad-Data-Forge`）生成。
每个产物必须在 `datasets.lock.json` 中有来源、版本与许可记录，且 `requires` 只能是 `redistributable`。

## language-families.json

- 来源：**Glottolog 5.3**（Max Planck Institute for Evolutionary Anthropology）
- 许可：**CC BY 4.0**（https://creativecommons.org/licenses/by/4.0/）
- 数据页：https://glottolog.org/ ；CLDF 发布：Zenodo 记录 18840967
- 处理：仅裁剪游戏用到的语言节点及其祖先链（75 个节点），不复制全量 8,238 languoids。
- 生成器：`forge.cli build-languages`（版本与 sha256 记录在 Forge 仓库 `datasets.lock.json`）。
- 署名要求：分发本文件或其衍生内容时，保留本说明与 Glottolog 链接。

## 约束

- 只允许 `requires: redistributable` 的产物进入本目录。
- `design_only`（PHOIBLE 衍生 / D-PLACE / Seshat / 订阅类）报告留在 Forge 仓库 `docs/`，不得进入游戏包。
- 新增产物必须同步更新本文件与 `datasets.lock.json`；`DatasetIntegrityTests` 会强制检查。
