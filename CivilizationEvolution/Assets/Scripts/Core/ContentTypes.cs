using System;
using System.Collections.Generic;
using CivilizationEvolution.Role;

namespace CivilizationEvolution.Core
{
    // ============================================================================
    // 模组化内容类型定义（企划书 1.2 模组扩展规范）
    // 全部类型：Base/Mods 双目录、JsonUtility 加载、同 Id 覆盖、失败仅告警
    // 概念锚点：族群=Ethnos（术语表），族群精神=Ethos（CK3 文化支柱类比），
    //          文化传统=Traditions（CK3 类比），语言=Ethnos 定义项
    // 本地化约定：定义文件只存键；显示文本查 Localization 表
    //   - 族群精神：&lt;ethosId&gt;_name / &lt;ethosId&gt;_desc
    //   - 文化传统：&lt;traditionId&gt;_name / &lt;traditionId&gt;_desc
    //   - 语言：&lt;languageId&gt;_name / _desc / _script / _naming
    //   - 族群：&lt;groupId&gt;_name / &lt;groupId&gt;_desc
    // ============================================================================

    /// <summary>通用效果条目（键由消费系统解释，如 development/levy/opinion...）</summary>
    [Serializable]
    public class EffectEntry
    {
        public string key;
        public float value;
    }

    /// <summary>
    /// 族群精神（Ethos）——族群的精神气质核心
    /// 类比民族精神；CK3 文化支柱 Ethos 的对应物；与信仰/宗教解耦
    /// 例：坚忍（战时损耗降低）、尚武（征召效率）、崇智（革新速率）
    /// </summary>
    [Serializable]
    public class EthosDef
    {
        public string ethosId;
        public List<EffectEntry> effects = new List<EffectEntry>();

        public string GetName() => Localization.Get(ethosId + "_name");
        public string GetDescription() => Localization.Get(ethosId + "_desc");
    }

    /// <summary>
    /// 文化传统（Tradition）——族群承载的习俗条目
    /// CK3 Traditions 类比；可习得/替换；有互斥关系
    /// 例：农耕礼俗、游牧骑射、海洋贸易、工匠精神、祖先祭祀
    /// </summary>
    [Serializable]
    public class TraditionDef
    {
        public string traditionId;
        public List<EffectEntry> effects = new List<EffectEntry>();
        /// <summary>互斥传统（同族群不可同时承载）</summary>
        public List<string> incompatibleWith = new List<string>();
        /// <summary>
        /// 解锁前置革新（革新与文化传统互相约束——必须持有革新才能持有传统）
        /// </summary>
        public List<int> requiredInnovations = new List<int>();
        /// <summary>
        /// 文化特有传统：专属文化 id 列表（空=通用传统，任何文化可持有；
        /// 非空=仅这些文化可持有——CK3 特有传统范式）
        /// </summary>
        public List<int> exclusiveToCultureIds = new List<int>();
        /// <summary>升级来源（特有传统 = 通用传统的强化版——效果数值高于来源；空=无升级关系）</summary>
        public string upgradesFrom = "";

        public string GetName() => Localization.Get(traditionId + "_name");
        public string GetDescription() => Localization.Get(traditionId + "_desc");

        /// <summary>是否通用传统（任何文化可持有）</summary>
        public bool IsCommon => exclusiveToCultureIds == null || exclusiveToCultureIds.Count == 0;

        /// <summary>文化是否可持有（通用=是；特有=文化 id 在专属列表）</summary>
        public bool CanCultureHold(int cultureId)
        {
            if (IsCommon) return true;
            return exclusiveToCultureIds != null && exclusiveToCultureIds.Contains(cultureId);
        }
    }

    /// <summary>
    /// 语言（Language）——族群支柱之一（Ethnos 定义项：共同语言）
    /// 名字池在文化包 CSV（已分离）；此处为语言本体定义
    /// </summary>
    [Serializable]
    public class LanguageDef
    {
        public string languageId;
        /// <summary>示例词汇（风味展示，数据非文本）</summary>
        public List<string> sampleWords = new List<string>();

        // ===== 语言模组化字段（2026-09-03 用户设计：人名/地名后缀——
        // 全部数据驱动可模组化——ContentRegistry 加载——Mods 覆盖） =====

