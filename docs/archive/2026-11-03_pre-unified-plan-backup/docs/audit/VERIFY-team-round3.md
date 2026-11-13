# VERIFY-team-round3：复核第三轮团队修复（P2 收口 + 角色入档）

- **复核者**：verifier（只读）
- **基线（复核时已落盘，均在最终代码之后）**：
  - 全量 EditMode：`Logs/editmode-results.xml` = **343/343 passed，0 failed，0 skipped，0 inconclusive**（`start 2026-10-31T11:51:06Z`，end `11:51:11Z`）
  - 定向最终：`Logs/team-round3b.xml` = **32/32**（`11:50:43Z`；32 = SaveAtomic 10 + Econ 12 + Character 6 + Legacy 3 + RoundTrip 1，逐类计数已复核）
  - 定向首跑：`Logs/team-round3.xml` = 31/32（`11:50:17Z`，失败项 `LegacySaveCompatibilityTests.GarbageJson_ReturnsNull_AndDoesNotCrash`，原因=预期的 Error 日志未声明；`LegacySaveCompatibilityTests.cs` 19:50:28 修正后 32/32）
  - PlayMode gfx：`Logs/playmode-gfx-results.xml` = **1/1 passed**（`11:51:29Z`），`playmode-gfx.log` 有真实渲染采样
  - 编译：`editmode-team3.log` 中 `error CS` = 0
  - 数量闭合：**331 + 1(save) + 2(econ) + 6(character) + 3(legacy) = 343** ✅
- **时间序**：脚本 19:41–19:48 → 测试 19:43–19:50:28 → 定向 19:50:43 → 全量 19:51:06 → PlayMode 19:51:29（最终代码被全部覆盖）
- **约束**：未改 `Assets/**`、未跑 Unity/编译/测试、git 只读、只写本文件

---

## 1. 逐项复核表

