# Audio 资产放置规则

- **UI 音效（运行时加载）**：`Assets/Audio/Resources/UI/`。
  `UiSfx` 继续使用 `Resources.Load<AudioClip>("UI/*")`，路径不要改。
- **新的音频资产**统一放在 `Assets/Audio/` 下，不要放回已删除的根 Resources 目录。
- `Assets/Audio/Music`、`Assets/Audio/SFX` 为占位目录，保留给未来音频资产组织。
  如果未来需要 `Resources.LoadAll("Music")` / `Resources.LoadAll("SFX")` 加载，
  对应资源应放到 `Assets/Audio/Resources/Music/`、`Assets/Audio/Resources/SFX/`；
  `UiSfx` 的 UI 音效路径保持 `UI/*` 不变。
- 音频资产已统一迁移到 `Assets/Audio/Resources/`；旧根资源目录已删除，不要回迁。
- 字体不在 Audio：字体归美术，见 `Assets/Art/Fonts/`。
