using CleanLiving.TestHelpers;
using FluentAssertions;
using Microsoft.Framework.OptionsModel;
using Moq;
using System;
using System.Linq;
using Xunit;

namespace CleanLiving.Engine.Tests
{
    public class GameEngineTests
    {
        public class Constructor
        {
            [UnitTest]
            public void WhenConfigNotProvidedThenThrowsException()
            {
                Action act = () => new GameEngine<Fake.GameTime>(null, new Mock<ITranslateTime<Fake.GameTime>>().Object, new Mock<IClock>().Object);
                act.ShouldThrow<ArgumentNullException>();
            }

            [UnitTest]
            public void WhenTranslatorNotProvidedThenThrowsException()
            {
                Action act = () => new GameEngine<Fake.GameTime>(new Mock<IOptions<TimeOptions<Fake.GameTime>>>().Object, null, new Mock<IClock>().Object);
                act.ShouldThrow<ArgumentNullException>();
            }

            [UnitTest]
            public void WhenClockNotProvidedThenThrowsException()
            {
                Action act = () => new GameEngine<Fake.GameTime>(new Mock<IOptions<TimeOptions<Fake.GameTime>>>().Object, new Mock<ITranslateTime<Fake.GameTime>>().Object, null);
                act.ShouldThrow<ArgumentNullException>();
            }

            [UnitTest]
            public void WhenConfigDoesNotProvideOptionsThenThrowsException()
            {
                var config = new Mock<IOptions<TimeOptions<Fake.GameTime>>>();
                Action act = () => new GameEngine<Fake.GameTime>(config.Object, new Mock<ITranslateTime<Fake.GameTime>>().Object, new Mock<IClock>().Object);
                act.ShouldThrow<ArgumentException>();
            }
        }

        public class Dispose
        {
            [UnitTest]
            public void WhenDisposedThenShouldThrowObjectDisposedExceptionWhenPublish()
            {
                // TODO(DANjEEEEE): I'm not sure if this is the correct way to test / implement this, so any advice would be useful.
                var engine = new GameEngine<Fake.GameTime>(DefaultConfig, DefaultTranslator, DefaultClock);
                engine.Dispose();

                Action act = () => engine.Publish(new Fake.Event());
                act.ShouldThrow<ObjectDisposedException>();
            }

            [UnitTest]
            public void WhenDisposedThenShouldThrowObjectDisposedExceptionWhenSubscribe()
            {
                // TODO(DANjEEEEE): I'm not sure if this is the correct way to test / implement this, so any advice would be useful.
                var engine = new GameEngine<Fake.GameTime>(DefaultConfig, DefaultTranslator, DefaultClock);
                var observer = new Mock<IObserver<Fake.Event>>();
                engine.Dispose();

                Action act = () => engine.Subscribe(observer.Object);
                act.ShouldThrow<ObjectDisposedException>();
            }

            [UnitTest]
            public void WhenDisposedThenSubscriptionsShouldAlsoBeDisposed()
            {
                // TODO(DANjEEEEE): I'm not sure how I should have written a test for this, so any advice would be useful.
                Assert.True(false);
            }
        }

        public class SubscribeForGameTime
        {
            [UnitTest]
            public void WhenSubscribeWithoutObserverThenThrowsException()
            {
                Action act = () => new GameEngine<Fake.GameTime>(DefaultConfig, DefaultTranslator, DefaultClock)
                    .Subscribe(null, new Fake.Event(), new Fake.GameTime());
                act.ShouldThrow<ArgumentNullException>();
            }

            [UnitTest]
            public void WhenSubscribeWithoutMessageThenThrowsException()
            {
                Action act = () => new GameEngine<Fake.GameTime>(DefaultConfig, DefaultTranslator, DefaultClock)
                    .Subscribe(DefaultTimeObserver, null, new Fake.GameTime());
                act.ShouldThrow<ArgumentNullException>();
            }

            [UnitTest]
            public void WhenSubscribeWithoutGameTimeThenThrowsException()
            {
                Action act = () => new GameEngine<Fake.GameTime>(DefaultConfig, DefaultTranslator, DefaultClock)
                    .Subscribe(DefaultTimeObserver, new Fake.Event(), null);
                act.ShouldThrow<ArgumentNullException>();
            }

            [UnitTest]
            public void WhenSubscribesForGameTimeThenReceivesSubscription()
            {
                var clock = new Mock<IClock>();
                clock.Setup(m => m.Subscribe(It.IsAny<IEngineTimeObserver<Fake.Event>>(), It.IsAny<Fake.Event>(), It.IsAny<EngineTime>()))
                    .Returns(new Mock<IClockSubscription>().Object);
                new GameEngine<Fake.GameTime>(DefaultConfig, DefaultTranslator, clock.Object)
                    .Subscribe(DefaultTimeObserver, new Fake.Event(), new Fake.GameTime())
                    .Should().NotBeNull();
            }

