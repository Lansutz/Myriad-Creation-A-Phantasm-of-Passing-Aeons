using System;
using System.Collections.Generic;
using System.Linq;

namespace CivilizationEvolution.Politics
{
    /// <summary>
    /// 政体约束规则系统（条件子选项组设计·完整版）
    ///
    /// 层级结构：
    /// A. 最高权力
    ///   A1 交接方式：世袭/选举/推举/僭夺/轮座/神命
    ///     - 世袭 → 继承法四轴：范围/支系/性别/长幼
    ///     - 选举 → 选举范围
    ///     - 推举 → 推举主体
    ///     - 主导+0~N次要（数量受行政容量限制）
    ///   A2 分配：头衔分配（独享/家族共享）+ 领地分配（独享/均分/采邑）
    /// B. 中央权力
    ///   B0 有无：无常设 / 有常设
    ///   B1 交接方式：任命/选举/考试/世袭
    ///   B2 机构类型：王庭/议会/长老议事会/官僚中枢/宗教会议/军事委员会
    ///     - 议会 → 议会构成（一院/两院/等级会议）
    /// C. 地方权力
    ///   C1 交接方式：任命/选举/世袭/城市特许（可选项）
    ///     - 任命 → 任命主体
    ///     - 世袭 → 领有身份
    ///     - 选举 → 选举范围
    ///     - 城市特许 → 特许类型
    ///   C2 职能范围：全权自治/征税司法/仅军事/完全直辖
    /// D. 央地结构：单一制/联邦制/邦联制
    /// </summary>
    public static partial class GovernmentConstraints
    {
        // ===== 1. 次要成分限制 =====

        /// <summary>
        /// 次要成分只适用于最高权力交接（A1）
        /// 数量受行政容量限制（默认最多2个，行政容量高可更多）
        /// </summary>
        public static bool AllowsSecondary(GovernmentDimension dimension)
        {
            return dimension == GovernmentDimension.SupremeSuccession;
        }

        /// <summary>获取最大次要成分数量（受行政容量影响）</summary>
        public static int GetMaxSecondaryCount(float administrativeCapacity = 0.5f)
        {
            // 行政容量0-1，默认0.5对应2个次要
            // 低容量(0-0.3): 1个
            // 中容量(0.3-0.7): 2个
            // 高容量(0.7-1.0): 3个
            if (administrativeCapacity < 0.3f) return 1;
            if (administrativeCapacity < 0.7f) return 2;
            return 3;
        }

        /// <summary>政体维度枚举</summary>
        public enum GovernmentDimension
        {
            SupremeSovereignty,   // A0 最高权力归属（君主制/共和制）
            SupremeSuccession,    // A1 最高权力·交接
            SupremeScope,         // A2 最高权力·分配（头衔+领地）
            CentralExistence,     // B0 中央权力·有无
            CentralSuccession,    // B1 中央权力·交接
            CentralInstitution,   // B2 中央权力·机构
            LocalSuccession,      // C1 地方权力·交接
            LocalScope,           // C2 地方权力·职能
            SpatialStructure      // D 央地结构
        }

        // ===== 2. 最高权力归属（A0） =====

        /// <summary>最高权力归属：君主制 / 共和制</summary>
        public enum SupremeSovereignty
        {
            Monarchy,     // 君主制：最高权力在一人（君主）手中，必须有最高头衔
            Republic      // 共和制：最高权力在机构或多人手中，最高头衔可选
        }

        // ===== 2. 继承法四轴（世袭君主专用） =====

        /// <summary>继承法范围轴</summary>
        public enum InheritanceScope
        {
            ClanOnly,           // 限于本族（同姓宗族）
            BloodRegardless     // 血亲不论姓氏（母系/姻亲也可继承）
        }

        /// <summary>继承法支系轴</summary>
        public enum InheritanceBranch
        {
            SeniorLine,         // 长支优先（嫡长支系）
            Fraternal           // 兄终弟及（兄弟相传）
        }

        /// <summary>继承法性别轴</summary>
        public enum InheritanceGender
        {
            MaleOnly,           // 仅男性
            MalePreference,     // 男性优先
            Equal,              // 男女平等
            FemalePreference,   // 女性优先
            FemaleOnly          // 仅女性
        }

        /// <summary>继承法长幼轴</summary>
        public enum InheritanceAge
        {
            ElderFirst,         // 年长者先
            YoungerFirst        // 年幼者先
        }

        // ===== 3. 最高权力分配（头衔+领地） =====

        /// <summary>
        /// 最高头衔分配
        /// 君主制：必须有最高头衔，独享或家族共享
        /// 共和制：最高头衔可选，无则中央机构为主导
        /// </summary>
        public enum TitleDistribution
        {
            Exclusive,          // 独享（一人独占最高头衔——君主制）
            FamilyShared,       // 家族共享（法兰克人式，家族共享最高头衔——君主制）
            HasTitle,           // 有最高头衔（共和制可选——如执政官/独裁官）
            NoTitle             // 无最高头衔（共和制——中央机构为主导，如元老院）
        }

