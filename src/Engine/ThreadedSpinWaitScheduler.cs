﻿using Microsoft.Framework.OptionsModel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CleanLiving.Engine
{
    [Obsolete("UNSAFE CLASS! This class must not be used in debug, unit or component tests,")]
    internal class ThreadedSpinWaitScheduler : IScheduler, IDisposable
    {
        private readonly IOptions<ThreadedSpinWaitSchedulerOptions> _config;

        private readonly ManualResetEventSlim _release = new ManualResetEventSlim();
        private readonly ManualResetEventSlim _completed = new ManualResetEventSlim();
        private readonly ReaderWriterLockSlim _subscriptionsLock = new ReaderWriterLockSlim();
        private readonly CancellationTokenSource _scheduler = new CancellationTokenSource();

        private ConcurrentDictionary<long, ConcurrentBag<SchedulerSubscription>> _subscriptions
            = new ConcurrentDictionary<long, ConcurrentBag<SchedulerSubscription>>();

        public ThreadedSpinWaitScheduler(IOptions<ThreadedSpinWaitSchedulerOptions> config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _config = config;
            var scheduler = new Thread(new ThreadStart(StartScheduler));
            scheduler.Name = _config.Options.SchedulerThreadName;
            scheduler.Start();
        }

        public IDisposable Subscribe(IObserver<long> observer, long nanosecondsFromNow)
        {
            var realtime = GameTime.Elapsed + nanosecondsFromNow;
            var subscription = new SchedulerSubscription(observer);
            _subscriptionsLock.EnterWriteLock();
            var realtimeEvent = _subscriptions.GetOrAdd(realtime, new ConcurrentBag<SchedulerSubscription>());
            realtimeEvent.Add(subscription);
            _subscriptionsLock.ExitWriteLock();
            _release.Set();
            return subscription;
        }

        private void StartScheduler()
        {
            while (true)
            {
                if (_scheduler.IsCancellationRequested) break;
                WaitForSubscriptions();

                // TODO: Review for optimal enumeration pattern
                var currentSubscriptions = GetCurrentSubscriptions();
                var elapsedSubscriptions = currentSubscriptions
                    .Where(x => x.Key <= (GameTime.Elapsed + _config.Options.AcceptableSpinWaitPeriodNanoseconds))
                    .ToList();
                foreach (var subscription in currentSubscriptions.Except(elapsedSubscriptions))
                    RescheduleSubscription(subscription);

                var sortedElapsedSubscriptions = new SortedList<long, ConcurrentBag<SchedulerSubscription>>(elapsedSubscriptions.Count);
                elapsedSubscriptions.ForEach(x => sortedElapsedSubscriptions.Add(x.Key, x.Value));
                foreach (var subscription in elapsedSubscriptions)
                    WaitToPublish(subscription);
            }
            DisposeSubscriptions();
        }

        private void DisposeSubscriptions()
        {
            // TODO : Address potential racing condition in concurrent STA subscription
            foreach (var subscription in GetCurrentSubscriptions())
                foreach (var observer in subscription.Value)
                    observer.Dispose();
            _completed.Set();
        }

        private void WaitToPublish(KeyValuePair<long, ConcurrentBag<SchedulerSubscription>> subscription)
        {
            while (GameTime.Elapsed > subscription.Key) Thread.SpinWait(_config.Options.SpinWaitIterations);
            foreach (var observer in subscription.Value)
                observer.Publish(GameTime.Elapsed);
        }

        private void WaitForSubscriptions()
        {
            _subscriptionsLock.EnterReadLock();
            var isSubscriptions = _subscriptions.Any();
            if (isSubscriptions && !_release.IsSet) _release.Set();
            else if (!isSubscriptions && _release.IsSet) _release.Reset();
            _subscriptionsLock.ExitReadLock();
            _release.Wait(_scheduler.Token);
        }

        private void RescheduleSubscription(KeyValuePair<long, ConcurrentBag<SchedulerSubscription>> subscription)
        {
            var scheduledSubscription = _subscriptions.GetOrAdd(subscription.Key, new ConcurrentBag<SchedulerSubscription>());
            foreach (var subscriptionObserver in subscription.Value)
                scheduledSubscription.Add(subscriptionObserver);
        }

        private ConcurrentDictionary<long, ConcurrentBag<SchedulerSubscription>> GetCurrentSubscriptions()
        {
            var newSubscriptions = new ConcurrentDictionary<long, ConcurrentBag<SchedulerSubscription>>();
            ConcurrentDictionary<long, ConcurrentBag<SchedulerSubscription>> currentSubscriptions = null;
            while (_subscriptions != newSubscriptions)
                currentSubscriptions = Interlocked.Exchange(ref _subscriptions, newSubscriptions);
            return currentSubscriptions;
        }

        public void Dispose()
        {
            _scheduler.Cancel();
            _release.Set();
            // TODO: Consider ramifications of slow disposal
            _completed.Wait();
        }
    }
}
