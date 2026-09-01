using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Politics;

namespace CivilizationEvolution.Thought
{
    /// <summary>
    /// 学派系统
    /// 前现代思想学派，有核心经典、代表人物、传播机制
    /// </summary>
    [System.Serializable]
    public class SchoolOfThought
    {
        public int schoolId;
        public string schoolName;
        public string description;
        public int founderCharacterId = -1;
        public int foundingYear;

        // 核心经典
        public List<string> coreTexts = new List<string>();
        public List<string> coreConcepts = new List<string>();

        // 学派属性
        public float metaphysicsWeight = 0.5f;    // 形而上学倾向
        public float ethicsWeight = 0.5f;          // 伦理倾向
        public float politicsWeight = 0.5f;         // 政治倾向
        public float naturalPhilosophyWeight = 0.5f; // 自然哲学倾向

        // 政治立场
        public float authoritarianism = 0.5f;  // 权威主义-自由主义轴
        public float traditionalism = 0.5f;     // 传统主义-进步主义轴
        public float collectivism = 0.5f;       // 集体主义-个体主义轴

        // 传播
        public float spreadPower = 1f;
        public float conversionRate = 0.01f;
        public List<int> followerCharacterIds = new List<int>();
        public Dictionary<int, float> regionPenetration = new Dictionary<int, float>();

        // 学派间关系
        public Dictionary<int, float> schoolRelations = new Dictionary<int, float>();

        /// <summary>计算学派综合影响力</summary>
        public float CalculateInfluence()
        {
            float totalPenetration = 0f;
            foreach (var kv in regionPenetration)
                totalPenetration += kv.Value;
            return followerCharacterIds.Count * 0.3f + totalPenetration * 0.7f;
        }

        /// <summary>每日学派Tick</summary>
        public void DailyTick()
        {
            // 传播衰减
            spreadPower = Mathf.Lerp(spreadPower, 1f, 0.001f);
        }
    }

    /// <summary>
    /// 信仰系统
    /// 宗教信仰，有神灵体系、仪式、教义、组织
    /// </summary>
    [System.Serializable]
    public class FaithSystem
    {
        public int faithId;
        public string faithName;
        public string description;
        public FaithType type = FaithType.Polytheistic;

        // 神灵体系
        public List<Deity> deities = new List<Deity>();
        public string chiefDeity;

        // 教义
        public List<Doctrine> doctrines = new List<Doctrine>();
        public List<string> rituals = new List<string>();
        public List<string> taboos = new List<string>();

        // 组织
        public FaithOrganizationType orgType = FaithOrganizationType.Decentralized;
        public int highPriestCharacterId = -1;
        public float churchWealth = 0f;
        public float churchInfluence = 0f;

        // 圣地
        public List<int> holySiteTileIndices = new List<int>();

        // 传播与信徒
        public float missionaryZeal = 0.5f;
        public float tolerance = 0.5f;
        public Dictionary<int, float> regionAdherence = new Dictionary<int, float>();
        public List<int> followerCharacterIds = new List<int>();

        // 宗教间关系
        public Dictionary<int, float> faithRelations = new Dictionary<int, float>();

        // ===== 美德/罪行（宗教对性格的判定——引用 PersonalityTraitDatabase 基 id） =====
        public List<string> virtues = new List<string>();
        public List<string> sins = new List<string>();

        // ===== 信仰热忱（Fervor——大圣战可用性数值） =====
        /// <summary>热忱 0-100（大圣战可用条件：≥60 + 存在宗教领袖）</summary>
        public float fervor = 50f;
        /// <summary>大圣战可用阈值</summary>
        public const float GreatHolyWarThreshold = 60f;

        /// <summary>热忱变化（增长：异教冲突+25/圣地丢失+50/殉道+25/圣战胜利+15/
        /// 大公会议成功+10；下降：内部丑闻-30/圣战失败-20/长期和平冷却-10）</summary>
        public void AddFervor(float delta)
        {
            fervor = Mathf.Clamp(fervor + delta, 0f, 100f);
        }

