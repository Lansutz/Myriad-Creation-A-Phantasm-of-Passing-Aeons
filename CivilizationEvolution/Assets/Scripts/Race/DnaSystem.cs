using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Role;

namespace CivilizationEvolution.Race
{
    /// <summary>
    /// DNA 与遗传系统（种族系统 ↔ 角色系统的交叉层）
    /// 设计文档：《DNA与遗传系统.md》
    /// - 7 固定基因座，每座一对等位基因（显性 A / 隐性 a），AA/Aa/aa 三种组合
    /// - 孟德尔式遗传：父母各随机传递一个等位基因
    /// - 简化生物模拟：不做逐基因演算，计算仅发生在角色创建/生育时，不参与每 Tick
    /// </summary>

    /// <summary>等位基因（显性 A / 隐性 a）</summary>
    public enum Allele
    {
        Dominant,   // 显性 A
        Recessive   // 隐性 a
    }

    /// <summary>基因座（原版 7 基因座，模组可扩展但建议 ≤15）</summary>
    public enum DnaLocus
    {
        Longevity,      // 寿命基因座：基础寿命偏移
        Intelligence,   // 智慧基因座：智慧值偏移
        Martial,        // 勇武基因座：勇武值偏移
        Reformism,      // 变革性基因座：变革性偏移
        Resistance,     // 抗性基因座：综合抗性偏移
        Appearance,     // 外观基因座：肤色/体型/面部（纯外观）
        TalentDefect    // 天赋/缺陷基因座：特殊天赋或隐性遗传病
    }

    /// <summary>一对等位基因（父源 + 母源）</summary>
    [Serializable]
    public struct LocusPair
    {
        public Allele paternal;   // 来自父亲的等位基因
        public Allele maternal;   // 来自母亲的等位基因

        public LocusPair(Allele p, Allele m) { paternal = p; maternal = m; }

        /// <summary>AA 纯合显性</summary>
        public bool IsHomozygousDominant => paternal == Allele.Dominant && maternal == Allele.Dominant;
        /// <summary>Aa 杂合</summary>
        public bool IsHeterozygous => paternal != maternal;
        /// <summary>aa 纯合隐性</summary>
        public bool IsHomozygousRecessive => paternal == Allele.Recessive && maternal == Allele.Recessive;
    }

    /// <summary>
    /// 个体 DNA：7 基因座 + 突变计数 + 近亲系数
    /// 仅有名角色存储；人口块不存个体 DNA（属性分布由种族基因频率+统计决定）
    /// </summary>
    [Serializable]
    public class DnaData
    {
        public Dictionary<DnaLocus, LocusPair> loci = new Dictionary<DnaLocus, LocusPair>();
        public int mutationCount;              // 该个体 DNA 发生突变的基因座数量
        public float inbreedingCoefficient;    // 近亲系数 0~1

        public LocusPair GetLocus(DnaLocus locus)
        {
            return loci.TryGetValue(locus, out var pair) ? pair : new LocusPair(Allele.Dominant, Allele.Dominant);
        }

        public void SetLocus(DnaLocus locus, LocusPair pair) => loci[locus] = pair;
    }

    /// <summary>DNA 表达结果（角色初始属性偏移与先天特征）</summary>
    [Serializable]
    public struct DnaExpression
    {
        public float longevityOffsetYears;   // 寿命偏移（年）
        public float intelligenceOffset;     // 智慧偏移（±15 量级）
        public float martialOffset;          // 勇武偏移（±15 量级）
        public float reformismOffset;        // 变革性偏移（±15 量级）
        public float resistanceOffset;       // 综合抗性偏移（±15 量级）
        public string appearanceTag;         // 外观标签（肤色/体型/面部）
        public string talentId;              // 触发的天赋（空=无）
        public string defectId;              // 触发的遗传病（空=无）
        public bool carriesDefect;           // 隐性携带者（Aa，不发病但可遗传）
    }

    /// <summary>天赋/缺陷定义</summary>
    [Serializable]
    public class TalentDefectDef
    {
        public string id;
        public string name;
        public bool isTalent;     // true=特殊天赋，false=隐性遗传病
        public string stat;       // 影响属性键：learning / martial / lifespan / appearance
        public float amount;      // 修正量
        public string description;
    }

