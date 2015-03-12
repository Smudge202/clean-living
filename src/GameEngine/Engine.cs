using System;

namespace CleanLiving.GameEngine
{
    public class Engine : IObservable<GameTick>, IObserver<GameTick>
    {
        private IObserver<GameTick> _observer;

        public Engine(IScheduler scheduler)
        {
            if (scheduler == null)
                throw new ArgumentNullException();
            scheduler.Subscribe(this);
        }

        public void OnNext(GameTick value)
        {
            _observer.OnNext(new GameTick());
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public IDisposable Subscribe(IObserver<GameTick> observer)
        {
            _observer = observer;
            return null;
        }
    }
}
