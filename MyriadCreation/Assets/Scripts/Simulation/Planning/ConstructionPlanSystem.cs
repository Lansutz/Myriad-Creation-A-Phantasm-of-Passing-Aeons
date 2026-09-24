using System;
using MyriadCreation.Core.Dto;
using MyriadCreation.Simulation.Society;
using MyriadCreation.Simulation.WorldState;

namespace MyriadCreation.Simulation.Planning
{
    /// <summary>
    /// 建造领域计划层：把建筑工程纳入统一 PlanSystem。
    /// PlanSystem 只负责生命周期，本系统负责施工对象与实际世界结果。
    /// </summary>
    public sealed class ConstructionPlanSystem
    {
        private readonly GameWorld _world;
        private readonly PlanSystem _plans;
        private readonly BuildingSystem _buildings;
        private readonly ConstructionPlanExecutor _executor;

        public ConstructionPlanSystem(GameWorld world, PlanSystem plans, BuildingSystem buildings)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _plans = plans ?? throw new ArgumentNullException(nameof(plans));
            _buildings = buildings ?? throw new ArgumentNullException(nameof(buildings));
            _executor = new ConstructionPlanExecutor(this);
            _plans.RegisterExecutor(_executor);
        }

        public Plan CreateConstructionPlan(
            int realmId,
            int initiatorId,
            int tileIndex,
            int buildingId,
            RealmData realm,
            int builderCharacterId = -1,
            int innovationId = -1)
        {
            if (realm == null || !_buildings.CanBuildBuilding(tileIndex, buildingId, realmId, realm))
                return null;

            var def = _buildings.GetBuildingDef(buildingId);
            if (!def.HasValue) return null;

            var plan = _plans.CreatePlan(
                PlanType.Construction,
                initiatorId,
                buildingId,
                _world.currentDay,
                "建造：" + def.Value.buildingName,
                "由统一计划系统调度的实际建筑工程。",
                def.Value.buildDays);

            plan.ownerId = realmId;
            plan.requirements.Add(new PlanRequirement("treasury", def.Value.buildCost));
            plan.requirements.Add(new PlanRequirement("construction_days", def.Value.buildDays));

            if (builderCharacterId >= 0)
                plan.participants.Add(new PlanParticipant(builderCharacterId, "builder"));

            if (!_buildings.StartPlannedBuilding(
                    tileIndex, buildingId, realmId, realm,
                    plan.planId, builderCharacterId, innovationId))
            {
                _plans.CancelPlan(plan.planId, "construction_start_failed");
                return null;
            }

            if (!_plans.AcceptPlan(plan.planId)
                || !_plans.BeginPreparation(plan.planId)
                || !_plans.BeginExecution(plan.planId))
            {
                _plans.CancelPlan(plan.planId, "construction_lifecycle_failed");
                return null;
            }

            return plan;
        }

        internal PlanExecutionResult Execute(Plan plan, float deltaDays)
        {
            float nextProgress = _buildings.AdvancePlannedConstruction(plan.planId, deltaDays);
            float delta = nextProgress - plan.progress;
            if (nextProgress >= 1f)
                return PlanExecutionResult.Complete("construction_completed", "施工");

            return PlanExecutionResult.Continue(delta, "施工");
        }

    }

    internal sealed class ConstructionPlanExecutor : IPlanExecutor
    {
        private readonly ConstructionPlanSystem _system;

        public ConstructionPlanExecutor(ConstructionPlanSystem system)
        {
            _system = system;
        }

        public PlanType Type => PlanType.Construction;

        public float Execute(Plan plan, float deltaDays)
            => _system.Execute(plan, deltaDays);

    }
}
