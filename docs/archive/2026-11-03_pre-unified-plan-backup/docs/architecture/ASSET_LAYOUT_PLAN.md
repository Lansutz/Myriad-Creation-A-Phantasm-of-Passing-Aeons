# 执行状态（Round 39 实际落地；以此为准）

> 盘点报告写于迁移进行中；实际执行按"更彻底但保持代码/加载路径有效"完成，偏差如下：
> - **P0 完成**：新增 `Assets/ASSET_README.md`（顶层职责/Resources/字体/音频/TMP 规则）。
> - **P1 完成**：字体归 `Assets/Art/Fonts/`；唯一 simhei 副本在 `Art/Fonts/Resources/simhei.ttf`，`Main.unity` 字体 GUID 已改指该副本，重复 ttf 已删；4 个 SourceHan 归档在 `Art/Fonts/`（0 引用，暂不删）。
> - **P2 完成**：`Assets/TextMesh Pro` → `Assets/Art/TextMesh Pro`（视觉/字体/Shader 属美术）；`TmpSettingsCreator` 硬编码路径已改 `Assets/Art/TextMesh Pro/Resources`；TMP Settings 仍按 `Resources.Load("TMP Settings")` 解析。
> - **P3 完成**：UI 音效 `Assets/Audio/Resources/UI/*`（`UiSfx` 路径改为 `UI/<name>`）；新增 `Assets/Audio/Resources/Music/README.md`；删除死代码 `MusicPlayer.cs`（StreamingAssets 音乐旧路径，0 引用）并清理 SettingsService 对它的引用；`Assets/Resources` 根已删除。
> - **P4 完成**：删除未引用 `Scenes/New Sprite Atlas.spriteatlas`、空占位 `Prefabs/`、`Settings/`、`ScriptableObjects/`（内容迁 Data）、`Editor/MapEditing/`、`StreamingAssets/Music/`、`Resources/Audio`、空目录 `.gitkeep`；`Art` 四个美术分类保留占位。
> - **P5 完成**：`DefaultWorldConfig.asset` → `Assets/Data/WorldConfig/`（场景 GUID 引用不变）。
> - **P6 部分**：`Editor/` 三套工具目录保留（属合法 Editor 归属）；`UI/` 保留在 Assets 根（UI 程序集职责，用户原则"UI 管所有 UI 定义"）；`StreamingAssets/Base|Mods` 目录名被 8+ 硬编码路径依赖，本批不改。
> - **P7 完成**：顶层最终为 `Art/Audio/Data/Editor/Scenes/Scripts/StreamingAssets/Tests/UI` + `csc.rsp`。
>
> 验证：编译 COMPILE_OK；EditMode 568/568；PlayMode 9/9（无图形/有图形）。
> - **P8 完成（Round 40）**：`Editor/` 重排为 `Tools/`（BatchCompileCheck/AutoOpenMainScene/GraphicsApiFix/TerrainPainter）、`Content/`（ContentPackageExporter）、`TMP/`（TmpSettingsCreator），并加 `Editor/README.md`；`Art/Fonts` 保留 4 个 SourceHan（用户决定保留），新增 `Art/Fonts/README.md`；`Audio/Resources` 为有意保留的运行时加载根（UI/Music），旧 `Resources/Audio`、`Audio/Music|SFX`、空目录均已清理。

# Assets 全盘资产盘点与目录整改方案

> 类型：只读盘点 + 分步整改方案（task-121）。**不移动/删除/修改任何资产或代码**，仅写本文档。
> 盘点时间：2026-11-02 10:4x（+08:00）；范围：`MyriadCreation/Assets/**`（含 .meta、StreamingAssets、csc.rsp）。
> 注意：盘点期间 worktree 已发生**并行资产迁移**（`Fonts/`、`Resources/Fonts`、`TextMesh Pro/`、`Resources/Audio`、`DefaultWorldConfig` 已分别迁入 `Art/Fonts`、`Art/TextMesh Pro`、`Audio/Resources/Audio`、`Data/WorldConfig`）。本文以**当前快照**为起点，P0 先冻结/核对，P1~P7 给出收尾与最终形态；若与并行任务冲突，以 P0 冻结点为基线重放。

---

## 1. 当前资产清单

### 1.1 顶层目录（当前快照）

