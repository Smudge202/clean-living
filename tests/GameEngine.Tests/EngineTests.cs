using FluentAssertions;
using Microsoft.Framework.OptionsModel;
using Moq;
using System;
using Xunit;

namespace CleanLiving.GameEngine.Tests
{
    public class EngineTests
    {
        [UnitTest]
        public void WhenSchedulerNotProvidedThenThrowsException()
        {
            Action act = () => new Engine(null);
            act.ShouldThrow<ArgumentNullException>();
        }

        [UnitTest]
        public void WhenSchedulerProvidedThenSubscribesForTicks()
        {
            var scheduler = new Mock<IScheduler>();
            var engine = new Engine(scheduler.Object);

            scheduler.Verify(m => m.Subscribe(engine), Times.Once);
        }

        [UnitTest]
        public void WhenSubscribedForNotificationOneTickFromNowThenReceivesNotification()
        {
            var scheduler = new Fake.Scheduler();
            var observer = new Mock<IObserver<GameTick>>();
            using (var subscription = SubscribeToEngine(scheduler, observer.Object))
            {
                scheduler.Tick();
                observer.Verify(m => m.OnNext(It.IsAny<GameTick>()), Times.Once);
            }                
        }

        [Theory]
        [InlineData(2), InlineData(5), InlineData(10)]
        public void WhenSubscribedForTicksThenReceivesCorrectNumberOfNotications(int tickCount)
        {
            var scheduler = new Fake.Scheduler();
            var observer = new Mock<IObserver<GameTick>>();
            using (var subscription = SubscribeToEngine(scheduler, observer.Object))
            {
                for (var i = 0; i < tickCount; i++)
                    scheduler.Tick();
                observer.Verify(m => m.OnNext(It.IsAny<GameTick>()), Times.Exactly(tickCount));
            }
        }

        private IDisposable SubscribeToEngine(IScheduler scheduler, IObserver<GameTick> observer)
        {
            var engine = new Engine(scheduler);
            return engine.Subscribe(observer);
        }
    }
}
