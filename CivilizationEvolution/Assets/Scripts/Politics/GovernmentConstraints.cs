using System;
using System.Collections.Generic;
using System.Linq;

namespace CivilizationEvolution.Politics
{
    /// <summary>
    /// 政体约束规则系统
    /// 管理政体七维组合的约束：次要交接限制、互斥性、组合模板、动态可选选项
    /// </summary>
    public static class GovernmentConstraints
    {
        // ===== 1. 次要成分限制 =====

        /// <summary>
        /// 次要成分只适用于最高权力交接（A1）
        /// 其他维度只有主导成分，没有次要成分
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
            CentralInstitution,   // B2 中央权力·分配
            LocalSuccession,      // C1 地方权力·交接
            LocalScope,           // C2 地方权力·分配
            SpatialStructure      // D 央地结构
        }

        // ===== 2. 政体类型推导 =====

        /// <summary>政体大类（由A1×A2推导）</summary>
        public enum GovernmentCategory
        {
            Monarchy,          // 君主制（个人传承系 或 选举系+全能）
            Republic,          // 共和制（选举系+非全能）
            Theocracy,         // 神权制（神命+神意约束）
            MilitaryJunta,     // 军事独裁（僭夺+军事委员会）
            Mixed              // 混合政体
        }

        /// <summary>推导政体大类</summary>
        public static GovernmentCategory DeriveCategory(GovernmentComposition comp)
        {
            var succession = (SupremeSuccession)comp.supremeSuccession.primary;
            var scope = (SupremeScope)comp.supremeScope.primary;

            // 神权制
            if (succession == SupremeSuccession.Divine && scope == SupremeScope.DivinelyBound)
                return GovernmentCategory.Theocracy;

            // 军事独裁
            if (succession == SupremeSuccession.Usurpation &&
                comp.centralInstitution.primary == (int)CentralInstitution.MilitaryCouncil)
                return GovernmentCategory.MilitaryJunta;

            // 君主制
            if (SupremeSuccessionLevel.IsMonarchy(succession, scope))
                return GovernmentCategory.Monarchy;

            // 共和制
            if (SupremeSuccessionLevel.IsRepublic(succession, scope))
                return GovernmentCategory.Republic;

            return GovernmentCategory.Mixed;
        }

        // ===== 3. 互斥性规则 =====

        /// <summary>检查两个成分是否互斥</summary>
        public static bool AreMutuallyExclusive(
            GovernmentDimension dim, int componentA, int componentB)
        {
            // 最高权力交接互斥
            if (dim == GovernmentDimension.SupremeSuccession)
            {
                var a = (SupremeSuccession)componentA;
                var b = (SupremeSuccession)componentB;
                // 世袭与选举互斥
                if ((a == SupremeSuccession.Hereditary && IsElective(b)) ||
                    (b == SupremeSuccession.Hereditary && IsElective(a)))
                    return true;
                // 僭夺与神命互斥
                if ((a == SupremeSuccession.Usurpation && b == SupremeSuccession.Divine) ||
                    (b == SupremeSuccession.Usurpation && a == SupremeSuccession.Divine))
                    return true;
            }

            // 最高权力分配互斥
            if (dim == GovernmentDimension.SupremeScope)
            {
                var a = (SupremeScope)componentA;
                var b = (SupremeScope)componentB;
                // 全能与任何受限形式互斥
                if (a == SupremeScope.Absolute && b != SupremeScope.Absolute)
                    return true;
                if (b == SupremeScope.Absolute && a != SupremeScope.Absolute)
                    return true;
            }

            // 央地结构互斥
            if (dim == GovernmentDimension.SpatialStructure)
            {
                // 单一制与邦联制互斥
                var a = (SpatialStructure)componentA;
                var b = (SpatialStructure)componentB;
                if ((a == SpatialStructure.Unitary && b == SpatialStructure.Confederal) ||
                    (b == SpatialStructure.Unitary && a == SpatialStructure.Confederal))
                    return true;
            }

            return false;
        }

        private static bool IsElective(SupremeSuccession s)
        {
            return s == SupremeSuccession.ElectiveDirect ||
                   s == SupremeSuccession.ElectiveRepresentative ||
                   s == SupremeSuccession.Rotation;
        }

        // ===== 4. 组合模板（政体大类推荐组合） =====

        /// <summary>政体组合模板</summary>
        public class GovernmentTemplate
        {
            public string name;
            public string description;
            public GovernmentCategory category;
            public Dictionary<GovernmentDimension, int> recommendedPrimary = new Dictionary<GovernmentDimension, int>();
            public List<GovernmentDimension> freeDimensions = new List<GovernmentDimension>(); // 自由选择的维度
        }

