using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Economy;
using CivilizationEvolution.Politics;
using CivilizationEvolution.Race;
using CivilizationEvolution.Tech;

namespace CivilizationEvolution.Role
{
    /// <summary>
    /// 人格七维（企划书 9.3：-100~100，家族遗传基线，压力&gt;60 漂移翻倍）。
    /// 定位：底层人格倾向 / AI 行为参数（参考 CK3 ai_boldness / ai_greed / ai_compassion
    /// / ai_zeal / ai_energy / ai_sociability / ai_honor 的连续值路径）；玩家可见的离散
    /// 性格七维映射为五级标签（强负/负/中性/正/强正，每级名词+形容词+行为描述，对齐 CK3 ai_personality_l_*.yml）；统治者额外推导经济原型（见 DetermineEconomicalArchetype）。性格管行为，特质（PersonalityTrait）管能力，二者分离。
    /// 统一枚举：取代此前散落于初始化/漂移/亲和/描述/事件各处的 "boldness" 魔法字符串。
    /// </summary>
    public enum PersonalityDimension
    {
        Boldness,       // 大胆（怯懦↔勇猛）
        Compassion,     // 悲悯（冷酷↔慈悲）
        Greed,          // 贪婪（慷慨↔贪婪）
        Honor,          // 荣誉（狡诈↔诚实/重诺）
        Rationality,    // 理性（冲动/狂热↔冷静理性）
        Vengefulness,   // 报复（宽恕↔睚眦必报）
        Piety           // 虔信（无神/愤世↔虔诚信奉）
    }

    /// <summary>人格七维元数据（唯一权威顺序表 / 字符串键 / 中文名；新增维度只需改此处）</summary>
    public static class PersonalityDimensions
    {
        /// <summary>七维固定顺序（遍历、数组下标、模板 bias 对齐均以此为唯一来源）</summary>
        public static readonly PersonalityDimension[] All =
        {
            PersonalityDimension.Boldness,
            PersonalityDimension.Compassion,
            PersonalityDimension.Greed,
            PersonalityDimension.Honor,
            PersonalityDimension.Rationality,
            PersonalityDimension.Vengefulness,
            PersonalityDimension.Piety
        };

        /// <summary>数据/存档/事件 JSON 使用的字符串键（与历史拼写完全一致，保证旧数据兼容）</summary>
        public static string Key(this PersonalityDimension dim) => dim switch
        {
            PersonalityDimension.Boldness => "boldness",
            PersonalityDimension.Compassion => "compassion",
            PersonalityDimension.Greed => "greed",
            PersonalityDimension.Honor => "honor",
            PersonalityDimension.Rationality => "rationality",
            PersonalityDimension.Vengefulness => "vengefulness",
            PersonalityDimension.Piety => "piety",
            _ => ""
        };

        /// <summary>中文显示名</summary>
        public static string DisplayName(this PersonalityDimension dim) => dim switch
        {
            PersonalityDimension.Boldness => "大胆",
            PersonalityDimension.Compassion => "悲悯",
            PersonalityDimension.Greed => "贪婪",
            PersonalityDimension.Honor => "荣誉",
            PersonalityDimension.Rationality => "理性",
            PersonalityDimension.Vengefulness => "报复",
            PersonalityDimension.Piety => "虔信",
            _ => "?"
        };

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
    /// 统治者经济原型（对齐 CK3 economical_archetype：由性格七维组合推导的互斥行为原型，决定 AI 预算与战略倾向）。
    /// CK3 原版 6 原型 + Balanced；扩展 4 原型（Administrator/Schemer/CulturalPatron/GodlessReformer）。
    /// 推导优先级见 CharacterData.DetermineEconomicalArchetype。
    /// </summary>
    public enum EconomicalArchetype
    {
        Balanced,           // 平衡型：无明显倾向（默认）
        Warlike,            // 好战者：boldness>0 + greed>=0 + 好战特质/高阈值
        Cautious,           // 谨慎者：boldness<=0 + 偏执/怯懦/低大胆+耐心
        EconomicalBoom,     // 经济繁荣者（建设者）：大胆>0 + 勤勉特质，非好战
        PiousBuilder,       // 虔诚建设者：zeal>0 + 虔诚/勤勉特质，非好战
        Conqueror,          // 征服者：特殊（征服特质），优先级最高
        Unpredictable,      // 不可预测者：轻浮/疯狂
        Administrator,      // 行政官僚（扩展）：理性高 + 荣誉高 + 贪婪低
        Schemer,            // 阴谋家（扩展）：狡诈高 + 报复高 + 理性中
        CulturalPatron,     // 文化赞助人（扩展）：慈悲高 + 荣誉高 + 虔诚中
        GodlessReformer     // 不敬神的改革者（扩展）：虔诚强负 + 理性高 + 大胆中
    }

    /// <summary>经济原型定义（显示名、描述、行为偏置向量）</summary>
    public class EconomicalArchetypeInfo
    {
        public EconomicalArchetype archetype;
        public string displayName;
        public string description;
        /// <summary>行为偏置：战争/建设/宗教/阴谋/外交 五维倾向（-1~1），供 AI 决策读取</summary>
        public float warBias, buildBias, faithBias, schemeBias, diplomacyBias;

        public EconomicalArchetypeInfo(EconomicalArchetype archetype, string displayName, string description,
            float war, float build, float faith, float scheme, float diplomacy)
        {
            this.archetype = archetype; this.displayName = displayName; this.description = description;
            warBias = war; buildBias = build; faithBias = faith; schemeBias = scheme; diplomacyBias = diplomacy;
        }
    }

    /// <summary>经济原型定义表</summary>
    public static class EconomicalArchetypes
    {
        public static readonly EconomicalArchetypeInfo[] All =
        {
            new EconomicalArchetypeInfo(EconomicalArchetype.Balanced, "均衡之主", "无明显战略倾向，行事中庸", 0f, 0f, 0f, 0f, 0f),
            new EconomicalArchetypeInfo(EconomicalArchetype.Warlike, "好战之君", "崇尚武力，以战争扩张为第一要务", 0.8f, -0.2f, 0.1f, 0.1f, -0.2f),
            new EconomicalArchetypeInfo(EconomicalArchetype.Cautious, "谨慎之主", "谋定后动，偏好防御与巩固", -0.3f, 0.3f, 0.1f, 0.1f, 0.2f),
            new EconomicalArchetypeInfo(EconomicalArchetype.EconomicalBoom, "繁荣缔造者", "专注经济建设与领地开发", -0.2f, 0.8f, 0f, 0f, 0.2f),
            new EconomicalArchetypeInfo(EconomicalArchetype.PiousBuilder, "虔信营建者", "以信仰为名兴建宗教建筑与转化", -0.1f, 0.4f, 0.8f, -0.1f, 0.1f),
            new EconomicalArchetypeInfo(EconomicalArchetype.Conqueror, "征服者", "为征服而生，永不满足于现有疆土", 1.0f, -0.3f, 0.2f, 0.2f, -0.3f),
            new EconomicalArchetypeInfo(EconomicalArchetype.Unpredictable, "不可预测者", "行事乖张，令人难以捉摸", 0.3f, 0f, 0f, 0.3f, -0.2f),
            new EconomicalArchetypeInfo(EconomicalArchetype.Administrator, "行政巨匠", "崇尚法治与行政效率，精于吏治", -0.2f, 0.4f, 0.1f, 0f, 0.3f),
            new EconomicalArchetypeInfo(EconomicalArchetype.Schemer, "阴谋大师", "在阴影中操纵一切，偏好暗杀与勒索", 0.1f, 0f, 0f, 0.9f, -0.1f),
            new EconomicalArchetypeInfo(EconomicalArchetype.CulturalPatron, "文化赞助人", "推崇文化艺术，以软实力教化四方", -0.2f, 0.5f, 0.2f, 0f, 0.4f),
            new EconomicalArchetypeInfo(EconomicalArchetype.GodlessReformer, "不敬神的改革者", "漠视宗教权威，推动世俗化与制度改革", 0.1f, 0.3f, -0.8f, 0.2f, 0.1f),
        };

        public static EconomicalArchetypeInfo Get(EconomicalArchetype archetype)
        {
            foreach (var a in All)
                if (a.archetype == archetype) return a;
            return All[0];
        }
    }

    /// <summary>
    /// 角色核心数值
    /// 普通人口块不存储个体数值，仅有名角色存储完整角色数值
    /// </summary>
    [System.Serializable]
    public class CharacterData
    {
        public int characterId;
        public string firstName;
        public string lastName;
        public string fullName => $"{firstName} {lastName}";

        // 基础属性
        public int age;
        public bool isMale;
        public int birthDay;
        public int birthYear;
        public int deathDay = -1;
        public int deathYear = -1;
        public bool isAlive => deathDay < 0;

        /// <summary>社会阶层（经济系统对接——政体资格判定/阶层好感依赖；默认农民）</summary>
        public GameEnums.SocialClass socialClass = GameEnums.SocialClass.Peasant;

        /// <summary>社会亚阶层（阶层细分——默认对应该主阶层的默认亚类）</summary>
        public GameEnums.SocialSubclass socialSubclass = GameEnums.SocialSubclass.Freeholder;

        /// <summary>设置社会阶层（自动同步默认亚阶层；未细分阶层保留原亚类）</summary>
        public void SetSocialClass(GameEnums.SocialClass cls)
        {
            socialClass = cls;
            var def = GameEnums.SocialClassHierarchy.GetDefaultSubclass(cls);
            if (def.HasValue)
                socialSubclass = def.Value;
        }
        /// <summary>
        /// 角色身份（CharacterRole）到社会主阶层映射。
        /// 角色是其所属阶层的有名代言人，身份必须与人口系统阶层对齐。
        /// 前现代有名军官多出身贵族行伍，故 Military 归贵族教士层；普通士兵由人口块表达，不建角色。
        /// </summary>
        public static GameEnums.SocialClass RoleToClass(CharacterRole role) => role switch
        {
            CharacterRole.Ruler or CharacterRole.Heir or CharacterRole.Spouse => GameEnums.SocialClass.Royalty,
            CharacterRole.Noble or CharacterRole.Clergy or CharacterRole.Courtier
                or CharacterRole.Military => GameEnums.SocialClass.NobilityClergy,
            CharacterRole.Merchant or CharacterRole.Scholar => GameEnums.SocialClass.MerchantFreeman,
            CharacterRole.Commoner => GameEnums.SocialClass.Peasant,
            _ => GameEnums.SocialClass.Peasant
        };

        /// <summary>角色身份到社会亚阶层（无细分阶层返回 null，由默认值处理）</summary>
        public static GameEnums.SocialSubclass? RoleToSubclass(CharacterRole role) => role switch
        {
            CharacterRole.Merchant => GameEnums.SocialSubclass.Merchant,
            CharacterRole.Scholar => GameEnums.SocialSubclass.Scholar,
            CharacterRole.Commoner => GameEnums.SocialSubclass.Freeholder,
            _ => null
        };

        /// <summary>按身份同步社会阶层与亚阶层（创建、继位、封官等身份变更时调用）</summary>
        public void SyncClassFromRole()
        {
            socialClass = RoleToClass(role);
            var sub = RoleToSubclass(role);
            socialSubclass = sub ?? GameEnums.SocialClassHierarchy.GetDefaultSubclass(socialClass)
                ?? GameEnums.SocialSubclass.Freeholder;
        }

        // 身份
        public int realmId = -1;
        public int familyId = -1;
        public int cultureId;
        public int raceId;
        /// <summary>社会信仰（展示给社会的——公开合法——原 faithId 语义）</summary>
        public int faithId;
        /// <summary>私人信仰（本人真实信仰——默认同社会信仰——可不同——
        /// 冲突时转入秘密信仰状态）</summary>
        public int privateFaithId = -1;
        /// <summary>个人信条（Personal Tenets——本人信条——可借其他信仰/
        /// 组合原有/自创——与官方教义冲突→偏离度）</summary>
        public List<string> personalTenets = new List<string>();
        /// <summary>秘密信仰标记（私人≠社会且被禁止时=true——身份暴露风险）</summary>
        public bool isSecretBeliever = false;
        public CharacterRole role = CharacterRole.Commoner;

