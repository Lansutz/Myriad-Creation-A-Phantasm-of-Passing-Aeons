# 总完成计划（MASTER COMPLETION PLAN）

> 目的：一次性界定"全部要做的事、怎么算完成、边界在哪"。之后不再逐步追问"下一步"，
> 而是按本表逐项收口；所有 W 项验收通过 = 工程达到本阶段"已完成"。

## 0. "完成"的定义（Definition of Done）

一项工作只有在以下**全部**满足时才算"已解决"：

1. **可编译**：`mc-compile.ps1` 输出 `COMPILE_OK`，0 error。
2. **全量测试绿**：EditMode 全绿、PlayMode（无图形 + 有图形）全绿，0 failed / 0 skipped（skip 必须为显式平台/环境豁免并有注释）。
3. **门禁绿**：`AsmdefArchitectureGateTests`、`AssetLayoutGateTests`、`TmpAssetIntegrityTests` 全绿。
4. **边界不变**：不新增未登记的 `UI→Simulation`、`Rendering→Simulation`、`Content→Simulation/World` 引用；不改 SaveVersion/序列化字段（除非单独批准）。
5. **文档同步**：`CURRENT_ARCHITECTURE.md` / 本计划 / 对应迁移/资产 README 状态更新；代码内无"临时 TODO/占位实现"遗留（明确标注的边界除外）。
6. **无悬空引用**：Assets 内无断 GUID、无重复字体、无被禁顶层目录/空顶层目录。

"已完成"（本阶段） = **W1–W12 全部满足上述 DoD**；只剩 §4 明确记录的边界项，且边界项有文档理由。

## 1. 工作包与验收（WBS）

| ID | 工作包 | 交付物 | 验收（自动/人工） | 状态 |
|---|---|---|---|---|
| W1 | 程序集与分层 | Core/Content/Laws/Faith/Culture/World/Simulation/Rendering/Infrastructure/App/App.Contracts/UI 依赖 DAG | `AsmdefArchitectureGateTests` 全绿；无白名单（除 Editor/Tests 显式豁免） | ✅ Round 34-35 |
| W2 | UI 与 Simulation 解耦 | UI asmdef 不含 Simulation 家族；面板走契约 | 门禁 `UI_DoesNotReferenceSimulationFamily`；`Assets/UI` Simulation using=0 | ✅ Round 34 |
| W3 | 世界组合根/服务层 | WorldFactory/GenerationService/CompositionRoot 1:1 hooks；App/Save 走服务 | `WorldServicesTests` 全绿；App/Save 内旧直调=0 | ✅ Round 35-37 |
| W4 | Content 程序集收敛 | Content 只引 Core；纯内容代码在 Content；host 仅剩运行时映射 | Content 内 Simulation/World using=0；`ContentDefinitionIndexTests`；ContentRegistry 16 测试零改；旧 namespace=0 | ✅ Round 42（P2 拆分 + namespace 统一 + host 边界定案） |
| W5 | UI 面板 100% 契约化 | 所有面板无"契约未提供"占位；无空实现 | `UiDecouplingTests`/面板 PlayMode 全绿；`grep "契约未提供"` UI=0 | ✅ Round 42（CharacterDetailDto 性格/特质/家族/兄弟姐妹） |
| W6 | 字体/美术资产 | 字体归 Art；TMP 归 Art；TMP 断链门禁；字体设置可调 | `AssetLayoutGateTests` + `TmpAssetIntegrityTests` + `TMPFontUtilityTests` 全绿；SDF 缓存/耗时诊断 | ✅ Round 41-42 |
| W7 | 音频资产 | 音效/音乐归 Audio，路径常量/加载可验证 | `UiSfxTests` + 资产门禁；`Resources.Load` 路径核对 | ✅ Round 39-41 |
| W8 | Data/配置资产 | DefaultWorldConfig 归 Data；配置 GUID 引用正常 | 场景 GUID 校验；资产门禁 | ✅ Round 39 |
| W9 | 存档/读档 | SaveVersion 不变；原子写；读档重绑 | `SaveSystemRoundTripTests`/`WorldRebindOnLoadTests` 全绿 | ✅ 既有基线 |
| W10 | 法律/法令内容管线 | JSON 唯一来源 + 最小回退健壮 | `LawContentJsonTests`/`LawFallbackRobustnessTests` 全绿 | ✅ Round 35 |
| W11 | 测试/门禁体系 | 架构/资产/TMP/UI/契约门禁 | 全量 EditMode+PlayMode 全绿；CompletionGateTests 5 条 | ✅ Round 42 |
| W12 | 文档与执行记录 | 架构/资产/迁移/计划文档同步 | 文档状态与门禁一致（无过期"待办"） | 🔄 每轮同步 |

