using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Economy;
using CivilizationEvolution.Politics;
using CivilizationEvolution.Race;

namespace CivilizationEvolution.Role
{
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

        // 身份
        public int realmId = -1;
        public int familyId = -1;
        public int cultureId;
        public int raceId;
        public int faithId;
        public CharacterRole role = CharacterRole.Commoner;

        // 血缘（DNA 遗传与近亲系数计算依赖）
        public int fatherId = -1;
        public int motherId = -1;

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

        /// <summary>计算军事指挥能力</summary>
        public float CalculateCommandAbility()
        {
            return martial * 0.6f + learning * 0.2f + intrigue * 0.2f;
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

            // 人格漂移（企划书 9.3：压力>60 漂移速度翻倍，随机游走）
            float drift = stress > 60f ? 0.02f : 0.01f;
            boldness = Mathf.Clamp(boldness + UnityEngine.Random.Range(-drift, drift), -100f, 100f);
            compassion = Mathf.Clamp(compassion + UnityEngine.Random.Range(-drift, drift), -100f, 100f);
            greed = Mathf.Clamp(greed + UnityEngine.Random.Range(-drift, drift), -100f, 100f);
            honor = Mathf.Clamp(honor + UnityEngine.Random.Range(-drift, drift), -100f, 100f);
            rationality = Mathf.Clamp(rationality + UnityEngine.Random.Range(-drift, drift), -100f, 100f);
            vengefulness = Mathf.Clamp(vengefulness + UnityEngine.Random.Range(-drift, drift), -100f, 100f);
            piety = Mathf.Clamp(piety + UnityEngine.Random.Range(-drift, drift), -100f, 100f);

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
        public int GetPersonalityTier(string dim)
        {
            float v = GetPersonalityValue(dim);
            float abs = Mathf.Abs(v);
            if (abs < 15f) return 0;
            if (abs < 35f) return 1;
            if (abs < 65f) return 2;
            return 3;
        }

        /// <summary>按维度名取人格值（boldness/compassion/greed/honor/rationality/vengefulness/piety）</summary>
        public float GetPersonalityValue(string dim)
        {
            return dim switch
            {
                "boldness" => boldness,
                "compassion" => compassion,
                "greed" => greed,
                "honor" => honor,
                "rationality" => rationality,
                "vengefulness" => vengefulness,
                "piety" => piety,
                _ => 0f
            };
        }

        /// <summary>
        /// 人格亲和度（-20~+20，借鉴 MPD 的 same/opposite opinion 机制）：
        /// 七维逐项比较——同向（同号且双方强度&gt;0）互喜、反向互厌，强度分档决定幅度；
        /// 用于关系好感缓慢漂移（性格相投日久生情，相斥渐行渐远）
        /// </summary>
        public float GetPersonalityAffinity(CharacterData other)
        {
            if (other == null) return 0f;
            float affinity = 0f;
            foreach (var dim in new[] { "boldness", "compassion", "greed", "honor", "rationality", "vengefulness", "piety" })
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

        /// <summary>生成写实人格描述：按最高 2 维组合套用场景模板</summary>
        public string GetPersonalityDescription()
        {
            // 取绝对值最高的两维
            var dims = new (string name, float value)[]
            {
                ("大胆", boldness), ("悲悯", compassion), ("贪婪", greed),
                ("荣誉", honor), ("理性", rationality), ("报复", vengefulness), ("虔信", piety)
            };
            Array.Sort(dims, (a, b) => Mathf.Abs(b.value).CompareTo(Mathf.Abs(a.value)));

            var top1 = dims[0];
            var top2 = dims[1];
            if (Mathf.Abs(top1.value) < 15f)
                return "性情平和中正，既不偏激也不执拗，处世随分安时。";

            string t1 = DescribeDimension(top1.name, top1.value);
            string t2 = DescribeDimension(top2.name, top2.value);
            return $"为人{t1}，行事{t2}。";
        }

        private static string DescribeDimension(string dim, float value)
        {
            bool high = value > 0f;
            return dim switch
            {
                "大胆" => high ? "胆气过人，临事敢为，鲜有畏葸" : "性谨慎，谋定后动，不喜冒险",
                "悲悯" => high ? "心肠慈悲，见不得民生疾苦，常施仁政" : "心硬如铁，视百姓如草芥，无情可动",
                "贪婪" => high ? "贪得无厌，见利忘义，库藏永不餍足" : "淡泊财货，不慕荣利，清廉自守",
                "荣誉" => high ? "重诺守信，把名誉看得比性命更重" : "轻诺寡信，名节于他不过是可售之物",
                "理性" => high ? "冷静理性，遇事权衡利害，不感情用事" : "率性而为，凭一时好恶决断，不计后果",
                "报复" => high ? "睚眦必报，恩怨分明，得罪过他的人他都记着" : "宽宏大量，受了委屈也多半一笑置之",
                "虔信" => high ? "虔诚信奉，常与神职人员来往，礼敬神祇" : "对神明半信半疑，礼数只是做给人看",
                _ => "性情难测"
            };
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

    public enum RelationshipType
    {
        Stranger,
        Acquaintance,
        Friend,
        Rival,
        Lover,
        Spouse,
        Parent,
        Child,
        Sibling,
        Mentor,
        Student,
        Liege,
        Vassal,
        Enemy,
        Nemesis
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

    public enum BondType
    {
        BloodBond,        // 血脉羁绊
        SwornBrotherhood, // 结义兄弟
        MentorBond,       // 师徒羁绊
        Rivalry,          // 宿敌羁绊
        Romance,          // 爱情羁绊
        ComradesInArms,   // 战友羁绊
        OathBond,         // 誓言羁绊
        Nemesis           // 死敌羁绊
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

            c.boldness = Mathf.Clamp(c.boldness + template.boldnessBias, -100f, 100f);
            c.compassion = Mathf.Clamp(c.compassion + template.compassionBias, -100f, 100f);
            c.greed = Mathf.Clamp(c.greed + template.greedBias, -100f, 100f);
            c.honor = Mathf.Clamp(c.honor + template.honorBias, -100f, 100f);
            c.rationality = Mathf.Clamp(c.rationality + template.rationalityBias, -100f, 100f);
            c.vengefulness = Mathf.Clamp(c.vengefulness + template.vengefulnessBias, -100f, 100f);
            c.piety = Mathf.Clamp(c.piety + template.pietyBias, -100f, 100f);
        }

        // ===== 人格七维（企划书 9.3：家族遗传基线 + 随机偏移） =====

        /// <summary>人格七维初始化：有父母取双亲平均 ±10（家族遗传基线），无父母围绕 0 随机 ±30</summary>
        private void InitializePersonality(CharacterData c, int fatherId, int motherId)
        {
            var father = fatherId >= 0 ? GetCharacter(fatherId) : null;
            var mother = motherId >= 0 ? GetCharacter(motherId) : null;

            if (father != null || mother != null)
            {
                float f = father != null ? 1f : 0f, m = mother != null ? 1f : 0f;
                float n = f + m;
                c.boldness = Mathf.Clamp((father != null ? father.boldness : 0f) * f / n + (mother != null ? mother.boldness : 0f) * m / n + UnityEngine.Random.Range(-10f, 10f), -100f, 100f);
                c.compassion = Mathf.Clamp((father != null ? father.compassion : 0f) * f / n + (mother != null ? mother.compassion : 0f) * m / n + UnityEngine.Random.Range(-10f, 10f), -100f, 100f);
                c.greed = Mathf.Clamp((father != null ? father.greed : 0f) * f / n + (mother != null ? mother.greed : 0f) * m / n + UnityEngine.Random.Range(-10f, 10f), -100f, 100f);
                c.honor = Mathf.Clamp((father != null ? father.honor : 0f) * f / n + (mother != null ? mother.honor : 0f) * m / n + UnityEngine.Random.Range(-10f, 10f), -100f, 100f);
                c.rationality = Mathf.Clamp((father != null ? father.rationality : 0f) * f / n + (mother != null ? mother.rationality : 0f) * m / n + UnityEngine.Random.Range(-10f, 10f), -100f, 100f);
                c.vengefulness = Mathf.Clamp((father != null ? father.vengefulness : 0f) * f / n + (mother != null ? mother.vengefulness : 0f) * m / n + UnityEngine.Random.Range(-10f, 10f), -100f, 100f);
                c.piety = Mathf.Clamp((father != null ? father.piety : 0f) * f / n + (mother != null ? mother.piety : 0f) * m / n + UnityEngine.Random.Range(-10f, 10f), -100f, 100f);
            }
            else
            {
                c.boldness = UnityEngine.Random.Range(-30f, 30f);
                c.compassion = UnityEngine.Random.Range(-30f, 30f);
                c.greed = UnityEngine.Random.Range(-30f, 30f);
                c.honor = UnityEngine.Random.Range(-30f, 30f);
                c.rationality = UnityEngine.Random.Range(-30f, 30f);
                c.vengefulness = UnityEngine.Random.Range(-30f, 30f);
                c.piety = UnityEngine.Random.Range(-30f, 30f);
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

        /// <summary>人格维度修正（事件驱动漂移；维度名：boldness/compassion/greed/honor/rationality/vengefulness/piety）</summary>
        public void ModifyPersonality(int characterId, string dimension, float delta)
        {
            var c = GetCharacter(characterId);
            if (c == null) return;
            switch (dimension)
            {
                case "boldness": c.boldness = Mathf.Clamp(c.boldness + delta, -100f, 100f); break;
                case "compassion": c.compassion = Mathf.Clamp(c.compassion + delta, -100f, 100f); break;
                case "greed": c.greed = Mathf.Clamp(c.greed + delta, -100f, 100f); break;
                case "honor": c.honor = Mathf.Clamp(c.honor + delta, -100f, 100f); break;
                case "rationality": c.rationality = Mathf.Clamp(c.rationality + delta, -100f, 100f); break;
                case "vengefulness": c.vengefulness = Mathf.Clamp(c.vengefulness + delta, -100f, 100f); break;
                case "piety": c.piety = Mathf.Clamp(c.piety + delta, -100f, 100f); break;
            }
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

        /// <summary>自主生育（最小机制）：成年异性同政权配对，低概率产子，保证 DNA 遗传持续发生</summary>
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

            foreach (var male in males)
            {
                if (CountChildren(male.characterId) >= 8) continue; // 子女上限

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

                var family = CreateFamily(lastName, ruler.characterId, currentYear);
                family.AddMember(spouse.characterId);
                spouse.familyId = family.familyId;
            }
        }

        /// <summary>创建家族</summary>
        public FamilyNode CreateFamily(string familyName, int founderId, int foundingYear)
        {
            var family = new FamilyNode
            {
                familyId = _nextFamilyId++,
                familyName = familyName,
                founderCharacterId = founderId,
                foundingYear = foundingYear
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
    }
}
