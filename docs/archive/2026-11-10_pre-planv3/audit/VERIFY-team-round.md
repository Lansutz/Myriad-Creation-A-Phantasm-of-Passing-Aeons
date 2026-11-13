# VERIFY-team-round：复核 task-7（原子写）/ task-8（经济守恒）/ Lead 全链路测试

- **复核者**：verifier（只读）
- **基线**：`Logs/editmode-results.xml` = **324/324 passed，0 failed，0 skipped**（`start 2026-10-31T11:17:26Z`，复算：`test-case` 条目 648 = 324×2）；定向 `Logs/team-fixes2.xml` = **13/13**（EconomyConservation 7 + SaveSystemAtomicWrite 5 + SaveSystemRoundTrip 1）；`editmode-team.log` 中 `error CS` / `Compilation failed` 均为 **0**
- **代码快照 vs 测试**：SaveSystem 19:08:01、TradeCenter 19:08:17、EconomyManager 19:08:30、SaveAtomic 19:08:56、EconomyConservation 19:08:58、RoundTrip 19:16:43 → 均早于全量 run（19:17:26）✅
- **约束**：未改 `Assets/**`、未跑 Unity/编译/测试、git 只读、只写本文件
- **git 归因限制**：`TradeCenter.cs` / `EconomyManager.cs` / 两个新测试 / RoundTrip 测试全部是 `??`（未跟踪），`git diff` 无法给出"本轮前后"；旧 `AddGoods` 语义用**遗留副本** `Scripts/Economy/EconomySystem.cs:25-90`（B 报告重复类型）作对照基线

---

## 1. 逐项复核表

