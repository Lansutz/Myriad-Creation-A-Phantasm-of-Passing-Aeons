# 文件架构：哪些没做完

> 2026-11-10 03:20。证据：15 个 asmdef 的实际引用集（本机读出，非推测）。目的：把"架构半成品"列成可施工项，接在地基清单 F1–F8 之后。

---

## 1. 实际依赖图（从 asmdef 读出）

```
Core            → （无引用）
Content         → Core
World           → Core
Laws            → Core, Content
Faith           → Core, Content
Culture         → Core, Content
Simulation      → Core, Content, World, Laws, Faith, Culture
Infrastructure  → Core, World, Simulation, Content, Faith, Culture      ← ⚠ 反向
Rendering       → Core, Content                                        ← ✅ 干净
App.Contracts   → Core
App             → Core, App.Contracts, Infrastructure, World, Simulation, Rendering
UI              → Core, Rendering, Infrastructure, World, Content, App, App.Contracts  ← ⚠ 反向 + 越层
Editor          → 全部（可接受）
Tests           → 全部（可接受）
```

**已经做对的两处**：`Rendering → Core/Content`（读模型，不碰模拟），`Laws/Faith/Culture → Core/Content`（叶子程序集已拆出来）。
**做反的两处**：`Infrastructure` 与 `UI`。

---

## 2. 六项未完成（按影响排序）

### A. 存档层方向反了（Infrastructure → Simulation）

- 现状：`MyriadCreation.Infrastructure` 引用 `Simulation/World/Content/Faith/Culture`。
- 后果：存档层知道模拟层的全部类型 ⇒ DTO 只能长在 Infrastructure 里、`SaveSystem` 直接读写 `GameWorld`；这也是"存档静默丢 20+ 字段"的结构性原因——每加一个字段，要改的地方分散在三个程序集。
- 目标形状：**Core.Contracts 定义 `ISaveStore`/`SaveCapture`/`SaveRestore`；Simulation 实现捕获与恢复；Infrastructure 只做"DTO ↔ 文件"**。
- 完成判据：`infrastructure.asmdef` 的 references 只剩 `Core`（+ 一个纯 DTO 契约程序集）。

### B. UI 依赖反向 + 越层

- 现状：`UI → App`、`UI → Infrastructure`、`UI → World`。
- 后果：UI 能直接调存档、直接读世界对象；而 `App.Contracts`（`ISettingsService/IAppFlow/IUiFactory`）**在 UI 侧零消费**（K 域审计已证）。这就是"契约写了但没人用"。
- 目标形状：`UI → App.Contracts + Core + Rendering`；**App → UI**（App 装配 UI，不是反过来）。
- 完成判据：`ui.asmdef` 去掉 `App`/`Infrastructure`/`World`；`App` 增 `UI` 引用。

### C. `GameWorld` 单程序集单类（13 个 partial）

- 现状：WorldState/Systems/Hosts/Generation/Save/Succession 全是 `GameWorld` 的 partial，全在 `sim.asmdef` 里。
- 后果：任何系统都能读写任何字段（`treasury` 14 处跨 8 程序集直写、`RealmData` 37 个 public 字段无守卫，都是它的症状）。
- 目标形状：`GameWorld` 只做**装配与 tick 编排**；状态类（`RealmData/TileData/...`）不再被 systems 直接改，走命令/查询契约。
- 完成判据：出现第一组"状态只能经契约改"的字段，并有测试证明外部写入不再编译通过。

### D. 存档双轨（`SaveData` vs `GameSaveData`，`SaveSystem` vs `[Obsolete] GameSaveSystem`）

- 现状：头衔实例、编年史只存在于 **obsolete 路径**（D 域审计）。
- 后果：活存档读回来丢头衔/编年史；且两套 DTO 让人不知道改哪边。
- 目标形状：删掉 `GameSaveSystem` 与 `GameSaveData`，字段全部并进 `SaveData`。
- 完成判据：`grep -r "GameSaveSystem"` 只剩历史注释；头衔/编年史有往返测试。

### E. 遗传双轨（`DnaData` legacy vs `GenomeInstance`）

- 现状：`LegacyDnaAdapter` 在两者之间来回转换（`CharacterData.cs:216-221`、`DnaData.cs:42`）。
- 后果：两个真源；表型只算不存；读档漂移。
- 目标形状：`GenomeInstance` 为唯一真源，`DnaData` 降为只读视图或删除。
- 完成判据：`LegacyDnaAdapter` 只被读档迁移用一次，运行时零调用。

### F. `*Host` 适配器还在单体里

- 现状：`LawHost/FaithHost/CultureHost/ContentHost` 四个目录在 `sim.asmdef` 内，而 `Laws/Faith/Culture` 已是独立程序集。
- 判读：这是**已开始但没做完**的抽取——叶子拆了，适配层没拆。
- 目标形状：Host 与叶子同层（或独立 `*.Host` 程序集），Simulation 只依赖 Host 的契约。
- 完成判据：Host 目录移出 `sim.asmdef` 后仍能编译。

