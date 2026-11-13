# 认知 / 命名 / 头衔 / 货币 / 灾害：从参考项目提炼的设计结论

> 来源：用户对 Earth、*Things That Happen on the Map*、CK3、civ-atlas 的比较与判断；
> 结合本仓库现有 `TitleDef/TitleDefinition/TitleManager/AdminDivision`、`CurrencySystem`、
> `PlaceNameGenerator`、`SocialClassAvailability/ClassEmergenceEvents`、`InnovationTree` 的代码事实。
> 本文是**需求与模型设计**，不改代码；落地项在 §7 列清单。
> 语言/音节/语义与族群分化的细化设计见 [LANGUAGE_NAMING_IDENTITY.md](LANGUAGE_NAMING_IDENTITY.md)。

---

## 1. 命名：认知驱动 + 通用名，双层命名

### 1.1 Earth 的优点与缺口

- 优点：**区域一开始没有名字**；某个族群接触到某块区域，才用自己的语言/命名规则给它命名。
  名字是"认知"的产物，不是地图生成时就写死的。
- 缺口：**没有通用名/共同名**。地图上总需要一个所有文化都能指认的标签，否则 UI、外交、编年史无法引用。

### 1.2 我们的设计：两层名字 + 认知事件

| 层 | 作用 | 谁写 |
|---|---|---|
| **通用名（canonical / common name）** | 地图标签、外交文书、编年史默认引用；一区域一个 | 玩家/系统：首次命名、征服者命名、通用语转写、历史沿革；可被玩家改名 |
| **族群名（per-group name / endonym & exonym）** | 每个文化/政体对同一区域的称呼（自称、他称、转写、古名） | 该族群接触后按自身语言/命名规则生成；AI 自动，也可编辑 |

数据模型（Core/Simulation）：

```csharp
RegionNameRecord {
    int regionId;
    string canonicalName;            // 通用名（地图标签）
    string canonicalSource;          // system/player/conquest/colonial/restored
    List<GroupNameEntry> names;      // 每族群的名字
    List<NameChange> history;        // 改名历史（年份/族群/旧名/新名/原因）
}
GroupNameEntry {
    int groupId;                     // 文化/民族/政体/宗教群体
    string name;
    string script;                   // 文字/转写系统
    NameOrigin origin;               // Native/Exonym/Translation/Colonial/Restored
    int firstContactYear;
    float confidence;                // 认知程度（传闻/勘测/定居）
    bool usedByOwner;                // 当前统治者是否用这个名字
}
```

规则：
1. **生成期不给区域起名**（只有地形/群系/坐标）；物理地物可以先有描述性别名（"大河""北山"），但不是正式名。
2. 命名触发：探索/接触、建城/定居、政体宣称、地图测绘革新、征服/殖民、传教、贸易路线。
3. 命名来源：该族群的语言/命名规则（我们已有 `PlaceNameGenerator`/`CultureLineage`/`NameGenerator`）；转写/意译规则可内容化。
4. 通用名规则：默认取"最先建立稳定认知的族群名"或"玩家指定"；当主导政体/通用语变化时可提议改名（不强制）；无通用名时 UI 显示"未命名区域（坐标）"。
4b. **词形必须来自语言生成**（音系/音节模板/词库/形态/语义），而不是随机拼写；语言差异意识触发名字生成，见 [LANGUAGE_NAMING_IDENTITY.md](LANGUAGE_NAMING_IDENTITY.md)。
5. UI 标签模式：通用名 / 当前统治者命名 / 本地（按观察者文化）/ 全部名字（tooltip 列历史）；地图迷雾/未接触区域不显示族群名。
6. AI 行为：AI 政体在占领/探索后调用命名命令；AI 也会把已有名字翻译/转写，而不是全部重命名。

> 与现有代码：`RealmData.realmName` 是单名；`PlaceNameGenerator` 是生成期一次性名字；需要新增 `RegionNameRecord` 与 `IRegionNameQuery/Command`，UI 地图标签改为读"通用名 + 观察者族群名"。