        /// <summary>大圣战是否可用（热忱达标+有宗教领袖——教宗/哈里发）</summary>
        public bool CanDeclareGreatHolyWar() => fervor >= GreatHolyWarThreshold && highPriestCharacterId >= 0;

        /// <summary>美德/罪行得分（宗教对性格的判定——traitId 匹配基 id 前缀）</summary>
        public int GetVirtueScore(CivilizationEvolution.Role.CharacterData character)
        {
            if (character == null || character.traits == null) return 0;
            int score = 0;
            foreach (var t in character.traits)
                foreach (var v in virtues)
                    if (t.traitId == v || t.traitId.StartsWith(v + "_"))
                        score++;
            return score;
        }

        public int GetSinScore(CivilizationEvolution.Role.CharacterData character)
        {
            if (character == null || character.traits == null) return 0;
            int score = 0;
            foreach (var t in character.traits)
                foreach (var s in sins)
                    if (t.traitId == s || t.traitId.StartsWith(s + "_"))
                        score++;
            return score;
        }

        /// <summary>计算宗教权威</summary>
        public float CalculateReligiousAuthority()
        {
            float totalAdherence = 0f;
            foreach (var kv in regionAdherence)
                totalAdherence += kv.Value;
            return churchInfluence * 0.4f + totalAdherence * 0.3f + followerCharacterIds.Count * 0.3f;
        }

        /// <summary>每日信仰Tick</summary>
        public void DailyTick()
        {
            churchInfluence = Mathf.Lerp(churchInfluence, 50f, 0.001f);
        }
    }

    public enum FaithType
    {
        Animistic,       // 泛灵论
        Polytheistic,    // 多神教
        Henotheistic,    // 单一主神教
        Monotheistic,    // 一神教
        Pantheistic,     // 泛神论
        Atheistic,       // 无神论
        Cosmic           // 宇宙论宗教
    }

    public enum FaithOrganizationType
    {
        Decentralized,   // 去中心化
        Congregational,  // 公理制
        Episcopal,       // 主教制
        Papal,           // 教皇制
        Theocratic,      // 神权制
        StateChurch      // 国教会
    }

    /// <summary>神灵</summary>
    [System.Serializable]
    public struct Deity
    {
        public string name;
        public string domain;      // 神职领域
        public float importance;   // 重要性 0~1
        public string symbol;
        public List<string> festivals;
    }

    /// <summary>教义</summary>
    [System.Serializable]
    public struct Doctrine
    {
        public string name;
        public string description;
        public DoctrineCategory category;
        public float strictness; // 严格程度 0~1
    }

    public enum DoctrineCategory
    {
        Cosmology,       // 宇宙论
        Soteriology,     // 救赎论
        Ethics,          // 伦理学
        Ritual,          // 仪式
        Ecclesiology,    // 教会论
        Eschatology,     // 末世论
        Political        // 政治神学
    }

    /// <summary>
    /// 法律与罪行系统（简化版）
    /// </summary>
    [System.Serializable]
    public class LawSystem
    {
        public int lawSystemId;
        public string lawSystemName;
        public LawSource source = LawSource.Customary;

        // 法律条文
        public List<Law> laws = new List<Law>();

        // 罪行定义
        public Dictionary<CrimeType, CrimeDefinition> crimes = new Dictionary<CrimeType, CrimeDefinition>();

        // 司法效率
        public float judicialEfficiency = 0.5f;
        public float corruption = 0.2f;
        public float lawEnforcement = 0.5f;

        // 刑罚偏好
        public float severity = 0.5f; // 刑罚严厉程度
        public bool useCapitalPunishment = true;
        public bool useCorporalPunishment = true;
        public bool useFines = true;
        public bool useImprisonment = true;
        public bool useExile = true;
        public bool useSlavery = true;