        // 血缘（DNA 遗传与近亲系数计算依赖）
        public int fatherId = -1;
        public int motherId = -1;
        /// <summary>配偶（-1=未婚；婚姻双向设置）</summary>
        public int spouseId = -1;

        // DNA（有名角色专属；人口块不存个体 DNA）
        public DnaData dna;

        /// <summary>个体预期寿命（年）：种族基准 + DNA 寿命偏移，出生时确定</summary>
        public float expectedLifespanYears = 75f;

        /// <summary>DNA 表达结果（出生时一次性计算，终身不变）</summary>
        public DnaExpression dnaExpression;

        /// <summary>个体综合抗性 0-100（种族抗性基准 + DNA 抗性偏移，疾病感染修正用）</summary>
        public float individualResistance = 50f;

        // 核心六维属性（0-100，企划书第九篇：武力/外交-社交/军事经略/学识/阴谋/管理）
        [Range(0f, 100f)] public float martial = 50f;      // 武力
        [Range(0f, 100f)] public float diplomacy = 50f;     // 外交-社交
        [Range(0f, 100f)] public float warfare = 50f;       // 军事经略（原 piety 位，2026-08-29 定稿）
        [Range(0f, 100f)] public float stewardship = 50f;   // 管理
        [Range(0f, 100f)] public float intrigue = 50f;       // 谋略
        [Range(0f, 100f)] public float learning = 50f;       // 学识

        // ===== 容量型数值（企划书：当前值 + 容量等级 + 容量上限） =====
        /// <summary>威望当前值（0~当前容量上限）</summary>
        public float prestige = 0f;
        /// <summary>威望容量等级 1-5（上限 100/300/600/1000/1500）</summary>
        public int prestigeCapacityLevel = 1;
        /// <summary>恶名当前值（0~当前容量上限，与威望并存）</summary>
        public float notoriety = 0f;

        // ===== 上限型数值（0-100 固定上限） =====
        [Range(0f, 100f)] public float health = 100f;        // 健康
        [Range(0f, 100f)] public float fertility = 50f;       // 生育力
        [Range(0f, 100f)] public float stress = 0f;           // 压力（>60 人格漂移翻倍，>80 精神疾病风险）
        [Range(0f, 100f)] public float dread = 0f;            // 恐惧
        [Range(0f, 100f)] public float obesity = 20f;         // 肥胖（饮食/活动驱动，影响健康/魅力）
        [Range(0f, 100f)] public float charm = 50f;           // 魅力

        // ===== 人格七维（企划书 9.3：-100~100，家族遗传基线，压力>60 漂移翻倍） =====
        [Range(-100f, 100f)] public float boldness;      // 大胆
        [Range(-100f, 100f)] public float compassion;    // 悲悯
        [Range(-100f, 100f)] public float greed;         // 贪婪
        [Range(-100f, 100f)] public float honor;         // 荣誉
        [Range(-100f, 100f)] public float rationality;   // 理性
        [Range(-100f, 100f)] public float vengefulness;  // 报复
        [Range(-100f, 100f)] public float piety;         // 虔信（人格维度，非六维属性）

        // ===== 精神疾病（简单版：单一活跃状态，由高压/恐惧/高龄/重病触发；id 见 MentalDisorderIds/注册表） =====
        public string mentalDisorderId = "";
        /// <summary>压力>80 持续天数（精神疾病触发计时）</summary>
        public int highStressDays = 0;
        /// <summary>压力<30 持续天数（精神疾病缓解计时，失智不可逆）</summary>
        public int lowStressRecoveryDays = 0;

        // 人格特质列表
        public List<PersonalityTrait> traits = new List<PersonalityTrait>();

        // 关系
        public Dictionary<int, CharacterRelation> relations = new Dictionary<int, CharacterRelation>();

        // 技能/经验
        public Dictionary<string, float> skills = new Dictionary<string, float>();

        // 财产
        public float gold = 0f;
        public List<int> ownedTitles = new List<int>();

        // 军队指挥
        public int commandedArmyId = -1;

        /// <summary>计算综合能力值（六维：武力/外交/军事经略/管理/谋略/学识）</summary>
        public float CalculateOverallAbility()
        {
            return (martial + diplomacy + warfare + stewardship + intrigue + learning) / 6f;
        }

        /// <summary>计算统治能力（用于政权稳定）</summary>
        public float CalculateRuleAbility()
        {
            return stewardship * 0.4f + diplomacy * 0.3f + intrigue * 0.2f + learning * 0.1f;
        }

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

        /// <summary>每日角色Tick</summary>
        public void DailyTick(int currentDay, int currentYear)
        {
            if (!isAlive) return;

            // 年龄增长（简化：每年生日加1岁）
            if (currentDay == birthDay)
                age++;

            // 健康自然变化（有 DNA 时按个体预期寿命衰减；无 DNA 保持原 50 岁起、100 岁满的线性）
            float onsetAge = dna != null ? expectedLifespanYears * 0.6f : 50f;
            float fullAge = dna != null ? expectedLifespanYears : 100f;
            float ageFactor = age > onsetAge ? (age - onsetAge) / Mathf.Max(1f, fullAge - onsetAge) : 0f;
            // 肥胖 >70 加速衰老
            float healthAgeMult = obesity > 70f ? 1.5f : 1f;
            health = Mathf.Clamp(health - ageFactor * 0.01f * healthAgeMult, 0f, 100f);

            // 压力恢复（受精神疾病影响：抑郁/焦虑恢复慢；压力>60 时几乎不恢复）
            var disorderDef = MentalHealthSystem.GetDef(mentalDisorderId);
            float stressDecay = disorderDef != null ? 0.05f * disorderDef.stressDecayMult : 0.05f;
            stress = Mathf.Max(0f, stress - (stress > 60f ? 0.01f : stressDecay));

            // 高压计时（精神疾病触发判定）
            if (stress > 80f)
                highStressDays++;
            else
                highStressDays = Mathf.Max(0, highStressDays - 2);

            // 人格漂移（企划书 9.3：压力>60 漂移速度翻倍，随机游走；七维统一走 Add 入口）
            float drift = stress > 60f ? 0.02f : 0.01f;
            foreach (var pd in PersonalityDimensions.All)
                AddPersonality(pd, UnityEngine.Random.Range(-drift, drift));

            // 肥胖自然回落（活动代谢）
            obesity = Mathf.Max(0f, obesity - 0.01f);

            // 容量型数值自然衰减（威望/恶名缓慢向零回归）
            prestige = Mathf.Max(0f, prestige - 0.05f);
            notoriety = Mathf.Max(0f, notoriety - 0.02f);

            // 健康过低死亡
            if (health <= 0f)
            {
                Die(currentDay, currentYear, "自然死亡");
            }

            // 特质效果
            foreach (var trait in traits)
            {
                trait.ApplyDailyEffect(this);
            }
        }

        // ===== 人格描述（企划书 9.3 顶层：写实场景化描述，禁止四字标签与善恶定性） =====

        /// <summary>
        /// 人格强度分档（借鉴 CK3 More Personality Depth 三级制：Mild/Normal/Intense）
        /// 0=无倾向(|v|&lt;15) 1=轻度(15-35) 2=中度(35-65) 3=重度(&gt;65)
        /// 分档驱动好感缩放与 AI 偏置幅度
        /// </summary>
        /// <summary>
        /// 人格强度分档（参考 CK3 More Personality Depth 三级制 Mild/Normal/Intense）：
        /// 0=无倾向(|v|&lt;15) 1=轻度(15-35) 2=中度(35-65) 3=重度(&gt;65)
        /// 分档驱动派生特质等级、好感缩放与 AI 偏置幅度
        /// </summary>
        public int GetPersonalityTier(PersonalityDimension dim)
        {
            float abs = Mathf.Abs(GetPersonalityValue(dim));
            if (abs < 15f) return 0;
            if (abs < 35f) return 1;
            if (abs < 65f) return 2;
            return 3;
        }

        /// <summary>按维度取人格值（唯一枚举入口；七维即 AI 行为参数）</summary>
        public float GetPersonalityValue(PersonalityDimension dim) => dim switch
        {
            PersonalityDimension.Boldness => boldness,
            PersonalityDimension.Compassion => compassion,
            PersonalityDimension.Greed => greed,
            PersonalityDimension.Honor => honor,
            PersonalityDimension.Rationality => rationality,
            PersonalityDimension.Vengefulness => vengefulness,
            PersonalityDimension.Piety => piety,
            _ => 0f
        };

        /// <summary>按维度写人格值（统一 clamp 到 -100~100；所有初始化/漂移/事件/模板的唯一写入口）</summary>
        public void SetPersonalityValue(PersonalityDimension dim, float value)
        {
            float v = Mathf.Clamp(value, -100f, 100f);
            switch (dim)
            {
                case PersonalityDimension.Boldness: boldness = v; break;
                case PersonalityDimension.Compassion: compassion = v; break;
                case PersonalityDimension.Greed: greed = v; break;
                case PersonalityDimension.Honor: honor = v; break;
                case PersonalityDimension.Rationality: rationality = v; break;
                case PersonalityDimension.Vengefulness: vengefulness = v; break;
                case PersonalityDimension.Piety: piety = v; break;
            }
        }

        /// <summary>按维度叠加偏移（事件/模板用，内部走 Set 以统一 clamp）</summary>
        public void AddPersonality(PersonalityDimension dim, float delta)
            => SetPersonalityValue(dim, GetPersonalityValue(dim) + delta);

        // —— 字符串键重载（数据驱动/事件 JSON 兼容；内部解析到枚举，不再各写一份 switch）——
        public int GetPersonalityTier(string dim)
            => PersonalityDimensions.TryParse(dim, out var d) ? GetPersonalityTier(d) : 0;
        public float GetPersonalityValue(string dim)
            => PersonalityDimensions.TryParse(dim, out var d) ? GetPersonalityValue(d) : 0f;

        /// <summary>
        /// 人格亲和度（-20~+20，借鉴 MPD 的 same/opposite opinion 机制）：
        /// 七维逐项比较——同向（同号且双方强度&gt;0）互喜、反向互厌，强度分档决定幅度；
        /// 用于关系好感缓慢漂移（性格相投日久生情，相斥渐行渐远）
        /// </summary>
        public float GetPersonalityAffinity(CharacterData other)
        {
            if (other == null) return 0f;
            float affinity = 0f;
            // 七维逐项比较（同向互喜、反向互厌，强度分档决定幅度——MPD same/opposite 好感的连续轴版本）
            foreach (var dim in PersonalityDimensions.All)
            {
                float a = GetPersonalityValue(dim);
                float b = other.GetPersonalityValue(dim);
                if (Mathf.Abs(a) < 15f || Mathf.Abs(b) < 15f) continue; // 无倾向不参与

                bool same = (a > 0f) == (b > 0f);
                int tier = Mathf.Min(GetPersonalityTier(dim), other.GetPersonalityTier(dim));
                if (same)
                    affinity += tier switch { 3 => 6f, 2 => 4f, _ => 2f };   // 同向：+2/+4/+6
                else
                    affinity -= tier switch { 3 => 5f, 2 => 3f, _ => 1f };   // 反向：-1/-3/-5
            }
            return Mathf.Clamp(affinity, -20f, 20f);
        }

