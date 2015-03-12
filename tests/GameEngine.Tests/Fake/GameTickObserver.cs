using System;
using System.Threading;

namespace CleanLiving.GameEngine.Tests.Fake
{
    internal sealed class GameTickObserver : IObserver<GameTick>
    {
        internal ManualResetEventSlim OnNextCalled = new ManualResetEventSlim();

        public void OnNext(GameTick value)
        {
            OnNextCalled.Set();
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }
    }
}