        public LawSystem()
        {
            InitializeDefaultCrimes();
        }

        private void InitializeDefaultCrimes()
        {
            crimes[CrimeType.Murder] = new CrimeDefinition
            {
                type = CrimeType.Murder,
                name = "谋杀",
                baseSeverity = 100f,
                defaultPunishment = PunishmentType.Death
            };
            crimes[CrimeType.Treason] = new CrimeDefinition
            {
                type = CrimeType.Treason,
                name = "叛国",
                baseSeverity = 100f,
                defaultPunishment = PunishmentType.Death
            };
            crimes[CrimeType.Theft] = new CrimeDefinition
            {
                type = CrimeType.Theft,
                name = "盗窃",
                baseSeverity = 30f,
                defaultPunishment = PunishmentType.Fine
            };
            crimes[CrimeType.Assault] = new CrimeDefinition
            {
                type = CrimeType.Assault,
                name = "伤害",
                baseSeverity = 40f,
                defaultPunishment = PunishmentType.Fine
            };
            crimes[CrimeType.Blasphemy] = new CrimeDefinition
            {
                type = CrimeType.Blasphemy,
                name = "亵渎",
                baseSeverity = 60f,
                defaultPunishment = PunishmentType.Exile
            };
            crimes[CrimeType.Heresy] = new CrimeDefinition
            {
                type = CrimeType.Heresy,
                name = "异端",
                baseSeverity = 80f,
                defaultPunishment = PunishmentType.Death
            };
            crimes[CrimeType.TaxEvasion] = new CrimeDefinition
            {
                type = CrimeType.TaxEvasion,
                name = "逃税",
                baseSeverity = 25f,
                defaultPunishment = PunishmentType.Fine
            };
            crimes[CrimeType.Desertion] = new CrimeDefinition
            {
                type = CrimeType.Desertion,
                name = "逃兵",
                baseSeverity = 70f,
                defaultPunishment = PunishmentType.Death
            };
        }

        /// <summary>审判罪行</summary>
        public TrialResult TrialCrime(CrimeType crime, int suspectCharacterId, int realmId, RealmData realm)
        {
            var result = new TrialResult();
            if (!crimes.TryGetValue(crime, out var crimeDef))
            {
                result.verdict = VerdictType.NotGuilty;
                return result;
            }

            // 定罪概率（受司法效率、腐败、嫌疑人身份影响）
            float convictionChance = judicialEfficiency * (1f - corruption);

            // 贵族/神职人员有更高的脱罪概率
            // 简化：假设身份影响
            convictionChance *= 0.8f;

            result.verdict = UnityEngine.Random.value < convictionChance
                ? VerdictType.Guilty
                : VerdictType.NotGuilty;

            if (result.verdict == VerdictType.Guilty)
            {
                // 量刑
                result.punishment = crimeDef.defaultPunishment;
                result.severity = crimeDef.baseSeverity * severity;

                // 罚款金额
                if (result.punishment == PunishmentType.Fine)
                    result.fineAmount = crimeDef.baseSeverity * 10f;
            }

            return result;
        }

        /// <summary>每日法律Tick</summary>
        public void DailyTick()
        {
            judicialEfficiency = Mathf.Clamp(judicialEfficiency + UnityEngine.Random.Range(-0.001f, 0.001f), 0.1f, 1f);
        }
    }

    public enum LawSource
    {
        Customary,      // 习惯法
        Statutory,       // 成文法
        Religious,       // 宗教法
        Common,          // 普通法
        Civil,           // 大陆法系
        Mixed            // 混合法
    }

    public enum CrimeType
    {
        Murder,
        Treason,
        Theft,
        Assault,
        Blasphemy,
        Heresy,
        TaxEvasion,
        Desertion,
        Fraud,
        Arson,
        Rape,
        Kidnapping,
        Smuggling,
        Piracy,
        Rebellion
    }

