# 审计 A：存档/读档完整性与静态注册表生命周期

- 仓库：`D:\Myriad-Creation-A-Phantasm-of-Passing-Aeons`，分支 `architecture-2-0-refactor`
- 审计对象：`MyriadCreation/Assets/Scripts/Infrastructure/Save/**`、`GameWorld` 存档入口/子系统重建、6 个静态注册表
- 方式：只读静态审计（未运行 Unity、未编译、未执行写操作类 git 命令）。所有结论给出 `文件:行`。
- 基准：`GameConstants.SaveVersion = 13`（`Core/MyriadCreation.Core.Constants/GameConstants.cs:13`）

---

## 1) 结论摘要（≤10 行）

1. **最严重的问题不在 v12/v13 新增的 DTO，而在"读档入口没有把世界初始化完整"**：`SaveSystem.LoadGame` 只 `AddComponent<GameWorld>()` + `ApplyToWorld`，**从不调用 `InitializeWorld()`**，而 `GameWorld.Awake` 是空函数。`unitDefs`、`_aiManager` 控制器、`InnovationTree`、`FaithSystem` 全部停留在"新建对象"状态。
2. 后果一：读档后 `unitDefs` 为空 → 军队人力/战力恒为 0，战斗结束判定 `0/0 = NaN` 使**战斗永不结束**（P0-1）。
3. 后果二：AI 控制器只由 `InitializeDefaultRealms` 创建（只在生成地形路径上），读档后**所有政权的 AI 完全停摆**（P0-2）。
4. 后果三：`InnovationTree` 全部为运行时字典、`SaveData` 无对应字段 → **所有政权科技/研究点/发现清零**（P0-3）。
5. `MapUnitRegistry.Clear()` 把 `_nextId` 重置为 1，而还原的地图单位都带显式 id；`ArmyPersistence.RegisterArmyViews` 之后从 1 开始分配 → **军队视图覆盖已还原的地图单位**（P0-4）。
6. 读档过程**非原子**：中途抛异常只 `return null`，但静态注册表已改写、新建的 GameWorld 既不销毁也不清理，旧世界的运行数据被新档污染，且泄漏的 GameWorld 仍每帧 `GameTick`（P0-5）。
7. v12/v13 的省份/聚落/营地/军队 DTO 本身覆盖度较好（市场库存、区划、城墙块、营地补给、军队 transport 货物/成员均已入档），问题集中在**核心世界状态与全局注册表**。
8. 另有 9 条 P1：玩家政权指针、地图环绕模式、信仰、角色特质/关系/技能/疾病/伤残、CharacterBond、DynamicGoods 跨局泄漏、旧档不可迁移、写盘非原子、关系持久化策略过窄。
9. 测试盲区：唯一全链路读档测试只断言**对象数量**（tiles/provinces/settlements/realms/characters/armies），不断言 unitDefs/AI/科技/mapUnits/population → 上述 P0 全部漏网。
10. 结论：**当前存档系统能"把数量读回来"，但不能"把世界读活"**；建议先修 P0-1/2/3（同一根因：LoadGame 缺一次完整的运行时初始化），再修 P0-4/5。

---

## 2) P0 清单

### P0-1 读档路径跳过 `InitializeWorld()`：`unitDefs` 为空，战斗/补给系统失效

- **现象**：`SaveSystem.LoadGame` 新建 GameWorld 后直接进入 `ApplyToWorld`，全流程没有任何 `InitializeWorld()` / `InitializeUnitDefs()` 调用；`GameWorld.Awake()` 是空函数。
- **证据**：
  - `Infrastructure/Save/SaveSystem.cs:89-91`（`new GameObject` → `AddComponent<GameWorld>` → `ApplyToWorld`）
  - `Simulation/MyriadCreation.Simulation.WorldState/GameWorld.cs:2002-2004`（`private void Awake() { }`）
  - `GameWorld.cs:92`（`public Dictionary<int, UnitDef> unitDefs = new ...`，初始为空）
  - `GameWorld.cs:555`（`InitializeUnitDefs()` 的唯一调用点在 `InitializeWorld()` 内）
  - `GameWorld.cs:504-562`（`InitializeWorld` 内依次 `InitializeSubsystems/InitializeGoodsDefs/InitializeUnitDefs/InitializeTradeCenters/InitializeDefaultRaces/InitializeDefaultCultures/ApplyContentOverrides/CultureLineage.Apply`）
  - `SaveSystem.cs:183` 注释声称"`AddComponent` 时 GameWorld.Awake 已先跑 InitializeWorld（含 unitDefs）"——与实际代码不符，是这处遗漏的成因
