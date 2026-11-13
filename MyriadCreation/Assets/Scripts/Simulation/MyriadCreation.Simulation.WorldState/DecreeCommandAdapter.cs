using MyriadCreation.Core.Contracts;

namespace MyriadCreation.Simulation.WorldState
{
/// <summary>
/// UI 4c：IDecreeCommand 的 Simulation host 适配器。
/// 只转调 GameWorld 现有 IssueDecreeOrders/私有 TickDecreeOrders（经新增 public wrapper），不复制逻辑。
/// </summary>
public sealed class DecreeCommandAdapter : IDecreeCommand
{
	private readonly GameWorld _world;

	public DecreeCommandAdapter(GameWorld world)
	{
		_world = world;
	}

	public int IssueDecreeOrders(string decreeId, int realmId)
	{
		if (_world == null || _world.realms == null || _world.tiles == null || _world.settlements == null
			|| string.IsNullOrEmpty(decreeId))
		{
			return 0;
		}
		var orders = _world.IssueDecreeOrders(decreeId, realmId);
		return (orders != null) ? orders.Count : 0;
	}

	public void TickDecreeOrders()
	{
		_world?.TickDecreeOrdersNow();
	}
}
}