    /// <summary>种族基因座频率（各基因座显性等位基因 A 的频率，0-1）</summary>
    [Serializable]
    public class LocusFrequency
    {
        public DnaLocus locus;
        [Range(0f, 1f)] public float dominantFrequency = 0.5f;
    }

    /// <summary>
    /// DNA 系统核心：生成 / 遗传 / 突变 / 近亲 / 表达
    /// 全部为一次性计算（角色创建、生育时），不参与每 Tick 运算
    /// </summary>
    public static class DnaSystem
    {
        // ===== 概率常量 =====
        public const float MutationChance = 0.0075f;              // 每基因座每代突变概率（文档 0.5%-1% 取中）
        public const float TalentDefectMutationChance = 0.015f;   // 天赋/缺陷基因座突变概率略高
        public const float TalentChanceAA = 0.25f;                // AA 触发特殊天赋概率
        public const float DefectChanceAA = 0.9f;                 // aa 触发遗传病概率（其余 10% 罕见正向突变）
        public const float OffsetRange = 15f;                     // 智慧/勇武/变革性/抗性基准 ±15 偏移量级

        // ===== 原版预设天赋表（模组可扩展） =====
        private static readonly List<TalentDefectDef> _talentDefs = new List<TalentDefectDef>
        {
            new TalentDefectDef { id = "talent_photographic", name = "过目不忘", isTalent = true, stat = "learning", amount = 5f, description = "记忆超群，见闻过目成诵" },
            new TalentDefectDef { id = "talent_divine_strength", name = "神力", isTalent = true, stat = "martial", amount = 5f, description = "天生神力，万夫莫当" },
            new TalentDefectDef { id = "talent_iron_body", name = "铁躯", isTalent = true, stat = "lifespan", amount = 10f, description = "体格强韧，寿数绵长" },
            new TalentDefectDef { id = "talent_keen_mind", name = "慧心", isTalent = true, stat = "learning", amount = 3f, description = "颖悟绝伦，触类旁通" }
        };

        // ===== 原版预设遗传病表（aa 纯合隐性发病） =====
        private static readonly List<TalentDefectDef> _defectDefs = new List<TalentDefectDef>
        {
            new TalentDefectDef { id = "defect_frail", name = "先天体弱", isTalent = false, stat = "lifespan", amount = -15f, description = "胎里带的孱弱，寿元受损" },
            new TalentDefectDef { id = "defect_feeblemind", name = "痴愚", isTalent = false, stat = "learning", amount = -20f, description = "心智蒙昧，难以开化" },
            new TalentDefectDef { id = "defect_hemophilia", name = "血友病", isTalent = false, stat = "lifespan", amount = -10f, description = "凝血障碍，伤病易危" },
            new TalentDefectDef { id = "defect_pale", name = "白化", isTalent = false, stat = "appearance", amount = 0f, description = "肤发无色，畏光避日" }
        };

        // ===== 外观标签池（显性/杂合/隐性的特征组合） =====
        private static readonly string[] _appearanceAA = { "肤色深邃", "体魄强健", "轮廓鲜明", "目光如炬" };
        private static readonly string[] _appearanceAa = { "肤色匀称", "体型中等", "轮廓分明", "眉目清朗" };
        private static readonly string[] _appearanceaa = { "肤色浅淡", "身形纤细", "轮廓柔和", "形容清减" };

        // ===== 查询 =====
        public static IReadOnlyList<TalentDefectDef> GetTalentDefs() => _talentDefs;
        public static IReadOnlyList<TalentDefectDef> GetDefectDefs() => _defectDefs;

        public static TalentDefectDef FindDef(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var t in _talentDefs) if (t.id == id) return t;
            foreach (var d in _defectDefs) if (d.id == id) return d;
            return null;
        }

        // ===== 生成：无父母时按种族基因频率随机 =====
        /// <summary>按种族基因频率随机生成个体 DNA（race 为空时各基因座频率取 0.5）</summary>
        public static DnaData GenerateRandom(RaceData race)
        {
            var dna = new DnaData();
            foreach (DnaLocus locus in Enum.GetValues(typeof(DnaLocus)))
            {
                float freq = race != null ? race.GetLocusAFrequency(locus) : 0.5f;
                var pair = new LocusPair(
                    UnityEngine.Random.value < freq ? Allele.Dominant : Allele.Recessive,
                    UnityEngine.Random.value < freq ? Allele.Dominant : Allele.Recessive);
                dna.SetLocus(locus, pair);
            }
            return dna;
        }

