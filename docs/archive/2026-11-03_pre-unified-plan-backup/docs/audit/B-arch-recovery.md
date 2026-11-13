# 审计 B：反编译恢复残留、重复类型与程序集依赖

- 分支：`architecture-2-0-refactor`，HEAD `f1ca764f`；工作区未提交改动：**935 删除 / 2704 新增（含 555 个反编译转储）/ 140 修改**（`git status --porcelain -uall` 统计）
- 只读审计：未修改 `MyriadCreation/Assets/**`，未运行 Unity/编译/测试，未执行改变工作区的 git 命令
- 机器可核对的数据来源：`Assets/Scripts` + `Assets/UI` + `Assets/Tests` 共 911 个 `.cs`，用 awk 提取 **1289 个类型定义（950 个唯一类型名）**，按最近 asmdef 归属程序集；`.meta` 1353 个
- 本报告口径：同名类型 = 简单类型名相同；顶层 = 缩进 ≤4（排除类内嵌套类型与 partial 分部）；FQN 冲突 = 命名空间+类型名+不同程序集

## 1. 结论摘要

1. **工作区不是"纯残留"，而是一次把 HEAD 新架构覆盖回旧 CivilizationEvolution 世界的恢复快照**。最硬的证据是主场景：工作区 `Assets/Scenes/Main.unity` 与未跟踪的 `Assets/_Recovery/0.unity` 仅差 178 行，而 HEAD 的 Main.unity 有 80 个 TextMeshProUGUI 组件、工作区为 0 个 TMP + 17 个旧 `UnityEngine.UI.Text`；场景 6 个项目脚本的 `m_EditorClassIdentifier` 从 `MyriadCreation.*` 整体变成 `CivilizationEvolution.*`。
2. **主场景有 3 个悬空脚本绑定**：Bootstrap（guid `fc17d0f2…`，HEAD 在 `Assets/UI/Bootstrap.cs.meta`）、GameManager（`e744e365…`，HEAD 在 `Assets/UI/GameManager.cs.meta`）、GameWorld（`e6a232a5…`，HEAD 在 `Assets/Scripts/Simulation/WorldState/GameWorld.cs.meta`）——这些 guid 在当前所有 `.meta` 中均不存在，EditMode 测试不加载场景所以测不出来。
3. **旧单块程序集 `CivilizationEvolution.Runtime`（`Assets/Scripts/CivilizationEvolution.Runtime.asmdef`，references 为空、91 个文件）是被 4 个新 asmdef 引用的"僵尸平行实现"**：新程序集代码 `using CivilizationEvolution.` 0 处，旧目录代码 `using MyriadCreation` 0 处，引用完全无用但足以让旧类型在新代码中"可编译地"被误用。
4. **双实现已经漂移，不是等价副本**：`GameConstants.SaveVersion` 新=13 / 旧=4；`GameWorld`、`GameManager`、`Bootstrap`、`MapRenderer`、`MapEditor`、`GreatHolyWarSystem` 各有两套完整实现；`GreatHolyWarSystem` 两份公开 API 完全相同、行数 127 vs 103，各自接在自己那套 `GameWorld` 上。
5. **重复类型规模**：950 个唯一类型名中 **318 个同名（含嵌套），其中顶层重复 262 个；跨程序集同 FQN 冲突 3 个**（`ThermalBelt`、`HumidityProvince`、`MusicPlayer`）；依赖方向在 asmdef 层"名义合规"，在命名空间层已破（World 程序集里定义/使用 `MyriadCreation.Simulation.*` 类型）。
6. **`.meta` 卫生面是干净的**：0 个 `.cs` 缺 `.meta`、0 个孤儿 `.cs.meta`、0 个重复 GUID、0 个畸形 GUID（1353/1353）。真正的 GUID 问题是**恢复时重新生成/挪用了 GUID**，导致场景与 `.asset` 里的旧 GUID 悬空。

---

## 2. P0（先修这些）

### P0-1 主场景被 `_Recovery/0.unity` 覆盖，TMP UI 全量降级为旧 Text

- 证据：`Assets/Scenes/Main.unity` 102498 B vs `Assets/_Recovery/0.unity` 100926 B，`diff` 仅 178 行；当前 Main.unity 中 TMP(guid `f4688fdb…`) 出现 **0** 次、旧 `UnityEngine.UI.Text`(`5f7201a1…`) 出现 **17** 次；HEAD 版本相反（TMP 80 次、Text 0 次）。`_Recovery/` 整个目录未跟踪（`??`）。
- 证据：`Assets/UI/Common/UIManager.cs:47-212` 声明约 25 个 `[SerializeField] private TMP_Text/ TMP_Dropdown` 字段，与实际场景中的 Text 组件类型不兼容。
- 影响：场景被替换成旧版；所有 TMP 文本字段在运行期反序列化为 null（UI 空白/NullReference），且旧场景不含新架构需要的 TMP_Dropdown 等对象。EditMode 测试不覆盖场景，故 298/305 全绿与运行时可用性无关。
- 建议：**优先从 HEAD 恢复 `Assets/Scenes/Main.unity`**（`git show f1ca764f:MyriadCreation/Assets/Scenes/Main.unity`），或在 Unity 内把场景重建到新架构；不要在当前 `_Recovery` 版本上继续做 UI 修复。

### P0-2 Main.unity 中 3 个项目 MonoBehaviour 悬空（Bootstrap / GameManager / GameWorld）

- 证据：场景内 `m_Script` GUID 共 18 个，项目脚本仅 3 个可解析（见 P0-3）；悬空 3 个：
  - `fc17d0f235e122b44983cbed448421ee` ← HEAD `MyriadCreation/Assets/UI/Bootstrap.cs.meta`，当前 `Assets/UI/Bootstrap.cs.meta` = `e21fec60a5a9b0f4b8dc444eb1052ec8`
  - `e744e3652aeca654788b9101e5a89e5f` ← HEAD `Assets/UI/GameManager.cs.meta`，当前 `Assets/UI/GameManager.cs.meta` = `109cea592bdfa694f8520acfc59850d3`
  - `e6a232a5706036d429909936f6a436f6` ← HEAD `Assets/Scripts/Simulation/WorldState/GameWorld.cs.meta`，当前 `Assets/Scripts/Simulation/MyriadCreation.Simulation.WorldState/GameWorld.cs.meta` = `dcde6bad2048e87478c6da58636d81f4`
- 影响：入口 Bootstrap、全局 GameManager、世界 GameWorld 三个组件在场景里显示为 Missing (Mono Script)，游戏无法初始化世界；这也是 `UI/Bootstrap.cs:71`、`UI/GameManager.cs:119/147`、`Scripts/Legacy/Core/Bootstrap.cs:73`、`SaveSystem.cs:98` 里 `AddComponent<…>` 兜底逻辑永远不会被触发的原因（脚本本身没挂上）。
- 建议：恢复 HEAD 三个 `.cs.meta` 的 GUID（`UI/Bootstrap.cs.meta`、`UI/GameManager.cs.meta`、`Simulation/MyriadCreation.Simulation.WorldState/GameWorld.cs.meta`），或在 Unity 中把场景三个物体重新赋脚本。

### P0-3 场景实际绑定的 MapRenderer / MapEditor 落在旧程序集，新渲染管线完全未被场景使用

- 证据（可解析的 3 个项目脚本）：
  - guid `c00d210e2240fb94b9f481eb13d3e885` → 当前 `Assets/Scripts/Render/MapRenderer.cs.meta`（命名空间 `CivilizationEvolution.Render`）；HEAD 该 GUID 属于 `Assets/Scripts/Rendering/MapRenderer.cs.meta`（现 GUID `83dc047fd499c0c47afc3d53e46258f0`）。
  - guid `e768508a1e365ab4db8244518d5b4956` → 当前 `Assets/Scripts/Map/MapEditor.cs.meta`（`CivilizationEvolution.Map`）；HEAD 属于 `Assets/Editor/MapEditing/MapEditor.cs.meta`（已删除）。
  - guid `e209577a9ac5f6e45baa592f91535940` → `Assets/UI/Common/UIManager.cs`（新 `MyriadCreation.UI.Common`，但场景类标识符仍写 `CivilizationEvolution.Runtime::CivilizationEvolution.UI.UIManager`）。
- 影响：场景渲染器是 `CivilizationEvolution.Render.MapRenderer`（`Assets/Scripts/Render/MapRenderer.cs:16`），它的 `[SerializeField] GameWorld world` 是**旧** `CivilizationEvolution.Core.GameWorld`；而场景 GameWorld 组件已悬空（P0-2），所以渲染器拿到 null 世界 → 地图无法渲染。新 `MyriadCreation.Rendering.MapRenderer`（`Assets/Scripts/Rendering/MapRenderer.cs:36`，5 个 partial）在场景中 0 引用。
- 建议：场景重新绑定新 `MyriadCreation.Rendering.MapRenderer` + 新编辑器；不要在旧类型上加 `[FormerlySerializedAs]` 掩盖问题。

### P0-4 旧程序集 `CivilizationEvolution.Runtime` 是必须迁移顺序的"僵尸单块"，直接删会连带场景

