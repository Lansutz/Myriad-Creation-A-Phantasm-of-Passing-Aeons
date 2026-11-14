# P9 革新扩容、可通行河道与城市兴衰

> 立项目标：把革新从“框架 + 186 条样例”做成能支撑全部系统的内容层；把河流当作与海洋同类的**水体**接通航行与贸易；让城市能因河流改道、气候变迁而自然衰亡。完成后才回到“第一座城市出现在第几千年”的校准问题。
>
> 原则不变：机制优先于抽象数值；先读现有代码再动手；改现有系统而不另开新系统；长周期用 `daysPerTick` 批处理；每步 build 全绿后提交推送。

---

## 1. 现状诊断（2026-11-14 实测）

### 1.1 革新

- `StreamingAssets/Base/Innovation/Innovations.json`：**186 条**，25 个 `InnovationField`，6 个 `era`。
- 时代分布极不均：era0 24、era1 36、era2 51、era3 53、era4 20、**era5 仅 2**。
- 领域分布也不均：工艺 28、农业 19、军事 19、行政 19，而文字 1、宗教 1、教育 1、建筑 2、数学 2、医学 3、法律 3。
- `unlocks` 里 145 条是 `capability:...` 字符串，只有 89 条有结构化的 `unlockRefs`；建筑、单位、配方、装备的真实门槛大多没接线。
- 来源字段大量是“技术史/制度史通识条目（待补具体来源）”，不满足可核查要求。

### 1.2 法律/法令

- `Laws.json`：12 组、**50 条法律**、**2 条法令**；开局已知 17 条。
- `requiredInnovationIds` **全部为空**（50/50、2/2）。`LawAndDecreeSystem` 有校验代码，但数据从未使用。
- 结果：法律/法令没有“随革新出现”的过程，玩家从开局就能看到几乎全部选项。

### 1.3 研究机制

- `InnovationTree.DailyTick` 只按 `researchRate` 累加研究点，达到 `researchCost` 即完成；没有社会实践、灵感、个人熟练度、政权掌握。
- `InnovationProgressConfig` 的一批常数（实践阈值、规模阈值、边际递减）没有接入主链，属于空转配置。

### 1.4 河流与水体

- `RiverNavigability.IsNavigable` 与 `TerrainConditions.IsNavigableRiver` 是两套重复判定，只影响单位移动速度。
- 经济贸易路线按“地区编号 ±1/±2/±4/±8”直接连边，**完全不看河道、海洋、地形**；河运对贸易没有任何作用。
- 代码中没有河流改道（avulsion）、没有河道迁移、没有“河流消失后沿岸农业衰退”的链路。
- 聚落已有废弃端口（人口/守军不足积累压力），但没有由环境驱动的城市死亡。

---

## 2. P9.1 革新目录扩容

### 2.1 数量与覆盖目标

以“可研究、可模拟、可核查”为准，不追求历史百科式堆量：

| 维度 | 现状 | 目标 |
|---|---|---|
| 总条目 | 186 | **420–500** |
| 每领域下限 | 1（文字/宗教/教育） | **≥10**（26 个领域） |
| era0（采集/早期农业） | 24 | 60–70 |
| era1（农业定居/青铜前） | 36 | 90–100 |
| era2（青铜/早期国家） | 51 | 110–120 |
| era3（铁器/古典） | 53 | 110–120 |
| era4（500–1500） | 20 | 60–70 |
| era5（1500 后） | 2 | 30–40 |

### 2.2 条目准入规则（沿用并加严）

1. 一条革新必须改变至少一个模拟量或解锁一个具体内容；纯描述性标签不进目录。
2. 禁止政体/结构分析术语（王国、帝国、封建、中央集权等）作为节点；它们是 `PolityForm/StateStructure` 的推断结果。
3. 历史专名走 `variantOf`，通用概念才是研究节点。
4. 每条必须有 `termEn`、可操作定义、`scope`、1–3 条可核查来源（DOI/ISBN/权威工具书条目）；不再允许“待补具体来源”。
5. 前置构成 DAG：无环、每领域有 2–3 条起点、跨领域前置必须写 `prerequisiteRationale`。

### 2.3 数据模型增补

`InnovationDef` 增加（与 P8.2 对齐）：

