using System;
using MyriadCreation.Simulation.Planning;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;

namespace MyriadCreation.Simulation.Systems
{
/// <summary>
/// 密谋计划（P3.5，S19 之后）：目标必须在发起政权的**地图知识**内，否则连立项都不成立。
/// 这是 D98“S19 落地后再开 WP5”的触发点；不建平行情报系统。
/// </summary>
public sealed class IntriguePlanSystem
{
	private readonly GameWorld _world;

	private readonly PlanSystem _plans;

	private readonly IntriguePlanExecutor _executor;

	public const float DefaultEstimatedDays = 30f;

	public IntriguePlanSystem(GameWorld world, PlanSystem plans)
	{
		_world = world ?? throw new ArgumentNullException("world");
		_plans = plans ?? throw new ArgumentNullException("plans");
		_executor = new IntriguePlanExecutor(this);
		_plans.RegisterExecutor(_executor);
	}

	/// <summary>发起密谋；目标地块不在知识层内时返回 null（信息不足不能凭空阴谋）。</summary>
	public Plan CreatePlan(int realmId, int initiatorCharacterId, int targetCharacterId)
	{
		if (_world?.Characters == null) return null;
		CharacterData target = _world.Characters.GetCharacter(targetCharacterId);
		if (target == null || !target.isAlive || target.characterId == initiatorCharacterId) return null;
		if (target.currentTile < 0 || !_world.Knowledge.IsKnown(realmId, target.currentTile)) return null;
		Plan plan = _plans.CreatePlan(PlanType.Intrigue, initiatorCharacterId, targetCharacterId, _world.currentDay,
			"密谋", "针对已知目标人物的秘密行动。", DefaultEstimatedDays, targetKind: "character");
		plan.ownerId = realmId;
		plan.participants.Add(new PlanParticipant(initiatorCharacterId, "plotter"));
		if (!_plans.AcceptPlan(plan.planId) || !_plans.BeginPreparation(plan.planId) || !_plans.BeginExecution(plan.planId))
		{
			_plans.CancelPlan(plan.planId, "intrigue_lifecycle_failed");
			return null;
		}
		return plan;
	}

	internal PlanExecutionResult Execute(Plan plan, float deltaDays)
	{
		if (plan == null) return PlanExecutionResult.Fail("missing_plan");
		CharacterData target = _world?.Characters?.GetCharacter(plan.targetId);
		if (target == null || !target.isAlive) return PlanExecutionResult.Fail("target_dead", "目标已死亡，密谋终止");
		int tileIndex = target.currentTile;
		if (tileIndex < 0 || !_world.Knowledge.IsKnown(plan.ownerId, tileIndex))
		{
			return PlanExecutionResult.Fail("target_lost", "目标离开已知区域，密谋失去线索");
		}
		float step = deltaDays / Math.Max(1f, plan.estimatedDays);
		if (plan.progress + step >= 1f)
		{
			if (HasGuardAt(tileIndex))
			{
				return PlanExecutionResult.Fail("guarded", "目标身边有军队戒备，密谋失败");
			}
			_world.GetChronicle()?.Add("intrigue_success", "一桩密谋在暗处收网", false, plan.ownerId);
			return PlanExecutionResult.Complete("intrigue_success", "密谋");
		}
		return PlanExecutionResult.Continue(step, "密谋");
	}

	private bool HasGuardAt(int tileIndex)
	{
		if (_world?.armies == null) return false;
		foreach (Army army in _world.armies.Values)
		{
			if (army != null && army.currentTileIndex == tileIndex) return true;
		}
		return false;
	}
}

internal sealed class IntriguePlanExecutor : IPlanExecutor
{
	private readonly IntriguePlanSystem _system;

	public PlanType Type => PlanType.Intrigue;

	public IntriguePlanExecutor(IntriguePlanSystem system)
	{
		_system = system;
	}

	public PlanExecutionResult Execute(Plan plan, float deltaDays)
	{
		return _system.Execute(plan, deltaDays);
	}
}
}
