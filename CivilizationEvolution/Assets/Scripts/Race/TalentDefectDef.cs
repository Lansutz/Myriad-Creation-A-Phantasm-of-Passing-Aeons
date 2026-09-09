using CivilizationEvolution.Core;
using System;

namespace CivilizationEvolution.Race
{
    [Serializable]
    public class TalentDefectDef
    {
        public string id;
        public string name;
        public bool isTalent;     // true=特殊天赋，false=隐性遗传病
        public string stat;       // 影响属性键：learning / martial / lifespan / appearance
        public float amount;      // 修正量
        public string description;

 /// <summary>显示名：本地化表优先（&lt;id&gt;_name），回退内置字段</summary>
        public string GetName() => Localization.Has(id + "_name") ? Localization.Get(id + "_name") : name;
 /// <summary>写实描述：本地化表优先（&lt;id&gt;_desc），回退内置字段</summary>
        public string GetDescription() => Localization.Has(id + "_desc") ? Localization.Get(id + "_desc") : description;
    }
}
