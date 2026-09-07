using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Economy;
using CivilizationEvolution.Politics;
using CivilizationEvolution.Race;
using CivilizationEvolution.Tech;
using CivilizationEvolution.Thought;

namespace CivilizationEvolution.Role
{
    /// <summary>
    /// 人格七维（企划书 9.3：-100~100，家族遗传基线，压力&gt;60 漂移翻倍）。
    /// 定位：底层人格倾向 / AI 行为参数（参考 CK3 ai_boldness / ai_greed / ai_compassion
    /// / ai_zeal / ai_energy / ai_sociability / ai_honor 的连续值路径）；玩家可见的离散
    /// 性格七维映射为五级标签（强负/负/中性/正/强正，每级名词+形容词+行为描述，对齐 CK3 ai_personality_l_*.yml）；统治者额外推导经济原型（见 DetermineEconomicalArchetype）。性格管行为，特质（PersonalityTrait）管能力，二者分离。
    /// 统一枚举：取代此前散落于初始化/漂移/亲和/描述/事件各处的 "boldness" 魔法字符串。
    /// </summary>


    /// <summary>人格七维元数据（唯一权威顺序表 / 字符串键 / 中文名；新增维度只需改此处）</summary>

    /// <summary>
    /// 统治者经济原型（对齐 CK3 economical_archetype：由性格七维组合推导的互斥行为原型，决定 AI 预算与战略倾向）。
    /// CK3 原版 6 原型 + Balanced；扩展 4 原型（Administrator/Schemer/CulturalPatron/GodlessReformer）。
    /// 推导优先级见 CharacterData.DetermineEconomicalArchetype。
    /// </summary>


    /// <summary>经济原型定义（显示名、描述、行为偏置向量）</summary>


    /// <summary>经济原型定义表</summary>


    /// <summary>
    /// 角色核心数值
    /// 普通人口块不存储个体数值，仅有名角色存储完整角色数值
    /// </summary>


    /// <summary>角色身份</summary>


    /// <summary>统治类型（企划书 9.1：威望/恶名组合）</summary>


    /// <summary>
    /// 人格特质
    /// 三层架构：基础特质 → 复合特质 → 文化特质
    /// </summary>


    /// <summary>
    /// 性格标签定义表（对齐 CK3 原版十余个性格特质 × MPD 三级递进）。
    /// 每个标签是独立的 PersonalityTrait，直接显示在人物界面；三级之间用 requiredTraits 表示递进（L1→L2→L3），
    /// 对立特质用 conflictingTraits 互斥。不搞从七维连续值推导标签的运行时计算。
    /// 新增标签只需在此数组追加。
    /// </summary>


    /// <summary>角色间关系</summary>
    [Serializable]
    public struct CharacterRelation
    {
        public int otherCharacterId;
        [UnityEngine.Range(-200f, 200f)] public float opinion;  // 好感度（企划书：-200~200，双向不对称存储）
        public RelationshipType type;
        public List<string> history;

        public float trust;
        public float fear;
        public float romanticAttraction;
    }

    /// <summary>
    /// 角色间结构性关系（客观身份：血缘/婚姻/师承/上下级——由家族、婚姻、任职派生或显式设定）。
    /// 动态情感（朋友/仇敌/恋人等好感状态）不在此枚举：好感高低看 CharacterRelation.opinion 连续值，
    /// 带机制加成的特殊联结看 BondType；二者分层，勿再在此堆叠 Friend/Rival/Lover/Enemy 等情感标签。
    /// </summary>


    /// <summary>
    /// 人物羁绊系统
    /// 角色间的特殊关系纽带，提供机制加成
    /// </summary>


    /// <summary>
    /// 人物羁绊（后天缔结、提供机制加成的特殊联结——区别于 RelationshipType 的客观身份）。
    /// 同一对角色可既有结构关系（如师徒 Mentor/Student）又缔结机制纽带（MentorBond）；
    /// 动态好感程度由 CharacterRelation.opinion 表达，Bond 只承载结下的"纽带"及其加成。
    /// Rivalry（宿怨）与 Nemesis（死敌）为程度不同的敌对纽带，故并存。
    /// </summary>


    /// <summary>
    /// 角色管理器
    /// 管理所有有名角色、家族、羁绊
    /// DNA 系统对接：角色创建时生成/遗传 DNA，表达为初始属性；生育时孟德尔遗传
    /// </summary>
    public class CharacterManager
    {
        /// <summary>种族定义表（由 GameWorld 注入，DNA 表达与混血基准依赖）</summary>
        public Dictionary<int, RaceData> Races { get; set; }

        /// <summary>革新树（由 GameWorld 注入，家族传统解锁前置检查依赖）</summary>
        public InnovationTree Innovations { get; set; }
        /// <summary>经济系统（由 GameWorld 注入，角色饮食联动依赖）</summary>
        public EconomyManager Economy { get; set; }
        /// <summary>地块表（由 GameWorld 注入，角色饮食按政权地块定位贸易中心）</summary>
        public TileData[] Tiles { get; set; }
        /// <summary>政权表（由 GameWorld 注入，角色饮食/领地定位依赖）</summary>
        public Dictionary<int, RealmData> Realms { get; set; }

        private readonly Dictionary<int, CharacterData> _characters = new Dictionary<int, CharacterData>();
        private readonly Dictionary<int, FamilyNode> _families = new Dictionary<int, FamilyNode>();
        private readonly List<CharacterBond> _bonds = new List<CharacterBond>();
        private int _nextCharacterId = 1;
        private int _nextFamilyId = 1;
        private int _nextBondId = 1;

