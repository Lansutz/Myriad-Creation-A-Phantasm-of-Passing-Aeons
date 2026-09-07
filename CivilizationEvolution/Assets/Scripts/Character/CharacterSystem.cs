using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Economy;
using CivilizationEvolution.Politics;
using CivilizationEvolution.Race;
using CivilizationEvolution.Tech;
using CivilizationEvolution.Thought;

namespace CivilizationEvolution.Character
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
    public partial class CharacterManager
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

        /// <summary>名字生成：文化名字池（type: 0男名 1女名 2姓氏），空池回退文化名</summary>
        private static string GenerateName(int cultureId, int type)
        {
            if (ContentRegistry.TryGetCulture(cultureId, out var pack))
                return ContentRegistry.GetRandomName(pack, type);
            return "无名";
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
    }
}