        // ===== 遗传：孟德尔式 + 突变 + 近亲修正 =====
        /// <summary>
        /// 后代 DNA 遗传：
        /// 1. 父母每个基因座随机传递一个等位基因
        /// 2. 每基因座低概率突变（显隐翻转；天赋/缺陷座概率略高）
        /// 3. 近亲系数越高，纯合概率越大（隐性遗传病风险上升）
        /// 父母缺失时按种族基因频率随机补位（混血场景：父方缺失用父种族频率）
        /// </summary>
        public static DnaData Inherit(DnaData father, DnaData mother, RaceData race, float inbreeding)
        {
            var dna = new DnaData { inbreedingCoefficient = Mathf.Clamp01(inbreeding) };
            foreach (DnaLocus locus in Enum.GetValues(typeof(DnaLocus)))
            {
                Allele paternal = father != null ? PickAllele(father.GetLocus(locus)) : RandomAllele(race, locus);
                Allele maternal = mother != null ? PickAllele(mother.GetLocus(locus)) : RandomAllele(race, locus);

                // 近亲修正：以近亲系数概率将一个等位基因复制为另一个（提高纯合度）
                if (dna.inbreedingCoefficient > 0f && UnityEngine.Random.value < dna.inbreedingCoefficient)
                {
                    if (UnityEngine.Random.value < 0.5f) paternal = maternal;
                    else maternal = paternal;
                }

                // 突变
                float mutationChance = locus == DnaLocus.TalentDefect ? TalentDefectMutationChance : MutationChance;
                if (UnityEngine.Random.value < mutationChance)
                {
                    if (UnityEngine.Random.value < 0.5f) paternal = Flip(paternal);
                    else maternal = Flip(maternal);
                    dna.mutationCount++;
                }

                dna.SetLocus(locus, new LocusPair(paternal, maternal));
            }
            return dna;
        }

        // ===== 表达：基因型 → 属性偏移与先天特征 =====
        /// <summary>计算 DNA 在种族基准上的表达（角色出生时一次性计算，终身不变）</summary>
        public static DnaExpression ComputeExpression(DnaData dna, RaceData race)
        {
            var expr = new DnaExpression();
            if (dna == null) return expr;

            // 寿命：偏移叠加在种族寿命区间上（区间半宽 lifespanRangeYears）
            expr.longevityOffsetYears = ComputeOffset(dna, DnaLocus.Longevity, race != null ? race.lifespanRangeYears : 15f);
            // 智慧/勇武/变革性/抗性：种族基准 ±15 量级偏移
            expr.intelligenceOffset = ComputeOffset(dna, DnaLocus.Intelligence, OffsetRange);
            expr.martialOffset = ComputeOffset(dna, DnaLocus.Martial, OffsetRange);
            expr.reformismOffset = ComputeOffset(dna, DnaLocus.Reformism, OffsetRange);
            expr.resistanceOffset = ComputeOffset(dna, DnaLocus.Resistance, OffsetRange);
            // 外观
            expr.appearanceTag = ComputeAppearance(dna);
            // 天赋/缺陷
            ApplyTalentDefect(dna, ref expr);
            return expr;
        }

        /// <summary>
        /// 偏移幅度规则：
        /// AA（纯合显性）= 正向，区间上限的 80%-100%
        /// Aa（杂合）   = 正向，区间上限的 30%-60%
        /// aa（纯合隐性）= 负向，区间下限的 50%-100%
        /// </summary>
        private static float ComputeOffset(DnaData dna, DnaLocus locus, float range)
        {
            var pair = dna.GetLocus(locus);
            if (pair.IsHomozygousDominant) return range * UnityEngine.Random.Range(0.8f, 1.0f);
            if (pair.IsHeterozygous) return range * UnityEngine.Random.Range(0.3f, 0.6f);
            return -range * UnityEngine.Random.Range(0.5f, 1.0f);
        }

        private static string ComputeAppearance(DnaData dna)
        {
            var pair = dna.GetLocus(DnaLocus.Appearance);
            var pool = pair.IsHomozygousDominant ? _appearanceAA
                : pair.IsHeterozygous ? _appearanceAa : _appearanceaa;
            return pool[UnityEngine.Random.Range(0, pool.Length)];
        }

