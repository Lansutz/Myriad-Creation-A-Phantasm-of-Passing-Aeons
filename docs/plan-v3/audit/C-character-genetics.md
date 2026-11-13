# 角色 / 家族 / 遗传 / 心理 / 人际关系 审计（2026-11-09）

## 0. 范围

**逐行读取（路径相对 `MyriadCreation/`；行数为实测 `Measure-Object -Line`）**
**引用约定**：每个文件首次出现给全路径，之后用 `…<程序集>/<文件>.cs:行号`。

| 目录/文件 | 规模 | 读法 |
|---|---|---|
| `Assets/Scripts/Simulation/MyriadCreation.Simulation.Genetics/` | **26 文件 / 1836 行** | 全读。核心：`GeneticsEngine.cs`(616)、`GeneticsCatalog.cs`(390)、`GeneticsContentManager.cs`(190)、`LegacyDnaAdapter.cs`(157)、`GeneticsAppearance.cs`(138)；其余 21 个为 DTO/枚举（10–54 行） |
| `…Simulation.Systems/`：`CharacterManager.cs`(1086)、`CharacterSchedule.cs`(34)、`DnaSystem.cs`(428)、`BodyPartSystem.cs`(487)、`CharacterDiseaseSystem.cs`(990)、`DiseaseSystem.cs`(375)、`EpithetSystem.cs`(685)、`EvaluationSystem.cs`(92)、`SuccessionSystem.cs`(431)、`CharacterKnowledgeRegistry.cs`(45)、`KnownCharacterInfo.cs`(11)、`CharacterLocationService.cs`(55)、`CharacterResearchManager.cs`(77)+`CharacterResearchData.cs`(78)、`RelationProjection.cs`(129) | 16 文件 / 约 4600 行 | 全读 |
| `…Simulation.State/`：`CharacterData.cs`(757)、`FamilyNode.cs`(192)、`MentalHealthSystem.cs`(128)、`PersonalityTrait.cs`(56)、`AchievementRecord.cs`(50)、`CharacterRelation.cs`/`CharacterBond.cs`/`ActiveCharacterDisease.cs`/`DnaData.cs`/`DnaExpression.cs`/`CharacterAISelector.cs`，及 `RealmData.cs`(统治者指针段) | 12 文件 | 全读 |
| `…Simulation.Definitions/`：`PersonalityTraitDatabase.cs`(1236／**108 条硬编码特质**)、`PersonalityArchetype.cs`(168) | 2 文件 | 全读 |
| `…Simulation.Actors/` | **17 文件 / 1765 行**（`MapActor.cs` 352、`MapActorManager.cs` 331、`CampManager.cs` 281、12 个 AI） | 全读 `MapActor.cs`，其余按"是否碰 CharacterData"定向读 |
| `Assets/StreamingAssets/Base/`：`Dna/DnaDefs.json`(729B)、`CharacterTemplate/CharacterTemplates.json`(1096B)、`MentalHealth/MentalHealthDefs.json`(705B)、`FamilyTradition/FamilyTraditions.json`(3243B) | 4 JSON | `ConvertFrom-Json` 解析计数 |
| `…Infrastructure.Save/`：`CharacterSaveData.cs`(803)、`WorldEntitySaveData.cs`(家族段 336–390)、`SaveEntityBridge.cs`(159–199)、`SaveSystem.cs`(573–600) | 4 文件 | 相关段全读 |
| UI：`Assets/UI/Common/UIManager.cs`(1157 序列化段)、`UIManager.Panels.cs`(747)、`UI/MyriadCreation.UI.Presentation/SettlementPeopleText.cs`(95)、`…WorldState/CharacterDetailQueryAdapter.cs`(199)、`Assets/Scenes/Main.unity`(序列化引用) | — | 定向读 |
| 测试：`Assets/Tests/EditMode/` 命中 9 fixture —— `CharacterLocationTests`(2)、`CharacterPersistenceTests`(11)、`GeneticsContentAndHybridTests`(6)、`GeneticsEngineTests`(7)、`LegacyMigrationIntegrationTests`(4)、`RelationProjectionTests`(3)、`SexChromosomeSystemTests`(5)、`SuccessionEndToEndTests`(11)、`UiTileCharacterPanelsTests`(5) | **54 用例** | 读源码 + 解析既有结果 |

**未覆盖（明说，不假装）**
- `BarrierSystem.cs`：读头部 40 行判定为**地形通行度**（`CalculatePassability` 只碰 `TileData`），不属角色域，本报告不判。
- `RelationEndpointValidator.cs` / `RelationRegistry`（实体级关系，属他域）；`ThoughtManager.cs`（学派/思潮，属文化信仰域）。
- 测试**未重跑**：三态全部来自静态调用图；测试数字引用既有 `_fc_verify/test-editmode-3.xml`（`total=706 passed=706 failed=0`，`start-time=2026-11-08 16:47:18Z`，非本次执行）。
- prefab/场景内是否有代码外的 UI 注入未逐一验证，只查了 `Main.unity` 序列化引用 + `UIManager` 的 `AddListener` 路径。
- 未跑 Unity，无法确认实际开局角色数量/生育率，只按概率与过滤条件静态推演。

