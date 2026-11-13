# CK3 1.20.1 教义（Doctrine）系统端到端拆解

> 只读调查。游戏根目录：`E:/SteamLibrary/steamapps/common/Crusader Kings III/game`（下文路径均相对此目录）。
> 版本证据：`clausewitz_branch.txt:1` = `titus/release/1.20.1`。
> 本文所有"证据"均为 `路径:行`；行号以 1.20.1 原版文件为准。

## 0. 一句话模型（1.20 与旧版最大的差别）

1.20.1 里 **faith 不再直接持有"每个组选哪个教义"的映射**。三层装配变成：

```
religion_types:  doctrine = X        （扁平重复行，建局时批量套给属下所有 faith）
faith_types:     doctrines = { X Y } （扁平列表，faith 固有教义）
rite_types:      doctrines = { X Y } （扁平列表，覆盖同组）
```

faith 通过 `main_rite = <rite_key>` 指向一个 **礼（Rite）**，由 Rite 持有真正的"每组的选项"。旧版 `doctrines = { <group> = <doctrine> }` 写法在本版 **不存在**（见 §4.1）。
证据：`common/religion/faith_types/_faith_types.info:37-44`、`common/religion/rite_types/_rite_types.info:74-82`、`common/religion/religion_types/_religion_types.info:25-26`。

---

## 1. 三级结构：category → group → doctrine

### 1.1 分类（doctrine_category_types，共 6 个）

文件：`common/religion/doctrine_category_types/00_doctrine_category_types.txt:1-26`
info：`common/religion/doctrine_category_types/_doctrine_category_types.info:1-22`

| 分类 key | sort_order | 可见 | gui_tags | 组数 | 教义数 | 证据 |
|---|---|---|---|---|---|---|
| main_group | 6 | yes | `main` | 10 | 30 | 00_doctrine_category_types.txt:1-4 |
| special | 5 | yes | `special` | 14 | 17 | :18-21 |
| marriage | 4 | yes | — | 4 | 14 | :6-8 |
| crimes | 3 | yes | — | 6 | 22 | :10-12 |
| clergy | 2 | yes | — | 8 | 28 | :14-16 |
| not_creatable | 1 | **no** | — | 7 | 15 | :23-26 |

排序规则原文：`sort_order` "Higher value appears earlier"（`_doctrine_category_types.info:12-16`）。
`visible = no` 的分类不会出现在 GUI（`_doctrine_category_types.info:6-10`）；`not_creatable` 就是靠它隐藏的。

### 1.2 组（doctrine_group_types，共 49 个）

文件：`common/religion/doctrine_group_types/00_doctrine_group_types.txt:1-325`
info：`common/religion/doctrine_group_types/_doctrine_group_types.info:1-47`

组按分类分布（组数 = 该分类下组的个数；教义数 = 这些组里选项的总数）：

| 分类 | 组数 | 教义数 | 组名 |
|---|---|---|---|
| main_group | 10 | 30 | doctrine_gender, doctrine_pluralism, doctrine_theocracy, doctrine_head_of_faith, doctrine_pilgrimage, doctrine_theism, doctrine_muhammad_succession, doctrine_temple_authority, doctrine_zoroastrian_branches, special_tolerance |
| marriage | 4 | 14 | doctrine_marriage_type, doctrine_divorce, doctrine_bastardry, doctrine_consanguinity |
| crimes | 6 | 22 | doctrine_homosexuality, doctrine_deviancy, doctrine_adultery_men, doctrine_adultery_women, doctrine_kinslaying, doctrine_witchcraft |
| clergy | 8 | 28 | doctrine_clerical_function, doctrine_clerical_gender, doctrine_clerical_marriage, doctrine_clerical_succession, doctrine_funeral, doctrine_coronation, monasticism_group, sacraments_group |
| special | 14 | 17 | is_christian_faith, is_islamic_faith, is_jewish_faith, is_eastern_faith, is_sinitic_faith, is_gnostic_faith, adoptionist_school, divine_destiny, full_tolerance, has_jizya_doctrine, immaterial_harmony, not_allowed_to_hof, nudity_doctrine, pope_blessing |
| not_creatable | 7 | 15 | hostility_group, is_hostile_to_maitreya, christian_tradition_classification, preservation, has_clerical_electors, heresy_hostility, unreformed_faith |

校验：`grep -c "doctrine_group_type" common/religion/doctrine_types/*.txt` = 126；每个组 ≥1 个选项，无孤立组、无无组选项（脚本校验）。

### 1.3 组 → 选项完整列表（49 行，括号内为 `index`）

格式：`组名 (分类, 选项数) [组特性]`。