    public enum PunishmentType
    {
        Death,
        Corporal,
        Fine,
        Imprisonment,
        Exile,
        Slavery,
        PublicHumiliation,
        Forfeiture,
        Pardon
    }

    public enum VerdictType
    {
        Guilty,
        NotGuilty,
        HungJury,
        Dismissed
    }

    [System.Serializable]
    public struct CrimeDefinition
    {
        public CrimeType type;
        public string name;
        public float baseSeverity;
        public PunishmentType defaultPunishment;
    }

    [System.Serializable]
    public struct Law
    {
        public int lawId;
        public string lawName;
        public string description;
        public LawCategory category;
        public int enactedDay;
        public bool isActive;
    }

    public enum LawCategory
    {
        Criminal,
        Civil,
        Constitutional,
        Religious,
        Military,
        Economic,
        Administrative
    }

    [System.Serializable]
    public struct TrialResult
    {
        public VerdictType verdict;
        public PunishmentType punishment;
        public float severity;
        public float fineAmount;
        public int imprisonmentDays;
    }

    /// <summary>
    /// 思潮系统（高阶解锁机制）
    /// 大规模思想运动，有起源、传播、高潮、衰退周期
    /// </summary>
    [System.Serializable]
    public class IdeologyMovement
    {
        public int movementId;
        public string movementName;
        public string description;
        public int originRegionId;
        public int startYear;
        public int peakYear = -1;
        public int endYear = -1;

        public MovementPhase phase = MovementPhase.Emerging;

        // 核心主张
        public List<string> coreTenets = new List<string>();
        public Dictionary<string, float> policyPositions = new Dictionary<string, float>();

        // 传播
        public float momentum = 0f;       // 势头 0~100
        public float radicalism = 0.5f;   // 激进程度
        public float appeal = 0.5f;        // 吸引力

        // 参与者
        public List<int> supporterCharacterIds = new List<int>();
        public Dictionary<int, float> regionSupport = new Dictionary<int, float>();

        // 关联学派/信仰
        public List<int> associatedSchoolIds = new List<int>();
        public List<int> associatedFaithIds = new List<int>();

        /// <summary>每日思潮Tick</summary>
        public void DailyTick(int currentYear)
        {
            // 思潮生命周期
            int age = currentYear - startYear;

            if (phase == MovementPhase.Emerging && age > 5)
                phase = MovementPhase.Growing;
            if (phase == MovementPhase.Growing && momentum > 70f)
            {
                phase = MovementPhase.Peak;
                peakYear = currentYear;
            }
            if (phase == MovementPhase.Peak && age > 20)
                phase = MovementPhase.Declining;
            if (phase == MovementPhase.Declining && momentum < 10f)
            {
                phase = MovementPhase.Extinct;
                endYear = currentYear;
            }

            // 势头变化
            float momentumChange = phase switch
            {
                MovementPhase.Emerging => 0.5f,
                MovementPhase.Growing => 2f,
                MovementPhase.Peak => 0f,
                MovementPhase.Declining => -1.5f,
                MovementPhase.Extinct => -0.5f,
                _ => 0f
            };
            momentum = Mathf.Clamp(momentum + momentumChange + UnityEngine.Random.Range(-1f, 1f), 0f, 100f);
        }
    }

    public enum MovementPhase
    {
        Emerging,     // 萌芽
        Growing,      // 成长
        Peak,         // 高潮
        Declining,    // 衰退
        Extinct       // 消亡
    }

    /// <summary>
    /// 思想与规范管理器
    /// 协调学派、信仰、法律、思潮系统
    /// </summary>
    public class ThoughtManager
    {
        private readonly Dictionary<int, SchoolOfThought> _schools = new Dictionary<int, SchoolOfThought>();
        private readonly Dictionary<int, FaithSystem> _faiths = new Dictionary<int, FaithSystem>();
        private readonly Dictionary<int, LawSystem> _lawSystems = new Dictionary<int, LawSystem>();
        private readonly List<IdeologyMovement> _movements = new List<IdeologyMovement>();
        private int _nextSchoolId = 1;
        private int _nextFaithId = 1;
        private int _nextLawId = 1;
        private int _nextMovementId = 1;