            [UnitTest]
            public void WhenSubscribedForGameTimeThenRequestsEngineTimeTranslation()
            {
                var time = new Fake.GameTime();
                var translator = new Mock<ITranslateTime<Fake.GameTime>>();
                translator.Setup(m => m.ToEngineTime(time)).Returns(EngineTime.Now);

                new GameEngine<Fake.GameTime>(DefaultConfig, translator.Object, DefaultClock)
                    .Subscribe(DefaultTimeObserver, new Fake.Event(), time);

                translator.Verify(m => m.ToEngineTime(time), Times.Once);
            }

            [UnitTest]
            public void WhenGameTimeTranslatedToEngineTimeThenEngineRequestsClockCallback()
            {
                var clock = new Mock<IClock>();
                var time = EngineTime.Now;
                var translator = new Mock<ITranslateTime<Fake.GameTime>>();
                translator.Setup(m => m.ToEngineTime(It.IsAny<Fake.GameTime>())).Returns(time);
                var engine = new GameEngine<Fake.GameTime>(DefaultConfig, translator.Object, clock.Object);
                var message = new Fake.Event();

                engine.Subscribe(DefaultTimeObserver, message, new Fake.GameTime());

                clock.Verify(m => m.Subscribe(engine, message, time), Times.Once);
            }

            [UnitTest]
            public void WhenEngineTimePublishedThenRequestsGameTime()
            {
                var clock = new Fake.Clock();
                var time = EngineTime.Now;
                var translator = new Mock<ITranslateTime<Fake.GameTime>>();
                translator.Setup(m => m.ToGameTime(time)).Returns(new Fake.GameTime());
                var message = new Fake.Event();

                new GameEngine<Fake.GameTime>(DefaultConfig, translator.Object, clock)
                    .Subscribe(DefaultTimeObserver, message, new Fake.GameTime());
                clock.Publish(message, time);

                translator.Verify(m => m.ToGameTime(time), Times.Once);
            }

            [UnitTest]
            public void WhenEngineTimePublishedThenEnginePassesBackMessage()
            {
                var clock = new Fake.Clock();
                var subscriber = new Mock<IGameTimeObserver<Fake.Event, Fake.GameTime>>();
                var message = new Fake.Event();

                new GameEngine<Fake.GameTime>(DefaultConfig, DefaultTranslator, clock)
                    .Subscribe(subscriber.Object, message, new Fake.GameTime());
                clock.Publish(message, EngineTime.Now);

                subscriber.Verify(m => m.OnNext(message, It.IsAny<Fake.GameTime>()), Times.Once);
            }

            [UnitTest]
            public void WhenEngineTimePublishedThenEnginePassesThroughTranslatedGameTime()
            {
                var clock = new Fake.Clock();
                var subscriber = new Mock<IGameTimeObserver<Fake.Event, Fake.GameTime>>();
                var time = new Fake.GameTime();
                var translator = new Mock<ITranslateTime<Fake.GameTime>>();
                translator.Setup(m => m.ToGameTime(It.IsAny<EngineTime>())).Returns(time);

                new GameEngine<Fake.GameTime>(DefaultConfig, translator.Object, clock)
                    .Subscribe(subscriber.Object, new Fake.Event(), time);
                clock.Publish(new Fake.Event(), EngineTime.Now);

                subscriber.Verify(m => m.OnNext(It.IsAny<Fake.Event>(), time), Times.Once);
            }

            [UnitTest]
            public void WhenSubscriptionFullfilledThenDisposesClockSubscription()
            {
                var clock = new Fake.Clock();
                var subscription = new Mock<IClockSubscription>();
                var time = new Fake.GameTime();
                var translator = new Mock<ITranslateTime<Fake.GameTime>>();
                translator.Setup(m => m.ToGameTime(It.IsAny<EngineTime>())).Returns(time);
                clock.SubscribeReturns(subscription.Object);

                new GameEngine<Fake.GameTime>(DefaultConfig, DefaultTranslator, clock)
                    .Subscribe(DefaultTimeObserver, new Fake.Event(), time);
                clock.Publish(new Fake.Event(), EngineTime.Now);

                subscription.Verify(m => m.Dispose(), Times.Once);
            }

