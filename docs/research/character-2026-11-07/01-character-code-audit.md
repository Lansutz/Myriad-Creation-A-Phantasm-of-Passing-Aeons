# 角色与家族系统 · 代码现状审计（2026-11-07）

> 审计对象：`D:/Myriad-Creation-A-Phantasm-of-Passing-Aeons`（Unity 6 + 纯 C# 模拟）
> 路径约定：以下路径相对 `MyriadCreation/`，形如 `Assets/.../CharacterData.cs:113`。
> 方法：`grep` 全仓交叉引用（调用方/消费者计数）+ 通读核心文件；**只读，未编译未运行**。抽样面 `Assets/Scripts` 共 1003 个 `.cs`。
> 对照稿：用户口述的 8.1 设计要点（三层属性 / 专精六维 / 双维健康 / 性格关系家族继承 / 反对宣战技能化）。仓内 `docs/角色系统整体设计.docx` 的 8.x 是 **DNA 章**，不是这份新稿，故不做文本对照。

## 0. 结论速览

1. **三层属性只有 0.5 层**：`BasicAbilities`（12 项）与 `SpecialtyType`（六维）两个类都写了，但**基本能力无任何生成/成长写入方，恒为全 50**；`SpecialtyType` **全仓零消费者**；**技能树、点数、前置、节点门槛一律不存在**；`skills` 字典无生产者。
2. **专精与基本能力之间没有任何公式**：六维是创角时 `Random.Range(20,80)` 直接滚出来的（`CharacterManager.cs:747-761`），与 `basicAbilities` 无关联；"专精=能力+训练"未实现。
3. **角色级疾病系统整建制空转**：`CharacterDiseaseSystem.cs`（990 行、32 种病）**从未被 `new`**；生产环境走人口级 `DiseaseSystem` 直接扣 `health`；`GetDiseaseMods` 零消费者 → 疾病不修正任何属性。
4. **特质与精神疾病对数值零影响**：汇总修正函数 `CharacterData.GetEffectiveStats:297` 与 `MentalHealthSystem.ApplyDisorderMods:106` **都是零调用**；`AddTrait:649` 也零调用 → 108 条特质的 `martialMod/diplomacyMod…` 永不生效，特质从不被授予。
5. **家族/继承是"骨架 + 一次性初始化"**：`FamilyNode` 只在开世界时每政权建一个（`CharacterManager.cs:934`）；分支、家族传统、家族声望/财富全无写入；继承只换 `monarchId`，**无领地/头衔/财产转移**（`ownedTitles` 零生产者、`DistributeLand` 零调用）。
6. **宣战当前确实没绑角色技能**（`AIController.cs:326-351` 只看 `coreTiles/treasury/relation`，`WarJustificationSystem` 不出现 Character）——但这更像"技能树根本不存在"的副作用，而非有意的解耦设计。

## 1. 数据模型清单

### 1.1 `CharacterData` 全字段（`Assets/Scripts/Simulation/MyriadCreation.Simulation.State/CharacterData.cs`，753 行）

