# Myriad Creation: A Phantasm of Passing Aeons

## 纷繁的世界：一场流逝的幻景

> 文明演化项目 · 完整代码框架 v0.2.0-alpha（Unity 工程化版）

## 项目概览

基于《文明演化项目 · 系统设计企划书》完整 14 篇架构实现的 Unity 游戏代码框架。

* **正式游戏名**：Myriad Creation: A Phantasm of Passing Aeons（纷繁的世界：一场流逝的幻景）

* **27 个 C# 文件**（25 运行时 + 2 编辑器），覆盖全部核心模块

* **程序集分离**：CivilizationEvolution.Runtime / CivilizationEvolution.Editor（asmdef）

* **数据资产化**：世界参数以 ScriptableObject（.asset）配置，支持多套预设

* **Unity 6 (6000.5.8f1)** 兼容，0 编译错误，Main.unity 场景已一键搭好可直接运行

## Unity 工程化结构

```
CivilizationEvolution/
├── Assets/
│   ├── Scenes/Main.unity                         # 主场景（已搭好全部对象，直接 Play）
│   ├── ScriptableObjects/
│   │   └── DefaultWorldConfig.asset              # 世界参数配置资产（海陆/气候滑块）
│   ├── Scripts/                                  # —— Runtime 程序集 ——
│   │   ├── CivilizationEvolution.Runtime.asmdef  # 运行时程序集定义（引用 UGUI）
│   │   ├── Core/                                 # 核心底层
│   │   │   ├── GameConstants.cs                  # 游戏名/版本号全局常量
│   │   │   ├── GameEnums.cs                      # 14 类全局枚举
│   │   │   ├── TileData.cs                       # 地块数据 + TileGrid 网格工具
│   │   │   ├── WorldConfig.cs                    # 世界配置 ScriptableObject（同名文件）
│   │   │   ├── GameWorld.cs                      # 世界主类（主循环/事件/动态地块）
│   │   │   ├── GameManager.cs                    # 游戏状态（暂停/存档/速度）
│   │   │   ├── SaveSystem.cs                     # 存档读档（BinaryFormatter + SO 的 JSON 快照）
│   │   │   └── Bootstrap.cs                      # 启动入口
│   │   ├── Map/  Climate/  Race/  Culture/       # 地图/气候/种族/文化
│   │   ├── Economy/  Politics/  War/             # 经济/政治/战争
│   │   ├── Diplomacy/  Role/  Thought/           # 外交/角色/思想
│   │   ├── Disaster/  Building/  Tech/  AI/      # 灾害/建筑/革新/AI
│   │   ├── Render/MapRenderer.cs                 # 地图渲染（6 显示模式/相机控制）
│   │   └── UI/UIManager.cs                       # UGUI（信息栏/地块详情/事件日志）
│   ├── Editor/                                   # —— Editor 程序集（仅编辑器编译）——
│   │   ├── CivilizationEvolution.Editor.asmdef   # 编辑器程序集定义（引用 Runtime）
│   │   ├── CivilizationEvolutionMenu.cs          # 顶部菜单：一键搭建场景/建配置/存场景
│   │   └── HeadlessBuilder.cs                    # 无界面批处理构建入口
│   ├── Art/ Audio/ Prefabs/ Settings/ StreamingAssets/
├── Packages/manifest.json                        # 包依赖（已对齐 Unity6 兼容版本）
├── ProjectSettings/                              # Unity 配置
├── .gitignore
└── README.md
```

### 程序集划分（asmdef）

* **CivilizationEvolution.Runtime**：游戏全部运行时代码，仅引用 UnityEngine.UI；可打包进发布版。

* **CivilizationEvolution.Editor**：编辑器扩展，`includePlatforms=Editor`，引用 Runtime；不会进入发布包。

* 二者分离后，编辑器代码（UnityEditor 命名空间）不会泄漏到运行时，编译更快、依赖更清晰。

### 配置资产化（ScriptableObject）

* `WorldConfig` 继承 ScriptableObject，承载海陆 5 滑块 + 气候 8 参数 + 物理常量。

