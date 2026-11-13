# 物资合成表（Recipes）

> 2026-11-04。配方数据在 `StreamingAssets/Base/Crafting/Recipes.json`；运行时由 `RecipeCatalog` 按稳定 `recipeId` 加载，`SettlementProduction.TryCraft` 消耗输入、产出输出。
> 原则：**配方只负责物资转化；革新只负责解锁物品/能力**，不再为“青铜兵器铸造/铁兵器锻造”单独造无意义革新。

## 1. 当前配方

| 输出 | 输入 | 说明 |
|---|---|---|
| 31 加工木材 | 30 原木 | 木材加工 |
| 14 木炭 | 30 原木 | 燃料/还原剂 |
| 53 铜 | 51 铜矿 | 冶炼 |
| 55 锡 | 54 锡矿 | 冶炼 |
| 52 铁 | 50 铁矿 | 冶炼 |
| 56 青铜 | 53 铜 + 55 锡 | 合金 |
| 57 钢 | 52 铁 + 14 木炭 | 渗碳/炼钢 |
| 100 面粉 | 0 小麦 | 加工 |
| 101 米 | 10 稻谷 | 加工 |
| 12 棉布 | 11 棉花 | 纺织 |
| 120 麻布 | 110 亚麻 | 纺织 |
| 121 莎草纸 | 112 莎草 | 造纸 |
| 122 苇席 | 111 芦苇 | 编织 |
| 70 武器 | 56 青铜 | 具体武器物品仍是 EquipmentDef；70 是物资口径 |
| 71 盔甲 | 56 青铜 | 同上 |

## 2. 与革新的连接

- `201 青铜冶炼`：解锁青铜装备（EquipmentDef 4/10/11/12）；
- `202 块炼铁`：解锁铁装备（EquipmentDef 5/13/14）；
- 不再保留 `300 青铜兵器铸造`、`301 铁兵器锻造` 这种单独革新；
- 配方本身不绑定某条革新；是否允许生产由设施/市场/科技前置在系统层控制。

## 3. 具体品种（不再有“粮食/谷物”这种通用物资）

- 0 小麦；
- 10 稻谷；
- 15 粟；
- 16 黍；
- 17 大麦；
- 18 玉米；
- 19 马铃薯；
- 100 面粉；
- 101 米。

“谷物类”只是 GoodsCategory/Crop 分类，不是物品名。

## 4. 物资 ID

新增：

- `12 布`（Cloth）；
- `13 皮革`（Leather，配方待补：需要皮/毛皮物资）；
- `14 木炭`（Wood）。

## 5. 配方字段

`GoodsRecipe` 增加：

```csharp
int requiredInnovation;   // 0=无
int requiredBuilding;     // 0=无
float craftDays;          // 0=未配置
CraftProcess process;     // 冶炼/铸造/锻造/热处理/纺织/造纸...
string processDetail;     // 范铸/失蜡/自由锻...
```

新增门禁重载：

```csharp
TryCraft(market, output, out produced, InnovationTree innovations, int realmId, Func<int,bool> hasBuilding)
```

- 新重载强制校验 `requiredInnovation/requiredBuilding`；
- 旧三参重载保持“只校验输入/容量”，供旧调用/测试使用；
- 铁配方示例：`requiredInnovation=202`，`requiredBuilding=200`（铁匠铺）。

## 6. 规则

- 配方输入必须存在、数量 > 0、不得自引用；
- 输出唯一（一个输出一条配方）；
- 多输入配方按 `inputs` 列表；
- 容量不足/输入不足时 `TryCraft` 返回 false；
- 配方可模组化：Base 后加载 Mods 的同路径 `Goods/Recipes.json`，同输出覆盖。

## 7. 待补

- 皮革/毛皮链：生皮/毛皮/皮革已加，毛皮来源（狩猎）待接；
- 陶器/玻璃/石灰等矿物加工；
- 工具/武器/盔甲的物资口径与 EquipmentDef 的对应；
- 配方可选字段：所需设施、所需革新、工时（当前 `GoodsRecipe` 只有输入/输出）。
