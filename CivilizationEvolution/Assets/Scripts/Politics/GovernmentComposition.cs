using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Role;

namespace CivilizationEvolution.Politics
{
    /// <summary>
    /// 政体系统（用户定稿 v3）
    /// 三个权力层级（最高/中央/地方）× 交接/分配两问 = 六维
    /// + 央地结构（第七维，用户原总表"权力结构·空间"恢复独立）
    /// 政体 = 七维各选一成分的自由组合；成分是模组化接口（可新增）
    /// "王国""帝国"等为外交头衔（纯称号，不产生机制影响）
    /// </summary>

    // ==================== A. 最高权力 ====================

    /// <summary>
    /// A1·最高权力·交接方式（用户定稿：怎么产生——不含形态；民主/贵族/寡头
    /// 是选举范围（EligibilityRules.scope）；禅让=推举（ElectiveDirect）；
    /// 储君预立=世袭内部预案（非独立制度）；选举君主=选举+个人全能（A2=Absolute））
    /// </summary>
    public enum SupremeSuccession
    {
        Hereditary,                 // 世袭：子选项=继承法（四轴+头衔+领地）；预立太子=内部预案
        ElectiveDirect,             // 选举·直接：公民大会/忽里台/红衣主教团/部落议事会推举（禅让在此）
        ElectiveRepresentative,     // 选举·代议：议会/选举人团（神罗选帝侯/英国议会）
        Usurpation,                 // 僭夺：武力夺权（兵强者上）
        Rotation,                   // 轮座：部落长老轮值
        Divine                      // 神命：祭司/神谕认定
    }

    /// <summary>
    /// 层级1 推导（用户定稿：君主/共和不是独立枚举——由交接方式×权力分配推导）
    /// 个人传承系（世袭/僭夺/神命）→ 君主
    /// 选举系（选举/轮座）→ A2=全能=选举君主（教宗/大汗/选帝侯皇帝）；A2=共议=共和
    /// </summary>
    public static class SupremeSuccessionLevel
    {
        public static bool IsMonarchy(SupremeSuccession s)
        {
            return s == SupremeSuccession.Hereditary
                || s == SupremeSuccession.Usurpation
                || s == SupremeSuccession.Divine;
        }

        /// <summary>选举系判定（君主/共和由 A2 权力分配决定）</summary>
        public static bool IsElective(SupremeSuccession s)
        {
            return s == SupremeSuccession.ElectiveDirect
                || s == SupremeSuccession.ElectiveRepresentative
                || s == SupremeSuccession.Rotation;
        }

        /// <summary>
        /// 完整推导：君主制=个人传承系 或（选举系且 A2=全能——当选者个人终身专权）
        /// 共和制=选举系且非全能（共议/受限——多人共治）
        /// </summary>
        public static bool IsMonarchy(SupremeSuccession s, SupremeScope scope)
        {
            if (IsMonarchy(s)) return true;
            if (IsElective(s)) return scope == SupremeScope.Absolute;
            return false;
        }

        public static bool IsRepublic(SupremeSuccession s, SupremeScope scope) => !IsMonarchy(s, scope);

        /// <summary>按政体组合推导（主导成分）</summary>
        public static bool IsMonarchy(GovernmentComposition comp)
        {
            return IsMonarchy((SupremeSuccession)comp.supremeSuccession.primary,
                (SupremeScope)comp.supremeScope.primary);
        }

        /// <summary>按政体组合推导是否共和制</summary>
        public static bool IsRepublic(GovernmentComposition comp) => !IsMonarchy(comp);
    }

    /// <summary>A2·最高权力·分配：最高权力掌握什么/受何约束</summary>
    public enum SupremeScope
    {
        Absolute,       // 全能：立法/司法/军事/财政全揽
        LegallyBound,   // 法理受限：受成文法/宪法约束
        CustomBound,    // 惯例约束：受习惯法/礼制约束（封建契约、礼制）
        Consensual,     // 共议制约：受贵族/公民会议制约
        DivinelyBound   // 神意约束：受祭司/神谕约束
    }

    // ==================== B. 中央权力 ====================

    /// <summary>
    /// B1·中央权力·交接：选人依据（怎么选官——用户定稿：产生机制 4 种；
    /// 考课晋升=管理非产生（移除）；恩庇推举=集体选择的范围（并入 Elected））
    /// </summary>
    public enum CentralSuccession
    {
        Appointed,      // 上级决断：君主/中枢任免
        Elected,        // 集体选择：选举/推举（范围=资格要素：大众/贵族/权贵）
        Examination,    // 客观标准：考试/竞争选任（科举/文官考试）
        Hereditary      // 血缘：官位世袭（世卿世禄）
    }

