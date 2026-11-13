# 参考项目分析（板块 / 气候 / 生态 / 灾害 / 自动化）

> 对象：`Things That Happen on the Map`、`FantasyMapSimulator`、`Earth Simulator`、`AzurLaneAutoScript`，
> 以及用户提供的 B 站 opus/视频线索。目的：为"生成期板块 + 局内地质气候动态 + 物产/阶层传导"找可落地的实现参考。
> 只读分析；未修改参考项目文件。

---

## 0. 总览

| 项目 | 形态 | 对本项目最有价值的部分 | 不直接照搬 |
|---|---|---|---|
| **Earth Simulator** | Electron/JS，源码可读（`EarthSimulator/*.js`） | **气候场（风/洋流/湿度/CO₂/海平面）**、**生态（NPP/植被/土壤/肥力/生物群系）**、**灾害改地形/气候**、**性能分条策略** | 它的"人口/粮食"抽象；我们的物产/阶层模型更细 |
| **FantasyMapSimulator** | Unity + StreamingAssets JSON | **BiomeResource 数据驱动**（群系→资源概率/价值/受哪些技能影响）、Buff 定义 | 它的视觉/地图渲染配置 |
| **Things That Happen on the Map** | Godot + `terrainfields` DLL + 文明 JSON | **"板块→地形→气候→文明更迭"的产品方向**（B 站开发计划标题）、文明配置分层（ideology/naming/polity profiles） | DLL 内部算法不可读；不复制其数据格式 |
| **AzurLaneAutoScript** | Python 任务型自动化框架 | **长跑调度/daemon/状态机/异常恢复/日志** 模式，可借鉴做"无头长周期模拟测试 harness" | 游戏具体逻辑与本项目无关 |
| **B 站 opus/视频** | 外部内容 | 方向确认：从板块漂移到文明更迭；具体参数需用户提供文案（本机抓取 B 站被内网 DNS 拦截） | — |

---

## 1. Earth Simulator（最有价值的实现参考）

源码位置：`E:/SteamLibrary/steamapps/common/Earth Simulator/EarthSimulator/`
关键文件：`climate.js`(502)、`ecology.js`(299)、`disasters.js`(374)、`core.js`(1361)、`civ.js`(2469)、`earthdata.js`(3137)。

### 1.1 气候模型（climate.js）

- **结构**：太阳辐射→纬度温度/季节→递减率→大陆度→**洋流（按季节风重算）**→温室/反照率→**风的水汽输运**（蒸发→平流→降水：迎风岸湿、内陆干、雨影、季风）。风与洋流是气候的参与者，不是装饰。
- **关键参数（可直接做成我们 `ClimateParams` 的初值）**：

| 参数 | 值 | 含义 |
|---|---|---|
| `T_equator / T_pole` | 30 / -25 | 赤道/极地基准温度 |
| `lapseK` | 32.5 | 递减率（6.5°C/1000m × 5000m） |
| `oceanInertia / landRelax` | 0.09 / 0.5 | 海洋热惯性/陆地松弛 |
| `oceanSeason` | 0.25 | 海洋季节振幅 |
| `co2Sens` | 3.6 | 每翻倍 CO₂ 的增温 |
| `iceThresh / meltThresh` | -2 / 1.5 | 结冰/融冰阈值 |
| `beltShiftDeg` | 7 | 风带季节位移 |
| `monsoonK / monsoonMax` | 0.055 / 0.55 | 季风强度/上限 |
| `qsatA/qsatB` | 9.0 / 0.058 | 饱和水汽（Clausius–Clapeyron 近似） |
| `evapOcean / evapLand` | 0.30 / 0.14 | 海/陆蒸发 |
| `windReach / advH` | 2.2 / 0.95 | 水汽平流距离/比例 |
| `rainBase/ITCZ/Midlat/SubtropDry/Oro/Cold` | 0.045/0.17/0.08/0.72/0.55/0.45 | 各降水项系数 |
| `mmScale` | 700 | 降水→mm/年 标定 |
| `seaPerDeg / seaRate` | 0.0001 / 0.00002 | 每升温 1° 的海平面上升/年最大速率 |
| `curEvery / curIters` | 8 / 110 | 洋流重算频率/迭代次数 |

