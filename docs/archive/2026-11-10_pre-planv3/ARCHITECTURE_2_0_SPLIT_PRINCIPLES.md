# Architecture 2.0：拆分尺度与 CK3 学习原则

## 目标

本项目学习 CK3 的模拟分层经验，但目标不是“比 CK3 拆得更多”，而是在相同复杂度下获得更清晰的责任边界、更好的性能、更高的 Mod 可修改性与更低的维护成本。

## 八个核心平衡维度

1. **性能与优化**：大地图、大量人口、角色、政权、军队运行时保持可控的模拟成本。
2. **修改便捷性 / Moddability**：内容与规则尽可能数据驱动，同时保持类型安全。
3. **逻辑清晰**：状态所有权、读取者、修改者、事件来源明确。
4. **模拟表现力**：系统能够形成可解释的因果链，而不是事件脚本堆叠。
5. **可维护 / 可测试**：领域可以独立理解、测试、替换。
6. **可扩展 / 内容生产效率**：新增内容优先复用定义、规则、Query、Action、Effect，而不是新增一套 System。
7. **可预测 / 可调试**：能够追踪“谁决定、为什么决定、谁执行、改变了什么”。
8. **Simulation Cost 意识**：任何高频逻辑都必须考虑调用次数、对象数量、缓存、Dirty、批处理与调度频率。

## 拆分的硬标准

> 新增抽象必须解决真实工程问题。

至少应明确解决以下之一：状态所有权、依赖方向、性能边界、Mod 扩展、测试隔离、生命周期、复用、调试 / 可追踪性。

如果只是把一段逻辑从 A 转发到 B，再从 B 转发到 C，则属于过度架构，不应继续拆分。

## 概念层 ≠ 必须一一对应代码类

`Entity → State → Context → Query → Condition → Modifier → Intent → Action → Effect → Event → Reaction → Dirty → Derived` 是责任边界模型，不要求每一层都创建独立 System/Class。

值得实体化的边界：`AI → DeclareWarIntent → DeclareWarCommand → Diplomacy/Warfare → WarState → WarDeclaredEvent`。

不值得实体化的边界：如果只是 `Intent → Wrapper → Service → System` 且每层没有独立规则、生命周期或性能边界，应合并。

## AI 的最终定位

AI 负责读取 Query / Context、评价候选 Action、产生 Intent。

AI 不负责直接修改 Treasury、Tile Development、WarState、Innovation 状态，也不负责直接执行外交关系变化。

当前 `AIIntentExecutor` 是迁移期路由器，最终应尽可能成为薄适配层，不能重新演变成第二个 God Object。

## 当前迁移结果

当前 AI 的八类 Intent 已全部进入 Command 边界：
- StartResearch → Innovation
- RaidSettlement → Diplomacy
- DeclareWar → Diplomacy/Warfare
- ImproveEconomy → Economy
- ProposeAlliance → Diplomacy
- SendGift → Diplomacy
- ConsolidateRealm → Politics
- MilitaryBuildUp → Warfare

这一步只迁移既有行为边界，**没有趁机扩大行为范围**。

## 性能原则

高频 Query 应避免重复全量扫描。优先：`Authoritative State → Dirty → Derived Cache → Query`，而不是让 AI、UI、Tooltip、Event 各自实现一套计算。

## 与 CK3 的关系

学习 CK3 的重点是 Domain separation、Definition / runtime state separation、Rule / Effect / Event 的组合、数据驱动内容与大规模模拟的调度意识。

但本项目不追求复制 CK3 内部实现。最终架构应围绕本项目自己的地图、人口、文化、革新、宗教、政权、军队和 Mod 需求进行取舍。