        /// <summary>获取所有预设政体模板</summary>
        public static List<GovernmentTemplate> GetTemplates()
        {
            var templates = new List<GovernmentTemplate>();

            // 君主制模板
            templates.Add(new GovernmentTemplate
            {
                name = "官僚君主国",
                description = "世袭君主+全能+中央任命+官僚中枢+中央集权",
                category = GovernmentCategory.Monarchy,
                recommendedPrimary = new Dictionary<GovernmentDimension, int>
                {
                    { GovernmentDimension.SupremeSuccession, (int)SupremeSuccession.Hereditary },
                    { GovernmentDimension.SupremeScope, (int)SupremeScope.Absolute },
                    { GovernmentDimension.CentralSuccession, (int)CentralSuccession.Appointed },
                    { GovernmentDimension.CentralInstitution, (int)CentralInstitution.BureaucraticCore },
                    { GovernmentDimension.LocalSuccession, (int)LocalSuccession.Appointed },
                    { GovernmentDimension.LocalScope, (int)LocalScope.None },
                    { GovernmentDimension.SpatialStructure, (int)SpatialStructure.Unitary }
                }
            });

            templates.Add(new GovernmentTemplate
            {
                name = "封建君主国",
                description = "世袭君主+惯例约束+官位世袭+王庭+世袭领主+全权自治+邦联",
                category = GovernmentCategory.Monarchy,
                recommendedPrimary = new Dictionary<GovernmentDimension, int>
                {
                    { GovernmentDimension.SupremeSuccession, (int)SupremeSuccession.Hereditary },
                    { GovernmentDimension.SupremeScope, (int)SupremeScope.CustomBound },
                    { GovernmentDimension.CentralSuccession, (int)CentralSuccession.Hereditary },
                    { GovernmentDimension.CentralInstitution, (int)CentralInstitution.Court },
                    { GovernmentDimension.LocalSuccession, (int)LocalSuccession.Hereditary },
                    { GovernmentDimension.LocalScope, (int)LocalScope.FullAutonomy },
                    { GovernmentDimension.SpatialStructure, (int)SpatialStructure.Confederal }
                }
            });

            // 共和制模板
            templates.Add(new GovernmentTemplate
            {
                name = "古典民主共和",
                description = "公民大会直接选举+共议制约+选举+一院议会+地方选举+全权自治+单一制",
                category = GovernmentCategory.Republic,
                recommendedPrimary = new Dictionary<GovernmentDimension, int>
                {
                    { GovernmentDimension.SupremeSuccession, (int)SupremeSuccession.ElectiveDirect },
                    { GovernmentDimension.SupremeScope, (int)SupremeScope.Consensual },
                    { GovernmentDimension.CentralSuccession, (int)CentralSuccession.Elected },
                    { GovernmentDimension.CentralInstitution, (int)CentralInstitution.Assembly },
                    { GovernmentDimension.LocalSuccession, (int)LocalSuccession.Elected },
                    { GovernmentDimension.LocalScope, (int)LocalScope.FullAutonomy },
                    { GovernmentDimension.SpatialStructure, (int)SpatialStructure.Unitary }
                }
            });

            templates.Add(new GovernmentTemplate
            {
                name = "贵族共和",
                description = "代议选举+法理受限+恩庇推举+元老院+中央任命+征税司法+联邦制",
                category = GovernmentCategory.Republic,
                recommendedPrimary = new Dictionary<GovernmentDimension, int>
                {
                    { GovernmentDimension.SupremeSuccession, (int)SupremeSuccession.ElectiveRepresentative },
                    { GovernmentDimension.SupremeScope, (int)SupremeScope.LegallyBound },
                    { GovernmentDimension.CentralSuccession, (int)CentralSuccession.Elected },
                    { GovernmentDimension.CentralInstitution, (int)CentralInstitution.Assembly },
                    { GovernmentDimension.LocalSuccession, (int)LocalSuccession.Appointed },
                    { GovernmentDimension.LocalScope, (int)LocalScope.FiscalJudicial },
                    { GovernmentDimension.SpatialStructure, (int)SpatialStructure.Federal }
                }
            });

            // 神权制模板
            templates.Add(new GovernmentTemplate
            {
                name = "神权政体",
                description = "神命+神意约束+教阶任命+宗教会议+教区委任+征税司法+单一制",
                category = GovernmentCategory.Theocracy,
                recommendedPrimary = new Dictionary<GovernmentDimension, int>
                {
                    { GovernmentDimension.SupremeSuccession, (int)SupremeSuccession.Divine },
                    { GovernmentDimension.SupremeScope, (int)SupremeScope.DivinelyBound },
                    { GovernmentDimension.CentralSuccession, (int)CentralSuccession.Appointed },
                    { GovernmentDimension.CentralInstitution, (int)CentralInstitution.ReligiousCouncil },
                    { GovernmentDimension.LocalSuccession, (int)LocalSuccession.Appointed },
                    { GovernmentDimension.LocalScope, (int)LocalScope.FiscalJudicial },
                    { GovernmentDimension.SpatialStructure, (int)SpatialStructure.Unitary }
                }
            });

            // 军事独裁模板
            templates.Add(new GovernmentTemplate
            {
                name = "军事独裁",
                description = "僭夺+全能+军事任命+军事委员会+军管+仅军事+单一制",
                category = GovernmentCategory.MilitaryJunta,
                recommendedPrimary = new Dictionary<GovernmentDimension, int>
                {
                    { GovernmentDimension.SupremeSuccession, (int)SupremeSuccession.Usurpation },
                    { GovernmentDimension.SupremeScope, (int)SupremeScope.Absolute },
                    { GovernmentDimension.CentralSuccession, (int)CentralSuccession.Appointed },
                    { GovernmentDimension.CentralInstitution, (int)CentralInstitution.MilitaryCouncil },
                    { GovernmentDimension.LocalSuccession, (int)LocalSuccession.Appointed },
                    { GovernmentDimension.LocalScope, (int)LocalScope.MilitaryOnly },
                    { GovernmentDimension.SpatialStructure, (int)SpatialStructure.Unitary }
                }
            });

            return templates;
        }

