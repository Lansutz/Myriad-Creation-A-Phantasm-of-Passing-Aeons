using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using CivilizationEvolution.Core.Events;
using CivilizationEvolution.Core.Contracts;
using CivilizationEvolution.Core.Simulation;
using CivilizationEvolution.Simulation.Modding;
using CivilizationEvolution.Simulation.Economy;
using CivilizationEvolution.Simulation.Disaster;
using CivilizationEvolution.Simulation.AI;
using CivilizationEvolution.Simulation.Characters;
using CivilizationEvolution.Simulation.Planning;
using CivilizationEvolution.Simulation.Diplomacy;
using CivilizationEvolution.Simulation.Society.Building;
using CivilizationEvolution.Simulation.Planning;

namespace CivilizationEvolution.Tests.EditMode
{
    public class ArchitectureContractsTests
    {
        [Test]
        public void DomainSchedules_RegisterThroughStableSchedulerContract()
        {
            var scheduler = new SimulationScheduler();
            var economy = new EconomyManager(
                new TileData[0],
                new Dictionary<int, TradeCenter>(),
                new Dictionary<int, GoodsDef>(),
                null,
                null);
            var buildings = new BuildingSystem(new TileData[0]);

            EconomySchedule.Register(scheduler, economy);
            BuildingSchedule.Register(scheduler, buildings);
            var disasters = new DisasterSystem(new TileData[0], 0, 0);
            var diseases = new DiseaseSystem(new TileData[0], null, 0, 0);
            var diplomacy = new DiplomacyManager(new Dictionary<int, RealmData>());
            DisasterSchedule.Register(scheduler, disasters, diseases, () => 1, () => 1);
            DiplomacySchedule.Register(scheduler, diplomacy, () => 1);

            Assert.AreEqual(4, scheduler.Count);
        }

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
        public void ContentSourceCatalog_OrdersBaseBeforeMod()
        {
            var sources = ContentSourceCatalog.CreateDefault("StreamingAssets");
            Assert.AreEqual(2, sources.Count);
            Assert.AreEqual(ContentSourceKind.Base, sources[0].kind);
            Assert.AreEqual(ContentSourceKind.Mod, sources[1].kind);
            Assert.Less(sources[0].priority, sources[1].priority);
        }

        [Test]
        public void ContentAuthoringWorkspace_PreservesCanonicalRuntimeFiles()
        {
            var workspace = new ContentAuthoringWorkspace();
            workspace.SetManifest(new ContentPackageManifest
            {
                packageId = "test.package",
                displayName = "Test Package"
            });

            workspace.UpsertFile("Innovation/Innovations.json", "{ \"innovations\": [] }");

            Assert.IsTrue(workspace.GetFiles().ContainsKey("Innovation/Innovations.json"));
            Assert.AreEqual("{ \"innovations\": [] }",
                workspace.GetFiles()["Innovation/Innovations.json"]);
        }

        [Test]
        public void ContentResolvers_ExposeReadOnlyStableContracts()
        {
            ContentRegistry.Reset();
            ContentRegistry.Initialize();

            Assert.IsTrue(ContentResolvers.Cultures.TryGet(1, out var culture));
            Assert.IsNotNull(culture);
            Assert.IsTrue(ContentResolvers.Innovations.TryGet(1, out var innovation));
            Assert.IsNotNull(innovation);
            Assert.IsTrue(ContentResolvers.Languages.All.Any());
        }

        [Test]
        public void ContentPackageValidator_RejectsUnsafeOrUnknownFiles()
        {
            var manifest = new ContentPackageManifest
            {
                packageId = "example.mod",
                displayName = "Example Mod",
                contentTypes = { "Race", "Innovation" }
            };

            var valid = ContentPackageValidator.Validate(
                manifest,
                new[]
                {
                    "mod.json",
                    "Race/RaceDefs.json",
                    "Innovation/Innovations.json"
                });
            Assert.IsTrue(valid.IsValid);

            var invalid = ContentPackageValidator.Validate(
                manifest,
                new[]
                {
                    "../outside.json",
                    "Scripts/Injected.txt"
                });
            Assert.IsFalse(invalid.IsValid);
            Assert.IsTrue(invalid.Errors.Count >= 2);
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

        }
        [Test]
        public void PlanSystem_UsesSharedPurposeAndStructuredResult()
        {
            var plans = new PlanSystem();
            plans.RegisterExecutor(new StructuredTestExecutor());

            var plan = plans.CreatePlan(
                PlanType.Custom, 1, 2, 10,
                purpose: "测试统一计划",
                targetKind: "realm");

            Assert.AreEqual("测试统一计划", plan.purpose);
            Assert.AreEqual("realm", plan.targetKind);
            Assert.IsTrue(plans.AcceptPlan(plan.planId));
            Assert.IsTrue(plans.BeginPreparation(plan.planId));
            Assert.IsTrue(plans.BeginExecution(plan.planId));
            plans.DailyTick(10, 1f);

            Assert.AreEqual(PlanState.Completed, plan.state);
            Assert.AreEqual("structured", plan.resultCode);
            Assert.AreEqual("完成测试", plan.resultSummary);
        }

