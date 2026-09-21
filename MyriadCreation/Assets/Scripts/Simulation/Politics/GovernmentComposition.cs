using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Core.Constants;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Dto;
using CivilizationEvolution.Core.Enums;
using CivilizationEvolution.Simulation.Characters;
using CivilizationEvolution.Simulation.Economy;
using CivilizationEvolution.Simulation.Events;
using CivilizationEvolution.Simulation.Generation;
using CivilizationEvolution.Simulation.Modding;
using CivilizationEvolution.Simulation.Society;
using CivilizationEvolution.Simulation.WorldState;




namespace CivilizationEvolution.Simulation.Politics
{
 /// 政体系统（v3）
 /// 三个权力层级（最高/中央/地方）× 交接/分配两问 = 六维
 /// + 央地结构（第七维，用户原总表"权力结构·空间"恢复独立）
 /// 政体 = 七维各选一成分的自由组合；成分是模组化接口（可新增）
 /// "王国""帝国"等为外交头衔（纯称号，不产生机制影响）
 // ==================== A. 最高权力 ====================
 /// A1·最高权力·交接方式（怎么产生——不含形态；民主/贵族/寡头
 /// 是选举范围（EligibilityRules.scope）；禅让=推举（ElectiveDirect）；
 /// 储君预立=世袭内部预案（非独立制度）；选举君主=选举+个人全能（A2=Absolute））

 /// 层级1 推导（君主/共和不是独立枚举——由交接方式×权力分配推导）
 /// 个人传承系（世袭/僭夺/神命）→ 君主
 /// 选举系（选举/轮座）→ A2=全能=选举君主（教宗/大汗/选帝侯皇帝）；A2=共议=共和

 /// <summary>A2·最高权力·分配：最高权力掌握什么/受何约束</summary>

 // ==================== B. 中央权力 ====================
 /// B1·中央权力·交接：选人依据（怎么选官——产生机制 4 种；
 /// 考课晋升=管理非产生（移除）；恩庇推举=集体选择的范围（并入 Elected））

 /// B2·中央权力·分配：机构性质（谁掌权的机构类型；
 /// 一院/两院/等级=议会的构成要素 AssemblyComposition，非独立机构）

 /// <summary>议会构成要素（B2=Assembly 时生效——谁来开会：一院/两院/按等级分庭）</summary>

 /// <summary>任命主体要素（C1=Appointed 时生效——谁任命地方官）</summary>

 /// <summary>领有身份要素（C1=Hereditary 时生效——世袭领有者身份）</summary>

 // ==================== C. 地方权力 ====================
 /// C1·地方权力·交接：产生方式（怎么产生地方权力者——
 /// 任命[主体要素]/选举[范围要素]/世袭[身份要素]；城市特许=自治权契约来源，独立保留）

 /// <summary>C2·地方权力·分配：地方治理管什么（职能范围）</summary>

 // ==================== D. 央地结构（第七维） ====================
 /// <summary>D1·央地结构·空间（中央与地方法定主权划分/自治度/法律统一/分裂风险）</summary>

 /// <summary>资格范围（候选人/参与者范围——横切所有交接方式）</summary>

 /// 通用资格规则（横切属性——性别等要素不只存在于世袭继承法，
 /// 选举/推举/轮座/神命同样有资格问题）
 /// 门槛语义：先按资格过滤（eligibility.gender 硬过滤），再按各交接方式的
 /// 排序规则（如继承法内 gender 偏好）排序——萨利克=资格男子专属+排序男子优先

 /// <summary>成分选择（每维：1 主导 + 0~2 次要；int 存该维度枚举序号）</summary>

 /// 政体成分组合（七维：三权力层级×交接/分配 + 央地结构）
 /// 政体 = 七维各选一成分的自由组合；成分是模组化接口（可新增）
    [Serializable]
    public class GovernmentComposition
    {
 // ===== A. 最高权力 ===== /// <summary>最高权力归属（A0）：君主制 / 共和制</summary>
        public GovernmentConstraints.SupremeSovereignty supremeSovereignty = GovernmentConstraints.SupremeSovereignty.Monarchy;

        public ComponentChoice supremeSuccession = new ComponentChoice((int)SupremeSuccession.Hereditary);
        public ComponentChoice supremeScope = new ComponentChoice((int)SupremeScope.Absolute);

 /// <summary>最高头衔分配（独享/家族共享——法兰克人式家族共享最高头衔）</summary>
        public GovernmentConstraints.TitleDistribution titleDistribution = GovernmentConstraints.TitleDistribution.Exclusive;

 /// <summary>最高领地分配（独享/均分/采邑——诸子均分或嫡长子继承核心其余分封）</summary>
        public GovernmentConstraints.DomainDistribution domainDistribution = GovernmentConstraints.DomainDistribution.Exclusive;

