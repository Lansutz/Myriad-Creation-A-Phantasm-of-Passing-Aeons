using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Tech;

namespace CivilizationEvolution.Politics
{
    // =====================================================================================
    // 政体变迁动力学（Regime Change Dynamics）
    // -------------------------------------------------------------------------------------
    // 理论：历史制度主义（路径依赖+关键节点）+ 唯物史观（上层建筑随经济基础变化，但需偶然事件）
    //       + 斯考切波（旧国家崩溃/精英分裂/底层组织三条件）+ 蒂利（战争-财政制造国家）。
    //
    // 核心命题：**条件具备 ≠ 必然变化**。
    //   ①结构条件：革新/阶层/思想只定义"哪些政体成分可能"（PolityComponentInnovations 硬约束）；
    //   ②结构性张力：现政体与变化了的社会基础错配，只缓慢积累、只提高危机概率，永不自动触发变革；
    //   ③关键节点：继承危机/战败/财政破产/精英分裂/起义/征服/强势改革者等**偶然事件**打开短暂窗口；
    //   ④节点博弈：派系力量对比 × 革新可行性 → 改革/妥协/复辟/停滞/崩溃，结果非决定论。
    //   窗口短暂，结束后回到路径依赖（制度自我强化）。
    // =====================================================================================

    /// <summary>关键节点类型（打开制度流动窗口的偶然事件）</summary>
    public enum CriticalJunctureType
    {
        SuccessionCrisis,   // 继承危机：绝嗣/幼主/继位争议
        WarDefeat,          // 战争失败：主力被歼/兵临首都，暴露国家无能（斯考切波/蒂利）
        FiscalCollapse,     // 财政破产：国库枯竭、国家停摆
        EliteSplit,         // 精英分裂：统治集团内斗、保守阵营凝聚力崩溃
        PopularUprising,    // 民众起义：最受压迫阶层组织化起事
        ForeignConquest,    // 外部征服：大片领土被占，强加或倒逼制度
        StrongReformer      // 强势改革者：高能力统治者借改革派上位
    }

    /// <summary>节点博弈结果</summary>
    public enum JunctureOutcomeType
    {
        None,           // 未发生
        Reform,         // 改革：胜派推动一个或多个政体成分改变
        Compromise,     // 妥协：力量接近，仅改最容易的一维
        Reaction,       // 反扑/复辟：保守派胜出，甚至强化旧制
        Stalemate,      // 停滞：窗口关闭，什么都没变（超稳定结构）
        Collapse        // 崩溃：各方失控，稳定度暴跌、内战风险（交由战争/叛乱系统）
    }

    /// <summary>结构性张力（中时段缓慢积累，不直接触发变革）</summary>
    [Serializable]
    public class StructuralTension
    {
        [Range(0f, 100f)] public float classMismatch;       // 阶级错配：壮大的阶层被政体排除
        [Range(0f, 100f)] public float fiscalMilitary;      // 财政-军事压力
        [Range(0f, 100f)] public float legitimacyErosion;   // 合法性侵蚀
        [Range(0f, 100f)] public float total;               // 综合张力

        public void Recalculate(RealmSociety society, RealmSituation sit, RealmData realm)
        {
            // 阶级错配：各阶层影响力 × 被政治通道排除程度（新兴自由民权重最高）
            float mismatch = 0f, wsum = 0f;
            foreach (var kv in society.classes)
            {
                var p = kv.Value;
                float access = sit.GetPoliticalAccess(kv.Key);
                float excluded = 1f - Mathf.Clamp01(access);
                float w = p.influence;
                mismatch += p.influence * excluded * (1f + (100f - p.satisfaction) / 200f);
                wsum += w;
            }
            classMismatch = wsum > 0f ? Mathf.Clamp(mismatch / wsum * 100f, 0f, 100f) : 0f;

            // 财政-军事压力：战争（尤其本土）+ 国库空虚 + 高税负痛苦
            float fiscal = 0f;
            if (sit.atWar) fiscal += 25f;
            if (sit.warOnHomeSoil) fiscal += 30f;
            fiscal += Mathf.Clamp(-realm.treasury / 20f, 0f, 30f); // 国库越负压力越大
            float avgTaxPain = 0f; int tc = 0;
            foreach (var v in sit.taxPain.Values) { avgTaxPain += v; tc++; }
            if (tc > 0) fiscal += (avgTaxPain / tc) * 0.15f;
            fiscalMilitary = Mathf.Clamp(fiscal, 0f, 100f);

            // 合法性侵蚀
            legitimacyErosion = Mathf.Clamp((100f - sit.legitimacy) * 0.6f + (100f - sit.stability) * 0.4f, 0f, 100f);

            total = Mathf.Clamp(classMismatch * 0.4f + fiscalMilitary * 0.3f + legitimacyErosion * 0.3f, 0f, 100f);
        }
    }

