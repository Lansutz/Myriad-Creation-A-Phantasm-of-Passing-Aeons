using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Role;

namespace CivilizationEvolution.Politics
{
    /// <summary>
    /// 政体系统（用户定稿：三个权力层级 × 交接/分配两问 = 六维结构）
    /// 最高权力/中央权力/地方权力——每一层都有"交接"（怎么产生/传承）与
    /// "分配"（掌握什么/管什么）两个问题
    /// 政体 = 六维各选一成分的自由组合；成分是定义（模组化接口），组合即政体
    /// "王国""帝国"等为外交头衔（纯称号，不产生机制影响）
    /// </summary>

    // ==================== A. 最高权力 ====================

    /// <summary>A1·最高权力·交接：最高权力者如何产生/传承（主体隐含于方式）</summary>
    public enum SupremeSuccession
    {
        Hereditary,     // 世袭：子选项=继承法（四轴人序+头衔模式+领地模式）——君主制
        Usurpation,     // 武力僭夺：兵强者上——僭主
        Election,       // 选举：公民/大会票选——民主
        Designation,    // 推举：贵族/长老共推——贵族制
        DivineMandate,  // 神命：祭司认定/神谕——神权
        Rotation        // 轮座：定期轮值——部落轮值
    }

    /// <summary>A2·最高权力·分配：最高权力掌握什么/受何约束</summary>
    public enum SupremeScope
    {
        Absolute,       // 全能：立法/司法/军事/财政全揽
        LegallyBound,   // 法理受限：受成文法/惯例约束
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
        Patronage           // 恩庇推举：门阀/荐举（九品中正）
    }

    /// <summary>B2·中央权力·分配：有无中央机构/形态与职能</summary>
    public enum CentralInstitution
    {
        None,               // 无常设：部落临时集会
        Court,              // 王庭：王室+近臣（宫廷决策）
        Assembly,           // 议会/元老院：公民或代表制（共和机构）
        EldersCouncil,      // 长老议事会：长老资格制（部落/贵族传统——非共和）
        BureaucraticCore,   // 官僚中枢：宰相府/尚书台（文书行政）
        ReligiousCouncil,   // 宗教会议：教廷/长老会（教阶制）
        MilitaryCouncil     // 军事委员会：将领共议
    }

    // ==================== C. 地方权力 ====================

    /// <summary>C1·地方权力·交接：地方权力者如何产生</summary>
    public enum LocalSuccession
    {
        CentralAppointed,   // 中央任命：流官制（郡县/行省）
        HereditaryLord,     // 世袭领有：封建领主/采邑（封臣世袭）
        LocalElected,       // 地方推举：城邦/部族自治选举
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
    /// 政体成分组合（六维：三个权力层级 × 交接/分配）
    /// 政体 = 六维各选一成分的自由组合；成分是模组化接口（可新增）
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

        /// <summary>A1=世袭时的子选项=继承法（四轴人序+头衔模式+领地模式）</summary>
        public InheritanceLaw successionLaw = InheritanceLaw.Primogeniture();

        public GovernmentComposition() { }

        // ===== 经典政体组合（用户/学术示例） =====

