using NUnit.Framework;
using MyriadCreation.Core.Events;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>事件总线：系统发布事实，统计/其他系统只订阅。</summary>
    public class SimulationEventBusTests
    {
        [Test]
        public void Recorder_CountsDomainEvents()
        {
            var bus = new SimulationEventBus();
            using var recorder = new SimulationStatisticsRecorder(bus);

            bus.Publish(new WarDeclaredEvent(1, 10, 20, 5));
            bus.Publish(new WarEndedEvent(1, 10, 20, 10, "victory", 15));
            bus.Publish(new EconomyDayTickedEvent(15));
            bus.Publish(new SeasonChangedEvent(15, 1, 2));

            Assert.AreEqual(1, recorder.WarsDeclared);
            Assert.AreEqual(1, recorder.WarsEnded);
            Assert.AreEqual(1, recorder.EconomyTicks);
            Assert.AreEqual(1, recorder.SeasonsChanged);
        }

        [Test]
        public void Unsubscribe_StopsReceiving()
        {
            var bus = new SimulationEventBus();
            int count = 0;
            System.Action<WarDeclaredEvent> handler = _ => count++;
            bus.Subscribe(handler);
            bus.Publish(new WarDeclaredEvent(1, 1, 2, 1));
            bus.Unsubscribe(handler);
            bus.Publish(new WarDeclaredEvent(2, 1, 2, 2));
            Assert.AreEqual(1, count);
        }

        [Test]
        public void DisposedRecorder_DoesNotReceive()
        {
            var bus = new SimulationEventBus();
            var recorder = new SimulationStatisticsRecorder(bus);
            bus.Publish(new WarDeclaredEvent(1, 1, 2, 1));
            recorder.Dispose();
            bus.Publish(new WarDeclaredEvent(2, 1, 2, 2));
            Assert.AreEqual(1, recorder.WarsDeclared);
        }
    }
}
