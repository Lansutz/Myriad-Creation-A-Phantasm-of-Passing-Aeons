# 海军特殊实体（Navy）

> 2026-11-03。海军不走陆军 `Army`，也不塞进普通 `TransportUnit`；它是独立实体，舰船是它的基本构成。

## 1. 为什么特殊实体

- 陆军的组织单位是团/营/兵种，补给按陆路；
- 海军的基本单位是**舰船**，受港口、风向/桨力、海上补给、撞角/炮战影响；
- 现有 `TransportUnit` 的 `carts`/`boats` 只是运输队附属，不足以承载舰队战。

所以 `Navy` 是独立实体，类似 `Army` 的平级结构：

```text
Realm
 ├─ Army        （陆军）
 ├─ Navy        （海军）
 └─ TransportUnit（运输队）
```

## 2. 数据模型

`MyriadCreation.Simulation.State.Navy`：

| 字段 | 说明 |
|---|---|
| `navyId` | 海军 id |
| `navyName` | 舰队名 |
| `ownerRealmId` | 所属政权 |
| `commanderId` | 提督/指挥官 |
| `homePortTile` | 母港 tile；-1=海上编队 |
| `currentTileIndex` | 当前 tile |
| `shipCounts` | 舰船 EquipmentDef id → 数量 |
| `state` | CombatState（Idle/Marching...） |
| `organization/morale/supply/training` | 组织/士气/补给/训练 |
| `movePath` / `moveTargetTile` / `moveProgress` | 海上移动 |

方法：

- `AddShips(equipmentId, count)`；
- `RemoveShips(equipmentId, count)`；
- `TotalShips`；
- `IsAtSea`；
- `DailyTick(supplyPerShip)`。

## 3. 舰船不是陆军单位

- `shipCounts` 的 key 是 `EquipmentDef` 的船条目（不是 `UnitDef` 兵种）；
- 船的属性来自 `ShipHullForm / ShipPropulsion / RamType / BatteryLayout / boardingPlatform`；
- 舰船 id 的解锁来自 `InnovationDef.unlockRefs`；
- 海军实体只负责编队/指挥/补给/移动，不重复定义船。

## 4. 与据点

- 母港/驻港属于据点，不由海军实体自己造建筑；
- `homePortTile` 指向据点所在 tile；
- 海军在港补员/补给；离港后按 `supplyPerShip` 消耗。

## 5. 后续

1. `Navy` 加入 `GameWorld`/存档 DTO；
2. `NavySystem`：招募、编队、海上移动、拦截、登陆支援；
3. 与 `Army` 的协同：运兵、登陆、炮击支援；
4. 海军 AI：巡逻/封锁/护航。

## 6. 当前状态

- `Navy` 数据类与基础方法已加入；
- `NavyEntityTests` 覆盖增删舰船、在港/在海上、补给消耗；
- `GameWorld.navies` + `CreateNavy` 已接入；`NavySaveData`/`NavyPersistence` 已入档（SaveVersion 17）；海战流程待接。