        /// <summary>男性名池（人物生成——GenerateName type=0 用）</summary>
        public List<string> maleNames = new List<string>();
        /// <summary>女性名池（type=1 用）</summary>
        public List<string> femaleNames = new List<string>();
        /// <summary>姓氏池（type=2 用——家族名）</summary>
        public List<string> familyNames = new List<string>();
        /// <summary>城名池（type=3 用——兼容旧城名——新体系走 placeSuffixes 语义组合）</summary>
        public List<string> cityNames = new List<string>();
        /// <summary>
        /// 地名后缀（按语义类组织——组合引擎 PlaceNameGenerator 用：
        /// 某人之地/家园=Region 模糊地区、城=City、堡垒=Fort、新城=NewCity、
        /// 建者之城=FoundedCity、地形类[Plain 平原/Highland 高原/Cliff 山崖]…
        /// 语义=组合自由度——规则限制在生成器）
        /// </summary>
        public List<PlaceSuffixDef> placeSuffixes = new List<PlaceSuffixDef>();
        /// <summary>
        /// 地形词干（自然词——组合引擎词干输入——semantic: mountain 山地/
        /// plain 平原/cliff 山崖/river 河/sea 海/valley 河谷/coast 滨海…
        /// word=语言真实词——同 placeSuffixes 结构）
        /// </summary>
        public List<PlaceSuffixDef> terrainWords = new List<PlaceSuffixDef>();

        public string GetName() => Localization.Get(languageId + "_name");
        public string GetDescription() => Localization.Get(languageId + "_desc");
        public string GetScriptType() => Localization.Get(languageId + "_script");
        public string GetNamingStyle() => Localization.Get(languageId + "_naming");
    }

    /// <summary>
    /// 地名后缀定义（语言内——语义类+词形）：
    /// 语义分类使自由组合可行（山崖+城=山崖之城——翻译为语言真实形态）
    /// </summary>
    [Serializable]
    public class PlaceSuffixDef
    {
        /// <summary>语义类键（region 模糊地/city 城/fort 堡垒/newcity 新城/
        /// founded 建者城/plain 平原/highland 高原/cliff 山崖/port 港…）</summary>
        public string semantic;
        /// <summary>词形（语言真实词——如城=该语言实际词）</summary>
        public string word;
    }

    /// <summary>
    /// 头衔定义（位阶柔性数值体系——2026-09-03 用户设计）：
    /// 三类（官僚/贵族/君主）+ 国名后缀（政权名层）
    /// TitleRank 位阶值=实数（2.0 王/2.4 王上王/1.8 藩王——整数=大等级
    /// 小数=同级微差）——权重=同级内选择——语义=组合/含义字段
    /// </summary>
    [Serializable]
    public class TitleDef
    {
        public string titleId;          // 键名（title_king_cn…）
        /// <summary>类别：bureaucratic 官僚/noble 贵族/monarch 君主/realmSuffix 国名后缀</summary>
        public string kind;
        /// <summary>位阶值（柔性实数——同级微差用小数的）</summary>
        public float rank;
        /// <summary>权重（同级内用哪个头衔——文化/政权默认选择加权）</summary>
        public float weight = 1f;
        /// <summary>语义字段（含义/类别——组合/显示用）</summary>
        public string semantic = "";
    }

    /// <summary>
    /// 族群（EthnicGroup / Ethnos）——文化的高阶共同体形态
    /// 企划书 7.2.3：酋邦后期三革新阈值后形成；7.2.4：稳定认同/边界/共同传统
    /// 支柱：族群精神 + 语言 + 文化传统（挂靠一个文化）
    /// </summary>
    [Serializable]
    public class EthnicGroupDef
    {
        public string groupId;
        /// <summary>挂靠文化（CultureData.cultureId）</summary>
        public int cultureId;
        /// <summary>族群精神（EthosDef.ethosId）</summary>
        public string ethosId;
        /// <summary>语言（LanguageDef.languageId）</summary>
        public string languageId;
        /// <summary>文化传统列表（TraditionDef.traditionId）</summary>
        public List<string> traditionIds = new List<string>();
        /// <summary>
        /// 族群拥有的革新（innovationId 列表——挂载在族群上但**不属于支柱**；
        /// 支柱=族群精神/语言/文化传统；革新增益于族群研究起始点）
        /// </summary>
        public List<int> innovationIds = new List<int>();