---

## 3. 与地基清单的关系

| 地基项 | 依赖哪条架构修复 |
|---|---|
| F2 存档闭合 | **A 必须先做**，否则新字段继续散在三层 |
| F3 双源合一 | D（存档双轨）+ E（遗传双轨）+ C（状态直写） |
| F4 内容外提 | 无（内容已在 Content 程序集） |
| F5 Mods 通路 | 无 |
| F7 国教 / F8 faithId | C（状态写入需契约） |

⇒ 建议顺序：**A → D → E → B → C → F**。A 是"存档闭合"的地基，不能倒过来做。

---

## 5. A 路线施工计划（存档层反向）

### A0（已完成）建立契约程序集，先搬一个纯 DTO

- 新建 `Assets/Scripts/Save/` + `Save.Contracts.asmdef`（name `MyriadCreation.Save.Contracts`，**references 只有 Core**）。
- `TileSaveData`（纯字段 struct）从 `Infrastructure.Save` 迁入，命名空间改 `MyriadCreation.Save.Contracts`；`Infrastructure.asmdef` 增加对该程序集的引用；`MapSaveData` / `GameSaveData` / `GameSaveSystem` 补 using；原文件删除。

### 关键发现：现有"DTO"其实混着映射器

`SettlementBlockSaveData`、`WallSegmentSaveData`、`SettlementMarketSaveData`、`SettlementPersistence`、`ArmyPersistence`、`FaithPersistence` 这些文件里，**类里带 `From/Capture/Restore/RestoreAll` 方法**，直接读写 `SettlementBlock`/`WallSegment`/`SettlementMarket`/`Army` 等模拟对象。

这才是 `Infrastructure → Simulation/World` 反向依赖的**真正来源**——不是"DTo 放错了层"，而是**数据与映射器没分开**。

### A1 拆分表（每个文件都要一分为二）

| 文件 | 纯字段部分（→ Save.Contracts） | 映射器（→ 状态所有者侧） |
|---|---|---|
| `TileSaveData` | ✅ 已迁 | 无（纯） |
| `MapSaveData` | 字段 + `EntityRelation[]` | 无（纯，可迁） |
| `ProvinceSaveData` | 待核 | 待核 |
| `SettlementBlockSaveData` | 字段 | `From` / `CaptureAll` / `RestoreAll` → **World.Settlement** |
| `WallSegmentSaveData` | 字段 | `From` / `CaptureAll` / `RestoreAll` → **World.Settlement** |
| `SettlementMarketSaveData` | 字段 | `Capture` → **World.Settlement** |
| `HabitationSiteSaveData` | 字段 | 待核 |
| `CharacterSaveData` / `ArmySaveData` / `NavySaveData` / `WorldEntitySaveData` | 字段 | 带 Simulation 枚举与状态类型，需逐个替换成 Core 原语 |
| `SaveData` / `GameSaveData` | 容器 | 引用了 Simulation.Economy/Warfare/Politics/State/Systems/Faith/Culture —— **最硬的一块** |

### A2 契约与实现反转

- `Core.Contracts` 定义 `ISaveSource { int SaveVersion { get; } TSnapshot Capture<TSnapshot>(); void Restore<TSnapshot>(TSnapshot, int fromVersion); }`（或按需拆成多个 typed source）。
- `Simulation` 侧新增 `WorldSaveCapture : ISaveSource`，把现在 `SaveSystem` 里的捕获/恢复逻辑搬过去。
- `Infrastructure` 只留：文件读写、JSON、版本迁移、原子写、备份。

### A3 收口判据

- `infrastructure.asmdef` 的 references = `Core` + `Save.Contracts`（**不再有 Simulation/World/Faith/Culture/Content**）。
- 删掉 `Infrastructure` 后 `Simulation` 仍能编译（存档能力由自身实现）。
- 头衔/编年史/外交四表在 `WorldSaveCapture` 里有往返测试。

---

## 7. A1 剩余项的锁：枚举与常量还压在 Simulation 里

把实体类 DTO 搬进 `Save.Contracts`（只引用 Core）时撞到的真实依赖链：

```
DTO（TransportUnitSaveData / MapActorSaveData / MapUnitSaveData / ArmySaveData / NavySaveData / CharacterSaveData）
  └─ 字段类型 = Simulation 的枚举
       UnitSubcategory / UnitCategory / TransportUnitType / UnitRole / MapActorType
       CharacterAICategory / UnitSourceKind / PackAnimalKind / CartKind / UnitFundingSource
         └─ 还有一个常量：UnitProvisioningCost.InvalidGoodsId（属于 Simulation 的类）
```

⇒ 只要枚举与常量在 `Simulation.*`，这些 DTO 就搬不动；而 `Save.Contracts` 引用 Simulation 就等于把反向依赖原样搬了个家——**等于没做**。

### 两条路

