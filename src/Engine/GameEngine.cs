using Microsoft.Framework.OptionsModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CleanLiving.Engine
{
    public class GameEngine<TTime> : IEngine<TTime>, IEngineTimeObserver<IEvent>
    {
        private readonly IClock _clock;
        private readonly ITranslateTime<TTime> _translator;

        // TODO : Move to dependency if/when used subscriptions need clearing
        private Dictionary<Type, List<GameEventSubscription>> _eventSubscriptions =
            new Dictionary<Type, List<GameEventSubscription>>();

        private Dictionary<Type, List<GameTimeSubscription>> _timeSubscriptions =
            new Dictionary<Type, List<GameTimeSubscription>>();

        public GameEngine(IOptions<TimeOptions<TTime>> config, ITranslateTime<TTime> timeTranslator, IClock clock)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (timeTranslator == null) throw new ArgumentNullException(nameof(timeTranslator));
            if (clock == null) throw new ArgumentNullException(nameof(clock));
            if (config.Options == null) throw new ArgumentException(nameof(config.Options));
            _clock = clock;
            _translator = timeTranslator;
        }

        public void OnNext<T>(T message, EngineTime time) where T : IEvent
        {
            if (_disposedValue) throw new ObjectDisposedException("GameEngine");

            var timeSubscriptions = _timeSubscriptions[typeof(T)]
                .Select(x => x as GameTimeSubscription<T, TTime>);
            foreach (var subscription in timeSubscriptions)
                subscription.Publish(message, _translator.ToGameTime(time));
        }

        public void Publish<T>(T message) where T : IEvent
        {
            if (_disposedValue) throw new ObjectDisposedException("GameEngine");

            var eventSubscriptions = _eventSubscriptions[typeof(T)]
                .Select(x => x as GameEventSubscription<T>);
            foreach (var subscription in eventSubscriptions)
                subscription.Publish(message);
        }

        public IDisposable Subscribe<T>(IObserver<T> observer) where T : IEvent
        {
            if (_disposedValue) throw new ObjectDisposedException("GameEngine");
            if (observer == null) throw new ArgumentNullException(nameof(observer));

            var subscription = new GameEventSubscription<T>(observer);
            var eventType = typeof(T);

            if (_eventSubscriptions.ContainsKey(eventType))
                _eventSubscriptions[eventType].Add(subscription);
            else
                _eventSubscriptions.Add(eventType, new List<GameEventSubscription>() { subscription });
            return subscription;
        }

        public IDisposable Subscribe<T>(IGameTimeObserver<T, TTime> observer, T message, TTime time) where T : IEvent
        {
            if (observer == null) throw new ArgumentNullException(nameof(observer));
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (time == null) throw new ArgumentNullException(nameof(time));

            var clockSubscription = _clock.Subscribe(this, message, _translator.ToEngineTime(time));
            var subscription = GameTimeSubscription.For(observer, message, clockSubscription);

            var messageType = typeof(T);
            if (_timeSubscriptions.ContainsKey(messageType))
                _timeSubscriptions[messageType].Add(subscription);
            else
                _timeSubscriptions.Add(messageType, new List<GameTimeSubscription>() { subscription });
            return subscription;
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        #region IDisposable Implementation

        private bool _disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (_eventSubscriptions != null && _eventSubscriptions.Any())
                    {
                        foreach (var item in _eventSubscriptions)
                        {
                            foreach (var subscription in item.Value)
                            {
                                subscription.Dispose();
                            }

                            _eventSubscriptions.Remove(item.Key);
                        }
                    }

                    if (_timeSubscriptions != null && _timeSubscriptions.Any())
                    {
                        foreach (var item in _timeSubscriptions)
                        {
                            foreach (var subscription in item.Value)
                            {
                                subscription.Dispose();
                            }

                            _timeSubscriptions.Remove(item.Key);
                        }
                    }
                }

                _eventSubscriptions = null;
                _timeSubscriptions = null;

                _disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