    /// <summary>进行中的关键节点（窗口）</summary>
    [Serializable]
    public class ActiveJuncture
    {
        public CriticalJunctureType type;
        public int startDay;
        public int remainingDays;      // 窗口剩余（窗口短暂）
        [Range(0f, 100f)] public float severity;
        public bool resolved;
        public JunctureOutcomeType outcome = JunctureOutcomeType.None;
        public string note = "";
    }

    /// <summary>单政权的政体变迁运行时状态</summary>
    [Serializable]
    public class RegimeChangeState
    {
        public int realmId;
        public StructuralTension tension = new StructuralTension();
        public ActiveJuncture activeJuncture;           // null=路径依赖期
        public int compositionEstablishedDay;           // 现政体确立日（路径依赖黏性）
        [Range(0f, 100f)] public float institutionalInertia; // 制度黏性（存续越久越难撼动）
        public List<string> history = new List<string>();// 变迁摘要（调试/编年史）
        public bool IsWindowOpen => activeJuncture != null && !activeJuncture.resolved;
    }

    /// <summary>
    /// 政体变迁动力学主体。每政权一份 RegimeChangeState；
    /// 由 GameWorld 在政治 Tick 调用 Tick，并在具体事件（继位/战败/叛乱）发生时调用 NotifyEvent。
    /// </summary>
    public class RegimeChangeDynamics
    {
        private readonly Dictionary<int, RegimeChangeState> _states = new Dictionary<int, RegimeChangeState>();
        private InnovationTree _innovations;
        private Chronicle _chronicle;

        // —— 调参常量 ——
        const int WindowDays = 120;                 // 关键节点窗口长度（约 4 个月，相对长期历史是短暂的）
        const float OpenSeverityBase = 45f;         // 打开窗口所需基础事件烈度
        const float InertiaPerYear = 1.2f;          // 每年累积制度黏性
        const float InertiaMax = 40f;
        const float AutoUprisingTension = 65f;      // 自动检测起义的张力门槛
        const float AutoFiscalTreasury = -200f;     // 财政破产门槛

        public RegimeChangeDynamics(InnovationTree innovations = null, Chronicle chronicle = null)
        {
            _innovations = innovations;
            _chronicle = chronicle;
        }

        public void SetInnovationTree(InnovationTree t) => _innovations = t;
        public RegimeChangeState GetState(int realmId) => _states.TryGetValue(realmId, out var s) ? s : null;

        RegimeChangeState Ensure(int realmId, int day)
        {
            if (!_states.TryGetValue(realmId, out var s))
            {
                s = new RegimeChangeState { realmId = realmId, compositionEstablishedDay = day };
                _states[realmId] = s;
            }
            return s;
        }

        /// <summary>现政体已存续年数（路径依赖）</summary>
        public float GetRegimeAgeYears(int realmId, int currentDay)
        {
            var s = GetState(realmId);
            return s == null ? 0f : Mathf.Max(0f, currentDay - s.compositionEstablishedDay) / 365f;
        }

        /// <summary>
        /// 主 Tick：更新张力与黏性；路径依赖期自动检测临界条件；窗口期倒计时并在到期时博弈解决。
        /// </summary>
        public void Tick(int currentDay, RealmData realm, RealmSociety society,
            RealmSituation sit, FactionManager factions)
        {
            var state = Ensure(realm.realmId, currentDay);

            // 1) 张力重算（每 Tick 反映最新社会基础，但只积累/呈现，不直接变革）
            state.tension.Recalculate(society, sit, realm);

            // 2) 制度黏性随存续年数增长（路径依赖自我强化）
            float ageYears = Mathf.Max(0f, currentDay - state.compositionEstablishedDay) / 365f;
            state.institutionalInertia = Mathf.Clamp(ageYears * InertiaPerYear, 0f, InertiaMax);

            // 3) 窗口期：倒计时 → 到期博弈
            if (state.IsWindowOpen)
            {
                state.activeJuncture.remainingDays--;
                if (state.activeJuncture.remainingDays <= 0)
                    ResolveJuncture(currentDay, realm, society, sit, factions, state);
                return;
            }

            // 4) 路径依赖期：自动检测可打开窗口的临界条件（偶然事件的内生识别）
            DetectAutoJuncture(currentDay, realm, society, sit, factions, state);
        }