| 顶层 | 非 .meta 文件数 | 类型分布 | 职责判断 |
|---|---:|---|---|
| `Art/` | 47 | shader 19、shadergraph 4、cginc 4、hlsl 1、otf 4、ttf 2、mat 2、asset 4、txt 3、占位 4 | **混入第三方 TMP 包**（`Art/TextMesh Pro/**` 共 37 文件中绝大部分），项目字体/着色器正确 |
| `Audio/` | 8 | wav 5、README 1、占位 2 | UI 音效在 `Audio/Resources/Audio/UI/`、路径正确；Music/SFX 仅占位 |
| `Data/` | 2 | asset 1、占位 1 | `WorldConfig/DefaultWorldConfig.asset` 正确；根 `.gitkeep` 冗余 |
| `Editor/` | 9 | cs 7、asmdef 1、占位 1 | 编辑器工具齐全；子目录命名重复/散乱 |
| `Scenes/` | 1 | Main.unity（34MB） | 唯一场景，正确 |
| `Scripts/` | 831 | cs 811、asmdef 11、md 9 | 运行时代码，正确 |
| `StreamingAssets/` | 40 | json 30、csv 8、md 1、占位 1 | 内容 JSON/CSV，**Unity 特殊目录必须保留根名** |
| `Tests/` | 106 | cs 104、asmdef 2 | EditMode/PlayMode 测试，正确（保持顶层） |
| `UI/` | 29 | cs 28、asmdef 1 | **纯代码**（无 UI 资产），不属于顶层资产域 |
| `csc.rsp` | 1 | rsp | Unity 项目根编译选项，**位置正确，不能移** |

> 当前已无顶层 `Fonts/`、`TextMesh Pro/`、`Resources/`、`ScriptableObjects/`、`Settings/`、`Prefabs/`（并行迁移已移除）。

### 1.2 关键资产现状（当前路径）

**Art/Fonts（项目字体，符合“字体归美术”）——当前快照（并行迁移已完成 simhei 收敛）**
- `Art/Fonts/Resources/Fonts/simhei.ttf`（9.3MB，GUID `b9afd0d9f07d44f4cbea8ead24323b18`）：唯一运行时中文字体；被 `Resources.Load<Font>("Fonts/simhei")` 使用，**Main.unity 2 处已指向该 GUID**（`m_SourceFontFileGUID`/`m_SourceFontFile`）。
- 旧 `Art/Fonts/simhei.ttf`（GUID `ac153...`）已由并行迁移删除，场景已改 `b9afd...`（P1 验收项，见 §2 A2）。
- `Art/Fonts/SourceHanSansSC-Medium.otf` / `-Regular.otf`、`SourceHanSerifSC-Regular.otf` / `-SemiBold.otf`（共 ~82.2MB，**代码/资产/场景 0 引用**）
- `Art/Fonts/Resources/` 是 Unity `Resources.Load("Fonts/simhei")` 的路径承载（嵌套 Resources 合法，不能随便改名）。

**Art/TextMesh Pro（第三方 TMP Essential Resources）**
- `Art/TextMesh Pro/Fonts/LiberationSans.ttf`、`Fonts/LiberationSans - OFL.txt`
- `Art/TextMesh Pro/Resources/TMP Settings.asset`（GUID `6db60c1a73366444a8e66dcf4eb62f45`；TMP 包按 `Resources.Load("TMP Settings")` 加载，**无项目代码/场景 GUID 引用**）
- `Art/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset`、`- Fallback.asset`、`- Outline.mat`、`- Drop Shadow.mat`
- `Art/TextMesh Pro/Resources/LineBreaking Leading/Following Characters.txt`、`Style Sheets/Default Style Sheet.asset`
- `Art/TextMesh Pro/Shaders/**`（19 shader + 4 shadergraph + cginc/hlsl）
- 共 37 文件，属**第三方 vendor 包**，不是项目美术资产。

**Audio**
- `Audio/Resources/Audio/UI/{click,hover,open,close,error}.wav`（UiSfx 用 `Resources.Load("Audio/UI/*")`，路径正确）
- `Audio/README.md`（已写明放置规则）
- `Audio/Music/.gitkeep`、`Audio/SFX/.gitkeep`（仅占位；**当前没有 `Audio/Resources/Music`/`SFX`**）
- 代码现状：`MusicPlayerSystem.cs:69` 用 `Resources.LoadAll<AudioClip>("Music")`；`MusicPlayer.cs:35/96` 读 `StreamingAssets/Music`——两者目前都无资源目录。

**Data**
- `Data/WorldConfig/DefaultWorldConfig.asset`（GUID `cf2df798f85bf194f9d47e6c0fb7da42`，**Main.unity:5027 `config:` 直接引用**）
- `Data/.gitkeep`（冗余）

