# ARCHITECTURE 2.0 — README 对照审计规则

> 来源：`main/README.md`
> 用途：约束后续架构审计，防止把设计说明、已实现代码、旧设计、未来路线混为一谈。

## 1. 证据优先级

1. 用户后续明确提出并确认的设计升级
2. 当前 Architecture 2.0 审计结论
3. 当前代码实际行为
4. README 中仍然有效的设计
5. README 中较早、已被后续设计替代的设计
6. README 中明确属于未来计划的内容

README 不作为“当前架构”的唯一真相源。

## 2. 革新系统：明确属于“已升级且有规模调整”的旧设计

README 中的革新系统描述是真实的历史设计基础，但**不能直接作为当前革新系统的完整定义**。

当前已经明确确认：

- 革新系统在 README 之后发生过一次明确的架构/规模升级；
- 这不是简单字段改名或局部修补，而是一次具有实际规模的调整；
- 因此 README 中关于革新分类、结构、推进方式以及与其他系统边界的描述，必须逐项与当前代码及后续设计重新核对；
- README 中仍然有效的核心原则可以保留，例如革新不是简单线性科技树、实践与生产经验的重要性、革新与经济/社会/政治/军事/文化的联动；
- 但 README 中具体的旧模型不能直接恢复为 Architecture 2.0 的目标模型。

### 2.1 特别重要：不能把“README 有描述”误判成“当前模型仍然如此”

README 对革新存在：

- 两级分类；
- 技术/思维/制度/传统；
- 具体技术前置链；
- 个人研究者；
- 实践驱动；
- 物产前置；
- JSON 数据驱动。

这些内容必须逐项标记为：

- 核心原则仍有效；
- 结构已升级；
- 已被后续设计替换；
- 当前代码已经实现；
- 当前代码尚未实现。

不能把整个 README 的革新章节作为一个整体复制到新 Domain Model。

## 3. 革新系统必须与其他领域同步审计

革新系统不能被留到 Map / Settlement / Building 等领域全部结束后再单独处理。

后续审计应采用并行轨道：

- Map / MapCell / Geography
- Anchor / Settlement
- Building / Structure / Manor
- MapUnit
- Network
- GreatProject
- Political / Distribution
- **Innovation**

Innovation 审计至少回答：

1. README 旧模型是什么；
2. 用户后来明确升级了什么；
3. 升级的规模和影响范围是什么；
4. 当前代码实际是什么；
5. 哪些旧类/字段/系统只是兼容层；
6. 哪些概念应进入 Architecture 2.0；
7. 哪些 README 设计必须标记为 superseded；
8. Innovation 与 Economy / Production / Character / Culture / Politics / Plan / Activity 的边界在哪里。

## 4. 当前判断

**Innovation 不是“README 已定义 → 直接搬进 2.0”的领域。**

它属于：

`历史设计 → 明确升级 → 当前重新建模`

因此必须先做领域审计和迁移矩阵，再决定最终 Domain 类型。

## 5. 与其他领域同步

以后每轮审计都采用：

`README 历史设计复核 + 当前代码事实 + 用户后续设计升级 + Domain Boundary + Field Migration`

最终形成统一的 Architecture 2.0 审计基线。

尤其是 Innovation：它已经发生过一次规模足够大的架构升级，不能再按 README 原始版本理解。

---

## 6. 当前结论

本文件原有的 Anchor / Settlement / MapActor / Trade / Plan / GreatProject 对照规则继续有效；本次新增的 Innovation 升级状态是同等优先级的审计约束。

**以后不能因为 README 写得详细，就默认该部分没有被后续设计升级。**