        /// <summary>按 id 解析种族（未注入/未找到返回 null）</summary>
        private RaceData ResolveRace(int raceId)
        {
            if (Races != null && Races.TryGetValue(raceId, out var race)) return race;
            return null;
        }

        /// <summary>创建新角色</summary>
        /// <param name="dna">显式 DNA；null 时若有父母则按孟德尔遗传生成，否则按种族基因频率随机</param>
        /// <param name="fatherId">父角色 id（-1=无）</param>
        /// <param name="motherId">母角色 id（-1=无）</param>
        /// <param name="expressionRace">表达基准种族；null 时用 raceId 对应种族（混血场景传双亲基准平均）</param>
        public CharacterData CreateCharacter(string firstName, string lastName, int age, bool isMale,
            int cultureId, int raceId, int faithId, CharacterRole role,
            DnaData dna = null, int fatherId = -1, int motherId = -1, RaceData expressionRace = null,
            CharacterTemplateDef template = null)
        {
            // ===== DNA：显式传入 > 父母遗传 > 种族随机 =====
            if (dna == null && (fatherId >= 0 || motherId >= 0))
            {
                var father = fatherId >= 0 ? GetCharacter(fatherId) : null;
                var mother = motherId >= 0 ? GetCharacter(motherId) : null;
                if (father != null || mother != null)
                {
                    float inbreeding = DnaSystem.CalculateInbreeding(father, mother, _characters);
                    dna = DnaSystem.Inherit(father?.dna, mother?.dna, ResolveRace(raceId), inbreeding);
                }
            }
            if (dna == null)
                dna = DnaSystem.GenerateRandom(ResolveRace(raceId));

            var character = new CharacterData
            {
                characterId = _nextCharacterId++,
                firstName = firstName,
                lastName = lastName,
                age = age,
                isMale = isMale,
                birthDay = UnityEngine.Random.Range(1, 365),
                birthYear = 0, // 简化
                cultureId = cultureId,
                raceId = raceId,
                faithId = faithId,
                role = role,
                fatherId = fatherId,
                motherId = motherId,
                dna = dna
            };

            // ===== DNA 表达 → 初始属性 =====
            RaceData exprRace = expressionRace ?? ResolveRace(raceId);
            var expr = DnaSystem.ComputeExpression(dna, exprRace);
            character.dnaExpression = expr;
            character.expectedLifespanYears = Mathf.Clamp(
                (exprRace != null ? exprRace.lifespanBaseYears : 75f) + expr.longevityOffsetYears, 20f, 150f);
            // 个体抗性：种族基准 + DNA 偏移（疾病感染修正用；变革性为种族设定，不做个体级）
            character.individualResistance = Mathf.Clamp(
                (exprRace != null ? exprRace.resistanceBaseline : 50f) + expr.resistanceOffset, 0f, 100f);

            if (dna != null)
            {
                // 有 DNA：勇武/学识由种族基准 + DNA 偏移 + 小随机浮动决定，其余四维保持随机
                float martialBase = exprRace != null ? exprRace.martialBaseline : 50f;
                float intelligenceBase = exprRace != null ? exprRace.intelligenceBaseline : 50f;
                character.martial = Mathf.Clamp(martialBase + expr.martialOffset + UnityEngine.Random.Range(-3f, 3f), 5f, 95f);
                character.learning = Mathf.Clamp(intelligenceBase + expr.intelligenceOffset + UnityEngine.Random.Range(-3f, 3f), 5f, 95f);
                character.diplomacy = UnityEngine.Random.Range(20f, 80f);
                character.stewardship = UnityEngine.Random.Range(20f, 80f);
                character.intrigue = UnityEngine.Random.Range(20f, 80f);
                character.warfare = UnityEngine.Random.Range(20f, 80f);
            }
            else
            {
                // 无 DNA（兼容旧路径）：全随机
                character.martial = UnityEngine.Random.Range(20f, 80f);
                character.diplomacy = UnityEngine.Random.Range(20f, 80f);
                character.stewardship = UnityEngine.Random.Range(20f, 80f);
                character.intrigue = UnityEngine.Random.Range(20f, 80f);
                character.learning = UnityEngine.Random.Range(20f, 80f);
                character.warfare = UnityEngine.Random.Range(20f, 80f);
            }

            // 天赋/缺陷叠加
            ApplyTalentDefectEffect(character, expr);

            // ===== 人格七维初始化（企划书 9.3：家族遗传基线 + 随机偏移） =====
            InitializePersonality(character, fatherId, motherId);

            // ===== 魅力初始（DNA 外观微调：AA +5 / Aa +2 / aa -3，±10 随机） =====
            float appearanceBonus = character.dna != null && character.dna.GetLocus(DnaLocus.Appearance).IsHomozygousDominant ? 5f
                : character.dna != null && character.dna.GetLocus(DnaLocus.Appearance).IsHeterozygous ? 2f : -3f;
            character.charm = Mathf.Clamp(50f + appearanceBonus + UnityEngine.Random.Range(-10f, 10f), 0f, 100f);

            // ===== 角色模板套用（第九篇角色生成参数模板：年龄范围/六维约束/人格倾向偏移） =====
            if (template != null)
                ApplyTemplate(character, template);

            // 身份与社会阶层对齐（修复角色阶层与人口系统断裂）
            character.SyncClassFromRole();
            _characters[character.characterId] = character;
            return character;
        }