**Editor**
- `Editor/MyriadCreation.Editor.asmdef`
- `Editor/ContentAuthoring/ContentPackageExporter.cs`
- `Editor/MyriadCreation.Editor/BatchCompileCheck.cs`、`TmpSettingsCreator.cs`
- `Editor/MyriadCreation.EditorTools/AutoOpenMainScene.cs`、`GraphicsApiFix.cs`
- `Editor/TerrainPainter/BrushMode.cs`、`BrushShape.cs`
- `Editor/.gitkeep`

**Scenes**
- `Scenes/Main.unity`（34MB，唯一场景，EditorBuildSettings 只引用它；无 SpriteAtlas 残留，原 `New Sprite Atlas.spriteatlas` 已不存在）

**StreamingAssets（Unity 特殊目录，根位置不可变）**
- `Base/`（Race/Culture/Religion/Laws/Innovation/Goods/Localization/... JSON/CSV，30 JSON + 8 CSV）
- `Mods/Dna/DnaDefs.json`
- `Base/_generated/README.md`、`language-families.json`
- 代码路径硬编码 `StreamingAssets/Base/...`、`StreamingAssets/Mods/...`（见 §3）。

**UI（当前仅代码）**
- 28 个 .cs + `MyriadCreation.UI.asmdef`；`Common/TMPFontUtility.cs`、`Common/UiSfx.cs` 是资产路径消费点。

**Scripts / Tests**：`Scripts/` 831（811 cs / 11 asmdef / 9 md）；`Tests/` 106（104 cs / 2 asmdef，EditMode + PlayMode）。

### 1.3 空目录 / 占位文件（当前）

`.gitkeep` 共 9 个：
`Art/Animations`、`Art/Materials`、`Art/Models`、`Art/Textures`、`Audio/Music`、`Audio/SFX`、`Data`、`Editor`、`StreamingAssets`。
（`Art/Fonts/Resources`、`Audio/Resources` 为承载路径的中间目录，保留；Unity 会忽略空目录，保留 `.gitkeep` 只为占位。）

---

## 2. 放错位置 / 待整改项清单

> 计数口径：**8 类待整改**（A2 已由并行迁移收敛，列入 P1 验收）；涉及约 **86 个文件**（含 37 个 TMP vendor 文件与 29 个 UI 代码文件）；其中文件级 5 项、目录/组织级 3 项。

| # | 当前路径 | 问题 | 建议路径 | 涉及文件 |
|---:|---|---|---|---|
| A1 | `Art/TextMesh Pro/**` | 第三方 TMP 包放在 Art（Art 只应放项目图片/材质/纹理/效果/字体） | `Assets/ThirdParty/TextMesh Pro/**`（若坚持 vendor 例外则保留并 README 声明） | 37 |
| A2 | ~~`Art/Fonts/simhei.ttf` 与 `Art/Fonts/Resources/Fonts/simhei.ttf` 重复~~ | **已由并行迁移收敛**：top 副本删除、Main.unity 2 处 GUID 已改 `b9afd...`；仅需 P1 验收 | 保留 `Art/Fonts/Resources/Fonts/simhei.ttf` 唯一副本（当前即此状态） | 0（验收） |
| A3 | `Art/Fonts/SourceHan*.otf`（4 个） | 约 82MB，代码/资产/场景 0 引用（死资源/未用源字体） | `Art/Fonts/Source/`（归档，保留可选）或确认后删除 | 4 |
| A4 | `Art/Shaders/CyowariHistoricalMap.shader` | 0 GUID 引用（无 material/场景使用） | 确认未用后 `Art/Shaders/` 删除；若保留需在 README 标注用途 | 1 |
| B1 | `Editor/MyriadCreation.Editor/`、`Editor/MyriadCreation.EditorTools/` | 子目录名与程序集名重复、命名不表职责 | `Editor/Build/`（BatchCompileCheck）、`Editor/TMP/`（TmpSettingsCreator）、`Editor/Tools/`（AutoOpenMainScene、GraphicsApiFix）；`ContentAuthoring`、`TerrainPainter` 保持 | 5 |
| B2 | `UI/**`（29 文件） | 顶层 UI 是纯代码，不是 UI 资产域 | `Assets/Scripts/UI/**`（asmdef 随迁，assembly name 不变）；或保留并在 README 声明为“UI 代码域” | 29 |
| C1 | `Audio/Music`、`Audio/SFX`（仅 `.gitkeep`） | 有目录无资源；两套音乐加载路径（Resources/Music 与 StreamingAssets/Music）都没有实际目录 | 明确单一策略：推荐 `Audio/Resources/Music/README.md` + `Audio/Resources/SFX/README.md`；`MusicPlayer` 走 StreamingAssets 的旧注释需统一/标注废弃 | 2 目录 |
| C2 | `Data/.gitkeep`、`Editor/.gitkeep`、`StreamingAssets/.gitkeep`、`Art/{Animations,Materials,Models,Textures}/.gitkeep` | 空占位，顶层“该删的删” | 删除 `.gitkeep` 与空目录（保留 `Art/Fonts/Resources`、`Audio/Resources` 等路径承载） | 9 |
| D1 | `Editor/MyriadCreation.Editor/TmpSettingsCreator.cs:21` | 硬编码旧路径 `Assets/TextMesh Pro/Resources`，TMP 已迁 `Art/TextMesh Pro` → 菜单生成 TMP Settings 会创建失败/错位 | 改成 `Assets/Art/TextMesh Pro/Resources`（若 P2 再迁 ThirdParty，同步改） | 1 |

