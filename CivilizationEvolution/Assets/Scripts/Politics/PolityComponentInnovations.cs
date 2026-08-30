using System.Collections.Generic;
using CivilizationEvolution.Tech;

namespace CivilizationEvolution.Politics
{
    /// <summary>
    /// 政体成分 ↔ 支撑革新（用户定稿：政体学的每个部分都有相应的革新）
    /// 每个政体成分（A1 交接/B1 选人/B2 机构/C1 地方产生/C2 职能/央地结构）
    /// 需要对应革新支撑——未持有则该成分不可用（或研究后解锁）
    /// 数据表式（成分 int → 革新 id 列表）；空表=基础可用
    /// </summary>
    public static class PolityComponentInnovations
    {
        // ===== A1 最高权力·交接 =====
        /// <summary>世袭：部落联盟（王权产生）</summary>
        private static readonly List<int> A1Hereditary = new List<int> { 500 };
        /// <summary>选举·直接：雅典议事（公民大会）</summary>
        private static readonly List<int> A1ElectiveDirect = new List<int> { 980 };
        /// <summary>选举·代议：等级会议/议会传统</summary>
        private static readonly List<int> A1ElectiveRepresentative = new List<int> { 603, 980 };
        /// <summary>僭夺：无需革新（武力）</summary>
        private static readonly List<int> A1Usurpation = new List<int> { };
        /// <summary>轮座：部落联盟（长老轮值）</summary>
        private static readonly List<int> A1Rotation = new List<int> { 500 };
        /// <summary>神命：一神教/神权体系</summary>
        private static readonly List<int> A1Divine = new List<int> { 601 };

        // ===== A2 最高权力·分配 =====
        /// <summary>全能：无需（基础君权）</summary>
        private static readonly List<int> A2Absolute = new List<int> { };
        /// <summary>法理受限：成文法</summary>
        private static readonly List<int> A2LegallyBound = new List<int> { 505 };
        /// <summary>惯例约束：成文法（习惯法传统）</summary>
        private static readonly List<int> A2CustomBound = new List<int> { 505 };
        /// <summary>共议制约：雅典议事/等级会议</summary>
        private static readonly List<int> A2Consensual = new List<int> { 980 };
        /// <summary>神意约束：一神教</summary>
        private static readonly List<int> A2DivinelyBound = new List<int> { 601 };

        // ===== B1 中央权力·交接（选人依据） =====
        /// <summary>上级决断：官僚制度</summary>
        private static readonly List<int> B1Appointed = new List<int> { 503 };
        /// <summary>集体选择：雅典议事/部落联盟</summary>
        private static readonly List<int> B1Elected = new List<int> { 980, 500 };
        /// <summary>客观标准：科举制度</summary>
        private static readonly List<int> B1Examination = new List<int> { 504 };
        /// <summary>血缘世袭：封建制度</summary>
        private static readonly List<int> B1Hereditary = new List<int> { 501 };

        // ===== B2 中央机构 =====
        /// <summary>无常设：部落联盟（部落议事）</summary>
        private static readonly List<int> B2None = new List<int> { 500 };
        /// <summary>王庭：无需（宫廷天然）</summary>
        private static readonly List<int> B2Court = new List<int> { };
        /// <summary>议会/元老院：雅典议事</summary>
        private static readonly List<int> B2Assembly = new List<int> { 980 };
        /// <summary>长老议事会：部落联盟</summary>
        private static readonly List<int> B2EldersCouncil = new List<int> { 500 };
        /// <summary>官僚中枢：官僚制度</summary>
        private static readonly List<int> B2BureaucraticCore = new List<int> { 503 };
        /// <summary>宗教会议：一神教</summary>
        private static readonly List<int> B2ReligiousCouncil = new List<int> { 601 };
        /// <summary>军事委员会：军事传统（重装骑兵/骑兵战术）</summary>
        private static readonly List<int> B2MilitaryCouncil = new List<int> { 302 };

        // ===== C1 地方权力·交接 =====
        /// <summary>任命：郡县制/行省制（中央任官）</summary>
        private static readonly List<int> C1Appointed = new List<int> { 959, 960 };
        /// <summary>选举推举：雅典议事/部落联盟</summary>
        private static readonly List<int> C1Elected = new List<int> { 980, 500 };
        /// <summary>世袭领有：封建制度/庄园</summary>
        private static readonly List<int> C1Hereditary = new List<int> { 501, 952 };
        /// <summary>城市特许：自由城市（城市特许——暂无独立革新，用铸币/贸易）</summary>
        private static readonly List<int> C1CityCharter = new List<int> { 701, 700 };

        // ===== C2 地方权力·分配 =====
        /// <summary>全权自治：部落联盟/邦联（自治传统）</summary>
        private static readonly List<int> C2FullAutonomy = new List<int> { 500 };
        /// <summary>征税司法：中央集权（中央控军权）</summary>
        private static readonly List<int> C2FiscalJudicial = new List<int> { 502 };
        /// <summary>仅军事驻防：军事传统</summary>
        private static readonly List<int> C2MilitaryOnly = new List<int> { 302 };
        /// <summary>完全直辖：中央集权+郡县</summary>
        private static readonly List<int> C2None = new List<int> { 502, 959 };

