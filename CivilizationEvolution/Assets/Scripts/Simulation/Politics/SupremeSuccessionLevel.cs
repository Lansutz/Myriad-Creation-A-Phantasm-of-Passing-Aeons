namespace CivilizationEvolution.Politics
{
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

 /// 完整推导：君主制=个人传承系 或（选举系且 A2=全能——当选者个人终身专权）
 /// 共和制=选举系且非全能（共议/受限——多人共治）
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
}
