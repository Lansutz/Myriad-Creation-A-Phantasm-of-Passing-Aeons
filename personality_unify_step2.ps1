# 人格统一化 Step2：派生特质系统（MPD 核心）+ 有效属性统一出口
$ErrorActionPreference = "Stop"
$path = "D:\Myriad-Creation-A-Phantasm-of-Passing-Aeons\CivilizationEvolution\Assets\Scripts\Role\CharacterSystem.cs"
$content = [System.IO.File]::ReadAllText($path) -replace "`r`n","`n"
function N($s){ $s -replace "`r`n","`n" }
$pairs = @()

# ── 替换A：PersonalityDimensions 辅助类后插入派生特质定义表 + DerivedTraitInstance ──
# 锚点：PersonalityDimensions 类的结束 } 后、CharacterData 类注释前
$pairs += [pscustomobject]@{ name="derived_trait_defs"; old=N @'
        /// <summary>字符串键解析为枚举（容错：无法识别返回 false，供数据驱动/事件入口使用）</summary>
        public static bool TryParse(string key, out PersonalityDimension dim)
        {
            if (!string.IsNullOrEmpty(key))
            {
                foreach (var d in All)
                    if (string.Equals(d.Key(), key, StringComparison.OrdinalIgnoreCase))
                    { dim = d; return true; }
            }
            dim = default;
            return false;
        }
    }

    /// <summary>
    /// 角色核心数值
'@; new=N @'
        /// <summary>字符串键解析为枚举（容错：无法识别返回 false，供数据驱动/事件入口使用）</summary>
        public static bool TryParse(string key, out PersonalityDimension dim)
        {
            if (!string.IsNullOrEmpty(key))
            {
                foreach (var d in All)
                    if (string.Equals(d.Key(), key, StringComparison.OrdinalIgnoreCase))
                    { dim = d; return true; }
            }
            dim = default;
            return false;
        }
    }

    /// <summary>
    /// 派生性格特质定义（参考 CK3 More Personality Depth：离散特质是连续人格轴的表现标签）。
    /// 七维达到阈值（|v|&gt;=15）后派生对应极的特质，等级=强度档（轻度/中度/重度=Mild/Normal/Intense）。
    /// 派生特质为只读表现层：用于 UI 性格标签、好感 same/opposite、AI 人格原型判定；
    /// 不直接加属性修正——属性修正统一由已获得的 PersonalityTrait（事件/文化/教育特质）与精神疾病提供，
    /// 走 CharacterData.GetEffectiveStats，避免"连续轴与离散特质两套并存互相矛盾"。
    /// 命名对齐 MPD 36 特质中的七维两极（勇敢/怯懦、慈悲/冷酷、贪婪/慷慨、诚实/狡诈、冷静/暴怒、报复/宽恕、虔诚/愤世）。
    /// </summary>
    public class DerivedTraitInfo
    {
        public string traitId;          // 特质键（与 MPD 命名对齐，如 brave / craven）
        public string displayName;      // 中文名
        public PersonalityDimension dim;// 所属人格维度
        public int sign;                // +1 正极 / -1 负极
        public string oppositeId;       // 对立特质键（same/opposite 好感用）

        public DerivedTraitInfo(string traitId, string displayName,
            PersonalityDimension dim, int sign, string oppositeId)
        {
            this.traitId = traitId; this.displayName = displayName;
            this.dim = dim; this.sign = sign; this.oppositeId = oppositeId;
        }
    }

    /// <summary>派生特质实例（定义 + 当前强度等级 1~3；0 表示无倾向不派生）</summary>
    public struct DerivedTraitInstance
    {
        public DerivedTraitInfo info;
        public int tier;   // 1 轻度 / 2 中度 / 3 重度（对齐 MPD L1/L2/L3）
        public DerivedTraitInstance(DerivedTraitInfo info, int tier) { this.info = info; this.tier = tier; }
    }

    /// <summary>
    /// 派生特质定义表（七维两极共 14 个；新增维度只需在此与 PersonalityDimensions.All 同步）。
    /// 阈值：|v|&gt;=15 派生，等级由 GetPersonalityTier 决定（15-35 轻度 / 35-65 中度 / &gt;65 重度）。
    /// </summary>
    public static class DerivedPersonalityTraits
    {
        public static readonly DerivedTraitInfo[] All =
        {
            // Boldness 大胆：怯懦 ↔ 勇敢
            new DerivedTraitInfo("craven",       "怯懦", PersonalityDimension.Boldness,     -1, "brave"),
            new DerivedTraitInfo("brave",        "勇敢", PersonalityDimension.Boldness,     +1, "craven"),
            // Compassion 悲悯：冷酷 ↔ 慈悲
            new DerivedTraitInfo("callous",      "冷酷", PersonalityDimension.Compassion,   -1, "compassionate"),
            new DerivedTraitInfo("compassionate","慈悲", PersonalityDimension.Compassion,   +1, "callous"),
            // Greed 贪婪：慷慨 ↔ 贪婪
            new DerivedTraitInfo("generous",     "慷慨", PersonalityDimension.Greed,        -1, "greedy"),
            new DerivedTraitInfo("greedy",       "贪婪", PersonalityDimension.Greed,        +1, "generous"),
            // Honor 荣誉：狡诈 ↔ 诚实
            new DerivedTraitInfo("deceitful",    "狡诈", PersonalityDimension.Honor,        -1, "honest"),
            new DerivedTraitInfo("honest",       "诚实", PersonalityDimension.Honor,        +1, "deceitful"),
            // Rationality 理性：暴怒 ↔ 冷静
            new DerivedTraitInfo("wrathful",     "暴怒", PersonalityDimension.Rationality,  -1, "calm"),
            new DerivedTraitInfo("calm",         "冷静", PersonalityDimension.Rationality,  +1, "wrathful"),
            // Vengefulness 报复：宽恕 ↔ 睚眦必报
            new DerivedTraitInfo("forgiving",    "宽恕", PersonalityDimension.Vengefulness, -1, "vengeful"),
            new DerivedTraitInfo("vengeful",     "报复", PersonalityDimension.Vengefulness, +1, "forgiving"),
            // Piety 虔信：愤世 ↔ 虔诚
            new DerivedTraitInfo("cynical",      "愤世", PersonalityDimension.Piety,        -1, "zealous"),
            new DerivedTraitInfo("zealous",      "虔诚", PersonalityDimension.Piety,        +1, "cynical"),
        };

        /// <summary>按维度+符号取派生特质定义（无则 null）</summary>
        public static DerivedTraitInfo Get(PersonalityDimension dim, int sign)
        {
            foreach (var t in All)
                if (t.dim == dim && t.sign == sign) return t;
            return null;
        }
    }

    /// <summary>
    /// 角色核心数值
