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

        public string GetName() => Localization.Get(traditionId + "_name");
        public string GetDescription() => Localization.Get(traditionId + "_desc");
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

        public string GetName() => Localization.Get(languageId + "_name");
        public string GetDescription() => Localization.Get(languageId + "_desc");
        public string GetScriptType() => Localization.Get(languageId + "_script");
        public string GetNamingStyle() => Localization.Get(languageId + "_naming");
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

        public string GetName() => Localization.Get(groupId + "_name");
        public string GetDescription() => Localization.Get(groupId + "_desc");
    }

    /// <summary>
    /// 家族传统（Family Tradition）——家族世代承载的传承条目
    /// 企划书 9.4：家族团结度/家法/家族文化偏移；FamilyNode.familyTraditions
    /// 为 Dictionary&lt;string,float&gt;（键=traditionId，值=传承强度/代际深度），本定义表解释键
    /// 例：耕读传家、尚武传家、商贾传家、簪缨世家
    /// </summary>
    [Serializable]
    public class FamilyTraditionDef
    {
        public string traditionId;
        public List<EffectEntry> effects = new List<EffectEntry>();
        /// <summary>互斥传统（同家族不可同时传承）</summary>
        public List<string> incompatibleWith = new List<string>();

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

        /// <summary>统计范围约束是否启用（任一维 > 0）</summary>
        public bool HasStatConstraints()
        {
            for (int i = 0; i < 6; i++)
                if (statMin != null && statMin[i] > 0f || statMax != null && statMax[i] > 0f) return true;
            return false;
        }
    }
}