- **触发条件**：任何一次 `SaveSystem.LoadGame` 成功后进入游戏。
- **后果**（`unitDefs` 空 → 所有按 unitTypeId 查表的路径静默降级）：
  - `Simulation/MyriadCreation.Simulation.State/Army.cs:78-89` `GetTotalManpower` 返回 0；
  - `Army.cs:91-111` `CalculateCombatPower` 返回 0；
  - `Army.cs:113-123` `DailyTick` 的补给消耗为 0（军队永不缺粮）；
  - `Army.cs:232-238` `CheckBattleEnd` 计算 `0 / 0 = NaN`，`NaN <= 0.3f` 恒 false → **战斗永不结束**；
  - `Simulation/MyriadCreation.Simulation.Systems/CombatManager.cs:16,27,35-37` 全程持有这个空字典。
  - 附带：`InitializeGoodsDefs`（mod/默认商品属性）、`InitializeDefaultRaces/Cultures`、`ApplyContentOverrides`（`GameWorld.cs:955-982`）、`CultureLineage.Apply`（`GameWorld.cs:560`）也同样被跳过；v13 存档靠 `data.goodsDefs/races/cultures` 兜住，但**任何旧档或 mod 内容都会因此缺段**。
- **建议修法**：在 `ApplyToWorld` 中显式补齐"读档专用的初始化顺序"，例如新增 `GameWorld.InitializeRuntimeForLoad()`：清空/重建 -> `InitializeSubsystems()` 已有；在其后立即 `InitializeUnitDefs()`、`InitializeGoodsDefs()`（仅当 `data.goodsDefs == null`）、`InitializeDefaultRaces/Cultures`（仅当对应段为 null）、`ApplyContentOverrides()`、`InitializeFaithSystems()`，再做 `SettlementPersistence.Restore*`。或者把 `unitDefs` 也纳入存档（UnitDef 是运行时静态表，推荐前者）。

### P0-2 读档后 AI 控制器缺失：所有政权 AI 停摆

- **现象**：`AIManager` 是空对象，`AIController` 只由 `InitializeDefaultRealms()` 里的 `_aiManager.CreateController(j)` 创建，而 `InitializeDefaultRealms` 只在生成地形/初始政权路径上被调用。
- **证据**：
  - `Simulation/MyriadCreation.Simulation.AI/AIManager.cs:11-19`（`_controllers` 私有字典 + `CreateController`）
  - `GameWorld.cs:944`（`_aiManager.CreateController(j)`，位于 `InitializeDefaultRealms`，`GameWorld.cs:888`）
  - `GameWorld.cs:1601 / 1672 / 1953`（`InitializeDefaultRealms()` 的三个调用点，全部在 `GenerateTerrain/GenerateTerrainWithConfig/...` 路径）
  - `GameWorld.cs:594`（`_aiManager = new AIManager()`，读档时经 `ReinitializeSubsystems` → `InitializeSubsystems` 新建，但无人填充控制器）
  - `AIManager.cs:21-27`（`DailyTick` 遍历 `_controllers`）、`:41-48`（`GetController` 返回 null）、`:50-57`（`GetResearchRate` 恒 0）
  - `GameWorld.cs:2275-2276`（`AISchedule.Register(... _aiManager ...)`、`InnovationResearchSchedule.Register(... _aiManager ...)`）
- **触发条件**：`LoadGame` 后开始推进时间。
- **后果**：AI 不产生任何意图/研究/外交/宣战决策；`GetResearchRate` 恒 0，配合 P0-3 使研究永久冻死；AI 政权的行为退化为只有被动系统。
- **建议修法**：与 P0-1 同一次修复中，在实体恢复后为 `world.realms.Keys` 逐个调用 `CreateController`（保留 `SetupOptions` 判定的 AI 数量），并调用 `_aiManager.SyncRulers(characters)` 同步统治者性格；若需保留 AI 人格/状态，给 `AIController` 加存档段。

### P0-3 `InnovationTree` 完全未入档：所有政权科技/研究进度清零