**结论：真正的“放错位置”主要是 A1（TMP vendor 放进 Art）、B2（UI 代码顶层）、B1（Editor 子目录组织）；A2 已由并行迁移完成（列入 P1 验收）、A3/A4 是死资源清理，C1/C2 是空目录/音频路径缺口，D1 是迁移后的代码残留。**

---

## 3. 引用方式与风险（按资产）

### 3.1 字体 / TMP

| 资产 | 引用方式 | 影响 |
|---|---|---|
| `Art/Fonts/Resources/Fonts/simhei.ttf` | `TMPFontUtility.cs:25`、`GovernmentPanel.cs:60` 的 `Resources.Load<Font>("Fonts/simhei")` | **路径依赖嵌套 Resources 名**；不能改 `Resources` 目录名/层级，否则代码同步改 |
| `Art/Fonts/simhei.ttf`（旧 top 副本，已删） | 曾由 Main.unity `m_SourceFontFileGUID`/`m_SourceFontFile` 引用（旧 GUID `ac153...`）；当前场景已改 `b9afd...` 指向嵌套副本 | 已完成；P1 只做验收（若回退旧基线再按此表改回） |
| `Art/TextMesh Pro/Resources/TMP Settings.asset` | TMP 包内部 `Resources.Load("TMP Settings")`（无项目代码/场景 GUID 引用） | **任意嵌套 Resources 均可**；但 `TmpSettingsCreator` 硬编码旧路径（D1） |
| `Art/TextMesh Pro/Resources/Fonts & Materials/*` | TMP 按资源名/默认设置加载，GUID 引用 0 | 随 TMP Settings/包移动时保持 `Resources/...` 相对路径不变 |
| `Art/Fonts/SourceHan*.otf` | 0 引用 | 删除/归档不影响编译运行（需确认非手工替换目标） |

### 3.2 音频

| 资产/入口 | 引用方式 | 影响 |
|---|---|---|
| `Audio/Resources/Audio/UI/*.wav` | `UiSfx.cs:23/87`：`Resources.Load<AudioClip>("Audio/UI/"+name)` | 目录名/层级不能随意改；移到 `Audio/` 下并保留 `Resources/Audio/UI` 路径即可 |
| `MusicPlayerSystem.cs:69` | `Resources.LoadAll<AudioClip>("Music")` | 当前无 `Resources/Music`；需建 `Audio/Resources/Music`（或改代码路径） |
| `MusicPlayer.cs:35/96` | `StreamingAssets/Music` | 当前 `StreamingAssets/Music` 不存在；若走 StreamingAssets 需建目录 + README |
| `Audio/README.md` | 文档规则 | 已要求音频在 `Audio/`、UI 音效路径不改；与代码现状一致 |

### 3.3 Data / Scene / StreamingAssets

| 资产 | 引用方式 | 影响 |
|---|---|---|
| `Data/WorldConfig/DefaultWorldConfig.asset` | Main.unity:5027 `config:` GUID `cf2df...` | 移动需同步改场景（P5 只做归位/README，不再移动） |
| `Scenes/Main.unity` | EditorBuildSettings 唯一场景 + 场景内 GUID 引用 | **34MB**，任何场景编辑需最小化 diff；移动资产时优先改 GUID 而非重存场景 |
| `StreamingAssets/Base/...` | `ContentRegistry.cs:93-105`、`ContentRuntime`、`Localization.cs:39-40`、`CultureStageCatalog.cs`、`LawContentLoader.cs` 等硬编码 `Base`/`Mods` 子路径 | **根目录必须叫 StreamingAssets**；`Base/Mods` 改名需同步 8+ 文件（P6 可选，不建议本批） |
| `csc.rsp` | Unity 项目根自动识别 | 正确位置，保留；不放 Assets 子目录 |

