using Microsoft.Framework.OptionsModel;
using System;

namespace CleanLiving.GameEngine
{
    public class Scheduler : IScheduler
    {
        public Scheduler(IOptions<SchedulerConfiguration> config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (config.Options == null)
                throw new SchedulerConfigurationException();
            if (config.Options.GameTicksPerSecond <= 0)
                throw new InvalidSchedulerConfigurationException(nameof(config.Options.GameTicksPerSecond));
        }

        public IDisposable Subscribe(IObserver<GameTick> observer)
        {
            throw new NotImplementedException();
        }
    }
}