        [Test]
        public void PlanSystem_RejectsInvalidLifecycleTransition()
        {
            var plans = new PlanSystem();
            var plan = plans.CreatePlan(PlanType.Custom, 1, 2, 10);

            Assert.IsFalse(plans.BeginExecution(plan.planId));
            Assert.AreEqual(PlanState.Proposed, plan.state);
            Assert.IsTrue(plans.AcceptPlan(plan.planId));
            Assert.IsFalse(plans.AcceptPlan(plan.planId));
            Assert.AreEqual(PlanState.Accepted, plan.state);
        }

        private sealed class StructuredTestExecutor : IPlanExecutor
        {
            public PlanType Type => PlanType.Custom;

            public PlanExecutionResult Execute(Plan plan, float deltaDays)
                => PlanExecutionResult.Complete("structured", "执行活动", "完成测试");
        }

        [Test]
        public void SimulationScheduler_RunsRegisteredCadencesInStableOrder()
        {
            var scheduler = new SimulationScheduler();
            var order = new List<string>();
            scheduler.Register("monthly", SimulationCadence.Monthly, 20, _ => order.Add("monthly"));
            scheduler.Register("daily", SimulationCadence.Daily, 10, _ => order.Add("daily"));
            scheduler.Register("weekly", SimulationCadence.Weekly, 15, _ => order.Add("weekly"));

            scheduler.Tick(30, 1);

            CollectionAssert.AreEqual(new[] { "daily", "weekly", "monthly" }, order);
        }

        [Test]
        public void SimulationScheduler_DirtyRegistrationRunsOnlyWhenMarked()
        {
            var scheduler = new SimulationScheduler();
            int runs = 0;
            scheduler.RegisterDirty("terrain", SimulationCadence.Daily, 10, "world.terrain", _ => runs++);

            scheduler.Tick(1, 1, 1);
            Assert.AreEqual(0, runs);

            scheduler.Dirty.Mark("world.terrain");
            scheduler.Tick(2, 1, 1);
            Assert.AreEqual(1, runs);

            scheduler.Tick(3, 1, 1);
            Assert.AreEqual(1, runs);
        }

        [Test]
        public void SimulationEventDirtyBridge_MapsFactsToDirtyAndUnsubscribes()
        {
            var events = new SimulationEventBus();
            var scheduler = new SimulationScheduler();
            using (var bridge = SimulationEventDirtyBridge.Create(events, scheduler.Dirty))
            {
                bridge.Register<PracticeRecordedEvent>("innovation.practice",
                    evt => evt.amount >= 2f);

                events.Publish(new PracticeRecordedEvent(1, 2, 1f));
                Assert.IsFalse(scheduler.Dirty.IsDirty("innovation.practice"));

                events.Publish(new PracticeRecordedEvent(1, 2, 2f));
                Assert.IsTrue(scheduler.Dirty.IsDirty("innovation.practice"));
            }

            scheduler.Dirty.ClearAll();
            events.Publish(new PracticeRecordedEvent(1, 2, 3f));
            Assert.IsFalse(scheduler.Dirty.IsDirty("innovation.practice"));
        }

        [Test]
        public void SimulationScheduler_DirtyExecutionPreservesReinvalidations()
        {
            var scheduler = new SimulationScheduler();
            int runs = 0;
            scheduler.RegisterDirty("terrain", SimulationCadence.Daily, 10, "world.terrain", _ =>
            {
                runs++;
                if (runs == 1)
                    scheduler.Dirty.Mark("world.terrain");
            });

            scheduler.Dirty.Mark("world.terrain");
            scheduler.Tick(1, 1, 1);
            Assert.AreEqual(1, runs);
            Assert.IsTrue(scheduler.Dirty.IsDirty("world.terrain"));

            scheduler.Tick(2, 1, 1);
            Assert.AreEqual(2, runs);
            Assert.IsFalse(scheduler.Dirty.IsDirty("world.terrain"));
        }