| 字段（行号） | 写入方 | 读取方 | 判定 |
|---|---|---|---|
| characterId:13, firstName:15, lastName:17, age:19, isMale:21, birthDay:23, birthYear:25, deathDay:27, deathYear:29 | `CreateCharacter:720-737`、`DailyTick:338-341`、`Die:642` | 全系统 + 存档 | 活 |
| currentSettlementAnchorId:31, currentTile:33, isPublicFigure:35, aiCategory:37 | 位置服务/`CreateInitialRulers:926-928` | 位置/渲染/UI | 活 |
| identity:39（`IdentityProfile`） | 仅 `SyncIdentityFromLegacy:217` 由 `socialClass` 派生 | 仅测试（`Tests/.../SocialIdentityEnumTests.cs:108`） | **派生 + 不入档，读档即丢** |
| socialClass:41, socialSubclass:43, realmId:45, cultureId:49, raceId:51, faithId:53, role:93 | `SyncClassFromRole:256`、创角、继位 | 全系统 | 活 |
| familyId:47 | `CreateFamily:441`、`Procreate:171-178`（仅父系）、`CreateInitialRulers:936` | 继承选人 `InheritanceHeirSelection.cs:32,70`、UI DTO:73/77 | 活但见 §4 |
| epithet:55, epithetEvaluated:65, posthumousTitle:67, legendaryTitle:69 | `EpithetSystem` | UI/编年史 | 活 |
| bodyMarks:57 | **无写入方**（全仓仅声明） | `EpithetSystem.cs:525-600` | 死写/空读 |
| achievements:61（`AchievementRecord`） | 战争/继承/劫掠（`GameWorld.Succession.cs:100/230/276`、`RaidSettlementCommandHandler.cs:37`） | `EpithetSystem`、`EvaluationSystem` | **活，但不入档** |
| accessionDay:63 | `GameWorld.Succession.cs:97` | `reignYears` 计算 `GameWorld.Succession.cs:269` | **不入档，读档后 -1** |
| 旧计数器 conquests:71, warsWon:73, diplomaticVictories:75, cultureWorks:77, religiousDeeds:79, expeditions:81, vigilanceActs:83 | **无写入方** | **无读取方**（全仓 `achievements.*` 才是真读写） | **全死，与 AchievementRecord 重复** |
| spiritualFulfillment:85, privateFaithId:87, isSecretBeliever:91 | `CharacterManager.cs:38-120` 信仰 API、`CanonizationSystem:77` | UI `piety` | 活 |
| personalTenets:89 | `AddPersonalTenet:54` | 信仰逻辑 | **活，但不入档** |
| fatherId:95, motherId:97, spouseId:99 | 创角/`Marry:231-232` | 家族树/继承/近亲 | 活 |
| dna:101（`DnaData` 遗留）、genome:103、appearance:105、dnaExpression:109、expectedLifespanYears:107、individualResistance:111 | `CreateCharacter:739-742`、`Procreate:162-170`、`GetGenome:263` | 遗传/疾病/外貌 | DNA 权威=`genome`；`dna` 为兼容层（读档置空，`CharacterSaveData.cs:512`）；`appearance/dnaExpression` 为派生缓存不入档 |
| **basicAbilities:113** | **无**（`new BasicAbilities()` 默认全 50；`BasicAbilities.Random` 零调用） | 仅存档往返 `CharacterSaveData.cs:173/236/512` | **全 50 常量，死字段** |
| **prowess:116, social:119, military:122, management:125, conspiracy:128, scholarship:131** | 创角随机 `CharacterManager.cs:747-761`；模板 clamp `807-824`；天赋 ±`859-862` | 派系 `FactionManager.cs:235,256`、腐败 `GameWorld.Simulation.cs:735`、强君事件 `GameWorld.Succession.cs:63`、UI `CharacterDetailQueryAdapter.cs:80-88` | 活（唯一真属性） |
| prestige:133, prestigeCapacityLevel:135, notoriety:137 | `ModifyPrestige:699`、`ModifyNotoriety:714` | 派系/绰号/UI | 活 |
| health:140, obesity:152 | 衰老 `DailyTick:346`、疾病 `DiseaseSystem.cs:209`、`DailyDiet:545-549` | 死亡判定/UI | 活 |
| **fertility:143** | 创角默认 50 | **无**（`Procreate:120-184` 不读生育力） | **死字段** |
| stress:146, dread:149, charm:155 | stress 仅 `DailyDiet:550`（+2）；dread 仅 `AddDread:640`（**零调用**）；charm 创角 `774` | stress：UI:88 + 漂移/疾病；dread：`CheckMentalDisorders:571`；charm：仅 `GetEffectiveStats`（死） | stress 半活；**dread 永不上升**；charm 死 |
| 人格 boldness:158…piety:176（7 个 float） | `SetPersonalityValue:410`、`DailyTick:358-363`、`InitializePersonality:832` | AI `AIController.cs:367-380`、绰号、经验原型 | 活；**缺 `sociability`，见 §5-5** |
| mentalDisorderId:178, highStressDays:180, lowStressRecoveryDays:182 | `CheckMentalDisorders:563-606` | 绰号/UI/`GetEffectiveStats`（死） | 活（写入链路见 §3） |
| bodyParts:59, activeDiseases:184, impairments:186 | 身体部位系统、存档恢复、`CharacterDiseaseSystem`（未接线） | 部位系统 | 部位活；疾病/伤残见 §3 |
| **traits:188**（`List<PersonalityTrait>`） | `AddTrait:649`（**零调用**）+ 存档恢复:547 | `GetEffectiveStats`（死）、`EconomicalArchetype` 判定 `591-633`、UI | **运行期永不授予** |
| **relations:191**（`Dictionary<int,CharacterRelation>`，`[NonSerialized]`） | `ModifyRelation:668`（**零调用**）+ 存档恢复:576 | `PersonalityOpinionDrift:609-628`（只改已存在边） | **运行期永不创建** |
| **skills:194**（`Dictionary<string,float>`，`[NonSerialized]`） | **无** | 仅存档:297/593 + 测试 | **纯占位** |
| **gold:196** | 无 | 仅测试 | **死字段**（角色财产未接经济） |
| **ownedTitles:198** | **无** | 仅测试 | **死字段**（无领地头衔实体） |
| **commandedArmyId:200** | **无赋值** | 死亡清理 `CharacterManager.cs:956-960` 只置 -1 | 死字段（军团侧不反查角色） |
| 属性 `fullName:202`、`isAlive:204` | — | 全系统 | 派生 |

