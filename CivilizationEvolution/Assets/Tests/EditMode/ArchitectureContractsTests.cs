using NUnit.Framework;
using CivilizationEvolution.Core.Events;
using CivilizationEvolution.Simulation.Planning;

namespace CivilizationEvolution.Tests.EditMode
{
    public class ArchitectureContractsTests
    {
        [Test]
        public void SimulationEventBus_DeliversAndUnsubscribes()
        {
            var bus = new SimulationEventBus();
            int received = 0;
            System.Action<PracticeRecordedEvent> handler = evt => received += (int)evt.amount;

            bus.Subscribe(handler);
            bus.Publish(new PracticeRecordedEvent(1, 2, 3f));
            Assert.AreEqual(3, received);

            bus.Unsubscribe(handler);
            bus.Publish(new PracticeRecordedEvent(1, 2, 4f));
            Assert.AreEqual(3, received);
        }

        [Test]
        public void PlanSystem_UsesExecutionResultAndCurrentActivity()
        {
            var plans = new PlanSystem();
            plans.RegisterExecutor(new TestExecutor());

            var plan = plans.CreatePlan(PlanType.Custom, 1, 2, 10);
            Assert.IsTrue(plans.AcceptPlan(plan.planId));
            Assert.IsTrue(plans.BeginPreparation(plan.planId));
            Assert.IsTrue(plans.BeginExecution(plan.planId));

            plans.DailyTick(10, 1f);

            Assert.AreEqual(PlanState.Completed, plan.state);
            Assert.AreEqual(1f, plan.progress);
            Assert.AreEqual("执行活动", plan.currentActivity);
            Assert.AreEqual("test_completed", plan.resultCode);
        }

        private sealed class TestExecutor : IPlanExecutor
        {
            public PlanType Type => PlanType.Custom;

            public PlanExecutionResult Execute(Plan plan, float deltaDays)
                => PlanExecutionResult.Complete("test_completed", "执行活动");

            public void OnPlanEnded(Plan plan) { }
        }
    }
}
