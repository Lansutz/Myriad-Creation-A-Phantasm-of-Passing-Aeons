using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.Planning;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;

namespace MyriadCreation.Simulation.Systems
{
/// <summary>
/// 探索计划（P4.1）：立项时算 BFS 路线并创建 Expedition；执行进度由远征 cursor 驱动。
/// 没有远征实体就不立项，避免“只有知识层空转”。
/// </summary>
public sealed class ExplorationPlanSystem
{
	private readonly GameWorld _world;
	private readonly PlanSystem _plans;
	private readonly ExplorationPlanExecutor _executor;

	public ExplorationPlanSystem(GameWorld world, PlanSystem plans)
	{
		_world = world ?? throw new ArgumentNullException("world");
		_plans = plans ?? throw new ArgumentNullException("plans");
		_executor = new ExplorationPlanExecutor(this);
		_plans.RegisterExecutor(_executor);
	}

	public Plan CreatePlan(int realmId, int leaderCharacterId, int targetTile)
	{
		if (_world?.Characters == null || _world.tiles == null) return null;
		CharacterData leader = _world.Characters.GetCharacter(leaderCharacterId);
		if (leader == null || !leader.isAlive) return null;
		if (targetTile < 0 || targetTile >= _world.tiles.Length || !_world.tiles[targetTile].isLand) return null;
		int start = leader.currentTile >= 0 ? leader.currentTile : FindOwnedTile(realmId);
		if (start < 0) return null;
		List<int> route = ExpeditionSystem.FindRoute(_world.tiles, _world.mapWidth, _world.mapHeight, start, targetTile);
		if (route == null || route.Count < 2) return null;
		var expedition = new Expedition
		{
			expeditionId = _world.NextExpeditionId(),
			ownerRealmId = realmId,
			leaderCharacterId = leaderCharacterId,
			homeTileIndex = start,
			currentTileIndex = start,
			route = route,
			startDay = _world.currentDay
		};
		_world.expeditions[expedition.expeditionId] = expedition;
		Plan plan = _plans.CreatePlan(PlanType.Exploration, leaderCharacterId, expedition.expeditionId, _world.currentDay,
			"远征", "由远征队实际执行的探索计划。", route.Count, targetKind: "expedition");
		plan.ownerId = realmId;
		plan.participants.Add(new PlanParticipant(leaderCharacterId, "explorer"));
		plan.requirements.Add(new PlanRequirement("supplies", 100f));
		if (!_plans.AcceptPlan(plan.planId) || !_plans.BeginPreparation(plan.planId) || !_plans.BeginExecution(plan.planId))
		{
			_plans.CancelPlan(plan.planId, "exploration_lifecycle_failed");
			return null;
		}
		return plan;
	}

	private int FindOwnedTile(int realmId)
	{
		for (int i = 0; i < _world.tiles.Length; i++)
		{
			if (_world.tiles[i].isLand && _world.tiles[i].ownerRealmId == realmId) return i;
		}
		return -1;
	}

	internal PlanExecutionResult Execute(Plan plan, float deltaDays)
	{
		if (plan == null) return PlanExecutionResult.Fail("missing_plan");
		if (_world.expeditions == null || !_world.expeditions.TryGetValue(plan.targetId, out Expedition expedition))
		{
			return PlanExecutionResult.Fail("expedition_missing");
		}
		if (expedition.state == ExpeditionState.Failed)
		{
			return PlanExecutionResult.Fail("expedition_" + expedition.failReason, "远征失败");
		}
		if (expedition.state == ExpeditionState.Completed)
		{
			return PlanExecutionResult.Complete("exploration_completed", "远征");
		}
		float target = expedition.route.Count <= 0 ? 0f : (float)expedition.routeCursor / expedition.route.Count;
		float delta = Math.Max(0f, target - plan.progress);
		return PlanExecutionResult.Continue(delta, "远征");
	}
}

internal sealed class ExplorationPlanExecutor : IPlanExecutor
{
	private readonly ExplorationPlanSystem _system;

	public PlanType Type => PlanType.Exploration;

	public ExplorationPlanExecutor(ExplorationPlanSystem system)
	{
		_system = system;
	}

	public PlanExecutionResult Execute(Plan plan, float deltaDays)
	{
		return _system.Execute(plan, deltaDays);
	}
}
}