### 1.2 基本能力（`Assets/Scripts/Content/MyriadCreation.Content.Definitions/BasicAbilities.cs`）

| 设计 | 代码 | 状态 |
|---|---|---|
| 体魄 力量/协调/坚韧 | strength:10 / coordination:13 / endurance:16 | ✅ 字段齐全，默认 50，`Range(0,100)` |
| 心智 记忆/逻辑/灵感 | memory:19 / logic:22 / inspiration:25 | ✅ |
| 交涉 口才/洞察/气度 | eloquence:28 / insight:31 / charisma:34 | ✅ 字段；**"交涉"英文取了 `Diplomacy`（:49），与政权外交同名** |
| 感知 观察/直觉/反应 | observation:37 / intuition:40 / reaction:43 | ✅ |
| 四类派生 | Physique:45、Mind:47、Diplomacy:49、Perception:51、Overall:53 | ✅ 有，但**无调用方** |
| 随机生成 | `BasicAbilities.Random:55-72`（clamp 5-95） | ❌ **零调用** |

### 1.3 专精六维（`.../Content.Definitions/SpecialtyType.cs`）

| 项 | 位置 | 状态 |
|---|---|---|
| 枚举六值 社交/阴谋/军事/管理/学识/勇武 | `SpecialtyType.cs:3-11` | ✅ 与设计一致 |
| `All` 数组 | `SpecialtyTypeExtensions.cs:5-13` | ✅ |
| 显示名/描述 | `GetDisplayName:15-27`、`GetDescription:29-41` | ✅ 已写，**全仓零调用** |
| 与 `CharacterData` 六维的桥接 | 无；且两者**顺序不同**（枚举 Social,Conspiracy,Military,Management,Scholarship,Prowess vs 字段 prowess,social,military,management,conspiracy,scholarship） | ❌ 若按枚举下标映射会错位 |
| 范围 0-100 | `CharacterData` 对应字段用 `[Range(0,100)]`，但代码路径 clamp 到 5-95（`747-748`）/20-80（`749-752`） | ⚠️ 声明与实现不一致 |

### 1.4 健康 / 压力 / 精神疾病 / 疾病