| # | 复核项 | 结论 | 关键证据 |
|---|---|---|---|
| 1 | 原子写·正常路径（写 .tmp → 回读校验 → File.Replace/Move → 清 .bak） | **PASS** | `SaveSystem.cs:58-91`；先写同目录 `.tmp`、长度回读校验（`:74`）、存在走 `File.Replace`（`:85`）、不存在走 `File.Move`（`:91`）、成功清 `.bak`（`:87`）、`finally` 清 `.tmp`（`:107`，幂等） |
| 2 | 原子写·失败路径是否保留旧档 | **PASS-with-caveat** | 写前失败/校验失败：旧档不动（`SaveSystem.cs:66-78`，失败仅 return false）；`.bak` 只在 `File.Replace` 内部充当旧档，但**代码里没有任何从 `.bak` 恢复的路径**（`LoadGame:142`、`ListSaves:221` 只认 `*.sav`）→ `:81-86` 注释承诺的"备份兜底"只兑现了一半 |
| 3 | `.tmp`/`.bak` 残留 | **PASS-with-caveat** | 成功路径 `DeleteFileQuietly(tempPath)`（`:107`）+ `DeleteFileQuietly(backupPath)`（`:87`）覆盖；但两者都是"清理失败只 `Debug.LogWarning`"（`:126-138`），且**没有启动时清扫陈旧 `.tmp/.bak`**（跨版本崩溃留下会永久堆积）；`File.Replace` 在 Unix 两步 rename 之间失败可留下"`.sav` 缺失 + `.bak` 存在"的半状态（Windows ReplaceFile 更稳），此时无恢复入口 → 用户丢档 |
| 4 | `IsValidSaveName` 是否误伤 / 是否够用 | **PASS-with-caveat** | 校验在创建目录前（`SaveSystem.cs:40`），空/空白/`/`/`\`/`..`/非法字符全拒（`:112-125`）；现网调用只有 `autosave`/`quicksave`（`UI/Bootstrap.cs:161,177`）→ 不误伤。**但 `LoadGame(saveName)`（`:142`）与 `DeleteSave(saveName)`（`:210`）没有同一校验** → `LoadGame("../x")` 会读到 Saves 目录之外、`DeleteSave("../x")` 可删目录外文件（本地游戏威胁低，但"写侧堵、读删侧不堵"不一致） |
| 5 | `AddGoodsReturningAccepted` 数学/边界 | **PASS** | `TradeCenter.cs:52-67`：`goodsId<0`→0、`amount<=0`→0、容量 0/负→0（`Mathf.Max(0, capacity-total)`）、部分入库返回实收量、不超容；与测试 `EconomyConservationTests.cs:51-60` 的 100/150 用例一致 |
| 6 | 旧 bool `AddGoods` 兼容性 | **PASS-with-caveat** | `TradeCenter.cs:72-79`：`amount<=0 → amount>=0`；正数整单 true / 部分 false（与旧 `Scripts/Economy/EconomySystem.cs:59-72` 的"部分入账也返回 false、但已收部分留着"一致）✅。差异仅在旧实现的腐化边界：旧代码在**已超容**时 `available<0` 会 `inventory[g] += available`（把其它库存减成负/凭空减货），新实现 clamp 为 0 —— 属修正而非回归；活调用方只有遗留 `EconomySystem.cs:504`（B 报 P0-3 的死/旧路径），无回归面 |
| 7 | 商队到货守恒（来源扣减 = 入库 + cargo 剩余 + 损失） | **PASS** | 发车：`InitiateTrades` `RemoveGoods` 后 `cargo[num]=num4`（`EconomyManager.cs:150-162`）→ 扣减=装载；到货：`DeliverCargo`（`:211-235`）逐项 `accepted = AddGoodsReturningAccepted`，`left` 写回 cargo（≤ε 才删）；退役：`ReturnCaravanCargo`（`:241-269`）先退库、余量记 `lost` 并 `Debug.LogWarning`（`:261`）后清空 cargo → 恒等式闭合，损失可追踪（仅日志、无累计统计字段） |
| 8 | `RetireCaravan` 重复调用是否重复退款 | **PASS** | `EconomyManager.cs:271-276`：`ReturnCaravanCargo` 末尾 `caravan.cargo.Clear()`（`:267`）→ 二次调用 `before=0`，返回 0、不再退库；`MoveCaravans` 倒序遍历（`:171`）+ `RemoveAt(index)` 后 `continue`，索引不越界；不存在外部重复调用点 |
| 9 | 容量不足商队滞留 / 20 上限 | **FAIL（P1）** | 到货装不下时商队**永久留在 `_caravans`**（`:196-206` 只警告、不退役；`isMoving=false` 后每 tick 重试）：① 目的地被**非易腐货物**填满（`ProcessShelfLife:366-382` 只衰减 `hasShelfLife` 的货，木材/武器/矿不衰减）时容量永不释放 → 商队永占；② `InitiateTrades` 仅 `if (_caravans.Count > 20) return;`（`:123`）→ 滞留可把队列占满，**贸易整体停摆**；③ `>` 而非 `>=`，且单次 `InitiateTrades` 会对所有中心/路线各发一辆、循环内不复查上限（`:127-166`）→ 实际可远超 20；④ 无"滞留 N 天后退役/减载/转运"机制 |
| 10 | 缺粮惩罚 vs GameWorld Famine 事件是否双重惩罚 | **PASS-with-caveat（P2）** | 新增 `ApplyFaminePenalty`（`:349-364`：稳定 -1/日、秩序 -0.5/日、人口 -0.1%/日）；既有 `PopulationTick` 在区域粮 < 人口 1% 时发 `GameEventType.Famine`（`GameWorld.cs:1093-1099`）→ `HandleFamine`（`:383-398`：人口 ×0.95、满意度 -20、稳定 -10）。两个调度都是 Daily 且经济(优先级12)先于人口(14) → **同一 tick 内叠加**，量级差约 10 倍（事件主导）。可接受但需明确设计口径。另：`RemoveGoods` 是全有全无（库存 < 当日需求时**不扣也不清零**，`:332-339`）→ 城市会长期"守着不足的粮饿死"，新惩罚把该既有缺陷放成"永久饥荒循环" |
| 11 | 新增 13 条测试是否真会失败 | **PASS-with-caveat** | 均为真实数值断言，非空转：`CityWithoutFood`（`EconomyConservationTests.cs:160-173`）断言 stability 59<60/order 39.5<40/pop 99.9、`CityWithFood` 断言精确 99 库存（`:176-190`）；`SaveGame_WriteFailure`（`:163-201`）用"`.tmp` 路径是目录"构造确定性 `File.WriteAllText` 异常，并逐字节比对旧档 ✅。缺口见 §3 |
| 12 | 既有 311 条测试旧语义依赖 | **PASS** | 全库 `AddGoods(` 的测试引用只有 `EconomyConservationTests`；`SaveGame(` 的既有测试只有 `InnovationPersistenceTests:87`（合法名）；`324/324` 全绿佐证 |
| 13 | 范围检查（越界改动 / 调试残留） | **PASS-with-caveat** | 本轮 19:00-19:16 改动仅：SaveSystem、TradeCenter、EconomyManager、3 个新测试 + `.meta`；**无既有测试文件被改**（`git status` 的 M 都不是本轮 mtime）；6 个文件内无 `TODO/FIXME/D:/`/调试残留。另 19:01-19:03 有 `MapRenderer.cs`、`MapRenderer.Colors.cs`、`MapVisibilityPlayModeTests.cs` 改动，**不在两张卡授权范围**（按时间与内容推测为 Lead 修 Round2-F2 假绿，git 无作者信息，无法归因） |
| 14 | Lead 的 `SaveSystemRoundTripTests` 字段级全链路 | **PASS-with-caveat** | `SaveSystemRoundTripTests.cs:76-214`：日历/地块/省份/聚落(人口财富守军市场区划设施)/军队(士气补给组织训练编制行军)/国库/区划 holder/革新逐字段断言，且先制造可识别值再比 → 真能抓字段丢失。唯一弱点是革新段：`researchedInnovation` 若研究未完成则整段断言被条件跳过（`:209-213`），研究链回归会静默漏检 |

---

## 2. 问题清单（按级别）

| 级别 | 问题 | 位置 |
|---|---|---|
| **P1** | 容量不足商队**无限占位**：非易腐货物填满仓储后容量永不释放，`_caravans` 只增不减；`InitiateTrades` 以 `>20` 封顶（且可单 tick 超发）→ 一旦滞留数 ≥ 上限，**贸易永久停摆**（进而城市缺粮）。这是"到货不蒸发"修复引入的新长期风险，且无测试覆盖 | `EconomyManager.cs:123,169-208,366-382` |
| **P1** | `.bak` 只有"创建/清理"没有"恢复"：代码注释承诺"备份兜底"（`SaveSystem.cs:81-86`），但 `LoadGame/ListSaves` 只认 `*.sav`（`:142,221`）；一旦 `File.Replace` 处于"已挪走旧档、未放进新档"的失败窗口（Unix 两步 rename；Windows ReplaceFile 概率低），用户旧档只剩不可见 `.bak` → 实际丢档 | `SaveSystem.cs:81-91,142,221` |
| **P2** | `LoadGame`/`DeleteSave` 未复用 `IsValidSaveName`：读/删侧未防目录穿越（写侧已防） | `SaveSystem.cs:40,112-125,142,210` |
| **P2** | 缺粮双惩罚叠加（新小额日罚 + 既有 Famine 事件大额）且"库存不足日需"时既不扣粮也不清空 → 长期饥荒循环；建议明确取舍（例如事件触发时抑制日罚，或对不足量按比例扣减） | `EconomyManager.cs:308-364` vs `GameWorld.cs:383-398,1093-1099` |
| **P2** | 新测试缺口：未覆盖 `MoveCaravans`/`InitiateTrades`/`RetireCaravan`/20 上限（守恒用 `DeliverCargo` 直调绕过循环）；写失败只覆盖"写前失败"，未覆盖"写后读回失败/替换失败"；`.tmp` 残留检测用 `Directory.GetFiles` 看不见 `.tmp` **目录**（本次靠 finally 兜底） | `EconomyConservationTests.cs:74-132`；`SaveSystemAtomicWriteTests.cs:163-201` |
| **P2** | 守恒"损失"只打日志、无累计字段/UI 可见性；`ReturnCaravanCargo` 的 `lost` 返回值无人消费 | `EconomyManager.cs:259-268` |
| **P2** | 遗留重复实现仍在：`Scripts/Economy/EconomySystem.cs` 里还有旧 `TradeCenter`/`Caravan`（`AddGoods` 保留"超容时加负数"腐化语义）；若场景仍绑旧 MapEditor（B P0-3），旧经济路径行为分叉 | `EconomySystem.cs:25-90,176-218` |
| **P2** | `SaveGame` 无并发/重入保护（autosave 与手动存档同 tick 可能交错写同一 `.tmp`） | `SaveSystem.cs:58-107` |

---

## 3. 测试有效性专项结论

- **真会失败**：13 条都含明确期望值/字节比对/对象状态断言，未发现恒真断言、try/catch 吞断言、`[Ignore]`。`SaveGame_WriteFailure` 的"目录占位"在 Windows 编辑器与 CI 上稳定触发 `WriteAllText` 异常（且 `finally` 清理，不污染后续）。
- **`LogAssert.ignoreFailingMessages = true` 不会跨测试泄漏**：框架按测试创建 `LogScope`（`BeforeAfterTestCommandBase.cs:150,243` 的 `using (new LogScope())`），属性挂在 `LogScope.Current` 上，测试结束 Dispose → 后续测试恢复默认 false（已核对，非隐患）。
- **缺口**：见 §2 的 P2 测试项；最关键的是"滞留/上限"与"替换阶段失败"两条新语义完全没有测试，而这正是两张卡最容易回归的地方。
- **全量 324/324 与定向 13/13 一致**：324 − 311 = 13 条新增，数量闭合，无跳过/忽略。

---

## 4. Merge gate 结论

| 卡 | 结论 | 理由 / 前置条件 |
|---|---|---|
| **task-7（原子写）** | **可合入（附 P2 follow-up）** | 正常/失败主路径正确，原子性和旧档保护在"写前失败"层面真实有效，测试可失败。合并前建议顺手修：① `LoadGame` 在 `.sav` 缺失且 `.bak` 存在时优先读 `.bak`（补齐注释承诺）；② `LoadGame/DeleteSave` 复用 `IsValidSaveName`；③ 启动时清扫陈旧 `.tmp/.bak`。均为小改动，不阻塞。 |
| **task-8（经济守恒）** | **需修后再合入** | 守恒恒等式/`AddGoodsReturningAccepted`/`RetireCaravan` 本身正确且测试扎实；但"容量不足商队无限占位 + 20 上限失效"是可复现的长期死锁（P1），且没有测试兜住 `MoveCaravans/InitiateTrades` 的循环。最低修复：① 商队滞留超过 N 天→退役（走 `RetireCaravan`，损失记日志）；② `InitiateTrades` 改 `>= 20` 并在循环内复查；③ 目的地容量不足时按比例/逐项减载或改道；④ 补 1 条"容量满 → 多日 tick → 队列不无限增长/贸易仍能恢复"的集成测试。 |
| Lead 全链路测试 | 可随 task-7 合入 | 字段级断言覆盖到位；建议把革新段改成"前置失败即 `Assert.Fail`"而非条件跳过。 |

---

## 5. 待验证

1. 未运行 Unity：§2 P1 贸易停摆为静态推演（容量填满需 100+ 车次、依赖非易腐货积累），建议用长时间 tick 集成测试确认；`.bak` 丢失窗口在 Windows 上概率低（ReplaceFile 语义），需视目标平台确认。
2. `MapRenderer*/MapVisibilityPlayModeTests` 19:01-19:03 改动的内容与质量未复核（不属本任务卡；若属 Round2-F2 修复，建议单独看假绿断言是否已修正）。
3. 未验证 `SaveGame` 并发重入的实际发生路径（UI 是否可能同帧触发两次存档）。
4. 遗留 `Scripts/Economy/EconomySystem.cs` 旧经济路径在运行时是否仍可达（B 报 P0-3 场景绑定问题），未复核。

## 附录：关键只读证据

```bash
grep -o 'testcasecount|total|passed|failed|start-time' Logs/editmode-results.xml   # 324/324/0
grep -o 'total|passed|failed' Logs/team-fixes2.xml                                  # 13/13
grep -c "error CS" Logs/editmode-team.log                                           # 0
sed -n '38,140p' SaveSystem.cs                                                      # 原子写实现
sed -n '52,79p' TradeCenter.cs                                                      # 新/旧 AddGoods
sed -n '121,276p' EconomyManager.cs                                                 # 上限/移动/到货/退役
sed -n '308,382p' EconomyManager.cs                                                 # 缺粮惩罚/保质期
sed -n '383,398p;1093,1099p' GameWorld.cs                                           # Famine 事件链
sed -n '25,90p' Scripts/Economy/EconomySystem.cs                                    # 旧 AddGoods 对照基线
find Assets -newermt "19:00" -type f                                                # 范围检查
```
