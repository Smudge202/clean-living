using System;

namespace CleanLiving.GameEngine
{
    public interface IScheduler : IObservable<GameTick> { }
}