## 1.1 执行结果（Round 42 收口）

| W | 结果 | 验证 |
|---|---|---|
| W1 | ✅ 程序集 DAG 门禁 | AsmdefArchitectureGateTests 全绿 |
| W2 | ✅ UI 去 Simulation 家族 | AssetLayoutGate/CompletionGate UI using=0 |
| W3 | ✅ 世界服务层 1:1 hooks | WorldServicesTests 全绿 |
| W4 | ✅ Content 收敛 + namespace 统一 | 旧 namespace=0；Content 164 cs；host 4 文件边界定案 |
| W5 | ✅ UI 面板契约化 | "契约未提供"=0；CharacterDetail 性格/特质/家族/兄弟姐妹 |
| W6 | ✅ 字体/TMP/美术资产 | 资产门禁 + TMP 完整性门禁 + 字体设置/缓存指标 |
| W7-W10 | ✅ 既有基线 | 音频/Data/存档/法律测试全绿 |
| W11 | ✅ 门禁体系 | CompletionGateTests 5 条；全量 EditMode 594/594、PlayMode 9/9 |
| W12 | ✅ 文档同步 | 本计划/CURRENT_ARCHITECTURE/ASSET/CONTENT 文档状态 |

**最终验收（Round 42）**：`COMPILE_OK`；EditMode **594/594**；PlayMode 无图形/有图形 **9/9**；架构/资产/TMP/完成门禁全绿；旧 Content namespace=0；UI 占位=0；断 GUID=0。

## 1.2 新增工作包（用户确认的方向，待排期）

| ID | 工作包 | 交付/验收 |
|---|---|---|
| W13 | **认知与头衔**：双层命名（通用名+族群名）、头衔运行时实例与可编辑绑定、`IPolityRef` 政治实体接口字段、AI 接合、官员能力因子 | 见 [CIV_COGNITION_TITLE_CURRENCY.md](../design/CIV_COGNITION_TITLE_CURRENCY.md)；未接触无名/接触命名/头衔改绑/多重持有测试 |
| W13b | **语言/命名/族群**：音系+音节+词库+语义命名、语言差异意识（自称/他称）、族群/大起源认同载体、文化与认同分离双机制 | 见 [LANGUAGE_NAMING_IDENTITY.md](../design/LANGUAGE_NAMING_IDENTITY.md)；同 seed 可复现/未接触不命名/混居与分化反例 |
| W13c | **民族主义门槛**：补"边界概念/公民权"革新；`1017 民族主义` 前置三元全部必要；`1018 民族国家` 在民族主义之后；门槛正反向测试 | 见 [LANGUAGE_NAMING_IDENTITY.md](../design/LANGUAGE_NAMING_IDENTITY.md) §4.6b；内容 JSON + 测试 |
| W16 | **Data Forge 数据接入**：游戏侧产物 header/lock/license 门禁（已加 `DatasetIntegrityTests`）；音系库存/核心概念/命名合法性/环境物产校准按 Adapter Contract v1 接入 | 见 [DATA_FORGE_INTEGRATION.md](../design/DATA_FORGE_INTEGRATION.md)；`_generated` 只允许 redistributable；旧档缺字段回退 |
| W17 | **革新精确化**：185→首批 150–250 条有模拟效果的条目（能力优先，不凑数）；按 OCM/Oslo 重构为 14 Domain × Subdomain × Kind；物产/器物/活动/能力先判定；术语精确化；前置审查与 schema/自动化门禁 | 见 [INNOVATION_CATALOG_BLUEPRINT.md](../design/INNOVATION_CATALOG_BLUEPRINT.md)（§3.5 研究依据/分组映射）、[INNOVATION_DEDUPE_AUDIT.md](../design/INNOVATION_DEDUPE_AUDIT.md)（现有 185 条去重）；`InnovationSchemaTests/PrecisionTests/PrerequisiteTests/NationalismGateTests` |
| W18 | **法律/法令系统**：32 法/2 法令 → 法律域/类型/权利/法院/程序；公民权/民主历史类型拆分；法律革新门槛接线；法令类型/范围/合宪性 | 见 [LAW_DECREE_AUDIT_AND_REDESIGN.md](../design/LAW_DECREE_AUDIT_AND_REDESIGN.md)；`LawSchemaTests/LawEffectTests/CitizenshipGateTests` |
| W14 | **制度/货币/灾害**：`UnlockRule` 革新→阶层/身份/职业/文化/宗教显式化；多货币/区域接受/兑换；风暴生命周期/合并/登陆衰减 | 见同上 §4–§6；正反向门槛、货币兑换、风暴行为测试 |

