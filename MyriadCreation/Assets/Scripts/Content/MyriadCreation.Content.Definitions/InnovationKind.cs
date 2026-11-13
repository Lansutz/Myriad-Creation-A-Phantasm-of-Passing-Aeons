namespace MyriadCreation.Content.Definitions
{
    /// <summary>革新类型（与 InnovationField 的领域分类正交）。</summary>
    public enum InnovationKind
    {
        Unknown = 0,
        Idea = 1,
        Technique = 2,
        Institution = 3,
        Practice = 4,
        Knowledge = 5,
        /// <summary>型制：具体的武器/器械/船只/车辆构型，不是泛器物名。</summary>
        Pattern = 6,
    }
}