- **现象**：`SaveData` 没有 innovations 段；`InnovationTree` 的六张表全是私有字典；`InitializeSubsystems` 每次 `new InnovationTree()`（从 ContentRegistry 重新装载定义），没有任何还原入口。
- **证据**：
  - `Simulation/MyriadCreation.Simulation.State/InnovationTree.cs:13-29`（`_innovations/_realmInnovations/_realmResearchPoints/_realmCurrentResearch/_realmDiscovered/_innovationProgress/_resourceCumulativeOutput`）
  - `GameWorld.cs:588`（`_innovationTree = new InnovationTree()`，读档必经）
  - `Infrastructure/Save/SaveData.cs:19-62`（完整字段表，无任何 innovation/tech 字段）
  - `GameWorld.cs:2276`（研究调度依赖 `_innovationTree` + `_aiManager`）
- **触发条件**：任何读档。
- **后果**：所有政权已完成的科技（解锁的法律、兵种、建筑、商品）、研究点、已发现项全部回退到初始；长局读档等于科技树重置，且与 P0-2 叠加后不再恢复。
- **建议修法**：新增 `InnovationTreeSaveData`（每政权：completedIds、discoveredIds、researchPoints、currentResearch、progress 字典拍平）并在 `ApplyToWorld` 末尾就地 `Restore`（注册表/树实例已由 `InitializeSubsystems` 捕获，必须就地填充），`SaveVersion` 升 14。

### P0-4 `MapUnitRegistry._nextId` 未续接：军队视图注册时覆盖已还原的地图单位

- **现象**：`Clear()` 把 `_nextId` 重置为 1；还原的 `MapUnit` 都带显式 `unitId`（`MapUnitSaveData.ToUnit()`），`Add()` 只在 `unitId <= 0` 时才分配新 id 并推进游标；随后 `ArmyPersistence.RegisterArmyViews` 用 `MapUnit.FromArmy` 生成的单位 `unitId = 0`，于是从 1 开始分配并与已还原条目撞 id。
- **证据**：
  - `Simulation/MyriadCreation.Simulation.Units/MapUnitRegistry.cs:13`（`_nextId = 1`）、`:19-23`（`Clear()` 重置 `_nextId = 1`）、`:25-33`（`Add` 仅在 id<=0 时 `_nextId++`）
  - `MapUnit.cs:81-105`（`FromArmy` 不设置 `unitId`）、`:55-79`（`FromMapActor` 同样不设置）
  - `SaveSystem.cs:341-350`（`MapUnitRegistry.Clear()` 后 `Add(dto.ToUnit())`，还原条目保留存档 id）
  - `SaveSystem.cs:243`（`ArmyPersistence.RegisterArmyViews(world)`）、`ArmyPersistence.cs:62-67`
  - 同一问题也影响 `GameWorld.cs:2738`（新建军队注册视图）与 `Simulation/MyriadCreation.Simulation.Actors/MapActorManager.cs:275`（新刷地图单位注册视图）
- **触发条件**：存档中同时存在非军队地图单位（商队/难民/游牧/野兽等）与军队，读档一次即发生。
- **后果**：`_units[id]` 被军队视图覆盖 → 已还原的商队/难民视图从注册表消失；按 unitId 选中的实体指向错误对象；运输队货物/成员视图错位。属于**读档即发生的静默数据损坏**，且不会自愈（只有再存再读或 `SyncFromWorld` 才重整）。
- **建议修法**：新增 `MapUnitRegistry.SetNextId(int)` / `AdoptNextId`，在还原循环后把 `_nextId` 推进到 `max(unitId)+1`；或让 `SyncFromWorld` 成为读档唯一入口（彻底不持久化视图），并在 `Add` 时对已存在 id 做 `Debug.LogError` 保护。

### P0-5 读档失败非原子：静态注册表被新档改写 + GameWorld 泄漏并继续 tick

- **现象**：`ApplyToWorld` 在恢复静态注册表（第 200-205 行）之后还有多个会抛异常的步骤；异常被 `LoadGame` 的 catch 吞掉，只返回 null，既不清空/回滚静态表，也不销毁已创建的 GameObject。
- **证据**：
  - `SaveSystem.cs:89-100`（`new GameObject` + `ApplyToWorld` 在 try 内，catch 只 `Debug.LogError` + `return null`，无 `Object.Destroy(go)`、无回滚）
  - `SaveSystem.cs:200-205`（`DynamicGoodsRegistry.Restore` / `RelationRegistry.Restore` / `ContractRegistry.Restore` 已改写全局静态表）
  - 抛异常点：`SaveSystem.cs:226-231` → `SettlementPersistence.cs:188`（重复 site UUID）、`:190`（非法 site_mode）、`:195`（camp manager 为空）、`:197`（非法 camp_type）、`:239`（营地属性数组不一致）；`ArmyPersistence.cs:50`（非法 armyId）、`:52`（重复 armyId）
  - `GameWorld.cs:2006-2014`（`Update` 每帧 `GameTick`）——泄漏的 GameWorld 仍会跑模拟