        private static void ApplyTalentDefect(DnaData dna, ref DnaExpression expr)
        {
            var pair = dna.GetLocus(DnaLocus.TalentDefect);
            if (pair.IsHomozygousDominant)
            {
                // AA：有概率触发特殊天赋
                if (UnityEngine.Random.value < TalentChanceAA)
                    expr.talentId = _talentDefs[UnityEngine.Random.Range(0, _talentDefs.Count)].id;
            }
            else if (pair.IsHeterozygous)
            {
                // Aa：隐性携带者，不发病但可遗传
                expr.carriesDefect = true;
            }
            else
            {
                // aa：纯合隐性发病；极低概率罕见正向突变（触发天赋）
                if (UnityEngine.Random.value < DefectChanceAA)
                    expr.defectId = _defectDefs[UnityEngine.Random.Range(0, _defectDefs.Count)].id;
                else
                    expr.talentId = _talentDefs[UnityEngine.Random.Range(0, _talentDefs.Count)].id;
            }
        }

        // ===== 近亲系数 =====
        /// <summary>
        /// 近亲系数（Wright 简化查表，谱系深度 ≤2 代）
        /// 亲子=0.25 / 全同胞=0.25 / 半同胞=0.125 / 叔侄=0.125 / 堂表=0.0625 / 更远=0.03125
        /// 近亲系数越高，隐性基因纯合概率越大，隐性遗传病发病率越高
        /// </summary>
        public static float CalculateInbreeding(CharacterData a, CharacterData b, Dictionary<int, CharacterData> characters)
        {
            if (a == null || b == null || a.characterId == b.characterId) return 0f;

            var ancA = GetAncestry(a, characters);
            var ancB = GetAncestry(b, characters);

            float best = 0f;
            foreach (var kv in ancA)
            {
                if (!ancB.TryGetValue(kv.Key, out int dB)) continue;
                int dA = kv.Value;
                int total = dA + dB;

                float f;
                if (total <= 1) f = 0.25f;                    // 亲子（0,1）
                else if (total == 2)
                {
                    // 同胞：全同胞（同一对父母）0.25，半同胞（仅共享一个父母）0.125
                    bool fullSibling = dA == 1 && dB == 1 && HasSameParents(a, b);
                    f = fullSibling ? 0.25f : 0.125f;
                }
                else if (total == 3) f = 0.125f;              // 叔侄/姑侄
                else if (total == 4) f = 0.0625f;             // 堂表同胞
                else f = 0.03125f;                            // 更远亲缘

                if (f > best) best = f;
            }
            return best;
        }

        /// <summary>收集角色祖先链（自身深度0，父母深度1，祖父母深度2）</summary>
        private static Dictionary<int, int> GetAncestry(CharacterData c, Dictionary<int, CharacterData> characters)
        {
            var result = new Dictionary<int, int>();
            CollectAncestors(c, characters, 0, result);
            return result;
        }

        private static void CollectAncestors(CharacterData c, Dictionary<int, CharacterData> characters, int depth, Dictionary<int, int> result)
        {
            if (c == null || depth > 2 || characters == null) return;

            if (!result.TryGetValue(c.characterId, out int existing) || depth < existing)
                result[c.characterId] = depth;

            if (c.fatherId >= 0 && characters.TryGetValue(c.fatherId, out var father))
                CollectAncestors(father, characters, depth + 1, result);
            if (c.motherId >= 0 && characters.TryGetValue(c.motherId, out var mother))
                CollectAncestors(mother, characters, depth + 1, result);
        }

        private static bool HasSameParents(CharacterData a, CharacterData b)
        {
            return a.fatherId >= 0 && a.fatherId == b.fatherId
                && a.motherId >= 0 && a.motherId == b.motherId;
        }

        // ===== 辅助 =====
        private static Allele PickAllele(LocusPair pair)
        {
            return UnityEngine.Random.value < 0.5f ? pair.paternal : pair.maternal;
        }

        private static Allele RandomAllele(RaceData race, DnaLocus locus)
        {
            float freq = race != null ? race.GetLocusAFrequency(locus) : 0.5f;
            return UnityEngine.Random.value < freq ? Allele.Dominant : Allele.Recessive;
        }

        private static Allele Flip(Allele a)
        {
            return a == Allele.Dominant ? Allele.Recessive : Allele.Dominant;
        }
    }
}