'@ }

# ── 替换B：CharacterData 内 GetPersonalityDescription 后新增 GetDerivedPersonalityTraits ──
$pairs += [pscustomobject]@{ name="get_derived_traits"; old=N @'
            string t1 = DescribeDimension(top1.dim, top1.value);
            string t2 = DescribeDimension(top2.dim, top2.value);
            return $"为人{t1}，行事{t2}。";
        }
'@; new=N @'
            string t1 = DescribeDimension(top1.dim, top1.value);
            string t2 = DescribeDimension(top2.dim, top2.value);
            return $"为人{t1}，行事{t2}。";
        }

        /// <summary>
        /// 当前派生性格特质列表（七维阈值→离散特质+等级，参考 MPD）。
        /// 只读表现层：每维 |v|&gt;=15 派生对应极的特质，等级=强度档（1轻度/2中度/3重度）；
        /// 用于 UI 性格标签、好感 same/opposite、AI 人格原型。属性修正不在这里，走 GetEffectiveStats。
        /// </summary>
        public List<DerivedTraitInstance> GetDerivedPersonalityTraits()
        {
            var result = new List<DerivedTraitInstance>();
            foreach (var dim in PersonalityDimensions.All)
            {
                float v = GetPersonalityValue(dim);
                if (Mathf.Abs(v) < 15f) continue;
                var info = DerivedPersonalityTraits.Get(dim, v > 0f ? +1 : -1);
                if (info != null)
                    result.Add(new DerivedTraitInstance(info, GetPersonalityTier(dim)));
            }
            return result;
        }

        /// <summary>派生特质的显示文本（如"勇敢·重度  慈悲·中度"），无倾向返回"性情平和"</summary>
        public string GetDerivedTraitsDisplay()
        {
            var traits = GetDerivedPersonalityTraits();
            if (traits.Count == 0) return "性情平和";
            var parts = new string[traits.Count];
            for (int i = 0; i < traits.Count; i++)
            {
                string tierName = traits[i].tier switch
                { 3 => "重度", 2 => "中度", _ => "轻度" };
                parts[i] = $"{traits[i].info.displayName}·{tierName}";
            }
            return string.Join("  ", parts);
        }