 // ===== B. 中央权力 ===== /// <summary>中央权力有无（无常设/有常设——选了"有"才显示机构类型和子选项）</summary>
        public GovernmentConstraints.CentralExistence centralExistence = GovernmentConstraints.CentralExistence.Established;

        public ComponentChoice centralSuccession = new ComponentChoice((int)CentralSuccession.Appointed);
        public ComponentChoice centralInstitution = new ComponentChoice((int)CentralInstitution.BureaucraticCore);

 // ===== C. 地方权力 =====
        public ComponentChoice localSuccession = new ComponentChoice((int)LocalSuccession.Appointed);
        public ComponentChoice localScope = new ComponentChoice((int)LocalScope.FiscalJudicial);

 // ===== D. 央地结构（第七维） =====
        public ComponentChoice spatialStructure = new ComponentChoice((int)SpatialStructure.Unitary);

 /// <summary>通用资格规则（横切属性——性别等要素横切所有交接方式，且**分层应用**：
 /// 最高/中央/地方各层资格可不同——中世纪欧洲最高权力男子优先、地方可有女领主）</summary>
        public EligibilityRules supremeEligibility = new EligibilityRules();

 /// <summary>中央层资格（B 层——官员/中央机构成员资格；默认与最高层一致语义，可独立配置）</summary>
        public EligibilityRules centralEligibility = new EligibilityRules();

 /// <summary>地方层资格（C 层——地方官/领主资格；可独立配置——女领主继承）</summary>
        public EligibilityRules localEligibility = new EligibilityRules();

 /// <summary>议会构成要素（B2=Assembly 时生效：一院/两院/等级会议）</summary>
        public AssemblyComposition assemblyComposition = AssemblyComposition.Unicameral;

 /// <summary>任命主体要素（C1=Appointed 时生效：中央/教会/军事）</summary>
        public LocalAppointAuthority localAppointAuthority = LocalAppointAuthority.Central;

 /// <summary>领有身份要素（C1=Hereditary 时生效：封臣/宗室/军功）</summary>
        public LocalLordship localLordship = LocalLordship.Vassal;

 /// <summary>A1=世袭时的子选项=继承法（四轴人序+头衔模式+领地模式）</summary>
        public InheritanceLaw successionLaw = InheritanceLaw.Primogeniture();

        public GovernmentComposition() { }

 // ===== 经典政体组合（学术示例） =====
 /// <summary>秦式官僚君主国：世袭+全能+任命+官僚中枢+中央任官+完全直辖+单一制</summary>
        public static GovernmentComposition BureaucraticMonarchy()
        {
            return new GovernmentComposition
            {
                supremeSuccession = new ComponentChoice((int)SupremeSuccession.Hereditary),
                supremeScope = new ComponentChoice((int)SupremeScope.Absolute),
                centralSuccession = new ComponentChoice((int)CentralSuccession.Appointed),
                centralInstitution = new ComponentChoice((int)CentralInstitution.BureaucraticCore),
                localSuccession = new ComponentChoice((int)LocalSuccession.Appointed),
            localAppointAuthority = LocalAppointAuthority.Central,
                localScope = new ComponentChoice((int)LocalScope.None),
                spatialStructure = new ComponentChoice((int)SpatialStructure.Unitary),
            supremeEligibility = new EligibilityRules { gender = InheritanceGender.MalePreference, scope = EligibilityScope.FreePeople },  // 秦：男子优先·自由民
                successionLaw = InheritanceLaw.ChinesePartible() // 宗祧+析产
            };
        }

 /// <summary>西周分封：世袭+全能+官位世袭+王庭+宗室采邑+全权自治+邦联式</summary>
        public static GovernmentComposition FeudalFiefdom()
        {
            return new GovernmentComposition
            {
                supremeSuccession = new ComponentChoice((int)SupremeSuccession.Hereditary),
                supremeScope = new ComponentChoice((int)SupremeScope.Absolute),
                centralSuccession = new ComponentChoice((int)CentralSuccession.Hereditary),
                centralInstitution = new ComponentChoice((int)CentralInstitution.Court),
                localSuccession = new ComponentChoice((int)LocalSuccession.Hereditary),
            localLordship = LocalLordship.Appanage,
                localScope = new ComponentChoice((int)LocalScope.FullAutonomy),
                spatialStructure = new ComponentChoice((int)SpatialStructure.Confederal),
            supremeEligibility = new EligibilityRules { gender = InheritanceGender.MalePreference, scope = EligibilityScope.ClanOnly },  // 西周：男子优先·宗族
                successionLaw = InheritanceLaw.Primogeniture()
            };
        }

