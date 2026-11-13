# 统一总计划 V2（UNIFIED MASTER PLAN）

> 生效：2026-11-03。**当前唯一执行口径**。
> 旧计划/设计快照已归档：[2026-11-03_pre-unified-plan-backup](../archive/2026-11-03_pre-unified-plan-backup/README.md)（46 个文件 + sha256 清单 + zip）。
> 架构硬规则见 [ARCHITECTURE_RULES.md](ARCHITECTURE_RULES.md)；旧 [MASTER_COMPLETION_PLAN.md](MASTER_COMPLETION_PLAN.md) 保留为历史（W1–W12 收口记录），不再作为当前排期依据。

---

## 1. 当前基线（事实）

| 项 | 值 |
|---|---|
| 编译 | `COMPILE_OK`（0 error） |
| EditMode | **656/656**（W17 全量 schema/前置/软前置/物品/资源/船型/武器/配方门禁；W21 海军实体+存档；W22 参考游戏统一规划） |
| PlayMode 无图形 | **9/9** |
| PlayMode 有图形 | **9/9** |
| SaveVersion | **16**（未批准不改） |
| 程序集 | 15 asmdef；DAG 无环；门禁全绿 |
| 代码改动 | 最近若干轮为**设计文档**，未改运行时代码 |
| 工作区 | 大量未提交改动；**不要 commit/checkout/stash** |

---

## 2. "完成"的定义（DoD）

一项工作只有同时满足以下条件才算完成：

1. `mc-compile.ps1` → `COMPILE_OK`；
2. 全量 EditMode 0 failed / 0 skipped（显式平台豁免除外）；
3. PlayMode 无图形 + 有图形均全绿；
4. `AsmdefArchitectureGateTests`、`AssetLayoutGateTests`、`TmpAssetIntegrityTests`、`CompletionGateTests`、`DatasetIntegrityTests` 全绿；
5. 不新增未登记跨层引用；`SaveVersion`/序列化变更单独批准并有迁移测试；
6. 文档同步：本计划 + `ARCHITECTURE_RULES.md` + 对应设计/迁移 README；
7. 无断 GUID、无重复字体、无被禁顶层目录、无临时 TODO/占位（明确边界除外）。

---

## 3. 架构与文件规则（摘要，详细见 ARCHITECTURE_RULES.md）

- **程序集方向**：Core ← Content ← {Laws, Faith, Culture} ← World ← Simulation ← {Rendering, Infrastructure} ← {App.Contracts, App} ← UI；UI **禁止** Simulation/Laws/Faith/Culture。
- **新功能先定位 owner**：Core 契约/DTO；Content 定义/内容；Laws/Faith/Culture 域模型；World 世界数据/算法；Simulation 运行时系统；Infrastructure 存档/音频；Rendering 视图；App 组合根；UI 面板；Tests 测试。
- **不新建平行系统**：接现有创建/调度/存档/UI 入口；不复制内容；不硬编码内容；不直接序列化运行时对象。
- **内容**：JSON 在 `StreamingAssets/Base/<系统>/`；Mod 覆盖；`_generated` 仅 `redistributable` 且有 lock/license/header；生成物 `DatasetIntegrityTests` 强制。
- **物产/器物/能力分流**：材料→`GoodsDef`；器物/设施→`EquipmentDef`/`BuildingDef`/`SettlementBlock`；宽泛活动→Capability/Subdomain；具体能力→`Innovation`。
- **存档**：`SaveVersion` 未批准不改；新字段默认值 + 显式迁移；DTO/Adapter；`JsonUtility` 不认 `[FormerlySerializedAs]`。
- **每批流程**：读现有事实（文件:行）→ 定接口/迁移映射 → 小步补丁 → 编译 → 新测试 → 全量测试 → 只读审计 → 文档。

---

## 4. 统一决策（2026-11 各轮汇总）

### 4.1 认同 / 语言 / 文化