---

## 4. 目标顶层目录树（推荐）

```
Assets/
  Art/
    Fonts/
      Resources/Fonts/simhei.ttf        # 唯一运行时中文字体（Resources.Load 路径）
      Source/                            # SourceHan 源字体（无运行时引用，可选归档）
      README.md                          # 字体命名/来源/引用规则
    Shaders/CyowariHistoricalMap.shader  # 项目着色器（effect，归 Art）
    Materials/  Models/  Textures/  Animations/   # 仅在有真实资产时保留（无资产则删空目录）
  Audio/
    README.md
    Resources/Audio/UI/*.wav             # UiSfx 固定路径
    Resources/Music/README.md            # 若走 Resources 音乐
    Resources/SFX/README.md
  Data/
    WorldConfig/DefaultWorldConfig.asset # 场景直引
    README.md
  Editor/
    MyriadCreation.Editor.asmdef
    Build/BatchCompileCheck.cs
    TMP/TmpSettingsCreator.cs
    Tools/AutoOpenMainScene.cs
    Tools/GraphicsApiFix.cs
    ContentAuthoring/ContentPackageExporter.cs
    TerrainPainter/{BrushMode,BrushShape}.cs
  Scenes/Main.unity
  Scripts/**                             # 运行时代码（含 Scripts/UI/ 若执行 B2）
  StreamingAssets/
    Base/**  Mods/**                     # Unity 特殊目录，根名不可改
  Tests/EditMode, Tests/PlayMode
  ThirdParty/TextMesh Pro/**             # TMP vendor（A1 目标；若保留 vendor 例外则留在 Art/TextMesh Pro）
  csc.rsp                                # Unity 根编译选项，保留
```

**理由**：
- `Art` 只放项目美术资产（图片/材质/纹理/效果/字体）；第三方 TMP 移 `ThirdParty`；
- `Audio` 放音频及其 Resources 包装；`Data` 放场景/代码直引的 ScriptableObject 数据；
- `Editor` 保留 Unity 特殊目录，按职责分 `Build/TMP/Tools/...`；
- `Scenes`、`StreamingAssets`、`Tests` 是 Unity/工程约定的特殊目录，保持顶层；
- `Scripts` 收所有运行时代码（UI 代码迁入 `Scripts/UI`）；
- `csc.rsp` 必须留 Assets 根；顶层不再出现 `Fonts/TextMesh Pro/Resources/ScriptableObjects/Settings/Prefabs`。

---

## 5. 详细分步计划（P0~P7）

> 通用规则：所有 `.cs/.asset/.ttf/.wav/.mat/.unity/.asmdef` 移动必须 **`.cs/.asset + .meta` 成对移动并保留 GUID**；改场景只改 GUID 行（`sed -i` 精确替换），不重存场景；每步单独提交/回滚。验证由 Lead 统一跑 Unity 编译 + EditMode/PlayMode；本卡不执行。

### P0 — 冻结/快照/引用清单（0 移动，先做）

- **动作**：
  1. 冻结 Assets 移动（并行任务完成后再开始 P1），记录当前 `git status` 与所有 `.meta` GUID；
  2. 生成引用清单：字体/音频/TMP/世界配置的 GUID 与 `Resources.Load` 路径、场景引用行（本文 §3 已给）；
  3. 加 `Assets/ASSET_README.md`（规则：Art/Audio/Data/Editor/Scenes/Scripts/StreamingAssets/Tests/ThirdParty；字体/音频/TMP vendor 归属；禁止顶层 Resources/Fonts/TextMesh Pro）。
- **验证**：现有编译/测试全绿（不改任何资产）；引用清单与本文一致。
- **回滚**：仅删除新增 README。
- **预计**：0 移动，0.5 人日。

### P1 — 字体归 Art（收敛验收 + 死字体归档）

- **当前状态（并行迁移已完成）**：`Art/Fonts/simhei.ttf` 已删除，场景 2 处 GUID 已由 `ac153...` 改为 `b9afd...`；`Art/Fonts/Resources/Fonts/simhei.ttf` 是唯一运行时副本。本步只需**验收**，不再重复移动。
- **仍待动作（逐文件）**：
  1. 验证：`grep ac153... Scenes/Main.unity` = 0、`grep b9afd...` = 2；`Resources.Load("Fonts/simhei")` 命中唯一副本；
  2. 移动 `Art/Fonts/SourceHanSansSC-Medium.otf`、`SourceHanSansSC-Regular.otf`、`SourceHanSerifSC-Regular.otf`、`SourceHanSerifSC-SemiBold.otf`（+meta）→ `Art/Fonts/Source/`（0 引用，GUID 不变）；
  3. 新增 `Art/Fonts/README.md`：说明 simhei 运行时副本/场景源、SourceHan 归档、TMP 中文动态 SDF 入口。
