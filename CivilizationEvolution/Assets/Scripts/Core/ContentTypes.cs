using System;
using System.Collections.Generic;

namespace CivilizationEvolution.Core
{
    // ============================================================================
    // 模组化内容类型定义（企划书 1.2 模组扩展规范）
    // 全部类型：Base/Mods 双目录、JsonUtility 加载、同 Id 覆盖、失败仅告警
    // 概念锚点：族群=Ethnos（术语表），族群精神=Ethos（CK3 文化支柱类比），
    //          文化传统=Traditions（CK3 类比），语言=Ethnos 定义项
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
        public string name;
        public string description;
        public List<EffectEntry> effects = new List<EffectEntry>();
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
        public string name;
        public string description;
        public List<EffectEntry> effects = new List<EffectEntry>();
        /// <summary>互斥传统（同族群不可同时承载）</summary>
        public List<string> incompatibleWith = new List<string>();
    }

    /// <summary>
    /// 语言（Language）——族群支柱之一（Ethnos 定义项：共同语言）
    /// 名字池在文化包 CSV（已分离）；此处为语言本体定义
    /// </summary>
    [Serializable]
    public class LanguageDef
    {
        public string languageId;
        public string name;
        /// <summary>书写系统（表意/音节/字母...）</summary>
        public string scriptType;
        public string description;
        /// <summary>命名风格标签（如"多音节+父名后缀"），供名称生成参考</summary>
        public string namingStyle;
        /// <summary>示例词汇（风味展示）</summary>
        public List<string> sampleWords = new List<string>();
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
        public string name;
        public string description;
        /// <summary>挂靠文化（CultureData.cultureId）</summary>
        public int cultureId;
        /// <summary>族群精神（EthosDef.ethosId）</summary>
        public string ethosId;
        /// <summary>语言（LanguageDef.languageId）</summary>
        public string languageId;
        /// <summary>文化传统列表（TraditionDef.traditionId）</summary>
        public List<string> traditionIds = new List<string>();
    }
}
