# VERIFY-team-round2：复核 follow-up 修复（.bak 恢复 / 商队滞留退役）

- **复核者**：verifier（只读）
- **基线（复核时已落盘）**：
  - 全量 EditMode：`Logs/editmode-results.xml` = **331/331 passed，0 failed，0 skipped**（`start 2026-10-31T11:26:58Z`，end `11:27:02Z`）
  - 定向：`Logs/team-round2.xml` = **20/20**（EconomyConservation 10 + SaveSystemAtomicWrite 9 + SaveSystemRoundTrip 1，`11:26:38Z`）
  - PlayMode gfx：`Logs/playmode-gfx-results.xml` = **1/1 passed**（`11:27:19Z`），`playmode-gfx.log` 有真实渲染采样
  - 331 = 324 + 7 条新增（SaveAtomic +4、Economy +3），数量闭合；`editmode-team2.log` 无 `error CS`，仅 licensing 噪声
- **时间序**：代码 19:21:35–19:25:01 → 定向 19:26:38 → 全量 19:26:58→19:27:02 → PlayMode 19:27:19；**测试结果覆盖最终代码** ✅
- **约束**：未改 `Assets/**`、未跑 Unity/编译/测试、git 只读、只写本文件

---

## 1. 逐项复核表

| # | 复核项 | 结论 | 证据 |
|---|---|---|---|
| 1 | `.bak` 恢复：`LoadGame` 主档缺失时回退 | **PASS** | `SaveSystem.cs:192-203`（主档缺失且 bak 存在 → `readPath=bak`）、`:205-216`（解析成功才 `RestoreSaveFromBackup` 回写主档）、`:147-165`（`File.Copy→.restore.tmp→Move`，失败保留 bak + `finally` 清理） |
| 2 | `.bak` 仅在 ListSaves 可见 | **PASS** | `SaveSystem.cs:288-299`：第二个循环按 `.sav.bak` 后缀追加"可恢复档"名，`!names.Contains` 去重；第一个循环显式 `EndsWith(".sav")` 过滤 Windows 通配符误匹配（`:278-286`） |
| 3 | 恢复失败/损坏 `.bak` 保留 | **PASS** | `SaveSystem.cs:207-215`：解析失败直接 return null（不复制、不删 bak）；测试 `SaveSystemAtomicWriteTests.cs:322-336` 覆盖 |
| 4 | 读/删 `saveName` 校验 | **PASS** | `LoadGame` `:170-175`、`DeleteSave` `:254-259` 复用 `IsValidSaveName`（`:118-128`）；测试 `:256-284` 用 Saves 目录外哨兵文件验证 DeleteSave 不穿越（**该测试对旧代码敏感**：旧 DeleteSave 会删掉哨兵文件） |
| 5 | 陈旧 `.tmp` 清理 | **PASS-with-caveat** | `SaveSystem.cs:61-63` 写档前 `DeleteFileQuietly(tempPath)`；`DeleteSave` 连带清 `.tmp/.restore.tmp`（`:265-268`）。⚠️ 测试 `:291-315` **非判别性**（见 P2-3）；`.restore.tmp` 崩溃残留不在 SaveGame 清理（仅 DeleteSave） |
| 6 | 商队滞留 N 天退役 | **PASS** | `EconomyManager.cs:43`（`CaravanStuckRetireDays=5`）、`:221-242`（连续滞留计数、`>=5` 退役、首日警告、可入库即移除计数与商队）；测试 `EconomyConservationTests.cs:135-165` 断言第 4 天仍在、第 5 天退役、货退回起点、总量守恒（旧代码必失败） |
| 7 | 退役货物守恒/可追踪 | **PASS** | 退役走 `RetireCaravan(315-320) → ReturnCaravanCargo`（退起点/最近库，装不下记 `lost` 并 `Debug.LogWarning`，见 `:300-312`），`cargo.Clear()`；测试断言 origin 50 + dest 10000 = 出发前 10000 + 在途 50 |
| 8 | 滞留计数与退役交互残留 | **PASS** | `_caravanStuckDays` 仅 4 处（`:56,227,231-232,318`）；两处移除覆盖全部商队移除路径（可入库移除 `:225-229`、退役 `:315-320`），无其它 `_caravans.RemoveAt` 旁路；计数为运行时字典、不入档 |
| 9 | `InitiateTrades` 上限 `>=` + 循环内复查 | **PASS** | `EconomyManager.cs:132-137`（入口 `>= MaxActiveCaravans` 返回）、`:142-146`（路线循环内再复查，命中即 return）；`MaxActiveCaravans=20`（`:39-40`）；测试 `EconomyConservationTests.cs:168-208` 两个场景（19→只能发 1；20→不发），对"仅改入口不改循环"或旧 `>` 均敏感 |
| 10 | 缺粮先吃剩余再轻罚 | **PASS** | `EconomyManager.cs:369-390`：`foodAvailable < amount` 时先 `RemoveGoods(food, foodAvailable)` 清零再 `ApplyFaminePenalty`；惩罚降级为轻量 `0.2/0.1/0.0002`（`:44-52`）并注释明确"重度由 GameWorld Famine 负责"；测试 `:268-287` 断言库存归零、59.8/39.9/99.98 精确值 → 旧实现必失败 |
| 11 | 与 GameWorld Famine 共存 | **PASS-with-caveat** | 轻量日罚 + `HandleFamine`（`GameWorld.cs:383-398` -10 稳定/×0.95 人口）仍会同日叠加，但量级已 10× 差异化并在 `:55-60` 注释声明设计口径；可接受（P2：建议后续给玩家/日志统一口径） |
| 12 | 是否新增存档字段 | **PASS** | `SaveData.cs` mtime 18:16（本轮未动）；`GameConstants.SaveVersion` 仍 14；商队与 `_caravanStuckDays` 均为运行时状态（`EconomyManager.cs:244` 注释"商队本身不入档"） |
| 13 | 新测试是否真能失败 / 条件跳过 | **PASS-with-caveat** | 6 条中 4 条强敏感（滞留退役、上限、半日缺粮、非法名删档）；2 条非判别（陈旧 .tmp 清理、损坏 .bak 保留）见 P2-3。`SaveSystemRoundTripTests.cs:209` 已改为 `Assert.Greater(researchedInnovation, 0, ...)` 前置失败即失败，**条件跳过已消除** ✅ |
| 14 | 全量结果与声明一致 | **PASS** | 331/331、0 skipped；新增 7 条在 XML 中均为 `result="Passed"`；20/20 定向一致 |
| 15 | 范围检查（只改授权文件） | **PASS** | 19:20 后仅 5 个文件变动：SaveSystem、EconomyManager、2 个测试 + RoundTrip 测试（Lead 的严格断言）；`TradeCenter.cs` 本轮未动（mtime 19:08，属上一轮）；无 `.meta`/场景/asset 改动；5 文件内无 `TODO/FIXME/D:/`/调试残留 |
| 16 | 既有测试旧语义依赖 | **PASS** | 331/331 全绿；全库 `AddGoods(`/`CaravanStuck`/`MaxActiveCaravans`/饥荒常量的测试引用只出现在两个新测试文件；未发现依赖旧 `>20`、旧惩罚系数或旧 `DeleteSave`（仅 `.sav`）语义的断言 |