| # | group (category, n) | 组特性 | 选项（按 index 升序） |
|---|---|---|---|
| 1 | hostility_group (not_creatable, 4) | — | abrahamic(0) / pagan(1) / eastern(2) / sinitic(3) |
| 2 | doctrine_marriage_type (marriage, 3) | sequence | monogamy(0) / polygamy(1) / concubines(2) |
| 3 | doctrine_divorce (marriage, 3) | sequence | disallowed(0) / approval(1) / allowed(2) |
| 4 | doctrine_bastardry (marriage, 3) | sequence | none(0) / legitimization(1) / all(2) |
| 5 | doctrine_homosexuality (crimes, 3) | sequence | crime(0) / shunned(1) / accepted(2) |
| 6 | doctrine_deviancy (crimes, 4) | sequence | crime(0) / shunned(1) / accepted(2) / virtuous(3) |
| 7 | doctrine_adultery_men (crimes, 3) | sequence | crime(0) / shunned(1) / accepted(2) |
| 8 | doctrine_adultery_women (crimes, 3) | sequence | crime(0) / shunned(1) / accepted(2) |
| 9 | doctrine_kinslaying (crimes, 5) | sequence | any_dynasty_member_crime(0) / extended_family_crime(1) / close_kin_crime(2) / shunned(3) / accepted(4) |
| 10 | doctrine_witchcraft (crimes, 4) | sequence | crime(0) / shunned(1) / accepted(2) / virtuous(3) |
| 11 | doctrine_gender (main_group, 3) | sequence | male_dominated(0) / equal(1) / female_dominated(2) |
| 12 | doctrine_consanguinity (marriage, 5) | sequence | dynastic(0) / restricted(1) / cousins(2) / aunt_nephew_and_uncle_niece(3) / unrestricted(4) |
| 13 | doctrine_pluralism (main_group, 3) | sequence | fundamentalist(0) / righteous(1) / pluralistic(2) |
| 14 | doctrine_theocracy (main_group, 2) | sequence | temporal(0) / lay_clergy(1) |
| 15 | doctrine_head_of_faith (main_group, 3) | **main_rite=yes** | no_head(0) / spiritual_head(1) / temporal_head(2) |
| 16 | doctrine_clerical_function (clergy, 3) | — | taxation(0) / alms_and_pacification(1) / recruitment(2) |
| 17 | doctrine_clerical_gender (clergy, 3) | sequence | male_only(0) / either(1) / female_only(2) |
| 18 | doctrine_clerical_marriage (clergy, 2) | sequence | allowed(0) / disallowed(1) |
| 19 | doctrine_clerical_succession (clergy, 5) | — | temporal_appointment(0) / spiritual_appointment(1) / temporal_fixed_appointment(2) / spiritual_fixed_appointment(3) / conditional(4) |
| 20 | doctrine_pilgrimage (main_group, 5) | sequence | forbidden(0) / encouraged(1) / local_rites(2) / mandatory(3) / mandatory_hajj(4) |
| 21 | doctrine_funeral (clergy, 6) | — | stoic(0) / bewailment(1) / cremation(2) / sky_burial(3) / mummification(4) / family_rites(5) |
| 22 | doctrine_coronation (clergy, 3) | avail: `has_ach_dlc_trigger` | no_anointment(0) / anointment_permitted(1) / imperial_anointment(2) |
| 23 | doctrine_theism (main_group, 2) | — | monotheist(0) / polytheist(1) |
| 24 | doctrine_muhammad_succession (main_group, 4) | avail: `religion_tag = islam_religion` | sunni(0) / shia(1) / muhakkima(2) / zandaqa(3) |
| 25 | doctrine_temple_authority (main_group, 4) | avail: judaism | rabbinic(0) / toric(1) / kohanim(2) / samaritan_torah(3) |
| 26 | doctrine_zoroastrian_branches (main_group, 2) | avail: zoroastrianism | behdin(0) / zandik(1) |
| 27 | is_christian_faith (special, 1) | — | special_doctrine_is_christian_faith |
| 28 | is_islamic_faith (special, 1) | — | special_doctrine_is_islamic_faith |
| 29 | is_jewish_faith (special, 1) | — | special_doctrine_is_jewish_faith |
| 30 | is_eastern_faith (special, 1) | — | special_doctrine_is_eastern_faith |
| 31 | is_sinitic_faith (special, 1) | — | special_doctrine_is_sinitic_faith |
| 32 | is_hostile_to_maitreya (not_creatable, 2) | — | special_doctrine_maitreya_hostility(0) / special_doctrine_is_maitreya(1) |
| 33 | christian_tradition_classification (not_creatable, 2) | — | special_doctrine_is_western_christian_faith(0) / special_doctrine_is_eastern_christian_faith(1) |
| 34 | preservation (not_creatable, 3) | — | doctrine_scriptural(0) / doctrine_mythic_literary(1) / doctrine_oral_storytelling(2) |
| 35 | is_gnostic_faith (special, 1) | — | special_doctrine_is_gnostic_faith |
| 36 | special_tolerance (main_group, 2) | **lock=faith** | special_doctrine_ecumenical_christian / special_doctrine_ecumenical_miaphysite |
| 37 | has_clerical_electors (not_creatable, 1) | **lock=faith** | special_doctrine_has_clerical_electors |
| 38 | heresy_hostility (not_creatable, 1) | — | heresy_hostility_doctrine |
| 39 | nudity_doctrine (special, 1) | — | special_doctrine_naked_priests |
| 40 | unreformed_faith (not_creatable, 2) | — | unreformed_faith_doctrine(0) / west_african_unreformed_faith_doctrine(1) |
| 41 | divine_destiny (special, 1) | — | divine_destiny_doctrine |
| 42 | adoptionist_school (special, 3) | — | elipandic(0) / felicitous(1) / radical(2) |
| 43 | not_allowed_to_hof (special, 2) | — | special_doctrine_not_allowed_to_hof / special_doctrine_prophecy_of_malachi(1) |
| 44 | full_tolerance (special, 1) | 仅 game rule 写入 | special_doctrine_full_tolerance |
| 45 | has_jizya_doctrine (special, 1) | — | special_doctrine_jizya |
| 46 | immaterial_harmony (special, 1) | — | special_doctrine_immaterial_harmony |
| 47 | monasticism_group (clergy, 4) | sequence | encouraged(0) / accepted(1) / absent(2) / forbidden(3) |
| 48 | sacraments_group (clergy, 2) | — | central(0) / absent(1) |
| 49 | pope_blessing (special, 1) | **lock=rite**, divergence=-10 | special_doctrine_pope_blessing |

