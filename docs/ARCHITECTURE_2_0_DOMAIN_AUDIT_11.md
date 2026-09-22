# Architecture 2.0 — Domain Audit 11
## README 同步复核：文化 / 宗教 / 政治 / 军事 / 灾害 / 角色 / 革新升级

本轮继续逐段阅读 main/README，并与现有代码事实、既有 2.0 审计及用户后续升级同步对照。

## 1. 最重要结论：革新系统已经发生过一次有规模的升级

README 旧模型强调：两级分类、技术/思维/制度/传统、个人研究者、实践积累、物产前置、JSON 数据驱动、InnovationField。

当前代码已经出现明显超过 README 原始模型的结构：InnovationDef、InnovationField、InnovationDomain、InnovationTree、InnovationProgress、InnovationKnowledgeSystem、CharacterResearchSystem、ResearchPlanSystem、CultureData.innovationAffinities，以及 GovernmentReform / SocialClassAvailability 对 InnovationTree 的依赖。

尤其 InnovationKnowledgeSystem 已明确把“社会已有知识”与“个人掌握程度 / 实践积累”分开；ResearchPlanSystem 又把个人突破后的研究/验证计划与正式革新解锁分开。

因此当前状态明确为：README 旧版设计基础 → 后续发生规模化升级 → 当前代码已有多层革新知识/个人研究/分类/推进结构 → Architecture 2.0 必须重新建模，而不是恢复 README 旧结构。

## 2. 革新与 Plan

当前边界应理解为：Innovation 是知识/能力状态；CharacterResearch 是个人掌握与实践；ResearchPlan 是个人研究/验证计划；PlanSystem 不是 Innovation 的权威状态容器。

这与用户已经确认的“Plan 系统管的是比较个人的东西”一致。

## 3. 革新与 Culture

CultureData 已出现 innovationAffinities，说明 Culture 不是 Innovation 的所有者。更合理的是 Culture 对某些 InnovationField 提供亲和/修正，而不是直接拥有全部 Innovation State。

后续需继续检查文化亲和、文化阶段、传统与 Innovation 的关系，避免 CultureData 同时成为 Definition、State、Modifier、Political Default 的超级实体。

## 4. 革新与 Politics

GovernmentReform 使用 InnovationTree，SocialClassAvailability / ClassEmergenceEvents 也读取 InnovationTree。这支持：Innovation 提供能力/条件/解锁；Politics/Economy 消费这些条件并改变自身状态。不能反过来让 Politics 成为 Innovation 的所有者。

## 5. Culture

README 七大板块包括生计主轴、移动模式、葬俗、原始崇拜权重、物质风格、象征实践、环境适应。当前 CultureData 有“考古文化包七大板块”的明确代码证据，因此这部分属于 README 设计与代码实现高度对应。

但文化阶段、国家形成、文明判定、革新亲和又跨越多个领域，后续必须继续拆 Culture Definition / State / Modifier / Relation。

## 6. Religion

README 的宗教设计包括组织谱系、思想谱系、教统、教义、神祇、仪式、神职、圣地、法律、传教、思想流派和个人化信仰。当前已确认 ReligionSystemTests 等代码证据，但测试存在不能证明 README 全部机制已经实现。

因此当前标记为：README 大规模设计说明；代码存在部分证据；需要逐文件继续审计。尤其要防止 Faith、Religion、ReligiousOrganization、Tradition、Doctrine、Character private faith 混成一个实体。

## 7. Politics

README 明确拒绝简单的君主制/共和制二分，而采用最高权力、权力交接、中央机构、地方治理、继承法、官职等多个维度；王国/帝国属于外交头衔而非机制本体。

当前代码已经存在 RealmData、GovernmentReform、OfficeTitle、PolityComponentInnovations 等结构。因此 Political Domain 仍需拆分政治状态、制度定义、Title、Office、Relation。

## 8. 行国 / 城国

README 把行国与城国定义为两种政治组织逻辑，而不是 Settlement 类型。行国可以控制定居城市，王庭可移动，不需要占据每一寸土地，并可发生定居化。

因此 SettlementCategory 与 Political Organization 必须保持独立：Camp/Burg/Outpost 是 Settlement 维度；行国/城国是 Political 维度。

## 9. Political Control 与 MapCell

README 明确前现代不存在现代固定边界，并区分势力范围、通行管制、边境摩擦、节点控制等现象。因此 TileData.ownerRealmId / occupyingRealmId 不能自动解释成完整领土边界。

后续至少需要区分法理/政治归属、实际控制、军事占领、通行权、聚落控制、区域影响。这些属于 Political / Distribution / Relation / Query，而不是 Geography 本身。

## 10. Military

README 明确山区高成本但非绝对不可通行；关隘可阻挡；堡垒产生区域影响；军队有行军、补给、征发/劫掠；营寨可升级为坞堡/聚落。

当前已确认 MilitaryMovementSystem 存在并实现高难度地形、Barrier、Fort 等移动逻辑。因此它是实现事实，但不应成为 MapCell 的权威状态。

目标仍是：Map/Geography 提供空间与地理事实；Structure/Network/Settlement 提供道路、关隘、堡垒等事实；Political/Control 提供控制与通行关系；MilitaryMovement Query 综合计算结果。

## 11. Disaster / Plague / Disease

README 明确区分群体层灾害瘟疫与个体层疾病：Disease 属于角色健康；Plague/Epidemic 属于人口/社会传播；Disaster 属于地区/群体灾害。

因此不能把 Disease、Plague、Disaster 全部塞进 Character Health Domain。灾害还受气候、水文、地质等条件驱动，并可影响人口、基础设施、农业、贸易和秩序。

## 12. Character

README 的角色系统远超过简单 CK3 clone：基本能力→专精→技能树；六维专精；性格四档；经济原型；本真/表现/期许；压力与人格漂移；威望/恶名；魅力；DNA；身体标记；社会/私人信仰；身体部位与子结构疾病；关系；家族；继承。

因此 Character Domain 后续必须独立审计，不能让 CharacterData 无限膨胀。

## 13. 本轮状态汇总

| 领域 | README状态 | 当前判断 |
|---|---|---|
| Innovation | 旧版设计，后续明确升级 | 已升级，必须重建边界 |
| Culture | 核心设计 + 部分实现 | 继续拆 Definition / State / Modifier |
| Religion | 大规模设计 | 部分可证，需逐项审计 |
| Politics | 多维设计 | 已有较强实现，但仍混合 |
| Xing-state / City-state | 高层政治设计 | Political，不是 Settlement |
| MilitaryMovement | README设计 + 代码实现 | 实现存在，需从 MapCell 权威状态中抽离 |
| Disaster | 群体层设计 | 独立于 Character Health |
| Disease | 个体层设计 | 向身体部位模型迁移 |
| Character | 大量明确设计 | 必须独立领域审计 |

## 14. 下一步

继续逐段读取 README 剩余的 AI、模组、存档、音频、开发路线，并同步进行 TradeCenter → TradeRoute → Road → Fort → Gate → SettlementControl → MilitaryMovement 的逐文件代码审计。

然后把结果合并进正式 Domain Boundary + Field Migration Matrix。本轮不创建最终 Domain 类，不做大规模迁移。