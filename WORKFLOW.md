# Team Workflow（本项目）

## 角色
- **Lead**：读代码 → 定接口 → 实现 → 编译/测试 → 汇总。
- **Code Auditor（只读 subagent）**：独立复审 diff 与现有系统一致性；不改文件、不跑 Unity batchmode。
- **Verifier（可选）**：在 Lead 测试完成后复核结果文件与回归基线；不并行跑 Unity。

## 硬性门禁（每批）
1. **读**：`codebase-first` 技能流程；写“现有事实”表（文件:行）。
2. **定**：先写接口/迁移映射，再写代码；命名先定义。
3. **改**：小步补丁；不新增平行系统，接现有创建/存档/UI 入口。
4. **编译**：`E:\TEMP\mc-compile.ps1 -LogName <name>`，必须 `COMPILE_OK`。
5. **测试**：新增测试 → `E:\TEMP\mc-test-editmode.ps1`；对比基线，不允许新增失败。
6. **审计**：挂起只读 subagent 复审；Lead 处理结论后才能报完成。
7. **记录**：更新技能参考/文档；未提交状态明确告知。

## 当前基线
- 全量 EditMode：以最近一次 `Logs/editmode-results.xml` 为准；**2026-11-05 + 第七十九轮（军事法维度审计/边境管制/兜底字段；656/656）：EditMode 656/656、PlayMode 9/9 通过**。
- Play Mode：`Logs/playmode-results.xml`（-nographics，无像素断言）与 `Logs/playmode-gfx-results.xml`（有图形设备，真像素断言：unique 颜色 >8、亮度层次 >0.02、非背景 >15%）**均 1/1 PASS**（有图形设备那条会在截图前强制地形模式，并断言颜色多样性、亮度层次、平均饱和度 >0.10、平面未被缩放、相机中心射线落在网格中心 ±8%）。改动场景/渲染/UI 后必须重跑两条。
- 当前总计划：`docs/architecture/UNIFIED_MASTER_PLAN.md`；架构/文件规则：`docs/architecture/ARCHITECTURE_RULES.md`；旧规划备份：`docs/archive/2026-11-03_pre-unified-plan-backup/`（zip 同目录）。所有新改动先按规则定位 owner 再写代码。
- SaveVersion：16（v12 省份/聚落/营地/行政区划树；v13 军队实体 + 自增 ID 续接；v14 革新/科技树；v15 信仰运行态 + 角色纽带；v16 randomSeed）。
- 程序集：Core ← World ← Simulation；World 不得引用 Simulation。**旧单块程序集 `CivilizationEvolution.Runtime` 已删除（Round 11 B 线第二步 b：20 旧目录 + 旧 asmdef + `_Recovery`，备份 `D:/TEMP/legacy-runtime-backup.tar.gz`）；删除后编译 + 全量测试全绿。**
- 场景：`Assets/Scenes/Main.unity` 已从 HEAD 恢复并重绑 4 个脚本 GUID（Bootstrap/GameManager/GameWorld/MapRenderer）；恢复时被 `_Recovery/0.unity` 覆盖的版本留在备份里，不要再回退到它。
- 注意：`JsonUtility.FromJson` **不认** `[FormerlySerializedAs]`，旧键迁移必须走 `SaveJsonMigration` 这类显式入口。
- 注意：恢复过程重生成过 `.meta` GUID，凡是旧引用（场景/asset/TMP Settings）都要按"当前 meta GUID"重新核对；TMP Settings 的 linebreaking 两处引用已修。

## 命名约定
- 点组：`SiteMode`（定居点组/据点组/营地组），组决定锚点代码分支。
- 锚点：内部空间/关系节点，不对外显示该词。
- 关系：结构事实（领属/附庸/宗主/占领/委任/建制/雇佣/指挥/驻扎/空间从属/行政区划/朝贡）。
- 契约：协议（特许状/雇佣合同/贸易协定/同盟条约/纳贡协议），与关系分离。
- 机制：过程（武装殖民等），不是关系名。