    /// <summary>
    /// B2·中央权力·分配：机构性质（谁掌权的机构类型；
    /// 一院/两院/等级=议会的构成要素 AssemblyComposition，非独立机构）
    /// </summary>
    public enum CentralInstitution
    {
        None,                   // 无常设：部落临时集会
        Court,                  // 王庭：王室+近臣（宫廷决策）
        Assembly,               // 议会/元老院：代表审议（构成=AssemblyComposition）
        EldersCouncil,          // 长老议事会：长老资格制（部落/贵族传统——非共和）
        BureaucraticCore,       // 官僚中枢：宰相府/尚书台（文书行政）
        ReligiousCouncil,       // 宗教会议：教廷/教阶
        MilitaryCouncil         // 军事委员会：将领共议
    }

    /// <summary>议会构成要素（B2=Assembly 时生效——谁来开会：一院/两院/按等级分庭）</summary>
    public enum AssemblyComposition
    {
        Unicameral,     // 一院制：单一代表院（雅典公民大会/罗马元老院）
        Bicameral,      // 两院制：贵族院+平民院（英国上下院）
        Estate          // 等级会议：按等级分庭（法国三级会议/神罗帝国议会）
    }

    /// <summary>任命主体要素（C1=Appointed 时生效——谁任命地方官）</summary>
    public enum LocalAppointAuthority
    {
        Central,        // 中央派任（流官/总督——郡县/行省）
        Religious,      // 教区委任（教区体系）
        Military        // 军事上级任免（军管区）
    }

    /// <summary>领有身份要素（C1=Hereditary 时生效——世袭领有者身份）</summary>
    public enum LocalLordship
    {
        Vassal,         // 世袭封臣（封建契约——异姓功臣）
        Appanage,       // 宗室采邑（分封宗亲——西周/阿拔斯）
        MeritLord       // 军功领邑（战功封赏）
    }

    // ==================== C. 地方权力 ====================

    /// <summary>
    /// C1·地方权力·交接：产生方式（怎么产生地方权力者——
    /// 任命[主体要素]/选举[范围要素]/世袭[身份要素]；城市特许=自治权契约来源，独立保留）
    /// </summary>
    public enum LocalSuccession
    {
        Appointed,      // 任命（主体=LocalAppointAuthority：中央/教会/军事）
        Elected,        // 选举/推举（范围=资格要素：本地公民/部落/自治市）
        Hereditary,     // 世袭领有（身份=LocalLordship：封臣/宗室/军功）
        CityCharter     // 城市特许自治：自治权来源=特许状契约（中世纪自由城市——内部选举+特许保障）
    }

    /// <summary>C2·地方权力·分配：地方治理管什么（职能范围）</summary>
    public enum LocalScope
    {
        FullAutonomy,       // 全权自治：内政全揽（联邦/邦联单元）
        FiscalJudicial,     // 征税+司法：中央控军权与外交
        MilitaryOnly,       // 仅军事驻防：军管区
        None                // 完全直辖：无地方层级（城邦直治）
    }

    // ==================== D. 央地结构（第七维） ====================

    /// <summary>D1·央地结构·空间（中央与地方法定主权划分/自治度/法律统一/分裂风险）</summary>
    public enum SpatialStructure
    {
        Unitary,        // 单一制：中央集权、法律统一、分裂风险低
        Federal,        // 联邦制：中央地方分权、自治度中
        Confederal      // 邦联制：地方主导、自治度高、分裂风险高
    }

    /// <summary>资格范围（候选人/参与者范围——横切所有交接方式）</summary>
    public enum EligibilityScope
    {
        ClanOnly,       // 本族/宗族内
        Citizens,       // 本城邦公民
        Nobility,       // 贵族/长老阶层
        Clergy,         // 教阶（宗教体系）
        FreePeople,     // 全体自由民
        All             // 全体（含非自由民）
    }

    /// <summary>
    /// 通用资格规则（横切属性——用户定稿：性别等要素不只存在于世袭继承法，
    /// 选举/推举/轮座/神命同样有资格问题）
    /// 门槛语义：先按资格过滤（eligibility.gender 硬过滤），再按各交接方式的
    /// 排序规则（如继承法内 gender 偏好）排序——萨利克=资格男子专属+排序男子优先
    /// </summary>
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

    /// <summary>成分选择（每维：1 主导 + 0~2 次要；int 存该维度枚举序号）</summary>
    [Serializable]
    public class ComponentChoice
    {
        /// <summary>主导成分（●，该维度枚举的 int 值）</summary>
        public int primary;
        /// <summary>次要成分（○，0~2 个，其余选项）</summary>
        public List<int> secondary = new List<int>();

        public ComponentChoice() { }

        public ComponentChoice(int primary, params int[] secondary)
        {
            this.primary = primary;
            if (secondary != null)
                this.secondary.AddRange(secondary);
        }

        /// <summary>是否包含某成分（主导或次要）</summary>
        public bool Contains(int component)
        {
            if (primary == component) return true;
            return secondary.Contains(component);
        }
    }