- **引用更新**：无需再改场景（已收敛）；不改代码（`Resources.Load("Fonts/simhei")` 路径不变）。
- **验证**：Unity 编译；`TMPFontUtility.ApplyChineseFontToAll` 中文 UI 正常（PlayMode MapVisibility/SettlementIcon/GovernmentPanel）；EditMode `UiSfxTests`/`SfxPlayerTests`（不受影响）；`grep ac153...` 场景=0。
- **回滚**：恢复字体文件+meta 与 Main.unity 2 行 GUID（若并行迁移被回退）。
- **风险**：场景现有 `b9afd...` 指向嵌套副本，P1 不再改场景；SourceHan 归档 0 引用、无运行时风险。

### P2 — TMP vendor 归 ThirdParty（或保留 vendor 例外）

- **移动清单（推荐 A1）**：
  - `Art/TextMesh Pro/` 整目录（37 文件 + 子目录 meta）→ `Assets/ThirdParty/TextMesh Pro/`；**保留内部 `Resources/` 层级名**，`Resources.Load("TMP Settings")` 与 `Resources.Load("Fonts & Materials/...")` 继续有效；
  - 更新 `Editor/MyriadCreation.Editor/TmpSettingsCreator.cs:21`：
    `string dir = "Assets/TextMesh Pro/Resources";` → `"Assets/ThirdParty/TextMesh Pro/Resources";`（并改 `CreateFolder` 父路径）；
  - 若保留当前 `Art/TextMesh Pro`（vendor 例外），只做 D1 路径修正 + `Assets/ASSET_README` 声明。
- **引用更新**：项目代码无 TMP 资产 GUID 引用（0）；TMP 包按名字加载；`TmpSettingsCreator` 路径必须同步。
- **验证**：编译；Editor 菜单生成 TMP Settings 到新路径不报错；PlayMode UI 文本（TMP）正常；`Art/TextMesh Pro` 下无残留（若移动）。
- **回滚**：整目录移回 `Art/TextMesh Pro`，恢复 `TmpSettingsCreator` 路径。
- **风险**：TMP 包升级/重导可能按默认 `Assets/TextMesh Pro` 路径覆盖/重建；移动后升级需注意迁移；`TMP Settings` 缺失会导致 `TMPFontUtility` 中文动态 SDF NRE 防御路径。
- **备选**：保留 `Art/TextMesh Pro`（当前并行迁移结果，零再移动），仅修正 D1；此方案把 TMP 作为 Art 的 vendor 例外，README 说明。

### P3 — 音频归 Audio + Resources 包装（Music/SFX 定策略）

- **动作**：
  1. `Audio/Resources/Audio/UI/*.wav` 保持（UiSfx 路径 `Audio/UI/*` 不变）；
  2. 新增 `Audio/Resources/Music/README.md`、`Audio/Resources/SFX/README.md`（声明：运行时音乐/音效放这里；`MusicPlayerSystem` 用 `Resources.LoadAll("Music"|"SFX")`）；
  3. 二选一明确 `MusicPlayer.cs` 的 `StreamingAssets/Music` 路线：若保留，新增 `StreamingAssets/Music/README.md` 并注明“外部用户曲目”；否则在 `MusicPlayer` 注释/README 标记废弃，统一用 `Audio/Resources/Music`；
  4. 删除 `Audio/Music/.gitkeep`、`Audio/SFX/.gitkeep`（由 Resources 包装替代）或保留为“无资源占位”README；
  5. 更新 `Audio/README.md` 与 `MusicPlayerSystem` 注释中的旧 `Resources/Music` 描述，避免两处路径漂移。
- **引用更新**：`UiSfx` 路径不变；`MusicPlayerSystem`/`MusicPlayer` 如改路径需同步代码；当前无音乐素材，改动以文档/目录为主。
- **验证**：EditMode `UiSfxTests`/`SfxPlayerTests`；PlayMode SettingsPanel（音乐面板）不报空目录错误。
- **回滚**：删除新增 README/恢复 `.gitkeep`/恢复注释。
- **风险**：Resources 下大音频会进包；大曲目建议走 StreamingAssets（保留 MusicPlayer）而非 Resources；两套播放器并存需产品确认。

