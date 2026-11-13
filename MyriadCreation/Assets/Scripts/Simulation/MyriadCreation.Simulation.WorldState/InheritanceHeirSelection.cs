using System.Collections.Generic;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.State;

namespace MyriadCreation.Simulation.WorldState
{
    /// <summary>
    /// 继承人选择（host 侧，B6/C）：原 <c>InheritanceLaw</c> 的 <c>DetermineHeir/GetHeirOrderedPool/IsSibling</c>
    /// 依赖 CharacterData，迁到 WorldState host；InheritanceLaw 在 Content 只保留配置/名称/分地规则。
    /// </summary>
    public static class InheritanceHeirSelection
    {
        public static CharacterData DetermineHeir(InheritanceLaw law, List<CharacterData> candidates, CharacterData currentRuler)
        {
            List<CharacterData> heirOrderedPool = GetHeirOrderedPool(law, candidates, currentRuler);
            if (heirOrderedPool == null || heirOrderedPool.Count == 0)
            {
                return null;
            }
            return heirOrderedPool[0];
        }

        public static List<CharacterData> GetHeirOrderedPool(InheritanceLaw law, List<CharacterData> candidates, CharacterData currentRuler)
        {
            if (law == null || candidates == null || candidates.Count == 0)
            {
                return null;
            }
            List<CharacterData> list = new List<CharacterData>(candidates);
            if (law.scope == InheritanceScope.ClanOnly && currentRuler != null)
            {
                list.RemoveAll((CharacterData c) => c.familyId != currentRuler.familyId);
            }
            if (law.gender == InheritanceGender.MaleOnly)
            {
                list.RemoveAll((CharacterData c) => !c.isMale);
            }
            else if (law.gender == InheritanceGender.FemaleOnly)
            {
                list.RemoveAll((CharacterData c) => c.isMale);
            }
            if (list.Count == 0)
            {
                return null;
            }
            list.Sort((CharacterData a, CharacterData b) =>
            {
                if (law.gender == InheritanceGender.MalePreference)
                {
                    int isMale = (a.isMale ? 1 : 0);
                    int isMale2 = (b.isMale ? 1 : 0);
                    if (isMale != isMale2)
                    {
                        return isMale2.CompareTo(isMale);
                    }
                }
                else if (law.gender == InheritanceGender.FemalePreference)
                {
                    int num = ((!a.isMale) ? 1 : 0);
                    int num2 = ((!b.isMale) ? 1 : 0);
                    if (num != num2)
                    {
                        return num2.CompareTo(num);
                    }
                }
                if (currentRuler != null)
                {
                    if (law.branch == InheritanceBranch.EldestLine)
                    {
                        int num3 = ((a.familyId == currentRuler.familyId) ? 1 : 0);
                        int num4 = ((b.familyId == currentRuler.familyId) ? 1 : 0);
                        if (num3 != num4)
                        {
                            return num4.CompareTo(num3);
                        }
                    }
                    else
                    {
                        int num5 = (IsSibling(a, currentRuler) ? 1 : 0);
                        int num6 = (IsSibling(b, currentRuler) ? 1 : 0);
                        if (num5 != num6)
                        {
                            return num6.CompareTo(num5);
                        }
                    }
                }
                return (law.age != InheritanceAge.Seniority) ? a.age.CompareTo(b.age) : b.age.CompareTo(a.age);
            });
            return list;
        }

        private static bool IsSibling(CharacterData a, CharacterData ruler)
        {
            if (ruler == null)
            {
                return false;
            }
            if (a.fatherId >= 0 && a.fatherId == ruler.fatherId)
            {
                return true;
            }
            if (a.motherId >= 0 && a.motherId == ruler.motherId)
            {
                return true;
            }
            return false;
        }
    }
}
