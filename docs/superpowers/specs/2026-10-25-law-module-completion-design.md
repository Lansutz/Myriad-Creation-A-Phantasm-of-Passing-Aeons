# 法律模块收尾与接线规格（阶段一收尾）

## 1. 定位

本规格不重复 `docs/architecture/ARCHITECTURE_2_0_LAW_OFFICE_NAME_REGION_MODEL.md`（下称设计稿）的设计论证，只界定**本轮要做什么、不做什么、按什么口径验收**。

设计稿是研究设计稿，覆盖面一直到阶段二、阶段三（头衔、继承三轨、文化/认同、姓名、键名、法理区域）。本轮只处理阶段一（设计稿 §9 的 #2–#5）以及它落地后暴露出的半迁移破损。

现状基线（2026-10-25 工作树，见实施计划第 1 节）与设计稿 §3 的"现状审计"已经不一致：三层枚举、29 组、15 条罪名、传统/文化新字段都已写进代码（未提交），但改名只做了一半，工作树目前不可编译；条文仍挂域不挂组；组级 API、组级效果、缓存指纹、司法审判都没有接线。所以本轮的性质是**收尾与接线**，不是从头实现。

## 2. 术语与口径

- 法律域 `LawDomain`、法律族 `LawFamily`、法律组 `LawGroup` 是**代码内部归类名**，只用于代码、存档键与开发文档；不进入玩家可见文本，不进游戏内百科。
- 归集方向固定为**从下到上**：组（一道题一组）→ 族（同主题归集）→ 域（对外粗粒度引用）。条文条目挂**组**；秘密、罪名、派系、资格等跨系统引用**域**。
- 题内子选项（父选项激活的链式选项）不独立成组。
- 硬约束（资格、条件）与权重分离：不通过硬约束直接排除，权重只在通过硬约束的候选内排序。
- 枚举值按 `int` 序列化（JsonUtility），**成员顺序即存档语义**；新增成员只允许追加，不允许重排或复用已删值。
- 跨程序集依赖方向：`Definitions` 不得反向依赖 `Systems`（罪名定义里引用域用 `int` 存即是为此）；域/族/组定义继续留在 `Systems/LawConstraints`，新增枚举若需被定义层引用，走已有先例（int 存值 + 访问器），不新开反向依赖。

## 3. 现状基线摘要

| 项 | 状态 | 证据 |
|---|---|---|
| 五域 / 十族 / 29 组 | 已写入代码（未提交），与设计稿 4.2 一致 | `Systems/LawConstraints.cs:59-125` |
| 组→族→域映射 | 已实现，`GetDomain` 不是硬编码 | `LawConstraints.cs:177-223` |
| `Government*`→`Law*` 改名 | **未收尾**，四程序集均有引用已删类型/字段，静态判定不可编译 | `State/RealmData.cs:104`、`WorldState/GameWorld.Simulation.cs:164`、`GameWorld.Succession.cs:225`、`Assets/UI/Panels/GovernmentPanel.cs`、`Assets/Editor/ComponentChoiceDrawer.cs:39-45`、5 个测试文件 |
| 条文挂载 | 仍挂 `LawDomain`，设计要求挂 `LawGroup` | `State/Law.cs:19` |
| 组级 API 消费 | `GetDomainName/GetFamilyName/GetGroupName`、`GetCitedDomains` 等零调用者 | 审计 grep |
| 组级效果 | `LawEffects` 只覆盖九维 + 法源，13 个占位组无映射 | `State/LawEffects.cs:89-266` |
| 缓存指纹 | 只拼 7 个 `ComponentChoice.primary`，漏 secondary 与多数组；`LawSystem` 从未实例化 | `Systems/LawSystem.cs:30-35,99-110` |
| 罪行管理器 | 15 条定义齐全，带 behaviors/targetClasses/contexts/域族组引用；但 `new Law` 全仓 0 处，引用关系空转，`MatchByBehavior` 零调用 | `Systems/CrimeRegistry.cs:25-258`、`State/Law.cs:22` |
| 司法执行层 | `JudicialSystem` 并入 `LawSystem.cs`（类在 :174），从未实例化、未入档 | `Systems/LawSystem.cs:174-274` |
| `LawCategory` | 已 `[Obsolete]`，零代码引用，只剩文件 | `Definitions/LawCategory.cs:28-38` |
| 头衔 / 资格 / 继承 / 文化 | 骨架或半成品，**本轮不改实现**，只登记阶段二接口 | 审计 §1–§6 |

## 4. 需求