选项名省略前缀：如 `monogamy` = `doctrine_monogamy`（`20_doctrines.txt:1`）；special 组的选项多为 `special_doctrine_*`（`40_doctrines_special.txt`）。

---

## 2. 组字段语义（原文 + 要点）

来源：`common/religion/doctrine_group_types/_doctrine_group_types.info`。

| 字段 | 原文（节选） | 要点 | 证据 |
|---|---|---|---|
| `category` | "What doctrine category type is this group part of?" | 必填，指向 §1.1 的 6 个分类之一 | info:7-10 |
| `main_rite` | "Flags if the doctrine is only applied when practiced by the main rite. Default is no. **This flag only enables UI warnings - the actual implementation of this logic is left to script**" | 纯 UI/语义标记，引擎不自动执行；全原版只有 `doctrine_head_of_faith` 一个组用了它 | info:12-16；00_doctrine_group_types.txt:72 |
| `divergence` | `category`："Different doctrines within the group are separate customs. Final impact on divergence is the **sum** of 2 compared doctrines from the group"；`sequence`："Different doctrines form a sequence of opinions on a spectrum with clear opposites. Final impact on divergence is **absolute difference** of 2 compared doctrines" | sequence=有方向的刻度（如 crime→shunned→accepted）；category=无序并列（各自独立累加）。默认视为 category（未写字段的组共 32 个，写 `sequence` 的 17 个） | info:18-24；`grep -c "divergence = sequence"` = 17 |
| `doctrine_lock` | "Whether this group is tied to a specific religion / faith / rite or not. specific changes can make a rite lose this doctrine group. … `rite`: removed when characters of this rite create a new rite；`faith`: removed when the rite diverges enough to move to a different faith or a new rite is created …；`religion`: removed when the rite is moved to a faith of a different religion" | 决定"分家时这个组跟谁走"。原版仅 3 处：`special_tolerance=faith`(216)、`has_clerical_electors=faith`(226)、`pope_blessing=rite`(320) | info:26-37；00_doctrine_group_types.txt:216,226,320 |
| `is_available_on_create` | "When should this doctrine group be shown when choosing doctrines? If not shown, the group will go away entirely on create if present on the old faith. Supported scopes: root (Faith)" | 建新礼/新信仰时的过滤条件；`always = no` 表示"不可新建，只在历史/继承里存在"（unreformed、adoptionist 用它） | info:39-46；00_doctrine_group_types.txt:105-107,248-251,266-267,275-277 |

补充：组 key 同时决定图标路径与本地化（`<key>_name`、`DOCTRINE_GROUP_TYPE_ICON_PATH`），见 `_doctrine_group_types.info:3-4`、`common/defines/graphic/00_graphics.txt:747`。

---

## 3. 选项（doctrine）字段语义

来源：`common/religion/doctrine_types/_doctrine_types.info`（整份 350 行即字段规范）。

| 字段 | 语义 | 作用域 | 原版使用量 | 证据 |
|---|---|---|---|---|
| `doctrine_group_type` | 属于哪个组，**必填** | — | 126/126 | info:69-72 |
| `index` | 组内排序；**用于算 divergence**；必须唯一，重复时先定义者胜；可为负 | — | 全部 | info:74-78 |
| `divergence` | 该选项的背离值，默认 0；组按 category/sequence 解释 | — | 大多数 | info:55-59 |
| `piety_cost` | 建立/改信时花多少虔诚；作用域 **root (Rite)**；用 script value | Rite | 93/126 | info:80-88 |
| `parameters` | 任意布尔标志列表，供 `has_doctrine_parameter` / `rite_has_parameter` 查询 | Faith/Rite | 全部 | info:61-67 |
| `special_parameters` | 结构化数值/映射覆盖（见 §3.2） | — | 见下 | info:136-283 |
| `is_shown` | 建教义界面是否显示 | Faith | 4/126 | info:90-98 |
| `can_pick` | 是否可被选择；可用 `doctrine:xxx = { is_in_list = selected_doctrines }` 做互斥 | Faith / actor | 37/126 | info:100-113 |
| `character_modifier` | 该礼所有角色获得的修正（例：monthly_piety_gain_mult=-0.25） | — | 21/126 | info:115-121 |
| `clergy_modifier` | 该礼神职人员获得的修正 | — | 少量 | info:123-128 |
| `traits` | 追加美德/罪（`virtues={...} sins={...}`，带 weight/scale；同一特质取 weight 最大者） | — | 19/126 | info:130-134 |
| `icon` / `name` / `desc` / `historical_glossary` | 图标覆盖 / 动态名/描述/史料词条，缺省走 `<key>_name`、`<key>_desc` | Faith | icon 58/126 | info:6-47 |
| `visible` | 是否在 GUI 显示，默认 yes | — | 少数 special | info:49-53 |

### 3.1 `parameters` 是怎么定义、怎么被读取的

- 定义：教义块里裸列标志名，例 `doctrine_gender_male_dominated` 的 `parameters = { male_dominated_law female_claims_are_weak … }`（`20_doctrines.txt:831-862`，字段在 855-862 行）。
- 读取（触发器，`_doctrine_types.info:340-345` 原文）：
  - `rite_has_doctrine = doctrine_name`（Rite 作用域）
  - `has_doctrine_parameter = parameter_name`（Faith 作用域）
  - 1.20 实际脚本里最常用的是 **`rite_has_parameter = <param>`（Rite 作用域）**：全原版出现 1606 次（`grep -rh rite_has_parameter common/ events/ | wc -l`），因为教义现在挂在礼上。