---

## 1. 系统清单

| 系统 | 三态 | 驱动证据（file:line） | 入口证据 | 存档证据 | 测试 | 一句话现状 |
|---|---|---|---|---|---|---|
| 角色日结 `characters.daily` | **在跑** | `GameWorld.Simulation.cs:821` → `…Systems/CharacterSchedule.cs:28-31` → `…Systems/CharacterManager.cs:1001-1023`（年龄/死亡/生育/饮食/心理/好感漂移） | 无玩家入口（纯规则驱动） | 全量经 `CharacterSaveData` | `CharacterPersistenceTests` 11 | 唯一每 tick 都在跑的角色总闸 |
| 成长（年龄） | **在跑** | `…State/CharacterData.cs:340-343`（`currentDay == birthDay` → `age++`） | — | `age/birthDay/birthYear` | 间接 | 只有"年龄+1"，无童年/教育/成年礼等阶段 |
| 生育 `Procreate` | **在跑（仅非婚路径）** | `CharacterManager.cs:436-489`：`AutoProcreate` 第一段要求 `spouseId>=0`（`:465`，**永不成立**），第二段同政权异性直接 `Procreate`（`:482`，概率 0.0004/日）；`Procreate` 本体 `:120-184` | — | 子代入 `_characters` + `memberIds` | 无专项测试 | 孩子会出生，但**没有婚姻**；`fertility` 不参与判定 |
| 婚姻 `Marry` | **空转** | 定义 `CharacterManager.cs:199`；`grep 'Marry\('` 全仓 **1 命中**（仅定义）。`LinkSpouses`（`:237`）/`CreateBond`（`:511`）只被它调用 | 无 | `spouseId` 恒 -1 | 无 | 全项目没人调用结婚 → 配偶/纽带/关系三条链全断 |
| 死亡 | **在跑** | `CharacterData.cs:344-372`（health 累积衰减 → `Die`）、`:646-651`；疫病另行 `…Systems/DiseaseSystem.cs:209-212` | — | `deathDay/deathYear` | 间接 | 自然死亡 + 疫病死亡都在跑 |
| 继承 / 摄政 | **半跑（新局断链）** | `GameWorld.Simulation.cs:822-825` → `GameWorld.Succession.cs:119` → `SuccessionSystem.cs:12`；候选池 = 家族成员 `SuccessionSystem.cs:331-352`。**缺**：`realm.monarchId` 在开局**从不赋值**（`…State/RealmData.cs:86` 默认 -1），只在读档回填 `SaveSystem.cs:589-591` 或继承成立时写 `SuccessionSystem.cs:53`；`GetSupremeRulerId` 返回 -1 → `ExecuteSuccession` 在 `:20-24` 早退 | 无 | `RealmDTO` 存 monarch/consul/regent（家族段见 §2） | `SuccessionEndToEndTests` 11、`RegencyTests` | **未读档的局里继承永不触发**；测试都手工 `_realm.monarchId = 100`（如 `SuccessionEndToEndTests.cs:55`）掩盖了这一点 |
| 家族树 `FamilyNode` | **半跑** | 写 `memberIds` 仅 3 处：`CharacterManager.cs:176`（出生）、`:502`、`:996`。读：`SuccessionSystem.cs:232,338`、`CharacterDetailQueryAdapter.cs:39-42` | UI：家族树面板（打不开，见下） | `FamilySaveData`（`WorldEntitySaveData.cs:340-390`，仅主干） | 无专项 | 家族只做"继承候选池"，没有家族级数值生命周期 |
| 家族传统 | **空转** | `…State/FamilyNode.cs:130` `AddFamilyTradition` **零调用者**（`grep` 1 命中=定义）；`:172` `GetTraditionEffect` 同；字段 `familyTraditions` 是 `[NonSerialized]`（`:31-32`）→ 不入档 | 无 | **不入档** | 无 | JSON 9 条 + 互斥/革新校验逻辑全在，但没有生产者也没有消费者 |
| 家族分支/纹章/世代 | **不存在 / 空转** | `CreateBranch`（`FamilyNode.cs:55`）、`GetAllMembers`、`GetGenerationDepth`、`CalculateTotalPrestige`、`coaPattern`(`:43`) 全部零外部调用 | 无 | `branches` 明确不入档（`WorldEntitySaveData.cs:337`） | 无 | 递归家族树只有壳 |
| 基因组遗传 | **在跑** | `CharacterManager.cs:163-170`（`GetGenome`→`DnaSystem.InheritGenome`→`.genome`）；`…Systems/DnaSystem.cs:200-205`→`GeneticsEngine.Reproduce`；`…Genetics/GeneticsEngine.cs:160-205`（减数分裂/交换/突变/inbreeding） | 无 | `GenomeInstance` 入档（`CharacterSaveData.cs:184,247,527`） | `GeneticsEngineTests` 7、`SexChromosomeSystemTests` 5 | 真能算出子代等位基因（多基因座+连锁交换），是目前遗传里唯一"有结果"的部分 |
| 表型 / 外观（Phenotype） | **空转** | `CharacterData.cs:275-282` `RefreshAppearance` **零调用者**（`grep` 1 命中=定义）；因此 `GeneticsEngine.ComputePhenotype`（`:399-496`）在生产链上从不执行；`appearance` 恒默认值 | 无（`charDnaText` 只印文化/信仰，`UIManager.Panels.cs:551-553`） | **不入档**（`CharacterSaveData.cs:21-22` 自述"派生缓存"） | `GeneticsContentAndHybridTests` 6 | "从基因组算肤色/瞳色/表型分数"的能力有，但没接上任何调用点 |
| 六基因座 `DnaData` + 表达 | **在跑** | 写：`CharacterManager.cs:783`（`DnaSystem.GenerateRandom`）/`:167`；表达：`:804` `DnaSystem.ComputeExpression`（`…Systems/DnaSystem.cs:263-332`）→ 寿命/智/武/抗性/魅力/才能缺陷 | — | `dna` **不入档**（`CharacterSaveData.cs:529` 置 null） | `LegacyMigrationIntegrationTests` 4 | 角色出生时数值全从这条 legacy 通路来，与新基因组双轨 |
| 才能 / 缺陷 | **半跑（仅出生时一次性）** | 判定 `DnaSystem.cs:308-332`（AA→25% 才能，aa→90% 缺陷，Aa→携带）；应用 `CharacterManager.cs:748-758,915-935` | 无（无 UI、无成长再判定） | 只存结果 `dnaExpression` 的间接后果（属性），`dnaExpression` 本身不入档 | 无 | 出生即定终身，`TalentDefectDef` 8 条来自 `Dna/DnaDefs.json` |
| 身体部位 `BodyPartSystem` | **空转（只建表）** | `InitializeBodyParts` 由 `CharacterManager.cs:805` 调用；`DailyUpdate`（`:349`）**零调用者**（`grep BodyPartSystem.DailyUpdate` = 0）；`GetPartFunction/GetVisionFunction/GetHearingFunction` 仅文件内互相调用 | 无 | **入档**（`CharacterSaveData.cs:150-175,382-...`，3 层拍平） | `CharacterPersistenceTests` 覆盖往返 | 每个角色都建 12+ 个部件的默认表并存进档，**没有任何规则读它**（无伤残/失明/失聪后果） |
| 个体疾病 / 伤残 `CharacterDiseaseSystem` | **空转** | `new CharacterDiseaseSystem` **0 命中**；`DailyTick(CharacterData...)`(`:699`)、`InfectCharacter`(`:861`)、`CombatInjury`(`:890`)、`CheckChronicDiseaseOnset`(`:918`) 全零外部调用 | 无 | `activeDiseases`/`impairments` **入档**（`CharacterSaveData.cs:132-148`）但运行态恒空 | `CharacterPersistenceTests` 手工造数据断言往返 | 疾病分期/伤残进展/战斗受伤整套 990 行代码从未运行 |
| 群体疫病 → 角色 | **在跑** | `GameWorld.Simulation.cs:796` → `DiseaseSystem.cs:118-130,179-216`：按地块感染、`individualResistance`(`:203`) 折算伤害、`health<=0` → `Die` | 无 UI（灾情在别域） | 间接（health/death） | `FragmentationClimateConsistencyTests` 等间接 | **遗传→抗性→疫病死亡**是唯一有真实因果的遗传出口 |
| 心理 / 精神健康 | **在跑（无 UI）** | 写：`CharacterManager.cs:620-672`（高压 90 天→抑郁/焦虑；dread>80→偏执；≥70 岁低学识→失智；低压力 120 天康复）；读：`CharacterData.cs:321-331`（六维修正）→ `GetEffectiveStats` → 研究/派系/法令/行军；`EpithetSystem.cs:90-101`（"疯子"） | **无**：`CharacterDetailDto` 不含该字段（`CharacterDetailQueryAdapter.cs:55-93`） | `mentalDisorderId/highStressDays/lowStressRecoveryDays` 入档 | 无专项 | 通电，但玩家看不到；`dread` 无写入源 → 偏执分支恒不触发 |
| 特质 `PersonalityTrait` | **半跑** | 生产端仅 `GrantSignatureTraits`（`CharacterManager.cs:256-282`，八轴阈值 ±40/±60/±80 → `<轴>_<1|2|3>`）＋读档恢复；消费端 `GetEffectiveStats`（`CharacterData.cs:299-332`）→ 4 处真实消费者 | 显示：`charPersonalityText`（面板不可达） | 只存 `traitIds`（`CharacterSaveData.cs:113-114,266-277`） | `CharacterPersistenceTests` | 108 条特质表里**只有 48 条（8 轴×2 极×3 级）可达**；`ApplyDailyEffect`/`OnAcquired` 基类空实现，无子类 |
| 性格七维 | **半跑** | 出生遗传父母均值±10（`CharacterManager.cs:893-913`）、日抖动 ±0.02（`CharacterData.cs:360-365`）；`ModifyPersonality`（`:714`）**零外部调用** → 事件/教育无法改性格；性格→特质→六维（真实）；性格→`AIPersonality` **无通路**（AI 性格是 `AIManager.cs:15` 随机独立生成） | 显示同上 | `personality[8]` 入档 | 无专项 | 性格只间接影响数值，不影响 AI 决策 |
| 人际关系 `CharacterRelation` | **空转** | 唯一写入源 `CharacterData.cs:672-684` `ModifyRelation`，其角色侧调用者只有 `CharacterManager.cs:239-240`（`LinkSpouses`），而 `LinkSpouses` 只由 `Marry` 调用 → **链断**；`PersonalityOpinionDrift`（`:674-694`）要求 `relations` 已有条目，因此恒空转 | 无 | `relations` 全字段入档（`CharacterSaveData.cs:116-126,279-307`） | `CharacterPersistenceTests` 手工注入 | `opinion/trust/fear/romanticAttraction/history` **只写不读**（`grep '\bopinion\b'` 全仓仅定义+写+存档+测试），永不进入任何判定 |
| 纽带 `CharacterBond` | **空转（只有存档往返）** | 生成仅 `CreateBond`（`CharacterManager.cs:511`）← `LinkSpouses`；`combatBonusWhenTogether/diplomacyBonus/stressReduction/sharedFate`（`…State/CharacterBond.cs:20-26`）**除存档 DTO 外零读取** | 无 | `CharacterBondPersistence`（`SaveEntityBridge.cs:169-199`） | `CharacterPersistenceTests` | 纽带只存在于存档里 |
| 相识 / 知识 `CharacterKnowledgeRegistry` | **半跑** | 写 3 处：`CharacterManager.cs:990,994`（宫廷）、`GameWorld.Simulation.cs:197`（授职）；读：`CharacterLocationService.cs:47` → `SettlementPeopleQueryAdapter` → 聚落人口面板 | 聚落"人物"列表（真实可点） | `KnownCharacterSaveData`（`SaveEntityBridge.cs:127-167`） | `CharacterLocationTests` 2、`UiReadModelExtensionTests` | 唯一影响"玩家能看见谁"的角色侧机制 |
| 谥号 / 盖棺评价 `EpithetSystem`+`EvaluationSystem` | **半跑** | 唯一调用点 `GameWorld.Succession.cs:304-331`（死君主）；因新局 `monarchId=-1` 继承不触发（见上）→ **实际上要读档后才可能发生**；其输入 `AchievementRecord` 21 个计数中 **13 个全仓无写入**（见 §2） | 编年史文本 | `epithet/posthumousTitle/epithetEvaluated` 入档 | 无专项 | 条件分支大部分永假；可达的多为"疯子/无冠者"这类兜底 |
| 角色研究 `CharacterResearchManager` | **空转** | `new CharacterResearchManager` **0 命中**；`:29` `StartCharacterResearch`、`:60` `DailyTick` 零调用 | 无 | 无 DTO | 无 | 真正的研究走 `ResearchPlanSystem`（`…Systems/ResearchPlanSystem.cs:267-279` 读 `GetEffectiveStats` 的 scholarship） |
| 威望 / 恶名 | **空转（只降不升）** | `ModifyPrestige`（`CharacterData.cs:703`）/`ModifyNotoriety`(`:718`) **各 1 命中=定义**；`prestige` 每 tick `-0.05`（`:367`）且出生默认 0（`:133` 未在 `CreateCharacter` 赋值）→ **全角色恒 0**。读者：`FactionManager.cs:236`、`SuccessionSystem.cs:248,385` | 显示 `charStatsText`（面板不可达） | 入档 | 无 | 威望/恶名体系没有生产端，摄政/共和打分退化为比年龄 |
| `MyriadCreation.Simulation.Actors`（17） | **半跑（仅运输/领队）** | 与角色唯一接口：`GameWorld.Simulation.cs:687-699`（传令官 `TryAddMember`+`SetLeader`）、`MapActor.cs:291-330`（领队影响行军）、`:383-396`（随队伤病 drain health）；其余 12 个 AI 只碰 `TileData`/货物 | 无 | `WorldEntitySaveData` 单位段 | 无 | 是**移动/编队**系统，不是角色系统；角色只是货物式的 transport 成员 |
| 角色面板 / 家族树 UI | **不存在（不可达）** | `UIManager.cs:173` `characterPanel` 在 `Main.unity:7763` 序列化为 `{fileID: 0}`（null），而 `CharacterPanel` 物体本身 `m_IsActive: 0`（`Main.unity:9338`）→ `OpenCharacterPanel`（`UIManager.Panels.cs:573-578`）无法显示任何东西；`familyTreeOpenButton` 同样为 0（`Main.unity` 序列化段），`OpenFamilyTreePanel`(`:302`) 仅能由该按钮触发 → 家族树也打不开 | **0 个可用入口** | — | `UiTileCharacterPanelsTests` 5（只测 DTO→文本，不测接线） | 6 个 Panel 里没有角色/家族面板；角色信息只能从"聚落人物"列表看到姓名/角色/标签（`SettlementPeopleText.cs:31-47`） |