* 右键 `Create/Civilization Evolution/World Config` 即可新建多套预设（盘古大陆、群岛世界……）。

* GameWorld 启动时 `Instantiate` 一份运行时副本（`DontSave`），运行时调参不污染磁盘资产；未挂资产时自动用内置默认值。

* 存档时用 `JsonUtility.ToJson` 存配置快照，读档 `FromJsonOverwrite` 恢复（SO 不能走 BinaryFormatter）。

### 编辑器一键工具（顶部菜单 Civilization Evolution）

1. **一键搭建游戏场景**：自动生成 GameManager / GameWorld（挂配置）/ MapPlane / 正交相机 / 平行光 / MapEditor / 完整 UGUI / EventSystem / Bootstrap。
2. **创建世界配置资产**：生成 DefaultWorldConfig.asset。
3. **保存当前场景到 Main.unity**。
4. **地图编辑器面板（EditorWindow）**：画笔设置（模式/形状/半径/强度/连续绘制）、快捷操作（填充/清空地块）、海陆参数 5 滑块 + 左右连通（编辑配置资产）、地形重生成（随机种子，播放模式）。
5. **就地升级 Dropdown 展开模板**：为旧场景补齐显示模式下拉框的完整展开列表层级（Template/Viewport/Content/Item+Toggle）。

* `HeadlessBuilder.BuildAll` 供命令行无界面构建：
  `Unity.exe -batchmode -quit -projectPath <项目> -executeMethod CivilizationEvolution.EditorTools.HeadlessBuilder.BuildAll`

## 核心模块详解

### 1. 海陆系统（SeaLandGenerator）

* 5 滑块：海平面/陆地总量/陆地破碎度/海岸破碎度/外海缓冲

* 海洋三级划分：海岸带/近海大陆架/远洋深海，支撑海军与贸易

* 连通性洪水填充，陆地完全阻隔海洋连通

* 自由绘制：弃用维诺图，可自由改变地图形状尺寸，支持左右连通（wrapX）

* 画笔改图后增量重算：标记本格 + 6 邻格脏区，重新统计海洋地块分配

* 六边形网格：even-r 偏移坐标，6 邻格 + 距离计算

### 2. 气候系统（PlanetClimateSimulator）

* 成因层 8 参数：环流模式（单/双/三环流）/热赤道/热带南北缘/恒星辐射/四季强度/热量交换/季风强度

* 天文参数自动推导：黄赤交角/回归线/极圈由 seasonIntensity 计算

* 盛行风向场：低纬信风/中纬西风/高纬极地东风/季风反转

* 迎风坡/背风坡：沿风向回溯上游高程，抬升降水/雨影减少

* 春夏秋冬四季：温度/降水/季风随季节变化；九大温度带 + 14 种群系匹配

### 3. 种族系统（RaceData）

* 生理固定参数：寿命/生长/繁殖/体能/抗病/环境耐受/感官/认知

* **变革性 0-100（唯一社会维度）**：文化成熟度/分离阻力/融合阻力/阶层好感/革新倍率/叛乱阈值/社会流动性/暴力倾向 8 项派生

* 环境适配度、人口自然增长率

### 4. 文化系统（CultureData）

* 7 维相似度：生计 0.25/移动 0.15/葬俗 0.15/崇拜余弦 0.20/物质 0.10/象征 0.10/环境 0.05

* 分类型变量：相同=1，相邻=0.5，不同=0；分离/融合阻力含变革性修正

### 5. 经济系统（EconomyManager）

* 22 种基础物资、11 大类，加工链/保质期/重量；奴隶为特殊贸易品不可直接消耗

* 贸易中心 + 节点：地区中心持库存，AI 自动匹配供需；商队沿路线移动

* 货币四阶段：以物易物→称量货币→铸币→纸币，劣币/超发通胀、货币战争

* 税收 9 税种：控制度效率/最优税率区间/阶层好感联动

* 军事-经济硬联动：兵种合成与损耗填补直接消耗物资，无抽象维护费

### 6. 政治系统（PoliticalManager）

* 政权数据：财政/威望/稳定值/集权度；5 阶层好感度影响叛乱