- 本地化：`doctrine_parameter_` + 参数名（info:335 原文 "Effects are localized based on the parameter name. `doctrine_parameter_` plus the name."）。英文侧共 **547** 个 `doctrine_parameter_*` 键（其中 `religion_l_english.yml:125`/`religion_core_tenets_l_english.yml:251` 等）；教义文件共声明 151 个参数 token。
- 同一个 `has_doctrine_parameter` 也用于 tenet 的 `parameters`（`_tenet_types.info:61,177-178`）。

### 3.2 `special_parameters` 全键与合并规则

info 定义的全部键（`_doctrine_types.info:139-282`）：
`number_of_spouses`、`number_of_consorts`、`divine_marriage_opinion`、`minimum_fervor`、`fervor_per_holy_site`、`bonus_fervor_gain`、`bonus_heresy_protection`、`divergence_protection`、`heresy_threshold`、`hostility_levels{same_religion,same_family,unrelated}`、`hostility_level_override`、`same_hof_hostility_override`、`parameter_opinion_override`、`hostility_towards_us_mult`、`piety_from_long_peace`、`allowed_holding_types`、`unreformed`、`warmonger`、`increased_dynasty_prestige_birth_mult`、`increased_dynasty_prestige_marriage_mult`、`increased_dynasty_prestige_long_reign_mult`。

优先级原文（info:308-330）：
- "Order of definition; later definition, higher precedence."
- 取最大：`number_of_spouses` / `number_of_consorts` / `divine_marriage_opinion` / `minimum_fervor` / `fervor_per_holy_site` / `bonus_fervor_gain` / `bonus_heresy_protection` / `divergence_protection` / `hostility_towards_us_mult`
- `heresy_threshold` 全部相加
- `piety_from_long_peace` 与 `increased_dynasty_prestige_*` 取"最后一个非零"
- 列表与 map 合并（同键后者胜）
- `unreformed` / `warmonger` 任一为 yes 即 yes

敌对度（hostility）语义（info:285-304）：`righteous(0) < astray(1) < hostile(2) < evil(3)`；同 faith 恒 righteous；`RITE_DIVERGENCE_HOSTILITY_THRESHOLD` 以下降一级；意见值/战斗系数由 define `HOSTILITY_OPINION_EFFECTS`、`HOSTILITY_COMBAT_MOD_MULT` 决定（`common/defines/00_defines.txt:848-870`）。

---

## 4. 信仰如何引用教义（1.20 礼体系）

### 4.1 旧写法在 1.20 已不存在

- 全库检索 `doctrine_group = ` 只有 1 处，且是触发器 `has_doctrine_group = has_clerical_electors`（`common/religion/doctrine_group_types/00_doctrine_group_types.txt:223`），**不是字段**。字段名在 1.20 是 `doctrine_group_type`。
- 对 faith 文件做结构扫描：76 个 `doctrines = { }` 块，**0 个**块内出现 `=`（脚本校验），即不存在 `{ group = doctrine }` 映射写法。
- 结论：任务描述里的 `doctrines = { <group> = <doctrine> }` 是 1.20 之前的旧语法；1.20.1 已改为 §0 的三层扁平列表。

### 4.2 三层装配与优先级

| 层 | 写法 | 语义 | 证据 |
|---|---|---|---|
| religion | 重复 `doctrine = X` | "applied to all faiths within it. This is only at game start; it is purely for script convenience… It can be overridden by putting a different doctrine in the group in the specific faith." | `_religion_types.info:25-26`；`00_christianity.txt:9-53`（27 行） |
| faith | `doctrines = { X Y }` | "Faith doctrines are additive to a scripted main Rite. The faith doctrine is added when the Rite and parent religion have not already filled its doctrine group; a doctrine on the Rite in the same group takes precedence." | `_faith_types.info:37-44` |
| rite | `doctrines = { X Y }` | "Rite doctrines override parent-religion and faith doctrines from the same doctrine group. Faith doctrines from otherwise unfilled groups are added to the instantiated main Rite." | `_rite_types.info:74-82` |

即：**rite > faith > religion**；同组只会有一个生效选项（覆盖语义）。这就是"每个 group 只能有一个"的机制来源——不是语法限制，而是覆盖式装配。
运行时修改用 `add_doctrine` / `remove_doctrine`（`common/scripted_effects/00_game_rule_effects.txt:1024-1034`、`00_learning_lifestyle_effects.txt:1099-1150`），也印证"改组=替换同组选项"。

### 4.3 没写会怎样

- faith 没写 → 由同 religion 的 `doctrine = X` 兜底（info:26 原文）。
- religion 也没写 → 该组**没有** doctrine。脚本层找不到任何"默认教义"定义：`_doctrine_types.info` 无 default 字段，全库无 "default doctrine" 字样（检索为 0）。
- 原版实测：核心可创建组全部被 religion 覆盖。抽 4 个 faith 后"有效组数"：catholic 30、sunni 29、orthodox 29、norse 27、akom 27（脚本按 religion+faith+rite 求并集测得）。缺的组全部是 `special` / `not_creatable`（`is_available_on_create` 受限，本来就不该有），例如 `divine_destiny` 103/103 个 faith 都没有，只在南亚决议里 `add_doctrine`（`common/decisions/80_major_decisions_south_asia.txt:103`）。
- 未见任何脚本证据表明引擎会在"组为空"时回退 index 0；判为不确定项（§9）。

### 4.4 `rite_types` 与 doctrine 的关系（1.20 的"礼"）

