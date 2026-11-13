namespace MyriadCreation.Core.Contracts
{
/// <summary>
/// UI 4c 法令命令契约（最小集）：UI 只依赖本接口，不直接调用 GameWorld.IssueDecreeOrders。
/// 现有玩家政权法令/政令的颁布/撤销已在 ILawCommand；这里只补 UI 实际用到的"下发命令"。
/// </summary>
public interface IDecreeCommand
{
	/// <summary>
	/// 向政权下级聚落下发法令命令；返回本次新增命令数（0 表示无下属官员/目标）。
	/// decreeId 为空或 realm 不存在时返回 0，不抛异常。
	/// </summary>
	int IssueDecreeOrders(string decreeId, int realmId);

	/// <summary>按当前世界日推进政令传递（宿主内部使用；无世界/无政令时安全空操作）。</summary>
	void TickDecreeOrders();
}
}
