using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Role;

namespace CivilizationEvolution.Politics
{
    /// <summary>
    /// 继承法系统（用户定稿：四轴独立选择）
    /// 轴1 继承范围：限本族 vs 血亲不论姓氏
    /// 轴2 支系：长支优先（长房） vs 兄终弟及（横向）
    /// 轴3 性别：男子优先/男子专属/男女平等/女子优先/女子专属
    /// 轴4 长幼：年长者先 vs 年幼者先
    /// 组合成具体继承法：如长子继承 = 血亲+长支+男子优先+年长；
    /// 游牧汗国 = 血亲+兄终弟及+男子专属+年长；幼子守灶 = 年幼先
    /// </summary>

    /// <summary>轴1·继承范围</summary>
    public enum InheritanceScope
    {
        ClanOnly,       // 限于本族：继承权仅限宗族/家族内（familyId 相同）
        CognaticKin     // 血亲不论姓氏：同血缘即可（父系/母系血亲链）
    }

    /// <summary>轴2·支系</summary>
    public enum InheritanceBranch
    {
        EldestLine,     // 长支优先：长房/嫡系优先于旁支
        Collateral      // 兄终弟及：横向——同辈兄弟优先，然后下一代（游牧汗国式）
    }

    /// <summary>轴3·性别</summary>
    public enum InheritanceGender
    {
        MalePreference,   // 男子优先：男子先于女子
        MaleOnly,         // 男子专属：女子无继承权
        Equal,            // 男女平等：不区分
        FemalePreference, // 女子优先：女子先于男子
        FemaleOnly        // 女子专属：男子无继承权（母系）
    }

    /// <summary>轴4·长幼</summary>
    public enum InheritanceAge
    {
        Seniority,      // 年长者先
        Juniority       // 年幼者先（幼子守灶）
    }

    /// <summary>继承法定义（四轴组合）</summary>
    [Serializable]
    public class InheritanceLaw
    {
        public InheritanceScope scope = InheritanceScope.CognaticKin;
        public InheritanceBranch branch = InheritanceBranch.EldestLine;
        public InheritanceGender gender = InheritanceGender.MalePreference;
        public InheritanceAge age = InheritanceAge.Seniority;

        public InheritanceLaw() { }

        public InheritanceLaw(InheritanceScope scope, InheritanceBranch branch,
            InheritanceGender gender, InheritanceAge age)
        {
            this.scope = scope;
            this.branch = branch;
            this.gender = gender;
            this.age = age;
        }

        // ===== 经典组合 =====

        /// <summary>长子继承（西欧晚期封建）：血亲+长支+男子优先+年长</summary>
        public static InheritanceLaw Primogeniture() =>
            new InheritanceLaw(InheritanceScope.CognaticKin, InheritanceBranch.EldestLine,
                InheritanceGender.MalePreference, InheritanceAge.Seniority);

        /// <summary>幼子守灶（部分游牧/山区传统）：年幼先</summary>
        public static InheritanceLaw Ultimogeniture() =>
            new InheritanceLaw(InheritanceScope.ClanOnly, InheritanceBranch.EldestLine,
                InheritanceGender.MalePreference, InheritanceAge.Juniority);

        /// <summary>兄终弟及（游牧汗国/早期王室）：血亲+横向+男子专属+年长</summary>
        public static InheritanceLaw Tanistry() =>
            new InheritanceLaw(InheritanceScope.CognaticKin, InheritanceBranch.Collateral,
                InheritanceGender.MaleOnly, InheritanceAge.Seniority);

        /// <summary>母系继承（女子专属）：血亲+长支+女子专属+年长</summary>
        public static InheritanceLaw Matrilineal() =>
            new InheritanceLaw(InheritanceScope.CognaticKin, InheritanceBranch.EldestLine,
                InheritanceGender.FemaleOnly, InheritanceAge.Seniority);

