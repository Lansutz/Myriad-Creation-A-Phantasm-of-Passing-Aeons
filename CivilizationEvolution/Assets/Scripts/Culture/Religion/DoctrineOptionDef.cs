using System.Collections.Generic;

namespace CivilizationEvolution.Culture
{
    [System.Serializable]
    public class DoctrineOptionDef
    {
        public string optionId;
 /// <summary>所属支柱（doctrine 教义/ethics 伦理教法/ritual 仪式/
 /// experience 体验/institution 组织/myth 神话/material 物质）</summary>
        public string pillar = "doctrine";
 /// <summary>词条名（中性——历史/宗教学普遍词汇）</summary>
        public string optionName;
 /// <summary>描述</summary>
        public string description = "";
 /// <summary>专属风味化（空=通用词条；非空=仅该宗教可用——
 /// 专属版=中性词条的风味化/加强版——如"吉兹亚"=不信者税的伊斯兰专属名）</summary>
        public List<int> exclusiveReligionIds = new List<int>();
 /// <summary>升级来源（专属版=通用版升级——如伊玛目无误=无中介的组织观升级）</summary>
        public string enhancedFrom = "";
    }
}