| 实体 | 位置 | 关键内容 |
|---|---|---|
| 肉体健康 | `CharacterData.cs:140` `[Range(0,100)] health=100` | 衰老衰减 `DailyTick:342-346`；≤0 死亡 `367-370`；疾病扣血 `DiseaseSystem.cs:209` |
| 压力 | `CharacterData.cs:146` `[Range(0,100)] stress` | 恢复 `DailyTick:348-349`；高压计数 `350-357`；**唯一外部上升通道 = 缺粮 +2（`CharacterManager.cs:550`）** |
| 恐惧 | `CharacterData.cs:149` `dread` | 仅 `AddDread:640`（零调用）+ 存档 → 实际恒为初始值，偏执判定 `571` 不可达 |
| 精神疾病定义 | `.../State/MentalHealthSystem.cs` | 常量 `9-17`（90/80/120/70/45）；5 条内置病 `19-77`；`GetDef:79`、`GetDisorderName:96`、`ApplyDisorderMods:106`（零调用） |
| 精神疾病判定 | `CharacterManager.cs:555-607` | 见 §3 |
| 角色级进行中疾病 | `.../State/ActiveCharacterDisease.cs:7-47` | 9 字段（diseaseId/stage/elapsedDays/stageDays/severity/isTreated/treatmentDays/infectionSource） |
| 角色疾病系统 | `.../Systems/CharacterDiseaseSystem.cs`（990 行） | 32 个 `CharacterDiseaseDef`（`18-679`）；`DailyTick:699`、`InfectCharacter:861`、`CombatInjury:890`、`CheckChronicDiseaseOnset:918`、`GetDiseaseMods:944`；**从未被实例化** |
| 疾病定义字段 | `.../Content.Definitions/CharacterDiseaseDef.cs` | 32 字段，含 `prowessMod…fertilityMod`、`stressThreshold`、`obesityThreshold`、`requiredInnovation`——因系统未接线全部无效 |
| 人口级疾病 | `.../Systems/DiseaseSystem.cs` | `DailyTick:118`、`InfectCharacters:179-219`（按 realm 随机，直接扣 health 并可能 `Die`）；生产唯一在跑的疾病路径 |

### 1.5 性格

| 实体 | 位置 | 状态 |
|---|---|---|
| 维度枚举 | `.../Content.Definitions/PersonalityDimension.cs` | **8 值**：Boldness, **Sociability**, Compassion, Greed, Honor, Rationality, Vengefulness, Piety |
| 维度表 | `PersonalityDimensions.cs:7-18` `All[8]` | 8 项 |
| 角色承载 | `CharacterData.cs:158-176` | **只有 7 个 float，无 sociability** |
| 取值/赋值 | `GetPersonalityValue:395-408`（`_ => 0f`）、`SetPersonalityValue:410-439`（`case Sociability: break;`） | **Sociability 静默吞掉** |
| 特质定义 | `.../State/PersonalityTrait.cs:6-59` + `.../Definitions/PersonalityTraitDatabase.cs:9`（`new PersonalityTrait[108]`） | 108 条定义齐全 |
| 授予路径 | `AddTrait:649` | **零调用 → 特质永不获得** |
| 性格演化 | `DailyTick:358-363`（±0.01 / 压力>60 时 ±0.02）、`InitializePersonality:832-852`（双亲均值±10；无父母 ±30） | ✅ 与设计一致 |
| 亲和度 | `GetPersonalityAffinity:464-503` | ✅ 但被 `PersonalityOpinionDrift:609` 使用时空转（无关系边） |

### 1.6 关系 / 纽带

| 实体 | 位置 | 状态 |
|---|---|---|
| 角色对关系 | `.../State/CharacterRelation.cs:9-25`（struct：otherCharacterId/opinion(-200~200)/type/history/trust/fear/romanticAttraction） | ✅ 结构完整 |
| 关系类型 | `.../Content.Definitions/RelationshipType.cs` | 9 值（Stranger…Vassal） |
| 写入口 | `CharacterData.ModifyRelation:668` | **零调用**；`new CharacterRelation` 只出现在存档恢复与测试 |
| 纽带 | `.../State/CharacterBond.cs:6-31` + `BondType`（8 值）+ `SpecialBondType`（3 值） | 结构完整 |
| 纽带创建 | `CharacterManager.CreateBond:446` | **仅测试调用** |
| 纽带效果 | `combatBonusWhenTogether:20 / diplomacyBonus:22 / stressReduction:24 / sharedFate:26` | **零消费者** |
| 纽带 tick | `CharacterBond.DailyTick:28-31`（`Random.Range(-1,1)`） | 每次日推随机漂移，无来源加成 |

### 1.7 家族 / 世族 / 继承 / DNA