**§1 小结：七个核心问题的答案**
1. **生命周期**：生成（`CreateInitialRulers`，仅每政权统治者+配偶，`CharacterManager.cs:976-999`）→ 成长（仅 `age++`）→ 婚育（**无婚姻**，仅 `AutoProcreate` 非婚生育）→ 死亡（health/疫病）→ 继承（**新局因 `monarchId=-1` 不触发**）。遗传里"能算出结果"的只有基因组减数分裂/突变与六基因座表达（决定出生属性、抗病、寿命）；**表型/外观/身体部位只存字段**。
2. **属性/特质消费者**：真实消费者是 `GetEffectiveStats` 的 4 个下游（`ResearchPlanSystem.cs:269,277` 突破概率、`FactionManager.cs:235,257` 派系力量、`DecreePropagationSystem.cs:203` 法令传播、`MapActor.cs:341` 行军速度）。**无人读**：`FindBestGeneral/FindBestRuler/CalculateCommandAbility/CalculateRuleAbility/CalculateOverallAbility/GetCharactersByRole`（各 1–2 命中，全在定义处）→ 六维不参与选将/选君；`DetermineEconomicalArchetype`（`CharacterData.cs:586`）零消费者。
3. **人际关系**：唯一写入者是 `Marry`→`LinkSpouses`（无调用者），`CharacterBond` 无读取者 → **不影响任何可观察结果**；关系史 `history` 只被写进存档。
4. **心理/精神健康**：**通电**（`CharacterManager.cs:620` 写、`CharacterData.cs:321` 读、入档、有 Localization 名），但无 UI、`dread` 无写入源、`opinionDecayMult` 无读取者。
5. **家族树与继承/头衔**：家族树**有调用**（继承候选池 + 摄政推举 + `InheritanceHeirSelection.cs:30-33,68-76` 的家族/同支排序）；头衔侧仅 `GameWorld.Succession.cs:56-78` 一处转交（法律侧另域）。家族传统与继承/法律**无任何联动**。
6. **存档范围**：入档见 `CharacterSaveData.cs:28-184`（身份/归属/血缘/六维/状态/成就/`genome`/特质 id/关系/技能/疾病/伤残/身体部位/性格）。**不入档**：`dna`、`dnaExpression`、`appearance`、`identity`、`familyTraditions`、`branches`、`parentFamily`、地图 AI 运行时对象、`ThoughtManager`。**读档重建**靠 `SaveEntityBridge` 反射注入 `_characters/_families/_nextId`（`SaveEntityBridge.cs:26-45`）+ `CharacterSaveData.ToCharacter()`（`:467-529`）+ bodyParts 默认重建（`:648-655`）+ `RestoreBonds`（`CharacterManager.cs:540-559`）+ `monarchId` 回填（`SaveSystem.cs:589-591`）。
7. **零调用者清单**：`CharacterDiseaseSystem`（整类）、`CharacterResearchManager`（整类）、`PersonalityArchetype`（整类）、`RelationProjection`（测试外）、`BodyPartSystem.DailyUpdate`、`CharacterManager.Marry/FindBestGeneral/FindBestRuler/Calculate*Ability/GetCharactersByRole`、`FamilyNode.CreateBranch/GetAllMembers/GetTotalMemberCount/GetGenerationDepth/CalculateTotalPrestige/FindCharacterFamily/AddFamilyTradition/RemoveFamilyTradition/GetTraditionEffect`、`CharacterLocationService.EnterSettlement/LeaveSettlement`、`CharacterData.ModifyPrestige/ModifyNotoriety/AddDread/AddStress/ModifyPersonality/DetermineEconomicalArchetype/GetEconomicalArchetypeDisplay/GetPersonalityTagsDisplay/RefreshAppearance`。

