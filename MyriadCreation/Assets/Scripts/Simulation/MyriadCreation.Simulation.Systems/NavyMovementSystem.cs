using System.Collections.Generic;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;

namespace MyriadCreation.Simulation.Systems
{
/// <summary>P1.5 海军寻路/行军：舰队按海域寻路，每日推进一格；不写陆军路径。</summary>
public static class NavyMovementSystem
{
	public static bool SetMoveTarget(GameWorld world, Navy navy, int targetTile)
	{
		if (world?.tiles == null || navy == null || targetTile < 0 || targetTile >= world.tiles.Length)
		{
			return false;
		}
		if (navy.currentTileIndex < 0 || navy.currentTileIndex >= world.tiles.Length)
		{
			return false;
		}
		var pathfinder = new PathfindingSystem(world, world.mapWidth, world.mapHeight);
		var result = pathfinder.FindPath(navy.currentTileIndex, targetTile, new PathfindingSystem.PathfindingParams
		{
			isNavy = true,
			armyRealmId = navy.ownerRealmId,
			canPassEnemyTerritory = false
		});
		if (!result.success || result.path == null || result.path.Count == 0)
		{
			return false;
		}
		navy.movePath = new List<int>(result.path);
		if (navy.movePath.Count > 0 && navy.movePath[0] == navy.currentTileIndex)
		{
			navy.movePath.RemoveAt(0);
		}
		navy.moveTargetTile = targetTile;
		navy.state = GameEnums.CombatState.Marching;
		navy.moveProgress = 0f;
		return true;
	}

	/// <summary>每日推进一格；无路可走时回 Idle。</summary>
	public static void MoveTick(Navy navy)
	{
		if (navy == null || navy.state != GameEnums.CombatState.Marching || navy.movePath == null || navy.movePath.Count == 0)
		{
			if (navy != null && navy.state == GameEnums.CombatState.Marching)
			{
				navy.state = GameEnums.CombatState.Idle;
			}
			return;
		}
		navy.currentTileIndex = navy.movePath[0];
		navy.movePath.RemoveAt(0);
		if (navy.movePath.Count == 0)
		{
			navy.moveTargetTile = -1;
			navy.state = GameEnums.CombatState.Idle;
		}
	}
}
}