- `practiceRequirement`：立项前需要的实践经验（默认 `researchCost × 0.5`）。
- `insightWeight`：灵感权重（0–1，稀有/颠覆性条目更高）。
- `requiredRealmMastery`：政权掌握门槛（默认 1；高难条目 2–3）。
- `unlockRefs`：全部结构化（`kind + id/key`），`capability:` 字符串只保留为派生缓存。

### 2.4 分组与命名

- 研究面板按 `field` 分组，`category` 只作展示子标签；同一 `category` 不得跨 3 个以上领域复用。
- 名称用中性模拟词，不用“法脉/教派”这类现实专名；现实专名只进 `variantOf`。

---

## 3. P9.2 全系统接线

### 3.1 法律与法令

- 50 条法律逐条补 `requiredInnovationIds`：tier1 可以 0–1 条，tier2+ 至少 1 条，涉及行政/财政/军制的 tier3+ 至少 2 条。
- 新增法律组与法令，覆盖目前没有制度表达的领域：土地/水利、市场/度量衡、劳役、公共卫生、教育、宗教管理、外交礼制。
- 目标：法律 80–100 条、法令 15–25 条；开局已知只保留“无革新也能运行的部族惯例”（≤12 条）。
- `LawAndDecreeSystem` 的“不可用原因”要能区分：缺法律前置、缺革新、缺机构、缺威望等级。

### 3.2 建筑、单位、装备、配方、货物、宗教

- 建筑：29 条全部补 `requiredInnovation` 或显式 `unlockedByDefault`；仓储类（粮仓/盐仓/金库/货栈）与升级配方按 P8.4 的设计接。
- 单位：14 条按兵种体系接（征召→常备→专业兵）。
- 装备：23 条按材料/工艺链接（石→铜→青铜→铁→钢）。
- 配方：43 条按工艺链接，禁止“有材料就能造”跳过关键工艺。
- 货物：29 条按开采/加工/保存工艺接。
- 宗教：`requiredInnovations` 已有字段但数据为空，按教义/制度类革新接。

### 3.3 研究机制（P8.2 落地）

- 社会实践池：按 `innovationId` 累计，来源为农业、手工、战争、行政、贸易、信仰/学术；`baseWeight × scale^0.5 × quality × durationDays`。
- 立项门槛：实践 ≥ `practiceRequirement × 0.5`；完成研究后政权获得掌握。
- 个人层：`mastery 0–3`、`practice`、`scholarship`；灵感基础概率低，受天才/机构/相关实践影响，上限受控。
- 法律/建筑/配方门槛从“已解锁”改为“已掌握（mastery ≥1）”；未掌握显示“已知未掌握”。
- `InnovationProgressConfig` 的空转常数全部接入或删除。

### 3.4 UI 与读模型

- 革新面板：当前研究、实践进度、可发现、已知未掌握、灵感事件；可立项/换项/取消。
- 法律/建筑/配方列表显示锁定原因（缺哪条革新/实践/机构）。
- 新增读模型查询接口，一个 UI 文件只负责一个面板并注册到管理器。

---

## 4. P9.3 河流作为水体

### 4.1 单一判定源

- 合并 `RiverNavigability` 与 `TerrainConditions.IsNavigableRiver` 为一份实现，`TerrainConditions` 委托调用。
- 新增 `WaterBodyKind`（Ocean/Coast/River/None）与 `IsBoatPassable`：海洋、海岸、可通行河道为真；浅滩/季节性河道按宽度与深度判定。
- 陆地单位过河仍可涉水，但速度/补给按 `IsDeepRiver`/`RiverDepthMeters` 惩罚；船运走 `IsBoatPassable`。

### 4.2 贸易与运输

- 贸易路线成本改为“地形 + 水体”模型：陆路按距离/坡度，河运按可通行河段，海运按海岸/海洋；同一路线可分段换乘。
- 河运效率要体现“逆流/顺流”差异（按河流流向与路线方向）。
- 港口/渡口/桥梁成为真正的运输节点，而不是装饰性建筑。

### 4.3 合流与河网

- 保留流量累积生成；补测试：支流汇入后 `streamOrder`、宽度、深度、流量在交汇处单调增加。
- 河网连通性检查：任何河段必须能追溯到海洋或内陆湖；断头河要么是内陆盆地，要么是数据错误。

