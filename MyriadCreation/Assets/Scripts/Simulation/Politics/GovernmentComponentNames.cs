namespace CivilizationEvolution.Simulation.Politics
{
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
