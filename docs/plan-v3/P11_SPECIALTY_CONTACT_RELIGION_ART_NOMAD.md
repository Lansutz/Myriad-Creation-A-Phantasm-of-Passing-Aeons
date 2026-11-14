# P11 专精、接敌、宗教词汇、美术、传统转换与游牧营地（2026-11-14）

> 本计划接 P10（已收口）。总原则不变：先读现有代码；机制优先于抽象数值；单一真源；数据驱动；过程优先；确定性；性能是约束；每批 build 全绿后提交推送。

## 0. 现状证据（先读代码）

| 主题 | 现有实现 | 缺口 |
|---|---|---|
| 专精 | `Content.Definitions.SpecialtyDerivation`：专精 = √(基本能力 × 训练)，0–100；`SpecialtyType` 六类 | 只有数值，没有"技能点/解锁操作"；无数据表、无存档、无测试 |
| 接敌 | `DetectionContactService`：`scoutRange`、`DetectionContact`（单向/双向）、`EncounterKind`（Skirmish/Battle/Siege）；`CombatManager.DailyTick` 只在 `distance==0` 结算战斗 | 探测取双方 `max(scoutRange)`，等于全知；斥候不能分遣；发现后没有"接近/伏击/避战"决策；侦查先手未进入会战变量 |
| 宗教 | `Doctrines.json` 46 项，`pillar` 六类：doctrine/ethics/institution/ritual/experience/material | 缺风俗（customs）与饮食（饮酒）等日常维度；伦理覆盖窄；选项普遍缺 `bindsTo` 可观察绑定 |
| 地图美术 | `MapRenderer` / `CyowariMapStyle`，地形材质较粗糙 | 无山影/等高线/法线混合；无素材来源与许可记录 |
| 传统 | `CultureData.traditionIds`；`Traditions.json` 13/13 `factors` 为空 | 革新→传统→习俗→教义/法律的转换链不存在 |
| 游牧营地 | `CampData : HabitationSite`（tileIndex/ownerRealmId/ownerActorId/permanence）、`CampManager`、`NomadAI` | 营地不可移动；"收起/带走"没有状态机；归属依赖 anchor/地块，移动易生悬空引用 |
| 存档欠账 | `Save.World` 仍是领域感知适配层 | 捕获器未折进 Simulation（A2 最终形态）；流程面板不入注册表；运输无独立 mode 字段 |

## 1. 学术依据（写作时引原文/权威综述，不凭印象）

- **技能与专精**：Bourdieu《实践感》habitus（身体化技能）；Lave & Wenger《情境学习》实践共同体与 apprenticeship；Ericsson 关于刻意练习的边界（区分"练习量"与"有效反馈"）。
- **侦察与会战**：Vegetius《Epitoma rei militaris》论侦察与前卫；Maurice《Strategikon》论斥候、行军纵队与接敌；John Keegan《The Face of Battle》战场迷雾与接触；Ardant du Picq 论遭遇战心理。
- **宗教分类**：Ninian Smart 七维（doctrinal, mythological, ethical, ritual, experiential, institutional, material）；Catherine Bell《Ritual Theory, Ritual Practice》；Ronald Grimes《Beginnings in Ritual Studies》；饮食戒律见 Mary Douglas《Purity and Danger》。
- **习俗与法律**：Malinowski《Crime and Custom in Savage Society》（习俗的规范效力）；Hoebel《The Law of Primitive Man》；习惯法成文化（十二表法、蛮族法典）作为"传统→法律"的历史路径。
- **游牧与营地**：Khazanov《Nomads and the Outside World》；Barfield《The Nomadic Alternative》；移动 pastoralism 的营地—草场—水源三角；ethnoarchaeology 的营地考古（Carol Kramer）。
- **术语**：汉语宗教学通用译名以《宗教大辞典》/《中国大百科全书·宗教》为准；避免把基督教专名当通用词（用户已多次强调）。

## 2. P11.1 专精与技能点（先做，P0）

**判断**：用户的方向是对的——专精应"解锁操作/战术"，而不是平铺 +X%。CK3 的生活方式 perk、EU4 的理念都证明可行；风险是 perk 通胀，所以每个节点必须绑定一个**动词或规则分支**。

设计：
- 两层分离，不另开系统：
  - **人物专精**（`SpecialtyType` 六类）→ 战术/操作解锁（将领、使节、学者）；
  - **政权能力**沿用既有革新/法令；专精不重复给政权加数值。
