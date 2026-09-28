using System.Collections.Generic;
using UnityEngine;
using MyriadCreation.Core;
using MyriadCreation.Core.Constants;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Dto;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Economy;
using MyriadCreation.Simulation.Events;
using MyriadCreation.Simulation.Generation;
using MyriadCreation.Simulation.Modding;
using MyriadCreation.Simulation.Society;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.Simulation.Definitions;



namespace MyriadCreation.Simulation.Systems
{
 /// LawConstraints.Templates —— 政体类型模板（推荐组合数据）（partial static class）
    public static partial class LawConstraints
    {

 // ===== 6. 政体类型模板（推荐组合，不强制） =====
 /// <summary>政体类型模板</summary>
        public class LawTemplate
        {
            public string name;
            public string description;
            public string category;
            public Dictionary<LawDimension, int> recommendedPrimary = new Dictionary<LawDimension, int>();
            public Dictionary<string, int> recommendedSubOptions = new Dictionary<string, int>();
            public InheritanceLawSubOptions recommendedInheritance = null;  // 继承法四轴
            public TitleDistribution recommendedTitleDist = TitleDistribution.Exclusive;
            public DomainDistribution recommendedDomainDist = DomainDistribution.Exclusive;
        }

 /// <summary>获取所有预设政体模板</summary>

