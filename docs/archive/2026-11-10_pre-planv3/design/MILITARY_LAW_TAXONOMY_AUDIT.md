# 军事法体系审计：域/族/组/项

> 2026-11-04。目的：按现有 `LawDomain → LawFamily → LawGroup → 项` 分层，核对军事法分组、命名和模拟必要性。
> 只读资料 + 现有代码；不改宗教/思想域。

---

## 1. 现有代码

- `LawConstraints.LawDomain`：`Institutional / Military / Faith / Governance / LegalTradition`
- `LawConstraints.LawFamily`：`SupremePower / CentralPower / LocalPower / RealmStructure / Military / FaithGovernance / Household / Economy / Frontier / LegalSource`
- `LawConstraints.LawGroup`：30 组（本轮新增 `MilitaryManagementGroup`）
- `LawComposition`：`militaryRecruitment`、`militaryOrganization`、`militaryManagement` 三个 `ComponentChoice`
- `LawContentLoader` / `Laws.json`：法律组、法律条目、`requiredInnovationIds`

当前军事域/军政族下代码已有：

| 组 | 代码 | 名称 |
|---|---|---|
| 兵役 | `MilitaryRecruitmentGroup` | 兵役 |
| 编制 | `MilitaryOrganizationGroup` | 军队组织 |
| 管理 | `MilitaryManagementGroup` | 军事管理 |

领域/族映射正确：三组都是 `LawFamily.Military` → `LawDomain.Military`。

---

## 2. 资料结论

### 2.1 军事法不是“一个法”

从罗马军法、军政、后勤、边防资料看，至少可分：

1. **兵源/兵役**：谁当兵、怎么征、服役多久、替代/豁免；
2. **编制/组织**：常备军、民兵、国家军、城市军、领主军、边军；
3. **指挥/统辖**：中央统一调遣、战区自主、边将临机专断；
4. **军法/军纪**：军事法庭、逃兵、抗命、劫掠、军民冲突；
5. **军需/后勤**：就地征发、兵站、承包商、运输；
6. **边防/边境**：关隘通行、边市互市、军镇驻防（与军事管理配合，但归境内治理/边域族）。

参考：