        /// <summary>
        /// 套用角色模板（第九篇角色生成参数模板）：
        /// - 年龄范围：调用方未指定年龄（age<=0）时在模板范围内随机
        /// - 六维范围约束：statMin/statMax（0 表示不约束，顺序 martial/diplomacy/warfare/stewardship/intrigue/learning）
        /// - 人格倾向偏移：七维 bias 叠加（在家族遗传基线之上）
        /// </summary>
        public void ApplyTemplate(CharacterData c, CharacterTemplateDef template)
        {
            if (c == null || template == null) return;

            if (c.age <= 0)
            {
                int minA = Mathf.Max(0, template.minAge);
                int maxA = Mathf.Max(0, template.maxAge);
                if (maxA > minA && maxA > 0)
                    c.age = UnityEngine.Random.Range(minA, maxA + 1);
                else if (maxA > 0)
                    c.age = maxA;
                else if (minA > 0)
                    c.age = minA;
            }

            float[] stats = { c.martial, c.diplomacy, c.warfare, c.stewardship, c.intrigue, c.learning };
            for (int i = 0; i < 6; i++)
            {
                if (template.statMin != null && i < template.statMin.Length && template.statMin[i] > 0f)
                    stats[i] = Mathf.Max(stats[i], template.statMin[i]);
                if (template.statMax != null && i < template.statMax.Length && template.statMax[i] > 0f)
                    stats[i] = Mathf.Min(stats[i], template.statMax[i]);
            }
            c.martial = stats[0];
            c.diplomacy = stats[1];
            c.warfare = stats[2];
            c.stewardship = stats[3];
            c.intrigue = stats[4];
            c.learning = stats[5];

            // 人格倾向偏移（七维统一叠加，bias 访问走模板的枚举索引器）
            foreach (var pd in PersonalityDimensions.All)
                c.AddPersonality(pd, template.GetPersonalityBias(pd));
        }

        // ===== 人格七维（企划书 9.3：家族遗传基线 + 随机偏移） =====

        /// <summary>人格七维初始化：有父母取双亲平均 ±10（家族遗传基线），无父母围绕 0 随机 ±30</summary>
        private void InitializePersonality(CharacterData c, int fatherId, int motherId)
        {
            var father = fatherId >= 0 ? GetCharacter(fatherId) : null;
            var mother = motherId >= 0 ? GetCharacter(motherId) : null;
            float f = father != null ? 1f : 0f, m = mother != null ? 1f : 0f;
            float n = f + m;

            foreach (var dim in PersonalityDimensions.All)
            {
                if (n > 0f)
                {
                    float baseline = (father != null ? father.GetPersonalityValue(dim) : 0f) * f / n
                                   + (mother != null ? mother.GetPersonalityValue(dim) : 0f) * m / n;
                    c.SetPersonalityValue(dim, baseline + UnityEngine.Random.Range(-10f, 10f));
                }
                else
                {
                    c.SetPersonalityValue(dim, UnityEngine.Random.Range(-30f, 30f));
                }
            }
        }

        // ===== 角色数值机制（饮食/精神疾病） =====

        /// <summary>
        /// 饮食联动（肥胖驱动，企划书上限型数值）：
        /// 每日从角色所属政权核心地块的贸易中心扣 1 单位粮食；
        /// 吃上 → 肥胖按身份增速（贵族/统治者吃得好，体力身份增长慢）；
        /// 缺粮 → 肥胖下降 + 压力上升
        /// </summary>
        private void DailyDiet()
        {
            if (Economy == null || Tiles == null || Realms == null) return;

            foreach (var c in _characters.Values)
            {
                if (!c.isAlive || c.realmId < 0) continue;
                if (!Realms.TryGetValue(c.realmId, out var realm) || realm.coreTiles.Count == 0) continue;

                int firstTile = -1;
                foreach (int t in realm.coreTiles) { firstTile = t; break; }
                if (firstTile < 0 || firstTile >= Tiles.Length) continue;

                int regionId = Tiles[firstTile].regionId;
                var tc = Economy.GetTradeCenter(regionId);
                if (tc != null && tc.RemoveGoods(0, 1f))
                {
                    float gain = c.role switch
                    {
                        CharacterRole.Ruler or CharacterRole.Noble => 0.04f,
                        CharacterRole.Military or CharacterRole.Commoner => 0.015f,
                        _ => 0.025f
                    };
                    c.obesity = Mathf.Clamp(c.obesity + gain, 0f, 100f);
                }
                else
                {
                    c.obesity = Mathf.Max(0f, c.obesity - 0.03f);
                    c.stress = Mathf.Min(100f, c.stress + 2f);
                }
            }
        }