---

## 2. 头衔：与地理分离、可绑定、可被 AI 接上

### 2.1 用户判断（作为设计前提）

- 所有参考项目（包括 CK3）都没真正讲清"头衔是什么"；CK3 把**头衔与地理区域完全绑定**，过于固化。
- 头衔与地理**有关系，但不是同一样东西**。
- 我们的创新：**每个头衔都有可控的绑定字段（局内可改），AI 会试图给它们接上**。
- 缺口：**没有把"政权/政治实体本身"作为接口字段**（头衔应该能绑定到 polity 实体，而不只是 realm/地理）。

### 2.2 现有代码事实

- `TitleDef`（Content）：`titleId/kind/rank/weight/semantic/authorities/modifiers/cultureId`——只是**定义**。
- `TitleDefinition`（Simulation.Definitions）：`superiorTitle/tier/eligibility/appellationId`——仍是定义/层级。
- `TitleManager`：静态注册表、按 kind/tier/superior 查询、资格过滤。
- `AdminDivision.titleId` + `holderCharacterId`：**地理区划直接挂 titleId**——正是要拆开的"头衔=地理"耦合。
- 缺少：**头衔的运行时实例**、**polity 接口字段**、**可编辑绑定**、**AI 绑定行为**、**宣称/多重持有/个人联合**。

### 2.3 目标模型

```csharp
// 定义（内容）：头衔"蓝图"
TitleBlueprint {
    string titleId;
    string kind;                 // 王/公爵/总督/祭司/军事统帅/经济总管…
    int rank;
    List<string> authorities;    // 能做什么（行政/军事/司法/经济/宗教）
    List<OfficeFactor> factors;  // 官员能力因子（见 §3）
    List<string> modifiers;
    int cultureId;
    EligibilityRule eligibility;
    string appellationId;
}

// 运行时实例：真正被持有/被绑定的一次头衔
TitleInstance {
    int instanceId;
    string blueprintId;
    int polityId;                 // ★ 政治实体（IPolityRef）——当前缺失的接口字段
    int holderCharacterId;        // -1 = 虚悬/空位
    TitleBinding binding;
    List<int> deJureRegionIds;    // 法理辖区（可为空 = 名义头衔）
    List<int> deFactoRegionIds;   // 实际控制
    List<int> claims;             // 宣称
    int createdYear;
    List<TitleChange> history;
}

TitleBinding {
    BindingMode mode;             // Personal/Territorial/Office/Elective/Claim/Union
    int boundPolityId;            // 绑定政权
    int boundRegionId;            // 绑定法理区（可 -1）
    bool editableByPlayer;        // 局内能否改绑
    float legitimacyCost;         // 改绑/强占的合法性代价
}
```

规则：
1. **头衔 ≠ 地理**：头衔可以没有领地（名义头衔/titular）、可以同时宣称多个区域、同一区域可以有多重头衔/宣称；地理只是**绑定字段之一**。
2. **绑定可编辑**：玩家/AI 可以改绑政治实体、辖区、持有人；改绑有合法性/稳定度/外交代价。
3. **AI 会尝试接上**：新头衔产生、统治者死亡、征服、联姻、选举时，AI 按利益/合法性/法理/实力给头衔找 polity/持有人；失败则虚悬。
4. **个人联合**：同一持有人可持多 polity 头衔；同一 polity 可有多头衔层级；头衔可在政体分裂/合并时拆分/合并。
5. **接口字段**：`IPolityRef`（政治实体引用：id/类型/名称/合法性/政体）进入 Core.Contracts；`TitleInstance.polityId` 用它，而不是 `realmId` 裸 int；UI/AI 通过 `ITitleQuery/ITitleCommand` 读写绑定。

> 落地：`TitleCatalog` 保持定义；新增 `TitleRuntimeManager`（实例/绑定/历史）、`PolityRegistry`/`IPolityRef`；`AdminDivision.titleId` 改为"区划的可选头衔引用"，不再作为头衔唯一归属；UI 政府面板加"头衔绑定"编辑。