        public string GetName() => Localization.Get(groupId + "_name");
        public string GetDescription() => Localization.Get(groupId + "_desc");

        /// <summary>
        /// 复数称谓（群体形式，如"莱希斯人"）
        /// 键：&lt;groupId&gt;_plural；缺键回退单数名
        /// </summary>
        public string GetPluralName() => Localization.Get(groupId + "_plural", GetName());

        /// <summary>
        /// 形容词形式（修饰语，如"莱希斯的商队"）
        /// 键：&lt;groupId&gt;_adj；缺键回退单数名
        /// </summary>
        public string GetAdjectiveName() => Localization.Get(groupId + "_adj", GetName());
    }

    /// <summary>
    /// 家族传统（Family Tradition）——家族世代承载的传承条目
    /// 企划书 9.4：家族团结度/家法/家族文化偏移；FamilyNode.familyTraditions
    /// 为 Dictionary&lt;string,float&gt;（键=traditionId，值=传承强度/代际深度），本定义表解释键
    /// 例：耕读传家、尚武传家、商贾传家、簪缨世家
    /// 解锁前置：requiredInnovations（革新 id 列表，家族须全部持有才能传承该传统）
    /// </summary>
    [Serializable]
    public class FamilyTraditionDef
    {
        public string traditionId;
        public List<EffectEntry> effects = new List<EffectEntry>();
        /// <summary>互斥传统（同家族不可同时传承）</summary>
        public List<string> incompatibleWith = new List<string>();
        /// <summary>解锁前置革新（InnovationDef.innovationId；家族须全部持有）</summary>
        public List<int> requiredInnovations = new List<int>();

        public string GetName() => Localization.Get(traditionId + "_name");
        public string GetDescription() => Localization.Get(traditionId + "_desc");
    }

    /// <summary>
    /// 角色模板（Character Template）——角色生成参数模板（第九篇角色生成）
    /// CreateCharacter 可选套用：六维范围约束 + 人格倾向偏移 + 年龄范围
    /// 范围字段 0 表示不约束；权重供 AI/随机选型
    /// </summary>
    [Serializable]
    public class CharacterTemplateDef
    {
        public string templateId;
        /// <summary>目标身份（Commoner/Noble/Military/Scholar/Ruler...；CharacterRole 位于 Role 命名空间）</summary>
        public CharacterRole role = CharacterRole.Commoner;
        /// <summary>生成年龄范围（0 表示不约束）</summary>
        public int minAge;
        public int maxAge;
        /// <summary>六维下限（顺序：martial/diplomacy/warfare/stewardship/intrigue/learning；0 不约束）</summary>
        public float[] statMin = new float[6];
        /// <summary>六维上限（同上；0 不约束）</summary>
        public float[] statMax = new float[6];
        /// <summary>人格倾向偏移（七维，-100~100；0 不偏移，生成时叠加）</summary>
        public float boldnessBias;
        public float compassionBias;
        public float greedBias;
        public float honorBias;
        public float rationalityBias;
        public float vengefulnessBias;
        public float pietyBias;
        /// <summary>生成权重（AI/随机选型）</summary>
        public float weight = 1f;

        public string GetName() => Localization.Get(templateId + "_name");
        public string GetDescription() => Localization.Get(templateId + "_desc");

        /// <summary>按人格维度取生成偏移（七维 bias 的统一枚举入口；供 CharacterManager.ApplyTemplate 遍历）</summary>
        public float GetPersonalityBias(PersonalityDimension dim) => dim switch
        {
            PersonalityDimension.Boldness => boldnessBias,
            PersonalityDimension.Compassion => compassionBias,
            PersonalityDimension.Greed => greedBias,
            PersonalityDimension.Honor => honorBias,
            PersonalityDimension.Rationality => rationalityBias,
            PersonalityDimension.Vengefulness => vengefulnessBias,
            PersonalityDimension.Piety => pietyBias,
            _ => 0f
        };

        /// <summary>统计范围约束是否启用（任一维 > 0）</summary>
        public bool HasStatConstraints()
        {
            for (int i = 0; i < 6; i++)
                if (statMin != null && statMin[i] > 0f || statMax != null && statMax[i] > 0f) return true;
            return false;
        }
    }
}