- 礼是一等对象：`rite_types` 共 **152** 个，`faith = <parent faith>` 只是"生成用父 faith"（`_rite_types.info:35-40`）。
- 一个礼可被多个 faith 共用：`roman_rite` 的 `faith = christian_faith`，但 `catholic` 与 `christian_faith` 的 `main_rite` 都指向它（`00_rite_types.txt:1-3`；`00_faith_types.txt:535-537`）。
- faith 的 `tenets`/`doctrines` 只在"没有脚本主礼"时用于**种子化动态主礼**；有脚本主礼时被主礼覆盖（`_faith_types.info:29-44`）。103 个 faith 中 91 个有脚本 `main_rite`，12 个没有（动态生成）。
- 建礼/建信仰的分叉由背离值决定：`RITE_CREATION_DIVERGENCE_FAITH_THRESHOLD = 100`（`common/defines/00_defines.txt:831`），低于阈值建 Rite、达到阈值建 Faith；固定花费 `RITE_CREATION_PIETY_COST = 1.0`（:832）；教义自身的 `piety_cost` 作用域是 Rite（`_doctrine_types.info:80-88`）。

---

## 5. 参数被谁消费：机制示例

`has_doctrine` 在 common 里 2495 次、events 786 次；`has_doctrine_parameter` common 553 次/102 文件、events 211 次/47 文件；`rite_has_parameter` 1606 次。下表给可验证的真实机制链路。

| # | 机制 | 参数/教义 | 消费点（证据） |
|---|---|---|---|
| 1 | 罪行判定（淫乱、同性、巫术、异端行为成为 crime） | `adultery_male_crime` / `homosexuality_illegal` / `witchcraft_illegal` / `deviancy_illegal` | 脚本触发器 `trait_is_criminal_in_rite_trigger`（`common/scripted_triggers/00_religious_triggers.txt:410-484`）与 `trait_is_shunned_in_rite_trigger`（:486 起）逐特质映射；例如 sodomite→`homosexuality_illegal`、witch→`witchcraft_illegal` |
| 2 | 罪行还影响特质意见 | `adultery_male_crime` 等 | trait 的 `triggered_opinion = { parameter = adultery_male_crime … }`（`common/traits/00_traits.txt:12116` 起） |
| 3 | 性别法 → 头衔继承法 | `male_dominated_law` / `female_dominated_law` / `gender_equal_law` | 头衔性别法 `male_only_law` 的 `can_keep`/`should_start_with` 检查 `rite_has_parameter = male_dominated_law`（`common/laws/00_succession_laws.txt:1952,1987,2004`） |
| 4 | 女性不能受封头衔 / 议会性别门槛 | `women_cannot_be_granted_titles` / `male_dominated_council` | 前者仅存在于本地化键（引擎硬编码消费，脚本 0 引用）；后者在 `common/scripted_triggers/00_councillor_triggers.txt:36,46` |
| 5 | 能否创建信仰领袖头衔（HoF） | `spiritual_head_of_faith` / `temporal_head_of_faith` / `block_from_create_hof_parameter` | `can_create_head_of_faith_title_trigger`（`00_religious_triggers.txt:1556`）、`can_create_spiritual_head_of_faith_title_trigger`（:1513）、block 检查（:1538-1539）；决策侧 `pam_decisions.txt:1161` |
| 6 | 吉兹亚（jizya）契约解锁 | `unlock_jizya_contract` | `common/subject_contracts/contracts/special_contracts.txt:542,562`；`common/tax_slots/obligations/clan_tax_collector.txt:53` |
| 7 | 裸体祭司（立绘/事件） | `naked_priests_active` | `common/scripted_triggers/00_clothing_triggers.txt:14,32`；只由耆那教 `digambara` / `yapaniya` 礼持有（`00_rite_types.txt:1384,1420`） |
| 8 | 大圣战开关 | `great_holy_wars_active`、`divine_destiny_doctrine` | `events/religion_events/faith_creation_events.txt:385`；`common/scripted_triggers/00_great_holy_war_triggers.txt:20,64,83`；`divine_destiny` 由决议 `add_doctrine` 授予（`80_major_decisions_south_asia.txt:103`） |
| 9 | 朝圣门槛 | `can_go_on_pilgrimage` | `00_religious_triggers.txt:1580`（`rite = { rite_has_parameter = can_go_on_pilgrimage }`） |
| 10 | 一夫多妻/侧室上限 | `special_parameters.number_of_spouses / number_of_consorts` | 教义侧 `doctrine_monogamy/polygamy/concubines`（`20_doctrines.txt:1-64`）；文化传统用同名参数覆盖（`common/culture/traditions/00_ritual_traditions.txt:44,132`），说明该键由引擎按名字硬编码读取 |
| 11 | 不可改革信仰与劫掠/征服 CB | `unreformed`、`conquest_cb_enabled`、`faith_can_raid` | `40_doctrines_special.txt:196-256`（unreformed 教义）；`RITE_DIVERGENCE_*`/`pagan_roots` 影响 UI 是否算"已改革"（`_religion_types.info:24`） |
| 12 | 神权政体/教会领地 | `theocracy_temple_lease` / `theocracy_temple_ownership` / `allowed_holding_types = { church_holding }` | `20_doctrines.txt:1223-1280`；special_parameters 定义 `_doctrine_types.info:245-248` |

**"doctrine 本质是机制开关/枚举设置，不是信念文本"——成立，但有半边反例：**
- 成立面：官方概念定义就是"religious laws that determine what is acceptable behavior"（`localization/english/game_concepts_l_english.yml:330`）；触发器/参数全是枚举/布尔；`name`/`desc` 只是表现层。
- 反例：doctrine 还直接承载**数值修正**——21 个带 `character_modifier`（如 male_dominated 给 `opinion_of_female_rulers=-10`）、19 个带 `traits`（增删美德/罪权重）、`special_parameters` 的 `number_of_spouses` 等是实数。即"开关/枚举 + 一组结构化数值"，仍不是叙事文本，但比纯开关多一层数值层。

