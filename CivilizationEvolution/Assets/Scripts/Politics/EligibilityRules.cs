using CivilizationEvolution.Character;
using System.Collections.Generic;
using System;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Politics
{
    [Serializable]
    public class EligibilityRules
    {
        /// <summary>性别资格（5 档，复用 InheritanceGender：优先=软排序、专属=硬过滤）</summary>
        public InheritanceGender gender = InheritanceGender.MalePreference;

        /// <summary>资格范围（候选池/选民/推举人范围）</summary>
        public EligibilityScope scope = EligibilityScope.FreePeople;

        /// <summary>是否性别合格（专属型硬过滤；优先型不硬过滤）</summary>
        public bool IsGenderEligible(bool isMale)
        {
            if (gender == InheritanceGender.MaleOnly) return isMale;
            if (gender == InheritanceGender.FemaleOnly) return !isMale;
            return true; // Preference/Equal 不硬过滤（排序由交接方式决定）
        }

        /// <summary>过滤候选人池（性别硬过滤；范围过滤由调用方提供候选池实现）</summary>
        public List<CharacterData> Filter(List<CharacterData> candidates)
        {
            if (candidates == null) return null;
            var result = new List<CharacterData>(candidates);
            result.RemoveAll(c => !IsGenderEligible(c.isMale));
            return result;
        }

        /// <summary>资格名称（中文）</summary>
        public string GetName()
        {
            string genderName = gender switch
            {
                InheritanceGender.MalePreference => "男子优先",
                InheritanceGender.MaleOnly => "男子专属",
                InheritanceGender.Equal => "男女平等",
                InheritanceGender.FemalePreference => "女子优先",
                InheritanceGender.FemaleOnly => "女子专属",
                _ => "男子优先"
            };
            string scopeName = scope switch
            {
                EligibilityScope.ClanOnly => "限本族",
                EligibilityScope.Citizens => "限公民",
                EligibilityScope.Nobility => "限贵族",
                EligibilityScope.Clergy => "限教阶",
                EligibilityScope.FreePeople => "限自由民",
                EligibilityScope.All => "全体",
                _ => "限自由民"
            };
            return $"{genderName}·{scopeName}";
        }

        /// <summary>
        /// 资格范围 ↔ 经济系统阶层映射（用户定稿：资格与 SocialClass 对接）
        /// ClanOnly 为血缘判定（无阶层映射，返回 null）
        /// </summary>
        public static List<GameEnums.SocialClass> ScopeToSocialClasses(EligibilityScope scope)
        {
            var result = new List<GameEnums.SocialClass>();
            switch (scope)
            {
                case EligibilityScope.Nobility:
                    result.Add(GameEnums.SocialClass.Royalty);
                    result.Add(GameEnums.SocialClass.NobilityClergy);
                    break;
                case EligibilityScope.Citizens:
                    result.Add(GameEnums.SocialClass.MerchantFreeman); // 城邦公民≈市民（自由民中的公民层）
                    break;
                case EligibilityScope.Clergy:
                    result.Add(GameEnums.SocialClass.NobilityClergy); // 教士阶层
                    break;
                case EligibilityScope.FreePeople:
                    result.Add(GameEnums.SocialClass.Royalty);
                    result.Add(GameEnums.SocialClass.NobilityClergy);
                    result.Add(GameEnums.SocialClass.MerchantFreeman);
                    result.Add(GameEnums.SocialClass.Peasant);
                    break;
                case EligibilityScope.All:
                    result.Add(GameEnums.SocialClass.Royalty);
                    result.Add(GameEnums.SocialClass.NobilityClergy);
                    result.Add(GameEnums.SocialClass.MerchantFreeman);
                    result.Add(GameEnums.SocialClass.Peasant);
                    result.Add(GameEnums.SocialClass.Slave);
                    break;
                // ClanOnly：血缘判定，无阶层映射
            }
            return result;
        }

        /// <summary>阶层是否在资格范围内（与经济系统 SocialClass 对接）</summary>
        public bool IsScopeEligible(GameEnums.SocialClass socialClass)
        {
            if (scope == EligibilityScope.ClanOnly) return true; // 血缘判定由调用方实现
            return ScopeToSocialClasses(scope).Contains(socialClass);
        }
    }
}
