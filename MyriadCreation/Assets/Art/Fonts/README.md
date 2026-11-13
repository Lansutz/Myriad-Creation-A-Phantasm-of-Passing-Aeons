# 字体（美术资源）

| 文件 | 用途 |
|---|---|
| `Resources/simhei.ttf` | 默认界面字体（`Resources.Load<Font>("simhei")`；SettingsModel.FontChoice=0） |
| `Resources/Fonts/SourceHanSansSC-Regular.otf` | 思源黑体（界面字体选项 1；`Resources.Load<Font>("Fonts/SourceHanSansSC-Regular")`） |
| `Resources/Fonts/SourceHanSerifSC-Regular.otf` | 思源宋体（界面字体选项 2；`Resources.Load<Font>("Fonts/SourceHanSerifSC-Regular")`） |
| `Resources/Fonts/SourceHanSansSC-Medium.otf` | 思源黑体 Medium，保留设计资产 |
| `Resources/Fonts/SourceHanSerifSC-SemiBold.otf` | 思源宋体 SemiBold，保留设计资产 |

规则：
- 字体归美术（Art），运行时加载的放 `Art/Fonts/Resources/`；不要放回已删除的旧资源根目录或音频目录。
- 设置面板「界面字体」的选项映射由 `SettingsModel.FontChoice` / `TMPFontUtility` 维护；新增字体时同步 `TMPFontUtility` 的路径/choice 映射与 `TMPFontUtilityTests`。
- 新增字体保留 `.meta`，通过 GUID 引用；不要复制 simhei。