- 证据：
  - `Assets/Scripts/CivilizationEvolution.Runtime.asmdef`（`references: []`，`name: CivilizationEvolution.Runtime`）覆盖 `Assets/Scripts` 根下 19 个目录共 **91 个 `.cs`**（AI/Audio/Building/Climate/Culture/Diplomacy/Disaster/Economy/Map/Military/Politics/Race/Render/Role/Tech/Thought/War + `Legacy/Core` 12 个）。
  - 引用它但 0 使用的 asmdef：`World/world.asmdef`、`Simulation/sim.asmdef`、`Infrastructure/MyriadCreation.Infrastructure.asmdef`、`Rendering/MyriadCreation.Rendering.asmdef`；新目录里 `using CivilizationEvolution` **0 处**，旧目录里 `using MyriadCreation` **0 处**。
  - 跨程序集 FQN 冲突 3 个（同时引用世界/旧程序集即 CS0433）：`MyriadCreation.World.Climate.ThermalBelt`（`Scripts/Climate/ThermalBelt.cs:3` vs `Scripts/World/MyriadCreation.World.Climate/ThermalBelt.cs`）、`MyriadCreation.World.Climate.HumidityProvince`（`Scripts/Climate/HumidityProvince.cs:3` vs World 同名）、`MyriadCreation.Infrastructure.Audio.MusicPlayer`（`Scripts/Audio/MusicPlayer.cs:11` vs `Scripts/Infrastructure/Audio/MusicPlayer.cs`）。
  - 旧程序集内还有 2 个重复 `MapEditor`：`Scripts/Map/MapEditor.cs:14`（`CivilizationEvolution.Map`）与 `Scripts/Render/MapEditor.cs:31`（`CivilizationEvolution.Render`）。
- 影响：它不是死代码——场景的 MapRenderer/MapEditor 就在里面（P0-3）；但除了这两处场景引用外没有其它活引用。这决定了它"能删但不能先删"。
- 建议：见第 6 节删除顺序。

### P0-5 双实现漂移已发生（存档兼容 + 行为分叉）

- `GameConstants`：新 `Assets/Scripts/Core/MyriadCreation.Core.Constants/GameConstants.cs:13` `SaveVersion = 13`；旧 `Assets/Scripts/Legacy/Core/GameConstants.cs` `SaveVersion = 4`。活调用（`Assets/Scripts/Infrastructure/Save/GameSaveData.cs:22`、`MapSaveData.cs:11`、`SaveSystem.cs:79/134`）走新=13；旧 `Legacy/Core/SaveSystem.cs:68/119` 走旧=4。两份存档代码对同一 `GameConstants` 名字给出不同版本。
- `GreatHolyWarSystem`：`Scripts/War/GreatHolyWarSystem.cs:34`（旧，127 行，带中文注释）vs `Scripts/Simulation/MyriadCreation.Simulation.Systems/GreatHolyWarSystem.cs:7`（新，反编译风格，103 行）；公开 API `Declare/BindWar/OnLinkedWarEnded/Rally/Resolve/Cleanup` 完全同名同参，实现各自演化，分别被旧 `GameWorld.cs:1410-1611` 与新 `GameWorld.cs:1397-2753` 调用。
- 其它双实现：`GameWorld`（`Scripts/Legacy/Core/GameWorld.cs:25` / `Simulation/MyriadCreation.Simulation.WorldState/GameWorld.cs:43`）、`GameManager`、`Bootstrap`、`SaveSystem`、`SaveData`、`WorldConfig`、`GameEnums`、`TileData`（3 处）。
- 影响：任何"顺手改一处"都会造成行为/存档分叉；旧存档（version 4 时代）与新存档（13）混用会静默走兼容分支。
- 建议：以旧实现为"待删"方向收敛到新实现（旧树除场景两处外无活引用）；`GameConstants` 只保留新的一份并把旧文件随旧程序集删除。

---

## 3. P1（最多 8 条）

### P1-1 项目根目录残留完整反编译工程 `MyriadCreation/TEMPdecompiled-sim/`

- 证据：`MyriadCreation/TEMPdecompiled-sim/` 下 **555 个 `.cs`、2.7 MB、20 个 `MyriadCreation.Simulation.*` 子目录**，另有 `MyriadCreation.Simulation.csproj`、`Properties/`、`UnitySourceGeneratedAssemblyMonoScriptTypes_v1.cs`；全部未跟踪（`??` 555 条）。它在 `Assets/` 之外，Unity 不编译，但会误导人和后续 agent。
- 证据（导入变换）：抽样 `MyriadCreation.Simulation.State/Army.cs`，导入到 `Assets/Scripts/Simulation/…/Army.cs` 后仅三处机械变换：CRLF→LF、file-scoped namespace→块级 namespace、新增 `using Random = UnityEngine.Random;`；内容未重写。
- 建议：确认 `Assets/Scripts/Simulation` 与之一致后删除整个目录（不要留在工程根）。

### P1-2 World 程序集里定义并使用 Simulation 命名空间类型（依赖方向实质违例）

- 证据：`Assets/Scripts/World/MyriadCreation.Simulation.Systems/SettlementEconomy.cs:3`（namespace `MyriadCreation.Simulation.Systems`，物理位于 World 程序集）、`SettlementEconomySnapshot.cs` 同；消费方 `Assets/Scripts/World/MyriadCreation.World.Settlement/SettlementData.cs:4-5` `using MyriadCreation.Simulation.Economy; using MyriadCreation.Simulation.Systems;`。
- 影响：asmdef 层 World 未引用 Simulation（合规），但命名空间层 World 已经在"拥有"Simulation 类型；一旦 Simulation 程序集里出现同名类型，跨程序集引用即 CS0433（当前靠名字不撞车）。
- 建议：这两个类型移回 Simulation（或改名为 `MyriadCreation.World.Economy`），`SettlementData` 改为使用 World 自己的类型。

### P1-3 Core 程序集里定义 Simulation 命名空间类型；三个同名单文件夹横跨三程序集

- 证据：`Assets/Scripts/Core/MyriadCreation.Simulation.Definitions/DerivedEconomicProperties.cs`、`Assets/Scripts/Core/MyriadCreation.Simulation.Economy/SettlementMarket.cs`（同一命名空间在 Core 与 Simulation 各有一个文件夹）；`MyriadCreation.Simulation.Systems` 在 World 与 Simulation 各一份文件夹。
- 影响：`Core ← World ← Simulation` 的"命名空间即层"约定失效；后续 `using MyriadCreation.Simulation.*` 在 Core 内部自洽只是偶然。
- 建议：先把这两个类型下沉/改名，再统一文件夹命名（见 P1-8）。

### P1-4 旧程序集命名空间污染：3 个 `MyriadCreation.*` 文件被编进 `CivilizationEvolution.Runtime`

- 证据：`Scripts/Climate/ThermalBelt.cs:3`、`Scripts/Climate/HumidityProvince.cs:3`（`MyriadCreation.World.Climate`）、`Scripts/Audio/MusicPlayer.cs:11`（`MyriadCreation.Infrastructure.Audio`）都位于无 asmdef 的旧目录，实际编译进 `CivilizationEvolution.Runtime`；相同 FQN 在 World / Infrastructure 中也有。
- 建议：这 3 个文件随旧程序集删除即可；删除前不要从新程序集引用它们。

### P1-5 `DefaultWorldConfig.asset` 两份都指向已失效的脚本 GUID

- 证据：`Assets/Data/DefaultWorldConfig.asset` 与未跟踪的 `Assets/ScriptableObjects/DefaultWorldConfig.asset` 的 `m_Script.guid` 均为 `e156bdf2ae2f0c2498b3ecef6e932c29`（HEAD 属于 `Assets/Scripts/Core/Data/WorldConfig.cs.meta`，该文件被删除/迁移；当前 `Assets/Scripts/Core/MyriadCreation.Core.Data/WorldConfig.cs.meta` = `c8dd9470…`）。两份 asset 内容重复。
- 影响：世界生成配置 ScriptableObject 丢失脚本 → 序列化为空对象，地图参数（wrapX/seaLevel 等）取默认值。
- 建议：删掉 `ScriptableObjects/` 副本；把 `Data/DefaultWorldConfig.asset` 的 `m_Script.guid` 恢复为新 WorldConfig 的 GUID（或 Unity 内重新赋类型）。

### P1-6 反编译产物风格未清理（可维护性 + Unity 6 坑面）

- 证据：制表符缩进行数 Core **66/68**、World **89/89**、Simulation **553/554**（Legacy 0/12、Infrastructure 0/21、Rendering 0/10）；`num/num2/num5` 风格局部变量出现在 **161 个文件**（Simulation 内 1152 处）；`(object)` 强转 **191 处**；未发现 `FindObjectOfType`/`Application.LoadLevel`/空 `catch {}`（0 处，这一项干净）。
- 影响：diff 噪声、后续人工改动易引入编译错误；`num` 系列变量掩盖语义。
- 建议：随旧程序集删除一并治理；新程序集文件先挂 `.editorconfig`（tab/CRLF 统一）再批量重命名。

### P1-7 4 个新 asmdef 对旧程序集的无用引用，会掩盖依赖违例

- 证据：`Assets/Scripts/World/world.asmdef`、`Simulation/sim.asmdef`、`Infrastructure/MyriadCreation.Infrastructure.asmdef`、`Rendering/MyriadCreation.Rendering.asmdef` 的 `references` 均含 `CivilizationEvolution.Runtime`；对应目录内 `using CivilizationEvolution` 文件数 **0/0/0/0**。
- 建议：先删 4 处引用（瞬间暴露真实依赖），再删旧程序集。

### P1-8 `Core/core.asmdef`、`Simulation/sim.asmdef` 与 HEAD 命名不一致，文件夹名=命名空间的做法不统一