---

## 3. 官员能力因子（借鉴 *Things That Happen on the Map* 的模块化）

- 它的可借鉴点：**官员关系可编辑**；**一个官员能干什么由一个"因子"决定**（如持有"控制经济"因子 = 经济方面）。
- 我们的 `TitleDef.authorities/modifiers`、`OfficeTitleEntry` 已经有雏形，但缺少**统一因子模型**与**可编辑字段**。
- 设计：

```csharp
OfficeFactor {
    string factorId;          // economy/control/military/judiciary/religion/diplomacy/…
    float weight;             // 该头衔在此因子上的权重/权限
    List<string> capabilities; // 可执行的命令：征税/调兵/立法/任命/审判/传教…
    List<string> constraints;  // 不能做什么/需要条件
}
```

- 因子类型：`ControlEconomy`、`ControlMilitary`、`ControlJudiciary`、`ControlReligion`、`ControlDiplomacy`、`ControlAdministration`、`ControlCulture`…
- `TitleBlueprint.factors` 决定该头衔能发起的命令集合；`OfficeTitleEntry` 绑定具体官员；UI 可编辑加权/启用因子（受法律/政体限制）。
- 与现有 `Law/OfficeTitleEntry` 对接：法令可限制某因子；政体可分配因子。

---

## 4. 革新 → 阶层/身份/职业 → 文化/宗教：我们已有的"小绑定"要显式化

用户判断：很多项目不如我们，因为我们做了大量小绑定：**完成某些革新才能产生某个阶级/阶层/身份/职业；产物推动革新；革新推动文化、宗教**；不是完全地理决定论，但关联性很强。这是本项目核心创新，应显式建模，而不是散落在代码里。

### 4.1 数据模型

```csharp
UnlockRule {
    string id;
    UnlockTargetKind target;      // SocialClass/Identity/Occupation/CultureTrait/Doctrine/Currency/Building/Title
    string targetId;
    List<int> requiredInnovations; // 革新门槛
    List<GoodsRequirement> requiredGoods; // 物产可得性/属性阈值（而非抽象"粮食"）
    List<string> requiredLaws;
    List<string> requiredBuildings;
    float minDevelopment;          // 可选
    float baseChance;              // 可选（历史偶然性）
    List<string> tags;             // 农业/冶金/航海/军事/宗教…
}
```

- **产物 → 革新**：革新研究需要具体物产（如青铜要铜锡；铁器要铁；马要牧场/马源）；不是"粮食数值"。
- **革新 → 阶层/职业**：例如重犁+轮作 → 自耕农/佃农分层；铸币/银行 → 商人/金融阶层；成文法 → 官僚/法官；一神教 → 祭司阶层。
- **革新 → 文化/宗教**：文化特质、教义解锁（已有 `CultureStage`/`DoctrinePool`，补 `UnlockRule` 关联）。
- **非地理决定论**：环境提供"物产可得性/成本"，革新与制度是中间层；同一环境可走不同路径。

### 4.2 落地

- Content：`Unlocks.json`（规则表），`Goods`/`Innovation`/`CultureTrait`/`Doctrine` 引用；
- Simulation：`UnlockRegistry`（查询/校验）、`ClassEmergenceEvents`/`SocialClassAvailability` 改读规则；`InnovationTree` 研究条件读物产需求；
- 测试：给定 innovation/goods 组合，断言阶层/职业/文化特质/教义的可用性；反向断言缺物产时不可用。

> 这是"我们最大创新"的代码化表达，建议在 MASTER 计划新增 **W13 认知与制度** 工作流。

### 4.3 术语与民族主义门槛（用户澄清）