---

## 5. P9.4 河流改道与城市死亡

### 5.1 改道机制（事件驱动，不做全图逐日扫描）

- 候选河段：低坡度、高含沙、冲积平原上的河段；按 `坡度 × 流量 × 泥沙` 计算易发性。
- 触发：洪水/暴雨事件 + 随机判定；触发后河道在局部改走新路径，旧河段降级为季节性/干涸。
- 后果：沿岸地块水分/肥力/农业产出下降；依赖该河段的聚落粮食剩余下降，废弃压力积累，最终降级为营地或废墟。
- 性能：只对候选河段按年/季结算，不进入逐日全图循环；大世界按地区分片。

### 5.2 气候驱动

- 已有气候模拟输出降水/温度；新增“多年干旱/季风减弱”状态，按年滑动窗口判定。
- 干旱持续时，河流流量、地下水位、农业产出、承载力的下降要能传递到聚落人口与废弃。

### 5.3 城市兴衰观测

- 长跑除 `firstCity/firstRealm` 外，记录：城市建立数、城市死亡数、死亡原因（河道改道/干旱/战争/疫病/人口流失）。
- 不要求“只能有两座城市”；允许城市在窗口内出生、繁荣、衰亡。
- 编年史为每座城市的建立/衰亡写条目，便于验收与玩家阅读。

### 5.4 文献依据

- 哈拉帕：Giosan et al. 2012, *Fluvial landscapes of the Harappan civilization*, PNAS, DOI 10.1073/pnas.1112743109（349 引）；Dixit et al. 2014, *Abrupt weakening of the summer monsoon in northwest India ~4100 yr ago*, Geology, DOI 10.1130/g35236.1（392 引）。
- 楼兰：2017, *The world's earliest Aral-Sea type disaster: the decline of the Loulan Kingdom in the Tarim Basin*, Scientific Reports, DOI 10.1038/srep43102。
- 技术扩散与累积文化：Comin & Hobijn 2010, *An Exploration of Technology Diffusion*, AER, DOI 10.1257/aer.100.5.2031（738 引）；Spolaore & Wacziarg 2013, *How Deep Are the Roots of Economic Development?*, JEL, DOI 10.1257/jel.51.2.325（743 引）；Henrich 2016, *Innovation in the collective brain*, Phil. Trans. R. Soc. B, DOI 10.1098/rstb.2015.0192（442 引）。
- 社会复杂度与历史数据库：Turchin et al. 2018, *Quantitative historical analysis uncovers a single dimension of complexity*, PNAS, DOI 10.1073/pnas.1708800115（292 引）。
- 技术史：Mokyr 1990, *The Lever of Riches*（1896 引）；Basalla 1988, *The Evolution of Technology*。
- 农业起源：Price & Bar-Yosef 2011, *The Origins of Agriculture: New Data, New Ideas*, Current Anthropology, DOI 10.1086/659964（283 引）。

> 说明：以上用于校准“机制形状”，不是把现实历史当剧本；游戏世界可以架空，只要求机制自洽。

---

## 6. 里程碑与验收

### M1 内容层（革新目录）

- 总数 ≥420，26 领域各 ≥10，era5 ≥30。
- 每条有 `termEn`、定义、来源；无“待补具体来源”；前置 DAG 无环。
- 自动化审计测试：覆盖矩阵、来源完整性、DAG、重复 ID、`variantOf` 合法性。

### M2 接线层

- 50 条既有法律全部有 `requiredInnovationIds`（按 tier 规则），新增法律/法令达到目标数量。
- 建筑/单位/装备/配方/货物/宗教的 `requiredInnovation` 覆盖率 100%（或显式默认解锁）。
- 测试：无革新时锁定；掌握后出现在可选列表。

### M3 研究机制

- 实践池/灵感/掌握三件套接入；无实践不能立项；50% 实践可立项；完成→掌握→解锁。
- 法律/建筑列表随掌握变化（读模型 + UI 测试）。

### M4 河流水体

- 单一 `IsBoatPassable` 判定；贸易路线成本模型接入河运/海运。
- 河网连通性与合流单调性测试通过。
- 河运对贸易量/成本有可观测影响（A/B 测试：同地图开关河运）。