        /// <summary>生成写实人格描述：按最高 2 维组合套用场景模板（维度顺序表唯一来源）</summary>
        public string GetPersonalityDescription()
        {
            var dims = new (PersonalityDimension dim, float value)[PersonalityDimensions.All.Length];
            for (int i = 0; i < PersonalityDimensions.All.Length; i++)
            {
                var d = PersonalityDimensions.All[i];
                dims[i] = (d, GetPersonalityValue(d));
            }
            Array.Sort(dims, (a, b) => Mathf.Abs(b.value).CompareTo(Mathf.Abs(a.value)));

            var top1 = dims[0];
            var top2 = dims[1];
            if (Mathf.Abs(top1.value) < 15f)
                return "性情平和中正，既不偏激也不执拗，处世随分安时。";

            string t1 = DescribeDimension(top1.dim, top1.value);
            string t2 = DescribeDimension(top2.dim, top2.value);
            return $"为人{t1}，行事{t2}。";
        }

        private static string DescribeDimension(PersonalityDimension dim, float value)
        {
            bool high = value > 0f;
            return dim switch
            {
                PersonalityDimension.Boldness => high ? "胆气过人，临事敢为，鲜有畏葸" : "性谨慎，谋定后动，不喜冒险",
                PersonalityDimension.Compassion => high ? "心肠慈悲，见不得民生疾苦，常施仁政" : "心硬如铁，视百姓如草芥，无情可动",
                PersonalityDimension.Greed => high ? "贪得无厌，见利忘义，库藏永不餍足" : "淡泊财货，不慕荣利，清廉自守",
                PersonalityDimension.Honor => high ? "重诺守信，把名誉看得比性命更重" : "轻诺寡信，名节于他不过是可售之物",
                PersonalityDimension.Rationality => high ? "冷静理性，遇事权衡利害，不感情用事" : "率性而为，凭一时好恶决断，不计后果",
                PersonalityDimension.Vengefulness => high ? "睚眦必报，恩怨分明，得罪过他的人他都记着" : "宽宏大量，受了委屈也多半一笑置之",
                PersonalityDimension.Piety => high ? "虔诚信奉，常与神职人员来往，礼敬神祇" : "对神明半信半疑，礼数只是做给人看",
                _ => "性情难测"
            };
        }
        /// <summary>是否持有指定特质（按 traitId 匹配；traits 是 List<PersonalityTrait> 对象列表）</summary>
        public bool HasTrait(string traitId)
        {
            foreach (var t in traits)
                if (t.traitId == traitId) return true;
            return false;
        }

        /// <summary>是否持有指定特质的任意等级（如"wrathful"匹配wrathful_1/2/3；特殊特质如conqueror仍精确匹配）</summary>
        public bool HasTraitAnyLevel(string baseTraitId)
        {
            foreach (var t in traits)
                if (t.traitId == baseTraitId || t.traitId.StartsWith(baseTraitId + "_"))
                    return true;
            return false;
        }

        /// <summary>角色持有的性格标签显示（从 traits 过滤 category=Personality，如"勇敢  慈悲者  虔诚者"）</summary>
        public string GetPersonalityTagsDisplay()
        {
            var parts = new System.Collections.Generic.List<string>();
            foreach (var t in traits)
                if (t.category == TraitCategory.Personality)
                    parts.Add(t.traitName);
            return parts.Count == 0 ? "无明显性格" : string.Join("  ", parts);
        }

        /// <summary>统治者经济原型推导（对齐 CK3 economical_archetype：由七维组合互斥推导，优先级从高到低）</summary>
        public EconomicalArchetype DetermineEconomicalArchetype()
        {
            float bold = GetPersonalityValue(PersonalityDimension.Boldness);
            float greed = GetPersonalityValue(PersonalityDimension.Greed);
            float zeal = GetPersonalityValue(PersonalityDimension.Piety);
            float compass = GetPersonalityValue(PersonalityDimension.Compassion);
            float honor = GetPersonalityValue(PersonalityDimension.Honor);
            float rational = GetPersonalityValue(PersonalityDimension.Rationality);
            float venge = GetPersonalityValue(PersonalityDimension.Vengefulness);

            // 1. 征服者：特殊标记，优先级最高
            if (HasTrait("conqueror") || HasTrait("greatest_of_khans"))
                return EconomicalArchetype.Conqueror;

            // 2. 好战者：大胆>0 + 贪婪>=0 + (好战特质 或 高阈值组合)，排除慈悲/怯懦
            bool warlikeTraits = HasTraitAnyLevel("wrathful") || HasTraitAnyLevel("ambitious") || HasTraitAnyLevel("vengeful") || HasTraitAnyLevel("zealous") || HasTraitAnyLevel("sadistic");
            bool warlikeThreshold = (bold >= 50f && greed >= 50f) || (bold >= 25f && greed >= 100f) || (bold >= 100f && greed >= 25f);
            if (bold > 0f && greed >= 0f && (warlikeTraits || warlikeThreshold)
                && compass < 75f && !HasTraitAnyLevel("compassionate") && !HasTraitAnyLevel("craven") && !HasTraitAnyLevel("calm"))
                return EconomicalArchetype.Warlike;

            // 3. 虔诚建设者：虔诚>0 + 虔诚/勤勉特质，排除愤世/贪婪
            if (zeal > 0f && (HasTraitAnyLevel("zealous") || HasTraitAnyLevel("forgiving") || HasTraitAnyLevel("diligent") || HasTraitAnyLevel("humble") || HasTraitAnyLevel("patient"))
                && !HasTraitAnyLevel("cynical") && !HasTraitAnyLevel("greedy"))
                return EconomicalArchetype.PiousBuilder;

            // 4. 经济繁荣者：大胆>0 + 勤勉/冷静特质，排除贪婪/急躁
            if (bold > 0f && (HasTraitAnyLevel("diligent") || HasTraitAnyLevel("calm") || HasTraitAnyLevel("patient") || HasTraitAnyLevel("generous") || HasTraitAnyLevel("stubborn"))
                && !HasTraitAnyLevel("greedy") && !HasTraitAnyLevel("impatient"))
                return EconomicalArchetype.EconomicalBoom;

            // 5. 不敬神的改革者（扩展）：虔诚强负 + 理性高 + 大胆中
            if (zeal <= -50f && rational >= 40f && bold >= 0f)
                return EconomicalArchetype.GodlessReformer;

            // 6. 阴谋家（扩展）：荣誉低（狡诈）+ 报复高 + 理性中
            if (honor <= -40f && venge >= 40f && rational >= 0f)
                return EconomicalArchetype.Schemer;

            // 7. 行政官僚（扩展）：理性高 + 荣誉高 + 贪婪低
            if (rational >= 40f && honor >= 40f && greed < 40f)
                return EconomicalArchetype.Administrator;

            // 8. 文化赞助人（扩展）：慈悲高 + 荣誉高 + 虔诚中
            if (compass >= 40f && honor >= 40f && zeal >= 0f)
                return EconomicalArchetype.CulturalPatron;

            // 9. 谨慎者：大胆<=0 + (偏执/怯懦 或 低大胆+耐心/冷静)
            if (bold <= 0f && (HasTraitAnyLevel("paranoid") || HasTraitAnyLevel("craven")
                || (bold <= -25f && (HasTraitAnyLevel("patient") || HasTraitAnyLevel("calm") || HasTraitAnyLevel("content")))))
                return EconomicalArchetype.Cautious;

            // 10. 不可预测者：轻浮/疯狂
            if (HasTraitAnyLevel("fickle") || HasTraitAnyLevel("lunatic"))
                return EconomicalArchetype.Unpredictable;

            // 11. 平衡型（默认）
            return EconomicalArchetype.Balanced;
        }

        /// <summary>经济原型的显示文本（如"好战之君（崇尚武力，以战争扩张为第一要务）"）</summary>
        public string GetEconomicalArchetypeDisplay()
        {
            var info = EconomicalArchetypes.Get(DetermineEconomicalArchetype());
            return $"{info.displayName}（{info.description}）";
        }

        /// <summary>角色死亡</summary>
        public void Die(int day, int year, string cause)
        {
            deathDay = day;
            deathYear = year;
            Debug.Log($"[Character] {fullName} 死亡：{cause}，享年{age}岁");
        }

        /// <summary>添加特质</summary>
        public void AddTrait(PersonalityTrait trait)
        {
            if (!traits.Exists(t => t.traitId == trait.traitId))
            {
                traits.Add(trait);
                trait.OnAcquired(this);
            }
        }

        /// <summary>移除特质</summary>
        public void RemoveTrait(string traitId)
        {
            var trait = traits.Find(t => t.traitId == traitId);
            if (trait != null)
            {
                trait.OnRemoved(this);
                traits.Remove(trait);
            }
        }

        /// <summary>修改与另一角色的关系</summary>
        public void ModifyRelation(int otherId, float opinionDelta, string reason)
        {
            if (!relations.TryGetValue(otherId, out var rel))
            {
                rel = new CharacterRelation { otherCharacterId = otherId };
                relations[otherId] = rel;
            }
            rel.opinion = Mathf.Clamp(rel.opinion + opinionDelta, -200f, 200f);
            rel.history.Add($"{reason}: {opinionDelta}");
        }

        // ===== 容量型数值（企划书 9.1：威望/恶名，当前值+容量等级+上限） =====

        /// <summary>威望容量上限（等级 1-5：100/300/600/1000/1500）</summary>
        public float GetPrestigeCapacity()
        {
            return prestigeCapacityLevel switch
            {
                2 => 300f,
                3 => 600f,
                4 => 1000f,
                5 => 1500f,
                _ => 100f
            };
        }

        /// <summary>恶名容量上限（与威望同级）</summary>
        public float GetNotorietyCapacity() => GetPrestigeCapacity();

        /// <summary>修改威望（含容量等级自动维护：达上限升级，低于 30% 降级）</summary>
        public void ModifyPrestige(float delta)
        {
            prestige = Mathf.Clamp(prestige + delta, 0f, GetPrestigeCapacity());
            if (prestige >= GetPrestigeCapacity() && prestigeCapacityLevel < 5)
            {
                prestigeCapacityLevel++;
                Debug.Log($"[Character] {fullName} 威望容量升至 {prestigeCapacityLevel} 级");
            }
            else if (prestige < GetPrestigeCapacity() * 0.3f && prestigeCapacityLevel > 1)
            {
                prestigeCapacityLevel--;
                Debug.Log($"[Character] {fullName} 威望容量降至 {prestigeCapacityLevel} 级");
            }
        }

        /// <summary>修改恶名（容量同威望，不触发等级变化）</summary>
        public void ModifyNotoriety(float delta)
        {
            notoriety = Mathf.Clamp(notoriety + delta, 0f, GetNotorietyCapacity());
        }

        /// <summary>统治类型判定（企划书：威望/恶名组合 → 明君/暴君/昏暴之君/平庸之主）</summary>
        public RulerType GetRulerType()
        {
            bool highP = prestige / Mathf.Max(1f, GetPrestigeCapacity()) > 0.6f;
            bool highN = notoriety / Mathf.Max(1f, GetNotorietyCapacity()) > 0.6f;
            if (highP && highN) return RulerType.TyrantFool;   // 昏暴之君
            if (highP) return RulerType.Benevolent;            // 明君
            if (highN) return RulerType.Tyrant;                // 暴君
            return RulerType.Mediocre;                         // 平庸之主
        }
    }

    /// <summary>角色身份</summary>
    public enum CharacterRole
    {
        Commoner,      // 平民
        Noble,         // 贵族
        Clergy,        // 神职人员
        Merchant,      // 商人
        Military,      // 军人
        Scholar,       // 学者
        Ruler,         // 统治者
        Heir,          // 继承人
        Spouse,        // 配偶
        Courtier       // 廷臣
    }

    /// <summary>统治类型（企划书 9.1：威望/恶名组合）</summary>
    public enum RulerType
    {
        Benevolent,    // 明君：威望高、恶名低
        Tyrant,        // 暴君：恶名高、威望低
        TyrantFool,    // 昏暴之君：威望恶名双高
        Mediocre       // 平庸之主：双低
    }