---

## 6. special 分类与 `special_doctrine_*`（27 个）

全部定义在 `common/religion/doctrine_types/40_doctrines_special.txt`（27 块，行号见下表）。共同特征：多数 `visible = no`、不可自由创建（`is_available_on_create` 绑 `has_doctrine = 自身` 或 `always = no`）、用于跨信仰关系与身份标记。

| 行 | doctrine | 组 | 关键 parameters | 作用 |
|---|---|---|---|---|
| 1 | special_doctrine_is_christian_faith | is_christian_faith | `special_doctrine_is_christian_faith`、`christian_syncretic_recipient`、`has_clerical_regions` | 标记"基督教系"；对 `tenet_christian_syncretism` 信仰 astray，对 adoptionist 两派 evil |
| 25 | special_doctrine_is_islamic_faith | is_islamic_faith | `islamic_syncretic_recipient` | 伊斯兰系标记 + 对 Islamic Syncretism astray |
| 45 | special_doctrine_is_jewish_faith | is_jewish_faith | `jewish_syncretic_recipient` | 犹太系标记 |
| 65 | special_doctrine_is_eastern_faith | is_eastern_faith | `eastern_syncretic_recipient` | 东方系标记 |
| 85 | special_doctrine_is_sinitic_faith | is_sinitic_faith | `sinitic_syncretic_recipient` | 华系标记 |
| 104 | special_doctrine_is_gnostic_faith | is_gnostic_faith | `granting_titles_gives_stress`、`gnosticism_cleric_interactions`、`gnosticism_mystic_bonus` | 诺斯替标记；traits 加 temperate 美德/gluttonous 罪；modifier `learning_per_piety_level=1`、`stewardship_per_piety_level=-1` |
| 136 | special_doctrine_ecumenical_christian | special_tolerance | —（纯 hostility_level_override） | 与 ecumenical/miaphysite 互 astray；lock=faith |
| 151 | special_doctrine_ecumenical_miaphysite | special_tolerance | — | 与 miaphysite righteous、与 ecumenical astray |
| 165 | special_doctrine_has_clerical_electors | has_clerical_electors | `has_clerical_electors` | 启用枢机选举人（天主教用） |
| 176 | heresy_hostility_doctrine | heresy_hostility | —（`same_religion = hostile`） | 同宗教内部按 hostile 处理（异端敌对） |
| 187 | special_doctrine_naked_priests | nudity_doctrine | `naked_priests_active` | 裸体祭司（耆那教两礼） |
| 196 | unreformed_faith_doctrine | unreformed_faith | `unreformed`、`conquest_cb_enabled`、`faith_can_raid`、`unreformed_syncretic_recipient` | 原始信仰：可劫掠、有征服 CB、威望+20%，`special_parameters.unreformed=yes` |
| 228 | west_african_unreformed_faith_doctrine | unreformed_faith | 同上 | 西非版不可改革（威望+10%） |
| 257 | divine_destiny_doctrine | divine_destiny | `divine_destiny_holy_war_cost_reduction`、`great_holy_wars_active` | 转轮圣王式"天命"：开启大圣战；由南亚决议动态 `add_doctrine` |
| 271/303/335 | adoptionist 三派（elipandic/felicitous/radical） | adoptionist_school | `adoptionists_like_ecumenism` / `adoptionists_are_unilateral_hostile` / `_evil` / `_mutual_evil`、`adoptionist_school_removed_on_reformation` | 嗣子论三派；各自 hostility 覆盖与美德/修正；改革时移除 |
| 369 | special_doctrine_not_allowed_to_hof | not_allowed_to_hof | `block_from_create_hof_parameter` | 禁止创建 HoF |
| 379 | special_doctrine_prophecy_of_malachi | not_allowed_to_hof | `block_from_create_hof_parameter` | 犹太版"玛拉基预言"（名/描述独立 loc） |
| 392 | special_doctrine_full_tolerance | full_tolerance | —（对自身 righteous） | **仅 game rule**：`00_game_rule_effects.txt:1034` 用 `add_doctrine` 注入 |
| 403 | special_doctrine_jizya | has_jizya_doctrine | `unlock_jizya_contract` | 吉兹亚契约 + 异教税/征召修正；所有伊斯兰 faith 的 faith 层都有 |
| 418 | special_doctrine_immaterial_harmony | immaterial_harmony | `immaterial_harmony_no_holy_orders`、`reduced_vassal_religion_discontent`、`immaterial_harmony_conversion_cost_reduced` | 无为和谐（佛教/儒/神道等）；对自身 righteous |
| 435/448 | maitreya_hostility / is_maitreya | is_hostile_to_maitreya | — | 弥勒信仰的敌对覆盖与标记 |
| 457/468 | is_western/eastern_christian_faith | christian_tradition_classification | `is_western_christian_faith` / `is_eastern_christian_faith` | 立绘/外观区分东西教会（注释明说 "assigned per faith so portraits can tell Western Christianity from Eastern"） |
| 479 | special_doctrine_pope_blessing | pope_blessing | `special_doctrine_pope_blessing_divergence` | 罗马教皇祝福；`divergence = -10`、`doctrine_lock = rite` |

---

## 7. 信条（Tenet）

### 7.1 数量与结构

- 原版 tenet **97** 个：`00_tenet_types.txt` 71 + `00_pam_tenets.txt` 26。
- 字段规范：`common/religion/tenet_types/_tenet_types.info:1-180`。