        /// <summary>秦式官僚君主国：世袭+全能+任命+官僚中枢+中央任命+完全直辖</summary>
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
                successionLaw = InheritanceLaw.ChinesePartible() // 宗祧+析产
            };
        }

        /// <summary>西周分封：世袭+全能+官位世袭+王庭+世袭领主+全权自治（封国）</summary>
        public static GovernmentComposition FeudalFiefdom()
        {
            return new GovernmentComposition
            {
                supremeSuccession = new ComponentChoice((int)SupremeSuccession.Hereditary),
                supremeScope = new ComponentChoice((int)SupremeScope.Absolute),
                centralSuccession = new ComponentChoice((int)CentralSuccession.HereditaryOffice),
                centralInstitution = new ComponentChoice((int)CentralInstitution.Court),
                localSuccession = new ComponentChoice((int)LocalSuccession.HereditaryLord),
                localScope = new ComponentChoice((int)LocalScope.FullAutonomy),
                successionLaw = InheritanceLaw.Primogeniture()
            };
        }

        /// <summary>雅典民主：选举+共议+选举+公民议会+地方选举+全权自治</summary>
        public static GovernmentComposition AthenianDemocracy()
        {
            return new GovernmentComposition
            {
                supremeSuccession = new ComponentChoice((int)SupremeSuccession.Election),
                supremeScope = new ComponentChoice((int)SupremeScope.Consensual),
                centralSuccession = new ComponentChoice((int)CentralSuccession.Elected),
                centralInstitution = new ComponentChoice((int)CentralInstitution.Assembly),
                localSuccession = new ComponentChoice((int)LocalSuccession.LocalElected),
                localScope = new ComponentChoice((int)LocalScope.FullAutonomy)
            };
        }

        /// <summary>罗马共和：推举+法理受限+恩庇+元老院+中央任命+征税司法</summary>
        public static GovernmentComposition SenatorialRepublic()
        {
            return new GovernmentComposition
            {
                supremeSuccession = new ComponentChoice((int)SupremeSuccession.Designation),
                supremeScope = new ComponentChoice((int)SupremeScope.LegallyBound),
                centralSuccession = new ComponentChoice((int)CentralSuccession.Patronage),
                centralInstitution = new ComponentChoice((int)CentralInstitution.Assembly),
                localSuccession = new ComponentChoice((int)LocalSuccession.CentralAppointed),
                localScope = new ComponentChoice((int)LocalScope.FiscalJudicial)
            };
        }

        /// <summary>神权（教廷）：神命+神意约束+任命+宗教会议+教区+全权</summary>
        public static GovernmentComposition Theocracy()
        {
            return new GovernmentComposition
            {
                supremeSuccession = new ComponentChoice((int)SupremeSuccession.DivineMandate),
                supremeScope = new ComponentChoice((int)SupremeScope.DivinelyBound),
                centralSuccession = new ComponentChoice((int)CentralSuccession.Appointed),
                centralInstitution = new ComponentChoice((int)CentralInstitution.ReligiousCouncil),
                localSuccession = new ComponentChoice((int)LocalSuccession.ReligiousAppointed),
                localScope = new ComponentChoice((int)LocalScope.FullAutonomy)
            };
        }

        /// <summary>蒙古汗国：轮座/世袭[兄终弟及]+全能+军功+军事委员会+分封+全权</summary>
        public static GovernmentComposition MongolHorde()
        {
            return new GovernmentComposition
            {
                supremeSuccession = new ComponentChoice((int)SupremeSuccession.Hereditary),
                supremeScope = new ComponentChoice((int)SupremeScope.Absolute),
                centralSuccession = new ComponentChoice((int)CentralSuccession.Appointed),
                centralInstitution = new ComponentChoice((int)CentralInstitution.MilitaryCouncil),
                localSuccession = new ComponentChoice((int)LocalSuccession.HereditaryLord),
                localScope = new ComponentChoice((int)LocalScope.FullAutonomy),
                successionLaw = InheritanceLaw.Tanistry() // 兄终弟及
            };
        }

        // ===== 正交组合示例（最高权力主体 ≠ 中央机构存在——两者独立组合） =====

        /// <summary>
        /// 君主立宪（英式）：最高权力在议会（parliamentary sovereignty），
        /// 君主仅为虚位元首（外交头衔/纯称号，不占成分）——A1 为选举/议会而非世袭
        /// </summary>
        public static GovernmentComposition ConstitutionalMonarchy()
        {
            return new GovernmentComposition
            {
                supremeSuccession = new ComponentChoice((int)SupremeSuccession.Election),
                supremeScope = new ComponentChoice((int)SupremeScope.Consensual),
                centralSuccession = new ComponentChoice((int)CentralSuccession.Elected),
                centralInstitution = new ComponentChoice((int)CentralInstitution.Assembly),
                localSuccession = new ComponentChoice((int)LocalSuccession.CentralAppointed),
                localScope = new ComponentChoice((int)LocalScope.FiscalJudicial)
                // 世袭君主不在六维成分中——头衔（国王）为外交称号，不产生机制影响
            };
        }

        /// <summary>
        /// 罗马帝国（皇帝+元老院）：世袭/僭夺+全能+恩庇+元老院+中央任命+征税司法
        /// 最高权力=皇帝一人，中央机构=元老院——与共和元老院层级对等
        /// </summary>
        public static GovernmentComposition ImperialSenate()
        {
            return new GovernmentComposition
            {
                supremeSuccession = new ComponentChoice((int)SupremeSuccession.Hereditary, (int)SupremeSuccession.Usurpation),
                supremeScope = new ComponentChoice((int)SupremeScope.Absolute),
                centralSuccession = new ComponentChoice((int)CentralSuccession.Patronage),
                centralInstitution = new ComponentChoice((int)CentralInstitution.Assembly),
                localSuccession = new ComponentChoice((int)LocalSuccession.CentralAppointed),
                localScope = new ComponentChoice((int)LocalScope.FiscalJudicial)
            };
        }

        /// <summary>
        /// 神圣罗马帝国（选帝侯+帝国议会）：选举[选帝侯]+共议+恩庇+议会+世袭领主+全权自治
        /// 最高权力=选举产生，中央机构=帝国议会——共和式主体与议会并存
        /// </summary>
        public static GovernmentComposition HolyRomanEmpire()
        {
            return new GovernmentComposition
            {
                supremeSuccession = new ComponentChoice((int)SupremeSuccession.Designation),
                supremeScope = new ComponentChoice((int)SupremeScope.Consensual),
                centralSuccession = new ComponentChoice((int)CentralSuccession.Patronage),
                centralInstitution = new ComponentChoice((int)CentralInstitution.Assembly),
                localSuccession = new ComponentChoice((int)LocalSuccession.HereditaryLord),
                localScope = new ComponentChoice((int)LocalScope.FullAutonomy)
            };
        }

        /// <summary>政体名称（中文：最高·中央·地方 三层概要）</summary>
        public string GetName()
        {
            return $"{GovernmentComponentNames.NameSupremeSuccession(supremeSuccession.primary)}·" +
                   $"{GovernmentComponentNames.NameCentralInstitution(centralInstitution.primary)}·" +
                   $"{GovernmentComponentNames.NameLocalSuccession(localSuccession.primary)}";
        }
    }

    /// <summary>成分中文名（按维度——枚举 int 值跨维度重叠，必须分维度查询）</summary>
    public static class GovernmentComponentNames
    {
        public static string NameSupremeSuccession(int c) => c switch
        {
            (int)SupremeSuccession.Hereditary => "世袭君主",
            (int)SupremeSuccession.Usurpation => "武力僭主",
            (int)SupremeSuccession.Election => "选举元首",
            (int)SupremeSuccession.Designation => "推举共主",
            (int)SupremeSuccession.DivineMandate => "神命君主",
            (int)SupremeSuccession.Rotation => "轮座执政",
            _ => "?"
        };

        public static string NameSupremeScope(int c) => c switch
        {
            (int)SupremeScope.Absolute => "全能",
            (int)SupremeScope.LegallyBound => "法理受限",
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
            (int)CentralSuccession.Patronage => "恩庇推举",
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
            (int)LocalSuccession.CentralAppointed => "中央任官",
            (int)LocalSuccession.HereditaryLord => "世袭领主",
            (int)LocalSuccession.LocalElected => "地方推举",
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
    }
}