    /// <summary>
    /// 人格特质
    /// 三层架构：基础特质 → 复合特质 → 文化特质
    /// </summary>
    [System.Serializable]
    public class PersonalityTrait
    {
        public string traitId;
        public string traitName;
        public string description;
        public TraitTier tier = TraitTier.Basic;
        public TraitCategory category = TraitCategory.Personality;

        // 属性修正
        public float martialMod = 0f;
        public float diplomacyMod = 0f;
        public float stewardshipMod = 0f;
        public float intrigueMod = 0f;
        public float learningMod = 0f;
        public float warfareMod = 0f;
        public float charmMod = 0f;       // 魅力修正（与 MentalDisorderDef 字段对齐，统一七维属性修正）

        // 互斥特质
        public List<string> conflictingTraits = new List<string>();

        // 前置特质
        public List<string> requiredTraits = new List<string>();

        /// <summary>获取特质时触发</summary>
        public virtual void OnAcquired(CharacterData character) { }

        /// <summary>失去特质时触发</summary>
        public virtual void OnRemoved(CharacterData character) { }

        /// <summary>每日效果</summary>
        public virtual void ApplyDailyEffect(CharacterData character) { }

        /// <summary>检查是否与角色现有特质冲突</summary>
        public bool IsCompatibleWith(CharacterData character)
        {
            foreach (var existing in character.traits)
            {
                if (conflictingTraits.Contains(existing.traitId))
                    return false;
            }
            return true;
        }
    }

    public enum TraitTier
    {
        Basic,      // 基础特质
        Complex,    // 复合特质（由基础特质组合）
        Cultural    // 文化特质（文化专属）
    }

    public enum TraitCategory
    {
        Personality,  // 性格
        Lifestyle,    // 生活方式
        Education,    // 教育背景
        Physical,     // 身体特征
        Mental,       // 心理特征
        Reputation,   // 声望特质
        Religious     // 宗教特质
    }