| ID | 需求 | 优先级 |
|---|---|---|
| REQ-001 | 工作树恢复可编译：Simulation / UI / Editor / Tests 四个程序集不再引用已删除的 `Government*` 类型或 `realm.composition` 字段 | P0 |
| REQ-002 | 条文条目挂法律组；域不再由条文直接存字段，改为按组派生查询 | P0 |
| REQ-003 | 组→族→域映射有唯一真源和完整性断言：29 组全部有族、有域，无未归集组、无非法双归属 | P0 |
| REQ-004 | 组级效果覆盖全部组：九维与法源按现有计算，13 个预留组返回中性值且不参与效果叠加 | P1 |
| REQ-005 | 法律变更有唯一入口（改革/变更统一走同一入口），效果缓存指纹覆盖全部组字段并按 `realmId` 失效；`LawSystem` 在运行时真正被实例化 | P1 |
| REQ-006 | 罪名可被法律引用并被审判消费：条文实例化时填充引用，司法受理依据引用关系，而非"全部罪名一律受理" | P1 |
| REQ-007 | `JudicialSystem` 明确归执行层、确定归属方（思考/政权）、确定是否入档，并接上审判入口 | P1 |
| REQ-008 | 法律 UI 只显示可见名：内部"域/族/组"字样不出现在界面；旧 `GovernmentPanel` 要么改写为组视图，要么删除 | P1 |
| REQ-009 | 存档兼容：`RealmDTO.laws` 字段改名有显式映射，`SaveVersion` 按约定递增，旧档载入后法律组可用；枚举值顺序不变 | P0 |
| REQ-010 | 清理陈旧物：删 `LawCategory`，清掉"七维/旧五域/8 罪名"等漂移注释与未接线死代码 | P2 |
| REQ-011 | 设计稿与代码现状对齐：设计稿 §3 现状表、§9 阶段表、附录按本轮结果校正 | P2 |
| REQ-012 | 阶段二接口闸门：头衔 / 资格 / 继承三轨 / 文化认同 / 姓名 / 键名 / 法理区域的最小契约与依赖顺序登记成清单，本轮不实现 | P2 |

## 5. 不在范围

- 不实现预留组的具体玩法档位（兵役制、户籍、税法、边市、国教链式前置等），只保留枚举位与中性效果。
- 不实现头衔管理器接线、称谓 first_valid 链、`OfficialOffice` 退场（设计稿 #6）。
- 不实现继承三轨落地（#7）、文化/族群认同层（#12）、姓名双轨（#13）、键名命名空间（#8）、法理区域（#9）。
- 不改数值平衡、不改地图/聚落模型、不重写 Unity 场景与 Prefab。
- 不新增外部依赖包。

## 6. 待决策（阻塞对应步骤）

| ID | 决策 | 阻塞 |
|---|---|---|
| D1 | `GovernmentPanel` 是改写为"法律组视图"还是直接删除并等待新面板？ | REQ-001 / REQ-008 |
| D2 | `JudicialSystem` 归属：`ThoughtManager`（现状）还是 `RealmData`？是否随政权入档？ | REQ-007 |
| D3 | `Law` 条文数据来源：按模板实例化，还是走内容包 JSON？ | REQ-006 / REQ-009 |
| D4 | 13 个预留组字段是否保留在 `LawComposition` 并继续进档？ | REQ-004 / REQ-009 |
| D5 | 旧档迁移策略：`[FormerlySerializedAs]` 兼容旧 `composition`，还是硬版本断代？ | REQ-009 |
| D6 | 法律组/档位的玩家可见名与 locale 键命名规范（本轮只定规范，不写全文案） | REQ-008 |

## 7. 验收定义

1. 四个程序集在 Unity Editor 中编译通过，无 `Government*` / `composition` 残留引用（grep 归零）。
2. `Law` 条文挂组；`LawGroup → LawFamily → LawDomain` 查询对 29 组全覆盖，EditMode 测试断言完整。
3. 改组后效果在下一个 Tick 刷新一次且只刷新一次（指纹生效）；`LawSystem` 有实例化点。
4. 一桩罪名事件 → 命中罪名定义 → 依据当前条文的引用关系决定是否受理，无引用时行为有明确默认口径并被测试覆盖。
5. 法律界面不出现"域/族/组/Internal/Institutional"等内部词；旧 `Government` 字样归零。
6. 旧存档（含 `composition` 字段）载入后法律组可用，且枚举不因顺序变化而错位；`SaveVersion` 已递增。
7. 工作树无新增未接线死代码；设计稿 §3/§9 与代码一致。

## 8. 依赖与风险

- **存档风险**：枚举 `int` 序列化 + `RealmDTO` 字段改名，若不加旧名映射会静默回默认值（现有 `HabitationSiteSaveData` 的 `FormerlySerializedAs`/`EnsureCanonicalFields` 是先例，可照做）。
- **范围风险**：13 个预留组字段已经进档；删字段或重排会静默丢数据，必须走 D4 决策。
- **验证风险**：本机 `dotnet` SDK 缺 `Microsoft.NET.SDK.WorkloadAutoImportPropsLocator` 等 Sdk 目录（MSB4276），不能替代 Unity 编译；批处理 Unity 此前因 license 返回 198，编译验证依赖 Editor 或可用的批处理 license。
- **并发风险**：阶段二骨架（`TitleDefinition`/`TitleManager`/`CultureTransition` 等）已在工作树但未接线；本轮改动集中在线性依赖强的法律链路上，避免与骨架文件交叉。
