# Architecture 2.0 Field Migration Matrix

> 第一版字段迁移矩阵。状态只描述迁移方向，不执行迁移。证据来自 Audit 04–19。

## 状态定义

- **KEEP**：当前归属基本正确。
- **SPLIT**：当前字段混合多个领域，需要拆分。
- **MOVE**：未来 authority 明确迁移。
- **DERIVE**：未来从其他事实计算。
- **QUERY**：不再保存为领域 authority。
- **COMPAT**：旧字段暂时保留，仅作迁移/兼容输入。
- **UNRESOLVED**：证据不足，禁止迁移。

## Map / Geography

| Current field | Current authority | Target | Status | Notes |
|---|---|---|---|---|
| TileData.elevation01 | TileData | MapCell/Geography | KEEP | 地理事实 |
| TileData.slopeDegree | TileData | MapCell/Geography | KEEP | 地理事实 |
| TileData.terrainShade | TileData | MapCell/Geography | KEEP | 地理/渲染输入需再区分 |
| TileData.isLand/isCoast | TileData | MapCell/Geography | KEEP | |
| TileData.oceanTier/oceanDepth01 | TileData | MapCell/Hydrology | KEEP | |
| TileData.seaConnectId | TileData | Hydrology/SeaNetwork | SPLIT | 地理关系，不应成为政治状态 |
| TileData.isRiver | TileData | Hydrology | MOVE | |
| TileData.climate* | TileData | Climate/MapCell geography | SPLIT | 需区分基础地理与派生气候 |
| TileData.biome | TileData | Geography/Biome Query | SPLIT | 需明确是否可重建 |

## Political / Distribution

| Current field | Target | Status |
|---|---|---|
| ownerRealmId | Political legal affiliation relation | MOVE |
| occupyingRealmId | Military occupation relation | MOVE |
| provinceId | Political/Administrative relation | MOVE |
| regionId | Region/Geographic relation | MOVE |
| barrierOwnerRealmId | Barrier political ownership | SPLIT |
| SettlementControl fields | Settlement↔Settlement control relation | MOVE |

## Settlement / Anchor

| Current field | Target | Status |
|---|---|---|
| BurgData.tileIndex | Anchor spatial relation | MOVE |
| BurgData.x/y | Anchor/Map coordinate | MOVE |
| BurgData.population | Population/Settlement relation | SPLIT |
| BurgData.development | Settlement/Economy derived/state | SPLIT |
| BurgData.wealth | Economy | MOVE |
| BurgData.tradePower | Economy/Trade Query | MOVE |
| BurgData.settlementCategory | SettlementCategory | KEEP |
| BurgData.settlementType | SettlementDefinition/Typology | SPLIT |
| BurgData.settlementLevel | SettlementLevel | KEEP |
| BurgData.isCapital | Political Role | MOVE |
| BurgData.isPort | Settlement Role / Port relation | SPLIT |
| BurgData.fortification | Settlement Fortification | MOVE |
| BurgData.garrison* | Military/Garrison | MOVE |
| BurgData.wallLevel | Structure/Fortification | SPLIT |
| BurgData.controllerBurgId | SettlementControl relation | MOVE |
| BurgData.controlledBurgIds | SettlementControl relation | MOVE |
| BurgData.controlProgress | SettlementControl relation | MOVE |
| BurgData.influenceRadius | SettlementControl/Influence calculation | MOVE |
| CampData.tileIndex | Anchor | MOVE |
| CampData.ownerActorId | MapActor relation | MOVE |
| CampData.ownerRealmId | Political relation | MOVE |
| CampData.population | Population | MOVE |
| tile.campId | Spatial index/cache | COMPAT |
| settlement destruction fields | Settlement lifecycle + Population/MapActor effects | SPLIT |

## Building

| Current field | Target | Status |
|---|---|---|
| TileData.buildingLevels | Building/Settlement-local state | MOVE |
| BuildingSystem._tileBuildings | Settlement/Anchor association | MOVE |
| BuildingCategory.Road | Structure/Network | MOVE |
| ordinary Building instances | Settlement-local Building | KEEP/MOVE |

## Structure / Network