- 我们说 **族群**（ethnos/people）与 **大起源**（heritage）作为认同载体；文化**嫁接**在它们上（`CultureData.ethnicGroupId/heritageId`，可改变）。
- **不称"民族"**；"民族（nation）"是近代概念。民族主义需要**三者缺一不可**：边界概念固定、先进印刷、公民权；之后才有**民族国家**。
- 现有数据缺口（`Innovations.json`）：`1017 民族主义` 前置只有 `[1016 报纸, 600 文字]`；`1018 民族国家` 前置 `[1017, 503]`；缺"边界概念/公民权"两个革新，也没有"全部必要"的门槛校验。需补 1019/1020 并改 1017 前置，补门槛测试。
- 文化分离 vs 认同分离：见 [LANGUAGE_NAMING_IDENTITY.md](LANGUAGE_NAMING_IDENTITY.md) §4.2b——文化分离落在**传统分类/因子**上（地理→物产/生计→传统），认同分离主要看**政治整合/政权分裂**（分裂时长 > 统一时长等），不能用一个抽象相似度代替。

---

## 5. 货币：地区的、具体的、可兑换的（参考 Earth 的实践）

用户判断：Earth 最值得借鉴的是**货币的具体实践（代码层面）**——不同地区用不同货币（贝壳、珍珠、盐、金属），不同政权货币名不同（即使同材质）；只是参考。

### 5.1 现有代码事实

`CurrencySystem`（每政权一个）：`currentStage`（以物易物/金银/铸币/纸币）、单 `currencyName`、`goldReserve/silverReserve/coinPurity/paperMoneyInCirculation/inflationRate/vault`。缺：多货币、区域差异、兑换、货币名随文化/政权变化、材质关联物产。

### 5.2 设计（对齐 Earth `walkeco.js` 的具体做法）

```csharp
CurrencyDef {
    string currencyId;
    string nameByCulture;         // 不同文化/语言的货币名（贝壳/贝币、珍珠、盐引…）
    int issuerPolityId;           // -1 = 自然货币
    int materialGoodsId;          // 关联具体物产（贝壳/珍珠/盐/铜/银/金）
    CurrencyForm form;            // Commodity/Coin/Paper/Credit
    float purity;                 // 成色
    float nominalValue;           // 面值/重量
    float acceptanceRadius;       // 接受范围（区域/贸易圈）
    List<int> acceptedByPolities; // 哪些政权接受
}

CurrencyWallet { int ownerId; int currencyId; float amount; } // 政权/区域/角色
ExchangeRate { int fromId; int toId; float rate; float spread; int year; } // 市场/官方
```

规则：
- **货币形式按时代+地理**：沿海部落贝壳、内陆部落珠子、早期古典谷物计量、古典硬币、工业纸币、高级信用——同 Earth `ecoForm`。
- **发行者链**：附庸用宗主货币（最多 4 层），同盟不共币；灭亡政权的货币只按金属价值流通（折价）。
- **工资锚**：以"本地货币的日工资"为价值单位，价格=工资天数；货币价值受稳定度/战争折价；兑换有手续费。
- 同一材质不同政权可有不同货币名（如"贝币/朋"）；名字由文化/政体命名规则生成（Earth 用 uid+seed 音节+变格）；
- 不同地区接受度不同：超出接受半径要兑换/折价/拒收；
- 兑换率由材质价值、成色、供需、政策（管制/官定）决定；
- 货币是具体物产的一种"凭证"：贝壳/珍珠/盐可直接作为物产库存，也可铸币；
- 税/军饷/贸易用具体货币结算（不要抽象"钱"）。

> 落地：`CurrencySystem` 拆为 `CurrencyDef`（内容）+ `CurrencyWallet`（政权/区域）+ `CurrencyExchangeSystem`（兑换/接受度）；与 `TradeCenter/SettlementMarket` 的价格用"本地货币"计价；UI 显示货币名/材质/成色。

---

## 6. 气象/灾害：从 Earth 的不足中学习