- **洋流**：风应力驱动 + 科氏力 + 边界反射 + 扩散；暖流增温增湿，寒流降温减湿；`advectSST` 把异常沿流输运。
- **降水**：水汽平流 + 地形抬升（迎风坡）+ 雨影 + ITCZ/中纬风暴 + 副热带高压抑制 + 季风海陆热力差。

### 1.2 生态（ecology.js）

- **NPP（Miami 模型）**：
  `nppT = 3000/(1+exp(1.315-0.119*T))`，`nppP = 3000*(1-exp(-0.000664*P))`，取 min/3000 → 0..1；高温（>NPP_HEAT0）再乘热限制。
- **肥力**：
  `base(biome.arable) × (0.5+0.5·precipGauss) × (0.6+0.4·tempGauss) + 0.35·riverFlux − 0.40·slope + 0.30·volcanic`，再乘 `0.45+0.55·eco`、减 `0.30·pollution`、`0.5·fallout`。
- **生物群系分类**：Whittaker（温度×降水）+ 高度/坡度/海岸/水深；**带确认/滞回**（`biomeHold`/`biomeConfirm`），避免季节"群系跳变"。
- **植被**：`veg += (npp-veg) * (npp>veg ? grow : decay)`（慢长快退），演替；`eco` 健康度向 `0.30+0.70·veg-pollution` 松弛。
- **火山土**：`volcanic` 衰减极慢（~150 年半衰），肥力加成；污染/辐射扩散。
- **性能**：`ECO_STRIPE=4` 分条处理（每步 1/4 行、每格"一次活 4 步"），全量仅在生成/笔刷；颜色每 60 步刷新。**这是 524k 格性能的关键参考**。

### 1.3 灾害（disasters.js）

- 灾害不是编年史文本，而是**改世界场**：人口、建筑、植被、降水、温度、地形、火山灰、海平面。
- 参数：`P_VOLCANO 0.012`、`P_QUAKE 0.022`、`P_HURRICANE 0.06`、`P_DROUGHT 0.030`、`P_FLOOD 0.028`、`P_FIRE 0.035`、`P_PLAGUE 0.010`、`P_METEOR 0.0005`、`P_BLIZZARD 0.020`（按标准图年概率，面积缩放）。
- 火山：喷发抬升地形（海底可造岛）、烧植被、火山灰（先降温后肥土）；地震：改地形/建筑，触发海啸；海啸：沿海淹没；火灾：干燥+风蔓延；干旱：降水×0.38；**动态海平面**：温度→目标海平面，按 `seaRate` 缓慢逼近，达到阈值才 `recomputeStatic` + resDirty + markDirty（避免每年全图重算）。
- 存档：`o.ext.disasters = { list }`，读档恢复活动灾害；`W.fire` 重建燃烧集合。

### 1.4 对我们项目的直接映射

| Earth Simulator | 本项目落点 |
|---|---|
| `W.temp/precip/windU/windV/currentU/currentV/veg/soil/fertility/volcanic/seaLevel` | `TileData` 环境字段 + 全局 `EnvironmentState` |
| `CLIM` 参数表 | `Content/Environment/ClimateParams.json`（可调） |
| `ECO` 参数 + stripe | `EnvironmentTickSystem` + 分块/分帧预算 |
| `disasterTrigger` | 扩展 `DisasterSystem`（现状硬编码→JSON；效果改我们的物产/建筑/人口） |
| `simOn('save'/'load')` | SaveVersion 17 `EnvironmentSaveData` |
| **不搬**：`pop/foodCap/urban/industry` 的抽象 | 我们的 **具体物产 + 阶层需求** 替代 |

---

## 2. FantasyMapSimulator（数据驱动内容 schema）

