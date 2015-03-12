using FluentAssertions;
using Microsoft.Framework.OptionsModel;
using Moq;
using System;

namespace CleanLiving.GameEngine.Tests
{
    public class SchedulerTests
    {
        [UnitTest]
        public void WhenOptionsNotSuppliedThenThrowsException()
        {
            var config = new Mock<IOptions<SchedulerConfiguration>>();
            config.SetupGet(m => m.Options).Returns((SchedulerConfiguration)null);
            Action act = () => new Scheduler(config.Object);
            act.ShouldThrow<SchedulerConfigurationException>();
        }

        [UnitTest]
        public void WhenGameTicksPerSecondIsNegativeThenThrowsException()
        {
            SetupSchedulerWithGameTicksPerSecondSetTo(-1)
                .ShouldThrow<InvalidSchedulerConfigurationException>();
        }

        [UnitTest]
        public void WhenGameTicksPerSecondIsZeroThenThrowsException()
        {
            SetupSchedulerWithGameTicksPerSecondSetTo(0)
                .ShouldThrow<InvalidSchedulerConfigurationException>();
        }

        private Action SetupSchedulerWithGameTicksPerSecondSetTo(int gameTicksPerSecond)
        {
            var config = SetupConfig(gameTicksPerSecond);
            return () => new Scheduler(config);
        }

        private IOptions<SchedulerConfiguration> SetupConfig(int gameTicksPerSecond)
        {
            var config = new Mock<IOptions<SchedulerConfiguration>>();
            config.SetupGet(m => m.Options).Returns(
                new SchedulerConfiguration { GameTicksPerSecond = gameTicksPerSecond }
            );
            return config.Object;
        }
    }
}
