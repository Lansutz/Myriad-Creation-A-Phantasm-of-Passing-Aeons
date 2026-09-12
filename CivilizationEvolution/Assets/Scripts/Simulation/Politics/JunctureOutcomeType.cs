namespace CivilizationEvolution.Simulation.Politics
{
    public enum JunctureOutcomeType
    {
        None,           // 未发生
        Reform,         // 改革：胜派推动一个或多个政体成分改变
        Compromise,     // 妥协：力量接近，仅改最容易的一维
        Reaction,       // 反扑/复辟：保守派胜出，甚至强化旧制
        Stalemate,      // 停滞：窗口关闭，什么都没变（超稳定结构）
        Collapse        // 崩溃：各方失控，稳定度暴跌、内战风险（交由战争/叛乱系统）
    }
}