    /// <summary>
    /// 性格标签定义表（对齐 CK3 原版十余个性格特质 × MPD 三级递进）。
    /// 每个标签是独立的 PersonalityTrait，直接显示在人物界面；三级之间用 requiredTraits 表示递进（L1→L2→L3），
    /// 对立特质用 conflictingTraits 互斥。不搞从七维连续值推导标签的运行时计算。
    /// 新增标签只需在此数组追加。
    /// </summary>
    public static class PersonalityTraitDatabase
    {
                public static readonly PersonalityTrait[] All =
        {
            // ===== 勇气 brave（L1勇气/大胆 · L2勇敢 · L3无畏）=====
            new PersonalityTrait { traitId="brave_1", traitName="勇气", description="大胆——当周围大多数人退缩时，却愿意迎难而上的意志",
                category=TraitCategory.Personality, martialMod=1f, warfareMod=1f,
                conflictingTraits=new List<string>{"craven_1","craven_2","craven_3"} },
            new PersonalityTrait { traitId="brave_2", traitName="勇敢", description="大胆——勇气是宫闱轶事中不可或缺的；战斗以其名字被铭记",
                category=TraitCategory.Personality, martialMod=2f, warfareMod=2f,
                conflictingTraits=new List<string>{"craven_1","craven_2","craven_3"},
                requiredTraits=new List<string>{"brave_1"} },
            new PersonalityTrait { traitId="brave_3", traitName="无畏", description="大胆——迎战险境，如同他人追求爱人一般；战场上罕有人能望其项背",
                category=TraitCategory.Personality, martialMod=3f, warfareMod=3f,
                conflictingTraits=new List<string>{"craven_1","craven_2","craven_3"},
                requiredTraits=new List<string>{"brave_2"} },
            // ===== 慎重 craven（L1慎重/小心 · L2谨慎 · L3怯懦）=====
            new PersonalityTrait { traitId="craven_1", traitName="慎重", description="小心——对风险、危险与对抗的回避，在压力下呼声愈高",
                category=TraitCategory.Personality, martialMod=-1f, intrigueMod=1f,
                conflictingTraits=new List<string>{"brave_1","brave_2","brave_3"} },
            new PersonalityTrait { traitId="craven_2", traitName="谨慎", description="小心——慎重是出了名的；战事都安排给别人去统领",
                category=TraitCategory.Personality, martialMod=-2f, intrigueMod=2f,
                conflictingTraits=new List<string>{"brave_1","brave_2","brave_3"},
                requiredTraits=new List<string>{"craven_1"} },
            new PersonalityTrait { traitId="craven_3", traitName="怯懦", description="小心——无论如何都不肯冒险；领地因此蒙受了无数种隐而未显的损失",
                category=TraitCategory.Personality, martialMod=-3f, intrigueMod=3f,
                conflictingTraits=new List<string>{"brave_1","brave_2","brave_3"},
                requiredTraits=new List<string>{"craven_2"} },
            // ===== 同情心 compassionate（L1同情心/善良 · L2慈悲 · L3圣母般）=====
            new PersonalityTrait { traitId="compassionate_1", traitName="同情心", description="善良——向有需要之人伸出援手，不计回报",
                category=TraitCategory.Personality, diplomacyMod=1f, charmMod=1f,
                conflictingTraits=new List<string>{"callous_1","callous_2","callous_3"} },
            new PersonalityTrait { traitId="compassionate_2", traitName="慈悲", description="善良——仁慈成了此地景象；灾荒年月，郡中穷苦人上门求助",
                category=TraitCategory.Personality, diplomacyMod=2f, charmMod=2f,
                conflictingTraits=new List<string>{"callous_1","callous_2","callous_3"},
                requiredTraits=new List<string>{"compassionate_1"} },
            new PersonalityTrait { traitId="compassionate_3", traitName="圣母般", description="善良——施予之多，已超出府中所能承受；大门向所有来者敞开",
                category=TraitCategory.Personality, diplomacyMod=3f, charmMod=3f,
                conflictingTraits=new List<string>{"callous_1","callous_2","callous_3"},
                requiredTraits=new List<string>{"compassionate_2"} },
            // ===== 冷漠 callous（L1冷漠/疏离 · L2无情 · L3铁石心肠）=====
            new PersonalityTrait { traitId="callous_1", traitName="冷漠", description="疏离——对不直接关系到自己的痛苦十分冷淡",
                category=TraitCategory.Personality, intrigueMod=1f, martialMod=1f,
                conflictingTraits=new List<string>{"compassionate_1","compassionate_2","compassionate_3"} },
            new PersonalityTrait { traitId="callous_2", traitName="无情", description="疏离——漠然在每次觐见中都明明白白；泪水无法打动他们",
                category=TraitCategory.Personality, intrigueMod=2f, martialMod=2f,
                conflictingTraits=new List<string>{"compassionate_1","compassionate_2","compassionate_3"},
                requiredTraits=new List<string>{"callous_1"} },
            new PersonalityTrait { traitId="callous_3", traitName="铁石心肠", description="疏离——已全然不再假装置身于仁慈；死刑令的签署也懒得多看一眼名字",
                category=TraitCategory.Personality, intrigueMod=3f, martialMod=3f,
                conflictingTraits=new List<string>{"compassionate_1","compassionate_2","compassionate_3"},
                requiredTraits=new List<string>{"callous_2"} },
            // ===== 贪念 greedy（L1贪念/精打细算 · L2贪婪 · L3贪得无厌）=====
            new PersonalityTrait { traitId="greedy_1", traitName="贪念", description="精打细算——对钱币与财物的抓攫，随着每笔交易愈发沉重",
                category=TraitCategory.Personality, stewardshipMod=1f, diplomacyMod=-1f,
                conflictingTraits=new List<string>{"generous_1","generous_2","generous_3"} },
            new PersonalityTrait { traitId="greedy_2", traitName="贪婪", description="精打细算——对财富的追逐形塑着每次觐见；礼物在收下之前先被称量",
                category=TraitCategory.Personality, stewardshipMod=2f, diplomacyMod=-2f,
                conflictingTraits=new List<string>{"generous_1","generous_2","generous_3"},
                requiredTraits=new List<string>{"greedy_1"} },
            new PersonalityTrait { traitId="greedy_3", traitName="贪得无厌", description="精打细算——已将国库榨干却浑然不觉；友谊本身也成了一桩交易",
                category=TraitCategory.Personality, stewardshipMod=3f, diplomacyMod=-3f,
                conflictingTraits=new List<string>{"generous_1","generous_2","generous_3"},
                requiredTraits=new List<string>{"greedy_2"} },
            // ===== 慷慨 generous（L1慷慨/乐善 · L2好施 · L3博施）=====
            new PersonalityTrait { traitId="generous_1", traitName="慷慨", description="乐善——对黄金、礼物与恩惠的洒脱之手，以及不求回报施予",
                category=TraitCategory.Personality, diplomacyMod=1f, stewardshipMod=-1f,
                conflictingTraits=new List<string>{"greedy_1","greedy_2","greedy_3"} },
            new PersonalityTrait { traitId="generous_2", traitName="好施", description="乐善——赈济在本地教堂里被称颂；上门的请愿者们很少空手而归",
                category=TraitCategory.Personality, diplomacyMod=2f, stewardshipMod=-2f,
                conflictingTraits=new List<string>{"greedy_1","greedy_2","greedy_3"},
                requiredTraits=new List<string>{"generous_1"} },
            new PersonalityTrait { traitId="generous_3", traitName="博施", description="乐善——不顾一切劝诫，施予无度；国库是拿来尊荣旁人的手段",
                category=TraitCategory.Personality, diplomacyMod=3f, stewardshipMod=-3f,
                conflictingTraits=new List<string>{"greedy_1","greedy_2","greedy_3"},
                requiredTraits=new List<string>{"generous_2"} },
            // ===== 诚信 honest（L1诚信/直率 · L2诚实 · L3坦率无隐）=====
            new PersonalityTrait { traitId="honest_1", traitName="诚信", description="直率——拒绝粉饰真相，即使礼节或自身利益要求如此",
                category=TraitCategory.Personality, diplomacyMod=1f, intrigueMod=-1f,
                conflictingTraits=new List<string>{"deceitful_1","deceitful_2","deceitful_3"} },
            new PersonalityTrait { traitId="honest_2", traitName="诚实", description="直率——以诚实的品格在宫廷中著称；对手们畏惧他那些毫不留情的质问",
                category=TraitCategory.Personality, diplomacyMod=2f, intrigueMod=-2f,
                conflictingTraits=new List<string>{"deceitful_1","deceitful_2","deceitful_3"},
                requiredTraits=new List<string>{"honest_1"} },
            new PersonalityTrait { traitId="honest_3", traitName="坦率无隐", description="直率——即使出于体面，也无法掩饰其坦率；君王们曾被当面指出过自己的过失",
                category=TraitCategory.Personality, diplomacyMod=3f, intrigueMod=-3f,
                conflictingTraits=new List<string>{"deceitful_1","deceitful_2","deceitful_3"},
                requiredTraits=new List<string>{"honest_2"} },
            // ===== 狡诈 deceitful（L1狡诈/机灵 · L2狡猾 · L3背信弃义）=====
            new PersonalityTrait { traitId="deceitful_1", traitName="狡诈", description="机灵——对谎言、半真半假的话和恰到好处的沉默驾轻就熟",
                category=TraitCategory.Personality, intrigueMod=2f, diplomacyMod=-1f,
                conflictingTraits=new List<string>{"honest_1","honest_2","honest_3"} },
            new PersonalityTrait { traitId="deceitful_2", traitName="狡猾", description="机灵——诡计多端的名声并非空穴来风；协议必被细读，誓言也需证人衡量",
                category=TraitCategory.Personality, intrigueMod=3f, diplomacyMod=-2f,
                conflictingTraits=new List<string>{"honest_1","honest_2","honest_3"},
                requiredTraits=new List<string>{"deceitful_1"} },
            new PersonalityTrait { traitId="deceitful_3", traitName="背信弃义", description="机灵——既不能托付密封的信件，也不能指望他信守承诺；每句话都像棋盘上的一步棋",
                category=TraitCategory.Personality, intrigueMod=4f, diplomacyMod=-3f,
                conflictingTraits=new List<string>{"honest_1","honest_2","honest_3"},
                requiredTraits=new List<string>{"deceitful_2"} },
            // ===== 平和 calm（L1平和/从容 · L2冷静 · L3心如止水）=====
            new PersonalityTrait { traitId="calm_1", traitName="平和", description="从容——一种沉稳的性情，即使面对挑衅也能保持泰然自若",
                category=TraitCategory.Personality, learningMod=1f, intrigueMod=1f,
                conflictingTraits=new List<string>{"wrathful_1","wrathful_2","wrathful_3"} },
            new PersonalityTrait { traitId="calm_2", traitName="冷静", description="从容——沉着是御前会议的定海神针；是房间里紧张气氛的逐渐降温",
                category=TraitCategory.Personality, learningMod=2f, intrigueMod=2f,
                conflictingTraits=new List<string>{"wrathful_1","wrathful_2","wrathful_3"},
                requiredTraits=new List<string>{"calm_1"} },
            new PersonalityTrait { traitId="calm_3", traitName="心如止水", description="从容——面对足以击垮他人的消息仍岿然不动；战祸、瘟疫与背叛递到同一张平静的面孔前",
                category=TraitCategory.Personality, learningMod=3f, intrigueMod=3f,
                conflictingTraits=new List<string>{"wrathful_1","wrathful_2","wrathful_3"},
                requiredTraits=new List<string>{"calm_2"} },
            // ===== 怒火 wrathful（L1怒火/性急 · L2易怒 · L3狂怒）=====
            new PersonalityTrait { traitId="wrathful_1", traitName="怒火", description="性急——一腔过于易燃又冷却太慢的脾气，在宫廷日常事务上留下焦痕",
                category=TraitCategory.Personality, martialMod=1f, diplomacyMod=-1f,
                conflictingTraits=new List<string>{"calm_1","calm_2","calm_3"} },
            new PersonalityTrait { traitId="wrathful_2", traitName="易怒", description="性急——怒气在廷中无人不知；仆从们步步小心，所受的轻蔑也鲜少被遗忘",
                category=TraitCategory.Personality, martialMod=2f, diplomacyMod=-2f,
                conflictingTraits=new List<string>{"calm_1","calm_2","calm_3"},
                requiredTraits=new List<string>{"wrathful_1"} },
            new PersonalityTrait { traitId="wrathful_3", traitName="狂怒", description="性急——暴怒已是低声相传的传奇；就连忠勇之士也学会了辨识风暴前的沉寂",
                category=TraitCategory.Personality, martialMod=3f, diplomacyMod=-3f,
                conflictingTraits=new List<string>{"calm_1","calm_2","calm_3"},
                requiredTraits=new List<string>{"wrathful_2"} },
            // ===== 热枕 zealous（L1热枕/虔诚 · L2狂热 · L3盲信）=====
            new PersonalityTrait { traitId="zealous_1", traitName="热枕", description="虔诚——对信仰怀有炽烈的确信，不容妥协，也几乎不能容忍怀疑",
                category=TraitCategory.Personality, learningMod=1f, warfareMod=1f,
                conflictingTraits=new List<string>{"cynical_1","cynical_2","cynical_3"} },
            new PersonalityTrait { traitId="zealous_2", traitName="狂热", description="虔诚——虔诚是整个家府的界碑；每逢瞻礼教堂必满，谈话也常绕回教义",
                category=TraitCategory.Personality, learningMod=2f, warfareMod=2f,
                conflictingTraits=new List<string>{"cynical_1","cynical_2","cynical_3"},
                requiredTraits=new List<string>{"zealous_1"} },
            new PersonalityTrait { traitId="zealous_3", traitName="盲信", description="虔诚——信仰炽烈，不容半分妥协；异端绝不容忍，即便是多年盟友也要按教义问答衡量",
                category=TraitCategory.Personality, learningMod=3f, warfareMod=3f,
                conflictingTraits=new List<string>{"cynical_1","cynical_2","cynical_3"},
                requiredTraits=new List<string>{"zealous_2"} },
            // ===== 犬儒主义 cynical（L1犬儒主义/不轻信 · L2愤世嫉俗 · L3虚无）=====
            new PersonalityTrait { traitId="cynical_1", traitName="犬儒主义", description="不轻信——对崇高目的抱有怀疑，并私下把每一种动机都解读为更卑劣的东西",
                category=TraitCategory.Personality, intrigueMod=1f, learningMod=-1f,
                conflictingTraits=new List<string>{"zealous_1","zealous_2","zealous_3"} },
            new PersonalityTrait { traitId="cynical_2", traitName="愤世嫉俗", description="不轻信——怀疑态度早已为人所知；神职人员害怕他的提问，誓言换来的只是淡淡一笑",
                category=TraitCategory.Personality, intrigueMod=2f, learningMod=-2f,
                conflictingTraits=new List<string>{"zealous_1","zealous_2","zealous_3"},
                requiredTraits=new List<string>{"cynical_1"} },
            new PersonalityTrait { traitId="cynical_3", traitName="虚无", description="不轻信——看不到欲望与私利之外的任何目的；信仰仪式也像忍受四季更替一样耐着性子遵守",
                category=TraitCategory.Personality, intrigueMod=3f, learningMod=-3f,
                conflictingTraits=new List<string>{"zealous_1","zealous_2","zealous_3"},
                requiredTraits=new List<string>{"cynical_2"} },
            // ===== 野心 ambitious（L1野心/渴望上进 · L2野心勃勃 · L3利欲熏心）=====
            new PersonalityTrait { traitId="ambitious_1", traitName="野心", description="渴望上进——一种不甘于现状，渴望更高职位、头衔或声望的追求",
                category=TraitCategory.Personality, warfareMod=1f, diplomacyMod=1f, stewardshipMod=-1f,
                conflictingTraits=new List<string>{"content_1","content_2","content_3"} },
            new PersonalityTrait { traitId="ambitious_2", traitName="野心勃勃", description="渴望上进——野心明眼人都看得出来；权衡联盟时看重影响范围，安排婚姻时看重机会",
                category=TraitCategory.Personality, warfareMod=2f, diplomacyMod=2f, stewardshipMod=-2f,
                conflictingTraits=new List<string>{"content_1","content_2","content_3"},
                requiredTraits=new List<string>{"ambitious_1"} },
            new PersonalityTrait { traitId="ambitious_3", traitName="利欲熏心", description="渴望上进——全然受晋身高位之念所役使；每一场筵席、征战、恩惠皆被用作向上攀爬的阶梯",
                category=TraitCategory.Personality, warfareMod=3f, diplomacyMod=3f, stewardshipMod=-3f,
                conflictingTraits=new List<string>{"content_1","content_2","content_3"},
                requiredTraits=new List<string>{"ambitious_2"} },
            // ===== 满足感 content（L1满足感/满意 · L2知足 · L3安于现状）=====
            new PersonalityTrait { traitId="content_1", traitName="满足感", description="满意——安然承受自身的命数，不再向门第或命运索取已被赐予之外的东西",
                category=TraitCategory.Personality, stewardshipMod=1f, diplomacyMod=1f, warfareMod=-1f,
                conflictingTraits=new List<string>{"ambitious_1","ambitious_2","ambitious_3"} },
            new PersonalityTrait { traitId="content_2", traitName="知足", description="满意——知足之名远近皆知；凡有晋身之邀皆谢而辞之；账册所计不过来岁收支",
                category=TraitCategory.Personality, stewardshipMod=2f, diplomacyMod=2f, warfareMod=-2f,
                conflictingTraits=new List<string>{"ambitious_1","ambitious_2","ambitious_3"},
                requiredTraits=new List<string>{"content_1"} },
            new PersonalityTrait { traitId="content_3", traitName="安于现状", description="满意——任凭何等诱引，亦不能使其动念求荣；帝国在其周遭兴亡更替，心神仍安放于旧日炉火之前",
                category=TraitCategory.Personality, stewardshipMod=3f, diplomacyMod=3f, warfareMod=-3f,
                conflictingTraits=new List<string>{"ambitious_1","ambitious_2","ambitious_3"},
                requiredTraits=new List<string>{"content_2"} },
            // ===== 勤勉 diligent（L1勤勉/尽责 · L2勤恳 · L3宵衣旰食）=====
            new PersonalityTrait { traitId="diligent_1", traitName="勤勉", description="尽责——不论心境与阴晴，对工作恒久不懈的投入",
                category=TraitCategory.Personality, stewardshipMod=1f, learningMod=1f,
                conflictingTraits=new List<string>{"lazy_1","lazy_2","lazy_3"} },
            new PersonalityTrait { traitId="diligent_2", traitName="勤恳", description="尽责——勤恳是宫廷的动力；无一事被遗忘，无一份请愿不被答复",
                category=TraitCategory.Personality, stewardshipMod=2f, learningMod=2f,
                conflictingTraits=new List<string>{"lazy_1","lazy_2","lazy_3"},
                requiredTraits=new List<string>{"diligent_1"} },
            new PersonalityTrait { traitId="diligent_3", traitName="宵衣旰食", description="尽责——每夜劳作到超出所有合理时刻，劝也劝不住；城堡其余角落早已沉寂，房厅灯火依旧亮着",
                category=TraitCategory.Personality, stewardshipMod=3f, learningMod=3f,
                conflictingTraits=new List<string>{"lazy_1","lazy_2","lazy_3"},
                requiredTraits=new List<string>{"diligent_2"} },
            // ===== 惰性 lazy（L1惰性/闲散 · L2懒惰 · L3怠惰成性）=====
            new PersonalityTrait { traitId="lazy_1", traitName="惰性", description="闲散——当他人之手可代劳时，自己便不愿起身、决断或劳作",
                category=TraitCategory.Personality, stewardshipMod=-1f, learningMod=-1f, intrigueMod=1f,
                conflictingTraits=new List<string>{"diligent_1","diligent_2","diligent_3"} },
            new PersonalityTrait { traitId="lazy_2", traitName="懒惰", description="闲散——厌劳人尽皆知；臣属们学会了将决议拟好大半再呈上",
                category=TraitCategory.Personality, stewardshipMod=-2f, learningMod=-2f, intrigueMod=2f,
                conflictingTraits=new List<string>{"diligent_1","diligent_2","diligent_3"},
                requiredTraits=new List<string>{"lazy_1"} },
            new PersonalityTrait { traitId="lazy_3", traitName="怠惰成性", description="闲散——已数月未签一纸谕令；信使候在门口，案头积满了灰，封地全赖旁人勉力维系",
                category=TraitCategory.Personality, stewardshipMod=-3f, learningMod=-3f, intrigueMod=3f,
                conflictingTraits=new List<string>{"diligent_1","diligent_2","diligent_3"},
                requiredTraits=new List<string>{"lazy_2"} },
            // ===== 耐性 patient（L1耐性/宽忍 · L2耐心 · L3坚忍）=====
            new PersonalityTrait { traitId="patient_1", traitName="耐性", description="宽忍——情愿等待、忍耐，让事态顺其自身的节奏展开",
                category=TraitCategory.Personality, learningMod=1f, diplomacyMod=1f, martialMod=-1f,
                conflictingTraits=new List<string>{"impatient_1","impatient_2","impatient_3"} },
            new PersonalityTrait { traitId="patient_2", traitName="耐心", description="宽忍——耐心是治理的工具；指望速速作答的对手，往往被审慎节奏弄得狼狈不堪",
                category=TraitCategory.Personality, learningMod=2f, diplomacyMod=2f, martialMod=-2f,
                conflictingTraits=new List<string>{"impatient_1","impatient_2","impatient_3"},
                requiredTraits=new List<string>{"patient_1"} },
            new PersonalityTrait { traitId="patient_3", traitName="坚忍", description="宽忍——之所以能比一切都更持久——无论是恩怨、争执，还是对手的寿命——仅仅是因为拒绝被催促",
                category=TraitCategory.Personality, learningMod=3f, diplomacyMod=3f, martialMod=-3f,
                conflictingTraits=new List<string>{"impatient_1","impatient_2","impatient_3"},
                requiredTraits=new List<string>{"patient_2"} },
            // ===== 不耐烦 impatient（L1不耐烦/毛躁 · L2急躁 · L3草率）=====
            new PersonalityTrait { traitId="impatient_1", traitName="不耐烦", description="毛躁——一股迫切的去行动、决断，或是在别人还没把话说完前就打断对方的冲动",
                category=TraitCategory.Personality, martialMod=1f, warfareMod=1f, diplomacyMod=-1f,
                conflictingTraits=new List<string>{"patient_1","patient_2","patient_3"} },
            new PersonalityTrait { traitId="impatient_2", traitName="急躁", description="毛躁——急躁推动着整个宫廷的节奏；臣属们学会了简明扼要地呈报问题",
                category=TraitCategory.Personality, martialMod=2f, warfareMod=2f, diplomacyMod=-2f,
                conflictingTraits=new List<string>{"patient_1","patient_2","patient_3"},
                requiredTraits=new List<string>{"impatient_1"} },
            new PersonalityTrait { traitId="impatient_3", traitName="草率", description="毛躁——无法忍受漫长的审议；御前会议头一轮反对还没说完，决断便已做出，事后犹豫已为时已晚",
                category=TraitCategory.Personality, martialMod=3f, warfareMod=3f, diplomacyMod=-3f,
                conflictingTraits=new List<string>{"patient_1","patient_2","patient_3"},
                requiredTraits=new List<string>{"impatient_2"} },
            // ===== 傲慢 arrogant（L1傲慢/自负 · L2狂妄 · L3目中无人）=====
            new PersonalityTrait { traitId="arrogant_1", traitName="傲慢", description="自负——对自身价值的高估，渗入到与各个层级的每一次互动中",
                category=TraitCategory.Personality, martialMod=1f, diplomacyMod=-1f, charmMod=-1f,
                conflictingTraits=new List<string>{"humble_1","humble_2","humble_3"} },
            new PersonalityTrait { traitId="arrogant_2", traitName="狂妄", description="自负——傲慢在每次觐见中都显露无遗；对地位较低者态度冷淡，对地位相当者通过言辞提醒安分守己",
                category=TraitCategory.Personality, martialMod=2f, diplomacyMod=-2f, charmMod=-2f,
                conflictingTraits=new List<string>{"humble_1","humble_2","humble_3"},
                requiredTraits=new List<string>{"arrogant_1"} },
            new PersonalityTrait { traitId="arrogant_3", traitName="目中无人", description="自负——将自己凌驾于所有人之上；就连宣誓效忠的领主也必须毕恭毕敬，任何轻慢之举绝不容忍",
                category=TraitCategory.Personality, martialMod=3f, diplomacyMod=-3f, charmMod=-3f,
                conflictingTraits=new List<string>{"humble_1","humble_2","humble_3"},
                requiredTraits=new List<string>{"arrogant_2"} },
            // ===== 谦卑 humble（L1谦卑/谦逊 · L2谦卑 · L3深藏若虚）=====
            new PersonalityTrait { traitId="humble_1", traitName="谦卑", description="谦逊——对赞誉与褒奖安之若素地推却，哪怕那是应得的",
                category=TraitCategory.Personality, diplomacyMod=1f, charmMod=1f, learningMod=1f,
                conflictingTraits=new List<string>{"arrogant_1","arrogant_2","arrogant_3"} },
            new PersonalityTrait { traitId="humble_2", traitName="谦卑", description="谦逊——谦逊广受称道；请愿者面对的是一个不拘礼节的倾听者，胜利功劳全数归于旁人",
                category=TraitCategory.Personality, diplomacyMod=2f, charmMod=2f, learningMod=2f,
                conflictingTraits=new List<string>{"arrogant_1","arrogant_2","arrogant_3"},
                requiredTraits=new List<string>{"humble_1"} },
            new PersonalityTrait { traitId="humble_3", traitName="深藏若虚", description="谦逊——无人能劝其接受荣耀；以其自身名义竖立的雕像至今蒙尘，编年史家只能在他人催促下动笔",
                category=TraitCategory.Personality, diplomacyMod=3f, charmMod=3f, learningMod=3f,
                conflictingTraits=new List<string>{"arrogant_1","arrogant_2","arrogant_3"},
                requiredTraits=new List<string>{"humble_2"} },
            // ===== 疑心 paranoid（L1疑心/警觉 · L2多疑 · L3疑神疑鬼）=====
            new PersonalityTrait { traitId="paranoid_1", traitName="疑心", description="警觉——将世间视作仇敌环伺，每阵沉默都读作密谋",
                category=TraitCategory.Personality, intrigueMod=1f, learningMod=1f, diplomacyMod=-1f,
                conflictingTraits=new List<string>{"trusting_1","trusting_2","trusting_3"} },
            new PersonalityTrait { traitId="paranoid_2", traitName="多疑", description="警觉——疑心已经影响到城堡的日常运转；食物要先经人试尝，信件要读上两遍",
                category=TraitCategory.Personality, intrigueMod=2f, learningMod=2f, diplomacyMod=-2f,
                conflictingTraits=new List<string>{"trusting_1","trusting_2","trusting_3"},
                requiredTraits=new List<string>{"paranoid_1"} },
            new PersonalityTrait { traitId="paranoid_3", traitName="疑神疑鬼", description="警觉——在每一道阴影中都窥见阴谋；卫兵增至三倍，廷臣们不敢两两低语，最真诚的朋友也不再被信任",
                category=TraitCategory.Personality, intrigueMod=3f, learningMod=3f, diplomacyMod=-3f,
                conflictingTraits=new List<string>{"trusting_1","trusting_2","trusting_3"},
                requiredTraits=new List<string>{"paranoid_2"} },
            // ===== 相信度 trusting（L1相信度/坦诚 · L2轻信他人 · L3天真）=====
            new PersonalityTrait { traitId="trusting_1", traitName="相信度", description="坦诚——相信他人的善言与善意，即使证据本不该如此薄弱",
                category=TraitCategory.Personality, diplomacyMod=1f, charmMod=1f, intrigueMod=-1f,
                conflictingTraits=new List<string>{"paranoid_1","paranoid_2","paranoid_3"} },
            new PersonalityTrait { traitId="trusting_2", traitName="轻信他人", description="坦诚——坦诚广为人知；顾问们被给予很大的自由，针对亲近伙伴的指控往往被挥手遗忘",
                category=TraitCategory.Personality, diplomacyMod=2f, charmMod=2f, intrigueMod=-2f,
                conflictingTraits=new List<string>{"paranoid_1","paranoid_2","paranoid_3"},
                requiredTraits=new List<string>{"trusting_1"} },
            new PersonalityTrait { traitId="trusting_3", traitName="天真", description="坦诚——对他人的信任已经成了领地承受不起的弱点；廷臣们公然利用这一点，只有核心忠臣还在保护他",
                category=TraitCategory.Personality, diplomacyMod=3f, charmMod=3f, intrigueMod=-3f,
                conflictingTraits=new List<string>{"paranoid_1","paranoid_2","paranoid_3"},
                requiredTraits=new List<string>{"trusting_2"} },
            // ===== 社交能力 gregarious（L1社交能力/善交际 · L2合群 · L3热情洋溢）=====
            new PersonalityTrait { traitId="gregarious_1", traitName="社交能力", description="善交际——对陪伴、交谈和热闹大厅里那份温暖的向往",
                category=TraitCategory.Personality, diplomacyMod=1f, charmMod=1f, stewardshipMod=1f,
                conflictingTraits=new List<string>{"shy_1","shy_2","shy_3"} },
            new PersonalityTrait { traitId="gregarious_2", traitName="合群", description="善交际——热衷于待客，塑造了整个家庭的氛围；城堡里几乎总是宾客盈门",
                category=TraitCategory.Personality, diplomacyMod=2f, charmMod=2f, stewardshipMod=2f,
                conflictingTraits=new List<string>{"shy_1","shy_2","shy_3"},
                requiredTraits=new List<string>{"gregarious_1"} },
            new PersonalityTrait { traitId="gregarious_3", traitName="热情洋溢", description="善交际——无法忍受空荡荡的大厅；这里总是盛宴不断，屋檐下总有来客，寂静对他来说像外语一样陌生",
                category=TraitCategory.Personality, diplomacyMod=3f, charmMod=3f, stewardshipMod=3f,
                conflictingTraits=new List<string>{"shy_1","shy_2","shy_3"},
                requiredTraits=new List<string>{"gregarious_2"} },
            // ===== 羞涩 shy（L1羞涩/腼腆 · L2害羞 · L3孤僻）=====
            new PersonalityTrait { traitId="shy_1", traitName="羞涩", description="腼腆——对陌生人的注视和拥挤厅堂的压迫感到的不适",
                category=TraitCategory.Personality, learningMod=1f, intrigueMod=1f, diplomacyMod=-1f,
                conflictingTraits=new List<string>{"gregarious_1","gregarious_2","gregarious_3"} },
            new PersonalityTrait { traitId="shy_2", traitName="害羞", description="腼腆——缄默是宫廷里流传的评价；求见者会被委婉地指导如何接近他",
                category=TraitCategory.Personality, learningMod=2f, intrigueMod=2f, diplomacyMod=-2f,
                conflictingTraits=new List<string>{"gregarious_1","gregarious_2","gregarious_3"},
                requiredTraits=new List<string>{"shy_1"} },
            new PersonalityTrait { traitId="shy_3", traitName="孤僻", description="腼腆——可以完全避开人群，只要条件允许；书信代替了演讲，安静房间里一位访客便足以度过一天",
                category=TraitCategory.Personality, learningMod=3f, intrigueMod=3f, diplomacyMod=-3f,
                conflictingTraits=new List<string>{"gregarious_1","gregarious_2","gregarious_3"},
                requiredTraits=new List<string>{"shy_2"} },
            // ===== 报复欲 vengeful（L1报复欲/记仇 · L2有仇必报 · L3睚眦必报）=====
            new PersonalityTrait { traitId="vengeful_1", traitName="报复欲", description="记仇——对被冒犯记忆悠长，讨还公道的耐心更持久，不论其间相隔多少年",
                category=TraitCategory.Personality, intrigueMod=1f, martialMod=1f,
                conflictingTraits=new List<string>{"forgiving_1","forgiving_2","forgiving_3"} },
            new PersonalityTrait { traitId="vengeful_2", traitName="有仇必报", description="记仇——仇怨远近皆知；旧敌恐惧自己落入其掌握的一天，微小的怠慢时机一到也会得到回应",
                category=TraitCategory.Personality, intrigueMod=2f, martialMod=2f,
                conflictingTraits=new List<string>{"forgiving_1","forgiving_2","forgiving_3"},
                requiredTraits=new List<string>{"vengeful_1"} },
            new PersonalityTrait { traitId="vengeful_3", traitName="睚眦必报", description="记仇——将数十年前的侮辱都已讨还，据称保有一整本未了仇怨的账簿；伸手可及范围内没有仇敌能安生度日",
                category=TraitCategory.Personality, intrigueMod=3f, martialMod=3f,
                conflictingTraits=new List<string>{"forgiving_1","forgiving_2","forgiving_3"},
                requiredTraits=new List<string>{"vengeful_2"} },
            // ===== 宽容心 forgiving（L1宽容心/宽宥 · L2宽恕 · L3宽宏大量）=====
            new PersonalityTrait { traitId="forgiving_1", traitName="宽容心", description="宽宥——愿意放下怨怼，让昨日的过错留在昨日",
                category=TraitCategory.Personality, diplomacyMod=1f, charmMod=1f,
                conflictingTraits=new List<string>{"vengeful_1","vengeful_2","vengeful_3"} },
            new PersonalityTrait { traitId="forgiving_2", traitName="宽恕", description="宽宥——宽大为怀是宫廷中有名一幕；请愿者来时旧已有罪却已被饶恕，一场叛乱都能以归还头衔结束",
                category=TraitCategory.Personality, diplomacyMod=2f, charmMod=2f,
                conflictingTraits=new List<string>{"vengeful_1","vengeful_2","vengeful_3"},
                requiredTraits=new List<string>{"forgiving_1"} },
            new PersonalityTrait { traitId="forgiving_3", traitName="宽宏大量", description="宽宥——几乎本能地宽恕他人；即便是重大的背叛，也会以归还土地和一顿安静的饭食作为回应",
                category=TraitCategory.Personality, diplomacyMod=3f, charmMod=3f,
                conflictingTraits=new List<string>{"vengeful_1","vengeful_2","vengeful_3"},
                requiredTraits=new List<string>{"forgiving_2"} },
            // ===== 色欲 lustful（L1色欲/轻媚 · L2好色 · L3荒淫）=====
            new PersonalityTrait { traitId="lustful_1", traitName="色欲", description="轻媚——对肉体欢愉的贪求，渗入每一次凝视与每一句挑逗之中",
                category=TraitCategory.Personality, diplomacyMod=1f, charmMod=1f,
                conflictingTraits=new List<string>{"chaste_1","chaste_2","chaste_3"} },
            new PersonalityTrait { traitId="lustful_2", traitName="好色", description="轻媚——好色之名传遍宫廷；情人的名单比臣属的名册还长，婚誓不过是一纸建议",
                category=TraitCategory.Personality, diplomacyMod=2f, charmMod=2f,
                conflictingTraits=new List<string>{"chaste_1","chaste_2","chaste_3"},
                requiredTraits=new List<string>{"lustful_1"} },
            new PersonalityTrait { traitId="lustful_3", traitName="荒淫", description="轻媚——已将床笫之事抬升到国策的高度；使节来朝先被引至内室，联姻的意义只剩洞房那一夜",
                category=TraitCategory.Personality, diplomacyMod=3f, charmMod=3f,
                conflictingTraits=new List<string>{"chaste_1","chaste_2","chaste_3"},
                requiredTraits=new List<string>{"lustful_2"} },
            // ===== 贞洁 chaste（L1贞洁/矜持 · L2守贞 · L3禁欲）=====
            new PersonalityTrait { traitId="chaste_1", traitName="贞洁", description="矜持——对肉体欲望的克制，以及对忠诚与纯洁之德的珍视",
                category=TraitCategory.Personality, learningMod=1f, diplomacyMod=1f,
                conflictingTraits=new List<string>{"lustful_1","lustful_2","lustful_3"} },
            new PersonalityTrait { traitId="chaste_2", traitName="守贞", description="矜持——守贞之名远近皆知；宫廷里不见暧昧的眼色，婚誓被视作不可触碰的圣约",
                category=TraitCategory.Personality, learningMod=2f, diplomacyMod=2f,
                conflictingTraits=new List<string>{"lustful_1","lustful_2","lustful_3"},
                requiredTraits=new List<string>{"chaste_1"} },
            new PersonalityTrait { traitId="chaste_3", traitName="禁欲", description="矜持——已将肉体视作灵魂的牢笼；宫廷里连夫妻间的温存都被视作堕落，独身成了唯一的圣洁",
                category=TraitCategory.Personality, learningMod=3f, diplomacyMod=3f,
                conflictingTraits=new List<string>{"lustful_1","lustful_2","lustful_3"},
                requiredTraits=new List<string>{"chaste_2"} },
            // ===== 贪吃 gluttonous（L1贪吃/饕客 · L2暴食 · L3无餍）=====
            new PersonalityTrait { traitId="gluttonous_1", traitName="贪吃", description="饕客——对美食与佳酿的过度热爱，餐桌成了一天中最重要的场合",
                category=TraitCategory.Personality, stewardshipMod=-1f, diplomacyMod=1f,
                conflictingTraits=new List<string>{"temperate_1","temperate_2","temperate_3"} },
            new PersonalityTrait { traitId="gluttonous_2", traitName="暴食", description="饕客——暴食是宫廷里的笑谈也是传奇；宴席从正午持续到深夜，国库的一半填进了厨房",
                category=TraitCategory.Personality, stewardshipMod=-2f, diplomacyMod=2f,
                conflictingTraits=new List<string>{"temperate_1","temperate_2","temperate_3"},
                requiredTraits=new List<string>{"gluttonous_1"} },
            new PersonalityTrait { traitId="gluttonous_3", traitName="无餍", description="饕客——已将吃升华为一种仪式；御厨比宰相更有权势，饥荒之年宫廷的宴席依旧如山",
                category=TraitCategory.Personality, stewardshipMod=-3f, diplomacyMod=3f,
                conflictingTraits=new List<string>{"temperate_1","temperate_2","temperate_3"},
                requiredTraits=new List<string>{"gluttonous_2"} },
            // ===== 节制 temperate（L1节制/有度 · L2克己 · L3绝嗜）=====
            new PersonalityTrait { traitId="temperate_1", traitName="节制", description="有度——对饮食与享乐的克制，凡事适可而止的生活态度",
                category=TraitCategory.Personality, stewardshipMod=1f, learningMod=1f,
                conflictingTraits=new List<string>{"gluttonous_1","gluttonous_2","gluttonous_3"} },
            new PersonalityTrait { traitId="temperate_2", traitName="克己", description="有度——克己是宫廷的典范；宴席上从不贪杯，饮食简单到让御厨觉得受了侮辱",
                category=TraitCategory.Personality, stewardshipMod=2f, learningMod=2f,
                conflictingTraits=new List<string>{"gluttonous_1","gluttonous_2","gluttonous_3"},
                requiredTraits=new List<string>{"temperate_1"} },
            new PersonalityTrait { traitId="temperate_3", traitName="绝嗜", description="有度——已将一切口腹之欲视作软弱；清水与粗面包是唯一的餐食，奢华的宴席令其作呕",
                category=TraitCategory.Personality, stewardshipMod=3f, learningMod=3f,
                conflictingTraits=new List<string>{"gluttonous_1","gluttonous_2","gluttonous_3"},
                requiredTraits=new List<string>{"temperate_2"} },
            // ===== 无常 arbitrary（L1无常/随性 · L2专断 · L3独裁专制）=====
            new PersonalityTrait { traitId="arbitrary_1", traitName="无常", description="随性——治事不循法度，唯凭一时喜怒；所下裁断亦随当日心境而屈伸",
                category=TraitCategory.Personality, intrigueMod=1f, diplomacyMod=-1f, stewardshipMod=-1f,
                conflictingTraits=new List<string>{"just_1","just_2","just_3"} },
            new PersonalityTrait { traitId="arbitrary_2", traitName="专断", description="随性——裁决是每日的赌局；请愿者得细究他们的情绪如同钻研法规，管家悄悄记录明日又可能被推翻的事项",
                category=TraitCategory.Personality, intrigueMod=2f, diplomacyMod=-2f, stewardshipMod=-2f,
                conflictingTraits=new List<string>{"just_1","just_2","just_3"},
                requiredTraits=new List<string>{"arbitrary_1"} },
            new PersonalityTrait { traitId="arbitrary_3", traitName="独裁专制", description="随性——治事全凭喜怒；昨日所赐赦免，明日或成绞索；廷中官吏无人敢信一纸令谕，除非墨迹已干过两回",
                category=TraitCategory.Personality, intrigueMod=3f, diplomacyMod=-3f, stewardshipMod=-3f,
                conflictingTraits=new List<string>{"just_1","just_2","just_3"},
                requiredTraits=new List<string>{"arbitrary_2"} },
            // ===== 公正 just（L1公正/公道 · L2正直 · L3大义凛然）=====
            new PersonalityTrait { traitId="just_1", traitName="公正", description="公道——对正义的坚持，无论出于法律还是良知，即使付出代价也不动摇",
                category=TraitCategory.Personality, diplomacyMod=1f, stewardshipMod=1f, learningMod=1f,
                conflictingTraits=new List<string>{"arbitrary_1","arbitrary_2","arbitrary_3"} },
            new PersonalityTrait { traitId="just_2", traitName="正直", description="公道——公正已成为其领地的一大特点；请愿者甚至从邻近的土地前来，编年史家记下了那些不利于领主亲族的裁决",
                category=TraitCategory.Personality, diplomacyMod=2f, stewardshipMod=2f, learningMod=2f,
                conflictingTraits=new List<string>{"arbitrary_1","arbitrary_2","arbitrary_3"},
                requiredTraits=new List<string>{"just_1"} },
            new PersonalityTrait { traitId="just_3", traitName="大义凛然", description="公道——建立起了跨越疆界的声誉；诸王会就公义之事向他请教，甚至连死敌也承认他裁断公平",
                category=TraitCategory.Personality, diplomacyMod=3f, stewardshipMod=3f, learningMod=3f,
                conflictingTraits=new List<string>{"arbitrary_1","arbitrary_2","arbitrary_3"},
                requiredTraits=new List<string>{"just_2"} },
            // ===== 施虐 sadistic（L1施虐/残忍 · L2虐待狂 · L3悖逆常伦）=====
            new PersonalityTrait { traitId="sadistic_1", traitName="施虐", description="残忍——一种从他人的痛苦中获得的快感，只要当天无事打扰便会主动去寻求",
                category=TraitCategory.Personality, intrigueMod=1f, martialMod=1f, diplomacyMod=-1f,
                conflictingTraits=new List<string>{"compassionate_1","compassionate_2","compassionate_3"} },
            new PersonalityTrait { traitId="sadistic_2", traitName="虐待狂", description="残忍——残忍是众所周知的；仆人们在进入房间前都会做好心理准备，囚犯的命运被精心策划、乐在其中地安排",
                category=TraitCategory.Personality, intrigueMod=2f, martialMod=2f, diplomacyMod=-2f,
                conflictingTraits=new List<string>{"compassionate_1","compassionate_2","compassionate_3"},
                requiredTraits=new List<string>{"sadistic_1"} },
            new PersonalityTrait { traitId="sadistic_3", traitName="悖逆常伦", description="残忍——纯粹从他人的痛苦中得到快乐；造访地牢乃是自愿，处刑的场面不止被容忍，更经其亲手布置",
                category=TraitCategory.Personality, intrigueMod=3f, martialMod=3f, diplomacyMod=-3f,
                conflictingTraits=new List<string>{"compassionate_1","compassionate_2","compassionate_3"},
                requiredTraits=new List<string>{"sadistic_2"} },
            // ===== 固执 stubborn（L1固执/坚决 · L2顽固 · L3冥顽不化）=====
            new PersonalityTrait { traitId="stubborn_1", traitName="固执", description="坚决——一旦采取立场便拒不退让，不论风向如何转变",
                category=TraitCategory.Personality, martialMod=1f, learningMod=1f, diplomacyMod=-1f,
                conflictingTraits=new List<string>{"fickle_1","fickle_2","fickle_3"} },
            new PersonalityTrait { traitId="stubborn_2", traitName="顽固", description="坚决——固执是御前会议的特点；幕僚们晓得定下的立场无法撼动，对手们也据此制定策略",
                category=TraitCategory.Personality, martialMod=2f, learningMod=2f, diplomacyMod=-2f,
                conflictingTraits=new List<string>{"fickle_1","fickle_2","fickle_3"},
                requiredTraits=new List<string>{"stubborn_1"} },
            new PersonalityTrait { traitId="stubborn_3", traitName="冥顽不化", description="坚决——不能被道理、证据或白纸黑字的错误所说动；整个领地会随他们一道坠下悬崖，在此之前绝不肯承认踏错了一步",
                category=TraitCategory.Personality, martialMod=3f, learningMod=3f, diplomacyMod=-3f,
                conflictingTraits=new List<string>{"fickle_1","fickle_2","fickle_3"},
                requiredTraits=new List<string>{"stubborn_2"} },
            // ===== 易变无定 fickle（L1易变无定/优柔寡断 · L2善变 · L3反复无常）=====
            new PersonalityTrait { traitId="fickle_1", traitName="易变无定", description="优柔寡断——偏好、忠诚和意图的摇摆不定，鲜少决定能维持过一周",
                category=TraitCategory.Personality, intrigueMod=1f, diplomacyMod=-1f, stewardshipMod=-1f,
                conflictingTraits=new List<string>{"stubborn_1","stubborn_2","stubborn_3"} },
            new PersonalityTrait { traitId="fickle_2", traitName="善变", description="优柔寡断——反复广受谈论；廷臣学会了绝不在确认今日旨意之前执行昨日的命令，条约的阅览也总瞄着下一次重谈",
                category=TraitCategory.Personality, intrigueMod=2f, diplomacyMod=-2f, stewardshipMod=-2f,
                conflictingTraits=new List<string>{"stubborn_1","stubborn_2","stubborn_3"},
                requiredTraits=new List<string>{"fickle_1"} },
            new PersonalityTrait { traitId="fickle_3", traitName="反复无常", description="优柔寡断——完全不受任何定见支配；宴席中途联盟便告瓦解，臣属的任免在同一个呼吸间颁布又收回",
                category=TraitCategory.Personality, intrigueMod=3f, diplomacyMod=-3f, stewardshipMod=-3f,
                conflictingTraits=new List<string>{"stubborn_1","stubborn_2","stubborn_3"},
                requiredTraits=new List<string>{"fickle_2"} },
            // ===== 怪癖 eccentric（L1怪癖/奇特 · L2古怪 · L3荒诞不经）=====
            new PersonalityTrait { traitId="eccentric_1", traitName="怪癖", description="奇特——在衣装、习俗和言谈上偏爱异乎寻常，使之有别于常礼",
                category=TraitCategory.Personality, learningMod=1f, intrigueMod=1f, diplomacyMod=-1f },
            new PersonalityTrait { traitId="eccentric_2", traitName="古怪", description="奇特——怪癖是宫廷中公开的特色；访客事先会得到警告，家宅也早已不再有非议",
                category=TraitCategory.Personality, learningMod=2f, intrigueMod=2f, diplomacyMod=-2f,
                requiredTraits=new List<string>{"eccentric_1"} },
            new PersonalityTrait { traitId="eccentric_3", traitName="荒诞不经", description="奇特——已全然偏离常俗；学者远道来研究他们，宫廷礼仪围绕着他们的习惯被重写，来访使节在归途上仍津津有味地复述他们的故事",
                category=TraitCategory.Personality, learningMod=3f, intrigueMod=3f, diplomacyMod=-3f,
                requiredTraits=new List<string>{"eccentric_2"} },
        };