---

## 2. 数据模型现状

**双遗传真源（并存，且分工不一致）**
- legacy `DnaData`（6 基因座 `DnaLocus`，`Allele` 二态）：出生时由 `DnaSystem.GenerateRandom` 生成 `CharacterManager.cs:783`，表达为属性 `:804`；遗传走 `DnaSystem.Inherit`(`:220-261`)。**不入档**（`CharacterSaveData.cs:529` 读档置 null）。
- 新 `GenomeInstance`（多基因座+交换+突变）：`CharacterManager.cs:166-170`，入档（`:184`）。
- **读档后行为漂移**（两处，P1）：① `CharacterData.cs:344-345` 的衰老曲线分支按 `dna != null` 取 `expectedLifespanYears`，读档后 `dna==null` → 固定按 50/100 计算，**同一年龄的掉血速率变了**；② `CharacterData.cs:271` 对 `genome==null` 的角色用 `new Random(characterId*397+age)` 造创始人基因组 —— 读档前该角色是从 `dna` 推导（`DnaData.cs:45-51`），读档后变成另一套随机基因组 → **其后续子女的等位基因整体改变**。

**只写不读 / 无生产者的字段（"承诺未兑现"）**
- `CharacterRelation.opinion/trust/fear/romanticAttraction/history`：写于 `CharacterData.cs:672-684`、`CharacterManager.cs:688`；**零读取者**（全仓 `\bopinion\b` 仅：定义、两处写、存档、测试）。
- `character.identity`（`IdentityProfile`）：只有 `LegacyIdentityMapping.FromLegacy` 写入（`CharacterData.cs:219-222`），全仓无读取者，且不入档。
- `CharacterData.fertility`：只入档，不参与 `Procreate`/`AutoProcreate` 任何判定。
- `CharacterData.dread`：写入器 `AddDread`(`:705`) **零调用** → 恒 0，`CharacterManager.cs:636` 的"深度恐惧→偏执"分支永假。
- `CharacterData.conquests/warsWon/diplomaticVictories/cultureWorks/religiousDeeds/expeditions/vigilanceActs`（`:71-83`）：**七个字段零引用**（写作/读取皆无），与 `AchievementRecord` 同名计数重复且都没有生产端。
- `AchievementRecord`（21 字段）：有写入的只有 7 个 —— `warsWon/defeatedBattles/defensiveWins`(`GameWorld.Succession.cs:267-281`)、`massacres`(`RaidSettlementCommandHandler.cs:37`)、`youngAccession/ruledUnderRegency`(`:137,141,176-177`)、`reignYears/regionalInfluence`(`:313,316`)；**无写入的 13 个**：`conquests/cultureActs/poetryActs/religionActs/faithChanges/rebellions/usurpedThrone/canonized/schemesSucceeded/threatsResolved/expeditions/lostAllLands/famineUnderRule`。`EpithetSystem` 与 `EvaluationSystem` 大量以这些字段为条件（如 `EpithetSystem.cs:43,51,59,102,225,241,265,353,385,436,444,476,500`）。
- `CharacterData.bodyMarks`：只入档（`CharacterSaveData.cs:87,251`）+ 被 `EpithetSystem` 读（`HasBodyMark`），**无写入者** → "美人"等条件永假。
- `PersonalityTrait.conflictingTraits/requiredTraits`：`IsCompatibleWith`(`PersonalityTrait.cs:48`) **零调用者** → 冲突/前置约束不生效；`ApplyDailyEffect`/`OnAcquired`/`OnRemoved` 无任何子类（`grep ': PersonalityTrait'` = 0）→ 空实现。
- `MentalDisorderDef.opinionDecayMult`：仅定义+出厂赋值（`MentalHealthSystem.cs:51`），无读取者。
- `PersonalityTraitDatabase` 108 条中 **60 条不可达**（除 8 轴×2 极×3 级外的 `wrathful_*/ambitious_*/diligent_*/sadistic_*/greedy_*` 等，生产端只有 `GrantSignatureTraits`）；`CharacterData.cs:595-597` 还引用了**表里不存在**的 id `"conqueror"`/`"greatest_of_khans"`（`grep greatest_of_khans` 全仓 1 命中=本行）。
- `PersonalityArchetype.cs`（168 行）：**零引用**。
- `CharacterData.charm`：出生时由 `dna.GetLocus(Appearance)` 给 ±5/2/-3（`CharacterManager.cs:826-834`），消费者仅 `GetEffectiveStats`。
- `birthYear` 恒为 0（`CharacterManager.cs:793`），年龄不按年推算。