---

## 2. 上一轮两个 P1 的关闭判定

| 上轮 P1 | 现状 | 判定 |
|---|---|---|
| `.bak` 只有创建/清理、无恢复 | 主档缺失→读 bak→解析成功→复制回 `.sav`（保留 bak）；ListSaves 可见；损坏 bak 返回 null 且保留 | **已解决**（PASS） |
| 容量不足商队无限占位 → 贸易停摆 | 连续 5 天无法入库即退役并处置货物；上限 `>=` + 循环内复查，`_caravans` 不再无界增长 | **已解决**（PASS） |

残余（不阻塞）：满仓目的地仍可能被继续发车（发车前不检查目的地容量），形成"发车→5 天→退役→货退回来"的**自愈循环**（无货损、最多占满 20 槽 5 天）——P2。

---

## 3. 问题清单（本轮无 P0/P1）

| 级别 | 问题 | 位置/建议 |
|---|---|---|
| P2-1 | 只处理"`.sav` 缺失"，不处理"`.sav` 存在但损坏 + 有效 `.bak`"：此时直接返回 null，不会回退 bak | `SaveSystem.cs:192-212`；建议解析失败且 bak 存在时再试 bak（需区分"缺格式"与"损坏"） |
| P2-2 | `.restore.tmp` 崩溃残留不在 `SaveGame` 清理（只在 `DeleteSave`） | `SaveSystem.cs:147-165` vs `:265-268`；建议 SaveGame 顺带清理 |
| P2-3 | 2 条新测试**非判别性**：①`SaveGame_CleansStaleTemporaryFileBeforeWrite` 即使删掉 `:63` 清理行，`File.WriteAllText` 也会覆盖陈旧 `.tmp`，断言全过；②`LoadGame_CorruptBackup_KeepsBackupAndReturnsNull` 在旧代码（无 bak 逻辑）下同样返回 null 且保留 bak，无法区分修复 | `SaveSystemAtomicWriteTests.cs:291-315,322-336`；建议用"陈旧 `.tmp` 内容必须被显式删除"的可观测信号（如日志/文件 mtime）或改为直接测试清理行为 |
| P2-4 | 发车前不检查目的地剩余容量：满仓目的地仍会被持续发车，退役后原货退回再发，形成周期空转与告警噪声（有上限、自愈，无损失） | `EconomyManager.cs:132-183`；建议发车前 `destination.GetTotalInventory() < capacity` 预检或转投其它路线 |
| P2-5 | 缺粮"轻量日罚 + Famine 事件重罚"仍叠加（差异 10×、注释已声明口径） | `EconomyManager.cs:44-60` vs `GameWorld.cs:383-398`；建议后续在日志/UI 统一表述 |
| P2-6 | 为可测性新增公有入口 `InitiateTrades/MoveCaravans/RegisterCaravan`，外部可绕过 `DailyTick` 驱动 | `EconomyManager.cs:131,185,244`；可接受，建议注释"测试/系统集成用" |
| P2-7 | `DeleteSave` 语义变更：仅 `.bak` 存在时也返回 true（旧为 false）；现有调用方未依赖返回值，暂安全 | `SaveSystem.cs:263-269` |

