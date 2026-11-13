using System;
using MyriadCreation.Simulation.Planning;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
public sealed class ConstructionPlanSystem
{
	private readonly GameWorld _world;

	private readonly PlanSystem _plans;

	private readonly BuildingSystem _buildings;

	private readonly ConstructionPlanExecutor _executor;

	public ConstructionPlanSystem(GameWorld world, PlanSystem plans, BuildingSystem buildings)
	{
		_world = world ?? throw new ArgumentNullException("world");
		_plans = plans ?? throw new ArgumentNullException("plans");
		_buildings = buildings ?? throw new ArgumentNullException("buildings");
		_executor = new ConstructionPlanExecutor(this);
		_plans.RegisterExecutor(_executor);
	}

	public Plan CreateConstructionPlan(int realmId, int initiatorId, int tileIndex, int buildingId, RealmData realm, int builderCharacterId = -1, int innovationId = -1)
	{
		if (realm == null || !_buildings.CanBuildBuilding(tileIndex, buildingId, realmId, realm))
		{
			return null;
		}
		BuildingDef? buildingDef = _buildings.GetBuildingDef(buildingId);
		if (!buildingDef.HasValue)
		{
			return null;
		}
		Plan plan = _plans.CreatePlan(PlanType.Construction, initiatorId, buildingId, _world.currentDay, "建造：" + buildingDef.Value.buildingName, "由统一计划系统调度的实际建筑工程。", buildingDef.Value.buildDays);
		plan.ownerId = realmId;
		plan.requirements.Add(new PlanRequirement("treasury", buildingDef.Value.buildCost));
		plan.requirements.Add(new PlanRequirement("construction_days", buildingDef.Value.buildDays));
		if (builderCharacterId >= 0)
		{
			plan.participants.Add(new PlanParticipant(builderCharacterId, "builder"));
		}
		if (!_buildings.StartPlannedBuilding(tileIndex, buildingId, realmId, realm, plan.planId, builderCharacterId, innovationId))
		{
			_plans.CancelPlan(plan.planId, "construction_start_failed");
			return null;
		}
		if (!_plans.AcceptPlan(plan.planId) || !_plans.BeginPreparation(plan.planId) || !_plans.BeginExecution(plan.planId))
		{
			_plans.CancelPlan(plan.planId, "construction_lifecycle_failed");
			return null;
		}
		return plan;
	}

	internal PlanExecutionResult Execute(Plan plan, float deltaDays)
	{
		float num = _buildings.AdvancePlannedConstruction(plan.planId, deltaDays);
		float progressDelta = num - plan.progress;
		if (num >= 1f)
		{
			return PlanExecutionResult.Complete("construction_completed", "施工");
		}
		return PlanExecutionResult.Continue(progressDelta, "施工");
	}
}
}
