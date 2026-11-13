namespace MyriadCreation.Content.Definitions
{
    /// <summary>继承法预设目录：预设 id → InheritanceLaw（封闭开发，不另立法律实体）。</summary>
    public static class InheritanceLawCatalog
    {
        public static InheritanceLaw Resolve(string presetId)
        {
            switch (presetId)
            {
                case "primogeniture": return InheritanceLaw.Primogeniture();
                case "ultimogeniture": return InheritanceLaw.Ultimogeniture();
                case "tanistry": return InheritanceLaw.Tanistry();
                case "matrilineal": return InheritanceLaw.Matrilineal();
                case "frankish_partible": return InheritanceLaw.FrankishPartible();
                case "salic": return InheritanceLaw.Salic();
                default: return InheritanceLaw.Primogeniture();
            }
        }
    }
}
