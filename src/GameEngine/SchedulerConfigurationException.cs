using System;

namespace CleanLiving.GameEngine
{
    public class SchedulerConfigurationException : Exception
    {
        public SchedulerConfigurationException() { }

        public SchedulerConfigurationException(string message) : base(message) { }

        // TODO: Add SerializationInfo ctors
    }
}