        /// <summary>最高领地分配</summary>
        public enum DomainDistribution
        {
            Exclusive,          // 独享（一人独占所有领地）
            Partible,           // 均分（诸子均分领地）
            Appanage            // 采邑（嫡长子继承核心，其余分封采邑）
        }

        // ===== 4. 中央权力有无 =====

        /// <summary>中央权力有无</summary>
        public enum CentralExistence
        {
            None,               // 无常设中央机构（部落联盟/城邦直治）
            Established         // 有常设中央机构
        }

        // ===== 5. 条件子选项组定义 =====

        /// <summary>子选项组定义</summary>
        public class SubOptionGroup
        {
            public string groupName;
            public string parentDimension;
            public int parentValue;
            public List<SubOption> options = new List<SubOption>();
            public bool isActive = false;
            public bool isOptional = false;  // 是否可选项（如城市特许）
        }

        /// <summary>单个子选项</summary>
        public class SubOption
        {
            public string name;
            public int value;
            public string description;
        }

        /// <summary>四轴继承法子选项（世袭君主专用，多轴并行）</summary>
        public class InheritanceLawSubOptions
        {
            public InheritanceScope scope = InheritanceScope.ClanOnly;
            public InheritanceBranch branch = InheritanceBranch.SeniorLine;
            public InheritanceGender gender = InheritanceGender.MalePreference;
            public InheritanceAge age = InheritanceAge.ElderFirst;

            public string GetDescription()
            {
                string scopeName = scope == InheritanceScope.ClanOnly ? "限于本族" : "血亲不论姓氏";
                string branchName = branch == InheritanceBranch.SeniorLine ? "长支优先" : "兄终弟及";
                string genderName = gender switch
                {
                    InheritanceGender.MaleOnly => "仅男性",
                    InheritanceGender.MalePreference => "男性优先",
                    InheritanceGender.Equal => "男女平等",
                    InheritanceGender.FemalePreference => "女性优先",
                    InheritanceGender.FemaleOnly => "仅女性",
                    _ => "男性优先"
                };
                string ageName = age == InheritanceAge.ElderFirst ? "年长者先" : "年幼者先";
                return $"{scopeName}·{branchName}·{genderName}·{ageName}";
            }
        }

        // ===== 7. 维度间自由组合（无互斥） =====

        public static bool IsCombinationValid(GovernmentComposition comp)
        {
            // 维度之间无互斥，任何组合都有效
            // 唯一约束：B0=无常设时，B2必须=None
            if (comp.centralExistence == CentralExistence.None &&
                comp.centralInstitution.primary != (int)CentralInstitution.None)
                return false;
            return true;
        }

        // ===== 8. 辅助方法 =====

        private static int GetCurrentPrimary(GovernmentDimension dimension, GovernmentComposition comp)
        {
            switch (dimension)
            {
                case GovernmentDimension.SupremeSovereignty: return (int)comp.supremeSovereignty;
                case GovernmentDimension.SupremeSuccession: return comp.supremeSuccession.primary;
                case GovernmentDimension.SupremeScope: return comp.supremeScope.primary;
                case GovernmentDimension.CentralExistence: return (int)comp.centralExistence;
                case GovernmentDimension.CentralSuccession: return comp.centralSuccession.primary;
                case GovernmentDimension.CentralInstitution: return comp.centralInstitution.primary;
                case GovernmentDimension.LocalSuccession: return comp.localSuccession.primary;
                case GovernmentDimension.LocalScope: return comp.localScope.primary;
                case GovernmentDimension.SpatialStructure: return comp.spatialStructure.primary;
                default: return -1;
            }
        }

        public static string GetDimensionName(GovernmentDimension dim)
        {
            return dim switch
            {
                GovernmentDimension.SupremeSovereignty => "最高权力·归属",
                GovernmentDimension.SupremeSuccession => "最高权力·交接",
                GovernmentDimension.SupremeScope => "最高权力·分配",
                GovernmentDimension.CentralExistence => "中央权力·有无",
                GovernmentDimension.CentralSuccession => "中央权力·交接",
                GovernmentDimension.CentralInstitution => "中央权力·机构",
                GovernmentDimension.LocalSuccession => "地方权力·交接",
                GovernmentDimension.LocalScope => "地方权力·职能",
                GovernmentDimension.SpatialStructure => "央地结构",
                _ => dim.ToString()
            };
        }

        public static string GetComponentName(GovernmentDimension dim, int value)
        {
            switch (dim)
            {
                case GovernmentDimension.SupremeSovereignty:
                    return ((SupremeSovereignty)value).ToString();
                case GovernmentDimension.SupremeSuccession:
                    return ((SupremeSuccession)value).ToString();
                case GovernmentDimension.SupremeScope:
                    return ((SupremeScope)value).ToString();
                case GovernmentDimension.CentralExistence:
                    return ((CentralExistence)value).ToString();
                case GovernmentDimension.CentralSuccession:
                    return ((CentralSuccession)value).ToString();
                case GovernmentDimension.CentralInstitution:
                    return ((CentralInstitution)value).ToString();
                case GovernmentDimension.LocalSuccession:
                    return ((LocalSuccession)value).ToString();
                case GovernmentDimension.LocalScope:
                    return ((LocalScope)value).ToString();
                case GovernmentDimension.SpatialStructure:
                    return ((SpatialStructure)value).ToString();
                default:
                    return value.ToString();
            }
        }