- **触发条件**：存档损坏/跨版本 site_mode 键变化/重复 id（例如手工改档、外部工具合并存档），或未来版本 schema 漂移。
- **后果**：`GameManager.LoadGame`（`Legacy/Core/GameManager.cs:128-146`，其中 `:131-132` 判空、`:134` 才销毁旧世界）拿到 null 后保留旧世界，但旧世界的 RelationRegistry/ContractRegistry/DynamicGoodsRegistry 已被"读了一半"的新档覆盖 → 旧世界关系/契约/动态商品瞬间错乱；泄漏的 GameWorld 继续 `Update → GameTick`，持续写 `MapUnitRegistry` 等静态表，造成二次污染与 CPU 空转。
- **建议修法**：`ApplyToWorld` 前置全部可抛校验（反序列化后先 `ValidateSaveData`）；重建包在 try 内，catch 中 `Object.Destroy(go)` + 显式恢复/清空静态表（或在局部先构建再一次性提交）；`LoadGame` 失败时提供 `LoadResult` 而非 null。

---

## 3) P1 清单

### P1-1 `PlayerRealmId` 未入档：读档后玩家失去自己的政权

- **证据**：`SaveData.cs:19-62`（无 playerRealmId）；`GameWorld.cs:89`（`public int PlayerRealmId = -1`）；`GameSaveData.cs:35`（旧系统有该字段）；消费方 `Rendering/MapRenderer.Colors.cs:199,220,304`、`Render/MapRenderer.cs:470,490`。
- **后果**：读档后所有"玩家视角"渲染/交互退化为"无玩家政权"（`-1`），旧存档系统保存的该值丢失。
- **修法**：`SaveData` 加 `playerRealmId`，`ToSaveData/ApplyToWorld` 双向映射；旧档缺失时按 `-1` 兜底并在 UI 提示重选。

### P1-2 世界级配置未入档：`wrapMode` 等导致读档后地图拓扑变化

- **证据**：`GameWorld.cs:65`（`wrapMode`）、`:62`（`randomSeed`）、`:68`（`tickInterval`）、`:70`（`daysPerTick`）、`:112/:116`（`startYear/startDay`）、`:142`（`GenConfig`），均不在 `SaveData.cs` 字段表中。
- **后果**：以 Flat/Toroidal 生成的地图读档后回落为默认 `Cylindrical`（`GameWorld.cs:65`）：`Rendering/MapRenderer.cs:153,273` 的贴图 wrap、`Render/MapEditor.cs:150-156,518-524` 的邻接计算与存档前不一致 → 跨边界邻接/寻路/渲染重复错误；`daysPerTick` 影响所有 tick 内乘法，读档后节奏与存档时不同。
- **修法**：把 mapWidth/mapHeight 之外的世界配置一并入档（独立 `WorldConfigSaveData` 段），`ApplyToWorld` 中先还原配置再 `ReinitializeSubsystems`。

### P1-3 信仰运行态未入档且读档不重建 `FaithSystem`

- **证据**：`GameWorld.cs:108`（`_faithSystems`）、`:982`（`InitializeFaithSystems()` 仅在 `ApplyContentOverrides` 末尾调用）、`:985-1008`（按 `ReligionCatalog` 重建 fervor=50 的教统并订阅 `_religionSimulationEvents`）；`SaveData.cs` 无信仰段；`GameWorld.cs:1327`（每 tick 遍历 `_faithSystems`）、`:2458-2461`（`GetFaithSystem` 返回 null）、`:2479-2483`/`:2690-2694`（null 时静默 `return false`）。
- **后果**：读档后教统列表为空 → 宗教 tick 空转、`ReligionSimulationEventHandler` 未订阅（宗教事件丢失）、圣地创建/大圣战/热情值全部失效，且不会报错（静默 false）。
- **修法**：`ApplyToWorld` 调用 `InitializeFaithSystems()`（与 P0-1 一并做），并把 `fervor / holySiteTileIndices / highPriestCharacterId` 等运行态入档。