| 设计概念 | 代码实体 | 状态 |
|---|---|---|
| 家族 | `.../State/FamilyNode.cs:10-191`（familyId/familyName/founder/foundingYear/memberIds/branches/parentFamily/familyPrestige/familyWealth/familyTraditions/holderRealmId/homelandCountryId/generationMarks/Innovations/coaPattern/coaColors） | 结构存在 |
| 王朝/世族 Dynasty/House | **无独立类**。只有 DTO 字段 `dynastyName`（`Core.Contracts/TileCharacterContracts.cs:136`），映射自 `family.familyName`（`CharacterDetailQueryAdapter.cs:73`） | 即 `FamilyNode` 的展示别名 |
| 家族创建 | `CharacterManager.CreateFamily:426-443` | 生产仅 `CreateInitialRulers:934` 调一次/政权 |
| 分支家族 | `FamilyNode.CreateBranch:55-67` | **零调用**；且用 `Random.Range(10000,99999)` 生成 id，与 `_nextFamilyId` 序列（`430`）会撞 |
| 家族传统 | `AddFamilyTradition:130` / `GetTraditionEffect:172` / `FamilyTraditionDef` | **零调用**（定义可加载，无玩法） |
| 家族声望/财富 | `FamilyNode.familyPrestige:27` / `familyWealth:29` | **零写入**（恒 0） |
| 家族纹章 | `coaPattern:43` / `coaColors:45` | **零读写** |
| 继承法 | `.../Content.Definitions/InheritanceLaw.cs:6-18`（scope/branch/gender/age/titleMode/landMode） | ✅ |
| 继承人选 | `.../WorldState/InheritanceHeirSelection.cs:13/23-90`（性别→长支/兄终弟及→年龄） | ✅ 生效 |
| 继位执行 | `.../Systems/SuccessionSystem.cs:12-63` | 只写 `realm.monarchId/consulId` + `heirId=-1` |
| 绝嗣/摄政 | `TryEnterInterregnum:89`、`TickInterregnum:110`、`SelectRegent:220` | ✅ 较完整 |
| 头衔继承 | `titleMode` 仅用于 `InheritanceLaw.GetName:91`（文案）；`ownedTitles` 零生产者；`RealmData` 只有 `officeHolders:24`（官职，非领地头衔） | ❌ 未实现 |
| 领地分配 | `InheritanceLaw.DistributeLand:108-138` | **零调用** |
| 财产继承 | `CharacterData.gold:196` 无消费者 | ❌ 未实现 |
| DNA/遗传 | `.../State/DnaData.cs`、`DnaExpression.cs`、`Simulation.Genetics/` 26 文件（`GeneticsEngine`/`GenomeInstance`/`GeneticInheritance`/`Wright 近亲`/性染色体/表型/杂交） | ✅ 比设计稿更完整；`CreateCharacter:739`、`Procreate:162-170` 接入 |

## 2. 三层属性是否真的落地

| 设计层 | 代码事实 | 状态 |
|---|---|---|
| 基本能力（4 类 × 3） | 类与 12 字段齐全（`BasicAbilities.cs:10-43`）；**无生成、无成长、无读取**，恒 50 | **字段有 / 系统没有** |
| 专精（六维 0-100） | 6 个 float（`CharacterData.cs:116-131`），创角随机 20-80 | **有字段，但"专精"仅是随机属性** |
| 专精 = 基本能力 + 技能 | 无公式。grep `Derive/Calculate/Special` 仅命中 `CalculateOverallAbility:282`、`CalculateRuleAbility:287`、`CalculateCommandAbility:292`——三者都只对**六维自身**加权，不含基本能力 | ❌ 未实现 |
| 技能树 | **无** `SkillTree/SkillNode/SkillPoint/Prereq/Unlock` 类型；唯一痕迹是 `Dictionary<string,float> skills:194`（`[NonSerialized]`），仅存档与测试读写 | ❌ 未实现 |
| 节点门槛 = 专精值 + 剩余点 + 前置 + 基本能力门槛 | 无任何门槛/解锁校验代码 | ❌ 未实现 |
| 技能获取/训练/成长 | 无训练入口；六维除创角、模板 clamp、天赋 ± 外**不会成长** | ❌ 未实现 |
| `SpecialtyType` 作为专精类型契约 | 全仓引用计数 = 2（枚举文件自身 + Extensions 文件自身） | ❌ **零消费者** |

**结论**：三层属性目前是"专精六维 + 一个永远 50 的基本能力 + 一个空技能字典"，层与层之间没有任何连接。