| Current field | Target | Status |
|---|---|---|
| TileData.roadLevel | RoadSegment/RoadNetwork | MOVE |
| TileData.hasBarrier | Barrier Structure | MOVE |
| TileData.isGate | Gate/Passage Structure | MOVE |
| TileData.barrierStrength | Barrier state | MOVE |
| TradeRoute.nodeTileIndices | Network path over MapCell/Anchor/links | SPLIT |
| TradeRoute.baseEfficiency | TradeRoute base state | KEEP |
| TradeRoute.currentEfficiency | TradeRoute Query/Derived | DERIVE |
| TradeRoute.isBlocked | Blockage Query | UNRESOLVED/DERIVE |
| nearbyFortId | Fort Influence Query cache | DERIVE |
| fortInfluenceLevel | Fort Influence Query result/cache | DERIVE |

## Economy

| Current field | Target | Status |
|---|---|---|
| TradeCenter.inventory | Economy | KEEP |
| TradeCenter.inventoryCapacity | Economy | KEEP |
| TradeCenter.localDemand | Economy | KEEP |
| TradeCenter.localSupply | Economy | KEEP |
| TradeCenter.centerTileIndex | Anchor/Settlement relation | MOVE |
| TradeCenter.tradeRoutes | Network relation | SPLIT |
| _resourceCumulativeOutput | Economy/Production evidence | MOVE |
| TileData.fertility | Geography/Economy input | SPLIT |
| TileData.development | Settlement/Economy state | SPLIT |

## Innovation

| Current field | Target | Status |
|---|---|---|
| InnovationDef | Innovation Definition Registry | KEEP |
| InnovationTree._realmInnovations | Social/Realm Innovation Knowledge | MOVE |
| InnovationTree._realmResearchPoints | Legacy research compatibility | COMPAT |
| InnovationTree._realmCurrentResearch | Legacy research selection compatibility | COMPAT |
| InnovationTree._innovationProgress | Innovation Practice Progress | UNRESOLVED |
| InnovationTree._resourceCumulativeOutput | Economy/Production evidence | MOVE |
| CharacterResearchData.currentResearchInnovationId | Character Research | KEEP |
| CharacterResearchData.specialty | Character Research | KEEP |
| CharacterResearchData.researchAbility | Character Research | KEEP |
| CharacterResearchData.inspiration* | Character Research | KEEP |
| CharacterResearchData.totalResearchContribution | Character Research | KEEP |
| CharacterResearchData.completedInnovations | Character Research history/state | KEEP |
| ResearchPlanData.* | Research Plan | KEEP |
| CultureData.innovationAffinities | Culture modifier input | KEEP |

## Save Schema

| Current persistence | Target | Status |
|---|---|---|
| TileSaveData.roadLevel | Road/Network DTO or migration field | COMPAT |
| MapSaveSystem roadLevel read/write | New Road Save Schema | MOVE |
| GameSaveSystem roadLevel read/write | New Road Save Schema | MOVE |
| Innovation dedicated DTO | Not currently evidenced | UNRESOLVED |
| CharacterResearch dedicated DTO | Not currently evidenced | UNRESOLVED |
| ResearchPlan dedicated DTO | Not currently evidenced | UNRESOLVED |
| Barrier dedicated DTO | Not currently evidenced | UNRESOLVED |
| Fort dedicated DTO | Not currently evidenced | UNRESOLVED |

## GreatProject

| Concept | Target | Status |
|---|---|---|
| ConstructionPlanSystem local building plan | Local Construction Plan | KEEP |
| PlanType.Engineering | Legacy/enum capability marker | COMPAT |
| GreatProject lifecycle | GreatProject domain | UNRESOLVED |
| GreatProject persistence | GreatProject Save Schema | UNRESOLVED |
| Great Wall result | FrontierDefenseNetwork | UNRESOLVED |

## Prohibited migrations

在没有进一步证据前，不得：
1. 直接把 TileData 重命名为 MapCell 后继续保留全部字段。
2. 把 InnovationTree 重命名为 InnovationKnowledgeSystem。
3. 把 GreatProject 做成 Plan 的子类。
4. 把 Fort Influence 写回 MapCell 作为新 authority。
5. 把 SettlementControl 简化为 MapCell.controlRealmId。
6. 把 Road 当作普通 Building 继续扩张。
7. 把 Save DTO 当作 Domain Entity。
8. 删除旧 Innovation research state，因为新 practice model 已存在并不等于旧 state 已无调用方。

## 下一阶段

矩阵建立后，下一步应逐项做 **Call Graph + Save/Load Path Audit**，优先闭合：
1. InnovationProgress scope。
2. CharacterResearch persistence。
3. ResearchPlan persistence。
4. Fort/Barrier persistence。
5. Road migration compatibility。
6. GreatProject 现有调用边界。

完成这些证据后，才能进入真正的 Field Migration Plan 与代码迁移。