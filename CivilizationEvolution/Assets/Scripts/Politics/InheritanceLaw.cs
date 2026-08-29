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

    /// <summary>
    /// 头衔继承模式（学术：头衔如何传承）
    /// SoleHeir=唯一继承（整体传位）/ FamilyShared=家族共享（法兰克传统：
    /// 所有兄弟共享"法兰克之王"王号，各自统治领地部分——凡尔登843三分的是
    /// 领地而非头衔）/ Elective=选举（波兰自由选王、神罗选帝侯、教宗）
    /// 注：共治君主（拜占庭 co-emperor）是生前统治形态，与继承正交，不属于继承模式
    /// </summary>
    public enum TitleInheritanceMode
    {
        SoleHeir,       // 唯一继承：头衔整体传予一人
        FamilyShared,   // 家族共享：头衔由全体继承人共同领有（墨洛温/加洛林：诸子皆王）
        Elective        // 选举：由选举产生（波兰 liberum veto、神罗选帝侯）
    }

    /// <summary>
    /// 领地继承模式（学术：土地如何分配）
    /// Primogeniture=长子独得/ Partible=诸子均分（西班牙1157-1300、中国析产）/
    /// Ultimogeniture=幼子守灶/ Seniority=兄终弟及/
    /// Entail=限定继承不可分割（英国 strict settlement、日本家督）/
    /// Collective=家族共有（印度 mitakshara joint family）
    /// </summary>
    public enum LandInheritanceMode
    {
        Primogeniture,  // 长子独得：全部领地归首位继承人
        Partible,       // 诸子均分：领地按继承人人数均分（中国析产、西班牙）
        Ultimogeniture, // 幼子守灶：全部归幼子
        Seniority,      // 兄终弟及：按人序轮转（游牧分封另论）
        Entail,         // 限定继承：不可分割、不可转让（strict settlement）
        Collective      // 家族共有：领地由家族共同经营（mitakshara）
    }

    /// <summary>继承法定义（四轴人序 + 头衔模式 + 领地模式，相互解耦）</summary>
    [Serializable]
    public class InheritanceLaw
    {
        public InheritanceScope scope = InheritanceScope.CognaticKin;
        public InheritanceBranch branch = InheritanceBranch.EldestLine;
        public InheritanceGender gender = InheritanceGender.MalePreference;
        public InheritanceAge age = InheritanceAge.Seniority;

        /// <summary>头衔继承模式（头衔如何传承）</summary>
        public TitleInheritanceMode titleMode = TitleInheritanceMode.SoleHeir;
        /// <summary>领地继承模式（土地如何分配）</summary>
        public LandInheritanceMode landMode = LandInheritanceMode.Primogeniture;

        public InheritanceLaw() { }

        public InheritanceLaw(InheritanceScope scope, InheritanceBranch branch,
            InheritanceGender gender, InheritanceAge age,
            TitleInheritanceMode titleMode = TitleInheritanceMode.SoleHeir,
            LandInheritanceMode landMode = LandInheritanceMode.Primogeniture)
        {
            this.scope = scope;
            this.branch = branch;
            this.gender = gender;
            this.age = age;
            this.titleMode = titleMode;
            this.landMode = landMode;
        }

        // ===== 经典组合 =====

        /// <summary>长子继承（西欧晚期封建）：血亲+长支+男子优先+年长；头衔唯一+领地长子独得</summary>
        public static InheritanceLaw Primogeniture() =>
            new InheritanceLaw(InheritanceScope.CognaticKin, InheritanceBranch.EldestLine,
                InheritanceGender.MalePreference, InheritanceAge.Seniority,
                TitleInheritanceMode.SoleHeir, LandInheritanceMode.Primogeniture);

        /// <summary>幼子守灶（部分游牧/山区传统）：年幼先；头衔唯一+领地幼子独得</summary>
        public static InheritanceLaw Ultimogeniture() =>
            new InheritanceLaw(InheritanceScope.ClanOnly, InheritanceBranch.EldestLine,
                InheritanceGender.MalePreference, InheritanceAge.Juniority,
                TitleInheritanceMode.SoleHeir, LandInheritanceMode.Ultimogeniture);

        /// <summary>兄终弟及（游牧汗国/早期王室）：血亲+横向+男子专属+年长；领地按人序轮转</summary>
        public static InheritanceLaw Tanistry() =>
            new InheritanceLaw(InheritanceScope.CognaticKin, InheritanceBranch.Collateral,
                InheritanceGender.MaleOnly, InheritanceAge.Seniority,
                TitleInheritanceMode.SoleHeir, LandInheritanceMode.Seniority);

        /// <summary>母系继承（女子专属）：血亲+长支+女子专属+年长</summary>
        public static InheritanceLaw Matrilineal() =>
            new InheritanceLaw(InheritanceScope.CognaticKin, InheritanceBranch.EldestLine,
                InheritanceGender.FemaleOnly, InheritanceAge.Seniority,
                TitleInheritanceMode.SoleHeir, LandInheritanceMode.Primogeniture);

        /// <summary>
        /// 法兰克式（墨洛温/加洛林传统）：王号家族共享（诸子皆"法兰克之王"）
        /// + 领地均分（凡尔登 843 三分的是领地，头衔仍为家族共有）
        /// </summary>
        public static InheritanceLaw FrankishPartible() =>
            new InheritanceLaw(InheritanceScope.CognaticKin, InheritanceBranch.EldestLine,
                InheritanceGender.MaleOnly, InheritanceAge.Seniority,
                TitleInheritanceMode.FamilyShared, LandInheritanceMode.Partible);

        /// <summary>萨利克法（法兰克）：男子专属+均分继承（排斥女性≠长子继承）</summary>
        public static InheritanceLaw Salic() =>
            new InheritanceLaw(InheritanceScope.CognaticKin, InheritanceBranch.EldestLine,
                InheritanceGender.MaleOnly, InheritanceAge.Seniority,
                TitleInheritanceMode.SoleHeir, LandInheritanceMode.Partible);

        /// <summary>中国式（唐代定论：宗祧与析产分离）：头衔嫡长子唯一（宗祧/官爵），领地诸子均分（析产）</summary>
        public static InheritanceLaw ChinesePartible() =>
            new InheritanceLaw(InheritanceScope.ClanOnly, InheritanceBranch.EldestLine,
                InheritanceGender.MalePreference, InheritanceAge.Seniority,
                TitleInheritanceMode.SoleHeir, LandInheritanceMode.Partible);

        /// <summary>选举王（波兰自由选王）：头衔选举产生，领地维持原状</summary>
        public static InheritanceLaw ElectiveMonarchy() =>
            new InheritanceLaw(InheritanceScope.CognaticKin, InheritanceBranch.EldestLine,
                InheritanceGender.Equal, InheritanceAge.Seniority,
                TitleInheritanceMode.Elective, LandInheritanceMode.Primogeniture);

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
            string titleName = titleMode switch
            {
                TitleInheritanceMode.SoleHeir => "头衔唯一",
                TitleInheritanceMode.FamilyShared => "头衔家族共享",
                TitleInheritanceMode.Elective => "头衔选举",
                _ => "头衔唯一"
            };
            string landName = landMode switch
            {
                LandInheritanceMode.Primogeniture => "领地长子独得",
                LandInheritanceMode.Partible => "领地均分",
                LandInheritanceMode.Ultimogeniture => "领地幼子守灶",
                LandInheritanceMode.Seniority => "领地轮序",
                LandInheritanceMode.Entail => "领地限定",
                LandInheritanceMode.Collective => "领地共有",
                _ => "领地长子独得"
            };
            return $"{scopeName}·{branchName}·{genderName}·{ageName}｜{titleName}·{landName}";
        }

        /// <summary>
        /// 领地分配（学术：LandInheritanceMode 的分配逻辑）
        /// 给定继承人数与领地数，返回分配方案（每人应得领地数；长子独得/幼子独得/限定=首位全得）
        /// </summary>
        public int[] DistributeLand(int heirCount, int landCount)
        {
            if (heirCount <= 0 || landCount <= 0) return new int[0];
            var result = new int[heirCount];

            switch (landMode)
            {
                case LandInheritanceMode.Partible:
                    // 诸子均分：按人数均分，余数归首位
                    int baseShare = landCount / heirCount;
                    int remainder = landCount % heirCount;
                    for (int i = 0; i < heirCount; i++)
                        result[i] = baseShare + (i < remainder ? 1 : 0);
                    break;
                case LandInheritanceMode.Ultimogeniture:
                    // 幼子守灶：全部归末位（人序末位=幼子）
                    result[heirCount - 1] = landCount;
                    break;
                case LandInheritanceMode.Collective:
                    // 家族共有：名义上归集体（首位代管登记）
                    result[0] = landCount;
                    break;
                default:
                    // Primogeniture/Seniority/Entail：首位（继承人）全得
                    result[0] = landCount;
                    break;
            }
            return result;
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