        // ===== 自动关键节点检测（外部也可用 NotifyEvent 主动注入）=====
        private void DetectAutoJuncture(int day, RealmData realm, RealmSociety society,
            RealmSituation sit, FactionManager factions, RegimeChangeState state)
        {
            // 财政破产
            if (realm.treasury < AutoFiscalTreasury && state.tension.fiscalMilitary > 50f)
            { TryOpen(day, state, CriticalJunctureType.FiscalCollapse, 60f); return; }

            // 本土战争失败倾向（兵临境内 + 低稳定）
            if (sit.warOnHomeSoil && realm.stability < 30f)
            { TryOpen(day, state, CriticalJunctureType.WarDefeat, 55f + (30f - realm.stability) * 0.5f); return; }

            // 民众起义：整体动荡超阈 + 最不安分阶层能量强
            var restless = society.Get(society.mostRestlessClass);
            if (society.unrestScore > AutoUprisingTension && restless != null && restless.unrest > 25f)
            { TryOpen(day, state, CriticalJunctureType.PopularUprising, society.unrestScore * 0.8f); return; }

            // 精英分裂：要求变革与维持现状的派系都很强且高度极化（接近 50:50）
            factions.GetChangeVsStatusQuo(realm.realmId, out float change, out float sq);
            if (change > 30f && sq > 30f && Mathf.Abs(change - sq) < 8f && state.tension.total > 55f)
            { TryOpen(day, state, CriticalJunctureType.EliteSplit, 50f + state.tension.total * 0.2f); }
        }

        /// <summary>
        /// 外部事件注入（继位/军事惨败/被征服/改革者上台等由对应系统调用）。
        /// 返回是否真的打开了窗口——低张力社会中事件会被现有制度吸收（窗口不开）。
        /// </summary>
        public bool NotifyEvent(int currentDay, int realmId, CriticalJunctureType type, float rawSeverity)
        {
            var state = Ensure(realmId, currentDay);
            if (state.IsWindowOpen) return false; // 已有窗口
            return TryOpen(currentDay, state, type, rawSeverity);
        }

        /// <summary>尝试打开窗口：事件烈度必须盖过"基础阈值 + 制度黏性"，否则被制度吸收</summary>
        private bool TryOpen(int day, RegimeChangeState state, CriticalJunctureType type, float severity)
        {
            float threshold = OpenSeverityBase + state.institutionalInertia;
            if (severity < threshold) return false; // 事件被路径依赖的制度韧性吸收

            state.activeJuncture = new ActiveJuncture
            {
                type = type,
                startDay = day,
                remainingDays = WindowDays,
                severity = severity
            };
            state.history.Add($"第{day}日 关键节点开启：{TypeName(type)}（烈度{severity:F0}）");
            _chronicle?.Add("juncture_open", $"{TypeName(type)}引发制度危机窗口", major: true, state.realmId);
            return true;
        }