## 3. 健康与压力的实际实现

| 机制 | 实现 | 数值/阈值 |
|---|---|---|
| 健康范围 | `CharacterData.cs:140` `[Range(0,100)]` | 0-100 float |
| 衰老衰减 | `DailyTick:342-346` | 超过 `lifespan*0.6` 后按比例衰减；肥胖>70 ×1.5 |
| 死亡 | `DailyTick:367-370`、`Die:642` | health≤0 |
| 压力恢复 | `DailyTick:348-349` | 压力>60：**-0.01/日**；否则 **-0.05×stressDecayMult**/日 |
| 压力上升 | `CharacterManager.DailyDiet:527-551` | **唯一来源：缺粮 +2/日**（`AddStress:631` 零调用）；进食不减压力 |
| 高压计数 | `DailyTick:350-357` | >80 → +1/日；否则 -2/日 |
| 精神疾病·长期高压 | `CharacterManager.CheckMentalDisorders:565-570` | `highStressDays ≥ 90` → 抑郁 60% / 焦虑 40% |
| 精神疾病·深恐 | `:571-575` | `dread>80` 且 0.2%/日 → 偏执（dread 无上升通道 ⇒ 实际不可达） |
| 精神疾病·失智 | `:576-584` | `age≥70 && scholarship<45`，0.05%×Δ/10/日 |
| 康复 | `:587-605` | 压力<30 累积 120 日；`reversible=false`（失智）跳过；`CureMentalDisorder:662` 手动治愈 |
| 人格漂移 | `CharacterData.DailyTick:358-363` | 每维 ±0.01；压力>60 时 ±0.02（**翻倍，与设计一致**） |
| 精神疾病对属性 | `GetEffectiveStats:319-329` / `ApplyDisorderMods:106` | **两个函数均零调用 ⇒ 修正值不生效** |
| 疾病→属性 | `GetDiseaseMods:944` | **零调用**；疾病只经 `DiseaseSystem.cs:209` 扣 health |
| 常量复用 | `MentalHealthSystem.cs:9-17` 定义 90/80/120/70/45 | **零引用**；同值以字面量硬编码在 `CharacterManager.cs:565/571/576/595` |

## 4. 家族与继承

- **实体**：`FamilyNode`（树形，`branches`+`parentFamily`），无 `Dynasty/House` 第二套；`dynastyName` 只是 DTO 别名。
- **`familyId` 关系**：`CharacterData.familyId:47` 指向 `CharacterManager._families`；写入=建族(`441`)/生育(`171-178`，**只跟随父系**)/建国(`936`)；消费=继承候选(`SuccessionSystem.cs:331-349`)与长支排序(`InheritanceHeirSelection.cs:32,70`)、UI。
- **继承候选人来源单一**：`CollectCandidates:331-349` 只遍历 `family.memberIds`；若 `ruler.familyId<0` 或家族缺失 → 候选空 → `ExecuteSuccession` 直接判"绝嗣"(`:35-45`)。成员表只在建族/生育时追加，**死亡不移除**（靠 `isAlive` 过滤）。
- **规则生效面**：scope（`ClanOnly` 过滤）、gender（4 档）、branch（长支/兄终弟及）、age（长/幼）全部生效；`titleMode/landMode` 只进 `GetName` 文案，`DistributeLand` 零调用 ⇒ **领地/头衔/财产继承全未实现**。
- **头衔**：无 landed-title 实体，只有 `RealmData.officeHolders:24`（官职）；`CharacterData.ownedTitles` 无生产者。
- **不入档家族字段**：`FamilySaveData`（`Infrastructure.Save/WorldEntitySaveData.cs:340-380`）丢 `branches/familyTraditions/coaPattern/coaColors/Innovations`（注释 `:336-338` 自认）。
- **reignYears 公式缺陷**：`GameWorld.Succession.cs:269-273` 用 `deathDay-accessionDay + (deathYear-birthYear)*365` ⇒ 结果≈**年龄**而非在位年数；且 `accessionDay` 不入档，读档后恒 -1，谥号/传奇的 `reignYears>=10/15/20` 判定（`EpithetSystem.cs:225/436/444`）会失真。

