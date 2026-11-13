# 革新 + 配方系统（MC 式数据驱动）

> 2026-11-04。目标：像 Minecraft 一样，**加 JSON 就加配方/革新**，不改代码；模组可按 id 覆盖。
> 路径：`StreamingAssets/Base/Crafting/Recipes.json`；Mods 同路径覆盖。

---

## 1. 配方 schema

```json
{
  "recipeId": "alloy_bronze",
  "displayName": "青铜",
  "outputKind": 0,
  "outputGoodsId": 56,
  "outputAmount": 1.0,
  "inputs": [
    { "goodsId": 53, "amount": 0.8 },
    { "goodsId": 55, "amount": 0.2 }
  ],
  "requiredInnovation": 201,
  "requiredBuilding": 0,
  "craftDays": 0.5,
  "process": 1,
  "processDetail": "铜锡合金"
}
```

字段：

| 字段 | 含义 |
|---|---|
| `recipeId` | 稳定 id；空则由产物生成；Mods 用同 id 覆盖 |
| `outputKind` | `Good/Equipment/Unit/Building`；当前运行时只消耗 Goods |
| `inputs` | 物资 id + 数量；多输入/替代配方都靠多条 recipe |
| `requiredInnovation` | 0=无；>0 需已掌握 |
| `requiredBuilding` | 0=无；>0 需设施 |
| `craftDays` | 工时（数据先行） |
| `process` | `CraftProcess` 大类 |
| `processDetail` | 范铸/失蜡/自由锻等细分 |

---

## 2. 为什么像 MC

- **数据定义**：新增配方 = 新增 JSON 条目，不改 `SettlementProduction`；
- **稳定 id**：模组可覆盖同 id，也可加新 id；
- **同产物多配方**：不再是 `Dictionary<output, recipe>` 单条；`GetByOutput` 返回多条；
- **输入计数**：`RecipeIngredient.amount`；
- **可选门槛**：科技/设施/工艺；
- **Base→Mods**：`RecipeCatalog` 先 Base 后 Mods，同 id 覆盖。

---

## 3. API

```csharp
RecipeCatalog.TryGet("alloy_bronze", out var recipe);
RecipeCatalog.TryGet(56, out var firstBronze); // 兼容旧调用
RecipeCatalog.GetByOutput(56);                // 多条替代配方
RecipeCatalog.GetAll();
RecipeCatalog.CanCraft(recipe, inventory);
```

合成：

```csharp
SettlementProduction.TryCraft(market, outputGoodsId, out produced); // 旧：只校验输入/容量
SettlementProduction.TryCraft(market, outputGoodsId, out produced,
    innovations, realmId, hasBuilding); // 新：科技/设施门禁
```

---

## 4. 革新连接

- `InnovationDef.unlockRefs` 新增 `InnovationUnlockKind.Recipe`；
- 例：201 青铜冶炼 → `smelt_copper`/`smelt_tin`/`alloy_bronze`；202 → `smelt_iron`；203 → `make_steel`；
- 配方自身也有 `requiredInnovation`，运行时校验；
- 当前 `Crafting/Recipes.json` 有 43 条，其中 24 条装备配方（outputKind=Equipment）；革新→配方→装备；
- 法律由 `InnovationUnlockKind.Law` 直接引用 lawId（制度不是物品，不走配方）。
- 物品多革新约束走 `EquipmentDef.requiredInnovationTree`（火绳枪=307+207，射石炮=308+207）。

---

## 5. 模组化

```text
Base/Crafting/Recipes.json   // 基准
Mods/<Mod>/Crafting/Recipes.json
```

规则：

- 同 `recipeId`：Mods 覆盖 Base；
- 新 `recipeId`：追加；
- 非法输入/自引用：跳过并 warning；
- 不复制代码，只改数据。

---

## 6. 测试

- `RecipeSystemTests`：稳定 id、同产物多条、Mods 同 id 覆盖；
- `RecipeTests`：多输入青铜；
- `RecipeGraphTests`：矿石/金属/钢/纤维/皮革/陶器/玻璃；
- `RecipeGateTests`：202/设施门禁；
- `InnovationUnlockRefTests`：Recipe 引用不悬空。

---

## 7. 仍待

- `outputKind != Good` 的运行时消费（装备/单位/建筑配方）；
- 配方 UI/配方书（按已掌握革新过滤）；
- 设施类别与配方 station 的绑定；
- 模组依赖/加载顺序声明。