### P1-4 角色运行态字段未入档：特质/关系/技能/疾病/伤残/身体部位

- **证据**（`CharacterSaveData.cs:24-100` 已覆盖的字段之外，`CharacterData.cs` 仍有）：
  - `CharacterData.cs:188` `traits`（List<PersonalityTrait>）
  - `CharacterData.cs:191` `relations`（Dictionary<int, CharacterRelation>）
  - `CharacterData.cs:194` `skills`（Dictionary<string,float>）
  - `CharacterData.cs:184` `activeDiseases`、`:186` `impairments`
  - `CharacterData.cs:59` `bodyParts`（由 `CharacterSaveData.cs:245-246` 在读档时**重新初始化**，等于每次读档伤病全愈）
  - `CharacterSaveData.cs:173-248`（`ToCharacter` 未含上述字段）
- **后果**：读档后角色性格特质、人物间好感/仇恨、技能等级、进行中的疾病与伤残全部丢失，人物行为/事件判定漂移；`traits` 丢失对婚姻、阴谋、继承等系统是可见退化。
- **修法**：`CharacterSaveData` 增加 traits/relations/skills/diseases/impairments 的拍平数组段（JsonUtility 不支持 Dictionary → List 包装，同 `RealmOfficeSaveData` 的做法），`ToCharacter` 中还原；`bodyParts` 若由 DNA 派生则改为派生而非重初始化。

### P1-5 `CharacterBond`（角色纽带）完全未入档

- **证据**：`Simulation/MyriadCreation.Simulation.Systems/CharacterManager.cs:18`（`private readonly List<CharacterBond> _bonds`）、`:24`（`_nextBondId`）；`Simulation/MyriadCreation.Simulation.State/CharacterBond.cs:6-27`（含 combatBonusWhenTogether/diplomacyBonus/stressReduction 等数值）；`Infrastructure/Save/SaveEntityBridge.cs:25-42`（反射字段清单里没有 `_bonds`）。
- **后果**：读档后所有角色纽带（含战斗/外交/减压加成）消失，之前建立的关系网蒸发；`_nextBondId` 从 1 重来。
- **修法**：`CharacterSaveData` 之外增加 bonds 段或在 `SaveEntityBridge` 增加 `InstallBonds`，并推进 `_nextBondId`（同 `_nextCharacterId` 的模式）。

### P1-6 `DynamicGoodsRegistry` 是全局静态表：新局不清空、存盘直接捕获全局表 → 跨世界泄漏

- **证据**：`Simulation/MyriadCreation.Simulation.Economy/DynamicGoodsRegistry.cs:22-25`（`Clear` 同时清 `_goods/_recipes`）、`:145-165`（`Capture` 全表）、`:166+`（`Restore` 先 `Clear`）；`GameWorld.cs:504-509`（`InitializeWorld` 只清 Relation/Contract/MapUnit/CharacterKnowledge **未清 DynamicGoodsRegistry**）；`SaveSystem.cs:147`（`dynamicGoods = DynamicGoodsRegistry.Capture()`）。
- **后果**：A 局生成的合金/合成商品会留在静态表里：开新局后仍存在（世界 A 的商品出现在世界 B），且下一次存盘会把它们写进 B 的存档；`Restore` 只接受 `goodsId >= 10000` 的条目，任何 <10000 的残留会静默消失。
- **修法**：`InitializeWorld` 增加 `DynamicGoodsRegistry.Clear()`；`Capture` 改为按世界(id 集合)过滤，或把动态商品改为 `GameWorld` 实例字段。

### P1-7 旧档（v10/旧格式）不可迁移：`SaveJsonMigration` 未接入主读档路径

