using System;
using System.Collections.Generic;
using System.Linq;

namespace CivilizationEvolution.Politics
{
    /// <summary>
    /// 政体约束规则系统（条件子选项组设计）
    ///
    /// 核心设计原则：
    /// 1. 维度之间自由组合 — 君主制可以配长老会/元老院/官僚中枢，选举君主也一样
    /// 2. 同维度内条件子选项组 — 选了特定值后展开对应子选项，不同值的子选项组互斥
    ///    例：C1地方交接选"世袭"→展开领有身份子选项；选"任命"→展开任命主体子选项
    ///    这两组子选项互斥（不能同时配置），但"世袭"和"任命"本身不互斥（可混合）
    /// 3. 类型模板不强制 — 封建/官僚是推荐组合，玩家可自由混合
    /// </summary>
    public static class GovernmentConstraints
    {
        // ===== 1. 次要成分限制 =====

        /// <summary>
        /// 次要成分只适用于最高权力交接（A1）
        /// 其他维度只有主导成分
        /// </summary>
        public static bool AllowsSecondary(GovernmentDimension dimension)
        {
            return dimension == GovernmentDimension.SupremeSuccession;
        }

        /// <summary>政体维度枚举</summary>
        public enum GovernmentDimension
        {
            SupremeSuccession,    // A1 最高权力·交接
            SupremeScope,         // A2 最高权力·分配
            CentralSuccession,    // B1 中央权力·交接
            CentralInstitution,   // B2 中央权力·机构
            LocalSuccession,      // C1 地方权力·交接
            LocalScope,           // C2 地方权力·职能
            SpatialStructure      // D 央地结构
        }

        // ===== 2. 条件子选项组定义 =====

        /// <summary>子选项组定义</summary>
        public class SubOptionGroup
        {
            public string groupName;           // 子选项组名称
            public string parentDimension;     // 所属维度
            public int parentValue;            // 触发该子选项组的父选项值
            public List<SubOption> options = new List<SubOption>();
            public bool isActive = false;      // 是否激活（父选项选中时激活）
        }

        /// <summary>单个子选项</summary>
        public class SubOption
        {
            public string name;
            public int value;
            public string description;
        }