            [Theory]
            [InlineData(1), InlineData(2), InlineData(5)]
            public void WhenSubscriptionsExistForDifferentTimesThenEnginePassesNotificationsToCorrectSubscribers(int numOfSubscribers)
            {
                var clock = new Fake.Clock();
                var testCases = Enumerable.Range(1, numOfSubscribers)
                    .Select(x => new
                    {
                        Subscriber = new Mock<IGameTimeObserver<Fake.Event, Fake.GameTime>>(),
                        EngineTime = EngineTime.Now.Add(x),
                        GameTime = new Fake.GameTime()
                    })
                    .ToList();
                var translator = new Mock<ITranslateTime<Fake.GameTime>>();
                foreach (var testCase in testCases)
                    translator.Setup(m => m.ToGameTime(testCase.EngineTime)).Returns(testCase.GameTime);

                var engine = new GameEngine<Fake.GameTime>(DefaultConfig, translator.Object, clock);
                foreach (var testCase in testCases)
                    engine.Subscribe(testCase.Subscriber.Object, new Fake.Event(), testCase.GameTime);

                foreach (var testCase in testCases)
                    clock.Publish(new Fake.Event(), testCase.EngineTime);

                foreach (var testCase in testCases)
                    testCase.Subscriber.Verify(m => m.OnNext(It.IsAny<Fake.Event>(), testCase.GameTime), Times.Once());
            }

            [UnitTest]
            public void WhenSubscriptionIsDisposedThenEngineShouldDisposeClockSubscription()
            {
                var clock = new Mock<IClock>(MockBehavior.Strict);
                var clockSubscription = new Mock<IClockSubscription>();
                clock.Setup(m => m.Subscribe(It.IsAny<IEngineTimeObserver<IEvent>>(), It.IsAny<IEvent>(), It.IsAny<EngineTime>()))
                    .Returns(clockSubscription.Object);
                var engine = new GameEngine<Fake.GameTime>(DefaultConfig, DefaultTranslator, clock.Object);

                using (engine.Subscribe(new Mock<IGameTimeObserver<Fake.Event, Fake.GameTime>>().Object, new Fake.Event(), new Fake.GameTime())) { }

                clockSubscription.Verify(m => m.Dispose(), Times.Once);
            }
        }

        public class SubscribeForEvent
        {
            [UnitTest]
            public void WhenSubscribeWithoutObserverThenThrowsException()
            {
                Action act = () => new GameEngine<Fake.GameTime>(DefaultConfig, DefaultTranslator, DefaultClock)
                    .Subscribe<IEvent>(null);
                act.ShouldThrow<ArgumentNullException>();
            }

            [UnitTest]
            public void WhenSubscribeForEventThenReceivesSubscription()
            {
                new GameEngine<Fake.GameTime>(DefaultConfig, DefaultTranslator, DefaultClock)
                    .Subscribe(new Mock<IObserver<IEvent>>().Object)
                    .Should().NotBeNull();
            }

            [UnitTest]
            public void WhenEventMatchingSubscriptionIsRaisedThenNotifiesSubscriber()
            {
                var engine = new GameEngine<Fake.GameTime>(DefaultConfig, DefaultTranslator, DefaultClock);
                var observer = new Mock<IObserver<Fake.Event>>();
                engine.Subscribe(observer.Object);
                var message = new Fake.Event();

                engine.Publish(message);

                observer.Verify(m => m.OnNext(message), Times.Once);
            }

            [UnitTest]
            public void WhenSubscriptionIsDisposedThenShouldNotNotifyObserver()
            {
                var engine = new GameEngine<Fake.GameTime>(DefaultConfig, DefaultTranslator, DefaultClock);
                var observer = new Mock<IObserver<Fake.Event>>();
                using (var subscription = engine.Subscribe(observer.Object)) { }

                engine.Publish(new Fake.Event());

                observer.Verify(m => m.OnNext(It.IsAny<Fake.Event>()), Times.Never);
            }
        }

        private static IOptions<TimeOptions<Fake.GameTime>> DefaultConfig
        {
            get
            {
                var config = new Mock<IOptions<TimeOptions<Fake.GameTime>>>();
                config.SetupGet(m => m.Options).Returns(new TimeOptions<Fake.GameTime>());
                return config.Object;
            }
        }

        private static ITranslateTime<Fake.GameTime> DefaultTranslator
        {
            get
            {
                return new Mock<ITranslateTime<Fake.GameTime>>().Object;
            }
        }

        private static IClock DefaultClock
        {
            get
            {
                return new Mock<IClock>().Object;
            }
        }

        private static IGameTimeObserver<Fake.Event, Fake.GameTime> DefaultTimeObserver
        {
            get
            {
                return new Mock<IGameTimeObserver<Fake.Event, Fake.GameTime>>().Object;
            }
        }
    }
}
