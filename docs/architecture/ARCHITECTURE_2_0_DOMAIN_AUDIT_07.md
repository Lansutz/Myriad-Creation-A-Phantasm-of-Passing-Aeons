# Architecture 2.0 — Domain Audit 07

## Camp / Settlement 生命周期与 Anchor 边界审计

本轮重点审计 CampData、SettlementEvolution、SettlementControl、SettlementDestruction、LandAbandonment。

## 1. CampData 证明“营地”不是单纯建筑

CampData 同时具有 campId、CampType、tileIndex、ownerRealmId、ownerActorId、population、defense、supplies、morale、establishedDay、permanence、isAbandoned 等状态。

CampType 又包含 Temporary、Military、Bandit、Nomad、Refugee、Custom。

因此 Camp 同时承担地图定位、聚居/驻扎、组织关联、防御补给和生命周期。

架构结论：

**Camp 应属于 SettlementCategory = Camp，而不是另一个完全不同的顶层世界对象。**

但 Camp 必须保留特殊性：可以快速建立/拆除，可以没有 Realm，可以与 MapActor 组织关联，可以由军队/盗匪/游牧/难民等建立，可以拔营，也可以演化。

## 2. ownerActorId 的语义

代码注释明确 ownerActorId 是“所属 MapActor（如游牧部落、土匪帮）”。

因此它不是 Founder，也不是 Settlement Owner。

更准确是：

**MapUnit / Actor Association**

未来应把：
- 政治所有权；
- MapUnit/Actor 组织关联；

分成两个关系。

## 3. Camp 的 tileIndex 是 Anchor 候选

CampManager 以 tileIndex 建立、查询和清除 Camp，并把 tile.campId 作为关联。

未来：

Camp Settlement
→ Anchor
→ MapCell

而不是 MapCell.campId 作为唯一权威。

tile.campId 最终更适合成为空间索引/缓存。

## 4. Camp → Settlement 目前是“新建 Burg + 删除 Camp”

TryEvolveCampToBurg 当前复制 Camp 状态创建新的 BurgData，然后 AbandonCamp 并清除 tile.campId。

这与目标模型有冲突。

更合理的是：

Anchor
→ MapCell

保持不变；

Settlement 的 Category 从：
Camp
→ Burg / Outpost

发生状态转换。

也就是说：

**Anchor 可以保持不变，Settlement 的存在形态发生变化。**

这正好符合“Anchor 负责怎么存在于地图上；Settlement 决定它是什么”。

## 5. 三种 SettlementCategory 应作为三种根本存在形态

当前代码已经有：

- Burg = 定居点
- Outpost = 据点
- Camp = 营地

未来它们应是 Settlement 的 Category，而不是三个互不相关的实体类。

例如：
Camp → Outpost
Camp → Burg

是 Settlement 的生命周期转换，而不是 CampData → BurgData 的硬删除/新建。

## 6. Category / Typology / Level / Political Role 必须分轴

当前 SettlementEvolution 同时修改：
- SettlementType
- SettlementLevel
- SettlementCategory
- BurgType
- wallLevel

这是当前分类混合的核心来源。

建议：

### SettlementCategory
三种根本存在形式：
- Camp
- Outpost
- Burg

### SettlementDefinition / Typology
具体聚居形态与功能：
- Village
- Town
- City
- Fort
- Port
- Fortress
等。

### SettlementLevel
规模/发展阶段。

### Political Role
例如 Capital。

特别注意：

**Capital 不应该因为政治首都身份而自动成为一种聚居点地理类型。**

现有 BurgType.Capital → SettlementType.City 的做法只是历史兼容逻辑，未来应拆出 Political Role。

## 7. SettlementControl 不等于 MapCell Ownership

现有 SettlementControlSystem 的设计明确强调“早期通过城市节点实现控制，不是明确边界；高等级城市控制周围低等级聚落”。

因此至少有三个不同概念：

- MapCell ownerRealmId：政治空间归属；
- Settlement control：聚居点之间的控制/影响关系；
- Realm control：更高层政治关系。

它们不能合并到 Anchor 或 Settlement 的单一 owner 字段。

## 8. LandAbandonment 混合了三件事

AbandonTile 同时：
1. 清除 ownerRealmId / occupyingRealmId；
2. 降低 order / development；
3. 处理人口；
4. 对 Camp 执行 AbandonCamp；
5. 允许 ResettleTile。

所以当前“弃地”同时承担政治、社会经济、人口和 Camp 生命周期。

未来：

**放弃 MapCell ≠ 摧毁 Settlement。**

一个政权可以放弃地块而 Settlement 仍然存在；Settlement 被毁也不必然意味着整个 MapCell 失去政治归属。

## 9. SettlementDestruction 也证明 Settlement 与 MapCell 必须分离

现有摧毁系统会修改 BurgData、生成 Refugee MapActor、修改部分 TileData，并在 Raze 时清除 ownerRealmId。

因此未来应更接近：

Settlement Destroyed
→ Settlement state / Ruin
→ Population effects
→ Structure damage
→ optional political/territorial effects

而不是默认：

Destroy Settlement
→ MapCell becomes unowned

后者只能是某些特定结果。

## 10. BurgGenerator 目前把 Anchor 生成和 Settlement 生成混在一起

BurgGenerator 直接接收 TileData、地图尺寸、Province 和 seed，并生成 BurgData。

未来生成流程更合理的是：

Map / Geography
→ 找到适合建立 Anchor 的位置
→ 创建 Anchor
→ 创建 Settlement
→ 根据 SettlementDefinition 初始化状态

这样生成算法可以使用 Geography Query，但 Settlement 不再成为地图空间对象。

## 11. Anchor 生命周期应独立于 Settlement

Anchor 负责：
- 创建；
- 固定于 MapCell；
- 空间位置；
- 重新关联；
- 销毁或迁移。

Settlement 负责：
- 建立；
- 发展；
- Category/Typology/Level 变化；
- Camp → Outpost/Burg；
- Burg → Ruin；
- 重建；
- 消亡。

因此可以出现：

Anchor → 当前有 Settlement

也可以：

Anchor → Settlement 已毁，但遗址仍存在

甚至：

Anchor → 暂时没有 Settlement

所以 Settlement 的删除不能自动等于 Anchor 的删除。

## 12. 当前生命周期目标

```
                    Anchor
                      │
                      │ fixed on MapCell
                      ▼
                  Settlement
                      │
            ┌─────────┼─────────┐
            ▼         ▼         ▼
          Camp      Outpost     Burg
            │         │          │
            │         │          ├── Typology
            │         │          ├── Level
            │         │          └── Political Roles
            │         │
            └────┬────┘
                 │
           Evolution / Transformation
                 │
                 ▼
          Settlement State Change

Settlement
    ↓ destruction
Ruin / Abandoned Settlement
    ↓ reconstruction
Settlement
```

Anchor 在正常演化过程中保持稳定。

## 13. 下一阶段

继续审计：
1. SettlementTypologySystem：Category / Type / Level / Political Role 分轴；
2. BurgGenerator：Anchor 生成点与 Settlement 生成逻辑；
3. BurgSaveData / MapSaveSystem：保存边界；
4. TileData.campId / world.burgs 等索引字段：区分权威状态与空间缓存；
5. 最后决定 Anchor / Settlement 是否进入正式代码迁移。

当前仍不创建最终框架类。