## 2. 剩余收尾清单（按执行顺序，一次性做完）

1. **W4-1 namespace 统一**：`Content/MyriadCreation.Content.Modding/**` 从 `MyriadCreation.Simulation.Modding` 改为 `MyriadCreation.Content.Modding`，全仓 using 分批替换；`Simulation.Modding` 4 个 host 文件改用新命名空间。验收：全仓旧 namespace 只出现在 4 个 host 文件？否——host 也应无旧 namespace 声明，仅保留 `using MyriadCreation.Content.Modding;`；`grep "namespace MyriadCreation.Simulation.Modding"` = 0。
2. **W4-2 ContentResolvers/ContentQueries 收口**：确认二者作为 host facade 的可接受边界（依赖 CultureContentPack/RaceData/BiomeDef），文档标注；若可拆则拆，不可拆记录边界。
3. **W6-1 TMP 门禁去硬编码**：`TmpAssetIntegrityTests` 的 shader guid 改为从 `TMP_SDF.shader.meta` 动态读取（防 TMP 重导入误报）。
4. **W6-2 字体切换耗时**：`TMPFontUtility` 增加可选耗时统计（Stopwatch，日志/测试可读），EditMode 断言第二次调用不重建；不做脆弱的时间阈值断言。
5. **W5-1 UI 面板占位清零**：`CharacterDetailDto` 补 personality/traits（及可用家族字段），`CharacterDetailQueryAdapter` 从 CharacterData 填充，UIManager.Panels 去掉"契约未提供"；测试覆盖。
6. **W11-1 汇总门禁**：新增 `CompletionGateTests`（只读断言）：
   - `Assets/UI` 无 `using MyriadCreation.Simulation/Laws/Faith/Culture`；
   - `Content` 无 `using MyriadCreation.Simulation/World`；
   - `Content/MyriadCreation.Content.Modding` 无旧 namespace 声明；
   - 无 `契约未提供` 占位；
   - 资产布局关键路径存在（复用 AssetLayoutGate 逻辑）。
7. **W12-1 文档收口**：CURRENT_ARCHITECTURE/ASSET_LAYOUT_PLAN/CONTENT_ASSEMBLY_MIGRATION/本计划状态全绿；`WORKFLOW.md` 基线数字更新。
8. **最终验收**：编译 + EditMode + PlayMode×2 + 全门禁，输出终值。

## 3. 执行原则

- 每个 W 项一次做完整（不是"先做一半再问"）；仅当出现"破坏 API/存档/运行时行为"的真实风险时暂停上报。
- 每轮结束必须更新本表状态 + 跑全量验证；未达 DoD 的项保持 🔄，不写"完成"。
- 所有改动不提交（工作区保持可审查状态）；不删除用户资产（SourceHan 保留）。

## 4. 明确边界（本阶段不做，且有理由）

1. `Assets/StreamingAssets/{Base,Mods}` 目录名被 8+ 内容加载路径依赖，不改名（改则破坏既有存档/内容包）。
2. `Assets/Art/TextMesh Pro` 保留在 Art（视觉资源归美术，用户确认）。
3. `Content` 内 4 个 host 运行时文件（ContentRegistry/ContentRuntime/ContentResolvers/ContentQueries）保留在 Simulation.Modding：它们映射 `CultureData/RaceData/BiomeDef/BiomeRegistry`/`DiscoveryGroups`，迁入 Content 会形成硬环；Content 侧已抽 `ContentDefinitionIndex` 承担纯内容逻辑。
4. 不改 SaveVersion/序列化字段；存档兼容由现有迁移测试保证。
5. 音频运行时加载仍用 `Assets/Audio/Resources/{UI,Music}`（Unity Resources 机制；用户未批准改 StreamingAssets/Addressables）。
6. TMP vendor 资源不另建 ThirdParty 目录（用户确认视觉资源在 Art）。