* 法理系统：核心领土/宣称/占领区分；附庸层级；占领/割让/政体改革

### 7. 战争系统（CombatManager）

* 3 大类（步/骑/水）×4 等级；军团组织度/士气/补给三轨

* 战斗力 = 攻防×地形×组织度×士气×补给；A\* 寻路，陆军不入深海

### 8. 外交系统（DiplomacyManager）

* 关系值/信任度/威胁感知三数值；8 种平等盟约 + 8 种不平等从属；12 类条款

### 9. 角色与家族（CharacterManager）

* 六维属性 + 次级属性；三层人格特质；8 种羁绊；递归家族树；死亡/继承/将领选拔

### 10. 思想与规范（ThoughtManager）

* 学派四维/7 类宗教/8 罪 7 刑/思潮 5 阶段生命周期

### 11. 疾病与灾害（DisasterSystem）

* 13 种灾害（气象/地质/生物/人为）；7 种疾病按 R0/感染率/死亡率/恢复率传播

### 12. 建筑基建（BuildingSystem）

* 六大类 30 种建筑 4 等级；资源消耗/建造时间；每地块上限 5

### 13. 革新树（InnovationTree）

* 7 大类 50+ 革新 4 时代；前置依赖；研究点/速率/完成效果

### 14. AI 系统（AIManager）

* 效用函数 6 目标决策；AI 人格；日常行为 + 每 30 天重大决策

### 15. 渲染与 UI

* 6 种显示模式（地形/气候/群系/政治/人口/经济）；缩放/拖拽/WASD

* 完整 UGUI：顶部信息栏、速度控制、显示模式下拉、地块详情面板、事件日志

## 设计原则

1. **参数精简**：每个物理概念对应唯一变量，消除状态冲突
2. **成因驱动**：不做"全局温度/全局降水"滑块，所有现象由成因层参数推导
3. **增量重算**：脏标记机制，避免全量重算性能问题
4. **数据下沉**：地块级存储，行省仅为逻辑分组
5. **变革性单一维度**：种族社会差异全部由变革性管控
6. **物资驱动**：军事-经济硬联动，兵种合成消耗对应物资
7. **Unity 原生**：MonoBehaviour 生命周期 + ScriptableObject 资产 + asmdef 程序集 + UGUI，不引入第三方框架

## 打开方式

1. 用 **Unity Hub** → 打开 → 选择 `CivilizationEvolution` 文件夹（Unity 6000.5.8f1）
2. 等待首次编译与包解析完成（已对齐兼容版本，0 错误）
3. 打开 `Assets/Scenes/Main.unity`——场景对象已全部搭好并接线
4. 确认 GameWorld 物体的 Config 字段已挂 `DefaultWorldConfig` 资产
5. 直接点击 ▶ 运行

> 若需在新场景重建：菜单 `Civilization Evolution / 1. 一键搭建游戏场景`，再用菜单 3 保存。

## 包版本对齐（Unity 6 兼容）

以下包从旧版升级以消除"已弃用 API 被当作错误"的编译失败：

* com.unity.collab-proxy：2.1.0 → **2.13.5**

* com.unity.timeline：1.7.6 → **1.8.12**

* com.unity.test-framework：1.1.33 → **1.7.0**

* com.unity.ide.rider：3.0.24 → **3.0.38**；com.unity.ide.visualstudio：2.0.22 → **2.0.26**

* 已移除废弃的 com.unity.ide.vscode；com.unity.modules.vr 未引入（规避找不到 VR 模块的报错）

## 快捷键

* **空格**：暂停/继续；**F5** 快速保存；**F9** 快速加载

* **鼠标滚轮**：缩放；**中键/右键拖拽**：平移；**WASD**：移动相机

* **左键点击**：选中地块查看详情

## 待完善

* [x] 地图编辑器 EditorWindow 面板（画笔工具/参数滑块面板/地形重生成）

* [x] Dropdown 下拉模板完整样式（Template/Viewport/Content/Item+Toggle 层级）

* [ ] 音效与音乐系统、动画系统

* [ ] 多人联机、模组加载器

* [ ] 性能优化（ECS / Job System / Burst）