 /// <summary>雅典民主：公民大会直接+共议+选举+一院公民大会+地方推举+全权自治+单一制</summary>
        public static GovernmentComposition AthenianDemocracy()
        {
            return new GovernmentComposition
            {
                supremeSuccession = new ComponentChoice((int)SupremeSuccession.ElectiveDirect),
                supremeScope = new ComponentChoice((int)SupremeScope.Consensual),
                centralSuccession = new ComponentChoice((int)CentralSuccession.Elected),
                centralInstitution = new ComponentChoice((int)CentralInstitution.Assembly),
                localSuccession = new ComponentChoice((int)LocalSuccession.Elected),
            assemblyComposition = AssemblyComposition.Unicameral,
                localScope = new ComponentChoice((int)LocalScope.FullAutonomy),
                spatialStructure = new ComponentChoice((int)SpatialStructure.Unitary),
            supremeEligibility = new EligibilityRules { gender = InheritanceGender.MaleOnly, scope = EligibilityScope.Citizens },  // 雅典：男子专属·公民
            };
        }

 /// <summary>罗马共和：委员会选举[双执政官]+法理受限+恩庇+一院元老院+中央任命+征税司法+联邦式</summary>
        public static GovernmentComposition SenatorialRepublic()
        {
            return new GovernmentComposition
            {
                supremeSuccession = new ComponentChoice((int)SupremeSuccession.ElectiveDirect),
                supremeScope = new ComponentChoice((int)SupremeScope.LegallyBound),
                centralSuccession = new ComponentChoice((int)CentralSuccession.Elected),
                centralInstitution = new ComponentChoice((int)CentralInstitution.Assembly),
                localSuccession = new ComponentChoice((int)LocalSuccession.Appointed),
            assemblyComposition = AssemblyComposition.Unicameral,
                localScope = new ComponentChoice((int)LocalScope.FiscalJudicial),
                spatialStructure = new ComponentChoice((int)SpatialStructure.Federal),
            supremeEligibility = new EligibilityRules { gender = InheritanceGender.MaleOnly, scope = EligibilityScope.Citizens },  // 罗马：男子专属·公民
            };
        }

 /// <summary>神权（教廷）：神命+神意约束+任命+宗教会议+教区+全权+单一制</summary>
        public static GovernmentComposition Theocracy()
        {
            return new GovernmentComposition
            {
                supremeSuccession = new ComponentChoice((int)SupremeSuccession.Divine),
                supremeScope = new ComponentChoice((int)SupremeScope.DivinelyBound),
                centralSuccession = new ComponentChoice((int)CentralSuccession.Appointed),
                centralInstitution = new ComponentChoice((int)CentralInstitution.ReligiousCouncil),
                localSuccession = new ComponentChoice((int)LocalSuccession.Appointed),
            localAppointAuthority = LocalAppointAuthority.Religious,
                localScope = new ComponentChoice((int)LocalScope.FullAutonomy),
                spatialStructure = new ComponentChoice((int)SpatialStructure.Unitary),
            supremeEligibility = new EligibilityRules { gender = InheritanceGender.MaleOnly, scope = EligibilityScope.Clergy },  // 神权：男子专属·教阶
            };
        }

 /// <summary>蒙古汗国：世袭[兄终弟及]+全能+军功+军事委员会+世袭封臣+全权+邦联式</summary>
        public static GovernmentComposition MongolHorde()
        {
            return new GovernmentComposition
            {
                supremeSuccession = new ComponentChoice((int)SupremeSuccession.ElectiveDirect, (int)SupremeSuccession.Hereditary), // 忽里台推举+黄金家族世袭
                supremeScope = new ComponentChoice((int)SupremeScope.Absolute),
                centralSuccession = new ComponentChoice((int)CentralSuccession.Appointed),
                centralInstitution = new ComponentChoice((int)CentralInstitution.MilitaryCouncil),
                localSuccession = new ComponentChoice((int)LocalSuccession.Hereditary),
            localLordship = LocalLordship.Vassal,
                localScope = new ComponentChoice((int)LocalScope.FullAutonomy),
                spatialStructure = new ComponentChoice((int)SpatialStructure.Confederal),
            supremeEligibility = new EligibilityRules { gender = InheritanceGender.MaleOnly, scope = EligibilityScope.Nobility },  // 蒙古：男子专属·贵族
                successionLaw = InheritanceLaw.Tanistry() // 兄终弟及
            };
        }