        // ===== D 央地结构 =====
        /// <summary>单一制：中央集权</summary>
        private static readonly List<int> DUnitary = new List<int> { 502 };
        /// <summary>联邦制：封建/契约传统</summary>
        private static readonly List<int> DFederal = new List<int> { 501 };
        /// <summary>邦联制：部落联盟</summary>
        private static readonly List<int> DConfederal = new List<int> { 500 };

        /// <summary>维度枚举（政体七维——跨枚举 int 值重叠，必须按维度分表）</summary>
        public enum PolityDimension
        {
            SupremeSuccession,  // A1 最高权力·交接
            SupremeScope,       // A2 最高权力·分配
            CentralSuccession,  // B1 中央权力·交接
            CentralInstitution, // B2 中央权力·分配
            LocalSuccession,    // C1 地方权力·交接
            LocalScope,         // C2 地方权力·分配
            SpatialStructure    // D 央地结构
        }

        private static readonly Dictionary<PolityDimension, Dictionary<int, List<int>>> Table =
            new Dictionary<PolityDimension, Dictionary<int, List<int>>>
        {
            [PolityDimension.SupremeSuccession] = new Dictionary<int, List<int>>
            {
                [(int)SupremeSuccession.Hereditary] = A1Hereditary,
                [(int)SupremeSuccession.ElectiveDirect] = A1ElectiveDirect,
                [(int)SupremeSuccession.ElectiveRepresentative] = A1ElectiveRepresentative,
                [(int)SupremeSuccession.Usurpation] = A1Usurpation,
                [(int)SupremeSuccession.Rotation] = A1Rotation,
                [(int)SupremeSuccession.Divine] = A1Divine
            },
            [PolityDimension.SupremeScope] = new Dictionary<int, List<int>>
            {
                [(int)SupremeScope.Absolute] = A2Absolute,
                [(int)SupremeScope.LegallyBound] = A2LegallyBound,
                [(int)SupremeScope.CustomBound] = A2CustomBound,
                [(int)SupremeScope.Consensual] = A2Consensual,
                [(int)SupremeScope.DivinelyBound] = A2DivinelyBound
            },
            [PolityDimension.CentralSuccession] = new Dictionary<int, List<int>>
            {
                [(int)CentralSuccession.Appointed] = B1Appointed,
                [(int)CentralSuccession.Elected] = B1Elected,
                [(int)CentralSuccession.Examination] = B1Examination,
                [(int)CentralSuccession.Hereditary] = B1Hereditary
            },
            [PolityDimension.CentralInstitution] = new Dictionary<int, List<int>>
            {
                [(int)CentralInstitution.None] = B2None,
                [(int)CentralInstitution.Court] = B2Court,
                [(int)CentralInstitution.Assembly] = B2Assembly,
                [(int)CentralInstitution.EldersCouncil] = B2EldersCouncil,
                [(int)CentralInstitution.BureaucraticCore] = B2BureaucraticCore,
                [(int)CentralInstitution.ReligiousCouncil] = B2ReligiousCouncil,
                [(int)CentralInstitution.MilitaryCouncil] = B2MilitaryCouncil
            },
            [PolityDimension.LocalSuccession] = new Dictionary<int, List<int>>
            {
                [(int)LocalSuccession.Appointed] = C1Appointed,
                [(int)LocalSuccession.Elected] = C1Elected,
                [(int)LocalSuccession.Hereditary] = C1Hereditary,
                [(int)LocalSuccession.CityCharter] = C1CityCharter
            },
            [PolityDimension.LocalScope] = new Dictionary<int, List<int>>
            {
                [(int)LocalScope.FullAutonomy] = C2FullAutonomy,
                [(int)LocalScope.FiscalJudicial] = C2FiscalJudicial,
                [(int)LocalScope.MilitaryOnly] = C2MilitaryOnly,
                [(int)LocalScope.None] = C2None
            },
            [PolityDimension.SpatialStructure] = new Dictionary<int, List<int>>
            {
                [(int)SpatialStructure.Unitary] = DUnitary,
                [(int)SpatialStructure.Federal] = DFederal,
                [(int)SpatialStructure.Confederal] = DConfederal
            }
        };

        /// <summary>成分所需支撑革新（按维度查；空=基础可用）</summary>
        public static List<int> GetRequiredInnovations(PolityDimension dimension, int component)
        {
            if (Table.TryGetValue(dimension, out var dimTable)
                && dimTable.TryGetValue(component, out var list))
                return list;
            return new List<int>();
        }

        /// <summary>
        /// 成分是否可用（按维度查；支撑革新**任一持有**即可——郡县或行省都能支撑
        /// "中央任命"；innovations 为 null 或空表=基础可用）
        /// </summary>
        public static bool IsComponentAvailable(PolityDimension dimension, int component,
            InnovationTree innovations, int realmId)
        {
            if (innovations == null) return true;
            var required = GetRequiredInnovations(dimension, component);
            if (required.Count == 0) return true;
            foreach (int id in required)
            {
                if (innovations.HasInnovation(realmId, id)) return true;
            }
            return false;
        }
    }
}