 /// <summary>获取所有预设政体模板</summary>
        public static List<LawTemplate> GetTemplates()
        {
            var templates = new List<LawTemplate>();

 // 官僚君主国（中式）
            templates.Add(new LawTemplate
            {
                name = "官僚君主国",
                description = "世袭君主+全能+有常设+中央任命+官僚中枢+中央派任+完全直辖+单一制",
                category = "君主制",
                recommendedPrimary = new Dictionary<LawDimension, int>
                {
                    { LawDimension.SupremeSuccession, (int)SupremeSuccession.Hereditary },
                    { LawDimension.SupremeScope, (int)SupremeScope.Absolute },
                    { LawDimension.CentralExistence, (int)CentralExistence.Established },
                    { LawDimension.CentralSuccession, (int)CentralSuccession.Appointed },
                    { LawDimension.CentralInstitution, (int)CentralInstitution.BureaucraticCore },
                    { LawDimension.LocalSuccession, (int)LocalSuccession.Appointed },
                    { LawDimension.LocalScope, (int)LocalScope.None },
                    { LawDimension.SpatialStructure, (int)SpatialStructure.Unitary }
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
            templates.Add(new LawTemplate
            {
                name = "封建君主国",
                description = "世袭君主+惯例约束+有常设+官位世袭+王庭+世袭封臣+全权自治+邦联",
                category = "君主制",
                recommendedPrimary = new Dictionary<LawDimension, int>
                {
                    { LawDimension.SupremeSuccession, (int)SupremeSuccession.Hereditary },
                    { LawDimension.SupremeScope, (int)SupremeScope.CustomBound },
                    { LawDimension.CentralExistence, (int)CentralExistence.Established },
                    { LawDimension.CentralSuccession, (int)CentralSuccession.Hereditary },
                    { LawDimension.CentralInstitution, (int)CentralInstitution.Court },
                    { LawDimension.LocalSuccession, (int)LocalSuccession.Hereditary },
                    { LawDimension.LocalScope, (int)LocalScope.FullAutonomy },
                    { LawDimension.SpatialStructure, (int)SpatialStructure.Confederal }
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
            templates.Add(new LawTemplate
            {
                name = "罗马-东罗马式",
                description = "僭主制主导+推举制第1次要+血缘世袭第2次要+有常设+军队推举+元老院+军事委员会",
                category = "君主制",
                recommendedPrimary = new Dictionary<LawDimension, int>
                {
                    { LawDimension.SupremeSuccession, (int)SupremeSuccession.Usurpation },
                    { LawDimension.SupremeScope, (int)SupremeScope.Absolute },
                    { LawDimension.CentralExistence, (int)CentralExistence.Established },
                    { LawDimension.CentralSuccession, (int)CentralSuccession.Appointed },
                    { LawDimension.CentralInstitution, (int)CentralInstitution.MilitaryCouncil },
                    { LawDimension.LocalSuccession, (int)LocalSuccession.Appointed },
                    { LawDimension.LocalScope, (int)LocalScope.FiscalJudicial },
                    { LawDimension.SpatialStructure, (int)SpatialStructure.Unitary }
                },
                recommendedTitleDist = TitleDistribution.Exclusive,
                recommendedDomainDist = DomainDistribution.Exclusive,
                recommendedSubOptions = new Dictionary<string, int>
                {
                    { "任命主体", 2 }  // 军事任免
                }
 // 次要成分：推举制(第1次要) + 血缘世袭(第2次要) // 需要高行政容量才能选2个次要
            });

 // 古典民主共和
            templates.Add(new LawTemplate
            {
                name = "古典民主共和",
                description = "公民大会直接选举+共议制约+有常设+选举+一院议会+地方选举+全权自治+单一制",
                category = "共和制",
                recommendedPrimary = new Dictionary<LawDimension, int>
                {
                    { LawDimension.SupremeSuccession, (int)SupremeSuccession.ElectiveDirect },
                    { LawDimension.SupremeScope, (int)SupremeScope.Consensual },
                    { LawDimension.CentralExistence, (int)CentralExistence.Established },
                    { LawDimension.CentralSuccession, (int)CentralSuccession.Elected },
                    { LawDimension.CentralInstitution, (int)CentralInstitution.Assembly },
                    { LawDimension.LocalSuccession, (int)LocalSuccession.Elected },
                    { LawDimension.LocalScope, (int)LocalScope.FullAutonomy },
                    { LawDimension.SpatialStructure, (int)SpatialStructure.Unitary }
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
            templates.Add(new LawTemplate
            {
                name = "贵族共和",
                description = "推举君主+法理受限+有常设+恩庇推举+元老院+中央任命+征税司法+联邦制",
                category = "共和制",
                recommendedPrimary = new Dictionary<LawDimension, int>
                {
                    { LawDimension.SupremeSuccession, (int)SupremeSuccession.ElectiveRepresentative },
                    { LawDimension.SupremeScope, (int)SupremeScope.LegallyBound },
                    { LawDimension.CentralExistence, (int)CentralExistence.Established },
                    { LawDimension.CentralSuccession, (int)CentralSuccession.Elected },
                    { LawDimension.CentralInstitution, (int)CentralInstitution.Assembly },
                    { LawDimension.LocalSuccession, (int)LocalSuccession.Appointed },
                    { LawDimension.LocalScope, (int)LocalScope.FiscalJudicial },
                    { LawDimension.SpatialStructure, (int)SpatialStructure.Federal }
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
            templates.Add(new LawTemplate
            {
                name = "神权政体",
                description = "神命+神意约束+有常设+教阶任命+宗教会议+教区委任+征税司法+单一制",
                category = "神权制",
                recommendedPrimary = new Dictionary<LawDimension, int>
                {
                    { LawDimension.SupremeSuccession, (int)SupremeSuccession.Divine },
                    { LawDimension.SupremeScope, (int)SupremeScope.DivinelyBound },
                    { LawDimension.CentralExistence, (int)CentralExistence.Established },
                    { LawDimension.CentralSuccession, (int)CentralSuccession.Appointed },
                    { LawDimension.CentralInstitution, (int)CentralInstitution.ReligiousCouncil },
                    { LawDimension.LocalSuccession, (int)LocalSuccession.Appointed },
                    { LawDimension.LocalScope, (int)LocalScope.FiscalJudicial },
                    { LawDimension.SpatialStructure, (int)SpatialStructure.Unitary }
                },
                recommendedTitleDist = TitleDistribution.Exclusive,
                recommendedDomainDist = DomainDistribution.Exclusive,
                recommendedSubOptions = new Dictionary<string, int>
                {
                    { "任命主体", 1 }  // 教区委任
                }
            });

 // 部落联盟（无常设中央机构）
            templates.Add(new LawTemplate
            {
                name = "部落联盟",
                description = "推举君主+共议制约+无常设中央+部落推举+地方世袭+全权自治+邦联",
                category = "混合",
                recommendedPrimary = new Dictionary<LawDimension, int>
                {
                    { LawDimension.SupremeSuccession, (int)SupremeSuccession.ElectiveRepresentative },
                    { LawDimension.SupremeScope, (int)SupremeScope.Consensual },
                    { LawDimension.CentralExistence, (int)CentralExistence.None },  // 无常设
                    { LawDimension.CentralSuccession, (int)CentralSuccession.Elected },
                    { LawDimension.CentralInstitution, (int)CentralInstitution.None },
                    { LawDimension.LocalSuccession, (int)LocalSuccession.Hereditary },
                    { LawDimension.LocalScope, (int)LocalScope.FullAutonomy },
                    { LawDimension.SpatialStructure, (int)SpatialStructure.Confederal }
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
