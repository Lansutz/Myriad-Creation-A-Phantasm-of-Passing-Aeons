namespace MyriadCreation.Simulation.ActionGate
{
    /// <summary>行动 ID 常量：避免裸字符串拼错导致 provider 静默不匹配。</summary>
    public static class ActionIds
    {
        public const string DeclareWar = "declare_war";
        public const string EnactLaw = "enact_law";
        public const string EnactDecree = "enact_decree";
        public const string AppointTitle = "appoint_title";
    }
}
