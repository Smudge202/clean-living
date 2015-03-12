using System;

namespace CleanLiving.GameEngine.Tests.Fake
{
    internal sealed class Scheduler : IScheduler
    {
        private IObserver<GameTick> _observer;

        internal void Tick()
        {
            _observer.OnNext(new GameTick());
        }

        public IDisposable Subscribe(IObserver<GameTick> observer)
        {
            _observer = observer;
            return null;
        }
    }
}
