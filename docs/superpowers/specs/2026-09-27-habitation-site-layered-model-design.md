# 栖居点分层模型设计

## 1. 目标与范围

本设计统一游戏内百科、存档数据和模拟代码中的栖居点术语，解决旧模型把 Anchor、Settlement、Camp、据点类别、发展等级和设施形制混为一体的问题。

本设计覆盖：

- 栖居点的正式中英文术语和 JSON 键名；
- 位点模式、位点分类、定居发展和设施形制的边界；
- Anchor 的保留范围；
- 模式转换、营地消散和设施损毁的规则。

本设计不决定具体的数值平衡、全部据点子类型、完整设施目录或 UI 布局。

## 2. 正式术语

### 2.1 父概念

| 中文正式术语 | 英文正式术语 | 集合键 | 单体键 |
|---|---|---|---|
| 栖居点 | `HabitationSite` | `habitation_sites` | `habitation_site` |

栖居点是地图上具有位置、占据记录和控制关系的人居实体。它可以在不同的功能模式之间转换，但转换通常保留同一个 `site_id`。

“栖居位点”只作为历史设计文档中的别名，不作为新的代码、存档或百科正式术语。

### 2.2 三种位点模式

| 中文 | UI 英文 | `site_mode` | 定义 |
|---|---|---|---|
| 定居点 | Sedentary Settlement | `sedentary_settlement` | 长周期常驻的民生栖居地，具有稳定人口、土地关系以及农耕或手工业基础。 |
| 据点 | Fortified Outpost | `fortified_outpost` | 以军事驻守、战略控制或前沿管控为核心的栖居点；平民人口可以存在，也可以缺失。 |
| 营地 | Temporary Encampment | `temporary_encampment` | 临时、季节性或流动人群的驻居点，不要求固化土地权属，可以因撤离或流散而整体消失。 |

底层枚举必须使用完整的小写下划线形式。`burg`、`holding` 和作为底层枚举的 `camp` 均禁止使用。

## 3. 分层数据模型

### 3.1 栖居点身份与状态层

该层只描述实体身份和当前状态：

```json
{
  "site_id": "uuid-xxxx",
  "site_mode": "sedentary_settlement",
  "site_name": "",
  "location": { "x": 0.0, "y": 0.0 },
  "site_class": "",
  "control_faction": "",
  "population": {},
  "facilities": []
}
```

`site_mode` 是模式判别字段，不使用通用 `type` 代替。`type` 如在其他领域存在，应保留给明确的地貌、遗址或资源分类，不能表示位点生命周期。

### 3.2 位点分类层

`site_class` 仅用于地貌、遗址或其他位点分类。它不能改变 `site_mode` 的含义，也不能承载城墙几何形状、发展等级或军事设施类别。

### 3.3 模式专属数据层

模式专属数据按模式分组：

- `sedentary_settlement_data`
  - `settlement_morphology`
  - `development_stage`
  - 其他只对定居点有意义的民生与发展字段；
- `fortified_outpost_data`
  - 可选的据点子类型或用途字段；
  - 只有在产生独立玩法规则时，才建立新的 subtype；
- `temporary_encampment_data`
  - 临时驻居、迁移、补给或撤离所需字段。

校验规则要求：只有与当前 `site_mode` 对应的模式数据生效。模式转换时，保留可迁移的历史信息，但不得让失效模式数据继续参与当前模拟。

定居点的 `settlement_morphology`（如村落、集镇、城市等）与 `development_stage` 是两个不同维度，不能用一个泛化的 `level` 替代。

### 3.4 设施层

设施是栖居点内可独立建造、损毁、修复或替换的对象，统一放在 `facilities[]` 中。设施至少应能表达：

- `facility_id`
- `facility_type`
- `construction_state`
- `damage_state`
- `material`
- `facility_form`

城墙、城门、壕沟、栅栏、土垒等均属于设施。**形制不属于栖居点本体。**