        public static List<int> GetAllOptions(GovernmentDimension dimension)
        {
            switch (dimension)
            {
                case GovernmentDimension.SupremeSovereignty:
                    return Enum.GetValues(typeof(SupremeSovereignty)).Cast<int>().ToList();
                case GovernmentDimension.SupremeSuccession:
                    return Enum.GetValues(typeof(SupremeSuccession)).Cast<int>().ToList();
                case GovernmentDimension.SupremeScope:
                    return Enum.GetValues(typeof(SupremeScope)).Cast<int>().ToList();
                case GovernmentDimension.CentralExistence:
                    return Enum.GetValues(typeof(CentralExistence)).Cast<int>().ToList();
                case GovernmentDimension.CentralSuccession:
                    return Enum.GetValues(typeof(CentralSuccession)).Cast<int>().ToList();
                case GovernmentDimension.CentralInstitution:
                    return Enum.GetValues(typeof(CentralInstitution)).Cast<int>().ToList();
                case GovernmentDimension.LocalSuccession:
                    return Enum.GetValues(typeof(LocalSuccession)).Cast<int>().ToList();
                case GovernmentDimension.LocalScope:
                    return Enum.GetValues(typeof(LocalScope)).Cast<int>().ToList();
                case GovernmentDimension.SpatialStructure:
                    return Enum.GetValues(typeof(SpatialStructure)).Cast<int>().ToList();
                default:
                    return new List<int>();
            }
        }

        /// <summary>
        /// 获取某维度的可用选项（根据当前政体组合过滤）
        /// 核心规则：
        /// - A0=共和制 → A1主导成分不能有世袭/神命，但僭夺可作次要成分
        /// - A0=君主制 → A2头衔分配只能有独享/家族共享
        /// - A0=共和制 → A2头衔分配只能有有头衔/无头衔
        /// </summary>
        public static List<int> GetAvailableOptions(
            GovernmentDimension dimension, GovernmentComposition comp, bool isPrimary = true)
        {
            var allOptions = GetAllOptions(dimension);
            var available = new List<int>();

            // A0最高权力归属：不过滤
            if (dimension == GovernmentDimension.SupremeSovereignty)
                return allOptions;

            // A1最高权力交接：根据A0过滤
            if (dimension == GovernmentDimension.SupremeSuccession)
            {
                foreach (var option in allOptions)
                {
                    var succession = (SupremeSuccession)option;
                    if (comp.supremeSovereignty == SupremeSovereignty.Republic)
                    {
                        // 共和制：排除世袭/神命（僭夺可以有，走主次逻辑）
                        if (succession == SupremeSuccession.Hereditary ||
                            succession == SupremeSuccession.Divine)
                            continue;
                    }
                    // 君主制：所有选项可用
                    available.Add(option);
                }
                return available;
            }

            // B1中央权力交接：共和制下排除世袭
            if (dimension == GovernmentDimension.CentralSuccession)
            {
                foreach (var option in allOptions)
                {
                    var succession = (CentralSuccession)option;
                    if (comp.supremeSovereignty == SupremeSovereignty.Republic)
                    {
                        // 共和制：排除世袭
                        if (succession == CentralSuccession.Hereditary)
                            continue;
                    }
                    available.Add(option);
                }
                return available;
            }

            // C1地方权力交接：共和制下排除世袭
            if (dimension == GovernmentDimension.LocalSuccession)
            {
                foreach (var option in allOptions)
                {
                    var succession = (LocalSuccession)option;
                    if (comp.supremeSovereignty == SupremeSovereignty.Republic)
                    {
                        // 共和制：排除世袭（城市特许可以有，属于自治）
                        if (succession == LocalSuccession.Hereditary)
                            continue;
                    }
                    available.Add(option);
                }
                return available;
            }

            // 其他维度：不过滤
            return allOptions;
        }

        /// <summary>
        /// 获取头衔分配的可用选项（根据A0最高权力归属过滤）
        /// 君主制：独享/家族共享
        /// 共和制：有最高头衔/无最高头衔
        /// </summary>
        public static List<TitleDistribution> GetAvailableTitleDistributions(GovernmentComposition comp)
        {
            if (comp.supremeSovereignty == SupremeSovereignty.Monarchy)
            {
                return new List<TitleDistribution>
                {
                    TitleDistribution.Exclusive,
                    TitleDistribution.FamilyShared
                };
            }
            else // Republic
            {
                return new List<TitleDistribution>
                {
                    TitleDistribution.HasTitle,
                    TitleDistribution.NoTitle
                };
            }
        }
    }
}
