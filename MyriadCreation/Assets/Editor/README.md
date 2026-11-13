# Editor（编辑器工具）规范

只允许 Editor 平台脚本（`MyriadCreation.Editor.asmdef`，includePlatforms=Editor）。

| 子目录 | 职责 |
|---|---|
| `Tools/` | 通用编辑器工具（批处理编译校验 `BatchCompileCheck`、自动打开主场景、图形 API 修正、地形画笔 `TerrainPainter/`） |
| `Content/` | 内容包导出/内容制作工具（`ContentPackageExporter`） |
| `TMP/` | TextMesh Pro 资源生成/修复（`TmpSettingsCreator`，路径指向 `Assets/Art/TextMesh Pro/Resources`） |

新增工具先归入上述三类；不要放运行时逻辑或资源资产。
