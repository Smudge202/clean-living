﻿using FluentAssertions;
using Moq;
using System;
using System.Linq;
using Xunit;

namespace CleanLiving.Engine.Tests
{
    public class StateEngineTests
    {
        [UnitTest]
        public void WhenClockNotProvidedThenThrowsException()
        {
            Action act = () => new StateEngine(null);
            act.ShouldThrow<ArgumentNullException>();
        }

        [UnitTest]
        public void WhenSubscribesForGameTimeThenReceivesSubscription()
        {
            var clock = new Mock<IClock>();
            clock.Setup(m => m.Subscribe(It.IsAny<IObserver<GameTime>>(), It.IsAny<GameTime>())).Returns(new Mock<IDisposable>().Object);
            new StateEngine(clock.Object).Subscribe(new Mock<IObserver<GameTime>>().Object, GameTime.Now.Add(1))
                .Should().NotBeNull();
        }

        [UnitTest]
        public void WhenSubscribedForGameTimeThenEngineRequestsClockCallback()
        {
            var clock = new Mock<IClock>();
            var time = GameTime.Now.Add(1);
            var engine = new StateEngine(clock.Object);
            engine.Subscribe(new Mock<IObserver<GameTime>>().Object, time);

            clock.Verify(m => m.Subscribe(engine, time), Times.Once);
        }

        [UnitTest]
        public void WhenSubscribedForGameTimeThenEnginePassesThroughClockNotifications()
        {
            var clock = new Fake.Clock();
            var subscriber = new Mock<IObserver<GameTime>>();
            var time = GameTime.Now.Add(1);
            new StateEngine(clock).Subscribe(subscriber.Object, time);
            clock.Publish(time);

            subscriber.Verify(m => m.OnNext(time), Times.Once);
        }

        [Theory]
        [InlineData(1), InlineData(2), InlineData(5)]
        public void WhenSubscriptionsExistForDifferentTimesThenEnginePassesNotificationsToCorrectSubscribers(int numOfSubscribers)
        {
            var clock = new Fake.Clock();
            var testCases = Enumerable.Range(1, numOfSubscribers)
                .Select(x => new { Subscriber = new Mock<IObserver<GameTime>>(), Time = GameTime.Now.Add(x) })
                .ToList();            
            var engine = new StateEngine(clock);
            foreach (var testCase in testCases)
                engine.Subscribe(testCase.Subscriber.Object, testCase.Time);

            foreach (var testCase in testCases)
                clock.Publish(testCase.Time);

            foreach (var testCase in testCases)
                testCase.Subscriber.Verify(m => m.OnNext(testCase.Time), Times.Once());
        }

        [UnitTest]
        public void WhenSubscriptionIsDisposedThenEngineShouldDisposeClockSubscription()
        {
            var clock = new Mock<IClock>();
            var clockSubscription = new Mock<IDisposable>();
            clock.Setup(m => m.Subscribe(It.IsAny<IObserver<GameTime>>(), It.IsAny<GameTime>())).Returns(clockSubscription.Object);
            using (var subscription = new StateEngine(clock.Object).Subscribe(new Mock<IObserver<GameTime>>().Object, GameTime.Now.Add(1))) { }

            clockSubscription.Verify(m => m.Dispose(), Times.Once);
        }
    }
}