        /// <summary>按 traitId 查找标签定义（无则 null）</summary>
        public static PersonalityTrait Get(string traitId)
        {
            foreach (var t in All)
                if (t.traitId == traitId) return t;
            return null;
        }

                /// <summary>获取某标签的升级目标（L1→L2，L2→L3；无升级返回 null）</summary>
        public static PersonalityTrait GetUpgrade(string traitId)
        {
            if (traitId.EndsWith("_1"))
                return Get(traitId.Replace("_1", "_2"));
            if (traitId.EndsWith("_2"))
                return Get(traitId.Replace("_2", "_3"));
            return null;
        }
    }

    /// <summary>角色间关系</summary>
    [Serializable]
    public struct CharacterRelation
    {
        public int otherCharacterId;
        [Range(-200f, 200f)] public float opinion;  // 好感度（企划书：-200~200，双向不对称存储）
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
    public enum RelationshipType
    {
        Stranger,   // 无结构关系
        Spouse,     // 配偶（婚姻）
        Parent,     // 父母（血缘）
        Child,      // 子女（血缘）
        Sibling,    // 兄弟姐妹（血缘）
        Mentor,     // 师（师承身份；机制化纽带见 BondType.MentorBond）
        Student,    // 徒
        Liege,      // 封君/上级（任职契约）
        Vassal      // 封臣/下级
    }