        // ===== 5. 动态可选选项计算 =====

        /// <summary>获取某维度的可选选项（基于当前政体组合过滤互斥项）</summary>
        public static List<int> GetAvailableOptions(
            GovernmentDimension dimension, GovernmentComposition currentComp)
        {
            var allOptions = GetAllOptions(dimension);
            var available = new List<int>();
            var currentPrimary = GetCurrentPrimary(dimension, currentComp);

            foreach (var option in allOptions)
            {
                // 检查与当前主导成分是否互斥
                if (currentPrimary >= 0 && AreMutuallyExclusive(dimension, currentPrimary, option))
                    continue;

                // 检查与次要成分是否互斥
                bool conflictWithSecondary = false;
                foreach (var secondary in GetCurrentSecondary(dimension, currentComp))
                {
                    if (AreMutuallyExclusive(dimension, secondary, option))
                    {
                        conflictWithSecondary = true;
                        break;
                    }
                }
                if (conflictWithSecondary) continue;

                // 政体大类约束
                if (!IsCompatibleWithCategory(dimension, option, currentComp))
                    continue;

                available.Add(option);
            }

            return available;
        }

        /// <summary>检查选项是否与当前政体大类兼容</summary>
        private static bool IsCompatibleWithCategory(
            GovernmentDimension dimension, int option, GovernmentComposition comp)
        {
            var category = DeriveCategory(comp);

            // 共和制约束：最高权力分配不能是全能
            if (category == GovernmentCategory.Republic &&
                dimension == GovernmentDimension.SupremeScope &&
                (SupremeScope)option == SupremeScope.Absolute)
                return false;

            // 君主制约束：最高权力交接不能是轮座（太分散）
            if (category == GovernmentCategory.Monarchy &&
                dimension == GovernmentDimension.SupremeSuccession &&
                (SupremeSuccession)option == SupremeSuccession.Rotation)
                return false;

            // 神权制约束：中央机构不能是军事委员会
            if (category == GovernmentCategory.Theocracy &&
                dimension == GovernmentDimension.CentralInstitution &&
                (CentralInstitution)option == CentralInstitution.MilitaryCouncil)
                return false;

            return true;
        }

        // ===== 辅助方法 =====

        private static List<int> GetAllOptions(GovernmentDimension dimension)
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

        private static List<int> GetCurrentSecondary(GovernmentDimension dimension, GovernmentComposition comp)
        {
            switch (dimension)
            {
                case GovernmentDimension.SupremeSuccession: return comp.supremeSuccession.secondary;
                case GovernmentDimension.SupremeScope: return comp.supremeScope.secondary;
                case GovernmentDimension.CentralSuccession: return comp.centralSuccession.secondary;
                case GovernmentDimension.CentralInstitution: return comp.centralInstitution.secondary;
                case GovernmentDimension.LocalSuccession: return comp.localSuccession.secondary;
                case GovernmentDimension.LocalScope: return comp.localScope.secondary;
                case GovernmentDimension.SpatialStructure: return comp.spatialStructure.secondary;
                default: return new List<int>();
            }
        }

        // ===== 维度名称 =====

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
    }
}