- 认同载体只有 **族群（ethnos）** 与 **大起源（heritage）**；文化是**嫁接**在它们上的传统组合（语言、习俗、生产、仪式、政治传统…），可改变。
- 机制到 **氏族→族群** 为止；不引入"语族"作为认同层；`LanguageFamilyNode` 只服务语言学/命名。
- **文化分离**：距离/阻隔 + 地理 → 物产/生计 → 传统项变化；`CultureSimilarity` 只是派生度量；**必须做 200–500 年参数实验**验证（无阻隔不分离/有阻隔+生计变化分离/通婚收敛/参数稳定）。
- **语言分离**：音系/词库/形态距离 + 接触 + 时间，可算法化、可测试。
- **认同分离**：与政治整合/政权分裂有关；分裂持续时间 > 此前统一时间（或整合度跌破阈值 + 法律/精英/制度分道）才分离；需要 `IdentityHistory`。
- 语言差异意识：`P(认成另一种语言)=f(接触, 互通度, 边界, 标准化, 时间)`；先认知后命名；有自称/他称/通用名。
- 命名：音系/音节/词库/语义/形态/历史禁忌；生成**词（lexeme）**不是词根；词可复合；语义分组（抽象/物体/地点/方位/人物/动作）；人名/地名/省名/头衔/族群名/语言名各有合法组合；法兰克例：政治联盟 → "自由人"词源/他称 → 族群认同 → 法兰克人之地。
- 民族主义门槛：**国界线 + 机械印刷出版 + 普遍公民权**；`民族国家` 是 `PolityForm`，不是革新节点。
- 公民权历史类型（城邦/罗马/城市/臣民）是 **内容变体/身份制度预设**，不是革新；革新层只保留 `公民身份/有限公民权/普遍公民权/归化/国籍登记`。

### 4.2 革新（当前大头）

- 现有 **185 条**；先做**去重/对照**（见 [INNOVATION_DEDUPE_AUDIT.md](../design/INNOVATION_DEDUPE_AUDIT.md)），有同义/近义一律复用/改名/拆分，不新增重复 ID。
- **革新 = 可迁移的一般概念**（Idea/Technique/Institution/Practice/Knowledge）；历史专名（罗马公民权/雅典民主/科举/郡县/行省）→ `InnovationVariant`，不是节点。
- **回溯性分析标签**（王国/帝国/城邦/封建/朝贡体系/官僚帝国/神权政治/共和/君主）→ `PolityForm` 描述维度，不是革新；**现代具体制度/技术/思想允许**（铁路/电报/职业文官制/普遍公民权/民族主义等），名称不加"现代/古代"前缀。
- **软前置**：`associatedGoods` 的物理属性推导能力阈值（如熔点 → 最高温度），由已解锁各项的总能力满足；不是材料阶梯加成，也不要求特定前置科技（如青铜）或特定燃料（如木炭）。
- **禁止伞形/过程词**：`官僚制度/印刷术/公民权/边界概念/中央集权/社会/文化/宗教/知识` 不能当条目；`中央集权化` 是过程/指标。
- **分类**：14 Domain（生存与食物/材料与制作/能源与公用/建筑与聚落/交通与通信/交换与金融/治理与行政/法律与司法/军事与战争/知识与健康/信仰与仪式/社会与身份/艺术与娱乐/外部关系）× Subdomain × Kind（Technique/Institution/Practice/Idea/Knowledge）× Scale。
- **粒度**：先分"物产/器物/宽泛活动/具体能力"；物产进 `GoodsDef`，器物/设施进 `EquipmentDef`/`BuildingDef`/`SettlementBlock`，宽泛活动拆到方法/资源层；只有改变模拟变量的具体能力才做革新。
- **首批 150–250 条有模拟效果的条目**，不追求百科数量；旧 185 条保留 ID + `legacyIds` 迁移；新 ID 分块（1100+ 政治/行政、1200+ 法律/司法、1300+ 军事、1400+ 经济、1500+ 社会/文化、1600+ 技术、1700+ 区域、1800+ 现代具体概念），避开 1025。
- **已有条目复用示例**：`808 硬质道路`、`801 拱券技术`、`106 水磨`、`200 陶器制作`、`205 造纸术`、`206 印刷术`、`935 玻璃制造`、`936 瓷器`、`207 火药`、`307 火器`、`305 攻城器械`、`304 弩`、`302 骑兵战术`、`924 马镫`、`1013 战车`、`1007 撞角战术`、`823 驿传邮驿`、`403 罗盘`、`990 星盘`、`401 帆船`、`807 轮车`、`800 夯土筑城`、`204 纺织机`、`1009 皮革加工`、`934/933 盐`、`802/803 采矿`、`963 石叶`、`966 岩画`、`907 弓箭`、`913 酿酒`、`976 漆器`、`974 养蚕`、`977 玉石`、`988 机械钟`、`989 风车`。
- **道路**：`808` 拆 `夯土道路/碎石路面/铺石路面/木栈道/堤道`；`道路网/驿道` 是制度/网络，不混。
- **桥梁**：按 `木梁桥/石拱桥/浮桥/吊桥/栈桥/桁架桥`，不写"桥梁建造"。
- **材料**：石灰石/大理石/混凝土/砖/瓦/水泥是物产；革新是 `石灰烧制/石灰砂浆配制/混凝土配制/大理石开采与切割/制砖/制瓦/水泥烧制`。
- **战车**：`1013` 已是**双马战车**（单位 210）；四马新增单位 `211 四马战车兵` + `四马战车` 条目；不写"战车制造"。
- **酿酒**：`913` 是**谷物酒/米酒**；做则 `谷物酿造` + 新增 `酒` 物产 + 配方 `谷物/粮食→酒` + 效果；不做泛"酿造"。
- **城墙**：见 §4.3；不写"城墙建造"。