        /// <summary>
        /// 精神疾病触发与缓解（简单版，角色级状态机）：
        /// - 触发：压力>80 持续 90 天 → 抑郁/焦虑；恐惧>80 → 偏执；高龄+低学识 → 失智
        /// - 缓解：压力<30 持续 120 天 → 康复（失智不可逆）
        /// </summary>
        private void CheckMentalDisorders()
        {
            foreach (var c in _characters.Values)
            {
                if (!c.isAlive) continue;

                if (string.IsNullOrEmpty(c.mentalDisorderId))
                {
                    if (c.highStressDays >= MentalHealthSystem.HighStressTriggerDays)
                    {
                        c.mentalDisorderId = UnityEngine.Random.value < 0.6f
                            ? MentalDisorderIds.Depression : MentalDisorderIds.Anxiety;
                        c.highStressDays = 0;
                        Debug.Log($"[Mental] {c.fullName} 罹患{MentalHealthSystem.GetDisorderName(c)}（长期高压）");
                    }
                    else if (c.dread > MentalHealthSystem.DreadParanoiaThreshold && UnityEngine.Random.value < 0.002f)
                    {
                        c.mentalDisorderId = MentalDisorderIds.Paranoia;
                        Debug.Log($"[Mental] {c.fullName} 罹患偏执（深度恐惧）");
                    }
                    else if (c.age >= MentalHealthSystem.DementiaAge
                        && c.learning < MentalHealthSystem.DementiaLearningGate)
                    {
                        float risk = 0.0005f * (c.age - MentalHealthSystem.DementiaAge + 1) / 10f;
                        if (UnityEngine.Random.value < risk)
                        {
                            c.mentalDisorderId = MentalDisorderIds.Dementia;
                            Debug.Log($"[Mental] {c.fullName} 罹患失智（年迈心智衰退）");
                        }
                    }
                }
                else
                {
                    var def = MentalHealthSystem.GetDef(c.mentalDisorderId);
                    if (def == null || !def.reversible) continue;

                    if (c.stress < 30f)
                    {
                        c.lowStressRecoveryDays++;
                        if (c.lowStressRecoveryDays >= MentalHealthSystem.LowStressRecoveryDays)
                        {
                            Debug.Log($"[Mental] {c.fullName} 从{def.GetName()}中康复");
                            c.mentalDisorderId = "";
                            c.lowStressRecoveryDays = 0;
                        }
                    }
                    else
                    {
                        c.lowStressRecoveryDays = Mathf.Max(0, c.lowStressRecoveryDays - 1);
                    }
                }
            }
        }

        // ===== 角色数值公共接口（事件/战争/疾病/AI 调用） =====

        /// <summary>
        /// 人格亲和漂移：已有角色对的关系按七维亲和度缓慢调整
        /// （借鉴 CK3 More Personality Depth 的 same/opposite opinion 机制；
        /// 仅作用于已建立的关系，不主动创建新关系）
        /// </summary>
        private void PersonalityOpinionDrift()
        {
            var chars = GetAliveCharacters();
            for (int i = 0; i < chars.Count; i++)
            {
                for (int j = i + 1; j < chars.Count; j++)
                {
                    var a = chars[i];
                    var b = chars[j];
                    if (!a.relations.TryGetValue(b.characterId, out var rel)) continue;

                    float affinity = a.GetPersonalityAffinity(b);
                    if (Mathf.Abs(affinity) < 0.5f) continue;

                    rel.opinion = Mathf.Clamp(rel.opinion + affinity * 0.002f, -200f, 200f);
                    a.relations[b.characterId] = rel; // struct 回写
                }
            }
        }

        /// <summary>施加压力（战争/缺粮/重大事件）</summary>
        public void AddStress(int characterId, float amount)
        {
            var c = GetCharacter(characterId);
            if (c != null) c.stress = Mathf.Clamp(c.stress + amount, 0f, 100f);
        }

        /// <summary>施加恐惧（处决/暴行/恐怖事件）</summary>
        public void AddDread(int characterId, float amount)
        {
            var c = GetCharacter(characterId);
            if (c != null) c.dread = Mathf.Clamp(c.dread + amount, 0f, 100f);
        }

        /// <summary>人格维度修正（枚举入口，事件驱动漂移）</summary>
        public void ModifyPersonality(int characterId, PersonalityDimension dimension, float delta)
        {
            var c = GetCharacter(characterId);
            c?.AddPersonality(dimension, delta);
        }

        /// <summary>人格维度修正（字符串键重载，事件 JSON 数据驱动用；内部解析到枚举）</summary>
        public void ModifyPersonality(int characterId, string dimension, float delta)
        {
            if (PersonalityDimensions.TryParse(dimension, out var d))
                ModifyPersonality(characterId, d, delta);
        }

        /// <summary>治愈精神疾病（贤者/事件/医学革新；失智不可逆）</summary>
        public bool CureMentalDisorder(int characterId)
        {
            var c = GetCharacter(characterId);
            if (c == null || string.IsNullOrEmpty(c.mentalDisorderId)) return false;
            var def = MentalHealthSystem.GetDef(c.mentalDisorderId);
            if (def != null && !def.reversible) return false;
            Debug.Log($"[Mental] {c.fullName} 经治疗摆脱{def?.GetName() ?? "病痛"}");
            c.mentalDisorderId = "";
            c.highStressDays = 0;
            c.lowStressRecoveryDays = 0;
            return true;
        }

        // ===== 天赋/缺陷应用 =====

        private void ApplyTalentDefectEffect(CharacterData c, DnaExpression expr)
        {
            var def = DnaSystem.FindDef(expr.talentId);
            if (def != null) ApplyDef(c, def);
            def = DnaSystem.FindDef(expr.defectId);
            if (def != null) ApplyDef(c, def);
        }

