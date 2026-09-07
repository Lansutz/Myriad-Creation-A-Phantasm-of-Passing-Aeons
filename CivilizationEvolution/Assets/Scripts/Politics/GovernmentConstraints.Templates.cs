using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Politics
{
    /// <summary>
    /// GovernmentConstraints.Templates —— 政体类型模板（推荐组合数据）（partial static class）
    /// </summary>
    public static partial class GovernmentConstraints
    {

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

    }
}