### 4.3 城防（已定稿）

```text
聚落
 └─ 城墙块 1..N（WallRole：Primary/Inner/Outer/Extension/Palace）
      ├─ FortificationTier 1..5
      ├─ WallMaterial：Palisade / Earth / Stone / Brick / Composite
      ├─ WallMaterialGrade 1..3
      ├─ WallLayout：Straight / Ring / Polygonal / MultiCircuit
      ├─ wallLayerCount 1..3
      └─ WallSegment 1..M（横向段：length/condition/塔楼/城门/护城河）
```

- `FortificationLevel 0–15 = (tier - 1) * 3 + materialGrade`；tier 1–5 = 简易围护/连续墙体/设防墙体/棱堡/多重防线；
- 一个城墙块最多 3 层；一个聚落可有多个城墙块；"段"是城墙块内部横向段，不是多个城墙块；
- `WallLevel` 改名 `FortificationLevel`；旧档映射：None=0、Palisade=1、EarthenRampart=4、StoneWall=5、FortifiedWall=7、StarFortification=10、MegaFortification=13；
- **多重城墙不单独做革新**：`MultiCircuit` 布局 + 多个城墙块 + tier 5（13–15）+ 工程能力/资源门槛；
- 现有代码：`GetBlocks/GetPrimaryWall/WallRole` 支持多城墙块，但 `WallDefense` 只算一个、无 `WallSegment` 列表、`wallLayerCount` 无上限、`SettlementTypologySystem` 自动抬墙；实现时补齐。

### 4.4 法律 / 法令

- 范围**缩窄**：只做对现有模拟有实际作用的骨架（政权制度/继承/君权/中央地方/军事/税收/宗教/身份/土地/贸易/度量衡/边境/少量刑法与司法）。
- **不做**：民法总则/物权/债权细节、刑法分则全部罪名、国际组织法、组织法——这些需要组织/国际组织等新系统，暂不引入。
- 法律条目仍按骨架接革新（成文法/法典/法院/上诉/陪审/辩护/证据/司法独立/法律职业/商法/契约/财产/土地登记/条约/战争/引渡/移民），不往细里展开。
- 现有 32 法/2 法令、效果仅 5 字段、`requiredInnovationIds` 全空；后续补 schema/门槛/少量内容。

### 4.5 物产 / 阶层 / 环境 / 货币 / 灾害