### M5 城市兴衰

- 改道/干旱能降低沿岸农业与粮食剩余，触发聚落废弃。
- 单元测试：构造一条河 + 一个依赖它的城，触发改道后 N 年内城市降级。
- 长跑观测输出城市建立/死亡数与原因；允许 ≥2 座城市先后兴衰。
- 长跑窗口内不出现“全图城市同时饿死”的系统性失真。

### 全局验收

- `build.cmd`：`EXIT=0`、零 `error CS`、EditMode 全绿。
- 新增/修改内容通过内容校验与审计测试。
- P9/DECISIONS/PROJECT 同步；每阶段 commit + push。

---

## 7. 顺序

1. **M1 内容层**（先行，纯数据 + 审计测试，风险最低）。
2. **M2 接线层**（依赖 M1 的 ID 稳定）。
3. **M3 研究机制**（与 M2 可并行；改 `InnovationTree` 与读模型）。
4. **M4 河流水体**（独立域，可与 M1–M3 并行）。
5. **M5 城市兴衰**（依赖 M4 的水体/改道与既有废弃端口）。
6. 最后回到 5000–7000 年校准：用 M1–M5 后的默认世界长跑重新观测，再讨论区间是否合理。

## 8. 与既有计划的关系

- 取代 P8.2 的“内容与接线”条目，保留其实践池/灵感/掌握设计。
- 与 P8.4 仓储设施合并：仓储建筑需要 `requiredInnovation` 与升级配方。
- P8.5 的观测/UI 验收并入 M5。
- 不新开系统：`InnovationTree`、`LawAndDecreeSystem`、`HydrologySystem`、`EconomyManager`、`SettlementEvolutionSystem` 都是改造对象。

---

## 9. M1 实施记录（2026-11-14）

- 革新 186→446 条；26 领域各 ≥10；era 分布 0:37 / 1:70 / 2:111 / 3:116 / 4:48 / 5:64。
- 145 条旧条目来源替换为领域内可核查文献；运行时兜底定义补来源。
- `InnovationCatalogAuditTests` 4 例：总量、领域/时代覆盖、来源、DAG 无环、ID/名称唯一。
- 修正 6 处跨时代前置（爆破采矿改 era5、待客礼/财产观念改挂珠饰交换、口述谱系去前置、书吏培养改 era2 挂文字）。
- 验证：`EXIT=0`、1043 例、1034 通过、0 失败、零 `error CS`。
- 下一步：M2 法律/法令与建筑/单位/配方接线。

## 10. M2 实施记录（2026-11-14）

- 法律 50/50、法令 2/2 全部有 `requiredInnovationIds`；tier1 用开端节点，tier2+ 至少一条。
- 开国基础制度知识 7 条，写入新政权，避免“开国无任何惯例可推行”。
- 建筑新增 `requiredInnovation` 字段并接入 `CanBuildBuilding`（直接掌握或 unlockRefs 两条路径）；29/29 映射。
- 单位 14/14、配方 43/43、加工货物 10 条、宗教 44/44 接通；原料保留显式默认。
- 新增 `LawInnovationGateTests`（5）、`ContentInnovationGateAuditTests`（6）；`LawContentJsonTests` 不再复制革新 id。
- 验证：`EXIT=0`、1054 例、1045 通过、0 失败、零 `error CS`。
- 下一步：M3 社会实践池/灵感/掌握链路。

## 11. M3 实施记录（2026-11-14）

- `InnovationTree` 新增政权实践池（按 `InnovationField`），来源为农业/矿产入库与配方成品；`AddPractice/GetPractice` 公开。
- `InnovationDef` 增加 `practiceRequirement / insightWeight / requiredRealmMastery`；立项要求实践 ≥ 需求 50%（world 为空时不读池）。
- `InnovationKnowledgeSystem.GetRealmMastery` 取政权内最高角色熟练度；研究完成自动 `Learn(1)`。
- `RealmPracticeGateTests` 4 例；保存/持久化测试补实践种子。
- 验证：`EXIT=0`、1058 例、1049 通过、0 失败、零 `error CS`。
- 下一步：M4 河流作为水体与贸易成本。