**真实消费链（写明"谁读它→影响什么"）**
- `traits`/`mentalDisorder` → `GetEffectiveStats` → ①学界突破概率 ②派系力量 ③法令传播效率 ④行军速度。
- `individualResistance` ← `dnaExpression.resistanceOffset` → `DiseaseSystem.cs:203-209` 感染伤害/死亡。
- `expectedLifespanYears` ← `longevityOffsetYears` → `CharacterData.cs:344-348` 衰老掉血。
- `familyId`/`memberIds` → 继承候选池、摄政推举、继承分支排序。
- `aiCategory` → `UnitAIFactory.cs:48-51`（仅传令官等地图单位领队生效）。

---

## 3. 内容数据现状

| 内容包 | 条目 | 被代码引用 | 结论 |
|---|---|---|---|
| `Dna/DnaDefs.json` | **8**（4 才能 + 4 缺陷，`defs[]`） | `DnaSystem.cs:126,145,164`（`ContentResolvers.TalentDefects`），`ContentDefinitionIndex.cs:248-251,390-395` 加载 | **8/8 生效**（注意 JSON 无 `name/description`，仅 `id/stat/amount`，界面/日志显示依赖硬编码兜底表 `DnaSystem.cs:34-112`） |
| `CharacterTemplate/CharacterTemplates.json` | **6**（commoner/noble/military/scholar/ruler/spouse） | 只有 `tmpl_ruler`、`tmpl_spouse` 被取用（`CharacterManager.cs:984-985`） | **2/6 生效**；其余 4 个模板无任何创建点，因为角色创建只有"统治者/配偶/新生儿"三条路 |
| `MentalHealth/MentalHealthDefs.json` | **5**（depression/anxiety/paranoia/delirium/dementia） | `ContentResolvers.MentalDisorders` 唯一读取点 `MentalHealthSystem.cs:85` | **5/5 生效**；JSON 无 `name/desc`，靠 `Localization` 的 10 个键（`depression_name` … 见 `Base/Localization/zh-Hans.json`）。注意 `delirium` 无任何写入方（从不授予），实际可达 4 种 |
| `FamilyTradition/FamilyTraditions.json` | **9**（`traditionId` + `effects` + `incompatibleWith` + `requiredInnovations`） | 加载正常（`ContentDefinitionIndex.cs:238-239,370-379`），唯一读者 `FamilyNode.cs:136,177`，而这两个方法**零调用者** | **0/9 生效**：内容、互斥关系、革新门槛全是死数据 |
| `…Definitions/PersonalityTraitDatabase.cs` | **108**（硬编码，非 JSON） | 生产端 48 条可达；`Get` 被 `CharacterManager.cs:277,292`、`CharacterSaveData.cs:563` 调用 | 44% 可达 |
| `AchievementRecord` 条件 | — | `EpithetSystem`/`EvaluationSystem` 读 21 字段 | 13 字段无写入 → 条件永假 |

