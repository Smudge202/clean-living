namespace CleanLiving.GameEngine
{
    public class InvalidSchedulerConfigurationException : SchedulerConfigurationException
    {
        public InvalidSchedulerConfigurationException(string parameterName)
            : base($"{parameterName} is invalid.") { }
    }
}