- **证据**：`SaveSystem.cs:71`（`JsonUtility.FromJson<SaveData>(json)`，未调用任何迁移）；`Infrastructure/Save/SaveJsonMigration.cs:45-57,60-72,74-100`（旧键 `burgs/settlements → habitation_sites` 迁移实现）；其调用点只有 `Infrastructure/Save/GameSaveSystem.cs:335` 与 `Infrastructure/Save/MapSaveSystem.cs:244`，而这两个类是 `[Obsolete]`/未接入（`GameSaveSystem.cs:42`）；且它们目录/扩展名不同：`GameSaveSystem.cs:48,100,323-325`（`GameSaves/*.json`）vs `SaveSystem.cs:33,43,63`（`Saves/*.sav`）。
- **后果**：旧格式存档既不在 `ListSaves()` 中，也无法被 `LoadGame` 读取；若手工改成 `.sav`，`data.provinces/habitationSites` 为 null，`RestoreSites` 直接清空，聚落/省份/城墙/市场全部消失，仅剩地块 —— 静默降级且无提示。
- **修法**：把 `SaveJsonMigration` 接入 `SaveSystem.LoadGame`（识别旧键/旧版本后迁移），或提供一次性导入器并在 UI 明示"旧档不可用"。

### P1-8 写盘非原子 + 异常信息被吞：半截文件即毁档

- **证据**：`SaveSystem.cs:36-56`（`Directory.CreateDirectory` → 直接 `File.WriteAllText(filePath, ...)` 覆盖同名文件；catch 只记录 `e.Message`，丢栈与异常类型）；`:104-113`（`DeleteSave` 同样直接删）。
- **后果**：写盘过程中崩溃/断电/磁盘满 → 原档被半截 JSON 覆盖（JsonUtility 读回 null 或半空对象），无 `.bak`、无临时文件 rename、无校验和；`e.Message` 丢失堆栈使线上排障困难。
- **修法**：写临时文件（`{name}.sav.tmp`）→ `File.Replace`/`Move`；保留上一份 `.bak`；catch 中 `Debug.LogException(e)`；对 `ToSaveData` 结果做一次自校验（如 tiles.Length 一致性）。

### P1-9 关系持久化策略过窄：已入档角色/锚点的关系被静默丢弃

- **证据**：`Infrastructure/Save/RelationPersistencePolicy.cs:12-15`（`IsWorldSaved` 只允许 `EntityKind.Realm || EntityKind.Province`）、`:17-28`（`ForWorldSave` 过滤）；`SaveSystem.cs:148`（`RelationEndpointValidator.Filter(..., out _)` 丢弃计数被忽略）；`Simulation/MyriadCreation.Simulation.Systems/RelationEndpointValidator.cs:9-34`（`Exists` 已支持 Anchor/Character，说明端点判定本身不是障碍）。
- **后果**：v11（角色）、v12（锚点）之后这些实体都已经在同一份存档里，但角色/锚点之间的好感、效忠、仇恨、附庸等关系在存盘时被丢掉；读档后关系网断裂，且日志无任何丢弃记录。
- **修法**：v14 允许 `Character/Anchor` 端点的关系（端点已持久化，`Exists` 可验证），并按 kind 分别统计 dropped 数写 warning。

---

## 4) P2 清单（简写）