- **技能点来源 = 真实实践**：复用 `AddPractice` 口径，按行为记经验（战斗、巡逻、谈判、审理、研究），达到阈值得 1 点；不设"升级弹窗奖励"。
- **解锁表** `SpecialtyPerks.json`：`perkId / specialty / requiredValue / requiredPractice / unlocks { action, tactic, rule } / prerequisites`。
- 已实现但需接线：`SpecialtyDerivation` 作为门槛值；`CharacterSaveData` 存已解锁 perk 与经验。
- 示例（军事）：斥候分遣、夜袭、佯退诱敌、据险设伏、劫粮；行政：巡按、审计；外交：联姻试探、离间。
- 验收：perk 解锁改变**可执行动作集合**（UI/命令层可见），至少 3 个 perk 有行为测试；专精值高但无实践不能解锁。

## 3. P11.2 接敌与侦察重做（P0）

问题：现在探测用 `max(双方 scoutRange)`，等于全知；且没有"发现→接近→接战"的过程。

设计：
- **斥候分遣**：`ScoutDetachment`（从主军成员分出，独立 currentTile、独立 scoutRange、leash 半径=主军 scoutRange+1，超出自动归队）；主军 scoutRange 由分遣覆盖与自身机动共同决定。
- **单向探测**：`DetectedBy` 每人独立计算；`mutual=false` 时进攻方获得"先手/伏击"变量，而不是直接开战。
- **接触≠会战**：接触产生 `ContactState`（aware/ shadowed / lost，带记忆衰减）；只有两军同 tile 且敌对才进入 `ResolveBattle`；不同 tile 时由 AI 选 `Approach / Avoid / Ambush`。
- **会战变量**：地形、疲劳、补给、将领专精（P11.1 解锁的战术）、侦察先手、阵型；不新增"硬类型"枚举门槛，仍用 `EncounterKind` 作结算标签。
- **术语**：把 UI/文档中的"省份"改为"地区"（代码 `province` 暂留，后续随大重构改名）；地块仍是最小空间单位。
- 验收：`DetectionContactTests`（单向发现、先手加成、斥候分遣与归队、leash 越界）；两军不同 tile 不产生战损；同 tile 才结算。

## 4. P11.3 宗教词汇与分组扩充（P1）

- **分组**改用宗教学中性术语（六维 + 日常）：
  - `cosmology` 宇宙论/神观（一神、多神、二元、泛灵、祖灵）
  - `ethics` 伦理（性、婚姻、通奸、巫术、弑亲、血仇、奴隶、放贷、伪誓）
  - `institution` 教制（神职性别、婚配、任命、教阶）
  - `ritual` 仪式（丧葬、祭献、朝圣、洁净）
  - `experience` 宗教体验（苦修、奉爱、秘传、内在之旅）
  - `material` 物质（圣地、神庙、圣髑、圣像）
  - `customs` **风俗（新增）**：饮酒（许可/节制/禁酒）、宴饮、日常祝祷、服饰、音乐舞蹈、节期（安息日/斋期）、待客礼
- 每个选项补 `bindsTo`：`law`（司法/婚姻/继承）、`decree`、`festival`、`dietary`；至少一个可观察量（犯罪判定、婚姻许可、节庆日程、饮酒惩罚）。
- 内容来源以宗教学工具书与民族志个案为准；不把某一宗教的专名当通用词。
- 验收：新增 customs 组 ≥8 项、ethics 扩充 ≥6 项；每项有 `bindsTo`；`DoctrineBindingTests` 覆盖新增绑定；无绑定的选项显式标注"仅展示"。

## 5. P11.4 地图美术与材质（P1）

- 地形：三平面/高度混合 + 法线 + AO；**山影（hillshade）**由高程实时生成；等高线按海拔分层；岩层/雪线按纬度与高度。
- 素材：只用许可明确的来源（Poly Haven CC0、ambientCG CC0、Bootstrap Icons MIT），记录 `docs/art/ASSET_LICENSES.md`；本地 DSH 工具可做批处理（下载/转换/图集）。
- 图标：`https://icons.bootcss.com/`（Bootstrap Icons，MIT）；优先本地化 SVG→PNG 图集，避免运行时网络。
- 验收：8192 地块下地形材质切换无 GC 峰值；山影开关可配置；素材许可清单完整；截图对比（before/after）存入 docs/art。

## 6. P11.5 革新 → 文化传统 → 习俗 → 教义/法律（P1）