        // ===== 节点博弈：派系力量 × 革新可行性 → 非决定论结果 =====
        private void ResolveJuncture(int day, RealmData realm, RealmSociety society,
            RealmSituation sit, FactionManager factions, RegimeChangeState state)
        {
            var j = state.activeJuncture;
            var factionsList = factions.GetFactions(realm.realmId);

            float reformPower = 0f, radicalPower = 0f, reactionPower = 0f, conservativePower = 0f;
            foreach (var f in factionsList)
            {
                switch (f.stance)
                {
                    case FactionStance.Reformist: reformPower += f.power; break;
                    case FactionStance.Radical: radicalPower += f.power; break;
                    case FactionStance.Reactionary: reactionPower += f.power; break;
                    case FactionStance.Conservative: conservativePower += f.power; break;
                }
            }
            float changePower = reformPower + radicalPower;

            // 崩溃判定：整体动荡极高且各方都无压倒性力量 → 失控
            if (society.unrestScore > 82f && changePower < conservativePower * 1.2f
                && j.type is CriticalJunctureType.PopularUprising or CriticalJunctureType.ForeignConquest)
            {
                Finish(state, j, JunctureOutcomeType.Collapse, "各方失控，国家权威崩溃");
                realm.stability = Mathf.Max(0f, realm.stability - 30f);
                return;
            }

            // 保守/复辟胜出：维持或回摆
            if (conservativePower >= changePower && conservativePower >= reactionPower)
            {
                // 保守派勉强胜出=停滞；强势胜出=反扑（稳定度回升，改革派受压）
                if (conservativePower > changePower * 1.25f)
                {
                    Finish(state, j, JunctureOutcomeType.Reaction, "保守派反扑，旧制强化");
                    realm.stability = Mathf.Clamp(realm.stability + 8f, 0f, 100f);
                }
                else
                {
                    Finish(state, j, JunctureOutcomeType.Stalemate, "僵持不下，窗口关闭而制度未变");
                }
                return;
            }

            // 变革阵营胜出：激进派占优则改动更彻底（多维），改革派占优或势均则妥协（一维）
            bool radicalLed = radicalPower > reformPower;
            int dimensionsToChange = radicalLed ? 2 : 1;
            var winPlatform = SelectWinningPlatform(factionsList, radicalLed);

            int changed = ApplyChangesTowardPlatform(day, realm, winPlatform, dimensionsToChange, state);
            if (changed == 0)
            {
                // 想改但没有革新支撑的可行目标——变革被技术/制度条件卡住（条件约束的真正含义）
                Finish(state, j, JunctureOutcomeType.Stalemate, "变革诉求缺乏支撑革新，无从落地，窗口空转");
                return;
            }
            state.compositionEstablishedDay = day; // 新制度进入新的路径依赖
            var outcome = (changed >= 2 || radicalLed) ? JunctureOutcomeType.Reform : JunctureOutcomeType.Compromise;
            Finish(state, j, outcome, $"按胜派政纲调整 {changed} 个政体维度");
        }

        /// <summary>选取胜派政纲（激进优先则取激进派，否则改革派，缺则用最不安分阶层反推）</summary>
        private FactionPlatform SelectWinningPlatform(IReadOnlyList<Faction> factions, bool radicalLed)
        {
            Faction winner = null; float best = -1f;
            foreach (var f in factions)
            {
                bool want = radicalLed ? f.stance == FactionStance.Radical
                                       : f.stance is FactionStance.Reformist or FactionStance.Radical;
                if (!want) continue;
                if (f.power > best) { best = f.power; winner = f; }
            }
            return winner != null ? winner.platform : new FactionPlatform { openness = 0.5f, centralization = 0f };
        }

        /// <summary>
        /// 按政纲方向，在七维中挑选"现状差距最大且存在革新可行替代"的维度落地改革。
        /// 严格通过 GovernmentReform.Reform（内含 PolityComponentInnovations 可行性检查）。
        /// </summary>
        private int ApplyChangesTowardPlatform(int day, RealmData realm, FactionPlatform platform,
            int maxChanges, RegimeChangeState state)
        {
            var candidates = new List<(PolityComponentInnovations.PolityDimension dim, int target, float gap)>();

            // 开放度 → A1 最高交接（选举系 vs 世袭系）
            AddDimensionCandidate(candidates, realm,
                PolityComponentInnovations.PolityDimension.SupremeSuccession,
                realm.composition.supremeSuccession.primary, platform.openness,
                openPositive: new[] { (int)SupremeSuccession.ElectiveRepresentative, (int)SupremeSuccession.ElectiveDirect, (int)SupremeSuccession.Rotation },
                openNegative: new[] { (int)SupremeSuccession.Hereditary, (int)SupremeSuccession.Divine });

            // 开放度 → B2 中央机构（议会 vs 王庭/长老）
            AddDimensionCandidate(candidates, realm,
                PolityComponentInnovations.PolityDimension.CentralInstitution,
                realm.composition.centralInstitution.primary, platform.openness,
                openPositive: new[] { (int)CentralInstitution.Assembly, (int)CentralInstitution.BureaucraticCore },
                openNegative: new[] { (int)CentralInstitution.Court, (int)CentralInstitution.EldersCouncil });

            // 开放度 → C1 地方交接（选举/考试/城市特许 vs 世袭）
            AddDimensionCandidate(candidates, realm,
                PolityComponentInnovations.PolityDimension.LocalSuccession,
                realm.composition.localSuccession.primary, platform.openness,
                openPositive: new[] { (int)LocalSuccession.Examination, (int)LocalSuccession.Elected, (int)LocalSuccession.CityCharter },
                openNegative: new[] { (int)LocalSuccession.Hereditary });

            // 集权度 → D 央地结构（单一 vs 联邦/邦联）
            AddDimensionCandidate(candidates, realm,
                PolityComponentInnovations.PolityDimension.SpatialStructure,
                realm.composition.spatialStructure.primary, platform.centralization,
                openPositive: new[] { (int)SpatialStructure.Unitary },
                openNegative: new[] { (int)SpatialStructure.Federal, (int)SpatialStructure.Confederal });

            // 集权度 → C2 地方职能（直辖 vs 自治）
            AddDimensionCandidate(candidates, realm,
                PolityComponentInnovations.PolityDimension.LocalScope,
                realm.composition.localScope.primary, platform.centralization,
                openPositive: new[] { (int)LocalScope.None, (int)LocalScope.FiscalJudicial },
                openNegative: new[] { (int)LocalScope.FullAutonomy });

            // 按差距降序，优先改矛盾最深的维度
            candidates.Sort((a, b) => b.gap.CompareTo(a.gap));

            int changed = 0;
            foreach (var cand in candidates)
            {
                if (changed >= maxChanges) break;
                if (cand.target == GetCurrentComponent(realm, cand.dim)) continue;
                // 革新可行性硬检查（不满足则跳过——这是"结构条件约束可能性空间"）
                if (!PolityComponentInnovations.IsComponentAvailable(cand.dim, cand.target, _innovations, realm.realmId))
                    continue;
                if (GovernmentReform.Reform(realm, cand.dim, cand.target, _innovations, _chronicle))
                    changed++;
            }
            return changed;
        }