- 物产是具体 goods（粮食/肉/鱼/盐/谷物/棉/马/木/石/矿/金属/武器/甲/丝/香料/奴隶…），有物理属性、产地、加工链、储藏/腐败；**不是抽象"食物"**。
- 阶层需求：5 类 × 8 维（Subsistence/Security/TaxBurden/PoliticalAccess/EconomicOpportunity/InstitutionalRecognition/Legitimacy/Privilege）；**不做生理条件**。
- 环境：板块/气候/洋流/水文/灾害可切换生成期 vs 游戏内模拟；海平面/海侵/盐碱化/沙漠化/人类影响；灾害合并/登陆衰减；地形先于气候。
- 货币：地区化/具体化（贝壳/珠子/谷物/硬币/纸币/信用）、发行者链、不同政权同材质不同名、工资锚、兑换/拒收/灭亡折价；升级现有单名 `CurrencySystem`。
- 头衔：`TitleInstance` + `IPolityRef` 政治实体接口 + 可编辑绑定 + AI 接合；头衔≠地理。
- 性能：学 civ-atlas 的计算组织（typed arrays/事件堆/检查点/计算渲染分离/确定性），不学 Earth UI。

### 4.6 Data Forge

- 已接 `_generated/language-families.json` + `datasets.lock.json` + `LICENSES.md` + `DatasetIntegrityTests`。
- 待接：`conlang-inventories`（音系库存）、`core-concept-lists`（语义组/互通度）、`biome-climate-map`、`production-calibration`、`climate-calibration`、`sea-level`、`climate-anomalies`；设计期报告（PHOIBLE/WALS/Grambank/UD/D-PLACE/Seshat）只做校验/文档，不进包。
- 游戏内 ID 自建；Forge 外部 ID 只作锚字段；`_generated` 只允许 `redistributable`。

---

## 5. 工作包全景（WBS）

| ID | 工作包 | 状态 | 交付/验收 |
|---|---|---|---|
| W1 | 程序集与分层 | ✅ 已收口 | `AsmdefArchitectureGateTests` 全绿 |
| W2 | UI/Simulation 解耦 | ✅ | UI 无 Simulation 家族 |
| W3 | 世界组合根/服务层 | ✅ | `WorldServicesTests` |
| W4 | Content 收敛 | ✅ | Content 边界/namespace |
| W5 | UI 契约化 | ✅ | 无"契约未提供" |
| W6 | 字体/美术资产 | ✅ | 资产/TMP 门禁 |
| W7 | 音频资产 | ✅ | `UiSfxTests`/门禁 |
| W8 | Data/配置资产 | ✅ | 场景 GUID/门禁 |
| W9 | 存档/读档 | ✅ | RoundTrip/Rebind 测试 |
| W10 | 法律内容管线 | ✅ | JSON 唯一来源/回退 |
| W11 | 测试/门禁体系 | ✅ | 全量 + CompletionGate |
| W12 | 文档同步 | 🔄 持续 | 本计划/架构/设计文档一致 |
| W13 | 认知/头衔 | 📝 设计 | 头衔运行时/IPolityRef/AI 接合/官员因子 |
| W13b | 语言/命名/族群 | 📝 设计 | 音系/语义命名/语言意识/文化-认同分离 |
| W13c | 民族主义门槛 | 🔄 1017 已落地 | `国界线+机械印刷出版+普遍公民权`；`NationalismGateTests`；1018 已移除；旧档未知 ID 兼容待验证 |
| W14 | 制度/货币/灾害 | 📝 设计 | UnlockRule/多货币/风暴生命周期 |
| W16 | Data Forge 接入 | 🔄 部分 | 已接语言谱系；其余按 §4.6 |
| W17 | 革新精确化 | ✅ 结构迁移完成 | 187 条全部 schema=2；300/301 已移除，青铜/铁武器直接挂 201/202；144 条 `unlocks` 仍只有 capability 标签；43 条已有具体引用 |
| W18 | 法律/法令 | 📝 设计缩窄 | 只做模拟有意义骨架；法律门槛 |
| W19 | **城防系统** | ✅ 2026-11-03 | 0–15/材质+工艺/横向段/层/多块聚合/旧档映射；全量 EditMode 612/612 |
| W20 | 统一文档/规则 | ✅ 本轮 | 本计划 + ARCHITECTURE_RULES + 备份 |

> 图例：✅ 已完成；🔄 持续/部分；📝 设计完成待实现；🚧 下一切片。

---

## 6. 实施顺序（一次做完整，不"滴灌"）