### P4 — 死资源 / 空目录清理

- **删除清单**：
  1. `.gitkeep`：`Art/Animations`、`Art/Materials`、`Art/Models`、`Art/Textures`、`Audio/Music`、`Audio/SFX`（P3 后）、`Data`、`Editor`、`StreamingAssets`；
  2. 空目录：上述 `.gitkeep` 所在目录（除非 P3 保留 Audio/Music 占位）；`Art/TextMesh Pro` 若 P2 已迁则空目录不保留；`Art/Fonts/Resources`、`Audio/Resources` 保留（路径承载）；
  3. `Art/Shaders/CyowariHistoricalMap.shader`（+meta）：0 GUID 引用，确认未用后删除/移到 `Art/Shaders/Archive/`（建议先归档一版）；
  4. `Art/Fonts/Source/*` 若确认非未来资产：删除（P1 已归档，可再评估）。
- **验证**：`find Assets -name '*.meta'` 无孤儿 meta；场景/资产无丢失引用（Unity 打开无 Missing）；`Art` 只剩真实资产；编译/EditMode 绿。
- **回滚**：从 git 恢复删除项（纯删除，git revert 即可）。
- **风险**：一键删空目录时误删 `Resources` 路径承载；**只删 `.gitkeep` 与真正空目录，保留 `Resources` 链**。

### P5 — Data / ScriptableObjects 合并

- **现状**：`Data/WorldConfig/DefaultWorldConfig.asset` 已是目标位置；`ScriptableObjects/` 顶层已不存在；`Data/.gitkeep` 冗余。
- **动作**：
  1. 删除 `Data/.gitkeep`，新增 `Data/README.md`（规则：Data 只放场景/代码直引数据；世界配置在 WorldConfig）；
  2. 若未来新增 ScriptableObject，统一 `Data/<Domain>/`（禁止复活顶层 ScriptableObjects）；
  3. 核对 `Main.unity:5027` 的 `cf2df...` 仍指向 asset（GUID 不变即可）。
- **验证**：场景加载世界配置正常；无 Missing；编译/PlayMode 主菜单开局。
- **回滚**：恢复 `.gitkeep`/README 删除。
- **风险**：场景引用是 GUID，移动/改名需保持 meta；本步不移动 asset。

### P6 — Editor / Tests / StreamingAssets / UI 归类

- **P6a Editor**（B1）：
  - 新建 `Editor/Build/`、`Editor/TMP/`、`Editor/Tools/`；
  - `Editor/MyriadCreation.Editor/BatchCompileCheck.cs` → `Editor/Build/BatchCompileCheck.cs`；
  - `Editor/MyriadCreation.Editor/TmpSettingsCreator.cs` → `Editor/TMP/TmpSettingsCreator.cs`；
  - `Editor/MyriadCreation.EditorTools/AutoOpenMainScene.cs`、`GraphicsApiFix.cs` → `Editor/Tools/`；
  - `ContentAuthoring/`、`TerrainPainter/` 保持；`Editor/MyriadCreation.Editor.asmdef` 保持 `Editor/` 根（文件名/路径变化不影响 asmdef 归属）；删 `Editor/.gitkeep`；
  - 更新 `TmpSettingsCreator` 路径（P2 同步）。
- **P6b UI**（B2，可选）：`UI/` 全目录（29 文件 + asmdef + meta）→ `Assets/Scripts/UI/`；assembly name 不变，引用按名解析；同步更新 README/文档中的旧路径（`UI/Common/...`）；验证全量 PlayMode UI。
- **P6c Tests**：保持 `Assets/Tests/{EditMode,PlayMode}`（Unity 常规）；只清 `.gitkeep`（当前无）；不改 asmdef。
- **P6d StreamingAssets**：保持根名与 `Base/Mods`（代码硬编码 8+ 文件）；若未来改名 `Content/`，单独一步同步 `ContentRegistry/ContentRuntime/Localization/CultureStageCatalog/LawContentLoader/...`，**本批不做**；保留 `csc.rsp`。
- **验证**：编译（Editor asmdef 路径不变）；Editor 菜单 `TmpSettingsCreator` 可执行；全量 EditMode/PlayMode（重点 UI/TMP/音频）；StreamingAssets 读取本地化/法律/文化正常。
- **回滚**：按子步骤（P6a/P6b 各自文件+meta 回移）恢复。
- **风险**：UI 代码移动涉及 29 个 meta/GUID 与文档路径，测试无路径依赖（按 asmdef name），但 `Tests` 下可能引用文件路径字符串（本仓测试多用类型名）；Editor 移动不影响 asmdef；StreamingAssets 改名风险高，建议永不做。