---

## 4. PlayMode gfx 真绿确认（顺带复核上轮 F2）

- `playmode-gfx.log`：`[MapVisible] 全图非背景 12/64，unique 颜色 13，亮度范围 0.145~0.730，平均饱和度 0.401`；测试断言 `uniqueColors>8`、`maxLum-minLum>0.02`（`MapVisibilityPlayModeTests.cs:155-156`）→ 走的是真实渲染分支。
- 测试落盘 `D:/map_verify.png` 经像素统计：640×360，**unique 颜色 15049**，主色 (25,38,64)=178335 像素（即相机背景 0.1/0.15/0.25），其余为地形色（(177,173,136) 等）→ **确为真实地图渲染，上轮"单一灰底假绿"已修复**。
- 注意：该测试存在 headless 单色时退化为几何断言的注释分支（`MapVisibilityPlayModeTests.cs:114` 附近），本次 gfx 运行未走退化分支；如未来在 `-nographics` 下跑，需确认该分支仍有足够约束（不属本轮卡）。

---

## 5. Merge gate 结论

| 卡 | 结论 | 说明 |
|---|---|---|
| **task-11（.bak 恢复/校验/陈旧 tmp）** | **可合入** | P1 关闭；331/331 + 20/20 覆盖；残余仅 P2-1/P2-2/P2-3，建议后续迭代补"损坏主档回退 bak"与两条测试判别性。 |
| **task-12（商队滞留/上限/缺粮轻罚）** | **可合入** | P1 关闭（滞留 5 天退役 + 货有归属 + 上限真正生效 + 计数无残留）；残余 P2-4/P2-5 不阻塞。 |
| Lead `SaveSystemRoundTripTests` | **可合入** | 严格断言已生效，条件跳过消除。 |
| PlayMode gfx | **真绿** | 有像素多样性 + 几何断言双重证据。 |

---

## 6. 待验证

1. 未运行 Unity；"满仓目的地的周期空转发车"为静态推演（依据价格公式 `GetGoodsPrice` 对饱和目的地的抑制并不完全，具体频率需长时间 tick 观察）。
2. 未验证 `.sav` 损坏 + `.bak` 有效这一组合在真实 File.Replace 失败链中出现的概率（P2-1 属鲁棒性增强，非当前回归）。
3. PlayMode headless 退化分支的约束强度未验证（本次 gfx 运行未触发）。

## 附录：关键只读证据

```bash
grep -o 'testcasecount|total|passed|failed|skipped|start-time|end-time' Logs/editmode-results.xml  # 331/331/0/0
grep -o 'total|passed|failed|start-time' Logs/team-round2.xml                                   # 20/20
grep -o 'total|passed|failed|start-time' Logs/playmode-gfx-results.xml                          # 1/1
grep "\[MapVisible\]" Logs/playmode-gfx.log                                                     # unique 13 / lum 0.145~0.730
# python(PIL): D:/map_verify.png → 640x360, unique=15049, 主色 (25,38,64) 为背景
sed -n '147,216p;252,302p' SaveSystem.cs                                                        # bak 恢复/校验/ListSaves
sed -n '36,60p;132,242p;315,320p;369,390p' EconomyManager.cs                                     # 上限/滞留/退役/缺粮
grep -n "researchedInnovation" Tests/EditMode/SaveSystemRoundTripTests.cs                        # :209 严格断言
find Assets -newermt "2026-10-31 19:20" -type f                                                 # 范围：仅 5 个授权文件
```