- 证据：HEAD 是 `Assets/Scripts/Core/MyriadCreation.Core.asmdef`、`Scripts/Simulation/MyriadCreation.Simulation.asmdef`；工作区变成 `Assets/Scripts/Core/core.asmdef`、`Scripts/Simulation/sim.asmdef`、`Scripts/World/world.asmdef`。同时工作区用 `MyriadCreation.Core.Data/`、`MyriadCreation.Simulation.Systems/` 等"文件夹=命名空间"目录，而 `Assets/Scripts/World/MyriadCreation.Simulation.Systems/` 又是错的命名空间。
- 影响：asmdef 文件名与程序集名脱钩，`find -name "*.asmdef"` 类脚本/CI 易漏；命名空间镜像目录与旧程序集目录风格混用。
- 建议：恢复到 HEAD 的 asmdef 文件名，并在一次提交内把目录名与命名空间对齐。

---

## 4. P2（简述）

- **P2-1 重复类型规模**：1289 个定义 / 950 唯一名 → 同名 318 组（含嵌套），顶层 262 组；绝大部分是"旧 `CivilizationEvolution.*`（91 文件）vs 新 `MyriadCreation.*`"。全表见附录 A。
- **P2-2 嵌套类型漂移实例**：`CivilizationEvolution.Politics.InheritanceScope` 在 `GovernmentConstraints.cs:83` 与 `InheritanceLaw.cs:19` 两处定义且枚举值不同（`ClanOnly,BloodRegardless` vs `ClanOnly,CognaticKin`）；`InheritanceBranch`（`SeniorLine,Fraternal` vs `EldestLine,Collateral`）、`InheritanceGender`（`ElderFirst/YoungerFirst` 对应 `Seniority/Juniority`）。同名不同值，属恢复期逻辑漂移，见 P2 级（当前无活调用方）。
- **P2-3 删除/新增规模**：422 个 `.cs` 被删（935 删除中）；核对方式是从 `HEAD` 提取被删文件的类型名，当前树多数已在 `MyriadCreation.Simulation.*` 新路径重建，属"删除+重加"而非真删除。未发现"被删类型仍被活代码引用"（编译面证据：0 处 `using CivilizationEvolution`，0 处旧类型被新代码使用）。
- **P2-4 `.meta` 配对干净**：951 个 `.cs` 全部有 `.meta`；0 孤儿 `.cs.meta`；0 重复 GUID；0 畸形 GUID。风险不在卫生，在 GUID 复用/重生成（P0-2/P0-3/P1-5）。
- **P2-5 `Assets/_Recovery/` 恢复残留**：`0.unity`、`0 (1).unity` + meta，全部未跟踪；且 `0.unity` 已被复制为 Main.unity（P0-1）。建议确认后删除。
- **P2-6 旧 `StreamingAssets/Localization` 16 个文件被删**：是否仍有按路径加载的代码未验证（**未验证**）；建议 grep `StreamingAssets` 后确认。

---

## 5. 重复类型摘要（判定规则 + 最危险 12 个）

判定规则（可复现）：
1. 用 awk 扫描全部 `.cs` 的顶层类型声明行，取 `(class|struct|enum|interface|record) Name`；
2. 以最近祖先 `.asmdef` 判定程序集，取第一个 `namespace` 判定命名空间；
3. **同名** = 简单名相同；**顶层重复** = 缩进 ≤4 且非 partial 分部；**FQN 冲突** = 名称+命名空间相同但程序集不同（会 CS0433）；
4. 判定：活代码 = 有 `using`/直接引用；双实现漂移 = 同名 API 但实现/常量/枚举值不同；死代码 = 0 引用且处于待删旧树。

汇总：**318 组同名（含嵌套）/ 262 组顶层重复 / 3 组跨程序集 FQN 冲突**；旧程序集 91 文件对 257 组顶层重复贡献绝大多数。

| # | 类型名 | 路径 A（程序集） | 路径 B（程序集） | 引用/判定 |
|---|---|---|---|---|
| 1 | `GameConstants` | `Scripts/Core/MyriadCreation.Core.Constants/GameConstants.cs:3`（Core） | `Scripts/Legacy/Core/GameConstants.cs:6`（旧） | 活新 + 旧活；**双实现漂移** SaveVersion 13 vs 4 |
| 2 | `GreatHolyWarSystem` | `Scripts/Simulation/MyriadCreation.Simulation.Systems/GreatHolyWarSystem.cs:7`（Sim） | `Scripts/War/GreatHolyWarSystem.cs:34`（旧） | 各被自家 GameWorld 调用；**双实现漂移** 103 vs 127 行 |
| 3 | `GameWorld` | `Scripts/Simulation/MyriadCreation.Simulation.WorldState/GameWorld.cs:43`（Sim） | `Scripts/Legacy/Core/GameWorld.cs:25`（旧） | 场景期望前者（GUID 悬空）；旧树仅 Legacy 内部引用；**双实现漂移** |
| 4 | `MapRenderer` | `Scripts/Rendering/MapRenderer.cs:36`（新，5 partial）（Rendering） | `Scripts/Render/MapRenderer.cs:16`（旧，场景绑定） | 旧=活（场景），新=0 引用；**双实现漂移 + 绑错** |
| 5 | `MapEditor` | `Scripts/Map/MapEditor.cs:14`（旧，场景绑定） | `Scripts/Render/MapEditor.cs:31`（旧，另一个命名空间） | 旧程序集内两份 + HEAD Editor 版已删；**三份实现** |
| 6 | `GameManager` | `UI/GameManager.cs:27`（UI，新） | `Scripts/Legacy/Core/GameManager.cs:12`（旧） | 新活；旧仅供旧 Bootstrap；**双实现** |
| 7 | `Bootstrap` | `UI/Bootstrap.cs:29`（UI，新） | `Scripts/Legacy/Core/Bootstrap.cs:29`（旧） | 两份都活；场景 GUID 悬空；**双实现** |
| 8 | `SaveSystem` / `SaveData` | `Scripts/Infrastructure/Save/SaveSystem.cs:31`、`SaveData.cs:20`（Infra） | `Scripts/Legacy/Core/SaveSystem.cs:20/199`（旧） | 新活；旧 SaveVersion 4；**双实现漂移** |
| 9 | `UIManager` | `UI/Common/UIManager.cs:37`（UI，4 partial） | 旧 `CivilizationEvolution.UI.UIManager` 已无文件（`Scripts/UI` 为空目录） | 活的只有新；场景标识符仍指旧名；**残留标识符** |
| 10 | `WorldConfig` | `Scripts/Core/MyriadCreation.Core.Data/WorldConfig.cs:7`（Core） | `Scripts/Legacy/Core/WorldConfig.cs:13`（旧） | 新活；旧死；**双实现**；asset GUID 悬空（P1-5） |
| 11 | `TileData` | `Scripts/Core/MyriadCreation.Core.Data/TileData.cs:8`（Core） | `Scripts/Legacy/Core/TileData.cs:12`（旧） | 新活；旧死；另有 `Scripts/Military/PathfindingSystem.cs:340` 内 `class TileData`；**三份** |
| 12 | `GameEnums` | `Scripts/Core/MyriadCreation.Core.Enums/GameEnums.cs:6`（Core） | `Scripts/Legacy/Core/GameEnums.cs:10`（旧） | 新活；旧死；**双实现**（枚举值未逐项比对：待验证） |

> 完整 262 行顶层重复类型表见附录 A；318 组含嵌套的完整口径可用第 5 节规则重跑。

---

## 6. 旧程序集 `CivilizationEvolution.Runtime`：能删，但不能先删

**结论：它不是"死代码"，是"仅剩 2 处活引用的迁移中代码"。** 活引用只有场景两处：`Scripts/Render/MapRenderer.cs`（guid `c00d…`）、`Scripts/Map/MapEditor.cs`（guid `e768…`）。其余 89 个文件只被旧树内部互相引用，旧树里 `using MyriadCreation` = 0。

**删除顺序（每步都要跑一次全量 EditMode）：**
1. **先修场景**：恢复 `Main.unity`（P0-1），重新绑定 Bootstrap/GameManager/GameWorld（P0-2）与新 MapRenderer/MapEditor（P0-3）。删除旧程序集之前场景必须已不再引用它。
2. **砍引用**：从 `World/world.asmdef`、`Simulation/sim.asmdef`、`Infrastructure/MyriadCreation.Infrastructure.asmdef`、`Rendering/MyriadCreation.Rendering.asmdef` 删除 `CivilizationEvolution.Runtime`（0 使用，删除应立即编译通过）。
3. **删文件**：先删 `Assets/Scripts/Legacy/**`（12 文件，纯旧 Core：Bootstrap/GameManager/GameWorld/SaveSystem/WorldConfig/TileData/GameEnums…），再删 19 个旧目录共 91 个 `.cs` 及其 `.cs.meta`；同步删除 `Assets/Scripts/CivilizationEvolution.Runtime.asmdef`。
4. **收尾**：删除 `Assets/Scripts/Climate/ThermalBelt.cs`、`HumidityProvince.cs`、`Audio/MusicPlayer.cs` 前先移除 P1-4 的同 FQN 冲突；删除 `TEMPdecompiled-sim/`、`_Recovery/`；修复 `DefaultWorldConfig.asset` 的 GUID（P1-5）。

