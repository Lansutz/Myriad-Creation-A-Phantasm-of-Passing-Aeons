using MyriadCreation.Core.Contracts;

namespace MyriadCreation.Simulation.WorldState
{
public partial class GameWorld
{
	private IDecreeCommand _decreeCommand;

	/// <summary>
	/// UI 4c 注入入口：返回当前世界的法令命令适配器（懒创建）。
	/// 仅新增 API；不改变现有 GameWorld/IGameUiHost 签名。
	/// </summary>
	public IDecreeCommand DecreeCommand => _decreeCommand ??= new DecreeCommandAdapter(this);

	/// <summary>UI 4c 显式创建入口（测试/多实例消费者用；每次返回新适配器）。</summary>
	public IDecreeCommand CreateDecreeCommand()
	{
		return new DecreeCommandAdapter(this);
	}

	/// <summary>
	/// 供 UI 命令适配器转调的公有壳：推进现有私有 TickDecreeOrders；空世界安全。
	/// 私有实现仍由 GameWorld.Simulation.cs 的私有 TickDecreeOrders() 持有。
	/// </summary>
	public void TickDecreeOrdersNow()
	{
		if (realms == null || tiles == null || settlements == null)
		{
			return;
		}
		TickDecreeOrders();
	}
}
}