- [Roman Military Oath: Sacramentum Militiae](https://scholar.google.fi/citations?view_op=view_citation&hl=en&user=k4SZKYEAAAAJ&citation_for_view=k4SZKYEAAAAJ:roLk4NBRz8UC)
- [The Meaning of the Terms Limes and Limitanei](https://core.ac.uk/download/143833444.pdf)
- [Roman military law / disciplina](https://publicaciones.defensa.gob.es/media/downloadable/files/links/R/E/REVISTAS_PDF592.pdf)
- [War Logistics](https://brill.com/display/book/9789004255678/B9789004255678_005.xml?print=%24B)
- [Ottoman menzil logistics](https://dergipark.org.tr/tr/download/issue-file/88179)
- [明清长城关口的民族贸易](https://changcheng.ctnews.com.cn/2024-12/24/content_168683.html)
- [US Army FM 1-04 command policy](https://irp.fas.org/doddir/army/fm1-04.pdf)

> 注：以上为检索到的资料入口；具体条目只作分类参考，不复制其文本/数据。

### 2.2 当前命名的问题

| 当前 | 问题 | 建议 |
|---|---|---|
| 兵役组里放“常备军” | 常备军是**编制/组织**，不是兵源 | 常备军移到编制组；兵役组用征兵/募兵/封建征召/雇佣/部落征召 |
| “军事管理”混合军纪/军需/军法/指挥 | 一个组只能回答一道题 | 拆成：指挥统辖组、军法军纪组、军需后勤组 |
| `border` 一个组合并关隘/边市/驻军 | 域/族/组不严 | 关隘 → `PassageGroup`；边市 → `BorderMarketGroup`；驻防 → 军事编制/边防组 |
| “战区自主” | 可理解，但需和“边将便宜行事”区分 | 指挥统辖组选项：中央统一调遣 / 战区自主 / 边将临机专断 |
| “军政族” | 内部名可保留；对外不显示 | 开发文档用“军政族”，玩家文本用中性可见名 |

---

## 3. 建议的严格分层

```text
域 LawDomain = Military
  └─ 族 LawFamily = Military
       ├─ 组 MilitaryRecruitmentGroup（兵源/兵役）
       │    项：征兵 / 募兵 / 封建征召 / 雇佣 / 部落征召
       ├─ 组 MilitaryOrganizationGroup（编制/组织）
       │    项：常备军 / 民兵 / 国家军 / 城市军 / 领主军 / 边军
       ├─ 组 MilitaryManagementGroup（指挥统辖/自主性）
       │    项：中央统一调遣 / 战区自主 / 边将临机专断
       ├─ 组 MilitaryJusticeGroup（军法/军纪）
       │    项：军纪整肃 / 军事法庭 / 军民分治
       └─ 组 MilitaryLogisticsGroup（军需/后勤）
            项：就地征发 / 兵站补给 / 承包商供应

域 LawDomain = Governance
  └─ 族 LawFamily = Frontier
       ├─ 组 PassageGroup（关隘通行）
       │    项：开放 / 管制 / 封锁
       └─ 组 BorderMarketGroup（边市互市）
            项：开放互市 / 限制 / 禁市
```

说明：

- **边防驻军**可归 `MilitaryOrganizationGroup`（边军）或新增 `FrontierDefenseGroup`；但**关隘/边市**必须留在 `Governance/Frontier`，不要塞进军政族。
- `MilitaryJusticeGroup`、`MilitaryLogisticsGroup` 是否落地，取决于模拟是否需要“逃兵/军纪/补给损耗”；不需要就只做数据/风味，不硬加系统。

---

## 4. 模拟必要性

| 项 | 模拟作用 | 必要性 |
|---|---|---|
| 兵源（征兵/募兵/封建/雇佣） | 人力池、征召成本、兵质、忠诚/契约 | **高** |
| 编制（常备/民兵/国家/城市/领主/边军） | 单位上限、维护费、组成、驻防 | **高** |
| 指挥统辖（中央/战区/边将） | 命令延迟、临机权、忠诚、边防风险 | **高**（有战役/远征时） |
| 军法/军纪 | 逃兵、劫掠、军民冲突、腐败 | 中 |
| 军需/后勤 | 补给、损耗、行军距离 | **高**（有远征/补给时） |
| 关隘通行/边市 | 贸易、税收、走私、入侵通道 | **高**（有边境经济时） |
| 军阶/礼仪/军服 | 无直接模拟 | 低，可不做 |

原则：

- **能改变人力/补给/忠诚/贸易/安全的**才进模拟；
- 只影响文本的军阶、礼仪、军服，不进法律组；
- 不要为“齐全”加没有模拟效果的法律。

---

## 5. 当前数据要改的地方

1. `military` 组：把 `standing_army` 移到 `military_organization`；`military` 只留兵源选项。
2. `military_admin`：拆成 `military_management`（指挥/自主）、`military_justice`（军法/军纪）、`military_logistics`（军需）。
3. `border`：拆成 `passage`、`border_market`；驻防相关移到军事编制/边防组。
4. 法律组增加 `groupKind`（映射 `LawConstraints.LawGroup`）或建立显式映射表，确保 JSON 组能落到域/族/组。
5. 更新 `LawCatalogReference`、`LawContentJsonTests` 的组数/法数。
6. 革新引用同步：兵役/编制/管理/军法/军需/关隘/边市各自挂对应革新。

---

## 6. 结论

- 现有“军事域 → 军政族”方向对；
- **三个组不够**：至少兵源、编制、指挥统辖；军法/军需视模拟需要；
- **常备军不是兵源**，应归编制；
- **军事管理不能混军法/军需/指挥**；
- **边境法归境内治理/边域族**，与军事管理配合，不混为一组；
- 先补“兵源/编制/指挥统辖”，军法/军需/关隘/边市按模拟需要逐步落地。


---

# 追加修订：维度分离、边境管制、兜底

## 7. 不能把不同维度塞进“编制”

| 维度 | 回答的问题 | 不能混入 |
|---|---|---|
| 政体形式 | 封建制 / 中央集权 / 城邦 / 部落？ | 不是“编制”选项；是其他法律的**条件** |
| 兵源/兵役 | 兵从哪里来：征兵、募兵、封建征召、雇佣、部落征召 | 不是“常备军/国家军” |
| 军制/编成 | 军队怎么构成：常备军、民兵、领主军、国家军、城市军 | 不是“兵从哪里来”，也≠边军 |
| 指挥统辖 | 谁调遣、前线自主权、中央/战区/边将 | 不是“编制” |
| 边防管制 | 通行/巡逻/有限管制/严格管制 | **不等于关隘** |
| 军法/军需 | 军纪、军事法庭、补给、征发 | 不是“指挥” |

### 7.1 重合与去重

- `feudal_host`（兵源：封建征召） vs `lords_host`（军制：领主军）**不是同一维**；
- `militia`（民兵）与 `city_army`（城市军）在“兵源/编成”上有重合，必须选一个主维度，另一个只作变体/条件；
- `standing_army`（常备军）是军制，不是兵源；`national_army` 是军制，不是“国家征兵”；
- `conscription`（征兵）是兵源；不要把它塞进军制；
- 封建制下不能直接出现“国家军/中央统一调遣”作为普适选项；这些选项必须由 `requiredPolityForm` 或前置法律门控。

### 7.2 政体形式是条件，不是选项

建议在 `LawRequirement` 加：

```csharp
public string requiredPolityForm; // feudal / centralized / city_state / tribal
```

或复用现有政体判定。法律选项只有在政体形式匹配时可见/可选；否则隐藏或置灰。

---

## 8. 边境管制：不是“关隘”

前现代/中世纪边境的实际情况：

- 领主之间的土地管理很松散；
- 有些地方**没有现代意义上的通行权概念**；
- 有些只是**巡逻**；
- 有些是**有限管制**（登记、检查、特定通道）；
- 有些是**严格管制**（许可、封禁、驻军）。

所以边境组应叫 **边境管制组**，选项按强度：

```text
无管制 / 习惯通行
巡逻
有限管制（登记/检查/指定通道）
严格管制（许可/封禁）
```

“关隘”只是**有限/严格管制**的一种物理设施，不是整个组；`PassageGroup` 这个名字太窄，应改为 `BorderControlGroup`，`PassageGroup` 可保留为“关隘设施/通道”子项。

边市互市仍是独立组：`BorderMarketGroup`（开放/限制/禁市），与管制强度配合，但不合并。

---

## 9. 兜底条款

每个法律组应有**兜底项**：

- 兵源组：`习惯征召`（无成文兵役法时的默认）；
- 军制组：`无常备军`（只有临时征召）；
- 指挥组：`默认统辖`（中央/领主按习惯）；
- 边境管制组：`无管制/习惯通行`；
- 边市组：`无固定边市`；
- 军法组：`习惯军纪`；
- 军需组：`就地征发`。

实现建议：

```csharp
public class LawDef { public bool isFallback; }
public class LawGroupDef { public string fallbackLawId; }
```

- 组内无主动选择时，使用 fallback；
- fallback 可以被基础革新解锁，或开局已知；
- 非 fallback 选项必须有 `requiredInnovationIds`。

---

## 10. 当前数据审计

当前 `Laws.json`：50 条法律，其中 **31 条没有 `requiredInnovationIds`**。

包括：

- 赋税 3 条；
- 民兵；
- 特权 2 条；
- 继承/范围/中央交接/地方交接/地方职能/结构等 25 条。

下一步：

1. 每条法律补 `requiredInnovationIds`，或标为 fallback；
2. 每组建 `fallbackLawId`；
3. 加 `requiredPolityForm` 条件；
4. 拆 `military_admin` 为指挥/军法/军需；
5. 拆 `border` 为边境管制/边市；
6. 把 `standing_army` 从兵源组移到军制组；
7. 去重 `feudal_host/lords_host`、`militia/city_army`。

---

## 11. 学术参考

- [Frontiers in Question: Introduction](https://blogs.fu-berlin.de/frontiers/files/2011/04/Power_FrontiersinQuestion.pdf)
- [The guidaticum safe-conduct in medieval Arago-Catalonia](https://brill.com/previewpdf/journals/me/1/1/article-p51_6.xml)
- [Military Mobilisation in King John's Reign](https://www.tandfonline.com/doi/full/10.1080/03044181.2026.2661628)
- [England's Northern Frontier: Conflict and Local Society in the Fifteenth-Century Scottish Marches](https://www.cambridge.org/lk/universitypress/subjects/history/british-history-1066-1450/englands-northern-frontier-conflict-and-local-society-fifteenth-century-scottish-marches?format=PB&isbn=9781108460859)
- [Schildgeld und Heersteuer](https://www.lhlt.mpg.de/1995375/)
- [Notes on Military Law](https://books.google.ci/books?hl=fr&id=GYVkwFZPA6IC&output=html_text&pg=PR16)