        private static void ApplyDef(CharacterData c, TalentDefectDef def)
        {
            switch (def.stat)
            {
                case "learning":
                    c.learning = Mathf.Clamp(c.learning + def.amount, 0f, 100f);
                    break;
                case "martial":
                    c.martial = Mathf.Clamp(c.martial + def.amount, 0f, 100f);
                    break;
                case "lifespan":
                    c.expectedLifespanYears = Mathf.Max(20f, c.expectedLifespanYears + def.amount);
                    break;
                case "appearance":
                    c.dnaExpression.appearanceTag += $"（{def.name}）";
                    break;
            }
        }

        // ===== 生育机制（最小实现，DNA 孟德尔遗传入口） =====

        /// <summary>
        /// 生育：父+母 → 孟德尔遗传 DNA → 后代角色
        /// 校验：双方存活、异性、成年（≥16）、非同一人、非直系亲子
        /// 近亲允许（文化/法律层面决策），但近亲系数进入遗传（纯合隐性风险上升）
        /// 混血（父母不同种族）：后代种族取父系，表达基准取双亲种族平均
        /// </summary>
        public CharacterData Procreate(int fatherId, int motherId, int birthYear)
        {
            if (fatherId < 0 || motherId < 0) return null;
            var father = GetCharacter(fatherId);
            var mother = GetCharacter(motherId);
            if (father == null || mother == null) return null;
            if (!father.isAlive || !mother.isAlive) return null;
            if (father.isMale == mother.isMale) return null;
            if (father.characterId == mother.characterId) return null;
            if (father.age < 16 || mother.age < 16) return null;
            // 直系亲子排除
            if (IsDirectLineage(father, mother)) return null;

            // 后代身份：父系传承（最小规则）
            int childRaceId = father.raceId;
            int childCultureId = father.cultureId;
            int childFaithId = father.faithId;

            // 混血：表达基准取双亲种族平均（基因频率仍用父种族）
            var fatherRace = ResolveRace(father.raceId);
            var motherRace = ResolveRace(mother.raceId);
            RaceData expressionRace = fatherRace ?? motherRace;
            if (fatherRace != null && motherRace != null && fatherRace.raceId != motherRace.raceId)
                expressionRace = AverageRaceBaselines(fatherRace, motherRace);

            float inbreeding = DnaSystem.CalculateInbreeding(father, mother, _characters);
            var dna = DnaSystem.Inherit(father.dna, mother.dna, fatherRace ?? motherRace, inbreeding);

            bool isMale = UnityEngine.Random.value < 0.5f;
            string firstName = GenerateName(childCultureId, isMale ? 0 : 1);

            var child = CreateCharacter(firstName, father.lastName, 0, isMale,
                childCultureId, childRaceId, childFaithId, CharacterRole.Commoner,
                dna, fatherId, motherId, expressionRace);

            // 挂入父系家族
            if (father.familyId >= 0)
            {
                child.familyId = father.familyId;
                if (_families.TryGetValue(father.familyId, out var fam))
                    fam.AddMember(child.characterId);
            }

            if (inbreeding > 0.05f)
                Debug.Log($"[Character] {father.fullName} × {mother.fullName} 产子 {child.fullName}（近亲系数 {inbreeding:F3}）");
            return child;
        }

        /// <summary>混血基准：双亲种族基准逐项取平均（返回临时 RaceData，仅用于表达）</summary>
        private static RaceData AverageRaceBaselines(RaceData a, RaceData b)
        {
            return new RaceData
            {
                raceId = -1,
                raceName = $"混血({a.raceName}+{b.raceName})",
                intelligenceBaseline = (a.intelligenceBaseline + b.intelligenceBaseline) * 0.5f,
                martialBaseline = (a.martialBaseline + b.martialBaseline) * 0.5f,
                lifespanBaseYears = (a.lifespanBaseYears + b.lifespanBaseYears) * 0.5f,
                lifespanRangeYears = (a.lifespanRangeYears + b.lifespanRangeYears) * 0.5f,
                resistanceBaseline = (a.resistanceBaseline + b.resistanceBaseline) * 0.5f
            };
        }

        /// <summary>直系血亲（亲子/全同胞）判定</summary>
        private static bool IsDirectLineage(CharacterData a, CharacterData b)
        {
            if (a.fatherId == b.characterId || a.motherId == b.characterId) return true;
            if (b.fatherId == a.characterId || b.motherId == a.characterId) return true;
            // 全同胞（同一对父母）
            return a.fatherId >= 0 && a.fatherId == b.fatherId
                && a.motherId >= 0 && a.motherId == b.motherId;
        }

        /// <summary>统计角色的子女人数</summary>
        public int CountChildren(int characterId)
        {
            int count = 0;
            foreach (var c in _characters.Values)
                if (c.fatherId == characterId || c.motherId == characterId) count++;
            return count;
        }

        /// <summary>名字生成：文化名字池（type: 0男名 1女名 2姓氏），空池回退文化名</summary>
        private static string GenerateName(int cultureId, int type)
        {
            if (ContentRegistry.TryGetCulture(cultureId, out var pack))
                return ContentRegistry.GetRandomName(pack, type);
            return "无名";
        }

        // ===== 婚姻与家族树（2026-09-01：配偶/子女遍历——家族树与生育衔接） =====