**风险：**
- 删旧程序集会连带删掉旧 `GameWorld`——如果场景恢复失败（P0-2 未解决），整个游戏不可启动；顺序不能颠倒。
- 旧树里有些类型与新树同名不同值（`GameExtensions` 类、`InheritanceScope` 嵌套枚举），如果**只删文件不删 asmdef**，残留文件会静默落到 Assembly-CSharp（没有全局 asmdef 兜底）——必须文件与 asmdef 一起删。
- 旧 `MapRenderer` 的序列化值（`mapWidth/displayMode/world`）不能靠改 GUID 迁移到新 `MapRenderer`：新类字段不同（`Rendering/MapRenderer.cs:36` 起 `_cameraFitSize`/`_maxZoom` 等），必须在 Unity 内重新赋值。

---

## 7. 建议立即处理清单

**先修 3 件事（按杠杆排序）：**
1. `MyriadCreation/Assets/Scenes/Main.unity`：从 HEAD 恢复，或在 Unity 内把 6 个项目组件重新绑定到新架构（当前是 `_Recovery/0.unity` 旧场景覆盖，P0-1/P0-2/P0-3 一网打尽）。
2. `MyriadCreation/Assets/UI/Bootstrap.cs.meta`、`MyriadCreation/Assets/UI/GameManager.cs.meta`、`MyriadCreation/Assets/Scripts/Simulation/MyriadCreation.Simulation.WorldState/GameWorld.cs.meta`：恢复 HEAD 的 guid（`fc17d0f2…`/`e744e365…`/`e6a232a5…`），否则场景 3 个组件永久 Missing。
3. `MyriadCreation/Assets/Scripts/CivilizationEvolution.Runtime.asmdef` + 4 个新 asmdef 的引用：先删 4 处 `references`，再按第 6 节顺序解体旧树；同步删 `TEMPdecompiled-sim/` 与 `_Recovery/`。

**其后（1-2 天内）：** `Data/DefaultWorldConfig.asset` GUID（P1-5）→ World/Simulation 命名空间越界（P1-2/P1-3）→ `GameConstants.SaveVersion` 单一化（P0-5）→ 反编译风格治理（P1-6）。

**不要做：** 不要为了"让场景能跑"把新 `MapRenderer` 改回旧命名空间；不要在旧类型上加 `[FormerlySerializedAs]`；不要只恢复 `Main.unity` 而不恢复三个 `.meta`（会退化成一个无 Bootstrap 的空场景）。

---

## 8. 未验证项（不编造）

- 旧 `StreamingAssets/Localization` 16 个删除文件是否仍被代码按路径加载（未验证）。
- `GameEnums` 新旧枚举值是否逐项一致（未验证，仅视为风险）。
- `Assets/_Recovery/0 (1).unity` 的用途与是否还被任何配置引用（未验证）。
- 旧程序集 91 个文件中是否还有第 4 类 Monobehaviour 被 `.prefab`/其它场景引用：已查 3 个 prefab 全在 TextMesh Pro 示例包内、工程仅 Main.unity 一个构建场景（`ProjectSettings/EditorBuildSettings.asset`），但未逐一反查所有 `.asset`（仅 19 个有 `m_Script`）之外的序列化引用。
- 场景内 15 个未解析 GUID 均判定为 Unity/TMP 包内建脚本（按 HEAD 场景同类组件推断），未在 `Library/PackageCache` 中逐个反查（未验证）。

---

## 附录 A：顶层重复类型全表（类型名 + 两处路径）

> 自动生成：条件 `缩进<=4`、同名 ≥2、非 partial 分部；顺序按类型名字母序。共 262 组（含跨程序集 FQN 冲突 3 组）。

