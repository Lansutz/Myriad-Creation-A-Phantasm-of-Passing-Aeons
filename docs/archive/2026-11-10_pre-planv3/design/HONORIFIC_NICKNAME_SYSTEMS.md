# 三套独立名称系统：头衔 / 尊号 / 绰号

> 2026-11-05。结论：头衔、尊号、绰号是三个独立系统，不能合并。
> 现有代码：`TitleDef/TitleDefinition/TitleManager`、`EpithetCatalog/EpithetSystem/EpithetDef/EpithetConnotation/EpithetTier`、`CharacterData.epithet`。

---

## 1. 三套系统的边界

| 系统 | 是什么 | 有无法律权利 | 谁授予 | 例子 |
|---|---|---|---|---|
| **头衔 Title** | 法律/官职/领地/贵族/荣誉职位 | 有（绑定/权利/义务） | 继承/任命/选举/授予 | 王、公爵、执政官、将军、领主 |
| **尊号 Honorific** | 正式尊崇称号/美称/谥号/庙号 | 无（或仅有礼仪地位） | 朝廷/教会/继任者/追尊 | 尊号、徽号、谥号、庙号 |
| **绰号 Nickname** | 非正式的外号/诨名/公众称呼 | 无 | 同时代人/军队/百姓/敌人 | 征服者、秃头、狐狸、疯子 |

一句话：

```text
头衔 = 法律地位
尊号 = 正式美称
绰号 = 非正式外号
```

---

## 2. 现有代码的问题

### 2.1 头衔

- `TitleDef`（内容）与 `TitleDefinition`（运行）重复；
- `TitleManager` 是另一个静态 registry，几乎未注册；
- 无 `TitleInstance/TitleHolding`；
- `CharacterData` 没有头衔持有列表。

### 2.2 绰号/尊号混在一起

`EpithetSystem` 现在同时做：

- 给角色发“征服者/秃头/狐狸/疯子”等——这是**绰号**；
- `DeterminePosthumousTitle` 返回“武/仁/明/厉/荒/暴”等——这是**谥号**，属于尊号/谥法系统；
- `EpithetTier` 有 `Kingly/Great`，把绰号与君主/伟大者混层；
- `CharacterData.epithet` 只有一个字符串，没有来源/类别/授予者/时间。

问题：

- 绰号和谥号混用；
- 尊号没有独立记录；
- 一个角色只能有一个 `epithet`，不能同时有“尊号 + 绰号 + 谥号”；
- 无法表达“生前绰号、死后尊号/谥号、正式头衔”三者并存。

---

## 3. 数据模型

### 3.1 头衔 Title

```text
TitleDef
  ├─ titleId
  ├─ referents[]
  ├─ appellations[]
  ├─ bindings[]
  └─ rights/claims

TitleHolding
  ├─ titleId
  ├─ holderCharacterId
  ├─ polityId / territoryId / officeId
  ├─ liegeTitleId
  ├─ startDay/endDay
  └─ legitimacy
```

### 3.2 尊号 Honorific

```text
HonorificRecord
  ├─ honorificId
  ├─ targetCharacterId
  ├─ category: 尊号/徽号/谥号/庙号/其他
  ├─ nameForm
  ├─ languageId
  ├─ issuer: 朝廷/教会/继任者/追尊
  ├─ issuedDay
  ├─ revokedDay
  ├─ formalRank
  └─ displayOrder
```

- 可以有多个；
- 可以追尊、加尊、改尊、夺号；
- 无绑定权利；
- 不与头衔共用 `TitleDef`。

### 3.3 绰号 Nickname

```text
NicknameRecord
  ├─ nicknameId
  ├─ targetCharacterId
  ├─ nameForm
  ├─ connotation: positive/neutral/negative/dual
  ├─ tier: common/kingly/great
  ├─ origin: 民间/军队/宫廷/敌人/身体特征/事件
  ├─ giverScope: 同时代人/后世/某群体
  ├─ coinedDay
  ├─ languageId
  └─ active
```

- 可以多个；
- 非正式，不能“授予官职”；
- 不能被“撤销”，只能淡出/被新绰号取代；
- 可进入史书/民间记忆，但不自动成为尊号或头衔。

---

## 4. 显示

角色页分栏：

```text
角色页
  ├─ 头衔 Title（法律）
  ├─ 尊号 Honorific（正式美称）
  ├─ 谥号/庙号（尊号子类）
  ├─ 绰号 Nickname（非正式）
  └─ 年号（纪年，不属于以上三者）
```

- 头衔显示：`referent + appellation`；
- 尊号显示：`nameForm` + 类别；
- 绰号显示：`nameForm` + 来源/褒贬；
- 地图不显示任何三者；
- 不把绰号/尊号塞进 `TitleDisplayStack`。

---

## 5. 互相关系

| 关系 | 规则 |
|---|---|
| 绰号 → 尊号 | 只有被朝廷/教会正式上尊号，才新建 `HonorificRecord`；原绰号保留 |
| 尊号 → 绰号 | 尊号被废/被嘲，可产生新绰号；不自动改名 |
| 头衔 → 尊号 | 头衔不自动生成尊号；尊号可独立授予 |
| 头衔 → 绰号 | 头衔不自动生成绰号；绰号由行为/身体/事件产生 |
| 谥号 | 死后由继任者/朝廷议定，属尊号系统，不是绰号 |
| 庙号 | 宗庙祭祀名号，属尊号系统 |
| 年号 | 纪年，不是头衔/尊号/绰号 |

---

## 6. 迁移

1. `EpithetSystem` 拆成 `NicknameSystem`（绰号）和 `HonorificSystem`（尊号/谥号/庙号）；
2. `CharacterData.epithet` 字符串迁移为 `NicknameRecord` 列表；
3. `DeterminePosthumousTitle` 从 `EpithetSystem` 移到 `HonorificSystem`；
4. `EpithetTier/Kingly/Great` 归绰号等级，不用于头衔；
5. 新增 `HonorificRecord`，支持多尊号/追尊/夺号；
6. 角色页分栏；
7. 头衔系统独立接 `TitleHolding`。

---

## 7. 结论

- 头衔、尊号、绰号是三个独立系统；
- 现有 `EpithetSystem` 把绰号和谥号混在一起，要拆；
- 绰号不能当尊号，尊号不能当头衔；
- 一个角色可以同时有多个头衔、多个尊号、多个绰号；
- 显示分栏，不合并。