    /// <summary>
    /// 政体成分组合（七维：三权力层级×交接/分配 + 央地结构）
    /// 政体 = 七维各选一成分的自由组合；成分是模组化接口（可新增）
    /// </summary>
    [Serializable]
    public class GovernmentComposition
    {
        // ===== A. 最高权力 =====
        /// <summary>最高权力归属（A0）：君主制 / 共和制</summary>
        public GovernmentConstraints.SupremeSovereignty supremeSovereignty = GovernmentConstraints.SupremeSovereignty.Monarchy;

        public ComponentChoice supremeSuccession = new ComponentChoice((int)SupremeSuccession.Hereditary);
        public ComponentChoice supremeScope = new ComponentChoice((int)SupremeScope.Absolute);

        /// <summary>最高头衔分配（独享/家族共享——法兰克人式家族共享最高头衔）</summary>
        public GovernmentConstraints.TitleDistribution titleDistribution = GovernmentConstraints.TitleDistribution.Exclusive;

        /// <summary>最高领地分配（独享/均分/采邑——诸子均分或嫡长子继承核心其余分封）</summary>
        public GovernmentConstraints.DomainDistribution domainDistribution = GovernmentConstraints.DomainDistribution.Exclusive;

        // ===== B. 中央权力 =====
        /// <summary>中央权力有无（无常设/有常设——选了"有"才显示机构类型和子选项）</summary>
        public GovernmentConstraints.CentralExistence centralExistence = GovernmentConstraints.CentralExistence.Established;

        public ComponentChoice centralSuccession = new ComponentChoice((int)CentralSuccession.Appointed);
        public ComponentChoice centralInstitution = new ComponentChoice((int)CentralInstitution.BureaucraticCore);

        // ===== C. 地方权力 =====
        public ComponentChoice localSuccession = new ComponentChoice((int)LocalSuccession.Appointed);
        public ComponentChoice localScope = new ComponentChoice((int)LocalScope.FiscalJudicial);

        // ===== D. 央地结构（第七维） =====
        public ComponentChoice spatialStructure = new ComponentChoice((int)SpatialStructure.Unitary);

        /// <summary>通用资格规则（横切属性——用户定稿：性别等要素横切所有交接方式，且**分层应用**：
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
    public static class GovernmentComponentNames
    {
        public static string NameSupremeSuccession(int c) => c switch
        {
            (int)SupremeSuccession.Hereditary => "世袭",
            (int)SupremeSuccession.ElectiveDirect => "选举·直接",
            (int)SupremeSuccession.ElectiveRepresentative => "选举·代议",
            (int)SupremeSuccession.Usurpation => "僭夺",
            (int)SupremeSuccession.Rotation => "轮座",
            (int)SupremeSuccession.Divine => "神命",
            _ => "?"
        };

        public static string NameSupremeScope(int c) => c switch
        {
            (int)SupremeScope.Absolute => "全能",
            (int)SupremeScope.LegallyBound => "法理受限",
            (int)SupremeScope.CustomBound => "惯例约束",
            (int)SupremeScope.Consensual => "共议制约",
            (int)SupremeScope.DivinelyBound => "神意约束",
            _ => "?"
        };

        public static string NameCentralSuccession(int c) => c switch
        {
            (int)CentralSuccession.Appointed => "上级决断",
            (int)CentralSuccession.Elected => "集体选择",
            (int)CentralSuccession.Examination => "客观标准",
            (int)CentralSuccession.Hereditary => "血缘世袭",
            _ => "?"
        };

        public static string NameCentralInstitution(int c) => c switch
        {
            (int)CentralInstitution.None => "无常设",
            (int)CentralInstitution.Court => "王庭",
            (int)CentralInstitution.Assembly => "议会/元老院",
            (int)CentralInstitution.EldersCouncil => "长老议事会",
            (int)CentralInstitution.BureaucraticCore => "官僚中枢",
            (int)CentralInstitution.ReligiousCouncil => "宗教会议",
            (int)CentralInstitution.MilitaryCouncil => "军事委员会",
            _ => "?"
        };

        public static string NameLocalSuccession(int c) => c switch
        {
            (int)LocalSuccession.Appointed => "任命",
            (int)LocalSuccession.Elected => "选举推举",
            (int)LocalSuccession.Hereditary => "世袭领有",
            (int)LocalSuccession.CityCharter => "城市特许自治",
            _ => "?"
        };

        public static string NameLocalScope(int c) => c switch
        {
            (int)LocalScope.FullAutonomy => "全权自治",
            (int)LocalScope.FiscalJudicial => "征税司法",
            (int)LocalScope.MilitaryOnly => "仅军事驻防",
            (int)LocalScope.None => "完全直辖",
            _ => "?"
        };

        public static string NameSpatialStructure(int c) => c switch
        {
            (int)SpatialStructure.Unitary => "单一制",
            (int)SpatialStructure.Federal => "联邦制",
            (int)SpatialStructure.Confederal => "邦联制",
            _ => "?"
        };
    }
}
