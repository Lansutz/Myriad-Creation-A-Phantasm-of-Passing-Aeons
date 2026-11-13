# VERIFY-round2：Round2 修复有界复核（R1/R2/R3/R5 + PlayMode 真绿）

- **复核者**：verifier（只读）
- **基线**：`architecture-2-0-refactor`；`Logs/editmode-results.xml` = **306/306**（`start 2026-10-31T10:01:35Z`，0 failed/0 skipped，复算于本轮）；`Logs/playmode-results.xml` = 1/1 passed（`10:01:05Z`，4.79s）
- **代码快照**：SaveSystem/MapUnitRegistry 17:57:53、UnitProvisioning 17:58:01、TreasuryLedger 17:59:07、WorldGenerationSmokeTests 17:59:15 → 全部早于两个结果文件（18:01），结果覆盖最终代码
- **约束**：未改 `Assets/**`、未运行 Unity/编译/测试、git 只读、只写本文件

## 0. 结论摘要

| 项 | 结论 | 一句话 |
|---|---|---|
| R2 代码顺序 | **PASS** | `AdoptNextId` 已移到 `RegisterArmyViews` 之前，代码层面真实修复 |
| R2 新测试有效性 | **PASS-with-caveat** | 断言方向对，但**当前构造顺序下对旧 bug 不敏感**（见 F3） |
| R1 回退链路 | **FAIL** | 真实生成局仍发不出传令官：国库有钱无粮、区域库有粮无钱，`CanPayFromRegional` 要求同一库两样齐全（F1） |
| R1 reason 正确性 | **PASS-with-caveat** | "区域库无粮国库有钱"→"国库粮草货物不足"正确；"两处都空"→只归因国库，非空但误导（F4） |
| R3 回滚 | **PASS-with-caveat** | `MapUnitRegistry.Restore` 已接入且快照合理；`CharacterKnowledgeRegistry` 仍未回滚（已知 P1） |
| R5 | **PASS（+边缘 caveat）** | money-only 显式 `InvalidGoodsId`、货币缺失走 `realm.treasury` 兜底；无货币物产时仍会"钱扣了货没发"（F5） |
| 306/306 | **PASS** | XML 复算通过，且 306 = 305 + 新增 1 条（`PersonalFunding_FallsBack...`） |
| PlayMode "地图可见" | **FAIL（假绿）** | 截图为**单一颜色 (205,205,205)**；断言基准写死 (0.1,0.15,0.25)，非背景 64/64 恒真（F2） |
| B 线场景/asset | **PASS-with-caveat** | Main.unity 的 4 个关键脚本 GUID 均可解析到当前 meta（GameManager/MapRenderer/GameWorld/Bootstrap）；场景能被 PlayMode 加载；未验证视觉完整性 |
| MapRenderer 守卫 | **PASS** | `world != null && world.config != null` 守卫存在于 `MapRenderer.cs:186-187,574-578`、`MapRenderer.Colors.cs:91-92`、`MapRenderer.Nomadic.cs`（无法从 git 归因"本轮新增"，文件未跟踪） |

---

## 1. R2：顺序修复 PASS（读代码确认）

`SaveSystem.cs` 当前 ApplyToWorld 尾部（逐个函数调用核对）：

```
RestorePersistentEntities(data, world);          // 内部 MapUnitRegistry.Clear() → _nextId=1，按存档 id 回填非军队单位
int nextUnitId = 1;
foreach (var mapUnit in MapUnitRegistry.All)     // 此时注册表只有已还原的地图单位
    if (mapUnit != null && mapUnit.unitId >= nextUnitId) nextUnitId = mapUnit.unitId + 1;
MapUnitRegistry.AdoptNextId(nextUnitId);         // ← 先推进游标
ArmyPersistence.RegisterArmyViews(world);        // ← 再登记军队视图（FromArmy 无 unitId → 从 max+1 分配）
world.PrepareLoadedWorld();
```