---

## 4. 缺口清单（按影响排序）

| # | 缺口 | 影响 | 证据 | 严重度 |
|---|---|---|---|---|
| 1 | `Marry` 零调用者 | 婚姻/配偶/婚后生育/纽带/关系全部不成立；`spouseId` 恒 -1；`AutoProcreate` 第一段死代码 | `grep 'Marry\('`=1（仅 `CharacterManager.cs:199` 定义）；`:465` 依赖 `spouseId>=0` | **P0** |
| 2 | 新局 `realm.monarchId` 从不赋值 | 不读档的局里：继承/摄政/谥号/盖棺评价永不触发；`GetRealmRuler` 返回 null 使战功计数不涨 | `RealmData.cs:86`（默认 -1）、`SuccessionSystem.cs:20-24` 早退、唯一回填 `SaveSystem.cs:589-591`；测试手工赋值 `SuccessionEndToEndTests.cs:55` | **P0** |
| 3 | 角色间关系无写入源且无读取者 | 人际系统 100% 空转；`opinion` 等 5 字段只写不读 | §2；`ModifyRelation` 角色侧调用者仅 `CharacterManager.cs:239-240` | **P0** |
| 4 | 读档后 `dna=null` 改变衰老曲线 | 同一角色读档前后掉血速率不同（存档不一致） | `CharacterData.cs:344-345` vs `CharacterSaveData.cs:529` | **P1** |
| 5 | 读档后 `genome` 派生方式改变 | 读档前后同一角色的等位基因不同 → 其后代遗传结果改变 | `CharacterData.cs:265-273`、`DnaData.cs:45-51` | **P1** |
| 6 | 角色/家族 UI 不可达 | 玩家看不到六维、特质、性格、心理病、家族树、遗传；只有聚落人物列表给出姓名+角色标签 | `Main.unity:7763`（`characterPanel: {fileID: 0}`）、`Main.unity:9338`（`m_IsActive: 0`）、`familyTreeOpenButton` 序列化为 0、`UIManager.Panels.cs:573-578,302-306` | **P1** |
| 7 | 108 特质仅 48 可达 + 引用不存在的 traitId | 大量称号/原型条件永假；`DetermineEconomicalArchetype` 实际退化为"Balanced" | `CharacterManager.cs:256-282`（唯一生产端）、`CharacterData.cs:595-597`（`conqueror`/`greatest_of_khans` 不在表内） | **P1** |
| 8 | `AchievementRecord` 13/21 无写入 | 谥号与评价体系大面积死分支；`EvaluationSystem.CalculateScore` 主要项恒 0 | §2 清单；`EpithetSystem.cs:43,51,...` | **P1** |
| 9 | 威望/恶名无生产端 | `prestige` 恒 0 → 摄政推举/共和排序退化为比年龄；派系力量少一项 | `ModifyPrestige`/`ModifyNotoriety` 各 1 命中（定义），`CharacterData.cs:367` 只降 | **P1** |
| 10 | 表型/外观链路未接 | 遗传算得出肤色/瞳色/表型分数，但无调用点、无 UI、不入档 | `RefreshAppearance` 1 命中（定义 `CharacterData.cs:275`） | **P2** |
| 11 | 身体部位/个体疾病/伤残三件套从未运行，但全部入档 | 存档里这三段永远是默认/空；玩家侧"伤残/患病"不存在 | `new CharacterDiseaseSystem`=0、`BodyPartSystem.DailyUpdate`=0、`CharacterSaveData.cs:132-175` | **P2** |
| 12 | 家族传统 9 条 + 校验 + 革新门槛全死，字段不入档 | 家族差异化能力缺失 | `FamilyNode.cs:130,167,172`（各 1 命中）、`familyTraditions` `[NonSerialized]` `:31` | **P2** |
| 13 | `dread` 无写入源、`ModifyPersonality` 无外部调用 | 心理病只剩"饿出来的抑郁/焦虑/老年失智"三种来源；外部事件无法塑性格 | `AddDread` 1 命中、`ModifyPersonality` 3 命中全在定义 | **P2** |
| 14 | 零调用类：`CharacterResearchManager`、`PersonalityArchetype`、`RelationProjection`、`BodyPartSystem.DailyUpdate`、`CharacterManager.FindBestGeneral/FindBestRuler/Calculate*Ability/GetCharactersByRole`、`FamilyNode.CreateBranch/GetAllMembers/GetTotalMemberCount/GetGenerationDepth/CalculateTotalPrestige/FindCharacterFamily`、`CharacterLocationService.EnterSettlement/LeaveSettlement` | 死 API 面大，易被误当"已实现" | 逐个 `grep` 命中数=1（仅定义），见上文各行 | **P2** |
| 15 | 官职/行政区授职要求 `role ∈ {Noble,Military,Scholar,…}`，而全项目角色 role 只有 Ruler/Spouse/Commoner | 新局里行政区持有者永远为空（`AssignAdminHolders` 过滤 `GameWorld.Simulation.cs:182` 恒不成立），角色不会因为当官获得位置/公共人物身份（`EnsureOfficeHolders` 的兜底只改 `aiCategory`，不改 `role`，`GameWorld.Simulation.cs:319-337`） | `CharacterManager.cs:986,991,169`（三个创建点）+ `GameWorld.Simulation.cs:182` | **P1** |