用户观察（Earth）：
- 飓风**不会合并**（多飓风相遇不合并；现实概率小但存在）；
- 飓风**一上岸就消失**（现实会继续深入、变性、带来暴雨）；
- 应先**生成地形 → 再按地形算气候**（我们计划已如此）；
- 北美飓风多确实影响原住民发展，但**更大的原因是地表矿产和大型牲畜太少**（环境不是单一决定论）。

设计修正：
1. **风暴生命周期**：暖海生成（SST>26°C、低风切变）→ 引导气流移动 → 增强/眼墙替换 → 登陆后**按摩擦/水汽供给衰减而非瞬间消失**，可深入内陆、变性为温带气旋、带来暴雨/洪水；离开暖海后缓慢减弱。
2. **合并**：两个热带气旋中心距离 < 合并半径且环流相容时，按概率/强度加权合并（Fujiwhara 效应）；小概率但存在，不能完全不支持。
3. **地形作用**：迎风坡降水增强、背风减弱、山脉减弱风场；沿海风暴潮与海平面耦合。
4. **灾害蔓延**：火灾随风/植被/湿度；蝗虫随植被/风；瘟疫随贸易路线/城市密度；不要只做"全图随机"。
5. **不做地理决定论**：飓风是风险因子；矿产/大型牲畜/作物是更直接的资源约束；AI/文明可适应（建筑、迁徙、保险/仓储）。
6. **性能**：civ-atlas 网页不卡的关键是**计算与渲染分离、Web Worker、typed arrays、事件堆/检查点、确定性表**；我们学计算组织方式，不学 Earth 的 UI。

---

## 7. 落地清单（建议新增 W13/W14，按小步）

| ID | 内容 | 文件/模块 | 验收 |
|---|---|---|---|
| C1 | `IPolityRef`/`PolityRegistry`（政治实体接口字段） | Core.Contracts、Simulation.State | 头衔可绑定 polity；测试 |
| C2 | `TitleInstance` + 绑定/历史/宣称/多重持有；`TitleRuntimeManager` | Simulation.Systems、SaveData | 改绑/继承/分裂/合并测试 |
| C3 | 头衔 UI：政府面板显示/编辑绑定；AI 接合逻辑 | UI/Panels、AI | 手动/自动绑定测试 |
| C4 | `RegionNameRecord` 双层命名 + 认知触发 + 地图标签模式 | Core.Contracts、Simulation、UI | 未接触无名/接触命名/通用名测试 |
| C5 | 官员因子 `OfficeFactor` + 可编辑权限 | Content/Simulation/UI | 因子→命令集合测试 |
| C6 | `UnlockRule` 革新→阶层/身份/职业/文化/宗教显式化 | Content JSON、Innovation/Class | 正反向门槛测试 |
| C7 | `CurrencyDef/Wallet/ExchangeRate` 多货币/区域接受/兑换 | Content、Economy、UI | 兑换/拒收/货币名测试 |
| C8 | 风暴生命周期/合并/登陆衰减/地形耦合 | Environment/Disaster | 行为测试 + 性能 |
| C9 | 性能：事件堆/检查点/typed arrays/计算渲染分离（借鉴 civ-atlas） | Simulation/Rendering | 长跑基准 |

> 优先级：C1→C2→C3（先补"政治实体接口字段"与头衔运行时，这是用户指出的缺口）→ C4（命名）→ C6（革新绑定显式化）→ C7（货币）→ C8（灾害）→ C9（性能）。

---

## 8. 与现有文档的关系

- 物产/阶层：见 [GOODS_CLASS_ENVIRONMENT_MODEL.md](GOODS_CLASS_ENVIRONMENT_MODEL.md)
- 环境/板块/气候：见 [GEOLOGICAL_CLIMATE_DYNAMICS_PLAN.md](GEOLOGICAL_CLIMATE_DYNAMICS_PLAN.md)
- 参考项目：见 [REFERENCE_PROJECTS_ANALYSIS.md](REFERENCE_PROJECTS_ANALYSIS.md)
- 本文补充：认知命名、头衔与政治实体、官员因子、革新小绑定、多货币、风暴行为。