| # | 项 | 证据 | 后果 / 建议 |
|---|---|---|---|
| P2-1 | 编年史未入档 | `Simulation/MyriadCreation.Simulation.State/Chronicle.cs:9-11`（`_entries/_nextEntryId`）；`GameWorld.cs:592`（`new Chronicle()`）；旧系统有 `GameSaveData.cs:56` `chronicleEntries` | 读档后历史页/编年史清空；建议加 `ChronicleEntrySaveData` 段 |
| P2-2 | 计划系统运行态未入档 | `Simulation/MyriadCreation.Simulation.Systems/PlanSystem.cs:10-18`（`_plans/_activePlanIds/_nextPlanId`）；Save 目录无 plan 段 | 读档后研究/建造/外交计划全部丢失，相关工程停摆到 AI 重新规划 |
| P2-3 | `saveName` 未消毒 | `SaveSystem.cs:43,63,106`（`Path.Combine(SaveDirectory, $"{saveName}.sav")`） | 名字含 `../` 可越出存档目录（写/删任意 `.sav`）；建议白名单字符 + `Path.GetFileName` |
| P2-4 | `Pipeline` 读档后为 null | `GameWorld.cs:253,510-513,2330-2336` | 读档后任何 `RegenerateFromPipeline` 会临时新建 pipeline（可接受，但语义与生成局不同） |
| P2-5 | `CultureLineage.Apply` 读档未调用 | `GameWorld.cs:560`（仅 InitializeWorld 内调用）；`Simulation/MyriadCreation.Simulation.Systems/CultureLineage.cs:10-53` | 若存档里 CultureData 已含 `languageFamilyId/parentCultureId` 则无影响（待验证：新增文化/语言的派生字段是否只在 Apply 时写入） |
| P2-6 | `TileData` 内部 `List<struct>` 的 JsonUtility 往返无断言 | `Core/MyriadCreation.Core.Data/TileData.cs:98-102`（`buildingLevels[] / List<PopulationBlock> / List<TileResource>`）；`Core/MyriadCreation.Core.Data/PopulationBlock.cs:7`、`TileResource.cs:6` 都是 struct；团队自己在 `Infrastructure/Save/WorldEntitySaveData.cs:38-40` 注释"JsonUtility 对结构体列表支持不稳"并改用拍平数组 | 若 struct 列表往返失败，`populationBlocks/resources` 会静默变空（人口清零）。**待验证**：建议加一条 SaveGame→LoadGame 后断言地块人口/资源的测试 |
| P2-7 | 双存档系统并存 | `Infrastructure/Save/MapSaveSystem.cs`（`607` 行，未标 `[Obsolete]`）vs `SaveSystem.cs`；`GameSaveSystem.cs:42` 已标 Obsolete | 容易被误用/误改；建议给 MapSaveSystem 同样打 Obsolete 或删除 |
| P2-8 | 读档生成的 GameWorld 无 `DontDestroyOnLoad` | `SaveSystem.cs:89`；`GameManager.LoadGame`（`Legacy/Core/GameManager.cs:128-146`）持有引用 | 切场景后世界对象被销毁而引用仍在（待验证具体场景切换路径是否有保护） |
| P2-9 | `data.mapUnits` 捕获包含军队视图但读档跳过 | `SaveSystem.cs:274-275`（全量捕获）vs `:348`（跳过 `UnitSourceKind.Army`） | 存档体积浪费 + 语义分裂；建议捕获时就排除军队视图 |

---

## 5) 仍未入档清单（字段 → 后果）

| 未入档项 | 定义位置 | 读档后果 |
|---|---|---|
| `GameWorld.unitDefs`（兵种定义表，运行时为空） | `GameWorld.cs:92`，填充仅 `:555` | 军队人力/战力 0、战斗 NaN 不结束（P0-1） |
| `AIManager._controllers`（每政权 AI 控制器/人格） | `AIManager.cs:11-19`，创建仅 `GameWorld.cs:944` | AI 完全停摆（P0-2） |
| `InnovationTree` 六张表 | `InnovationTree.cs:13-29` | 科技/研究点/发现全清零（P0-3） |
| `GameWorld.PlayerRealmId` | `GameWorld.cs:89` | 玩家失去自己的政权（P1-1） |
| `wrapMode / randomSeed / tickInterval / daysPerTick / startYear / startDay / GenConfig` | `GameWorld.cs:62,65,68,70,112,116,142` | 地图拓扑模式与节奏回退（P1-2） |
| `_faithSystems`（fervor/圣地/教统状态）+ `_religionSimulationEvents` | `GameWorld.cs:108,982-1008` | 宗教系统静默失效（P1-3） |
| `CharacterData.traits / relations / skills / activeDiseases / impairments / bodyParts` | `CharacterData.cs:59,184,186,188,191,194` | 人物特质、人际、技能、疾病、伤残丢失（P1-4） |
| `CharacterManager._bonds` / `_nextBondId` | `CharacterManager.cs:18,24` | 纽带及加成丢失（P1-5） |
| `_chronicle`（ChronicleEntry 列表、`_nextEntryId`） | `Chronicle.cs:9-11` | 历史/编年史清空（P2-1） |
| `PlanSystem / ResearchPlanSystem / ConstructionPlanSystem` 运行态 | `PlanSystem.cs:10-18` | 在读计划全部丢失（P2-2） |
| `_judicialSystems`（司法系统缓存） | `GameWorld.cs:188` | 懒加载可从 RealmData.laws 重建（可接受，记录在案） |
| `CharacterKnowledgeRegistry` | 已由 `SaveEntityBridge.cs:127-161` 处理 | ✅ 已覆盖 |
| 省份/聚落/营地/军队/官职/行政区划/市场库存/城墙块/城市区划/地图单位视图 | `SettlementPersistence.cs`、`ArmyPersistence.cs`、`RealmDTO.cs`、`AdminDivisionSaveData.cs` | ✅ 已覆盖（DTO 覆盖度良好，问题只在"运行时初始化缺失"） |

---

## 6) 最小复现思路（P0/P1 各一句）