---

## 5. 我核实不了的事

1. **运行期真实数据**：开局每政权几个角色、多久生出第一个孩子、多久死第一任统治者 —— 只按概率与过滤条件静态推演，未跑 Unity。
2. **测试是否仍然全绿**：引用的是既有 `_fc_verify/test-editmode-3.xml`（706/706），本次未重跑；也无法确认是否新增了未提交的测试。
3. **`characterPanel`/`familyTreeOpenButton` 为 null 是"设计未接"还是"序列化丢失"**：只能确认 `Main.unity` 当前序列化值为 0，无法判断历史意图。
4. **是否有代码外（prefab/Inspector 手搭、编辑器脚本）路径**能打开角色面板或注入 `CharacterDiseaseSystem`；只验证了 C# 与 `Main.unity`。
5. **`GeneticsContentManager`（190 行，含校验/内容包）是否会被 mod 流程调用**：仓内唯一引用者是测试 `GeneticsContentAndHybridTests`，mod 运行期入口未追（属 Modding 域）。
6. **`EvaluateDeadRulerLife` 在"读档后"是否真的能跑通**：依赖 `accessionDay>=0`（`GameWorld.Succession.cs:310`），而 `accessionDay` 只在 `CheckRulerSuccessions` 内写（`:134`），新局路径不写 → 推演为"读档后也常驻 -1，谥号仍不出"，但未实跑确认。