### P7 — 顶层最终形态 + 门禁

- **目标**：§4 目录树；顶层只允许：`Art/ Audio/ Data/ Editor/ Scenes/ Scripts/ StreamingAssets/ Tests/ ThirdParty/ csc.rsp`（若 UI 不迁则加 `UI/`）。
- **动作**：
  1. 新增/完善 `Assets/ASSET_README.md`（命名/归属/Resources 规则/禁止项）；
  2. 加轻量门禁（可选，EditMode 测试或 Editor 校验）：顶层禁止 `Fonts/TextMesh Pro/Resources/ScriptableObjects/Settings/Prefabs`；`Art` 下禁止 `.wav/.mp3`，`Audio` 下禁止 `.png/.ttf`；`.gitkeep` 只允许在真实占位目录；所有非 meta 资产有 `.meta`；
  3. 全量回归：Unity 编译 + EditMode/PlayMode + 主菜单/存档/地图/UI 冒烟。
- **验证**：门禁测试绿；资源无 Missing；`git status` 只包含计划内移动。
- **回滚**：门禁/README 单步 revert；资产移动按 P1~P6 各自回滚点。
- **风险**：门禁过严会阻正常新资产；建议先 warn 后 error。

---

## 6. 风险总表

| 风险 | 影响 | 缓解 |
|---|---|---|
| 场景 GUID 改错（simhei/DefaultWorldConfig） | TMP 中文字体源/世界配置丢失，PlayMode UI 回退 | 只改精确 GUID 行；改后 `grep` 校验；保留备份/分支 |
| `Resources.Load` 路径改名 | 字体/UI 音效静默缺失 | 保留嵌套 `Resources` 相对路径（`Art/Fonts/Resources/Fonts`、`Audio/Resources/Audio/UI`） |
| TMP 包路径迁移 | TMP Settings 找不到 → `TMPFontUtility` NRE/中文异常；包升级覆盖 | P2 二选一；保留 `Resources/TMP Settings.asset` 相对路径；`TmpSettingsCreator` 同步路径 |
| 并行资产迁移冲突 | 本盘点/方案基于移动中途快照 | P0 冻结；以最终 `git status` 重放；不覆盖他人改动 |
| 34MB Main.unity 重存 | 巨大 diff/误改 | 只文本替换 GUID 行，不重存场景 |
| 删除“看起来未引用”资产 | 运行时/编辑器动态加载资产可能被误删 | 0 GUID 引用只代表静态引用；SourceHan/Shader 先归档一版，PlayMode UI 全量验证后再删 |
| StreamingAssets 改名 | ContentRegistry/Localization/Law/Culture 8+ 硬编码路径 | 本批不改 `Base/Mods`；如需改单独一步全仓同步 |
| UI 代码移动 | 文档路径/测试字符串引用 | asmdef name 不变；grep 同步文档；全量测试 |

---

## 7. 最小首步 3 项（建议先做）

1. **P0 冻结/快照**：记录当前资产与 GUID 引用清单（本文 §3），加 `Assets/ASSET_README.md`，0 移动（0 风险）。
2. **P1 字体收敛**：删除重复 `Art/Fonts/simhei.ttf` + 改 Main.unity 2 处 GUID → `b9afd...`（或保留副本），SourceHan 归 `Art/Fonts/Source/`；验证中文 UI/TMP。
3. **P2 TMP vendor 定案**：移动 `Art/TextMesh Pro` → `ThirdParty/TextMesh Pro`（或保留 vendor 例外），同步修 `TmpSettingsCreator.cs:21` 硬编码路径；验证 TMP Settings 生成 + UI 文本。

> 建议 P0 先合入（只读/README/冻结），再按 P1→P3→P4→P5→P6→P7 小步提交；每步只做一类事情，避免一次大移动污染 34MB 场景与 100+ GUID。

---

## 8. 盘点口径与限制

- 文件清单来自 `find Assets -type f ! -name '*.meta'`；GUID 引用扫描覆盖 `.unity/.asset/.prefab/.mat/.spriteatlas/.json/.cs/.asmdef/.shader/.shadergraph/.txt` 文本内容（GUID 字符串）。
- 未运行 Unity/编译/测试；`Resources.Load` 路径按 Unity 规则静态推断（任意嵌套 `Resources` 目录 + 相对子路径），最终以 Lead 编译/PlayMode 为准。
- 盘点期间 worktree 有并行资产迁移（见开头），因此“当前路径”可能继续变化；P0 冻结后以最新 `git status` 为准。
