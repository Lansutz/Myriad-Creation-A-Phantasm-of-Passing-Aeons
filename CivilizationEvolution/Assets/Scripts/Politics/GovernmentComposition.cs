using System;
using System.Collections.Generic;
using UnityEngine;
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

    /// <summary>A1·最高权力·交接：最高权力者如何产生/传承（主体隐含于方式）</summary>
    public enum SupremeSuccession
    {
        Hereditary,             // 世袭：子选项=继承法（四轴人序+头衔+领地）——君主制
        Usurpation,             // 武力僭夺：兵强者上——僭主
        DirectAssembly,         // 公民大会直接：公民直接表决（雅典）
        RepresentativeElection, // 代议选举：议会/代表间接选出
        CollegialElection,      // 委员会选举：多人执政团选出（罗马执政官/威尼斯总督）
        NobleDesignation,       // 贵族推举：长老/贵族共推（选帝侯）
        SuccessorDesignation,   // 生前指定储君：现任指定继承人（中国预立太子/罗马养子继位）
        DivineMandate,          // 神命：祭司认定/神谕——神权
        Rotation                // 轮座：定期轮值（部落轮值）
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

    /// <summary>B1·中央权力·交接：中央机构/官僚如何产生</summary>
    public enum CentralSuccession
    {
        Appointed,          // 君主任命：官僚由君主任免
        HereditaryOffice,   // 官位世袭：官职父子相传（世卿世禄）
        Elected,            // 选举产生：议会/公民选出
        Examination,        // 考试选任：科举/文官考试
        MeritPromotion,     // 考课晋升：绩效/军功升迁（秦汉考课、军功爵）
        Patronage           // 恩庇推举：门阀/荐举（九品中正）
    }

    /// <summary>B2·中央权力·分配：有无中央机构/形态与职能</summary>
    public enum CentralInstitution
    {
        None,                   // 无常设：部落临时集会
        Court,                  // 王庭：王室+近臣（宫廷决策）
        UnicameralAssembly,     // 一院制议会：公民大会/元老院（单院）
        BicameralAssembly,      // 两院制议会：贵族院+平民院（英国上下院）
        EstateAssembly,         // 等级会议：按等级分庭（法国三级会议/帝国议会）
        EldersCouncil,          // 长老议事会：长老资格制（部落/贵族传统——非共和）
        BureaucraticCore,       // 官僚中枢：宰相府/尚书台（文书行政）
        ReligiousCouncil,       // 宗教会议：教廷/长老会（教阶制）
        MilitaryCouncil         // 军事委员会：将领共议
    }

    // ==================== C. 地方权力 ====================

    /// <summary>C1·地方权力·交接：地方权力者如何产生</summary>
    public enum LocalSuccession
    {
        CentralAppointed,   // 中央任命：流官制（郡县/行省）
        HereditaryVassal,   // 世袭封臣：封建契约领有（西欧封臣）
        FeudalAppanage,     // 宗室采邑：分封宗亲（西周/阿拔斯）
        LocalElected,       // 地方推举：城邦/部族自治选举
        CityCharter,        // 城市特许自治：特许状（中世纪自由城市）
        ReligiousAppointed  // 教区委任：宗教体系任免
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
        public ComponentChoice supremeSuccession = new ComponentChoice((int)SupremeSuccession.Hereditary);
        public ComponentChoice supremeScope = new ComponentChoice((int)SupremeScope.Absolute);

        // ===== B. 中央权力 =====
        public ComponentChoice centralSuccession = new ComponentChoice((int)CentralSuccession.Appointed);
        public ComponentChoice centralInstitution = new ComponentChoice((int)CentralInstitution.BureaucraticCore);

        // ===== C. 地方权力 =====
        public ComponentChoice localSuccession = new ComponentChoice((int)LocalSuccession.CentralAppointed);
        public ComponentChoice localScope = new ComponentChoice((int)LocalScope.FiscalJudicial);

        // ===== D. 央地结构（第七维） =====
        public ComponentChoice spatialStructure = new ComponentChoice((int)SpatialStructure.Unitary);

        /// <summary>通用资格规则（横切属性——对所有交接方式生效：世袭候选/选举被选举权/推举资格/轮值资格）</summary>
        public EligibilityRules eligibility = new EligibilityRules();

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
                localSuccession = new ComponentChoice((int)LocalSuccession.CentralAppointed),
                localScope = new ComponentChoice((int)LocalScope.None),
                spatialStructure = new ComponentChoice((int)SpatialStructure.Unitary),
            eligibility = new EligibilityRules { gender = InheritanceGender.MalePreference, scope = EligibilityScope.FreePeople },  // 秦：男子优先·自由民,
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
                centralSuccession = new ComponentChoice((int)CentralSuccession.HereditaryOffice),
                centralInstitution = new ComponentChoice((int)CentralInstitution.Court),
                localSuccession = new ComponentChoice((int)LocalSuccession.FeudalAppanage),
                localScope = new ComponentChoice((int)LocalScope.FullAutonomy),
                spatialStructure = new ComponentChoice((int)SpatialStructure.Confederal),
            eligibility = new EligibilityRules { gender = InheritanceGender.MalePreference, scope = EligibilityScope.ClanOnly },  // 西周：男子优先·宗族,
                successionLaw = InheritanceLaw.Primogeniture()
            };
        }

        /// <summary>雅典民主：公民大会直接+共议+选举+一院公民大会+地方推举+全权自治+单一制</summary>
        public static GovernmentComposition AthenianDemocracy()
        {
            return new GovernmentComposition
            {
                supremeSuccession = new ComponentChoice((int)SupremeSuccession.DirectAssembly),
                supremeScope = new ComponentChoice((int)SupremeScope.Consensual),
                centralSuccession = new ComponentChoice((int)CentralSuccession.Elected),
                centralInstitution = new ComponentChoice((int)CentralInstitution.UnicameralAssembly),
                localSuccession = new ComponentChoice((int)LocalSuccession.LocalElected),
                localScope = new ComponentChoice((int)LocalScope.FullAutonomy),
                spatialStructure = new ComponentChoice((int)SpatialStructure.Unitary),
            eligibility = new EligibilityRules { gender = InheritanceGender.MaleOnly, scope = EligibilityScope.Citizens },  // 雅典：男子专属·公民
            };
        }

        /// <summary>罗马共和：委员会选举[双执政官]+法理受限+恩庇+一院元老院+中央任命+征税司法+联邦式</summary>
        public static GovernmentComposition SenatorialRepublic()
        {
            return new GovernmentComposition
            {
                supremeSuccession = new ComponentChoice((int)SupremeSuccession.CollegialElection),
                supremeScope = new ComponentChoice((int)SupremeScope.LegallyBound),
                centralSuccession = new ComponentChoice((int)CentralSuccession.Patronage),
                centralInstitution = new ComponentChoice((int)CentralInstitution.UnicameralAssembly),
                localSuccession = new ComponentChoice((int)LocalSuccession.CentralAppointed),
                localScope = new ComponentChoice((int)LocalScope.FiscalJudicial),
                spatialStructure = new ComponentChoice((int)SpatialStructure.Federal),
            eligibility = new EligibilityRules { gender = InheritanceGender.MaleOnly, scope = EligibilityScope.Citizens },  // 罗马：男子专属·公民
            };
        }

        /// <summary>神权（教廷）：神命+神意约束+任命+宗教会议+教区+全权+单一制</summary>
        public static GovernmentComposition Theocracy()
        {
            return new GovernmentComposition
            {
                supremeSuccession = new ComponentChoice((int)SupremeSuccession.DivineMandate),
                supremeScope = new ComponentChoice((int)SupremeScope.DivinelyBound),
                centralSuccession = new ComponentChoice((int)CentralSuccession.Appointed),
                centralInstitution = new ComponentChoice((int)CentralInstitution.ReligiousCouncil),
                localSuccession = new ComponentChoice((int)LocalSuccession.ReligiousAppointed),
                localScope = new ComponentChoice((int)LocalScope.FullAutonomy),
                spatialStructure = new ComponentChoice((int)SpatialStructure.Unitary),
            eligibility = new EligibilityRules { gender = InheritanceGender.MaleOnly, scope = EligibilityScope.Clergy },  // 神权：男子专属·教阶
            };
        }

        /// <summary>蒙古汗国：世袭[兄终弟及]+全能+军功+军事委员会+世袭封臣+全权+邦联式</summary>
        public static GovernmentComposition MongolHorde()
        {
            return new GovernmentComposition
            {
                supremeSuccession = new ComponentChoice((int)SupremeSuccession.Hereditary),
                supremeScope = new ComponentChoice((int)SupremeScope.Absolute),
                centralSuccession = new ComponentChoice((int)CentralSuccession.MeritPromotion),
                centralInstitution = new ComponentChoice((int)CentralInstitution.MilitaryCouncil),
                localSuccession = new ComponentChoice((int)LocalSuccession.HereditaryVassal),
                localScope = new ComponentChoice((int)LocalScope.FullAutonomy),
                spatialStructure = new ComponentChoice((int)SpatialStructure.Confederal),
            eligibility = new EligibilityRules { gender = InheritanceGender.MaleOnly, scope = EligibilityScope.Nobility },  // 蒙古：男子专属·贵族,
                successionLaw = InheritanceLaw.Tanistry() // 兄终弟及
            };
        }

        /// <summary>君主立宪（英式）：代议选举[议会主权]+共议+选举+两院议会+中央任命+征税司法+单一制</summary>
        public static GovernmentComposition ConstitutionalMonarchy()
        {
            return new GovernmentComposition
            {
                supremeSuccession = new ComponentChoice((int)SupremeSuccession.RepresentativeElection),
                supremeScope = new ComponentChoice((int)SupremeScope.Consensual),
                centralSuccession = new ComponentChoice((int)CentralSuccession.Elected),
                centralInstitution = new ComponentChoice((int)CentralInstitution.BicameralAssembly),
                localSuccession = new ComponentChoice((int)LocalSuccession.CentralAppointed),
                localScope = new ComponentChoice((int)LocalScope.FiscalJudicial),
                spatialStructure = new ComponentChoice((int)SpatialStructure.Unitary),
            eligibility = new EligibilityRules { gender = InheritanceGender.MalePreference, scope = EligibilityScope.FreePeople },  // 立宪早期：男子优先·自由民,
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
                centralSuccession = new ComponentChoice((int)CentralSuccession.Patronage),
                centralInstitution = new ComponentChoice((int)CentralInstitution.UnicameralAssembly),
                localSuccession = new ComponentChoice((int)LocalSuccession.CentralAppointed),
                localScope = new ComponentChoice((int)LocalScope.FiscalJudicial),
                spatialStructure = new ComponentChoice((int)SpatialStructure.Unitary),
            eligibility = new EligibilityRules { gender = InheritanceGender.MalePreference, scope = EligibilityScope.FreePeople },  // 罗马帝国：男子优先·自由民
            };
        }

        /// <summary>神圣罗马帝国：贵族推举[选帝侯]+共议+恩庇+等级会议[帝国议会]+世袭封臣+全权+邦联式</summary>
        public static GovernmentComposition HolyRomanEmpire()
        {
            return new GovernmentComposition
            {
                supremeSuccession = new ComponentChoice((int)SupremeSuccession.NobleDesignation),
                supremeScope = new ComponentChoice((int)SupremeScope.Consensual),
                centralSuccession = new ComponentChoice((int)CentralSuccession.Patronage),
                centralInstitution = new ComponentChoice((int)CentralInstitution.EstateAssembly),
                localSuccession = new ComponentChoice((int)LocalSuccession.HereditaryVassal),
                localScope = new ComponentChoice((int)LocalScope.FullAutonomy),
                spatialStructure = new ComponentChoice((int)SpatialStructure.Confederal),
            eligibility = new EligibilityRules { gender = InheritanceGender.MaleOnly, scope = EligibilityScope.Nobility },  // 神罗：男子专属·选帝侯贵族
            };
        }

        /// <summary>威尼斯共和：委员会选举[总督]+共议+选举+两院[大议会+元老院]+城市特许+全权+单一制</summary>
        public static GovernmentComposition VenetianRepublic()
        {
            return new GovernmentComposition
            {
                supremeSuccession = new ComponentChoice((int)SupremeSuccession.CollegialElection),
                supremeScope = new ComponentChoice((int)SupremeScope.Consensual),
                centralSuccession = new ComponentChoice((int)CentralSuccession.Elected),
                centralInstitution = new ComponentChoice((int)CentralInstitution.BicameralAssembly),
                localSuccession = new ComponentChoice((int)LocalSuccession.CityCharter),
                localScope = new ComponentChoice((int)LocalScope.FullAutonomy),
                spatialStructure = new ComponentChoice((int)SpatialStructure.Unitary),
            eligibility = new EligibilityRules { gender = InheritanceGender.MaleOnly, scope = EligibilityScope.Nobility },  // 威尼斯：男子专属·贵族
            };
        }

        /// <summary>储君制帝国（唐/明）：生前指定储君+全能+考课晋升+官僚中枢+中央任官+完全直辖+单一制</summary>
        public static GovernmentComposition SuccessorDesignationEmpire()
        {
            return new GovernmentComposition
            {
                supremeSuccession = new ComponentChoice((int)SupremeSuccession.SuccessorDesignation),
                supremeScope = new ComponentChoice((int)SupremeScope.Absolute),
                centralSuccession = new ComponentChoice((int)CentralSuccession.MeritPromotion),
                centralInstitution = new ComponentChoice((int)CentralInstitution.BureaucraticCore),
                localSuccession = new ComponentChoice((int)LocalSuccession.CentralAppointed),
                localScope = new ComponentChoice((int)LocalScope.None),
                spatialStructure = new ComponentChoice((int)SpatialStructure.Unitary),
            eligibility = new EligibilityRules { gender = InheritanceGender.MalePreference, scope = EligibilityScope.FreePeople },  // 储君制：男子优先·自由民
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
            (int)SupremeSuccession.Hereditary => "世袭君主",
            (int)SupremeSuccession.Usurpation => "武力僭主",
            (int)SupremeSuccession.DirectAssembly => "公民大会直选",
            (int)SupremeSuccession.RepresentativeElection => "代议选举",
            (int)SupremeSuccession.CollegialElection => "委员会选举",
            (int)SupremeSuccession.NobleDesignation => "贵族推举",
            (int)SupremeSuccession.SuccessorDesignation => "储君指定",
            (int)SupremeSuccession.DivineMandate => "神命君主",
            (int)SupremeSuccession.Rotation => "轮座执政",
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
            (int)CentralSuccession.Appointed => "君主任命",
            (int)CentralSuccession.HereditaryOffice => "官位世袭",
            (int)CentralSuccession.Elected => "选举产生",
            (int)CentralSuccession.Examination => "考试选任",
            (int)CentralSuccession.MeritPromotion => "考课晋升",
            (int)CentralSuccession.Patronage => "恩庇推举",
            _ => "?"
        };

        public static string NameCentralInstitution(int c) => c switch
        {
            (int)CentralInstitution.None => "无常设",
            (int)CentralInstitution.Court => "王庭",
            (int)CentralInstitution.UnicameralAssembly => "一院议会",
            (int)CentralInstitution.BicameralAssembly => "两院议会",
            (int)CentralInstitution.EstateAssembly => "等级会议",
            (int)CentralInstitution.EldersCouncil => "长老议事会",
            (int)CentralInstitution.BureaucraticCore => "官僚中枢",
            (int)CentralInstitution.ReligiousCouncil => "宗教会议",
            (int)CentralInstitution.MilitaryCouncil => "军事委员会",
            _ => "?"
        };

        public static string NameLocalSuccession(int c) => c switch
        {
            (int)LocalSuccession.CentralAppointed => "中央任官",
            (int)LocalSuccession.HereditaryVassal => "世袭封臣",
            (int)LocalSuccession.FeudalAppanage => "宗室采邑",
            (int)LocalSuccession.LocalElected => "地方推举",
            (int)LocalSuccession.CityCharter => "城市特许自治",
            (int)LocalSuccession.ReligiousAppointed => "教区委任",
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