| # | 复核项 | 结论 | 证据 |
|---|---|---|---|
| 1 | **上轮 P2-1** 损坏主档回退 `.bak` | **PASS** | `SaveSystem.cs:223-240`（主档解析失败→试 bak，标记 `recoveredFromBackup`）、`:254-256`（仅成功才回写）；测试 `SaveSystemAtomicWriteTests.cs:394-422`（损坏主档→恢复世界、`.sav` 字节=备份、`.bak` 保留），`:358-387`（主档+备份都损坏→null、保留 bak 与损坏主档不改写，且用 `LogAssert.Expect` 断言两条 Warning 契约） |
| 2 | **上轮 P2-2** `.restore.tmp` 清理 | **PASS** | `SaveSystem.cs:60-66`（SaveGame 写前清 `.tmp` 与 `.restore.tmp`）、`:307-310`（DeleteSave 四个产物全清）；测试 `:310-347` 预置**只读** `.tmp` + `.restore.tmp`，断言 SaveGame 成功且两者消失 |
| 3 | **上轮 P2-4** 满仓不发车 | **PASS** | `EconomyManager.cs:49-55`（10% / 1 容量阈值）、`:187-196`（发车前 `destinationCapacityLeft` 预检，不足则 `continue` 且不扣货源）、`:214-225`（Warn 按 30 次调用节流，`_tradeStepCount++` 在 `:153`）；测试 `EconomyConservationTests.cs:211-236`（满仓→active 数不变/不扣货；腾出容量→恢复发车），对删掉预检的实现会真红 |
| 4 | **缺粮不再由本系统扣人口** | **PASS** | `EconomyManager.cs:445-455`：`ApplyFaminePenalty` 只改 stability/order；常量与注释改为口径 (a)（`:56-63`）；日志文案同步（`:440`）；测试 `EconomyConservationTests.cs:317-330` 新增 `FaminePenalty_DoesNotTouchPopulation` |
| 5 | econ 两条人口断言是否仅口径变更 | **PASS（口径变更，非削弱）** | `CityWithoutFood`（`:263-277`）把"人口下降"改为"精确 100"；`CityWithHalfDayFood`（`:295-315`）把 99.98 → 100；其余断言（稳定性/秩序/库存）原样且更精确；同时新增一条独立测试兜住该口径 → 属**增强**而非放宽 |
| 6 | `DeleteFileQuietly` 复位只读的语义变化 | **PASS-with-caveat（P2）** | `SaveSystem.cs:135-154`：仅对调用方传入路径（全部由已校验 saveName 派生，无通配/目录遍历）复位 `ReadOnly` 后删除；影响面：`DeleteSave` 现在能删掉用户手动标只读的存档（旧实现会抛异常），`SaveGame` 能清只读陈旧 `.tmp`（本轮测试依赖）。无"误删无关文件"路径；但"静默清 ReadOnly 再删"属行为契约变化，建议记录为设计决策 |
| 7 | `RestoreSaveFromBackup` 先删后 rename 窗口 | **PASS-with-caveat（P2）** | `SaveSystem.cs:173-195`：先 `Copy→.restore.tmp`，再 `DeleteFileQuietly(main)`、`File.Exists` 兜底检查、`File.Move`；`.bak` 始终保留；finally 清 `.restore.tmp`。崩溃窗口 = 主档缺失 + `.bak` + `.restore.tmp`，下次 LoadGame 走"主档缺失→bak"路径自愈，且 `File.Copy(...true)` 覆盖陈旧 restore.tmp。删除失败时抛异常→catch 保留损坏主档与 bak（不丢数据）。未发现永久丢档路径 |
| 8 | 告警文案精确契约（`LogAssert.Expect`） | **PASS-with-caveat（P2）** | `SaveSystemAtomicWriteTests.cs:373,381-382` 用**精确字符串**匹配两条 Warning（含完整路径）；路径由 `SaveSystem.GetSaveDirectory()` 组合，环境无关；仅对文案措辞敏感，属"可接受的判别性测试代价"，建议后续抽 `const string` 共享或改 `LogAssert.Expect(Regex)` |
| 9 | CharacterSaveData 旧档 ragged/缺失数组安全 | **PASS** | `CharacterSaveData.cs:419-527`：每类并行数组用 `MinArrayLength`（任一段 null→0，`:518-527`），history 用 `relationHistoryCounts` + `relationHistoryEntries` 下标双守卫（`:441-453`），trait/disease/impairment 空 id 跳过；`:411` bodyParts 缺失重建；测试 `CharacterPersistenceTests.cs:255-300`（全 null 数组不抛 + `skillIds` 2/`skillValues` 1 只还原共同前缀，验证不越界）、`:216-253`（v11-13 旧 JSON 全默认）、`:302-311`（未知 trait 跳过并警告） |
| 10 | relations 拍平是否丢字段/孤儿 | **PASS-with-caveat（P2）** | `CharacterRelation` 全部 6 个字段（`otherCharacterId/opinion/type/history/trust/fear/romanticAttraction`）均入档并在 `:457-466` 还原，history 顺序按段拼接一致；无字段丢失。孤儿风险：还原不校验目标角色是否存在，但消费侧用 `TryGetValue`（`CharacterManager.cs:569`、`CharacterData.cs:670`）→ 不会 NRE；仅在极端"目标角色未入档"时留下悬空关系（P2，建议 restore 后按角色表过滤） |
| 11 | 新增测试是否真判别 / 互相污染 | **PASS** | 2 条上轮"非判别"测试已改为判别性（只读 `.tmp` 依赖显式清理；损坏 bak 用 Warning 契约）；`LoadGame_CorruptMainFile_RecoversFromValidBackup` 新增且敏感；Character 6 条覆盖空/旧/ragged/未知 id/往返；各 fixture TearDown 清理注册表与存档文件，复核时 `Saves` 目录为空（无残留），未见跨测试污染 |
| 12 | 范围检查（只改授权文件） | **PASS** | 19:30 后仅 9 个文件：`SaveSystem.cs`、`EconomyManager.cs`、`CharacterSaveData.cs` + 4 个测试 + 2 个新 `.meta`；其余脚本/场景/asset 零改动；无调试残留；`TradeCenter.cs` 本轮未动 |
| 13 | 全量数与 XML 闭合 / 无既有测试被改弱 | **PASS** | 343 = 331 + 12；逐类计数与 XML 一致（10/12/6/3/1）；无 Failed/Inconclusive/Skipped。既有测试本轮仅 2 个文件被改：`SaveSystemAtomicWriteTests`（2 条改判别性 + 1 新增，均增强）与 `EconomyConservationTests`（2 条口径 + 2 新增，见 #5）；其余既有 331 条未被触碰 |
| 14 | 首跑 31/32 的失败原因与修复 | **PASS（已排除回归）** | `team-round3.xml` 失败消息为 `Unhandled log message: '[Error] [SaveSystem] 读档失败…'`（严格 LogAssert 下预期 Error 未声明），非产品缺陷；`LegacySaveCompatibilityTests.cs:88-96` 现显式 `ignoreFailingMessages = true` 且仍断言 `DoesNotThrow`+`IsNull`。⚠️ 该文件为未跟踪文件，无法用 git 证明第二次改动仅此一处（内容与失败消息一致，判定合理） |
| 15 | 角色运行态入档是否有存档版本/前向兼容问题 | **PASS-with-caveat（P2）** | `SaveVersion` 仍 14，新数组字段以"可选段"方式追加（旧档反序列化为 null→默认），向后兼容 ✅；未升版本意味着**新档给旧客户端**会静默丢运行态（前向兼容非本作要求，记录 P2） |

