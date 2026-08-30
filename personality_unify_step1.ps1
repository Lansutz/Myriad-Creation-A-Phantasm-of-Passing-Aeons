# 人格系统统一化 Step1：七维枚举化 + 消除魔法字符串重复 + warfare指挥 + 关系枚举去重
# 输入文件 UTF8 no BOM, CRLF；脚本内 old/new 用 LF，匹配前统一规范化
$ErrorActionPreference = "Stop"
$path = "D:\Myriad-Creation-A-Phantasm-of-Passing-Aeons\CivilizationEvolution\Assets\Scripts\Role\CharacterSystem.cs"
$content = [System.IO.File]::ReadAllText($path) -replace "`r`n","`n"

function N($s){ $s -replace "`r`n","`n" }

$pairs = @()

# ── 替换1：namespace 后插入 PersonalityDimension 枚举 + PersonalityDimensions 辅助类 ──
$pairs += [pscustomobject]@{ name="insert_enum"; old=N @'
namespace CivilizationEvolution.Role
{
    /// <summary>
    /// 角色核心数值
'@; new=N @'
namespace CivilizationEvolution.Role
{
    /// <summary>
    /// 人格七维（企划书 9.3：-100~100，家族遗传基线，压力&gt;60 漂移翻倍）。
    /// 定位：底层人格倾向 / AI 行为参数（参考 CK3 ai_boldness / ai_greed / ai_compassion
    /// / ai_zeal / ai_energy / ai_sociability / ai_honor 的连续值路径）；玩家可见的离散
    /// 性格特质由这七维阈值派生（见 GetDerivedPersonalityTraits），等级=强度档，二者不再并存。
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
    /// 角色核心数值
'@ }

# ── 替换2：GetPersonalityTier / GetPersonalityValue 枚举化 + 统一 Set/Add + string 重载 ──
$pairs += [pscustomobject]@{ name="tier_value_enum"; old=N @'
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
'@; new=N @'
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
'@ }

# ── 替换3：GetPersonalityAffinity 遍历枚举 ──
$pairs += [pscustomobject]@{ name="affinity_loop"; old=N @'
            float affinity = 0f;
            foreach (var dim in new[] { "boldness", "compassion", "greed", "honor", "rationality", "vengefulness", "piety" })
            {
                float a = GetPersonalityValue(dim);
                float b = other.GetPersonalityValue(dim);
'@; new=N @'
            float affinity = 0f;
            // 七维逐项比较（同向互喜、反向互厌，强度分档决定幅度——MPD same/opposite 好感的连续轴版本）
            foreach (var dim in PersonalityDimensions.All)
            {
                float a = GetPersonalityValue(dim);
                float b = other.GetPersonalityValue(dim);
'@ }

# ── 替换4：GetPersonalityDescription / DescribeDimension 枚举化 ──
$pairs += [pscustomobject]@{ name="desc_enum"; old=N @'
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
'@; new=N @'
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
'@ }

# ── 替换5：DailyTick 人格漂移 7 行 → 遍历枚举 ──
$pairs += [pscustomobject]@{ name="daily_drift_loop"; old=N @'
            // 人格漂移（企划书 9.3：压力>60 漂移速度翻倍，随机游走）
            float drift = stress > 60f ? 0.02f : 0.01f;
            boldness = Mathf.Clamp(boldness + UnityEngine.Random.Range(-drift, drift), -100f, 100f);
            compassion = Mathf.Clamp(compassion + UnityEngine.Random.Range(-drift, drift), -100f, 100f);
            greed = Mathf.Clamp(greed + UnityEngine.Random.Range(-drift, drift), -100f, 100f);
            honor = Mathf.Clamp(honor + UnityEngine.Random.Range(-drift, drift), -100f, 100f);
            rationality = Mathf.Clamp(rationality + UnityEngine.Random.Range(-drift, drift), -100f, 100f);
            vengefulness = Mathf.Clamp(vengefulness + UnityEngine.Random.Range(-drift, drift), -100f, 100f);
            piety = Mathf.Clamp(piety + UnityEngine.Random.Range(-drift, drift), -100f, 100f);
'@; new=N @'
            // 人格漂移（企划书 9.3：压力>60 漂移速度翻倍，随机游走；七维统一走 Add 入口）
            float drift = stress > 60f ? 0.02f : 0.01f;
            foreach (var pd in PersonalityDimensions.All)
                AddPersonality(pd, UnityEngine.Random.Range(-drift, drift));
'@ }

# ── 替换6：CalculateCommandAbility 改用 warfare 主导 ──
$pairs += [pscustomobject]@{ name="command_ability_warfare"; old=N @'
        /// <summary>计算军事指挥能力</summary>
        public float CalculateCommandAbility()
        {
            return martial * 0.6f + learning * 0.2f + intrigue * 0.2f;
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
'@ }

# ── 替换7：InitializePersonality 两分支 14 行 → 遍历枚举 ──
$pairs += [pscustomobject]@{ name="init_personality_loop"; old=N @'
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
'@; new=N @'
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
'@ }

# ── 替换8：ApplyTemplate 七维 bias → 遍历（template.GetPersonalityBias 在 ContentTypes.cs 同步添加） ──
$pairs += [pscustomobject]@{ name="template_bias_loop"; old=N @'
            c.boldness = Mathf.Clamp(c.boldness + template.boldnessBias, -100f, 100f);
            c.compassion = Mathf.Clamp(c.compassion + template.compassionBias, -100f, 100f);
            c.greed = Mathf.Clamp(c.greed + template.greedBias, -100f, 100f);
            c.honor = Mathf.Clamp(c.honor + template.honorBias, -100f, 100f);
            c.rationality = Mathf.Clamp(c.rationality + template.rationalityBias, -100f, 100f);
            c.vengefulness = Mathf.Clamp(c.vengefulness + template.vengefulnessBias, -100f, 100f);
            c.piety = Mathf.Clamp(c.piety + template.pietyBias, -100f, 100f);
'@; new=N @'
            // 人格倾向偏移（七维统一叠加，bias 访问走模板的枚举索引器）
            foreach (var pd in PersonalityDimensions.All)
                c.AddPersonality(pd, template.GetPersonalityBias(pd));
'@ }

# ── 替换9：ModifyPersonality 枚举化 + string 重载 ──
$pairs += [pscustomobject]@{ name="modify_personality_enum"; old=N @'
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
'@; new=N @'
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
'@ }

# ── 替换10：PersonalityTrait 补 charmMod（与 MentalDisorderDef 字段对齐） ──
$pairs += [pscustomobject]@{ name="trait_charm_mod"; old=N @'
        public float martialMod = 0f;
        public float diplomacyMod = 0f;
        public float stewardshipMod = 0f;
        public float intrigueMod = 0f;
        public float learningMod = 0f;
        public float warfareMod = 0f;
'@; new=N @'
        public float martialMod = 0f;
        public float diplomacyMod = 0f;
        public float stewardshipMod = 0f;
        public float intrigueMod = 0f;
        public float learningMod = 0f;
        public float warfareMod = 0f;
        public float charmMod = 0f;       // 魅力修正（与 MentalDisorderDef 字段对齐，统一七维属性修正）
'@ }

# ── 替换11：RelationshipType 去重（只留结构性关系，动态情感交给 opinion/Bond） + BondType 注释划界 ──
$pairs += [pscustomobject]@{ name="relationship_type_dedup"; old=N @'
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
'@; new=N @'
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
'@ }

$pairs += [pscustomobject]@{ name="bond_type_comment"; old=N @'
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
'@; new=N @'
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
'@ }

# ── 执行替换 ──
$ok = 0; $fail = @()
foreach($p in $pairs){
  $o = N $p.old
  if($content.Contains($o)){
    $content = $content.Replace($o, (N $p.new))
    Write-Host ("[OK] " + $p.name)
    $ok++
  } else {
    Write-Host ("[FAIL] " + $p.name + "  —— old_string 未匹配")
    $fail += $p.name
  }
}

if($fail.Count -gt 0){
  Write-Host "`n=== 有 $($fail.Count) 个替换失败，未写回文件 ===" -ForegroundColor Red
  exit 1
}

# 写回 UTF8 no BOM + CRLF
$content = $content -replace "`n","`r`n"
[System.IO.File]::WriteAllText($path, $content, (New-Object System.Text.UTF8Encoding $false))
Write-Host "`n=== 全部 $ok 个替换成功，已写回 $path ===" -ForegroundColor Green