        /// <summary>获取某维度的所有条件子选项组</summary>
        public static List<SubOptionGroup> GetSubOptionGroups(GovernmentDimension dimension)
        {
            var groups = new List<SubOptionGroup>();

            switch (dimension)
            {
                // A1 最高权力交接的子选项
                case GovernmentDimension.SupremeSuccession:
                    // 世袭 → 继承法
                    groups.Add(new SubOptionGroup
                    {
                        groupName = "继承法",
                        parentDimension = "SupremeSuccession",
                        parentValue = (int)SupremeSuccession.Hereditary,
                        options = new List<SubOption>
                        {
                            new SubOption { name = "长子继承", value = 0, description = "长子优先继承" },
                            new SubOption { name = "幼子继承", value = 1, description = "幼子优先继承" },
                            new SubOption { name = "均分继承", value = 2, description = "诸子均分领地" },
                            new SubOption { name = "选举继承", value = 3, description = "家族内选举" }
                        }
                    });
                    // 选举直接 → 选举范围
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
                    // 选举代议 → 选举人团
                    groups.Add(new SubOptionGroup
                    {
                        groupName = "选举人团",
                        parentDimension = "SupremeSuccession",
                        parentValue = (int)SupremeSuccession.ElectiveRepresentative,
                        options = new List<SubOption>
                        {
                            new SubOption { name = "贵族选举", value = 0, description = "贵族阶层选举" },
                            new SubOption { name = "元老院选举", value = 1, description = "元老院选举" },
                            new SubOption { name = "选帝侯", value = 2, description = "选帝侯选举" }
                        }
                    });
                    break;

                // B2 中央权力机构的子选项
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
                    // 城市特许 → 特许类型
                    groups.Add(new SubOptionGroup
                    {
                        groupName = "特许类型",
                        parentDimension = "LocalSuccession",
                        parentValue = (int)LocalSuccession.CityCharter,
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
        /// </summary>
        public static List<SubOptionGroup> GetActiveSubOptionGroups(
            GovernmentDimension dimension, GovernmentComposition comp)
        {
            var allGroups = GetSubOptionGroups(dimension);
            var currentPrimary = GetCurrentPrimary(dimension, comp);

            var activeGroups = new List<SubOptionGroup>();
            foreach (var group in allGroups)
            {
                // 只有父选项被选中时，子选项组才激活
                group.isActive = (group.parentValue == currentPrimary);
                if (group.isActive)
                    activeGroups.Add(group);
            }

            return activeGroups;
        }

        // ===== 3. 政体类型模板（推荐组合，不强制） =====

        /// <summary>政体类型模板</summary>
        public class GovernmentTemplate
        {
            public string name;
            public string description;
            public string category;  // 君主制/共和制/神权制/军事独裁/混合
            public Dictionary<GovernmentDimension, int> recommendedPrimary = new Dictionary<GovernmentDimension, int>();
            public Dictionary<string, int> recommendedSubOptions = new Dictionary<string, int>(); // 推荐子选项
        }

        /// <summary>获取所有预设政体模板</summary>
        public static List<GovernmentTemplate> GetTemplates()
        {
            var templates = new List<GovernmentTemplate>();

            // 官僚君主国（封建vs官僚中的"官僚"类型）
            templates.Add(new GovernmentTemplate
            {
                name = "官僚君主国",
                description = "世袭君主+全能+中央任命+官僚中枢+中央派任+完全直辖+单一制",
                category = "君主制",
                recommendedPrimary = new Dictionary<GovernmentDimension, int>
                {
                    { GovernmentDimension.SupremeSuccession, (int)SupremeSuccession.Hereditary },
                    { GovernmentDimension.SupremeScope, (int)SupremeScope.Absolute },
                    { GovernmentDimension.CentralSuccession, (int)CentralSuccession.Appointed },
                    { GovernmentDimension.CentralInstitution, (int)CentralInstitution.BureaucraticCore },
                    { GovernmentDimension.LocalSuccession, (int)LocalSuccession.Appointed },
                    { GovernmentDimension.LocalScope, (int)LocalScope.None },
                    { GovernmentDimension.SpatialStructure, (int)SpatialStructure.Unitary }
                },
                recommendedSubOptions = new Dictionary<string, int>
                {
                    { "任命主体", 0 }  // 中央派任
                }
            });

            // 封建君主国（封建vs官僚中的"封建"类型）
            templates.Add(new GovernmentTemplate
            {
                name = "封建君主国",
                description = "世袭君主+惯例约束+官位世袭+王庭+世袭领主+全权自治+邦联",
                category = "君主制",
                recommendedPrimary = new Dictionary<GovernmentDimension, int>
                {
                    { GovernmentDimension.SupremeSuccession, (int)SupremeSuccession.Hereditary },
                    { GovernmentDimension.SupremeScope, (int)SupremeScope.CustomBound },
                    { GovernmentDimension.CentralSuccession, (int)CentralSuccession.Hereditary },
                    { GovernmentDimension.CentralInstitution, (int)CentralInstitution.Court },
                    { GovernmentDimension.LocalSuccession, (int)LocalSuccession.Hereditary },
                    { GovernmentDimension.LocalScope, (int)LocalScope.FullAutonomy },
                    { GovernmentDimension.SpatialStructure, (int)SpatialStructure.Confederal }
                },
                recommendedSubOptions = new Dictionary<string, int>
                {
                    { "领有身份", 0 }  // 世袭封臣
                }
            });

            // 古典民主共和
            templates.Add(new GovernmentTemplate
            {
                name = "古典民主共和",
                description = "公民大会直接选举+共议制约+选举+一院议会+地方选举+全权自治+单一制",
                category = "共和制",
                recommendedPrimary = new Dictionary<GovernmentDimension, int>
                {
                    { GovernmentDimension.SupremeSuccession, (int)SupremeSuccession.ElectiveDirect },
                    { GovernmentDimension.SupremeScope, (int)SupremeScope.Consensual },
                    { GovernmentDimension.CentralSuccession, (int)CentralSuccession.Elected },
                    { GovernmentDimension.CentralInstitution, (int)CentralInstitution.Assembly },
                    { GovernmentDimension.LocalSuccession, (int)LocalSuccession.Elected },
                    { GovernmentDimension.LocalScope, (int)LocalScope.FullAutonomy },
                    { GovernmentDimension.SpatialStructure, (int)SpatialStructure.Unitary }
                },
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
                description = "代议选举+法理受限+恩庇推举+元老院+中央任命+征税司法+联邦制",
                category = "共和制",
                recommendedPrimary = new Dictionary<GovernmentDimension, int>
                {
                    { GovernmentDimension.SupremeSuccession, (int)SupremeSuccession.ElectiveRepresentative },
                    { GovernmentDimension.SupremeScope, (int)SupremeScope.LegallyBound },
                    { GovernmentDimension.CentralSuccession, (int)CentralSuccession.Elected },
                    { GovernmentDimension.CentralInstitution, (int)CentralInstitution.Assembly },
                    { GovernmentDimension.LocalSuccession, (int)LocalSuccession.Appointed },
                    { GovernmentDimension.LocalScope, (int)LocalScope.FiscalJudicial },
                    { GovernmentDimension.SpatialStructure, (int)SpatialStructure.Federal }
                },
                recommendedSubOptions = new Dictionary<string, int>
                {
                    { "选举人团", 1 },  // 元老院选举
                    { "议会构成", 0 },  // 一院制（元老院）
                    { "任命主体", 0 }   // 中央派任
                }
            });

            // 神权政体
            templates.Add(new GovernmentTemplate
            {
                name = "神权政体",
                description = "神命+神意约束+教阶任命+宗教会议+教区委任+征税司法+单一制",
                category = "神权制",
                recommendedPrimary = new Dictionary<GovernmentDimension, int>
                {
                    { GovernmentDimension.SupremeSuccession, (int)SupremeSuccession.Divine },
                    { GovernmentDimension.SupremeScope, (int)SupremeScope.DivinelyBound },
                    { GovernmentDimension.CentralSuccession, (int)CentralSuccession.Appointed },
                    { GovernmentDimension.CentralInstitution, (int)CentralInstitution.ReligiousCouncil },
                    { GovernmentDimension.LocalSuccession, (int)LocalSuccession.Appointed },
                    { GovernmentDimension.LocalScope, (int)LocalScope.FiscalJudicial },
                    { GovernmentDimension.SpatialStructure, (int)SpatialStructure.Unitary }
                },
                recommendedSubOptions = new Dictionary<string, int>
                {
                    { "任命主体", 1 }  // 教区委任
                }
            });

            // 军事独裁
            templates.Add(new GovernmentTemplate
            {
                name = "军事独裁",
                description = "僭夺+全能+军事任命+军事委员会+军管+仅军事+单一制",
                category = "军事独裁",
                recommendedPrimary = new Dictionary<GovernmentDimension, int>
                {
                    { GovernmentDimension.SupremeSuccession, (int)SupremeSuccession.Usurpation },
                    { GovernmentDimension.SupremeScope, (int)SupremeScope.Absolute },
                    { GovernmentDimension.CentralSuccession, (int)CentralSuccession.Appointed },
                    { GovernmentDimension.CentralInstitution, (int)CentralInstitution.MilitaryCouncil },
                    { GovernmentDimension.LocalSuccession, (int)LocalSuccession.Appointed },
                    { GovernmentDimension.LocalScope, (int)LocalScope.MilitaryOnly },
                    { GovernmentDimension.SpatialStructure, (int)SpatialStructure.Unitary }
                },
                recommendedSubOptions = new Dictionary<string, int>
                {
                    { "任命主体", 2 }  // 军事任免
                }
            });

            return templates;
        }

        // ===== 4. 维度间自由组合（无互斥） =====

        /// <summary>
        /// 维度之间自由组合，无互斥限制
        /// 君主制可以配长老会/元老院/官僚中枢，选举君主也一样
        /// 封建和官僚本身不互斥，可以混合
        /// </summary>
        public static bool IsCombinationValid(GovernmentComposition comp)
        {
            // 维度之间无互斥，任何组合都有效
            // 唯一约束：同一维度内子选项组的激活状态由父选项决定
            return true;
        }

        // ===== 5. 辅助方法 =====

        private static int GetCurrentPrimary(GovernmentDimension dimension, GovernmentComposition comp)
        {
            switch (dimension)
            {
                case GovernmentDimension.SupremeSuccession: return comp.supremeSuccession.primary;
                case GovernmentDimension.SupremeScope: return comp.supremeScope.primary;
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

        /// <summary>获取某维度所有选项（用于下拉菜单）</summary>
        public static List<int> GetAllOptions(GovernmentDimension dimension)
        {
            switch (dimension)
            {
                case GovernmentDimension.SupremeSuccession:
                    return Enum.GetValues(typeof(SupremeSuccession)).Cast<int>().ToList();
                case GovernmentDimension.SupremeScope:
                    return Enum.GetValues(typeof(SupremeScope)).Cast<int>().ToList();
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
