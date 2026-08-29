using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;

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

        // 核心六维属性（0-100）
        [Range(0f, 100f)] public float martial = 50f;      // 军事
        [Range(0f, 100f)] public float diplomacy = 50f;     // 外交
        [Range(0f, 100f)] public float stewardship = 50f;   // 管理
        [Range(0f, 100f)] public float intrigue = 50f;       // 谋略
        [Range(0f, 100f)] public float learning = 50f;       // 学识
        [Range(0f, 100f)] public float piety = 50f;          // 虔诚

        // 次级属性
        [Range(0f, 100f)] public float health = 100f;        // 健康
        [Range(0f, 100f)] public float fertility = 50f;       // 生育力
        [Range(0f, 100f)] public float prestige = 0f;         // 威望
        [Range(0f, 100f)] public float stress = 0f;           // 压力
        [Range(0f, 100f)] public float dread = 0f;            // 恐惧

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

        /// <summary>计算综合能力值</summary>
        public float CalculateOverallAbility()
        {
            return (martial + diplomacy + stewardship + intrigue + learning) / 5f;
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

            // 健康自然变化
            float ageFactor = age > 50 ? (age - 50f) / 50f : 0f;
            health = Mathf.Clamp(health - ageFactor * 0.01f, 0f, 100f);

            // 压力自然恢复
            stress = Mathf.Max(0f, stress - 0.1f);

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
            rel.opinion = Mathf.Clamp(rel.opinion + opinionDelta, -100f, 100f);
            rel.history.Add($"{reason}: {opinionDelta}");
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
        public float pietyMod = 0f;

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
    [System.Serializable]
    public struct CharacterRelation
    {
        public int otherCharacterId;
        [Range(-100f, 100f)] public float opinion;  // 好感度
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
    }

    /// <summary>
    /// 角色管理器
    /// 管理所有有名角色、家族、羁绊
    /// </summary>
    public class CharacterManager
    {
        private readonly Dictionary<int, CharacterData> _characters = new Dictionary<int, CharacterData>();
        private readonly Dictionary<int, FamilyNode> _families = new Dictionary<int, FamilyNode>();
        private readonly List<CharacterBond> _bonds = new List<CharacterBond>();
        private int _nextCharacterId = 1;
        private int _nextFamilyId = 1;
        private int _nextBondId = 1;

        /// <summary>创建新角色</summary>
        public CharacterData CreateCharacter(string firstName, string lastName, int age, bool isMale,
            int cultureId, int raceId, int faithId, CharacterRole role)
        {
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
                role = role
            };

            // 随机属性
            character.martial = UnityEngine.Random.Range(20f, 80f);
            character.diplomacy = UnityEngine.Random.Range(20f, 80f);
            character.stewardship = UnityEngine.Random.Range(20f, 80f);
            character.intrigue = UnityEngine.Random.Range(20f, 80f);
            character.learning = UnityEngine.Random.Range(20f, 80f);
            character.piety = UnityEngine.Random.Range(20f, 80f);

            _characters[character.characterId] = character;
            return character;
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
