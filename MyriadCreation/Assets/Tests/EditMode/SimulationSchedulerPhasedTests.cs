using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Simulation.Scheduling;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P2.6：调度器两相注册——所有 Compute 先跑，Apply 再统一写。</summary>
    public class SimulationSchedulerPhasedTests
    {
        [Test]
        public void PhasedEntry_ComputesBeforeAllApplies()
        {
            var scheduler = new SimulationScheduler();
            var order = new List<string>();
            scheduler.RegisterPhased("a.phased", SimulationCadence.Daily, 1,
                _ => order.Add("compute-a"),
                _ => order.Add("apply-a"));
            scheduler.Register("b.normal", SimulationCadence.Daily, 2, _ => order.Add("normal-b"));

            scheduler.Tick(1, 1, 1);

            CollectionAssert.AreEqual(new[] { "compute-a", "normal-b", "apply-a" }, order,
                "Compute 必须全部完成后才进入 Apply 相");
        }

        [Test]
        public void PhasedEntries_ApplyInOrder_AfterComputes()
        {
            var scheduler = new SimulationScheduler();
            var order = new List<string>();
            scheduler.RegisterPhased("a.first", SimulationCadence.Daily, 1,
                _ => order.Add("c1"), _ => order.Add("a1"));
            scheduler.RegisterPhased("b.second", SimulationCadence.Daily, 2,
                _ => order.Add("c2"), _ => order.Add("a2"));

            scheduler.Tick(1, 1, 1);

            CollectionAssert.AreEqual(new[] { "c1", "c2", "a1", "a2" }, order);
        }
    }
}