城墙的几何形状放在设施的形制数据中，例如：

```json
{
  "facility_type": "wall",
  "facility_form": {
    "wall_shape": "concentric"
  }
}
```

`wall_shape` 只表达城墙布局或几何形态；它不能被写入 `site_mode`、`site_class` 或定居发展字段。材料、施工状态和损毁状态同样不应借用位点模式字段。

## 4. Anchor 边界

Anchor 保留为内部基础设施，仅负责地图定位、空间索引或其他通用引用能力。它不再是栖居点的游戏业务类型，也不出现在游戏内百科的正式概念中。

Anchor 不得继续承载以下业务含义：

- 定居点、据点或营地的模式；
- 栖居点发展形态或等级；
- 设施形制；
- 营地是否可以消散。

栖居点业务数据以 `HabitationSite` 为唯一入口。Anchor 与栖居点通过明确的内部引用关联，例如 `site_id` 或专用索引字段；实现阶段应避免让业务代码反向依赖旧的 Anchor 语义。

## 5. 模式转换

### 5.1 允许的转换

```text
sedentary_settlement <-> fortified_outpost
fortified_outpost   <-> temporary_encampment
sedentary_settlement -> temporary_encampment
temporary_encampment -> fortified_outpost
```

转换通常只更新 `site_mode`，保留 `site_id`、位置和可迁移历史。可选的 `prev_site_mode` 用于历史记录，`pending_site_mode` 用于延迟转换。

### 5.2 禁止的转换

- `temporary_encampment` 不得直接转换为 `sedentary_settlement`，必须先经过 `fortified_outpost`；
- `sedentary_settlement` 不得直接删除；
- 设施形制变化不得伪装成位点模式变化。

### 5.3 营地消散

`temporary_encampment` 可以因军队撤离、游牧迁移、难民流散等原因整体消散。消散时直接从 `habitation_sites` 集合移除该栖居点，不保留空的地产或 `holding` 外壳。

## 6. 损毁与转化边界

破坏行为、行动进度和实际损毁是不同概念：

- 行动进度描述围攻、焚烧、拆除等行为是否完成；
- 设施损毁描述具体设施当前的损坏程度；
- 位点模式转换描述栖居点功能状态是否改变；
- 后果规则决定设施移除、修复、降级或营地消散。

局部设施受损不应自动修改整个栖居点的 `site_mode`。只有明确的模式转换规则才能改变位点模式；转换判定应读取相关设施和人口、控制等状态，而不是把某个全局 `damage_state` 当作唯一依据。

## 7. 百科与代码规范

游戏百科只展示玩家可见且具有玩法意义的概念：

- 栖居点、定居点、据点、营地；
- 定居发展形态；
- 据点子类型（仅当有独立玩法）；
- 设施、设施形制和城墙形状。

百科不展示 UUID、内部 Anchor、管理器类名或 JSON 实现细节。

代码和存档规范：

1. 栖居点业务统一使用 `HabitationSite` 语义；
2. 禁止把 `habitation_site` 简写成 `settlement`；
3. 禁止使用 `burg` 作为栖居点或定居点键名；
4. 禁止把 CK3 的 `holding` 作为核心模型；
5. 禁止使用 `camp` 作为底层模式枚举；
6. 设施形制必须位于设施对象；
7. 模式转换必须保留 `site_id`，营地消散除外；
8. 新增术语必须同时给出中文正式名、英文正式名和键名，并明确所属层级。

## 8. 实施边界

后续实现应先建立分层数据契约和校验，再迁移旧的 Settlement/Camp 业务。旧字段或兼容读取逻辑只有在明确映射到上述层级后才能保留；不得为了减少改动而继续扩大旧术语的语义。

本规格不要求一次性完成全部旧系统迁移。实现计划应将术语迁移、数据结构、转换规则、设施损毁和存档兼容拆成可验证的步骤。
