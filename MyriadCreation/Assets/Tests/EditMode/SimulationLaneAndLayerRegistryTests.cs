using NUnit.Framework;
using MyriadCreation.Simulation.Scheduling;
using MyriadCreation.Rendering;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>双频率车道调度 + 地图图层注册表。</summary>
    public class SimulationLaneAndLayerRegistryTests
    {
        [Test]
        public void Scheduler_RunsOnlyRequestedLane()
        {
            var scheduler = new SimulationScheduler();
            int env = 0, civ = 0, both = 0;
            scheduler.Register("env", SimulationCadence.Daily, 0, SimulationLane.Environment, _ => env++);
            scheduler.Register("civ", SimulationCadence.Daily, 0, SimulationLane.Civilization, _ => civ++);
            scheduler.Register("both", SimulationCadence.Daily, 0, _ => both++);

            scheduler.Tick(1, 1, 1, SimulationLane.Environment);
            Assert.AreEqual(1, env);
            Assert.AreEqual(0, civ);
            Assert.AreEqual(1, both);

            scheduler.Tick(2, 1, 1, SimulationLane.Civilization);
            Assert.AreEqual(1, env);
            Assert.AreEqual(1, civ);
            Assert.AreEqual(2, both);

            scheduler.Tick(3, 1, 1, SimulationLane.None);
            Assert.AreEqual(2, both, "None 不跑任何车道");
        }

        [Test]
        public void Scheduler_BothLaneRunsEverything()
        {
            var scheduler = new SimulationScheduler();
            int env = 0, civ = 0;
            scheduler.Register("env", SimulationCadence.Daily, 0, SimulationLane.Environment, _ => env++);
            scheduler.Register("civ", SimulationCadence.Daily, 0, SimulationLane.Civilization, _ => civ++);
            scheduler.Tick(1, 1, 1);
            Assert.AreEqual(1, env);
            Assert.AreEqual(1, civ);
        }

        [Test]
        public void LayerRegistry_ExposesBuiltInsAndFilters()
        {
            MapLayerRegistry.Clear();
            var all = MapLayerRegistry.List();
            Assert.GreaterOrEqual(all.Count, 12);
            Assert.IsTrue(MapLayerRegistry.TryGet("culture", out var culture));
            Assert.AreEqual(MapDisplayMode.Culture, culture.mode);
            Assert.IsTrue(MapLayerRegistry.TryGet("religion_tradition", out var tradition));
            Assert.AreEqual(MapDisplayMode.ReligionTradition, tradition.mode);

            var overlays = MapLayerRegistry.List("overlay");
            Assert.IsTrue(overlays.Exists(l => l.id == "admin_borders"));
            Assert.IsFalse(overlays.Exists(l => l.id == "culture"));
        }

        [Test]
        public void LayerRegistry_CustomLayerReplacesById()
        {
            MapLayerRegistry.Clear();
            MapLayerRegistry.Register(new MapLayerDefinition
            {
                id = "culture", label = "自定义文化", group = "society",
                mode = MapDisplayMode.Culture, order = 99,
            });
            Assert.IsTrue(MapLayerRegistry.TryGet("culture", out var layer));
            Assert.AreEqual("自定义文化", layer.label);
        }
    }
}