### Phase 0（本轮，已完成）

- 统一规划（本文）；
- 架构规则（ARCHITECTURE_RULES）；
- 旧规划快照 + sha256 + zip。

### Phase 1（✅ 2026-11-03）W19 城防系统

**目标**：把已定稿的城墙模型接进现有聚落/块系统，不新增平行系统。

**实现结果（2026-11-03）**：
- 新增 `WallMaterial` / `WallSegment` / `FortificationLevels`（0–15 = tier×3+grade）；
- `SettlementBlock` 增加材质/工艺/横向段，`wallLayerCount` clamp 1–3；
- `WallDefense` 聚合所有城墙块 + 所有段 + 材质/工艺/等级/层数/护城河/守军；
- `SettlementData` 用 `fortificationLevel`，读档后从块回填；拆除/夷平关闭所有墙块；
- 旧 `WallLevel` 按 0/1/4/5/7/10/13 映射；`SettlementBlockSaveData` 增加 `wallDataVersion` 兼容旧档；
- 新增 `WallFortificationTests`（8 条），全量 EditMode **606/606**，PlayMode 9/9。

1. 枚举/字段：
   - `WallMaterial`（Palisade/Earth/Stone/Brick/Composite）、`WallMaterialGrade`（1–3）、`FortificationLevel`（0–15）；
   - `SettlementBlock`：`wallMaterial`、`wallMaterialGrade`、`List<WallSegment> wallSegments`；`wallLayerCount` clamp 1–3；
   - `WallSegment`：`length/condition/towerCount/hasGate/moatLevel`（按现有字段最小集）。
2. 旧档映射：`WallLevel` → `FortificationLevel`（0/1/4/5/7/10/13），显式迁移。
3. 聚合：`WallDefense` 用 `GetBlocks(Wall)` 聚合所有块 + 所有段 + 材质/工艺/tier/塔楼/护城河/层数；不再只看一个块。
4. 创建/升级：`SettlementTypologySystem` 不再按聚落等级自动抬墙；改成条件式（资源/工程能力/革新/等级上限）。
5. 测试：多块聚合、段总长、层数 clamp、0–15/旧档映射、材质/工艺对防御影响；现有测试不回归。
6. 存档：新增字段默认值；`SaveVersion` 若需变更，单独批准并写迁移测试。

### Phase 2（🔄 2026-11-03 进行中）W17 革新 I0（schema + 去重迁移）

**已完成（2026-11-03）**：
- `InnovationDef` 新增 `contentSchemaVersion/termEn/definition/scope/kind/category/sources/prerequisiteRationale/unlocks/legacyIds/variantOf/regions`；新增嵌套前置 `prerequisiteTree`（All/Any/Leaf，可嵌套）与结构化 `unlockRefs`（Kind+id/key）；旧扁平前置字段已移除；
- 新增 `InnovationKind`、`InnovationPrerequisiteRationale`；
- 新增精确条目 `1101 国界线`、`1110 机械印刷出版`、`1103 普遍公民权`；`1017 民族主义` 改为三者全部必要；
- 新增 `InnovationSchemaTests`（4 条）与 `NationalismGateTests`（2 条），全量 EditMode **612/612**；
- I0b 已全部结构迁移（189 条 schema=2；42 条精修 + 147 条结构补齐）：`502 单一制`、`503 科层制`、`504 考试选官`、`205 纤维打浆`、`206 雕版印刷`、`306 设防城堡`、`800 夯土版筑`、`801 拱券结构`、`913 谷物酿造`、`958 领土主权`、`1013 双马战车`、`1017 民族主义`、`1101 国界线`、`1103 普遍公民权`、`1110 机械印刷出版`、`1601 抄纸成型`、`822 文书行政`、`940 档案典藏`、`941 上计`、`942 考课`、`944 职官俸禄`、`1016 报纸`、`106 水力磨粉`、`989 风力磨粉`、`952 领主庄园`、`953 庄园法庭`、`954 公地放牧`、`955 磨坊领主权`、`956 劳役地租`、`961 包税`、`995 军队编制`、`998 军功爵`、`1002 常备军`、`1003 募兵`、`1006 具装甲骑`、`207 黑火药配制`、`304 弩机制作`、`305 配重式抛射机构`、`307 火绳枪机`、`202 块炼铁冶炼`、`203 渗碳炼钢`、`308 射石炮铸造`；
- 147 条结构补齐条目：已补英文名/定义/类型/前置理由；`unlocks` 暂为 capability 标签，具体解锁引用待补；`1018 民族国家` 已从革新目录移除（作为 PolityForm）。

