# 文件与架构规则（ARCHITECTURE RULES）

> 生效日期：2026-11-03。适用于**之后所有**代码/内容/文档改动。
> 事实来源：`MyriadCreation/Assets/**/*.asmdef`、`AsmdefArchitectureGateTests`、`CURRENT_ARCHITECTURE.md`、`SYSTEM_OWNERSHIP.md`。
> 任何新功能先按本文件定位 owner，再写代码；不得先写代码再补架构。

---

## 1. 程序集与依赖方向（硬边界）

```text
Core
 ← Content
      ← Laws / Faith / Culture（只引 Core + Content）
      ← World（只引 Core）
           ← Simulation（Core + World + Content + Laws + Faith + Culture）
                ← Rendering（Core + Content）
                ← Infrastructure（Core + World + Simulation + Content + Faith + Culture）
                     ← App.Contracts（Core + World + Simulation）
                     ← App（Core + Infrastructure + World + Simulation + Rendering + App.Contracts + UGUI）
                          ← UI（Core/Rendering/Infrastructure/World/Content/App/App.Contracts/UGUI/TMP；**不含 Simulation 家族**）
```

| 层 | 程序集 | 允许引用 | 禁止 |
|---|---|---|---|
| L0 | `MyriadCreation.Core` | 无 | 任何上层 |
| L1 | `MyriadCreation.Content` | Core | World/Simulation/UI/Rendering/Infrastructure/App |
| L2 | `Laws` / `Faith` / `Culture` | Core + Content | 上层/World/Simulation |
| L3 | `MyriadCreation.World` | Core | Simulation/Content/UI |
| L4 | `MyriadCreation.Simulation` | Core/World/Content/Laws/Faith/Culture | UI/Rendering/Infrastructure/App |
| L5 | `Rendering` | **Core + Content** | World/Simulation/App.Contracts/Infrastructure |
| L5 | `Infrastructure` | Core/World/Simulation/Content/Faith/Culture | UI/Rendering |
| L6 | `App.Contracts` / `App` | 按 asmdef | App 禁引 UI；App.Contracts 不得反向引 App |
| L7 | `UI` | Core/Rendering/Infrastructure/World/Content/App/App.Contracts + UGUI/TMP | **Simulation/Laws/Faith/Culture** |

门禁：`AsmdefArchitectureGateTests`（无环、Content 边界、子系统边界、App/UI 方向、分层序、Rendering 边界、UI 硬边界）。测试/Editor 豁免仅限测试/工具，不得被业务程序集引用。

---

## 2. 新功能放置决策树

新增任何功能，按顺序问：

1. **是纯契约/DTO/枚举/事件/跨系统只读数据吗？**
   → `Core`（`Scripts/Core/MyriadCreation.Core.*`）。
2. **是内容定义/目录/JSON 解析/静态内容校验吗？**
   → `Content`（`Scripts/Content/MyriadCreation.Content.Definitions` / `.Modding` 的纯内容部分）。
   → 运行时加载/宿主映射仍按现有边界放 `Simulation` 的 host 文件，不擅自搬迁。
3. **是法律/信仰/文化域模型吗？**
   → `Laws` / `Faith` / `Culture`（只引 Core+Content）；host glue 在 `Simulation`。
4. **是地图/地形/气候/水文/聚落数据模型或世界生成算法吗？**
   → `World`（只引 Core）。
5. **是模拟系统/运行时状态/调度/组合根吗？**
   → `Simulation`；优先接现有创建/调度/存档入口，不新建平行系统。
6. **是存档/读档/音频兼容层吗？**
   → `Infrastructure`（Save/Audio）；不引 UI/Rendering。
7. **是视图/渲染/编辑命令吗？**
   → `Rendering`（只引 Core+Content；经 Core.Contracts 读世界）。
8. **是启动/场景流/设置/组合根吗？**
   → `App`；不引 UI。
9. **是 UI 面板/控件/展示模型吗？**
   → `Assets/UI`；只消费 Core/App/App.Contracts 与非 Simulation 运行时；禁止 `using MyriadCreation.Simulation/Laws/Faith/Culture`。
10. **是测试/工具吗？**
    → `Assets/Tests/EditMode`、`Assets/Tests/PlayMode`、`Assets/Editor`；测试可引全量运行时。

**禁止**：同一职责新建第二个程序集/目录；把运行时对象直接序列化；在 UI 里写模拟逻辑；在 Content 里引用运行时状态；在 World 里引用 Simulation；用 `Resources.Load` 新路径绕过资产门禁。

---

## 3. 数据类型与文件位置