 /// <summary>君主立宪（英式）：代议选举[议会主权]+共议+选举+两院议会+中央任命+征税司法+单一制</summary>
        public static GovernmentComposition ConstitutionalMonarchy()
        {
            return new GovernmentComposition
            {
                supremeSuccession = new ComponentChoice((int)SupremeSuccession.ElectiveRepresentative),
                supremeScope = new ComponentChoice((int)SupremeScope.Consensual),
                centralSuccession = new ComponentChoice((int)CentralSuccession.Elected),
                centralInstitution = new ComponentChoice((int)CentralInstitution.Assembly),
                localSuccession = new ComponentChoice((int)LocalSuccession.Appointed),
            assemblyComposition = AssemblyComposition.Bicameral,
                localScope = new ComponentChoice((int)LocalScope.FiscalJudicial),
                spatialStructure = new ComponentChoice((int)SpatialStructure.Unitary),
            supremeEligibility = new EligibilityRules { gender = InheritanceGender.MalePreference, scope = EligibilityScope.FreePeople },  // 立宪早期：男子优先·自由民
 // 世袭君主不在成分中——头衔（国王）为外交称号
            };
        }

 /// <summary>罗马帝国（皇帝+元老院）：世袭/僭夺+全能+恩庇+一院元老院+中央任命+征税司法+单一制</summary>
        public static GovernmentComposition ImperialSenate()
        {
            return new GovernmentComposition
            {
                supremeSuccession = new ComponentChoice((int)SupremeSuccession.Hereditary, (int)SupremeSuccession.Usurpation),
                supremeScope = new ComponentChoice((int)SupremeScope.Absolute),
                centralSuccession = new ComponentChoice((int)CentralSuccession.Elected),
                centralInstitution = new ComponentChoice((int)CentralInstitution.Assembly),
                localSuccession = new ComponentChoice((int)LocalSuccession.Appointed),
            assemblyComposition = AssemblyComposition.Unicameral,
                localScope = new ComponentChoice((int)LocalScope.FiscalJudicial),
                spatialStructure = new ComponentChoice((int)SpatialStructure.Unitary),
            supremeEligibility = new EligibilityRules { gender = InheritanceGender.MalePreference, scope = EligibilityScope.FreePeople },  // 罗马帝国：男子优先·自由民
            };
        }

 /// <summary>神圣罗马帝国：贵族推举[选帝侯]+共议+恩庇+等级会议[帝国议会]+世袭封臣+全权+邦联式</summary>
        public static GovernmentComposition HolyRomanEmpire()
        {
            return new GovernmentComposition
            {
                supremeSuccession = new ComponentChoice((int)SupremeSuccession.ElectiveRepresentative),
                supremeScope = new ComponentChoice((int)SupremeScope.Absolute), // 皇帝当选后个人全能=选举君主；帝国议会=中央机构（B2）
                centralSuccession = new ComponentChoice((int)CentralSuccession.Elected),
                centralInstitution = new ComponentChoice((int)CentralInstitution.Assembly),
                localSuccession = new ComponentChoice((int)LocalSuccession.Hereditary),
            assemblyComposition = AssemblyComposition.Estate,
                        localLordship = LocalLordship.Vassal,
                localScope = new ComponentChoice((int)LocalScope.FullAutonomy),
                spatialStructure = new ComponentChoice((int)SpatialStructure.Confederal),
            supremeEligibility = new EligibilityRules { gender = InheritanceGender.MaleOnly, scope = EligibilityScope.Nobility },  // 神罗：男子专属·选帝侯贵族
            };
        }

 /// <summary>威尼斯共和：委员会选举[总督]+共议+选举+两院[大议会+元老院]+城市特许+全权+单一制</summary>
        public static GovernmentComposition VenetianRepublic()
        {
            return new GovernmentComposition
            {
                supremeSuccession = new ComponentChoice((int)SupremeSuccession.ElectiveDirect),
                supremeScope = new ComponentChoice((int)SupremeScope.Consensual),
                centralSuccession = new ComponentChoice((int)CentralSuccession.Elected),
                centralInstitution = new ComponentChoice((int)CentralInstitution.Assembly),
                localSuccession = new ComponentChoice((int)LocalSuccession.CityCharter),
            assemblyComposition = AssemblyComposition.Bicameral,
                localScope = new ComponentChoice((int)LocalScope.FullAutonomy),
                spatialStructure = new ComponentChoice((int)SpatialStructure.Unitary),
            supremeEligibility = new EligibilityRules { gender = InheritanceGender.MaleOnly, scope = EligibilityScope.Nobility },  // 威尼斯：男子专属·贵族
            };
        }


 /// <summary>政体名称（中文：最高·中央·地方 三层概要 + 央地结构）</summary>
        public string GetName()
        {
            return $"{GovernmentComponentNames.NameSupremeSuccession(supremeSuccession.primary)}·" +
                   $"{GovernmentComponentNames.NameCentralInstitution(centralInstitution.primary)}·" +
                   $"{GovernmentComponentNames.NameLocalSuccession(localSuccession.primary)}·" +
                   $"{GovernmentComponentNames.NameSpatialStructure(spatialStructure.primary)}";
        }
    }

 /// <summary>成分中文名（按维度——枚举 int 值跨维度重叠，必须分维度查询）</summary>
}