---

## 2. 问题清单（本轮无 P0/P1）

| 级别 | 问题 | 位置/建议 |
|---|---|---|
| P2-1 | `DeleteFileQuietly` 会静默清 `ReadOnly` 再删：`DeleteSave` 由"抛异常"变为"删掉用户的只读存档" | `SaveSystem.cs:143-148`；建议在注释/UX 明确，或对主档删除保留"只读则拒绝"的选项 |
| P2-2 | `LogAssert.Expect` 精确文案契约：两条 Warning 一旦改措辞即测试红 | `SaveSystemAtomicWriteTests.cs:373,381`；建议共享常量或正则 |
| P2-3 | 关系还原不校验目标角色存在 → 极端旧档可能留下悬空关系（消费侧 `TryGetValue` 已安全） | `CharacterSaveData.cs:439-467`；建议 restore 后按角色表过滤 |
| P2-4 | `CaptureRuntimeState` 的 impairments 循环未判空元素（relations 是 struct 无此问题；diseases 已判空） | `CharacterSaveData.cs:313-332`；`impairment == null` 时 NRE（内部列表一般无 null，风险低） |
| P2-5 | 发车预检阈值 10%/1 是启发式：目的地余量 9、计划 100 时整单跳过（本可部分入库），可能略保守 | `EconomyManager.cs:190-191`；可按 `min(计划量, 余量)` 部分发车替代跳过 |
| P2-6 | 角色运行态每角色 8+ 数组，长局存档体积/序列化耗时增长未评估 | `CharacterSaveData.cs:104-140`；建议后续做一次 1k 角色档的体积/耗时基准 |
| P2-7 | 首跑失败后修改测试（而非产品）达到 32/32；未跟踪文件无法审计该次修改的完整性 | 已在 #14 说明；建议后续测试变更也记录 diff 摘要 |

---

## 3. 上轮 P2 关闭确认

