namespace MyriadCreation.Simulation.Laws
{
    /// <summary>
    /// 法律生命周期：未知→已知→提案→推行→生效→固化→根本化→废除。
    /// 根本化不是自动升级，需要革新 + 程序 + 合法性。
    /// </summary>
    public enum LegalNormStage
    {
        Unknown = 0,
        Known = 1,
        Proposed = 2,
        Pending = 3,
        Active = 4,
        Consolidated = 5,
        Fundamentalized = 6,
        Repealed = 7,
    }
}