| 字段 | 语义 | 数量 | 证据 |
|---|---|---|---|
| `icon` | 支持 `trigger`+`reference` 的条件图标 | — | info:6-18 |
| `name` / `desc` / `historical_glossary` | 动态名/描述，默认 `<key>_name`、`<key>_desc`；作用域 root(Rite) | — | info:20-46 |
| `visible` | GUI 是否显示 | — | info:48-52 |
| `requires_dlc_flag` | 需要 DLC 标志，"doesn't automatically fail is_shown checks, this only applies to code-driven tenet lists" | 少量 | info:54-58 |
| `parameters` | 与 doctrine 同名机制，`has_doctrine_parameter` 查询 | 大多数 | info:60-66 |
| `piety_cost` | 花费；作用域 root(Rite)；原版用 `faith_tenet_cost_*` script value | 96/97 | info:68-74 |
| `is_shown` / `can_pick` | 显示/可选；`can_pick` 用 `tenet:required_tenet = { is_in_list = selected_doctrines }` 做互斥 | 80 有 can_pick | info:76-104 |
| `can_pick_as_personal_tenet` | **能否作为"个人信条"**，作用域 root(character) | 14 | info:106-114 |
| `divergence_multiplier` | 背离值乘数，只作用于正 divergence，默认 1 | — | info:116-121 |
| `character_modifier` / `clergy_modifier` / `county_modifier` | 角色/神职/伯爵领修正 | 70/0/18 | info:123-147 |
| `traits` | 追加美德/罪，tenet 优先于 religion 同名条目 | 97/97 | info:149-154 |
| `special_parameters` | 同 doctrine 的 `special_parameters` | 15 | info:156-161 |
| `personal_tenet_modifier` | 选为个人信条的角色获得的修正 | 57 | info:163-168 |
| `personal_tenet_parameters` | 个人信条标志，用 `has_personal_tenet_flag` 查询 | 95 | info:170-176 |

**纠错：`tenet_types` 没有 `effects` 字段。** `_tenet_types.info` 全文 0 次 `effects`；原版 tenet 定义 0 处 `effects =`。tenet 的"效果"由 `parameters`（脚本里被触发器消费）+ 三种 modifier + `traits` + `special_parameters` 组合实现。证据：`grep -rn "^\s*effects\s*=" common/religion/tenet_types/*.txt` 无输出。

### 7.2 一个 faith 能带几个 tenet

- 核心信条上限由 define 写死：`FAITH_CORE_TENETS_CAP = 3`（`common/defines/00_defines.txt:908`，注释 "max amount of core tenets a faith can have"）。
- 静态数据的表现：faith 的 `tenets = {}` 只在"无脚本主礼"时种子化动态主礼；有脚本主礼时以礼的 `tenets` 为准（`_faith_types.info:29-44`）。所以 faith 块里常见 0 条（catholic、sunni），礼里有 1-3 条（roman_rite 1 条 `tenet_communion`，hanafi 3 条）。
- 例：`catholic` 的 faith 层 `tenets` 为空，主礼 `roman_rite` 给 1 条（`00_faith_types.txt:535`；`00_rite_types.txt:1-14`）。

### 7.3 `tenet_status`（1.20 礼体系下）

- 状态共 **5 档：unknown / known / permitted / prohibited / core**（不是任务描述里的 "mainline"）。
  - `common/game_concepts/pam_game_concepts.txt:332-357` 定义 `tenet_status` 概念及五个子概念，`parent = rite`（"Rite-side stance"）。
  - 本地化：`localization/english/dlc/pam/pam_effects_l_english.yml:51-55`。
  - 顺序与背离值：`TENET_STATUS_DIVERGENCE = { 15 15 30 5 -5 }`，注释 "Values are Unknown, Known, Prohibited, Permitted, Core；Core tenets are checked both ways"（`00_defines.txt:838-840`）。
- 存储/写入：`set_tenet_status = { tenet = X status = permitted|prohibited|known|unknown|core }` 是 **faith 作用域效果**（`common/on_action/religion_on_actions.txt:813-825`；`pam_ecumenical_council_effects.txt:648-688`，大公会议可把 tenet 升为 core 或降为 permitted/prohibited）。
- 查询：`has_tenet_status = { tenet = X status = core }`，在 **rite 作用域**调用（`common/customizable_localization/ecumenical_council_debate_status_loc.txt:20-23` 用 `faith.main_rite = { has_tenet_status … }`）。
- "mainline" 不是 tenet 状态：`mainline_rite` 是独立的游戏概念（"the faith's main rite"，`pam_game_concepts.txt:206-209`）。任务里的 "core/mainline/known/…" 应更正为上述五档。

### 7.4 personal tenet（个人信条）

- PAM（By God Alone）DLC 功能：`personal_tenet` 概念 `requires_dlc_flag = by_god_alone`（`pam_game_concepts.txt:323-327`）。
- 角色维度，不属于 faith：上限 `BASE_PERSONAL_TENETS_CAP = 1`（"base amount of personal tenets a playable character can subscribe to"，可被虔诚等级提高），换/删冷却 `PERSONAL_TENET_CHANGE_COOLDOWN_DAYS = 1095`（`00_defines.txt:902-906`）。
- 数据：`can_pick_as_personal_tenet`（14 个 tenet）、`personal_tenet_modifier`（57 个）、`personal_tenet_parameters`（95 个）。
- 脚本查询：`has_personal_tenet`、`has_personal_tenet_flag`（例：`common/scripted_effects/00_activity_effects.txt:3679`）。
- 精神满足结算：`SPIRITUAL_FULFILLMENT_MULT_PERSONAL_TENET = 2`、`..._CORE_TENET = 2`、`..._PERMITTED_TENET = 0.5`、`..._RELIGION_TRAIT = 1`（`00_defines.txt:892-900`）。