与 Round1 的失败形态（`RegisterArmyViews` → `AdoptNextId`）相反，**顺序已正确**，军队视图不会再占用已还原条目的 id。代码层 PASS。

## 2. F1【P0 · R1 仍未在真实生成局修好】

**链路（全新标准局，不改任何法律）**

1. `InitializeTradeCenters` 只给每个区域库粮草：`GameWorld.cs:846 tradeCenter.inventory[0] = 1000f;`（**无货币物产 60**）。
2. 国家金库只从 `realm.treasury` 迁入货币：`TreasuryLedger.cs:92-121 ReconcileNationalCurrency`（只迁 currencyGoodsId，**永远不会出现粮草**）。
3. 默认法律 → `Personal` → `TreasuryLedger.cs:176-186` 先 `TryPayNational`：货币够、粮草为 0 → 退款 + `"国库粮草货物不足"`。
4. 回退区域库：`CanPayFromRegional`（`TreasuryLedger.cs:209-246`）在 `cost.money>0` 时**要求区域库有货币物产**、`cost.food>0` 时要求有粮草 → 区域库只有粮、没有货币物产 60 → 回退同样失败。
5. 结论：`SpawnDecreeCouriers` 仍 `order.state=Failed`（`GameWorld.cs:2186-2191`），传令官依旧发不出。只有当经济系统在此后若干天把货币物产产进该区域库，才可能成功——**开局/前期仍不可用**，R1 未真正关闭。

**新增测试为何没抓到**：`ProvisioningLedgerTests.cs:166-168` 手工给区域库同时塞了 `inventory[0]=100; inventory[60]=100`（还刻意 `treasury:0`），恰好绕开了"生产环境区域库没有货币物产"这一决定性事实——与 Round1 同类问题（测试构造掩盖生产现实）。

**最小反例（可直接转成 EditMode 测试）**：新建 64×32 标准世界（`InitializeTradeCenters` 后区域库只有 key 0）→ 取默认法律政权 → `world.IssueDecreeOrders(decreeId, realmId)` → 断言 `orders` 全部 `Failed`（现实现），期望为有传令官。或纯逻辑复刻：`realm.treasury=1000`、`tradeCenters[0].inventory={0:1000}`、`cost=ForCourier(5,0,60)` → `TryFund(...)` 返回 false。

**建议最小修法**：允许"拆分付款"——货币走国家金库（`realm.currencySystem.vault` 已由 `ReconcileNationalCurrency` 备好），粮草走区域库（`tradeCenters[regionId].inventory[0]`）；或生成局时给区域库也播种少量货币物产；或 `SpawnDecreeCouriers` 前用 `realm.treasury` 从区域库购粮。三者任选，需补"生成局默认法律真能发传令官"端到端测试。

## 3. F4【P2 · reason 归因】lead 指定的两个场景

| 场景 | 实际返回 | 评价 |
|---|---|---|
| 区域库无粮、国库有钱 | `"国库粮草货物不足"`（`TryPayNational` 设，Personal 回退失败后原样返回） | 正确：国库确有粮草缺口 |
| 两处都空 | `"国库货币货物不足"`（先货币后粮草，第一个失败点即返回） | **非空但不完整**：区域库也不足未体现；玩家/日志会误以为只需补国库 |

另：`TryPayNational` 的每条 false 分支都会写 reason（`"国库货币货物不足"/"国库不足"/"国库粮草货物不足"`），不存在空 reason 落到 `order.note` 的情况 ✅（`GameWorld.cs:2186-2191` 的 note 不会是空串）。

## 4. F2【P0 · 测试假绿】PlayMode "地图可见" 判定不成立