        /// <summary>创建学派</summary>
        public SchoolOfThought CreateSchool(string name, int founderId, int year)
        {
            var school = new SchoolOfThought
            {
                schoolId = _nextSchoolId++,
                schoolName = name,
                founderCharacterId = founderId,
                foundingYear = year
            };
            _schools[school.schoolId] = school;
            return school;
        }

        /// <summary>创建信仰</summary>
        public FaithSystem CreateFaith(string name, FaithType type)
        {
            var faith = new FaithSystem
            {
                faithId = _nextFaithId++,
                faithName = name,
                type = type
            };
            _faiths[faith.faithId] = faith;
            return faith;
        }

        /// <summary>创建法律体系</summary>
        public LawSystem CreateLawSystem(string name, LawSource source)
        {
            var law = new LawSystem
            {
                lawSystemId = _nextLawId++,
                lawSystemName = name,
                source = source
            };
            _lawSystems[law.lawSystemId] = law;
            return law;
        }

        /// <summary>创建思潮运动</summary>
        public IdeologyMovement CreateMovement(string name, int originRegionId, int startYear)
        {
            var movement = new IdeologyMovement
            {
                movementId = _nextMovementId++,
                movementName = name,
                originRegionId = originRegionId,
                startYear = startYear
            };
            _movements.Add(movement);
            return movement;
        }

        /// <summary>每日思想Tick</summary>
        public void DailyTick(int currentYear)
        {
            foreach (var school in _schools.Values)
                school.DailyTick();

            foreach (var faith in _faiths.Values)
                faith.DailyTick();

            foreach (var law in _lawSystems.Values)
                law.DailyTick();

            for (int i = _movements.Count - 1; i >= 0; i--)
            {
                _movements[i].DailyTick(currentYear);
                if (_movements[i].phase == MovementPhase.Extinct)
                    _movements.RemoveAt(i);
            }
        }

        // ===== 查询接口 =====
        public SchoolOfThought GetSchool(int id) => _schools.TryGetValue(id, out var s) ? s : null;
        public FaithSystem GetFaith(int id) => _faiths.TryGetValue(id, out var f) ? f : null;
        public LawSystem GetLawSystem(int id) => _lawSystems.TryGetValue(id, out var l) ? l : null;
        public IdeologyMovement GetMovement(int id) => _movements.Find(m => m.movementId == id);

        public IReadOnlyDictionary<int, SchoolOfThought> GetAllSchools() => _schools;
        public IReadOnlyDictionary<int, FaithSystem> GetAllFaiths() => _faiths;
        public IReadOnlyList<IdeologyMovement> GetAllMovements() => _movements;

        /// <summary>获取地区最主流信仰</summary>
        public FaithSystem GetDominantFaith(int regionId)
        {
            FaithSystem dominant = null;
            float maxAdherence = 0f;
            foreach (var faith in _faiths.Values)
            {
                if (faith.regionAdherence.TryGetValue(regionId, out var adherence) && adherence > maxAdherence)
                {
                    maxAdherence = adherence;
                    dominant = faith;
                }
            }
            return dominant;
        }

        /// <summary>获取地区最有影响力学派</summary>
        public SchoolOfThought GetDominantSchool(int regionId)
        {
            SchoolOfThought dominant = null;
            float maxPenetration = 0f;
            foreach (var school in _schools.Values)
            {
                if (school.regionPenetration.TryGetValue(regionId, out var penetration) && penetration > maxPenetration)
                {
                    maxPenetration = penetration;
                    dominant = school;
                }
            }
            return dominant;
        }
    }
}