        /// <summary>绝对均分继承（诸子平分）：血亲+年长+男子优先（分割在分配层处理）</summary>
        public static InheritanceLaw Partible() =>
            new InheritanceLaw(InheritanceScope.CognaticKin, InheritanceBranch.EldestLine,
                InheritanceGender.MalePreference, InheritanceAge.Seniority);

        /// <summary>继承法名称（中文）</summary>
        public string GetName()
        {
            string scopeName = scope == InheritanceScope.ClanOnly ? "宗族" : "血亲";
            string branchName = branch == InheritanceBranch.EldestLine ? "长支" : "兄终弟及";
            string genderName = gender switch
            {
                InheritanceGender.MalePreference => "男子优先",
                InheritanceGender.MaleOnly => "男子专属",
                InheritanceGender.Equal => "男女平等",
                InheritanceGender.FemalePreference => "女子优先",
                InheritanceGender.FemaleOnly => "女子专属",
                _ => "男子优先"
            };
            string ageName = age == InheritanceAge.Seniority ? "年长先" : "年幼先";
            return $"{scopeName}·{branchName}·{genderName}·{ageName}";
        }

        /// <summary>
        /// 依继承法从候选人中确定继承人（纯判定：过滤+排序后返回首位，无合格者返回 null）
        /// 判定顺序：范围过滤 → 性别过滤/偏好 → 支系 → 长幼
        /// </summary>
        public CharacterData DetermineHeir(List<CharacterData> candidates, CharacterData currentRuler)
        {
            if (candidates == null || candidates.Count == 0) return null;

            var pool = new List<CharacterData>(candidates);

            // 轴1 范围过滤：限本族——仅保留与统治者同家族者
            if (scope == InheritanceScope.ClanOnly && currentRuler != null)
            {
                pool.RemoveAll(c => c.familyId != currentRuler.familyId);
            }

            // 轴3 性别过滤（专属型硬过滤）
            if (gender == InheritanceGender.MaleOnly)
                pool.RemoveAll(c => !c.isMale);
            else if (gender == InheritanceGender.FemaleOnly)
                pool.RemoveAll(c => c.isMale);

            if (pool.Count == 0) return null;

            // 复合排序（单一比较器，优先级：性别偏好 > 支系 > 长幼）
            pool.Sort((a, b) =>
            {
                // 轴3 性别偏好（优先型软排序，最高优先）
                if (gender == InheritanceGender.MalePreference)
                {
                    int ga = a.isMale ? 1 : 0, gb = b.isMale ? 1 : 0;
                    if (ga != gb) return gb.CompareTo(ga);
                }
                else if (gender == InheritanceGender.FemalePreference)
                {
                    int ga = a.isMale ? 0 : 1, gb = b.isMale ? 0 : 1;
                    if (ga != gb) return gb.CompareTo(ga);
                }

                // 轴2 支系：长支（同族优先于旁支）vs 兄终弟及（同辈=与统治者共享父母者优先）
                if (currentRuler != null)
                {
                    if (branch == InheritanceBranch.EldestLine)
                    {
                        int fa = a.familyId == currentRuler.familyId ? 1 : 0;
                        int fb = b.familyId == currentRuler.familyId ? 1 : 0;
                        if (fa != fb) return fb.CompareTo(fa);
                    }
                    else // Collateral 兄终弟及
                    {
                        int sa = IsSibling(a, currentRuler) ? 1 : 0;
                        int sb = IsSibling(b, currentRuler) ? 1 : 0;
                        if (sa != sb) return sb.CompareTo(sa);
                    }
                }

                // 轴4 长幼排序
                return age == InheritanceAge.Seniority ? b.age.CompareTo(a.age) : a.age.CompareTo(b.age);
            });

            return pool[0];
        }

        /// <summary>是否与指定角色为同辈兄弟（共享任一父母）</summary>
        private static bool IsSibling(CharacterData a, CharacterData ruler)
        {
            if (ruler == null) return false;
            if (a.fatherId >= 0 && a.fatherId == ruler.fatherId) return true;
            if (a.motherId >= 0 && a.motherId == ruler.motherId) return true;
            return false;
        }
    }
}