        /// <summary>婚姻：双向设置配偶（异性/成年/存活/非直系——与 Procreate 同检查）</summary>
        public bool Marry(int aId, int bId)
        {
            var a = GetCharacter(aId);
            var b = GetCharacter(bId);
            if (a == null || b == null) return false;
            if (!a.isAlive || !b.isAlive) return false;
            if (a.isMale == b.isMale) return false;
            if (a.characterId == b.characterId) return false;
            if (a.age < 16 || b.age < 16) return false;
            if (IsDirectLineage(a, b)) return false;
            if (a.spouseId >= 0 || b.spouseId >= 0) return false; // 已有配偶不重婚

            a.spouseId = b.characterId;
            b.spouseId = a.characterId;
            return true;
        }

        /// <summary>获取角色配偶（无返回 null）</summary>
        public CharacterData GetSpouse(int characterId)
        {
            var c = GetCharacter(characterId);
            return c != null && c.spouseId >= 0 ? GetCharacter(c.spouseId) : null;
        }

        /// <summary>获取子女（父或母=指定角色——反查）</summary>
        public List<CharacterData> GetChildren(int characterId)
        {
            var result = new List<CharacterData>();
            foreach (var c in _characters.Values)
                if (c.fatherId == characterId || c.motherId == characterId)
                    result.Add(c);
            return result;
        }

        /// <summary>获取兄弟姐妹（共享任一父母，排除自身）</summary>
        public List<CharacterData> GetSiblings(int characterId)
        {
            var c = GetCharacter(characterId);
            var result = new List<CharacterData>();
            if (c == null) return result;
            foreach (var other in _characters.Values)
            {
                if (other.characterId == characterId) continue;
                if ((c.fatherId >= 0 && other.fatherId == c.fatherId)
                    || (c.motherId >= 0 && other.motherId == c.motherId))
                    result.Add(other);
            }
            return result;
        }

        /// <summary>获取祖先链（父系优先递归，含父母/祖父母…）</summary>
        public List<CharacterData> GetAncestors(int characterId, int maxDepth = 4)
        {
            var result = new List<CharacterData>();
            var c = GetCharacter(characterId);
            int depth = 0;
            while (c != null && depth < maxDepth)
            {
                var parent = c.fatherId >= 0 ? GetCharacter(c.fatherId) : null;
                if (parent == null && c.motherId >= 0) parent = GetCharacter(c.motherId);
                if (parent == null) break;
                result.Add(parent);
                c = parent;
                depth++;
            }
            return result;
        }

        /// <summary>获取孙辈及以下（子女的子女——深度 2）</summary>
        public List<CharacterData> GetGrandchildren(int characterId)
        {
            var result = new List<CharacterData>();
            foreach (var child in GetChildren(characterId))
                result.AddRange(GetChildren(child.characterId));
            return result;
        }

        /// <summary>家族树文本（分代缩进：配偶/祖辈/本人/子女/孙辈——家族树面板用）</summary>
        public string BuildFamilyTreeText(int characterId)
        {
            var sb = new System.Text.StringBuilder();
            var c = GetCharacter(characterId);
            if (c == null) return "（无角色）";

            sb.AppendLine($"◆ {c.firstName} {c.lastName}（{c.age}岁，{(c.isMale ? "男" : "女")}）");

            // 配偶
            var spouse = GetSpouse(characterId);
            sb.AppendLine(spouse != null ? $"  配偶：{spouse.firstName} {spouse.lastName}（{spouse.age}岁）" : "  配偶：无");

            // 祖辈
            var ancestors = GetAncestors(characterId);
            if (ancestors.Count > 0)
            {
                sb.AppendLine("  祖辈：");
                foreach (var a in ancestors)
                    sb.AppendLine($"    - {a.firstName} {a.lastName}（{a.age}岁）");
            }

            // 父母（双亲显式列出——家族树含父系+母系）
            if (c.fatherId >= 0 || c.motherId >= 0)
            {
                sb.AppendLine("  父母：");
                if (c.fatherId >= 0)
                {
                    var f = GetCharacter(c.fatherId);
                    if (f != null) sb.AppendLine($"    父 - {f.firstName} {f.lastName}（{f.age}岁）");
                }
                if (c.motherId >= 0)
                {
                    var m = GetCharacter(c.motherId);
                    if (m != null) sb.AppendLine($"    母 - {m.firstName} {m.lastName}（{m.age}岁）");
                }
            }

            // 子女
            var children = GetChildren(characterId);
            if (children.Count > 0)
            {
                sb.AppendLine($"  子女（{children.Count}）：");
                foreach (var ch in children)
                    sb.AppendLine($"    - {ch.firstName} {ch.lastName}（{ch.age}岁，{(ch.isMale ? "男" : "女")}）");
            }

            // 孙辈
            var grands = GetGrandchildren(characterId);
            if (grands.Count > 0)
            {
                sb.AppendLine($"  孙辈（{grands.Count}）：");
                foreach (var g in grands)
                    sb.AppendLine($"    - {g.firstName} {g.lastName}（{g.age}岁）");
            }
            return sb.ToString();
        }