- 证据 1：测试自己落盘的 `D:/map_verify.png`（640×360，18:01，与本次 playmode 运行同刻）经像素统计为**整幅单一颜色 (205,205,205)**（230400/230400 像素同色，unique=1）。这张图**不是地图**。
- 证据 2：`MapVisibilityPlayModeTests.cs` 的判定是"像素 ≠ 背景色 (0.1,0.15,0.25) ±0.05"，而实际渲染底色是 0.804 灰 → 每个采样点都"非背景"，日志 `[MapVisible] 全图非背景 64/64` 是**恒真式**，与地图是否渲染无关。
- 证据 3：`LogAssert.ignoreFailingMessages = false` 是严格的（PlayMode 日志无 Error/Exception，仅 licensing 与 shader fallback 警告），所以**不是 LogAssert 宽松导致的假绿**；假绿来自断言基准与场景实际 clear/bg 不同 + "非背景"阈值定义过弱（全灰/纯洋红/黑都会通过）。
- 后果：不能宣称"地图可见"已修复。当前测试只能证明"场景能加载、GameWorld/MapRenderer/相机对象存在、离屏渲染没崩"。

**修法建议（任选/组合）**：① 断言"像素颜色多样性"（如 unique 颜色数 > 8，或陆地/海洋/政权色中至少两类出现）；② 用相机 `clearFlags` / 场景实际 clear color 动态取基准，而不是写死；③ 直接断言相机视锥内有地图 shader 输出（如采样 `MapPlane` 渲染纹理的 average 非均一 + 方差 > 阈值）；④ 保存的 PNG 做哈希/直方图并纳入断言（当前只写盘不断言）。

## 5. R3：回滚补齐 PASS-with-caveat

- `SaveSystem.LoadGame` 现在快照 `new List<MapUnit>(MapUnitRegistry.All)`，catch 中 `MapUnitRegistry.Restore(previousMapUnits)`（`MapUnitRegistry.cs:29-43`：Clear → 按对象 unitId 重登记 → `AdoptNextId(max+1)`）。快照是引用拷贝；ApplyToWorld 只新建 MapUnit 对象、不改旧对象，故回滚有效 ✅。
- **仍漏**：`CharacterKnowledgeRegistry.Clear()/Install`（`SaveSystem.cs:353-356`）不在回滚集；失败发生在该步之后的读档仍会清掉旧世界角色知识。lead 已声明为 P1 记录，本次不计 FAIL，但 P0-5 仍非完全闭环。
- 其余静态表（Relation/Contract/DynamicGoods）Round1 已覆盖且为深拷贝 ✅。

## 6. R5：PASS（+ F5 边缘 caveat）

- `UnitProvisioning.cs:47-56` money-only 重载显式 `foodGoodsId/currencyGoodsId = InvalidGoodsId` ✅（Round1 的"0=未指定→用粮当货币"陷阱关闭）。
- `TreasuryLedger.cs:163-173` 删除了"缺少货币物产"的硬失败，货币缺失时走 `realm.treasury` 旧账本（`TryPayNational` 的 `currencyGoodsId < 0` 分支）✅；粮食缺失仍硬失败（`:169-173`）✅。
- **F5【P2】**：该兜底路径下 `funding.currencyGoodsId` 仍为 -1，而 `UnitProvisioning.Equip` 只在 `currencyGoodsId >= 0` 时发货（`UnitProvisioning.cs:111-114`）→ 在没有 PreciousMetal 物产定义（模组/精简内容）时，"货币从 realm.treasury 扣了、背包没收到"的旧症状局部复现；当前生产调用点 `SpawnDecreeCouriers` 传的是 `FindCurrencyGoodsId()`（通常 60），仅内容表缺失时触发。

## 7. F3【P2 · 测试灵敏度】R2 新断言按当前构造顺序抓不到旧 bug

`WorldGenerationSmokeTests.cs` 顺序：`CreateArmy(firstRealmId,...)`（:91）→ `SpawnActor(Caravan)`（:94）→ save → load → 断言 `Count == armies.Count + AllActors.Count`（:126）。