- **P0-1**：`WorldGenerationSmokeTests` 同款流程：`NewWorld(48,24)` + `GenerateTerrain(11)` → `SaveSystem.SaveGame` → `LoadGame` → 断言 `loaded.unitDefs.Count > 0`（必失败），或进入游戏打一场仗观察永不结束。
- **P0-2**：同上流程 → 断言 `loaded.GetAIManager().GetAllControllers().Count == loaded.realms.Count`（必失败）；或读档后推进 30 天，观察各政权 AI 计划/外交无变化。
- **P0-3**：生成局研究完成 1 项科技（`world.Innovations.HasCapabilityValues`）→ 存读 → 断言该政权 `Innovations.GetRealmInnovations` 仍包含该项（必失败）。
- **P0-4**：造一个含商队（`MapActorType.Caravan`，注册为 `MapUnit`）+ 1 支军队的世界 → 存读 → 比较读档前后 `MapUnitRegistry.All` 的 `(unitId,source,sourceId)` 集合（必出现军队视图 id 覆盖商队条目）。
- **P0-5**：手工把存档 JSON 里某个 `habitationSites[i].site_mode` 改成非法值 → `LoadGame` 返回 null，但随后检查 `RelationRegistry.Capture()/ContractRegistry.Count` 已被该档覆盖、场景中多出一个仍在 tick 的 `GameWorld_xxx` 对象。
- **P1-1**：设置 `world.PlayerRealmId = 0` → 存读 → 断言 `loaded.PlayerRealmId == 0`（必失败）。
- **P1-2**：`world.wrapMode = MapWrapMode.Flat` → 存读 → 断言 `loaded.wrapMode == Flat`（必失败，回到 Cylindrical）。
- **P1-3**：存读后断言 `loaded.GetFaithSystem(0) != null`（必失败）或 `CreateHolySite(faithId, tile)` 返回 true（必失败）。
- **P1-4**：给角色手动加 `traits`/疾病 → 存读 → 断言 `character.traits.Count` 与 `activeDiseases.Count` 保持（必失败）。
- **P1-5**：`CharacterManager.CreateBond(a,b,BondType.…)` → 存读 → 断言 `GetAllBonds().Count` 保持（必失败）。
- **P1-6**：A 局 `DynamicGoodsRegistry` 生成一个合金 → 直接 `GameWorld.InitializeWorld()` 开新局 → 断言新局 `DynamicGoodsRegistry.Count == 0`（必失败）。
- **P1-7**：把旧 `GameSaves/*.json` 拷成 `Saves/x.sav` → `LoadGame("x")` → 观察 `habitationSites` 丢失且无迁移日志（`SaveJsonMigration` 从未被调用）。
- **P1-8**：对同名存档 `SaveGame` 期间强制结束进程（或把目标文件设为只读后观察是否留下半截文件），检查是否存在 `.sav` 内容截断且无 `.bak`。
- **P1-9**：对同一政权的两座聚落锚点建立 `EntityKind.Anchor` 关系 → 存盘后解析 JSON，断言 `relations` 中不含该关系（必失败），且日志无 dropped 计数。

---

## 附：本次审计的测试覆盖观察

- 唯一的全链路读档测试：`Assets/Tests/EditMode/WorldGenerationSmokeTests.cs:71-99`，先 `NewWorld()`（内部调用 `InitializeWorld()`，`:48`）再 `GenerateTerrain`，读档后只比较 `Snapshot`（`:101-119`）里的 7 个**数量**，不触碰 `unitDefs` / AI / 科技 / `MapUnitRegistry` / 人口明细。
- 分模块测试（`ArmyPersistenceTests.cs:88-96`、`SettlementPersistenceTests.cs:141-151`）只做 `JsonUtility.ToJson(SaveData) → FromJson → Restore*` 的 DTO 级往返，绕过了 `SaveSystem.LoadGame` 的完整流程，因此系统性缺失（P0-1/2/3）不可能被测出。
- `SettlementPersistenceTests.cs:249` 的注释"JsonUtility 对缺失的 List 字段给空列表（不是 null）"是重要事实：`SaveData` 根类的 List 字段**没有字段初始化器**（`SaveData.cs:34-61`），所以旧档检测用的 `data.characters == null` 这类判断成立；而嵌套 DTO 的 List 有初始化器，缺键时为空列表 —— 两套 null 语义并存，改字段时要格外小心。