        [Test]
        public void SimulationCommandBus_DispatchesWithoutExposingTarget()
        {
            var bus = new SimulationCommandBus();
            bus.Register(new TestCommandHandler());
            var result = bus.Send(new TestCommand(7));
            Assert.IsTrue(result.success);
            Assert.AreEqual("handled", result.code);
            Assert.IsTrue(bus.Unregister<TestCommand>());
            Assert.IsFalse(bus.Send(new TestCommand(8)).success);
        }

        [Test]
        public void SimulationQueryBus_ReturnsReadOnlyResult()
        {
            var bus = new SimulationQueryBus();
            bus.Register<TestQuery, int>(new TestQueryHandler());
            Assert.AreEqual(42, bus.Ask<TestQuery, int>(new TestQuery(7)));
            Assert.IsTrue(bus.TryAsk<TestQuery, int>(new TestQuery(8), out var value));
            Assert.AreEqual(43, value);
        }

        [Test]
        public void JsonContentProvider_LoadsCanonicalWrappedFileIntoStore()
        {
            string root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "myriad-content-provider-test");
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(root, "Race"));
            string file = System.IO.Path.Combine(root, "Race", "RaceDefs.json");
            try
            {
                System.IO.File.WriteAllText(file, "{ \"items\": [{ \"id\": \"race.test\" }] }");
                var provider = new JsonFileContentProvider<string, TestContent, TestWrapper>(
                    "Test", "Race/RaceDefs.json", text => UnityEngine.JsonUtility.FromJson<TestWrapper>(text),
                    wrapper => wrapper.items, item => item == null ? null : item.id);
                var store = new ContentStore<string, TestContent>();
                provider.Load(new ContentSource(ContentSourceKind.Mod, "test", root, 100), store);
                Assert.IsTrue(store.TryGet("race.test", out var value));
                Assert.AreEqual("race.test", value.id);
            }
            finally { if (System.IO.Directory.Exists(root)) System.IO.Directory.Delete(root, true); }
        }

        [Test]
        public void ContentProviderCatalog_LoadsAllRegisteredProviders()
        {
            var store = new ContentStore<string, TestContent>();
            var catalog = new ContentProviderCatalog();
            catalog.Add(new TestProvider("a", "alpha"), store);
            catalog.Add(new TestProvider("b", "beta"), store);

            catalog.Load(new ContentSource(ContentSourceKind.Base, "test", "unused", 0));

            Assert.AreEqual(2, catalog.Count);
            Assert.IsTrue(store.TryGet("a", out var a));
            Assert.IsTrue(store.TryGet("b", out var b));
            Assert.AreEqual("alpha", a.id);
            Assert.AreEqual("beta", b.id);
        }

        [Test]
        public void ContentStore_ProvidesMutableStorageBehindStableResolver()
        {
            var store = new ContentStore<string, TestContent>();
            store.Set("a", new TestContent { id = "a" });

            IContentResolver<string, TestContent> resolver = store;
            Assert.IsTrue(resolver.TryGet("a", out var value));
            Assert.AreEqual("a", value.id);
            Assert.AreEqual(1, resolver.All.Count());

            Assert.IsTrue(store.Remove("a"));
            Assert.IsFalse(resolver.TryGet("a", out _));
        }

        private readonly struct TestCommand : ISimulationCommand
        {
            public readonly int value;
            public TestCommand(int value) { this.value = value; }
        }

        private sealed class TestCommandHandler : ISimulationCommandHandler<TestCommand>
        {
            public CommandResult Handle(TestCommand command) => CommandResult.Success("handled");
        }

        private readonly struct TestQuery : ISimulationQuery<int>
        {
            public readonly int value;
            public TestQuery(int value) { this.value = value; }
        }

        private sealed class TestQueryHandler : ISimulationQueryHandler<TestQuery, int>
        {
            public int Handle(TestQuery query) => query.value + 35;
        }

        private sealed class TestProvider : IContentProvider<string, TestContent>
        {
            private readonly string _id;
            private readonly string _value;

            public TestProvider(string id, string value)
            {
                _id = id;
                _value = value;
            }

            public string ContentType => _id;

            public void Load(ContentSource source, IContentStore<string, TestContent> target)
                => target.Set(_id, new TestContent { id = _value });
        }

        [System.Serializable]
        private sealed class TestWrapper { public List<TestContent> items = new List<TestContent>(); }

        [System.Serializable]
        private sealed class TestContent { public string id; }

    }
}