        /// <summary>自主生育（已婚夫妇优先——配偶生育；未婚保留随机配对，保证 DNA 遗传持续发生）</summary>
        private void AutoProcreate(int currentYear)
        {
            var males = new List<CharacterData>();
            var females = new List<CharacterData>();
            foreach (var c in _characters.Values)
            {
                if (!c.isAlive || c.age < 16 || c.age > 45) continue; // 育龄 16-45
                if (c.isMale) males.Add(c); else females.Add(c);
            }
            if (males.Count == 0 || females.Count == 0) return;

            // 已婚夫妇优先生育（spouseId 双向配对——婚姻制度的生育）
            foreach (var male in males)
            {
                if (male.spouseId < 0) continue;
                var wife = GetCharacter(male.spouseId);
                if (wife == null || !wife.isAlive) continue;
                if (wife.age < 16 || wife.age > 45) continue;
                if (CountChildren(male.characterId) >= 8) continue;
                if (CountChildren(wife.characterId) >= 8) continue;
                if (IsDirectLineage(male, wife)) continue;

                // 已婚夫妇每年约 25% 生育概率（≈0.0007/天——比未婚配对高）
                if (UnityEngine.Random.value < 0.0007f)
                {
                    var child = Procreate(male.characterId, wife.characterId, currentYear);
                    if (child != null)
                        Debug.Log($"[Character] 已婚生育：{male.firstName}×{wife.firstName} → {child.firstName} {child.lastName}");
                }
            }

            // 未婚随机配对（原有机制保留——简化社会未婚生育）
            foreach (var male in males)
            {
                if (male.spouseId >= 0) continue; // 已婚不再随机配对
                if (CountChildren(male.characterId) >= 8) continue;

                foreach (var female in females)
                {
                    if (female.realmId != male.realmId) continue;
                    if (CountChildren(female.characterId) >= 8) continue;
                    if (IsDirectLineage(male, female)) continue;

                    // 每年约 15% 生育概率（≈0.0004/天）
                    if (UnityEngine.Random.value < 0.0004f)
                    {
                        Procreate(male.characterId, female.characterId, currentYear);
                        break; // 每轮每名男性至多一个孩子
                    }
                }
            }
        }

        /// <summary>
        /// 初始统治者：为每个政权创建统治者+配偶并建家族
        /// 角色系统与生育机制的角色源头（政权初始无人治理的填补）
        /// </summary>
        public void CreateInitialRulers(Dictionary<int, RealmData> realms, int currentYear)
        {
            foreach (var realm in realms.Values)
            {
                int cultureId = realm.realmId;   // 政权 0/1/2 → 文化 0/1/2（内容覆盖后 id 1 为内容文化）
                int raceId = 0;                  // 预种族已删除（2026-08-29 定稿）：当前仅人类

                string lastName = GenerateName(cultureId, 2);
                bool rulerIsMale = UnityEngine.Random.value < 0.5f;

                // 角色模板（注册表有则套用：tmpl_ruler/tmpl_spouse；无则回退随机年龄）
                ContentRegistry.TryGetCharacterTemplate("tmpl_ruler", out var rulerTpl);
                ContentRegistry.TryGetCharacterTemplate("tmpl_spouse", out var spouseTpl);

                var ruler = CreateCharacter(GenerateName(cultureId, rulerIsMale ? 0 : 1), lastName,
                    0, rulerIsMale, cultureId, raceId, 0, CharacterRole.Ruler,
                    template: rulerTpl);
                ruler.realmId = realm.realmId;

                var spouse = CreateCharacter(GenerateName(cultureId, rulerIsMale ? 1 : 0), lastName,
                    0, !rulerIsMale, cultureId, raceId, 0, CharacterRole.Spouse,
                    template: spouseTpl);
                spouse.realmId = realm.realmId;

                var family = CreateFamily(lastName, ruler.characterId, currentYear, realm.realmId);
                family.AddMember(spouse.characterId);
                spouse.familyId = family.familyId;
            }
        }

        /// <summary>创建家族</summary>
        public FamilyNode CreateFamily(string familyName, int founderId, int foundingYear, int realmId = -1)
        {
            var family = new FamilyNode
            {
                familyId = _nextFamilyId++,
                familyName = familyName,
                founderCharacterId = founderId,
                foundingYear = foundingYear,
                holderRealmId = realmId,
                Innovations = Innovations // 传递革新树引用（家族传统解锁前置检查）
            };
            family.AddMember(founderId);
            _families[family.familyId] = family;

            if (_characters.TryGetValue(founderId, out var founder))
                founder.familyId = family.familyId;

            return family;
        }

        /// <summary>创建羁绊</summary>
        public CharacterBond CreateBond(int charAId, int charBId, BondType type)
        {
            var bond = new CharacterBond
            {
                bondId = _nextBondId++,
                characterAId = charAId,
                characterBId = charBId,
                type = type,
                establishedDay = 0
            };
            _bonds.Add(bond);
            return bond;
        }

        /// <summary>每日角色Tick</summary>
        public void DailyTick(int currentDay, int currentYear)
        {
            // 角色更新
            foreach (var character in _characters.Values)
            {
                character.DailyTick(currentDay, currentYear);
            }

            // 羁绊更新
            foreach (var bond in _bonds)
            {
                bond.DailyTick();
            }

            // 自主生育（最小机制：DNA 遗传持续发生）
            AutoProcreate(currentYear);

            // 饮食联动（肥胖驱动）
            DailyDiet();

            // 精神疾病触发与缓解
            CheckMentalDisorders();

            // 人格亲和漂移（借鉴 MPD 好感机制：性格相投日久生情，相斥渐行渐远）
            PersonalityOpinionDrift();

            // 清理死亡角色的军队指挥
            foreach (var character in _characters.Values)
            {
                if (!character.isAlive && character.commandedArmyId >= 0)
                {
                    Debug.Log($"[Character] {character.fullName} 死亡，军队 {character.commandedArmyId} 失去指挥");
                    character.commandedArmyId = -1;
                }
            }
        }

