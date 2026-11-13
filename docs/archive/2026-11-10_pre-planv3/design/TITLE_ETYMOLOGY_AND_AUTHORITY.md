# 头衔词源与权威类型：为什么位阶不能是一个数值轴

> 2026-11-08。方法：查词源资料 + 对照 `TITLE_APPELLATION_TYPOLOGY.md` 的跨文明例证。
> 目的：为「位阶 = 权威 × 绑定 × 称谓组合」提供词源学依据；说明同一译名（王/公爵/君主）可能来自完全不同的制度。

## 0. 结论

1. **头衔的"高低"不是同一个东西的不同刻度**，而是不同制度（亲属、军事、侍从、宗教、官僚、选举）的不同产物。
2. 把 `knyaz`、`dux`、`comes`、`emir`、`sultan`、`rex` 都译成"王/公爵/君主"再排一个 rank，是把**制度差异**压成**数值差**——正是本项目要避免的抽象数值。
3. 因此数据模型里 **authority（权威来源）是一等字段**；`rank` 浮点应删除；位阶用偏序 + 礼制/臣属/主权关系裁决。

## 1. 词源例证

| 头衔 | 语言/来源 | 词源 | 字面义 | 权威类型 |
|---|---|---|---|---|
| knyaz（罗斯） | 原始斯拉夫 `*kъnędzь` ← 原始日耳曼 `*kuningaz` | `*kuningaz` ← `*kunją`（亲族、家族）+ `*-ingaz`（属于…的） | **宗族之长 / 宗长** | 亲属·宗族 |
| velikiy knyaz（罗斯大公） | `velikiy`（伟大的、至高的）+ `knyaz` | 同上 | **至高宗长 / 大宗长** | 亲属·宗族（更高一级的宗族权威） |
| dux（拉丁） | `dux, ducis` ← `ducere`（引导） | 领队者 | **军事领袖 / 引导者** | 军事 |
| comes（拉丁） | `comes, comitis` | 同伴、随从 | **侍从 / 同伴** | 宫廷·侍从 |
| earl / jarl | 古英语 `eorl`；古诺斯 `jarl` | 勇士、首领（词源有争议） | ** warrior / 酋首** | 军事·贵族 |
| rex（拉丁） | `rex` ← `regere`（治理、校正） | 治理者 | **治者 / 正者** | 统治·司法 |
| basileus（希腊） | 迈锡尼 `qa-si-re-u`（地方首领） | 前希腊语来源，存疑 | 王/首领 | 王权（来源存疑） |
| shah（波斯） | 古波斯 `xšāyaθiya` | 统治者、有权者 | 王 | 统治 |
| khan / qaghan | 突厥-蒙古语 | 首领 / 至高首领 | 汗 / 大汗 | 草原统治 |
| emir（阿拉伯） | `amīr` ← `amara`（命令） | 指挥官 | **司令 / 指挥官** | 军事指挥 |
| sultan（阿拉伯） | `sulṭān` | 权力、权威（抽象名词） | **权威者** | 抽象权威 |
| malik（阿拉伯） | 塞米语根 `m-l-k`（统治、拥有） | 王 | 王/所有者 | 统治 |
| tsar（俄国） | 拉丁 `Caesar`（人名→头衔） | 恺撒 | 恺撒（借名立号） | 帝国继承/宣称 |
| imperator（罗马） | `imperare`（命令） | 统帅 | **统帅** | 军事欢呼 |
| augustus（罗马） | `augustus`（神圣的、受尊崇的） | 受尊崇者 | **尊崇者 / 神圣者** | 宗教·卡里斯玛 |
| princeps（罗马） | `princeps`（第一） | 第一人 | **第一公民** | 元首·首席 |
| 王（中国） | 甲骨/金文 | 斧钺象形（一说） | 王 | 统治 |
| 皇帝（中国） | 皇（煌、大、君主）+ 帝（天帝、神性） | 复合词 | **神圣最高统治者** | 宗教·宇宙论 |
| 公/侯/伯/子/男 | 周代爵制 | 亲属/封建授予 | 爵位 | 亲属·封建 |
| 宗长 / 族长 | 汉语 | 宗族之长 | 宗族首领 | 亲属·宗族 |

> 中文头衔（王/公/侯/伯/子/男/后）另见 `CHINESE_TITLE_ETYMOLOGY.md`：本义与权力来源各不相同，`后` 发生"君主→后妃"的来源转移。

引用：[Knyaz（Wikipedia 存档）](https://webarchiveweb.wayback.bac-lac.canada.ca/web/20051215000000/http://en.wikipedia.org/wiki/Knyaz)、[Grand prince（存档）](https://eresources.nlb.gov.sg/webarchives/wayback/20131114024712/https://en.wikipedia.org/wiki/Grand_prince)、[etymonline: knyaz](https://www.etymonline.com/search?q=knyaz)、[etymonline: kuningas](https://www.etymonline.com/search?q=kuningas)、[Šāh（Encyclopaedia of Islam）](https://referenceworks.brill.com/display/entries/EI1O/SIM-5266.xml)。

## 2. 对模型的含义

1. **"王 / 公爵 / 君主"是翻译层**，不是制度层：
   - 罗斯的 `knyaz` 直译是"宗长"，把它译成"王公/公爵"再和西欧 duke 比 rank，就是拿译名当机制。
   - 同理 `comes` 字面是"同伴/侍从"，不是"伯爵"这个等级。
2. **权威来源决定制度**（也就决定默认继承）：
   - 亲属/宗族（knyaz、宗长）→ 宗族推举/长老（tanistry、seniority）；
   - 军事（dux、emir、imperator）→ 军事推举/强者；
   - 侍从/宫廷（comes）→ 任命；
   - 宗教/神圣（augustus、皇帝）→ 加冕/宗教程序；
   - 抽象权威（sultan）→ 权力占有；
   - 帝国继承（tsar）→ 宣称/继承；
   - 元首（princeps）→ 首席/选举。
3. **数据落点**：
   - `authority`：`SemanticAuthority` 的 10 类（已有）；
   - `etymologyRoot` / `literalGloss`（可选文本）：记录词源与字面义，供词典/说明/翻译提示，不参与排序；
   - 称谓组合：词干 + 限定 + 关系式 + 复合 + 累积（已在 §11）；
   - `rank` 删除；`precedence` 由同域组合规则 + 跨域礼制/臣属/主权裁决。
4. **本地化/译名**：
   - 译名应标注制度来源（如 `knyaz` → 宗长/王公，`velikiy knyaz` → 大宗长/大公，`dux` → 军事领袖/公爵，`comes` → 侍从/伯爵）；
   - 避免"译名 → rank 数值"的隐式映射。

## 3. 与现有系统的衔接

- `SemanticAuthority` 已有 10 类，可直接作为 `authority` 字段；
- `TITLE_APPELLATION_TYPOLOGY.md` 已有跨文明组合类型（限定/关系式/复合/累积/敬称），本表是它的词源补充；
- `TitleAppellationContext` 需扩 `authority` 与 `register`；
- 继承：`authority` → 默认 `TitleSuccessionMode`/`ownLawPreset`（见 `TITLE_INHERITANCE_PLAN.md` §11.4）。

## 4. 待办

- [ ] `TitleDefinition` 删 `rank`，加 `authority`/`etymologyRoot`/`literalGloss`/`appellation` 组合；
- [ ] `CompositionRank` 规则表（同域严格序 + 跨域不可比）；
- [ ] 译名表按制度来源重标；
- [ ] 测试：`王/大王/王中之王/长老`、`knyaz/velikiy knyaz` 不可与西欧 rank 直接比较。