---

## 8. 统计与抽样

### 8.1 原版数量（统计命令 + 结果）

| 对象 | 数量 | 统计命令（在 game 目录执行） |
|---|---|---|
| doctrine_category | 6 | `grep -cE '^[a-zA-Z_][a-zA-Z0-9_]* = \{' common/religion/doctrine_category_types/00_doctrine_category_types.txt` |
| doctrine_group | 49 | 同上，对 `doctrine_group_types/00_doctrine_group_types.txt` |
| doctrine（选项） | 126 | `grep -rh "doctrine_group_type" common/religion/doctrine_types/*.txt \| wc -l` |
| tenet | 97 | 逐文件 `^[a-zA-Z_][a-zA-Z0-9_]* = \{` 计数（71+26） |
| faith | 103 | `grep -cE '^[a-zA-Z_][a-zA-Z0-9_]* = \{' common/religion/faith_types/00_faith_types.txt`（+首行 BOM 1） |
| religion | 49 | `grep -h "_religion = {" common/religion/religion_types/*.txt \| wc -l` |
| rite（礼） | 152 | 逐文件计数：76+10+66 |
| doctrine_parameter loc 键 | 547 | python 正则去重：`^\s*doctrine_parameter_[A-Za-z0-9_]+:` 扫 `localization/english/religion/*.yml` + `localization/english/dlc/pam/*.yml` |

> 注：`grep -cE '^...'` 对带 BOM 的首行会漏计 1，故 faith/ritual/tenet 需 +1，表中数字为已修正值。

### 8.2 抽样：faith 真实数据

**catholic**（`00_faith_types.txt:535`）：`main_rite = roman_rite`，religion = `christianity_religion`；faith 层 `doctrines = { special_doctrine_ecumenical_christian, special_doctrine_has_clerical_electors }`，`tenets` 空。
主礼 `roman_rite`（`00_rite_types.txt:1-14`，`faith = christian_faith` 在第 5 行）：`tenets = { tenet_communion }`，`doctrines = { special_doctrine_is_western_christian_faith, doctrine_clerical_marriage_disallowed, doctrine_adultery_men_shunned, doctrine_homosexuality_shunned }`。
有效教义 30 组：来自 religion 的 25 组（27 条 `doctrine =` 中 2 条被覆盖；`00_christianity.txt:9-53`），来自 rite 的 4 组（christian_tradition_classification、doctrine_adultery_men、doctrine_clerical_marriage、doctrine_homosexuality），来自 faith 的 1 组（special_tolerance）。

**sunni**（`00_faith_types.txt:2604`）：`main_rite = hanafi`，religion = `islam_religion`（27 条 religion 级 `doctrine =`）；faith 层 `special_doctrine_jizya`。
主礼 `hanafi`（`00_rite_types.txt:569`）：`tenets = { tenet_struggle_submission, tenet_religious_legal_pronouncements, tenet_legalism }`，`doctrines = { muhammad_succession_sunni_doctrine, doctrine_temporal_head }`。有效 29 组。

**akom_pagan**（`00_faith_types.txt:1`，原始信仰）：`main_rite = akom_pagan`；faith 层 `doctrines = { west_african_unreformed_faith_doctrine }`；主礼 `akom_pagan`（`02_mainline_rite_types.txt:14`）同样 3 条 tenet（`tenet_adorcism, tenet_ritual_celebrations, tenet_ancestor_worship`）+ unreformed 教义。有效 27 组，其中 `unreformed_faith` 组来自 rite，其余 26 来自 religion。

**norse_pagan**（`00_faith_types.txt:2097`）：`main_rite = norse_pagan`（`02_mainline_rite_types.txt:330`）；tenet 仅 `tenet_warmonger`（礼与 faith 同）；`doctrine = unreformed_faith_doctrine` 出现在 faith 与 rite 两层（同组覆盖，结果一致）。

---

## 9. 无法确定 / 需进一步验证的点

1. **"组完全为空"时引擎的行为**：脚本层没有任何 default doctrine 定义，也没有 "默认取 index 0" 的注释或效果。原版数据里核心组都被 religion 覆盖，因此无实测样本。判断"缺组=该组机制全为 false"是推断，不是证据；需引擎调试或故意留空做实验。
2. **`rite_has_parameter` 触发器未见于任何随游戏发布的 `.info`/文档**（CK3 不放触发器文档）。其存在与作用域仅能从 1606 次使用反推（例：`common/laws/00_succession_laws.txt:1952` 中 `rite = { rite_has_parameter = ... }`）。它是否严格等价于"该 Rite 有某 doctrine 的 parameters 含该标志"未经官方文本确认。
3. **部分参数完全由引擎硬编码消费**：如 `women_cannot_be_granted_titles`、`women_cannot_inherit_implicit_claims` 只出现在定义与本地化里，脚本 0 引用；`number_of_spouses` 只在 doctrine 与文化传统里被"定义"，无脚本消费点。因此"参数被谁消费"的完整清单必然有一部分在黑盒里。
4. **`TENET_STATUS_DIVERGENCE` 的具体映射算法**：define 给了 {15 15 30 5 -5} 与五个状态名，但状态改变在大公会议/事件里是显式 `set_tenet_status` 调用；该数组究竟参与哪一步计算（自动降级？cost？）无脚本证据。
5. 「每个 group 只能有一个选项」在 1.20 是**覆盖语义的结果**，不是语法校验：理论上同一层写两个同组 doctrine 时谁胜出取决于定义顺序/后者胜，脚本未给出显式报错证据（仅 info 说 "later definition, higher precedence"）。
