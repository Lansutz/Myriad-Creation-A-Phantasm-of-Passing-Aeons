# Assets 目录规范（负责什么就放什么）

> 本文件是资产归属的唯一权威说明。新增资源前先对照下表放位置；不要新建无主的顶层目录。

## 顶层职责

| 目录 | 职责 | 允许内容 | 禁止 |
|---|---|---|---|
| `Art/` | **美术**：只放视觉与音频表现资源（图片/材质/纹理/Shader/字体/特效/第三方视觉插件） | 贴图、材质、Shader、字体、TMP 插件资源、动画、模型 | 业务代码、内容 JSON、场景 |
| `Audio/` | **音频资产**：音效/音乐源文件 + `Resources` 包装（运行时按固定路径加载） | wav/ogg/mp3、`Resources/UI/*`、`Resources/Music/*` | 字体（归 Art）、代码 |
| `Data/` | **内容数据**：ScriptableObject 配置/世界默认配置 | `.asset` 配置数据 | 运行时资源、代码 |
| `Editor/` | **编辑器工具**：仅 Editor 程序集脚本 | Editor asmdef 下的工具 | 运行时逻辑 |
| `Scenes/` | **场景**：只放 `.unity` 场景 | Main.unity 等 | SpriteAtlas、Prefab、数据资产 |
| `Scripts/` | **运行时代码**：按程序集/系统分目录（Core/Content/World/Simulation/Rendering/Infrastructure/App…） | `.cs` + `.asmdef` | 美术/音频/内容资产 |
| `StreamingAssets/` | **可热改内容**：JSON 内容表、本地化、Mods | `Base/**/*.json`、`Mods/**` | 二进制大资源 |
| `Tests/` | **测试**：EditMode/PlayMode 测试程序集 | `.cs`、测试用 fixture | 运行时资产 |
| `UI/` | **UI**：UI 定义/面板/表现逻辑（UI 程序集） | UI 代码、UI asmdef | 模拟业务逻辑（通过契约访问） |
| `csc.rsp` | Unity C# 编译响应文件（必须位于 Assets 根） | 编译器选项 | — |

## 资源加载约定（Resources）

- 运行时动态加载的资源必须位于某个 `Resources/` 目录内；`.meta` 与资源成对移动/删除，GUID 不变。
- 字体：唯一副本 `Assets/Art/Fonts/Resources/simhei.ttf`，通过 `Resources.Load<Font>("simhei")` 加载；新增字体放 `Assets/Art/Fonts/`，需要运行时加载才放 `Art/Fonts/Resources/`。
- UI 音效：`Assets/Audio/Resources/UI/*`，通过 `Resources.Load<AudioClip>("UI/<name>")` 加载（`UiSfx`）。
- 音乐：`Assets/Audio/Resources/Music/*`，`MusicPlayerSystem` 通过 `Resources.LoadAll<AudioClip>("Music")` 加载。
- TMP：`Assets/Art/TextMesh Pro/Resources/TMP Settings.asset` 按 `Resources.Load("TMP Settings")` 解析，不要重命名该 Resources 子目录。

## 约束

1. 不出现重复字体/重复音频副本；资源移动必须同时移动 `.meta`。
2. 空占位目录用 `.gitkeep` 保留（`Art/` 下由美术分类）；有内容目录不留 `.gitkeep`。
3. 新增顶层目录前先更新本文件并在 PR/提交说明里写明职责。