    /// <summary>
    /// 人物羁绊系统
    /// 角色间的特殊关系纽带，提供机制加成
    /// </summary>
    [System.Serializable]
    public class CharacterBond
    {
        public int bondId;
        public int characterAId;
        public int characterBId;
        public BondType type;
        public int establishedDay;
        public float strength = 50f; // 羁绊强度 0~100

        // 羁绊效果
        public float combatBonusWhenTogether = 0f;
        public float diplomacyBonus = 0f;
        public float stressReduction = 0f;
        public bool sharedFate = false;

        /// <summary>每日羁绊Tick</summary>
        public void DailyTick()
        {
            // 羁绊强度自然变化
            strength = Mathf.Clamp(strength + UnityEngine.Random.Range(-1f, 1f), 0f, 100f);
        }
    }

    /// <summary>
    /// 人物羁绊（后天缔结、提供机制加成的特殊联结——区别于 RelationshipType 的客观身份）。
    /// 同一对角色可既有结构关系（如师徒 Mentor/Student）又缔结机制纽带（MentorBond）；
    /// 动态好感程度由 CharacterRelation.opinion 表达，Bond 只承载结下的"纽带"及其加成。
    /// Rivalry（宿怨）与 Nemesis（死敌）为程度不同的敌对纽带，故并存。
    /// </summary>
    public enum BondType
    {
        BloodBond,        // 血脉羁绊（跨代血缘的机制化联结）
        SwornBrotherhood, // 结义兄弟
        MentorBond,       // 师徒羁绊（师承的机制化纽带，对应 RelationshipType.Mentor/Student）
        Rivalry,          // 宿怨（敌对纽带·轻度）
        Romance,          // 爱情羁绊
        ComradesInArms,   // 战友羁绊
        OathBond,         // 誓言羁绊
        Nemesis           // 死敌（敌对纽带·重度）
    }