## 5. 不合理 / 风险清单

| # | 严重度 | 问题 | 证据 |
|---|---|---|---|
| 1 | P0 | 三层属性断链：能力恒 50、专精纯随机、技能树不存在 | `BasicAbilities.cs:55`零调用；`CharacterManager.cs:747-761`；`SpecialtyType` 0 消费者 |
| 2 | P0 | 角色级疾病系统（990 行/32 病）从未实例化；人口级另有一套直接扣血 | `CharacterDiseaseSystem.cs:13`；`DiseaseSystem.cs:179-219`；`GetDiseaseMods:944` 0 调用 |
| 3 | P0 | 特质与精神疾病不改变任何数值；特质永不授予 | `GetEffectiveStats:297` / `ApplyDisorderMods:106` / `AddTrait:649` 均 0 调用 |
| 4 | P0 | 存档漏字段：`achievements/accessionDay/personalTenets/bodyMarks/identity/7 个旧计数器` | `CharacterSaveData.cs:31-176` 无这些字段；`FromCharacter:181-238` 不写 |
| 5 | P1 | 性格 8 维枚举 vs 7 个字段：`Sociability` 静默丢弃；注释/存档称"七维"却写 8 个槽位 | `PersonalityDimension.cs` 8 值；`CharacterData.cs:158-176` 7 字段；`SetPersonalityValue:436-438` no-op；`CharacterSaveData.cs:170/243-246` |
| 6 | P1 | 关系系统空转：无运行期创建；`history` 无上限增长 | `new CharacterRelation` 仅 `CharacterData.cs:672`+存档；`ModifyRelation:679` 无条件 `Add` |
| 7 | P1 | 纽带无任何效果，却参与日推随机漂移 | `CharacterBond.cs:20-31`；`CreateBond:446` 仅测试 |
| 8 | P1 | 家族只初始化一次、分支/传统/声望/财富全死；`CreateBranch` id 随机与序列冲突 | `CharacterManager.cs:934`；`FamilyNode.cs:55,120,130`；`430` |
| 9 | P1 | 继承不转移头衔/领地/财产；`DistributeLand` 死代码 | `SuccessionSystem.cs:12-63`；`InheritanceLaw.cs:108`；`ownedTitles` 0 生产者 |
| 10 | P1 | `reignYears` 实为年龄；`accessionDay` 不入档 | `GameWorld.Succession.cs:269-273`；`CharacterSaveData.cs` 无该字段 |
| 11 | P1 | 双存档路径：`SaveSystem` 存角色，`GameSaveSystem` 0 处角色引用 | `SaveSystem.cs:509-521`；`GameSaveSystem.cs` grep `Character` = 0 |
| 12 | P1 | 压力只有"缺粮 +2"一个入口；`AddStress/AddDread` 零调用 ⇒ 压力/恐惧系统近乎没有输入 | `CharacterManager.cs:550,631,640` |
| 13 | P2 | 命名冲突：交涉=`BasicAbilities.Diplomacy` vs 政权外交 `DiplomacyManager/DiplomaticRelation`；社交=`SpecialtyType.Social`/`social` 字段/`PersonalityDimension.Sociability` 三个"社交"；`charm` 与 `charisma` 重复 | `BasicAbilities.cs:34,49`；`SpecialtyType.cs:5`；`CharacterData.cs:119` |
| 14 | P2 | 旧计数器 7 字段与 `AchievementRecord` 同义重复 | `CharacterData.cs:71-83` vs `AchievementRecord` |
| 15 | P2 | 魔法数散落；`MentalHealthSystem` 常量形同虚设 | `CharacterManager.cs:400/417/550/565/571/576/578/595` |
| 16 | P2 | 六维声明 `[Range(0,100)]` 实际 clamp 5-95 / 20-80 | `CharacterData.cs:116` vs `CharacterManager.cs:747-761` |
| 17 | P2 | `[NonSerialized]` 与手工 DTO 拍平并存，注释自相矛盾（"不序列化"但实际入档） | `CharacterData.cs:190-194` vs `CharacterSaveData.cs:120-122,297,593` |
| 18 | P2 | 无消费者 API 堆积 | `SpecialtyType*`、`GetEffectiveStats`、`ApplyDisorderMods`、`GetDiseaseMods`、`CalculateOverallAbility:282`、`FindBestGeneral:1033`、`FindBestRuler:1052`、`BasicAbilities.Random`、`FamilyNode.CreateBranch/AddFamilyTradition/GetTraditionEffect`、`InheritanceLaw.DistributeLand` |
| 19 | — | **AI 读取面**：读人格七维（`AIController.cs:367-380`）、六维+prestige（`FactionManager.cs:235,256`）、conspiracy（`GameWorld.Simulation.cs:735`）、六维均值（`GameWorld.Succession.cs:63`）；**不读** `basicAbilities/skills/traits`（唯一读点是被判死的 `GetEffectiveStats`）；宣战不读任何角色属性（`AIController.cs:326-351`；`WarJustificationSystem` 无 Character 引用） | 与设计"反对宣战绑技能树"的**方向一致，但属巧合** |

