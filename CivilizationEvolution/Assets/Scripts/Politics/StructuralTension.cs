using System;
using UnityEngine;

namespace CivilizationEvolution.Politics
{
    [Serializable]
    public class StructuralTension
    {
        [UnityEngine.Range(0f, 100f)] public float classMismatch;       // 阶级错配：壮大的阶层被政体排除
        [UnityEngine.Range(0f, 100f)] public float fiscalMilitary;      // 财政-军事压力
        [UnityEngine.Range(0f, 100f)] public float legitimacyErosion;   // 合法性侵蚀
        [UnityEngine.Range(0f, 100f)] public float total;               // 综合张力

        public void Recalculate(RealmSociety society, RealmSituation sit, RealmData realm)
        {
            // 阶级错配：各阶层影响力 × 被政治通道排除程度（新兴自由民权重最高）
            float mismatch = 0f, wsum = 0f;
            foreach (var kv in society.classes)
            {
                var p = kv.Value;
                float access = sit.GetPoliticalAccess(kv.Key);
                float excluded = 1f - Mathf.Clamp01(access);
                float w = p.influence;
                mismatch += p.influence * excluded * (1f + (100f - p.satisfaction) / 200f);
                wsum += w;
            }
            classMismatch = wsum > 0f ? Mathf.Clamp(mismatch / wsum * 100f, 0f, 100f) : 0f;

            // 财政-军事压力：战争（尤其本土）+ 国库空虚 + 高税负痛苦
            float fiscal = 0f;
            if (sit.atWar) fiscal += 25f;
            if (sit.warOnHomeSoil) fiscal += 30f;
            fiscal += Mathf.Clamp(-realm.treasury / 20f, 0f, 30f); // 国库越负压力越大
            float avgTaxPain = 0f; int tc = 0;
            foreach (var v in sit.taxPain.Values) { avgTaxPain += v; tc++; }
            if (tc > 0) fiscal += (avgTaxPain / tc) * 0.15f;
            fiscalMilitary = Mathf.Clamp(fiscal, 0f, 100f);

            // 合法性侵蚀
            legitimacyErosion = Mathf.Clamp((100f - sit.legitimacy) * 0.6f + (100f - sit.stability) * 0.4f, 0f, 100f);

            total = Mathf.Clamp(classMismatch * 0.4f + fiscalMilitary * 0.3f + legitimacyErosion * 0.3f, 0f, 100f);
        }
    }
}
