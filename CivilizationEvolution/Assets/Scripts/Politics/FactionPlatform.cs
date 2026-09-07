using System;
using UnityEngine;

namespace CivilizationEvolution.Politics
{
    [Serializable]
    public struct FactionPlatform
    {
        [UnityEngine.Range(-1f, 1f)] public float openness;        // 开放度：负=世袭排他，正=选举包容（政治通道）
        [UnityEngine.Range(-1f, 1f)] public float centralization;  // 集权度：负=地方分权/封建，正=中央集权/官僚
        [UnityEngine.Range(-1f, 1f)] public float commerce;        // 经济取向：负=重农抑商/管制，正=重商/市场
        [UnityEngine.Range(-1f, 1f)] public float taxRelief;       // 税负诉求：正=要求减税，负=可接受增税（如备战/福利）

        public static FactionPlatform operator +(FactionPlatform a, FactionPlatform b) => new FactionPlatform
        {
            openness = a.openness + b.openness,
            centralization = a.centralization + b.centralization,
            commerce = a.commerce + b.commerce,
            taxRelief = a.taxRelief + b.taxRelief
        };
        public FactionPlatform Scaled(float k) => new FactionPlatform
        {
            openness = openness * k, centralization = centralization * k,
            commerce = commerce * k, taxRelief = taxRelief * k
        };
        public void Clamp()
        {
            openness = Mathf.Clamp(openness, -1f, 1f);
            centralization = Mathf.Clamp(centralization, -1f, 1f);
            commerce = Mathf.Clamp(commerce, -1f, 1f);
            taxRelief = Mathf.Clamp(taxRelief, -1f, 1f);
        }
    }
}
