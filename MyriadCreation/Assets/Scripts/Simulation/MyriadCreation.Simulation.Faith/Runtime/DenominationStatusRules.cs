namespace MyriadCreation.Simulation.Faith
{
/// <summary>
/// 宗派状态判定（P2.2）：声明性的正统越界优先于分歧度——越过明示边界即异端；
/// 未越界但分歧度达到容忍阈值则为「分裂容忍」；否则主流。
/// 阈值是内容参数前的占位（TODO_M05），不写成跨宗教永久常量。
/// </summary>
public static class DenominationStatusRules
{
	/// <summary>分歧度达到该值即不再视为主流（TODO_M05）。</summary>
	public const float ToleratedDivergence = 30f;

	public static DenominationStatus Evaluate(float divergence, int violations)
	{
		if (violations > 0)
		{
			return DenominationStatus.Heterodox;
		}
		return divergence >= ToleratedDivergence ? DenominationStatus.Tolerated : DenominationStatus.Mainstream;
	}
}
}
