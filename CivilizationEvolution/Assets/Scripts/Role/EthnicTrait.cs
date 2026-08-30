using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Culture;
using CivilizationEvolution.Map;

namespace CivilizationEvolution.Role
{
    /// <summary>民族特质（文化的性格/行为倾向基线）</summary>
    public enum EthnicTrait
    {
        FightToTheEnd,      // 死战：不轻易言和
        StrengthenCore,     // 强核：核心区域整合优先
        ManifestDestiny,    // 天命：持续扩张倾向
        BullyTheWeak,       // 欺弱：偏好进攻弱邻
        DefyTheStrong,      // 抗强：威胁感知敏锐
        TotalWar,           // 总体战：战争投入大
        WellTrained,        // 精兵：军队质量高
        CoreDefense,        // 守土：防御优先
        CavalryTradition,   // 骑兵传统：骑兵偏好
        HomelandDefense,    // 卫家：防御加成
        Conqueror,          // 征服者：扩张意愿强
        SiegeCapture,       // 攻城：攻城偏好
        FieldCombatElite    // 野战精锐：野战加成
    }

    /// <summary>民族特质条目（文化概率表——name=枚举名, value=概率 0-100）</summary>
    [Serializable]
    public class EthnicTraitEntry
    {
        public string name;
        public float value;
    }

    /// <summary>
    /// 民族特质系统：文化概率表 → 采样 → 人格/行为修正
    /// 特质映射：扩张意愿（Conqueror/ManifestDestiny/BullyTheWeak 正、HomelandDefense/CoreDefense 负）、
    /// 威胁感知（DefyTheStrong 正）、军事质量（WellTrained/FieldCombatElite 正）
    /// </summary>
    public static class EthnicTraitSystem
    {
        /// <summary>从文化概率表采样命中的特质（每特质按概率独立判定）</summary>
        public static List<EthnicTrait> Sample(CultureData culture, System.Random rng)
        {
            var result = new List<EthnicTrait>();
            if (culture == null || culture.traitProbabilities == null || rng == null) return result;

            foreach (var entry in culture.traitProbabilities)
            {
                if (Enum.TryParse(entry.name, out EthnicTrait trait))
                {
                    if (rng.NextDouble() * 100f < entry.value)
                        result.Add(trait);
                }
            }
            return result;
        }

        /// <summary>扩张意愿修正（正=好战扩张，负=保守防御）</summary>
        public static float GetExpansionModifier(IEnumerable<EthnicTrait> traits)
        {
            float mod = 0f;
            foreach (var t in traits)
            {
                switch (t)
                {
                    case EthnicTrait.Conqueror: mod += 0.3f; break;
                    case EthnicTrait.ManifestDestiny: mod += 0.2f; break;
                    case EthnicTrait.BullyTheWeak: mod += 0.15f; break;
                    case EthnicTrait.HomelandDefense: mod -= 0.2f; break;
                    case EthnicTrait.CoreDefense: mod -= 0.15f; break;
                    case EthnicTrait.TotalWar: mod += 0.1f; break;
                }
            }
            return Mathf.Clamp(mod, -0.5f, 0.5f);
        }

        /// <summary>威胁感知修正（DefyTheStrong 更早识别威胁）</summary>
        public static float GetThreatSensitivity(IEnumerable<EthnicTrait> traits)
        {
            float mod = 0f;
            foreach (var t in traits)
            {
                if (t == EthnicTrait.DefyTheStrong) mod += 0.25f;
                if (t == EthnicTrait.FightToTheEnd) mod += 0.1f;
            }
            return Mathf.Clamp(mod, 0f, 0.4f);
        }

        /// <summary>军事质量修正（精兵/野战/骑兵传统）</summary>
        public static float GetMilitaryModifier(IEnumerable<EthnicTrait> traits)
        {
            float mod = 0f;
            foreach (var t in traits)
            {
                switch (t)
                {
                    case EthnicTrait.WellTrained: mod += 0.15f; break;
                    case EthnicTrait.FieldCombatElite: mod += 0.1f; break;
                    case EthnicTrait.SiegeCapture: mod += 0.1f; break;
                    case EthnicTrait.CavalryTradition: mod += 0.1f; break;
                }
            }
            return Mathf.Clamp(mod, 0f, 0.4f);
        }
    }
}