        // ===== 查询接口 =====
        public CharacterData GetCharacter(int id)
        {
            return _characters.TryGetValue(id, out var c) ? c : null;
        }

        public FamilyNode GetFamily(int id)
        {
            return _families.TryGetValue(id, out var f) ? f : null;
        }

        public List<CharacterData> GetAliveCharacters()
        {
            var result = new List<CharacterData>();
            foreach (var c in _characters.Values)
                if (c.isAlive) result.Add(c);
            return result;
        }

        public List<CharacterData> GetCharactersByRealm(int realmId)
        {
            var result = new List<CharacterData>();
            foreach (var c in _characters.Values)
                if (c.realmId == realmId && c.isAlive) result.Add(c);
            return result;
        }

        public List<CharacterData> GetCharactersByRole(CharacterRole role)
        {
            var result = new List<CharacterData>();
            foreach (var c in _characters.Values)
                if (c.role == role && c.isAlive) result.Add(c);
            return result;
        }

        /// <summary>查找政权的现任统治者（Role=Ruler 且存活；无则 null）</summary>
        public CharacterData FindRulerOfRealm(int realmId)
        {
            foreach (var c in _characters.Values)
                if (c.isAlive && c.realmId == realmId && c.role == CharacterRole.Ruler)
                    return c;
            return null;
        }

        /// <summary>寻找最适合的将领</summary>
        public CharacterData FindBestGeneral(int realmId)
        {
            CharacterData best = null;
            float bestScore = 0f;
            foreach (var c in _characters.Values)
            {
                if (c.realmId != realmId || !c.isAlive) continue;
                float score = c.CalculateCommandAbility();
                if (score > bestScore)
                {
                    bestScore = score;
                    best = c;
                }
            }
            return best;
        }

        /// <summary>寻找最适合的统治者</summary>
        public CharacterData FindBestRuler(int realmId)
        {
            CharacterData best = null;
            float bestScore = 0f;
            foreach (var c in _characters.Values)
            {
                if (c.realmId != realmId || !c.isAlive) continue;
                float score = c.CalculateRuleAbility();
                if (score > bestScore)
                {
                    bestScore = score;
                    best = c;
                }
            }
            return best;
        }

        public int GetTotalCharacterCount() => _characters.Count;
        public int GetAliveCharacterCount() => GetAliveCharacters().Count;
        public int GetTotalFamilyCount() => _families.Count;
        public IReadOnlyDictionary<int, CharacterData> GetAllCharacters() => _characters;
        public IReadOnlyDictionary<int, FamilyNode> GetAllFamilies() => _families;
        public IReadOnlyList<CharacterBond> GetAllBonds() => _bonds;
        // ===== 三信仰形态（社会/私人/秘密） =====

        /// <summary>初始化私人信仰（角色创建时=社会信仰一致）</summary>
        public void InitPrivateFaith(CharacterData c)
        {
            if (c == null || c.privateFaithId == -1)
            {
                if (c != null) c.privateFaithId = c.faithId;
            }
        }

        /// <summary>
        /// 添加个人信条（Personal Tenet——本人信条）
        /// 来源：借其他信仰（个人融合）/组合原有/自创——与官方教义冲突→偏离度
        /// </summary>
        public bool AddPersonalTenet(CharacterData c, string optionId)
        {
            if (c == null || string.IsNullOrEmpty(optionId)) return false;
            if (c.personalTenets.Contains(optionId)) return false;
            c.personalTenets.Add(optionId);
            // 私人信仰偏离社会信仰 → 可能触发秘密信仰（由外部判定——此处只记录）
            return true;
        }

        /// <summary>移除个人信条</summary>
        public bool RemovePersonalTenet(CharacterData c, string optionId)
        {
            if (c == null) return false;
            return c.personalTenets.Remove(optionId);
        }

        /// <summary>私人信仰是否偏离社会信仰（信条差异——偏离度>0）</summary>
        public bool HasFaithDivergence(CharacterData c)
        {
            if (c == null) return false;
            if (c.privateFaithId != c.faithId) return true;
            return c.personalTenets.Count > 0;
        }

        /// <summary>秘密信仰状态更新（私人≠社会且被禁止时=true——暴露风险）</summary>
        public void UpdateSecretBelief(CharacterData c)
        {
            if (c == null) return;
            c.isSecretBeliever = HasFaithDivergence(c);
        }

        /// <summary>
        /// 灵性满足每日更新（角色虔诚——体验支柱动态化）：
        /// 美德特质 +0.01/日（等级制——3 级美德 +0.03）｜罪行特质 -0.02/日｜
        /// 秘密信徒（私人≠社会）额外 -0.01（信仰撕裂——灵性煎熬）
        /// 虔诚上限型：0-100——高虔诚=圣人候选资格/统治合法性加成
        /// </summary>
        public void UpdatePiety(CharacterData c, FaithSystem faith)
        {
            if (c == null) return;
            float delta = 0f;
            if (faith != null)
            {
                delta += faith.GetVirtueScore(c) * 0.01f;
                delta -= faith.GetSinScore(c) * 0.02f;
            }
            if (c.isSecretBeliever) delta -= 0.01f;
            c.spiritualFulfillment = Mathf.Clamp(c.spiritualFulfillment + delta, 0f, 100f);
        }
    }
}