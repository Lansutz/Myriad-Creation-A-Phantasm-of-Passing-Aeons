using System;

namespace CivilizationEvolution.Role
{
    [Serializable]
    public class MentalDisorderDef
    {
        public string id;
        /// <summary>内置回退名（未加载本地化表时用）</summary>
        public string name;
        /// <summary>内置回退描述</summary>
        public string description;
        public bool reversible = true;   // 失智不可逆

        // 属性修正
        public float martialMod;
        public float diplomacyMod;
        public float warfareMod;
        public float stewardshipMod;
        public float intrigueMod;
        public float learningMod;
        public float charmMod;

        /// <summary>压力恢复倍率（&lt;1 恢复慢：抑郁/焦虑使压力缠绵不去）</summary>
        public float stressDecayMult = 1f;
        /// <summary>好感自然衰减倍率（&gt;1 关系恶化快：偏执）</summary>
        public float opinionDecayMult = 1f;

        /// <summary>显示名：本地化表优先（&lt;id&gt;_name），回退内置字段</summary>
        public string GetName() => Localization.Has(id + "_name") ? Localization.Get(id + "_name") : name;
        /// <summary>写实描述：本地化表优先（&lt;id&gt;_desc），回退内置字段</summary>
        public string GetDescription() => Localization.Has(id + "_desc") ? Localization.Get(id + "_desc") : description;
    }
}