    /// <summary>
    /// 递归家族系统
    /// 支持无限层级的家族树结构，每一代都有独立的分支
    /// </summary>
    [System.Serializable]
    public class FamilyNode
    {
        public int familyId;
        public string familyName;
        public int founderCharacterId;
        public int foundingYear;

        // 家族核心成员
        public List<int> memberIds = new List<int>();

        // 递归子家族（分支）
        public List<FamilyNode> branches = new List<FamilyNode>();

        // 父家族（null表示主家族）
        [NonSerialized] public FamilyNode parentFamily;

        // 家族属性
        public float familyPrestige = 0f;
        public float familyWealth = 0f;
        public Dictionary<string, float> familyTraditions = new Dictionary<string, float>();

        /// <summary>家族所属政权（-1=未知；家族传统解锁前置革新按此政权检查）</summary>
        public int holderRealmId = -1;

        /// <summary>家族故国（homeland——发源地政权；借鉴《地图上发生的事》homeland_country）</summary>
        public int homelandCountryId = -1;

        /// <summary>代数标记（generation_marks——每代的命名/标记序列，本地化键）</summary>
        public List<string> generationMarks = new List<string>();

        /// <summary>革新树引用（CreateFamily 时由管理器注入；家族传统解锁前置检查用，不入档）</summary>
        [NonSerialized] public InnovationTree Innovations;

        // 家徽/纹章
        public string coaPattern;
        public string coaColors;

        /// <summary>添加成员</summary>
        public void AddMember(int characterId)
        {
            if (!memberIds.Contains(characterId))
                memberIds.Add(characterId);
        }

        /// <summary>创建分支家族</summary>
        public FamilyNode CreateBranch(string branchName, int founderId, int year)
        {
            var branch = new FamilyNode
            {
                familyId = UnityEngine.Random.Range(10000, 99999),
                familyName = branchName,
                founderCharacterId = founderId,
                foundingYear = year,
                parentFamily = this
            };
            branches.Add(branch);
            return branch;
        }

        /// <summary>获取全家族成员（包括所有分支）</summary>
        public List<int> GetAllMembers()
        {
            var all = new List<int>(memberIds);
            foreach (var branch in branches)
                all.AddRange(branch.GetAllMembers());
            return all;
        }

        /// <summary>获取家族总人数</summary>
        public int GetTotalMemberCount()
        {
            int count = memberIds.Count;
            foreach (var branch in branches)
                count += branch.GetTotalMemberCount();
            return count;
        }

        /// <summary>获取家族代数（深度）</summary>
        public int GetGenerationDepth()
        {
            if (branches.Count == 0) return 1;
            int maxDepth = 0;
            foreach (var branch in branches)
                maxDepth = Mathf.Max(maxDepth, branch.GetGenerationDepth());
            return maxDepth + 1;
        }

        /// <summary>查找角色所在的家族节点</summary>
        public FamilyNode FindCharacterFamily(int characterId)
        {
            if (memberIds.Contains(characterId)) return this;
            foreach (var branch in branches)
            {
                var found = branch.FindCharacterFamily(characterId);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>计算家族总威望</summary>
        public float CalculateTotalPrestige()
        {
            float total = familyPrestige;
            foreach (var branch in branches)
                total += branch.CalculateTotalPrestige() * 0.5f; // 分支威望减半计入主家族
            return total;
        }

        // ===== 家族传统（企划书 9.4 家族文化偏移；定义表解释键，见 FamilyTraditionDef） =====

        /// <summary>
        /// 添加家族传统：
        /// 注册表未定义 → 拒绝并警告；与已传承传统互斥（incompatibleWith）→ 拒绝；
        /// 解锁前置革新未全部持有（requiredInnovations）→ 拒绝（革新树未注入时跳过检查）；
        /// 传承强度起点 1（代际深度由家族系统后续累积）
        /// </summary>
        public bool AddFamilyTradition(string traditionId)
        {
            if (string.IsNullOrEmpty(traditionId) || familyTraditions.ContainsKey(traditionId)) return false;
            if (!ContentRegistry.TryGetFamilyTradition(traditionId, out var def))
            {
                Debug.LogWarning($"[Family] 家族传统 {traditionId} 未在注册表定义，拒绝添加");
                return false;
            }
            if (def.incompatibleWith != null)
            {
                foreach (var existing in familyTraditions.Keys)
                {
                    if (def.incompatibleWith.Contains(existing))
                    {
                        Debug.Log($"[Family] 家族传统 {traditionId} 与既有传统 {existing} 互斥，拒绝添加");
                        return false;
                    }
                }
            }
            // 解锁前置革新检查（革新树注入且家族归属政权已知时生效；否则宽松跳过）
            if (Innovations != null && holderRealmId >= 0
                && def.requiredInnovations != null && def.requiredInnovations.Count > 0)
            {
                foreach (int reqId in def.requiredInnovations)
                {
                    if (!Innovations.HasInnovation(holderRealmId, reqId))
                    {
                        Debug.Log($"[Family] 家族传统 {traditionId} 需要革新 {reqId} 解锁，家族所在政权尚未持有，拒绝添加");
                        return false;
                    }
                }
            }
            familyTraditions[traditionId] = 1f;
            return true;
        }

        /// <summary>移除家族传统</summary>
        public bool RemoveFamilyTradition(string traditionId) => familyTraditions.Remove(traditionId);

        /// <summary>计算家族传统在指定键上的总效果（键由 FamilyTraditionDef.effects 解释，如 unity/prestige/learning）</summary>
        public float GetTraditionEffect(string key)
        {
            float total = 0f;
            foreach (var kv in familyTraditions)
            {
                if (!ContentRegistry.TryGetFamilyTradition(kv.Key, out var def) || def.effects == null) continue;
                foreach (var e in def.effects)
                {
                    if (e.key == key) total += e.value * kv.Value;
                }
            }
            return total;
        }
    }

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
    }
}