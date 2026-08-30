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
    public static class GovernmentConstraints
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
            SupremeSuccession,    // A1 最高权力·交接
            SupremeScope,         // A2 最高权力·分配（头衔+领地）
            CentralExistence,     // B0 中央权力·有无
            CentralSuccession,    // B1 中央权力·交接
            CentralInstitution,   // B2 中央权力·机构
            LocalSuccession,      // C1 地方权力·交接
            LocalScope,           // C2 地方权力·职能
            SpatialStructure      // D 央地结构
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

        /// <summary>最高头衔分配</summary>
        public enum TitleDistribution
        {
            Exclusive,          // 独享（一人独占最高头衔）
            FamilyShared        // 家族共享（法兰克人式，家族共享最高头衔）
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

        /// <summary>获取某维度的所有条件子选项组</summary>
        public static List<SubOptionGroup> GetSubOptionGroups(GovernmentDimension dimension)
        {
            var groups = new List<SubOptionGroup>();

            switch (dimension)
            {
                // A1 最高权力交接的子选项
                case GovernmentDimension.SupremeSuccession:
                    // 世袭 → 继承法四轴（多轴并行，不是单选）
                    groups.Add(new SubOptionGroup
                    {
                        groupName = "继承法（四轴）",
                        parentDimension = "SupremeSuccession",
                        parentValue = (int)SupremeSuccession.Hereditary,
                        options = new List<SubOption>
                        {
                            new SubOption { name = "范围轴", value = 0, description = "限于本族 / 血亲不论姓氏" },
                            new SubOption { name = "支系轴", value = 1, description = "长支优先 / 兄终弟及" },
                            new SubOption { name = "性别轴", value = 2, description = "仅男/男优/平等/女优/仅女" },
                            new SubOption { name = "长幼轴", value = 3, description = "年长者先 / 年幼者先" }
                        }
                    });
                    // 选举君主 → 选举范围
                    groups.Add(new SubOptionGroup
                    {
                        groupName = "选举范围",
                        parentDimension = "SupremeSuccession",
                        parentValue = (int)SupremeSuccession.ElectiveDirect,
                        options = new List<SubOption>
                        {
                            new SubOption { name = "全体公民", value = 0, description = "所有公民直接投票" },
                            new SubOption { name = "公民大会", value = 1, description = "公民大会选举" },
                            new SubOption { name = "部落联盟", value = 2, description = "各部落代表选举" }
                        }
                    });
                    // 推举君主 → 推举主体
                    groups.Add(new SubOptionGroup
                    {
                        groupName = "推举主体",
                        parentDimension = "SupremeSuccession",
                        parentValue = (int)SupremeSuccession.ElectiveRepresentative,
                        options = new List<SubOption>
                        {
                            new SubOption { name = "贵族推举", value = 0, description = "贵族阶层推举" },
                            new SubOption { name = "元老院推举", value = 1, description = "元老院推举（罗马式）" },
                            new SubOption { name = "选帝侯", value = 2, description = "选帝侯推举（神罗式）" },
                            new SubOption { name = "军队推举", value = 3, description = "军队推举（罗马禁卫军式）" }
                        }
                    });
                    break;

                // A2 最高权力分配的子选项（头衔分配+领地分配，双轴并行）
                case GovernmentDimension.SupremeScope:
                    groups.Add(new SubOptionGroup
                    {
                        groupName = "头衔分配",
                        parentDimension = "SupremeScope",
                        parentValue = -1,  // 始终显示
                        options = new List<SubOption>
                        {
                            new SubOption { name = "独享", value = 0, description = "一人独占最高头衔" },
                            new SubOption { name = "家族共享", value = 1, description = "法兰克人式，家族共享最高头衔" }
                        }
                    });
                    groups.Add(new SubOptionGroup
                    {
                        groupName = "领地分配",
                        parentDimension = "SupremeScope",
                        parentValue = -1,  // 始终显示
                        options = new List<SubOption>
                        {
                            new SubOption { name = "独享", value = 0, description = "一人独占所有领地" },
                            new SubOption { name = "均分", value = 1, description = "诸子均分领地" },
                            new SubOption { name = "采邑", value = 2, description = "嫡长子继承核心，其余分封采邑" }
                        }
                    });
                    break;

                // B2 中央权力机构的子选项（只有B0=有常设时才显示）
                case GovernmentDimension.CentralInstitution:
                    // 议会 → 议会构成
                    groups.Add(new SubOptionGroup
                    {
                        groupName = "议会构成",
                        parentDimension = "CentralInstitution",
                        parentValue = (int)CentralInstitution.Assembly,
                        options = new List<SubOption>
                        {
                            new SubOption { name = "一院制", value = 0, description = "单一代表院（雅典公民大会/罗马元老院）" },
                            new SubOption { name = "两院制", value = 1, description = "贵族院+平民院（英国上下院）" },
                            new SubOption { name = "等级会议", value = 2, description = "按等级分庭（法国三级会议/神罗帝国议会）" }
                        }
                    });
                    // 长老议事会 → 长老构成
                    groups.Add(new SubOptionGroup
                    {
                        groupName = "长老构成",
                        parentDimension = "CentralInstitution",
                        parentValue = (int)CentralInstitution.CouncilOfElders,
                        options = new List<SubOption>
                        {
                            new SubOption { name = "氏族长老", value = 0, description = "各氏族长老组成" },
                            new SubOption { name = "贵族长老", value = 1, description = "贵族阶层长老组成" },
                            new SubOption { name = "功勋长老", value = 2, description = "按功勋选拔的长老" }
                        }
                    });
                    // 官僚中枢 → 官僚体系
                    groups.Add(new SubOptionGroup
                    {
                        groupName = "官僚体系",
                        parentDimension = "CentralInstitution",
                        parentValue = (int)CentralInstitution.BureaucraticCore,
                        options = new List<SubOption>
                        {
                            new SubOption { name = "三省六部", value = 0, description = "中式三省六部制" },
                            new SubOption { name = "三公九卿", value = 1, description = "中式三公九卿制" },
                            new SubOption { name = "部院制", value = 2, description = "近代部院制" }
                        }
                    });
                    break;

                // C1 地方权力交接的子选项（核心：封建vs官僚的子选项组互斥）
                case GovernmentDimension.LocalSuccession:
                    // 任命 → 任命主体（官僚体系的子选项）
                    groups.Add(new SubOptionGroup
                    {
                        groupName = "任命主体",
                        parentDimension = "LocalSuccession",
                        parentValue = (int)LocalSuccession.Appointed,
                        options = new List<SubOption>
                        {
                            new SubOption { name = "中央派任", value = 0, description = "中央派任流官/总督（郡县/行省）" },
                            new SubOption { name = "教区委任", value = 1, description = "教区委任（教区体系）" },
                            new SubOption { name = "军事任免", value = 2, description = "军事上级任免（军管区）" }
                        }
                    });
                    // 世袭 → 领有身份（封建体系的子选项）
                    groups.Add(new SubOptionGroup
                    {
                        groupName = "领有身份",
                        parentDimension = "LocalSuccession",
                        parentValue = (int)LocalSuccession.Hereditary,
                        options = new List<SubOption>
                        {
                            new SubOption { name = "世袭封臣", value = 0, description = "封建契约——异姓功臣" },
                            new SubOption { name = "宗室采邑", value = 1, description = "分封宗亲——西周/阿拔斯" },
                            new SubOption { name = "军功领邑", value = 2, description = "战功封赏" }
                        }
                    });
                    // 选举 → 选举范围
                    groups.Add(new SubOptionGroup
                    {
                        groupName = "选举范围",
                        parentDimension = "LocalSuccession",
                        parentValue = (int)LocalSuccession.Elected,
                        options = new List<SubOption>
                        {
                            new SubOption { name = "本地公民", value = 0, description = "本地公民选举" },
                            new SubOption { name = "部落推举", value = 1, description = "部落长老推举" },
                            new SubOption { name = "自治市议会", value = 2, description = "自治市议会选举" }
                        }
                    });
                    // 城市特许 → 特许类型（可选项）
                    groups.Add(new SubOptionGroup
                    {
                        groupName = "特许类型",
                        parentDimension = "LocalSuccession",
                        parentValue = (int)LocalSuccession.CityCharter,
                        isOptional = true,  // 可选项
                        options = new List<SubOption>
                        {
                            new SubOption { name = "自由市", value = 0, description = "完全自治自由市" },
                            new SubOption { name = "帝国自由市", value = 1, description = "直属于最高权力的自由市" },
                            new SubOption { name = "特许市镇", value = 2, description = "有限自治特许市镇" }
                        }
                    });
                    break;
            }

            return groups;
        }

        /// <summary>
        /// 获取当前激活的子选项组（基于当前政体组合）
        /// 同一维度内，只有选中的父选项对应的子选项组被激活
        /// 不同父选项的子选项组互斥（不能同时激活）
        /// B2中央机构只有B0=有常设时才显示子选项
        /// </summary>
        public static List<SubOptionGroup> GetActiveSubOptionGroups(
            GovernmentDimension dimension, GovernmentComposition comp)
        {
            var allGroups = GetSubOptionGroups(dimension);
            var currentPrimary = GetCurrentPrimary(dimension, comp);

            // B2中央机构：只有B0=有常设时才显示
            if (dimension == GovernmentDimension.CentralInstitution)
            {
                if (comp.centralExistence != CentralExistence.Established)
                    return new List<SubOptionGroup>();  // 无常设中央机构，不显示子选项
            }

            var activeGroups = new List<SubOptionGroup>();
            foreach (var group in allGroups)
            {
                // parentValue=-1表示始终显示（如A2的头衔分配和领地分配）
                if (group.parentValue == -1)
                {
                    group.isActive = true;
                    activeGroups.Add(group);
                    continue;
                }

                // 只有父选项被选中时，子选项组才激活
                group.isActive = (group.parentValue == currentPrimary);
                if (group.isActive)
                    activeGroups.Add(group);
            }

            return activeGroups;
        }

        // ===== 6. 政体类型模板（推荐组合，不强制） =====

        /// <summary>政体类型模板</summary>
        public class GovernmentTemplate
        {
            public string name;
            public string description;
            public string category;
            public Dictionary<GovernmentDimension, int> recommendedPrimary = new Dictionary<GovernmentDimension, int>();
            public Dictionary<string, int> recommendedSubOptions = new Dictionary<string, int>();
            public InheritanceLawSubOptions recommendedInheritance = null;  // 继承法四轴
            public TitleDistribution recommendedTitleDist = TitleDistribution.Exclusive;
            public DomainDistribution recommendedDomainDist = DomainDistribution.Exclusive;
        }

        /// <summary>获取所有预设政体模板</summary>
        public static List<GovernmentTemplate> GetTemplates()
        {
            var templates = new List<GovernmentTemplate>();

            // 官僚君主国（中式）
            templates.Add(new GovernmentTemplate
            {
                name = "官僚君主国",
                description = "世袭君主+全能+有常设+中央任命+官僚中枢+中央派任+完全直辖+单一制",
                category = "君主制",
                recommendedPrimary = new Dictionary<GovernmentDimension, int>
                {
                    { GovernmentDimension.SupremeSuccession, (int)SupremeSuccession.Hereditary },
                    { GovernmentDimension.SupremeScope, (int)SupremeScope.Absolute },
                    { GovernmentDimension.CentralExistence, (int)CentralExistence.Established },
                    { GovernmentDimension.CentralSuccession, (int)CentralSuccession.Appointed },
                    { GovernmentDimension.CentralInstitution, (int)CentralInstitution.BureaucraticCore },
                    { GovernmentDimension.LocalSuccession, (int)LocalSuccession.Appointed },
                    { GovernmentDimension.LocalScope, (int)LocalScope.None },
                    { GovernmentDimension.SpatialStructure, (int)SpatialStructure.Unitary }
                },
                recommendedInheritance = new InheritanceLawSubOptions
                {
                    scope = InheritanceScope.ClanOnly,
                    branch = InheritanceBranch.SeniorLine,
                    gender = InheritanceGender.MalePreference,
                    age = InheritanceAge.ElderFirst
                },
                recommendedTitleDist = TitleDistribution.Exclusive,
                recommendedDomainDist = DomainDistribution.Appanage,
                recommendedSubOptions = new Dictionary<string, int>
                {
                    { "任命主体", 0 },  // 中央派任
                    { "官僚体系", 0 }   // 三省六部
                }
            });

            // 封建君主国（西欧式）
            templates.Add(new GovernmentTemplate
            {
                name = "封建君主国",
                description = "世袭君主+惯例约束+有常设+官位世袭+王庭+世袭封臣+全权自治+邦联",
                category = "君主制",
                recommendedPrimary = new Dictionary<GovernmentDimension, int>
                {
                    { GovernmentDimension.SupremeSuccession, (int)SupremeSuccession.Hereditary },
                    { GovernmentDimension.SupremeScope, (int)SupremeScope.CustomBound },
                    { GovernmentDimension.CentralExistence, (int)CentralExistence.Established },
                    { GovernmentDimension.CentralSuccession, (int)CentralSuccession.Hereditary },
                    { GovernmentDimension.CentralInstitution, (int)CentralInstitution.Court },
                    { GovernmentDimension.LocalSuccession, (int)LocalSuccession.Hereditary },
                    { GovernmentDimension.LocalScope, (int)LocalScope.FullAutonomy },
                    { GovernmentDimension.SpatialStructure, (int)SpatialStructure.Confederal }
                },
                recommendedInheritance = new InheritanceLawSubOptions
                {
                    scope = InheritanceScope.ClanOnly,
                    branch = InheritanceBranch.SeniorLine,
                    gender = InheritanceGender.MalePreference,
                    age = InheritanceAge.ElderFirst
                },
                recommendedTitleDist = TitleDistribution.FamilyShared,  // 法兰克人式家族共享
                recommendedDomainDist = DomainDistribution.Partible,    // 诸子均分
                recommendedSubOptions = new Dictionary<string, int>
                {
                    { "领有身份", 0 }  // 世袭封臣
                }
            });

            // 罗马-东罗马式（僭主主导+推举次要+世袭次要）
            templates.Add(new GovernmentTemplate
            {
                name = "罗马-东罗马式",
                description = "僭主制主导+推举制第1次要+血缘世袭第2次要+有常设+军队推举+元老院+军事委员会",
                category = "君主制",
                recommendedPrimary = new Dictionary<GovernmentDimension, int>
                {
                    { GovernmentDimension.SupremeSuccession, (int)SupremeSuccession.Usurpation },
                    { GovernmentDimension.SupremeScope, (int)SupremeScope.Absolute },
                    { GovernmentDimension.CentralExistence, (int)CentralExistence.Established },
                    { GovernmentDimension.CentralSuccession, (int)CentralSuccession.Appointed },
                    { GovernmentDimension.CentralInstitution, (int)CentralInstitution.MilitaryCouncil },
                    { GovernmentDimension.LocalSuccession, (int)LocalSuccession.Appointed },
                    { GovernmentDimension.LocalScope, (int)LocalScope.FiscalJudicial },
                    { GovernmentDimension.SpatialStructure, (int)SpatialStructure.Unitary }
                },
                recommendedTitleDist = TitleDistribution.Exclusive,
                recommendedDomainDist = DomainDistribution.Exclusive,
                recommendedSubOptions = new Dictionary<string, int>
                {
                    { "任命主体", 2 }  // 军事任免
                }
                // 次要成分：推举制(第1次要) + 血缘世袭(第2次要)
                // 需要高行政容量才能选2个次要
            });

            // 古典民主共和
            templates.Add(new GovernmentTemplate
            {
                name = "古典民主共和",
                description = "公民大会直接选举+共议制约+有常设+选举+一院议会+地方选举+全权自治+单一制",
                category = "共和制",
                recommendedPrimary = new Dictionary<GovernmentDimension, int>
                {
                    { GovernmentDimension.SupremeSuccession, (int)SupremeSuccession.ElectiveDirect },
                    { GovernmentDimension.SupremeScope, (int)SupremeScope.Consensual },
                    { GovernmentDimension.CentralExistence, (int)CentralExistence.Established },
                    { GovernmentDimension.CentralSuccession, (int)CentralSuccession.Elected },
                    { GovernmentDimension.CentralInstitution, (int)CentralInstitution.Assembly },
                    { GovernmentDimension.LocalSuccession, (int)LocalSuccession.Elected },
                    { GovernmentDimension.LocalScope, (int)LocalScope.FullAutonomy },
                    { GovernmentDimension.SpatialStructure, (int)SpatialStructure.Unitary }
                },
                recommendedTitleDist = TitleDistribution.Exclusive,
                recommendedDomainDist = DomainDistribution.Exclusive,
                recommendedSubOptions = new Dictionary<string, int>
                {
                    { "选举范围", 0 },  // 全体公民
                    { "议会构成", 0 }   // 一院制
                }
            });

            // 贵族共和（元老院）
            templates.Add(new GovernmentTemplate
            {
                name = "贵族共和",
                description = "推举君主+法理受限+有常设+恩庇推举+元老院+中央任命+征税司法+联邦制",
                category = "共和制",
                recommendedPrimary = new Dictionary<GovernmentDimension, int>
                {
                    { GovernmentDimension.SupremeSuccession, (int)SupremeSuccession.ElectiveRepresentative },
                    { GovernmentDimension.SupremeScope, (int)SupremeScope.LegallyBound },
                    { GovernmentDimension.CentralExistence, (int)CentralExistence.Established },
                    { GovernmentDimension.CentralSuccession, (int)CentralSuccession.Elected },
                    { GovernmentDimension.CentralInstitution, (int)CentralInstitution.Assembly },
                    { GovernmentDimension.LocalSuccession, (int)LocalSuccession.Appointed },
                    { GovernmentDimension.LocalScope, (int)LocalScope.FiscalJudicial },
                    { GovernmentDimension.SpatialStructure, (int)SpatialStructure.Federal }
                },
                recommendedTitleDist = TitleDistribution.Exclusive,
                recommendedDomainDist = DomainDistribution.Exclusive,
                recommendedSubOptions = new Dictionary<string, int>
                {
                    { "推举主体", 1 },  // 元老院推举
                    { "议会构成", 0 },  // 一院制（元老院）
                    { "任命主体", 0 }   // 中央派任
                }
            });

            // 神权政体
            templates.Add(new GovernmentTemplate
            {
                name = "神权政体",
                description = "神命+神意约束+有常设+教阶任命+宗教会议+教区委任+征税司法+单一制",
                category = "神权制",
                recommendedPrimary = new Dictionary<GovernmentDimension, int>
                {
                    { GovernmentDimension.SupremeSuccession, (int)SupremeSuccession.Divine },
                    { GovernmentDimension.SupremeScope, (int)SupremeScope.DivinelyBound },
                    { GovernmentDimension.CentralExistence, (int)CentralExistence.Established },
                    { GovernmentDimension.CentralSuccession, (int)CentralSuccession.Appointed },
                    { GovernmentDimension.CentralInstitution, (int)CentralInstitution.ReligiousCouncil },
                    { GovernmentDimension.LocalSuccession, (int)LocalSuccession.Appointed },
                    { GovernmentDimension.LocalScope, (int)LocalScope.FiscalJudicial },
                    { GovernmentDimension.SpatialStructure, (int)SpatialStructure.Unitary }
                },
                recommendedTitleDist = TitleDistribution.Exclusive,
                recommendedDomainDist = DomainDistribution.Exclusive,
                recommendedSubOptions = new Dictionary<string, int>
                {
                    { "任命主体", 1 }  // 教区委任
                }
            });

            // 部落联盟（无常设中央机构）
            templates.Add(new GovernmentTemplate
            {
                name = "部落联盟",
                description = "推举君主+共议制约+无常设中央+部落推举+地方世袭+全权自治+邦联",
                category = "混合",
                recommendedPrimary = new Dictionary<GovernmentDimension, int>
                {
                    { GovernmentDimension.SupremeSuccession, (int)SupremeSuccession.ElectiveRepresentative },
                    { GovernmentDimension.SupremeScope, (int)SupremeScope.Consensual },
                    { GovernmentDimension.CentralExistence, (int)CentralExistence.None },  // 无常设
                    { GovernmentDimension.CentralSuccession, (int)CentralSuccession.Elected },
                    { GovernmentDimension.CentralInstitution, (int)CentralInstitution.None },
                    { GovernmentDimension.LocalSuccession, (int)LocalSuccession.Hereditary },
                    { GovernmentDimension.LocalScope, (int)LocalScope.FullAutonomy },
                    { GovernmentDimension.SpatialStructure, (int)SpatialStructure.Confederal }
                },
                recommendedTitleDist = TitleDistribution.FamilyShared,
                recommendedDomainDist = DomainDistribution.Partible,
                recommendedSubOptions = new Dictionary<string, int>
                {
                    { "推举主体", 0 },  // 贵族推举
                    { "领有身份", 1 }   // 宗室采邑
                }
            });

            return templates;
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
    }
}