'@ }

# ── 替换C：CalculateCommandAbility 后新增 GetEffectiveStats（统一有效属性出口） ──
$pairs += [pscustomobject]@{ name="get_effective_stats"; old=N @'
        /// <summary>
        /// 计算军事指挥能力（选将/统兵）：以 warfare 军事经略为主导（大兵团组织/战役指挥），
        /// martial 个人勇武、intrigue 谋略、learning 学识为辅——修正旧版误用 martial 主导、
        /// 导致"军事经略"属性不参与选将的矛盾
        /// </summary>
        public float CalculateCommandAbility()
        {
            return warfare * 0.6f + martial * 0.2f + intrigue * 0.1f + learning * 0.1f;
        }
'@; new=N @'
        /// <summary>
        /// 计算军事指挥能力（选将/统兵）：以 warfare 军事经略为主导（大兵团组织/战役指挥），
        /// martial 个人勇武、intrigue 谋略、learning 学识为辅——修正旧版误用 martial 主导、
        /// 导致"军事经略"属性不参与选将的矛盾
        /// </summary>
        public float CalculateCommandAbility()
        {
            return warfare * 0.6f + martial * 0.2f + intrigue * 0.1f + learning * 0.1f;
        }

        /// <summary>
        /// 有效属性（唯一权威出口）：基础六维 + 魅力，依次叠加已获得特质修正、精神疾病修正。
        /// UI 显示、AI 判定、能力计算的"含状态最终值"均应取此结果，避免"基础值/修正值"两套口径；
        /// 也让 PersonalityTrait 上原本空转的 XxxMod 字段真正生效。
        /// 派生特质（七维表现标签）不加属性修正——它是表现层，属性修正只来自已获得特质与疾病。
        /// </summary>
        public void GetEffectiveStats(out float martial, out float diplomacy, out float warfare,
            out float stewardship, out float intrigue, out float learning, out float charm)
        {
            martial = this.martial; diplomacy = this.diplomacy; warfare = this.warfare;
            stewardship = this.stewardship; intrigue = this.intrigue;
            learning = this.learning; charm = this.charm;

            // 已获得特质（事件/文化/教育/身体等 PersonalityTrait）修正——此前空转，此处统一生效
            if (traits != null)
            {
                foreach (var t in traits)
                {
                    martial += t.martialMod; diplomacy += t.diplomacyMod; warfare += t.warfareMod;
                    stewardship += t.stewardshipMod; intrigue += t.intrigueMod;
                    learning += t.learningMod; charm += t.charmMod;
                }
            }

            // 精神疾病修正（注册表定义，失智/抑郁等）
            var disorder = MentalHealthSystem.GetDef(mentalDisorderId);
            if (disorder != null)
            {
                martial += disorder.martialMod; diplomacy += disorder.diplomacyMod;
                warfare += disorder.warfareMod; stewardship += disorder.stewardshipMod;
                intrigue += disorder.intrigueMod; learning += disorder.learningMod;
                charm += disorder.charmMod;
            }
        }
'@ }

# ── 执行 ──
$ok=0; $fail=@()
foreach($p in $pairs){
  $o=N $p.old
  if($content.Contains($o)){ $content=$content.Replace($o,(N $p.new)); Write-Host ("[OK] "+$p.name); $ok++ }
  else { Write-Host ("[FAIL] "+$p.name); $fail+=$p.name }
}
if($fail.Count -gt 0){ Write-Host "`n=== $($fail.Count) 个失败，未写回 ===" -ForegroundColor Red; exit 1 }
$content = $content -replace "`n","`r`n"
[System.IO.File]::WriteAllText($path,$content,(New-Object System.Text.UTF8Encoding $false))
Write-Host "`n=== Step2 全部 $ok 个成功，已写回 ===" -ForegroundColor Green