| 上轮 P2 | 现状 |
|---|---|
| P2-1 只处理 `.sav` 缺失、不处理损坏主档 | **已解决**（`TryParseSaveData` + 双路径回退 + 恢复后主档=备份字节） |
| P2-2 `.restore.tmp` 崩溃残留不清理 | **已解决**（SaveGame 写前清理 + DeleteSave 清理 + 判别性测试） |
| P2-3 两条非判别测试 | **已解决**（只读 `.tmp` / Warning 契约） |
| P2-4 满仓仍会被反复发车 | **已解决**（发车前容量预检 + 减量不发车不占名额） |
| P2-5 缺粮双罚（本系统扣人口） | **已解决**（口径 a：本系统只轻罚稳定度/秩序，人口归 Famine 事件） |

---

## 4. Merge gate 结论

| 卡 | 结论 | 说明 |
|---|---|---|
| **task-14（save：损坏主档回退/restore.tmp/只读清理）** | **可合入** | 上轮 P2 全关；10 条测试含 2 条新判别性用例；全量 343/343。残余 P2-1/P2-2 不阻塞 |
| **task-15（econ：满仓预检/缺粮口径 a）** | **可合入** | 预检与告警节流实现正确、测试敏感；人口口径变更经代码+测试双向确认，属增强。残余 P2-5 不阻塞 |
| **task-16（character：5 项运行态拍平入档）** | **可合入** | 旧档/ragged/null 全安全（无越界、未知 id 跳过），6 条测试覆盖到位；残余 P2-3/P2-4/P2-6 建议后续迭代 |
| **Lead LegacySaveCompatibilityTests（3 条）** | **可合入** | v11 旧档/版本不一致/非法 JSON 覆盖有效；首跑失败为预期日志未声明，已合理修正 |

**总体：本轮无 P0/P1；三张实现卡与 Lead 测试均可合入。**

---

## 5. 待验证

1. 未运行 Unity：P2-5（预检阈值是否造成可感知的贸易量下降）与 P2-6（存档体积）需运行时数据；`RestoreSaveFromBackup` 崩溃窗口为静态推演（未做故障注入）。
2. `LegacySaveCompatibilityTests.cs` 首跑→复跑之间除 `ignoreFailingMessages` 外是否有其它改动无法用 git 证明（未跟踪文件）；当前内容已逐行通读，未发现放宽断言。
3. PlayMode gfx 测试本轮未改动（19:03），本轮 gfx PASS 复用上轮图形断言；`D:/map_verify.png` 同刻采样日志显示 unique 13/亮度 0.145~0.730（非单一底色）。
4. 角色关系"孤儿"在真实长局中的出现概率未测（依赖角色是否可能从管理器移除而不入档）。

## 附录：关键只读证据

```bash
grep -o 'testcasecount|total|passed|failed|skipped|start-time|end-time' Logs/editmode-results.xml  # 343/343/0/0
grep -c 'test-case…classname="…SaveSystemAtomicWriteTests"' Logs/editmode-results.xml               # 10（Econ 12/Char 6/Legacy 3/RoundTrip 1）
grep -o 'total|passed|failed|start-time' Logs/team-round3b.xml                                    # 32/32
grep -o 'total|passed|failed|start-time' Logs/playmode-gfx-results.xml                            # 1/1
grep -c "error CS" Logs/editmode-team3.log                                                        # 0
sed -n '130,300p' SaveSystem.cs                                                                    # TryParse/回退/ReadOnly/DeleteSave
sed -n '45,70p;160,236p;420,460p' EconomyManager.cs                                                 # 阈值/预检/节流/口径 a
sed -n '100,140p;225,340p;415,529p' CharacterSaveData.cs                                           # 数组定义/捕获/还原/兼容
grep "GarbageJson" Logs/team-round3.xml                                                            # 首跑失败=预期 Error 未声明
ls "…/AppData/LocalLow/.../Saves"                                                                  # 测试后为空（无残留）
find Assets -newermt "2026-10-31 19:30" -type f                                                    # 范围=9 个授权文件
```