- 链：`Innovation`（地理/生计类）→ `TraditionCandidate`（如"河运""游牧转场""山地放牧"）→ 重复实践累积为 `Custom` → 可被**法典化**（law/decree）或被**宗教吸收**（doctrine option）。
- 数据表 `TraditionConversions.json`：`traditionId → { doctrineOptionId | lawId | decreeId, requires { practice, class, religionPillar } }`；过程优先，不瞬时。
- 现有 `Traditions.json` 的 `factors` 补齐（当前 13/13 为空），并给传统加 `sourceInnovationIds`。
- 验收：一条完整链路的 EditMode 测试（革新完成 → 传统出现 → 累积成俗 → 转教义/法律各一条）；转换有冷却与条件，不能空转。

## 7. P11.6 游牧营地移动与归属（P0/P1）

问题：营地是 `HabitationSite` + anchor，移动会让归属/管辖引用悬空。

设计（单一真源）：
- `CampData.mobility`：`Pitched`（已扎营，anchor 固定）/ `Packed`（收起，随主人移动）/ `Moving`（迁移中）。
- 归属只认 `ownerActorId`（人/部落）；`ownerRealmId` 是从属关系的结果，由"营地当前地块的管辖权"推导，不双写。
- 迁移：`Moving` 时 anchor 跟随 actor 的 currentTile；到达后重新 Pitched，籍贯/税收/征募按新地块重算；旧地块留下"旧营地痕迹"（可选，考古层）。
- 存档：`CampSaveData` 带 mobility/route/arrivalDay；读档重建 anchor，避免重复 anchor。
- 验收：`CampMobilityTests`（收起→移动→扎营、归属不悬空、读档往返、越界/被占用地块处理）。

## 8. P11.7 遗留欠账

- `Save.World` 捕获器折进 `Simulation`（A2 最终形态）或明确保留为适配层并写判据；
- 流程面板注册口径维持 D155/D160；
- 运输"方式"若需要独立语义，再加显式字段（当前由兽/车/船编成表达）。

## 9. 批次与验收

| 批次 | 内容 | 验收 |
|---|---|---|
| P11.1 | 专精解锁表 + 人物经验 | 3 个 perk 行为测试；无实践不解锁 |
| P11.2 | 斥候分遣 + 单向探测 + 接触/会战分离 | 4 类探测测试；不同 tile 无战损 |
| P11.3 | 宗教分组与绑定扩充 | ≥14 新选项均带 bindsTo；绑定测试 |
| P11.4 | 地形材质/山影/图标/许可 | 截图 + 许可清单 + 性能 |
| P11.5 | 传统转换链 | 端到端测试 |
| P11.6 | 营地移动状态机 | 往返 + 归属测试 |
| P11.7 | 欠账 | 每项有判据 |

每批：`EXIT=0`、零 warning、EditMode 全绿、DECISIONS/PROJECT/P11 同步、commit + push。

## 10. P11.1 实施记录（2026-11-14）

- `SpecialtyPerkDef`/`SpecialtyPerkCatalog` 读 `Base/Specialty/SpecialtyPerks.json`（8 节点）。
- 解锁门槛 = 既有技能值 + 该专精真实实践；`CharacterData.specialtyExperience/unlockedPerks` 入档。
- `SpecialtyPerkSystem.TryUnlock/TryUnlockAll/CanUseAction`；`SpecialtyPerkTests` 4 例。
- 验证：`EXIT=0`、1086 例、1075 通过、零 warning CS。下一步 P11.2：斥候分遣与单向探测。

## 11. P11.2 实施记录（2026-11-14）

- `CanDetect` 单向化；`CanContact` = 任一方看到。
- `Army.scoutDetached/scoutTileIndex`；`TryDetachScouts` 需 `scout_detach`；观测原点前移，不加射程；leash 越界归队。
- `CombatManager` 按观察者去重记录接触；仍只在同地块结算战损。
- 存档字段与 4 个探测测试；验证：`EXIT=0`、1089 例、1078 通过、零 warning CS。
- 机制优先复盘：分遣收益来自"观测原点前移"，未使用 +射程抽象加成。

## 12. P11.3 实施记录（2026-11-14）

- `customs` 组（9 项）与伦理扩充（6 项），共 61 项教义全部带 `bindsTo`；`ValidateOption` 强制绑定合法。
- 饮酒/弑亲/放贷三行为真的进入 `IsDoctrineCriminal/IsDoctrineShunned`；其余绑定点登记待消费者。
- `DoctrineCustomsTests` 5 例；验证：`EXIT=0`、1094 例、1083 通过、零 warning CS。
- 机制复盘：禁酒不是数值惩罚，而是走既有司法/审判流程。