## 6. 设计稿 vs 代码对照表

| # | 设计条目 | 代码状态 | 证据 |
|---|---|---|---|
| 1 | 基本能力 4 类 × 3 子属性 | **部分**：字段与派生齐全，但全局恒 50、无生成/成长/消费 | `BasicAbilities.cs:10-53`；`BasicAbilities.Random:55` 0 调用 |
| 2 | 专精六维 0-100 | **部分**：6 字段存在，范围靠 clamp 近似，映射契约 `SpecialtyType` 零消费者 | `CharacterData.cs:116-131`；`SpecialtyType.cs` |
| 3 | 专精 = 基本能力 + 学到的技能 | **没有**：无公式、无训练、六维与能力无关联 | `Calculate*Ability:282-295` 只含自身 |
| 4 | 技能树（分叉/前置/节点） | **没有**：无任何技能树类型 | grep `SkillTree/SkillNode/Prereq` 无命中 |
| 5 | 节点门槛 = 专精+剩余点+前置+基本能力门槛 | **没有** | 未找到 |
| 6 | 健康：肉体 0-100 | **已实现** | `CharacterData.cs:140`；`DailyTick:342-370` |
| 7 | 精神健康 = 压力 + 精神疾病 | **已实现**（无独立精神值） | `stress:146`、`mentalDisorderId:178`、`MentalHealthSystem` |
| 8 | 压力>60 人格漂移翻倍 | **已实现** | `CharacterData.cs:358-363` |
| 9 | 压力>80 精神疾病风险 | **部分**：实现为">80 持续 90 天"，另有恐惧/失智支线；且压力几乎无输入 | `CharacterManager.cs:565-584` |
| 10 | 性格 | **部分**：七维可用，第 8 维 `Sociability` 破裂；特质无授予链路 | `PersonalityDimension.cs` vs `CharacterData.cs:158-176` |
| 11 | 关系 | **偏离**：结构完整但无运行期创建、无纽带效果 | `CharacterRelation.cs`；`CreateBond:446` |
| 12 | 家族 | **部分**：实体在，但只初始化一次、分支/传统/声望全死 | `FamilyNode.cs`；`CharacterManager.cs:934` |
| 13 | 继承 | **部分**：王位继承可用（含绝嗣/摄政），无头衔/领地/财产继承 | `SuccessionSystem.cs`；`ownedTitles`/`DistributeLand` |
| 14 | DNA/遗传 | **已实现且超出设计** | `Simulation.Genetics/` 26 文件；`DnaSystem.cs` |
| 15 | 反对"宣战"绑角色技能树 | **符合现状**（宣战读政权指标+AI 性格），但原因是技能树不存在 | `AIController.cs:326-351`；`WarJustificationSystem` |

## 7. 审计边界

- 只读；未跑 Unity 编译/测试，未验证运行时行为，结论基于静态引用图。
- 未逐行通读全部 1003 个 `.cs`，但 §5-18 的"零消费者"结论均以全仓 `grep` 计数为准（含 `Tests/`）。
- 建议下一轮：① 先定"三层属性"的唯一权威（`BasicAbilities` vs 六维）再动手；② 角色级疾病与人口级疾病二选一或明确分工；③ 补 `achievements/accessionDay` 等入档字段；④ 修 `Sociability` 或删维度。
