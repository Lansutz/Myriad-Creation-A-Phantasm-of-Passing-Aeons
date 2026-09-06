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
        /// <summary>绰号/诨号（行为后验授予——中性西方传统——与原型[性格画像]分离）</summary>
        public string epithet = "";
        /// <summary>身体/外观特征标记（秃顶/跛足/失明/矮小/黑甲/白袍……——
        /// 事件/伤病系统写入——外貌型绰号[秃头/瘸子/瞎子/黑王]判定源）</summary>
        public List<string> bodyMarks = new List<string>();
        /// <summary>一生成就计数（行为计数器——GameWorld 各系统事件写入——
        /// 死亡时 EvaluateAndGrant 评估绰号/谥号——评价分级的数据源）</summary>
        public Culture.EvaluationSystem.AchievementRecord achievements;
        /// <summary>即位日（在位年数 reignYears 计算——继位时写入）</summary>
        public int accessionDay = -1;
        /// <summary>绰号已评估标记（死亡评估一次）</summary>
        public bool epithetEvaluated = false;
        /// <summary>尊号/谥号（死后——华夏式——按一生行为定谥）</summary>
        public string posthumousTitle = "";
        /// <summary>传奇评价（"X王"档——征服王/冒险王/诗人王——极难达成的终身成就——
        /// 与普通绰号分离——升格制：达成传奇可覆盖普通绰号）</summary>
        public string legendaryTitle = "";

        // ===== 行为计数器（绰号/传奇评价授予依据——后验统计） =====
        public int conquests = 0;              // 征服领地数（军事扩张）
        public int warsWon = 0;                // 胜仗数
        public int diplomaticVictories = 0;    // 外交诈术/背盟成功（狐狸）
        public int cultureWorks = 0;           // 文治成就（智者/诗人）
        public int religiousDeeds = 0;         // 宗教行为（虔诚者）
        public int expeditions = 0;            // 远征次数（冒险者/冒险王）
        public int vigilanceActs = 0;          // 警觉行为（识破阴谋/平叛）
        /// <summary>灵性满足（0-100——体验支柱动态化——角色与信仰的真实关系：
        /// 美德特质增长/罪行特质下降/秘密信仰煎熬——CK3 唯主是依 Spiritual
        /// Fulfillment；区别于人格维度 piety 虔信——这是虔诚资源）</summary>
        public float spiritualFulfillment = 50f;
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
        [NonSerialized] public Dictionary<int, CharacterRelation> relations = new Dictionary<int, CharacterRelation>();


        // 技能/经验
        [NonSerialized] public Dictionary<string, float> skills = new Dictionary<string, float>();


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
}