| 类型 | 位置 | 规则 |
|---|---|---|
| 内容定义（Def） | `Content/Definitions` | 纯数据 + 校验；不含运行时状态/Unity 场景引用 |
| 内容 JSON | `Assets/StreamingAssets/Base/<系统>/` | 唯一内容来源；`JsonUtility` 可解析；缺省字段必须安全 |
| Mod 覆盖 | `Assets/StreamingAssets/Mods/` | 与 Base 同 schema；按现有加载顺序覆盖 |
| 生成产物 | `Assets/StreamingAssets/Base/_generated/` | 必须有 `schema_version/generated_at/artifact/requires/source`；只允许 `redistributable`；需 `datasets.lock.json`/`LICENSES.md`；`DatasetIntegrityTests` 强制 |
| 世界数据模型 | `World/**` | 只依赖 Core；不序列化 Unity 对象 |
| 运行时状态 | `Simulation/**` | 不直接进 Save DTO；由 Infrastructure 映射 |
| Save DTO | `Infrastructure/Save/**` | 显式字段；`JsonUtility` 不支持 `[FormerlySerializedAs]`，旧键必须走 `SaveJsonMigration` |
| 查询/命令契约 | `Core/Contracts` 或 `App.Contracts` | UI 只依赖契约；实现由 App/Simulation 注入 |
| UI 展示模型 | `Assets/UI/**` | 不引用 Simulation 家族；数据来自契约 |

---

## 4. 内容与数据规则

1. **定义是定义，脚本是脚本**：JSON 是内容唯一完整来源；代码只做加载/校验/回退。
2. **不硬编码内容**：新增物产/革新/法律/建筑/单位优先进 JSON；运行时只保留最小回退集。
3. **不复制内容**：同一概念只允许一个定义源；区域变体用 `variantOf`/`region/cultureTags`，不复制整张表。
4. **ID 稳定**：已有 ID 不改语义；拆分/改名必须保留 `legacyIds` 或迁移映射；新增按域分块，避开已占用 ID（含代码保留的 1025）。
5. **物产 vs 器物 vs 建筑 vs 能力**：
   - 可储存/交易的材料 → `GoodsDef`；
   - 单个器物/设施 → `EquipmentDef`/`BuildingDef`/`SettlementBlock`；
   - 宽泛活动 → Capability/Subdomain；
   - 只有改变模拟变量的具体技术/制度/做法/思想 → `Innovation`。
6. **生成物许可**：`design_only`（NC/ND/PHOIBLE 衍生等）不得进入 `StreamingAssets`；游戏内只允许 redistributable。
7. **内容版本**：新增 schema 字段必须可缺省；`schema_version` 不兼容时拒绝并回退，不崩溃。

---

## 5. 存档与兼容

1. `SaveVersion` 变更必须单独批准，并写迁移/回滚测试；未批准前不改序列化字段语义。
2. 新字段必须有默认值；旧档缺字段走显式迁移。
3. 领域对象不直接序列化；用 DTO + Adapter。
4. 读档顺序：Content → 基础世界 → Realm → 域状态 → 角色/研究 → 派生/查询重建；不能从定义反推已完成状态。
5. 每次存档改动跑 `SaveSystemRoundTripTests` / `WorldRebindOnLoadTests`（或新增同级测试）。

---

## 6. 测试与门禁（每批必须）

1. **编译**：`E:\TEMP\mc-compile.ps1 -LogName <name>` → `COMPILE_OK`。
2. **EditMode**：`E:\TEMP\mc-test-editmode.ps1` → 0 failed / 0 skipped（除显式平台豁免）。
3. **PlayMode**：`E:\TEMP\mc-test-playmode.ps1` + `E:\TEMP\mc-test-playmode-gfx.ps1` → 均全绿。
4. **门禁**：`AsmdefArchitectureGateTests`、`AssetLayoutGateTests`、`TmpAssetIntegrityTests`、`CompletionGateTests`、`DatasetIntegrityTests`。
5. **新增测试**放在对应 EditMode/PlayMode 程序集；行为测试优先于纯反射测试。
6. 不允许用"测试里写死期望"掩盖系统不一致；测试必须从真实入口驱动。

---

## 7. 每次改动的固定流程

1. 写"现有事实"表：文件:行、现有字段/调用点/存档路径。
2. 定 owner/接口/迁移映射（含旧档/旧 ID）。
3. 小步补丁；接现有创建/调度/存档/UI 入口。
4. 编译 → 新增测试 → 全量 EditMode → PlayMode×2。
5. 只读 auditor 复审 diff 与现有系统一致性。
6. 更新本文件/`CURRENT_ARCHITECTURE`/统一计划/相关设计文档。
7. 报告：改了什么、未改什么、测试数字、边界/风险。

---

## 8. 当前明确边界（不得擅自改）

1. `Assets/StreamingAssets/{Base,Mods}` 目录名不改。
2. `Assets/Art/TextMesh Pro` 保留在 Art。
3. Content 的 4 个 host 运行时文件（ContentRegistry/ContentRuntime/ContentResolvers/ContentQueries）保留在 Simulation，不强行迁 Content。
4. 音频仍用 `Assets/Audio/Resources/{UI,Music}`；不擅自改 Addressables/StreamingAssets。
5. 不新建 `ThirdParty` 目录；TMP vendor 资源留在 Art。
6. 未批准不改 `SaveVersion`、不改场景 GUID/字体引用。
7. 法律/组织/国际组织等新系统：未立项不引入；法律只做模拟有意义的骨架。