| 类型名 | 路径（程序集） |
|---|---|
| AIBuildDecision | Scripts/Map/SettlementAISystem.cs:416 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.AI/AIBuildDecision.cs:5 (Simulation) |
| AIController | Scripts/AI/AIController.cs:19 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.AI/AIController.cs:16 (Simulation) |
| AIDecisionSystem | Scripts/AI/AIDecisionSystem.cs:14 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.AI/AIDecisionSystem.cs:8 (Simulation) |
| AIGoal | Scripts/AI/AIController.cs:586 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.AI/AIGoal.cs:3 (Simulation) |
| AIManager | Scripts/AI/AIController.cs:601 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.AI/AIManager.cs:9 (Simulation) |
| AIPersonality | Scripts/AI/AIController.cs:484 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.AI/AIPersonality.cs:10 (Simulation) |
| ActiveBuilding | Scripts/Building/BuildingSystem.cs:220 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/ActiveBuilding.cs:6 (Simulation) |
| ActiveDisaster | Scripts/Disaster/DisasterSystem.cs:410 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/ActiveDisaster.cs:8 (Simulation) |
| ActiveDisease | Scripts/Disaster/DisasterSystem.cs:867 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/ActiveDisease.cs:8 (Simulation) |
| ActiveJuncture | Scripts/Politics/RegimeChangeDynamics.cs:89 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/ActiveJuncture.cs:7 (Simulation) |
| Allele | Scripts/Race/DnaSystem.cs:18 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/Allele.cs:3 (Simulation) |
| Alliance | Scripts/Diplomacy/DiplomacySystem.cs:196 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/Alliance.cs:7 (Simulation) |
| AllianceType | Scripts/Diplomacy/DiplomacySystem.cs:185 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/AllianceType.cs:3 (Simulation) |
| Army | Scripts/Simulation/MyriadCreation.Simulation.State/Army.cs:13 (Simulation) ; Scripts/War/WarSystem.cs:44 (CivEvoRuntime(root)) |
| AssemblyComposition | Scripts/Politics/GovernmentComposition.cs:120 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/AssemblyComposition.cs:3 (Simulation) |
| AtmosphericCirculation | Scripts/Map/AtmosphericCirculation.cs:23 (CivEvoRuntime(root)) ; Scripts/World/MyriadCreation.World.Climate/AtmosphericCirculation.cs:8 (World) |
| BarrierSystem | Scripts/Map/BarrierSystem.cs:15 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/BarrierSystem.cs:9 (Simulation) |
| BattleResult | Scripts/Simulation/MyriadCreation.Simulation.State/BattleResult.cs:7 (Simulation) ; Scripts/War/WarSystem.cs:438 (CivEvoRuntime(root)) |
| BondType | Scripts/Role/CharacterSystem.cs:1376 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/BondType.cs:3 (Simulation) |
| Bootstrap | Scripts/Legacy/Core/Bootstrap.cs:29 (CivEvoRuntime(root)) ; UI/Bootstrap.cs:29 (UI) |
| BottleneckType | Scripts/Map/SettlementTypologyEnums.cs:126 (CivEvoRuntime(root)) ; Scripts/World/MyriadCreation.World.Settlement/BottleneckType.cs:3 (World) |
| BuildableType | Scripts/Map/BuildingRequirements.cs:17 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/BuildableType.cs:3 (Simulation) |
| BuildingAvailability | Scripts/Map/BuildingRequirements.cs:208 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/BuildingAvailability.cs:5 (Simulation) |
| BuildingAvailabilitySystem | Scripts/Map/BuildingAvailabilitySystem.cs:13 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/BuildingAvailabilitySystem.cs:10 (Simulation) |
| BuildingCategory | Scripts/Building/BuildingSystem.cs:230 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/BuildingCategory.cs:3 (Simulation) |
| BuildingData | Scripts/Military/PathfindingSystem.cs:356 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/BuildingData.cs:3 (Simulation) |
| BuildingDef | Scripts/Building/BuildingSystem.cs:205 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/BuildingDef.cs:7 (Simulation) |
| BuildingRequirement | Scripts/Map/BuildingRequirements.cs:78 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/BuildingRequirement.cs:8 (Simulation) |
| BuildingSystem | Scripts/Building/BuildingSystem.cs:14 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/BuildingSystem.cs:13 (Simulation) |
| CanonizationSystem | Scripts/Culture/CanonizationSystem.cs:32 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/CanonizationSystem.cs:7 (Simulation) |
| Caravan | Scripts/Economy/EconomySystem.cs:176 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/Caravan.cs:8 (Simulation) |
| CarryingCapacitySystem | Scripts/Politics/CarryingCapacitySystem.cs:12 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/CarryingCapacitySystem.cs:9 (Simulation) |
| CasusBelli | Scripts/Diplomacy/WarJustificationSystem.cs:14 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/CasusBelli.cs:7 (Simulation) |
| CentralInstitution | Scripts/Politics/GovernmentComposition.cs:108 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/CentralInstitution.cs:3 (Simulation) |
| CentralSuccession | Scripts/Politics/GovernmentComposition.cs:96 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/CentralSuccession.cs:3 (Simulation) |
| CharacterBond | Scripts/Role/CharacterSystem.cs:1347 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/CharacterBond.cs:6 (Simulation) |
| CharacterData | Scripts/Role/CharacterSystem.cs:153 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/CharacterData.cs:11 (Simulation) |
| CharacterManager | Scripts/Role/CharacterSystem.cs:1570 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/CharacterManager.cs:12 (Simulation) |
| CharacterRelation | Scripts/Role/CharacterSystem.cs:1312 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/CharacterRelation.cs:9 (Simulation) |
| CharacterRole | Scripts/Role/CharacterSystem.cs:760 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/CharacterRole.cs:3 (Simulation) |
| CharacterTemplateDef | Scripts/Legacy/Core/ContentTypes.cs:163 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Modding/CharacterTemplateDef.cs:8 (Simulation) |
| Chronicle | Scripts/Legacy/Core/Chronicle.cs:25 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/Chronicle.cs:7 (Simulation) |
| ChronicleEntry | Scripts/Legacy/Core/Chronicle.cs:12 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/ChronicleEntry.cs:5 (Simulation) |
| CirculationMode | Scripts/Core/MyriadCreation.Core.Enums/GameEnums.cs:8 (Core) ; Scripts/World/MyriadCreation.World.Generation/MapGenerationConfig.cs:61 (World) |
| CityFocus | Scripts/Map/SettlementTypologyEnums.cs:51 (CivEvoRuntime(root)) ; Scripts/World/MyriadCreation.World.Settlement/CityFocus.cs:3 (World) |
| CityForm | Scripts/Map/SettlementTypologyEnums.cs:68 (CivEvoRuntime(root)) ; Scripts/World/MyriadCreation.World.Settlement/CityForm.cs:3 (World) |
| ClassEmergenceEvents | Scripts/Economy/ClassEmergenceEvents.cs:13 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Economy/ClassEmergenceEvents.cs:8 (Simulation) |
| ClassNeedDimension | Scripts/Politics/ClassNeedsSystem.cs:24 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/ClassNeedDimension.cs:3 (Simulation) |
| ClassNeedReport | Scripts/Politics/ClassNeedsSystem.cs:93 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/ClassNeedReport.cs:10 (Simulation) |
| ClassNeedWeightTable | Scripts/Politics/ClassNeedsSystem.cs:112 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/ClassNeedWeightTable.cs:7 (Simulation) |
| ClassNeedsSystem | Scripts/Politics/ClassNeedsSystem.cs:216 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/ClassNeedsSystem.cs:10 (Simulation) |
| ClassProfile | Scripts/Politics/SocietySystem.cs:25 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/ClassProfile.cs:9 (Simulation) |
| CombatManager | Scripts/Simulation/MyriadCreation.Simulation.Systems/CombatManager.cs:12 (Simulation) ; Scripts/War/WarSystem.cs:261 (CivEvoRuntime(root)) |
| ComponentChoice | Scripts/Politics/GovernmentComposition.cs:292 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/ComponentChoice.cs:7 (Simulation) |
| ContentRegistry | Scripts/Legacy/Core/ContentRegistry.cs:29 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Modding/ContentRegistry.cs:12 (Simulation) |
| CrimeDefinition | Scripts/Simulation/MyriadCreation.Simulation.Definitions/CrimeDefinition.cs:7 (Simulation) ; Scripts/Thought/ThoughtSystem.cs:443 (CivEvoRuntime(root)) |
| CrimeType | Scripts/Simulation/MyriadCreation.Simulation.Definitions/CrimeType.cs:3 (Simulation) ; Scripts/Thought/ThoughtSystem.cs:402 (CivEvoRuntime(root)) |
| CriticalJunctureType | Scripts/Politics/RegimeChangeDynamics.cs:24 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/CriticalJunctureType.cs:3 (Simulation) |
| CultureData | Scripts/Culture/CultureData.cs:11 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/CultureData.cs:11 (Simulation) |
| CultureSimilarity | Scripts/Culture/CultureData.cs:89 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/CultureSimilarity.cs:9 (Simulation) |
| CurrencySystem | Scripts/Economy/EconomySystem.cs:222 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/CurrencySystem.cs:9 (Simulation) |
| Deity | Scripts/Simulation/MyriadCreation.Simulation.State/Deity.cs:7 (Simulation) ; Scripts/Thought/ThoughtSystem.cs:224 (CivEvoRuntime(root)) |
| DiplomacyManager | Scripts/Diplomacy/DiplomacySystem.cs:353 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/DiplomacyManager.cs:11 (Simulation) |
| DiplomaticEvent | Scripts/Diplomacy/DiplomacySystem.cs:148 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Diplomacy/DiplomaticEvent.cs:7 (Simulation) |
| DiplomaticEventType | Scripts/Diplomacy/DiplomacySystem.cs:159 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/DiplomaticEventType.cs:3 (Simulation) |
| DiplomaticRelation | Scripts/Diplomacy/DiplomacySystem.cs:14 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/DiplomaticRelation.cs:11 (Simulation) |
| DisasterCategory | Scripts/Disaster/DisasterSystem.cs:454 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/DisasterCategory.cs:3 (Simulation) |
| DisasterDef | Scripts/Disaster/DisasterSystem.cs:423 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/DisasterDef.cs:6 (Simulation) |
| DisasterSystem | Scripts/Disaster/DisasterSystem.cs:14 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/DisasterSystem.cs:12 (Simulation) |
| DisasterType | Scripts/Disaster/DisasterSystem.cs:434 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/DisasterType.cs:3 (Simulation) |
| DiseaseDef | Scripts/Disaster/DisasterSystem.cs:884 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/DiseaseDef.cs:6 (Simulation) |
| DiseaseSystem | Scripts/Disaster/DisasterSystem.cs:467 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/DiseaseSystem.cs:12 (Simulation) |
| DiseaseType | Scripts/Disaster/DisasterSystem.cs:897 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/DiseaseType.cs:3 (Simulation) |
| DnaData | Scripts/Race/DnaSystem.cs:58 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/DnaData.cs:9 (Simulation) |
| DnaExpression | Scripts/Race/DnaSystem.cs:75 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/DnaExpression.cs:6 (Simulation) |
| DnaLocus | Scripts/Race/DnaSystem.cs:26 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/DnaLocus.cs:3 (Simulation) |
| DnaSystem | Scripts/Race/DnaSystem.cs:121 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/DnaSystem.cs:12 (Simulation) |
| Doctrine | Scripts/Simulation/MyriadCreation.Simulation.State/Doctrine.cs:7 (Simulation) ; Scripts/Thought/ThoughtSystem.cs:235 (CivEvoRuntime(root)) |
| DoctrineCategory | Scripts/Simulation/MyriadCreation.Simulation.Definitions/DoctrineCategory.cs:3 (Simulation) ; Scripts/Thought/ThoughtSystem.cs:243 (CivEvoRuntime(root)) |
| DoctrineOptionDef | Scripts/Culture/DoctrinePool.cs:11 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/DoctrineOptionDef.cs:7 (Simulation) |
| DoctrinePool | Scripts/Culture/DoctrinePool.cs:29 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/DoctrinePool.cs:6 (Simulation) |
| EcclesiasticalTitle | Scripts/Simulation/MyriadCreation.Simulation.State/EcclesiasticalTitle.cs:3 (Simulation) ; Scripts/Thought/ThoughtSystem.cs:192 (CivEvoRuntime(root)) |
| EconomicalArchetype | Scripts/Role/CharacterSystem.cs:90 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/EconomicalArchetype.cs:3 (Simulation) |
| EconomicalArchetypeInfo | Scripts/Role/CharacterSystem.cs:106 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/EconomicalArchetypeInfo.cs:3 (Simulation) |
| EconomicalArchetypes | Scripts/Role/CharacterSystem.cs:123 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/EconomicalArchetypes.cs:3 (Simulation) |
| EconomyManager | Scripts/Economy/EconomySystem.cs:327 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/EconomyManager.cs:11 (Simulation) |
| EffectEntry | Scripts/Legacy/Core/ContentTypes.cs:21 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Modding/EffectEntry.cs:6 (Simulation) |
| EligibilityRules | Scripts/Politics/GovernmentComposition.cs:195 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/EligibilityRules.cs:10 (Simulation) |
| EligibilityScope | Scripts/Politics/GovernmentComposition.cs:178 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/EligibilityScope.cs:3 (Simulation) |
| EpithetCatalog | Scripts/Culture/EpithetCatalog.cs:38 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/EpithetCatalog.cs:5 (Simulation) |
| EpithetConnotation | Scripts/Culture/EpithetCatalog.cs:8 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/EpithetConnotation.cs:3 (Simulation) |
| EpithetDef | Scripts/Culture/EpithetCatalog.cs:25 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/EpithetDef.cs:3 (Simulation) |
| EpithetSystem | Scripts/Culture/PersonalityArchetype.cs:118 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/EpithetSystem.cs:6 (Simulation) |
| EpithetTier | Scripts/Culture/EpithetCatalog.cs:17 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/EpithetTier.cs:3 (Simulation) |
| EthnicGroupDef | Scripts/Legacy/Core/ContentTypes.cs:103 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Modding/EthnicGroupDef.cs:8 (Simulation) |
| EthosDef | Scripts/Legacy/Core/ContentTypes.cs:33 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Modding/EthosDef.cs:8 (Simulation) |
| EvaluationLevel | Scripts/Culture/EvaluationSystem.cs:14 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/EvaluationLevel.cs:3 (Simulation) |
| EvaluationSystem | Scripts/Culture/EvaluationSystem.cs:29 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/EvaluationSystem.cs:7 (Simulation) |
| EvolutionStage | Scripts/Map/SettlementTypologyEnums.cs:156 (CivEvoRuntime(root)) ; Scripts/World/MyriadCreation.World.Settlement/EvolutionStage.cs:3 (World) |
| Faction | Scripts/Politics/FactionSystem.cs:68 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/Faction.cs:10 (Simulation) |
| FactionManager | Scripts/Politics/FactionSystem.cs:107 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/FactionManager.cs:11 (Simulation) |
| FactionPlatform | Scripts/Politics/FactionSystem.cs:38 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/FactionPlatform.cs:7 (Simulation) |
| FactionStance | Scripts/Politics/FactionSystem.cs:26 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/FactionStance.cs:3 (Simulation) |
| FaithOrganizationType | Scripts/Simulation/MyriadCreation.Simulation.Definitions/FaithOrganizationType.cs:3 (Simulation) ; Scripts/Thought/ThoughtSystem.cs:212 (CivEvoRuntime(root)) |
| FaithSystem | Scripts/Simulation/MyriadCreation.Simulation.Systems/FaithSystem.cs:10 (Simulation) ; Scripts/Thought/ThoughtSystem.cs:70 (CivEvoRuntime(root)) |
| FaithType | Scripts/Simulation/MyriadCreation.Simulation.Definitions/FaithType.cs:3 (Simulation) ; Scripts/Thought/ThoughtSystem.cs:201 (CivEvoRuntime(root)) |
| FamilyNode | Scripts/Role/CharacterSystem.cs:1393 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/FamilyNode.cs:9 (Simulation) |
| FamilyTraditionDef | Scripts/Legacy/Core/ContentTypes.cs:144 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Modding/FamilyTraditionDef.cs:8 (Simulation) |
| GameConstants | Scripts/Core/MyriadCreation.Core.Constants/GameConstants.cs:3 (Core) ; Scripts/Legacy/Core/GameConstants.cs:6 (CivEvoRuntime(root)) |
| GameEnums | Scripts/Core/MyriadCreation.Core.Enums/GameEnums.cs:6 (Core) ; Scripts/Legacy/Core/GameEnums.cs:10 (CivEvoRuntime(root)) |
| GameEvent | Scripts/Legacy/Core/GameWorld.cs:1661 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Events/GameEvent.cs:5 (Simulation) |
| GameEventType | Scripts/Legacy/Core/GameWorld.cs:1647 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/GameEventType.cs:3 (Simulation) |
| GameManager | Scripts/Legacy/Core/GameManager.cs:12 (CivEvoRuntime(root)) ; UI/GameManager.cs:27 (UI) |
| GameWorld | Scripts/Legacy/Core/GameWorld.cs:25 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.WorldState/GameWorld.cs:43 (Simulation) |
| GoodsDef | Scripts/Economy/EconomySystem.cs:9 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/GoodsDef.cs:6 (Simulation) |
| GreatHolyWarState | Scripts/Simulation/MyriadCreation.Simulation.State/GreatHolyWarState.cs:7 (Simulation) ; Scripts/War/GreatHolyWarSystem.cs:16 (CivEvoRuntime(root)) |
| GreatHolyWarSystem | Scripts/Simulation/MyriadCreation.Simulation.Systems/GreatHolyWarSystem.cs:7 (Simulation) ; Scripts/War/GreatHolyWarSystem.cs:34 (CivEvoRuntime(root)) |
| HoldridgeBiomeClassifier | Scripts/Map/HoldridgeBiomeClassifier.cs:17 (CivEvoRuntime(root)) ; Scripts/World/MyriadCreation.World.Biome/HoldridgeBiomeClassifier.cs:7 (World) |
| HydraulicErosion | Scripts/Map/HydraulicErosion.cs:22 (CivEvoRuntime(root)) ; Scripts/World/MyriadCreation.World.Terrain/HydraulicErosion.cs:8 (World) |
| HydrologySystem | Scripts/Map/HydrologySystem.cs:21 (CivEvoRuntime(root)) ; Scripts/World/MyriadCreation.World.Hydrology/HydrologySystem.cs:8 (World) |
| IGameEventListener | Scripts/Legacy/Core/GameWorld.cs:1671 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Events/IGameEventListener.cs:3 (Simulation) |
| ISimulationCommand | Scripts/Core/MyriadCreation.Core.Contracts/ISimulationCommand.cs:3 (Core) ; Scripts/Core/MyriadCreation.Core.Simulation/ISimulationCommand.cs:3 (Core) |
| ISimulationQuery | Scripts/Core/MyriadCreation.Core.Contracts/ISimulationQuery.cs:3 (Core) ; Scripts/Core/MyriadCreation.Core.Simulation/ISimulationQuery.cs:3 (Core) |
| IdeologyMovement | Scripts/Simulation/MyriadCreation.Simulation.State/IdeologyMovement.cs:9 (Simulation) ; Scripts/Thought/ThoughtSystem.cs:488 (CivEvoRuntime(root)) |
| InheritanceAge | Scripts/Politics/InheritanceLaw.cs:43 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/InheritanceAge.cs:3 (Simulation) |
| InheritanceBranch | Scripts/Politics/InheritanceLaw.cs:26 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/InheritanceBranch.cs:3 (Simulation) |
| InheritanceGender | Scripts/Politics/InheritanceLaw.cs:33 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/InheritanceGender.cs:3 (Simulation) |
| InheritanceLaw | Scripts/Politics/InheritanceLaw.cs:80 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/InheritanceLaw.cs:8 (Simulation) |
| InheritanceScope | Scripts/Politics/InheritanceLaw.cs:19 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/InheritanceScope.cs:3 (Simulation) |
| InnovationDef | Scripts/Simulation/MyriadCreation.Simulation.Definitions/InnovationDef.cs:8 (Simulation) ; Scripts/Tech/InnovationTypes.cs:108 (CivEvoRuntime(root)) |
| InnovationDomain | Scripts/Simulation/MyriadCreation.Simulation.Definitions/InnovationDomain.cs:3 (Simulation) ; Scripts/Tech/InnovationTypes.cs:18 (CivEvoRuntime(root)) |
| InnovationDomainMap | Scripts/Simulation/MyriadCreation.Simulation.Definitions/InnovationDomainMap.cs:5 (Simulation) ; Scripts/Tech/InnovationTypes.cs:62 (CivEvoRuntime(root)) |
| InnovationField | Scripts/Simulation/MyriadCreation.Simulation.Definitions/InnovationField.cs:3 (Simulation) ; Scripts/Tech/InnovationTypes.cs:27 (CivEvoRuntime(root)) |
| InnovationTree | Scripts/Simulation/MyriadCreation.Simulation.State/InnovationTree.cs:13 (Simulation) ; Scripts/Tech/InnovationTree.cs:16 (CivEvoRuntime(root)) |
| IntBoolEntry | Scripts/Core/MyriadCreation.Core.Dto/IntBoolEntry.cs:6 (Core) ; Scripts/Legacy/Core/SaveSystem.cs:236 (CivEvoRuntime(root)) |
| IntFloatEntry | Scripts/Core/MyriadCreation.Core.Dto/IntFloatEntry.cs:6 (Core) ; Scripts/Legacy/Core/SaveSystem.cs:226 (CivEvoRuntime(root)) |
| JunctureOutcomeType | Scripts/Politics/RegimeChangeDynamics.cs:36 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/JunctureOutcomeType.cs:3 (Simulation) |
| LandInheritanceMode | Scripts/Politics/InheritanceLaw.cs:68 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/LandInheritanceMode.cs:3 (Simulation) |
| LanguageDef | Scripts/Legacy/Core/ContentTypes.cs:85 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Modding/LanguageDef.cs:8 (Simulation) |
| Law | Scripts/Simulation/MyriadCreation.Simulation.State/Law.cs:9 (Simulation) ; Scripts/Thought/ThoughtSystem.cs:452 (CivEvoRuntime(root)) |
| LawCategory | Scripts/Simulation/MyriadCreation.Simulation.Definitions/LawCategory.cs:6 (Simulation) ; Scripts/Thought/ThoughtSystem.cs:462 (CivEvoRuntime(root)) |
| LawSource | Scripts/Simulation/MyriadCreation.Simulation.Definitions/LawSource.cs:3 (Simulation) ; Scripts/Thought/ThoughtSystem.cs:392 (CivEvoRuntime(root)) |
| LawSystem | Scripts/Simulation/MyriadCreation.Simulation.Systems/LawSystem.cs:8 (Simulation) ; Scripts/Thought/ThoughtSystem.cs:258 (CivEvoRuntime(root)) |
| LocalAppointAuthority | Scripts/Politics/GovernmentComposition.cs:128 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/LocalAppointAuthority.cs:3 (Simulation) |
| LocalLordship | Scripts/Politics/GovernmentComposition.cs:136 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/LocalLordship.cs:3 (Simulation) |
| LocalScope | Scripts/Politics/GovernmentComposition.cs:159 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/LocalScope.cs:3 (Simulation) |
| LocalSuccession | Scripts/Politics/GovernmentComposition.cs:149 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/LocalSuccession.cs:3 (Simulation) |
| Localization | Scripts/Core/MyriadCreation.Core/Localization.cs:8 (Core) ; Scripts/Legacy/Core/Localization.cs:17 (CivEvoRuntime(root)) |
| LocusFrequency | Scripts/Race/DnaSystem.cs:110 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/LocusFrequency.cs:7 (Simulation) |
| LocusPair | Scripts/Race/DnaSystem.cs:38 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/LocusPair.cs:7 (Simulation) |
| ManpowerSystem | Scripts/Politics/CarryingCapacitySystem.cs:139 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/ManpowerSystem.cs:8 (Simulation) |
| MapDisplayMode | Scripts/Render/MapRenderer.cs:668 (CivEvoRuntime(root)) ; Scripts/Rendering/MapRenderer.cs:590 (Rendering) |
| MapEditor | Scripts/Map/MapEditor.cs:14 (CivEvoRuntime(root)) ; Scripts/Render/MapEditor.cs:31 (CivEvoRuntime(root)) |
| MapGenerationConfig | Scripts/Map/MapGenerationConfig.cs:30 (CivEvoRuntime(root)) ; Scripts/World/MyriadCreation.World.Generation/MapGenerationConfig.cs:10 (World) |
| MapRenderer | Scripts/Render/MapRenderer.cs:16 (CivEvoRuntime(root)) ; Scripts/Rendering/MapRenderer.Colors.cs:34 (Rendering) ; Scripts/Rendering/MapRenderer.Nomadic.cs:32 (Rendering) ; Scripts/Rendering/MapRenderer.Texture.cs:35 (Rendering) ; Scripts/Rendering/MapRenderer.cs:36 (Rendering) |
| MapSizePreset | Scripts/Core/MyriadCreation.Core.Enums/MapSizePreset.cs:3 (Core) ; Scripts/Legacy/Core/Bootstrap.cs:11 (CivEvoRuntime(root)) ; Scripts/World/MyriadCreation.World.Generation/MapGenerationConfig.cs:39 (World) |
| MapWrapMode | Scripts/Core/MyriadCreation.Core.Enums/MapWrapMode.cs:3 (Core) ; Scripts/Legacy/Core/Bootstrap.cs:22 (CivEvoRuntime(root)) |
| MentalDisorderDef | Scripts/Role/MentalHealth.cs:32 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/MentalDisorderDef.cs:7 (Simulation) |
| MentalDisorderIds | Scripts/Role/MentalHealth.cs:20 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/MentalDisorderIds.cs:3 (Simulation) |
| MentalHealthSystem | Scripts/Role/MentalHealth.cs:62 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/MentalHealthSystem.cs:7 (Simulation) |
| MilitaryMovementSystem | Scripts/Map/MilitaryMovementSystem.cs:17 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/MilitaryMovementSystem.cs:9 (Simulation) |
| MinHeap | Scripts/Map/HydrologySystem.cs:42 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/SettlementInfluenceSystem.cs:18 (Simulation) ; Scripts/World/MyriadCreation.World.Hydrology/HydrologySystem.cs:10 (World) |
| MissionarySystem | Scripts/Culture/MissionarySystem.cs:14 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/MissionarySystem.cs:10 (Simulation) |
| MovementControlSystem | Scripts/Map/MovementControlSystem.cs:23 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/MovementControlSystem.cs:8 (Simulation) |
| MovementPhase | Scripts/Simulation/MyriadCreation.Simulation.Definitions/MovementPhase.cs:3 (Simulation) ; Scripts/Thought/ThoughtSystem.cs:554 (CivEvoRuntime(root)) |
| MusicPlayer | Scripts/Audio/MusicPlayer.cs:11 (CivEvoRuntime(root)) ; Scripts/Infrastructure/Audio/MusicPlayer.cs:11 (Infrastructure) |
| MusicPlayerSystem | Scripts/Audio/MusicPlayerSystem.cs:10 (CivEvoRuntime(root)) ; Scripts/Infrastructure/Audio/MusicPlayerSystem.cs:8 (Infrastructure) |
| NeedDimensionScore | Scripts/Politics/ClassNeedsSystem.cs:84 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/NeedDimensionScore.cs:8 (Simulation) |
| OceanCurrentSimulator | Scripts/Map/OceanCurrentSimulator.cs:21 (CivEvoRuntime(root)) ; Scripts/World/MyriadCreation.World.Hydrology/OceanCurrentSimulator.cs:8 (World) |
| OfficeTitleCatalog | Scripts/Politics/OfficeTitle.cs:31 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/OfficeTitleCatalog.cs:6 (Simulation) |
| OfficeTitleEntry | Scripts/Politics/OfficeTitle.cs:20 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/OfficeTitleEntry.cs:6 (Simulation) |
| OfficialOffice | Scripts/Politics/OfficeTitle.cs:8 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/OfficialOffice.cs:3 (Simulation) |
| PathfindingSystem | Scripts/Military/PathfindingSystem.cs:14 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/PathfindingSystem.cs:12 (Simulation) |
| PeaceNegotiationSystem | Scripts/Diplomacy/PeaceNegotiationSystem.cs:13 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/PeaceNegotiationSystem.cs:8 (Simulation) |
| PersonalityArchetype | Scripts/Culture/PersonalityArchetype.cs:13 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/PersonalityArchetype.cs:7 (Simulation) |
| PersonalityDimension | Scripts/Role/CharacterSystem.cs:20 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/PersonalityDimension.cs:3 (Simulation) |
| PersonalityDimensions | Scripts/Role/CharacterSystem.cs:32 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/PersonalityDimensions.cs:5 (Simulation) |
| PersonalityTrait | Scripts/Role/CharacterSystem.cs:788 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/PersonalityTrait.cs:6 (Simulation) |
| PersonalityTraitDatabase | Scripts/Role/CharacterSystem.cs:856 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/PersonalityTraitDatabase.cs:6 (Simulation) |
| PlanetClimateSimulator | Scripts/Climate/PlanetClimateSimulator.cs:14 (CivEvoRuntime(root)) ; Scripts/World/MyriadCreation.World.Climate/PlanetClimateSimulator.cs:9 (World) |
| PlanetTerrainGenerator | Scripts/Map/PlanetTerrainGenerator.cs:20 (CivEvoRuntime(root)) ; Scripts/World/MyriadCreation.World.Terrain/PlanetTerrainGenerator.cs:12 (World) |
| PoliticalAccessAnalyzer | Scripts/Politics/ClassNeedsSystem.cs:137 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/PoliticalAccessAnalyzer.cs:10 (Simulation) |
| PoliticalManager | Scripts/Politics/PoliticalSystem.cs:164 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/PoliticalManager.cs:8 (Simulation) |
| PolityComponentInnovations | Scripts/Politics/PolityComponentInnovations.cs:12 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/PolityComponentInnovations.cs:5 (Simulation) |
| PopulationBlock | Scripts/Core/MyriadCreation.Core.Data/PopulationBlock.cs:7 (Core) ; Scripts/Legacy/Core/TileData.cs:96 (CivEvoRuntime(root)) |
| PopulationStats | Scripts/Politics/CarryingCapacitySystem.cs:194 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/PopulationStats.cs:8 (Simulation) |
| PortTier | Scripts/Map/SettlementTypologyEnums.cs:113 (CivEvoRuntime(root)) ; Scripts/World/MyriadCreation.World.Settlement/PortTier.cs:3 (World) |
| PriorityQueue | Scripts/Military/PathfindingSystem.cs:364 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/PriorityQueue.cs:6 (Simulation) |
| Province | Scripts/Map/Province.cs:11 (CivEvoRuntime(root)) ; Scripts/World/MyriadCreation.World/Province.cs:8 (World) |
| ProvinceGenerator | Scripts/Map/Province.cs:47 (CivEvoRuntime(root)) ; Scripts/World/MyriadCreation.World/ProvinceGenerator.cs:8 (World) |
| PunishmentType | Scripts/Simulation/MyriadCreation.Simulation.Definitions/PunishmentType.cs:3 (Simulation) ; Scripts/Thought/ThoughtSystem.cs:421 (CivEvoRuntime(root)) |
| RaceDTO | Scripts/Legacy/Core/SaveSystem.cs:246 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/RaceDTO.cs:10 (Simulation) |
| RaceData | Scripts/Race/RaceData.cs:13 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/RaceData.cs:11 (Simulation) |
| RealmDTO | Scripts/Legacy/Core/SaveSystem.cs:421 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/RealmDTO.cs:11 (Simulation) |
| RealmData | Scripts/Politics/PoliticalSystem.cs:10 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/RealmData.cs:14 (Simulation) |
| RealmSituation | Scripts/Politics/ClassNeedsSystem.cs:41 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/RealmSituation.cs:9 (Simulation) |
| RealmSituationBuilder | Scripts/Politics/RealmSituationBuilder.cs:17 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/RealmSituationBuilder.cs:11 (Simulation) |
| RealmSociety | Scripts/Politics/SocietySystem.cs:43 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/RealmSociety.cs:9 (Simulation) |
| RegimeChangeDynamics | Scripts/Politics/RegimeChangeDynamics.cs:117 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/RegimeChangeDynamics.cs:8 (Simulation) |
| RegimeChangeState | Scripts/Politics/RegimeChangeDynamics.cs:102 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/RegimeChangeState.cs:9 (Simulation) |
| RelationshipType | Scripts/Role/CharacterSystem.cs:1329 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/RelationshipType.cs:3 (Simulation) |
| ReligionCatalog | Scripts/Culture/ReligionDef.cs:102 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/ReligionCatalog.cs:8 (Simulation) |
| ReligionDef | Scripts/Culture/ReligionDef.cs:18 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/ReligionDef.cs:8 (Simulation) |
| ReligionMapLevel | Scripts/Culture/ReligionDef.cs:317 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/ReligionMapLevel.cs:3 (Simulation) |
| ReligionNodeType | Scripts/Culture/ReligionDef.cs:325 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/ReligionNodeType.cs:3 (Simulation) |
| ReligionPanelText | Scripts/Culture/ReligionPanelText.cs:12 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Presentation/ReligionPanelText.cs:9 (Simulation) |
| SaintDef | Scripts/Culture/CanonizationSystem.cs:12 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/SaintDef.cs:6 (Simulation) |
| SaveData | Scripts/Infrastructure/Save/SaveData.cs:20 (Infrastructure) ; Scripts/Legacy/Core/SaveSystem.cs:199 (CivEvoRuntime(root)) |
| SaveSystem | Scripts/Infrastructure/Save/SaveSystem.cs:31 (Infrastructure) ; Scripts/Legacy/Core/SaveSystem.cs:20 (CivEvoRuntime(root)) |
| SchoolOfThought | Scripts/Simulation/MyriadCreation.Simulation.State/SchoolOfThought.cs:8 (Simulation) ; Scripts/Thought/ThoughtSystem.cs:14 (CivEvoRuntime(root)) |
| SeaLandGenerator | Scripts/Map/SeaLandGenerator.cs:12 (CivEvoRuntime(root)) ; Scripts/World/MyriadCreation.World.Generation/SeaLandGenerator.cs:8 (World) ; Scripts/World/MyriadCreation.World.Terrain/SeaLandGenerator.cs:8 (World) |
| Seed | Scripts/World/MyriadCreation.World.Generation/Seed.cs:3 (World) ; Scripts/World/MyriadCreation.World.Terrain/SphericalVoronoi.cs:10 (World) |
| SettlementAISystem | Scripts/Map/SettlementAISystem.cs:12 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.AI/SettlementAISystem.cs:9 (Simulation) |
| SettlementEvolutionSystem | Scripts/Map/SettlementEvolutionSystem.cs:12 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/SettlementEvolutionSystem.cs:11 (Simulation) |
| SettlementFunction | Scripts/Map/SettlementTypologyEnums.cs:29 (CivEvoRuntime(root)) ; Scripts/World/MyriadCreation.World.Settlement/SettlementFunction.cs:6 (World) |
| SettlementLevel | Scripts/Map/SettlementTypologyEnums.cs:15 (CivEvoRuntime(root)) ; Scripts/World/MyriadCreation.World.Settlement/SettlementLevel.cs:3 (World) |
| SettlementTypologySystem | Scripts/Map/SettlementTypologySystem.cs:12 (CivEvoRuntime(root)) ; Scripts/World/MyriadCreation.World.Settlement/SettlementTypologySystem.cs:9 (World) |
| SocialClassAvailability | Scripts/Economy/SocialClassAvailability.cs:14 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/SocialClassAvailability.cs:6 (Simulation) |
| SocialDifferentiation | Scripts/Politics/SocialDifferentiation.cs:26 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/SocialDifferentiation.cs:10 (Simulation) |
| SocietyManager | Scripts/Politics/SocietySystem.cs:62 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/SocietyManager.cs:11 (Simulation) |
| SpatialStructure | Scripts/Politics/GovernmentComposition.cs:170 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/SpatialStructure.cs:3 (Simulation) |
| SpecialBondType | Scripts/Diplomacy/DiplomacySystem.cs:244 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/SpecialBondType.cs:3 (Simulation) |
| SphericalMapRenderer | Scripts/Render/SphericalMapRenderer.cs:11 (CivEvoRuntime(root)) ; Scripts/Rendering/Terrain/SphericalMapRenderer.cs:9 (Rendering) |
| SphericalNoise | Scripts/Map/SphericalNoise.cs:10 (CivEvoRuntime(root)) ; Scripts/World/MyriadCreation.World.Terrain/SphericalNoise.cs:5 (World) |
| SphericalVoronoi | Scripts/Map/SphericalVoronoi.cs:18 (CivEvoRuntime(root)) ; Scripts/World/MyriadCreation.World.Terrain/SphericalVoronoi.cs:8 (World) |
| StructuralTension | Scripts/Politics/RegimeChangeDynamics.cs:48 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/StructuralTension.cs:9 (Simulation) |
| Subordination | Scripts/Diplomacy/DiplomacySystem.cs:253 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/Subordination.cs:7 (Simulation) |
| SubordinationType | Scripts/Diplomacy/DiplomacySystem.cs:231 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/SubordinationType.cs:3 (Simulation) |
| SuccessionSystem | Scripts/Politics/SuccessionSystem.cs:11 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/SuccessionSystem.cs:8 (Simulation) |
| SupremeScope | Scripts/Politics/GovernmentComposition.cs:81 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/SupremeScope.cs:3 (Simulation) |
| SupremeSuccession | Scripts/Politics/GovernmentComposition.cs:24 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/SupremeSuccession.cs:3 (Simulation) |
| SupremeSuccessionLevel | Scripts/Politics/GovernmentComposition.cs:39 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/SupremeSuccessionLevel.cs:5 (Simulation) |
| TalentDefectDef | Scripts/Race/DnaSystem.cs:93 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/TalentDefectDef.cs:7 (Simulation) |
| TaxSystem | Scripts/Economy/EconomySystem.cs:276 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/TaxSystem.cs:10 (Simulation) |
| TaxSystemDTO | Scripts/Legacy/Core/SaveSystem.cs:368 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/TaxSystemDTO.cs:10 (Simulation) |
| TerrainGenerator | Scripts/Map/TerrainGenerator.cs:10 (CivEvoRuntime(root)) ; Scripts/World/MyriadCreation.World.Terrain/TerrainGenerator.cs:6 (World) |
| TerrainPCA | Scripts/Map/TerrainPCA.cs:25 (CivEvoRuntime(root)) ; Scripts/World/MyriadCreation.World.Terrain/TerrainPCA.cs:8 (World) |
| ThoughtManager | Scripts/Simulation/MyriadCreation.Simulation.Systems/ThoughtManager.cs:9 (Simulation) ; Scripts/Thought/ThoughtSystem.cs:567 (CivEvoRuntime(root)) |
| TileData | Scripts/Core/MyriadCreation.Core.Data/TileData.cs:8 (Core) ; Scripts/Legacy/Core/TileData.cs:12 (CivEvoRuntime(root)) ; Scripts/Military/PathfindingSystem.cs:340 (CivEvoRuntime(root)) |
| TileGrid | Scripts/Core/MyriadCreation.Core.Data/TileGrid.cs:6 (Core) ; Scripts/Legacy/Core/TileData.cs:113 (CivEvoRuntime(root)) |
| TitleInheritanceMode | Scripts/Politics/InheritanceLaw.cs:55 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/TitleInheritanceMode.cs:3 (Simulation) |
| TradeCenter | Scripts/Economy/EconomySystem.cs:25 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/TradeCenter.cs:9 (Simulation) |
| TradeCenterDTO | Scripts/Legacy/Core/SaveSystem.cs:495 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/TradeCenterDTO.cs:8 (Simulation) |
| TradeRoute | Scripts/Economy/EconomySystem.cs:137 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/TradeRoute.cs:7 (Simulation) |
| TraditionDef | Scripts/Legacy/Core/ContentTypes.cs:48 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Modding/TraditionDef.cs:9 (Simulation) |
| TraitCategory | Scripts/Role/CharacterSystem.cs:839 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/TraitCategory.cs:3 (Simulation) |
| TraitTier | Scripts/Role/CharacterSystem.cs:832 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/TraitTier.cs:3 (Simulation) |
| Treaty | Scripts/Diplomacy/DiplomacySystem.cs:278 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/Treaty.cs:8 (Simulation) |
| TreatyClause | Scripts/Diplomacy/DiplomacySystem.cs:306 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/TreatyClause.cs:7 (Simulation) |
| TreatyClauseType | Scripts/Diplomacy/DiplomacySystem.cs:319 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Definitions/TreatyClauseType.cs:3 (Simulation) |
| TrialResult | Scripts/Simulation/MyriadCreation.Simulation.State/TrialResult.cs:7 (Simulation) ; Scripts/Thought/ThoughtSystem.cs:474 (CivEvoRuntime(root)) |
| UIManager | UI/Common/UIManager.Feedback.cs:34 (UI) ; UI/Common/UIManager.MapMode.cs:34 (UI) ; UI/Common/UIManager.Panels.cs:37 (UI) ; UI/Common/UIManager.cs:37 (UI) |
| UnitCategory | Scripts/Core/MyriadCreation.Core.Enums/GameEnums.cs:288 (Core) ; Scripts/Simulation/MyriadCreation.Simulation.Units/UnitCategory.cs:3 (Simulation) |
| UnitDef | Scripts/Simulation/MyriadCreation.Simulation.Definitions/UnitDef.cs:8 (Simulation) ; Scripts/War/WarSystem.cs:11 (CivEvoRuntime(root)) |
| UpgradePath | Scripts/Map/SettlementTypologyEnums.cs:141 (CivEvoRuntime(root)) ; Scripts/World/MyriadCreation.World.Settlement/UpgradePath.cs:3 (World) |
| VerdictType | Scripts/Simulation/MyriadCreation.Simulation.Definitions/VerdictType.cs:3 (Simulation) ; Scripts/Thought/ThoughtSystem.cs:434 (CivEvoRuntime(root)) |
| WallLevel | Scripts/Map/SettlementTypologyEnums.cs:168 (CivEvoRuntime(root)) ; Scripts/World/MyriadCreation.World.Settlement/WallLevel.cs:3 (World) |
| WarGoal | Scripts/Diplomacy/WarJustificationSystem.cs:40 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/WarGoal.cs:9 (Simulation) |
| WarJustificationSystem | Scripts/Diplomacy/WarJustificationSystem.cs:95 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.Systems/WarJustificationSystem.cs:9 (Simulation) |
| WarRules | Scripts/Diplomacy/WarRules.cs:12 (CivEvoRuntime(root)) ; Scripts/Simulation/MyriadCreation.Simulation.State/WarRules.cs:6 (Simulation) |
| WarState | Scripts/Simulation/MyriadCreation.Simulation.State/WarState.cs:3 (Simulation) ; Scripts/War/WarSystem.cs:451 (CivEvoRuntime(root)) |
| WorldConfig | Scripts/Core/MyriadCreation.Core.Data/WorldConfig.cs:7 (Core) ; Scripts/Legacy/Core/WorldConfig.cs:13 (CivEvoRuntime(root)) |