**待续**：147 条 legacy 逐条补 definition/去重/变体/政体标签迁移；I2 按域扩条目。

1. Schema：`InnovationKind/Category/Domain/Subdomain`、`termEn/definition/scope/sources/prerequisiteRationale/unlocks/legacyIds/variantOf`；
2. 旧 185 条：保留 ID、补 definition、伞形词降分类、历史专名转变体、政体标签移出、与现有条目去重；
3. 校验：`InnovationSchemaTests/PrecisionTests/PrerequisiteTests/NationalismGateTests/MigrationTests`；
4. 不一次性把 150–250 条全灌入；先完成迁移框架 + 政治/民族主义簇（I1），再按域扩（I2）。

### Phase 3 W18 法律（缩窄版）

- 只补模拟有意义骨架：法律类型/域/来源/管辖/权利、少量法院/程序、法律革新门槛；
- 不做民刑细节/组织/国际组织。

### Phase 4 W13b/W13c 语言/命名/认同 + W13 头衔

- 音系库存/语义命名/语言意识/`RegionNameRecord`；
- `IdentityHistory`/文化分离实验；
- `IPolityRef`/`TitleInstance`/绑定/AI 接合。

### Phase 5 W14 货币/灾害 + W16 Data Forge + 环境/物产

- 多货币/兑换；风暴生命周期；Data Forge 其余产物；环境 E0–E7 按专项计划。

---

## 7. 验收门禁（每批）

```text
powershell.exe -NoProfile -ExecutionPolicy Bypass -File 'E:\TEMP\mc-compile.ps1' -LogName <name>
E:\TEMP\mc-test-editmode.ps1
E:\TEMP\mc-test-playmode.ps1
E:\TEMP\mc-test-playmode-gfx.ps1
```

- 结果 XML：`MyriadCreation/Logs/{editmode,playmode,playmode-gfx}-results.xml`；
- 新增测试必须进入对应 EditMode/PlayMode 程序集；
- 架构/资产/TMP/完成/数据集门禁全绿；
- 报告：改了什么、未改什么、测试数字、边界/风险、未提交状态。

---

## 8. 明确边界 / 暂不做

1. `StreamingAssets/{Base,Mods}` 目录名不改。
2. `Art/TextMesh Pro` 留在 Art。
3. Content 4 个 host 文件留在 Simulation。
4. 音频仍在 `Assets/Audio/Resources/{UI,Music}`。
5. 不建 `ThirdParty` 目录。
6. `SaveVersion` 未批准不改；未批准不改场景 GUID/字体引用。
7. 法律不做民刑细节/组织/国际组织；组织/国际组织若要做，先单独立项设计。
8. 革新不做百科式全量；先 150–250 条有模拟效果的能力；物产/器物另表。
9. 不引入"语族"作为认同层；历史专名不做通用革新。
10. 不做 commit/checkout/stash；工作区保持可审查。

---

## 9. 备份与追溯

- 快照：`docs/archive/2026-11-03_pre-unified-plan-backup/`；
- 压缩：`docs/archive/2026-11-03_pre-unified-plan-backup.zip`；
- 清单：快照内 `README.md`（46 文件 + 字节数 + sha256）；
- 恢复：按清单相对路径复制回仓库根（覆盖前确认）；
- 旧计划：`MASTER_COMPLETION_PLAN.md` 仅作 W1–W12 历史记录；当前以本文件为准。

---

## 10. 变更记录

| 日期 | 变更 |
|---|---|
| 2026-11-03 | 建立统一总计划 V2 + 架构规则 + 旧计划备份；锁定城防 0–15/材质/段/层/多块；革新去重与粒度规则；法律缩窄；确认后续按 Phase 1→5 执行。 |