- 测试用 `WorldSetupOptions.CreateStandard()`，Standard 预设 `generateInitialUnits=false, initialUnitCount=0`（`WorldSetupOptions.cs` Standard 分支）→ `GenerateInitialUnits` 提前返回，生成期**不产生**地图单位。
- 于是存盘时注册表 = {army 视图 id=1, caravan id=2}。按 Round1 旧代码（`RegisterArmyViews` 先于 `AdoptNextId`）：Clear 后 `_nextId=1` → 还原 caravan(id2) → 军队视图拿 id1 → 注册表仍 {1:army, 2:caravan}，`Count=2 = 1+1` → **断言照样通过**。
- 要让它能抓旧 bug：caravan 必须**先于** `CreateArmy` 创建（caravan id1 → 旧代码军队视图抢 id1 覆盖它 → Count=1≠2），或直接比较 `(unitId, source, sourceId)` 集合。当前断言是"方向正确但不敏感"，建议改成上述之一。

## 8. 回归/遗留清单

| 级别 | 项 | 状态 |
|---|---|---|
| P0 | R1 生成局仍发不出传令官（F1） | **未修好** |
| P0（测试质量） | PlayMode "地图可见" 假绿（F2） | **新增证据**；本轮无功能回归，但宣称 PASS 不成立 |
| P1 | `CharacterKnowledgeRegistry` 未纳入读档回滚（R3 已知） | 遗留（lead 已记录） |
| P2 | R2 新断言对旧 bug 不敏感（F3） | 建议改顺序/改断言 |
| P2 | "两处都空" reason 只归因国库（F4） | 建议补区域库原因 |
| P2 | 无货币物产内容下"钱扣了货没发"局部复现（F5） | 边缘，建议 `Equip`/`TryPay` 记录实际扣款账户 |

## 9. 建议（按优先级）

1. **立即修 F1**（R1 真关闭）：拆分付款或生成局给区域库播货币；补"默认法律 + 真实生成局 `IssueDecreeOrders` 能 spawn 传令官"端到端测试，禁止再用手工塞区域库的构造。
2. **立即修 F2**：PlayMode 断言改为颜色多样性/方差/动态基准；把 `D:/map_verify.png` 的像素统计纳入断言（否则该 PNG 是唯一线索，且当前证明地图未渲染）。
3. **F3**：交换 smoke 测试中 caravan/army 的创建顺序（或在 `CreateArmy` 前再 spawn 一个 actor），使 R2 断言具备回归灵敏度。
4. **F4/F5/R3 遗留**：reason 合并归因；无货币物产时的账实一致；`CharacterKnowledgeRegistry` 回滚可复用 `SaveEntityBridge.CaptureKnownCharacters/InstallKnownCharacters`。

## 10. 待验证

- F1 为静态推演（未运行 Unity）。若产品口径允许"经济系统跑几天后区域库自然有货币"，则影响降级为"前期不可用"而非"永远不可用"；仍建议端到端测试定性。
- F2：未逐像素确认 205 灰到底是 URP skybox 还是 RenderTexture 未写入；无论哪种，**测试证明不了地图可见**这一结论成立。
- B 线场景视觉完整性（TMP 字体、渲染管线、UI 绑定）未复核，仅验证 GUID 可解析 + 场景可加载。
- MapRenderer `world.config` 守卫无法归因本轮（文件未跟踪、无 diff 基线）。

## 附录：本轮关键证据命令（只读）

```bash
grep -o 'testcasecount|total|passed|failed' Logs/editmode-results.xml   # 306/306/0
head -c 800 Logs/playmode-results.xml                                   # 1/1 passed
grep -n "\[MapVisible\]" Logs/playmode.log                              # 非背景 64/64
# python(PIL)：D:/map_verify.png → size 640x360, unique colors = 1, (205,205,205)×230400
sed -n '230,300p' SaveSystem.cs                                          # AdoptNextId → RegisterArmyViews 顺序
sed -n '130,270p' TreasuryLedger.cs                                      # Personal 回退/来源回写
sed -n '1860,1910p' GameWorld.cs + WorldSetupOptions Standard 分支        # 生成期不产 actor
grep m_Script Assets/Scenes/Main.unity → 4 个关键 GUID 解析到当前 meta
```