        /// <summary>构造一个维度的候选目标：按倾向方向选目标，差距=|倾向|（倾向越强越优先）</summary>
        private void AddDimensionCandidate(
            List<(PolityComponentInnovations.PolityDimension, int, float)> list, RealmData realm,
            PolityComponentInnovations.PolityDimension dim, int current, float tendency,
            int[] openPositive, int[] openNegative)
        {
            if (Mathf.Abs(tendency) < 0.15f) return; // 政纲在此维度无明显诉求
            int[] pool = tendency > 0f ? openPositive : openNegative;
            // 候选不选当前值；优先取池内第一个（最典型方向）
            foreach (int t in pool)
            {
                if (t == current) continue;
                list.Add((dim, t, Mathf.Abs(tendency)));
                return;
            }
        }

        private static int GetCurrentComponent(RealmData realm, PolityComponentInnovations.PolityDimension d) => d switch
        {
            PolityComponentInnovations.PolityDimension.SupremeSuccession => realm.composition.supremeSuccession.primary,
            PolityComponentInnovations.PolityDimension.SupremeScope => realm.composition.supremeScope.primary,
            PolityComponentInnovations.PolityDimension.CentralSuccession => realm.composition.centralSuccession.primary,
            PolityComponentInnovations.PolityDimension.CentralInstitution => realm.composition.centralInstitution.primary,
            PolityComponentInnovations.PolityDimension.LocalSuccession => realm.composition.localSuccession.primary,
            PolityComponentInnovations.PolityDimension.LocalScope => realm.composition.localScope.primary,
            PolityComponentInnovations.PolityDimension.SpatialStructure => realm.composition.spatialStructure.primary,
            _ => -1
        };

        private void Finish(RegimeChangeState state, ActiveJuncture j, JunctureOutcomeType outcome, string note)
        {
            j.resolved = true; j.outcome = outcome; j.note = note;
            string line = $"关键节点结束：{TypeName(j.type)} → {OutcomeName(outcome)}（{note}）";
            state.history.Add(line);
            _chronicle?.Add("juncture_resolve", line, major: true, state.realmId);
            state.activeJuncture = null; // 回到路径依赖期
        }

        public static string TypeName(CriticalJunctureType t) => t switch
        {
            CriticalJunctureType.SuccessionCrisis => "继承危机",
            CriticalJunctureType.WarDefeat => "战争失败",
            CriticalJunctureType.FiscalCollapse => "财政破产",
            CriticalJunctureType.EliteSplit => "精英分裂",
            CriticalJunctureType.PopularUprising => "民众起义",
            CriticalJunctureType.ForeignConquest => "外部征服",
            CriticalJunctureType.StrongReformer => "强势改革者",
            _ => "关键节点"
        };
        public static string OutcomeName(JunctureOutcomeType o) => o switch
        {
            JunctureOutcomeType.Reform => "政体改革",
            JunctureOutcomeType.Compromise => "妥协微调",
            JunctureOutcomeType.Reaction => "保守反扑",
            JunctureOutcomeType.Stalemate => "停滞未变",
            JunctureOutcomeType.Collapse => "统治崩溃",
            _ => "无"
        };
    }
}