源码数据：`D:/Steam/steamapps/common/FantasyMapSimulator/FantasyMapSimulator_Data/StreamingAssets/Base/`

- `BiomeResource/BiomeResource.json`：
  ```json
  resources: [ { "Name": "Tuna", "BaseValue": 120, "AffectedBy": ["FishAndHunting","Maritime"] }, ... ]
  biomeDistribution: [ { "Name": "Marine", "Resources": [ {"Name":"Tuna","Probability":0.6}, ... ] }, ... ]
  ```
  → **群系→资源清单（概率/价值/受哪些技能影响）**。这正好对应我们 `GoodsDef.originBiomes` + `baseAbundance` 的"内容化"：把"哪个群系产哪些物产、概率多少、受哪些革新/阶层技能修正"做成 JSON，而不是硬编码。
- `Buff/BuffDef.json`：通用 buff 定义（key/type/duration/value）→ 可用于我们的"环境修正/革新/法律"对物产产出的乘子。

**借鉴结论**：新增 `Environment/BiomeResources.json`（群系→物产权重/概率/受修正项）与 `ClassDemand.json`；`GoodsDef` 保留物理属性，产出用"群系/环境适宜度 × 修正项"计算。

---

## 3. Things That Happen on the Map（产品方向参考）

- 本机为 Godot 发布版：`map.exe` + `libterrainfields.windows.template_release.x86_64.dll` + 大量 `*.civilization.json` + `*.wsave`。
- `terrainfields` 是地形生成库（DLL 不可读，但名字表明"terrain fields"：高度/湿度/温度/河流等场）。
- `*.civilization.json` 是**文明配置**（`assignable/color/display_name/ideology_profile/naming_profile/polity_profile/generation_sequences/pools/schema_version`），不是环境数据；说明它把"文明生成规则"与地图分离。
- B 站线索：搜索到相关视频标题 [【文明与地图】从板块漂移到文明更迭：10月11日更新和接下来的开发计划](https://www.bilibili.com/video/BV1Wdpv6wEPH/)（用户给的 opus 链接本机抓取被内网 DNS 拦截，未能读取正文）。这确认其方向：**板块→地形/气候→文明更迭**，与我们计划一致。
- **借鉴**：把板块作为生成期一等输入；文明/物产规则与地图分离（我们已有 Content JSON + 契约分层，方向一致）。
- **不能借鉴**：DLL 内部算法/存档格式（闭源二进制）；需自行实现并测试。

---

## 4. AzurLaneAutoScript（工程化参考，不是玩法参考）

- Python 任务型自动化（`module/daemon`、`campaign`、`os`、`handler`、GUI、24/7 长跑、异常恢复、日志）。
- 对本项目有价值的不是游戏逻辑，而是**长周期自动化的工程模式**：
  - 任务调度/守护（对应我们的 headless 多年度模拟测试）；
  - 状态机 + 异常恢复（对应模拟不变量检查、失败重跑）；
  - 结构化日志/回放（对应模拟 trace/replay，验证确定性）。
- 建议：做一个 **Headless Simulation Harness**（Unity batchmode 或纯 C# 测试驱动），跑 100–1000 年模拟、断言不变量（人口/物产守恒、无 NaN、性能预算），借鉴其 daemon/任务模式；不建议引入其 Python 运行时。

---

## 5. 落地后的设计增量（相对现有 plan）

1. **气候参数/生态参数内容化**：`ClimateParams.json`、`EcologyParams.json`（借用 Earth Simulator 的数值作初值）。
2. **生态算法**：NPP（Miami）、肥力公式、生物群系带滞回、植被演替、火山土、污染——全部按 Earth Simulator 公式移植到 C#（含参数）。
3. **灾害场效应**：火山抬地形/造岛、灰降温暖后肥土、地震改地形、海啸沿海、动态海平面阶梯式、火灾风蔓延；灾害入档。
4. **性能**：环境 tick 用 `ECO_STRIPE` 式分条 + dirty 块 + 跨帧预算；海平面变化才重算海岸。
5. **内容 schema**：`BiomeResources.json`（FantasyMapSimulator 式）+ `ClassDemand.json` + `Disaster.json`。
6. **测试 harness**：借鉴 ALAS 的 daemon/长跑思路，headless 跑多年并断言确定性/守恒/性能。
7. **产品方向**：参考 Things That Happen on the Map：板块→地形→气候→文明；文明配置与地图数据分离（我们已分离）。

> 边界：不复制任何参考项目的美术/数据格式/闭源算法；仅借鉴模型思想与参数，重新实现并测试。


---

## 6. civ-atlas（`https://github.com/guaner-334/civ-atlas`）—— 最贴近本项目的完整参考

> 本机已下载分析 v1.5.0（`D:/TEMP/civ-atlas-ref/civ-atlas-main`，TypeScript/React/Vite，245 个 ts 文件，
> `src/gen` 约 2.2 万行）。**许可证 AGPL-3.0**：只学习算法/架构/数据模型，**不复制代码**；
> 若要复用代码必须接受 AGPL 或单独取得授权。

### 6.1 它做的事（README / 源码）

`球面网格 → 板块构造 → 抬升与侵蚀 → 河流湖泊 → 气温/风/降水/洋流 → 生物群落 → 宜居度 → 州/城 → 民族/国家/战争 → 编年史`

- 球面：球面泊松圆盘 + Delaunay，默认 **36,000 地块**（可到 20 万），东西无缝、含两极。
- 板块：大小悬殊的板块绕**欧拉轴**运动；汇聚带造山、俯冲带海沟、离散带洋中脊；大陆/微大陆/岛弧/极地大陆；板块可编辑（漂移/切开/合并/换地壳）。
- 侵蚀：**流水功率定律** `dh/dt = U - K·A^m·S`，**Braun & Willett (2013) 隐式解**；**Priority-Flood** 填洼 + 排水。
- 气候：纬度温度表 + 海拔递减；**三圈环流风带**输运水汽，迎风坡雨、背风坡雨影；洋流按大洋环流规律（暖/寒流、上升流）修正沿岸温湿。
- 生态：**Whittaker 图**（年均温 × 年降水）划分 16 种群落；湿地/河口/海冰。
- 推演：事件堆 + 变化日志；每 100 年检查点；可拖动时间轴；可施加"地形大事"（火山、地震抬升、海水侵入），**以前的历史不变，之后按新地形继续**。
- 确定性：`subSeed(seed,'用途')` 派生随机、`keyed` 按"地块锚点+年份+用途"取随机；超越函数 `round24` 舍入到 24 位；**生成器版本号**（算法改动则 +1，旧存档仍可打开）；存档只存种子/参数/修改（通常几 KB）。

### 6.2 可直接借鉴的算法/参数

| 模块 | 关键做法/参数 | 对本项目 |
|---|---|---|
| `params.ts` | `cells=36000, landFraction=0.33, plates=30, mountains=1, temperature=0, rainfall=1, continents=0(自动2~5), gather/rugged/hardness/north` | 暴露到 `WorldSetupOptions` + 新建面板 |
| `tectonics.ts` | 板块种子→不规则 Voronoi 生长（对数正态大小）；每板块欧拉轴角速度 `ω`，速度 `= ω × p`；汇聚/离散用边界处速度差；洋壳年龄→深度（脊 -2600m → 深海平原 -5000m）；海沟 -7000~-9000m；大陆架/岛弧/热点 | 新增 `PlateTectonicsGenerator`；`plateId/tectonicStress/crustAge/oceanDepth` |
| `erosion.ts` | 流功率 `K=1, m=0.5, dt=0.9, steps=STEPS/2`；Braun-Willett 隐式；Priority-Flood（Barnes 2014）；汇水按降雨加权 | 生成期侵蚀 + 局内长期侵蚀（可复用现有 `HydraulicErosion` 升级） |
| `climate.ts` | `TEMP_BY_LAT`（0→27, 30→20, 45→11, 60→1, 75→-11, 90→-24）；`RAIN_BY_LAT`（0→1.25, 16→0.7, 30→0.38, 45→0.85, 60→0.6, 75→0.35, 90→0.2）；6.5°C/km；风带 0–30 信风、30–60 西风、60+ 极地东风，±5° 平滑；季节风带 ±6°；海陆蒸发/平流/雨影 | 替换/校正现有 `PlanetClimateSimulator`；可直接做 `ClimateParams` |
| `currents.ts` | 流函数画环流：副热带 0–46°、副极地 46–72°；西边界流宽 300km；影响半径 1300–2200km；盆地 1000km 起、3500km 完整；西/东侧 SST 偏差表 | 升级现有 `OceanCurrentSimulator`（当前较简单） |
| `biomes.ts` | 16 群落；每群落数值：`habitability/travelCost/roadCost/trekPace/famineRisk/soilFragility/grain/pasture/timber/oil`（0–1） | **直接对应我们的物产**：grain/pasture/timber/oil → 谷物/牧草/木材/燃料类；`soilFragility` → 侵蚀/盐碱敏感度 |
| `development.ts` | 每州 12 类资源（粮/牲畜/铜锡/铁/煤/木/盐/渔/石油/稀有/金银）**0–3 档**；发现/可采/枯竭/回长；地形大事后**按新地形重分档**；时代门槛卡资源；人口上限×生产能力 | 我们的 `TileData` 资源/物产产出与阶层需求可直接用此模型 |
| `engine/rand.ts` | `subSeed`、`keyed`、`anchorTag(cell,n)`、`round24`（Veltkamp 拆分） | 环境 tick/灾害的确定性 RNG 方案 |
| `world.ts` | 生成分段 `yield`（可中断/继续）；生成器版本；`growTerrain` 分阶段 | 我们的 `GenerationPipeline` 可借鉴"分段+可中断" |
| `terrainEdits.ts` | 火山/山脉/抬陆/沉海在**侵蚀之前**以"自然力"注入，之后侵蚀/排水/气候/群落照常重算；改过的地形不参与"最高峰"归一；海岸加噪声；气候重算可回滚 | 局内海侵/火山/地震抬升的实现模板 |
| 存档 | 只存 seed/params/edits；检查点每 100 年；变化日志 | 我们的 Save 可大幅减小（但现有存档 DTO 需兼容） |

### 6.3 产品层面可直接对照

- README 明确："缺盐、缺马、缺铁、缺金银的国家会去打边境对面有这些物产的州"——与我们的**具体物产驱动战争**一致。
- 未来路线："土地会因为开垦、水利变好，也会因为过度耕作、旱灾变差；人口从按州记改成按更小地块记"——与我们 E5 人类反馈方向一致。
- 地形大事："在指定年份触发火山喷发、地震抬升或海水侵入；这一年以前的历史不变，之后按新地形继续"——**正是我们"局内地质变化 + 开关"要的交互模型**。

### 6.5 civ-atlas 的族群分离机制不足（用户判断 + 源码依据）

- 它是**一州一 Culture 的硬分区**（`assimilation.ts`：按概率把州改换民族；`cultures.ts`：一个民族占一片州），没有混合人口/多族群共居；
- 身份只有"文化"一维，没有语言/宗教/阶层/血缘交叉，也没有"我们/他们"的语言感知、自称/他称；
- 分化（`DivergeCheck`）只看"地理隔断/不同国家 + 面积/定居年限"，新族名=母族+方位（"北某某"），缺少经济网络、通婚、制度、精英策略、宗教等原因；
- 族名由命名风格统一生成，不是群体自我命名/他称的历史结果；不能嵌套（氏族/教派/分支）。
- 我们的替代模型见 [LANGUAGE_NAMING_IDENTITY.md](LANGUAGE_NAMING_IDENTITY.md) §4：多层身份 + 人口构成 + 边界强度 + 有行动者的事件（同化/分化/民族生成/语言转用/离散/融合），允许混居、嵌套、双言、多重认同。

### 6.4 不能直接照搬

- 它是 TypeScript Web/Worker、球面 Delaunay 网格（3.6 万 cell）；我们是 Unity/C#、1024×512 tile 网格 + 球面渲染。算法可移植，**数据结构要落到我们的 TileData/区域/聚落体系**。
- AGPL-3.0：不能复制源码进本仓库（除非本项目管理许可变更）。只按文档/公式独立实现。

---

## 7. Doubao Agent 工作区（本机提取的《Things That Happen on the Map》Godot 工程）

路径：`C:/Users/25652/AppData/Local/Doubao/.../agents/m_0cwE5PC3NQp/`

- `pck_dump/` 是被解包的 Godot PCK：**162 个脚本类**、`map_generator`、`terrain_fields` 原生扩展、地图预设（真实世界高度图）、`headless_*_runner` 测试。
- 地图/气候：**气候带由纬度（`lat_north/lat_south`）+ 三圈环流（`circulation`, 3 cell）+ 海陆对比生成**；`temperature/humidity/elevation` 是省字段；地形类型约 10 种（desert/forest/mountain/plains/sea/lake/savanna/rainforest/taiga/ice/snow mountain）；`Sea Level`、`Coast Detail`、`Land Amount`、`Edge Ocean`、`Seed` 可调。
- 地形编辑：`terraform` 画笔 → 目标地形 → **Recompute Climate from current terrain**（带 transition width、预览、失败回滚、sink/rise 统计）；人口容量由地形决定。
- 工程化：大量 `headless_*_runner`（map/border/coast/nav/road/world setup），这是我们可以借鉴的"无头地图/环境测试"模式。
- **该版本没有动态板块/气候演算**（板块漂移是 B 站开发计划里的新方向）；其价值在"地图/气候生成 + 编辑器重算流程 + 无头测试"。
- 同目录 CK3 数据：`ck3_culture.txt`（文化清单）、`innov_index.txt`（农业革新：刀耕火种/休耕/轮作/三圃/重犁/马轭/水磨/灌溉）——正好作为我们"土地生产力/环境修正"的革新门槛参考。


## 9. Earth Simulator 的货币实现（`walkeco.js`，值得逐条借鉴）

用户指出：Earth 最值得借鉴的是**货币在代码层面的具体实践**。确实如此，`walkeco.js` 里：

- **每政权自己的货币**：`wkCurrency(st)`；形式按时代+地理：
  - 部落：沿海用**贝壳（shell）**、内陆用**珠子（bead）**；
  - 早期古典、未发明"货币"前用**谷物（grain）**计量；
  - 古典→**硬币（coin）**、工业→**纸币（note）**、太空→**信用（credit）**。
- **货币名由政权 uid+世界种子生成音节**（如"дарик/талин"），同一政权名不变；有语法变格；不同政权同材质也不同名。
- **发行者链**：附庸用宗主货币（`ECO_UNION`，链最多 4 层）；同盟不共币；灭亡政权的货币只按金属价值流通（`ECO_DEAD_RATE`）。
- **工资/价格锚**：`wkWage(st)` 按 `gdpPc^0.6`；价格以"本地货币×工资天数"表示；`ecoVal` 用稳定度/战争折价；兑换有手续费 `ECO_FEE`。
- **钱包按政权**（`WKP.purse[uid]`），金额/格式/图标/变格完整；灭亡政权保留"货币指纹"（最后形式+名字+工资）。
- **市场**：价格受饥荒/瘟疫/战争/距离/黑市/声望影响；商店卖什么由时代、发明、矿藏、法律、饥荒、瘟疫、战争决定。

对我们的 `CurrencySystem`（现只有单一 `currencyName`+阶段）是直接升级蓝本：`CurrencyDef`（form/material/issuer/nameByCulture/value anchor/acceptance）+ `CurrencyWallet`（政权/区域）+ `ExchangeRate`（含发行者链、灭亡货币、手续费）。