| 方案 | 做法 | 代价 | 收益 |
|---|---|---|---|
| **A. 枚举归位** | 把 9 个枚举 + `InvalidGoodsId` 常量迁到 `MyriadCreation.Core.Enums`，全仓补 using | 9 个文件移动 + 预计 30–60 处 using；一次性 | 长期最干净；之后所有 DTO 都能进 Contracts |
| **B. DTO 内整数编码** | DTO 里的枚举字段改 `int`，映射器里强转（`TransportUnitSaveData` 已有 `int[] memberRoles` 的先例） | 约 15 个字段 + 对应映射器；局部 | **JSON 格式兼容**——JsonUtility 本来就把 enum 写成整数，换 `int` 不改变存档字节 |

**建议顺序：先 B 后 A。**
- B 能让 `ArmySaveData` / `NavySaveData` / `TransportUnitSaveData` 立刻进 Contracts，A1 收尾；
- A 是长期债务（任何人新增 DTO 都会再撞一次），等 A2 做完（`ISaveSource` + `WorldSaveCapture`）再一次性清。

### B 的校验点（做完必须验）

1. 老存档仍能读：`JsonUtility` 对 enum 与 int 的 JSON 表示一致；
2. 映射器里强转的边界：把 `int` 转回 enum 时必须 `Enum.IsDefined` 或范围检查，避免脏档静默变 `(Enum)999`；
3. 往返测试：`ArmySaveData`/`NavySaveData` 各一条 `ToJson → FromJson → 断言字段相等`。

---

## 7.5 方向校正：我们的存档该学 CK3，而不是"给运行时对象照镜子"

> 用户口径："我们的架构是学的 CK3"。这一句否掉了 §7 里 B 方案的前提（把枚举编码成 int），因为 **CK3 的存档根本不用运行时类型的镜像**。

CK3（Jomini）的存档模型，四条与我们不同：

| CK3 做法 | 我们的现状 | 后果差异 |
|---|---|---|
| **定义（definition）与实例（instance）分离**：`common/` 定义内容，存档**只存实例状态** | 我们两头都存：内容在 JSON，实例又在 `*SaveData` 里带一份定义 id | 内容改一次，存档就可能对不上 |
| **定义引用用字符串 key**（`faith="catholic"`、`title="d_xxx"`），不是数组下标 | 我们用 `int` id（`religionId`、`biomeId`、`buildingDefId`…） | 内容重排/增删 → 老档指向错条目（我们已踩过：群系 id 从 0 起、宗教 id 从 100 起） |
| **扁平实体表 + 父指针**：`titles`/`provinces`/`blocks` 各是一张表，层级靠 `parent=` 字段 | 我们用**嵌套对象树**（`SettlementBlockSaveData.children[]`） | 撞 Unity 序列化深度 10（已实测），深层数据被静默截断 |
| **加载期迁移 + 版本号**，不维护旧形状 | 我们维护 `[FormerlySerializedAs]` + 多套 DTO（`SaveData` vs `GameSaveData`） | 双轨、丢失、无人敢删 |

⇒ **结论：`Save.Contracts` 不该是"运行时对象的影子"，而该是"存档文件自身的 schema"**：扁平表、字符串键、父指针、显式字段清单。

### 这对已完成部分的影响

| 已做 | 判断 |
|---|---|
| 契约程序集与 Infra 瘦身方向（A0/A3） | ✅ 仍然正确——存档格式定义本来就不该依赖模拟层 |
| 纯字段 DTO 迁出（Tile/WallSegment/Province/Market/HabitationSite/Block/Entity 三项） | ✅ 保留；它们已经是"文件 schema"的一部分 |
| B 方案（把枚举编码成 int） | ❌ **撤销**。CK3 用字符串键，我们用 int 是把问题换了个形式 |
| 嵌套 `children[]` | ⚠️ 需改扁平：`blocks` 表 + `parentBlockId` |

### 修正后的下一步顺序

1. **扁平化区块树**：`SettlementBlockSaveData.children[]` → 同一张 `blocks[]` 表 + `parentBlockId`，一次解决深度 10 与递归映射两个问题（映射器改为两趟：先建实体、再挂父）。
2. **定义引用串化**：新迁入的 DTO 里，`biomeId`/`buildingDefId`/`unitTypeId` 等逐步改 `string` key（配 `ContentResolvers` 反查）；老 int 字段保留只读兼容。
3. **再谈枚举**：枚举留在 Simulation 也无妨——只要 DTO 用 `string`/`int` 原语承载，`Save.Contracts` 就不需要枚举。

---

## 8. 一条判据（以后每次改架构都问）

> **如果删掉一个程序集的引用，编译还能过吗？**
> 能过，说明这层真的解耦了；只报几百个错，说明它还在直接摸上层。

当前答案：`Rendering`、`Laws`、`Faith`、`Culture` 能过；`Infrastructure`、`UI` 不能——这就是"没做完"的定义。
